using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoModule;
using UnityEngine;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Kalman Filter Points Filter.
    /// v 2.0.0
    /// </summary>
    public class KFPointsFilter : PointsFilterBase
    {
        // Constants
        private const double DEFAULT_DIFF_CHECK_SENSITIVITY = 1;
        private const double DEFAULT_DIFF_DLIB = 1;
        private const int DEFAULT_STATE_NUM = 272;
        private const int DEFAULT_MEASURE_NUM = 136;

        // Adaptive threshold constants
        private const double BASE_DIFF_THRESHOLD = 10.0; // Base threshold per point (increased)
        private const double MIN_AREA_FACTOR = 0.01;     // Minimum area factor (increased)
        private const double MAX_AREA_FACTOR = 1.0;      // Maximum area factor (increased)
        private const double POINT_DENSITY_FACTOR = 1.0; // Point density compensation (increased)

        // Color constants for debug drawing
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_DLIB = new Scalar(255, 0, 0, 255).ToValueTuple();
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_KALMAN = new Scalar(0, 255, 0, 255).ToValueTuple();

        // Public Fields
        public double DiffCheckSensitivity = DEFAULT_DIFF_CHECK_SENSITIVITY;

        // Private Fields
        private bool _flag = false;
        private double _diffDlib = DEFAULT_DIFF_DLIB;
        private MatOfPoint _prevTrackPtsMat;
        private Vec2f[] _lastPoints;

        // Kalman Filter
        private int _stateNum = DEFAULT_STATE_NUM;
        private int _measureNum = DEFAULT_MEASURE_NUM;
        private KalmanFilter _kf;
        private Mat _statePreMat;
        private Mat _statePreMat_32FC2;
        private Mat _statePostMat;
        private Mat _statePostMat_32FC2;
        private Mat _measurement;
        private Mat _measurement_32FC2;

        public KFPointsFilter(int numberOfElements) : base(numberOfElements)
        {
            // Calculate adaptive threshold based on number of elements
            _diffDlib = CalculateAdaptiveThreshold(numberOfElements);
            _prevTrackPtsMat = new MatOfPoint();

            _lastPoints = new Vec2f[numberOfElements];
            for (int i = 0; i < numberOfElements; i++)
            {
                _lastPoints[i] = new Vec2f();
            }

            // Initialize Kalman Filter
            _stateNum = numberOfElements * 4;
            _measureNum = numberOfElements * 2;
            InitializeKalmanFilter();
        }

#if NET_STANDARD_2_1
        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be empty when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points span.</param>
        /// <returns>Output points span. Returns predicted points from previous state when srcPoints is empty.</returns>
        public override Span<Vec2f> Process(Mat img, ReadOnlySpan<Vec2f> srcPoints, Span<Vec2f> dstPoints)
#else
        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be null when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points. If null, a new array will be created.</param>
        /// <returns>Output points. Returns predicted points from previous state when srcPoints is null.</returns>
        public override Vec2f[] Process(Mat img, Vec2f[] srcPoints, Vec2f[] dstPoints = null)
#endif
        {
            ThrowIfDisposed();

            if (srcPoints != null && srcPoints.Length != _numberOfElements)
                throw new ArgumentException("The number of srcPoints elements is different.");

            if (dstPoints != null && dstPoints.Length != _numberOfElements)
                throw new ArgumentException("The number of dstPoints elements is different.");

            if (dstPoints == null)
            {
                dstPoints = new Vec2f[_numberOfElements];
                for (int i = 0; i < _numberOfElements; i++)
                {
                    dstPoints[i] = new Vec2f();
                }
            }

            // If no detection, predict from previous state
            if (srcPoints == null)
            {
                if (_flag)
                {
                    // Kalman Prediction
                    _kf.predict();

                    // Get predicted result
                    using (Mat predicted = _kf.get_statePre())
                    using (Mat predicted_32FC2 = predicted.reshape(2))
                    {
                        predicted_32FC2.get(0, 0, dstPoints);
                    }

                    if (IsDebugMode)
                    {
                        Debug.Log("Kalman Prediction (no detection)");
                        for (int i = 0; i < _numberOfElements; i++)
                        {
                            Imgproc.circle(img, (dstPoints[i].Item1, dstPoints[i].Item2), 2, DEBUG_COLOR_KALMAN, -1);
                        }
                    }
                }
                else
                {
#if NET_STANDARD_2_1
                    _lastPoints.CopyTo(dstPoints);
#else
                    Array.Copy(_lastPoints, dstPoints, _numberOfElements);
#endif
                }

                return dstPoints;
            }

            // Calculate adaptive diffDlib threshold
            var rect = CalculateBoundingRect(srcPoints);
            double diffDlib = CalculateAdaptiveDiffThreshold(rect, DiffCheckSensitivity);

            // if the face is moving so fast, use dlib to detect the face
            double diff = CalDistanceDiff(srcPoints, _lastPoints);
            if (IsDebugMode)
                Debug.Log("variance:" + diff + " diffDlib:" + diffDlib);
            if (diff > diffDlib)
            {
#if NET_STANDARD_2_1
                srcPoints.CopyTo(dstPoints);
#else
                Array.Copy(srcPoints, dstPoints, _numberOfElements);
#endif

                if (IsDebugMode)
                {
                    Debug.Log("DLIB");
                    for (int i = 0; i < _numberOfElements; i++)
                    {
                        Imgproc.circle(img, (srcPoints[i].Item1, srcPoints[i].Item2), 2, DEBUG_COLOR_DLIB, -1);
                    }
                }

                _flag = false;
            }
            else
            {
                if (!_flag)
                {
                    // Set initial state estimate.
                    _statePreMat_32FC2.put(0, 0, srcPoints);
                    _statePostMat_32FC2.put(0, 0, srcPoints);

                    _flag = true;
                }

                // Kalman Prediction
                _kf.predict();

                // Update Measurement
                _measurement_32FC2.put(0, 0, srcPoints);

                // Correct Measurement
                using (Mat estimated = _kf.correct(_measurement))
                using (Mat estimated_32FC2 = estimated.reshape(2))
                {
                    estimated_32FC2.get(0, 0, dstPoints);
                }

                if (IsDebugMode)
                {
                    Debug.Log("Kalman Filter");
                    for (int i = 0; i < _numberOfElements; i++)
                    {
                        Imgproc.circle(img, (dstPoints[i].Item1, dstPoints[i].Item2), 2, DEBUG_COLOR_KALMAN, -1);
                    }
                }
            }

            // Update last points for next iteration
            // Note: _lastPoints is used for diff calculation, not for Kalman state
#if NET_STANDARD_2_1
            dstPoints.CopyTo(_lastPoints);
#else
            Array.Copy(dstPoints, _lastPoints, _numberOfElements);
#endif

            return dstPoints;
        }

#if NET_STANDARD_2_1
        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be null when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points. If null, a new array will be created.</param>
        /// <returns>Output points. Returns predicted points from previous state when srcPoints is null.</returns>
        public override Vec2f[] Process(Mat img, Vec2f[] srcPoints, Vec2f[] dstPoints = null)
        {
            ThrowIfDisposed();

            if (dstPoints == null)
            {
                dstPoints = new Vec2f[_numberOfElements];
                for (int i = 0; i < _numberOfElements; i++)
                {
                    dstPoints[i] = new Vec2f();
                }
            }
            Process(img, srcPoints.AsSpan(), dstPoints.AsSpan());

            return dstPoints;
        }
#endif

        /// <summary>
        /// Resets filter.
        /// </summary>
        public override void Reset()
        {
            ThrowIfDisposed();

            // Reset Kalman Filter
            _flag = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                DisposeKalmanFilter();

                _prevTrackPtsMat?.Dispose(); _prevTrackPtsMat = null;
            }

            base.Dispose(disposing);
        }

        // Private Methods
        protected virtual void InitializeKalmanFilter()
        {
            _kf = new KalmanFilter(_stateNum, _measureNum, 0, CvType.CV_32F);

            _statePreMat = _kf.get_statePre();
            _statePreMat_32FC2 = _statePreMat.reshape(2);
            _statePostMat = _kf.get_statePost();
            _statePostMat_32FC2 = _statePostMat.reshape(2);

            _measurement = Mat.zeros(_measureNum, 1, CvType.CV_32F);
            _measurement_32FC2 = _measurement.reshape(2);

            // Generate the Measurement Matrix
            _kf.set_transitionMatrix(Mat.zeros(_stateNum, _stateNum, CvType.CV_32F));
            for (int i = 0; i < _stateNum; i++)
            {
                for (int j = 0; j < _stateNum; j++)
                {
                    if (i == j || (j - _measureNum) == i)
                    {
                        _kf.get_transitionMatrix().put(i, j, new float[] { 1.0f });
                    }
                    else
                    {
                        _kf.get_transitionMatrix().put(i, j, new float[] { 0.0f });
                    }
                }
            }

            // measurement matrix (H)
            Core.setIdentity(_kf.get_measurementMatrix());
            // process noise covariance matrix (Q)
            Core.setIdentity(_kf.get_processNoiseCov(), Scalar.all(1e-5));
            // measurement noise covariance matrix (R)
            Core.setIdentity(_kf.get_measurementNoiseCov(), Scalar.all(1e-1));
            // posteriori error estimate covariance matrix (P(k)): P(k)=(I-K(k)*H)*P'(k)
            Core.setIdentity(_kf.get_errorCovPost(), Scalar.all(0.1));
        }

        protected virtual void DisposeKalmanFilter()
        {
            _lastPoints = null;
            _kf?.Dispose(); _kf = null;
            _statePreMat?.Dispose(); _statePreMat = null;
            _statePreMat_32FC2?.Dispose(); _statePreMat_32FC2 = null;
            _statePostMat?.Dispose(); _statePostMat = null;
            _statePostMat_32FC2?.Dispose(); _statePostMat_32FC2 = null;
            _measurement?.Dispose(); _measurement = null;
            _measurement_32FC2?.Dispose(); _measurement_32FC2 = null;
        }

        /// <summary>
        /// Calculates adaptive threshold based on number of elements.
        /// </summary>
        /// <param name="numberOfElements">Number of landmark points.</param>
        /// <returns>Adaptive threshold value.</returns>
        private double CalculateAdaptiveThreshold(int numberOfElements)
        {
            // Base threshold adjusted for point density
            double baseThreshold = BASE_DIFF_THRESHOLD;

            // Compensate for different point densities
            double pointDensityCompensation = Math.Sqrt(68.0 / numberOfElements) * POINT_DENSITY_FACTOR;

            return baseThreshold * pointDensityCompensation;
        }

        /// <summary>
        /// Calculates adaptive diff threshold based on face area and sensitivity.
        /// </summary>
        /// <param name="rect">Bounding rectangle of face points.</param>
        /// <param name="sensitivity">Sensitivity multiplier.</param>
        /// <returns>Adaptive diff threshold.</returns>
        private double CalculateAdaptiveDiffThreshold((float x, float y, float width, float height) rect, double sensitivity)
        {
            // Calculate face area
            double faceArea = rect.width * rect.height;

            // Normalize area factor (clamp between MIN and MAX)
            // Use smaller normalization factor for more reasonable thresholds
            double areaFactor = Math.Max(MIN_AREA_FACTOR, Math.Min(MAX_AREA_FACTOR, faceArea / 100000.0));

            // Calculate adaptive threshold
            double adaptiveThreshold = _diffDlib * areaFactor * sensitivity;

            // Apply point density compensation
            double pointDensityCompensation = Math.Sqrt(68.0 / _numberOfElements) * POINT_DENSITY_FACTOR;

            return adaptiveThreshold * pointDensityCompensation;
        }
    }
}
