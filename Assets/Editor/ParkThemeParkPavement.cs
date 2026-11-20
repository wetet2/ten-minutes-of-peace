using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkThemeParkSetup
    {
        void MakeForecourt(Transform root)
        {
            const float halfWidth=62, halfDepth=37.5f, centerZ=-5, cornerRadius=29;
            const int width=1024, height=640, steps=24;
            var pixels=new Color32[width*height];
            Color sky=new Color32(116,172,220,255);
            Color lavender=new Color32(169,160,225,255);
            Color lilac=new Color32(185,166,225,255);
            Color stripe=new Color32(255,218,131,255);
            for(int row=0;row<height;row++)
            {
                float z=centerZ+((row+.5f)/height*2-1)*halfDepth;
                float leftBend=12*Mathf.Sin(z*.068f)+3*Mathf.Sin(z*.15f);
                float rightBend=11*Mathf.Sin(z*.064f+.65f);
                for(int column=0;column<width;column++)
                {
                    float x=((column+.5f)/width*2-1)*halfWidth;
                    float left=x+34-leftBend;
                    float right=x-34-rightBend;
                    var paint=Color.Lerp(sky,Color.Lerp(lavender,lilac,Mathf.SmoothStep(0,1,(z+25)/59)),
                        Mathf.Clamp01(.5f+Mathf.Min(left,-right)*5));
                    float loop=Mathf.Abs(Mathf.Sqrt((x-3)*(x-3)*.35f+(z+14)*(z+14))-18.5f);
                    float distance=Mathf.Min(loop,Mathf.Min(Mathf.Abs(left+2.2f),Mathf.Abs(right-2.3f)));
                    float grain=.978f+(((column*37+row*17)^(column*7+row*39))&15)*.0025f;
                    paint=Color.Lerp(paint,stripe,1-Mathf.InverseLerp(.76f,.94f,distance));
                    pixels[row*width+column]=new Color(paint.r*grain,paint.g*grain,paint.b*grain,1);
                }
            }
            var random=new System.Random(321);
            var dots=new[]{new Color32(249,186,183,255), new Color32(255,224,153,255),
                new Color32(156,208,211,255),new Color32(209,184,233,255),new Color32(243,220,235,255)};
            for(int i=0;i<112;i++)
            {
                int x=random.Next(width), z=random.Next(height), radius=random.Next(3,11);
                for(int row=Mathf.Max(0,z-radius);row<=Mathf.Min(height-1,z+radius);row++)
                    for(int col=Mathf.Max(0,x-radius);col<=Mathf.Min(width-1,x+radius);col++)
                    {
                        float d=Vector2.Distance(new Vector2(col,row),new Vector2(x,z));
                        int index=row*width+col;
                        pixels[index]=Color32.Lerp(pixels[index],dots[i%dots.Length],Mathf.Clamp01(radius-d));
                    }
            }
            var map=new Texture2D(width,height,TextureFormat.RGB24,true)
            {
                name="Blue and lavender rubber with painted stripes",
                wrapMode=TextureWrapMode.Clamp, anisoLevel=4, filterMode=FilterMode.Trilinear
            };
            map.SetPixels32(pixels);
            map.Apply(true,true);
            var material=Paint("Matte rubber plaza","#FFFFFF");
            material.SetColor("_Shade",new Color(.77f,.80f,.91f));
            material.mainTexture=map;
            material.EnableKeyword("PEACE_PARK_PAINT_MAP");

            var vertices=new Vector3[4*(steps+1)+1];
            var uv=new Vector2[vertices.Length];
            var triangles=new int[(vertices.Length-1)*3];
            vertices[0]=new Vector3(0,.015f,centerZ);
            uv[0]=new Vector2(.5f,.5f);
            for(int corner=0;corner<4;corner++)
            {
                float xSign=corner==0||corner==3?1:-1;
                float zSign=corner<2?1:-1;
                for(int step=0;step<=steps;step++)
                {
                    float angle=(corner+(float)step/steps)*Mathf.PI*.5f;
                    float x=xSign*(halfWidth-cornerRadius)+Mathf.Cos(angle)*cornerRadius;
                    float z=zSign*(halfDepth-cornerRadius)+Mathf.Sin(angle)*cornerRadius;
                    int index=1+corner*(steps+1)+step;
                    vertices[index]=new Vector3(x,.015f,centerZ+z);
                    uv[index]=new Vector2(.5f+x/(2*halfWidth),.5f+z/(2*halfDepth));
                    int triangle=(index-1)*3;
                    triangles[triangle]=0;
                    triangles[triangle+1]=index==vertices.Length-1?1:index+1;
                    triangles[triangle+2]=index;
                }
            }
            var mesh=new Mesh{name="Rounded rubber plaza",vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            ParkGeometry.Place("Continuous painted rubber plaza",mesh,root,Vector3.zero,Vector3.one,material);
        }
    }
}
