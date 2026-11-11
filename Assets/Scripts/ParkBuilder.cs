using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark
{
    // Geometry is built in the editor and persisted as ordinary editable scene objects.
    public static class ParkBuilder
    {
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        public static IEnumerable<Material> Materials { get { return palette.Values; } }
        static Material grass, leaf, wood, cream, pink, purple, yellow, white, chocolate, blue, peach;
        static Mesh canopyMesh, trunkMesh, bevelMesh;

        static Material Mat(string name, string hex)
        {
            Color c; ColorUtility.TryParseHtmlString(hex, out c);
            var m = new Material(Shader.Find("Standard")) { name = name, color = c };
            m.SetFloat("_Glossiness", .12f);
            palette.Add(name, m); return m;
        }
        static Transform Group(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }
        static Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 p, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }
        static Transform Box(string name, Transform parent, Vector3 p, Vector3 scale, Material material)
        { return ParkGeometry.Place(name,bevelMesh,parent,p,scale,material); }
        static Transform Ball(string name, Transform parent, Vector3 p, Vector3 scale, Material material)
        { return Shape(name, PrimitiveType.Sphere, parent, p, scale, material); }
        static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float width, Material m)
        {
            var t = Shape(name, PrimitiveType.Cylinder, parent, (a+b)/2, new Vector3(width, Vector3.Distance(a,b)/2, width), m);
            t.localRotation = Quaternion.FromToRotation(Vector3.up, b-a);
        }
        public static GameObject Build()
        {
            palette.Clear(); ParkGeometry.Meshes.Clear();
            canopyMesh=ParkSurface.Crown(); trunkMesh=ParkGeometry.Trunk(); bevelMesh=ParkSurface.RoundedBox(); ParkSurface.Textures.Clear();
            grass=Mat("Meadow", "#A9CD87"); leaf=Mat("Leaves", "#80AF67"); wood=Mat("Honey wood", "#C28B58");
            cream=Mat("Warm cream", "#F3DDB0"); pink=Mat("Petal pink", "#EDABC2"); purple=Mat("Lavender", "#B3A0D1");
            yellow=Mat("Butter yellow", "#F4D575"); white=Mat("Icing", "#FFF1DB"); chocolate=Mat("Cocoa", "#805849"); blue=Mat("Window blue", "#91C5CD");
            peach=Mat("Peach plaster", "#EAC398");
            var softTexture=ParkSurface.Painted("Soft painted grain",false);
            var timberTexture=ParkSurface.Painted("Warm timber grain",true);
            foreach(var material in palette.Values) material.mainTexture=softTexture;
            wood.mainTexture=timberTexture;chocolate.mainTexture=timberTexture;
            grass.mainTextureScale=new Vector2(7,7);
            var root = new GameObject("Peace Park"); Transform r = root.transform;
            Box("Meadow island", r, new Vector3(0,-.45f,0), new Vector3(30,.85f,25), grass);
            Box("Earth edge", r, new Vector3(0,-1,0), new Vector3(29.8f,.5f,24.8f), wood);
            ParkGeometry.Place("Winding path",ParkGeometry.Path(),r,Vector3.zero,Vector3.one,cream);
            Shape("Entry path",PrimitiveType.Cube,r,new Vector3(0,.024f,-10),new Vector3(2.2f,.015f,4),cream);
            House(r,new Vector3(-7,0,5),false);
            House(r,new Vector3(7,0,5),true);
            for (int i=0;i<6;i++)
            {
                float x=-12.5f+i*5;
                Tree(r,new Vector3(x,0,10.4f), .85f+(i%3)*.12f);
            }
            for(int i=0;i<4;i++) { Tree(r,new Vector3(-13,0,5-i*4),.9f); Tree(r,new Vector3(13,0,5-i*4),1.05f); }
            Fence(r,new Vector3(-14,0,-11.7f),new Vector3(-2,0,-11.7f));
            Fence(r,new Vector3(2,0,-11.7f),new Vector3(14,0,-11.7f));
            Fence(r,new Vector3(-14,0,11.7f),new Vector3(14,0,11.7f));
            Fence(r,new Vector3(-14,0,-11.7f),new Vector3(-14,0,11.7f));
            Fence(r,new Vector3(14,0,-11.7f),new Vector3(14,0,11.7f));
            Flower(r,new Vector3(-4.6f,.9f,-8.8f),.48f,yellow); Flower(r,new Vector3(4.6f,.9f,-8.8f),.48f,pink);
            Flowerbed(r,new Vector3(-11,0,1)); Flowerbed(r,new Vector3(11,0,1));
            for(int i=0;i<22;i++)
            {
                float x=Mathf.Sin(i*12.43f)*11, z=Mathf.Cos(i*7.9f)*9;
                var tuft=Group("Grass tuft",r,new Vector3(x,0,z));
                for(int j=0;j<3;j++) {var blade=Ball("Blade",tuft,new Vector3((j-1)*.14f,.22f,0),new Vector3(.1f,.5f,.055f),leaf);blade.localRotation=Quaternion.Euler(0,0,(j-1)*-28);}
            }
            return root;
        }
        static void House(Transform parent, Vector3 p, bool candy)
        {
            var h=Group(candy?"Candy cottage":"Flower cottage",parent,p);
            Box("Foundation",h,new Vector3(0,.15f,0),new Vector3(5.5f,.3f,4.5f),white);
            Box("Walls",h,new Vector3(0,1.8f,0),new Vector3(5,3.3f,4),candy?cream:peach);
            ParkGeometry.Place("Sculpted roof",ParkGeometry.Roof(),h,new Vector3(0,3.35f,0),Vector3.one,candy?chocolate:purple);
            if(candy)
            {
                for(int i=0;i<24;i++)
                {
                    float x=-2.85f+i*5.7f/23;
                    float y=3.6f+1.38f*(1-Mathf.Pow(Mathf.Abs(x)/2.95f,1.5f));
                    Ball("Scalloped icing",h,new Vector3(x,y,-2.49f),new Vector3(.52f,.25f+(i%3)*.035f,.32f),white);
                }
                for(int side=-1;side<=1;side+=2) for(int i=0;i<16;i++)
                    Ball("Eave icing",h,new Vector3(side*2.86f,3.45f,-2.35f+i*.31f),new Vector3(.25f,.22f,.4f),white);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Box("Corner trim",h,new Vector3(side*2.46f,1.7f,-2),new Vector3(.18f,3.1f,.18f),candy?white:wood);
                Box("Window sill",h,new Vector3(side*1.65f,1.4f,-2.22f),new Vector3(1.3f,.14f,.32f),white);
            }
            Box("Chimney",h,new Vector3(-1.5f,4.8f,.9f),new Vector3(.65f,1.7f,.7f),candy?chocolate:purple);
            Box("Chimney cap",h,new Vector3(-1.5f,5.65f,.9f),new Vector3(.85f,.2f,.9f),cream);
            ParkGeometry.Place("Arched door surround",ParkGeometry.Arch(1.9f,2.95f,.22f),h,new Vector3(0,.15f,-2.08f),Vector3.one,wood);
            ParkGeometry.Place("Arched door",ParkGeometry.Arch(1.52f,2.66f,.16f),h,new Vector3(0,.17f,-2.23f),Vector3.one,candy?chocolate:wood);
            for(int i=-2;i<=2;i++) Box("Door plank seam",h,new Vector3(i*.25f,1.22f,-2.32f),new Vector3(.014f,1.95f,.008f),chocolate);
            Ball("Door knob",h,new Vector3(.45f,1.3f,-2.32f),Vector3.one*.17f,yellow);
            Box("Step",h,new Vector3(0,.13f,-2.65f),new Vector3(2.2f,.26f,.9f),cream);
            for(int side=-1;side<=1;side+=2)
            {
                Window(h,new Vector3(side*1.65f,2,-2.1f));
                var sideWindow=Group("Side window",h,new Vector3(side*2.53f,2,0)); sideWindow.localRotation=Quaternion.Euler(0,-side*90,0); Window(sideWindow,Vector3.zero);
                if(candy)
                {
                    for(int j=0;j<10;j++) Shape("Candy stripe",PrimitiveType.Cylinder,h,new Vector3(side*2.5f,.25f+j*.31f,-2.15f),new Vector3(.28f,.155f,.28f),j%2==0?pink:white);
                }
                else
                {
                    Shape("Flower pot",PrimitiveType.Cylinder,h,new Vector3(side*1.9f,.4f,-2.8f),new Vector3(.75f,.4f,.75f),wood);
                    Flower(h,new Vector3(side*1.9f,1.3f,-2.8f),.4f,side<0?pink:yellow);
                }
            }
            for(int i=0;i<3;i++)
            {
                float x=-1.55f+i*1.55f;
                Vector3 point=new Vector3(x,4.25f-Mathf.Abs(x)*.24f,-2.6f);
                if(candy) Ball("Candy button",h,point,new Vector3(.7f,.7f,.2f),i%2==0?pink:yellow);
                else Flower(h,point+Vector3.up*.2f,.65f,i==0?pink:i==1?yellow:white,false);
            }
            Shape("Cottage walkway",PrimitiveType.Cube,h,new Vector3(0,.035f,-3.8f),new Vector3(1.9f,.035f,2.2f),cream);
        }
        static void Window(Transform h,Vector3 p)
        {
            ParkGeometry.Place("Arched window frame",ParkGeometry.Arch(1.15f,1.45f,.15f),h,p-Vector3.up*.6f,Vector3.one,white);
            ParkGeometry.Place("Arched glass",ParkGeometry.Arch(.92f,1.2f,.05f),h,p+new Vector3(0,-.48f,-.1f),Vector3.one,blue);
            Box("Window cross",h,p+new Vector3(0,0,-.13f),new Vector3(.075f,1,.05f),white);
            Box("Window cross",h,p+new Vector3(0,0,-.13f),new Vector3(.9f,.075f,.05f),white);
        }
        static void Tree(Transform parent,Vector3 p,float size)
        {
            var t=Group("Faceted garden tree",parent,p); t.localScale=Vector3.one*size;
            t.localRotation=Quaternion.Euler(0,p.x*17+p.z*11,0);
            ParkGeometry.Place("Flared trunk",trunkMesh,t,Vector3.zero,Vector3.one,wood);
            ParkGeometry.Place("Crown",canopyMesh,t,new Vector3(0,3.25f,0),new Vector3(1.8f,1.65f,1.65f),leaf);
            ParkGeometry.Place("Lower crown",canopyMesh,t,new Vector3(-.4f,2.85f,.15f),new Vector3(1.45f,1.2f,1.35f),leaf);

        }
        static void Flower(Transform parent,Vector3 p,float radius,Material petals,bool stem=true)
        {
            if(stem)
            {
                Beam("Stem",parent,p-Vector3.up*.8f,p,.065f,leaf);
                for(int side=-1;side<=1;side+=2){var l=Ball("Flower leaf",parent,p+new Vector3(side*.16f,-.57f,0),new Vector3(.17f,.5f,.08f),leaf);l.localRotation=Quaternion.Euler(0,0,-side*40);}
            }
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                Ball("Petal",parent,p+new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*radius*.65f,new Vector3(radius*.8f,radius*.8f,.12f),petals);
            }
            Ball("Flower centre",parent,p+Vector3.back*.08f,new Vector3(radius*.5f,radius*.5f,.15f),yellow);
        }
        static void Flowerbed(Transform parent,Vector3 p)
        {
            var b=Group("Flower bed",parent,p);
            Box("Raised planter",b,new Vector3(0,.2f,0),new Vector3(2.6f,.4f,1.2f),wood);
            Box("Soil",b,new Vector3(0,.42f,0),new Vector3(2.35f,.05f,.95f),chocolate);
            for(int i=0;i<5;i++) Flower(b,new Vector3(-.95f+i*.48f,1.05f+(i%2)*.12f,0),.27f,i%2==0?pink:purple);
        }
        static void Fence(Transform parent,Vector3 a,Vector3 b)
        {
            int count=Mathf.CeilToInt(Vector3.Distance(a,b)/1.8f);
            for(int i=0;i<=count;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,(float)i/count);
                Box("Fence post",parent,p+Vector3.up*.65f,new Vector3(.18f,1.3f,.18f),wood);
                Ball("Post cap",parent,p+Vector3.up*1.3f,Vector3.one*.23f,cream);
            }
            Beam("Fence upper rail",parent,a+Vector3.up*.95f,b+Vector3.up*.95f,.12f,wood);
            Beam("Fence lower rail",parent,a+Vector3.up*.45f,b+Vector3.up*.45f,.12f,wood);
        }
        static void Bench(Transform parent,Vector3 p,float angle)
        {
            var t=Group("Garden bench",parent,p);t.localRotation=Quaternion.Euler(0,angle,0);
            for(int i=0;i<3;i++) Box("Seat slat",t,new Vector3(0,.65f,-.28f+i*.28f),new Vector3(2.3f,.12f,.23f),wood);
            for(int i=0;i<2;i++) Box("Back slat",t,new Vector3(0,1.05f+i*.27f,.45f),new Vector3(2.3f,.2f,.12f),wood);
            for(int side=-1;side<=1;side+=2) { Box("Bench leg",t,new Vector3(side*.85f,.32f,0),new Vector3(.14f,.65f,.7f),chocolate); Box("Back support",t,new Vector3(side*.85f,.9f,.45f),new Vector3(.12f,1,.12f),chocolate); }
        }
    }
}
