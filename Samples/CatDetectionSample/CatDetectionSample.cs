using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorSample
{
	/// <summary>
	/// Cat detection sample.
	/// </summary>
	public class CatDetectionSample : MonoBehaviour
	{

		/// <summary>
		/// The texture2 d.
		/// </summary>
		public Texture2D texture2D;

		// Use this for initialization
		void Start ()
		{

			gameObject.transform.localScale = new Vector3 (texture2D.width, texture2D.height, 1);
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


			FaceLandmarkDetector faceLandmarkDetector = new FaceLandmarkDetector (Utils.getFilePath ("frontal_cat_face.svm"), Utils.getFilePath ("shape_predictor_68_cat_face_landmarks.dat"));
			faceLandmarkDetector.SetImage (texture2D);


			List<Rect> detectResult = faceLandmarkDetector.Detect ();

			foreach (var rect in detectResult) {
				Debug.Log ("face : " + rect);
				faceLandmarkDetector.DrawDetectResult (texture2D, 255, 0, 0, 255, 3);

				List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);
				
				Debug.Log ("face points count : " + points.Count);
				if (points.Count > 0) {
					foreach (var point in points) {
						Debug.Log ("face point : x " + point.x + " y " + point.y);
					}
					
					faceLandmarkDetector.DrawDetectLandmarkResult (texture2D, 0, 255, 0, 255);
				}
			}

			faceLandmarkDetector.Dispose ();

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture2D;

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
