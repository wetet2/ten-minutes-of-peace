using UnityEngine;

namespace PeacePark
{
    public sealed class ParkHouse : MonoBehaviour
    {
        public string DisplayName="꽃집";
        public Transform DoorHinge;
        public Transform Inside;
        public Transform Threshold;
        public Transform Welcome;
        public float DoorDuration=.7f;
        public float OpenAngle=110;
        public bool HasRoundedPorch;
        public static float PorchHeight(float localZ)=>.30f+(.025f-.30f)*((-localZ-2.25f)/1.45f);
        public float DoorFraction {get;private set;}
        public bool IsBusy {get;private set;}
        public int OpenCount {get;private set;}
        public int CompletedExits {get;private set;}
        bool opening;

        public bool TryReserve()
        {
            if(IsBusy)return false;
            IsBusy=true;opening=true;OpenCount++;return true;
        }
        public void CloseAfterExit(){opening=false;CompletedExits++;}
        public void Advance(float deltaTime)
        {
            DoorFraction=Mathf.MoveTowards(DoorFraction,opening?1:0,deltaTime/Mathf.Max(.1f,DoorDuration));
            if(DoorHinge)DoorHinge.localRotation=Quaternion.Euler(0,Mathf.SmoothStep(0,1,DoorFraction)*OpenAngle,0);
            if(!opening&&DoorFraction<=0)IsBusy=false;
        }
        public Vector3 ExitPosition(float progress)
        {
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress));
            var position=t<.55f?Vector3.Lerp(Inside.position,Threshold.position,t/.55f):Vector3.Lerp(Threshold.position,Welcome.position,(t-.55f)/.45f);
            if(HasRoundedPorch)
            {
                var local=transform.InverseTransformPoint(position);
                local.y=local.z> -2.25f?.30f:local.z< -3.7f?.025f:PorchHeight(local.z);
                position=transform.TransformPoint(local);
            }
            return position;
        }
        public Quaternion Facing=>Quaternion.LookRotation(transform.forward,Vector3.up);
    }
}
