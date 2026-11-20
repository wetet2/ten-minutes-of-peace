using UnityEngine;

namespace PeacePark
{
    public sealed class ParkForestRide : MonoBehaviour, IParkAnimalRide
    {
        public enum Course { Stumps, Bridge, Zipline }
        enum Stage { Approach, Traverse, Grip, Glide, Dismount, Exit }
        public const float HandleDrop = .52f;

        [SerializeField] Course course;
        [SerializeField] Vector3 approach, cableStart, cableEnd;
        [SerializeField] Vector3[] treads, landing;
        [SerializeField] Transform trolley;

        ParkRabbitAgent rider;
        Stage stage;
        int step;
        float elapsed;
        Vector3 ZipDirection => Vector3.ProjectOnPlane(cableEnd - cableStart, Vector3.up).normalized;

        internal ParkAnimalAction Action => course switch
        {
            Course.Stumps => ParkAnimalAction.StumpHop,
            Course.Bridge => ParkAnimalAction.WoodenBridge,
            _ => ParkAnimalAction.Zipline
        };
        ParkAnimalAction IParkAnimalRide.Action => Action;
        internal Vector3 Approach => transform.TransformPoint(approach);
        internal Transform Trolley => trolley;
        internal bool IsAirborne { get; private set; }
        public bool IsMovingPart(Transform part) => trolley && part.IsChildOf(trolley);

        public void Configure(Course kind, Vector3 entry, Vector3[] steps)
        {
            course = kind;
            approach = entry;
            treads = steps;
        }

        public void ConfigureZipline(Transform carrier, Vector3 start, Vector3 end, Vector3[] exitSteps)
        {
            trolley = carrier;
            cableStart = start;
            cableEnd = end;
            landing = exitSteps;
            MoveTrolley(0);
        }

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || rider || treads == null || treads.Length == 0 ||
                !animal.TryReserveRequestedRide(this, Approach)) return false;
            rider = animal;
            Enter(Stage.Approach);
            return true;
        }

        internal void Advance(float dt)
        {
            if (!rider) return;
            elapsed += dt;
            switch (stage)
            {
                case Stage.Approach:
                    if (rider.WalkToRideMark(Approach, Facing(approach, treads[0]), dt)) Enter(Stage.Traverse);
                    break;
                case Stage.Traverse:
                    if (!AdvanceSteps(approach, treads, course == Course.Stumps, dt)) break;
                    if (course == Course.Zipline) Enter(Stage.Grip);
                    else Finish();
                    break;
                case Stage.Grip:
                    MoveWithHandle(treads[treads.Length - 1], true, .48f);
                    if (elapsed >= .48f) Enter(Stage.Glide);
                    break;
                case Stage.Glide:
                    float progress = Mathf.Clamp01(elapsed / 4.6f);
                    MoveTrolley(Mathf.SmoothStep(0, 1, progress));
                    SetHangingPose(HangingPosition(), 1, Mathf.Sin(progress * Mathf.PI * 6) * Mathf.Sin(progress * Mathf.PI));
                    IsAirborne = true;
                    if (progress >= 1) Enter(Stage.Dismount);
                    break;
                case Stage.Dismount:
                    MoveWithHandle(landing[0], false, .54f);
                    if (elapsed >= .54f)
                    {
                        Enter(Stage.Exit);
                        step = 1;
                    }
                    break;
                case Stage.Exit:
                    if (AdvanceSteps(landing[0], landing, false, dt)) Finish();
                    break;
            }
        }

        void Enter(Stage next)
        {
            stage = next;
            elapsed = 0;
            step = 0;
            IsAirborne = false;
        }

        bool AdvanceSteps(Vector3 origin, Vector3[] path, bool hop, float dt)
        {
            var from = step == 0 ? origin : path[step - 1];
            var to = path[step];
            float length = Vector3.Distance(from, to);
            float duration = hop ? Mathf.Clamp(length / 1.7f, .72f, 1.12f) : Mathf.Clamp(length / 1.46f, .30f, .95f);
            float progress = Mathf.Clamp01(elapsed / duration);
            float travel = hop ? Mathf.InverseLerp(.16f, .88f, progress) : progress;
            var point = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, travel));
            if (!hop) point.y += Mathf.Sin(Mathf.PI * progress) * (.055f + Mathf.Abs(to.y - from.y) * .48f);
            var rotation = Quaternion.RotateTowards(rider.transform.rotation, Facing(from, to), 400 * dt);
            rider.transform.SetPositionAndRotation(transform.TransformPoint(point), rotation);
            IsAirborne = hop && progress > .17f && progress < .81f;
            if (hop) rider.Character.PlayHopPose(progress);
            else rider.Character.PlayRideStepPose(progress, step % 2 == 0);
            if (progress < 1) return false;
            elapsed = 0;
            return ++step == path.Length;
        }

        Quaternion Facing(Vector3 from, Vector3 to) =>
            Quaternion.LookRotation(transform.TransformDirection(Vector3.ProjectOnPlane(from - to, Vector3.up).normalized));

        void MoveTrolley(float progress)
        {
            if (!trolley) return;
            trolley.SetPositionAndRotation(transform.TransformPoint(Vector3.Lerp(cableStart, cableEnd, progress)),
                Facing(cableStart, cableEnd));
        }

        Vector3 Handle => trolley.position - transform.up * HandleDrop;
        Vector3 HangingPosition() => Handle - transform.up * rider.Character.ZiplineGripHeight
            - transform.TransformDirection(ZipDirection) * .19f;

        void SetHangingPose(Vector3 point, float grip, float swing)
        {
            rider.transform.SetPositionAndRotation(point, Facing(cableStart, cableEnd));
            rider.Character.PlayZiplinePose(Handle, grip, swing);
        }

        void MoveWithHandle(Vector3 deck, bool boarding, float duration)
        {
            float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
            float grip = boarding ? progress : 1 - progress;
            SetHangingPose(Vector3.Lerp(transform.TransformPoint(deck), HangingPosition(), grip), grip, 0);
            IsAirborne = grip > .16f;
        }

        Vector3 ExitPoint => course == Course.Zipline ? landing[landing.Length - 1] : treads[treads.Length - 1];

        void Finish()
        {
            var animal = rider;
            rider = null;
            IsAirborne = false;
            MoveTrolley(0);
            animal.EndRide();
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if (rider != animal || stage != Stage.Approach) return false;
            rider = null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if (rider != animal) return;
            rider = null;
            IsAirborne = false;
            MoveTrolley(0);
        }

        void OnDisable()
        {
            if (!rider) return;
            if (stage != Stage.Approach) rider.transform.position = transform.TransformPoint(ExitPoint);
            Finish();
        }
    }
}
