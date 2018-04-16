using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Texture2DToMat Example
    /// An example of using "Dlib FaceLandmark Detector" together with "OpenCV for Unity".
    /// </summary>
    public class Texture2DToMatExample : MonoBehaviour
    {
        /// <summary>
        /// The image texture.
        /// </summary>
        public Texture2D imgTexture;

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

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = DlibFaceLandmarkDetector.Utils.getFilePathAsync (dlibShapePredictorFileName, (result) => {
                coroutines.Clear ();

                dlibShapePredictorFilePath = result;
                Run ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.Utils.getFilePath (dlibShapePredictorFileName);
            Run ();
            #endif
        }

        private void Run ()
        {
            Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            // Convert Unity Texture2D to OpenCV Mat.
            OpenCVForUnity.Utils.texture2DToMat (imgTexture, imgMat);
            Debug.Log ("imgMat dst ToString " + imgMat.ToString ());

            gameObject.transform.localScale = new Vector3 (imgTexture.width, imgTexture.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
            
            float width = imgMat.width ();
            float height = imgMat.height ();
            
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector (dlibShapePredictorFilePath);

            OpenCVForUnityUtils.SetImage (faceLandmarkDetector, imgMat);

        
            //detect face rectdetecton
            List<FaceLandmarkDetector.RectDetection> detectResult = faceLandmarkDetector.DetectRectDetection ();

            foreach (var result in detectResult) {
                Debug.Log ("rect : " + result.rect);
                Debug.Log ("detection_confidence : " + result.detection_confidence);
                Debug.Log ("weight_index : " + result.weight_index);

                //detect landmark points
                List<Vector2> points = faceLandmarkDetector.DetectLandmark (result.rect);

                Debug.Log ("face points count : " + points.Count);
                //draw landmark points
                OpenCVForUnityUtils.DrawFaceLandmark (imgMat, points, new Scalar (0, 255, 0, 255), 2, true);

                //draw face rect
                OpenCVForUnityUtils.DrawFaceRect (imgMat, result, new Scalar (255, 0, 0, 255), 2);
            }

            faceLandmarkDetector.Dispose ();

            Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);

            // Convert OpenCV Mat to Unity Texture2D.
            OpenCVForUnity.Utils.matToTexture2D (imgMat, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            if (fpsMonitor != null){                
                fpsMonitor.Add ("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add ("width", width.ToString());
                fpsMonitor.Add ("height", height.ToString());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString());
            }
        }

        // Update is called once per frame
        void Update ()
        {
    
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
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
    }
}