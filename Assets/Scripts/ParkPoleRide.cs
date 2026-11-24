using UnityEngine;

namespace PeacePark
{
    public sealed class ParkPoleRide : MonoBehaviour, IParkAnimalRide
    {
        public const int LadderRungs=12;
        internal const float ArrivalWalkSeconds=1.30f;
        const float ClimbSeconds=5.20f,DeckWalkSeconds=1.25f;
        const float MountSeconds=.85f,DescentSeconds=2.45f,ReleaseSeconds=.40f,ExitWalkSeconds=1.15f;
        internal const float SlideSequenceSeconds=MountSeconds+DescentSeconds+ReleaseSeconds+ExitWalkSeconds;
        const float PoleReach=.42f,LadderStandOff=.24f;
        [SerializeField] ParkHouse entrance;
        [SerializeField] Transform pole;
        [SerializeField] Vector3 waveSpot,ladderFoot,ladderTop;
        ParkRabbitAgent rider;
        bool climbing;
        float elapsed;

        ParkAnimalAction IParkAnimalRide.Action=>ParkAnimalAction.Pole;
        internal bool Available=>!rider;
        internal Transform Pole=>pole;
        internal Vector3 WaveSpot=>transform.TransformPoint(waveSpot);
        internal Vector3 LadderApproach=>transform.TransformPoint(ladderFoot+Vector3.right*.78f);
        Quaternion LadderFacing=>Quaternion.LookRotation(transform.right);

        public void Configure(ParkHouse house,Transform axis,Vector3 greeting,Vector3 foot,Vector3 top)
        {
            entrance=house;
            pole=axis;
            waveSpot=greeting;
            ladderFoot=foot;
            ladderTop=top;
        }

        internal bool TryQueue(ParkPlayground park,ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled||!Available||entrance.IsBusy||park.HasWaitingArrival(entrance)||
                !animal.TryReserveRequestedRide(this,LadderApproach))return false;
            rider=animal;
            climbing=false;
            elapsed=0;
            return true;
        }

        internal void Advance(float dt)
        {
            if(!rider)return;
            if(!climbing)
            {
                if(!rider.WalkToRideMark(LadderApproach,LadderFacing,dt,automaticSpeed:5.6f))return;
                climbing=true;
                rider.BeginRideActivity();
                return;
            }
            elapsed+=dt;
            if(elapsed<ClimbSeconds)Climb(elapsed/ClimbSeconds);
            else if(elapsed<ClimbSeconds+DeckWalkSeconds)
            {
                var entry=transform.TransformPoint(ladderTop+Vector3.right*LadderStandOff);
                rider.WalkAtRideStation(Vector3.Lerp(entry,WaveSpot,(elapsed-ClimbSeconds)/DeckWalkSeconds));
            }
            else
            {
                float slideTime=elapsed-ClimbSeconds-DeckWalkSeconds;
                AdvanceSlide(rider,slideTime);
                if(slideTime>=SlideSequenceSeconds)
                {
                    var finished=rider;
                    rider=null;
                    finished.EndRide();
                }
            }
        }

        void Climb(float progress)
        {
            var rail=Vector3.Lerp(ladderFoot,ladderTop,progress);
            float step=progress*(LadderRungs+1);
            float approach=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(step));
            var feet=rail+Vector3.right*(LadderStandOff+.54f*approach);
            rider.transform.SetPositionAndRotation(transform.TransformPoint(feet),LadderFacing);
            float stride=Mathf.Sin(step*Mathf.PI);
            var leftGrip=GripPoint(2.7f+.58f*stride,.24f);
            var rightGrip=GripPoint(2.7f-.58f*stride,-.24f);
            float strength=(1-approach)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.92f,1,progress)));
            rider.Character.PlayLadderPose(leftGrip,rightGrip,stride,strength);

            Vector3 GripPoint(float rise,float side)
            {
                float at=Mathf.Clamp01((step+rise)/(LadderRungs+1));
                return transform.TransformPoint(Vector3.Lerp(ladderFoot,ladderTop,at)+Vector3.forward*side);
            }
        }

        internal void AdvanceSlide(ParkRabbitAgent animal,float seconds)
        {
            var axis=pole.position;
            var baseFeet=axis+transform.forward*PoleReach;
            var topFeet=new Vector3(baseFeet.x,WaveSpot.y,baseFeet.z);
            if(seconds<MountSeconds)
            {
                float t=seconds/MountSeconds;
                var edge=topFeet+transform.forward*.48f;
                if(t<.60f)animal.WalkAtRideStation(Vector3.Lerp(WaveSpot,edge,t/.60f));
                else
                {
                    float grip=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,1,t));
                    animal.transform.SetPositionAndRotation(Vector3.Lerp(edge,topFeet,grip),entrance.Facing);
                    animal.Character.PlayPolePose(axis,grip);
                }
                return;
            }
            float descentTime=seconds-MountSeconds;
            if(descentTime<DescentSeconds)
            {
                float t=descentTime/DescentSeconds;
                float descent=t-.115f*Mathf.Sin(2*Mathf.PI*t);
                var turn=Quaternion.AngleAxis(Mathf.SmoothStep(0,360,t),Vector3.up);
                var position=axis+turn*(baseFeet-axis);
                position.y=Mathf.Lerp(topFeet.y,baseFeet.y,descent);
                animal.transform.SetPositionAndRotation(position,turn*entrance.Facing);
                animal.Character.PlayPolePose(axis,1);
                return;
            }

            float releaseTime=descentTime-DescentSeconds;
            var stepAside=baseFeet-transform.right*.51f;
            var exit=entrance.Welcome.position;
            var facing=Quaternion.LookRotation(Vector3.ProjectOnPlane(stepAside-exit,Vector3.up));
            if(releaseTime<ReleaseSeconds)
            {
                float release=Mathf.SmoothStep(0,1,releaseTime/ReleaseSeconds);
                animal.transform.SetPositionAndRotation(Vector3.Lerp(baseFeet,stepAside,release),
                    Quaternion.Slerp(entrance.Facing,facing,release));
                animal.Character.PlayPolePose(axis,1-release);
            }
            else animal.WalkAtRideStation(Vector3.Lerp(stepAside,exit,(releaseTime-ReleaseSeconds)/ExitWalkSeconds));
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if(rider!=animal||climbing)return false;
            rider=null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if(rider==animal)rider=null;
        }

        void OnDisable()
        {
            if(!rider)return;
            if(climbing)rider.transform.position=entrance.Welcome.position;
            rider.EndRide();
            rider=null;
        }
    }
}
