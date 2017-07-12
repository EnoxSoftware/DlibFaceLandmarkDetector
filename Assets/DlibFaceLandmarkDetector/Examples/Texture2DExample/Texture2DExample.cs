using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Texture2D example. (Example of face landmark detection from Texture2D)
    /// </summary>
    public class Texture2DExample : MonoBehaviour
    {
        /// <summary>
        /// The texture2D.
        /// </summary>
        public Texture2D texture2D;

        /// <summary>
        /// The shape_predictor_68_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_face_landmarks_dat_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        private Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = Utils.getFilePathAsync ("shape_predictor_68_face_landmarks.dat", (result) => {
                coroutines.Clear ();

                shape_predictor_68_face_landmarks_dat_filepath = result;
                Run ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            shape_predictor_68_face_landmarks_dat_filepath = Utils.getFilePath ("shape_predictor_68_face_landmarks.dat");
            Run ();
            #endif
        }

        private void Run ()
        {
            //if true, The error log of the Native side Dlib will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);


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

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector (shape_predictor_68_face_landmarks_dat_filepath);
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
                faceLandmarkDetector.DrawDetectLandmarkResult (texture2D, 0, 255, 0, 255);

            }

            //draw face rect
            faceLandmarkDetector.DrawDetectResult (texture2D, 255, 0, 0, 255, 2);

            faceLandmarkDetector.Dispose ();

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture2D;


            Utils.setDebugMode (false);
        }

        // Update is called once per frame
        void Update ()
        {

        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
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