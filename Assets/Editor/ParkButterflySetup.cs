using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkButterflySetup
    {
        internal static void Apply(Scene scene)
        {
            var park=Array.Find(scene.GetRootGameObjects(),root=>root.name=="Peace Park");
            if(!park||park.transform.Find("Garden butterflies"))return;
            var flock=Group("Garden butterflies",park.transform,Vector3.zero);
            var sphere=PrimitiveMesh(PrimitiveType.Sphere);
            var stem=PrimitiveMesh(PrimitiveType.Cylinder);
            var outline=Paint("Butterfly wing edge","#654647");
            var cream=Paint("Butterfly wing spots","#FFF3C7");
            var body=Paint("Butterfly body","#584047");
            var orange=Paint("Marigold butterfly","#FFAE44");
            var rose=Paint("Rose butterfly","#F479AA");
            var lilac=Paint("Lilac butterfly","#BD91FF");
            var wingShape=WingMesh();
            Add(flock,wingShape,sphere,stem,outline,cream,body,orange,new Vector3(-12.6f,1.65f,.1f),new Vector2(1.7f,1.25f),.67f,2.8f,1);
            Add(flock,wingShape,sphere,stem,outline,cream,body,rose,new Vector3(12.6f,1.6f,.1f),new Vector2(1.65f,1.35f),.79f,.9f,.94f);
            Add(flock,wingShape,sphere,stem,outline,cream,body,lilac,new Vector3(0,2.05f,0),new Vector2(3.95f,3.2f),.51f,4.1f,1.05f);
            Add(flock,wingShape,sphere,stem,outline,cream,body,orange,new Vector3(-4.9f,1.25f,-7.1f),new Vector2(1.6f,1.25f),.74f,2.1f,.89f);
            Add(flock,wingShape,sphere,stem,outline,cream,body,rose,new Vector3(5.4f,1.45f,-6.1f),new Vector2(1.85f,1.35f),.58f,5.3f,.96f);
        }

        static void Add(Transform flock,Mesh wingShape,Mesh sphere,Mesh stem,Material outline,Material cream,
            Material body,Material color,Vector3 center,Vector2 loop,float speed,float phase,float scale)
        {
            var butterfly=Group(color.name,flock,center);
            butterfly.localScale=Vector3.one*scale;
            var details=new Pieces();
            details.Add(sphere,body,Vector3.zero,new Vector3(.085f,.095f,.41f));
            details.Add(sphere,body,new Vector3(0,.01f,.175f),Vector3.one*.095f);
            foreach(int side in new[]{-1,1})
            {
                var tip=new Vector3(side*.084f,.065f,.274f);
                details.Beam(stem,body,new Vector3(side*.026f,.028f,.19f),tip,.012f);
                details.Add(sphere,body,tip,Vector3.one*.026f);
            }
            details.Build(butterfly);

            var left=Group("Left wing hinge",butterfly,Vector3.zero);
            var right=Group("Right wing hinge",butterfly,Vector3.zero);
            var leftShape=Group("Left wing shape",left,Vector3.zero);
            leftShape.localScale=new Vector3(-1,1,1);
            AddWing(leftShape,wingShape,sphere,outline,cream,color);
            AddWing(right,wingShape,sphere,outline,cream,color);
            foreach(var renderer in butterfly.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode=ShadowCastingMode.Off;
            butterfly.gameObject.AddComponent<ParkButterfly>().Configure(left,right,center,loop,speed,phase);
        }

        static void AddWing(Transform hinge,Mesh wingShape,Mesh sphere,Material outline,Material cream,Material color)
        {
            var wing=ParkGeometry.Place("Butterfly wing",wingShape,hinge,Vector3.zero,Vector3.one,outline);
            wing.GetComponent<MeshRenderer>().sharedMaterials=new[]{outline,color};
            var spots=new Pieces();
            spots.Add(sphere,cream,new Vector3(.38f,0,.215f),new Vector3(.137f,.024f,.099f),Quaternion.Euler(0,-28,0));
            spots.Add(sphere,cream,new Vector3(.465f,0,.12f),new Vector3(.059f,.023f,.052f));
            spots.Add(sphere,cream,new Vector3(.235f,0,-.156f),new Vector3(.083f,.023f,.075f));
            spots.Build(hinge);
        }

        static Mesh WingMesh()
        {
            Vector2[] edge={
                new(.02f,.09f),new(.13f,.23f),new(.26f,.33f),new(.41f,.36f),
                new(.51f,.31f),new(.55f,.23f),new(.53f,.13f),new(.46f,.035f),
                new(.37f,-.03f),new(.39f,-.13f),new(.35f,-.23f),new(.26f,-.29f),
                new(.155f,-.275f),new(.075f,-.215f),new(.028f,-.13f)
            };
            int count=edge.Length;
            var vertices=new Vector3[count*4+2];
            var normals=new Vector3[vertices.Length];
            var border=new System.Collections.Generic.List<int>();
            var interior=new System.Collections.Generic.List<int>();
            var center=new Vector2(.23f,.04f);
            for(int side=0;side<2;side++)
            {
                int start=side*(count*2+1),middle=start+count*2;
                float y=side==0?.004f:-.004f;
                vertices[middle]=new Vector3(center.x,y,center.y);
                for(int i=0;i<count;i++)
                {
                    var inner=Vector2.Lerp(edge[i],center,.115f);
                    int a=start+i,b=start+(i+1)%count;
                    vertices[a]=new Vector3(edge[i].x,y,edge[i].y);
                    vertices[a+count]=new Vector3(inner.x,y,inner.y);
                    if(side==0)
                    {
                        border.AddRange(new[]{a,b,b+count,a,b+count,a+count});
                        interior.AddRange(new[]{middle,a+count,b+count});
                    }
                    else
                    {
                        border.AddRange(new[]{a,b+count,b,a,a+count,b+count});
                        interior.AddRange(new[]{middle,b+count,a+count});
                    }
                }
                for(int i=start;i<=middle;i++)normals[i]=side==0?Vector3.up:Vector3.down;
            }
            var mesh=new Mesh{name="Two-sided butterfly wing",vertices=vertices,normals=normals,subMeshCount=2};
            mesh.SetTriangles(border,0);mesh.SetTriangles(interior,1);
            return mesh;
        }
    }
}
