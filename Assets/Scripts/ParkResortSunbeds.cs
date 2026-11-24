using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkResortSunbeds : MonoBehaviour, IParkAnimalRide
    {
        const float TransitionSeconds = 1.2f;
        const float RestSeconds = 5f;
        static readonly Quaternion ApproachFacing = Quaternion.LookRotation(Vector3.back);
        static readonly Quaternion RestFacing = Quaternion.LookRotation(Vector3.left);

        [Serializable]
        public struct Spot
        {
            public Vector3 Approach, Cushion;
        }

        enum Stage { Approach, LieDown, Rest, GetUp }

        sealed class Rider
        {
            public ParkRabbitAgent Animal;
            public Stage Stage;
            public float Time;
            public Vector3 PoseOffset;
        }

        [SerializeField] Spot[] spots;
        Rider[] riders;
        readonly Dictionary<AnimalKind, Vector3> poseOffsets = new Dictionary<AnimalKind, Vector3>();
        Mesh poseSample;

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Sunbed;
        internal int Capacity => spots?.Length ?? 0;
        internal bool IsResting(ParkRabbitAgent animal)
        {
            int index = FindRider(animal);
            return index >= 0 && riders[index].Stage == Stage.Rest;
        }

        public void Configure(Spot[] stations) => spots = stations;

        internal bool TryQueue(ParkPlayground park, ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || Capacity == 0) return false;
            riders ??= new Rider[spots.Length];
            var origin = animal.transform.position;
            float nearest = float.PositiveInfinity;
            int available = -1;
            Vector3[] route = null;
            for (int i = 0; i < spots.Length; i++)
            {
                if (riders[i] != null) continue;
                var mark = transform.TransformPoint(spots[i].Approach);
                float distance = (mark - origin).sqrMagnitude;
                if (distance >= nearest || !park.TryBuildRidePath(origin, mark, out var candidateRoute)) continue;
                available = i;
                nearest = distance;
                route = candidateRoute;
            }
            if (available < 0) return false;
            animal.ReserveRide(this, route);
            riders[available] = new Rider { Animal = animal };
            return true;
        }

        internal void Advance(float dt)
        {
            if (riders == null) return;
            for (int i = 0; i < riders.Length; i++)
            {
                var rider = riders[i];
                if (rider == null) continue;
                if (!rider.Animal) { riders[i] = null; continue; }
                var spot = spots[i];
                rider.Time += dt;
                switch (rider.Stage)
                {
                    case Stage.Approach:
                        if (!rider.Animal.WalkToRideMark(transform.TransformPoint(spot.Approach), transform.rotation * ApproachFacing, dt)) break;
                        var character = rider.Animal.Character;
                        if (!poseOffsets.TryGetValue(character.Kind, out rider.PoseOffset))
                        {
                            poseSample ??= new Mesh { name = "Sunbed pose sample" };
                            rider.PoseOffset = character.MeasureSunbedPose(poseSample);
                            poseOffsets.Add(character.Kind, rider.PoseOffset);
                        }
                        rider.Animal.BeginRideActivity();
                        Begin(rider, Stage.LieDown);
                        break;
                    case Stage.LieDown:
                        PoseRider(rider, spot, rider.Time / TransitionSeconds);
                        if (rider.Time >= TransitionSeconds) Begin(rider, Stage.Rest);
                        break;
                    case Stage.Rest:
                        PoseRider(rider, spot, 1, rider.Time);
                        if (rider.Time >= RestSeconds) Begin(rider, Stage.GetUp);
                        break;
                    case Stage.GetUp:
                        PoseRider(rider, spot, 1 - rider.Time / TransitionSeconds);
                        if (rider.Time >= TransitionSeconds) Finish(i);
                        break;
                }
            }
        }

        void PoseRider(Rider rider, Spot spot, float progress, float breathing = 0)
        {
            float t = Mathf.SmoothStep(0, 1, progress);
            var point = transform.TransformPoint(Vector3.Lerp(spot.Approach, spot.Cushion, t));
            point += Vector3.up * (.37f * Mathf.Sin(Mathf.PI * t));
            rider.Animal.transform.SetPositionAndRotation(point, transform.rotation * Quaternion.Slerp(ApproachFacing, RestFacing, t));
            float recline = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.16f, .92f, t));
            rider.Animal.Character.PlaySunbedPose(recline, breathing, rider.PoseOffset);
        }

        static void Begin(Rider rider, Stage stage)
        {
            rider.Stage = stage;
            rider.Time = 0;
        }

        int FindRider(ParkRabbitAgent animal)
        {
            if (riders != null)
                for (int i = 0; i < riders.Length; i++)
                    if (riders[i]?.Animal == animal) return i;
            return -1;
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            int index = FindRider(animal);
            if (index < 0 || riders[index].Stage != Stage.Approach) return false;
            riders[index] = null;
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            int index = FindRider(animal);
            if (index >= 0) riders[index] = null;
        }

        void Finish(int index)
        {
            var rider = riders[index];
            riders[index] = null;
            if (rider == null || !rider.Animal) return;
            if (rider.Stage != Stage.Approach) rider.Animal.transform.position = transform.TransformPoint(spots[index].Approach);
            rider.Animal.EndRide();
        }

        void OnDisable()
        {
            if (riders == null) return;
            for (int i = 0; i < riders.Length; i++) Finish(i);
        }

        void OnDestroy()
        {
            if (poseSample) Destroy(poseSample);
        }
    }
}
