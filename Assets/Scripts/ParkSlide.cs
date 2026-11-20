using UnityEngine;

namespace PeacePark
{
    public sealed class ParkSlide : MonoBehaviour
    {
        public Vector3[] Steps;
        public Vector3[] Chute;
        public Vector3[] Runout;
        public ParkHouse Entrance;
        ParkRabbitAgent rider;

        internal const float EntrySeconds=1.15f, DescentSeconds=3.55f, StandSeconds=.36f, ExitWalkSeconds=.78f;
        internal Vector3 Approach=>transform.TransformPoint(Steps[0]);
        internal Vector3 StairDirection=>Vector3.ProjectOnPlane(transform.TransformVector(Steps[1]-Steps[0]),Vector3.up).normalized;
        internal Quaternion StairFacing=>Quaternion.LookRotation(-StairDirection);
        public Vector3 Exit=>transform.TransformPoint(Runout[Runout.Length-1]);
        internal bool Available=>isActiveAndEnabled&&!rider&&!Entrance.IsBusy;

        internal bool Reserve(ParkRabbitAgent animal)
        {
            if(!Available)return false;
            rider=animal;
            return true;
        }

        internal void Release(ParkRabbitAgent animal)
        {
            if(rider==animal)rider=null;
        }

        internal Vector3 Climb(float progress)=>transform.TransformPoint(Sample(Steps,progress));
        internal Vector3 WalkOut(float progress)=>transform.TransformPoint(Sample(Runout,progress));

        internal void SlideDown(float progress,out Vector3 position,out Quaternion rotation)
        {
            float t=Mathf.Clamp01(progress);
            // Ease in at the top and brake before standing up.
            float along=t-.12f*Mathf.Sin(2*Mathf.PI*t);
            position=transform.TransformPoint(Sample(Chute,along));
            var heading=transform.TransformVector(Sample(Chute,along+.006f)-Sample(Chute,along-.006f));
            heading.y=0;
            rotation=Quaternion.LookRotation(-heading);
        }

        static Vector3 Sample(Vector3[] route,float progress)
        {
            float index=Mathf.Clamp01(progress)*(route.Length-1);
            int leg=Mathf.Min((int)index,route.Length-2);
            return Vector3.Lerp(route[leg],route[leg+1],index-leg);
        }
    }
}
