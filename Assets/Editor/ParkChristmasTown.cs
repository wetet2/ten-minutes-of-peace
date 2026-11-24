using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkChristmasTown
    {
        const float FrontShopLine = -32f;
        readonly Mesh cube = PrimitiveMesh(PrimitiveType.Cube);
        readonly Mesh cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Mesh hippedRoof = HippedRoof();
        readonly Mesh dome = Dome();
        readonly Mesh arcade = ParkGeometry.Arch(1,1.69f,.12f);
        readonly Material iron=Paint("Wrought iron","#423D44");
        readonly Material wood=Paint("Walnut front doors","#78503B");
        readonly Material greenDoor=Paint("Painted olive doors","#50675D");
        readonly Material fittings=Paint("Brushed brass handles","#D3A35F");
        readonly System.Random random = new System.Random(41871);
        readonly Material shopLights = new Material(Shader.Find("Unlit/Color"))
            { name = "Warm shop and palazzo windows", color = new Color32(253,194,114,255) };
        readonly District[] districts =
        {
            new District(.02f),
            new District(.19f),
            new District(.34f)
        };

        internal static void Create(Transform parent) => new ParkChristmasTown().Build(parent);

        void Build(Transform parent)
        {
            var city = Group("Winter city around the cathedral", parent, Vector3.zero);
            BuildDistrictGrid(city);
            BuildCourtyard(city);
        }

        const float SquareSide=22.35f, SquareRear=32.45f;
        const float MainStreetX=36f, BlockWidth=25f, BlockDepth=21f;
        const int ColumnsPerSide=9, RowsPerSide=8;

        // The innermost streets are wider apart to make room for the square and its frontage.
        static float StreetX(int i)=>i==0 ? 0 : Mathf.Sign(i)*(MainStreetX+(Mathf.Abs(i)-1)*BlockWidth);

        void BuildDistrictGrid(Transform city)
        {
            var grid=Group("Residential blocks and lanes",city,Vector3.zero);
            var pavement=Paint("Residential stone sidewalks","#D6C4C4");
            var ground=new Pieces();
            float edge=StreetX(ColumnsPerSide),end=RowsPerSide*BlockDepth;
            var cobbles=new Material(Shader.Find("PeacePark/Winter Cobbles")){name="Old town granite setts"};
            Street(-edge,edge,-end,FrontShopLine);
            Street(-edge,edge,SquareRear,end);
            Street(-edge,-SquareSide,FrontShopLine,SquareRear);
            Street(SquareSide,edge,FrontShopLine,SquareRear);
            ground.Build(Group("Connected cobbled street bed",grid,Vector3.zero));

            for(int row=-RowsPerSide;row<RowsPerSide;row++)
            for(int first=-ColumnsPerSide;first<ColumnsPerSide;first+=3)
            {
                var blocks=new Pieces();
                bool nearby=false;
                for(int col=first;col<first+3;col++)
                {
                    if((col==-1||col==0)&&row>=-2&&row<2)continue;
                    float left=StreetX(col),right=StreetX(col+1),bottom=row*BlockDepth;
                    float x=(left+right)/2,z=bottom+BlockDepth/2;
                    float distance=Mathf.Max(Mathf.Abs(x),Mathf.Abs(z));
                    int layer=distance<74 ? 0 : distance<115 ? 1 : 2;
                    nearby|=layer==0;
                    // Each block is one raised island. Neighbours leave half a street on either side.
                    float w=right-left-4.4f,d=BlockDepth-3.8f;
                    blocks.Add(cube,pavement,new Vector3(x,.06f,z),new Vector3(w,.12f,d));
                    bool alongX=(row+col)%3!=0;
                    float span=(alongX?w:d)-2.5f;
                    float depth=((alongX?d:w)-3.2f)/2;
                    int lots=Mathf.Max(2,Mathf.RoundToInt(span/8.4f));
                    for(int side=-1;side<=1;side+=2)
                    for(int lot=0;lot<lots;lot++)
                    {
                        float offset=((lot+.5f)/lots-.5f)*span;
                        var point=alongX?new Vector3(x+offset,0,z+side*depth/2):new Vector3(x+side*depth/2,0,z+offset);
                        var facing=Quaternion.Euler(0,alongX?(side<0?0:180):-side*90,0);
                        float width=span/lots-.38f,height=Range(5.3f,9.2f)+layer*.88f;
                        if(lot==0&&side==1&&Mathf.Abs(row)%5==3&&Mathf.Abs(col)%4==2)
                            AddLandmark(blocks,districts[layer],point,facing,width,depth-.46f,height,col%2==0);
                        else
                            AddBuilding(blocks,districts[layer],point,facing,width,depth-.46f,height,layer);
                    }
                }
                BuildSide(blocks,$"Blocks {first}, {row}",grid,nearby);
            }

            void Street(float left,float right,float bottom,float top)
            {
                ground.Add(cube,cobbles,new Vector3((left+right)/2,.002f,(bottom+top)/2),
                    new Vector3(right-left,.008f,top-bottom));
            }
        }

        void BuildCourtyard(Transform parent)
        {
            var court=Group("Homes facing the cathedral square",parent,Vector3.zero);
            foreach(int side in new[]{-1,1})
            {
                var houses=new Pieces();
                var facing=Quaternion.Euler(0,90*side,0);
                // The openings in both frontages line up with the three east-west lanes.
                float[] ends={-29.8f,-23.1f,-18.9f,-2.1f,2.1f,18.9f,23.1f,29.8f};
                int index=0;
                for(int pair=0;pair<ends.Length;pair+=2)
                {
                    float length=ends[pair+1]-ends[pair];
                    int count=Mathf.RoundToInt(length/8);
                    for(int lot=0;lot<count;lot++,index++)
                    {
                        float depth=Range(6.5f,7.2f),width=length/count;
                        var point=new Vector3(side*(SquareSide+depth/2),0,ends[pair]+(lot+.5f)*width);
                        AddPalazzo(houses,districts[0],point,facing,width-.30f,depth,index,side+1);
                    }
                }
                BuildSide(houses,side<0?"West residences":"East residences",court,true);
            }
            foreach(int side in new[]{-1,1})
            {
                var houses=new Pieces();
                var facing=Quaternion.Euler(0,side<0?180:0,0);
                float line=side<0?FrontShopLine:SquareRear;
                // Leave the foreground open so these facades don't mask the ice from the default camera.
                float innerEdge=side<0?18.1f:2.5f;
                int lotsPerHalf=side<0?2:4;
                for(int half=-1;half<=1;half+=2)
                for(int lot=0;lot<lotsPerHalf;lot++)
                {
                    const float width=7.2f;
                    float depth=Range(6.3f,7.05f);
                    var point=new Vector3(half*(innerEdge+(lot+.5f)*width),0,line+side*depth/2);
                    AddPalazzo(houses,districts[0],point,facing,width-.30f,depth,lot,side+2);
                }
                BuildSide(houses,side<0?"South residences":"North residences",court,true);
            }
            BuildCourtyardLanes(court,SquareSide,-18.2f,SquareRear);
        }

        void AddPalazzo(Pieces pieces, District district, Vector3 point, Quaternion orientation, float width, float depth,
            int index, int row)
        {
            var stone=district.Facades[(index*3+row)%district.Facades.Length];
            var tiles=district.Tiles[(index+row)%district.Tiles.Length];
            float height = 6.45f + .59f*((index*3+row)%5) + Range(-.17f,.22f);
            float front = -depth/2;
            float ridge = Range(1.24f,1.79f);
            Box(stone,new Vector3(0,height/2,0),new Vector3(width,height,depth));
            Box(district.Cornice,new Vector3(0,1.39f,front-.07f),new Vector3(width,2.78f,.20f));
            Box(district.Cornice,new Vector3(0,2.84f,front-.083f),new Vector3(width+.063f,.18f,.32f));
            Box(district.Cornice,new Vector3(0,height-.09f,0),new Vector3(width+.23f,.23f,depth+.27f));
            Add(hippedRoof,tiles,new Vector3(0,height,0),new Vector3(width+.37f,ridge,depth+.43f));
            Add(hippedRoof,district.Snow,new Vector3(0,height+ridge*.24f,0),new Vector3(width*.79f,ridge*.79f,depth*.80f));
            for (int i=0;i<2;i++)
            {
                float x=(i==0?-1:1)*width*.21f;
                float z=(i==0 ? .18f : -.10f)*depth;
                Box(tiles,new Vector3(x,height+.80f,z),new Vector3(.55f,1.58f,.57f));
                Box(district.Snow,new Vector3(x,height+1.61f,z),new Vector3(.68f,.105f,.68f));
            }

            const int bays=4;
            float bayWidth=width/bays;
            AddEntrance(pieces,district,point,orientation,width,front-.18f,index+row);

            int floors=height>7.65f ? 3 : 2;
            float floor=(height-3.11f)/floors;
            // The return walls are exposed at the alley mouths; the rear faces the next street.
            for(int level=0;level<=floors;level++)
            {
                float y=level==0?1.55f:3.06f+(level-.51f)*floor;
                for(int bay=0;bay<3;bay++)
                {
                    float x=(bay-1)*width*.29f;
                    if(level==0 && bay==1)Box(wood,new Vector3(x,1.04f,depth/2+.05f),new Vector3(1.18f,2.08f,.13f));
                    else Box(district.DarkWindow,new Vector3(x,y,depth/2+.05f),new Vector3(.78f,1.06f,.12f));
                }
                for(int side=-1;side<=1;side+=2)
                for(int bay=-1;bay<=1;bay+=2)
                    Box(district.DarkWindow,new Vector3(side*(width*.5f+.026f),y,bay*depth*.25f),new Vector3(.062f,1.04f,.70f));
            }
            for (int level=0;level<floors;level++)
            for (int bay=0;bay<bays;bay++)
            {
                float x=(bay+.5f-bays/2f)*bayWidth;
                float y=3.06f+(level+.49f)*floor;
                float w=bayWidth*.46f,h=floor*.65f;
                Box(district.Cornice,new Vector3(x,y,front-.044f),new Vector3(w+.16f,h+.14f,.16f));
                Box((index+bay+level)%6==0?shopLights:district.DarkWindow,new Vector3(x,y,front-.149f),new Vector3(w,h,.082f));
                    Box(tiles,new Vector3(x-w*.63f,y,front-.14f),new Vector3(w*.20f,h,.06f));
                    Box(tiles,new Vector3(x+w*.63f,y,front-.14f),new Vector3(w*.20f,h,.06f));
                Box(district.Cornice,new Vector3(x,y-h*.56f,front-.163f),new Vector3(w+.23f,.084f,.27f));
                if(level==0 && (index+bay)%4==0)
                    Box(district.Cornice,new Vector3(x,y-h*.65f,front-.275f),new Vector3(w+.36f,.093f,.45f));
            }

            void Box(Material material,Vector3 at,Vector3 scale) => Add(cube,material,at,scale);
            void Add(Mesh mesh,Material material,Vector3 at,Vector3 scale) => pieces.Add(mesh,material,point+orientation*at,scale,orientation);
        }

        void AddEntrance(Pieces pieces,District district,Vector3 point,Quaternion facing,float width,float front,int variant)
        {
            var door=variant%3==0?greenDoor:wood;
            float entryX=(variant%3-1)*width*.105f;
            void Place(Mesh mesh,Material mat,float x,float y,float z,float w,float h,float d)=>
                pieces.Add(mesh,mat,point+facing*new Vector3(x,y,front+z),new Vector3(w,h,d),facing);
            void Box(Material mat,float x,float y,float z,float w,float h,float d)=>Place(cube,mat,x,y,z,w,h,d);

            Place(arcade,district.Cornice,entryX,.09f,-.10f,1.72f,1.47f,1.5f);
            Place(arcade,shopLights,entryX,.14f,-.20f,1.40f,1.32f,.8f);
            Box(door,entryX,1.03f,-.26f,1.43f,1.84f,.10f);
            Box(district.Cornice,entryX,.11f,-.36f,1.77f,.22f,.78f);
            for(int side=-1;side<=1;side+=2)
            {
                float leafX=entryX+side*.34f;
                Box(district.DarkWindow,leafX,1.37f,-.323f,.48f,.73f,.035f);
                Box(door,leafX,1.37f,-.349f,.40f,.64f,.028f);
                Box(door,leafX,.59f,-.324f,.50f,.44f,.05f);
                Box(fittings,entryX+side*.115f,.96f,-.37f,.043f,.18f,.085f);

                float outer=side*(width/2-.25f),inner=entryX+side*1.08f;
                float center=(outer+inner)/2,span=Mathf.Abs(outer-inner);
                float windowWidth=Mathf.Min(1.55f,span*.77f);
                Box(district.DarkWindow,center,1.54f,-.08f,windowWidth+.16f,1.57f,.14f);
                Box((variant+side)%3==0?shopLights:district.WarmWindow,center,1.54f,-.164f,windowWidth,1.38f,.06f);
                Box(district.Cornice,center,.79f,-.21f,windowWidth+.28f,.15f,.42f);
                // Two shallow iron enclosures frame the opening. The stone step stays accessible.
                Box(district.Cornice,center,.115f,-.53f,span+.08f,.23f,1.0f);
                Box(iron,center,.77f,-.99f,span+.08f,.065f,.072f);
                Box(iron,center,.34f,-.99f,span,.040f,.045f);
                int spaces=Mathf.Max(3,Mathf.CeilToInt(span/.25f));
                for(int i=0;i<=spaces;i++)
                {
                    bool post=i==0||i==spaces;
                    float x=Mathf.Lerp(inner,outer,i/(float)spaces);
                    Box(iron,x,post?.56f:.51f,-.99f,post?.083f:.037f,post?.77f:.60f,post?.085f:.04f);
                    if(post)Box(iron,x,.77f,-.55f,.062f,.065f,.87f);
                }
            }
        }

        void BuildCourtyardLanes(Transform parent,float sideFront,float start,float rearFront)
        {
            var root=Group("Cobbled streets around the cathedral and square",parent,Vector3.zero);
            var pieces=new Pieces();
            var bedding=Paint("Muted cobblestone joints", "#A58D9D");
            var stones=new[] { Paint("Lilac grey paving", "#BCA1A9"), Paint("Rose grey paving", "#C8ACAD"),
                Paint("Cool paving stones", "#B3A2AB") };
            const float inner=16.15f;
            Surface(-sideFront,-inner,start,rearFront);
            Surface(inner,sideFront,start,rearFront);
            Surface(-inner,inner,26.36f,rearFront);
            // Connect the existing approach to the arcades without layering tile surfaces.
            Surface(-1.70f,1.70f,FrontShopLine+3,ParkChristmasRinkSetup.FrontEdge-.20f);
            Surface(-sideFront,-inner,FrontShopLine+3,start);
            Surface(inner,sideFront,FrontShopLine+3,start);
            Surface(-sideFront,sideFront,FrontShopLine,FrontShopLine+3);

            pieces.Build(root);

            void Surface(float left,float right,float front,float back)
            {
                float width=right-left,length=back-front;
                pieces.Add(cube,bedding,new Vector3((left+right)/2,.005f,(front+back)/2),new Vector3(width,.013f,length));
                int columns=Mathf.CeilToInt(width/1.05f),rows=Mathf.CeilToInt(length/.86f);
                float w=width/columns,d=length/rows;
                for (int row=0;row<rows;row++)
                for (int column=0;column<columns;column++)
                {
                    float x=left+(column+.5f)*w,z=front+(row+.5f)*d;
                    pieces.Add(cube,stones[(row*11+column*7+row%5)%stones.Length],new Vector3(x,.014f,z),
                        new Vector3(w-.044f,.010f,d-.037f));
                }
            }
        }

        static void BuildSide(Pieces pieces, string name, Transform parent, bool castShadow)
        {
            var side = Group(name, parent, Vector3.zero);
            pieces.Build(side);
            if (castShadow) return;
            foreach (var renderer in side.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        void AddBuilding(Pieces pieces, District district, Vector3 position, Quaternion yaw, float width, float depth,
            float height, int layer)
        {
            var stucco = district.Facades[random.Next(district.Facades.Length)];
            var tile = district.Tiles[random.Next(district.Tiles.Length)];
            float roofHeight = Range(.75f,1.60f) + layer*.24f;
            bool flat = random.Next(5) == 0;
            AddBox(stucco, new Vector3(0,height/2,0), new Vector3(width,height,depth));
            AddBox(district.Cornice,new Vector3(0,height-.13f,0),new Vector3(width+.24f,.26f,depth+.23f));
            if (flat)
            {
                AddBox(district.Snow,new Vector3(0,height+.06f,0),new Vector3(width+.31f,.18f,depth+.31f));
                AddBox(stucco,new Vector3(width*.25f,height+.38f,depth*.15f),new Vector3(width*.28f,.72f,depth*.32f));
            }
            else
            {
                Add(hippedRoof,tile,new Vector3(0,height,0),new Vector3(width+.55f,roofHeight,depth+.55f));
                Add(hippedRoof,district.Snow,new Vector3(0,height+roofHeight*.24f,0),
                    new Vector3(width*.77f,roofHeight*.79f,depth*.77f));
            }
            if (layer != 2 || random.Next(4) == 0)
            {
                float chimney = height+roofHeight*.40f;
                AddBox(tile,new Vector3(width*.26f,chimney,depth*.15f),new Vector3(.56f,roofHeight+.52f,.66f));
                AddBox(district.Snow,new Vector3(width*.26f,chimney+(roofHeight+.52f)*.50f,depth*.15f),new Vector3(.69f,.103f,.82f));
            }
            if (layer == 2) return;

            int floors = Mathf.Clamp(Mathf.RoundToInt(height/2.35f),2,4);
            int windows = Mathf.Clamp(Mathf.RoundToInt(width/1.46f),3,6);
            float floorHeight = height/floors;
            float front = -depth/2;
            for (int floor = 0; floor < floors; floor++)
            {
                if (floor == 0) continue;
                float top = (floor+.57f)*floorHeight;
                for (int window = 0; window < windows; window++)
                {
                    float x = ((window+.5f)/windows-.5f)*width*.91f;
                    var glass = random.Next(5) == 0 ? district.WarmWindow : district.DarkWindow;
                    float w = width/windows*.48f;
                    if (layer == 0)
                        AddBox(district.Cornice,new Vector3(x,top,front-.044f),new Vector3(w+.17f,floorHeight*.52f+.13f,.120f));
                    AddBox(glass,new Vector3(x,top,front-(layer == 0 ? .12f : .034f)),new Vector3(w,floorHeight*.52f,.07f));
                }
                if (layer == 0 && floor > 0)
                {
                    AddBox(district.Cornice,new Vector3(0,floor*floorHeight-.085f,front-.043f),new Vector3(width,.10f,.14f));
                    for (int side = -1; side <= 1; side += 2)
                    for (int window = -1; window <= 1; window++)
                        AddBox(district.DarkWindow,new Vector3(side*(width*.5f+.029f),top,window*depth*.27f),
                            new Vector3(.07f,floorHeight*.49f,.61f));
                }
            }
            if (layer == 0)
            {
                AddEntrance(pieces,district,position,yaw,width,front,random.Next(6));
                for (int side = -1; side <= 1; side += 2)
                    AddBox(district.Cornice,new Vector3(side*(width*.5f-.09f),height*.49f,front-.03f),new Vector3(.19f,height*.98f,.20f));
            }
            else
            {
                AddBox(wood,new Vector3(0,1.06f,front-.07f),new Vector3(1.24f,2.12f,.14f));
            }

            void AddBox(Material material, Vector3 center, Vector3 scale) => Add(cube,material,center,scale);
            void Add(Mesh mesh, Material material, Vector3 center, Vector3 scale) =>
                pieces.Add(mesh,material,position+yaw*center,scale,yaw);
        }

        void AddLandmark(Pieces pieces, District district, Vector3 position, Quaternion yaw, float width, float depth,
            float height, bool domed)
        {
            AddBuilding(pieces, district, position, yaw, width, depth, height, 0);
            float span = width * (domed ? .65f : .38f);
            float drum = domed ? 2.48f : 5.75f;
            var side = domed ? district.Cornice : district.Facades[2];
            var shape = domed ? cylinder : cube;
            var center = position + Vector3.up * (height + drum/2);
            pieces.Add(shape, side, center, new Vector3(span, domed ? drum/2 : drum, span));
            pieces.Add(shape, district.Cornice, position + Vector3.up*(height+drum),
                new Vector3(span+.22f,domed ? .16f : .31f,span+.22f));
            float front = position.z-span*.50f-.04f;
            for (int i = -1; i <= 1; i++)
                pieces.Add(cube,district.DarkWindow,new Vector3(position.x+i*span*.21f,height+drum*.69f,front),
                    new Vector3(span*.098f,drum*.38f,.09f));
            var capPosition = position + Vector3.up*(height+drum+.15f);
            pieces.Add(domed ? dome : hippedRoof, district.Tiles[0], capPosition,
                new Vector3(span+.55f,span*(domed ? .64f : .50f),span+.55f));
            pieces.Add(domed ? dome : hippedRoof, district.Snow,capPosition+Vector3.up*(span*.27f),
                new Vector3(span*.68f,span*(domed ? .40f : .25f),span*.68f));
            pieces.Add(cube,district.Cornice,capPosition+Vector3.up*(span*(domed ? .71f : .56f)),new Vector3(.18f,span*.26f,.18f));
        }

        float Range(float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());

        sealed class District
        {


            internal readonly Material[] Facades, Tiles;
            internal readonly Material Snow, Cornice, DarkWindow, WarmWindow;

            internal District(float haze)
            {

                Facades = new[] { PaintTown("Pale stone", "#DEBEA6"), PaintTown("Biscuit plaster", "#BD9E87"),
                    PaintTown("Faded rose stucco", "#BF8A82"), PaintTown("Muted gold plaster", "#CBA980"),
                    PaintTown("Dove-grey plaster", "#B4ACB6") };
                Tiles = new[] { PaintTown("Aged terracotta", "#975E54"), PaintTown("Dusty brick", "#A3706D"),
                    PaintTown("Cool clay tiles", "#806A77") };
                Snow = PaintTown("Soft rooftop snow", "#E7DEF5");
                Cornice = PaintTown("Carved cornice", "#CBB6B0");
                DarkWindow = PaintTown("Dim windows", "#705A6A");
                WarmWindow = PaintTown("Warm distant windows", "#DDB184");

                Material PaintTown(string label,string hex)
                {
                    var material = Paint(label,hex);
                    material.color = Color.Lerp(material.color,new Color32(139,131,174,255),haze);
                    material.SetColor("_Shade", new Color(.78f,.73f,.83f));
                    return material;
                }
            }
        }

        static Mesh HippedRoof()
        {
            var a = new Vector3(-.5f,0,-.5f);
            var b = new Vector3(-.5f,0,.5f);
            var c = new Vector3(.5f,0,.5f);
            var d = new Vector3(.5f,0,-.5f);
            var left = new Vector3(-.31f,1,0);
            var right = new Vector3(.31f,1,0);
            var mesh = new Mesh { name = "Low hipped terracotta roof",
                vertices = new[] { a,b,left, b,c,right,left, c,d,right, d,a,left,right },
                triangles = new[] { 0,1,2, 3,4,5,3,5,6, 7,8,9, 10,11,12,10,12,13 } };
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh Dome()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 12, rings = 5;
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings-1) * Mathf.PI/2;
                for (int side = 0; side < sides; side++)
                {
                    float a = side * 2 * Mathf.PI / sides;
                    float radius = Mathf.Cos(t)*.50f;
                    vertices.Add(new Vector3(Mathf.Cos(a)*radius, Mathf.Sin(t), Mathf.Sin(a)*radius));
                    if (i == rings-1) continue;
                    int index = i*sides+side, next = i*sides+(side+1)%sides;
                    triangles.AddRange(new[] { index,index+sides,next,next,index+sides,next+sides });
                }
            }
            var mesh = new Mesh { name = "Layered civic dome",vertices = vertices.ToArray(),triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
