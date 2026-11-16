using System.Collections.Generic;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed partial class ParkChristmasCathedral
    {
        readonly Material[] slates = {
            Stone("Blue slate shingles", "#5E6689", "#58617D"),
            Stone("Deep violet slate", "#4C536E", "#464E69"),
            Stone("Weathered slate shingles", "#6F7291", "#626780")
        };

        void BuildLongRoof(Transform parent)
        {
            var roof = Group("Slate nave roof, carved ridge and bell tower", parent, Vector3.zero);
            var p = new Pieces();
            const float start = -6.31f, end = ApseStart, span = end-start;
            const float rise = RoofRidge-RoofEave, width = UpperHalfWidth+.18f;
            const int courses = 17, shingles = 24;
            float slopeLength = Mathf.Sqrt(width*width + rise*rise);
            float pitch = Mathf.Atan2(rise,width)*Mathf.Rad2Deg;
            var frontGable = ArchSolid("Stone gable beneath the front roof",new List<Vector2> {
                new Vector2(-width,0),new Vector2(width,0),new Vector2(0,rise) },.48f);
            p.Add(frontGable,wall,new Vector3(0,RoofEave,start+.23f),Vector3.one);
            p.Add(slab,wall,new Vector3(0,(TerraceHeight+RoofEave)/2,start+.28f),
                new Vector3(UpperHalfWidth*2,RoofEave-TerraceHeight,.51f));
            for (int side = -1; side <= 1; side += 2)
            {
                var rotation = Quaternion.Euler(0,0,-side*pitch);
                var normal = new Vector3(side*rise,width,0).normalized;
                p.Add(slab,slates[1],Surface(.5f,(start+end)/2) - normal*.12f,new Vector3(slopeLength,.23f,span),rotation);
                for (int course = 0; course < courses; course++)
                for (int shingle = 0; shingle < shingles; shingle++)
                {
                    float shift = course%2==0 ? -.23f : .23f;
                    float z = Mathf.Lerp(start,end,(shingle+.5f+shift)/shingles);
                    float tileDepth = shingle==0 || shingle==shingles-1 ? span/shingles*.55f : span/shingles*.946f;
                    var color = slates[(course*13+shingle*7+(course+shingle)%5)%slates.Length];
                    p.Add(slab,color,Surface((course+.5f)/courses,z)+normal*.024f,
                        new Vector3(slopeLength/courses*.955f,.064f,tileDepth),rotation);
                }

                AddDrift(.96f,1,.092f, side == -1 ? 0 : 1.3f);
                AddDrift(.025f,.10f,.086f, .78f*side);
                for (int i = 1; i <= 5; i++)
                {
                    float z = Mathf.Lerp(start,end,i/6f);
                    Member(p,ivory,Surface(.04f,z)+normal*.079f,Surface(.98f,z)+normal*.079f,.082f,.105f);
                    AddPinnacle(p,new Vector3(side*width,RoofEave+.06f,z),1.2f,.26f);
                }
                p.Add(slab,rose,Surface(1,(start+end)*.5f)+Vector3.down*.123f,new Vector3(.28f,.22f,span));
                p.Add(slab,ivory,Surface(1,(start+end)*.5f)+Vector3.down*.031f,new Vector3(.33f,.126f,span+.08f));

                Vector3 Surface(float x,float z) => new Vector3(side*width*x,RoofRidge-rise*x,z);

                void AddDrift(float low,float high,float lift,float seed)
                {
                    const int count = 36;
                    var verts = new Vector3[(count+1)*2];
                    var triangles = new int[count*6];
                    for (int i = 0; i <= count; i++)
                    {
                        float t = i/(float)count;
                        float z = Mathf.Lerp(start+.075f,end-.045f,t);
                        float scallop = .061f*(.5f+.5f*Mathf.Sin(t*21f+seed))*Mathf.Abs(Mathf.Sin(t*46f+seed));
                        float inner = low<.2f ? low : low-scallop;
                        float outer = high<.2f ? high+scallop : high;
                        verts[i*2] = Surface(inner,z)+normal*lift;
                        verts[i*2+1] = Surface(outer,z)+normal*lift;
                        if (i == count) continue;
                        int a=i*2,k=i*6;
                        triangles[k]=a; triangles[k+1]=a+(side>0 ? 2 : 1); triangles[k+2]=a+(side>0 ? 1 : 2);
                        triangles[k+3]=a+1; triangles[k+4]=a+(side>0 ? 2 : 3); triangles[k+5]=a+(side>0 ? 3 : 2);
                    }
                    p.Add(RoofMesh("Uneven snow along the slate roof",verts,triangles),snow,Vector3.zero,Vector3.one);
                }
            }

            p.Add(slab,rose,new Vector3(0,RoofRidge+.045f,(start+end)/2),new Vector3(.22f,.21f,span+.10f));
            p.Add(slab,ivory,new Vector3(0,RoofRidge+.151f,(start+end)/2),new Vector3(.29f,.10f,span+.18f));
            for (int i = 0; i <= 21; i++)
            {
                float z = Mathf.Lerp(start+.17f,end-.13f,i/21f);
                p.Add(bead,ivory,new Vector3(0,RoofRidge+.20f,z),new Vector3(.10f,.43f,.14f));
            }
            p.Build(roof);
            BuildRoofBellTower(roof,new Vector3(0,RoofRidge-.42f,3.65f));
        }

        void AddFlyingButtress(Pieces p, float x, float terraceWidth)
        {
            var outside = new Vector3(x,TerraceHeight+.45f,-.045f);
            var inside = new Vector3(x,RoofEave-.52f,terraceWidth+.02f);
            const int segments = 11;
            for (int i = 0; i < segments; i++)
            {
                var start = Point(i/(float)segments);
                var end = Point((i+1)/(float)segments);
                Member(p,rose,start,end,.275f,.33f);
                Member(p,ivory,start+Vector3.up*.175f,end+Vector3.up*.175f,.125f,.40f);
            }

            Vector3 Point(float t) => Vector3.Lerp(outside,inside,t)+Vector3.up*(.91f*Mathf.Sin(t*Mathf.PI));
        }

        void BuildApse(Transform parent)
        {
            const int panels = 8;
            const float upperBack = ApseBack-.74f;
            var apse = Group("Faceted rear apse and wraparound stone gallery",parent,Vector3.zero);
            var perimeter = new Pieces();
            for (int i = 0; i < panels; i++)
            {
                float a = i*Mathf.PI/panels, b = (i+1)*Mathf.PI/panels;
                var lowA = Point(a,AisleHalfWidth,ApseBack,TerraceHeight);
                var lowB = Point(b,AisleHalfWidth,ApseBack,TerraceHeight);
                var highA = Point(a,UpperHalfWidth,upperBack,TerraceHeight);
                var highB = Point(b,UpperHalfWidth,upperBack,TerraceHeight);
                BuildPanel(lowA,lowB,0,TerraceHeight,true);
                BuildPanel(highA,highB,TerraceHeight,RoofEave,false);
                perimeter.Add(RoofMesh("Stone gallery around apse",new[] {lowA,highA,highB,lowB},new[]{0,1,2,0,2,3}),
                    snow,Vector3.up*.055f,Vector3.one);

                var eaveA = highA + Vector3.up*(RoofEave-TerraceHeight);
                var eaveB = highB + Vector3.up*(RoofEave-TerraceHeight);
                var ridge = new Vector3(0,RoofRidge,ApseStart);
                var facet = RoofMesh("Slate facets over the apse",new[] {eaveA,ridge,eaveB},new[]{0,1,2});
                perimeter.Add(facet,slates[i%slates.Length],Vector3.zero,Vector3.one);
                Member(perimeter,ivory,eaveA+Vector3.up*.07f,ridge+Vector3.up*.07f,.088f,.093f);
                Member(perimeter,rose,eaveA,eaveB,.16f,.23f);
                if (i > 0) AddPinnacle(perimeter,eaveA-Vector3.up*.04f,1.31f,.32f);
            }
            perimeter.Build(apse);

            Vector3 Point(float angle,float width,float rear,float height) =>
                new Vector3(Mathf.Cos(angle)*width,height,ApseStart+Mathf.Sin(angle)*(rear-ApseStart));

            void BuildPanel(Vector3 a,Vector3 b,float bottom,float top,bool gallery)
            {
                var center = (a+b)*.5f;
                var tangent = (b-a).normalized;
                var outward = Vector3.Cross(Vector3.up,tangent);
                var panel = Group(gallery ? "Lower apse bay" : "Clerestory apse bay",apse,new Vector3(center.x,bottom,center.z));
                panel.localRotation=Quaternion.LookRotation(-outward);
                var pieces=new Pieces();
                float width=Vector3.Distance(a,b);
                float height=top-bottom;
                pieces.Add(slab,wall,new Vector3(0,height*.5f,.29f),new Vector3(width+.12f,height,.63f));
                Moulding(pieces,0,height-.31f,width,0);
                AddWindow(panel,pieces,0,gallery ? 2.04f : .46f,Mathf.Min(1.85f,width*.59f),gallery ? 5.4f : 3.16f,0);
                AddPier(pieces,width*.5f,0,height,-.14f,gallery ? .49f : .33f);
                if (gallery)
                {
                    AddBalustrade(pieces,0,height+.11f,width,-.19f);
                    AddPinnacle(pieces,new Vector3(width*.5f,height+.14f,-.12f),2.23f,.49f);
                }
                pieces.Build(panel);
            }
        }

        void BuildRoofBellTower(Transform parent, Vector3 center)
        {
            var tower=Group("Open belfry above the nave",parent,center);
            var p=new Pieces();
            p.Add(rounded,rose,new Vector3(0,.41f,0),new Vector3(3.06f,.85f,3.01f));
            p.Add(rounded,ivory,new Vector3(0,.82f,0),new Vector3(3.14f,.21f,3.11f));
            p.Add(slab,ivory,new Vector3(0,2.74f,0),new Vector3(3.01f,.28f,2.98f));
            foreach (int side in new[]{-1,1})
            {
                AddPier(p,side*1.13f,.83f,1.89f,-1.04f,.38f);
                AddPier(p,side*1.13f,.83f,1.89f,1.04f,.38f);
            }
            for (int i=0; i<4; i++)
            {
                var face=Group("Open lancet in the belfry",tower,Vector3.zero);
                face.localRotation=Quaternion.Euler(0,i*90,0);
                var pieces=new Pieces();
                Rim(pieces,Arch(.94f,.94f,1.73f,2.63f),Arch(1.07f,.89f,1.76f,2.76f),ivory,-1.11f,.25f);
                pieces.Build(face);
            }
            p.Add(spire,slates[1],new Vector3(0,2.80f,0),new Vector3(3.0f,2.19f,3.0f));
            p.Add(collar,snow,new Vector3(0,2.79f,0),new Vector3(3.16f,.13f,3.16f));
            p.Add(bead,brass,new Vector3(0,4.94f,0),new Vector3(.24f,.35f,.24f));
            var bell = SpireMesh("Small brass roof bell",new Vector2(.58f,0),new Vector2(.61f,.075f),
                new Vector2(.43f,.22f),new Vector2(.31f,.57f),new Vector2(.27f,.84f),new Vector2(.06f,1));
            p.Add(bell,brass,new Vector3(0,1.37f,0),new Vector3(1.08f,1.14f,1.08f));
            p.Add(bead,iron,new Vector3(0,1.20f,0),new Vector3(.19f,.24f,.19f));
            p.Build(tower);
        }

        static Mesh RoofMesh(string name,Vector3[] vertices,int[] triangles)
        {
            var mesh=new Mesh {name=name,vertices=vertices,triangles=triangles};
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
