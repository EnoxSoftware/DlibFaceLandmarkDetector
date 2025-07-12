using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Texture2DToMat Example
    /// An example of using "Dlib FaceLandmark Detector" together with "OpenCV for Unity".
    /// </summary>
    public class Texture2DToMatExample : MonoBehaviour
    {
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
        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        private string _dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

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

            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;

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

        private void OnDestroy()
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

            Mat imgMat = new Mat(ImgTexture.height, ImgTexture.width, CvType.CV_8UC4);

            // Convert Unity Texture2D to OpenCV Mat.
            OpenCVMatUtils.Texture2DToMat(ImgTexture, imgMat);
            Debug.Log("imgMat dst ToString " + imgMat.ToString());


            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            DlibOpenCVUtils.SetImage(faceLandmarkDetector, imgMat);


            //detect face rectdetecton
            List<FaceLandmarkDetector.RectDetection> detectResult = faceLandmarkDetector.DetectRectDetection();

            foreach (var result in detectResult)
            {
                Debug.Log("rect : " + result.rect);
                Debug.Log("detection_confidence : " + result.detection_confidence);
                Debug.Log("weight_index : " + result.weight_index);

                //detect landmark points
                List<Vector2> points = faceLandmarkDetector.DetectLandmark(result.rect);

                Debug.Log("face points count : " + points.Count);
                //draw landmark points
                DlibOpenCVUtils.DrawFaceLandmark(imgMat, points, new Scalar(0, 255, 0, 255), 2, true);

                //draw face rect
                DlibOpenCVUtils.DrawFaceRect(imgMat, result, new Scalar(255, 0, 0, 255), 2);
            }

            faceLandmarkDetector.Dispose();

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            // Convert OpenCV Mat to Unity Texture2D.
            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", imgMat.width().ToString());
                _fpsMonitor.Add("height", imgMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }
    }
}
