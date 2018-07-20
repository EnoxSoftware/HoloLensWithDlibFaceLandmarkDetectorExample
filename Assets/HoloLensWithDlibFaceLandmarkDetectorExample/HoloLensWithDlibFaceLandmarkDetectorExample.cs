using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    public class HoloLensWithDlibFaceLandmarkDetectorExample : ExampleSceneBase
    {
        // Use this for initialization
        protected override void Start ()
        {
            base.Start ();
            
        }
        
        // Update is called once per frame
        void Update ()
        {
            
        }

        public void OnShowLicenseButtonClick ()
        {
            LoadScene ("ShowLicense");
        }

        public void OnHoloLensPhotoCaptureExampleButtonClick ()
        {
            LoadScene ("HoloLensPhotoCaptureExample");
        }
        
        public void OnHoloLensFaceLandmarkDetectionExampleButtonClick ()
        {
            LoadScene ("HoloLensFaceLandmarkDetectionExample");
        }

        public void OnHoloLensARHeadExampleButtonClick ()
        {
            LoadScene ("HoloLensARHeadExample");
        }
    }
}