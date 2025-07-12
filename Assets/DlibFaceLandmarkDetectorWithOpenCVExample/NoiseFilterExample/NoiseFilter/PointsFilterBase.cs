using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Points Filter Base.
    /// v 2.0.0
    /// </summary>
    public abstract class PointsFilterBase : IDisposable
    {
        // Public Properties
        /// <summary>
        /// Gets or sets whether debug mode is enabled for drawing debug points.
        /// </summary>
        public bool IsDebugMode { get; set; } = false;

        // Protected Fields
        protected int _numberOfElements;
        protected bool _disposed = false;

        public PointsFilterBase(int numberOfElements)
        {
            this._numberOfElements = numberOfElements;
        }

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be null when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points. If null, a new array will be created.</param>
        /// <returns>Output points. Returns predicted points from previous state when srcPoints is null.</returns>
        public abstract Vec2f[] Process(Mat img, Vec2f[] srcPoints, Vec2f[] dstPoints = null);

#if NET_STANDARD_2_1

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be empty when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points span.</param>
        /// <returns>Output points span. Returns predicted points from previous state when srcPoints is empty.</returns>
        public abstract Span<Vec2f> Process(Mat img, ReadOnlySpan<Vec2f> srcPoints, Span<Vec2f> dstPoints);

#endif

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat.</param>
        /// <param name="srcPoints">Input points.</param>
        /// <param name="dstPoints">Output points.</param>
        /// <returns>Output points.</returns>
        public virtual List<Vector2> Process(Mat img, List<Vector2> srcPoints, List<Vector2> dstPoints = null)
        {
            ThrowIfDisposed();

            // Convert List<Vector2> to Vec2f[]
            Vec2f[] srcVec2f = new Vec2f[srcPoints.Count];
            if (srcPoints != null)
            {
                for (int i = 0; i < srcPoints.Count; i++)
                {
                    srcVec2f[i] = new Vec2f(srcPoints[i].x, srcPoints[i].y);
                }
            }

            // Convert dstPoints to Vec2f[] if provided
            Vec2f[] dstVec2f = null;
            if (dstPoints != null)
            {
                dstVec2f = new Vec2f[dstPoints.Count];
                for (int i = 0; i < dstPoints.Count; i++)
                {
                    dstVec2f[i] = new Vec2f(dstPoints[i].x, dstPoints[i].y);
                }
            }

            // Call the abstract Vec2f[] Process method
            Vec2f[] resultVec2f = Process(img, srcVec2f, dstVec2f);

            // If dstPoints was provided, write the result to it
            if (dstPoints != null)
            {
                // Clear the list and add results
                dstPoints.Clear();
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    dstPoints.Add(new Vector2(resultVec2f[i].Item1, resultVec2f[i].Item2));
                }

                return dstPoints;
            }
            else
            {
                // Convert result back to new List<Vector2>
                List<Vector2> result = new List<Vector2>(resultVec2f.Length);
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    result.Add(new Vector2(resultVec2f[i].Item1, resultVec2f[i].Item2));
                }

                return result;
            }
        }

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be null when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points. If null, a new array will be created.</param>
        /// <returns>Output points. Returns predicted points from previous state when srcPoints is null.</returns>
        public virtual List<(double x, double y)> Process(Mat img, List<(double x, double y)> srcPoints, List<(double x, double y)> dstPoints = null)
        {
            ThrowIfDisposed();

            // Convert List<(double x, double y)> to Vec2f[]
            Vec2f[] srcVec2f = new Vec2f[srcPoints.Count];
            if (srcPoints != null)
            {
                for (int i = 0; i < srcPoints.Count; i++)
                {
                    srcVec2f[i] = new Vec2f((float)srcPoints[i].x, (float)srcPoints[i].y);
                }
            }

            // Convert dstPoints to Vec2f[] if provided
            Vec2f[] dstVec2f = null;
            if (dstPoints != null)
            {
                dstVec2f = new Vec2f[dstPoints.Count];
                for (int i = 0; i < dstPoints.Count; i++)
                {
                    dstVec2f[i] = new Vec2f((float)dstPoints[i].x, (float)dstPoints[i].y);
                }
            }

            // Call the abstract Vec2f[] Process method
            Vec2f[] resultVec2f = Process(img, srcVec2f, dstVec2f);

            // If dstPoints was provided, write the result to it
            if (dstPoints != null)
            {
                // Clear the list and add results
                dstPoints.Clear();
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    dstPoints.Add((resultVec2f[i].Item1, resultVec2f[i].Item2));
                }

                return dstPoints;
            }
            else
            {
                // Convert result back to new List<(double x, double y)>
                List<(double x, double y)> result = new List<(double x, double y)>(resultVec2f.Length);
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    result.Add((resultVec2f[i].Item1, resultVec2f[i].Item2));
                }

                return result;
            }
        }

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be null when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points. If null, a new array will be created.</param>
        /// <returns>Output points. Returns predicted points from previous state when srcPoints is null.</returns>
        public virtual float[] Process(Mat img, float[] srcPoints, float[] dstPoints = null)
        {
            ThrowIfDisposed();

            if (srcPoints != null && srcPoints.Length % 2 != 0)
                throw new ArgumentException("srcPoints.Length must be even", nameof(srcPoints));

            // Convert float[] to Vec2f[]
            Vec2f[] srcVec2f = new Vec2f[srcPoints.Length / 2];
            if (srcPoints != null)
            {
                for (int i = 0; i < srcVec2f.Length; i++)
                {
                    srcVec2f[i] = new Vec2f(srcPoints[i * 2], srcPoints[i * 2 + 1]);
                }
            }

            // Convert dstPoints to Vec2f[] if provided
            Vec2f[] dstVec2f = null;
            if (dstPoints != null)
            {
                if (dstPoints.Length % 2 != 0)
                    throw new ArgumentException("dstPoints.Length must be even", nameof(dstPoints));

                dstVec2f = new Vec2f[dstPoints.Length / 2];
                for (int i = 0; i < dstVec2f.Length; i++)
                {
                    dstVec2f[i] = new Vec2f(dstPoints[i * 2], dstPoints[i * 2 + 1]);
                }
            }

            // Call the abstract Vec2f[] Process method
            Vec2f[] resultVec2f = Process(img, srcVec2f, dstVec2f);

            // If dstPoints was provided, write the result to it
            if (dstPoints != null)
            {
                // Ensure dstPoints has enough capacity
                if (dstPoints.Length < resultVec2f.Length * 2)
                {
                    throw new ArgumentException($"dstPoints.Length ({dstPoints.Length}) must be at least {resultVec2f.Length * 2}", nameof(dstPoints));
                }

                // Write results to dstPoints
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    dstPoints[i * 2] = resultVec2f[i].Item1;
                    dstPoints[i * 2 + 1] = resultVec2f[i].Item2;
                }

                return dstPoints;
            }
            else
            {
                // Convert result back to new float[]
                float[] result = new float[resultVec2f.Length * 2];
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    result[i * 2] = resultVec2f[i].Item1;
                    result[i * 2 + 1] = resultVec2f[i].Item2;
                }

                return result;
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
        public virtual ReadOnlySpan<float> Process(Mat img, ReadOnlySpan<float> srcPoints, Span<float> dstPoints)
        {
            ThrowIfDisposed();

            if (srcPoints != null && srcPoints.Length % 2 != 0)
                throw new ArgumentException("srcPoints.Length must be even", nameof(srcPoints));

            if (dstPoints != null && dstPoints.Length % 2 != 0)
                throw new ArgumentException("dstPoints.Length must be even", nameof(dstPoints));

            // Use MemoryMarshal for optimized conversion
            ReadOnlySpan<Vec2f> srcVec2fSpan = null;
            if (srcPoints != null)
            {
                srcVec2fSpan = MemoryMarshal.Cast<float, Vec2f>(srcPoints);
            }
            Span<Vec2f> dstVec2fSpan = null;
            if (dstPoints != null)
            {
                dstVec2fSpan = MemoryMarshal.Cast<float, Vec2f>(dstPoints);
            }

            // Call the abstract Span<Vec2f> Process method
            Span<Vec2f> resultVec2fSpan = Process(img, srcVec2fSpan, dstVec2fSpan);

            // Use MemoryMarshal for optimized conversion
            Span<float> resultSpan = MemoryMarshal.Cast<Vec2f, float>(resultVec2fSpan);

            return resultSpan;
        }

#endif


        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat for processing (used for optical flow, debug drawing, etc.).</param>
        /// <param name="srcPoints">Input points. Can be null when no detection is available (will use previous state for prediction).</param>
        /// <param name="dstPoints">Output points. If null, a new array will be created.</param>
        /// <returns>Output points. Returns predicted points from previous state when srcPoints is null.</returns>
        public virtual double[] Process(Mat img, double[] srcPoints, double[] dstPoints = null)
        {
            ThrowIfDisposed();

            if (srcPoints != null && srcPoints.Length % 2 != 0)
                throw new ArgumentException("srcPoints.Length must be even", nameof(srcPoints));

            // Convert double[] to Vec2f[]
            Vec2f[] srcVec2f = new Vec2f[srcPoints.Length / 2];
            if (srcPoints != null)
            {
                for (int i = 0; i < srcVec2f.Length; i++)
                {
                    srcVec2f[i] = new Vec2f((float)srcPoints[i * 2], (float)srcPoints[i * 2 + 1]);
                }
            }

            // Convert dstPoints to Vec2f[] if provided
            Vec2f[] dstVec2f = null;
            if (dstPoints != null)
            {
                if (dstPoints.Length % 2 != 0)
                    throw new ArgumentException("dstPoints.Length must be even", nameof(dstPoints));

                dstVec2f = new Vec2f[dstPoints.Length / 2];
                for (int i = 0; i < dstVec2f.Length; i++)
                {
                    dstVec2f[i] = new Vec2f((float)dstPoints[i * 2], (float)dstPoints[i * 2 + 1]);
                }
            }

            // Call the abstract Vec2f[] Process method
            Vec2f[] resultVec2f = Process(img, srcVec2f, dstVec2f);

            // If dstPoints was provided, write the result to it
            if (dstPoints != null)
            {
                // Ensure dstPoints has enough capacity
                if (dstPoints.Length < resultVec2f.Length * 2)
                {
                    throw new ArgumentException($"dstPoints.Length ({dstPoints.Length}) must be at least {resultVec2f.Length * 2}", nameof(dstPoints));
                }

                // Write results to dstPoints
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    dstPoints[i * 2] = resultVec2f[i].Item1;
                    dstPoints[i * 2 + 1] = resultVec2f[i].Item2;
                }

                return dstPoints;
            }
            else
            {
                // Convert result back to new double[]
                double[] result = new double[resultVec2f.Length * 2];
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    result[i * 2] = resultVec2f[i].Item1;
                    result[i * 2 + 1] = resultVec2f[i].Item2;
                }

                return result;
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
        public virtual Span<double> Process(Mat img, ReadOnlySpan<double> srcPoints, Span<double> dstPoints)
        {
            ThrowIfDisposed();

            if (srcPoints != null && srcPoints.Length % 2 != 0)
                throw new ArgumentException("srcPoints.Length must be even", nameof(srcPoints));

            if (dstPoints != null && dstPoints.Length % 2 != 0)
                throw new ArgumentException("dstPoints.Length must be even", nameof(dstPoints));

            // Convert ReadOnlySpan<double> to Vec2f[]
            Vec2f[] srcVec2f = new Vec2f[srcPoints.Length / 2];
            for (int i = 0; i < srcVec2f.Length; i++)
            {
                srcVec2f[i] = new Vec2f((float)srcPoints[i * 2], (float)srcPoints[i * 2 + 1]);
            }

            // Convert dstPoints to Vec2f[] for processing
            Vec2f[] dstVec2f = new Vec2f[dstPoints.Length / 2];
            for (int i = 0; i < dstVec2f.Length; i++)
            {
                dstVec2f[i] = new Vec2f((float)dstPoints[i * 2], (float)dstPoints[i * 2 + 1]);
            }

            // Call the abstract Vec2f[] Process method
            Vec2f[] resultVec2f = Process(img, srcVec2f, dstVec2f);

            // If dstPoints was provided, write the result to it
            if (dstPoints != null)
            {
                // Ensure dstPoints has enough capacity
                if (dstPoints.Length < resultVec2f.Length * 2)
                {
                    throw new ArgumentException($"dstPoints.Length ({dstPoints.Length}) must be at least {resultVec2f.Length * 2}", nameof(dstPoints));
                }

                // Write results to dstPoints
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    dstPoints[i * 2] = resultVec2f[i].Item1;
                    dstPoints[i * 2 + 1] = resultVec2f[i].Item2;
                }

                return dstPoints;
            }
            else
            {
                // Convert result back to new double[]
                double[] result = new double[resultVec2f.Length * 2];
                for (int i = 0; i < resultVec2f.Length; i++)
                {
                    result[i * 2] = resultVec2f[i].Item1;
                    result[i * 2 + 1] = resultVec2f[i].Item2;
                }

                return result;
            }
        }

#endif

        /// <summary>
        /// Resets filter.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="PointsFilterBase"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here

                }

                // Dispose unmanaged resources here

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer for the <see cref="PointsFilterBase"/> class.
        /// </summary>
        ~PointsFilterBase()
        {
            Dispose(false);
        }

        // Protected Methods
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PointsFilterBase));
            }
        }

        // This function is to calculate the variance
        protected virtual double CalDistanceDiff(Vec2f[] curPoints, Vec2f[] lastPoints)
        {
            double variance = 0.0;
            double sum = 0.0;
            if (curPoints.Length == lastPoints.Length)
            {
                double[] diffs = new double[curPoints.Length];
                for (int i = 0; i < curPoints.Length; i++)
                {
                    Vec2f curPoint = curPoints[i];
                    Vec2f lastPoint = lastPoints[i];
                    double diff = Math.Sqrt(Math.Pow(curPoint.Item1 - lastPoint.Item1, 2.0) + Math.Pow(curPoint.Item2 - lastPoint.Item2, 2.0));
                    sum += diff;
                    diffs[i] = diff;
                }
                double mean = sum / curPoints.Length;
                for (int i = 0; i < curPoints.Length; i++)
                {
                    variance += Math.Pow(diffs[i] - mean, 2);
                }
                return variance / curPoints.Length;
            }
            return variance;
        }

#if NET_STANDARD_2_1
        // This function is to calculate the variance
        protected virtual double CalDistanceDiff(ReadOnlySpan<Vec2f> curPoints, ReadOnlySpan<Vec2f> lastPoints)
        {
            double variance = 0.0;
            double sum = 0.0;
            if (curPoints.Length == lastPoints.Length)
            {
                double[] diffs = new double[curPoints.Length];
                for (int i = 0; i < curPoints.Length; i++)
                {
                    Vec2f curPoint = curPoints[i];
                    Vec2f lastPoint = lastPoints[i];
                    double diff = Math.Sqrt(Math.Pow(curPoint.Item1 - lastPoint.Item1, 2.0) + Math.Pow(curPoint.Item2 - lastPoint.Item2, 2.0));
                    sum += diff;
                    diffs[i] = diff;
                }
                double mean = sum / curPoints.Length;
                for (int i = 0; i < curPoints.Length; i++)
                {
                    variance += Math.Pow(diffs[i] - mean, 2);
                }
                return variance / curPoints.Length;
            }
            return variance;
        }
#endif

        protected virtual void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }

        /// <summary>
        /// Calculates the bounding rectangle for a set of Vec2f points.
        /// This is equivalent to Imgproc.boundingRect but works with Vec2f instead of Vec2i.
        /// </summary>
        /// <param name="points">Array of Vec2f points.</param>
        /// <returns>Bounding rectangle as (x, y, width, height).</returns>
        protected virtual (float x, float y, float width, float height) CalculateBoundingRect(Vec2f[] points)
        {
            if (points == null || points.Length == 0)
                return (0, 0, 0, 0);

            float minX = points[0].Item1;
            float maxX = points[0].Item1;
            float minY = points[0].Item2;
            float maxY = points[0].Item2;

            for (int i = 1; i < points.Length; i++)
            {
                float x = points[i].Item1;
                float y = points[i].Item2;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            return (minX, minY, maxX - minX, maxY - minY);
        }


#if NET_STANDARD_2_1
        /// <summary>
        /// Calculates the bounding rectangle for a set of Vec2f points using ReadOnlySpan.
        /// This is equivalent to Imgproc.boundingRect but works with Vec2f instead of Vec2i.
        /// </summary>
        /// <param name="points">ReadOnlySpan of Vec2f points.</param>
        /// <returns>Bounding rectangle as (x, y, width, height).</returns>
        protected virtual (float x, float y, float width, float height) CalculateBoundingRect(ReadOnlySpan<Vec2f> points)
        {
            if (points.Length == 0)
                return (0, 0, 0, 0);

            float minX = points[0].Item1;
            float maxX = points[0].Item1;
            float minY = points[0].Item2;
            float maxY = points[0].Item2;

            for (int i = 1; i < points.Length; i++)
            {
                float x = points[i].Item1;
                float y = points[i].Item2;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            return (minX, minY, maxX - minX, maxY - minY);
        }
#endif
    }
}
