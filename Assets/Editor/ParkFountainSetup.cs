using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkFountainSetup
    {
        internal static ParkFountain Create(Transform park)
        {
            var root = Group("Duck fountain", park, Vector3.zero);
            var moving = Group("Moving water and ducks", root, Vector3.zero);
            var sphere = PrimitiveMesh(PrimitiveType.Sphere);
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            var stone = Paint("Mint glazed stone", "#86C4B8");
            var ivory = Paint("Ivory fountain rim", "#FFF1D8");
            var water = Paint("Lagoon blue", "#4DCBDB");
            var shallows = Paint("Shallow blue", "#7DE0DE");
            var foam = Paint("Water glints", "#D5FCF5");

            // Profile pairs store radius and height from the outer foot to the inside wall.
            var basin = Lathe("Scalloped bowl", new[] {
                new Vector2(2.36f,-.07f), new Vector2(2.55f,-.025f), new Vector2(2.61f,.06f),
                new Vector2(2.58f,.14f), new Vector2(2.48f,.20f), new Vector2(2.53f,.36f),
                new Vector2(2.61f,.48f), new Vector2(2.49f,.56f), new Vector2(2.38f,.43f),
                new Vector2(2.27f,.20f) }, .042f);
            ParkGeometry.Place("Glazed basin", basin, root, Vector3.zero, Vector3.one, stone);
            Rim(root, ivory, "Rounded ivory coping", 2.49f, .57f, .135f, .042f);
            Rim(root, ivory, "Ivory foot trim", 2.53f, .115f, .04f, .042f);

            float level = ParkFountain.WaterHeight;
            var surface = ParkGeometry.Place("Water", cylinder, root, new Vector3(0, level - .027f, 0),
                new Vector3(4.92f, .027f, 4.92f), water);
            surface.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Rim(root, shallows, "Shallow water around the edge", 2.27f, level + .004f, .055f);
            Rim(root, shallows, "Outer fountain ripple", .98f, level + .004f, .024f);
            Rim(root, foam, "Inner fountain ripple", .71f, level + .005f, .022f);

            var centerpiece = new Pieces();
            centerpiece.Add(cylinder, stone, new Vector3(0, .66f, 0), new Vector3(.44f, .27f, .44f));
            centerpiece.Add(sphere, ivory, new Vector3(0, .95f, 0), new Vector3(.68f, .27f, .68f));
            centerpiece.Add(sphere, water, new Vector3(0, 1.06f, 0), new Vector3(.48f, .19f, .48f));
            centerpiece.Add(sphere, foam, new Vector3(0, 1.18f, 0), new Vector3(.16f, .29f, .16f));
            centerpiece.Build(root);

            var droplets = new Transform[11];
            for (int i = 0; i < droplets.Length; i++)
                droplets[i] = ParkGeometry.Place("Fountain spray", sphere, moving, Vector3.zero, Vector3.one, foam);

            var duckYellow = Paint("Mother duck butter yellow", "#FFD04B");
            var ducklingYellow = Paint("Duckling lemon yellow", "#FFDE6D");
            var wing = Paint("Duck wing gold", "#F4AD38");
            var beak = Paint("Tangerine beaks", "#F38B3F");
            var mouth = Paint("Tangerine beak seam", "#CC662F");
            var eye = Paint("Duck eyes", "#392D27");
            var blush = Paint("Duck cheeks", "#FFAA69");
            var swimmers = new Transform[3];
            var ducks = new Transform[swimmers.Length];
            for (int i = 0; i < swimmers.Length; i++)
            {
                bool adult = i == 0;
                swimmers[i] = Group(adult ? "Mother duck path" : "Duckling path " + i, moving, Vector3.zero);
                ducks[i] = Group(adult ? "Mother duck" : "Duckling " + i, swimmers[i], Vector3.zero);
                float scale = adult ? .5f : .305f;
                ducks[i].localScale = Vector3.one * scale;
                Duck(ducks[i], sphere, adult ? duckYellow : ducklingYellow, wing, beak, mouth, eye, ivory, blush);
                Wake(swimmers[i], shallows, scale);
            }

            foreach (var renderer in moving.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            var fountain = root.gameObject.AddComponent<ParkFountain>();
            fountain.Configure(moving, swimmers, ducks, droplets);
            return fountain;
        }

        static void Duck(Transform duck, Mesh sphere, Material yellow, Material wing, Material beak, Material mouth,
            Material eye, Material glint, Material blush)
        {
            var parts = new Pieces();
            parts.Add(sphere, yellow, new Vector3(0,.15f,-.12f), new Vector3(.70f,.46f,1.03f));
            parts.Add(sphere, yellow, new Vector3(0,.21f,-.49f), new Vector3(.35f,.28f,.43f), Quaternion.Euler(32,0,0));
            parts.Add(sphere, yellow, new Vector3(0,.38f,.19f), new Vector3(.42f,.50f,.40f));
            parts.Add(sphere, yellow, new Vector3(0,.56f,.28f), new Vector3(.58f,.57f,.57f));
            parts.Add(sphere, yellow, new Vector3(-.058f,.815f,.21f), new Vector3(.13f,.16f,.22f), Quaternion.Euler(-27,0,20));
            parts.Add(sphere, beak, new Vector3(0,.445f,.537f), new Vector3(.33f,.141f,.36f));
            parts.Add(sphere, mouth, new Vector3(0,.423f,.569f), new Vector3(.305f,.026f,.297f));
            parts.Add(sphere, beak, new Vector3(0,.405f,.537f), new Vector3(.289f,.075f,.32f));
            foreach (int side in new[] { -1, 1 })
            {
                parts.Add(sphere, wing, new Vector3(side*.285f,.215f,-.21f), new Vector3(.19f,.235f,.65f),
                    Quaternion.Euler(-13,side*-11,side*21));
                parts.Add(sphere, eye, new Vector3(side*.229f,.599f,.432f), new Vector3(.087f,.11f,.083f));
                parts.Add(sphere, glint, new Vector3(side*.252f,.624f,.456f), Vector3.one*.034f);
                parts.Add(sphere, blush, new Vector3(side*.258f,.493f,.389f), new Vector3(.065f,.082f,.103f));
            }
            parts.Build(duck);
        }

        static void Wake(Transform swimmer, Material material, float scale)
        {
            var wake = Lathe("Duck ripple", new[] {
                new Vector2(.42f,.014f), new Vector2(.445f,.019f), new Vector2(.47f,.014f) },
                arcStart: 2.73f, arcLength: 2.55f);
            ParkGeometry.Place("Duck wake", wake, swimmer, new Vector3(0,0,-.12f * scale),
                new Vector3(scale,1,scale*1.5f), material);
        }

        static void Rim(Transform parent, Material material, string name, float radius, float height, float thickness,
            float scallop = 0)
        {
            var profile = new Vector2[13];
            for (int i = 0; i < profile.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / (profile.Length - 1);
                profile[i] = new Vector2(radius + Mathf.Cos(angle) * thickness, height + Mathf.Sin(angle) * thickness);
            }
            var part = ParkGeometry.Place(name, Lathe(name, profile, scallop), parent, Vector3.zero, Vector3.one, material);
            if (radius < 2.4f) part.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static Mesh Lathe(string name, Vector2[] profile, float scallop = 0, float arcStart = 0,
            float arcLength = Mathf.PI * 2)
        {
            const int sides = 96;
            bool closed = arcLength >= Mathf.PI * 2;
            int columns = closed ? sides : sides + 1;
            var points = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < columns; i++)
            {
                float angle = arcStart + i * arcLength / sides;
                float offset = Mathf.Cos(8 * angle) * scallop;
                foreach (var point in profile)
                    points.Add(new Vector3(Mathf.Cos(angle) * (point.x + offset), point.y, Mathf.Sin(angle) * (point.x + offset)));

                if (i == sides) continue;
                int current = i * profile.Length, next = (i + 1) % columns * profile.Length;
                for (int j = 0; j < profile.Length - 1; j++)
                    triangles.AddRange(new[] { current+j, current+j+1, next+j, next+j, current+j+1, next+j+1 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(points);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
