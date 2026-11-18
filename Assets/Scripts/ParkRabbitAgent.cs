using UnityEngine;

namespace PeacePark
{
    public enum ParkRabbitState { OpeningDoor,WalkingOut,Waving,Wandering }

    public sealed class ParkRabbitAgent : MonoBehaviour
    {
        public int Id {get;private set;}
        public int HouseIndex {get;private set;}
        public ParkRabbitState State {get;private set;}
        public int WaveCount {get;private set;}
        public int VisitedTargets {get;private set;}
        public RabbitCharacter Character {get;private set;}
        public float WalkedDistance {get;private set;}
        public bool ExitedThroughOpenDoor {get;private set;}=true;
        ParkPlayground park;
        ParkHouse house;
        System.Random random;
        float elapsed,phase,pause,blocked,wanderSpeed;
        Vector3 destination;
        bool leavingEntrance;
        internal enum Activity { None, Hop, Dance, ApproachSlide, Climb, Sit, Slide, LeaveSlide }
        internal Activity CurrentActivity {get;private set;}
        ParkSlide slide;
        internal bool UseContactShadow {get;set;}
        MeshRenderer contactShadow;
        MaterialPropertyBlock shadowProperties;
        static Material contactShadowMaterial;
        static Mesh contactShadowMesh;
        static readonly int ShadowOpacity=Shader.PropertyToID("_Opacity");
        float activityTime,activityDelay;
        const float WaveDuration=1.65f;
        internal const float WanderSpeed=.70f;
        internal const float AnimalSpacing=.82f;
        static readonly float[] DepartureAngles={-84,-56,-28,0,28,56,84};
        static readonly float[] AvoidanceAngles={30,-30,60,-60,90,-90,120,-120,150,-150,180};
        internal const float SizeMultiplier=1.3f;

        public void Initialize(ParkPlayground owner,ParkHouse source,int houseIndex,int id,byte[] front,byte[] back,bool raw=false,AnimalKind kind=AnimalKind.Rabbit)
        {
            park=owner;house=source;HouseIndex=houseIndex;Id=id;random=new System.Random(Random.Range(0,int.MaxValue));
            Character=gameObject.AddComponent<RabbitCharacter>();
            Character.EnsureBuilt(kind);
            Character.ExternalMotion=true;Character.Grounded=true;Character.ApplyPainting(front,back,raw);
            float previousScale=kind==AnimalKind.Giraffe?.40f:kind==AnimalKind.Elephant?.39f:.42f;
            transform.SetPositionAndRotation(house.Inside.position,house.Facing);
            transform.localScale=Vector3.one*(previousScale*SizeMultiplier);
            Character.PlayIdlePose();Character.Skin.enabled=false;State=ParkRabbitState.OpeningDoor;
            activityDelay=Range(8,17);
            UseContactShadow=Application.platform==RuntimePlatform.Android;
        }

        void CreateContactShadow()
        {
            if(!contactShadowMaterial)
            {
                var shader=Resources.Load<Shader>("AnimalContactShadow");
                if(!shader||!shader.isSupported)return;
                contactShadowMaterial=new Material(shader);
            }
            if(!contactShadowMesh)
            {
                contactShadowMesh=new Mesh{name="Animal contact shadow quad"};
                contactShadowMesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),
                    new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};
                contactShadowMesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
                contactShadowMesh.triangles=new[]{0,1,2,0,2,3};
            }
            var shadow=new GameObject("Animal contact shadow"){layer=gameObject.layer};
            try
            {
                shadow.transform.SetParent(transform,false);
                shadow.transform.localScale=new Vector3(3.05f,2.25f,1);
                shadow.AddComponent<MeshFilter>().sharedMesh=contactShadowMesh;
                var renderer=shadow.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=contactShadowMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                Character.Skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                contactShadow=renderer;
            }
            catch
            {
                Destroy(shadow);
                throw;
            }
        }

        void LateUpdate()
        {
            // Shadow setup is optional and runs after the animal has started its entrance.
            if(UseContactShadow&&Character&&Character.Skin.enabled)
            {
                UseContactShadow=false;
                try{CreateContactShadow();}
                catch(System.Exception error){Debug.LogWarning($"Could not create contact shadow for animal {Id}: {error}");}
            }
            if(contactShadow)UpdateContactShadow();
        }

        void UpdateContactShadow()
        {
            contactShadow.enabled=Character.Skin.enabled&&CurrentActivity<Activity.Climb&&
                !(State==ParkRabbitState.WalkingOut&&
                    ((house.EntranceSlide&&elapsed<house.ExitDuration-ParkSlide.ExitWalkSeconds)||
                    (house.EntrancePole&&transform.position.y>house.EntrancePole.position.y+.02f)));
            if(!contactShadow.enabled)return;
            contactShadow.transform.SetPositionAndRotation(transform.position+Vector3.up*.012f,
                Quaternion.Euler(90,transform.eulerAngles.y,0));
        }

        internal void SetVisibility(float visibility)
        {
            Character.SetVisibility(visibility);
            if(!contactShadow)return;
            shadowProperties??=new MaterialPropertyBlock();
            shadowProperties.SetFloat(ShadowOpacity,visibility);
            contactShadow.SetPropertyBlock(shadowProperties);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearContactShadowResources()
        {
            if(contactShadowMaterial)Destroy(contactShadowMaterial);
            if(contactShadowMesh)Destroy(contactShadowMesh);
            contactShadowMaterial=null;
            contactShadowMesh=null;
        }
        public void Advance(float deltaTime)
        {
            elapsed+=deltaTime;
            if(State==ParkRabbitState.OpeningDoor)
            {
                if(house.DoorFraction<.999f)return;
                State=ParkRabbitState.WalkingOut;elapsed=0;Character.Skin.enabled=true;
            }
            if(State==ParkRabbitState.WalkingOut)
            {
                ExitedThroughOpenDoor&=house.DoorFraction>=.999f;
                if(house.BushEntrance)AdvanceBushEntrance();
                else if(house.EntranceSlide)AdvanceSlideEntrance();
                else if(house.EntrancePole)AdvancePoleEntrance();
                else WalkTo(house.ExitPosition(elapsed/house.ExitDuration));
                if(elapsed>=house.ExitDuration)
                {var p=transform.position;p.y=park.SurfaceHeight(p);transform.position=p;State=ParkRabbitState.Waving;elapsed=0;WaveCount++;house.CloseAfterExit();}
            }
            else if(State==ParkRabbitState.Waving)
            {
                Character.PlayWavePose(elapsed/WaveDuration);
                if(elapsed<WaveDuration)return;
                State=ParkRabbitState.Wandering;
                elapsed=0;
                phase=0;
                wanderSpeed=0;
                leavingEntrance=true;
                ChooseDeparture();
            }
            else if(State==ParkRabbitState.Wandering)
            {
                if(!AdvanceActivity(deltaTime))Wander(deltaTime);
            }
        }
        void AdvanceBushEntrance()
        {
            transform.rotation=house.Facing;
            if(elapsed<ParkHouse.BushPeekSeconds)
            {
                float peek=Mathf.SmoothStep(0,1,elapsed/ParkHouse.BushPeekSeconds);
                transform.position=Vector3.Lerp(house.Inside.position,house.Threshold.position,peek);
                Character.PlayPeekPose(0);
                return;
            }
            float lookTime=elapsed-ParkHouse.BushPeekSeconds;
            if(lookTime<ParkHouse.BushLookSeconds)
            {
                float progress=lookTime/ParkHouse.BushLookSeconds;
                float yaw=progress<.3f?Mathf.Lerp(0,-38,Mathf.SmoothStep(0,1,progress/.3f)):
                    progress<.7f?Mathf.Lerp(-38,38,Mathf.SmoothStep(0,1,(progress-.3f)/.4f)):
                    Mathf.Lerp(38,0,Mathf.SmoothStep(0,1,(progress-.7f)/.3f));
                transform.position=house.Threshold.position;
                Character.PlayPeekPose(yaw);
                return;
            }
            float walkTime=lookTime-ParkHouse.BushLookSeconds;
            WalkTo(Vector3.Lerp(house.Threshold.position,house.Welcome.position,walkTime/ParkHouse.BushWalkSeconds));
            Character.ApplyBushLean(1-Mathf.SmoothStep(0,1,walkTime/.5f));
        }
        void AdvanceSlideEntrance()
        {
            var entranceSlide=house.EntranceSlide;
            float rideTime=elapsed-ParkSlide.EntrySeconds;
            if(rideTime<0)
            {
                entranceSlide.SlideDown(0,out var lip,out _);
                float entry=Mathf.Clamp01(elapsed/ParkSlide.EntrySeconds);
                var position=Vector3.Lerp(house.Inside.position,lip,entry);
                if(entry<.70f)WalkTo(position);
                else
                {
                    transform.position=position;
                    Character.PlaySlidePose(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,entry)));
                }
            }
            else if(rideTime<ParkSlide.DescentSeconds)Ride(entranceSlide,rideTime/ParkSlide.DescentSeconds);
            else
            {
                float standTime=rideTime-ParkSlide.DescentSeconds;
                if(standTime<ParkSlide.StandSeconds)
                {
                    Ride(entranceSlide,1);
                    Character.PlaySlidePose(1-Mathf.SmoothStep(0,1,standTime/ParkSlide.StandSeconds));
                }
                else WalkTo(entranceSlide.WalkOut((standTime-ParkSlide.StandSeconds)/ParkSlide.ExitWalkSeconds));
            }
        }

        void AdvancePoleEntrance()
        {
            var pole=house.EntrancePole.position;
            var feetAtBase=pole+house.transform.forward*.42f;
            var feetAtTop=feetAtBase;
            feetAtTop.y=house.Threshold.position.y;
            float rideTime=elapsed-ParkHouse.PoleEntrySeconds;
            if(rideTime<0)
            {
                float approach=Mathf.Clamp01(elapsed/ParkHouse.PoleEntrySeconds);
                var point=Vector3.Lerp(house.Inside.position,feetAtTop,approach);
                transform.rotation=house.Facing;
                if(approach<.66f)WalkTo(point);
                else
                {
                    transform.position=point;
                    Character.PlayPolePose(pole,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,1,approach)));
                }
                return;
            }
            if(rideTime<ParkHouse.PoleDescentSeconds)
            {
                float t=rideTime/ParkHouse.PoleDescentSeconds;
                float descent=t-.115f*Mathf.Sin(2*Mathf.PI*t);
                var turn=Quaternion.AngleAxis(Mathf.SmoothStep(0,360,t),Vector3.up);
                var position=pole+turn*(feetAtBase-pole);
                position.y=Mathf.Lerp(feetAtTop.y,feetAtBase.y,descent);
                transform.SetPositionAndRotation(position,turn*house.Facing);
                Character.PlayPolePose(pole,1);
                return;
            }

            // Step around the pole while letting go, then walk out to the greeting spot.
            float releaseTime=rideTime-ParkHouse.PoleDescentSeconds;
            var stepAside=feetAtBase-house.transform.right*.51f;
            var direction=house.Welcome.position-stepAside;
            direction.y=0;
            var facing=Quaternion.LookRotation(-direction);
            if(releaseTime<ParkHouse.PoleReleaseSeconds)
            {
                float release=Mathf.SmoothStep(0,1,releaseTime/ParkHouse.PoleReleaseSeconds);
                transform.SetPositionAndRotation(Vector3.Lerp(feetAtBase,stepAside,release),
                    Quaternion.Slerp(house.Facing,facing,release));
                Character.PlayPolePose(pole,1-release);
                return;
            }
            transform.rotation=facing;
            WalkTo(Vector3.Lerp(stepAside,house.Welcome.position,(releaseTime-ParkHouse.PoleReleaseSeconds)/ParkHouse.PoleWalkSeconds));
        }

        void Ride(ParkSlide course,float progress)
        {
            course.SlideDown(progress,out var position,out var rotation);
            transform.SetPositionAndRotation(position,rotation);
            Character.PlaySlidePose(1);
        }

        void WalkTo(Vector3 position)
        {
            var step=position-transform.position;
            float distance=new Vector2(step.x,step.z).magnitude;
            transform.position=position;
            phase=Character.AdvanceWalkPhase(phase,distance);
            WalkedDistance+=distance;
            Character.PlayWalkingPose(phase);
        }

        float Range(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        internal bool IsEmerging=>State==ParkRabbitState.OpeningDoor||State==ParkRabbitState.WalkingOut;

        internal void RestartEmergence(ParkHouse entrance,int index)
        {
            house=entrance;HouseIndex=index;
            State=ParkRabbitState.OpeningDoor;
            elapsed=0;phase=0;wanderSpeed=0;
            leavingEntrance=false;
            transform.SetPositionAndRotation(house.Inside.position,house.Facing);
            Character.PlayIdlePose();Character.Skin.enabled=false;
            gameObject.SetActive(true);
        }

        internal void ChangeVenue(ParkHouse entrance,int index)
        {
            if(CurrentActivity!=Activity.None)FinishActivity();
            float exitProgress=elapsed/house.ExitDuration;
            bool changedEntranceType=(bool)house.EntranceSlide!=(bool)entrance.EntranceSlide||
                (bool)house.EntrancePole!=(bool)entrance.EntrancePole||house.BushEntrance!=entrance.BushEntrance;
            house=entrance;HouseIndex=index;
            switch(State)
            {
                case ParkRabbitState.OpeningDoor:
                    transform.SetPositionAndRotation(house.Inside.position,house.Facing);
                    break;
                case ParkRabbitState.WalkingOut:
                    if(changedEntranceType)
                    {
                        RestartEmergence(entrance,index);
                        break;
                    }
                    elapsed=exitProgress*house.ExitDuration;
                    if(house.BushEntrance)AdvanceBushEntrance();
                    else if(house.EntranceSlide)AdvanceSlideEntrance();
                    else if(house.EntrancePole)AdvancePoleEntrance();
                    else transform.SetPositionAndRotation(house.ExitPosition(exitProgress),house.Facing);
                    break;
                case ParkRabbitState.Waving:
                    transform.SetPositionAndRotation(house.Welcome.position,house.Facing);
                    GroundAtCurrentPosition();
                    break;
                case ParkRabbitState.Wandering:
                    var p=transform.position;
                    p.x=Mathf.Clamp(p.x,park.WalkArea.xMin,park.WalkArea.xMax);
                    p.z=Mathf.Clamp(p.z,park.WalkArea.yMin,park.WalkArea.yMax);
                    if(!park.IsSceneryClear(p))p=house.Welcome.position;
                    transform.position=p;
                    GroundAtCurrentPosition();
                    ChooseDestination();
                    break;
            }
        }

        // The first walk starts normally. Optional activities are scheduled later during roaming.
        bool AdvanceActivity(float dt)
        {
            var position=transform.position;
            if((CurrentActivity==Activity.Hop||CurrentActivity==Activity.Dance)&&OverlapAt(position)>0)
            {
                FinishActivity();
                return false;
            }
            if(CurrentActivity==Activity.None)
            {
                if(leavingEntrance)return false;
                activityDelay-=dt;
                if(activityDelay>0||!park.IsClear(position,this,1.2f))return false;
                activityDelay=Range(4,7);
                var candidate=park.Slide;
                if(candidate&&Vector3.Distance(position,candidate.Approach)<6&&TryStartSlide())return false;
                StartActivity(random.Next(2)==0?Activity.Hop:Activity.Dance);
            }

            activityTime+=dt;
            switch(CurrentActivity)
            {
                case Activity.Hop:
                    Character.PlayHopPose(Mathf.Clamp01(activityTime/.9f));
                    if(activityTime>=.9f)FinishActivity();
                    return true;
                case Activity.Dance:
                    Character.PlayDancePose(Mathf.Clamp01(activityTime/2.15f));
                    if(activityTime>=2.15f)FinishActivity();
                    return true;
                case Activity.ApproachSlide:
                    if(activityTime>13||!slide.isActiveAndEnabled){FinishActivity();return false;}
                    if(Vector3.Distance(transform.position,slide.Approach)>.07f)return false;
                    StartActivity(Activity.Climb);
                    return true;
                case Activity.Climb:
                    const float climbSeconds=7.0f;
                    Vector3 next=slide.Climb(activityTime/climbSeconds);
                    Vector3 heading=next-transform.position;heading.y=0;
                    if(heading.sqrMagnitude>.000001f)
                        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-heading),dt*420);
                    WalkTo(next);
                    if(activityTime>=climbSeconds)StartActivity(Activity.Sit);
                    return true;
                case Activity.Sit:
                    slide.SlideDown(0,out _,out var facing);
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,facing,dt*520);
                    Character.PlaySlidePose(Mathf.SmoothStep(0,1,activityTime/.4f));
                    if(activityTime>=.4f)StartActivity(Activity.Slide);
                    return true;
                case Activity.Slide:
                    Ride(slide,activityTime/ParkSlide.DescentSeconds);
                    if(activityTime>=ParkSlide.DescentSeconds)StartActivity(Activity.LeaveSlide);
                    return true;
                case Activity.LeaveSlide:
                    if(activityTime<=ParkSlide.StandSeconds)
                    {
                        Character.PlaySlidePose(1-Mathf.SmoothStep(0,1,activityTime/ParkSlide.StandSeconds));
                        return true;
                    }
                    float walk=(activityTime-ParkSlide.StandSeconds)/ParkSlide.ExitWalkSeconds;
                    WalkTo(slide.WalkOut(walk));
                    if(walk>=1)FinishActivity();
                    return true;
                default:return false;
            }
        }

        internal bool TryStartSlide()
        {
            var candidate=park.Slide;
            if(State!=ParkRabbitState.Wandering||CurrentActivity!=Activity.None||!candidate||
                !PathIsClear(candidate.Approach)||!park.IsClear(candidate.Exit,this,AnimalSpacing)||!candidate.Reserve(this))return false;
            slide=candidate;
            destination=slide.Approach;
            leavingEntrance=false;
            StartActivity(Activity.ApproachSlide);
            return true;
        }

        internal void StartActivity(Activity activity)
        {
            CurrentActivity=activity;
            activityTime=0;
            pause=0;
            wanderSpeed=0;
        }

        void FinishActivity()
        {
            if(slide)slide.Release(this);
            slide=null;
            CurrentActivity=Activity.None;
            activityDelay=Range(9,21);
            Character.PlayIdlePose();
            ChooseDestination();
        }

        void OnDisable()
        {
            if(slide)slide.Release(this);
        }
        void GroundAtCurrentPosition()
        {
            var p=transform.position;p.y=park.SurfaceHeight(p);transform.position=p;
        }
        void ChooseDeparture()
        {
            int previous=house.LastDepartureDirection;
            var forward=-house.transform.forward;
            // In a narrow aisle, try a shorter step before falling back to a room-wide destination.
            for(float distance=2.5f;distance>=.5f;distance-=1f)
            {
                int selected=-1,choices=0;
                var selectedPoint=Vector3.zero;
                for(int index=0;index<DepartureAngles.Length;index++)
                {
                    var point=transform.position+Quaternion.Euler(0,DepartureAngles[index],0)*forward*distance;
                    if(!PathIsClear(point)||!park.IsClear(point,this,AnimalSpacing))continue;
                    if(index==previous)
                    {
                        if(selected<0){selected=index;selectedPoint=point;}
                        continue;
                    }
                    if(random.Next(++choices)!=0)continue;
                    selected=index;
                    selectedPoint=point;
                }
                if(selected<0)continue;
                destination=selectedPoint;
                house.LastDepartureDirection=selected;
                blocked=0;
                return;
            }
            ChooseDestination();
        }
        bool PathIsClear(Vector3 point)
        {
            if(!park.WalkArea.Contains(new Vector2(point.x,point.z)))return false;
            var origin=transform.position;
            int segments=Mathf.CeilToInt(Vector3.Distance(origin,point)/.4f);
            for(int i=1;i<=segments;i++)
                if(!park.IsSceneryClear(Vector3.Lerp(origin,point,(float)i/segments)))return false;
            return true;
        }
        void ChooseDestination()
        {
            var area=park.WalkArea;
            destination=transform.position;
            for(int i=0;i<20;i++)
            {
                var point=new Vector3(Range(area.xMin,area.xMax),park.GroundHeight,Range(area.yMin,area.yMax));
                if(Vector3.Distance(point,transform.position)<=2||!PathIsClear(point)||!park.IsClear(point,this,.9f))continue;
                destination=point;
                break;
            }
            blocked=0;
        }
        void Wander(float deltaTime)
        {
            var position=transform.position;
            float overlap=OverlapAt(position);
            if(pause>0&&overlap==0){pause-=deltaTime;wanderSpeed=0;Character.PlayIdlePose();return;}
            var offset=destination-position;offset.y=0;
            if(offset.magnitude<.18f&&overlap==0&&CurrentActivity!=Activity.ApproachSlide)
            {
                if(!leavingEntrance)
                {VisitedTargets++;pause=Range(.6f,1.7f);wanderSpeed=0;ChooseDestination();Character.PlayIdlePose();return;}
                ChooseDeparture();
                offset=destination-position;offset.y=0;
            }
            var desired=offset.normalized;
            foreach(var other in park.Animals)
            {
                if(other==this||other.State==ParkRabbitState.OpeningDoor)continue;
                var away=position-other.transform.position;away.y=0;
                float distance=away.magnitude;
                if(distance<1.15f)
                {
                    if(distance<.001f)away=Id<other.Id?Vector3.left:Vector3.right;
                    desired+=away.normalized*(1-Mathf.Clamp01(distance/1.15f))*2.1f;
                }
            }
            if(desired.sqrMagnitude<.001f)desired=-transform.forward;
            wanderSpeed=Mathf.MoveTowards(wanderSpeed,WanderSpeed,deltaTime*2.6f);
            var step=desired.normalized*(wanderSpeed*deltaTime);
            // Keep accelerating if a very short frame rounds the first step to the same position.
            if((position+step).Equals(position))return;
            if(!TryStep(position,step,overlap,out var next))
            {
                blocked+=deltaTime;wanderSpeed=0;Character.PlayIdlePose();
                if(blocked>1.2f)
                {
                    if(CurrentActivity==Activity.ApproachSlide)FinishActivity();
                    else ChooseDestination();
                }
                return;
            }
            var heading=next-position;
            var targetRotation=Quaternion.LookRotation(-heading/heading.magnitude,Vector3.up);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,deltaTime*(leavingEntrance?300:150));
            float turn=Quaternion.Angle(transform.rotation,targetRotation);
            if(overlap==0&&turn>35&&!leavingEntrance)
            {wanderSpeed=0;Character.PlayIdlePose();return;}
            if(overlap==0)
                next=Vector3.Lerp(position,next,1-Mathf.SmoothStep(0,leavingEntrance?.55f:.8f,Mathf.InverseLerp(15,65,turn)));
            heading=next-position;
            next.y=park.SurfaceHeight(next);
            float moved=heading.magnitude;transform.position=next;WalkedDistance+=moved;
            phase=Character.AdvanceWalkPhase(phase,moved);
            float walkBlend=Mathf.SmoothStep(0,1,moved/(WanderSpeed*deltaTime));
            Character.PlayWalkingPose(phase,walkBlend);blocked=0;
            if(turn<=15)leavingEntrance=false;
        }
        float OverlapAt(Vector3 point)
        {
            float overlap=0;
            foreach(var other in park.Animals)
            {
                if(other==this||other.State==ParkRabbitState.OpeningDoor)continue;
                var delta=point-other.transform.position;
                float distanceSquared=delta.x*delta.x+delta.z*delta.z;
                if(distanceSquared>=AnimalSpacing*AnimalSpacing)continue;
                float penetration=AnimalSpacing-Mathf.Sqrt(distanceSquared);
                overlap+=penetration*penetration;
            }
            return overlap;
        }
        bool TryStep(Vector3 position,Vector3 step,float overlap,out Vector3 next)
        {
            var area=park.WalkArea;
            next=position;
            float best=overlap;
            for(int i=-1;i<AvoidanceAngles.Length;i++)
            {
                var candidate=position+(i<0?step:Quaternion.Euler(0,AvoidanceAngles[i],0)*step);
                candidate.x=Mathf.Clamp(candidate.x,area.xMin,area.xMax);
                candidate.z=Mathf.Clamp(candidate.z,area.yMin,area.yMax);
                if(candidate.Equals(position)||!park.IsSceneryClear(candidate))continue;
                float cost=OverlapAt(candidate);
                if(cost==0){next=candidate;return true;}
                // An animal already inside another may take a step that reduces the overlap.
                if(cost>=best)continue;
                best=cost;
                next=candidate;
            }
            return !next.Equals(position);
        }
    }
}
