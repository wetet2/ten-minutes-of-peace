using UnityEngine;

namespace PeacePark
{
    public sealed class ParkRoundaboutSpin : MonoBehaviour
    {
        const float DegreesPerSecond = 12f;

        void Update()
        {
            transform.Rotate(Vector3.up, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
