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
        public Vector2[] ExitSlope;
        public static float PorchHeight(float localZ)=>.30f+(.025f-.30f)*((-localZ-2.25f)/1.45f);
        public float DoorFraction {get;private set;}
        public bool IsBusy {get;private set;}
        public int OpenCount {get;private set;}
        public int CompletedExits {get;private set;}
        bool opening;
        internal int LastDepartureDirection {get;set;}=-1;

        public bool TryReserve()
        {
            if(IsBusy)return false;
            IsBusy=true;opening=true;OpenCount++;return true;
        }
        public void CloseAfterExit(){opening=false;CompletedExits++;}
        internal void ContinueFrom(ParkHouse previous)
        {
            opening=previous&&previous.opening;IsBusy=previous&&previous.IsBusy;DoorFraction=previous?previous.DoorFraction:0;
            if(DoorHinge)DoorHinge.localRotation=Quaternion.Euler(0,Mathf.SmoothStep(0,1,DoorFraction)*OpenAngle,0);
        }
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
                if(local.z> -2.25f)local.y=.30f;
                else if(local.z>= -3.7f)local.y=PorchHeight(local.z);
                else local.y=Mathf.Lerp(.025f,Welcome.localPosition.y,
                    Mathf.InverseLerp(-3.7f,Welcome.localPosition.z,local.z));
                position=transform.TransformPoint(local);
            }
            else if(ExitSlope!=null&&ExitSlope.Length>1)
            {
                var local=transform.InverseTransformPoint(position);
                local.y=ExitSlope[0].y;
                for(int i=1;i<ExitSlope.Length;i++)
                {
                    var from=ExitSlope[i-1];var to=ExitSlope[i];
                    local.y=Mathf.Lerp(from.y,to.y,Mathf.InverseLerp(from.x,to.x,local.z));
                    if(local.z>=to.x)break;
                }
                position=transform.TransformPoint(local);
            }
            return position;
        }
        public Quaternion Facing=>Quaternion.LookRotation(transform.forward,Vector3.up);
    }
}
