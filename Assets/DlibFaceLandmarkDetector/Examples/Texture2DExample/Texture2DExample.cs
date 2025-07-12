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
    /// Texture2D Example
    /// An example of detecting face landmarks in a Texture2D image.
    /// </summary>
    public class Texture2DExample : MonoBehaviour
    {
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
        /// The texture2D.
        /// </summary>
        public Texture2D Texture2D;

        // Private Fields
        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        private string _dlibShapePredictorFilePath;

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
            _cts?.Dispose();
        }

        // Public Methods
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

            //if true, The error log of the Native side Dlib will be displayed on the Unity Editor Console.
            DlibDebug.SetDebugMode(true);

            Texture2D dstTexture2D = new Texture2D(Texture2D.width, Texture2D.height, Texture2D.format, false);
            dstTexture2D.SetPixels32(Texture2D.GetPixels32());
            dstTexture2D.Apply();

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);
            faceLandmarkDetector.SetImage(Texture2D);

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

            if (faceLandmarkDetector.GetShapePredictorNumParts() != 68)
                Debug.LogWarning("The DrawDetectLandmarkResult method does not support ShapePredictorNumParts sizes other than 68 points, so the drawing will be incorrect."
                    + " If you want to draw the result correctly, we recommend using the OpenCVForUnityUtils.DrawFaceLandmark method.");

            //draw face rect
            faceLandmarkDetector.DrawDetectResult(dstTexture2D, 255, 0, 0, 255, 2);

            faceLandmarkDetector.Dispose();

            ResultPreview.texture = dstTexture2D;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)dstTexture2D.width / dstTexture2D.height;

            DlibDebug.SetDebugMode(false);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", dstTexture2D.width.ToString());
                _fpsMonitor.Add("height", dstTexture2D.height.ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }
    }
}
