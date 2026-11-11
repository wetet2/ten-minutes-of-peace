using UnityEngine;

namespace PeacePark
{
    // Apply the preview's quality in Play mode as well as in a standalone player.
    public sealed class ParkVisualQuality : MonoBehaviour
    {
        void OnEnable()
        {
            QualitySettings.antiAliasing=8;
            QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades=4;
            QualitySettings.shadowDistance=65;
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            Application.targetFrameRate=60;
        }
    }
}
