using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkChristmasCathedral
    {
        const float Front = -7.45f;
        const float DoorHalfWidth = 1.90f, DoorSpring = 2.85f, DoorCrown = 5.62f, DoorFloor = .18f;
        internal const float PlazaPosition = 12.7f;
        const float AisleStart = -6.22f, ApseStart = 9.45f, ApseBack = 12.78f;
        internal const float FootprintBack = PlazaPosition + ApseBack;
        const float AisleHalfWidth = 15.36f, UpperHalfWidth = 10.64f, TerraceHeight = 9.73f;
        const float RoofEave = 14.28f, RoofRidge = 17.95f;
        readonly Mesh slab = PrimitiveMesh(PrimitiveType.Cube);
        readonly Mesh rounded = ParkSurface.RoundedBox();
        readonly Mesh column = PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Mesh glimmerPlane = PrimitiveMesh(PrimitiveType.Quad);
        readonly Mesh bead = SpireMesh("Carved stone bead", new Vector2(.015f,0), new Vector2(.39f,.16f),
            new Vector2(.50f,.5f), new Vector2(.39f,.84f), new Vector2(.015f,1));
        readonly Mesh spire = SpireMesh("Tapered Gothic spire", new Vector2(.50f,0), new Vector2(.48f,.16f),
            new Vector2(.28f,.51f), new Vector2(.12f,.80f), new Vector2(.009f,1));
        readonly Mesh collar = SpireMesh("Carved stone collar", new Vector2(.43f,0), new Vector2(.51f,.20f),
            new Vector2(.51f,.74f), new Vector2(.36f,1));
        readonly Mesh traceryRing = Annulus("Tracery ring", .78f, 1, .11f, 24);
        readonly Material wall = Stone("Warm ivory marble", "#F1E3DA", "#B6A5B9");
        readonly Material ivory = Stone("Ivory carved marble", "#FAEFE5", "#C6B4C1");
        readonly Material rose = Stone("Rose marble relief", "#DEBFB8", "#A9899B");
        readonly Material shadow = Stone("Shadowed recess stone", "#A37C76", "#84687B");
        readonly Material snow = Stone("Snow on the cathedral", "#F7F0FF", "#B3B9E1");
        readonly Material timber = Paint("Oak entrance doors", "#A75D2D");
        readonly Material iron = Paint("Forged clock hands and door fittings", "#3D2C32");
        readonly Material brass = Paint("Golden brass fittings", "#DCAF57");
        readonly Material lightMarble = Interior("Lit nave marble", "#EDBC83");
        readonly Material naveShadow = Interior("Nave floor and vault recesses", "#A27257");
        readonly Material glassRim = Solid("Deep amber stained glass", "#CD6C26");
        readonly Material glass = Solid("Golden interior window light", "#FFBF5D");
        readonly Material litGlass = LancetGlass();
        readonly Material flame = Solid("Ivory chandelier flames", "#FFF3B5");
        readonly Material dial = Solid("White clock face", "#FFFFFF");
        readonly Material ink = Solid("Roman numerals and clock hands", "#321F25");
        readonly Material green = Paint("Green Christmas garlands", "#28583F");
        readonly Material greenTips = Paint("Fir garland tips", "#477450");
        readonly Material berry = Paint("Red ribbons and berries", "#AF3C42");
        readonly Material windowGlow = Halo("Warm glow around the lancet windows", "#FF9D43", .23f);
        readonly Material chandelierGlow = Halo("Amber chandelier glow", "#FF992E", .58f);
        readonly Material windowSpill = Halo("Warm window light on the snow", "#FFAD63", .41f);

        internal static ParkHouse Create(Transform parent) => new ParkChristmasCathedral().Build(parent);

        ParkHouse Build(Transform parent)
        {
            var cathedral = Group("Christmas clock cathedral", parent, new Vector3(0, 0, PlazaPosition));
            var facade = Group("Gothic marble facade", cathedral, Vector3.zero);
            BuildCentralBay(facade);
            for (int side = -1; side <= 1; side += 2)
            {
                BuildWindowBay(facade, side * 6.70f, 6.06f, 13.05f, -7.08f, true);
                BuildWindowBay(facade, side * 12.63f, 5.78f, 10.66f, -6.72f, false);
                BuildSideWall(cathedral, side);
            }
            BuildLongRoof(cathedral);
            BuildApse(cathedral);
            BuildPinnacles(facade);
            BuildNave(cathedral);

            var house = cathedral.gameObject.AddComponent<ParkHouse>();
            house.DisplayName = "시계 성당";
            house.DoorDuration = 1.30f;
            house.OpenAngle = 104;
            house.ExitSeconds = 3.85f;
            house.DoorHinge = BuildDoor(cathedral, -1);
            house.SecondDoorHinge = BuildDoor(cathedral, 1);
            house.Inside = Group("Inside the cathedral", cathedral, new Vector3(0,DoorFloor,-4.28f));
            house.Threshold = Group("Gothic doorway threshold", cathedral, new Vector3(0,DoorFloor,Front-.24f));
            house.Welcome = Group("Plaza welcome", cathedral, new Vector3(0,.025f,Front-2.70f));
            house.ExitSlope = new[] { new Vector2(-4.28f,DoorFloor), new Vector2(Front-.40f,DoorFloor),
                new Vector2(Front-1.96f,.025f) };

            house.EntranceBell = Group("Swinging brass bell", cathedral, new Vector3(0,19.34f,Front+.50f));
            var bell = SpireMesh("Curved brass bell", new Vector2(.63f,0), new Vector2(.67f,.08f), new Vector2(.49f,.23f),
                new Vector2(.35f,.53f), new Vector2(.30f,.85f), new Vector2(.05f,1));
            ParkGeometry.Place("Bell shell", bell, house.EntranceBell, Vector3.down*.83f, Vector3.one*.82f, brass);
            ParkGeometry.Place("Bell clapper", bead, house.EntranceBell, Vector3.down*.95f, Vector3.one*.21f, brass);
            var chime = cathedral.gameObject.AddComponent<AudioSource>();
            chime.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ChristmasClockChime.wav");
            chime.playOnAwake = false;
            chime.volume = .46f;
            chime.spatialBlend = .12f;
            if (chime.clip) house.EntranceChime = chime;
            var thresholdGlow = Halo("Chandelier light spilling through the open doors", "#FF9C39", 0);
            var spill = AddGlow(cathedral, "Doorway light on the plaza", thresholdGlow,
                new Vector3(0,.036f,Front-1.85f), new Vector2(8.4f,6.1f), true);
            house.EntranceLightSpill = spill;
            return house;
        }

        void BuildCentralBay(Transform parent)
        {
            var root = Group("Great portal and Roman clock", parent, Vector3.zero);
            var p = new Pieces();
            const float halfBay = 3.66f, wallHeight = 15.75f;
            foreach (int side in new[] { -1, 1 })
            {
                float pier = (halfBay + DoorHalfWidth)*.5f;
                p.Add(slab, wall, new Vector3(side*pier,wallHeight/2,Front+.53f), new Vector3(halfBay-DoorHalfWidth,wallHeight,1.06f));
                p.Add(rounded, rose, new Vector3(side*2.91f,.37f,Front-.09f), new Vector3(1.35f,.74f,.60f));
                AddNiche(p, side*2.88f, 1.18f, Front-.07f, .95f, 2.28f);
                AddNiche(p, side*2.92f, 5.9f, Front-.07f, .96f, 2.39f);
            }
            // Facade over the opening. The lower edge follows the real, open, pointed arch.
            var arch = Arch(DoorHalfWidth, DoorFloor, DoorSpring, DoorCrown);
            AddWallAbove(p, arch, 6.75f, Front+.51f, 1.02f);
            p.Add(slab, wall, new Vector3(0,(6.75f+wallHeight)/2,Front+.53f), new Vector3(DoorHalfWidth*2,wallHeight-6.75f,1.06f));
            Rim(p, arch, Arch(DoorHalfWidth+.12f,DoorFloor,DoorSpring,DoorCrown+.15f), shadow, Front-.13f,.27f);
            Rim(p, Arch(DoorHalfWidth+.12f,DoorFloor,DoorSpring,DoorCrown+.15f),
                Arch(DoorHalfWidth+.37f,DoorFloor,DoorSpring+.03f,DoorCrown+.49f), ivory, Front-.25f,.34f);
            Rim(p, Arch(DoorHalfWidth+.37f,DoorFloor,DoorSpring+.03f,DoorCrown+.49f),
                Arch(DoorHalfWidth+.48f,DoorFloor,DoorSpring+.05f,DoorCrown+.62f), rose, Front-.31f,.41f);
            Rim(p, Arch(DoorHalfWidth+.48f,DoorFloor,DoorSpring+.05f,DoorCrown+.62f),
                Arch(DoorHalfWidth+.65f,DoorFloor,DoorSpring+.08f,DoorCrown+.90f), ivory, Front-.36f,.31f);
            var doorTrim = Arch(DoorHalfWidth+.67f, DoorFloor, DoorSpring+.11f, DoorCrown+.91f);
            for (int i = 3; i < doorTrim.Count-3; i += 2)
                p.Add(bead, ivory, InPlane(doorTrim[i],Front-.54f), new Vector3(.14f,.23f,.20f));
            AddGarland(p, Arch(DoorHalfWidth+.29f,DoorFloor,DoorSpring,DoorCrown+.36f), Front-.56f);
            Moulding(p, 0,7.35f,halfBay*2,Front);
            AddBalustrade(p, 0,7.55f,halfBay*1.94f,Front-.33f);
            AddClock(root,p,new Vector3(0,11.85f,Front-.085f));
            foreach (int side in new[] { -1, 1 })
            {
                Quatrefoil(p, new Vector3(side*2.28f,8.65f,Front-.22f), .49f);
                Quatrefoil(p, new Vector3(side*2.25f,14.97f,Front-.21f), .45f);
                AddBlindLancet(p,new Vector3(side*3.06f,8.05f,Front-.11f),.36f,5.60f);
                AddBlindLancet(p,new Vector3(side*1.85f,15.49f,Front-.11f),.37f,1.23f);
            }
            AddPediment(p, 0, 15.35f, 7.45f, 3.56f, Front, 1.85f);
            RoseWindow(p,new Vector3(0,16.65f,Front-.35f),1.15f);

            // A low tapered landing puts the character's feet on the same slope as the stone.
            var ramp = ParkGeometry.Extrude("Sloping marble entrance", new List<Vector2>
            {
                new Vector2(Front-1.96f,0), new Vector2(Front+.18f,0), new Vector2(Front+.18f,DoorFloor),
                new Vector2(Front-.40f,DoorFloor), new Vector2(Front-1.96f,.025f)
            }, 4.9f);
            p.Add(ramp, ivory, Vector3.zero, Vector3.one,Quaternion.Euler(0,-90,0));
            p.Build(root);
        }

        void BuildWindowBay(Transform parent, float x, float width, float eave, float z, bool inner)
        {
            var root = Group(inner ? "Upper lancet bay" : "Outer lancet bay", parent, new Vector3(x,0,z));
            var p = new Pieces();
            p.Add(slab, wall, new Vector3(0,eave/2,.51f), new Vector3(width,eave,1.02f));
            p.Add(rounded, rose, new Vector3(0,.31f,-.015f), new Vector3(width+.08f,.62f,.54f));
            Moulding(p,0,.69f,width,0);
            for (int row = 1; row < eave - .4f; row++)
            {
                float length = width*.48f-.82f;
                foreach (int side in new[] {-1,1})
                    p.Add(slab, rose, new Vector3(side*(width*.5f-length*.5f),row*.98f,-.011f), new Vector3(length,.023f,.035f));
            }
            float lowHeight = inner ? 4.23f : 3.98f;
            AddWindow(root,p,0,1.05f,inner ? 2.10f : 2.52f,lowHeight,0);
            float upperBottom = inner ? 6.67f : 5.83f;
            AddWindow(root,p,0,upperBottom,inner ? 2.32f : 2.72f,inner ? 4.83f : 3.84f,0);
            Moulding(p,0,upperBottom-.75f,width,0);
            AddBalustrade(p,0,upperBottom-.59f,width*.90f,-.37f);
            AddWreath(p,new Vector3(0,1.13f,-.48f),.46f);
            foreach (int side in new[] {-1,1})
            {
                AddNiche(p,side*(width*.345f),1.42f,-.09f,.66f,1.84f);
                AddNiche(p,side*(width*.345f),upperBottom+.40f,-.09f,.64f,1.90f);
                AddBlindLancet(p,new Vector3(side*width*.34f,upperBottom+2.33f,-.089f),.43f,inner ? 2.68f : 2.5f);
                AddBlindLancet(p,new Vector3(side*width*.36f,3.31f,-.081f),.46f,upperBottom-4.17f);
                AddBlindLancet(p,new Vector3(side*width*.21f,eave-.03f,-.088f),.46f,inner ? 1.46f : 1.37f);
            }
            float gableHeight = inner ? 2.98f : 2.84f;
            AddPediment(p,0,eave-.22f,width+.11f,gableHeight,0,1.72f);
            AddBlindLancet(p,new Vector3(0,eave-.10f,-.097f),.92f,gableHeight*.85f);
            Quatrefoil(p,new Vector3(0,eave+.77f,-.24f),inner ? .67f : .62f);
            p.Build(root);
        }

        void BuildSideWall(Transform parent, int side)
        {
            var body = Group(side < 0 ? "West aisle and terrace" : "East aisle and terrace", parent,
                new Vector3(side*AisleHalfWidth,0,AisleStart));
            body.localRotation = Quaternion.Euler(0,-90*side,0);
            var p = new Pieces();
            const int bays = 6;
            const float span = ApseStart-AisleStart, bayWidth = span/bays;
            const float terraceWidth = AisleHalfWidth-UpperHalfWidth;
            float mid = side*span*.5f;
            p.Add(slab,wall,new Vector3(mid,TerraceHeight*.5f,.42f),new Vector3(span,TerraceHeight,.88f));
            p.Add(slab,wall,new Vector3(mid,(TerraceHeight+RoofEave)*.5f,terraceWidth+.31f),
                new Vector3(span,RoofEave-TerraceHeight,.69f));
            p.Add(slab,rose,new Vector3(mid,TerraceHeight-.07f,terraceWidth*.5f),new Vector3(span,.21f,terraceWidth+.32f));
            p.Add(slab,snow,new Vector3(mid,TerraceHeight+.06f,terraceWidth*.56f),new Vector3(span,.055f,terraceWidth*.69f));
            Moulding(p,mid,1.1f,span,0);
            Moulding(p,mid,TerraceHeight-.32f,span,0);
            Moulding(p,mid,RoofEave-.27f,span,terraceWidth);
            AddBalustrade(p,mid,TerraceHeight+.11f,span,-.20f);
            for (int i = 0; i < bays; i++)
            {
                float x = side*((i+.5f)*bayWidth);
                float pier = side*(.12f+i*bayWidth);
                AddWindow(body,p,x,2.12f,1.60f,5.40f,0);
                AddWindow(body,p,x,TerraceHeight+.42f,1.62f,3.24f,terraceWidth);
                AddPier(p,pier,0,TerraceHeight+.03f,-.21f,.53f);
                AddPier(p,pier,TerraceHeight+.01f,4.46f,terraceWidth-.09f,.40f);
                AddPinnacle(p,new Vector3(pier,TerraceHeight+.15f,-.11f),2.40f+i%2*.56f,.51f);
                AddFlyingButtress(p,pier,terraceWidth);
            }
            p.Build(body);
        }

        void BuildPinnacles(Transform parent)
        {
            var root = Group("Buttresses, finials and marble spires",parent,Vector3.zero);
            var p = new Pieces();
            // Smaller spires recede behind the ornate front to give the roofline real depth.
            for (int side = -1; side <= 1; side += 2)
            {
                AddPier(p,side*3.65f,0,15.56f,Front-.26f,.94f);
                AddPier(p,side*9.77f,0,12.87f,-7.16f,.92f);
                AddPier(p,side*15.44f,0,10.66f,-6.77f,.98f);
                AddPinnacle(p,new Vector3(side*3.65f,15.43f,Front-.25f),5.33f,.99f);
                AddPinnacle(p,new Vector3(side*9.77f,12.68f,-7.14f),5.45f,1.04f);
                AddPinnacle(p,new Vector3(side*15.44f,10.48f,-6.78f),5.07f,.97f);
                AddPinnacle(p,new Vector3(side*1.23f,18.2f,Front+.52f),3.25f,.54f);
                AddPinnacle(p,new Vector3(side*2.45f,17.09f,Front+.40f),2.90f,.50f);
                AddPinnacle(p,new Vector3(side*6.7f,15.78f,-6.97f),2.83f,.62f);
                AddPinnacle(p,new Vector3(side*12.63f,13.21f,-6.62f),2.70f,.62f);
                for (int i = 0; i < 9; i++)
                {
                    float x = side*(4.57f+i*1.24f);
                    float y = 13.55f-i*.26f + (i%3 == 1 ? .95f : 0);
                    AddPinnacle(p,new Vector3(x,y,-5.62f-i%2*.78f),2.8f+i%3*.62f,.51f+i%2*.1f);
                }
            }
            // The tall open lantern above the central gable houses the swinging bell.
            foreach (int side in new[] { -1,1 })
                AddPier(p,side*.55f,18.12f,1.48f,Front+.45f,.20f);
            p.Add(slab,ivory,new Vector3(0,19.69f,Front+.46f),new Vector3(1.52f,.20f,1.37f));
            AddPinnacle(p,new Vector3(0,19.68f,Front+.49f),4.28f,1.00f);
            p.Build(root);
        }

        void BuildNave(Transform parent)
        {
            var root = Group("Illuminated vaulted nave", parent,Vector3.zero);
            var p = new Pieces();
            const float rear = ApseBack-.75f, naveDepth = rear-Front, naveCenter = (rear+Front)*.5f;
            p.Add(slab,lightMarble,new Vector3(0,.08f,naveCenter),new Vector3(6.70f,.20f,naveDepth));
            p.Add(slab,naveShadow,new Vector3(0,4.14f,rear),new Vector3(7.01f,8.26f,.43f));
            p.Add(slab,naveShadow,new Vector3(0,7.83f,naveCenter),new Vector3(6.74f,.41f,naveDepth));
            foreach (int side in new[] {-1,1})
            {
                p.Add(slab,naveShadow,new Vector3(side*3.33f,3.93f,naveCenter),new Vector3(.36f,7.82f,naveDepth));
                for (int i = 0; i < 24; i++)
                    p.Add(slab,naveShadow,new Vector3(side*.94f,.183f,Front+.33f+i*.79f), new Vector3(.032f,.007f,.80f));
            }
            var ribOuter = Arch(3.23f,.18f,4.64f,7.58f);
            var ribInner = Arch(2.97f,.18f,4.61f,7.26f);
            for (int i = 0; i < 9; i++)
            {
                float z = -6.27f+i*2.02f;
                Rim(p,ribInner,ribOuter,lightMarble,z,.29f);
                p.Add(slab,naveShadow,new Vector3(0,.181f,z),new Vector3(6.36f,.009f,.04f));
                foreach (int side in new[] {-1,1})
                    p.Add(collar,lightMarble,new Vector3(side*3.04f,.18f,z),new Vector3(.67f,.37f,.67f));
            }
            p.Add(ArchSolid("Backlit nave apse",Arch(1.18f,1.67f,3.66f,5.71f),.10f),glass,new Vector3(0,0,rear-.30f),Vector3.one);
            p.Build(root);
            AddChandelier(root, new Vector3(0,4.17f,-4.91f),1.15f,1.18f);
            AddChandelier(root, new Vector3(-.86f,4.51f,-1.64f),.84f,.84f);
            AddChandelier(root, new Vector3(.91f,4.34f,1.22f),.71f,0);
            AddChandelier(root, new Vector3(.11f,4.57f,5.88f),.66f,0);
        }

        void AddChandelier(Transform parent, Vector3 center, float size, float intensity)
        {
            var root = Group("Hanging brass chandelier",parent,center);
            var p = new Pieces();
            p.Add(collar,brass,new Vector3(0,.06f,0),new Vector3(.38f,.25f,.38f)*size);
            p.Add(bead,brass,new Vector3(0,-.35f*size,0),new Vector3(.31f,.46f,.31f)*size);
            p.Add(slab,brass,new Vector3(0,(7.52f-center.y)*.5f,0),new Vector3(.044f,7.52f-center.y,.044f));
            for (int tier = 0; tier < 2; tier++)
            {
                float radius = (tier == 0 ? 1.14f : .67f)*size;
                float height = (tier == 0 ? -.11f : .62f)*size;
                int candles = tier == 0 ? 12 : 8;
                p.Add(traceryRing,brass,new Vector3(0,height,0),new Vector3(radius,radius,.69f),Quaternion.Euler(90,0,0));
                for (int i = 0; i < candles; i++)
                {
                    float angle = (i+.5f)*2*Mathf.PI/candles;
                    var r = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var cup = r*radius + Vector3.up*height;
                    Member(p,brass,new Vector3(0,(height+.27f*size),0),cup-r*.15f*size,.040f*size,.040f*size);
                    p.Add(collar,brass,cup,new Vector3(.16f,.10f,.16f)*size);
                    p.Add(collar,ivory,cup+Vector3.up*.09f*size,new Vector3(.073f,.25f,.073f)*size);
                    p.Add(bead,flame,cup+Vector3.up*.30f*size,new Vector3(.111f,.227f,.111f)*size);
                }
            }
            p.Build(root);
            AddGlow(root,"Chandelier candle halo",chandelierGlow,new Vector3(0,.44f,-1.06f*size),new Vector2(3.55f,2.52f)*size);
            if (intensity <= 0) return;
            var light = Group("Real warm point light",root,new Vector3(0,.33f,0)).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f,.69f,.34f);
            light.intensity = intensity;
            light.range = 8.3f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        Transform BuildDoor(Transform parent, int side)
        {
            float hinge = side*DoorHalfWidth;
            var root = Group(side < 0 ? "West oak door hinge" : "East oak door hinge",parent,new Vector3(hinge,DoorFloor,Front-.12f));
            var full = Arch(DoorHalfWidth,0,DoorSpring-DoorFloor,DoorCrown-DoorFloor);
            int top = (full.Count+1)/2;
            // Each leaf is a convex half of the same pointed arch used for the stone opening.
            var leaf = new List<Vector2>();
            if (side < 0)
            {
                leaf.Add(new Vector2(0,0));
                leaf.Add(new Vector2(DoorHalfWidth,0));
                for (int i = top; i < full.Count; i++) leaf.Add(full[i] - new Vector2(hinge,0));
            }
            else
            {
                leaf.Add(new Vector2(-DoorHalfWidth,0));
                leaf.Add(new Vector2(0,0));
                for (int i = 2; i <= top; i++) leaf.Add(full[i] - new Vector2(hinge,0));
            }
            var p = new Pieces();
            p.Add(ArchSolid("Pointed oak door leaf",leaf,.17f),timber,Vector3.zero,Vector3.one);
            for (int i = 1; i <= 7; i++)
            {
                float localX = -side * DoorHalfWidth*i/8;
                float x = localX+hinge;
                float height = DoorSpring-DoorFloor + (DoorCrown-DoorSpring)*Mathf.Sqrt(1-Mathf.Abs(x)/DoorHalfWidth);
                p.Add(slab,shadow,new Vector3(localX,height*.49f,-.092f),new Vector3(.016f,height-.13f,.014f));
                p.Add(slab,shadow,new Vector3(localX,height*.49f,.092f),new Vector3(.022f,height-.13f,.014f));
            }
            foreach (float y in new[] { .67f,2.16f })
            {
                p.Add(rounded,iron,new Vector3(-side*.80f,y,-.112f),new Vector3(1.52f,.098f,.058f));
                p.Add(rounded,iron,new Vector3(-side*.80f,y,.112f),new Vector3(1.52f,.094f,.044f));
                for (int i = 0; i < 5; i++)
                {
                    p.Add(bead,brass,new Vector3(-side*(.22f+i*.29f),y-.033f,-.155f),Vector3.one*.073f);
                    p.Add(bead,brass,new Vector3(-side*(.22f+i*.29f),y-.033f,.143f),Vector3.one*.071f);
                }
            }
            p.Add(traceryRing,brass,new Vector3(-side*1.63f,1.22f,-.14f),new Vector3(.15f,.18f,.36f));
            p.Add(traceryRing,brass,new Vector3(-side*1.63f,1.22f,.14f),new Vector3(.15f,.18f,.36f));
            p.Build(root);
            return root;
        }

        void AddClock(Transform parent,Pieces p,Vector3 center)
        {
            p.Add(Annulus("Outer carved clock cornice",2.68f,3.00f,.37f,64),ivory,center,Vector3.one);
            p.Add(Annulus("Pink clock surround",2.61f,2.81f,.23f,64),rose,center+Vector3.back*.18f,Vector3.one);
            p.Add(Annulus("Gilt dial border",2.57f,2.64f,.11f,64),brass,center+Vector3.back*.27f,Vector3.one);
            p.Add(column,dial,center+Vector3.back*.263f,new Vector3(5.18f,.045f,5.18f),Quaternion.Euler(90,0,0));
            var front = center+Vector3.back*.36f;
            for (int i = 0; i < 60; i++)
            {
                var rotation = Quaternion.Euler(0,0,-i*6);
                p.Add(slab,ink,front+rotation*(Vector3.up*2.43f),new Vector3(i%5==0 ? .036f : .020f,.092f,.026f),rotation);
                p.Add(bead,ivory,center+rotation*(Vector3.up*2.895f)+Vector3.back*.233f,new Vector3(.084f,.11f,.12f));
            }
            string[] numerals = { "XII","I","II","III","IV","V","VI","VII","VIII","IX","X","XI" };
            for (int i = 0; i < numerals.Length; i++)
            {
                var rotation = Quaternion.Euler(0,0,-i*30);
                var anchor = front+rotation*(Vector3.up*2.075f);
                AddRoman(p,numerals[i],anchor,rotation);
            }
            var hands=Group("Moving clock hands",parent,front);
            var hour=AddHand(hands,"Hour hand",.052f,1.56f,.16f);
            var minute=AddHand(hands,"Minute hand",.115f,2.07f,.106f);
            var clock=hands.gameObject.AddComponent<ParkTowerClock>();
            clock.HourHand=hour;
            clock.MinuteHand=minute;
            p.Add(bead,brass,front+new Vector3(0,-.146f,-.18f),new Vector3(.32f,.30f,.264f));
        }

        void AddRoman(Pieces p, string numeral, Vector3 anchor, Quaternion rotation)
        {
            const float h = .52f, gap = .052f;
            float Width(char glyph) => glyph == 'I' ? .125f : .248f;
            float width = gap*(numeral.Length-1);
            foreach (char glyph in numeral) width += Width(glyph);
            float x = -width/2;
            foreach (char glyph in numeral)
            {
                float w = Width(glyph);
                if (glyph == 'I') Stroke(w*.5f,-h/2,w*.5f,h/2,.041f);
                if (glyph == 'V')
                {
                    Stroke(.025f,h/2,w*.50f,-h/2,.047f);
                    Stroke(w*.50f,-h/2,w-.026f,h/2,.033f);
                }
                if (glyph == 'X')
                {
                    Stroke(.018f,-h/2,w-.018f,h/2,.042f);
                    Stroke(.019f,h/2,w-.019f,-h/2,.028f);
                }
                if (glyph == 'I')
                {
                    Stroke(0,h/2,w,h/2,.028f);
                    Stroke(0,-h/2,w,-h/2,.028f);
                }
                else
                {
                    Stroke(-.010f,h/2,.062f,h/2,.024f);
                    Stroke(w-.060f,h/2,w+.008f,h/2,.024f);
                    if (glyph == 'X')
                    {
                        Stroke(-.008f,-h/2,.063f,-h/2,.024f);
                        Stroke(w-.065f,-h/2,w+.008f,-h/2,.024f);
                    }
                }
                x += w+gap;

                void Stroke(float a, float ay, float b, float by, float thickness) => Member(p,ink,
                    anchor+rotation*new Vector3(x+a,ay,0),anchor+rotation*new Vector3(x+b,by,0),thickness*1.36f,.046f);
            }
        }

        Transform AddHand(Transform parent,string name,float depth,float length,float width)
        {
            var blade = ParkGeometry.Extrude("Tapered decorative clock hand", new List<Vector2>
            {
                new Vector2(-width*.36f,-.22f),new Vector2(width*.36f,-.22f),new Vector2(width*.50f,length*.67f),
                new Vector2(width*.79f,length*.80f),new Vector2(0,length),new Vector2(-width*.79f,length*.80f),
                new Vector2(-width*.50f,length*.67f)
            },.052f);
            return ParkGeometry.Place(name,blade,parent,Vector3.back*depth,Vector3.one,ink);
        }

        void AddWindow(Transform parent, Pieces p, float x, float bottom, float width, float height, float z)
        {
            var position = new Vector3(x,bottom,z);
            float radius = width/2;
            var outer = Arch(radius,0,height*.55f,height);
            var inset = Arch(radius-.12f,.10f,height*.56f,height-.18f);
            p.Add(ArchSolid("Deep pointed-window recess",outer,.09f),shadow,position+Vector3.back*.041f,Vector3.one);
            p.Add(ArchSolid("Amber Gothic window",inset,.046f),glassRim,position+Vector3.back*.12f,Vector3.one);
            float innerRadius = radius-.19f;
            for (int pane = -1; pane <= 1; pane++)
            {
                float paneWidth = innerRadius/3.08f;
                float paneHeight = height*(pane == 0 ? .88f : .73f);
                var paneShape = Arch(paneWidth,.17f,paneHeight*.65f,paneHeight);
                var panePosition = position+new Vector3(pane*innerRadius*.655f,0,-.16f);
                p.Add(GlassPane(paneShape,paneWidth,paneHeight),litGlass,panePosition+Vector3.back*.022f,Vector3.one);
                Rim(p,Arch(paneWidth-.029f,.16f,paneHeight*.65f,paneHeight-.07f),paneShape,rose,panePosition,.093f);
            }
            for (int bar=1;bar<4;bar++)
                p.Add(slab,rose,position+new Vector3(0,.22f+bar*height*.123f,-.195f),new Vector3(2*innerRadius,.049f,.068f));
            PutRim(inset,outer,ivory,.14f,.20f);
            PutRim(outer,Arch(radius+.11f,-.04f,height*.57f,height+.23f),rose,.19f,.16f);
            PutRim(Arch(radius+.11f,-.04f,height*.57f,height+.23f),
                Arch(radius+.21f,-.08f,height*.57f,height+.43f),ivory,.22f,.16f);
            p.Add(rounded,ivory,position+new Vector3(0,.011f,-.125f),new Vector3(width+.49f,.22f,.57f));
            Quatrefoil(p,position+new Vector3(0,height*.78f,-.214f),radius*.30f);
            AddGlow(parent,"Warm light in pointed window",windowGlow,position+new Vector3(0,height*.46f,-.43f),
                new Vector2(width*1.74f,height*1.27f));
            if (bottom < 2.3f) AddGlow(parent,"Window light on the snow",windowSpill,
                new Vector3(x,.038f,z-1.33f),new Vector2(width*1.89f,3.81f),true);

            void PutRim(List<Vector2> a, List<Vector2> b, Material material, float offset, float depth) =>
                Rim(p,a,b,material,position+Vector3.back*offset,depth);
        }

        void AddPediment(Pieces p, float x, float bottom, float width, float rise, float z, float depth)
        {
            var points = new List<Vector2> { new Vector2(-width/2,0),new Vector2(width/2,0),new Vector2(0,rise) };
            var position = new Vector3(x,bottom,z+depth/2);
            p.Add(ArchSolid("Triangular Gothic gable",points,depth),wall,position,Vector3.one);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 foot = new Vector3(x+side*width*.51f,bottom,z-.27f);
                Vector3 top = new Vector3(x,bottom+rise+.11f,z-.27f);
                Member(p,rose,foot,top,.31f,.55f);
                Member(p,ivory,foot+Vector3.back*.19f,top+Vector3.back*.19f,.125f,.24f);
                Member(p,snow,foot+Vector3.up*.13f,top+Vector3.up*.13f,.101f,.47f);
                for (int i = 1; i <= 6; i++)
                {
                    var place = Vector3.Lerp(foot,top,i/7f)+new Vector3(side*.046f,.075f,-.19f);
                    p.Add(bead,ivory,place,new Vector3(.18f,.31f,.25f),Quaternion.Euler(0,0,-side*23));
                }
            }
        }

        void AddPier(Pieces p, float x, float y, float height, float z, float width)
        {
            var bottom = new Vector3(x,y,z);
            p.Add(slab,wall,bottom+Vector3.up*height*.50f,new Vector3(width*.83f,height,width*.92f));
            p.Add(collar,rose,bottom,new Vector3(width*1.28f,.32f,width*1.20f));
            p.Add(collar,ivory,bottom+Vector3.up*.30f,new Vector3(width*1.08f,.22f,width*1.04f));
            foreach (int side in new[] {-1,0,1})
            {
                var face = bottom+new Vector3(side*width*.32f,0,-width*.45f);
                p.Add(column,ivory,face+Vector3.up*height*.49f,new Vector3(width*.215f,height*.43f,width*.215f));
                p.Add(collar,ivory,face+Vector3.up*height*.91f,new Vector3(width*.3f,.24f,width*.3f));
            }
            p.Add(collar,rose,bottom+Vector3.up*height*.96f,new Vector3(width*1.30f,.22f,width*1.33f));
            p.Add(collar,snow,bottom+Vector3.up*(height*.96f+.19f),new Vector3(width*1.36f,.08f,width*1.40f));
            if (height < 4 || width < .55f) return;
            AddNiche(p,x,y+2.46f,z-width*.49f,width*.74f,2.25f);
            AddNiche(p,x,y+6.07f,z-width*.49f,width*.69f,1.92f);
        }

        void AddPinnacle(Pieces p, Vector3 at, float height, float width)
        {
            p.Add(collar,rose,at+Vector3.down*.32f,new Vector3(width*1.40f,height*.097f+.32f,width*1.40f));
            p.Add(collar,snow,at+Vector3.up*height*.086f,new Vector3(width*1.46f,height*.041f,width*1.46f));
            foreach (int x in new[] {-1,1})
            foreach (int z in new[] {-1,1})
            {
                var corner = at+new Vector3(x*width*.36f,height*.086f,z*width*.36f);
                p.Add(column,ivory,corner+Vector3.up*height*.126f,new Vector3(width*.179f,height*.136f,width*.179f));
                p.Add(spire,rose,corner+Vector3.up*height*.19f,new Vector3(width*.32f,height*.28f,width*.32f));
            }
            p.Add(collar,ivory,at+Vector3.up*height*.286f,new Vector3(width*1.17f,height*.075f,width*1.17f));
            p.Add(spire,ivory,at+Vector3.up*height*.322f,new Vector3(width*1.12f,height*.61f,width*1.12f));
            for (int i = 0; i < 4; i++)
            foreach (int side in new[] {-1,1})
            {
                float y = .419f+i*.102f;
                float r = width*(.44f-i*.092f);
                p.Add(bead,rose,at+new Vector3(side*r,height*y,-r*.28f),new Vector3(width*.21f,height*.071f,width*.26f),
                    Quaternion.Euler(0,0,-side*25));
            }
            p.Add(bead,brass,at+Vector3.up*height*.908f,new Vector3(width*.24f,height*.08f,width*.24f));
            p.Add(slab,ivory,at+Vector3.up*height*.972f,new Vector3(width*.08f,height*.115f,width*.08f));
            p.Add(slab,ivory,at+Vector3.up*height*.98f,new Vector3(width*.26f,height*.022f,width*.095f));
        }

        void AddNiche(Pieces p, float x, float y, float z, float width, float height)
        {
            var at = new Vector3(x,y,z);
            var arch = Arch(width*.5f,0,height*.62f,height);
            p.Add(ArchSolid("Shadowed statue niche",arch,.09f),shadow,at,Vector3.one);
            Rim(p,arch,Arch(width*.55f,-.03f,height*.62f,height+.15f),ivory,at+Vector3.back*.12f,.16f);
            p.Add(collar,ivory,at+new Vector3(0,-.11f,-.12f),new Vector3(width*.74f,.18f,width*.63f));
            p.Add(spire,ivory,at+new Vector3(0,.07f,-.18f),new Vector3(width*.54f,height*.65f,width*.39f));
            p.Add(bead,ivory,at+new Vector3(0,height*.64f,-.225f),new Vector3(width*.33f,height*.15f,width*.32f));
        }

        void AddBlindLancet(Pieces p, Vector3 at, float width, float height)
        {
            var recess=Arch(width*.5f,0,height*.57f,height);
            p.Add(ArchSolid("Carved blind lancet",recess,.052f),rose,at,Vector3.one);
            Rim(p,recess,Arch(width*.64f,-.01f,height*.58f,height+.09f),ivory,at+Vector3.back*.038f,.105f);
            p.Add(slab,ivory,at+new Vector3(0,height*.34f,-.055f),new Vector3(.039f,height*.63f,.095f));
        }

        void Moulding(Pieces p,float x,float y,float span,float z)
        {
            p.Add(rounded,rose,new Vector3(x,y,z-.10f),new Vector3(span+.09f,.25f,.43f));
            p.Add(slab,ivory,new Vector3(x,y+.13f,z-.17f),new Vector3(span+.13f,.105f,.49f));
            p.Add(slab,snow,new Vector3(x,y+.201f,z-.16f),new Vector3(span+.10f,.057f,.47f));
        }

        void AddBalustrade(Pieces p,float x,float bottom,float width,float z)
        {
            const float h = .53f;
            p.Add(slab,ivory,new Vector3(x,bottom,z),new Vector3(width,.09f,.135f));
            p.Add(slab,ivory,new Vector3(x,bottom+h,z),new Vector3(width,.09f,.14f));
            int cells = Mathf.RoundToInt(width/.54f);
            float step = width/cells;
            for (int i=0;i<cells;i++)
            {
                float left = x-width/2+i*step;
                var a = new Vector3(left+.07f,bottom+.065f,z);
                var b = new Vector3(left+step-.07f,bottom+.065f,z);
                Member(p,rose,a,b+Vector3.up*(h-.13f),.056f,.10f);
                Member(p,ivory,b,a+Vector3.up*(h-.13f),.052f,.10f);
                p.Add(collar,ivory,new Vector3(left,bottom,z),new Vector3(.10f,h,.114f));
            }
        }

        void RoseWindow(Pieces p, Vector3 center, float radius)
        {
            p.Add(column,shadow,center+Vector3.forward*.18f,new Vector3(radius*2,.13f,radius*2),Quaternion.Euler(90,0,0));
            p.Add(traceryRing,ivory,center,new Vector3(radius,radius,1.70f));
            Quatrefoil(p,center+Vector3.back*.069f,radius*.75f);
            p.Add(bead,ivory,center+new Vector3(0,-.071f,-.13f),new Vector3(.23f,.18f,.15f));
        }

        void Quatrefoil(Pieces p, Vector3 center, float radius)
        {
            for (int i=0;i<4;i++)
            {
                var offset = Quaternion.Euler(0,0,i*90)*Vector3.up*radius*.46f;
                p.Add(traceryRing,ivory,center+offset,new Vector3(radius*.48f,radius*.48f,.65f));
            }
        }

        void AddWreath(Pieces p,Vector3 center,float radius)
        {
            for (int i=0;i<19;i++)
            {
                float a=i*2*Mathf.PI/19;
                var at=center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
                p.Add(bead,i%2==0?green:greenTips,at,new Vector3(.245f,.24f,.20f));
                if(i%4==0)p.Add(bead,berry,at+Vector3.back*.115f,Vector3.one*.125f);
            }
            AddBow(p,center+new Vector3(0,radius*.81f,-.16f),.57f);
        }

        void AddGarland(Pieces p,List<Vector2> arch,float z)
        {
            for (int i=3;i<arch.Count-3;i++)
            {
                var from=arch[i];
                for (int j=0;j<2;j++)
                {
                    var point=InPlane(Vector2.Lerp(from,arch[i+1],j*.5f),z);
                    p.Add(bead,(i+j)%2==0?green:greenTips,point,new Vector3(.278f,.31f,.205f));
                    if(j==0 && i%3==0)p.Add(bead,flame,point+new Vector3(.01f,.06f,-.135f),Vector3.one*.10f);
                }
            }
            AddBow(p,new Vector3(0,arch[(arch.Count+1)/2].y-.13f,z-.10f),1.1f);
        }

        void AddBow(Pieces p, Vector3 center, float size)
        {
            foreach (int side in new[] {-1,1})
            {
                p.Add(rounded,berry,center+new Vector3(side*.22f,.038f,0)*size,new Vector3(.39f,.18f,.13f)*size,
                    Quaternion.Euler(0,0,side*24));
                p.Add(rounded,berry,center+new Vector3(side*.165f,-.156f,.015f)*size,new Vector3(.147f,.35f,.11f)*size,
                    Quaternion.Euler(0,0,-side*26));
            }
            p.Add(bead,berry,center+new Vector3(0,-.102f,-.076f)*size,new Vector3(.18f,.215f,.16f)*size);
        }

        void AddWallAbove(Pieces p, List<Vector2> profile, float top, float z, float depth)
        {
            for (int i=1;i<profile.Count-2;i++)
            {
                Vector2 right=profile[i], left=profile[i+1];
                if (Mathf.Abs(left.x-right.x)<.0001f) continue;
                var shape=new List<Vector2>{left,right,new Vector2(right.x,top),new Vector2(left.x,top)};
                p.Add(ArchSolid("Marble over the pointed opening",shape,depth),wall,new Vector3(0,0,z),Vector3.one);
            }
        }

        static List<Vector2> Arch(float halfWidth,float bottom,float spring,float crown)
        {
            const int arc = 12;
            var outline = new List<Vector2> { new Vector2(-halfWidth,bottom),new Vector2(halfWidth,bottom) };
            for (int i=0;i<=arc*2;i++)
            {
                float t=1-Mathf.Abs(i/(float)arc-1);
                float x=(i<=arc ? 1 : -1)*halfWidth*(1-t*t);
                outline.Add(new Vector2(x,Mathf.Lerp(spring,crown,t)));
            }
            return outline;
        }

        static Mesh ArchSolid(string name,List<Vector2> outline,float depth) => ParkGeometry.Extrude(name,outline,depth);

        static void Rim(Pieces p,List<Vector2> inner,List<Vector2> outer,Material material,float z,float depth) =>
            Rim(p,inner,outer,material,new Vector3(0,0,z),depth);

        static void Rim(Pieces p,List<Vector2> inner,List<Vector2> outer,Material material,Vector3 center,float depth)
        {
            var vertices=new List<Vector3>();
            var triangles=new List<int>();
            // Cap the two jamb feet and trace the curved header with a single extruded ribbon.
            for(int i=1;i<inner.Count;i++)
            {
                int next=(i+1)%inner.Count;
                var a=inner[i];var b=inner[next];var c=outer[next];var d=outer[i];
                float front=-depth/2,back=depth/2;
                Surface(a,b,c,d,front,false);
                Surface(a,b,c,d,back,true);
                Edge(a,b,false); Edge(d,c,true);
            }
            Edge(inner[0],outer[0],false);
            Edge(outer[1],inner[1],false);
            var mesh=new Mesh {name="Carved pointed arch molding",vertices=vertices.ToArray(),triangles=triangles.ToArray()};
            mesh.RecalculateNormals();
            p.Add(mesh,material,center,Vector3.one);

            void Surface(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float z,bool flip) =>
                Quad(InPlane(a,z),InPlane(b,z),InPlane(c,z),InPlane(d,z),flip);
            void Edge(Vector2 a,Vector2 b,bool flip) => Quad(InPlane(a,-depth/2),InPlane(a,depth/2),
                InPlane(b,depth/2),InPlane(b,-depth/2),flip);
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool flip)
            {
                int i=vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                if (flip) triangles.AddRange(new[] {i,i+2,i+1,i,i+3,i+2});
                else triangles.AddRange(new[] {i,i+1,i+2,i,i+2,i+3});
            }
        }

        void Member(Pieces p,Material material,Vector3 from,Vector3 to,float width,float depth)
        {
            var axis=to-from;
            p.Add(slab,material,(from+to)/2,new Vector3(width,axis.magnitude,depth),Quaternion.FromToRotation(Vector3.up,axis));
        }

        Renderer AddGlow(Transform parent,string name,Material material,Vector3 center,Vector2 size,bool floor=false)
        {
            var glow=ParkGeometry.Place(name,glimmerPlane,parent,center,new Vector3(size.x,size.y,1),material);
            if(floor)glow.localRotation=Quaternion.Euler(90,0,0);
            var renderer=glow.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            return renderer;
        }

        static Vector3 InPlane(Vector2 p,float z) => new Vector3(p.x,p.y,z);

        static Material Stone(string name,string tint,string shade)
        {
            var material=Paint(name,tint);
            material.SetColor("_Shade",ColorFrom(shade));
            material.SetFloat("_Variation",.036f);
            return material;
        }

        static Material Interior(string name,string color)
        {
            var material=new Material(Shader.Find("Standard")){name=name,color=ColorFrom(color)};
            material.SetFloat("_Glossiness",.14f);
            return material;
        }

        static Material Solid(string name,string color) => new Material(Shader.Find("Unlit/Color")){name=name,color=ColorFrom(color)};

        static Material LancetGlass()
        {
            var material=new Material(Shader.Find("PeacePark/Cathedral Glass")){name="Glowing diamond-leaded glass",color=ColorFrom("#FFD47B")};
            material.SetColor("_EdgeColor",ColorFrom("#D77A3D"));
            return material;
        }

        static Mesh GlassPane(List<Vector2> shape, float halfWidth, float height)
        {
            var vertices=new Vector3[shape.Count];
            var uv=new Vector2[shape.Count];
            var triangles=new int[(shape.Count-2)*3];
            for(int i=0;i<shape.Count;i++)
            {
                vertices[i]=InPlane(shape[i],0);
                uv[i]=new Vector2(.5f+shape[i].x/(halfWidth*2),shape[i].y/height);
            }
            for(int i=1;i<shape.Count-1;i++)
            {
                int j=(i-1)*3;
                triangles[j]=0;triangles[j+1]=i+1;triangles[j+2]=i;
            }
            var mesh=new Mesh {name="Pointed leaded-glass insert",vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals();
            return mesh;
        }

        static Material Halo(string name,string color,float strength)
        {
            var material=new Material(Shader.Find("PeacePark/Cathedral Glow")){name=name,color=ColorFrom(color)};
            material.SetFloat("_Strength",strength);
            return material;
        }

        static Color ColorFrom(string hex)
        {
            ColorUtility.TryParseHtmlString(hex,out var color);
            return color;
        }

        static Mesh SpireMesh(string name,params Vector2[] profile)
        {
            const int sectors=8;
            var vertices=new List<Vector3>();
            var triangles=new List<int>();
            for(int layer=0;layer<profile.Length-1;layer++)
            for(int i=0;i<sectors;i++)
            {
                int start=vertices.Count;
                vertices.Add(Point(layer,i));vertices.Add(Point(layer+1,i));
                vertices.Add(Point(layer+1,i+1));vertices.Add(Point(layer,i+1));
                triangles.AddRange(new[] {start,start+1,start+2,start,start+2,start+3});
            }
            var mesh=new Mesh {name=name,vertices=vertices.ToArray(),triangles=triangles.ToArray()};
            mesh.RecalculateNormals();
            return mesh;

            Vector3 Point(int layer,int i)
            {
                var ring=profile[layer];
                float angle=(i+.5f)*2*Mathf.PI/sectors;
                return new Vector3(Mathf.Cos(angle)*ring.x,ring.y,Mathf.Sin(angle)*ring.x);
            }
        }

        static Mesh Annulus(string name,float inner,float outer,float depth,int steps)
        {
            var p=new List<Vector3>();
            var t=new List<int>();
            for(int i=0;i<steps;i++)
            {
                float a=i*2*Mathf.PI/steps,b=(i+1)*2*Mathf.PI/steps;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                float z=depth/2;
                Surface(u*inner,v*inner,v*outer,u*outer,-z);
                Surface(u*outer,v*outer,v*inner,u*inner,z);
                Quad(InPlane(u*outer,-z),InPlane(v*outer,-z),InPlane(v*outer,z),InPlane(u*outer,z));
                Quad(InPlane(u*inner,z),InPlane(v*inner,z),InPlane(v*inner,-z),InPlane(u*inner,-z));
            }
            var mesh=new Mesh {name=name,vertices=p.ToArray(),triangles=t.ToArray()};
            mesh.RecalculateNormals();
            return mesh;

            void Surface(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float z) => Quad(InPlane(a,z),InPlane(b,z),InPlane(c,z),InPlane(d,z));
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int i=p.Count;
                p.Add(a);p.Add(b);p.Add(c);p.Add(d);
                t.AddRange(new[] {i,i+1,i+2,i,i+2,i+3});
            }
        }
    }
}
