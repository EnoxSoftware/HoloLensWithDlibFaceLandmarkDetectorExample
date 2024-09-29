using OpenCVForUnity.CoreModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HoloLensWithDlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// HoloLensWithDlibFaceLandmarkDetector Example
    /// </summary>
    public class HoloLensWithDlibFaceLandmarkDetectorExample : MonoBehaviour
    {
        public Text exampleTitle;
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        public enum DlibShapePredictorNamePreset : int
        {
            sp_human_face_68,
            sp_human_face_68_for_mobile,
            sp_human_face_17,
            sp_human_face_17_for_mobile,
            sp_human_face_6,
        }

        public Dropdown dlibShapePredictorNameDropdown;

        static DlibShapePredictorNamePreset dlibShapePredictorName = DlibShapePredictorNamePreset.sp_human_face_17_for_mobile;

        /// <summary>
        /// The name of dlib shape predictor file to use in the example scenes.
        /// </summary>
        public static string dlibShapePredictorFileName
        {
            get
            {
                return "DlibFaceLandmarkDetector/" + dlibShapePredictorName.ToString() + ".dat";
            }
        }

        // Use this for initialization
        protected void Start()
        {
            exampleTitle.text = "HoloLensWithDlibFaceLandmarkDetector Example " + Application.version;

            versionInfo.text = Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnity.UnityUtils.Utils.getVersion() + " (" + Core.VERSION + ")";
            versionInfo.text += " / " + "dlibfacelandmarkdetector" + " " + DlibFaceLandmarkDetector.UnityUtils.Utils.getVersion();
            versionInfo.text += " / UnityEditor " + Application.unityVersion;
            versionInfo.text += " / ";

#if UNITY_EDITOR
            versionInfo.text += "Editor";
#elif UNITY_STANDALONE_WIN
            versionInfo.text += "Windows";
#elif UNITY_STANDALONE_OSX
            versionInfo.text += "Mac OSX";
#elif UNITY_STANDALONE_LINUX
            versionInfo.text += "Linux";
#elif UNITY_ANDROID
            versionInfo.text += "Android";
#elif UNITY_IOS
            versionInfo.text += "iOS";
#elif UNITY_WSA
            versionInfo.text += "WSA";
#elif UNITY_WEBGL
            versionInfo.text += "WebGL";
#endif
            versionInfo.text += " ";
#if ENABLE_MONO
            versionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            versionInfo.text += ".NET";
#endif

            versionInfo.text += " / ";

#if XR_PLUGIN_WINDOWSMR
            versionInfo.text += "XR_PLUGIN_WINDOWSMR";
#elif XR_PLUGIN_OPENXR
            versionInfo.text += "XR_PLUGIN_OPENXR";
#elif BUILTIN_XR
            versionInfo.text += "BUILTIN_XR";
#else
            versionInfo.text += "XR system unknown";
#endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;

            dlibShapePredictorNameDropdown.value = (int)dlibShapePredictorName;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnScrollRectValueChanged()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }


        public void OnShowLicenseButtonClick()
        {
            SceneManager.LoadScene("ShowLicense");
        }

        public void OnHLPhotoCaptureExampleButtonClick()
        {
            SceneManager.LoadScene("HLPhotoCaptureExample");
        }

        public void OnHLFaceLandmarkDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("HLFaceLandmarkDetectionExample");
        }

        public void OnHLARHeadExampleButtonClick()
        {
            SceneManager.LoadScene("HLARHeadExample");
        }


        /// <summary>
        /// Raises the dlib shape predictor name dropdown value changed event.
        /// </summary>
        public void OnDlibShapePredictorNameDropdownValueChanged(int result)
        {
            dlibShapePredictorName = (DlibShapePredictorNamePreset)result;
        }
    }
}