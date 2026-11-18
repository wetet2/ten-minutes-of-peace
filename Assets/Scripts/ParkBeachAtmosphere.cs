using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark
{
    public sealed class ParkBeachAtmosphere : MonoBehaviour
    {
        Light sun;
        Quaternion sunRotation;
        Color sunColor,sky,equator,ground;
        float intensity,shadowStrength;
        AmbientMode mode;
        public Material SkyMaterial;
        Material previousSkybox;
        Camera sceneCamera;
        CameraClearFlags previousClearFlags;
        float previousFarClip;
        float previousShadowDistance;

        void OnEnable()
        {
            sun=RenderSettings.sun;
            previousSkybox=RenderSettings.skybox;
            previousShadowDistance=QualitySettings.shadowDistance;
            QualitySettings.shadowDistance=100;
            sceneCamera=Camera.main;
            if(sceneCamera){previousClearFlags=sceneCamera.clearFlags;previousFarClip=sceneCamera.farClipPlane;sceneCamera.clearFlags=CameraClearFlags.Skybox;sceneCamera.farClipPlane=420;}
            if(SkyMaterial)RenderSettings.skybox=SkyMaterial;
            mode=RenderSettings.ambientMode;
            sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.43f,.59f,.70f);
            RenderSettings.ambientEquatorColor=new Color(.22f,.31f,.25f);
            RenderSettings.ambientGroundColor=new Color(.18f,.14f,.10f);
            if(!sun)return;
            sunRotation=sun.transform.rotation;sunColor=sun.color;intensity=sun.intensity;shadowStrength=sun.shadowStrength;
            sun.transform.rotation=Quaternion.Euler(50,-42,0);sun.color=new Color(1,.95f,.83f);sun.intensity=1.18f;sun.shadowStrength=.78f;
        }

        void OnDisable()
        {
            RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;
            RenderSettings.skybox=previousSkybox;
            QualitySettings.shadowDistance=previousShadowDistance;
            if(sceneCamera){sceneCamera.clearFlags=previousClearFlags;sceneCamera.farClipPlane=previousFarClip;}
            RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            if(!sun)return;
            sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=intensity;sun.shadowStrength=shadowStrength;
        }
    }
}
