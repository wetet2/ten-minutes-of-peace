using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public static class ParkGeometry
    {
        public static readonly List<Mesh> Meshes = new List<Mesh>();
        static Mesh Make(string name, List<Vector3> vertices, List<int> triangles, bool flat)
        {
            if(flat)
            {
                var v=new List<Vector3>(); var t=new List<int>();
                foreach(int index in triangles) { t.Add(v.Count); v.Add(vertices[index]); }
                vertices=v;triangles=t;
            }
            var mesh=new Mesh { name=name };mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            var uv=new List<Vector2>();for(int i=0;i<vertices.Count;i++) {var p=vertices[i];var n=mesh.normals[i];uv.Add(Mathf.Abs(n.y)>.6f ? new Vector2(p.x*.25f,p.z*.25f):Mathf.Abs(n.x)>.6f?new Vector2(p.z*.3f,p.y*.3f):new Vector2(p.x*.3f,p.y*.3f));}
            mesh.SetUVs(0,uv);mesh.RecalculateBounds();Meshes.Add(mesh);return mesh;
        }
        public static Transform Place(string name, Mesh mesh, Transform parent, Vector3 p, Vector3 scale, Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
        // Staggered rings produce irregular triangular planes instead of a smooth primitive sphere.
        public static Mesh Canopy()
        {
            var v=new List<Vector3>();var t=new List<int>();const int sides=11,rings=7;
            for(int j=0;j<=rings;j++)
            {
                float latitude=Mathf.PI*j/rings;
                for(int i=0;i<sides;i++)
                {
                    float a=(i+(j%2)*.5f)*Mathf.PI*2/sides;
                    float r=Mathf.Sin(latitude)*(1+.075f*Mathf.Sin(i*13.7f+j*5.4f));
                    v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Cos(latitude),Mathf.Sin(a)*r));
                }
            }
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
            {
                int a=j*sides+i,b=j*sides+(i+1)%sides,c=a+sides,d=b+sides;
                t.AddRange(new[]{a,b,c,b,d,c});
            }
            return Make("Faceted foliage",v,t,true);
        }
        public static Mesh Trunk()
        {
            var v=new List<Vector3>();var t=new List<int>();const int n=9;
            for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++)
            {
                float a=i*2*Mathf.PI/n;
                float radius=ring==0 ? (i%2==0?.54f:.36f) : ring==1?.29f:ring==2?.23f:.16f;
                v.Add(new Vector3(Mathf.Cos(a)*radius+ring*.055f,ring*.85f,Mathf.Sin(a)*radius));
            }
            for(int j=0;j<3;j++)for(int i=0;i<n;i++) { int a=j*n+i,b=j*n+(i+1)%n;t.AddRange(new[]{a,a+n,b,b,a+n,b+n}); }
            return Make("Tapered trunk and roots",v,t,true);
        }
        // CCW XY outline, extruded in Z. Used for real arched doors and closed roofs.
        public static Mesh Extrude(string name,List<Vector2> outline,float depth)
        {
            var v=new List<Vector3>();var t=new List<int>();int n=outline.Count;
            foreach(var p in outline)v.Add(new Vector3(p.x,p.y,-depth/2));
            foreach(var p in outline)v.Add(new Vector3(p.x,p.y,depth/2));
            for(int i=1;i<n-1;i++)t.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});
            for(int i=0;i<n;i++){int b=(i+1)%n;t.AddRange(new[]{i,b,i+n,b,b+n,i+n});}
            return Make(name,v,t,true);
        }
        public static Mesh Arch(float width,float height,float depth)
        {
            float r=width/2;var points=new List<Vector2>{new Vector2(-r,0),new Vector2(r,0)};
            for(int i=0;i<=16;i++){float a=i*Mathf.PI/16;points.Add(new Vector2(Mathf.Cos(a)*r,height-r+Mathf.Sin(a)*r));}
            return Extrude("Arched panel",points,depth);
        }
        public static Mesh Roof()
        {
            return Extrude("Soft pitched roof",new List<Vector2>{new Vector2(-2.95f,0),new Vector2(2.95f,0),new Vector2(2.75f,.36f),new Vector2(1.55f,1.15f),new Vector2(.28f,1.65f),new Vector2(-.28f,1.65f),new Vector2(-1.55f,1.15f),new Vector2(-2.75f,.36f)},4.9f);
        }
        public static Mesh Path()
        {
            var v=new List<Vector3>();var t=new List<int>();const int n=100;
            // U-shaped continuous ribbon joining the two cottage entrances.
            for(int i=0;i<=n;i++)
            {
                float a=Mathf.PI*i/n;float x=-7*Mathf.Cos(a);float z=2-10*Mathf.Sin(a);
                var tangent=new Vector3(7*Mathf.Sin(a),0,-10*Mathf.Cos(a)).normalized;
                var normal=new Vector3(-tangent.z,0,tangent.x)*1.1f;
                v.Add(new Vector3(x,.025f,z)-normal);v.Add(new Vector3(x,.025f,z)+normal);
                if(i<n){int b=i*2;t.AddRange(new[]{b,b+1,b+2,b+1,b+3,b+2});}
            }
            return Make("Continuous garden path",v,t,false);
        }
    }
}
