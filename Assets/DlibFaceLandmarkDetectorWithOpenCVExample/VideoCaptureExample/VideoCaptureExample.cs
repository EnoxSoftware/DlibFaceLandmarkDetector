using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// VideoCapture Example
    /// An example of detecting face landmarks in VideoCapture images.
    /// </summary>
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
    public class VideoCaptureExample : MonoBehaviour
    {
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
        /// The video capture to mat helper.
        /// </summary>
        VideoCaptureToMatHelper sourceToMatHelper;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "couple_mjpeg.mjpeg";

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            sourceToMatHelper = gameObject.GetComponent<VideoCaptureToMatHelper>();


            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_dlibShapePredictorFilePath_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync(dlibShapePredictorFileName, (result) =>
            {
                dlibShapePredictorFilePath = result;
            });
            yield return getFilePathAsync_dlibShapePredictorFilePath_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            if (string.IsNullOrEmpty(sourceToMatHelper.requestedVideoFilePath))
                sourceToMatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            sourceToMatHelper.outputColorFormat = VideoCaptureToMatHelper.ColorFormat.RGB;
            sourceToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the video capture to mat helper initialized event.
        /// </summary>
        public void OnVideoCaptureToMatHelperInitialized()
        {
            Debug.Log("OnVideoCaptureToMatHelperInitialized");

            Mat rgbMat = sourceToMatHelper.GetMat();

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            Utils.fastMatToTexture2D(rgbMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(rgbMat.cols(), rgbMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", sourceToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", sourceToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = rgbMat.width();
            float height = rgbMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }
        }

        /// <summary>
        /// Raises the video capture to mat helper disposed event.
        /// </summary>
        public void OnVideoCaptureToMatHelperDisposed()
        {
            Debug.Log("OnVideoCaptureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the video capture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnVideoCaptureToMatHelperErrorOccurred(VideoCaptureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnVideoCaptureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (sourceToMatHelper.IsPlaying() && sourceToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbMat = sourceToMatHelper.GetMat();

                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbMat);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                foreach (var rect in detectResult)
                {
                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                    //draw landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark(rgbMat, points, new Scalar(0, 255, 0), 2);

                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect(rgbMat, rect, new Scalar(255, 0, 0), 2);
                }

                //Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255), 1, Imgproc.LINE_AA, false);

                Utils.fastMatToTexture2D(rgbMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (sourceToMatHelper != null)
                sourceToMatHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }
    }
}