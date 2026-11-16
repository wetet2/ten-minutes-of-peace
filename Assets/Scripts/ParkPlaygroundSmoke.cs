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
            Require(Mathf.Approximately(ParkCamera.DistanceFromPinch(30,100,150),20)&&
                Mathf.Approximately(ParkCamera.DistanceFromPinch(20,150,100),30)&&
                ParkCamera.DistanceFromPinch(30,100,900)==7&&ParkCamera.DistanceFromPinch(30,900,100)==48,
                "Pinch zoom did not follow the touch span or respect the camera limits.");
            Debug.Log("PINCH ZOOM PASS: zoom in, zoom out and camera limits.");
            var initialFocus=orbit.focus;
            float initialYaw=orbit.yaw,initialPitch=orbit.pitch;
            orbit.ApplyTwoFingerGesture(new Vector2(100,40),900,200,200);
            var movedFocus=orbit.focus;
            Require(Vector3.Distance(movedFocus,initialFocus)>1&&movedFocus.y==initialFocus.y&&
                orbit.distance==initialZoom&&orbit.yaw==initialYaw&&orbit.pitch==initialPitch,
                "Two-finger drag did not pan without changing zoom or rotation.");
            orbit.ApplyTwoFingerGesture(Vector2.zero,900,200,250);
            Require(orbit.focus==movedFocus&&Mathf.Approximately(orbit.distance,initialZoom*.8f),
                "A centered pinch moved the scene or failed to zoom.");
            orbit.ApplyTwoFingerGesture(new Vector2(100000,100000),900,250,200);
            Require(Mathf.Abs(orbit.focus.x)==15&&Mathf.Abs(orbit.focus.z)==12&&Mathf.Approximately(orbit.distance,initialZoom),
                "Combined pinch and pan did not respect the movement limits.");
            orbit.ResetView();
            Debug.Log("TOUCH PAN PASS: two-finger drag, centered pinch, combined pan/zoom and bounds.");
            park.ChooseDrawingMode();
            Require(!park.Lan.BeginJoin("invalid")&&park.Toast.Message!=null,"An invalid IP did not show a toast.");
            yield return new WaitForSecondsRealtime(2.5f);
            Require(park.Toast.Message!=null,"The toast expired early.");
            yield return new WaitForSecondsRealtime(.65f);
            Require(park.Toast.Message==null,"The toast stayed visible after three seconds.");
            park.CloseDialog();
            Debug.Log("TOAST PASS: connection errors appear and clear automatically.");
            park.ShowPark();
            park.OpenSettings();
            var settings=GameObject.Find("Park Settings Popup");
            Require(park.IsSettingsOpen&&settings&&!orbit.InputEnabled,"Settings did not open over the park.");
            var returnButton=settings.transform.Find("시작 화면으로").GetComponent<UnityEngine.UI.Button>();
            returnButton.onClick.Invoke();
            Require(park.IsSelectingMode&&!park.IsSettingsOpen&&!park.Lan.IsHosting,"Settings did not return to the start screen.");
            Debug.Log("PARK SETTINGS PASS: settings opened and its button returned to the start screen.");
            var sunflower=FindAnyObjectByType<RoofSunflowerSpin>();
            Require(sunflower&&GameObject.Find("Candy roof ornaments"),"Roof ornaments missing from the park.");
            foreach(var renderer in sunflower.GetComponentsInChildren<MeshRenderer>())
                Require(!renderer.isPartOfStaticBatch,"The rotating sunflower was statically batched.");
            var startAngle=sunflower.transform.localRotation;
            yield return new WaitForSecondsRealtime(.4f);
            float turn=Quaternion.Angle(startAngle,sunflower.transform.localRotation);
            Require(turn>2f&&turn<12f,"The roof sunflower is not turning slowly.");
            Debug.Log($"ROOF ORNAMENTS PASS: sunflower turned {turn:F1} degrees; candy roof ornaments loaded.");
            var butterflies=FindObjectsByType<ParkButterfly>(FindObjectsSortMode.None);
            Require(butterflies.Length==5,"Expected five butterflies in the park.");
            var butterfly=butterflies[0];
            var wing=butterfly.transform.Find("Right wing hinge");
            Require(wing,"The butterfly has no animated wing.");
            var butterflyStart=butterfly.transform.position;
            var wingStart=wing.localRotation;
            bool wingsMoved=false;
            for(int frame=0;frame<5;frame++)
            {
                yield return new WaitForSecondsRealtime(.05f);
                wingsMoved|=Quaternion.Angle(wingStart,wing.localRotation)>5f;
            }
            foreach(var insect in butterflies)
                foreach(var renderer in insect.GetComponentsInChildren<MeshRenderer>())
                    Require(!renderer.isPartOfStaticBatch,"A butterfly was statically batched.");
            Require(wingsMoved&&Vector3.Distance(butterflyStart,butterfly.transform.position)>.1f,
                "The butterfly did not fly and flap its wings.");
            Debug.Log("BUTTERFLIES PASS: five butterflies fly with moving wings in the park.");
            var fountain=park.Fountain;
            var lawn=GameObject.Find("Meadow island").transform;
            Require(Mathf.Abs(lawn.localScale.x-36)<.01f&&Mathf.Abs(lawn.localScale.z-30)<.01f,
                "The park did not expand by 20% on both axes.");
            Require(fountain&&!park.IsClear(fountain.transform.position,null,.82f),
                "The central fountain is missing or animals can walk through it.");
            var swimmers=new Transform[3];
            var before=new Vector3[swimmers.Length];
            var moving=fountain.transform.Find("Moving water and ducks");
            for(int i=0;i<swimmers.Length;i++)
            {
                swimmers[i]=moving.Find(i==0?"Mother duck path":"Duckling path "+i);
                Require(swimmers[i],"Expected one mother duck and two ducklings.");
                var duck=swimmers[i].Find(i==0?"Mother duck":"Duckling "+i);
                Require(duck&&Mathf.Abs(duck.localScale.x-(i==0?.5f:.305f))<.0001f,"Duck is not half size.");
                before[i]=swimmers[i].localPosition;
                foreach(var renderer in swimmers[i].GetComponentsInChildren<MeshRenderer>())
                    Require(!renderer.isPartOfStaticBatch,"A swimming duck was statically batched.");
            }
            yield return new WaitForSecondsRealtime(.5f);
            for(int i=0;i<swimmers.Length;i++)
            {
                var position=swimmers[i].localPosition;
                Require(Vector3.Distance(position,before[i])>.1f&&
                    Mathf.Abs(position.y-ParkFountain.WaterHeight)<.001f&&new Vector2(position.x,position.z).magnitude<2.1f,
                    "A duck failed to swim on the water inside the fountain.");
            }
            Debug.Log("FOUNTAIN PASS: expanded park, clear walking boundary, and three swimming ducks.");
            park.ShowPark();
            CheckPandazolePlanters();
            yield return Capture("expanded-park.png");
            orbit.enabled=false;
            Camera.main.transform.position=new Vector3(-3.8f,6.5f,-7.3f);
            Camera.main.transform.LookAt(new Vector3(0,.46f,0));
            yield return Capture("duck-fountain.png");
            orbit.enabled=true;
            var regular=ParkFonts.Regular;var bold=ParkFonts.Bold;
            Require(regular&&bold&&regular.name=="BaskinRobbins-Regular"&&bold.name=="BaskinRobbins-Bold","Bundled Baskin Robbins fonts missing");
            foreach(char letter in "애들아놀라빠분만쉰다놀이공간열기동물색칠하기연결앞모습뒷보내기자동원트리케라톱스")
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
            if(park.Venues?.Length>1)
            {
                int forestIndex=Array.FindIndex(park.Venues,venue=>venue.Name=="숲");
                if(forestIndex>=0)
                {
                    park.ChangeVenue(forestIndex);
                    yield return Capture("forest-lawn.png");
                }
                int cafeIndex=Array.FindIndex(park.Venues,venue=>venue.Name=="놀이터");
                Require(cafeIndex>=0,"The kids cafe is missing from the place picker.");
                park.ChangeVenue(cafeIndex);
                yield return CheckKidsCafe(park);
                int desertIndex=Array.FindIndex(park.Venues,venue=>venue.DuneTerrain);
                if(desertIndex>=0)
                {
                    park.ChangeVenue(desertIndex);
                    yield return CheckDesert(park);
                }
                park.ChangeVenue(0);
            }
            park.ShowDrawing();yield return null;
            CheckNewAnimals(park.Drawing);
            CheckContinuousDrawing(park.Drawing);
            CheckPaletteLayout();
            CheckAutoFill(park);
            Paint(park.Drawing.Front,new Color32(245,111,151,255));Paint(park.Drawing.Back,new Color32(245,111,151,255));
            CheckWaveToWalkPose();
            CheckDepartureVariation(park);
            CheckFullSizeEmergence(park);
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
            {
                Require(animal.State==ParkRabbitState.Wandering&&animal.WaveCount==1&&animal.ExitedThroughOpenDoor,"Player arrival/wandering failed");
                Require(Mathf.Abs(animal.transform.localScale.x-.546f)<.0001f,"An outdoor rabbit is not 130% of its previous size.");
            }
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
            if(park.Venues?.Length>1&&park.Venues[1].Name=="숲")
            {
                park.ChangeVenue(1);
                var front=park.Drawing.Front.Paint.EncodeToPNG();
                var back=park.Drawing.Back.Paint.EncodeToPNG();
                int firstArrival=park.Animals.Count;
                Require(park.Houses.Length==1&&park.Houses[0].name=="Baker's house","Forest must have one bakery entrance.");
                var bakery=park.Houses[0];
                var welcome=bakery.ExitPosition(1);
                Require(park.WalkArea.Contains(new Vector2(welcome.x,welcome.z))&&
                    Mathf.Abs(welcome.y-park.SurfaceHeight(welcome))<.001f,
                    "Bakery exit does not end on the walkable lawn.");
                for(int forward=0;forward<=8;forward++)
                for(int side=-2;side<=2;side++)
                {
                    var point=welcome-bakery.transform.forward*(forward*.5f)+bakery.transform.right*(side*.625f);
                    Require(park.WalkArea.Contains(new Vector2(point.x,point.z))&&
                        new Vector2((point.x-park.Venues[1].PondCenter.x)/park.Venues[1].PondRadii.x,
                            (point.z-park.Venues[1].PondCenter.y)/park.Venues[1].PondRadii.y).sqrMagnitude>=1,
                        "The pond obstructs the lawn in front of the bakery.");
                }
                Debug.Log("BAKERY CLEARANCE PASS: the pond is outside the first four units of walkable lawn in front of the door.");
                Require(park.TrySubmit((int)AnimalKind.Rabbit,front,back)&&
                    park.TrySubmit((int)AnimalKind.Bear,front,back),"Forest submission failed.");
                park.ChangeVenue(0);
                Require(park.TrySubmit((int)AnimalKind.Frog,front,back,1),"Second park entrance submission failed.");
                park.Advance(.25f);
                Require(park.Animals.Count==firstArrival+2&&park.PendingCount==1,"Park entrances did not reserve independently.");
                park.ChangeVenue(1);
                Require(park.Animals.Count==firstArrival+1&&park.PendingCount==2,"Two entrance queues did not merge.");
                var finishedBefore=bakery.CompletedExits;
                const float step=.025f;
                var previousPositions=new Vector3[3];
                for(int frame=0;frame<750;frame++)
                {
                    int existing=park.Animals.Count-firstArrival;
                    for(int i=0;i<existing;i++)previousPositions[i]=park.Animals[firstArrival+i].transform.position;
                    park.Advance(step);
                    for(int i=0;i<existing;i++)
                    {
                        var agent=park.Animals[firstArrival+i];
                        if(agent.State!=ParkRabbitState.Wandering)continue;
                        var delta=agent.transform.position-previousPositions[i];
                        Require(delta.magnitude<.65f*step+.001f,"Forest animal jumped after waving: "+delta);
                    }
                }
                for(int i=firstArrival;i<park.Animals.Count;i++)
                    Require(park.Animals[i].State==ParkRabbitState.Wandering&&park.Animals[i].WaveCount==1&&
                        park.Animals[i].HouseIndex==0&&park.Animals[i].ExitedThroughOpenDoor,
                        "An animal did not finish emerging from the bakery.");
                Require(park.Animals.Count==firstArrival+3&&park.PendingCount==0&&bakery.CompletedExits==finishedBefore+3,
                    "A queued animal was lost when switching to the bakery.");
                Debug.Log("BAKERY ARRIVALS PASS: merged both park entrances, all three animals exited through one door and walked from lawn level.");
            }
            CheckAnimalLimit(park);
            CheckCrowdSeparation(park);
            CheckFastPlaygroundArrivals(park);
            if(!failed){Debug.Log("LIVING PARK PLAYER PASS: drawing Apply/cooldown, five independent friends, rendered paint shader, door emergence and bounded wandering.");Application.Quit(0);}
        }
        static void CheckPandazolePlanters()
        {
            var garden=GameObject.Find("Peace Park");
            if(!garden||!garden.transform.Find("Pandazole flower clusters"))return;
            int beds=0;
            foreach(Transform item in garden.transform)
            {
                if(item.name=="Flower bed")
                {
                    var flowers=item.Find("Pandazole planter flowers");
                    Require(flowers&&flowers.childCount==8,"A wooden planter did not receive eight Pandazole flowers.");
                    beds++;
                }
                if(item.name=="Petal"||item.name=="Stem"||item.name=="Flower centre"||item.name=="Flower leaf")
                    Require(!item.gameObject.activeSelf,"A standalone old flower remained by the fence.");
            }
            Require(beds==2,"The two front planters were not replanted.");
            Debug.Log("PANDAZOLE PLANTERS PASS: both beds replanted and the old flowers by the fence hidden.");
        }
        IEnumerator CheckDesert(ParkPlayground park)
        {
            var desert=park.Venues[park.VenueIndex];
            Require(park.ThemeName=="사막"&&desert.Root.activeInHierarchy&&desert.Preview&&park.Houses.Length==2&&
                park.WalkArea.width>park.Venues[0].WalkArea.width*1.5f,
                "The desert did not activate with a larger walking area and two dune entrances.");
            var scenery=desert.Root.transform.Find("Pandazole desert scenery");
            Require(scenery&&scenery.childCount>=70,"The desert is missing Pandazole scenery.");
            var oasis=desert.Root.transform.Find("Desert oasis");
            Require(oasis&&oasis.Find("Oasis palms").childCount==5&&oasis.Find("Oasis shore stones and reeds").childCount>=18,
                "The oasis is missing its palms, rocks or reeds.");
            var water=oasis.Find("Turquoise oasis water");
            var pond=new Vector3(desert.PondCenter.x,0,desert.PondCenter.y);
            Require(water&&park.SurfaceHeight(pond)<water.position.y-.3f&&!park.IsSceneryClear(pond)&&
                !park.IsSceneryClear(pond+Vector3.right*desert.PondRadii.x*.93f)&&
                park.IsSceneryClear(pond+Vector3.back*(desert.PondRadii.y+1.2f)),
                "The oasis is not recessed or its water does not have a clear walking boundary.");
            yield return Capture("desert.png");
            var orbit=park.OrbitCamera;
            orbit.enabled=false;
            Camera.main.transform.position=new Vector3(-4.8f,8.6f,-13.5f);
            Camera.main.transform.LookAt(pond+Vector3.up*.75f);
            yield return Capture("desert-oasis.png");
            orbit.enabled=true;
            Debug.Log("OASIS PASS: recessed water, five palms, shoreline rocks and reeds, and dry access around the pool.");
            var front=park.Drawing.Front.Paint.EncodeToPNG();
            var back=park.Drawing.Back.Paint.EncodeToPNG();
            for(int index=0;index<park.Houses.Length;index++)
            {
                var house=park.Houses[index];
                var sample=new GameObject("Desert animal probe");
                var agent=sample.AddComponent<ParkRabbitAgent>();
                agent.Initialize(park,house,index,0,front,back,false,AnimalKind.Giraffe);
                var camera=Camera.main.transform.position;
                var head=house.Inside.position+Vector3.up*2.5f;
                bool hiddenByDune=false;
                for(int i=1;i<64;i++)
                {
                    var point=Vector3.Lerp(camera,head,i/64f);
                    hiddenByDune|=park.SurfaceHeight(point)>point.y+.05f;
                }
                var landing=house.Welcome.position;
                Require(hiddenByDune&&park.WalkArea.Contains(new Vector2(landing.x,landing.z))&&park.IsSceneryClear(landing),
                    "A dune entrance is exposed or its trailhead is obstructed.");
                Require(house.TryReserve(),"The dune entrance did not reserve a slot.");
                float crest=0;
                bool photographed=false;
                for(int step=0;step<Mathf.CeilToInt((house.ExitDuration+3)/.05f);step++)
                {
                    house.Advance(.05f);
                    agent.Advance(.05f);
                    var position=sample.transform.position;
                    Require(Mathf.Abs(position.y-park.SurfaceHeight(position))<.015f,"The animal left the desert ground.");
                    crest=Mathf.Max(crest,position.y);
                    if(index==0&&!photographed&&agent.Character.Skin.enabled&&position.z<=house.Threshold.position.z)
                    {
                        photographed=true;
                        yield return Capture("desert-arrival.png");
                    }
                }
                Require(crest>4.5f&&agent.WaveCount==1&&agent.State==ParkRabbitState.Wandering&&!house.IsBusy,
                    "The animal did not cross the dune, wave, and continue walking.");
                sample.SetActive(false);
                Destroy(sample);
            }
            Debug.Log("DESERT PASS: both entrances hide the spawn, cross the dune at terrain height, wave, and release the queue.");
        }
        static void CheckCrowdSeparation(ParkPlayground park)
        {
            park.Advance(8f);
            int venueCount=park.Venues?.Length??1;
            for(int venue=0;venue<venueCount;venue++)
            {
                park.ChangeVenue(venue);
                var area=park.WalkArea;
                var corner=new Vector3(area.xMin,0,area.yMin);
                corner.y=park.SurfaceHeight(corner);
                foreach(var animal in park.Animals)
                {
                    Require(animal.State==ParkRabbitState.Wandering,"Crowd probe needs walking animals.");
                    animal.transform.position=corner;
                }
                park.Advance(18f);
                for(int i=0;i<park.Animals.Count;i++)
                {
                    var position=park.Animals[i].transform.position;
                    Require(area.Contains(new Vector2(position.x,position.z))&&park.IsSceneryClear(position),
                        "An animal escaped into the scenery when separating.");
                    for(int j=0;j<i;j++)
                    {
                        var offset=position-park.Animals[j].transform.position;
                        offset.y=0;
                        Require(offset.magnitude>=ParkRabbitAgent.AnimalSpacing-.01f,
                            $"Two animals stayed tangled in {park.ThemeName}: {offset.magnitude:F3}.");
                    }
                }
            }
            Debug.Log("CROWD SEPARATION PASS: 15 animals placed on the same corner spread out in all venues.");
        }
        static void CheckFastPlaygroundArrivals(ParkPlayground park)
        {
            int venue=Array.FindIndex(park.Venues,item=>item.Slide&&item.Houses.Length==2&&item.Houses[1].EntrancePole);
            if(venue<0)return;
            park.ChangeVenue(venue);
            for(int i=0;i<park.Animals.Count;i++)
            {
                // Leave the two arrivals room to start walking while the existing crowd roams.
                var point=new Vector3((i%5-2)*1.3f,0,-7.9f+i/5*1.09f);
                park.Animals[i].transform.position=point;
            }
            var front=park.Drawing.Front.Paint.EncodeToPNG();
            var back=park.Drawing.Back.Paint.EncodeToPNG();
            int firstId=park.SubmissionCount+1;
            Require(park.TrySubmit((int)AnimalKind.Rabbit,front,back,0)&&
                park.TrySubmit((int)AnimalKind.Bear,front,back,1),"Playground submission failed.");

            const int rate=240;
            var walkStart=new[]{-1,-1};
            var startDistance=new float[2];
            var firstSecond=new float[2];
            for(int frame=0;frame<12*rate;frame++)
            {
                park.Advance(1f/rate);
                foreach(var animal in park.Animals)
                {
                    int index=animal.Id-firstId;
                    if(index<0||index>=walkStart.Length||animal.State!=ParkRabbitState.Wandering)continue;
                    if(walkStart[index]<0)
                    {
                        walkStart[index]=frame;
                        startDistance[index]=animal.WalkedDistance;
                        Require(animal.HouseIndex==index&&animal.WaveCount==1&&animal.ExitedThroughOpenDoor,
                            "The playground animal did not finish its entrance and wave.");
                    }
                    if(frame==walkStart[index]+rate)firstSecond[index]=animal.WalkedDistance-startDistance[index];
                }
            }
            Require(firstSecond[0]>.27f&&firstSecond[1]>.27f,
                $"Both playground animals must start walking at {rate} Hz after waving. Slide={firstSecond[0]:F3}, pole={firstSecond[1]:F3}.");
            Debug.Log($"PLAYGROUND DEPARTURE PASS: first second after waving at {rate} Hz. Slide={firstSecond[0]:F3}, pole={firstSecond[1]:F3}.");
        }

        IEnumerator CheckKidsCafe(ParkPlayground park)
        {
            var cafe=park.Venues[park.VenueIndex];
            Require(park.ThemeName=="놀이터"&&cafe.Root.activeInHierarchy&&cafe.Preview&&park.Houses.Length==2,
                "Playground did not activate with a preview and two entrances.");
            var room=cafe.Root.transform;
            var roundabout=room.Find("Roundabout");
            var platform=room.Find("Roundabout/Rotating platform");
            var trampoline=room.Find("Trampoline");
            var seesaw=room.Find("Seesaw");
            Require(roundabout&&platform&&trampoline&&seesaw&&!room.Find("Guardian table and chairs")&&
                trampoline.localPosition.x<0&&seesaw.localPosition.x>0&&
                trampoline.localPosition.z<0&&seesaw.localPosition.z<0&&
                !park.IsSceneryClear(roundabout.position+Vector3.back*3f)&&
                park.IsSceneryClear(roundabout.position+Vector3.back*3.8f)&&
                !park.IsSceneryClear(trampoline.position)&&!park.IsSceneryClear(seesaw.position)&&
                park.IsSceneryClear(new Vector3(0,0,-5.5f)),
                "Playground equipment or the walkable aisle around it is missing.");
            var initialPlatformRotation=platform.localRotation;
            yield return new WaitForSeconds(.25f);
            Require(Quaternion.Angle(initialPlatformRotation,platform.localRotation)>1f,
                "The roundabout platform did not rotate.");
            var floor=room.Find("Rectangular play floor").GetComponent<MeshRenderer>().bounds;
            var meadow=room.Find("Continuous terrace meadow").GetComponent<MeshRenderer>().bounds;
            var neighborhood=room.Find("Playground neighborhood");
            Require(floor.size.x*floor.size.z>445&&park.WalkArea.width>23&&park.WalkArea.height>15,
                "The enlarged floor or walk area is missing.");
            Require(Mathf.Abs(floor.max.y-meadow.max.y)<.001f&&meadow.size.x>300&&meadow.size.z>300&&
                room.Find("Low rails and flowerbeds")&&room.Find("Distant playground hills")&&neighborhood&&
                !room.Find("Wooded terrace landscape")&&!room.Find("Rear cafe pavilion")&&
                !room.Find("Left wall")&&!room.Find("Back wall sill")&&park.IsSceneryClear(new Vector3(-5.4f,0,7.5f)),
                "The cafe is not open to the continuous meadow.");
            var buildings=neighborhood.Find("Low village buildings");
            Require(buildings&&buildings.childCount==3&&neighborhood.Find("Rear pedestrian lane"),
                "The street must have two houses and an ice cream shop.");
            foreach(string name in new[]{"Peach house","Sage house","Ice cream shop"})
            {
                var building=buildings.Find(name);
                Require(building&&building.position.z>floor.max.z+3,
                    "A village building is missing or extends into the playground.");
                foreach(var renderer in building.GetComponentsInChildren<MeshRenderer>())
                    Require(renderer.bounds.max.y<5&&renderer.bounds.min.z>floor.max.z,
                        "A village building is too tall or too close to the playground.");
            }
            Require(buildings.Find("Ice cream shop/Ice cream cone sign"),"The ice cream shop has no visual sign.");
            foreach(string name in new[]{"Left sidewalk","Right sidewalk"})
            {
                var sidewalk=neighborhood.Find(name);
                Require(sidewalk&&Mathf.Abs(sidewalk.position.x)>floor.max.x&&sidewalk.Find("Sandstone sidewalk")&&
                    sidewalk.Find("Honey timber")&&sidewalk.Find("Jade lamp posts")&&sidewalk.Find("Street tree light green"),
                    "A sidewalk is missing its pavement, bench, streetlight, or trees.");
            }
            var lanes=neighborhood.Find("Curved village lanes");
            Require(lanes&&lanes.Find("Asphalt")&&lanes.Find("Front sidewalk link")&&
                lanes.Find("Lane toward distant houses/Asphalt")&&lanes.Find("Lane toward the next block/Asphalt"),
                "The neighborhood sidewalks do not join the loop road.");
            var road=lanes.Find("Asphalt").GetComponent<MeshRenderer>().bounds;
            Require(road.size.x>44&&road.size.z>42&&road.max.y<.10f,"The lane does not wrap around the block at ground level.");
            foreach(string name in new[]{"Left front crossing","Right front crossing","Left rear crossing","Right rear crossing"})
            {
                var crossing=lanes.Find(name);
                Require(crossing&&crossing.Find("Warm ivory trim")&&crossing.Find("Sandstone sidewalk")&&
                    Mathf.Abs(crossing.position.x)>floor.max.x&&Mathf.Abs(crossing.position.z)>floor.max.z,
                    "A sidewalk end does not have a crossing and a landing.");
            }
            var yards=neighborhood.Find("Fenced backyards");
            Require(yards&&yards.childCount==2,"The houses do not both have a garden.");
            foreach(Transform yard in yards)
                Require(yard.Find("Low garden fence")&&yard.Find("Vegetable beds")&&
                    yard.position.z>buildings.position.z+20,"A backyard is missing its enclosure or planting.");
            var roofs=neighborhood.Find("Distant village roofs");
            Require(roofs&&roofs.childCount==4,"Distant village roofs are missing.");
            Debug.Log("PLAYGROUND NEIGHBORHOOD PASS: curved lane with four crossings, two fenced gardens, and four distant roofs.");
            var ride=cafe.Slide;
            var door=park.Houses[0];
            var supports=ride.transform.Find("Lookout supports");
            Require(supports&&supports.childCount==2,"The lookout must have two ground support posts.");
            foreach(Transform support in supports)
            {
                var bounds=support.Find("Post").GetComponent<MeshRenderer>().bounds;
                Require(Mathf.Abs(bounds.min.y)<.03f&&bounds.max.y>door.Threshold.position.y-.30f,
                    "The lookout post does not reach from the ground to the deck.");
                var foot=new Vector2(support.localPosition.x,support.localPosition.z);
                foreach(var point in ride.Chute)
                    Require(Vector2.Distance(foot,new Vector2(point.x,point.z))>1.36f,
                        "A support post crosses the spiral chute.");
                foreach(var point in ride.Steps)
                    Require(Vector2.Distance(foot,new Vector2(point.x,point.z))>.68f||
                        point.y>bounds.max.y+.10f,"A support post crosses the climb path.");
            }
            ride.SlideDown(0,out var lip,out var startFacing);
            ride.SlideDown(1,out var bottom,out _);
            Require(door.EntranceSlide==ride&&ride.Entrance==door&&lip.y>6&&
                Mathf.Abs(door.Threshold.position.y-lip.y)<.001f&&Mathf.Abs(bottom.y)<.001f&&
                Vector3.Distance(ride.Exit,door.Welcome.position)<.001f&&park.IsSceneryClear(ride.Exit),
                "The lookout door, chute or floor-level exit is disconnected.");
            float turn=0;
            var previousFacing=startFacing;
            var previousPoint=lip;
            for(int i=1;i<=100;i++)
            {
                ride.SlideDown(i/100f,out var point,out var facing);
                turn+=Quaternion.Angle(previousFacing,facing);
                Require(point.y<=previousPoint.y+.001f&&Vector3.Distance(point,previousPoint)<.6f,
                    "The spiral path jumps or rises.");
                previousPoint=point;
                previousFacing=facing;
            }
            Require(turn>350&&turn<370,"The slide does not make one complete turn.");
            Debug.Log("CAFE SPIRAL PASS: raised doorway, continuous turn and clear floor-level runout.");
            var sample=new GameObject("Kids cafe animal probe");
            var agent=sample.AddComponent<ParkRabbitAgent>();
            Paint(park.Drawing.Front,new Color32(251,225,124,255));
            Paint(park.Drawing.Back,new Color32(247,172,132,255));
            agent.Initialize(park,door,0,0,park.Drawing.Front.Paint.EncodeToPNG(),park.Drawing.Back.Paint.EncodeToPNG());
            agent.UseContactShadow=true;
            int exits=door.CompletedExits;
            Require(door.TryReserve(),"Kids cafe door did not open.");
            yield return null;
            Require(!sample.transform.Find("Animal contact shadow")&&!agent.Character.Skin.enabled,
                "The mobile shadow was initialized before the animal started to emerge.");
            bool sawDoorway=false,sawDescent=false;
            for(int i=0;i<185;i++)
            {
                door.Advance(.05f);
                agent.Advance(.05f);
                var position=sample.transform.position;
                if(agent.State==ParkRabbitState.WalkingOut&&position.y>6)
                {
                    sawDoorway=true;
                    Require(door.DoorFraction==1&&agent.Character.Skin.enabled&&!ride.Available,
                        "The animal was hidden, the door was closed, or the chute was available during emergence.");
                }
                if(agent.State==ParkRabbitState.WalkingOut&&position.y>1&&position.y<3.5f&&!sawDescent)
                {
                    sawDescent=true;
                    yield return Capture("kids-cafe-entrance-ride.png");
                }
                if(agent.State==ParkRabbitState.Waving)
                    Require(sawDoorway&&sawDescent&&Mathf.Abs(position.y)<.001f&&
                        Vector3.Distance(position,ride.Exit)<.08f,
                        "The animal waved before reaching the bottom of the spiral.");
            }
            Require(agent.ExitedThroughOpenDoor&&agent.WaveCount==1&&agent.State==ParkRabbitState.Wandering&&
                agent.Character.Skin.enabled&&door.CompletedExits==exits+1&&door.DoorFraction==0&&!door.IsBusy,
                "The mobile animal did not appear, exit, wave and release the closed door.");
            float travelled=0;
            // Reserve the slide before the roaming animal's first random activity is due.
            for(int i=0;i<120;i++)
            {
                var before=sample.transform.position;
                agent.Advance(.05f);
                var after=sample.transform.position;
                travelled+=Vector3.Distance(before,after);
                Require(park.IsSceneryClear(after)&&park.WalkArea.Contains(new Vector2(after.x,after.z)),
                    "The cafe animal walked into equipment or outside the room.");
            }
            Require(travelled>2,"The cafe animal did not walk away from the entrance.");
            yield return Capture("kids-cafe.png");
            var orbit=park.OrbitCamera;
            orbit.enabled=false;
            Camera.main.transform.position=new Vector3(25f,16f,-13f);
            Camera.main.transform.LookAt(new Vector3(0,1.7f,1.3f));
            yield return Capture("kids-cafe-side.png");
            Camera.main.transform.position=new Vector3(0,29f,13.5f);
            Camera.main.transform.LookAt(new Vector3(0,.2f,23.6f));
            yield return Capture("playground-backyard-gardens.png");
            orbit.enabled=true;
            var shadow=sample.transform.Find("Animal contact shadow").GetComponent<MeshRenderer>();
            Require(shadow.enabled&&shadow.sharedMaterial.shader.isSupported&&
                agent.Character.Skin.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off&&
                shadow.GetComponent<MeshFilter>().sharedMesh.vertexCount==4&&!shadow.GetComponent<Collider>()&&
                Vector3.Distance(shadow.transform.position,sample.transform.position+Vector3.up*.012f)<.001f,
                "The mobile contact shadow did not follow the walking animal.");
            Debug.Log("CONTACT SHADOW PASS: animal appears and releases the closed door, and its late-created shadow follows it without a collider.");
            Require(ride&&ride.Available,"The cafe slide is not available.");
            sample.transform.SetPositionAndRotation(ride.Approach+Vector3.forward*.50f,ride.transform.rotation);
            Require(agent.TryStartSlide()&&!ride.Available,"The animal did not reserve the slide.");
            Require(!ride.Reserve(agent)&&!door.TryReserve(),"The occupied slide accepted another reservation or entrance.");
            var stages=new System.Collections.Generic.HashSet<ParkRabbitAgent.Activity>();
            for(int i=0;i<350&&agent.CurrentActivity!=ParkRabbitAgent.Activity.None;i++)
            {
                agent.Advance(.05f);
                var activity=agent.CurrentActivity;
                bool firstFrame=stages.Add(activity);
                if(activity==ParkRabbitAgent.Activity.Climb&&firstFrame)Require(sample.transform.position.y<.1f,"Stairs began above floor level.");
                if(activity==ParkRabbitAgent.Activity.Sit&&firstFrame)Require(sample.transform.position.y>6f,"The animal did not reach the platform.");
                if(activity==ParkRabbitAgent.Activity.Slide&&agent.transform.position.y<3.5f&&firstFrame==false&&
                    !stages.Contains(ParkRabbitAgent.Activity.LeaveSlide))
                {
                    yield return Capture("kids-cafe-slide.png");
                    break;
                }
            }
            for(int i=0;i<130&&agent.CurrentActivity!=ParkRabbitAgent.Activity.None;i++)
            {agent.Advance(.05f);stages.Add(agent.CurrentActivity);}
            Require(stages.Contains(ParkRabbitAgent.Activity.Climb)&&stages.Contains(ParkRabbitAgent.Activity.Sit)&&
                stages.Contains(ParkRabbitAgent.Activity.Slide)&&stages.Contains(ParkRabbitAgent.Activity.LeaveSlide)&&
                agent.CurrentActivity==ParkRabbitAgent.Activity.None&&ride.Available&&park.IsSceneryClear(sample.transform.position)&&
                Vector3.Distance(sample.transform.position,ride.Exit)<.08f,"The animal did not finish the slide and return to the floor.");
            foreach(var activity in new[]{ParkRabbitAgent.Activity.Hop,ParkRabbitAgent.Activity.Dance})
            {
                var position=sample.transform.position;
                agent.StartActivity(activity);
                agent.Advance(.35f);
                Require(agent.CurrentActivity==activity&&sample.transform.position==position,"An activity moved the animal off its spot.");
                agent.Advance(2.2f);
                Require(agent.CurrentActivity==ParkRabbitAgent.Activity.None,"An activity did not return to wandering.");
            }
            Debug.Log("CAFE ACTIVITIES PASS: stairs, sit, slide, walk out, hop and dance.");
            Destroy(sample);
            yield return CheckPoleEntrance(park);
            Debug.Log("PLAYGROUND PASS: two animated entrances, wave, grounded wandering and equipment avoidance.");
        }
        IEnumerator CheckPoleEntrance(ParkPlayground park)
        {
            var gate=park.Houses[1];
            var pole=gate.EntrancePole;
            Require(pole&&pole.position.x<0&&pole.position.z>0&&gate.Threshold.position.y>5&&
                park.IsSceneryClear(gate.Welcome.position)&&!park.IsSceneryClear(pole.position)&&
                !park.Venues[park.VenueIndex].Root.transform.Find("Soft climbing toys"),
                "The pole tower did not replace the blocks in the rear corner.");
            var sample=new GameObject("Pole entrance animal probe");
            var agent=sample.AddComponent<ParkRabbitAgent>();
            agent.Initialize(park,gate,1,0,park.Drawing.Front.Paint.EncodeToPNG(),park.Drawing.Back.Paint.EncodeToPNG());
            Require(park.Houses[0].TryReserve()&&gate.TryReserve(),"The two entrances could not reserve independently.");
            park.Houses[0].CloseAfterExit();
            bool captured=false,groundedWave=false;
            float rotation=0;
            for(int i=0;i<190;i++)
            {
                var previous=sample.transform.position;
                park.Houses[0].Advance(.05f);
                gate.Advance(.05f);
                agent.Advance(.05f);
                var position=sample.transform.position;
                if(agent.State==ParkRabbitState.WalkingOut&&position.y>pole.position.y+.001f&&position.y<previous.y)
                {
                    var radius=position-pole.position;radius.y=0;
                    var previousRadius=previous-pole.position;previousRadius.y=0;
                    float step=Vector3.SignedAngle(previousRadius,radius,Vector3.up);
                    rotation+=step;
                    Require(gate.DoorFraction==1&&agent.Character.Skin.enabled&&step>=-.01f&&step<18&&
                        Mathf.Abs(radius.magnitude-.42f)<.002f&&Vector3.Dot(sample.transform.forward,radius.normalized)>.999f,
                        "The animal did not turn smoothly around the pole while facing it.");
                    var bones=agent.Character.Skin.bones;
                    for(int side=-1;side<=1;side+=2)
                    {
                        var hand=bones[side<0?2:3].TransformPoint(new Vector3(side*.78f,-.32f,-.02f));
                        var offset=hand-pole.position;offset.y=0;
                        Require(offset.sqrMagnitude<.18f*.18f,"A hand did not grip the pole.");
                    }
                    if(!captured&&rotation>=75)
                    {
                        captured=true;
                        var orbit=park.OrbitCamera;
                        orbit.enabled=false;
                        Camera.main.transform.position=pole.position+new Vector3(-5.4f,5.6f,-8.5f);
                        Camera.main.transform.LookAt(pole.position+Vector3.up*4.15f);
                        yield return Capture("playground-pole-arrival.png");
                        orbit.enabled=true;
                        orbit.ResetView();
                    }
                }
                if(agent.State==ParkRabbitState.Waving)
                {
                    groundedWave=true;
                    Require(rotation>355&&rotation<365&&Vector3.Distance(position,gate.Welcome.position)<.01f,
                        "The animal waved before completing one turn and reaching the pole landing.");
                }
            }
            Require(groundedWave&&agent.State==ParkRabbitState.Wandering&&agent.WaveCount==1&&
                agent.ExitedThroughOpenDoor&&!gate.IsBusy&&gate.DoorFraction==0,
                "The pole animal did not wave, roam and release the entrance.");
            Destroy(sample);
            Debug.Log("POLE ARRIVAL PASS: one complete turn with both hands gripping, ground-level wave and released queue.");
        }

        static void CheckAnimalLimit(ParkPlayground park)
        {
            var drawing=park.Drawing;
            var front=drawing.Front.Paint.GetRawTextureData();
            var back=drawing.Back.Paint.GetRawTextureData();
            while(park.Animals.Count<15)
            {
                Require(park.SubmitLan(AnimalKind.Rabbit,front,back)==ParkLan.Accepted,"Animal rejected before the park was full.");
                park.Advance(7f);
            }
            var oldest=park.Animals[0];
            foreach(var animal in park.Animals)
                if(animal.Id<oldest.Id)oldest=animal;
            var visible=oldest.Character.Skin.sharedMaterial;
            Require(!visible.IsKeywordEnabled("PEACE_PARK_ANIMAL_FADING"),"A walking animal used the fade shader.");
            Require(park.SubmitLan(AnimalKind.Rabbit,front,back)==ParkLan.Accepted,"The 16th animal was rejected.");
            int newestId=park.SubmissionCount;
            park.Advance(.6f);
            var properties=new MaterialPropertyBlock();
            oldest.Character.Skin.GetPropertyBlock(properties);
            float opacity=properties.GetFloat("_AnimalVisibility");
            Require(park.Animals.Count==15&&oldest.gameObject.activeSelf&&opacity>0&&opacity<1&&
                visible.IsKeywordEnabled("PEACE_PARK_ANIMAL_FADING"),
                "The oldest animal did not visibly fade while the replacement waited.");
            park.Advance(1f);
            bool added=false;
            foreach(var animal in park.Animals)added|=animal.Id==newestId;
            Require(park.Animals.Count==15&&!oldest.gameObject.activeSelf&&added&&park.PendingCount==0,
                "The 16th animal did not replace the oldest.");
            Debug.Log("ANIMAL LIMIT PASS: 16th LAN submission fades out the oldest and maintains the cap of 15.");
        }
        static void Paint(AnimalCanvas canvas,Color32 color)
        {for(float y=0;y<AnimalCanvas.Height+64;y+=96)canvas.Stroke(new Vector2(0,y),new Vector2(AnimalCanvas.Width,y),64,color);}
        static void CheckNewAnimals(DrawingPage drawing)
        {
            const float tabWidth=160;
            var area=new Rect(20,10,1120,157);
            float maxScroll=tabWidth*AnimalKinds.Count-area.width;
            void Tab(EventType type,float x)=>Require(drawing.HandleTabGesture(
                type,0,new Vector2(x,65),Vector2.zero,area,tabWidth,0,maxScroll),"Picker did not handle "+type);
            int original=drawing.SelectedAnimal;
            Tab(EventType.MouseDown,1100);
            Tab(EventType.MouseDrag,590);
            Tab(EventType.MouseUp,590);
            Require(drawing.SelectedAnimal==original,"Swiping the animal picker also selected a card.");
            for(int card=0;card<3;card++)
            {
                float x=area.xMax-tabWidth*(card+.5f);
                Tab(EventType.MouseDown,x);
                Tab(EventType.MouseUp,x);
                var kind=(AnimalKind)((int)AnimalKind.Gorilla-card);
                Require(drawing.SelectedAnimal==(int)kind,$"The scrolled animal card did not select {kind}. Selected {drawing.SelectedAnimal}, locked {drawing.LockedAnimal}, sending {drawing.Sending}.");
                Require(Resources.Load<Texture2D>("Drawing/"+kind+"Head"),"Missing animal icon for "+kind);
                int pixelX=AnimalCanvas.Width/2,pixelY=AnimalCanvas.Height/2;
                var point=new Vector2(pixelX,pixelY);
                Require(drawing.Front.IsInside(pixelX,pixelY)&&drawing.Back.IsInside(pixelX,pixelY),"Missing coloring area for "+kind);
                var front=new Color32(37,169,211,255);
                var back=new Color32(217,90,71,255);
                drawing.Front.Stroke(point,point,24,front);
                drawing.Back.Stroke(point,point,24,back);
                var sample=new GameObject(kind+" coloring probe");
                var model=sample.AddComponent<RabbitCharacter>();
                model.EnsureBuilt(kind);
                model.ApplyPainting(drawing.Front.Paint.EncodeToPNG(),drawing.Back.Paint.EncodeToPNG());
                var material=model.Skin.sharedMaterials[0];
                var frontTexture=(Texture2D)material.GetTexture("_FrontTex");
                var backTexture=(Texture2D)material.GetTexture("_BackTex");
                Require(model.Kind==kind&&model.SkinVertexCount>0&&
                    ((Color32)frontTexture.GetPixel(pixelX,pixelY)).Equals(front)&&
                    ((Color32)backTexture.GetPixel(pixelX,pixelY)).Equals(back),"The 3D model lost the selected animal or its painting: "+kind);
                sample.SetActive(false);
                Destroy(sample);
            }
            Tab(EventType.MouseDown,50);
            Tab(EventType.MouseDrag,560);
            Tab(EventType.MouseUp,560);
            drawing.SelectAnimal(original);
            Debug.Log("NEW ANIMALS PASS: horizontally scrolled to crocodile, hippo and gorilla, loaded icons and transferred separate front/back paint to each 3D model.");
        }
        static void CheckWaveToWalkPose()
        {
            var sample=new GameObject("Wave to walk pose probe");
            var character=sample.AddComponent<RabbitCharacter>();
            var baked=new Mesh();
            try
            {
                character.EnsureBuilt();
                character.Grounded=true;
                const float phase=.7f;
                character.PlayWavePose(1);
                character.Skin.BakeMesh(baked,false);
                var endOfWave=baked.vertices;
                character.PlayWalkingPose(phase,0);
                character.Skin.BakeMesh(baked,false);
                var firstWalk=baked.vertices;
                for(int i=0;i<firstWalk.Length;i++)
                    Require((endOfWave[i]-firstWalk[i]).sqrMagnitude<.000001f,"The wave and walking poses snap at the blend boundary.");
                Debug.Log("WAVE TRANSITION PASS: the lowered-arm pose matches the first walking pose.");
            }
            finally{Destroy(baked);Destroy(sample);}
        }
        static void CheckContinuousDrawing(DrawingPage drawing)
        {
            var front=new Rect(50,100,AnimalCanvas.Width,AnimalCanvas.Height);
            var back=new Rect(front.xMax+80,front.y,front.width,front.height);
            int x=AnimalCanvas.Width/2,y=AnimalCanvas.Height/2;
            Require(drawing.Front.IsInside(x,y)&&drawing.Back.IsInside(x,y),"Drawing probe missed the silhouette.");
            var frontCenter=new Vector2(front.xMin+x,front.yMax-y);
            var backCenter=new Vector2(back.xMin+x,back.yMax-y);
            Color frontBefore=drawing.Front.Paint.GetPixel(x,y),backBefore=drawing.Back.Paint.GetPixel(x,y);
            void Send(EventType type,Vector2 position)=>drawing.HandlePaint(
                new Event{type=type,button=0,mousePosition=position},front,back);

            Send(EventType.MouseDown,new Vector2(front.xMin-25,frontCenter.y));
            Send(EventType.MouseDrag,frontCenter);
            Require(drawing.Front.Paint.GetPixel(x,y)!=frontBefore,"Stroke did not enter the front canvas from outside.");
            Send(EventType.MouseDrag,backCenter);
            Require(drawing.Back.Paint.GetPixel(x,y)!=backBefore,"Stroke did not continue into the back canvas.");
            var outsideFront=drawing.Front.Paint.GetPixel(0,y);
            Require(!drawing.Front.IsInside(0,y)&&Mathf.Approximately(outsideFront.a,0),"Stroke escaped the silhouette.");
            Send(EventType.MouseUp,backCenter);

            drawing.SelectAnimal(drawing.SelectedAnimal);
            Send(EventType.MouseDown,backCenter);
            Send(EventType.MouseDrag,frontCenter);
            Require(drawing.Front.Paint.GetPixel(x,y)!=frontBefore&&drawing.Back.Paint.GetPixel(x,y)!=backBefore,
                "Stroke did not continue from the back canvas to the front.");
            Send(EventType.MouseUp,frontCenter);
            drawing.SelectAnimal(drawing.SelectedAnimal);
            Debug.Log("CONTINUOUS DRAWING PASS: outside-to-inside and both directions across the canvas gap.");
        }
        static void CheckPaletteLayout()
        {
            Require(DrawingPage.PaletteColorCount==12,"The drawing palette must display twelve colors.");
            foreach(float width in new[]{98f,152f,218f})
            {
                var area=new Rect(26,197,width,585);
                var first=DrawingPage.PaletteButtonRect(area,0);
                float columnGap=DrawingPage.PaletteButtonRect(area,1).xMin-first.xMax;
                Require(columnGap>8&&first.width<90,"Palette circles have no suitable gap or size.");
                for(int i=0;i<DrawingPage.PaletteColorCount;i++)
                {
                    var rect=DrawingPage.PaletteButtonRect(area,i);
                    Require(Mathf.Approximately(rect.width,first.width)&&Mathf.Approximately(rect.width,rect.height)&&
                        rect.xMin>=area.xMin-.001f&&rect.xMax<=area.xMax+.001f&&
                        rect.yMin>=area.yMin&&rect.yMax<=area.yMax,"A palette circle extends past the available area.");
                    if(i>=2)
                    {
                        var previous=DrawingPage.PaletteButtonRect(area,i-2);
                        Require(Mathf.Abs(rect.yMin-previous.yMax-columnGap)<.001f,
                            "The rows and columns of the palette should have equal gaps.");
                    }
                }
            }
            Debug.Log("COLOR PALETTE PASS: twelve evenly spaced circles fit in narrow and wide drawing panels.");
        }

        static void CheckAutoFill(ParkPlayground park)
        {
            var drawing=park.Drawing;
            int submissions=park.SubmissionCount;
            int x=AnimalCanvas.Width/2,y=AnimalCanvas.Height/2;
            Color32 before=drawing.Front.Paint.GetPixel(x,y);
            var random=UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(24);
                drawing.AutoFill();
                Color32 first=drawing.Front.Paint.GetPixel(x,y);
                Require(!first.Equals(before)&&((Color32)drawing.Back.Paint.GetPixel(x,y)).Equals(first)&&
                    first.a==255&&drawing.Front.Paint.GetPixel(0,y).a==0&&drawing.Back.Paint.GetPixel(0,y).a==0,
                    "Auto fill must color both silhouettes and preserve transparent canvas margins.");
                drawing.AutoFill();
                Color32 second=drawing.Front.Paint.GetPixel(x,y);
                Require(!second.Equals(first)&&((Color32)drawing.Back.Paint.GetPixel(x,y)).Equals(second)&&
                    park.SubmissionCount==submissions&&park.IsDrawing,"Auto fill must change the color without submitting.");
            }
            finally{UnityEngine.Random.state=random;}
            Debug.Log("AUTO FILL PASS: two different colors cover both silhouettes without painting the margins or sending.");
        }

        static void CheckDepartureVariation(ParkPlayground park)
        {
            var front=park.Drawing.Front.Paint.EncodeToPNG();
            var back=park.Drawing.Back.Paint.EncodeToPNG();
            int venueCount=park.Venues?.Length??1;
            for(int venue=0;venue<venueCount;venue++)
            {
                park.ChangeVenue(venue);
                foreach(var house in park.Houses)
                {
                    int last=-1;
                    for(int attempt=0;attempt<4;attempt++)
                    {
                        var sample=new GameObject("Departure direction probe");
                        var agent=sample.AddComponent<ParkRabbitAgent>();
                        agent.Initialize(park,house,0,attempt,front,back);
                        Require(house.TryReserve(),"Departure probe could not open the door.");
                        var welcome=house.Welcome.position;
                        bool checkedFirstStep=false;
                        Require(park.WalkArea.Contains(new Vector2(welcome.x,welcome.z)),
                            $"The welcome point at {house.name} is outside the walking area.");
                        for(int step=0;step<Mathf.CeilToInt((house.ExitDuration+5.5f)/.05f);step++)
                        {
                            bool waving=agent.State==ParkRabbitState.Waving;
                            bool walking=agent.State==ParkRabbitState.Wandering;
                            var previous=sample.transform.position;
                            house.Advance(.05f);
                            agent.Advance(.05f);
                            var motion=sample.transform.position-previous;
                            motion.y=0;
                            if(waving)Require(motion.sqrMagnitude<.000001f,"The animal moved before lowering its arm.");
                            if(walking&&!checkedFirstStep)
                            {
                                Require(motion.sqrMagnitude>.000001f,$"The animal waited after waving at {house.name}.");
                                checkedFirstStep=true;
                            }
                            if(agent.State==ParkRabbitState.Wandering)
                                Require(motion.magnitude<=.65f*.05f+.001f,"The first steps exceeded ordinary walking speed.");
                        }
                        int chosen=house.LastDepartureDirection;
                        var moved=sample.transform.position-welcome;
                        moved.y=0;
                        Require(checkedFirstStep&&chosen>=0&&chosen!=last&&moved.magnitude>.6f,
                            $"The animal did not walk in a different direction after waving at {house.name}.");
                        last=chosen;
                        sample.SetActive(false);
                        Destroy(sample);
                    }
                }
            }
            park.ChangeVenue(0);
            Debug.Log("DEPARTURE DIRECTIONS PASS: consecutive animals walk in different directions after waving at each entrance.");
        }
        static void CheckFullSizeEmergence(ParkPlayground park)
        {
            var front=park.Drawing.Front.Paint.EncodeToPNG();
            var back=park.Drawing.Back.Paint.EncodeToPNG();
            var venues=park.Venues??new[]{new ParkVenue{Houses=park.Houses}};
            foreach(var venue in venues)
            foreach(var house in venue.Houses)
            {
                if(house.ExitTerrain||house.EntranceSlide||house.EntrancePole)continue;
                float authoredScale=house.name=="Baker's house"?.94f:1f;
                Require(Vector3.Distance(house.transform.localScale,Vector3.one*(authoredScale*1.1f))<.001f,
                    $"House {house.name} is not uniformly scaled to 110%.");
            }
            var checkedSkins=new System.Collections.Generic.HashSet<AnimalKind>();
            foreach(var venue in venues)
            foreach(var house in venue.Houses)
            for(int i=0;i<AnimalKinds.Count;i++)
            {
                var sample=new GameObject("Door clearance probe");
                var agent=sample.AddComponent<ParkRabbitAgent>();
                agent.Initialize(park,house,0,0,front,back,false,(AnimalKind)i);
                Require(agent.Character.Skin.sharedMesh.indexFormat==UnityEngine.Rendering.IndexFormat.UInt16,
                    "Animal exceeded the mobile skin index budget: "+(AnimalKind)i);
                if(checkedSkins.Add((AnimalKind)i))CheckSkinWeights(agent.Character);
                float scale=sample.transform.localScale.x;
                var kind=(AnimalKind)i;
                float original=kind==AnimalKind.Giraffe?.40f:kind==AnimalKind.Elephant?.39f:.42f;
                Require(Mathf.Abs(scale-original*1.3f)<.0001f,"An animal starts its entrance at a reduced scale.");
                Require(house.TryReserve(),"The door did not open for the full-size animal.");
                for(int step=0;step<Mathf.CeilToInt((house.ExitDuration+4f)/.05f);step++)
                {
                    house.Advance(.05f);
                    agent.Advance(.05f);
                    Require(Mathf.Abs(sample.transform.localScale.x-scale)<.0001f,"Animal scale changed during emergence.");
                }
                Require(agent.ExitedThroughOpenDoor&&agent.WaveCount==1,"The full-size animal did not finish its entrance.");
                Destroy(sample);
            }
            Debug.Log("FULL SIZE EMERGENCE PASS: every animal exits at 130% scale with uniformly enlarged 110% houses.");
            Debug.Log("SKIN WEIGHTS PASS: all species have sorted, normalized influences and rigid head vertices are bound to the head bone.");
        }
        static void CheckSkinWeights(RabbitCharacter character)
        {
            var mesh=character.Skin.sharedMesh;
            var vertices=mesh.vertices;
            var weights=mesh.boneWeights;
            int headVertices=0;
            for(int i=0;i<character.SkinVertexCount;i++)
            {
                var w=weights[i];
                if(w.weight0<w.weight1||w.weight1<w.weight2||w.weight2<w.weight3||w.weight3<-.00001f||
                    Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.0001f)
                    throw new Exception($"Invalid skin weights for {character.Kind} at vertex {i}.");
                if(vertices[i].y<2.6f)continue;
                if(w.boneIndex0!=1||w.weight0<.9999f)
                    throw new Exception($"A rigid head vertex of {character.Kind} lost its primary influence.");
                headVertices++;
            }
            Require(headVertices>0,"Head vertex test found no head for "+character.Kind);
        }
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
