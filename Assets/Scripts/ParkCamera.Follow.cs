using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PeacePark
{
    public sealed partial class ParkCamera
    {
        const float DefaultFollowDistance=15f;
        const float FollowFramingScale=2.1f;
        const float AutoFollowDistanceScale=1.5f;
        const float FollowAimTime=.32f,FollowAngleTime=.42f;
        const float AutoHandoffTimeScale=2f,AutoHandoffDuration=FollowAngleTime*AutoHandoffTimeScale*3f;
        const float AutoSelectionHoldSeconds=4f;
        const float SledLaunchRightAngle=15f,SledLaunchPitch=45f,SledLaunchEasing=.075f;
        ParkPlayground park;
        Camera viewCamera;
        ParkRabbitAgent followTarget,tappedAnimal,hoveredAnimal;
        ParkElephantTrain seatedTrain,tappedTrain;
        bool locomotiveView,tappedLocomotive;
        bool following,returning,pointerActive,pointerDragged;
        bool freeAim,savedFreeAim;
        bool sledLaunchFramed,aimAutoFollowToVenue;
        int pointerId;
        Vector2 pointerStart;
        float pointerStartedAt,followDistance,zoomVelocity,yawVelocity,pitchVelocity,autoHandoffRemaining;
        float autoSelectionRemaining;
        float? wheelDistance;
        Vector3 focusVelocity,followOffset;
        Vector3 savedFocus;
        float savedDistance,savedYaw,savedPitch;
        readonly Dictionary<Mesh,Bounds> headBounds=new Dictionary<Mesh,Bounds>();
        readonly System.Random autoFollowRandom=new System.Random();
        internal ParkRabbitAgent FollowTarget=>seatedTrain?null:followTarget;
        internal ParkRabbitAgent ManualFollowTarget=>AutoFollowEnabled?null:FollowTarget;
        internal bool AutoFollowEnabled {get;private set;}
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
                if(AutoFollowEnabled)
                {
                    StopAutoFollow();
                    CancelAnimalFollow(true);
                }
                StopWheelZoom();
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
                if(animal.State==ParkRabbitState.WalkingOut||animal.ActiveAction!=ParkAnimalAction.None)
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
        Vector3 AnimalFocus()
        {
            if((park.ResortSwim&&park.ResortSwim.IsSwimming(followTarget))||
                (park.ResortSlide&&park.ResortSlide.IsSwimming(followTarget)))
                return followTarget.Character.SwimChest+Vector3.up*.36f+followOffset;
            return RaisedAnimalFocus(followTarget.transform.position);
        }
        Vector3 RaisedAnimalFocus(Vector3 point) =>
            point+Vector3.up*(followTarget.Character.Skin.sharedMesh.bounds.max.y*
                followTarget.transform.lossyScale.y*.55f)+followOffset;

        void AdvanceFollowAim(float dt,bool autoHandoff)
        {
            var sled=park.SandSled;
            if(sled&&followTarget.IsHiddenForRide)
            {
                aimAutoFollowToVenue=false;
                var launch=sled.LaunchViewAnchor();
                focus=Vector3.SmoothDamp(focus,RaisedAnimalFocus(launch.position),ref focusVelocity,
                    autoHandoff?FollowAimTime:SledLaunchEasing,Mathf.Infinity,dt);
                DampFollowAngles(launch.rotation.eulerAngles.y-SledLaunchRightAngle,SledLaunchPitch,
                    autoHandoff?FollowAngleTime:SledLaunchEasing,dt);
                return;
            }
            if(sled&&sled.IsRevealing(followTarget))
            {
                if(!sledLaunchFramed||autoHandoff)
                {
                    aimAutoFollowToVenue=false;
                    sledLaunchFramed=true;
                    // The sled moves forward, so a negative orbit yaw puts the camera on its rear-right.
                    float launchYaw=sled.LaunchViewAnchor().rotation.eulerAngles.y-SledLaunchRightAngle;
                    if(autoHandoff)
                    {
                        focus=Vector3.SmoothDamp(focus,AnimalFocus(),ref focusVelocity,FollowAimTime,Mathf.Infinity,dt);
                        DampFollowAngles(launchYaw,SledLaunchPitch,FollowAngleTime,dt);
                        return;
                    }
                    focus=AnimalFocus();
                    yaw=launchYaw;
                    pitch=SledLaunchPitch;
                    focusVelocity=Vector3.zero;
                    yawVelocity=pitchVelocity=0;
                    return;
                }
            }
            else sledLaunchFramed=false;
            if(aimAutoFollowToVenue)
            {
                DampFollowAngles(initialYaw,initialPitch,FollowAngleTime,dt);
                if(Mathf.Abs(Mathf.DeltaAngle(yaw,initialYaw))<.03f&&Mathf.Abs(pitch-initialPitch)<.03f)
                    aimAutoFollowToVenue=false;
            }
            focus=Vector3.SmoothDamp(focus,AnimalFocus(),ref focusVelocity,FollowAimTime,Mathf.Infinity,dt);
        }

        void DampFollowAngles(float targetYaw,float targetPitch,float smoothTime,float dt)
        {
            yaw=Mathf.SmoothDampAngle(yaw,targetYaw,ref yawVelocity,smoothTime,Mathf.Infinity,dt);
            pitch=Mathf.SmoothDampAngle(pitch,targetPitch,ref pitchVelocity,smoothTime,Mathf.Infinity,dt);
        }

        internal void UpdateAnimalHover(Vector2 position,bool pointerAvailable)
        {
            ParkRabbitAgent animal=null;
            if(pointerAvailable&&InputEnabled&&!seatedTrain&&park&&park.IsViewingPark&&viewCamera&&CanStartPointer(position,-1))
            {
                var train=park.Train;
                float depth=train&&train.isActiveAndEnabled?train.PickDepth(viewCamera.ScreenPointToRay(position),out _):float.PositiveInfinity;
                animal=AnimalAt(position,depth);
            }
            SetHoveredAnimal(animal);
        }

        void SetHoveredAnimal(ParkRabbitAgent animal)
        {
            if(hoveredAnimal==animal)return;
            if(hoveredAnimal&&hoveredAnimal.Character)hoveredAnimal.Character.SetHoverHighlight(false);
            hoveredAnimal=animal;
            if(hoveredAnimal)hoveredAnimal.Character.SetHoverHighlight(true);
        }

        internal void ToggleAnimalFollow(ParkRabbitAgent animal)
        {
            if(!park||!park.CanFollowAnimal(animal))return;
            if(IsResetting)InterruptReturn();
            if(AutoFollowEnabled)
            {
                if(following&&followTarget==animal)return;
                BeginAnimalFollow(animal,following);
                // Let the camera settle on a selected animal even when it has no current activity.
                if(!animal.IsUsingRide)autoSelectionRemaining=AutoSelectionHoldSeconds;
                return;
            }
            if(following&&followTarget==animal){ReleaseAnimalFollow();return;}
            BeginAnimalFollow(animal);
        }

        internal void ToggleAutoFollow()
        {
            if(AutoFollowEnabled)
            {
                StopAutoFollow();
                return;
            }
            if(!park||!park.IsViewingPark||!InputEnabled)return;
            LeaveTrainView();
            if(IsResetting)InterruptReturn();
            StopWheelZoom();
            AutoFollowEnabled=true;
            var candidate=PickAutoFollowTarget();
            if(candidate)
            {
                BeginAnimalFollow(candidate);
                return;
            }
            if(!following&&!returning&&!freeAim)focus=NormalFocus(focus,distance);
            SaveVenueDefaultView();
            BeginFollowReturn();
        }

        internal void StopAutoFollow()
        {
            if(!AutoFollowEnabled)return;
            AutoFollowEnabled=false;
            ReleaseAnimalFollow();
        }

        void UpdateAutoFollowTarget(float dt)
        {
            if(!AutoFollowEnabled||!InputEnabled||seatedTrain||!park||!park.IsViewingPark)return;
            if(following&&followTarget&&park.CanKeepFollowingAnimal(followTarget))
            {
                if(followTarget.IsUsingRide)
                {
                    autoSelectionRemaining=0;
                    return;
                }
                autoSelectionRemaining=Mathf.Max(0,autoSelectionRemaining-dt);
                if(autoSelectionRemaining>0)return;
            }
            Vector3? previousPosition=following?(followTarget?followTarget.transform.position:focus):null;
            var candidate=PickAutoFollowTarget(previousPosition);
            if(candidate)BeginAnimalFollow(candidate,following);
            else ReleaseAnimalFollow();
        }

        ParkRabbitAgent PickAutoFollowTarget(Vector3? previousPosition=null)
        {
            ParkRabbitAgent selected=null;
            float nearestDistance=float.PositiveInfinity;
            int count=0;
            foreach(var animal in park.Animals)
            {
                if(!animal||!animal.IsUsingRide||!park.CanFollowAnimal(animal))continue;
                if(!previousPosition.HasValue)
                {
                    if(autoFollowRandom.Next(++count)==0)selected=animal;
                    continue;
                }
                float distance=(animal.transform.position-previousPosition.Value).sqrMagnitude;
                if(distance<nearestDistance)
                {
                    nearestDistance=distance;
                    selected=animal;
                }
            }
            return selected;
        }

        void SaveVenueDefaultView()
        {
            savedFocus=initialFocus;savedDistance=initialDistance;savedYaw=initialYaw;savedPitch=initialPitch;
            savedFreeAim=false;
        }

        void BeginAnimalFollow(ParkRabbitAgent animal,bool autoHandoff=false)
        {
            StopWheelZoom();
            // Use the displayed orbit for a hand-selected animal and the venue overview for automatic follow.
            if(AutoFollowEnabled)SaveVenueDefaultView();
            if(!following&&!returning)
            {
                if(!AutoFollowEnabled)
                {savedFocus=focus;savedDistance=distance;savedYaw=yaw;savedPitch=pitch;savedFreeAim=freeAim;}
                if(!freeAim)focus=NormalFocus(focus,distance);
            }
            following=true;returning=false;followTarget=animal;followOffset=Vector3.zero;sledLaunchFramed=false;
            aimAutoFollowToVenue=AutoFollowEnabled;
            autoHandoffRemaining=autoHandoff?AutoHandoffDuration:0;
            autoSelectionRemaining=0;
            // Leave room around the animal, including taller animals in narrow windows.
            float height=animal.Character.Skin.sharedMesh.bounds.size.y*animal.transform.lossyScale.y;
            float halfAngle=viewCamera.fieldOfView*.5f*Mathf.Deg2Rad;
            float framing=height*.75f/(Mathf.Tan(halfAngle)*Mathf.Min(1,viewCamera.aspect));
            float framedDistance=Mathf.Max(DefaultFollowDistance,framing*FollowFramingScale);
            float manualDistance=Mathf.Min(savedDistance,framedDistance);
            followDistance=Mathf.Clamp(manualDistance*(AutoFollowEnabled?AutoFollowDistanceScale:1),MinimumDistance,maxDistance);
            focusVelocity=Vector3.zero;zoomVelocity=yawVelocity=pitchVelocity=0;
        }
        internal void ReleaseAnimalFollow()
        {
            if(following)BeginFollowReturn();
        }
        void BeginFollowReturn()
        {
            following=false;returning=true;followTarget=null;
            aimAutoFollowToVenue=false;
            autoHandoffRemaining=autoSelectionRemaining=0;
            focusVelocity=Vector3.zero;zoomVelocity=yawVelocity=pitchVelocity=0;
        }
        internal void CancelAnimalFollow(bool restore)
        {
            SetHoveredAnimal(null);
            resetElapsed=-1f;
            StopWheelZoom();
            if(restore&&(following||returning))
            {focus=savedFocus;distance=savedDistance;yaw=savedYaw;pitch=savedPitch;freeAim=savedFreeAim;}
            else if(!restore)freeAim=false;
            following=returning=sledLaunchFramed=aimAutoFollowToVenue=false;followTarget=null;followOffset=focusVelocity=Vector3.zero;
            autoHandoffRemaining=autoSelectionRemaining=0;
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
            StopWheelZoom();
            value=Mathf.Clamp(value,MinimumDistance,maxDistance);
            if(following)followDistance=value;else distance=value;
        }
        internal void ApplyWheelZoom(float scroll)
        {
            if(!InputEnabled||seatedTrain||scroll==0)return;
            InterruptReturn();
            float current=following?followDistance:(wheelDistance??distance);
            float target=Mathf.Clamp(current*Mathf.Exp(-scroll*.1f),MinimumDistance,maxDistance);
            if(following)followDistance=target;
            else if(target==distance)StopWheelZoom();
            else wheelDistance=target;
        }
        void StopWheelZoom()
        {
            if(!wheelDistance.HasValue)return;
            wheelDistance=null;
            zoomVelocity=0;
        }
        void LateUpdate()
        {
            if(seatedTrain)
            {
                if(seatedTrain.isActiveAndEnabled)Apply();
                else LeaveTrainView();
            }
            else if(InputEnabled||IsResetting)AdvanceAnimalFollow(Time.unscaledDeltaTime);
            UpdateAnimalHover(Input.mousePosition,Application.isFocused&&!Application.isMobilePlatform&&Input.mousePresent&&
                Input.touchCount==0&&!waitForTouchRelease&&GUIUtility.hotControl==0&&
                !Input.GetMouseButton(0)&&!Input.GetMouseButton(1)&&!Input.GetMouseButton(2));
        }
        internal void AdvanceAnimalFollow(float dt)
        {
            if(dt<=0)return;
            if(IsResetting)
            {
                resetElapsed+=dt;
                if(resetElapsed>=ResetDuration)RestoreDefaultView();
                else Apply();
                return;
            }
            UpdateAutoFollowTarget(dt);
            if(following&&(!followTarget||!park.CanKeepFollowingAnimal(followTarget)))ReleaseAnimalFollow();
            if(following)
            {
                // Slow the handoff until the camera has nearly settled, then track at the usual speed.
                float handoffStep=Mathf.Min(dt,autoHandoffRemaining);
                autoHandoffRemaining-=handoffStep;
                float followStep=dt-handoffStep+handoffStep/AutoHandoffTimeScale;
                AdvanceFollowAim(followStep,handoffStep>0);
                distance=Mathf.SmoothDamp(distance,followDistance,ref zoomVelocity,.40f,Mathf.Infinity,followStep);
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
            else if(wheelDistance.HasValue)
            {
                float target=wheelDistance.Value;
                distance=Mathf.Clamp(Mathf.SmoothDamp(distance,target,ref zoomVelocity,.16f,Mathf.Infinity,dt),
                    MinimumDistance,maxDistance);
                if(Mathf.Abs(distance-target)<.003f)
                {
                    distance=target;
                    StopWheelZoom();
                }
            }
            Apply();
        }
        void OnDisable()=>SetHoveredAnimal(null);
        void OnApplicationFocus(bool hasFocus)
        {
            if(hasFocus)return;
            ClearTouchGesture();
            mousePanning=false;
            SetHoveredAnimal(null);
        }
    }
}
