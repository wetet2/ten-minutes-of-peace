using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkDesertCampSetup
    {
        internal static readonly Rect SceneryClearance = Rect.MinMaxRect(8.7f, -10.8f, 19.55f, 6f);
        static readonly Vector2 PicnicCenter = new Vector2(12.9f, -6.15f);

        internal static ParkDesertCamp Create(Transform desert, ParkDuneTerrain terrain, List<Rect> obstacles,
            out ParkHouse entrance)
        {
            var root = Group("Oasis campsite", desert, Vector3.zero);
            var rounded = ParkSurface.RoundedBox();
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            var sphere = PrimitiveMesh(PrimitiveType.Sphere);
            var wood = Paint("Campsite honey oak", "#A5653F");
            var rope = Paint("Warm flax", "#F4D8A9");
            var red = Paint("Clay-red picnic weave", "#C7674B");
            var tent = MakeTent(root, wood, rope, cylinder);
            var rug = Group("Round picnic mat", root, Vector3.zero);
            MakeRug(rug, red, rope, rounded);
            var matCenter = Group("Picnic circle", root, At(PicnicCenter));
            var flames = MakeCampfire(matCenter, cylinder, rounded);
            obstacles.Add(new Rect(PicnicCenter - Vector2.one * 2.52f, Vector2.one * 5.04f));
            obstacles.Add(Rect.MinMaxRect(12.25f, -2.20f, 19f, 4.2f));
            AddPalm(root, obstacles);

            var spots = new[]
            {
                new Vector2(-1.82f, 1.05f), new Vector2(1.82f, 1.05f), new Vector2(0, -2.10f)
            };
            var arrivals = new[]
            {
                new Vector2(-3.5f, 1.05f), new Vector2(3.5f, 1.05f), new Vector2(0, -3.52f)
            };
            string[] colors = { "#E7B25E", "#83C6BC", "#BF86B1" };
            var seats = new ParkDesertCamp.Place[spots.Length];
            for (int i = 0; i < seats.Length; i++)
            {
                var cushion = Group("Fold-out cushion", root, At(PicnicCenter + spots[i]) + Vector3.up * .055f);
                var facing = -spots[i];
                cushion.localRotation = Quaternion.LookRotation(new Vector3(facing.x, 0, facing.y));
                ParkGeometry.Place("Soft cushion", rounded, cushion, Vector3.up * .085f,
                    new Vector3(1.63f, .17f, 1.48f), Paint("Picnic cushion", colors[i]));
                seats[i] = new ParkDesertCamp.Place
                {
                    Approach = Group("Picnic approach", root, At(PicnicCenter + arrivals[i])),
                    Cushion = cushion
                };
            }

            var lanternBase = Group("Picnic lantern", root, At(PicnicCenter + new Vector2(2.50f, -2.05f)));
            var frame = new Pieces();
            frame.Add(cylinder, wood, Vector3.up * .105f, new Vector3(.55f, .105f, .55f));
            frame.Add(cylinder, wood, Vector3.up * .66f, new Vector3(.56f, .085f, .56f));
            frame.Add(sphere, rope, Vector3.up * .80f, new Vector3(.20f, .17f, .20f));
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2 / 5;
                frame.Add(rounded, wood, new Vector3(Mathf.Cos(angle) * .22f, .40f, Mathf.Sin(angle) * .22f),
                    new Vector3(.047f, .52f, .047f));
            }
            frame.Build(lanternBase);
            var glassMaterial = new Material(Shader.Find("Unlit/Color")) { name = "Warm glowing lantern glass" };
            var glass = ParkGeometry.Place("Lantern glass", cylinder, lanternBase, Vector3.up * .41f,
                new Vector3(.43f, .24f, .43f), glassMaterial).GetComponent<MeshRenderer>();
            var light = Group("Lantern glow", lanternBase, Vector3.up * .59f).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1, .65f, .25f);
            light.intensity = .55f;
            light.range = 4.5f;
            light.enabled = false;

            var snack = Group("Shared cookie", root, Vector3.zero);
            var cookie = new Pieces();
            cookie.Add(cylinder, Paint("Golden oat cookie", "#EDB362"), Vector3.zero, new Vector3(.37f, .045f, .37f),
                Quaternion.Euler(72, 0, 0));
            var chips = Paint("Chocolate cookie chips", "#8F4B34");
            foreach (var chip in new[] { new Vector2(-.075f, .07f), new Vector2(.083f, .034f), new Vector2(-.02f, -.083f) })
                cookie.Add(sphere, chips, new Vector3(chip.x, chip.y, .047f), Vector3.one * .053f);
            cookie.Build(snack);

            var camp = root.gameObject.AddComponent<ParkDesertCamp>();
            entrance = tent.gameObject.AddComponent<ParkHouse>();
            entrance.DisplayName = "텐트";
            entrance.CampTent = camp;
            entrance.ExitTerrain = terrain;
            entrance.DoorDuration = .14f;
            entrance.ExitSeconds = 2.65f;
            var inside = Marker("Inside tent", 0, -1.01f);
            var door = Marker("Tent doorway", 0, -2.10f);
            entrance.Inside = Marker("Tent arrival start", 0, 1.15f);
            entrance.Threshold = door;
            entrance.Welcome = Marker("Tent greeting spot", -2.65f, -4.12f);
            camp.Configure(seats, Marker("Tent approach", 0, -4.12f), inside, door,
                matCenter, snack, glass, light, flames, entrance);
            return camp;

            Transform Marker(string name, float x, float z)
            {
                var marker = Group(name, root, Vector3.zero);
                var point = root.InverseTransformPoint(tent.TransformPoint(new Vector3(x, 0, z)));
                marker.localPosition = At(new Vector2(point.x, point.z));
                return marker;
            }
        }

        static Transform MakeCampfire(Transform center, Mesh cylinder, Mesh rounded)
        {
            var fire = Group("Stone-ring campfire", center, Vector3.zero);
            var rocks = new[] { Paint("Sandstone fire ring", "#B9865C"), Paint("Pale hearth stones", "#C8A078") };
            var logs = Paint("Campfire wood", "#76513B");
            var cuts = Paint("Split wood ends", "#C19A61");
            var embers = Paint("Glowing coals", "#A84E30");
            var stone = ParkGeometry.Canopy();
            var hearth = new Pieces();
            hearth.Add(cylinder, embers, Vector3.up * .045f, new Vector3(1.28f, .045f, 1.28f));
            for (int i = 0; i < 11; i++)
            {
                float angle = i * Mathf.PI * 2 / 11;
                var point = new Vector3(Mathf.Cos(angle) * .78f, .11f, Mathf.Sin(angle) * .78f);
                hearth.Add(stone, rocks[i % rocks.Length], point, new Vector3(.48f, .26f + .04f * (i % 3), .40f),
                    Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 7 * (i % 3 - 1)));
            }
            for (int i = 0; i < 3; i++)
            {
                var direction = Quaternion.Euler(0, i * 60 + 12, 0) * Vector3.forward;
                var basePoint = Vector3.up * (.15f + .08f * i);
                hearth.Beam(cylinder, logs, basePoint - direction * .59f, basePoint + direction * .59f, .22f);
                foreach (int side in new[] { -1, 1 })
                    hearth.Beam(cylinder, cuts, basePoint + direction * (.585f * side),
                        basePoint + direction * (.603f * side), .18f);
            }
            hearth.Build(fire);

            AddSkewers(fire, cylinder, rounded);
            var flames = Group("Flickering flames", fire, Vector3.up * .24f);
            var mesh = FlameMesh();
            var orange = Glow("Warm orange flame", new Color(1, .39f, .10f));
            var gold = Glow("Gold flame tips", new Color(1, .69f, .22f));
            Flame("Low orange flame", new Vector3(.13f, 0, .07f), new Vector3(.68f, .32f, .65f), 24, orange);
            Flame("Small orange flame", new Vector3(-.22f, -.035f, -.03f), new Vector3(.53f, .23f, .57f), -46, orange);
            Flame("Golden flame center", new Vector3(-.02f, .015f, -.16f), new Vector3(.56f, .24f, .58f), -18, gold);
            Flame("Small golden flame", new Vector3(.27f, -.035f, -.13f), new Vector3(.37f, .17f, .43f), 112, gold);
            return flames;

            static Material Glow(string name, Color color) =>
                new Material(Shader.Find("Unlit/Color")) { name = name, color = color };

            void Flame(string name, Vector3 position, Vector3 size, float yaw, Material color)
            {
                var item = ParkGeometry.Place(name, mesh, flames, position, size, color);
                item.localRotation = Quaternion.Euler(0, yaw, 0);
                item.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void AddSkewers(Transform fire, Mesh cylinder, Mesh rounded)
        {
            const float height = .82f;
            var grill = Group("Skewers over the fire", fire, Vector3.zero);
            grill.localRotation = Quaternion.Euler(0, -18, 0);
            var iron = Paint("Dark skewer supports", "#544639");
            var bamboo = Paint("Bamboo skewer", "#EBBE77");
            var meat = Paint("Golden-brown grilled meat", "#B46639");
            var caramel = Paint("Caramelized meat edges", "#8F4629");
            var sear = Paint("Dark grill marks", "#603420");
            var support = new Pieces();
            foreach (float x in new[] { -.66f, .66f })
            {
                foreach (float z in new[] { -.35f, .35f })
                    support.Beam(cylinder, iron, new Vector3(x, .04f, z), new Vector3(x, height - .035f, z), .055f);
                support.Beam(cylinder, iron, new Vector3(x, height - .035f, -.40f),
                    new Vector3(x, height - .035f, .40f), .064f);
            }
            support.Build(Group("Low cooking supports", grill, Vector3.zero));

            for (int row = 0; row < 2; row++)
            {
                float z = row == 0 ? -.20f : .20f;
                var skewer = Group($"Meat skewer {row + 1}", grill, new Vector3(0, height, z));
                var pieces = new Pieces();
                pieces.Beam(cylinder, bamboo, Vector3.left * .94f, Vector3.right * 1.11f, .066f);
                for (int i = 0; i < 4; i++)
                {
                    var position = new Vector3(-.46f + i * .29f + row * .04f, .032f, 0);
                    var twist = Quaternion.Euler(0, (i % 3 - 1) * 8f, (i % 2 == 0 ? 1 : -1) * 5f);
                    pieces.Add(rounded, i % 3 == 0 ? caramel : meat, position, new Vector3(.26f, .21f, .28f), twist);
                    foreach (float x in new[] { -.058f, .045f })
                        pieces.Add(rounded, sear, position + twist * new Vector3(x, .103f, 0),
                            new Vector3(.021f, .015f, .158f), twist);
                }
                pieces.Build(skewer);
            }
        }

        static Mesh FlameMesh()
        {
            const int sides = 9;
            var rings = new[]
            {
                new Vector3(0, .00f, .23f), new Vector3(-.035f, .22f, .36f),
                new Vector3(-.13f, .56f, .26f), new Vector3(-.075f, .90f, .13f)
            };
            var vertices = new List<Vector3>();
            var faces = new List<int>();
            foreach (var ring in rings)
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(ring.x + Mathf.Cos(angle) * ring.z, ring.y, Mathf.Sin(angle) * ring.z * .76f));
            }
            int peak = vertices.Count;
            vertices.Add(new Vector3(.13f, 1.27f, 0));
            for (int band = 0; band < rings.Length; band++)
            for (int side = 0; side < sides; side++)
            {
                int current = band * sides + side;
                int next = band * sides + (side + 1) % sides;
                if (band == rings.Length - 1) faces.AddRange(new[] { current, peak, next });
                else faces.AddRange(new[] { current, current + sides, next, next, current + sides, next + sides });
            }
            var mesh = new Mesh { name = "Curved campfire flame", vertices = vertices.ToArray(), triangles = faces.ToArray() };
            mesh.RecalculateNormals();
            return mesh;
        }

        static Transform MakeTent(Transform root, Material wood, Material rope, Mesh cylinder)
        {
            var tent = Group("Teal A-frame tent", root, At(new Vector2(15.65f, .85f)) - Vector3.up * .07f);
            tent.localRotation = Quaternion.Euler(0, 24, 0);
            const float half = 2.51f, height = 4.42f, front = -2.02f, back = 2.10f;
            var peakFront = new Vector3(0, height, front);
            var peakBack = new Vector3(0, height, back);
            var teal = Paint("Oasis teal canvas", "#548C83");
            var inside = Paint("Shaded canvas interior", "#4A7062");
            var flap = Paint("Cream canvas door flaps", "#F4E1BB");
            var trim = Paint("Terracotta tent seams", "#D5894C");
            foreach (int side in new[] { -1, 1 })
            {
                var edgeFront = new Vector3(side * half, 0, front);
                var edgeBack = new Vector3(side * half, 0, back);
                Panel(tent, "Canvas roof panel", teal, edgeFront, edgeBack, peakBack, peakFront);
                Panel(tent, "Tied-back canvas flap", flap, edgeFront + Vector3.back * .025f,
                    new Vector3(side * .87f, .10f, front - .17f),
                    new Vector3(side * 1.13f, 1.11f, front - .10f), peakFront + Vector3.back * .035f);
            }
            Panel(tent, "Canvas back wall", inside, new Vector3(-half, 0, back), peakBack, new Vector3(half, 0, back));

            var posts = new Pieces();
            foreach (float z in new[] { front - .12f, back + .10f })
            foreach (int side in new[] { -1, 1 })
            {
                posts.Beam(cylinder, wood, new Vector3(side * 2.60f, -.13f, z),
                    new Vector3(side * -.34f, height + .57f, z), .105f);
                posts.Beam(cylinder, rope, new Vector3(side * .86f, .24f, front - .21f),
                    new Vector3(side * 1.12f, .44f, front - .18f), .095f);
                posts.Beam(cylinder, trim, new Vector3(side * half, .015f, z), new Vector3(0, height, z), .060f);
            }
            posts.Beam(cylinder, trim, peakFront + Vector3.back * .18f, peakBack + Vector3.forward * .18f, .072f);
            posts.Build(tent);
            return tent;
        }

        static void Panel(Transform parent, string name, Material material, params Vector3[] corners)
        {
            var vertices = new Vector3[corners.Length * 2];
            corners.CopyTo(vertices, 0);
            corners.CopyTo(vertices, corners.Length);
            var faces = new List<int>();
            for (int i = 1; i < corners.Length - 1; i++)
            {
                faces.AddRange(new[] { 0, i, i + 1, corners.Length, corners.Length + i + 1, corners.Length + i });
            }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = faces.ToArray() };
            mesh.RecalculateNormals();
            ParkGeometry.Place(name, mesh, parent, Vector3.zero, Vector3.one, material);
        }

        static void MakeRug(Transform rug, Material red, Material rope, Mesh rounded)
        {
            const int segments = 56;
            const float outer = 2.96f;
            Band(1.20f, 1.36f, rope);
            Band(1.36f, 2.34f, red);
            Band(2.34f, 2.48f, rope);
            Band(2.48f, 2.74f, red);
            Band(2.74f, outer, rope);
            var fringe = new Pieces();
            for (int i = 0; i < segments; i += 2)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                fringe.Add(rounded, rope, At(PicnicCenter + radial * (outer + .05f)) + Vector3.up * .04f,
                    new Vector3(.17f, .026f, .086f), Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0));
            }
            fringe.Build(rug);

            void Band(float inner, float edge, Material color)
            {
                var vertices = new Vector3[segments * 2];
                var faces = new int[segments * 6];
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    var radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    int index = i * 2;
                    int next = (index + 2) % vertices.Length;
                    vertices[index] = At(PicnicCenter + radial * inner) + Vector3.up * .045f;
                    vertices[index + 1] = At(PicnicCenter + radial * edge) + Vector3.up * .045f;
                    int face = i * 6;
                    faces[face] = index;
                    faces[face + 1] = next + 1;
                    faces[face + 2] = index + 1;
                    faces[face + 3] = index;
                    faces[face + 4] = next;
                    faces[face + 5] = next + 1;
                }
                var mesh = new Mesh { name = color.name + " mat band", vertices = vertices, triangles = faces };
                mesh.RecalculateNormals();
                ParkGeometry.Place(mesh.name, mesh, rug, Vector3.zero, Vector3.one, color);
            }
        }

        static void AddPalm(Transform root, List<Rect> obstacles)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                ParkNaturePack.PandazoleFolder + "Prefabs/Tree_10_Spring.prefab");
            if (!source) return;
            var bounds = ParkNaturePack.PrefabBounds(source);
            if (bounds.size.y <= 0) return;
            var point = new Vector2(11.6f, 2.9f);
            var tree = Object.Instantiate(source, root, false);
            tree.name = "Campsite shade palm";
            float scale = 5.8f / bounds.size.y;
            tree.transform.SetLocalPositionAndRotation(At(point) - Vector3.up * (bounds.min.y * scale),
                Quaternion.Euler(0, 58, 0));
            tree.transform.localScale = Vector3.one * scale;
            foreach (var collider in tree.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            obstacles.Add(new Rect(point - Vector2.one * .64f, Vector2.one * 1.28f));
        }

        static Vector3 At(Vector2 point) => new Vector3(point.x, ParkDuneTerrain.Height(point.x, point.y), point.y);
    }
}
