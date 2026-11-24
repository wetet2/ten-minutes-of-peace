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
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-peaceParkSmoke")<0&&Array.IndexOf(args,"-peaceParkCampSmoke")<0&&
                Array.IndexOf(args,"-peaceParkForestSmoke")<0&&Array.IndexOf(args,"-peaceParkPlaygroundSmoke")<0&&
                Array.IndexOf(args,"-peaceParkChristmasSmoke")<0&&Array.IndexOf(args,"-peaceParkCarouselSmoke")<0&&
                Array.IndexOf(args,"-peaceParkActivityMixSmoke")<0&&Array.IndexOf(args,"-peaceParkResortBridgeSmoke")<0&&
                Array.IndexOf(args,"-peaceParkAutoFocusSmoke")<0&&Array.IndexOf(args,"-peaceParkDefaultBrushSmoke")<0)return;
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
            if(Array.IndexOf(args,"-peaceParkDefaultBrushSmoke")>=0)
            {
                park.ShowDrawing();
                CheckDefaultBrush(park.Drawing);
                Application.Quit(0);
                yield break;
            }
            if(Array.IndexOf(args,"-peaceParkAutoFocusSmoke")>=0)
            {
                int desert=Array.FindIndex(park.Venues,venue=>venue.DesertCamp);
                Require(desert>=0,"The desert campsite venue is missing.");
                park.ChangeVenue(desert);
                park.ShowPark();
                CheckAutoAnimalFocus(park);
                Application.Quit(0);
                yield break;
            }
            if(Array.IndexOf(args,"-peaceParkResortBridgeSmoke")>=0)
            {
                int pool=Array.FindIndex(park.Venues,venue=>venue.ResortRiver);
                Require(pool>=0,"The pool venue is missing.");
                park.ChangeVenue(pool);
                park.ShowPark();
                CheckResortBridge(park);
                CheckResortSunbeds(park);
                CheckResortAutoActivities(park);
                Application.Quit(0);
                yield break;
            }
            if(Array.IndexOf(args,"-peaceParkActivityMixSmoke")>=0)
            {
                int garden=Array.FindIndex(park.Venues,venue=>venue.Name=="공원");
                Require(garden>=0,"The park venue is missing.");
                park.ChangeVenue(garden);
                park.ShowPark();
                CheckParkActivityMix(park);
                Application.Quit(0);
                yield break;
            }
            if(Array.IndexOf(args,"-peaceParkCarouselSmoke")>=0)
            {
                int amusement=Array.FindIndex(park.Venues,venue=>venue.Carousel);
                Require(amusement>=0,"The carousel venue is missing.");
                park.ChangeVenue(amusement);
                park.ShowPark();
                yield return CheckCompactAmusementPark(park);
                Application.Quit(0);
                yield break;
            }
            if(Array.IndexOf(args,"-peaceParkChristmasSmoke")>=0)
            {
                int square=Array.FindIndex(park.Venues,venue=>venue.IceRink);
                Require(square>=0,"The ice rink is missing from the clock tower square.");
                park.ChangeVenue(square);
                park.ShowPark();
                yield return CheckChristmasDeer(park);
                yield return CheckChristmasSkating(park);
                CheckVenueAutomaticActivities(park,18);
                Application.Quit(0);
                yield break;
            }
            if(Array.IndexOf(args,"-peaceParkPlaygroundSmoke")>=0)
            {
                int playground=Array.FindIndex(park.Venues,venue=>venue.PoleRide);
                Require(playground>=0,"The pole tower playground is missing.");
                park.ChangeVenue(playground);
                park.ShowPark();
                CheckMouseCameraControls(park);
                yield return CheckKidsCafe(park);
                CheckFastPlaygroundArrivals(park);
                Application.Quit(0);
                yield break;
            }
            bool testCamp=Array.IndexOf(args,"-peaceParkCampSmoke")>=0;
            if(Array.IndexOf(args,"-peaceParkForestSmoke")>=0)
            {
                int forest=Array.FindIndex(park.Venues,venue=>venue.Name=="숲");
                Require(forest>=0,"The forest venue is missing.");
                park.ChangeVenue(forest);
                park.ShowPark();
                yield return Capture("forest-lawn.png");
                yield return CheckForestRides(park);
                CheckVenueAutomaticActivities(park,24);
                if(!testCamp){Application.Quit(0);yield break;}
            }
            if(testCamp)
            {
                int desert=Array.FindIndex(park.Venues,venue=>venue.DesertCamp);
                Require(desert>=0,"The desert campsite venue is missing.");
                park.ChangeVenue(desert);
                park.ShowPark();
                yield return CheckDesert(park);
                yield return CheckSandSled(park);
                yield return CheckDesertCamp(park);
                CheckVenueAutomaticActivities(park,12);
                CheckAutoAnimalFocus(park);
                Debug.Log("DESERT CAMP PLAYER PASS: campsite, sled and automatic desert activities.");
                Application.Quit(0);
                yield break;
            }
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
            CheckMouseCameraControls(park);
            CheckMouseWheelZoom(park);
            yield return CheckPlaceToolbar(park);
            CheckInGameVenuePicker(park);
            var resetPaint=park.Drawing.Front.Paint.EncodeToPNG();
            for(int i=0;i<3;i++)
                Require(park.TrySubmit((int)AnimalKind.Rabbit,resetPaint,resetPaint,0),"The session reset probe could not submit an animal.");
            park.Advance(1f);
            Require(park.Animals.Count==1&&park.PendingCount==2&&park.Houses[0].IsBusy,
                "The session reset probe needs an active entrance and queued animals.");
            var resetAnimal=park.Animals[0];
            park.OpenSettings();
            var settings=GameObject.Find("Park Settings Popup");
            Require(park.IsSettingsOpen&&settings&&!orbit.InputEnabled,"Settings did not open over the park.");
            var returnButton=settings.transform.Find("시작 화면으로").GetComponent<UnityEngine.UI.Button>();
            returnButton.onClick.Invoke();
            Require(park.IsSelectingMode&&!park.IsSettingsOpen&&!park.Lan.IsHosting,"Settings did not return to the start screen.");
            Require(park.Animals.Count==0&&park.PendingCount==0&&park.SubmissionCount==0&&!resetAnimal.gameObject.activeSelf&&
                Array.TrueForAll(park.Houses,house=>house.CanReserve&&house.DoorFraction==0),
                "Returning to the start screen left animals, queued submissions or an occupied entrance.");
            Debug.Log("PARK SETTINGS RESET PASS: returning to the start screen cleared animals, pending submissions and entrance reservations.");
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
                var supported=new[]{"공원","숲","놀이터","사막","시계탑 광장","놀이공원","수영장"};
                Require(Array.TrueForAll(names,name=>Array.IndexOf(supported,name)>=0)&&
                    Array.IndexOf(names,"시계탑 광장")>=0&&Array.IndexOf(names,"놀이공원")>=0&&Array.IndexOf(names,"수영장")>=0,
                    "The active place picker does not match the supported venues.");
                Debug.Log("VENUES PASS: "+string.Join(", ",names));
                int forestIndex=Array.FindIndex(park.Venues,venue=>venue.Name=="숲");
                if(forestIndex>=0)
                {
                    park.ChangeVenue(forestIndex);
                    yield return Capture("forest-lawn.png");
                    yield return CheckForestRides(park);
                    CheckVenueAutomaticActivities(park,24);
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
                    yield return CheckDesertCamp(park);
                    CheckVenueAutomaticActivities(park,12);
                    CheckAutoAnimalFocus(park);
                }
                park.ChangeVenue(Array.IndexOf(names,"시계탑 광장"));
                yield return CheckChristmasDeer(park);
                yield return CheckChristmasSkating(park);
                CheckVenueAutomaticActivities(park,12);
                park.ChangeVenue(Array.IndexOf(names,"놀이공원"));
                yield return CheckCompactAmusementPark(park);
                yield return Capture("amusement-compact-overview.png");
                CheckContinuousFerrisWheel(park);
                yield return CheckElephantTrain(park);
                park.ChangeVenue(Array.IndexOf(names,"수영장"));
                yield return CheckResortPool(park);
                park.ChangeVenue(0);
            }
            park.ShowDrawing();yield return null;
            CheckNewAnimals(park.Drawing);
            CheckDefaultBrush(park.Drawing);
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
                var departureFrames=new int[3];
                for(int frame=0;frame<750;frame++)
                {
                    int existing=park.Animals.Count-firstArrival;
                    for(int i=0;i<existing;i++)previousPositions[i]=park.Animals[firstArrival+i].transform.position;
                    park.Advance(step);
                    for(int i=0;i<existing;i++)
                    {
                        var agent=park.Animals[firstArrival+i];
                        if(agent.State!=ParkRabbitState.Wandering)continue;
                        if(departureFrames[i]++>=Mathf.CeilToInt(1f/step))continue;
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
            CheckChristmasDeerAvoidance(park);
            yield return CheckFocusedSandSledReplay(park);
            if(!failed){Debug.Log("LIVING PARK PLAYER PASS: drawing Apply/cooldown, five independent friends, rendered paint shader, door emergence and bounded wandering.");Application.Quit(0);}
        }
        IEnumerator CheckStartVenueLayout(ParkPlayground park)
        {
            if(park.Venues==null||park.Venues.Length<6)yield break;
            var welcome=park.GetComponentInChildren<ParkWelcome>();
            var canvas=welcome.GetComponent<Canvas>();
            var meadow=(RectTransform)welcome.transform.Find("Park Illustration");
            var bear=(RectTransform)welcome.transform.Find("Safe Area/Bear");
            var actions=welcome.transform.Find("Safe Area/Start Actions");
            var host=(RectTransform)actions.Find("놀이 공간 열기");
            var together=(RectTransform)actions.Find("함께 놀기");
            var meadowPosition=meadow.anchoredPosition;
            var bearPosition=bear.anchoredPosition;
            var hostPosition=host.anchoredPosition;
            var togetherPosition=together.anchoredPosition;
            yield return new WaitForSecondsRealtime(.3f);
            Require(host.anchoredPosition==hostPosition&&together.anchoredPosition==togetherPosition,
                "The start buttons moved without a layout change.");
            CaptureVenueGrid(canvas,"start-menu.png");
            Require(park.ChooseHostMode(),"The start-screen place picker did not open.");
            Require(!actions.gameObject.activeSelf&&meadow.gameObject.activeInHierarchy&&bear.gameObject.activeInHierarchy&&
                meadow.anchoredPosition==meadowPosition&&bear.anchoredPosition==bearPosition,
                "Opening the place picker changed the illustrated backdrop.");
            var tiles=GameObject.Find("Start Venue Tiles").transform;
            CheckIllustratedVenueGrid(park,tiles);
            var pickerPosition=((RectTransform)tiles).anchoredPosition;
            yield return new WaitForSecondsRealtime(.3f);
            Require(((RectTransform)tiles).anchoredPosition==pickerPosition,"The place picker moved without a layout change.");
            CaptureVenueGrid(canvas,"start-venue-picker.png");
            tiles.Find("뒤로").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Require(actions.gameObject.activeInHierarchy&&!tiles.gameObject.activeSelf,"Back did not restore the start actions.");
            Debug.Log($"START VENUE GRID PASS: {park.Venues.Length} cards in two rows at {Screen.width}x{Screen.height}.");
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

        static void CheckMouseCameraControls(ParkPlayground park)
        {
            var orbit=park.OrbitCamera;
            var venue=park.Venues[park.VenueIndex];
            var screen=Screen.safeArea;
            var center=screen.center;
            var delta=new Vector2(72,36);
            orbit.SetVenueView(venue);

            Send(center,leftHeld:true,leftPressed:true);
            Send(center+delta,leftHeld:true);
            Send(center+delta,leftReleased:true);
            float expectedYaw=delta.x*.25f*900f/Screen.height;
            Require(Mathf.Abs(Mathf.DeltaAngle(venue.Yaw,orbit.yaw)-expectedYaw)<.001f&&orbit.pitch<venue.Pitch&&
                orbit.focus==venue.Focus&&orbit.distance==venue.Distance,
                "Left mouse drag did not rotate the view independently of pan and zoom.");

            orbit.SetVenueView(venue);
            orbit.ApplyTwoFingerGesture(delta,Screen.height,200,200);
            var touchFocus=orbit.focus;
            orbit.SetVenueView(venue);
            Send(center,panHeld:true,panPressed:true);
            Send(center+delta,panHeld:true);
            Send(center+delta);
            Require(Vector3.Distance(touchFocus,venue.Focus)>.1f&&Vector3.Distance(orbit.focus,touchFocus)<.0001f&&
                orbit.yaw==venue.Yaw&&orbit.pitch==venue.Pitch&&orbit.distance==venue.Distance,
                "Right mouse drag did not pan by the same amount as a two-finger drag.");

            orbit.SetVenueView(venue);
            Send(new Vector2(screen.xMin-5,center.y),panHeld:true,panPressed:true);
            Send(center,panHeld:true);
            Send(center);
            Require(orbit.focus==venue.Focus,"A mouse drag that began outside the game moved the camera.");

            Send(center,scroll:3);
            Require(orbit.distance==venue.Distance,"Mouse scroll snapped to a new zoom level.");
            for(int i=0;i<120;i++)orbit.AdvanceAnimalFollow(1f/60f);
            float zoomed=orbit.distance;
            Require(zoomed<venue.Distance,"An upward mouse scroll did not zoom in.");
            Send(center,scroll:-3);
            for(int i=0;i<120;i++)orbit.AdvanceAnimalFollow(1f/60f);
            Require(orbit.distance>zoomed&&Mathf.Abs(orbit.distance-venue.Distance)<.01f,
                "A downward mouse scroll did not zoom out.");
            orbit.SetVenueView(venue);
            Debug.Log("MOUSE CAMERA CONTROLS PASS: left drag rotates, right drag matches two-finger pan, and the wheel zooms both ways.");

            void Send(Vector2 point,bool leftHeld=false,bool leftPressed=false,bool leftReleased=false,
                bool panHeld=false,bool panPressed=false,float scroll=0) =>
                orbit.UpdateMouse(point,leftHeld,leftPressed,leftReleased,panHeld,panPressed,scroll,scaleLikeTouch:true);
        }

        static void CheckMouseWheelZoom(ParkPlayground park)
        {
            var orbit=park.OrbitCamera;
            int previousVenue=park.VenueIndex;
            int venues=park.Venues.Length;
            const float frame=1f/60;
            for(int i=0;i<venues;i++)
            {
                park.ChangeVenue(i);
                float start=orbit.distance;
                var position=orbit.transform.position;
                orbit.ApplyWheelZoom(1);
                orbit.ApplyWheelZoom(2);
                Require(orbit.distance==start&&orbit.transform.position==position,"The mouse wheel snapped the camera.");
                float target=start*Mathf.Exp(-.3f);
                orbit.AdvanceAnimalFollow(frame);
                Require(orbit.distance<start&&orbit.distance>target,"Wheel zoom did not ease into the view.");
                Advance();
                Require(Mathf.Abs(orbit.distance-target)<.004f,"Rapid wheel inputs lost part of their zoom distance.");

                orbit.ApplyWheelZoom(-3);
                orbit.AdvanceAnimalFollow(frame);
                Require(orbit.distance>target&&orbit.distance<start,"Wheel zoom out did not ease away from the view.");
                Advance();
                Require(Mathf.Abs(orbit.distance-start)<.004f,"Zooming in and out did not return to the starting distance.");

                orbit.ApplyWheelZoom(200);
                Advance();
                Require(orbit.distance==ParkCamera.MinimumDistance,"Wheel zoom went past the minimum distance.");
                orbit.ApplyWheelZoom(-200);
                Advance();
                var venue=park.Venues[i];
                Require(orbit.distance==Mathf.Max(48,Mathf.Max(venue.Distance,venue.MaxDistance)),
                    "Wheel zoom went past the venue's maximum distance.");
                orbit.ResetView();
                orbit.AdvanceAnimalFollow(1);
                Require(orbit.distance==start,"Camera reset did not restore the venue's distance.");
            }
            orbit.ApplyWheelZoom(2);
            orbit.AdvanceAnimalFollow(frame);
            float pinched=orbit.distance*.8f;
            orbit.ApplyTwoFingerGesture(Vector2.zero,900,200,250);
            Advance();
            Require(Mathf.Abs(orbit.distance-pinched)<.001f,"An in-progress wheel zoom overrode a pinch gesture.");
            orbit.ApplyWheelZoom(2);
            int next=(park.VenueIndex+1)%venues;
            park.ChangeVenue(next);
            Advance();
            Require(orbit.distance==park.Venues[next].Distance,"A wheel zoom carried into a newly selected venue.");
            park.ChangeVenue(previousVenue);
            Debug.Log($"MOUSE WHEEL ZOOM PASS: smooth zoom in and out across {venues} venues, combined inputs, limits, pinch, reset and venue changes.");

            void Advance()
            {
                for(int sample=0;sample<100;sample++)orbit.AdvanceAnimalFollow(frame);
            }
        }

        IEnumerator CheckPlaceToolbar(ParkPlayground park)
        {
            var safe=Screen.safeArea;
            float scale=Mathf.Min(safe.height/900f,safe.width/665f);
            float width=safe.width/scale;
            foreach(float layoutWidth in new[]{665f,width})
            foreach(bool korean in new[]{true,false})
            {
                var layout=new ParkPlayground.ToolbarLayout(layoutWidth,korean);
                Require(layout.Place.xMin>=0&&layout.Paint.xMin>layout.Place.xMax&&layout.Paint.xMax<layout.DistanceGuide.xMin&&
                    layout.DistanceGuide.xMax<layout.AutoFollow.xMin&&layout.AutoFollow.xMax<layout.ResetCamera.xMin&&
                    layout.ResetCamera.xMax<layout.Settings.xMin&&layout.Settings.xMax<layout.Collapse.xMin&&layout.Collapse.xMax<=layoutWidth,
                    "The place toolbar buttons overlap or are out of order.");
            }
            var actual=new ParkPlayground.ToolbarLayout(width,ParkLanguage.IsKorean);
            foreach(var rect in new[]{actual.Place,actual.Paint,actual.DistanceGuide,actual.AutoFollow,actual.ResetCamera,actual.Settings,actual.Collapse})
                Require(park.ToolbarContains(ScreenPoint(rect.center)),"A toolbar button could pass a tap through to the camera.");
            var gap=new Vector2((actual.LeftGroup.xMax+actual.RightGroup.xMin)*.5f,actual.Place.center.y);
            Require(!park.ToolbarContains(ScreenPoint(gap)),"The empty gap between the toolbar groups blocked camera gestures.");
            Require(ParkLanguage.Translate("전체화면",false)=="Full Screen"&&
                ParkLanguage.Translate("전체화면 종료",false)=="Exit Full Screen","The fullscreen labels are missing in English.");
            Require(ParkLanguage.Translate("자동 포커싱 켜기",false)=="Enable Auto Focus"&&
                ParkLanguage.Translate("자동 포커싱 끄기",false)=="Disable Auto Focus","The automatic camera labels are missing in English.");

            park.OpenSettings();
            var settings=GameObject.Find("Park Settings Popup");
            var fullscreen=settings.transform.Find("전체화면").GetComponent<UnityEngine.UI.Button>();
            var fullscreenText=fullscreen.GetComponentInChildren<UnityEngine.UI.Text>();
            string enterLabel=ParkLanguage.Text("전체화면"),exitLabel=ParkLanguage.Text("전체화면 종료");
            Require(park.IsSettingsOpen&&fullscreen.isActiveAndEnabled&&
                fullscreenText.text==(ParkVisualQuality.IsFullScreen?exitLabel:enterLabel),"Settings did not show the fullscreen action.");
            CaptureVenueGrid(settings.GetComponentInParent<Canvas>(),"settings-fullscreen.png",Camera.main);
            if(Application.platform==RuntimePlatform.WindowsPlayer&&!ParkVisualQuality.IsFullScreen)
            {
                var window=new Vector2Int(Screen.width,Screen.height);
                int venue=park.VenueIndex;
                fullscreen.onClick.Invoke();
                float deadline=Time.realtimeSinceStartup+6;
                while((!ParkVisualQuality.IsFullScreen||fullscreenText.text!=exitLabel)&&Time.realtimeSinceStartup<deadline)yield return null;
                Require(ParkVisualQuality.IsFullScreen&&park.IsSettingsOpen&&park.IsViewingPark&&park.VenueIndex==venue&&fullscreenText.text==exitLabel,
                    "The settings toggle did not retain the current view and switch to Exit Full Screen.");
                fullscreen.onClick.Invoke();
                deadline=Time.realtimeSinceStartup+6;
                while((ParkVisualQuality.IsFullScreen||Screen.width!=window.x||Screen.height!=window.y||fullscreenText.text!=enterLabel)&&
                    Time.realtimeSinceStartup<deadline)yield return null;
                Require(!ParkVisualQuality.IsFullScreen&&Screen.width==window.x&&Screen.height==window.y&&park.IsSettingsOpen&&fullscreenText.text==enterLabel,
                    "Leaving fullscreen did not restore the previous window size and settings label.");
            }
            park.CloseDialog();
            Debug.Log($"PLACE TOOLBAR PASS: separated tool groups, free gesture space and fullscreen round trip inside Settings at {Screen.width}x{Screen.height}.");

            Vector2 ScreenPoint(Vector2 point)=>new Vector2(safe.xMin+point.x*scale,safe.yMax-point.y*scale);
        }

        void CheckInGameVenuePicker(ParkPlayground park)
        {
            if(park.Venues==null||park.Venues.Length<6)return;
            Require(park.Lan.IsHosting||park.Lan.StartHost(),"The place picker probe could not start hosting.");
            var welcome=park.GetComponentInChildren<ParkWelcome>(true);
            var camera=park.OrbitCamera.GetComponent<Camera>();
            int visibleLayers=camera.cullingMask;
            park.OpenVenuePicker();
            var panel=welcome.transform.Find("Safe Area/Start Venue Tiles");
            Require(park.IsVenuePickerOpen&&panel.gameObject.activeInHierarchy&&camera.cullingMask==visibleLayers&&
                !park.OrbitCamera.InputEnabled&&!welcome.transform.Find("Safe Area/Start Actions").gameObject.activeSelf,
                "The fullscreen picker did not preserve the current scene while blocking camera input.");
            Require(!welcome.transform.Find("Park Illustration").gameObject.activeSelf&&
                !welcome.transform.Find("Safe Area/Bear").gameObject.activeSelf,
                "The in-game picker covered the current place with the start illustration.");
            CheckIllustratedVenueGrid(park,panel);
            for(int index=0;index<park.Venues.Length;index++)
            {
                var marker=panel.Find(park.Venues[index].Name+"/Artwork Frame/Current Place");
                Require(marker&&marker.gameObject.activeSelf==(index==park.VenueIndex),
                    "The fullscreen picker did not mark the current place.");
            }
            CaptureVenueGrid(welcome.GetComponent<Canvas>(),"in-game-venue-picker.png",camera);
            panel.Find("뒤로").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Require(!welcome.gameObject.activeSelf&&!park.IsVenuePickerOpen&&park.IsViewingPark&&
                park.OrbitCamera.InputEnabled&&camera.cullingMask==visibleLayers&&park.Lan.IsHosting,
                "Back did not restore the hosted park and camera.");
            park.OpenVenuePicker();
            panel.Find(park.Venues[park.VenueIndex].Name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Require(!welcome.gameObject.activeSelf&&!park.IsVenuePickerOpen&&park.Lan.IsHosting,
                "Selecting the current place did not close the fullscreen picker.");
            Debug.Log($"VENUE PICKER PASS: shared fullscreen screen, current highlight, back and selection at {Screen.width}x{Screen.height}.");
        }

        static void CheckIllustratedVenueGrid(ParkPlayground park,Transform tiles)
        {
            RectTransform Tile(int index)=>(RectTransform)tiles.Find(park.Venues[index].Name);
            Vector2 TilePosition(int index)=>Tile(index).anchoredPosition;
            int count=park.Venues.Length,columns=ParkWelcome.VenueColumns(count);
            float top=TilePosition(0).y,bottom=TilePosition(columns).y;
            Require(top>bottom,"The place picker did not lay out two rows.");
            for(int index=0;index<count;index++)
            {
                var point=TilePosition(index);
                Require(Mathf.Approximately(point.y,index<columns?top:bottom)&&
                    (index%columns==0||point.x>TilePosition(index-1).x),"The place cards are out of order in their rows.");
            }
            float firstCenter=(TilePosition(0).x+TilePosition(columns-1).x)*.5f;
            float secondCenter=(TilePosition(columns).x+TilePosition(count-1).x)*.5f;
            Require(Mathf.Abs(firstCenter-secondCenter)<.02f,"The partial venue row was not centered.");
            var covers=Resources.Load<Texture2D>("Welcome/VenueCovers");
            for(int index=0;index<count;index++)
            {
                var artwork=Tile(index).Find("Artwork Frame/Venue Preview").GetComponent<UnityEngine.UI.RawImage>();
                if(index<6)
                    Require(covers&&artwork.texture==covers&&(int)(artwork.uvRect.center.x*3)==index%3&&
                        (int)(artwork.uvRect.center.y*2)==1-index/3,"The venue card is missing its illustrated cover.");
                else Require(artwork.texture&&artwork.texture==park.Venues[index].Preview,"The resort card is missing its cover.");
            }
        }

        IEnumerator CheckResortPool(ParkPlayground park)
        {
            var venue=park.Venues[park.VenueIndex];
            var river=park.ResortRiver;
            Vector3 World(Vector3 point)=>river.transform.TransformPoint(point);
            Vector3 Offset(Vector3 vector)=>river.transform.TransformVector(vector);
            var deck=venue.Root.transform.Find("Limestone pool terraces");
            var poolWater=deck.Find("Main pool water").GetComponent<MeshRenderer>();
            var riverWater=deck.Find("Closed river water circuit").GetComponent<MeshRenderer>();
            Require(river&&poolWater.sharedMaterial.shader.isSupported&&riverWater.sharedMaterial.GetFloat("_Current")==1&&
                venue.Preview&&ParkLanguage.Translate(venue.Name,false)=="Pool"&&
                ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.Tube),false)=="Ride a Tube"&&
                Array.IndexOf(park.AnimalActions,ParkAnimalAction.Tube)>=0,"The resort, water materials or localized tube action are missing.");
            float length=ParkResortRiver.LapLength;
            Require(Mathf.Abs(park.WalkArea.width-40.975f)<.01f&&Mathf.Abs(park.WalkArea.height-19.7f)<.01f&&
                Mathf.Abs(poolWater.bounds.size.x-9.75f)<.01f&&Mathf.Abs(poolWater.bounds.size.z-12.25f)<.01f,
                "The resort deck, swimming pool and walk area did not shrink together.");
            Require(length*river.transform.lossyScale.x>37.5f&&Vector2.Distance(ParkResortRiver.ChannelPoint(0),ParkResortRiver.ChannelPoint(length))<.001f,
                "The current does not form a closed circuit.");
            var boarding=World(ParkResortRiver.BoardingMark);
            var exit=World(ParkResortRiver.ExitMark);
            var pool=ParkResortRiver.PoolCenter;
            Require(park.IsSceneryClear(boarding)&&park.IsSceneryClear(exit)&&!park.IsSceneryClear(World(new Vector3(pool.x,0,pool.y)))&&
                !park.WalkingSegmentClear(boarding,World(new Vector3(20,0,15))),"Walking routes can leave the deck or cross the pool.");
            var bridge=deck.Find("Limestone bridge over the river");
            Require(bridge&&bridge.Find("Continuous curved stone deck"),"The curved bridge deck is missing.");
            float bridgeX=ParkResortRiver.BridgeCenterX;
            float islandEnd=bridgeX-ParkResortRiver.BridgeHalfSpan, terraceEnd=bridgeX+ParkResortRiver.BridgeHalfSpan;
            Require(Mathf.Abs(ParkResortRiver.BridgeSurfaceHeight(islandEnd+.2f)-ParkResortRiver.IslandPathHeight)<.006f&&
                Mathf.Abs(ParkResortRiver.BridgeSurfaceHeight(terraceEnd-.2f)-ParkResortRiver.DeckHeight)<.006f,
                "The bridge does not meet both landings gently.");
            float steepest=0;
            for(float x=islandEnd;x<terraceEnd;x+=.1f)
                steepest=Mathf.Max(steepest,Mathf.Abs(ParkResortRiver.BridgeSurfaceHeight(x+.1f)-ParkResortRiver.BridgeSurfaceHeight(x))/.1f);
            Require(steepest<.55f,"The river bridge has a steep grade.");
            var gardens=venue.Root.transform.Find("Pandazole resort gardens");
            Require(gardens&&gardens.Find("Front arrival garden and loggia/Tiled arrival loggia")&&
                gardens.Find("West hillside gardens/Shaded garden pavilion")&&gardens.Find("East hillside gardens/Shaded garden pavilion")&&
                gardens.Find("Layered coastal groves and hills/Coastal hill"),"The pool lost a surrounding garden or pavilion.");
            var flowerBed=gardens.Find("Flower beds along the pool terraces/Integrated limestone planter/Pandazole garden colors");
            Require(flowerBed&&flowerBed.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                "The poolside garden is missing its Pandazole planting.");
            var hotel=venue.Root.transform.Find("Limestone resort hotel");
            Require(hotel&&hotel.Find("Central hotel pavilion/Grand lobby elevation/Grand guest entrance portico")&&park.Houses.Length==1,
                "The resort must have one entrance at the hotel lobby.");
            var lobby=hotel.Find("Central hotel pavilion/Grand lobby elevation");
            Require(lobby.Find("Lobby reception and lounges")&&lobby.Find("Clear sea-tinted lobby glass"),
                "The resort lobby is missing its reception or its fitted glass.");
            var lobbyEntrance=park.Houses[0];
            Require(lobbyEntrance.transform.parent==lobby&&hotel.GetComponentsInChildren<ParkHouse>().Length==1&&
                lobbyEntrance.DoorHinge&&lobbyEntrance.SecondDoorHinge&&Mathf.Abs(lobbyEntrance.Threshold.position.x)<.01f&&
                park.IsSceneryClear(lobbyEntrance.Welcome.position),"The central doorway or its greeting point is out of line.");
            yield return CaptureVenueView(park,"resort-hotel-facade.png",World(new Vector3(0,4.5f,21)),Offset(new Vector3(0,14.5f,-55)));
            yield return CaptureVenueView(park,"resort-hotel-lobby.png",World(new Vector3(0,3.4f,19.6f)),Offset(new Vector3(0,1.10f,-18.8f)));
            yield return CaptureVenueView(park,"resort-hotel-guest-wing.png",World(new Vector3(21.7f,5.2f,22.1f)),Offset(new Vector3(11.7f,4.2f,-20.6f)));
            CheckResortArrivalQueue(park,lobbyEntrance);
            Debug.Log("RESORT HOTEL PASS: continuous facade and two queued arrivals through the central lobby doors.");
            yield return CaptureVenueView(park,"resort-front-garden.png",World(new Vector3(-5f,1.9f,-26f)),Offset(new Vector3(5.5f,10.5f,31)));
            yield return CaptureVenueView(park,"resort-east-garden.png",World(new Vector3(32f,1.1f,0)),Offset(new Vector3(-18f,11.5f,-14)));
            yield return CaptureVenueView(park,"resort-west-garden.png",World(new Vector3(-31f,1.1f,0)),Offset(new Vector3(22f,8.5f,-8)));
            Debug.Log("RESORT GARDEN PASS: the front loggia, coastal groves, side pavilions and Pandazole poolside planting are present.");

            var riders=new[]{PrepareRideProbe(park,AnimalKind.Rabbit),PrepareRideProbe(park,AnimalKind.Giraffe)};
            for(int i=0;i<riders.Length;i++)
            {
                riders[i].transform.position=boarding+Offset(new Vector3(i*1.6f,0,-.55f));
                Require(riders[i].RequestAction(ParkAnimalAction.Tube),"A resort animal could not request a tube.");
            }
            var started=new bool[2];
            var finished=new bool[2];
            int quadrants=0;
            bool alongside=false,bridgePass=false,photographed=false;
            for(int frame=0;frame<1400&&(!finished[0]||!finished[1]);frame++)
            {
                foreach(var rider in riders)rider.Advance(.05f);
                river.Advance(.05f);
                for(int i=0;i<riders.Length;i++)
                {
                    if(finished[i])continue;
                    var tube=river.TubeFor(riders[i]);
                    if(!tube)
                    {
                        if(!started[i])continue;
                        Require(riders[i].ActiveAction==ParkAnimalAction.None&&Vector3.Distance(riders[i].transform.position,exit)<.02f,
                            "A tube rider did not disembark at the same dock.");
                        finished[i]=true;
                        continue;
                    }
                    if(!started[i])
                        foreach(var shape in tube.GetComponentsInChildren<MeshRenderer>())Require(!shape.isPartOfStaticBatch,"A moving tube was batched into the deck.");
                    started[i]=true;
                    if(!river.IsFloating(riders[i]))continue;
                    var animal=riders[i];
                    var waist=tube.position+Vector3.up*ParkResortRiver.TubeWaistRise;
                    float waistError=Vector3.Distance(waist,animal.Character.TubeWaist);
                    float depth=ParkResortRiver.WaterHeight-animal.transform.position.y;
                    float chestHeight=animal.Character.SwimChest.y-ParkResortRiver.WaterHeight;
                    Require(waistError<.025f&&depth>.5f&&chestHeight>.17f,
                        $"The {animal.Character.Kind} is misaligned in its tube: waist {waistError:F2}, depth {depth:F2}, chest {chestHeight:F2}.");
                    var p=river.transform.InverseTransformPoint(tube.position);
                    var center=ParkResortRiver.RiverCenter;
                    if(i==0)quadrants|=1<<((p.x>center.x?1:0)+(p.z>center.y?2:0));
                    if(i==1&&!bridgePass&&Mathf.Abs(p.x-bridgeX)<.25f&&Mathf.Abs(p.z-ParkResortRiver.BridgeZ)<.20f)
                    {
                        float clearance=ResortBridgeClearance(animal,river.transform);
                        Require(clearance>.12f&&!float.IsInfinity(clearance),$"The giraffe's bridge clearance was {clearance:F2}.");
                        bridgePass=true;
                        yield return CaptureVenueView(park,"resort-bridge.png",World(new Vector3(bridgeX,1.15f,ParkResortRiver.BridgeZ)),
                            Offset(new Vector3(1.65f,3.55f,-12.3f)));
                        Debug.Log($"RESORT BRIDGE PASS: continuous arch and giraffe head clearance {clearance:F2}.");
                    }
                    if(i>0||!river.IsFloating(riders[1]))continue;
                    var second=river.TubeFor(riders[1]);
                    Require(Vector3.Distance(tube.position,second.position)>2.35f,"Two occupied tubes touched on the river.");
                    alongside=true;
                    if(!photographed&&p.x>center.x+9&&p.z<2)
                    {
                        photographed=true;
                        yield return Capture("resort-pool-and-river.png");
                        yield return CaptureVenueView(park,"resort-tube-in-water.png",waist+Vector3.up*.24f,
                            animal.transform.right*4.9f-animal.transform.forward*5.2f+Vector3.up*2.5f);
                    }
                }
            }
            Require(finished[0]&&finished[1]&&river.CompletedLaps==2&&quadrants==15&&alongside&&bridgePass&&photographed,
                "Two tubes did not finish separate full circuits around the resort island.");
            foreach(var rider in riders)
            {
                rider.gameObject.SetActive(false);
                Destroy(rider.gameObject);
            }
            Debug.Log("RESORT POOL PASS: a separate swimming pool and continuous current, two independently moving tubes, bridge passage and dismount at the original dock.");
            yield return CheckResortSwimming(park);
            yield return CheckResortWaterSlide(park);
            CheckResortAutoActivities(park);
            CheckResortBridge(park);
            CheckResortSunbeds(park);
        }

        static void CheckResortBridge(ParkPlayground park)
        {
            const ParkAnimalAction action=ParkAnimalAction.ResortBridge;
            const float step=.05f;
            var river=park.ResortRiver;
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            var label=ParkPlayground.ActionLabel(action);
            Require(label=="다리 건너기"&&ParkLanguage.Translate(label,false)=="Cross the Bridge"&&
                Array.IndexOf(park.AnimalActions,action)>=0,"The pool is missing its localized bridge action.");
            Require(park.Animals.Count==0&&park.TrySubmit((int)AnimalKind.Rabbit,paint,paint),"Could not submit a bridge walker.");
            for(int i=0;i<400&&(park.Animals.Count==0||!park.CanFollowAnimal(park.Animals[0]));i++)park.Advance(step);
            Require(park.Animals.Count==1&&park.CanFollowAnimal(park.Animals[0]),"The bridge walker did not arrive through the lobby.");
            var animal=park.Animals[0];
            park.OrbitCamera.ToggleAnimalFollow(animal);
            Cross(entering:true);

            Require(!park.RequestAnimalAction(ParkAnimalAction.Swim),"A water activity was offered inside the island garden.");
            float walked=animal.WalkedDistance;
            for(int i=0;i<900;i++)
            {
                park.Advance(step);
                Require(river.IsOnIsland(animal.transform.position)&&animal.ActiveAction!=action&&
                    park.IsSceneryClear(animal.transform.position),"The animal left the island without a bridge request or walked into scenery.");
            }
            Require(animal.WalkedDistance>walked+5,"The animal could not roam the island after crossing.");
            Cross(entering:false);

            park.ReturnToModeSelection();
            park.ShowPark();
            Debug.Log("RESORT BRIDGE PASS: walked across the right bridge in both directions and stayed on the island between requests.");

            void Cross(bool entering)
            {
                Require(river.IsOnIsland(animal.transform.position)!=entering&&park.RequestAnimalAction(action),
                    $"The bridge action could not be requested (entering: {entering}).");
                bool crossedCrest=false;
                float previousX=river.transform.InverseTransformPoint(animal.transform.position).x;
                for(int i=0;i<1000;i++)
                {
                    park.Advance(step);
                    var local=river.transform.InverseTransformPoint(animal.transform.position);
                    if(animal.IsUsingRide&&animal.ActiveAction==action)
                    {
                        Require(Mathf.Abs(local.z-ParkResortRiver.BridgeZ)<.015f&&
                            Mathf.Abs(local.y-ParkResortRiver.BridgeSurfaceHeight(local.x))<.015f,
                            "The animal stepped off the center of the bridge deck.");
                        if(Mathf.Abs(local.x-ParkResortRiver.BridgeCenterX)<.25f)
                        {
                            Require(entering?local.x<previousX:local.x>previousX,"The animal crossed in the wrong direction.");
                            crossedCrest=true;
                        }
                    }
                    if(crossedCrest&&animal.ActiveAction==ParkAnimalAction.None)
                    {
                        Require(river.IsOnIsland(animal.transform.position)==entering&&park.IsSceneryClear(animal.transform.position),
                            "The bridge did not end on a walkable landing at the opposite bank.");
                        return;
                    }
                    previousX=local.x;
                }
                throw new Exception($"The animal did not complete the bridge crossing (entering: {entering}).");
            }
        }

        static void CheckResortArrivalQueue(ParkPlayground park,ParkHouse entrance)
        {
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            Require(park.Animals.Count==0&&park.TrySubmit((int)AnimalKind.Rabbit,paint,paint)&&
                park.TrySubmit((int)AnimalKind.Giraffe,paint,paint),"The resort did not accept two arrivals.");
            park.Advance(.05f);
            Require(park.Animals.Count==1&&park.PendingCount==1,"A second animal entered while the central door was busy.");
            for(int i=0;i<180&&entrance.CompletedExits<2;i++)
            {
                park.Advance(.05f);
                foreach(var animal in park.Animals)
                    if(animal.IsEmerging)
                        Require(animal.HouseIndex==0&&Mathf.Abs(animal.transform.position.x-entrance.Threshold.position.x)<.01f,
                            "An animal emerged away from the central lobby doors.");
            }
            Require(entrance.CompletedExits==2&&park.PendingCount==0&&park.Animals.Count==2&&
                park.Animals[0].ExitedThroughOpenDoor&&park.Animals[1].ExitedThroughOpenDoor,
                "Both animals must walk out through the open central lobby doors.");
            park.ReturnToModeSelection();
            park.ShowPark();
        }

        IEnumerator CheckResortWaterSlide(ParkPlayground park)
        {
            var slide=park.ResortSlide;
            Vector3 World(Vector3 point)=>slide.transform.TransformPoint(point);
            Require(slide&&Array.IndexOf(park.AnimalActions,ParkAnimalAction.WaterSlide)>=0&&
                ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.WaterSlide),false)=="Ride Water Slide",
                "The resort water slide or its bilingual action is missing.");
            var approach=World(ParkResortSlide.Approach);
            var exit=World(ParkResortSlide.Exit);
            var outlet=World(ParkResortSlide.Outlet);
            Require(park.IsSceneryClear(approach)&&park.IsSceneryClear(exit),
                "The slide tower or pool ladder has no path on the deck.");
            Require(exit.z-outlet.z>5&&Mathf.Abs(World(ParkResortSlide.TopLanding).y-11.7f)<.01f&&
                slide.transform.Find("Limestone slide tower/Top stair landing and launch deck"),
                "The slide tower lost its height, stairwell or separate exit.");
            var spray=slide.transform.Find("Slide landing spray").GetComponent<ParticleSystem>();
            var ripples=slide.transform.Find("Expanding slide impact ripples").GetComponent<MeshRenderer>();
            Require(!ripples.isPartOfStaticBatch&&!ripples.enabled,"The impact rings cannot animate independently.");
            var prev=ParkResortSlide.TrackPoint(0);
            for(int i=1;i<=320;i++)
            {
                var next=ParkResortSlide.TrackPoint(i/320f);
                Require(next.y<=prev.y+.001f&&Vector3.Distance(next,prev)<.4f,"The waterslide track rises or has a break.");
                prev=next;
            }
            Require(park.ResortRiver.BlocksWalking(outlet)&&
                ParkResortSlide.Outlet.x<ParkResortRiver.PoolCenter.x-6&&
                Mathf.Abs(ParkResortSlide.Outlet.z-ParkResortRiver.PoolCenter.y)<ParkResortRiver.PoolHalf.y-2f,
                "The slide does not run into the left swimming pool.");
            yield return Capture("resort-water-slide-overview.png");
            var animal=PrepareRideProbe(park,AnimalKind.Rabbit);
            animal.transform.position=World(ParkResortSlide.Approach+Vector3.right*2.65f);
            Require(animal.RequestAction(ParkAnimalAction.WaterSlide),"An animal could not queue for the water slide.");
            bool slid=false,landed=false,photographed=false,steppedOut=false,splashSeen=false,topSeen=false;
            float peak=0;
            float farthestSwim=float.NegativeInfinity;
            for(int i=0;i<900;i++)
            {
                animal.Advance(.05f);
                slide.Advance(.05f);
                peak=Mathf.Max(peak,animal.transform.position.y);
                slid|=slide.IsSliding(animal);
                landed|=slide.IsSwimming(animal)&&Vector3.Distance(animal.transform.position,outlet)<1.65f;
                if(slide.IsSwimming(animal))
                {
                    farthestSwim=Mathf.Max(farthestSwim,animal.Character.SwimChest.z);
                    if(!splashSeen&&spray.particleCount>=45&&ripples.enabled)
                    {
                        splashSeen=true;
                        yield return new WaitForSecondsRealtime(.18f);
                        yield return CaptureVenueView(park,"resort-slide-splash.png",World(ParkResortSlide.ImpactPoint+Vector3.up*.38f),
                            new Vector3(-.95f,4.25f,-8.5f));
                    }
                }
                if(!topSeen&&!slid&&animal.transform.position.y>11.63f)
                {
                    topSeen=true;
                    yield return CaptureVenueView(park,"resort-slide-upper-landing.png",World(ParkResortSlide.TopLanding+new Vector3(1.25f,.10f,-.55f)),
                        new Vector3(-7.8f,2.66f,6.05f));
                }
                if(!photographed&&slide.IsSliding(animal)&&animal.transform.position.y<6.3f&&animal.transform.position.z<1.8f)
                {
                    photographed=true;
                    yield return CaptureVenueView(park,"resort-water-slide.png",World(new Vector3(-35f,5.3f,1.4f)),new Vector3(-3f,16.5f,-25));
                }
                if(slid&&animal.ActiveAction==ParkAnimalAction.None)
                {
                    steppedOut=Vector3.Distance(animal.transform.position,exit)<.025f;
                    break;
                }
            }
            Require(slid&&landed&&steppedOut&&photographed&&topSeen&&peak>11.6f&&splashSeen&&farthestSwim>World(new Vector3(0,0,6.75f)).z,
                "The animal did not reach the landing, splash into the water or swim to the far ladder.");
            animal.gameObject.SetActive(false);
            Destroy(animal.gameObject);
            Debug.Log("RESORT WATER SLIDE PASS: open top stairwell, visible landing spray and ripples, and swim to the far ladder.");
        }

        static float ResortBridgeClearance(ParkRabbitAgent animal,Transform river)
        {
            var skin=animal.Character.Skin;
            var mesh=skin.sharedMesh;
            var headToWorld=skin.bones[1].localToWorldMatrix*mesh.bindposes[1];
            var vertices=mesh.vertices;
            var weights=mesh.boneWeights;
            float minimum=float.PositiveInfinity;
            for(int i=0;i<vertices.Length;i++)
            {
                if(weights[i].boneIndex0!=1||weights[i].weight0<.999f)continue;
                var point=river.InverseTransformPoint(headToWorld.MultiplyPoint3x4(vertices[i]));
                if(Mathf.Abs(point.z-ParkResortRiver.BridgeZ)>ParkResortRiver.BridgeHalfWidth)continue;
                float underside=ParkResortRiver.BridgeSurfaceHeight(point.x)-ParkResortRiver.BridgeDeckDepth;
                minimum=Mathf.Min(minimum,underside-point.y);
            }
            return minimum;
        }

        IEnumerator CheckResortSwimming(ParkPlayground park)
        {
            var swim=park.ResortSwim;
            Vector3 World(Vector3 point)=>swim.transform.TransformPoint(point);
            Require(swim&&Array.IndexOf(park.AnimalActions,ParkAnimalAction.Swim)>=0&&
                ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.Swim),false)=="Swim",
                "The localized swimming action is missing.");
            var animals=new[]{PrepareRideProbe(park,AnimalKind.Rabbit),PrepareRideProbe(park,AnimalKind.Hippo)};
            var pool=ParkResortRiver.PoolCenter;
            var half=ParkResortRiver.PoolHalf;
            float deck=ParkResortRiver.DeckHeight;
            var rims=new[]{World(new Vector3(pool.x-3.95f,deck,pool.y+half.y)),World(new Vector3(pool.x+half.x,deck,pool.y-3.6f))};
            var inward=new[]{Vector3.back,Vector3.left};
            float scale=swim.transform.lossyScale.x;
            var starts=new[]{rims[0]-inward[0]*5.19f*scale,rims[1]-inward[1]*2.70f*scale};
            var exits=new[]{World(ParkResortSwim.ExitMark(0)),World(ParkResortSwim.ExitMark(1))};
            var jumped=new bool[2];
            var entered=new bool[2];
            var crossed=new bool[2];
            var finished=new bool[2];
            bool photographed=false;
            var runner=animals[0];
            var bones=runner.Character.Skin.bones;
            var sole=new Vector3(0,-RabbitMesh.HipHeight,-.14f);
            float StepSpread()=>Mathf.Abs(Vector3.Dot(bones[4].TransformPoint(sole)-bones[5].TransformPoint(sole),runner.transform.forward));
            float walkingSpread=0,joggingSpread=0,peakSpeed=0,arrivalSpeed=0;
            for(int frame=0;frame<48;frame++)
            {
                runner.Character.PlayWalkingPose(frame*Mathf.PI/24);
                walkingSpread=Mathf.Max(walkingSpread,StepSpread());
            }
            runner.Character.PlayIdlePose();
            for(int i=0;i<animals.Length;i++)
            {
                Require(park.IsSceneryClear(exits[i]),"The swimming stairs have no dry exit.");
                Require(park.WalkingSegmentClear(starts[i],rims[i]-inward[i]*.5f*scale),
                    "The swimming approach probe needs a clear path to the nearest rim.");
                animals[i].transform.SetPositionAndRotation(starts[i],Quaternion.LookRotation(-inward[i]));
                Require(animals[i].RequestAction(ParkAnimalAction.Swim),"An animal could not request swimming.");
                animals[i].Advance(.05f);
            }
            for(int frame=0;frame<660&&(!finished[0]||!finished[1]);frame++)
            {
                foreach(var animal in animals)animal.Advance(.05f);
                var previous=runner.transform.position;
                swim.Advance(.05f);
                float remaining=Vector3.Dot(rims[0]-previous,inward[0])-.64f*scale;
                if(!jumped[0]&&remaining>.04f)
                {
                    float speed=Vector3.Dot(runner.transform.position-previous,inward[0])/.05f;
                    if(remaining>1)
                    {
                        peakSpeed=Mathf.Max(peakSpeed,speed);
                        joggingSpread=Mathf.Max(joggingSpread,StepSpread());
                    }
                    if(remaining<.16f)arrivalSpeed=Mathf.Max(arrivalSpeed,speed);
                }
                for(int i=0;i<animals.Length;i++)
                {
                    var animal=animals[i];
                    if(!entered[i])
                    {
                        var offset=animal.transform.position-rims[i];
                        float along=Vector3.Dot(offset,inward[i]);
                        var sideways=offset-inward[i]*along;
                        sideways.y=0;
                        Require(sideways.sqrMagnitude<.25f*.25f,"The swimmer detoured away from the nearest pool rim.");
                        jumped[i]|=animal.transform.position.y>deck+.5f&&Mathf.Abs(along)<1.4f;
                        if(swim.IsSwimming(animal))
                        {
                            Require(jumped[i]&&Vector3.Dot(animal.Character.SwimChest-rims[i],inward[i])>.8f,
                                "The swimmer did not jump into the water at the nearest edge.");
                            entered[i]=true;
                        }
                    }
                    if(swim.IsSwimming(animal))
                    {
                        var chest=animal.Character.SwimChest;
                        var rig=animal.transform.Find("Rabbit rig");
                        Require(park.ResortRiver.BlocksWalking(chest)&&
                            Mathf.Abs(chest.y-ParkResortRiver.WaterHeight)<.16f&&
                            Vector3.Dot(rig.up,-animal.transform.forward)>.92f,
                            "The swimmer left the surface or stood upright during its lap.");
                        crossed[i]|=chest.z>World(new Vector3(0,0,pool.y+5)).z;
                    }
                    else if(crossed[i]&&!finished[i]&&animal.ActiveAction==ParkAnimalAction.None)
                    {
                        Require(Vector3.Distance(animal.transform.position,exits[i])<.025f,
                            "The swimmer did not climb back onto the deck.");
                        finished[i]=true;
                    }
                }
                if(!photographed&&swim.IsSwimming(animals[0])&&swim.IsSwimming(animals[1])&&animals[0].Character.SwimChest.z>-5.2f*scale)
                {
                    photographed=true;
                    var animal=animals[0];
                    yield return CaptureVenueView(park,"resort-pool-swimming.png",animal.Character.SwimChest+Vector3.up*.27f,
                        animal.transform.right*4.8f-animal.transform.forward*4.7f+Vector3.up*2.1f);
                }
            }
            Require(photographed&&finished[0]&&finished[1],"Two swimmers did not complete their pool laps and return to the stairs.");
            Require(joggingSpread>walkingSpread*1.75f&&peakSpeed>2.90f&&arrivalSpeed is >.45f and <1.15f,
                $"The action approach did not widen its steps and slow at the pool rim. Foot spread {walkingSpread:F2} to {joggingSpread:F2}; speed {peakSpeed:F2} to {arrivalSpeed:F2}.");
            Debug.Log($"ACTION APPROACH GAIT PASS: foot spread {walkingSpread:F2} to {joggingSpread:F2}; speed {peakSpeed:F2} to {arrivalSpeed:F2} at the pool rim.");
            foreach(var animal in animals)
            {
                animal.gameObject.SetActive(false);
                Destroy(animal.gameObject);
            }
            Debug.Log("RESORT SWIMMING PASS: two animals jump in from the nearest edges, swim at the surface, and exit by the steps.");
        }

        static void CheckResortSunbeds(ParkPlayground park)
        {
            const ParkAnimalAction action=ParkAnimalAction.Sunbed;
            const float step=.05f;
            var beds=park.ResortSunbeds;
            Require(beds&&beds.Capacity==3&&beds.transform.childCount==3&&Array.IndexOf(park.AnimalActions,action)>=0&&
                ParkLanguage.Translate(ParkPlayground.ActionLabel(action),false)=="Use Sunbed",
                "The pool needs three wide sunbeds and their localized action.");
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            var sample=new Mesh{name="Sunbed contact probe"};
            foreach(var kind in new[]{AnimalKind.Giraffe,AnimalKind.Crocodile})
            {
                Require(park.TrySubmit((int)kind,paint,paint),$"Could not submit a {kind} sunbed probe.");
                for(int i=0;i<400&&(park.Animals.Count==0||!park.CanFollowAnimal(park.Animals[0]));i++)park.Advance(step);
                Require(park.Animals.Count==1&&park.CanFollowAnimal(park.Animals[0]),$"The {kind} did not arrive.");
                var animal=park.Animals[0];
                park.OrbitCamera.ToggleAnimalFollow(animal);
                Require(park.RequestAnimalAction(action),$"The {kind} could not request a sunbed.");
                bool usedBed=false,checkedPose=false;
                float restingTime=0;
                for(int i=0;i<1000;i++)
                {
                    park.Advance(step);
                    usedBed|=animal.ActiveAction==action;
                    if(beds.IsResting(animal))
                    {
                        restingTime+=step;
                        if(!checkedPose)
                        {
                            var skin=animal.Character.Skin;
                            skin.BakeMesh(sample,false);
                            sample.RecalculateBounds();
                            var bounds=sample.bounds;
                            Require(Mathf.Abs(Vector3.Dot(skin.rootBone.up,Vector3.up))<.02f&&
                                bounds.min.y is >=-.025f and <=.07f&&
                                bounds.size.x<2.92f*.5f+.04f&&bounds.size.z<6.05f*.5f+.04f,
                                $"The {kind} did not fit flat against the sunbed cushion: {bounds}.");
                            checkedPose=true;
                        }
                    }
                    if(usedBed&&animal.ActiveAction==ParkAnimalAction.None)break;
                }
                Require(restingTime is >=4.94f and <=5.10f&&checkedPose&&animal.ActiveAction==ParkAnimalAction.None&&
                    Mathf.Abs(animal.transform.position.y-park.SurfaceHeight(animal.transform.position))<.025f&&
                    park.IsSceneryClear(animal.transform.position),$"The {kind} did not rest for five seconds and stand up on the deck.");
                park.ReturnToModeSelection();
                park.ShowPark();
            }
            Destroy(sample);
            Debug.Log("RESORT SUNBEDS PASS: three wide loungers, giraffe and crocodile fit, five seconds reclining, and return to the deck.");
        }

        static void CheckResortAutoActivities(ParkPlayground park)
        {
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            Require(park.Animals.Count==0&&park.TrySubmit((int)AnimalKind.Rabbit,paint,paint),
                "Could not submit a swimmer for the automatic pool activity probe.");
            ParkRabbitAgent animal=null;
            var automaticActions=park.AutomaticAnimalActions;
            var triedActions=new HashSet<ParkAnimalAction>();
            bool riding=false,walked=false,hopped=false,danced=false;
            int startedRides=0;
            float rideSeconds=0,groundSeconds=0;
            float firstRideTime=0;
            const int rideSample=30;
            const float dt=.05f;
            for(int frame=0;frame<36000;frame++)
            {
                float previousDistance=animal?animal.WalkedDistance:0;
                park.Advance(dt);
                if(park.Animals.Count==0)continue;
                animal=park.Animals[0];
                var action=animal.ActiveAction;
                bool reserved=Array.IndexOf(automaticActions,action)>=0;
                if(reserved&&!riding)
                {
                    if(++startedRides==1)firstRideTime=frame*dt;
                    if(startedRides>rideSample)break;
                    triedActions.Add(action);
                }
                riding=reserved;
                if(startedRides==0)continue;
                if(animal.IsUsingRide)rideSeconds+=dt;
                else groundSeconds+=dt;
                walked|=action==ParkAnimalAction.None&&animal.WalkedDistance>previousDistance;
                hopped|=action==ParkAnimalAction.Hop;
                danced|=action==ParkAnimalAction.Dance;
            }
            Require(firstRideTime<16&&startedRides==rideSample+1&&triedActions.SetEquals(automaticActions)&&walked&&hopped&&danced,
                "The resort animal did not mix all pool activities with walking, hopping and dancing.");
            float rideShare=rideSeconds/(rideSeconds+groundSeconds);
            float target=automaticActions.Length/(automaticActions.Length+1f);
            Require(Mathf.Abs(rideShare-target)<.07f,$"The resort ride share missed its {target:P0} target: {rideShare:P1}.");
            var requested=animal.ActiveAction==ParkAnimalAction.Tube?ParkAnimalAction.Swim:ParkAnimalAction.Tube;
            Require(animal.RequestAction(requested),"The resort animal did not accept a manual action during its automatic approach.");
            park.Advance(dt);
            Require(animal.ActiveAction==requested&&animal.PendingAction==ParkAnimalAction.None,
                "A manual pool action did not take priority over the automatic choice.");
            park.ReturnToModeSelection();
            park.ShowPark();
            Debug.Log($"RESORT AUTOMATIC ACTIVITIES PASS: {rideShare:P1} on rides over {rideSample} rides; all pool behaviors and manual {requested} priority verified.");
        }

        IEnumerator CaptureVenueView(ParkPlayground park,string name,Vector3 focus,Vector3 offset)
        {
            var orbit=park.OrbitCamera;
            var camera=Camera.main;
            var position=camera.transform.position;
            var rotation=camera.transform.rotation;
            orbit.enabled=false;
            camera.transform.position=focus+offset;
            camera.transform.LookAt(focus);
            yield return Capture(name);
            camera.transform.SetPositionAndRotation(position,rotation);
            orbit.enabled=true;
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
            Require(park.ThemeName=="사막"&&desert.Root.activeInHierarchy&&desert.Preview&&park.Houses.Length==3&&
                Mathf.Abs(park.WalkArea.width-44.8f)<.01f&&Mathf.Abs(park.WalkArea.height-29.6f)<.01f,
                "The compact desert did not activate with three entrances.");
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
            Camera.main.transform.position=desert.Root.transform.TransformPoint(new Vector3(-5.8f,9.7f,-16.2f));
            Camera.main.transform.LookAt(pond+Vector3.up*.75f);
            yield return Capture("desert-oasis.png");
            orbit.enabled=true;
            Debug.Log("OASIS PASS: enlarged basin, translucent shallows, submerged stones, and dry access around five palms.");
            var front=park.Drawing.Front.Paint.EncodeToPNG();
            var back=park.Drawing.Back.Paint.EncodeToPNG();
            for(int index=0;index<2;index++)
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
                    $"Dune entrance {index + 1} is exposed or obstructed. Hidden: {hiddenByDune}, trailhead: {landing}, clear: {park.IsSceneryClear(landing)}.");
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
                Require(crest>4.5f*desert.Root.transform.lossyScale.y&&firstWave>0&&firstWave<=5.6f&&agent.WaveCount==1&&
                    agent.State==ParkRabbitState.Wandering&&!house.IsBusy,
                    "The animal did not cross the dune, wave, and continue walking.");
                sample.SetActive(false);
                Destroy(sample);
            }
            Debug.Log("DESERT PASS: both entrances cross the dune at terrain height and wave at its foot within 5.6 seconds.");
            yield return CheckDesertTentArrivals(park,front,back);
        }

        IEnumerator CheckDesertTentArrivals(ParkPlayground park,byte[] front,byte[] back)
        {
            var tent=park.Houses[2];
            var camp=park.DesertCamp;
            Require(camp&&tent.CampTent==camp&&ParkLanguage.Translate(tent.DisplayName,false)=="the tent"&&
                tent.transform.InverseTransformPoint(tent.Inside.position).z>0&&
                park.IsSceneryClear(tent.Welcome.position)&&park.SandSled.CanWanderAt(tent.Welcome.position),
                "The tent arrival must start inside and finish on clear sand on the campsite side.");

            var camper=PrepareRideProbe(park,AnimalKind.Rabbit);
            var approach=camp.TentEntrance+Vector3.back*.26f;
            approach.y=park.SurfaceHeight(approach);
            camper.transform.position=approach;
            Require(camper.RequestAction(ParkAnimalAction.Camp),"The tent occupancy probe could not request camping.");
            camper.Advance(.05f);
            Require(camp.TentReserved&&!tent.CanReserve,"An arrival could reserve the tent while a camper had it.");
            camper.gameObject.SetActive(false);
            Require(!camp.TentReserved&&tent.CanReserve,"Canceling the campground visit did not free the tent.");

            Require(park.Animals.Count==0,"The desert test needs an empty venue.");
            for(int i=0;i<4;i++)
                Require(park.TrySubmit((int)(i==2?AnimalKind.Giraffe:AnimalKind.Rabbit),front,back),
                    "The simultaneous desert arrival could not be queued.");
            park.Advance(.05f);
            Require(park.Animals.Count==3&&park.PendingCount==1&&
                park.Animals[0].HouseIndex==0&&park.Animals[1].HouseIndex==1&&park.Animals[2].HouseIndex==2,
                "Three simultaneous animals did not reserve the two dunes and tent independently.");
            var tentGuest=park.Animals[2];
            Require(tent.IsBusy&&tentGuest.State==ParkRabbitState.OpeningDoor&&
                Vector3.Distance(tentGuest.transform.position,tent.Inside.position)<.02f,
                "The third animal did not start inside the reserved tent.");
            camper.gameObject.SetActive(true);
            Require(camper.RequestAction(ParkAnimalAction.Camp),"The campsite could not be requested during arrivals.");
            camper.Advance(.05f);
            Require(camper.ActiveAction==ParkAnimalAction.Camp&&!camp.TentReserved,
                "A camper should go straight to the picnic while an arrival uses the tent.");
            camper.gameObject.SetActive(false);
            Destroy(camper.gameObject);

            bool photographed=false,waved=false;
            for(int step=0;step<380;step++)
            {
                park.Advance(.05f);
                if(tentGuest.State==ParkRabbitState.WalkingOut)
                {
                    var point=tentGuest.transform.position;
                    Require(Mathf.Abs(point.y-park.SurfaceHeight(point))<.015f,"A tent arrival left the ground.");
                    float depth=tent.transform.InverseTransformPoint(point).z;
                    if(!photographed&&depth is < -2.1f and > -2.8f)
                    {
                        photographed=true;
                        yield return CaptureVenueView(park,"desert-tent-arrival.png",
                            tent.Threshold.position+Vector3.up*.95f,new Vector3(-4.5f,2.8f,-7.6f));
                    }
                }
                else if(!waved&&tentGuest.State==ParkRabbitState.Waving)
                {
                    waved=true;
                    Require(Vector3.Distance(tentGuest.transform.position,tent.Welcome.position)<.025f,
                        "The tent arrival waved before reaching its greeting spot.");
                }
                if(waved&&park.Animals.Count==4&&park.Animals[3].WaveCount==1)break;
            }
            Require(photographed&&waved&&park.PendingCount==0&&park.Animals.Count==4&&
                tentGuest.State==ParkRabbitState.Wandering&&tentGuest.ExitedThroughOpenDoor,
                "The tent arrival or the next queued arrival did not finish its greeting.");
            foreach(var animal in park.Animals)
                Require(animal.WaveCount==1,"A queued desert animal missed its greeting.");
            park.ReturnToModeSelection();
            park.ShowPark();
            Debug.Log("DESERT TENT ARRIVALS PASS: two dunes and the tent received simultaneous animals with separate camping and arrival access.");
        }
        IEnumerator CheckDesertCamp(ParkPlayground park)
        {
            var camp=park.DesertCamp;
            string label=ParkPlayground.ActionLabel(ParkAnimalAction.Camp);
            Require(camp&&park.AnimalActions.Length==2&&Array.IndexOf(park.AnimalActions,ParkAnimalAction.Camp)>=0&&
                ParkLanguage.Translate(label,true)=="캠핑하기"&&ParkLanguage.Translate(label,false)=="Go Camping",
                "The desert campsite or its localized action is missing.");
            var campRoot=camp.transform;
            var tent=campRoot.Find("Teal A-frame tent");
            var mat=campRoot.Find("Picnic circle");
            var fire=mat?mat.Find("Stone-ring campfire"):null;
            var flames=fire?fire.Find("Flickering flames"):null;
            var grill=fire?fire.Find("Skewers over the fire"):null;
            var lantern=campRoot.GetComponentInChildren<Light>();
            var cookie=campRoot.Find("Shared cookie");
            Require(tent&&mat&&lantern&&cookie&&tent.position.x>park.Venues[park.VenueIndex].PondRadii.x&&
                !park.IsSceneryClear(tent.position)&&!park.IsSceneryClear(mat.position)&&park.IsSceneryClear(camp.TentEntrance),
                "The tent or mat is misplaced, has no navigation footprint, or has a blocked entrance.");
            Require(fire&&flames&&fire.position.z<tent.position.z&&flames.GetComponentsInChildren<MeshRenderer>().Length>0,
                "The campfire is missing from the center of the seating circle in front of the tent.");
            Require(grill&&grill.Find("Meat skewer 1")&&grill.Find("Meat skewer 2"),
                "The two meat skewers are missing from the fire.");
            var flameMeshes=flames.GetComponentsInChildren<MeshRenderer>();
            float skewerHeight=Mathf.Min(grill.Find("Meat skewer 1").position.y,grill.Find("Meat skewer 2").position.y);
            foreach(var renderer in campRoot.GetComponentsInChildren<MeshRenderer>(true))
                if(camp.IsMovingPart(renderer.transform))
                    Require(!renderer.isPartOfStaticBatch,"An animated campsite prop was batched as scenery.");
            var animals=new[]{PrepareRideProbe(park,AnimalKind.Giraffe),PrepareRideProbe(park,AnimalKind.Bear),
                PrepareRideProbe(park,AnimalKind.Rabbit)};
            var starts=new[]{camp.TentEntrance+Vector3.back*.26f,
                campRoot.TransformPoint(new Vector3(17.2f,0,-7.4f)),
                campRoot.TransformPoint(new Vector3(12.8f,0,-10.65f))};
            var colors=new[]{new Color32(166,118,203,255),new Color32(250,202,95,255),new Color32(114,206,174,255)};
            var paint=new Texture2D(2,2);
            for(int i=0;i<animals.Length;i++)
            {
                var animal=animals[i];
                paint.SetPixels32(new[]{colors[i],colors[i],colors[i],colors[i]});
                paint.Apply();
                var bytes=paint.EncodeToPNG();
                animal.Character.ApplyPainting(bytes,bytes);
                var point=starts[i];
                point.y=park.SurfaceHeight(point);
                animal.transform.position=point;
                Require(park.IsSceneryClear(point)&&animal.RequestAction(ParkAnimalAction.Camp),
                    "The camper could not begin from a clear path.");
            }
            Destroy(paint);
            const float step=.05f;
            var reserved=new bool[animals.Length];
            var seatedTime=new float[animals.Length];
            var initialFlames=flames.localScale;
            bool passed=false,lit=false,photo=false,firePhoto=false,flickered=false;
            int seated=0,peekFrames=0,sharingFrames=0;
            for(int frame=0;frame<750;frame++)
            {
                foreach(var animal in animals)animal.Advance(step);
                camp.Advance(step);
                Require(Array.TrueForAll(flameMeshes,mesh=>mesh.bounds.max.y<skewerHeight-.025f),
                    "The flames reached the meat skewers.");
                for(int i=0;i<animals.Length;i++)
                {
                    var animal=animals[i];
                    reserved[i]|=animal.ActiveAction==ParkAnimalAction.Camp;
                    if(!camp.IsSeated(animal))continue;
                    seatedTime[i]+=step;
                    var inward=Vector3.ProjectOnPlane(fire.position-animal.transform.position,Vector3.up);
                    Require(inward.magnitude is >1.6f and <1.78f&&Vector3.Dot(inward.normalized,-animal.transform.forward)>.98f,
                        "A seated camper is too far from the fire ring or faces away from it.");
                }
                flickered|=Vector3.Distance(initialFlames,flames.localScale)>.06f;
                lit|=lantern.enabled;
                passed|=camp.IsSharingSnack&&cookie.gameObject.activeInHierarchy;
                seated=Mathf.Max(seated,camp.SeatedCount);
                if(camp.IsPeeking(animals[0])&&++peekFrames==18)
                    yield return CaptureVenueView(park,"desert-camp-peek.png",tent.position+Vector3.up*1.05f,
                        new Vector3(-3.6f,2.9f,-7.4f));
                sharingFrames=camp.IsSharingSnack?sharingFrames+1:0;
                if(!photo&&sharingFrames==22)
                {
                    photo=true;
                    yield return CaptureVenueView(park,"desert-camp-picnic.png",(tent.position+mat.position)*.5f+Vector3.up*.9f,
                        new Vector3(-9f,7f,-8.5f));
                }
                if(!firePhoto&&camp.SeatedCount==3&&seatedTime[0]>1)
                {
                    firePhoto=true;
                    yield return CaptureVenueView(park,"desert-campfire.png",mat.position+Vector3.up*.75f,
                        new Vector3(-7.6f,7.2f,-8.5f));
                    yield return CaptureVenueView(park,"desert-grilling-skewers.png",fire.position+Vector3.up*.54f,
                        new Vector3(-4.3f,3.8f,-2.8f));
                }
                if(Array.TrueForAll(reserved,value=>value)&&Array.TrueForAll(animals,animal=>animal.ActiveAction==ParkAnimalAction.None))break;
            }
            Require(peekFrames>=18&&seated==3&&lit&&passed&&photo&&!lantern.enabled&&!cookie.gameObject.activeSelf,
                $"Camping did not complete the tent peek, three seats, lit lantern and snack exchange (peek {peekFrames}, seats {seated}, lit {lit}, exchange {passed}, photo {photo}).");
            Require(firePhoto&&flickered&&Array.TrueForAll(seatedTime,time=>Mathf.Abs(time-15.6f)<.11f),
                $"Campfire flicker or seated time did not match the activity: {string.Join(", ",seatedTime)} seconds.");
            foreach(var animal in animals)
            {
                Require(animal.ActiveAction==ParkAnimalAction.None&&park.IsSceneryClear(animal.transform.position)&&
                    Mathf.Abs(animal.transform.position.y-park.SurfaceHeight(animal.transform.position))<.015f,
                    "A camper did not return to the walkable sand.");
                animal.gameObject.SetActive(false);
                Destroy(animal.gameObject);
            }
            Debug.Log("DESERT CAMP PASS: three campers faced the animated campfire for 15.6 seconds and returned to dry paths.");
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
            float scale=ride.transform.lossyScale.y;
            Require(ride.CrestViewPoint.y is >12.5f and <14.5f&&
                ParkDuneTerrain.Height(crest.x,crest.y)>ParkDuneTerrain.Height(-12.4f,24f)*2,
                "The lowered sled crest is outside its expected range or too close to the nearby dune height.");
            var landing=ParkDuneTerrain.SledLanding;
            var landingSide=ParkDuneTerrain.SledDescentSide*3.45f;
            var left=ride.transform.TransformPoint(new Vector3(landing.x-landingSide.x,0,landing.y-landingSide.y));
            var right=ride.transform.TransformPoint(new Vector3(landing.x+landingSide.x,0,landing.y+landingSide.y));
            Require(!ride.IsInDescentLane(left)&&!ride.IsInDescentLane(right)&&!ride.CanWanderAt(left)&&ride.CanWanderAt(right)&&
                !park.WalkingSegmentClear(left,right),"A pedestrian can cross the sled landing lane.");
            right.y=park.SurfaceHeight(right);
            Require(ride.CanWanderAt(ride.Approach)&&park.TryBuildRidePath(right,ride.Approach,out _),
                "The sled exit could not reach pickup without crossing the descent.");

            var house=park.Houses[0];
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            const float step=.05f;
            var animals=new ParkRabbitAgent[3];
            var sledQueue=new Vector3[animals.Length];
            var started=new bool[animals.Length];
            var hidden=new bool[animals.Length];
            var revealed=new bool[animals.Length];
            var finished=new bool[animals.Length];
            var fadeSamples=new int[animals.Length];
            var revealSamples=new int[animals.Length];
            var fadeVisibility=new[]{1f,1f,1f};
            var sleds=new Transform[animals.Length];
            var movingParts=new MeshRenderer[animals.Length][];
            var fadeProperty=Shader.PropertyToID("_AnimalVisibility");
            var properties=new MaterialPropertyBlock();
            var pickupParts=ride.Sled.parent.GetComponentsInChildren<MeshRenderer>();
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
                var position=ride.Approach+new Vector3(1.1f+i*1.1f,0,-1.55f);
                position.y=park.SurfaceHeight(position);
                animal.transform.position=position;
                sledQueue[i]=position;
                Require(animal.State==ParkRabbitState.Wandering&&park.TryBuildRidePath(position,ride.Approach,out _)&&
                    animal.RequestAction(ParkAnimalAction.SandSled),"The animal could not reach or request the sand sled.");
                animals[i]=animal;
            }

            float pullSeconds=0,peak=0;
            int completed=0,maxSleds=0;
            bool towPhoto=false,fadePhoto=false,revealPhoto=false,slidePhoto=false,multiplePhoto=false,slopePhoto=false;
            for(int frame=0;frame<2000&&completed<animals.Length;frame++)
            {
                for(int i=0;i<animals.Length;i++)
                {
                    var animal=animals[i];
                    // Keep the test group near the pickup until each reservation is accepted.
                    if(!started[i]&&animal.PendingAction==ParkAnimalAction.SandSled)
                        animal.transform.position=sledQueue[i];
                    animal.Advance(step);
                }
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
                            park.IsSceneryClear(animal.transform.position)&&ride.CanWanderAt(animal.transform.position),
                            "The animal or its sled was left in the landing lane.");
                        finished[i]=true;
                        completed++;
                        continue;
                    }
                    Require(sled!=ride.Sled&&(!started[i]||sleds[i]==sled),"A rider lost its own sled.");
                    if(!started[i])movingParts[i]=sled.parent.GetComponentsInChildren<MeshRenderer>();
                    started[i]=true;
                    sleds[i]=sled;
                    var position=sled.position;
                    float opacity=ReadVisibility(animal.Character.Skin);
                    Require(Array.TrueForAll(movingParts[i],part=>Mathf.Abs(ReadVisibility(part)-opacity)<.001f&&
                        part.sharedMaterial.IsKeywordEnabled("PEACE_PARK_ANIMAL_FADING")),
                        "The rider, sled and rope did not fade together.");
                    if(ride.IsPulling(animal)||animal.IsHiddenForRide)
                    {
                        Require(opacity<=fadeVisibility[i],"The sled fade-out reversed.");
                        fadeVisibility[i]=opacity;
                        if(opacity>0&&opacity<1)
                        {
                            fadeSamples[i]++;
                            if(!fadePhoto&&opacity is >.35f and <.65f)
                            {
                                fadePhoto=true;
                                yield return CaptureVenueView(park,"desert-sled-fade.png",
                                    (position+animal.transform.position)*.5f+Vector3.up*.7f,new Vector3(-4.5f,3.4f,-6f));
                            }
                        }
                    }
                    if(animal.IsHiddenForRide)
                    {
                        Require(!animal.Character.Skin.enabled&&!sled.gameObject.activeInHierarchy,
                            "An animal or its sled was visible during the shortcut.");
                        Require(opacity==0&&fadeSamples[i]>=6,"The sled shortcut hid the rider before a visible fade.");
                        hidden[i]=true;
                        continue;
                    }
                    if(ride.IsRevealing(animal))
                    {
                        Require(hidden[i]&&animal.Character.Skin.enabled&&sled.gameObject.activeInHierarchy,
                            "The hidden sled did not enter the crest reveal.");
                        Require((revealed[i]||opacity==0)&&opacity>=fadeVisibility[i]&&
                            Vector3.ProjectOnPlane(position-ride.CrestViewPoint,Vector3.up).sqrMagnitude<.01f,
                            "The sled did not fade in from transparent while stopped at the crest.");
                        fadeVisibility[i]=opacity;
                        if(opacity>0&&opacity<1)revealSamples[i]++;
                        revealed[i]=true;
                    }
                    else if(ride.IsSliding(animal))
                    {
                        Require(revealed[i]&&revealSamples[i]>=6&&opacity==1,
                            "The sled started descending before a complete fade-in.");
                        CheckDescentAlignment(park,sled);
                    }
                    else Require(Mathf.Abs(position.y-park.SurfaceHeight(position)-.022f)<.035f,"The sled left the sand surface.");
                    for(int other=0;other<i;other++)
                    {
                        var leading=ride.SledFor(animals[other]);
                        if(leading&&leading.gameObject.activeInHierarchy)
                            Require(Vector3.Distance(leading.position,position)>2.85f*scale,"Two moving sand sleds touched.");
                    }
                    if(i!=0) continue;
                    peak=Mathf.Max(peak,position.y);
                    if(ride.IsPulling(animal))
                    {
                        pullSeconds+=step;
                        var feet=animal.transform.position;
                        Require(Mathf.Abs(feet.y-park.SurfaceHeight(feet))<.016f,"The animal floated while pulling the sled.");
                        if(!towPhoto&&pullSeconds>.4f)
                        {
                            towPhoto=true;
                            yield return Capture("desert-sled-side-path.png");
                        }
                    }
                    if(!revealPhoto&&ride.IsRevealing(animal)&&opacity is >.4f and <.65f)
                    {
                        revealPhoto=true;
                        yield return CaptureVenueView(park,"desert-sled-crest-reveal.png",
                            animal.transform.position+Vector3.up*1.15f,new Vector3(-6f,5.5f,7.5f));
                    }
                    if(!ride.IsPulling(animal)&&peak>15*scale&&position.y<8.5f*scale&&!slidePhoto)
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
                if(!multiplePhoto&&ride.ActiveSledCount==animals.Length&&peak>11*scale)
                {
                    multiplePhoto=true;
                    yield return Capture("desert-sled-multiple.png");
                }
            }
            Require(completed==animals.Length&&maxSleds==animals.Length&&towPhoto&&revealPhoto&&slidePhoto&&multiplePhoto&&slopePhoto&&
                pullSeconds>0&&pullSeconds<2.5f&&peak>16*scale&&ride.ActiveSledCount==0,
                $"The three sleds did not complete overlapping rides (finished {completed}, peak count {maxSleds}, " +
                $"peak height {peak:F2}, pulled {pullSeconds:F2}s, active {ride.ActiveSledCount}, overlap captured {multiplePhoto}).");
            Require(fadePhoto&&Array.TrueForAll(pickupParts,part=>ReadVisibility(part)==1),
                "The parked sled faded or the moving sled never displayed intermediate visibility.");
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
            Debug.Log($"SAND SLED PASS: {completed} riders pulled for {pullSeconds:F1}s and shared the descent with separate sleds.");
            Debug.Log($"SAND SLED FADE PASS: rider, sled and rope faded out at the foot over {fadeSamples[0]} steps and in at the crest over {revealSamples[0]} steps.");
            Debug.Log("SAND SLED GRADE PASS: both runners followed the graded descent without heading drift, cross-slope, or clipping.");

            float ReadVisibility(Renderer renderer)
            {
                renderer.GetPropertyBlock(properties);
                return properties.HasFloat(fadeProperty)?properties.GetFloat(fadeProperty):1;
            }
        }

        static void CheckDescentAlignment(ParkPlayground park,Transform sled)
        {
            var direction=ParkDuneTerrain.SledDescentDirection;
            var heading=new Vector3(direction.x,0,direction.y);
            var flatForward=Vector3.ProjectOnPlane(sled.forward,Vector3.up).normalized;
            Require(Vector3.Dot(flatForward,heading)>.9999f&&Mathf.Abs(sled.right.y)<.002f&&sled.forward.y<.002f,
                "A sand sled slid sideways, banked, or pointed uphill on the descent.");
            var edge=sled.TransformVector(Vector3.right*1.6f);
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

        IEnumerator CheckFocusedSandSledReplay(ParkPlayground park)
        {
            int venue=Array.FindIndex(park.Venues,item=>item.SandSled);
            if(venue<0)yield break;
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
            var start=ride.Approach+Vector3.right*1.5f;
            start.y=park.SurfaceHeight(start);
            animal.transform.position=start;
            TapAnimal();
            Require(park.RequestAnimalAction(ParkAnimalAction.SandSled),"The selected animal could not request the first ride.");
            bool hidden=false,revealed=false,crest=false;
            for(int frame=0;frame<800&&!ride.IsDismounting(animal);frame++)
            {
                Advance();
                hidden|=animal.IsHiddenForRide;
                if(!revealed&&ride.IsRevealing(animal))
                {
                    Require(hidden,"The camera did not keep following through the hidden transfer.");
                    CheckLaunchFraming(1f);
                    revealed=true;
                }
                if(!crest&&ride.IsSliding(animal))
                {
                    CheckLaunchFraming(5f);
                    crest=true;
                    bool manual=park.ManualSimulation;
                    park.ManualSimulation=true;
                    orbit.enabled=false;
                    yield return Capture("desert-sled-focused-crest.png");
                    orbit.enabled=true;
                    park.ManualSimulation=manual;
                }
            }
            Require(revealed&&crest,"The selected rider did not appear at the crest in the ride camera.");
            Require(ride.IsDismounting(animal),"The first ride did not reach the landing.");
            Debug.Log("SAND SLED CREST CAMERA PASS: selected rider kept focus through the fade, framed 15 degrees rear-right and 45 degrees down.");

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
            Require(secondSled&&secondSled!=firstSled,$"The animal did not return for its next sled while selected. Position: {animal.transform.position}, action: {animal.ActiveAction}, pending: {animal.PendingAction}.");
            for(int frame=0;frame<800&&ride.SledFor(animal);frame++)Advance();
            Require(animal.ActiveAction==ParkAnimalAction.None&&animal.PendingAction==ParkAnimalAction.None&&
                park.RequestAnimalAction(ParkAnimalAction.SandSled),"The selected animal could not request a ride just after getting off.");
            for(int frame=0;frame<200&&!ride.SledFor(animal);frame++)Advance();
            Require(ride.SledFor(animal)&&ride.SledFor(animal)!=secondSled,"The selected animal did not take a fresh sled from the exit.");
            Debug.Log("FOCUSED SAND SLED REPLAY PASS: requests while stepping off and after dismount both start a new ride with the camera still following.");
            Debug.Log("ANIMAL HOVER PASS: the pointed-at animal gains a highlight and loses it when the pointer leaves.");
            var safe=Screen.safeArea;
            float scale=Mathf.Min(safe.height/900f,safe.width/665f);
            var row=ParkPlayground.ActionButtonRow(safe.width/scale,safe.height/scale,park.AnimalActions.Length);
            var close=new Vector2(safe.xMin+(row.xMax-row.height*.5f)*scale,safe.yMax-row.center.y*scale);
            Require(park.ToolbarContains(close)&&ParkLanguage.Translate("동물 따라가기 해제",false)=="Stop Following",
                "The rightmost follow-close control did not have a protected tap area and English label.");
            orbit.ReleaseAnimalFollow();
            Require(!orbit.FollowTarget&&orbit.IsReturning&&!park.ToolbarContains(close),
                "Releasing follow did not start a smooth return and hide the action row.");
            Debug.Log("FOLLOW CLOSE PASS: the rightmost action slot blocks camera input while following and disappears when follow is released.");

            park.OpenSettings();
            GameObject.Find("Park Settings Popup").transform.Find("시작 화면으로").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Require(park.IsSelectingMode&&park.Animals.Count==0&&ride.ActiveSledCount==0&&!orbit.FollowTarget,
                "Resetting the session left a sled ride or camera target active.");

            void CheckLaunchFraming(float tolerance)
            {
                var camera=Camera.main;
                var offset=Vector3.ProjectOnPlane(camera.transform.position-animal.transform.position,Vector3.up);
                var rear=Vector3.ProjectOnPlane(animal.transform.forward,Vector3.up);
                float angle=Vector3.SignedAngle(rear,offset,Vector3.up);
                float pitch=Mathf.Asin(-camera.transform.forward.y)*Mathf.Rad2Deg;
                var point=animal.transform.position+Vector3.up;
                var screen=camera.WorldToViewportPoint(point);
                Require(Mathf.Abs(angle+15)<tolerance&&Mathf.Abs(pitch-45)<.5f&&
                    screen.z>0&&screen.x is >.2f and <.8f&&screen.y is >.15f and <.85f,
                    $"The sled camera missed its framing: {angle:F1} degrees horizontally, {pitch:F1} degrees down, viewport {screen}.");
                for(int i=1;i<12;i++)
                {
                    var rayPoint=Vector3.Lerp(camera.transform.position,point,i/12f);
                    Require(rayPoint.y>park.SurfaceHeight(rayPoint),"The dune blocks the view of the selected rider at the crest.");
                }
            }

            void TapAnimal()
            {
                var point=animal.transform.position+Vector3.up*.65f;
                orbit.focus=point;
                orbit.distance=14;
                orbit.AdvanceAnimalFollow(step);
                var screen=Camera.main.WorldToScreenPoint(point);
                var properties=new MaterialPropertyBlock();
                orbit.UpdateAnimalHover(screen,true);
                animal.Character.Skin.GetPropertyBlock(properties);
                Require(properties.GetFloat("_AnimalHighlight")==1,"The animal under the pointer was not highlighted.");
                orbit.UpdateAnimalHover(screen,false);
                animal.Character.Skin.GetPropertyBlock(properties);
                Require(properties.GetFloat("_AnimalHighlight")==0,"The highlight stayed after the mouse left.");
                orbit.BeginAnimalTap(screen,-1);
                orbit.EndAnimalTap(screen,-1);
                Require(orbit.FollowTarget==animal,"The selected animal did not enter camera follow.");
            }

            void Advance()
            {
                park.Advance(step);
                orbit.AdvanceAnimalFollow(step);
                Require(orbit.FollowTarget==animal,"The camera lost the animal before it took another sled.");
                foreach(var pedestrian in park.Animals)
                    if(pedestrian.ActiveAction!=ParkAnimalAction.SandSled)
                        Require(!ride.IsInDescentLane(pedestrian.transform.position),"A pedestrian walked into the sled lane.");
            }
        }

        static void CheckChristmasDeerAvoidance(ParkPlayground park)
        {
            int venue=Array.FindIndex(park.Venues,item=>item.Deer?.Length>0);
            Require(venue>=0,"The deer venue is missing.");
            park.ChangeVenue(venue);
            var deer=park.Deer[0];
            var left=deer.BoardingPoint(park,-1);
            var right=deer.BoardingPoint(park,1);
            Require(!park.DeerPathClear(left,right)&&park.TryBuildRidePath(left,right,out var route,avoidDeer:true)&&route.Length>0,
                "The boarding planner walked through a deer instead of routing around it.");

            park.Advance(8);
            ParkRabbitAgent walker=null;
            foreach(var animal in park.Animals)
                if(animal.State==ParkRabbitState.Wandering&&animal.ActiveAction==ParkAnimalAction.None)
                {
                    walker=animal;
                    break;
                }
            Require(walker,"The square did not have a roaming animal for the avoidance probe.");
            walker.transform.position=deer.transform.position;
            Require(deer.AnimalPenetration(walker.transform.position)>.5f,"The overlap probe did not start near the deer body.");
            for(int i=0;i<90;i++)park.Advance(.05f);
            Require(deer.AnimalPenetration(walker.transform.position)<.002f,"The walking animal did not separate from the deer.");
            for(int i=0;i<160;i++)
            {
                park.Advance(.05f);
                foreach(var animal in park.Animals)
                {
                    if(animal.State==ParkRabbitState.OpeningDoor||animal.ActiveAction==ParkAnimalAction.Deer)continue;
                    foreach(var buck in park.Deer)
                        Require(buck.AnimalPenetration(animal.transform.position)<.004f,"A deer and a roaming animal crossed into each other.");
                }
            }
            Debug.Log("CHRISTMAS DEER AVOIDANCE PASS: boarding routes avoided the deer body, and a crowd mingled with walking deer without overlap.");
        }

        IEnumerator CheckChristmasDeer(ParkPlayground park)
        {
            var herd=park.Deer;
            Require(herd?.Length==3&&ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.Deer),false)=="Ride a Deer"&&
                Array.IndexOf(park.AnimalActions,ParkAnimalAction.Deer)>=0,"Three rideable deer or their action are missing from the square.");
            var kinds=new[]{AnimalKind.Rabbit,AnimalKind.Elephant,AnimalKind.Giraffe};
            const float dt=.05f;
            var animals=new ParkRabbitAgent[herd.Length];
            for(int i=0;i<herd.Length;i++)
            {
                var deer=herd[i];
                Require(deer.CanBoard&&deer.Seat.y>1.2f&&deer.Seat.y<2.7f,
                    $"The enlarged deer has an invalid back seat at {deer.Seat}.");
                var skin=deer.GetComponentInChildren<SkinnedMeshRenderer>();
                Require(skin&&skin.sharedMaterial.shader.name=="PeacePark/DeerCloth"&&skin.sharedMaterial.shader.isSupported&&
                    skin.sharedMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord1)==2&&
                    deer.GetComponentsInChildren<MeshRenderer>(true).Length==0,
                    "The red cloth was not projected directly onto the animated deer skin.");
                var probe=animals[i]=PrepareRideProbe(park,kinds[i]);
                var mark=deer.BoardingPoint(park,1);
                if(!park.WalkArea.Contains(new Vector2(mark.x,mark.z))||!park.IsSceneryClear(mark))mark=deer.BoardingPoint(park,-1);
                var start=mark+Vector3.back*.6f;
                probe.transform.position=park.IsSceneryClear(start)?start:mark;
                Require(probe.RequestAction(ParkAnimalAction.Deer),"The animal could not request a deer.");
            }
            var buck=herd[0];
            yield return CaptureVenueView(park,"clock-plaza-deer-cloth.png",buck.Seat-Vector3.up*.35f,
                buck.transform.right*5.1f+buck.transform.forward*1.3f+Vector3.up*1.6f);
            var assignments=new ParkRabbitAgent[herd.Length];
            var stopped=new Vector3[herd.Length];
            var travel=new float[herd.Length];
            var mounted=new float[herd.Length];
            int concurrent=0;
            bool photo=false;
            for(int frame=0;frame<900;frame++)
            {
                foreach(var animal in animals)animal.Advance(dt);
                int riding=0;
                for(int i=0;i<herd.Length;i++)
                {
                    var deer=herd[i];
                    var previous=deer.transform.position;
                    if(!assignments[i]&&deer.Rider)
                    {
                        assignments[i]=deer.Rider;
                        stopped[i]=previous;
                    }
                    deer.Advance(park,dt);
                    Require(park.IsSceneryClear(deer.transform.position),"A walking deer crossed the square scenery.");
                    if(deer.Rider&&mounted[i]==0)
                        Require(Vector3.Distance(stopped[i],deer.transform.position)<.002f,"A reserved deer kept walking during approach or boarding.");
                    if(!deer.IsRiding)continue;
                    riding++;
                    mounted[i]+=dt;
                    travel[i]+=Vector3.Distance(previous,deer.transform.position);
                    var bones=deer.Rider.Character.Skin.bones;
                    Require(Vector3.Distance((bones[4].position+bones[5].position)*.5f,deer.Seat)<.025f&&
                        Vector3.Dot(-deer.Rider.transform.forward,deer.transform.forward)>.98f,
                        "The rider moved off the deer's animated back or faced the wrong way.");
                }
                concurrent=Mathf.Max(concurrent,riding);
                if(!photo&&riding==herd.Length)
                {
                    photo=true;
                    yield return Capture("clock-plaza-three-mounted-deer.png");
                    var deer=Array.Find(herd,item=>item.Rider.Character.Kind==AnimalKind.Rabbit);
                    var orbit=park.OrbitCamera;
                    var camera=Camera.main.transform;
                    var original=new Pose(camera.position,camera.rotation);
                    bool tracking=orbit.enabled;
                    orbit.enabled=false;
                    camera.position=deer.Seat+deer.transform.right*5.7f+deer.transform.forward*2.3f+Vector3.up*2;
                    camera.LookAt(deer.Seat);
                    yield return Capture("clock-plaza-deer-ride.png");
                    camera.SetPositionAndRotation(original.position,original.rotation);
                    orbit.enabled=tracking;
                }
                bool finished=true;
                for(int i=0;i<herd.Length;i++)finished&=mounted[i]>0&&!herd[i].Rider;
                if(finished)break;
            }
            Require(concurrent==3&&photo,"The three deer could not carry separate animals at the same time.");
            for(int i=0;i<herd.Length;i++)
            {
                var deer=herd[i];
                var animal=assignments[i];
                Require(animal&&deer.CanBoard&&mounted[i]>=9.8f&&mounted[i]<=10.35f&&travel[i]>3&&
                    animal.ActiveAction==ParkAnimalAction.None&&park.IsSceneryClear(animal.transform.position)&&
                    Mathf.Abs(park.SurfaceHeight(animal.transform.position)-animal.transform.position.y)<.012f,
                    $"The deer ride did not end on clear ground after ten seconds. Mounted {mounted[i]:F1}s, moved {travel[i]:F1}.");
                animal.gameObject.SetActive(false);
                Destroy(animal.gameObject);
            }
            Debug.Log("CHRISTMAS DEER PASS: three deer stopped for separate riders, walked with seated animals for ten seconds, and dismounted on clear ground.");
        }

        IEnumerator CheckChristmasSkating(ParkPlayground park)
        {
            var rink=park.IceRink;
            Require(rink&&ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.Skate),false)=="Ice Skate"&&
                Array.IndexOf(park.AnimalActions,ParkAnimalAction.Skate)>=0,"The skating action or its localization is missing.");
            var surface=rink.transform.Find("Smooth blue ice");
            var gates=new[]{rink.transform.Find("Left rink gate"),rink.transform.Find("Right rink gate")};
            float back=rink.transform.TransformPoint(Vector3.forward*ParkIceRink.HalfDepth).z;
            Require(surface&&surface.GetComponent<Renderer>().sharedMaterial.shader.isSupported&&
                !park.IsSceneryClear(rink.transform.position)&&Mathf.Abs(park.WalkArea.yMin-back)<.3f&&
                Array.TrueForAll(gates,gate=>gate&&Array.TrueForAll(gate.GetComponentsInChildren<MeshRenderer>(),r=>!r.isPartOfStaticBatch)),
                "The ice, scenery boundary, or gates are missing or misplaced.");
            var iceBounds=surface.GetComponent<Renderer>().bounds;
            Require(Mathf.Abs(iceBounds.size.x-20f)<.02f&&Mathf.Abs(iceBounds.size.z-11.8f)<.02f&&
                Mathf.Abs(back+7.8f)<.02f&&park.IsSceneryClear(rink.transform.position+Vector3.right*11),
                "The ice and its footprint were not reduced by a third around the same entrance.");

            Paint(park.Drawing.Front,new Color32(242,151,131,255));
            Paint(park.Drawing.Back,new Color32(237,150,125,255));
            var rabbit=PrepareRideProbe(park,AnimalKind.Rabbit);
            Paint(park.Drawing.Front,new Color32(205,153,98,255));
            Paint(park.Drawing.Back,new Color32(182,132,84,255));
            var skaters=new[]{rabbit,PrepareRideProbe(park,AnimalKind.Bear)};
            for(int i=0;i<skaters.Length;i++)
            {
                skaters[i].transform.position=rink.Approach(2*i-1)+Vector3.forward*.42f;
                Require(skaters[i].RequestAction(ParkAnimalAction.Skate),"The animal could not request the rink.");
            }
            var spin=new float[2];
            var apex=new float[2];
            var airborne=new bool[2];
            var entered=new bool[2];
            var landedBackwards=new bool[2];
            var heading=new Vector3[2];
            var oldPosition=new Vector3[2];
            var oldYaw=new float[2];
            var skatingTime=new float[2];
            var quadrants=new int[2];
            var crossings=new int[2];
            float closestSpacing=float.PositiveInfinity;
            bool captured=false,gateOpened=false;
            const float dt=.025f;
            for(int i=0;i<2400;i++)
            {
                for(int j=0;j<skaters.Length;j++)
                {
                    oldPosition[j]=skaters[j].transform.position;
                    oldYaw[j]=skaters[j].transform.eulerAngles.y;
                    skaters[j].Advance(dt);
                }
                rink.Advance(dt);
                gateOpened|=Quaternion.Angle(gates[0].localRotation,Quaternion.identity)>65;
                if(Array.TrueForAll(skaters,skater=>skater.IsUsingRide&&rink.IsOnIce(skater.transform.position)))
                {
                    var spacing=Vector3.ProjectOnPlane(skaters[0].transform.position-skaters[1].transform.position,Vector3.up);
                    closestSpacing=Mathf.Min(closestSpacing,spacing.magnitude);
                }
                for(int j=0;j<skaters.Length;j++)
                {
                    var skater=skaters[j];
                    var p=skater.transform.position;
                    if(skater.IsUsingRide)skatingTime[j]+=dt;
                    if(skater.ActiveAction!=ParkAnimalAction.Skate||!rink.IsOnIce(p))continue;
                    var local=rink.transform.InverseTransformPoint(p);
                    var previousLocal=rink.transform.InverseTransformPoint(oldPosition[j]);
                    if(Mathf.Abs(local.x)>5&&Mathf.Abs(local.z)>3.3f)
                        quadrants[j]|=1<<((local.x>0?1:0)+(local.z>0?2:0));
                    if(Mathf.Sign(local.x)!=Mathf.Sign(previousLocal.x)&&Mathf.Abs(local.z)<.4f)
                        crossings[j]++;
                    entered[j]=true;
                    Require(skater.IsUsingRide&&Mathf.Abs(p.x)<ParkIceRink.HalfWidth-.6f&&
                        Mathf.Abs(p.z-rink.transform.position.z)<ParkIceRink.HalfDepth+.02f,"A skater left the ice or stopped playing during the lap.");
                    float lift=p.y-rink.SurfaceY-skater.transform.lossyScale.y*RabbitCharacter.SkateBladeDepth;
                    apex[j]=Mathf.Max(apex[j],lift);
                    var direction=Vector3.ProjectOnPlane(p-oldPosition[j],Vector3.up);
                    bool nowAirborne=lift>.003f;
                    if((nowAirborne||airborne[j])&&direction.sqrMagnitude>.000001f&&heading[j].sqrMagnitude>.000001f)
                        spin[j]+=Mathf.DeltaAngle(oldYaw[j],skater.transform.eulerAngles.y)-Vector3.SignedAngle(heading[j],direction,Vector3.up);
                    if(airborne[j]&&!nowAirborne)
                        landedBackwards[j]=Vector3.Dot(skater.transform.forward,direction.normalized)>.97f;
                    airborne[j]=nowAirborne;
                    heading[j]=direction;
                }
                if(!captured&&apex[0]>.95f&&apex[1]>.95f)
                {
                    captured=true;
                    foreach(var skater in skaters)
                    {
                        var bones=skater.Character.Skin.bones;
                        Require(bones[4].Find("Ice skate").gameObject.activeInHierarchy&&bones[5].Find("Ice skate").gameObject.activeInHierarchy,
                            "A skater did not wear both skates during the Axel.");
                    }
                    yield return Capture("clock-plaza-ice-rink.png");
                    yield return CaptureVenueView(park,"clock-plaza-triple-axel.png",rink.transform.position+Vector3.up*.8f,
                        new Vector3(.75f,13.5f,-15f));
                }
                if(entered[0]&&entered[1]&&Array.TrueForAll(skaters,skater=>skater.ActiveAction==ParkAnimalAction.None))break;
            }
            rink.Advance(1);
            Require(captured&&gateOpened&&Quaternion.Angle(gates[0].localRotation,Quaternion.identity)<.1f,
                "The rink did not open and close around the two skaters.");
            Require(closestSpacing>2f&&!float.IsInfinity(closestSpacing),
                $"Simultaneous skaters converged at the crossing: {closestSpacing:F2} units apart.");
            for(int i=0;i<skaters.Length;i++)
            {
                var skater=skaters[i];
                Require(Mathf.Abs(skatingTime[i]-35.6f)<.13f&&crossings[i]==4&&quadrants[i]==15,
                    $"Skater {i+1} did not complete two full figure eights in 35.6 seconds. Time={skatingTime[i]:F2}, crossings={crossings[i]}, quadrants={quadrants[i]}.");
                Require(apex[i]>1.3f&&Mathf.Abs(spin[i]-360*ParkIceRink.AxelTurns)<12&&landedBackwards[i]&&
                    skater.ActiveAction==ParkAnimalAction.None&&!rink.IsOnIce(skater.transform.position)&&
                    park.IsSceneryClear(skater.transform.position)&&Mathf.Abs(skater.transform.position.y-park.SurfaceHeight(skater.transform.position))<.01f,
                    $"Skater {i+1} did not complete the Axel and return to the plaza. Apex={apex[i]:F2}, rotation={spin[i]:F1}, backward={landedBackwards[i]}.");
                skater.gameObject.SetActive(false);
                Destroy(skater.gameObject);
            }
            Debug.Log($"CHRISTMAS SKATING PASS: two skaters crossed the gate, wore skates, completed {spin[0]/360:F2} / {spin[1]/360:F2} turns, and landed backwards.");
            Debug.Log($"CHRISTMAS FIGURE EIGHT PASS: two full eight-shaped circuits each, {skatingTime[0]:F2} / {skatingTime[1]:F2} seconds, and {closestSpacing:F2} units of separation.");
        }

        IEnumerator CheckCompactAmusementPark(ParkPlayground park)
        {
            var venue=park.Venues[park.VenueIndex].Root.transform;
            var track=venue.Find("Oval roller coaster track/Track bed").GetComponent<Renderer>();
            var wheelTop=park.Wheel.transform.TransformPoint(new Vector3(0,
                ParkThemeParkWheel.HubHeight+ParkThemeParkWheel.Radius+2.75f,ParkThemeParkWheel.CabinOffset));
            Require(Mathf.Abs(park.WalkArea.width-48)<.01f&&Mathf.Abs(park.OrbitCamera.distance-54)<.01f&&
                Camera.main.WorldToViewportPoint(wheelTop).y<.96f&&
                track.bounds.size.x is >73f and <76f&&
                Mathf.Abs(ParkThemeParkWheel.Radius*park.Wheel.transform.lossyScale.y-12.4f)<.01f&&
                park.Carousel.transform.lossyScale.x is >2.3f and <2.4f,
                "The amusement park, camera and attractions are not using the compact layout.");
            var gardens=venue.Find("Flower beds and park decorations");
            var trees=gardens.Find("Garden tree models");
            var plaza=gardens.Find("Central plaza flower beds");
            var entrance=gardens.Find("Entrance flower beds");
            Require(plaza&&plaza.childCount==6&&entrance&&entrance.childCount==4&&trees.childCount>0,
                "The amusement flower beds or perimeter trees are missing.");
            for(int pair=0;pair<3;pair++)
                CheckPair(plaza,pair,9.4f,-7.8f+pair*8f);
            for(int pair=0;pair<2;pair++)
                CheckPair(entrance,pair,17+pair*12f,-39.8f);
            foreach(Transform tree in trees)
                Require(!ParkElephantTrain.WithinTrackLoop(venue.InverseTransformPoint(tree.position)),
                    "A garden tree was planted inside the train loop.");

            static void CheckPair(Transform row,int index,float offset,float depth)
            {
                var left=row.GetChild(index*2).localPosition;
                var right=row.GetChild(index*2+1).localPosition;
                Require(left==new Vector3(-offset,0,depth)&&right==new Vector3(offset,0,depth),
                    "The amusement flower beds were not spaced as aligned pairs.");
            }

            var ride=park.Carousel;
            var platform=ride.transform.Find("Turning carousel platform");
            const float dt=.05f;
            var seats=new Transform[7];
            var lifts=new Transform[seats.Length];
            var poles=new Transform[seats.Length];
            var collars=new Transform[seats.Length];
            var low=new float[seats.Length];
            var high=new float[seats.Length];
            var muzzleNames=new[]{"Rosy muzzle","Biscuit muzzle","Cream mint muzzle","Lilac muzzle"};
            for(int i=0;i<seats.Length;i++)
            {
                var mount=platform.Find("Carousel horse "+(i+1));
                Require(mount,"The carousel is missing a horse mount.");
                var lift=mount.Find("Rising horse");
                lifts[i]=lift;
                seats[i]=lift.Find("Saddle "+(i+1));
                poles[i]=mount.Find("Support pole");
                collars[i]=lift.Find("Sliding pole collar");
                var muzzle=lift.Find(muzzleNames[i%muzzleNames.Length]).GetComponent<Renderer>();
                float muzzleZ=lift.InverseTransformPoint(muzzle.bounds.center).z;
                float poleZ=poles[i].localPosition.z;
                Require(poles[i]&&collars[i]&&seats[i]&&poleZ>seats[i].localPosition.z+.16f&&poleZ<muzzleZ-.12f&&
                    Array.TrueForAll(lift.GetComponentsInChildren<MeshRenderer>(),renderer=>!renderer.isPartOfStaticBatch),
                    "The pole is not between the saddle and muzzle, or the horse cannot move freely.");
                low[i]=high[i]=seats[i].position.y;
            }
            const float midpoint=.225f;
            float previousHeight=lifts[0].localPosition.y;
            float lastRisingCrossing=-1,measuredCycle=0;
            for(int frame=0;frame<160;frame++)
            {
                ride.Advance(dt);
                float oddHeight=lifts[0].localPosition.y;
                float evenHeight=lifts[1].localPosition.y;
                Require(Mathf.Abs(oddHeight+evenHeight-2*midpoint)<.0001f,
                    "Odd and even carousel horses did not move in opposite phases.");
                if(previousHeight<midpoint&&oddHeight>=midpoint)
                {
                    float time=(frame+1)*dt;
                    if(lastRisingCrossing>=0)measuredCycle=time-lastRisingCrossing;
                    lastRisingCrossing=time;
                }
                previousHeight=oddHeight;
                for(int i=0;i<seats.Length;i++)
                {
                    Require(Mathf.Abs(lifts[i].localPosition.y-(i%2==0?oddHeight:evenHeight))<.0001f,
                        "Carousel horses of the same parity did not stay synchronized.");
                    var difference=Vector3.ProjectOnPlane(collars[i].position-poles[i].position,Vector3.up);
                    var shaft=poles[i].GetComponentInChildren<MeshRenderer>().bounds;
                    Require(difference.sqrMagnitude<.000025f&&collars[i].position.y>shaft.min.y&&collars[i].position.y<shaft.max.y,
                        "A rising horse detached from its support shaft.");
                    low[i]=Mathf.Min(low[i],seats[i].position.y);
                    high[i]=Mathf.Max(high[i],seats[i].position.y);
                }
            }
            Require(Mathf.Abs(measuredCycle-3.75f)<.06f,$"The carousel lift cycle took {measuredCycle:F2} seconds instead of 3.75.");
            for(int i=0;i<seats.Length;i++)Require(high[i]-low[i]>.70f,"An empty carousel horse did not rise and fall.");

            var animal=PrepareRideProbe(park,AnimalKind.Rabbit);
            Require(animal.RequestAction(ParkAnimalAction.Carousel),"A rider could not request the carousel.");
            bool started=false,carried=false,captured=false;
            float lowestRider=float.PositiveInfinity,highestRider=float.NegativeInfinity;
            for(int frame=0;frame<2400;frame++)
            {
                var previous=platform.localRotation;
                animal.Advance(dt);
                ride.Advance(dt);
                started|=animal.ActiveAction==ParkAnimalAction.Carousel;
                if(started&&animal.transform.position.y>.7f&&Quaternion.Angle(previous,platform.localRotation)>.02f)
                {
                    carried=true;
                    var bones=animal.Character.Skin.bones;
                    var hips=(bones[4].position+bones[5].position)*.5f;
                    var seat=seats[0];
                    foreach(var candidate in seats)
                        if((candidate.position-hips).sqrMagnitude<(seat.position-hips).sqrMagnitude)seat=candidate;
                    Require(Vector3.Distance(hips,seat.position)<.025f,"The rider separated from the moving saddle.");
                    lowestRider=Mathf.Min(lowestRider,hips.y);
                    highestRider=Mathf.Max(highestRider,hips.y);
                    if(!captured&&highestRider-lowestRider>.42f)
                    {
                        captured=true;
                        var outward=Vector3.ProjectOnPlane(seat.position-ride.transform.position,Vector3.up).normalized;
                        yield return CaptureVenueView(park,"amusement-carousel-lift.png",seat.position+Vector3.up*.9f,
                            outward*7.7f-seat.forward*2.1f+Vector3.up*2.5f);
                    }
                }
                if(started&&animal.ActiveAction==ParkAnimalAction.None)break;
            }
            Require(carried&&captured&&highestRider-lowestRider>.70f&&animal.ActiveAction==ParkAnimalAction.None&&
                park.IsSceneryClear(animal.transform.position),
                "The compact carousel did not carry the animal and return it to the plaza.");
            animal.gameObject.SetActive(false);
            Destroy(animal.gameObject);
            Debug.Log("COMPACT AMUSEMENT PARK PASS: smaller attractions and track, closer overview, and a full carousel ride.");
            Debug.Log($"CAROUSEL LIFT PASS: seven poles between saddles and heads, riders seated throughout a {highestRider-lowestRider:F2}-unit rise and fall.");
            Debug.Log($"CAROUSEL RHYTHM PASS: two synchronized groups in opposite phases with a {measuredCycle:F2}-second cycle.");
        }

        static void CheckContinuousFerrisWheel(ParkPlayground park)
        {
            var ride=park.Wheel;
            var rotor=ride?ride.transform.Find("Grand Ferris wheel rotor"):null;
            Require(rotor,"The Ferris wheel rotor is missing.");
            const float dt=.05f;
            for(int frame=0;frame<400;frame++)
            {
                var previous=rotor.localRotation;
                ride.Advance(dt);
                Require(Quaternion.Angle(previous,rotor.localRotation)>.015f,"An empty Ferris wheel stopped at an empty cabin.");
            }
            var animal=PrepareRideProbe(park,AnimalKind.Rabbit);
            animal.transform.position=ride.BoardingPoint+Vector3.back*.8f;
            Require(animal.RequestAction(ParkAnimalAction.Wheel),"The animal could not request the Ferris wheel.");
            bool started=false;
            float degrees=0,stationary=0;
            for(int frame=0;frame<2400;frame++)
            {
                animal.Advance(dt);
                var previous=rotor.localRotation;
                ride.Advance(dt);
                float change=Quaternion.Angle(previous,rotor.localRotation);
                started|=animal.ActiveAction==ParkAnimalAction.Wheel;
                if(!started)continue;
                degrees+=change;
                if(change<.015f)stationary+=dt;
                if(animal.ActiveAction==ParkAnimalAction.None)break;
            }
            Require(started&&degrees>=350&&stationary>=5&&stationary<8&&animal.ActiveAction==ParkAnimalAction.None&&
                park.IsSceneryClear(animal.transform.position),"The wheel did not stop only to board and unload its passenger.");
            var stopped=rotor.localRotation;
            ride.Advance(.25f);
            Require(Quaternion.Angle(stopped,rotor.localRotation)>.03f,"The Ferris wheel remained stopped after its last passenger left.");
            animal.gameObject.SetActive(false);
            Destroy(animal.gameObject);
            Debug.Log("CONTINUOUS FERRIS WHEEL PASS: empty cabins passed continuously and the wheel stopped to board and unload a passenger.");
        }

        static ParkRabbitAgent PrepareRideProbe(ParkPlayground park,AnimalKind kind)
        {
            var house=park.Houses[0];
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            var probe=new GameObject(kind+" ride probe").AddComponent<ParkRabbitAgent>();
            probe.Initialize(park,house,0,0,paint,paint,false,kind);
            Require(house.TryReserve(),"The ride probe could not enter the venue.");
            const float dt=.05f;
            for(int i=0;i<Mathf.CeilToInt((house.ExitDuration+house.DoorDuration*2+3)/dt);i++)
            {
                house.Advance(dt);
                probe.Advance(dt);
            }
            Require(probe.State==ParkRabbitState.Wandering,"The ride probe did not finish greeting.");
            return probe;
        }

        static void CheckAutoAnimalFocus(ParkPlayground park)
        {
            var orbit=park.OrbitCamera;
            var camp=park.DesertCamp;
            var sled=park.SandSled;
            var defaultPosition=orbit.transform.position;
            var defaultRotation=orbit.transform.rotation;
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            camp.enabled=sled.enabled=false;
            for(int i=0;i<3;i++)Require(park.TrySubmit((int)AnimalKind.Rabbit,paint,paint,i),"Could not submit an automatic camera probe.");
            park.Advance(10f);
            camp.enabled=sled.enabled=true;
            Require(park.Animals.Count==3,"The three automatic camera probes did not arrive.");
            var first=park.Animals[0];
            var farther=park.Animals[1];
            var nearby=park.Animals[2];
            first.transform.position=OnSand(-4,-8);
            farther.transform.position=OnSand(13,-9);
            nearby.transform.position=OnSand(0,-8);

            orbit.ToggleAnimalFollow(first);
            StepCamera(300);
            Require(orbit.ManualFollowTarget==first,"The camera probe could not start manual follow.");
            float manualDistance=orbit.distance;
            orbit.ReleaseAnimalFollow();
            StepCamera(300);

            first.ReserveRide(camp,Array.Empty<Vector3>());
            farther.StartActivity(ParkRabbitAgent.Activity.Hop);
            nearby.StartActivity(ParkRabbitAgent.Activity.Dance);
            orbit.ToggleAutoFollow();
            StepCamera(1);
            Require(orbit.AutoFollowEnabled&&!orbit.FollowTarget,"Automatic focus picked an animal that was waiting, hopping or dancing.");

            first.BeginRideActivity();
            StepCamera(1);
            Require(orbit.FollowTarget==first&&!orbit.ManualFollowTarget&&
                Vector3.Distance(orbit.transform.position,defaultPosition)<.5f,"Automatic focus did not ease in when the activity started.");
            StepCamera(300);
            float ratio=orbit.distance/manualDistance;
            Require(Mathf.Abs(ratio-1.5f)<.01f,$"Automatic focus distance was {ratio:F2} times manual focus.");
            Require(!park.RequestAnimalAction(ParkAnimalAction.SandSled),"An automatic focus target could receive manual action requests.");

            farther.ReserveRide(camp,Array.Empty<Vector3>());
            farther.BeginRideActivity();
            nearby.ReserveRide(camp,Array.Empty<Vector3>());
            nearby.BeginRideActivity();
            StepCamera(1);
            Require(orbit.FollowTarget==first,"Automatic focus left an animal before its activity ended.");
            var beforeSwitch=orbit.transform.position;
            var aimBeforeSwitch=orbit.focus;
            first.EndRide();
            StepCamera(1);
            Require(orbit.FollowTarget==nearby&&!orbit.IsReturning&&Vector3.Distance(beforeSwitch,orbit.transform.position)<.5f,
                "Automatic focus did not transition smoothly to the closest animal in an activity.");
            StepCamera(35);
            var handoffAim=orbit.focus;
            StepCamera(300);
            var settledAim=orbit.focus;
            var previousAim=aimBeforeSwitch;
            var previousVelocity=Vector3.zero;
            for(int i=0;i<18;i++)
                previousAim=Vector3.SmoothDamp(previousAim,settledAim,ref previousVelocity,.32f,Mathf.Infinity,1f/60f);
            float travel=Vector3.Distance(aimBeforeSwitch,settledAim);
            Require(travel>.5f&&Vector3.Distance(handoffAim,previousAim)<travel*.01f,
                "After 0.6 seconds, the automatic handoff did not match 0.3 seconds of the previous camera motion.");
            Debug.Log("AUTO FOCUS HANDOFF PASS: the camera covered the same distance in 0.6 seconds as it previously did in 0.3 seconds.");
            nearby.EndRide();
            StepCamera(1);
            Require(orbit.FollowTarget==farther,"Automatic focus did not switch to the remaining eligible animal.");
            farther.EndRide();
            StepCamera(300);
            Require(orbit.AutoFollowEnabled&&!orbit.FollowTarget&&Vector3.Distance(orbit.transform.position,defaultPosition)<.02f&&
                Quaternion.Angle(orbit.transform.rotation,defaultRotation)<.1f,"Automatic focus did not wait at the venue overview.");

            first.ReserveRide(camp,Array.Empty<Vector3>());
            first.BeginRideActivity();
            StepCamera(1);
            Require(orbit.FollowTarget==first,"Automatic focus did not resume after waiting.");

            farther.ReserveRide(camp,Array.Empty<Vector3>());
            farther.BeginRideActivity();
            var beforeSelection=orbit.transform.position;
            orbit.ToggleAnimalFollow(farther);
            StepCamera(1);
            Require(orbit.AutoFollowEnabled&&orbit.FollowTarget==farther&&!orbit.ManualFollowTarget&&
                Vector3.Distance(orbit.transform.position,beforeSelection)<.5f,"Selecting another active animal interrupted automatic focus or snapped the camera.");
            StepCamera(300);
            Require(orbit.FollowTarget==farther&&Mathf.Abs(orbit.distance/manualDistance-1.5f)<.01f,
                "The chosen animal did not keep the automatic camera framing until its activity ended.");
            farther.EndRide();
            StepCamera(1);
            Require(orbit.FollowTarget==first,"Automatic focus did not resume after the selected animal finished.");

            orbit.ToggleAnimalFollow(nearby);
            StepCamera(180);
            Require(orbit.AutoFollowEnabled&&orbit.FollowTarget==nearby,"The automatic camera skipped a selected idle animal.");
            StepCamera(120);
            Require(orbit.FollowTarget==first,"The automatic camera did not return to an active animal after observing an idle animal.");
            orbit.ToggleAnimalFollow(nearby);
            StepCamera(60);
            nearby.ReserveRide(camp,Array.Empty<Vector3>());
            nearby.BeginRideActivity();
            StepCamera(300);
            Require(orbit.FollowTarget==nearby,"The automatic camera left a selected animal after it began an activity.");
            nearby.EndRide();
            StepCamera(1);
            Require(orbit.FollowTarget==first,"The automatic camera did not resume after the selected animal's new activity.");
            orbit.ToggleAnimalFollow(first);
            Require(orbit.AutoFollowEnabled&&orbit.FollowTarget==first,"Tapping the current target turned off automatic focus.");
            orbit.ToggleAutoFollow();
            Require(!orbit.AutoFollowEnabled&&!orbit.FollowTarget&&orbit.IsReturning,"Turning automatic focus off did not return to the overview.");
            park.ReturnToModeSelection();
            park.ShowPark();
            Debug.Log($"AUTO FOCUS PASS: selected active and idle animals without leaving automatic mode, resumed after activities, and tracked at {ratio:F2}x manual distance.");

            Vector3 OnSand(float x,float z)
            {
                var point=new Vector3(x,0,z);
                point.y=park.SurfaceHeight(point);
                return point;
            }

            void StepCamera(int frames)
            {
                for(int i=0;i<frames;i++)orbit.AdvanceAnimalFollow(1f/60f);
            }
        }

        static void CheckParkActivityMix(ParkPlayground park)
        {
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            Require(park.TrySubmit((int)AnimalKind.Rabbit,paint,paint),"Could not submit a park animal.");
            const float step=.05f;
            int walked=0,danced=0,hopped=0;
            for(int frame=0;frame<6000;frame++)
            {
                float previousDistance=park.Animals.Count>0?park.Animals[0].WalkedDistance:0;
                park.Advance(step);
                if(park.Animals.Count==0)continue;
                var animal=park.Animals[0];
                if(animal.State!=ParkRabbitState.Wandering)continue;
                if(animal.ActiveAction==ParkAnimalAction.Dance)danced++;
                else if(animal.ActiveAction==ParkAnimalAction.Hop)hopped++;
                else if(animal.WalkedDistance>previousDistance)walked++;
            }
            float movingFrames=walked+danced+hopped;
            Require(movingFrames*step>150,"The park animal did not roam long enough to measure its activity mix.");
            float walkShare=walked/movingFrames,danceShare=danced/movingFrames,hopShare=hopped/movingFrames;
            string shares=$"{walkShare:P1} walking, {danceShare:P1} dancing, {hopShare:P1} hopping";
            Require(Mathf.Abs(walkShare-.6f)<.02f&&Mathf.Abs(danceShare-.2f)<.02f&&Mathf.Abs(hopShare-.2f)<.02f,
                $"Park activity mix missed its 60/20/20 target: {shares}.");
            Debug.Log($"PARK ACTIVITY MIX PASS: {shares}.");
        }

        static void CheckVenueAutomaticActivities(ParkPlayground park,int sample)
        {
            var available=park.AnimalActions;
            Require(Array.IndexOf(available,ParkAnimalAction.Hop)<0&&
                Array.IndexOf(available,ParkAnimalAction.Dance)<0,
                "Ground animations should not be visible as action buttons.");
            var paint=park.Drawing.Front.Paint.EncodeToPNG();
            Require(park.Animals.Count==0&&park.TrySubmit((int)AnimalKind.Rabbit,paint,paint),
                "Could not submit an animal for the automatic activity probe.");
            const float step=.05f;
            var actions=new HashSet<ParkAnimalAction>();
            int started=0;
            float rides=0,ground=0;
            bool riding=false;
            for(int frame=0;frame<24000;frame++)
            {
                park.Advance(step);
                if(park.Animals.Count==0)continue;
                var animal=park.Animals[0];
                if(animal.IsUsingRide&&!riding)
                {
                    if(++started>sample)break;
                    actions.Add(animal.ActiveAction);
                }
                riding=animal.IsUsingRide;
                if(started==0)continue;
                if(riding)rides+=step;
                else ground+=step;
            }
            Require(started==sample+1&&actions.SetEquals(available),
                $"{park.ThemeName} did not automatically use all {available.Length} attractions in {started} rides.");
            float share=rides/(rides+ground);
            float target=available.Length/(available.Length+1f);
            Require(Mathf.Abs(share-target)<=.07f,$"{park.ThemeName} ride share missed its {target:P0} target: {share:P1}.");
            park.ReturnToModeSelection();
            park.ShowPark();
            Debug.Log($"VENUE AUTOMATIC ACTIVITIES PASS: {park.ThemeName}, {share:P1} riding over {sample} rides, targeting {target:P0}.");
        }

        IEnumerator CheckForestRides(ParkPlayground park)
        {
            var rides=park.ForestRides;
            var actions=new[]{ParkAnimalAction.StumpHop,ParkAnimalAction.WoodenBridge,ParkAnimalAction.Zipline};
            var english=new[]{"Hop Across Stumps","Cross the Log Bridge","Ride the Zipline"};
            Require(rides?.Length==3&&park.AnimalActions.Length==3&&rides[0].transform.childCount==5,
                "The forest is missing one of its three rides or five stepping stumps.");
            float stumpCenter=0;
            foreach(Transform stump in rides[0].transform)
            {
                var surface=stump.Find("Stump surface with annual rings");
                var renderer=surface?surface.GetComponent<MeshRenderer>():null;
                Require(renderer&&renderer.sharedMaterial.mainTexture is Texture2D texture&&
                    texture.mipmapCount>1&&stump.childCount==3,
                    "A stepping stump needs a single textured cut surface and its two bark detail meshes.");
                stumpCenter+=renderer.bounds.center.x/rides[0].transform.childCount;
            }
            var bridgeRenderers=rides[1].GetComponentsInChildren<MeshRenderer>();
            var bridgeBounds=bridgeRenderers[0].bounds;
            foreach(var renderer in bridgeRenderers)bridgeBounds.Encapsulate(renderer.bounds);
            Require(stumpCenter>-6f&&stumpCenter<-4.5f&&bridgeBounds.center.x>6.5f&&bridgeBounds.center.x<8f,
                "The stepping stumps or wooden bridge did not move inward.");
            float rockClearance=float.PositiveInfinity;
            var scenery=park.Venues[park.VenueIndex].Root.transform.Find("Ultimate Nature Starter scenery");
            foreach(Transform prop in scenery)
            {
                if(!prop.name.Contains("_Rock_"))continue;
                foreach(var renderer in prop.GetComponentsInChildren<MeshRenderer>())
                {
                    var rock=renderer.bounds;
                    float dx=Mathf.Max(0,Mathf.Max(bridgeBounds.min.x-rock.max.x,rock.min.x-bridgeBounds.max.x));
                    float dz=Mathf.Max(0,Mathf.Max(bridgeBounds.min.z-rock.max.z,rock.min.z-bridgeBounds.max.z));
                    rockClearance=Mathf.Min(rockClearance,new Vector2(dx,dz).magnitude);
                }
            }
            Require(!float.IsInfinity(rockClearance)&&rockClearance>.35f,
                $"The bridge did not leave a gap from ground rocks. Clearance: {rockClearance:F2}.");
            Debug.Log($"FOREST LAYOUT PASS: moved the bridge and five stumps toward the center and left {rockClearance:F2} units to the nearest rock bounds.");
            yield return CheckForestForeground(park,scenery);
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
                if(i==1)Require(highest>1.26f&&end.x<3f,"The animal did not climb and cross the wooden ladder.");
                if(i==2)Require(highest>4.4f&&trolleyTravel>21&&photographedExit&&Vector3.Dot(end-landingPost.position,exitSide)>2.1f&&
                    Vector3.Distance(trolley.position,trolleyStart)<.01f,"The animal did not climb, ride the diagonal cable and land.");
            }
            animal.gameObject.SetActive(false);
            Destroy(animal.gameObject);
            yield return CheckTallZipline(park,rides[2]);
            Debug.Log("FOREST RIDES PASS: five stumps, footstep ladder crossing, and zipline landing clear of the post with a side exit.");
        }

        IEnumerator CheckForestForeground(ParkPlayground park,Transform scenery)
        {
            const float closeDistance=18.5f;
            var venue=park.Venues[park.VenueIndex];
            var rotation=Quaternion.Euler(21f,venue.Yaw,0);
            var direction=rotation*Vector3.forward;
            var position=new Vector3(4f,1.2f,-7f)-direction*closeDistance;
            var ray=new Ray(position,direction);
            float frontHeight=0,backHeight=0;
            foreach(Transform prop in scenery)
            {
                if(!prop.name.StartsWith("UNS_Spruce",StringComparison.Ordinal))continue;
                foreach(var renderer in prop.GetComponentsInChildren<MeshRenderer>())
                {
                    var bounds=renderer.bounds;
                    if(prop.position.z < -15f)frontHeight=Mathf.Max(frontHeight,bounds.max.y-venue.LawnHeight);
                    if(prop.position.z > 10f)backHeight=Mathf.Max(backHeight,bounds.max.y-venue.LawnHeight);
                    Require(!bounds.IntersectRay(ray,out float hit)||hit>=closeDistance,
                        "A forest spruce blocked the low camera's sightline into the lawn.");
                }
            }
            Require(frontHeight>1.5f&&frontHeight<3.85f&&backHeight>5.5f,
                $"Forest tree heights are wrong: foreground {frontHeight:F2}, background {backHeight:F2}.");
            var orbit=park.OrbitCamera;
            var camera=orbit.transform;
            var originalPosition=camera.position;
            var originalRotation=camera.rotation;
            bool wasEnabled=orbit.enabled;
            orbit.enabled=false;
            try
            {
                camera.SetPositionAndRotation(position,rotation);
                yield return Capture("forest-low-camera.png");
            }
            finally
            {
                camera.SetPositionAndRotation(originalPosition,originalRotation);
                orbit.enabled=wasEnabled;
            }
            Debug.Log($"FOREST FOREGROUND PASS: clear low camera sightline, foreground height {frontHeight:F2}, background height {backHeight:F2}.");
        }

        IEnumerator CheckTallZipline(ParkPlayground park,ParkForestRide ride)
        {
            var giraffe=PrepareRideProbe(park,AnimalKind.Giraffe);
            var start=ride.Trolley.position;
            Require(start.y>7.55f&&ParkForestRide.HandleDrop>1.65f&&
                giraffe.RequestAction(ParkAnimalAction.Zipline),"The higher cable or lower zipline handle was not available.");
            var skin=giraffe.Character.Skin;
            var mesh=skin.sharedMesh;
            var head=skin.bones[1];
            var headBind=mesh.bindposes[1];
            var vertices=mesh.vertices;
            var weights=mesh.boneWeights;
            var headVertices=new List<Vector3>();
            for(int i=0;i<vertices.Length;i++)
                if(weights[i].boneIndex0==1&&weights[i].weight0>=.995f)
                    headVertices.Add(headBind.MultiplyPoint3x4(vertices[i]));
            Require(headVertices.Count>100,"The giraffe has no rigid head vertices for the cable check.");
            float minimumClearance=float.PositiveInfinity;
            int samples=0;
            bool started=false,photo=false;
            const float dt=.05f;
            for(int frame=0;frame<1200;frame++)
            {
                giraffe.Advance(dt);
                ride.Advance(dt);
                started|=giraffe.ActiveAction==ParkAnimalAction.Zipline;
                var travel=ride.Trolley.position-start;
                if(ride.IsAirborne&&travel.magnitude>1&&travel.magnitude<20)
                {
                    var horizontal=Vector3.ProjectOnPlane(travel,Vector3.up);
                    float slope=travel.y/horizontal.magnitude;
                    var forward=horizontal.normalized;
                    var center=ride.Trolley.position;
                    foreach(var vertex in headVertices)
                    {
                        var world=head.TransformPoint(vertex);
                        float cableY=center.y+slope*Vector3.Dot(world-center,forward);
                        minimumClearance=Mathf.Min(minimumClearance,cableY-world.y);
                    }
                    samples++;
                    if(!photo&&travel.magnitude>10.5f)
                    {
                        photo=true;
                        var orbit=park.OrbitCamera;
                        var camera=Camera.main.transform;
                        var view=new Pose(camera.position,camera.rotation);
                        bool tracking=orbit.enabled;
                        orbit.enabled=false;
                        var side=Vector3.Cross(Vector3.up,forward);
                        var focus=Vector3.Lerp(giraffe.transform.position,ride.Trolley.position,.51f);
                        camera.position=focus+side*6.3f+forward*2.5f+Vector3.up*2.1f;
                        camera.LookAt(focus);
                        yield return Capture("forest-zipline-giraffe.png");
                        camera.SetPositionAndRotation(view.position,view.rotation);
                        orbit.enabled=tracking;
                    }
                }
                if(started&&giraffe.ActiveAction==ParkAnimalAction.None)break;
            }
            var ground=giraffe.transform.position;
            Require(photo&&samples>15&&minimumClearance>.23f&&giraffe.ActiveAction==ParkAnimalAction.None&&
                park.IsSceneryClear(ground)&&Mathf.Abs(ground.y-park.SurfaceHeight(ground))<.012f,
                $"The giraffe did not ride below the cable and finish on clear ground; clearance {minimumClearance:F2}.");
            giraffe.gameObject.SetActive(false);
            Destroy(giraffe.gameObject);
            Debug.Log($"GIRAFFE ZIPLINE PASS: minimum cable clearance {minimumClearance:F2}, with a grounded side exit.");
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

            orbit.focus=venue.TransformPoint(new Vector3(-9.5f,1.2f,-32.6f));
            orbit.distance=(Screen.width>Screen.height?46:86)*venue.lossyScale.x;
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
                    var point=train.transform.InverseTransformPoint(animal.transform.position);
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
                var worldPoint=animal.transform.position;
                var point=train.transform.InverseTransformPoint(worldPoint);
                Require(tookPlatformSteps[i]&&animal.ActiveAction==ParkAnimalAction.None&&
                    point.z>ParkElephantTrain.StationRailZ+4.5f&&Mathf.Abs(point.y-ParkElephantTrain.DeckHeight)<.01f&&
                    Mathf.Abs(worldPoint.y-park.SurfaceHeight(worldPoint))<.01f,
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
                bool changing=park.VenueIndex!=venue;
                var previous=new Dictionary<int,Vector3>();
                foreach(var animal in park.Animals)previous.Add(animal.Id,animal.transform.position);
                park.ChangeVenue(venue);
                if(changing)
                {
                    bool redistributed=false;
                    Require(park.Animals.Count==previous.Count,"Switching venues lost an animal.");
                    foreach(var animal in park.Animals)
                    {
                        var point=animal.transform.position;
                        Require(previous.ContainsKey(animal.Id)&&park.WalkArea.Contains(new Vector2(point.x,point.z))&&
                            Mathf.Abs(point.y-park.SurfaceHeight(point))<.001f&&park.IsClear(point,animal,ParkRabbitAgent.AnimalSpacing),
                            "An animal switched venues to an occupied or off-ground location.");
                        redistributed|=Vector3.Distance(previous[animal.Id],point)>.5f;
                    }
                    Require(redistributed,"The herd kept its old positions when changing venues.");
                }
                // This probe measures pedestrians separating on the lawn.
                var potentialRides=new List<Behaviour>{park.Wheel,park.Train,park.Carousel,park.Slide,park.Trampoline,
                    park.Seesaw,park.Roundabout,park.SandSled,park.DesertCamp,park.ResortRiver,park.ResortSwim,park.ResortSlide};
                if(park.ForestRides!=null)potentialRides.AddRange(park.ForestRides);
                if(park.Deer!=null)potentialRides.AddRange(park.Deer);
                var rides=potentialRides.FindAll(ride=>ride&&ride.enabled);
                foreach(var ride in rides)ride.enabled=false;
                var area=park.WalkArea;
                var corner=new Vector3(area.xMin,0,area.yMin);
                if(park.SandSled)corner.x=area.xMax-.5f;
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
            Debug.Log($"VENUE RELOCATION AND CROWD SEPARATION PASS: {park.Animals.Count} animals started at clear, separate points and spread out from the same corner in each venue.");
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
            CheckContinuousTrampoline(park,agent);
            Destroy(sample);
            yield return CheckPoleEntrance(park);
            Debug.Log("PLAYGROUND PASS: two animated entrances, wave, grounded wandering and equipment avoidance.");
        }
        static void CheckContinuousTrampoline(ParkPlayground park,ParkRabbitAgent animal)
        {
            var ride=park.Trampoline;
            Require(ride&&animal.RequestAction(ParkAnimalAction.Trampoline),"The trampoline could not be requested.");
            const float dt=.02f;
            int jumps=0;
            float floorTime=0,longestFloorTime=0,highest=0;
            bool started=false,airborne=false;
            for(int frame=0;frame<2500;frame++)
            {
                animal.Advance(dt);
                ride.Advance(dt);
                started|=animal.ActiveAction==ParkAnimalAction.Trampoline;
                var point=ride.transform.InverseTransformPoint(animal.transform.position);
                if(started&&Mathf.Abs(point.z)<.04f&&Mathf.Abs(point.x)<.04f)
                {
                    float y=animal.transform.position.y-ride.MatSurfaceY;
                    Require(y>=-.003f,"The trampoline animation moved the animal through the mat.");
                    highest=Mathf.Max(highest,y);
                    bool nowAirborne=y>.10f;
                    if(nowAirborne&&!airborne)
                    {
                        if(jumps>0)longestFloorTime=Mathf.Max(longestFloorTime,floorTime);
                        jumps++;
                        floorTime=0;
                    }
                    else if(!nowAirborne&&jumps>0)floorTime+=dt;
                    airborne=nowAirborne;
                }
                if(started&&animal.ActiveAction==ParkAnimalAction.None)break;
            }
            Require(jumps==10&&highest>.77f&&longestFloorTime<=.081f&&animal.ActiveAction==ParkAnimalAction.None&&
                park.IsSceneryClear(animal.transform.position),
                $"The trampoline did not bounce continuously and finish at the exit; {jumps} jumps, {longestFloorTime:F3}s near the mat.");
            Debug.Log($"TRAMPOLINE BOUNCE PASS: {jumps} consecutive jumps, maximum {longestFloorTime:F3}s below a tenth of a unit between jumps.");
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
            var ride=park.PoleRide;
            var pole=gate.EntrancePole;
            Require(ride&&pole&&ride.transform.Find("Right ladder")&&ride.transform.Find("Door-to-pole platform")&&
                pole.position.x<0&&pole.position.z>0&&gate.Threshold.position.y>5&&
                gate.Threshold.position.z-ride.WaveSpot.z>1.2f&&ride.WaveSpot.z-pole.position.z>1.3f&&
                ride.LadderApproach.x>pole.position.x+2.8f&&park.IsSceneryClear(ride.LadderApproach)&&
                park.WalkArea.Contains(new Vector2(ride.LadderApproach.x,ride.LadderApproach.z))&&
                park.IsSceneryClear(gate.Welcome.position)&&!park.IsSceneryClear(pole.position)&&
                !park.Venues[park.VenueIndex].Root.transform.Find("Soft climbing toys"),
                "The pole tower is missing a greeting deck, right ladder or clear ground exit.");
            Require(Array.IndexOf(park.AnimalActions,ParkAnimalAction.Pole)>=0&&
                ParkLanguage.Translate(ParkPlayground.ActionLabel(ParkAnimalAction.Pole),false)=="Pole Slide",
                "The playground is missing the localized pole action.");
            var sample=new GameObject("Pole entrance animal probe");
            var agent=sample.AddComponent<ParkRabbitAgent>();
            agent.Initialize(park,gate,1,0,park.Drawing.Front.Paint.EncodeToPNG(),park.Drawing.Back.Paint.EncodeToPNG());
            Require(park.Houses[0].TryReserve()&&gate.TryReserve(),"The two entrances could not reserve independently.");
            park.Houses[0].CloseAfterExit();
            bool captured=false,upperWave=false,capturedWave=false;
            float rotation=0,waveTime=0;
            for(int i=0;i<240;i++)
            {
                var previous=sample.transform.position;
                park.Houses[0].Advance(.05f);
                gate.Advance(.05f);
                agent.Advance(.05f);
                var position=sample.transform.position;
                if(agent.WaveCount==1&&!upperWave)
                {
                    upperWave=true;
                    Require(rotation==0&&Vector3.Distance(position,ride.WaveSpot)<.01f&&
                        Quaternion.Angle(sample.transform.rotation,gate.Facing)<.1f,
                        "The arriving animal did not face the front and wave from the upper platform before the descent.");
                }
                if(upperWave&&Vector3.Distance(position,ride.WaveSpot)<.01f&&!capturedWave&&(waveTime+=.05f)>.50f)
                {
                    capturedWave=true;
                    yield return PolePhoto("playground-pole-wave.png");
                }
                if(agent.State==ParkRabbitState.WalkingOut&&position.y>pole.position.y+.001f&&position.y<previous.y)
                {
                    var radius=position-pole.position;radius.y=0;
                    var previousRadius=previous-pole.position;previousRadius.y=0;
                    float step=Vector3.SignedAngle(previousRadius,radius,Vector3.up);
                    rotation+=step;
                    Require(upperWave&&gate.DoorFraction==1&&agent.Character.Skin.enabled&&step>=-.01f&&step<18&&
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
                        yield return PolePhoto("playground-pole-arrival.png");
                    }
                }
                Require(agent.State!=ParkRabbitState.Waving,"The animal stopped for a second greeting on the ground.");
            }
            Require(capturedWave&&rotation>355&&rotation<365&&agent.State==ParkRabbitState.Wandering&&agent.WaveCount==1&&
                agent.ExitedThroughOpenDoor&&!gate.IsBusy&&gate.DoorFraction==0,
                "The pole animal did not wave, roam and release the entrance.");
            Debug.Log("POLE ARRIVAL PASS: opened the upper door, walked out, waved on the deck and descended once gripping the pole.");

            agent.transform.position=ride.LadderApproach+Vector3.right*1.1f;
            Require(gate.TryReserve()&&agent.RequestAction(ParkAnimalAction.Pole),"The animal could not request the shared pole.");
            agent.Advance(.05f);
            Require(ride.Available&&agent.PendingAction==ParkAnimalAction.Pole,
                "The ladder ride reserved the pole while a new arrival held the tower.");
            gate.CloseAfterExit();
            bool climbed=false,crossedDeck=false,descended=false,started=false;
            for(int i=0;i<700;i++)
            {
                var previous=agent.transform.position;
                gate.Advance(.05f);
                agent.Advance(.05f);
                ride.Advance(.05f);
                var position=agent.transform.position;
                if(agent.ActiveAction==ParkAnimalAction.Pole)
                {
                    started=true;
                    Require(!gate.CanReserve,"An arrival could reserve the pole tower during the ladder ride.");
                    if(!climbed&&position.y>2.2f&&position.y>previous.y)
                    {
                        Require(position.x>pole.position.x+1.8f&&
                            Vector3.Dot(agent.transform.forward,ride.transform.right)>.99f&&agent.IsUsingRide,
                            "The animal did not climb facing the right ladder.");
                        climbed=true;
                        yield return PolePhoto("playground-pole-ladder.png");
                    }
                    crossedDeck|=climbed&&position.y>ride.WaveSpot.y-.02f&&Mathf.Abs(position.x-pole.position.x)<.3f;
                    descended|=crossedDeck&&position.y>1f&&position.y<previous.y&&
                        Mathf.Abs(Vector3.ProjectOnPlane(position-pole.position,Vector3.up).magnitude-.42f)<.01f;
                }
                else if(started)break;
            }
            Require(climbed&&crossedDeck&&descended&&ride.Available&&gate.CanReserve&&agent.WaveCount==1&&
                Vector3.Distance(agent.transform.position,gate.Welcome.position)<.02f&&park.IsSceneryClear(agent.transform.position),
                "The pole action did not finish climbing, crossing the deck, descending and releasing the tower.");
            Destroy(sample);
            Debug.Log("POLE ACTION PASS: localized action, exclusive tower access, right ladder ascent, upper deck crossing and shared descent.");

            IEnumerator PolePhoto(string name)
            {
                var orbit=park.OrbitCamera;
                var camera=orbit.transform;
                var position=camera.position;
                var orientation=camera.rotation;
                orbit.enabled=false;
                camera.position=pole.position+new Vector3(7.7f,6.45f,-10.1f);
                camera.LookAt(pole.position+new Vector3(.45f,4.25f,1.0f));
                yield return Capture(name);
                camera.SetPositionAndRotation(position,orientation);
                orbit.enabled=true;
            }
        }

        static void CheckOpenEntranceDispatch(ParkPlayground park)
        {
            park.ChangeVenue(0);
            var occupied=park.Houses[0];
            var available=park.Houses[1];
            Require(park.PendingCount==0&&park.Animals.Count+2<=ParkPlayground.AnimalLimit&&
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
            float fadeDelay=startCount+3>ParkPlayground.AnimalLimit?ParkPlayground.AnimalFadeDuration:0;
            park.Advance(occupied.DoorDuration+fadeDelay+.1f);
            CheckNext(2,0,1);
            park.Advance(12);
            Require(park.PendingCount==0&&park.Animals.Count==Mathf.Min(startCount+4,ParkPlayground.AnimalLimit),
                "A queued arrival was lost when the entrance reopened.");
            for(int i=0;i<4;i++)
            {
                var animal=FindArrival(i);
                Require(animal&&animal.WaveCount==1,"A burst arrival was dropped or skipped its greeting.");
            }
            Debug.Log("OPEN ENTRANCE DISPATCH PASS: the open door took the first two LAN submissions while the other was occupied; the reopened door took the next.");

            void CheckNext(int offset,int door,int pending)
            {
                var animal=FindArrival(offset);
                Require(park.PendingCount==pending&&park.Animals.Count==Mathf.Min(startCount+offset+1,ParkPlayground.AnimalLimit)&&
                    animal&&animal.HouseIndex==door,
                    "The oldest network submission did not use the first door that became available.");
            }
            ParkRabbitAgent FindArrival(int offset)
            {
                foreach(var animal in park.Animals)
                    if(animal.Id==firstId+offset)return animal;
                return null;
            }
        }

        static void CheckAnimalLimit(ParkPlayground park)
        {
            var drawing=park.Drawing;
            var front=drawing.Front.Paint.GetRawTextureData();
            var back=drawing.Back.Paint.GetRawTextureData();
            const int limit=10;
            while(park.Animals.Count<limit)
            {
                Require(park.SubmitLan(AnimalKind.Rabbit,front,back)==ParkLan.Accepted,"Animal rejected before the park was full.");
                park.Advance(7f);
            }
            var oldest=park.Animals[0];
            foreach(var animal in park.Animals)
                if(animal.Id<oldest.Id)oldest=animal;
            var visible=oldest.Character.Skin.sharedMaterial;
            Require(!visible.IsKeywordEnabled("PEACE_PARK_ANIMAL_FADING"),"A walking animal used the fade shader.");
            Require(park.SubmitLan(AnimalKind.Rabbit,front,back)==ParkLan.Accepted,"The 11th animal was rejected.");
            int newestId=park.SubmissionCount;
            park.Advance(.6f);
            var properties=new MaterialPropertyBlock();
            oldest.Character.Skin.GetPropertyBlock(properties);
            float opacity=properties.GetFloat("_AnimalVisibility");
            Require(park.Animals.Count==limit&&oldest.gameObject.activeSelf&&opacity>0&&opacity<1&&
                visible.IsKeywordEnabled("PEACE_PARK_ANIMAL_FADING"),
                "The oldest animal did not visibly fade while the replacement waited.");
            if(park.Venues?.Length>1)
            {
                park.ChangeVenue((park.VenueIndex+1)%park.Venues.Length);
                oldest.Character.Skin.GetPropertyBlock(properties);
                Require(Mathf.Approximately(properties.GetFloat("_AnimalVisibility"),opacity),
                    "Switching venues reset the oldest animal's fade.");
            }
            park.Advance(1f);
            bool added=false;
            foreach(var animal in park.Animals)added|=animal.Id==newestId;
            Require(park.Animals.Count==limit&&!oldest.gameObject.activeSelf&&added&&park.PendingCount==0,
                "The 11th animal did not replace the oldest.");
            Debug.Log("ANIMAL LIMIT PASS: 11th LAN submission fades out the oldest and maintains the cap of 10.");
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
        static void CheckDefaultBrush(DrawingPage drawing)
        {
            int selected=drawing.SelectedAnimal;
            drawing.SelectAnimal((int)AnimalKind.Rabbit);
            var front=new Rect(0,0,AnimalCanvas.Width,AnimalCanvas.Height);
            var back=new Rect(front.xMax+50,0,front.width,front.height);
            int x=AnimalCanvas.Width/2,y=AnimalCanvas.Height/2;
            var point=new Vector2(x,front.yMax-y);
            Require(drawing.Front.IsInside(x,y+52),"The default brush probe is outside the drawing template.");
            var previous=drawing.Front.Paint.GetPixel(x,y+52);
            drawing.HandleTouchPaint(0,TouchPhase.Began,point,front,back);
            drawing.HandleTouchPaint(0,TouchPhase.Ended,point,front,back);
            Require(drawing.Front.Paint.GetPixel(x,y+52)!=previous,"The drawing did not start with the widest brush.");
            drawing.SelectAnimal(selected);
            Debug.Log("DEFAULT BRUSH PASS: the first stroke used the widest brush.");
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

            void Touch(int finger,TouchPhase phase,Vector2 position)=>drawing.HandleTouchPaint(finger,phase,position,front,back);
            var grip=new Vector2(1,frontCenter.y);
            Touch(17,TouchPhase.Began,grip);
            Touch(17,TouchPhase.Moved,frontCenter);
            Require(drawing.Front.Paint.GetPixel(x,y)==frontBefore,"A grip that began outside the canvas painted after drifting inside.");

            Touch(32,TouchPhase.Began,frontCenter+Vector2.left*70);
            Touch(17,TouchPhase.Moved,backCenter);
            Touch(32,TouchPhase.Moved,frontCenter);
            Require(drawing.Front.Paint.GetPixel(x,y)!=frontBefore&&drawing.Back.Paint.GetPixel(x,y)==backBefore,
                "The first contact blocked the drawing finger or painted on the other canvas.");
            Touch(17,TouchPhase.Ended,backCenter);
            Touch(32,TouchPhase.Moved,backCenter);
            Require(drawing.Back.Paint.GetPixel(x,y)!=backBefore,"Releasing the grip interrupted the active drawing stroke.");
            Touch(32,TouchPhase.Canceled,backCenter);

            drawing.Front.Clear();
            Touch(32,TouchPhase.Moved,frontCenter);
            Require(drawing.Front.Paint.GetPixel(x,y)==frontBefore,"A canceled contact continued to paint.");
            Touch(58,TouchPhase.Began,frontCenter);
            Require(drawing.Front.Paint.GetPixel(x,y)!=frontBefore,"A new finger could not start a stroke after cancellation.");
            Touch(58,TouchPhase.Ended,frontCenter);
            drawing.SelectAnimal(drawing.SelectedAnimal);
            Debug.Log("TOUCH DRAWING PASS: a second finger painted with an edge contact held, crossed both canvases and continued after the grip was released.");
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
                var expectedScale=Vector3.one*(authoredScale*(house.SecondDoorHinge?1f:1.1f));
                if(venue.Carousel)expectedScale.y/=venue.Root.transform.lossyScale.y;
                Require(Vector3.Distance(house.transform.localScale,expectedScale)<.001f,
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
