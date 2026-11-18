using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkChristmasSetup
    {
        readonly Mesh box = ParkSurface.RoundedBox();
        readonly Mesh slab = PrimitiveMesh(PrimitiveType.Cube);
        readonly Mesh ball = PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh foliage = RoundProfile("Fir branches", 16, .11f, new Vector2(.04f, 0), new Vector2(1, .09f),
            new Vector2(.88f, .23f), new Vector2(.56f, .45f), new Vector2(.32f, .68f), new Vector2(.015f, 1));
        readonly Mesh snowTier = RoundProfile("Snow on fir branches", 16, .16f, new Vector2(.04f, .04f), new Vector2(.97f, .17f),
            new Vector2(.82f, .32f), new Vector2(.50f, .55f), new Vector2(.25f, .77f), new Vector2(.01f, 1));
        readonly Material snow = Paint("Lilac shaded snow", "#F3EDFC");
        readonly Material timber = Paint("Dark gingerbread timber", "#835033");
        readonly Material redLight = Paint("Scarlet shingles", "#B95356");
        readonly Material stone = Paint("Mauve foundation stone", "#9C8290");
        readonly Material green = Paint("Blue spruce", "#346950");
        readonly Material greenLight = Paint("Spruce tips", "#528061");
        readonly Material brass = Paint("Golden brass", "#F3BA51");
        readonly Material amber = Glow("Amber window glass", "#FFCF83");
        readonly Material[] bulbs={Bulb("Candlelight","#FFD585"),Bulb("Ruby","#F85B53"),Bulb("Champagne","#FFF1C9")};
        readonly Mesh bulb=RoundProfile("Round tree light",8,new Vector2(.01f,-.5f),new Vector2(.42f,-.25f),
            new Vector2(.50f,0),new Vector2(.42f,.25f),new Vector2(.01f,.5f));

        internal static ParkVenue Create(Transform root) => new ParkChristmasSetup().Build(root);

        ParkVenue Build(Transform root)
        {
            snow.SetColor("_Shade", new Color(.67f, .71f, .90f));
            snow.SetFloat("_Variation", .018f);
            snow.SetFloat("_GrassDetail", .035f);
            var ground = ParkGeometry.Place("Snow-covered village ground", slab, root, new Vector3(0, -.16f, 0),
                new Vector3(560, .32f, 560), snow);
            ground.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            BuildPaths(root);
            var cathedral = ParkChristmasCathedral.Create(root);

            var scenery = Group("Evergreens, lanterns and snowdrifts", root, Vector3.zero);
            var pieces = new Pieces();
            AddFir(pieces,new Vector3(-12.6f,0,-6f),7,true);
            AddFir(pieces,new Vector3(12.9f,0,-5.8f),6.8f,true);
            foreach(int side in new[] {-1,1})
            {
                Lantern(pieces,new Vector3(side*3.1f,0,-28.8f));
                AddFir(pieces,new Vector3(side*13.5f,0,-27.5f),side<0?4.3f:5.1f,false);
            }
            foreach (var p in new[] { new Vector3(-6.7f,0,-10.3f), new Vector3(6.7f,0,-10.3f),
                new Vector3(-10.4f,0,-3.2f), new Vector3(11.3f,0,-2.8f), new Vector3(-16.5f,0,2.8f), new Vector3(16.5f,0,3.5f) })
                Lantern(pieces, p);
            for (int i = 0; i < 12; i++)
            {
                float side = i < 6 ? -1 : 1;
                int row = i % 6;
                float x = 32.25f + i % 3 * .20f;
                float z=row<3?-27+row*10:7+(row-3)*10;
                var p = new Vector3(side * x, 0, z);
                AddFir(pieces, p, 3.8f + i % 4 * .68f, false);
                pieces.Add(ball, snow, p, new Vector3(4.1f, .44f, 2.45f));
            }
            for (int i = 0; i < 13; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var p = new Vector3(side * (12.85f + (i % 3) * .45f), -.04f, -9.9f + i * 1.5f);
                pieces.Add(ball, snow, p, new Vector3(2.1f + i % 4 * .38f, .4f + i % 3 * .10f, 2.4f));
            }
            pieces.Build(scenery);
            ParkChristmasTown.Create(root);
            BuildHills(root);
            AddSnowfall(root);
            ParkChristmasDeerSetup.AddDeer(root);

            return new ParkVenue
            {
                Name = "시계탑 광장", Root = root.gameObject, Houses = new[] { cathedral }, LawnHeight = .025f,
                WalkArea = Rect.MinMaxRect(-12, -12.4f, 12, 7.8f),
                WalkObstacles = new[] { Rect.MinMaxRect(-15.9f, 4.33f, 15.9f, ParkChristmasCathedral.FootprintBack+.4f),
                    Rect.MinMaxRect(-12.6f, -8.13f, -10.75f, -4.65f),Rect.MinMaxRect(10.95f,-7.9f,14.85f,-3.7f) },
                Focus = new Vector3(0, 8.95f, 2.75f), Distance = 40, Pitch = 16, Yaw = 13, MaxDistance = 62,
                FogEnd = 198, Sky = new Color32(116, 115, 181, 255)
            };
        }

        void AddFir(Pieces p, Vector3 point, float height, bool decorated)
        {
            p.Add(box, timber, point + Vector3.up * height * .13f, new Vector3(height*.09f,height*.28f,height*.09f));
            for (int layer = 0; layer < 5; layer++)
            {
                float y = height * (.12f + layer*.146f);
                float width = height * (.325f - layer*.054f) * (layer % 2 == 0 ? 1 : .95f);
                float length = height * (.33f - layer*.015f);
                var rotation = Quaternion.Euler(0, layer*27 + point.x*12.5f, 0);
                p.Add(foliage, layer % 2 == 0 ? green : greenLight, point + Vector3.up*y, new Vector3(width,length,width), rotation);
                p.Add(snowTier, snow, point + Vector3.up*(y+.10f), new Vector3(width*.90f,length*1.03f,width*.90f), rotation);
                if (!decorated) continue;
                for (int i = 0; i < 15; i++)
                {
                    float angle = i * Mathf.PI * 2 / 15 + layer*.51f;
                    var lightPosition=point+new Vector3(Mathf.Cos(angle)*width*1.015f,y+length*(.19f+.033f*Mathf.Sin(angle*2)),Mathf.Sin(angle)*width*1.015f);
                    p.Add(bulb,bulbs[(i+layer)%bulbs.Length],lightPosition,Vector3.one*(i%3==0?.21f:.17f));
                }
            }
            if (!decorated) return;
            var starCenter = point + Vector3.up * (height * .995f);
            var core = new List<Vector2>();
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.PI * (.5f + .4f * i);
                Vector2 a = StarPoint(angle - Mathf.PI/5,.29f);
                Vector2 b = StarPoint(angle,.63f);
                Vector2 c = StarPoint(angle + Mathf.PI/5,.29f);
                core.Add(a);
                var tip = ParkGeometry.Extrude("Christmas star tip", new List<Vector2> { a,b,c }, .19f);
                p.Add(tip, brass, starCenter, Vector3.one);
            }
            p.Add(ParkGeometry.Extrude("Christmas star center", core, .19f), brass, starCenter, Vector3.one);

            Vector2 StarPoint(float angle, float radius) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        void Lantern(Pieces p, Vector3 point)
        {
            p.Add(box, stone, point + Vector3.up*.13f, new Vector3(.52f,.26f,.52f));
            p.Add(box, timber, point + Vector3.up*1.10f, new Vector3(.15f,2.12f,.15f));
            p.Add(box, amber, point + Vector3.up*2.13f, new Vector3(.43f,.49f,.43f));
            p.Add(box, timber, point + Vector3.up*2.39f, new Vector3(.58f,.13f,.58f));
            p.Add(ball, snow, point + Vector3.up*2.47f, new Vector3(.60f,.20f,.60f));
            p.Add(box, redLight, point + new Vector3(0,1.56f,-.11f), new Vector3(.38f,.16f,.16f));
        }

        void BuildPaths(Transform root)
        {
            var path = Group("Curving sandstone paths", root, Vector3.zero);
            var joint = Paint("Sandstone joints", "#AC8991");
            var tones = new[] { Paint("Warm sandstone", "#D6B197"), Paint("Peach sandstone", "#E2BE9F"),
                Paint("Rose sandstone", "#CCAB9D"), Paint("Honey sandstone", "#DDB896") };
            var vertices = new Dictionary<Material, List<Vector3>>();
            const float jointDepth = .004f;
            // The main loop sits just above both connections, so only its paving is visible at a junction.
            AddRing(10.65f, 6.9f, -4.25f, 76, .022f);
            AddRing(7.65f, 4.40f, 2.8f, 54, .013f);
            const float approach = .004f;
            const int aisleRows = 25;
            for (int row = 0; row < aisleRows; row++)
            {
                AddAisleStrip(joint,-1.70f,1.70f,row,row+1,approach);
                for (int col = -1; col <= 1; col++)
                {
                    float x = col * 1.12f;
                    AddAisleStrip(tones[(row * 5 + col + 1) % tones.Length], x-.54f,x+.54f,
                        row+.025f,row+.975f,approach+jointDepth);
                }
            }

            foreach (var (material, corners) in vertices)
            {
                var triangles = new int[corners.Count / 4 * 6];
                for (int i = 0, k = 0; i < corners.Count; i += 4, k += 6)
                {
                    triangles[k] = i; triangles[k+1] = i+1; triangles[k+2] = i+2;
                    triangles[k+3] = i; triangles[k+4] = i+2; triangles[k+5] = i+3;
                }
                var mesh = new Mesh { name = material.name, vertices = corners.ToArray(), triangles = triangles };
                mesh.RecalculateNormals();
                ParkGeometry.Place(material.name, mesh, path, Vector3.zero, Vector3.one, material);
            }

            void AddRing(float xRadius, float zRadius, float zCenter, int segments, float height)
            {
                const float halfWidth = 1.21f, rowWidth = .78f;
                for (int i = 0; i < segments; i++)
                {
                    AddStrip(joint, i, i + 1, -halfWidth, halfWidth, height);
                    for (int row = -1; row <= 1; row++)
                    {
                        float start = i + (row == 0 ? .5f : 0);
                        float center = row * rowWidth;
                        AddStrip(tones[(i*5+row*3+3)%tones.Length], start + .022f, start + .978f,
                            center - .371f, center + .371f, height + jointDepth);
                    }
                }

                void AddStrip(Material material, float start, float end, float inner, float outer, float y) =>
                    AddQuad(material, Point(start, outer, y), Point(start, inner, y), Point(end, inner, y), Point(end, outer, y));

                Vector3 Point(float segment, float offset, float y)
                {
                    float angle = Mathf.Repeat(segment, segments) * Mathf.PI * 2 / segments;
                    float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                    var center = new Vector3(xRadius*cos, y, zCenter+zRadius*sin);
                    var normal = new Vector3(zRadius*cos, 0, xRadius*sin).normalized;
                    return center + normal * offset;
                }
            }

            void AddAisleStrip(Material material,float left,float right,float front,float back,float y) =>
                AddQuad(material,Aisle(left,front,y),Aisle(left,back,y),Aisle(right,back,y),Aisle(right,front,y));

            Vector3 Aisle(float x,float row,float y)
            {
                float t=row/aisleRows;
                float bend=-1.85f*Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*2*Mathf.PI);
                return new Vector3(x+bend,y,Mathf.Lerp(-15.325f,5.12f,t));
            }

            void AddQuad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                if (!vertices.TryGetValue(material, out var corners))
                    vertices.Add(material, corners = new List<Vector3>());
                corners.Add(a); corners.Add(b); corners.Add(c); corners.Add(d);
            }
        }

        void BuildHills(Transform root)
        {
            var hills = Group("Dusky hills behind the village", root, Vector3.zero);
            var p = new Pieces();
            var distant = Paint("Periwinkle mountain silhouettes", "#7775AE");
            var near = Paint("Icy lavender hills", "#A8A0CC");
            for (int i = 0; i < 9; i++)
            {
                float x = (i - 4) * 42f;
                p.Add(ball, distant, new Vector3(x,-3.1f,181+i%3*3), new Vector3(67,29+i%4*5.3f,27));
                p.Add(ball, near, new Vector3(x+9.2f,-2.5f,170+i%3*3), new Vector3(55,17+i%3*3.6f,18));
            }
            p.Build(hills);
        }

        static void AddSnowfall(Transform root)
        {
            var flakes = Group("Falling snow", root, new Vector3(0, 15, -3));
            flakes.localRotation = Quaternion.Euler(83, 12, 0);
            var particles = flakes.gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9, 12);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.15f, 1.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(.050f, .125f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 660;
            var emission = particles.emission;
            emission.rateOverTime = 54;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(39, 29, .70f);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = RoundProfile("Snowflake", 5, new Vector2(.015f,-.35f), new Vector2(.50f,0), new Vector2(.015f,.35f));
            renderer.sharedMaterial = Glow("Snowflakes", "#FFF6FF");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static Material Glow(string name, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return new Material(Shader.Find("Unlit/Color")) { name = name, color = color };
        }

        static Material Bulb(string name,string hex)
        {
            ColorUtility.TryParseHtmlString(hex,out var color);
            return new Material(Shader.Find("PeacePark/Festive Bulbs")){name=name+" tree bulbs",color=color};
        }

        static Mesh RoundProfile(string name, int sides, params Vector2[] profile)
            => RoundProfile(name, sides, 0, profile);

        static Mesh RoundProfile(string name, int sides, float scallop, params Vector2[] profile)
        {
            var vertices = new Vector3[sides*profile.Length];
            var triangles = new int[sides*(profile.Length-1)*6];
            int index = 0;
            for (int ring = 0; ring < profile.Length; ring++)
            for (int side = 0; side < sides; side++)
            {
                int a = ring*sides+side;
                float angle = side * Mathf.PI * 2 / sides;
                float wave = Mathf.Sin(angle*5+.4f)*.68f + Mathf.Sin(angle*3+1.3f)*.32f;
                float radius = profile[ring].x * (1 + scallop*wave);
                float y = profile[ring].y + wave*scallop*.56f*profile[ring].x;
                vertices[a] = new Vector3(Mathf.Cos(angle)*radius, y, Mathf.Sin(angle)*radius);
                if (ring == profile.Length-1) continue;
                int b = ring*sides+(side+1)%sides;
                triangles[index++] = a; triangles[index++] = a+sides; triangles[index++] = b;
                triangles[index++] = b; triangles[index++] = a+sides; triangles[index++] = b+sides;
            }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
