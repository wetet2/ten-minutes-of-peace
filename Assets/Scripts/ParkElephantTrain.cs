using System;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkElephantTrain : MonoBehaviour, IParkAnimalRide
    {
        public const float Straight = 56f;
        public const float Radius = 27f;
        public const float StationRailZ = -32f;
        public const float CenterZ = StationRailZ + Radius;
        public const float RailHeight = .22f;
        public const float DeckHeight = .12f;
        public const float CarFloor = .64f;
        public const float WheelRadius = .31f;
        public const float CarSpacing = 3.45f;
        public const float Length = 2 * Straight + 2 * Mathf.PI * Radius;
        public const float HillHeight = 4.8f;
        public const float FarStraightStart = Straight / 2 - 7 + Mathf.PI * Radius;
        const float DockOffset = Straight / 2 + 7;
        const float LapSeconds = 64f;
        const float AccelerationSeconds = 2f;
        const float TransferSeconds = 2.45f;
        const float DisembarkSeconds = 2.85f;
        const float SlotDelay = .25f;

        [SerializeField] Transform[] cars, axles, couplers;
        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Train;
        internal enum RidePhase { AtStart, Running, Unloading, Boarding }
        internal RidePhase Phase { get; private set; }
        internal bool CanAutoBoard => Phase is RidePhase.Boarding or RidePhase.Unloading ||
            Phase == RidePhase.Running && elapsed >= LapSeconds - 16f;
        internal int CompletedLaps { get; private set; }
        internal int SeatedAnimals
        {
            get
            {
                int count = 0;
                foreach (var slot in seats)
                    if (slot.Rider) count++;
                return count;
            }
        }

        sealed class Seat
        {
            public Transform Car;
            public Vector3 Door, Step, Wait, Exit, Floor, Rest;
            public Quaternion AtDoor, FacingForward;
            public ParkRabbitAgent Waiting, Rider;
            public float BoardingTime, WaitingTime;
            public bool AtMark;
        }

        Seat[] seats = Array.Empty<Seat>();
        float elapsed;

        public void Configure(Transform[] trainCars, Transform[] trainAxles, Transform[] links)
        {
            cars = trainCars;
            axles = trainAxles;
            couplers = links;
            PlaceTrain(0);
        }

        public static Vector3 TrackPoint(float distance)
        {
            float s = Mathf.Repeat(DockOffset + distance, Length);
            float half = Straight / 2;
            float curve = Mathf.PI * Radius;
            if (s < Straight) return new Vector3(s - half, RailHeight, StationRailZ);
            s -= Straight;
            if (s < curve)
            {
                float angle = s / Radius;
                return new Vector3(half + Mathf.Sin(angle) * Radius, RailHeight, CenterZ - Mathf.Cos(angle) * Radius);
            }
            s -= curve;
            if (s < Straight)
            {
                float rise = Mathf.Sin(Mathf.PI * s / Straight);
                return new Vector3(half - s, RailHeight + HillHeight * rise * rise, CenterZ + Radius);
            }
            float turn = (s - Straight) / Radius;
            return new Vector3(-half - Mathf.Sin(turn) * Radius, RailHeight, CenterZ + Mathf.Cos(turn) * Radius);
        }

        public static bool WithinTrackLoop(Vector3 point, float margin=0)
        {
            float beyondStraight=Mathf.Max(0,Mathf.Abs(point.x)-Straight/2);
            float depth=point.z-CenterZ;
            float radius=Radius+margin;
            return beyondStraight*beyondStraight+depth*depth<=radius*radius;
        }

        internal Pose RideView(bool locomotive)
        {
            if (locomotive)
            {
                var engine = cars[0];
                return new Pose(engine.TransformPoint(new Vector3(0, 2.83f, 1.67f)),
                    engine.rotation * Quaternion.Euler(3, 0, 0));
            }
            var car = cars[1];
            return new Pose(car.TransformPoint(new Vector3(-.82f, 2.87f, .52f)),
                car.rotation * Quaternion.Euler(6, -36, 0));
        }

        internal float PickDepth(Ray ray, out bool locomotive)
        {
            locomotive = false;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < cars.Length; i++)
            {
                var bounds = i == 0
                    ? new Bounds(new Vector3(0, 1.62f, .52f), new Vector3(3.40f, 3.24f, 4.85f))
                    : new Bounds(new Vector3(0, .85f, 0), new Vector3(3.78f, 1.72f, 3.22f));
                float depth = ParkCamera.HitDepth(ray, bounds, cars[i].localToWorldMatrix);
                if (depth >= nearest) continue;
                nearest = depth;
                locomotive = i == 0;
            }
            return nearest;
        }

        void OnEnable()
        {
            if (cars == null || cars.Length < 2) return;
            Phase = RidePhase.AtStart;
            elapsed = 0;
            CompletedLaps = 0;
            PlaceTrain(0);
            seats = new Seat[cars.Length - 1];
            for (int i = 0; i < seats.Length; i++)
            {
                var car = cars[i + 1];
                var door = car.TransformPoint(new Vector3(-2.60f, 0, .32f));
                door.y = transform.TransformPoint(Vector3.up * DeckHeight).y;
                seats[i] = new Seat
                {
                    Car = car,
                    Door = door,
                    Step = car.TransformPoint(new Vector3(-1.62f, .33f, .32f)),
                    Wait = door + car.TransformVector(new Vector3(-1.45f, 0, .62f)),
                    Exit = door + car.TransformVector(new Vector3(-2.35f, 0, -.60f)),
                    Floor = car.TransformPoint(new Vector3(-.71f, CarFloor, .32f)),
                    Rest = new Vector3(0, CarFloor, -.30f),
                    AtDoor = Quaternion.LookRotation(-car.right),
                    FacingForward = car.rotation * Quaternion.Euler(0, 180, 0)
                };
            }
        }

        void OnDisable()
        {
            ClearPassengers();
        }

        internal void ClearPassengers()
        {
            foreach (var slot in seats)
            {
                if (slot.Waiting) slot.Waiting.EndRide();
                if (slot.Rider) slot.Rider.EndRide();
                slot.Waiting = slot.Rider = null;
                slot.BoardingTime = slot.WaitingTime = 0;
                slot.AtMark = false;
            }
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            foreach (var slot in seats)
            {
                if (slot.Waiting == animal)
                {
                    ClearWaiting(slot);
                }
                if (slot.Rider == animal) slot.Rider = null;
            }
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            foreach(var slot in seats)
            {
                if(slot.Waiting!=animal||slot.BoardingTime>0)continue;
                ClearWaiting(slot);
                return true;
            }
            return false;
        }

        internal bool TryRequestedRide(ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled)return false;
            foreach(var slot in seats)
            {
                if(slot.Waiting||(Phase==RidePhase.Boarding&&slot.Rider)||
                    !animal.TryReserveRequestedRide(this,slot.Wait))continue;
                slot.Waiting=animal;
                return true;
            }
            return false;
        }

        static void ClearWaiting(Seat slot)
        {
            slot.Waiting=null;
            slot.AtMark=false;
            slot.BoardingTime=slot.WaitingTime=0;
        }

        internal void Advance(float dt)
        {
            if (seats.Length == 0) return;
            elapsed += dt;
            switch (Phase)
            {
                case RidePhase.AtStart:
                    if (elapsed >= 1.8f) SetPhase(RidePhase.Running);
                    break;
                case RidePhase.Running:
                    WalkToQueue(dt);
                    PlaceTrain(LapDistance(elapsed));
                    foreach (var slot in seats)
                        if (slot.Rider) SitInCar(slot);
                    if (elapsed >= LapSeconds)
                    {
                        CompletedLaps++;
                        SetPhase(SeatedAnimals > 0 ? RidePhase.Unloading : RidePhase.Boarding);
                    }
                    break;
                case RidePhase.Unloading:
                    WalkToQueue(dt);
                    Unload();
                    if (SeatedAnimals == 0) SetPhase(RidePhase.Boarding);
                    break;
                case RidePhase.Boarding:
                    WalkToQueue(dt);
                    Board(dt);
                    break;
            }
        }

        void SetPhase(RidePhase phase)
        {
            Phase = phase;
            elapsed = 0;
        }

        static float LapDistance(float time)
        {
            float t = Mathf.Clamp(time, 0, LapSeconds);
            float remaining = LapSeconds - t;
            float area = t - AccelerationSeconds / 2;
            if (t < AccelerationSeconds) area = t * t / (2 * AccelerationSeconds);
            if (remaining < AccelerationSeconds)
                area = LapSeconds - AccelerationSeconds - remaining * remaining / (2 * AccelerationSeconds);
            return Length * (area / (LapSeconds - AccelerationSeconds));
        }

        void PlaceTrain(float distance)
        {
            for (int i = 0; i < cars.Length; i++)
            {
                float s = distance - i * CarSpacing;
                var front = TrackPoint(s + .95f);
                var back = TrackPoint(s - .95f);
                cars[i].SetLocalPositionAndRotation((front + back) * .5f, Quaternion.LookRotation(front - back));
            }
            float rolling = (distance + CompletedLaps * Length) / WheelRadius * Mathf.Rad2Deg;
            foreach (var axle in axles) axle.localRotation = Quaternion.Euler(rolling, 0, 0);
            for (int i = 0; i < couplers.Length; i++)
            {
                var frontCar = cars[i];
                var rearCar = cars[i + 1];
                var from = frontCar.localPosition + frontCar.localRotation * new Vector3(0, .38f, -1.49f);
                var to = rearCar.localPosition + rearCar.localRotation * new Vector3(0, .38f, 1.49f);
                var link = couplers[i];
                link.SetLocalPositionAndRotation((from + to) * .5f, Quaternion.LookRotation(from - to));
                link.localScale = new Vector3(.24f, .20f, Vector3.Distance(from, to) + .10f);
            }
        }

        void WalkToQueue(float dt)
        {
            foreach (var slot in seats)
            {
                if(!slot.Waiting)continue;
                if(!slot.AtMark)slot.AtMark=slot.Waiting.WalkToRideMark(slot.Wait,slot.AtDoor,dt);
                else if(slot.BoardingTime<=0)
                {
                    slot.WaitingTime+=dt;
                    slot.Waiting.Character.PlayRideQueuePose(slot.WaitingTime,slot.Waiting.Id);
                }
            }
        }

        void Unload()
        {
            for (int i = 0; i < seats.Length; i++)
            {
                var slot = seats[i];
                if (!slot.Rider || elapsed < i * SlotDelay) continue;
                float progress = (elapsed - i * SlotDelay) / DisembarkSeconds;
                if (progress < 1)
                {
                    Transfer(slot, slot.Rider, 1 - progress, false);
                    continue;
                }
                Transfer(slot, slot.Rider, 0, false);
                slot.Rider.EndRide();
                slot.Rider = null;
            }
        }

        void Board(float dt)
        {
            bool transferring = false;
            for (int i = 0; i < seats.Length; i++)
            {
                var slot = seats[i];
                if (!slot.AtMark || !slot.Waiting || elapsed < .5f + i * SlotDelay) continue;
                if (slot.BoardingTime == 0 && elapsed >= 5f) continue;
                if (slot.BoardingTime == 0) slot.Waiting.BeginRideActivity();
                slot.BoardingTime += dt;
                Transfer(slot, slot.Waiting, Mathf.Clamp01(slot.BoardingTime / TransferSeconds), true);
                if (slot.BoardingTime < TransferSeconds)
                {
                    transferring = true;
                    continue;
                }
                slot.Rider = slot.Waiting;
                ClearWaiting(slot);
            }
            if (!transferring && elapsed >= 5f) SetPhase(RidePhase.Running);
        }

        static void Transfer(Seat slot, ParkRabbitAgent animal, float progress, bool boarding)
        {
            float stepUp = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.40f, .60f, progress));
            float cross = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.60f, .82f, progress));
            float sit = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, 1, progress));
            var seat = slot.Car.TransformPoint(slot.Rest);
            var platform = boarding ? slot.Wait : slot.Exit;
            if (progress < .40f)
            {
                float walk = Mathf.InverseLerp(0, .40f, progress);
                animal.WalkAtRideStation(Vector3.Lerp(platform, slot.Door, walk));
                return;
            }
            var point = progress < .60f ? Vector3.Lerp(slot.Door, slot.Step, stepUp) :
                progress < .82f ? Vector3.Lerp(slot.Step, slot.Floor, cross) : Vector3.Lerp(slot.Floor, seat, sit);
            var doorFacing = boarding ? slot.AtDoor : Quaternion.LookRotation(slot.Car.right);
            var facing = Quaternion.Slerp(doorFacing, slot.FacingForward, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, .96f, progress)));
            animal.transform.SetPositionAndRotation(point, facing);
            if (progress < .82f)
                animal.Character.PlayRideStepPose(progress < .60f ? stepUp : cross, progress < .60f);
            else
                animal.Character.PlayRideSeatPose(sit);
        }

        static void SitInCar(Seat slot)
        {
            slot.Rider.transform.SetPositionAndRotation(slot.Car.TransformPoint(slot.Rest),
                slot.Car.rotation * Quaternion.Euler(0, 180, 0));
            slot.Rider.Character.PlayRideSeatPose(1);
        }
    }
}
