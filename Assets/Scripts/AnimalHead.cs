using UnityEngine;

namespace PeacePark
{
    // Species-specific contours and facial layers; all coordinates are shared canvas space.
    public static class AnimalHead
    {
        static readonly Color ink=new Color32(44,43,44,255);
        static readonly Color rose=new Color32(247,131,165,255);
        static float E(float x,float y,float cx,float cy,float rx,float ry,float angle=0)
        {
            float dx=x-cx,dy=y-cy;
            float a=(dx*Mathf.Cos(angle)-dy*Mathf.Sin(angle))/rx,b=(dx*Mathf.Sin(angle)+dy*Mathf.Cos(angle))/ry;
            return (Mathf.Sqrt(a*a+b*b)-1)*Mathf.Min(rx,ry);
        }
        static float Join(float a,float b,float k=.012f)
        {float h=Mathf.Clamp01(.5f+.5f*(b-a)/k);return Mathf.Lerp(b,a,h)-k*h*(1-h);}
        static float Line(float x,float y,float ax,float ay,float bx,float by)
        {var p=new Vector2(x-ax,y-ay);var v=new Vector2(bx-ax,by-ay);return (p-v*Mathf.Clamp01(Vector2.Dot(p,v)/v.sqrMagnitude)).magnitude;}
        static void Layer(ref Color c,Color color,float coverage)
        {float a=Mathf.Clamp01(coverage);if(a<=0)return;float total=a+c.a*(1-a);var rgb=(new Vector3(color.r,color.g,color.b)*a+new Vector3(c.r,c.g,c.b)*c.a*(1-a))/total;c=new Color(rgb.x,rgb.y,rgb.z,total);}
        static void Ring(ref Color c,float d,float thickness=.0028f){Layer(ref c,ink,(thickness-Mathf.Abs(d))*1200+.5f);}
        static void Fill(ref Color c,float d,Color color){Layer(ref c,color,.5f-d*1200);}
        static void Eye(ref Color c,float x,float y,float cx,float cy,float size=.012f)
        {Fill(ref c,E(x,y,cx,cy,size,size*1.15f),ink);Fill(ref c,E(x,y,cx-.003f,cy+.005f,.0036f,.0036f),Color.white);}
        static void Smile(ref Color c,float x,float y,float cx,float cy,float width)
        {if(y<cy)Ring(ref c,E(x,y,cx,cy,width,.022f),.0023f);}
        public static void Sample(int kind,float x,float y,bool back,out float shape,out Color face)
        {
            face=Color.clear;
            switch(kind)
            {
                case 0: // Bear: round ears and a broad oval muzzle.
                    shape=E(x,y,.5f,.735f,.26f,.16f);
                    shape=Join(shape,E(x,y,.315f,.854f,.068f,.06f));shape=Join(shape,E(x,y,.685f,.854f,.068f,.06f));
                    if(back)break;
                    Ring(ref face,E(x,y,.315f,.854f,.042f,.036f));Ring(ref face,E(x,y,.685f,.854f,.042f,.036f));
                    Ring(ref face,E(x,y,.5f,.681f,.103f,.065f));
                    Eye(ref face,x,y,.407f,.754f);Eye(ref face,x,y,.593f,.754f);
                    Fill(ref face,E(x,y,.5f,.7f,.026f,.015f),ink);
                    Ring(ref face,Line(x,y,.5f,.69f,.5f,.673f),.0022f);
                    Smile(ref face,x,y,.478f,.678f,.022f);Smile(ref face,x,y,.522f,.678f,.022f);
                    break;
                case 1: // Tiger: full cheeks, little rounded ears, forehead and cheek stripes.
                    shape=E(x,y,.5f,.738f,.258f,.157f);
                    shape=Join(shape,E(x,y,.32f,.855f,.06f,.05f));shape=Join(shape,E(x,y,.68f,.855f,.06f,.05f));
                    shape=Join(shape,E(x,y,.28f,.686f,.055f,.047f));shape=Join(shape,E(x,y,.72f,.686f,.055f,.047f));
                    if(!back)
                    {
                        Ring(ref face,E(x,y,.32f,.855f,.034f,.026f));Ring(ref face,E(x,y,.68f,.855f,.034f,.026f));
                        Ring(ref face,Mathf.Min(E(x,y,.462f,.68f,.064f,.044f),E(x,y,.538f,.68f,.064f,.044f)));
                        Eye(ref face,x,y,.401f,.755f);Eye(ref face,x,y,.599f,.755f);
                        Fill(ref face,E(x,y,.5f,.7f,.023f,.014f),rose);
                        Smile(ref face,x,y,.5f,.669f,.03f);
                    }
                    Fill(ref face,E(x,y,.5f,.853f,.012f,.035f),ink);
                    Fill(ref face,E(x,y,.455f,.859f,.009f,.025f,-.4f),ink);Fill(ref face,E(x,y,.545f,.859f,.009f,.025f,.4f),ink);
                    for(int side=-1;side<=1;side+=2)for(int j=0;j<3;j++)
                        Fill(ref face,E(x,y,.5f+side*(.22f-j*.008f),.77f-j*.037f,.035f-j*.004f,.007f,side*(.18f-j*.14f)),ink);
                    break;
                case 2: // Rabbit: long oval ears, small muzzle, whiskers and pink nose.
                    shape=E(x,y,.5f,.731f,.255f,.156f);
                    shape=Join(shape,E(x,y,.382f,.887f,.064f,.104f,-.1f));shape=Join(shape,E(x,y,.618f,.887f,.064f,.104f,.1f));
                    if(back)break;
                    float ear=Mathf.Min(E(x,y,.382f,.891f,.033f,.074f,-.1f),E(x,y,.618f,.891f,.033f,.074f,.1f));
                    Layer(ref face,rose,(.0025f-Mathf.Abs(ear))*1200+.5f);
                    Eye(ref face,x,y,.411f,.744f,.011f);Eye(ref face,x,y,.589f,.744f,.011f);
                    Fill(ref face,E(x,y,.5f,.706f,.019f,.011f),rose);
                    Ring(ref face,Line(x,y,.5f,.696f,.5f,.679f),.0021f);
                    Smile(ref face,x,y,.48f,.685f,.02f);Smile(ref face,x,y,.52f,.685f,.02f);
                    for(int side=-1;side<=1;side+=2)for(int j=0;j<2;j++)
                        Ring(ref face,Line(x,y,.5f+side*.09f,.694f-j*.018f,.5f+side*.15f,.7f-j*.028f),.0017f);
                    break;
                case 3: // Frog: wide low face and raised eye lobes; no mammalian muzzle or ears.
                    shape=E(x,y,.5f,.703f,.286f,.128f);
                    shape=Join(shape,E(x,y,.345f,.812f,.082f,.076f));shape=Join(shape,E(x,y,.655f,.812f,.082f,.076f));
                    if(back)break;
                    Ring(ref face,E(x,y,.345f,.812f,.052f,.05f));Ring(ref face,E(x,y,.655f,.812f,.052f,.05f));
                    Eye(ref face,x,y,.345f,.812f,.025f);Eye(ref face,x,y,.655f,.812f,.025f);
                    Fill(ref face,E(x,y,.471f,.725f,.006f,.009f),ink);Fill(ref face,E(x,y,.529f,.725f,.006f,.009f),ink);
                    if(y<.703f)Ring(ref face,E(x,y,.5f,.703f,.155f,.05f),.0028f);
                    break;
                default: // Giraffe: narrow forehead, leaf ears, capped ossicones and a small soft muzzle.
                    shape=E(x,y,.5f,.756f,.19f,.178f);
                    shape=Join(shape,E(x,y,.5f,.655f,.22f,.08f));
                    for(int side=-1;side<=1;side+=2)
                    {
                        shape=Join(shape,E(x,y,.5f+side*.225f,.844f,.09f,.04f,side*-.35f));
                        shape=Join(shape,Line(x,y,.5f+side*.075f,.888f,.5f+side*.092f,.952f)-.017f);
                        shape=Join(shape,E(x,y,.5f+side*.094f,.96f,.029f,.024f));
                        if(!back)Ring(ref face,E(x,y,.5f+side*.225f,.844f,.056f,.021f,side*-.35f));
                    }
                    // A few outlined patches remain colorable instead of pre-filling the head.
                    Ring(ref face,E(x,y,.5f,.877f,.039f,.027f));
                    Ring(ref face,E(x,y,.378f,.837f,.019f,.025f,-.3f));Ring(ref face,E(x,y,.622f,.837f,.019f,.025f,.3f));
                    if(back)break;
                    // The muzzle sits inside the lower face. It must not span from
                    // cheek to cheek or read like a second oversized head outline.
                    Ring(ref face,E(x,y,.5f,.666f,.135f,.052f));
                    Eye(ref face,x,y,.416f,.771f);Eye(ref face,x,y,.584f,.771f);
                    Fill(ref face,E(x,y,.455f,.681f,.008f,.011f,-.2f),ink);Fill(ref face,E(x,y,.545f,.681f,.008f,.011f,.2f),ink);
                    Smile(ref face,x,y,.5f,.65f,.042f);
                    break;
            }
        }
    }
}
