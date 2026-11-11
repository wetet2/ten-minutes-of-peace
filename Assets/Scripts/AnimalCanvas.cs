using UnityEngine;

namespace PeacePark
{
    // The silhouette mask and immutable outline are separate from the child's paint.
    public sealed class AnimalCanvas
    {
        public const int Width=960, Height=1200;
        public readonly Texture2D Paint, Outline;
        readonly bool[] mask=new bool[Width*Height];
        readonly Color32[] pixels=new Color32[Width*Height];
        public bool IsInside(int x,int y) {return x>=0&&y>=0&&x<Width&&y<Height&&mask[y*Width+x];}
        static bool Ellipse(float x,float y,float cx,float cy,float rx,float ry)
        {return Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2)<=1;}
        public AnimalCanvas(int animal,bool back)
        {
            Paint=new Texture2D(Width,Height,TextureFormat.RGBA32,false){name="Child paint",filterMode=FilterMode.Bilinear};
            Outline=new Texture2D(Width,Height,TextureFormat.RGBA32,false){name="Template outline",filterMode=FilterMode.Bilinear};
            var lines=new Color32[Width*Height];
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
            {
                float u=(float)x/Width,v=(float)y/Height;
                // Shared upright toddler-like body for all five species.
                // Big-headed animation-character proportion. The body stays anchored
                // to the floor while becoming shorter and narrower; the head grows
                // around its own visual centre so facial details keep their ratios.
                const float bodyScaleX=.84f,bodyScaleY=.90f,headScale=1.16f;
                float bodyX=.5f+(u-.5f)/bodyScaleX;
                float bodyY=.03f+(v-.03f)/bodyScaleY;
                float shape=BodyDistance(bodyX,bodyY)*Mathf.Min(bodyScaleX,bodyScaleY);
                float headX=.5f+(u-.5f)/headScale;
                // Scale around a high pivot so tall ears and horns grow downward
                // into the available canvas instead of being clipped at the top.
                float headY=.94f+(v-.94f)/headScale;
                AnimalHead.Sample(animal,headX,headY,back,out float head,out Color feature);
                head*=headScale;
                shape=Merge(shape,head,.009f);
                int index=y*Width+x;mask[index]=shape<0;
                pixels[index]=new Color(1,1,1,Mathf.Clamp01(.5f-shape*Width));
                Color ink=new Color32(46,38,35,255);
                float line=shape;
                bool detail=Mathf.Abs(head)<.0038f&&bodyY<.74f;

                if(back)
                {
                    float tail=E(bodyX,bodyY,.5f,.32f,.037f,.031f);
                    if(animal==2)for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;tail=Mathf.Min(tail,E(bodyX,bodyY,.5f+Mathf.Cos(angle)*.028f,.32f+Mathf.Sin(angle)*.023f,.018f,.016f));}
                    detail|=animal!=3&&Mathf.Abs(tail)<.003f;
                }
                if(mask[index])
                {
                    if(feature.a>0)lines[index]=feature;
                    if(detail)lines[index]=ink;
                    float edge=Mathf.Clamp01((.007f-Mathf.Abs(line))*Width);
                    if(edge>0){ink.a=edge;lines[index]=ink;}
                }
            }
            Paint.SetPixels32(pixels);Paint.Apply();Outline.SetPixels32(lines);Outline.Apply();
        }
        // Hand-shaped cubic outline: no separate wrist, ankle or protruding foot blobs.
        // Rasterized once so all ten templates share the identical soft body silhouette.
        static float[] bodyField;
        public static float BodyDistance(float x,float y)
        {
            if(bodyField==null) BuildBodyField();
            int ix=Mathf.Clamp(Mathf.RoundToInt(x*Width),0,Width-1),iy=Mathf.Clamp(Mathf.RoundToInt(y*Height),0,Height-1);
            return bodyField[iy*Width+ix];
        }
        static void BuildBodyField()
        {
            var points=new System.Collections.Generic.List<Vector2>();
            Vector2 current=new Vector2(.5f,.615f);
            points.Add(current);
            AddCurve(points,ref current,new Vector2(.65f,.615f),new Vector2(.74f,.55f),new Vector2(.80f,.475f));
            AddCurve(points,ref current,new Vector2(.85f,.414f),new Vector2(.88f,.36f),new Vector2(.83f,.335f));
            AddCurve(points,ref current,new Vector2(.78f,.31f),new Vector2(.735f,.345f),new Vector2(.71f,.405f));
            AddCurve(points,ref current,new Vector2(.735f,.30f),new Vector2(.725f,.18f),new Vector2(.72f,.085f));
            AddCurve(points,ref current,new Vector2(.72f,.035f),new Vector2(.675f,.025f),new Vector2(.625f,.03f));
            AddCurve(points,ref current,new Vector2(.55f,.028f),new Vector2(.55f,.05f),new Vector2(.545f,.105f));
            AddCurve(points,ref current,new Vector2(.543f,.15f),new Vector2(.53f,.188f),new Vector2(.5f,.195f));
            int rightCount=points.Count;
            for(int i=rightCount-2;i>=0;i--)points.Add(new Vector2(1-points[i].x,points[i].y));
            bodyField=new float[Width*Height];
            // Scanline even/odd fill initializes the sign independently of contour distance.
            var crossings=new System.Collections.Generic.List<float>();
            for(int y=0;y<Height;y++)
            {
                float py=(float)y/Height;crossings.Clear();
                for(int j=0;j<points.Count-1;j++)
                {
                    var a=points[j];var b=points[j+1];
                    if((a.y>py)!=(b.y>py))crossings.Add(a.x+(py-a.y)*(b.x-a.x)/(b.y-a.y));
                }
                crossings.Sort();
                for(int x=0;x<Width;x++)
                {
                    float px=(float)x/Width;bool inside=false;
                    for(int j=0;j+1<crossings.Count;j+=2)if(px>=crossings[j]&&px<=crossings[j+1]){inside=true;break;}
                    bodyField[y*Width+x]=inside?-1:1;
                }
            }
            const float band=.014f;
            for(int j=0;j<points.Count-1;j++)
            {
                var a=points[j];var b=points[j+1];var ab=b-a;
                int x0=Mathf.Max(0,Mathf.FloorToInt((Mathf.Min(a.x,b.x)-band)*Width)),x1=Mathf.Min(Width-1,Mathf.CeilToInt((Mathf.Max(a.x,b.x)+band)*Width));
                int y0=Mathf.Max(0,Mathf.FloorToInt((Mathf.Min(a.y,b.y)-band)*Height)),y1=Mathf.Min(Height-1,Mathf.CeilToInt((Mathf.Max(a.y,b.y)+band)*Height));
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    var pa=new Vector2((float)x/Width,(float)y/Height)-a;
                    float d=(pa-ab*Mathf.Clamp01(Vector2.Dot(pa,ab)/Mathf.Max(ab.sqrMagnitude,.0000001f))).magnitude;
                    int index=y*Width+x;bodyField[index]=Mathf.Sign(bodyField[index])*Mathf.Min(Mathf.Abs(bodyField[index]),d);
                }
            }
        }
        static void AddCurve(System.Collections.Generic.List<Vector2> points,ref Vector2 current,Vector2 c1,Vector2 c2,Vector2 end)
        {
            Vector2 start=current;
            for(int i=1;i<=16;i++){float t=i/16f,q=1-t;points.Add(q*q*q*start+3*q*q*t*c1+3*q*t*t*c2+t*t*t*end);}
            current=end;
        }
        static float Capsule(float x,float y,Vector2 a,Vector2 b,float radius)
        {var p=new Vector2(x,y)-a;var ab=b-a;return (p-ab*Mathf.Clamp01(Vector2.Dot(p,ab)/ab.sqrMagnitude)).magnitude-radius;}
        static float E(float x,float y,float cx,float cy,float rx,float ry)
        {float a=(x-cx)/rx,b=(y-cy)/ry;return (Mathf.Sqrt(a*a+b*b)-1)*Mathf.Min(rx,ry);}
        static float Merge(float a,float b,float k)
        {float h=Mathf.Clamp01(.5f+.5f*(b-a)/k);return Mathf.Lerp(b,a,h)-k*h*(1-h);}
        static float Ear(float x,float y,float cx,float cy,float angle)
        {float a=x-cx,b=y-cy;return E(a*Mathf.Cos(angle)-b*Mathf.Sin(angle),a*Mathf.Sin(angle)+b*Mathf.Cos(angle),0,0,.083f,.148f);}
        static bool Ring(float x,float y,float cx,float cy,float rx,float ry,float thickness)
        {return Ellipse(x,y,cx,cy,rx,ry)&&!Ellipse(x,y,cx,cy,rx-thickness,ry-thickness);}
        public void Stroke(Vector2 from,Vector2 to,float radius,Color color)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/Mathf.Max(1,radius*.4f)));
            for(int s=0;s<=steps;s++)
            {
                var p=Vector2.Lerp(from,to,(float)s/steps);
                for(int y=Mathf.Max(0,Mathf.FloorToInt(p.y-radius));y<=Mathf.Min(Height-1,Mathf.CeilToInt(p.y+radius));y++)
                for(int x=Mathf.Max(0,Mathf.FloorToInt(p.x-radius));x<=Mathf.Min(Width-1,Mathf.CeilToInt(p.x+radius));x++)
                    if(mask[y*Width+x]&&(new Vector2(x,y)-p).sqrMagnitude<=radius*radius)pixels[y*Width+x]=color;
            }
            Paint.SetPixels32(pixels);Paint.Apply();
        }
        public void Dispose(){Object.DestroyImmediate(Paint);Object.DestroyImmediate(Outline);}
    }
}
