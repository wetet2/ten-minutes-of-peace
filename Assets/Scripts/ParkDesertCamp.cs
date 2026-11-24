using System;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkDesertCamp : MonoBehaviour, IParkAnimalRide
    {
        const float PicnicSeconds = 15.6f;
        const float SnackSeconds = 2.35f;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [Serializable]
        public struct Place
        {
            public Transform Approach, Cushion;
        }

        [SerializeField] Place[] places;
        [SerializeField] Transform tentApproach, tentInside, tentDoor, mat, snack, fireFlames;
        [SerializeField] Renderer lanternGlass;
        [SerializeField] Light lantern;
        [SerializeField] ParkHouse tentArrival;

        enum Stage { Approach, EnterTent, Peek, ExitTent, ApproachMat, Settle, Picnic, Stand, Leave }

        sealed class Visit
        {
            public readonly ParkRabbitAgent Animal;
            public readonly Place Place;
            public readonly Vector3[] MatRoute;
            public readonly bool StartsAtTent;
            public Stage Stage;
            public float Time;
            public bool Ate;

            public Visit(ParkRabbitAgent animal, Place place, Vector3[] matRoute, bool startsAtTent)
            {
                Animal = animal;
                Place = place;
                MatRoute = matRoute;
                StartsAtTent = startsAtTent;
            }

            public void Enter(Stage next)
            {
                Stage = next;
                Time = 0;
            }
        }

        Visit[] visits;
        Visit tentGuest, snackGiver, snackGuest;
        float snackTime, fireTime;
        ParkDuneTerrain terrain;
        MaterialPropertyBlock properties;
        bool lampOn;

        ParkAnimalAction IParkAnimalRide.Action => ParkAnimalAction.Camp;
        internal bool TentReserved => tentGuest != null;
        internal Vector3 TentEntrance => tentApproach.position;
        internal int SeatedCount
        {
            get
            {
                int seated = 0;
                foreach (var visit in visits)
                    if (visit?.Stage == Stage.Picnic) seated++;
                return seated;
            }
        }
        internal bool IsSharingSnack => snackGiver != null && snackGuest != null;
        internal bool IsPeeking(ParkRabbitAgent animal) => Array.Exists(visits,
            visit => visit?.Stage == Stage.Peek && visit.Animal == animal);
        internal bool IsSeated(ParkRabbitAgent animal) => Array.Exists(visits,
            visit => visit?.Stage == Stage.Picnic && visit.Animal == animal);

        public void Configure(Place[] seats, Transform entrance, Transform inside, Transform door, Transform rug,
            Transform cookie, Renderer glass, Light light, Transform flames, ParkHouse arrival)
        {
            places = seats;
            tentApproach = entrance;
            tentInside = inside;
            tentDoor = door;
            mat = rug;
            snack = cookie;
            lanternGlass = glass;
            lantern = light;
            fireFlames = flames;
            tentArrival = arrival;
        }

        public bool IsMovingPart(Transform part)
        {
            if (part == lanternGlass.transform || part.IsChildOf(snack) || (fireFlames && part.IsChildOf(fireFlames))) return true;
            foreach (var place in places)
                if (part.IsChildOf(place.Cushion)) return true;
            return false;
        }

        void OnEnable()
        {
            if (places == null) return;
            terrain = GetComponentInParent<ParkDuneTerrain>();
            visits = new Visit[places.Length];
            foreach (var place in places) place.Cushion.gameObject.SetActive(false);
            snack.gameObject.SetActive(false);
            fireTime = 0;
            lampOn = true;
            SetLantern(false);
        }

        internal bool TryQueue(ParkPlayground park, ParkRabbitAgent animal)
        {
            if (!isActiveAndEnabled || visits == null || Array.IndexOf(visits, null) < 0 ||
                Array.Exists(visits, visit => visit?.Animal == animal)) return false;
            Vector3[] tentRoute = null;
            bool canVisitTent = !TentReserved && (!tentArrival || (!tentArrival.IsBusy && !park.HasWaitingArrival(tentArrival))) &&
                park.TryBuildRidePath(animal.transform.position, TentEntrance, out tentRoute);
            for (int i = 0; i < places.Length; i++)
            {
                if (visits[i] != null) continue;
                var place = places[i];
                if (canVisitTent && park.TryBuildRidePath(TentEntrance, place.Approach.position, out var afterTent))
                {
                    animal.ReserveRide(this, tentRoute);
                    visits[i] = tentGuest = new Visit(animal, place, afterTent, true);
                    return true;
                }
                if (!animal.TryReserveRequestedRide(this, place.Approach.position)) continue;
                visits[i] = new Visit(animal, place, null, false);
                return true;
            }
            return false;
        }

        internal void Advance(float dt)
        {
            bool lit = false;
            for (int i = 0; i < visits.Length; i++)
            {
                var visit = visits[i];
                if (visit == null) continue;
                if (!visit.Animal || AdvanceVisit(visit, dt)) ClearVisit(i, true);
                else lit |= visit.Stage <= Stage.Stand && visit.Place.Cushion.gameObject.activeSelf;
            }
            SetLantern(lit);
            AnimateCampfire(dt);
            AdvanceSnack(dt);
        }

        bool AdvanceVisit(Visit visit, float dt)
        {
            visit.Time += dt;
            var animal = visit.Animal;
            var character = animal.Character;
            var approach = visit.Place.Approach.position;
            var cushion = visit.Place.Cushion;
            var seat = cushion.position;
            var facing = Face(seat, mat.position);
            switch (visit.Stage)
            {
                case Stage.Approach:
                    if (visit.StartsAtTent)
                    {
                        if (animal.WalkToRideMark(TentEntrance, Face(TentEntrance, tentInside.position), dt))
                        {
                            animal.BeginRideActivity();
                            visit.Enter(Stage.EnterTent);
                        }
                    }
                    else if (animal.WalkToRideMark(approach, facing, dt))
                    {
                        animal.BeginRideActivity();
                        visit.Enter(Stage.Settle);
                    }
                    break;
                case Stage.EnterTent:
                    Walk(animal, TentEntrance, tentInside.position, Ease(visit.Time / 1.22f));
                    FitUnderTent(character, Ease(visit.Time / .58f));
                    if (visit.Time >= 1.22f) visit.Enter(Stage.Peek);
                    break;
                case Stage.Peek:
                    float peek = Ease(visit.Time / .50f);
                    animal.transform.SetPositionAndRotation(OnSand(Vector3.Lerp(tentInside.position, tentDoor.position, peek)),
                        Face(tentInside.position, TentEntrance));
                    character.PlayWavePose(Mathf.InverseLerp(.32f, 1.95f, visit.Time));
                    FitUnderTent(character, 1);
                    if (visit.Time >= 2.02f) visit.Enter(Stage.ExitTent);
                    break;
                case Stage.ExitTent:
                    Walk(animal, tentDoor.position, TentEntrance, Ease(visit.Time / .85f));
                    FitUnderTent(character, 1 - Ease(visit.Time / .85f));
                    if (visit.Time >= .85f)
                    {
                        tentGuest = null;
                        animal.SetRideRoute(visit.MatRoute);
                        visit.Enter(Stage.ApproachMat);
                    }
                    break;
                case Stage.ApproachMat:
                    if (animal.WalkToRideMark(approach, facing, dt)) visit.Enter(Stage.Settle);
                    break;
                case Stage.Settle:
                    if (visit.Time < .96f)
                    {
                        Walk(animal, approach, seat, Ease(visit.Time / .96f));
                        break;
                    }
                    if (!cushion.gameObject.activeSelf) ShowCushion(visit);
                    float unfurl = Ease((visit.Time - .96f) / .60f);
                    float seated = Ease((visit.Time - 1.56f) / .48f);
                    cushion.localScale = new Vector3(Mathf.Lerp(.17f, 1, unfurl), 1, Mathf.Lerp(.27f, 1, unfurl));
                    SetAtCushion(animal, seat, facing, seated);
                    character.PlayCampSettlePose(seat + Vector3.up * .13f, unfurl, seated);
                    if (seated >= 1) visit.Enter(Stage.Picnic);
                    break;
                case Stage.Picnic:
                    character.PlayCampPicnicPose(visit.Time + (animal.Id % 7) * .31f, mat.position);
                    if (visit.Time >= PicnicSeconds) visit.Enter(Stage.Stand);
                    break;
                case Stage.Stand:
                    float rising = Ease(visit.Time / .55f);
                    SetAtCushion(animal, seat, facing, 1 - rising);
                    character.PlayRideSeatPose(1 - rising);
                    if (rising >= 1) visit.Enter(Stage.Leave);
                    break;
                case Stage.Leave:
                    Walk(animal, seat, approach, Ease(visit.Time / .96f));
                    return visit.Time >= .96f;
            }
            return false;
        }

        internal void FitUnderTent(RabbitCharacter character, float weight) =>
            character.FitCampTent(transform.lossyScale.y * 2.6f, weight);

        void AnimateCampfire(float dt)
        {
            if (!fireFlames) return;
            fireTime += dt;
            float tall = Mathf.Sin(fireTime * 9.1f);
            float flutter = Mathf.Sin(fireTime * 14.7f + .7f);
            fireFlames.localScale = new Vector3(1 - tall * .06f, 1 + tall * .13f + flutter * .05f, 1 - flutter * .05f);
            fireFlames.localRotation = Quaternion.Euler(flutter * 3.4f, tall * 8, -tall * 4.5f);
        }

        void AdvanceSnack(float dt)
        {
            if (snackGiver != null &&
                (snackGiver.Stage != Stage.Picnic || (snackGuest != null && snackGuest.Stage != Stage.Picnic)))
                StopSnack();
            if (snackGiver == null)
            {
                foreach (var visit in visits)
                {
                    if (!ReadyForSnack(visit) || visit.Ate) continue;
                    var other = Array.Find(visits, candidate => candidate != visit && ReadyForSnack(candidate));
                    if (other == null && visit.Time < 3.35f) continue;
                    snackGiver = visit;
                    snackGuest = other;
                    visit.Ate = true;
                    if (other != null) other.Ate = true;
                    snack.gameObject.SetActive(true);
                    break;
                }
            }
            if (snackGiver == null) return;
            snackTime += dt;
            if (snackTime >= SnackSeconds)
            {
                StopSnack();
                return;
            }

            var giver = snackGiver.Animal;
            float take = Ease((snackTime - .90f) / .34f);
            float eat = Ease((snackTime - 1.23f) / .55f);
            var receiver = snackGuest?.Animal;
            var reachPoint = (receiver ? (giver.transform.position + receiver.transform.position) / 2 : mat.position)
                + Vector3.up * .65f;
            var from = giver.Character.CampSnackHand(reachPoint, Ease(snackTime / .48f) * (receiver ? 1 - take : 1),
                receiver ? 0 : eat);
            var to = receiver ? receiver.Character.CampSnackHand(reachPoint, Ease((snackTime - .19f) / .45f), eat) : from;
            snack.position = Vector3.Lerp(from, to, take);

            static bool ReadyForSnack(Visit visit) => visit?.Stage == Stage.Picnic &&
                visit.Time >= .6f && visit.Time <= PicnicSeconds - SnackSeconds;
        }

        void StopSnack()
        {
            snackGiver = snackGuest = null;
            snackTime = 0;
            snack.gameObject.SetActive(false);
        }

        void ShowCushion(Visit visit)
        {
            var cushion = visit.Place.Cushion;
            var renderer = cushion.GetComponentInChildren<MeshRenderer>(true);
            var color = visit.Animal.Character.CampingCushionColor(renderer.sharedMaterial.color);
            SetColor(renderer, color);
            cushion.gameObject.SetActive(true);
        }

        void SetLantern(bool lit)
        {
            if (lampOn == lit) return;
            lampOn = lit;
            lantern.enabled = lit;
            SetColor(lanternGlass, lit ? new Color(1, .81f, .39f) : new Color(.53f, .40f, .26f));
        }

        void SetColor(Renderer renderer, Color color)
        {
            properties ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(ColorId, color);
            renderer.SetPropertyBlock(properties);
        }

        void Walk(ParkRabbitAgent animal, Vector3 from, Vector3 to, float progress) =>
            animal.WalkAtRideStation(OnSand(Vector3.Lerp(from, to, progress)));

        Vector3 OnSand(Vector3 point)
        {
            point.y = terrain.SurfaceHeight(point);
            return point;
        }

        void SetAtCushion(ParkRabbitAgent animal, Vector3 point, Quaternion facing, float seated) =>
            animal.transform.SetPositionAndRotation(point + Vector3.up * (.16f * transform.lossyScale.y * seated), facing);

        static Quaternion Face(Vector3 from, Vector3 to) =>
            Quaternion.LookRotation(Vector3.ProjectOnPlane(from - to, Vector3.up));
        static float Ease(float time) => Mathf.SmoothStep(0, 1, time);

        void ClearVisit(int index, bool finish)
        {
            var visit = visits[index];
            visits[index] = null;
            visit.Place.Cushion.gameObject.SetActive(false);
            if (tentGuest == visit) tentGuest = null;
            if (snackGiver == visit || snackGuest == visit) StopSnack();
            if (finish && visit.Animal) visit.Animal.EndRide();
        }

        bool IParkAnimalRide.TryLeaveQueue(ParkRabbitAgent animal)
        {
            int index = Array.FindIndex(visits, visit => visit?.Animal == animal && visit.Stage == Stage.Approach);
            if (index < 0) return false;
            ClearVisit(index, false);
            return true;
        }

        void IParkAnimalRide.Release(ParkRabbitAgent animal)
        {
            int index = Array.FindIndex(visits, visit => visit?.Animal == animal);
            if (index >= 0) ClearVisit(index, false);
        }

        void OnDisable()
        {
            if (visits == null) return;
            for (int i = 0; i < visits.Length; i++)
            {
                var visit = visits[i];
                if (visit == null) continue;
                if (visit.Animal && visit.Stage != Stage.Approach) visit.Animal.transform.position = visit.Place.Approach.position;
                ClearVisit(i, true);
            }
            SetLantern(false);
        }
    }
}
