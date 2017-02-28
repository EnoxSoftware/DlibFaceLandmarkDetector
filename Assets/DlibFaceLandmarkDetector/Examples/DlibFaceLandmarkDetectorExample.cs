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
            #if UNITY_WSA_10_0
            GameObject.Find("VideoCaptureExample").gameObject.SetActive(false);
            GameObject.Find("VideoCaptureARExample").gameObject.SetActive(false);
            #endif
        }

        // Update is called once per frame
        void Update ()
        {

        }

        public void OnShowLicenseButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnTexture2DExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DExample");
            #else
            Application.LoadLevel ("Texture2DExample");
            #endif
        }

        public void OnWebCamTextureExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureExample");
            #else
            Application.LoadLevel ("WebCamTextureExample");
            #endif
        }

        public void OnCatDetectionExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("CatDetectionExample");
            #else
            Application.LoadLevel ("CatDetectionExample");
            #endif
        }

        public void OnTexture2DToMatExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DToMatExample");
            #else
            Application.LoadLevel ("Texture2DToMatExample");
            #endif
        }

        public void OnWebCamTextureToMatExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatExample");
            #else
            Application.LoadLevel ("WebCamTextureToMatExample");
            #endif
        }

        public void OnWebCamTextureARExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureARExample");
            #else
            Application.LoadLevel ("WebCamTextureARExample");
            #endif
        }

        public void OnVideoCaptureExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureExample");
            #else
            Application.LoadLevel ("VideoCaptureExample");
            #endif
        }

        public void OnVideoCaptureARExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureARExample");
            #else
            Application.LoadLevel ("VideoCaptureARExample");
            #endif
        }
        
        public void OnOptimizationExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OptimizationExample");
            #else
            Application.LoadLevel ("OptimizationExample");
            #endif
        }
    }
}