using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace PeacePark
{
    // Opt-in standalone-player smoke test. Normal launches never create this component.
    public sealed class ParkPlaygroundSmoke : MonoBehaviour
    {
        string output;
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-peaceParkSmoke")<0)return;
            Application.runInBackground=true;var go=new GameObject("Living park player validation");go.AddComponent<ParkPlaygroundSmoke>();
        }
        void OnEnable(){Application.logMessageReceived+=Log;}
        void OnDisable(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception){failed=true;Application.Quit(1);}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-peaceParkOutput");
            output=index>=0&&index+1<args.Length?args[index+1]:Path.GetFullPath(Path.Combine(Application.dataPath,"../Previews"));Directory.CreateDirectory(output);
            ParkPlayground park=null;float deadline=Time.realtimeSinceStartup+60;
            while(!park||!park.IsReady)
            {park=FindAnyObjectByType<ParkPlayground>();if(Time.realtimeSinceStartup>deadline)throw new Exception("Living park did not initialize");yield return null;}
            park.ManualSimulation=true;
            var orbit=park.OrbitCamera;
            var initialPosition=orbit.transform.position;
            var initialRotation=orbit.transform.rotation;
            float initialZoom=orbit.distance;
            orbit.focus+=Vector3.right;
            orbit.distance=15;
            orbit.yaw=75;
            orbit.pitch=20;
            orbit.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            orbit.ResetView();
            Require(Vector3.Distance(orbit.transform.position,initialPosition)<.0001f&&
                Quaternion.Angle(orbit.transform.rotation,initialRotation)<.01f&&orbit.distance==initialZoom,
                "Camera reset did not restore the scene's initial position, angle and zoom.");
            Debug.Log("CAMERA RESET PASS: restored the scene's initial position, angle and zoom.");
            var regular=ParkFonts.Regular;var bold=ParkFonts.Bold;
            Require(regular&&bold&&regular.name=="BaskinRobbins-Regular"&&bold.name=="BaskinRobbins-Bold","Bundled Baskin Robbins fonts missing");
            foreach(char letter in "애들아놀라빠분만쉰다놀이공간열기동물색칠하기연결앞모습뒷적용원트리케라톱스")
                Require(regular.HasCharacter(letter)&&bold.HasCharacter(letter),"Korean glyph missing: "+letter);
            Require(GameObject.Find("Storybook countryside"),"Continuous countryside missing");
            Require(!GameObject.Find("Earth edge"),"Display plinth still visible");
            Debug.Log("STORYBOOK FONT/ART PASS: bundled Baskin Robbins Regular/Bold render Korean UI glyphs; continuous landscape replaces visible plinth.");
            foreach(var house in park.Houses)
            {
                Require(house.HasRoundedPorch&&!house.transform.Find("Step")&&!house.transform.Find("Cottage walkway"),"Old rectangular porch still present");
                Require(house.transform.Find("Rounded themed porch/Sloping rounded landing"),"Rounded porch missing");
                for(int i=0;i<=100;i++)
                {
                    var position=house.transform.InverseTransformPoint(house.ExitPosition(i/100f));
                    if(position.z<=-2.25f&&position.z>=-3.7f)Require(Mathf.Abs(position.y-ParkHouse.PorchHeight(position.z))<.0001f,"Rabbit does not follow porch slope");
                }
            }
            Debug.Log("PORCH PASS: both old slabs replaced, 202 emergence samples follow the new landing height.");
            park.ShowDrawing();yield return null;
            Paint(park.Drawing.Front,new Color32(245,111,151,255));Paint(park.Drawing.Back,new Color32(245,111,151,255));
            Require(park.Drawing.ApplyDrawing()&&!park.Drawing.ApplyDrawing(),"Player Apply/cooldown failed");
            var colors=new Color32[]{new Color32(64,175,216,255),new Color32(153,201,79,255),new Color32(248,176,64,255),new Color32(180,124,216,255)};
            for(int i=0;i<colors.Length;i++)
            {
                Paint(park.Drawing.Front,colors[i]);Paint(park.Drawing.Back,colors[i]);
                Require(park.TrySubmit(2,park.Drawing.Front.Paint.EncodeToPNG(),park.Drawing.Back.Paint.EncodeToPNG(),i%2),"Player submission failed");
            }
            park.Advance(1.3f);var first=park.Animals[0];
            Require(first.Character.Skin.sharedMaterials[0].shader&&first.Character.Skin.sharedMaterials[0].shader.isSupported,"Player paint shader missing");
            Require(first.Character.Skin.sharedMaterials[0].shader.name=="PeacePark/RabbitPaint","Wrong player paint shader");
            park.OrbitCamera.enabled=false;var cottage=park.Houses[first.HouseIndex].transform.position;
            Camera.main.transform.position=cottage+new Vector3(3.5f,3.1f,-8.5f);Camera.main.transform.LookAt(cottage+new Vector3(0,1.4f,-2.2f));
            yield return Capture("living-park-player-door.png");
            park.Advance(35);
            Require(park.Animals.Count==5&&park.PendingCount==0,"Player queues did not finish");
            foreach(var animal in park.Animals)
                Require(animal.State==ParkRabbitState.Wandering&&animal.WaveCount==1&&animal.ExitedThroughOpenDoor,"Player arrival/wandering failed");
            Camera.main.transform.position=new Vector3(0,22,-25);Camera.main.transform.LookAt(new Vector3(0,1,0));
            yield return Capture("living-park-player-friends.png");
            foreach(var house in park.Houses)
            {
                var origin=house.transform.position;Camera.main.transform.position=origin+new Vector3(2.8f,2.4f,-7.5f);
                Camera.main.transform.LookAt(origin+new Vector3(0,.8f,-2.5f));
                yield return Capture(house.name=="Flower cottage"?"porch-flower-1440p.png":"porch-candy-1440p.png");
            }
            Camera.main.transform.position=new Vector3(0,22,-25);Camera.main.transform.LookAt(new Vector3(0,1,0));
            var contactProbe=new Mesh();float maxGap=0,maxFootError=0;
            var walkingSkin=first.Character.Skin;
            var bindVertices=walkingSkin.sharedMesh.vertices;
            var feet=new[]{new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
            var posedVertices=new System.Collections.Generic.List<Vector3>();
            for(int i=0;i<first.Character.SkinVertexCount;i++)
                if(bindVertices[i].y<=.45f)feet[bindVertices[i].x<0?0:1].Add(i);
            try
            {
                first.Character.PlayIdlePose();
                walkingSkin.BakeMesh(contactProbe,false);
                var idleVertices=contactProbe.vertices;
                var anchors=new int[2,3];
                var forwardLimit=new[]{float.PositiveInfinity,float.PositiveInfinity};
                var backwardLimit=new[]{float.NegativeInfinity,float.NegativeInfinity};
                for(int foot=0;foot<feet.Length;foot++)
                {
                    Require(feet[foot].Count>0,"No foot vertices found for walking validation");
                    for(int j=0;j<3;j++)anchors[foot,j]=feet[foot][0];
                    foreach(int vertex in feet[foot])
                    {
                        var point=bindVertices[vertex];
                        if(point.z<bindVertices[anchors[foot,0]].z)anchors[foot,0]=vertex;
                        if(point.z>bindVertices[anchors[foot,1]].z)anchors[foot,1]=vertex;
                        if(Mathf.Abs(point.x)>Mathf.Abs(bindVertices[anchors[foot,2]].x))anchors[foot,2]=vertex;
                    }
                }
                for(int i=0;i<=128;i++)
                {
                    first.Character.PlayWalkingPose(i*Mathf.PI*2/128);walkingSkin.BakeMesh(contactProbe,false);contactProbe.RecalculateBounds();
                    maxGap=Mathf.Max(maxGap,Mathf.Abs(contactProbe.bounds.min.y));
                    contactProbe.GetVertices(posedVertices);
                    for(int foot=0;foot<feet.Length;foot++)
                    {
                        float toeZ=posedVertices[anchors[foot,0]].z;
                        forwardLimit[foot]=Mathf.Min(forwardLimit[foot],toeZ);
                        backwardLimit[foot]=Mathf.Max(backwardLimit[foot],toeZ);
                    }
                    // Within-foot distances isolate deformation from rotation and movement.
                    for(int foot=0;foot<feet.Length;foot++)
                    foreach(int vertex in feet[foot])
                    for(int j=0;j<3;j++)
                    {
                        int anchor=anchors[foot,j];
                        float restLength=Vector3.Distance(idleVertices[vertex],idleVertices[anchor]);
                        float posedLength=Vector3.Distance(posedVertices[vertex],posedVertices[anchor]);
                        maxFootError=Mathf.Max(maxFootError,Mathf.Abs(posedLength-restLength));
                    }
                }
                for(int foot=0;foot<feet.Length;foot++)
                    Require(backwardLimit[foot]-forwardLimit[foot]>.25f,"Foot does not swing with its leg");
            }
            finally{Destroy(contactProbe);}
            Require(maxGap<.003f,"Walking feet do not contact floor: "+maxGap);
            Require(maxFootError<.0001f,"Feet deform during walking: "+maxFootError);
            Debug.Log("FOOT CONTACT PASS: 129 walking poses, maximum ground error "+maxGap.ToString("F5")+" world units.");
            Debug.Log($"FOOT SHAPE PASS: {feet[0].Count+feet[1].Count} foot vertices retain their shape across 129 walking poses, maximum error {maxFootError:F6}.");
            park.ManualSimulation=false;
            // Warm up before reporting paced frame times; exclude submission/PNG work.
            yield return new WaitForSecondsRealtime(2);
            var timings=new System.Collections.Generic.List<float>();float until=Time.realtimeSinceStartup+5;
            var benchmarkTarget=new RenderTexture(2560,1440,24){antiAliasing=4};var benchmarkCamera=Camera.main;
            var previousTarget=benchmarkCamera.targetTexture;benchmarkCamera.targetTexture=benchmarkTarget;
            try
            {
                while(Time.realtimeSinceStartup<until)
                {benchmarkCamera.Render();yield return null;timings.Add(Time.unscaledDeltaTime*1000);}
            }
            finally{benchmarkCamera.targetTexture=previousTarget;benchmarkTarget.Release();Destroy(benchmarkTarget);}
            timings.Sort();float sum=0;foreach(float timing in timings)sum+=timing;
            Debug.Log($"PLAYER FRAME SAMPLE: explicit 2560x1440 offscreen rendering, 5 rabbits, {1000/(sum/timings.Count):F1} average fps, p95 {timings[Mathf.Min(timings.Count-1,Mathf.FloorToInt(timings.Count*.95f))]:F1} ms; excludes visible-window presentation, not a device guarantee.");
            if(!failed){Debug.Log("LIVING PARK PLAYER PASS: drawing Apply/cooldown, five independent friends, rendered paint shader, door emergence and bounded wandering.");Application.Quit(0);}
        }
        static void Paint(AnimalCanvas canvas,Color32 color)
        {for(float y=0;y<AnimalCanvas.Height+64;y+=96)canvas.Stroke(new Vector2(0,y),new Vector2(AnimalCanvas.Width,y),64,color);}
        IEnumerator Capture(string name)
        {
            // Hidden validation windows have no system framebuffer. Render the real
            // player camera into an explicit target, including its live skinned meshes.
            yield return null;
            string path=Path.Combine(output,name);
            var camera=Camera.main;var previous=RenderTexture.active;var oldTarget=camera.targetTexture;
            var target=new RenderTexture(2560,1440,24){antiAliasing=4};
            var texture=new Texture2D(2560,1440,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,2560,1440),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally{camera.targetTexture=oldTarget;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(texture);}
            Require(File.Exists(path),"Player screenshot missing: "+name);
        }
    }
}
