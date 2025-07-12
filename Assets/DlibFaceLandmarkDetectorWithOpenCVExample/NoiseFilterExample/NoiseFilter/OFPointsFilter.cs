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
    /// Optical Flow Points Filter.
    /// v 2.0.0
    /// </summary>
    public class OFPointsFilter : PointsFilterBase
    {
        // Constants
        private const double DEFAULT_DIFF_CHECK_SENSITIVITY = 1;
        private const double DEFAULT_DIFF_DLIB = 1;

        // Adaptive threshold constants
        private const double BASE_DIFF_THRESHOLD = 10.0; // Base threshold per point
        private const double MIN_AREA_FACTOR = 0.01;     // Minimum area factor
        private const double MAX_AREA_FACTOR = 1.0;      // Maximum area factor
        private const double POINT_DENSITY_FACTOR = 1.0; // Point density compensation

        // Color constants for debug drawing
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_DLIB = new Scalar(255, 0, 0, 255).ToValueTuple();
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_OPTICAL = new Scalar(0, 0, 255, 255).ToValueTuple();

        // Public Fields
        public double DiffCheckSensitivity = DEFAULT_DIFF_CHECK_SENSITIVITY;

        // Private Fields
        private bool _flag = false;
        private double _diffDlib = DEFAULT_DIFF_DLIB;
        private MatOfPoint _prevTrackPtsMat;

        // Optical Flow
        private Mat _prevgray, _gray;
        private Vec2f[] _prevTrackPts;
        private Vec2f[] _nextTrackPts;
        private MatOfPoint2f _mOP2fPrevTrackPts;
        private MatOfPoint2f _mOP2fNextTrackPts;
        private MatOfByte _status;
        private MatOfFloat _err;

        public OFPointsFilter(int numberOfElements) : base(numberOfElements)
        {
            // Calculate adaptive threshold based on number of elements
            _diffDlib = CalculateAdaptiveThreshold(numberOfElements);
            _prevTrackPtsMat = new MatOfPoint();

            // Initialize Optical Flow
            InitializeOpticalFlow();
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

            // If no detection, predict from previous state using optical flow
            if (srcPoints == null)
            {
                if (_flag && _prevgray.total() > 0)
                {
                    // Use previous prediction result and new image for optical flow processing
                    if (img.channels() == 4)
                    {
                        Imgproc.cvtColor(img, _gray, Imgproc.COLOR_RGBA2GRAY);
                    }
                    else if (img.channels() == 3)
                    {
                        Imgproc.cvtColor(img, _gray, Imgproc.COLOR_RGB2GRAY);
                    }
                    else
                    {
                        if (_gray.total() == 0)
                        {
                            _gray = img.clone();
                        }
                        else
                        {
                            img.copyTo(_gray);
                        }
                    }

                    _mOP2fPrevTrackPts.fromVec2fArray(_prevTrackPts);
                    Video.calcOpticalFlowPyrLK(_prevgray, _gray, _mOP2fPrevTrackPts, _mOP2fNextTrackPts, _status, _err);
                    _nextTrackPts = _mOP2fNextTrackPts.toVec2fArray();

                    // Check optical flow calculation success
                    byte[] statusArray = _status.toArray();
                    bool hasFailedPoints = false;
                    for (int i = 0; i < statusArray.Length; i++)
                    {
                        if (statusArray[i] == 0) // Failed tracking
                        {
                            hasFailedPoints = true;
                            break;
                        }
                    }

                    if (hasFailedPoints)
                    {
                        // If optical flow failed, use previous prediction for failed points
                        if (IsDebugMode)
                            Debug.Log("Optical Flow failed, using previous prediction");

                        // Replace failed points with previous prediction
                        for (int i = 0; i < statusArray.Length; i++)
                        {
                            if (statusArray[i] == 0) // Failed tracking
                            {
                                _nextTrackPts[i] = _prevTrackPts[i]; // Use previous position
                            }
                        }
                    }

#if NET_STANDARD_2_1
                    _nextTrackPts.CopyTo(dstPoints);
#else
                    Array.Copy(_nextTrackPts, dstPoints, _numberOfElements);
#endif

                    if (IsDebugMode)
                    {
                        Debug.Log("Optical Flow (No Detection)");
                        for (int i = 0; i < _numberOfElements; i++)
                        {
                            Imgproc.circle(img, (_nextTrackPts[i].Item1, _nextTrackPts[i].Item2), 2, DEBUG_COLOR_OPTICAL, -1);
                        }
                    }

                    Swap(ref _prevTrackPts, ref _nextTrackPts);
                    Swap(ref _prevgray, ref _gray);
                }
                else
                {
                    // If not initialized, return previous result
#if NET_STANDARD_2_1
                    _prevTrackPts.CopyTo(dstPoints);
#else
                    Array.Copy(_prevTrackPts, dstPoints, _numberOfElements);
#endif
                }

                return dstPoints;
            }

            if (!_flag)
            {
                if (img.channels() == 4)
                {
                    Imgproc.cvtColor(img, _prevgray, Imgproc.COLOR_RGBA2GRAY);
                }
                else if (img.channels() == 3)
                {
                    Imgproc.cvtColor(img, _prevgray, Imgproc.COLOR_RGB2GRAY);
                }
                else
                {
                    if (_prevgray.total() == 0)
                    {
                        _prevgray = img.clone();
                    }
                    else
                    {
                        img.copyTo(_prevgray);
                    }
                }

#if NET_STANDARD_2_1
                srcPoints.CopyTo(_prevTrackPts);
#else
                Array.Copy(srcPoints, _prevTrackPts, _numberOfElements);
#endif

                _flag = true;
            }

            if (img.channels() == 4)
            {
                Imgproc.cvtColor(img, _gray, Imgproc.COLOR_RGBA2GRAY);
            }
            else if (img.channels() == 3)
            {
                Imgproc.cvtColor(img, _gray, Imgproc.COLOR_RGB2GRAY);
            }
            else
            {
                if (_gray.total() == 0)
                {
                    _gray = img.clone();
                }
                else
                {
                    img.copyTo(_gray);
                }
            }

            if (_prevgray.total() > 0)
            {
                _mOP2fPrevTrackPts.fromVec2fArray(_prevTrackPts);
                Video.calcOpticalFlowPyrLK(_prevgray, _gray, _mOP2fPrevTrackPts, _mOP2fNextTrackPts, _status, _err);
                _nextTrackPts = _mOP2fNextTrackPts.toVec2fArray();

                // Check optical flow calculation success
                byte[] statusArray = _status.toArray();
                bool hasFailedPoints = false;
                for (int i = 0; i < statusArray.Length; i++)
                {
                    if (statusArray[i] == 0) // Failed tracking
                    {
                        hasFailedPoints = true;
                        break;
                    }
                }

                if (hasFailedPoints)
                {
                    // If optical flow failed, use previous prediction for failed points
                    if (IsDebugMode)
                        Debug.Log("Optical Flow failed, using previous prediction");

                    // Replace failed points with previous prediction
                    for (int i = 0; i < statusArray.Length; i++)
                    {
                        if (statusArray[i] == 0) // Failed tracking
                        {
                            _nextTrackPts[i] = _prevTrackPts[i]; // Use previous position
                        }
                    }
                }

                // Calculate adaptive diffDlib threshold
                var rect = CalculateBoundingRect(srcPoints);
                double diffDlib = CalculateAdaptiveDiffThreshold(rect, DiffCheckSensitivity);

                // if the face is moving so fast, use dlib to detect the face
                double diff = CalDistanceDiff(_prevTrackPts, _nextTrackPts);
                if (IsDebugMode)
                    Debug.Log("variance:" + diff + " diffDlib:" + diffDlib);
                if (diff > diffDlib)
                {
#if NET_STANDARD_2_1
                    srcPoints.CopyTo(_nextTrackPts);
                    srcPoints.CopyTo(dstPoints);
#else
                    Array.Copy(srcPoints, _nextTrackPts, _numberOfElements);
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
                }
                else
                {
                    // In this case, use Optical Flow
#if NET_STANDARD_2_1
                    _nextTrackPts.CopyTo(dstPoints);
#else
                    Array.Copy(_nextTrackPts, dstPoints, _numberOfElements);
#endif

                    if (IsDebugMode)
                    {
                        Debug.Log("Optical Flow");
                        for (int i = 0; i < _numberOfElements; i++)
                        {
                            Imgproc.circle(img, (_nextTrackPts[i].Item1, _nextTrackPts[i].Item2), 2, DEBUG_COLOR_OPTICAL, -1);
                        }
                    }
                }

                Swap(ref _prevTrackPts, ref _nextTrackPts);
                Swap(ref _prevgray, ref _gray);
            }
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

            _flag = false;

            // Reset Optical Flow
            for (int i = 0; i < _numberOfElements; i++)
            {
                _prevTrackPts[i].Item1 = 0f;
                _prevTrackPts[i].Item2 = 0f;
            }
            for (int i = 0; i < _numberOfElements; i++)
            {
                _nextTrackPts[i].Item1 = 0f;
                _nextTrackPts[i].Item2 = 0f;
            }

            _prevgray?.Dispose(); _prevgray = new Mat();
            _gray?.Dispose(); _gray = new Mat();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                DisposeOpticalFlow();

                _prevTrackPtsMat?.Dispose(); _prevTrackPtsMat = null;
            }

            base.Dispose(disposing);
        }

        // Private Methods
        protected virtual void InitializeOpticalFlow()
        {
            _prevTrackPts = new Vec2f[_numberOfElements];
            for (int i = 0; i < _numberOfElements; i++)
            {
                _prevTrackPts[i] = new Vec2f();
            }
            _nextTrackPts = new Vec2f[_numberOfElements];
            for (int i = 0; i < _numberOfElements; i++)
            {
                _nextTrackPts[i] = new Vec2f();
            }
            _prevgray = new Mat();
            _gray = new Mat();
            _mOP2fPrevTrackPts = new MatOfPoint2f(_prevTrackPts);
            _mOP2fNextTrackPts = new MatOfPoint2f(_nextTrackPts);
            _status = new MatOfByte();
            _err = new MatOfFloat();
        }

        protected virtual void DisposeOpticalFlow()
        {
            _prevTrackPts = null;
            _nextTrackPts = null;
            _prevgray?.Dispose(); _prevgray = null;
            _gray?.Dispose(); _gray = null;
            _mOP2fPrevTrackPts?.Dispose(); _mOP2fPrevTrackPts = null;
            _mOP2fNextTrackPts?.Dispose(); _mOP2fNextTrackPts = null;
            _status?.Dispose(); _status = null;
            _err?.Dispose(); _err = null;
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
