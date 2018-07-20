using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    public class ShowLicense : ExampleSceneBase
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

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            LoadScene ("HoloLensWithDlibFaceLandmarkDetectorExample");
        }
    }
}
