using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PeacePark.Editor
{
    public static class ParkPorchSetup
    {
        const string Folder="Assets/PorchGenerated";
        static Color Hex(string value){ColorUtility.TryParseHtmlString(value,out var color);return color;}
        static Material Paint(string name,string color)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("PeacePark/Storybook"));AssetDatabase.CreateAsset(material,path);}
            material.color=Hex(color);material.SetColor("_Shade",new Color(.48f,.65f,.60f));EditorUtility.SetDirty(material);return material;
        }
        static Transform Piece(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Vector3 scale)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localScale=scale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        static Mesh PorchMesh()
        {
            var controls=new[]{new Vector2(-.86f,-2.16f),new Vector2(.86f,-2.16f),new Vector2(1.08f,-2.38f),
                new Vector2(1.36f,-3.04f),new Vector2(1.27f,-3.47f),new Vector2(.90f,-3.7f),
                new Vector2(-.90f,-3.7f),new Vector2(-1.27f,-3.47f),new Vector2(-1.36f,-3.04f),new Vector2(-1.08f,-2.38f)};
            var outline=new List<Vector2>();
            for(int k=0;k<controls.Length;k++)for(int j=0;j<8;j++)
            {
                float t=j/8f;var a=controls[(k+controls.Length-1)%controls.Length];var b=controls[k];var c=controls[(k+1)%controls.Length];var d=controls[(k+2)%controls.Length];
                outline.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
            var vertices=new List<Vector3>();var top=new List<int>();var sides=new List<int>();int count=outline.Count;
            vertices.Add(new Vector3(0,ParkHouse.PorchHeight(-2.95f),-2.95f));
            // A softly bevelled perimeter surrounds one uninterrupted sloping top.
            for(int ring=0;ring<3;ring++)foreach(var p in outline)
            {
                var q=ring==0?Vector2.Lerp(p,new Vector2(0,-2.95f),.035f):p;
                vertices.Add(new Vector3(q.x,ring==2?-.035f:ParkHouse.PorchHeight(q.y)-(ring==1?.025f:0),q.y));
            }
            for(int i=0;i<count;i++)
            {
                int j=(i+1)%count;top.AddRange(new[]{0,1+i,1+j});
                for(int ring=0;ring<2;ring++)
                {int a=1+ring*count+i,b=1+ring*count+j,c=a+count,d=b+count;sides.AddRange(new[]{a,c,b,b,c,d});}
            }
            var mesh=new Mesh{name="Rounded continuous porch"};mesh.SetVertices(vertices);mesh.subMeshCount=2;
            mesh.SetTriangles(top,0);mesh.SetTriangles(sides,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh Persist(Mesh mesh)
        {
            string path=Folder+"/Rounded porch.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        public static void ApplyToOpenScene()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var mesh=Persist(PorchMesh());
            var sphereObject=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=sphereObject.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(sphereObject);
            var lavender=Paint("Lavender edging","#C5AADF");var ivory=Paint("Flower paving","#F6E7CF");
            var biscuit=Paint("Golden biscuit","#E7B67A");var caramel=Paint("Biscuit edging","#BC803F");
            var pink=Paint("Strawberry inlay","#EF83AE");var white=Paint("Vanilla inlay","#FFF7E3");var mint=Paint("Mint inlay","#85CBB0");
            foreach(var house in Object.FindObjectsByType<ParkHouse>(FindObjectsSortMode.None))
            {
                foreach(string name in new[]{"Step","Cottage walkway","Rounded themed porch"})
                {var old=house.transform.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
                bool candy=house.name=="Candy cottage";var group=new GameObject("Rounded themed porch").transform;group.SetParent(house.transform,false);
                var slab=Piece(group,"Sloping rounded landing",mesh,candy?biscuit:ivory,Vector3.zero,Vector3.one);
                slab.GetComponent<MeshRenderer>().sharedMaterials=new[]{candy?biscuit:ivory,candy?caramel:lavender};
                // Flat inlays sit at the sides, leaving the doorway and walking line clear.
                for(int side=-1;side<=1;side+=2)
                {
                    var center=new Vector3(side*1.04f,ParkHouse.PorchHeight(-3.12f)+.006f,-3.12f);
                    if(!candy)
                    {
                        for(int i=0;i<5;i++)
                        {
                            float angle=i*Mathf.PI*2/5;var offset=new Vector3(Mathf.Cos(angle)*.085f,0,Mathf.Sin(angle)*.085f);
                            offset.y=ParkHouse.PorchHeight(center.z+offset.z)-ParkHouse.PorchHeight(center.z);
                            var petal=Piece(group,"Inlaid flower petal",sphere,pink,center+offset,new Vector3(.14f,.014f,.085f));petal.localRotation=Quaternion.Euler(-10.74f, -angle*Mathf.Rad2Deg,0);
                        }
                        Piece(group,"Inlaid flower centre",sphere,white,center+Vector3.up*.004f,new Vector3(.095f,.018f,.095f));
                    }
                    else for(int i=0;i<3;i++)
                    {
                        float z=-2.88f-i*.25f;var dot=Piece(group,"Candy inset",sphere,i%2==0?pink:mint,new Vector3(side*(1.05f+i*.02f),ParkHouse.PorchHeight(z)+.006f,z),new Vector3(.11f,.014f,.16f));
                        dot.localRotation=Quaternion.Euler(-10.74f,0,0);
                    }
                }
                house.HasRoundedPorch=true;
                house.Threshold.localPosition=new Vector3(0,ParkHouse.PorchHeight(-2.65f),-2.65f);
                house.Welcome.localPosition=new Vector3(0,.025f,-4.2f);EditorUtility.SetDirty(house);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("PORCH REFINEMENT APPLIED: one low bevelled landing per cottage; flower/lavender stone and candy/biscuit inlays; exit path follows ramp height.");
        }
        [MenuItem("Peace Park/Refine Cottage Entrances")]
        public static void Apply()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ParkPlaygroundSetup.ScenePath);ApplyToOpenScene();
        }
        public static void PrepareAndBuild()
        {EditorSceneManager.OpenScene(ParkPlaygroundSetup.ScenePath);ApplyToOpenScene();ParkPlaygroundSetup.BuildWindows();}
    }
}
