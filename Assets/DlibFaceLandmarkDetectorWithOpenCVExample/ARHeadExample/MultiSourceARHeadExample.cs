using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using DlibFaceLandmarkDetectorWithOpenCVExample.RectangleTrack;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace DlibFaceLandmarkDetectorWithOpenCVExample
{
    /// <summary>
    /// MultiSource AR Head Example
    /// This example was referring to http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
    /// and use effect asset from http://ktk-kumamoto.hatenablog.com/entry/2014/09/14/092400.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper), typeof(ImageOptimizationHelper))]
    public class MultiSourceARHeadExample : MonoBehaviour
    {
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

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        private FaceLandmarkDetector _faceLandmarkDetector;


        /// <summary>
        /// The 3d face object points.
        /// </summary>
        private Vector3[] _objectPoints68;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        private Vector3[] _objectPoints17;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        private Vector3[] _objectPoints6;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        private Vector3[] _objectPoints5;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        private ImageOptimizationHelper _imageOptimizationHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The detection based tracker.
        /// </summary>
        private RectangleTracker _rectangleTracker;

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

            DisplayFacePointsToggle.isOn = DisplayFacePoints;
            DisplayAxesToggle.isOn = DisplayAxes;
            DisplayHeadToggle.isOn = DisplayHead;
            DisplayEffectsToggle.isOn = DisplayEffects;
            EnableDownScaleToggle.isOn = EnableDownScale;
            EnableSkipFrameToggle.isOn = EnableSkipFrame;
            EnableLowPassFilterToggle.isOn = EnableLowPassFilter;

            _imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
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
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();


                // detect faces.
                List<Rect> detectResult = new List<Rect>();

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

                    // detect face rects
                    List<UnityEngine.Rect> result = _faceLandmarkDetector.Detect();

                    if (EnableDownScale)
                    {
                        for (int i = 0; i < result.Count; ++i)
                        {
                            var rect = result[i];
                            result[i] = new UnityEngine.Rect(
                                rect.x * DOWNSCALE_RATIO,
                                rect.y * DOWNSCALE_RATIO,
                                rect.width * DOWNSCALE_RATIO,
                                rect.height * DOWNSCALE_RATIO);
                        }
                    }


                    foreach (var unityRect in result)
                    {
                        detectResult.Add(new Rect((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                    }
                }


                // face tracking.
                _rectangleTracker.UpdateTrackedObjects(detectResult);
                List<TrackedRect> trackedRects = new List<TrackedRect>();
                _rectangleTracker.GetObjects(trackedRects, true);

                // Reset when trackedRects.Count is 0
                if (trackedRects.Count == 0)
                {
                    // Debug.Log("trackedRects count is 0.");
                    _rectangleTracker.Reset();
                }

                // Log tracked rectangles information
                //LogTrackedRects(trackedRects);

                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();

                // detect face landmark points.
                DlibOpenCVUtils.SetImage(_faceLandmarkDetector, rgbaMat);

                foreach (var tr in trackedRects)
                {
                    if (tr.State >= TrackedState.NEW_HIDED)
                    {
                        continue;
                    }

                    UnityEngine.Rect rect = new UnityEngine.Rect(tr.x, tr.y, tr.width, tr.height);

                    List<Vector2> points = _faceLandmarkDetector.DetectLandmark(rect);

                    if (points != null)
                    {
                        //Debug.Log("detect");

                        if (DisplayFacePoints)
                            DlibOpenCVUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

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


                        ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, "ARFace_" + tr.Id, ArHelper.transform);
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

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
            _imageOptimizationHelper?.Dispose();
            _rectangleTracker?.Dispose();
            _faceLandmarkDetector?.Dispose();
            _cts?.Dispose();
        }

        // Public Methods
        private void Run()
        {
            if (string.IsNullOrEmpty(_dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from \"DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/\" to \"Assets/StreamingAssets/DlibFaceLandmarkDetector/\" folder. ");
            }

            //set 3d face object points. (right-handed coordinates system)
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
            _rectangleTracker = new RectangleTracker();
            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat webCamTextureMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(webCamTextureMat, _texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            // Set the camera's orthographicSize to half of the texture height
            Camera.main.orthographicSize = _texture.height / 2f;

            // Get the camera's aspect ratio
            float cameraAspect = Camera.main.aspect;

            // Get the texture's aspect ratio
            float textureAspect = (float)_texture.width / _texture.height;

            // Calculate imageSizeScale
            float imageSizeScale;
            if (textureAspect > cameraAspect)
            {
                // Calculate the camera width (height is already fixed)
                float cameraWidth = Camera.main.orthographicSize * 2f * cameraAspect;

                // Scale so that the texture width fits within the camera width
                imageSizeScale = cameraWidth / _texture.width;
            }
            else
            {
                // Scale so that the texture height fits within the camera height
                imageSizeScale = 1f; // No scaling needed since height is already fixed
            }
            Debug.Log("imageSizeScale " + imageSizeScale);

            transform.localScale = new Vector3(_texture.width * imageSizeScale, _texture.height * imageSizeScale, 1);


            // Initialize ARHelper.
            ArHelper.Initialize();
            // Set ARCamera parameters.
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, webCamTextureMat.width(), webCamTextureMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelper.ARCamera.SetCamMatrixValuesFromImageSize();


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("dlib shape predictor", _dlibShapePredictorFileName);
                _fpsMonitor.Add("width", _multiSource2MatHelper.GetWidth().ToString());
                _fpsMonitor.Add("height", _multiSource2MatHelper.GetHeight().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _rectangleTracker.Reset();

            ArHelper?.Dispose();
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
        /// Called when an ARGameObject enters the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnEnterARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnEnterARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            if (arGameObject.TryGetComponent<ARFace>(out ARFace arFace))
            {
                arFace.DisplayHead = DisplayHead;
                arFace.DisplayAxes = DisplayAxes;
                arFace.DisplayEffects = DisplayEffects;
            }
            arGameObject.UseLowPassFilter = EnableLowPassFilter;

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
            Debug.Log("OnExitARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            arGameObject.gameObject.SetActive(false);
            //StartCoroutine(arGameObject.GetComponent<ARFace>().ExitAnimation(arGameObject.gameObject, 1f, 0f, 0.2f));

        }

        // Private Methods
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
        /// Logs tracked rectangles information to Debug.Log.
        /// </summary>
        /// <param name="trackedRects">List of tracked rectangles to log</param>
        private void LogTrackedRects(List<TrackedRect> trackedRects)
        {
            if (trackedRects == null || trackedRects.Count == 0)
            {
                Debug.Log("trackedRects count: 0");
                return;
            }

            var logMessage = new System.Text.StringBuilder();
            logMessage.AppendLine($"trackedRects count: {trackedRects.Count}");

            foreach (var trackedRect in trackedRects)
            {
                logMessage.AppendLine($"trackedRect Id: {trackedRect.Id} State: {trackedRect.State} x: {trackedRect.x} y: {trackedRect.y} width: {trackedRect.width} height: {trackedRect.height}");
            }

            Debug.Log(logMessage.ToString());
        }
    }
}
