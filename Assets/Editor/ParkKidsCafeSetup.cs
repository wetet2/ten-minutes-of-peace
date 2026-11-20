using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkKidsCafeSetup
    {
        static readonly Vector2 RoundaboutCenter = new Vector2(-.25f,-.30f);
        const float RoundaboutRadius = 3.05f;
        const float RoomWidth=26f,RoomDepth=17.5f;
        static readonly Vector3 TowerPosition=new Vector3(5.95f,0,3.35f);
        static readonly Vector3 PolePosition=new Vector3(-8.10f,0,4.45f);
        static readonly Vector3 TrampolinePosition=new Vector3(-8.80f,0,-5.10f);
        static readonly Vector3 SeesawPosition=new Vector3(8.15f,0,-5.58f);

        internal static ParkVenue Create(Transform root)
        {
            var box=ParkSurface.RoundedBox();
            var sphere=PrimitiveMesh(PrimitiveType.Sphere);
            var cylinder=PrimitiveMesh(PrimitiveType.Cylinder);
            var mint=Paint("Mint padded trim","#55BD9B");
            var cream=Paint("Vanilla play mat","#F3D5A5");
            var plaster=Paint("Peach plaster","#FAD5AF");
            var wood=Paint("Honey wood","#D39656");
            var shade=Paint("Entrance passage","#A97660");
            var blue=Paint("Sky-blue foam","#549BE0");
            var yellow=Paint("Buttercup slide","#F5BD32");
            var coral=Paint("Apricot foam","#EF805E");
            float left=-RoomWidth/2,back=RoomDepth/2;
            ParkCafeTerrace.Create(root,RoomWidth,RoomDepth,cream,box,sphere);

            var roundabout=CreateRoundabout(root,sphere,cylinder,mint,yellow);
            var slide=CreateSlide(root,box,sphere,cylinder,mint,blue,yellow,coral);
            var gate=CreateLookout("Spiral slide lookout",slide.transform,new Vector3(SpiralRadius,TowerHeight,.80f),
                box,sphere,mint,wood,shade,plaster,yellow);
            gate.DisplayName="미끄럼틀 꼭대기";
            gate.Welcome=Group("Welcome",gate.transform,gate.transform.InverseTransformPoint(slide.Exit));
            gate.EntranceSlide=slide;
            slide.Entrance=gate;
            var pole=CreatePoleTower(root,box,sphere,cylinder,blue,wood,shade,plaster,yellow,coral,mint);
            var trampoline=CreateTrampoline(root,box,sphere,cylinder,mint,coral,blue);
            var seesaw=CreateSeesaw(root,box,sphere,cylinder,mint,coral,blue,yellow);
            ClearMaterials(root);
            root.gameObject.AddComponent<ParkPlaygroundAtmosphere>();
            return new ParkVenue
            {
                Name="놀이터",Root=root.gameObject,Houses=new[]{gate,pole},LawnHeight=0,Slide=slide,
                Trampoline=trampoline,Seesaw=seesaw,Roundabout=roundabout,
                WalkArea=Rect.MinMaxRect(left-.40f,-back-.75f,-left+.40f,back-.90f),
                PondCenter=RoundaboutCenter,PondRadii=Vector2.one*(RoundaboutRadius+.35f),
                WalkObstacles=new[]{
                    Rect.MinMaxRect(TowerPosition.x-3.7f,TowerPosition.z-2.75f,TowerPosition.x+3.72f,TowerPosition.z+3.72f),
                    Rect.MinMaxRect(TowerPosition.x+3.72f,TowerPosition.z-2.45f,TowerPosition.x+6.57f,TowerPosition.z+3.74f),
                    Rect.MinMaxRect(PolePosition.x-1.93f,PolePosition.z-2.13f,PolePosition.x+1.93f,PolePosition.z+2.65f),
                    Rect.MinMaxRect(TrampolinePosition.x-2.35f,TrampolinePosition.z-2.75f,TrampolinePosition.x+2.35f,TrampolinePosition.z+2.75f),
                    Rect.MinMaxRect(SeesawPosition.x-2.64f,SeesawPosition.z-1.19f,SeesawPosition.x+2.64f,SeesawPosition.z+1.19f)},
                Focus=new Vector3(.25f,1.55f,1.8f),Distance=41f,Yaw=-8,Pitch=34,
                FogStart=95,FogEnd=210,Sky=new Color32(170,208,231,255)
            };
        }

        static void ClearMaterials(Transform root)
        {
            // Clone venue materials so shared assets in other places keep their existing appearance.
            var copies=new Dictionary<Material,Material>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];
                    if(!source||source.shader.name!="PeacePark/Storybook")continue;
                    if(!copies.TryGetValue(source,out var copy))
                    {
                        copy=new Material(source){name=source.name+" - clear playground"};
                        copy.SetColor("_Shade",new Color(.49f,.53f,.57f));
                        copy.SetFloat("_ShadowFloor",.52f);copy.SetFloat("_ShadowFadeDistance",85);copies.Add(source,copy);
                    }
                    materials[i]=copy;
                }
                renderer.sharedMaterials=materials;
            }
        }

        static ParkHouse CreateLookout(string name,Transform parent,Vector3 position,Mesh box,Mesh sphere,
            Material trim,Material wood,Material shade,Material wall,Material brass)
        {
            var entry=Group(name,parent,position);
            var frame=new Pieces();
            frame.Add(Arch(1.27f,1.56f,1.05f,.28f),trim,Vector3.zero,Vector3.one);
            frame.Add(box,wall,new Vector3(-1.20f,1.34f,.91f),new Vector3(.21f,2.68f,1.8f));
            frame.Add(box,wall,new Vector3(1.20f,1.34f,.91f),new Vector3(.21f,2.68f,1.8f));
            frame.Add(box,shade,new Vector3(0,1.36f,1.78f),new Vector3(2.42f,2.72f,.23f));
            frame.Add(box,trim,new Vector3(0,2.76f,.85f),new Vector3(2.73f,.30f,2.37f));
            frame.Add(ParkGeometry.Roof(),trim,new Vector3(0,2.82f,.85f),new Vector3(.56f,.54f,.55f));
            frame.Add(sphere,brass,new Vector3(0,3.83f,.85f),Vector3.one*.37f);
            frame.Build(entry);
            var hinge=Group("Door hinge",entry,new Vector3(-1.03f,0,-.085f));
            var door=new Pieces();
            door.Add(Arch(1.025f,1.55f,0,.15f),wood,new Vector3(1.03f,0,0),Vector3.one);
            for(int i=0;i<5;i++)
                door.Add(box,wood,new Vector3(.205f+i*.412f,.79f,-.079f),new Vector3(.394f,1.60f,.067f));
            door.Add(sphere,brass,new Vector3(1.79f,1.12f,-.15f),Vector3.one*.20f);
            door.Build(hinge);
            var house=entry.gameObject.AddComponent<ParkHouse>();
            house.DoorHinge=hinge;
            house.OpenAngle=-104;
            house.Inside=Group("Inside",entry,new Vector3(0,0,1.11f));
            house.Threshold=Group("Threshold",entry,Vector3.zero);
            return house;
        }

        static ParkHouse CreatePoleTower(Transform root,Mesh box,Mesh sphere,Mesh cylinder,Material blue,
            Material wood,Material shade,Material wall,Material yellow,Material coral,Material mint)
        {
            const float deckHeight=5.20f,poleZ=-1.70f;
            var tower=Group("Fire pole tower",root,PolePosition);
            var flat=PrimitiveMesh(PrimitiveType.Cube);
            var pieces=new Pieces();
            pieces.Add(box,mint,new Vector3(0,.018f,-1.83f),new Vector3(2.94f,.036f,2.40f));
            pieces.Add(flat,blue,new Vector3(0,deckHeight-.17f,.55f),new Vector3(3.01f,.34f,3.69f));
            for(int side=-1;side<=1;side+=2)
            {
                foreach(float z in new[]{-.94f,1.92f})
                {
                    pieces.Add(box,blue,new Vector3(side*1.32f,deckHeight/2,z),new Vector3(.29f,deckHeight,.29f));
                    pieces.Add(box,mint,new Vector3(side*1.32f,.22f,z),new Vector3(.46f,.44f,.46f));
                }
                pieces.Beam(cylinder,blue,new Vector3(side*1.32f,3.30f,1.92f),
                    new Vector3(side*1.32f,deckHeight-.14f,-.94f),.19f);
                pieces.Add(box,coral,new Vector3(side*1.32f,deckHeight+.44f,-.79f),new Vector3(.12f,.13f,.98f));
                pieces.Add(box,blue,new Vector3(side*1.32f,deckHeight+.26f,-1.24f),new Vector3(.15f,.52f,.15f));
                pieces.Beam(cylinder,blue,new Vector3(1.49f,0,1.92f+side*.43f),
                    new Vector3(1.49f,deckHeight+.33f,1.62f+side*.43f),.14f);
            }
            for(int rung=1;rung<=12;rung++)
            {
                float y=rung*.41f;
                float z=Mathf.Lerp(1.92f,1.62f,y/(deckHeight+.33f));
                pieces.Beam(cylinder,rung%3==0?yellow:blue,new Vector3(1.49f,y,z-.43f),
                    new Vector3(1.49f,y,z+.43f),.12f);
            }

            const float poleHeight=7.35f;
            pieces.Beam(cylinder,blue,new Vector3(0,poleHeight,.04f),new Vector3(0,poleHeight,poleZ),.21f);
            pieces.Beam(cylinder,coral,new Vector3(0,.02f,poleZ),new Vector3(0,poleHeight,poleZ),.16f);
            pieces.Add(sphere,yellow,new Vector3(0,poleHeight,poleZ),Vector3.one*.29f);
            pieces.Add(cylinder,yellow,new Vector3(0,.08f,poleZ),new Vector3(.32f,.08f,.32f));
            pieces.Build(tower);

            var gate=CreateLookout("Fire pole lookout",tower,new Vector3(0,deckHeight,-.26f),
                box,sphere,blue,wood,shade,wall,yellow);
            gate.DisplayName="봉 타워";
            gate.EntrancePole=Group("Pole axis",tower,new Vector3(0,0,poleZ));
            gate.Welcome=Group("Welcome",tower,new Vector3(-1.42f,0,-3.22f));
            return gate;
        }

        static Mesh Arch(float outer,float spring,float inner,float depth)
        {
            var points=new List<Vector3>();
            var indices=new List<int>();
            float half=depth/2;
            Vector3 Face(Vector2 p,float z)=>new Vector3(p.x,p.y,z);
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int n=points.Count;
                points.AddRange(new[]{a,b,c,d});
                indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var outline=new List<Vector2>{new Vector2(outer,0)};
            var inset=new List<Vector2>{new Vector2(inner,0)};
            for(int i=0;i<=36;i++)
            {
                float angle=i*Mathf.PI/36;
                var origin=Vector2.up*spring;
                var ray=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                outline.Add(origin+outer*ray);
                inset.Add(origin+inner*ray);
            }
            outline.Add(new Vector2(-outer,0));
            inset.Add(new Vector2(-inner,0));
            for(int i=0;i<outline.Count-1;i++)
            {
                var a=outline[i];var b=outline[i+1];var c=inset[i+1];var d=inset[i];
                Quad(Face(d,-half),Face(c,-half),Face(b,-half),Face(a,-half));
                Quad(Face(a,half),Face(b,half),Face(c,half),Face(d,half));
                Quad(Face(a,-half),Face(b,-half),Face(b,half),Face(a,half));
                if(inner>0)Quad(Face(c,-half),Face(d,-half),Face(d,half),Face(c,half));
            }
            var mesh=new Mesh{name="Rounded play arch"};
            mesh.SetVertices(points);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            return mesh;
        }

        static ParkRoundaboutRide CreateRoundabout(Transform root,Mesh sphere,Mesh cylinder,Material mint,Material yellow)
        {
            var roundabout=Group("Roundabout",root,new Vector3(RoundaboutCenter.x,0,RoundaboutCenter.y));
            var teal=Paint("Roundabout teal","#559A89");
            ParkGeometry.Place("Fixed bearing",cylinder,roundabout,Vector3.up*.14f,
                new Vector3(1.78f,.14f,1.78f),teal);

            var platform=Group("Rotating platform",roundabout,Vector3.zero);
            var spin=platform.gameObject.AddComponent<ParkRoundaboutSpin>();
            const float deckHeight=.53f,railRadius=1.30f;
            var pieces=new Pieces();
            float diameter=2*RoundaboutRadius;
            pieces.Add(cylinder,teal,new Vector3(0,.29f,0),new Vector3(diameter-.12f,.13f,diameter-.12f));
            pieces.Add(cylinder,mint,new Vector3(0,deckHeight-.10f,0),new Vector3(diameter,.10f,diameter));
            pieces.Add(cylinder,teal,new Vector3(0,.54f,0),new Vector3(.66f,.025f,.66f));
            var handles=new Transform[3];
            for(int i=0;i<handles.Length;i++)
            {
                var angle=Quaternion.Euler(0,20f+i*120f,0);
                var left=angle*new Vector3(-.58f,.52f,railRadius);
                var right=angle*new Vector3(.58f,.52f,railRadius);
                var rail=Vector3.up*.92f;
                handles[i]=Group("Handrail grip "+(i+1),platform,(left+right)*.5f+rail);
                const float tube=.19f;
                pieces.Beam(cylinder,yellow,left,left+rail,tube);
                pieces.Beam(cylinder,yellow,right,right+rail,tube);
                pieces.Beam(cylinder,yellow,left+rail,right+rail,tube);
                pieces.Add(sphere,yellow,left+rail,Vector3.one*tube);
                pieces.Add(sphere,yellow,right+rail,Vector3.one*tube);
                pieces.Add(cylinder,teal,left,new Vector3(.31f,.04f,.31f));
                pieces.Add(cylinder,teal,right,new Vector3(.31f,.04f,.31f));
            }
            pieces.Build(platform);
            var ride=roundabout.gameObject.AddComponent<ParkRoundaboutRide>();
            ride.Configure(spin,handles,RoundaboutRadius,deckHeight);
            return ride;
        }

        const float SpiralRadius=2.55f,TowerHeight=6.1f,RunoutDepth=1.6f;

        static ParkSlide CreateSlide(Transform venue,Mesh box,Mesh sphere,Mesh cylinder,Material mint,Material blue,
            Material yellow,Material coral)
        {
            var root=Group("Spiral slide tower",venue,TowerPosition);
            var slide=root.gameObject.AddComponent<ParkSlide>();
            var structure=new Pieces();
            structure.Add(cylinder,mint,new Vector3(0,2.995f,0),new Vector3(.94f,2.995f,.94f));
            structure.Add(cylinder,blue,new Vector3(0,.12f,0),new Vector3(1.63f,.12f,1.63f));
            for(int band=1;band<=5;band++)
                structure.Add(cylinder,blue,new Vector3(0,band*1.15f,0),new Vector3(.99f,.075f,.99f));
            var flat=PrimitiveMesh(PrimitiveType.Cube);
            structure.Add(flat,mint,new Vector3(2.56f,TowerHeight-.125f,1.30f),new Vector3(2.53f,.25f,2.66f));
            structure.Add(flat,mint,new Vector3(4.40f,TowerHeight-.125f,.18f),new Vector3(1.23f,.25f,.40f));
            structure.Add(box,blue,new Vector3(SpiralRadius,.019f,-2.43f),new Vector3(2.50f,.038f,1.82f));
            structure.Beam(cylinder,mint,new Vector3(0,5.67f,0),new Vector3(2.55f,5.84f,.88f),.15f);

            // Carry the lookout deck from the ground outside the spiral and the stair treads.
            var supports=Group("Lookout supports",root,Vector3.zero);
            const float columnHeight=TowerHeight-.25f;
            foreach(var foot in new[]{new Vector3(3.50f,0,1.98f),new Vector3(2.29f,0,3.34f)})
            {
                var column=Group("Lookout column",supports,foot);
                ParkGeometry.Place("Post",box,column,Vector3.up*(columnHeight/2),
                    new Vector3(.30f,columnHeight,.30f),mint);
                ParkGeometry.Place("Padded footing",box,column,Vector3.up*.19f,
                    new Vector3(.44f,.38f,.44f),blue);
                structure.Beam(cylinder,mint,foot+Vector3.up*(columnHeight-.08f),
                    new Vector3(2.62f,columnHeight-.06f,1.68f),.21f);
            }

            // Build the chute and centerline from the same points, including its straight floor-level end.
            const int turns=112,braking=11;
            var route=new Vector3[turns+braking+1];
            for(int i=0;i<=turns;i++)
            {
                float t=i/(float)turns;
                float angle=t*(-2*Mathf.PI);
                float height=t-.105f*Mathf.Sin(2*Mathf.PI*t);
                route[i]=new Vector3(SpiralRadius*Mathf.Cos(angle),Mathf.Lerp(TowerHeight,.37f,height),SpiralRadius*Mathf.Sin(angle));
            }
            for(int i=1;i<=braking;i++)
            {
                float t=i/(float)braking;
                route[turns+i]=new Vector3(SpiralRadius,.37f*(1-t)*(1-t),-RunoutDepth*t);
            }
            slide.Chute=route;
            slide.Runout=new[]{route[route.Length-1],new Vector3(SpiralRadius,0,-3.03f)};
            ParkGeometry.Place("Golden spiral chute",ChuteMesh(route),root,Vector3.zero,Vector3.one,yellow);
            for(int i=1;i<8;i++)
            {
                var point=route[turns*i/8];
                var rim=new Vector3(point.x,0,point.z).normalized;
                structure.Beam(cylinder,mint,new Vector3(0,Mathf.Max(.3f,point.y-.84f),0),
                    point-rim*.22f-Vector3.up*.17f,.13f);
            }
            slide.Steps=BuildStairs(structure,flat,box,sphere,blue,mint,coral);
            structure.Build(root);
            return slide;
        }

        static Mesh ChuteMesh(Vector3[] route)
        {
            var vertices=new List<Vector3>();
            var indices=new List<int>();
            var section=new[]{new Vector2(-1.12f,-.15f),new Vector2(-1.12f,.34f),new Vector2(-.98f,.39f),
                new Vector2(-.88f,.04f),new Vector2(-.80f,0),new Vector2(.80f,0),new Vector2(.88f,.04f),
                new Vector2(.98f,.39f),new Vector2(1.12f,.34f),new Vector2(1.12f,-.15f)};
            for(int i=0;i<route.Length;i++)
            {
                var heading=route[Mathf.Min(i+1,route.Length-1)]-route[Mathf.Max(i-1,0)];
                var across=new Vector3(-heading.z,0,heading.x).normalized;
                foreach(var p in section)
                {
                    float height=p.y;
                    if(height>0)height*=Mathf.Clamp01((route.Length-1-i)/10f);
                    vertices.Add(route[i]+across*p.x+Vector3.up*height);
                }
                if(i==0)continue;
                int previous=(i-1)*section.Length;
                for(int j=0;j<section.Length;j++)
                {
                    int a=previous+j,b=previous+(j+1)%section.Length;
                    int c=a+section.Length,d=b+section.Length;
                    indices.AddRange(new[]{a,b,c,b,d,c});
                }
            }
            var mesh=new Mesh{name="Rounded spiral chute"};
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            return mesh;
        }

        static Vector3[] BuildStairs(Pieces pieces,Mesh tread,Mesh box,Mesh sphere,Material blue,Material mint,Material coral)
        {
            const float lowerX=5.82f, upperX=4.40f, stepWidth=1.23f, landingZ=3.47f, midHeight=3.38f;
            float landingX=(lowerX+upperX)/2;
            var route=new List<Vector3>{new Vector3(lowerX,0,-3.13f)};
            Flight(lowerX,-2.39f,2.87f,0,midHeight,13);
            pieces.Add(tread,blue,new Vector3(landingX,midHeight-.14f,3.16f),new Vector3(2.65f,.28f,.58f));
            route.Add(new Vector3(lowerX,midHeight,3.16f));
            route.Add(new Vector3(upperX,midHeight,3.16f));
            Flight(upperX,2.87f,.38f,midHeight,TowerHeight,9);
            route.Add(new Vector3(upperX,TowerHeight,.34f));
            route.Add(new Vector3(SpiralRadius,TowerHeight,.34f));
            route.Add(new Vector3(SpiralRadius,TowerHeight,0));

            pieces.Add(box,mint,new Vector3(landingX,midHeight+.41f,landingZ),new Vector3(2.68f,.76f,.12f));
            foreach(float x in new[]{upperX,lowerX})
                pieces.Add(box,mint,new Vector3(x,midHeight*.5f,3.05f),new Vector3(.32f,midHeight,.33f));
            return route.ToArray();

            void Flight(float x,float fromZ,float toZ,float fromY,float toY,int count)
            {
                float depth=Mathf.Abs(toZ-fromZ)/count;
                for(int step=0;step<count;step++)
                {
                    float end=(step+1)/(float)count;
                    float height=Mathf.Lerp(fromY,toY,end);
                    float z=Mathf.Lerp(fromZ,toZ,(step+.5f)/count);
                    pieces.Add(tread,step%3==1?mint:blue,new Vector3(x,height-.115f,z),new Vector3(stepWidth,.23f,depth+.012f));
                    route.Add(new Vector3(x,height,z));
                    if(step%3!=0&&step!=count-1)continue;
                    float side=x==lowerX?1:-1;
                    var post=new Vector3(x+side*stepWidth/2,height,z);
                    pieces.Add(box,mint,post+Vector3.up*.40f,new Vector3(.10f,.80f,.10f));
                    pieces.Add(sphere,coral,post+Vector3.up*.84f,Vector3.one*.21f);
                }
            }
        }

        static ParkTrampoline CreateTrampoline(Transform root,Mesh box,Mesh sphere,Mesh cylinder,Material mint,Material coral,Material blue)
        {
            var trampoline=Group("Trampoline",root,TrampolinePosition);
            var mat=Paint("Trampoline navy mat","#3D5478");
            var rope=Paint("Trampoline safety net","#628DAB");
            var pieces=new Pieces();
            pieces.Add(cylinder,blue,new Vector3(0,.53f,0),new Vector3(4.32f,.13f,4.32f));
            pieces.Add(cylinder,coral,new Vector3(0,.67f,0),new Vector3(4.44f,.10f,4.44f));
            pieces.Add(cylinder,mat,new Vector3(0,.77f,0),new Vector3(3.78f,.019f,3.78f));
            const int subdivisions=5,segments=3*subdivisions;
            const float opening=.40f;
            for(int half=0;half<2;half++)
            {
                int side=half==0?-1:1;
                pieces.Add(box,mint,new Vector3(0,.16f,side*2.41f),new Vector3(1.48f,.32f,.66f));
                pieces.Add(box,blue,new Vector3(0,.34f,side*2.13f),new Vector3(1.48f,.37f,.57f));
                float start=half*Mathf.PI+opening,step=(Mathf.PI-2*opening)/segments;
                for(int i=0;i<=segments;i++)
                {
                    float a=start+i*step,b=a+step;
                    if(i%subdivisions==0)
                    {
                        var foot=RailPoint(a,.12f);
                        pieces.Add(cylinder,blue,foot,new Vector3(.28f,.12f,.28f));
                        pieces.Beam(cylinder,mint,foot,RailPoint(a,2.35f),.128f);
                        pieces.Add(sphere,coral,RailPoint(a,2.35f),Vector3.one*.24f);
                    }
                    else pieces.Beam(cylinder,rope,RailPoint(a,.80f),RailPoint(a,2.30f),.015f);
                    if(i==segments)break;
                    for(int row=0;row<5;row++)
                    {
                        float height=.86f+row*.35f;
                        pieces.Beam(cylinder,rope,RailPoint(a,height),RailPoint(b,height),.017f);
                    }
                    pieces.Beam(cylinder,mint,RailPoint(a,2.30f),RailPoint(b,2.30f),.075f);
                }
            }
            pieces.Build(trampoline);
            return trampoline.gameObject.AddComponent<ParkTrampoline>();

            static Vector3 RailPoint(float angle,float height)=>new Vector3(2.10f*Mathf.Sin(angle),height,-2.10f*Mathf.Cos(angle));
        }

        static ParkSeesaw CreateSeesaw(Transform root,Mesh box,Mesh sphere,Mesh cylinder,Material mint,Material coral,
            Material blue,Material yellow)
        {
            var seesaw=Group("Seesaw",root,SeesawPosition);
            seesaw.localRotation=Quaternion.Euler(0,-12,0);
            var basePieces=new Pieces();
            basePieces.Add(box,mint,new Vector3(0,.12f,0),new Vector3(1.20f,.24f,1.14f));
            basePieces.Add(box,blue,new Vector3(0,.57f,0),new Vector3(.53f,.91f,.62f));
            basePieces.Add(cylinder,yellow,new Vector3(0,.92f,0),new Vector3(.43f,.43f,.43f),Quaternion.Euler(90,0,0));
            basePieces.Build(seesaw);

            var beam=Group("Seesaw plank",seesaw,new Vector3(0,.98f,0));
            var seats=new Pieces();
            seats.Add(box,yellow,Vector3.zero,new Vector3(4.81f,.24f,.45f));
            for(int side=-1;side<=1;side+=2)
            {
                var seat=side<0?coral:mint;
                seats.Add(box,seat,new Vector3(side*1.98f,.17f,0),new Vector3(.83f,.22f,.74f));
                seats.Add(box,seat,new Vector3(side*2.23f,.34f,0),new Vector3(.24f,.42f,.73f));
                var handle=new Vector3(side*1.41f,0,0);
                seats.Beam(cylinder,blue,handle+Vector3.up*.11f,handle+Vector3.up*.59f,.10f);
                seats.Beam(cylinder,blue,handle+new Vector3(0,.58f,-.26f),handle+new Vector3(0,.58f,.26f),.13f);
                for(int grip=-1;grip<=1;grip+=2)
                    seats.Add(sphere,seat,handle+new Vector3(0,.58f,grip*.26f),Vector3.one*.19f);
            }
            seats.Build(beam);
            var ride=seesaw.gameObject.AddComponent<ParkSeesaw>();
            ride.Beam=beam;
            return ride;
        }
    }
}
