#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using DlibFaceLandmarkDetector;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// AR Head WebCamTexture Example
    /// This example was referring to http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
    /// and use effect asset from http://ktk-kumamoto.hatenablog.com/entry/2014/09/14/092400.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper), typeof(ImageOptimizationHelper))]
    public class ARHeadWebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if displays face points.
        /// </summary>
        public bool displayFacePoints;

        /// <summary>
        /// The display face points toggle.
        /// </summary>
        public Toggle displayFacePointsToggle;

        /// <summary>
        /// Determines if displays display axes
        /// </summary>
        public bool displayAxes;

        /// <summary>
        /// The display axes toggle.
        /// </summary>
        public Toggle displayAxesToggle;

        /// <summary>
        /// Determines if displays head.
        /// </summary>
        public bool displayHead;

        /// <summary>
        /// The display head toggle.
        /// </summary>
        public Toggle displayHeadToggle;

        /// <summary>
        /// Determines if displays effects.
        /// </summary>
        public bool displayEffects;

        /// <summary>
        /// The display effects toggle.
        /// </summary>
        public Toggle displayEffectsToggle;

        [Space(10)]

        /// <summary>
        /// The axes.
        /// </summary>
        public GameObject axes;

        /// <summary>
        /// The head.
        /// </summary>
        public GameObject head;

        /// <summary>
        /// The right eye.
        /// </summary>
        public GameObject rightEye;

        /// <summary>
        /// The left eye.
        /// </summary>
        public GameObject leftEye;

        /// <summary>
        /// The mouth.
        /// </summary>
        public GameObject mouth;

        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera ARCamera;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public GameObject ARGameObject;

        [Space(10)]

        /// <summary>
        /// Determines if request the AR camera moving.
        /// </summary>
        public bool shouldMoveARCamera;

        [Space(10)]

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle enableDownScaleToggle;

        /// <summary>
        /// Determines if enable skipframe.
        /// </summary>
        public bool enableSkipFrame;

        /// <summary>
        /// The enable skipframe toggle.
        /// </summary>
        public Toggle enableSkipFrameToggle;

        /// <summary>
        /// Determines if enable low pass filter.
        /// </summary>
        public bool enableLowPassFilter;

        /// <summary>
        /// The enable low pass filter toggle.
        /// </summary>
        public Toggle enableLowPassFilterToggle;

        /// <summary>
        /// The position low pass. (Value in meters)
        /// </summary>
        public float positionLowPass = 8f;
        //4

        /// <summary>
        /// The rotation low pass. (Value in degrees)
        /// </summary>
        public float rotationLowPass = 4f;
        //2

        /// <summary>
        /// The old pose data.
        /// </summary>
        PoseData oldPoseData;

        /// <summary>
        /// The mouth particle system.
        /// </summary>
        ParticleSystem[] mouthParticleSystem;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distortion coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The matrix that inverts the Y-axis.
        /// </summary>
        Matrix4x4 invertYM;

        /// <summary>
        /// The matrix that inverts the Z-axis.
        /// </summary>
        Matrix4x4 invertZM;

        /// <summary>
        /// The matrix that AR camera P * V.
        /// </summary>
        Matrix4x4 VP;

        /// <summary>
        /// The transformation matrix.
        /// </summary>
        Matrix4x4 transformationM = new Matrix4x4();

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints68;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints17;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints6;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints5;

        /// <summary>
        /// The image points.
        /// </summary>
        MatOfPoint2f imagePoints;

        /// <summary>
        /// The rvec.
        /// </summary>
        Mat rvec;

        /// <summary>
        /// The tvec.
        /// </summary>
        Mat tvec;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The detection result.
        /// </summary>
        List<UnityEngine.Rect> detectionResult;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            displayFacePointsToggle.isOn = displayFacePoints;
            displayAxesToggle.isOn = displayAxes;
            displayHeadToggle.isOn = displayHead;
            displayEffectsToggle.isOn = displayEffects;
            enableDownScaleToggle.isOn = enableDownScale;
            enableSkipFrameToggle.isOn = enableSkipFrame;
            enableLowPassFilterToggle.isOn = enableLowPassFilter;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();


            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
#if UNITY_WEBGL
            getFilePath_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync(dlibShapePredictorFileName, (result) =>
            {
                getFilePath_Coroutine = null;

                dlibShapePredictorFilePath = result;
                Run();
            });
            StartCoroutine(getFilePath_Coroutine);
#else
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
            Run();
#endif
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            //set 3d face object points. (right-handed coordinates system)
            objectPoints68 = new MatOfPoint3f(
                new Point3(-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3(34, 90, 83),//r eye (Interpupillary breadth)
                new Point3(0.0, 50, 117),//nose (Tip)
                new Point3(0.0, 32, 97),//nose (Subnasale)
                new Point3(-79, 90, 10),//l ear (Bitragion breadth)
                new Point3(79, 90, 10)//r ear (Bitragion breadth)
            );

            objectPoints17 = new MatOfPoint3f(
                new Point3(-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3(34, 90, 83),//r eye (Interpupillary breadth)
                new Point3(0.0, 50, 117),//nose (Tip)
                new Point3(0.0, 32, 97),//nose (Subnasale)
                new Point3(-79, 90, 10),//l ear (Bitragion breadth)
                new Point3(79, 90, 10)//r ear (Bitragion breadth)
            );

            objectPoints6 = new MatOfPoint3f(
                new Point3(-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3(34, 90, 83),//r eye (Interpupillary breadth)
                new Point3(0.0, 50, 117),//nose (Tip)
                new Point3(0.0, 32, 97)//nose (Subnasale)
            );

            objectPoints5 = new MatOfPoint3f(
                new Point3(-23, 90, 83),//l eye (Inner corner of the eye)
                new Point3(23, 90, 83),//r eye (Inner corner of the eye)
                new Point3(-50, 90, 80),//l eye (Tail of the eye)
                new Point3(50, 90, 80),//r eye (Tail of the eye)
                new Point3(0.0, 32, 97)//nose (Subnasale)
            );

            imagePoints = new MatOfPoint2f();

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", webCamTextureToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", webCamTextureToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }


            //set cameraparam
            int max_d = (int)Mathf.Max(width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;
            camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);
            Debug.Log("camMatrix " + camMatrix.dump());


            distCoeffs = new MatOfDouble(0, 0, 0, 0);
            Debug.Log("distCoeffs " + distCoeffs.dump());

            // create AR camera P * V Matrix
            Matrix4x4 P = ARUtils.CalculateProjectionMatrixFromCameraMatrixValues((float)fx, (float)fy, (float)cx, (float)cy, width, height, 0.3f, 2000f);
            Matrix4x4 V = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            VP = P * V;

            //calibration camera
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            //To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);


            //Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale)
            {
                ARCamera.fieldOfView = (float)(fovx[0] * fovXScale);
            }
            else
            {
                ARCamera.fieldOfView = (float)(fovy[0] * fovYScale);
            }


            invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
            Debug.Log("invertYM " + invertYM.ToString());

            invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            Debug.Log("invertZM " + invertZM.ToString());


            axes.SetActive(false);
            head.SetActive(false);
            rightEye.SetActive(false);
            leftEye.SetActive(false);
            mouth.SetActive(false);

            mouthParticleSystem = mouth.GetComponentsInChildren<ParticleSystem>(true);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            camMatrix.Dispose();
            distCoeffs.Dispose();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                // detect faces on the downscale image
                if (!enableSkipFrame || !imageOptimizationHelper.IsCurrentFrameSkipped())
                {
                    Mat downScaleRgbaMat = null;
                    float DOWNSCALE_RATIO = 1.0f;
                    if (enableDownScale)
                    {
                        downScaleRgbaMat = imageOptimizationHelper.GetDownScaleMat(rgbaMat);
                        DOWNSCALE_RATIO = imageOptimizationHelper.downscaleRatio;
                    }
                    else
                    {
                        downScaleRgbaMat = rgbaMat;
                        DOWNSCALE_RATIO = 1.0f;
                    }

                    // set the downscale mat
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, downScaleRgbaMat);

                    //detect face rects
                    detectionResult = faceLandmarkDetector.Detect();

                    if (enableDownScale)
                    {
                        for (int i = 0; i < detectionResult.Count; ++i)
                        {
                            var rect = detectionResult[i];
                            detectionResult[i] = new UnityEngine.Rect(
                                rect.x * DOWNSCALE_RATIO,
                                rect.y * DOWNSCALE_RATIO,
                                rect.width * DOWNSCALE_RATIO,
                                rect.height * DOWNSCALE_RATIO);
                        }
                    }
                }

                List<Vector2> points = null;
                if (detectionResult != null && detectionResult.Count > 0)
                {
                    // set the original scale image
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

                    //detect landmark points
                    points = faceLandmarkDetector.DetectLandmark(detectionResult[0]);
                }

                if (points != null)
                {
                    if (displayFacePoints)
                        OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                    MatOfPoint3f objectPoints = null;
                    bool isRightEyeOpen = false;
                    bool isLeftEyeOpen = false;
                    bool isMouthOpen = false;
                    if (points.Count == 68)
                    {
                        objectPoints = objectPoints68;

                        imagePoints.fromArray(
                            new Point((points[38].x + points[41].x) / 2, (points[38].y + points[41].y) / 2),//l eye (Interpupillary breadth)
                            new Point((points[43].x + points[46].x) / 2, (points[43].y + points[46].y) / 2),//r eye (Interpupillary breadth)
                            new Point(points[30].x, points[30].y),//nose (Tip)
                            new Point(points[33].x, points[33].y),//nose (Subnasale)
                            new Point(points[0].x, points[0].y),//l ear (Bitragion breadth)
                            new Point(points[16].x, points[16].y)//r ear (Bitragion breadth)
                        );

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

                        objectPoints = objectPoints17;

                        imagePoints.fromArray(
                            new Point((points[2].x + points[3].x) / 2, (points[2].y + points[3].y) / 2),//l eye (Interpupillary breadth)
                            new Point((points[4].x + points[5].x) / 2, (points[4].y + points[5].y) / 2),//r eye (Interpupillary breadth)
                            new Point(points[0].x, points[0].y),//nose (Tip)
                            new Point(points[1].x, points[1].y),//nose (Subnasale)
                            new Point(points[6].x, points[6].y),//l ear (Bitragion breadth)
                            new Point(points[8].x, points[8].y)//r ear (Bitragion breadth)
                        );

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

                        objectPoints = objectPoints6;

                        imagePoints.fromArray(
                            new Point((points[2].x + points[3].x) / 2, (points[2].y + points[3].y) / 2),//l eye (Interpupillary breadth)
                            new Point((points[4].x + points[5].x) / 2, (points[4].y + points[5].y) / 2),//r eye (Interpupillary breadth)
                            new Point(points[0].x, points[0].y),//nose (Tip)
                            new Point(points[1].x, points[1].y)//nose (Subnasale)
                        );

                    }
                    else if (points.Count == 5)
                    {

                        objectPoints = objectPoints5;

                        imagePoints.fromArray(
                            new Point(points[3].x, points[3].y),//l eye (Inner corner of the eye)
                            new Point(points[1].x, points[1].y),//r eye (Inner corner of the eye)
                            new Point(points[2].x, points[2].y),//l eye (Tail of the eye)
                            new Point(points[0].x, points[0].y),//r eye (Tail of the eye)
                            new Point(points[4].x, points[4].y)//nose (Subnasale)
                        );

                        if (fpsMonitor != null)
                        {
                            fpsMonitor.consoleText = "This example supports mainly the face landmark points of 68/17/6 points.";
                        }
                    }

                    // estimate head pose
                    if (rvec == null || tvec == null)
                    {
                        rvec = new Mat(3, 1, CvType.CV_64FC1);
                        tvec = new Mat(3, 1, CvType.CV_64FC1);
                        Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                    }


                    double tvec_x = tvec.get(0, 0)[0], tvec_y = tvec.get(1, 0)[0], tvec_z = tvec.get(2, 0)[0];

                    bool isNotInViewport = false;
                    Vector4 pos = VP * new Vector4((float)tvec_x, (float)tvec_y, (float)tvec_z, 1.0f);
                    if (pos.w != 0)
                    {
                        float x = pos.x / pos.w, y = pos.y / pos.w, z = pos.z / pos.w;
                        if (x < -1.0f || x > 1.0f || y < -1.0f || y > 1.0f || z < -1.0f || z > 1.0f)
                            isNotInViewport = true;
                    }

                    if (double.IsNaN(tvec_z) || isNotInViewport)
                    { // if tvec is wrong data, do not use extrinsic guesses. (the estimated object is not in the camera field of view)
                        Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                    }
                    else
                    {
                        Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec, true, Calib3d.SOLVEPNP_ITERATIVE);
                    }

                    //Debug.Log (tvec.dump());

                    if (!isNotInViewport)
                    {

                        if (displayHead)
                            head.SetActive(true);
                        if (displayAxes)
                            axes.SetActive(true);

                        if (displayEffects)
                        {
                            rightEye.SetActive(isRightEyeOpen);
                            leftEye.SetActive(isLeftEyeOpen);

                            if (isMouthOpen)
                            {
                                mouth.SetActive(true);
                                foreach (ParticleSystem ps in mouthParticleSystem)
                                {
                                    var em = ps.emission;
                                    em.enabled = true;
#if UNITY_5_5_OR_NEWER
                                    var main = ps.main;
                                    main.startSizeMultiplier = 20;
#else
                                    ps.startSize = 20;
#endif
                                }
                            }
                            else
                            {
                                foreach (ParticleSystem ps in mouthParticleSystem)
                                {
                                    var em = ps.emission;
                                    em.enabled = false;
                                }
                            }
                        }

                        // Convert to unity pose data.
                        double[] rvecArr = new double[3];
                        rvec.get(0, 0, rvecArr);
                        double[] tvecArr = new double[3];
                        tvec.get(0, 0, tvecArr);
                        PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                        // Changes in pos/rot below these thresholds are ignored.
                        if (enableLowPassFilter)
                        {
                            ARUtils.LowpassPoseData(ref oldPoseData, ref poseData, positionLowPass, rotationLowPass);
                        }
                        oldPoseData = poseData;

                        // Create transform matrix.
                        transformationM = Matrix4x4.TRS(poseData.pos, poseData.rot, Vector3.one);
                    }


                    // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                    // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                    ARM = invertYM * transformationM * invertYM;

                    // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
                    ARM = ARM * invertYM * invertZM;

                    if (shouldMoveARCamera)
                    {
                        ARM = ARGameObject.transform.localToWorldMatrix * ARM.inverse;
                        ARUtils.SetTransformFromMatrix(ARCamera.transform, ref ARM);
                    }
                    else
                    {
                        ARM = ARCamera.transform.localToWorldMatrix * ARM;
                        ARUtils.SetTransformFromMatrix(ARGameObject.transform, ref ARM);
                    }
                }
                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                Utils.fastMatToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (imageOptimizationHelper != null)
                imageOptimizationHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
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
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }

        /// <summary>
        /// Raises the display face points toggle value changed event.
        /// </summary>
        public void OnDisplayFacePointsToggleValueChanged()
        {
            if (displayFacePointsToggle.isOn)
            {
                displayFacePoints = true;
            }
            else
            {
                displayFacePoints = false;
            }
        }

        /// <summary>
        /// Raises the display axes toggle value changed event.
        /// </summary>
        public void OnDisplayAxesToggleValueChanged()
        {
            if (displayAxesToggle.isOn)
            {
                displayAxes = true;
            }
            else
            {
                displayAxes = false;
                axes.SetActive(false);
            }
        }

        /// <summary>
        /// Raises the display head toggle value changed event.
        /// </summary>
        public void OnDisplayHeadToggleValueChanged()
        {
            if (displayHeadToggle.isOn)
            {
                displayHead = true;
            }
            else
            {
                displayHead = false;
                head.SetActive(false);
            }
        }

        /// <summary>
        /// Raises the display effects toggle value changed event.
        /// </summary>
        public void OnDisplayEffectsToggleValueChanged()
        {
            if (displayEffectsToggle.isOn)
            {
                displayEffects = true;
            }
            else
            {
                displayEffects = false;
                rightEye.SetActive(false);
                leftEye.SetActive(false);
                mouth.SetActive(false);
            }
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            if (enableDownScaleToggle.isOn)
            {
                enableDownScale = true;
            }
            else
            {
                enableDownScale = false;
            }
        }

        /// <summary>
        /// Raises the enable skipframe toggle value changed event.
        /// </summary>
        public void OnEnableSkipFrameToggleValueChanged()
        {
            if (enableSkipFrameToggle.isOn)
            {
                enableSkipFrame = true;
            }
            else
            {
                enableSkipFrame = false;
            }
        }

        /// <summary>
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged()
        {
            if (enableLowPassFilterToggle.isOn)
            {
                enableLowPassFilter = true;
            }
            else
            {
                enableLowPassFilter = false;
            }
        }
    }
}

#endif