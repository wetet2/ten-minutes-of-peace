using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkSandSled : MonoBehaviour, IParkAnimalRide
    {
        const float TowGap = 2.65f;
        const float ClimbSpeed = 7.1f;
        const float SlideSeconds = 6.1f;
        const float ClimbSpacing = 7.5f;
        const float GlideSpacing = 4.2f;
        const float CrestClearance = 6.4f;
        const float GroundClearance = .022f;
        public const float RunnerHalfSpacing = .78f;
        internal const float RearRunnerContact = -1.14f, FrontRunnerContact = .69f;
        static readonly Vector2 Base = new Vector2(-17.3f, -15.2f);
        static readonly Vector2 TowBase = new Vector2(-17.3f, -17.24f);
        static readonly Vector2 Arrival = ParkDuneTerrain.SledLanding;
        static readonly Vector2 Crest = ParkDuneTerrain.SledDuneCrest;
        static readonly Vector2 DescentDirection = ParkDuneTerrain.SledDescentDirection;
        static readonly SandRoute Up = new SandRoute(TowBase, new Vector2(-41, -16), new Vector2(-51, 18), Crest);
        static readonly SandRoute Down = new SandRoute(Crest, Vector2.Lerp(Crest, Arrival, 1f / 3),
            Vector2.Lerp(Crest, Arrival, 2f / 3), Arrival);
        static readonly Vector3 ExitPoint = OnSand(Arrival + new Vector2(-DescentDirection.y, DescentDirection.x) * 2.3f);

        [SerializeField] Transform pickupRig, parkedSled, parkedRope;

        enum Stage { Approach, Grab, Pull, Sit, Slide, Stand }
        readonly List<Ride> rides = new List<Ride>();

        sealed class Ride
        {
            public readonly ParkRabbitAgent Animal;
            public Stage Phase;
            public Transform Rig, Sled, Rope;
            public ParticleSystem Dust;
            public float Elapsed, Distance, Speed, DustBudget;
            public Vector3 TransitionPosition, StartSledPosition;
            public Quaternion TransitionFacing, StartSledRotation;

            public Ride(ParkRabbitAgent animal) => Animal = animal;

            public void Enter(Stage next)
            {
                Elapsed = 0;
                Phase = next;
            }

            public void SaveTransition()
            {
                TransitionPosition = Animal.transform.position;
                TransitionFacing = Animal.transform.rotation;
                StartSledPosition = Sled.position;
                StartSledRotation = Sled.rotation;
            }

            public float Ease(float duration) => Mathf.SmoothStep(0, 1, Elapsed / duration);
        }

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.SandSled;
        internal Vector3 Approach => ToWorld(Up.At(TowGap));
        internal Transform Sled => parkedSled;
        internal int ActiveSledCount
        {
            get
            {
                int count = 0;
                foreach (var ride in rides)
                    if (ride.Rig) count++;
                return count;
            }
        }

        internal Transform SledFor(ParkRabbitAgent animal) => FindRide(animal)?.Sled;
        internal bool IsPulling(ParkRabbitAgent animal) => FindRide(animal)?.Phase == Stage.Pull;
        internal bool IsSliding(ParkRabbitAgent animal) => FindRide(animal)?.Phase == Stage.Slide;
        internal bool IsDismounting(ParkRabbitAgent animal) => FindRide(animal)?.Phase == Stage.Stand;

        public static Vector3 PickupPoint => OnSand(Base);

        public void Configure(Transform rig, Transform vehicle, Transform rope)
        {
            pickupRig = rig;
            parkedSled = vehicle;
            parkedRope = rope;
            var facing = Up.Tangent(0);
            parkedSled.SetPositionAndRotation(ToWorld(PickupPoint) + transform.up * GroundClearance, VehicleRotation(facing));
            var displayLead = Base + new Vector2(facing.x, facing.z).normalized * TowGap;
            DrawRope(parkedRope, Nose(parkedSled), ToWorld(OnSand(displayLead)) + transform.up * .12f);
        }

        public bool IsMovingPart(Transform part) => part.IsChildOf(pickupRig);

        public static Vector3 CoursePoint(bool downhill, float progress)
        {
            var route = downhill ? Down : Up;
            return route.At(route.Length * Mathf.Clamp01(progress));
        }

        public static bool InCourseCorridor(Vector2 point, float clearance)
        {
            return Up.Near(point, clearance) || Down.Near(point, clearance) ||
                (point - Base).sqrMagnitude < clearance * clearance ||
                (point - new Vector2(ExitPoint.x, ExitPoint.z)).sqrMagnitude < clearance * clearance;
        }

        internal bool TryQueue(ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || !pickupRig || FindRide(animal) != null) return false;
            if (rides.Count > 0)
            {
                var last = rides[rides.Count - 1];
                if (last.Phase < Stage.Pull || (last.Phase == Stage.Pull && last.Distance < ClimbSpacing)) return false;
            }
            if (!animal.TryReserveRequestedRide(this, Approach)) return false;
            rides.Add(new Ride(animal));
            return true;
        }

        internal void Advance(float dt)
        {
            for (int i = 0; i < rides.Count;)
            {
                var ride = rides[i];
                var ahead = i == 0 ? null : rides[i - 1];
                if (ride.Animal && !AdvanceRide(ride, ahead, dt))
                {
                    i++;
                    continue;
                }
                rides.RemoveAt(i);
                DisposeRide(ride, finish: true);
            }
        }

        bool AdvanceRide(Ride ride, Ride ahead, float dt)
        {
            ride.Elapsed += dt;
            var animal = ride.Animal;
            var character = animal.Character;
            switch (ride.Phase)
            {
                case Stage.Approach:
                    if (animal.WalkToRideMark(Approach, Facing(Up.Tangent(TowGap)), dt, speed: 2.65f))
                    {
                        SpawnSled(ride);
                        ride.Enter(Stage.Grab);
                    }
                    break;
                case Stage.Grab:
                    float reach = ride.Ease(.44f);
                    character.PlayIdlePose();
                    DrawRope(ride.Rope, Nose(ride.Sled), Vector3.Lerp(Approach + transform.up * .12f, character.GripSandSledRope(reach), reach));
                    if (reach >= 1) ride.Enter(Stage.Pull);
                    break;
                case Stage.Pull:
                    ride.Speed = Mathf.MoveTowards(ride.Speed, ClimbSpeed, dt * 12);
                    float limit = UphillLimit(ahead);
                    float previous = ride.Distance;
                    ride.Distance = Mathf.MoveTowards(previous, Mathf.Max(previous, limit), ride.Speed * dt);
                    if (ride.Distance > previous)
                    {
                        PoseUphill(ride.Sled, ride.Distance);
                        animal.WalkAtRideStation(ToWorld(Up.At(ride.Distance + TowGap)), cadenceScale: .48f);
                    }
                    else character.PlayIdlePose();
                    DrawRope(ride.Rope, Nose(ride.Sled), character.GripSandSledRope(1));
                    if (ride.Distance >= Up.Length)
                    {
                        ride.SaveTransition();
                        ride.Enter(Stage.Sit);
                    }
                    break;
                case Stage.Sit:
                    float board = ride.Ease(.92f);
                    var launch = DownhillPose(0);
                    ride.Sled.SetPositionAndRotation(Vector3.Lerp(ride.StartSledPosition, launch.position, board),
                        Quaternion.Slerp(ride.StartSledRotation, launch.rotation, board));
                    animal.transform.SetPositionAndRotation(Vector3.Lerp(ride.TransitionPosition, SeatPosition(ride.Sled), board),
                        Quaternion.Slerp(ride.TransitionFacing, SeatedFacing(ride.Sled), board));
                    character.PlayRideSeatPose(board);
                    StowRope(ride);
                    if (board >= 1)
                    {
                        ride.Distance = 0;
                        if (ride.Dust) ride.Dust.Play();
                        ride.Enter(Stage.Slide);
                    }
                    break;
                case Stage.Slide:
                    float progress = Mathf.Clamp01(ride.Elapsed / SlideSeconds);
                    float desired = Down.Length * Mathf.SmoothStep(0, 1, progress);
                    float ceiling = DownhillLimit(ahead);
                    ride.Distance = Mathf.Max(ride.Distance, Mathf.Min(desired, ceiling));
                    if (desired > ride.Distance)
                    {
                        // Resume the easing curve at the distance the sled could actually travel.
                        float travelled = ride.Distance / Down.Length;
                        progress = .5f - Mathf.Sin(Mathf.Asin(1 - 2 * travelled) / 3);
                        ride.Elapsed = progress * SlideSeconds;
                    }
                    var downhill = DownhillPose(ride.Distance);
                    ride.Sled.SetPositionAndRotation(downhill.position, downhill.rotation);
                    animal.transform.SetPositionAndRotation(SeatPosition(ride.Sled), SeatedFacing(ride.Sled));
                    character.PlayRideSeatPose(1);
                    StowRope(ride);
                    EmitDust(ride, dt, progress);
                    if (ride.Distance >= Down.Length)
                    {
                        if (ride.Dust) ride.Dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        ride.SaveTransition();
                        ride.Enter(Stage.Stand);
                    }
                    break;
                case Stage.Stand:
                    float rise = ride.Ease(.92f);
                    var exit = ToWorld(ExitPoint);
                    var direction = Vector3.ProjectOnPlane(exit - ride.TransitionPosition, transform.up);
                    animal.transform.SetPositionAndRotation(Vector3.Lerp(ride.TransitionPosition, exit, rise),
                        Quaternion.Slerp(ride.TransitionFacing, Quaternion.LookRotation(-direction, transform.up), rise));
                    character.PlayRideSeatPose(1 - rise);
                    return rise >= 1;
            }
            return false;
        }

        static float UphillLimit(Ride ahead)
        {
            if (ahead == null) return Up.Length;
            if (ahead.Phase == Stage.Pull) return ahead.Distance - ClimbSpacing;
            if (ahead.Phase == Stage.Sit || (ahead.Phase == Stage.Slide && ahead.Distance < CrestClearance))
                return Up.Length - ClimbSpacing;
            return Up.Length;
        }

        static float DownhillLimit(Ride ahead)
        {
            if (ahead == null) return Down.Length;
            if (ahead.Phase == Stage.Stand) return Down.Length - GlideSpacing;
            return ahead.Phase == Stage.Slide ? Mathf.Max(0, ahead.Distance - GlideSpacing) : 0;
        }

        void SpawnSled(Ride ride)
        {
            // The departure lane is beside the parked sled so both copies have room to be visible.
            var copy = Instantiate(pickupRig, transform, false);
            copy.name = "Rider's sand sled";
            ride.Rig = copy;
            ride.Sled = copy.GetChild(parkedSled.GetSiblingIndex());
            ride.Rope = copy.GetChild(parkedRope.GetSiblingIndex());
            ride.Dust = copy.GetComponentInChildren<ParticleSystem>();
            PoseUphill(ride.Sled, 0);
            DrawRope(ride.Rope, Nose(ride.Sled), Approach + transform.up * .12f);
        }

        static Vector3 Nose(Transform sled) => sled.TransformPoint(new Vector3(0, .29f, 1.22f));
        static Vector3 SeatPosition(Transform sled) => sled.TransformPoint(new Vector3(0, .10f, -.22f));
        static Quaternion SeatedFacing(Transform sled) => sled.rotation * Quaternion.Euler(0, 180, 0);
        Vector3 ToWorld(Vector3 local) => transform.TransformPoint(local);
        Quaternion Facing(Vector3 localDirection) => Quaternion.LookRotation(-transform.TransformDirection(
            Vector3.ProjectOnPlane(localDirection, Vector3.up).normalized), transform.up);
        Quaternion VehicleRotation(Vector3 localDirection) => Quaternion.LookRotation(transform.TransformDirection(localDirection), transform.up);

        void PoseUphill(Transform sled, float travel) => sled.SetPositionAndRotation(
            ToWorld(Up.At(travel)) + transform.up * GroundClearance, VehicleRotation(Up.Tangent(travel)));

        Pose DownhillPose(float travel)
        {
            var center = Down.At(travel);
            var xz = new Vector2(center.x, center.z);
            var rear = OnSand(xz + DescentDirection * RearRunnerContact);
            var front = OnSand(xz + DescentDirection * FrontRunnerContact);
            var forward = (front - rear).normalized;

            // Seat the rigid runners on the grade without digging into convex parts of the slope.
            center.y = Mathf.Max(center.y, Mathf.Max(RequiredHeight(RearRunnerContact), RequiredHeight(FrontRunnerContact)));
            center.y += GroundClearance;
            return new Pose(ToWorld(center), VehicleRotation(forward));

            float RequiredHeight(float runnerZ)
            {
                var contact = center + forward * runnerZ;
                return ParkDuneTerrain.Height(contact.x, contact.z) - forward.y * runnerZ;
            }
        }

        static void DrawRope(Transform rope, Vector3 start, Vector3 end)
        {
            var line = end - start;
            rope.SetPositionAndRotation((start + end) / 2, Quaternion.FromToRotation(Vector3.up, line));
            rope.localScale = new Vector3(.048f, line.magnitude / 2, .048f);
        }

        static void StowRope(Ride ride) => DrawRope(ride.Rope, ride.Sled.TransformPoint(new Vector3(-.45f, .34f, 1.18f)),
            ride.Sled.TransformPoint(new Vector3(.45f, .34f, 1.18f)));

        static void EmitDust(Ride ride, float dt, float progress)
        {
            if (!ride.Dust) return;
            ride.Dust.transform.position = ride.Sled.TransformPoint(new Vector3(0, .12f, -1.28f));
            ride.DustBudget += Mathf.Sin(Mathf.PI * progress) * dt * 28;
            int grains = Mathf.FloorToInt(ride.DustBudget);
            if (grains == 0) return;
            ride.DustBudget -= grains;
            ride.Dust.Emit(grains);
        }

        Ride FindRide(ParkRabbitAgent animal)
        {
            foreach (var ride in rides)
                if (ride.Animal == animal) return ride;
            return null;
        }

        void DisposeRide(Ride ride, bool finish)
        {
            if (ride.Rig)
            {
                ride.Rig.gameObject.SetActive(false);
                Destroy(ride.Rig.gameObject);
            }
            if (finish && ride.Animal) ride.Animal.EndRide();
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            var ride = FindRide(animal);
            return ride != null && ride.Phase == Stage.Approach && rides.Remove(ride);
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            var ride = FindRide(animal);
            if (ride == null) return;
            rides.Remove(ride);
            DisposeRide(ride, finish: false);
        }

        void OnDisable()
        {
            foreach (var ride in rides)
            {
                if (ride.Animal && ride.Rig) ride.Animal.transform.position = ToWorld(ExitPoint);
                DisposeRide(ride, finish: true);
            }
            rides.Clear();
        }

        static Vector3 OnSand(Vector2 point) => new Vector3(point.x, ParkDuneTerrain.Height(point.x, point.y), point.y);

        sealed class SandRoute
        {
            readonly Vector3[] points;
            readonly float[] mileage;
            public float Length => mileage[mileage.Length - 1];

            public SandRoute(params Vector2[] controls)
            {
                const int segments = 120;
                var samples = new List<Vector3> { OnSand(controls[0]) };
                for (int i = 0; i + 3 < controls.Length; i += 3)
                for (int j = 1; j <= segments; j++)
                {
                    float t = (float)j / segments, s = 1 - t;
                    var point = controls[i] * (s * s * s) + controls[i + 1] * (3 * s * s * t) +
                        controls[i + 2] * (3 * s * t * t) + controls[i + 3] * (t * t * t);
                    samples.Add(OnSand(point));
                }
                points = samples.ToArray();
                mileage = new float[points.Length];
                for (int i = 1; i < points.Length; i++) mileage[i] = mileage[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            }

            public Vector3 At(float travel)
            {
                if (travel <= 0) return points[0];
                if (travel >= Length)
                {
                    var end = points[points.Length - 1];
                    var direction = Vector3.ProjectOnPlane(end - points[points.Length - 2], Vector3.up).normalized;
                    var projected = end + direction * (travel - Length);
                    return OnSand(new Vector2(projected.x, projected.z));
                }
                int next = Array.BinarySearch(mileage, travel);
                if (next >= 0) return points[next];
                next = ~next;
                var point = Vector3.Lerp(points[next - 1], points[next], Mathf.InverseLerp(mileage[next - 1], mileage[next], travel));
                return OnSand(new Vector2(point.x, point.z));
            }

            public Vector3 Tangent(float travel) => At(Mathf.Min(travel + .38f, Length)) - At(Mathf.Max(0, travel - .38f));

            public bool Near(Vector2 point, float clearance)
            {
                float limit = clearance * clearance;
                foreach (var sample in points)
                    if ((point - new Vector2(sample.x, sample.z)).sqrMagnitude < limit) return true;
                return false;
            }
        }
    }
}
