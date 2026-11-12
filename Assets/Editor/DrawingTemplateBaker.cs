using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace PeacePark.Editor
{
    public static class DrawingTemplateBaker
    {
        const string Folder="Assets/Resources/Drawing/Templates";
        const string Revision=Folder+"/Revision.txt";
        static readonly string[] Sources={
            "Assets/Scripts/AnimalCanvas.cs","Assets/Scripts/AnimalHead.cs","Assets/Scripts/AnimalKind.cs"
        };

        [InitializeOnLoadMethod]
        static void WatchPlayMode()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.ExitingEditMode)Bake();
            };
        }

        static string RevisionOfSources()
        {
            using var sha=SHA256.Create();
            return string.Join("\n",System.Array.ConvertAll(Sources,path=>
                System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)))));
        }

        public static void Bake()
        {
            string revision=RevisionOfSources();
            bool complete=File.Exists(Revision)&&File.ReadAllText(Revision)==revision;
            for(int animal=0;animal<AnimalKinds.Count&&complete;animal++)
                complete=File.Exists(PathFor(animal,false))&&File.Exists(PathFor(animal,true));
            if(complete)return;

            Directory.CreateDirectory(Folder);
            for(int animal=0;animal<AnimalKinds.Count;animal++)
            for(int side=0;side<2;side++)
            {
                bool back=side==1;
                var canvas=new AnimalCanvas(animal,back,useBaked:false);
                try
                {
                    using var data=new MemoryStream();
                    canvas.WriteTemplate(data);
                    File.WriteAllBytes(PathFor(animal,back),data.ToArray());
                }
                finally{canvas.Dispose();}
            }
            File.WriteAllText(Revision,revision);
            AssetDatabase.Refresh();
            Debug.Log($"DRAWING TEMPLATES BAKED: {AnimalKinds.Count*2} front and back templates.");
        }

        static string PathFor(int animal,bool back)=>"Assets/Resources/"+AnimalCanvas.TemplateResource(animal,back)+".bytes";
    }
}
