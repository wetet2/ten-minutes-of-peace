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
        const float ExitDuration=3.75f,WaveDuration=1.65f;
        const float WanderSpeed=.65f;
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
                var previous=transform.position;transform.position=house.ExitPosition(elapsed/ExitDuration);
                float distance=Vector3.Distance(previous,transform.position);phase+=distance*12;WalkedDistance+=distance;
                Character.PlayWalkingPose(phase);
                if(elapsed>=ExitDuration)
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
            else if(State==ParkRabbitState.Wandering)Wander(deltaTime);
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
            house=entrance;HouseIndex=index;
            switch(State)
            {
                case ParkRabbitState.OpeningDoor:
                    transform.SetPositionAndRotation(house.Inside.position,house.Facing);
                    break;
                case ParkRabbitState.WalkingOut:
                    transform.SetPositionAndRotation(house.ExitPosition(elapsed/ExitDuration),house.Facing);
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
            if(offset.magnitude<.18f&&overlap==0)
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
            if(!TryStep(position,desired.normalized*(wanderSpeed*deltaTime),overlap,out var next))
            {
                blocked+=deltaTime;wanderSpeed=0;Character.PlayIdlePose();if(blocked>1.2f)ChooseDestination();return;
            }
            var heading=next-position;
            var targetRotation=Quaternion.LookRotation(-heading,Vector3.up);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,deltaTime*(leavingEntrance?300:150));
            float turn=Quaternion.Angle(transform.rotation,targetRotation);
            if(overlap==0&&turn>35&&!leavingEntrance)
            {wanderSpeed=0;Character.PlayIdlePose();return;}
            if(overlap==0)
                next=Vector3.Lerp(position,next,1-Mathf.SmoothStep(0,leavingEntrance?.55f:.8f,Mathf.InverseLerp(15,65,turn)));
            heading=next-position;
            next.y=park.SurfaceHeight(next);
            float moved=heading.magnitude;transform.position=next;WalkedDistance+=moved;phase+=moved*12;
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
                if((candidate-position).sqrMagnitude<.00000001f||!park.IsSceneryClear(candidate))continue;
                float cost=OverlapAt(candidate);
                if(cost==0){next=candidate;return true;}
                // An animal already inside another may take a step that reduces the overlap.
                if(cost>=best)continue;
                best=cost;
                next=candidate;
            }
            return next!=position;
        }
    }
}
