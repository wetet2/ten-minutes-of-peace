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
        static float Horn(float x,float y,Vector2 from,Vector2 to,float baseRadius,float tipRadius)
        {
            var axis=to-from;
            var offset=new Vector2(x,y)-from;
            float along=Mathf.Clamp01(Vector2.Dot(offset,axis)/axis.sqrMagnitude);
            return (offset-axis*along).magnitude-Mathf.Lerp(baseRadius,tipRadius,along);
        }
        static void Layer(ref Color c,Color color,float coverage)
        {float a=Mathf.Clamp01(coverage);if(a<=0)return;float total=a+c.a*(1-a);var rgb=(new Vector3(color.r,color.g,color.b)*a+new Vector3(c.r,c.g,c.b)*c.a*(1-a))/total;c=new Color(rgb.x,rgb.y,rgb.z,total);}
        static void Ring(ref Color c,float d,float thickness=.0028f){Layer(ref c,ink,(thickness-Mathf.Abs(d))*1200+.5f);}
        static void Fill(ref Color c,float d,Color color){Layer(ref c,color,.5f-d*1200);}
        static void Eye(ref Color c,float x,float y,float cx,float cy,float size=.012f)
        {Fill(ref c,E(x,y,cx,cy,size,size*1.15f),ink);Fill(ref c,E(x,y,cx-.003f,cy+.005f,.0036f,.0036f),Color.white);}
        static void Smile(ref Color c,float x,float y,float cx,float cy,float width)
        {if(y<cy)Ring(ref c,E(x,y,cx,cy,width,.022f),.0023f);}
        static float SmileHeight(float x,float halfWidth,float center,float lift)
        {
            float t=(x-.5f)/halfWidth;
            return center+lift*t*t;
        }

        static void Tooth(ref Color face,float x,float y,float toothX,float rootY,float width,float length,bool rounded)
        {
            if(y>rootY)return;
            float depth=Mathf.Clamp01((rootY-y)/length);
            float edge=Mathf.Lerp(width,width*(rounded?.78f:.36f),depth);
            float tip=rootY-length;
            float distance=Mathf.Max(Mathf.Abs(x-toothX)-edge,tip-y);
            // The mouth line is the top edge of the tooth.
            Ring(ref face,distance,.0019f);
        }
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
                case 4: // Giraffe: a long neck, leaf ears, capped ossicones and a soft muzzle.
                    shape=E(x,y,.5f,.777f,.186f,.151f);
                    shape=Join(shape,E(x,y,.5f,.690f,.182f,.066f),.022f);
                    shape=Join(shape,E(x,y,.5f,.573f,.080f,.124f),.025f);
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
                    Eye(ref face,x,y,.416f,.783f);Eye(ref face,x,y,.584f,.783f);
                    Fill(ref face,E(x,y,.455f,.705f,.008f,.009f,-.2f),ink);Fill(ref face,E(x,y,.545f,.705f,.008f,.009f,.2f),ink);
                    Smile(ref face,x,y,.5f,.689f,.042f);
                    if(y<.656f&&Mathf.Abs(x-.5f)<.083f)
                        Ring(ref face,E(x,y,.5f,.682f,.117f,.053f),.0019f);
                    break;
                case 5: // Elephant: large ears and a long trunk overlapping the chest.
                    shape=E(x,y,.5f,.740f,.211f,.158f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        shape=Join(shape,E(x,y,.5f+side*.222f,.735f,.105f,.132f,side*-.23f));
                        if(!back)
                        {
                            Ring(ref face,E(x,y,.5f+side*.225f,.738f,.072f,.099f,side*-.23f));
                            Eye(ref face,x,y,.5f+side*.107f,.771f,.014f);
                        }
                    }
                    if(back)break;
                    float trunkY=Mathf.Clamp(y,.522f,.727f);
                    float trunkRadius=Mathf.Lerp(.033f,.052f,Mathf.InverseLerp(.522f,.727f,trunkY));
                    float trunk=new Vector2(x-.5f,y-trunkY).magnitude-trunkRadius;
                    shape=Join(shape,trunk,.015f);
                    float outline=Mathf.Clamp01((.0022f-Mathf.Abs(trunk))*1200+.5f);
                    Layer(ref face,ink,outline*Mathf.Clamp01((.730f-y)/.029f));
                    Fill(ref face,E(x,y,.487f,.514f,.007f,.008f),ink);
                    Fill(ref face,E(x,y,.513f,.514f,.007f,.008f),ink);
                    break;
                case 6: // Triceratops: scalloped frill, two brow horns and one nose horn.
                    float frill=E(x,y,.5f,.797f,.302f,.151f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        frill=Join(frill,E(x,y,.5f+side*.232f,.743f,.094f,.101f,side*-.25f),.024f);
                        frill=Join(frill,E(x,y,.5f+side*.263f,.860f,.067f,.055f),.018f);
                        frill=Join(frill,E(x,y,.5f+side*.153f,.918f,.059f,.045f),.017f);
                    }
                    frill=Join(frill,E(x,y,.5f,.946f,.062f,.039f),.016f);
                    float neck=E(x,y,.5f,.636f,.077f,.086f);
                    frill=Join(frill,neck,.022f);
                    if(back){shape=frill;break;}
                    float faceShape=Join(E(x,y,.5f,.745f,.195f,.131f),E(x,y,.5f,.686f,.145f,.064f),.020f);
                    shape=Join(frill,faceShape,.019f);
                    float horns=Horn(x,y,new Vector2(.5f,.723f),new Vector2(.5f,.795f),.034f,.016f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        float brow=Horn(x,y,new Vector2(.5f+side*.100f,.819f),
                            new Vector2(.5f+side*.151f,.910f),.040f,.014f);
                        horns=Mathf.Min(horns,brow);
                    }
                    shape=Join(shape,horns,.008f);
                    if(horns>.002f)Ring(ref face,faceShape,.0024f);
                    Ring(ref face,horns,.0024f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(ref face,x,y,.5f+side*.092f,.750f,.015f);
                        Fill(ref face,E(x,y,.5f+side*.052f,.707f,.006f,.007f),ink);
                        Fill(ref face,E(x,y,.5f+side*.130f,.712f,.015f,.009f),rose);
                    }
                    Smile(ref face,x,y,.5f,.689f,.067f);
                    break;
                case (int)AnimalKind.Crocodile:
                    float snout=E(x,y,.5f,.677f,.220f,.080f);
                    shape=Join(E(x,y,.5f,.752f,.230f,.139f),snout,.021f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        float cx=.5f+side*.137f;
                        shape=Join(shape,E(x,y,cx,.845f,.069f,.066f));
                        if(back)continue;
                        Eye(ref face,x,y,cx,.834f,.017f);
                        Fill(ref face,E(x,y,.5f+side*.108f,.716f,.012f,.007f,side*.22f),ink);
                        float toothX=.5f+side*.129f;
                        Tooth(ref face,x,y,toothX,SmileHeight(x,.183f,.654f,.023f),.0115f,.024f,false);
                    }
                    if(!back)
                    {
                        float mouthX=Mathf.Clamp(x,.317f,.683f);
                        float mouthY=SmileHeight(mouthX,.183f,.654f,.023f);
                        Fill(ref face,new Vector2(x-mouthX,y-mouthY).magnitude-.0027f,ink);
                    }
                    break;
                case (int)AnimalKind.Hippo:
                    float hippoMuzzle=E(x,y,.5f,.695f,.228f,.080f);
                    shape=Join(E(x,y,.5f,.768f,.215f,.143f),hippoMuzzle,.023f);
                    shape=Join(shape,E(x,y,.5f,.643f,.144f,.062f),.019f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        float cx=.5f+side*.172f;
                        shape=Join(shape,E(x,y,cx,.877f,.047f,.044f));
                        if(back)continue;
                        Ring(ref face,E(x,y,cx,.880f,.024f,.021f));
                        Eye(ref face,x,y,.5f+side*.112f,.796f,.014f);
                        Fill(ref face,E(x,y,.5f+side*.121f,.714f,.019f,.010f,side*.25f),ink);
                        Tooth(ref face,x,y,.5f+side*.043f,SmileHeight(x,.151f,.655f,.024f),.016f,.026f,true);
                    }
                    if(!back)
                    {
                        float mouthX=Mathf.Clamp(x,.349f,.651f);
                        float mouthY=SmileHeight(mouthX,.151f,.655f,.024f);
                        Fill(ref face,new Vector2(x-mouthX,y-mouthY).magnitude-.0027f,ink);
                    }
                    break;
                case (int)AnimalKind.Gorilla:
                    shape=Join(E(x,y,.5f,.775f,.226f,.154f),E(x,y,.5f,.686f,.200f,.101f),.031f);
                    for(int side=-1;side<=1;side+=2)
                        shape=Join(shape,E(x,y,.5f+side*.207f,.753f,.046f,.051f),.014f);
                    if(back)break;
                    float gorillaBrow=Join(E(x,y,.420f,.782f,.096f,.071f,-.12f),E(x,y,.580f,.782f,.096f,.071f,.12f),.025f);
                    float faceMask=Join(gorillaBrow,E(x,y,.5f,.702f,.155f,.089f),.039f);
                    Ring(ref face,faceMask,.0024f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        float cx=.5f+side*.077f;
                        Eye(ref face,x,y,cx,.783f,.015f);
                        Fill(ref face,E(x,y,.5f+side*.035f,.727f,.014f,.008f,side*.35f),ink);
                        if(side*(x-.5f)>.213f)
                            Ring(ref face,E(x,y,.5f+side*.222f,.753f,.017f,.030f),.0018f);
                    }
                    if(y>.726f)Ring(ref face,E(x,y,.5f,.726f,.061f,.028f),.0019f);
                    Smile(ref face,x,y,.5f,.687f,.078f);
                    break;
                default:
                    shape=1;
                    break;
            }
        }
    }
}
