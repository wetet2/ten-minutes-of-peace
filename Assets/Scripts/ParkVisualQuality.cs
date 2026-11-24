using UnityEngine;

namespace PeacePark
{
    // Apply the preview's quality in Play mode as well as in a standalone player.
    public sealed class ParkVisualQuality : MonoBehaviour
    {
        bool showPerformance;
        bool highQuality;
        float averageFrameTime;
#if UNITY_ANDROID && !UNITY_EDITOR
        static bool androidFullScreen=true;
#else
        static Vector2Int windowSize=new Vector2Int(1600,900);
#endif
        void OnEnable()
        {
            QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowCascades=2;
            QualitySettings.shadowDistance=45;
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            QualitySettings.vSyncCount=1;
            Application.targetFrameRate=60;
            ApplyQuality();
        }
        void ApplyQuality()
        {
            QualitySettings.antiAliasing=highQuality?4:2;
            QualitySettings.shadowResolution=highQuality?ShadowResolution.VeryHigh:ShadowResolution.High;
            if(RenderSettings.sun)RenderSettings.sun.shadowCustomResolution=highQuality?4096:2048;
        }
        void Update()
        {
            averageFrameTime=Mathf.Lerp(averageFrameTime,Time.unscaledDeltaTime,.05f);
            if(Input.GetKeyDown(KeyCode.F3))showPerformance=!showPerformance;
            if(Input.GetKeyDown(KeyCode.F10))
            {
                highQuality=!highQuality;
                ApplyQuality();
            }
            if(Input.GetKeyDown(KeyCode.F11))ToggleFullScreen();
        }
        internal static bool IsFullScreen
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return androidFullScreen;
#else
                return Screen.fullScreen;
#endif
            }
        }
        internal static void ToggleFullScreen()
        {
            if(Application.isEditor)return;
#if UNITY_ANDROID && !UNITY_EDITOR
            androidFullScreen=!androidFullScreen;
            Screen.SetResolution(Screen.width,Screen.height,androidFullScreen);
#else
            if(IsFullScreen)
            {
                Screen.SetResolution(windowSize.x,windowSize.y,FullScreenMode.Windowed);
                return;
            }
            windowSize=new Vector2Int(Screen.width,Screen.height);
            var display=Screen.currentResolution;
            Screen.SetResolution(display.width,display.height,FullScreenMode.FullScreenWindow);
#endif
        }
        void OnGUI()
        {
            if(!showPerformance)return;
            var old=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            string quality=highQuality?"High":"Balanced";
            GUI.Box(new Rect(16,Screen.height-80,455,36),$"{Screen.width} x {Screen.height}  |  {1/Mathf.Max(.0001f,averageFrameTime):F0} FPS  |  {quality}  |  F10 Quality");
            GUI.matrix=old;
        }
    }
}
