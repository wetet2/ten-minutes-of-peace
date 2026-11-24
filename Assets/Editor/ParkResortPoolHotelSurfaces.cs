using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkResortPoolSetup
    {
        const float GuestDoorTop = 2.64f;
        readonly Mesh hotelPane = MeshOf("Fitted hotel glazing pane",
            new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) },
            new[] { 0,2,1,0,3,2 }, new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
        readonly Material hotelHip = Paint("Weathered terracotta ridge caps", "#AC705A");

        void HotelWindow(Pieces parts, Rect opening, Material glazing, bool doors, bool tall, float doorTop)
        {
            var panel = opening;
            if (doors) panel.yMin -= .12f;
            const float casing = .115f;
            var stone = Rect.MinMaxRect(panel.xMin - casing, panel.yMin - casing, panel.xMax + casing, panel.yMax + casing);
            var reveal = FrameRectangle(parts, stone, casing + .055f, -.023f, .135f, hotelTrim);
            var glass = FrameRectangle(parts, reveal, .058f, .154f, .107f, hotelBronze);

            if (doors)
            {
                glass.yMin = doorTop + .019f;
                parts.Add(cube, hotelBronze, new Vector3(glass.center.x, doorTop, .157f), new Vector3(reveal.width, .064f, .113f));
            }
            AddGlazing(parts, glass, .214f, glazing);
            if (!doors)
            {
                float transom = glass.yMin + glass.height * (tall ? .47f : .73f);
                parts.Add(cube, hotelBronze, new Vector3(glass.center.x, transom, .169f), new Vector3(glass.width, .053f, .078f));
                int panes = tall ? 4 : 2;
                for (int i = 1; i < panes; i++)
                    parts.Add(cube, hotelBronze, new Vector3(glass.xMin + glass.width * i / panes, glass.center.y, .172f),
                        new Vector3(tall ? .062f : .048f, glass.height, .082f));
                if (tall)
                    parts.Add(cube, hotelBronze, new Vector3(glass.center.x, Mathf.Lerp(transom, glass.yMax, .55f), .169f),
                        new Vector3(glass.width, .052f, .076f));
            }
            if (!doors && !tall)
                parts.Add(cube, hotelTrim, new Vector3(opening.center.x, opening.yMin - .088f, -.094f),
                    new Vector3(stone.width + .12f, .12f, .37f));
        }

        Rect FrameRectangle(Pieces parts, Rect box, float border, float z, float depth, Material material)
        {
            var inside = Rect.MinMaxRect(box.xMin + border, box.yMin + border, box.xMax - border, box.yMax - border);
            foreach (int side in new[] { -1, 1 })
            {
                parts.Add(cube, material, new Vector3(box.center.x + side * (box.width - border) * .5f, inside.center.y, z),
                    new Vector3(border, inside.height, depth));
                parts.Add(cube, material, new Vector3(box.center.x, box.center.y + side * (box.height - border) * .5f, z),
                    new Vector3(box.width, border, depth));
            }
            return inside;
        }

        void AddGlazing(Pieces parts, Rect bounds, float z, Material glass)
        {
            const float lap = .025f;
            parts.Add(hotelPane, glass, new Vector3(bounds.center.x, bounds.center.y, z), new Vector3(bounds.width + lap, bounds.height + lap, 1));
        }

        void HotelBalcony(Pieces parts, Rect opening)
        {
            float width = opening.width + .46f, y = opening.yMin, x = opening.center.x;
            const float front = -.94f;
            parts.Add(cube, hotelShadow, new Vector3(x, y - .172f, -.48f), new Vector3(width + .10f, .155f, 1.13f));
            parts.Add(cube, hotelTrim, new Vector3(x, y - .078f, -.49f), new Vector3(width + .18f, .108f, 1.17f));
            parts.Add(cube, hotelBronze, new Vector3(x, y + .785f, front), new Vector3(width, .066f, .075f));
            parts.Add(cube, hotelBronze, new Vector3(x, y + .113f, front), new Vector3(width, .044f, .045f));
            int posts = Mathf.RoundToInt(width / .232f);
            for (int i = 0; i <= posts; i++)
                parts.Add(cube, hotelBronze, new Vector3(x + width * (i / (float)posts - .5f), y + .385f, front),
                    new Vector3(i == 0 || i == posts ? .059f : .029f, .79f, .041f));
            foreach (int side in new[] { -1, 1 })
            {
                float edge = x + side * width * .5f;
                parts.Add(cube, hotelBronze, new Vector3(edge, y + .785f, front * .5f), new Vector3(.060f, .066f, -front + .068f));
                parts.Add(cube, hotelBronze, new Vector3(edge, y + .113f, front * .5f), new Vector3(.038f, .041f, -front));
                for (int post = 1; post <= 3; post++)
                    parts.Add(cube, hotelBronze, new Vector3(edge, y + .397f, front * post / 4f), new Vector3(.034f, .76f, .036f));
            }
        }

        void BuildHotelRoof(Transform parent, Vector3 eave, float width, float depth, float rise)
        {
            var roofGroup = Group("Hipped terracotta hotel roof", parent, eave);
            float x = width * .5f, z = depth * .5f, ridgeX = x - Mathf.Min(z * .73f, x * .60f);
            var corners = new[] { new Vector3(-x,0,-z), new Vector3(x,0,-z), new Vector3(x,0,z), new Vector3(-x,0,z) };
            var ridgeLeft = new Vector3(-ridgeX, rise, 0);
            var ridgeRight = new Vector3(ridgeX, rise, 0);
            var points = new List<Vector3>(14);
            var uv = new List<Vector2>(14);
            var triangles = new List<int>(18);
            AddSlope(false, corners[0], ridgeLeft, ridgeRight, corners[1]);
            AddSlope(true, corners[1], ridgeRight, corners[2]);
            AddSlope(false, corners[2], ridgeRight, ridgeLeft, corners[3]);
            AddSlope(true, corners[3], ridgeLeft, corners[0]);
            MakeMesh("Continuous terracotta roof slopes", MeshOf("UV-aligned hip roof", points.ToArray(), triangles.ToArray(), uv.ToArray()),
                hotelRoofTile, roofGroup);
            var trims = new Pieces();
            trims.Add(cube, hotelTrim, Vector3.down * .085f, new Vector3(width - .065f, .125f, depth - .065f));
            trims.Beam(cylinder, hotelHip, ridgeLeft, ridgeRight, .206f);
            for (int i = 0; i < corners.Length; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % corners.Length];
                var axis = b - a;
                var rotation = Quaternion.LookRotation(axis);
                trims.Add(cube, hotelTrim, (a + b) * .5f - Vector3.up * .079f, new Vector3(.23f, .16f, axis.magnitude + .19f), rotation);
                trims.Add(cube, hotelShadow, (a + b) * .5f + Vector3.up * .012f, new Vector3(.15f, .085f, axis.magnitude + .13f), rotation);
                trims.Beam(cylinder, hotelHip, a + Vector3.up * .034f, i == 0 || i == 3 ? ridgeLeft : ridgeRight, .135f);
            }
            trims.Build(roofGroup);

            void AddSlope(bool end, params Vector3[] face)
            {
                int first = points.Count;
                float run = end ? x - ridgeX : z;
                float slope = Mathf.Sqrt(run * run + rise * rise);
                foreach (var point in face)
                {
                    points.Add(point);
                    uv.Add(new Vector2((end ? point.z : point.x) / 1.92f, point.y / rise * slope / 2.76f));
                }
                for (int i = 1; i < face.Length - 1; i++)
                {
                    triangles.Add(first);
                    triangles.Add(first + i);
                    triangles.Add(first + i + 1);
                }
            }
        }

        void CreateHotelSurfaceMaterials()
        {
            hotelGlass = Paint("Cool blue-green guestroom glazing", "#FFFFFF");
            lobbyGlass = Paint("Warm lobby glazing", "#FFFFFF");
            hotelRoofTile = Paint("Matte kiln-fired hotel roof tiles", "#FFFFFF");
            lobbyEntranceGlass = new Material(Shader.Find("Unlit/Transparent")) {
                name = "Clear sea-tinted lobby glass", mainTexture = LobbyGlassReflection()
            };
            ApplyTexture(hotelGlass, HotelGlassTexture(false));
            ApplyTexture(lobbyGlass, HotelGlassTexture(true));
            ApplyTexture(hotelRoofTile, HotelTileTexture());

            static void ApplyTexture(Material material, Texture2D texture)
            {
                material.mainTexture = texture;
                material.EnableKeyword("PEACE_PARK_PAINT_MAP");
            }
        }

        static Texture2D LobbyGlassReflection()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) {
                name = "Sheer sky reflection on lobby glazing", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear
            };
            var colors = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (size - 1f), v = y / (size - 1f);
                float band = Mathf.Clamp01(1 - Mathf.Abs(v - .55f - u * .23f) * 10);
                colors[y * size + x] = new Color(.52f, .68f, .69f, .125f + .21f * v + .11f * band * band);
            }
            texture.SetPixels32(colors);
            texture.Apply(true);
            return texture;
        }

        static Texture2D HotelGlassTexture(bool lobby)
        {
            const int width = 64, height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, true) {
                name = lobby ? "Warm reflected lobby glass" : "Sea-glass and softly reflected sky",
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear
            };
            var colors = new Color32[width * height];
            var low = new Color(.145f, .240f, .252f);
            var high = new Color(.430f, .600f, .630f);
            var reflection = new Color(.73f, .79f, .78f);
            var curtain = new Color(.71f, .672f, .565f);
            var lobbyLight = new Color(.65f, .485f, .301f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f), v = y / (height - 1f);
                float band = Mathf.Clamp01(1 - Mathf.Abs(v - .68f - u * .11f) * 8.4f);
                float margin = Mathf.Min(u, 1 - u);
                float fold = .76f + .24f * Mathf.Cos(u * Mathf.PI * 36);
                Color color = Color.Lerp(low, high, Mathf.SmoothStep(0, 1, v));
                color = Color.Lerp(color, reflection, band * band * .13f);
                if (lobby)
                {
                    float warmth = (1 - Mathf.SmoothStep(0, 1, v)) * (.65f + .35f * Mathf.Sin(u * Mathf.PI));
                    color = Color.Lerp(color, lobbyLight, warmth * .69f);
                }
                else
                {
                    float drape = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.05f, .19f, margin));
                    color = Color.Lerp(color, curtain, drape * fold * .45f);
                }
                colors[y * width + x] = color;
            }
            texture.SetPixels32(colors);
            texture.Apply(true);
            return texture;
        }

        static Texture2D HotelTileTexture()
        {
            const int size = 192;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true) {
                name = "Staggered terracotta roof tiles", wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
            var colors = new Color32[size * size];
            var clay = new Color(.692f, .390f, .288f);
            var joint = new Color(.45f, .24f, .184f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float row = y * 6f / size, tile = x * 4f / size + .5f * (Mathf.FloorToInt(row) % 2);
                float along = row - Mathf.Floor(row), across = tile - Mathf.Floor(tile);
                int column = Mathf.FloorToInt(tile) % 4, course = Mathf.FloorToInt(row);
                float tone = .96f + ((column * 17 + course * 7) % 11) * .008f;
                float roll = .97f + .08f * Mathf.Sin(across * Mathf.PI);
                float seam = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.012f, .057f, Mathf.Min(across, 1 - across)));
                seam *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.015f, .095f, Mathf.Min(along * 1.2f, 1 - along)));
                colors[y * size + x] = Color.Lerp(joint, clay * (tone * roll), .35f + .65f * seam);
            }
            texture.SetPixels32(colors);
            texture.Apply(true);
            return texture;
        }
    }
}
