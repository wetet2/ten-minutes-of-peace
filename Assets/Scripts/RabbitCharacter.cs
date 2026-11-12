using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed class RabbitCharacter : MonoBehaviour
    {
        Material paintMaterial;
        Material[] materials;
        Texture2D frontTexture,backTexture;
        Transform model,body,head,leftArm,rightArm,leftLeg,rightLeg,leftShoulder,rightShoulder;
        Mesh mesh;
        SkinnedMeshRenderer skin;
        float sequenceTime;
        bool running;
        bool correctiveReady;
        sealed class Geometry
        {
            public Mesh Mesh;
            public int SkinVertexCount;
            public int[] SkinTriangles;
            public bool RuntimeOwned;
        }
        static readonly Dictionary<AnimalKind,Geometry> cache=new Dictionary<AnimalKind,Geometry>();
        static Shader configuredPaintShader;
        RabbitRigAsset rigAsset;
        bool ownsMesh;
        public AnimalKind Kind {get;private set;}=AnimalKind.Rabbit;
        public bool Grounded {get;set;}
        public bool ExternalMotion {get;set;}
        public bool IsRunning=>running;
        public int SkinVertexCount {get;private set;}
        public int[] SkinTriangles {get;private set;}
        public SkinnedMeshRenderer Skin=>skin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            foreach(var entry in cache.Values)if(entry.Mesh&&entry.RuntimeOwned)Destroy(entry.Mesh);
            cache.Clear();
            configuredPaintShader=null;
        }
        public static void ConfigurePaintShader(Shader shader){configuredPaintShader=shader;}
        public static void Prewarm()
        {
            if(cache.ContainsKey(AnimalKind.Rabbit))return;
            var temporary=new GameObject("Rabbit mesh warmup");
            var rabbit=temporary.AddComponent<RabbitCharacter>();
            rabbit.EnsureBuilt();
            rabbit.Skin.enabled=false;
            Destroy(temporary);
        }

        public void EnsureBuilt(AnimalKind kind=AnimalKind.Rabbit)
        {
            if(skin)return;
            Kind=kind;
            Build();
        }
        Material Solid(string name,Color color,float smoothness=.18f)
        {
            var shader=Shader.Find("PeacePark/Storybook");if(!shader)shader=Shader.Find("Standard");
            var material=new Material(shader){name=name,color=color};
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",smoothness);return material;
        }
        Transform Pivot(string name,Transform parent,Vector3 position)
        {var bone=new GameObject(name).transform;bone.SetParent(parent,false);bone.localPosition=position;return bone;}
        void Build()
        {
            rigAsset=Resources.Load<RabbitRigAsset>(AnimalKinds.RigResource(Kind));
            if(rigAsset&&rigAsset.GeometryVersion!=RabbitMesh.GeometryVersion(Kind))rigAsset=null;
            if(rigAsset&&!cache.ContainsKey(Kind))
            {
                cache[Kind]=new Geometry
                {
                    Mesh=rigAsset.Mesh,SkinVertexCount=rigAsset.SkinVertexCount,SkinTriangles=rigAsset.SkinTriangles
                };
            }
            paintMaterial=new Material(configuredPaintShader?configuredPaintShader:Shader.Find("PeacePark/RabbitPaint")){name="Child-painted rabbit skin"};
            paintMaterial.SetFloat("_Species",(int)Kind);
            frontTexture=WhiteTexture();backTexture=WhiteTexture();
            paintMaterial.SetTexture("_FrontTex",frontTexture);paintMaterial.SetTexture("_BackTex",backTexture);
            materials=new[]{paintMaterial,Solid("Warm dark eyes and smile",new Color32(54,40,38,255),.38f),Solid("Soft pink",new Color32(247,161,181,255)),Solid("Warm white muzzle",new Color32(255,248,237,255))};
            model=Pivot("Rabbit rig",transform,Vector3.zero);
            body=Pivot("Body",model,new Vector3(0,1.68f,0));head=Pivot("Head and neck",model,new Vector3(0,2.32f,0));
            // Clavicles pivot near the chest. Their outer ends carry the upper-arm joints,
            // so raising a clavicle elevates both the shoulder surface and the arm socket.
            leftShoulder=Pivot("Left clavicle",model,new Vector3(-.22f,2.08f,0));rightShoulder=Pivot("Right clavicle",model,new Vector3(.22f,2.08f,0));
            leftArm=Pivot("Left upper arm",leftShoulder,new Vector3(-.35f,RabbitMesh.ShoulderHeight-2.08f,0));rightArm=Pivot("Right upper arm",rightShoulder,new Vector3(.35f,RabbitMesh.ShoulderHeight-2.08f,0));
            leftLeg=Pivot("Left hip",model,new Vector3(-.32f,RabbitMesh.HipHeight,0));rightLeg=Pivot("Right hip",model,new Vector3(.32f,RabbitMesh.HipHeight,0));
            var bones=new[]{body,head,leftArm,rightArm,leftLeg,rightLeg,leftShoulder,rightShoulder};
            bool reuse=cache.TryGetValue(Kind,out var geometry);
            if(reuse){mesh=geometry.Mesh;SkinVertexCount=geometry.SkinVertexCount;SkinTriangles=geometry.SkinTriangles;}
            else
            {
                mesh=RabbitMesh.Build(Kind,out int count,out int[] triangles);SkinVertexCount=count;SkinTriangles=triangles;
                ownsMesh=!Application.isPlaying;
                var binds=new Matrix4x4[bones.Length];for(int i=0;i<binds.Length;i++)binds[i]=bones[i].worldToLocalMatrix*transform.localToWorldMatrix;
                mesh.bindposes=binds;
            }
            // A single mesh and renderer also contain the facial material regions.
            skin=gameObject.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=bones;skin.rootBone=model;
            skin.sharedMaterials=materials;skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;
            skin.localBounds=new Bounds(new Vector3(0,2.6f,0),new Vector3(4,6.4f,4));
            if(!reuse)
            {
                RabbitShoulderCorrective.Build(this);
                if(Application.isPlaying)cache[Kind]=new Geometry
                {
                    Mesh=mesh,SkinVertexCount=SkinVertexCount,SkinTriangles=SkinTriangles,RuntimeOwned=true
                };
            }
            correctiveReady=true;
        }
        Texture2D WhiteTexture()
        {
            var texture=new Texture2D(4,4,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[16];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;
            texture.SetPixels(colors);texture.Apply();return texture;
        }
        public void ApplyPainting(byte[] front,byte[] back,bool raw=false)
        {
            if(!skin)EnsureBuilt();
            if(frontTexture)Destroy(frontTexture);if(backTexture)Destroy(backTexture);
            frontTexture=raw?RabbitPaintTexture.FromRaw(front,"Rabbit front painting"):RabbitPaintTexture.Decode(front,"Rabbit front painting");
            backTexture=raw?RabbitPaintTexture.FromRaw(back,"Rabbit back painting"):RabbitPaintTexture.Decode(back,"Rabbit back painting");
            paintMaterial.SetTexture("_FrontTex",frontTexture);paintMaterial.SetTexture("_BackTex",backTexture);
            sequenceTime=0;running=true;transform.position=new Vector3(0,0,1.7f);transform.rotation=Quaternion.identity;
            IdlePose();skin.enabled=true;
        }
        public void ResetPreview()
        {running=false;sequenceTime=0;transform.position=Vector3.zero;transform.rotation=Quaternion.identity;IdlePose();skin.enabled=false;}
        // Deterministic poses are also used by the editor validation.
        public void PreviewPose(float time)
        {
            IdlePose();transform.position=new Vector3(0,0,-.2f);transform.rotation=Quaternion.identity;
            if(time<2.2f)
            {transform.position=Vector3.Lerp(new Vector3(0,0,1.7f),new Vector3(0,0,-.2f),Mathf.SmoothStep(0,1,time/2.2f));WalkPose(time*7);}
            else if(time>=2.75f&&time<4.4f)
            {
                PlayWavePose((time-2.75f)/1.65f);
            }
            else if(time>=4.4f)
            {float t=time-4.4f;transform.position=new Vector3(Mathf.Sin(t*.35f)*1.8f,0,-.2f+Mathf.Sin(t*.7f)*.28f);transform.rotation=Quaternion.Euler(0,Mathf.Sin(t*.35f)*18,0);WalkPose(t*6);}
        }
        void PlaceFeet(float floor)
        {if(Grounded)model.localPosition-=Vector3.up*floor;}
        public void PlayIdlePose(){IdlePose();PlaceFeet(rigAsset?rigAsset.IdleFloor:mesh.bounds.min.y);}
        public void PlayWalkingPose(float phase){IdlePose();WalkPose(phase);PlaceFeet(rigAsset?rigAsset.WalkingFloor(phase):mesh.bounds.min.y);}
        public void PlayWavePose(float progress)
        {
                IdlePose();float t=Mathf.Clamp01(progress);
                float release=Mathf.SmoothStep(0,1,Mathf.Clamp01((1-t)/.2f));
                float shoulderEase=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.15f))*release;
                float ease=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.035f)/.2f))*release;
                float lift=Mathf.Lerp(-40,50+Mathf.Sin(t*Mathf.PI*4)*6,ease);
                float clavicleLift=(20+Mathf.Sin(t*Mathf.PI*4)*1.5f)*shoulderEase;
                rightShoulder.localRotation=Quaternion.Euler(0,0,clavicleLift);
                rightShoulder.localPosition+=Vector3.up*(.04f*shoulderEase);
                rightArm.localRotation=Quaternion.Euler(0,0,lift-clavicleLift);head.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*2)*3);
                if(correctiveReady){skin.SetBlendShapeWeight(0,0);skin.SetBlendShapeWeight(2,1+t*99);}
                PlaceFeet(rigAsset?rigAsset.IdleFloor:mesh.bounds.min.y);
        }
        void Update(){if(running&&!ExternalMotion){sequenceTime+=Time.deltaTime;PreviewPose(sequenceTime);}}
        void WalkPose(float phase)
        {
            if(correctiveReady){skin.SetBlendShapeWeight(0,0);skin.SetBlendShapeWeight(1,1+Mathf.Repeat(phase,Mathf.PI*2)/(Mathf.PI*2)*99);}
            float swing=Mathf.Sin(phase)*15;
            leftArm.localRotation=Quaternion.Euler(swing,0,40);rightArm.localRotation=Quaternion.Euler(-swing,0,-40);
            leftLeg.localRotation=Quaternion.Euler(-swing,0,0);rightLeg.localRotation=Quaternion.Euler(swing,0,0);
            model.localPosition=new Vector3(0,Mathf.Abs(Mathf.Sin(phase))*.035f,0);
            body.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase)*1.5f);head.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(phase)*1.5f);
        }
        void IdlePose()
        {
            if(correctiveReady){skin.SetBlendShapeWeight(0,100);skin.SetBlendShapeWeight(1,0);skin.SetBlendShapeWeight(2,0);}
            leftLeg.localRotation=rightLeg.localRotation=body.localRotation=head.localRotation=Quaternion.identity;model.localPosition=Vector3.zero;
            leftShoulder.localPosition=new Vector3(-.22f,2.08f,0);rightShoulder.localPosition=new Vector3(.22f,2.08f,0);
            leftShoulder.localRotation=rightShoulder.localRotation=Quaternion.identity;
            leftArm.localRotation=Quaternion.Euler(0,0,40);rightArm.localRotation=Quaternion.Euler(0,0,-40);
        }
        void OnDestroy()
        {
            void Release(Object item){if(!item)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
            Release(frontTexture);Release(backTexture);
            if(ownsMesh)Release(mesh);
            if(materials!=null)foreach(var material in materials)Release(material);
        }
    }
}
