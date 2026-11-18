using System;
using UnityEngine;

namespace PeacePark
{
    public sealed class ParkTowerClock : MonoBehaviour
    {
        public Transform HourHand,MinuteHand;
        double nextRefresh;

        void OnEnable()=>Refresh();
        void OnApplicationPause(bool paused){if(!paused&&isActiveAndEnabled)Refresh();}
        void Update()
        {
            if(Time.unscaledTimeAsDouble>=nextRefresh)Refresh();
        }

        void Refresh()
        {
            nextRefresh=Time.unscaledTimeAsDouble+1;
            if(!HourHand||!MinuteHand)return;
            var now=DateTime.Now;
            float minutes=now.Minute+now.Second/60f;
            HourHand.localRotation=Quaternion.Euler(0,0,-30f*(now.Hour%12+minutes/60f));
            MinuteHand.localRotation=Quaternion.Euler(0,0,-6f*minutes);
        }
    }
}
