using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

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
            yield return CheckStartVenueLayout(park);
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
            Require(orbit.transform.position.sqrMagnitude<.000001f&&orbit.distance==15,
                "Starting a camera reset jumped to the default view.");
            orbit.AdvanceAnimalFollow(.45f);
            Require(Vector3.Distance(orbit.transform.position,initialPosition)>.01f&&
                orbit.transform.position.sqrMagnitude>.01f&&
                Mathf.Approximately(orbit.distance,(15+initialZoom)*.5f),
                "Reset did not move and zoom through an intermediate view.");
            orbit.ResetView();
            orbit.AdvanceAnimalFollow(.5f);
            Require(Vector3.Distance(orbit.transform.position,initialPosition)<.0001f&&
                Quaternion.Angle(orbit.transform.rotation,initialRotation)<.01f&&orbit.distance==initialZoom,
                "Camera reset did not restore the scene's initial position, angle and zoom.");
            Debug.Log("CAMERA RESET PASS: eased from the displayed view to the venue's initial position, angle and zoom.");
            Require(Mathf.Approximately(ParkCamera.DistanceFromPinch(30,100,150),20)&&
                Mathf.Approximately(ParkCamera.DistanceFromPinch(20,150,100),30)&&
                ParkCamera.DistanceFromPinch(30,100,900)==3.5f&&ParkCamera.DistanceFromPinch(30,900,100)==48,
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
            orbit.AdvanceAnimalFollow(1f);
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
            CheckDistanceGuide(park);
            CheckInGameVenuePicker(park);
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
            string family=ParkLanguage.IsKorean?"BaskinRobbins":"ComicRelief";
            Require(regular&&bold&&regular.name==family+"-Regular"&&bold.name==family+"-Bold","Bundled language fonts missing");
            foreach(char letter in ParkLanguage.IsKorean?"애들아놀라빠분만쉰다놀이공간열기동물색칠하기연결앞모습뒷보내기자동원트리케라톱스":"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
                Require(regular.HasCharacter(letter)&&bold.HasCharacter(letter),"UI glyph missing: "+letter);
            Require(GameObject.Find("Storybook countryside"),"Continuous countryside missing");
            Require(!GameObject.Find("Earth edge"),"Display plinth still visible");
            Debug.Log("STORYBOOK FONT/ART PASS: bundled language fonts render UI glyphs; continuous landscape replaces visible plinth.");
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
                var names=Array.ConvertAll(park.Venues,venue=>venue.Name);
                var supported=new[]{"공원","숲","놀이터","사막","시계탑 광장","놀이공원"};
                Require(Array.TrueForAll(names,name=>Array.IndexOf(supported,name)>=0)&&
                    Array.IndexOf(names,"시계탑 광장")>=0&&Array.IndexOf(names,"놀이공원")>=0,
                    "The active place picker does not match the supported venues.");
                Debug.Log("VENUES PASS: "+string.Join(", ",names));
                int forestIndex=Array.FindIndex(park.Venues,venue=>venue.Name=="숲");
                if(forestIndex>=0)
                {
                    park.ChangeVenue(forestIndex);
                    yield return Capture("forest-lawn.png");
                    yield return CheckForestRides(park);
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
                    yield return CheckSandSled(park);
                }
                park.ChangeVenue(Array.IndexOf(names,"놀이공원"));
                yield return CheckElephantTrain(park);
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
            var contactProbe=new Mesh();float maxFootError=0;
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
            Require(maxFootError<.0001f,"Feet deform during walking: "+maxFootError);
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
                        Require(delta.magnitude<ParkRabbitAgent.WanderSpeed*step+.001f,"Forest animal jumped after waving: "+delta);
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
            CheckOpenEntranceDispatch(park);
            CheckAnimalLimit(park);
            CheckCrowdSeparation(park);
            CheckFastPlaygroundArrivals(park);
            CheckFocusedSandSledReplay(park);
            if(!failed){Debug.Log("LIVING PARK PLAYER PASS: drawing Apply/cooldown, five independent friends, rendered paint shader, door emergence and bounded wandering.");Application.Quit(0);}
        }
        IEnumerator CheckStartVenueLayout(ParkPlayground park)
        {
            if(park.Venues?.Length!=6)yield break;
            Require(park.ChooseHostMode(),"The start-screen place picker did not open.");
            var tiles=GameObject.Find("Start Venue Tiles").transform;
            CheckIllustratedVenueGrid(park,tiles);
            CaptureVenueGrid(tiles.GetComponentInParent<Canvas>(),"start-venue-picker.png");
            tiles.Find("뒤로").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Debug.Log($"START VENUE GRID PASS: three columns in two rows at {Screen.width}x{Screen.height}.");
            yield return null;
        }

        void CheckDistanceGuide(ParkPlayground park)
        {
            var camera=Camera.main;
            int visibleLayers=camera.cullingMask;
            park.OpenDistanceGuide();
            var guide=park.GetComponentInChildren<ParkDistanceGuide>();
            Require(guide&&park.IsDistanceGuideOpen&&camera.cullingMask==0&&!park.OrbitCamera.InputEnabled,
                "The distance guide did not cover the playing area or block camera gestures.");
            var cover=guide.transform.Find("Opaque screen cover").GetComponent<UnityEngine.UI.Image>();
            var headline=guide.transform.Find("Reminder heading").GetComponent<UnityEngine.UI.Text>();
            var illustration=guide.transform.Find("Bunny stepping back").GetComponent<UnityEngine.UI.RawImage>();
            Require(((Color32)cover.color).Equals(new Color32(255,251,240,255))&&cover.raycastTarget&&
                cover.rectTransform.anchorMin==Vector2.zero&&cover.rectTransform.anchorMax==Vector2.one&&
                illustration.texture&&guide.transform.childCount==3,
                "The distance guide must contain only its opaque ivory background, illustration and heading.");
            Require(headline.text==ParkLanguage.Text(ParkDistanceGuide.Heading),"The reminder did not use the system language.");
            foreach(bool korean in new[]{true,false})
            {
                guide.SetLanguage(korean);
                var expected=korean?ParkDistanceGuide.Heading:"Can you take one step\nback from the screen?";
                Require(headline.text==expected&&headline.font.name==(korean?"BaskinRobbins-Bold":"ComicRelief-Bold"),
                    "The distance guide copy or font is missing for one locale.");
                Canvas.ForceUpdateCanvases();
                Require(headline.cachedTextGenerator.lineCount==2,"The distance guide heading did not fit on two lines.");
                CaptureVenueGrid(guide.GetComponent<Canvas>(),korean?"distance-guide-ko.png":"distance-guide-en.png");
            }
            guide.SetLanguage(ParkLanguage.IsKorean);
            Canvas.ForceUpdateCanvases();
            var tap=new PointerEventData(EventSystem.current){position=Screen.safeArea.center,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(tap,hits);
            Require(hits.Count>0&&hits[0].gameObject==cover.gameObject,"Tapping over the illustration did not hit the screen cover.");
            ExecuteEvents.Execute(hits[0].gameObject,tap,ExecuteEvents.pointerClickHandler);
            Require(!guide.gameObject.activeSelf&&!park.IsDistanceGuideOpen&&park.OrbitCamera.InputEnabled&&
                camera.cullingMask==visibleLayers,"Tapping the cover did not restore the game view and its camera.");
            Debug.Log($"DISTANCE GUIDE PASS: flat ivory cover, Korean and English copy, and tap-to-resume at {Screen.width}x{Screen.height}.");
        }

        void CheckInGameVenuePicker(ParkPlayground park)
        {
            if(park.Venues?.Length!=6)return;
            var dialogs=park.GetComponentInChildren<ParkDialogs>(true);
            dialogs.Show(false,false,true);
            var panel=dialogs.transform.Find("Venue Picker");
            CheckIllustratedVenueGrid(park,panel);
            for(int index=0;index<6;index++)
            {
                var marker=panel.Find(park.Venues[index].Name+"/Artwork Frame/Current Place");
                Require(marker&&marker.gameObject.activeSelf==(index==park.VenueIndex),
                    "The popup did not mark the current place.");
            }
            CaptureVenueGrid(panel.GetComponentInParent<Canvas>(),"in-game-venue-picker.png",Camera.main);
            dialogs.transform.Find("Modal Backdrop").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Require(!panel.gameObject.activeInHierarchy&&park.IsViewingPark,"Tapping outside did not close the place popup.");
            Debug.Log($"VENUE POPUP PASS: illustrated grid, current highlight and outside dismissal at {Screen.width}x{Screen.height}.");
        }

        static void CheckIllustratedVenueGrid(ParkPlayground park,Transform tiles)
        {
            RectTransform Tile(int index)=>(RectTransform)tiles.Find(park.Venues[index].Name);
            Vector2 TilePosition(int index)=>Tile(index).anchoredPosition;
            float top=TilePosition(0).y,bottom=TilePosition(3).y;
            Require(top>bottom,"The place picker did not lay out two rows.");
            for(int column=0;column<3;column++)
            {
                var upper=TilePosition(column);
                var lower=TilePosition(column+3);
                Require(Mathf.Approximately(upper.y,top)&&Mathf.Approximately(lower.y,bottom)&&
                    Mathf.Approximately(upper.x,lower.x)&&(column==0||upper.x>TilePosition(column-1).x),
                    "The place picker does not show three columns in each row.");
            }
            var covers=Resources.Load<Texture2D>("Welcome/VenueCovers");
            for(int index=0;index<6;index++)
            {
                var artwork=Tile(index).Find("Artwork Frame/Venue Preview").GetComponent<UnityEngine.UI.RawImage>();
                Require(covers&&artwork.texture==covers&&(int)(artwork.uvRect.center.x*3)==index%3&&
                    (int)(artwork.uvRect.center.y*2)==1-index/3,"The venue card is missing its illustrated cover.");
            }
        }

        void CaptureVenueGrid(Canvas canvas,string name,Camera camera=null)
        {
            int width=Screen.width,height=Screen.height;
            bool ownsCamera=!camera;
            if(ownsCamera)
            {
                camera=new GameObject("Place picker review camera").AddComponent<Camera>();
                camera.enabled=false;
                camera.transform.position=Vector3.up*2500;
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=Color.white;
                camera.farClipPlane=10;
            }
            var oldTarget=camera.targetTexture;
            var target=new RenderTexture(width,height,24);
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                canvas.worldCamera=camera;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.planeDistance=2;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);
                File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera=null;
                camera.targetTexture=oldTarget;
                RenderTexture.active=previous;
                target.Release();Destroy(target);Destroy(pixels);
                if(ownsCamera)Destroy(camera.gameObject);
            }
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
            var surface=water.GetComponent<MeshRenderer>().sharedMaterial;
            var bed=oasis.Find("Oasis submerged stones");
            Require(ParkDuneTerrain.OasisRadii.x>6&&ParkDuneTerrain.OasisRadii.y>4.3f&&surface.shader.isSupported&&
                surface.renderQueue==(int)UnityEngine.Rendering.RenderQueue.Transparent&&
                surface.GetColor("_Shallow").a<surface.GetColor("_Deep").a&&surface.GetColor("_Deep").a<.75f&&
                bed&&bed.childCount>=6,"The enlarged oasis is missing its transparent shallows or submerged stones.");
            foreach(var stone in bed.GetComponentsInChildren<MeshRenderer>())
                Require(stone.bounds.max.y<water.position.y,"An oasis bed stone rose through the water.");
            yield return Capture("desert.png");
            var orbit=park.OrbitCamera;
            orbit.enabled=false;
            Camera.main.transform.position=new Vector3(-5.8f,9.7f,-16.2f);
            Camera.main.transform.LookAt(pond+Vector3.up*.75f);
            yield return Capture("desert-oasis.png");
            orbit.enabled=true;
            Debug.Log("OASIS PASS: enlarged basin, translucent shallows, submerged stones, and dry access around five palms.");
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
                float crest=0,firstWave=0;
                bool photographed=false;
                for(int step=0;step<Mathf.CeilToInt((house.ExitDuration+3)/.05f);step++)
                {
                    house.Advance(.05f);
                    agent.Advance(.05f);
                    var position=sample.transform.position;
                    Require(Mathf.Abs(position.y-park.SurfaceHeight(position))<.015f,"The animal left the desert ground.");
                    crest=Mathf.Max(crest,position.y);
                    if(firstWave==0&&agent.WaveCount>0)
                    {
                        firstWave=(step+1)*.05f;
                        Require(Vector3.Distance(position,landing)<.02f,"The animal greeted before reaching the foot of the dune.");
                    }
                    if(index==0&&!photographed&&agent.Character.Skin.enabled&&position.z<=house.Threshold.position.z)
                    {
                        photographed=true;
                        yield return Capture("desert-arrival.png");
                    }
                }
                Require(crest>4.5f&&firstWave>0&&firstWave<=5.6f&&agent.WaveCount==1&&
                    agent.State==ParkRabbitState.Wandering&&!house.IsBusy,
                    "The animal did not cross the dune, wave, and continue walking.");
                sample.SetActive(false);
                Destroy(sample);
            }
            Debug.Log("DESERT PASS: both entrances cross the dune at terrain height and wave at its foot within 5.6 seconds.");
        }
        IEnumerator CheckSandSled(ParkPlayground park)
        {
            var ride=park.SandSled;
            Require(ride&&ride.Sled&&Array.IndexOf(park.AnimalActions,ParkAnimalAction.SandSled)>=0&&
                ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.SandSled),false)=="Ride the Sand Sled",
                "The sand sled or its localized action is missing.");
            foreach(var part in ride.Sled.GetComponentsInChildren<MeshRenderer>())
                Require(!part.isPartOfStaticBatch,"A moving sand sled part was batched with the scenery.");
            var crest=ParkDuneTerrain.SledDuneCrest;
            Require(ParkDuneTerrain.Height(crest.x,crest.y)>ParkDuneTerrain.Height(-12.4f,24f)*2,
                "The sled dune must be at least twice the nearby dune height.");

            var house=park.Houses[0];
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            const float step=.05f;
            var animals=new ParkRabbitAgent[3];
            var started=new bool[animals.Length];
            var finished=new bool[animals.Length];
            var sleds=new Transform[animals.Length];
            var parked=ride.Sled.position;
            var parkedFacing=ride.Sled.rotation;
            for(int i=0;i<animals.Length;i++)
            {
                var animal=new GameObject($"Sand sled rider probe {i+1}").AddComponent<ParkRabbitAgent>();
                animal.Initialize(park,house,(byte)i,0,paint,paint);
                Require(house.TryReserve(),"The sled activity probe could not enter the desert.");
                for(int frame=0;frame<Mathf.CeilToInt((house.ExitDuration+5.5f)/step);frame++)
                {
                    house.Advance(step);
                    animal.Advance(step);
                }
                var position=ride.Approach+new Vector3(-2.5f-i*1.15f,0,-1.1f);
                position.y=park.SurfaceHeight(position);
                animal.transform.position=position;
                Require(animal.State==ParkRabbitState.Wandering&&park.TryBuildRidePath(position,ride.Approach,out _)&&
                    animal.RequestAction(ParkAnimalAction.SandSled),"The animal could not reach or request the sand sled.");
                animals[i]=animal;
            }

            float pullSeconds=0,peak=0;
            int completed=0,maxSleds=0;
            bool climbPhoto=false,slidePhoto=false,multiplePhoto=false,slopePhoto=false;
            for(int frame=0;frame<2000&&completed<animals.Length;frame++)
            {
                foreach(var animal in animals) animal.Advance(step);
                ride.Advance(step);
                Require(ride.Sled.position==parked&&ride.Sled.rotation==parkedFacing&&ride.Sled.gameObject.activeInHierarchy,
                    "The pickup sled moved or disappeared during a ride.");
                maxSleds=Mathf.Max(maxSleds,ride.ActiveSledCount);
                for(int i=0;i<animals.Length;i++)
                {
                    if(finished[i]) continue;
                    var animal=animals[i];
                    var sled=ride.SledFor(animal);
                    if(!sled)
                    {
                        if(!started[i]) continue;
                        Require(animal.ActiveAction==ParkAnimalAction.None&&(!sleds[i]||!sleds[i].gameObject.activeInHierarchy)&&
                            park.IsSceneryClear(animal.transform.position),"The animal or its sled was left in the landing lane.");
                        finished[i]=true;
                        completed++;
                        continue;
                    }
                    Require(sled!=ride.Sled&&(!started[i]||sleds[i]==sled),"A rider lost its own sled.");
                    started[i]=true;
                    sleds[i]=sled;
                    var position=sled.position;
                    if(ride.IsSliding(animal))CheckDescentAlignment(park,sled);
                    else Require(Mathf.Abs(position.y-park.SurfaceHeight(position)-.022f)<.035f,"The sled left the sand surface.");
                    for(int other=0;other<i;other++)
                    {
                        var leading=ride.SledFor(animals[other]);
                        if(leading) Require(Vector3.Distance(leading.position,position)>2.85f,"Two moving sand sleds touched.");
                    }
                    if(i!=0) continue;
                    peak=Mathf.Max(peak,position.y);
                    if(ride.IsPulling(animal))
                    {
                        pullSeconds+=step;
                        var feet=animal.transform.position;
                        Require(Mathf.Abs(feet.y-park.SurfaceHeight(feet))<.016f,"The animal floated while pulling the sled.");
                        if(!climbPhoto&&position.y>7)
                        {
                            climbPhoto=true;
                            yield return Capture("desert-sled-uphill.png");
                        }
                    }
                    if(!ride.IsPulling(animal)&&peak>15&&position.y<8.5f&&!slidePhoto)
                    {
                        slidePhoto=true;
                        Require(Vector3.Distance(animal.transform.position,position)<.45f,"The rider lost the sled seat on the descent.");
                        yield return Capture("desert-sled-downhill.png");
                    }
                    if(!slopePhoto&&ride.IsSliding(animal)&&sled.forward.y<-.48f)
                    {
                        slopePhoto=true;
                        var orbit=park.OrbitCamera;
                        var camera=Camera.main.transform;
                        var view=new Pose(camera.position,camera.rotation);
                        bool tracking=orbit.enabled;
                        orbit.enabled=false;
                        camera.position=sled.position+sled.right*8+sled.forward*3+Vector3.up*3;
                        camera.LookAt(sled.position+Vector3.up*.5f);
                        yield return Capture("desert-sled-slope-alignment.png");
                        camera.SetPositionAndRotation(view.position,view.rotation);
                        orbit.enabled=tracking;
                    }
                }
                if(!multiplePhoto&&ride.ActiveSledCount==animals.Length&&peak>11)
                {
                    multiplePhoto=true;
                    yield return Capture("desert-sled-multiple.png");
                }
            }
            Require(completed==animals.Length&&maxSleds==animals.Length&&climbPhoto&&slidePhoto&&multiplePhoto&&slopePhoto&&
                pullSeconds>0&&pullSeconds<11&&peak>16&&ride.ActiveSledCount==0,
                "The three sand sleds did not complete overlapping rides and return to the pickup prop alone.");
            var repeat=animals[0];
            Require(repeat.RequestAction(ParkAnimalAction.SandSled),"The first rider could not ask to ride again.");
            for(int frame=0;frame<600&&!ride.SledFor(repeat);frame++)
            {
                repeat.Advance(step);
                ride.Advance(step);
            }
            Require(ride.SledFor(repeat)&&ride.ActiveSledCount==1,"The repeat ride did not get a new sled.");
            repeat.gameObject.SetActive(false);
            Require(ride.ActiveSledCount==0&&ride.Sled.position==parked,"Interrupting a ride did not clear its copy.");
            foreach(var animal in animals)
            {
                animal.gameObject.SetActive(false);
                Destroy(animal.gameObject);
            }
            Debug.Log($"SAND SLED PASS: {completed} riders shared the course with {maxSleds} separate sleds, {pullSeconds:F1}s uphill, stationary original, and cleanup after riding again or leaving.");
            Debug.Log("SAND SLED GRADE PASS: both runners followed the graded descent without heading drift, cross-slope, or clipping.");
        }

        static void CheckDescentAlignment(ParkPlayground park,Transform sled)
        {
            var direction=ParkDuneTerrain.SledDescentDirection;
            var heading=new Vector3(direction.x,0,direction.y);
            var flatForward=Vector3.ProjectOnPlane(sled.forward,Vector3.up).normalized;
            Require(Vector3.Dot(flatForward,heading)>.9999f&&Mathf.Abs(sled.right.y)<.002f&&sled.forward.y<.002f,
                "A sand sled slid sideways, banked, or pointed uphill on the descent.");
            var edge=sled.right*1.6f;
            Require(Mathf.Abs(park.SurfaceHeight(sled.position-edge)-park.SurfaceHeight(sled.position+edge))<.005f,
                "The sand lane is tilted across the runners.");
            float closest=float.MaxValue;
            foreach(int side in new[]{-1,1})
            {
                float x=side*ParkSandSled.RunnerHalfSpacing;
                CheckContact(x,ParkSandSled.RearRunnerContact);
                CheckContact(x,0);
                CheckContact(x,ParkSandSled.FrontRunnerContact);
            }
            Require(closest<.034f,"Both sand sled runners floated above their lane.");

            void CheckContact(float x,float z)
            {
                var contact=sled.TransformPoint(new Vector3(x,0,z));
                float clearance=contact.y-park.SurfaceHeight(contact);
                if(clearance<.01f||clearance>=.13f)
                    throw new Exception($"Sand sled runner clearance {clearance:F4} at ({x:F2}, {z:F2}), " +
                        $"sled {sled.position}, forward {sled.forward:F5}, scale {sled.lossyScale}, contact {contact:F5}.");
                closest=Mathf.Min(closest,clearance);
            }
        }

        static void CheckFocusedSandSledReplay(ParkPlayground park)
        {
            int venue=Array.FindIndex(park.Venues,item=>item.SandSled);
            if(venue<0)return;
            park.ChangeVenue(venue);
            park.ShowPark();
            var ride=park.SandSled;
            var orbit=park.OrbitCamera;
            ParkRabbitAgent animal=null;
            foreach(var candidate in park.Animals)
            {
                if(candidate.State!=ParkRabbitState.Wandering||candidate.ActiveAction!=ParkAnimalAction.None||
                    candidate.PendingAction!=ParkAnimalAction.None||!park.CanFollowAnimal(candidate))continue;
                animal=candidate;
                break;
            }
            Require(animal,"There was no animal available for the focused replay probe.");
            const float step=.05f;
            var start=ride.Approach+Vector3.left*2;
            start.y=park.SurfaceHeight(start);
            animal.transform.position=start;
            TapAnimal();
            Require(park.RequestAnimalAction(ParkAnimalAction.SandSled),"The selected animal could not request the first ride.");
            for(int frame=0;frame<800&&!ride.IsDismounting(animal);frame++)Advance();
            Require(ride.IsDismounting(animal),"The first ride did not reach the landing.");

            orbit.CancelAnimalFollow(true);
            TapAnimal();
            var firstSled=ride.SledFor(animal);
            Require(park.RequestAnimalAction(ParkAnimalAction.SandSled)&&animal.PendingAction==ParkAnimalAction.SandSled,
                "Pressing the action on a selected animal at landing did not queue the next ride.");
            Require(!park.RequestAnimalAction(ParkAnimalAction.SandSled),"Repeated taps queued another replay.");
            for(int frame=0;frame<200;frame++)
            {
                Advance();
                var copy=ride.SledFor(animal);
                if(copy&&copy!=firstSled)break;
            }
            var secondSled=ride.SledFor(animal);
            Require(secondSled&&secondSled!=firstSled,"The animal did not return for its next sled while selected.");
            for(int frame=0;frame<800&&ride.SledFor(animal);frame++)Advance();
            Require(animal.ActiveAction==ParkAnimalAction.None&&animal.PendingAction==ParkAnimalAction.None&&
                park.RequestAnimalAction(ParkAnimalAction.SandSled),"The selected animal could not request a ride just after getting off.");
            for(int frame=0;frame<200&&!ride.SledFor(animal);frame++)Advance();
            Require(ride.SledFor(animal)&&ride.SledFor(animal)!=secondSled,"The selected animal did not take a fresh sled from the exit.");
            Debug.Log("FOCUSED SAND SLED REPLAY PASS: requests while stepping off and after dismount both start a new ride with the camera still following.");

            void TapAnimal()
            {
                var point=animal.transform.position+Vector3.up*.65f;
                orbit.focus=point;
                orbit.distance=14;
                orbit.AdvanceAnimalFollow(step);
                var screen=Camera.main.WorldToScreenPoint(point);
                orbit.BeginAnimalTap(screen,-1);
                orbit.EndAnimalTap(screen,-1);
                Require(orbit.FollowTarget==animal,"The selected animal did not enter camera follow.");
            }

            void Advance()
            {
                park.Advance(step);
                orbit.AdvanceAnimalFollow(step);
                Require(orbit.FollowTarget==animal,"The camera lost the animal before it took another sled.");
            }
        }

        IEnumerator CheckForestRides(ParkPlayground park)
        {
            var rides=park.ForestRides;
            var actions=new[]{ParkAnimalAction.StumpHop,ParkAnimalAction.WoodenBridge,ParkAnimalAction.Zipline};
            var english=new[]{"Hop Across Stumps","Cross the Log Bridge","Ride the Zipline"};
            Require(rides?.Length==3&&park.AnimalActions.Length==3&&rides[0].transform.childCount==5,
                "The forest is missing one of its three rides or five stepping stumps.");
            for(int i=0;i<actions.Length;i++)
                Require(park.AnimalActions[i]==actions[i]&&rides[i].Action==actions[i]&&
                    ParkLanguage.Translate(ParkPlayground.ActionLabel(actions[i]),false)==english[i],
                    "A forest action is missing from the selected animal's actions or English text.");
            var trolley=rides[2].Trolley;
            var landingPost=rides[2].transform.Find("Landing cable anchor");
            Require(landingPost&&trolley&&Array.TrueForAll(trolley.GetComponentsInChildren<MeshRenderer>(),r=>!r.isPartOfStaticBatch),
                "The forest trolley is missing or cannot move independently of the cable.");
            var trolleyStart=trolley.position;

            var house=park.Houses[0];
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            var animal=new GameObject("Forest activity probe").AddComponent<ParkRabbitAgent>();
            animal.Initialize(park,house,0,0,paint,paint);
            Require(house.TryReserve(),"The forest activity probe could not leave the cabin.");
            for(int step=0;step<Mathf.CeilToInt((house.ExitDuration+5.5f)/.05f);step++)
            {
                house.Advance(.05f);
                animal.Advance(.05f);
            }
            Require(animal.State==ParkRabbitState.Wandering,"The forest activity probe did not finish waving.");
            for(int i=0;i<rides.Length;i++)
            {
                var ride=rides[i];
                Require(park.TryBuildRidePath(animal.transform.position,ride.Approach,out _)&&animal.RequestAction(actions[i]),
                    "A forest ride could not be requested or reached from the lawn.");
                float highest=0,trolleyTravel=0;
                int jumps=0;
                bool airborne=false,started=false,photographed=false,photographedExit=false;
                Vector3 exitSide=Vector3.zero;
                for(int frame=0;frame<1700;frame++)
                {
                    animal.Advance(.05f);
                    ride.Advance(.05f);
                    started|=animal.ActiveAction==actions[i];
                    float height=animal.transform.position.y;
                    highest=Mathf.Max(highest,height);
                    trolleyTravel=Mathf.Max(trolleyTravel,Vector3.Distance(trolleyStart,trolley.position));
                    if(ride.IsAirborne&&!airborne)jumps++;
                    airborne=ride.IsAirborne;
                    if(!photographed&&(i==0&&jumps>=3&&airborne||i==1&&height>1.25f||i==2&&trolleyTravel>9))
                    {
                        photographed=true;
                        yield return Capture($"forest-action-{i+1}.png");
                    }
                    if(i==2&&trolleyTravel>21)
                    {
                        if(exitSide==Vector3.zero)
                        {
                            var glide=Vector3.ProjectOnPlane(trolley.position-trolleyStart,Vector3.up).normalized;
                            exitSide=Vector3.Cross(Vector3.up,glide);
                        }
                        var fromPost=Vector3.ProjectOnPlane(animal.transform.position-landingPost.position,Vector3.up);
                        Require(fromPost.magnitude>.86f,"The zipline rider crossed through the landing post.");
                        if(!photographedExit&&Vector3.Dot(fromPost,exitSide)>1.13f)
                        {
                            photographedExit=true;
                            var orbit=park.OrbitCamera;
                            var camera=Camera.main;
                            orbit.enabled=false;
                            var forward=Vector3.Cross(exitSide,Vector3.up);
                            camera.transform.position=landingPost.position+exitSide*5-forward*4.7f+Vector3.up*3.9f;
                            camera.transform.LookAt(landingPost.position+exitSide*.58f+Vector3.up*.47f);
                            yield return Capture("forest-zipline-side-exit.png");
                            orbit.enabled=true;
                        }
                    }
                    if(started&&animal.ActiveAction==ParkAnimalAction.None)break;
                }
                var end=animal.transform.position;
                Require(started&&photographed&&animal.ActiveAction==ParkAnimalAction.None&&
                    park.IsSceneryClear(end)&&Mathf.Abs(end.y-park.SurfaceHeight(end))<.01f,
                    "A forest ride did not finish on the walkable grass.");
                if(i==0)Require(jumps==6&&end.x>-4,"The animal did not hop between all five stumps and back onto the grass.");
                if(i==1)Require(highest>1.26f&&end.z>2.2f,"The animal did not climb and cross the wooden ladder.");
                if(i==2)Require(highest>4.4f&&trolleyTravel>21&&photographedExit&&Vector3.Dot(end-landingPost.position,exitSide)>2.1f&&
                    Vector3.Distance(trolley.position,trolleyStart)<.01f,"The animal did not climb, ride the diagonal cable and land.");
            }
            animal.gameObject.SetActive(false);
            Destroy(animal.gameObject);
            Debug.Log("FOREST RIDES PASS: five stumps, footstep ladder crossing, and zipline landing clear of the post with a side exit.");
        }

        IEnumerator CheckElephantTrain(ParkPlayground park)
        {
            var train=park.Train;
            var orbit=park.OrbitCamera;
            var camera=Camera.main;
            var locomotive=train.transform.Find("Elephant locomotive");
            var venue=park.Venues[park.VenueIndex].Root.transform;
            var station=venue.Find("Elephant train station/Station boarding deck");
            Require(locomotive&&station&&!venue.Find("Elephant train exit platform and footbridge"),
                "The train must have only its original platform.");

            orbit.focus=new Vector3(-9.5f,1.2f,-32.6f);
            orbit.distance=Screen.width>Screen.height?46:86;
            orbit.yaw=park.Venues[park.VenueIndex].Yaw;
            orbit.pitch=53;
            orbit.AdvanceAnimalFollow(.05f);
            var overview=new Pose(orbit.transform.position,orbit.transform.rotation);

            var head=camera.WorldToScreenPoint(locomotive.TransformPoint(new Vector3(0,1.9f,1.5f)));
            Require(head.z>0&&Screen.safeArea.Contains(head),"The smoke test camera framed the locomotive outside the viewport.");
            Tap(head);
            CheckView();
            yield return Capture("elephant-first-person-station.png");
            train.Advance(1.8f);
            train.Advance(8f);
            // Give the mounted camera a LateUpdate after the train moves.
            yield return new WaitForEndOfFrame();
            CheckView();
            yield return Capture("elephant-first-person-moving.png");
            Tap(new Vector2(Screen.width*.5f,Screen.height*.46f));
            Require(Vector3.Distance(orbit.transform.position,overview.position)<.001f&&
                Quaternion.Angle(orbit.transform.rotation,overview.rotation)<.01f,
                "Tapping out of the elephant view did not restore the previous angle and zoom.");
            Debug.Log("ELEPHANT CAMERA PASS: engine head tap, moving first-person view, and scene tap restores the previous view.");

            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            var passengers=new ParkRabbitAgent[2];
            for(int i=0;i<passengers.Length;i++)
            {
                var house=park.Houses[i];
                var animal=new GameObject("Train passenger probe").AddComponent<ParkRabbitAgent>();
                animal.Initialize(park,house,i,i,paint,paint);
                Require(house.TryReserve(),"Train probe could not open the arrival door.");
                for(int step=0;step<Mathf.CeilToInt((house.ExitDuration+5.5f)/.05f);step++)
                {
                    house.Advance(.05f);
                    animal.Advance(.05f);
                }
                Require(train.TryRequestedRide(animal),"Train probe could not queue on the boarding side.");
                passengers[i]=animal;
            }
            for(int attempt=0;attempt<1800&&train.SeatedAnimals<passengers.Length;attempt++)train.Advance(.05f);
            Require(train.SeatedAnimals==passengers.Length,"Not all waiting passengers boarded the train.");
            int lap=train.CompletedLaps;
            for(int step=0;step<1500&&train.CompletedLaps==lap;step++)train.Advance(.05f);
            Require(train.Phase==ParkElephantTrain.RidePhase.Unloading,"The train did not stop to unload.");
            var tookPlatformSteps=new bool[passengers.Length];
            for(int step=0;step<100&&train.SeatedAnimals>0;step++)
            {
                train.Advance(.05f);
                for(int i=0;i<passengers.Length;i++)
                {
                    var animal=passengers[i];
                    var point=animal.transform.position;
                    Require(point.z>=ParkElephantTrain.StationRailZ-.01f,"A passenger moved toward the far side of the train.");
                    if(point.z>ParkElephantTrain.StationRailZ+1.7f&&point.y>ParkElephantTrain.DeckHeight+.10f)
                    {
                        Require(Vector3.Dot(-animal.transform.forward,Vector3.forward)>.8f,
                            "A passenger did not face the platform while stepping down.");
                        tookPlatformSteps[i]=true;
                    }
                }
            }
            for(int i=0;i<passengers.Length;i++)
            {
                var animal=passengers[i];
                var point=animal.transform.position;
                Require(tookPlatformSteps[i]&&animal.ActiveAction==ParkAnimalAction.None&&
                    point.z>ParkElephantTrain.StationRailZ+4.5f&&Mathf.Abs(point.y-ParkElephantTrain.DeckHeight)<.01f&&
                    Mathf.Abs(point.y-park.SurfaceHeight(point))<.01f,
                    "A passenger did not finish disembarking on the boarding platform.");
            }
            yield return Capture("amusement-train-platform-unload.png");
            foreach(var animal in passengers)
            {
                animal.gameObject.SetActive(false);
                Destroy(animal.gameObject);
            }
            Debug.Log("TRAIN RETURN PASS: two passengers stepped down forward onto the same platform where they boarded.");

            void Tap(Vector2 screen)
            {
                orbit.BeginAnimalTap(screen,-1);
                orbit.EndAnimalTap(screen,-1);
            }
            void CheckView()
            {
                var eye=train.RideView(true);
                Require(Vector3.Distance(orbit.transform.position,eye.position)<.001f&&
                    Quaternion.Angle(orbit.transform.rotation,eye.rotation)<.01f,
                    "The camera did not stay at the front elephant's viewpoint.");
            }
        }

        static void CheckCrowdSeparation(ParkPlayground park)
        {
            park.Advance(8f);
            int venueCount=park.Venues?.Length??1;
            for(int venue=0;venue<venueCount;venue++)
            {
                park.ChangeVenue(venue);
                // This probe measures pedestrians separating on the lawn.
                var rides=Array.FindAll(new Behaviour[]{park.Wheel,park.Train,park.Carousel,park.Trampoline,park.Seesaw,park.Roundabout},
                    ride=>ride&&ride.enabled);
                foreach(var ride in rides)ride.enabled=false;
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
                foreach(var ride in rides)ride.enabled=true;
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
            park.Advance(.25f);
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
            var stairDirection=Vector3.ProjectOnPlane(ride.Climb(.025f)-ride.Approach,Vector3.up).normalized;
            var side=Vector3.Cross(Vector3.up,stairDirection);
            sample.transform.SetPositionAndRotation(ride.Approach-stairDirection*1.30f-side*1.65f,ride.transform.rotation);
            Require(agent.RequestAction(ParkAnimalAction.Slide),"The animal did not accept the slide action.");
            agent.Advance(.05f);
            Require(agent.CurrentActivity==ParkRabbitAgent.Activity.ApproachSlide&&!ride.Available,"The animal did not reserve the slide.");
            Require(!ride.Reserve(agent)&&!door.TryReserve(),"The occupied slide accepted another reservation or entrance.");
            var stages=new System.Collections.Generic.HashSet<ParkRabbitAgent.Activity>();
            bool facedStairsBeforeClimb=false;
            for(int i=0;i<350&&agent.CurrentActivity!=ParkRabbitAgent.Activity.None;i++)
            {
                agent.Advance(.05f);
                var activity=agent.CurrentActivity;
                bool firstFrame=stages.Add(activity);
                var position=sample.transform.position;
                if(activity==ParkRabbitAgent.Activity.ApproachSlide&&Vector3.Distance(position,ride.Approach)<.30f)
                {
                    Require(Vector3.Dot(-sample.transform.forward,stairDirection)>.96f,"The final approach did not face up the stairs.");
                    facedStairsBeforeClimb=true;
                }
                if(activity==ParkRabbitAgent.Activity.Climb&&position.y<.30f)
                    Require(Vector3.Dot(-sample.transform.forward,stairDirection)>.96f&&(!firstFrame||position.y<.1f),
                        "The animal turned away or skipped the first stair at the start of the climb.");
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
            Require(facedStairsBeforeClimb&&stages.Contains(ParkRabbitAgent.Activity.Climb)&&stages.Contains(ParkRabbitAgent.Activity.Sit)&&
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
            Debug.Log("CAFE ACTIVITIES PASS: straight approach into stairs, sit, slide, walk out, hop and dance.");
            yield return CheckRoundabout(park,agent,platform);
            Destroy(sample);
            yield return CheckPoleEntrance(park);
            Debug.Log("PLAYGROUND PASS: two animated entrances, wave, grounded wandering and equipment avoidance.");
        }
        IEnumerator CheckRoundabout(ParkPlayground park,ParkRabbitAgent agent,Transform platform)
        {
            var ride=park.Roundabout;
            Require(ride&&Array.IndexOf(park.AnimalActions,ParkAnimalAction.Roundabout)>=0,
                "The playground does not offer the roundabout action.");
            for(int rail=1;rail<=3;rail++)
            {
                var grip=platform.Find("Handrail grip "+rail);
                Require(grip&&Vector3.ProjectOnPlane(grip.localPosition,Vector3.up).magnitude<1.5f,
                    "A roundabout handrail is too close to the outer edge.");
            }
            var start=ride.transform.position+Vector3.forward*3.95f;
            agent.transform.position=start;
            Require(!park.WalkingSegmentClear(start,ride.transform.position-Vector3.forward*3.8f)&&
                agent.RequestAction(ParkAnimalAction.Roundabout),"The roundabout request did not need a path around the deck.");
            const float dt=.05f;
            agent.Advance(dt);
            Require(agent.ActiveAction==ParkAnimalAction.Roundabout,"The roundabout request did not reserve a place.");
            float rotation=0;
            bool boarding=false,captured=false,sawDismount=false,walkedIn=false,walkedOut=false;
            for(int i=0;i<1000&&agent.ActiveAction==ParkAnimalAction.Roundabout;i++)
            {
                var before=platform.localRotation;
                ride.Advance(dt);
                var position=agent.transform.position;
                float moved=Quaternion.Angle(before,platform.localRotation);
                float height=position.y-start.y;
                var radial=Vector3.ProjectOnPlane(position-ride.transform.position,Vector3.up);
                boarding|=height>.01f;
                if(!boarding)Require(park.IsSceneryClear(position),"The animal walked through the roundabout base.");
                if(height>.5f&&moved<.001f&&radial.magnitude>2.09f&&radial.magnitude<2.46f)
                {
                    if(rotation<.01f)walkedIn=true;
                    if(rotation>350)walkedOut=true;
                }
                if(height>.5f&&moved>.001f)
                {
                    rotation+=moved;
                    Require(radial.magnitude<2f&&Vector3.Dot(agent.transform.forward,radial.normalized)>.999f&&
                        Mathf.Abs(ride.SurfaceY(position)-position.y)<.002f,
                        "The animal lost its footing or stopped facing its rail while spinning.");
                    if(!captured&&rotation>45)
                    {
                        captured=true;
                        park.OrbitCamera.enabled=false;
                        Camera.main.transform.position=position-agent.transform.forward*4.6f+agent.transform.right*4.1f+Vector3.up*3;
                        Camera.main.transform.LookAt(position+Vector3.up*.67f);
                        yield return Capture("playground-roundabout-ride.png");
                        park.OrbitCamera.enabled=true;
                    }
                }
                if(rotation>350&&height>.01f&&height<.48f)sawDismount=true;
            }
            Require(captured&&walkedIn&&walkedOut&&sawDismount&&rotation>357&&rotation<363&&agent.ActiveAction==ParkAnimalAction.None&&
                park.IsSceneryClear(agent.transform.position)&&Mathf.Abs(agent.transform.position.y-start.y)<.01f&&
                Vector3.Dot(agent.transform.forward,ride.transform.forward)>.999f,
                "The animal did not complete a full roundabout lap and return to the floor.");
            Debug.Log("ROUNDABOUT RIDE PASS: action, obstacle-aware approach, boarding, a full lap and grounded dismount.");
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

        static void CheckOpenEntranceDispatch(ParkPlayground park)
        {
            park.ChangeVenue(0);
            var occupied=park.Houses[0];
            var available=park.Houses[1];
            Require(park.PendingCount==0&&park.Animals.Count+4<=ParkPlayground.AnimalLimit&&
                !available.IsBusy&&occupied.TryReserve(),"The entrance dispatch probe could not hold one door open.");
            var front=park.Drawing.Front.Paint.GetRawTextureData();
            var back=park.Drawing.Back.Paint.GetRawTextureData();
            int firstId=park.SubmissionCount+1;
            int startCount=park.Animals.Count;
            for(int i=0;i<4;i++)
                Require(park.SubmitLan(AnimalKind.Rabbit,front,back)==ParkLan.Accepted,"A request in the LAN burst was dropped.");

            park.Advance(.05f);
            CheckNext(0,1,3);
            park.Advance(available.ExitDuration+available.DoorDuration*2+.15f);
            Require(occupied.IsBusy,"The test entrance was released too soon.");
            CheckNext(1,1,2);
            occupied.CloseAfterExit();
            park.Advance(occupied.DoorDuration+.1f);
            CheckNext(2,0,1);
            park.Advance(12);
            Require(park.PendingCount==0&&park.Animals.Count==startCount+4,
                "A queued arrival was lost when the entrance reopened.");
            for(int i=0;i<4;i++)
            {
                var animal=park.Animals[startCount+i];
                Require(animal.Id==firstId+i&&animal.WaveCount==1,"A burst arrival was duplicated or skipped its greeting.");
            }
            Debug.Log("OPEN ENTRANCE DISPATCH PASS: the open door took the first two LAN submissions while the other was occupied; the reopened door took the next.");

            void CheckNext(int offset,int door,int pending)
            {
                Require(park.PendingCount==pending&&park.Animals.Count==startCount+offset+1&&
                    park.Animals[startCount+offset].Id==firstId+offset&&park.Animals[startCount+offset].HouseIndex==door,
                    "The oldest network submission did not use the first door that became available.");
            }
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
                for(int frame=48;frame<60;frame++)
                {
                    float waveTime=frame/60f;
                    character.PlayWavePose(waveTime);
                    Require(Mathf.Abs(character.Skin.GetBlendShapeWeight(2)-(1+99*waveTime))<.01f&&
                        character.Skin.GetBlendShapeWeight(0)==0,
                        "Shoulder correction must follow the lowering arm without blending in the relaxed pose.");
                }
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
                                Require(motion.magnitude<=ParkRabbitAgent.WanderSpeed*.05f+.001f,"The first steps exceeded ordinary walking speed.");
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
                float expectedScale=authoredScale*(house.SecondDoorHinge?1f:1.1f);
                Require(Vector3.Distance(house.transform.localScale,Vector3.one*expectedScale)<.001f,
                    $"House {house.name} does not match its expected scale of {expectedScale:F2}.");
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
                if(checkedSkins.Add((AnimalKind)i))
                {
                    CheckSkinWeights(agent.Character);
                    CheckSpringGait(agent.Character);
                }
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
            Debug.Log("FULL SIZE EMERGENCE PASS: every animal exits at 130% scale through its venue's doorway.");
            Debug.Log("SKIN WEIGHTS PASS: all species have sorted, normalized influences and rigid head vertices are bound to the head bone.");
        }
        static void CheckSpringGait(RabbitCharacter character)
        {
            var mesh=new Mesh();
            var skin=character.Skin;
            float lowest=0,highest=0;
            int groundedFrames=0;
            const int framesPerStep=48;
            try
            {
                for(int frame=0;frame<framesPerStep*2;frame++)
                {
                    character.PlayWalkingPose(frame*Mathf.PI/framesPerStep);
                    skin.BakeMesh(mesh,false);
                    mesh.RecalculateBounds();
                    float height=mesh.bounds.min.y;
                    lowest=Mathf.Min(lowest,height);
                    highest=Mathf.Max(highest,height);
                    if(Mathf.Abs(height)<.003f)groundedFrames++;
                }
                Require(lowest>-.003f&&highest>.008f&&highest<.085f&&groundedFrames>=70&&groundedFrames<96,
                    $"{character.Kind} lost its planted step or controlled lift-off. Floor range {lowest:F4} to {highest:F4}; planted frames {groundedFrames}.");
                Debug.Log($"SPRING GAIT PASS: {character.Kind}, {groundedFrames}/96 grounded frames, lift {highest:F3}.");
            }
            finally
            {
                character.PlayIdlePose();
                Destroy(mesh);
            }
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
