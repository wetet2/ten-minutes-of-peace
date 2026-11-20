using System;
using UnityEngine;

namespace PeacePark
{
    public static partial class RabbitMesh
    {
        static float Oval(Vector3 point,float x,float y,float z,float rx,float ry,float rz) =>
            Ellipsoid(point,new Vector3(x,y,z),new Vector3(rx,ry,rz));

        static float RoundedBlock(Vector3 point,Vector3 center,Vector3 halfSize,float radius)
        {
            var d=point-center;
            var q=new Vector3(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z))-halfSize+Vector3.one*radius;
            return Vector3.Max(q,Vector3.zero).magnitude+Mathf.Min(Mathf.Max(q.x,Mathf.Max(q.y,q.z)),0)-radius;
        }

        static float TaperedCapsule(Vector3 point,Vector3 from,Vector3 to,float baseRadius,float tipRadius)
        {
            var axis=to-from;
            var offset=point-from;
            float along=Mathf.Clamp01(Vector3.Dot(offset,axis)/axis.sqrMagnitude);
            return (offset-axis*along).magnitude-Mathf.Lerp(baseRadius,tipRadius,along);
        }

        static Vector3 CubicPoint(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)
        {
            float u=1-t;
            return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
        }

        static float CurvedTube(Vector3 point,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float rootRadius,float tipRadius)
        {
            const int segments=16;
            float distance=100f;
            var previous=a;
            for(int i=1;i<=segments;i++)
            {
                float t=i/(float)segments;
                var next=CubicPoint(a,b,c,d,t);
                distance=Mathf.Min(distance,TaperedCapsule(point,previous,next,
                    Mathf.Lerp(rootRadius,tipRadius,(i-1)/(float)segments),Mathf.Lerp(rootRadius,tipRadius,t)));
                previous=next;
            }
            return distance;
        }

        static float Leaf(Vector3 point,Vector3 center,Vector3 radius,float angle)
        {
            var offset=point-center;
            float sine=Mathf.Sin(angle),cosine=Mathf.Cos(angle);
            var local=new Vector3(offset.x*cosine+offset.y*sine,-offset.x*sine+offset.y*cosine,offset.z);
            return Ellipsoid(local,Vector3.zero,radius);
        }

        static float SpeciesHead(Vector3 p,AnimalKind kind)
        {
            float x=Mathf.Abs(p.x);
            var symmetric=new Vector3(x,p.y,p.z);
            switch(kind)
            {
                case AnimalKind.Bear:
                {
                    float head=Oval(p,0,3.02f,.01f,.96f,.78f,.88f);
                    return Blend(head,Oval(symmetric,.68f,3.66f,.07f,.29f,.30f,.24f),.11f);
                }
                case AnimalKind.Tiger:
                {
                    float crown=Oval(p,0,3.12f,.06f,.94f,.76f,.88f);
                    float cheek=Oval(symmetric,.43f,2.94f,-.20f,.54f,.42f,.57f);
                    float head=Blend(crown,cheek,.17f);
                    float whiskerPad=Oval(symmetric,.165f,2.833f,-.753f,.307f,.228f,.295f);
                    float chin=Oval(p,0,2.665f,-.43f,.53f,.235f,.53f);
                    head=Blend(Blend(head,whiskerPad,.17f),chin,.16f);
                    return Blend(head,Oval(symmetric,.652f,3.765f,.03f,.237f,.237f,.223f),.10f);
                }
                case AnimalKind.Frog:
                {
                    float head=Oval(p,0,2.86f,.01f,1.05f,.61f,.85f);
                    return Blend(head,Oval(symmetric,.53f,3.39f,-.20f,.35f,.38f,.38f),.18f);
                }
                case AnimalKind.Giraffe:
                {
                    float neck=Oval(p,0,3.02f,.08f,.31f,.92f,.35f);
                    float skull=Oval(p,0,4.03f,.01f,.66f,.63f,.68f);
                    float muzzle=Oval(p,0,3.80f,-.64f,.49f,.37f,.56f);
                    float head=Blend(Blend(neck,skull,.23f),muzzle,.17f);
                    float ear=Leaf(symmetric,new Vector3(.73f,4.24f,.02f),new Vector3(.42f,.18f,.16f),.39f);
                    float horn=Capsule(symmetric,new Vector3(.26f,4.43f,.13f),new Vector3(.30f,4.94f,.15f),.104f);
                    horn=Blend(horn,Oval(symmetric,.30f,4.97f,.15f,.16f,.148f,.15f),.06f);
                    return Blend(Blend(head,ear,.13f),horn,.09f);
                }
                case AnimalKind.Elephant:
                {
                    float head=Oval(p,0,3.08f,.08f,.94f,.83f,.89f);
                    float ear=Leaf(symmetric,new Vector3(1.05f,3.02f,.08f),new Vector3(.60f,.73f,.26f),-.26f);
                    head=Blend(head,ear,.20f);
                    // A hanging curve with a small forward curl, rather than a straight cone.
                    float trunk=CurvedTube(p,new Vector3(0,2.99f,-.76f),new Vector3(0,2.52f,-.92f),
                        new Vector3(.08f,1.72f,-.84f),new Vector3(.27f,1.95f,-1.38f),.30f,.135f);
                    return Blend(head,trunk,.21f);
                }
                case AnimalKind.Triceratops:
                {
                    float head=Oval(p,0,3.01f,-.09f,.89f,.73f,.83f);
                    float muzzle=Oval(p,0,2.78f,-.70f,.53f,.34f,.49f);
                    muzzle=Blend(muzzle,Oval(p,0,2.76f,-1.14f,.33f,.235f,.36f),.17f);
                    head=Blend(head,muzzle,.19f);
                    float frill=Oval(p,0,3.49f,.38f,1.26f,.86f,.34f);
                    // The lower edge fans around the cheeks while staying behind the face.
                    frill=Blend(frill,Leaf(symmetric,new Vector3(1.00f,3.18f,.36f),
                        new Vector3(.40f,.49f,.28f),-.25f),.20f);
                    frill=Blend(frill,Oval(symmetric,1.13f,3.76f,.36f,.29f,.27f,.24f),.15f);
                    frill=Blend(frill,Oval(symmetric,.68f,4.12f,.34f,.28f,.24f,.22f),.14f);
                    frill=Blend(frill,Oval(p,0,4.26f,.32f,.26f,.19f,.22f),.12f);
                    head=Blend(head,frill,.17f);
                    float browHorn=TaperedCapsule(symmetric,
                        new Vector3(.44f,3.50f,-.56f),new Vector3(.58f,4.00f,-.93f),.208f,.081f);
                    float noseHorn=TaperedCapsule(p,
                        new Vector3(0,2.99f,-1.15f),new Vector3(0,3.29f,-1.43f),.155f,.065f);
                    return Blend(Blend(head,browHorn,.12f),noseHorn,.10f);
                }
                case AnimalKind.Crocodile:
                {
                    float skull=Oval(p,0,3.02f,.12f,.80f,.63f,.73f);
                    float brow=Oval(symmetric,.485f,3.485f,-.25f,.305f,.285f,.396f);
                    float bridge=Oval(p,0,2.91f,-.59f,.648f,.294f,.585f);
                    float snout=RoundedBlock(p,new Vector3(0,2.871f,-1.13f),new Vector3(.57f,.195f,.61f),.17f);
                    float nose=Oval(symmetric,.258f,3.025f,-1.476f,.208f,.116f,.202f);
                    float upper=Blend(Blend(bridge,snout,.13f),nose,.09f);
                    float jaw=RoundedBlock(p,new Vector3(0,2.613f,-1.055f),new Vector3(.533f,.147f,.635f),.122f);
                    jaw=Blend(jaw,Oval(p,0,2.67f,-.46f,.622f,.225f,.475f),.12f);
                    return Blend(Blend(skull,brow,.11f),Blend(upper,jaw,.105f),.13f);
                }
                case AnimalKind.Hippo:
                {
                    float skull=Oval(p,0,3.105f,.065f,.83f,.70f,.805f);
                    float cheek=Oval(symmetric,.595f,2.854f,-.165f,.327f,.355f,.57f);
                    float muzzle=RoundedBlock(p,new Vector3(0,2.835f,-.754f),new Vector3(.851f,.302f,.608f),.268f);
                    float nostril=Oval(symmetric,.425f,3.036f,-1.029f,.273f,.151f,.246f);
                    float jaw=RoundedBlock(p,new Vector3(0,2.565f,-.729f),new Vector3(.689f,.216f,.536f),.187f);
                    float ears=Leaf(symmetric,new Vector3(.663f,3.721f,.029f),new Vector3(.182f,.219f,.154f),-.31f);
                    return Blend(Blend(Blend(skull,cheek,.13f),Blend(Blend(muzzle,nostril,.11f),jaw,.095f),.18f),ears,.083f);
                }
                case AnimalKind.Gorilla:
                {
                    float crown=Oval(p,0,3.13f,.09f,1.00f,.82f,.86f);
                    float jaw=Oval(p,0,2.80f,-.46f,.76f,.50f,.58f);
                    float ears=Oval(symmetric,.91f,3.10f,.06f,.23f,.24f,.18f);
                    return Blend(Blend(crown,jaw,.17f),ears,.10f);
                }
                default:return 100f;
            }
        }

        static float SpeciesTail(Vector3 p,AnimalKind kind)
        {
            switch(kind)
            {
                case AnimalKind.Rabbit:return Oval(p,0,1.31f,.47f,.28f,.28f,.28f);
                case AnimalKind.Bear:return Oval(p,0,1.34f,.49f,.22f,.19f,.20f);
                case AnimalKind.Tiger:
                    return TaperedCapsule(p,new Vector3(.03f,1.40f,.40f),new Vector3(.82f,.83f,1.08f),.137f,.100f);
                case AnimalKind.Triceratops:
                    return TaperedCapsule(p,new Vector3(0,1.40f,.36f),new Vector3(.66f,.89f,1.19f),.23f,.10f);
                case AnimalKind.Crocodile:
                    return CrocodileTail(p);
                case AnimalKind.Hippo:
                    return TaperedCapsule(p,new Vector3(.01f,1.40f,.453f),new Vector3(.093f,1.043f,.738f),.090f,.057f);
                case AnimalKind.Elephant:
                {
                    var end=new Vector3(.17f,.72f,.84f);
                    float tail=CurvedTube(p,new Vector3(0,1.45f,.39f),new Vector3(0,1.28f,.80f),
                        new Vector3(.11f,.98f,.85f),end,.078f,.048f);
                    return Blend(tail,Ellipsoid(p,end,new Vector3(.080f,.12f,.075f)),.045f);
                }
                case AnimalKind.Giraffe:
                {
                    var end=new Vector3(.12f,.60f,.77f);
                    float tail=CurvedTube(p,new Vector3(0,1.49f,.38f),new Vector3(.01f,1.43f,.83f),
                        new Vector3(.06f,.96f,.70f),end,.084f,.054f);
                    var tuft=new Vector3(.14f,.52f,.78f);
                    return Blend(tail,Ellipsoid(p,tuft,new Vector3(.083f,.155f,.077f)),.043f);
                }
                default:return 100f; // Adult frogs have no tail.
            }
        }

        static float CrocodileTail(Vector3 p)
        {
            if(p.z<.18f||p.y>1.82f||p.y<.48f)return 100f;
            var root=new Vector3(.025f,1.46f,.355f);
            var shoulder=new Vector3(.02f,1.30f,.87f);
            var curve=new Vector3(.39f,.80f,1.28f);
            var tip=new Vector3(.715f,1.022f,1.76f);
            float tail=CurvedTube(p,root,shoulder,curve,tip,.235f,.051f);
            for(int i=0;i<5;i++)
            {
                float t=.20f+i*.145f;
                var ridge=CubicPoint(root,shoulder,curve,tip,t);
                float width=Mathf.Lerp(.122f,.058f,t);
                tail=Blend(tail,Oval(p,ridge.x,ridge.y+Mathf.Lerp(.19f,.062f,t),ridge.z,
                    width,Mathf.Lerp(.152f,.078f,t),.105f),.050f);
            }
            return tail;
        }

        static float CrocodileBack(Vector3 p)
        {
            if(p.z<.27f||p.y<1.46f||p.y>2.49f||Mathf.Abs(p.x)>.50f)return 100f;
            var side=new Vector3(Mathf.Abs(p.x),p.y,p.z);
            float plates=Oval(side,.19f,2.24f,.359f,.139f,.178f,.165f);
            plates=Blend(plates,Oval(side,.214f,1.998f,.471f,.152f,.174f,.181f),.062f);
            return Blend(plates,Oval(side,.224f,1.747f,.505f,.150f,.163f,.166f),.062f);
        }

        static Vector2 SpeciesUv(AnimalKind kind,Vector3 point,Vector3 drawingPoint)
        {
            float y=drawingPoint.y/4.5f;
            if(point.y>2.15f)
            {
                // The shared chest keeps its coordinates. Each head fits its own canvas contour.
                float faceTop=kind==AnimalKind.Giraffe?5.12f:kind==AnimalKind.Triceratops?4.46f:kind==AnimalKind.Frog||kind==AnimalKind.Crocodile?3.77f:3.94f;
                float topUv=kind==AnimalKind.Giraffe?.965f:kind==AnimalKind.Frog?.88f:kind==AnimalKind.Crocodile?.907f:.92f;
                y=Mathf.Lerp(2.15f/4.5f,topUv,Mathf.InverseLerp(2.15f,faceTop,point.y));
            }
            float halfWidth=kind==AnimalKind.Elephant?1.72f:kind==AnimalKind.Triceratops?1.68f:
                kind==AnimalKind.Tiger?1.57f:1.3f;
            float headBlend=Smooth(2.23f,2.63f,point.y);
            return new Vector2(.5f+drawingPoint.x/(2*Mathf.Lerp(1.3f,halfWidth,headBlend)),y);
        }

        static float FrontSurface(AnimalKind kind,float x,float y)
        {
            const float step=.035f;
            float start=kind==AnimalKind.Crocodile?-1.94f:-1.72f;
            for(float z=start;z<.25f;z+=step)
            {
                if(Field(new Vector3(x,y,z),kind)>0)continue;
                float outside=z-step,inside=z;
                for(int i=0;i<8;i++)
                {
                    float middle=(outside+inside)*.5f;
                    if(Field(new Vector3(x,y,middle),kind)>0)outside=middle;else inside=middle;
                }
                return (outside+inside)*.5f;
            }
            return 0;
        }

        static float CrocodileMouthHeight(float z)=>2.680f+.103f*Smooth(-1.59f,-.65f,z);

        static float CrocodileSide(float y,float z)
        {
            const float step=.035f;
            for(float x=.85f;x>=0;x-=step)
            {
                if(Field(new Vector3(x,y,z),AnimalKind.Crocodile)>0)continue;
                float inside=x,outside=x+step;
                for(int i=0;i<7;i++)
                {
                    float middle=(inside+outside)*.5f;
                    if(Field(new Vector3(middle,y,z),AnimalKind.Crocodile)>0)outside=middle;else inside=middle;
                }
                return (inside+outside)*.5f;
            }
            return 0;
        }

        static void AddSpeciesDetails(AnimalKind kind,Action<Vector3,Vector3,int,float,int> add,
            Action<Vector3,Vector3> addIncisor)
        {
            void Feature(float x,float y,Vector3 size,int material,float offset=0,float roll=0)
            {
                float z=FrontSurface(kind,x,y)+offset;
                add(new Vector3(x,y,z),size,material,roll,1);
            }
            void Eye(float x,float y,bool frog)
            {
                float z=FrontSurface(kind,x,y);
                if(frog)
                {
                    add(new Vector3(x,y,z+.045f),new Vector3(.238f,.242f,.15f),3,0,1);
                    z-=.094f;
                }
                var size=frog?new Vector3(.139f,.165f,.061f):new Vector3(.084f,.112f,.061f);
                add(new Vector3(x,y,z),size,1,0,1);
                add(new Vector3(x-.027f,y+.037f,z-size.z*.87f),Vector3.one*(frog?.032f:.024f),3,0,1);
            }
            void Smile(float centerY,float width,float height)
            {
                for(int i=0;i<=27;i++)
                {
                    float t=i/27f*2-1;
                    Feature(width*t,centerY+height*t*t,new Vector3(.032f,.021f,.017f),1,-.004f);
                }
            }
            switch(kind)
            {
                case AnimalKind.Bear:
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.355f,3.16f,false);
                        Feature(side*.68f,3.73f,new Vector3(.17f,.17f,.048f),2,.018f);
                    }
                    Feature(0,2.77f,new Vector3(.39f,.24f,.14f),3,.014f);
                    Feature(0,2.87f,new Vector3(.128f,.077f,.068f),1,-.119f);
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<=7;i++)
                    {
                        float t=i/7f;
                        float z=FrontSurface(kind,0,2.77f)-.127f;
                        add(new Vector3(side*t*.135f,2.745f-Mathf.Sin(t*Mathf.PI)*.046f,z),Vector3.one*.023f,1,0,1);
                    }
                    break;
                }
                case AnimalKind.Tiger:
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        const float eyeY=3.19f;
                        float eyeX=side*.368f;
                        float eyeZ=FrontSurface(kind,eyeX,eyeY);
                        add(new Vector3(eyeX,eyeY,eyeZ),new Vector3(.103f,.117f,.064f),1,0,1);
                        var iris=new Vector3(eyeX,eyeY,eyeZ-.052f);
                        add(iris,new Vector3(.076f,.083f,.021f),4,0,1);
                        add(iris+Vector3.back*.018f,new Vector3(.057f,.065f,.015f),1,0,1);
                        add(iris+new Vector3(-.022f,.031f,-.031f),Vector3.one*.020f,3,0,1);
                        Feature(side*.657f,3.810f,new Vector3(.132f,.136f,.040f),2,.012f);
                        for(int dot=0;dot<3;dot++)
                        {
                            float x=side*(.200f+dot*.063f);
                            float y=2.845f+dot%2*.032f;
                            Feature(x,y,new Vector3(.015f,.015f,.011f),1,-.004f);
                        }
                    }
                    Feature(0,2.946f,new Vector3(.102f,.058f,.044f),1,-.019f);
                    break;
                }
                case AnimalKind.Frog:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.53f,3.49f,true);
                        Feature(side*.70f,2.88f,new Vector3(.153f,.075f,.041f),2,.014f);
                        Feature(side*.115f,2.96f,new Vector3(.024f,.024f,.015f),1,.003f);
                    }
                    Smile(2.67f,.45f,.13f);
                    break;
                case AnimalKind.Giraffe:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.31f,4.15f,false);
                        Feature(side*.83f,4.29f,new Vector3(.245f,.105f,.034f),2,.020f,side*22);
                        Feature(side*.20f,3.91f,new Vector3(.055f,.035f,.025f),1,.005f,side*12);
                    }
                    Smile(3.69f,.27f,.057f);
                    break;
                case AnimalKind.Elephant:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.425f,3.24f,false);
                        Feature(side*1.14f,3.03f,new Vector3(.44f,.54f,.048f),2,.025f,side*-15);
                        Feature(side*.65f,2.94f,new Vector3(.13f,.068f,.036f),2,.015f);
                        Feature(.27f+side*.045f,1.95f,new Vector3(.024f,.027f,.014f),1,.006f);
                    }
                    break;
                case AnimalKind.Triceratops:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.37f,3.21f,false);
                        Feature(side*.60f,2.91f,new Vector3(.127f,.072f,.035f),2,.012f);
                        Feature(side*.167f,2.85f,new Vector3(.030f,.024f,.018f),1,.005f,side*13);
                    }
                    Smile(2.63f,.265f,.075f);
                    break;
                case AnimalKind.Crocodile:
                    for(int side=-1;side<=1;side+=2)
                    {
                        float x=side*.488f;
                        const float y=3.487f;
                        float z=FrontSurface(kind,x,y);
                        add(new Vector3(x,y,z+.012f),new Vector3(.146f,.137f,.069f),1,0,1);
                        var iris=new Vector3(x-side*.019f,y+.003f,z-.047f);
                        add(iris,new Vector3(.093f,.104f,.040f),4,0,1);
                        add(iris+Vector3.back*.035f,new Vector3(.022f,.081f,.023f),1,0,1);
                        add(iris+new Vector3(-.026f,.044f,-.046f),Vector3.one*.017f,3,0,1);
                        Feature(side*.258f,3.040f,new Vector3(.080f,.046f,.024f),1,-.003f,side*16);
                        for(int tooth=0;tooth<4;tooth++)
                        {
                            float toothZ=-1.56f+tooth*.205f;
                            float toothY=CrocodileMouthHeight(toothZ)-.028f;
                            float toothX=CrocodileSide(toothY,toothZ);
                            add(new Vector3(side*(toothX-.008f),toothY,toothZ),new Vector3(.039f,.062f-tooth*.004f,.038f),3,side*8,1);
                        }
                    }
                    break;
                case AnimalKind.Hippo:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.454f,3.351f,false);
                        Feature(side*.685f,3.751f,new Vector3(.088f,.118f,.039f),2,.009f,-side*14);
                        Feature(side*.424f,3.036f,new Vector3(.122f,.061f,.027f),1,-.005f,side*15);
                        float toothX=side*.148f;
                        const float toothY=2.606f;
                        addIncisor(new Vector3(toothX,toothY,FrontSurface(kind,toothX,toothY)-.018f),
                            new Vector3(.102f,.101f,.048f));
                    }
                    break;
                case AnimalKind.Gorilla:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Eye(side*.30f,3.20f,false);
                        Feature(side*.13f,2.96f,new Vector3(.051f,.036f,.028f),1,.008f);
                    }
                    Smile(2.72f,.23f,.06f);
                    break;
            }
        }
    }
}
