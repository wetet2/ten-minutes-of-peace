using UnityEngine;

namespace PeacePark
{
    public sealed partial class ParkCamera : MonoBehaviour
    {
        public const float MinimumDistance=3.5f;
        const float ResetDuration=.9f;
        public Vector3 focus = new Vector3(0, 1, 0);
        public float distance = 30;
        public float yaw = 0;
        public float pitch = 48;
        public bool InputEnabled=true;
        public bool ShowControls=true;
        Vector2 previousMouse;
        bool mousePanning,touchInputActive;
        int firstPinchFinger=-1, secondPinchFinger=-1;
        float previousPinchSpan;
        Vector2 previousTouchCenter;
        bool waitForTouchRelease, touchBlockedByUi;
        Vector3 initialFocus;
        float initialDistance, initialYaw, initialPitch;
        float maxDistance=48,panX=15,panZ=12;
        float closeFocusHeight;
        Vector3 resetStartFocus, resetStartAngles;
        float resetStartDistance, resetElapsed=-1f;
        bool IsResetting=>resetElapsed>=0f;

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
            Vector2 mouse=Input.mousePosition;
            int touchCount=Input.touchCount;
            if(!InputEnabled||GUIUtility.hotControl!=0)
            {
                previousMouse=mouse;
                ClearTouchGesture();
                mousePanning=false;
                touchBlockedByUi=touchCount>0;
                return;
            }
            if(touchCount>0)
            {
                // Unity can also emit simulated mouse input for these touches.
                if(!touchInputActive)CancelAnimalTap();
                touchInputActive=true;
                mousePanning=false;
                previousMouse=mouse;
                UpdateTouch(touchCount);
                return;
            }
            if(touchInputActive||touchBlockedByUi)ClearTouchGesture();
            UpdateMouse(mouse,Input.GetMouseButton(0),Input.GetMouseButtonDown(0),Input.GetMouseButtonUp(0),
                Input.GetMouseButton(1)||Input.GetMouseButton(2),Input.GetMouseButtonDown(1)||Input.GetMouseButtonDown(2),
                Input.mouseScrollDelta.y,Application.isMobilePlatform);
            if (Input.GetKeyDown(KeyCode.Home)) ResetView();
            else Apply();
        }

        internal void UpdateMouse(Vector2 mouse,bool leftHeld,bool leftPressed,bool leftReleased,
            bool panHeld,bool panPressed,float scroll,bool scaleLikeTouch)
        {
            Vector2 delta=mouse-previousMouse;
            previousMouse=mouse;
            float motionHeight=scaleLikeTouch?Screen.height:900f;
            if(leftPressed)BeginAnimalTap(mouse,-1);
            if(panHeld)CancelAnimalTap();
            if(leftHeld&&!leftPressed&&MoveAnimalTap(mouse))
                Orbit(delta*(900f/motionHeight));
            if(!panHeld)mousePanning=false;
            else if(panPressed)mousePanning=CanStartPointer(mouse,-1);
            else if(mousePanning)PanPointer(delta,motionHeight);
            if(leftReleased)EndAnimalTap(mouse,-1);
            if(scroll!=0&&CanStartPointer(mouse,-1))ApplyWheelZoom(scroll);
        }
        void Orbit(Vector2 delta)
        {
            if(seatedTrain)return;
            InterruptReturn();
            aimAutoFollowToVenue=false;
            yaw+=delta.x*.25f;
            pitch=Mathf.Clamp(pitch-delta.y*.2f,12,82);
        }
        void Pan(Vector2 delta)
        {
            if(seatedTrain)return;
            InterruptReturn();
            var heading=Quaternion.Euler(0,yaw,0);
            var movement=heading*new Vector3(delta.x,0,delta.y)*distance*.0013f;
            if(following){followOffset=Vector3.ClampMagnitude(followOffset-movement,3);return;}
            focus-=movement;
            focus.x=Mathf.Clamp(focus.x,-panX,panX);
            focus.z=Mathf.Clamp(focus.z,-panZ,panZ);
        }
        void UpdateTouch(int count)
        {
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
            PanPointer(centerDelta,screenHeight);
        }
        void PanPointer(Vector2 delta,float screenHeight)
        {
            if(delta.sqrMagnitude>0)Pan(delta*(900f/screenHeight));
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
            touchInputActive=false;
        }
        internal static float DistanceFromPinch(float current,float previousSpan,float span,float limit=48)
        {
            return Mathf.Clamp(current*previousSpan/Mathf.Max(1,span),MinimumDistance,limit);
        }
        void Apply()
        {
            if(seatedTrain)
            {
                var seat=seatedTrain.RideView(locomotiveView);
                transform.SetPositionAndRotation(seat.position,seat.rotation);
                return;
            }
            float roll=0f;
            if(IsResetting)
            {
                float eased=Mathf.SmoothStep(0f,1f,resetElapsed/ResetDuration);
                focus=Vector3.Lerp(resetStartFocus,NormalFocus(initialFocus,initialDistance),eased);
                distance=Mathf.Lerp(resetStartDistance,initialDistance,eased);
                yaw=Mathf.LerpAngle(resetStartAngles.y,initialYaw,eased);
                pitch=Mathf.LerpAngle(resetStartAngles.x,initialPitch,eased);
                roll=Mathf.LerpAngle(resetStartAngles.z,0f,eased);
            }
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
            var target=following||returning||freeAim?focus:NormalFocus(focus,distance);
            var position=target-transform.forward*distance;
            if(following&&followTarget&&park.SandSled&&followTarget.ActiveAction==ParkAnimalAction.SandSled)
            {
                float clearance=park.SurfaceHeight(position)+.65f;
                if(position.y<clearance)
                {
                    position.y=clearance;
                    transform.rotation=Quaternion.LookRotation(target-position,Vector3.up);
                }
            }
            transform.position=position;
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
            if(IsResetting)return;
            resetStartAngles=transform.eulerAngles;
            resetStartDistance=distance;
            resetStartFocus=transform.position+transform.forward*distance;
            seatedTrain=null;
            AutoFollowEnabled=false;
            CancelAnimalFollow(false);
            freeAim=true;
            resetElapsed=0f;
            Apply();
        }
        void RestoreDefaultView()
        {
            seatedTrain=null;
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
            RestoreDefaultView();
        }
        void OnGUI()
        {
            if(!ShowControls)return;
            GUI.Box(new Rect(16, 16, 540, 44), "PEACE PARK     Drag: orbit  |  Right drag: pan  |  Wheel: zoom");
            if (GUI.Button(new Rect(Screen.width - 126, 20, 110, 34), "Reset view")) ResetView();
        }
    }
}
