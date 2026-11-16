using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkBeachWildlifeSetup
    {
        readonly Mesh ball=PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh pole=PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Material coral=Paint("Crab coral shell","#D96837");
        readonly Material claw=Paint("Crab ivory tips","#F2B27C");
        readonly Material eye=Paint("Marine dark eyes","#253B40");
        readonly Material ivory=Paint("Scallop ivory","#F2D9AD");
        readonly Material rose=Paint("Scallop rose","#DBA096");
        readonly Material orange=Paint("Starfish orange","#DA7D43");
        readonly Material gold=Paint("Reef fish gold","#F5C747");
        readonly Material blue=Paint("Reef fish azure","#5AA4B8");
        readonly Material silver=Paint("Reef fish silver","#D6E8D0");

        internal static void Create(Transform parent)=>new ParkBeachWildlifeSetup().Build(parent);

        void Build(Transform parent)
        {
            var shore=Group("Seashells and starfish",parent,Vector3.zero);
            var pieces=new Pieces();
            var shell=Shell();
            var star=Starfish();
            var spot=Spot();
            for(int i=0;i<36;i++)
            {
                float x=Mathf.Sin(i*2.4f)*13.5f;
                float z=ParkBeachMeshes.Shore(x)+1.2f+(i%7)*.55f;
                float scale=.32f+(i%5)*.065f;
                pieces.Add(shell,i%3==0?rose:ivory,new Vector3(x,ParkBeachMeshes.Height(x,z)+.025f,z),
                    Vector3.one*scale,Quaternion.Euler(0,i*137.5f,0));
            }
            for(int i=0;i<9;i++)
            {
                float x=-11+i*2.8f;
                float z=ParkBeachMeshes.Shore(x)+1.65f+(i%3)*.85f;
                float scale=.39f+(i%3)*.08f;
                var point=new Vector3(x,ParkBeachMeshes.Height(x,z)+.035f,z);
                var rotation=Quaternion.Euler(0,i*67,0);
                pieces.Add(star,orange,point,Vector3.one*scale,rotation);
                for(int arm=0;arm<5;arm++)
                for(int dot=1;dot<5;dot++)
                {
                    float a=arm*Mathf.PI*2/5;
                    var at=new Vector3(Mathf.Sin(a)*dot*.16f,.11f-dot*.016f,Mathf.Cos(a)*dot*.16f)*scale;
                    pieces.Add(spot,claw,point+rotation*at,Vector3.one*(.042f*scale));
                }
            }
            pieces.Build(shore);
            for(int i=0;i<6;i++)Crab(parent,i);
            for(int i=0;i<12;i++)Fish(parent,i);
        }

        void Crab(Transform parent,int index)
        {
            var root=Group($"Sand crab {index+1}",parent,Vector3.zero);
            var pieces=new Pieces();
            pieces.Add(ball,coral,new Vector3(0,.19f,0),new Vector3(.53f,.25f,.38f));
            for(int side=-1;side<=1;side+=2)
            {
                pieces.Beam(pole,coral,new Vector3(side*.13f,.26f,.10f),new Vector3(side*.18f,.40f,.15f),.035f);
                pieces.Add(ball,eye,new Vector3(side*.18f,.40f,.15f),Vector3.one*.07f);
                pieces.Beam(pole,coral,new Vector3(side*.23f,.17f,.13f),new Vector3(side*.43f,.22f,.36f),.045f);
                pieces.Add(ball,coral,new Vector3(side*.43f,.25f,.40f),new Vector3(.17f,.14f,.20f));
                for(int finger=-1;finger<=1;finger+=2)
                    pieces.Add(ball,claw,new Vector3(side*.43f+finger*.055f,.25f,.52f),new Vector3(.055f,.075f,.15f),Quaternion.Euler(0,-finger*16,0));
            }
            pieces.Build(root);
            var legs=new List<Transform>();
            for(int side=-1;side<=1;side+=2)
            for(int leg=0;leg<4;leg++)
            {
                var joint=Group("Scuttling leg",root,new Vector3(side*.21f,.17f,.08f-leg*.07f));
                var limbs=new Pieces();
                var knee=new Vector3(side*.21f,.015f,.12f-leg*.075f);
                var foot=new Vector3(side*.34f,-.15f,.15f-leg*.10f);
                limbs.Beam(pole,coral,Vector3.zero,knee,.027f);
                limbs.Beam(pole,coral,knee,foot,.021f);
                limbs.Build(joint);legs.Add(joint);
            }
            var motion=root.gameObject.AddComponent<ParkBeachWildlife>();
            motion.Center=new Vector3(-10+index*4,0,1.9f+index%2*.65f);
            motion.Radius=new Vector2(1.7f,.25f);motion.Speed=.29f+index*.018f;motion.Phase=index*1.7f;
            motion.Legs=legs.ToArray();motion.Pose(0);
        }

        void Fish(Transform parent,int index)
        {
            var root=Group($"Reef fish {index+1}",parent,Vector3.zero);
            var color=index%3==0?gold:index%3==1?blue:silver;
            var pieces=new Pieces();
            pieces.Add(ball,color,Vector3.zero,new Vector3(.25f,.35f,.75f));
            pieces.Add(ball,silver,new Vector3(0,-.06f,.08f),new Vector3(.24f,.20f,.53f));
            for(int side=-1;side<=1;side+=2)
            {
                pieces.Add(ball,eye,new Vector3(side*.102f,.055f,.24f),Vector3.one*.058f);
                pieces.Add(Fin(),color,new Vector3(side*.09f,-.015f,-.04f),new Vector3(side*.22f,.16f,.31f),Quaternion.Euler(0,0,side*55));
            }
            pieces.Add(Fin(),color,new Vector3(0,.12f,-.10f),new Vector3(.02f,.23f,.4f));
            pieces.Build(root);
            var tail=Group("Swimming tail",root,new Vector3(0,0,-.32f));
            ParkGeometry.Place("Tail fin",Fin(),tail,new Vector3(0,0,-.04f),new Vector3(.025f,.40f,-.32f),color);
            var motion=root.gameObject.AddComponent<ParkBeachWildlife>();
            motion.IsFish=true;motion.Center=new Vector3((index/4-1)*6,-.36f-(index%3)*.03f,-13.2f-(index%4)*.9f);
            motion.Radius=new Vector2(2.5f,.80f);motion.Speed=.30f+(index%4)*.025f;motion.Phase=index*.74f;
            motion.Tail=tail;motion.Pose(0);
        }

        static Mesh Fin()
        {
            var mesh=new Mesh{name="Tapered fish fin"};
            mesh.vertices=new[]{Vector3.zero,new Vector3(0,.5f,1),new Vector3(0,-.5f,1),
                Vector3.zero,new Vector3(0,.5f,1),new Vector3(0,-.5f,1)};
            mesh.triangles=new[]{0,1,2,3,5,4};mesh.RecalculateNormals();return mesh;
        }

        static Mesh Spot()
        {
            var mesh=new Mesh{name="Small starfish texture bump"};
            mesh.vertices=new[]{Vector3.up*.5f,Vector3.down*.5f,Vector3.left*.5f,
                Vector3.forward*.5f,Vector3.right*.5f,Vector3.back*.5f};
            mesh.triangles=new[]{0,2,3,0,3,4,0,4,5,0,5,2,1,3,2,1,4,3,1,5,4,1,2,5};
            mesh.RecalculateNormals();return mesh;
        }

        static Mesh Shell()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int rings=12,segments=40;
            for(int row=0;row<=rings;row++)
            for(int column=0;column<=segments;column++)
            {
                float r=row/(float)rings,a=Mathf.Lerp(-1.2f,1.2f,column/(float)segments);
                float rib=.018f*(.5f+.5f*Mathf.Cos(a*24))*Mathf.Sin(Mathf.PI*r);
                vertices.Add(new Vector3(Mathf.Sin(a)*r,.23f*Mathf.Sin(r*Mathf.PI)+rib,Mathf.Cos(a)*r));
                if(row==rings||column==segments)continue;
                int v=row*(segments+1)+column;triangles.AddRange(new[]{v,v+segments+1,v+1,v+1,v+segments+1,v+segments+2});
            }
            var mesh=new Mesh{name="Ribbed scallop shell"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }

        static Mesh Starfish()
        {
            var vertices=new List<Vector3>{new Vector3(0,.13f,0)};var triangles=new List<int>();
            const int count=40;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;
                float radius=.28f+.62f*Mathf.Pow(Mathf.Max(0,.5f+.5f*Mathf.Cos(a*5)),3);
                vertices.Add(new Vector3(Mathf.Sin(a)*radius,.025f,Mathf.Cos(a)*radius));
            }
            for(int i=0;i<count;i++)triangles.AddRange(new[]{0,i+1,(i+1)%count+1});
            var mesh=new Mesh{name="Five armed starfish"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
    }
}
