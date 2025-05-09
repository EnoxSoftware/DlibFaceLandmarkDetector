using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using System.Collections.Generic;
using System.Threading;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The image texture.
        /// </summary>
        public Texture2D imgTexture;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            dlibShapePredictorFilePath = await DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsyncTask(dlibShapePredictorFileName, cancellationToken: cts.Token);

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

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            // Convert Unity Texture2D to OpenCV Mat.
            OpenCVForUnity.UnityUtils.Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat dst ToString " + imgMat.ToString());


            FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            OpenCVForUnityUtils.SetImage(faceLandmarkDetector, imgMat);


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
                OpenCVForUnityUtils.DrawFaceLandmark(imgMat, points, new Scalar(0, 255, 0, 255), 2, true);

                //draw face rect
                OpenCVForUnityUtils.DrawFaceRect(imgMat, result, new Scalar(255, 0, 0, 255), 2);
            }

            faceLandmarkDetector.Dispose();

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            // Convert OpenCV Mat to Unity Texture2D.
            OpenCVForUnity.UnityUtils.Utils.matToTexture2D(imgMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", imgMat.width().ToString());
                fpsMonitor.Add("height", imgMat.height().ToString());
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