using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorSample
{
	/// <summary>
	/// Face tracker AR from WebCamTexture Sample.
	/// This sample was referring to http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
	/// and use effect asset from http://ktk-kumamoto.hatenablog.com/entry/2014/09/14/092400
	/// </summary>
	[RequireComponent(typeof(WebCamTextureToMatHelper))]
	public class WebCamTextureARSample : MonoBehaviour
	{
		/// <summary>
		/// The should draw face points.
		/// </summary>
		public bool shouldDrawFacePoints;

		/// <summary>
		/// The should draw axes.
		/// </summary>
		public bool shouldDrawAxes;

		/// <summary>
		/// The should draw head.
		/// </summary>
		public bool shouldDrawHead;

		/// <summary>
		/// The should draw effects.
		/// </summary>
		public bool shouldDrawEffects;
		
		/// <summary>
		/// The axes.
		/// </summary>
		public GameObject axes;
		
		/// <summary>
		/// The head.
		/// </summary>
		public GameObject head;
		
		/// <summary>
		/// The right eye.
		/// </summary>
		public GameObject rightEye;
		
		/// <summary>
		/// The left eye.
		/// </summary>
		public GameObject leftEye;
		
		/// <summary>
		/// The mouth.
		/// </summary>
		public GameObject mouth;

		/// <summary>
		/// The mouth particle system.
		/// </summary>
		ParticleSystem[] mouthParticleSystem;
		
		/// <summary>
		/// The rvec noise filter range.
		/// </summary>
		[Range(0, 50)]
		public float
			rvecNoiseFilterRange = 8;
		
		/// <summary>
		/// The tvec noise filter range.
		/// </summary>
		[Range(0, 360)]
		public float
			tvecNoiseFilterRange = 90;
		
		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;
		
		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// The face landmark detector.
		/// </summary>
		FaceLandmarkDetector faceLandmarkDetector;
		
		/// <summary>
		/// The AR camera.
		/// </summary>
		public Camera ARCamera;
		
		/// <summary>
		/// The cam matrix.
		/// </summary>
		Mat camMatrix;
		
		/// <summary>
		/// The dist coeffs.
		/// </summary>
		MatOfDouble distCoeffs;
		
		/// <summary>
		/// The invert Y.
		/// </summary>
		Matrix4x4 invertYM;
		
		/// <summary>
		/// The transformation m.
		/// </summary>
		Matrix4x4 transformationM = new Matrix4x4 ();
		
		/// <summary>
		/// The invert Z.
		/// </summary>
		Matrix4x4 invertZM;
		
		/// <summary>
		/// The ar m.
		/// </summary>
		Matrix4x4 ARM;

		/// <summary>
		/// The ar game object.
		/// </summary>
		public GameObject ARGameObject;

		/// <summary>
		/// The should move AR camera.
		/// </summary>
		public bool shouldMoveARCamera;
		
		/// <summary>
		/// The 3d face object points.
		/// </summary>
		MatOfPoint3f objectPoints;
		
		/// <summary>
		/// The image points.
		/// </summary>
		MatOfPoint2f imagePoints;
		
		/// <summary>
		/// The rvec.
		/// </summary>
		Mat rvec;
		
		/// <summary>
		/// The tvec.
		/// </summary>
		Mat tvec;
		
		/// <summary>
		/// The rot m.
		/// </summary>
		Mat rotM;

		/// <summary>
		/// The web cam texture to mat helper.
		/// </summary>
		WebCamTextureToMatHelper webCamTextureToMatHelper;
		
		// Use this for initialization
		void Start ()
		{
			//set 3d face object points.
			objectPoints = new MatOfPoint3f (
				new Point3 (-31, 72, 86),//l eye
				new Point3 (31, 72, 86),//r eye
				new Point3 (0, 40, 114),//nose
				new Point3 (-23, 19, 76),//l mouse
				new Point3 (23, 19, 76),//r mouse
				new Point3 (-70, 60, -9),//l ear
				new Point3 (70, 60, -9)//r ear
			);
			imagePoints = new MatOfPoint2f ();
			rvec = new Mat ();
			tvec = new Mat ();
			rotM = new Mat (3, 3, CvType.CV_64FC1);

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

			float imageScale = 1.0f;
			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale < heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
				imageScale = (float)Screen.height / (float)Screen.width;
			} else {
				Camera.main.orthographicSize = height / 2;
			}
			
			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
									
									
			int max_d = Mathf.Max (webCamTextureMat.rows (), webCamTextureMat.cols ());
			camMatrix = new Mat (3, 3, CvType.CV_64FC1);
			camMatrix.put (0, 0, max_d);
			camMatrix.put (0, 1, 0);
			camMatrix.put (0, 2, webCamTextureMat.cols () / 2.0f);
			camMatrix.put (1, 0, 0);
			camMatrix.put (1, 1, max_d);
			camMatrix.put (1, 2, webCamTextureMat.rows () / 2.0f);
			camMatrix.put (2, 0, 0);
			camMatrix.put (2, 1, 0);
			camMatrix.put (2, 2, 1.0f);
									
			Size imageSize = new Size (webCamTextureMat.cols () * imageScale, webCamTextureMat.rows () * imageScale);
			double apertureWidth = 0;
			double apertureHeight = 0;
			double[] fovx = new double[1];
			double[] fovy = new double[1];
			double[] focalLength = new double[1];
			Point principalPoint = new Point ();
			double[] aspectratio = new double[1];
									
									
									
									
			Calib3d.calibrationMatrixValues (camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);
									
			Debug.Log ("imageSize " + imageSize.ToString ());
			Debug.Log ("apertureWidth " + apertureWidth);
			Debug.Log ("apertureHeight " + apertureHeight);
			Debug.Log ("fovx " + fovx [0]);
			Debug.Log ("fovy " + fovy [0]);
			Debug.Log ("focalLength " + focalLength [0]);
			Debug.Log ("principalPoint " + principalPoint.ToString ());
			Debug.Log ("aspectratio " + aspectratio [0]);
									
									
			if (widthScale < heightScale) {
				ARCamera.fieldOfView = (float)fovx [0];
			} else {
				ARCamera.fieldOfView = (float)fovy [0];
			}

									
			Debug.Log ("camMatrix " + camMatrix.dump ());
									
									
			distCoeffs = new MatOfDouble (0, 0, 0, 0);
			Debug.Log ("distCoeffs " + distCoeffs.dump ());
									
									
									
			invertYM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, -1, 1));
			Debug.Log ("invertYM " + invertYM.ToString ());
			
			invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
			Debug.Log ("invertZM " + invertZM.ToString ());
			
			
			axes.SetActive (false);
			head.SetActive (false);
			rightEye.SetActive (false);
			leftEye.SetActive (false);
			mouth.SetActive (false);


			mouthParticleSystem = mouth.GetComponentsInChildren<ParticleSystem> (true);

		}
		
		/// <summary>
		/// Raises the web cam texture to mat helper disposed event.
		/// </summary>
		public void OnWebCamTextureToMatHelperDisposed ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperDisposed");

			camMatrix.Dispose ();
			distCoeffs.Dispose ();
		}

		// Update is called once per frame
		void Update ()
		{

			if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {
				
				Mat rgbaMat = webCamTextureToMatHelper.GetMat ();


				OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);

				List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect ();

				if (detectResult.Count > 0) {

					List<Vector2> points = faceLandmarkDetector.DetectLandmark (detectResult [0]);

					if (points.Count > 0) {
						if (shouldDrawFacePoints)
							OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, points, new Scalar (0, 255, 0, 255), 2);

						imagePoints.fromArray (
							new Point ((points [38].x + points [41].x) / 2, (points [38].y + points [41].y) / 2),//l eye
							new Point ((points [43].x + points [46].x) / 2, (points [43].y + points [46].y) / 2),//r eye
							new Point (points [33].x, points [33].y),//nose
							new Point (points [48].x, points [48].y),//l mouth
							new Point (points [54].x, points [54].y) //r mouth
														,
							new Point (points [0].x, points [0].y),//l ear
							new Point (points [16].x, points [16].y)//r ear
						);
																		
																		
						Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);


						if (tvec.get (2, 0) [0] > 0){

							if (Mathf.Abs ((float)(points [43].y - points [46].y)) > Mathf.Abs ((float)(points [42].x - points [45].x)) / 6.0) {
								if (shouldDrawEffects)
									rightEye.SetActive (true);
							}

							if (Mathf.Abs ((float)(points [38].y - points [41].y)) > Mathf.Abs ((float)(points [39].x - points [36].x)) / 6.0) {
								if (shouldDrawEffects)
									leftEye.SetActive (true);
							}
							if (shouldDrawHead)
								head.SetActive (true);
							if (shouldDrawAxes)
								axes.SetActive (true);
													
													
							float noseDistance = Mathf.Abs ((float)(points [27].y - points [33].y));
							float mouseDistance = Mathf.Abs ((float)(points [62].y - points [66].y));
							if (mouseDistance > noseDistance / 5.0) {
								if (shouldDrawEffects) {
									mouth.SetActive (true);
									foreach (ParticleSystem ps in mouthParticleSystem) {
										ps.enableEmission = true;
										ps.startSize = 40 * (mouseDistance / noseDistance);
									}
								}
							} else {
								if (shouldDrawEffects) {
									foreach (ParticleSystem ps in mouthParticleSystem) {
										ps.enableEmission = false;
									}
								}
							}

													
							Calib3d.Rodrigues (rvec, rotM);
													
							transformationM .SetRow (0, new Vector4 ((float)rotM.get (0, 0) [0], (float)rotM.get (0, 1) [0], (float)rotM.get (0, 2) [0], (float)tvec.get (0, 0) [0]));
							transformationM.SetRow (1, new Vector4 ((float)rotM.get (1, 0) [0], (float)rotM.get (1, 1) [0], (float)rotM.get (1, 2) [0], (float)tvec.get (1, 0) [0]));
							transformationM.SetRow (2, new Vector4 ((float)rotM.get (2, 0) [0], (float)rotM.get (2, 1) [0], (float)rotM.get (2, 2) [0], (float)tvec.get (2, 0) [0]));
							transformationM.SetRow (3, new Vector4 (0, 0, 0, 1));
													
							if (shouldMoveARCamera) {

								if (ARGameObject != null) {
									ARM = ARGameObject.transform.localToWorldMatrix * invertZM * transformationM.inverse * invertYM;
									ARUtils.SetTransformFromMatrix (ARCamera.transform, ref ARM);
									ARGameObject.SetActive (true);
								}
							} else {
								ARM = ARCamera.transform.localToWorldMatrix * invertYM * transformationM * invertZM;

								if (ARGameObject != null) {
									ARUtils.SetTransformFromMatrix (ARGameObject.transform, ref ARM);
									ARGameObject.SetActive (true);
								}
							}

						}
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
				
		public void OnDrawFacePointsButton ()
		{
			if (shouldDrawFacePoints) {
				shouldDrawFacePoints = false;
			} else {
				shouldDrawFacePoints = true;
			}
		}
				
		public void OnDrawAxesButton ()
		{
			if (shouldDrawAxes) {
				shouldDrawAxes = false;
				axes.SetActive (false);
			} else {
				shouldDrawAxes = true;
			}
		}
				
		public void OnDrawHeadButton ()
		{
			if (shouldDrawHead) {
				shouldDrawHead = false;
				head.SetActive (false);
			} else {
				shouldDrawHead = true;
			}
		}

		public void OnDrawEffectsButton ()
		{
			if (shouldDrawEffects) {
				shouldDrawEffects = false;
				rightEye.SetActive (false);
				leftEye.SetActive (false);
				mouth.SetActive (false);
			} else {
				shouldDrawEffects = true;
			}
		}

	}
}