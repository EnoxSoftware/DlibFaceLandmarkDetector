using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DlibFaceLandmarkDetector.UnityUtils;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Cat Detection Example
    /// An example of detecting cat face landmarks in a Texture2D image.
    /// </summary>
    public class CatDetectionExample : MonoBehaviour
    {
        /// <summary>
        /// The texture2D.
        /// </summary>
        public Texture2D texture2D;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The frontal_cat_face_svm_filepath.
        /// </summary>
        string frontal_cat_face_svm_filepath;

        /// <summary>
        /// The sp_cat_face_68_dat_filepath.
        /// </summary>
        string sp_cat_face_68_dat_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
        #endif

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = GetFilePath ();
            StartCoroutine (getFilePath_Coroutine);
            #else
            frontal_cat_face_svm_filepath = Utils.getFilePath ("frontal_cat_face.svm");
            sp_cat_face_68_dat_filepath = Utils.getFilePath ("sp_cat_face_68.dat");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine = Utils.getFilePathAsync ("frontal_cat_face.svm", (result) => {
                frontal_cat_face_svm_filepath = result;
            });
            yield return getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine;

            var getFilePathAsync_sp_cat_face_68_dat_filepath_Coroutine = Utils.getFilePathAsync ("sp_cat_face_68.dat", (result) => {
                sp_cat_face_68_dat_filepath = result;
            });
            yield return getFilePathAsync_sp_cat_face_68_dat_filepath_Coroutine;

            getFilePath_Coroutine = null;

            Run ();
        }
        #endif

        private void Run ()
        {
            if (string.IsNullOrEmpty (frontal_cat_face_svm_filepath)) {
                Debug.LogError ("object detecter file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            if (string.IsNullOrEmpty (sp_cat_face_68_dat_filepath)) {
                Debug.LogError ("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            Texture2D dstTexture2D = new Texture2D (texture2D.width, texture2D.height, texture2D.format, false);
            Graphics.CopyTexture (texture2D, dstTexture2D);

            gameObject.transform.localScale = new Vector3 (texture2D.width, texture2D.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
            
            float width = gameObject.transform.localScale.x;
            float height = gameObject.transform.localScale.y;
            
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector (frontal_cat_face_svm_filepath, sp_cat_face_68_dat_filepath);
            faceLandmarkDetector.SetImage (texture2D);

            //detect face rects
            List<Rect> detectResult = faceLandmarkDetector.Detect ();

            foreach (var rect in detectResult) {
                Debug.Log ("face : " + rect);

                //detect landmark points
                List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);
                
                Debug.Log ("face points count : " + points.Count);
                foreach (var point in points) {
                    Debug.Log ("face point : x " + point.x + " y " + point.y);
                }

                //draw landmark points
                faceLandmarkDetector.DrawDetectLandmarkResult (dstTexture2D, 0, 255, 0, 255);
            }

            //draw face rects
            faceLandmarkDetector.DrawDetectResult (dstTexture2D, 255, 0, 0, 255, 3);

            faceLandmarkDetector.Dispose ();

            gameObject.GetComponent<Renderer> ().material.mainTexture = dstTexture2D;

            if (fpsMonitor != null) {
                fpsMonitor.Add ("dlib object detector", "frontal_cat_face.svm");
                fpsMonitor.Add ("dlib shape predictor", "sp_cat_face_68.dat");
                fpsMonitor.Add ("width", width.ToString ());
                fpsMonitor.Add ("height", height.ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
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
    }
}