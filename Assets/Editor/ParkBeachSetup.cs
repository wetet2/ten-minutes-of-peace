using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkBeachSetup
    {
        readonly System.Random random=new System.Random(73581);
        readonly Material sand=Surface("Warm coral sand","#D8BD8F",0);
        readonly Material bark=Surface("Weathered coconut bark","#967654",1);
        readonly Material forestBark=Surface("Ancient rainforest bark","#796C4E",4);
        readonly Material rock=ParkBeachMountainSetup.RockMaterial();
        readonly Material[] leaves={Surface("Sunlit palm leaf","#648D29",3),Surface("Emerald understory","#245322",3),
            Surface("Fresh broad leaves","#49832C",3),Surface("Deep tropical canopy","#254E29",3)};
        readonly Material canopy=ParkBeachMountainSetup.Foliage("Dense broadleaf tree canopy",new Color32(55,95,31,255));
        readonly Mesh crown=ParkBeachMountainSetup.Crown();
        readonly Mesh forkedWood=ParkGeometry.Trunk();
        readonly Material sunlitCanopy=ParkBeachMountainSetup.Foliage("Sunlit jungle crowns",new Color32(78,119,49,255));
        readonly Mesh bough=ParkIslandLeaves.Bough();
        readonly Material boughPaint=ParkIslandLeaves.Material("Rainforest leaf sprays",new Color(.54f,.68f,.47f));
        readonly Mesh leaf=ParkBeachMeshes.Leaf(1,.46f,.13f);
        readonly Mesh banana=ParkBeachMeshes.Leaf(1,.40f,.35f,18);
        readonly Mesh frond=ParkBeachMeshes.PalmFrond();
        readonly Mesh sphere=PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh cylinder=PrimitiveMesh(PrimitiveType.Cylinder);
        readonly List<Rect> obstacles=new List<Rect>();
        readonly ParkBeachShorePlacement shoreline=new ParkBeachShorePlacement();

        internal static ParkVenue Create(Transform parent)=>new ParkBeachSetup().Build(parent);

        ParkVenue Build(Transform parent)
        {
            foreach(var material in leaves)material.SetFloat("_Wind",.055f);
            ParkGeometry.Place("Continuous beach and visible seabed",ParkBeachMeshes.Ground(false),parent,Vector3.zero,Vector3.one,sand);
            var oceanMaterial=new Material(Shader.Find("PeacePark/Beach Water")){name="Clear turquoise coastal water"};
            var sea=ParkGeometry.Place("Animated transparent sea",ParkBeachMeshes.Ground(true),parent,Vector3.zero,Vector3.one,oceanMaterial);
            sea.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            ParkBeachMountainSetup.Create(parent);

            foreach(var palm in new[]{new Vector3(-13,.18f,-3),new Vector3(-10,.18f,5.5f),new Vector3(-15,.18f,9),
                new Vector3(11,.18f,5.5f),new Vector3(15,.18f,2),new Vector3(18,.18f,9),new Vector3(-21,.18f,2),new Vector3(24,.18f,8)})
                Palm(parent,palm,Range(7.4f,10.5f),palm.x<0?Range(1,2.4f):Range(-2.4f,-1));

            for(int row=0;row<5;row++)
            {
                var plants=new Pieces();
                for(int column=-8;column<=8;column++)
                {
                    float x=column*4.5f+Range(-1.9f,1.9f),z=12+row*5.5f+Range(-1.3f,1.3f);
                    if(row==0&&Mathf.Abs(x)<2.8f)continue;
                    Tree(plants,new Vector3(x,.18f,z),Range(6.8f,10.3f)+row*.38f);
                }
                BuildPlants(plants,parent,$"Layer {row+1} of tropical canopy");
            }
            var emergent=new Pieces();
            Tree(emergent,new Vector3(-17,.18f,23),16.5f);
            BuildPlants(emergent,parent,"Tall rainforest hardwood");
            for(int side=-1;side<=1;side+=2)
            {
                var plants=new Pieces();
                for(int i=0;i<9;i++)Tree(plants,new Vector3(side*Range(23,32),.18f,-3+i*3.6f),Range(6,10));
                BuildPlants(plants,parent,side<0?"Western headland forest":"Eastern headland forest");
            }
            for(int row=0;row<4;row++)
            {
                var plants=new Pieces();
                for(int column=-20;column<=20;column++)
                {
                    float x=column*1.25f+Range(-.4f,.4f);
                    float z=8.9f+row*1.4f+Range(-.5f,.5f)-Mathf.Max(0,Mathf.Abs(x)-9)*.62f;
                    if(Mathf.Abs(x)<1.6f&&z<14)continue;
                    Rosette(plants,new Vector3(x,.18f,z),Range(1.3f,2.5f),column%3==0);
                }
                BuildPlants(plants,parent,$"Dense coastal understory {row+1}");
            }
            for(int row=0;row<4;row++)
            {
                var plants=new Pieces();
                for(int column=-12;column<=12;column++)
                {
                    float x=column*2.6f+Range(-.8f,.8f),z=14.8f+row*3.0f+Range(-.8f,.8f);
                    if(row==0&&Mathf.Abs(x)<1.7f)continue;
                    Rosette(plants,new Vector3(x,.18f,z),Range(2.9f,5.0f),column%2==0);
                }
                BuildPlants(plants,parent,$"Layered midstory foliage {row+1}");
            }
            var stones=new Pieces();
            for(int side=-1;side<=1;side+=2)
            for(int i=0;i<17;i++)
            {
                float x=side*(16.5f+i*.80f+Range(-1,1)),z=-8+i*1.4f+Range(-1.0f,1.0f);
                float height=Range(1.0f,i<5?2.1f:2.8f);
                var size=new Vector3(height*Range(.9f,1.4f),height,height*Range(.7f,1.1f));
                stones.Add(ParkBeachMeshes.Rock(i+side*29),rock,new Vector3(x,ParkBeachMeshes.Height(x,z)+height*.35f,z),
                    size,Quaternion.Euler(0,i*41,0));
                shoreline.AddRock(x,z,Mathf.Max(size.x,size.z)*1.2f);
            }
            stones.Build(Group("Granite headlands",parent,Vector3.zero));
            Flowers(parent);
            ShoreDetails(parent);
            ParkBeachWildlifeSetup.Create(parent,shoreline);
            var entrances=new List<ParkHouse>();
            for(int i=0;i<5;i++)
            {
                var entrance=Group($"Bush arrival {i+1}",parent,new Vector3((i-2)*3.5f,.18f,8.6f));
                var house=entrance.gameObject.AddComponent<ParkHouse>();
                house.DisplayName="해변 수풀";
                house.BushEntrance=true;
                house.Inside=Group("Hidden behind leaves",entrance,new Vector3(0,0,1.1f));
                house.Threshold=Group("Peeking through leaves",entrance,Vector3.zero);
                house.Welcome=Group("Greeting on the sand",entrance,new Vector3(0,0,-2.4f));
                house.SecondDoorHinge=Group("Entrance reference",entrance,Vector3.zero);
                house.OpenAngle=0;
                entrances.Add(house);
                var cover=new Pieces();
                for(int plant=-1;plant<=1;plant++)
                    Rosette(cover,entrance.position+new Vector3(plant*.56f,0,-.36f),.95f,true);
                BuildPlants(cover,parent,$"Low leaves framing arrival {i+1}");
            }
            var atmosphere=parent.gameObject.AddComponent<ParkBeachAtmosphere>();
            atmosphere.SkyMaterial=new Material(Shader.Find("PeacePark/Island Sky")){name="Tropical sky with drifting clouds"};
            return new ParkVenue
            {
                Name="해변가",Root=parent.gameObject,Houses=entrances.ToArray(),LawnHeight=.18f,
                WalkArea=Rect.MinMaxRect(-10,-4.8f,10,7.2f),WalkObstacles=obstacles.ToArray(),
                Focus=new Vector3(0,7,1.4f),Distance=43,Yaw=-7,Pitch=12,MaxDistance=62,
                CameraPanLimits=new Vector2(70,90),CloseFocusHeight=.9f,
                FogEnd=370,Sky=new Color32(151,206,220,255)
            };
        }

        void Palm(Transform parent,Vector3 point,float height,float lean)
        {
            var pieces=new Pieces();
            var rotation=Quaternion.Euler(0,Range(-30,30),0);
            pieces.Add(ParkBeachMeshes.Trunk(height,lean),bark,point,Vector3.one,rotation);
            var crown=point+rotation*ParkBeachMeshes.TrunkPoint(height,lean,1);
            for(int i=0;i<12;i++)
            {
                var turn=Quaternion.Euler(Range(-13,22),i*137.5f,Range(-8,8));
                pieces.Add(frond,leaves[i%3==0?2:0],crown,Vector3.one*Range(.78f,1.1f),turn);
            }
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                pieces.Add(sphere,bark,crown+new Vector3(Mathf.Sin(a)*.3f,-.23f,Mathf.Cos(a)*.3f),new Vector3(.38f,.48f,.37f));
            }
            pieces.Build(Group("Coconut palm",parent,Vector3.zero));
            obstacles.Add(Rect.MinMaxRect(point.x-.7f,point.z-.7f,point.x+.7f,point.z+.7f));
        }

        void Tree(Pieces pieces,Vector3 point,float height)
        {
            if(ParkBeachMeshes.LandDistance(point.x,point.z)<2)return;
            point.y=Mathf.Max(point.y,ParkBeachMountainSetup.Height(point.x,point.z)-.1f);
            height=Mathf.Max(3.6f,height-point.y*.76f);
            var fork=point+new Vector3(Range(-.045f,.045f),.57f,Range(-.035f,.035f))*height;
            Wood(point,fork,height*.15f);
            float spread=height*Range(.20f,.29f);
            float azimuth=Range(0,360);
            for(int limb=0;limb<4;limb++)
            {
                float yaw=azimuth+limb*99+Range(-17,17);
                var outwards=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                var start=Vector3.Lerp(point,fork,Range(.72f,.98f));
                var elbow=fork+outwards*spread*.46f+Vector3.up*height*.072f;
                var tip=fork+outwards*spread+Vector3.up*height*Range(.16f,.26f);
                Wood(start,elbow,height*.082f);
                Wood(elbow,tip,height*.050f);
                float radius=spread*Range(.78f,1.07f);
                var turn=Quaternion.Euler(0,yaw,0);
                pieces.Add(crown,limb%3==0?sunlitCanopy:canopy,tip,new Vector3(radius,radius*.68f,radius*.92f),turn);
                // Fine leaf sprays break up the outline; most foliage belongs to one connected crown.
                for(int spray=0;spray<5;spray++)
                {
                    var twist=Quaternion.Euler(0,yaw+spray*72,0);
                    var offset=twist*new Vector3(0,.22f+(spray%2)*.16f,.80f)*radius;
                    pieces.Add(bough,boughPaint,tip+offset,Vector3.one*(radius*.72f),twist);
                }
            }
            var leader=fork+Vector3.up*height*.30f;
            Wood(fork,leader,height*.068f);
            pieces.Add(crown,sunlitCanopy,leader,new Vector3(spread*1.10f,height*.14f,spread),Quaternion.Euler(0,azimuth,0));

            void Wood(Vector3 from,Vector3 to,float girth)
            {
                var axis=to-from;
                pieces.Add(forkedWood,forestBark,from,new Vector3(girth,axis.magnitude/2.55f,girth),
                    Quaternion.FromToRotation(Vector3.up,axis));
            }
        }

        void Rosette(Pieces pieces,Vector3 point,float height,bool broad)
        {
            for(int i=0;i<13;i++)
            {
                float angle=i*137.5f+point.x*19;
                float length=height*Range(.68f,1.28f);
                pieces.Add(broad?banana:leaf,leaves[i%3],point+Vector3.up*Range(.05f,.4f),
                    new Vector3(length*(broad?1.3f:.85f),length,length),Quaternion.Euler(Range(-65,-13),angle,0));
            }
        }

        void ShoreDetails(Transform parent)
        {
            var pieces=new Pieces();
            var ivory=Surface("Shell ivory","#EADAC0",2);
            var pink=Surface("Shell peach","#D9AC8E",2);
            for(int i=0;i<144;i++)
            {
                var point=shoreline.Point(i%3,Range(.03f,.97f),Range(.7f,4.2f));
                if(!shoreline.Clear(point,.12f))continue;
                float size=Range(.035f,.11f);
                pieces.Add(sphere,i%4==0?pink:ivory,point+Vector3.up*size*.18f,
                    new Vector3(size,size*.36f,size*.75f),Quaternion.Euler(0,i*73,0));
            }
            pieces.Build(Group("Small shells in the tide line",parent,Vector3.zero));
        }

        void Flowers(Transform parent)
        {
            var flowers=new Pieces();
            var coral=Surface("Wild hibiscus coral","#DC5039",3);
            var gold=Surface("Hibiscus golden pollen","#E9B537",2);
            for(int i=0;i<24;i++)
            {
                float x=(i%2==0?-1:1)*Range(11,22),z=Range(1,10);
                if(ParkBeachMeshes.LandDistance(x,z)<2)continue;
                var center=new Vector3(x,Range(.8f,1.6f),z);
                flowers.Beam(cylinder,leaves[1],new Vector3(x,.18f,z),center,.025f);
                for(int petal=0;petal<5;petal++)
                {
                    float a=petal*Mathf.PI*2/5;
                    flowers.Add(sphere,coral,center+new Vector3(Mathf.Cos(a)*.19f,Mathf.Sin(a)*.19f,0),
                        new Vector3(.30f,.35f,.08f),Quaternion.Euler(0,0,petal*72));
                }
                flowers.Add(sphere,gold,center+Vector3.back*.06f,Vector3.one*.13f);
            }
            flowers.Build(Group("Wild hibiscus around the bay",parent,Vector3.zero));
        }

        static void BuildPlants(Pieces pieces,Transform parent,string name)=>pieces.Build(Group(name,parent,Vector3.zero));
        float Range(float from,float to)=>Mathf.Lerp(from,to,(float)random.NextDouble());
        static Material Surface(string name,string hex,int kind)
        {
            ColorUtility.TryParseHtmlString(hex,out var color);
            var material=new Material(Shader.Find("PeacePark/Beach Surface")){name=name,color=color};
            material.SetFloat("_Kind",kind);return material;
        }
    }
}
