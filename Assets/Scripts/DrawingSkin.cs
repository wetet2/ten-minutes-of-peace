using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    // Rasterize soft bevels and contact shadows once, not on every GUI repaint.
    public sealed class DrawingSkin
    {
        readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        public void Draw(Rect r,Color color,float radius,bool glossy=false)
        {
            string key=$"{r.width:F0}:{r.height:F0}:{radius:F0}:{ColorUtility.ToHtmlStringRGBA(color)}:{glossy}";
            if(!cache.TryGetValue(key,out var texture))
            {
                const int pad=12;int w=Mathf.CeilToInt(r.width),h=Mathf.CeilToInt(r.height);
                texture=new Texture2D(w+pad*2,h+pad*2,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};
                var pixels=new Color[(w+pad*2)*(h+pad*2)];
                for(int y=0;y<h+pad*2;y++)for(int x=0;x<w+pad*2;x++)
                {
                    float px=x-pad-w*.5f,py=y-pad-h*.5f;
                    float d=Distance(px,py,w,h,radius);
                    float shadow=Distance(px,py+4,w,h,radius);
                    Color c=new Color(.47f,.31f,.12f,Mathf.Exp(-Mathf.Max(0,shadow)/3)*.22f);
                    float alpha=Mathf.Clamp01(.5f-d);
                    if(alpha>0)
                    {
                        float v=Mathf.Clamp01((py+h*.5f)/h);
                        float bevel=Mathf.Exp(-Mathf.Abs(d+2)/2.2f);
                        float light=(v-.45f)*(glossy?.26f:.045f)+bevel*(v-.45f)*.6f;
                        Color fill=light>0?Color.Lerp(color,Color.white,light):Color.Lerp(color,new Color(.45f,.31f,.12f),-light);
                        if(glossy)
                        {
                            float shine=Mathf.Exp(-Mathf.Pow((px+w*.17f)/(w*.6f),2)-Mathf.Pow((py-h*.31f)/(h*.11f),2))*.3f;
                            fill=Color.Lerp(fill,Color.white,shine);
                        }
                        c=Color.Lerp(c,fill,alpha);c.a=Mathf.Max(c.a,alpha);
                    }
                    pixels[y*(w+pad*2)+x]=c;
                }
                texture.SetPixels(pixels);texture.Apply();cache.Add(key,texture);
            }
            GUI.DrawTexture(new Rect(r.x-12,r.y-12,r.width+24,r.height+24),texture);
        }
        static float Distance(float x,float y,float w,float h,float r)
        {float qx=Mathf.Abs(x)-w*.5f+r,qy=Mathf.Abs(y)-h*.5f+r;return new Vector2(Mathf.Max(qx,0),Mathf.Max(qy,0)).magnitude+Mathf.Min(Mathf.Max(qx,qy),0)-r;}
        public void Dispose(){foreach(var t in cache.Values)Object.Destroy(t);cache.Clear();}
    }
}
