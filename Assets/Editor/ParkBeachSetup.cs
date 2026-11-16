using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkBeachSetup
    {
        readonly System.Random random=new System.Random(73581);
        readonly Material sand=Surface("Warm coral sand","#C8AE80",0);
        readonly Material bark=Surface("Weathered coconut bark","#967654",1);
        readonly Material rock=Surface("Salt worn granite","#A89F87",2);
        readonly Material[] leaves={Surface("Sunlit palm leaf","#65852B",3),Surface("Emerald understory","#346127",3),
            Surface("Fresh broad leaves","#608633",3),Surface("Deep tropical canopy","#365B2E",3)};
        readonly Mesh leaf=ParkBeachMeshes.Leaf(1,.46f,.13f);
        readonly Mesh banana=ParkBeachMeshes.Leaf(1,.40f,.35f,18);
        readonly Mesh frond=ParkBeachMeshes.PalmFrond();
        readonly Mesh sphere=PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh cylinder=PrimitiveMesh(PrimitiveType.Cylinder);
        readonly List<Rect> obstacles=new List<Rect>();

        internal static ParkVenue Create(Transform parent)=>new ParkBeachSetup().Build(parent);

        ParkVenue Build(Transform parent)
        {
            foreach(var material in leaves)material.SetFloat("_Wind",.055f);
            ParkGeometry.Place("Continuous beach and visible seabed",ParkBeachMeshes.Ground(false),parent,Vector3.zero,Vector3.one,sand);
            var oceanMaterial=new Material(Shader.Find("PeacePark/Beach Water")){name="Clear turquoise coastal water"};
            var sea=ParkGeometry.Place("Animated transparent sea",ParkBeachMeshes.Ground(true),parent,Vector3.zero,Vector3.one,oceanMaterial);
            sea.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;

            foreach(var palm in new[]{new Vector3(-13,.18f,-3),new Vector3(-10,.18f,5.5f),new Vector3(-15,.18f,9),
                new Vector3(11,.18f,5.5f),new Vector3(15,.18f,2),new Vector3(18,.18f,9),new Vector3(-21,.18f,2),new Vector3(24,.18f,8)})
                Palm(parent,palm,Range(7.4f,10.5f),palm.x<0?Range(1,2.4f):Range(-2.4f,-1));

            for(int row=0;row<5;row++)
            {
                var plants=new Pieces();
                for(int column=-8;column<=8;column++)
                {
                    float x=column*4.5f+Range(-1.2f,1.2f),z=12+row*5.5f+Range(-.8f,.8f);
                    if(row==0&&Mathf.Abs(x)<2.8f)continue;
                    Tree(plants,new Vector3(x,.18f,z),Range(7,11)+row*.6f);
                }
                BuildPlants(plants,parent,$"Layer {row+1} of tropical canopy");
            }
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
                float x=side*(16.5f+i*.62f+Range(-1,1)),z=-7+i*.82f+Range(-1.4f,1.4f);
                float height=Range(1.0f,3.5f);
                stones.Add(ParkBeachMeshes.Rock(i+side*29),rock,new Vector3(x,ParkBeachMeshes.Height(x,z)+height*.35f,z),
                    new Vector3(height*Range(.9f,1.4f),height,height*Range(.7f,1.1f)),Quaternion.Euler(0,i*41,0));
            }
            stones.Build(Group("Granite headlands",parent,Vector3.zero));
            ShoreDetails(parent);
            ParkBeachWildlifeSetup.Create(parent);
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
            parent.gameObject.AddComponent<ParkBeachAtmosphere>();
            return new ParkVenue
            {
                Name="해변가",Root=parent.gameObject,Houses=entrances.ToArray(),LawnHeight=.18f,
                WalkArea=Rect.MinMaxRect(-10,-4.8f,10,7.2f),WalkObstacles=obstacles.ToArray(),
                Focus=new Vector3(0,2.5f,1.4f),Distance=39,Yaw=-10,Pitch=26,MaxDistance=57,
                FogEnd=210,Sky=new Color32(126,202,232,255)
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
            pieces.Add(cylinder,bark,point+Vector3.up*height*.37f,new Vector3(.38f,height*.37f,.38f));
            for(int branch=0;branch<9;branch++)
            {
                float angle=branch*2.4f+point.x;
                var crown=point+new Vector3(Mathf.Cos(angle)*height*.20f,height*(.60f+.042f*branch),Mathf.Sin(angle)*height*.20f);
                pieces.Beam(cylinder,bark,point+Vector3.up*height*.51f,crown,.12f);
                for(int sprig=0;sprig<10;sprig++)
                {
                    float yaw=angle*Mathf.Rad2Deg+sprig*137.5f;
                    var offset=Quaternion.Euler(0,yaw,0)*Vector3.forward*Range(.05f,.65f);
                    pieces.Add(leaf,leaves[branch%2==0?3:1],crown+offset,Vector3.one*Range(1.5f,2.4f),Quaternion.Euler(Range(-32,14),yaw,Range(-20,20)));
                }
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
            for(int i=0;i<115;i++)
            {
                float x=Range(-15,15),z=ParkBeachMeshes.Shore(x)+Range(.7f,3.0f);
                float size=Range(.035f,.11f);
                pieces.Add(sphere,i%4==0?pink:ivory,new Vector3(x,ParkBeachMeshes.Height(x,z)+size*.18f,z),
                    new Vector3(size,size*.36f,size*.75f),Quaternion.Euler(0,i*73,0));
            }
            pieces.Build(Group("Small shells in the tide line",parent,Vector3.zero));
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
