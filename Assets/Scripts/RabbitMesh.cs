using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark
{
    // The skin is extracted from one smooth surface, with shared vertices at every joint.
    public static partial class RabbitMesh
    {
        const float HeadLift=.23f;
        const float HeadWidth=.94f;
        const float HeadHeight=.8f;
        const float HeadCenterY=2.85f;
        const float HeadDepth=.96f;
        const float PreviousHeadDepth=.62f;
        // The baked rigs include ground-contact samples for the shared walking pose.
        public const int GeometryVersion=11;
        public const float ShoulderHeight=2f;
        const float LegScale=.9f;
        const float TemplateHipHeight=1.26f;
        public const float HipHeight=TemplateHipHeight*LegScale;
        static float ShortenLegHeight(float y,out float derivative)
        {
            if(y<=1.3f){derivative=LegScale;return y*LegScale;}
            if(y>=1.8f){derivative=1;return y;}
            float t=(y-1.3f)/.5f,s=t*t*(3-2*t),offset=1.3f*(1-LegScale);
            derivative=1+offset*(6*t-6*t*t)/.5f;
            return y-offset*(1-s);
        }
        static float Blend(float a,float b,float k=.18f)
        {float h=Mathf.Clamp01(.5f+.5f*(b-a)/k);return Mathf.Lerp(b,a,h)-k*h*(1-h);}
        static float Ellipsoid(Vector3 p,Vector3 center,Vector3 radius)
        {
            var q=p-center;float k0=Vector3.Scale(q,new Vector3(1/radius.x,1/radius.y,1/radius.z)).magnitude;
            float k1=Vector3.Scale(q,new Vector3(1/(radius.x*radius.x),1/(radius.y*radius.y),1/(radius.z*radius.z))).magnitude;
            return k0*(k0-1)/Mathf.Max(.00001f,k1);
        }
        static float Capsule(Vector3 p,Vector3 a,Vector3 b,float r)
        {var ab=b-a;return (p-a-ab*Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude)).magnitude-r;}
        static float CoreTorso(Vector3 p)=>Ellipsoid(p,new Vector3(0,1.68f,0),new Vector3(.65f,.67f,.46f));
        static float Torso(Vector3 p)
        {
            float d=CoreTorso(p);
            // Broad, shallow shoulder slopes fill the dip beside the neck. The arm root
            // sits below this slope instead of forming a separate raised shoulder cap.
            for(int side=-1;side<=1;side+=2)
                d=Blend(d,Ellipsoid(p,new Vector3(side*.31f,2f,0),new Vector3(.35f,.28f,.34f)),.16f);
            return d;
        }
        static float Arm(Vector3 p,int side)=>Capsule(p,new Vector3(side*.57f,ShoulderHeight,0),new Vector3(side*1.35f,1.68f,-.02f),.215f);
        static float HeadSection(Vector3 point)
        {
            float x=point.x/HeadWidth;
            float y=(point.y-HeadCenterY)/HeadHeight;
            return Mathf.Sqrt(Mathf.Max(0,1-x*x-y*y));
        }
        public static float RaisedShoulderSurface(Vector3 p,Matrix4x4 inverseClavicle,Matrix4x4 inverseArm,int side)
        {
            float d=CoreTorso(p);
            d=Blend(d,Ellipsoid(p,new Vector3(-side*.31f,2f,0),new Vector3(.35f,.28f,.34f)),.16f);
            d=Blend(d,Ellipsoid(inverseClavicle.MultiplyPoint3x4(p),new Vector3(side*.31f,2f,0),new Vector3(.35f,.28f,.34f)),.2f);
            return Blend(d,Arm(inverseArm.MultiplyPoint3x4(p),side),.24f);
        }
        static float Field(Vector3 p,AnimalKind kind)
        {
            float d=Torso(p);
            // Keep a short visible neck between the shoulders and chin, without widening
            // the join into a direct head-to-torso union.
            d=Blend(d,Capsule(p,new Vector3(0,2.12f,0),new Vector3(0,2.46f,0),.31f),.14f);
            var headPoint=p-Vector3.up*HeadLift;
            if(kind==AnimalKind.Rabbit)
                d=Blend(d,Ellipsoid(headPoint,new Vector3(0,HeadCenterY,0),new Vector3(HeadWidth,HeadHeight,HeadDepth)),.1f);
            else if(p.y>1.68f)
                d=Blend(d,SpeciesHead(p,kind),.13f);
            for(int side=-1;side<=1;side+=2)
            {
                if(kind==AnimalKind.Rabbit)
                    d=Blend(d,Capsule(headPoint,new Vector3(side*.48f,3.4f,.02f),new Vector3(side*.59f,4.27f,.02f),.205f),.16f);
                d=Blend(d,Arm(p,side),.12f);
                d=Blend(d,Capsule(p,new Vector3(side*.32f,TemplateHipHeight,0),new Vector3(side*.33f,.32f,0),.265f),.2f);
                d=Blend(d,Ellipsoid(p,new Vector3(side*.33f,.24f,-.14f),new Vector3(.31f,.23f,.4f)),.13f);
            }
            // A rounded U-shaped opening leaves room for the inner thighs during a stride.
            float opening=Capsule(new Vector3(p.x,p.y,0),new Vector3(0,-.3f,0),new Vector3(0,.971f,0),.085f);
            d=-Blend(-d,opening,.07f);
            return Blend(d,SpeciesTail(p,kind),.14f);
        }
        static Vector3 Normal(Vector3 p,AnimalKind kind)
        {const float e=.002f;return new Vector3(Field(p+Vector3.right*e,kind)-Field(p-Vector3.right*e,kind),Field(p+Vector3.up*e,kind)-Field(p-Vector3.up*e,kind),Field(p+Vector3.forward*e,kind)-Field(p-Vector3.forward*e,kind)).normalized;}
        static float Smooth(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static BoneWeight Weight(Vector3 p,AnimalKind kind)
        {
            if(kind==AnimalKind.Elephant&&p.z<-.56f&&p.y>1.70f)
                return new BoneWeight{boneIndex0=1,weight0=1};
            if(kind!=AnimalKind.Rabbit&&p.z>.54f&&p.y<2.22f)
                return new BoneWeight{boneIndex0=0,weight0=1};
            int side=p.x<0?-1:1;
            if(p.y<1.61f&&Mathf.Abs(p.x)<.75f)
            {
                // Keep the whole foot on its leg bone. Fade in the inner-thigh blend above the foot.
                int legBone=side<0?4:5;
                if(p.y<=.55f)return new BoneWeight{boneIndex0=legBone,weight0=1};
                float innerThigh=Smooth(.06f,.29f,Mathf.Abs(p.x));
                float leg=(1-Smooth(.76f,1.54f,p.y))*Mathf.Lerp(1,innerThigh,Smooth(.55f,.95f,p.y));
                return OrderedWeights(0,1-leg,legBone,leg);
            }
            float region=Smooth(-.09f,.09f,Torso(p)-Arm(p,side));
            float head=Smooth(2.15f,2.55f,p.y);
            region*=Smooth(.5f,.88f,Mathf.Abs(p.x))*(1-Smooth(ShoulderHeight,ShoulderHeight+.18f,p.y))*(1-head);
            var axis=new Vector3(.78f,-.32f,0).normalized;
            float along=Vector3.Dot(new Vector3(Mathf.Abs(p.x)-.57f,p.y-ShoulderHeight,p.z),axis);
            float tip=Smooth(.04f,.34f,along);
            float arm=region*tip;
            // The full shoulder slope belongs partly to the clavicle, including vertices
            // above the old arm-only mask. Fade inward so the neck and chest stay stable.
            float cap=Smooth(.18f,.6f,Mathf.Abs(p.x))*Smooth(1.78f,2.08f,p.y)*(1-Smooth(2.18f,2.43f,p.y));
            float shoulder=Mathf.Max(region*(1-tip),cap*(1-head-arm));
            return OrderedWeights(0,1-arm-shoulder-head,side<0?6:7,shoulder,side<0?2:3,arm,1,head);
        }

        // Unity reads the weights in descending order. A head-only vertex must put
        // its single influence in the first slot, just like the facial details do.
        static BoneWeight OrderedWeights(int first,float a,int second,float b,int third=0,float c=0,int fourth=0,float d=0)
        {
            void SortPair(ref int x,ref float xWeight,ref int y,ref float yWeight)
            {
                if(xWeight>=yWeight)return;
                (x,y)=(y,x);
                (xWeight,yWeight)=(yWeight,xWeight);
            }
            SortPair(ref first,ref a,ref second,ref b);
            SortPair(ref third,ref c,ref fourth,ref d);
            SortPair(ref first,ref a,ref third,ref c);
            SortPair(ref second,ref b,ref fourth,ref d);
            SortPair(ref second,ref b,ref third,ref c);
            return new BoneWeight{boneIndex0=first,weight0=a,boneIndex1=second,weight1=b,
                boneIndex2=third,weight2=c,boneIndex3=fourth,weight3=d};
        }

        public static Mesh Build(AnimalKind kind,out int skinVertexCount,out int[] skinTriangles)
        {
            const float step=.065f;
            int nx=kind==AnimalKind.Elephant?59:53;
            int ny=kind==AnimalKind.Giraffe?88:81;
            int nz=kind==AnimalKind.Crocodile?57:kind==AnimalKind.Elephant||kind==AnimalKind.Giraffe?53:
                kind==AnimalKind.Triceratops?51:kind==AnimalKind.Tiger?41:kind==AnimalKind.Hippo?47:37;
            var origin=new Vector3(-(nx-1)/2*step,-.08f,-(nz-1)/2*step);
            var points=new Vector3[nx*ny*nz];var values=new float[points.Length];
            for(int z=0;z<nz;z++)for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
            {int id=x+nx*(y+ny*z);points[id]=origin+new Vector3(x,y,z)*step;values[id]=Field(points[id],kind);}
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var weights=new List<BoneWeight>();var uv=new List<Vector2>();var sides=new List<Vector2>();
            var restPositions=new List<Vector3>();
            var faces=new List<int>();var cache=new Dictionary<ulong,int>();
            int Vertex(int a,int b)
            {
                ulong key=((ulong)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
                if(cache.TryGetValue(key,out int found))return found;
                var p=Vector3.Lerp(points[a],points[b],values[a]/(values[a]-values[b]));
                var shortened=p;float newHeight=ShortenLegHeight(p.y,out float derivative);
                // In the bind A-pose the hands also reach this height. Restrict the
                // compression to the central lower body so arm length stays unchanged.
                float blend=Mathf.InverseLerp(.7f,.9f,Mathf.Abs(p.x));
                float lowerBody=1-blend*blend*(3-2*blend);
                float dyDx=(newHeight-p.y)*(-6*blend+6*blend*blend)/.2f*Mathf.Sign(p.x);
                float dyDy=1+lowerBody*(derivative-1);
                shortened.y=Mathf.Lerp(p.y,newHeight,lowerBody);
                var normal=Normal(p,kind);normal.x-=dyDx/dyDy*normal.y;normal.y/=dyDy;
                int id=vertices.Count;cache.Add(key,id);vertices.Add(shortened);normals.Add(normal.normalized);weights.Add(Weight(p,kind));
                // UVs follow the lowered drawing pose although the rig is bound in an A-pose.
                var drawingPoint=p;var w=weights[id];float influence=0;
                if(w.boneIndex0==2||w.boneIndex0==3)influence+=w.weight0;
                if(w.boneIndex1==2||w.boneIndex1==3)influence+=w.weight1;
                if(w.boneIndex2==2||w.boneIndex2==3)influence+=w.weight2;
                if(w.boneIndex3==2||w.boneIndex3==3)influence+=w.weight3;
                if(influence>0){var pivot=new Vector3(p.x<0?-.57f:.57f,ShoulderHeight,0);drawingPoint=Vector3.Lerp(p,pivot+Quaternion.Euler(0,0,p.x<0?40:-40)*(p-pivot),influence);}
                drawingPoint.y-=HeadLift*Smooth(2.15f,2.55f,p.y);
                // Stretch the existing drawing's leg region onto the longer 3D legs,
                // while preserving the old shoulder/head paint landmarks.
                if(drawingPoint.y<1.057f)drawingPoint.y*=.655f/1.057f;
                else if(drawingPoint.y<2.35f)drawingPoint.y=Mathf.Lerp(.655f,2.35f,Mathf.InverseLerp(1.057f,2.35f,drawingPoint.y));
                Vector2 paintUv=new Vector2((drawingPoint.x+1.3f)/2.6f,drawingPoint.y/4.5f);
                if(kind!=AnimalKind.Rabbit)paintUv=SpeciesUv(kind,p,drawingPoint);
                uv.Add(paintUv);
                // Keep the front/back paint transition proportional around the fuller cheeks.
                float skull=Smooth(2.3f,2.7f,p.y)*(1-Smooth(3.66f,3.9f,p.y));
                float sideBlend=.18f*Mathf.Lerp(1,HeadDepth/PreviousHeadDepth,skull);
                sides.Add(new Vector2(Mathf.SmoothStep(0,1,Mathf.InverseLerp(-sideBlend,sideBlend,p.z)),0));
                restPositions.Add(p);
                return id;
            }
            void Triangle(int a,int b,int c)
            {
                if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0){int swap=b;b=c;c=swap;}
                faces.Add(a);faces.Add(b);faces.Add(c);
            }
            void Tetra(int a,int b,int c,int d)
            {
                bool sign=values[a]<0;
                if((values[b]<0)==sign&&(values[c]<0)==sign&&(values[d]<0)==sign)return;
                int[] ids={a,b,c,d};var inside=new List<int>(4);var outside=new List<int>(4);
                foreach(int id in ids){if(values[id]<0)inside.Add(id);else outside.Add(id);}
                if(inside.Count==0||inside.Count==4)return;
                if(inside.Count==1||inside.Count==3)
                {var one=inside.Count==1?inside:outside;var three=inside.Count==1?outside:inside;Triangle(Vertex(one[0],three[0]),Vertex(one[0],three[1]),Vertex(one[0],three[2]));}
                else
                {int p=Vertex(inside[0],outside[0]),q=Vertex(inside[0],outside[1]),r=Vertex(inside[1],outside[0]),s=Vertex(inside[1],outside[1]);Triangle(p,q,r);Triangle(q,s,r);}
            }
            for(int z=0;z<nz-1;z++)for(int y=0;y<ny-1;y++)for(int x=0;x<nx-1;x++)
            {
                int a=x+nx*(y+ny*z),b=a+1,c=a+nx,d=c+1,e=a+nx*ny,f=e+1,g=e+nx,h=g+1;
                Tetra(a,b,d,h);Tetra(a,d,c,h);Tetra(a,c,g,h);Tetra(a,g,e,h);Tetra(a,e,f,h);Tetra(a,f,b,h);
            }
            skinVertexCount=vertices.Count;skinTriangles=faces.ToArray();
            var submeshes=new[]{faces,new List<int>(),new List<int>(),new List<int>()};
            void Detail(Vector3 center,Vector3 radius,int material,float roll,int bone)
            {
                int start=vertices.Count;const int rings=10,segments=16;
                var rotation=Quaternion.Euler(0,0,roll);
                for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++)
                {
                    float v=y*Mathf.PI/rings,u=x*Mathf.PI*2/segments;var n=new Vector3(Mathf.Sin(v)*Mathf.Cos(u),Mathf.Cos(v),Mathf.Sin(v)*Mathf.Sin(u));
                    vertices.Add(center+rotation*Vector3.Scale(n,radius));normals.Add(rotation*new Vector3(n.x/radius.x,n.y/radius.y,n.z/radius.z).normalized);
                    weights.Add(new BoneWeight{boneIndex0=bone,weight0=1});uv.Add(Vector2.zero);sides.Add(Vector2.zero);
                    restPositions.Add(center);
                }
                for(int y=0;y<rings;y++)for(int x=0;x<segments;x++)
                {int a=start+y*(segments+1)+x,b=a+1,c=a+segments+1,d=c+1;submeshes[material].AddRange(new[]{a,b,c,b,d,c});}
            }
            void FaceDetail(Vector3 center,Vector3 radius,int material)
            {
                // Preserve each feature's inset into the skin as the front surface moves.
                center.z-=(HeadDepth-PreviousHeadDepth)*HeadSection(center);
                Detail(center+Vector3.up*HeadLift,radius,material,0,1);
            }
            if(kind!=AnimalKind.Rabbit)AddSpeciesDetails(kind,Detail);
            else
            {
                for(int side=-1;side<=1;side+=2)
                {
                    FaceDetail(new Vector3(side*.34f,2.99f,-.574f),new Vector3(.075f,.105f,.055f),1);
                    FaceDetail(new Vector3(side*.355f,3.025f,-.624f),Vector3.one*.022f,3);
                    FaceDetail(new Vector3(side*.58f,2.7f,-.48f),new Vector3(.13f,.075f,.035f),2);
                    Detail(new Vector3(side*.54f,3.94f+HeadLift,-.171f),new Vector3(.105f,.41f,.04f),2,0,1);
                    FaceDetail(new Vector3(side*.125f,2.66f,-.585f),new Vector3(.16f,.115f,.065f),3);
                }
                FaceDetail(new Vector3(0,2.76f,-.666f),new Vector3(.09f,.06f,.045f),2);
                for(int s=-1;s<=1;s+=2)for(int i=0;i<10;i++)
                {float t=i/9f;FaceDetail(new Vector3(s*t*.13f,2.66f-.045f*Mathf.Sin(t*Mathf.PI),-.653f),new Vector3(.018f,.018f,.012f),1);}
            }
            var mesh=new Mesh{name=kind+" continuous skinned surface",
                indexFormat=vertices.Count<=ushort.MaxValue?IndexFormat.UInt16:IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,sides);mesh.SetUVs(2,restPositions);mesh.boneWeights=weights.ToArray();mesh.subMeshCount=4;
            for(int i=0;i<4;i++)mesh.SetTriangles(submeshes[i],i);mesh.RecalculateBounds();
            RabbitShoulderCorrective.SmoothWeights(mesh,skinVertexCount,skinTriangles);return mesh;
        }
    }
}
