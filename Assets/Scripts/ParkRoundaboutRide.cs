using UnityEngine;

namespace PeacePark
{
    public sealed class ParkRoundaboutRide : MonoBehaviour, IParkAnimalRide
    {
        const float BoardingDistance=3.78f,DeckEdgeInset=.36f,StandBack=.49f;
        const float HopSeconds=1.12f,DeckWalkSeconds=1.05f,GripSeconds=.46f,RideSeconds=14f,TurnSeconds=.65f;
        [SerializeField] ParkRoundaboutSpin spin;
        [SerializeField] Transform[] handles;
        [SerializeField] float deckRadius,deckHeight;
        ParkRabbitAgent rider;
        Transform handle;
        float elapsed,alignmentDegrees,alignmentSpeed;
        enum Stage { Approach, Align, Board, Ride, TurnToExit, Exit }
        Stage stage;

        ParkAnimalAction IParkAnimalRide.Action=>ParkAnimalAction.Roundabout;
        Vector3 BoardingMark=>transform.TransformPoint(Vector3.back*BoardingDistance);
        Vector3 DeckEntry=>transform.TransformPoint(Vector3.back*(deckRadius-DeckEdgeInset)+Vector3.up*deckHeight);
        Quaternion Inward=>Quaternion.LookRotation(-transform.forward,transform.up);
        Quaternion Outward=>Quaternion.LookRotation(transform.forward,transform.up);
        Vector3 Radial=>Vector3.ProjectOnPlane(handle.position-spin.transform.position,transform.up).normalized;
        Vector3 DeckMark=>transform.position+transform.up*deckHeight+
            Vector3.ProjectOnPlane(handle.position-transform.position,transform.up)+Radial*StandBack;
        Vector3 GripPoint=>handle.position+Radial*.045f;

        public void Configure(ParkRoundaboutSpin platform,Transform[] rails,float radius,float height)
        {
            spin=platform;
            handles=rails;
            deckRadius=radius;
            deckHeight=height;
            spin.DriveExternally();
        }

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled||rider||!spin||handles==null||handles.Length==0||
                !animal.TryReserveRequestedRide(this,BoardingMark))return false;
            rider=animal;
            Enter(Stage.Approach);
            return true;
        }

        internal void Advance(float dt)
        {
            if(!rider)
            {
                spin.Advance(dt*ParkRoundaboutSpin.DegreesPerSecond);
                return;
            }

            float previous=elapsed;
            elapsed+=dt;
            switch(stage)
            {
                case Stage.Approach:
                    spin.Advance(dt*ParkRoundaboutSpin.DegreesPerSecond);
                    if(rider.WalkToRideMark(BoardingMark,Inward,dt))ChooseHandle();
                    break;
                case Stage.Align:
                    AdvanceAlignment(dt);
                    break;
                case Stage.Board:
                    AdvanceBoarding();
                    break;
                case Stage.Ride:
                    float start=LapDegrees(previous);
                    float finish=LapDegrees(elapsed);
                    spin.Advance(finish-start);
                    float t=Mathf.Clamp01(elapsed/RideSeconds);
                    HoldRail(1,Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*Mathf.PI*6));
                    if(elapsed>=RideSeconds)Enter(Stage.TurnToExit);
                    break;
                case Stage.TurnToExit:
                    TurnToExit(Mathf.Clamp01(elapsed/TurnSeconds));
                    if(elapsed>=TurnSeconds)Enter(Stage.Exit);
                    break;
                case Stage.Exit:
                    AdvanceExit();
                    break;
            }
        }

        static float LapDegrees(float time)=>Mathf.SmoothStep(0,360,time/RideSeconds);

        void Enter(Stage next)
        {
            stage=next;
            elapsed=0;
        }

        void ChooseHandle()
        {
            alignmentDegrees=360;
            foreach(var candidate in handles)
            {
                var radial=Vector3.ProjectOnPlane(candidate.position-spin.transform.position,transform.up);
                float angle=Vector3.SignedAngle(radial,-transform.forward,transform.up);
                float forward=Mathf.Repeat(angle*spin.Direction,360);
                if(forward>=alignmentDegrees)continue;
                alignmentDegrees=forward;
                handle=candidate;
            }
            alignmentSpeed=ParkRoundaboutSpin.DegreesPerSecond;
            Enter(Stage.Align);
        }

        void AdvanceAlignment(float dt)
        {
            float targetSpeed=Mathf.Lerp(5,32,Mathf.Sqrt(Mathf.Clamp01(alignmentDegrees/42f)));
            alignmentSpeed=Mathf.MoveTowards(alignmentSpeed,targetSpeed,38*dt);
            float degrees=Mathf.Min(alignmentDegrees,alignmentSpeed*dt);
            spin.Advance(degrees);
            alignmentDegrees-=degrees;
            rider.Character.PlayRideQueuePose(elapsed,rider.Id);
            if(alignmentDegrees<=0)
            {
                rider.BeginRideActivity();
                Enter(Stage.Board);
            }
        }

        void HoldRail(float grip,float sway)
        {
            rider.transform.SetPositionAndRotation(DeckMark,Quaternion.LookRotation(Radial,transform.up));
            rider.Character.PlayRoundaboutPose(GripPoint,grip,sway);
        }

        void AdvanceBoarding()
        {
            const float gripStart=HopSeconds+DeckWalkSeconds;
            if(elapsed<HopSeconds)Hop(BoardingMark,DeckEntry,Inward,elapsed/HopSeconds);
            else if(elapsed<gripStart)
                rider.WalkAtRideStation(Vector3.Lerp(DeckEntry,DeckMark,(elapsed-HopSeconds)/DeckWalkSeconds));
            else HoldRail(Mathf.SmoothStep(0,1,(elapsed-gripStart)/GripSeconds),0);
            if(elapsed>=gripStart+GripSeconds)Enter(Stage.Ride);
        }

        void AdvanceExit()
        {
            if(elapsed<DeckWalkSeconds)
                rider.WalkAtRideStation(Vector3.Lerp(DeckMark,DeckEntry,elapsed/DeckWalkSeconds));
            else Hop(DeckEntry,BoardingMark,Outward,Mathf.Clamp01((elapsed-DeckWalkSeconds)/HopSeconds));
            if(elapsed>=DeckWalkSeconds+HopSeconds)ReleaseRider(returnToMark:true);
        }

        void TurnToExit(float progress)
        {
            const float release=.32f;
            if(progress<release)
            {
                HoldRail(1-Mathf.SmoothStep(0,1,progress/release),0);
                return;
            }
            float turn=Mathf.InverseLerp(release,1,progress);
            var facing=Quaternion.AngleAxis(Mathf.SmoothStep(0,180,turn),transform.up)*Inward;
            rider.transform.SetPositionAndRotation(DeckMark,facing);
            rider.Character.PlayRideStepPose(Mathf.Repeat(turn*2,1),turn>=.5f);
        }

        void Hop(Vector3 from,Vector3 to,Quaternion facing,float progress)
        {
            float airborne=Mathf.InverseLerp(.17f,.81f,progress);
            float travel=Mathf.SmoothStep(0,1,airborne);
            var point=Vector3.Lerp(from,to,travel);
            rider.transform.SetPositionAndRotation(point,facing);
            rider.Character.PlayHopPose(progress);
        }

        internal float SurfaceY(Vector3 position)
        {
            var flat=Vector3.ProjectOnPlane(position-transform.position,transform.up);
            return flat.sqrMagnitude<=deckRadius*deckRadius?transform.position.y+deckHeight:transform.position.y;
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if(rider!=animal||(stage!=Stage.Approach&&stage!=Stage.Align))return false;
            rider=null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if(rider==animal)rider=null;
        }

        void OnDisable()=>ReleaseRider(returnToMark:stage>=Stage.Board);

        void ReleaseRider(bool returnToMark)
        {
            if(!rider)return;
            var animal=rider;
            rider=null;
            if(returnToMark)animal.transform.SetPositionAndRotation(BoardingMark,Outward);
            animal.EndRide();
        }
    }
}
