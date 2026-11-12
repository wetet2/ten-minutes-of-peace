using UnityEngine;

namespace PeacePark
{
    // Expensive surface/shoulder construction is baked by the editor for players.
    [PreferBinarySerialization]
    public sealed class RabbitRigAsset : ScriptableObject
    {
        public Mesh Mesh;
        public int GeometryVersion;
        public int SkinVertexCount;
        public int[] SkinTriangles;
        public float IdleFloor;
        public float[] WalkFloor;
        public float WalkingFloor(float phase)
        {
            float sample=Mathf.Repeat(phase,Mathf.PI*2)/(Mathf.PI*2)*(WalkFloor.Length-1);
            int index=Mathf.Min(Mathf.FloorToInt(sample),WalkFloor.Length-2);
            return Mathf.Lerp(WalkFloor[index],WalkFloor[index+1],sample-index);
        }
    }
}
