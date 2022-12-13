using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.VideoModule;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Optical Flow Points Filter.
    /// v 1.0.4
    /// </summary>
    public class OFPointsFilter : PointsFilterBase
    {
        public double diffCheckSensitivity = 1;

        bool flag = false;
        double diffDlib = 1;
        MatOfPoint prevTrackPtsMat;

        // Optical Flow
        Mat prevgray, gray;
        List<Point> prevTrackPts;
        List<Point> nextTrackPts;
        MatOfPoint2f mOP2fPrevTrackPts;
        MatOfPoint2f mOP2fNextTrackPts;
        MatOfByte status;
        MatOfFloat err;

        public OFPointsFilter(int numberOfElements) : base(numberOfElements)
        {
            diffDlib = diffDlib * (double)numberOfElements / 68.0;
            prevTrackPtsMat = new MatOfPoint();

            // Initialize Optical Flow
            InitializeOpticalFlow();
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

            if (srcPoints == null)
            {
                return dstPoints == null ? srcPoints : dstPoints;
            }

            if (!flag)
            {
                if (img.channels() == 4)
                {
                    Imgproc.cvtColor(img, prevgray, Imgproc.COLOR_RGBA2GRAY);
                }
                else if (img.channels() == 3)
                {
                    Imgproc.cvtColor(img, prevgray, Imgproc.COLOR_RGB2GRAY);
                }
                else
                {
                    if (prevgray.total() == 0)
                    {
                        prevgray = img.clone();
                    }
                    else
                    {
                        img.copyTo(prevgray);
                    }
                }

                for (int i = 0; i < numberOfElements; i++)
                {
                    prevTrackPts[i] = new Point(srcPoints[i].x, srcPoints[i].y);
                }

                flag = true;
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

                if (img.channels() == 4)
                {
                    Imgproc.cvtColor(img, gray, Imgproc.COLOR_RGBA2GRAY);
                }
                else if (img.channels() == 3)
                {
                    Imgproc.cvtColor(img, gray, Imgproc.COLOR_RGB2GRAY);
                }
                else
                {
                    if (gray.total() == 0)
                    {
                        gray = img.clone();
                    }
                    else
                    {
                        img.copyTo(gray);
                    }
                }

                if (prevgray.total() > 0)
                {
                    mOP2fPrevTrackPts.fromList(prevTrackPts);
                    mOP2fNextTrackPts.fromList(nextTrackPts);
                    Video.calcOpticalFlowPyrLK(prevgray, gray, mOP2fPrevTrackPts, mOP2fNextTrackPts, status, err);
                    prevTrackPts = mOP2fPrevTrackPts.toList();
                    nextTrackPts = mOP2fNextTrackPts.toList();

                    // clac diffDlib
                    prevTrackPtsMat.fromList(prevTrackPts);
                    OpenCVForUnity.CoreModule.Rect rect = Imgproc.boundingRect(prevTrackPtsMat);
                    double diffDlib = this.diffDlib * rect.area() / 40000.0 * diffCheckSensitivity;

                    // if the face is moving so fast, use dlib to detect the face
                    double diff = calDistanceDiff(prevTrackPts, nextTrackPts);
                    if (drawDebugPoints)
                        Debug.Log("variance:" + diff);
                    if (diff > diffDlib)
                    {
                        for (int i = 0; i < numberOfElements; i++)
                        {
                            nextTrackPts[i].x = srcPoints[i].x;
                            nextTrackPts[i].y = srcPoints[i].y;

                            dstPoints[i] = srcPoints[i];
                        }

                        if (drawDebugPoints)
                        {
                            Debug.Log("DLIB");
                            for (int i = 0; i < numberOfElements; i++)
                            {
                                Imgproc.circle(img, new Point(srcPoints[i].x, srcPoints[i].y), 2, new Scalar(255, 0, 0, 255), -1);
                            }
                        }
                    }
                    else
                    {
                        // In this case, use Optical Flow
                        for (int i = 0; i < numberOfElements; i++)
                        {
                            dstPoints[i] = new Vector2((float)nextTrackPts[i].x, (float)nextTrackPts[i].y);
                        }

                        if (drawDebugPoints)
                        {
                            Debug.Log("Optical Flow");
                            for (int i = 0; i < numberOfElements; i++)
                            {
                                Imgproc.circle(img, nextTrackPts[i], 2, new Scalar(0, 0, 255, 255), -1);
                            }
                        }
                    }
                }
                Swap(ref prevTrackPts, ref nextTrackPts);
                Swap(ref prevgray, ref gray);
            }
            return dstPoints;
        }

        /// <summary>
        /// Resets filter.
        /// </summary>
        public override void Reset()
        {
            flag = false;

            // Reset Optical Flow
            for (int i = 0; i < numberOfElements; i++)
            {
                prevTrackPts[i].x = 0.0;
                prevTrackPts[i].y = 0.0;
            }
            for (int i = 0; i < numberOfElements; i++)
            {
                nextTrackPts[i].x = 0.0;
                nextTrackPts[i].y = 0.0;
            }

            if (prevgray != null)
            {
                prevgray.Dispose();
                prevgray = new Mat();
            }
            if (gray != null)
            {
                gray.Dispose();
                gray = new Mat();
            }
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public override void Dispose()
        {
            DisposeOpticalFlow();

            if (prevTrackPtsMat != null)
                prevTrackPtsMat.Dispose();
        }

        protected virtual void InitializeOpticalFlow()
        {
            prevTrackPts = new List<Point>();
            for (int i = 0; i < numberOfElements; i++)
            {
                prevTrackPts.Add(new Point(0, 0));
            }
            nextTrackPts = new List<Point>();
            for (int i = 0; i < numberOfElements; i++)
            {
                nextTrackPts.Add(new Point(0, 0));
            }
            prevgray = new Mat();
            gray = new Mat();
            mOP2fPrevTrackPts = new MatOfPoint2f();
            mOP2fNextTrackPts = new MatOfPoint2f();
            status = new MatOfByte();
            err = new MatOfFloat();
        }

        protected virtual void DisposeOpticalFlow()
        {
            if (prevTrackPts != null)
                prevTrackPts.Clear();
            if (nextTrackPts != null)
                nextTrackPts.Clear();
            if (prevgray != null)
                prevgray.Dispose();
            if (gray != null)
                gray.Dispose();
            if (mOP2fPrevTrackPts != null)
                mOP2fPrevTrackPts.Dispose();
            if (mOP2fNextTrackPts != null)
                mOP2fNextTrackPts.Dispose();
            if (status != null)
                status.Dispose();
            if (err != null)
                err.Dispose();
        }
    }
}