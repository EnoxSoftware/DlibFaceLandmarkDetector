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
    /// Cat detection example.
    /// </summary>
    public class CatDetectionExample : MonoBehaviour
    {

        /// <summary>
        /// The texture2 d.
        /// </summary>
        public Texture2D texture2D;

        /// <summary>
        /// The frontal_cat_face_svm_filepath.
        /// </summary>
        private string frontal_cat_face_svm_filepath;

        /// <summary>
        /// The shape_predictor_68_cat_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_cat_face_landmarks_dat_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        private Stack<IEnumerator> coroutineStack = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var filepath_coroutine = getFilePathCoroutine ();
            coroutineStack.Push (filepath_coroutine);
            StartCoroutine (filepath_coroutine);
            #else
            frontal_cat_face_svm_filepath = Utils.getFilePath ("frontal_cat_face.svm");
            shape_predictor_68_cat_face_landmarks_dat_filepath = Utils.getFilePath ("shape_predictor_68_cat_face_landmarks.dat");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator getFilePathCoroutine ()
        {
            var getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine = Utils.getFilePathAsync ("frontal_cat_face.svm", (result) => {
                frontal_cat_face_svm_filepath = result;
            });
            coroutineStack.Push (getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine);
            yield return StartCoroutine (getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine);

            var getFilePathAsync_shape_predictor_68_cat_face_landmarks_dat_filepath_Coroutine = Utils.getFilePathAsync ("shape_predictor_68_cat_face_landmarks.dat", (result) => {
                shape_predictor_68_cat_face_landmarks_dat_filepath = result;
            });
            coroutineStack.Push (getFilePathAsync_shape_predictor_68_cat_face_landmarks_dat_filepath_Coroutine);
            yield return StartCoroutine (getFilePathAsync_shape_predictor_68_cat_face_landmarks_dat_filepath_Coroutine);

            coroutineStack.Clear ();

            Run ();
        }
        #endif

        private void Run ()
        {

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

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector (frontal_cat_face_svm_filepath, shape_predictor_68_cat_face_landmarks_dat_filepath);
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

                //draw face rects
                faceLandmarkDetector.DrawDetectResult (texture2D, 255, 0, 0, 255, 3);
            }

            faceLandmarkDetector.Dispose ();

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture2D;
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
            foreach (var coroutine in coroutineStack) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
        }

        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorExample");
            #endif
        }
    }
}