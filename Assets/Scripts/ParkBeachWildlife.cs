using UnityEngine;

namespace PeacePark
{
    public sealed class ParkBeachWildlife : MonoBehaviour
    {
        public bool IsFish;
        public Vector3 Center;
        public Vector2 Radius;
        public float Speed=.3f;
        public float Phase;
        public Transform Tail;
        public Transform[] Legs;
        public Vector3[] ShorePath;
        public Vector3[] ShoreNormals;
        float elapsed;

        void OnEnable(){elapsed=0;Pose(0);}
        void Update(){elapsed+=Time.deltaTime;Pose(elapsed);}

        public void Pose(float seconds)
        {
            float time=seconds*Speed+Phase;
            if(IsFish)
            {
                var offset=new Vector3(Mathf.Sin(time)*Radius.x,.065f*Mathf.Sin(time*2.7f),Mathf.Cos(time)*Radius.y);
                var heading=new Vector3(Mathf.Cos(time)*Radius.x,0,-Mathf.Sin(time)*Radius.y);
                transform.SetPositionAndRotation(Center+offset,Quaternion.LookRotation(heading));
                if(Tail)Tail.localRotation=Quaternion.Euler(0,Mathf.Sin(seconds*7.5f+Phase)*24,0);
                return;
            }
            // Ease to a short stop at each end of a sideways scuttle.
            float travel=Mathf.Sin(time);
            if(ShorePath!=null&&ShorePath.Length>1)
            {
                float at=(travel*.5f+.5f)*(ShorePath.Length-1);
                int segment=Mathf.Min((int)at,ShorePath.Length-2);
                var tangent=ShorePath[segment+1]-ShorePath[segment];
                transform.position=Vector3.Lerp(ShorePath[segment],ShorePath[segment+1],at-segment);
                var normal=ShoreNormals!=null&&ShoreNormals.Length==ShorePath.Length
                    ?Vector3.Slerp(ShoreNormals[segment],ShoreNormals[segment+1],at-segment):Vector3.up;
                var forward=Vector3.ProjectOnPlane(new Vector3(-tangent.z,0,tangent.x),normal);
                transform.rotation=Quaternion.LookRotation(forward,normal);
            }
            else
            {
                float x=Center.x+Radius.x*travel;
                float shore=-7-.014f*x*x+.25f*Mathf.Sin(x*.4f);
                float bank=Center.z+Radius.y*Mathf.Sin(time*2);
                float y=Mathf.SmoothStep(0,.18f,Mathf.Clamp01(bank/2.2f));
                transform.position=new Vector3(x,y,shore+bank);
                transform.rotation=Quaternion.Euler(0,-Mathf.Atan(-.028f*x+.1f*Mathf.Cos(x*.4f))*Mathf.Rad2Deg,0);
            }
            float pace=Mathf.Abs(Mathf.Cos(time));
            if(Legs==null)return;
            for(int i=0;i<Legs.Length;i++)
            {
                float stride=Mathf.Sin(seconds*14+Phase+i*Mathf.PI*.78f)*pace;
                int side=i<Legs.Length/2?-1:1;
                Legs[i].localRotation=Quaternion.Euler(stride*15,0,side*stride*12);
            }
        }
    }
}
