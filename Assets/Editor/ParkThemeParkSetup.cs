using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkThemeParkSetup
    {
        const float ParkScale=2f/3f;
        const int TrainCarCount=6;
        static readonly Vector3 WheelLocation=new Vector3(-31.4f,0,-5.5f);
        static readonly Vector3 CarouselLocation=new Vector3(28.6f,0,2.6f);
        readonly Mesh box = ParkSurface.RoundedBox();
        readonly Mesh cube = PrimitiveMesh(PrimitiveType.Cube);
        readonly Mesh sphere = PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Material grass = Paint("Spring grass", "#9DC65C");
        readonly Material mint = Paint("Mint enamel", "#6FC9AF");
        readonly Material yellow = Paint("Custard yellow", "#FFCC56");
        readonly Material cream = Paint("Vanilla canvas", "#FFF0CB");
        readonly Material coral = Paint("Coral enamel", "#F58171");
        readonly Material blue = Paint("Sky blue enamel", "#73BAE8");
        readonly Material pink = Paint("Pink canvas", "#F5A7AA");
        readonly Material ink = Paint("Licorice", "#39303A");
        readonly Material steel = Paint("Polished steel", "#788697");
        readonly Material wood = Paint("Honey timber", "#AD7754");
        readonly Material paving = Paint("Peach paving", "#ECC4A8");
        readonly Material leaf = Paint("Apple green", "#83AF4C");
        readonly Material lightLeaf = Paint("Lime green", "#A9CB5B");

        internal static ParkVenue Create(Transform root) => new ParkThemeParkSetup().Build(root);

        ParkVenue Build(Transform root)
        {
            grass.SetFloat("_GrassDetail",.11f);
            ParkGeometry.Place("Theme park lawn", cube, root, new Vector3(0,-.23f,0),new Vector3(500,.46f,437.5f),grass);
            MakeForecourt(root);
            MakeTrack(root);
            var deck = MakeStation(root);
            var train = MakeTrain(root);
            var popcorn = MakeShop(root, new Vector3(-9.5f,.015f,-16.8f), "Popcorn shop", "팝콘 가게",coral);
            var balloons = MakeShop(root, new Vector3(8.3f,.015f,-16.8f), "Balloon shop", "풍선 가게",mint);
            MakeLandscaping(root);
            var wheel=MakeFerrisWheel(root);
            var carousel=MakeCarousel(root);
            return ScaleVenue(new ParkVenue
            {
                Name="놀이공원", Root=root.gameObject, Houses=new[]{popcorn,balloons}, Train=train, Wheel=wheel, Carousel=carousel, Path=deck, LawnHeight=.015f,
                WalkArea=Rect.MinMaxRect(-36f,ParkElephantTrain.StationRailZ+3.1f,36f,-5f),
                PlazaWalkArea=Rect.MinMaxRect(-3.8f,-13.5f,3.8f,-5.5f),
                WalkObstacles=new[]{
                    ShopFootprint(popcorn),
                    ShopFootprint(balloons),
                    // Keep the route into the central plaza between the two landscaped beds.
                    Rect.MinMaxRect(-37f,-11.7f,-4.3f,-4f),
                    Rect.MinMaxRect(4.3f,-11.7f,37f,-4f),
                    Rect.MinMaxRect(10.98f,-25.49f,11.82f,-24.65f),
                    Rect.MinMaxRect(15.88f,-25.49f,16.72f,-24.65f)},
                Focus=new Vector3(-1,13f,-6), Distance=81, Yaw=-4, Pitch=27, MaxDistance=132,
                CameraPanLimits=new Vector2(58,43), FogStart=135, FogEnd=212, FarClipDistance=220,
                Sky=new Color32(164,208,239,255),
                Preview=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Venues/ThemePark.jpg")
            }, Vector3.one*ParkScale, viewScale: ParkScale);
        }

        void MakeTrack(Transform root)
        {
            var track=Group("Oval roller coaster track",root,Vector3.zero);
            var ballast=Paint("Track gravel", "#87796D");
            RailBand(track, "Track bed", ballast, 0, 2.34f, -.105f, .21f);
            RailBand(track, "Inner rail",steel,-.79f,.11f,0,.086f);
            RailBand(track, "Outer rail",steel,.79f,.11f,0,.086f);
            var sleepers=new Pieces();
            int count=Mathf.RoundToInt(ParkElephantTrain.Length/.51f);
            for(int i=0;i<count;i++)
            {
                float distance=i*ParkElephantTrain.Length/count;
                var p=ParkElephantTrain.TrackPoint(distance);
                var tangent=ParkElephantTrain.TrackPoint(distance+.18f)-ParkElephantTrain.TrackPoint(distance-.18f);
                sleepers.Add(cube,wood,p-Vector3.up*.11f,new Vector3(2.12f,.11f,.22f),Quaternion.LookRotation(tangent));
            }
            sleepers.Build(track);
            MakeCoasterHill(track);
        }

        void MakeCoasterHill(Transform track)
        {
            const int segments=88;
            var vertices=new List<Vector3>();
            var uv=new List<Vector2>();
            var triangles=new List<int>();
            for(int i=0;i<=segments;i++)
            {
                var rail=ParkElephantTrain.TrackPoint(ParkElephantTrain.FarStraightStart+ParkElephantTrain.Straight*i/segments);
                float rise=rail.y-ParkElephantTrain.RailHeight;
                float foot=1.67f+rise*1.26f;
                vertices.Add(new Vector3(rail.x,0,rail.z-foot));
                vertices.Add(new Vector3(rail.x,rise,rail.z-1.15f));
                vertices.Add(new Vector3(rail.x,rise,rail.z+1.15f));
                vertices.Add(new Vector3(rail.x,0,rail.z+foot));
                for(int j=0;j<4;j++)
                {
                    var p=vertices[i*4+j];
                    uv.Add(new Vector2(p.x/30,p.z/30));
                    if(i==segments||j==3)continue;
                    int k=i*4+j;
                    triangles.AddRange(new[]{k,k+4,k+1,k+1,k+4,k+5});
                }
            }
            var mesh=new Mesh{name="Grassy coaster hill"};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            ParkGeometry.Place("Tall landscaped railway hill",mesh,track,Vector3.down*.006f,Vector3.one,grass);
        }

        static void RailBand(Transform parent,string name,Material color,float offset,float width,float top,float depth)
        {
            const int segments=640;
            var vertices=new Vector3[(segments+1)*4];
            var indices=new int[segments*24];
            for(int i=0;i<=segments;i++)
            {
                float distance=i*ParkElephantTrain.Length/segments;
                var p=ParkElephantTrain.TrackPoint(distance);
                var forward=(ParkElephantTrain.TrackPoint(distance+.12f)-ParkElephantTrain.TrackPoint(distance-.12f)).normalized;
                var right=Vector3.Cross(Vector3.up,forward).normalized;
                p+=Vector3.up*top+right*offset;
                int ring=i*4;
                vertices[ring]=p-right*(width/2)-Vector3.up*depth;
                vertices[ring+1]=p+right*(width/2)-Vector3.up*depth;
                vertices[ring+2]=p+right*(width/2);
                vertices[ring+3]=p-right*(width/2);
                if(i==segments)continue;
                for(int edge=0;edge<4;edge++)
                {
                    int a=ring+edge,b=ring+(edge+1)%4,k=i*24+edge*6;
                    indices[k]=a;indices[k+1]=b;indices[k+2]=a+4;
                    indices[k+3]=b;indices[k+4]=b+4;indices[k+5]=a+4;
                }
            }
            var mesh=new Mesh{name=name,vertices=vertices,triangles=indices};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            ParkGeometry.Place(name,mesh,parent,Vector3.zero,Vector3.one,color);
        }

        MeshFilter MakeStation(Transform root)
        {
            const float stationX=1.2f, deckEnd=16.53f;
            float rearCarX=ParkElephantTrain.TrackPoint(-(TrainCarCount-1)*ParkElephantTrain.CarSpacing).x;
            float deckStart=rearCarX-stationX-3.76f;
            float deckMiddle=(deckStart+deckEnd)*.5f;
            float deckLength=deckEnd-deckStart;
            var station=Group("Elephant train station",root,new Vector3(stationX,0,ParkElephantTrain.StationRailZ+4.65f));
            var trim=new Pieces();
            trim.Add(box,cream,new Vector3(deckMiddle,.055f,0),new Vector3(deckLength,.105f,5.3f));
            float firstPaver=deckStart+1.01f;
            int pavers=Mathf.FloorToInt((7.35f-firstPaver)/.61f)+1;
            for(int i=0;i<pavers;i++)
            {
                float x=firstPaver+i*.61f;
                trim.Add(cube,i%2==0?cream:paving,new Vector3(x,.11f,-2.43f),new Vector3(.58f,.021f,.24f));
            }
            const float canopyLength=5f;
            var roof=ParkGeometry.Extrude("Curved platform awning",new List<Vector2>{
                new Vector2(-2.10f,0),new Vector2(2.10f,0),new Vector2(2.1f,.30f),new Vector2(1.74f,.64f),
                new Vector2(1.03f,.92f),new Vector2(0,1.00f),new Vector2(-1.03f,.92f),new Vector2(-1.74f,.64f),new Vector2(-2.1f,.30f)},.95f);
            for(int lane=0;lane<8;lane++)
                trim.Add(roof,lane%2==0?coral:cream,new Vector3(12.65f+(lane-3.5f)*canopyLength/8,3.48f,.15f),
                    new Vector3(1,1,canopyLength/(8*.95f)),Quaternion.Euler(0,90,0));
            foreach(float x in new[]{10.20f,15.10f})
                foreach(float z in new[]{-1.96f,2.28f})
                {
                    trim.Add(box,wood,new Vector3(x,1.80f,z),new Vector3(.22f,3.40f,.22f));
                    trim.Add(box,cream,new Vector3(x,.41f,z),new Vector3(.36f,.70f,.36f));
                }
            float rearFenceX=deckStart+.76f;
            Fence(trim,new Vector3(rearFenceX,0,-1.2f),new Vector3(rearFenceX,0,-2.4f),mint);
            Fence(trim,new Vector3(7.65f,0,-1.2f),new Vector3(7.65f,0,-2.4f),mint);
            Fence(trim,new Vector3(rearFenceX,0,-2.4f),new Vector3(rearFenceX+2.55f,0,-2.4f),mint);
            Fence(trim,new Vector3(6.4f,0,-2.4f),new Vector3(7.65f,0,-2.4f),mint);
            trim.Build(station);
            var verts=new[]{
                new Vector3(deckStart,ParkElephantTrain.DeckHeight,2.51f),new Vector3(deckStart,ParkElephantTrain.DeckHeight,-2.52f),
                new Vector3(deckEnd,ParkElephantTrain.DeckHeight,-2.52f),new Vector3(deckEnd,ParkElephantTrain.DeckHeight,2.51f),
                new Vector3(deckStart,.018f,3.35f),new Vector3(deckEnd,.018f,3.35f)};
            var floor=new Mesh{name="Station walking surface",vertices=verts,triangles=new[]{0,2,1,0,3,2,4,3,0,4,5,3}};
            floor.RecalculateNormals();floor.RecalculateBounds();
            return ParkGeometry.Place("Station boarding deck",floor,station,Vector3.zero,Vector3.one,paving).GetComponent<MeshFilter>();
        }

        void Fence(Pieces parts,Vector3 start,Vector3 end,Material paint)
        {
            int gaps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(start,end)/.58f));
            for(int i=0;i<=gaps;i++)
            {
                var p=Vector3.Lerp(start,end,(float)i/gaps);
                bool endPost=i==0||i==gaps;
                parts.Add(cylinder,paint,p+Vector3.up*.58f,new Vector3(endPost?.15f:.085f,endPost?.55f:.41f,endPost?.15f:.085f));
                if(endPost)parts.Add(sphere,paint,p+Vector3.up*1.14f,Vector3.one*.23f);
            }
            foreach(float y in new[]{.38f,.97f})parts.Beam(cylinder,paint,start+Vector3.up*y,end+Vector3.up*y,.095f);
        }

        ParkHouse MakeShop(Transform root,Vector3 position,string name,string label,Material color)
        {
            var shop=Group(name,root,position);
            shop.localScale=new Vector3(1,1/ParkScale,1);
            var body=new Pieces();
            body.Add(box,cream,new Vector3(-1.38f,1.22f,1.09f),new Vector3(.62f,2.5f,2.98f));
            body.Add(box,cream,new Vector3(1.38f,1.22f,1.09f),new Vector3(.62f,2.5f,2.98f));
            body.Add(box,wood,new Vector3(0,1.2f,2.41f),new Vector3(3.3f,2.4f,.21f));
            body.Add(box,color,new Vector3(0,2.55f,1.12f),new Vector3(3.48f,.40f,3.21f));
            for(int i=0;i<8;i++)
                body.Add(box,i%2==0?color:cream,new Vector3(-1.58f+.45f*i,2.76f,-.26f),new Vector3(.45f,.19f,1.05f),Quaternion.Euler(-12,0,0));
            body.Add(sphere,yellow,new Vector3(0,3.08f,1.1f),new Vector3(.74f,.5f,.74f));
            body.Build(shop);
            var door=Group("Shop door hinge",shop,new Vector3(-1.07f,0,-.065f));
            ParkGeometry.Place("Painted shop door",box,door,new Vector3(1.07f,1.22f,0),new Vector3(2.14f,2.45f,.18f),color);
            ParkGeometry.Place("Brass pull",sphere,door,new Vector3(1.88f,1.16f,-.14f),Vector3.one*.17f,yellow);
            var house=shop.gameObject.AddComponent<ParkHouse>();
            house.DisplayName=label;house.DoorHinge=door;house.OpenAngle=-92;house.ExitSeconds=3.8f;
            house.Inside=Group("Inside shop",shop,new Vector3(0,0,1.40f));
            house.Threshold=Group("Shop threshold",shop,new Vector3(0,0,-.18f));
            house.Welcome=Group("Forecourt greeting point",shop,new Vector3(0,0,-3.20f));
            return house;
        }

        static Rect ShopFootprint(ParkHouse house)
        {
            var position=house.transform.localPosition;
            // Include the awning on the enlarged shop, plus room for the animal's body.
            const float clearance=1.02f;
            return Rect.MinMaxRect(position.x-1.99f-clearance,position.z-.88f-clearance,
                position.x+1.99f+clearance,position.z+3.00f+clearance);
        }
    }
}
