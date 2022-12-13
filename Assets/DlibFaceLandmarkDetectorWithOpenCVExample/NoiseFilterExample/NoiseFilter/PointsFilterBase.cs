using OpenCVForUnity.CoreModule;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Points Filter Base.
    /// v 1.0.4
    /// </summary>
    public abstract class PointsFilterBase
    {
        protected int numberOfElements;

        public PointsFilterBase(int numberOfElements)
        {
            this.numberOfElements = numberOfElements;
        }

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat.</param>
        /// <param name="srcPoints">Input points.</param>
        /// <param name="dstPoints">Output points.</param>
        /// <param name="drawDebugPoints">if true, draws debug points.</param>
        /// <returns>Output points.</returns>
        public abstract List<Vector2> Process(Mat img, List<Vector2> srcPoints, List<Vector2> dstPoints = null, bool drawDebugPoints = false);

        /// <summary>
        /// Resets filter.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public abstract void Dispose();

        // This function is to calculate the variance
        protected virtual double calDistanceDiff(IList<Point> curPoints, IList<Point> lastPoints)
        {
            double variance = 0.0;
            double sum = 0.0;
            List<double> diffs = new List<double>();
            if (curPoints.Count == lastPoints.Count)
            {
                for (int i = 0; i < curPoints.Count; i++)
                {
                    double diff = Math.Sqrt(Math.Pow(curPoints[i].x - lastPoints[i].x, 2.0) + Math.Pow(curPoints[i].y - lastPoints[i].y, 2.0));
                    sum += diff;
                    diffs.Add(diff);
                }
                double mean = sum / diffs.Count;
                for (int i = 0; i < curPoints.Count; i++)
                {
                    variance += Math.Pow(diffs[i] - mean, 2);
                }
                return variance / diffs.Count;
            }
            return variance;
        }

        protected virtual void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }
    }
}