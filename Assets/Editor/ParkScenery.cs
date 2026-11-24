using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PeacePark.Editor
{
    internal static class ParkScenery
    {
        internal static Mesh PrimitiveMesh(PrimitiveType kind)
        {
            var primitive = GameObject.CreatePrimitive(kind);
            var mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
            return mesh;
        }

        internal static Material Paint(string name, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return new Material(Shader.Find("PeacePark/Storybook")) { name = name, color = color };
        }

        internal static Transform Group(string name, Transform parent, Vector3 position)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
        }

        internal static ParkVenue ScaleVenue(ParkVenue venue, Vector3 scale, float viewScale)
        {
            // Geometry and ride paths use venue coordinates. Navigation and camera settings use world coordinates.
            venue.Root.transform.localScale = scale;
            var horizontal = new Vector2(scale.x, scale.z);
            venue.WalkArea = ScaleRect(venue.WalkArea);
            venue.PlazaWalkArea = ScaleRect(venue.PlazaWalkArea);
            for (int i = 0; i < venue.WalkObstacles.Length; i++)
                venue.WalkObstacles[i] = ScaleRect(venue.WalkObstacles[i]);
            venue.PondCenter = Vector2.Scale(venue.PondCenter, horizontal);
            venue.PondRadii = Vector2.Scale(venue.PondRadii, horizontal);
            venue.LawnHeight *= scale.y;
            venue.Focus = Vector3.Scale(venue.Focus, scale);
            venue.CameraPanLimits = Vector2.Scale(venue.CameraPanLimits, horizontal);
            venue.Distance *= viewScale;
            venue.MaxDistance *= viewScale;
            venue.FarClipDistance *= viewScale;
            venue.FogStart *= viewScale;
            venue.FogEnd *= viewScale;
            venue.ShadowDistance *= viewScale;
            return venue;

            Rect ScaleRect(Rect rect) => new Rect(Vector2.Scale(rect.position, horizontal), Vector2.Scale(rect.size, horizontal));
        }

        internal static Transform PlaceOnGround(string name, Mesh mesh, Material material, Transform parent,
            Vector3 ground, float height, Quaternion rotation)
        {
            var bounds = mesh.bounds;
            float scale = height / bounds.size.y;
            var baseCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var item = ParkGeometry.Place(name, mesh, parent, ground - rotation * baseCenter * scale,
                Vector3.one * scale, material);
            item.localRotation = rotation;
            return item;
        }

        internal sealed class Pieces
        {
            readonly Dictionary<Material, List<CombineInstance>> batches = new Dictionary<Material, List<CombineInstance>>();

            public void Add(Mesh mesh, Material material, Vector3 center, Vector3 size, Quaternion? rotation = null)
            {
                if (!batches.TryGetValue(material, out var parts))
                    batches.Add(material, parts = new List<CombineInstance>());
                parts.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(center, rotation ?? Quaternion.identity, size) });
            }

            public void Beam(Mesh mesh, Material material, Vector3 from, Vector3 to, float width)
            {
                var axis = to - from;
                Add(mesh, material, (from + to) / 2, new Vector3(width, axis.magnitude / 2, width),
                    Quaternion.FromToRotation(Vector3.up, axis));
            }

            public void Build(Transform parent)
            {
                foreach (var batch in batches)
                {
                    var mesh = new Mesh { name = parent.name + " - " + batch.Key.name };
                    mesh.CombineMeshes(batch.Value.ToArray());
                    var part = Group(batch.Key.name, parent, Vector3.zero);
                    part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = batch.Key;
                }
            }
        }
    }
}
