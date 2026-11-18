using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkBeachMountainSetup
    {
        // Connected asymmetric masses: headlands, interior ridges, then the distant island skyline.
        static readonly Vector4[] peaks={
            new Vector4(-27,29,12,10),new Vector4(30,32,15,13),
            new Vector4(-5,58,43,13),new Vector4(-34,83,20,26),new Vector4(36,83,23,27),
            new Vector4(9,128,39,43),new Vector4(-39,124,29,35),new Vector4(49,136,30,36),
            new Vector4(-58,101,9,34),new Vector4(-23,138,10,43),new Vector4(-46,145,8,39),
            new Vector4(1,191,56,55),new Vector4(-76,184,38,43),new Vector4(75,185,42,45),
            new Vector4(24,176,11,55),new Vector4(-16,205,13,57)};

        internal static void Create(Transform parent)
        {
            var island=Group("Wild island ridges and ancient rainforest",parent,Vector3.zero);
            var rock=RockMaterial();
            ParkGeometry.Place("Continuous island mountain range",Terrain(),island,Vector3.zero,Vector3.one,rock);
            Canopy(island);
            ValleyMist(island);
            var waterfall=Group("Flowing summit waterfall",island,Vector3.zero);
            var motion=waterfall.gameObject.AddComponent<ParkWaterfall>();
            var water=new Material(Shader.Find("PeacePark/Waterfall")){name="Falling whitewater"};
            var ribbon=ParkGeometry.Place("Narrow animated cascade",Ribbon(),waterfall,Vector3.zero,Vector3.one,water);
            var renderer=ribbon.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            motion.Streams=new Renderer[]{renderer};
            var foam=new Material(Shader.Find("PeacePark/Waterfall Mist")){name="Soft waterfall spray"};
            var foot=StreamPoint(.85f);
            motion.Spray=Particles(waterfall,"Drifting splash droplets",foam,foot,false);
            motion.Mist=Particles(waterfall,"Soft mist above the jungle",foam,foot,true);
            motion.SetFlowTime(0);
        }

        internal static Material RockMaterial()
        {
            var rock=new Material(Shader.Find("PeacePark/Beach Mountain")){name="Fractured island rock and moss"};
            rock.SetTexture("_RockTexture",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Island/CliffRock.png"));
            return rock;
        }

        internal static float Height(float x,float z)
        {
            float height=0;
            for(int i=0;i<peaks.Length;i++)
            {
                var p=peaks[i];
                float warpX=(Mathf.PerlinNoise(x*.045f+51,z*.05f)*2-1)*3.8f;
                float warpZ=(Mathf.PerlinNoise(x*.055f+93,z*.04f)*2-1)*3.4f;
                float dx=(x+warpX-p.x)/p.z,dz=(z+warpZ-p.y)/(p.z*(i<2?1.6f:1.20f));
                float a=Mathf.Atan2(dz,dx);
                float r=Mathf.Sqrt(dx*dx+dz*dz)*(1+.10f*Mathf.Sin(a*5+i)+.055f*Mathf.Sin(a*11+i*3));
                if(r>=1)continue;
                float profile;
                if(p.z<15&&i>2)profile=Mathf.Pow(1-r,.30f);
                else if(r<.28f)profile=1-.23f*Mathf.SmoothStep(0,1,r/.28f);
                else if(r<.55f)profile=.77f-.13f*(r-.28f)/.27f;
                else if(r<.72f)profile=.64f-.43f*Mathf.SmoothStep(0,1,(r-.55f)/.17f);
                else profile=.21f*Mathf.Pow(Mathf.Clamp01((1-r)/.28f),.85f);
                height=Mathf.Max(height,p.w*profile);
            }
            if(height<=0)return 0;
            float detail=(Mathf.PerlinNoise(x*.16f+54,z*.12f)*2-1)*1.8f+
                .65f*Mathf.Sin(x*.81f+Mathf.Sin(z*.12f)*2)+.3f*Mathf.Sin(z*1.1f+x*.51f)+
                (Mathf.PerlinNoise(x*.75f+123,z*.64f)*2-1)*.85f;
            height+=detail*Mathf.SmoothStep(0,1,height/8);
            // A recessed gorge leaves an exposed, steep drop below the main summit's lip.
            if(z<119&&z>77)
            {
                float gorge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.8f,9,Mathf.Abs(x-4)));
                float lip=Mathf.Lerp(7,39,Mathf.SmoothStep(0,1,Mathf.InverseLerp(108,119,z)));
                height=Mathf.Lerp(height,Mathf.Min(height,lip),gorge);
            }
            return Mathf.Max(0,height);
        }

        static Mesh Terrain()
        {
            var points=new List<Vector3>();var triangles=new List<int>();
            const int columns=168,rows=180;
            for(int row=0;row<=rows;row++)
            for(int col=0;col<=columns;col++)
            {
                float x=Mathf.Lerp(-117,122,col/(float)columns),z=Mathf.Lerp(2,250,row/(float)rows);
                float h=Height(x,z);
                // Empty mountain cells follow the sandy seabed instead of covering it with a flat rock rectangle.
                float seabed=ParkBeachMeshes.Height(x,z)-.2f;
                float terrain=Mathf.Lerp(seabed,h-.1f,Mathf.SmoothStep(0,1,h/.7f));
                points.Add(new Vector3(x,terrain,z));
                if(row==rows||col==columns)continue;
                int v=row*(columns+1)+col;
                if((row+col)%2==0)triangles.AddRange(new[]{v,v+columns+1,v+1,v+1,v+columns+1,v+columns+2});
                else triangles.AddRange(new[]{v,v+columns+1,v+columns+2,v,v+columns+2,v+1});
            }
            var mesh=new Mesh{name="Interconnected craggy island ridges and headlands"};
            mesh.SetVertices(points);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }

        internal static Vector3 StreamPoint(float t)
        {
            float z=Mathf.Lerp(119.6f,108,t);
            return new Vector3(4+.14f*Mathf.Sin(t*6),Height(4,z)+.62f,z-.35f);
        }

        static Mesh Ribbon()
        {
            var points=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int rows=96,columns=8;
            for(int row=0;row<=rows;row++)
            for(int col=0;col<=columns;col++)
            {
                float t=row/(float)rows,u=col/(float)columns;
                var center=StreamPoint(t);
                center.x+=(u-.5f)*Mathf.Lerp(1.0f,2.1f,t);center.z-=Mathf.Sin(u*Mathf.PI)*.20f;
                points.Add(center);uv.Add(new Vector2(u,t));
                if(row==rows||col==columns)continue;
                int v=row*(columns+1)+col;triangles.AddRange(new[]{v,v+1,v+columns+1,v+1,v+columns+2,v+columns+1});
            }
            var mesh=new Mesh{name="Whitewater following the summit gorge"};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }

        static void Canopy(Transform parent)
        {
            var colors=new[]{Foliage("Island deep forest",new Color32(40,79,35,255)),Foliage("Island sunlit canopy",new Color32(76,109,37,255)),
                Foliage("Island fern canopy",new Color32(58,101,43,255))};
            var crown=Crown();var random=new System.Random(8217);
            var bough=ParkIslandLeaves.Bough();var leaf=ParkIslandLeaves.Material("Mountain rainforest leaf clusters",new Color(.77f,.91f,.78f));
            for(int row=0;row<60;row++)
            {
                var pieces=new Pieces();
                for(int col=0;col<72;col++)
                {
                    float x=-110+col*3.15f+(float)random.NextDouble()*2;
                    float z=14+row*3.9f+(float)random.NextDouble()*2;
                    float h=Height(x,z);
                    if(h<1||Mathf.Abs(x-4)<3&&z>102&&z<124)continue;
                    float slope=Mathf.Max(Mathf.Abs(Height(x+1,z)-Height(x-1,z)),Mathf.Abs(Height(x,z+1)-Height(x,z-1)));
                    if(slope>3.4f)continue;
                    float size=.8f+(float)random.NextDouble()*1.25f;
                    var turn=Quaternion.Euler(0,col*137.5f,0);
                    var center=new Vector3(x,h+.55f,z);
                    pieces.Add(crown,colors[(row+col)%3],center,new Vector3(size*1.35f,size*.82f,size*1.2f),turn);
                    if(z<110)
                        pieces.Add(bough,leaf,center+turn*new Vector3(size, size*.25f,0),Vector3.one*size*.55f,turn);
                }
                pieces.Build(Group($"Rainforest along ridge {row+1}",parent,Vector3.zero));
            }
        }

        internal static Material Foliage(string name,Color color)=>new Material(Shader.Find("PeacePark/Island Foliage")){name=name,color=color};
        internal static Mesh Crown()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int rings=9,sides=18;
            for(int ring=0;ring<=rings;ring++)
            for(int side=0;side<=sides;side++)
            {
                float a=side*Mathf.PI*2/sides,b=ring*Mathf.PI/rings;
                float radius=1+.12f*Mathf.Sin(a*5+b*7)+.06f*Mathf.Cos(a*11-b*3);
                vertices.Add(new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Cos(b),Mathf.Sin(a)*Mathf.Sin(b))*radius);
                if(ring==rings||side==sides)continue;
                int v=ring*(sides+1)+side;triangles.AddRange(new[]{v,v+1,v+sides+1,v+1,v+sides+2,v+sides+1});
            }
            var mesh=new Mesh{name="Irregular leafy canopy clusters"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }

        static void ValleyMist(Transform parent)
        {
            var material=new Material(Shader.Find("PeacePark/Island Mist")){name="Wisps drifting between jungle ridges"};
            var mesh=new Mesh{name="Soft horizontal valley mist ribbon"};
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();
            var places=new[]{new Vector3(-25,16,85),new Vector3(27,19,106),new Vector3(-54,26,151),new Vector3(22,31,161)};
            for(int i=0;i<places.Length;i++)
            {
                var placed=ParkGeometry.Place($"Valley cloud wisp {i+1}",mesh,parent,places[i],new Vector3(55+i*4,5.3f,1),material);
                placed.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }

        static ParticleSystem Particles(Transform parent,string name,Material material,Vector3 position,bool mist)
        {
            var root=Group(name,parent,position);root.localRotation=Quaternion.Euler(-90,0,0);
            var particles=root.gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=true;main.playOnAwake=true;main.duration=5;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.startLifetime=new ParticleSystem.MinMaxCurve(mist?2.2f:.9f,mist?4:1.7f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(mist?.3f:1,mist?1.1f:2.8f);
            main.startSize=new ParticleSystem.MinMaxCurve(mist?2.3f:.14f,mist?4.8f:.34f);
            main.startColor=new Color(.85f,.96f,1,mist?.23f:.65f);main.maxParticles=mist?45:65;main.gravityModifier=mist?0:.10f;
            var emission=particles.emission;emission.rateOverTime=mist?8:28;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=mist?55:42;shape.radius=mist?1.5f:.7f;
            var color=particles.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.5f));
            var render=particles.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=material;render.renderMode=ParticleSystemRenderMode.Billboard;render.shadowCastingMode=ShadowCastingMode.Off;render.receiveShadows=false;
            particles.useAutoRandomSeed=false;particles.randomSeed=mist?127u:89u;if(Application.isPlaying)particles.Play();return particles;
        }
    }
}
