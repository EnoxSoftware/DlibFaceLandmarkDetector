using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DlibFaceLandmarkDetector;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using VideoCapture = OpenCVForUnity.VideoioModule.VideoCapture;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Noise Filter VideoCapture Example
    /// </summary>
    public class NoiseFilterVideoCaptureExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if is debug mode.
        /// </summary>
        public bool isDebugMode = false;

        [Space (10)]

        /// <summary>
        /// The draw low pass filter toggle.
        /// </summary>
        public Toggle drawLowPassFilterToggle;

        /// <summary>
        /// Determines if draws low pass filter.
        /// </summary>
        public bool drawLowPassFilter;

        /// <summary>
        /// The draw kalman filter toggle.
        /// </summary>
        public Toggle drawKalmanFilterToggle;

        /// <summary>
        /// Determines if draws kalman filter.
        /// </summary>
        public bool drawKalmanFilter;

        /// <summary>
        /// The draw optical flow filter toggle.
        /// </summary>
        public Toggle drawOpticalFlowFilterToggle;

        /// <summary>
        /// Determines if draws optical flow filter.
        /// </summary>
        public bool drawOpticalFlowFilter;

        /// <summary>
        /// The draw OF and LP filter toggle.
        /// </summary>
        public Toggle drawOFAndLPFilterToggle;

        /// <summary>
        /// Determines if draws OF and LP filter.
        /// </summary>
        public bool drawOFAndLPFilter;

        /// <summary>
        /// The video capture.
        /// </summary>
        VideoCapture capture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

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
        string dlibShapePredictorFileName = "sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "dance_mjpeg.mjpeg";

        /// <summary>
        /// The video_filepath.
        /// </summary>
        string video_filepath;

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

        List<Vector2> lowPassFilteredPoints = new List<Vector2> ();
        List<Vector2> kalmanFilteredPoints = new List<Vector2> ();
        List<Vector2> opticalFlowFilteredPoints = new List<Vector2> ();
        List<Vector2> ofAndLPFilteredPoints = new List<Vector2> ();

        /// <summary>
        /// The number of skipped frames.
        /// </summary>
        int skippedFrames;

        /// <summary>
        /// The number of maximum allowed skipped frames.
        /// </summary>
        const int maximumAllowedSkippedFrames = 8;

        #if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
        #endif

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            drawLowPassFilterToggle.isOn = drawLowPassFilter;
            drawKalmanFilterToggle.isOn = drawKalmanFilter;
            drawOpticalFlowFilterToggle.isOn = drawOpticalFlowFilter;
            drawOFAndLPFilterToggle.isOn = drawOFAndLPFilter;

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            #if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = GetFilePath ();
            StartCoroutine (getFilePath_Coroutine);
            #else
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath (dlibShapePredictorFileName);
            video_filepath = OpenCVForUnity.UnityUtils.Utils.getFilePath (VIDEO_FILENAME);
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_dlibShapePredictorFilePath_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync (dlibShapePredictorFileName, (result) => {
                dlibShapePredictorFilePath = result;
            });
            yield return getFilePathAsync_dlibShapePredictorFilePath_Coroutine;

            var getFilePathAsync_dance_avi_filepath_Coroutine = OpenCVForUnity.UnityUtils.Utils.getFilePathAsync (VIDEO_FILENAME, (result) => {
                video_filepath = result;
            });
            yield return getFilePathAsync_dance_avi_filepath_Coroutine;

            getFilePath_Coroutine = null;

            Run ();
        }
        #endif

        private void Run ()
        {
            if (string.IsNullOrEmpty (dlibShapePredictorFilePath)) {
                Debug.LogError ("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector (dlibShapePredictorFilePath);

            lowPassFilter = new LowPassPointsFilter ((int)faceLandmarkDetector.GetShapePredictorNumParts ());
            kalmanFilter = new KFPointsFilter ((int)faceLandmarkDetector.GetShapePredictorNumParts ());
            opticalFlowFilter = new OFPointsFilter ((int)faceLandmarkDetector.GetShapePredictorNumParts ());

            rgbMat = new Mat ();

            capture = new VideoCapture ();
            capture.open (video_filepath);

            if (!capture.isOpened ()) {
                Debug.LogError ("capture.isOpened() is false. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }


            Debug.Log ("CAP_PROP_FORMAT: " + capture.get (Videoio.CAP_PROP_FORMAT));
            Debug.Log ("CAP_PROP_POS_MSEC: " + capture.get (Videoio.CAP_PROP_POS_MSEC));
            Debug.Log ("CAP_PROP_POS_FRAMES: " + capture.get (Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log ("CAP_PROP_POS_AVI_RATIO: " + capture.get (Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log ("CAP_PROP_FRAME_COUNT: " + capture.get (Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log ("CAP_PROP_FPS: " + capture.get (Videoio.CAP_PROP_FPS));
            Debug.Log ("CAP_PROP_FRAME_WIDTH: " + capture.get (Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log ("CAP_PROP_FRAME_HEIGHT: " + capture.get (Videoio.CAP_PROP_FRAME_HEIGHT));
            double ext = capture.get (Videoio.CAP_PROP_FOURCC);
            Debug.Log ("CAP_PROP_FOURCC: " + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));

            capture.grab ();
            capture.retrieve (rgbMat, 0);
            int frameWidth = rgbMat.cols ();
            int frameHeight = rgbMat.rows ();
            texture = new Texture2D (frameWidth, frameHeight, TextureFormat.RGB24, false);
            gameObject.transform.localScale = new Vector3 ((float)frameWidth, (float)frameHeight, 1);
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }
            capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            if (fpsMonitor != null) {
                fpsMonitor.Add ("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add ("width", frameWidth.ToString ());
                fpsMonitor.Add ("height", frameHeight.ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
            }
        }

        // Update is called once per frame
        void Update ()
        {
            if (capture == null)
                return;

            //Loop play
            if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
            if (capture.grab ()) {

                capture.retrieve (rgbMat, 0);

                Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                //Debug.Log ("Mat toString " + rgbMat.ToString ());


                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbMat);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect ();

                UnityEngine.Rect rect = new UnityEngine.Rect ();
                List<Vector2> points = null;
                if (detectResult.Count > 0) {

                    rect = detectResult [0];

                    //detect landmark points
                    points = faceLandmarkDetector.DetectLandmark (rect);

                    skippedFrames = 0;
                } else {
                    skippedFrames++;
                    if (skippedFrames == maximumAllowedSkippedFrames) {
                        if (drawLowPassFilter)
                            lowPassFilter.Reset ();
                        if (drawKalmanFilter)
                            kalmanFilter.Reset ();
                        if (drawOpticalFlowFilter)
                            opticalFlowFilter.Reset ();
                        if (drawOFAndLPFilter)
                            opticalFlowFilter.Reset ();
                        lowPassFilter.Reset ();
                    }
                }

                if (drawLowPassFilter) {
                    lowPassFilter.Process (rgbMat, points, lowPassFilteredPoints, isDebugMode);
                }
                if (drawKalmanFilter) {
                    kalmanFilter.Process (rgbMat, points, kalmanFilteredPoints, isDebugMode);
                }
                if (drawOpticalFlowFilter) {
                    opticalFlowFilter.Process (rgbMat, points, opticalFlowFilteredPoints, isDebugMode);
                }
                if (drawOFAndLPFilter) {
                    opticalFlowFilter.Process (rgbMat, points, points, false);
                    lowPassFilter.Process (rgbMat, points, ofAndLPFilteredPoints, isDebugMode);
                }


                if (points != null && !isDebugMode) {
                    // draw raw landmark points.
                    OpenCVForUnityUtils.DrawFaceLandmark (rgbMat, points, new Scalar (0, 255, 0), 2);
                }

                // draw face rect.
                //OpenCVForUnityUtils.DrawFaceRect (rgbMat, rect, new Scalar (255, 0, 0), 2);

                // draw filtered lam points. 
                if (points != null && !isDebugMode) {
                    if (drawLowPassFilter)
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbMat, lowPassFilteredPoints, new Scalar (0, 255, 255), 2);
                    if (drawKalmanFilter)
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbMat, kalmanFilteredPoints, new Scalar (0, 0, 255), 2);
                    if (drawOpticalFlowFilter)
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbMat, opticalFlowFilteredPoints, new Scalar (255, 0, 0), 2);
                    if (drawOFAndLPFilter)
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbMat, ofAndLPFilteredPoints, new Scalar (255, 0, 255), 2);
                }

                //Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D (rgbMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (capture != null)
                capture.release ();

            if (rgbMat != null)
                rgbMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy (texture);
                texture = null;
            }

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (lowPassFilter != null)
                lowPassFilter.Dispose ();
            if (kalmanFilter != null)
                kalmanFilter.Dispose ();
            if (opticalFlowFilter != null)
                opticalFlowFilter.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
            }
            #endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the draw low pass filter toggle value changed event.
        /// </summary>
        public void OnDrawLowPassFilterToggleValueChanged ()
        {
            if (drawLowPassFilterToggle.isOn) {
                drawLowPassFilter = true;
                if (lowPassFilter != null)
                    lowPassFilter.Reset ();
            } else {
                drawLowPassFilter = false;
            }
        }

        /// <summary>
        /// Raises the draw kalman filter toggle value changed event.
        /// </summary>
        public void OnDrawKalmanFilterToggleValueChanged ()
        {
            if (drawKalmanFilterToggle.isOn) {
                drawKalmanFilter = true;
                if (kalmanFilter != null)
                    kalmanFilter.Reset ();
            } else {
                drawKalmanFilter = false;
            }
        }

        /// <summary>
        /// Raises the draw optical flow filter toggle value changed event.
        /// </summary>
        public void OnDrawOpticalFlowFilterToggleValueChanged ()
        {
            if (drawOpticalFlowFilterToggle.isOn) {
                drawOpticalFlowFilter = true;
                if (opticalFlowFilter != null)
                    opticalFlowFilter.Reset ();
            } else {
                drawOpticalFlowFilter = false;
            }
        }

        /// <summary>
        /// Raises the draw OF and LP filter toggle value changed event.
        /// </summary>
        public void OnDrawOFAndLPFilterToggleValueChanged ()
        {
            if (drawOFAndLPFilterToggle.isOn) {
                drawOFAndLPFilter = true;
                if (opticalFlowFilter != null)
                    opticalFlowFilter.Reset ();
                if (lowPassFilter != null)
                    lowPassFilter.Reset ();
            } else {
                drawOFAndLPFilter = false;
            }
        }
    }
}