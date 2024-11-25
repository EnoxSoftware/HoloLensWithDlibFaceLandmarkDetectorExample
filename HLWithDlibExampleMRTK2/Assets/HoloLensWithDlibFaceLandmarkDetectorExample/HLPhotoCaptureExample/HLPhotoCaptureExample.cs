using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System.Collections;
using System.Linq;
using System.Threading;

#if UNITY_2018_2_OR_NEWER
using UnityEngine.Windows.WebCam;
using WSAWebCamCameraParameters = UnityEngine.Windows.WebCam.CameraParameters;
#else
using UnityEngine.XR.WSA.WebCam;
using WSAWebCamCameraParameters = UnityEngine.XR.WSA.WebCam.CameraParameters;
#endif

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLens PhotoCapture Example
    /// An example of holographic photo blending using the PhotocCapture class on Hololens.
    /// Referring to https://github.com/microsoft/OpenXR-Unity-MixedReality-Samples/blob/main/BasicSample/Assets/LocatableCamera/Scripts/LocatableCamera.cs
    /// </summary>
    public class HLPhotoCaptureExample : MonoBehaviour
    {
        [SerializeField]
        private Shader textureShader = null;

        [SerializeField]
        private Text text = null;

        private PhotoCapture photoCaptureObject = null;
        private Resolution cameraResolution = default(Resolution);
        private bool isCapturingPhoto, isReadyToCapturePhoto = false;
        private uint numPhotos = 0;

        private FaceLandmarkDetector faceLandmarkDetector;
        private string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";
        private string dlibShapePredictorFilePath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (text != null)
            {
                text.text = "Preparing file access...";
            }

            dlibShapePredictorFileName = HoloLensWithDlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
            string dlibShapePredictor_filepath = await DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsyncTask(dlibShapePredictorFileName, cancellationToken: cts.Token);

            if (text != null)
            {
                text.text = "";
            }

            if (string.IsNullOrEmpty(dlibShapePredictor_filepath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }
            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictor_filepath);


            var resolutions = PhotoCapture.SupportedResolutions;
            if (resolutions == null || resolutions.Count() == 0)
            {
                if (text != null)
                {
                    text.text = "Resolutions not available. Did you provide web cam access?";
                }
                return;
            }

            cameraResolution = resolutions.OrderByDescending((res) => res.width * res.height).First();
            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);

            if (text != null)
            {
                text.text = "Starting camera...";
            }
        }

        private void OnDestroy()
        {
            isReadyToCapturePhoto = false;

            if (photoCaptureObject != null)
            {
                photoCaptureObject.StopPhotoModeAsync(OnPhotoCaptureStopped);

                if (text != null)
                {
                    text.text = "Stopping camera...";
                }
            }

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            if (cts != null)
                cts.Dispose();
        }

        private void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            if (text != null)
            {
                text.text += "\nPhotoCapture created...";
            }

            photoCaptureObject = captureObject;

            WSAWebCamCameraParameters cameraParameters = new WSAWebCamCameraParameters(WebCamMode.PhotoMode)
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = CapturePixelFormat.BGRA32
            };

            captureObject.StartPhotoModeAsync(cameraParameters, OnPhotoModeStarted);
        }

        private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                isReadyToCapturePhoto = true;

                if (text != null)
                {
                    text.text = "Ready!\nPress button to take a picture.";
                }
            }
            else
            {
                isReadyToCapturePhoto = false;

                if (text != null)
                {
                    text.text = "Unable to start photo mode!";
                }
            }
        }

        /// <summary>
        /// Takes a photo and attempts to load it into the scene using its location data.
        /// </summary>
        public void TakePhoto()
        {
            if (!isReadyToCapturePhoto || isCapturingPhoto)
            {
                return;
            }

            isCapturingPhoto = true;

            if (text != null)
            {
                text.text = "Taking picture...";
            }

            photoCaptureObject.TakePhotoAsync(OnPhotoCaptured);
        }

        private void OnPhotoCaptured(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            if (result.success)
            {
                if (text != null)
                {
                    text.text += "\nTook picture!";
                }

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Photo{numPhotos++}";
                quad.transform.parent = transform;

                float ratio = cameraResolution.height / (float)cameraResolution.width;
                quad.transform.localScale = new Vector3(2f, 2f * ratio, 1);

                Renderer quadRenderer = quad.GetComponent<Renderer>();
                quadRenderer.material = new Material(textureShader);

                Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height, TextureFormat.BGRA32, false);
                photoCaptureFrame.UploadImageDataToTexture(targetTexture);



                Mat bgraMat = new Mat(targetTexture.height, targetTexture.width, CvType.CV_8UC4);

                // For BGRA or BGR format, use the texture2DToMatRaw method.
                OpenCVForUnity.UnityUtils.Utils.texture2DToMatRaw(targetTexture, bgraMat);

                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, bgraMat);

                //detect face
                List<FaceLandmarkDetector.RectDetection> detectResult = faceLandmarkDetector.DetectRectDetection();

                foreach (var r in detectResult)
                {
                    Debug.Log("rect : " + r.rect);

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(r.rect);

                    Debug.Log("face points count : " + points.Count);

                    //draw landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark(bgraMat, points, new Scalar(0, 255, 0, 255), 2);

                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect(bgraMat, r.rect, new Scalar(255, 0, 0, 255), 2);
                }

                // draw an edge lines.
                Imgproc.rectangle(bgraMat, new Point(0, 0), new Point(bgraMat.width(), bgraMat.height()), new Scalar(255, 0, 0, 255), 2);

                Imgproc.putText(bgraMat, targetTexture.format + " W:" + bgraMat.width() + " H:" + bgraMat.height(), new Point(5, bgraMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.5, new Scalar(255, 0, 0, 255), 2, Imgproc.LINE_AA, false);

                // For BGRA or BGR format, use the matToTexture2DRaw method.
                OpenCVForUnity.UnityUtils.Utils.matToTexture2DRaw(bgraMat, targetTexture);
                bgraMat.Dispose();



                quadRenderer.sharedMaterial.SetTexture("_MainTex", targetTexture);

                if (photoCaptureFrame.hasLocationData)
                {
                    photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

                    Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                    Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                    photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);

                    targetTexture.wrapMode = TextureWrapMode.Clamp;

                    quadRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", cameraToWorldMatrix.inverse);
                    quadRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);

                    quad.transform.position = position;
                    quad.transform.rotation = rotation;

                    if (text != null)
                    {
                        text.text += $"\nPosition: ({position.x}, {position.y}, {position.z})";
                        text.text += $"\nRotation: ({rotation.x}, {rotation.y}, {rotation.z}, {rotation.w})";
                    }
                }
                else
                {
                    if (text != null)
                    {
                        text.text += "\nNo location data :(";
                    }
                }
            }
            else
            {
                if (text != null)
                {
                    text.text += "\nPicture taking failed: " + result.hResult;
                }
            }

            isCapturingPhoto = false;
        }

        private void OnPhotoCaptureStopped(PhotoCapture.PhotoCaptureResult result)
        {
            if (text != null)
            {
                text.text = result.success ? "Photo mode stopped." : "Unable to stop photo mode.";
            }

            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("HoloLensWithDlibFaceLandmarkDetectorExample");
        }
    }
}