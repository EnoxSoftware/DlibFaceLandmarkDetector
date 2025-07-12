using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// MultiSource Noise Filter Example
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MultiSourceNoiseFilterExample : MonoBehaviour
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
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        private FaceLandmarkDetector _faceLandmarkDetector;

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
        /// The mean points filter.
        /// </summary>
        private LowPassPointsFilter _lowPassFilter;

        /// <summary>
        /// The kanlam filter points filter.
        /// </summary>
        private KFPointsFilter _kalmanFilter;

        /// <summary>
        /// The optical flow points filter.
        /// </summary>
        private OFPointsFilter _opticalFlowFilter;

        private List<Vector2> _lowPassFilteredPoints = null;
        private List<Vector2> _kalmanFilteredPoints = null;
        private List<Vector2> _opticalFlowFilteredPoints = null;
        private List<Vector2> _ofAndLPFilteredPoints = null;

        /// <summary>
        /// The number of skipped frames.
        /// </summary>
        private int _skippedFrames;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        // Use this for initialization
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathTaskAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();

            // Update GUI
            IsDebugModeToggle.isOn = IsDebugMode;
            FilterModeDropdown.value = (int)CurrentFilterMode;
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

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
                                _lowPassFilter.Reset();
                            _lowPassFilteredPoints = _lowPassFilter.Process(rgbaMat, points, _lowPassFilteredPoints);
                            break;
                        case FilterMode.KalmanFilter:
                            if (shouldResetfilter)
                                _kalmanFilter.Reset();
                            _kalmanFilteredPoints = _kalmanFilter.Process(rgbaMat, points, _kalmanFilteredPoints);
                            break;
                        case FilterMode.OpticalFlowFilter:
                            if (shouldResetfilter)
                                _opticalFlowFilter.Reset();
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

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _faceLandmarkDetector?.Dispose();

            _lowPassFilter?.Dispose();
            _kalmanFilter?.Dispose();
            _opticalFlowFilter?.Dispose();

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

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", _multiSource2MatHelper.GetWidth().ToString());
                _fpsMonitor.Add("height", _multiSource2MatHelper.GetHeight().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _lowPassFilter?.Reset();
            _kalmanFilter?.Reset();
            _opticalFlowFilter?.Reset();
            _skippedFrames = 0;
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode;
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
        public void OnPauseButtonCkick()
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
        private void Run()
        {
            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ");
            }

            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            _lowPassFilter = new LowPassPointsFilter((int)_faceLandmarkDetector.GetShapePredictorNumParts());
            _kalmanFilter = new KFPointsFilter((int)_faceLandmarkDetector.GetShapePredictorNumParts());
            _opticalFlowFilter = new OFPointsFilter((int)_faceLandmarkDetector.GetShapePredictorNumParts());

            _multiSource2MatHelper.Initialize();
        }
    }
}
