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
    /// WebCamTexture Example
    /// An example of detecting face landmarks in WebCamTexture images.
    /// </summary>
    public class WebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// Set the name of the device to use.
        /// </summary>
        [SerializeField, TooltipAttribute ("Set the name of the device to use.")]
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the width of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute ("Set the width of WebCamTexture.")]
        public int requestedWidth = 320;

        /// <summary>
        /// Set the height of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute ("Set the height of WebCamTexture.")]
        public int requestedHeight = 240;

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, TooltipAttribute ("Set whether to use the front facing camera.")]
        public bool requestedIsFrontFacing = false;

        /// <summary>
        /// Set FPS of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute ("Set FPS of WebCamTexture.")]
        public int requestedFPS = 30;

        /// <summary>
        /// Sets whether to rotate WebCamTexture 90 degrees.
        /// </summary>
        [SerializeField, TooltipAttribute ("Sets whether to rotate WebCamTexture 90 degrees.")]
        public bool requestedRotate90Degree = false;

        /// <summary>
        /// Determines if flips vertically.
        /// </summary>
        [SerializeField, TooltipAttribute ("Determines if flips vertically.")]
        public bool flipVertical = false;

        /// <summary>
        /// Determines if flips horizontal.
        /// </summary>
        [SerializeField, TooltipAttribute ("Determines if flips horizontal.")]
        public bool flipHorizontal = false;

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
        /// Determines if rotates 90 degree.
        /// </summary>
        bool rotate90Degree = false;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        protected bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        bool hasInitDone = false;
        
        /// <summary>
        /// The screenOrientation.
        /// </summary>
        ScreenOrientation screenOrientation;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The texture2D.
        /// </summary>
        Texture2D texture2D;

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
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = Utils.getFilePathAsync ("sp_human_face_68.dat", (result) => {
                coroutines.Clear ();

                sp_human_face_68_dat_filepath = result;
                Run ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            sp_human_face_68_dat_filepath = Utils.getFilePath ("sp_human_face_68.dat");
            Run ();
            #endif
        }

        private void Run ()
        {
            faceLandmarkDetector = new FaceLandmarkDetector (sp_human_face_68_dat_filepath);
    
            StartCoroutine (Initialize ());
        }

        private IEnumerator Initialize ()
        {
            if (isInitWaiting)
                yield break;

            if (webCamTexture != null) {
                webCamTexture.Stop ();
                texture2D = null;
                rotate90Degree = false;
                isInitWaiting = false;
                hasInitDone = false;
            }

            isInitWaiting = true;

            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                webCamTexture = new WebCamTexture (requestedDeviceName, requestedWidth, requestedHeight, requestedFPS);
            } else {
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestedIsFrontFacing) {

                        webCamDevice = WebCamTexture.devices [cameraIndex];
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);

                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                } else {
                    isInitWaiting = false;

                    yield break;
                }
            }
            

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
                     
                    #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
                    if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
                        rotate90Degree = true;
                    }else{
                        rotate90Degree = false;
                    }

                    #endif

                    if (rotate90Degree || requestedRotate90Degree) {
                        rotate90Degree = true;
                        texture2D = new Texture2D (webCamTexture.height, webCamTexture.width, TextureFormat.RGBA32, false);
                    } else {
                        texture2D = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                    }
                        
                        
                    gameObject.GetComponent<Renderer> ().material.mainTexture = texture2D;


                    gameObject.transform.localScale = new Vector3 (texture2D.width, texture2D.height, 1);
                    Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

                        
                    float width = texture2D.width;
                    float height = texture2D.height;

                    float widthScale = (float)Screen.width / width;
                    float heightScale = (float)Screen.height / height;
                    if (widthScale < heightScale) {
                        Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                    } else {
                        Camera.main.orthographicSize = height / 2;
                    }
                        
                    screenOrientation = Screen.orientation;
                    isInitWaiting = false;
                    hasInitDone = true;
                        
                    break;
                } else {
                    yield return 0;
                }
            }
        }
            
    
        // Update is called once per frame
        void Update ()
        {
            if (!hasInitDone)
                return;
                
            if (screenOrientation != Screen.orientation) {
                StartCoroutine (Initialize ());
            }

           
            if (webCamTexture.didUpdateThisFrame) {

                webCamTexture.GetPixels32 (colors);

                //Adjust an array of color pixels according to screen orientation and WebCamDevice parameter.
                colors = RotateAndFlip (colors, webCamTexture.width, webCamTexture.height);


                faceLandmarkDetector.SetImage<Color32> (colors, texture2D.width, texture2D.height, 4, true);
        
                //detect face rects
                List<Rect> detectResult = faceLandmarkDetector.Detect ();
        
                foreach (var rect in detectResult) {
                    //Debug.Log ("face : " + rect);
            
                    //detect landmark points
                    faceLandmarkDetector.DetectLandmark (rect);

                    //draw landmark points
                    faceLandmarkDetector.DrawDetectLandmarkResult<Color32> (colors, texture2D.width, texture2D.height, 4, true, 0, 255, 0, 255);
                }

                //draw face rect
                faceLandmarkDetector.DrawDetectResult<Color32> (colors, texture2D.width, texture2D.height, 4, true, 255, 0, 0, 255, 2);


                    
                texture2D.SetPixels32 (colors);
                texture2D.Apply ();
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (webCamTexture != null)
                webCamTexture.Stop ();
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
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            requestedIsFrontFacing = !requestedIsFrontFacing;
            StartCoroutine (Initialize ());
        }

        /// <summary>
        /// Rotates and flip.
        /// </summary>
        /// <returns>Output Colors.</returns>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        Color32[] RotateAndFlip (Color32[] colors, int width, int height)
        {
            if (rotate90Degree) {
                colors = RotateCW (colors, width, height);
            }

            int flipCode = int.MinValue;

            if (webCamDevice.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0) {
                    flipCode = 1;
                } else if (webCamTexture.videoRotationAngle == 90) {
                    flipCode = 1;
                }
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = 0;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = 0;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = -1;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = -1;
                }
            }

            if (flipVertical) {
                if (flipCode == int.MinValue) {
                    flipCode = 0;
                } else if (flipCode == 0) {
                    flipCode = int.MinValue;
                } else if (flipCode == 1) {
                    flipCode = -1;
                } else if (flipCode == -1) {
                    flipCode = 1;
                }
            }

            if (flipHorizontal) {
                if (flipCode == int.MinValue) {
                    flipCode = 1;
                } else if (flipCode == 0) {
                    flipCode = -1;
                } else if (flipCode == 1) {
                    flipCode = int.MinValue;
                } else if (flipCode == -1) {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue) {
                if (rotate90Degree) {
                    if (flipCode == 0) {
                        colors = FlipVertical (colors, height, width);
                    } else if (flipCode == 1) {
                        colors = FlipHorizontal (colors, height, width);
                    } else if (flipCode < 0) {
                        colors = FlipVertical (colors, height, width);
                        colors = FlipHorizontal (colors, height, width);
                    }
                } else {
                    if (flipCode == 0) {
                        colors = FlipVertical (colors, width, height);
                    } else if (flipCode == 1) {
                        colors = FlipHorizontal (colors, width, height);
                    } else if (flipCode < 0) {
                        colors = FlipVertical (colors, width, height);
                        colors = FlipHorizontal (colors, width, height);
                    }
                }
            }

            return colors;
        }

        /// <summary>
        /// Flips vertical.
        /// </summary>
        /// <returns>Output Colors.</returns>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        Color32[] FlipVertical (Color32[] colors, int width, int height)
        {
            Color32[] result = new Color32[colors.Length];
            int i = 0;
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    result [x + ((height - 1) - y) * width] = colors [x + y * width];
                    i++;
                }
            }

            return result;
        }

        /// <summary>
        /// Flips horizontal.
        /// </summary>
        /// <returns>Output Colors.</returns>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        Color32[] FlipHorizontal (Color32[] colors, int width, int height)
        {
            Color32[] result = new Color32[colors.Length];
            int i = 0;
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    result [((width - 1) - x) + y * width] = colors [x + y * width];
                    i++;
                }
            }

            return result;
        }

        /// <summary>
        /// Rotates 90 CLOCKWISE.
        /// </summary>
        /// <returns>Output Colors.</returns>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        Color32[] RotateCW (Color32[] colors, int height, int width)
        {
            Color32[] result = new Color32[colors.Length];
            int i = 0;
            for (int x = height - 1; x >= 0; x--) {
                for (int y = 0; y < width; y++) {
                    result [i] = colors [x + y * height];
                    i++;
                }
            }
            return result;
        }

        /// <summary>
        /// Rotates 90 COUNTERCLOCKWISE.
        /// </summary>
        /// <returns>Output Colors.</returns>
        /// <param name="colors">Colors.</param>
        /// <param name="height">Height.</param>
        /// <param name="width">Width.</param>
        Color32[] RotateCCW (Color32[] colors, int width, int height)
        {
            Color32[] result = new Color32[colors.Length];
            int i = 0;
            for (int x = 0; x < width; x++) {
                for (int y = height - 1; y >= 0; y--) {
                    result [i] = colors [x + y * width];
                    i++;
                }
            }
            return result;
        }
    }
}