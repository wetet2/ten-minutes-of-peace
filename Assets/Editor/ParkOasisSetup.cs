using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkOasisSetup
    {
        const string Prefabs = ParkNaturePack.PandazoleFolder + "Prefabs/";
        const float WaterLevel = -.16f;
        static readonly (string Model, float X, float Z, float Height, float Yaw)[] Palms = {
            ("Tree_10_Spring", -5.55f, 1.65f, 4.7f, 34),
            ("Tree_06_Spring", -3.8f, 3.6f, 3.65f, 108),
            ("Tree_08_Spring", 3.6f, 3.65f, 4.5f, 23),
            ("Tree_10_Spring", 5.6f, 1.2f, 3.6f, 143),
            ("Tree_13_Spring", 5.25f, -1.7f, 1.9f, 225)
        };
        static readonly string[] Rocks = { "HardRock_31", "HardRock_36", "HardRock_41" };
        static readonly string[] Reeds = { "Foliage_02", "Grass_15", "Foliage_03" };

        internal static void Create(Transform desert, List<Rect> obstacles)
        {
            var center = ParkDuneTerrain.OasisCenter;
            var oasis = Group("Desert oasis", desert, new Vector3(center.x, 0, center.y));
            var water = Group("Turquoise oasis water", oasis, Vector3.up * WaterLevel);
            water.gameObject.AddComponent<MeshFilter>().sharedMesh = WaterMesh();
            var waterRenderer = water.gameObject.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = new Material(Shader.Find("PeacePark/Oasis Water")) { name = "Clear turquoise water" };
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;

            const float scaleFromOriginal = ParkDuneTerrain.OasisScale;
            var plants = Group("Oasis palms", oasis, Vector3.zero);
            foreach (var palm in Palms)
            {
                var prefab = Load(palm.Model);
                if (!prefab) continue;
                var bounds = ParkNaturePack.PrefabBounds(prefab);
                if (bounds.size.y <= 0) continue;
                float scale = palm.Height / bounds.size.y;
                var tree = Object.Instantiate(prefab, plants, false);
                tree.name = palm.Model;
                float x = palm.X * scaleFromOriginal, z = palm.Z * scaleFromOriginal;
                tree.transform.SetLocalPositionAndRotation(Ground(x, z) - Vector3.up * (bounds.min.y * scale),
                    Quaternion.Euler(0, palm.Yaw, 0));
                tree.transform.localScale = Vector3.one * scale;
                foreach (var collider in tree.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                Block(x, z, .38f + scale * .14f);
            }

            var bank = Group("Oasis shore stones and reeds", oasis, Vector3.zero);
            for (int i = 0; i < 10; i++)
            {
                float angle = (i + .24f) * Mathf.PI * 2 / 10;
                float x = Mathf.Cos(angle) * ParkDuneTerrain.OasisRadii.x;
                float z = Mathf.Sin(angle) * ParkDuneTerrain.OasisRadii.y;
                float offset = 1.10f + .055f * (i % 3);
                if (i % 4 != 2) Place(bank, Rocks[i % Rocks.Length], x * offset, z * offset, .33f + .13f * (i % 4), i * 61);
                Place(bank, Reeds[i % Reeds.Length], x * 1.13f - .27f, z * 1.13f + .30f, .49f + .10f * (i % 3), i * 46);
            }
            Place(bank, Rocks[1], -6f * scaleFromOriginal, .10f * scaleFromOriginal, .93f, 36);
            Place(bank, Rocks[2], 5.9f * scaleFromOriginal, 2.2f * scaleFromOriginal, .82f, 71);

            var bed = Group("Oasis submerged stones", oasis, Vector3.zero);
            var submerged = new[] {
                new Vector2(-.66f,-.25f), new Vector2(-.58f,-.34f), new Vector2(.47f,-.45f),
                new Vector2(.55f,-.39f), new Vector2(.65f,.17f), new Vector2(.52f,.28f), new Vector2(-.20f,.65f)
            };
            for (int i = 0; i < submerged.Length; i++)
            {
                var point = Vector2.Scale(submerged[i], ParkDuneTerrain.OasisRadii);
                Place(bed, Rocks[i % Rocks.Length], point.x, point.y, .15f + .065f * (i % 3), 23 + i * 47);
            }

            void Place(Transform parent, string model, float x, float z, float height, float yaw)
            {
                var prefab = Load(model);
                if (!prefab || !prefab.TryGetComponent<MeshFilter>(out var filter) || !filter.sharedMesh) return;
                var mesh = filter.sharedMesh;
                if (mesh.bounds.size.y <= 0) return;
                PlaceOnGround(model, mesh, prefab.GetComponent<MeshRenderer>().sharedMaterial, parent, Ground(x, z),
                    height, Quaternion.Euler(0, yaw, 0));
                if (height >= .7f) Block(x, z, .5f);
            }

            Vector3 Ground(float x, float z) => new Vector3(x, ParkDuneTerrain.Height(x + center.x, z + center.y) - .035f, z);

            void Block(float x, float z, float radius) =>
                obstacles.Add(Rect.MinMaxRect(center.x + x - radius, center.y + z - radius,
                    center.x + x + radius, center.y + z + radius));
        }

        static GameObject Load(string model) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + model + ".prefab");

        static Mesh WaterMesh()
        {
            const int segments = 96;
            var vertices = new Vector3[1 + segments];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            for (int step = 0; step < segments; step++)
            {
                float angle = step * Mathf.PI * 2 / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ParkDuneTerrain.OasisBankRadius;
                var point = Vector2.Scale(direction, ParkDuneTerrain.OasisRadii);
                vertices[step + 1] = new Vector3(point.x, 0, point.y);
                uv[step + 1] = direction;
                triangles[step * 3 + 1] = 1 + (step + 1) % segments;
                triangles[step * 3 + 2] = 1 + step;
            }
            return new Mesh { name = "Oasis water and shallows", vertices = vertices, uv = uv, triangles = triangles };
        }
    }
}
