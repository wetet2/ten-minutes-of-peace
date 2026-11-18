using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkDesertSetup
    {
        const string Prefabs = ParkNaturePack.PandazoleFolder + "Prefabs/";
        static readonly string[] Cacti = { "Cactus_37_A", "Cactus_28_A", "Cactus_13_A", "Cactus_22_A" };
        static readonly string[] Rocks = { "HardRock_31", "HardRock_36", "HardRock_41", "HardRock_51", "HardRock_56" };
        static readonly string[] Trees = { "TreeTrunk_05", "TreeTrunk_25", "TreeTrunk_33" };
        static readonly string[] Brush = { "Thorns_01", "Thorns_07" };

        // Each pair holds the X/Z position of a group of desert plants and stones.
        static readonly Vector2[] Groves = {
            new Vector2(-21,-12), new Vector2(20,-13), new Vector2(-13,-16), new Vector2(13,-16),
            new Vector2(-21,-3), new Vector2(20,-4), new Vector2(-14,-6), new Vector2(13,-5),
            new Vector2(-22,6), new Vector2(21,6), new Vector2(-17,11), new Vector2(18,12),
            new Vector2(-25,18), new Vector2(25,19), new Vector2(-5,10), new Vector2(6,11),
            new Vector2(-6,-23), new Vector2(9,-26), new Vector2(-28,-25), new Vector2(26,-30),
            new Vector2(-16,-37), new Vector2(3,-39), new Vector2(33,-41)
        };

        internal static bool IsInstalled => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + Cacti[0] + ".prefab");

        internal static ParkVenue Create(Transform root)
        {
            var terrain = root.gameObject.AddComponent<ParkDuneTerrain>();
            var sand = Paint("Honey sand", "#F2C079");
            sand.SetColor("_Shade", new Color(.76f, .48f, .28f));
            sand.SetFloat("_Variation", .043f);
            sand.SetFloat("_GrassDetail", .045f);
            var ground = ParkGeometry.Place("Continuous sand and dunes", SandMesh(), root, Vector3.zero, Vector3.one, sand);
            ground.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var obstacles = new List<Rect>();
            var scenery = Group("Pandazole desert scenery", root, Vector3.zero);
            var loadedProps = new Dictionary<string, MeshFilter>();
            foreach (var names in new[] { Cacti, Rocks, Trees, Brush })
            foreach (string name in names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                if (prefab && prefab.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh &&
                    filter.sharedMesh.bounds.size.y > 0) loadedProps.Add(name, filter);
            }

            for (int i = 0; i < Groves.Length; i++)
            {
                var p = Groves[i];
                var side = Mathf.Sign(p.x);
                Place(Cacti[i % Cacti.Length], p, i % 4 == 0 ? 3.3f : 1.1f + i % 4 * .40f, i * 51);
                Place(Cacti[(i + 1) % Cacti.Length], p + new Vector2(side * 1.4f, 1.15f), .55f + (i % 3) * .20f, 20 + i * 36);
                Place(Rocks[i % Rocks.Length], p + new Vector2(-side * 1.65f, -.75f), .78f + i % 3 * .32f, i * 72);
                Place(Brush[i % Brush.Length], p + new Vector2(side * .75f, -1.3f), .5f, i * 73);
                if (i % 3 == 1) Place(Trees[(i / 3) % Trees.Length], p + new Vector2(side * 2.4f, 2.1f), 2.75f + .25f * (i % 5), i * 43);
            }
            Place(Trees[0], new Vector2(0, 15), 3.3f, 22);
            Place(Rocks[2], new Vector2(-1.3f, 14.5f), 1.25f, 25);
            Place(Rocks[1], new Vector2(5.8f, -7.2f), .78f, 73);
            Place(Rocks[4], new Vector2(-6.8f, -8.4f), .62f, 58);
            for (int i = 0; i < 7; i++)
            {
                float x = -29 + 9.6f * i;
                Place(Rocks[i % Rocks.Length], new Vector2(x, 39 + (i % 3) * 3), 1.5f + (i % 4) * .50f, 26 + i * 38);
            }
            ParkOasisSetup.Create(root, obstacles);

            return new ParkVenue {
                Name = "사막", Root = root.gameObject, DuneTerrain = terrain,
                Houses = new[] { DuneEntrance(root, terrain, -11.8f, 24f), DuneEntrance(root, terrain, 11.3f, 24.6f) },
                WalkArea = Rect.MinMaxRect(-25, -18, 25, 17.5f), WalkObstacles = obstacles.ToArray(),
                PondCenter = ParkDuneTerrain.OasisCenter, PondRadii = ParkDuneTerrain.OasisRadii * 1.25f + Vector2.one * .22f,
                Focus = new Vector3(0, 2.6f, 3.5f), Distance = 54, MaxDistance = 72, Pitch = 31,
                Sky = new Color32(240, 213, 172, 255)
            };

            void Place(string name, Vector2 point, float height, float yaw)
            {
                if (!loadedProps.TryGetValue(name, out var filter)) return;
                var renderer = filter.GetComponent<MeshRenderer>();
                var mesh = filter.sharedMesh;
                if (name.StartsWith("HardRock_", StringComparison.Ordinal))
                {
                    var size = mesh.bounds.size;
                    float width = Mathf.Max(size.x, size.z);
                    float maxWidth = Mathf.Abs(point.y) > 35 ? 5 : 3;
                    height = Mathf.Min(height, size.y * maxWidth / width);
                }
                var position = new Vector3(point.x, ParkDuneTerrain.Height(point.x, point.y) - .035f, point.y);
                var prop = PlaceOnGround(name, mesh, renderer.sharedMaterial, scenery, position, height, Quaternion.Euler(0, yaw, 0));
                if (height < .65f || point.y < -20) return;
                var bounds = prop.GetComponent<MeshRenderer>().bounds;
                float radius = Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z) * .85f, .24f, 1.25f);
                obstacles.Add(Rect.MinMaxRect(point.x - radius, point.y - radius, point.x + radius, point.y + radius));
            }
        }

        static ParkHouse DuneEntrance(Transform root, ParkDuneTerrain terrain, float x, float crestZ)
        {
            var entrance = Group("Dune crossing", root, Vector3.zero).gameObject.AddComponent<ParkHouse>();
            entrance.DisplayName = "모래언덕";
            entrance.DoorDuration = .1f;
            entrance.ExitSeconds = 9.6f;
            entrance.ExitTerrain = terrain;
            entrance.Inside = Point("Behind the dune", crestZ + 8.5f);
            entrance.Threshold = Point("Dune crest", crestZ);
            entrance.Welcome = Point("Dune trailhead", 16.5f);
            return entrance;

            Transform Point(string name, float z) =>
                Group(name, entrance.transform, new Vector3(x, ParkDuneTerrain.Height(x, z), z));
        }

        static Mesh SandMesh()
        {
            var coordinates = SandCoordinates();
            int rings = coordinates.Count;
            var vertices = new Vector3[rings * rings];
            var triangles = new int[(rings - 1) * (rings - 1) * 6];
            int triangle = 0;
            for (int z = 0; z < rings; z++)
            for (int x = 0; x < rings; x++)
            {
                float px = coordinates[x];
                float pz = coordinates[z];
                int index = z * rings + x;
                // A small offset keeps animated feet above triangle interpolation on the crests.
                vertices[index] = new Vector3(px, ParkDuneTerrain.Height(px, pz) - .022f, pz);
                if (x == rings - 1 || z == rings - 1) continue;
                triangles[triangle++] = index;
                triangles[triangle++] = index + rings;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + rings;
                triangles[triangle++] = index + rings + 1;
            }
            var mesh = new Mesh { name = "Wide rolling desert sand", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }

        static List<float> SandCoordinates()
        {
            const int edgeCells = 100;
            const int oasisCells = 8;
            const float spacing = .8f;
            var coordinates = new List<float>();
            for (int cell = -edgeCells; cell <= edgeCells; cell++)
            {
                float point = cell * spacing;
                coordinates.Add(ExtendHorizon(point));
                // Keep the waterline smooth without subdividing the distant dunes.
                if (cell < -oasisCells || cell >= oasisCells) continue;
                coordinates.Add(point + spacing / 3);
                coordinates.Add(point + spacing * 2 / 3);
            }
            return coordinates;
        }

        static float ExtendHorizon(float coordinate)
        {
            float edge = Mathf.Max(0, Mathf.Abs(coordinate) - 62);
            return coordinate + Mathf.Sign(coordinate) * edge * edge * .27f;
        }
    }
}
