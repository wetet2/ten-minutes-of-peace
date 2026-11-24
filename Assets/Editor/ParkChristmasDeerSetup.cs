using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkChristmasDeerSetup
    {
        const string ModelPath = "Assets/ithappy/Animals_FREE/Meshes/Deer_001.fbx";
        // Average backward travel speed of the grounded hooves in the original one-second walk clip.
        const float UnscaledStrideSpeed = .99f;

        internal static ParkVillageDeer[] AddDeer(Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!source) return System.Array.Empty<ParkVillageDeer>();
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().ToArray();
            var idle = clips.FirstOrDefault(clip => clip.name == "Deer_001_idle");
            var walk = clips.FirstOrDefault(clip => clip.name == "Deer_001_walk");
            if (!idle || !walk) return System.Array.Empty<ParkVillageDeer>();
            Mesh clothMesh = null;
            Material clothMaterial = null;
            return new[] {
                Create("West plaza buck", 3.02f, .06f, PlazaLoop(-1)),
                Create("East plaza buck", 2.86f, .36f, PlazaLoop(1)),
                Create("Central plaza buck", 3.12f, .62f, new[]{new Vector2(-1.92f,-3.65f),new Vector2(-1.9f,.25f),
                    new Vector2(-1.35f,2.48f),new Vector2(1.58f,2.58f),new Vector2(2.03f,.20f),new Vector2(1.72f,-3.45f)})
            };

            ParkVillageDeer Create(string name, float height, float phase, Vector2[] route)
            {
                var deer = Group(name, parent, Vector3.zero);
                var model = Object.Instantiate(source, deer);
                model.name = "ithappy Deer";
                var rig = model.GetComponent<Animator>();
                if (!rig) rig = model.AddComponent<Animator>();
                rig.runtimeAnimatorController = null;
                rig.applyRootMotion = false;
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                var bones = model.GetComponentsInChildren<Transform>();
                var front = HipCenter("front_thigh");
                var rear = HipCenter("thigh");
                var forward = Vector3.ProjectOnPlane(front - rear, Vector3.up);
                model.transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(forward));
                var baked = new Mesh();
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = deer.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                    minY = Mathf.Min(minY, vertices[i].y);
                    maxY = Mathf.Max(maxY, vertices[i].y);
                }
                var back = deer.InverseTransformPoint(Vector3.Lerp(HipCenter("thigh"), HipCenter("front_thigh"), .49f));
                var triangles = baked.triangles;
                back.y = BackHeight(vertices, triangles, back, back.y + (maxY - minY) * .12f);
                Object.DestroyImmediate(baked);
                float scale = height / (maxY - minY);
                if (!clothMesh)
                {
                    float torsoLength = deer.InverseTransformVector(forward).magnitude;
                    clothMesh = MeshWithClothCoordinates(skin.sharedMesh, vertices, back, torsoLength, scale);
                    var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/ithappy/Animals_FREE/Materials/Material.mat");
                    clothMaterial = new Material(original) { name = "Deer with red riding cloth",
                        shader = Shader.Find("PeacePark/DeerCloth") };
                }
                skin.sharedMesh = clothMesh;
                skin.sharedMaterial = clothMaterial;
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = Vector3.down * minY * scale;
                var seat = Group("Deer back anchor", deer, (back - Vector3.up * minY) * scale);
                var backBone = bones.Where(bone => bone.name.StartsWith("spine", System.StringComparison.Ordinal))
                    .OrderBy(bone => (bone.position - seat.position).sqrMagnitude).FirstOrDefault();
                if (backBone) seat.SetParent(backBone, true);
                var animal = deer.gameObject.AddComponent<ParkVillageDeer>();
                animal.Configure(rig, idle, walk, seat, route, UnscaledStrideSpeed * scale, phase);
                return animal;

                Vector3 HipCenter(string name) => (bones.First(bone => bone.name == name + ".L").position +
                    bones.First(bone => bone.name == name + ".R").position) * .5f;
            }
        }

        static Mesh MeshWithClothCoordinates(Mesh original, Vector3[] body, Vector3 back, float torsoLength, float scale)
        {
            // Store the rectangular cloth's projection on the skin in the rest pose.
            // Skinning carries these coordinates through the same deformation as the body.
            var coordinates = new List<Vector2>(body.Length);
            foreach (var vertex in body)
            {
                var offset = vertex - back;
                coordinates.Add(new Vector2(offset.z / (torsoLength * .32f),
                    (offset.y * scale + .10f) / .32f));
            }
            var mesh = Object.Instantiate(original);
            mesh.name = "Deer skin with red cloth coordinates";
            mesh.SetUVs(1, coordinates);
            return mesh;
        }

        static Vector2[] PlazaLoop(int side) => new[] {
            new Vector2(4.48f*side,-1.25f),new Vector2(4.68f*side,1.7f),new Vector2(6.52f*side,2.35f),
            new Vector2(9.25f*side,1.78f),new Vector2(9.35f*side,-3.75f),
            new Vector2(6.93f*side,-5.14f),new Vector2(4.62f*side,-4.79f)
        };

        static float BackHeight(Vector3[] vertices, int[] triangles, Vector3 point, float fallback)
        {
            float height = float.NegativeInfinity;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var ab = vertices[triangles[i + 1]] - a;
                var ac = vertices[triangles[i + 2]] - a;
                var d = point - a;
                float area = ab.x * ac.z - ab.z * ac.x;
                if (Mathf.Abs(area) < .000001f) continue;
                float u = (d.x * ac.z - d.z * ac.x) / area;
                float v = (ab.x * d.z - ab.z * d.x) / area;
                if (u >= 0 && v >= 0 && u + v <= 1) height = Mathf.Max(height, a.y + ab.y * u + ac.y * v);
            }
            return float.IsNegativeInfinity(height) ? fallback : height;
        }
    }
}
