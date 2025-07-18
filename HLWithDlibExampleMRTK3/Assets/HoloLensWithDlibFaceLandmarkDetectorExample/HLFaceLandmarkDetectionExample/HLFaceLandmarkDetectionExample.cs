using DlibFaceLandmarkDetector;
using DlibFaceLandmarkDetector.UnityIntegration;
using HoloLensCameraStream;
using HoloLensWithDlibFaceLandmarkDetectorExample.RectangleTrack;
using HoloLensWithOpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLens Face Landmark Detection Example
    /// An example of face landmark detection using OpenCVForUnity and DlibLandmarkDetector on Hololens.
    /// Referring to https://github.com/Itseez/opencv/blob/master/modules/objdetect/src/detection_based_tracker.cpp.
    /// </summary>
    [RequireComponent(typeof(HLCameraStream2MatHelper), typeof(ImageOptimizationHelper))]
    public class HLFaceLandmarkDetectionExample : MonoBehaviour
    {

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
        /// Determines if displays camera image.
        /// </summary>
        public bool displayCameraImage = false;

        /// <summary>
        /// The display camera image toggle.
        /// </summary>
        public Toggle displayCameraImageToggle;

        /// <summary>
        /// Determines if displays detected face rect.
        /// </summary>
        public bool displayDetectedFaceRect = false;

        /// <summary>
        /// The is  display detected face rect toggle.
        /// </summary>
        public Toggle displayDetectedFaceRectToggle;

        /// <summary>
        /// The min detection size ratio.
        /// </summary>
        public float minDetectionSizeRatio = 0.07f;

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
        /// The quad renderer.
        /// </summary>
        Renderer quad_renderer;

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
        List<List<Vector2>> resultFaceLandmarkPoints = new List<List<Vector2>>();

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
            enableDownScaleToggle.isOn = enableDownScale;
            useSeparateDetectionToggle.isOn = useSeparateDetection;
            useOpenCVDetectorToggle.isOn = useOpenCVDetector;
            displayCameraImageToggle.isOn = displayCameraImage;
            displayDetectedFaceRectToggle.isOn = displayDetectedFaceRect;

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

            texture = new Texture2D(grayMat.cols(), grayMat.rows(), TextureFormat.Alpha8, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));


            //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            DebugUtils.AddDebugStr(webCamTextureToMatHelper.GetWidth() + " x " + webCamTextureToMatHelper.GetHeight() + " : " + webCamTextureToMatHelper.GetFPS());
            if (enableDownScale)
                DebugUtils.AddDebugStr("enableDownScale = true: " + imageOptimizationHelper.DownscaleRatio + " / " + webCamTextureToMatHelper.GetWidth() / imageOptimizationHelper.DownscaleRatio + " x " + webCamTextureToMatHelper.GetHeight() / imageOptimizationHelper.DownscaleRatio);


            Matrix4x4 projectionMatrix;
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            projectionMatrix = webCamTextureToMatHelper.GetProjectionMatrix();
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
#else
            //This value is obtained from PhotoCapture's TryGetProjectionMatrix() method.I do not know whether this method is good.
            //Please see the discussion of this thread.Https://forums.hololens.com/discussion/782/live-stream-of-locatable-camera-webcam-in-unity
            projectionMatrix = Matrix4x4.identity;
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
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
#endif

            quad_renderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);

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

                // set original size image
                DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                resultFaceLandmarkPoints.Clear();
                foreach (Rect rect in resultObjects)
                {
                    // restore to original size rect
                    rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                    rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                    rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                    rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                    // detect face landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));
                    resultFaceLandmarkPoints.Add(points);
                }

                if (displayCameraImage)
                {
                    Imgproc.putText(grayMat, "W:" + grayMat.width() + " H:" + grayMat.height(), new Point(5, grayMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    // fill all black.
                    Imgproc.rectangle(grayMat, new Point(0, 0), new Point(grayMat.width(), grayMat.height()), new Scalar(0, 0, 0, 0), -1);
                }

                if (displayDetectedFaceRect)
                {
                    // draw face rects
                    foreach (Rect rect in resultObjects)
                    {
                        DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);
                    }
                }

                // draw face landmark points
                foreach (List<Vector2> points in resultFaceLandmarkPoints)
                {
                    DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
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

                // set original size image
                DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                resultFaceLandmarkPoints.Clear();
                foreach (Rect rect in resultObjects)
                {
                    // restore to original size rect
                    rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                    rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                    rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                    rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                    // detect face landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));
                    resultFaceLandmarkPoints.Add(points);
                }

                if (displayCameraImage)
                {
                    Imgproc.putText(grayMat, "W:" + grayMat.width() + " H:" + grayMat.height(), new Point(5, grayMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    // fill all black.
                    Imgproc.rectangle(grayMat, new Point(0, 0), new Point(grayMat.width(), grayMat.height()), new Scalar(0, 0, 0, 0), -1);
                }

                if (displayDetectedFaceRect)
                {
                    // draw previous rects
                    DrawDownScaleFaceRects(grayMat, rectsWhereRegions, DOWNSCALE_RATIO, COLOR_GRAY, 1);

                    // draw face rects
                    foreach (Rect rect in resultObjects)
                    {
                        DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);
                    }
                }

                // draw face landmark points
                foreach (List<Vector2> points in resultFaceLandmarkPoints)
                {
                    DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                }
            }

            DebugUtils.TrackTick();

            Enqueue(() =>
            {
                if (!webCamTextureToMatHelper.IsPlaying()) return;

                OpenCVMatUtils.MatToTexture2D(grayMat, texture);
                grayMat.Dispose();

                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
                quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2) * 2.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;

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

                    // set original size image
                    DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                    resultFaceLandmarkPoints.Clear();
                    foreach (Rect rect in resultObjects)
                    {

                        // restore to original size rect
                        rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                        rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                        rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                        rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));
                        resultFaceLandmarkPoints.Add(points);
                    }

                    if (displayCameraImage)
                    {
                        Imgproc.putText(grayMat, "W:" + grayMat.width() + " H:" + grayMat.height(), new Point(5, grayMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                    else
                    {
                        // fill all black.
                        Imgproc.rectangle(grayMat, new Point(0, 0), new Point(grayMat.width(), grayMat.height()), new Scalar(0, 0, 0, 0), -1);
                    }

                    if (displayDetectedFaceRect)
                    {
                        // draw face rects
                        foreach (Rect rect in resultObjects)
                        {
                            DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);
                        }
                    }

                    // draw face landmark points
                    foreach (List<Vector2> points in resultFaceLandmarkPoints)
                    {
                        DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
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

                    // set original size image
                    DlibOpenCVUtils.SetImage(faceLandmarkDetector, grayMat);

                    resultFaceLandmarkPoints.Clear();
                    foreach (Rect rect in resultObjects)
                    {

                        // restore to original size rect
                        rect.x = (int)(rect.x * DOWNSCALE_RATIO);
                        rect.y = (int)(rect.y * DOWNSCALE_RATIO);
                        rect.width = (int)(rect.width * DOWNSCALE_RATIO);
                        rect.height = (int)(rect.height * DOWNSCALE_RATIO);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));
                        resultFaceLandmarkPoints.Add(points);
                    }

                    if (displayCameraImage)
                    {
                        Imgproc.putText(grayMat, "W:" + grayMat.width() + " H:" + grayMat.height(), new Point(5, grayMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                    else
                    {
                        // fill all black.
                        Imgproc.rectangle(grayMat, new Point(0, 0), new Point(grayMat.width(), grayMat.height()), new Scalar(0, 0, 0, 0), -1);
                    }

                    if (displayDetectedFaceRect)
                    {
                        // draw previous rects
                        DrawDownScaleFaceRects(grayMat, rectsWhereRegions, DOWNSCALE_RATIO, COLOR_GRAY, 1);

                        // draw face rects
                        foreach (Rect rect in resultObjects)
                        {
                            DlibOpenCVUtils.DrawFaceRect(grayMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_GRAY, 2);
                        }
                    }

                    // draw face landmark points
                    foreach (List<Vector2> points in resultFaceLandmarkPoints)
                    {
                        DlibOpenCVUtils.DrawFaceLandmark(grayMat, points, COLOR_WHITE, 4);
                    }
                }

                DebugUtils.TrackTick();

                OpenCVMatUtils.MatToTexture2D(grayMat, texture);
            }

            if (webCamTextureToMatHelper.IsPlaying())
            {

                Matrix4x4 cameraToWorldMatrix = Camera.main.cameraToWorldMatrix;
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2) * 2.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }
        }
#endif

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

        private void DrawDownScaleFaceRects(Mat img, Rect[] rects, float downscaleRatio, Scalar color, int thickness)
        {
            int len = rects.Length;
            for (int i = 0; i < len; i++)
            {
                Rect rect = new Rect(
                    (int)(rects[i].x * downscaleRatio),
                    (int)(rects[i].y * downscaleRatio),
                    (int)(rects[i].width * downscaleRatio),
                    (int)(rects[i].height * downscaleRatio)
                );
                Imgproc.rectangle(img, rect, color, thickness);
            }
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
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("HoloLensWithDlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.RequestedIsFrontFacing = !webCamTextureToMatHelper.RequestedIsFrontFacing;
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
        /// Raises the display camera image toggle value changed event.
        /// </summary>
        public void OnDisplayCameraImageToggleValueChanged()
        {
            displayCameraImage = displayCameraImageToggle.isOn;
        }

        /// <summary>
        /// Raises the display detected face rect toggle value changed event.
        /// </summary>
        public void OnDisplayDetectedFaceRectToggleValueChanged()
        {
            displayDetectedFaceRect = displayDetectedFaceRectToggle.isOn;
        }
    }
}