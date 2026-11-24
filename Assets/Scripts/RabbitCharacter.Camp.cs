using UnityEngine;

namespace PeacePark
{
    public sealed partial class RabbitCharacter
    {
        bool campCrouched;

        internal Color CampingCushionColor(Color fallback)
        {
            var color = fallback;
            float best = .15f;
            for (int row = 1; row <= 3; row++)
            for (int column = 1; column <= 3; column++)
            {
                var sample = frontTexture.GetPixelBilinear(column * .25f, row * .25f);
                Color.RGBToHSV(sample, out _, out float saturation, out float value);
                float score = saturation * Mathf.Lerp(.35f, 1, value);
                if (score <= best) continue;
                color = Color.Lerp(sample, new Color(1, .92f, .80f), .18f);
                best = score;
            }
            return color;
        }

        internal void FitCampTent(float openingHeight, float weight)
        {
            float height = mesh.bounds.size.y * transform.lossyScale.y;
            float fit = Mathf.Lerp(1, Mathf.Min(1, openingHeight / height), weight);
            model.localScale = new Vector3(Mathf.Lerp(1, fit, .70f), fit, fit);
            model.localPosition = Vector3.Scale(model.localPosition, model.localScale);
            body.localRotation = Quaternion.Euler(12 * weight, 0, 0);
            campCrouched = true;
        }

        internal void PlayCampSettlePose(Vector3 cushion, float unfurl, float seated)
        {
            PlayRideSeatPose(seated);
            float bend = Mathf.Sin(unfurl * Mathf.PI);
            body.localRotation = Quaternion.Euler(29 * bend - 5 * seated, 0, 0);
            model.localPosition -= Vector3.up * (.19f * bend);
            AimHand(leftArm, -1, cushion - transform.right * .28f, bend);
            AimHand(rightArm, 1, cushion + transform.right * .28f, bend);
        }

        internal void PlayCampPicnicPose(float time, Vector3 fire)
        {
            PlayRideSeatPose(1);
            head.localRotation = Quaternion.Euler(5, 13 * Mathf.Sin(time * 1.75f), 3 * Mathf.Sin(time * 2.1f));
            float cycle = Mathf.Repeat(time, 5.1f);
            float warming = Mathf.SmoothStep(0, 1, cycle - 2.05f) * Mathf.SmoothStep(0, 1, 4.50f - cycle);
            if (warming <= 0) return;
            var warmth = fire + Vector3.up * .55f;
            AimHand(leftArm, -1, warmth - transform.right * .26f, warming * .72f);
            AimHand(rightArm, 1, warmth + transform.right * .26f, warming * .72f);
        }

        internal Vector3 CampSnackHand(Vector3 meetingPoint, float reach, float eating)
        {
            bool useRight = Vector3.Dot(transform.right, meetingPoint - transform.position) > 0;
            var arm = useRight ? rightArm : leftArm;
            int side = useRight ? 1 : -1;
            var mouth = head.TransformPoint(new Vector3(0, .12f, -.66f));
            AimHand(arm, side, Vector3.Lerp(meetingPoint, mouth, eating), reach);
            head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(19, 0, 0), eating);
            return arm.TransformPoint(new Vector3(side * .70f, -.24f, -.025f));
        }
    }
}
