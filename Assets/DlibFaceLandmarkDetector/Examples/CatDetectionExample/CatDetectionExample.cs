using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Cat Detection Example
    /// An example of detecting cat face landmarks in a Texture2D image.
    /// </summary>
    public class CatDetectionExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

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
        protected static readonly string OBJECT_DETECTOR_FILENAME = "DlibFaceLandmarkDetector/frontal_cat_face.svm";

        /// <summary>
        /// The object_detector_filepath.
        /// </summary>
        string object_detector_filepath;

        /// <summary>
        /// SHAPE_PREDICTOR_FILENAME
        /// </summary>
        protected static readonly string SHAPE_PREDICTOR_FILENAME = "DlibFaceLandmarkDetector/sp_cat_face_68.dat";

        /// <summary>
        /// The shape_predictor_filepath.
        /// </summary>
        string shape_predictor_filepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            object_detector_filepath = await Utils.getFilePathAsyncTask(OBJECT_DETECTOR_FILENAME, cancellationToken: cts.Token);
            shape_predictor_filepath = await Utils.getFilePathAsyncTask(SHAPE_PREDICTOR_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(object_detector_filepath))
            {
                Debug.LogError("object detecter file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            if (string.IsNullOrEmpty(shape_predictor_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }

            Texture2D dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            dstTexture2D.SetPixels32(texture2D.GetPixels32());
            dstTexture2D.Apply();

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

            resultPreview.texture = dstTexture2D;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)dstTexture2D.width / dstTexture2D.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib object detector", "frontal_cat_face.svm");
                fpsMonitor.Add("dlib shape predictor", "sp_cat_face_68.dat");
                fpsMonitor.Add("width", dstTexture2D.width.ToString());
                fpsMonitor.Add("height", dstTexture2D.height.ToString());
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
    }
}