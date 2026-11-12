using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark.Editor
{
    public static class ParkArtDirection
    {
        const string Folder="Assets/StorybookGenerated";
        static readonly Dictionary<string,string> Colors=new Dictionary<string,string>
        {
            {"Meadow","#78BF58"},{"Leaves","#43A950"},{"Honey wood","#C7833D"},
            {"Warm cream","#FFE0A0"},{"Petal pink","#FF73B1"},{"Lavender","#A06CE8"},
            {"Butter yellow","#FFD442"},{"Icing","#FFF8DF"},{"Cocoa","#98532E"},
            {"Window blue","#57C7E7"},{"Peach plaster","#FFB378"}
        };
        static Color Hex(string value){ColorUtility.TryParseHtmlString(value,out var color);return color;}
        static Material Material(string name,string color,float variation=0)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("PeacePark/Storybook"));AssetDatabase.CreateAsset(material,path);}
            material.color=Hex(color);material.SetColor("_Shade",new Color(.48f,.65f,.60f));material.SetFloat("_Variation",variation);
            EditorUtility.SetDirty(material);return material;
        }
        static Transform Place(string name,Mesh mesh,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localScale=scale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        static Mesh Save(Mesh mesh,string name)
        {
            mesh.name=name;string path=Folder+"/"+name+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous){EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(previous);return previous;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh Crown()
        {
            var vertices=new List<Vector3>();var indices=new List<int>();const int rings=20,sides=40;
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            {
                float phi=Mathf.PI*j/rings,a=Mathf.PI*2*i/sides,s=Mathf.Sin(phi);
                float lobes=1+.085f*Mathf.Cos(a*5+phi*.7f)*s*s+.035f*Mathf.Sin(a*3-phi*2)*s;
                vertices.Add(new Vector3(Mathf.Cos(a)*s*lobes,Mathf.Cos(phi)*(.96f+.04f*Mathf.Sin(a*3)*s),Mathf.Sin(a)*s*lobes));
                if(j<rings&&i<sides){int k=j*(sides+1)+i;indices.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});}
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        [MenuItem("Peace Park/Apply Storybook Art")]
        public static void Apply()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ParkPlaygroundSetup.ScenePath);ApplyToOpenScene();
        }
        public static void ApplyToOpenScene()
        {
            var existing=GameObject.Find("Storybook countryside");if(existing)Object.DestroyImmediate(existing);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var root=GameObject.Find("Peace Park");var replacements=new Dictionary<Material,Material>();
            var crown=Save(Crown(),"Soft lobed tree crown");
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var source=renderer.sharedMaterial;if(!source)continue;
                if(!replacements.TryGetValue(source,out var replacement))
                {
                    foreach(var pair in Colors)if(source.name.StartsWith(pair.Key))
                    {replacement=Material(pair.Key,pair.Value,pair.Key=="Meadow"?.025f:0);break;}
                    if(!replacement)replacement=Material(source.name,ColorUtility.ToHtmlStringRGB(source.color).Insert(0,"#"));
                    replacements[source]=replacement;
                }
                renderer.sharedMaterial=replacement;
                if(renderer.name=="Crown"||renderer.name=="Lower crown")renderer.GetComponent<MeshFilter>().sharedMesh=crown;
            }
            // Keep the original lawn surface and walk coordinates; bury its thin display plinth.
            var earth=GameObject.Find("Earth edge");if(earth)earth.SetActive(false);
            var group=new GameObject("Storybook countryside").transform;
            var meadow=Material("Meadow",Colors["Meadow"],.025f);var hills=Material("Distant green","#79BD68");
            var farHills=Material("Far meadow","#91CE88");var cloud=Material("Cloud cream","#FFFDF0");
            var sphereObject=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=sphereObject.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(sphereObject);
            var floor=new Mesh();floor.vertices=new[]{new Vector3(-300,-.026f,-300),new Vector3(-300,-.026f,300),new Vector3(300,-.026f,300),new Vector3(300,-.026f,-300)};
            floor.triangles=new[]{0,1,2,0,2,3};floor.RecalculateNormals();floor.RecalculateBounds();
            Place("Continuous meadow",Save(floor,"Continuous meadow"),group,Vector3.zero,Vector3.one,meadow);
            // Hills sit beyond the playable fence and continue all around the orbit camera.
            for(int i=0;i<14;i++)
            {
                float angle=i*Mathf.PI*2/14,dist=48+(i%3)*9;
                Place("Rolling hill",sphere,group,new Vector3(Mathf.Sin(angle)*dist,-2,Mathf.Cos(angle)*dist),new Vector3(38+(i%3)*8,13+(i%4)*2,30+(i%3)*7),i%2==0?hills:farHills);
            }
            // A connected welcoming trail continues beyond the gate instead of ending at a platform edge.
            var trail=new Mesh();var trailPoints=new List<Vector3>();var trailIndices=new List<int>();
            for(int i=0;i<=24;i++)
            {
                float t=i/24f,z=-12-t*46,x=Mathf.Pow(Mathf.Sin(t*Mathf.PI),2)*4;
                trailPoints.Add(new Vector3(x-1.1f,.026f,z));trailPoints.Add(new Vector3(x+1.1f,.026f,z));
                if(i<24){int k=i*2;trailIndices.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            }
            trail.SetVertices(trailPoints);trail.SetTriangles(trailIndices,0);trail.RecalculateNormals();trail.RecalculateBounds();
            Place("Welcome trail",Save(trail,"Countryside trail"),group,Vector3.zero,Vector3.one,Material("Warm cream",Colors["Warm cream"]));
            // Small groups in the middle distance make a continuous world visible when orbiting.
            var tree=root.transform.Find("Faceted garden tree");
            for(int i=0;i<18&&tree;i++)
            {
                float a=i*Mathf.PI*2/18,dist=25+(i%4)*2.3f;
                var copy=Object.Instantiate(tree.gameObject,group).transform;copy.name="Meadow tree";
                copy.position=new Vector3(Mathf.Sin(a)*dist,0,Mathf.Cos(a)*dist);copy.localScale=Vector3.one*(.65f+(i%3)*.12f);
            }
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*2/8;var center=new Vector3(Mathf.Sin(a)*80,23+(i%3)*4,Mathf.Cos(a)*80);
                for(int j=0;j<3;j++)Place("Soft cloud",sphere,group,center+new Vector3((j-1)*3.3f,j==1?1:0,0),new Vector3(8,3.5f,4),cloud);
            }
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("#D5F4FF");
            RenderSettings.ambientEquatorColor=Hex("#AECDA3");RenderSettings.ambientGroundColor=Hex("#819969");
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=Hex("#BAE8F2");RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=155;
            if(RenderSettings.sun){RenderSettings.sun.color=Hex("#FFF8E9");RenderSettings.sun.intensity=.95f;RenderSettings.sun.shadowStrength=.65f;}
            var camera=Camera.main;camera.backgroundColor=Hex("#BAE8F2");camera.farClipPlane=220;
            var orbit=camera.GetComponent<ParkCamera>();orbit.pitch=43;orbit.distance=32;
            ParkPorchSetup.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);EditorSceneManager.SaveScene(camera.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("STORYBOOK ART APPLIED: continuous countryside, non-tiling saturated palette, soft lobed trees, atmospheric distant hills.");
        }
        public static void PrepareAndBuild()
        {EditorSceneManager.OpenScene(ParkPlaygroundSetup.ScenePath);ApplyToOpenScene();ParkPlaygroundSetup.BuildWindows();}
    }
}
