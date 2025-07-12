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
    public class WebCamTextureExample : MonoBehaviour
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

        // Private Fields
        private WebCamTexture _webCamTexture;
        private WebCamDevice _webCamDevice;
        private Color32[] _colors;
        private Color32[] _rotatedColors;
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
            if (AdjustPixelsDirection)
            {
                if (_screenOrientation != Screen.orientation && (_screenWidth != Screen.width || _screenHeight != Screen.height))
                {
                    Initialize();
                }
                else
                {
                    _screenWidth = Screen.width;
                    _screenHeight = Screen.height;
                }
            }
            if (_hasInitDone && _webCamTexture.isPlaying && _webCamTexture.didUpdateThisFrame)
            {
                Color32[] colors = GetColors();
                if (colors != null)
                {
                    _faceLandmarkDetector.SetImage<Color32>(colors, _texture.width, _texture.height, 4, true);
                    List<Rect> detectResult = _faceLandmarkDetector.Detect();
                    foreach (var rect in detectResult)
                    {
                        _faceLandmarkDetector.DetectLandmark(rect);
                        _faceLandmarkDetector.DrawDetectLandmarkResult<Color32>(colors, _texture.width, _texture.height, 4, true, 0, 255, 0, 255);
                    }
                    _faceLandmarkDetector.DrawDetectResult<Color32>(colors, _texture.width, _texture.height, 4, true, 255, 0, 0, 255, 2);
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
            if (AdjustPixelsDirectionToggle.isOn != AdjustPixelsDirection)
            {
                AdjustPixelsDirection = AdjustPixelsDirectionToggle.isOn;
                Initialize();
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
            Initialize();
            if (_faceLandmarkDetector.GetShapePredictorNumParts() != 68)
                Debug.LogWarning("The DrawDetectLandmarkResult method does not support ShapePredictorNumParts sizes other than 68 points, so the drawing will be incorrect."
                    + " If you want to draw the result correctly, we recommend using the OpenCVForUnityUtils.DrawFaceLandmark method.");
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
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", _texture.width.ToString());
                _fpsMonitor.Add("height", _texture.height.ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
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
    }
}
