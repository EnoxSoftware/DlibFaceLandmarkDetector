using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace DlibFaceLandmarkDetectorSample
{
	public class DlibFaceLandmarkDetectorSample : MonoBehaviour
	{

		// Use this for initialization
		void Start ()
		{
			#if UNITY_WSA_10_0
			GameObject.Find("VideoCaptureSample").gameObject.SetActive(false);
			GameObject.Find("VideoCaptureARSample").gameObject.SetActive(false);
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

		public void OnTexture2DSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("Texture2DSample");
			#else
			Application.LoadLevel ("Texture2DSample");
			#endif
		}
		
		public void OnWebCamTextureSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("WebCamTextureSample");
			#else
			Application.LoadLevel ("WebCamTextureSample");
			#endif
		}

		public void OnCatDetectionSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("CatDetectionSample");
			#else
			Application.LoadLevel ("CatDetectionSample");
			#endif
		}

		public void OnTexture2DToMatSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("Texture2DToMatSample");
			#else
			Application.LoadLevel ("Texture2DToMatSample");
			#endif
		}
				
		public void OnWebCamTextureToMatSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("WebCamTextureToMatSample");
			#else
			Application.LoadLevel ("WebCamTextureToMatSample");
			#endif
		}
				
		public void OnWebCamTextureARSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("WebCamTextureARSample");
			#else
			Application.LoadLevel ("WebCamTextureARSample");
			#endif
		}

		public void OnVideoCaptureSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("VideoCaptureSample");
			#else
			Application.LoadLevel ("VideoCaptureSample");
			#endif
		}
		
		public void OnVideoCaptureARSample ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("VideoCaptureARSample");
			#else
			Application.LoadLevel ("VideoCaptureARSample");
			#endif
		}
	}
}
		