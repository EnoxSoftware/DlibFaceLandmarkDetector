using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// AR Head Example
    /// This example was referring to http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
    /// and use effect asset from http://ktk-kumamoto.hatenablog.com/entry/2014/09/14/092400.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class ARHeadExample : MonoBehaviour
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
        
        /// <summary>
        /// Determines if request the AR camera moving.
        /// </summary>
        public bool shouldMoveARCamera;
        
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
        /// The matrix that inverts the Y axis.
        /// </summary>
        Matrix4x4 invertYM;
        
        /// <summary>
        /// The matrix that inverts the Z axis.
        /// </summary>
        Matrix4x4 invertZM;
        
        /// <summary>
        /// The transformation matrix.
        /// </summary>
        Matrix4x4 transformationM = new Matrix4x4 ();

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;
        
        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints;
        
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
        /// The rot mat.
        /// </summary>
        Mat rotMat;
        
        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        
        /// <summary>
        /// The sp_human_face_68_dat_filepath.
        /// </summary>
        string sp_human_face_68_dat_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif
        
        // Use this for initialization
        void Start ()
        {
            displayFacePointsToggle.isOn = displayFacePoints;
            displayAxesToggle.isOn = displayAxes;
            displayHeadToggle.isOn = displayHead;
            displayEffectsToggle.isOn = displayEffects;

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = DlibFaceLandmarkDetector.Utils.getFilePathAsync ("sp_human_face_68.dat", (result) => {
                coroutines.Clear ();

                sp_human_face_68_dat_filepath = result;
                Run ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            sp_human_face_68_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath ("sp_human_face_68.dat");
            Run ();
            #endif
        }

        private void Run ()
        {
            //set 3d face object points.
            objectPoints = new MatOfPoint3f (
                new Point3 (-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3 (34, 90, 83),//r eye (Interpupillary breadth)
                new Point3 (0.0, 50, 120),//nose (Nose top)
                new Point3 (-26, 15, 83),//l mouse (Mouth breadth)
                new Point3 (26, 15, 83),//r mouse (Mouth breadth)
                new Point3 (-79, 90, 0.0),//l ear (Bitragion breadth)
                new Point3 (79, 90, 0.0)//r ear (Bitragion breadth)
            );
            imagePoints = new MatOfPoint2f ();
            rotMat = new Mat (3, 3, CvType.CV_64FC1);
            
            faceLandmarkDetector = new FaceLandmarkDetector (sp_human_face_68_dat_filepath);

            webCamTextureToMatHelper.Initialize ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
            
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();
            
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);
            
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            
            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
            
            
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
            
            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
            
            
            //set cameraparam
            int max_d = (int)Mathf.Max (width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;
            camMatrix = new Mat (3, 3, CvType.CV_64FC1);
            camMatrix.put (0, 0, fx);
            camMatrix.put (0, 1, 0);
            camMatrix.put (0, 2, cx);
            camMatrix.put (1, 0, 0);
            camMatrix.put (1, 1, fy);
            camMatrix.put (1, 2, cy);
            camMatrix.put (2, 0, 0);
            camMatrix.put (2, 1, 0);
            camMatrix.put (2, 2, 1.0f);
            Debug.Log ("camMatrix " + camMatrix.dump ());
            
            
            distCoeffs = new MatOfDouble (0, 0, 0, 0);
            Debug.Log ("distCoeffs " + distCoeffs.dump ());
            
            
            //calibration camera
            Size imageSize = new Size (width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point (0, 0);
            double[] aspectratio = new double[1];
            
            Calib3d.calibrationMatrixValues (camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);
            
            Debug.Log ("imageSize " + imageSize.ToString ());
            Debug.Log ("apertureWidth " + apertureWidth);
            Debug.Log ("apertureHeight " + apertureHeight);
            Debug.Log ("fovx " + fovx [0]);
            Debug.Log ("fovy " + fovy [0]);
            Debug.Log ("focalLength " + focalLength [0]);
            Debug.Log ("principalPoint " + principalPoint.ToString ());
            Debug.Log ("aspectratio " + aspectratio [0]);
            
            
            //To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan ((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2 ((float)cx, (float)fx) + Mathf.Atan2 ((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan ((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2 ((float)cy, (float)fy) + Mathf.Atan2 ((float)(imageSize.height - cy), (float)fy));
            
            Debug.Log ("fovXScale " + fovXScale);
            Debug.Log ("fovYScale " + fovYScale);
            
            
            //Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale) {
                ARCamera.fieldOfView = (float)(fovx [0] * fovXScale);
            } else {
                ARCamera.fieldOfView = (float)(fovy [0] * fovYScale);
            }
            

            invertYM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, -1, 1));
            Debug.Log ("invertYM " + invertYM.ToString ());

            invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
            Debug.Log ("invertZM " + invertZM.ToString ());
            
            
            axes.SetActive (false);
            head.SetActive (false);
            rightEye.SetActive (false);
            leftEye.SetActive (false);
            mouth.SetActive (false);
            
            mouthParticleSystem = mouth.GetComponentsInChildren<ParticleSystem> (true);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");
            
            camMatrix.Dispose ();
            distCoeffs.Dispose ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }
        
        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
                
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();
                
                
                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
                
                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect ();
                
                if (detectResult.Count > 0) {
                    
                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (detectResult [0]);
                    
                    if (displayFacePoints)
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, points, new Scalar (0, 255, 0, 255), 2);
                    
                    imagePoints.fromArray (
                        new Point ((points [38].x + points [41].x) / 2, (points [38].y + points [41].y) / 2),//l eye (Interpupillary breadth)
                        new Point ((points [43].x + points [46].x) / 2, (points [43].y + points [46].y) / 2),//r eye (Interpupillary breadth)
                        new Point (points [30].x, points [30].y),//nose (Nose top)
                        new Point (points [48].x, points [48].y),//l mouth (Mouth breadth)
                        new Point (points [54].x, points [54].y), //r mouth (Mouth breadth)
                        new Point (points [0].x, points [0].y),//l ear (Bitragion breadth)
                        new Point (points [16].x, points [16].y)//r ear (Bitragion breadth)
                    );
                    
                    // Estimate head pose.
                    if (rvec == null || tvec == null) {
                        rvec = new Mat (3, 1, CvType.CV_64FC1);
                        tvec = new Mat (3, 1, CvType.CV_64FC1);
                        Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                    }
                        
                    double tvec_z = tvec.get (2, 0) [0];

                    if (double.IsNaN (tvec_z) || tvec_z < 0) { // if tvec is wrong data, do not use extrinsic guesses.
                        Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                    } else {
                        Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec, true, Calib3d.SOLVEPNP_ITERATIVE);
                    }

//                    Debug.Log (tvec.dump());
                    
                    if (!double.IsNaN (tvec_z)) {
                        
                        if (Mathf.Abs ((float)(points [43].y - points [46].y)) > Mathf.Abs ((float)(points [42].x - points [45].x)) / 6.0) {
                            if (displayEffects)
                                rightEye.SetActive (true);
                        }
                        
                        if (Mathf.Abs ((float)(points [38].y - points [41].y)) > Mathf.Abs ((float)(points [39].x - points [36].x)) / 6.0) {
                            if (displayEffects)
                                leftEye.SetActive (true);
                        }
                        if (displayHead)
                            head.SetActive (true);
                        if (displayAxes)
                            axes.SetActive (true);
                        
                        
                        float noseDistance = Mathf.Abs ((float)(points [27].y - points [33].y));
                        float mouseDistance = Mathf.Abs ((float)(points [62].y - points [66].y));
                        if (mouseDistance > noseDistance / 5.0) {
                            if (displayEffects) {
                                mouth.SetActive (true);
                                foreach (ParticleSystem ps in mouthParticleSystem) {
                                    var em = ps.emission;
                                    em.enabled = true;
#if UNITY_5_5_OR_NEWER
                                    var main = ps.main;
                                    main.startSizeMultiplier = 40 * (mouseDistance / noseDistance);
#else
                                    ps.startSize = 40 * (mouseDistance / noseDistance);
#endif
                                }
                            }
                        } else {
                            if (displayEffects) {
                                foreach (ParticleSystem ps in mouthParticleSystem) {
                                    var em = ps.emission;
                                    em.enabled = false;
                                }
                            }
                        }

                        Calib3d.Rodrigues (rvec, rotMat);
                        
                        transformationM.SetRow (0, new Vector4 ((float)rotMat.get (0, 0) [0], (float)rotMat.get (0, 1) [0], (float)rotMat.get (0, 2) [0], (float)tvec.get (0, 0) [0]));
                        transformationM.SetRow (1, new Vector4 ((float)rotMat.get (1, 0) [0], (float)rotMat.get (1, 1) [0], (float)rotMat.get (1, 2) [0], (float)tvec.get (1, 0) [0]));
                        transformationM.SetRow (2, new Vector4 ((float)rotMat.get (2, 0) [0], (float)rotMat.get (2, 1) [0], (float)rotMat.get (2, 2) [0], (float)tvec.get (2, 0) [0]));
                        transformationM.SetRow (3, new Vector4 (0, 0, 0, 1));
                        
                        // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                        ARM = invertYM * transformationM;
                        
                        // Apply Z axis inverted matrix.
                        ARM = ARM * invertZM;
                        
                        if (shouldMoveARCamera) {

                            ARM = ARGameObject.transform.localToWorldMatrix * ARM.inverse;
                            
                            ARUtils.SetTransformFromMatrix (ARCamera.transform, ref ARM);
                        } else {

                            ARM = ARCamera.transform.localToWorldMatrix * ARM;
                            
                            ARUtils.SetTransformFromMatrix (ARGameObject.transform, ref ARM);
                        }
                    }
                }
                
                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
                
                OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose ();
            
            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorExample");
            #endif
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        /// <summary>
        /// Raises the display face points toggle value changed event.
        /// </summary>
        public void OnDisplayFacePointsToggleValueChanged ()
        {
            if (displayFacePointsToggle.isOn) {
                displayFacePoints = true;
            } else {
                displayFacePoints = false;
            }
        }

        /// <summary>
        /// Raises the display axes toggle value changed event.
        /// </summary>
        public void OnDisplayAxesToggleValueChanged ()
        {
            if (displayAxesToggle.isOn) {
                displayAxes = true;
            } else {
                displayAxes = false;
                axes.SetActive (false);
            }
        }

        /// <summary>
        /// Raises the display head toggle value changed event.
        /// </summary>
        public void OnDisplayHeadToggleValueChanged ()
        {
            if (displayHeadToggle.isOn) {
                displayHead = true;
            } else {
                displayHead = false;
                head.SetActive (false);
            }
        }

        /// <summary>
        /// Raises the display effects toggle value changed event.
        /// </summary>
        public void OnDisplayEffectsToggleValueChanged ()
        {
            if (displayEffectsToggle.isOn) {
                displayEffects = true;
            } else {
                displayEffects = false;
                rightEye.SetActive (false);
                leftEye.SetActive (false);
                mouth.SetActive (false);
            }
        }
    }
}