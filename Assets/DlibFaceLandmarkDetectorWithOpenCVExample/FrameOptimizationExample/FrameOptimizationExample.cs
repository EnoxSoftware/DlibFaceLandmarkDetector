using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.XobjdetectModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DlibOpenCVUtils = DlibFaceLandmarkDetector.Extensions.DlibOpenCVUtils;
using FpsMonitor = DlibFaceLandmarkDetector.UnityIntegration.Helper.UI.FpsMonitor;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Frame Optimization Example
    /// Speeds up Dlib face landmark detection by downscaling and skipping frames before running detection on live input.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Using <see cref="ImageOptimizationHelper"/> with <see cref="MultiSourceToMatHelper"/> for downscale and frame skip
    /// - Optional OpenCV <see cref="CascadeClassifier"/> face detection instead of Dlib detection
    /// - Running landmark inference on the full-resolution Mat after scaled detection
    /// - Drawing results with <see cref="DlibOpenCVUtils"/> and previewing via <see cref="OpenCVMatUnityUtils.MatToTexture2D"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Imgproc"/>, <see cref="CascadeClassifier"/>, <see cref="MatOfRect"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="ImageOptimizationHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    ///
    /// Dlib classes and APIs used:
    /// - <see cref="FaceLandmarkDetector"/>: DetectValueTuple, DetectLandmark
    /// - <see cref="DlibOpenCVUtils"/>: SetImage, DrawFaceLandmark, DrawFaceRect
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://www.learnopencv.com/speeding-up-dlib-facial-landmark-detector/
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper), typeof(ImageOptimizationHelper))]
    public class FrameOptimizationExample : MonoBehaviour
    {
        // Constants
        private static readonly string DLIB_SHAPE_PREDICTOR_FILE_NAME = "DlibFaceLandmarkDetector/sp_human_face_68.dat";
        private static readonly string HAARCASCADE_FRONTALFACE_ALT_XML_FILE_NAME = "DlibFaceLandmarkDetector/haarcascade_frontalface_alt.xml";

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
        private Mat _grayMat;
        private Texture2D _texture;
        private CascadeClassifier _cascade;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private ImageOptimizationHelper _imageOptimizationHelper;
        private FaceLandmarkDetector _faceLandmarkDetector;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private List<(double x, double y, double width, double height)> _detectionResult;
        private string _haarcascadeFrontalfaceAltXmlFilepath;
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;
        private string _dlibShapePredictorFilePath;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            EnableDownScaleToggle.isOn = EnableDownScale;
            EnableSkipFrameToggle.isOn = EnableSkipFrame;
            UseOpenCVFaceDetectorToggle.isOn = UseOpenCVFaceDetector;

            _imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Uses the dlib shape predictor file name selected on the main menu scene.
            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _haarcascadeFrontalfaceAltXmlFilepath = await DlibEnv.GetFilePathAsync(HAARCASCADE_FRONTALFACE_ALT_XML_FILE_NAME, cancellationToken: _cts.Token);
            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ", this);
            }

            _cascade = new CascadeClassifier(_haarcascadeFrontalfaceAltXmlFilepath);
#if !UNITY_WSA_10_0
            if (_cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from \"OpenCVForUnity/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ", this);
            }
#endif

            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();
            _cts?.Cancel();
            DisposeFrameProcessingResources();
            CleanupPreviewResources();
            if (_imageOptimizationHelper != null)
            {
                _imageOptimizationHelper.Dispose();
                _imageOptimizationHelper = null;
            }
            _faceLandmarkDetector?.Dispose();
            _faceLandmarkDetector = null;
            _cascade?.Dispose();
            _cascade = null;
            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            // detect faces on the downscale image
            if (!EnableSkipFrame || !_imageOptimizationHelper.IsCurrentFrameSkipped())
            {

                Mat downScaleRgbaMat = null;
                float downscaleRatio = 1.0f;
                if (EnableDownScale)
                {
                    downScaleRgbaMat = _imageOptimizationHelper.GetDownScaleMat(rgbaMat);
                    downscaleRatio = _imageOptimizationHelper.DownscaleRatio;
                }
                else
                {
                    downScaleRgbaMat = rgbaMat;
                    downscaleRatio = 1.0f;
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

                        _cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0 | Xobjdetect.CASCADE_SCALE_IMAGE, (equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), (0, 0));

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
                            _detectionResult[i].x * downscaleRatio,
                            _detectionResult[i].y * downscaleRatio,
                            _detectionResult[i].width * downscaleRatio,
                            _detectionResult[i].height * downscaleRatio
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

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            Mat downscaleMat = _imageOptimizationHelper.GetDownScaleMat(rgbaMat);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(rgbaMat);

            if (_fpsMonitor != null)
            {
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("DlibShapePredictor", "\n" + _dlibShapePredictorFileName);
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("OriginalWidth", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("OriginalHeight", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("DownscaleRatio", _imageOptimizationHelper.DownscaleRatio.ToString());
                _fpsMonitor.Add("FrameSkippingRatio", _imageOptimizationHelper.FrameSkippingRatio.ToString());
                _fpsMonitor.Add("DownscaleWidth", downscaleMat.width().ToString());
                _fpsMonitor.Add("DownscaleHeight", downscaleMat.height().ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }

            // Call Play only when the helper is not already playing or paused.
            // Re-initialization may restore the previous playback state.
            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
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
        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private void DisposeFrameProcessingResources()
        {
            _grayMat?.Dispose();
            _grayMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat frameMat)
        {
            if (frameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();
            _grayMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC1);
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }
    }
}
