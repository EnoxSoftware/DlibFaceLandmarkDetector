using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorSample
{
	/// <summary>
	/// Face Landmark Detection from Texture2DToMat Sample.
	/// </summary>
	public class Texture2DToMatSample : MonoBehaviour
	{
		/// <summary>
		/// The image texture.
		/// </summary>
		public Texture2D imgTexture;

		// Use this for initialization
		void Start ()
		{

			gameObject.transform.localScale = new Vector3 (imgTexture.width, imgTexture.height, 1);
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


			Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

			OpenCVForUnity.Utils.texture2DToMat (imgTexture, imgMat);
			Debug.Log ("imgMat dst ToString " + imgMat.ToString ());


			FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat"));

			OpenCVForUnityUtils.SetImage (faceLandmarkDetector, imgMat);

			
			List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect ();
			
			foreach (var rect in detectResult) {
				Debug.Log ("face : " + rect);

				OpenCVForUnityUtils.DrawFaceRect (imgMat, rect, new Scalar (255, 0, 0, 255), 2);


				List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);
								
				Debug.Log ("face points count : " + points.Count);
				if (points.Count > 0) {
					OpenCVForUnityUtils.DrawFaceLandmark (imgMat, points, new Scalar (0, 255, 0, 255), 2);

				}
			}

			
			faceLandmarkDetector.Dispose ();


			Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);

			OpenCVForUnity.Utils.matToTexture2D (imgMat, texture);

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

		}
	
		// Update is called once per frame
		void Update ()
		{
	
		}

		public void OnBackButton ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("DlibFaceLandmarkDetectorSample");
			#else
			Application.LoadLevel ("DlibFaceLandmarkDetectorSample");
			#endif
		}
	}
}
