using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLens WebCamTexture (and face landmark detection) example.
    /// </summary>
    [RequireComponent (typeof(OptimizationWebCamTextureToMatHelper))]
    public class HoloLensWebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if use dlib face detector.
        /// </summary>
        public bool useDlibFaceDetecter = false;

        /// <summary>
        /// The use dlib face detecter toggle.
        /// </summary>
        public Toggle useDlibFaceDetecterToggle;

        /// <summary>
        /// The processing area mat.
        /// </summary>
        Mat processingAreaMat;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The faces.
        /// </summary>
        MatOfRect faces;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        OptimizationWebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;


        Renderer quad_renderer = null;

        OpenCVForUnity.Rect processingAreaRect;
        public Vector2 outsideClippingRatio = new Vector2 (0.17f, 0.19f);
        public Vector2 clippingOffset = new Vector2 (0.043f, -0.041f);
        public float vignetteScale = 1.8f;

        // Debug
        //        public Vector2 outsideClippingRatio = new Vector2(0.0f, 0.0f);
        //        public Vector2 clippingOffset = new Vector2(0.0f, 0.0f);
        //        public float vignetteScale = 0.3f;


        // Use this for initialization
        void Start ()
        {
            useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;

            cascade = new CascadeClassifier (OpenCVForUnity.Utils.getFilePath ("haarcascade_frontalface_alt.xml"));

            faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat"));

            webCamTextureToMatHelper = gameObject.GetComponent<OptimizationWebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Initialize ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
        
            Mat webCamTextureMat = webCamTextureToMatHelper.GetDownScaleMat (webCamTextureToMatHelper.GetMat ());
        
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
            Debug.Log ("webCamTextureMat.width " + webCamTextureMat.width () + " webCamTextureMat.height " + webCamTextureMat.height ());


            processingAreaRect = new OpenCVForUnity.Rect ((int)(webCamTextureMat.cols () * (outsideClippingRatio.x - clippingOffset.x)), (int)(webCamTextureMat.rows () * (outsideClippingRatio.y + clippingOffset.y)),
                (int)(webCamTextureMat.cols () * (1f - outsideClippingRatio.x * 2)), (int)(webCamTextureMat.rows () * (1f - outsideClippingRatio.y * 2)));
            processingAreaRect = processingAreaRect.intersect (new OpenCVForUnity.Rect (0, 0, webCamTextureMat.cols (), webCamTextureMat.rows ()));

            Debug.Log ("webCamTextureMat.width " + webCamTextureMat.width () + " webCamTextureMat.height " + webCamTextureMat.height ());
            Debug.Log ("processingAreaRect.x " + processingAreaRect.x + " processingAreaRect.y " + processingAreaRect.y + " processingAreaRect.width " + processingAreaRect.width + " processingAreaRect.height " + processingAreaRect.height);


            processingAreaMat = new Mat (processingAreaRect.height, processingAreaRect.width, CvType.CV_8UC4);

            grayMat = new Mat (processingAreaMat.rows (), processingAreaMat.cols (), CvType.CV_8UC1);

            faces = new MatOfRect ();


            quad_renderer = gameObject.GetComponent<Renderer> () as Renderer;
            quad_renderer.sharedMaterial.SetTexture ("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector ("_VignetteOffset", new Vector4 (clippingOffset.x, clippingOffset.y));

            //This value is obtained from PhotoCapture's TryGetProjectionMatrix() method.I do not know whether this method is good.
            //Please see the discussion of this thread.Https://forums.hololens.com/discussion/782/live-stream-of-locatable-camera-webcam-in-unity
            Matrix4x4 projectionMatrix = Matrix4x4.identity;
            projectionMatrix.m00 = 2.31029f;
            projectionMatrix.m01 = 0.00000f;
            projectionMatrix.m02 = 0.09614f;
            projectionMatrix.m03 = 0.00000f;
            projectionMatrix.m10 = 0.00000f;
            projectionMatrix.m11 = 4.10427f;
            projectionMatrix.m12 = -0.06231f;
            projectionMatrix.m13 = 0.00000f;
            projectionMatrix.m20 = 0.00000f;
            projectionMatrix.m21 = 0.00000f;
            projectionMatrix.m22 = -1.00000f;
            projectionMatrix.m23 = 0.00000f;
            projectionMatrix.m30 = 0.00000f;
            projectionMatrix.m31 = 0.00000f;
            projectionMatrix.m32 = -1.00000f;
            projectionMatrix.m33 = 0.00000f;
            quad_renderer.sharedMaterial.SetMatrix ("_CameraProjectionMatrix", projectionMatrix);
            quad_renderer.sharedMaterial.SetFloat ("_VignetteScale", vignetteScale);


            float halfOfVerticalFov = Mathf.Atan (1.0f / projectionMatrix.m11);
            float aspectRatio = (1.0f / Mathf.Tan (halfOfVerticalFov)) / projectionMatrix.m00;
            Debug.Log ("halfOfVerticalFov " + halfOfVerticalFov);
            Debug.Log ("aspectRatio " + aspectRatio);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            processingAreaMat.Dispose ();
            grayMat.Dispose ();
            faces.Dispose ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
            
                Mat rgbaMat = webCamTextureToMatHelper.GetDownScaleMat (webCamTextureToMatHelper.GetMat ());


                Mat rgbaMatClipROI = new Mat (rgbaMat, processingAreaRect);

                rgbaMatClipROI.copyTo (processingAreaMat);


                // fill all black.
                Imgproc.rectangle (rgbaMat, new Point (0, 0), new Point (rgbaMat.width (), rgbaMat.height ()), new Scalar (0, 0, 0, 0), -1);


                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, processingAreaMat);

                // detect faces.
                List<OpenCVForUnity.Rect> detectResult = new List<OpenCVForUnity.Rect> ();
                if (useDlibFaceDetecter) {
                    
                    List<UnityEngine.Rect> result = faceLandmarkDetector.Detect ();

                    foreach (var unityRect in result) {
                        detectResult.Add (new OpenCVForUnity.Rect ((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
                    }
                } else {
                    // convert image to greyscale.
                    Imgproc.cvtColor (processingAreaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


                    Imgproc.equalizeHist (grayMat, grayMat);

                    cascade.detectMultiScale (grayMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size (grayMat.cols () * 0.15, grayMat.cols () * 0.15), new Size ());

                    detectResult = faces.toList ();


                    // Adjust to Dilb's result.
                    foreach (OpenCVForUnity.Rect r in detectResult) {
                        r.y += (int)(r.height * 0.1f);
                    }
                }


                foreach (var rect in detectResult) {

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                    //draw landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark (rgbaMatClipROI, points, new Scalar (0, 255, 0, 255), 2);

                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect (rgbaMatClipROI, new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height), new Scalar (255, 0, 0, 255), 2);
                }

                Imgproc.putText (rgbaMatClipROI, "W:" + rgbaMatClipROI.width () + " H:" + rgbaMatClipROI.height () + " SO:" + Screen.orientation, new Point (5, rgbaMatClipROI.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 0, 0, 255), 1, Imgproc.LINE_AA, false);


                Imgproc.rectangle (rgbaMat, new Point (0, 0), new Point (rgbaMat.width (), rgbaMat.height ()), new Scalar (255, 0, 0, 255), 2);

                // Draw prosessing area rectangle.
                Imgproc.rectangle (rgbaMat, processingAreaRect.tl (), processingAreaRect.br (), new Scalar (255, 255, 0, 255), 2);

                OpenCVForUnity.Utils.fastMatToTexture2D (rgbaMat, texture);

                rgbaMatClipROI.Dispose ();
            }

            if (webCamTextureToMatHelper.IsPlaying ()) {

                Matrix4x4 cameraToWorldMatrix = Camera.main.cameraToWorldMatrix;
                ;
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                texture.wrapMode = TextureWrapMode.Clamp;

                quad_renderer.sharedMaterial.SetMatrix ("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn (3) - cameraToWorldMatrix.GetColumn (2);

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation (-cameraToWorldMatrix.GetColumn (2), cameraToWorldMatrix.GetColumn (1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }
        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (cascade != null)
                cascade.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoloLensWithDlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("HoloLensWithDlibFaceLandmarkDetectorExample");
            #endif
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        /// <summary>
        /// Raises the use Dlib face detector toggle value changed event.
        /// </summary>
        public void OnUseDlibFaceDetecterToggleValueChanged ()
        {
            if (useDlibFaceDetecterToggle.isOn) {
                useDlibFaceDetecter = true;
            } else {
                useDlibFaceDetecter = false;
            }
        }
    }
}