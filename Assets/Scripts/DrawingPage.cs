using System;
using UnityEngine;

namespace PeacePark
{
    public sealed class DrawingPage : MonoBehaviour
    {
        public event Action<int,byte[],byte[]> Applied;
        public int SelectedAnimal {get;private set;}
        public float RemainingLock {get{return Mathf.Max(0,lockedUntil-Time.unscaledTime);}}
        public AnimalCanvas Front {get;private set;}
        public AnimalCanvas Back {get;private set;}
        readonly string[] names={"곰","호랑이","토끼","개구리","기린"};
        readonly Color[] colors={new Color32(246,76,82,255),new Color32(255,160,41,255),new Color32(255,220,55,255),new Color32(105,194,83,255),new Color32(59,200,212,255),new Color32(67,130,231,255),new Color32(164,103,217,255),new Color32(242,143,190,255),new Color32(160,106,69,255),new Color32(52,49,48,255)};
        readonly float[] radii={5,11,20,32};
        // Pixel bounds include transparent padding and preserve every ear/horn.
        readonly Rect[] headBounds={new Rect(15,199,422,398),new Rect(453,204,415,393),new Rect(883,91,400,511),new Rect(1306,226,413,371),new Rect(1722,152,447,451)};
        Texture2D headAtlas;
        readonly DrawingSkin skin=new DrawingSkin();
        int colorIndex=4,brushIndex=2,activeSide=-1;
        Vector2 last;
        float lockedUntil;
        Texture2D round;
        GUIStyle label,title,button;
        Font font;
        public void SelectAnimal(int index)
        {
            Front?.Dispose();Back?.Dispose();SelectedAnimal=index;
            Front=new AnimalCanvas(index,false);Back=new AnimalCanvas(index,true);activeSide=-1;
        }
        void Awake()
        {
            SelectAnimal(2);headAtlas=Resources.Load<Texture2D>("Drawing/AnimalHeads");
            round=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(31.5f-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))));
            round.SetPixels(pixels);round.Apply();font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Apple SD Gothic Neo","Noto Sans CJK KR","Arial"},28);
        }
        void Styles()
        {
            if(label!=null)return;
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=25,alignment=TextAnchor.MiddleCenter};label.normal.textColor=new Color32(84,65,51,255);
            title=new GUIStyle(label){fontSize=30,fontStyle=FontStyle.Bold};
            button=new GUIStyle(label){fontSize=32,fontStyle=FontStyle.Bold};
        }
        void Panel(Rect r,Color c,float radius=24)
        {
            if(radius==0){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;return;}
            skin.Draw(r,c,radius);
        }
        void Circle(Rect r,Color c){skin.Draw(r,c,r.width/2,true);}
        static Rect Fit(Rect area)
        {float w=Mathf.Min(area.width,area.height*AnimalCanvas.Width/AnimalCanvas.Height);float h=w*AnimalCanvas.Height/AnimalCanvas.Width;return new Rect(area.center.x-w/2,area.center.y-h/2,w,h);}
        void Canvas(Rect r,AnimalCanvas canvas)
        {GUI.DrawTexture(r,canvas.Paint);GUI.DrawTexture(r,canvas.Outline);}
        public bool ApplyDrawing()
        {
            if(RemainingLock>0)return false;
            lockedUntil=Time.unscaledTime+3;
            Applied?.Invoke(SelectedAnimal,Front.Paint.EncodeToPNG(),Back.Paint.EncodeToPNG());return true;
        }
        void OnGUI()
        {
            Styles();float scale=Screen.height/900f;float width=Screen.width/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            Panel(new Rect(0,0,width,900),new Color32(251,244,226,255),0);
            float tabW=Mathf.Min(280,(width-80)/5);float start=(width-5*tabW)/2;
            for(int i=0;i<5;i++)
            {
                var r=new Rect(start+i*tabW+6,18,tabW-12,140);
                if(SelectedAnimal==i)Panel(new Rect(r.x-4,r.y-4,r.width+8,r.height+8),new Color32(145,230,151,255),38);
                Panel(r,SelectedAnimal==i?new Color32(192,242,197,255):new Color32(255,246,226,255),34);
                var bounds=headBounds[i];float ih=i==2?134:120;float iw=ih*bounds.width/bounds.height;
                var icon=new Rect(r.center.x-iw/2,r.center.y-ih/2,iw,ih);
                if(headAtlas)GUI.DrawTextureWithTexCoords(icon,headAtlas,new Rect(bounds.x/2172f,1-bounds.yMax/724f,bounds.width/2172f,bounds.height/724f));
                else GUI.Label(r,names[i],title);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)&&SelectedAnimal!=i)SelectAnimal(i);
            }
            float left=Mathf.Clamp(width*.17f,150,270),right=150;
            Panel(new Rect(12,182,left-24,700),new Color32(255,245,221,255),34);
            float diameter=Mathf.Min(108,(left-48)/2);
            for(int i=0;i<10;i++)
            {
                var r=new Rect(24+(i%2)*(left-42)/2,220+(i/2)*133,diameter,diameter);
                if(colorIndex==i)Circle(new Rect(r.x-4,r.y-4,r.width+8,r.height+8),new Color32(230,241,204,255));
                Circle(r,colors[i]);if(GUI.Button(r,GUIContent.none,GUIStyle.none))colorIndex=i;
            }
            Panel(new Rect(width-right+6,182,right-18,592),new Color32(255,246,228,255),34);
            for(int i=0;i<4;i++)
            {
                var r=new Rect(width-right+22,209+i*141,100,100);
                if(brushIndex==i)Circle(new Rect(r.x-4,r.y-4,108,108),new Color32(137,227,143,255));
                Circle(r,brushIndex==i?new Color32(205,250,215,255):new Color32(255,246,228,255));
                float d=10+i*13;GUI.color=new Color32(30,30,24,255);GUI.DrawTexture(new Rect(r.center.x-d/2,r.center.y-d/2,d,d),round);GUI.color=Color.white;
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))brushIndex=i;
            }
            float cw=width-left-right-10;Panel(new Rect(left,182,cw,700),Color.white,34);
            float half=cw/2;
            GUI.Label(new Rect(left,195,half,50),"앞모습",title);GUI.Label(new Rect(left+half,195,half,50),"뒷모습",title);
            var frontRect=Fit(new Rect(left+8,245,half-16,580));var backRect=Fit(new Rect(left+half+8,245,half-16,580));
            Canvas(frontRect,Front);Canvas(backRect,Back);
            var apply=new Rect(width-340,804,326,77);
            skin.Draw(apply,RemainingLock>0?new Color32(197,211,179,255):new Color32(116,211,33,255),38,true);
            var old=button.normal.textColor;button.normal.textColor=Color.white;button.fontSize=40;
            GUI.Label(apply,RemainingLock>0?Mathf.CeilToInt(RemainingLock)+"초":"적용",button);button.normal.textColor=old;
            GUI.enabled=RemainingLock<=0;if(GUI.Button(apply,GUIContent.none,GUIStyle.none))ApplyDrawing();GUI.enabled=true;
            HandlePaint(frontRect,backRect);
        }
        void HandlePaint(Rect frontRect,Rect backRect)
        {
            var e=Event.current;var mouse=e.mousePosition;
            if(e.type==EventType.MouseDown&&e.button==0)
            {
                activeSide=frontRect.Contains(mouse)?0:backRect.Contains(mouse)?1:-1;
                if(activeSide>=0){last=Point(activeSide==0?frontRect:backRect,mouse);Paint(last,last);e.Use();}
            }
            else if(e.type==EventType.MouseDrag&&activeSide>=0)
            {
                var r=activeSide==0?frontRect:backRect;var p=Point(r,mouse);Paint(last,p);last=p;e.Use();
            }
            else if(e.rawType==EventType.MouseUp)activeSide=-1;
        }
        static Vector2 Point(Rect r,Vector2 p){return new Vector2((p.x-r.x)/r.width*AnimalCanvas.Width,(1-(p.y-r.y)/r.height)*AnimalCanvas.Height);}
        void Paint(Vector2 a,Vector2 b){(activeSide==0?Front:Back).Stroke(a,b,radii[brushIndex]*2,colors[colorIndex]);}
        void OnDestroy(){Front?.Dispose();Back?.Dispose();skin.Dispose();if(round)Destroy(round);if(font)Destroy(font);}
    }
}
