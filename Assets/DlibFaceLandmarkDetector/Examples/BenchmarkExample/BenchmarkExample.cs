using System;
using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
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
        // Enums
        public enum DetectMode
        {
            None,
            ValueTuple,
            NoAlloc
        }

        // Constants
        private static readonly string DLIB_SHAPE_PREDICTOR_FILE_NAME = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The number of benchmark times.
        /// </summary>
        public int Times = 100;

        public Texture2D SmallImage;

        public Texture2D LargeImage;

        // Private Fields
        private Texture2D _dstTexture2D;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        private FaceLandmarkDetector _faceLandmarkDetector;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        private string _dlibShapePredictorFilePath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathTaskAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {

        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        private void OnDisable()
        {
            if (_dstTexture2D != null) Texture2D.Destroy(_dstTexture2D); _dstTexture2D = null;
            _faceLandmarkDetector?.Dispose();
            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the benchmark small mage button click event.
        /// </summary>
        public void OnBenchmarkSmallImageButtonClick()
        {
            if (_faceLandmarkDetector == null)
                return;

            StartBenchmark(SmallImage, _faceLandmarkDetector, Times);
        }

        /// <summary>
        /// Raises the benchmark large image button click event.
        /// </summary>
        public void OnBenchmarkLargeImageButtonClick()
        {
            if (_faceLandmarkDetector == null)
                return;

            StartBenchmark(LargeImage, _faceLandmarkDetector, Times);
        }

        /// <summary>
        /// Raises the benchmark small mage button click event.
        /// </summary>
        public void OnBenchmarkValueTupleSmallImageButtonClick()
        {
            if (_faceLandmarkDetector == null)
                return;

            StartBenchmark(SmallImage, _faceLandmarkDetector, Times, DetectMode.ValueTuple);
        }

        /// <summary>
        /// Raises the benchmark large image button click event.
        /// </summary>
        public void OnBenchmarkValueTupleLargeImageButtonClick()
        {
            if (_faceLandmarkDetector == null)
                return;

            StartBenchmark(LargeImage, _faceLandmarkDetector, Times, DetectMode.ValueTuple);
        }

        /// <summary>
        /// Raises the benchmark small mage button click event.
        /// </summary>
        public void OnBenchmarkNoGCAllocSmallImageButtonClick()
        {
            if (_faceLandmarkDetector == null)
                return;

            StartBenchmark(SmallImage, _faceLandmarkDetector, Times, DetectMode.NoAlloc);
        }

        /// <summary>
        /// Raises the benchmark large image button click event.
        /// </summary>
        public void OnBenchmarkNoGCAllocLargeImageButtonClick()
        {
            if (_faceLandmarkDetector == null)
                return;

            StartBenchmark(LargeImage, _faceLandmarkDetector, Times, DetectMode.NoAlloc);
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        // Private Methods
        private void Run()
        {
            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ");
            }

            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            if (_faceLandmarkDetector.GetShapePredictorNumParts() != 68)
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

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = result;
            }
        }

        private string Benchmark(Texture2D targetImg, FaceLandmarkDetector detector, int times = 100)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            string result = "sp_name: " + _dlibShapePredictorFileName + "\n";
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

            string result = "sp_name: " + _dlibShapePredictorFileName + "\n";
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
            List<(double x, double y, double width, double height)> detectResult = detector.DetectValueTuple();
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

            string result = "sp_name: " + _dlibShapePredictorFileName + "\n";
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
            if (_dstTexture2D != null)
                Texture2D.Destroy(_dstTexture2D);
            _dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            _dstTexture2D.SetPixels32(texture2D.GetPixels32());
            _dstTexture2D.Apply();

            _faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            List<Rect> detectResult = _faceLandmarkDetector.Detect();

            foreach (var rect in detectResult)
            {
                //detect landmark points
                _faceLandmarkDetector.DetectLandmark(rect);
                // draw landmark points
                _faceLandmarkDetector.DrawDetectLandmarkResult(_dstTexture2D, 0, 255, 0, 255);
            }

            // draw face rect
            _faceLandmarkDetector.DrawDetectResult(_dstTexture2D, 255, 0, 0, 255, 2);

            ResultPreview.texture = _dstTexture2D;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_dstTexture2D.width / _dstTexture2D.height;
        }

        private void ShowImageValueTuple(Texture2D texture2D)
        {
            if (_dstTexture2D != null)
                Texture2D.Destroy(_dstTexture2D);
            _dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            _dstTexture2D.SetPixels32(texture2D.GetPixels32());
            _dstTexture2D.Apply();

            _faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            List<(double x, double y, double width, double height)> detectResult = _faceLandmarkDetector.DetectValueTuple();

            foreach (var rect in detectResult)
            {
                //detect landmark points
                _faceLandmarkDetector.DetectLandmark(rect);
                // draw landmark points
                _faceLandmarkDetector.DrawDetectLandmarkResult(_dstTexture2D, 0, 255, 0, 255);
            }

            // draw face rect
            _faceLandmarkDetector.DrawDetectResult(_dstTexture2D, 255, 0, 0, 255, 2);

            ResultPreview.texture = _dstTexture2D;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_dstTexture2D.width / _dstTexture2D.height;
        }

        private void ShowImageNoGCAlloc(Texture2D texture2D)
        {
            if (_dstTexture2D != null)
                Texture2D.Destroy(_dstTexture2D);
            _dstTexture2D = new Texture2D(texture2D.width, texture2D.height, texture2D.format, false);
            _dstTexture2D.SetPixels32(texture2D.GetPixels32());
            _dstTexture2D.Apply();

            _faceLandmarkDetector.SetImage(texture2D);

            //detect face rects
            int detectCount = _faceLandmarkDetector.DetectOnly();
#if NET_STANDARD_2_1
            Span<double> detectResult = stackalloc double[detectCount * 6];
#else
            double[] detectResult = new double[detectCount * 6];
#endif
            _faceLandmarkDetector.GetDetectResult(detectResult);

#if NET_STANDARD_2_1
            Span<double> detectLandmarkResult = stackalloc double[(int)_faceLandmarkDetector.GetShapePredictorNumParts() * 2];
#else
            double[] detectLandmarkResult = new double[(int)_faceLandmarkDetector.GetShapePredictorNumParts() * 2];
#endif

            for (int i = 0; i < detectCount; i++)
            {
                //detect landmark points
                _faceLandmarkDetector.DetectLandmarkOnly(detectResult[i * 6 + 0], detectResult[i * 6 + 1], detectResult[i * 6 + 2], detectResult[i * 6 + 3]);
                _faceLandmarkDetector.GetDetectLandmarkResult(detectLandmarkResult);

                // draw landmark points
                _faceLandmarkDetector.DrawDetectLandmarkResult(_dstTexture2D, 0, 255, 0, 255);
            }

            // draw face rect
            _faceLandmarkDetector.DrawDetectResult(_dstTexture2D, 255, 0, 0, 255, 2);

            ResultPreview.texture = _dstTexture2D;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_dstTexture2D.width / _dstTexture2D.height;
        }
    }
}
