using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkCafeTerrace
    {
        internal static void Create(Transform root,float width,float depth,Material floor,Mesh box,Mesh sphere)
        {
            float halfWidth=width/2,halfDepth=depth/2;
            var grass=Paint("Sunlit cafe meadow","#78AD56");
            grass.SetFloat("_Variation",.063f);
            grass.SetFloat("_GrassDetail",.17f);
            var hills=Paint("Far cafe hills","#6E9E6A");
            hills.SetFloat("_Variation",.035f);
            var grassMesh=Meadow(halfWidth,halfDepth);
            var meadow=ParkGeometry.Place("Continuous terrace meadow",grassMesh,root,Vector3.zero,Vector3.one,grass);
            meadow.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            ParkCafeFloor.Create(root,width,depth,floor);
            ParkGeometry.Place("Meadow approach path",ApproachPath(halfDepth),root,Vector3.zero,Vector3.one,
                Paint("Cream gravel","#EBD39D")).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;

            var landscape=Group("Distant playground hills",root,Vector3.zero);
            for(int i=0;i<14;i++)
            {
                float angle=(i+.22f)*2*Mathf.PI/14;
                var p=new Vector3(Mathf.Cos(angle)*(46+i%3*6),-3.6f,Mathf.Sin(angle)*(45+i%4*5));
                var size=new Vector3(34+i%4*5,10+i%3*2.3f,31);
                ParkGeometry.Place("Rolling meadow hill",sphere,landscape,p,size,hills);
            }
            ParkPlaygroundNeighborhood.Create(root,halfWidth,halfDepth,box,sphere);
            BuildBorders(root,halfWidth,halfDepth,box,sphere);
        }

        static void BuildBorders(Transform root,float halfWidth,float halfDepth,Mesh box,Mesh sphere)
        {
            var railing=Paint("Terrace rail","#EADBA7");
            var planter=Paint("Cafe flowerbed timber","#C28C63");
            var soil=Paint("Cafe flowerbed soil","#7F5D44");
            bool hasFlowers=ParkFlowerPlanting.TryLoadFlowers(out var flowers,out var flowerMaterial);
            const float soilY=.4f,soilThickness=.065f;
            var leaf=hasFlowers?null:Paint("Terrace garden leaf","#72A756");
            var petals=hasFlowers?null:new[]{Paint("Terrace petal pink","#F595A3"),Paint("Terrace petal vanilla","#FFF0CD"),
                Paint("Terrace petal lavender","#AF98D6"),Paint("Terrace petal yellow","#F9D46E")};
            var borders=Group("Low rails and flowerbeds",root,Vector3.zero);
            var pieces=new Pieces();
            int planted=0;
            foreach(int side in new[]{-1,1})
            {
                float x=side*(halfWidth+.85f);
                for(int row=0;row<3;row++)
                {
                    float z=-6.9f+row*4.65f;
                    for(int end=-1;end<=1;end+=2)
                        pieces.Add(box,railing,new Vector3(x,.39f,z+end*1.55f),new Vector3(.18f,.78f,.21f));
                    for(int rail=0;rail<2;rail++)
                        pieces.Add(box,railing,new Vector3(x,.28f+rail*.28f,z),new Vector3(.13f,.12f,3.12f));
                }
                for(int bed=0;bed<2;bed++)
                {
                    float z=-halfDepth-1.4f;
                    float bedX=side*(4.95f+bed*4.15f);
                    pieces.Add(box,planter,new Vector3(bedX,.195f,z),new Vector3(3.15f,.39f,1.08f));
                    pieces.Add(box,soil,new Vector3(bedX,soilY,z),new Vector3(2.90f,soilThickness,.87f));
                    if(hasFlowers)
                    {
                        planted+=ParkFlowerPlanting.FillPlanter(borders,flowers,flowerMaterial,
                            new Vector3(bedX,soilY+soilThickness/2,z),.69f,planted);
                        continue;
                    }
                    for(int i=0;i<7;i++)
                    {
                        float point=bedX-1.19f+i*.39f;
                        float depth=z+(i%2==0?-.12f:.15f);
                        float height=.57f+(i%3)*.07f;
                        pieces.Add(sphere,leaf,new Vector3(point,.52f,depth),new Vector3(.32f,.25f,.28f));
                        pieces.Add(sphere,petals[(i+bed+(side==1?1:0))%petals.Length],new Vector3(point,height,depth-.04f),
                            new Vector3(.28f,.22f,.28f));
                    }
                }
            }
            pieces.Build(borders);
            if(planted>0)Debug.Log($"CAFE FLOWERS: {planted} Pandazole flowers in the front planters.");
        }

        static Mesh Meadow(float x,float z)
        {
            const float far=210;
            var vertices=new[]{
                new Vector3(-far,0,-far),new Vector3(-far,0,far),new Vector3(far,0,far),new Vector3(far,0,-far),
                new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(x,0,-z)
            };
            var triangles=new List<int>(24);
            for(int i=0;i<4;i++)
            {
                int next=(i+1)%4;
                triangles.AddRange(new[]{i,next,next+4,i,next+4,i+4});
            }
            var mesh=new Mesh{name="Terrace meadow with flush play area"};
            mesh.vertices=vertices;
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh ApproachPath(float halfDepth)
        {
            var points=new List<Vector3>();
            var triangles=new List<int>();
            for(int i=0;i<=28;i++)
            {
                float t=i/28f;
                float x=1.4f+Mathf.Sin(t*2.6f)*.52f;
                float z=-halfDepth-.009f-t*2.57f;
                points.Add(new Vector3(x-1.55f,.012f,z));
                points.Add(new Vector3(x+1.55f,.012f,z));
                if(i==0)continue;
                int edge=2*(i-1);
                triangles.AddRange(new[]{edge,edge+1,edge+2,edge+1,edge+3,edge+2});
            }
            var mesh=new Mesh{name="Terrace gravel path"};
            mesh.SetVertices(points);
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
