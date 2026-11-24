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
        public ParkPoleRide PoleRide;
        public ParkTrampoline Trampoline;
        public ParkSeesaw Seesaw;
        public ParkRoundaboutRide Roundabout;
        public ParkElephantTrain Train;
        public ParkThemeParkWheel Wheel;
        public ParkCarouselRide Carousel;
        public ParkForestRide[] ForestRides;
        public ParkSandSled SandSled;
        public ParkDesertCamp DesertCamp;
        public ParkResortRiver ResortRiver;
        public ParkResortSwim ResortSwim;
        public ParkResortSlide ResortSlide;
        public ParkResortSunbeds ResortSunbeds;
        public ParkVillageDeer[] Deer;
        public ParkIceRink IceRink;
        public MeshFilter Path;
        public ParkDuneTerrain DuneTerrain;
        public float LawnHeight;
        public Rect WalkArea;
        public Rect PlazaWalkArea;
        public Vector2 PondCenter, PondRadii;
        public Rect[] WalkObstacles;
        public Vector3 Focus;
        public float Distance, Yaw, Pitch;
        public float MaxDistance;
        public float FarClipDistance;
        public Vector2 CameraPanLimits;
        public float CloseFocusHeight;
        public float FogEnd;
        public float FogStart;
        public float ShadowDistance;
        public Color Sky;
        public Texture2D Preview;
    }
}
