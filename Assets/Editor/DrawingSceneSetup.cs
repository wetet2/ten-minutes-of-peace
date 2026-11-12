using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PeacePark.Editor
{
    public static class DrawingSceneSetup
    {
        [MenuItem("Peace Park/Open Drawing Page")]
        public static void Open()
        {
            const string path="Assets/Scenes/Drawing.unity";
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(File.Exists(path)){EditorSceneManager.OpenScene(path);return;}
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Drawing Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;camera.tag="MainCamera";
            new GameObject("Drawing Page").AddComponent<DrawingPage>();
            Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);
        }
        static int captureFrames;
        static bool captured;
        public static void Capture()
        {
            Validate();
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.update+=CaptureUpdate;
            EditorApplication.isPlaying=true;
        }
        static void CaptureUpdate()
        {
            if(!EditorApplication.isPlaying)return;
            captureFrames++;
            if(captureFrames==90)
            {
                Directory.CreateDirectory("Previews");
                ScreenCapture.CaptureScreenshot("Previews/drawing-page-big-head.png");captured=true;
            }
            if(captured&&captureFrames>120&&File.Exists("Previews/drawing-page-big-head.png"))EditorApplication.Exit(0);
            if(captureFrames>600)EditorApplication.Exit(1);
        }
        public static void Validate()
        {
            DrawingTemplateBaker.Bake();
            Open();
            var page=Object.FindAnyObjectByType<DrawingPage>();
            page.SelectAnimal((int)AnimalKind.Rabbit);
            var timer=System.Diagnostics.Stopwatch.StartNew();
            page.SelectAnimal((int)AnimalKind.Tiger);
            double bakedSwitch=timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            var proceduralFront=new AnimalCanvas((int)AnimalKind.Tiger,false,useBaked:false);
            var proceduralBack=new AnimalCanvas((int)AnimalKind.Tiger,true,useBaked:false);
            double proceduralSwitch=timer.Elapsed.TotalMilliseconds;
            proceduralFront.Dispose();proceduralBack.Dispose();
            Debug.Log($"DRAWING TAB SAMPLE: tiger front and back, baked switch {bakedSwitch:F1} ms, procedural generation {proceduralSwitch:F1} ms.");
            var sheet=new Texture2D(360*AnimalKinds.Count,450,TextureFormat.RGB24,false);
            for(int animal=0;animal<AnimalKinds.Count;animal++)for(int side=0;side<2;side++)
            {
                var canvas=new AnimalCanvas(animal,side==1);
                if(animal==(int)AnimalKind.Triceratops)
                    for(int y=(int)(.49f*AnimalCanvas.Height);y<.64f*AnimalCanvas.Height;y++)
                        if(!canvas.IsInside(AnimalCanvas.Width/2,y))
                            throw new System.Exception("Triceratops head is disconnected from its body in the canvas.");
                if(side==0)
                {
                    var paint=canvas.Paint.GetPixels32();var outline=canvas.Outline.GetPixels32();
                    for(int sy=0;sy<450;sy++)for(int sx=0;sx<360;sx++)
                    {
                        int index=Mathf.Min(AnimalCanvas.Height-1,sy*AnimalCanvas.Height/450)*AnimalCanvas.Width+Mathf.Min(AnimalCanvas.Width-1,sx*AnimalCanvas.Width/360);
                        Color col=Color.Lerp(Color.white,paint[index],paint[index].a/255f);col=Color.Lerp(col,outline[index],outline[index].a/255f);
                        sheet.SetPixel(animal*360+sx,sy,col);
                    }
                }
                canvas.Stroke(new Vector2(0,450),new Vector2(959,450),40,Color.red);
                if(canvas.Paint.GetPixel(0,450).a!=0)throw new System.Exception("Paint escaped silhouette");
                if(canvas.Paint.GetPixel(480,450).g>.1f)throw new System.Exception("Stroke missing");
                if(canvas.IsInside(480,120))throw new System.Exception("Legs are not separated");
                if(!canvas.IsInside(370,140)||!canvas.IsInside(590,140))throw new System.Exception("Standing legs missing");
                if(!canvas.IsInside(275,410)||!canvas.IsInside(685,410))throw new System.Exception("Soft arms missing");
                canvas.Dispose();
            }
            sheet.Apply();Directory.CreateDirectory("Previews");File.WriteAllBytes("Previews/animal-species-templates.png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
            page.SelectAnimal(2);
            if(!page.ApplyDrawing()||page.ApplyDrawing())throw new System.Exception("Apply lock failed");
            page.Front.Stroke(new Vector2(480,360),new Vector2(520,400),40,Color.red);
            page.SelectAnimal(0);
            if(page.Front.Paint.GetPixel(480,360).g<.99f)throw new System.Exception("Animal switch retained paint");
            Debug.Log($"DRAWING CHECKS PASSED: {AnimalKinds.Count*2} masks, stroke clipping, apply cooldown, reset on animal selection.");
        }

        public static void ValidateAndBuild()
        {
            Validate();
            ParkPlaygroundSetup.BuildWindows();
        }
    }
}
