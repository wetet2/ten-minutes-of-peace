using UnityEngine;
using UnityEngine.UI;

namespace PeacePark
{
    internal sealed class ParkToast : MonoBehaviour
    {
        const float Duration=3,Enter=.28f,Exit=.22f;
        RectTransform banner;
        CanvasGroup group;
        Text label;
        Canvas canvas;
        float expires;
        Rect bounds;
        readonly DrawingSkin skin=new DrawingSkin();
        GUIStyle style;
        internal string Message=>Time.unscaledTime<expires?label.text:null;

        internal void Build(Sprite background)
        {
            canvas=gameObject.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=20;
            banner=ParkWelcome.Child("Toast",(RectTransform)transform);
            var image=banner.gameObject.AddComponent<Image>();
            image.sprite=background;
            image.type=Image.Type.Sliced;
            image.color=new Color32(40,84,58,255);
            image.raycastTarget=false;
            group=banner.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;
            group.interactable=false;
            label=ParkWelcome.Label("Message",banner,"",ParkFonts.Bold);
            label.color=new Color32(246,255,231,255);
            label.verticalOverflow=VerticalWrapMode.Truncate;
            Clear();
        }

        internal void Show(string message)
        {
            label.text=message;
            expires=Time.unscaledTime+Duration;
            banner.gameObject.SetActive(true);
            Update();
        }

        internal void Clear()
        {
            expires=0;
            banner.gameObject.SetActive(false);
        }

        internal void UseDrawingLayer(bool drawing)=>canvas.enabled=!drawing;

        internal void DrawOverColoring()
        {
            if(canvas.enabled||Message==null||Event.current.type!=EventType.Repaint)return;
            style??=new GUIStyle(GUI.skin.label){font=ParkFonts.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            style.fontSize=label.fontSize;
            style.normal.textColor=label.color;
            Matrix4x4 previousMatrix=GUI.matrix;
            Color previousColor=GUI.color;
            GUI.matrix=Matrix4x4.identity;
            GUI.color=new Color(1,1,1,group.alpha);
            skin.Draw(bounds,new Color32(40,84,58,255),bounds.height/2);
            float inset=label.rectTransform.anchoredPosition.x;
            GUI.Label(new Rect(bounds.x+inset,bounds.y,bounds.width-2*inset,bounds.height),Message,style);
            GUI.color=previousColor;
            GUI.matrix=previousMatrix;
        }

        void Update()
        {
            if(!banner||!banner.gameObject.activeSelf)return;
            float remaining=expires-Time.unscaledTime;
            if(remaining<=0){Clear();return;}
            var safe=Screen.safeArea;
            if(safe.width<=0||safe.height<=0)return;
            float scale=Mathf.Min(safe.height/900f,safe.width/665f);
            label.fontSize=Mathf.Max(1,Mathf.RoundToInt(25*scale));
            float padding=24*scale;
            float width=Mathf.Min(safe.width-padding,label.preferredWidth+2*padding);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-2*padding);
            float height=Mathf.Max(56*scale,label.preferredHeight+20*scale);
            float rise=Mathf.SmoothStep(0,1,(Duration-remaining)/Enter);
            group.alpha=rise*Mathf.SmoothStep(0,1,remaining/Exit);
            float bottom=Screen.height-safe.yMin;
            float top=Mathf.Lerp(bottom,bottom-height-22*scale,rise);
            bounds=new Rect(safe.center.x-width/2,top,width,height);
            ParkWelcome.Place(banner,bounds);
            ParkWelcome.Place(label.rectTransform,new Rect(padding,0,width-2*padding,height));
        }

        void OnDestroy()=>skin.Dispose();
    }
}
