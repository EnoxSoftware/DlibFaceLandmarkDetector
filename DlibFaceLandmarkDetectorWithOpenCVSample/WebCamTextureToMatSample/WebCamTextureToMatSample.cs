using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorSample
{
	/// <summary>
	/// Face Landmark Detection from WebCamTextureToMat Sample.
	/// </summary>
	[RequireComponent(typeof(WebCamTextureToMatHelper))]
	public class WebCamTextureToMatSample : MonoBehaviour
	{
	
		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;

		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// The web cam texture to mat helper.
		/// </summary>
		WebCamTextureToMatHelper webCamTextureToMatHelper;

		/// <summary>
		/// The face landmark detector.
		/// </summary>
		FaceLandmarkDetector faceLandmarkDetector;

		// Use this for initialization
		void Start ()
		{
			faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat"));

			webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
			webCamTextureToMatHelper.Init ();
		}

		/// <summary>
		/// Raises the web cam texture to mat helper inited event.
		/// </summary>
		public void OnWebCamTextureToMatHelperInited ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperInited");

			Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

			colors = new Color32[webCamTextureMat.cols () * webCamTextureMat.rows ()];
			texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);


			gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
			Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
									
			float width = gameObject.transform.localScale.x;
			float height = gameObject.transform.localScale.y;
									
			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale < heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
			} else {
				Camera.main.orthographicSize = height / 2;
			}

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

		}

		/// <summary>
		/// Raises the web cam texture to mat helper disposed event.
		/// </summary>
		public void OnWebCamTextureToMatHelperDisposed ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperDisposed");

		}

		// Update is called once per frame
		void Update ()
		{

			if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {

				Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

				OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);


				List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect ();
				
				foreach (var rect in detectResult) {

					OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 0, 0, 255), 2);

					List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);

					if (points.Count > 0) {
						OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, points, new Scalar (0, 255, 0, 255), 2);
					}
				}

				Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

				OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, colors);
			}

		}
	
		/// <summary>
		/// Raises the disable event.
		/// </summary>
		void OnDisable ()
		{
			webCamTextureToMatHelper.Dispose ();

			faceLandmarkDetector.Dispose ();
		}

		/// <summary>
		/// Raises the back button event.
		/// </summary>
		public void OnBackButton ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("DlibFaceLandmarkDetectorSample");
			#else
			Application.LoadLevel ("DlibFaceLandmarkDetectorSample");
			#endif
		}

		/// <summary>
		/// Raises the play button event.
		/// </summary>
		public void OnPlayButton ()
		{
			webCamTextureToMatHelper.Play ();
		}

		/// <summary>
		/// Raises the pause button event.
		/// </summary>
		public void OnPauseButton ()
		{
			webCamTextureToMatHelper.Pause ();
		}

		/// <summary>
		/// Raises the stop button event.
		/// </summary>
		public void OnStopButton ()
		{
			webCamTextureToMatHelper.Stop ();
		}

		/// <summary>
		/// Raises the change camera button event.
		/// </summary>
		public void OnChangeCameraButton ()
		{
			webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
		}
	}
}