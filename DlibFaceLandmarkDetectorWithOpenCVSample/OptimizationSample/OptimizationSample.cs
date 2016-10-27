using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorSample
{
    /// <summary>
    /// OptimizationSample (Resize Frame+Skip frame)
    /// http://www.learnopencv.com/speeding-up-dlib-facial-landmark-detector/
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class OptimizationSample : MonoBehaviour
    {

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The FAC e_ DOWNSAMPL e_ RATI.
        /// </summary>
        public int FACE_DOWNSAMPLE_RATIO = 2;

        /// <summary>
        /// The SKI p_ FRAME.
        /// </summary>
        public int SKIP_FRAMES = 2;

        /// <summary>
        /// The count.
        /// </summary>
        int count;

        /// <summary>
        /// The rgba mat_downscale.
        /// </summary>
        Mat rgbaMat_downscale;

        /// <summary>
        /// The detect result.
        /// </summary>
        List<UnityEngine.Rect> detectResult;

        // Use this for initialization
        void Start ()
        {
            faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat"));

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Init ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper inited event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInited ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInited");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
                                    
            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();
                                    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            rgbaMat_downscale = new Mat ();
            detectResult = new List<UnityEngine.Rect> ();
            count = 0;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

        }

        // Update is called once per frame
        void Update ()
        {

            if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                // Resize image for face detection
                Imgproc.resize (rgbaMat, rgbaMat_downscale, new Size (), 1.0 / FACE_DOWNSAMPLE_RATIO, 1.0 / FACE_DOWNSAMPLE_RATIO, Imgproc.INTER_LINEAR);


                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat_downscale);


                // Detect faces on resize image
                if (count % SKIP_FRAMES == 0) {
                    //detect face rects
                    detectResult = faceLandmarkDetector.Detect ();
                }
                
                foreach (var rect in detectResult) {

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);

                    if (points.Count > 0) {
                        List<Vector2> originalPoints = new List<Vector2> (points.Count);
                        foreach (var point in points) {
                            originalPoints.Add (new Vector2 (point.x * FACE_DOWNSAMPLE_RATIO, point.y * FACE_DOWNSAMPLE_RATIO));
                        }

                        //draw landmark points
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, originalPoints, new Scalar (0, 255, 0, 255), 2);
                    }

                    UnityEngine.Rect originalRect = new UnityEngine.Rect (rect.x * FACE_DOWNSAMPLE_RATIO, rect.y * FACE_DOWNSAMPLE_RATIO, rect.width * FACE_DOWNSAMPLE_RATIO, rect.height * FACE_DOWNSAMPLE_RATIO);
                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect (rgbaMat, originalRect, new Scalar (255, 0, 0, 255), 2);
                }

                Imgproc.putText (rgbaMat, "Original: (" + rgbaMat.width () + "," + rgbaMat.height () + ") DownScale; (" + rgbaMat_downscale.width () + "," + rgbaMat_downscale.height () + ") SkipFrames: " + SKIP_FRAMES, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors());

                count++;
            }

        }
    
        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            webCamTextureToMatHelper.Dispose ();

            faceLandmarkDetector.Dispose ();
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorSample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorSample");
            #endif
        }

        /// <summary>
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton ()
        {
            webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
        }
        
    }
}