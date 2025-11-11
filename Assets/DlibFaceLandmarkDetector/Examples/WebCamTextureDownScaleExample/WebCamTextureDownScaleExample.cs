using System;
using System.Collections;
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
    /// WebCamTexture Example
    /// An example of detecting face landmarks in WebCamTexture images.
    /// </summary>
    public class WebCamTextureDownScaleExample : MonoBehaviour
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
        /// Set the name of the device to use.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the name of the device to use.")]
        public string RequestedDeviceName = null;

        /// <summary>
        /// Set the width of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
        public int RequestedWidth = 320;

        /// <summary>
        /// Set the height of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
        public int RequestedHeight = 240;

        /// <summary>
        /// Set FPS of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
        public int RequestedFPS = 30;

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, TooltipAttribute("Set whether to use the front facing camera.")]
        public bool RequestedIsFrontFacing = false;

        /// <summary>
        /// The adjust pixels direction toggle.
        /// </summary>
        public Toggle AdjustPixelsDirectionToggle;

        /// <summary>
        /// Determines if adjust pixels direction.
        /// </summary>
        [SerializeField, TooltipAttribute("Determines if adjust pixels direction.")]
        public bool AdjustPixelsDirection = false;

        /// <summary>
        /// Down scale rate for Dlib processing (0.1-1.0).
        /// </summary>
        [SerializeField, TooltipAttribute("Down scale rate for Dlib processing (0.1-1.0).")]
        [Range(0.1f, 1.0f)]
        public float DownScaleRate = 1.0f;

        // Private Fields
        private WebCamTexture _webCamTexture;
        private WebCamDevice _webCamDevice;
        private Color32[] _colors;
        private Color32[] _rotatedColors;
        private Color32[] _downscaledColors;
        private int _downscaledWidth;
        private int _downscaledHeight;
        private float _lastDownScaleRate = 1.0f;
        private bool _rotate90Degree = false;
        private bool _isInitWaiting = false;
        private bool _hasInitDone = false;
        private ScreenOrientation _screenOrientation;
        private int _screenWidth;
        private int _screenHeight;
        private FaceLandmarkDetector _faceLandmarkDetector;
        private Texture2D _texture;
        private FpsMonitor _fpsMonitor;
        private string _dlibShapePredictorFileName = DLIB_SHAPE_PREDICTOR_FILE_NAME;
        private string _dlibShapePredictorFilePath;
        private CancellationTokenSource _cts = new CancellationTokenSource();
#if ((UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        private bool _isUserRequestingPermission;
#endif

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();
            _dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibShapePredictorFileName;
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";
            _dlibShapePredictorFilePath = await DlibEnv.GetFilePathTaskAsync(_dlibShapePredictorFileName, cancellationToken: _cts.Token);
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";
            Run();
        }

        private void Update()
        {
            if (!_hasInitDone)
                return;

            if (AdjustPixelsDirection)
            {
                if (_screenOrientation != Screen.orientation)
                {
                    Initialize();
                }
            }
            if (_webCamTexture.isPlaying && _webCamTexture.didUpdateThisFrame)
            {
                // Check if downscale rate has changed and reinitialize if needed
                if (_lastDownScaleRate != DownScaleRate)
                {
                    _lastDownScaleRate = DownScaleRate;
                    InitializeDownscaledBuffer();
                    UpdateFpsMonitorDownscaleInfo();
                }

                Color32[] colors = GetColors();
                if (colors != null)
                {
                    if (DownScaleRate < 1.0f && _downscaledColors != null)
                    {
                        // Downscale processing path
                        // 1. Downscale the image
                        DownscaleImage(colors, _texture.width, _texture.height, _downscaledColors, _downscaledWidth, _downscaledHeight);

                        // 2. Set downscaled image and detect faces
                        _faceLandmarkDetector.SetImage<Color32>(_downscaledColors, _downscaledWidth, _downscaledHeight, 4, true);
                        List<Rect> detectResult = _faceLandmarkDetector.Detect();

                        // 3. Detect landmarks on downscaled image and scale up coordinates
                        List<(Rect scaledRect, List<Vector2> scaledLandmarks)> scaledResults = new List<(Rect, List<Vector2>)>();
                        foreach (var rect in detectResult)
                        {
                            List<Vector2> landmarks = _faceLandmarkDetector.DetectLandmark(rect);
                            Rect scaledRect = ScaleUpRect(rect);
                            List<Vector2> scaledLandmarks = ScaleUpLandmarks(landmarks);
                            scaledResults.Add((scaledRect, scaledLandmarks));
                        }

                        // 4. Draw on original size image
                        foreach (var (_, scaledLandmarks) in scaledResults)
                        {
                            DrawLandmarksManual(colors, _texture.width, _texture.height, scaledLandmarks, 0, 255, 0, 255, true);
                        }
                        foreach (var (scaledRect, _) in scaledResults)
                        {
                            DrawRectManual(colors, _texture.width, _texture.height, scaledRect, 255, 0, 0, 255, true);
                        }
                    }
                    else
                    {
                        // Original processing path (no downscale)
                        _faceLandmarkDetector.SetImage<Color32>(colors, _texture.width, _texture.height, 4, true);
                        List<Rect> detectResult = _faceLandmarkDetector.Detect();
                        foreach (var rect in detectResult)
                        {
                            _faceLandmarkDetector.DetectLandmark(rect);
                            _faceLandmarkDetector.DrawDetectLandmarkResult<Color32>(colors, _texture.width, _texture.height, 4, true, 0, 255, 0, 255);
                        }
                        _faceLandmarkDetector.DrawDetectResult<Color32>(colors, _texture.width, _texture.height, 4, true, 255, 0, 0, 255, 2);
                    }
                    _texture.SetPixels32(colors);
                    _texture.Apply(false);
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
            _faceLandmarkDetector?.Dispose();
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

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            if (_hasInitDone)
            {
                RequestedDeviceName = null;
                RequestedIsFrontFacing = !RequestedIsFrontFacing;
                Initialize();
            }
        }

        /// <summary>
        /// Raises the adjust pixels direction toggle value changed event.
        /// </summary>
        public void OnAdjustPixelsDirectionToggleValueChanged()
        {
            if (_hasInitDone)
            {
                if (AdjustPixelsDirectionToggle.isOn != AdjustPixelsDirection)
                {
                    AdjustPixelsDirection = AdjustPixelsDirectionToggle.isOn;
                    Initialize();
                }
            }
        }

        // Private Methods
        private void Run()
        {
            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ");
                return;
            }
            _faceLandmarkDetector = new FaceLandmarkDetector(_dlibShapePredictorFilePath);
            Initialize();
        }

        /// <summary>
        /// Initializes webcam texture.
        /// </summary>
        private void Initialize()
        {
            if (_isInitWaiting)
                return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (RequestedIsFrontFacing)
            {
                int rearCameraFPS = RequestedFPS;
                RequestedFPS = 15;
                StartCoroutine(_Initialize());
                RequestedFPS = rearCameraFPS;
            }
            else
            {
                StartCoroutine(_Initialize());
            }
#else
            StartCoroutine(_Initialize());
#endif
        }

        /// <summary>
        /// Initializes webcam texture by coroutine.
        /// </summary>
        private IEnumerator _Initialize()
        {
            if (_hasInitDone)
                Dispose();
            _isInitWaiting = true;
#if (UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER
            UserAuthorization mode = UserAuthorization.WebCam;
            if (!Application.HasUserAuthorization(mode))
            {
                _isUserRequestingPermission = true;
                yield return Application.RequestUserAuthorization(mode);
                float timeElapsed = 0;
                while (_isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        _isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            if (!Application.HasUserAuthorization(mode))
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Camera permission is denied.";
                }
                _isInitWaiting = false;
                yield break;
            }
#elif UNITY_ANDROID && UNITY_2018_3_OR_NEWER
            string permission = UnityEngine.Android.Permission.Camera;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                _isUserRequestingPermission = true;
                UnityEngine.Android.Permission.RequestUserPermission(permission);
                float timeElapsed = 0;
                while (_isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        _isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Camera permission is denied.";
                }
                _isInitWaiting = false;
                yield break;
            }
#endif
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("Camera device does not exist.");
                _isInitWaiting = false;
                yield break;
            }
            if (!String.IsNullOrEmpty(RequestedDeviceName))
            {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse(RequestedDeviceName, out requestedDeviceIndex))
                {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                    {
                        _webCamDevice = devices[requestedDeviceIndex];
                        _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
                    }
                }
                else
                {
                    for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                    {
                        if (devices[cameraIndex].name == RequestedDeviceName)
                        {
                            _webCamDevice = devices[cameraIndex];
                            _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
                            break;
                        }
                    }
                }
                if (_webCamTexture == null)
                    Debug.Log("Cannot find camera device " + RequestedDeviceName + ".");
            }
            if (_webCamTexture == null)
            {
                var prioritizedKinds = new WebCamKind[]
                {
                    WebCamKind.WideAngle,
                    WebCamKind.Telephoto,
                    WebCamKind.UltraWideAngle,
                    WebCamKind.ColorAndDepth
                };
                foreach (var kind in prioritizedKinds)
                {
                    foreach (var device in devices)
                    {
                        if (device.kind == kind && device.isFrontFacing == RequestedIsFrontFacing)
                        {
                            _webCamDevice = device;
                            _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
                            break;
                        }
                    }
                    if (_webCamTexture != null) break;
                }
            }
            if (_webCamTexture == null)
            {
                _webCamDevice = devices[0];
                _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
            }
            _webCamTexture.Play();
            while (true)
            {
                if (_webCamTexture.didUpdateThisFrame)
                {
                    Debug.Log("name:" + _webCamTexture.deviceName + " width:" + _webCamTexture.width + " height:" + _webCamTexture.height + " fps:" + _webCamTexture.requestedFPS);
                    Debug.Log("videoRotationAngle:" + _webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + _webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + _webCamDevice.isFrontFacing);
                    _screenOrientation = Screen.orientation;
                    _screenWidth = Screen.width;
                    _screenHeight = Screen.height;
                    _isInitWaiting = false;
                    _hasInitDone = true;
                    OnInited();
                    break;
                }
                else
                {
                    yield return 0;
                }
            }
        }
#if ((UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        private IEnumerator OnApplicationFocus(bool hasFocus)
        {
            yield return null;
            if (_isUserRequestingPermission && hasFocus)
                _isUserRequestingPermission = false;
        }
#endif
        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose()
        {
            _rotate90Degree = false;
            _isInitWaiting = false;
            _hasInitDone = false;
            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
                WebCamTexture.Destroy(_webCamTexture);
                _webCamTexture = null;
            }
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
            _downscaledColors = null;
        }

        /// <summary>
        /// Raises the webcam texture initialized event.
        /// </summary>
        private void OnInited()
        {
            if (_colors == null || _colors.Length != _webCamTexture.width * _webCamTexture.height)
            {
                _colors = new Color32[_webCamTexture.width * _webCamTexture.height];
                _rotatedColors = new Color32[_webCamTexture.width * _webCamTexture.height];
            }
            if (AdjustPixelsDirection)
            {
#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                {
                    _rotate90Degree = true;
                }
                else
                {
                    _rotate90Degree = false;
                }
#endif
            }
            if (_rotate90Degree)
            {
                _texture = new Texture2D(_webCamTexture.height, _webCamTexture.width, TextureFormat.RGBA32, false);
            }
            else
            {
                _texture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
            }
            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            // Initialize downscaled image buffer
            _lastDownScaleRate = DownScaleRate;
            InitializeDownscaledBuffer();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", _texture.width.ToString());
                _fpsMonitor.Add("height", _texture.height.ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                UpdateFpsMonitorDownscaleInfo();
            }
        }

        /// <summary>
        /// Initializes or reinitializes the downscaled image buffer based on current settings.
        /// </summary>
        private void InitializeDownscaledBuffer()
        {
            if (!_hasInitDone || _texture == null)
                return;

            if (DownScaleRate < 1.0f)
            {
                _downscaledWidth = Mathf.Max(1, Mathf.RoundToInt(_texture.width * DownScaleRate));
                _downscaledHeight = Mathf.Max(1, Mathf.RoundToInt(_texture.height * DownScaleRate));
                if (_downscaledColors == null || _downscaledColors.Length != _downscaledWidth * _downscaledHeight)
                {
                    _downscaledColors = new Color32[_downscaledWidth * _downscaledHeight];
                }
            }
            else
            {
                _downscaledColors = null;
            }
        }

        /// <summary>
        /// Updates the FpsMonitor display with downscale information.
        /// </summary>
        private void UpdateFpsMonitorDownscaleInfo()
        {
            if (_fpsMonitor != null)
            {
                if (DownScaleRate < 1.0f)
                {
                    _fpsMonitor.Add("downscale rate", DownScaleRate.ToString("F2"));
                    _fpsMonitor.Add("downscaled size", _downscaledWidth.ToString() + "x" + _downscaledHeight.ToString());
                }
                else
                {
                    // Remove downscale info when not using downscale
                    _fpsMonitor.Remove("downscale rate");
                    _fpsMonitor.Remove("downscaled size");
                }
            }
        }

        /// <summary>
        /// Gets the current WebCameraTexture frame that converted to the correct direction.
        /// </summary>
        private Color32[] GetColors()
        {
            _webCamTexture.GetPixels32(_colors);
            if (AdjustPixelsDirection)
            {
                if (_rotate90Degree)
                {
                    Rotate90CW(_colors, _rotatedColors, _webCamTexture.width, _webCamTexture.height);
                    FlipColors(_rotatedColors, _webCamTexture.width, _webCamTexture.height);
                    return _rotatedColors;
                }
                else
                {
                    FlipColors(_colors, _webCamTexture.width, _webCamTexture.height);
                    return _colors;
                }
            }
            return _colors;
        }

        /// <summary>
        /// Flips the colors.
        /// </summary>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private void FlipColors(Color32[] colors, int width, int height)
        {
            int flipCode = int.MinValue;
            if (_webCamDevice.isFrontFacing)
            {
                if (_webCamTexture.videoRotationAngle == 0)
                {
                    flipCode = 1;
                }
                else if (_webCamTexture.videoRotationAngle == 90)
                {
                    flipCode = 1;
                }
                if (_webCamTexture.videoRotationAngle == 180)
                {
                    flipCode = 0;
                }
                else if (_webCamTexture.videoRotationAngle == 270)
                {
                    flipCode = 0;
                }
            }
            else
            {
                if (_webCamTexture.videoRotationAngle == 180)
                {
                    flipCode = -1;
                }
                else if (_webCamTexture.videoRotationAngle == 270)
                {
                    flipCode = -1;
                }
            }
            if (flipCode > int.MinValue)
            {
                if (_rotate90Degree)
                {
                    if (flipCode == 0)
                    {
                        FlipVertical(colors, colors, _webCamTexture.height, _webCamTexture.width);
                    }
                    else if (flipCode == 1)
                    {
                        FlipHorizontal(colors, colors, _webCamTexture.height, _webCamTexture.width);
                    }
                    else if (flipCode < 0)
                    {
                        Rotate180(colors, colors, _webCamTexture.height, _webCamTexture.width);
                    }
                }
                else
                {
                    if (flipCode == 0)
                    {
                        FlipVertical(colors, colors, _webCamTexture.width, _webCamTexture.height);
                    }
                    else if (flipCode == 1)
                    {
                        FlipHorizontal(colors, colors, _webCamTexture.width, _webCamTexture.height);
                    }
                    else if (flipCode < 0)
                    {
                        Rotate180(colors, colors, _webCamTexture.height, _webCamTexture.width);
                    }
                }
            }
        }

        /// <summary>
        /// Flips vertical.
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private void FlipVertical(Color32[] src, Color32[] dst, int width, int height)
        {
            for (var i = 0; i < height / 2; i++)
            {
                var y = i * width;
                var x = (height - i - 1) * width;
                for (var j = 0; j < width; j++)
                {
                    int s = y + j;
                    int t = x + j;
                    Color32 c = src[s];
                    dst[s] = src[t];
                    dst[t] = c;
                }
            }
        }

        /// <summary>
        /// Flips horizontal.
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private void FlipHorizontal(Color32[] src, Color32[] dst, int width, int height)
        {
            for (int i = 0; i < height; i++)
            {
                int y = i * width;
                int x = y + width - 1;
                for (var j = 0; j < width / 2; j++)
                {
                    int s = y + j;
                    int t = x - j;
                    Color32 c = src[s];
                    dst[s] = src[t];
                    dst[t] = c;
                }
            }
        }

        /// <summary>
        /// Rotates 180 degrees.
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private void Rotate180(Color32[] src, Color32[] dst, int height, int width)
        {
            int i = src.Length;
            for (int x = 0; x < i / 2; x++)
            {
                Color32 t = src[x];
                dst[x] = src[i - x - 1];
                dst[i - x - 1] = t;
            }
        }

        /// <summary>
        /// Rotates 90 degrees (CLOCKWISE).
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private void Rotate90CW(Color32[] src, Color32[] dst, int height, int width)
        {
            int i = 0;
            for (int x = height - 1; x >= 0; x--)
            {
                for (int y = 0; y < width; y++)
                {
                    dst[i] = src[x + y * height];
                    i++;
                }
            }
        }

        /// <summary>
        /// Rotates 90 degrees (COUNTERCLOCKWISE).
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="height">Height.</param>
        /// <param name="width">Width.</param>
        private void Rotate90CCW(Color32[] src, Color32[] dst, int width, int height)
        {
            int i = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    dst[i] = src[x + y * width];
                    i++;
                }
            }
        }

        /// <summary>
        /// Downscales the image using nearest neighbor interpolation.
        /// </summary>
        /// <param name="src">Source color array.</param>
        /// <param name="srcWidth">Source width.</param>
        /// <param name="srcHeight">Source height.</param>
        /// <param name="dst">Destination color array.</param>
        /// <param name="dstWidth">Destination width.</param>
        /// <param name="dstHeight">Destination height.</param>
        private void DownscaleImage(Color32[] src, int srcWidth, int srcHeight, Color32[] dst, int dstWidth, int dstHeight)
        {
            float xScale = (float)srcWidth / dstWidth;
            float yScale = (float)srcHeight / dstHeight;

            for (int y = 0; y < dstHeight; y++)
            {
                int srcY = Mathf.FloorToInt(y * yScale);
                int dstY = y * dstWidth;
                for (int x = 0; x < dstWidth; x++)
                {
                    int srcX = Mathf.FloorToInt(x * xScale);
                    int srcIndex = srcY * srcWidth + srcX;
                    int dstIndex = dstY + x;
                    dst[dstIndex] = src[srcIndex];
                }
            }
        }

        /// <summary>
        /// Scales up a Rect from downscaled image coordinates to original image coordinates.
        /// </summary>
        /// <param name="rect">Rect in downscaled image coordinates.</param>
        /// <returns>Rect in original image coordinates.</returns>
        private Rect ScaleUpRect(Rect rect)
        {
            float scaleFactor = 1.0f / DownScaleRate;
            return new Rect(rect.xMin * scaleFactor, rect.yMin * scaleFactor, rect.width * scaleFactor, rect.height * scaleFactor);
        }

        /// <summary>
        /// Scales up landmark coordinates from downscaled image to original image.
        /// </summary>
        /// <param name="landmarks">Landmark coordinates in downscaled image.</param>
        /// <returns>Landmark coordinates in original image.</returns>
        private List<Vector2> ScaleUpLandmarks(List<Vector2> landmarks)
        {
            if (landmarks == null || landmarks.Count == 0)
                return landmarks;

            float scaleFactor = 1.0f / DownScaleRate;
            List<Vector2> scaledLandmarks = new List<Vector2>(landmarks.Count);
            for (int i = 0; i < landmarks.Count; i++)
            {
                scaledLandmarks.Add(new Vector2(landmarks[i].x * scaleFactor, landmarks[i].y * scaleFactor));
            }
            return scaledLandmarks;
        }

        /// <summary>
        /// Draws a rectangle on the image buffer.
        /// </summary>
        /// <param name="colors">Color array of the image.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="rect">Rectangle to draw.</param>
        /// <param name="r">Red component (0-255).</param>
        /// <param name="g">Green component (0-255).</param>
        /// <param name="b">Blue component (0-255).</param>
        /// <param name="a">Alpha component (0-255).</param>
        /// <param name="flip">If true, the coordinates will be flipped vertically.</param>
        private void DrawRectManual(Color32[] colors, int width, int height, Rect rect, byte r, byte g, byte b, byte a, bool flip)
        {
            // Scale thickness based on image size (base: 320x240, default thickness: 2)
            const float baseSize = 320f;
            const int baseThickness = 2;
            float scaleFactor = Mathf.Max(width, height) / baseSize;
            int scaledThickness = Mathf.Max(1, Mathf.RoundToInt(baseThickness * scaleFactor));

            int xMin = Mathf.Clamp(Mathf.RoundToInt(rect.xMin), 0, width - 1);
            int xMax = Mathf.Clamp(Mathf.RoundToInt(rect.xMax), 0, width - 1);
            int yMin, yMax;
            if (flip)
            {
                // Flip Y coordinates when flip is true
                yMin = Mathf.Clamp(height - 1 - Mathf.RoundToInt(rect.yMax), 0, height - 1);
                yMax = Mathf.Clamp(height - 1 - Mathf.RoundToInt(rect.yMin), 0, height - 1);
            }
            else
            {
                yMin = Mathf.Clamp(Mathf.RoundToInt(rect.yMin), 0, height - 1);
                yMax = Mathf.Clamp(Mathf.RoundToInt(rect.yMax), 0, height - 1);
            }

            Color32 color = new Color32(r, g, b, a);

            // Draw top and bottom edges
            for (int x = xMin; x <= xMax; x++)
            {
                for (int t = 0; t < scaledThickness; t++)
                {
                    int topY = Mathf.Clamp(yMin + t, 0, height - 1);
                    int bottomY = Mathf.Clamp(yMax - t, 0, height - 1);
                    colors[topY * width + x] = color;
                    colors[bottomY * width + x] = color;
                }
            }

            // Draw left and right edges
            for (int y = yMin; y <= yMax; y++)
            {
                for (int t = 0; t < scaledThickness; t++)
                {
                    int leftX = Mathf.Clamp(xMin + t, 0, width - 1);
                    int rightX = Mathf.Clamp(xMax - t, 0, width - 1);
                    colors[y * width + leftX] = color;
                    colors[y * width + rightX] = color;
                }
            }
        }

        /// <summary>
        /// Draws facial landmarks on the image buffer.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines.
        /// </summary>
        /// <param name="colors">Color array of the image.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="landmarks">List of landmark coordinates.</param>
        /// <param name="r">Red component (0-255).</param>
        /// <param name="g">Green component (0-255).</param>
        /// <param name="b">Blue component (0-255).</param>
        /// <param name="a">Alpha component (0-255).</param>
        /// <param name="flip">If true, the coordinates will be flipped vertically.</param>
        private void DrawLandmarksManual(Color32[] colors, int width, int height, List<Vector2> landmarks, byte r, byte g, byte b, byte a, bool flip)
        {
            if (landmarks == null || landmarks.Count == 0)
                return;

            Color32 color = new Color32(r, g, b, a);

            // Scale point radius and line thickness based on image size
            // Base size: 320x240, scale factor based on larger dimension
            const float baseSize = 320f;
            float scaleFactor = Mathf.Max(width, height) / baseSize;
            int pointRadius = Mathf.Max(1, Mathf.RoundToInt(2 * scaleFactor));
            int lineThickness = Mathf.Max(1, Mathf.RoundToInt(1 * scaleFactor));

            // Draw points
            for (int i = 0; i < landmarks.Count; i++)
            {
                int x = Mathf.RoundToInt(landmarks[i].x);
                int y = Mathf.RoundToInt(landmarks[i].y);
                if (flip)
                {
                    y = height - 1 - y;
                }
                DrawCircle(colors, width, height, x, y, pointRadius, color);
            }

            // Draw connecting lines based on point count
            if (landmarks.Count == 5)
            {
                var p0 = landmarks[0];
                var p1 = landmarks[1];
                var p2 = landmarks[2];
                var p3 = landmarks[3];
                var p4 = landmarks[4];

                DrawLine(colors, width, height, p0, p1, color, flip, lineThickness);
                DrawLine(colors, width, height, p1, p4, color, flip, lineThickness);
                DrawLine(colors, width, height, p4, p3, color, flip, lineThickness);
                DrawLine(colors, width, height, p3, p2, color, flip, lineThickness);
            }
            else if (landmarks.Count == 6)
            {
                var p0 = landmarks[0];
                var p1 = landmarks[1];
                var p2 = landmarks[2];
                var p3 = landmarks[3];
                var p4 = landmarks[4];
                var p5 = landmarks[5];

                DrawLine(colors, width, height, p2, p3, color, flip, lineThickness);
                DrawLine(colors, width, height, p4, p5, color, flip, lineThickness);
                DrawLine(colors, width, height, p3, p0, color, flip, lineThickness);
                DrawLine(colors, width, height, p4, p0, color, flip, lineThickness);
                DrawLine(colors, width, height, p0, p1, color, flip, lineThickness);
            }
            else if (landmarks.Count == 17)
            {
                var p0 = landmarks[0];
                var p1 = landmarks[1];
                var p2 = landmarks[2];
                var p3 = landmarks[3];
                var p4 = landmarks[4];
                var p5 = landmarks[5];
                var p9 = landmarks[9];
                var p10 = landmarks[10];
                var p11 = landmarks[11];
                var p12 = landmarks[12];
                var p13 = landmarks[13];
                var p16 = landmarks[16];

                DrawLine(colors, width, height, p2, p9, color, flip, lineThickness);
                DrawLine(colors, width, height, p9, p3, color, flip, lineThickness);
                DrawLine(colors, width, height, p3, p10, color, flip, lineThickness);
                DrawLine(colors, width, height, p10, p2, color, flip, lineThickness);

                DrawLine(colors, width, height, p4, p11, color, flip, lineThickness);
                DrawLine(colors, width, height, p11, p5, color, flip, lineThickness);
                DrawLine(colors, width, height, p5, p12, color, flip, lineThickness);
                DrawLine(colors, width, height, p12, p4, color, flip, lineThickness);

                DrawLine(colors, width, height, p3, p0, color, flip, lineThickness);
                DrawLine(colors, width, height, p4, p0, color, flip, lineThickness);
                DrawLine(colors, width, height, p0, p1, color, flip, lineThickness);

                for (int i = 14; i <= 16; ++i)
                {
                    var current = landmarks[i];
                    var previous = landmarks[i - 1];
                    DrawLine(colors, width, height, current, previous, color, flip, lineThickness);
                }
                DrawLine(colors, width, height, p16, p13, color, flip, lineThickness);

                for (int i = 6; i <= 8; i++)
                {
                    var point = landmarks[i];
                    int x = Mathf.RoundToInt(point.x);
                    int y = Mathf.RoundToInt(point.y);
                    if (flip)
                    {
                        y = height - 1 - y;
                    }
                    DrawCircle(colors, width, height, x, y, pointRadius, color);
                }
            }
            else if (landmarks.Count == 68)
            {
                // Face outline (0-16)
                for (int i = 1; i <= 16; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }

                // Nose bridge (27-30)
                for (int i = 28; i <= 30; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }

                // Left eyebrow (17-21)
                for (int i = 18; i <= 21; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }

                // Right eyebrow (22-26)
                for (int i = 23; i <= 26; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }

                // Nose base (30-35)
                for (int i = 31; i <= 35; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }
                DrawLine(colors, width, height, landmarks[30], landmarks[35], color, flip, lineThickness);

                // Left eye (36-41)
                for (int i = 37; i <= 41; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }
                DrawLine(colors, width, height, landmarks[36], landmarks[41], color, flip, lineThickness);

                // Right eye (42-47)
                for (int i = 43; i <= 47; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }
                DrawLine(colors, width, height, landmarks[42], landmarks[47], color, flip, lineThickness);

                // Mouth outer (48-59)
                for (int i = 49; i <= 59; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }
                DrawLine(colors, width, height, landmarks[48], landmarks[59], color, flip, lineThickness);

                // Mouth inner (60-67)
                for (int i = 61; i <= 67; i++)
                {
                    DrawLine(colors, width, height, landmarks[i - 1], landmarks[i], color, flip, lineThickness);
                }
                DrawLine(colors, width, height, landmarks[60], landmarks[67], color, flip, lineThickness);
            }
            // For other point counts, only points are drawn (already drawn above)
        }

        /// <summary>
        /// Draws a line between two points using Bresenham's algorithm with specified thickness.
        /// </summary>
        /// <param name="colors">Color array of the image.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="start">Start point.</param>
        /// <param name="end">End point.</param>
        /// <param name="color">Line color.</param>
        /// <param name="flip">If true, the coordinates will be flipped vertically.</param>
        /// <param name="thickness">Line thickness in pixels.</param>
        private void DrawLine(Color32[] colors, int width, int height, Vector2 start, Vector2 end, Color32 color, bool flip, int thickness)
        {
            int x0 = Mathf.RoundToInt(start.x);
            int y0 = Mathf.RoundToInt(start.y);
            int x1 = Mathf.RoundToInt(end.x);
            int y1 = Mathf.RoundToInt(end.y);

            if (flip)
            {
                y0 = height - 1 - y0;
                y1 = height - 1 - y1;
            }

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;
            int radius = Mathf.Max(0, (thickness - 1) / 2);

            while (true)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (thickness > 1)
                    {
                        // Draw thick line by drawing circles at each point
                        DrawCircle(colors, width, height, x, y, radius, color);
                    }
                    else
                    {
                        colors[y * width + x] = color;
                    }
                }

                if (x == x1 && y == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        /// <summary>
        /// Draws a filled circle.
        /// </summary>
        private void DrawCircle(Color32[] colors, int width, int height, int centerX, int centerY, int radius, Color32 color)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int x = centerX + dx;
                        int y = centerY + dy;
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            colors[y * width + x] = color;
                        }
                    }
                }
            }
        }
    }
}
