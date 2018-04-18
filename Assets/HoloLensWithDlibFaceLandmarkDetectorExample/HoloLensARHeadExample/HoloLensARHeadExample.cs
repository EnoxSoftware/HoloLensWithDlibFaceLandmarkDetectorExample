using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using OpenCVForUnity.RectangleTrack;
using System.Threading;
using UnityEngine.EventSystems;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using Rect = OpenCVForUnity.Rect;
using DlibFaceLandmarkDetector;

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLens AR Head Example
    /// An example of AR head projection using OpenCVForUnity and DlibLandmarkDetector on Hololens.
    /// </summary>
    [RequireComponent(typeof(HololensCameraStreamToMatHelper))]
    public class HoloLensARHeadExample : MonoBehaviour
    {
        [SerializeField, HeaderAttribute ("Preview")]

        /// <summary>
        /// The preview quad.
        /// </summary>
        public GameObject previewQuad;

        /// <summary>
        /// Determines if displays the camera preview.
        /// </summary>
        public bool displayCameraPreview;

        /// <summary>
        /// The toggle for switching the camera preview display state.
        /// </summary>
        public Toggle displayCameraPreviewToggle;


        [SerializeField, HeaderAttribute ("Detection")]

        /// <summary>
        /// Determines if enables the detection.
        /// </summary>
        public bool enableDetection = true;

        /// <summary>
        /// Determines if uses separate detection.
        /// </summary>
        public bool useSeparateDetection = false;

        /// <summary>
        /// The use separate detection toggle.
        /// </summary>
        public Toggle useSeparateDetectionToggle;

        /// <summary>
        /// The min detection size ratio.
        /// </summary>
        public float minDetectionSizeRatio = 0.07f;


        [SerializeField, HeaderAttribute ("AR")]

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        /// <summary>
        /// Determines if displays axes.
        /// </summary>
        public bool displayAxes;

        /// <summary>
        /// The display axes toggle.
        /// </summary>
        public Toggle displayAxesToggle;

        /// <summary>
        /// Determines if displays head.
        /// </summary>
        public bool displayHead;

        /// <summary>
        /// Determines if displays head.
        /// </summary>
        public Toggle displayHeadToggle;

        /// <summary>
        /// Determines if displays effects.
        /// </summary>
        public bool displayEffects;

        /// <summary>
        /// The display effects toggle.
        /// </summary>
        public Toggle displayEffectsToggle;

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
        /// The AR game object.
        /// </summary>
        public GameObject arGameObject;

        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera arCamera;        

        [Space(10)]

        /// <summary>
        /// Determines if enable optical flow filter.
        /// </summary>
        public bool enableOpticalFlowFilter;

        /// <summary>
        /// The enable optical flow filter toggle.
        /// </summary>
        public Toggle enableOpticalFlowFilterToggle;

        [Space(10)]

        /// <summary>
        /// Determines if enable low pass filter.
        /// </summary>
        public bool enableLowPassFilter;

        /// <summary>
        /// The enable low pass filter toggle.
        /// </summary>
        public Toggle enableLowPassFilterToggle;

        /// <summary>
        /// The position low pass. (Value in meters)
        /// </summary>
        public float positionLowPass = 0.025f;

        /// <summary>
        /// The rotation low pass. (Value in degrees)
        /// </summary>
        public float rotationLowPass = 3f;

        /// <summary>
        /// The old pose data.
        /// </summary>
        PoseData oldPoseData;

        /// <summary>
        /// The optical flow points filter.
        /// </summary>
        OFPointsFilter opticalFlowFilter;

        /// <summary>
        /// The mouth particle system.
        /// </summary>
        ParticleSystem[] mouthParticleSystem;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The matrix that inverts the Y-axis.
        /// </summary>
        Matrix4x4 invertYM;

        /// <summary>
        /// The matrix that inverts the Z-axis.
        /// </summary>
        Matrix4x4 invertZM;

        /// <summary>
        /// The transformation matrix.
        /// </summary>
        Matrix4x4 transformationM;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

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
        /// The rot mat.
        /// </summary>
        Mat rotMat;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        HololensCameraStreamToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The detection result.
        /// </summary>
        MatOfRect detectionResult;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;


        // The camera matrix value of Hololens camera 896x504 size.
        // For details on the camera matrix, please refer to this page. (http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html)
        // These values ​​are unique to my device, obtained from the "Windows.Media.Devices.Core.CameraIntrinsics" class. (https://docs.microsoft.com/en-us/uwp/api/windows.media.devices.core.cameraintrinsics)
        // Can get these values by using this helper script. (https://github.com/EnoxSoftware/HoloLensWithOpenCVForUnityExample/tree/master/Assets/HololensCameraIntrinsicsChecker/CameraIntrinsicsCheckerHelper)
        double fx = 1035.149;//focal length x.
        double fy = 1034.633;//focal length y.
        double cx = 404.9134;//principal point x.
        double cy = 236.2834;//principal point y.
        MatOfDouble distCoeffs;
        double distCoeffs1 = 0.2036923;//radial distortion coefficient k1.
        double distCoeffs2 = -0.2035773;//radial distortion coefficient k2.
        double distCoeffs3 = 0.0;//tangential distortion coefficient p1.
        double distCoeffs4 = 0.0;//tangential distortion coefficient p2.
        double distCoeffs5 = -0.2388065;//radial distortion coefficient k3.

        Mat grayMat4Thread;
        CascadeClassifier cascade4Thread;
        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object ();

        bool _isThreadRunning = false;
        bool isThreadRunning {
            get { lock (sync)
                return _isThreadRunning; }
            set { lock (sync)
                _isThreadRunning = value; }
        }

        RectangleTracker rectangleTracker;
        float coeffTrackingWindowSize = 2.0f;
        float coeffObjectSizeToTrack = 0.85f;
        Rect[] rectsWhereRegions;
        List<Rect> detectedObjectsInRegions = new List<Rect> ();
        List<Rect> resultObjects = new List<Rect> ();

        bool _isDetecting = false;
        bool isDetecting {
            get { lock (sync)
                return _isDetecting; }
            set { lock (sync)
                _isDetecting = value; }
        }

        bool _hasUpdatedDetectionResult = false;
        bool hasUpdatedDetectionResult {
            get { lock (sync)
                return _hasUpdatedDetectionResult; }
            set { lock (sync)
                _hasUpdatedDetectionResult = value; }
        }

        // Use this for initialization
        void Start ()
        {
            displayCameraPreviewToggle.isOn = displayCameraPreview;
            useSeparateDetectionToggle.isOn = useSeparateDetection;
            displayAxesToggle.isOn = displayAxes;
            displayHeadToggle.isOn = displayHead;
            displayEffectsToggle.isOn = displayEffects;
            enableOpticalFlowFilterToggle.isOn = enableOpticalFlowFilter;
            enableLowPassFilterToggle.isOn = enableLowPassFilter;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper> ();
            webCamTextureToMatHelper = gameObject.GetComponent<HololensCameraStreamToMatHelper> ();
            #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Initialize ();

            rectangleTracker = new RectangleTracker ();
            faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("sp_human_face_68.dat"));
//            faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("sp_human_face_68_for_mobile.dat"));

            // The coordinates of the detection object on the real world space connected with the pixel coordinates.(mm)
            objectPoints = new MatOfPoint3f (
                new Point3 (-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3 (34, 90, 83),//r eye (Interpupillary breadth)
                new Point3 (0.0, 50, 120),//nose (Nose top)
                new Point3 (-26, 15, 83),//l mouse (Mouth breadth)
                new Point3 (26, 15, 83),//r mouse (Mouth breadth)
                new Point3 (-79, 90, 0.0),//l ear (Bitragion breadth)
                new Point3 (79, 90, 0.0)//r ear (Bitragion breadth)
            );

            imagePoints = new MatOfPoint2f ();
            rotMat = new Mat (3, 3, CvType.CV_64FC1);

            opticalFlowFilter = new OFPointsFilter ((int)faceLandmarkDetector.GetShapePredictorNumParts());
            opticalFlowFilter.diffDlib /= imageOptimizationHelper.downscaleRatio;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = imageOptimizationHelper.GetDownScaleMat(webCamTextureToMatHelper.GetMat ());

            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            // HololensCameraStream always returns image data in BGRA format.
            texture = new Texture2D ((int)width, (int)height, TextureFormat.BGRA32, false);
            #else
            texture = new Texture2D ((int)width, (int)height, TextureFormat.RGBA32, false);
            #endif

            previewQuad.GetComponent<MeshRenderer>().material.mainTexture = texture;
            previewQuad.transform.localScale = new Vector3 (1, height/width, 1);
            previewQuad.SetActive (displayCameraPreview);


            double fx = this.fx;
            double fy = this.fy;
            double cx = this.cx / imageOptimizationHelper.downscaleRatio;
            double cy = this.cy / imageOptimizationHelper.downscaleRatio;

            camMatrix = new Mat (3, 3, CvType.CV_64FC1);
            camMatrix.put (0, 0, fx);
            camMatrix.put (0, 1, 0);
            camMatrix.put (0, 2, cx);
            camMatrix.put (1, 0, 0);
            camMatrix.put (1, 1, fy);
            camMatrix.put (1, 2, cy);
            camMatrix.put (2, 0, 0);
            camMatrix.put (2, 1, 0);
            camMatrix.put (2, 2, 1.0f);
            Debug.Log ("camMatrix " + camMatrix.dump ());

            distCoeffs = new MatOfDouble (distCoeffs1, distCoeffs2, distCoeffs3, distCoeffs4, distCoeffs5);
            Debug.Log ("distCoeffs " + distCoeffs.dump ());

            //Calibration camera
            Size imageSize = new Size (width, height);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point (0, 0);
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


            transformationM = new Matrix4x4 ();

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


            //If WebCamera is frontFaceing,flip Mat.
            if (webCamTextureToMatHelper.GetWebCamDevice ().isFrontFacing) {
                webCamTextureToMatHelper.flipHorizontal = true;
            }

            grayMat = new Mat ();
            cascade = new CascadeClassifier ();
            cascade.load (OpenCVForUnity.Utils.getFilePath ("lbpcascade_frontalface.xml"));

            // "empty" method is not working on the UWP platform.
            //            if (cascade.empty ()) {
            //                Debug.LogError ("cascade file is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            //            }

            grayMat4Thread = new Mat ();
            cascade4Thread = new CascadeClassifier ();
            cascade4Thread.load (OpenCVForUnity.Utils.getFilePath ("haarcascade_frontalface_alt.xml"));

            // "empty" method is not working on the UWP platform.
            //            if (cascade4Thread.empty ()) {
            //                Debug.LogError ("cascade file is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            //            }

            detectionResult = new MatOfRect ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            StopThread ();
            lock (sync) {
                ExecuteOnMainThread.Clear ();
            }

            if (grayMat != null)
                grayMat.Dispose ();

            if (cascade != null)
                cascade.Dispose ();

            if (grayMat4Thread != null)
                grayMat4Thread.Dispose ();

            if (cascade4Thread != null)
                cascade4Thread.Dispose ();

            rectangleTracker.Reset ();

            camMatrix.Dispose ();
            distCoeffs.Dispose ();

            if (rvec != null) {
                rvec.Dispose ();
                rvec = null;
            }

            if (tvec != null) {
                tvec.Dispose ();
                tvec = null;
            }

            if (opticalFlowFilter != null)
                opticalFlowFilter.Dispose ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode){
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
        public void OnFrameMatAcquired (Mat bgraMat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix)
        {
            Mat downScaleFrameMat = imageOptimizationHelper.GetDownScaleMat(bgraMat);
                
            Imgproc.cvtColor (downScaleFrameMat, grayMat, Imgproc.COLOR_BGRA2GRAY);
            Imgproc.equalizeHist (grayMat, grayMat);

            if (enableDetection && !isDetecting ) {

                isDetecting = true;

                grayMat.copyTo (grayMat4Thread);

                System.Threading.Tasks.Task.Run(() => {

                    isThreadRunning = true;
                    DetectObject ();
                    isThreadRunning = false;
                    OnDetectionDone ();
                });
            }

            OpenCVForUnityUtils.SetImage (faceLandmarkDetector, grayMat);

            Mat bgraMat4preview = null;
            if (displayCameraPreview) {
                bgraMat4preview = new Mat ();
                downScaleFrameMat.copyTo (bgraMat4preview);
            }

            List<Vector2> points = null;
            Rect[] rects;
            if (!useSeparateDetection) {
                if (hasUpdatedDetectionResult) {
                    hasUpdatedDetectionResult = false;

                    lock (rectangleTracker) {
                        rectangleTracker.UpdateTrackedObjects (detectionResult.toList ());
                    }
                }

                lock (rectangleTracker) {
                    rectangleTracker.GetObjects (resultObjects, true);
                }
                rects = resultObjects.ToArray ();

                if(rects.Length > 0){

                    OpenCVForUnity.Rect rect = rects [0];

                    // Adjust to Dilb's result.
                    rect.y += (int)(rect.height * 0.1f);

                    //detect landmark points
                    points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                    if (enableOpticalFlowFilter) {
                        opticalFlowFilter.Process (bgraMat, points, points, false);
                    }

                    if (displayCameraPreview && bgraMat4preview != null) {
                        //draw landmark points
                        OpenCVForUnityUtils.DrawFaceLandmark (bgraMat4preview, points, new Scalar (0, 255, 0, 255), 2);
                    }
                }

            }else {

                if (hasUpdatedDetectionResult) {
                    hasUpdatedDetectionResult = false;

                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                    //}, true);

                    lock (rectangleTracker) {
                        rectsWhereRegions = detectionResult.toArray ();
                    }

                } else {
                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log("process: get rectsWhereRegions from previous positions");
                    //}, true);

                    lock (rectangleTracker) {
                        rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects ();
                    }                        
                }

                detectedObjectsInRegions.Clear ();
                if (rectsWhereRegions.Length > 0) {
                    int len = rectsWhereRegions.Length;
                    for (int i = 0; i < len; i++) {
                        DetectInRegion (grayMat, rectsWhereRegions [i], detectedObjectsInRegions);
                    }
                }                

                lock (rectangleTracker) {
                    rectangleTracker.UpdateTrackedObjects (detectedObjectsInRegions);
                    rectangleTracker.GetObjects (resultObjects, true);
                }

                if(resultObjects.Count > 0) {

                    OpenCVForUnity.Rect rect = resultObjects [0];

                    // Adjust to Dilb's result.
                    rect.y += (int)(rect.height * 0.1f);

                    //detect landmark points
                    points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                    if (enableOpticalFlowFilter) {
                        opticalFlowFilter.Process (bgraMat, points, points, false);
                    }

                    if (displayCameraPreview && bgraMat4preview != null) {
                        //draw landmark points
                        OpenCVForUnityUtils.DrawFaceLandmark (bgraMat4preview, points, new Scalar (0, 255, 0, 255), 2);
                    }
                }
            }
                

            UnityEngine.WSA.Application.InvokeOnAppThread(() => {

                if (!webCamTextureToMatHelper.IsPlaying ()) return;

                if (displayCameraPreview && bgraMat4preview != null) {
                        OpenCVForUnity.Utils.fastMatToTexture2D(bgraMat4preview, texture);
                }

                if (points != null){
                    UpdateARHeadTransform (points, cameraToWorldMatrix);
                }

                bgraMat.Dispose ();
                if (bgraMat4preview != null){
                    bgraMat4preview.Dispose();
                }

            }, false);
        }

        #else

        // Update is called once per frame
        void Update ()
        {
            lock (sync) {
                while (ExecuteOnMainThread.Count > 0) {
                    ExecuteOnMainThread.Dequeue ().Invoke ();
                }
            }

            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = imageOptimizationHelper.GetDownScaleMat(webCamTextureToMatHelper.GetMat ());

                Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist (grayMat, grayMat);

                if (enableDetection && !isDetecting ) {
                    isDetecting = true;

                    grayMat.copyTo (grayMat4Thread);

                    StartThread (ThreadWorker);
                }

                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, grayMat);

                Rect[] rects;
                if (!useSeparateDetection) {
                    if (hasUpdatedDetectionResult) 
                    {
                        hasUpdatedDetectionResult = false;

                        rectangleTracker.UpdateTrackedObjects (detectionResult.toList());
                    }

                    rectangleTracker.GetObjects (resultObjects, true);

                    rects = rectangleTracker.CreateCorrectionBySpeedOfRects ();

                    if(rects.Length > 0){

                        OpenCVForUnity.Rect rect = rects [0];

                        // Adjust to Dilb's result.
                        rect.y += (int)(rect.height * 0.1f);

                        //detect landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                        if (enableOpticalFlowFilter) {
                            opticalFlowFilter.Process (rgbaMat, points, points, false);
                        }

                        UpdateARHeadTransform (points, arCamera.cameraToWorldMatrix);

                        if (displayCameraPreview) {
                            //draw landmark points
                            OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, points, new Scalar (0, 255, 0, 255), 2);
                        }
                    }

                } else {

                    if (hasUpdatedDetectionResult) {
                        hasUpdatedDetectionResult = false;

                        //Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                        rectsWhereRegions = detectionResult.toArray ();
                    } else {
                        //Debug.Log("process: get rectsWhereRegions from previous positions");
                        rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects ();
                    }

                    detectedObjectsInRegions.Clear ();
                    if (rectsWhereRegions.Length > 0) {
                        int len = rectsWhereRegions.Length;
                        for (int i = 0; i < len; i++) {
                            DetectInRegion (grayMat, rectsWhereRegions [i], detectedObjectsInRegions);
                        }
                    }

                    rectangleTracker.UpdateTrackedObjects (detectedObjectsInRegions);
                    rectangleTracker.GetObjects (resultObjects, true);

                    if(resultObjects.Count > 0) {

                        OpenCVForUnity.Rect rect = resultObjects [0];

                        // Adjust to Dilb's result.
                        rect.y += (int)(rect.height * 0.1f);

                        //detect landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                        if (enableOpticalFlowFilter) {
                            opticalFlowFilter.Process (rgbaMat, points, points, false);
                        }

                        UpdateARHeadTransform (points, arCamera.cameraToWorldMatrix);

                        if (displayCameraPreview) {
                            //draw landmark points
                            OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, points, new Scalar (0, 255, 0, 255), 2);
                        }
                    }
                }

                if (displayCameraPreview) {
                    OpenCVForUnity.Utils.fastMatToTexture2D (rgbaMat, texture);
                }
            }
        }
        #endif

        private void UpdateARHeadTransform(List<Vector2> points, Matrix4x4 cameraToWorldMatrix)
        {
            // The coordinates in pixels of the object detected on the image projected onto the plane.
            imagePoints.fromArray (
                new Point ((points [38].x + points [41].x) / 2, (points [38].y + points [41].y) / 2),//l eye (Interpupillary breadth)
                new Point ((points [43].x + points [46].x) / 2, (points [43].y + points [46].y) / 2),//r eye (Interpupillary breadth)
                new Point (points [30].x, points [30].y),//nose (Nose top)
                new Point (points [48].x, points [48].y),//l mouth (Mouth breadth)
                new Point (points [54].x, points [54].y),//r mouth (Mouth breadth)
                new Point (points [0].x, points [0].y),//l ear (Bitragion breadth)
                new Point (points [16].x, points [16].y)//r ear (Bitragion breadth)
            );
                
            // Estimate head pose.
            if (rvec == null || tvec == null) {
                rvec = new Mat (3, 1, CvType.CV_64FC1);
                tvec = new Mat (3, 1, CvType.CV_64FC1);
                Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
            }

            double tvec_z = tvec.get (2, 0) [0];

            if (double.IsNaN(tvec_z) || tvec_z < 0) { // if tvec is wrong data, do not use extrinsic guesses.
                Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
            }else{
                Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec, true, Calib3d.SOLVEPNP_ITERATIVE);
            }
                
            if (applyEstimationPose && !double.IsNaN(tvec_z)) {

                if (Mathf.Abs ((float)(points [43].y - points [46].y)) > Mathf.Abs ((float)(points [42].x - points [45].x)) / 5.0) {
                    if (displayEffects)
                        rightEye.SetActive (true);
                } else {
                    if (displayEffects)
                        rightEye.SetActive (false);
                }

                if (Mathf.Abs ((float)(points [38].y - points [41].y)) > Mathf.Abs ((float)(points [39].x - points [36].x)) / 5.0) {
                    if (displayEffects)
                        leftEye.SetActive (true);
                } else {
                    if (displayEffects)
                        leftEye.SetActive (false);
                }

                if (displayHead)
                    head.SetActive (true);
                if (displayAxes)
                    axes.SetActive (true);


                float noseDistance = Mathf.Abs ((float)(points [27].y - points [33].y));
                float mouseDistance = Mathf.Abs ((float)(points [62].y - points [66].y));
                if (mouseDistance > noseDistance / 5.0) {
                    if (displayEffects) {
                        mouth.SetActive (true);
                        foreach (ParticleSystem ps in mouthParticleSystem) {
                            var em = ps.emission;
                            em.enabled = true;
                            #if UNITY_5_5_OR_NEWER
                            var main = ps.main;
                            main.startSizeMultiplier = 40 * (mouseDistance / noseDistance);
                            #else
                            ps.startSize = 40 * (mouseDistance / noseDistance);
                            #endif
                        }
                    }
                } else {
                    if (displayEffects) {
                        foreach (ParticleSystem ps in mouthParticleSystem) {
                            var em = ps.emission;
                            em.enabled = false;
                        }
                    }
                }
                    
                // Convert to unity pose data.
                double[] rvecArr = new double[3];
                rvec.get (0, 0, rvecArr);
                double[] tvecArr = new double[3];
                tvec.get (0, 0, tvecArr);
                tvecArr [0] = tvecArr [0] / 1000.0;
                tvecArr[1] = tvecArr[1] / 1000.0;
                tvecArr[2] = tvecArr[2] / 1000.0 / imageOptimizationHelper.downscaleRatio;
                PoseData poseData = ARUtils.ConvertRvecTvecToPoseData (rvecArr, tvecArr);

                // Changes in pos/rot below these thresholds are ignored.
                if (enableLowPassFilter) {
                    ARUtils.LowpassPoseData (ref oldPoseData, ref poseData, positionLowPass, rotationLowPass);
                }
                oldPoseData = poseData;

                // Create transform matrix.
                transformationM = Matrix4x4.TRS (poseData.pos, poseData.rot, Vector3.one);

                // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                ARM = invertYM * transformationM;

                // Apply Z-axis inverted matrix.
                ARM = ARM * invertZM;

                // Apply the cameraToWorld matrix with the Z-axis inverted.
                ARM = cameraToWorldMatrix * invertZM * ARM;

                ARUtils.SetTransformFromMatrix (arGameObject.transform, ref ARM);
            }
        }

        private void StartThread(Action action)
        {
            #if UNITY_METRO && NETFX_CORE
            System.Threading.Tasks.Task.Run(() => action());
            #elif UNITY_METRO
            action.BeginInvoke(ar => action.EndInvoke(ar), null);
            #else
            ThreadPool.QueueUserWorkItem (_ => action());
            #endif
        }

        private void StopThread ()
        {
            if (!isThreadRunning)
                return;

            while (isThreadRunning) {
                //Wait threading stop
            } 
        }

        private void ThreadWorker()
        {
            isThreadRunning = true;

            DetectObject ();

            lock (sync) {
                if (ExecuteOnMainThread.Count == 0) {
                    ExecuteOnMainThread.Enqueue (() => {
                        OnDetectionDone ();
                    });
                }
            }

            isThreadRunning = false;
        }

        private void DetectObject()
        {
            MatOfRect objects = new MatOfRect ();
            if (cascade4Thread != null)
                cascade4Thread.detectMultiScale (grayMat, objects, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                    new Size (grayMat.cols () * minDetectionSizeRatio, grayMat.rows () * minDetectionSizeRatio), new Size ());

            detectionResult = objects;
        }

        private void OnDetectionDone()
        {
            hasUpdatedDetectionResult = true;

            isDetecting = false;
        }

        private void DetectInRegion (Mat img, Rect r, List<Rect> detectedObjectsInRegions)
        {
            Rect r0 = new Rect (new Point (), img.size ());
            Rect r1 = new Rect (r.x, r.y, r.width, r.height);
            Rect.inflate (r1, (int)((r1.width * coeffTrackingWindowSize) - r1.width) / 2,
                (int)((r1.height * coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect (r0, r1);

            if ((r1.width <= 0) || (r1.height <= 0)) {
                Debug.Log ("DetectionBasedTracker::detectInRegion: Empty intersection");
                return;
            }

            int d = Math.Min (r.width, r.height);
            d = (int)Math.Round (d * coeffObjectSizeToTrack);

            MatOfRect tmpobjects = new MatOfRect ();

            Mat img1 = new Mat (img, r1);//subimage for rectangle -- without data copying

            cascade.detectMultiScale (img1, tmpobjects, 1.1, 2, 0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE | Objdetect.CASCADE_FIND_BIGGEST_OBJECT, new Size (d, d), new Size ());


            Rect[] tmpobjectsArray = tmpobjects.toArray ();
            int len = tmpobjectsArray.Length;
            for (int i = 0; i < len; i++) {
                Rect tmp = tmpobjectsArray [i];
                Rect curres = new Rect (new Point (tmp.x + r1.x, tmp.y + r1.y), tmp.size ());
                detectedObjectsInRegions.Add (curres);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            imageOptimizationHelper.Dispose ();
            #if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (rectangleTracker != null)
                rectangleTracker.Dispose ();

            if (rotMat != null)
                rotMat.Dispose ();
        }

        /// <summary>
        /// Raises the back button clicked click event.
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
        /// Raises the play button clicked click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button clicked click click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button clicked click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button clicked click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
        }

        /// <summary>
        /// Raises the display camera preview toggle value changed event.
        /// </summary>
        public void OnDisplayCamreaPreviewToggleValueChanged ()
        {
            if (displayCameraPreviewToggle.isOn) {
                displayCameraPreview = true;
            } else {
                displayCameraPreview = false;
            }
            previewQuad.SetActive (displayCameraPreview);
        }

        /// <summary>
        /// Raises the use separate detection toggle value changed event.
        /// </summary>
        public void OnUseSeparateDetectionToggleValueChanged ()
        {
            if (useSeparateDetectionToggle.isOn) {
                useSeparateDetection = true;
            } else {
                useSeparateDetection = false;
            }

            lock (rectangleTracker) {
                if (rectangleTracker != null)
                    rectangleTracker.Reset ();
            }
        }

        /// <summary>
        /// Raises the display axes toggle value changed event.
        /// </summary>
        public void OnDisplayAxesToggleValueChanged ()
        {
            if (displayAxesToggle.isOn) {
                displayAxes = true;
            } else {
                displayAxes = false;
                axes.SetActive (false);
            }
        }

        /// <summary>
        /// Raises the display head toggle value changed event.
        /// </summary>
        public void OnDisplayHeadToggleValueChanged ()
        {
            if (displayHeadToggle.isOn) {
                displayHead = true;
            } else {
                displayHead = false;
                head.SetActive (false);
            }
        }

        /// <summary>
        /// Raises the display effects toggle value changed event.
        /// </summary>
        public void OnDisplayEffectsToggleValueChanged ()
        {
            if (displayEffectsToggle.isOn) {
                displayEffects = true;
            } else {
                displayEffects = false;
                rightEye.SetActive (false);
                leftEye.SetActive (false);
                mouth.SetActive (false);
            }
        }

        /// <summary>
        /// Raises the enable optical flow filter toggle value changed event.
        /// </summary>
        public void OnEnableOpticalFlowFilterToggleValueChanged ()
        {
            if (enableOpticalFlowFilterToggle.isOn) {
                enableOpticalFlowFilter = true;
            } else {
                enableOpticalFlowFilter = false;
            }
        }

        /// <summary>
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged ()
        {
            if (enableLowPassFilterToggle.isOn) {
                enableLowPassFilter = true;
                if (opticalFlowFilter != null)
                    opticalFlowFilter.Reset ();
            } else {
                enableLowPassFilter = false;
            }
        }

        /// <summary>
        /// Raises the tapped event.
        /// </summary>
        public void OnTapped ()
        {
            if (EventSystem.current.IsPointerOverGameObject ())
                return;

            if (applyEstimationPose) {
                applyEstimationPose = false;
                head.GetComponent<MeshRenderer> ().material.color = Color.gray;
            } else {
                applyEstimationPose = true;
                head.GetComponent<MeshRenderer> ().material.color = Color.red;

                // resets extrinsic guess.
                rvec = null;
                tvec = null;
            }
        }
    }
}