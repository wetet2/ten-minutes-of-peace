using System;
using System.Collections.Generic;
using UnityEngine;
using static PeacePark.ParkResortRiver;

namespace PeacePark
{
    public sealed class ParkResortSlide : MonoBehaviour, IParkAnimalRide
    {
        public static readonly Vector2 Tower = new Vector2(-41.6f, 4.8f);
        public const float TerraceWest = -52.2f, LaunchHeight = 11.7f, TrackHalfWidth = 1.08f;
        public const int Flights = 4;
        public static Vector3 TopLanding => new Vector3(Tower.x - 2.84f, LaunchHeight, StairEnd(Flights - 1).z);
        public static Vector3 Approach => new Vector3(Tower.x - 2.35f, DeckHeight, Tower.y + 3.75f);
        public static Vector3 Exit => new Vector3(PoolCenter.x - PoolHalf.x - 1.03f, DeckHeight, 7.17f);
        public static Vector3 Outlet => new Vector3(PoolCenter.x - PoolHalf.x + .98f, .205f, -5.69f);
        public static Vector3 ImpactPoint => new Vector3(Outlet.x + .73f, WaterHeight, Outlet.z + .12f);
        public static Vector3 StairStart(int flight) => new Vector3(Tower.x + (flight % 2 == 0 ? -2.30f : 2.30f),
            Mathf.Lerp(DeckHeight, LaunchHeight, flight / (float)Flights), Tower.y + (flight % 2 == 0 ? -.84f : .84f));
        public static Vector3 StairEnd(int flight)
        {
            var start = StairStart(flight);
            return new Vector3(2 * Tower.x - start.x, Mathf.Lerp(DeckHeight, LaunchHeight, (flight + 1f) / Flights), start.z);
        }

        const float EntryShare = .046f, CoilEnd = .80f, Radius = 5f, LowestCoil = 2.06f;
        const float ClimbSeconds = 10.3f, SitSeconds = .42f, SlideSeconds = 10.5f, SwimSeconds = 5.8f, ExitSeconds = 1.25f;
        const float ImpactDelay = .27f, RippleSeconds = 1.28f;
        const int CourseSamples = 320;
        static readonly Vector3 WaterLanding = ImpactPoint - Vector3.up * .075f;
        static readonly Vector3 Ladder = new Vector3(PoolCenter.x - PoolHalf.x + .66f, WaterLanding.y, Exit.z);
        static readonly Vector3[] stairs = BuildStairs();
        static readonly Vector3[] channel = BuildChannel();
        static readonly float[] stairDistances = Distances(stairs);
        static readonly float[] channelDistances = Distances(channel);

        [SerializeField] ParticleSystem splash;
        [SerializeField] MeshRenderer ripples;
        static readonly int RippleStrength = Shader.PropertyToID("_Strength");
        MaterialPropertyBlock rippleProperties;
        ParkRabbitAgent rider;
        enum Stage { Approach, Climb, Sit, Slide, Swim, Exit }
        Stage stage;
        float time;
        float rippleAge = RippleSeconds;
        Vector3 transitionRoot;
        Quaternion transitionFacing;

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.WaterSlide;
        internal bool IsSwimming(ParkRabbitAgent animal) => rider == animal && stage == Stage.Swim;
        internal bool IsSliding(ParkRabbitAgent animal) => rider == animal && stage == Stage.Slide;
        internal bool ShouldHideShadow(ParkRabbitAgent animal) => rider == animal && stage != Stage.Approach;
        public bool IsMovingPart(Transform part) => ripples && ripples.transform == part;
        public void Configure(ParticleSystem waterSplash, MeshRenderer surfaceRipples)
        {
            splash = waterSplash;
            ripples = surfaceRipples;
        }

        public static Vector3 TrackPoint(float progress)
        {
            if (progress <= EntryShare)
            {
                var start = new Vector3(Tower.x, LaunchHeight, Tower.y - 2.22f);
                var end = CoilPoint(0);
                return Bezier(progress / EntryShare, start, start + new Vector3(0, -.10f, -1.56f),
                    end + new Vector3(-1.12f, .17f, -.647f), end);
            }
            if (progress <= CoilEnd) return CoilPoint(Mathf.InverseLerp(EntryShare, CoilEnd, progress));
            var low = CoilPoint(1);
            return Bezier(Mathf.InverseLerp(CoilEnd, 1, progress), low, low + new Vector3(5.47f, -.82f, 0),
                Outlet + new Vector3(-4.5f, .17f, 0), Outlet);
        }

        static Vector3 CoilPoint(float progress)
        {
            float angle = (progress - 1) * (23f * Mathf.PI / 6f) - Mathf.PI * .5f;
            return new Vector3(Tower.x + Radius * Mathf.Cos(angle), Mathf.Lerp(LaunchHeight - .56f, LowestCoil, progress),
                Tower.y + Radius * Mathf.Sin(angle));
        }

        static Vector3 Bezier(float t, Vector3 start, Vector3 a, Vector3 b, Vector3 end)
        {
            float v = 1 - t;
            return v * v * v * start + 3 * v * v * t * a + 3 * v * t * t * b + t * t * t * end;
        }

        public static Vector3 TrackTangent(float progress) =>
            (TrackPoint(Mathf.Min(1, progress + .001f)) - TrackPoint(Mathf.Max(0, progress - .001f))).normalized;

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || rider || !animal.TryReserveRequestedRide(this, transform.TransformPoint(Approach))) return false;
            rider = animal;
            Begin(Stage.Approach);
            return true;
        }

        internal void Advance(float dt)
        {
            AdvanceRipples(dt);
            if (!rider) return;
            time += dt;
            switch (stage)
            {
                case Stage.Approach:
                    if (rider.WalkToRideMark(transform.TransformPoint(Approach), Facing(Vector3.back), dt))
                    {
                        rider.BeginRideActivity();
                        Begin(Stage.Climb);
                    }
                    break;
                case Stage.Climb:
                    var point = Sample(stairs, stairDistances, time / ClimbSeconds, out _);
                    rider.WalkAtRideStation(transform.TransformPoint(point), .86f);
                    if (time >= ClimbSeconds)
                    {
                        transitionFacing = rider.transform.rotation;
                        Begin(Stage.Sit);
                    }
                    break;
                case Stage.Sit:
                    float sitting = Mathf.SmoothStep(0, 1, time / SitSeconds);
                    rider.transform.rotation = Quaternion.Slerp(transitionFacing, Facing(TrackTangent(0)), sitting);
                    rider.Character.PlaySlidePose(sitting);
                    if (time >= SitSeconds) Begin(Stage.Slide);
                    break;
                case Stage.Slide:
                    float progress = Mathf.Clamp01(time / SlideSeconds);
                    float travel = progress * (.55f + .45f * progress);
                    var section = Sample(channel, channelDistances, travel, out var heading);
                    rider.transform.SetPositionAndRotation(transform.TransformPoint(section + Vector3.up * .024f), Facing(heading));
                    rider.Character.PlaySlidePose(1);
                    if (progress >= 1)
                    {
                        transitionRoot = rider.transform.position;
                        transitionFacing = rider.transform.rotation;
                        Begin(Stage.Swim);
                    }
                    break;
                case Stage.Swim:
                    SwimToLadder();
                    if (time >= ImpactDelay && time - dt < ImpactDelay) SplashOnEntry();
                    if (time >= SwimSeconds)
                    {
                        transitionRoot = rider.transform.position;
                        Begin(Stage.Exit);
                    }
                    break;
                case Stage.Exit:
                    float exit = Mathf.Clamp01(time / ExitSeconds);
                    var to = transform.TransformPoint(Exit);
                    var grip = transform.TransformPoint(new Vector3(Exit.x + .51f, DeckHeight + .12f, Exit.z));
                    var step = Vector3.Lerp(transitionRoot, grip, Mathf.SmoothStep(0, 1, exit / .62f));
                    rider.transform.rotation = Facing(Vector3.left);
                    if (exit <= .62f)
                    {
                        rider.transform.position = step;
                        rider.Character.PlaySwimmingPose(1 - Mathf.SmoothStep(0, 1, exit / .62f), time);
                    }
                    else rider.WalkAtRideStation(Vector3.Lerp(grip, to, Mathf.InverseLerp(.62f, 1, exit)));
                    if (exit >= 1) Finish();
                    break;
            }
        }

        void SwimToLadder()
        {
            float s = Mathf.Clamp01(time / SwimSeconds);
            float dive = Mathf.SmoothStep(0, 1, time / .56f);
            var a = WaterLanding + new Vector3(.48f, 0, 1.81f);
            var b = Ladder + new Vector3(.74f, 0, -1.52f);
            var point = Bezier(s, WaterLanding, a, b, Ladder);
            var ahead = Bezier(Mathf.Min(1, s + .015f), WaterLanding, a, b, Ladder);
            var behind = Bezier(Mathf.Max(0, s - .015f), WaterLanding, a, b, Ladder);
            var heading = ahead - behind;
            rider.transform.rotation = Quaternion.Slerp(transitionFacing, Facing(heading), dive);
            rider.Character.PlaySwimmingPose(dive, time);
            var chest = rider.Character.SwimChest - rider.transform.position;
            rider.transform.position = Vector3.Lerp(transitionRoot, transform.TransformPoint(point) - chest, dive);
        }

        void SplashOnEntry()
        {
            if (splash)
            {
                splash.Play(false);
                splash.Emit(68);
            }
            if (ripples)
            {
                rippleAge = 0;
                ripples.enabled = true;
                AdvanceRipples(0);
            }
        }

        void AdvanceRipples(float dt)
        {
            if (!ripples || rippleAge >= RippleSeconds) return;
            rippleAge += dt;
            float t = Mathf.Clamp01(rippleAge / RippleSeconds);
            float radius = Mathf.Lerp(.62f, 2.68f, 1 - (1 - t) * (1 - t));
            ripples.transform.localScale = new Vector3(radius, 1, radius * .86f);
            rippleProperties ??= new MaterialPropertyBlock();
            rippleProperties.SetFloat(RippleStrength, .73f * (1 - t));
            ripples.SetPropertyBlock(rippleProperties);
            ripples.enabled = t < 1;
        }

        static Vector3[] BuildStairs()
        {
            var path = new List<Vector3> { Approach, new Vector3(Approach.x, DeckHeight, StairStart(0).z) };
            for (int i = 0; i < Flights; i++)
            {
                path.Add(StairStart(i));
                path.Add(StairEnd(i));
            }
            path.Add(TopLanding);
            path.Add(new Vector3(TopLanding.x, LaunchHeight, Tower.y - .56f));
            path.Add(new Vector3(Tower.x - 2.53f, LaunchHeight, Tower.y - 1.08f));
            path.Add(new Vector3(Tower.x - .83f, LaunchHeight, Tower.y - 1.51f));
            path.Add(TrackPoint(0));
            return path.ToArray();
        }

        static Vector3[] BuildChannel()
        {
            var points = new Vector3[CourseSamples + 1];
            for (int i = 0; i <= CourseSamples; i++) points[i] = TrackPoint(i / (float)CourseSamples);
            return points;
        }

        static float[] Distances(Vector3[] path)
        {
            var lengths = new float[path.Length];
            for (int i = 1; i < path.Length; i++) lengths[i] = lengths[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            return lengths;
        }

        static Vector3 Sample(Vector3[] path, float[] lengths, float t, out Vector3 direction)
        {
            float distance = Mathf.Clamp01(t) * lengths[lengths.Length - 1];
            int end = Array.BinarySearch(lengths, distance);
            end = Mathf.Clamp(end < 0 ? ~end : end, 1, lengths.Length - 1);
            direction = path[end] - path[end - 1];
            return Vector3.Lerp(path[end - 1], path[end], Mathf.InverseLerp(lengths[end - 1], lengths[end], distance));
        }

        Quaternion Facing(Vector3 direction) => Quaternion.LookRotation(-transform.TransformVector(direction));
        void Begin(Stage next) { stage = next; time = 0; }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if (rider != animal || stage != Stage.Approach) return false;
            rider = null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if (rider == animal) rider = null;
        }

        void Finish()
        {
            var animal = rider;
            rider = null;
            if (!animal) return;
            if (stage != Stage.Approach) animal.transform.position = transform.TransformPoint(Exit);
            animal.EndRide();
        }

        void OnDisable()
        {
            if (splash) splash.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            rippleAge = RippleSeconds;
            if (ripples) ripples.enabled = false;
            Finish();
        }
    }
}
