using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkPlaygroundNeighborhood
    {
        internal static void Create(Transform venue,float halfWidth,float halfDepth,Mesh box,Mesh sphere)
        {
            var neighborhood=Group("Playground neighborhood",venue,Vector3.zero);
            var slab=PrimitiveMesh(PrimitiveType.Cube);
            var cylinder=PrimitiveMesh(PrimitiveType.Cylinder);
            var cream=Paint("Warm ivory trim","#FFF2DA");
            var timber=Paint("Honey timber","#B57C50");
            var blueGlass=Paint("Powder blue window glass","#82BEC4");
            var pavement=Paint("Sandstone sidewalk","#E3D6BF");
            var joints=Paint("Warm sidewalk joints","#CABBA2");
            var greens=new[]{Paint("Street tree light green","#A4C753"),Paint("Street tree leaf green","#7FAB56"),
                Paint("Street tree shaded green","#69984F")};
            var canopy=ParkGeometry.Canopy();
            var trunk=ParkGeometry.Trunk();
            var metal=Paint("Jade lamp posts","#457675");
            var lamp=Paint("Warm lantern glass","#FFE6A3");

            var street=Group("Rear pedestrian lane",neighborhood,Vector3.zero);
            ParkGeometry.Place("Rear paving",slab,street,new Vector3(0,.041f,halfDepth+2.01f),
                new Vector3(halfWidth*2+12.0f,.082f,2.65f),pavement);
            ParkGeometry.Place("Rear curb",slab,street,new Vector3(0,.062f,halfDepth+.72f),
                new Vector3(halfWidth*2+12.0f,.124f,.21f),cream);
            ParkGeometry.Place("Shopfront pavement",slab,street,new Vector3(0,.031f,halfDepth+4.14f),
                new Vector3(halfWidth*2+12.0f,.062f,1.68f),joints);

            var buildings=Group("Low village buildings",neighborhood,Vector3.zero);
            var roof=ParkGeometry.Roof();
            var doorway=ParkGeometry.Arch(1.07f,1.81f,.12f);
            var peachWall=Paint("Apricot plaster","#ECA987");
            var sageWall=Paint("Sage-painted plaster","#ACCCAE");
            var clayRoof=Paint("Terracotta roof","#AA6B50");
            var slateRoof=Paint("Slate blue roof","#6A8F9F");
            BuildHouse(buildings,"Peach house",new Vector3(-15.4f,0,halfDepth+6.68f),-13f,peachWall,clayRoof,-1);
            BuildHouse(buildings,"Sage house",new Vector3(15.0f,0,halfDepth+7.10f),11f,sageWall,slateRoof,1);
            BuildShop();

            // The front crossings land on the far side of the loop road. Set the houses
            // back from this sidewalk so their windows face the park across small lawns.
            var front=Group("Front residential street",neighborhood,Vector3.zero);
            var frontPaths=new Pieces();
            const float sidewalkZ=-16.75f;
            frontPaths.Add(slab,pavement,new Vector3(0,.035f,sidewalkZ),new Vector3(35,.07f,1.9f));
            FrontHouse("Honey cottage",-15,-22.7f,170,Paint("Honey plaster","#EACB95"),slateRoof);
            FrontHouse("Rose cottage",-2.6f,-25.4f,186,peachWall,clayRoof);
            FrontHouse("Garden cottage",13,-21.8f,173,sageWall,clayRoof);
            frontPaths.Build(Group("Front pavement and garden paths",front,Vector3.zero));

            var frontTrees=new Pieces();
            foreach(var point in new[]{new Vector3(-20,0,-20),new Vector3(-9,0,-21.8f),new Vector3(5.6f,0,-24),
                new Vector3(19.6f,0,-23),new Vector3(-24,0,-31),new Vector3(-11,0,-29),
                new Vector3(8.2f,0,-32),new Vector3(24,0,-30)})
                AddTree(frontTrees,canopy,trunk,timber,greens,point,3.1f+Mathf.Abs(point.x%2)*.6f);
            frontTrees.Build(Group("Front garden trees",front,Vector3.zero));

            foreach(int side in new[]{-1,1})
            {
                var sidewalk=Group(side<0?"Left sidewalk":"Right sidewalk",neighborhood,
                    new Vector3(side*(halfWidth+2.67f),0,0));
                var surface=new Pieces();
                var furnishings=new Pieces();
                const float sidewalkBottom=-11.35f;
                float sidewalkTop=halfDepth+3.32f;
                float length=sidewalkTop-sidewalkBottom;
                float center=(sidewalkTop+sidewalkBottom)/2;
                surface.Add(slab,pavement,new Vector3(0,.05f,center),new Vector3(3.25f,.10f,length));
                surface.Add(box,cream,new Vector3(side*-1.57f,.055f,center),new Vector3(.16f,.11f,length));
                for(float z=sidewalkBottom+1.65f;z<sidewalkTop;z+=1.65f)
                    surface.Add(slab,joints,new Vector3(0,.099f,z),new Vector3(3.07f,.006f,.040f));
                surface.Build(sidewalk);

                foreach(float z in new[]{-6.95f,1.83f})
                    AddBench(furnishings,box,timber,metal,new Vector3(side*.80f,.1f,z),side*90);
                foreach(float z in new[]{-9.55f,-2.95f,5.22f})
                    AddLantern(furnishings,box,cylinder,sphere,metal,lamp,new Vector3(side*.89f,.1f,z));
                foreach(float z in new[]{-4.99f,.03f,8.16f})
                {
                    var point=new Vector3(side*1.49f,.1f,z);
                    furnishings.Add(box,joints,point+Vector3.up*.035f,new Vector3(.88f,.08f,.88f));
                    AddTree(furnishings,canopy,trunk,timber,greens,point,2.71f+Mathf.Abs(z%2)*.15f);
                }
                furnishings.Build(sidewalk);
            }

            BuildLanes(neighborhood,slab,halfWidth+2.67f,halfDepth+2.01f,pavement,cream);
            var yards=Group("Fenced backyards",neighborhood,Vector3.zero);
            var fence=Paint("Painted backyard fence","#F1DFC2");
            var soil=Paint("Vegetable garden soil","#936342");
            var carrot=Paint("Garden carrots","#E99949");
            var tomato=Paint("Garden tomatoes","#D9775D");
            foreach(var name in new[]{"Peach house","Sage house"})
            {
                var point=buildings.Find(name).localPosition;
                point.x+=Mathf.Sign(point.x)*1.2f;
                point.z+=6.45f;
                BuildYard(yards,name+" backyard",point,box,slab,sphere,timber,fence,soil,greens,carrot,tomato);
            }
            BuildDistantHouses(neighborhood,box,roof,new[]{new Vector3(-13.1f,0,33.3f),new Vector3(-5.6f,0,37.26f),
                new Vector3(4.2f,0,34.1f),new Vector3(12.7f,0,36.85f)});
            BuildDistantHouses(front,box,roof,new[]{new Vector3(-22,0,-34),new Vector3(-10,0,-32),
                new Vector3(3,0,-36),new Vector3(18,0,-32.7f)});

            var garden=new Pieces();
            foreach(var point in new[]{new Vector3(-19.0f,0,14.8f),new Vector3(18.8f,0,14.7f),
                new Vector3(-10.2f,0,19.1f),new Vector3(10.9f,0,22.7f),new Vector3(-27.0f,0,25.4f),
                new Vector3(25.3f,0,28.0f),new Vector3(6.6f,0,24.5f),new Vector3(-3.3f,0,33.8f)})
                AddTree(garden,canopy,trunk,timber,greens,point,3.22f+Mathf.Abs(point.x%2)*.33f);
            garden.Build(Group("Scattered village trees",neighborhood,Vector3.zero));

            void FrontHouse(string name,float x,float z,float yaw,Material plaster,Material shingles)
            {
                var point=new Vector3(x,0,z);
                BuildHouse(front,name,point,yaw,plaster,shingles,x<0?-1:1);
                var doorstep=point+Quaternion.Euler(0,yaw,0)*new Vector3(0,0,-2.55f);
                float sidewalkEdge=sidewalkZ-.95f;
                frontPaths.Add(slab,joints,new Vector3(doorstep.x,.021f,(doorstep.z+sidewalkEdge)/2),
                    new Vector3(1.28f,.042f,sidewalkEdge-doorstep.z));
            }

            void BuildHouse(Transform parent,string name,Vector3 point,float yaw,Material plaster,Material shingles,int chimneySide)
            {
                var house=Group(name,parent,point);
                house.localRotation=Quaternion.Euler(0,yaw,0);
                var pieces=new Pieces();
                pieces.Add(box,plaster,new Vector3(0,1.37f,0),new Vector3(5.18f,2.75f,4.03f));
                pieces.Add(box,cream,new Vector3(0,.20f,0),new Vector3(5.29f,.36f,4.16f));
                pieces.Add(roof,shingles,new Vector3(0,2.61f,0),new Vector3(1.02f,.73f,.96f));
                pieces.Add(box,shingles,new Vector3(chimneySide*1.43f,3.37f,.61f),new Vector3(.56f,1.32f,.53f));
                pieces.Add(box,cream,new Vector3(chimneySide*1.43f,4.02f,.61f),new Vector3(.67f,.15f,.64f));
                pieces.Add(doorway,cream,new Vector3(0,.03f,-2.02f),new Vector3(1.19f,1.1f,1));
                pieces.Add(doorway,timber,new Vector3(0,.12f,-2.10f),Vector3.one);
                pieces.Add(sphere,cream,new Vector3(.29f,.97f,-2.18f),Vector3.one*.14f);
                pieces.Add(box,cream,new Vector3(0,.115f,-2.29f),new Vector3(1.49f,.19f,.59f));
                foreach(float x in new[]{-1.53f,1.53f})
                {
                    AddWindow(pieces,box,cream,blueGlass,new Vector3(x,1.66f,-2.025f),1.04f,.91f);
                    pieces.Add(box,timber,new Vector3(x,1.09f,-2.16f),new Vector3(1.15f,.19f,.32f));
                    for(int leaf=-1;leaf<=1;leaf++)
                        pieces.Add(sphere,greens[(leaf+2)%3],new Vector3(x+leaf*.31f,1.22f,-2.18f),
                            new Vector3(.34f,.26f,.31f));
                }
                pieces.Build(house);
            }

            void BuildShop()
            {
                var shop=Group("Ice cream shop",buildings,new Vector3(.12f,0,halfDepth+6.25f));
                var pink=Paint("Strawberry canvas","#DF7D8C");
                var peach=Paint("Vanilla plaster","#F7DBB1");
                var teal=Paint("Mint roof","#67A89D");
                var cone=Paint("Waffle cone","#CF925A");
                var pieces=new Pieces();
                pieces.Add(box,peach,new Vector3(0,1.58f,0),new Vector3(6.27f,3.17f,4.34f));
                pieces.Add(box,teal,new Vector3(0,3.16f,0),new Vector3(6.55f,.25f,4.61f));
                pieces.Add(box,cream,new Vector3(0,.19f,0),new Vector3(6.33f,.35f,4.43f));
                AddWindow(pieces,box,cream,blueGlass,new Vector3(-1.30f,1.56f,-2.18f),2.88f,1.50f);
                pieces.Add(box,cream,new Vector3(1.84f,1.27f,-2.18f),new Vector3(1.36f,2.16f,.18f));
                pieces.Add(box,teal,new Vector3(1.84f,1.24f,-2.27f),new Vector3(1.12f,1.96f,.15f));
                pieces.Add(box,blueGlass,new Vector3(1.84f,1.53f,-2.35f),new Vector3(.72f,1.13f,.055f));
                pieces.Add(sphere,cream,new Vector3(2.23f,1.11f,-2.39f),Vector3.one*.14f);
                for(int i=0;i<10;i++)
                {
                    float x=(i-4.5f)*.584f;
                    var stripe=i%2==0?pink:cream;
                    pieces.Add(box,stripe,new Vector3(x,2.79f,-2.52f),new Vector3(.59f,.12f,.94f),Quaternion.Euler(-17,0,0));
                    pieces.Add(box,stripe,new Vector3(x,2.59f,-2.96f),new Vector3(.58f,.23f,.13f));
                }
                pieces.Add(box,cream,new Vector3(0,3.70f,-1.35f),new Vector3(2.69f,1.10f,.25f));
                pieces.Add(box,teal,new Vector3(0,3.72f,-1.28f),new Vector3(2.88f,1.27f,.24f));
                var waffle=Group("Ice cream cone sign",shop,new Vector3(0,3.30f,-1.52f));
                var sign=new Pieces();
                var wedge=ParkGeometry.Extrude("Waffle cone silhouette",
                    new List<Vector2>{new Vector2(0,0),new Vector2(.44f,.77f),new Vector2(-.44f,.77f)},.24f);
                sign.Add(wedge,cone,Vector3.zero,Vector3.one);
                sign.Add(sphere,pink,new Vector3(-.15f,.82f,0),new Vector3(.77f,.56f,.36f));
                sign.Add(sphere,cream,new Vector3(.21f,.87f,0),new Vector3(.64f,.59f,.33f));
                sign.Add(sphere,teal,new Vector3(-.02f,1.16f,.015f),new Vector3(.68f,.57f,.35f));
                sign.Build(waffle);
                pieces.Build(shop);
            }
        }

        static void BuildLanes(Transform parent,Mesh slab,float sidewalkX,float rearZ,Material pavement,Material stripes)
        {
            const float front=-13.48f,width=3.9f;
            var streets=Group("Curved village lanes",parent,Vector3.zero);
            var asphalt=Paint("Warm charcoal lane","#86847F");
            var shoulder=Paint("Stone lane edge","#B9B0A1");
            var corners=new[]
            {
                new Vector2(-sidewalkX,front),new Vector2(-8.1f,front-.08f),new Vector2(0,front),
                new Vector2(8.1f,front+.08f),new Vector2(sidewalkX,front),new Vector2(20.2f,-12.39f),
                new Vector2(22.18f,-7.5f),new Vector2(21.35f,-1.4f),new Vector2(20.82f,rearZ),
                new Vector2(23.10f,18.55f),new Vector2(20.4f,25.05f),new Vector2(12.0f,28.5f),
                new Vector2(3.4f,27.15f),new Vector2(-6.5f,29.05f),new Vector2(-17.38f,27.25f),
                new Vector2(-23.25f,22.3f),new Vector2(-22.70f,16.66f),new Vector2(-20.66f,rearZ),
                new Vector2(-21.63f,1.9f),new Vector2(-22.80f,-5.5f),new Vector2(-21.33f,-10.8f)
            };
            const int steps=12;
            var route=SampleLane(corners,steps,true);
            ParkGeometry.Place("Stone edging",LaneMesh(route,width+.34f,.028f,true),streets,Vector3.zero,Vector3.one,shoulder);
            ParkGeometry.Place("Asphalt",LaneMesh(route,width,.049f,true),streets,Vector3.zero,Vector3.one,asphalt);
            AddBranch("Lane toward distant houses",new[]{new Vector2(-.5f,27.9f),new Vector2(-.7f,33.4f),
                new Vector2(.3f,39.6f),new Vector2(3.6f,45.5f),new Vector2(6.2f,53.9f)});
            AddBranch("Lane toward the next block",new[]{new Vector2(23.15f,18.7f),new Vector2(27.9f,19.4f),
                new Vector2(33.4f,22.2f),new Vector2(40.7f,21.4f),new Vector2(48.1f,17.36f)});
            ParkGeometry.Place("Front sidewalk link",slab,streets,new Vector3(0,.035f,-11.33f),
                new Vector3(sidewalkX*2,.07f,.74f),pavement);

            AddCrossing("Left front crossing",0);
            AddCrossing("Right front crossing",4);
            AddCrossing("Right rear crossing",8);
            AddCrossing("Left rear crossing",17);

            void AddBranch(string name,Vector2[] points)
            {
                var branch=Group(name,streets,Vector3.zero);
                var path=SampleLane(points,steps,false);
                ParkGeometry.Place("Stone edging",LaneMesh(path,3.26f,.026f,false),branch,Vector3.zero,Vector3.one,shoulder);
                ParkGeometry.Place("Asphalt",LaneMesh(path,2.96f,.046f,false),branch,Vector3.zero,Vector3.one,asphalt);
            }

            void AddCrossing(string name,int corner)
            {
                int point=corner*steps;
                var tangent=(route[(point+1)%route.Length]-route[(point+route.Length-1)%route.Length]).normalized;
                var across=new Vector3(-tangent.y,0,tangent.x);
                var center=new Vector3(route[point].x,0,route[point].y);
                var crossing=Group(name,streets,center);
                crossing.localRotation=Quaternion.LookRotation(across);
                var pieces=new Pieces();
                const int stripeCount=5;
                const float stripeLength=1.75f,stripeGap=.15f,edgeInset=.17f;
                float stripeWidth=(width-2*edgeInset-(stripeCount-1)*stripeGap)/stripeCount;
                for(int band=0;band<stripeCount;band++)
                {
                    float offset=(band-(stripeCount-1)*.5f)*(stripeWidth+stripeGap);
                    pieces.Add(slab,stripes,new Vector3(0,.064f,offset),new Vector3(stripeLength,.016f,stripeWidth));
                }
                pieces.Add(slab,pavement,new Vector3(0,.044f,-2.59f),new Vector3(2.38f,.088f,1.36f));
                pieces.Add(slab,pavement,new Vector3(0,.045f,2.10f),new Vector3(2.38f,.09f,.44f));
                pieces.Build(crossing);
            }
        }

        static Vector2[] SampleLane(Vector2[] corners,int steps,bool closed)
        {
            int legs=closed?corners.Length:corners.Length-1;
            var route=new Vector2[legs*steps+(closed?0:1)];
            for(int i=0;i<legs;i++)
            {
                Vector2 a=Corner(i-1),b=Corner(i),c=Corner(i+1),d=Corner(i+2);
                for(int j=0;j<steps;j++)
                {
                    float t=j/(float)steps;
                    route[i*steps+j]=.5f*(2*b+(c-a)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                }
            }
            if(!closed)route[route.Length-1]=corners[corners.Length-1];
            return route;

            Vector2 Corner(int index)=>corners[closed?(index+corners.Length)%corners.Length:Mathf.Clamp(index,0,corners.Length-1)];
        }

        static Mesh LaneMesh(Vector2[] route,float width,float height,bool closed)
        {
            var points=new Vector3[route.Length*2];
            var indices=new List<int>(route.Length*6);
            for(int i=0;i<route.Length;i++)
            {
                int next=closed?(i+1)%route.Length:Mathf.Min(i+1,route.Length-1);
                int previous=closed?(i+route.Length-1)%route.Length:Mathf.Max(i-1,0);
                var tangent=(route[next]-route[previous]).normalized;
                var offset=new Vector2(-tangent.y,tangent.x)*(width/2);
                var inner=route[i]-offset;
                var outer=route[i]+offset;
                points[2*i]=new Vector3(inner.x,height,inner.y);
                points[2*i+1]=new Vector3(outer.x,height,outer.y);
                if(i!=next)indices.AddRange(new[]{2*i,2*i+1,2*next,2*i+1,2*next+1,2*next});
            }
            var mesh=new Mesh{name="Continuous curved lane ribbon"};
            mesh.vertices=points;
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            return mesh;
        }

        static void BuildYard(Transform yards,string name,Vector3 point,Mesh box,Mesh slab,Mesh sphere,
            Material timber,Material fence,Material soil,Material[] greens,Material carrot,Material tomato)
        {
            var yard=Group(name,yards,point);
            var enclosure=Group("Low garden fence",yard,Vector3.zero);
            var posts=new Pieces();
            const float halfWidth=3.14f,halfDepth=3.08f;
            Rail(new Vector3(-halfWidth,0,-halfDepth),new Vector3(-halfWidth,0,halfDepth));
            Rail(new Vector3(-halfWidth,0,halfDepth),new Vector3(halfWidth,0,halfDepth));
            Rail(new Vector3(halfWidth,0,halfDepth),new Vector3(halfWidth,0,-halfDepth));
            Rail(new Vector3(halfWidth,0,-halfDepth),new Vector3(-halfWidth,0,-halfDepth));
            posts.Build(enclosure);

            var garden=Group("Vegetable beds",yard,Vector3.zero);
            var bed=new Pieces();
            var steppingStones=new Pieces();
            for(int i=0;i<4;i++)
                steppingStones.Add(box,fence,new Vector3(0,.026f,-2.48f+i*1.21f),new Vector3(.71f,.06f,.62f),
                    Quaternion.Euler(0,i%2==0?9:-7,0));
            steppingStones.Build(Group("Garden stepping stones",yard,Vector3.zero));
            foreach(int side in new[]{-1,1})
            {
                float x=side*1.58f;
                bed.Add(box,timber,new Vector3(x,.16f,.20f),new Vector3(1.61f,.32f,4.09f));
                bed.Add(slab,soil,new Vector3(x,.331f,.20f),new Vector3(1.39f,.045f,3.88f));
                for(int row=0;row<6;row++)
                for(int column=0;column<2;column++)
                {
                    var crop=new Vector3(x+(column-.5f)*.62f,.43f,-1.38f+row*.60f);
                    if(side<0)
                    {
                        bed.Add(sphere,carrot,crop,new Vector3(.24f,.23f,.25f));
                        bed.Add(sphere,greens[row%greens.Length],crop+new Vector3(.03f,.15f,0),new Vector3(.14f,.24f,.13f),
                            Quaternion.Euler(0,0,-22));
                    }
                    else
                    {
                        bed.Add(sphere,greens[row%greens.Length],crop,new Vector3(.43f,.23f,.40f));
                        bed.Add(sphere,tomato,crop+new Vector3(.06f,.12f,.035f),new Vector3(.17f,.15f,.17f));
                    }
                }
            }
            bed.Build(garden);

            void Rail(Vector3 from,Vector3 to)
            {
                float length=Vector3.Distance(from,to);
                var turn=Quaternion.LookRotation(to-from);
                for(int rail=0;rail<2;rail++)
                    posts.Add(box,fence,(from+to)/2+Vector3.up*(.29f+rail*.26f),new Vector3(.09f,.103f,length),turn);
                const int postsPerSide=5;
                for(int i=0;i<postsPerSide;i++)
                    posts.Add(box,fence,Vector3.Lerp(from,to,i/(float)postsPerSide)+Vector3.up*.36f,new Vector3(.14f,.72f,.14f));
            }
        }

        static void BuildDistantHouses(Transform parent,Mesh box,Mesh roof,Vector3[] points)
        {
            var skyline=Group("Distant village roofs",parent,Vector3.zero);
            var plaster=Paint("Distant warm plaster","#CFBCA4");
            var shingles=new[]{Paint("Distant terracotta","#AB7765"),Paint("Distant slate","#788E9B"),
                Paint("Distant muted plum","#9D8496")};
            for(int i=0;i<points.Length;i++)
            {
                var house=Group($"Distant house {i+1}",skyline,points[i]);
                house.localRotation=Quaternion.Euler(0,11*i-14,0);
                float scale=.71f+i%3*.09f;
                var pieces=new Pieces();
                pieces.Add(box,plaster,Vector3.up*(1.25f*scale),new Vector3(4.95f,2.50f,3.91f)*scale);
                pieces.Add(roof,shingles[i%shingles.Length],Vector3.up*(2.38f*scale),new Vector3(.94f,.78f,.93f)*scale);
                pieces.Add(box,shingles[i%shingles.Length],new Vector3(1.28f,2.89f,.48f)*scale,new Vector3(.42f,1.12f,.47f)*scale);
                pieces.Build(house);
            }
        }

        static void AddWindow(Pieces pieces,Mesh box,Material trim,Material glass,Vector3 at,float width,float height)
        {
            pieces.Add(box,trim,at,new Vector3(width+.22f,height+.22f,.19f));
            pieces.Add(box,glass,at-Vector3.forward*.101f,new Vector3(width,height,.088f));
            pieces.Add(box,trim,at-Vector3.forward*.147f,new Vector3(.09f,height,.045f));
            pieces.Add(box,trim,at-Vector3.forward*.151f,new Vector3(width,.08f,.045f));
        }

        static void AddBench(Pieces pieces,Mesh box,Material timber,Material metal,Vector3 ground,float yaw)
        {
            var turn=Quaternion.Euler(0,yaw,0);
            foreach(float x in new[]{-.81f,.81f})
                pieces.Add(box,metal,ground+turn*new Vector3(x,.32f,0),new Vector3(.14f,.64f,.56f),turn);
            for(int i=0;i<3;i++)
            {
                pieces.Add(box,timber,ground+turn*new Vector3(0,.62f,-.21f+i*.20f),new Vector3(2.27f,.12f,.195f),turn);
                pieces.Add(box,timber,ground+turn*new Vector3(0,.77f+i*.155f,.28f),new Vector3(2.27f,.14f,.10f),turn);
            }
        }

        static void AddLantern(Pieces pieces,Mesh box,Mesh cylinder,Mesh sphere,Material metal,Material glow,Vector3 ground)
        {
            pieces.Add(cylinder,metal,ground+Vector3.up*.11f,new Vector3(.42f,.11f,.42f));
            pieces.Beam(cylinder,metal,ground+Vector3.up*.18f,ground+Vector3.up*2.26f,.115f);
            pieces.Add(box,glow,ground+Vector3.up*2.49f,new Vector3(.34f,.49f,.34f));
            pieces.Add(box,metal,ground+Vector3.up*2.25f,new Vector3(.43f,.11f,.43f));
            pieces.Add(box,metal,ground+Vector3.up*2.74f,new Vector3(.48f,.14f,.48f));
            pieces.Add(sphere,metal,ground+Vector3.up*2.83f,Vector3.one*.15f);
        }

        static void AddTree(Pieces pieces,Mesh canopy,Mesh trunk,Material bark,Material[] greens,Vector3 point,float height)
        {
            pieces.Add(trunk,bark,point,new Vector3(height*.13f,height*.27f,height*.13f));
            pieces.Add(canopy,greens[0],point+new Vector3(.12f,height*.75f,.05f),Vector3.one*(height*.32f));
            pieces.Add(canopy,greens[1],point+new Vector3(-.31f,height*.69f,.03f),new Vector3(height*.24f,height*.26f,height*.23f),
                Quaternion.Euler(0,39,0));
            pieces.Add(canopy,greens[2],point+new Vector3(.38f,height*.64f,-.13f),Vector3.one*(height*.19f),
                Quaternion.Euler(0,78,0));
        }
    }
}
