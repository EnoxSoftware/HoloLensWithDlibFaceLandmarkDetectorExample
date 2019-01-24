using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.RectangleTrack;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using DlibFaceLandmarkDetector;
using Rect = OpenCVForUnity.CoreModule.Rect;
using HoloLensWithOpenCVForUnity.UnityUtils.Helper;

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLens Face Landmark Detection Example
    /// An example of face landmark detection using OpenCVForUnity and DlibLandmarkDetector on Hololens.
    /// Referring to https://github.com/Itseez/opencv/blob/master/modules/objdetect/src/detection_based_tracker.cpp.
    /// </summary>
    [RequireComponent(typeof(HololensCameraStreamToMatHelper))]
    public class HoloLensFaceLandmarkDetectionExample : ExampleSceneBase
    {

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
        string dlibShapePredictorFileName = "sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        #if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
        int CVTCOLOR_CODE = Imgproc.COLOR_BGRA2GRAY;
        Scalar COLOR_RED = new Scalar(0, 0, 255, 255);
        Scalar COLOR_GREEN = new Scalar(0, 255, 0, 255);
        Scalar COLOR_BLUE = new Scalar(255, 0, 0, 255);
        #else
        int CVTCOLOR_CODE = Imgproc.COLOR_RGBA2GRAY;
        Scalar COLOR_RED = new Scalar(255, 0, 0, 255);
        Scalar COLOR_GREEN = new Scalar(0, 255, 0, 255);
        Scalar COLOR_BLUE = new Scalar(0, 0, 255, 255);
        #endif

        Mat grayMat4Thread;
        CascadeClassifier cascade4Thread;
        FaceLandmarkDetector faceLandmarkDetector4Thread;
        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

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
        List<Rect> detectedObjectsInRegions = new List<Rect>();
        List<Rect> resultObjects = new List<Rect>();

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
        protected override void Start()
        {
            base.Start();

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<HololensCameraStreamToMatHelper>();
            #if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Initialize();

            rectangleTracker = new RectangleTracker();

            dlibShapePredictorFileName = HoloLensWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);


            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath("sp_human_face_6.dat");
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            faceLandmarkDetector4Thread = new FaceLandmarkDetector(dlibShapePredictorFilePath);
 

            useSeparateDetectionToggle.isOn = useSeparateDetection;
            useOpenCVDetectorToggle.isOn = useOpenCVDetector;
            displayCameraImageToggle.isOn = displayCameraImage;
            displayDetectedFaceRectToggle.isOn = displayDetectedFaceRect;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            #if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            // HololensCameraStream always returns image data in BGRA format.
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.BGRA32, false);
            #else
            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            #endif
            texture.wrapMode = TextureWrapMode.Clamp;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));

            Matrix4x4 projectionMatrix;
            #if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            projectionMatrix = webCamTextureToMatHelper.GetProjectionMatrix ();
            quad_renderer.sharedMaterial.SetMatrix ("_CameraProjectionMatrix", projectionMatrix);
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


            grayMat = new Mat();
            cascade = new CascadeClassifier();
            cascade.load (Utils.getFilePath ("lbpcascade_frontalface.xml"));
            #if !UNITY_WSA_10_0 || UNITY_EDITOR
            // "empty" method is not working on the UWP platform.
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            #endif

            grayMat4Thread = new Mat();
            cascade4Thread = new CascadeClassifier();
            cascade4Thread.load(Utils.getFilePath("haarcascade_frontalface_alt.xml"));
            #if !UNITY_WSA_10_0 || UNITY_EDITOR
            // "empty" method is not working on the UWP platform.
            if (cascade4Thread.empty()) {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            #endif
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            StopThread();
            lock (ExecuteOnMainThread) {
                ExecuteOnMainThread.Clear();
            }

            if (grayMat != null)
                grayMat.Dispose();

            if (cascade != null)
                cascade.Dispose();

            if (grayMat4Thread != null)
                grayMat4Thread.Dispose();

            if (cascade4Thread != null)
                cascade4Thread.Dispose();

            rectangleTracker.Reset();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode) {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        #if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
        public void OnFrameMatAcquired (Mat bgraMat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix)
        {
            Imgproc.cvtColor(bgraMat, grayMat, CVTCOLOR_CODE);

            Mat downScaleGrayMat = imageOptimizationHelper.GetDownScaleMat(grayMat);

            if (useOpenCVDetector)
                Imgproc.equalizeHist(downScaleGrayMat, downScaleGrayMat);

            if (enableDetection && !isDetecting ) {

                isDetecting = true;

                downScaleGrayMat.copyTo (grayMat4Thread);

                System.Threading.Tasks.Task.Run(() => {

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
                    OnDetectionDone ();
                });
            }

        
            if (!displayCameraImage) {
                // fill all black.
                Imgproc.rectangle (bgraMat, new Point (0, 0), new Point (bgraMat.width (), bgraMat.height ()), new Scalar (0, 0, 0, 0), -1);
            }


            if (!useSeparateDetection) {
                if (hasUpdatedDetectionResult) {
                    hasUpdatedDetectionResult = false;

                    lock (rectangleTracker) {
                        rectangleTracker.UpdateTrackedObjects (detectionResult);
                    }
                }

                lock (rectangleTracker) {
                    rectangleTracker.GetObjects (resultObjects, true);
                }

                // set original size image
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, grayMat);

                int len = resultObjects.Count;
                for (int i = 0; i < len; i++)
                {
                    Rect rect = resultObjects[i];

                    // restore to original size rect
                    float downscaleRatio = imageOptimizationHelper.downscaleRatio;
                    rect.x = (int)(rect.x * downscaleRatio);
                    rect.y = (int)(rect.y * downscaleRatio);
                    rect.width = (int)(rect.width * downscaleRatio);
                    rect.height = (int)(rect.height * downscaleRatio);

                    // detect face landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                    // draw face landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark (bgraMat, points, COLOR_GREEN, 2);

                    if (displayDetectedFaceRect) {
                        // draw face rect
                        OpenCVForUnityUtils.DrawFaceRect (bgraMat, new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height), COLOR_RED, 2);
                    }
                }

            }else {

                Rect[] rectsWhereRegions;

                if (hasUpdatedDetectionResult) {
                    hasUpdatedDetectionResult = false;

                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                    //}, true);

                    lock (rectangleTracker) {
                        rectsWhereRegions = detectionResult.ToArray ();
                    }

                    if (displayDetectedFaceRect)
                        DrawDownScaleFaceRects(bgraMat, rectsWhereRegions, imageOptimizationHelper.downscaleRatio, COLOR_BLUE, 1);

                } else {
                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log("process: get rectsWhereRegions from previous positions");
                    //}, true);

                    if (useOpenCVDetector)
                    {
                        lock (rectangleTracker)
                        {
                            rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects();
                        }
                    }
                    else
                    {
                        lock(rectangleTracker)
                        {
                            rectsWhereRegions = rectangleTracker.CreateRawRects();
                        }
                    }

                    if (displayDetectedFaceRect)
                        DrawDownScaleFaceRects(bgraMat, rectsWhereRegions, imageOptimizationHelper.downscaleRatio, COLOR_GREEN, 1);
                }

                detectedObjectsInRegions.Clear();
                int len = rectsWhereRegions.Length;
                for (int i = 0; i < len; i++)
                {
                    if (useOpenCVDetector)
                    {
                        DetectInRegion(downScaleGrayMat, rectsWhereRegions[i], detectedObjectsInRegions, cascade, true);
                    }
                    else
                    {
                        DetectInRegion(downScaleGrayMat, rectsWhereRegions[i], detectedObjectsInRegions, faceLandmarkDetector);
                    }
                }

                lock (rectangleTracker) {
                    rectangleTracker.UpdateTrackedObjects (detectedObjectsInRegions);
                    rectangleTracker.GetObjects (resultObjects, false);
                }

                // set original size image
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, grayMat);

                len = resultObjects.Count;
                for (int i = 0; i < len; i++)
                {
                    Rect rect = resultObjects[i];

                    // restore to original size rect
                    float downscaleRatio = imageOptimizationHelper.downscaleRatio;
                    rect.x = (int)(rect.x * downscaleRatio);
                    rect.y = (int)(rect.y * downscaleRatio);
                    rect.width = (int)(rect.width * downscaleRatio);
                    rect.height = (int)(rect.height * downscaleRatio);

                    // detect face landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height));

                    // draw face landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark (bgraMat, points, COLOR_GREEN, 2);

                    if (displayDetectedFaceRect) {
                        // draw face rect
                        OpenCVForUnityUtils.DrawFaceRect (bgraMat, new UnityEngine.Rect (rect.x, rect.y, rect.width, rect.height), COLOR_RED, 2);
                    }
                }
            }
        

            Enqueue(() => {

                if (!webCamTextureToMatHelper.IsPlaying ()) return;

                Utils.fastMatToTexture2D(bgraMat, texture);
                bgraMat.Dispose ();

                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix ("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn (3) - cameraToWorldMatrix.GetColumn (2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation (-cameraToWorldMatrix.GetColumn (2), cameraToWorldMatrix.GetColumn (1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;

            });
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
            lock (ExecuteOnMainThread) {
                while (ExecuteOnMainThread.Count > 0) {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }
            
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();
                Imgproc.cvtColor(rgbaMat, grayMat, CVTCOLOR_CODE);

                Mat downScaleGrayMat = imageOptimizationHelper.GetDownScaleMat(grayMat);

                if (useOpenCVDetector)
                    Imgproc.equalizeHist(downScaleGrayMat, downScaleGrayMat);

                if (enableDetection && !isDetecting) {
                    isDetecting = true;

                    downScaleGrayMat.copyTo(grayMat4Thread);

                    StartThread(ThreadWorker);
                }
                
                if (!displayCameraImage) {
                    // fill all black
                    Imgproc.rectangle(rgbaMat, new Point(0, 0), new Point(rgbaMat.width(), rgbaMat.height()), new Scalar(0, 0, 0, 0), -1);
                }
                
                if (!useSeparateDetection) {
                    if (hasUpdatedDetectionResult)
                    {
                        hasUpdatedDetectionResult = false;

                        rectangleTracker.UpdateTrackedObjects(detectionResult);
                    }

                    rectangleTracker.GetObjects(resultObjects, true);

                    // set original size image
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, grayMat);

                    int len = resultObjects.Count;
                    for (int i = 0; i < len; i++)
                    {
                        Rect rect = resultObjects[i];

                        // restore to original size rect
                        float downscaleRatio = imageOptimizationHelper.downscaleRatio;
                        rect.x = (int)(rect.x * downscaleRatio);
                        rect.y = (int)(rect.y * downscaleRatio);
                        rect.width= (int)(rect.width * downscaleRatio);
                        rect.height = (int)(rect.height * downscaleRatio);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));

                        // draw face landmark points
                        OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                        if (displayDetectedFaceRect) {
                            // draw face rect
                            OpenCVForUnityUtils.DrawFaceRect(rgbaMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_RED, 2);
                        }
                    }
                    
                } else {

                    Rect[] rectsWhereRegions;

                    if (hasUpdatedDetectionResult) {
                        hasUpdatedDetectionResult = false;

                        //Debug.Log("process: get rectsWhereRegions were got from detectionResult");
                        rectsWhereRegions = detectionResult.ToArray();

                        if (displayDetectedFaceRect)
                            DrawDownScaleFaceRects(rgbaMat, rectsWhereRegions, imageOptimizationHelper.downscaleRatio, COLOR_BLUE, 1);
                    } else {
                        //Debug.Log("process: get rectsWhereRegions from previous positions");
                        if (useOpenCVDetector)
                        {
                            rectsWhereRegions = rectangleTracker.CreateCorrectionBySpeedOfRects();
                        }
                        else
                        {
                            rectsWhereRegions = rectangleTracker.CreateRawRects();
                        }

                        if (displayDetectedFaceRect)
                            DrawDownScaleFaceRects(rgbaMat, rectsWhereRegions, imageOptimizationHelper.downscaleRatio, COLOR_GREEN, 1);
                    }

                    detectedObjectsInRegions.Clear();
                    int len = rectsWhereRegions.Length;
                    for (int i = 0; i < len; i++)
                    {
                        if (useOpenCVDetector)
                        {
                            DetectInRegion(downScaleGrayMat, rectsWhereRegions[i], detectedObjectsInRegions, cascade, true);
                        }
                        else
                        {
                            DetectInRegion(downScaleGrayMat, rectsWhereRegions[i], detectedObjectsInRegions, faceLandmarkDetector);
                        }
                    }

                    rectangleTracker.UpdateTrackedObjects(detectedObjectsInRegions);
                    rectangleTracker.GetObjects(resultObjects, false);

                    // set original size image
                    OpenCVForUnityUtils.SetImage(faceLandmarkDetector, grayMat);

                    len = resultObjects.Count;
                    for (int i = 0; i < len; i++)
                    {
                        Rect rect = resultObjects[i];

                        // restore to original size rect
                        float downscaleRatio = imageOptimizationHelper.downscaleRatio;
                        rect.x = (int)(rect.x * downscaleRatio);
                        rect.y = (int)(rect.y * downscaleRatio);
                        rect.width = (int)(rect.width * downscaleRatio);
                        rect.height = (int)(rect.height * downscaleRatio);

                        // detect face landmark points
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height));

                        // draw face landmark points
                        OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                        if (displayDetectedFaceRect) {
                            //draw face rect
                            OpenCVForUnityUtils.DrawFaceRect(rgbaMat, new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), COLOR_RED, 2);
                        }
                    }
                }
                
                Utils.fastMatToTexture2D(rgbaMat, texture);
            }

            if (webCamTextureToMatHelper.IsPlaying()) {

                Matrix4x4 cameraToWorldMatrix = Camera.main.cameraToWorldMatrix;
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }
        }
        #endif

        private void StartThread (Action action)
        {
            #if WINDOWS_UWP || (!UNITY_WSA_10_0 && (NET_4_6 || NET_STANDARD_2_0))
            System.Threading.Tasks.Task.Run(() => action());
            #else
            ThreadPool.QueueUserWorkItem(_ => action());
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

        private void ThreadWorker ()
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

            lock (ExecuteOnMainThread) {
                if (ExecuteOnMainThread.Count == 0) {
                    ExecuteOnMainThread.Enqueue(() => {
                        OnDetectionDone();
                    });
                }
            }

            isThreadRunning = false;
        }
        
        private void DetectObject (Mat img, out List<Rect> detectedObjects, FaceLandmarkDetector landmarkDetector)
        {
            OpenCVForUnityUtils.SetImage(landmarkDetector, img);

            List<UnityEngine.Rect> detectResult = landmarkDetector.Detect();

            detectedObjects = new List<Rect>();

            int len = detectResult.Count;
            for (int i = 0; i < len; i++)
            {
                UnityEngine.Rect r = detectResult[i];
                detectedObjects.Add(new Rect((int)r.x, (int)r.y, (int)r.width, (int)r.height));
            }
        }

        private void DetectObject (Mat img, out List<Rect> detectedObjects, CascadeClassifier cascade, bool correctToDlibResult = false)
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

        private void OnDetectionDone ()
        {
            hasUpdatedDetectionResult = true;

            isDetecting = false;
        }

        private void DetectInRegion (Mat img, Rect region, List<Rect> detectedObjectsInRegions, FaceLandmarkDetector landmarkDetector)
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

                OpenCVForUnityUtils.SetImage(landmarkDetector, img1);

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

        private void DetectInRegion (Mat img, Rect region, List<Rect> detectedObjectsInRegions, CascadeClassifier cascade, bool correctToDlibResult = false)
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

        private void DrawDownScaleFaceRects (Mat img, Rect[] rects, float downscaleRatio, Scalar color, int thickness)
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


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            imageOptimizationHelper.Dispose ();
            #if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            if (faceLandmarkDetector4Thread != null)
                faceLandmarkDetector4Thread.Dispose();

            if (rectangleTracker != null)
                rectangleTracker.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            LoadScene ("HoloLensWithDlibFaceLandmarkDetectorExample");
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
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
        }

        /// <summary>
        /// Raises the use separate detection toggle value changed event.
        /// </summary>
        public void OnUseSeparateDetectionToggleValueChanged ()
        {
            useSeparateDetection = useSeparateDetectionToggle.isOn;

            lock (rectangleTracker) {
                if (rectangleTracker != null)
                    rectangleTracker.Reset ();
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
        public void OnDisplayCameraImageToggleValueChanged ()
        {
            displayCameraImage = displayCameraImageToggle.isOn;
        }

        /// <summary>
        /// Raises the display detected face rect toggle value changed event.
        /// </summary>
        public void OnDisplayDetectedFaceRectToggleValueChanged ()
        {
            displayDetectedFaceRect = displayDetectedFaceRectToggle.isOn;
        }
    }
}