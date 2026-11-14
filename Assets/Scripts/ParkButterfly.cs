using UnityEngine;

namespace PeacePark
{
    public sealed class ParkButterfly : MonoBehaviour
    {
        [SerializeField] Transform leftWing, rightWing;
        [SerializeField] Vector3 center;
        [SerializeField] Vector2 loopSize;
        [SerializeField] float speed, phase;
        float elapsed;

        public void Configure(Transform left, Transform right, Vector3 origin, Vector2 size, float rate, float offset)
        {
            leftWing=left;rightWing=right;center=origin;loopSize=size;speed=rate;phase=offset;
            Pose(0);
        }

        void Update()
        {
            elapsed+=Time.deltaTime;
            Pose(elapsed);
        }

        void Pose(float time)
        {
            float angle=phase+time*speed;
            float sway=angle*2+.6f;
            transform.localPosition=center+new Vector3(
                loopSize.x*(Mathf.Cos(angle)+.18f*Mathf.Sin(sway)),
                .24f*Mathf.Sin(sway)+.10f*Mathf.Sin(angle*3),
                loopSize.y*Mathf.Sin(angle));
            var heading=new Vector3(loopSize.x*(-Mathf.Sin(angle)+.36f*Mathf.Cos(sway)),
                .48f*Mathf.Cos(sway)+.30f*Mathf.Cos(angle*3),loopSize.y*Mathf.Cos(angle));
            transform.localRotation=Quaternion.LookRotation(heading)*
                Quaternion.Euler(0,0,11*Mathf.Sin(angle));
            float flutter=12+47*Mathf.Sin(time*(34+speed*5)+phase);
            leftWing.localRotation=Quaternion.Euler(0,0,-flutter);
            rightWing.localRotation=Quaternion.Euler(0,0,flutter);
        }
    }
}
