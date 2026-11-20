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

        static float RoundedRectangle(float x,float y,float cx,float cy,float halfWidth,float halfHeight,float radius)
        {
            var q=new Vector2(Mathf.Abs(x-cx)-halfWidth+radius,Mathf.Abs(y-cy)-halfHeight+radius);
            return Vector2.Max(q,Vector2.zero).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
        }

        static void Tooth(ref Color face,float x,float y,float toothX,float rootY,float width,float length)
        {
            if(y>rootY)return;
            float depth=Mathf.Clamp01((rootY-y)/length);
            float edge=Mathf.Lerp(width,width*.32f,depth);
            float tip=rootY-length;
            float distance=Mathf.Max(Mathf.Abs(x-toothX)-edge,tip-y);
            // The mouth line is the top edge of the tooth.
            Ring(ref face,distance,.0019f);
        }

        static float Tiger(float x,float y,bool back,ref Color face)
        {
            const float center=.5f;
            float side=x<center?-1f:1f;
            float offset=Mathf.Abs(x-center);
            float shape=E(x,y,center,.764f,.274f,.157f);
            shape=Join(shape,E(offset,y,.133f,.726f,.138f,.099f),.024f);
            shape=Join(shape,E(offset,y,.209f,.877f,.071f,.066f),.016f);
            if(shape>.006f)return shape;

            for(int stripe=0;stripe<3;stripe++)
            {
                float tipY=.770f-stripe*.045f;
                var from=new Vector2(center+side*.274f,tipY+.024f);
                var to=new Vector2(center+side*(.200f+stripe*.009f),tipY);
                Fill(ref face,Horn(x,y,from,to,.010f,.004f),ink);
            }
            for(int stripe=0;stripe<3;stripe++)
            {
                float stripeX=center+(stripe-1)*.073f;
                float rootY=.920f-Mathf.Abs(stripe-1)*.008f;
                Fill(ref face,Horn(x,y,new Vector2(stripeX,rootY),
                    new Vector2(Mathf.Lerp(stripeX,center,.16f),rootY-.047f),.011f,.004f),ink);
            }
            if(back)return shape;

            Eye(ref face,x,y,center+side*.114f,.761f,.026f);
            Ring(ref face,E(offset,y,.210f,.886f,.043f,.038f),.0029f);

            float pads=Join(E(offset,y,.053f,.689f,.092f,.048f),E(x,y,center,.676f,.108f,.038f),.015f);
            Ring(ref face,pads,.0027f);
            Fill(ref face,E(x,y,center,.714f,.031f,.017f),ink);
            for(int dot=0;dot<3;dot++)
                Fill(ref face,E(offset,y,.063f+dot*.021f,.692f+(dot%2)*.011f,.0045f,.0045f),ink);
            const float smileWidth=.086f,smileY=.683f;
            float mouthX=Mathf.Min(offset,smileWidth);
            float mouthY=smileY-.020f*Mathf.Sin(mouthX/smileWidth*Mathf.PI);
            Fill(ref face,new Vector2(offset-mouthX,y-mouthY).magnitude-.0031f,ink);
            Fill(ref face,Line(x,y,center,.708f,center,smileY)-.0027f,ink);
            return shape;
        }

        static float Crocodile(float x,float y,bool back,ref Color face)
        {
            const float center=.5f;
            float side=Mathf.Sign(x-center);
            float offset=Mathf.Abs(x-center);
            float shape=E(x,y,center,.747f,.254f,.137f);
            shape=Join(shape,RoundedRectangle(x,y,center,.694f,.204f,.081f,.054f),.018f);
            shape=Join(shape,RoundedRectangle(x,y,center,.641f,.177f,.049f,.040f),.016f);
            shape=Join(shape,E(offset,y,.162f,.841f,.089f,.073f),.018f);
            if(back||shape>.006f)return shape;

            const float eyeY=.846f;
            Ring(ref face,E(offset,y,.162f,eyeY,.048f,.040f),.0032f);
            Fill(ref face,E(offset,y,.162f,eyeY,.0105f,.030f),ink);
            Fill(ref face,E(x,y,center+side*.162f-.003f,eyeY+.012f,.004f,.004f),Color.white);
            Fill(ref face,E(offset,y,.087f,.744f,.022f,.0115f,-.24f),ink);

            float snoutX=Mathf.Clamp(x,.335f,.665f);
            float snoutY=SmileHeight(snoutX,.165f,.778f,-.062f);
            Fill(ref face,new Vector2(x-snoutX,y-snoutY).magnitude-.0026f,ink);
            float lipX=Mathf.Clamp(x,.309f,.691f);
            float lipY=SmileHeight(lipX,.191f,.656f,.022f);
            Fill(ref face,new Vector2(x-lipX,y-lipY).magnitude-.0045f,ink);
            for(int tooth=0;tooth<3;tooth++)
            {
                float toothX=center+side*(.085f+tooth*.044f);
                Tooth(ref face,x,y,toothX,SmileHeight(toothX,.191f,.656f,.022f),.012f-tooth*.0012f,.028f-tooth*.0032f);
            }
            return shape;
        }

        static float Hippo(float x,float y,bool back,ref Color face)
        {
            const float center=.5f;
            float offset=Mathf.Abs(x-center);
            float side=Mathf.Sign(x-center);
            float shape=E(x,y,center,.772f,.254f,.145f);
            shape=Join(shape,RoundedRectangle(x,y,center,.715f,.273f,.084f,.070f),.021f);
            shape=Join(shape,RoundedRectangle(x,y,center,.645f,.222f,.064f,.054f),.023f);
            shape=Join(shape,E(offset,y,.219f,.889f,.049f,.056f,.26f),.012f);
            if(back||shape>.006f)return shape;

            Ring(ref face,E(offset,y,.220f,.890f,.027f,.033f,.26f),.0026f);
            Eye(ref face,x,y,center+side*.150f,.799f,.020f);
            Fill(ref face,E(offset,y,.141f,.733f,.038f,.017f,-.20f),ink);
            float tooth=RoundedRectangle(offset,y,.049f,.647f,.026f,.024f,.008f);
            float lipX=Mathf.Clamp(x,.278f,.722f);
            float lipY=SmileHeight(lipX,.222f,.658f,.031f);
            if(tooth>-.002f)
                Fill(ref face,new Vector2(x-lipX,y-lipY).magnitude-.0050f,ink);
            Ring(ref face,tooth,.0028f);
            return shape;
        }

        public static void Sample(int kind,float x,float y,bool back,out float shape,out Color face)
        {
            face=Color.clear;
            if(((AnimalKind)kind is AnimalKind.Tiger or AnimalKind.Crocodile or AnimalKind.Hippo)&&
                (y<.54f||y>.98f||x<.12f||x>.88f))
            {shape=1;return;}
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
                case (int)AnimalKind.Tiger:
                    shape=Tiger(x,y,back,ref face);
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
                    float trunkT=1-Mathf.InverseLerp(.522f,.727f,trunkY);
                    float trunkX=.5f+.045f*trunkT*trunkT;
                    float trunkRadius=Mathf.Lerp(.033f,.052f,Mathf.InverseLerp(.522f,.727f,trunkY));
                    float trunk=new Vector2(x-trunkX,y-trunkY).magnitude-trunkRadius;
                    shape=Join(shape,trunk,.015f);
                    float outline=Mathf.Clamp01((.0022f-Mathf.Abs(trunk))*1200+.5f);
                    Layer(ref face,ink,outline*Mathf.Clamp01((.730f-y)/.029f));
                    Fill(ref face,E(x,y,.532f,.514f,.007f,.008f),ink);
                    Fill(ref face,E(x,y,.558f,.514f,.007f,.008f),ink);
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
                    shape=Crocodile(x,y,back,ref face);
                    break;
                case (int)AnimalKind.Hippo:
                    shape=Hippo(x,y,back,ref face);
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
