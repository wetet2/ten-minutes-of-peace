using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkPlayground : MonoBehaviour
    {
        public DrawingPage Drawing;
        public ParkCamera OrbitCamera;
        public ParkHouse[] Houses;
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
        Queue<Submission>[] queues;
        Vector3[] pathPoints;
        int[] pathTriangles;
        float lawnHeight;
        Transform animalRoot;
        Camera parkCamera;
        int parkLayers;
        bool parkUsesMsaa;
        string notice;
        float noticeUntil;
        GUIStyle title,button;
        readonly DrawingSkin skin=new DrawingSkin();
        ParkLanPanel lan;
        ParkWelcome welcome;
        ParkDialogs dialogs;
        enum View { ModeSelection, Park, Drawing }
        enum Dialog { None, Connection, ParkInfo }
        View view;
        Dialog dialog;
        internal ParkLanPanel Lan => lan;
        internal string ThemeName=>themeName;
        internal bool IsSelectingMode=>view==View.ModeSelection;
        internal bool IsConnecting=>dialog==Dialog.Connection;
        internal bool IsParkInfoOpen=>dialog==Dialog.ParkInfo;
        internal bool IsViewingPark=>view==View.Park;
        sealed class Submission {public int Id;public AnimalKind Kind;public byte[] Front,Back;public bool Raw;}

        void Start()
        {
            var lawn=GameObject.Find("Meadow island");lawnHeight=lawn?lawn.GetComponent<Renderer>().bounds.max.y:GroundHeight;
            var path=GameObject.Find("Winding path");
            if(path)
            {
                var mesh=path.GetComponent<MeshFilter>().sharedMesh;pathPoints=mesh.vertices;pathTriangles=mesh.triangles;
                for(int i=0;i<pathPoints.Length;i++)pathPoints[i]=path.transform.TransformPoint(pathPoints[i]);
            }
            if(!Drawing)Drawing=FindAnyObjectByType<DrawingPage>();
            if(!OrbitCamera)OrbitCamera=FindAnyObjectByType<ParkCamera>();
            parkCamera=OrbitCamera?OrbitCamera.GetComponent<Camera>():Camera.main;
            if(parkCamera)
            {
                parkLayers=parkCamera.cullingMask;
                parkUsesMsaa=parkCamera.allowMSAA;
            }
            if(Houses==null||Houses.Length==0)Houses=FindObjectsByType<ParkHouse>(FindObjectsSortMode.None);
            queues=new Queue<Submission>[Houses.Length];for(int i=0;i<queues.Length;i++)queues[i]=new Queue<Submission>();
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
            RabbitCharacter.ConfigurePaintShader(RabbitPaintShader);
            SetView(View.ModeSelection);
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
            if(animals.Count+waiting>=24)return ParkLan.Full;
            if(waiting>=8)return ParkLan.Busy;
            return Enqueue(kind,front,back,-1,true)?ParkLan.Accepted:ParkLan.Busy;
        }
        bool Enqueue(AnimalKind kind,byte[] front,byte[] back,int preferredHouse,bool raw)
        {
            int house=preferredHouse>=0?preferredHouse:Random.Range(0,Houses.Length);
            if(house>=queues.Length)return false;
            var submission=new Submission{Id=++SubmissionCount,Kind=kind,Front=front,Back=back,Raw=raw};
            queues[house].Enqueue(submission);
            notice=Houses[house].DisplayName+"에서 "+AnimalKinds.Name(kind)+" 친구가 나와요!";noticeUntil=Time.unscaledTime+5;
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
            lan.ClearMessage();
            SetDialog(Dialog.Connection);
        }
        internal void OpenParkInfo()
        {
            if(IsViewingPark)SetDialog(Dialog.ParkInfo);
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
            dialogs.Show(IsConnecting,IsParkInfoOpen);
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
        public bool IsClear(Vector3 position,ParkRabbitAgent self,float radius)
        {
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
                for(int i=0;i<Houses.Length;i++)
                {
                    Houses[i].Advance(dt);
                    if(queues[i].Count==0||!Houses[i].TryReserve())continue;
                    var submission=queues[i].Dequeue();
                    var go=new GameObject("Painted "+submission.Kind+" "+submission.Id);go.transform.SetParent(animalRoot,false);
                    var agent=go.AddComponent<ParkRabbitAgent>();agent.Initialize(this,Houses[i],i,submission.Id,submission.Front,submission.Back,submission.Raw,submission.Kind);animals.Add(agent);
                }
                foreach(var animal in animals)animal.Advance(dt);
            }
        }
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){font=ParkFonts.Bold,fontSize=24,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal};title.normal.textColor=new Color32(80,62,47,255);
            button=new GUIStyle(title){fontSize=25};button.normal.textColor=Color.white;
        }
        void OnGUI()
        {
            if(lan==null||!IsViewingPark||IsParkInfoOpen)return;
            var area=Screen.safeArea;
            if(area.width<=0||area.height<=0)return;
            var old=GUI.matrix;
            float scale=Mathf.Min(area.height/900f,area.width/575f),width=area.width/scale;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(area.xMin,Screen.height-area.yMax),Quaternion.identity,new Vector3(scale,scale,1));
            Styles();
            float themeWidth=Mathf.Clamp(title.CalcSize(new GUIContent(ThemeName)).x+44,110,140);
            if(HudButton(new Rect(20,18,themeWidth,60),ThemeName))OpenParkInfo();
            else if(HudButton(new Rect(width-415,18,158,60),"카메라 초기화")&&OrbitCamera)OrbitCamera.ResetView();
            else if(HudButton(new Rect(width-244,18,224,60),"동물 색칠하기",true))ShowDrawing();
            if(IsViewingPark&&!IsParkInfoOpen&&Time.unscaledTime<noticeUntil&&!string.IsNullOrEmpty(notice))
            {var message=new Rect((width-490)/2,94,490,48);skin.Draw(message,new Color32(255,247,226,255),23);GUI.Label(message,notice,title);}
            GUI.matrix=old;
        }
        bool HudButton(Rect rect,string text,bool accent=false)
        {
            skin.Draw(rect,accent?new Color32(104,193,66,255):new Color32(255,247,226,255),27,accent);
            GUI.Label(rect,text,accent?button:title);
            return GUI.Button(rect,GUIContent.none,GUIStyle.none);
        }
        void OnDestroy()
        {
            lan?.Dispose();
            if(Drawing){Drawing.Applied-=OnApplied;Drawing.ParkRequested-=LeaveDrawing;}
            skin.Dispose();
        }
    }
}
