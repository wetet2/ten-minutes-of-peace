using UnityEngine;

namespace PeacePark
{
    public sealed class ParkFountain : MonoBehaviour
    {
        public const float OuterRadius = 2.67f;
        public const float WaterHeight = .49f;

        [SerializeField] Transform movingParts;
        [SerializeField] Transform[] swimmers;
        [SerializeField] Transform[] ducks;
        [SerializeField] Transform[] droplets;
        float elapsed;

        public void Configure(Transform moving, Transform[] paths, Transform[] birds, Transform[] spray)
        {
            movingParts = moving;
            swimmers = paths;
            ducks = birds;
            droplets = spray;
            Pose(0);
        }

        public bool IsMovingPart(Transform part) => part.IsChildOf(movingParts);

        internal bool Overlaps(Vector3 position)
        {
            var local = transform.InverseTransformPoint(position);
            const float clearance = OuterRadius + .36f;
            return local.x * local.x + local.z * local.z < clearance * clearance;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            Pose(elapsed);
        }

        internal void Pose(float time)
        {
            for (int i = 0; i < swimmers.Length; i++)
            {
                float angle = 3.38f + time * .32f - i * .54f;
                float radius = 1.64f - i * .06f;
                float sway = time * 3 + i * 1.4f;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                swimmers[i].localPosition = new Vector3(radius * x, WaterHeight, radius * z);
                swimmers[i].localRotation = Quaternion.LookRotation(new Vector3(-z, 0, x));
                ducks[i].localPosition = Vector3.up * (.023f * Mathf.Sin(sway));
                ducks[i].localRotation = Quaternion.Euler(2.5f * Mathf.Sin(sway), 0, 4f * Mathf.Cos(sway));
            }

            for (int i = 0; i < droplets.Length; i++)
            {
                float flight = Mathf.Repeat(time * 1.15f + i * .31f, 1);
                float angle = i * 2.399963f;
                float reach = (.60f + i % 3 * .13f) * flight;
                float height = Mathf.Lerp(1.12f, WaterHeight, flight) + 2.85f * flight * (1 - flight);
                droplets[i].localPosition = new Vector3(Mathf.Cos(angle) * reach, height, Mathf.Sin(angle) * reach);
                float scale = Mathf.Sin(flight * Mathf.PI);
                droplets[i].localScale = new Vector3(.12f, .23f, .12f) * (.45f + .55f * scale);
            }
        }
    }
}
