using System.Collections.Generic;
using UnityEngine;

namespace PeacePark.Editor
{
    // Sample the actual rounded coastline, then move inland along its normal.
    internal sealed class ParkBeachShorePlacement
    {
        readonly List<Vector3> rocks=new List<Vector3>();
        internal void AddRock(float x,float z,float radius)=>rocks.Add(new Vector3(x,radius,z));

        internal Vector3 Point(int coast,float along,float inset)
        {
            float x=0,z=0,low,high;
            if(coast==0)
            {
                x=Mathf.Lerp(-14,14,along);low=-35;high=10;
                for(int i=0;i<24;i++){float m=(low+high)*.5f;if(ParkBeachMeshes.LandDistance(x,m)<0)low=m;else high=m;}
                z=(low+high)*.5f;
            }
            else
            {
                z=Mathf.Lerp(-3,58,along);low=0;high=100;
                float side=coast==1?-1:1;
                for(int i=0;i<24;i++){float m=(low+high)*.5f;if(ParkBeachMeshes.LandDistance(side*m,z)>0)low=m;else high=m;}
                x=side*(low+high)*.5f;
            }
            var normal=new Vector2(ParkBeachMeshes.LandDistance(x+.1f,z)-ParkBeachMeshes.LandDistance(x-.1f,z),
                ParkBeachMeshes.LandDistance(x,z+.1f)-ParkBeachMeshes.LandDistance(x,z-.1f)).normalized;
            x+=normal.x*inset;z+=normal.y*inset;
            return new Vector3(x,ParkBeachMeshes.Height(x,z),z);
        }

        internal bool Clear(Vector3 point,float margin)
        {
            if(ParkBeachMeshes.LandDistance(point.x,point.z)<margin+.3f)return false;
            // Do not bury decorations in the rocky headlands or the mountain slopes.
            for(int i=0;i<5;i++)
            {
                var offset=i==0?Vector2.zero:new Vector2(Mathf.Sin(i*Mathf.PI*.5f),Mathf.Cos(i*Mathf.PI*.5f))*margin;
                if(ParkBeachMountainSetup.Height(point.x+offset.x,point.z+offset.y)>.12f)return false;
            }
            foreach(var rock in rocks)
                if(new Vector2(point.x-rock.x,point.z-rock.z).sqrMagnitude<(rock.y+margin)*(rock.y+margin))return false;
            return true;
        }

        internal Vector3[] CrabPath(int coast)
        {
            // Prefer long routes, then fit shorter ones into the exposed sand between headlands.
            var centers=new List<float>(coast==0?new[]{.43f,.62f,.28f}:coast==1?new[]{.25f,.43f,.68f}:new[]{.10f,.83f,.93f,.55f,.72f});
            for(int i=2;i<48;i++)centers.Add(i/50f);
            foreach(float span in new[]{.105f,.065f,.04f})
            foreach(float inset in new[]{1.4f,2.1f,2.9f})
            foreach(float center in centers)
            {
                if(center-span*.5f<0||center+span*.5f>1)continue;
                var path=new Vector3[33];bool clear=true;
                for(int i=0;i<path.Length;i++)
                {
                    path[i]=Point(coast,center+(i/32f-.5f)*span,inset+.10f*Mathf.Sin(i/32f*Mathf.PI*2));
                    if(!Clear(path[i],.65f))clear=false;
                }
                if(clear)return path;
            }
            throw new System.InvalidOperationException("No clear crab route on coast "+coast);
        }
    }
}
