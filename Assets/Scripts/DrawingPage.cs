using System;
using UnityEngine;

namespace PeacePark
{
    public sealed class DrawingPage : MonoBehaviour
    {
        bool visible=true;
        public bool Visible
        {
            get=>visible;
            set{visible=value;if(!value)painting=tabPressed=false;}
        }
        public int LockedAnimal {get;set;}=-1;
        public Func<bool> RemoteApply { get; set; }
        public bool Sending { get; set; }
        internal ParkToast Toast {get;set;}
        public event Action<int,byte[],byte[]> Applied;
        public int SelectedAnimal {get;private set;}
        public float RemainingLock {get{return Mathf.Max(0,lockedUntil-Time.unscaledTime);}}
        public AnimalCanvas Front {get;private set;}
        public AnimalCanvas Back {get;private set;}
        static readonly Color32[] colors=
        {
            new Color32(246,76,82,255),new Color32(255,160,41,255),
            new Color32(255,220,55,255),new Color32(105,194,83,255),
            new Color32(59,200,212,255),new Color32(67,130,231,255),
            new Color32(164,103,217,255),new Color32(242,143,190,255),
            new Color32(160,106,69,255),new Color32(52,49,48,255),
            new Color32(255,255,255,255),new Color32(255,201,157,255)
        };
        internal static int PaletteColorCount=>colors.Length;
        readonly float[] radii={5,11,20,32};
        // Pixel bounds include transparent padding and preserve every ear/horn.
        readonly Rect[] headBounds={new Rect(15,199,422,398),new Rect(453,204,415,393),new Rect(883,91,400,511),new Rect(1306,226,413,371),new Rect(1722,152,447,451)};
        Texture2D headAtlas,elephantIcon,triceratopsIcon;
        readonly Texture2D[] newIcons=new Texture2D[3];
        float tabScroll;
        float tabPointerStart,tabScrollStart;
        bool tabPressed,tabDragging;
        readonly DrawingSkin skin=new DrawingSkin();
        int colorIndex=4,brushIndex=2;
        bool painting;
        Vector2 last;
        float lockedUntil;
        Texture2D round;
        GUIStyle label,title,button,navigation;
        Font font;
        public void SelectAnimal(int index)
        {
            if(Front==null||Back==null)
            {
                Front=new AnimalCanvas(index,false);
                Back=new AnimalCanvas(index,true);
            }
            else if(SelectedAnimal==index)
            {
                Front.Clear();
                Back.Clear();
            }
            else
            {
                Front.SetTemplate(index,false);
                Back.SetTemplate(index,true);
            }
            SelectedAnimal=index;
            painting=false;
        }
        void Awake()
        {
            SelectAnimal(2);headAtlas=Resources.Load<Texture2D>("Drawing/AnimalHeads");
            elephantIcon=Resources.Load<Texture2D>("Drawing/ElephantHead");
            triceratopsIcon=Resources.Load<Texture2D>("Drawing/TriceratopsHead");
            for(int i=0;i<newIcons.Length;i++)
                newIcons[i]=Resources.Load<Texture2D>("Drawing/"+((AnimalKind)((int)AnimalKind.Crocodile+i))+"Head");
            round=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(31.5f-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))));
            round.SetPixels(pixels);round.Apply();font=ParkFonts.Regular;
        }
        void Styles()
        {
            if(label!=null)return;
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=25,alignment=TextAnchor.MiddleCenter};label.normal.textColor=new Color32(84,65,51,255);
            title=new GUIStyle(label){font=ParkFonts.Bold,fontSize=30,fontStyle=FontStyle.Normal};
            button=new GUIStyle(title){fontSize=32};
            navigation=new GUIStyle(title){fontSize=22};
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
            if(RemainingLock>0||Sending)return false;
            lockedUntil=Time.unscaledTime+3;
            if(RemoteApply!=null)return RemoteApply();
            Applied?.Invoke(SelectedAnimal,Front.Paint.EncodeToPNG(),Back.Paint.EncodeToPNG());return true;
        }
        internal void AutoFill()
        {
            if(Sending)return;
            painting=false;
            Color32 color=Color.HSVToRGB(UnityEngine.Random.value,UnityEngine.Random.Range(.56f,.9f),
                UnityEngine.Random.Range(.8f,1f));
            Front.Fill(color);
            Back.Fill(color);
        }
        void OnGUI()
        {
            if(!Visible)return;
            Styles();float scale=Screen.height/900f;float width=Screen.width/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            Panel(new Rect(0,0,width,900),new Color32(251,244,226,255),0);
            DrawAnimalTabs(width);
            float left=Mathf.Clamp(width*.17f,150,270),right=150;
            var palette=new Rect(12,182,left-24,700);
            Panel(palette,new Color32(255,245,221,255),34);
            var automatic=new Rect(palette.x+14,palette.yMax-86,palette.width-28,72);
            var grid=new Rect(automatic.x,palette.y+15,automatic.width,automatic.y-palette.y-29);
            DrawPalette(grid);
            skin.Draw(automatic,new Color32(218,199,246,255),26,true);
            GUI.Label(automatic,"자동",title);
            GUI.enabled=!Sending;
            if(GUI.Button(automatic,GUIContent.none,GUIStyle.none))AutoFill();
            GUI.enabled=true;
            var brushPanel=new Rect(width-right+6,182,right-18,592);
            Panel(brushPanel,new Color32(255,246,228,255),34);
            for(int i=0;i<4;i++)
            {
                var r=new Rect(brushPanel.center.x-50,209+i*141,100,100);
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
            var apply=new Rect(brushPanel.x,brushPanel.yMax+12,brushPanel.width,96);
            skin.Draw(apply,RemainingLock>0||Sending?new Color32(197,211,179,255):new Color32(116,211,33,255),38,true);
            var old=button.normal.textColor;button.normal.textColor=Color.white;button.fontSize=Sending?24:36;
            GUI.Label(apply,Sending?"보내는 중":RemainingLock>0?Mathf.CeilToInt(RemainingLock)+"초":"보내기",button);button.normal.textColor=old;
            GUI.enabled=RemainingLock<=0&&!Sending;if(GUI.Button(apply,GUIContent.none,GUIStyle.none))ApplyDrawing();GUI.enabled=true;
            Toast?.DrawOverColoring();
            HandlePaint(Event.current,frontRect,backRect);
        }
        void DrawPalette(Rect area)
        {
            for(int i=0;i<PaletteColorCount;i++)
            {
                var r=PaletteButtonRect(area,i);
                if(colorIndex==i)Circle(new Rect(r.x-4,r.y-4,r.width+8,r.height+8),new Color32(170,216,155,255));
                Circle(r,colors[i]);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))colorIndex=i;
            }
        }
        internal static Rect PaletteButtonRect(Rect area,int index)
        {
            const int columns=2;
            const float gap=14;
            int rows=(PaletteColorCount+columns-1)/columns;
            float diameter=Mathf.Min(84,Mathf.Min((area.width-gap)/columns,(area.height-(rows-1)*gap)/rows));
            float pitch=diameter+gap;
            var size=new Vector2(columns*pitch-gap,rows*pitch-gap);
            var first=area.center-size/2;
            return new Rect(first.x+(index%columns)*pitch,first.y+(index/columns)*pitch,diameter,diameter);
        }
        void DrawAnimalTabs(float width)
        {
            const float inset=20;
            var area=new Rect(inset,10,width-2*inset,157);
            float tabWidth=Mathf.Clamp(area.width/7,136,240);
            float maxScroll=Mathf.Max(0,tabWidth*AnimalKinds.Count-area.width);
            tabScroll=Mathf.Clamp(tabScroll,0,maxScroll);
            float start=Mathf.Max(0,(area.width-tabWidth*AnimalKinds.Count)/2);
            var e=Event.current;
            if(HandleTabGesture(e.type,e.button,e.mousePosition,e.delta,area,tabWidth,start,maxScroll))e.Use();
            GUI.BeginGroup(area);
            for(int i=0;i<AnimalKinds.Count;i++)
            {
                var card=new Rect(start+i*tabWidth-tabScroll+6,8,tabWidth-12,140);
                if(card.xMax+12<0||card.xMin-12>area.width)continue;
                if(SelectedAnimal==i)Panel(new Rect(card.x-4,card.y-4,card.width+8,card.height+8),new Color32(145,230,151,255),38);
                Panel(card,SelectedAnimal==i?new Color32(192,242,197,255):new Color32(255,246,226,255),34);
                var art=new Rect(card.x+6,card.y+4,card.width-12,105);
                Texture2D icon=i switch
                {
                    (int)AnimalKind.Elephant=>elephantIcon,
                    (int)AnimalKind.Triceratops=>triceratopsIcon,
                    >=(int)AnimalKind.Crocodile=>newIcons[i-(int)AnimalKind.Crocodile],
                    _=>null
                };
                if(icon)GUI.DrawTexture(art,icon,ScaleMode.ScaleToFit);
                else if(i<headBounds.Length&&headAtlas)
                {
                    var bounds=headBounds[i];float ratio=bounds.width/bounds.height;
                    float iw=Mathf.Min(art.width,art.height*ratio),ih=iw/ratio;
                    GUI.DrawTextureWithTexCoords(new Rect(art.center.x-iw/2,art.center.y-ih/2,iw,ih),headAtlas,
                        new Rect(bounds.x/2172f,1-bounds.yMax/724f,bounds.width/2172f,bounds.height/724f));
                }
                GUI.Label(new Rect(card.x,card.y+108,card.width,28),AnimalKinds.Name((AnimalKind)i),navigation);
            }
            GUI.EndGroup();
            if(maxScroll>0&&Event.current.type==EventType.Repaint)
            {
                float fraction=area.width/(area.width+maxScroll);
                var track=new Rect(area.center.x-60,162,120,4);
                Panel(track,new Color32(229,215,184,255),0);
                var thumb=new Rect(track.x+track.width*(1-fraction)*tabScroll/maxScroll,track.y,track.width*fraction,track.height);
                Panel(thumb,new Color32(123,168,104,255),0);
            }
        }
        internal bool HandleTabGesture(EventType type,int button,Vector2 pointer,Vector2 wheel,Rect area,float tabWidth,float start,float maxScroll)
        {
            if(type==EventType.ScrollWheel&&area.Contains(pointer)&&maxScroll>0)
            {
                float delta=Mathf.Abs(wheel.x)>Mathf.Abs(wheel.y)?wheel.x:wheel.y;
                tabScroll=Mathf.Clamp(tabScroll+delta*24,0,maxScroll);
                return true;
            }
            if(button!=0)return false;
            if(type==EventType.MouseDown&&area.Contains(pointer))
            {
                tabPressed=true;tabDragging=false;tabPointerStart=pointer.x;tabScrollStart=tabScroll;
                painting=false;
                return true;
            }
            if(!tabPressed)return false;
            if(type==EventType.MouseDrag)
            {
                float shift=pointer.x-tabPointerStart;
                tabDragging|=Mathf.Abs(shift)>8;
                if(tabDragging)tabScroll=Mathf.Clamp(tabScrollStart-shift,0,maxScroll);
                return true;
            }
            if(type==EventType.MouseUp)
            {
                tabPressed=false;
                if(!tabDragging&&area.Contains(pointer)&&!Sending)
                {
                    int index=Mathf.FloorToInt((pointer.x-area.x-start+tabScroll)/tabWidth);
                    if(AnimalKinds.IsValid(index)&&index!=SelectedAnimal&&(LockedAnimal<0||LockedAnimal==index))SelectAnimal(index);
                }
                return true;
            }
            return false;
        }
        internal void HandlePaint(Event e,Rect frontRect,Rect backRect)
        {
            if(Sending){painting=false;return;}
            var mouse=e.mousePosition;
            if(e.rawType==EventType.MouseDown&&e.button==0)
            {
                if(tabPressed||e.type==EventType.Used){painting=false;return;}
                painting=true;
                last=mouse;
                if(e.type==EventType.MouseDown)
                {
                    PaintAcrossCanvases(last,mouse,frontRect,backRect);
                    e.Use();
                }
            }
            else if(painting&&e.rawType==EventType.MouseDrag&&e.button==0)
            {
                PaintAcrossCanvases(last,mouse,frontRect,backRect);
                last=mouse;
                if(e.type==EventType.MouseDrag)e.Use();
            }
            else if(e.rawType==EventType.MouseUp&&e.button==0)
            {
                if(painting&&e.type==EventType.MouseUp)PaintAcrossCanvases(last,mouse,frontRect,backRect);
                painting=false;
            }
        }
        void OnApplicationFocus(bool focused){if(!focused)painting=tabPressed=false;}
        void PaintAcrossCanvases(Vector2 from,Vector2 to,Rect frontRect,Rect backRect)
        {
            PaintSegment(Front,frontRect,from,to);
            PaintSegment(Back,backRect,from,to);
        }
        void PaintSegment(AnimalCanvas canvas,Rect rect,Vector2 from,Vector2 to)
        {
            if(rect.width<=0||rect.height<=0)return;
            var delta=to-from;
            float enter=0,exit=1;
            if(!ClipAxis(from.x,delta.x,rect.xMin,rect.xMax,ref enter,ref exit)||
                !ClipAxis(from.y,delta.y,rect.yMin,rect.yMax,ref enter,ref exit))return;
            canvas.Stroke(Point(rect,from+delta*enter),Point(rect,from+delta*exit),radii[brushIndex]*2,colors[colorIndex]);
        }
        static bool ClipAxis(float origin,float delta,float min,float max,ref float enter,ref float exit)
        {
            if(Mathf.Approximately(delta,0))return origin>=min&&origin<=max;
            float a=(min-origin)/delta,b=(max-origin)/delta;
            enter=Mathf.Max(enter,Mathf.Min(a,b));
            exit=Mathf.Min(exit,Mathf.Max(a,b));
            return enter<=exit;
        }
        static Vector2 Point(Rect r,Vector2 p){return new Vector2((p.x-r.x)/r.width*AnimalCanvas.Width,(1-(p.y-r.y)/r.height)*AnimalCanvas.Height);}
        void OnDestroy()
        {
            Front?.Dispose();Back?.Dispose();skin.Dispose();
            if(round)Destroy(round);
        }
    }
}
