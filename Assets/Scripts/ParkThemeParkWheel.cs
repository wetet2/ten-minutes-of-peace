using System;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkThemeParkWheel : MonoBehaviour, IParkAnimalRide
    {
        public const float Radius = 18.6f;
        public const float HubHeight = Radius + 2.55f;
        public const float PlatformHeight = .62f;
        public const float CabinFloor = PlatformHeight - (HubHeight - Radius);
        public const float CabinOffset = -2.45f;
        const float TravelTime = 6f;
        const float TransferTime = 2.6f;
        const float CruiseSpeed = 1f / TravelTime;
        const float Acceleration = .21f;
        static readonly Vector3 SeatPosition = new Vector3(0, CabinFloor, .33f);
        static readonly Vector3 QueuePosition = new Vector3(.25f, .015f, CabinOffset - 4.6f);
        static readonly Vector3 ExitPosition = new Vector3(-1.95f, .015f, CabinOffset - 4.65f);
        static readonly Vector3 StepPosition = new Vector3(0, .31f, CabinOffset - 2.96f);
        static readonly Vector3 DoorPosition = new Vector3(0, PlatformHeight, CabinOffset - 1.45f);
        static readonly Vector3 StepStart = new Vector3(0, .015f, CabinOffset - 3.67f);

        [SerializeField] Transform wheel;
        [SerializeField] Transform[] gondolas;
        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Wheel;
        ParkRabbitAgent[] riders = Array.Empty<ParkRabbitAgent>();
        ParkRabbitAgent waiting;
        enum StopPhase { Boarding, Turning, Unloading }
        StopPhase phase;
        int dock;
        float time, boardingTime, rotation, speed;
        bool atMark;
        internal Vector3 BoardingPoint => transform.TransformPoint(QueuePosition);

        public void Configure(Transform wheelRoot, Transform[] cabins)
        {
            wheel = wheelRoot;
            gondolas = cabins;
        }

        void OnEnable()
        {
            if (!wheel) return;
            riders = new ParkRabbitAgent[gondolas.Length];
            dock = 0;
            rotation = 0;
            speed = CruiseSpeed;
            SetPhase(StopPhase.Turning);
            PlaceWheel(0);
        }

        void OnDisable()
        {
            if (waiting) waiting.EndRide();
            foreach (var rider in riders)
                if (rider) rider.EndRide();
            waiting = null;
            riders = Array.Empty<ParkRabbitAgent>();
            atMark = false;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if (waiting == animal)
            {
                waiting = null;
                atMark = false;
                boardingTime = 0;
            }
            for (int i = 0; i < riders.Length; i++)
                if (riders[i] == animal) riders[i] = null;
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if(waiting!=animal||boardingTime>0)return false;
            waiting=null;
            atMark=false;
            return true;
        }

        internal bool TryRequestedRide(ParkRabbitAgent animal)
        {
            if(!isActiveAndEnabled||riders.Length==0||waiting||
                !animal.TryReserveRequestedRide(this,BoardingPoint))return false;
            waiting=animal;
            return true;
        }

        internal void Advance(float dt)
        {
            if (riders.Length == 0) return;
            time += dt;
            if (waiting && !atMark)
                atMark = waiting.WalkToRideMark(BoardingPoint,
                    transform.rotation * Quaternion.Euler(0, 180, 0), dt);

            switch (phase)
            {
                case StopPhase.Boarding:
                    Board(dt);
                    break;
                case StopPhase.Turning:
                    Turn(dt);
                    break;
                case StopPhase.Unloading:
                    if (riders[dock])
                    {
                        Transfer(riders[dock], 1 - Mathf.Clamp01(time / TransferTime), false);
                        if (time < TransferTime) return;
                        riders[dock].EndRide();
                        riders[dock] = null;
                    }
                    SetPhase(waiting && atMark ? StopPhase.Boarding : StopPhase.Turning);
                    break;
            }
        }

        void Turn(float dt)
        {
            int next = Mathf.FloorToInt(rotation) + 1;
            int nextCabin = next % gondolas.Length;
            float remaining = next - rotation;
            bool stop = riders[nextCabin] || (waiting && atMark);
            float target = stop ? Mathf.Min(CruiseSpeed, Mathf.Sqrt(2 * Acceleration * remaining)) : CruiseSpeed;
            speed = Mathf.MoveTowards(speed, target, Acceleration * dt);
            float step = speed * dt;
            if (stop && remaining <= Mathf.Max(step, .001f))
            {
                rotation = dock = nextCabin;
                speed = 0;
                PlaceWheel(rotation);
                SetPhase(riders[dock] ? StopPhase.Unloading : StopPhase.Boarding);
                return;
            }
            rotation = Mathf.Repeat(rotation + step, gondolas.Length);
            PlaceWheel(rotation);
        }

        void SetPhase(StopPhase next)
        {
            phase = next;
            time = boardingTime = 0;
        }

        void Board(float dt)
        {
            if (!waiting || !atMark)
            {
                SetPhase(StopPhase.Turning);
                return;
            }
            if (time < .4f) return;
            if (boardingTime == 0) waiting.BeginRideActivity();
            boardingTime += dt;
            Transfer(waiting, Mathf.Clamp01(boardingTime / TransferTime), true);
            if (boardingTime < TransferTime) return;
            riders[dock] = waiting;
            waiting = null;
            atMark = false;
            SetPhase(StopPhase.Turning);
        }

        void PlaceWheel(float stops)
        {
            float angle = stops * 360 / gondolas.Length;
            wheel.localRotation = Quaternion.Euler(0, 0, -angle);
            var upright = Quaternion.Euler(0, 0, angle);
            for (int i = 0; i < gondolas.Length; i++)
            {
                var cabin = gondolas[i];
                cabin.localRotation = upright;
                if (riders[i]) riders[i].transform.SetPositionAndRotation(cabin.TransformPoint(SeatPosition), cabin.rotation);
            }
        }

        void Transfer(ParkRabbitAgent animal, float progress, bool boarding)
        {
            var terminal = boarding ? QueuePosition : ExitPosition;
            float walk = Mathf.InverseLerp(0, .24f, progress);
            if (progress < .24f)
            {
                var foot = Vector3.Lerp(terminal, StepStart, walk);
                animal.WalkAtRideStation(transform.TransformPoint(foot));
                return;
            }

            float firstStep = Ease(.24f, .44f, progress);
            float secondStep = Ease(.44f, .64f, progress);
            float enter = Ease(.64f, .84f, progress);
            float sit = Ease(.84f, 1, progress);
            var cabin = gondolas[dock];
            var destination = cabin.TransformPoint(SeatPosition);
            var door = transform.TransformPoint(DoorPosition);
            var position = progress < .44f ? Vector3.Lerp(transform.TransformPoint(StepStart),
                    transform.TransformPoint(StepPosition), firstStep) :
                progress < .64f ? Vector3.Lerp(transform.TransformPoint(StepPosition), door, secondStep) :
                Vector3.Lerp(door, destination, enter);
            float turn = boarding ? 180 * (1 - Ease(.74f, .98f, progress)) : 0;
            var facing = cabin.rotation * Quaternion.Euler(0, turn, 0);
            if (progress >= .64f && progress < .84f)
            {
                animal.WalkAtRideStation(position);
                animal.transform.rotation = facing;
                return;
            }
            animal.transform.SetPositionAndRotation(position, facing);
            if (progress < .64f)
                animal.Character.PlayRideStepPose(progress < .44f ? firstStep : secondStep, progress < .44f);
            else
                animal.Character.PlayRideSeatPose(sit);
        }

        static float Ease(float from, float to, float progress) =>
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, progress));
    }
}
