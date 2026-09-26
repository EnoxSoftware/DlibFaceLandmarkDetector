using DlibFaceLandmarkDetector.UnityIntegration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// DlibFaceLandmarkDetector Example
    /// The main menu scene that lists all sample scenes and displays DlibFaceLandmarkDetector and Unity version information.
    /// Lets the user select the dlib shape predictor file name via <see cref="DlibShapePredictorNameDropdown"/> for use in example scenes.
    /// Disables example buttons that are not supported on the current platform or graphics device.
    /// </summary>
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
        private const string NATIVE_LIBRARY_NAME = "dlibfacelandmarkdetector";
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static float _verticalNormalizedPosition = 1f;
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static DlibShapePredictorNamePreset _dlibShapePredictorName = DlibShapePredictorNamePreset.sp_human_face_68;

        // Public Fields
        public Text VersionInfo;
        public ScrollRect ScrollRect;
        public Dropdown DlibShapePredictorNameDropdown;

        // Public Properties
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

        // Unity Lifecycle Methods
        private void Start()
        {
            VersionInfo.text = NATIVE_LIBRARY_NAME + " " + DlibEnv.GetVersion();
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

            ScrollRect.verticalNormalizedPosition = _verticalNormalizedPosition;

            DlibShapePredictorNameDropdown.value = (int)_dlibShapePredictorName;

#if UNITY_6000_0_OR_NEWER
            // WebCamTextureExample and WebCamTextureDownScaleExample do not work on WebGPU.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
            {
                GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/BasicGroup/WebCamTextureExampleButton").GetComponent<Button>().interactable = false;
                GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/BasicGroup/WebCamTextureDownScaleExampleButton").GetComponent<Button>().interactable = false;
            }
#endif
        }

        private void Update()
        {

        }

        // Public Methods
        public void OnScrollRectValueChanged()
        {
            _verticalNormalizedPosition = ScrollRect.verticalNormalizedPosition;
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

        public void OnWebCamTextureDownScaleExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTextureDownScaleExample");
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

        public void OnMultiSourceToMatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("MultiSourceToMatHelperExample");
        }

        public void OnARHeadExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("ARHeadExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("ARHeadExample_SRP");
            }
        }

        public void OnFrameOptimizationExampleButtonClick()
        {
            SceneManager.LoadScene("FrameOptimizationExample");
        }

        public void OnNoiseFilterExampleButtonClick()
        {
            SceneManager.LoadScene("NoiseFilterExample");
        }

        /// <summary>
        /// Raises the dlib shape predictor name dropdown value changed event.
        /// </summary>
        public void OnDlibShapePredictorNameDropdownValueChanged(int result)
        {
            _dlibShapePredictorName = (DlibShapePredictorNamePreset)result;
        }
    }
}
