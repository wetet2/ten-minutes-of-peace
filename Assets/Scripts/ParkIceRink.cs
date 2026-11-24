using UnityEngine;

namespace PeacePark
{
    public sealed class ParkIceRink : MonoBehaviour, IParkAnimalRide
    {
        public const float HalfWidth=10f,HalfDepth=5.9f,IceY=.045f,GateHalfWidth=1.65f;
        internal const float AxelTurns=3.5f;
        const float GroundHeight=.025f,AccessSeconds=2.60f;
        const float CircuitSeconds=15.2f,LapSeconds=2*CircuitSeconds;
        const float JumpStart=11.4f,JumpSeconds=1.22f,JumpHeight=1.43f;
        const float CircuitRadiusX=HalfWidth*.84f,CircuitRadiusZ=HalfDepth*.77f;
        [SerializeField] Transform leftGate,rightGate,skateTemplate;

        enum Stage { Approach, Enter, Lap, Exit }

        sealed class Lane
        {
            internal readonly int Side;
            internal ParkRabbitAgent Animal;
            internal Stage Stage;
            internal float Elapsed;
            internal bool AtGate;
            internal Lane(int side) => Side=side;
        }

        readonly Lane[] lanes={new Lane(-1),new Lane(1)};
        float gateFraction;
        ParkAnimalAction IParkAnimalRide.Action=>ParkAnimalAction.Skate;
        internal float SurfaceY=>transform.TransformPoint(Vector3.up*IceY).y;

        public void Configure(Transform left,Transform right,Transform skates)
        {
            leftGate=left;
            rightGate=right;
            skateTemplate=skates;
        }

        internal Vector3 Approach(int side)=>transform.TransformPoint(new Vector3(.85f*side,GroundHeight,HalfDepth+1.08f));
        internal bool IsOnIce(Vector3 point)
        {
            var local=transform.InverseTransformPoint(point);
            return Mathf.Abs(local.x)<HalfWidth&&Mathf.Abs(local.z)<HalfDepth;
        }
        public bool IsMovingPart(Transform part)=>part.IsChildOf(leftGate)||part.IsChildOf(rightGate)||part.IsChildOf(skateTemplate);

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled)return false;
            int first=transform.InverseTransformPoint(animal.transform.position).x<0?0:1;
            for(int i=0;i<lanes.Length;i++)
            {
                var lane=lanes[(first+i)%lanes.Length];
                if(lane.Animal||!animal.TryReserveRequestedRide(this,Approach(lane.Side),avoidDeer:true))continue;
                lane.Animal=animal;
                Enter(lane,Stage.Approach);
                return true;
            }
            return false;
        }

        internal void Advance(float dt)
        {
            bool open=false;
            foreach(var lane in lanes)
            {
                if(!lane.Animal)continue;
                AdvanceLane(lane,dt);
                open|=lane.Animal&&(lane.Stage==Stage.Enter||lane.Stage==Stage.Exit);
            }
            gateFraction=Mathf.MoveTowards(gateFraction,open?1:0,dt*3.8f);
            PoseGate();
        }

        void AdvanceLane(Lane lane,float dt)
        {
            var animal=lane.Animal;
            if(lane.Stage==Stage.Approach)
            {
                if(!lane.AtGate)lane.AtGate=animal.WalkToRideMark(Approach(lane.Side),transform.rotation,dt);
                if(!lane.AtGate||!CanEnter(lane))return;
                animal.BeginRideActivity();
                Enter(lane,Stage.Enter);
                return;
            }

            lane.Elapsed+=dt;
            if(lane.Stage==Stage.Lap)
            {
                SkateLap(lane);
                if(lane.Elapsed>=LapSeconds)Enter(lane,Stage.Exit);
                return;
            }

            bool entering=lane.Stage==Stage.Enter;
            float progress=Mathf.Clamp01(lane.Elapsed/AccessSeconds);
            float access=entering?progress:1-progress;
            var point=AccessPoint(lane.Side,access);
            if(point.z<HalfDepth-.10f)
            {
                animal.Character.WearSkates(skateTemplate);
                var nearby=AccessPoint(lane.Side,Mathf.Clamp01(access+(entering?.012f:-.012f)));
                var heading=nearby-point;
                if(heading.sqrMagnitude>.000001f)
                {
                    var facing=transform.rotation*Quaternion.LookRotation(-heading);
                    animal.transform.rotation=Quaternion.RotateTowards(animal.transform.rotation,facing,dt*330);
                }
                PutOnIce(animal,point,0);
                animal.Character.PlaySkatingPose(Mathf.Sin(lane.Elapsed*5.6f)*.65f,0,0);
            }
            else
            {
                animal.Character.RemoveSkates();
                point.y=GroundHeight;
                animal.WalkAtRideStation(transform.TransformPoint(point));
            }

            if(progress<1)return;
            if(entering)Enter(lane,Stage.Lap);
            else Finish(lane);
        }

        bool CanEnter(Lane lane)
        {
            var other=lanes[lane.Side<0?1:0];
            if(!other.Animal||other.Stage is Stage.Approach or Stage.Exit)return true;
            // Start the second skater a quarter circuit apart so they take turns at the crossing.
            float sinceEntry=other.Elapsed+(other.Stage==Stage.Lap?AccessSeconds:0);
            float phase=Mathf.Repeat(sinceEntry,CircuitSeconds*.5f);
            return Mathf.Abs(phase-CircuitSeconds*.25f)<.36f;
        }

        void SkateLap(Lane lane)
        {
            float time=Mathf.Min(lane.Elapsed,LapSeconds);
            float angle=StartAngle(lane.Side)+time/CircuitSeconds*2*Mathf.PI;
            var point=LapPoint(angle);
            var direction=new Vector3(CircuitRadiusX*Mathf.Cos(angle),0,2*CircuitRadiusZ*Mathf.Cos(2*angle));
            float jump=Mathf.Clamp01((time-JumpStart)/JumpSeconds);
            float airborne=4*jump*(1-jump);
            float rotation=Mathf.SmoothStep(0,AxelTurns*360,jump);
            // An Axel takes off forwards and lands backwards. Stay backwards before the step turn.
            float sinceLanding=time-JumpStart-JumpSeconds;
            if(sinceLanding>0)rotation=180*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.15f,2.05f,sinceLanding)));
            var animal=lane.Animal;
            PutOnIce(animal,point,airborne*JumpHeight);
            animal.transform.rotation=transform.rotation*Quaternion.LookRotation(-direction)*Quaternion.Euler(0,rotation,0);
            float tuck=Mathf.SmoothStep(0,1,airborne);
            float landing=sinceLanding>=0?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.72f,sinceLanding)):0;
            float takeoff=Mathf.SmoothStep(0,1,Mathf.InverseLerp(JumpStart-.62f,JumpStart,time))*(1-tuck);
            if(sinceLanding>=0)takeoff=0;
            float push=Mathf.Lerp(Mathf.Lerp(Mathf.Sin(time*5.4f),-.95f,takeoff),.94f,landing);
            animal.Character.PlaySkatingPose(push,tuck,landing);
        }

        void PutOnIce(ParkRabbitAgent animal,Vector3 point,float lift)
        {
            point.y=IceY+animal.transform.lossyScale.y*RabbitCharacter.SkateBladeDepth+lift;
            animal.transform.position=transform.TransformPoint(point);
        }

        static float StartAngle(int side)=>(side<0?1.25f:.25f)*Mathf.PI;

        static Vector3 LapPoint(float angle) =>
            new Vector3(CircuitRadiusX*Mathf.Sin(angle),0,CircuitRadiusZ*Mathf.Sin(2*angle));

        static Vector3 AccessPoint(int side,float t)
        {
            var start=new Vector3(side*.85f,0,HalfDepth+1.08f);
            var end=LapPoint(StartAngle(side));
            var lead=new Vector3(side*.85f,0,Mathf.Lerp(end.z,HalfDepth,.20f));
            var merge=new Vector3(end.x*.65f,0,end.z);
            return Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(start,lead,t),Vector3.Lerp(lead,merge,t),t),
                Vector3.Lerp(Vector3.Lerp(lead,merge,t),Vector3.Lerp(merge,end,t),t),t);
        }

        void PoseGate()
        {
            float turn=Mathf.SmoothStep(0,96,gateFraction);
            if(leftGate)leftGate.localRotation=Quaternion.Euler(0,turn,0);
            if(rightGate)rightGate.localRotation=Quaternion.Euler(0,-turn,0);
        }

        static void Enter(Lane lane,Stage stage)
        {
            lane.Stage=stage;
            lane.Elapsed=0;
            lane.AtGate=false;
        }

        void Finish(Lane lane)
        {
            var animal=lane.Animal;
            lane.Animal=null;
            animal.Character.RemoveSkates();
            animal.transform.position=Approach(lane.Side);
            animal.EndRide();
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            foreach(var lane in lanes)
            {
                if(lane.Animal!=animal||lane.Stage!=Stage.Approach)continue;
                lane.Animal=null;
                return true;
            }
            return false;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            foreach(var lane in lanes)
            {
                if(lane.Animal!=animal)continue;
                lane.Animal=null;
                if(animal.Character)animal.Character.RemoveSkates();
            }
        }

        void OnDisable()
        {
            foreach(var lane in lanes)
                if(lane.Animal)Finish(lane);
            gateFraction=0;
            PoseGate();
        }
    }
}
