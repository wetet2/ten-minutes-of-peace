using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkPlayground : MonoBehaviour
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
        public int PendingCount {get{int count=0;if(queues!=null)foreach(var queue in queues)count+=queue.Count;return count;}}
        public int SubmissionCount {get;private set;}
        public bool IsDrawing=>Drawing&&Drawing.Visible;
        public bool IsReady=>queues!=null;
        readonly List<ParkRabbitAgent> animals=new List<ParkRabbitAgent>();
        ParkRabbitAgent fadingAnimal;
        float fadeRemaining;
        Queue<Submission>[] queues;
        Vector3[] pathPoints;
        int[] pathTriangles;
        float lawnHeight;
        Vector2 pondCenter,pondRadii;
        Rect[] walkObstacles;
        Transform animalRoot;
        Camera parkCamera;
        int parkLayers;
        bool parkUsesMsaa;
        internal ParkToast Toast {get;private set;}
        GUIStyle title,button;
        Texture2D settingsIcon;
        readonly DrawingSkin skin=new DrawingSkin();
        ParkLanPanel lan;
        ParkWelcome welcome;
        ParkDialogs dialogs;
        enum View { ModeSelection, Park, Drawing }
        enum Dialog { None, Connection, Settings, Venues }
        View view;
        Dialog dialog;
        internal ParkLanPanel Lan => lan;
        internal string ThemeName=>themeName;
        internal bool IsSelectingMode=>view==View.ModeSelection;
        internal bool IsConnecting=>dialog==Dialog.Connection;
        internal bool IsSettingsOpen=>dialog==Dialog.Settings;
        internal bool IsVenuePickerOpen=>dialog==Dialog.Venues;
        internal int VenueIndex {get;private set;}
        internal bool CanChooseVenue=>IsViewingPark&&lan.IsHosting&&Venues!=null&&Venues.Length>1;
        internal bool IsViewingPark=>view==View.Park;
        sealed class Submission
        {
            public int Id;
            public AnimalKind Kind;
            public byte[] Front,Back;
            public bool Raw;
            public ParkRabbitAgent WaitingAgent;
        }

        void Start()
        {
            if(!Drawing)Drawing=FindAnyObjectByType<DrawingPage>();
            if(!OrbitCamera)OrbitCamera=FindAnyObjectByType<ParkCamera>();
            parkCamera=OrbitCamera?OrbitCamera.GetComponent<Camera>():Camera.main;
            if(parkCamera)
            {
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
            queues=NewQueues(Houses.Length);
            animalRoot=new GameObject("Painted friends").transform;
            Drawing.ShowParkNavigation=true;Drawing.Applied+=OnApplied;Drawing.ParkRequested+=LeaveDrawing;
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
            var toastObject=new GameObject("Park Notifications",typeof(RectTransform));
            toastObject.transform.SetParent(transform,false);
            Toast=toastObject.AddComponent<ParkToast>();
            Toast.Build(dialogs.ToastBackground);
            Drawing.Toast=Toast;
            RabbitCharacter.ConfigurePaintShader(RabbitPaintShader);
            SetView(View.ModeSelection);
        }
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
            WalkArea=venue.WalkArea;lawnHeight=venue.LawnHeight;
            pondCenter=venue.PondCenter;pondRadii=venue.PondRadii;
            walkObstacles=venue.WalkObstacles;
            ReadPath(venue.Path);
            if(OrbitCamera)OrbitCamera.SetVenueView(venue);
            if(parkCamera)parkCamera.backgroundColor=venue.Sky;
            RenderSettings.fogColor=venue.Sky;
        }
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
            var waiting=new List<(int House,Submission Entry)>();
            for(int i=0;i<queues.Length;i++)
                foreach(var entry in queues[i])waiting.Add((Mathf.Min(i,entrances.Length-1),entry));
            var paused=new List<ParkRabbitAgent>();
            foreach(var animal in animals)
            {
                if(!animal.IsEmerging)continue;
                int slot=Mathf.Min(animal.HouseIndex,entrances.Length-1);
                if(!entrances[slot])entrances[slot]=Houses[animal.HouseIndex];
                else
                {
                    paused.Add(animal);
                    waiting.Add((slot,new Submission{Id=animal.Id,WaitingAgent=animal}));
                    animal.gameObject.SetActive(false);
                }
            }
            foreach(var animal in paused)animals.Remove(animal);
            for(int i=0;i<entrances.Length;i++)next.Houses[i].ContinueFrom(entrances[i]);
            queues=NewQueues(entrances.Length);
            waiting.Sort((a,b)=>a.Entry.Id.CompareTo(b.Entry.Id));
            foreach(var item in waiting)queues[item.House].Enqueue(item.Entry);
            previous.Root.SetActive(false);
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
        static Queue<Submission>[] NewQueues(int count)
        {
            var result=new Queue<Submission>[count];
            for(int i=0;i<count;i++)result[i]=new Queue<Submission>();
            return result;
        }
        public float SurfaceHeight(Vector3 position)
        {
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
            if(!AnimalKinds.IsValid(animal)||queues==null||queues.Length==0||front==null||back==null||front.Length==0||back.Length==0)return false;
            return Enqueue((AnimalKind)animal,(byte[])front.Clone(),(byte[])back.Clone(),preferredHouse,false);
        }
        internal byte SubmitLan(AnimalKind kind,byte[] front,byte[] back)
        {
            if(!IsReady||queues.Length==0)return ParkLan.Busy;
            int waiting=PendingCount;
            if(waiting>=8)return ParkLan.Busy;
            return Enqueue(kind,front,back,-1,true)?ParkLan.Accepted:ParkLan.Busy;
        }
        bool Enqueue(AnimalKind kind,byte[] front,byte[] back,int preferredHouse,bool raw)
        {
            int house=preferredHouse>=0?preferredHouse:Random.Range(0,Houses.Length);
            if(house>=queues.Length)return false;
            var submission=new Submission{Id=++SubmissionCount,Kind=kind,Front=front,Back=back,Raw=raw};
            queues[house].Enqueue(submission);
            Toast.Show(Houses[house].DisplayName+"에서 "+AnimalKinds.Name(kind)+" 친구가 나와요!");
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
            if(!IsSelectingMode||IsConnecting||!lan.StartHost())return false;
            ShowPark();
            return true;
        }
        internal void ChooseDrawingMode()
        {
            if(!IsSelectingMode||IsConnecting)return;
            Toast.Clear();
            SetDialog(Dialog.Connection);
        }
        internal void OpenSettings()
        {
            if(IsViewingPark)SetDialog(Dialog.Settings);
        }
        internal void CloseDialog()
        {
            if(IsConnecting)lan.CancelJoin();
            SetDialog(Dialog.None);
        }
        void SetDialog(Dialog next)
        {
            dialog=next;
            welcome.SetModalOpen(IsConnecting);
            dialogs.Show(IsConnecting,IsSettingsOpen,IsVenuePickerOpen);
            if(OrbitCamera)OrbitCamera.InputEnabled=IsViewingPark&&next==Dialog.None;
        }
        internal void ReturnToModeSelection()
        {
            if(Drawing.Sending)return;
            lan.ResetSession();
            SetView(View.ModeSelection);
        }
        void SetView(View next)
        {
            view=next;
            CloseDialog();
            welcome.SetVisible(next==View.ModeSelection);
            Drawing.Visible=next==View.Drawing;
            Toast.UseDrawingLayer(Drawing.Visible);
            bool showPark=next==View.Park;
            if(!parkCamera)return;
            parkCamera.cullingMask=showPark?parkLayers:0;
            parkCamera.allowMSAA=showPark&&parkUsesMsaa;
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
            lan?.Update();
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(IsDrawing)LeaveDrawing();
                else if(dialog!=Dialog.None)CloseDialog();
            }
            if(!ManualSimulation&&(IsViewingPark||(IsDrawing&&!lan.IsJoined)))Advance(Time.deltaTime);
        }
        public void Advance(float seconds)
        {
            if(queues==null||seconds<=0)return;
            int steps=Mathf.CeilToInt(seconds/.05f);float dt=seconds/steps;
            for(int step=0;step<steps;step++)
            {
                AdvanceFade(dt);
                for(int i=0;i<Houses.Length;i++)
                {
                    Houses[i].Advance(dt);
                    if(queues[i].Count==0||Houses[i].IsBusy)continue;
                    if(animals.Count>=AnimalLimit)
                    {
                        BeginOldestFade();
                        continue;
                    }
                    if(!Houses[i].TryReserve())continue;
                    var submission=queues[i].Dequeue();
                    if(submission.WaitingAgent)
                    {
                        submission.WaitingAgent.RestartEmergence(Houses[i],i);
                        animals.Add(submission.WaitingAgent);
                        continue;
                    }
                    var go=new GameObject("Painted "+submission.Kind+" "+submission.Id);go.transform.SetParent(animalRoot,false);
                    var agent=go.AddComponent<ParkRabbitAgent>();agent.Initialize(this,Houses[i],i,submission.Id,submission.Front,submission.Back,submission.Raw,submission.Kind);animals.Add(agent);
                }
                foreach(var animal in animals)animal.Advance(dt);
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
        void AdvanceFade(float deltaTime)
        {
            if(!fadingAnimal)return;
            fadeRemaining=Mathf.Max(0,fadeRemaining-deltaTime);
            fadingAnimal.Character.SetVisibility(fadeRemaining/AnimalFadeDuration);
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
            settingsIcon=CreateSettingsIcon();
        }
        void OnGUI()
        {
            if(lan==null||!IsViewingPark||dialog!=Dialog.None)return;
            var area=Screen.safeArea;
            if(area.width<=0||area.height<=0)return;
            var old=GUI.matrix;
            float scale=Mathf.Min(area.height/900f,area.width/665f),width=area.width/scale;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(area.xMin,Screen.height-area.yMax),Quaternion.identity,new Vector3(scale,scale,1));
            Styles();
            const float themeWidth=125;
            var place=new Rect(8,18,themeWidth,60);
            if(CanChooseVenue)
            {
                if(HudButton(place,ThemeName+"   "))OpenVenuePicker();
                if(Event.current.type==EventType.Repaint)
                {
                    var tint=GUI.color;
                    GUI.color=title.normal.textColor;
                    for(int row=0;row<6;row++)
                        GUI.DrawTexture(new Rect(place.xMax-29+row,place.y+28+row,11-2*row,1),Texture2D.whiteTexture);
                    GUI.color=tint;
                }
            }
            else GUI.Label(place,ThemeName,title);
            if(HudButton(new Rect(width-487,18,158,60),"카메라 초기화")&&OrbitCamera)OrbitCamera.ResetView();
            if(HudButton(new Rect(width-316,18,224,60),"동물 색칠하기",true))ShowDrawing();
            var settingsButton=new Rect(width-80,18,60,60);
            skin.Draw(settingsButton,new Color32(255,247,226,255),27);
            GUI.DrawTexture(new Rect(settingsButton.center.x-17,settingsButton.center.y-17,34,34),settingsIcon);
            if(GUI.Button(settingsButton,new GUIContent("", "설정"),GUIStyle.none))OpenSettings();
            GUI.matrix=old;
        }
        bool HudButton(Rect rect,string text,bool accent=false)
        {
            skin.Draw(rect,accent?new Color32(104,193,66,255):new Color32(255,247,226,255),27,accent);
            GUI.Label(rect,text,accent?button:title);
            return GUI.Button(rect,GUIContent.none,GUIStyle.none);
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
            if(Drawing){Drawing.Applied-=OnApplied;Drawing.ParkRequested-=LeaveDrawing;}
            if(settingsIcon)Destroy(settingsIcon);
            skin.Dispose();
        }
    }
}
