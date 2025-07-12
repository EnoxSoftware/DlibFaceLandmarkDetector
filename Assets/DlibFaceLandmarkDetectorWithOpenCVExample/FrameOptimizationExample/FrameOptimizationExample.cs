using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Frame Optimization Example
    /// An example of frame downscaling and skipping using the Optimization MultiSource2MatHelper.
    /// http://www.learnopencv.com/speeding-up-dlib-facial-landmark-detector/
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper), typeof(ImageOptimizationHelper))]
    public class FrameOptimizationExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool EnableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle EnableDownScaleToggle;

        /// <summary>
        /// Determines if enable skipframe.
        /// </summary>
        public bool EnableSkipFrame;

        /// <summary>
        /// The enable skipframe toggle.
        /// </summary>
        public Toggle EnableSkipFrameToggle;

        /// <summary>
        /// Determines if use OpenCV FaceDetector for face detection.
        /// </summary>
        public bool UseOpenCVFaceDetector;

        /// <summary>
        /// The use OpenCV FaceDetector toggle.
        /// </summary>
        public Toggle UseOpenCVFaceDetectorToggle;

        // Private Fields
        /// <summary>
        /// The gray mat.
        /// </summary>
        private Mat _grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The cascade.
        /// </summary>
        private CascadeClassifier _cascade;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        private ImageOptimizationHelper _imageOptimizationHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        private FaceLandmarkDetector _faceLandmarkDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The detection result.
        /// </summary>
        private List<(double x, double y, double width, double height)> _detectionResult;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        private string _haarcascadeFrontalfaceAltXmlFilepath;

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

            EnableDownScaleToggle.isOn = EnableDownScale;
            EnableSkipFrameToggle.isOn = EnableSkipFrame;
            UseOpenCVFaceDetectorToggle.isOn = UseOpenCVFaceDetector;

            _imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _haarcascadeFrontalfaceAltXmlFilepath = await DlibEnv.GetFilePathTaskAsync("DlibFaceLandmarkDetector/haarcascade_frontalface_alt.xml", cancellationToken: _cts.Token);
            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathTaskAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                // detect faces on the downscale image
                if (!EnableSkipFrame || !_imageOptimizationHelper.IsCurrentFrameSkipped())
                {

                    Mat downScaleRgbaMat = null;
                    float DOWNSCALE_RATIO = 1.0f;
                    if (EnableDownScale)
                    {
                        downScaleRgbaMat = _imageOptimizationHelper.GetDownScaleMat(rgbaMat);
                        DOWNSCALE_RATIO = _imageOptimizationHelper.DownscaleRatio;
                    }
                    else
                    {
                        downScaleRgbaMat = rgbaMat;
                        DOWNSCALE_RATIO = 1.0f;
                    }

                    // set the downscale mat
                    DlibOpenCVUtils.SetImage(_faceLandmarkDetector, downScaleRgbaMat);

                    //detect face rects
                    if (UseOpenCVFaceDetector)
                    {
                        // convert image to greyscale.
                        Imgproc.cvtColor(downScaleRgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

                        using (Mat equalizeHistMat = new Mat())
                        using (MatOfRect faces = new MatOfRect())
                        {
                            Imgproc.equalizeHist(_grayMat, equalizeHistMat);

                            _cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, (equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), (0, 0));

                            _detectionResult = faces.toValueTupleArrayAsDouble().ToList();
                        }
                    }
                    else
                    {
                        // Dlib's face detection processing time increases in proportion to image size.
                        _detectionResult = _faceLandmarkDetector.DetectValueTuple();
                    }

                    if (EnableDownScale && _detectionResult != null)
                    {
                        for (int i = 0; i < _detectionResult.Count; ++i)
                        {
                            _detectionResult[i] = (
                                _detectionResult[i].x * DOWNSCALE_RATIO,
                                _detectionResult[i].y * DOWNSCALE_RATIO,
                                _detectionResult[i].width * DOWNSCALE_RATIO,
                                _detectionResult[i].height * DOWNSCALE_RATIO
                            );
                        }
                    }
                }


                if (_detectionResult != null)
                {
                    // set the original scale image
                    DlibOpenCVUtils.SetImage(_faceLandmarkDetector, rgbaMat);
                    // detect face landmarks on the original image
                    foreach (var rect in _detectionResult)
                    {

                        //detect landmark points
                        List<(double x, double y)> points = _faceLandmarkDetector.DetectLandmark(rect);

                        //draw landmark points
                        DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, points, (0, 255, 0, 255), 2);
                        //draw face rect
                        DlibOpenCVUtils.DrawFaceRect(rgbaMat, rect, (255, 0, 0, 255), 2);
                    }
                }

                Imgproc.putText(rgbaMat, "Original:(" + rgbaMat.width() + "," + rgbaMat.height() + ") DownScale:(" + rgbaMat.width() / _imageOptimizationHelper.DownscaleRatio + "," + rgbaMat.height() / _imageOptimizationHelper.DownscaleRatio + ") FrameSkipping: " + _imageOptimizationHelper.FrameSkippingRatio, (5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
            _imageOptimizationHelper?.Dispose();
            _faceLandmarkDetector?.Dispose();
            _cascade?.Dispose();
            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();
            Mat downscaleMat = _imageOptimizationHelper.GetDownScaleMat(rgbaMat);

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("original_width", _multiSource2MatHelper.GetWidth().ToString());
                _fpsMonitor.Add("original_height", _multiSource2MatHelper.GetHeight().ToString());
                _fpsMonitor.Add("downscaleRaito", _imageOptimizationHelper.DownscaleRatio.ToString());
                _fpsMonitor.Add("frameSkippingRatio", _imageOptimizationHelper.FrameSkippingRatio.ToString());
                _fpsMonitor.Add("downscale_width", downscaleMat.width().ToString());
                _fpsMonitor.Add("downscale_height", downscaleMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _grayMat?.Dispose(); _grayMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            if (EnableDownScaleToggle.isOn)
            {
                EnableDownScale = true;
            }
            else
            {
                EnableDownScale = false;
            }
        }

        /// <summary>
        /// Raises the enable skipframe toggle value changed event.
        /// </summary>
        public void OnEnableSkipFrameToggleValueChanged()
        {
            if (EnableSkipFrameToggle.isOn)
            {
                EnableSkipFrame = true;
            }
            else
            {
                EnableSkipFrame = false;
            }
        }

        /// <summary>
        /// Raises the use OpenCV FaceDetector toggle value changed event.
        /// </summary>
        public void OnUseOpenCVFaceDetectorToggleValueChanged()
        {
            if (UseOpenCVFaceDetectorToggle.isOn)
            {
                UseOpenCVFaceDetector = true;
            }
            else
            {
                UseOpenCVFaceDetector = false;
            }
        }

        // Private Methods
        private void Run()
        {
            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ");
            }

            _cascade = new CascadeClassifier(_haarcascadeFrontalfaceAltXmlFilepath);
#if !UNITY_WSA_10_0
            if (_cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from \"OpenCVForUnity/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ");
            }
#endif

            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            _multiSource2MatHelper.Initialize();
        }
    }
}
