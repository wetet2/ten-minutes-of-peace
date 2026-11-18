using UnityEngine;

namespace PeacePark
{
    public sealed partial class ParkCamera : MonoBehaviour
    {
        public const float MinimumDistance=3.5f;
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
        float maxDistance=48,panX=15,panZ=12;
        float closeFocusHeight;

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
            if(Input.GetMouseButtonDown(0))BeginAnimalTap(mouse,-1);
            if(Input.GetMouseButton(1)||Input.GetMouseButton(2))CancelAnimalTap();
            if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) && MoveAnimalTap(mouse))
            {
                Orbit(delta);
            }
            if ((Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1)) ||
                (Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2)))
                Pan(delta);
            if(Input.GetMouseButtonUp(0))EndAnimalTap(mouse,-1);
            if(Input.mouseScrollDelta.y!=0)ZoomTo(ZoomDistance*Mathf.Exp(-Input.mouseScrollDelta.y*.1f));
            previous = mouse;
            if (Input.GetKeyDown(KeyCode.Home)) ResetView();
            else Apply();
        }
        void Orbit(Vector2 delta)
        {
            InterruptReturn();
            yaw+=delta.x*.25f;
            pitch=Mathf.Clamp(pitch-delta.y*.2f,12,82);
        }
        void Pan(Vector2 delta)
        {
            InterruptReturn();
            var heading=Quaternion.Euler(0,yaw,0);
            var movement=heading*new Vector3(delta.x,0,delta.y)*distance*.0013f;
            if(following){followOffset=Vector3.ClampMagnitude(followOffset-movement,3);return;}
            focus-=movement;
            focus.x=Mathf.Clamp(focus.x,-panX,panX);
            focus.z=Mathf.Clamp(focus.z,-panZ,panZ);
        }
        void UpdateTouch()
        {
            int count=Input.touchCount;
            if(count==0){ClearTouchGesture();return;}
            if(touchBlockedByUi)return;
            if(count!=2)
            {
                if(count>2){CancelAnimalTap();waitForTouchRelease=true;previousPinchSpan=0;return;}
                var touch=Input.GetTouch(0);
                if(touch.phase==TouchPhase.Began)
                {
                    if(!CanStartPointer(touch.position,touch.fingerId)){touchBlockedByUi=true;CancelAnimalTap();return;}
                    if(!waitForTouchRelease)BeginAnimalTap(touch.position,touch.fingerId);
                }
                if(!waitForTouchRelease&&touch.phase==TouchPhase.Moved&&MoveAnimalTap(touch.position))
                {
                    Orbit(touch.deltaPosition*900f/Screen.height);
                    Apply();
                }
                if(touch.phase==TouchPhase.Ended&&!waitForTouchRelease)EndAnimalTap(touch.position,touch.fingerId);
                if(touch.phase==TouchPhase.Canceled)CancelAnimalTap();
                previousPinchSpan=0;
                return;
            }

            CancelAnimalTap();
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
            else if(!CanStartPointer(a.position,a.fingerId)||!CanStartPointer(b.position,b.fingerId))
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
            ZoomTo(DistanceFromPinch(ZoomDistance,previousSpan,span,maxDistance));
            if(centerDelta.sqrMagnitude>0)Pan(centerDelta*(900f/screenHeight));
        }
        bool BelowToolbar(Vector2 position)
        {
            if(park)return !park.ToolbarContains(position);
            var area=Screen.safeArea;
            return position.y<area.yMax-82*Mathf.Min(area.height/900f,area.width/665f);
        }
        void ClearTouchGesture()
        {
            CancelAnimalTap();
            firstPinchFinger=secondPinchFinger=-1;
            previousPinchSpan=0;
            waitForTouchRelease=false;
            touchBlockedByUi=false;
        }
        internal static float DistanceFromPinch(float current,float previousSpan,float span,float limit=48)
        {
            return Mathf.Clamp(current*previousSpan/Mathf.Max(1,span),MinimumDistance,limit);
        }
        void Apply()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            var target=following||returning||freeAim?focus:NormalFocus(focus,distance);
            transform.position = target - transform.forward * distance;
        }
        Vector3 NormalFocus(Vector3 target,float viewDistance)
        {
            if(closeFocusHeight>0)
            {
                // The island's wide view aims high at the mountains. Close views aim at animals and sand.
                float wide=Mathf.SmoothStep(0,1,Mathf.InverseLerp(MinimumDistance,Mathf.Min(24,initialDistance),viewDistance));
                target.y=Mathf.Lerp(closeFocusHeight,target.y,wide);
            }
            return target;
        }
        internal void ResetView()
        {
            CancelAnimalFollow(false);
            focus = initialFocus;
            distance = initialDistance;
            yaw = initialYaw;
            pitch = initialPitch;
            Apply();
        }
        internal void SetVenueView(ParkVenue venue)
        {
            initialFocus=venue.Focus;initialDistance=venue.Distance;initialYaw=venue.Yaw;initialPitch=venue.Pitch;
            maxDistance=Mathf.Max(48,Mathf.Max(venue.MaxDistance,venue.Distance));
            panX=Mathf.Max(15,Mathf.Max(venue.WalkArea.width*.46f,venue.CameraPanLimits.x));
            panZ=Mathf.Max(12,Mathf.Max(venue.WalkArea.height*.46f,venue.CameraPanLimits.y));
            closeFocusHeight=venue.CloseFocusHeight;
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
