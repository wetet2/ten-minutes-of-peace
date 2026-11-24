using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace PeacePark
{
    public sealed class ParkVillageDeer : MonoBehaviour, IParkAnimalRide
    {
        const float MountSeconds = 1.05f;
        const float RideSeconds = 10;
        const float BoardOffset = 1.55f;
        const float BodyHalfLength = .82f;
        const float AnimalClearance = .98f;
        [SerializeField] Animator animator;
        [SerializeField] AnimationClip idleClip, walkClip;
        [SerializeField, FormerlySerializedAs("saddle")] Transform backAnchor;
        [SerializeField] Vector2[] waypoints;
        [SerializeField] float strideSpeed, startingPhase;

        enum Stage { Roam, Approach, Mount, Riding, Dismount }
        Stage stage;
        float elapsed, transitionTime, walkBlend;
        Vector3 target, groundPoint;
        bool hasTarget;
        ParkRabbitAgent rider;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable walk;

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Deer;
        internal bool CanBoard => isActiveAndEnabled && graph.IsValid() && backAnchor && !rider;
        internal ParkRabbitAgent Rider => rider;
        internal bool IsRiding => stage == Stage.Riding && rider;
        internal Vector3 Seat => backAnchor.position;

        internal Vector3 OffsetFromBody(Vector3 point) => BodyOffset(point, transform.position, transform.forward);
        internal float AnimalPenetration(Vector3 point) => Mathf.Max(0, AnimalClearance - OffsetFromBody(point).magnitude);

        static Vector3 BodyOffset(Vector3 point, Vector3 position, Vector3 forward)
        {
            var offset = point - position;
            offset.y = 0;
            float along = Mathf.Clamp(Vector3.Dot(offset, forward), -BodyHalfLength, BodyHalfLength);
            return offset - forward * along;
        }

        float GroundOverlap(ParkPlayground park, Vector3 position, Quaternion facing)
        {
            float cost = 0;
            var forward = facing * Vector3.forward;
            foreach (var animal in park.Animals)
            {
                if (!animal || animal == rider || animal.State == ParkRabbitState.OpeningDoor) continue;
                float gap = AnimalClearance - BodyOffset(animal.transform.position, position, forward).magnitude;
                if (gap > 0) cost += gap * gap;
            }
            return cost;
        }

        public void Configure(Animator rig, AnimationClip idle, AnimationClip walking, Transform seat,
            Vector2[] route, float speed, float phase)
        {
            animator = rig;
            idleClip = idle;
            walkClip = walking;
            backAnchor = seat;
            waypoints = route;
            strideSpeed = speed;
            startingPhase = phase;
            PlaceAtStart();
        }

        void OnEnable()
        {
            if (waypoints == null || waypoints.Length < 3 || !animator || !idleClip || !walkClip) return;
            PlaceAtStart();
            elapsed = startingPhase * 24;
            walkBlend = 1;
            graph = PlayableGraph.Create("Village deer animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var idle = AnimationClipPlayable.Create(graph, idleClip);
            walk = AnimationClipPlayable.Create(graph, walkClip);
            graph.Connect(idle, 0, mixer, 0);
            graph.Connect(walk, 0, mixer, 1);
            mixer.SetInputWeight(1, 1);
            AnimationPlayableOutput.Create(graph, "Deer", animator).SetSourcePlayable(mixer);
            graph.Play();
        }

        void PlaceAtStart()
        {
            int index = Mathf.FloorToInt(waypoints.Length * startingPhase) % waypoints.Length;
            var origin = LocalPoint(waypoints[index]);
            var next = LocalPoint(waypoints[(index + 1) % waypoints.Length]);
            transform.SetLocalPositionAndRotation(origin, Quaternion.LookRotation(next - origin));
            hasTarget = false;
        }

        void OnDisable()
        {
            var animal = rider;
            if (animal && stage >= Stage.Mount)
                animal.transform.position = stage == Stage.Riding ? transform.position : groundPoint;
            ClearRider();
            if (animal) animal.EndRide();
            if (graph.IsValid()) graph.Destroy();
        }

        internal Vector3 BoardingPoint(ParkPlayground park, int side)
        {
            var point = transform.position + transform.right * (side * BoardOffset);
            point.y = park.SurfaceHeight(point);
            return point;
        }

        internal void Reserve(ParkRabbitAgent animal, Vector3 point, Vector3[] path)
        {
            rider = animal;
            groundPoint = point;
            animal.ReserveRide(this, path);
            stage = Stage.Approach;
        }

        internal void Advance(ParkPlayground park, float dt)
        {
            if (!graph.IsValid()) return;
            elapsed += dt;
            switch (stage)
            {
                case Stage.Roam:
                    if (elapsed % 18f < 15.4f) Walk(park, dt);
                    else AnimateWalk(0, dt);
                    break;
                case Stage.Approach:
                    AnimateWalk(0, dt);
                    if (rider && rider.WalkToRideMark(groundPoint, GroundFacing, dt, speed: 2.25f))
                    {
                        rider.BeginRideActivity();
                        transitionTime = 0;
                        stage = Stage.Mount;
                    }
                    break;
                case Stage.Mount:
                    transitionTime += dt;
                    TransitionRider(Mathf.SmoothStep(0, 1, transitionTime / MountSeconds), true);
                    if (transitionTime >= MountSeconds)
                    {
                        transitionTime = 0;
                        hasTarget = false;
                        stage = Stage.Riding;
                    }
                    break;
                case Stage.Riding:
                    transitionTime += dt;
                    if (transitionTime >= RideSeconds && TryDismountPoint(park, out var landing))
                    {
                        groundPoint = landing;
                        transitionTime = 0;
                        stage = Stage.Dismount;
                        AnimateWalk(0, dt);
                    }
                    else Walk(park, dt);
                    TransitionRider(1, true);
                    break;
                case Stage.Dismount:
                    AnimateWalk(0, dt);
                    transitionTime += dt;
                    TransitionRider(1 - Mathf.SmoothStep(0, 1, transitionTime / MountSeconds), false);
                    if (transitionTime >= MountSeconds)
                    {
                        var animal = rider;
                        ClearRider();
                        if (animal) animal.EndRide();
                    }
                    break;
            }
        }

        void LateUpdate()
        {
            // Match the animated back after the walk clip updates its bones.
            if (IsRiding) TransitionRider(1, true);
        }

        void Walk(ParkPlayground park, float dt)
        {
            var position = transform.position;
            if (!hasTarget) SelectDestination(park, position);
            if (!hasTarget)
            {
                AnimateWalk(0, dt);
                return;
            }

            var heading = target - position;
            var facing = Quaternion.LookRotation(heading);
            var rotation = Quaternion.RotateTowards(transform.rotation, facing, dt * 130);
            float pace = 1 - Mathf.InverseLerp(8, 35, Quaternion.Angle(rotation, facing));
            pace *= Mathf.Lerp(.48f, 1, heading.magnitude / .9f);
            float stride = strideSpeed * pace * Mathf.MoveTowards(walkBlend, 1, dt * 3) * dt;
            var next = Vector3.MoveTowards(position, target, stride);
            float overlap = GroundOverlap(park, next, rotation);
            if (overlap > 0 && overlap >= GroundOverlap(park, position, transform.rotation))
            {
                hasTarget = false;
                AnimateWalk(0, dt);
                return;
            }
            AnimateWalk(pace, dt);
            transform.SetPositionAndRotation(next, rotation);
            if ((target - transform.position).sqrMagnitude < .012f) hasTarget = false;
        }

        void SelectDestination(ParkPlayground park, Vector3 position)
        {
            int start = Random.Range(0, waypoints.Length);
            for (int i = 0; i < waypoints.Length; i++)
            {
                var point = transform.parent.TransformPoint(LocalPoint(waypoints[(start + i) % waypoints.Length]));
                var direction = point - position;
                if (direction.sqrMagnitude < 2.25f) continue;
                var clearance = Vector3.Cross(Vector3.up, direction.normalized) * .55f;
                if (!park.WalkingSegmentClear(position, point) ||
                    !park.WalkingSegmentClear(position + clearance, point + clearance) ||
                    !park.WalkingSegmentClear(position - clearance, point - clearance)) continue;
                var opening = Vector3.MoveTowards(position, point, 1.07f);
                if (GroundOverlap(park, opening, Quaternion.LookRotation(direction)) > 0) continue;
                target = point;
                hasTarget = true;
                return;
            }
        }

        void AnimateWalk(float pace, float dt)
        {
            walkBlend = Mathf.MoveTowards(walkBlend, pace > 0 ? 1 : 0, dt * 3);
            mixer.SetInputWeight(0, 1 - walkBlend);
            mixer.SetInputWeight(1, walkBlend);
            walk.SetSpeed(pace > 0 ? pace : .35f);
        }

        bool TryDismountPoint(ParkPlayground park, out Vector3 point)
        {
            for (int i = 0; i < 8; i++)
            {
                int side = i % 2 == 0 ? 1 : -1;
                var offset = Quaternion.Euler(0, (i / 2) * 45, 0) * transform.right * (side * BoardOffset);
                point = transform.position + offset;
                point.y = park.SurfaceHeight(point);
                if (park.WalkArea.Contains(new Vector2(point.x, point.z)) &&
                    park.WalkingSegmentClear(transform.position, point) &&
                    park.IsClear(point, rider, ParkRabbitAgent.AnimalSpacing)) return true;
            }
            point = default;
            return false;
        }

        Quaternion GroundFacing => Quaternion.LookRotation(Vector3.ProjectOnPlane(groundPoint - transform.position, Vector3.up));

        void TransitionRider(float seated, bool boarding)
        {
            if (!rider) return;
            var ridingFacing = Quaternion.LookRotation(-transform.forward);
            rider.transform.rotation = seated >= 1 ? ridingFacing :
                Quaternion.Slerp(boarding ? GroundFacing : GroundFacing * Quaternion.Euler(0, 180, 0), ridingFacing, seated);
            var character = rider.Character;
            var hipOffset = character.PlayStraddleRidePose(seated);
            var root = Vector3.Lerp(groundPoint, Seat - hipOffset, seated);
            root.y += .32f * Mathf.Sin(seated * Mathf.PI);
            rider.transform.position = root;
            float grip = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 1, seated));
            if (grip > 0)
                character.GripRideHandle(Seat + transform.forward * .52f + Vector3.up * .25f, grip);
        }

        void ClearRider()
        {
            rider = null;
            stage = Stage.Roam;
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            if (rider != animal || stage != Stage.Approach) return false;
            ClearRider();
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            if (rider == animal) ClearRider();
        }

        static Vector3 LocalPoint(Vector2 point) => new Vector3(point.x, .025f, point.y);
    }
}
