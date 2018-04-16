using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace DlibFaceLandmarkDetectorExample
{
    public class DlibFaceLandmarkDetectorExample : MonoBehaviour
    {
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;


        public enum DlibShapePredictorNamePreset : int
        {
            sp_human_face_68,
            sp_human_face_68_for_mobile,
            shape_predictor_5_face_landmarks,
        }

        public Dropdown dlibShapePredictorNameDropdown;

        static DlibShapePredictorNamePreset dlibShapePredictorName = DlibShapePredictorNamePreset.sp_human_face_68;

        /// <summary>
        /// The name of dlib shape predictor file to use in the example scenes.
        /// </summary>
        public static string dlibShapePredictorFileName {
            get{
                return dlibShapePredictorName.ToString() + ".dat";
            }
        }

        // Use this for initialization
        void Start ()
        {
            versionInfo.text = "dlibfacelandmarkdetector" + " " + DlibFaceLandmarkDetector.Utils.getVersion ();
            versionInfo.text += " / UnityEditor " + Application.unityVersion;
            versionInfo.text += " / ";

            #if UNITY_EDITOR
            versionInfo.text += "Editor";
            #elif UNITY_STANDALONE_WIN
            versionInfo.text += "Windows";
            #elif UNITY_STANDALONE_OSX
            versionInfo.text += "Mac OSX";
            #elif UNITY_STANDALONE_LINUX
            versionInfo.text += "Linux";
            #elif UNITY_ANDROID
            versionInfo.text += "Android";
            #elif UNITY_IOS
            versionInfo.text += "iOS";
            #elif UNITY_WSA
            versionInfo.text += "WSA";
            #elif UNITY_WEBGL
            versionInfo.text += "WebGL";
            #endif
            versionInfo.text +=  " ";
            #if ENABLE_MONO
            versionInfo.text +=  "Mono";
            #elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
            #elif ENABLE_DOTNET
            versionInfo.text += ".NET";
            #endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;

            dlibShapePredictorNameDropdown.value = (int)dlibShapePredictorName;
        }

        // Update is called once per frame
        void Update ()
        {

        }

        public void OnScrollRectValueChanged ()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }


        public void OnShowSystemInfoButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowSystemInfo");
            #else
            Application.LoadLevel ("ShowSystemInfo");
            #endif
        }

        public void OnTexture2DExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DExample");
            #else
            Application.LoadLevel ("Texture2DExample");
            #endif
        }

        public void OnWebCamTextureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureExample");
            #else
            Application.LoadLevel ("WebCamTextureExample");
            #endif
        }

        public void OnCatDetectionExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("CatDetectionExample");
            #else
            Application.LoadLevel ("CatDetectionExample");
            #endif
        }
        


        public void OnShowLicenseButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnTexture2DToMatExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DToMatExample");
            #else
            Application.LoadLevel ("Texture2DToMatExample");
            #endif
        }

        public void OnWebCamTextureToMatHelperExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatHelperExample");
            #else
            Application.LoadLevel ("WebCamTextureToMatHelperExample");
            #endif
        }

        public void OnVideoCaptureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureExample");
            #else
            Application.LoadLevel ("VideoCaptureExample");
            #endif
        }

        public void OnARHeadWebCamTextureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ARHeadWebCamTextureExample");
            #else
            Application.LoadLevel ("ARHeadWebCamTextureExample");
            #endif
        }

        public void OnARHeadVideoCaptureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ARHeadVideoCaptureExample");
            #else
            Application.LoadLevel ("ARHeadVideoCaptureExample");
            #endif
        }

        public void OnFrameOptimizationExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("FrameOptimizationExample");
            #else
            Application.LoadLevel ("FrameOptimizationExample");
            #endif
        }

        public void OnNoiseFilterWebCamTextureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("NoiseFilterWebCamTextureExample");
            #else
            Application.LoadLevel ("NoiseFilterWebCamTextureExample");
            #endif
        }

        public void OnNoiseFilterVideoCaptureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("NoiseFilterVideoCaptureExample");
            #else
            Application.LoadLevel ("NoiseFilterVideoCaptureExample");
            #endif
        }




        /// <summary>
        /// Raises the dlib shape predictor name dropdown value changed event.
        /// </summary>
        public void OnDlibShapePredictorNameDropdownValueChanged (int result)
        {
            dlibShapePredictorName = (DlibShapePredictorNamePreset)result;
        }
    }
}