using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkKidsCafeSetup
    {
        static readonly Vector2 BallPit = new Vector2(-.25f,-.30f);
        static readonly Vector2 PitRadii = new Vector2(4.05f,2.75f);
        const float RoomWidth=26f,RoomDepth=17.5f,WallThickness=.38f,WallHeight=5.1f;
        static readonly Vector3 EntrancePosition=new Vector3(-RoomWidth/2+3.95f,0,RoomDepth/2-2.85f);
        static readonly Vector3 TablePosition=new Vector3(-RoomWidth/2+3.17f,0,-RoomDepth/2+3.28f);

        internal static ParkVenue Create(Transform root)
        {
            var box=ParkSurface.RoundedBox();
            var sphere=PrimitiveMesh(PrimitiveType.Sphere);
            var cylinder=PrimitiveMesh(PrimitiveType.Cylinder);
            var mint=Paint("Mint padded trim","#8CCDB5");
            var cream=Paint("Vanilla play mat","#FFE0AF");
            var plaster=Paint("Peach plaster","#FAD5AF");
            var wood=Paint("Honey wood","#D39656");
            var shade=Paint("Entrance passage","#A97660");
            var blue=Paint("Sky-blue foam","#8BB9EA");
            var yellow=Paint("Buttercup slide","#FFD358");
            var coral=Paint("Apricot foam","#FA946F");
            var white=Paint("Vanilla clouds","#FFF3DB");
            var leaf=Paint("Window treetops","#94C95D");
            var square=PrimitiveMesh(PrimitiveType.Cube);
            var room=new Pieces();
            void Box(Material m,float x,float y,float z,float w,float h,float d)=>room.Add(box,m,new Vector3(x,y,z),new Vector3(w,h,d));
            void Straight(string name,Material m,float x,float y,float z,float w,float h,float d)=>
                ParkGeometry.Place(name,square,root,new Vector3(x,y,z),new Vector3(w,h,d),m);
            float left=-RoomWidth/2,back=RoomDepth/2;
            float sideX=left+WallThickness/2,backZ=back-WallThickness/2;
            float sideDepth=RoomDepth-WallThickness,sideZ=-WallThickness/2;
            Straight("Rectangular floor base",mint,0,-.555f,0,RoomWidth,.59f,RoomDepth);
            Straight("Rectangular play floor",cream,0,-.13f,0,RoomWidth,.26f,RoomDepth);
            float wallTop=WallHeight-.10f;
            Straight("Left wall",plaster,sideX,wallTop/2,sideZ,WallThickness,wallTop,sideDepth);
            Straight("Left wall cap",mint,sideX,WallHeight,sideZ,WallThickness,.20f,sideDepth);
            Straight("Left padded skirting",mint,left+WallThickness+.065f,.48f,sideZ,.13f,.96f,sideDepth);
            Straight("Back wall sill",plaster,0,.77f,backZ,RoomWidth,1.54f,WallThickness);
            Straight("Back wall header",plaster,0,(4.46f+wallTop)/2,backZ,RoomWidth,wallTop-4.46f,WallThickness);
            Straight("Back wall cap",mint,0,WallHeight,backZ,RoomWidth,.20f,WallThickness);
            const float windowHalfWidth=3.12f;
            float wallStart=left;
            foreach(float x in new[]{-.65f,7.15f})
            {
                float windowLeft=x-windowHalfWidth;
                Straight("Back wall beside window",plaster,(wallStart+windowLeft)/2,3,backZ,windowLeft-wallStart,2.92f,WallThickness);
                wallStart=x+windowHalfWidth;
                Box(blue,x,3,back-.08f,6.3f,2.94f,.12f);
                for(int j=0;j<6;j++)
                    room.Add(sphere,leaf,new Vector3(x-2.5f+j,1.75f+j%3*.15f,back-.21f),new Vector3(1.58f,1.0f+j%2*.22f,.15f));
                float frameZ=back-WallThickness-.03f;
                for(int edge=-1;edge<=1;edge++)Box(wood,x+edge*windowHalfWidth,3,frameZ,.20f,3.1f,.20f);
                for(int edge=-1;edge<=1;edge++)Box(wood,x,3+edge*1.46f,frameZ,6.45f,.19f,.20f);
            }
            Straight("Back wall right end",plaster,(wallStart-left)/2,3,backZ,-left-wallStart,2.92f,WallThickness);
            for(int i=0;i<5;i++)
            {
                float z=1.9f+i*.31f;
                room.Add(sphere,white,new Vector3(left+WallThickness+.11f,3.65f+Mathf.Sin(i*Mathf.PI/4)*.24f,z),new Vector3(.18f,.66f,.73f));
            }
            Box(mint,6.6f,.015f,4.84f,7.12f,.035f,4.7f);
            room.Build(root);

            var gate=CreateEntrance(root,box,sphere,mint,wood,shade,plaster,yellow);
            CreateBallPit(root,box,sphere,cylinder,mint,coral,blue,yellow,white);
            CreateTable(root,box,sphere,cylinder,wood,yellow,mint,white);
            var play=new Pieces();
            play.Add(box,coral,new Vector3(4.4f,.42f,5.15f),new Vector3(1.65f,.84f,1.65f));
            play.Add(box,blue,new Vector3(9.24f,.52f,5.15f),new Vector3(1.64f,1.04f,1.65f));
            play.Add(Arch(1.4f,1.08f,.60f,1.35f),yellow,new Vector3(6.81f,0,5.12f),new Vector3(1.55f,1.21f,1.34f));
            play.Add(box,coral,new Vector3(8.18f,.39f,2.64f),new Vector3(.75f,.78f,.76f),Quaternion.Euler(0,22,0));
            play.Add(box,blue,new Vector3(9.08f,.30f,2.29f),new Vector3(.62f,.60f,.62f),Quaternion.Euler(0,-15,0));
            play.Add(box,yellow,new Vector3(7.4f,.46f,2.74f),new Vector3(.74f,.92f,.74f));
            play.Build(Group("Soft climbing toys",root,Vector3.zero));

            return new ParkVenue
            {
                Name="키즈카페",Root=root.gameObject,Houses=new[]{gate},LawnHeight=0,
                WalkArea=Rect.MinMaxRect(left+1.15f,-back+1.08f,-left-1.15f,back-.90f),
                PondCenter=BallPit,PondRadii=PitRadii+new Vector2(.62f,.62f),
                WalkObstacles=new[]{Rect.MinMaxRect(BallPit.x+SlideCenter-SlideWidth/2-.8f,BallPit.y+SlideTop-.5f,
                    BallPit.x+SlideCenter+SlideWidth/2+.8f,BallPit.y+SlideTop+LandingDepth+StepDepth*StepCount+.55f),
                    Rect.MinMaxRect(3.05f,1.79f,10.36f,6.37f),
                    Rect.MinMaxRect(TablePosition.x-2.14f,TablePosition.z-1.48f,TablePosition.x+2.13f,TablePosition.z+1.62f),
                    Rect.MinMaxRect(EntrancePosition.x-1.79f,EntrancePosition.z-.17f,EntrancePosition.x+1.90f,EntrancePosition.z+2.05f)},
                Focus=new Vector3(0,1.48f,.15f),Distance=29.7f,Yaw=-15,Pitch=37,
                Sky=new Color32(219,226,207,255)
            };
        }

        static ParkHouse CreateEntrance(Transform root,Mesh box,Mesh sphere,Material trim,Material wood,Material shade,Material wall,Material brass)
        {
            // The entrance is grounded to the same floor as its landing and walk area.
            var entry=Group("Kids cafe entrance",root,EntrancePosition);
            var frame=new Pieces();
            frame.Add(Arch(1.45f,1.90f,1.18f,.35f),trim,Vector3.zero,Vector3.one);
            frame.Add(box,wall,new Vector3(-1.62f,1.55f,.99f),new Vector3(.40f,3.1f,2.03f));
            frame.Add(box,wall,new Vector3(1.62f,1.55f,.99f),new Vector3(.40f,3.1f,2.03f));
            frame.Add(box,shade,new Vector3(0,1.58f,1.94f),new Vector3(3.10f,3.15f,.23f));
            frame.Build(entry);
            var hinge=Group("Door hinge",entry,new Vector3(-1.14f,0,-.10f));
            var door=new Pieces();
            door.Add(Arch(1.13f,1.84f,0,.15f),wood,new Vector3(1.14f,0,0),Vector3.one);
            for(int i=0;i<5;i++)door.Add(box,wood,new Vector3(.24f+i*.44f,.96f,-.09f),new Vector3(.424f,1.92f,.09f));
            door.Add(sphere,brass,new Vector3(1.91f,1.18f,-.185f),Vector3.one*.20f);
            door.Build(hinge);
            var house=entry.gameObject.AddComponent<ParkHouse>();
            house.DisplayName="키즈카페 입구";
            house.DoorHinge=hinge;
            house.OpenAngle=104;
            house.Inside=Group("Inside",entry,new Vector3(0,0,1.43f));
            house.Threshold=Group("Threshold",entry,new Vector3(0,0,-.44f));
            house.Welcome=Group("Welcome",entry,new Vector3(0,0,-2.56f));
            return house;
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

        static void CreateBallPit(Transform root,Mesh box,Mesh sphere,Mesh cylinder,Material mint,Material coral,Material blue,Material yellow,Material white)
        {
            var pit=Group("Ball pit and slide",root,new Vector3(BallPit.x,0,BallPit.y));
            var pieces=new Pieces();
            pieces.Add(cylinder,mint,new Vector3(0,.034f,0),new Vector3(8.70f,.028f,6.05f));
            pieces.Add(cylinder,coral,new Vector3(0,.23f,0),new Vector3(8.1f,.19f,5.5f));
            for(int i=0;i<36;i++)
            {
                float angle=i*2*Mathf.PI/36;
                var tangent=new Vector3(-PitRadii.x*Mathf.Sin(angle),0,PitRadii.y*Mathf.Cos(angle));
                pieces.Add(box,coral,new Vector3(PitRadii.x*Mathf.Cos(angle),.58f,PitRadii.y*Mathf.Sin(angle)),
                    new Vector3(.73f,1.08f,tangent.magnitude*2*Mathf.PI/36+.08f),Quaternion.LookRotation(tangent));
            }
            var colors=new[]{yellow,blue,mint,white,Paint("Rose balls","#F6ABD0"),Paint("Lavender balls","#C5AAEF")};
            var random=new System.Random(2418);
            for(int i=0;i<127;i++)
            {
                float angle=i*2.399963f,r=Mathf.Sqrt((i+.4f)/127);
                float diameter=.40f+(float)random.NextDouble()*.20f;
                pieces.Add(sphere,colors[random.Next(colors.Length)],new Vector3(r*3.67f*Mathf.Cos(angle),.50f+(float)random.NextDouble()*.21f,
                    r*2.37f*Mathf.Sin(angle)),Vector3.one*diameter);
            }
            pieces.Build(pit);
            CreateSlide(pit,box,cylinder,blue,yellow);
        }

        const float SlideCenter=2.1f,SlideWidth=1.65f,SlideTop=2.03f,LandingHeight=2.35f;
        const float LandingDepth=.92f,StepDepth=.53f;
        const int StepCount=5;

        static void CreateSlide(Transform pit,Mesh box,Mesh cylinder,Material blue,Material yellow)
        {
            var root=Group("Slide and stairs",pit,new Vector3(SlideCenter,0,0));
            var rails=new Pieces();
            var flatTread=PrimitiveMesh(PrimitiveType.Cube);
            float stairStart=SlideTop+LandingDepth;
            for(int step=0;step<StepCount;step++)
            {
                float height=LandingHeight*(StepCount-step)/StepCount;
                ParkGeometry.Place($"Stair {StepCount-step}",step==0?flatTread:box,root,
                    new Vector3(0,height/2,stairStart+(step+.5f)*StepDepth),
                    new Vector3(SlideWidth,height,step==0?StepDepth:StepDepth+.04f),blue);
            }
            // The chute and landing share the same top surface and finish flush with the stair tread.
            var profile=new List<Vector2>();
            for(int i=0;i<=24;i++)
            {
                float t=i/24f;
                profile.Add(new Vector2(Mathf.Lerp(-.44f,SlideTop,t),Mathf.Lerp(.68f,LandingHeight,Mathf.SmoothStep(0,1,t))));
            }
            profile.Add(new Vector2(stairStart,LandingHeight));
            var vertices=new List<Vector3>();
            var indices=new List<int>();
            for(int i=0;i<profile.Count;i++)
            {
                var point=profile[i];
                float half=SlideWidth/2;
                vertices.AddRange(new[]{new Vector3(-half,point.y,point.x),new Vector3(half,point.y,point.x),
                    new Vector3(-half,point.y-.16f,point.x),new Vector3(half,point.y-.16f,point.x)});
                if(i==0)continue;
                int v=(i-1)*4;
                foreach(int index in new[]{0,4,5,0,5,1,2,3,7,2,7,6,0,2,6,0,6,4,1,5,7,1,7,3})indices.Add(v+index);
                for(int side=-1;side<=1;side+=2)
                {
                    var previous=profile[i-1];
                    float x=side*(half-.035f);
                    var from=new Vector3(x,previous.y+.16f,previous.x);
                    var to=new Vector3(x,point.y+.16f,point.x);
                    rails.Beam(cylinder,yellow,from,to,.17f);
                }
            }
            int end=vertices.Count-4;
            indices.AddRange(new[]{0,1,3,0,3,2,end,end+2,end+3,end,end+3,end+1});
            var chute=new Mesh{name="Continuous slide and landing"};
            chute.SetVertices(vertices);
            chute.SetTriangles(indices,0);
            chute.RecalculateNormals();
            chute.RecalculateBounds();
            ParkGeometry.Place("Chute and landing",chute,root,Vector3.zero,Vector3.one,yellow);
            rails.Build(root);
        }

        static void CreateTable(Transform root,Mesh box,Mesh sphere,Mesh cylinder,Material wood,Material cushion,Material mint,Material white)
        {
            var table=Group("Guardian table and chairs",root,TablePosition);
            var pieces=new Pieces();
            pieces.Add(cylinder,wood,new Vector3(0,1.26f,0),new Vector3(1.88f,.12f,1.88f));
            pieces.Add(cylinder,wood,new Vector3(0,.65f,0),new Vector3(.33f,.63f,.33f));
            pieces.Add(cylinder,wood,new Vector3(0,.11f,0),new Vector3(.91f,.09f,.91f));
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*1.36f;
                pieces.Add(box,cushion,new Vector3(x,.57f,.12f),new Vector3(.66f,.19f,.72f));
                pieces.Add(box,wood,new Vector3(x+side*.29f,.90f,.12f),new Vector3(.12f,1.05f,.67f));
                for(int offset=-1;offset<=1;offset+=2)
                for(int z=-1;z<=1;z+=2)
                    pieces.Add(box,wood,new Vector3(x+offset*.23f,.29f,.12f+z*.25f),new Vector3(.10f,.56f,.10f));
            }
            pieces.Add(sphere,white,new Vector3(0,1.46f,0),new Vector3(.36f,.33f,.36f));
            pieces.Beam(cylinder,mint,new Vector3(0,1.50f,0),new Vector3(0,1.95f,0),.065f);
            for(int i=0;i<5;i++)
            {
                float angle=i*2*Mathf.PI/5;
                pieces.Add(sphere,white,new Vector3(.15f*Mathf.Cos(angle),1.96f+.15f*Mathf.Sin(angle),0),Vector3.one*.19f);
            }
            pieces.Add(sphere,cushion,new Vector3(0,1.96f,-.05f),Vector3.one*.17f);
            pieces.Build(table);
        }
    }
}
