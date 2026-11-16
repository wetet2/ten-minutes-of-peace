using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PeacePark.Editor
{
    internal static class ParkNaturePack
    {
        internal const string PandazoleFolder = "Assets/Pandazole_Ultimate_Pack/Pandazole Nature Environment Pack/";
        const string PrefabFolder = "Assets/Pack_FREE_Trees/Prefabs/";
        const string TreesAndFlowersFolder = "Assets/Trees,Flowers/Prefabs/";
        const string BarkTexture = "Assets/Pack_FREE_Trees/Materials/Textures/treeBody.png";
        const float TreeScale = 1.5f;
        // Each six placements end with one fruit tree. Continue the sequence across the four groups.
        static readonly int[] GardenTrees = { 3, 5, 3, 5, 3, 4, 5, 3, 5, 3, 5, 2 };
        static readonly int[] MeadowTrees = { 5, 3, 5, 3, 5, 8, 3, 5, 3, 5, 3, 9 };
        static readonly int[] GardenTreesAndFlowers = { 9, 13, 16, 10, 11, 9, 16, 13, 19, 9 };
        static readonly int[] MeadowTreesAndFlowers = { 26, 9, 7, 11, 13, 22, 16, 26, 19, 7 };

        sealed class TreeSet
        {
            public readonly GameObject[] Prefabs;
            public readonly Bounds[] Bounds;

            public TreeSet(int[] numbers, int[] treesAndFlowers)
            {
                Prefabs = Array.ConvertAll(numbers, LoadTree);
                var leafy = Array.ConvertAll(treesAndFlowers,
                    number => AssetDatabase.LoadAssetAtPath<GameObject>(TreesAndFlowersFolder + $"Tree_{number}.prefab"));
                if (Array.TrueForAll(leafy, tree => tree))
                {
                    int plain = 0;
                    for (int i = 0; i < Prefabs.Length; i++)
                        if (i % 6 != 5) Prefabs[i] = leafy[plain++];
                }
                Bounds = Array.ConvertAll(Prefabs, prefab => prefab ? PrefabBounds(prefab) : default);
            }

            public bool Ready => Array.TrueForAll(Bounds, bounds => bounds.size.y > 0);
        }

        internal static GameObject LoadTree(int number) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + $"Tree_{number}_Smooth.prefab");

        internal static int Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var park = Array.Find(roots, root => root.name == "Peace Park");
            var meadow = Array.Find(roots, root => root.name == "Storybook countryside");
            var gardenTrees = new TreeSet(GardenTrees, GardenTreesAndFlowers);
            var meadowTrees = new TreeSet(MeadowTrees, MeadowTreesAndFlowers);
            if (!park || !meadow || !gardenTrees.Ready || !meadowTrees.Ready) return 0;
            var materials = new Dictionary<Material, Material>();
            int count = ReplaceGroup(park.transform, "Faceted garden tree", gardenTrees, 4.45f, 0, materials);
            count += ReplaceGroup(meadow.transform, "Meadow tree", meadowTrees, 4.25f, count, materials);
            count += AddBorderTrees(park.transform, gardenTrees, count, materials);
            count += AddMeadowTrees(meadow.transform, meadowTrees, count, materials);
            return count;
        }

        static int ReplaceGroup(Transform group, string anchorName, TreeSet trees, float height, int planted, Dictionary<Material, Material> materials)
        {
            int count = 0;
            foreach (Transform anchor in group)
            {
                if (anchor.name != anchorName) continue;
                var crown = anchor.Find("Crown");
                if (!crown || !crown.gameObject.activeSelf) continue;
                Plant(anchor, trees, planted + count, height, Vector3.zero, 0, materials);
                foreach (string childName in new[] { "Flared trunk", "Crown", "Lower crown" })
                {
                    var original = anchor.Find(childName);
                    if (original) original.gameObject.SetActive(false);
                }
                count++;
            }
            return count;
        }

        static int AddBorderTrees(Transform park, TreeSet trees, int planted, Dictionary<Material, Material> materials)
        {
            const string name = "Additional fence trees";
            if (park.Find(name)) return 0;
            var row = new GameObject(name).transform;
            row.SetParent(park, false);
            int count = 0;
            foreach (int side in new[] { -1, 1 })
            foreach (float z in new[] { -9.7f, -5f, -1f, 3f, 7.7f })
            {
                Plant(row, trees, planted + count, 3.1f + count % 3 * .18f, ParkGardenLayout.Expand(new Vector3(side * 13.6f, 0, z)),
                    count * 49, materials);
                count++;
            }
            for (int x = -10; x <= 10; x += 5)
            {
                Plant(row, trees, planted + count, 3.5f + count % 3 * .22f, ParkGardenLayout.Expand(new Vector3(x, 0, 12.3f)), count * 49, materials);
                count++;
            }
            return count;
        }

        static int AddMeadowTrees(Transform meadow, TreeSet trees, int planted, Dictionary<Material, Material> materials)
        {
            const string name = "Additional meadow trees";
            if (meadow.Find(name)) return 0;
            var grove = new GameObject(name).transform;
            grove.SetParent(meadow, false);
            int count = 0;
            for (int i = 0; i < 28; i++)
            {
                float angle = (i + .35f) * Mathf.PI * 2 / 28;
                float radius = 19.4f + i % 4 * 1.08f;
                var position = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                if (position.z < -12 && Mathf.Abs(position.x) < 7) continue;
                Plant(grove, trees, planted + count, 3.3f + i % 5 * .26f, ParkGardenLayout.Expand(position), i * 67, materials);
                count++;
            }
            // Fill the gap between the fence and the first meadow ring behind both houses.
            for (int i = 0; i < 6; i++)
            {
                float depth = i == 5 ? 14.1f : 15.7f + i % 2 * .8f;
                var position = new Vector3(-14.5f + i * 5.8f, 0, depth);
                Plant(grove, trees, planted + count, 3.65f + i % 3 * .23f, ParkGardenLayout.Expand(position), 26 + i * 41, materials);
                count++;
            }
            foreach (int side in new[] { -1, 1 })
            foreach (float z in new[] { -8f, -.1f, 7.7f })
            {
                Plant(grove, trees, planted + count, 3.55f + count % 3 * .22f,
                    ParkGardenLayout.Expand(new Vector3(side * 16.8f, 0, z)), count * 59, materials);
                count++;
            }
            return count;
        }

        static void Plant(Transform parent, TreeSet trees, int index, float height, Vector3 position, float yaw,
            Dictionary<Material, Material> materials)
        {
            index %= trees.Prefabs.Length;
            var prefab = trees.Prefabs[index];
            var bounds = trees.Bounds[index];
            float scale = TreeScale * Mathf.Min(height / bounds.size.y, 3.75f / Mathf.Max(bounds.size.x, bounds.size.z));
            var tree = Object.Instantiate(prefab, parent, false);
            bool olyPoly = prefab.name.EndsWith("_Smooth", StringComparison.Ordinal);
            tree.name = prefab.name + (olyPoly ? " (OlyPoly)" : " (Trees & Flowers)");
            var rotation = Quaternion.Euler(0, yaw, 0);
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = rotation;
            var baseCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            tree.transform.localPosition = position - rotation * baseCenter * scale;
            if (olyPoly) ApplyMaterials(tree, materials);
        }

        internal static void ApplyMaterials(GameObject tree, Dictionary<Material, Material> replacements)
        {
            foreach (var renderer in tree.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source) continue;
                    if (!replacements.TryGetValue(source, out var replacement))
                    {
                        replacement = new Material(Shader.Find("Standard")) { name = source.name + " (Built-in)" };
                        replacement.SetFloat("_Glossiness", .10f);
                        switch (source.name)
                        {
                            case "TreeBody":
                                replacement.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BarkTexture);
                                break;
                            case "Apple":
                                replacement.color = new Color32(216, 77, 55, 255);
                                break;
                            default:
                                replacement.color = new Color32(111, 166, 70, 255);
                                break;
                        }
                        replacements.Add(source, replacement);
                    }
                    materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
            }
        }

        internal static Bounds PrefabBounds(GameObject prefab)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var root = prefab.transform;
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
            {
                var local = renderer.localBounds;
                var toRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -local.extents.x : local.extents.x,
                        (corner & 2) == 0 ? -local.extents.y : local.extents.y,
                        (corner & 4) == 0 ? -local.extents.z : local.extents.z);
                    bounds.Encapsulate(toRoot.MultiplyPoint3x4(local.center + offset));
                }
            }
            return bounds;
        }
    }
}
