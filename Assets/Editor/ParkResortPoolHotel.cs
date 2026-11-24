using UnityEngine;
using static PeacePark.Editor.ParkScenery;
using static PeacePark.ParkResortRiver;

namespace PeacePark.Editor
{
    internal sealed partial class ParkResortPoolSetup
    {
        readonly Material roof = Paint("Terracotta roof", "#CB6F4C");
        readonly Material sunTile = Paint("Sunlit terracotta tiles", "#E58961");
        readonly Material clayTile = Paint("Warm terracotta tiles", "#D57952");
        readonly Material hotelStone = Paint("Hotel warm ivory plaster", "#E7DAC3");
        readonly Material hotelTrim = Paint("Hotel sanded limestone trim", "#FAF1DE");
        readonly Material hotelShadow = Paint("Hotel honed stone bands", "#BBAF9A");
        readonly Material hotelBronze = Paint("Hotel dark champagne window metal", "#555247");
        readonly Material hotelBrass = Paint("Hotel brushed brass accents", "#BB935D");
        Material hotelGlass, lobbyGlass, lobbyEntranceGlass, hotelRoofTile;

        ParkHouse MakeHotel(Transform root)
        {
            hotelStone.SetFloat("_Variation", .018f);
            CreateHotelSurfaceMaterials();
            var hotel = Group("Limestone resort hotel", root, Vector3.zero);
            var paving = Tiles("Hotel travertine forecourt", "#E0CDB0", "#C7B79D", 1.08f, .72f, true);
            MakeMesh("Continuous hotel terrace", Fill(Rectangle(-32.75f, 20f, 32.75f, 34.1f), DeckHeight), paving, hotel);
            MakeHotelWing(hotel, -1);
            var lobby = BuildHotelLobby(hotel);
            MakeHotelWing(hotel, 1);
            ScaleAboveDeck(hotel, .86f);
            return lobby;
        }

        void MakeHotelWing(Transform hotel, int side)
        {
            const float width = 22.74f, depth = 10.7f, eave = 10.73f, doorWidth = 2.68f;
            var wing = Group(side < 0 ? "West guest wing" : "East guest wing", hotel, new Vector3(side * 21.15f, 0, 21.30f));
            var front = HotelElevation(wing, "Pool-facing elevation", Vector3.zero, 0);
            var rear = HotelElevation(wing, "Garden-facing elevation", new Vector3(0, 0, depth), 180);
            var outward = HotelElevation(wing, "End elevation", new Vector3(side * width * .5f, 0, depth * .5f), -side * 90);
            BuildGuestElevation(front, width, 5, eave, openingWidth: doorWidth, entrance: true, balconies: true);
            BuildGuestElevation(rear, width, 5, eave, openingWidth: doorWidth, entrance: false, balconies: true);
            BuildGuestElevation(outward, depth, 3, eave, openingWidth: 2.04f, entrance: false, balconies: false);
            BuildHotelRoof(wing, new Vector3(0, eave, depth * .5f), width + .96f, depth + .90f, 1.76f);
            FurnishGuestTerrace(front, width);
            BuildGuestDoors(front, doorWidth);
        }

        void BuildGuestElevation(Transform elevation, float width, int bays, float eave, float openingWidth, bool entrance, bool balconies)
        {
            var parts = new Pieces();
            HotelRoomRow(parts, width, bays, DeckHeight, 3.72f, .55f, 2.61f, openingWidth, false, entrance, lobbyGlass);
            HotelRoomRow(parts, width, bays, 3.72f, 6.94f, 4.12f, 2.37f, openingWidth - .12f, balconies, false, hotelGlass);
            HotelRoomRow(parts, width, bays, 6.94f, eave, 7.34f, 2.46f, openingWidth - .12f, balconies, false, hotelGlass);
            DetailElevation(parts, width, bays, eave, 3.72f, 6.94f);
            parts.Build(elevation);
        }

        ParkHouse BuildHotelLobby(Transform hotel)
        {
            const float width = 19.92f, depth = 12.32f, eave = 13.05f;
            const float lobbyDoorWidth = 4.67f, lobbyDoorTop = 3.36f;
            var pavilion = Group("Central hotel pavilion", hotel, new Vector3(0, 0, 19.67f));
            var front = HotelElevation(pavilion, "Grand lobby elevation", Vector3.zero, 0);
            var rear = HotelElevation(pavilion, "Upper garden elevation", new Vector3(0, 0, depth), 180);
            foreach (var elevation in new[] { front, rear })
            {
                var parts = new Pieces();
                bool isFront = elevation == front;
                HotelRoomRow(parts, width, 3, DeckHeight, 6.13f, .36f, 5.19f, lobbyDoorWidth, false, isFront,
                    isFront ? lobbyEntranceGlass : lobbyGlass, tall: true, doorTop: lobbyDoorTop);
                HotelRoomRow(parts, width, 3, 6.13f, 9.30f, 6.57f, 2.30f, 3.33f, true, false, hotelGlass);
                HotelRoomRow(parts, width, 3, 9.30f, eave, 9.74f, 2.39f, 3.33f, true, false, hotelGlass);
                DetailElevation(parts, width, 3, eave, 6.13f, 9.30f);
                parts.Build(elevation);
            }
            var returns = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                returns.Add(cube, hotelStone, new Vector3(side * (width * .5f - .19f), (eave + DeckHeight) * .5f, depth * .5f),
                    new Vector3(.38f, eave - DeckHeight, depth));
                foreach (float y in new[] { 6.13f, 9.30f, eave - .22f })
                    returns.Add(cube, hotelTrim, new Vector3(side * width * .5f, y, depth * .5f), new Vector3(.18f, .19f, depth + .16f));
            }
            returns.Build(Group("Limestone pavilion returns", pavilion, Vector3.zero));
            BuildLobbyInterior(front);
            BuildLobbyPortico(front);
            BuildHotelRoof(pavilion, new Vector3(0, eave, depth * .5f), width + 1.04f, depth + .96f, 2.05f);
            var arrival = Group("Central lobby arrival doors", front, Vector3.forward * .152f);
            var house = arrival.gameObject.AddComponent<ParkHouse>();
            house.DisplayName = "호텔 중앙 로비";
            house.DoorDuration = .54f;
            house.ExitSeconds = 2.5f;
            house.Inside = Group("Arrival just inside", arrival, new Vector3(0, DeckHeight, 1.65f));
            house.Threshold = Group("Lobby threshold", arrival, new Vector3(0, DeckHeight, -.12f));
            house.Welcome = Group("Poolside greeting spot", arrival, new Vector3(0, DeckHeight, -4.12f));
            (house.DoorHinge, house.SecondDoorHinge) = BuildDoubleDoors(arrival, lobbyDoorWidth, lobbyDoorTop, lobbyEntranceGlass);
            return house;
        }

        static Transform HotelElevation(Transform parent, string name, Vector3 point, float heading)
        {
            var elevation = Group(name, parent, point);
            elevation.localRotation = Quaternion.Euler(0, heading, 0);
            return elevation;
        }

        void HotelRoomRow(Pieces parts, float width, int bays, float floor, float ceiling, float sill, float tallness,
            float aperture, bool balconies, bool entrance, Material glazing, bool tall = false, float doorTop = GuestDoorTop)
        {
            float bay = width / bays, head = sill + tallness;
            for (int i = 0; i < bays; i++)
            {
                float center = (i + .5f - bays * .5f) * bay;
                bool door = entrance && i == bays / 2;
                float bottom = door || (tall && i == bays / 2) ? DeckHeight : sill;
                var opening = new Rect(center - aperture * .5f, bottom, aperture, head - bottom);
                float left = center - bay * .5f, right = center + bay * .5f;
                Panel(left, right, floor, bottom);
                Panel(left, right, head, ceiling);
                Panel(left, opening.xMin, bottom, head);
                Panel(opening.xMax, right, bottom, head);
                HotelWindow(parts, opening, glazing, door, tall, doorTop);
                if (balconies) HotelBalcony(parts, opening);
            }

            void Panel(float left, float right, float bottom, float top)
            {
                if (top - bottom < .001f) return;
                parts.Add(cube, hotelStone, new Vector3((left + right) * .5f, (bottom + top) * .5f, .235f),
                    new Vector3(right - left, top - bottom, .47f));
            }
        }

        void DetailElevation(Pieces parts, float width, int bays, float eave, float firstCourse, float secondCourse)
        {
            foreach (var course in new[] { (firstCourse, .18f, .19f), (secondCourse, .16f, .18f), (eave - .27f, .27f, .44f) })
            {
                parts.Add(cube, hotelShadow, new Vector3(0, course.Item1 - .12f, -.039f),
                    new Vector3(width, .07f, course.Item3 * .86f));
                parts.Add(cube, hotelTrim, new Vector3(0, course.Item1, -.056f),
                    new Vector3(width + .14f, course.Item2, course.Item3));
            }
            float bay = width / bays, height = firstCourse - DeckHeight;
            for (int i = 0; i <= bays; i++)
            {
                float x = (i - bays * .5f) * bay;
                parts.Add(cube, hotelTrim, new Vector3(x, DeckHeight + height * .5f, -.045f), new Vector3(.31f, height, .15f));
                parts.Add(softBox, hotelShadow, new Vector3(x, .23f, -.067f), new Vector3(.43f, .32f, .23f));
            }
        }

        void BuildGuestDoors(Transform elevation, float openingWidth)
        {
            float half = openingWidth * .5f - .113f;
            var arrival = Group("Glazed guest wing doors", elevation, Vector3.forward * .152f);
            BuildDoubleDoors(arrival, openingWidth, GuestDoorTop, lobbyGlass);
            var vestibule = new Pieces();
            vestibule.Add(cube, hotelShadow, new Vector3(0, 1.30f, 2.18f), new Vector3(openingWidth + .29f, 2.58f, .12f));
            foreach (int side in new[] { -1, 1 })
                vestibule.Add(cube, hotelStone, new Vector3(side * (half + .12f), 1.30f, 1.11f), new Vector3(.18f, 2.58f, 2.15f));
            vestibule.Build(Group("Recessed arrival vestibule", arrival, Vector3.zero));
        }

        (Transform Left, Transform Right) BuildDoubleDoors(Transform arrival, float openingWidth, float doorTop, Material glazing)
        {
            float half = openingWidth * .5f - .113f;
            return (MakeDoor(-1), MakeDoor(1));

            Transform MakeDoor(int side)
            {
                var hinge = Group(side < 0 ? "Left bronze door hinge" : "Right bronze door hinge", arrival, new Vector3(side * half, DeckHeight, 0));
                var fittings = new Pieces();
                float doorWidth = half - .018f, height = doorTop - DeckHeight;
                float center = -side * doorWidth * .5f;
                var frame = new Rect(center - doorWidth * .5f, .017f, doorWidth, height - .054f);
                Rect glass = FrameRectangle(fittings, frame, .085f, .009f, .105f, hotelBronze);
                AddGlazing(fittings, glass, .052f, glazing);
                fittings.Add(cube, hotelBronze, new Vector3(center, .23f, .01f), new Vector3(doorWidth, .27f, .104f));
                fittings.Add(softBox, hotelBrass, new Vector3(-side * (doorWidth - .17f), 1.19f, -.081f), new Vector3(.048f, .42f, .073f));
                fittings.Build(hinge);
                return hinge;
            }
        }

        void BuildLobbyPortico(Transform lobby)
        {
            var portico = Group("Grand guest entrance portico", lobby, Vector3.zero);
            var pieces = new Pieces();
            var canopy = Paint("Warm patinated bronze canopy", "#78736A");
            const float halfWidth = 7.84f, front = -3.02f;
            pieces.Add(cube, hotelTrim, new Vector3(0, 4.02f, -1.47f), new Vector3(16.8f, .40f, 3.72f));
            pieces.Add(cube, canopy, new Vector3(0, 4.25f, -1.47f), new Vector3(16.94f, .12f, 3.79f));
            pieces.Add(cube, hotelShadow, new Vector3(0, 4.04f, front - .235f), new Vector3(16.75f, .25f, .10f));
            pieces.Add(cube, hotelBrass, new Vector3(0, 4.14f, -3.338f), new Vector3(16.82f, .039f, .031f));
            for (int column = 1; column < 12; column++)
            {
                pieces.Add(cube, wood, new Vector3((column - 6) * 1.29f, 3.805f, -1.47f), new Vector3(.08f, .10f, 3.63f));
                pieces.Add(cube, hotelShadow, new Vector3((column - 6) * 1.29f, 4.318f, -1.47f), new Vector3(.032f, .032f, 3.71f));
            }
            foreach (int side in new[] { -1, 1 })
            {
                var p = new Vector3(side * halfWidth, 0, front);
                pieces.Add(softBox, hotelShadow, p + Vector3.up * .232f, new Vector3(.88f, .35f, .91f));
                pieces.Add(cube, hotelTrim, p + Vector3.up * 2.09f, new Vector3(.61f, 3.63f, .60f));
                pieces.Add(cube, hotelShadow, p + Vector3.up * 3.75f, new Vector3(.75f, .14f, .75f));
                pieces.Add(cube, hotelBronze, p + new Vector3(.005f, 2.55f, -.33f), new Vector3(.29f, .56f, .16f));
                pieces.Add(cube, cream, p + new Vector3(.005f, 2.54f, -.424f), new Vector3(.215f, .38f, .037f));
                var world = lobby.TransformPoint(p);
                Block(world.x, world.z, .49f, .50f);
            }
            var sun = new Vector3(0, 4.04f, front - .331f);
            pieces.Add(cylinder, hotelBrass, sun, new Vector3(.25f, .038f, .25f), Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 8; i++)
            {
                var direction = Quaternion.Euler(0, 0, i * 45);
                pieces.Add(cube, hotelBrass, sun + direction * (Vector3.up * .183f), new Vector3(.026f, .08f, .039f), direction);
            }
            foreach (int side in new[] { -1, 1 })
                pieces.Add(softBox, hotelBrass, new Vector3(side * .185f, 1.24f, .131f), new Vector3(.042f, .46f, .071f));
            pieces.Build(portico);
        }

        void BuildLobbyInterior(Transform elevation)
        {
            var lobby = Group("Lobby reception and lounges", elevation, Vector3.zero);
            var walls = Paint("Warm sand-colored lobby walls", "#E6CEAD");
            var upholstery = Paint("Sage lobby upholstery", "#94AA92");
            var glow = new Material(Shader.Find("Unlit/Color")) {
                name = "Opal lobby pendant light", color = new Color32(255, 226, 165, 255)
            };
            var floor = Tiles("Lobby honed limestone floor", "#DED0B7", "#B9AA8F", .85f, .85f, false);
            MakeMesh("Lobby stone floor", Fill(Rectangle(-9.53f, .22f, 9.53f, 5.55f), DeckHeight + .008f), floor, lobby);
            var pieces = new Pieces();
            pieces.Add(cube, hotelTrim, new Vector3(0, 5.40f, 3.04f), new Vector3(19.1f, .20f, 5.18f));
            pieces.Add(cube, walls, new Vector3(0, 2.84f, 5.57f), new Vector3(19.1f, 5.6f, .23f));
            pieces.Add(cube, hotelShadow, new Vector3(0, .20f, 5.428f), new Vector3(19.1f, .27f, .085f));
            foreach (int side in new[] { -1, 1 })
            {
                pieces.Add(cube, walls, new Vector3(side * 9.50f, 2.84f, 3.02f), new Vector3(.17f, 5.6f, 5.18f));
                pieces.Add(cube, hotelTrim, new Vector3(side * 8.95f, 3.28f, 5.39f), new Vector3(.35f, 4.6f, .19f));
            }
            for (int slat = -5; slat <= 5; slat++)
                pieces.Add(cube, wood, new Vector3(slat * .43f, 2.61f, 5.405f), new Vector3(.20f, 4.35f, .15f));
            pieces.Add(softBox, hotelTrim, new Vector3(0, 2.64f, 5.26f), new Vector3(2.52f, 1.36f, .20f));
            pieces.Add(cylinder, hotelBrass, new Vector3(0, 2.68f, 5.13f), new Vector3(.91f, .061f, .91f), Quaternion.Euler(90, 0, 0));
            pieces.Add(cylinder, hotelTrim, new Vector3(0, 2.68f, 5.053f), new Vector3(.73f, .042f, .73f), Quaternion.Euler(90, 0, 0));
            pieces.Add(softBox, wood, new Vector3(0, .64f, 3.64f), new Vector3(3.25f, 1.15f, .81f));
            pieces.Add(softBox, hotelTrim, new Vector3(0, 1.24f, 3.64f), new Vector3(3.42f, .16f, .95f));
            for (int slat = -6; slat <= 6; slat++)
                pieces.Add(cube, hotelBrass, new Vector3(slat * .23f, .65f, 3.217f), new Vector3(.034f, .78f, .045f));

            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 6.56f;
                pieces.Add(softBox, hotelShadow, new Vector3(x, .31f, 4.36f), new Vector3(3.20f, .38f, 1.10f));
                pieces.Add(softBox, upholstery, new Vector3(x, .59f, 4.28f), new Vector3(3.02f, .28f, 1.03f));
                pieces.Add(softBox, upholstery, new Vector3(x, .96f, 4.74f), new Vector3(3.05f, .62f, .32f));
                for (int corner = -1; corner <= 1; corner += 2)
                    pieces.Add(softBox, cream, new Vector3(x + corner * .93f, .925f, 4.505f), new Vector3(.52f, .54f, .21f),
                        Quaternion.Euler(-12, 0, corner * 11));
                pieces.Add(cylinder, hotelBronze, new Vector3(x, .30f, 2.72f), new Vector3(.13f, .22f, .13f));
                pieces.Add(cylinder, hotelTrim, new Vector3(x, .54f, 2.72f), new Vector3(1.00f, .052f, .87f));
                pieces.Add(cylinder, hotelBrass, new Vector3(x, 3.91f, 2.39f), new Vector3(.041f, 1.35f, .041f));
                pieces.Add(cylinder, hotelBrass, new Vector3(x, 2.83f, 2.39f), new Vector3(.53f, .05f, .53f));
                pieces.Add(sphere, glow, new Vector3(x, 2.64f, 2.39f), new Vector3(.48f, .37f, .48f));
            }
            for (int i = -1; i <= 1; i++)
            {
                float y = 2.87f - .19f * Mathf.Abs(i), x = i * .68f;
                pieces.Add(cylinder, hotelBrass, new Vector3(x, (y + 5.30f) * .5f, 2.23f), new Vector3(.033f, (5.30f - y) * .5f, .033f));
                pieces.Add(sphere, glow, new Vector3(x, y, 2.23f), Vector3.one * .43f);
            }
            pieces.Build(lobby);
        }

        void FurnishGuestTerrace(Transform elevation, float width)
        {
            var terrace = Group("Guest terrace seating and planters", elevation, Vector3.zero);
            var pieces = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                var seat = new Vector3(side * width * .277f, DeckHeight, -.61f);
                pieces.Add(softBox, hotelShadow, seat + Vector3.up * .22f, new Vector3(2.35f, .44f, .64f));
                pieces.Add(softBox, wood, seat + Vector3.up * .47f, new Vector3(2.42f, .11f, .72f));
                var planter = new Vector3(side * width * .109f, DeckHeight, -.59f);
                pieces.Add(softBox, hotelTrim, planter + Vector3.up * .31f, new Vector3(1.13f, .63f, .73f));
                pieces.Add(softBox, plantingSoil, planter + Vector3.up * .621f, new Vector3(.99f, .036f, .59f));
                for (int i = 0; i < 5; i++)
                    PlantFlower(pieces, planter + new Vector3((i - 2) * .17f, .625f, (i % 2 - .5f) * .18f), .48f + i % 2 * .09f, i * 3 + side + 1);
            }
            pieces.Build(terrace);
        }

        void AddRoof(Transform parent, Vector3 eave, float width, float depth, float rise)
        {
            var rooftop = Group("Hipped terracotta roof", parent, eave);
            float x = width * .5f, z = depth * .5f, peak = x - Mathf.Min(z * .77f, x * .54f);
            var points = new[]{new Vector3(-x,0,-z), new Vector3(x,0,-z), new Vector3(x,0,z), new Vector3(-x,0,z),
                new Vector3(-peak,rise,0),new Vector3(peak,rise,0)};
            MakeMesh("Joined roof planes", MeshOf("Hipped roof", points, new[]{0,4,1,1,4,5,1,5,2,2,5,3,3,5,4,3,4,0}), roof, rooftop);
            var tiles = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                var slope = Quaternion.Euler(Mathf.Atan2(side * rise, z) * Mathf.Rad2Deg, 0, 0);
                int columns = Mathf.CeilToInt(width / .72f);
                for (int row = 0; row < 7; row++)
                {
                    float progress = (row + .38f) / 7;
                    float half = Mathf.Lerp(x, peak, progress);
                    for (int tile = 0; tile < columns; tile++)
                    {
                        float px = (tile + .5f) / columns * width - x;
                        if (Mathf.Abs(px) + .18f > half) continue;
                        tiles.Add(cube, (tile + row * 2) % 5 < 2 ? sunTile : clayTile,
                            new Vector3(px, progress * rise + .07f, side * z * (1 - progress)),
                            new Vector3(width / columns * .944f, .14f, z / 7 * 1.08f), slope);
                    }
                }
                tiles.Add(cube, ivory, new Vector3(0, -.03f, side * z), new Vector3(width, .14f, .23f));
            }
            tiles.Add(softBox, sunTile, new Vector3(0, rise + .055f, 0), new Vector3(peak * 2 + .33f, .29f, .31f));
            tiles.Build(rooftop);
        }

        void Arc(Pieces pieces, Material material, Vector3 center, float radius, float thick, float rise = 1)
        {
            const int sections = 13;
            for (int i = 0; i < sections; i++)
            {
                var a = center + Point(i);
                var b = center + Point(i + 1);
                var axis = b - a;
                pieces.Add(cube, material, (a + b) * .5f, new Vector3(axis.magnitude + .024f, thick, .36f),
                    Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg));
            }
            Vector3 Point(int i)
            {
                float angle = Mathf.PI * i / sections;
                return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * rise, 0) * radius;
            }
        }

        ParkResortSunbeds MakeLoungers(Transform root)
        {
            var patio = Group("Poolside loungers and striped parasols", root, Vector3.zero);
            ScaleAboveDeck(patio, .78f);
            var spots = new ParkResortSunbeds.Spot[3];
            var cloth = UmbrellaPanel();
            var rows = new[] { 0, 2, 3 };
            for (int index = 0; index < rows.Length; index++)
            {
                int i = rows[index];
                float z = -10.2f + i * 6.37f;
                var origin = new Vector3(-30.48f, DeckHeight, z);
                var nook = Group("Striped shade and wide sunbed", patio, origin);
                var pieces = new Pieces();
                const float shadeX = -3.65f;
                pieces.Add(cylinder, darkWood, new Vector3(shadeX, 1.65f, 0), new Vector3(.12f, 1.67f, .12f));
                pieces.Add(cylinder, honey, new Vector3(shadeX, .11f, 0), new Vector3(.61f, .11f, .61f));
                pieces.Add(sphere, wood, new Vector3(shadeX, 3.49f, 0), Vector3.one * .18f);
                for (int panel = 0; panel < 10; panel++)
                    pieces.Add(cloth, panel % 2 == 0 ? cream : sage, new Vector3(shadeX, 3.38f, 0), Vector3.one,
                        Quaternion.Euler(0, panel * 36 + i * 14, 0));
                var center = new Vector3(.05f, .43f, 0);
                var cushion = center + Vector3.up * .17f;
                pieces.Add(softBox, wood, center, new Vector3(6.22f, .22f, 3.08f));
                pieces.Add(softBox, cream, cushion, new Vector3(6.05f, .20f, 2.92f));
                pieces.Add(softBox, sage, cushion + new Vector3(2.13f, .099f, 0), new Vector3(.54f, .018f, 2.85f));
                foreach (int side in new[] { -1, 1 })
                    foreach (int end in new[] { -1, 1 })
                        pieces.Add(softBox, darkWood, center + new Vector3(end * 2.67f, -.24f, side * 1.25f), new Vector3(.24f, .39f, .24f));
                pieces.Build(nook);
                Block(origin.x + center.x, z, 3.17f, 1.59f);
                Block(origin.x + shadeX, z, .37f, .37f);
                spots[index] = new ParkResortSunbeds.Spot
                {
                    Approach = origin + new Vector3(center.x, 0, -2.23f),
                    Cushion = origin + cushion + Vector3.up * .10f
                };
            }
            var sunbeds = patio.gameObject.AddComponent<ParkResortSunbeds>();
            sunbeds.Configure(spots);
            return sunbeds;
        }

        static Mesh UmbrellaPanel()
        {
            const int sections = 5;
            var points = new Vector3[2 * (sections + 1) + 1];
            var top = new int[sections * 9];
            for (int i = 0; i <= sections; i++)
            {
                float angle = 36 * Mathf.Deg2Rad * i / sections;
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                points[1 + 2 * i] = direction * 1.24f - Vector3.up * .38f;
                points[2 + 2 * i] = direction * 1.90f - Vector3.up * (.69f + .04f * Mathf.Sin(Mathf.PI * i / sections));
                if (i == sections) continue;
                int a = 1 + i * 2, k = i * 9;
                top[k] = 0; top[k + 1] = a + 2; top[k + 2] = a;
                top[k + 3] = a; top[k + 4] = a + 2; top[k + 5] = a + 1;
                top[k + 6] = a + 1; top[k + 7] = a + 2; top[k + 8] = a + 3;
            }
            return MeshOf("Canvas parasol panel", points, top);
        }

    }
}
