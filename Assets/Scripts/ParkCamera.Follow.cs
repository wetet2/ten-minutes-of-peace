using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PeacePark
{
    public sealed partial class ParkCamera
    {
        const float DefaultFollowDistance=10f;
        const float FollowFramingScale=1.4f;
        ParkPlayground park;
        Camera viewCamera;
        ParkRabbitAgent followTarget,tappedAnimal;
        ParkElephantTrain seatedTrain,tappedTrain;
        bool locomotiveView,tappedLocomotive;
        bool following,returning,pointerActive,pointerDragged;
        bool freeAim,savedFreeAim;
        int pointerId;
        Vector2 pointerStart;
        float pointerStartedAt,followDistance,zoomVelocity,yawVelocity,pitchVelocity;
        Vector3 focusVelocity,followOffset;
        Vector3 savedFocus;
        float savedDistance,savedYaw,savedPitch;
        readonly Dictionary<Mesh,Bounds> headBounds=new Dictionary<Mesh,Bounds>();
        internal ParkRabbitAgent FollowTarget=>seatedTrain?null:followTarget;
        internal bool IsReturning=>returning;
        float ZoomDistance=>following?followDistance:distance;

        internal void SetPark(ParkPlayground owner){park=owner;viewCamera=GetComponent<Camera>();}

        bool CanStartPointer(Vector2 position,int id)=>Screen.safeArea.Contains(position)&&BelowToolbar(position)&&
            (!EventSystem.current||!EventSystem.current.IsPointerOverGameObject(id));

        internal void BeginAnimalTap(Vector2 position,int id)
        {
            CancelAnimalTap();
            if(!InputEnabled||!CanStartPointer(position,id))return;
            pointerActive=true;pointerId=id;pointerStart=position;pointerStartedAt=Time.unscaledTime;
            if(seatedTrain||!park||!park.IsViewingPark||!viewCamera)return;
            var train=park.Train;
            var ray=viewCamera.ScreenPointToRay(position);
            float trainDepth=train&&train.isActiveAndEnabled?train.PickDepth(ray,out tappedLocomotive):float.PositiveInfinity;
            tappedAnimal=AnimalAt(position,trainDepth);
            if(!tappedAnimal&&!float.IsPositiveInfinity(trainDepth)&&
                !Physics.Raycast(ray,Mathf.Max(0,trainDepth-.03f),~0,QueryTriggerInteraction.Ignore))
                tappedTrain=train;
        }
        internal bool MoveAnimalTap(Vector2 position)
        {
            if(!pointerActive)return false;
            float tolerance=Mathf.Clamp(Screen.height*.012f,10,24);
            if((position-pointerStart).sqrMagnitude>tolerance*tolerance)
            {pointerDragged=true;tappedAnimal=null;tappedTrain=null;}
            return pointerDragged;
        }
        internal void EndAnimalTap(Vector2 position,int id)
        {
            MoveAnimalTap(position);
            var animal=tappedAnimal;
            var train=tappedTrain;
            bool locomotive=tappedLocomotive;
            bool tap=pointerActive&&!pointerDragged&&id==pointerId&&Time.unscaledTime-pointerStartedAt<=.45f&&
                CanStartPointer(position,id)&&InputEnabled;
            CancelAnimalTap();
            if(!tap)return;
            if(seatedTrain)LeaveTrainView();
            else if(animal)ToggleAnimalFollow(animal);
            else if(train&&train.isActiveAndEnabled&&park&&park.Train==train)
            {
                seatedTrain=train;
                locomotiveView=locomotive;
                Apply();
            }
        }
        internal void CancelAnimalTap(){pointerActive=false;pointerDragged=false;tappedAnimal=null;tappedTrain=null;tappedLocomotive=false;}

        internal void LeaveTrainView()
        {
            if(!seatedTrain)return;
            seatedTrain=null;
            Apply();
        }

        internal ParkRabbitAgent AnimalAt(Vector2 position,float maxDepth=float.PositiveInfinity)
        {
            if(!park||!viewCamera||!park.IsViewingPark)return null;
            var ray=viewCamera.ScreenPointToRay(position);
            ParkRabbitAgent nearest=null;float nearestDistance=maxDepth;
            foreach(var animal in park.Animals)
            {
                if(!park.CanFollowAnimal(animal))continue;
                // During arrivals the animated head and torso can shift away from the animal's root.
                var headPose=animal.transform.localToWorldMatrix;
                var bodyPose=headPose;
                if(animal.State==ParkRabbitState.WalkingOut)
                {
                    var skin=animal.Character.Skin;
                    var bones=skin.bones;
                    var bind=skin.sharedMesh.bindposes;
                    headPose=bones[1].localToWorldMatrix*bind[1];
                    bodyPose=bones[0].localToWorldMatrix*bind[0];
                }
                float head=HitDepth(ray,HeadBounds(animal),headPose);
                var body=new Bounds(new Vector3(0,1.15f,-.05f),new Vector3(2.3f,2.3f,1.6f));
                float depth=Mathf.Min(head,HitDepth(ray,body,bodyPose));
                if(depth<0||depth>=nearestDistance)continue;
                nearest=animal;nearestDistance=depth;
            }
            if(nearest&&Physics.Raycast(ray,out var obstacle,Mathf.Max(0,nearestDistance-.03f),~0,QueryTriggerInteraction.Ignore)&&
                !obstacle.transform.IsChildOf(nearest.transform))return null;
            return nearest;
        }
        internal static float HitDepth(Ray ray,Bounds bounds,Matrix4x4 pose)
        {
            var inverse=pose.inverse;
            var local=new Ray(inverse.MultiplyPoint3x4(ray.origin),inverse.MultiplyVector(ray.direction).normalized);
            if(!bounds.IntersectRay(local,out float distance))return float.PositiveInfinity;
            var hit=pose.MultiplyPoint3x4(local.GetPoint(distance));
            return Vector3.Dot(hit-ray.origin,ray.direction);
        }
        Bounds HeadBounds(ParkRabbitAgent animal)
        {
            var mesh=animal.Character.Skin.sharedMesh;
            if(headBounds.TryGetValue(mesh,out var bounds))return bounds;
            var vertices=mesh.vertices;var weights=mesh.boneWeights;bool first=true;
            for(int i=0;i<animal.Character.SkinVertexCount;i++)
            {
                var w=weights[i];
                float head=(w.boneIndex0==1?w.weight0:0)+(w.boneIndex1==1?w.weight1:0)+
                    (w.boneIndex2==1?w.weight2:0)+(w.boneIndex3==1?w.weight3:0);
                if(head<.6f)continue;
                if(first){bounds=new Bounds(vertices[i],Vector3.zero);first=false;}else bounds.Encapsulate(vertices[i]);
            }
            bounds.Expand(.10f);headBounds[mesh]=bounds;return bounds;
        }
        Vector3 AnimalFocus()=>followTarget.transform.position+
            Vector3.up*(followTarget.Character.Skin.sharedMesh.bounds.max.y*followTarget.transform.lossyScale.y*.55f)+followOffset;

        internal void ToggleAnimalFollow(ParkRabbitAgent animal)
        {
            if(!park||!park.CanFollowAnimal(animal))return;
            if(IsResetting)InterruptReturn();
            if(following&&followTarget==animal){ReleaseAnimalFollow();return;}
            if(!following&&!returning)
            {
                savedFocus=focus;savedDistance=distance;savedYaw=yaw;savedPitch=pitch;savedFreeAim=freeAim;
                if(!freeAim)focus=NormalFocus(focus,distance);
            }
            following=true;returning=false;followTarget=animal;followOffset=Vector3.zero;
            // Leave room around the animal, including taller animals in narrow windows.
            float height=animal.Character.Skin.sharedMesh.bounds.size.y*animal.transform.lossyScale.y;
            float halfAngle=viewCamera.fieldOfView*.5f*Mathf.Deg2Rad;
            float framing=height*.75f/(Mathf.Tan(halfAngle)*Mathf.Min(1,viewCamera.aspect));
            float framedDistance=Mathf.Max(DefaultFollowDistance,framing*FollowFramingScale);
            followDistance=Mathf.Clamp(Mathf.Min(savedDistance,framedDistance),MinimumDistance,maxDistance);
            focusVelocity=Vector3.zero;zoomVelocity=yawVelocity=pitchVelocity=0;
        }
        internal void ReleaseAnimalFollow()
        {
            if(!following)return;
            following=false;returning=true;followTarget=null;
            focusVelocity=Vector3.zero;zoomVelocity=yawVelocity=pitchVelocity=0;
        }
        internal void CancelAnimalFollow(bool restore)
        {
            resetElapsed=-1f;
            if(restore&&(following||returning))
            {focus=savedFocus;distance=savedDistance;yaw=savedYaw;pitch=savedPitch;freeAim=savedFreeAim;}
            else if(!restore)freeAim=false;
            following=returning=false;followTarget=null;followOffset=focusVelocity=Vector3.zero;
            zoomVelocity=yawVelocity=pitchVelocity=0;CancelAnimalTap();
        }
        void InterruptReturn()
        {
            if(!returning&&!IsResetting)return;
            // Keep the exact displayed aim if a manual gesture interrupts the return.
            resetElapsed=-1f;
            returning=false;freeAim=true;focusVelocity=Vector3.zero;zoomVelocity=yawVelocity=pitchVelocity=0;
        }
        void ZoomTo(float value)
        {
            if(seatedTrain)return;
            InterruptReturn();
            value=Mathf.Clamp(value,MinimumDistance,maxDistance);
            if(following)followDistance=value;else distance=value;
        }
        void LateUpdate()
        {
            if(seatedTrain)
            {
                if(seatedTrain.isActiveAndEnabled)Apply();
                else LeaveTrainView();
                return;
            }
            if(!InputEnabled&&!IsResetting)return;
            AdvanceAnimalFollow(Time.unscaledDeltaTime);
        }
        internal void AdvanceAnimalFollow(float dt)
        {
            if(following&&(!followTarget||!park.CanFollowAnimal(followTarget)))ReleaseAnimalFollow();
            if(dt<=0)return;
            if(IsResetting)
            {
                resetElapsed+=dt;
                if(resetElapsed>=ResetDuration)RestoreDefaultView();
                else Apply();
                return;
            }
            if(following)
            {
                focus=Vector3.SmoothDamp(focus,AnimalFocus(),ref focusVelocity,.32f,Mathf.Infinity,dt);
                distance=Mathf.SmoothDamp(distance,followDistance,ref zoomVelocity,.40f,Mathf.Infinity,dt);
            }
            else if(returning)
            {
                var target=savedFreeAim?savedFocus:NormalFocus(savedFocus,savedDistance);
                focus=Vector3.SmoothDamp(focus,target,ref focusVelocity,.42f,Mathf.Infinity,dt);
                distance=Mathf.SmoothDamp(distance,savedDistance,ref zoomVelocity,.45f,Mathf.Infinity,dt);
                yaw=Mathf.SmoothDampAngle(yaw,savedYaw,ref yawVelocity,.42f,Mathf.Infinity,dt);
                pitch=Mathf.SmoothDampAngle(pitch,savedPitch,ref pitchVelocity,.42f,Mathf.Infinity,dt);
                if((focus-target).sqrMagnitude<.000025f&&Mathf.Abs(distance-savedDistance)<.005f&&
                    Mathf.Abs(Mathf.DeltaAngle(yaw,savedYaw))<.03f&&Mathf.Abs(pitch-savedPitch)<.03f)
                    CancelAnimalFollow(true);
            }
            Apply();
        }
        void OnApplicationFocus(bool hasFocus){if(!hasFocus)ClearTouchGesture();}
    }
}
