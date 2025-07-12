using DlibFaceLandmarkDetector.UnityIntegration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    public class DlibFaceLandmarkDetectorExample : MonoBehaviour
    {
        // Enums
        public enum DlibShapePredictorNamePreset : int
        {
            sp_human_face_68,
            sp_human_face_68_for_mobile,
            sp_human_face_17,
            sp_human_face_17_for_mobile,
            sp_human_face_6,
        }

        // Constants
        private static float VERTICAL_NORMALIZED_POSITION = 1f;
        private static DlibShapePredictorNamePreset _dlibShapePredictorName = DlibShapePredictorNamePreset.sp_human_face_68;

        // Public Fields
        public Text VersionInfo;
        public ScrollRect ScrollRect;
        public Dropdown DlibShapePredictorNameDropdown;

        // Unity Lifecycle Methods
        private void Start()
        {
            VersionInfo.text = "dlibfacelandmarkdetector" + " " + DlibEnv.GetVersion();
            VersionInfo.text += " / UnityEditor " + Application.unityVersion;
            VersionInfo.text += " / ";

#if UNITY_EDITOR
            VersionInfo.text += "Editor";
#elif UNITY_STANDALONE_WIN
            VersionInfo.text += "Windows";
#elif UNITY_STANDALONE_OSX
            VersionInfo.text += "Mac OSX";
#elif UNITY_STANDALONE_LINUX
            VersionInfo.text += "Linux";
#elif UNITY_ANDROID
            VersionInfo.text += "Android";
#elif UNITY_IOS
            VersionInfo.text += "iOS";
#elif UNITY_VISIONOS
            VersionInfo.text += "VisionOS";
#elif UNITY_WSA
            VersionInfo.text += "WSA";
#elif UNITY_WEBGL
            VersionInfo.text += "WebGL";
#endif
            VersionInfo.text += " ";
#if ENABLE_MONO
            VersionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            VersionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            VersionInfo.text += ".NET";
#endif

            ScrollRect.verticalNormalizedPosition = VERTICAL_NORMALIZED_POSITION;

            DlibShapePredictorNameDropdown.value = (int)_dlibShapePredictorName;
        }

        private void Update()
        {

        }

        // Public Methods
        public void OnScrollRectValueChanged()
        {
            VERTICAL_NORMALIZED_POSITION = ScrollRect.verticalNormalizedPosition;
        }

        public void OnShowSystemInfoButtonClick()
        {
            SceneManager.LoadScene("ShowSystemInfo");
        }

        public void OnTexture2DExampleButtonClick()
        {
            SceneManager.LoadScene("Texture2DExample");
        }

        public void OnWebCamTextureExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTextureExample");
        }

        public void OnBenchmarkExampleButtonClick()
        {
            SceneManager.LoadScene("BenchmarkExample");
        }

        public void OnShowLicenseButtonClick()
        {
            SceneManager.LoadScene("ShowLicense");
        }

        public void OnTexture2DToMatExampleButtonClick()
        {
            SceneManager.LoadScene("Texture2DToMatExample");
        }

        public void OnWebCamTexture2MatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTexture2MatHelperExample");
        }

        public void OnVideoCapture2MatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("VideoCapture2MatHelperExample");
        }

        public void OnWebCamTextureARHeadExampleButtonClick()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                SceneManager.LoadScene("WebCamTextureARHeadExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("WebCamTextureARHeadExample_SRP");
            }
        }

        public void OnVideoCaptureARHeadExampleButtonClick()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                SceneManager.LoadScene("VideoCaptureARHeadExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("VideoCaptureARHeadExample_SRP");
            }
        }

        public void OnFrameOptimizationExampleButtonClick()
        {
            SceneManager.LoadScene("FrameOptimizationExample");
        }

        public void OnWebCamTextureNoiseFilterExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTextureNoiseFilterExample");
        }

        public void OnVideoCaptureNoiseFilterExampleButtonClick()
        {
            SceneManager.LoadScene("VideoCaptureNoiseFilterExample");
        }

        /// <summary>
        /// Raises the dlib shape predictor name dropdown value changed event.
        /// </summary>
        public void OnDlibShapePredictorNameDropdownValueChanged(int result)
        {
            _dlibShapePredictorName = (DlibShapePredictorNamePreset)result;
        }

        /// <summary>
        /// The name of dlib shape predictor file to use in the example scenes.
        /// </summary>
        public static string DlibShapePredictorFileName
        {
            get
            {
                return "DlibFaceLandmarkDetector/" + _dlibShapePredictorName.ToString() + ".dat";
            }
        }
    }
}
