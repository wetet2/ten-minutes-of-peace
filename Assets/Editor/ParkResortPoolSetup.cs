using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;
using static PeacePark.ParkResortRiver;
using static PeacePark.ParkResortSlide;

namespace PeacePark.Editor
{
    internal sealed partial class ParkResortPoolSetup
    {
        const float FootprintScale = .50f;
        readonly Mesh cube = PrimitiveMesh(PrimitiveType.Cube);
        readonly Mesh cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Mesh sphere = PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh softBox = ParkSurface.RoundedBox();
        readonly Material ivory = Paint("Ivory limestone", "#FFF0D7");
        readonly Material honey = Paint("Honey limestone", "#DDB793");
        readonly Material wood = Paint("Teak", "#AE713F");
        readonly Material darkWood = Paint("Dark teak", "#795036");
        readonly Material cream = Paint("Canvas ivory", "#FFF6E4");
        readonly Material sage = Paint("Sea-green linen", "#A4C6A0");
        readonly Material grass = Paint("Garden lawn", "#88AA59");
        readonly Material orange = Paint("Golden stamens", "#FAC15A");
        readonly List<Rect> obstacles = new List<Rect>();

        internal static ParkVenue Create(Transform root) => new ParkResortPoolSetup().Build(root);

        static void ScaleAboveDeck(Transform group, float heightScale)
        {
            group.localScale = new Vector3(1, heightScale, 1);
            group.localPosition += Vector3.up * (DeckHeight * (1 - heightScale));
        }

        ParkVenue Build(Transform root)
        {
            var grounds = Group("Limestone pool terraces", root, Vector3.zero);
            MakeWaterAndDeck(grounds);
            MakeBridge(grounds);
            MakeLandscaping(root);
            var sunbeds = MakeLoungers(root);
            var entrance = MakeHotel(root);
            var slide = MakeWaterSlide(root);
            var river = Group("Resort lazy river", root, Vector3.zero).gameObject.AddComponent<ParkResortRiver>();
            river.Configure(MakeTube(river.transform));
            var swim = Group("Main pool swimming", root, Vector3.zero).gameObject.AddComponent<ParkResortSwim>();
            return ScaleVenue(new ParkVenue
            {
                Name = "수영장", Root = root.gameObject, ResortRiver = river, ResortSwim = swim, ResortSlide = slide,
                ResortSunbeds = sunbeds, Houses = new[] { entrance },
                LawnHeight = DeckHeight, WalkArea = Rect.MinMaxRect(TerraceWest + 1.35f, -20.30f, 31.1f, 19.1f),
                WalkObstacles = obstacles.ToArray(), Focus = new Vector3(-9.45f, 3.65f, 4.1f), Distance = 52,
                Pitch = 31, Yaw = -2, MaxDistance = 120, FarClipDistance = 200, CameraPanLimits = new Vector2(56, 37),
                FogStart = 108, FogEnd = 176, ShadowDistance = 92, Sky = new Color32(178, 221, 239, 255),
                Preview = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Venues/ResortPool.png")
            }, new Vector3(FootprintScale, 1, FootprintScale), viewScale: .64f);
        }

        void MakeWaterAndDeck(Transform root)
        {
            const float divider = -5.25f, poolFloor = -1.88f, riverFloor = -1.85f;
            var paving = Tiles("Warm limestone pavers", "#E0BB99", "#CDAF92", 1.31f, .93f, true);
            var liner = Tiles("Pale turquoise mosaic", "#67B3B2", "#83BDBC", .39f, .39f, false);
            var floor = Tiles("Clear blue pool floor", "#83C3BC", "#A0D0C6", .49f, .49f, false);
            var pool = Outline(PoolCenter, PoolHalf, PoolRadius);
            var outside = RiverBank(ChannelHalfWidth, true);
            var inside = RiverBank(-ChannelHalfWidth, false);
            var leftDeck = Rectangle(TerraceWest, -21.2f, divider, 20f);
            var rightDeck = Rectangle(divider, -21.2f, 32.5f, 20f);
            MakeMesh("Main pool deck", Band(leftDeck, pool, DeckHeight, DeckHeight), paving, root);
            MakeMesh("River deck", Band(rightDeck, outside, DeckHeight, DeckHeight), paving, root);
            MakeMesh("Garden surrounds", Band(Rectangle(-190, -170, 190, 185), Rectangle(TerraceWest, -21.2f, 32.5f, 20f), -.05f, .035f), grass, root);
            MakeMesh("Main pool mosaic bedding", Fill(pool, poolFloor), floor, root);
            MakeMesh("Main pool mosaic walls", Band(pool, pool, DeckHeight, poolFloor), liner, root);
            MakeMesh("River mosaic bedding", Band(outside, inside, riverFloor, riverFloor), floor, root);
            MakeMesh("River outer wall", Band(outside, outside, DeckHeight, riverFloor), liner, root);
            MakeMesh("River island wall", Band(inside, inside, riverFloor, DeckHeight), liner, root);
            MakeMesh("Garden island terrace", Fill(inside, DeckHeight), paving, root);
            var islandGarden = Outline(RiverCenter, RiverHalf - Vector2.one * IslandGardenInset, RiverRadius - IslandGardenInset);
            MakeMesh("Raised garden lawn", Fill(islandGarden, IslandLawnHeight), grass, root);
            MakeMesh("Raised garden limestone edge", Band(islandGarden, islandGarden, DeckHeight, IslandLawnHeight), honey, root);

            MakeMesh("Main pool stone coping", Band(Outline(PoolCenter, PoolHalf + Vector2.one * .32f, PoolRadius + .32f), pool, .127f, .127f), ivory, root);
            MakeMesh("River outside stone coping", Band(RiverBank(ChannelHalfWidth + .32f, true), outside, .127f, .127f), ivory, root);
            MakeMesh("River island stone coping", Band(inside, RiverBank(-ChannelHalfWidth - .32f, false), .127f, .127f), ivory, root);

            var baseWater = new Material(Shader.Find("PeacePark/Resort Water")) { name = "Clear swimming pool" };
            var riverWater = new Material(baseWater) { name = "Slow river current" };
            riverWater.SetFloat("_Current", 1);
            MakeMesh("Main pool water", Fill(pool, WaterHeight), baseWater, root, false);
            MakeMesh("Closed river water circuit", Band(outside, inside, WaterHeight, WaterHeight), riverWater, root, false);

            var steps = Group("Wide pool steps", root, Vector3.zero);
            var rim = ParkResortSwim.StepsRim;
            for (int i = 0; i < ParkResortSwim.StepCount; i++)
            {
                float radius = ParkResortSwim.StepRadius(i);
                float y = ParkResortSwim.StepHeight(i);
                var outline = new Vector2[25];
                for (int j = 0; j < outline.Length; j++)
                {
                    float angle = Mathf.PI * j / (outline.Length - 1);
                    outline[j] = rim + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                MakeMesh("Rounded submerged tread", Fill(outline, y), liner, steps);
                MakeMesh("Rounded submerged riser", Band(outline, outline, y - .33f, y), liner, steps);
            }
            var dock = Group("Tube boarding steps", root, Vector3.zero);
            var treads = new Pieces();
            for (int i = 0; i < 4; i++)
                treads.Add(cube, i == 0 ? ivory : liner, new Vector3(RiverCenter.x, -.12f - .245f * i, DockEdge + .22f + .49f * i),
                    new Vector3(3.55f, .28f, .52f));
            foreach (int side in new[] { -1, 1 })
            {
                var post = new Vector3(RiverCenter.x + side * 2.22f, .32f, DockEdge - .05f);
                treads.Add(softBox, ivory, post, new Vector3(.52f, .65f, .52f));
                treads.Add(sphere, ivory, post + Vector3.up * .43f, Vector3.one * .43f);
            }
            treads.Build(dock);
        }

        void MakeBridge(Transform root)
        {
            var bridge = Group("Limestone bridge over the river", root, new Vector3(BridgeCenterX, 0, BridgeZ));
            const int sections = 64;
            var centerline = new Vector3[sections + 1];
            for (int i = 0; i <= sections; i++)
            {
                float x = Mathf.Lerp(-BridgeHalfSpan, BridgeHalfSpan, i / (float)sections);
                centerline[i] = new Vector3(x, BridgeSurfaceHeight(BridgeCenterX + x), 0);
            }
            MakeMesh("Continuous curved stone deck", BridgeSpan(centerline, new Vector3(0, -BridgeDeckDepth * .5f, 0),
                BridgeHalfWidth * 2, BridgeDeckDepth), ivory, bridge);

            var pieces = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                float edge = side * (BridgeHalfWidth - .11f);
                MakeMesh("Continuous limestone handrail", BridgeSpan(centerline, new Vector3(0, .66f, edge), .18f, .12f), ivory, bridge);
                for (int i = 4; i < sections; i += 4)
                {
                    var foot = centerline[i] + new Vector3(0, 0, edge);
                    pieces.Add(softBox, ivory, foot + Vector3.up * .056f, new Vector3(.195f, .112f, .195f));
                    pieces.Add(cylinder, honey, foot + Vector3.up * .292f, new Vector3(.125f, .315f, .125f));
                }
                foreach (int end in new[] { 0, sections })
                {
                    var foot = centerline[end] + new Vector3(0, 0, edge);
                    pieces.Add(softBox, ivory, foot + Vector3.up * .365f, new Vector3(.40f, .73f, .40f));
                    pieces.Add(sphere, ivory, foot + Vector3.up * .775f, Vector3.one * .27f);
                }
            }
            pieces.Build(bridge);
        }

        static Mesh BridgeSpan(Vector3[] centerline, Vector3 offset, float width, float thickness)
        {
            float halfWidth = width * .5f, halfThickness = thickness * .5f;
            var corners = new[]
            {
                new Vector3(0, halfThickness, -halfWidth), new Vector3(0, halfThickness, halfWidth),
                new Vector3(0, -halfThickness, halfWidth), new Vector3(0, -halfThickness, -halfWidth)
            };
            var points = new List<Vector3>(centerline.Length * 8 + 8);
            var triangles = new List<int>((centerline.Length - 1) * 24 + 12);
            for (int face = 0; face < corners.Length; face++)
            {
                int run = points.Count;
                foreach (var point in centerline)
                {
                    points.Add(point + offset + corners[face]);
                    points.Add(point + offset + corners[(face + 1) % corners.Length]);
                }
                for (int i = 0; i < centerline.Length - 1; i++)
                {
                    int a = run + i * 2, b = a + 2;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                    triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
                }
            }
            Cap(0, true);
            Cap(centerline.Length - 1, false);
            return MeshOf("Smooth bridge span", points.ToArray(), triangles.ToArray());

            void Cap(int end, bool reverse)
            {
                int first = points.Count;
                foreach (var corner in corners) points.Add(centerline[end] + offset + corner);
                triangles.Add(first); triangles.Add(first + (reverse ? 2 : 1)); triangles.Add(first + (reverse ? 1 : 2));
                triangles.Add(first); triangles.Add(first + (reverse ? 3 : 2)); triangles.Add(first + (reverse ? 2 : 3));
            }
        }

        Transform MakeTube(Transform parent)
        {
            var tube = Group("Tube at the boarding steps", parent, Vector3.zero);
            tube.localScale = new Vector3(1 / FootprintScale, 1, 1 / FootprintScale);
            var blue = Paint("Lagoon-blue tube", "#4BB5ED");
            var peach = Paint("Peach tube", "#F58F77");
            var mesh = Torus(false);
            MakeMesh("Ivory tube bands", Torus(true), cream, tube, false);
            MakeMesh("Blue bands", mesh, blue, tube, false);
            MakeMesh("Peach bands", mesh, peach, tube, false).gameObject.SetActive(false);
            var handles = new Pieces();
            foreach (int side in new[] { -1, 1 })
                handles.Add(softBox, wood, new Vector3(TubeHandle.x * side, TubeHandle.y, TubeHandle.z), new Vector3(.145f, .09f, .25f));
            handles.Build(tube);
            return tube;
        }

        static Mesh Torus(bool stripes)
        {
            const int ringSegments = 64, crossSegments = 12;
            var points = new List<Vector3>();
            var faces = new List<int>();
            for (int i = 0; i < ringSegments; i++)
            {
                if ((i / 8 % 2 == 0) != stripes) continue;
                for (int j = 0; j < crossSegments; j++)
                {
                    int n = points.Count;
                    points.Add(Vertex(i, j)); points.Add(Vertex(i, j + 1));
                    points.Add(Vertex(i + 1, j)); points.Add(Vertex(i + 1, j + 1));
                    faces.Add(n); faces.Add(n + 1); faces.Add(n + 2);
                    faces.Add(n + 1); faces.Add(n + 3); faces.Add(n + 2);
                }
            }
            return MeshOf("Inflatable tube band geometry", points.ToArray(), faces.ToArray());

            Vector3 Vertex(int i, int j)
            {
                float angle = 2 * Mathf.PI * i / ringSegments, section = 2 * Mathf.PI * j / crossSegments;
                float thickness = .28f * Mathf.Cos(section);
                return new Vector3(Mathf.Cos(angle) * (.635f + thickness), .22f * Mathf.Sin(section),
                    Mathf.Sin(angle) * (.58f + thickness));
            }
        }

        static Vector2[] Rectangle(float left, float front, float right, float back)
        {
            var rect = Rect.MinMaxRect(left, front, right, back);
            return Outline(rect.center, rect.size * .5f, 0);
        }

        static Vector2[] RiverBank(float offset, bool dock)
        {
            var half = RiverHalf + Vector2.one * offset;
            var bank = Outline(RiverCenter, half, RiverRadius + offset);
            if (dock)
                for (int i = 0; i < bank.Length; i++)
                    if (Mathf.Abs(bank[i].y - (RiverCenter.y - half.y)) < .01f)
                        bank[i].y -= DockExtension(Mathf.Abs(bank[i].x - RiverCenter.x));
            return bank;
        }

        static Mesh Fill(Vector2[] outline, float height)
        {
            var points = new Vector3[outline.Length + 1];
            var triangles = new int[outline.Length * 3];
            var center = Vector2.zero;
            for (int i = 0; i < outline.Length; i++)
            {
                center += outline[i];
                points[i + 1] = At(outline[i], height);
                triangles[3 * i + 1] = (i + 1) % outline.Length + 1;
                triangles[3 * i + 2] = i + 1;
            }
            points[0] = At(center / outline.Length, height);
            return MeshOf("Filled resort outline", points, triangles);
        }

        static Mesh Band(Vector2[] outside, Vector2[] inside, float outerY, float innerY)
        {
            int count = outside.Length;
            var points = new Vector3[(count + 1) * 2];
            var uv = new Vector2[points.Length];
            var triangles = new int[count * 6];
            var lengths = new float[count + 1];
            for (int i = 1; i <= count; i++)
                lengths[i] = lengths[i - 1] + Vector2.Distance((outside[i - 1] + inside[i - 1]) * .5f,
                    (outside[i % count] + inside[i % count]) * .5f);
            for (int i = 0; i <= count; i++)
            {
                int point = 2 * i;
                points[point] = At(outside[i % count], outerY);
                points[point + 1] = At(inside[i % count], innerY);
                uv[point] = new Vector2(lengths[i] / lengths[count], 0);
                uv[point + 1] = new Vector2(uv[point].x, 1);
                if (i == count) continue;
                int face = 6 * i;
                triangles[face] = point; triangles[face + 1] = point + 1; triangles[face + 2] = point + 2;
                triangles[face + 3] = point + 2; triangles[face + 4] = point + 1; triangles[face + 5] = point + 3;
            }
            return MeshOf("Continuous resort border", points, triangles, uv);
        }

        static Vector3 At(Vector2 p, float height) => new Vector3(p.x, height, p.y);

        static Mesh MeshOf(string name, Vector3[] points, int[] triangles, Vector2[] uv = null)
        {
            var mesh = new Mesh { name = name, vertices = points, triangles = triangles };
            if (uv != null) mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Transform MakeMesh(string name, Mesh mesh, Material material, Transform root, bool shadows = true)
        {
            var part = ParkGeometry.Place(name, mesh, root, Vector3.zero, Vector3.one, material);
            if (!shadows) part.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return part;
        }

        static Material Tiles(string name, string color, string grout, float width, float height, bool stagger)
        {
            var material = new Material(Shader.Find("PeacePark/Resort Paving")) { name = name };
            ColorUtility.TryParseHtmlString(color, out var pigment);
            ColorUtility.TryParseHtmlString(grout, out var joints);
            material.SetColor("_Color", pigment);
            material.SetColor("_Grout", joints);
            material.SetVector("_Cell", new Vector4(width, height, 0, 0));
            material.SetFloat("_Stagger", stagger ? 1 : 0);
            return material;
        }

        void Block(float x, float z, float halfWidth, float halfDepth) =>
            obstacles.Add(Rect.MinMaxRect(x - halfWidth, z - halfDepth, x + halfWidth, z + halfDepth));
    }
}
