using UnityEngine;

namespace PeacePark
{
    public sealed class ParkDuneTerrain : MonoBehaviour
    {
        public static readonly Vector2 OasisCenter = new Vector2(0, -.7f);
        public static readonly Vector2 OasisRadii = new Vector2(4.35f, 3.1f);

        // Center X/Z, footprint radii X/Z, and crest height.
        static readonly (float X, float Z, float Width, float Depth, float Height)[] Dunes = {
            (-12.4f, 24f, 14.2f, 9.8f, 7f), (11.2f, 24.6f, 14.6f, 10.2f, 6.8f),
            (-23f, 31f, 17f, 11f, 4.6f), (24f, 32f, 17f, 12f, 5.1f),
            (-30f, 11f, 10.5f, 16.5f, 3.8f), (29.5f, 7.5f, 11f, 16f, 4.2f),
            (-28f, -12f, 12f, 13.5f, 2.7f), (29f, -15f, 12f, 15f, 2.9f),
            (-22f, -27f, 16f, 11f, 2.5f), (22f, -27f, 15f, 11f, 2.7f),
            (-45f, 39f, 22f, 16f, 6.8f), (37f, 46f, 24f, 17f, 7.5f), (-3f, 53f, 25f, 13f, 7.2f)
        };

        public static float Height(float x, float z)
        {
            float height = .09f * Mathf.Sin(x * .23f + z * .08f) * Mathf.Sin(z * .29f);
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
