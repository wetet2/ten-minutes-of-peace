using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkBeachWildlifeSetup
    {
        readonly Mesh ball=PrimitiveMesh(PrimitiveType.Sphere);
        readonly Mesh pole=PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Mesh shadowQuad=PrimitiveMesh(PrimitiveType.Quad);
        readonly Material crabShadow=new Material(Resources.Load<Shader>("AnimalContactShadow")){name="Sand crab contact shadow"};
        readonly Material coral=Paint("Crab coral shell","#D96837");
        readonly Material claw=Paint("Crab ivory tips","#F2B27C");
        readonly Material eye=Paint("Marine dark eyes","#253B40");
        readonly Material ivory=Paint("Scallop ivory","#FFF0CF");
        readonly Material rose=Paint("Scallop rose","#EF9DAD");
        readonly Material peach=Paint("Scallop peach","#F6BD93");
        readonly Material orange=Paint("Starfish orange","#DA7D43");
        readonly Material gold=Paint("Reef fish gold","#F5C747");
        readonly Material blue=Paint("Reef fish azure","#5AA4B8");
        readonly Material silver=Paint("Reef fish silver","#D6E8D0");

        readonly System.Random random=new System.Random(42917);
        readonly List<Vector3> occupied=new List<Vector3>();
        readonly Vector3[][] crabPaths=new Vector3[3][];
        ParkBeachShorePlacement shoreline;
        internal static void Create(Transform parent,ParkBeachShorePlacement shoreline)=>new ParkBeachWildlifeSetup{shoreline=shoreline}.Build(parent);

        void Build(Transform parent)
        {
            for(int coast=0;coast<crabPaths.Length;coast++)
                crabPaths[coast]=shoreline.CrabPath(coast);
            var shore=Group("Seashells and starfish",parent,Vector3.zero);
            var pieces=new Pieces();
            var shell=Shell();
            var star=Starfish();
            var spot=Spot();
            for(int i=0;i<33;i++)
            {
                int coast=i<9?0:i<21?1:2;
                if(!DecorationPoint(coast,out var position))continue;
                float scale=Range(.34f,.69f);
                var color=i%3==0?rose:i%3==1?ivory:peach;
                position.y+=.025f;
                var rotation=Quaternion.Euler(0,Range(0,360),0);
                var placed=ParkGeometry.Place($"Cartoon scallop {i+1} coast {coast}",shell,shore,position,Vector3.one*scale,color);
                placed.localRotation=rotation;
                ParkGeometry.Place("Rounded shell hinge",ball,placed,new Vector3(0,.09f,.035f),new Vector3(.46f,.23f,.28f),color);
            }
            for(int i=0;i<13;i++)
            {
                int coast=i<3?0:i<8?1:2;
                if(!DecorationPoint(coast,out var point))continue;
                float scale=Range(.28f,.50f);point.y+=.035f;
                var rotation=Quaternion.Euler(0,Range(0,360),0);
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
            for(int i=0;i<crabPaths.Length;i++)Crab(parent,i);
            for(int i=0;i<12;i++)Fish(parent,i);
        }

        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        bool DecorationPoint(int coast,out Vector3 point)
        {
            for(int attempt=0;attempt<160;attempt++)
            {
                // Loose groups separated by empty sand, with independent offsets from the tide line.
                float cluster=new[]{.15f,.47f,.81f}[random.Next(3)];
                float along=Mathf.Clamp01(cluster+Range(-.115f,.115f));
                point=shoreline.Point(coast,along,Range(.95f,4.5f));
                if(!shoreline.Clear(point,.7f))continue;
                // Keep the shell, its shadow, and a passing crab's feet visually separate.
                bool clear=true;
                foreach(var waypoint in crabPaths[coast])
                    if((point-waypoint).sqrMagnitude<1.5f*1.5f){clear=false;break;}
                if(!clear)continue;
                foreach(var previous in occupied)if((point-previous).sqrMagnitude<1.05f*1.05f){clear=false;break;}
                if(!clear)continue;
                occupied.Add(point);return true;
            }
            point=Vector3.zero;return false;
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
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var shadow=ParkGeometry.Place("Soft shadow under shell",shadowQuad,root,new Vector3(0,.011f,.045f),
                new Vector3(.94f,.69f,1),crabShadow);
            shadow.localRotation=Quaternion.Euler(90,0,0);
            var shadowRenderer=shadow.GetComponent<MeshRenderer>();
            shadowRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows=false;
            motion.ShorePath=crabPaths[index];
            motion.ShoreNormals=new Vector3[motion.ShorePath.Length];
            for(int i=0;i<motion.ShorePath.Length;i++)
            {
                var point=motion.ShorePath[i];
                motion.ShorePath[i].y=ParkBeachMeshes.SandSurface(point.x,point.z,out motion.ShoreNormals[i]);
            }
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
            const int rings=18,segments=64;
            int layer=(rings+1)*(segments+1);
            // A closed, thick shell with seven broad ribs and a softly scalloped outline.
            for(int underside=0;underside<2;underside++)
            for(int row=0;row<=rings;row++)
            for(int column=0;column<=segments;column++)
            {
                float r=row/(float)rings,u=column/(float)segments,a=Mathf.Lerp(-1.28f,1.28f,u);
                float rib=.5f+.5f*Mathf.Cos(u*Mathf.PI*14);
                float radius=r*(1+.055f*rib*r*r);
                float dome=.30f*Mathf.Sin(r*Mathf.PI)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.25f);
                float y=underside==0?.065f+dome+.15f*rib*Mathf.Sin(r*Mathf.PI*.92f):.012f;
                vertices.Add(new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius));
                if(row==rings||column==segments)continue;
                int v=underside*layer+row*(segments+1)+column;
                if(underside==0)triangles.AddRange(new[]{v,v+segments+1,v+1,v+1,v+segments+1,v+segments+2});
                else triangles.AddRange(new[]{v,v+1,v+segments+1,v+1,v+segments+2,v+segments+1});
            }
            for(int column=0;column<segments;column++)Edge(rings*(segments+1)+column,rings*(segments+1)+column+1);
            for(int row=0;row<rings;row++)
            {
                Edge(row*(segments+1),(row+1)*(segments+1));
                Edge((row+1)*(segments+1)+segments,row*(segments+1)+segments);
            }
            var mesh=new Mesh{name="Thick seven rib cartoon scallop"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
            void Edge(int a,int b)=>triangles.AddRange(new[]{a,a+layer,b,b,a+layer,b+layer});
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
