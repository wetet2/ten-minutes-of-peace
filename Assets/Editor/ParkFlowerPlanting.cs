using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeacePark.Editor
{
    internal static class ParkFlowerPlanting
    {
        const string AssetFolder = ParkNaturePack.PandazoleFolder;
        internal const string GroupName = "Pandazole flower clusters";

        static readonly int[] FlowerNumbers = { 19, 20, 21, 22, 23, 26, 27, 29, 34 };
        // Centers are written in the garden's original footprint so they follow its expansion.
        static readonly Vector2[] Centers = {
            new Vector2(-10.2f, 2.9f), new Vector2(-4.6f, 3.6f),
            new Vector2(4.6f, 3.7f), new Vector2(10.1f, 2.8f),
            new Vector2(-9.9f, -1.3f), new Vector2(10.1f, -1.7f),
            new Vector2(-9.1f, -5.1f), new Vector2(9.3f, -5.2f),
            new Vector2(-7.7f, -8.5f), new Vector2(7.3f, -8.6f),
            new Vector2(-3f, -10.4f), new Vector2(3f, -10.4f),
            new Vector2(-3.1f, -3.7f), new Vector2(3f, -3.5f),
            new Vector2(-3.5f, .35f), new Vector2(3.4f, .5f)
        };

        internal static int Apply(Scene scene)
        {
            var park = Array.Find(scene.GetRootGameObjects(), root => root.name == "Peace Park");
            if (!park || park.transform.Find(GroupName)) return 0;
            var lawn = park.transform.Find("Meadow island").GetComponent<MeshRenderer>();
            if (!TryLoadFlowers(out var meshes, out var material)) return 0;

            int beds = ReplantBeds(park.transform, meshes, material);
            var group = ParkScenery.Group(GroupName, park.transform, Vector3.zero);
            float ground = group.InverseTransformPoint(lawn.bounds.max).y;
            const int flowersPerCluster = 7;
            for (int index = 0; index < Centers.Length; index++)
            {
                var center = Centers[index];
                var position = ParkGardenLayout.Expand(new Vector3(center.x, ground, center.y));
                var cluster = ParkScenery.Group($"Flower cluster {index + 1}", group, position);
                for (int i = 0; i < flowersPerCluster; i++)
                {
                    int flower = (index * 2 + i + i % 3) % meshes.Length;
                    var mesh = meshes[flower];
                    float angle = (i * 137.5f + index * 29) * Mathf.Deg2Rad;
                    float radius = .30f * Mathf.Sqrt(i);
                    var offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    var rotation = Quaternion.Euler(0, index * 43 + i * 72, 0);
                    float height = .42f + .12f * ((index + i * 3) % 5);
                    ParkScenery.PlaceOnGround($"Flower_{FlowerNumbers[flower]:00}", mesh, material,
                        cluster, offset, height, rotation);
                }
            }
            return Centers.Length * flowersPerCluster + beds;
        }

        internal static bool TryLoadFlowers(out Mesh[] meshes, out Material material)
        {
            material = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "Materials/PandaMat.mat");
            meshes = Array.ConvertAll(FlowerNumbers, number =>
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetFolder + $"Prefabs/Flower_{number:00}.prefab");
                return prefab && prefab.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
            });
            return material && Array.TrueForAll(meshes, mesh => mesh && mesh.bounds.size.y > 0);
        }

        internal static int FillPlanter(Transform parent, Mesh[] meshes, Material material,
            Vector3 soil, float spacing, int paletteOffset)
        {
            const int flowerCount = 8;
            var flowers = ParkScenery.Group("Pandazole planter flowers", parent, soil);
            for (int i = 0; i < flowerCount; i++)
            {
                int flower = (paletteOffset + i * 2) % meshes.Length;
                var position = new Vector3(-.025f + (i % 4 - 1.5f) * spacing + (i / 4) * .09f, 0,
                    i < 4 ? -.23f : .23f);
                ParkScenery.PlaceOnGround($"Flower_{FlowerNumbers[flower]:00}", meshes[flower], material,
                    flowers, position, .46f + (i % 3) * .055f + (i / 4) * .20f, Quaternion.Euler(0, i * 61, 0));
            }
            return flowerCount;
        }

        static int ReplantBeds(Transform park, Mesh[] meshes, Material material)
        {
            int count = 0;
            foreach (Transform item in park)
            {
                if (IsOldFlower(item)) item.gameObject.SetActive(false);
                if (item.name != "Flower bed") continue;
                var soil = item.Find("Soil").GetComponent<Renderer>();
                float ground = item.InverseTransformPoint(soil.bounds.max).y;
                foreach (Transform part in item)
                    if (IsOldFlower(part)) part.gameObject.SetActive(false);
                count += FillPlanter(item, meshes, material, new Vector3(0, ground, 0), .57f, count);
            }
            return count;
        }

        static bool IsOldFlower(Transform item) =>
            item.name == "Stem" || item.name == "Flower leaf" || item.name == "Petal" || item.name == "Flower centre";
    }
}
