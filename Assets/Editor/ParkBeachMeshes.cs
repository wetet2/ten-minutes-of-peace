using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark.Editor
{
    internal static class ParkBeachMeshes
    {
        const int SandColumns=224,SandRows=256;
        const float MinX=-150,MaxX=150,MinZ=-190,MaxZ=270;
        internal static float Shore(float x) => -7-.014f*x*x+.25f*Mathf.Sin(x*.4f);
        internal static float LandDistance(float x,float z)
        {
            float edge=(1-new Vector2((x+8)/62,(z-100)/120).magnitude)*55;
            float slope=-.028f*x+.1f*Mathf.Cos(x*.4f);
            float bay=(z-Shore(x))/Mathf.Sqrt(1+slope*slope);
            float blend=Mathf.Clamp01(.5f+.5f*(edge-bay)/4);
            return Mathf.Lerp(edge,bay,blend)-4*blend*(1-blend);
        }
        internal static float Height(float x,float z)
        {
            float d=LandDistance(x,z);
            return d<0 ? d*.17f : Mathf.SmoothStep(0,.18f,Mathf.Clamp01(d/2.2f));
        }

        // Use the same diagonal as Ground: a crab's feet sit on the rendered triangle,
        // which can sit below the smooth shoreline profile between grid vertices.
        internal static float SandSurface(float x,float z,out Vector3 normal)
        {
            float dx=(MaxX-MinX)/SandColumns,dz=(MaxZ-MinZ)/SandRows;
            int col=Mathf.Clamp(Mathf.FloorToInt((x-MinX)/dx),0,SandColumns-1);
            int row=Mathf.Clamp(Mathf.FloorToInt((z-MinZ)/dz),0,SandRows-1);
            float x0=MinX+col*dx,z0=MinZ+row*dz;
            float u=(x-x0)/dx,v=(z-z0)/dz;
            float a=Height(x0,z0),c=Height(x0+dx,z0+dz);
            float side=u<v?Height(x0,z0+dz):Height(x0+dx,z0);
            float riseX=u<v?c-side:side-a;
            float riseZ=u<v?side-a:c-side;
            normal=new Vector3(-riseX/dx,1,-riseZ/dz).normalized;
            return a+u*riseX+v*riseZ;
        }

        internal static Mesh Ground(bool water)
        {
            var mesh=new Shape();
            int columns=water?192:SandColumns,rows=water?192:SandRows;
            for(int z=0;z<=rows;z++)
            for(int x=0;x<=columns;x++)
            {
                float px=Mathf.Lerp(MinX,MaxX,x/(float)columns);
                float pz=Mathf.Lerp(MinZ,MaxZ,z/(float)rows);
                if(water)
                {
                    // Dense vertices around the bay; a continuous outer ocean beyond the fog/far clip.
                    float u=x/(float)columns*2-1,v=z/(float)rows*2-1;
                    px=200*u+500*Mathf.Pow(u,5);pz=40+200*v+500*Mathf.Pow(v,5);
                }
                mesh.Add(new Vector3(px,water?.04f:Height(px,pz),pz),new Vector2(x/(float)columns,z/(float)rows));
                if(x==columns||z==rows)continue;
                int a=z*(columns+1)+x;
                mesh.Quad(a,a+columns+1,a+columns+2,a+1);
            }
            return mesh.Build(water?"Sheltered tropical sea":"Sand and submerged seabed");
        }

        internal static Mesh Leaf(float length,float width,float bend,int segments=12)
        {
            var mesh=new Shape();
            for(int row=0;row<=segments;row++)
            {
                float t=row/(float)segments;
                float span=width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.72f);
                for(int side=-1;side<=1;side++)
                {
                    float y=length*(Mathf.Sin(t*Mathf.PI)*.20f-bend*t*t)-Mathf.Abs(side)*span*.17f;
                    mesh.Add(new Vector3(side*span*.5f,y,t*length),new Vector2((side+1)*.5f,t));
                }
                if(row==segments)continue;
                for(int side=0;side<2;side++)
                {
                    int a=row*3+side;
                    mesh.Quad(a,a+3,a+4,a+1);
                }
            }
            return mesh.Build("Curved tropical leaf with folded midrib");
        }

        internal static Mesh PalmFrond()
        {
            var mesh=new Shape();
            const float length=4.8f;
            for(int step=0;step<33;step++)
            {
                float t=.08f+step*.026f;
                var center=FrondPoint(t);
                float span=.20f+1.03f*Mathf.Pow(Mathf.Sin(t*Mathf.PI),.65f);
                for(int side=-1;side<=1;side+=2)
                {
                    int start=mesh.Count;
                    for(int part=0;part<5;part++)
                    {
                        float u=part/4f;
                        float breadth=.095f*Mathf.Sin(Mathf.PI*u);
                        var point=center+new Vector3(side*span*u,-.35f*u*u, .62f*u);
                        mesh.Add(point+new Vector3(0,0,-breadth),new Vector2(0,u));
                        mesh.Add(point+new Vector3(0,.035f*Mathf.Sin(u*Mathf.PI),0),new Vector2(.5f,u));
                        mesh.Add(point+new Vector3(0,0,breadth),new Vector2(1,u));
                        if(part==4)continue;
                        for(int strip=0;strip<2;strip++)
                        {
                            int a=start+part*3+strip;
                            mesh.Quad(a,a+3,a+4,a+1);
                        }
                    }
                }
            }
            for(int step=0;step<=32;step++)
            {
                float t=step/32f;
                int a=mesh.Count;
                mesh.Add(FrondPoint(t)+Vector3.left*.025f,new Vector2(0,t));
                mesh.Add(FrondPoint(t)+Vector3.right*.025f,new Vector2(1,t));
                if(step<32)mesh.Quad(a,a+2,a+3,a+1);
            }
            return mesh.Build("Feathered coconut frond with 66 leaflets");
            Vector3 FrondPoint(float t)=>new Vector3(0,Mathf.Sin(t*Mathf.PI)*1.25f-t*t*.65f,t*length);
        }

        internal static Vector3 TrunkPoint(float height,float lean,float t)=>new Vector3(lean*t*t,height*t,.24f*Mathf.Sin(t*Mathf.PI));

        internal static Mesh Trunk(float height,float lean)
        {
            var mesh=new Shape();
            const int rows=72,sides=16;
            for(int row=0;row<=rows;row++)
            {
                float t=row/(float)rows;
                float radius=Mathf.Lerp(.29f,.16f,t)*(1+.08f*Mathf.Cos(t*height*28));
                for(int side=0;side<=sides;side++)
                {
                    float a=side*Mathf.PI*2/sides;
                    mesh.Add(TrunkPoint(height,lean,t)+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),new Vector2(side/(float)sides,t));
                    if(row==rows||side==sides)continue;
                    int v=row*(sides+1)+side;
                    mesh.Quad(v,v+1,v+sides+2,v+sides+1);
                }
            }
            return mesh.Build("Curving ringed palm trunk");
        }

        internal static Mesh Rock(int seed)
        {
            var mesh=new Shape();
            const int rings=14,sides=24;
            for(int ring=0;ring<=rings;ring++)
            for(int side=0;side<=sides;side++)
            {
                float lat=ring*Mathf.PI/rings,lon=side*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon),Mathf.Cos(lat),Mathf.Sin(lat)*Mathf.Sin(lon));
                float rough=1+.13f*Mathf.Sin(p.x*5+seed)*Mathf.Cos(p.z*4+seed*.31f)+.06f*Mathf.Sin(p.y*13+seed);
                mesh.Add(p*rough,new Vector2(side/(float)sides,ring/(float)rings));
                if(ring==rings||side==sides)continue;
                int a=ring*(sides+1)+side;
                mesh.Quad(a,a+1,a+sides+2,a+sides+1);
            }
            return mesh.Build("Weathered coastal granite");
        }

        sealed class Shape
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> triangles=new List<int>();
            internal int Count=>vertices.Count;
            internal void Add(Vector3 point,Vector2 texture){vertices.Add(point);uv.Add(texture);}
            internal void Quad(int a,int b,int c,int d){triangles.AddRange(new[]{a,b,c,a,c,d});}
            internal Mesh Build(string name)
            {
                var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
                var colors=new Color[vertices.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;
                mesh.colors=colors;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
