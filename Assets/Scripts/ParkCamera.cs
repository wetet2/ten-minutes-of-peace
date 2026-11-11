using UnityEngine;

namespace PeacePark
{
    public sealed class ParkCamera : MonoBehaviour
    {
        public Vector3 focus = new Vector3(0, 1, 0);
        public float distance = 30;
        public float yaw = 0;
        public float pitch = 48;
        Vector3 previous;

        void OnEnable() { Apply(); }
        void Update()
        {
            Vector3 mouse = Input.mousePosition;
            Vector3 delta = mouse - previous;
            if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) && mouse.y < Screen.height - 70)
            {
                yaw += delta.x * .25f;
                pitch = Mathf.Clamp(pitch - delta.y * .2f, 12, 82);
            }
            if ((Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1)) ||
                (Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2)))
            {
                Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                focus -= (right * delta.x + forward * delta.y) * distance * .0013f;
                focus.x = Mathf.Clamp(focus.x, -15, 15);
                focus.z = Mathf.Clamp(focus.z, -12, 12);
            }
            distance = Mathf.Clamp(distance * Mathf.Exp(-Input.mouseScrollDelta.y * .1f), 7, 48);
            if (Input.GetKeyDown(KeyCode.Home)) ResetView();
            previous = mouse;
            Apply();
        }
        void Apply()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = focus - transform.forward * distance;
        }
        void ResetView() { focus = new Vector3(0, 1, 0); distance = 30; yaw = 0; pitch = 48; }
        void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 540, 44), "PEACE PARK     Drag: orbit  |  Right drag: pan  |  Wheel: zoom");
            if (GUI.Button(new Rect(Screen.width - 126, 20, 110, 34), "Reset view")) ResetView();
        }
    }
}
