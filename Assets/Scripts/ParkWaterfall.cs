using UnityEngine;

namespace PeacePark
{
    public sealed class ParkWaterfall : MonoBehaviour
    {
        public Renderer[] Streams;
        public ParticleSystem Spray;
        public ParticleSystem Mist;
        public float FlowTime {get;private set;}
        MaterialPropertyBlock properties;
        static readonly int FlowTimeId=Shader.PropertyToID("_FlowTime");

        void OnEnable(){SetFlowTime(0);}
        void Update(){SetFlowTime(FlowTime+Time.deltaTime);}
        public void SetFlowTime(float seconds)
        {
            FlowTime=seconds;
            if(Streams==null)return;
            properties??=new MaterialPropertyBlock();
            properties.SetFloat(FlowTimeId,seconds);
            foreach(var stream in Streams)if(stream)stream.SetPropertyBlock(properties);
        }
    }
}
