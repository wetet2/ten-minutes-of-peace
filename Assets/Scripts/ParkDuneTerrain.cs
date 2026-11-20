using UnityEngine;

namespace PeacePark
{
    public sealed class ParkDuneTerrain : MonoBehaviour
    {
        public static readonly Vector2 OasisCenter = new Vector2(0, -.7f);
        public const float OasisScale = 1.4f;
        public const float OasisBankRadius = 1.25f;
        public static readonly Vector2 OasisRadii = new Vector2(4.35f, 3.1f) * OasisScale;
        public static readonly Vector2 SledDuneCrest = new Vector2(-33.8f, 26.8f);
        public static readonly Vector2 SledLanding = new Vector2(-12.4f, -13.15f);
        public static readonly Vector2 SledDescentDirection = (SledLanding - SledDuneCrest).normalized;
        public const float SledDuneHeight = 16.4f;

        // Center X/Z, footprint radii X/Z, and crest height.
        static readonly (float X, float Z, float Width, float Depth, float Height)[] Dunes = {
            (-12.4f, 24f, 14.2f, 9.8f, 7f), (11.2f, 24.6f, 14.6f, 10.2f, 6.8f),
            (-23f, 31f, 17f, 11f, 4.6f), (24f, 32f, 17f, 12f, 5.1f),
            (-30f, 11f, 10.5f, 16.5f, 3.8f), (29.5f, 7.5f, 11f, 16f, 4.2f),
            (-28f, -12f, 12f, 13.5f, .55f), (29f, -15f, 12f, 15f, 2.9f),
            (-22f, -27f, 16f, 11f, 2.5f), (22f, -27f, 15f, 11f, 2.7f),
            (-45f, 39f, 22f, 16f, 6.8f), (37f, 46f, 24f, 17f, 7.5f), (-3f, 53f, 25f, 13f, 7.2f),
            // Low shoulders frame the sandy gap; the second ridge closes the southern horizon.
            (-8f, -32f, 17f, 10f, 3.8f), (16f, -36f, 18f, 12f, 4.3f),
            (-43f, -49f, 28f, 14f, 7.4f), (3f, -55f, 29f, 14f, 8.2f),
            (41f, -50f, 24f, 17f, 6.7f)
        };

        static readonly float DescentLength = Vector2.Distance(SledDuneCrest, SledLanding);
        static readonly float CrestHeight = NaturalHeight(SledDuneCrest.x, SledDuneCrest.y);
        static readonly float LandingHeight = NaturalHeight(SledLanding.x, SledLanding.y);

        public static float Height(float x, float z)
        {
            const float endPadding = 1.6f, endBlend = 3.5f;
            float height = NaturalHeight(x, z);
            var offset = new Vector2(x, z) - SledDuneCrest;
            float along = Vector2.Dot(offset, SledDescentDirection);
            if (along <= -endPadding - endBlend || along >= DescentLength + endPadding + endBlend) return height;
            float across = Mathf.Abs(offset.x * SledDescentDirection.y - offset.y * SledDescentDirection.x);
            if (across >= 6.2f) return height;

            // Keep the sled lane level across both runners and blend its shoulders into the dune.
            float width = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2.1f, 6.2f, across));
            // The whole sled still needs even ground when its center is at either end.
            float beforeStart = Mathf.InverseLerp(-endPadding - endBlend, -endPadding, along);
            float afterFinish = Mathf.InverseLerp(DescentLength + endPadding, DescentLength + endPadding + endBlend, along);
            float ends = Mathf.SmoothStep(0, 1, beforeStart) * (1 - Mathf.SmoothStep(0, 1, afterFinish));
            float grade = Mathf.SmoothStep(CrestHeight, LandingHeight, along / (DescentLength * .75f));
            return Mathf.Lerp(height, grade, width * ends);
        }

        static float NaturalHeight(float x, float z)
        {
            float height = .09f * Mathf.Sin(x * .23f + z * .08f) * Mathf.Sin(z * .29f);
            float sledAlong = (z - SledDuneCrest.y) / 32.2f;
            float sledAcross = (x - SledDuneCrest.x + .09f * (z - SledDuneCrest.y)) / 22;
            float sledFootprint = 1 - sledAlong * sledAlong - sledAcross * sledAcross;
            if (sledFootprint > 0) height += SledDuneHeight * Mathf.Pow(sledFootprint, 2.4f);
            foreach (var dune in Dunes)
            {
                float along = (z - dune.Z) / dune.Depth;
                float across = (x - dune.X + .28f * (z - dune.Z)) / dune.Width;
                float weight = Mathf.Max(0, 1 - along * along - across * across);
                height += dune.Height * weight * weight * weight;
            }
            var oasis = new Vector2((x - OasisCenter.x) / OasisRadii.x, (z - OasisCenter.y) / OasisRadii.y);
            if (oasis.sqrMagnitude < 1.7f)
            {
                float radius = oasis.magnitude + .052f * Mathf.Sin(x * .9f + z * .49f) + .031f * Mathf.Cos(z * 1.3f);
                height -= .72f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.67f, 1.22f, radius)));
            }
            return height;
        }

        internal float SurfaceHeight(Vector3 worldPosition)
        {
            var point = transform.InverseTransformPoint(worldPosition);
            point.y = Height(point.x, point.z);
            return transform.TransformPoint(point).y;
        }
    }
}
