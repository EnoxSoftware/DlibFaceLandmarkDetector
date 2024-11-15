using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// MultiSource Noise Filter Example
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MultiSourceNoiseFilterExample : MonoBehaviour
    {
        public enum FilterMode : int
        {
            None,
            LowPassFilter,
            KalmanFilter,
            OpticalFlowFilter,
            OFAndLPFilter,
        }

        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// Determines if is debug mode.
        /// </summary>
        public bool isDebugMode = false;

        [Space(10)]

        /// <summary>
        /// The filter mode dropdown.
        /// </summary>
        public Dropdown filterModeDropdown;

        /// <summary>
        /// The filter Mmode.
        /// </summary>
        public FilterMode filterMode = FilterMode.OFAndLPFilter;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        /// <summary>
        /// The mean points filter.
        /// </summary>
        LowPassPointsFilter lowPassFilter;

        /// <summary>
        /// The kanlam filter points filter.
        /// </summary>
        KFPointsFilter kalmanFilter;

        /// <summary>
        /// The optical flow points filter.
        /// </summary>
        OFPointsFilter opticalFlowFilter;

        List<Vector2> lowPassFilteredPoints = new List<Vector2>();
        List<Vector2> kalmanFilteredPoints = new List<Vector2>();
        List<Vector2> opticalFlowFilteredPoints = new List<Vector2>();
        List<Vector2> ofAndLPFilteredPoints = new List<Vector2>();

        /// <summary>
        /// The number of skipped frames.
        /// </summary>
        int skippedFrames;

        /// <summary>
        /// The number of maximum allowed skipped frames.
        /// </summary>
        const int maximumAllowedSkippedFrames = 4;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            dlibShapePredictorFilePath = await DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsyncTask(dlibShapePredictorFileName, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }
        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            lowPassFilter = new LowPassPointsFilter((int)faceLandmarkDetector.GetShapePredictorNumParts());
            kalmanFilter = new KFPointsFilter((int)faceLandmarkDetector.GetShapePredictorNumParts());
            opticalFlowFilter = new OFPointsFilter((int)faceLandmarkDetector.GetShapePredictorNumParts());

            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", multiSource2MatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", multiSource2MatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            if (lowPassFilter != null)
                lowPassFilter.Reset();
            if (kalmanFilter != null)
                kalmanFilter.Reset();
            if (opticalFlowFilter != null)
                opticalFlowFilter.Reset();
            skippedFrames = 0;
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                UnityEngine.Rect rect = new UnityEngine.Rect();
                List<Vector2> points = null;
                bool shouldResetfilter = false;
                if (detectResult.Count > 0)
                {
                    rect = detectResult[0];

                    //detect landmark points
                    points = faceLandmarkDetector.DetectLandmark(rect);

                    skippedFrames = 0;
                }
                else
                {
                    skippedFrames++;
                    if (skippedFrames == maximumAllowedSkippedFrames)
                    {
                        shouldResetfilter = true;
                    }
                }

                switch (filterMode)
                {
                    default:
                    case FilterMode.None:
                        break;
                    case FilterMode.LowPassFilter:
                        if (shouldResetfilter)
                            lowPassFilter.Reset();
                        lowPassFilter.Process(rgbaMat, points, lowPassFilteredPoints, isDebugMode);
                        break;
                    case FilterMode.KalmanFilter:
                        if (shouldResetfilter)
                            kalmanFilter.Reset();
                        kalmanFilter.Process(rgbaMat, points, kalmanFilteredPoints, isDebugMode);
                        break;
                    case FilterMode.OpticalFlowFilter:
                        if (shouldResetfilter)
                            opticalFlowFilter.Reset();
                        opticalFlowFilter.Process(rgbaMat, points, opticalFlowFilteredPoints, isDebugMode);
                        break;
                    case FilterMode.OFAndLPFilter:
                        if (shouldResetfilter)
                        {
                            opticalFlowFilter.Reset();
                            lowPassFilter.Reset();
                        }

                        opticalFlowFilter.Process(rgbaMat, points, points, false);
                        lowPassFilter.Process(rgbaMat, points, ofAndLPFilteredPoints, isDebugMode);
                        break;
                }


                if (points != null && !isDebugMode)
                {
                    // draw raw landmark points.
                    OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);
                }

                // draw face rect.
                //OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 0, 0, 255), 2);

                // draw filtered lam points. 
                if (points != null && !isDebugMode)
                {
                    switch (filterMode)
                    {
                        default:
                        case FilterMode.None:
                            break;
                        case FilterMode.LowPassFilter:
                            OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, lowPassFilteredPoints, new Scalar(0, 255, 255, 255), 2);
                            break;
                        case FilterMode.KalmanFilter:
                            OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, kalmanFilteredPoints, new Scalar(0, 0, 255, 255), 2);
                            break;
                        case FilterMode.OpticalFlowFilter:
                            OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, opticalFlowFilteredPoints, new Scalar(255, 0, 0, 255), 2);
                            break;
                        case FilterMode.OFAndLPFilter:
                            OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, ofAndLPFilteredPoints, new Scalar(255, 0, 255, 255), 2);
                            break;
                    }
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (multiSource2MatHelper != null)
                multiSource2MatHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (lowPassFilter != null)
                lowPassFilter.Dispose();
            if (kalmanFilter != null)
                kalmanFilter.Dispose();
            if (opticalFlowFilter != null)
                opticalFlowFilter.Dispose();

            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonCkick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the filter mode dropdown value changed event.
        /// </summary>
        public void OnFilterModeDropdownValueChanged(int result)
        {
            if ((int)filterMode != result)
            {
                filterMode = (FilterMode)result;

                if (lowPassFilter != null)
                    lowPassFilter.Reset();
                if (kalmanFilter != null)
                    kalmanFilter.Reset();
                if (opticalFlowFilter != null)
                    opticalFlowFilter.Reset();
                skippedFrames = 0;
            }
        }
    }
}
