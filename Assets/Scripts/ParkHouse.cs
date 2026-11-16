using UnityEngine;

namespace PeacePark
{
    public sealed class ParkHouse : MonoBehaviour
    {
        public string DisplayName="꽃집";
        public Transform DoorHinge;
        public Transform SecondDoorHinge;
        public Transform EntranceBell;
        public AudioSource EntranceChime;
        public Renderer EntranceLightSpill;
        public Transform Inside;
        public Transform Threshold;
        public Transform Welcome;
        public float DoorDuration=.7f;
        public float OpenAngle=110;
        public bool HasRoundedPorch;
        public Vector2[] ExitSlope;
        public ParkDuneTerrain ExitTerrain;
        public ParkSlide EntranceSlide;
        public Transform EntrancePole;
        public float ExitSeconds;
        public bool BushEntrance;
        internal const float BushPeekSeconds=.75f, BushLookSeconds=2.1f, BushWalkSeconds=2.8f;
        internal const float PoleEntrySeconds=1.20f,PoleDescentSeconds=2.45f,PoleReleaseSeconds=.4f,PoleWalkSeconds=1.15f;
        internal float ExitDuration=>BushEntrance?BushPeekSeconds+BushLookSeconds+BushWalkSeconds:EntranceSlide?
            ParkSlide.EntrySeconds+ParkSlide.DescentSeconds+ParkSlide.StandSeconds+ParkSlide.ExitWalkSeconds:
            EntrancePole?PoleEntrySeconds+PoleDescentSeconds+PoleReleaseSeconds+PoleWalkSeconds:
            ExitSeconds>0?ExitSeconds:3.75f;
        public static float PorchHeight(float localZ)=>.30f+(.025f-.30f)*((-localZ-2.25f)/1.45f);
        public float DoorFraction {get;private set;}
        public bool IsBusy {get;private set;}
        public int OpenCount {get;private set;}
        public int CompletedExits {get;private set;}
        bool opening;
        float bellTime;
        MaterialPropertyBlock entranceLightProperties;
        static readonly int LightStrength=Shader.PropertyToID("_Strength");
        internal int LastDepartureDirection {get;set;}=-1;

        public bool TryReserve()
        {
            if(IsBusy||(EntranceSlide&&EntranceSlide.isActiveAndEnabled&&!EntranceSlide.Available))return false;
            IsBusy=true;opening=true;OpenCount++;
            if(EntranceBell)bellTime=1.35f;
            if(EntranceChime)EntranceChime.Play();
            return true;
        }
        public void CloseAfterExit(){opening=false;CompletedExits++;}
        internal void ContinueFrom(ParkHouse previous)
        {
            opening=previous&&previous.opening;IsBusy=previous&&previous.IsBusy;DoorFraction=previous?previous.DoorFraction:0;
            PoseDoors();
        }
        public void Advance(float deltaTime)
        {
            DoorFraction=Mathf.MoveTowards(DoorFraction,opening?1:0,deltaTime/Mathf.Max(.1f,DoorDuration));
            PoseDoors();
            if(EntranceBell&&bellTime>0)
            {
                bellTime=Mathf.Max(0,bellTime-deltaTime);
                EntranceBell.localRotation=Quaternion.Euler(0,0,13*bellTime*Mathf.Sin((1.35f-bellTime)*13));
            }
            if(!opening&&DoorFraction<=0)IsBusy=false;
        }
        void PoseDoors()
        {
            float angle=Mathf.SmoothStep(0,1,DoorFraction)*OpenAngle;
            if(DoorHinge)DoorHinge.localRotation=Quaternion.Euler(0,angle,0);
            if(SecondDoorHinge)SecondDoorHinge.localRotation=Quaternion.Euler(0,-angle,0);
            if(EntranceLightSpill)
            {
                entranceLightProperties??=new MaterialPropertyBlock();
                entranceLightProperties.SetFloat(LightStrength,Mathf.SmoothStep(0,.65f,DoorFraction));
                EntranceLightSpill.SetPropertyBlock(entranceLightProperties);
            }
        }
        public Vector3 ExitPosition(float progress)
        {
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress));
            var position=t<.55f?Vector3.Lerp(Inside.position,Threshold.position,t/.55f):Vector3.Lerp(Threshold.position,Welcome.position,(t-.55f)/.45f);
            if(ExitTerrain)
                position.y=ExitTerrain.SurfaceHeight(position);
            else if(HasRoundedPorch)
            {
                var local=transform.InverseTransformPoint(position);
                if(local.z> -2.25f)local.y=.30f;
                else if(local.z>= -3.7f)local.y=PorchHeight(local.z);
                else local.y=Mathf.Lerp(.025f,Welcome.localPosition.y,
                    Mathf.InverseLerp(-3.7f,Welcome.localPosition.z,local.z));
                position=transform.TransformPoint(local);
            }
            else if(ExitSlope!=null&&ExitSlope.Length>1)
            {
                var local=transform.InverseTransformPoint(position);
                local.y=ExitSlope[0].y;
                for(int i=1;i<ExitSlope.Length;i++)
                {
                    var from=ExitSlope[i-1];var to=ExitSlope[i];
                    local.y=Mathf.Lerp(from.y,to.y,Mathf.InverseLerp(from.x,to.x,local.z));
                    if(local.z>=to.x)break;
                }
                position=transform.TransformPoint(local);
            }
            return position;
        }
        public Quaternion Facing=>Quaternion.LookRotation(transform.forward,Vector3.up);
    }
}
