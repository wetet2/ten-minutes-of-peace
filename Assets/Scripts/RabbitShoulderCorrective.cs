using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    // Fair the skin-weight field, then build pose-space, volume-preserving corrections.
    // These run once at creation; normal playback uses Unity's skinned blend shapes.
    public static class RabbitShoulderCorrective
    {
        static float Smooth(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static List<int>[] Neighbours(int count,int[] triangles)
        {
            var result=new List<int>[count];for(int i=0;i<count;i++)result[i]=new List<int>(8);
            void Link(int a,int b){if(!result[a].Contains(b))result[a].Add(b);if(!result[b].Contains(a))result[b].Add(a);}
            for(int i=0;i<triangles.Length;i+=3){Link(triangles[i],triangles[i+1]);Link(triangles[i+1],triangles[i+2]);Link(triangles[i+2],triangles[i]);}
            return result;
        }
        static float Mask(Vector3 p,bool both=false)
        {
            float x=both?Mathf.Abs(p.x):p.x;
            return Smooth(.18f,.4f,x)*(1-Smooth(1f,1.25f,x))*Smooth(1.55f,1.83f,p.y)*(1-Smooth(2.22f,2.48f,p.y));
        }
        public static void SmoothWeights(Mesh mesh,int count,int[] triangles)
        {
            var neighbours=Neighbours(count,triangles);var points=mesh.vertices;var weights=mesh.boneWeights;
            var current=new float[count*8];var next=new float[current.Length];
            for(int i=0;i<count;i++)
            {
                var w=weights[i];current[i*8+w.boneIndex0]+=w.weight0;current[i*8+w.boneIndex1]+=w.weight1;
                current[i*8+w.boneIndex2]+=w.weight2;current[i*8+w.boneIndex3]+=w.weight3;
            }
            for(int iteration=0;iteration<24;iteration++)
            {
                for(int i=0;i<count;i++)for(int bone=0;bone<8;bone++)
                {
                    float sum=0;foreach(int other in neighbours[i])sum+=current[other*8+bone];
                    next[i*8+bone]=Mathf.Lerp(current[i*8+bone],sum/Mathf.Max(1,neighbours[i].Count),Mask(points[i],true)*.65f);
                }
                var swap=current;current=next;next=swap;
            }
            var selected=new int[4];var values=new float[4];
            for(int i=0;i<count;i++)
            {
                if(Mask(points[i],true)==0)continue;
                for(int j=0;j<4;j++){selected[j]=0;values[j]=0;}
                for(int bone=0;bone<8;bone++)
                {
                    float value=current[i*8+bone];
                    for(int j=0;j<4;j++)if(value>values[j])
                    {for(int k=3;k>j;k--){values[k]=values[k-1];selected[k]=selected[k-1];}values[j]=value;selected[j]=bone;break;}
                }
                float sum=values[0]+values[1]+values[2]+values[3];
                weights[i]=new BoneWeight{boneIndex0=selected[0],weight0=values[0]/sum,boneIndex1=selected[1],weight1=values[1]/sum,boneIndex2=selected[2],weight2=values[2]/sum,boneIndex3=selected[3],weight3=values[3]/sum};
            }
            mesh.boneWeights=weights;
        }
        static Quaternion Add(Quaternion a,Quaternion b,float weight)
        {return new Quaternion(a.x+b.x*weight,a.y+b.y*weight,a.z+b.z*weight,a.w+b.w*weight);}
        static Quaternion Scale(Quaternion a,float value)=>new Quaternion(a.x*value,a.y*value,a.z*value,a.w*value);
        static void Fair(Vector3[] points,Vector3[] work,List<int>[] neighbours,float[] mask,float amount)
        {
            for(int i=0;i<neighbours.Length;i++)
            {
                var average=Vector3.zero;foreach(int other in neighbours[i])average+=points[other];
                work[i]=Vector3.LerpUnclamped(points[i],average/Mathf.Max(1,neighbours[i].Count),amount*mask[i]);
            }
            for(int i=0;i<neighbours.Length;i++)points[i]=work[i];
        }
        public static void Build(RabbitCharacter rabbit)
        {
            var skin=rabbit.Skin;var mesh=skin.sharedMesh;var rest=mesh.vertices;var normals=mesh.normals;var weights=mesh.boneWeights;
            var bind=mesh.bindposes;var bones=skin.bones;int count=rabbit.SkinVertexCount;
            var neighbours=Neighbours(count,rabbit.SkinTriangles);var mask=new float[rest.Length];
            for(int i=0;i<count;i++)mask[i]=Mask(rest[i],true);
            const int frames=20;
            for(int clip=0;clip<3;clip++)
            for(int frame=0;frame<=(clip==0?0:frames);frame++)
            {
                float t=(float)frame/frames;
                if(clip==0)rabbit.PlayIdlePose();
                else if(clip==1)rabbit.PlayWalkingPose(t*Mathf.PI*2);
                else rabbit.PlayWavePose(t);
                var matrices=new Matrix4x4[bones.Length];var rotations=new Quaternion[bones.Length];var duals=new Quaternion[bones.Length];
                for(int i=0;i<bones.Length;i++)
                {
                    matrices[i]=rabbit.transform.worldToLocalMatrix*bones[i].localToWorldMatrix*bind[i];
                    rotations[i]=matrices[i].rotation;var translation=matrices[i].GetColumn(3);
                    duals[i]=Scale(new Quaternion(translation.x,translation.y,translation.z,0)*rotations[i],.5f);
                }
                var targets=new Vector3[rest.Length];var linear=new Matrix4x4[rest.Length];
                for(int i=0;i<rest.Length;i++)
                {
                    var w=weights[i];var reference=rotations[w.boneIndex0];var real=new Quaternion(0,0,0,0);var dual=new Quaternion(0,0,0,0);var matrix=new Matrix4x4();
                    void Accumulate(int bone,float weight)
                    {
                        if(weight<=0)return;float sign=Quaternion.Dot(reference,rotations[bone])<0?-1:1;
                        real=Add(real,rotations[bone],weight*sign);dual=Add(dual,duals[bone],weight*sign);
                        for(int k=0;k<16;k++)matrix[k]+=matrices[bone][k]*weight;
                    }
                    Accumulate(w.boneIndex0,w.weight0);Accumulate(w.boneIndex1,w.weight1);Accumulate(w.boneIndex2,w.weight2);Accumulate(w.boneIndex3,w.weight3);
                    float length=Mathf.Sqrt(Quaternion.Dot(real,real));real=Scale(real,1/length);dual=Scale(dual,1/length);
                    dual=Add(dual,real,-Quaternion.Dot(real,dual));var translated=Scale(dual*Quaternion.Inverse(real),2);
                    targets[i]=real*rest[i]+new Vector3(translated.x,translated.y,translated.z);linear[i]=matrix;
                }
                var work=new Vector3[rest.Length];
                // Sculpt the raised junction as a smooth union of chest, moving shoulder
                // and arm. This removes the crease left by rotation blending alone.
                var inverseBody=matrices[0].inverse;
                for(int i=0;i<count;i++)if(mask[i]>.01f)
                {
                    int side=rest[i].x<0?-1:1;
                    var inverseClavicle=matrices[side<0?6:7].inverse;var inverseArm=matrices[side<0?2:3].inverse;
                    float Surface(Vector3 point)=>RabbitMesh.RaisedShoulderSurface(point,inverseBody,inverseClavicle,inverseArm,side);
                    var w=weights[i];float head=0;
                    if(w.boneIndex0==1)head+=w.weight0;if(w.boneIndex1==1)head+=w.weight1;
                    if(w.boneIndex2==1)head+=w.weight2;if(w.boneIndex3==1)head+=w.weight3;
                    float strength=mask[i]*(1-Smooth(.1f,.35f,head));
                    var p=targets[i];
                    for(int pass=0;pass<6;pass++)
                    {
                        const float e=.003f;float d=Surface(p);
                        var gradient=new Vector3(Surface(p+Vector3.right*e)-Surface(p-Vector3.right*e),Surface(p+Vector3.up*e)-Surface(p-Vector3.up*e),Surface(p+Vector3.forward*e)-Surface(p-Vector3.forward*e))/(2*e);
                        p-=Vector3.ClampMagnitude(gradient*d/Mathf.Max(.01f,gradient.sqrMagnitude),.06f);
                    }
                    targets[i]=Vector3.Lerp(targets[i],p,strength);
                }
                for(int pass=0;pass<12;pass++){Fair(targets,work,neighbours,mask,.5f);Fair(targets,work,neighbours,mask,-.45f);}
                for(int i=0;i<rest.Length;i++)targets[i]=Vector3.Lerp(linear[i].MultiplyPoint3x4(rest[i]),targets[i],mask[i]);
                var targetNormals=new Vector3[rest.Length];var triangles=rabbit.SkinTriangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var n=Vector3.Cross(targets[b]-targets[a],targets[c]-targets[a]);
                    targetNormals[a]+=n;targetNormals[b]+=n;targetNormals[c]+=n;
                }
                var delta=new Vector3[rest.Length];var deltaNormals=new Vector3[rest.Length];
                for(int i=0;i<count;i++)if(mask[i]>0)
                {
                    var inverse=linear[i].inverse;
                    delta[i]=inverse.MultiplyVector(targets[i]-linear[i].MultiplyPoint3x4(rest[i]));
                    deltaNormals[i]=(inverse.MultiplyVector(targetNormals[i].normalized)-normals[i])*mask[i];
                }
                mesh.AddBlendShapeFrame(clip==0?"Relaxed shoulders":clip==1?"Walking shoulder volume":"Waving shoulder volume",clip==0?100:1+t*99,delta,deltaNormals,null);
            }
            rabbit.PlayIdlePose();
        }
    }
}
