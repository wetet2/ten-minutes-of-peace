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

        internal static void AddDeer(Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!source) return;
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().ToArray();
            var idle = clips.FirstOrDefault(clip => clip.name == "Deer_001_idle");
            var walk = clips.FirstOrDefault(clip => clip.name == "Deer_001_walk");
            if (!idle || !walk) return;

            Create("West plaza deer", 1.8f, .08f, new[] {
                new Vector2(-5.5f,-7.8f), new Vector2(-7.1f,-9), new Vector2(-8.5f,-7.7f),
                new Vector2(-7,-4.3f), new Vector2(-7.1f,.6f), new Vector2(-5.3f,3.2f), new Vector2(-5.1f,-2.1f) });
            Create("East plaza deer", 1.56f, .13f, new[] {
                new Vector2(5,-6.1f), new Vector2(7.2f,-7.8f), new Vector2(9.8f,-6.7f),
                new Vector2(10.5f,-2.3f), new Vector2(9.9f,2), new Vector2(6.7f,3.8f), new Vector2(5.4f,-.6f) });

            void Create(string name, float height, float phase, Vector2[] route)
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
                foreach (var vertex in vertices)
                {
                    float y = deer.InverseTransformPoint(skin.transform.TransformPoint(vertex)).y;
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
                Object.DestroyImmediate(baked);
                skin.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/ithappy/Animals_FREE/Materials/Material.mat");
                float scale = height / (maxY - minY);
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = Vector3.down * minY * scale;
                deer.gameObject.AddComponent<ParkVillageDeer>().Configure(rig, idle, walk, route, UnscaledStrideSpeed * scale, phase);

                Vector3 HipCenter(string name) => (bones.First(bone => bone.name == name + ".L").position +
                    bones.First(bone => bone.name == name + ".R").position) * .5f;
            }
        }
    }
}
