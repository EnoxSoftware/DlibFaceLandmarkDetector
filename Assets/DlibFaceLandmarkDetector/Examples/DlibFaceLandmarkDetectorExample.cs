using DlibFaceLandmarkDetector.UnityUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            sp_human_face_17,
            sp_human_face_17_for_mobile,
            sp_human_face_6,
        }

        public Dropdown dlibShapePredictorNameDropdown;

        static DlibShapePredictorNamePreset dlibShapePredictorName = DlibShapePredictorNamePreset.sp_human_face_68;

        /// <summary>
        /// The name of dlib shape predictor file to use in the example scenes.
        /// </summary>
        public static string dlibShapePredictorFileName
        {
            get
            {
                return "DlibFaceLandmarkDetector/" + dlibShapePredictorName.ToString() + ".dat";
            }
        }

        // Use this for initialization
        void Start()
        {
            versionInfo.text = "dlibfacelandmarkdetector" + " " + Utils.getVersion();
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
            versionInfo.text += " ";
#if ENABLE_MONO
            versionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            versionInfo.text += ".NET";
#endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;

            dlibShapePredictorNameDropdown.value = (int)dlibShapePredictorName;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnScrollRectValueChanged()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
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

        public void OnCatDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("CatDetectionExample");
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
            dlibShapePredictorName = (DlibShapePredictorNamePreset)result;
        }
    }
}