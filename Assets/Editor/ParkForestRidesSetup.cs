using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkForestRidesSetup
    {
        readonly Mesh box = ParkSurface.RoundedBox();
        readonly Mesh cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Mesh stone = ParkGeometry.Canopy();
        readonly Material bark = Paint("Chestnut tree bark", "#815039");
        readonly Material darkWood = Paint("Deep woodland timber", "#986038");
        readonly Material wood = Paint("Honey oak", "#C88B4D");
        readonly Material lightWood = Paint("Golden cut wood", "#E7B872");
        readonly Material metal = Paint("Zipline cable and hardware", "#55594C");
        readonly float ground;
        readonly List<Rect> obstacles;

        ParkForestRidesSetup(float ground, List<Rect> obstacles)
        {
            this.ground = ground;
            this.obstacles = obstacles;
        }

        internal static ParkForestRide[] Create(Transform root, float ground, List<Rect> obstacles)
        {
            var builder = new ParkForestRidesSetup(ground, obstacles);
            return new[] { builder.MakeStumps(root), builder.MakeBridge(root), builder.MakeZipline(root) };
        }

        Vector3 Floor(float x, float z) => new Vector3(x, ground, z);

        ParkForestRide MakeStumps(Transform root)
        {
            var group = Group("Forest stepping stumps", root, Vector3.zero);
            var tops = new[] { Floor(-8.80f,-5.90f), Floor(-7.13f,-5.18f), Floor(-5.38f,-5.41f),
                Floor(-3.64f,-6.09f), Floor(-1.83f,-6.27f) };
            var heights = new[] { .43f, .56f, .34f, .61f, .40f };
            var widths = new[] { 1.32f, 1.43f, 1.38f, 1.48f, 1.36f };
            var route = new Vector3[tops.Length + 1];
            var surface = MakeStumpSurface();
            var finish = MakeStumpFinish();
            for (int i = 0; i < tops.Length; i++)
            {
                var foot = tops[i];
                float height = heights[i], width = widths[i];
                var parts = new Pieces();
                parts.Add(surface, finish, foot + Vector3.up * (height / 2), new Vector3(width, height / 2, width));
                for (int j = 0; j < 14; j++)
                {
                    float angle = j * 360f / 14 + i * 13;
                    var radial = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    parts.Add(box, j % 3 == 0 ? wood : darkWood, foot + radial * (width * .49f) + Vector3.up * (height * .40f),
                        new Vector3(.045f + j % 3 * .018f, height * (.46f + j % 4 * .10f), .055f), Quaternion.Euler(0, angle, -6 + j % 4 * 4));
                }
                parts.Build(Group("Stepping stump " + (i + 1), group, Vector3.zero));
                route[i] = foot + Vector3.up * (height + .01f);
                AddFootprint(foot, width * .52f, width * .52f);
            }
            route[route.Length - 1] = Floor(-.62f, -6.25f);
            var ride = group.gameObject.AddComponent<ParkForestRide>();
            ride.Configure(ParkForestRide.Course.Stumps, Floor(-10.08f, -6.38f), route);
            return ride;
        }

        Mesh MakeStumpSurface()
        {
            var mesh = Object.Instantiate(cylinder);
            mesh.name = "Stump with one cut surface";
            var points = mesh.vertices;
            var normals = mesh.normals;
            var uv = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
                uv[i] = normals[i].y > .9f ? new Vector2(points[i].x + .5f, points[i].z + .5f) : Vector2.zero;
            mesh.uv = uv;
            return mesh;
        }

        Material MakeStumpFinish()
        {
            const int size = 128;
            const float blendWidth = 1f / size;
            var barkColor = bark.color;
            var cutColor = lightWood.color;
            Color ringColor = new Color32(197, 144, 86, 255);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2((x + .5f) / size - .5f, (y + .5f) / size - .5f);
                var color = Color.Lerp(cutColor, barkColor, Mathf.InverseLerp(.48f - blendWidth, .48f, point.magnitude));
                for (int i = 0; i < 7; i++)
                {
                    float radius = .42f - i * .055f;
                    float distance = (point - new Vector2(.0065f, -.0043f) * i).magnitude;
                    color = Color.Lerp(color, i % 2 == 0 ? ringColor : cutColor,
                        1 - Mathf.InverseLerp(radius - blendWidth, radius, distance));
                }
                pixels[y * size + x] = color;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Chestnut bark and annual rings", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            var material = Paint("Stump surface with annual rings", "#FFFFFF");
            material.mainTexture = texture;
            material.EnableKeyword("PEACE_PARK_PAINT_MAP");
            return material;
        }

        ParkForestRide MakeBridge(Transform root)
        {
            var group = Group("Forest wooden cloud ladder", root, Vector3.zero);
            var parts = new Pieces();
            var start = Floor(10.75f, -.65f);
            var end = Floor(3.70f, 1.60f);
            var along = (end - start).normalized;
            var side = Vector3.Cross(Vector3.up, along);
            var rotation = Quaternion.LookRotation(along);
            const int count = 13;
            const float half = 1.13f;
            const float halfTread = .245f;
            const float landingClearance = .86f;
            float footprintX = Mathf.Abs(side.x) * half + Mathf.Abs(along.x) * halfTread;
            float footprintZ = Mathf.Abs(side.z) * half + Mathf.Abs(along.z) * halfTread;
            var path = new Vector3[count + 1];
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                var step = Vector3.Lerp(start, end, t) + Vector3.up * (.19f + 1.33f * Mathf.Sin(Mathf.PI * t));
                path[i] = step;
                Plank(parts, i % 3 == 0 ? lightWood : wood, step, new Vector3(half * 2, .13f, halfTread * 2), rotation);
                AddFootprint(step, footprintX, footprintZ);
                foreach (int sign in new[] { -1, 1 })
                {
                    var edge = step + side * (sign * (half - .12f));
                    if (i % 3 == 0)
                    {
                        float footHeight = edge.y - ground + .79f;
                        parts.Add(box, darkWood, edge + Vector3.up * (.42f - footHeight * .5f + .37f),
                            new Vector3(.18f, footHeight, .18f));
                        parts.Add(box, lightWood, edge + Vector3.up * .80f, Vector3.one * .22f);
                    }
                    if (i == 0) continue;
                    var before = path[i - 1] + side * (sign * (half - .12f));
                    Timber(parts, before - Vector3.up * .16f, edge - Vector3.up * .16f, .21f, darkWood);
                    Timber(parts, before + Vector3.up * .68f, edge + Vector3.up * .68f, .14f, wood);
                }
            }
            parts.Build(group);
            path[count] = end + along * landingClearance;
            var entry = start - along * landingClearance;
            var ride = group.gameObject.AddComponent<ParkForestRide>();
            ride.Configure(ParkForestRide.Course.Bridge, entry, path);
            return ride;
        }

        ParkForestRide MakeZipline(Transform root)
        {
            var group = Group("Forest tree zipline", root, Vector3.zero);
            var tree = Floor(-10.88f, 2.43f);
            var launch = Floor(-8.61f, 1.27f) + Vector3.up * 4.38f;
            var dock = Floor(11.25f, -8.94f) + Vector3.up * .50f;
            var along = Vector3.ProjectOnPlane(dock - launch, Vector3.up).normalized;
            var lateral = Vector3.Cross(Vector3.up, along);
            var deckRotation = Quaternion.LookRotation(along);
            MakeLaunchTree(group, tree);

            var steps = new List<Vector3>();
            var climbing = new Pieces();
            const int count = 11;
            const float radius = 2.03f;
            for (int i = 0; i < count; i++)
            {
                float angle = (-166 + 13.2f * i) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var tangent = new Vector3(-radial.z, 0, radial.x);
                var top = tree + radial * radius + Vector3.up * (.30f + .371f * i);
                steps.Add(top);
                Plank(climbing, i % 3 == 0 ? lightWood : wood, top, new Vector3(1.30f, .16f, .69f), Quaternion.LookRotation(tangent));
                Timber(climbing, tree + radial * .72f + Vector3.up * (top.y - ground - .49f),
                    top - Vector3.up * .20f, .15f, darkWood);
            }
            climbing.Build(Group("Spiral log steps", group, Vector3.zero));
            var first = steps[0];
            var firstRadial = Vector3.ProjectOnPlane(first - tree, Vector3.up).normalized;
            var entry = tree + firstRadial * 2.92f;
            steps.Add(launch);

            var decking = new Pieces();
            Deck(decking, launch, 2.6f, 2.05f, deckRotation, ground + 2.35f);
            Deck(decking, dock, 2.48f, 2.24f, deckRotation, ground);
            RailDock(decking, launch, along, lateral, .90f);
            var farRail = dock - lateral * 1.13f;
            var exitRail = dock + lateral * 1.13f;
            RailRun(decking, farRail - along * .90f, farRail + along * .90f, .58f);
            RailRun(decking, exitRail + along * .60f, exitRail + along * 1.01f, .58f);
            RailPost(decking, exitRail - along * 1.07f, .58f);

            var dismount = dock - along * .25f;
            var upperStep = dismount + lateral * 1.42f - Vector3.up * .18f;
            var lowerStep = dismount + lateral * 1.82f - Vector3.up * .35f;
            var exiting = new[] { dismount, dismount + lateral * .53f, dismount + lateral * 1.04f,
                upperStep, lowerStep, dismount + lateral * 2.57f - Vector3.up * .50f };
            var sideStairs = Quaternion.LookRotation(lateral);
            Plank(decking, wood, upperStep, new Vector3(1.58f, .20f, .40f), sideStairs);
            Plank(decking, lightWood, lowerStep, new Vector3(1.58f, .16f, .40f), sideStairs);
            decking.Build(Group("Launch and landing decks", group, Vector3.zero));

            var from = launch + Vector3.up * (1.62f + ParkForestRide.HandleDrop);
            var to = dock + Vector3.up * (1.57f + ParkForestRide.HandleDrop);
            var suspended = new Pieces();
            // The cable overhangs the centre of each platform so the rider can mount and land there.
            var cablePerMeter = (to - from) / Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;
            var launchAnchor = from - cablePerMeter * .94f;
            var landingAnchor = to + cablePerMeter * .77f;
            suspended.Beam(cylinder, metal, launchAnchor, landingAnchor, .058f);
            suspended.Build(Group("Diagonal woodland cable", group, Vector3.zero));
            Anchor(group, "Launch cable anchor", launchAnchor, launch.y, lateral);
            Anchor(group, "Landing cable anchor", landingAnchor, dock.y, lateral);

            var trolley = Group("Zipline trolley", group, from);
            var carrier = new Pieces();
            carrier.Add(box, lightWood, new Vector3(0, .006f, 0), new Vector3(.25f, .23f, .46f));
            foreach (float z in new[] { -.14f, .14f })
                carrier.Add(cylinder, metal, new Vector3(0, .055f, z), new Vector3(.16f, .145f, .16f), Quaternion.Euler(0, 0, 90));
            const float halfHandle = .77f;
            foreach (int side in new[] { -1, 1 })
            {
                var shoulder = new Vector3(side * halfHandle, -.30f, 0);
                carrier.Beam(cylinder, metal, Vector3.down * .10f, shoulder, .054f);
                carrier.Beam(cylinder, metal, shoulder, new Vector3(shoulder.x, -ParkForestRide.HandleDrop, 0), .054f);
            }
            carrier.Beam(cylinder, wood, new Vector3(-halfHandle,-ParkForestRide.HandleDrop,0),
                new Vector3(halfHandle,-ParkForestRide.HandleDrop,0), .095f);
            carrier.Build(trolley);

            obstacles.Add(Rect.MinMaxRect(-13.34f, .02f, -7.11f, 3.6f));
            AddFootprint(dock, 1.62f, 1.64f);
            float stairHalfX = Mathf.Abs(along.x) * .79f + Mathf.Abs(lateral.x) * .20f;
            float stairHalfZ = Mathf.Abs(along.z) * .79f + Mathf.Abs(lateral.z) * .20f;
            AddFootprint(upperStep, stairHalfX, stairHalfZ);
            AddFootprint(lowerStep, stairHalfX, stairHalfZ);
            var ride = group.gameObject.AddComponent<ParkForestRide>();
            ride.Configure(ParkForestRide.Course.Zipline, entry, steps.ToArray());
            ride.ConfigureZipline(trolley, from, to, exiting);
            return ride;
        }

        void MakeLaunchTree(Transform parent, Vector3 tree)
        {
            var group = Group("Zipline pine trunk", parent, tree);
            var trunk = new Pieces();
            for (int i = 0; i < 7; i++)
            {
                float angle = 2 * Mathf.PI * i / 7;
                var radial = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                trunk.Add(stone, i % 2 == 0 ? bark : darkWood, radial * .76f + Vector3.up * .16f,
                    new Vector3(.52f,.32f,1.74f), Quaternion.LookRotation(radial));
            }
            const float height = 13.55f;
            trunk.Add(TaperedTrunk(), bark, Vector3.zero, new Vector3(1, height, 1));
            trunk.Build(group);
            var pine = new Pieces();
            var needles = Paint("Forest zipline pine needles", "#63894C");
            var tips = Paint("Forest pine needle tips", "#749A53");
            var bough = PineBough();
            for (int i = 0; i < 5; i++)
            {
                float width = 3.36f - i * .48f;
                pine.Add(bough, i % 2 == 0 ? needles : tips, Vector3.up * (8.83f + i * 1.06f),
                    new Vector3(width, 2.48f - i * .16f, width), Quaternion.Euler(0, i * 29, 0));
            }
            pine.Build(Group("Zipline pine crown", group, Vector3.zero));
        }

        void Deck(Pieces pieces, Vector3 top, float width, float length, Quaternion rotation, float supportBase)
        {
            var forward = rotation * Vector3.forward;
            var right = rotation * Vector3.right;
            for (int i = 0; i < 5; i++)
                Plank(pieces, i % 2 == 0 ? wood : lightWood, top + forward * ((i - 2) * length / 5),
                    new Vector3(width, .19f, length / 5 - .017f), rotation);
            foreach (int sign in new[] { -1, 1 })
            {
                var edge = top + right * (sign * width * .40f) - Vector3.up * .26f;
                Timber(pieces, edge - forward * (length * .52f), edge + forward * (length * .52f), .18f, darkWood);
                float supportHeight = edge.y - supportBase;
                pieces.Add(box, darkWood, new Vector3(edge.x, supportBase + supportHeight / 2, edge.z),
                    new Vector3(.25f, supportHeight, .25f));
            }
        }

        void RailDock(Pieces pieces, Vector3 deck, Vector3 along, Vector3 lateral, float height)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var edge = deck + lateral * (side * 1.13f);
                RailRun(pieces, edge - along * .83f, edge + along * .83f, height);
            }
        }

        void RailRun(Pieces pieces, Vector3 a, Vector3 b, float height)
        {
            RailPost(pieces, a, height);
            RailPost(pieces, b, height);
            Timber(pieces, a + Vector3.up * (height * .9f), b + Vector3.up * (height * .9f), .13f, lightWood);
        }

        void RailPost(Pieces pieces, Vector3 point, float height) =>
            pieces.Add(box, wood, point + Vector3.up * (height / 2), new Vector3(.17f, height, .17f));

        void Anchor(Transform parent, string name, Vector3 top, float floor, Vector3 lateral)
        {
            var offset = lateral * .31f;
            var footing = new Vector3(top.x, floor, top.z) + offset;
            var parts = new Pieces();
            Timber(parts, Vector3.zero, Vector3.up * (top.y - floor + .14f), .29f, darkWood);
            parts.Beam(cylinder, metal, top - offset * .21f - footing, top + offset - footing, .18f);
            parts.Build(Group(name, parent, footing));
        }

        void Plank(Pieces pieces, Material finish, Vector3 top, Vector3 size, Quaternion rotation) =>
            pieces.Add(box, finish, top - Vector3.up * (size.y / 2), size, rotation);

        void Timber(Pieces pieces, Vector3 from, Vector3 to, float width, Material finish)
        {
            var length = to - from;
            pieces.Add(box, finish, (from + to) / 2, new Vector3(width, width, length.magnitude + .047f),
                Quaternion.FromToRotation(Vector3.forward, length));
        }

        void AddFootprint(Vector3 point, float halfX, float halfZ) =>
            obstacles.Add(Rect.MinMaxRect(point.x - halfX, point.z - halfZ, point.x + halfX, point.z + halfZ));

        static Mesh TaperedTrunk() => RadialProfile("Tapered pine bark", 14,
            new[] { 0f, .10f, .48f, .83f, 1f }, new[] { 1.14f, .91f, .75f, .53f, .28f }, .065f);

        static Mesh PineBough() => RadialProfile("Layered evergreen bough", 24,
            new[] { .10f, 0f, .22f, .67f, 1f }, new[] { .04f, 1f, .82f, .48f, .012f }, .075f);

        static Mesh RadialProfile(string name, int sides, float[] heights, float[] radii, float variation)
        {
            var points = new List<Vector3>();
            var indices = new List<int>();
            for (int ring = 0; ring < heights.Length; ring++)
            for (int i = 0; i < sides; i++)
            {
                float angle = 2 * Mathf.PI * i / sides;
                float radius = radii[ring] * (1 + Mathf.Sin(i * 2.76f) * variation);
                points.Add(new Vector3(Mathf.Sin(angle) * radius, heights[ring], Mathf.Cos(angle) * radius));
                if (ring == heights.Length - 1) continue;
                int a = ring * sides + i;
                int b = ring * sides + (i + 1) % sides;
                indices.AddRange(new[] { a,b,b+sides, a,b+sides,a+sides });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(points);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
