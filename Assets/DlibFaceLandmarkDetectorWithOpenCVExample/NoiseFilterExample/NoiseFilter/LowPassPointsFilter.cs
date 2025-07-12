using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Low Pass Points Filter.
    /// v 2.0.0
    /// </summary>
    public class LowPassPointsFilter : PointsFilterBase
    {
        // Constants
        private const double DEFAULT_DIFF_LOW_PASS = 2;

        // Color constants for debug drawing
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_FILTERED = new Scalar(0, 255, 0, 255).ToValueTuple();
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_UNFILTERED = new Scalar(255, 0, 0, 255).ToValueTuple();
        private static readonly (double v0, double v1, double v2, double v3) DEBUG_COLOR_INITIAL = new Scalar(0, 0, 255, 255).ToValueTuple();

        // Public Fields
        public double DiffLowPass = DEFAULT_DIFF_LOW_PASS;

        // Private Fields
        private bool _flag = false;
        private Vec2f[] _lastPoints;

        public LowPassPointsFilter(int numberOfElements) : base(numberOfElements)
        {
            _lastPoints = new Vec2f[numberOfElements];
            for (int i = 0; i < numberOfElements; i++)
            {
                _lastPoints[i] = new Vec2f();
            }
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

            if (srcPoints == null)
                return dstPoints == null ? _lastPoints : dstPoints;

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

            if (_flag)
            {
                for (int i = 0; i < _numberOfElements; i++)
                {
                    ref readonly Vec2f srcPoint = ref srcPoints[i];
                    ref Vec2f lastPoint = ref _lastPoints[i];
                    double diff = Math.Sqrt(Math.Pow(srcPoint.Item1 - lastPoint.Item1, 2.0) + Math.Pow(srcPoint.Item2 - lastPoint.Item2, 2.0));
                    if (diff > DiffLowPass)
                    {
                        lastPoint.Item1 = srcPoint.Item1;
                        lastPoint.Item2 = srcPoint.Item2;
                        if (IsDebugMode)
                            Imgproc.circle(img, (srcPoint.Item1, srcPoint.Item2), 1, DEBUG_COLOR_FILTERED, -1);
                    }
                    else
                    {
                        if (IsDebugMode)
                            Imgproc.circle(img, (lastPoint.Item1, lastPoint.Item2), 1, DEBUG_COLOR_UNFILTERED, -1);
                    }
                }
#if NET_STANDARD_2_1
                _lastPoints.CopyTo(dstPoints);
#else
                Array.Copy(_lastPoints, dstPoints, _numberOfElements);
#endif
            }
            else
            {
#if NET_STANDARD_2_1
                srcPoints.CopyTo(_lastPoints);
                srcPoints.CopyTo(dstPoints);
#else
                Array.Copy(srcPoints, _lastPoints, _numberOfElements);
                Array.Copy(srcPoints, dstPoints, _numberOfElements);
#endif

                if (IsDebugMode)
                {
                    for (int i = 0; i < _numberOfElements; i++)
                    {
                        Vec2f srcPoint = srcPoints[i];
                        Imgproc.circle(img, (srcPoint.Item1, srcPoint.Item2), 1, DEBUG_COLOR_INITIAL, -1);
                    }
                }
                _flag = true;
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
            for (int i = 0; i < _numberOfElements; i++)
            {
                _lastPoints[i] = new Vec2f();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _lastPoints = null;
            }

            base.Dispose(disposing);
        }
    }
}
