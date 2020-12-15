using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// OBJECT_DETECTOR_FILENAME
        /// </summary>
        protected static readonly string OBJECT_DETECTOR_FILENAME = "frontal_cat_face.svm";

        /// <summary>
        /// The object_detector_filepath.
        /// </summary>
        string object_detector_filepath;

        /// <summary>
        /// SHAPE_PREDICTOR_FILENAME
        /// </summary>
        protected static readonly string SHAPE_PREDICTOR_FILENAME = "sp_cat_face_68.dat";

        /// <summary>
        /// The shape_predictor_filepath.
        /// </summary>
        string shape_predictor_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            object_detector_filepath = Utils.getFilePath(OBJECT_DETECTOR_FILENAME);
            shape_predictor_filepath = Utils.getFilePath(SHAPE_PREDICTOR_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine = Utils.getFilePathAsync(OBJECT_DETECTOR_FILENAME, (result) =>
            {
                object_detector_filepath = result;
            });
            yield return getFilePathAsync_frontal_cat_face_svm_filepath_Coroutine;

            var getFilePathAsync_sp_cat_face_68_dat_filepath_Coroutine = Utils.getFilePathAsync(SHAPE_PREDICTOR_FILENAME, (result) =>
            {
                shape_predictor_filepath = result;
            });
            yield return getFilePathAsync_sp_cat_face_68_dat_filepath_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        private void Run()
        {
            if (string.IsNullOrEmpty(object_detector_filepath))
            {
                Debug.LogError("object detecter file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            if (string.IsNullOrEmpty(shape_predictor_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            Texture2D dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            dstTexture2D.SetPixels32(texture2D.GetPixels32());
            dstTexture2D.Apply();

            gameObject.transform.localScale = new Vector3(texture2D.width, texture2D.height, 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = gameObject.transform.localScale.x;
            float height = gameObject.transform.localScale.y;

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

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector(object_detector_filepath, shape_predictor_filepath);
            faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            List<Rect> detectResult = faceLandmarkDetector.Detect();

            foreach (var rect in detectResult)
            {
                Debug.Log("face : " + rect);

                //detect landmark points
                List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                Debug.Log("face points count : " + points.Count);
                foreach (var point in points)
                {
                    Debug.Log("face point : x " + point.x + " y " + point.y);
                }

                //draw landmark points
                faceLandmarkDetector.DrawDetectLandmarkResult(dstTexture2D, 0, 255, 0, 255);
            }

            //draw face rects
            faceLandmarkDetector.DrawDetectResult(dstTexture2D, 255, 0, 0, 255, 3);

            faceLandmarkDetector.Dispose();

            gameObject.GetComponent<Renderer>().material.mainTexture = dstTexture2D;

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib object detector", "frontal_cat_face.svm");
                fpsMonitor.Add("dlib shape predictor", "sp_cat_face_68.dat");
                fpsMonitor.Add("width", width.ToString());
                fpsMonitor.Add("height", height.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
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