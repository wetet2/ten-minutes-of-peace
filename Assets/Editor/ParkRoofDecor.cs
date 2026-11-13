using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkRoofDecor
    {
        internal static void Apply(Scene scene)
        {
            var park = Array.Find(scene.GetRootGameObjects(), root => root.name == "Peace Park");
            if (!park) return;
            var flower = park.transform.Find("Flower cottage");
            var candy = park.transform.Find("Candy cottage");
            if (!flower || !candy || flower.Find("Sunflower roof ornament")) return;

            var sphere = PrimitiveMesh(PrimitiveType.Sphere);
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            var cream = Paint("Vanilla candy", "#FFF1C5");
            AddSunflower(flower, sphere, cylinder);
            AddCandyRoof(candy, sphere, cylinder, cream);
        }

        static void AddSunflower(Transform house, Mesh sphere, Mesh cylinder)
        {
            var ornament = Group("Sunflower roof ornament", house, Vector3.zero);
            var stem = Paint("Sunflower stem", "#59A250");
            var gold = Paint("Sunflower petals", "#FFD23D");
            var amber = Paint("Sunflower inner petals", "#F4A63D");
            var brown = Paint("Sunflower center", "#915432");
            var seeds = Paint("Sunflower seeds", "#D49456");
            var head = new Vector3(.6f, 6.28f, -.72f);

            var baseParts = new Pieces();
            var basePoint = new Vector3(.50f, 4.80f, -.48f);
            baseParts.Add(sphere, stem, basePoint + Vector3.up * .12f, new Vector3(.69f, .31f, .60f));
            baseParts.Beam(cylinder, stem, basePoint, head + Vector3.forward * .19f, .19f);
            baseParts.Add(sphere, stem, new Vector3(.22f, 5.35f, -.60f), new Vector3(.34f, .76f, .16f),
                Quaternion.Euler(14, 0, 52));
            baseParts.Add(sphere, stem, new Vector3(.95f, 5.43f, -.61f), new Vector3(.33f, .70f, .16f),
                Quaternion.Euler(10, 0, -54));
            baseParts.Build(ornament);

            var tilt = Group("Sunflower mount", ornament, head);
            tilt.localRotation = Quaternion.Euler(23, -8, 0);
            var rotor = Group("Rotating sunflower", tilt, Vector3.zero);
            rotor.gameObject.AddComponent<RoofSunflowerSpin>();
            var petals = new Pieces();
            for (int i = 0; i < 13; i++)
            {
                float angle = i * 360f / 13;
                var direction = Quaternion.Euler(0, 0, angle) * Vector3.up;
                petals.Add(sphere, gold, direction * .96f, new Vector3(.52f, .96f, .27f),
                    Quaternion.Euler(0, 0, angle));
                float innerAngle = angle + 180f / 13;
                var inner = Quaternion.Euler(0, 0, innerAngle) * Vector3.up;
                petals.Add(sphere, amber, inner * .72f - Vector3.forward * .055f, new Vector3(.37f, .58f, .22f),
                    Quaternion.Euler(0, 0, innerAngle));
            }
            petals.Add(sphere, stem, Vector3.forward * .12f, new Vector3(1.21f, 1.21f, .28f));
            petals.Add(sphere, brown, -Vector3.forward * .055f, new Vector3(1.34f, 1.34f, .44f));
            for (int i = 0; i < 32; i++)
            {
                float radius = .56f * Mathf.Sqrt((i + .5f) / 32);
                float angle = i * 2.399963f;
                float depth = -.07f - .22f * Mathf.Sqrt(1 - radius * radius / (.67f * .67f));
                var point = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, depth);
                petals.Add(sphere, seeds, point, new Vector3(.107f, .107f, .067f));
            }
            petals.Build(rotor);
        }

        static void AddCandyRoof(Transform house, Mesh sphere, Mesh cylinder, Material cream)
        {
            var roof = Group("Candy roof ornaments", house, Vector3.zero);
            var rose = Paint("Strawberry candy", "#F96FA4");
            var blue = Paint("Blueberry candy", "#63CCEF");
            var lavender = Paint("Grape candy", "#B792EF");
            var yellow = Paint("Lemon candy", "#FFD863");
            Lollipop(roof, sphere, cylinder, cream, rose, new Vector3(.28f, 6.06f, -.42f), 4.84f, .83f, -12f);
            Lollipop(roof, sphere, cylinder, cream, blue, new Vector3(1.78f, 5.37f, -.60f), 4.26f, .65f, 16f);
            WrappedCandy(roof, sphere, lavender, cream, new Vector3(-1.19f, 4.77f, -1.73f), 26f, 1.0f);
            WrappedCandy(roof, sphere, yellow, cream, new Vector3(-2.04f, 4.25f, -.63f), 34f, .92f);
            WrappedCandy(roof, sphere, rose, cream, new Vector3(2.37f, 4.15f, -1.67f), -30f, .90f);
        }

        static void Lollipop(Transform roof, Mesh sphere, Mesh cylinder, Material cream, Material flavor,
            Vector3 center, float roofHeight, float radius, float lean)
        {
            var mount = Group(flavor.name + " lollipop", roof, Vector3.zero);
            var disc = Group("Swirl candy", mount, center);
            disc.localRotation = Quaternion.Euler(25, lean, -lean * .5f);
            var support = new Pieces();
            var root = new Vector3(center.x - lean * .016f, roofHeight, center.z + .04f);
            support.Add(sphere, cream, root + Vector3.up * .08f, new Vector3(.39f, .21f, .39f));
            support.Beam(cylinder, cream, root, center, .13f);
            support.Build(mount);

            const float halfThickness = .17f;
            var parts = new Pieces();
            parts.Add(sphere, flavor, Vector3.zero, new Vector3(radius * 2, radius * 2, halfThickness * 2));
            var swirls = Swirl(radius, halfThickness);
            parts.Add(swirls, cream, Vector3.zero, Vector3.one);
            parts.Build(disc);
            Object.DestroyImmediate(swirls);
        }

        static Mesh Swirl(float radius, float halfThickness)
        {
            const int steps = 84;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();
            foreach (int side in new[] { -1, 1 })
            {
                int start = vertices.Count;
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    float angle = t * Mathf.PI * 4.8f;
                    float middle = radius * (.046f + .855f * t);
                    float width = radius * .068f * Mathf.Min(1, Mathf.Min(t, 1 - t) * 19);
                    for (int edge = -1; edge <= 1; edge += 2)
                    {
                        float r = middle + width * edge;
                        float z = side * (halfThickness * Mathf.Sqrt(1 - r * r / (radius * radius)) + .013f);
                        vertices.Add(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, z));
                        normals.Add(Vector3.forward * side);
                    }
                    if (i == steps) continue;
                    int a = start + i * 2;
                    indices.AddRange(side > 0 ? new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }
                        : new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }
            var mesh = new Mesh { name = "Double-sided lollipop swirl" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }

        static void WrappedCandy(Transform roof, Mesh sphere, Material flavor, Material cream,
            Vector3 point, float tilt, float size)
        {
            var candy = Group(flavor.name + " wrapped candy", roof, point);
            candy.localRotation = Quaternion.Euler(25, 0, tilt);
            candy.localScale = Vector3.one * size;
            var parts = new Pieces();
            parts.Add(sphere, flavor, Vector3.zero, new Vector3(.73f, .48f, .46f));
            parts.Add(sphere, cream, -Vector3.forward * .047f, new Vector3(.16f, .485f, .39f),
                Quaternion.Euler(0, 0, -24));
            foreach (int side in new[] { -1, 1 })
            {
                parts.Add(sphere, cream, new Vector3(side * .33f, 0, 0), Vector3.one * .21f);
                for (int fan = -1; fan <= 1; fan++)
                    parts.Add(sphere, flavor, new Vector3(side * .45f, fan * .075f, 0),
                        new Vector3(.33f, .115f, .105f), Quaternion.Euler(0, 0, side * fan * 34));
            }
            parts.Build(candy);
        }

    }
}
