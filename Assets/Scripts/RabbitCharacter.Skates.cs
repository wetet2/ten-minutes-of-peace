using UnityEngine;

namespace PeacePark
{
    public sealed partial class RabbitCharacter
    {
        internal const float SkateBladeDepth=.16f;
        Transform leftSkate,rightSkate;
        Renderer[] skateRenderers;
        MaterialPropertyBlock skateProperties;
        float skateVisibility=1;

        internal void WearSkates(Transform template)
        {
            if(!leftSkate)
            {
                leftSkate=AttachSkate(template,leftLeg,leftSole.RestHeight);
                rightSkate=AttachSkate(template,rightLeg,rightSole.RestHeight);
                var left=leftSkate.GetComponentsInChildren<Renderer>(true);
                var right=rightSkate.GetComponentsInChildren<Renderer>(true);
                skateRenderers=new Renderer[left.Length+right.Length];
                left.CopyTo(skateRenderers,0);
                right.CopyTo(skateRenderers,left.Length);
                ApplySkateVisibility();
            }
            if(leftSkate.gameObject.activeSelf)return;
            leftSkate.gameObject.SetActive(true);
            rightSkate.gameObject.SetActive(true);
        }

        static Transform AttachSkate(Transform template,Transform leg,float soleHeight)
        {
            var skate=Instantiate(template,leg,false);
            skate.name="Ice skate";
            skate.localPosition=new Vector3(0,soleHeight,-.15f);
            return skate;
        }

        internal void RemoveSkates()
        {
            if(!leftSkate||!leftSkate.gameObject.activeSelf)return;
            leftSkate.gameObject.SetActive(false);
            rightSkate.gameObject.SetActive(false);
        }

        void ApplySkateVisibility()
        {
            if(skateRenderers==null)return;
            skateProperties??=new MaterialPropertyBlock();
            skateProperties.SetFloat(AnimalVisibilityId,skateVisibility);
            foreach(var renderer in skateRenderers)renderer.SetPropertyBlock(skateProperties);
        }

        internal void PlaySkatingPose(float push,float tuck,float landing)
        {
            PlayIdlePose();
            float left=Mathf.Max(0,push),right=Mathf.Max(0,-push);
            float open=1-tuck;
            leftLeg.localPosition+=new Vector3(-.24f*left*open+.13f*tuck,.22f*left+.13f*tuck,.45f*left+.13f*tuck);
            rightLeg.localPosition+=new Vector3(.24f*right*open-.17f*tuck,.22f*right+.12f*tuck,.45f*right-.15f*tuck);
            leftLeg.localRotation=Quaternion.Euler(-24*left,0,-10*left*open);
            rightLeg.localRotation=Quaternion.Euler(-24*right,0,10*right*open);
            var crouch=Vector3.down*(.18f*landing);
            body.localPosition+=crouch;
            head.localPosition+=crouch;
            leftShoulder.localPosition+=crouch;
            rightShoulder.localPosition+=crouch;
            body.localRotation=Quaternion.Euler(-7*open+22*landing,0,push*7*open);
            head.localRotation=Quaternion.Euler(5*open-11*landing,0,-push*9*open);
            leftArm.localRotation=Quaternion.Euler(8*push*open,Mathf.Lerp(12,108,tuck),Mathf.Lerp(5,24,tuck));
            rightArm.localRotation=Quaternion.Euler(-8*push*open,Mathf.Lerp(-12,-108,tuck),Mathf.Lerp(-5,-24,tuck));
            if(correctiveReady)skin.SetBlendShapeWeight(0,100*(1-.7f*open));
        }
    }
}
