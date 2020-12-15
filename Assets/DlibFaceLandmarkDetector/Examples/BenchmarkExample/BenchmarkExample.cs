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
    /// Benchmark Example
    /// </summary>
    public class BenchmarkExample : MonoBehaviour
    {
        /// <summary>
        /// The number of benchmark times.
        /// </summary>
        public int times = 100;

        public Texture2D smallImage;

        public Texture2D largeImage;

        Texture2D dstTexture2D;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

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
            Run();
#endif
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);
        }

        private void StartBenchmark(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100)
        {
            string result = Benchmark(targetImg, detector, times);
            Debug.Log(result);
            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = result;
            }

            ShowImage(targetImg);
        }

        private string Benchmark(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            string result = "sp_name: " + dlibShapePredictorFileName + "\n";
            result += "times: " + times + " (size: " + targetImg.width + "*" + targetImg.height + ")" + "\n";

            detector.SetImage(targetImg);

            // FaceLandmarkDetector.Detect() benchmark.
            sw.Start();
            for (int i = 0; i < times; ++i)
            {
                detector.Detect();
            }
            sw.Stop();
            result += " Detect(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms" + "\n";


            // FaceLandmarkDetector.DetectLandmark() benchmark.
            List<Rect> detectResult = detector.Detect();
            sw.Reset();
            sw.Start();
            for (int i = 0; i < times; ++i)
            {
                foreach (var rect in detectResult)
                {
                    detector.DetectLandmark(rect);
                }
            }
            sw.Stop();
            result += " DetectLandmark(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms";

            return result;
        }

        private void ShowImage(Texture2D texture2D)
        {
            if (dstTexture2D != null)
                Texture2D.Destroy(dstTexture2D);
            dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            dstTexture2D.SetPixels32(texture2D.GetPixels32());
            dstTexture2D.Apply();

            gameObject.transform.localScale = new Vector3(texture2D.width, texture2D.height, 1);

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

            faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            List<Rect> detectResult = faceLandmarkDetector.Detect();

            foreach (var rect in detectResult)
            {
                //detect landmark points
                faceLandmarkDetector.DetectLandmark(rect);
                // draw landmark points
                faceLandmarkDetector.DrawDetectLandmarkResult(dstTexture2D, 0, 255, 0, 255);
            }

            // draw face rect
            faceLandmarkDetector.DrawDetectResult(dstTexture2D, 255, 0, 0, 255, 2);

            gameObject.GetComponent<Renderer>().material.mainTexture = dstTexture2D;
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
            if (dstTexture2D != null)
                Texture2D.Destroy(dstTexture2D);

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
        }

        /// <summary>
        /// Raises the benchmark small mage button click event.
        /// </summary>
        public void OnBenchmarkSmallImageButtonClick()
        {
            if (faceLandmarkDetector == null)
                return;

            StartBenchmark(smallImage, faceLandmarkDetector, times);
        }

        /// <summary>
        /// Raises the benchmark large image button click event.
        /// </summary>
        public void OnBenchmarkLargeImageButtonClick()
        {
            if (faceLandmarkDetector == null)
                return;

            StartBenchmark(largeImage, faceLandmarkDetector, times);
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