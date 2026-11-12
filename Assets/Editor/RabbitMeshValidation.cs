using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeacePark.Editor
{
    static class RabbitMeshValidation
    {
        public static void Check(RabbitCharacter rabbit)
        {
            int count=rabbit.SkinVertexCount;var parent=new int[count];for(int i=0;i<count;i++)parent[i]=i;
            int Root(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
            void Join(int a,int b){parent[Root(a)]=Root(b);}
            var edges=new Dictionary<ulong,int>();
            void Edge(int a,int b){ulong key=((ulong)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);edges.TryGetValue(key,out int n);edges[key]=n+1;}
            var triangles=rabbit.SkinTriangles;
            for(int i=0;i<triangles.Length;i+=3)
            {int a=triangles[i],b=triangles[i+1],c=triangles[i+2];Join(a,b);Join(a,c);Edge(a,b);Edge(b,c);Edge(c,a);}
            var components=new HashSet<int>();for(int i=0;i<count;i++)components.Add(Root(i));
            if(components.Count!=1)throw new Exception("Rabbit skin has disconnected components: "+components.Count);
            foreach(var edge in edges)if(edge.Value!=2)throw new Exception("Rabbit skin has an open or nonmanifold edge");
            if(rabbit.GetComponentsInChildren<Renderer>().Length!=1)throw new Exception("Rabbit must use one renderer");
            var mesh=rabbit.Skin.sharedMesh;foreach(var w in mesh.boneWeights)
                if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)throw new Exception("Invalid skin weights");
            var baked=new Mesh();var rest=mesh.vertices;float pelvisDrift=0;int checkedShoulderFaces=0;
            var skull=new Bounds(new Vector3(0,3.08f,0),Vector3.zero);
            var torso=new Bounds(new Vector3(0,1.68f,0),Vector3.zero);
            for(int i=0;i<count;i++)
            {
                var point=rest[i];
                if(Mathf.Abs(point.y-3.08f)<.12f)skull.Encapsulate(point);
                if(Mathf.Abs(point.y-1.68f)<.09f&&Mathf.Abs(point.x)<.5f)torso.Encapsulate(point);
            }
            if(skull.size.z<1.8f||skull.size.z<skull.size.x*.95f||skull.size.z<torso.size.z*1.85f)
                throw new Exception($"Rabbit head is too shallow: head={skull.size.z:F3}, width={skull.size.x:F3}, torso={torso.size.z:F3}");
            Debug.Log($"RABBIT HEAD PASS: depth {skull.size.z:F3}, width {skull.size.x:F3}, torso depth {torso.size.z:F3}.");
            float innerRight=float.PositiveInfinity,innerLeft=float.NegativeInfinity;
            for(int i=0;i<count;i++)if(rest[i].y>.3f&&rest[i].y<.5f&&Mathf.Abs(rest[i].z)<.12f)
            {if(rest[i].x>0)innerRight=Mathf.Min(innerRight,rest[i].x);else innerLeft=Mathf.Max(innerLeft,rest[i].x);}
            float legGap=innerRight-innerLeft;
            if(legGap<.12f||legGap>.22f)throw new Exception("Incorrect narrow leg gap: "+legGap);
            int neckSamples=0;float neckWidth=0;
            // The shorter neck sits at shoulder-cap height; exclude those outer caps.
            for(int i=0;i<count;i++)if(rest[i].y>2.32f&&rest[i].y<2.35f&&Mathf.Abs(rest[i].x)<.48f)
            {neckSamples++;neckWidth=Mathf.Max(neckWidth,Mathf.Abs(rest[i].x)*2);}
            if(neckSamples<12||neckWidth<.55f||neckWidth>.9f)throw new Exception("Missing or incorrectly proportioned short neck: "+neckWidth);
            var poses=new[]{0f,Mathf.PI/14,Mathf.PI*3/14,1f,2.5f,3.1f,3.3f,3.6f,3.9f,4.2f,5f};
            foreach(float pose in poses)
            {
                rabbit.PreviewPose(pose);rabbit.Skin.BakeMesh(baked);
                var deformed=baked.vertices;
                var deformedNormals=baked.normals;
                for(int i=0;i<deformed.Length;i++)
                {
                    var p=deformed[i];
                    if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))throw new Exception("Invalid deformed position");
                    if(i<count&&Mathf.Abs(rest[i].x)<.06f&&rest[i].y>1.1f&&rest[i].y<1.4f)
                        pelvisDrift=Mathf.Max(pelvisDrift,Vector3.Distance(rest[i],p));
                }
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var centre=(rest[a]+rest[b]+rest[c])/3;
                    if(centre.x<.4f||centre.x>1f||centre.y<1.9f||centre.y>2.35f)continue;
                    var before=Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]);if(before.magnitude<.00002f)continue;
                    var after=Vector3.Cross(deformed[b]-deformed[a],deformed[c]-deformed[a]);
                    if(after.magnitude<before.magnitude*.05f)throw new Exception("Shoulder triangle collapses during wave at "+pose);
                    if(Vector3.Dot(after.normalized,(deformedNormals[a]+deformedNormals[b]+deformedNormals[c]).normalized)<-.1f)
                        throw new Exception("Shoulder surface folds inside-out at "+pose);
                    checkedShoulderFaces++;
                }
            }
            if(pelvisDrift>.06f)throw new Exception("Pelvis centre pulled by the leg bones: "+pelvisDrift);
            rabbit.PreviewPose(2.5f);rabbit.Skin.BakeMesh(baked);var idle=baked.vertices;
            var armRoot=rabbit.transform.InverseTransformPoint(rabbit.Skin.bones[3].position);
            float armLength=0,sole=float.PositiveInfinity,crotchTop=0;
            var skinWeights=mesh.boneWeights;
            for(int i=0;i<count;i++)
            {
                var w=skinWeights[i];float armWeight=0;
                if(w.boneIndex0==3)armWeight+=w.weight0;if(w.boneIndex1==3)armWeight+=w.weight1;
                if(w.boneIndex2==3)armWeight+=w.weight2;if(w.boneIndex3==3)armWeight+=w.weight3;
                if(armWeight>.8f)armLength=Mathf.Max(armLength,Vector3.Distance(armRoot,idle[i]));
                sole=Mathf.Min(sole,idle[i].y);
                if(Mathf.Abs(rest[i].x)<.1f&&Mathf.Abs(rest[i].z)<.12f&&rest[i].y<1.5f)crotchTop=Mathf.Max(crotchTop,idle[i].y);
            }
            float visibleLegLength=crotchTop-sole;
            const float previousLegLength=1.059f;
            if(Mathf.Abs(visibleLegLength-previousLegLength*.9f)>.005f||Mathf.Abs(armLength-1.057f)>.005f)
                throw new Exception($"Leg must be 10% shorter with unchanged arms: leg={visibleLegLength}, arm={armLength}");
            float idleSocket=rabbit.transform.InverseTransformPoint(rabbit.Skin.bones[3].position).y;
            rabbit.PreviewPose(3.3f);rabbit.Skin.BakeMesh(baked);var waving=baked.vertices;
            float socketRise=rabbit.transform.InverseTransformPoint(rabbit.Skin.bones[3].position).y-idleSocket;
            int shoulderSamples=0;float shoulderRise=0;
            for(int i=0;i<count;i++)if(rest[i].x>.4f&&rest[i].x<.7f&&rest[i].y>2.15f&&rest[i].y<2.28f)
            {shoulderSamples++;shoulderRise+=waving[i].y-idle[i].y;}
            shoulderRise/=Mathf.Max(1,shoulderSamples);
            if(shoulderSamples<12||shoulderRise<.035f||socketRise<.08f)
                throw new Exception($"Shoulder must rise with arm: samples={shoulderSamples}, surface={shoulderRise}, socket={socketRise}");
            // Repeated evaluation must not accumulate the clavicle translation.
            rabbit.PreviewPose(3.3f);
            if(Mathf.Abs(rabbit.transform.InverseTransformPoint(rabbit.Skin.bones[3].position).y-idleSocket-socketRise)>.0001f)
                throw new Exception("Clavicle lift accumulates between animation frames");
            UnityEngine.Object.Destroy(baked);rabbit.PreviewPose(2.5f);
            Debug.Log($"RABBIT MESH PASS: one closed connected skin, {count} skin vertices, {triangles.Length/3} skin triangles, one renderer, normalized weights, {poses.Length} baked poses, pelvis centre drift {pelvisDrift:F4}, visible neck width {neckWidth:F3}, shoulder surface rise {shoulderRise:F3}, arm socket rise {socketRise:F3}; {checkedShoulderFaces} shoulder face checks without inversion/collapse; leg gap {legGap:F3}; visible leg length {visibleLegLength:F3}, arm length {armLength:F3}.");
        }
    }
}
