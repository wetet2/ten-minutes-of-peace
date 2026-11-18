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
    }
}
