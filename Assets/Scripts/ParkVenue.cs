using System;
using UnityEngine;

namespace PeacePark
{
    [Serializable]
    public sealed class ParkVenue
    {
        public string Name;
        public GameObject Root;
        public ParkHouse[] Houses;
        public ParkFountain Fountain;
        public ParkSlide Slide;
        public MeshFilter Path;
        public ParkDuneTerrain DuneTerrain;
        public float LawnHeight;
        public Rect WalkArea;
        public Vector2 PondCenter, PondRadii;
        public Rect[] WalkObstacles;
        public Vector3 Focus;
        public float Distance, Yaw, Pitch;
        public float MaxDistance;
        public Vector2 CameraPanLimits;
        public float CloseFocusHeight;
        public float FogEnd;
        public float FogStart;
        public Color Sky;
        public Texture2D Preview;
    }
}
