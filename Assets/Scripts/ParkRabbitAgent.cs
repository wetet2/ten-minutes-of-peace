using UnityEngine;

namespace PeacePark
{
    internal interface IParkAnimalRide
    {
        ParkAnimalAction Action { get; }
        bool TryLeaveQueue(ParkRabbitAgent animal);
        void Release(ParkRabbitAgent animal);
    }

    internal enum ParkAnimalAction { None, Wheel, Train, Carousel, Slide, Hop, Dance, Trampoline, Seesaw, Roundabout,
        StumpHop, WoodenBridge, Zipline, SandSled, Deer, Tube, Swim, WaterSlide, Camp, Pole, Skate, BlowBubbles, ResortBridge, Sunbed }

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
        internal enum Activity { None, Stroll, Hop, Dance, BlowBubbles, ApproachSlide, Climb, Sit, Slide, LeaveSlide }
        internal Activity CurrentActivity {get;private set;}
        ParkSlide slide;
        IParkAnimalRide ride;
        Vector3 plazaDestination;
        ParkAnimalAction pendingAction;
        float actionRetry;
        float deerRouteRetry;
        float groundTimeBalance;
        float parkPlayTimeBudget,parkDanceTimeBalance;
        bool rideActivityStarted;
        Vector3[] rideRoute;
        int rideWaypoint;
        bool jogToRide;
        bool automaticRideApproach;
        float rideApproachJog;
        internal ParkAnimalAction PendingAction => pendingAction;
        internal bool IsUsingRide => (ride!=null&&rideActivityStarted)||CurrentActivity>=Activity.Climb;
        internal bool ShouldExtendAutomaticRide => automaticRideApproach&&groundTimeBalance<-.75f;
        internal bool CanJoinSeesaw=>State==ParkRabbitState.Wandering&&!leavingEntrance&&ride==null&&
            CurrentActivity==Activity.None&&(pendingAction==ParkAnimalAction.None||pendingAction==ParkAnimalAction.Seesaw);
        internal ParkAnimalAction ActiveAction => ride?.Action ?? (CurrentActivity switch
        {
            Activity.None or Activity.Stroll => ParkAnimalAction.None,
            Activity.Hop => ParkAnimalAction.Hop,
            Activity.Dance => ParkAnimalAction.Dance,
            Activity.BlowBubbles => ParkAnimalAction.BlowBubbles,
            _ => ParkAnimalAction.Slide
        });
        internal bool UseContactShadow {get;set;}
        internal bool IsHiddenForRide {get;private set;}
        MeshRenderer contactShadow;
        MaterialPropertyBlock shadowProperties;
        float lifetimeVisibility=1,rideVisibility=1;
        static Material contactShadowMaterial;
        static Mesh contactShadowMesh;
        static readonly int ShadowOpacity=Shader.PropertyToID("_Opacity");
        float activityTime,activityDelay;
        internal const float WaveDuration=1.65f;
        internal const float WanderSpeed=.70f;
        internal const float AnimalSpacing=.82f;
        static readonly float[] DepartureAngles={-84,-56,-28,0,28,56,84};
        static readonly float[] AvoidanceAngles={30,-30,60,-60,90,-90,120,-120,150,-150,180};
        const float GroundTimeDebtLimit=12f;
        const float HopDuration=.9f,DanceDuration=2.15f;
        const float BubbleDuration=1.95f;
        static readonly float[] BubblePuffTimes={.36f,.94f,1.52f};
        internal const float SizeMultiplier=1.3f;
        bool UsesParkActivityMix=>park.ThemeName=="공원";

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
                ApplyVisibility();
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
                !(ride is ParkSeesaw&&transform.position.y>park.GroundHeight+.05f)&&
                !(ride is ParkForestRide forestRide&&forestRide.IsAirborne)&&
                !((ride is ParkResortRiver or ParkResortSwim)&&transform.position.y<ParkResortRiver.WaterHeight)&&
                !(ride is ParkResortSlide resortSlide&&resortSlide.ShouldHideShadow(this))&&
                !(ride is ParkPoleRide&&transform.position.y>park.SurfaceHeight(transform.position)+.02f)&&
                !(State==ParkRabbitState.WalkingOut&&
                    ((house.EntranceSlide&&elapsed<house.ExitDuration-ParkSlide.ExitWalkSeconds)||
                    (house.EntrancePole&&transform.position.y>house.EntrancePole.position.y+.02f)));
            if(!contactShadow.enabled)return;
            var surface=transform.position;
            if(ride is ParkTrampoline trampoline)surface.y=Mathf.Min(surface.y,trampoline.MatSurfaceY);
            if(ride is ParkRoundaboutRide roundabout)surface.y=roundabout.SurfaceY(surface);
            if(ride is ParkIceRink rink&&rink.IsOnIce(surface))surface.y=rink.SurfaceY;
            if(ride is ParkSandSled or ParkVillageDeer or ParkDesertCamp)surface.y=park.SurfaceHeight(surface);
            contactShadow.transform.SetPositionAndRotation(surface+Vector3.up*.012f,
                Quaternion.Euler(90,transform.eulerAngles.y,0));
        }

        internal void SetVisibility(float visibility)
        {
            lifetimeVisibility=visibility;
            ApplyVisibility();
        }

        internal void SetRideVisibility(float visibility)
        {
            rideVisibility=visibility;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            float visibility=lifetimeVisibility*rideVisibility;
            Character.SetVisibility(visibility);
            if(!contactShadow)return;
            shadowProperties??=new MaterialPropertyBlock();
            shadowProperties.SetFloat(ShadowOpacity,visibility);
            contactShadow.SetPropertyBlock(shadowProperties);
        }

        internal void SetRideHidden(bool hidden)
        {
            IsHiddenForRide=hidden;
            Character.Skin.enabled=!hidden;
            if(contactShadow)UpdateContactShadow();
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
            if(ride!=null)
            {
                AdvanceActivityClock(deltaTime);
                return;
            }
            elapsed+=deltaTime;
            if(State==ParkRabbitState.OpeningDoor)
            {
                if(house.DoorFraction<.999f)return;
                State=ParkRabbitState.WalkingOut;elapsed=0;Character.Skin.enabled=true;
            }
            if(State==ParkRabbitState.WalkingOut)
            {
                ExitedThroughOpenDoor&=house.DoorFraction>=.999f;
                if(house.EntranceSlide)AdvanceSlideEntrance();
                else if(house.PoleRide){AdvancePoleEntrance();return;}
                else if(house.CampTent)AdvanceTentArrival(deltaTime);
                else WalkTo(house.ExitPosition(elapsed/house.ExitDuration),cadenceScale:house.ExitTerrain ? .74f : 1);
                if(elapsed>=house.ExitDuration)
                {var p=transform.position;p.y=park.SurfaceHeight(p);transform.position=p;State=ParkRabbitState.Waving;elapsed=0;WaveCount++;house.CloseAfterExit();}
            }
            else if(State==ParkRabbitState.Waving)
            {
                Character.PlayWavePose(elapsed/WaveDuration);
                if(elapsed<WaveDuration)return;
                BeginWanderingAfterGreeting();
            }
            else if(State==ParkRabbitState.Wandering)
            {
                if(!leavingEntrance)AdvanceActivityClock(deltaTime);
                AdvanceRequestedAction(deltaTime);
                if(ride!=null)return;
                if(!AdvanceActivity(deltaTime))Wander(deltaTime);
            }
        }

        void BeginWanderingAfterGreeting()
        {
            State=ParkRabbitState.Wandering;
            elapsed=phase=wanderSpeed=0;
            leavingEntrance=true;
            activityDelay=Range(1.15f,2.2f);
            ChooseDeparture();
        }
        void AdvanceTentArrival(float dt)
        {
            float progress=elapsed/house.ExitDuration;
            var point=house.ExitPosition(progress);
            var heading=Vector3.ProjectOnPlane(point-transform.position,Vector3.up);
            if(heading.sqrMagnitude>.000001f)
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-heading),dt*300);
            WalkTo(point);
            if(progress<.88f)
                house.CampTent.FitUnderTent(Character,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,.88f,progress)));
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
            var poleRide=house.PoleRide;
            float waveTime=elapsed-ParkPoleRide.ArrivalWalkSeconds;
            if(waveTime<0)
            {
                transform.rotation=house.Facing;
                WalkTo(Vector3.Lerp(house.Inside.position,poleRide.WaveSpot,elapsed/ParkPoleRide.ArrivalWalkSeconds));
                return;
            }
            if(WaveCount==0)WaveCount++;
            if(waveTime<WaveDuration)
            {
                transform.SetPositionAndRotation(poleRide.WaveSpot,house.Facing);
                Character.PlayWavePose(waveTime/WaveDuration);
                return;
            }
            poleRide.AdvanceSlide(this,waveTime-WaveDuration);
            if(elapsed<house.ExitDuration)return;
            GroundAtCurrentPosition();
            house.CloseAfterExit();
            BeginWanderingAfterGreeting();
        }

        void Ride(ParkSlide course,float progress)
        {
            course.SlideDown(progress,out var position,out var rotation);
            transform.SetPositionAndRotation(position,rotation);
            Character.PlaySlidePose(1);
        }

        void WalkTo(Vector3 position,float cadenceScale=1f,float jog=0)
        {
            var step=position-transform.position;
            float distance=new Vector2(step.x,step.z).magnitude;
            transform.position=position;
            phase=Character.AdvanceWalkPhase(phase,distance*cadenceScale,jog);
            WalkedDistance+=distance;
            Character.PlayWalkingPose(phase,1,jog);
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

        internal void ChangeVenue(ParkHouse entrance,int index,Vector3 position)
        {
            house=entrance;HouseIndex=index;
            groundTimeBalance=0;
            parkPlayTimeBudget=parkDanceTimeBalance=0;
            rideActivityStarted=false;
            if(State==ParkRabbitState.OpeningDoor)
            {
                transform.SetPositionAndRotation(house.Inside.position,house.Facing);
                return;
            }
            ride?.Release(this);
            ride=null;
            pendingAction=ParkAnimalAction.None;
            State=ParkRabbitState.Wandering;
            elapsed=phase=pause=wanderSpeed=0;
            leavingEntrance=false;
            position.y=park.SurfaceHeight(position);
            transform.SetPositionAndRotation(position,Quaternion.Euler(0,Range(0,360),0));
            FinishActivity();
            activityDelay=Range(.8f,2.2f);
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
            if(CurrentActivity==Activity.Stroll&&groundTimeBalance<=0)FinishActivity();
            if(CurrentActivity==Activity.None)
            {
                if(leavingEntrance||pendingAction!=ParkAnimalAction.None)return false;
                activityDelay-=dt;
                if(activityDelay>0)return false;
                if(TryStartAutomaticActivity())return true;
                if(CurrentActivity==Activity.None)return false;
            }

            float previousTime=activityTime;
            activityTime+=dt;
            if(UsesParkActivityMix&&(CurrentActivity is Activity.Hop or Activity.Dance))
            {
                parkPlayTimeBudget-=dt;
                parkDanceTimeBalance+=CurrentActivity==Activity.Dance?dt:-dt;
            }
            switch(CurrentActivity)
            {
                case Activity.Hop:
                    Character.PlayHopPose(Mathf.Clamp01(activityTime/HopDuration));
                    if(activityTime>=HopDuration)FinishActivity();
                    return true;
                case Activity.Dance:
                    Character.PlayDancePose(Mathf.Clamp01(activityTime/DanceDuration));
                    if(activityTime>=DanceDuration)FinishActivity();
                    return true;
                case Activity.BlowBubbles:
                    float puff=0;
                    foreach(float time in BubblePuffTimes)
                        puff=Mathf.Max(puff,Mathf.SmoothStep(1,0,Mathf.Abs(activityTime-time)/.24f));
                    Character.PlayBubblePose(puff);
                    foreach(float time in BubblePuffTimes)
                        if(previousTime<time&&activityTime>=time)
                            park.EmitBubbles(Character.BubbleMouth,Character.BubbleDirection);
                    if(activityTime>=BubbleDuration)FinishActivity();
                    return true;
                case Activity.ApproachSlide:
                    if(!slide.isActiveAndEnabled||(rideRoute==null&&activityTime>13)){FinishActivity();return false;}
                    if(rideRoute!=null)
                    {
                        if(!WalkToRideMark(slide.Approach,slide.StairFacing,dt,keepWalking:true))return true;
                        rideRoute=null;
                    }
                    else
                    {
                        if(Vector3.Distance(transform.position,destination)>.07f)return false;
                        if(destination!=slide.Approach){destination=slide.Approach;return false;}
                        transform.rotation=slide.StairFacing;
                        WalkTo(slide.Approach);
                    }
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

        void StartPlazaStroll()
        {
            var area=park.Venues[park.VenueIndex].PlazaWalkArea;
            for(int i=0;i<12;i++)
            {
                var target=new Vector3(Range(area.xMin,area.xMax),park.GroundHeight,Range(area.yMin,area.yMax));
                if(!park.IsClear(target,this,AnimalSpacing)||!park.TryBuildRidePath(transform.position,target,out var route))continue;
                plazaDestination=target;
                rideRoute=route;
                rideWaypoint=0;
                destination=route.Length>0?route[rideWaypoint++]:target;
                blocked=0;
                StartActivity(Activity.Stroll);
                return;
            }
        }

        internal bool TryStartSlide(bool requested=false)
        {
            var candidate=park.Slide;
            if(State!=ParkRabbitState.Wandering||CurrentActivity!=Activity.None||!candidate||!candidate.Available||
                !park.IsClear(candidate.Exit,this,AnimalSpacing))return false;
            var stairs=candidate.Approach;
            var direction=candidate.StairDirection;
            var toStairs=Vector3.ProjectOnPlane(stairs-transform.position,Vector3.up);
            // Leave a straight final step into the stairs when arriving from the side.
            bool needsLineup=toStairs.sqrMagnitude>.035f*.035f&&Vector3.Dot(toStairs.normalized,direction)<.995f;
            var entry=needsLineup?stairs-direction*1.05f:stairs;
            Vector3[] route=null;
            if(requested?!park.TryBuildRidePath(transform.position,entry,out route):!PathIsClear(entry))return false;
            if(needsLineup&&!park.WalkingSegmentClear(entry,stairs))return false;
            if(!candidate.Reserve(this))return false;
            if(requested&&needsLineup)
            {
                System.Array.Resize(ref route,route.Length+1);
                route[route.Length-1]=entry;
            }
            slide=candidate;
            rideRoute=route;
            rideWaypoint=0;
            jogToRide=requested;
            automaticRideApproach=requested&&pendingAction!=ParkAnimalAction.Slide;
            rideApproachJog=0;
            destination=entry;
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
            rideRoute=null;
            CurrentActivity=Activity.None;
            activityDelay=0;
            Character.PlayIdlePose();
            ChooseDestination();
        }

        void OnDisable()
        {
            if(slide)slide.Release(this);
            ride?.Release(this);
            ride=null;
            rideRoute=null;
            pendingAction=ParkAnimalAction.None;
        }
        internal bool CanRequestAction(ParkAnimalAction action) => action!=pendingAction&&
            (action!=ActiveAction||(action==ParkAnimalAction.SandSled&&ride is ParkSandSled sled&&sled.IsDismounting(this)));

        internal bool RequestAction(ParkAnimalAction action)
        {
            if(!CanRequestAction(action))return false;
            pendingAction=action;
            actionRetry=0;
            if(CurrentActivity==Activity.Stroll)FinishActivity();
            if(ride!=null&&ride.TryLeaveQueue(this))
            {
                ride=null;
                rideRoute=null;
                ChooseDestination();
            }
            return true;
        }
        void AdvanceRequestedAction(float dt)
        {
            if(pendingAction==ParkAnimalAction.None||CurrentActivity!=Activity.None)return;
            actionRetry-=dt;
            if(actionRetry>0)return;
            actionRetry=.55f;
            if(TryStartAction(pendingAction))pendingAction=ParkAnimalAction.None;
        }

        void AdvanceActivityClock(float dt)
        {
            if(UsesParkActivityMix)return;
            // One share of ground time is balanced against as many ride-time shares as there are actions.
            // Approaching, waiting, and spontaneous hops or dances belong to the ground time.
            int actionCount=park.AutomaticAnimalActions.Length;
            if(actionCount==0)return;
            if(IsUsingRide)groundTimeBalance+=dt/actionCount;
            else groundTimeBalance=Mathf.Max(-GroundTimeDebtLimit,groundTimeBalance-dt);
        }

        bool TryStartAutomaticActivity()
        {
            if(!park.CanFollowAnimal(this))return false;
            if(UsesParkActivityMix)
            {
                // Balance actual animation time because a dance lasts longer than a hop.
                bool dance=parkDanceTimeBalance<=0;
                float duration=dance?DanceDuration:HopDuration;
                if(parkPlayTimeBudget>=duration&&park.IsClear(transform.position,this,1.2f))
                    StartActivity(dance?Activity.Dance:Activity.Hop);
                return false;
            }
            if(groundTimeBalance<=0&&(!park.ResortRiver||!park.ResortRiver.IsOnIsland(transform.position))&&
                TryStartRandomRide(park.AutomaticAnimalActions))return true;

            if(random.Next(3)!=0&&park.IsClear(transform.position,this,1.2f))
                StartActivity(random.Next(2)==0?Activity.Hop:Activity.Dance);
            else
            {
                pause=0;
                activityDelay=groundTimeBalance>0?Mathf.Min(groundTimeBalance,Range(1.15f,2.5f)):Range(.8f,1.7f);
                if(groundTimeBalance>0&&park.Carousel)StartPlazaStroll();
            }
            return false;
        }

        bool TryStartRandomRide(ParkAnimalAction[] actions)
        {
            if(actions.Length==0)return false;
            int start=random.Next(actions.Length);
            int stride=random.Next(2)==0?1:actions.Length-1;
            for(int i=0;i<actions.Length;i++)
            {
                var action=actions[(start+i*stride)%actions.Length];
                if(action==ParkAnimalAction.Train&&(!park.Train||!park.Train.CanAutoBoard))continue;
                if(action==ParkAnimalAction.Slide&&park.Slide&&park.HasWaitingArrival(park.Slide.Entrance))continue;
                if(TryAutoRide(action))return true;
            }
            return false;

            bool TryAutoRide(ParkAnimalAction action)
            {
                foreach(var animal in park.Animals)
                    if(animal.pendingAction==action&&animal.ride==null&&animal.State==ParkRabbitState.Wandering&&
                        animal.CurrentActivity==Activity.None&&park.CanFollowAnimal(animal))return false;
                return TryStartAction(action);
            }
        }

        bool TryStartAction(ParkAnimalAction action) => action switch
            {
                ParkAnimalAction.Wheel => park.Wheel&&park.Wheel.TryRequestedRide(this),
                ParkAnimalAction.Train => park.Train&&park.Train.TryRequestedRide(this),
                ParkAnimalAction.Carousel => park.Carousel&&park.Carousel.TryRequestedRide(this),
                ParkAnimalAction.Slide => TryStartSlide(requested:true),
                ParkAnimalAction.Pole => park.PoleRide&&park.PoleRide.TryQueue(park,this),
                ParkAnimalAction.Trampoline => park.Trampoline&&park.Trampoline.TryQueue(this),
                ParkAnimalAction.Seesaw => park.Seesaw&&park.Seesaw.TryPair(park,this),
                ParkAnimalAction.Roundabout => park.Roundabout&&park.Roundabout.TryQueue(this),
                ParkAnimalAction.StumpHop or ParkAnimalAction.WoodenBridge or ParkAnimalAction.Zipline => park.TryRequestForestRide(this,action),
                ParkAnimalAction.SandSled => park.SandSled&&park.SandSled.TryQueue(this),
                ParkAnimalAction.Camp => park.DesertCamp&&park.DesertCamp.TryQueue(park,this),
                ParkAnimalAction.Deer => park.TryRequestDeerRide(this),
                ParkAnimalAction.Skate => park.IceRink&&park.IceRink.TryQueue(this),
                ParkAnimalAction.Tube => park.ResortRiver&&park.ResortRiver.TryQueue(this),
                ParkAnimalAction.Swim => park.ResortSwim&&park.ResortSwim.TryQueue(park,this),
                ParkAnimalAction.WaterSlide => park.ResortSlide&&park.ResortSlide.TryQueue(this),
                ParkAnimalAction.ResortBridge => park.ResortRiver&&park.ResortRiver.TryQueueBridge(this),
                ParkAnimalAction.Sunbed => park.ResortSunbeds&&park.ResortSunbeds.TryQueue(park,this),
                ParkAnimalAction.BlowBubbles => TryBlowBubbles(),
                _ => false
            };

        bool TryBlowBubbles()
        {
            if(!UsesParkActivityMix||leavingEntrance)return false;
            StartActivity(Activity.BlowBubbles);
            return true;
        }
        internal bool TryReserveRequestedRide(IParkAnimalRide attraction,Vector3 mark,bool avoidDeer=false)
        {
            if(!park.TryBuildRidePath(transform.position,mark,out var route,avoidDeer))return false;
            ReserveRide(attraction,route);
            return true;
        }
        internal void ReserveRide(IParkAnimalRide attraction,Vector3[] route)
        {
            bool requested=pendingAction==attraction.Action;
            automaticRideApproach=!requested;
            jogToRide=true;
            if(requested)pendingAction=ParkAnimalAction.None;
            rideActivityStarted=false;
            leavingEntrance=false;
            ride=attraction;
            phase=pause=wanderSpeed=0;
            SetRideRoute(route);
            deerRouteRetry=0;
        }
        internal void SetRideRoute(Vector3[] route)
        {
            rideRoute=route;
            rideWaypoint=0;
            rideApproachJog=0;
        }
        internal bool WalkToRideMark(Vector3 mark,Quaternion facing,float dt,bool keepWalking=false,float speed=1.38f,
            float automaticSpeed=4.5f)
        {
            deerRouteRetry-=dt;
            bool approaching=rideRoute!=null&&rideWaypoint<rideRoute.Length;
            var destination=approaching?rideRoute[rideWaypoint]:mark;
            var target=new Vector3(destination.x,transform.position.y,destination.z);
            var heading=target-transform.position;
            if(heading.sqrMagnitude<.035f*.035f)
            {
                if(!RideStepClear(target,mark))return false;
                if(approaching)
                {
                    target.y=park.SurfaceHeight(target);
                    transform.position=target;
                    rideWaypoint++;
                    return false;
                }
                transform.rotation=facing;
                rideApproachJog=0;
                if(keepWalking)WalkTo(mark);
                else
                {
                    transform.position=mark;
                    Character.PlayIdlePose();
                }
                return true;
            }
            var facingTarget=Quaternion.LookRotation(-heading);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,facingTarget,dt*310);
            if(jogToRide)
            {
                float turn=1-Mathf.InverseLerp(15,75,Quaternion.Angle(transform.rotation,facingTarget));
                float room=Mathf.InverseLerp(.22f,approaching?1.0f:1.45f,heading.magnitude);
                rideApproachJog=Mathf.MoveTowards(rideApproachJog,room*turn,dt*5);
                float runSpeed=Mathf.Max(3f,speed*1.27f);
                if(automaticRideApproach)
                {
                    // Shorten long approaches when they have already used the ground-time allowance.
                    float urgency=Mathf.InverseLerp(0,8,-groundTimeBalance);
                    runSpeed=Mathf.Max(runSpeed,Mathf.Lerp(3.55f,automaticSpeed,urgency));
                }
                speed=Mathf.Lerp(.84f,runSpeed,rideApproachJog)*Mathf.Lerp(.32f,1,turn);
            }
            var next=Vector3.MoveTowards(transform.position,target,dt*speed);
            if(!RideStepClear(next,mark))
            {
                rideApproachJog=0;
                Character.PlayIdlePose();
                return false;
            }
            next.y=park.SurfaceHeight(next);
            WalkTo(next,jog:rideApproachJog);
            return false;
        }
        bool RideStepClear(Vector3 next,Vector3 mark)
        {
            if(!park.WalkingSegmentClear(transform.position,next))return false;
            if(ride is not (ParkVillageDeer or ParkIceRink)||park.DeerPathClear(transform.position,next))return true;
            if(deerRouteRetry>0)return false;
            deerRouteRetry=.35f;
            if(park.TryBuildRidePath(transform.position,mark,out var route,avoidDeer:true))
            {
                rideRoute=route;
                rideWaypoint=0;
            }
            return false;
        }
        internal void WalkAtRideStation(Vector3 point,float cadenceScale=1f)
        {
            var heading=point-transform.position;
            heading.y=0;
            if(heading.sqrMagnitude>.000001f)transform.rotation=Quaternion.LookRotation(-heading);
            WalkTo(point,cadenceScale);
        }
        internal void EndRide()
        {
            rideActivityStarted=false;
            ride=null;
            rideRoute=null;
            if(!isActiveAndEnabled||!park||!Character)return;
            activityDelay=0;
            GroundAtCurrentPosition();
            Character.PlayIdlePose();
            ChooseDestination();
            pause=.35f;
        }
        internal void BeginRideActivity() => rideActivityStarted=true;
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
        internal bool PathIsClear(Vector3 point)
        {
            return park.WalkArea.Contains(new Vector2(point.x,point.z))&&park.WalkingSegmentClear(transform.position,point);
        }
        void ChooseDestination()
        {
            var area=park.ResortRiver&&park.ResortRiver.IsOnIsland(transform.position)?park.ResortRiver.IslandWalkBounds:park.WalkArea;
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
                if(CurrentActivity==Activity.Stroll)
                {
                    if(destination==plazaDestination)
                    {
                        VisitedTargets++;
                        FinishActivity();
                        pause=Range(.6f,1.7f);
                    }
                    else destination=rideWaypoint<rideRoute.Length?rideRoute[rideWaypoint++]:plazaDestination;
                    return;
                }
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
            if(park.Deer!=null)
                foreach(var deer in park.Deer)
                {
                    if(!deer||!deer.isActiveAndEnabled)continue;
                    var away=deer.OffsetFromBody(position);
                    float distance=away.magnitude;
                    if(distance>=1.60f)continue;
                    if(distance<.001f)away=deer.transform.right;
                    desired+=away.normalized*(1-distance/1.60f)*2.6f;
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
                    if(CurrentActivity==Activity.ApproachSlide||CurrentActivity==Activity.Stroll)FinishActivity();
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
            // Six seconds of walking earn four seconds split between dancing and hopping.
            if(moved>0&&!leavingEntrance&&UsesParkActivityMix)
                parkPlayTimeBudget=Mathf.Min(DanceDuration,parkPlayTimeBudget+deltaTime*(4f/6f));
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
            if(park.Deer!=null)
                foreach(var deer in park.Deer)
                {
                    if(!deer||!deer.isActiveAndEnabled)continue;
                    float penetration=deer.AnimalPenetration(point);
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
                if(candidate.Equals(position)||!park.WalkingSegmentClear(position,candidate))continue;
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
