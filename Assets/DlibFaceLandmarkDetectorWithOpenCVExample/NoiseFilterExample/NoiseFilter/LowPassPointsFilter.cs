using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Low Pass Points Filter.
    /// v 1.0.4
    /// </summary>
    public class LowPassPointsFilter : PointsFilterBase
    {
        public double diffLawPass = 2;

        bool flag = false;

        List<Vector2> lastPoints;

        public LowPassPointsFilter(int numberOfElements) : base(numberOfElements)
        {
            lastPoints = new List<Vector2>();
            for (int i = 0; i < numberOfElements; i++)
            {
                lastPoints.Add(new Vector2());
            }
        }

        /// <summary>
        /// Processes points by filter.
        /// </summary>
        /// <param name="img">Image mat.</param>
        /// <param name="srcPoints">Input points.</param>
        /// <param name="dstPoints">Output points.</param>
        /// <param name="drawDebugPoints">if true, draws debug points.</param>
        /// <returns>Output points.</returns>
        public override List<Vector2> Process(Mat img, List<Vector2> srcPoints, List<Vector2> dstPoints = null, bool drawDebugPoints = false)
        {
            if (srcPoints != null && srcPoints.Count != numberOfElements)
            {
                throw new ArgumentException("The number of elements is different.");
            }

            if (srcPoints != null)
            {

                if (dstPoints == null)
                {
                    dstPoints = new List<Vector2>();
                }
                if (dstPoints != null && dstPoints.Count != numberOfElements)
                {
                    dstPoints.Clear();
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        dstPoints.Add(new Vector2());
                    }
                }

                if (flag)
                {
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        double diff = Math.Sqrt(Math.Pow(srcPoints[i].x - lastPoints[i].x, 2.0) + Math.Pow(srcPoints[i].y - lastPoints[i].y, 2.0));
                        if (diff > diffLawPass)
                        {
                            lastPoints[i] = srcPoints[i];
                            if (drawDebugPoints)
                                Imgproc.circle(img, new Point(srcPoints[i].x, srcPoints[i].y), 1, new Scalar(0, 255, 0, 255), -1);
                        }
                        else
                        {
                            if (drawDebugPoints)
                                Imgproc.circle(img, new Point(lastPoints[i].x, lastPoints[i].y), 1, new Scalar(255, 0, 0, 255), -1);
                        }
                        dstPoints[i] = lastPoints[i];
                    }
                }
                else
                {
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        lastPoints[i] = srcPoints[i];
                        dstPoints[i] = srcPoints[i];
                    }
                    if (drawDebugPoints)
                    {
                        for (int i = 0; i < numberOfElements; i++)
                        {
                            Imgproc.circle(img, new Point(srcPoints[i].x, srcPoints[i].y), 1, new Scalar(0, 0, 255, 255), -1);
                        }
                    }
                    flag = true;
                }
                return dstPoints;
            }
            else
            {
                return dstPoints == null ? srcPoints : dstPoints;
            }
        }

        /// <summary>
        /// Resets filter.
        /// </summary>
        public override void Reset()
        {
            flag = false;
            for (int i = 0; i < lastPoints.Count; i++)
            {
                lastPoints[i] = new Vector2();
            }
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public override void Dispose()
        {
            if (lastPoints != null)
                lastPoints.Clear();
        }
    }
}