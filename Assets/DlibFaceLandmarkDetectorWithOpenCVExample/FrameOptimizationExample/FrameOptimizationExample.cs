using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Frame Optimization Example
    /// An example of frame downscaling and skipping using the Optimization MultiSource2MatHelper.
    /// http://www.learnopencv.com/speeding-up-dlib-facial-landmark-detector/
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper), typeof(ImageOptimizationHelper))]
    public class FrameOptimizationExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

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
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

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
        (double x, double y, double width, double height)[] detectionResult;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            enableDownScaleToggle.isOn = enableDownScale;
            enableSkipFrameToggle.isOn = enableSkipFrame;
            useOpenCVFaceDetectorToggle.isOn = useOpenCVFaceDetector;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            haarcascade_frontalface_alt_xml_filepath = await DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsyncTask("DlibFaceLandmarkDetector/haarcascade_frontalface_alt.xml", cancellationToken: cts.Token);
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

            cascade = new CascadeClassifier(haarcascade_frontalface_alt_xml_filepath);
#if !UNITY_WSA_10_0
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
#endif

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();
            Mat downscaleMat = imageOptimizationHelper.GetDownScaleMat(rgbaMat);

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("original_width", multiSource2MatHelper.GetWidth().ToString());
                fpsMonitor.Add("original_height", multiSource2MatHelper.GetHeight().ToString());
                fpsMonitor.Add("downscaleRaito", imageOptimizationHelper.downscaleRatio.ToString());
                fpsMonitor.Add("frameSkippingRatio", imageOptimizationHelper.frameSkippingRatio.ToString());
                fpsMonitor.Add("downscale_width", downscaleMat.width().ToString());
                fpsMonitor.Add("downscale_height", downscaleMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

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
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

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

                            cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, (equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), (0, 0));

                            detectionResult = faces.toValueTupleArrayAsDouble();
                        }
                    }
                    else
                    {
                        // Dlib's face detection processing time increases in proportion to image size.
                        detectionResult = faceLandmarkDetector.DetectValueTuple();
                    }

                    if (enableDownScale && detectionResult != null)
                    {
                        for (int i = 0; i < detectionResult.Length; ++i)
                        {
                            detectionResult[i].x *= DOWNSCALE_RATIO;
                            detectionResult[i].y *= DOWNSCALE_RATIO;
                            detectionResult[i].width *= DOWNSCALE_RATIO;
                            detectionResult[i].height *= DOWNSCALE_RATIO;
                        }
                    }
                }


                if (detectionResult != null)
                {
                    // set the original scale image
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);
                    // detect face landmarks on the original image
                    foreach (var rect in detectionResult)
                    {

                        //detect landmark points
                        (double x, double y)[] points = faceLandmarkDetector.DetectLandmark(rect);

                        //draw landmark points
                        OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, (0, 255, 0, 255), 2);
                        //draw face rect
                        OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, (255, 0, 0, 255), 2);
                    }
                }

                Imgproc.putText(rgbaMat, "Original:(" + rgbaMat.width() + "," + rgbaMat.height() + ") DownScale:(" + rgbaMat.width() / imageOptimizationHelper.downscaleRatio + "," + rgbaMat.height() / imageOptimizationHelper.downscaleRatio + ") FrameSkipping: " + imageOptimizationHelper.frameSkippingRatio, (5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

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

            if (imageOptimizationHelper != null)
                imageOptimizationHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (cascade != null)
                cascade.Dispose();

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
        public void OnPauseButtonClick()
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
