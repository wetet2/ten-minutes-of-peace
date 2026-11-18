using UnityEngine;

namespace PeacePark
{
    public sealed class Rabbit3DPoc : MonoBehaviour
    {
        DrawingPage drawing;
        RabbitCharacter rabbit;
        GUIStyle buttonStyle;
        Font font;

        void Start()
        {
            drawing=FindAnyObjectByType<DrawingPage>();rabbit=FindAnyObjectByType<RabbitCharacter>();
            if(drawing){drawing.LockedAnimal=2;drawing.SelectAnimal(2);drawing.Applied+=OnApplied;}
            font=ParkFonts.Bold;
        }

        void OnApplied(int animal,byte[] front,byte[] back)
        {
            // This PoC intentionally accepts only the rabbit tab.
            if(animal!=2)return;
            drawing.Visible=false;rabbit.ApplyPainting(front,back);
        }

        void OnGUI()
        {
            if(!rabbit||!rabbit.IsRunning)return;
            if(buttonStyle==null)
            {
                buttonStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=24,fontStyle=FontStyle.Normal};
                buttonStyle.normal.textColor=new Color32(70,54,43,255);
            }
            if(GUI.Button(new Rect(24,24,190,58),ParkLanguage.Text("다시 색칠하기"),buttonStyle))
            {
                rabbit.ResetPreview();drawing.SelectAnimal(2);drawing.Visible=true;
            }
        }

        void OnDestroy(){if(drawing)drawing.Applied-=OnApplied;}
    }
}
