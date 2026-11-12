using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace PeacePark
{
    // The silhouette mask and immutable outline are separate from the child's paint.
    public sealed class AnimalCanvas
    {
        public const int Width=960, Height=1200;
        public readonly Texture2D Paint, Outline;
        readonly byte[] mask=new byte[Width*Height];
        readonly byte[] initialAlpha=new byte[Width*Height];
        // Canvas texture operations run on Unity's main thread.
        static readonly byte[] outlineBuffer=new byte[Width*Height*4];
        public bool IsInside(int x,int y) {return x>=0&&y>=0&&x<Width&&y<Height&&mask[y*Width+x]!=0;}
        static bool Ellipse(float x,float y,float cx,float cy,float rx,float ry)
        {return Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2)<=1;}
        public AnimalCanvas(int animal,bool back,bool useBaked=true)
        {
            Paint=new Texture2D(Width,Height,TextureFormat.RGBA32,false){name="Child paint",filterMode=FilterMode.Bilinear};
            Outline=new Texture2D(Width,Height,TextureFormat.RGBA32,false){name="Template outline",filterMode=FilterMode.Bilinear};
            SetTemplate(animal,back,useBaked);
        }

        public static string TemplateResource(int animal,bool back) =>
            "Drawing/Templates/"+animal+(back?"-back":"-front");

        public void SetTemplate(int animal,bool back,bool useBaked=true)
        {
            if(useBaked&&TryLoadTemplate(animal,back))return;
            Rasterize(animal,back);
        }

        bool TryLoadTemplate(int animal,bool back)
        {
            var asset=Resources.Load<TextAsset>(TemplateResource(animal,back));
            if(!asset)return false;
            try
            {
                using var source=new MemoryStream(asset.bytes,false);
                using var zip=new DeflateStream(source,CompressionMode.Decompress);
                using var header=new BinaryReader(zip);
                if(header.ReadInt32()!=Width||header.ReadInt32()!=Height)return false;
                ReadFully(zip,mask);
                ReadFully(zip,initialAlpha);
                ReadFully(zip,outlineBuffer);
                Outline.LoadRawTextureData(outlineBuffer);
                Outline.Apply(false);
                Clear();
                return true;
            }
            catch(IOException)
            {
                return false;
            }
            catch(InvalidDataException)
            {
                return false;
            }
        }

        static void ReadFully(Stream source,byte[] destination)
        {
            int offset=0;
            while(offset<destination.Length)
            {
                int read=source.Read(destination,offset,destination.Length-offset);
                if(read==0)throw new EndOfStreamException();
                offset+=read;
            }
        }

        public void WriteTemplate(Stream destination)
        {
            using var zip=new DeflateStream(destination,System.IO.Compression.CompressionLevel.Optimal,true);
            using var writer=new BinaryWriter(zip);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(mask);
            writer.Write(initialAlpha);
            writer.Write(Outline.GetRawTextureData());
        }

        void Rasterize(int animal,bool back)
        {
            var lines=new Color32[Width*Height];
            var pixels=Paint.GetRawTextureData<Color32>();
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
                // Lower the giraffe's shoulders in the template so its neck is visible
                // in the silhouette instead of being outlined across the chest.
                if(animal==(int)AnimalKind.Giraffe&&bodyY>.32f)
                    bodyY=.32f+(bodyY-.32f)/.75f;
                float shape=BodyDistance(bodyX,bodyY)*Mathf.Min(bodyScaleX,bodyScaleY);
                float headX=.5f+(u-.5f)/headScale;
                // Scale around a high pivot so tall ears and horns grow downward
                // into the available canvas instead of being clipped at the top.
                float headY=.94f+(v-.94f)/headScale;
                AnimalHead.Sample(animal,headX,headY,back,out float head,out Color feature);
                head*=headScale;
                shape=Merge(shape,head,.009f);
                float tail=back?TailDistance((AnimalKind)animal,bodyX,bodyY):1f;
                if(back&&animal!=(int)AnimalKind.Rabbit)
                    shape=Merge(shape,tail*Mathf.Min(bodyScaleX,bodyScaleY),.006f);
                int index=y*Width+x;mask[index]=(byte)(shape<0?1:0);
                pixels[index]=new Color(1,1,1,Mathf.Clamp01(.5f-shape*Width));
                initialAlpha[index]=pixels[index].a;
                Color ink=new Color32(46,38,35,255);
                float line=shape;
                bool detail=animal!=(int)AnimalKind.Giraffe&&Mathf.Abs(head)<.0038f&&bodyY<.74f;
                if(animal==(int)AnimalKind.Triceratops&&headY<.63f)detail=false;

                detail|=back&&Mathf.Abs(tail)<.003f;
                if(mask[index]!=0)
                {
                    if(feature.a>0)lines[index]=feature;
                    if(detail)lines[index]=ink;
                    float edge=Mathf.Clamp01((.007f-Mathf.Abs(line))*Width);
                    if(edge>0){ink.a=edge;lines[index]=ink;}
                }
            }
            Paint.Apply(false);Outline.SetPixels32(lines);Outline.Apply(false);
        }
        static float TailDistance(AnimalKind kind,float x,float y)
        {
            switch(kind)
            {
                case AnimalKind.Rabbit:
                    float tail=E(x,y,.5f,.32f,.037f,.031f);
                    for(int k=0;k<6;k++)
                    {
                        float angle=k*Mathf.PI/3;
                        tail=Mathf.Min(tail,E(x,y,.5f+Mathf.Cos(angle)*.028f,.32f+Mathf.Sin(angle)*.023f,.018f,.016f));
                    }
                    return tail;
                case AnimalKind.Bear:return E(x,y,.5f,.32f,.032f,.025f);
                case AnimalKind.Tiger:
                    var from=new Vector2(.50f,.35f);
                    var axis=new Vector2(.82f,.22f)-from;
                    var offset=new Vector2(x,y)-from;
                    float along=Mathf.Clamp01(Vector2.Dot(offset,axis)/axis.sqrMagnitude);
                    return (offset-axis*along).magnitude-Mathf.Lerp(.025f,.019f,along);
                case AnimalKind.Triceratops:
                    var root=new Vector2(.50f,.34f);
                    var tailAxis=new Vector2(.83f,.23f)-root;
                    var tailOffset=new Vector2(x,y)-root;
                    float tailT=Mathf.Clamp01(Vector2.Dot(tailOffset,tailAxis)/tailAxis.sqrMagnitude);
                    return (tailOffset-tailAxis*tailT).magnitude-Mathf.Lerp(.046f,.020f,tailT);
                case AnimalKind.Giraffe:
                case AnimalKind.Elephant:
                    float end=kind==AnimalKind.Giraffe?.618f:.414f;
                    return Mathf.Min(Capsule(x,y,new Vector2(.50f,.35f),new Vector2(end,.245f),.012f),
                        E(x,y,end,.237f,.021f,.026f));
                default:return 1f;
            }
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
            var pixels=Paint.GetRawTextureData<Color32>();
            Color32 rgba=color;
            int steps=Mathf.CeilToInt(Vector2.Distance(from,to)/Mathf.Max(1,radius*.4f));
            float radiusSquared=radius*radius;
            bool changed=false;
            for(int step=0;step<=steps;step++)
            {
                var point=steps==0?from:Vector2.Lerp(from,to,(float)step/steps);
                int firstX=Mathf.Max(0,Mathf.FloorToInt(point.x-radius));
                int lastX=Mathf.Min(Width-1,Mathf.CeilToInt(point.x+radius));
                int firstY=Mathf.Max(0,Mathf.FloorToInt(point.y-radius));
                int lastY=Mathf.Min(Height-1,Mathf.CeilToInt(point.y+radius));
                for(int y=firstY;y<=lastY;y++)
                {
                    float dy=y-point.y;
                    float rowRadiusSquared=radiusSquared-dy*dy;
                    int row=y*Width;
                    for(int x=firstX;x<=lastX;x++)
                    {
                        float dx=x-point.x;
                        int index=row+x;
                        if(dx*dx>rowRadiusSquared||mask[index]==0)continue;
                        var previous=pixels[index];
                        if(previous.r==rgba.r&&previous.g==rgba.g&&previous.b==rgba.b&&previous.a==rgba.a)continue;
                        pixels[index]=rgba;
                        changed=true;
                    }
                }
            }
            if(changed)Paint.Apply(false);
        }
        public void Clear()
        {
            var pixels=Paint.GetRawTextureData<Color32>();
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,initialAlpha[i]);
            Paint.Apply(false);
        }
        public void Dispose(){Object.DestroyImmediate(Paint);Object.DestroyImmediate(Outline);}
    }
}
