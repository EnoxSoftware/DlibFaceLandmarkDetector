using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Face Landmark Detection from WebCamTexture Example.
    /// </summary>
    public class WebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// The web cam texture.
        /// </summary>
        WebCamTexture webCamTexture;
        
        /// <summary>
        /// The web cam device.
        /// </summary>
        WebCamDevice webCamDevice;
        
        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;
        
        /// <summary>
        /// Should use front facing.
        /// </summary>
        public bool shouldUseFrontFacing = false;
        
        /// <summary>
        /// The width.
        /// </summary>
        int width = 320;
        
        /// <summary>
        /// The height.
        /// </summary>
        int height = 240;

        /// <summary>
        /// The init done.
        /// </summary>
        bool initDone = false;
        
        /// <summary>
        /// The screenOrientation.
        /// </summary>
        ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The texture2 d.
        /// </summary>
        Texture2D texture2D;

        /// <summary>
        /// The flip.
        /// </summary>
        bool flip;

        /// <summary>
        /// The shape_predictor_68_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_face_landmarks_dat_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        private Stack<IEnumerator> coroutineStack = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var filepath_Coroutine = Utils.getFilePathAsync ("shape_predictor_68_face_landmarks.dat", (result) => {
                coroutineStack.Clear ();

                shape_predictor_68_face_landmarks_dat_filepath = result;
                Run ();
            });
            coroutineStack.Push (filepath_Coroutine);
            StartCoroutine (filepath_Coroutine);
            #else
            shape_predictor_68_face_landmarks_dat_filepath = Utils.getFilePath ("shape_predictor_68_face_landmarks.dat");
            Run ();
            #endif
        }

        private void Run ()
        {

            faceLandmarkDetector = new FaceLandmarkDetector (shape_predictor_68_face_landmarks_dat_filepath);
    
            StartCoroutine (init ());
        }

        private IEnumerator init ()
        {
            if (webCamTexture != null) {
                webCamTexture.Stop ();
                initDone = false;
            }
            
            // Checks how many and which cameras are available on the device
            for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                
                if (WebCamTexture.devices [cameraIndex].isFrontFacing == shouldUseFrontFacing) {
                    
                    Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
                    
                    webCamDevice = WebCamTexture.devices [cameraIndex];
                    
                    webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
                    
                    break;
                }
            }
            
            if (webCamTexture == null) {
                //          Debug.Log ("webCamTexture is null");
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
                } else {
                    webCamTexture = new WebCamTexture (width, height);
                }
            }
            
            Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);

            // Starts the camera
            webCamTexture.Play ();

            while (true) {
                //If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
                #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                #else
                if (webCamTexture.didUpdateThisFrame) {
                    #if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
                    while (webCamTexture.width <= 16) {
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    } 
                    #endif
                #endif
                        
                    Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
                    Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);
                        
                    colors = new Color32[webCamTexture.width * webCamTexture.height];
                        
                    texture2D = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                        
                    gameObject.GetComponent<Renderer> ().material.mainTexture = texture2D;
                        
                    updateLayout ();
                        
                    screenOrientation = Screen.orientation;
                    initDone = true;
                        
                    break;
                } else {
                    yield return 0;
                }
            }
        }

        private void updateLayout ()
        {
            gameObject.transform.localRotation = new Quaternion (0, 0, 0, 0);
            gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
                
            if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270) {
                gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
            }
                
            float width = 0;
            float height = 0;
            if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270) {
                width = gameObject.transform.localScale.y;
                height = gameObject.transform.localScale.x;
            } else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180) {
                width = gameObject.transform.localScale.x;
                height = gameObject.transform.localScale.y;
            }
                
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            flip = true;
            if (webCamDevice.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 90) {
                    flip = !flip;
                }
                if (webCamTexture.videoRotationAngle == 180) {
                    flip = !flip;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180) {
                    flip = !flip;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flip = !flip;
                }
            }
            
            if (!flip) {
                gameObject.transform.localScale = new Vector3 (gameObject.transform.localScale.x, -gameObject.transform.localScale.y, 1);
            }
        }
    
        // Update is called once per frame
        void Update ()
        {

            if (!initDone)
                return;
                
            if (screenOrientation != Screen.orientation) {
                screenOrientation = Screen.orientation;
                updateLayout ();
            }

            #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
            if (webCamTexture.width > 16 && webCamTexture.height > 16) {
            #else
            if (webCamTexture.didUpdateThisFrame) {
            #endif

                webCamTexture.GetPixels32 (colors);
                faceLandmarkDetector.SetImage<Color32> (colors, webCamTexture.width, webCamTexture.height, 4, flip);
        
                //detect face rects
                List<Rect> detectResult = faceLandmarkDetector.Detect ();
        
                foreach (var rect in detectResult) {
                //Debug.Log ("face : " + rect);
            
                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);


                    //Debug.Log ("face point : " + points.Count);

                    //draw landmark points
                    faceLandmarkDetector.DrawDetectLandmarkResult<Color32> (colors, webCamTexture.width, webCamTexture.height, 4, flip, 0, 255, 0, 255);


                    //draw face rect
                    faceLandmarkDetector.DrawDetectResult<Color32> (colors, webCamTexture.width, webCamTexture.height, 4, flip, 255, 0, 0, 255, 2);
                }

                texture2D.SetPixels32 (colors);
                texture2D.Apply ();
            }
        }

        void OnDisable ()
        {
            if(webCamTexture != null)
                webCamTexture.Stop ();
            if(faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutineStack) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
        }

        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorExample");
            #endif
        }

        public void OnChangeCameraButton ()
        {
            shouldUseFrontFacing = !shouldUseFrontFacing;
            StartCoroutine (init ());
        }
    }
}