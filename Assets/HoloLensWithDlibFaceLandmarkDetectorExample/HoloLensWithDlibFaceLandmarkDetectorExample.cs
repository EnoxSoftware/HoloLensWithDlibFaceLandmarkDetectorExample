using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    public class HoloLensWithDlibFaceLandmarkDetectorExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            
        }
        
        // Update is called once per frame
        void Update ()
        {
            
        }

        public void OnShowLicenseButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnHoloLensPhotoCaptureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoloLensPhotoCaptureExample");
            #else
            Application.LoadLevel ("HoloLensPhotoCaptureExample");
            #endif
        }

        public void OnHoloLensWebCamTextureExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoloLensWebCamTextureExample");
            #else
            Application.LoadLevel ("HoloLensWebCamTextureExample");
            #endif
        }
        
        public void OnHoloLensFaceLandmarkDetectionExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoloLensFaceLandmarkDetectionExample");
            #else
            Application.LoadLevel ("HoloLensFaceLandmarkDetectionExample");
            #endif
        }

        public void OnHoloLensARHeadExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoloLensARHeadExample");
            #else
            Application.LoadLevel ("HoloLensARHeadExample");
            #endif
        }
    }
}