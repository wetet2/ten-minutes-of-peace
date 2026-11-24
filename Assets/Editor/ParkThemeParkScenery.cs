using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkThemeParkSetup
    {
        readonly Mesh flowerSphere=SmallSphere();

        static Mesh SmallSphere()
        {
            const int rings=6,sides=10;
            var vertices=new List<Vector3>();
            var normals=new List<Vector3>();
            var triangles=new List<int>();
            for(int ring=0;ring<=rings;ring++)
            {
                float latitude=Mathf.PI*ring/rings;
                for(int side=0;side<sides;side++)
                {
                    float angle=2*Mathf.PI*side/sides;
                    var normal=new Vector3(Mathf.Cos(angle)*Mathf.Sin(latitude),Mathf.Cos(latitude),Mathf.Sin(angle)*Mathf.Sin(latitude));
                    vertices.Add(normal*.5f);normals.Add(normal);
                    if(ring==rings)continue;
                    int a=ring*sides+side,b=ring*sides+(side+1)%sides;
                    if(ring>0)triangles.AddRange(new[]{a,b,a+sides});
                    if(ring+1<rings)triangles.AddRange(new[]{b,b+sides,a+sides});
                }
            }
            var mesh=new Mesh{name="Small blossom mesh"};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            return mesh;
        }

        void MakeLandscaping(Transform root)
        {
            var gardens=Group("Flower beds and park decorations",root,Vector3.zero);
            var treeGroup=Group("Garden tree models",gardens,Vector3.zero);
            var treeModels=ParkNaturePack.LoadGardenTrees();
            var treeMaterials=new Dictionary<Material,Material>();
            var flowerColors=new[]{yellow,coral,pink,cream,blue};
            MakeFlowerBeds(gardens,flowerColors);
            var treeSpots=new[]{new Vector3(-9.8f,0,3.5f),new Vector3(8.9f,0,5.9f),new Vector3(7.45f,0,-2.75f),
                new Vector3(-25.7f,0,13.2f),new Vector3(-30.5f,0,7.75f),new Vector3(34.7f,0,17.0f),new Vector3(-39.65f,0,9),
                new Vector3(37.4f,0,-14.5f),new Vector3(-11,0,15),new Vector3(3,0,14.5f),new Vector3(11,0,14),
                new Vector3(-46.8f,0,-33),new Vector3(41.4f,0,-35.6f),new Vector3(-34,0,12.6f),new Vector3(51.4f,0,20.9f),
                new Vector3(-26,0,34.4f),new Vector3(37.8f,0,31.4f),new Vector3(-7.75f,0,35.0f)};
            for(int i=0;i<treeSpots.Length;i++)
                PlantTree(i,treeSpots[i],4f+i%4*.46f);
            for(int i=0;i<32;i++)
            {
                float distance=(i+.5f)*ParkElephantTrain.Length/32;
                var rail=ParkElephantTrain.TrackPoint(distance);
                var tangent=ParkElephantTrain.TrackPoint(distance+.25f)-ParkElephantTrain.TrackPoint(distance-.25f);
                var outside=Vector3.Cross(Vector3.up,tangent).normalized;
                var spot=rail+outside*(i%3==0?12.5f:9f);
                if(spot.z<-38&&Mathf.Abs(spot.x)<22)continue;
                spot.y=0;
                PlantTree(i+treeSpots.Length,spot,4.65f+i%5*.64f);
            }
            MakeSurroundingScenery(root,treeModels,treeMaterials,flowerColors);
            Bunting(root,new Vector3(-14,0,6),new Vector3(-4,0,8),flowerColors);
            Bunting(root,new Vector3(-4,0,12),new Vector3(6,0,12),flowerColors);
            Bunting(root,new Vector3(-43,0,-13),new Vector3(-36,0,-15),flowerColors);
            Bunting(root,new Vector3(24,0,-16),new Vector3(38,0,-16),flowerColors);
            var perimeter=Group("Picket fence around the park",root,Vector3.zero);
            FenceSection("Back",new Vector3(-66,0,37),new Vector3(66,0,37));
            FenceSection("Left",new Vector3(-66,0,-43),new Vector3(-66,0,37));
            FenceSection("Right",new Vector3(66,0,-43),new Vector3(66,0,37));
            FenceSection("Front left",new Vector3(-66,0,-43),new Vector3(-12,0,-43));
            FenceSection("Front right",new Vector3(12,0,-43),new Vector3(66,0,-43));
            MakeBalloonCart(root,new Vector3(4.15f,.02f,11.25f));
            MakeBalloonCart(root,new Vector3(23.9f,.02f,-37.3f));
            var benches=new Pieces();
            foreach(float x in new[]{-34.7f,-16.3f,-7.5f,9.3f,22.7f,34.8f})
            {
                benches.Add(box,wood,new Vector3(x,.6f,-36.85f),new Vector3(2.03f,.17f,.68f));
                benches.Add(box,wood,new Vector3(x,1.04f,-37.16f),new Vector3(2.03f,.60f,.14f));
                foreach(float offset in new[]{-.73f,.73f})
                    benches.Add(box,ink,new Vector3(x+offset,.32f,-36.85f),new Vector3(.12f,.61f,.59f));
            }
            benches.Build(Group("Park benches",root,Vector3.zero));

            void PlantTree(int index,Vector3 spot,float height)
            {
                if(ParkElephantTrain.WithinTrackLoop(spot,margin:2f))return;
                treeModels.Plant(treeGroup,index,height,height*.98f,spot,index*137.5f,treeMaterials);
            }
            void FenceSection(string name,Vector3 start,Vector3 end)
            {
                var fence=new Pieces();
                Fence(fence,start,end,cream);
                fence.Build(Group(name,perimeter,Vector3.zero));
            }
        }

        void MakeFlowerBeds(Transform gardens,Material[] flowerColors)
        {
            var plaza=Group("Central plaza flower beds",gardens,Vector3.zero);
            var entrance=Group("Entrance flower beds",gardens,Vector3.zero);
            var parts=new Pieces();
            parts.Add(cylinder,paving,new Vector3(0,.11f,0),new Vector3(4.24f,.11f,2.65f));
            parts.Add(cylinder,leaf,new Vector3(0,.21f,0),new Vector3(4.03f,.10f,2.43f));
            for(int i=0;i<21;i++)
            {
                float angle=i*2.39996f;
                float radius=Mathf.Sqrt((i+.6f)/21);
                var center=new Vector3(Mathf.Cos(angle)*1.76f*radius,.50f+.13f*Mathf.Sin(i*3.12f),Mathf.Sin(angle)*1.03f*radius);
                parts.Beam(cylinder,leaf,new Vector3(center.x,.23f,center.z),center,.035f);
                for(int petal=0;petal<5;petal++)
                {
                    float a=petal*Mathf.PI*2/5;
                    parts.Add(flowerSphere,flowerColors[i%flowerColors.Length],
                        center+new Vector3(.134f*Mathf.Cos(a),0,.134f*Mathf.Sin(a)),new Vector3(.22f,.12f,.22f));
                }
                parts.Add(flowerSphere,yellow,center+Vector3.up*.026f,Vector3.one*.145f);
            }

            // Match each bed to its partner on the opposite side of the open central aisle.
            for(int row=0;row<3;row++)
                PlacePair(plaza,9.4f,-7.8f+row*8f);
            for(int pair=0;pair<2;pair++)
                PlacePair(entrance,17+pair*12f,-39.8f);

            void PlacePair(Transform parent,float offset,float depth)
            {
                parts.Build(Group("West flower bed",parent,new Vector3(-offset,0,depth)));
                parts.Build(Group("East flower bed",parent,new Vector3(offset,0,depth)));
            }
        }

        void MakeSurroundingScenery(Transform root,ParkNaturePack.TreeSet trees,
            Dictionary<Material,Material> treeMaterials,Material[] flowers)
        {
            var landscape=Group("Hills and gardens around the park",root,Vector3.zero);
            var pieces=new Pieces();
            var near=new[]{Paint("Orchard greens","#9FC976"),Paint("Green hills","#91BB6A"),
                Paint("Sage hills","#A9CB87")};
            var far=new[]{Paint("Blue-green ridges","#87AB98"),Paint("Sage ridges","#92B8A4")};
            var white=Paint("Soft white clouds","#FFF9EB");
            white.SetColor("_Shade",new Color(.90f,.93f,.99f));
            white.SetFloat("_ShadowFloor",1);
            var random=new System.Random(645);
            Vector3 Ring(float angle,float horizontal,float depth,float y=0) =>
                new Vector3(Mathf.Sin(angle)*horizontal,y,ParkElephantTrain.CenterZ+Mathf.Cos(angle)*depth);
            float Variety(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
            const float fullTurn=2*Mathf.PI;
            for(int i=0;i<24;i++)
            {
                float angle=(i+Variety(-.23f,.23f))*fullTurn/24;
                var turn=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
                pieces.Add(sphere,near[i%near.Length],Ring(angle,Variety(88,98),Variety(58,68),-3),
                    new Vector3(Variety(29,43),Variety(14,25),Variety(22,33)),turn);
                pieces.Add(sphere,far[i%far.Length],Ring(angle+.12f,Variety(126,142),Variety(94,109),-6.2f),
                    new Vector3(Variety(47,62),Variety(26,42),Variety(38,54)),turn);
            }
            for(int i=0;i<16;i++)
            {
                float angle=(i+Variety(-.22f,.22f))*fullTurn/16;
                var turn=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
                var p=Ring(angle,Variety(99,116),Variety(68,87),Variety(19,30));
                float size=Variety(.85f,1.7f);
                pieces.Add(sphere,white,p,new Vector3(10,2.6f,4.2f)*size,turn);
                pieces.Add(sphere,white,p+turn*new Vector3(-1.65f,1.25f,0)*size,new Vector3(4.9f,3.7f,4)*size,turn);
                pieces.Add(sphere,white,p+turn*new Vector3(1.7f,.70f,0)*size,new Vector3(4.6f,3.15f,3.5f)*size,turn);
            }
            pieces.Build(landscape);

            var groves=Group("Surrounding garden groves",landscape,Vector3.zero);
            var borders=new Pieces();
            for(int i=0;i<36;i++)
            {
                float angle=(i+Variety(-.20f,.20f))*fullTurn/36;
                float xRadius=Variety(70,77), zRadius=Variety(44,51);
                var p=Ring(angle,xRadius,zRadius);
                bool centerEntrance=p.z< -43&&Mathf.Abs(p.x)<15;
                if(!centerEntrance)
                {
                    var second=Ring(angle+.044f,xRadius+3,zRadius+3);
                    float height=Variety(5.0f,8.4f);
                    trees.Plant(groves,i,height,height*.98f,p,Variety(0,360),treeMaterials);
                    trees.Plant(groves,i+4,height*.73f,height*.75f,second,Variety(0,360),treeMaterials);
                }
                if(centerEntrance)continue;
                MeadowFlowers(Ring(angle-.055f,xRadius-4,zRadius-3),i);
            }
            for(int side=-1;side<=1;side+=2)
            {
                trees.Plant(groves,side<0?5:11,5.85f,5.8f,new Vector3(side*12,0,-51.5f),side*81,treeMaterials);
                for(int bed=0;bed<3;bed++)
                    MeadowFlowers(new Vector3(side*(7+bed*3.6f),0,-49-bed*2.7f),bed+3);
            }
            borders.Build(Group("Flowering meadow borders",landscape,Vector3.zero));

            void MeadowFlowers(Vector3 drift,int palette)
            {
                for(int shrub=0;shrub<4;shrub++)
                {
                    var offset=new Vector3(Variety(-1.9f,1.9f),0,Variety(-1.55f,1.55f));
                    borders.Add(flowerSphere,shrub%2==0?leaf:lightLeaf,drift+offset+Vector3.up*.58f,
                        new Vector3(Variety(1.25f,2.1f),Variety(.9f,1.45f),Variety(1.3f,1.9f)));
                }
                for(int flower=0;flower<9;flower++)
                {
                    var spot=drift+new Vector3(Variety(-2.0f,2f),.33f,Variety(-2.3f,2.3f));
                    borders.Beam(cylinder,leaf,spot-Vector3.up*.31f,spot,.044f);
                    for(int petal=0;petal<5;petal++)
                    {
                        float a=petal*fullTurn/5;
                        borders.Add(flowerSphere,flowers[(palette+flower)%flowers.Length],
                            spot+new Vector3(.14f*Mathf.Cos(a),0,.14f*Mathf.Sin(a)),new Vector3(.23f,.12f,.23f));
                    }
                    borders.Add(flowerSphere,yellow,spot+Vector3.up*.03f,Vector3.one*.15f);
                }
            }
        }

        void Bunting(Transform root,Vector3 from,Vector3 to,Material[] colors)
        {
            var poles=Group("Festival bunting",root,Vector3.zero);
            var pieces=new Pieces();
            foreach(var post in new[]{from,to})
            {
                pieces.Add(cylinder,wood,post+Vector3.up*2.03f,new Vector3(.17f,2.03f,.17f));
                pieces.Add(sphere,yellow,post+Vector3.up*4.11f,Vector3.one*.24f);
            }
            Vector3 Cord(float t)=>Vector3.Lerp(from,to,t)+Vector3.up*(4.03f-3.40f*t*(1-t));
            var flag=ParkGeometry.Extrude("Triangular pennant",new List<Vector2>{
                new Vector2(-.29f,0),new Vector2(0,-.58f),new Vector2(.29f,0)},.025f);
            for(int i=0;i<12;i++)
            {
                var a=Cord(i/12f);var b=Cord((i+1)/12f);
                pieces.Beam(cylinder,wood,a,b,.024f);
                pieces.Add(flag,colors[i%colors.Length],(a+b)*.5f,Vector3.one,
                    Quaternion.Euler(10,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg));
            }
            pieces.Build(poles);
        }

        void MakeBalloonCart(Transform root,Vector3 location)
        {
            var cart=Group("Popcorn and balloon cart",root,location);
            var parts=new Pieces();
            parts.Add(box,coral,new Vector3(0,.57f,0),new Vector3(1.70f,.90f,.99f));
            parts.Add(box,cream,new Vector3(0,1.03f,0),new Vector3(1.81f,.18f,1.13f));
            foreach(float x in new[]{-.6f,.6f})
            {
                parts.Add(cylinder,wood,new Vector3(x,.35f,-.57f),new Vector3(.50f,.085f,.50f),Quaternion.Euler(90,0,0));
                parts.Add(box,wood,new Vector3(x,1.78f,.37f),new Vector3(.07f,1.62f,.08f));
            }
            for(int i=0;i<6;i++)
            {
                parts.Add(box,i%2==0?coral:cream,new Vector3((i-2.5f)*.31f,2.55f,.08f),new Vector3(.31f,.10f,1.15f),Quaternion.Euler(-12,0,0));
                parts.Add(sphere,yellow,new Vector3((i-2.5f)*.21f,1.15f,.0f),Vector3.one*.19f);
            }
            var colors=new[]{blue,coral,yellow,pink,mint};
            for(int i=0;i<5;i++)
            {
                var p=new Vector3(1.25f+.32f*Mathf.Cos(i*2.4f),2.38f+.32f*(i%3),.50f+.25f*Mathf.Sin(i*2.4f));
                parts.Add(sphere,colors[i],p,new Vector3(.56f,.76f,.52f));
                parts.Beam(cylinder,cream,new Vector3(.75f,1.02f,.25f),p-Vector3.up*.35f,.017f);
            }
            parts.Build(cart);
        }

        ParkThemeParkWheel MakeFerrisWheel(Transform root)
        {
            const float rimDepth=.66f;
            const float cabinBearingZ=1.46f;
            var rimFront=Vector3.back*rimDepth;
            var rimBack=Vector3.forward*rimDepth;
            var ride=Group("Grand Ferris wheel",root,WheelLocation);
            var center=new Vector3(0,ParkThemeParkWheel.HubHeight,0);
            var front=Vector3.forward*ParkThemeParkWheel.CabinOffset;
            var legs=new Pieces();
            legs.Add(box,paving,new Vector3(0,.11f,-.32f),new Vector3(24.8f,.22f,10.15f));
            legs.Add(box,cream,new Vector3(0,.191f,-.32f),new Vector3(24.83f,.043f,10.17f));
            var frontEdge=front.z-2.72f;
            Fence(legs,new Vector3(-11.85f,0,frontEdge),new Vector3(-2.24f,0,frontEdge),cream);
            Fence(legs,new Vector3(2.24f,0,frontEdge),new Vector3(11.85f,0,frontEdge),cream);
            foreach(float x in new[]{-11.85f,11.85f})
                Fence(legs,new Vector3(x,0,frontEdge),new Vector3(x,0,4.2f),cream);
            foreach(float x in new[]{-10.2f,10.2f})
            {
                foreach(float z in new[]{1.48f,3.16f})
                {
                    legs.Add(box,blue,new Vector3(x,.25f,z),new Vector3(1.62f,.50f,1.62f));
                    legs.Beam(cylinder,cream,new Vector3(x,.31f,z),center+Vector3.forward*z,.77f);
                }
                var lower=Vector3.Lerp(new Vector3(x,.31f,1.48f),center+Vector3.forward*1.48f,.30f);
                var upper=Vector3.Lerp(new Vector3(x,.31f,1.48f),center+Vector3.forward*1.48f,.62f);
                var span=Vector3.forward*(3.16f-1.48f);
                legs.Beam(cylinder,steel,lower,lower+span,.20f);
                legs.Beam(cylinder,steel,upper,upper+span,.20f);
                legs.Beam(cylinder,steel,lower,upper+span,.14f);
            }
            legs.Beam(cylinder,coral,center-Vector3.forward*1.05f,center+Vector3.forward*3.47f,.79f);
            foreach(float z in new[]{1.48f,3.16f})
                legs.Add(sphere,steel,center+Vector3.forward*z,Vector3.one*1.08f);
            legs.Add(box,blue,front+new Vector3(0,.153f,-3.02f),new Vector3(2.80f,.306f,1.30f));
            legs.Add(box,mint,front+new Vector3(0,.305f,-1.87f),new Vector3(2.88f,.61f,1.08f));
            legs.Add(box,cream,front+new Vector3(0,.300f,-3.02f),new Vector3(2.64f,.025f,1.18f));
            legs.Add(box,cream,front+new Vector3(0,.613f,-1.87f),new Vector3(2.77f,.035f,1.04f));
            foreach(int side in new[]{-1,1})
            {
                var x=side*1.64f;
                legs.Beam(cylinder,mint,front+new Vector3(x,.62f,-3.6f),front+new Vector3(x,1.22f,-1.46f),.12f);
                legs.Beam(cylinder,mint,front+new Vector3(x,.04f,-3.6f),front+new Vector3(x,.62f,-3.6f),.12f);
                legs.Beam(cylinder,mint,front+new Vector3(x,.56f,-1.46f),front+new Vector3(x,1.22f,-1.46f),.12f);
            }
            legs.Build(ride);
            var pivot=Group("Grand Ferris wheel rotor",ride,center);
            var frame=new Pieces();
            var trusses=new Pieces();
            var cabins=new Transform[8];
            var colors=new[]{coral,yellow,mint,blue,pink,yellow,mint,blue};
            Vector3 Point(int i,int count)
            {
                float angle=i*Mathf.PI*2/count-Mathf.PI/2;
                return new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*ParkThemeParkWheel.Radius;
            }
            const int rimSegments=128;
            for(int i=0;i<rimSegments;i++)
            {
                var first=Point(i,rimSegments);
                var next=Point(i+1,rimSegments);
                frame.Beam(cylinder,coral,first+rimFront,next+rimFront,.28f);
                frame.Beam(cylinder,coral,first+rimBack,next+rimBack,.28f);
                if(i%4!=0)continue;
                var across=Point(i+4,rimSegments);
                frame.Beam(cylinder,cream,first+rimFront,first+rimBack,.15f);
                frame.Beam(cylinder,steel,first+rimFront,across+rimBack,.10f);
            }
            var planes=new[]{rimFront,rimBack};
            for(int i=0;i<cabins.Length;i++)
            {
                var point=Point(i,cabins.Length);
                var next=Point(i+1,cabins.Length);
                var outerNode=Point(2*i+1,2*cabins.Length);
                foreach(var plane in planes)
                {
                    var innerNode=point*.59f+plane;
                    var nextInnerNode=next*.59f+plane;
                    trusses.Beam(cylinder,cream,plane,point+plane,.26f);
                    trusses.Beam(cylinder,cream,innerNode,outerNode+plane,.15f);
                    trusses.Beam(cylinder,cream,nextInnerNode,outerNode+plane,.15f);
                    trusses.Add(flowerSphere,steel,innerNode,Vector3.one*.37f);
                    trusses.Add(flowerSphere,coral,outerNode+plane,Vector3.one*.36f);
                    trusses.Add(flowerSphere,yellow,point+plane,Vector3.one*.44f);
                }
                var bearing=point+front+Vector3.forward*cabinBearingZ;
                frame.Beam(cylinder,steel,point+rimBack,bearing,.34f);
                var cabin=cabins[i]=Group("Rideable gondola "+(i+1),pivot,point+front);
                cabin.localScale=new Vector3(1.17f,1,1);
                FerrisCabin(cabin,colors[i],cabinBearingZ);
            }
            frame.Add(cylinder,yellow,Vector3.zero,new Vector3(3.6f,.85f,3.6f),Quaternion.Euler(90,0,0));
            frame.Add(sphere,cream,new Vector3(0,0,-.92f),Vector3.one*1.15f);
            for(int i=0;i<12;i++)
                frame.Add(flowerSphere,cream,Point(i,12)*(1.43f/ParkThemeParkWheel.Radius)+Vector3.back*.86f,Vector3.one*.20f);
            frame.Build(pivot);
            trusses.Build(Group("Connected wheel spokes and braces",pivot,Vector3.zero));
            var controller=ride.gameObject.AddComponent<ParkThemeParkWheel>();
            controller.Configure(pivot,cabins);
            return controller;
        }

        void FerrisCabin(Transform cabin,Material color,float bearingZ)
        {
            float floor=ParkThemeParkWheel.CabinFloor;
            const float roof=2.40f;
            var body=new Pieces();
            body.Add(box,color,new Vector3(0,floor-.11f,0),new Vector3(3.16f,.22f,2.82f));
            body.Add(box,cream,new Vector3(0,floor+.02f,0),new Vector3(2.92f,.04f,2.60f));
            body.Add(box,color,new Vector3(0,floor+.38f,1.27f),new Vector3(3.1f,.77f,.25f));
            body.Add(box,cream,new Vector3(0,floor+.15f,.66f),new Vector3(2.27f,.30f,.91f));
            body.Add(box,cream,new Vector3(0,floor+.77f,1.05f),new Vector3(2.27f,.60f,.19f));
            body.Add(box,color,new Vector3(0,roof,0),new Vector3(3.36f,.24f,2.99f));
            body.Add(box,cream,new Vector3(0,roof+.13f,0),new Vector3(3.08f,.13f,2.71f));
            var bearing=new Vector3(0,0,bearingZ);
            body.Add(cylinder,steel,bearing,new Vector3(.53f,.11f,.53f),Quaternion.Euler(90,0,0));
            foreach(float x in new[]{-.68f,.68f})
            {
                body.Beam(cylinder,steel,new Vector3(x,floor+.14f,1.37f),bearing,.15f);
                body.Beam(cylinder,steel,bearing,new Vector3(x*.6f,roof,1.42f),.15f);
            }
            foreach(float x in new[]{-1.11f,1.11f})
                body.Add(flowerSphere,yellow,new Vector3(x,floor+.44f,-1.385f),new Vector3(.19f,.19f,.08f));
            foreach(float x in new[]{-1.47f,1.47f})
            {
                body.Add(box,color,new Vector3(x,floor+.42f,0),new Vector3(.24f,.80f,2.79f));
                foreach(float z in new[]{-1.24f,1.22f})
                    body.Beam(cylinder,cream,new Vector3(x,floor+.68f,z),new Vector3(x,roof-.02f,z),.095f);
                body.Add(box,color,new Vector3(x*.86f,floor+.42f,-1.26f),new Vector3(.56f,.80f,.22f));
            }
            body.Build(cabin);
        }

        ParkCarouselRide MakeCarousel(Transform root)
        {
            const float deckHeight=.46f, eaveHeight=4.4f, peakHeight=8.65f, roofRadius=3.83f;
            const int panels=12, slicesPerPanel=5, rings=12, slices=panels*slicesPerPanel;
            var carousel=Group("Grand carousel",root,CarouselLocation);
            carousel.localScale=new Vector3(3.52f,2.5f,3.52f);
            var shell=new Pieces();
            shell.Add(cylinder,coral,new Vector3(0,.15f,0),new Vector3(7.75f,.15f,7.75f));
            shell.Add(cylinder,yellow,new Vector3(0,.255f,0),new Vector3(7.78f,.035f,7.78f));
            shell.Add(cylinder,coral,new Vector3(0,eaveHeight-.15f,0),new Vector3(7.67f,.16f,7.67f));
            foreach(float trimHeight in new[]{eaveHeight-.30f,eaveHeight+.02f})
                shell.Add(cylinder,yellow,new Vector3(0,trimHeight,0),new Vector3(7.73f,.045f,7.73f));
            var lining=Paint("Ivory canopy lining","#F4DFC4");
            lining.SetColor("_Shade",new Color(.91f,.82f,.72f));
            lining.SetFloat("_ShadowFloor",.90f);
            shell.Add(cylinder,lining,new Vector3(0,eaveHeight-.353f,0),new Vector3(7.54f,.019f,7.54f));
            shell.Add(cylinder,yellow,new Vector3(0,peakHeight+.10f,0),new Vector3(.25f,.23f,.25f));
            shell.Add(sphere,yellow,new Vector3(0,peakHeight+.34f,0),Vector3.one*.37f);
            shell.Add(box,paving,new Vector3(0,.085f,-4.20f),new Vector3(1.66f,.17f,.87f));
            shell.Add(box,cream,new Vector3(0,.20f,-4.00f),new Vector3(1.66f,.14f,.76f));
            Vector3 RoofPoint(int ring,int slice)
            {
                float r=(float)ring/rings,angle=slice*Mathf.PI*2/slices;
                float y=Mathf.Lerp(peakHeight,eaveHeight,Mathf.Pow(r,.69f));
                return new Vector3(Mathf.Cos(angle)*roofRadius*r,y,Mathf.Sin(angle)*roofRadius*r);
            }
            for(int panel=0;panel<panels;panel++)
            {
                var vertices=new List<Vector3>();
                var triangles=new List<int>();
                const int rowLength=slicesPerPanel+1;
                for(int r=0;r<=rings;r++)
                for(int i=0;i<=slicesPerPanel;i++)
                {
                    int n=vertices.Count;
                    vertices.Add(RoofPoint(r,panel*slicesPerPanel+i));
                    if(r==rings||i==slicesPerPanel)continue;
                    if(r>0)triangles.AddRange(new[]{n,n+1,n+rowLength});
                    triangles.AddRange(new[]{n+1,n+rowLength+1,n+rowLength});
                }
                var mesh=new Mesh{name="Striped carousel roof panel"};
                mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                shell.Add(mesh,panel%2==0?coral:cream,Vector3.zero,Vector3.one);
                int seam=panel*slicesPerPanel;
                for(int r=1;r<rings;r++)
                    shell.Beam(cylinder,yellow,RoofPoint(r,seam)+Vector3.up*.016f,RoofPoint(r+1,seam)+Vector3.up*.016f,.026f);
                var outward=RoofPoint(rings,seam);
                outward.y=0;
                var liningHeight=Vector3.up*(eaveHeight-.379f);
                shell.Beam(cylinder,cream,outward*.20f+liningHeight,outward*.98f+liningHeight,.048f);
            }
            for(int i=0;i<slices;i++)
            {
                float a=i*Mathf.PI*2/slices;
                var rim=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                shell.Add(flowerSphere,i%5==0?yellow:cream,rim*3.82f+Vector3.up*(eaveHeight-.17f),Vector3.one*.14f);
            }
            shell.Build(carousel);

            var moving=Group("Turning carousel platform",carousel,Vector3.zero);
            var riders=new Pieces();
            riders.Add(cylinder,coral,new Vector3(0,.30f,0),new Vector3(7.14f,.12f,7.14f));
            riders.Add(cylinder,cream,new Vector3(0,deckHeight-.045f,0),new Vector3(7.01f,.045f,7.01f));
            riders.Add(cylinder,coral,Vector3.up*((eaveHeight+.36f)*.5f),new Vector3(1.36f,(eaveHeight-.36f)*.5f,1.36f));
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*2/8;
                var post=new Vector3(Mathf.Cos(a)*.69f,0,Mathf.Sin(a)*.69f);
                riders.Beam(cylinder,cream,post+Vector3.up*.42f,post+Vector3.up*(eaveHeight-.12f),.10f);
            }
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48;
                riders.Add(flowerSphere,i%2==0?yellow:pink,new Vector3(3.57f*Mathf.Cos(a),.31f,3.57f*Mathf.Sin(a)),Vector3.one*.13f);
            }
            var seats=AddCarouselHorses(moving,deckHeight,eaveHeight,out var lifts);
            riders.Build(moving);
            var spin=moving.gameObject.AddComponent<ParkRoundaboutSpin>();
            spin.ReverseDirection=true;
            var ride=carousel.gameObject.AddComponent<ParkCarouselRide>();
            ride.Configure(spin,seats,deckHeight,lifts);
            return ride;
        }
    }
}
