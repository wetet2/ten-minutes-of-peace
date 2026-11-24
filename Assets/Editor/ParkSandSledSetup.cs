using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkSandSledSetup
    {
        internal static ParkSandSled Create(Transform root, Material sand)
        {
            var course = Group("Desert sand sled course", root, Vector3.zero);
            var pickup = Group("Parked sand sled", course, Vector3.zero);
            var sled = Group("Wooden sand sled", pickup, Vector3.zero);
            var box = ParkSurface.RoundedBox();
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            var wood = SledPaint("Amber sled wood", "#BF7B37");
            var light = SledPaint("Honey sled slats", "#E6AA5F");
            var deep = SledPaint("Bent wooden runners", "#8B582C");
            var parts = new Pieces();
            for (int i = -3; i <= 3; i++)
                parts.Add(box, i % 2 == 0 ? light : wood, new Vector3(i * .225f, .23f, -.10f), new Vector3(.216f, .16f, 2.20f));
            foreach (float z in new[] { -.92f, .65f })
                parts.Add(box, deep, new Vector3(0, .135f, z), new Vector3(1.75f, .15f, .16f));
            parts.Add(box, wood, new Vector3(0, .51f, -.90f), new Vector3(1.55f, .39f, .14f), Quaternion.Euler(12, 0, 0));
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * ParkSandSled.RunnerHalfSpacing;
                var skid = new[] { new Vector2(-1.40f,.10f),new Vector2(-1.14f,.068f),new Vector2(.69f,.068f),
                    new Vector2(.98f,.12f),new Vector2(1.22f,.27f),new Vector2(1.38f,.48f),
                    new Vector2(1.38f,.63f),new Vector2(1.28f,.74f) };
                for (int i = 1; i < skid.Length; i++)
                    Beam(parts, box, deep, new Vector3(x,skid[i-1].y,skid[i-1].x), new Vector3(x,skid[i].y,skid[i].x), .136f);
                Beam(parts, box, wood, new Vector3(x,.39f,-.87f), new Vector3(x,.42f,.71f), .12f);
                foreach (float z in new[] { -.70f, .57f })
                    Beam(parts, box, wood, new Vector3(x,.23f,z), new Vector3(x,.43f,z), .13f);
            }
            Beam(parts, box, light, new Vector3(-.78f,.29f,1.22f), new Vector3(.78f,.29f,1.22f), .12f);
            parts.Build(sled);
            var rope = ParkGeometry.Place("Sled tow rope", cylinder, pickup, Vector3.zero, Vector3.one,
                SledPaint("Woven golden tow rope", "#BD8949"));
            var track = new Material(sand) { name = "Subtle sand sled trails", color = new Color32(231,181,111,255) };
            ParkGeometry.Place("Side path tow marks", TrackMesh(false, .43f), course, Vector3.zero, Vector3.one, track);
            ParkGeometry.Place("Downhill runner marks", TrackMesh(true, ParkSandSled.RunnerHalfSpacing), course, Vector3.zero, Vector3.one, track);

            var particles = Group("Sled sand spray", pickup, Vector3.zero).gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.28f, .60f);
            main.startSize = new ParticleSystem.MinMaxCurve(.08f, .19f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.24f, .58f);
            main.gravityModifier = .23f;
            main.maxParticles = 34;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .27f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = ParkGeometry.Canopy();
            renderer.sharedMaterial = Paint("Sled dust ochre", "#F5D196");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var ride = course.gameObject.AddComponent<ParkSandSled>();
            ride.Configure(pickup, sled, rope);
            return ride;
        }

        static Material SledPaint(string name, string color)
        {
            var material = Paint(name, color);
            material.EnableKeyword("PEACE_PARK_ANIMAL_FADING");
            return material;
        }

        static void Beam(Pieces pieces, Mesh box, Material material, Vector3 from, Vector3 to, float width)
        {
            var axis = to - from;
            pieces.Add(box,material,(from + to) / 2, new Vector3(width, width, axis.magnitude + width * .60f),
                Quaternion.FromToRotation(Vector3.forward, axis));
        }

        static Mesh TrackMesh(bool downhill, float offset)
        {
            const int segments = 192;
            var vertices = new Vector3[(segments + 1) * 4];
            var triangles = new int[segments * 12];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                var point = ParkSandSled.CoursePoint(downhill,t);
                var direction = ParkSandSled.CoursePoint(downhill,t + .002f) - ParkSandSled.CoursePoint(downhill,t - .002f);
                var side = Vector3.Cross(Vector3.up,direction).normalized;
                float half = (downhill ? .105f : .092f) * Mathf.SmoothStep(.14f,1f,Mathf.Min(t,1-t) * 17);
                for (int j = 0; j < 4; j++)
                {
                    var mark = point + side * ((j < 2 ? -offset : offset) + (j % 2 == 0 ? -half : half));
                    mark.y = ParkDuneTerrain.Height(mark.x,mark.z) + .003f;
                    vertices[i * 4 + j] = mark;
                }
                if (i == segments) continue;
                for (int track = 0; track < 2; track++)
                {
                    int a = i * 4 + track * 2, k = i * 12 + track * 6;
                    triangles[k] = a; triangles[k + 1] = a + 4; triangles[k + 2] = a + 5;
                    triangles[k + 3] = a; triangles[k + 4] = a + 5; triangles[k + 5] = a + 1;
                }
            }
            var mesh = new Mesh { name = "Sand runner trails", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
