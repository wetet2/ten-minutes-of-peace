using UnityEngine;

namespace PeacePark
{
    public sealed class ParkRoundaboutSpin : MonoBehaviour
    {
        internal const float DegreesPerSecond = 12f;
        public bool ReverseDirection;
        [SerializeField] bool externallyDriven;
        internal float Direction => ReverseDirection ? -1 : 1;

        void Update()
        {
            if(!externallyDriven)Advance(Time.deltaTime*DegreesPerSecond);
        }

        internal void DriveExternally()=>externallyDriven=true;
        internal void Advance(float degrees)=>transform.Rotate(Vector3.up,Direction*degrees,Space.Self);
    }
}
