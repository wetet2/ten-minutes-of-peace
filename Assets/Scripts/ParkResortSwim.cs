using System;
using UnityEngine;
using static PeacePark.ParkResortRiver;

namespace PeacePark
{
    public sealed class ParkResortSwim : MonoBehaviour, IParkAnimalRide
    {
        public const int StepCount = 4;
        public static Vector2 StepsRim => PoolCenter + new Vector2(-2.1f, -PoolHalf.y);
        public static float StepRadius(int step) => 1.65f + .78f * step;
        public static float StepHeight(int step) => -.07f - .305f * step;

        const float JumpSeconds = .78f, ExitSeconds = 3.1f, SwimSeconds = 15f;
        const float TakeoffClearance = .64f, LandingInset = 2.35f, EntrySpacing = 2f;
        const float LaneOffset = 3.95f, LaneWidth = 2.37f, LaneLength = 7.35f;
        static readonly Vector2[] rim = Outline(PoolCenter, PoolHalf, PoolRadius);
        readonly Swimmer[] lanes = new Swimmer[2];

        enum Stage { Approach, Jumping, Swimming, Leaving }

        readonly struct Entry
        {
            public readonly Vector3 Takeoff, Landing, Inward;

            public Entry(Vector2 edge, Vector2 outward)
            {
                var deck = edge + outward * TakeoffClearance;
                var water = edge - outward * LandingInset;
                Takeoff = new Vector3(deck.x, DeckHeight, deck.y);
                Landing = new Vector3(water.x, WaterHeight - .075f, water.y);
                Inward = new Vector3(-outward.x, 0, -outward.y);
            }
        }

        sealed class Swimmer
        {
            public readonly ParkRabbitAgent Animal;
            public readonly Entry Entry;
            public readonly float StartLap, EndLap, JoinSeconds;
            public Stage Stage;
            public float Time;
            public Quaternion TakeoffFacing;

            public Swimmer(ParkRabbitAgent animal, Entry entry, int lane)
            {
                Animal = animal;
                Entry = entry;
                float closest = float.PositiveInfinity;
                const int samples = 48;
                for (int i = 0; i < samples; i++)
                {
                    float lap = i / (float)samples;
                    Course(lane, lap, out var water, out _);
                    float distance = (water - entry.Landing).sqrMagnitude;
                    if (distance >= closest) continue;
                    closest = distance;
                    StartLap = lap;
                }
                EndLap = StartLap < .5f ? 1 : 2;
                JoinSeconds = Mathf.Max(.4f, Mathf.Sqrt(closest) / 3.2f);
            }
        }

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Swim;

        internal static Vector3 ExitMark(int lane) =>
            new Vector3(StepsRim.x + (lane == 0 ? -1 : 1) * 1.05f, DeckHeight, StepsRim.y - .84f);

        internal bool IsSwimming(ParkRabbitAgent animal)
        {
            int i = FindLane(animal);
            return i >= 0 && lanes[i].Stage == Stage.Swimming;
        }

        internal bool TryQueue(ParkPlayground park, ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || FindLane(animal) >= 0) return false;
            if (lanes[0] != null && lanes[1] != null) return false;
            if (!TryFindEntry(park, animal.transform.position, out var entry, out var route)) return false;
            int lane = entry.Landing.x < PoolCenter.x ? 0 : 1;
            if (lanes[lane] != null) lane = 1 - lane;
            animal.ReserveRide(this, route);
            lanes[lane] = new Swimmer(animal, entry, lane);
            return true;
        }

        bool TryFindEntry(ParkPlayground park, Vector3 origin, out Entry entry, out Vector3[] route)
        {
            var local = transform.InverseTransformPoint(origin);
            var ground = new Vector2(local.x, local.z);
            var closestBySide = new Entry[4];
            var distanceBySide = new[] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
            var bestEntry = default(Entry);
            Vector3[] bestRoute = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < rim.Length; i++)
            {
                var start = rim[i];
                var edge = rim[(i + 1) % rim.Length] - start;
                var outward = new Vector2(edge.y, -edge.x).normalized;
                float projection = Mathf.Clamp01(Vector2.Dot(ground - start, edge) / edge.sqrMagnitude);
                var side = Mathf.Abs(outward.x) > Mathf.Abs(outward.y) ? (outward.x < 0 ? 0 : 1) : (outward.y < 0 ? 2 : 3);
                ConsiderEntry(new Entry(start + edge * projection, outward), side);
                // Long straight sides still need options when the closest point is occupied or obstructed.
                int sections = Mathf.CeilToInt(edge.magnitude / 1.6f);
                for (int j = 1; j < sections; j++)
                    ConsiderEntry(new Entry(start + edge * (j / (float)sections), outward), side);
            }

            // Check a detour only when its straight-line distance could beat the best clear approach.
            for (int side = 0; side < closestBySide.Length; side++)
            {
                if (distanceBySide[side] >= bestDistance) continue;
                var candidate = closestBySide[side];
                var mark = transform.TransformPoint(candidate.Takeoff);
                if (!park.TryBuildRidePath(origin, mark, out var path)) continue;
                float distance = 0;
                var previous = origin;
                foreach (var waypoint in path)
                {
                    distance += Vector3.Distance(previous, waypoint);
                    previous = waypoint;
                }
                distance += Vector3.Distance(previous, mark);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestEntry = candidate;
                bestRoute = path;
            }
            entry = bestEntry;
            route = bestRoute;
            return route != null;

            void ConsiderEntry(Entry candidate, int side)
            {
                var mark = transform.TransformPoint(candidate.Takeoff);
                float distance = Vector3.Distance(origin, mark);
                if (distance >= bestDistance || !park.WalkArea.Contains(new Vector2(mark.x, mark.z)) || !park.IsSceneryClear(mark)) return;
                var outlet = ParkResortSlide.ImpactPoint;
                if ((new Vector2(candidate.Landing.x - outlet.x, candidate.Landing.z - outlet.z)).sqrMagnitude < 4f) return;
                foreach (var swimmer in lanes)
                    if (swimmer != null && (swimmer.Stage == Stage.Approach || swimmer.Stage == Stage.Jumping) &&
                        transform.TransformVector(swimmer.Entry.Takeoff - candidate.Takeoff).sqrMagnitude < EntrySpacing * EntrySpacing) return;

                if (park.WalkingSegmentClear(origin, mark))
                {
                    bestDistance = distance;
                    bestEntry = candidate;
                    bestRoute = Array.Empty<Vector3>();
                }
                else if (distance < distanceBySide[side])
                {
                    distanceBySide[side] = distance;
                    closestBySide[side] = candidate;
                }
            }
        }

        internal void Advance(float dt)
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                var swimmer = lanes[i];
                if (swimmer == null) continue;
                if (!swimmer.Animal)
                {
                    lanes[i] = null;
                    continue;
                }

                swimmer.Time += dt;
                switch (swimmer.Stage)
                {
                    case Stage.Approach:
                        var facing = swimmer.Animal.transform.rotation;
                        if (swimmer.Animal.WalkToRideMark(transform.TransformPoint(swimmer.Entry.Takeoff), facing, dt, speed: 2.2f))
                        {
                            swimmer.TakeoffFacing = facing;
                            swimmer.Animal.BeginRideActivity();
                            NextStage(swimmer, Stage.Jumping);
                        }
                        break;
                    case Stage.Jumping:
                        JumpIntoPool(swimmer);
                        if (swimmer.Time >= JumpSeconds) NextStage(swimmer, Stage.Swimming);
                        break;
                    case Stage.Swimming:
                        SwimToSteps(swimmer, i);
                        break;
                    case Stage.Leaving:
                        LeaveBySteps(swimmer, i);
                        if (swimmer.Time >= ExitSeconds)
                        {
                            lanes[i] = null;
                            swimmer.Animal.EndRide();
                        }
                        break;
                }
            }
        }

        void JumpIntoPool(Swimmer swimmer)
        {
            float progress = Mathf.Clamp01(swimmer.Time / JumpSeconds);
            var entry = swimmer.Entry;
            var root = Vector3.Lerp(entry.Takeoff, entry.Landing, progress);
            root.y = DeckHeight + .94f * 4 * progress * (1 - progress);
            float immersion = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.53f, 1, progress));
            var facing = Quaternion.Slerp(swimmer.TakeoffFacing, Facing(entry.Inward), Mathf.Clamp01(progress * 4));
            Paddle(swimmer.Animal, entry.Landing, facing, immersion, swimmer.Time, root);
        }

        void SwimToSteps(Swimmer swimmer, int lane)
        {
            float laps = swimmer.StartLap + Mathf.Max(0, swimmer.Time - swimmer.JoinSeconds) / SwimSeconds;
            Course(lane, Mathf.Min(laps, swimmer.EndLap), out var water, out var tangent);
            float join = Mathf.Clamp01(swimmer.Time / swimmer.JoinSeconds);
            var point = Vector3.Lerp(swimmer.Entry.Landing, water, join);
            var facing = Quaternion.Slerp(Facing(swimmer.Entry.Inward), Facing(tangent), Mathf.SmoothStep(0, 1, join));
            Paddle(swimmer.Animal, point, facing, 1, JumpSeconds + swimmer.Time);
            if (laps >= swimmer.EndLap) NextStage(swimmer, Stage.Leaving);
        }

        void LeaveBySteps(Swimmer swimmer, int lane)
        {
            const float stepPortion = .60f;
            float progress = 1 - Mathf.Clamp01(swimmer.Time / ExitSeconds);
            var mark = ExitMark(lane);
            var wading = new Vector3(mark.x, 0, StepsRim.y + 2.83f);
            if (progress < stepPortion)
            {
                var point = Vector3.Lerp(mark, wading, progress / stepPortion);
                point.y = TreadHeight(point);
                swimmer.Animal.WalkAtRideStation(transform.TransformPoint(point), .83f);
                return;
            }

            wading.y = TreadHeight(wading);
            Course(lane, 0, out var water, out var tangent);
            float blend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(stepPortion, 1, progress));
            var stepFacing = Facing(Vector3.back);
            float seconds = JumpSeconds + swimmer.JoinSeconds + (swimmer.EndLap - swimmer.StartLap) * SwimSeconds + swimmer.Time;
            Paddle(swimmer.Animal, water, Quaternion.Slerp(stepFacing, Facing(tangent), blend), blend, seconds, wading);
        }

        static float TreadHeight(Vector3 point)
        {
            var radial = new Vector2(point.x, point.z) - StepsRim;
            float height = Mathf.Lerp(DeckHeight, StepHeight(0), Mathf.InverseLerp(-.18f, .12f, radial.y));
            for (int i = 1; i < StepCount; i++)
            {
                float edge = StepRadius(i - 1);
                height = Mathf.Lerp(height, StepHeight(i), Mathf.InverseLerp(edge - .14f, edge + .18f, radial.magnitude) *
                    Mathf.InverseLerp(-.18f, .12f, radial.y));
            }
            return height;
        }

        static void Course(int lane, float progress, out Vector3 point, out Vector3 heading)
        {
            int side = lane == 0 ? -1 : 1;
            float angle = Mathf.PI * 2 * progress;
            point = new Vector3(PoolCenter.x + side * (LaneOffset + LaneWidth * Mathf.Sin(angle)),
                WaterHeight - .075f, PoolCenter.y - LaneLength * Mathf.Cos(angle));
            heading = new Vector3(side * LaneWidth * Mathf.Cos(angle), 0, LaneLength * Mathf.Sin(angle));
        }

        Quaternion Facing(Vector3 direction) => Quaternion.LookRotation(-transform.TransformDirection(direction));

        void Paddle(ParkRabbitAgent animal, Vector3 water, Quaternion rotation, float strength, float seconds, Vector3 steps = default)
        {
            animal.transform.rotation = rotation;
            animal.Character.PlaySwimmingPose(strength, seconds);
            var chestOffset = animal.Character.SwimChest - animal.transform.position;
            water.y += .04f * Mathf.Sin(seconds * 6.8f);
            var afloat = transform.TransformPoint(water) - chestOffset;
            animal.transform.position = Vector3.Lerp(transform.TransformPoint(steps), afloat, strength);
        }

        static void NextStage(Swimmer swimmer, Stage stage)
        {
            swimmer.Stage = stage;
            swimmer.Time = 0;
        }

        int FindLane(ParkRabbitAgent animal)
        {
            for (int i = 0; i < lanes.Length; i++)
                if (lanes[i]?.Animal == animal) return i;
            return -1;
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            int lane = FindLane(animal);
            if (lane < 0 || lanes[lane].Stage != Stage.Approach) return false;
            lanes[lane] = null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            int lane = FindLane(animal);
            if (lane >= 0) lanes[lane] = null;
        }

        void OnDisable()
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                var swimmer = lanes[i];
                lanes[i] = null;
                if (swimmer == null || !swimmer.Animal) continue;
                if (swimmer.Stage != Stage.Approach) swimmer.Animal.transform.position = transform.TransformPoint(ExitMark(i));
                swimmer.Animal.EndRide();
            }
        }
    }
}
