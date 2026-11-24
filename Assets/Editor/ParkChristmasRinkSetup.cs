using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkChristmasRinkSetup
    {
        internal const float RearEdge=-7.8f;
        const float CenterZ=RearEdge-ParkIceRink.HalfDepth;
        internal const float FrontEdge=CenterZ-ParkIceRink.HalfDepth;
        internal static Rect Footprint => Rect.MinMaxRect(-ParkIceRink.HalfWidth-.34f,FrontEdge-.31f,
            ParkIceRink.HalfWidth+.34f,RearEdge+.29f);

        internal static ParkIceRink Create(Transform parent)
        {
            var root=Group("Christmas ice rink",parent,new Vector3(0,0,CenterZ));
            var box=ParkSurface.RoundedBox();
            var sphere=PrimitiveMesh(PrimitiveType.Sphere);
            var cylinder=PrimitiveMesh(PrimitiveType.Cylinder);
            var wood=Paint("Honey-stained rink rail", "#B97B50");
            var post=Paint("Warm oak posts", "#97644A");
            var snow=Paint("Ivory snow on the rink", "#F8EEF8");
            var white=Paint("Pale skate leather", "#EEF5FF");
            var steel=Paint("Silver skate blades", "#A3BAC8");
            var bootTrim=Paint("Indigo skate trim", "#4B729A");
            foreach(var material in new[]{white,steel,bootTrim})material.EnableKeyword("PEACE_PARK_ANIMAL_FADING");
            var light=new Material(Shader.Find("Unlit/Color"))
                {name="Golden rink lights",color=new Color(1f,.70f,.30f,1)};
            var ice=new Material(Shader.Find("PeacePark/Christmas Ice")){name="Blue ice with lantern reflections"};
            const float w=ParkIceRink.HalfWidth,d=ParkIceRink.HalfDepth;
            const float corner=w*.15f;
            ParkGeometry.Place("Snow border",Surface(w+.28f,d+.28f,corner+.28f),root,new Vector3(0,.030f,0),Vector3.one,snow);
            var frozen=ParkGeometry.Place("Smooth blue ice",Surface(w,d,corner),root,new Vector3(0,ParkIceRink.IceY,0),Vector3.one,ice);
            frozen.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;

            var rails=new Pieces();
            const float opening=ParkIceRink.GateHalfWidth;
            var outline=Outline(w,d,corner);
            for(int i=0;i<outline.Count;i++)
            {
                var start=outline[i];
                var end=outline[(i+1)%outline.Count];
                if(start.y>d-.001f&&end.y>d-.001f&&start.x>0&&end.x<0)
                {
                    AddRun(start,new Vector2(opening,d));
                    AddRun(new Vector2(-opening,d),end);
                }
                else AddRun(start,end);
            }
            AddPost(new Vector2(opening,d),true);
            AddPost(new Vector2(-opening,d),true);
            rails.Build(Group("Snow-capped rink rails",root,Vector3.zero));

            var scenery=new Pieces();
            for(int side=-1;side<=1;side+=2)
            {
                var p=new Vector3(side*(w+.73f),.025f,-1.05f);
                scenery.Add(box,wood,p+new Vector3(0,.42f,0),new Vector3(.44f,.13f,2.11f));
                scenery.Add(box,snow,p+new Vector3(0,.49f,.01f),new Vector3(.40f,.06f,2.03f));
                scenery.Add(box,wood,p+new Vector3(.21f*side,.68f,0),new Vector3(.11f,.30f,2.10f));
                foreach(int end in new[]{-1,1})
                    scenery.Add(box,post,p+new Vector3(.05f*side,.20f,end*.77f),new Vector3(.41f,.40f,.13f));
            }
            scenery.Build(Group("Snowy rink benches",root,Vector3.zero));

            var skates=Group("Rink skate template",root,Vector3.zero);
            var skate=new Pieces();
            skate.Add(box,steel,new Vector3(0,-.128f,0),new Vector3(.078f,.064f,.99f));
            skate.Add(box,steel,new Vector3(0,-.075f,.26f),new Vector3(.094f,.12f,.10f));
            skate.Add(box,steel,new Vector3(0,-.075f,-.26f),new Vector3(.094f,.12f,.10f));
            skate.Add(box,bootTrim,new Vector3(0,.006f,0),new Vector3(.57f,.083f,.88f));
            skate.Add(box,white,new Vector3(0,.105f,-.045f),new Vector3(.53f,.23f,.81f));
            skate.Add(box,bootTrim,new Vector3(0,.195f,.19f),new Vector3(.48f,.14f,.28f));
            skate.Build(skates);
            skates.gameObject.SetActive(false);

            var ride=root.gameObject.AddComponent<ParkIceRink>();
            ride.Configure(Gate(-1),Gate(1),skates);
            return ride;

            void AddRun(Vector2 from,Vector2 to)
            {
                var a=At(from,0);
                var b=At(to,0);
                int segments=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/1.68f));
                for(int i=0;i<segments;i++)
                {
                    var foot=Vector2.Lerp(from,to,i/(float)segments);
                    bool atGate=Mathf.Abs(Mathf.Abs(foot.x)-opening)<.01f&&Mathf.Abs(foot.y-d)<.01f;
                    if(!atGate)AddPost(foot,i%2==0||segments==1);
                }
                rails.Beam(cylinder,wood,a+Vector3.up*.39f,b+Vector3.up*.39f,.16f);
                rails.Beam(cylinder,wood,a+Vector3.up*.78f,b+Vector3.up*.78f,.20f);
                rails.Beam(cylinder,snow,a+Vector3.up*.876f,b+Vector3.up*.876f,.084f);
            }

            void AddPost(Vector2 point,bool lit)
            {
                var at=At(point,0);
                rails.Add(box,post,at+Vector3.up*.44f,new Vector3(.23f,.88f,.23f));
                rails.Add(sphere,snow,at+Vector3.up*.91f,new Vector3(.27f,.17f,.27f));
                if(!lit)return;
                var inward=new Vector3(-point.x/w,0,-point.y/d).normalized;
                rails.Add(sphere,light,at+Vector3.up*.63f+inward*.15f,Vector3.one*.17f);
            }

            Transform Gate(int side)
            {
                var frame=Group(side<0?"Left rink gate":"Right rink gate",root,new Vector3(side*opening,.025f,d));
                var gate=new Pieces();
                const float leaf=opening-.09f;
                for(int i=0;i<6;i++)
                {
                    float t=i/6f;
                    float top=.82f+.28f*Mathf.Sin(t*Mathf.PI*.5f);
                    gate.Add(box,wood,new Vector3(-side*(.10f+t*(leaf-.06f)),top*.5f,0),new Vector3(.13f,top,.12f));
                }
                foreach(float height in new[]{.24f,.63f})
                    gate.Add(box,post,new Vector3(-side*leaf*.5f,height,0),new Vector3(leaf,.12f,.17f));
                gate.Build(frame);
                return frame;
            }
        }

        static Mesh Surface(float width,float depth,float corner)
        {
            var outline=Outline(width,depth,corner);
            var vertices=new Vector3[outline.Count+1];
            var uv=new Vector2[vertices.Length];
            var triangles=new int[outline.Count*3];
            uv[0]=new Vector2(.5f,.5f);
            for(int i=0;i<outline.Count;i++)
            {
                var p=outline[i];
                vertices[i+1]=At(p,0);
                uv[i+1]=new Vector2(.5f+p.x/(2*width),.5f+p.y/(2*depth));
                triangles[i*3+1]=(i+1)%outline.Count+1;
                triangles[i*3+2]=i+1;
            }
            var mesh=new Mesh{name="Rounded ice surface",vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals();
            return mesh;
        }

        static List<Vector2> Outline(float width,float depth,float radius)
        {
            var points=new List<Vector2>();
            var centers=new[]{new Vector2(width-radius,-depth+radius),new Vector2(width-radius,depth-radius),
                new Vector2(-width+radius,depth-radius),new Vector2(-width+radius,-depth+radius)};
            for(int corner=0;corner<centers.Length;corner++)
            for(int i=0;i<=7;i++)
            {
                float angle=(-90+90*(corner+i/7f))*Mathf.Deg2Rad;
                points.Add(centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
            }
            return points;
        }

        static Vector3 At(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
    }
}
