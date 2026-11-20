#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PeacePark
{
    // Opt-in editor regression check; excluded from player builds.
    public sealed class ParkCameraFollowValidation : MonoBehaviour
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ParkPlayground.unity");
            EditorApplication.isPlaying=true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-peaceParkFollowCheck")<0)return;
            new GameObject("Camera follow validation").AddComponent<ParkCameraFollowValidation>();
        }
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type)
        {if(type==LogType.Exception||type==LogType.Error)EditorApplication.delayCall+=()=>EditorApplication.Exit(1);}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Step(ParkCamera camera,int count)
        {for(int i=0;i<count;i++)camera.AdvanceAnimalFollow(1f/60);}
        static Vector2 ScreenPoint(Camera camera,ParkRabbitAgent animal)=>camera.WorldToScreenPoint(animal.transform.position+Vector3.up*1.1f);
        static void Tap(ParkCamera orbit,Camera camera,ParkRabbitAgent animal,int finger=-1)
        {
            var point=ScreenPoint(camera,animal);
            Require(orbit.AnimalAt(point)==animal,"Screen ray did not select the intended animal");
            orbit.BeginAnimalTap(point,finger);orbit.EndAnimalTap(point,finger);
        }
        IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+30;
            ParkPlayground park;
            while(!(park=FindAnyObjectByType<ParkPlayground>())||!park.IsReady)
            {
                if(Time.realtimeSinceStartup>deadline)
                {
                    Debug.LogError("Camera follow check could not start the park");
                    EditorApplication.Exit(1);
                    yield break;
                }
                yield return null;
            }
            Application.logMessageReceived+=Log;
            park.ManualSimulation=true;park.ShowPark();
            var orbit=park.OrbitCamera;var camera=orbit.GetComponent<Camera>();orbit.enabled=false;
            var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.cyan,Color.cyan,Color.cyan,Color.cyan});texture.Apply();
            var png=texture.EncodeToPNG();Destroy(texture);
            Require(park.TrySubmit((int)AnimalKind.Elephant,png,png,0),"Elephant submission failed");
            Require(park.TrySubmit((int)AnimalKind.Giraffe,png,png,1),"Giraffe submission failed");
            park.Advance(25);
            Require(park.Animals.Count==2,"Two animals did not spawn");
            var a=park.Animals[0];var b=park.Animals[1];
            Require(park.CanFollowAnimal(a)&&park.CanFollowAnimal(b),"Animals did not finish their entrances");
            a.transform.position=new Vector3(-2,park.SurfaceHeight(new Vector3(-2,0,-4)),-4);
            b.transform.position=new Vector3(2,park.SurfaceHeight(new Vector3(2,0,-4)),-4);
            a.Character.PlayIdlePose();b.Character.PlayIdlePose();
            yield return null;
            Vector3 originalPosition=orbit.transform.position;Quaternion originalRotation=orbit.transform.rotation;
            float originalDistance=orbit.distance;
            Capture(camera,"wide");
            Tap(orbit,camera,a);
            Require(orbit.FollowTarget==a&&Vector3.Distance(originalPosition,orbit.transform.position)<.0001f,"Selection snapped the camera");
            Step(orbit,1);
            Require(orbit.distance<originalDistance&&orbit.distance>originalDistance*.9f,"Zoom-in did not start gradually");
            Step(orbit,300);
            Require(orbit.distance<originalDistance*.6f,"Camera did not zoom in");
            Capture(camera,"following");
            a.transform.position+=Vector3.right*3;
            Step(orbit,120);
            Require(Mathf.Abs(orbit.focus.x-a.transform.position.x)<.03f,"Camera did not track the moving animal");
            float settledHeight=orbit.focus.y;
            for(int i=0;i<65;i++){a.Character.PlayWalkingPose(i*Mathf.PI/32);Step(orbit,1);}
            Require(Mathf.Abs(orbit.focus.y-settledHeight)<.01f,"Walking bob leaked into camera focus");
            b.transform.position=a.transform.position+camera.transform.right*2;
            Tap(orbit,camera,b,0);Step(orbit,240);
            Require(orbit.FollowTarget==b,"Target switching failed");
            float closeDistance=orbit.distance;
            Tap(orbit,camera,b,0);Step(orbit,1);
            Require(orbit.IsReturning&&orbit.distance>closeDistance&&orbit.distance<originalDistance,"Release did not zoom out gradually");
            Step(orbit,360);
            Require(!orbit.FollowTarget&&!orbit.IsReturning&&Vector3.Distance(originalPosition,orbit.transform.position)<.01f&&
                Quaternion.Angle(originalRotation,orbit.transform.rotation)<.05f,"Return did not restore the original camera view");
            Capture(camera,"returned");

            var point=ScreenPoint(camera,a);
            orbit.BeginAnimalTap(point,0);orbit.MoveAnimalTap(point+Vector2.right*50);orbit.EndAnimalTap(point,0);
            Require(!orbit.FollowTarget,"A drag was mistaken for a tap");
            orbit.BeginAnimalTap(point,0);orbit.CancelAnimalTap();orbit.EndAnimalTap(point,0);
            Require(!orbit.FollowTarget,"A canceled/multitouch gesture selected an animal");
            orbit.BeginAnimalTap(new Vector2(point.x,Screen.safeArea.yMax-2),0);orbit.EndAnimalTap(point,0);
            Require(!orbit.FollowTarget,"Toolbar input leaked into animal selection");
            orbit.BeginAnimalTap(point,0);orbit.EndAnimalTap(point,1);
            Require(!orbit.FollowTarget,"Different finger ended the tap");
            orbit.BeginAnimalTap(point,0);yield return new WaitForSecondsRealtime(.5f);orbit.EndAnimalTap(point,0);
            Require(!orbit.FollowTarget,"Long press selected an animal");
            orbit.InputEnabled=false;orbit.BeginAnimalTap(point,0);orbit.EndAnimalTap(point,0);orbit.InputEnabled=true;
            Require(!orbit.FollowTarget,"Disabled camera accepted a tap");
            orbit.ToggleAnimalFollow(a);Step(orbit,180);
            orbit.ApplyTwoFingerGesture(new Vector2(20,0),900,100,125);Step(orbit,120);
            Require(orbit.FollowTarget==a&&orbit.distance>=ParkCamera.MinimumDistance,"Pinch/pan canceled following or broke zoom limits");
            a.gameObject.SetActive(false);Step(orbit,1);
            Require(!orbit.FollowTarget&&orbit.IsReturning,"Missing target did not release the camera");
            a.gameObject.SetActive(true);Step(orbit,360);
            orbit.ToggleAnimalFollow(a);Step(orbit,60);
            var beforeReset=orbit.transform.position;
            orbit.ResetView();
            Require(!orbit.FollowTarget&&Vector3.Distance(beforeReset,orbit.transform.position)<.001f,"Starting reset snapped out of tracking");
            Step(orbit,60);
            Require(!orbit.FollowTarget&&!orbit.IsReturning&&Mathf.Abs(orbit.distance-originalDistance)<.001f,"Reset did not clear tracking");

            int square=Array.FindIndex(park.Venues,v=>v.Name=="시계탑 광장");
            Require(square>=0,"Clock Tower Square is missing");
            orbit.ToggleAnimalFollow(a);park.ChangeVenue(square);
            Require(!orbit.FollowTarget&&!orbit.IsReturning,"Venue switch did not clear tracking");
            originalPosition=orbit.transform.position;originalRotation=orbit.transform.rotation;
            orbit.ToggleAnimalFollow(a);Step(orbit,1);
            Require(Vector3.Distance(originalPosition,orbit.transform.position)<5,"Square follow transition snapped");
            Step(orbit,360);
            orbit.ReleaseAnimalFollow();Step(orbit,480);
            Require(Vector3.Distance(originalPosition,orbit.transform.position)<.01f&&Quaternion.Angle(originalRotation,orbit.transform.rotation)<.05f,"Square return did not restore the wide view");
            // A manual gesture must preserve the currently displayed aim while the camera is returning.
            orbit.ToggleAnimalFollow(a);Step(orbit,300);orbit.ReleaseAnimalFollow();Step(orbit,20);
            var interruptedPosition=orbit.transform.position;
            orbit.ApplyTwoFingerGesture(Vector2.zero,900,100,100);Step(orbit,1);
            Require(!orbit.IsReturning&&Vector3.Distance(interruptedPosition,orbit.transform.position)<.001f,"Interrupting the square return caused a jump");
            orbit.ToggleAnimalFollow(a);Step(orbit,200);orbit.ReleaseAnimalFollow();Step(orbit,400);
            Require(Vector3.Distance(interruptedPosition,orbit.transform.position)<.01f,"A follow from an interrupted view lost the saved aim");
            orbit.ResetView();
            Step(orbit,60);
            orbit.ToggleAnimalFollow(a);Step(orbit,60);
            typeof(ParkPlayground).GetField("fadingAnimal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(park,a);
            Step(orbit,1);Require(!orbit.FollowTarget&&orbit.IsReturning,"Fading animal kept tracking");
            typeof(ParkPlayground).GetField("fadingAnimal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(park,null);
            Step(orbit,400);
            park.ChangeVenue(0);
            Require(park.TrySubmit((int)AnimalKind.Rabbit,png,png,0),"Garden arrival submission failed");
            park.Advance(.01f);
            var arriving=park.Animals[park.Animals.Count-1];
            Require(arriving.State==ParkRabbitState.OpeningDoor&&!park.CanFollowAnimal(arriving),"Hidden animal was selectable");
            for(int i=0;i<200&&!arriving.Character.Skin.enabled;i++)park.Advance(.02f);
            Require(arriving.State==ParkRabbitState.WalkingOut&&park.CanFollowAnimal(arriving),"First visible arrival was not selectable");
            var headBone=arriving.Character.Skin.bones[1];
            Vector2 head=camera.WorldToScreenPoint(headBone.TransformPoint(Vector3.up*.65f));
            Require(orbit.AnimalAt(head)==arriving,"First visible head did not receive its screen hit");
            orbit.BeginAnimalTap(head,0);orbit.EndAnimalTap(head,0);
            bool sawWave=false;
            for(int i=0;i<600&&arriving.State!=ParkRabbitState.Wandering;i++)
            {
                park.Advance(.025f);orbit.AdvanceAnimalFollow(.025f);
                Require(orbit.FollowTarget==arriving,"Arrival lost tracking before walking away");
                sawWave|=arriving.State==ParkRabbitState.Waving;
            }
            Require(sawWave&&arriving.State==ParkRabbitState.Wandering,"Did not follow through the greeting");
            Tap(orbit,camera,arriving,0);Require(orbit.IsReturning,"Retap after the greeting did not release tracking");
            Step(orbit,400);
            orbit.ToggleAnimalFollow(a);Step(orbit,120);park.ShowDrawing();
            Require(!orbit.FollowTarget&&!orbit.IsReturning,"Coloring view retained tracking");
            Debug.Log("CAMERA FOLLOW PASS: screen picking, smooth zoom in/out, movement, bob isolation, target switch, gestures, reset, missing target, venue/view changes and immediate arrival selection.");
            EditorApplication.Exit(0);
        }
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1280,720,24){antiAliasing=4};
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                Directory.CreateDirectory("Previews");File.WriteAllBytes("Previews/camera-follow-"+name+".png",pixels.EncodeToPNG());
            }
            finally{camera.targetTexture=previousTarget;RenderTexture.active=previous;rt.Release();Destroy(rt);Destroy(pixels);}
        }
    }
}
#endif
