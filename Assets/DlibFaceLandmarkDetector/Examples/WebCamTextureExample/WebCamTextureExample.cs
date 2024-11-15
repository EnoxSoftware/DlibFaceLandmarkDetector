using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// Set the name of the device to use.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the name of the device to use.")]
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the width of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
        public int requestedWidth = 320;

        /// <summary>
        /// Set the height of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
        public int requestedHeight = 240;

        /// <summary>
        /// Set FPS of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
        public int requestedFPS = 30;

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, TooltipAttribute("Set whether to use the front facing camera.")]
        public bool requestedIsFrontFacing = false;

        /// <summary>
        /// The adjust pixels direction toggle.
        /// </summary>
        public Toggle adjustPixelsDirectionToggle;

        /// <summary>
        /// Determines if adjust pixels direction.
        /// </summary>
        [SerializeField, TooltipAttribute("Determines if adjust pixels direction.")]
        public bool adjustPixelsDirection = false;

        /// <summary>
        /// The webcam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The webcam device.
        /// </summary>
        WebCamDevice webCamDevice;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The rotated colors.
        /// </summary>
        Color32[] rotatedColors;

        /// <summary>
        /// Determines if rotates 90 degree.
        /// </summary>
        bool rotate90Degree = false;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        bool hasInitDone = false;

        /// <summary>
        /// The screenOrientation.
        /// </summary>
        ScreenOrientation screenOrientation;

        /// <summary>
        /// The width of the screen.
        /// </summary>
        int screenWidth;

        /// <summary>
        /// The height of the screen.
        /// </summary>
        int screenHeight;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

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

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            dlibShapePredictorFilePath = await Utils.getFilePathAsyncTask(dlibShapePredictorFileName, cancellationToken: cts.Token);

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

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            Initialize();

            if (faceLandmarkDetector.GetShapePredictorNumParts() != 68)
                Debug.LogWarning("The DrawDetectLandmarkResult method does not support ShapePredictorNumParts sizes other than 68 points, so the drawing will be incorrect."
                    + " If you want to draw the result correctly, we recommend using the OpenCVForUnityUtils.DrawFaceLandmark method.");
        }

        /// <summary>
        /// Initializes webcam texture.
        /// </summary>
        private void Initialize()
        {
            if (isInitWaiting)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            if (requestedIsFrontFacing)
            {
                int rearCameraFPS = requestedFPS;
                requestedFPS = 15;
                StartCoroutine(_Initialize());
                requestedFPS = rearCameraFPS;
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
            if (hasInitDone)
                Dispose();

            isInitWaiting = true;

            // Checks camera permission state.
#if (UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER
            UserAuthorization mode = UserAuthorization.WebCam;
            if (!Application.HasUserAuthorization(mode))
            {
                isUserRequestingPermission = true;
                yield return Application.RequestUserAuthorization(mode);

                float timeElapsed = 0;
                while (isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;

                    yield return null;
                }
            }

            if (!Application.HasUserAuthorization(mode))
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.consoleText = "Camera permission is denied.";
                }
                isInitWaiting = false;
                yield break;
            }
#elif UNITY_ANDROID && UNITY_2018_3_OR_NEWER
            string permission = UnityEngine.Android.Permission.Camera;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                isUserRequestingPermission = true;
                UnityEngine.Android.Permission.RequestUserPermission(permission);

                float timeElapsed = 0;
                while (isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;

                    yield return null;
                }
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.consoleText = "Camera permission is denied.";
                }
                isInitWaiting = false;
                yield break;
            }
#endif

            // Creates a WebCamTexture with settings closest to the requested name, resolution, and frame rate.
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("Camera device does not exist.");
                isInitWaiting = false;
                yield break;
            }

            if (!String.IsNullOrEmpty(requestedDeviceName))
            {
                // Try to parse requestedDeviceName as an index
                int requestedDeviceIndex = -1;
                if (Int32.TryParse(requestedDeviceName, out requestedDeviceIndex))
                {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                    {
                        webCamDevice = devices[requestedDeviceIndex];
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                    }
                }
                else
                {
                    // Search for a device with a matching name
                    for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                    {
                        if (devices[cameraIndex].name == requestedDeviceName)
                        {
                            webCamDevice = devices[cameraIndex];
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                            break;
                        }
                    }
                }
                if (webCamTexture == null)
                    Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (webCamTexture == null)
            {
                var prioritizedKinds = new WebCamKind[]
                {
                    WebCamKind.WideAngle,
                    WebCamKind.Telephoto,
                    WebCamKind.UltraWideAngle,
                    WebCamKind.ColorAndDepth
                };

                // Checks how many and which cameras are available on the device
                foreach (var kind in prioritizedKinds)
                {
                    foreach (var device in devices)
                    {
                        if (device.kind == kind && device.isFrontFacing == requestedIsFrontFacing)
                        {
                            webCamDevice = device;
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                            break;
                        }
                    }
                    if (webCamTexture != null) break;
                }
            }

            if (webCamTexture == null)
            {
                webCamDevice = devices[0];
                webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
            }

            // Starts the camera
            webCamTexture.Play();

            while (true)
            {
                if (webCamTexture.didUpdateThisFrame)
                {
                    Debug.Log("name:" + webCamTexture.deviceName + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS);
                    Debug.Log("videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
                    isInitWaiting = false;
                    hasInitDone = true;

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
        bool isUserRequestingPermission;

        IEnumerator OnApplicationFocus(bool hasFocus)
        {
            yield return null;

            if (isUserRequestingPermission && hasFocus)
                isUserRequestingPermission = false;
        }
#endif

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose()
        {
            rotate90Degree = false;
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                WebCamTexture.Destroy(webCamTexture);
                webCamTexture = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture initialized event.
        /// </summary>
        private void OnInited()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
            {
                colors = new Color32[webCamTexture.width * webCamTexture.height];
                rotatedColors = new Color32[webCamTexture.width * webCamTexture.height];
            }

            if (adjustPixelsDirection)
            {
#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                {
                    rotate90Degree = true;
                }
                else
                {
                    rotate90Degree = false;
                }
#endif
            }
            if (rotate90Degree)
            {
                texture = new Texture2D(webCamTexture.height, webCamTexture.width, TextureFormat.RGBA32, false);
            }
            else
            {
                texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            }

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", texture.width.ToString());
                fpsMonitor.Add("height", texture.height.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (adjustPixelsDirection)
            {
                // Catch the orientation change of the screen.
                if (screenOrientation != Screen.orientation && (screenWidth != Screen.width || screenHeight != Screen.height))
                {
                    Initialize();
                }
                else
                {
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
                }
            }


            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
            {

                Color32[] colors = GetColors();

                if (colors != null)
                {

                    faceLandmarkDetector.SetImage<Color32>(colors, texture.width, texture.height, 4, true);

                    //detect face rects
                    List<Rect> detectResult = faceLandmarkDetector.Detect();

                    foreach (var rect in detectResult)
                    {
                        //Debug.Log ("face : " + rect);

                        //detect landmark points
                        faceLandmarkDetector.DetectLandmark(rect);

                        //draw landmark points
                        faceLandmarkDetector.DrawDetectLandmarkResult<Color32>(colors, texture.width, texture.height, 4, true, 0, 255, 0, 255);
                    }

                    //draw face rect
                    faceLandmarkDetector.DrawDetectResult<Color32>(colors, texture.width, texture.height, 4, true, 255, 0, 0, 255, 2);

                    texture.SetPixels32(colors);
                    texture.Apply(false);
                }
            }
        }

        /// <summary>
        /// Gets the current WebCameraTexture frame that converted to the correct direction.
        /// </summary>
        private Color32[] GetColors()
        {
            webCamTexture.GetPixels32(colors);

            if (adjustPixelsDirection)
            {
                //Adjust an array of color pixels according to screen orientation and WebCamDevice parameter.
                if (rotate90Degree)
                {
                    Rotate90CW(colors, rotatedColors, webCamTexture.width, webCamTexture.height);
                    FlipColors(rotatedColors, webCamTexture.width, webCamTexture.height);
                    return rotatedColors;
                }
                else
                {
                    FlipColors(colors, webCamTexture.width, webCamTexture.height);
                    return colors;
                }
            }
            return colors;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

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

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            if (hasInitDone)
            {
                requestedDeviceName = null;
                requestedIsFrontFacing = !requestedIsFrontFacing;
                Initialize();
            }
        }

        /// <summary>
        /// Raises the adjust pixels direction toggle value changed event.
        /// </summary>
        public void OnAdjustPixelsDirectionToggleValueChanged()
        {
            if (adjustPixelsDirectionToggle.isOn != adjustPixelsDirection)
            {
                adjustPixelsDirection = adjustPixelsDirectionToggle.isOn;
                Initialize();
            }
        }

        /// <summary>
        /// Flips the colors.
        /// </summary>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        void FlipColors(Color32[] colors, int width, int height)
        {
            int flipCode = int.MinValue;

            if (webCamDevice.isFrontFacing)
            {
                if (webCamTexture.videoRotationAngle == 0)
                {
                    flipCode = 1;
                }
                else if (webCamTexture.videoRotationAngle == 90)
                {
                    flipCode = 1;
                }
                if (webCamTexture.videoRotationAngle == 180)
                {
                    flipCode = 0;
                }
                else if (webCamTexture.videoRotationAngle == 270)
                {
                    flipCode = 0;
                }
            }
            else
            {
                if (webCamTexture.videoRotationAngle == 180)
                {
                    flipCode = -1;
                }
                else if (webCamTexture.videoRotationAngle == 270)
                {
                    flipCode = -1;
                }
            }

            if (flipCode > int.MinValue)
            {
                if (rotate90Degree)
                {
                    if (flipCode == 0)
                    {
                        FlipVertical(colors, colors, height, width);
                    }
                    else if (flipCode == 1)
                    {
                        FlipHorizontal(colors, colors, height, width);
                    }
                    else if (flipCode < 0)
                    {
                        Rotate180(colors, colors, height, width);
                    }
                }
                else
                {
                    if (flipCode == 0)
                    {
                        FlipVertical(colors, colors, width, height);
                    }
                    else if (flipCode == 1)
                    {
                        FlipHorizontal(colors, colors, width, height);
                    }
                    else if (flipCode < 0)
                    {
                        Rotate180(colors, colors, height, width);
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
        void FlipVertical(Color32[] src, Color32[] dst, int width, int height)
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
        void FlipHorizontal(Color32[] src, Color32[] dst, int width, int height)
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
        void Rotate180(Color32[] src, Color32[] dst, int height, int width)
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
        void Rotate90CW(Color32[] src, Color32[] dst, int height, int width)
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
        void Rotate90CCW(Color32[] src, Color32[] dst, int width, int height)
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