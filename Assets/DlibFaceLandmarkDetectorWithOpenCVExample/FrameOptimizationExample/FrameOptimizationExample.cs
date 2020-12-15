#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Frame Optimization Example
    /// An example of frame downscaling and skipping using the OptimizationWebCamTextureToMatHelper.
    /// http://www.learnopencv.com/speeding-up-dlib-facial-landmark-detector/
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper), typeof(ImageOptimizationHelper))]
    public class FrameOptimizationExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle enableDownScaleToggle;

        /// <summary>
        /// Determines if enable skipframe.
        /// </summary>
        public bool enableSkipFrame;

        /// <summary>
        /// The enable skipframe toggle.
        /// </summary>
        public Toggle enableSkipFrameToggle;

        /// <summary>
        /// Determines if use OpenCV FaceDetector for face detection.
        /// </summary>
        public bool useOpenCVFaceDetector;

        /// <summary>
        /// The use OpenCV FaceDetector toggle.
        /// </summary>
        public Toggle useOpenCVFaceDetectorToggle;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The detection result.
        /// </summary>
        List<UnityEngine.Rect> detectionResult;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            enableDownScaleToggle.isOn = enableDownScale;
            enableSkipFrameToggle.isOn = enableSkipFrame;
            useOpenCVFaceDetectorToggle.isOn = useOpenCVFaceDetector;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.UnityUtils.Utils.getFilePath("haarcascade_frontalface_alt.xml");
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = OpenCVForUnity.UnityUtils.Utils.getFilePathAsync("haarcascade_frontalface_alt.xml", (result) =>
            {
                haarcascade_frontalface_alt_xml_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync(dlibShapePredictorFileName, (result) =>
            {
                dlibShapePredictorFilePath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

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

            cascade = new CascadeClassifier(haarcascade_frontalface_alt_xml_filepath);
#if !UNITY_WSA_10_0
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
#endif

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();
            Mat downscaleMat = imageOptimizationHelper.GetDownScaleMat(webCamTextureMat);

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("original_width", webCamTextureToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("original_height", webCamTextureToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("downscaleRaito", imageOptimizationHelper.downscaleRatio.ToString());
                fpsMonitor.Add("frameSkippingRatio", imageOptimizationHelper.frameSkippingRatio.ToString());
                fpsMonitor.Add("downscale_width", downscaleMat.width().ToString());
                fpsMonitor.Add("downscale_height", downscaleMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

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


            grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);

            detectionResult = new List<UnityEngine.Rect>();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (grayMat != null)
            {
                grayMat.Dispose();
                grayMat = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                // detect faces on the downscale image
                if (!enableSkipFrame || !imageOptimizationHelper.IsCurrentFrameSkipped())
                {

                    Mat downScaleRgbaMat = null;
                    float DOWNSCALE_RATIO = 1.0f;
                    if (enableDownScale)
                    {
                        downScaleRgbaMat = imageOptimizationHelper.GetDownScaleMat(rgbaMat);
                        DOWNSCALE_RATIO = imageOptimizationHelper.downscaleRatio;
                    }
                    else
                    {
                        downScaleRgbaMat = rgbaMat;
                        DOWNSCALE_RATIO = 1.0f;
                    }

                    // set the downscale mat
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, downScaleRgbaMat);

                    //detect face rects
                    if (useOpenCVFaceDetector)
                    {
                        // convert image to greyscale.
                        Imgproc.cvtColor(downScaleRgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                        using (Mat equalizeHistMat = new Mat())
                        using (MatOfRect faces = new MatOfRect())
                        {
                            Imgproc.equalizeHist(grayMat, equalizeHistMat);

                            cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size(equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), new Size());

                            List<OpenCVForUnity.CoreModule.Rect> opencvDetectResult = faces.toList();

                            // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                            detectionResult.Clear();
                            foreach (var opencvRect in opencvDetectResult)
                            {
                                detectionResult.Add(new UnityEngine.Rect((float)opencvRect.x, (float)opencvRect.y + (float)(opencvRect.height * 0.1f), (float)opencvRect.width, (float)opencvRect.height));
                            }
                        }

                    }
                    else
                    {
                        // Dlib's face detection processing time increases in proportion to image size.
                        detectionResult = faceLandmarkDetector.Detect();
                    }

                    if (enableDownScale)
                    {
                        for (int i = 0; i < detectionResult.Count; ++i)
                        {
                            var rect = detectionResult[i];
                            detectionResult[i] = new UnityEngine.Rect(
                                rect.x * DOWNSCALE_RATIO,
                                rect.y * DOWNSCALE_RATIO,
                                rect.width * DOWNSCALE_RATIO,
                                rect.height * DOWNSCALE_RATIO);
                        }
                    }
                }

                // set the original scale image
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);
                // detect face landmarks on the original image
                foreach (var rect in detectionResult)
                {

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                    //draw landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);
                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(255, 0, 0, 255), 2);
                }

                //Imgproc.putText (rgbaMat, "Original:(" + rgbaMat.width () + "," + rgbaMat.height () + ") DownScale:(" + rgbaMat.width () / imageOptimizationHelper.downscaleRatio + "," + rgbaMat.height () / imageOptimizationHelper.downscaleRatio + ") FrameSkipping: " + imageOptimizationHelper.frameSkippingRatio, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (imageOptimizationHelper != null)
                imageOptimizationHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (cascade != null)
                cascade.Dispose();

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

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            if (enableDownScaleToggle.isOn)
            {
                enableDownScale = true;
            }
            else
            {
                enableDownScale = false;
            }
        }

        /// <summary>
        /// Raises the enable skipframe toggle value changed event.
        /// </summary>
        public void OnEnableSkipFrameToggleValueChanged()
        {
            if (enableSkipFrameToggle.isOn)
            {
                enableSkipFrame = true;
            }
            else
            {
                enableSkipFrame = false;
            }
        }

        /// <summary>
        /// Raises the use OpenCV FaceDetector toggle value changed event.
        /// </summary>
        public void OnUseOpenCVFaceDetectorToggleValueChanged()
        {
            if (useOpenCVFaceDetectorToggle.isOn)
            {
                useOpenCVFaceDetector = true;
            }
            else
            {
                useOpenCVFaceDetector = false;
            }
        }
    }
}

#endif