using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeacePark.Editor
{
    public sealed class ParkPlayerBuild : IProcessSceneWithReport
    {
        public const string WindowsExecutable = "Builds/Windows/TenMinutesOfPeace.exe";

        public int callbackOrder=>0;
        public void OnProcessScene(Scene scene,BuildReport report)
        {
            if(scene.path!=ParkPlaygroundSetup.ScenePath)return;
            ParkGardenLayout.Apply(scene);
            int importedTrees=ParkNaturePack.Apply(scene);
            if(importedTrees>0)Debug.Log($"NATURE PACK: applied {importedTrees} tree prefabs.");
            ParkRoofDecor.Apply(scene);
            ParkButterflySetup.Apply(scene);
            ParkVenuesSetup.Apply(scene);
            foreach(var root in scene.GetRootGameObjects())
            foreach(var house in root.GetComponentsInChildren<ParkHouse>(true))
                house.transform.localScale*=1.1f;
            if(report==null)return;
            foreach(var root in scene.GetRootGameObjects())foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                // SurfaceHeight reads the small path mesh once at startup.
                if(renderer.name=="Winding path")
                {GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,0);continue;}
                var house=renderer.GetComponentInParent<ParkHouse>();
                if(house&&renderer.transform.IsChildOf(house.DoorHinge))
                {GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,0);continue;}
                var fountain=renderer.GetComponentInParent<ParkFountain>();
                if(renderer.GetComponentInParent<RoofSunflowerSpin>()||renderer.GetComponentInParent<ParkButterfly>()||
                    (fountain&&fountain.IsMovingPart(renderer.transform)))
                {GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,0);continue;}
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic);
            }
        }

        public static void BakeRigs()
        {
            for(int index=0;index<AnimalKinds.Count;index++)BakeRig((AnimalKind)index);
        }

        static void BakeRig(AnimalKind kind)
        {
            string rigPath="Assets/Resources/"+AnimalKinds.RigResource(kind)+".asset";
            var rig=AssetDatabase.LoadAssetAtPath<RabbitRigAsset>(rigPath);
            int version=RabbitMesh.GeometryVersion;
            if(rig&&rig.Mesh&&rig.GeometryVersion==version)return;
            Directory.CreateDirectory(Path.GetDirectoryName(rigPath));AssetDatabase.Refresh();
            var go=new GameObject("Bake corrected "+kind+" for player");var probe=new Mesh();
            try
            {
                var rabbit=go.AddComponent<RabbitCharacter>();rabbit.EnsureBuilt(kind);
                if(!rig)
                {
                    rig=ScriptableObject.CreateInstance<RabbitRigAsset>();
                    AssetDatabase.CreateAsset(rig,rigPath);
                }
                var sourceMesh=rabbit.Skin.sharedMesh;
                if(rig.Mesh)EditorUtility.CopySerialized(sourceMesh,rig.Mesh);
                else
                {
                    rig.Mesh=Object.Instantiate(sourceMesh);
                    AssetDatabase.AddObjectToAsset(rig.Mesh,rig);
                }
                rig.Mesh.name="Baked "+kind+" with shoulder corrections";
                rig.SkinVertexCount=rabbit.SkinVertexCount;rig.SkinTriangles=rabbit.SkinTriangles;
                rabbit.PlayIdlePose();rabbit.Skin.BakeMesh(probe);probe.RecalculateBounds();rig.IdleFloor=probe.bounds.min.y;
                rig.WalkFloor=new float[65];
                for(int i=0;i<rig.WalkFloor.Length;i++)
                {rabbit.PlayWalkingPose(i*Mathf.PI*2/64);rabbit.Skin.BakeMesh(probe);probe.RecalculateBounds();rig.WalkFloor[i]=probe.bounds.min.y;}
                rig.GeometryVersion=version;
                EditorUtility.SetDirty(rig.Mesh);
                EditorUtility.SetDirty(rig);
                AssetDatabase.SaveAssets();
                Debug.Log($"ANIMAL RIG BAKED: {kind}, {rig.Mesh.vertexCount} vertices, idle floor {rig.IdleFloor:F4}, 65 contact samples.");
            }
            finally{Object.DestroyImmediate(probe);Object.DestroyImmediate(go);}
        }
    }
}
