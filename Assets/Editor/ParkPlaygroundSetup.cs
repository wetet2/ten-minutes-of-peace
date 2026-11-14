using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PeacePark.Editor
{
    public static class ParkPlaygroundSetup
    {
        public const string ScenePath="Assets/Scenes/ParkPlayground.unity";
        const string SourceScene="Assets/Scenes/ParkPolished.unity";
        const string AssetFolder="Assets/PlaygroundGenerated";

        [MenuItem("Peace Park/Open Living Park")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(File.Exists(ScenePath))EditorSceneManager.OpenScene(ScenePath);else Create();
        }
        public static void Create()
        {
            if(File.Exists(ScenePath))throw new System.InvalidOperationException("Living park already exists; open it instead of overwriting edits.");
            if(!File.Exists(SourceScene))throw new FileNotFoundException("Polished park scene missing",SourceScene);
            if(!AssetDatabase.CopyAsset(SourceScene,ScenePath))throw new IOException("Could not copy the park scene");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(AssetFolder);AssetDatabase.Refresh();
            var flower=GameObject.Find("Flower cottage");var candy=GameObject.Find("Candy cottage");
            var houses=new[]{ConfigureHouse(flower.transform,"꽃집"),ConfigureHouse(candy.transform,"과자집")};
            var drawing=new GameObject("Drawing Page").AddComponent<DrawingPage>();
            var controller=new GameObject("Living Park").AddComponent<ParkPlayground>();controller.Drawing=drawing;controller.Houses=houses;
            controller.RabbitPaintShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/RabbitPaint.shader");
            var orbit=Object.FindAnyObjectByType<ParkCamera>();controller.OrbitCamera=orbit;orbit.ShowControls=false;orbit.distance=32;
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        }
        static Transform Group(Transform parent,string name,Vector3 position)
        {var child=new GameObject(name).transform;child.SetParent(parent,false);child.localPosition=position;return child;}
        static void Piece(Transform parent,string name,Vector3 position,Vector3 scale,Mesh mesh,Material material)
        {
            var piece=Group(parent,name,position);piece.localScale=scale;
            piece.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;piece.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static ParkHouse ConfigureHouse(Transform house,string name)
        {
            var walls=house.Find("Walls");var box=walls.GetComponent<MeshFilter>().sharedMesh;var wallMaterial=walls.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(walls.gameObject);
            // Actual side/rear walls and a facade with a real doorway replace the solid box.
            Piece(house,"Left wall",new Vector3(-2.39f,1.8f,0),new Vector3(.22f,3.3f,4),box,wallMaterial);
            Piece(house,"Right wall",new Vector3(2.39f,1.8f,0),new Vector3(.22f,3.3f,4),box,wallMaterial);
            Piece(house,"Rear wall",new Vector3(0,1.8f,1.89f),new Vector3(5,3.3f,.22f),box,wallMaterial);
            for(int side=-1;side<=1;side+=2)
                Piece(house,"Doorway side wall",new Vector3(side*1.665f,1.8f,-1.89f),new Vector3(1.67f,3.3f,.22f),box,wallMaterial);
            var lintel=Persist(Lintel(),name+" doorway lintel");Piece(house,"Curved doorway lintel",new Vector3(0,0,-1.89f),Vector3.one,lintel,wallMaterial);
            var surround=house.Find("Arched door surround");var frameMaterial=surround.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(surround.gameObject);
            Piece(house,"Open arched frame",new Vector3(0,.15f,-2.08f),Vector3.one,Persist(ArchFrame(),name+" open arch"),frameMaterial);
            var interior=new Material(Shader.Find("Standard")){name=name+" interior",color=new Color32(67,45,34,255)};
            interior.SetFloat("_Glossiness",0);AssetDatabase.CreateAsset(interior,AssetDatabase.GenerateUniqueAssetPath(AssetFolder+"/"+interior.name+".mat"));
            Piece(house,"Interior shadow wall",new Vector3(0,1.55f,.65f),new Vector3(2.4f,2.8f,.08f),box,interior);
            var hinge=Group(house,"Door hinge",new Vector3(-.76f,.17f,-2.23f));var moving=new List<Transform>();
            foreach(Transform child in house)
                if(child.name=="Arched door"||child.name=="Door knob"||child.name=="Door plank seam")moving.Add(child);
            foreach(var child in moving)child.SetParent(hinge,true);
            var logic=house.gameObject.AddComponent<ParkHouse>();logic.DisplayName=name;logic.DoorHinge=hinge;
            logic.Inside=Group(house,"Animal inside point",new Vector3(0,.305f,-1));
            logic.Threshold=Group(house,"Animal threshold point",new Vector3(0,.27f,-2.65f));
            logic.Welcome=Group(house,"Animal welcome point",new Vector3(0,.04f,-4.2f));
            return logic;
        }
        static Mesh Persist(Mesh mesh,string name)
        {mesh.name=name;AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(AssetFolder+"/"+name+".asset"));return mesh;}

        sealed class Surface
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<int> triangles=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {int start=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
            public Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var uv=new List<Vector2>();foreach(var p in vertices)uv.Add(new Vector2(p.x*.3f,p.y*.3f));mesh.SetUVs(0,uv);return mesh;
            }
        }
        static Mesh Lintel()
        {
            var surface=new Surface();const float radius=.83f,spring=2.09f,top=3.45f,z=.11f;
            for(int i=0;i<24;i++)
            {
                float a=Mathf.PI-i*Mathf.PI/24,b=Mathf.PI-(i+1)*Mathf.PI/24;
                float x0=Mathf.Cos(a)*radius,x1=Mathf.Cos(b)*radius,y0=spring+Mathf.Sin(a)*radius,y1=spring+Mathf.Sin(b)*radius;
                var p=new Vector3(x0,y0,-z);var q=new Vector3(x0,top,-z);var r=new Vector3(x1,top,-z);var s=new Vector3(x1,y1,-z);
                var depth=Vector3.forward*z*2;
                surface.Quad(p,q,r,s);surface.Quad(s+depth,r+depth,q+depth,p+depth);
                surface.Quad(p,s,s+depth,p+depth);surface.Quad(q+depth,r+depth,r,q);
                if(i==0)surface.Quad(p+depth,q+depth,q,p);
                if(i==23)surface.Quad(s,r,r+depth,s+depth);
            }
            return surface.Mesh("Open doorway lintel");
        }
        static List<Vector2> ArchPath(float width,float height)
        {
            float radius=width/2;var points=new List<Vector2>{new Vector2(-radius,0)};
            for(int i=0;i<=24;i++){float angle=Mathf.PI-i*Mathf.PI/24;points.Add(new Vector2(Mathf.Cos(angle)*radius,height-radius+Mathf.Sin(angle)*radius));}
            points.Add(new Vector2(radius,0));return points;
        }
        static Mesh ArchFrame()
        {
            var outer=ArchPath(1.9f,2.95f);var inner=ArchPath(1.52f,2.66f);var surface=new Surface();const float z=.11f;
            for(int i=0;i<outer.Count-1;i++)
            {
                Vector3 a=new Vector3(outer[i].x,outer[i].y,-z),b=new Vector3(outer[i+1].x,outer[i+1].y,-z);
                Vector3 c=new Vector3(inner[i+1].x,inner[i+1].y,-z),d=new Vector3(inner[i].x,inner[i].y,-z);var depth=Vector3.forward*z*2;
                surface.Quad(a,b,c,d);surface.Quad(d+depth,c+depth,b+depth,a+depth);
                surface.Quad(a+depth,b+depth,b,a);surface.Quad(d,c,c+depth,d+depth);
                if(i==0)surface.Quad(a,d,d+depth,a+depth);
                if(i==outer.Count-2)surface.Quad(b+depth,c+depth,c,b);
            }
            return surface.Mesh("Open arched frame");
        }
        [MenuItem("Peace Park/Build Living Park Windows")]
        public static void BuildWindows()
        {
            DrawingTemplateBaker.Bake();
            ParkPlayerBuild.BakeRigs();
            PlayerSettings.gpuSkinning=true;
            PlayerSettings.defaultScreenWidth=2560;PlayerSettings.defaultScreenHeight=1440;PlayerSettings.resizableWindow=true;
            if(!File.Exists(ScenePath))Create();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},ParkPlayerBuild.WindowsExecutable,BuildTarget.StandaloneWindows64,BuildOptions.CleanBuildCache);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Living park build failed: "+report.summary.result);
            File.Copy("Assets/Resources/Fonts/BaskinRobbins-NOTICE.txt","Builds/Windows/BaskinRobbins-NOTICE.txt",true);
        }

        [MenuItem("Peace Park/Build Living Park Android APK")]
        public static void BuildAndroid()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
                throw new System.InvalidOperationException("Install Android Build Support, SDK & NDK Tools, and OpenJDK in Unity Hub.");
            DrawingTemplateBaker.Bake();
            ParkPlayerBuild.BakeRigs();
            if(!File.Exists(ScenePath))Create();
            const string folder="Builds/Android";
            Directory.CreateDirectory(folder);
            string published=Path.Combine(folder,"TenMinutesOfPeace.apk");
            var nextVersion=AndroidApkVersion.Next(published);
            string staging=Path.Combine(folder,".staging");
            Directory.CreateDirectory(staging);
            string staged=Path.Combine(staging,Path.GetFileName(published));
            string previousVersion=PlayerSettings.bundleVersion;
            int previousCode=PlayerSettings.Android.bundleVersionCode;

            var deformation=PlayerSettings.meshDeformation;
            bool autoGraphics=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            var graphics=PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            bool appBundle=EditorUserBuildSettings.buildAppBundle;
            bool exportProject=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool internetPermission=PlayerSettings.Android.forceInternetPermission;
            bool portrait=PlayerSettings.allowedAutorotateToPortrait;
            bool upsideDown=PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool landscapeLeft=PlayerSettings.allowedAutorotateToLandscapeLeft;
            bool landscapeRight=PlayerSettings.allowedAutorotateToLandscapeRight;
            var orientation=PlayerSettings.defaultInterfaceOrientation;
            try
            {
                // Keep the multi-frame corrective blend shapes on the more portable CPU path.
                PlayerSettings.meshDeformation=MeshDeformation.CPU;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
                PlayerSettings.Android.forceInternetPermission=true;
                PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait=PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
                PlayerSettings.allowedAutorotateToLandscapeLeft=PlayerSettings.allowedAutorotateToLandscapeRight=true;
                EditorUserBuildSettings.buildAppBundle=false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
                PlayerSettings.bundleVersion=nextVersion.Name;
                PlayerSettings.Android.bundleVersionCode=nextVersion.Code;
                var report=BuildPipeline.BuildPlayer(new[]{ScenePath},staged,BuildTarget.Android,BuildOptions.None);
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new System.InvalidOperationException("Android APK build failed: "+report.summary.result);
                if(AndroidApkVersion.Read(staged)!=nextVersion)
                    throw new System.InvalidOperationException("The generated APK has an unexpected version.");
                if(File.Exists(published))File.Replace(staged,published,null);
                else File.Move(staged,published);
                Debug.Log($"Android APK {nextVersion.Name} (build {nextVersion.Code}): {published}");
            }
            finally
            {
                PlayerSettings.bundleVersion=previousVersion;
                PlayerSettings.Android.bundleVersionCode=previousCode;
                PlayerSettings.meshDeformation=deformation;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,graphics);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,autoGraphics);
                PlayerSettings.Android.forceInternetPermission=internetPermission;
                PlayerSettings.defaultInterfaceOrientation=orientation;
                PlayerSettings.allowedAutorotateToPortrait=portrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown=upsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft=landscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeRight=landscapeRight;
                EditorUserBuildSettings.buildAppBundle=appBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject=exportProject;
            }
        }
    }
}
