using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;
using Object=UnityEngine.Object;

namespace PeacePark.Editor
{
    internal static class ParkBakerySetup
    {
        const string PrefabPath="Assets/LowpolyBakersHouse/Prefabs/Baker_house.prefab";
        internal static readonly Vector3 Location=new Vector3(-2.4f,0,7.3f);
        internal static GameObject Prefab=>AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        internal static ParkHouse Create(Transform parent,float ground)
        {
            var root=Group("Baker's house",parent,Location+Vector3.up*ground);
            root.localScale=Vector3.one*.94f;
            var model=Object.Instantiate(Prefab,root,false);
            var filter=model.GetComponentInChildren<MeshFilter>();
            var source=filter.sharedMesh;
            var transformToHouse=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var shell=new Geometry();
            var door=new Geometry();
            var hingePosition=new Vector3(1.166f,.944f,-3.86f);

            // Coordinates measured in the imported 2.0 house prefab. The panel sits in
            // front of a separate, unbroken wall, so both surfaces need editing.
            var opening=new[]{new Vector2(1.172f,.798f),new Vector2(2.710f,.798f),
                new Vector2(2.710f,2.602f),new Vector2(2.631f,2.948f),new Vector2(2.339f,3.258f),
                new Vector2(1.941f,3.366f),new Vector2(1.543f,3.258f),new Vector2(1.251f,2.948f),
                new Vector2(1.172f,2.602f)};
            var positions=source.vertices;
            var normals=source.normals;
            var uvs=source.uv;
            var indices=source.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                var triangle=new Vertex[3];
                for(int j=0;j<3;j++)
                {
                    int k=indices[i+j];
                    triangle[j]=new Vertex(transformToHouse.MultiplyPoint3x4(positions[k]),
                        transformToHouse.MultiplyVector(normals[k]).normalized,uvs[k]);
                }
                if(Array.TrueForAll(triangle,v=>Mathf.Abs(v.Point.z+3.84f)<.002f&&
                    v.Point.x>=1.165f&&v.Point.x<=2.717f&&v.Point.y>=.943f&&v.Point.y<=3.372f))
                {
                    foreach(var v in triangle)door.Add(new Vertex(v.Point-hingePosition,v.Normal,v.Uv));
                    continue;
                }
                bool front=Array.TrueForAll(triangle,v=>Mathf.Abs(v.Point.z+3.747f)<.002f||Mathf.Abs(v.Point.z+3.861f)<.002f);
                if(!front){shell.AddPolygon(triangle);continue;}
                var remainder=new List<Vertex>(triangle);
                for(int edge=0;edge<opening.Length&&remainder.Count>=3;edge++)
                {
                    var start=opening[edge];
                    var end=opening[(edge+1)%opening.Length];
                    shell.AddPolygon(Clip(remainder,start,end,false));
                    remainder=Clip(remainder,start,end,true);
                }
            }
            if(door.Count!=21)throw new InvalidOperationException("Baker's House door mesh differs from version 2.0.");
            var renderer=filter.GetComponent<MeshRenderer>();
            var facade=renderer.sharedMaterial;
            ParkGeometry.Place("Baker house facade",shell.Mesh("Bakery facade with doorway"),root,Vector3.zero,Vector3.one,facade);
            Object.DestroyImmediate(model);
            var hinge=Group("Bakery door hinge",root,hingePosition);
            ParkGeometry.Place("Original wooden door",door.Mesh("Bakery wood door"),hinge,Vector3.zero,Vector3.one,facade);
            ParkGeometry.Place("Door back",door.Mesh("Bakery door back",true),hinge,Vector3.forward*.035f,Vector3.one,
                Paint("Bakery inner wood","#A96E38"));
            var dark=Paint("Bakery interior","#483128");
            var rounded=ParkSurface.RoundedBox();
            ParkGeometry.Place("Interior back wall",rounded,root,new Vector3(1.94f,1.98f,-2.25f),new Vector3(3,3.6f,.12f),dark);
            ParkGeometry.Place("Interior floor",rounded,root,new Vector3(1.94f,.68f,-3.08f),new Vector3(2.7f,.24f,1.64f),dark);
            var knob=PrimitiveMesh(PrimitiveType.Sphere);
            ParkGeometry.Place("Brass door knob",knob,hinge,new Vector3(1.29f,.79f,-.034f),new Vector3(.135f,.135f,.115f),
                Paint("Bakery brass","#EDBE62"));

            var house=root.gameObject.AddComponent<ParkHouse>();
            house.DisplayName="빵집";house.DoorHinge=hinge;house.OpenAngle=109;
            house.Inside=Group("Bakery inside point",root,new Vector3(1.94f,.80f,-2.80f));
            house.Threshold=Group("Bakery threshold point",root,new Vector3(1.94f,.83f,-4.04f));
            house.Welcome=Group("Bakery welcome point",root,new Vector3(1.94f,0,-6.14f));
            house.ExitSlope=new[]{new Vector2(-2.80f,.80f),new Vector2(-4.26f,.83f),new Vector2(-4.47f,.55f),
                new Vector2(-4.70f,.28f),new Vector2(-4.99f,0),new Vector2(-6.14f,0)};
            return house;
        }

        static List<Vertex> Clip(List<Vertex> polygon,Vector2 start,Vector2 end,bool inside)
        {
            var result=new List<Vertex>();
            var from=polygon[polygon.Count-1];
            float fromSide=Side(from.Point,start,end);
            foreach(var to in polygon)
            {
                float toSide=Side(to.Point,start,end);
                if((fromSide>=0)!=(toSide>=0))result.Add(Vertex.Lerp(from,to,fromSide/(fromSide-toSide)));
                if((toSide>=0)==inside)result.Add(to);
                from=to;fromSide=toSide;
            }
            return result;
        }

        static float Side(Vector3 p,Vector2 a,Vector2 b)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);

        readonly struct Vertex
        {
            internal readonly Vector3 Point,Normal;
            internal readonly Vector2 Uv;
            internal Vertex(Vector3 point,Vector3 normal,Vector2 uv){Point=point;Normal=normal;Uv=uv;}
            internal static Vertex Lerp(Vertex a,Vertex b,float t)=>
                new Vertex(Vector3.Lerp(a.Point,b.Point,t),Vector3.Lerp(a.Normal,b.Normal,t).normalized,Vector2.Lerp(a.Uv,b.Uv,t));
        }

        sealed class Geometry
        {
            readonly List<Vector3> points=new(),normals=new();
            readonly List<Vector2> uvs=new();
            internal int Count=>points.Count;
            internal void Add(Vertex v){points.Add(v.Point);normals.Add(v.Normal);uvs.Add(v.Uv);}
            internal void AddPolygon(IList<Vertex> polygon)
            {
                for(int i=1;i<polygon.Count-1;i++){Add(polygon[0]);Add(polygon[i]);Add(polygon[i+1]);}
            }
            internal Mesh Mesh(string name,bool reverse=false)
            {
                var mesh=new Mesh{name=name};
                mesh.SetVertices(points);mesh.SetUVs(0,uvs);
                mesh.SetNormals(reverse?normals.ConvertAll(normal=>-normal):normals);
                var indices=new int[Count];
                for(int i=0;i<Count;i++)indices[i]=reverse?Count-1-i:i;
                mesh.SetTriangles(indices,0);
                return mesh;
            }
        }
    }
}
