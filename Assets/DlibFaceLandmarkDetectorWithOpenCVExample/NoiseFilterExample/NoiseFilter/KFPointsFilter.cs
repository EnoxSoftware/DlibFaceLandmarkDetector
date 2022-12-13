using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.VideoModule;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Kalman Filter Points Filter.
    /// v 1.0.4
    /// </summary>
    public class KFPointsFilter : PointsFilterBase
    {
        public double diffCheckSensitivity = 1;

        bool flag = false;
        double diffDlib = 1;
        MatOfPoint prevTrackPtsMat;

        List<Point> src_points;
        List<Point> last_points;

        // Kalman Filter
        List<Point> predict_points;
        int stateNum = 272;
        int measureNum = 136;
        KalmanFilter KF;
        Mat measurement;

        public KFPointsFilter(int numberOfElements) : base(numberOfElements)
        {
            diffDlib = diffDlib * (double)numberOfElements / 68.0;
            prevTrackPtsMat = new MatOfPoint();

            src_points = new List<Point>();
            for (int i = 0; i < numberOfElements; i++)
            {
                src_points.Add(new Point(0.0, 0.0));
            }
            last_points = new List<Point>();
            for (int i = 0; i < numberOfElements; i++)
            {
                last_points.Add(new Point(0.0, 0.0));
            }

            // Initialize Kalman Filter
            stateNum = numberOfElements * 4;
            measureNum = numberOfElements * 2;
            InitializeKalmanFilter();
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

                for (int i = 0; i < numberOfElements; i++)
                {
                    src_points[i].x = srcPoints[i].x;
                    src_points[i].y = srcPoints[i].y;
                }

                // clac diffDlib
                prevTrackPtsMat.fromList(src_points);
                OpenCVForUnity.CoreModule.Rect rect = Imgproc.boundingRect(prevTrackPtsMat);
                double diffDlib = this.diffDlib * rect.area() / 40000.0 * diffCheckSensitivity;

                // if the face is moving so fast, use dlib to detect the face
                double diff = calDistanceDiff(src_points, last_points);
                if (drawDebugPoints)
                    Debug.Log("variance:" + diff);
                if (diff > diffDlib)
                {
                    for (int i = 0; i < numberOfElements; i++)
                    {
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

                    flag = false;
                }
                else
                {
                    if (!flag)
                    {
                        // Set initial state estimate.
                        Mat statePreMat = KF.get_statePre();
                        float[] tmpStatePre = new float[statePreMat.total()];
                        for (int i = 0; i < numberOfElements; i++)
                        {
                            tmpStatePre[i * 2] = (float)srcPoints[i].x;
                            tmpStatePre[i * 2 + 1] = (float)srcPoints[i].y;
                        }
                        statePreMat.put(0, 0, tmpStatePre);
                        Mat statePostMat = KF.get_statePost();
                        float[] tmpStatePost = new float[statePostMat.total()];
                        for (int i = 0; i < numberOfElements; i++)
                        {
                            tmpStatePost[i * 2] = (float)srcPoints[i].x;
                            tmpStatePost[i * 2 + 1] = (float)srcPoints[i].y;
                        }
                        statePostMat.put(0, 0, tmpStatePost);

                        flag = true;
                    }

                    // Kalman Prediction
                    KF.predict();

                    // Update Measurement
                    float[] tmpMeasurement = new float[measurement.total()];
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        tmpMeasurement[i * 2] = (float)srcPoints[i].x;
                        tmpMeasurement[i * 2 + 1] = (float)srcPoints[i].y;
                    }
                    measurement.put(0, 0, tmpMeasurement);

                    // Correct Measurement
                    Mat estimated = KF.correct(measurement);
                    float[] tmpEstimated = new float[estimated.total()];
                    estimated.get(0, 0, tmpEstimated);
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        predict_points[i].x = tmpEstimated[i * 2];
                        predict_points[i].y = tmpEstimated[i * 2 + 1];
                    }
                    estimated.Dispose();

                    for (int i = 0; i < numberOfElements; i++)
                    {
                        dstPoints[i] = new Vector2((float)predict_points[i].x, (float)predict_points[i].y);
                    }

                    if (drawDebugPoints)
                    {
                        Debug.Log("Kalman Filter");
                        for (int i = 0; i < numberOfElements; i++)
                        {
                            Imgproc.circle(img, predict_points[i], 2, new Scalar(0, 255, 0, 255), -1);
                        }
                    }
                }

                for (int i = 0; i < numberOfElements; i++)
                {
                    last_points[i].x = src_points[i].x;
                    last_points[i].y = src_points[i].y;
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

            // Reset Kalman Filter
            for (int i = 0; i < numberOfElements; i++)
            {
                predict_points[i].x = 0.0;
                predict_points[i].y = 0.0;
            }
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public override void Dispose()
        {
            if (src_points != null)
                src_points.Clear();

            DisposeKalmanFilter();

            if (prevTrackPtsMat != null)
                prevTrackPtsMat.Dispose();
        }

        protected virtual void InitializeKalmanFilter()
        {
            predict_points = new List<Point>();
            for (int i = 0; i < numberOfElements; i++)
            {
                predict_points.Add(new Point(0.0, 0.0));
            }

            KF = new KalmanFilter(stateNum, measureNum, 0, CvType.CV_32F);
            measurement = Mat.zeros(measureNum, 1, CvType.CV_32F);

            // Generate the Measurement Matrix
            KF.set_transitionMatrix(Mat.zeros(stateNum, stateNum, CvType.CV_32F));
            for (int i = 0; i < stateNum; i++)
            {
                for (int j = 0; j < stateNum; j++)
                {
                    if (i == j || (j - measureNum) == i)
                    {
                        KF.get_transitionMatrix().put(i, j, new float[] { 1.0f });
                    }
                    else
                    {
                        KF.get_transitionMatrix().put(i, j, new float[] { 0.0f });
                    }
                }
            }

            // measurement matrix (H)
            Core.setIdentity(KF.get_measurementMatrix());
            // process noise covariance matrix (Q)
            Core.setIdentity(KF.get_processNoiseCov(), Scalar.all(1e-5));
            // measurement noise covariance matrix (R)
            Core.setIdentity(KF.get_measurementNoiseCov(), Scalar.all(1e-1));
            // posteriori error estimate covariance matrix (P(k)): P(k)=(I-K(k)*H)*P'(k)
            Core.setIdentity(KF.get_errorCovPost(), Scalar.all(0.1));
        }

        protected virtual void DisposeKalmanFilter()
        {
            if (predict_points != null)
                predict_points.Clear();
            if (KF != null)
                KF.Dispose();
            if (measurement != null)
                measurement.Dispose();
        }
    }
}