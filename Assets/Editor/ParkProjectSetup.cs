using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark.Editor
{
    [InitializeOnLoad]
    public static class ParkProjectSetup
    {
        const string ScenePath = "Assets/Scenes/ParkPolished.unity";
        static ParkProjectSetup() { EditorApplication.delayCall += FirstOpen; }
        static void FirstOpen()
        {
            if (!File.Exists("Assets/Scenes/Park.unity") && !File.Exists("Assets/Scenes/ParkReference.unity") && !File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isBatchMode) Create();
        }
        [MenuItem("Peace Park/Create Park Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild park?", "The generated park scene will be replaced. Save your edited scene under another name first.", "Rebuild", "Cancel")) return;
            Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Materials"); Directory.CreateDirectory("Assets/Meshes"); Directory.CreateDirectory("Assets/Textures");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ParkBuilder.Build();
            foreach(var texture in ParkSurface.Textures)
                AssetDatabase.CreateAsset(texture,AssetDatabase.GenerateUniqueAssetPath("Assets/Textures/"+texture.name+".asset"));
            foreach (var mat in ParkBuilder.Materials)
            {
                string path="Assets/Materials/"+mat.name+".mat";
                path=AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(mat,path);
            }
            foreach(var mesh in ParkGeometry.Meshes)
                AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath("Assets/Meshes/"+mesh.name+".asset"));
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.38f,.42f,.48f);
            RenderSettings.ambientEquatorColor=new Color(.3f,.34f,.28f);
            RenderSettings.ambientGroundColor=new Color(.2f,.19f,.16f);
            RenderSettings.fog=false;
            var sun=new GameObject("Afternoon sun").AddComponent<Light>();
            sun.type=LightType.Directional;sun.intensity=.85f;sun.shadowStrength=.48f;sun.color=new Color(1,.94f,.84f);sun.shadows=LightShadows.Soft;sun.shadowCustomResolution=4096;sun.shadowBias=.025f;sun.shadowNormalBias=.18f;
            sun.transform.rotation=Quaternion.Euler(48,-35,0); RenderSettings.sun=sun;
            QualitySettings.shadowDistance=75;QualitySettings.shadows=ShadowQuality.All;QualitySettings.antiAliasing=8;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadowCascades=4;QualitySettings.shadowProjection=ShadowProjection.StableFit;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;QualitySettings.lodBias=2;
            var camera=new GameObject("Park Camera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.76f,.87f,.91f);
            camera.allowMSAA=true;camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=150;
            camera.gameObject.AddComponent<ParkVisualQuality>();camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<ParkCamera>();
            PlayerSettings.companyName="Peace Park";
            PlayerSettings.runInBackground=true;PlayerSettings.defaultScreenWidth=2560;PlayerSettings.defaultScreenHeight=1440;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,0),Quaternion.Euler(48,0,0),22);
        }
        public static void GenerateAndCapture()
        {
            if(!File.Exists(ScenePath)) Create();
            else EditorSceneManager.OpenScene(ScenePath);
            var camera=Camera.main;
            camera.transform.position=new Vector3(0,22,-25);
            camera.transform.LookAt(new Vector3(0,1,0));
            var rt=new RenderTexture(3840,2160,24){antiAliasing=8};camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;var image=new Texture2D(3840,2160,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,3840,2160),0,0);image.Apply();
            Directory.CreateDirectory("Previews");File.WriteAllBytes("Previews/park-polished-4k.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }
        [MenuItem("Peace Park/Build Windows Preview")]
        public static void BuildWindows()
        {
            if(!File.Exists(ScenePath)) Create();
            Directory.CreateDirectory("Builds/Windows");
            var report=ParkPlayerBuild.BuildWindows(ScenePath,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed: "+report.summary.result);
        }
    }
}
