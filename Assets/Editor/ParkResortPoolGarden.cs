using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;
using static PeacePark.ParkResortRiver;
using static PeacePark.ParkResortSlide;

namespace PeacePark.Editor
{
    internal sealed partial class ParkResortPoolSetup
    {
        static readonly string[] ResortPalms = { "Tree_10_Spring", "Tree_06_Spring", "Tree_08_Spring", "Tree_13_Spring" };
        static readonly string[] ResortRocks = { "HardRock_31", "HardRock_36", "HardRock_41", "HardRock_51", "HardRock_56" };
        static readonly string[] ResortFoliage = { "Foliage_02", "Foliage_03", "Grass_15" };
        readonly Dictionary<string, Mesh> gardenMeshes = new Dictionary<string, Mesh>();
        readonly Dictionary<string, (GameObject Prefab, Bounds Bounds)> gardenTrees = new Dictionary<string, (GameObject, Bounds)>();
        readonly Material shellStone = Paint("Weathered limestone from the garden", "#C6B397");
        readonly Material slateStone = Paint("Soft gray garden stones", "#A49F8E");
        readonly Material plantingSoil = Paint("Dark terracotta planting soil", "#82644F");
        readonly Material gardenHills = Paint("Green coastal hills", "#799D68");
        readonly Material distantHills = Paint("Blue-green hills", "#799C79");
        Material gardenPalette, palmCrown, palmTrunk, shadeLeaves;
        Mesh[] gardenFlowers, lowLeaves;

        void MakeLandscaping(Transform root)
        {
            var garden = Group("Pandazole resort gardens", root, Vector3.zero);
            ScaleAboveDeck(garden, .74f);
            Palette();
            grass.SetFloat("_Variation", .057f);
            grass.SetFloat("_GrassDetail", .13f);
            MakePoolsidePlanting(garden);
            MakeArrivalGarden(garden);
            MakeSideGardens(garden);
            MakeCoastalBackdrop(garden);
        }

        void MakePoolsidePlanting(Transform garden)
        {
            var poolside = Group("Flower beds along the pool terraces", garden, Vector3.zero);
            var frontBeds = new[] { new Vector2(-45.4f,5.88f), new Vector2(-32.8f,4.65f), new Vector2(-19.4f,6.32f),
                new Vector2(-4.5f,5.64f), new Vector2(10.85f,5.78f), new Vector2(25.55f,5.44f) };
            for (int i = 0; i < frontBeds.Length; i++)
            {
                var bed = frontBeds[i];
                float z = -19.16f + (i % 2) * .18f;
                float depth = .87f + i % 3 * .06f;
                PlantingBed(poolside, new Vector3(bed.x, DeckHeight, z), new Vector2(bed.y, depth), i, true);
                Block(bed.x, z, bed.y + .12f, depth + .10f);
            }
            PlantingBed(poolside, new Vector3(-35.38f, DeckHeight, 7.48f), new Vector2(.81f, 4.25f), 23, true);
            PlantingBed(poolside, new Vector3(-35.38f, DeckHeight, -10.35f), new Vector2(.81f, 3.18f), 16, true);
            Block(-35.38f, 7.48f, .91f, 4.39f);
            Block(-35.38f, -10.35f, .91f, 3.31f);
            foreach (float x in new[] { -32f, -15.7f, -8.1f, 2.82f, 12.9f, 28.9f })
            {
                var urn = Group("Flowering urn", poolside, new Vector3(x, DeckHeight, 18.62f));
                var pieces = new Pieces();
                pieces.Add(cylinder, honey, Vector3.up * .11f, new Vector3(.72f, .11f, .72f));
                pieces.Add(sphere, ivory, Vector3.up * .34f, new Vector3(1.03f, .65f, 1.03f));
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.PI * 2 / 5;
                    PlantFlower(pieces, new Vector3(Mathf.Cos(angle) * .26f, .59f, Mathf.Sin(angle) * .26f), .32f + i % 3 * .077f, (int)(x + 42) + i);
                }
                pieces.Build(urn);
                Block(x, 18.62f, .62f, .62f);
            }

            var island = Group("Botanical island garden", garden, Vector3.zero);
            PlantPalm(island, new Vector3(6.9f, .30f, -4.7f), 6.45f, 0);
            PlantPalm(island, new Vector3(15.7f, .30f, -4f), 7.15f, 1);
            PlantPalm(island, new Vector3(6.5f, .30f, 4.4f), 4.8f, 2);
            PlantPalm(island, new Vector3(14.3f, .30f, 6.08f), 5.5f, 3);
            IslandBed(8f, -4.4f, 2.5f, 2.0f, 19);
            IslandBed(14.7f, -4.1f, 2.55f, 2.15f, 26);
            IslandBed(7.1f, 3.55f, 2.25f, 2.0f, 32);
            IslandBed(14.25f, 6.04f, 1.95f, 1.38f, 30);
            var islandPath = Tiles("Shell stone garden path", "#D8BF91", "#BB9E76", 1.06f, .58f, true);
            MakeMesh("Island path from the bridge", Fill(Rectangle(IslandPathStartX, BridgeZ - IslandPathHalfWidth,
                BridgeCenterX - BridgeHalfSpan, BridgeZ + IslandPathHalfWidth), IslandPathHeight), islandPath, island);
            Bench(island, new Vector3(10.62f, IslandLawnHeight, 1.45f), 0);
            Block(10.62f, 1.45f, 1.23f, .46f);

            void IslandBed(float x, float z, float halfWidth, float halfDepth, int seed)
            {
                PlantingBed(island, new Vector3(x, IslandLawnHeight, z), new Vector2(halfWidth, halfDepth), seed, false);
                Block(x, z, halfWidth + .18f, halfDepth + .18f);
            }
        }

        void MakeArrivalGarden(Transform garden)
        {
            var arrival = Group("Front arrival garden and loggia", garden, Vector3.zero);
            var walk = Tiles("Arrival garden travertine", "#D8BB94", "#B59B78", 1.22f, .66f, true);
            MakeMesh("Cross terrace promenade", Fill(Rectangle(TerraceWest, -25f, 32.5f, -21.2f), DeckHeight), walk, arrival);
            MakeMesh("Palm walk to the loggia", Fill(Rectangle(-9.1f, -45.2f, -1.3f, -25f), DeckHeight), walk, arrival);
            MakeGardenPavilion(arrival, "Tiled arrival loggia", new Vector3(-5.2f, DeckHeight, -30.1f), 0, 3, 4.42f);
            GardenWall(arrival, new Vector3(-22.2f, .037f, -29.8f), 16.4f, 0);
            GardenWall(arrival, new Vector3(11.2f, .037f, -29.8f), 16.4f, 0);
            GardenWall(arrival, new Vector3(25.65f, .037f, -26.7f), 8.7f, -22);

            int patch = 0;
            foreach (var center in new[] { new Vector2(-29, -26.7f), new Vector2(-19.2f, -27f), new Vector2(-12.85f, -32.5f),
                new Vector2(3.5f, -33.1f), new Vector2(12.45f, -26.9f), new Vector2(21.75f, -28.5f), new Vector2(30, -25.65f),
                new Vector2(-25, -37.5f), new Vector2(15.5f, -38.5f) })
            {
                PlantingBed(arrival, At(center, .032f), new Vector2(2.9f + patch % 3 * .38f, 1.66f + patch % 2 * .44f), patch + 41, false);
                PlantPalm(arrival, At(center + new Vector2(.5f, .15f), -.02f), 4.9f + patch % 4 * .63f, patch + 8);
                patch++;
            }
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 5; i++)
                    PlantPalm(arrival, new Vector3(-5.2f + side * (5.5f + i % 2 * .44f), -.015f, -36f - 4.8f * i),
                        5.4f + (i * 3 + side + 1) % 5 * .5f, i * 3 + side + 1);
                Bench(arrival, new Vector3(-5.2f + side * 12.65f, .06f, -23.25f), 180);
            }
        }

        void MakeSideGardens(Transform garden)
        {
            foreach (int side in new[] { -1, 1 })
            {
                bool west = side < 0;
                float edge = west ? TerraceWest : 32.5f;
                var border = Group(west ? "West hillside gardens" : "East hillside gardens", garden, Vector3.zero);
                var paving = Tiles("Side promenade stone", "#D2B696", "#B29873", .96f, .55f, true);
                float otherEdge = edge + side * 2.25f;
                MakeMesh("Poolside garden promenade", Fill(Rectangle(Mathf.Min(edge, otherEdge), -20.95f,
                    Mathf.Max(edge, otherEdge), 19.85f), DeckHeight), paving, border);
                MakeGardenPavilion(border, "Shaded garden pavilion",
                    new Vector3(edge + side * 8.6f, DeckHeight, west ? 6.7f : -1.9f), side * 90, 2, 3.65f);

                for (int i = 0; i < 7; i++)
                {
                    float z = -17.5f + i * 6.65f, x = edge + side * (3.8f + i % 2 * .5f);
                    bool besidePavilion = west ? i == 3 || i == 4 : i == 2 || i == 3;
                    float groveOffset = besidePavilion ? side * 8.65f : side * 2.9f;
                    PlantingBed(border, new Vector3(x, .036f, z), new Vector2(1.22f, 2.67f + i % 2 * .56f), i * 5 + (west ? 21 : 34), true);
                    if (!besidePavilion)
                        PlantPalm(border, new Vector3(x + side * .77f, -.019f, z + .52f), 4.8f + i % 4 * .62f, i * 3 + side + 1);
                    PlantPalm(border, new Vector3(x + groveOffset, -.02f, z + (i % 3 - 1) * 1.15f),
                        5.6f + i % 5 * .48f, i * 7 + side + 1);
                    if (!besidePavilion && i % 2 == 0)
                        GardenWall(border, new Vector3(x + side * 3f, .036f, z), 4.8f, 90);
                }
            }
        }

        void MakeCoastalBackdrop(Transform garden)
        {
            var backdrop = Group("Layered coastal groves and hills", garden, Vector3.zero);
            for (int i = 0; i < 24; i++)
            {
                float angle = (i + .23f) * Mathf.PI * 2 / 24;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var grove = Vector2.Scale(direction, new Vector2(56 + i % 3 * 3.6f, 48 + i % 4 * 3.1f));
                var tangent = new Vector2(-direction.y, direction.x);
                for (int tree = 0; tree < 3; tree++)
                {
                    var point = grove + tangent * ((tree - 1) * 3.55f) + direction * ((i + tree * 3) % 7);
                    if (point.y < -36 && Mathf.Abs(point.x + 5.2f) < 6.1f) continue;
                    PlantPalm(backdrop, At(point, -.037f), 5.35f + (i * 5 + tree * 2) % 7 * .49f, i * 5 + tree);
                }
                if (i % 2 == 0)
                {
                    var hill = Vector2.Scale(direction, new Vector2(79 + i % 5 * 3.5f, 76 + i % 3 * 7.2f));
                    MakeHill(backdrop, hill, new Vector2(25 + i % 4 * 3.1f, 17 + i % 5 * 2.3f),
                        6.2f + i % 7 * 1.23f, angle);
                }
            }
        }

        void MakeGardenPavilion(Transform parent, string name, Vector3 position, float yaw, int bays, float bayWidth)
        {
            var pavilion = Group(name, parent, position);
            pavilion.localRotation = Quaternion.Euler(0, yaw, 0);
            float width = bays * bayWidth, depth = bays == 3 ? 5.66f : 4.35f, eave = 4.38f;
            var paving = Tiles("Pavilion patio tiles", "#E1C6A5", "#B9A080", .9f, .67f, true);
            MakeMesh("Pavilion stone floor", Fill(Rectangle(-width * .5f - .38f, -depth * .5f - .28f,
                width * .5f + .38f, depth * .5f + .28f), .028f), paving, pavilion);

            var masonry = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                float z = side * (depth * .5f - .32f);
                for (int column = 0; column <= bays; column++)
                {
                    float x = (column - bays * .5f) * bayWidth;
                    masonry.Add(softBox, honey, new Vector3(x, .14f, z), new Vector3(.83f, .28f, .85f));
                    masonry.Add(cube, ivory, new Vector3(x, 2.07f, z), new Vector3(.49f, 3.91f, .50f));
                    masonry.Add(cube, honey, new Vector3(x, 2.16f, z), new Vector3(.67f, .15f, .64f));
                }
                for (int bay = 0; bay < bays; bay++)
                    Arc(masonry, ivory, new Vector3((bay + .5f - bays * .5f) * bayWidth, 2.16f, z), bayWidth * .5f - .09f, .28f, .74f);
                masonry.Add(cube, ivory, new Vector3(0, 4.10f, z), new Vector3(width + .70f, .32f, .72f));
                masonry.Add(cube, honey, new Vector3(0, 4.31f, z), new Vector3(width + .70f, .13f, .60f));
            }
            foreach (int side in new[] { -1, 1 })
                masonry.Add(cube, ivory, new Vector3(side * width * .5f, 4.16f, 0), new Vector3(.60f, .31f, depth));
            for (int i = 0; i < bays; i++)
            {
                float center = (i + .5f - bays * .5f) * bayWidth;
                masonry.Add(cylinder, darkWood, new Vector3(center, 3.91f, 0), new Vector3(.055f, .48f, .055f));
                masonry.Add(softBox, orange, new Vector3(center, 3.37f, 0), new Vector3(.25f, .34f, .27f));
                masonry.Add(cube, darkWood, new Vector3(center, 3.54f, 0), new Vector3(.33f, .08f, .33f));
            }
            masonry.Build(pavilion);
            AddRoof(pavilion, Vector3.up * eave, width + 1.32f, depth + .72f, bays == 3 ? 1.35f : .98f);
            if (bays == 2) Bench(pavilion, new Vector3(0, .03f, 1.19f), 180);
        }

        void GardenWall(Transform parent, Vector3 position, float length, float yaw)
        {
            var wall = Group("Low limestone garden wall", parent, position);
            wall.localRotation = Quaternion.Euler(0, yaw, 0);
            var masonry = new Pieces();
            masonry.Add(cube, honey, Vector3.up * .44f, new Vector3(length, .88f, .40f));
            masonry.Add(softBox, ivory, Vector3.up * .92f, new Vector3(length + .22f, .22f, .65f));
            int columns = Mathf.CeilToInt(length / 4.6f);
            for (int i = 0; i <= columns; i++)
            {
                float x = (i / (float)columns - .5f) * length;
                masonry.Add(softBox, shellStone, new Vector3(x, .58f, 0), new Vector3(.68f, 1.18f, .63f));
                masonry.Add(sphere, ivory, new Vector3(x, 1.18f, 0), Vector3.one * .35f);
            }
            masonry.Build(wall);
        }

        void Bench(Transform parent, Vector3 position, float yaw)
        {
            var bench = Group("Teak and limestone garden seat", parent, position);
            bench.localRotation = Quaternion.Euler(0, yaw, 0);
            var woodwork = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                woodwork.Add(softBox, honey, new Vector3(side * .83f, .24f, 0), new Vector3(.34f, .48f, .53f));
                woodwork.Add(cube, darkWood, new Vector3(side * .82f, .70f, .235f), new Vector3(.088f, .56f, .10f));
            }
            for (int slat = 0; slat < 3; slat++)
                woodwork.Add(softBox, wood, new Vector3(0, .49f, (slat - 1) * .159f), new Vector3(2.17f, .10f, .145f));
            for (int slat = 0; slat < 2; slat++)
                woodwork.Add(softBox, wood, new Vector3(0, .73f + slat * .152f, .251f), new Vector3(2.17f, .135f, .084f));
            woodwork.Build(bench);
        }

        void MakeHill(Transform parent, Vector2 center, Vector2 size, float height, float yaw)
        {
            const int segments = 20, rings = 5;
            var points = new Vector3[1 + rings * segments];
            var triangles = new int[segments * (1 + (rings - 1) * 2) * 3];
            points[0] = At(center, height);
            for (int ring = 0; ring < rings; ring++)
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                float radius = (ring + 1f) / rings;
                float lobe = 1 + .12f * Mathf.Sin(angle * 3 + yaw) + .077f * Mathf.Sin(angle * 2 - yaw * 3);
                var local = Vector2.Scale(size * radius * lobe, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                float elevation = height * Mathf.Pow(Mathf.Cos(radius * Mathf.PI * .5f), 2);
                points[1 + ring * segments + i] = At(center + local, elevation - .085f);
            }
            int face = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[face++] = 0; triangles[face++] = next + 1; triangles[face++] = i + 1;
                for (int ring = 1; ring < rings; ring++)
                {
                    int inner = 1 + (ring - 1) * segments, outer = inner + segments;
                    triangles[face++] = inner + i; triangles[face++] = inner + next; triangles[face++] = outer + next;
                    triangles[face++] = inner + i; triangles[face++] = outer + next; triangles[face++] = outer + i;
                }
            }
            MakeMesh("Coastal hill", MeshOf("Rolling hillside", points, triangles), height > 10 ? distantHills : gardenHills, parent, false);
        }

        void PlantingBed(Transform parent, Vector3 center, Vector2 radii, int seed, bool raised)
        {
            var bed = Group(raised ? "Integrated limestone planter" : "Natural palm understory", parent, center);
            float top = raised ? .13f : .014f;
            if (raised)
            {
                var edge = Outline(Vector2.zero, radii, .51f);
                var chamfer = Outline(Vector2.zero, radii - Vector2.one * .065f, .445f);
                var soil = Outline(Vector2.zero, radii - Vector2.one * .235f, .275f);
                MakeMesh("Recessed planting soil", Fill(soil, top), plantingSoil, bed, false);
                MakeMesh("Low limestone reveal", Band(edge, edge, .011f, .112f), honey, bed);
                MakeMesh("Honed chamfer", Band(edge, chamfer, .112f, .177f), ivory, bed);
                MakeMesh("Wide limestone coping", Band(chamfer, soil, .177f, .177f), ivory, bed);
                MakeMesh("Inner shadow joint", Band(soil, soil, .177f, top), plantingSoil, bed);
            }
            var pieces = new Pieces();
            int count = Mathf.CeilToInt((radii.x + radii.y) * 5.1f);
            float stoneWidth = Mathf.Min(1.02f, Mathf.Min(radii.x, radii.y) * .66f);
            for (int i = 0; i < count; i++)
            {
                var offset = PlantOffset(i, count, 21, .82f);
                var point = At(offset, top - .009f);
                int index = i + seed;
                if (i % 7 == 0)
                    GardenPlant(pieces, ResortRocks[index % ResortRocks.Length], new Vector3(point.x * .65f, point.y, point.z * .65f),
                        .22f + i % 3 * .073f, index * 46, i % 2 == 0 ? shellStone : slateStone, stoneWidth);
                else if (i % 5 is 1 or 4)
                    GardenPlant(pieces, ResortFoliage[index % ResortFoliage.Length], point, .38f + index % 5 * .073f, index * 53, gardenPalette);
                else
                    PlantFlower(pieces, point, .43f + index % 5 * .085f, index);
            }
            int groundCover = Mathf.CeilToInt((radii.x + radii.y) * 2.45f);
            for (int i = 0; i < groundCover; i++)
            {
                var offset = PlantOffset(i, groundCover, 33, .76f);
                var point = At(offset, top - .018f);
                AddGardenMesh(pieces, lowLeaves[(i + seed) % lowLeaves.Length], i % 3 == 0 ? palmCrown : shadeLeaves,
                    point, .44f + i % 4 * .125f, seed * 43 + i * 61, Mathf.Min(1.74f, Mathf.Min(radii.x, radii.y) * 1.72f));
            }
            pieces.Build(bed);

            Vector2 PlantOffset(int index, int number, int twist, float spread)
            {
                float angle = (index * 137.5f + seed * twist) * Mathf.Deg2Rad;
                if (!raised)
                {
                    float radius = spread * Mathf.Sqrt((index + .38f) / number);
                    return new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y) * radius;
                }
                bool alongX = radii.x > radii.y;
                float length = (alongX ? radii.x : radii.y) - .47f;
                float width = (alongX ? radii.y : radii.x) - .26f;
                float along = Mathf.Lerp(-length, length, (index + .5f) / number);
                float across = Mathf.Sin(angle) * width * spread * .76f;
                return alongX ? new Vector2(along, across) : new Vector2(across, along);
            }
        }

        void Palette()
        {
            ParkFlowerPlanting.TryLoadFlowers(out gardenFlowers, out var source);
            gardenPalette = Paint("Pandazole garden colors", "#FFF4E7");
            palmCrown = Paint("Pandazole palm fronds", "#BCD4BB");
            shadeLeaves = Paint("Pandazole groundcover leaves", "#9CB6A1");
            palmTrunk = Paint("Pandazole palm bark", "#F0D2BA");
            lowLeaves = new[] { CrownMesh("Tree_08_Spring"), CrownMesh("Tree_13_Spring") };
            if (!source || !source.mainTexture) return;
            foreach (var material in new[] { gardenPalette, palmCrown, palmTrunk, shadeLeaves })
            {
                material.mainTexture = source.mainTexture;
                material.EnableKeyword("PEACE_PARK_PAINT_MAP");
            }
        }

        static Mesh CrownMesh(string name)
        {
            var prefab = LoadGardenPrefab(name);
            if (!prefab) return null;
            foreach (var part in prefab.GetComponentsInChildren<MeshFilter>())
                if (part.transform != prefab.transform) return part.sharedMesh;
            return null;
        }

        void PlantFlower(Pieces pieces, Vector3 ground, float height, int seed) =>
            AddGardenMesh(pieces, gardenFlowers[seed % gardenFlowers.Length], gardenPalette, ground, height, seed * 53);

        void GardenPlant(Pieces pieces, string model, Vector3 ground, float height, float yaw, Material material, float maxWidth = float.PositiveInfinity)
        {
            if (!gardenMeshes.TryGetValue(model, out var mesh))
            {
                var asset = LoadGardenPrefab(model);
                mesh = asset && asset.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                gardenMeshes.Add(model, mesh);
            }
            AddGardenMesh(pieces, mesh, material, ground, height, yaw, maxWidth);
        }

        static void AddGardenMesh(Pieces pieces, Mesh mesh, Material material, Vector3 ground, float height, float yaw,
            float maxWidth = float.PositiveInfinity)
        {
            if (!mesh || mesh.bounds.size.y <= 0) return;
            var bounds = mesh.bounds;
            float scale = Mathf.Min(height / bounds.size.y, maxWidth / Mathf.Max(bounds.size.x, bounds.size.z));
            var pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var rotation = Quaternion.Euler(0, yaw, 0);
            pieces.Add(mesh, material, ground - rotation * pivot * scale, Vector3.one * scale, rotation);
        }

        void PlantPalm(Transform parent, Vector3 point, float height, int seed)
        {
            string name = ResortPalms[seed % ResortPalms.Length];
            if (!gardenTrees.TryGetValue(name, out var asset))
            {
                var prefab = LoadGardenPrefab(name);
                asset = (prefab, prefab ? ParkNaturePack.PrefabBounds(prefab) : default);
                gardenTrees.Add(name, asset);
            }
            if (!asset.Prefab || asset.Bounds.size.y <= 0) return;
            bool stout = name == "Tree_13_Spring";
            float scale = (stout ? Mathf.Min(height, 4.25f) : height) / asset.Bounds.size.y;
            var tree = Object.Instantiate(asset.Prefab, parent, false);
            tree.name = name + " (Pandazole)";
            tree.transform.SetLocalPositionAndRotation(point - Vector3.up * (asset.Bounds.min.y * scale),
                Quaternion.Euler(0, seed * 61, 0));
            tree.transform.localScale = (stout ? new Vector3(.70f, 1, .70f) : Vector3.one) * scale;
            foreach (var renderer in tree.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterial = renderer.transform == tree.transform ? palmTrunk : palmCrown;
            foreach (var collider in tree.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        }

        static GameObject LoadGardenPrefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ParkNaturePack.PandazoleFolder + "Prefabs/" + name + ".prefab");
    }
}
