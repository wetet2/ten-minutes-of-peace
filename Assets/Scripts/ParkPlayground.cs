using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed partial class ParkPlayground : MonoBehaviour
    {
        internal const int AnimalLimit=15;
        internal const float AnimalFadeDuration=1.2f;
        public DrawingPage Drawing;
        public ParkCamera OrbitCamera;
        public ParkHouse[] Houses;
        public ParkFountain Fountain;
        public ParkVenue[] Venues;
        public Shader RabbitPaintShader;
        public Rect WalkArea=new Rect(-9.5f,-7.4f,19f,8.2f);
        public float GroundHeight=.04f;
        [SerializeField] string themeName="공원";
        public bool ManualSimulation {get;set;}
        public IReadOnlyList<ParkRabbitAgent> Animals=>animals;
        public int PendingCount=>arrivals?.Count??0;
        public int SubmissionCount {get;private set;}
        public bool IsDrawing=>Drawing&&Drawing.Visible;
        public bool IsReady=>arrivals!=null&&!startupSplash;
        readonly List<ParkRabbitAgent> animals=new List<ParkRabbitAgent>();
        ParkRabbitAgent fadingAnimal;
        float fadeRemaining;
        List<Submission> arrivals;
        int nextEntrance;
        Vector3[] pathPoints;
        int[] pathTriangles;
        float lawnHeight;
        float defaultFogStart,defaultFogEnd,defaultFarClip;
        ParkDuneTerrain duneTerrain;
        Vector2 pondCenter,pondRadii;
        Rect[] walkObstacles;
        Transform animalRoot;
        Camera parkCamera;
        int parkLayers;
        bool parkUsesMsaa;
        internal ParkToast Toast {get;private set;}
        GUIStyle title,button;
        Texture2D venueTriangle,settingsIcon,cameraIcon,distanceGuideIcon;
        bool menuCollapsed;
        Texture2D pressedHand;
        bool cursorPressed;
        float menuVisibility=1;
        int dialogClosedFrame=-1;
        readonly DrawingSkin skin=new DrawingSkin();
        ParkLanPanel lan;
        ParkMusic music;
        ParkWelcome welcome;
        PoriSplash startupSplash;
        ParkDialogs dialogs;
        ParkDistanceGuide distanceGuide;
        enum View { ModeSelection, Park, Drawing }
        enum Dialog { None, Connection, Settings, Venues, DistanceGuide }
        View view;
        Dialog dialog;
        internal ParkLanPanel Lan => lan;
        internal string ThemeName=>themeName;
        internal bool IsSelectingMode=>view==View.ModeSelection;
        internal bool IsConnecting=>dialog==Dialog.Connection;
        internal bool IsSettingsOpen=>dialog==Dialog.Settings;
        internal bool IsVenuePickerOpen=>dialog==Dialog.Venues;
        internal bool IsDistanceGuideOpen=>dialog==Dialog.DistanceGuide;
        internal int VenueIndex {get;private set;}
        internal bool CanChooseVenue=>IsViewingPark&&lan.IsHosting&&Venues!=null&&Venues.Length>1;
        internal bool IsViewingPark=>view==View.Park;
        sealed class Submission
        {
            public int Id;
            public int PreferredHouse;
            public AnimalKind Kind;
            public byte[] Front,Back;
            public bool Raw;
            public ParkRabbitAgent WaitingAgent;
        }

        void Start()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ApplyWindowsTitle();
#endif
            if(!Drawing)Drawing=FindAnyObjectByType<DrawingPage>();
            if(!OrbitCamera)OrbitCamera=FindAnyObjectByType<ParkCamera>();
            if(OrbitCamera)OrbitCamera.SetPark(this);
            music=new ParkMusic(gameObject);
            defaultFogEnd=RenderSettings.fogEndDistance;
            defaultFogStart=RenderSettings.fogStartDistance;
            parkCamera=OrbitCamera?OrbitCamera.GetComponent<Camera>():Camera.main;
            if(parkCamera)
            {
                defaultFarClip=parkCamera.farClipPlane;
                parkLayers=parkCamera.cullingMask;
                parkUsesMsaa=parkCamera.allowMSAA;
            }
            if(Venues!=null&&Venues.Length>0)ApplyVenue(Venues[0]);
            else
            {
                var lawn=GameObject.Find("Meadow island");
                lawnHeight=lawn?lawn.GetComponent<Renderer>().bounds.max.y:GroundHeight;
                var path=GameObject.Find("Winding path");
                ReadPath(path?path.GetComponent<MeshFilter>():null);
                if(Houses==null||Houses.Length==0)Houses=FindObjectsByType<ParkHouse>(FindObjectsSortMode.None);
            }
            arrivals=new List<Submission>();
            animalRoot=new GameObject("Painted friends").transform;
            Drawing.Applied+=OnApplied;
            if(OrbitCamera)OrbitCamera.ShowControls=false;
            lan = new ParkLanPanel(this);
            var welcomeObject=new GameObject("Start Screen",typeof(RectTransform));
            welcomeObject.transform.SetParent(transform,false);
            welcome=welcomeObject.AddComponent<ParkWelcome>();
            welcome.Build(this);
            var dialogObject=new GameObject("Park Popups",typeof(RectTransform));
            dialogObject.transform.SetParent(transform,false);
            dialogs=dialogObject.AddComponent<ParkDialogs>();
            dialogs.Build(this);
            var guideObject=new GameObject("Step Back Reminder",typeof(RectTransform));
            guideObject.transform.SetParent(transform,false);
            distanceGuide=guideObject.AddComponent<ParkDistanceGuide>();
            distanceGuide.Build(this);
            var toastObject=new GameObject("Park Notifications",typeof(RectTransform));
            toastObject.transform.SetParent(transform,false);
            Toast=toastObject.AddComponent<ParkToast>();
            Toast.Build(dialogs.ToastBackground,this);
            Drawing.Toast=Toast;
            RabbitCharacter.ConfigurePaintShader(RabbitPaintShader);
            SetView(View.ModeSelection);
            startupSplash = PoriSplash.Play(transform, () => welcome.SetVisible(true));
            if (startupSplash) welcome.SetVisible(false);
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Unity removes filename punctuation from the standalone product name.
        static void ApplyWindowsTitle()
        {
            using var process=System.Diagnostics.Process.GetCurrentProcess();
            var window=System.IntPtr.Zero;
            while((window=FindWindowExW(System.IntPtr.Zero,window,"UnityWndClass",null))!=System.IntPtr.Zero)
            {
                GetWindowThreadProcessId(window,out uint owner);
                if(owner!=process.Id)continue;
                SetWindowTextW(window,ParkLanguage.Title);
                return;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,ExactSpelling=true)]
        static extern System.IntPtr FindWindowExW(System.IntPtr parent,System.IntPtr after,string className,string title);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(System.IntPtr window,out uint processId);

        [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,ExactSpelling=true)]
        static extern bool SetWindowTextW(System.IntPtr window,string title);
#endif
        void ReadPath(MeshFilter path)
        {
            pathPoints=null;pathTriangles=null;
            if(!path)return;
            var mesh=path.sharedMesh;
            pathPoints=mesh.vertices;pathTriangles=mesh.triangles;
            for(int i=0;i<pathPoints.Length;i++)pathPoints[i]=path.transform.TransformPoint(pathPoints[i]);
        }
        void ApplyVenue(ParkVenue venue)
        {
            themeName=venue.Name;Houses=venue.Houses;Fountain=venue.Fountain;
            Slide=venue.Slide;
            Trampoline=venue.Trampoline;
            Seesaw=venue.Seesaw;
            Roundabout=venue.Roundabout;
            Train=venue.Train;
            Wheel=venue.Wheel;
            Carousel=venue.Carousel;
            ForestRides=venue.ForestRides;
            SandSled=venue.SandSled;
            WalkArea=venue.WalkArea;lawnHeight=venue.LawnHeight;
            duneTerrain=venue.DuneTerrain;
            pondCenter=venue.PondCenter;pondRadii=venue.PondRadii;
            walkObstacles=venue.WalkObstacles;
            ReadPath(venue.Path);
            if(OrbitCamera)OrbitCamera.SetVenueView(venue);
            if(parkCamera)parkCamera.backgroundColor=venue.Sky;
            RenderSettings.fogColor=venue.Sky;
            RenderSettings.fogEndDistance=venue.FogEnd>0?venue.FogEnd:defaultFogEnd;
            RenderSettings.fogStartDistance=venue.FogStart>0?venue.FogStart:defaultFogStart;
            if(IsViewingPark)music.PlayVenue(themeName);
        }
        internal ParkSlide Slide {get;private set;}
        internal ParkTrampoline Trampoline {get;private set;}
        internal ParkSeesaw Seesaw {get;private set;}
        internal ParkRoundaboutRide Roundabout {get;private set;}
        internal ParkElephantTrain Train {get;private set;}
        internal ParkThemeParkWheel Wheel {get;private set;}
        internal ParkCarouselRide Carousel {get;private set;}
        internal ParkForestRide[] ForestRides {get;private set;}
        internal ParkSandSled SandSled {get;private set;}
        internal void OpenVenuePicker()
        {
            if(CanChooseVenue)SetDialog(Dialog.Venues);
        }
        internal bool SelectVenue(int index)
        {
            if(!CanChooseVenue||!IsVenuePickerOpen||index<0||index>=Venues.Length)return false;
            ChangeVenue(index);
            CloseDialog();
            return true;
        }
        internal void ChangeVenue(int index)
        {
            if(index==VenueIndex)return;
            var previous=Venues[VenueIndex];
            var next=Venues[index];
            var entrances=new ParkHouse[next.Houses.Length];
            foreach(var entry in arrivals)entry.PreferredHouse=Mathf.Min(entry.PreferredHouse,entrances.Length-1);
            var paused=new List<ParkRabbitAgent>();
            foreach(var animal in animals)
            {
                if(!animal.IsEmerging)continue;
                int slot=Mathf.Min(animal.HouseIndex,entrances.Length-1);
                if(!entrances[slot])entrances[slot]=Houses[animal.HouseIndex];
                else
                {
                    paused.Add(animal);
                    arrivals.Add(new Submission{Id=animal.Id,PreferredHouse=slot,WaitingAgent=animal});
                    animal.gameObject.SetActive(false);
                }
            }
            foreach(var animal in paused)animals.Remove(animal);
            for(int i=0;i<entrances.Length;i++)next.Houses[i].ContinueFrom(entrances[i]);
            arrivals.Sort((a,b)=>a.Id.CompareTo(b.Id));
            nextEntrance=0;
            previous.Root.SetActive(false);
            if(parkCamera)
                parkCamera.farClipPlane=next.FarClipDistance>0?next.FarClipDistance:defaultFarClip;
            VenueIndex=index;
            next.Root.SetActive(true);
            ApplyVenue(next);
            foreach(var animal in animals)
            {
                int slot=Mathf.Min(animal.HouseIndex,Houses.Length-1);
                animal.ChangeVenue(Houses[slot],slot);
            }
            Toast.Clear();
        }
        public float SurfaceHeight(Vector3 position)
        {
            if(duneTerrain)return duneTerrain.SurfaceHeight(position);
            if(pathPoints!=null)for(int i=0;i<pathTriangles.Length;i+=3)
            {
                var a=pathPoints[pathTriangles[i]];var b=pathPoints[pathTriangles[i+1]];var c=pathPoints[pathTriangles[i+2]];
                float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if(Mathf.Abs(det)<.000001f)continue;
                float u=((b.z-c.z)*(position.x-c.x)+(c.x-b.x)*(position.z-c.z))/det;
                float v=((c.z-a.z)*(position.x-c.x)+(a.x-c.x)*(position.z-c.z))/det;
                if(u>=0&&v>=0&&u+v<=1)return u*a.y+v*b.y+(1-u-v)*c.y;
            }
            return lawnHeight;
        }
        public bool TrySubmit(int animal,byte[] front,byte[] back,int preferredHouse=-1)
        {
            if(!AnimalKinds.IsValid(animal)||arrivals==null||Houses.Length==0||front==null||back==null||front.Length==0||back.Length==0)return false;
            return Enqueue((AnimalKind)animal,(byte[])front.Clone(),(byte[])back.Clone(),preferredHouse,false);
        }
        internal byte SubmitLan(AnimalKind kind,byte[] front,byte[] back)
        {
            if(!IsReady||Houses.Length==0)return ParkLan.Busy;
            int waiting=PendingCount;
            if(waiting>=8)return ParkLan.Busy;
            return Enqueue(kind,front,back,-1,true)?ParkLan.Accepted:ParkLan.Busy;
        }
        bool Enqueue(AnimalKind kind,byte[] front,byte[] back,int preferredHouse,bool raw)
        {
            if(preferredHouse>=Houses.Length)return false;
            arrivals.Add(new Submission{Id=++SubmissionCount,PreferredHouse=preferredHouse,Kind=kind,Front=front,Back=back,Raw=raw});
            if(!raw)Toast.Show("친구가 곧 나와요!");
            return true;
        }
        void OnApplied(int animal,byte[] front,byte[] back)
        {if(TrySubmit(animal,front,back))ShowPark();}
        public void ShowDrawing()
        {
            Drawing.SelectAnimal(Drawing.SelectedAnimal);
            SetView(View.Drawing);
        }
        public void ShowPark()
        {
            RabbitCharacter.Prewarm();
            SetView(View.Park);
        }
        internal bool ChooseHostMode()
        {
            if(!IsReady||!IsSelectingMode||IsConnecting||welcome.IsChoosingVenue)return false;
            if(Venues==null||Venues.Length==0)return EnterHostedVenue(-1);
            Toast.Clear();
            welcome.ShowVenueChoices(true);
            return true;
        }
        internal bool ChooseStartingVenue(int index)
        {
            if(!IsReady||!IsSelectingMode||IsConnecting||!welcome.IsChoosingVenue||
                index<0||index>=Venues.Length)return false;
            return EnterHostedVenue(index);
        }
        bool EnterHostedVenue(int index)
        {
            if(!lan.StartHost())return false;
            if(index>=0)ChangeVenue(index);
            ShowPark();
            return true;
        }
        internal void ChooseDrawingMode()
        {
            if(!IsReady||!IsSelectingMode||IsConnecting||welcome.IsChoosingVenue)return;
            Toast.Clear();
            SetDialog(Dialog.Connection);
        }
        internal void OpenSettings()
        {
            if(IsViewingPark)SetDialog(Dialog.Settings);
        }
        internal void OpenDistanceGuide()
        {
            if(IsViewingPark)SetDialog(Dialog.DistanceGuide);
        }
        internal float MusicVolume=>music.Volume;
        internal void SetMusicVolume(float volume)
        {
            bool wasSilent=music.Volume==0f;
            music.SetVolume(volume);
            if(IsViewingPark&&wasSilent&&music.Volume>0f)music.PlayVenue(themeName,fadeIn:false);
        }
        internal void CloseDialog()
        {
            if(IsConnecting)lan.CancelJoin();
            SetDialog(Dialog.None);
        }
        void SetDialog(Dialog next)
        {
            if(dialog!=Dialog.None&&next==Dialog.None)dialogClosedFrame=Time.frameCount;
            if(IsSettingsOpen&&next!=Dialog.Settings)music.SaveVolume();
            dialog=next;
            welcome.SetModalOpen(IsConnecting);
            dialogs.Show(IsConnecting,IsSettingsOpen,IsVenuePickerOpen);
            distanceGuide.gameObject.SetActive(IsDistanceGuideOpen);
            if(OrbitCamera)OrbitCamera.InputEnabled=IsViewingPark&&next==Dialog.None;
            if(parkCamera)
            {
                bool showScene=IsViewingPark&&!IsDistanceGuideOpen;
                parkCamera.cullingMask=showScene?parkLayers:0;
                parkCamera.allowMSAA=showScene&&parkUsesMsaa;
            }
        }
        internal void ReturnToModeSelection()
        {
            if(Drawing.Sending)return;
            lan.ResetSession();
            SetView(View.ModeSelection);
        }
        void SetView(View next)
        {
            if(next!=View.Park&&OrbitCamera)
            {
                OrbitCamera.LeaveTrainView();
                OrbitCamera.CancelAnimalFollow(true);
            }
            view=next;
            CloseDialog();
            welcome.SetVisible(next==View.ModeSelection);
            Drawing.Visible=next==View.Drawing;
            Toast.UseDrawingLayer(Drawing.Visible);
            bool showPark=next==View.Park;
            if(showPark)music.PlayVenue(themeName);
            else music.FadeOut();
        }
        void LeaveDrawing()
        {
            if(Drawing.Sending)return;
            if(lan.IsJoined)ReturnToModeSelection();
            else ShowPark();
        }
        internal bool IsSceneryClear(Vector3 position)
        {
            if(walkObstacles!=null)
                foreach(var obstacle in walkObstacles)
                    if(obstacle.Contains(new Vector2(position.x,position.z)))return false;
            if(Fountain&&Fountain.Overlaps(position))return false;
            if(pondRadii.x>0)
            {
                var offset=new Vector2(position.x,position.z)-pondCenter;
                if(new Vector2(offset.x/pondRadii.x,offset.y/pondRadii.y).sqrMagnitude<1)return false;
            }
            return true;
        }
        public bool IsClear(Vector3 position,ParkRabbitAgent self,float radius)
        {
            if(!IsSceneryClear(position))return false;
            foreach(var animal in animals)
            {
                if(animal==self||animal.State==ParkRabbitState.OpeningDoor)continue;
                var delta=position-animal.transform.position;delta.y=0;if(delta.sqrMagnitude<radius*radius)return false;
            }
            return true;
        }
        void Update()
        {
            if(!Application.isMobilePlatform)
                SetCursorPressed(Application.isFocused&&Input.mousePresent&&Input.GetMouseButton(0));
            menuVisibility=Mathf.MoveTowards(menuVisibility,menuCollapsed?0:1,Time.unscaledDeltaTime*6);
            lan?.Update();
            music?.Advance(Time.unscaledDeltaTime);
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(IsDrawing)LeaveDrawing();
                else if(dialog!=Dialog.None)CloseDialog();
                else if(IsSelectingMode&&welcome.IsChoosingVenue)welcome.ShowVenueChoices(false);
                else if(OrbitCamera)OrbitCamera.LeaveTrainView();
            }
            if(!ManualSimulation&&(IsViewingPark||(IsDrawing&&!lan.IsJoined)))Advance(Time.deltaTime);
        }
        void OnEnable()
        {
            if(IsViewingPark)music?.PlayVenue(themeName);
        }
        void SetCursorPressed(bool pressed)
        {
            if(cursorPressed==pressed)return;
            cursorPressed=pressed;
            if(pressed&&!pressedHand)pressedHand=Resources.Load<Texture2D>("Cursors/ChildHandPressed");
            // A null texture restores the pointing hand configured in Player Settings.
            Cursor.SetCursor(pressed?pressedHand:null,pressed?new Vector2(2,2):Vector2.zero,CursorMode.Auto);
        }
        void OnApplicationFocus(bool focused){if(!focused)SetCursorPressed(false);}
        void OnDisable()
        {
            SetCursorPressed(false);
            music?.Stop();
        }
        void OnApplicationPause(bool paused)
        {
            if(paused&&IsSettingsOpen)music.SaveVolume();
        }
        public void Advance(float seconds)
        {
            if(arrivals==null||seconds<=0)return;
            int steps=Mathf.CeilToInt(seconds/.05f);float dt=seconds/steps;
            for(int step=0;step<steps;step++)
            {
                AdvanceFade(dt);
                foreach(var house in Houses)house.Advance(dt);
                SpawnWaitingAnimals();
                foreach(var animal in animals)animal.Advance(dt);
                if(Wheel&&Wheel.isActiveAndEnabled)Wheel.Advance(dt);
                if(Train&&Train.isActiveAndEnabled)Train.Advance(dt);
                if(Carousel&&Carousel.isActiveAndEnabled)Carousel.Advance(dt);
                if(Trampoline&&Trampoline.isActiveAndEnabled)Trampoline.Advance(dt);
                if(Seesaw&&Seesaw.isActiveAndEnabled)Seesaw.Advance(dt);
                if(Roundabout&&Roundabout.isActiveAndEnabled)Roundabout.Advance(dt);
                if(SandSled&&SandSled.isActiveAndEnabled)SandSled.Advance(dt);
                if(ForestRides!=null)
                    foreach(var ride in ForestRides)
                        if(ride&&ride.isActiveAndEnabled)ride.Advance(dt);
            }
        }
        void SpawnWaitingAnimals()
        {
            int firstEntrance=nextEntrance;
            for(int offset=0;offset<Houses.Length&&arrivals.Count>0;offset++)
            {
                int index=(firstEntrance+offset)%Houses.Length;
                var house=Houses[index];
                if(!house.CanReserve)continue;
                int waiting=arrivals.FindIndex(entry=>entry.PreferredHouse<0||entry.PreferredHouse==index);
                if(waiting<0)continue;
                if(animals.Count>=AnimalLimit)
                {
                    BeginOldestFade();
                    return;
                }
                if(!house.TryReserve())continue;
                var submission=arrivals[waiting];
                arrivals.RemoveAt(waiting);
                nextEntrance=(index+1)%Houses.Length;
                var agent=submission.WaitingAgent;
                if(agent)agent.RestartEmergence(house,index);
                else
                {
                    var go=new GameObject("Painted "+submission.Kind+" "+submission.Id);
                    go.transform.SetParent(animalRoot,false);
                    agent=go.AddComponent<ParkRabbitAgent>();
                    agent.Initialize(this,house,index,submission.Id,submission.Front,submission.Back,submission.Raw,submission.Kind);
                    Toast.Show(ParkLanguage.Arrival(house.DisplayName,submission.Kind));
                }
                animals.Add(agent);
            }
        }
        void BeginOldestFade()
        {
            if(fadingAnimal)return;
            var oldest=animals[0];
            foreach(var animal in animals)
                if(animal.Id<oldest.Id)oldest=animal;
            if(oldest.IsEmerging)return;
            fadingAnimal=oldest;
            fadeRemaining=AnimalFadeDuration;
        }
        internal bool CanFollowAnimal(ParkRabbitAgent animal)=>animal&&animal!=fadingAnimal&&
            animal.gameObject.activeInHierarchy&&animal.Character&&animal.Character.Skin.enabled&&
            animals.Contains(animal);
        void AdvanceFade(float deltaTime)
        {
            if(!fadingAnimal)return;
            fadeRemaining=Mathf.Max(0,fadeRemaining-deltaTime);
            fadingAnimal.SetVisibility(fadeRemaining/AnimalFadeDuration);
            if(fadeRemaining>0)return;
            animals.Remove(fadingAnimal);
            fadingAnimal.gameObject.SetActive(false);
            Destroy(fadingAnimal.gameObject);
            fadingAnimal=null;
        }
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){font=ParkFonts.Bold,fontSize=24,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal};title.normal.textColor=new Color32(80,62,47,255);
            button=new GUIStyle(title){fontSize=25};button.normal.textColor=Color.white;
            venueTriangle=CreateVenueTriangle(title.normal.textColor);
            settingsIcon=CreateSettingsIcon();
            cameraIcon=Resources.Load<Texture2D>("CameraViewReset");
            distanceGuideIcon=ParkDistanceGuide.CreateToolbarIcon();
        }
        void OnGUI()
        {
            if(lan==null||!IsViewingPark||dialog!=Dialog.None||Time.frameCount==dialogClosedFrame)return;
            var area=Screen.safeArea;
            if(area.width<=0||area.height<=0)return;
            var old=GUI.matrix;
            float scale=HudScale(area),width=area.width/scale;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(area.xMin,Screen.height-area.yMax),Quaternion.identity,new Vector3(scale,scale,1));
            Styles();
            if(menuVisibility>0)
            {
                GUI.BeginGroup(new Rect(0,0,width-80,84));
                GUI.BeginGroup(new Rect(0,-84*(1-Mathf.SmoothStep(0,1,menuVisibility)),width,84));
                bool wasEnabled=GUI.enabled;
                GUI.enabled=wasEnabled&&!menuCollapsed;
                DrawMenu(width);
                GUI.enabled=wasEnabled;
                GUI.EndGroup();
                GUI.EndGroup();
            }
            var handle=new Rect(width-76,18,60,60);
            skin.Draw(handle,new Color32(255,247,226,255),27);
            GUI.DrawTextureWithTexCoords(new Rect(handle.center.x-10,handle.center.y-7,20,14),venueTriangle,
                menuCollapsed?new Rect(0,0,1,1):new Rect(0,1,1,-1));
            if(GUI.Button(new Rect(width-80,0,80,82),new GUIContent("",ParkLanguage.Text(menuCollapsed?"메뉴 펼치기":"메뉴 접기")),GUIStyle.none))
                menuCollapsed=!menuCollapsed;
            DrawAnimalActions(width,area.height/scale);
            GUI.matrix=old;
        }
        static float HudScale(Rect area)=>Mathf.Min(area.height/900f,area.width/665f);
        internal bool ToolbarContains(Vector2 screenPoint)
        {
            var area=Screen.safeArea;
            float scale=HudScale(area);
            if(ActionButtonsContain(screenPoint,area,scale))return true;
            if(screenPoint.y<area.yMax-84*scale)return false;
            return menuVisibility>0 || screenPoint.x>=area.xMax-80*scale;
        }
        void DrawMenu(float width)
        {
            string themeLabel=ParkLanguage.Text(ThemeName);
            float drawingWidth=Mathf.Clamp(width-495,170,222);
            float drawingLeft=width-298-drawingWidth;
            float themeWidth=Mathf.Min(drawingLeft-15,Mathf.Max(125,title.CalcSize(new GUIContent(themeLabel)).x+42));
            var place=new Rect(8,18,themeWidth,60);
            if(CanChooseVenue)
            {
                if(HudButton(place,themeLabel+"   "))OpenVenuePicker();
                if(Event.current.type==EventType.Repaint)
                    GUI.DrawTexture(new Rect(place.xMax-30,place.center.y-5,14,10),venueTriangle);
            }
            else GUI.Label(place,themeLabel,title);
            if(HudButton(new Rect(drawingLeft,18,drawingWidth,60),"동물 색칠하기",true))ShowDrawing();
            if(IconButton(new Rect(width-286,18,60,60),distanceGuideIcon,43,ParkDistanceGuide.ToolbarHint))OpenDistanceGuide();
            if(IconButton(new Rect(width-216,18,60,60),cameraIcon,48,"카메라 초기화")&&OrbitCamera)OrbitCamera.ResetView();
            if(IconButton(new Rect(width-146,18,60,60),settingsIcon,34,"설정"))OpenSettings();
        }
        bool IconButton(Rect rect,Texture2D icon,float size,string hint)
        {
            skin.Draw(rect,new Color32(255,247,226,255),27);
            GUI.DrawTexture(new Rect(rect.center.x-size/2,rect.center.y-size/2,size,size),icon);
            return GUI.Button(rect,new GUIContent("",ParkLanguage.Text(hint)),GUIStyle.none);
        }
        bool HudButton(Rect rect,string text,bool accent=false)
        {
            skin.Draw(rect,accent?new Color32(104,193,66,255):new Color32(255,247,226,255),27,accent);
            var style=accent?button:title;
            text=ParkLanguage.Text(text);
            int previousSize=style.fontSize;
            while(style.fontSize>12&&style.CalcSize(new GUIContent(text)).x>rect.width-18)style.fontSize--;
            GUI.Label(rect,text,style);
            style.fontSize=previousSize;
            return GUI.Button(rect,GUIContent.none,GUIStyle.none);
        }
        static Texture2D CreateVenueTriangle(Color32 color)
        {
            const int width=32,height=24;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false)
                {name="Venue dropdown triangle",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float row=y+.5f;
                float halfWidth=(row-2)*.7f;
                float edge=Mathf.Min(22-row,halfWidth-Mathf.Abs(x+.5f-width*.5f));
                color.a=(byte)Mathf.RoundToInt(255*Mathf.Clamp01(edge+.5f));
                pixels[y*width+x]=color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false,true);
            return texture;
        }
        static Texture2D CreateSettingsIcon()
        {
            const int size=96;
            const float toothAngle=Mathf.PI/4;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Settings gear"};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var point=new Vector2(x+.5f-size*.5f,y+.5f-size*.5f);
                float radius=point.magnitude;
                float ray=Mathf.Atan2(point.y,point.x)/toothAngle;
                float toothWidth=Mathf.Abs(ray-Mathf.Round(ray))*toothAngle*radius;
                float solid=Mathf.Max(33-radius,Mathf.Min(43-radius,8-toothWidth));
                float alpha=Mathf.Clamp01(.5f+Mathf.Min(solid,radius-14));
                pixels[y*size+x]=new Color32(80,62,47,(byte)Mathf.RoundToInt(255*alpha));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false,true);
            return texture;
        }
        void OnDestroy()
        {
            lan?.Dispose();
            if(Drawing)Drawing.Applied-=OnApplied;
            if(venueTriangle)Destroy(venueTriangle);
            if(settingsIcon)Destroy(settingsIcon);
            if(distanceGuideIcon)Destroy(distanceGuideIcon);
            skin.Dispose();
        }
    }
}
