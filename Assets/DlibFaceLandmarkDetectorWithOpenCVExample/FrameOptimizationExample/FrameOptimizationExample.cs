using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Frame Optimization Example
    /// An example of frame resizing and skipping using the OptimizationWebCamTextureToMatHelper.
    /// http://www.learnopencv.com/speeding-up-dlib-facial-landmark-detector/
    /// </summary>
    [RequireComponent (typeof(OptimizationWebCamTextureToMatHelper))]
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
        /// Determines if use OpenCV FaceDetector.
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
        OptimizationWebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The detection result.
        /// </summary>
        List<UnityEngine.Rect> detectionResult;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The sp_human_face_68_dat_filepath.
        /// </summary>
        string sp_human_face_68_dat_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            enableDownScaleToggle.isOn = enableDownScale;
            enableSkipFrameToggle.isOn = enableSkipFrame;
            useOpenCVFaceDetectorToggle.isOn = useOpenCVFaceDetector;

            webCamTextureToMatHelper = gameObject.GetComponent<OptimizationWebCamTextureToMatHelper> ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = GetFilePath ();
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.Utils.getFilePath ("haarcascade_frontalface_alt.xml");
            sp_human_face_68_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath ("sp_human_face_68.dat");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
        var getFilePathAsync_0_Coroutine = OpenCVForUnity.Utils.getFilePathAsync ("haarcascade_frontalface_alt.xml", (result) => {
        haarcascade_frontalface_alt_xml_filepath = result;
        });
        coroutines.Push (getFilePathAsync_0_Coroutine);
        yield return StartCoroutine (getFilePathAsync_0_Coroutine);

        var getFilePathAsync_1_Coroutine = DlibFaceLandmarkDetector.Utils.getFilePathAsync ("sp_human_face_68.dat", (result) => {
        sp_human_face_68_dat_filepath = result;
        });
        coroutines.Push (getFilePathAsync_1_Coroutine);
        yield return StartCoroutine (getFilePathAsync_1_Coroutine);

        coroutines.Clear ();

        Run ();
        }
        #endif

        private void Run ()
        {
            cascade = new CascadeClassifier (haarcascade_frontalface_alt_xml_filepath);
            //            if (cascade.empty ()) {
            //                Debug.LogError ("cascade file is not loaded.Please copy from “FaceTrackerExample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            //            }


            faceLandmarkDetector = new FaceLandmarkDetector (sp_human_face_68_dat_filepath);

            webCamTextureToMatHelper.Initialize ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
                                    
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
                                    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);

            detectionResult = new List<UnityEngine.Rect> ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            grayMat.Dispose ();

        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Mat downScaleRgbaMat = null;
                float DOWNSCALE_RATIO = 1.0f;
                if (enableDownScale) {
                    downScaleRgbaMat = webCamTextureToMatHelper.GetDownScaleMat (rgbaMat);
                    DOWNSCALE_RATIO = webCamTextureToMatHelper.downscaleRatio;
                } else {
                    downScaleRgbaMat = rgbaMat;
                    DOWNSCALE_RATIO = 1.0f;
                }


                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, downScaleRgbaMat);

                // Detect faces on resize image
                if (!enableSkipFrame || !webCamTextureToMatHelper.IsCurrentFrameSkipped ()) {
                    //detect face rects
                    if (useOpenCVFaceDetector) {
                        // convert image to greyscale.
                        Imgproc.cvtColor (downScaleRgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                        using (Mat equalizeHistMat = new Mat ())
                        using (MatOfRect faces = new MatOfRect ()) {
                            Imgproc.equalizeHist (grayMat, equalizeHistMat);

                            cascade.detectMultiScale (equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size (equalizeHistMat.cols () * 0.15, equalizeHistMat.cols () * 0.15), new Size ());

                            List<OpenCVForUnity.Rect> opencvDetectResult = faces.toList ();

                            // adjust to Dilb's result.
                            detectionResult.Clear ();
                            foreach (var opencvRect in opencvDetectResult) {
                                detectionResult.Add (new UnityEngine.Rect ((float)opencvRect.x, (float)opencvRect.y + (float)(opencvRect.height * 0.1f), (float)opencvRect.width, (float)opencvRect.height));
                            }
                        }
                            
                    } else {
                        
                        detectionResult = faceLandmarkDetector.Detect ();

                    }
                }


                
                foreach (var rect in detectionResult) {

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);

                    List<Vector2> originalPoints = new List<Vector2> (points.Count);
                    foreach (var point in points) {
                        originalPoints.Add (new Vector2 (point.x * DOWNSCALE_RATIO, point.y * DOWNSCALE_RATIO));
                    }

                    //draw landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, originalPoints, new Scalar (0, 255, 0, 255), 2);

                    UnityEngine.Rect originalRect = new UnityEngine.Rect (rect.x * DOWNSCALE_RATIO, rect.y * DOWNSCALE_RATIO, rect.width * DOWNSCALE_RATIO, rect.height * DOWNSCALE_RATIO);
                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect (rgbaMat, originalRect, new Scalar (255, 0, 0, 255), 2);
                }

                Imgproc.putText (rgbaMat, "Original:(" + rgbaMat.width () + "," + rgbaMat.height () + ") DownScale:(" + downScaleRgbaMat.width () + "," + downScaleRgbaMat.height () + ") FrameSkipping: " + webCamTextureToMatHelper.frameSkippingRatio, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (cascade != null)
                cascade.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorExample");
            #endif
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged ()
        {
            if (enableDownScaleToggle.isOn) {
                enableDownScale = true;
            } else {
                enableDownScale = false;
            }
        }

        /// <summary>
        /// Raises the enable skipframe toggle value changed event.
        /// </summary>
        public void OnEnableSkipFrameToggleValueChanged ()
        {
            if (enableSkipFrameToggle.isOn) {
                enableSkipFrame = true;
            } else {
                enableSkipFrame = false;
            }
        }

        /// <summary>
        /// Raises the use OpenCV FaceDetector toggle value changed event.
        /// </summary>
        public void OnUseOpenCVFaceDetectorToggleValueChanged ()
        {
            if (useOpenCVFaceDetectorToggle.isOn) {
                useOpenCVFaceDetector = true;
            } else {
                useOpenCVFaceDetector = false;
            }
        }
    }
}