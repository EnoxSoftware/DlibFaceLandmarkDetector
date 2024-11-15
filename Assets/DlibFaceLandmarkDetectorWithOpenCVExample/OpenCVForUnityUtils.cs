using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Utility class for the integration of DlibFaceLandmarkDetector and OpenCVForUnity.
    /// </summary>
    public static class OpenCVForUnityUtils
    {
        /// <summary>
        /// Sets the image for the specified <see cref="FaceLandmarkDetector"/> using a given <see cref="Mat"/> object.
        /// </summary>
        /// <param name="faceLandmarkDetector">
        /// An instance of <see cref="FaceLandmarkDetector"/> that processes the specified image.
        /// </param>
        /// <param name="imgMat">
        /// A <see cref="Mat"/> object representing the image to set. The matrix must be continuous and valid for processing.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="faceLandmarkDetector"/> or <paramref name="imgMat"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="imgMat"/> is not continuous. Ensure <c>imgMat.isContinuous() == true</c>.
        /// </exception>
        /// <remarks>
        /// This method directly assigns the <paramref name="imgMat"/> data pointer, width, height, and element size to the
        /// specified <see cref="FaceLandmarkDetector"/>. It avoids additional memory allocations by reusing the existing <paramref name="imgMat"/> data.
        /// </remarks>
        public static void SetImage(FaceLandmarkDetector faceLandmarkDetector, Mat imgMat)
        {
            if (faceLandmarkDetector == null)
                throw new ArgumentNullException(nameof(faceLandmarkDetector));
            if (faceLandmarkDetector != null)
                faceLandmarkDetector.ThrowIfDisposed();

            if (imgMat == null)
                throw new ArgumentNullException(nameof(imgMat));
            if (imgMat != null)
                imgMat.ThrowIfDisposed();
            if (!imgMat.isContinuous())
                throw new ArgumentException("imgMat.isContinuous() must be true.");

            faceLandmarkDetector.SetImage((IntPtr)imgMat.dataAddr(), imgMat.width(), imgMat.height(), (int)imgMat.elemSize());
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="UnityEngine.Rect"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="Scalar"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, UnityEngine.Rect rect, Scalar color, int thickness)
        {
            Imgproc.rectangle(imgMat, new Point(rect.xMin, rect.yMin), new Point(rect.xMax, rect.yMax), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="UnityEngine.Rect"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="Vec4d"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, UnityEngine.Rect rect, in Vec4d color, int thickness)
        {
            Imgproc.rectangle(imgMat, new Vec2d(rect.xMin, rect.yMin), new Vec2d(rect.xMax, rect.yMax), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="UnityEngine.Rect"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, UnityEngine.Rect rect, in (double v0, double v1, double v2, double v3) color, int thickness)
        {
            Imgproc.rectangle(imgMat, (rect.xMin, rect.yMin), (rect.xMax, rect.yMax), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="OpenCVForUnity.CoreModule.Rect"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="Scalar"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, OpenCVForUnity.CoreModule.Rect rect, Scalar color, int thickness)
        {
            Imgproc.rectangle(imgMat, rect, color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="Vec4i"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="Vec4d"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, in Vec4i rect, in Vec4d color, int thickness)
        {
            Imgproc.rectangle(imgMat, rect, color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="Vec4d"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="Vec4d"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, in Vec4d rect, in Vec4d color, int thickness)
        {
            Imgproc.rectangle(imgMat, rect.ToVec4i(), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="ValueTuple{Int32, Int32, Int32, Int32}"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, in (int x, int y, int width, int height) rect, in (double v0, double v1, double v2, double v3) color, int thickness)
        {
            Imgproc.rectangle(imgMat, rect, color, thickness);
        }

        /// <summary>
        /// Draws a rectangle on the specified image to indicate a detected face region.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle.</param>
        /// <param name="rect">The <see cref="ValueTuple{Double, Double, Double, Double}"/> defining the area to highlight.</param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        public static void DrawFaceRect(Mat imgMat, in (double x, double y, double width, double height) rect, in (double v0, double v1, double v2, double v3) color, int thickness)
        {
            Imgproc.rectangle(imgMat, ((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle and detection information on the specified image based on the given <see cref="RectDetection"/> data.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle and text.</param>
        /// <param name="rect">The <see cref="DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection"/> containing the rectangle and detection details.</param>
        /// <param name="color">The <see cref="Scalar"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rect"/> is null.</exception>
        public static void DrawFaceRect(Mat imgMat, DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection rect, Scalar color, int thickness)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));

            UnityEngine.Rect _rect = rect.rect;
            Imgproc.putText(imgMat, "detection_confidence : " + rect.detection_confidence, new Point(_rect.xMin, _rect.yMin - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "weight_index : " + rect.weight_index, new Point(_rect.xMin, _rect.yMin - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(imgMat, new Point(_rect.xMin, _rect.yMin), new Point(_rect.xMax, _rect.yMax), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle and detection information on the specified image based on the given <see cref="DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection"/> data.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle and text.</param>
        /// <param name="rect">The <see cref="DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection"/> containing the rectangle and detection details.</param>
        /// <param name="color">The <see cref="Vec4d"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rect"/> is null.</exception>
        public static void DrawFaceRect(Mat imgMat, DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection rect, in Vec4d color, int thickness)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));

            UnityEngine.Rect _rect = rect.rect;
            Imgproc.putText(imgMat, "detection_confidence : " + rect.detection_confidence, new Vec2d(_rect.xMin, _rect.yMin - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Vec4d(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "weight_index : " + rect.weight_index, new Vec2d(_rect.xMin, _rect.yMin - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Vec4d(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(imgMat, new Vec2d(_rect.xMin, _rect.yMin), new Vec2d(_rect.xMax, _rect.yMax), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle and detection information on the specified image based on the given <see cref="DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection"/> data.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle and text.</param>
        /// <param name="rect">The <see cref="DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection"/> containing the rectangle and detection details.</param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rect"/> is null.</exception>
        public static void DrawFaceRect(Mat imgMat, DlibFaceLandmarkDetector.FaceLandmarkDetector.RectDetection rect, in (double v0, double v1, double v2, double v3) color, int thickness)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));

            UnityEngine.Rect _rect = rect.rect;
            Imgproc.putText(imgMat, "detection_confidence : " + rect.detection_confidence, (_rect.xMin, _rect.yMin - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "weight_index : " + rect.weight_index, (_rect.xMin, _rect.yMin - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(imgMat, (_rect.xMin, _rect.yMin), (_rect.xMax, _rect.yMax), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle and detection information on the specified image based on the provided rectangle data.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle and text.</param>
        /// <param name="rect">
        /// An array containing the rectangle data in the format <c>[x, y, width, height, detection_confidence, weight_index]</c>. 
        /// The last two values are optional and used for displaying additional detection information.
        /// </param>
        /// <param name="color">The <see cref="Scalar"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rect"/> is null.</exception>
        public static void DrawFaceRect(Mat imgMat, double[] rect, Scalar color, int thickness)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));

            if (rect.Length > 4)
                Imgproc.putText(imgMat, "detection_confidence : " + rect[4], new Point(rect[0], rect[1] - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            if (rect.Length > 5)
                Imgproc.putText(imgMat, "weight_index : " + rect[5], new Point(rect[0], rect[1] - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(imgMat, new Point(rect[0], rect[1]), new Point(rect[0] + rect[2], rect[1] + rect[3]), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle and detection information on the specified image based on the provided rectangle data.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle and text.</param>
        /// <param name="rect">
        /// An array containing the rectangle data in the format <c>[x, y, width, height, detection_confidence, weight_index]</c>. 
        /// The last two values are optional and used for displaying additional detection information.
        /// </param>
        /// <param name="color">The <see cref="Vec4d"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rect"/> is null.</exception>
        public static void DrawFaceRect(Mat imgMat, double[] rect, in Vec4d color, int thickness)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));

            if (rect.Length > 4)
                Imgproc.putText(imgMat, "detection_confidence : " + rect[4], new Vec2d(rect[0], rect[1] - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Vec4d(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            if (rect.Length > 5)
                Imgproc.putText(imgMat, "weight_index : " + rect[5], new Vec2d(rect[0], rect[1] - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Vec4d(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(imgMat, new Vec2d(rect[0], rect[1]), new Vec2d(rect[0] + rect[2], rect[1] + rect[3]), color, thickness);
        }

        /// <summary>
        /// Draws a rectangle and detection information on the specified image based on the provided rectangle data.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the rectangle and text.</param>
        /// <param name="rect">
        /// An array containing the rectangle data in the format <c>[x, y, width, height, detection_confidence, weight_index]</c>. 
        /// The last two values are optional and used for displaying additional detection information.
        /// </param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color of the rectangle border.</param>
        /// <param name="thickness">The thickness of the rectangle border.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rect"/> is null.</exception>
        public static void DrawFaceRect(Mat imgMat, double[] rect, in (double v0, double v1, double v2, double v3) color, int thickness)
        {
            if (rect == null)
                throw new ArgumentNullException(nameof(rect));

            if (rect.Length > 4)
                Imgproc.putText(imgMat, "detection_confidence : " + rect[4], (rect[0], rect[1] - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            if (rect.Length > 5)
                Imgproc.putText(imgMat, "weight_index : " + rect[5], (rect[0], rect[1] - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(imgMat, (rect[0], rect[1]), (rect[0] + rect[2], rect[1] + rect[3]), color, thickness);
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A list of <see cref="Vector2"/> points representing the landmark positions. The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks. 
        /// </param>
        /// <param name="color">The <see cref="Scalar"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, IList<Vector2> points, Scalar color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (points.Count == 5)
            {

                Imgproc.line(imgMat, new Point(points[0].x, points[0].y), new Point(points[1].x, points[1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[1].x, points[1].y), new Point(points[4].x, points[4].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[4].x, points[4].y), new Point(points[3].x, points[3].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[3].x, points[3].y), new Point(points[2].x, points[2].y), color, thickness);

            }
            else if (points.Count == 6)
            {

                Imgproc.line(imgMat, new Point(points[2].x, points[2].y), new Point(points[3].x, points[3].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[4].x, points[4].y), new Point(points[5].x, points[5].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[3].x, points[3].y), new Point(points[0].x, points[0].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[4].x, points[4].y), new Point(points[0].x, points[0].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[0].x, points[0].y), new Point(points[1].x, points[1].y), color, thickness);

            }
            else if (points.Count == 17)
            {

                Imgproc.line(imgMat, new Point(points[2].x, points[2].y), new Point(points[9].x, points[9].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[9].x, points[9].y), new Point(points[3].x, points[3].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[3].x, points[3].y), new Point(points[10].x, points[10].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[10].x, points[10].y), new Point(points[2].x, points[2].y), color, thickness);

                Imgproc.line(imgMat, new Point(points[4].x, points[4].y), new Point(points[11].x, points[11].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[11].x, points[11].y), new Point(points[5].x, points[5].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[5].x, points[5].y), new Point(points[12].x, points[12].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[12].x, points[12].y), new Point(points[4].x, points[4].y), color, thickness);

                Imgproc.line(imgMat, new Point(points[3].x, points[3].y), new Point(points[0].x, points[0].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[4].x, points[4].y), new Point(points[0].x, points[0].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[0].x, points[0].y), new Point(points[1].x, points[1].y), color, thickness);

                for (int i = 14; i <= 16; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[16].x, points[16].y), new Point(points[13].x, points[13].y), color, thickness);

                for (int i = 6; i <= 8; i++)
                    Imgproc.circle(imgMat, new Point(points[i].x, points[i].y), 2, color, -1);

            }
            else if (points.Count == 68)
            {

                for (int i = 1; i <= 16; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);

                for (int i = 28; i <= 30; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);

                for (int i = 18; i <= 21; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                for (int i = 23; i <= 26; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                for (int i = 31; i <= 35; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[30].x, points[30].y), new Point(points[35].x, points[35].y), color, thickness);

                for (int i = 37; i <= 41; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[36].x, points[36].y), new Point(points[41].x, points[41].y), color, thickness);

                for (int i = 43; i <= 47; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[42].x, points[42].y), new Point(points[47].x, points[47].y), color, thickness);

                for (int i = 49; i <= 59; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[48].x, points[48].y), new Point(points[59].x, points[59].y), color, thickness);

                for (int i = 61; i <= 67; ++i)
                    Imgproc.line(imgMat, new Point(points[i].x, points[i].y), new Point(points[i - 1].x, points[i - 1].y), color, thickness);
                Imgproc.line(imgMat, new Point(points[60].x, points[60].y), new Point(points[67].x, points[67].y), color, thickness);
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Imgproc.circle(imgMat, new Point(points[i].x, points[i].y), 2, color, -1);
                }
            }

            // Draw the index number of facelandmark points.
            if (drawIndexNumbers)
            {
                for (int i = 0; i < points.Count; ++i)
                    Imgproc.putText(imgMat, i.ToString(), new Point(points[i].x, points[i].y), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            }
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A list of <see cref="Vec2d"/> points representing the landmark positions. The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks. 
        /// </param>
        /// <param name="color">The <see cref="Vec4d"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, in Vec2d[] points, in Vec4d color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (points.Length == 5)
            {

                Imgproc.line(imgMat, points[0], points[1], color, thickness);
                Imgproc.line(imgMat, points[1], points[4], color, thickness);
                Imgproc.line(imgMat, points[4], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[2], color, thickness);

            }
            else if (points.Length == 6)
            {

                Imgproc.line(imgMat, points[2], points[3], color, thickness);
                Imgproc.line(imgMat, points[4], points[5], color, thickness);
                Imgproc.line(imgMat, points[3], points[0], color, thickness);
                Imgproc.line(imgMat, points[4], points[0], color, thickness);
                Imgproc.line(imgMat, points[0], points[1], color, thickness);

            }
            else if (points.Length == 17)
            {

                Imgproc.line(imgMat, points[2], points[9], color, thickness);
                Imgproc.line(imgMat, points[9], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[10], color, thickness);
                Imgproc.line(imgMat, points[10], points[2], color, thickness);

                Imgproc.line(imgMat, points[4], points[11], color, thickness);
                Imgproc.line(imgMat, points[11], points[5], color, thickness);
                Imgproc.line(imgMat, points[5], points[12], color, thickness);
                Imgproc.line(imgMat, points[12], points[4], color, thickness);

                Imgproc.line(imgMat, points[3], points[0], color, thickness);
                Imgproc.line(imgMat, points[4], points[0], color, thickness);
                Imgproc.line(imgMat, points[0], points[1], color, thickness);

                for (int i = 14; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[16], points[13], color, thickness);

                for (int i = 6; i <= 8; i++)
                    Imgproc.circle(imgMat, points[i], 2, color, -1);

            }
            else if (points.Length == 68)
            {

                for (int i = 1; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 28; i <= 30; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 18; i <= 21; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 23; i <= 26; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 31; i <= 35; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[30], points[35], color, thickness);

                for (int i = 37; i <= 41; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[36], points[41], color, thickness);

                for (int i = 43; i <= 47; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[42], points[47], color, thickness);

                for (int i = 49; i <= 59; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[48], points[59], color, thickness);

                for (int i = 61; i <= 67; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[60], points[67], color, thickness);
            }
            else
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Imgproc.circle(imgMat, points[i], 2, color, -1);
                }
            }

            // Draw the index number of facelandmark points.
            if (drawIndexNumbers)
            {
                for (int i = 0; i < points.Length; ++i)
                    Imgproc.putText(imgMat, i.ToString(), points[i], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Vec4d(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            }
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A list of <see cref="ValueTuple{Double, Double}"/> points representing the landmark positions. The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks. 
        /// </param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, in (double x, double y)[] points, in (double v0, double v1, double v2, double v3) color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (points.Length == 5)
            {

                Imgproc.line(imgMat, points[0], points[1], color, thickness);
                Imgproc.line(imgMat, points[1], points[4], color, thickness);
                Imgproc.line(imgMat, points[4], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[2], color, thickness);

            }
            else if (points.Length == 6)
            {

                Imgproc.line(imgMat, points[2], points[3], color, thickness);
                Imgproc.line(imgMat, points[4], points[5], color, thickness);
                Imgproc.line(imgMat, points[3], points[0], color, thickness);
                Imgproc.line(imgMat, points[4], points[0], color, thickness);
                Imgproc.line(imgMat, points[0], points[1], color, thickness);

            }
            else if (points.Length == 17)
            {

                Imgproc.line(imgMat, points[2], points[9], color, thickness);
                Imgproc.line(imgMat, points[9], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[10], color, thickness);
                Imgproc.line(imgMat, points[10], points[2], color, thickness);

                Imgproc.line(imgMat, points[4], points[11], color, thickness);
                Imgproc.line(imgMat, points[11], points[5], color, thickness);
                Imgproc.line(imgMat, points[5], points[12], color, thickness);
                Imgproc.line(imgMat, points[12], points[4], color, thickness);

                Imgproc.line(imgMat, points[3], points[0], color, thickness);
                Imgproc.line(imgMat, points[4], points[0], color, thickness);
                Imgproc.line(imgMat, points[0], points[1], color, thickness);

                for (int i = 14; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[16], points[13], color, thickness);

                for (int i = 6; i <= 8; i++)
                    Imgproc.circle(imgMat, points[i], 2, color, -1);

            }
            else if (points.Length == 68)
            {

                for (int i = 1; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 28; i <= 30; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 18; i <= 21; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 23; i <= 26; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 31; i <= 35; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[30], points[35], color, thickness);

                for (int i = 37; i <= 41; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[36], points[41], color, thickness);

                for (int i = 43; i <= 47; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[42], points[47], color, thickness);

                for (int i = 49; i <= 59; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[48], points[59], color, thickness);

                for (int i = 61; i <= 67; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[60], points[67], color, thickness);
            }
            else
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Imgproc.circle(imgMat, points[i], 2, color, -1);
                }
            }

            // Draw the index number of facelandmark points.
            if (drawIndexNumbers)
            {
                for (int i = 0; i < points.Length; ++i)
                    Imgproc.putText(imgMat, i.ToString(), points[i], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            }
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A list of <see cref="Point"/> points representing the landmark positions. The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks.
        /// </param>
        /// <param name="color">The <see cref="Scalar"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, IList<Point> points, Scalar color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (points.Count == 5)
            {

                Imgproc.line(imgMat, points[0], points[1], color, thickness);
                Imgproc.line(imgMat, points[1], points[4], color, thickness);
                Imgproc.line(imgMat, points[4], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[2], color, thickness);

            }
            else if (points.Count == 6)
            {

                Imgproc.line(imgMat, points[2], points[3], color, thickness);
                Imgproc.line(imgMat, points[4], points[5], color, thickness);
                Imgproc.line(imgMat, points[3], points[0], color, thickness);
                Imgproc.line(imgMat, points[4], points[0], color, thickness);
                Imgproc.line(imgMat, points[0], points[1], color, thickness);

            }
            else if (points.Count == 17)
            {

                Imgproc.line(imgMat, points[2], points[9], color, thickness);
                Imgproc.line(imgMat, points[9], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[10], color, thickness);
                Imgproc.line(imgMat, points[10], points[2], color, thickness);

                Imgproc.line(imgMat, points[4], points[11], color, thickness);
                Imgproc.line(imgMat, points[11], points[5], color, thickness);
                Imgproc.line(imgMat, points[5], points[12], color, thickness);
                Imgproc.line(imgMat, points[12], points[4], color, thickness);

                Imgproc.line(imgMat, points[3], points[0], color, thickness);
                Imgproc.line(imgMat, points[4], points[0], color, thickness);
                Imgproc.line(imgMat, points[0], points[1], color, thickness);

                for (int i = 14; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[16], points[13], color, thickness);

                for (int i = 6; i <= 8; i++)
                    Imgproc.circle(imgMat, points[i], 2, color, -1);

            }
            else if (points.Count == 68)
            {

                for (int i = 1; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 28; i <= 30; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 18; i <= 21; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 23; i <= 26; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 31; i <= 35; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[30], points[35], color, thickness);

                for (int i = 37; i <= 41; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[36], points[41], color, thickness);

                for (int i = 43; i <= 47; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[42], points[47], color, thickness);

                for (int i = 49; i <= 59; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[48], points[59], color, thickness);

                for (int i = 61; i <= 67; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[60], points[67], color, thickness);
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Imgproc.circle(imgMat, points[i], 2, color, -1);
                }
            }

            // Draw the index number of facelandmark points.
            if (drawIndexNumbers)
            {
                for (int i = 0; i < points.Count; ++i)
                    Imgproc.putText(imgMat, i.ToString(), points[i], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            }
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A list of <see cref="Point"/> points representing the landmark positions. The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks.
        /// </param>
        /// <param name="color">The <see cref="Scalar"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, double[] points, Scalar color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            List<Vector2> _points = new List<Vector2>();
            for (int i = 0; i < points.Length; i = i + 2)
            {
                _points.Add(new Vector2((float)points[i], (float)points[i + 1]));
            }
            DrawFaceLandmark(imgMat, _points, color, thickness, drawIndexNumbers);
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A <see cref="double"/> array representing the landmark positions, where each pair of consecutive values represents the X and Y coordinates of a point. 
        /// The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks.
        /// </param>
        /// <param name="color">The <see cref="Vec4d"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, double[] points, in Vec4d color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            Vec2d[] pointsVec2d = new Vec2d[points.Length / 2];
#if NET_STANDARD_2_1
            Span<Vec2d> pointsSpan = MemoryMarshal.Cast<double, Vec2d>(points);
            pointsSpan.CopyTo(pointsVec2d);
#else          
            for (int i = 0; i < pointsVec2d.Length; i++)
            {
                pointsVec2d[i].Item1 = points[i * 2 + 0];
                pointsVec2d[i].Item2 = points[i * 2 + 1];
            }
#endif
            DrawFaceLandmark(imgMat, pointsVec2d, color, thickness, drawIndexNumbers);
        }

        /// <summary>
        /// Draws a face landmark on the specified image.
        /// This method supports drawing landmarks for 68, 17, 6, or 5 landmark points.
        /// The landmarks are drawn by connecting the points with lines, and optionally, index numbers can be drawn on each point.
        /// </summary>
        /// <param name="imgMat">The <see cref="Mat"/> image on which to draw the face landmarks.</param>
        /// <param name="points">
        /// A <see cref="double"/> array representing the landmark positions, where each pair of consecutive values represents the X and Y coordinates of a point. 
        /// The number of points must match one of the following:
        /// 5 points for a basic face shape (e.g., eyes, nose, mouth),
        /// 6 points for a face with more detailed landmarks,
        /// 17 points for a detailed face shape, or
        /// 68 points for a full set of face landmarks.
        /// </param>
        /// <param name="color">The <see cref="ValueTuple{Double, Double, Double, Double}"/> color used to draw the landmarks and lines.</param>
        /// <param name="thickness">The thickness of the lines used to connect the landmarks.</param>
        /// <param name="drawIndexNumbers">
        /// If set to <c>true</c>, index numbers will be drawn next to each landmark point.
        /// If set to <c>false</c>, no index numbers will be drawn. Default is <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is <c>null</c>.</exception>
        public static void DrawFaceLandmark(Mat imgMat, double[] points, in (double v0, double v1, double v2, double v3) color, int thickness, bool drawIndexNumbers = false)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            (double x, double y)[] pointsValueTuple = new (double x, double y)[points.Length / 2];
            for (int i = 0; i < pointsValueTuple.Length; i++)
            {
                pointsValueTuple[i].Item1 = points[i * 2 + 0];
                pointsValueTuple[i].Item2 = points[i * 2 + 1];
            }
            DrawFaceLandmark(imgMat, pointsValueTuple, color, thickness, drawIndexNumbers);
        }


        /// <summary>
        /// Convert Vector2 list to Vector2 array.
        /// </summary>
        /// <param name="src">List of Vector2.</param>
        /// <param name="dst">Array of Vector2.</param>
        /// <returns>Array of Vector2.</returns>
        public static Vector2[] ConvertVector2ListToVector2Array(IList<Vector2> src, Vector2[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Vector2[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].x = src[i].x;
                dst[i].y = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Vector2 list to Point list.
        /// </summary>
        /// <param name="src">List of Vector2.</param>
        /// <param name="dst">List of Point.</param>
        /// <returns>List of Point.</returns>
        public static List<Point> ConvertVector2ListToPointList(IList<Vector2> src, List<Point> dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst == null)
            {
                dst = new List<Point>();
            }

            if (dst.Count != src.Count)
            {
                dst.Clear();
                for (int i = 0; i < src.Count; i++)
                {
                    dst.Add(new Point());
                }
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].x = src[i].x;
                dst[i].y = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Vector2 list to Point array.
        /// </summary>
        /// <param name="src">List of Vector2.</param>
        /// <param name="dst">Array of Point.</param>
        /// <returns>Array of Point.</returns>
        public static Point[] ConvertVector2ListToPointArray(IList<Vector2> src, Point[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Point[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i] = new Point(src[i].x, src[i].y);
            }

            return dst;
        }

        /// <summary>
        /// Convert Vector2 list to double array.
        /// </summary>
        /// <param name="src">List of Vector2.</param>
        /// <param name="dst">Array of double.</param>
        /// <returns>Array of double.</returns>
        public static double[] ConvertVector2ListToArray(IList<Vector2> src, double[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count * 2 != dst.Length)
                throw new ArgumentException("src.Count * 2 != dst.Length");

            if (dst == null)
            {
                dst = new double[src.Count * 2];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i * 2] = src[i].x;
                dst[i * 2 + 1] = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Vector2 list to Vec2d array.
        /// </summary>
        /// <param name="src">List of Vector2.</param>
        /// <param name="dst">Array of Vec2d.</param>
        /// <returns>Array of Vec2d.</returns>
        public static Vec2d[] ConvertVector2ListToVec2dArray(IList<Vector2> src, Vec2d[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Vec2d[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].Item1 = src[i].x;
                dst[i].Item2 = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Vector2 list to ValueTuple array.
        /// </summary>
        /// <param name="src">List of Vector2.</param>
        /// <param name="dst">Array of ValueTuple.</param>
        /// <returns>Array of ValueTuple.</returns>
        public static (double x, double y)[] ConvertVector2ListToValueTupleArray(IList<Vector2> src, (double x, double y)[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new (double x, double y)[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].Item1 = src[i].x;
                dst[i].Item2 = src[i].y;
            }

            return dst;
        }


        /// <summary>
        /// Convert Point list to Point array.
        /// </summary>
        /// <param name="src">List of Point.</param>
        /// <param name="dst">Array of Point.</param>
        /// <returns>Array of Point.</returns>
        public static Point[] ConvertPointListToPointArray(IList<Point> src, Point[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Point[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i] = new Point(src[i].x, src[i].y);
            }

            return dst;
        }

        /// <summary>
        /// Convert Point list to Vector2 list.
        /// </summary>
        /// <param name="src">List of Point.</param>
        /// <param name="dst">List of Vector2.</param>
        /// <returns>List of Vector2.</returns>
        public static List<Vector2> ConvertPointListToVector2List(IList<Point> src, List<Vector2> dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst == null)
            {
                dst = new List<Vector2>();
            }

            dst.Clear();

            for (int i = 0; i < src.Count; ++i)
            {
                dst.Add(new Vector2((float)src[i].x, (float)src[i].y));
            }

            return dst;
        }

        /// <summary>
        /// Convert Point list to Vector2 array.
        /// </summary>
        /// <param name="src">List of Point.</param>
        /// <param name="dst">Array of Vector2.</param>
        /// <returns>Array of Vector2.</returns>
        public static Vector2[] ConvertPointListToVector2Array(IList<Point> src, Vector2[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Vector2[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].x = (float)src[i].x;
                dst[i].y = (float)src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Point list to double array.
        /// </summary>
        /// <param name="src">List of Point.</param>
        /// <param name="dst">Array of double.</param>
        /// <returns>Array of double.</returns>
        public static double[] ConvertPointListToArray(IList<Point> src, double[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count * 2 != dst.Length)
                throw new ArgumentException("src.Count * 2 != dst.Length");

            if (dst == null)
            {
                dst = new double[src.Count * 2];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i * 2] = src[i].x;
                dst[i * 2 + 1] = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Point list to Vec2d array.
        /// </summary>
        /// <param name="src">List of Point.</param>
        /// <param name="dst">Array of Vec2d.</param>
        /// <returns>Array of Vec2d.</returns>
        public static Vec2d[] ConvertPointListToVec2dArray(IList<Point> src, Vec2d[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Vec2d[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].Item1 = src[i].x;
                dst[i].Item2 = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert Point list to ValueTuple array.
        /// </summary>
        /// <param name="src">List of Point.</param>
        /// <param name="dst">Array of ValueTuple.</param>
        /// <returns>Array of ValueTuple.</returns>
        public static (double x, double y)[] ConvertPointListToValueTupleArray(IList<Point> src, (double x, double y)[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Count != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new (double x, double y)[src.Count];
            }

            for (int i = 0; i < src.Count; ++i)
            {
                dst[i].Item1 = src[i].x;
                dst[i].Item2 = src[i].y;
            }

            return dst;
        }

        /// <summary>
        /// Convert double array to Vector2 list.
        /// </summary>
        /// <param name="src">Array of double.</param>
        /// <param name="dst">List of Vector2.</param>
        /// <returns>List of Vector2.</returns>
        public static List<Vector2> ConvertArrayToVector2List(double[] src, List<Vector2> dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst == null)
            {
                dst = new List<Vector2>();
            }

            dst.Clear();

            int len = src.Length / 2;
            for (int i = 0; i < len; ++i)
            {
                dst.Add(new Vector2((float)src[i * 2], (float)src[i * 2 + 1]));
            }

            return dst;
        }

        /// <summary>
        /// Convert double array to Vector2 array.
        /// </summary>
        /// <param name="src">Array of double.</param>
        /// <param name="dst">Array of Vector2.</param>
        /// <returns>Array of Vector2.</returns>
        public static Vector2[] ConvertArrayToVector2Array(double[] src, Vector2[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length / 2 != dst.Length)
                throw new ArgumentException("src.Length / 2 != dst.Length");

            if (dst == null)
            {
                dst = new Vector2[src.Length / 2];
            }

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i].x = (float)src[i * 2];
                dst[i].y = (float)src[i * 2 + 1];
            }

            return dst;
        }

        /// <summary>
        /// Convert double array to Point list.
        /// </summary>
        /// <param name="src">Array of double.</param>
        /// <param name="dst">List of Point.</param>
        /// <returns>List of Point.</returns>
        public static List<Point> ConvertArrayToPointList(double[] src, List<Point> dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst == null)
            {
                dst = new List<Point>();
            }

            if (dst.Count != src.Length / 2)
            {
                dst.Clear();
                for (int i = 0; i < src.Length / 2; i++)
                {
                    dst.Add(new Point());
                }
            }

            for (int i = 0; i < dst.Count; ++i)
            {
                dst[i].x = src[i * 2];
                dst[i].y = src[i * 2 + 1];
            }

            return dst;
        }

        /// <summary>
        /// Convert double array to Point array.
        /// </summary>
        /// <param name="src">Array of double.</param>
        /// <param name="dst">Array of Point.</param>
        /// <returns>Array of Point.</returns>
        public static Point[] ConvertArrayToPointArray(double[] src, Point[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length / 2 != dst.Length)
                throw new ArgumentException("src.Length / 2 != dst.Length");

            if (dst == null)
            {
                dst = new Point[src.Length / 2];
            }

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = new Point(src[i * 2], src[i * 2 + 1]);
            }

            return dst;
        }

        /// <summary>
        /// Convert double array to Vec2d array.
        /// </summary>
        /// <param name="src">Array of double.</param>
        /// <param name="dst">Array of Vec2d.</param>
        /// <returns>Array of Vec2d.</returns>
        public static Vec2d[] ConvertArrayToVec2dArray(double[] src, Vec2d[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length / 2 != dst.Length)
                throw new ArgumentException("src.Length / 2 != dst.Length");

            if (dst == null)
            {
                dst = new Vec2d[src.Length / 2];
            }

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i].Item1 = src[i * 2];
                dst[i].Item2 = src[i * 2 + 1];
            }

            return dst;
        }

        /// <summary>
        /// Convert double array to ValueTuple array.
        /// </summary>
        /// <param name="src">Array of double.</param>
        /// <param name="dst">Array of ValueTuple.</param>
        /// <returns>Array of ValueTuple.</returns>
        public static (double x, double y)[] ConvertArrayToValueTupleArray(double[] src, (double x, double y)[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length / 2 != dst.Length)
                throw new ArgumentException("src.Length / 2 != dst.Length");

            if (dst == null)
            {
                dst = new (double x, double y)[src.Length / 2];
            }

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i].Item1 = src[i * 2];
                dst[i].Item2 = src[i * 2 + 1];
            }

            return dst;
        }

        /// <summary>
        /// Convert Vec2d array to Point list.
        /// </summary>
        /// <param name="src">Array of Vec2d.</param>
        /// <param name="dst">List of Point.</param>
        /// <returns>List of Point.</returns>
        public static List<Point> ConvertVec2dArrayToPointList(in Vec2d[] src, List<Point> dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst == null)
            {
                dst = new List<Point>();
            }

            if (dst.Count != src.Length)
            {
                dst.Clear();
                for (int i = 0; i < src.Length; i++)
                {
                    dst.Add(new Point());
                }
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i].x = src[i].Item1;
                dst[i].y = src[i].Item2;
            }

            return dst;
        }

        /// <summary>
        /// Convert Vec2d array to Point array.
        /// </summary>
        /// <param name="src">Array of Vec2d.</param>
        /// <param name="dst">Array of Point.</param>
        /// <returns>Array of Point.</returns>
        public static Point[] ConvertVec2dArrayToPointArray(in Vec2d[] src, Point[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Point[src.Length];
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i] = new Point(src[i].Item1, src[i].Item2);
            }

            return dst;
        }

        /// <summary>
        /// Convert Vec2d array to double array.
        /// </summary>
        /// <param name="src">Array of Vec2d.</param>
        /// <param name="dst">Array of double.</param>
        /// <returns>Array of double.</returns>
        public static double[] ConvertVec2dArrayToArray(in Vec2d[] src, double[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length * 2 != dst.Length)
                throw new ArgumentException("src.Count * 2 != dst.Length");

            if (dst == null)
            {
                dst = new double[src.Length * 2];
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i * 2] = src[i].Item1;
                dst[i * 2 + 1] = src[i].Item2;
            }

            return dst;
        }

        /// <summary>
        /// Convert Vec2d array to ValueTuple array.
        /// </summary>
        /// <param name="src">Array of Vec2d.</param>
        /// <param name="dst">Array of ValueTuple.</param>
        /// <returns>Array of ValueTuple.</returns>
        public static (double x, double y)[] ConvertVec2dArrayToValueTupleArray(in Vec2d[] src, (double x, double y)[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new (double x, double y)[src.Length];
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i].Item1 = src[i].Item1;
                dst[i].Item2 = src[i].Item2;
            }

            return dst;
        }

        /// <summary>
        /// Convert ValueTuple array to Point list.
        /// </summary>
        /// <param name="src">Array of ValueTuple.</param>
        /// <param name="dst">List of Point.</param>
        /// <returns>List of Point.</returns>
        public static List<Point> ConvertValueTupleArrayToPointList(in (double x, double y)[] src, List<Point> dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst == null)
            {
                dst = new List<Point>();
            }

            if (dst.Count != src.Length)
            {
                dst.Clear();
                for (int i = 0; i < src.Length; i++)
                {
                    dst.Add(new Point());
                }
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i].x = src[i].Item1;
                dst[i].y = src[i].Item2;
            }

            return dst;
        }

        /// <summary>
        /// Convert ValueTuple array to Point array.
        /// </summary>
        /// <param name="src">Array of ValueTuple.</param>
        /// <param name="dst">Array of Point.</param>
        /// <returns>Array of Point.</returns>
        public static Point[] ConvertValueTupleArrayToPointArray(in (double x, double y)[] src, Point[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Point[src.Length];
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i] = new Point(src[i].Item1, src[i].Item2);
            }

            return dst;
        }

        /// <summary>
        /// Convert ValueTuple array to double array.
        /// </summary>
        /// <param name="src">Array of ValueTuple.</param>
        /// <param name="dst">Array of double.</param>
        /// <returns>Array of double.</returns>
        public static double[] ConvertValueTupleArrayToArray(in (double x, double y)[] src, double[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length * 2 != dst.Length)
                throw new ArgumentException("src.Count * 2 != dst.Length");

            if (dst == null)
            {
                dst = new double[src.Length * 2];
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i * 2] = src[i].Item1;
                dst[i * 2 + 1] = src[i].Item2;
            }

            return dst;
        }

        /// <summary>
        /// Convert ValueTuple array to Vec2d array.
        /// </summary>
        /// <param name="src">Array of ValueTuple.</param>
        /// <param name="dst">Array of Vec2d.</param>
        /// <returns>Array of Vec2d.</returns>
        public static Vec2d[] ConvertValueTupleArrayToVec2dArray(in (double x, double y)[] src, Vec2d[] dst = null)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (dst != null && src.Length != dst.Length)
                throw new ArgumentException("src.Count != dst.Length");

            if (dst == null)
            {
                dst = new Vec2d[src.Length];
            }

            for (int i = 0; i < src.Length; ++i)
            {
                dst[i].Item1 = src[i].x;
                dst[i].Item2 = src[i].y;
            }

            return dst;
        }
    }
}
