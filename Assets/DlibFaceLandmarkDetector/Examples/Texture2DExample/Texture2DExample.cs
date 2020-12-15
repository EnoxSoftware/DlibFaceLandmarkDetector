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
    /// Texture2D Example
    /// An example of detecting face landmarks in a Texture2D image.
    /// </summary>
    public class Texture2DExample : MonoBehaviour
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

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(dlibShapePredictorFileName, (result) =>
            {
                getFilePath_Coroutine = null;

                dlibShapePredictorFilePath = result;
                Run();
            });
            StartCoroutine(getFilePath_Coroutine);
#else
            dlibShapePredictorFilePath = Utils.getFilePath(dlibShapePredictorFileName);
            //You can set the relative file path from the project folder.
            //dlibShapePredictorFilePath = "./Assets/StreamingAssets/"+dlibShapePredictorFileName;

            Run();
#endif
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            //if true, The error log of the Native side Dlib will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

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

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);
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

            //draw face rect
            faceLandmarkDetector.DrawDetectResult(dstTexture2D, 255, 0, 0, 255, 2);

            faceLandmarkDetector.Dispose();

            gameObject.GetComponent<Renderer>().material.mainTexture = dstTexture2D;


            Utils.setDebugMode(false);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
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
        /// Raises the disable event.
        /// </summary>
        void OnDisable()
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