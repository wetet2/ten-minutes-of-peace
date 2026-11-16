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

        void OnEnable()
        {
            sun=RenderSettings.sun;
            mode=RenderSettings.ambientMode;
            sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.45f,.58f,.67f);
            RenderSettings.ambientEquatorColor=new Color(.25f,.33f,.28f);
            RenderSettings.ambientGroundColor=new Color(.20f,.17f,.12f);
            if(!sun)return;
            sunRotation=sun.transform.rotation;sunColor=sun.color;intensity=sun.intensity;shadowStrength=sun.shadowStrength;
            sun.transform.rotation=Quaternion.Euler(58,-35,0);sun.color=new Color(1,.95f,.83f);sun.intensity=1.05f;sun.shadowStrength=.76f;
        }

        void OnDisable()
        {
            RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;
            RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            if(!sun)return;
            sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=intensity;sun.shadowStrength=shadowStrength;
        }
    }
}
