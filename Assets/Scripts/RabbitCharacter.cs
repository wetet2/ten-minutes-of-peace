using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed partial class RabbitCharacter : MonoBehaviour
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
            public FootSole LeftSole, RightSole;
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
        static readonly int AnimalVisibilityId=Shader.PropertyToID("_AnimalVisibility");
        static readonly int AnimalHighlightId=Shader.PropertyToID("_AnimalHighlight");
        MaterialPropertyBlock skinProperties;
        bool fading;

        internal void SetVisibility(float visibility)
        {
            if(skateVisibility!=visibility){skateVisibility=visibility;ApplySkateVisibility();}
            bool needsFade=visibility<1;
            if(!needsFade&&!fading)return;
            if(needsFade!=fading)
            {
                foreach(var material in materials)
                {
                    if(needsFade)material.EnableKeyword("PEACE_PARK_ANIMAL_FADING");
                    else material.DisableKeyword("PEACE_PARK_ANIMAL_FADING");
                }
                fading=needsFade;
            }
            SetSkinProperty(AnimalVisibilityId,visibility);
        }

        internal void SetHoverHighlight(bool highlighted)
        {
            if(skin)SetSkinProperty(AnimalHighlightId,highlighted?1:0);
        }

        void SetSkinProperty(int property,float value)
        {
            skinProperties??=new MaterialPropertyBlock();
            skin.GetPropertyBlock(skinProperties);
            skinProperties.SetFloat(property,value);
            skin.SetPropertyBlock(skinProperties);
        }

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
            if(rigAsset&&rigAsset.GeometryVersion!=RabbitMesh.GeometryVersionFor(Kind))rigAsset=null;
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
            if(Kind is AnimalKind.Tiger or AnimalKind.Crocodile)
            {
                System.Array.Resize(ref materials,5);
                materials[4]=Kind==AnimalKind.Tiger
                    ?Solid("Tiger amber iris",new Color32(250,174,55,255),.36f)
                    :Solid("Crocodile golden iris",new Color32(229,192,68,255),.34f);
            }
            model=Pivot("Rabbit rig",transform,Vector3.zero);
            body=Pivot("Body",model,BodyRest);head=Pivot("Head and neck",model,new Vector3(0,2.32f,0));
            // Clavicles pivot near the chest. Their outer ends carry the upper-arm joints,
            // so raising a clavicle elevates both the shoulder surface and the arm socket.
            leftShoulder=Pivot("Left clavicle",model,new Vector3(-.22f,2.08f,0));rightShoulder=Pivot("Right clavicle",model,new Vector3(.22f,2.08f,0));
            leftArm=Pivot("Left upper arm",leftShoulder,new Vector3(-.35f,RabbitMesh.ShoulderHeight-2.08f,0));rightArm=Pivot("Right upper arm",rightShoulder,new Vector3(.35f,RabbitMesh.ShoulderHeight-2.08f,0));
            leftLeg=Pivot("Left hip",model,LeftHipRest);rightLeg=Pivot("Right hip",model,RightHipRest);
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
            gait=GaitFor(Kind);
            if(reuse&&geometry.LeftSole!=null){leftSole=geometry.LeftSole;rightSole=geometry.RightSole;}
            else PrepareSoles(mesh);
            if(reuse){geometry.LeftSole=leftSole;geometry.RightSole=rightSole;}
            if(!reuse)
            {
                RabbitShoulderCorrective.Build(this);
                if(Application.isPlaying)cache[Kind]=new Geometry
                {
                    Mesh=mesh,SkinVertexCount=SkinVertexCount,SkinTriangles=SkinTriangles,RuntimeOwned=true,LeftSole=leftSole,RightSole=rightSole
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
        internal void PlayRideQueuePose(float waited,int id)
        {
            PlayIdlePose();
            float cycle=Mathf.Repeat(waited+(id%7)*.39f,8.3f);
            float left=Glance(cycle,1.1f,1.4f);
            float right=Glance(cycle,4.35f,1.6f);
            float turn=left-right;
            head.localRotation=Quaternion.Euler(-3*(left+right),27*turn,-4*turn);

            static float Glance(float time,float start,float length)
            {
                float progress=Mathf.Clamp01((time-start)/length);
                return Mathf.SmoothStep(0,1,Mathf.Min(progress,1-progress)/.32f);
            }
        }
        public void PlayWalkingPose(float phase)=>PlayWalkingPose(phase,1);
        internal void PlayWalkingPose(float phase,float strength,float jog=0)
        {
            IdlePose();
            WalkPose(phase,strength,jog);
            PlaceFeet(rigAsset?rigAsset.IdleFloor:mesh.bounds.min.y);
        }
        public void PlayWavePose(float progress)
        {
            IdlePose();
            float t=Mathf.Clamp01(progress);
            float release=Mathf.SmoothStep(0,1,Mathf.Clamp01((1-t)/.2f));
            float shoulderEase=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.15f))*release;
            float ease=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.035f)/.2f))*release;
            float lift=Mathf.Lerp(-40,50+Mathf.Sin(t*Mathf.PI*4)*6,ease);
            float clavicleLift=(20+Mathf.Sin(t*Mathf.PI*4)*1.5f)*shoulderEase;
            rightShoulder.localRotation=Quaternion.Euler(0,0,clavicleLift);
            rightShoulder.localPosition+=Vector3.up*(.04f*shoulderEase);
            rightArm.localRotation=Quaternion.Euler(0,0,lift-clavicleLift);
            head.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*2)*3*shoulderEase);
            if(correctiveReady)
            {
                // The weight selects a baked pose by time, including the lowered arm at the end.
                skin.SetBlendShapeWeight(0,0);
                skin.SetBlendShapeWeight(2,1+t*99);
            }
            PlaceFeet(rigAsset?rigAsset.IdleFloor:mesh.bounds.min.y);
        }
        internal void PlayHopPose(float progress)
        {
            PlayIdlePose();
            float travel=Mathf.Clamp01((progress-.17f)/.64f);
            float airborne=4*travel*(1-travel);
            float crouch=progress<.17f?Mathf.Sin(progress/.17f*Mathf.PI):
                Mathf.Sin(Mathf.Clamp01((progress-.81f)/.19f)*Mathf.PI);
            model.localPosition+=Vector3.up*(airborne*.72f-crouch*.055f);
            body.localRotation=Quaternion.Euler(-8*airborne+6*crouch,0,0);
            leftLeg.localRotation=rightLeg.localRotation=Quaternion.Euler(-17*airborne,0,0);
            leftArm.localRotation=Quaternion.Euler(0,0,40+24*airborne);
            rightArm.localRotation=Quaternion.Euler(0,0,-40-24*airborne);
        }

        internal void PlayTrampolinePose(float lift,float compression)
        {
            PlayIdlePose();
            var spring=Vector3.down*(.15f*compression);
            body.localPosition+=spring;
            head.localPosition+=spring;
            leftShoulder.localPosition+=spring;
            rightShoulder.localPosition+=spring;
            body.localRotation=Quaternion.Euler(11*compression-6*lift,0,0);
            head.localRotation=Quaternion.Euler(-5*compression+3*lift,0,0);
            leftLeg.localRotation=rightLeg.localRotation=Quaternion.Euler(-13*lift,0,0);
            float armPitch=16*compression-23*lift;
            float armSpread=40+21*lift;
            leftArm.localRotation=Quaternion.Euler(armPitch,0,armSpread);
            rightArm.localRotation=Quaternion.Euler(armPitch,0,-armSpread);
        }

        internal void PlayDancePose(float progress)
        {
            PlayIdlePose();
            float envelope=Mathf.SmoothStep(0,1,Mathf.Min(progress,1-progress)/.16f);
            float sway=Mathf.Sin(progress*Mathf.PI*6)*envelope;
            body.localRotation=Quaternion.Euler(0,0,8*sway);
            head.localRotation=Quaternion.Euler(0,9*sway,-7*sway);
            leftArm.localRotation=Quaternion.Euler(15*sway,0,40+22*envelope+13*sway);
            rightArm.localRotation=Quaternion.Euler(-15*sway,0,-40-22*envelope+13*sway);
            leftLeg.localRotation=Quaternion.Euler(Mathf.Max(0,sway)*10,0,0);
            rightLeg.localRotation=Quaternion.Euler(Mathf.Max(0,-sway)*10,0,0);
        }

        internal void PlayBubblePose(float puff)
        {
            PlayIdlePose();
            body.localRotation=Quaternion.Euler(-5*puff,0,0);
            head.localRotation=Quaternion.Euler(9*puff,0,0);
            leftArm.localRotation=Quaternion.Euler(-15*puff,0,40-10*puff);
            rightArm.localRotation=Quaternion.Euler(-15*puff,0,-40+10*puff);
        }

        internal Vector3 BubbleMouth => head.TransformPoint(Kind switch
        {
            AnimalKind.Giraffe => new Vector3(0,1.37f,-1.24f),
            AnimalKind.Elephant => new Vector3(.27f,-.37f,-1.54f),
            AnimalKind.Triceratops => new Vector3(0,.31f,-1.48f),
            AnimalKind.Crocodile => new Vector3(0,.37f,-1.79f),
            AnimalKind.Hippo => new Vector3(0,.29f,-1.37f),
            AnimalKind.Gorilla => new Vector3(0,.40f,-1.07f),
            AnimalKind.Bear => new Vector3(0,.42f,-1.02f),
            AnimalKind.Tiger => new Vector3(0,.48f,-1.02f),
            AnimalKind.Frog => new Vector3(0,.35f,-.85f),
            _ => new Vector3(0,.37f,-.82f)
        });
        internal Vector3 BubbleDirection=>-head.forward;

        internal void PlaySlidePose(float seated)
        {
            PlayIdlePose();
            model.localPosition-=Vector3.up*(.84f*seated);
            body.localRotation=Quaternion.Euler(-10*seated,0,0);
            head.localRotation=Quaternion.Euler(7*seated,0,0);
            leftLeg.localRotation=rightLeg.localRotation=Quaternion.Euler(72*seated,0,0);
            leftArm.localRotation=Quaternion.Euler(15*seated,0,40-10*seated);
            rightArm.localRotation=Quaternion.Euler(15*seated,0,-40+10*seated);
        }
        internal void PlayRideStepPose(float progress, bool rightFoot)
        {
            PlayIdlePose();
            float step = Mathf.Sin(progress * Mathf.PI);
            var lead = rightFoot ? rightLeg : leftLeg;
            lead.localPosition += Vector3.up * (.07f * step);
            lead.localRotation = Quaternion.Euler(28 * step, 0, 0);
            body.localRotation = Quaternion.Euler(-10 * step, 0, 0);
            head.localRotation = Quaternion.Euler(6 * step, 0, 0);
            leftArm.localRotation = Quaternion.Euler(30 * step, 0, 40);
            rightArm.localRotation = Quaternion.Euler(34 * step, 0, -40);
        }

        internal void PlayRideSeatPose(float seated)
        {
            PlayIdlePose();
            model.localPosition -= Vector3.up * (.58f * seated);
            body.localRotation = Quaternion.Euler(-5 * seated, 0, 0);
            head.localRotation = Quaternion.Euler(4 * seated, 0, 0);
            leftLeg.localRotation = rightLeg.localRotation = Quaternion.Euler(60 * seated, 0, 0);
            leftArm.localRotation = Quaternion.Euler(30 * seated, -12 * seated, 40 - 16 * seated);
            rightArm.localRotation = Quaternion.Euler(30 * seated, 12 * seated, -40 + 16 * seated);
        }
        internal Vector3 PlayStraddleRidePose(float seated)
        {
            PlayIdlePose();
            body.localRotation=Quaternion.Euler(-4*seated,0,0);
            head.localRotation=Quaternion.Euler(3*seated,0,0);
            leftLeg.localRotation=Quaternion.Euler(32*seated,0,-22*seated);
            rightLeg.localRotation=Quaternion.Euler(32*seated,0,22*seated);
            leftArm.localRotation=Quaternion.Euler(63*seated,20*seated,40-24*seated);
            rightArm.localRotation=Quaternion.Euler(63*seated,-20*seated,-40+24*seated);
            return (leftLeg.position+rightLeg.position)*.5f-transform.position;
        }
        internal void GripRideHandle(Vector3 handle,float grip)
        {
            AimHand(leftArm,-1,handle-transform.right*.14f,grip);
            AimHand(rightArm,1,handle+transform.right*.14f,grip);
        }
        internal void PlayRoundaboutPose(Vector3 handle,float grip,float sway)
        {
            PlayIdlePose();
            body.localRotation=Quaternion.Euler(6*grip,0,4*sway);
            head.localRotation=Quaternion.Euler(2*grip,8*sway,-4*sway);
            leftLeg.localRotation=Quaternion.Euler(0,0,-5*grip);
            rightLeg.localRotation=Quaternion.Euler(0,0,5*grip);
            AimHand(leftArm,-1,handle-transform.right*.18f,grip);
            AimHand(rightArm,1,handle+transform.right*.18f,grip);
        }
        internal float ZiplineGripHeight => (RabbitMesh.ShoulderHeight+.74f)*transform.lossyScale.y;

        internal Vector3 GripSandSledRope(float grip)
        {
            var reach = transform.TransformDirection(new Vector3(-.30f, -.31f, .78f).normalized);
            var hand = leftArm.position + reach * (.83f * transform.lossyScale.x);
            AimHand(leftArm, -1, hand, grip);
            return hand;
        }

        internal void PlayZiplinePose(Vector3 handle,float grip,float swing)
        {
            PlayIdlePose();
            leftLeg.localRotation=Quaternion.Euler(26*grip+7*swing,0,-9*grip);
            rightLeg.localRotation=Quaternion.Euler(36*grip+7*swing,0,9*grip);
            head.localRotation=Quaternion.Euler(-8*grip,0,3*swing);
            float handSpacing=.47f*transform.lossyScale.x;
            AimHand(leftArm,-1,handle-transform.right*handSpacing,grip);
            AimHand(rightArm,1,handle+transform.right*handSpacing,grip);
        }
        internal void PlayPolePose(Vector3 pole,float grip)
        {
            PlayIdlePose();
            head.localRotation=Quaternion.Euler(22*grip,40*grip,-5*grip);
            body.localRotation=Quaternion.Euler(6*grip,0,0);
            leftLeg.localRotation=Quaternion.Euler(28*grip,-10*grip,-7*grip);
            rightLeg.localRotation=Quaternion.Euler(39*grip,14*grip,8*grip);
            AimHandAtPole(leftArm,-1,pole,.14f,grip);
            AimHandAtPole(rightArm,1,pole,.035f,grip);
            if(correctiveReady)skin.SetBlendShapeWeight(0,100*(1-grip));
        }

        internal void PlayLadderPose(Vector3 leftGrip,Vector3 rightGrip,float stride,float strength)
        {
            PlayIdlePose();
            float leftStep=Mathf.Max(0,stride)*strength,rightStep=Mathf.Max(0,-stride)*strength;
            leftLeg.localPosition+=Vector3.up*(leftStep*.25f);
            rightLeg.localPosition+=Vector3.up*(rightStep*.25f);
            leftLeg.localRotation=Quaternion.Euler(32*leftStep,0,-8*strength);
            rightLeg.localRotation=Quaternion.Euler(32*rightStep,0,8*strength);
            body.localRotation=Quaternion.Euler(-8*strength,0,0);
            head.localRotation=Quaternion.Euler(10*strength,0,-6*stride*strength);
            AimHand(leftArm,-1,leftGrip,strength);
            AimHand(rightArm,1,rightGrip,strength);
        }

        internal Vector3 TubeWaist => Vector3.Lerp((leftLeg.position+rightLeg.position)*.5f,body.position,.46f);
        internal Vector3 SwimChest => Vector3.Lerp(body.position,head.position,.4f);

        internal void PlayTubeFloatPose(float immersed,float seconds)
        {
            PlayIdlePose();
            float kick=Mathf.Sin(seconds*5.5f)*immersed;
            leftLeg.localRotation=Quaternion.Euler(immersed*5+12*kick,0,-4*immersed);
            rightLeg.localRotation=Quaternion.Euler(immersed*5-12*kick,0,4*immersed);
            head.localRotation=Quaternion.Euler(-3*immersed,7*Mathf.Sin(seconds*1.1f)*immersed,0);
        }

        internal void HoldTube(Vector3 leftHandle,Vector3 rightHandle,float grip)
        {
            AimHand(leftArm,-1,leftHandle,grip);
            AimHand(rightArm,1,rightHandle,grip);
        }

        internal void PlaySwimmingPose(float strength,float seconds)
        {
            PlayIdlePose();
            float phase=seconds*6.8f;
            float kick=Mathf.Sin(phase*1.75f);
            float pull=Mathf.Sin(phase);
            float reach=Mathf.Cos(phase);
            model.localRotation=Quaternion.Euler(-76*strength,0,5*pull*strength);
            head.localRotation=Quaternion.Euler(61*strength,8*pull*strength,-4*pull*strength);
            leftLeg.localRotation=Quaternion.Euler(12*kick*strength,0,-7*strength);
            rightLeg.localRotation=Quaternion.Euler(-12*kick*strength,0,7*strength);
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-15+26*reach,-37-26*pull,3+23*reach),strength);
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-15-26*reach,37-26*pull,-3+23*reach),strength);
        }

        internal void PlaySunbedPose(float recline,float seconds,Vector3 offset)
        {
            PlayIdlePose();
            model.localRotation=Quaternion.Euler(90*recline,0,0);
            model.localPosition+=offset*recline;
            body.localPosition+=Vector3.back*(.018f*(1-Mathf.Cos(seconds*2.8f))*recline);
            head.localRotation=Quaternion.Euler(-6*recline,0,0);
            leftArm.localRotation=Quaternion.Euler(8*recline,0,40+31*recline);
            rightArm.localRotation=Quaternion.Euler(8*recline,0,-40-31*recline);
            leftLeg.localRotation=Quaternion.Euler(8*recline,0,4*recline);
            rightLeg.localRotation=Quaternion.Euler(8*recline,0,-4*recline);
        }

        internal Vector3 MeasureSunbedPose(Mesh sample)
        {
            var scale=transform.localScale;
            transform.localScale=Vector3.one;
            PlaySunbedPose(1,0,Vector3.zero);
            skin.BakeMesh(sample,false);
            sample.RecalculateBounds();
            var bounds=sample.bounds;
            transform.localScale=scale;
            PlayIdlePose();
            // Fit the back of the posed mesh to the cushion for each species.
            return new Vector3(-bounds.center.x,.025f-bounds.min.y,-bounds.center.z);
        }

        void AimHandAtPole(Transform arm,int side,Vector3 pole,float rise,float grip)
        {
            var target=pole+transform.right*(side*.062f);
            target.y=arm.position.y+rise;
            AimHand(arm,side,target,grip);
        }

        void AimHand(Transform arm,int side,Vector3 target,float grip)
        {
            var restDirection=new Vector3(side*.78f,-.32f,-.02f);
            var direction=arm.parent.InverseTransformPoint(target)-arm.localPosition;
            var reach=Quaternion.FromToRotation(restDirection,direction);
            arm.localRotation=Quaternion.Slerp(arm.localRotation,reach,grip);
        }
        void Update(){if(running&&!ExternalMotion){sequenceTime+=Time.deltaTime;PreviewPose(sequenceTime);}}
        void IdlePose()
        {
            if(campCrouched){model.localScale=Vector3.one;campCrouched=false;}
            if(correctiveReady){skin.SetBlendShapeWeight(0,100);skin.SetBlendShapeWeight(1,0);skin.SetBlendShapeWeight(2,0);}
            leftLeg.localRotation=rightLeg.localRotation=body.localRotation=head.localRotation=Quaternion.identity;
            model.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            body.localPosition=BodyRest;
            leftLeg.localPosition=LeftHipRest;
            rightLeg.localPosition=RightHipRest;
            head.localPosition=new Vector3(0,2.32f,0);
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
