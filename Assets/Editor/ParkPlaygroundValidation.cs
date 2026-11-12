using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PeacePark.Editor
{
    public static class ParkPlaygroundValidation
    {
        static bool complete;
        static int frames;
        public static void Capture()
        {
            // This runner deliberately uses the separate validation project, not the user's open scene.
            if(!Application.dataPath.Replace('\\','/').EndsWith("/TempArtValidation/Assets",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Run park validation in TempArtValidation only.");
            complete=false;frames=0;
            if(File.Exists(ParkPlaygroundSetup.ScenePath))AssetDatabase.DeleteAsset(ParkPlaygroundSetup.ScenePath);
            ParkPlaygroundSetup.Create();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.update+=Update;EditorApplication.isPlaying=true;
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Update()
        {
            if(!EditorApplication.isPlaying||complete)return;
            var park=UnityEngine.Object.FindAnyObjectByType<ParkPlayground>();
            if(!park||!park.IsReady){if(++frames>2000)EditorApplication.Exit(1);return;}
            complete=true;park.ManualSimulation=true;
            try{Validate(park);EditorApplication.Exit(0);}
            catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
            EditorApplication.update-=Update;
        }
        static void Simulate(ParkPlayground park,float seconds)
        {int steps=Mathf.CeilToInt(seconds/.05f);for(int i=0;i<steps;i++)park.Advance(seconds/steps);}
        static void Validate(ParkPlayground park)
        {
            Require(park.Houses.Length==2,"Expected flower and candy houses");
            foreach(var house in park.Houses)
            {
                Require(!house.transform.Find("Walls"),"House still contains solid doorway-blocking walls");
                Require(!house.transform.Find("Arched door surround"),"Door surround still fills the opening");
                Require(house.DoorHinge.childCount==7,"Panel, five seams and knob must move with the hinge");
            }
            var colors=new Color32[]{new Color32(245,111,151,255),new Color32(64,175,216,255),new Color32(153,201,79,255),new Color32(248,176,64,255),new Color32(180,124,216,255)};
            park.ShowDrawing();Require(park.IsDrawing&&!park.OrbitCamera.InputEnabled,"Drawing does not own input");
            RabbitPaintValidation.PaintWithBrush(park.Drawing.Front,colors[0]);RabbitPaintValidation.PaintWithBrush(park.Drawing.Back,colors[0]);
            Require(park.Drawing.ApplyDrawing(),"Drawing apply rejected");
            Require(!park.Drawing.ApplyDrawing(),"Three-second Apply lock missing");
            Require(!park.IsDrawing&&park.PendingCount==1,"Apply did not enqueue and return to park");
            park.Advance(.05f);Require(park.Animals.Count==1,"First rabbit missing");
            var first=park.Animals[0];int firstHouse=first.HouseIndex;
            var scaleProbe=new Mesh();first.Character.Skin.BakeMesh(scaleProbe,false);scaleProbe.RecalculateBounds();float unscaledHeight=scaleProbe.bounds.size.y;
            first.Character.Skin.BakeMesh(scaleProbe,true);scaleProbe.RecalculateBounds();float scaledHeight=scaleProbe.bounds.size.y;UnityEngine.Object.Destroy(scaleProbe);
            Debug.Log($"PARK SCALE: source={first.Character.Skin.sharedMesh.bounds.size.y}, Bake(false)={unscaledHeight}, Bake(true)={scaledHeight}, transform={first.transform.lossyScale.y}");
            Require(Mathf.Abs(unscaledHeight-first.Character.Skin.sharedMesh.bounds.size.y*.42f)<.01f,"Rabbit scale does not fit the doorway");
            Require(first.State==ParkRabbitState.OpeningDoor&&!first.Character.Skin.enabled,"Rabbit visible through closed door");
            for(int i=1;i<colors.Length;i++)
            {
                park.ShowDrawing();RabbitPaintValidation.PaintWithBrush(park.Drawing.Front,colors[i]);RabbitPaintValidation.PaintWithBrush(park.Drawing.Back,colors[i]);
                var front=park.Drawing.Front.Paint.EncodeToPNG();var back=park.Drawing.Back.Paint.EncodeToPNG();
                int house=i<3?firstHouse:1-firstHouse;
                Require(park.TrySubmit(2,front,back,house),"Queued rabbit rejected");
                // Mutating the caller's buffer must not alter a queued friend's snapshot.
                front[0]=0;back[0]=0;
            }
            Require(!park.TrySubmit(AnimalKinds.Count,new byte[]{1},new byte[]{1}),"Unsupported animal accepted");
            Require(!park.TrySubmit(2,Array.Empty<byte>(),Array.Empty<byte>()),"Empty drawing accepted");
            park.ShowPark();Require(park.PendingCount==4,"Queued submissions lost");
            Simulate(park,1.2f);
            Require(first.State==ParkRabbitState.WalkingOut&&park.Houses[firstHouse].DoorFraction>.99f,"Door did not open before emergence");
            var originalRotation=park.Houses[firstHouse].DoorHinge.localRotation;
            Require(Quaternion.Angle(originalRotation,Quaternion.identity)>90,"Door hinge failed to turn");
            var cottage=park.Houses[firstHouse].transform.position;
            CaptureView("Previews/living-park-door.png",cottage+new Vector3(3.5f,3.1f,-8.5f),cottage+new Vector3(0,1.4f,-2.2f));
            Simulate(park,3.8f);
            Require(first.State==ParkRabbitState.Waving&&first.WaveCount==1,"Rabbit did not greet once after exiting");
            CaptureView("Previews/living-park-welcome.png",cottage+new Vector3(2.3f,2.7f,-9.5f),first.transform.position+Vector3.up*.95f);
            Simulate(park,30);
            Require(park.Animals.Count==5&&park.PendingCount==0,"House queues did not drain without replacing friends");
            foreach(var animal in park.Animals)
            {
                Require(animal.State==ParkRabbitState.Wandering&&animal.WaveCount==1,"Wrong arrival/wander state");
                Require(animal.ExitedThroughOpenDoor,"Rabbit crossed a closed door");
                Require(park.WalkArea.Contains(new Vector2(animal.transform.position.x,animal.transform.position.z)),"Rabbit escaped the garden walk area");
                Require(animal.WalkedDistance>5,"Rabbit is not walking through the park");
                Require(animal.Character.Skin.sharedMesh==first.Character.Skin.sharedMesh,"Rabbit mesh is rebuilt for each animal");
                var material=animal.Character.Skin.sharedMaterials[0];var texture=(Texture2D)material.GetTexture("_FrontTex");
                var pixel=texture.GetPixelBilinear(.5f,.5f);var expected=(Color)colors[animal.Id-1];
                Require(Mathf.Abs(pixel.r-expected.r)<.01f&&Mathf.Abs(pixel.g-expected.g)<.01f&&Mathf.Abs(pixel.b-expected.b)<.01f,"Per-animal paint snapshot changed");
                foreach(var other in park.Animals)if(other!=animal)
                    Require(material!=other.Character.Skin.sharedMaterials[0],"Animals share mutable paint materials");
            }
            foreach(var house in park.Houses)Require(!house.IsBusy&&house.DoorFraction==0,"Door did not close after queue completion");
            int count=park.Animals.Count;park.ShowDrawing();Simulate(park,.5f);park.ShowPark();
            Require(park.Animals.Count==count&&park.OrbitCamera.InputEnabled,"Changing pages removed friends or locked the camera");
            CaptureView("Previews/living-park-friends.png",new Vector3(0,22,-25),new Vector3(0,1,0));
            Debug.Log("LIVING PARK PASS: real Apply/cooldown, two working doorways, same-house FIFO queues, five independent painted friends, shared corrected mesh, open-door exits, one greeting each, closed doors, bounded wandering, page navigation and camera input.");
        }
        static void CaptureView(string path,Vector3 position,Vector3 target)
        {
            Directory.CreateDirectory("Previews");var camera=Camera.main;var orbit=camera.GetComponent<ParkCamera>();bool enabled=orbit.enabled;orbit.enabled=false;
            camera.transform.position=position;camera.transform.LookAt(target);
            var previews=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();var skins=new System.Collections.Generic.List<SkinnedMeshRenderer>();
            foreach(var animal in UnityEngine.Object.FindObjectsByType<ParkRabbitAgent>(FindObjectsSortMode.None))
            {
                var skin=animal.Character.Skin;if(!skin.enabled)continue;
                // Bake(false) already contains inherited bone scale in this rig.
                var baked=new Mesh();skin.BakeMesh(baked,false);var preview=new GameObject("Exact park pose capture");
                preview.transform.SetPositionAndRotation(animal.transform.position,animal.transform.rotation);
                preview.AddComponent<MeshFilter>().sharedMesh=baked;preview.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                previews.Add(preview);meshes.Add(baked);skins.Add(skin);skin.enabled=false;
            }
            var rt=new RenderTexture(2560,1440,24){antiAliasing=4};var previous=RenderTexture.active;var oldTarget=camera.targetTexture;
            var texture=new Texture2D(2560,1440,TextureFormat.RGB24,false);
            try
            {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,2560,1440),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=previous;
                foreach(var skin in skins)skin.enabled=true;foreach(var preview in previews)UnityEngine.Object.DestroyImmediate(preview);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
                rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);orbit.enabled=enabled;
            }
        }
    }
}
