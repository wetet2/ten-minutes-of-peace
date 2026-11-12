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
        Font font;
        GUIStyle title,button,caption;
        readonly DrawingSkin skin=new DrawingSkin();
        ParkLanPanel lan;
        internal ParkLanPanel Lan => lan;
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
            font=ParkFonts.Regular;
            ShowPark();RabbitCharacter.ConfigurePaintShader(RabbitPaintShader);RabbitCharacter.Prewarm();
            lan = new ParkLanPanel(this);
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
            SetDrawingView(true);
        }
        public void ShowPark()
        {
            SetDrawingView(false);
        }
        void SetDrawingView(bool drawing)
        {
            Drawing.Visible=drawing;
            if(OrbitCamera)OrbitCamera.InputEnabled=!drawing;
            if(!parkCamera)return;
            // The opaque drawing UI only needs the camera's clear pass.
            parkCamera.cullingMask=drawing?0:parkLayers;
            parkCamera.allowMSAA=!drawing&&parkUsesMsaa;
        }
        void LeaveDrawing()
        {
            if(Drawing.Sending)return;
            lan.Disconnect();
            ShowPark();
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
            if(Input.GetKeyDown(KeyCode.Escape)&&Drawing.Visible)LeaveDrawing();
            if(!ManualSimulation&&lan?.IsJoined!=true)Advance(Time.deltaTime);
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
            caption=new GUIStyle(title){font=font,fontSize=18};
        }
        void OnGUI()
        {
            if(!Drawing||Drawing.Visible)return;
            var old=GUI.matrix;float scale=Screen.height/900f,width=Screen.width/scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));Styles();
            var info=new Rect(20,18,410,56);skin.Draw(info,new Color32(255,247,226,255),27);
            GUI.Label(info,"우리 공원 · 친구 "+animals.Count+"마리"+(PendingCount>0?" · 기다리는 친구 "+PendingCount:""),title);
            var draw=new Rect(width-244,18,224,60);skin.Draw(draw,new Color32(104,193,66,255),29,true);GUI.Label(draw,"동물 색칠하기",button);
            if(!lan.IsOpen&&GUI.Button(draw,GUIContent.none,GUIStyle.none))ShowDrawing();
            if(Time.unscaledTime<noticeUntil&&!string.IsNullOrEmpty(notice))
            {var message=new Rect((width-490)/2,94,490,48);skin.Draw(message,new Color32(255,247,226,255),23);GUI.Label(message,notice,title);}
            GUI.Label(new Rect(20,850,540,32),"마우스 드래그: 회전   ·   휠: 확대/축소   ·   Home: 처음 시점",caption);
            lan.Draw(width);
            GUI.matrix=old;
        }
        void OnDestroy()
        {
            lan?.Dispose();
            if(Drawing){Drawing.Applied-=OnApplied;Drawing.ParkRequested-=LeaveDrawing;}
            skin.Dispose();
        }
    }
}
