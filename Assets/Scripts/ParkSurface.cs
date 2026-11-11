using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public static class ParkSurface
    {
        public static readonly List<Texture2D> Textures = new List<Texture2D>();
        public static Texture2D Painted(string name, bool timber)
        {
            const int n=512;
            var tex=new Texture2D(n,n,TextureFormat.RGB24,true){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
            var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(float)x/n,v=(float)y/n;
                float broad=Mathf.PerlinNoise(u*6,v*6);
                float fine=Mathf.PerlinNoise(u*110,v*110);
                float grain=timber ? Mathf.Sin(u*90+Mathf.PerlinNoise(u*3,v*4)*6)*.035f : 0;
                float c=.88f+broad*.09f+fine*.035f+grain;
                colors[y*n+x]=new Color(c,c,c,1);
            }
            tex.SetPixels(colors);tex.Apply();Textures.Add(tex);return tex;
        }
        public static Mesh RoundedBox()
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3[] directions={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            const int steps=8;const float radius=.07f;
            foreach(var normal in directions)
            {
                Vector3 axis=normal.y!=0?Vector3.right:Vector3.up;
                Vector3 other=Vector3.Cross(normal,axis);int start=vertices.Count;
                for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
                {
                    var p=normal*.5f+axis*((float)x/steps-.5f)+other*((float)y/steps-.5f);
                    var inner=new Vector3(Mathf.Clamp(p.x,-.5f+radius,.5f-radius),Mathf.Clamp(p.y,-.5f+radius,.5f-radius),Mathf.Clamp(p.z,-.5f+radius,.5f-radius));
                    var norm=(p-inner).normalized;vertices.Add(inner+norm*radius);normals.Add(norm);uv.Add(new Vector2((float)x/steps,(float)y/steps));
                }
                for(int y=0;y<steps;y++)for(int x=0;x<steps;x++)
                {int a=start+y*(steps+1)+x,b=a+steps+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            }
            var mesh=new Mesh{name="Soft bevel box"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();ParkGeometry.Meshes.Add(mesh);return mesh;
        }
        public static Mesh Crown()
        {
            Vector3[] baseV={Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};
            int[] indices={0,4,3,0,2,4,0,5,2,0,3,5,1,3,4,1,4,2,1,2,5,1,5,3};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<indices.Length;i+=3) Subdivide(baseV[indices[i]],baseV[indices[i+1]],baseV[indices[i+2]],2,vertices,triangles);
            var mesh=new Mesh{name="Organic faceted crown"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ParkGeometry.Meshes.Add(mesh);return mesh;
        }
        static void Subdivide(Vector3 a,Vector3 b,Vector3 c,int depth,List<Vector3> v,List<int> t)
        {
            if(depth>0){var ab=(a+b).normalized;var bc=(b+c).normalized;var ca=(c+a).normalized;Subdivide(a,ab,ca,depth-1,v,t);Subdivide(ab,b,bc,depth-1,v,t);Subdivide(ca,bc,c,depth-1,v,t);Subdivide(ab,bc,ca,depth-1,v,t);return;}
            foreach(var p in new[]{a,b,c}){float bulge=1+.04f*Mathf.Sin(p.x*7+p.z*5)*Mathf.Cos(p.y*6);t.Add(v.Count);v.Add(p*bulge);}
        }
    }
}
