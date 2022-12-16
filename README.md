# HoloLens With DlibFaceLandmarkDetector Example


## What's new
* Support for Hololens1 and Hololens2. (XR System: Legacy Built-in XR / XR Plugin Management WindowsMR / XR Plugin Management OpenXR)


## Demo Video (old version)
[![](http://img.youtube.com/vi/egMZB_3DGVc/0.jpg)](https://youtu.be/egMZB_3DGVc)


## Environment
* Hololens1 10.0.17763.3532 / Hololens2 20348.1522
* Windows 10 SDK 10.0.19041.0
* Visual Studio 2019
* Unity 2019.4.31f1 / 2020.3.38f1
* [Microsoft Mixed Reality Toolkit](https://github.com/Microsoft/MixedRealityToolkit-Unity/releases) v2.8.2  
* [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?aid=1011l4ehR) 2.5.0+ 
* [Dlib FaceLandmarkDetector](https://assetstore.unity.com/packages/tools/integration/dlib-facelandmark-detector-64314?aid=1011l4ehR) 1.3.4+ 
* [EnoxSoftware/HoloLensCameraStream](https://github.com/EnoxSoftware/HoloLensCameraStream)


## Setup
1. Download the latest release unitypackage. [HoloLensWithDlibFaceLandmarkDetectorExample.unitypackage](https://github.com/EnoxSoftware/HoloLensWithDlibFaceLandmarkDetectorExample/releases)
1. Create a new project. (`HoloLensWithDlibFaceLandmarkDetectorExample`)
    * Change the platform to `UWP` in the "Build Settings" window.
1. Import the OpenCVForUnity.
    * Select MenuItem[Tools/OpenCV for Unity/Open Setup Tools].
    * Click the [Move StreamingAssets Folder] button.
    * Leave the following files and delete the rest. ("StreamingAssets/OpenCVForUnity/objdetect/haarcascade_frontalface_alt.xml", "lbpcascade_ frontalface.xml")
1. Import the DlibFaceLandmarkDetector.
    * Select MenuItem[Tools/Dlib FaceLandmark Detector/Open Setup Tools].
    * Click the [Move StreamingAssets Folder] button.
    * Leave the following files and delete the rest. ("StreamingAssets/DlibFaceLandmarkDetector/sp_human_face_68.dat", "sp_human_face_68_for_mobile.dat", "sp_human_face_17.dat", "sp_human_face_17_for_mobile.dat" and "sp_human_face_6.dat")
1. Clone HoloLensCameraStream repository.
    * Copy the "HoloLensCameraStream/HoloLensVideoCaptureExample/Assets/CamStream/" folder to the "Assets/" folder.
1. Import the Microsoft Mixed Reality Toolkit. (Recommend using [MixedRealityFeatureTool](https://www.microsoft.com/en-us/download/details.aspx?id=102778))
    * Follow the MRTK2 configuration dialog to set up the project.
1. Import the HoloLensWithDlibFaceLandmarkDetectorExample.unitypackage.
1. Add the "Assets/HoloLensWithDlibFaceLandmarkDetectorExample/*.unity" files to the "Scenes In Build" list in the "Build Settings" window.
1. Configure settings in the "Project Settings" window.
    * Add Define Symbols: the following to `Scripting Define Symbols` depending on the XR system used in your project; Legacy built-in XR: `BUILTIN_XR`; XR Plugin Management (Windows Mixed Reality): `XR_PLUGIN_WINDOWSMR`; XR Plugin Management (OpenXR):`XR_PLUGIN_OPENXR`.
    * Enable `WebCam` Capabilties in Publishing settings tab.
1. (Optional) Setup a performance environment suitable for Holorens. (See [https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/recommended-settings-for-unity](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/recommended-settings-for-unity))
1. **Build the project:** You can now build the Unity project, which generates a Visual Studio Solution (which you will then have to also build). With the Build Settings window still open, click **Build**. In the explorer window that appears, make a new folder called `App`, which should live as a sibling next to the 'Assets` folder. Then click Select Folder to generate the VS solution in that folder. Then wait for Unity to build the solution.
1. **Open the VS Solution:** When the solution is built, a Windows explorer folder will open. Open the newly-built VS solution, which lives in `App/HoloLensWithDlibFaceLandmarkDetectorExample.sln`. This is the solution that ultimately gets deployed to your HoloLens.
1. **Configure the deploy settings:** In the Visual Studio toolbar, change the solution platform from `ARM` to `x86` if you are building for Hololens1 or to `ARM64` if you are building for Hololens2; Change the deploy target (the green play button) to `Device` (if your HoloLens is plugged into your computer), or `Remote Machine` (if your HoloLens is connected via WiFi).
1. **Run the app:** Go to **Debug > Start Debugging**. Once the app is deployed to the HoloLens, you should see some confirmation output in the Output window.

|Project Assets|Build Settings|
|---|---|
|![ProjectAssets.jpg](ProjectAssets.jpg)|![BuildSettings.jpg](BuildSettings.jpg)|

|Player Settings|
|---|
|![PlayerSettings.jpg](PlayerSettings.jpg)|


## ScreenShot (old version)
![screenshot01.jpg](screenshot01.jpg) 

![screenshot02.jpg](screenshot02.jpg) 

![screenshot03.jpg](screenshot03.jpg) 

