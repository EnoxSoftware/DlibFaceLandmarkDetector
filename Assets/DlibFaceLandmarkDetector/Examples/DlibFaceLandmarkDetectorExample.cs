using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace DlibFaceLandmarkDetectorExample
{
    public class DlibFaceLandmarkDetectorExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {

        }

        // Update is called once per frame
        void Update ()
        {

        }

        public void OnShowLicenseButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
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

        public void OnARHeadExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ARHeadExample");
            #else
            Application.LoadLevel ("ARHeadExample");
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
    }
}