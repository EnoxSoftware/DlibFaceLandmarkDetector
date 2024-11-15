using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Benchmark Example
    /// </summary>
    public class BenchmarkExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

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
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        public enum DetectMode
        {
            None,
            ValueTuple,
            NoAlloc
        }

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            dlibShapePredictorFilePath = await Utils.getFilePathAsyncTask(dlibShapePredictorFileName, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            if (faceLandmarkDetector.GetShapePredictorNumParts() != 68)
                Debug.LogWarning("The DrawDetectLandmarkResult method does not support ShapePredictorNumParts sizes other than 68 points, so the drawing will be incorrect."
                    + " If you want to draw the result correctly, we recommend using the OpenCVForUnityUtils.DrawFaceLandmark method.");
        }

        private void StartBenchmark(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100, DetectMode detectMode = DetectMode.None)
        {
            string result = null;
            switch (detectMode)
            {
                case DetectMode.None:
                    result = Benchmark(targetImg, detector, times);
                    Debug.Log(result);
                    ShowImage(targetImg);
                    break;
                case DetectMode.ValueTuple:
                    result = BenchmarkValueTuple(targetImg, detector, times);
                    Debug.Log(result);
                    ShowImageValueTuple(targetImg);
                    break;
                case DetectMode.NoAlloc:
                    result = BenchmarkNoGCAlloc(targetImg, detector, times);
                    Debug.Log(result);
                    ShowImageNoGCAlloc(targetImg);
                    break;
                default:
                    break;
            }

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = result;
            }
        }

        private string Benchmark(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            string result = "sp_name: " + dlibShapePredictorFileName + "\n";
            result += "times: " + times + " (size: " + targetImg.width + "*" + targetImg.height + ")" + "\n";

            detector.SetImage(targetImg);

            // FaceLandmarkDetector.Detect() benchmark.
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample("GCAllocTest: Detect()");
            for (int i = 0; i < times; ++i)
            {
                detector.Detect();
            }
            UnityEngine.Profiling.Profiler.EndSample();
            sw.Stop();
            result += " Detect(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms" + "\n";


            // FaceLandmarkDetector.DetectLandmark() benchmark.
            List<Rect> detectResult = detector.Detect();
            sw.Reset();
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample("GCAllocTest: DetectLandmark()");
            for (int i = 0; i < times; ++i)
            {
                foreach (var rect in detectResult)
                {
                    detector.DetectLandmark(rect);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            sw.Stop();
            result += " DetectLandmark(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms";

            return result;
        }

        private string BenchmarkValueTuple(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            string result = "sp_name: " + dlibShapePredictorFileName + "\n";
            result += "times: " + times + " (size: " + targetImg.width + "*" + targetImg.height + ")" + "\n";

            detector.SetImage(targetImg);

            // FaceLandmarkDetector.Detect() benchmark.
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample("GCAllocTest: DetectValueTuple()");
            for (int i = 0; i < times; ++i)
            {
                detector.DetectValueTuple();
            }
            UnityEngine.Profiling.Profiler.EndSample();
            sw.Stop();
            result += " Detect(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms" + "\n";


            // FaceLandmarkDetector.DetectLandmark() benchmark.
            (double x, double y, double width, double height)[] detectResult = detector.DetectValueTuple();
            sw.Reset();
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample("GCAllocTest: DetectLandmark()");
            for (int i = 0; i < times; ++i)
            {
                foreach (var rect in detectResult)
                {
                    detector.DetectLandmark(rect);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            sw.Stop();
            result += " DetectLandmark(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms";

            return result;
        }

        private string BenchmarkNoGCAlloc(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            string result = "sp_name: " + dlibShapePredictorFileName + "\n";
            result += "times: " + times + " (size: " + targetImg.width + "*" + targetImg.height + ")" + "\n";

            detector.SetImage(targetImg);

            int detectCount = detector.DetectOnly();
#if NET_STANDARD_2_1
            Span<double> detectResult = stackalloc double[detectCount * 6];
#else
            double[] detectResult = new double[detectCount * 6];
#endif

            // FaceLandmarkDetector.Detect() benchmark.
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample("GCAllocTest: DetectOnly()");
            for (int i = 0; i < times; ++i)
            {
                detector.DetectOnly();
                detector.GetDetectResult(detectResult);
            }
            UnityEngine.Profiling.Profiler.EndSample();
            sw.Stop();
            result += " Detect(): " + sw.ElapsedMilliseconds + "ms" + " Avg:" + sw.ElapsedMilliseconds / times + "ms" + "\n";


            // FaceLandmarkDetector.DetectLandmark() benchmark.
            int detectLandmarkCount = detector.DetectLandmarkOnly(detectResult[0], detectResult[1], detectResult[2], detectResult[3]);
#if NET_STANDARD_2_1
            Span<double> detectLandmarkResult = stackalloc double[detectLandmarkCount * 2];
#else
            double[] detectLandmarkResult = new double[detectLandmarkCount * 2];
#endif

            sw.Reset();
            sw.Start();
            UnityEngine.Profiling.Profiler.BeginSample("GCAllocTest: DetectLandmarkOnly()");
            for (int i = 0; i < times; ++i)
            {
                for (int p = 0; p < detectCount; p++)
                {
                    detector.DetectLandmarkOnly(detectResult[p * 6 + 0], detectResult[p * 6 + 1], detectResult[p * 6 + 2], detectResult[p * 6 + 3]);
                    detector.GetDetectLandmarkResult(detectLandmarkResult);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
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

            resultPreview.texture = dstTexture2D;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)dstTexture2D.width / dstTexture2D.height;
        }

        private void ShowImageValueTuple(Texture2D texture2D)
        {
            if (dstTexture2D != null)
                Texture2D.Destroy(dstTexture2D);
            dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            dstTexture2D.SetPixels32(texture2D.GetPixels32());
            dstTexture2D.Apply();

            faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            (double x, double y, double width, double height)[] detectResult = faceLandmarkDetector.DetectValueTuple();

            foreach (var rect in detectResult)
            {
                //detect landmark points
                faceLandmarkDetector.DetectLandmark(rect);
                // draw landmark points
                faceLandmarkDetector.DrawDetectLandmarkResult(dstTexture2D, 0, 255, 0, 255);
            }

            // draw face rect
            faceLandmarkDetector.DrawDetectResult(dstTexture2D, 255, 0, 0, 255, 2);

            resultPreview.texture = dstTexture2D;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)dstTexture2D.width / dstTexture2D.height;
        }

        private void ShowImageNoGCAlloc(Texture2D texture2D)
        {
            if (dstTexture2D != null)
                Texture2D.Destroy(dstTexture2D);
            dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            dstTexture2D.SetPixels32(texture2D.GetPixels32());
            dstTexture2D.Apply();

            faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            int detectCount = faceLandmarkDetector.DetectOnly();
#if NET_STANDARD_2_1
            Span<double> detectResult = stackalloc double[detectCount * 6];
#else
            double[] detectResult = new double[detectCount * 6];
#endif
            faceLandmarkDetector.GetDetectResult(detectResult);

#if NET_STANDARD_2_1
            Span<double> detectLandmarkResult = stackalloc double[(int)faceLandmarkDetector.GetShapePredictorNumParts() * 2];
#else
            double[] detectLandmarkResult = new double[(int)faceLandmarkDetector.GetShapePredictorNumParts() * 2];
#endif

            for (int i = 0; i < detectCount; i++)
            {
                //detect landmark points
                faceLandmarkDetector.DetectLandmarkOnly(detectResult[i * 6 + 0], detectResult[i * 6 + 1], detectResult[i * 6 + 2], detectResult[i * 6 + 3]);
                faceLandmarkDetector.GetDetectLandmarkResult(detectLandmarkResult);

                // draw landmark points
                faceLandmarkDetector.DrawDetectLandmarkResult(dstTexture2D, 0, 255, 0, 255);
            }

            // draw face rect
            faceLandmarkDetector.DrawDetectResult(dstTexture2D, 255, 0, 0, 255, 2);

            resultPreview.texture = dstTexture2D;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)dstTexture2D.width / dstTexture2D.height;
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

            if (cts != null)
                cts.Dispose();
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
        /// Raises the benchmark small mage button click event.
        /// </summary>
        public void OnBenchmarkValueTupleSmallImageButtonClick()
        {
            if (faceLandmarkDetector == null)
                return;

            StartBenchmark(smallImage, faceLandmarkDetector, times, DetectMode.ValueTuple);
        }

        /// <summary>
        /// Raises the benchmark large image button click event.
        /// </summary>
        public void OnBenchmarkValueTupleLargeImageButtonClick()
        {
            if (faceLandmarkDetector == null)
                return;

            StartBenchmark(largeImage, faceLandmarkDetector, times, DetectMode.ValueTuple);
        }

        /// <summary>
        /// Raises the benchmark small mage button click event.
        /// </summary>
        public void OnBenchmarkNoGCAllocSmallImageButtonClick()
        {
            if (faceLandmarkDetector == null)
                return;

            StartBenchmark(smallImage, faceLandmarkDetector, times, DetectMode.NoAlloc);
        }

        /// <summary>
        /// Raises the benchmark large image button click event.
        /// </summary>
        public void OnBenchmarkNoGCAllocLargeImageButtonClick()
        {
            if (faceLandmarkDetector == null)
                return;

            StartBenchmark(largeImage, faceLandmarkDetector, times, DetectMode.NoAlloc);
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