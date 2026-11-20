using UnityEngine;

namespace PeacePark
{
    public sealed class ParkSeesaw : MonoBehaviour, IParkAnimalRide
    {
        public Transform Beam;
        const float TransferSeconds=1.55f,RockPeriod=2.8f,RockSeconds=RockPeriod*3;
        readonly ParkRabbitAgent[] riders=new ParkRabbitAgent[2];
        readonly bool[] ready=new bool[2];
        enum Stage { Approaching, Mounting, Rocking, Dismounting }
        Stage stage;
        float elapsed;
        ParkAnimalAction IParkAnimalRide.Action=>ParkAnimalAction.Seesaw;
        static int Side(int seat)=>seat==0?-1:1;
        Vector3 Mark(int seat)=>transform.TransformPoint(new Vector3(Side(seat)*1.98f,0,-2f));
        Quaternion Facing(int seat)=>Quaternion.LookRotation(Side(seat)*transform.right);

        internal bool TryPair(ParkPlayground park,ParkRabbitAgent first)
        {
            if(!isActiveAndEnabled||!Beam||riders[0]||!first.CanJoinSeesaw||!park.CanFollowAnimal(first))return false;
            int seat=transform.InverseTransformPoint(first.transform.position).x<0?0:1;
            foreach(var other in park.Animals)
            {
                if(other==first||!other.CanJoinSeesaw||!park.CanFollowAnimal(other))continue;
                if(!park.TryBuildRidePath(first.transform.position,Mark(seat),out var route))return false;
                if(!park.TryBuildRidePath(other.transform.position,Mark(1-seat),out var partnerRoute))continue;
                first.ReserveRide(this,route);
                other.ReserveRide(this,partnerRoute);
                riders[seat]=first;
                riders[1-seat]=other;
                stage=Stage.Approaching;
                elapsed=0;
                return true;
            }
            return false;
        }

        internal void Advance(float dt)
        {
            if(!riders[0])return;
            if(stage==Stage.Approaching)
            {
                for(int i=0;i<2;i++)
                    if(!ready[i])ready[i]=riders[i].WalkToRideMark(Mark(i),Facing(i),dt);
                if(ready[0]&&ready[1])stage=Stage.Mounting;
                return;
            }
            elapsed+=dt;
            float seated=1;
            if(stage==Stage.Rocking)
            {
                float t=Mathf.Min(elapsed,RockSeconds);
                float ease=Mathf.SmoothStep(0,1,Mathf.Min(t,RockSeconds-t)/.6f);
                Beam.localRotation=Quaternion.AngleAxis(13f*ease*Mathf.Sin(2*Mathf.PI*t/RockPeriod),Vector3.forward);
            }
            else
            {
                seated=Mathf.SmoothStep(0,1,elapsed/TransferSeconds);
                if(stage==Stage.Dismounting)seated=1-seated;
            }
            for(int i=0;i<2;i++)PlaceRider(i,seated);
            if(elapsed<(stage==Stage.Rocking?RockSeconds:TransferSeconds))return;
            elapsed=0;
            if(stage==Stage.Mounting)stage=Stage.Rocking;
            else if(stage==Stage.Rocking)stage=Stage.Dismounting;
            else ReleasePair();
        }

        void PlaceRider(int i,float seated)
        {
            var animal=riders[i];
            int side=Side(i);
            animal.transform.rotation=Quaternion.LookRotation(side*Beam.right,Beam.up);
            var hipOffset=animal.Character.PlayCarouselSeatPose(seated);
            var seat=Beam.TransformPoint(new Vector3(side*1.98f,.28f,0));
            animal.transform.position=Vector3.Lerp(Mark(i),seat-hipOffset,seated)+Vector3.up*(.34f*4*seated*(1-seated));
            animal.Character.GripCarouselHandle(Beam.TransformPoint(new Vector3(side*1.41f,.58f,0)),seated);
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if(stage!=Stage.Approaching||(riders[0]!=animal&&riders[1]!=animal))return false;
            ReleasePair();
            return true;
        }
        void IParkAnimalRide.Release(ParkRabbitAgent animal)=>ReleasePair(animal);
        void OnDisable()=>ReleasePair();
        void ReleasePair(ParkRabbitAgent removed=null)
        {
            if(Beam)Beam.localRotation=Quaternion.identity;
            for(int i=0;i<2;i++)
            {
                var animal=riders[i];
                riders[i]=null;
                ready[i]=false;
                if(!animal||animal==removed)continue;
                if(stage!=Stage.Approaching)animal.transform.position=Mark(i);
                animal.EndRide();
            }
            stage=Stage.Approaching;
        }
    }
}
