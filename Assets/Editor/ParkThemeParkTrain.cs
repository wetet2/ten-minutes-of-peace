using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkThemeParkSetup
    {
        ParkElephantTrain MakeTrain(Transform root)
        {
            var train=Group("Elephant train",root,Vector3.zero).gameObject.AddComponent<ParkElephantTrain>();
            var cars=new Transform[TrainCarCount];
            var axles=new List<Transform>(2*cars.Length);
            var couplers=new Transform[cars.Length-1];
            var colors=new[]{blue,yellow,coral,mint,pink,Paint("Lavender carriage enamel","#AE9BDD")};
            for(int i=0;i<cars.Length;i++)
            {
                var car=cars[i]=Group(i==0?"Elephant locomotive":"Passenger car "+i,train.transform,Vector3.zero);
                var shell=new Pieces();
                shell.Add(box,ink,new Vector3(0,.41f,0),new Vector3(2.66f,.23f,3.16f));
                shell.Add(box,colors[i],new Vector3(0,.535f,0),new Vector3(2.90f,.21f,3.16f));
                if(i==0) ElephantEngine(shell);
                else PassengerCar(shell,colors[i]);
                shell.Build(car);
                foreach(float z in new[]{-.95f,.95f})
                {
                    var axle=Group("Rolling axle",car,new Vector3(0,ParkElephantTrain.WheelRadius,z));
                    var wheels=new Pieces();
                    foreach(int side in new[]{-1,1})
                    {
                        float x=side*.79f;
                        wheels.Add(cylinder,ink,new Vector3(x,0,0),new Vector3(.62f,.145f,.62f),Quaternion.Euler(0,0,90));
                        wheels.Add(cylinder,yellow,new Vector3(x+side*.146f,0,0),new Vector3(.49f,.045f,.49f),Quaternion.Euler(0,0,90));
                        wheels.Add(cylinder,coral,new Vector3(x+side*.195f,0,0),new Vector3(.17f,.03f,.17f),Quaternion.Euler(0,0,90));
                        for(int spoke=0;spoke<4;spoke++)
                        {
                            var normal=Quaternion.Euler(spoke*90,0,0);
                            var center=normal*Vector3.up*.15f+Vector3.right*(x+side*.195f);
                            wheels.Add(box,cream,center,new Vector3(.025f,.10f,.032f),normal);
                        }
                    }
                    wheels.Build(axle);
                    axles.Add(axle);
                }
            }
            for(int i=0;i<couplers.Length;i++)
                couplers[i]=ParkGeometry.Place("Coupler "+(i+1),box,train.transform,Vector3.zero,Vector3.one,ink);
            train.Configure(cars,axles.ToArray(),couplers);
            return train;
        }

        void ElephantEngine(Pieces parts)
        {
            var blueShade=Paint("Elephant dark blue", "#4094C9");
            parts.Add(box,blueShade,new Vector3(0,1.39f,-.57f),new Vector3(2.58f,1.63f,1.69f));
            parts.Add(box,blue,new Vector3(0,2.21f,-.63f),new Vector3(2.78f,.43f,1.85f));
            parts.Add(box,cream,new Vector3(0,1.52f,-1.43f),new Vector3(.84f,.63f,.07f));
            parts.Add(sphere,blue,new Vector3(0,1.68f,.74f),new Vector3(2.18f,2.10f,1.98f));
            foreach(int side in new[]{-1,1})
            {
                var ear=Quaternion.Euler(2,side*15,side*13);
                parts.Add(sphere,blue,new Vector3(side*1.06f,1.69f,.23f),new Vector3(1.14f,1.53f,.43f),ear);
                parts.Add(sphere,pink,new Vector3(side*1.12f,1.69f,.421f),new Vector3(.82f,1.16f,.12f),ear);
                parts.Add(sphere,ink,new Vector3(side*.62f,1.93f,1.55f),new Vector3(.23f,.29f,.16f),Quaternion.Euler(0,side*27,0));
                parts.Add(sphere,cream,new Vector3(side*.635f,1.99f,1.62f),Vector3.one*.08f);
                parts.Add(sphere,coral,new Vector3(side*.76f,1.55f,1.43f),new Vector3(.29f,.21f,.095f),Quaternion.Euler(0,side*37,0));
            }
            Vector3 nose=new Vector3(0,1.57f,1.44f),tip=new Vector3(0,2.23f,2.54f);
            var trunk=TrunkCurve(nose,new Vector3(0,.91f,1.93f),new Vector3(0,1.37f,2.95f),tip);
            parts.Add(trunk,blue,Vector3.zero,Vector3.one);
            parts.Add(sphere,blue,tip,Vector3.one*.54f);
            parts.Add(sphere,blueShade,tip+new Vector3(0,.26f,-.02f),new Vector3(.29f,.075f,.25f));
            parts.Add(cylinder,coral,new Vector3(0,2.86f,-.43f),new Vector3(.48f,.29f,.48f));
            parts.Add(cylinder,yellow,new Vector3(0,3.13f,-.43f),new Vector3(.68f,.09f,.68f));
            parts.Add(box,yellow,new Vector3(0,.56f,1.67f),new Vector3(2.31f,.42f,.57f),Quaternion.Euler(-14,0,0));
            for(int slat=-3;slat<=3;slat++)
                parts.Add(box,cream,new Vector3(slat*.28f,.59f,1.978f),new Vector3(.065f,.29f,.067f),Quaternion.Euler(-14,0,0));
        }

        static Mesh TrunkCurve(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            const int rings=24,sides=16;
            var v=new List<Vector3>();
            var t=new List<int>();
            for(int j=0;j<=rings;j++)
            {
                float s=(float)j/rings,k=1-s;
                var p=k*k*k*a+3*k*k*s*b+3*k*s*s*c+s*s*s*d;
                var tangent=(k*k*(b-a)+2*k*s*(c-b)+s*s*(d-c)).normalized;
                var cross=Vector3.Cross(tangent,Vector3.right);
                float width=Mathf.Lerp(.45f,.25f,s);
                for(int i=0;i<sides;i++)
                {
                    float angle=i*Mathf.PI*2/sides;
                    v.Add(p+(Vector3.right*Mathf.Cos(angle)+cross*Mathf.Sin(angle))*width);
                    if(j==rings)continue;
                    int index=j*sides+i,next=j*sides+(i+1)%sides;
                    t.AddRange(new[]{index,next,index+sides,next,next+sides,index+sides});
                }
            }
            var mesh=new Mesh{name="Rounded upward elephant trunk"};
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }

        void PassengerCar(Pieces parts,Material color)
        {
            parts.Add(box,color,new Vector3(0,.95f,-1.43f),new Vector3(2.90f,.65f,.25f));
            parts.Add(box,color,new Vector3(0,.94f,1.44f),new Vector3(2.9f,.61f,.25f));
            parts.Add(box,color,new Vector3(1.34f,.945f,0),new Vector3(.23f,.62f,2.90f));
            foreach(float z in new[]{-1.10f,1.155f})
                parts.Add(box,color,new Vector3(-1.34f,.95f,z),new Vector3(.23f,.63f,.69f));
            parts.Add(box,color,new Vector3(-1.59f,.26f,.32f),new Vector3(.59f,.14f,.89f));
            parts.Add(box,cream,new Vector3(0,.825f,-.64f),new Vector3(2.60f,.27f,.90f));
            parts.Add(box,cream,new Vector3(0,1.19f,-1.02f),new Vector3(2.59f,.61f,.26f));
            parts.Add(box,cream,new Vector3(0,1.25f,1.44f),new Vector3(2.76f,.11f,.18f));
            parts.Beam(cylinder,steel,new Vector3(-1.13f,1.13f,.90f),new Vector3(1.13f,1.13f,.90f),.052f);
            foreach(int side in new[]{-1,1})
                parts.Beam(cylinder,steel,new Vector3(side*1.13f,.71f,.90f),new Vector3(side*1.13f,1.13f,.90f),.052f);
        }
    }
}
