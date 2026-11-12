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
        float elapsed,phase,pause,blocked;
        Vector3 destination;
        const float ExitDuration=3.6f,WaveDuration=1.65f;

        public void Initialize(ParkPlayground owner,ParkHouse source,int houseIndex,int id,byte[] front,byte[] back,bool raw=false,AnimalKind kind=AnimalKind.Rabbit)
        {
            park=owner;house=source;HouseIndex=houseIndex;Id=id;random=new System.Random(id*7919+17);
            Character=gameObject.AddComponent<RabbitCharacter>();
            Character.EnsureBuilt(kind);
            Character.ExternalMotion=true;Character.Grounded=true;Character.ApplyPainting(front,back,raw);
            float scale=kind==AnimalKind.Giraffe?.40f:kind==AnimalKind.Elephant?.39f:.42f;
            transform.localScale=Vector3.one*scale;transform.SetPositionAndRotation(house.Inside.position,house.Facing);
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
                if(elapsed>=WaveDuration){State=ParkRabbitState.Wandering;elapsed=0;ChooseDestination();}
            }
            else if(State==ParkRabbitState.Wandering)Wander(deltaTime);
        }
        float Range(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        void ChooseDestination()
        {
            var area=park.WalkArea;
            for(int i=0;i<20;i++)
            {
                destination=new Vector3(Range(area.xMin,area.xMax),park.GroundHeight,Range(area.yMin,area.yMax));
                if(Vector3.Distance(destination,transform.position)>2&&park.IsClear(destination,this,.9f))break;
            }
            blocked=0;
        }
        void Wander(float deltaTime)
        {
            if(pause>0){pause-=deltaTime;Character.PlayIdlePose();return;}
            var offset=destination-transform.position;offset.y=0;
            if(offset.magnitude<.18f)
            {VisitedTargets++;pause=Range(.6f,1.7f);ChooseDestination();Character.PlayIdlePose();return;}
            var desired=offset.normalized;
            foreach(var other in park.Animals)
            {
                if(other==this||other.State==ParkRabbitState.OpeningDoor)continue;
                var away=transform.position-other.transform.position;away.y=0;
                float distance=away.magnitude;
                if(distance<1.15f)
                {
                    if(distance<.001f)away=Id<other.Id?Vector3.left:Vector3.right;
                    desired+=away.normalized*(1-Mathf.Clamp01(distance/1.15f))*2.1f;
                }
            }
            if(desired.sqrMagnitude<.001f)desired=offset.normalized;
            desired.Normalize();var targetRotation=Quaternion.LookRotation(-desired,Vector3.up);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,deltaTime*150);
            if(Quaternion.Angle(transform.rotation,targetRotation)>35){Character.PlayIdlePose();return;}
            var next=transform.position+desired*(.68f*deltaTime);
            next.x=Mathf.Clamp(next.x,park.WalkArea.xMin,park.WalkArea.xMax);next.z=Mathf.Clamp(next.z,park.WalkArea.yMin,park.WalkArea.yMax);
            next.y=park.SurfaceHeight(next);
            if(!park.IsClear(next,this,.82f))
            {
                blocked+=deltaTime;Character.PlayIdlePose();if(blocked>1.2f)ChooseDestination();return;
            }
            float moved=Vector3.Distance(next,transform.position);transform.position=next;WalkedDistance+=moved;phase+=moved*12;
            Character.PlayWalkingPose(phase);blocked=0;
        }
    }
}
