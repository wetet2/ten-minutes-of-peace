using UnityEngine;

namespace PeacePark
{
    public sealed class RoofSunflowerSpin : MonoBehaviour
    {
        const float DegreesPerSecond = 12f;

        void Update()
        {
            transform.Rotate(Vector3.forward, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
