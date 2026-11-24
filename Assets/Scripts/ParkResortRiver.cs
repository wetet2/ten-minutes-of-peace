using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkResortRiver : MonoBehaviour, IParkAnimalRide
    {
        public static readonly Vector2 PoolCenter = new Vector2(-16.8f, -.2f);
        public static readonly Vector2 PoolHalf = new Vector2(9.75f, 12.25f);
        public const float PoolRadius = 3.35f;
        public static readonly Vector2 RiverCenter = new Vector2(11.3f, -.2f);
        public static readonly Vector2 RiverHalf = new Vector2(12.05f, 11.75f);
        public const float RiverRadius = 5.1f, ChannelHalfWidth = 2.45f;
        public const float DeckHeight = .06f, WaterHeight = -.26f;
        public const float BridgeZ = 3.05f, BridgeHalfSpan = 7.40f, BridgeHalfWidth = 1.46f;
        public const float IslandPathHeight = .314f, BridgeDeckDepth = .29f;
        public const float IslandPathStartX = 9.7f, IslandPathHalfWidth = .97f, IslandGardenInset = 3.02f, IslandLawnHeight = .30f;
        public static float BridgeCenterX => RiverCenter.x + RiverHalf.x;
        static float BridgeIslandX => BridgeCenterX - BridgeHalfSpan;
        static float BridgeTerraceX => BridgeCenterX + BridgeHalfSpan;
        public static readonly Vector3 TubeHandle = new Vector3(.655f, .232f, .12f);
        internal const float TubeWaistRise = .06f;
        const float DockWidth = 4.1f, DockDepth = 1.65f, DockOffset = 2.36f;
        const float BoardingSeconds = 1.1f, CurrentSpeed = 3.2f, TubeSpacing = 5.5f;
        public static float DockEdge => RiverCenter.y - RiverHalf.y - ChannelHalfWidth - DockDepth;
        internal static Vector3 BoardingMark => new Vector3(RiverCenter.x - .50f, DeckHeight, DockEdge - .83f);
        internal static Vector3 ExitMark => new Vector3(RiverCenter.x + 1.65f, DeckHeight, DockEdge - .88f);

        public static float BridgeSurfaceHeight(float x)
        {
            float t = Mathf.InverseLerp(BridgeIslandX, BridgeTerraceX, x);
            float arch = Mathf.Sin(t * Mathf.PI);
            return Mathf.SmoothStep(IslandPathHeight, DeckHeight, t) + 2.43f * arch * arch;
        }

        static readonly Vector2[] route = Outline(RiverCenter, RiverHalf, RiverRadius);
        static readonly float[] distances = RouteDistances();
        internal static float LapLength => distances[distances.Length - 1];

        [SerializeField] Transform parkedTube;
        readonly List<Rider> riders = new List<Rider>();
        BridgeCrossing bridge;
        int nextTube;

        enum Stage { Approach, Boarding, Floating, Disembarking }

        sealed class Rider
        {
            public readonly ParkRabbitAgent Animal;
            public Stage Stage;
            public Transform Tube;
            public Vector3 Start;
            public Quaternion Facing;
            public float Time, Distance;

            public Rider(ParkRabbitAgent animal) { Animal = animal; }
        }

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Tube;
        internal int CompletedLaps { get; private set; }
        internal Transform TubeFor(ParkRabbitAgent animal) => FindRider(animal)?.Tube;
        internal bool IsFloating(ParkRabbitAgent animal) => FindRider(animal)?.Stage == Stage.Floating;
        public bool IsMovingPart(Transform part) => parkedTube && part.IsChildOf(parkedTube);

        public void Configure(Transform tube)
        {
            parkedTube = tube;
            SetTubePose(tube, 0, 0);
        }

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || !parkedTube || FindRider(animal) != null) return false;
            foreach (var rider in riders)
                if (rider.Stage < Stage.Floating) return false;
            if (!animal.TryReserveRequestedRide(this, transform.TransformPoint(BoardingMark))) return false;
            riders.Add(new Rider(animal));
            return true;
        }

        internal bool TryQueueBridge(ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled) return false;
            bridge ??= new BridgeCrossing(this);
            return bridge.TryQueue(animal);
        }

        internal void Advance(float dt)
        {
            for (int i = 0; i < riders.Count;)
            {
                var rider = riders[i];
                var ahead = i > 0 ? riders[i - 1] : null;
                if (rider.Animal && !AdvanceRider(rider, ahead, dt))
                {
                    i++;
                    continue;
                }
                riders.RemoveAt(i);
                Finish(rider, finishAnimal: true);
            }
            if (parkedTube) parkedTube.gameObject.SetActive(!DockOccupied());
            bridge?.Advance(dt);
        }

        bool AdvanceRider(Rider rider, Rider ahead, float dt)
        {
            var animal = rider.Animal;
            rider.Time += dt;
            switch (rider.Stage)
            {
                case Stage.Approach:
                    var mark = transform.TransformPoint(BoardingMark);
                    if (!animal.WalkToRideMark(mark, Quaternion.LookRotation(-transform.forward), dt, speed: 2.25f) || DockOccupied()) break;
                    rider.Tube = Instantiate(parkedTube, transform, false);
                    rider.Tube.name = "River tube " + ++nextTube;
                    var fit = animal.Character.Kind switch
                    {
                        AnimalKind.Hippo => new Vector3(1.24f, 1, 1.19f),
                        AnimalKind.Crocodile => new Vector3(1, 1, 1.16f),
                        AnimalKind.Giraffe or AnimalKind.Elephant => new Vector3(.96f, 1, .96f),
                        _ => Vector3.one
                    };
                    rider.Tube.localScale = Vector3.Scale(parkedTube.localScale, fit);
                    SelectTubeColors(rider.Tube, nextTube);
                    rider.Tube.gameObject.SetActive(true);
                    rider.Start = animal.transform.position;
                    rider.Facing = animal.transform.rotation;
                    rider.Stage = Stage.Boarding;
                    animal.BeginRideActivity();
                    rider.Time = 0;
                    break;
                case Stage.Boarding:
                    float board = Smooth(rider.Time / BoardingSeconds);
                    SetTubePose(rider.Tube, 0, rider.Time);
                    animal.transform.rotation = Quaternion.Slerp(rider.Facing, RiderFacing(rider.Tube), board);
                    var boarding = Vector3.Lerp(rider.Start, FloatPosition(rider, board), board);
                    animal.transform.position = boarding + Vector3.up * (.48f * Mathf.Sin(board * Mathf.PI));
                    HoldTube(rider, board);
                    if (board >= 1)
                    {
                        rider.Stage = Stage.Floating;
                        rider.Time = 0;
                    }
                    break;
                case Stage.Floating:
                    float limit = ahead == null ? LapLength : Mathf.Max(0, ahead.Distance - TubeSpacing);
                    float ease = Mathf.Lerp(.46f, 1, Mathf.InverseLerp(0, 3.4f, Mathf.Min(rider.Distance, LapLength - rider.Distance)));
                    rider.Distance = Mathf.Min(rider.Distance + CurrentSpeed * ease * dt, limit);
                    SetTubePose(rider.Tube, rider.Distance, rider.Time);
                    animal.transform.rotation = RiderFacing(rider.Tube);
                    animal.transform.position = FloatPosition(rider, 1);
                    HoldTube(rider, 1);
                    if (rider.Distance >= LapLength)
                    {
                        CompletedLaps++;
                        rider.Stage = Stage.Disembarking;
                        rider.Start = animal.transform.position;
                        rider.Facing = animal.transform.rotation;
                        rider.Time = 0;
                    }
                    break;
                case Stage.Disembarking:
                    float leave = Smooth(rider.Time / BoardingSeconds);
                    var exit = transform.TransformPoint(ExitMark);
                    var feet = Vector3.Lerp(rider.Start, exit, leave);
                    feet += Vector3.up * (.52f * Mathf.Sin(leave * Mathf.PI));
                    animal.transform.SetPositionAndRotation(feet, Quaternion.Slerp(rider.Facing, Quaternion.LookRotation(transform.forward), leave));
                    animal.Character.PlayTubeFloatPose(1 - leave, rider.Time);
                    HoldTube(rider, 1 - leave);
                    return leave >= 1;
            }
            return false;
        }

        bool DockOccupied()
        {
            foreach (var rider in riders)
                if (rider.Stage == Stage.Boarding || rider.Stage == Stage.Disembarking ||
                    (rider.Stage == Stage.Floating && (rider.Distance < TubeSpacing || rider.Distance > LapLength - TubeSpacing))) return true;
            return false;
        }

        static void SelectTubeColors(Transform tube, int number)
        {
            // The mesh bands are shared by the parked tube and all of its copies.
            var bands = tube.Find(number % 2 == 0 ? "Peach bands" : "Blue bands");
            if (!bands) return;
            tube.Find(number % 2 == 0 ? "Blue bands" : "Peach bands").gameObject.SetActive(false);
            bands.gameObject.SetActive(true);
        }

        void SetTubePose(Transform tube, float distance, float seconds)
        {
            var point = ChannelPoint(distance);
            float nearDock = Mathf.Min(distance, LapLength - distance);
            float dock = 1 - Smooth(nearDock / 5.2f);
            point.y -= DockOffset * dock;
            var direction = ChannelPoint(distance + .35f) - ChannelPoint(distance - .35f);
            float height = WaterHeight + .045f + .035f * Mathf.Sin(seconds * 2.6f);
            tube.SetPositionAndRotation(transform.TransformPoint(new Vector3(point.x, height, point.y)),
                Quaternion.LookRotation(transform.TransformDirection(new Vector3(direction.x, 0, direction.y))));
        }

        static Vector3 FloatPosition(Rider rider, float immersed)
        {
            var animal = rider.Animal;
            animal.Character.PlayTubeFloatPose(immersed, rider.Time);
            return rider.Tube.position + Vector3.up * TubeWaistRise - (animal.Character.TubeWaist - animal.transform.position);
        }

        static void HoldTube(Rider rider, float grip)
        {
            var rightHandle = new Vector3(-TubeHandle.x, TubeHandle.y, TubeHandle.z);
            rider.Animal.Character.HoldTube(rider.Tube.TransformPoint(TubeHandle), rider.Tube.TransformPoint(rightHandle), grip);
        }

        static Quaternion RiderFacing(Transform tube) => Quaternion.LookRotation(-tube.forward);
        static float Smooth(float t) => Mathf.SmoothStep(0, 1, t);

        Rider FindRider(ParkRabbitAgent animal)
        {
            foreach (var rider in riders)
                if (rider.Animal == animal) return rider;
            return null;
        }

        void Finish(Rider rider, bool finishAnimal)
        {
            if (rider.Tube)
            {
                rider.Tube.gameObject.SetActive(false);
                Destroy(rider.Tube.gameObject);
            }
            if (finishAnimal && rider.Animal) rider.Animal.EndRide();
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            var rider = FindRider(animal);
            return rider != null && rider.Stage == Stage.Approach && riders.Remove(rider);
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            var rider = FindRider(animal);
            if (rider == null) return;
            riders.Remove(rider);
            Finish(rider, finishAnimal: false);
            if (parkedTube) parkedTube.gameObject.SetActive(!DockOccupied());
        }

        void OnDisable()
        {
            bridge?.Reset();
            foreach (var rider in riders)
            {
                if (rider.Animal && rider.Tube) rider.Animal.transform.position = transform.TransformPoint(ExitMark);
                Finish(rider, finishAnimal: true);
            }
            riders.Clear();
            if (parkedTube) parkedTube.gameObject.SetActive(true);
        }

        internal bool BlocksWalking(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            var point = new Vector2(local.x, local.z);
            const float clearance = .38f;
            if (RoundedRectangle(point - PoolCenter, PoolHalf, PoolRadius) < clearance) return true;
            if (IslandDistance(point, ChannelHalfWidth) <= -clearance)
                return point.x > BridgeIslandX && Mathf.Abs(point.y - BridgeZ) < BridgeHalfWidth + clearance;
            var half = RiverHalf + Vector2.one * ChannelHalfWidth;
            if (RoundedRectangle(point - RiverCenter, half, RiverRadius + ChannelHalfWidth) < clearance) return true;
            float dock = Mathf.Abs(point.x - RiverCenter.x);
            return point.y >= RiverCenter.y - half.y - DockExtension(dock) - clearance &&
                point.y < RiverCenter.y - half.y && dock < DockWidth + clearance;
        }

        internal bool IsOnIsland(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            return IslandDistance(new Vector2(local.x, local.z), ChannelHalfWidth) <= 0;
        }

        internal Rect IslandWalkBounds
        {
            get
            {
                var half = RiverHalf - Vector2.one * ChannelHalfWidth;
                var min = transform.TransformPoint(new Vector3(RiverCenter.x - half.x, DeckHeight, RiverCenter.y - half.y));
                var max = transform.TransformPoint(new Vector3(RiverCenter.x + half.x, DeckHeight, RiverCenter.y + half.y));
                return Rect.MinMaxRect(min.x, min.z, max.x, max.z);
            }
        }

        internal bool TryGetIslandHeight(Vector3 world, out float height)
        {
            var local = transform.InverseTransformPoint(world);
            var point = new Vector2(local.x, local.z);
            height = 0;
            if (IslandDistance(point, ChannelHalfWidth) > 0) return false;
            bool onPath = local.x >= IslandPathStartX && local.x <= BridgeIslandX &&
                Mathf.Abs(local.z - BridgeZ) <= IslandPathHalfWidth;
            local.y = onPath ? IslandPathHeight : IslandDistance(point, IslandGardenInset) <= 0 ? IslandLawnHeight : DeckHeight;
            height = transform.TransformPoint(local).y;
            return true;
        }

        static float IslandDistance(Vector2 point, float inset) =>
            RoundedRectangle(point - RiverCenter, RiverHalf - Vector2.one * inset, RiverRadius - inset);

        sealed class BridgeCrossing : IParkAnimalRide
        {
            const float WalkSpeed = 1.55f;
            static float IslandMark => BridgeIslandX - 1.25f;
            static float TerraceMark => BridgeTerraceX + .20f;
            readonly ParkResortRiver river;
            ParkRabbitAgent animal;
            bool crossing;
            float startX, endX, x;

            internal BridgeCrossing(ParkResortRiver river) { this.river = river; }
            ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.ResortBridge;

            internal bool TryQueue(ParkRabbitAgent candidate)
            {
                if (animal) return false;
                bool onIsland = river.IsOnIsland(candidate.transform.position);
                float start = onIsland ? IslandMark : TerraceMark;
                if (!candidate.TryReserveRequestedRide(this, Point(start))) return false;
                animal = candidate;
                startX = x = start;
                endX = onIsland ? TerraceMark : IslandMark;
                crossing = false;
                return true;
            }

            internal void Advance(float dt)
            {
                if (!animal) return;
                if (!crossing)
                {
                    var facing = Quaternion.LookRotation(endX > startX ? -river.transform.right : river.transform.right);
                    if (!animal.WalkToRideMark(Point(startX), facing, dt)) return;
                    crossing = true;
                    animal.BeginRideActivity();
                    return;
                }

                x = Mathf.MoveTowards(x, endX, WalkSpeed * dt / river.transform.TransformVector(Vector3.right).magnitude);
                animal.WalkAtRideStation(Point(x));
                if (x == endX) Finish();
            }

            internal void Reset()
            {
                if (!animal) return;
                if (crossing) animal.transform.position = Point(startX);
                Finish();
            }

            void Finish()
            {
                var completed = animal;
                animal = null;
                completed.EndRide();
            }

            Vector3 Point(float x) => river.transform.TransformPoint(new Vector3(x, BridgeSurfaceHeight(x), BridgeZ));

            bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent candidate)
            {
                if (animal != candidate || crossing) return false;
                animal = null;
                return true;
            }

            void IParkAnimalRide.Release(ParkRabbitAgent candidate)
            {
                if (animal == candidate) animal = null;
            }
        }

        public static float DockExtension(float distanceFromCenter) => DockDepth * (1 - Smooth(distanceFromCenter / DockWidth));

        static float RoundedRectangle(Vector2 point, Vector2 half, float radius)
        {
            var edge = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - half + Vector2.one * radius;
            return Vector2.Max(edge, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(edge.x, edge.y), 0) - radius;
        }

        public static Vector2[] Outline(Vector2 center, Vector2 half, float radius)
        {
            const int cornerSteps = 12, frontSteps = 9;
            var points = new List<Vector2>(4 * (cornerSteps + 1) + frontSteps * 2 - 1);
            float straight = half.x - radius;
            for (int i = 0; i < frontSteps; i++)
                points.Add(center + new Vector2(straight * i / frontSteps, -half.y));
            for (int i = 0; i < 4; i++)
            {
                var arcCenter = center + new Vector2(i < 2 ? straight : -straight, i == 0 || i == 3 ? radius - half.y : half.y - radius);
                for (int j = 0; j <= cornerSteps; j++)
                {
                    float angle = Mathf.PI * .5f * (i - 1 + (float)j / cornerSteps);
                    points.Add(arcCenter + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
            }
            for (int i = frontSteps - 1; i > 0; i--)
                points.Add(center + new Vector2(-straight * i / frontSteps, -half.y));
            return points.ToArray();
        }

        static float[] RouteDistances()
        {
            var result = new float[route.Length + 1];
            for (int i = 0; i < route.Length; i++) result[i + 1] = result[i] + Vector2.Distance(route[i], route[(i + 1) % route.Length]);
            return result;
        }

        internal static Vector2 ChannelPoint(float distance)
        {
            distance = Mathf.Repeat(distance, LapLength);
            int index = System.Array.BinarySearch(distances, distance);
            if (index >= 0) return route[index % route.Length];
            int next = ~index;
            return Vector2.Lerp(route[next - 1], route[next % route.Length], Mathf.InverseLerp(distances[next - 1], distances[next], distance));
        }
    }
}
