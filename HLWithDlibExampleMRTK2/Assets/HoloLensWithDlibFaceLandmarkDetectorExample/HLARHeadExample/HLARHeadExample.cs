using System;
using System.Collections.Generic;
using System.Threading;
using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using HoloLensCameraStream;
using HoloLensWithDlibFaceLandmarkDetectorExample.RectangleTrack;
using HoloLensWithOpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.OpenCVARUtils;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLens AR Head Example
    /// An example of AR head projection using OpenCVForUnity and DlibLandmarkDetector on Hololens.
    /// </summary>
    [RequireComponent(typeof(HLCameraStream2MatHelper), typeof(ImageOptimizationHelper))]
    public class HLARHeadExample : MonoBehaviour
    {
        [SerializeField, HeaderAttribute("Preview")]

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


        [SerializeField, HeaderAttribute("Detection")]

        /// <summary>
        /// Determines if enables the detection.
        /// </summary>
        public bool enableDetection = true;

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle enableDownScaleToggle;

        /// <summary>
        /// Determines if uses separate detection.
        /// </summary>
        public bool useSeparateDetection = false;

        /// <summary>
        /// The use separate detection toggle.
        /// </summary>
        public Toggle useSeparateDetectionToggle;

        /// <summary>
        /// Determines if use OpenCV FaceDetector for face detection.
        /// </summary>
        public bool useOpenCVDetector;

        /// <summary>
        /// The use OpenCV FaceDetector toggle.
        /// </summary>
        public Toggle useOpenCVDetectorToggle;

        /// <summary>
        /// The min detection size ratio.
        /// </summary>
        public float minDetectionSizeRatio = 0.07f;


        [SerializeField, HeaderAttribute("AR")]

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
        public ARGameObject arGameObject;

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
        /// Determines if enable lerp filter.
        /// </summary>
        public bool enableLerpFilter;

        /// <summary>
        /// The enable lerp filter toggle.
        /// </summary>
        public Toggle enableLerpFilterToggle;

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
        /// The distCoeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The matrix that inverts the Y-axis.
        /// </summary>
        Matrix4x4 invertYM;

        /// <summary>
        /// The matrix that inverts the Z-axis.
        /// </summary>
        Matrix4x4 invertZM;

        /// <summary>
        /// The matrix that AR camera P * V.
        /// </summary>
        Matrix4x4 VP;

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
        MatOfPoint3f objectPoints68;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints17;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints6;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints5;

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
        /// The webcam texture to mat helper.
        /// </summary>
        HLCameraStream2MatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

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
        List<Rect> detectionResult = new List<Rect>();

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        Scalar COLOR_WHITE = new Scalar(255, 255, 255, 255);
        Scalar COLOR_GRAY = new Scalar(128, 128, 128, 255);

        Mat grayMat4Thread;
        CascadeClassifier cascade4Thread;
        FaceLandmarkDetector faceLandmarkDetector4Thread;
        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

        bool _isThreadRunning = false;
        bool isThreadRunning
        {
            get
            {
                lock (sync)
                    return _isThreadRunning;
            }
            set
            {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        RectangleTracker rectangleTracker;
        float coeffTrackingWindowSize = 2.0f;
        float coeffObjectSizeToTrack = 0.85f;
        List<Rect> detectedObjectsInRegions = new List<Rect>();
        List<Rect> resultObjects = new List<Rect>();

        bool _isDetecting = false;
        bool isDetecting
        {
            get
            {
                lock (sync)
                    return _isDetecting;
            }
            set
            {
                lock (sync)
                    _isDetecting = value;
            }
        }

        bool _hasUpdatedDetectionResult = false;
        bool hasUpdatedDetectionResult
        {
            get
            {
                lock (sync)
                    return _hasUpdatedDetectionResult;
            }
            set
            {
                lock (sync)
                    _hasUpdatedDetectionResult = value;
            }
        }

        bool _isDetectingInFrameArrivedThread = false;
        bool isDetectingInFrameArrivedThread
        {
            get
            {
                lock (sync)
                    return _isDetectingInFrameArrivedThread;
            }
            set
            {
                lock (sync)
                    _isDetectingInFrameArrivedThread = value;
            }
        }


        [HeaderAttribute("Debug")]

        public Text renderFPS;
        public Text videoFPS;
        public Text trackFPS;
        public Text debugStr;


        string cascade_filepath;
        string cascade4Thread_filepath;
        string dlibShapePredictor_filepath;
        string dlibShapePredictor4Thread_filepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            displayCameraPreviewToggle.isOn = displayCameraPreview;
            enableDownScaleToggle.isOn = enableDownScale;
            useSeparateDetectionToggle.isOn = useSeparateDetection;
            useOpenCVDetectorToggle.isOn = useOpenCVDetector;
            displayAxesToggle.isOn = displayAxes;
            displayHeadToggle.isOn = displayHead;
            displayEffectsToggle.isOn = displayEffects;
            enableOpticalFlowFilterToggle.isOn = enableOpticalFlowFilter;
            enableLerpFilterToggle.isOn = enableLerpFilter;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<HLCameraStream2MatHelper>();
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.FrameMatAcquired += OnFrameMatAcquired;
#endif

            rectangleTracker = new RectangleTracker();


            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (debugStr != null)
            {
                debugStr.text = "Preparing file access...";
            }

            cascade_filepath = await DlibEnv.GetFilePathTaskAsync("OpenCVForUnityExample/objdetect/lbpcascade_frontalface.xml", cancellationToken: cts.Token);
            //cascade4Thread_filepath = await DlibEnv.GetFilePathTaskAsync("OpenCVForUnityExample/objdetect/haarcascade_frontalface_alt.xml", cancellationToken: cts.Token);
            cascade4Thread_filepath = await DlibEnv.GetFilePathTaskAsync("OpenCVForUnityExample/objdetect/lbpcascade_frontalface.xml", cancellationToken: cts.Token);
            dlibShapePredictorFileName = HoloLensWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            dlibShapePredictor_filepath = await DlibEnv.GetFilePathTaskAsync(dlibShapePredictorFileName, cancellationToken: cts.Token);
            dlibShapePredictor4Thread_filepath = await DlibEnv.GetFilePathTaskAsync("DlibFaceLandmarkDetector/sp_human_face_6.dat", cancellationToken: cts.Token);

            if (debugStr != null)
            {
                debugStr.text = "";
            }

            Run();
        }

        // Use this for initialization
        void Run()
        {
            cascade = new CascadeClassifier();
            cascade.load(cascade_filepath);
#if !WINDOWS_UWP || UNITY_EDITOR
            // "empty" method is not working on the UWP platform.
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/objdetect/” to “Assets/StreamingAssets/OpenCVForUnity/objdetect/” folder. ");
            }
#endif

            cascade4Thread = new CascadeClassifier();
            cascade4Thread.load(cascade4Thread_filepath);
#if !WINDOWS_UWP || UNITY_EDITOR
            // "empty" method is not working on the UWP platform.
            if (cascade4Thread.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/objdetect/” to “Assets/StreamingAssets/OpenCVForUnity/objdetect/” folder. ");
            }
#endif

            if (string.IsNullOrEmpty(dlibShapePredictor_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictor_filepath);

            if (string.IsNullOrEmpty(dlibShapePredictor4Thread_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector4Thread = new FaceLandmarkDetector(dlibShapePredictor4Thread_filepath);


            // set 3d face object points. (right-handed coordinates system)
            objectPoints68 = new MatOfPoint3f(
                new Point3(-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3(34, 90, 83),//r eye (Interpupillary breadth)
                new Point3(0.0, 50, 117),//nose (Tip)
                new Point3(0.0, 32, 97),//nose (Subnasale)
                new Point3(-79, 90, 10),//l ear (Bitragion breadth)
                new Point3(79, 90, 10)//r ear (Bitragion breadth)
            );

            objectPoints17 = new MatOfPoint3f(
                new Point3(-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3(34, 90, 83),//r eye (Interpupillary breadth)
                new Point3(0.0, 50, 117),//nose (Tip)
                new Point3(0.0, 32, 97),//nose (Subnasale)
                new Point3(-79, 90, 10),//l ear (Bitragion breadth)
                new Point3(79, 90, 10)//r ear (Bitragion breadth)
            );

            objectPoints6 = new MatOfPoint3f(
                new Point3(-34, 90, 83),//l eye (Interpupillary breadth)
                new Point3(34, 90, 83),//r eye (Interpupillary breadth)
                new Point3(0.0, 50, 117),//nose (Tip)
                new Point3(0.0, 32, 97)//nose (Subnasale)
            );

            objectPoints5 = new MatOfPoint3f(
                new Point3(-23, 90, 83),//l eye (Inner corner of the eye)
                new Point3(23, 90, 83),//r eye (Inner corner of the eye)
                new Point3(-50, 90, 80),//l eye (Tail of the eye)
                new Point3(50, 90, 80),//r eye (Tail of the eye)
                new Point3(0.0, 32, 97)//nose (Subnasale)
            );

            // adjust object points to the scale of real world space.
            AjustPointScale(objectPoints68, 0.001);
            AjustPointScale(objectPoints17, 0.001);
            AjustPointScale(objectPoints6, 0.001);
            AjustPointScale(objectPoints5, 0.001);

            imagePoints = new MatOfPoint2f();

            opticalFlowFilter = new OFPointsFilter((int)faceLandmarkDetector.GetShapePredictorNumParts());
            opticalFlowFilter.DiffCheckSensitivity /= imageOptimizationHelper.DownscaleRatio;

            webCamTextureToMatHelper.OutputColorFormat = Source2MatHelperColorFormat.GRAY;
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat grayMat = webCamTextureToMatHelper.GetMat();

            float width = grayMat.width();
            float height = grayMat.height();

            texture = new Texture2D((int)width, (int)height, TextureFormat.Alpha8, false);
            previewQuad.GetComponent<MeshRenderer>().material.mainTexture = texture;
            previewQuad.transform.localScale = new Vector3(0.2f * width / height, 0.2f, 1);
            previewQuad.SetActive(displayCameraPreview);


            //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            DebugUtils.AddDebugStr(webCamTextureToMatHelper.GetWidth() + " x " + webCamTextureToMatHelper.GetHeight() + " : " + webCamTextureToMatHelper.GetFPS());
            if (enableDownScale)
                DebugUtils.AddDebugStr("enableDownScale = true: " + imageOptimizationHelper.DownscaleRatio + " / " + webCamTextureToMatHelper.GetWidth() / imageOptimizationHelper.DownscaleRatio + " x " + webCamTextureToMatHelper.GetHeight() / imageOptimizationHelper.DownscaleRatio);


#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

            CameraIntrinsics cameraIntrinsics = webCamTextureToMatHelper.GetCameraIntrinsics();

            camMatrix = CreateCameraMatrix(cameraIntrinsics.FocalLengthX, cameraIntrinsics.FocalLengthY, cameraIntrinsics.PrincipalPointX, cameraIntrinsics.PrincipalPointY);
            distCoeffs = new MatOfDouble(cameraIntrinsics.RadialDistK1, cameraIntrinsics.RadialDistK2, cameraIntrinsics.RadialDistK3, cameraIntrinsics.TangentialDistP1, cameraIntrinsics.TangentialDistP2);

            Debug.Log("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");

            DebugUtils.AddDebugStr("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");

            // create AR camera P * V Matrix
            Matrix4x4 P = OpenCVARUtils.CalculateProjectionMatrixFromCameraMatrixValues((float)cameraIntrinsics.FocalLengthX, (float)cameraIntrinsics.FocalLengthY, (float)cameraIntrinsics.PrincipalPointX, (float)cameraIntrinsics.PrincipalPointY, width, height, 0.3f, 5f);
            Matrix4x4 V = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            VP = P * V;
#else

            // The camera matrix value of Hololens camera 896x504 size.
            // For details on the camera matrix, please refer to this page. (http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html)
            // These values ​​are unique to my device, obtained from the "Windows.Media.Devices.Core.CameraIntrinsics" class. (https://docs.microsoft.com/en-us/uwp/api/windows.media.devices.core.cameraintrinsics)
            // Can get these values by using this helper script. (https://github.com/EnoxSoftware/HoloLensWithOpenCVForUnityExample/tree/master/Assets/HololensCameraIntrinsicsChecker/CameraIntrinsicsCheckerHelper)
            double fx = 1035.149;//focal length x.
            double fy = 1034.633;//focal length y.
            double cx = 404.9134;//principal point x.
            double cy = 236.2834;//principal point y.
            double distCoeffs1 = 0.2036923;//radial distortion coefficient k1.
            double distCoeffs2 = -0.2035773;//radial distortion coefficient k2.
            double distCoeffs3 = 0.0;//tangential distortion coefficient p1.
            double distCoeffs4 = 0.0;//tangential distortion coefficient p2.
            double distCoeffs5 = -0.2388065;//radial distortion coefficient k3.

            camMatrix = CreateCameraMatrix(fx, fy, cx, cy);
            distCoeffs = new MatOfDouble(distCoeffs1, distCoeffs2, distCoeffs3, distCoeffs4, distCoeffs5);

            Debug.Log("Created a dummy CameraParameters (896x504).");

            DebugUtils.AddDebugStr("Created a dummy CameraParameters (896x504).");

            // create AR camera P * V Matrix
            Matrix4x4 P = OpenCVARUtils.CalculateProjectionMatrixFromCameraMatrixValues((float)fx, (float)fy, (float)cx, (float)cy, width, height, 0.3f, 5f);
            Matrix4x4 V = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            VP = P * V;
#endif

            //Debug.Log("camMatrix " + camMatrix.dump());
            //Debug.Log("distCoeffs " + distCoeffs.dump());


            //Calibration camera
            Size imageSize = new Size(width, height);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            transformationM = new Matrix4x4();

            invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
            Debug.Log("invertYM " + invertYM.ToString());

            invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            Debug.Log("invertZM " + invertZM.ToString());


            axes.SetActive(false);
            head.SetActive(false);
            rightEye.SetActive(false);
            leftEye.SetActive(false);
            mouth.SetActive(false);

            mouthParticleSystem = mouth.GetComponentsInChildren<ParticleSystem>(true);

            grayMat4Thread = new Mat();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

            while (isDetectingInFrameArrivedThread)
            {
                //Wait detecting stop
            }
#endif

            StopThread();
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Clear();
            }

            if (grayMat4Thread != null)
                grayMat4Thread.Dispose();

            rectangleTracker.Reset();

            camMatrix.Dispose();
            distCoeffs.Dispose();

            if (rvec != null)
            {
                rvec.Dispose();
                rvec = null;
            }

            if (tvec != null)
            {
                tvec.Dispose();
                tvec = null;
            }

            if (opticalFlowFilter != null)
                opticalFlowFilter.Dispose();

            if (debugStr != null)
            {
                debugStr.text = string.Empty;
            }
            DebugUtils.ClearDebugStr();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode + ":" + message);
        }

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
        public void OnFrameMatAcquired(Mat grayMat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, CameraIntrinsics cameraIntrinsics)
        {
            isDetectingInFrameArrivedThread = true;

            DebugUtils.VideoTick();

            Mat downScaleMat = null;
            float DOWNSCALE_RATIO;
            if (enableDownScale)
            {
                downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
                DOWNSCALE_RATIO = imageOptimizationHelper.DownscaleRatio;
            }
            else
            {
                downScaleMat = grayMat;
                DOWNSCALE_RATIO = 1.0f;
            }

            if (useOpenCVDetector)
                Imgproc.equalizeHist(downScaleMat, downScaleMat);

            if (enableDetection && !isDetecting)
            {
                isDetecting = true;

                downScaleMat.copyTo(grayMat4Thread);

                System.Threading.Tasks.Task.Run(() =>
                {
                    isThreadRunning = true;

                    if (useOpenCVDetector)
                    {
                        DetectObject(grayMat4Thread, out detectionResult, cascade4Thread, true);
                    }
                    else
                    {
                        DetectObject(grayMat4Thread, out detectionResult, faceLandmarkDetector4Thread);
                    }

                    isThreadRunning = false;
                    OnDetectionDone();
                });
            }

            List<Vector2> points = null;
            if (!useSeparateDetection)
            {
                if (hasUpdatedDetectionResult)
                {
                    hasUpdatedDetectionResult = false;

                    lock (rectangleTracker)
                    {
                        rectangleTracker.UpdateTrackedObjects(detectionResult);
                    }
                }

                lock (rectangleTracker)
                {
                    rectangleTracker.GetObjects(resultObjects, true);
                }

                if (resultObjects.Count > 0)
                {
                    // set original size image
                    DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                    Rect rect = resultObjects[0];

                    // restore to original size rect
                    rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                    rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                    rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                    rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                    // detect face landmark points
                    points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));

                    if (enableOpticalFlowFilter)
                    {
                        opticalFlowFilter.Process(grayMat, points, points);
                    }

                    if (displayCameraPreview)
                    {
                        // draw face rect
                        DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);

                        // draw landmark points
                        DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                    }
                }

            }
            else
            {

                Rect[] rectsWhereRegions;

                if (hasUpdatedDetectionResult)
                {
                    hasUpdatedDetectionResult = false;

                    //Enqueue(() =>
                    //{
                    //    Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                    //});

                    lock (rectangleTracker)
                    {
                        rectsWhereRegions = detectionResult.ToArray();
                    }
                }
                else
                {
                    //Enqueue(() =>
                    //{
                    //    Debug.Log("process: get rectsWhereRegions from previous positions");
                    //});

                    if (useOpenCVDetector)
                    {
                        lock (rectangleTracker)
                        {
                            rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects();
                        }
                    }
                    else
                    {
                        lock (rectangleTracker)
                        {
                            rectsWhereRegions = rectangleTracker.CreateRawRects();
                        }
                    }
                }

                detectedObjectsInRegions.Clear();
                foreach (Rect rect in rectsWhereRegions)
                {
                    if (useOpenCVDetector)
                    {
                        DetectInRegion(downScaleMat, rect, detectedObjectsInRegions, cascade, true);
                    }
                    else
                    {
                        DetectInRegion(downScaleMat, rect, detectedObjectsInRegions, faceLandmarkDetector);
                    }
                }

                lock (rectangleTracker)
                {
                    rectangleTracker.UpdateTrackedObjects(detectedObjectsInRegions);
                    rectangleTracker.GetObjects(resultObjects, false);
                }

                if (resultObjects.Count > 0)
                {
                    // set original size image
                    DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                    Rect rect = resultObjects[0];

                    // restore to original size rect
                    rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                    rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                    rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                    rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                    // detect face landmark points
                    points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));

                    if (enableOpticalFlowFilter)
                    {
                        opticalFlowFilter.Process(grayMat, points, points);
                    }

                    if (displayCameraPreview)
                    {
                        // draw face rect
                        DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);

                        // draw landmark points
                        DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                    }
                }
            }

            DebugUtils.TrackTick();

            Mat camMatrix = CreateCameraMatrix(cameraIntrinsics.FocalLengthX, cameraIntrinsics.FocalLengthY, cameraIntrinsics.PrincipalPointX, cameraIntrinsics.PrincipalPointY);
            MatOfDouble distCoeffs = new MatOfDouble(cameraIntrinsics.RadialDistK1, cameraIntrinsics.RadialDistK2, cameraIntrinsics.RadialDistK3, cameraIntrinsics.TangentialDistP1, cameraIntrinsics.TangentialDistP2);

            Enqueue(() =>
            {
                if (!webCamTextureToMatHelper.IsPlaying()) return;

                if (displayCameraPreview)
                {
                    OpenCVMatUtils.MatToTexture2D(grayMat, texture);
                }

                if (points != null)
                {
                    UpdateARHeadTransform(points, cameraToWorldMatrix, camMatrix, distCoeffs);
                }

                camMatrix.Dispose();
                distCoeffs.Dispose();

                grayMat.Dispose();

            });

            isDetectingInFrameArrivedThread = false;
        }

        private void Update()
        {
            lock (ExecuteOnMainThread)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }
        }

        private void Enqueue(Action action)
        {
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Enqueue(action);
            }
        }

#else

        // Update is called once per frame
        void Update()
        {
            lock (ExecuteOnMainThread)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                DebugUtils.VideoTick();

                Mat grayMat = webCamTextureToMatHelper.GetMat();

                Mat downScaleMat = null;
                float DOWNSCALE_RATIO;
                if (enableDownScale)
                {
                    downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
                    DOWNSCALE_RATIO = imageOptimizationHelper.DownscaleRatio;
                }
                else
                {
                    downScaleMat = grayMat;
                    DOWNSCALE_RATIO = 1.0f;
                }

                if (useOpenCVDetector)
                    Imgproc.equalizeHist(downScaleMat, downScaleMat);

                if (enableDetection && !isDetecting)
                {
                    isDetecting = true;

                    downScaleMat.copyTo(grayMat4Thread);

                    StartThread(ThreadWorker);
                }

                if (!useSeparateDetection)
                {
                    if (hasUpdatedDetectionResult)
                    {
                        hasUpdatedDetectionResult = false;

                        rectangleTracker.UpdateTrackedObjects(detectionResult);
                    }

                    rectangleTracker.GetObjects(resultObjects, true);

                    if (resultObjects.Count > 0)
                    {
                        // set original size image
                        DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                        Rect rect = resultObjects[0];

                        // restore to original size rect
                        rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                        rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                        rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                        rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));

                        if (enableOpticalFlowFilter)
                        {
                            opticalFlowFilter.Process(grayMat, points, points);
                        }

                        DebugUtils.TrackTick();

                        UpdateARHeadTransform(points, arCamera.cameraToWorldMatrix, camMatrix, distCoeffs);

                        if (displayCameraPreview)
                        {
                            // draw face rect
                            DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);

                            // draw landmark points
                            DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                        }
                    }

                }
                else
                {

                    Rect[] rectsWhereRegions;

                    if (hasUpdatedDetectionResult)
                    {
                        hasUpdatedDetectionResult = false;

                        //Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                        rectsWhereRegions = detectionResult.ToArray();
                    }
                    else
                    {
                        //Debug.Log("process: get rectsWhereRegions from previous positions");
                        if (useOpenCVDetector)
                        {
                            rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects();
                        }
                        else
                        {
                            rectsWhereRegions = rectangleTracker.CreateRawRects();
                        }
                    }

                    detectedObjectsInRegions.Clear();
                    foreach (Rect rect in rectsWhereRegions)
                    {
                        if (useOpenCVDetector)
                        {
                            DetectInRegion(downScaleMat, rect, detectedObjectsInRegions, cascade, true);
                        }
                        else
                        {
                            DetectInRegion(downScaleMat, rect, detectedObjectsInRegions, faceLandmarkDetector);
                        }
                    }

                    rectangleTracker.UpdateTrackedObjects(detectedObjectsInRegions);
                    rectangleTracker.GetObjects(resultObjects, false);

                    if (resultObjects.Count > 0)
                    {

                        // set original size image
                        DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                        Rect rect = resultObjects[0];

                        // restore to original size rect
                        rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                        rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                        rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                        rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));

                        if (enableOpticalFlowFilter)
                        {
                            opticalFlowFilter.Process(grayMat, points, points);
                        }

                        DebugUtils.TrackTick();

                        UpdateARHeadTransform(points, arCamera.cameraToWorldMatrix, camMatrix, distCoeffs);

                        if (displayCameraPreview)
                        {
                            // draw face rect
                            DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);

                            // draw landmark points
                            DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                        }
                    }
                }

                if (displayCameraPreview)
                {
                    OpenCVMatUtils.MatToTexture2D(grayMat, texture);
                }
            }
        }
#endif

        private void UpdateARHeadTransform(List<Vector2> points, Matrix4x4 cameraToWorldMatrix, Mat camMatrix, MatOfDouble distCoeffs)
        {
            MatOfPoint3f objectPoints = null;
            bool isRightEyeOpen = false;
            bool isLeftEyeOpen = false;
            bool isMouthOpen = false;
            if (points.Count == 68)
            {

                objectPoints = objectPoints68;

                imagePoints.fromArray(
                    new Point((points[38].x + points[41].x) / 2, (points[38].y + points[41].y) / 2),//l eye (Interpupillary breadth)
                    new Point((points[43].x + points[46].x) / 2, (points[43].y + points[46].y) / 2),//r eye (Interpupillary breadth)
                    new Point(points[30].x, points[30].y),//nose (Tip)
                    new Point(points[33].x, points[33].y),//nose (Subnasale)
                    new Point(points[0].x, points[0].y),//l ear (Bitragion breadth)
                    new Point(points[16].x, points[16].y)//r ear (Bitragion breadth)
                );

                if (Mathf.Abs((float)(points[43].y - points[46].y)) > Mathf.Abs((float)(points[42].x - points[45].x)) / 5.0)
                {
                    isRightEyeOpen = true;
                }

                if (Mathf.Abs((float)(points[38].y - points[41].y)) > Mathf.Abs((float)(points[39].x - points[36].x)) / 5.0)
                {
                    isLeftEyeOpen = true;
                }

                float noseDistance = Mathf.Abs((float)(points[27].y - points[33].y));
                float mouseDistance = Mathf.Abs((float)(points[62].y - points[66].y));
                if (mouseDistance > noseDistance / 5.0)
                {
                    isMouthOpen = true;
                }
                else
                {
                    isMouthOpen = false;
                }

            }
            else if (points.Count == 17)
            {

                objectPoints = objectPoints17;

                imagePoints.fromArray(
                    new Point((points[2].x + points[3].x) / 2, (points[2].y + points[3].y) / 2),//l eye (Interpupillary breadth)
                    new Point((points[4].x + points[5].x) / 2, (points[4].y + points[5].y) / 2),//r eye (Interpupillary breadth)
                    new Point(points[0].x, points[0].y),//nose (Tip)
                    new Point(points[1].x, points[1].y),//nose (Subnasale)
                    new Point(points[6].x, points[6].y),//l ear (Bitragion breadth)
                    new Point(points[8].x, points[8].y)//r ear (Bitragion breadth)
                );

                if (Mathf.Abs((float)(points[11].y - points[12].y)) > Mathf.Abs((float)(points[4].x - points[5].x)) / 5.0)
                {
                    isRightEyeOpen = true;
                }

                if (Mathf.Abs((float)(points[9].y - points[10].y)) > Mathf.Abs((float)(points[2].x - points[3].x)) / 5.0)
                {
                    isLeftEyeOpen = true;
                }

                float noseDistance = Mathf.Abs((float)(points[3].y - points[1].y));
                float mouseDistance = Mathf.Abs((float)(points[14].y - points[16].y));
                if (mouseDistance > noseDistance / 2.0)
                {
                    isMouthOpen = true;
                }
                else
                {
                    isMouthOpen = false;
                }

            }
            else if (points.Count == 6)
            {

                objectPoints = objectPoints6;

                imagePoints.fromArray(
                    new Point((points[2].x + points[3].x) / 2, (points[2].y + points[3].y) / 2),//l eye (Interpupillary breadth)
                    new Point((points[4].x + points[5].x) / 2, (points[4].y + points[5].y) / 2),//r eye (Interpupillary breadth)
                    new Point(points[0].x, points[0].y),//nose (Tip)
                    new Point(points[1].x, points[1].y)//nose (Subnasale)
                );

            }
            else if (points.Count == 5)
            {

                objectPoints = objectPoints5;

                imagePoints.fromArray(
                    new Point(points[3].x, points[3].y),//l eye (Inner corner of the eye)
                    new Point(points[1].x, points[1].y),//r eye (Inner corner of the eye)
                    new Point(points[2].x, points[2].y),//l eye (Tail of the eye)
                    new Point(points[0].x, points[0].y),//r eye (Tail of the eye)
                    new Point(points[4].x, points[4].y)//nose (Nose top)
                );
            }

            // estimate head pose
            if (rvec == null || tvec == null)
            {
                rvec = new Mat(3, 1, CvType.CV_64FC1);
                tvec = new Mat(3, 1, CvType.CV_64FC1);
                Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
            }


            double tvec_x = tvec.get(0, 0)[0], tvec_y = tvec.get(1, 0)[0], tvec_z = tvec.get(2, 0)[0];

            bool isNotInViewport = false;
            Vector4 pos = VP * new Vector4((float)tvec_x, (float)tvec_y, (float)tvec_z, 1.0f);
            if (pos.w != 0)
            {
                float x = pos.x / pos.w, y = pos.y / pos.w, z = pos.z / pos.w;
                if (x < -1.0f || x > 1.0f || y < -1.0f || y > 1.0f || z < -1.0f || z > 1.0f)
                    isNotInViewport = true;
            }

            if (double.IsNaN(tvec_z) || isNotInViewport)
            { // if tvec is wrong data, do not use extrinsic guesses. (the estimated object is not in the camera field of view)
                Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
            }
            else
            {
                Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec, true, Calib3d.SOLVEPNP_ITERATIVE);
            }

            if (applyEstimationPose && !isNotInViewport)
            {

                // Display effects.
                if (displayHead)
                    head.SetActive(true);
                if (displayAxes)
                    axes.SetActive(true);

                if (displayEffects)
                {
                    rightEye.SetActive(isRightEyeOpen);
                    leftEye.SetActive(isLeftEyeOpen);

                    if (isMouthOpen)
                    {
                        mouth.SetActive(true);
                        foreach (ParticleSystem ps in mouthParticleSystem)
                        {
                            var em = ps.emission;
                            em.enabled = true;
#if UNITY_5_5_OR_NEWER
                            var main = ps.main;
                            main.startSizeMultiplier = 20;
#else
                            ps.startSize = 20;
#endif
                        }
                    }
                    else
                    {
                        foreach (ParticleSystem ps in mouthParticleSystem)
                        {
                            var em = ps.emission;
                            em.enabled = false;
                        }
                    }
                }

                // Convert to unity pose data.
                double[] rvecArr = new double[3];
                rvec.get(0, 0, rvecArr);
                double[] tvecArr = new double[3];
                tvec.get(0, 0, tvecArr);
                PoseData poseData = OpenCVARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                // Create transform matrix.
                transformationM = Matrix4x4.TRS(poseData.Pos, poseData.Rot, Vector3.one);

                // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                ARM = invertYM * transformationM * invertYM;

                // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
                ARM = ARM * invertYM * invertZM;

                Matrix4x4 localToWorldMatrix = cameraToWorldMatrix * invertZM;
                ARM = localToWorldMatrix * ARM;

                if (enableLerpFilter)
                {
                    arGameObject.SetMatrix4x4(ARM);
                }
                else
                {
                    OpenCVARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
                }
            }
        }

        private void StartThread(Action action)
        {
#if WINDOWS_UWP || (!UNITY_WSA_10_0 && (NET_4_6 || NET_STANDARD_2_0))
            System.Threading.Tasks.Task.Run(() => action());
#else
            ThreadPool.QueueUserWorkItem(_ => action());
#endif
        }

        private void StopThread()
        {
            if (!isThreadRunning)
                return;

            while (isThreadRunning)
            {
                //Wait threading stop
            }
        }

        private void ThreadWorker()
        {
            isThreadRunning = true;

            if (useOpenCVDetector)
            {
                DetectObject(grayMat4Thread, out detectionResult, cascade4Thread, true);
            }
            else
            {
                DetectObject(grayMat4Thread, out detectionResult, faceLandmarkDetector4Thread);
            }

            lock (ExecuteOnMainThread)
            {
                if (ExecuteOnMainThread.Count == 0)
                {
                    ExecuteOnMainThread.Enqueue(() =>
                    {
                        OnDetectionDone();
                    });
                }
            }

            isThreadRunning = false;
        }

        private void DetectObject(Mat img, out List<Rect> detectedObjects, FaceLandmarkDetector landmarkDetector)
        {
            DlibOpenCVUtils.SetImage(landmarkDetector, img);

            List<UnityEngine.Rect> detectResult = landmarkDetector.Detect();

            detectedObjects = new List<Rect>();

            int len = detectResult.Count;
            for (int i = 0; i < len; i++)
            {
                UnityEngine.Rect r = detectResult[i];
                detectedObjects.Add(new Rect((int)r.x, (int)r.y, (int)r.width, (int)r.height));
            }
        }

        private void DetectObject(Mat img, out List<Rect> detectedObjects, CascadeClassifier cascade, bool correctToDlibResult = false)
        {
            int d = Mathf.Min(img.width(), img.height());
            d = (int)Mathf.Round(d * minDetectionSizeRatio);

            MatOfRect objects = new MatOfRect();
            if (cascade != null)
                cascade.detectMultiScale(img, objects, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, new Size(d, d), new Size());

            detectedObjects = objects.toList();

            if (correctToDlibResult)
            {
                int len = detectedObjects.Count;
                for (int i = 0; i < len; i++)
                {
                    Rect r = detectedObjects[i];
                    // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                    r.x += (int)(r.width * 0.05f);
                    r.y += (int)(r.height * 0.1f);
                    r.width = (int)(r.width * 0.9f);
                    r.height = (int)(r.height * 0.9f);
                }
            }
        }

        private void OnDetectionDone()
        {
            hasUpdatedDetectionResult = true;

            isDetecting = false;
        }

        private void DetectInRegion(Mat img, Rect region, List<Rect> detectedObjectsInRegions, FaceLandmarkDetector landmarkDetector)
        {
            Rect r0 = new Rect(new Point(), img.size());
            Rect r1 = new Rect(region.x, region.y, region.width, region.height);
            Rect.inflate(r1, (int)((r1.width * coeffTrackingWindowSize) - r1.width) / 2,
                (int)((r1.height * coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if ((r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("detectInRegion: Empty intersection");
                return;
            }

            using (Mat img1_roi = new Mat(img, r1))
            using (Mat img1 = new Mat(r1.size(), img.type()))
            {
                img1_roi.copyTo(img1);

                DlibOpenCVUtils.SetImage(landmarkDetector, img1);

                List<UnityEngine.Rect> detectResult = landmarkDetector.Detect();

                int len = detectResult.Count;
                for (int i = 0; i < len; i++)
                {
                    UnityEngine.Rect tmp = detectResult[i];
                    Rect r = new Rect((int)(tmp.x + r1.x), (int)(tmp.y + r1.y), (int)tmp.width, (int)tmp.height);
                    detectedObjectsInRegions.Add(r);
                }
            }
        }

        private void DetectInRegion(Mat img, Rect region, List<Rect> detectedObjectsInRegions, CascadeClassifier cascade, bool correctToDlibResult = false)
        {
            Rect r0 = new Rect(new Point(), img.size());
            Rect r1 = new Rect(region.x, region.y, region.width, region.height);
            Rect.inflate(r1, (int)((r1.width * coeffTrackingWindowSize) - r1.width) / 2,
                (int)((r1.height * coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if ((r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("detectInRegion: Empty intersection");
                return;
            }

            int d = Math.Min(region.width, region.height);
            d = (int)Math.Round(d * coeffObjectSizeToTrack);

            using (MatOfRect tmpobjects = new MatOfRect())
            using (Mat img1 = new Mat(img, r1)) //subimage for rectangle -- without data copying
            {
                cascade.detectMultiScale(img1, tmpobjects, 1.1, 2, 0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE | Objdetect.CASCADE_FIND_BIGGEST_OBJECT, new Size(d, d), new Size());

                Rect[] tmpobjectsArray = tmpobjects.toArray();
                int len = tmpobjectsArray.Length;
                for (int i = 0; i < len; i++)
                {
                    Rect tmp = tmpobjectsArray[i];
                    Rect r = new Rect(new Point(tmp.x + r1.x, tmp.y + r1.y), tmp.size());

                    if (correctToDlibResult)
                    {
                        // correct the deviation of the detection result of the face rectangle of OpenCV and Dlib.
                        r.x += (int)(r.width * 0.05f);
                        r.y += (int)(r.height * 0.1f);
                        r.width = (int)(r.width * 0.9f);
                        r.height = (int)(r.height * 0.9f);
                    }

                    detectedObjectsInRegions.Add(r);
                }
            }
        }

        private void AjustPointScale(MatOfPoint3f p, double scale)
        {
            Point3[] arr = p.toArray();
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i].x *= scale;
                arr[i].y *= scale;
                arr[i].z *= scale;
            }
            p.fromArray(arr);
        }
        private Mat CreateCameraMatrix(double fx, double fy, double cx, double cy)
        {
            Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);

            return camMatrix;
        }

        void LateUpdate()
        {
            DebugUtils.RenderTick();
            float renderDeltaTime = DebugUtils.GetRenderDeltaTime();
            float videoDeltaTime = DebugUtils.GetVideoDeltaTime();
            float trackDeltaTime = DebugUtils.GetTrackDeltaTime();

            if (renderFPS != null)
            {
                renderFPS.text = string.Format("Render: {0:0.0} ms ({1:0.} fps)", renderDeltaTime, 1000.0f / renderDeltaTime);
            }
            if (videoFPS != null)
            {
                videoFPS.text = string.Format("Video: {0:0.0} ms ({1:0.} fps)", videoDeltaTime, 1000.0f / videoDeltaTime);
            }
            if (trackFPS != null)
            {
                trackFPS.text = string.Format("Track:   {0:0.0} ms ({1:0.} fps)", trackDeltaTime, 1000.0f / trackDeltaTime);
            }
            if (debugStr != null)
            {
                if (DebugUtils.GetDebugStrLength() > 0)
                {
                    if (debugStr.preferredHeight >= debugStr.rectTransform.rect.height)
                        debugStr.text = string.Empty;

                    debugStr.text += DebugUtils.GetDebugStr();
                    DebugUtils.ClearDebugStr();
                }
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.FrameMatAcquired -= OnFrameMatAcquired;
#endif
            webCamTextureToMatHelper.Dispose();
            imageOptimizationHelper.Dispose();

            if (cascade != null)
                cascade.Dispose();

            if (cascade4Thread != null)
                cascade4Thread.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (faceLandmarkDetector4Thread != null)
                faceLandmarkDetector4Thread.Dispose();

            if (rectangleTracker != null)
                rectangleTracker.Dispose();

            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the back button clicked click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("HoloLensWithDlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button clicked click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button clicked click click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button clicked click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button clicked click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.RequestedIsFrontFacing = !webCamTextureToMatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the display camera preview toggle value changed event.
        /// </summary>
        public void OnDisplayCamreaPreviewToggleValueChanged()
        {
            displayCameraPreview = displayCameraPreviewToggle.isOn;

            previewQuad.SetActive(displayCameraPreview);
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            enableDownScale = enableDownScaleToggle.isOn;

            if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
            {
                webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the use separate detection toggle value changed event.
        /// </summary>
        public void OnUseSeparateDetectionToggleValueChanged()
        {
            useSeparateDetection = useSeparateDetectionToggle.isOn;

            if (rectangleTracker != null)
            {
                lock (rectangleTracker)
                {
                    rectangleTracker.Reset();
                }
            }
        }

        /// <summary>
        /// Raises the use OpenCV Detector toggle value changed event.
        /// </summary>
        public void OnUseOpenCVDetectorToggleValueChanged()
        {
            useOpenCVDetector = useOpenCVDetectorToggle.isOn;
        }

        /// <summary>
        /// Raises the display axes toggle value changed event.
        /// </summary>
        public void OnDisplayAxesToggleValueChanged()
        {
            if (displayAxesToggle.isOn)
            {
                displayAxes = true;
            }
            else
            {
                displayAxes = false;
                axes.SetActive(false);
            }
        }

        /// <summary>
        /// Raises the display head toggle value changed event.
        /// </summary>
        public void OnDisplayHeadToggleValueChanged()
        {
            if (displayHeadToggle.isOn)
            {
                displayHead = true;
            }
            else
            {
                displayHead = false;
                head.SetActive(false);
            }
        }

        /// <summary>
        /// Raises the display effects toggle value changed event.
        /// </summary>
        public void OnDisplayEffectsToggleValueChanged()
        {
            if (displayEffectsToggle.isOn)
            {
                displayEffects = true;
            }
            else
            {
                displayEffects = false;
                rightEye.SetActive(false);
                leftEye.SetActive(false);
                mouth.SetActive(false);
            }
        }

        /// <summary>
        /// Raises the enable optical flow filter toggle value changed event.
        /// </summary>
        public void OnEnableOpticalFlowFilterToggleValueChanged()
        {
            if (enableOpticalFlowFilterToggle.isOn)
            {
                enableOpticalFlowFilter = true;
                if (opticalFlowFilter != null)
                    opticalFlowFilter.Reset();
            }
            else
            {
                enableOpticalFlowFilter = false;
            }
        }

        /// <summary>
        /// Raises the enable lerp filter toggle value changed event.
        /// </summary>
        public void OnEnableLerpFilterToggleValueChanged()
        {
            enableLerpFilter = enableLerpFilterToggle.isOn;
        }
    }
}