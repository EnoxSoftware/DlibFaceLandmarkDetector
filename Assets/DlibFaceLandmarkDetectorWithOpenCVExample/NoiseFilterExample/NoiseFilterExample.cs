using System;
using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DlibOpenCVUtils = DlibFaceLandmarkDetector.Extensions.DlibOpenCVUtils;
using FpsMonitor = DlibFaceLandmarkDetector.UnityIntegration.Helper.UI.FpsMonitor;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// Noise Filter Example
    /// Detects Dlib face landmarks on live frames and smooths landmark coordinates with optional temporal filters.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Selecting among low-pass, Kalman, optical-flow, and combined filters at runtime
    /// - Resetting filters after consecutive frames without a detected face
    /// - Drawing raw and filtered landmarks in different colors for comparison
    /// - Previewing annotated frames via <see cref="OpenCVMatUnityUtils.MatToTexture2D"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    ///
    /// Dlib classes and APIs used:
    /// - <see cref="FaceLandmarkDetector"/>: Detect, DetectLandmark
    /// - <see cref="DlibOpenCVUtils"/>: SetImage, DrawFaceLandmark
    /// - <see cref="LowPassPointsFilter"/>, <see cref="KFPointsFilter"/>, <see cref="OFPointsFilter"/>
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class NoiseFilterExample : MonoBehaviour
    {
        // Enums
        public enum FilterMode : int
        {
            None,
            LowPassFilter,
            KalmanFilter,
            OpticalFlowFilter,
            OFAndLPFilter,
        }

        // Constants
        private static readonly string DLIB_SHAPE_PREDICTOR_FILE_NAME = "DlibFaceLandmarkDetector/sp_human_face_68.dat";
        private const int MAXIMUM_ALLOWED_SKIPPED_FRAMES = 4;

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The Toggle for debug mode.
        /// </summary>
        public Toggle IsDebugModeToggle;

        /// <summary>
        /// Determines if is debug mode.
        /// </summary>
        public bool IsDebugMode = false;

        [Space(10)]

        /// <summary>
        /// The filter mode dropdown.
        /// </summary>
        public Dropdown FilterModeDropdown;

        /// <summary>
        /// The filter Mode.
        /// </summary>
        public FilterMode CurrentFilterMode = FilterMode.OFAndLPFilter;

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FaceLandmarkDetector _faceLandmarkDetector;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;
        private string _dlibShapePredictorFilePath;
        private LowPassPointsFilter _lowPassFilter;
        private KFPointsFilter _kalmanFilter;
        private OFPointsFilter _opticalFlowFilter;
        private List<Vector2> _lowPassFilteredPoints = null;
        private List<Vector2> _kalmanFilteredPoints = null;
        private List<Vector2> _opticalFlowFilteredPoints = null;
        private List<Vector2> _ofAndLPFilteredPoints = null;
        private int _skippedFrames;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

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

            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ", this);
            }

            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            _lowPassFilter = new LowPassPointsFilter((int)_faceLandmarkDetector.GetShapePredictorNumParts());
            _kalmanFilter = new KFPointsFilter((int)_faceLandmarkDetector.GetShapePredictorNumParts());
            _opticalFlowFilter = new OFPointsFilter((int)_faceLandmarkDetector.GetShapePredictorNumParts());

            // Update GUI
            IsDebugModeToggle.isOn = IsDebugMode;
            FilterModeDropdown.value = (int)CurrentFilterMode;

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            _faceLandmarkDetector?.Dispose();
            _faceLandmarkDetector = null;

            _lowPassFilter?.Dispose();
            _lowPassFilter = null;
            _kalmanFilter?.Dispose();
            _kalmanFilter = null;
            _opticalFlowFilter?.Dispose();
            _opticalFlowFilter = null;

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

            DlibOpenCVUtils.SetImage(_faceLandmarkDetector, rgbaMat);

            //detect face rects
            List<UnityEngine.Rect> detectResult = _faceLandmarkDetector.Detect();

            UnityEngine.Rect rect = new UnityEngine.Rect();
            List<Vector2> points = null;
            bool shouldResetfilter = false;
            if (detectResult.Count > 0)
            {
                rect = detectResult[0];

                //detect landmark points
                points = _faceLandmarkDetector.DetectLandmark(rect);

                _skippedFrames = 0;
            }
            else
            {
                _skippedFrames++;
                if (_skippedFrames == MAXIMUM_ALLOWED_SKIPPED_FRAMES)
                {
                    shouldResetfilter = true;
                }
            }

            if (points != null)
            {
                switch (CurrentFilterMode)
                {
                    default:
                    case FilterMode.None:
                        break;
                    case FilterMode.LowPassFilter:
                        if (shouldResetfilter)
                        {
                            _lowPassFilter.Reset();
                        }

                        _lowPassFilteredPoints = _lowPassFilter.Process(rgbaMat, points, _lowPassFilteredPoints);
                        break;
                    case FilterMode.KalmanFilter:
                        if (shouldResetfilter)
                        {
                            _kalmanFilter.Reset();
                        }

                        _kalmanFilteredPoints = _kalmanFilter.Process(rgbaMat, points, _kalmanFilteredPoints);
                        break;
                    case FilterMode.OpticalFlowFilter:
                        if (shouldResetfilter)
                        {
                            _opticalFlowFilter.Reset();
                        }

                        _opticalFlowFilteredPoints = _opticalFlowFilter.Process(rgbaMat, points, _opticalFlowFilteredPoints);
                        break;
                    case FilterMode.OFAndLPFilter:
                        if (shouldResetfilter)
                        {
                            _opticalFlowFilter.Reset();
                            _lowPassFilter.Reset();
                        }

                        _opticalFlowFilteredPoints = _opticalFlowFilter.Process(rgbaMat, points, _opticalFlowFilteredPoints);
                        _ofAndLPFilteredPoints = _lowPassFilter.Process(rgbaMat, _opticalFlowFilteredPoints, _ofAndLPFilteredPoints);
                        break;
                }
            }

            if (points != null && !IsDebugMode)
            {
                // draw raw landmark points.
                DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);
            }

            // draw face rect.
            //OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 0, 0, 255), 2);

            // draw filtered lam points.
            if (points != null && !IsDebugMode)
            {
                switch (CurrentFilterMode)
                {
                    default:
                    case FilterMode.None:
                        break;
                    case FilterMode.LowPassFilter:
                        DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, _lowPassFilteredPoints, new Scalar(255, 255, 0, 255), 2);
                        break;
                    case FilterMode.KalmanFilter:
                        DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, _kalmanFilteredPoints, new Scalar(0, 0, 255, 255), 2);
                        break;
                    case FilterMode.OpticalFlowFilter:
                        DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, _opticalFlowFilteredPoints, new Scalar(255, 0, 0, 255), 2);
                        break;
                    case FilterMode.OFAndLPFilter:
                        DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, _ofAndLPFilteredPoints, new Scalar(255, 0, 255, 255), 2);
                        break;
                }
            }

            //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

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

            RecreatePreviewTexture();

            if (_fpsMonitor != null)
            {
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("DlibShapePredictor", "\n" + _dlibShapePredictorFileName);
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }

            _lowPassFilter?.Reset();
            _kalmanFilter?.Reset();
            _opticalFlowFilter?.Reset();
            _skippedFrames = 0;

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

            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

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
        /// Raises the is debug mode toggle value changed event.
        /// </summary>
        /// <param name="result">Result.</param>
        public void OnIsDebugModeToggleValueChanged(bool result)
        {
            IsDebugMode = result;
            _lowPassFilter.IsDebugMode = IsDebugMode;
            _kalmanFilter.IsDebugMode = IsDebugMode;
            _opticalFlowFilter.IsDebugMode = IsDebugMode;
        }

        /// <summary>
        /// Raises the filter mode dropdown value changed event.
        /// </summary>
        public void OnFilterModeDropdownValueChanged(int result)
        {
            if ((int)CurrentFilterMode != result)
            {
                CurrentFilterMode = (FilterMode)result;

                _lowPassFilter?.Reset();
                _kalmanFilter?.Reset();
                _opticalFlowFilter?.Reset();
                _skippedFrames = 0;
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
