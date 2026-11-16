using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;
using Object = UnityEngine.Object;

namespace PeacePark.Editor
{
    internal static class ParkForestSetup
    {
        const string Pack = "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/";
        internal static bool IsInstalled => AssetDatabase.IsValidFolder(Pack+"Trees/Fir/Prefabs");
        static readonly Vector2 PondCenter = new Vector2(5.8f, -4.7f);
        static readonly Vector2 PondSize = new Vector2(3.2f, 2.25f);
        const float Ground = -.026f;

        internal static ParkVenue Create(Transform root, ParkVenue garden)
        {
            var nature = new NatureModels(root);
            var sphere = PrimitiveMesh(PrimitiveType.Sphere);
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            var meadow = Paint("Forest lawn", "#83B652");
            meadow.SetFloat("_Variation", .067f);
            meadow.SetFloat("_GrassDetail", .22f);
            var hill = Paint("Forest hills", "#618D65");
            hill.SetFloat("_GrassDetail", .15f);
            var water = Paint("Forest pond turquoise", "#60B9B4");
            var rim = Paint("Pond shore", "#9AAB73");
            var pad = Paint("Water lily leaves", "#6C984B");
            var petal = Paint("Water lily petals", "#FFD5B5");
            var pollen = Paint("Water lily centers", "#F9BF58");
            var dirt = new Mesh { name = "Forest floor" };
            dirt.vertices = new[] { new Vector3(-210,0,-210), new Vector3(-210,0,210), new Vector3(210,0,210), new Vector3(210,0,-210) };
            dirt.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            dirt.RecalculateNormals();
            ParkGeometry.Place("Forest floor", dirt, root, Vector3.up * Ground, Vector3.one, meadow);
            AddLawnBlades(root, meadow);

            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                var point = new Vector3(Mathf.Cos(angle)*44, -3, Mathf.Sin(angle)*44);
                ParkGeometry.Place("Wooded hillside", sphere, root, point, new Vector3(26+i%3*6,10+i%4*1.8f,30), hill);
            }

            ParkHouse house;
            if(ParkBakerySetup.Prefab)house=ParkBakerySetup.Create(root,Ground);
            else
            {
                house=Object.Instantiate(garden.Houses[0],root,false);
                house.name="Moss lodge";
                house.DisplayName="이끼 오두막";
                house.transform.localPosition=new Vector3(-2.4f,0,6.3f);
                var landing=house.Welcome.localPosition;
                landing.y=Ground;
                house.Welcome.localPosition=landing;
                StyleCabin(house,sphere);
            }

            MakePond(root, nature, sphere, cylinder, water, rim, pad, petal, pollen);
            PlantForest(nature);
            Debug.Log($"ULTIMATE NATURE: placed {nature.Count} instances from {nature.ModelsLoaded} prefabs.");
            return new ParkVenue
            {
                Name="숲", Root=root.gameObject, Houses=new[]{house},
                LawnHeight=Ground, WalkArea=Rect.MinMaxRect(-9.6f,-7.5f,9.6f,2.3f),
                PondCenter=PondCenter, PondRadii=PondSize+new Vector2(.48f,.48f),
                Focus=new Vector3(0,1.5f,1.1f), Distance=30.8f, Yaw=-6, Pitch=32,
                Sky=new Color32(196,227,214,255)
            };
        }

        static void AddLawnBlades(Transform root, Material lawn)
        {
            var points=new List<Vector3>();
            var triangles=new List<int>();
            var random=new System.Random(8761);
            for(int z=-19;z<=19;z++)
            for(int x=-24;x<=24;x++)
            {
                float px=x+(float)random.NextDouble()-.5f;
                float pz=z+(float)random.NextDouble()-.5f;
                var pond=new Vector2((px-PondCenter.x)/(PondSize.x*1.13f),(pz-PondCenter.y)/(PondSize.y*1.16f));
                bool nearCabin=Mathf.Abs(px-ParkBakerySetup.Location.x)<4.65f&&pz>2.4f&&pz<12f;
                if(pond.sqrMagnitude<1||nearCabin)continue;
                var p=new Vector3(px,Ground+.004f,pz);
                for(int i=0;i<3;i++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2;
                    var side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    float height=.105f+(float)random.NextDouble()*.125f;
                    var basePoint=p+side*(i-1)*.07f;
                    int v=points.Count;
                    points.Add(basePoint-side*.034f);
                    points.Add(basePoint+side*.034f);
                    points.Add(basePoint+side*.063f+Vector3.up*height);
                    triangles.AddRange(new[]{v,v+1,v+2,v,v+2,v+1});
                }
            }
            var mesh=new Mesh {name="Short meadow grass"};
            mesh.SetVertices(points);
            mesh.SetTriangles(triangles,0);
            var normals=new Vector3[points.Count];
            for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;
            mesh.normals=normals;
            var blades=new Material(lawn){name="Forest grass blades",color=new Color32(114,162,59,255)};
            var part=ParkGeometry.Place("Grass blades",mesh,root,Vector3.zero,Vector3.one,blades);
            part.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        static void StyleCabin(ParkHouse cabin, Mesh sphere)
        {
            var body = Paint("Cabin timber", "#C69A67");
            var roof = Paint("Woodland roof", "#568453");
            var ridge = Paint("Woodland roof ridge", "#6D9D61");
            var trim = Paint("Golden woodland trim", "#F2D5A0");
            var wood = Paint("Door and eaves", "#956438");
            var slate = Paint("Porch stone", "#A39983");
            foreach (Transform child in cabin.transform)
            {
                switch (child.name)
                {
                    case "Sunflower roof ornament": case "Flower pot": case "Stem":
                    case "Flower leaf": case "Petal": case "Flower centre":
                        child.gameObject.SetActive(false);
                        break;
                    case "Sculpted roof": case "Chimney": SetPaint(child,roof); break;
                    case "Left wall": case "Right wall": case "Rear wall": case "Doorway side wall":
                    case "Curved doorway lintel": SetPaint(child,body); break;
                    case "Foundation": case "Chimney cap": case "Corner trim": case "Window sill": SetPaint(child,trim); break;
                    case "Open arched frame": SetPaint(child,wood); break;
                }
            }
            var landing=cabin.transform.Find("Rounded themed porch/Sloping rounded landing");
            if(landing)
            {
                landing.GetComponent<MeshRenderer>().sharedMaterials=new[]{slate,wood};
                foreach(Transform detail in landing.parent)
                    if(detail!=landing)detail.gameObject.SetActive(false);
            }
            var roofDetails=new Pieces();
            roofDetails.Add(sphere,ridge,new Vector3(0,4.99f,0),new Vector3(.63f,.19f,5.02f));
            for(int side=-1;side<=1;side+=2)
            {
                roofDetails.Add(sphere,wood,new Vector3(side*2.82f,3.56f,0),new Vector3(.21f,.20f,4.98f));
                for(int i=0;i<4;i++)
                {
                    float x=.64f+i*.56f;
                    float y=4.95f-(x-.36f)*.51f;
                    roofDetails.Add(sphere,ridge,new Vector3(side*x,y,0),new Vector3(.10f,.092f,4.9f));
                }
            }
            roofDetails.Build(cabin.transform);
        }

        static void SetPaint(Transform part, Material material) => part.GetComponent<MeshRenderer>().sharedMaterial=material;

        static void MakePond(Transform root, NatureModels nature, Mesh sphere, Mesh cylinder,
            Material water, Material rim, Material pad, Material petal, Material pollen)
        {
            var point = new Vector3(PondCenter.x, Ground, PondCenter.y);
            ParkGeometry.Place("Pond bank", cylinder, root, point + Vector3.up*.009f,
                new Vector3(PondSize.x*2.2f,.018f,PondSize.y*2.27f),rim);
            var surface=ParkGeometry.Place("Still woodland water",cylinder,root,point+Vector3.up*.037f,
                new Vector3(PondSize.x*2,.013f,PondSize.y*2),water);
            surface.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            for (int i = 0; i < 19; i++)
            {
                float angle=i*2*Mathf.PI/19;
                var shore=point+new Vector3(Mathf.Cos(angle)*PondSize.x*1.04f,0,Mathf.Sin(angle)*PondSize.y*1.04f);
                if(i%3!=1)nature.Place("UNS_Tiny_Rock_0"+(1+i%3),shore,.24f+i%4*.085f,i*61);
                if(i%4==1)nature.Place("UNS_Grass",shore,.40f,i*37);
            }
            var lilies = new Pieces();
            for(int i=0;i<5;i++)
            {
                float angle=.45f+i*1.32f;
                var center=point+new Vector3(Mathf.Cos(angle)*2.13f,.059f,Mathf.Sin(angle)*1.42f);
                lilies.Add(sphere,pad,center,new Vector3(.57f+i%2*.18f,.043f,.49f),Quaternion.Euler(0,i*39,0));
                if(i%2==0)
                {
                    for(int j=0;j<5;j++)
                    {
                        float a=j*72;
                        var offset=Quaternion.Euler(0,a,0)*new Vector3(0,.062f,.10f);
                        lilies.Add(sphere,petal,center+offset,new Vector3(.145f,.085f,.23f),Quaternion.Euler(-18,a,0));
                    }
                    lilies.Add(sphere,pollen,center+Vector3.up*.088f,new Vector3(.14f,.09f,.14f));
                }
            }
            lilies.Build(root);
        }

        static void PlantForest(NatureModels nature)
        {
            var random = new System.Random(5309);
            for(int ring=0;ring<3;ring++)
            {
                int count=ring==0?31:ring==1?39:45;
                for(int i=0;i<count;i++)
                {
                    float angle=(i+(float)random.NextDouble()*.55f)*2*Mathf.PI/count;
                    float radius=15.2f+ring*7.7f+(float)random.NextDouble()*2.5f;
                    var p=new Vector3(Mathf.Cos(angle)*radius*1.10f,Ground,Mathf.Sin(angle)*radius*.86f+1.5f);
                    if(ring==0&&p.z< -6&&Mathf.Abs(p.x)<8)continue;
                    if(ring==1&&p.z< -13&&Mathf.Abs(p.x)<4)continue;
                    float height=ring==0?5.5f:7f;
                    nature.Place(i%3==0?"UNS_Spruce_02":"UNS_Spruce_01",p,height+(float)random.NextDouble()*3.5f,i*67+ring*89);
                    if(ring<2&&i%2==0)nature.Place("UNS_Bush",p+new Vector3(1.35f,0,-.80f),.67f+i%4*.22f,i*26);
                    if(ring<2&&i%4==0)nature.Place("UNS_Standard_Rock_0"+(1+i%3),p+new Vector3(-1,0,1.1f),.78f+i%5*.26f,i*31);
                }
            }
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<8;i++)
                {
                    var p=new Vector3(side*(9.7f+(i%3)*1.2f),Ground,-8.8f+i*2.4f);
                    nature.Place(i%2==0?"UNS_Mushroom_Patch":"UNS_Grass",p,.32f+i%3*.13f,i*53);
                    nature.Place(i%3==0?"UNS_Flower":"UNS_Grass",p+new Vector3(-side*.7f,0,1.05f),.33f+i%4*.08f,i*38);
                }
                nature.Place("UNS_Stump",new Vector3(side*10.7f,Ground,4.1f+side),.78f,side*26);
            }
            nature.Place("UNS_Standard_Rock_02",new Vector3(-7.1f,Ground,2.8f),.95f,30);
            nature.Place("UNS_Mushroom_Patch",new Vector3(-5.6f,Ground,2.9f),.48f,70);
            nature.Place("UNS_Grass",new Vector3(-3.1f,Ground,2.7f),.45f,10);
            for(int i=0;i<16;i++)
            {
                float x=-7.7f+i*1.07f;
                float z= -8.5f+.20f*x*x/2.3f;
                nature.Place(i%4==0?"UNS_Flower":i%3==0?"UNS_Mushroom_Patch":"UNS_Grass",
                    new Vector3(x,Ground,z-1.32f),.25f+(i%4)*.064f,i*57);
            }
        }

        internal sealed class NatureModels
        {
            readonly Transform root;
            readonly Dictionary<string,(GameObject Prefab,Bounds Bounds)> prefabs=new();
            readonly Dictionary<string,Material> materials=new();
            readonly Shader shader=Shader.Find("PeacePark/Forest foliage");
            public int Count {get;private set;}
            public int ModelsLoaded=>prefabs.Count;

            public NatureModels(Transform parent)
            {
                root=Group("Ultimate Nature Starter scenery",parent,Vector3.zero);
                foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Pack.TrimEnd('/')}))
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if(!prefab)continue;
                    var bounds=new Bounds(Vector3.zero,Vector3.zero);
                    foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                        bounds.Encapsulate(renderer.bounds);
                    if(bounds.size.y>.001f)prefabs[prefab.name]=(prefab,bounds);
                }
            }

            public void Place(string name, Vector3 point, float height, float angle)
            {
                if(!prefabs.TryGetValue(name,out var model))return;
                var item=Object.Instantiate(model.Prefab,root,false);
                item.name=name;
                var rotation=Quaternion.Euler(0,angle,0);
                float scale=height/model.Bounds.size.y;
                item.transform.localScale*=scale;
                item.transform.SetLocalPositionAndRotation(point-rotation*
                    new Vector3(model.Bounds.center.x,model.Bounds.min.y,model.Bounds.center.z)*scale,rotation);
                foreach(var renderer in item.GetComponentsInChildren<MeshRenderer>())
                {
                    var slots=renderer.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)slots[i]=CompatibleMaterial(slots[i]);
                    renderer.sharedMaterials=slots;
                }
                foreach(var lod in item.GetComponentsInChildren<LODGroup>())
                {
                    var levels=lod.GetLODs();
                    if(levels.Length==0)continue;
                    levels[levels.Length-1].screenRelativeTransitionHeight=.008f;
                    lod.SetLODs(levels);
                }
                foreach(var collider in item.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
                Count++;
            }

            Material CompatibleMaterial(Material original)
            {
                string name=original?original.name:"UNS_Color_Palette";
                if(materials.TryGetValue(name,out var material))return material;
                var (texture,tint,cutoff) = name switch
                {
                    "UNS_Spruce_Tree_Branch" => ("Trees/Fir/Textures/UNS_Spruce_Tree_Branch.png","#57824E",.42f),
                    "UNS_Bush_Leaves" => ("Vegetation/Bushes/Textures/UNS_Bush_Leaves.png","#739C50",.4f),
                    "UNS_Grass" => ("Vegetation/Grass/Textures/UNS_Grass.png","#709843",.30f),
                    "UNS_Flower_Leaf" => ("Vegetation/Flowers/Textures/UNS_Flower_Leaf.png","#84AA58",.35f),
                    "UNS_Flower" => ("Vegetation/Flowers/Textures/UNS_Flower.png","#FFF2C0",.35f),
                    _ => ("Shared/Palettes/Textures/UNS_Color_Palette.png","#FFFFFF",0f)
                };
                ColorUtility.TryParseHtmlString(tint,out var color);
                material=new Material(shader){name=name+" (Forest)",color=color};
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Pack+texture);
                material.SetFloat("_Cutoff",cutoff);
                material.SetInt("_Cull",cutoff>0?(int)CullMode.Off:(int)CullMode.Back);
                materials.Add(name,material);
                return material;
            }
        }
    }
}
