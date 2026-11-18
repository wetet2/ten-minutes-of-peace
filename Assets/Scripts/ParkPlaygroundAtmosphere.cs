using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark
{
    public sealed class ParkPlaygroundAtmosphere : MonoBehaviour
    {
        Light sun;
        Quaternion sunRotation;
        Color sunColor,sky,equator,ground;
        float intensity,shadowStrength,shadowDistance;
        AmbientMode mode;

        void OnEnable()
        {
            mode=RenderSettings.ambientMode;
            sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
            shadowDistance=QualitySettings.shadowDistance;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.52f,.60f,.72f);
            RenderSettings.ambientEquatorColor=new Color(.43f,.45f,.46f);
            RenderSettings.ambientGroundColor=new Color(.26f,.25f,.23f);
            QualitySettings.shadowDistance=85;
            sun=RenderSettings.sun;
            if(!sun)return;
            sunRotation=sun.transform.rotation;sunColor=sun.color;intensity=sun.intensity;shadowStrength=sun.shadowStrength;
            sun.transform.rotation=Quaternion.Euler(49,-35,0);sun.color=new Color(1,.98f,.95f);sun.intensity=1.05f;sun.shadowStrength=.78f;
        }

        void OnDisable()
        {
            RenderSettings.ambientMode=mode;
            RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            QualitySettings.shadowDistance=shadowDistance;
            if(!sun)return;
            sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=intensity;sun.shadowStrength=shadowStrength;
        }
    }
}
