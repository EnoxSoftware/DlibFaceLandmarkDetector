using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using DlibFaceLandmarkDetector.UnityIntegration.Helper.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DlibOpenCVUtils = DlibFaceLandmarkDetector.Extensions.DlibOpenCVUtils;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Texture2DToMat Example
    /// Loads a Unity <see cref="Texture2D"/>, converts it to an OpenCV <see cref="Mat"/>, and detects Dlib face landmarks.
    ///
    /// Demonstrates:
    /// - Creating a Mat with a matching element type (<see cref="CvType.CV_8UC4"/>)
    /// - Round-trip conversion with <see cref="OpenCVMatUnityUtils.Texture2DToMat"/> and MatToTexture2D
    /// - Face detection and landmark drawing via <see cref="FaceLandmarkDetector"/> and <see cref="DlibOpenCVUtils"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="CvType"/>
    /// - <see cref="OpenCVMatUnityUtils"/>: Texture2DToMat, MatToTexture2D
    ///
    /// Dlib classes and APIs used:
    /// - <see cref="FaceLandmarkDetector"/>: DetectRectDetection, DetectLandmark
    /// - <see cref="DlibOpenCVUtils"/>: SetImage, DrawFaceLandmark, DrawFaceRect
    /// </summary>
    public class Texture2DToMatExample : MonoBehaviour
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
        /// The image texture.
        /// </summary>
        public Texture2D ImgTexture;

        // Private Fields
        private FpsMonitor _fpsMonitor;
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;
        private string _dlibShapePredictorFilePath;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Uses the dlib shape predictor file name selected on the main menu scene.
            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
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

            Mat imgMat = new Mat(ImgTexture.height, ImgTexture.width, CvType.CV_8UC4);

            // Convert Unity Texture2D to OpenCV Mat.
            OpenCVMatUnityUtils.Texture2DToMat(ImgTexture, imgMat);
            Debug.Log("imgMat dst ToString " + imgMat.ToString());

            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            DlibOpenCVUtils.SetImage(faceLandmarkDetector, imgMat);

            //detect face rectdetecton
            List<FaceLandmarkDetector.RectDetection> detectResult = faceLandmarkDetector.DetectRectDetection();

            foreach (var result in detectResult)
            {
                Debug.Log("rect : " + result.Rect);
                Debug.Log("detection_confidence : " + result.DetectionConfidence);
                Debug.Log("weight_index : " + result.WeightIndex);

                //detect landmark points
                List<Vector2> points = faceLandmarkDetector.DetectLandmark(result.Rect);

                Debug.Log("face points count : " + points.Count);
                //draw landmark points
                DlibOpenCVUtils.DrawFaceLandmark(imgMat, points, new Scalar(0, 255, 0, 255), 2, true);

                //draw face rect
                DlibOpenCVUtils.DrawFaceRect(imgMat, result, new Scalar(255, 0, 0, 255), 2);
            }

            faceLandmarkDetector.Dispose();

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            // Convert OpenCV Mat to Unity Texture2D.
            OpenCVMatUnityUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", "\n" + _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", imgMat.width().ToString());
                _fpsMonitor.Add("height", imgMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }
    }
}
