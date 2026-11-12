using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark.Editor
{
    public static class RabbitPocSceneSetup
    {
        const string ScenePath="Assets/Scenes/Rabbit3DPoc.unity";

        [MenuItem("Peace Park/Open Rabbit 3D POC")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            Create();
        }

        public static void Create()
        {
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Drawing Page").AddComponent<DrawingPage>().SelectAnimal(2);
            new GameObject("Painted Rabbit").AddComponent<RabbitCharacter>();
            new GameObject("Rabbit 3D POC").AddComponent<Rabbit3DPoc>();

            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Soft preview ground";ground.transform.localScale=new Vector3(2.2f,1,1.5f);
            var groundMaterial=new Material(Shader.Find("Standard")){color=new Color32(176,210,148,255)};groundMaterial.SetFloat("_Glossiness",.05f);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;

            var camera=new GameObject("POC Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,2.65f,-9.2f);camera.transform.LookAt(new Vector3(0,2.15f,0));
            camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(211,231,235,255);camera.allowMSAA=true;
            camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<ParkVisualQuality>();

            var key=new GameObject("Soft key light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.9f;key.color=new Color(1,.93f,.82f);key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(42,-35,0);
            var fill=new GameObject("Face fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.34f;fill.color=new Color(.72f,.86f,1);fill.transform.rotation=Quaternion.Euler(20,155,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.58f,.64f,.68f);RenderSettings.ambientEquatorColor=new Color(.38f,.42f,.38f);RenderSettings.ambientGroundColor=new Color(.24f,.23f,.2f);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        }

        static int frames;
        static bool applied,captured;
        static bool brushRegression;
        public static void CaptureBrushRegression(){brushRegression=true;Capture();}
        public static void Capture()
        {
            frames=0;applied=false;captured=false;
            if(File.Exists(ScenePath))AssetDatabase.DeleteAsset(ScenePath);
            Create();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.update+=CaptureUpdate;EditorApplication.isPlaying=true;
        }

        static void CaptureUpdate()
        {
            if(!EditorApplication.isPlaying)return;frames++;
            if(frames==35&&!applied)
            {
                var page=Object.FindAnyObjectByType<DrawingPage>();
                if(brushRegression)
                {
                    RabbitPaintValidation.PaintWithBrush(page.Front,Color.yellow);
                    RabbitPaintValidation.PaintWithBrush(page.Back,Color.yellow);
                }
                else
                {
                    page.Front.Stroke(new Vector2(220,300),new Vector2(720,820),70,new Color32(54,194,207,255));
                    page.Front.Stroke(new Vector2(720,300),new Vector2(300,920),55,new Color32(239,132,184,255));
                    page.Back.Stroke(new Vector2(250,300),new Vector2(700,850),70,new Color32(255,213,55,255));
                }
                if(!page.ApplyDrawing())throw new System.Exception("Rabbit Apply failed");applied=true;
                var rabbit=Object.FindAnyObjectByType<RabbitCharacter>();
                try
                {
                    RabbitMeshValidation.Check(rabbit);
                    if(brushRegression)
                    {
                        RabbitPaintValidation.CheckSurfaceCoverage(rabbit,Color.yellow,Color.yellow);
                        Debug.Log("YELLOW BRUSH APPLY PASS: DrawingPage.Applied -> RabbitCharacter -> all surface UVs, no white halo.");
                    }
                }
                catch(System.Exception exception){Debug.LogException(exception);EditorApplication.update-=CaptureUpdate;EditorApplication.Exit(1);return;}
                rabbit.enabled=false;
            }
            if(frames==60||frames==130||frames==200||frames==270||frames==340)
            {
                var rabbit=Object.FindAnyObjectByType<RabbitCharacter>();rabbit.PreviewPose(frames==130?3.3f:frames>=270?Mathf.PI/14:2.5f);
                if(frames==200)rabbit.transform.rotation=Quaternion.Euler(0,150,0);
                if(frames==340)rabbit.transform.rotation=Quaternion.Euler(0,55,0);
            }
            if(frames==120||frames==190||frames==260||frames==330||frames==400)
            {
                Directory.CreateDirectory("Previews");
                string name=frames==120?"rabbit-3d-poc":frames==190?"rabbit-connected-wave":frames==260?"rabbit-connected-back":frames==330?"rabbit-walk-front":"rabbit-walk-three-quarter";
                CapturePose("Previews/"+name+".png");captured=true;
            }
            if(frames==410)
            {var rabbit=Object.FindAnyObjectByType<RabbitCharacter>();rabbit.PreviewPose(3.3f);rabbit.transform.rotation=Quaternion.Euler(0,55,0);}
            if(frames==450)CapturePose("Previews/rabbit-wave-three-quarter.png");
            if(brushRegression&&frames==455)
            {var rabbit=Object.FindAnyObjectByType<RabbitCharacter>();rabbit.PreviewPose(2.5f);rabbit.transform.rotation=Quaternion.Euler(0,90,0);}
            if(brushRegression&&frames==460)CapturePose("Previews/rabbit-yellow-side-right.png");
            if(brushRegression&&frames==465)Object.FindAnyObjectByType<RabbitCharacter>().transform.rotation=Quaternion.Euler(0,-90,0);
            if(brushRegression&&frames==466)CapturePose("Previews/rabbit-yellow-side-left.png");
            if(frames==470)
            {
                var rabbit=Object.FindAnyObjectByType<RabbitCharacter>();var page=Object.FindAnyObjectByType<DrawingPage>();
                try{RabbitPaintValidation.CheckAndApplySolid(rabbit,page.Front,page.Back);}
                catch(System.Exception exception){Debug.LogException(exception);EditorApplication.update-=CaptureUpdate;EditorApplication.Exit(1);return;}
                rabbit.PreviewPose(2.5f);rabbit.transform.rotation=Quaternion.Euler(0,90,0);
            }
            if(frames==520)CapturePose("Previews/rabbit-painted-side-right.png");
            if(frames==530)Object.FindAnyObjectByType<RabbitCharacter>().transform.rotation=Quaternion.Euler(0,-90,0);
            if(frames==580)CapturePose("Previews/rabbit-painted-side-left.png");
            if(frames==590)Object.FindAnyObjectByType<RabbitCharacter>().transform.rotation=Quaternion.identity;
            if(frames==640)CapturePose("Previews/rabbit-painted-front.png");
            if(frames==650)Object.FindAnyObjectByType<RabbitCharacter>().transform.rotation=Quaternion.Euler(0,180,0);
            if(frames==700)CapturePose("Previews/rabbit-painted-back.png");
            if(captured&&frames>740&&File.Exists("Previews/rabbit-painted-back.png"))EditorApplication.Exit(0);
            if(frames>1000)EditorApplication.Exit(1);
        }

        // ScreenCapture can be delayed across several editor updates. Bake the exact requested
        // pose and render synchronously so each validation image shows the named pose.
        static void CapturePose(string path)
        {
            var rabbit=Object.FindAnyObjectByType<RabbitCharacter>();var camera=Camera.main;
            var mesh=new Mesh();rabbit.Skin.BakeMesh(mesh);
            var preview=new GameObject("Exact pose capture");preview.transform.SetPositionAndRotation(rabbit.transform.position,rabbit.transform.rotation);
            preview.transform.localScale=rabbit.transform.lossyScale;
            preview.AddComponent<MeshFilter>().sharedMesh=mesh;preview.AddComponent<MeshRenderer>().sharedMaterials=rabbit.Skin.sharedMaterials;
            var target=new RenderTexture(2560,1440,24,RenderTextureFormat.ARGB32){antiAliasing=4};
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            var pixels=new Texture2D(2560,1440,TextureFormat.RGB24,false);
            try
            {
                rabbit.Skin.enabled=false;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,2560,1440),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally
            {
                rabbit.Skin.enabled=true;camera.targetTexture=previousTarget;RenderTexture.active=previous;
                Object.DestroyImmediate(preview);Object.DestroyImmediate(mesh);Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}
