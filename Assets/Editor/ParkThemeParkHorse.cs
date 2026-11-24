using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkThemeParkSetup
    {
        Transform[] AddCarouselHorses(Transform platform,float deckHeight,float eaveHeight,out Transform[] lifts)
        {
            var body=HorseLoft("Rounded toy horse body",
                Profile(0,1.15f,-.87f,0,0),
                Profile(0,1.13f,-.74f,.30f,.30f),
                Profile(0,1.10f,-.51f,.39f,.405f),
                Profile(0,1.06f,-.12f,.359f,.385f),
                Profile(0,1.11f,.265f,.371f,.404f),
                Profile(0,1.16f,.51f,.302f,.345f),
                Profile(0,1.21f,.73f,0,0));
            var neck=HorseLoft("Rounded toy horse neck",
                Profile(0,1.04f,.41f,0,0),
                Profile(0,1.25f,.49f,.263f,.31f),
                Profile(0,1.52f,.60f,.231f,.293f),
                Profile(0,1.75f,.70f,.192f,.224f),
                Profile(0,1.96f,.855f,.146f,.16f),
                Profile(0,2.049f,.92f,0,0));
            var head=HorseLoft("Wide toy horse cheek and muzzle",
                Profile(0,1.958f,.78f,0,0),
                Profile(0,1.973f,.91f,.193f,.209f),
                Profile(0,1.907f,1.042f,.221f,.225f),
                Profile(0,1.80f,1.159f,.169f,.165f),
                Profile(0,1.686f,1.282f,.174f,.134f),
                Profile(0,1.657f,1.39f,.125f,.092f),
                Profile(0,1.658f,1.448f,0,0));
            var mane=HorseLoft("Molded carousel horse mane",
                Profile(.04f,1.415f,.18f,0,0),
                Profile(.106f,1.51f,.265f,.094f,.081f),
                Profile(.082f,1.66f,.364f,.163f,.119f),
                Profile(.066f,1.835f,.492f,.15f,.122f),
                Profile(.038f,1.988f,.642f,.124f,.102f),
                Profile(.008f,2.067f,.832f,.085f,.075f),
                Profile(0,2.105f,.982f,0,0));
            var tail=HorseLoft("Curled toy horse tail",
                Profile(0,1.28f,-.744f,0,0),
                Profile(0,1.23f,-.899f,.137f,.10f),
                Profile(.025f,1.06f,-1.034f,.154f,.113f),
                Profile(.074f,.831f,-1.132f,.159f,.104f),
                Profile(.126f,.695f,-1.246f,.126f,.096f),
                Profile(.173f,.695f,-1.36f,.061f,.05f),
                Profile(.176f,.741f,-1.417f,0,0));
            var forelock=HorseLoft("Sculpted toy horse forelock",
                Profile(-.044f,2.167f,.892f,0,0),
                Profile(.018f,2.172f,.971f,.133f,.057f),
                Profile(.045f,2.092f,1.054f,.076f,.052f),
                Profile(.099f,2.014f,1.104f,0,0));
            var blaze=HorseBlaze(head);

            Material Enamel(string name,string color)
            {
                var paint=Paint(name,color);
                paint.SetColor("_Shade",new Color(.81f,.83f,.92f));
                paint.SetFloat("_ShadowFloor",.84f);
                return paint;
            }
            var coats=new[]{Enamel("Rose carousel horse","#F2A6AF"),Enamel("Vanilla carousel horse","#FFF0CD"),
                Enamel("Mint carousel horse","#A1DBC9"),Enamel("Lilac carousel horse","#C6B7EF")};
            var hair=new[]{Enamel("Plum enamel","#AE6D9E"),Enamel("Apricot enamel","#DF9168"),
                Enamel("Teal enamel","#5EA8A7"),Enamel("Periwinkle enamel","#7A91D5")};
            var muzzles=new[]{Enamel("Rosy muzzle","#FFD1CD"),Enamel("Biscuit muzzle","#E9C198"),
                Enamel("Cream mint muzzle","#D4EEE0"),Enamel("Lilac muzzle","#EEE0FA")};
            var blush=Enamel("Painted carousel cheeks","#EC91AB");
            var blanket=new[]{Enamel("Honey saddle blanket","#F6CC79"),Enamel("Blue saddle blanket","#8CC5E9"),
                Enamel("Rose saddle blanket","#EF9BAE"),Enamel("Mint saddle blanket","#90D9BC")};
            var trim=Enamel("Warm brass saddle trim","#FCCB73");
            var legs=new Mesh[4];
            var hooves=new Mesh[4];
            for(int leg=0;leg<legs.Length;leg++)
            {
                bool front=leg<2;
                float side=leg%2==0?1:-1;
                var hip=new Vector3(side*.22f,front?1.24f:1.28f,front?.44f:-.59f);
                var knee=new Vector3(side*.27f,front?.88f:.87f,front?.57f:-.445f);
                var hock=front
                    ? new Vector3(side*.245f,side>0?.67f:.53f,side>0?.84f:.49f)
                    : new Vector3(side*.274f,side>0?.51f:.59f,side>0?-.78f:-.887f);
                var ankle=front
                    ? new Vector3(side*.25f,side>0?.49f:.26f,side>0?.725f:.60f)
                    : new Vector3(side*.255f,side>0?.21f:.337f,side>0?-.816f:-.935f);
                var coronet=ankle+new Vector3(0,-.063f,.033f);
                legs[leg]=HorseLoft("Jointed horse leg",
                    Profile(hip+Vector3.up*.09f,0,0),
                    Profile(hip,front?.118f:.15f,front?.157f:.192f),
                    Profile(Vector3.Lerp(hip,knee,.55f),front?.080f:.117f,front?.106f:.139f),
                    Profile(knee,.077f,.089f),
                    Profile(hock,.061f,.072f),
                    Profile(Vector3.Lerp(hock,ankle,.68f),.056f,.066f),
                    Profile(ankle,.058f,.065f),
                    Profile(coronet,.066f,.072f));
                hooves[leg]=HorseLoft("Rounded toy horse hoof",
                    Profile(coronet+new Vector3(0,.037f,-.024f),0,0),
                    Profile(coronet,.067f,.075f),
                    Profile(coronet+new Vector3(0,-.062f,.034f),.083f,.094f),
                    Profile(coronet+new Vector3(0,-.085f,.037f),.065f,.080f),
                    Profile(coronet+new Vector3(0,-.091f,.037f),0,0));
            }

            var ears=new Mesh[2];
            for(int s=0;s<ears.Length;s++)
            {
                float side=s==0?1:-1;
                ears[s]=HorseLoft("Rounded toy horse ear",
                    Profile(side*.102f,2.04f,.934f,.061f,.044f),
                    Profile(side*.139f,2.145f,.938f,.066f,.049f),
                    Profile(side*.156f,2.241f,.958f,.041f,.034f),
                    Profile(side*.158f,2.28f,.986f,0,0));
            }
            const float horseScale=.5f;
            const float poleForward=.56f;
            var scaling=new Vector3(.78f,1.10f,.78f)*horseScale;
            const int horseCount=7;
            var seats=new Transform[horseCount];
            lifts=new Transform[horseCount];
            for(int i=0;i<horseCount;i++)
            {
                int scheme=i%coats.Length;
                var turn=Quaternion.Euler(0,(i+.3f)*360f/horseCount,0);
                float mountHeight=deckHeight+(.08f+.15f*(i%2))*horseScale;
                var origin=turn*Vector3.right*2.70f+Vector3.up*mountHeight;
                var mount=Group("Carousel horse "+(i+1),platform,origin);
                mount.localRotation=turn;
                var lift=lifts[i]=Group("Rising horse",mount,Vector3.zero);
                var horse=new Pieces();
                Vector3 Point(Vector3 offset)=>Vector3.Scale(offset,scaling);
                var seat=seats[i]=Group("Saddle "+(i+1),lift,Point(new Vector3(0,1.57f,-.135f)));
                seat.localRotation=Quaternion.Euler(0,180,0);
                void Sculpt(Mesh mesh,Material material) => horse.Add(mesh,material,Vector3.zero,scaling);
                void Detail(Mesh mesh,Material material,Vector3 at,Vector3 size) =>
                    horse.Add(mesh,material,Point(at),Vector3.Scale(size,scaling));
                void Band(Material material,Vector3 a,Vector3 b,float width) =>
                    horse.Beam(cylinder,material,Point(a),Point(b),width*horseScale);

                var post=Point(Vector3.forward*poleForward);
                var shaft=Group("Support pole",mount,post);
                var poleParts=new Pieces();
                poleParts.Beam(cylinder,yellow,Vector3.up*(deckHeight-mountHeight),
                    Vector3.up*(eaveHeight-.11f-mountHeight),.062f);
                poleParts.Build(shaft);
                var collar=Group("Sliding pole collar",lift,Point(new Vector3(0,1.91f,poleForward)));
                var collarParts=new Pieces();
                collarParts.Add(cylinder,trim,Vector3.zero,new Vector3(.107f,.072f,.107f));
                collarParts.Build(collar);
                var handle=new Vector3(0,1.84f,.255f);
                Group("Saddle handle",seat,Quaternion.Inverse(seat.localRotation)*(Point(handle)-seat.localPosition));
                var bar=Vector3.right*.16f;
                Band(trim,handle-bar,handle+bar,.080f);
                foreach(float side in new[]{-1f,1f})
                    Band(trim,new Vector3(side*.16f,1.38f,.30f),handle+bar*side,.080f);
                Sculpt(body,coats[scheme]);
                Sculpt(neck,coats[scheme]);
                Sculpt(head,coats[scheme]);
                Sculpt(mane,hair[scheme]);
                Sculpt(tail,hair[scheme]);
                Sculpt(forelock,hair[scheme]);
                if(scheme!=1)Sculpt(blaze,cream);
                foreach(var ear in ears)Sculpt(ear,coats[scheme]);
                for(int leg=0;leg<legs.Length;leg++)
                {
                    Sculpt(legs[leg],coats[scheme]);
                    Sculpt(hooves[leg],hair[scheme]);
                }

                Detail(sphere,muzzles[scheme],new Vector3(0,1.655f,1.367f),new Vector3(.322f,.19f,.203f));
                Detail(sphere,trim,new Vector3(0,1.312f,-.103f),new Vector3(.786f,.375f,.728f));
                Detail(sphere,blanket[scheme],new Vector3(0,1.331f,-.103f),new Vector3(.798f,.347f,.668f));
                Detail(sphere,hair[scheme],new Vector3(0,1.498f,-.135f),new Vector3(.566f,.187f,.57f));
                Detail(sphere,hair[scheme],new Vector3(0,1.566f,-.316f),new Vector3(.479f,.139f,.188f));
                Detail(sphere,hair[scheme],new Vector3(0,1.559f,.071f),new Vector3(.347f,.129f,.167f));
                foreach(float side in new[]{-1,1})
                {
                    Detail(flowerSphere,muzzles[scheme],new Vector3(side*.157f,2.174f,.978f),new Vector3(.080f,.128f,.043f));
                    Detail(sphere,ink,new Vector3(side*.202f,1.943f,1.094f),new Vector3(.081f,.115f,.111f));
                    Detail(flowerSphere,cream,new Vector3(side*.236f,1.971f,1.113f),Vector3.one*.037f);
                    Detail(flowerSphere,blush,new Vector3(side*.171f,1.813f,1.152f),new Vector3(.051f,.081f,.124f));
                    Detail(flowerSphere,hair[scheme],new Vector3(side*.091f,1.678f,1.449f),new Vector3(.032f,.034f,.034f));
                    var stud=new Vector3(side*.146f,1.695f,1.310f);
                    var bridge=new Vector3(side*.091f,1.797f,1.376f);
                    Band(blanket[scheme],stud,bridge,.047f);
                    Band(blanket[scheme],bridge,new Vector3(0,1.824f,1.387f),.047f);
                    Detail(flowerSphere,trim,stud,new Vector3(.050f,.088f,.088f));
                    var rosette=new Vector3(side*.400f,1.316f,-.103f);
                    for(int petal=0;petal<5;petal++)
                    {
                        float angle=petal*Mathf.PI*2/5;
                        Detail(flowerSphere,cream,rosette+new Vector3(0,Mathf.Cos(angle)*.045f,Mathf.Sin(angle)*.045f),
                            new Vector3(.032f,.052f,.052f));
                    }
                    Detail(flowerSphere,trim,rosette+Vector3.right*(side*.015f),Vector3.one*.057f);
                    var stirrup=new Vector3(side*.355f,1.045f,-.088f);
                    Band(hair[scheme],new Vector3(side*.343f,1.44f,-.117f),stirrup,.047f);
                    Band(trim,stirrup,stirrup+new Vector3(0,-.091f,-.055f),.032f);
                    Band(trim,stirrup,stirrup+new Vector3(0,-.091f,.055f),.032f);
                    Band(trim,stirrup+new Vector3(0,-.091f,-.055f),stirrup+new Vector3(0,-.091f,.055f),.032f);
                }
                horse.Build(lift);
            }
            return seats;
        }

        readonly struct HorseSection
        {
            public readonly Vector3 Center;
            public readonly Vector3 Radii;
            public HorseSection(Vector3 center,float width,float depth)
            {
                Center=center;
                Radii=new Vector3(width,depth,0);
            }
        }

        static HorseSection Profile(float x,float y,float z,float width,float depth) =>
            Profile(new Vector3(x,y,z),width,depth);
        static HorseSection Profile(Vector3 center,float width,float depth) => new HorseSection(center,width,depth);

        const int HorseLoftSides=20, HorseLoftSubdivisions=4;

        static Mesh HorseBlaze(Mesh head)
        {
            const int start=HorseLoftSubdivisions+1, end=HorseLoftSubdivisions*5+1;
            const int rows=end-start+1;
            var surface=head.vertices;
            var normals=head.normals;
            var vertices=new Vector3[rows*3];
            var triangles=new int[(rows-1)*12];
            for(int row=0;row<rows;row++)
            {
                int crown=(start+row)*HorseLoftSides+HorseLoftSides/4;
                float width=.65f*Mathf.Sin(Mathf.PI*(row+.3f)/rows);
                for(int column=0;column<3;column++)
                {
                    int edge=crown+(column-1)*2;
                    vertices[row*3+column]=Vector3.Lerp(surface[crown],surface[edge],width)+normals[crown]*.006f;
                    if(row==rows-1||column==2)continue;
                    int vertex=row*3+column, index=row*12+column*6;
                    triangles[index]=vertex;
                    triangles[index+1]=vertex+1;
                    triangles[index+2]=vertex+3;
                    triangles[index+3]=vertex+1;
                    triangles[index+4]=vertex+4;
                    triangles[index+5]=vertex+3;
                }
            }
            var mesh=new Mesh{name="Painted blaze on horse forehead",vertices=vertices,triangles=triangles};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh HorseLoft(string name,params HorseSection[] sections)
        {
            const int subdivisions=HorseLoftSubdivisions,sides=HorseLoftSides;
            int count=(sections.Length-1)*subdivisions+1;
            var centers=new Vector3[count];
            var radii=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                int segment=Mathf.Min(i/subdivisions,sections.Length-2);
                float t=(float)i/subdivisions-segment;
                var a=sections[Mathf.Max(segment-1,0)];
                var b=sections[segment];
                var c=sections[segment+1];
                var d=sections[Mathf.Min(segment+2,sections.Length-1)];
                centers[i]=HorseSpline(a.Center,b.Center,c.Center,d.Center,t);
                radii[i]=HorseSpline(a.Radii,b.Radii,c.Radii,d.Radii,t);
            }
            var vertices=new Vector3[count*sides];
            var indices=new int[(count-1)*sides*6];
            for(int i=0;i<count;i++)
            {
                var tangent=(centers[Mathf.Min(i+1,count-1)]-centers[Mathf.Max(i-1,0)]).normalized;
                var up=Vector3.Cross(tangent,Vector3.right).normalized;
                var right=Vector3.Cross(up,tangent);
                for(int side=0;side<sides;side++)
                {
                    float angle=2*Mathf.PI*side/sides;
                    int index=i*sides+side;
                    vertices[index]=centers[i]+right*(Mathf.Cos(angle)*Mathf.Max(.001f,radii[i].x))+
                        up*(Mathf.Sin(angle)*Mathf.Max(.001f,radii[i].y));
                    if(i==count-1)continue;
                    int next=i*sides+(side+1)%sides,k=index*6;
                    indices[k]=index;
                    indices[k+1]=next;
                    indices[k+2]=index+sides;
                    indices[k+3]=next;
                    indices[k+4]=next+sides;
                    indices[k+5]=index+sides;
                }
            }
            var mesh=new Mesh{name=name,vertices=vertices,triangles=indices};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 HorseSpline(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)
        {
            float squared=t*t;
            return .5f*(2*b+(c-a)*t+(2*a-5*b+4*c-d)*squared+(-a+3*b-3*c+d)*(squared*t));
        }
    }
}
