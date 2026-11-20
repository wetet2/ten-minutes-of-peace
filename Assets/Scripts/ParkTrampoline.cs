using UnityEngine;

namespace PeacePark
{
    public sealed class ParkTrampoline : MonoBehaviour, IParkAnimalRide
    {
        const float GateDistance=3.32f,MatHeight=.789f,WalkSeconds=2.65f;
        const float BounceSeconds=1.05f;
        const int Bounces=5;
        ParkRabbitAgent waiting,rider;
        bool atEntrance;
        float time;
        ParkAnimalAction IParkAnimalRide.Action=>ParkAnimalAction.Trampoline;
        Vector3 Entrance=>transform.TransformPoint(Vector3.back*GateDistance);
        Vector3 Exit=>transform.TransformPoint(Vector3.forward*GateDistance);
        internal float MatSurfaceY=>transform.TransformPoint(Vector3.up*MatHeight).y;

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled||waiting||!animal.TryReserveRequestedRide(this,Entrance))return false;
            waiting=animal;
            atEntrance=false;
            return true;
        }

        internal void Advance(float dt)
        {
            if(waiting&&!atEntrance)
                atEntrance=waiting.WalkToRideMark(Entrance,Quaternion.LookRotation(-transform.forward),dt);
            if(!rider)
            {
                if(!waiting||!atEntrance)return;
                rider=waiting;
                waiting=null;
                atEntrance=false;
                time=0;
            }
            time+=dt;
            const float endBounce=WalkSeconds+Bounces*BounceSeconds;
            if(time<WalkSeconds)
                WalkAcross(Mathf.Lerp(-GateDistance,0,time/WalkSeconds));
            else if(time<endBounce)
            {
                float cycle=(time-WalkSeconds)/BounceSeconds;
                float phase=cycle-Mathf.Floor(cycle);
                float jump=Mathf.Clamp01((phase-.17f)/.64f);
                rider.transform.position=transform.TransformPoint(Vector3.up*(MatHeight+.80f*4*jump*(1-jump)));
                rider.Character.PlayHopPose(phase);
            }
            else
            {
                WalkAcross(Mathf.Lerp(0,GateDistance,(time-endBounce)/WalkSeconds));
                if(time>=endBounce+WalkSeconds)
                {
                    rider.EndRide();
                    rider=null;
                }
            }
        }

        void WalkAcross(float z)
        {
            float distance=Mathf.Abs(z);
            float height=.32f*Mathf.InverseLerp(2.89f,2.60f,distance)
                +.205f*Mathf.InverseLerp(2.44f,2.18f,distance)
                +(MatHeight-.525f)*Mathf.InverseLerp(2.14f,1.86f,distance);
            rider.WalkAtRideStation(transform.TransformPoint(new Vector3(0,height,z)));
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if(waiting!=animal)return false;
            waiting=null;
            atEntrance=false;
            return true;
        }
        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if(rider==animal)rider=null;
            if(waiting==animal){waiting=null;atEntrance=false;}
        }
        void OnDisable()
        {
            if(rider){rider.transform.position=Exit;rider.EndRide();}
            if(waiting)waiting.EndRide();
            waiting=rider=null;
            atEntrance=false;
        }
    }
}
