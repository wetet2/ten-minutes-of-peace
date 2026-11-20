using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed partial class ParkPlayground
    {
        static readonly ParkAnimalAction[] ThemeParkActions =
            { ParkAnimalAction.Wheel, ParkAnimalAction.Train, ParkAnimalAction.Carousel };
        static readonly ParkAnimalAction[] PlaygroundActions =
            { ParkAnimalAction.Slide, ParkAnimalAction.Trampoline, ParkAnimalAction.Seesaw, ParkAnimalAction.Roundabout };
        static readonly ParkAnimalAction[] GroundActions = { ParkAnimalAction.Hop, ParkAnimalAction.Dance };
        static readonly ParkAnimalAction[] ForestActions =
            { ParkAnimalAction.StumpHop, ParkAnimalAction.WoodenBridge, ParkAnimalAction.Zipline };
        static readonly ParkAnimalAction[] DesertActions = { ParkAnimalAction.SandSled, ParkAnimalAction.Hop, ParkAnimalAction.Dance };

        internal ParkAnimalAction[] AnimalActions => SandSled ? DesertActions : ForestRides?.Length>0 ? ForestActions :
            Carousel ? ThemeParkActions : Slide ? PlaygroundActions : GroundActions;
        internal float ActionToastSpace=>ActionAnimal?90:0;

        ParkRabbitAgent ActionAnimal
        {
            get
            {
                if(!IsViewingPark||dialog!=Dialog.None||!OrbitCamera)return null;
                var animal=OrbitCamera.FollowTarget;
                return CanFollowAnimal(animal)?animal:null;
            }
        }

        internal bool RequestAnimalAction(ParkAnimalAction action)
        {
            var animal=ActionAnimal;
            return animal&&Array.IndexOf(AnimalActions,action)>=0&&animal.RequestAction(action);
        }

        internal bool TryRequestForestRide(ParkRabbitAgent animal,ParkAnimalAction action)
        {
            if(ForestRides==null)return false;
            foreach(var ride in ForestRides)
                if(ride&&ride.Action==action)return ride.TryQueue(animal);
            return false;
        }

        internal static string ActionLabel(ParkAnimalAction action) => action switch
        {
            ParkAnimalAction.Wheel => "대관람차 타기",
            ParkAnimalAction.Train => "코끼리 기차 타기",
            ParkAnimalAction.Carousel => "회전목마 타기",
            ParkAnimalAction.Slide => "미끄럼틀 타기",
            ParkAnimalAction.Trampoline => "방방 타기",
            ParkAnimalAction.Seesaw => "시소 타기",
            ParkAnimalAction.Roundabout => "뺑뺑이 타기",
            ParkAnimalAction.Hop => "깡충 뛰기",
            ParkAnimalAction.Dance => "춤추기",
            ParkAnimalAction.StumpHop => "그루터기 건너기",
            ParkAnimalAction.WoodenBridge => "구름사다리 건너기",
            ParkAnimalAction.Zipline => "짚라인 타기",
            ParkAnimalAction.SandSled => "모래썰매 타기",
            _ => ""
        };

        static Rect ActionButtonRow(float width,float height,int count)
        {
            float total=Mathf.Min(width-32,count*242+(count-1)*10);
            return new Rect((width-total)*.5f,height-101,total,68);
        }

        void DrawAnimalActions(float width,float height)
        {
            var animal=ActionAnimal;
            if(!animal)return;
            var actions=AnimalActions;
            var row=ActionButtonRow(width,height,actions.Length);
            float buttonWidth=(row.width-(actions.Length-1)*10)/actions.Length;
            for(int i=0;i<actions.Length;i++)
            {
                var bounds=new Rect(row.x+i*(buttonWidth+10),row.y,buttonWidth,row.height);
                var action=actions[i];
                bool selected=!animal.CanRequestAction(action);
                if(HudButton(bounds,ActionLabel(action),selected))RequestAnimalAction(action);
            }
        }

        bool ActionButtonsContain(Vector2 screenPoint,Rect safe,float scale)
        {
            if(scale<=0||!ActionAnimal)return false;
            var localPoint=new Vector2((screenPoint.x-safe.xMin)/scale,(safe.yMax-screenPoint.y)/scale);
            return ActionButtonRow(safe.width/scale,safe.height/scale,AnimalActions.Length).Contains(localPoint);
        }

        internal bool TryBuildRidePath(Vector3 origin,Vector3 target,out Vector3[] path)
        {
            path=null;
            var area=WalkArea;
            if(!area.Contains(new Vector2(target.x,target.z))||!IsSceneryClear(target))return false;
            if(WalkingSegmentClear(origin,target))
            {
                path=Array.Empty<Vector3>();
                return true;
            }

            const float cellSize=.60f;
            int columns=Mathf.CeilToInt(area.width/cellSize);
            int rows=Mathf.CeilToInt(area.height/cellSize);
            int count=columns*rows;
            var parents=new int[count];
            var frontier=new int[count];
            Array.Fill(parents,-2);
            float dx=area.width/columns,dz=area.height/rows;
            int startX=Mathf.Clamp(Mathf.FloorToInt((origin.x-area.xMin)/dx),0,columns-1);
            int startZ=Mathf.Clamp(Mathf.FloorToInt((origin.z-area.yMin)/dz),0,rows-1);
            int read=0,write=0,finish=-1;
            for(int z=Mathf.Max(0,startZ-2);z<=Mathf.Min(rows-1,startZ+2);z++)
                for(int x=Mathf.Max(0,startX-2);x<=Mathf.Min(columns-1,startX+2);x++)
                    Enqueue(x,z,-1,origin);
            while(read<write)
            {
                int current=frontier[read++];
                var point=GridPoint(current);
                if((point-target).sqrMagnitude<2.25f&&WalkingSegmentClear(point,target))
                {
                    finish=current;
                    break;
                }
                int x=current%columns,z=current/columns;
                Enqueue(x-1,z,current,point);
                Enqueue(x+1,z,current,point);
                Enqueue(x,z-1,current,point);
                Enqueue(x,z+1,current,point);
            }
            if(finish<0)return false;

            var reverse=new List<Vector3>();
            for(int node=finish;node>=0;node=parents[node])reverse.Add(GridPoint(node));
            var waypoints=new List<Vector3>();
            var previous=origin;
            for(int i=reverse.Count-1;i>=0;)
            {
                if(WalkingSegmentClear(previous,target))break;
                int next=i;
                while(next>0&&WalkingSegmentClear(previous,reverse[next-1]))next--;
                previous=reverse[next];
                waypoints.Add(previous);
                i=next-1;
            }
            path=waypoints.ToArray();
            return true;

            Vector3 GridPoint(int cell) =>
                new Vector3(area.xMin+(cell%columns+.5f)*dx,target.y,area.yMin+(cell/columns+.5f)*dz);

            void Enqueue(int x,int z,int parent,Vector3 from)
            {
                if(x<0||x>=columns||z<0||z>=rows)return;
                int cell=z*columns+x;
                if(parents[cell]!=-2||!WalkingSegmentClear(from,GridPoint(cell)))return;
                parents[cell]=parent;
                frontier[write++]=cell;
            }
        }

        internal bool WalkingSegmentClear(Vector3 from,Vector3 to)
        {
            if(walkObstacles!=null)
                foreach(var obstacle in walkObstacles)
                {
                    float enter=0,leave=1;
                    if(ClipAxis(from.x,to.x,obstacle.xMin,obstacle.xMax,ref enter,ref leave)&&
                        ClipAxis(from.z,to.z,obstacle.yMin,obstacle.yMax,ref enter,ref leave))return false;
                }
            if(!Fountain&&pondRadii.x<=0)return true;
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/.22f));
            for(int i=1;i<=steps;i++)
                if(!IsSceneryClear(Vector3.Lerp(from,to,(float)i/steps)))return false;
            return true;
        }

        static bool ClipAxis(float from,float to,float min,float max,ref float enter,ref float leave)
        {
            float delta=to-from;
            if(Mathf.Abs(delta)<.00001f)return from>=min&&from<=max;
            float a=(min-from)/delta,b=(max-from)/delta;
            enter=Mathf.Max(enter,Mathf.Min(a,b));
            leave=Mathf.Min(leave,Mathf.Max(a,b));
            return enter<=leave;
        }
    }
}
