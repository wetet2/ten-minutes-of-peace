using UnityEngine;

namespace PeacePark
{
    public sealed class ParkCamera : MonoBehaviour
    {
        public Vector3 focus = new Vector3(0, 1, 0);
        public float distance = 30;
        public float yaw = 0;
        public float pitch = 48;
        public bool InputEnabled=true;
        public bool ShowControls=true;
        Vector3 previous;
        int firstPinchFinger=-1, secondPinchFinger=-1;
        float previousPinchSpan;
        Vector2 previousTouchCenter;
        bool waitForTouchRelease, touchBlockedByUi;
        Vector3 initialFocus;
        float initialDistance, initialYaw, initialPitch;

        void Awake()
        {
            initialFocus = focus;
            initialDistance = distance;
            initialYaw = yaw;
            initialPitch = pitch;
        }
        void OnEnable() { ClearTouchGesture(); Apply(); }
        void Update()
        {
            Vector3 mouse = Input.mousePosition;
            if(!InputEnabled||GUIUtility.hotControl!=0)
            {
                previous=mouse;
                ClearTouchGesture();
                touchBlockedByUi=Input.touchCount>0;
                return;
            }
            if(Input.touchCount>0||Application.isMobilePlatform||waitForTouchRelease||touchBlockedByUi)
            {
                previous=mouse;
                UpdateTouch();
                return;
            }
            Vector3 delta = mouse - previous;
            if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) && mouse.y < Screen.height - 70)
            {
                Orbit(delta);
            }
            if ((Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1)) ||
                (Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2)))
                Pan(delta);
            distance = Mathf.Clamp(distance * Mathf.Exp(-Input.mouseScrollDelta.y * .1f), 7, 48);
            previous = mouse;
            if (Input.GetKeyDown(KeyCode.Home)) ResetView();
            else Apply();
        }
        void Orbit(Vector2 delta)
        {
            yaw+=delta.x*.25f;
            pitch=Mathf.Clamp(pitch-delta.y*.2f,12,82);
        }
        void Pan(Vector2 delta)
        {
            var heading=Quaternion.Euler(0,yaw,0);
            focus-=heading*new Vector3(delta.x,0,delta.y)*distance*.0013f;
            focus.x=Mathf.Clamp(focus.x,-15,15);
            focus.z=Mathf.Clamp(focus.z,-12,12);
        }
        void UpdateTouch()
        {
            int count=Input.touchCount;
            if(count==0){ClearTouchGesture();return;}
            if(touchBlockedByUi)return;
            if(count!=2)
            {
                if(count>2){waitForTouchRelease=true;previousPinchSpan=0;return;}
                var touch=Input.GetTouch(0);
                if(touch.phase==TouchPhase.Began&&!BelowToolbar(touch.position))touchBlockedByUi=true;
                if(!waitForTouchRelease&&touch.phase==TouchPhase.Moved&&BelowToolbar(touch.position-touch.deltaPosition))
                {
                    Orbit(touch.deltaPosition*900f/Screen.height);
                    Apply();
                }
                previousPinchSpan=0;
                return;
            }

            waitForTouchRelease=true;
            var a=Input.GetTouch(0);
            var b=Input.GetTouch(1);
            if(a.phase==TouchPhase.Ended||a.phase==TouchPhase.Canceled||
                b.phase==TouchPhase.Ended||b.phase==TouchPhase.Canceled)
            {
                previousPinchSpan=0;
                return;
            }
            float span=Vector2.Distance(a.position,b.position);
            var center=(a.position+b.position)*.5f;
            bool samePair=a.fingerId==firstPinchFinger&&b.fingerId==secondPinchFinger||
                a.fingerId==secondPinchFinger&&b.fingerId==firstPinchFinger;
            if(previousPinchSpan>0&&samePair&&a.phase!=TouchPhase.Began&&b.phase!=TouchPhase.Began)
            {
                ApplyTwoFingerGesture(center-previousTouchCenter,Screen.height,previousPinchSpan,span);
                Apply();
            }
            else if(!BelowToolbar(a.position)||!BelowToolbar(b.position))
            {
                previousPinchSpan=0;
                touchBlockedByUi=true;
                return;
            }
            firstPinchFinger=a.fingerId;
            secondPinchFinger=b.fingerId;
            previousPinchSpan=Mathf.Max(1,span);
            previousTouchCenter=center;
        }
        internal void ApplyTwoFingerGesture(Vector2 centerDelta,float screenHeight,float previousSpan,float span)
        {
            distance=DistanceFromPinch(distance,previousSpan,span);
            if(centerDelta.sqrMagnitude>0)Pan(centerDelta*(900f/screenHeight));
        }
        static bool BelowToolbar(Vector2 position)
        {
            var area=Screen.safeArea;
            return position.y<area.yMax-82*Mathf.Min(area.height/900f,area.width/665f);
        }
        void ClearTouchGesture()
        {
            firstPinchFinger=secondPinchFinger=-1;
            previousPinchSpan=0;
            waitForTouchRelease=false;
            touchBlockedByUi=false;
        }
        internal static float DistanceFromPinch(float current,float previousSpan,float span)
        {
            return Mathf.Clamp(current*previousSpan/Mathf.Max(1,span),7,48);
        }
        void Apply()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = focus - transform.forward * distance;
        }
        internal void ResetView()
        {
            focus = initialFocus;
            distance = initialDistance;
            yaw = initialYaw;
            pitch = initialPitch;
            Apply();
        }
        internal void SetVenueView(ParkVenue venue)
        {
            initialFocus=venue.Focus;initialDistance=venue.Distance;initialYaw=venue.Yaw;initialPitch=venue.Pitch;
            ResetView();
        }
        void OnGUI()
        {
            if(!ShowControls)return;
            GUI.Box(new Rect(16, 16, 540, 44), "PEACE PARK     Drag: orbit  |  Right drag: pan  |  Wheel: zoom");
            if (GUI.Button(new Rect(Screen.width - 126, 20, 110, 34), "Reset view")) ResetView();
        }
    }
}
