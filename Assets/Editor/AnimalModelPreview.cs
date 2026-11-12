using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace PeacePark.Editor
{
    public static class AnimalModelPreview
    {
        static readonly AnimalKind[] order={
            AnimalKind.Giraffe,AnimalKind.Bear,AnimalKind.Tiger,AnimalKind.Frog,AnimalKind.Elephant,AnimalKind.Triceratops
        };
        static readonly Color32[] palette={
            new Color32(247,194,100,255),new Color32(177,119,81,255),new Color32(255,165,73,255),
            new Color32(121,195,97,255),new Color32(155,174,219,255),new Color32(92,191,170,255)
        };

        public static void CaptureAndBuild()
        {
            Capture();
            EditorSceneManager.OpenScene(ParkPlaygroundSetup.ScenePath);
            DrawingSceneSetup.Validate();
            ParkPlaygroundSetup.BuildWindows();
        }

        public static void Capture()
        {
            // This entry point changes the open scene and is intended for the validation copy.
            if(!Application.dataPath.Replace('\\','/').Contains("/TempArtValidation/"))
                throw new InvalidOperationException("Run the animal capture in TempArtValidation.");
            ParkPlayerBuild.BakeRigs();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Model review camera").AddComponent<Camera>();
            camera.orthographic=true;
            camera.orthographicSize=2.82f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color32(242,239,231,255);
            var key=new GameObject("Model review light").AddComponent<Light>();
            key.type=LightType.Directional;
            key.intensity=1;
            key.transform.rotation=Quaternion.Euler(42,-32,0);
            RenderSettings.sun=key;
            Directory.CreateDirectory("Previews");
            const int width=490,height=570;
            var sheet=new Texture2D(width*order.Length,height*2,TextureFormat.RGB24,false);
            for(int i=0;i<order.Length;i++)
            {
                var kind=order[i];
                var root=new GameObject(kind+" review");
                var model=root.AddComponent<RabbitCharacter>();
                model.EnsureBuilt(kind);
                VerifyConnectedSkin(model);
                var color=new Texture2D(1,1);
                color.SetPixel(0,0,palette[i]);color.Apply();
                var materials=model.Skin.sharedMaterials;
                materials[0].SetTexture("_FrontTex",color);
                materials[0].SetTexture("_BackTex",color);
                var posed=new Mesh();
                VerifyDoorFit(model,posed);
                model.PlayIdlePose();
                model.Skin.BakeMesh(posed);
                model.Skin.enabled=false;
                var preview=new GameObject(kind+" rendered pose");
                preview.AddComponent<MeshFilter>().sharedMesh=posed;
                preview.AddComponent<MeshRenderer>().sharedMaterials=materials;
                var center=new Vector3(0,2.53f,0);
                Look(camera,center,0);
                var front=Render(camera,width,height);
                sheet.SetPixels32(i*width,height,width,height,front);
                Look(camera,center,90);
                var side=Render(camera,width,height);
                sheet.SetPixels32(i*width,0,width,height,side);
                if(kind==AnimalKind.Triceratops)
                {
                    Look(camera,center,34);
                    var review=new Texture2D(width*3,height,TextureFormat.RGB24,false);
                    review.SetPixels32(0,0,width,height,front);
                    review.SetPixels32(width,0,width,height,Render(camera,width,height));
                    review.SetPixels32(width*2,0,width,height,side);
                    review.Apply();
                    File.WriteAllBytes("Previews/triceratops-model.png",review.EncodeToPNG());
                    Object.DestroyImmediate(review);
                }
                if(kind==AnimalKind.Elephant||kind==AnimalKind.Triceratops)
                    CaptureModelHead(camera,model,posed,preview);
                Object.DestroyImmediate(preview);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(posed);
                Object.DestroyImmediate(color);
            }
            sheet.Apply();
            File.WriteAllBytes("Previews/animals-front-and-side.png",sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            CaptureEditedContours();
            AssetDatabase.Refresh();
            Debug.Log($"ANIMAL MODEL REVIEW PASS: {order.Length} connected species, door fit and painted front/side previews.");
        }

        static void CaptureEditedContours()
        {
            var views=new[]{(AnimalKind.Tiger,true),(AnimalKind.Giraffe,false),
                (AnimalKind.Elephant,false),(AnimalKind.Triceratops,true)};
            const int width=360,height=450;
            var sheet=new Texture2D(width*views.Length,height,TextureFormat.RGB24,false);
            for(int column=0;column<views.Length;column++)
            {
                var (kind,back)=views[column];
                var canvas=new AnimalCanvas((int)kind,back,useBaked:false);
                try
                {
                    var outline=canvas.Outline.GetPixels32();
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                    {
                        int index=(y*AnimalCanvas.Height/height)*AnimalCanvas.Width+x*AnimalCanvas.Width/width;
                        var pixel=outline[index];
                        sheet.SetPixel(column*width+x,y,Color.Lerp(Color.white,pixel,pixel.a/255f));
                    }
                }
                finally{canvas.Dispose();}
            }
            sheet.Apply();
            File.WriteAllBytes("Previews/animal-contour-revisions.png",sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        static void Look(Camera camera,Vector3 target,float yaw)
        {
            camera.transform.position=target+Quaternion.Euler(0,yaw,0)*new Vector3(0,.15f,-10);
            camera.transform.LookAt(target);
        }

        static Color32[] Render(Camera camera,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4};
            var pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);
                return pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;
                target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);
            }
        }

        static void CaptureModelHead(Camera camera,RabbitCharacter model,Mesh posed,GameObject preview)
        {
            var head=Object.Instantiate(posed);
            var weights=model.Skin.sharedMesh.boneWeights;
            bool BelongsToHead(int vertex)
            {
                var w=weights[vertex];
                return (w.boneIndex0==1?w.weight0:0)+(w.boneIndex1==1?w.weight1:0)
                     +(w.boneIndex2==1?w.weight2:0)+(w.boneIndex3==1?w.weight3:0)>.55f;
            }
            for(int sub=0;sub<head.subMeshCount;sub++)
            {
                var triangles=posed.GetTriangles(sub);
                var face=new List<int>();
                for(int i=0;i<triangles.Length;i+=3)
                    if(BelongsToHead(triangles[i])&&BelongsToHead(triangles[i+1])&&BelongsToHead(triangles[i+2]))
                    {face.Add(triangles[i]);face.Add(triangles[i+1]);face.Add(triangles[i+2]);}
                head.SetTriangles(face,sub);
            }
            preview.GetComponent<MeshFilter>().sharedMesh=head;
            float originalSize=camera.orthographicSize;
            Color originalBackground=camera.backgroundColor;
            bool dinosaur=model.Kind==AnimalKind.Triceratops;
            camera.orthographicSize=dinosaur?1.58f:1.82f;
            camera.backgroundColor=Color.clear;
            Look(camera,dinosaur?new Vector3(0,3.37f,-.25f):new Vector3(0,2.93f,-.30f),12);
            var icon=new Texture2D(512,512,TextureFormat.RGBA32,false);
            icon.SetPixels32(Render(camera,512,512));icon.Apply();
            File.WriteAllBytes($"Previews/{model.Kind}-model-head.png",icon.EncodeToPNG());
            camera.orthographicSize=originalSize;
            camera.backgroundColor=originalBackground;
            preview.GetComponent<MeshFilter>().sharedMesh=posed;
            Object.DestroyImmediate(icon);
            Object.DestroyImmediate(head);
        }

        static void VerifyConnectedSkin(RabbitCharacter model)
        {
            int count=model.SkinVertexCount;
            if(count<1000)throw new Exception(model.Kind+" skin is missing.");
            var parent=new int[count];
            for(int i=0;i<count;i++)parent[i]=i;
            int Root(int vertex)
            {
                while(parent[vertex]!=vertex){parent[vertex]=parent[parent[vertex]];vertex=parent[vertex];}
                return vertex;
            }
            var edges=new Dictionary<ulong,int>();
            void Connect(int a,int b)
            {
                parent[Root(a)]=Root(b);
                ulong key=((ulong)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
                edges.TryGetValue(key,out int use);
                edges[key]=use+1;
            }
            var triangles=model.SkinTriangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                Connect(a,b);Connect(b,c);Connect(c,a);
            }
            for(int i=1;i<count;i++)if(Root(i)!=Root(0))throw new Exception(model.Kind+" has a disconnected skin.");
            foreach(var use in edges.Values)if(use!=2)throw new Exception(model.Kind+" has an open skin edge.");
        }

        static void VerifyDoorFit(RabbitCharacter model,Mesh mesh)
        {
            float scale=model.Kind==AnimalKind.Giraffe?.40f:model.Kind==AnimalKind.Elephant?.39f:.42f;
            for(int i=0;i<8;i++)
            {
                model.PlayWalkingPose(i*Mathf.PI/4);
                model.Skin.BakeMesh(mesh);
                foreach(var vertex in mesh.vertices)
                {
                    float x=Mathf.Abs(vertex.x*scale);
                    float y=vertex.y*scale+.30f-.15f;
                    if(x>.76f||(y>1.90f&&x*x+(y-1.90f)*(y-1.90f)>.76f*.76f))
                        throw new Exception(model.Kind+" exceeds the doorway in a walking pose.");
                }
            }
            Debug.Log("ANIMAL MODEL PASS: "+model.Kind+" has a closed skin and fits the doorway through the walking cycle.");
        }
    }
}
