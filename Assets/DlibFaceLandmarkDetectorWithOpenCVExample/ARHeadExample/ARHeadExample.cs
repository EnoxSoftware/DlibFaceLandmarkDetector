using System;
using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.MOT;
using OpenCVForUnity.Extensions.MOT.ByteTrack;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DlibOpenCVUtils = DlibFaceLandmarkDetector.Extensions.DlibOpenCVUtils;
using FpsMonitor = DlibFaceLandmarkDetector.UnityIntegration.Helper.UI.FpsMonitor;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// AR Head Example
    /// Estimates head pose from Dlib landmarks and drives AR face objects in a live frames scene.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Downscaled face detection with full-resolution landmark inference
    /// - Multi-face tracking via <see cref="BYTETracker"/>
    /// - Mapping landmark subsets to <see cref="ARHelper"/> image and object points
    /// - Toggling AR face visualization, axes, head mesh, and effects at runtime
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="ImageOptimizationHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// - <see cref="BYTETracker"/>
    /// - <see cref="ARHelper"/>, <see cref="ARCamera"/>, <see cref="ARGameObject"/>
    ///
    /// Dlib classes and APIs used:
    /// - <see cref="FaceLandmarkDetector"/>: SetImage, Detect, DetectLandmark
    /// - <see cref="DlibOpenCVUtils"/>: SetImage, DrawFaceLandmark
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
    /// </para>
    /// <para>
    /// Effect asset from:
    /// http://ktk-kumamoto.hatenablog.com/entry/2014/09/14/092400
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper), typeof(ImageOptimizationHelper))]
    public class ARHeadExample : MonoBehaviour
    {
        // Constants
        private static readonly string DLIB_SHAPE_PREDICTOR_FILE_NAME = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        // Public Fields
        /// <summary>
        /// Determines if displays face points.
        /// </summary>
        public bool DisplayFacePoints;

        /// <summary>
        /// The display face points toggle.
        /// </summary>
        public Toggle DisplayFacePointsToggle;

        /// <summary>
        /// Determines if displays display axes
        /// </summary>
        public bool DisplayAxes;

        /// <summary>
        /// The display axes toggle.
        /// </summary>
        public Toggle DisplayAxesToggle;

        /// <summary>
        /// Determines if displays head.
        /// </summary>
        public bool DisplayHead;

        /// <summary>
        /// The display head toggle.
        /// </summary>
        public Toggle DisplayHeadToggle;

        /// <summary>
        /// Determines if displays effects.
        /// </summary>
        public bool DisplayEffects;

        /// <summary>
        /// The display effects toggle.
        /// </summary>
        public Toggle DisplayEffectsToggle;

        [Space(10)]

        /// <summary>
        /// ARHelper
        /// </summary>
        public ARHelper ArHelper;

        /// <summary>
        /// ARFacePrefab
        /// </summary>
        public GameObject ArFacePrefab;

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
        /// Determines if enable low pass filter.
        /// </summary>
        public bool EnableLowPassFilter;

        /// <summary>
        /// The enable low pass filter toggle.
        /// </summary>
        public Toggle EnableLowPassFilterToggle;

        /// <summary>
        /// Determines if enable smoothing filter.
        /// </summary>
        public bool EnableSmoothingFilter;

        /// <summary>
        /// The enable smoothing filter toggle.
        /// </summary>
        public Toggle EnableSmoothingFilterToggle;

        // Private Fields
        private Texture2D _texture;
        private FaceLandmarkDetector _faceLandmarkDetector;
        private Vector3[] _objectPoints68;
        private Vector3[] _objectPoints17;
        private Vector3[] _objectPoints6;
        private Vector3[] _objectPoints5;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private ImageOptimizationHelper _imageOptimizationHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private BYTETracker _byteTracker;
        private BYTETrackInfoVisualizer _byteTrackInfoVisualizer;
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;
        private string _dlibShapePredictorFilePath;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            DisplayFacePointsToggle.isOn = DisplayFacePoints;
            DisplayAxesToggle.isOn = DisplayAxes;
            DisplayHeadToggle.isOn = DisplayHead;
            DisplayEffectsToggle.isOn = DisplayEffects;
            EnableDownScaleToggle.isOn = EnableDownScale;
            EnableSkipFrameToggle.isOn = EnableSkipFrame;
            EnableLowPassFilterToggle.isOn = EnableLowPassFilter;
            EnableSmoothingFilterToggle.isOn = EnableSmoothingFilter;

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

            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ", this);
            }

            _objectPoints68 = new Vector3[] {
                new Vector3(-34, 90, 83), // l eye (Interpupillary breadth)
                new Vector3(34, 90, 83), // r eye (Interpupillary breadth)
                new Vector3(0, 50, 117), // nose (Tip)
                new Vector3(0, 32, 97), // nose (Subnasale)
                new Vector3(-79, 90, 10), // l ear (Bitragion breadth)
                new Vector3(79, 90, 10) // r ear (Bitragion breadth)
            };

            _objectPoints17 = new Vector3[] {
                new Vector3(-34, 90, 83), // l eye (Interpupillary breadth)
                new Vector3(34, 90, 83), // r eye (Interpupillary breadth)
                new Vector3(0, 50, 117), // nose (Tip)
                new Vector3(0, 32, 97), // nose (Subnasale)
                new Vector3(-79, 90, 10), // l ear (Bitragion breadth)
                new Vector3(79, 90, 10) // r ear (Bitragion breadth)
            };

            _objectPoints6 = new Vector3[] {
                new Vector3(-34, 90, 83), // l eye (Interpupillary breadth)
                new Vector3(34, 90, 83), // r eye (Interpupillary breadth)
                new Vector3(0, 50, 117), // nose (Tip)
                new Vector3(0, 32, 97) // nose (Subnasale)
            };

            _objectPoints5 = new Vector3[] {
                new Vector3(-23, 90, 83), // l eye (Inner corner of the eye)
                new Vector3(23, 90, 83), // r eye (Inner corner of the eye)
                new Vector3(-50, 90, 80), // l eye (Tail of the eye)
                new Vector3(50, 90, 80), // r eye (Tail of the eye)
                new Vector3(0, 32, 97) // nose (Subnasale)
            };

            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();
            _cts?.Cancel();
            if (_imageOptimizationHelper != null)
            {
                _imageOptimizationHelper.Dispose();
                _imageOptimizationHelper = null;
            }
            DisposeFrameProcessingResources();
            CleanupPreviewResources();
            _faceLandmarkDetector?.Dispose();
            _faceLandmarkDetector = null;
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

            bool isCurrentFrameSkipped = EnableSkipFrame && _imageOptimizationHelper.IsCurrentFrameSkipped();

            // detect faces.
            List<Rect> detectResult = new List<Rect>();

            // detect faces on the downscale image
            if (!isCurrentFrameSkipped)
            {
                Mat downScaleRgbaMat = null;
                float downscaleRatio;
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

                // detect face rects
                List<UnityEngine.Rect> result = _faceLandmarkDetector.Detect();

                if (EnableDownScale)
                {
                    for (int i = 0; i < result.Count; ++i)
                    {
                        var rect = result[i];
                        result[i] = new UnityEngine.Rect(
                            rect.x * downscaleRatio,
                            rect.y * downscaleRatio,
                            rect.width * downscaleRatio,
                            rect.height * downscaleRatio);
                    }
                }

                foreach (var unityRect in result)
                {
                    detectResult.Add(new Rect((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                }
            }

            // face tracking.
            if (_byteTracker != null)
            {
                if (!isCurrentFrameSkipped)
                {
                    _byteTracker.Update(ConvertToBBoxes(detectResult));
                }

                BYTETrackInfo[] trackInfos = _byteTracker.GetActiveTrackInfos();

                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();

                // detect face landmark points.
                DlibOpenCVUtils.SetImage(_faceLandmarkDetector, rgbaMat);

                foreach (var trackInfo in trackInfos)
                {
                    ref readonly BBox bbox = ref trackInfo.BBox;
                    UnityEngine.Rect rect = new UnityEngine.Rect(bbox.X, bbox.Y, bbox.Width, bbox.Height);

                    List<Vector2> points = _faceLandmarkDetector.DetectLandmark(rect);

                    if (points != null)
                    {
                        //Debug.Log("detect");

                        if (DisplayFacePoints)
                        {
                            DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);
                        }

                        Vector3[] objectPoints = null;
                        Vector2[] imagePoints = null;
                        bool isRightEyeOpen = false;
                        bool isLeftEyeOpen = false;
                        bool isMouthOpen = false;
                        if (points.Count == 68)
                        {
                            objectPoints = _objectPoints68;

                            imagePoints = new Vector2[] {
                                new Vector2((points[38].x + points[41].x) / 2, (points[38].y + points[41].y) / 2), // l eye (Interpupillary breadth)
                                new Vector2((points[43].x + points[46].x) / 2, (points[43].y + points[46].y) / 2), // r eye (Interpupillary breadth)
                                new Vector2(points[30].x, points[30].y), // nose (Tip)
                                new Vector2(points[33].x, points[33].y), // nose (Subnasale)
                                new Vector2(points[0].x, points[0].y), // l ear (Bitragion breadth)
                                new Vector2(points[16].x, points[16].y) // r ear (Bitragion breadth)
                            };

                            if (Mathf.Abs((float)(points[43].y - points[46].y)) > Mathf.Abs((float)(points[42].x - points[45].x)) / 5.0)
                            {
                                isRightEyeOpen = true;
                            }

                            if (Mathf.Abs((float)(points[38].y - points[41].y)) > Mathf.Abs((float)(points[39].x - points[36].x)) / 5.0)
                            {
                                isLeftEyeOpen = true;
                            }

                            float noseDistance = Mathf.Abs((float)(points[27].y - points[33].y));
                            float mouseDistance = Mathf.Abs((float)(points[62].y - points[66].y));
                            if (mouseDistance > noseDistance / 5.0)
                            {
                                isMouthOpen = true;
                            }
                            else
                            {
                                isMouthOpen = false;
                            }
                        }
                        else if (points.Count == 17)
                        {

                            objectPoints = _objectPoints17;

                            imagePoints = new Vector2[] {
                                new Vector2((points[2].x + points[3].x) / 2, (points[2].y + points[3].y) / 2), // l eye (Interpupillary breadth)
                                new Vector2((points[4].x + points[5].x) / 2, (points[4].y + points[5].y) / 2), // r eye (Interpupillary breadth)
                                new Vector2(points[0].x, points[0].y), // nose (Tip)
                                new Vector2(points[1].x, points[1].y), // nose (Subnasale)
                                new Vector2(points[6].x, points[6].y), // l ear (Bitragion breadth)
                                new Vector2(points[8].x, points[8].y) // r ear (Bitragion breadth)
                            };

                            if (Mathf.Abs((float)(points[11].y - points[12].y)) > Mathf.Abs((float)(points[4].x - points[5].x)) / 5.0)
                            {
                                isRightEyeOpen = true;
                            }

                            if (Mathf.Abs((float)(points[9].y - points[10].y)) > Mathf.Abs((float)(points[2].x - points[3].x)) / 5.0)
                            {
                                isLeftEyeOpen = true;
                            }

                            float noseDistance = Mathf.Abs((float)(points[3].y - points[1].y));
                            float mouseDistance = Mathf.Abs((float)(points[14].y - points[16].y));
                            if (mouseDistance > noseDistance / 2.0)
                            {
                                isMouthOpen = true;
                            }
                            else
                            {
                                isMouthOpen = false;
                            }
                        }
                        else if (points.Count == 6)
                        {

                            objectPoints = _objectPoints6;

                            imagePoints = new Vector2[] {
                                new Vector2((points[2].x + points[3].x) / 2, (points[2].y + points[3].y) / 2), // l eye (Interpupillary breadth)
                                new Vector2((points[4].x + points[5].x) / 2, (points[4].y + points[5].y) / 2), // r eye (Interpupillary breadth)
                                new Vector2(points[0].x, points[0].y), // nose (Tip)
                                new Vector2(points[1].x, points[1].y) // nose (Subnasale)
                            };

                        }
                        else if (points.Count == 5)
                        {

                            objectPoints = _objectPoints5;

                            imagePoints = new Vector2[] {
                                new Vector2(points[3].x, points[3].y), // l eye (Inner corner of the eye)
                                new Vector2(points[1].x, points[1].y), // r eye (Inner corner of the eye)
                                new Vector2(points[2].x, points[2].y), // l eye (Tail of the eye)
                                new Vector2(points[0].x, points[0].y), // r eye (Tail of the eye)
                                new Vector2(points[4].x, points[4].y) // nose (Subnasale)
                            };

                            if (_fpsMonitor != null)
                            {
                                _fpsMonitor.ConsoleText = "This example supports mainly the face landmark points of 68/17/6 points.";
                            }
                        }

                        ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, "ARFace_" + trackInfo.TrackId, ArHelper.transform);
                        aRGameObject.ObjectPoints = objectPoints;
                        aRGameObject.ImagePoints = imagePoints;

                        if (aRGameObject.TryGetComponent<ARFace>(out ARFace arFace))
                        {
                            arFace.IsRightEyeOpen = isRightEyeOpen;
                            arFace.IsLeftEyeOpen = isLeftEyeOpen;
                            arFace.IsMouthOpen = isMouthOpen;
                        }
                    }
                }

                // Debugging code to log the tracked rects and visualize the track infos
                // LogTrackedRects(trackInfos);
                // VisualizeTrackedRects(rgbaMat, trackInfos);
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

            Mat webCamTextureMat = _multiSourceToMatHelper.FrameMat;

            RecreatePreviewTexture();

            // Initialize ARHelper.
            ArHelper.Initialize();
            UpdateARCameraParameters(webCamTextureMat);

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

            CreateOrRecreateProcessingResources();

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

            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            RecreatePreviewTexture();
            UpdateARCameraParameters(frameMat);
            CreateOrRecreateProcessingResources();

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

            if (ArHelper != null)
            {
                ArHelper.Dispose();
            }
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
        /// Raises the display face points toggle value changed event.
        /// </summary>
        public void OnDisplayFacePointsToggleValueChanged()
        {
            if (DisplayFacePointsToggle.isOn)
            {
                DisplayFacePoints = true;
            }
            else
            {
                DisplayFacePoints = false;
            }
        }

        /// <summary>
        /// Raises the display axes toggle value changed event.
        /// </summary>
        public void OnDisplayAxesToggleValueChanged()
        {
            if (DisplayAxesToggle.isOn)
            {
                DisplayAxes = true;
            }
            else
            {
                DisplayAxes = false;
            }
            // Get all ARFaces from ARHelper and set displayAxes
            foreach (var arGameObject in ArHelper.ARGameObjects)
            {
                if (arGameObject.TryGetComponent<ARFace>(out ARFace arFace))
                {
                    arFace.DisplayAxes = DisplayAxes;
                }
            }
        }

        /// <summary>
        /// Raises the display head toggle value changed event.
        /// </summary>
        public void OnDisplayHeadToggleValueChanged()
        {
            if (DisplayHeadToggle.isOn)
            {
                DisplayHead = true;
            }
            else
            {
                DisplayHead = false;
            }
            // Get all ARFaces from ARHelper and set displayHead
            foreach (var arGameObject in ArHelper.ARGameObjects)
            {
                if (arGameObject.TryGetComponent<ARFace>(out ARFace arFace))
                {
                    arFace.DisplayHead = DisplayHead;
                }
            }
        }

        /// <summary>
        /// Raises the display effects toggle value changed event.
        /// </summary>
        public void OnDisplayEffectsToggleValueChanged()
        {
            if (DisplayEffectsToggle.isOn)
            {
                DisplayEffects = true;
            }
            else
            {
                DisplayEffects = false;
            }
            // Get all ARFaces from ARHelper and set displayEffects
            foreach (var arGameObject in ArHelper.ARGameObjects)
            {
                if (arGameObject.TryGetComponent<ARFace>(out ARFace arFace))
                {
                    arFace.DisplayEffects = DisplayEffects;
                }
            }
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
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged()
        {
            if (EnableLowPassFilterToggle.isOn)
            {
                EnableLowPassFilter = true;
            }
            else
            {
                EnableLowPassFilter = false;
            }
            foreach (var arGameObject in ArHelper.ARGameObjects)
            {
                arGameObject.UseLowPassFilter = EnableLowPassFilter;
            }
        }

        /// <summary>
        /// Raises the enable smoothing filter toggle value changed event.
        /// </summary>
        public void OnEnableSmoothingFilterToggleValueChanged()
        {
            if (EnableSmoothingFilterToggle.isOn)
            {
                EnableSmoothingFilter = true;
            }
            else
            {
                EnableSmoothingFilter = false;
            }
            foreach (var arGameObject in ArHelper.ARGameObjects)
            {
                arGameObject.UseSmoothingFilter = EnableSmoothingFilter;
            }
        }

        /// <summary>
        /// Called when an ARGameObject enters the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnEnterARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnEnterARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name, this);

            if (arGameObject.TryGetComponent<ARFace>(out ARFace arFace))
            {
                arFace.DisplayHead = DisplayHead;
                arFace.DisplayAxes = DisplayAxes;
                arFace.DisplayEffects = DisplayEffects;
            }
            arGameObject.UseLowPassFilter = EnableLowPassFilter;
            arGameObject.UseSmoothingFilter = EnableSmoothingFilter;

            arGameObject.gameObject.SetActive(true);
            //StartCoroutine(arGameObject.GetComponent<ARFace>().EnterAnimation(arGameObject.gameObject, 0f, 1f, 1.0f));

        }

        /// <summary>
        /// Called when an ARGameObject exits the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnExitARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnExitARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name, this);

            arGameObject.gameObject.SetActive(false);
            //StartCoroutine(arGameObject.GetComponent<ARFace>().ExitAnimation(arGameObject.gameObject, 1f, 0f, 0.2f));

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

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.mainTexture = _texture;
            }
        }

        private void UpdateARCameraParameters(Mat frameMat)
        {
            if (frameMat == null || _texture == null || Camera.main == null)
            {
                return;
            }

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation, this);

            Camera.main.orthographicSize = _texture.height / 2f;

            float cameraAspect = Camera.main.aspect;
            float textureAspect = (float)_texture.width / _texture.height;
            float imageSizeScale;
            if (textureAspect > cameraAspect)
            {
                float cameraWidth = Camera.main.orthographicSize * 2f * cameraAspect;
                imageSizeScale = cameraWidth / _texture.width;
            }
            else
            {
                imageSizeScale = 1f;
            }

            Debug.Log("imageSizeScale " + imageSizeScale, this);
            transform.localScale = new Vector3(_texture.width * imageSizeScale, _texture.height * imageSizeScale, 1);

            if (ArHelper != null)
            {
                ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, frameMat.width(), frameMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
                ArHelper.ARCamera.SetCamMatrixValuesFromImageSize();
            }
        }

        private void DisposeFrameProcessingResources()
        {
            _byteTracker?.Dispose();
            _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose();
            _byteTrackInfoVisualizer = null;
        }

        private void CreateOrRecreateProcessingResources()
        {
            DisposeFrameProcessingResources();

            int fps = 30;
            if (_multiSourceToMatHelper.MatSource is ICameraMatSource cameraHelper)
            {
                fps = (int)cameraHelper.FPS;
            }
            else if (_multiSourceToMatHelper.MatSource is IVideoFileMatSource videoHelper)
            {
                fps = (int)videoHelper.FPS;
            }

            _byteTracker = new BYTETracker(fps, 30, mot20: false);
            _byteTrackInfoVisualizer = new BYTETrackInfoVisualizer();
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

        /// <summary>
        /// Finds or creates an ARGameObject with the specified AR marker name.
        /// </summary>
        /// <param name="arGameObjects"></param>
        /// <param name="id"></param>
        /// <param name="parentTransform"></param>
        /// <returns></returns>
        private ARGameObject FindOrCreateARGameObject(List<ARGameObject> arGameObjects, string id, Transform parentTransform)
        {
            ARGameObject FindARGameObjectByName(List<ARGameObject> arGameObjects, string targetName)
            {
                foreach (ARGameObject obj in arGameObjects)
                {
                    if (obj != null && obj.name == targetName)
                    {
                        return obj;
                    }
                }
                return null;
            }

            ARGameObject arGameObject = FindARGameObjectByName(arGameObjects, id);
            if (arGameObject == null)
            {
                arGameObject = Instantiate(ArFacePrefab, parentTransform).GetComponent<ARGameObject>();
                arGameObject.name = id;

                arGameObject.GetComponent<ARFace>().SetInfoPlateTexture(id);

                arGameObject.OnEnterARCameraViewport.AddListener(OnEnterARCameraViewport);
                arGameObject.OnExitARCameraViewport.AddListener(OnExitARCameraViewport);
                arGameObject.gameObject.SetActive(false);
                arGameObjects.Add(arGameObject);
            }
            return arGameObject;
        }

        /// <summary>
        /// Converts detected face rectangles to BBox array for BYTETracker.
        /// </summary>
        /// <param name="detectedObjects">Detected face rectangles.</param>
        /// <returns>BBox array.</returns>
        private static BBox[] ConvertToBBoxes(List<Rect> detectedObjects)
        {
            BBox[] bboxes = new BBox[detectedObjects.Count];
            for (int i = 0; i < detectedObjects.Count; i++)
            {
                Rect rect = detectedObjects[i];
                bboxes[i] = new BBox(rect.x, rect.y, rect.width, rect.height, 1.0f, 0);
            }
            return bboxes;
        }

        /// <summary>
        /// Logs tracked face information to Debug.Log.
        /// </summary>
        /// <param name="trackInfos">Active track information to log.</param>
        private void LogTrackedRects(BYTETrackInfo[] trackInfos)
        {
            if (trackInfos == null || trackInfos.Length == 0)
            {
                Debug.Log("trackInfos count: 0");
                return;
            }

            var logMessage = new System.Text.StringBuilder();
            logMessage.AppendLine($"trackInfos count: {trackInfos.Length}");

            foreach (var trackInfo in trackInfos)
            {
                ref readonly BBox bbox = ref trackInfo.BBox;
                logMessage.AppendLine($"trackInfo TrackId: {trackInfo.TrackId} State: {trackInfo.State} x: {bbox.X} y: {bbox.Y} width: {bbox.Width} height: {bbox.Height}");
            }

            Debug.Log(logMessage.ToString());
        }

        /// <summary>
        /// Visualizes tracked face rectangles on the image for debugging.
        /// </summary>
        /// <param name="rgbaMat">RGBA image to draw on.</param>
        /// <param name="trackInfos">Track information to visualize.</param>
        /// <param name="printResult">Whether to print track details to the console.</param>
        private void VisualizeTrackedRects(Mat rgbaMat, BYTETrackInfo[] trackInfos, bool printResult = false)
        {
            if (_byteTrackInfoVisualizer == null || trackInfos == null)
            {
                return;
            }

            _byteTrackInfoVisualizer.Visualize(rgbaMat, trackInfos, printResult, isRGB: true);
        }
    }
}
