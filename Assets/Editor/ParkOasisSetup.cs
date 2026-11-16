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
            waterRenderer.sharedMaterials = new[] {
                Paint("Oasis blue", "#30ACA9"), Paint("Oasis turquoise", "#4CC5B6"), Paint("Oasis shallows", "#80D7BC")
            };
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var glints = new Pieces();
            var oval = PrimitiveMesh(PrimitiveType.Sphere);
            var foam = Paint("Oasis reflections", "#BDEBDD");
            for (int i = 0; i < 7; i++)
            {
                var point = new Vector3(Mathf.Sin(i * 1.9f) * 2.95f, WaterLevel + .009f, Mathf.Cos(i * 2.1f) * 1.85f);
                glints.Add(oval, foam, point, new Vector3(.34f + .16f * (i % 4), .013f, .055f), Quaternion.Euler(0, 11, 0));
            }
            var waterDetails = Group("Oasis reflections", oasis, Vector3.zero);
            glints.Build(waterDetails);
            foreach (var renderer in waterDetails.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;

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
                tree.transform.SetLocalPositionAndRotation(Shore(palm.X, palm.Z) - Vector3.up * (bounds.min.y * scale),
                    Quaternion.Euler(0, palm.Yaw, 0));
                tree.transform.localScale = Vector3.one * scale;
                foreach (var collider in tree.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                Block(palm.X, palm.Z, .38f + scale * .14f);
            }

            var bank = Group("Oasis shore stones and reeds", oasis, Vector3.zero);
            for (int i = 0; i < 10; i++)
            {
                float angle = (i + .24f) * Mathf.PI * 2 / 10;
                float x = Mathf.Cos(angle) * ParkDuneTerrain.OasisRadii.x;
                float z = Mathf.Sin(angle) * ParkDuneTerrain.OasisRadii.y;
                float offset = 1.10f + .055f * (i % 3);
                if (i % 4 != 2) Place(Rocks[i % Rocks.Length], x * offset, z * offset, .33f + .13f * (i % 4), i * 61);
                Place(Reeds[i % Reeds.Length], x * 1.13f - .27f, z * 1.13f + .30f, .49f + .10f * (i % 3), i * 46);
            }
            Place(Rocks[1], -6f, .10f, .93f, 36);
            Place(Rocks[2], 5.9f, 2.2f, .82f, 71);

            void Place(string model, float x, float z, float height, float yaw)
            {
                var prefab = Load(model);
                if (!prefab || !prefab.TryGetComponent<MeshFilter>(out var filter) || !filter.sharedMesh) return;
                var mesh = filter.sharedMesh;
                if (mesh.bounds.size.y <= 0) return;
                PlaceOnGround(model, mesh, prefab.GetComponent<MeshRenderer>().sharedMaterial, bank, Shore(x, z),
                    height, Quaternion.Euler(0, yaw, 0));
                if (height >= .7f) Block(x, z, .5f);
            }

            Vector3 Shore(float x, float z) => new Vector3(x, ParkDuneTerrain.Height(x + center.x, z + center.y) - .035f, z);

            void Block(float x, float z, float radius) =>
                obstacles.Add(Rect.MinMaxRect(center.x + x - radius, center.y + z - radius,
                    center.x + x + radius, center.y + z + radius));
        }

        static GameObject Load(string model) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + model + ".prefab");

        static Mesh WaterMesh()
        {
            const int segments = 64;
            var rings = new[] { .68f, .91f, 1.25f };
            var vertices = new Vector3[1 + rings.Length * segments];
            var normals = new Vector3[vertices.Length];
            normals[0] = Vector3.up;
            var mesh = new Mesh { name = "Oasis water and shallows" };
            for (int ring = 0; ring < rings.Length; ring++)
            for (int step = 0; step < segments; step++)
            {
                float angle = step * Mathf.PI * 2 / segments;
                float radius = rings[ring] * (1 + .022f * Mathf.Sin(angle * 3));
                int vertex = 1 + ring * segments + step;
                vertices[vertex] = new Vector3(Mathf.Cos(angle) * ParkDuneTerrain.OasisRadii.x * radius, 0,
                    Mathf.Sin(angle) * ParkDuneTerrain.OasisRadii.y * radius);
                normals[vertex] = Vector3.up;
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.subMeshCount = rings.Length;
            for (int ring = 0; ring < rings.Length; ring++)
            {
                var indices = new List<int>(segments * 6);
                for (int step = 0; step < segments; step++)
                {
                    int outer = 1 + ring * segments + step;
                    int outerNext = 1 + ring * segments + (step + 1) % segments;
                    if (ring == 0) indices.AddRange(new[] { 0, outerNext, outer });
                    else
                    {
                        int inner = outer - segments;
                        int innerNext = outerNext - segments;
                        indices.AddRange(new[] { inner, innerNext, outer, outer, innerNext, outerNext });
                    }
                }
                mesh.SetTriangles(indices, ring);
            }
            return mesh;
        }
    }
}
