using UnityEngine;

namespace PeacePark
{
    // Apply the preview's quality in Play mode as well as in a standalone player.
    public sealed class ParkVisualQuality : MonoBehaviour
    {
        bool showPerformance;
        bool highQuality;
        float averageFrameTime;
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
            if(!Application.isEditor&&Input.GetKeyDown(KeyCode.F11))
            {
                if(Screen.fullScreen)Screen.SetResolution(1600,900,FullScreenMode.Windowed);
                else{var display=Screen.currentResolution;Screen.SetResolution(display.width,display.height,FullScreenMode.FullScreenWindow);}
            }
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
