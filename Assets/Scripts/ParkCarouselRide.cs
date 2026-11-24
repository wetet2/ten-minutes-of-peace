using UnityEngine;

namespace PeacePark
{
    public sealed class ParkCarouselRide : MonoBehaviour, IParkAnimalRide
    {
        static readonly Vector3 QueuePosition=new Vector3(0,.006f,-4.72f);
        static readonly Vector3 FirstStep=new Vector3(0,.17f,-4.30f);
        static readonly Vector3 SecondStep=new Vector3(0,.27f,-4.02f);
        const float TransferSeconds=4f;
        const float HorseStroke=.45f,HorseCycleDegrees=45f;

        [SerializeField] ParkRoundaboutSpin spin;
        [SerializeField] Transform[] saddles,horseLifts;
        [SerializeField] float deckHeight;
        ParkRabbitAgent rider;
        Transform saddle, grip;
        float remainingDegrees, transferTime, liftRotation;
        enum Stage { Approaching, Aligning, Mounting, Riding, Dismounting }
        Stage stage;

        ParkAnimalAction IParkAnimalRide.Action=>ParkAnimalAction.Carousel;
        Vector3 QueuePoint=>transform.TransformPoint(QueuePosition);
        Quaternion AtSteps=>Quaternion.LookRotation(-transform.forward);

        public void Configure(ParkRoundaboutSpin platform,Transform[] seats,float deck,Transform[] lifts=null)
        {
            spin=platform;
            saddles=seats;
            horseLifts=lifts;
            deckHeight=deck;
            spin.DriveExternally();
        }

        internal bool TryRequestedRide(ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled||rider||!spin||saddles==null||saddles.Length==0||
                !animal.TryReserveRequestedRide(this,QueuePoint))return false;
            rider=animal;
            stage=Stage.Approaching;
            return true;
        }

        void OnDisable()
        {
            if(!rider)return;
            if(stage!=Stage.Approaching)rider.transform.SetPositionAndRotation(QueuePoint,AtSteps);
            rider.EndRide();
            rider=null;
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if(animal!=rider||(stage!=Stage.Approaching&&stage!=Stage.Aligning))return false;
            rider=null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if(rider==animal)rider=null;
        }

        internal void Advance(float dt)
        {
            if(!spin)return;
            float degrees=dt*ParkRoundaboutSpin.DegreesPerSecond;
            if(!rider)
            {
                AdvancePlatform(degrees);
                return;
            }

            switch(stage)
            {
                case Stage.Approaching:
                    AdvancePlatform(degrees);
                    if(rider.WalkToRideMark(QueuePoint,AtSteps,dt))AlignWithSteps();
                    break;
                case Stage.Aligning:
                    if(Turn(degrees))
                    {
                        rider.BeginRideActivity();
                        stage=Stage.Mounting;
                        transferTime=0;
                    }
                    break;
                case Stage.Mounting:
                    transferTime+=dt;
                    Transfer(Mathf.Clamp01(transferTime/TransferSeconds));
                    if(transferTime>=TransferSeconds){stage=Stage.Riding;remainingDegrees=360;}
                    break;
                case Stage.Riding:
                    bool stopped=Turn(degrees);
                    SeatRider();
                    if(stopped){stage=Stage.Dismounting;transferTime=0;}
                    break;
                case Stage.Dismounting:
                    transferTime+=dt;
                    Transfer(1-Mathf.Clamp01(transferTime/TransferSeconds));
                    if(transferTime>=TransferSeconds)
                    {
                        rider.EndRide();
                        rider=null;
                    }
                    break;
            }
        }

        void AlignWithSteps()
        {
            remainingDegrees=360;
            foreach(var seat in saddles)
            {
                var radial=Vector3.ProjectOnPlane(seat.position-spin.transform.position,transform.up);
                float difference=Vector3.SignedAngle(radial,-transform.forward,transform.up);
                float forwardAngle=Mathf.Repeat(difference*spin.Direction,360);
                if(forwardAngle>=remainingDegrees)continue;
                remainingDegrees=forwardAngle;
                saddle=seat;
            }
            grip=saddle.Find("Saddle handle");
            stage=Stage.Aligning;
        }

        bool Turn(float degrees)
        {
            float step=Mathf.Min(degrees,remainingDegrees);
            AdvancePlatform(step);
            remainingDegrees-=step;
            return remainingDegrees<=0;
        }

        void AdvancePlatform(float degrees)
        {
            spin.Advance(degrees);
            if(horseLifts==null)return;
            liftRotation=Mathf.Repeat(liftRotation+degrees,HorseCycleDegrees);
            float phase=liftRotation/HorseCycleDegrees*(2*Mathf.PI);
            float rise=.5f-.5f*Mathf.Cos(phase);
            for(int i=0;i<horseLifts.Length;i++)
            {
                var lift=horseLifts[i];
                if(!lift)continue;
                var position=lift.localPosition;
                position.y=HorseStroke*(i%2==0?rise:1-rise);
                lift.localPosition=position;
            }
        }

        void Transfer(float progress)
        {
            var deckEntry=transform.TransformPoint(new Vector3(0,deckHeight,-3.44f));
            var besideHorse=saddle.position-transform.forward*.85f;
            besideHorse.y=deckEntry.y;
            if(progress>=.71f)
            {
                float mount=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.71f,1,progress));
                rider.transform.rotation=Quaternion.Slerp(AtSteps,saddle.rotation,mount);
                var hipOffset=rider.Character.PlayStraddleRidePose(mount);
                rider.transform.position=Vector3.Lerp(besideHorse,saddle.position-hipOffset,mount);
                rider.Character.GripRideHandle(grip.position,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,mount)));
                return;
            }
            Vector3 point=progress<.24f
                ? Vector3.Lerp(QueuePoint,transform.TransformPoint(FirstStep),progress/.24f)
                : progress<.40f
                    ? Vector3.Lerp(transform.TransformPoint(FirstStep),transform.TransformPoint(SecondStep),(progress-.24f)/.16f)
                    : progress<.60f
                        ? Vector3.Lerp(transform.TransformPoint(SecondStep),deckEntry,(progress-.40f)/.20f)
                        : Vector3.Lerp(deckEntry,besideHorse,(progress-.60f)/.11f);
            rider.WalkAtRideStation(point);
        }

        void SeatRider()
        {
            rider.transform.rotation=saddle.rotation;
            var hipOffset=rider.Character.PlayStraddleRidePose(1);
            rider.transform.position=saddle.position-hipOffset;
            rider.Character.GripRideHandle(grip.position,1);
        }
    }
}
