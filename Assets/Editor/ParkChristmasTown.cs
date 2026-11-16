using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal sealed class ParkChristmasTown
    {
        readonly Mesh cube = PrimitiveMesh(PrimitiveType.Cube);
        readonly Mesh cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
        readonly Mesh hippedRoof = HippedRoof();
        readonly Mesh dome = Dome();
        readonly Mesh arcade = ParkGeometry.Arch(1,1.69f,.12f);
        readonly System.Random random = new System.Random(41871);
        readonly Material shopLights = new Material(Shader.Find("Unlit/Color"))
            { name = "Warm shop and palazzo windows", color = new Color32(253,194,114,255) };
        readonly District[] districts =
        {
            new District("Winter palazzi", .02f),
            new District("Snowy terracotta rooftops", .19f),
            new District("Distant city skyline", .34f)
        };

        internal static void Create(Transform parent) => new ParkChristmasTown().Build(parent);

        void Build(Transform parent)
        {
            var city = Group("Winter city around the cathedral", parent, Vector3.zero);
            BuildCourtyard(city);
            float[] depths = { 44, 55, 66, 77, 88, 99, 111, 124, 137, 150, 163 };
            for (int row = 0; row < depths.Length; row++)
            {
                float z = depths[row];
                int layer = row < 3 ? 0 : row < 7 ? 1 : 2;
                var district = districts[layer];
                float spacing = layer == 0 ? 9.4f : layer == 1 ? 11.4f : 13.3f;
                int halfCount = Mathf.CeilToInt((96 + z * .91f) / spacing);
                for (int column = -halfCount; column <= halfCount; column++)
                {
                    float x = (column + (row % 2 == 0 ? .24f : -.21f)) * spacing + Range(-.62f,.62f);
                    float depth = z + Range(-.82f,.82f) + Mathf.Sin(column*.46f + row*.8f)*.88f +
                        Mathf.Sin(x*.039f+row*.61f)*1.7f;
                    float width = spacing * Range(.57f,.79f);
                    float footprint = Range(5.95f,8.35f) + layer * .43f;
                    float y = Range(4.45f,8.85f) + layer * 1.38f;
                    var position = new Vector3(x, 0, depth);
                    var rotation = Quaternion.Euler(0, Range(-6.6f,6.6f) + Mathf.Sin(row*.85f)*4, 0);
                    var pieces = x < 0 ? district.West : district.East;

                    // A few towers and civic domes stand above the blocks along the deeper streets.
                    if ((row == 1 && column == -4) || (row == 3 && column == 5) ||
                        (row == 5 && column == -7) || (row == 8 && column == 8))
                        AddLandmark(pieces, district, position, rotation, width, footprint, y, row % 4 == 1);
                    else
                        AddBuilding(pieces, district, position, rotation, width, footprint, y, layer);
                }
            }

            for (int layer = 0; layer < districts.Length; layer++)
            {
                var district = districts[layer];
                var row = Group(district.Name, city, Vector3.zero);
                BuildSide(district.West, "West", row, layer == 0);
                BuildSide(district.East, "East", row, layer == 0);
            }
            BuildSideStreets(city, -1);
            BuildSideStreets(city, 1);
        }

        void BuildSideStreets(Transform parent, int side)
        {
            var streets = Group(side < 0 ? "West city streets" : "East city streets", parent, Vector3.zero);
            const int bands = 17, bandsPerBatch = 3;
            float streetX = 39.2f;
            for (int firstBand = 0; firstBand < bands; firstBand += bandsPerBatch)
            {
                var pieces = new Pieces();
                for (int band = firstBand; band < Mathf.Min(firstBand + bandsPerBatch, bands); band++)
                {
                    int layer = band < 3 ? 0 : band < 7 ? 1 : 2;
                    var district = districts[layer];
                    float spacing = 9.8f + layer * 1.35f;
                    float streetZ = 30 - (band % 3) * .8f;
                    // The outer streets extend farther around the square so oblique views run into fog.
                    float frontLimit = -64 - (streetX - 39.2f) * .57f;
                    for (int lot = 0; streetZ >= frontLimit; lot++)
                    {
                        float x = side * (streetX + Range(-.57f, .57f));
                        float z = streetZ + Range(-.45f, .45f);
                        float width = spacing * Range(.64f, .80f);
                        float depth = Range(6.2f, 8.1f) + layer * .5f;
                        float height = Range(4.5f, 8.9f) + layer * 1.1f;
                        var point = new Vector3(x, 0, z);
                        var orientation = Quaternion.Euler(0, side * 90 + Range(-5f, 5f), 0);
                        if ((band == 1 && lot == 3) || (band == 4 && lot == (side < 0 ? 6 : 2)))
                            AddLandmark(pieces, district, point, orientation, width, depth, height, side < 0);
                        else
                            AddBuilding(pieces, district, point, orientation, width, depth, height, layer);

                        // Leave cross streets between groups of four buildings.
                        streetZ -= spacing + (lot % 4 == 3 ? 2.7f : 0);
                    }
                    streetX += 11.2f + layer * 1.35f;
                }
                BuildSide(pieces, $"Street blocks {firstBand / bandsPerBatch + 1}", streets, firstBand == 0);
            }
        }

        void BuildCourtyard(Transform parent)
        {
            const float sideFront = 22.35f, sideStart = -18.20f, rearFront = 32.45f;
            const float sideEnd = 31.45f, span = sideEnd-sideStart;
            const int blocks = 7;
            var court = Group("Palazzi surrounding the open square",parent,Vector3.zero);
            var district = districts[0];
            foreach (int side in new[] {-1,1})
            {
                var pieces = new Pieces();
                var rotation = Quaternion.Euler(0,90*side,0);
                for (int i = 0; i < blocks; i++)
                {
                    float width = span/blocks;
                    float depth = Range(6.48f,7.38f);
                    var point = new Vector3(side*(sideFront+depth/2),0,sideStart+(i+.5f)*width);
                    AddPalazzo(pieces,district,point,rotation,width-.05f,depth,i,side>0 ? 1 : 0);
                }
                BuildSide(pieces,side<0 ? "West square frontage" : "East square frontage",court,true);
            }

            var rear = new Pieces();
            const int rearBlocks = 6;
            const float rearWidth = 44f/rearBlocks;
            for (int i = 0; i < rearBlocks; i++)
            {
                float depth = Range(6.45f,7.58f);
                var point = new Vector3((i+.5f-rearBlocks/2f)*rearWidth,0,rearFront+depth/2);
                AddPalazzo(rear,district,point,Quaternion.identity,rearWidth-.05f,depth,i,2);
            }
            BuildSide(rear,"Rear frontage and street corners",court,true);
            BuildCourtyardLanes(court,sideFront,sideStart,rearFront);
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
            for (int bay=0;bay<bays;bay++)
            {
                float x=(bay+.5f-bays/2f)*bayWidth;
                Add(arcade,district.DarkWindow,new Vector3(x,.064f,front-.195f),new Vector3(bayWidth*.79f,1.31f,1));
                Add(arcade,shopLights,new Vector3(x,.17f,front-.274f),new Vector3(bayWidth*.59f,1.09f,.65f));
                Box(district.DarkWindow,new Vector3(x,1.026f,front-.338f),new Vector3(.06f,1.70f,.07f));
                Box(district.DarkWindow,new Vector3(x,1.34f,front-.337f),new Vector3(bayWidth*.59f,.058f,.08f));
                Box(stone,new Vector3(x-bayWidth*.493f,1.24f,front-.23f),new Vector3(.15f,2.48f,.24f));
            }

            int floors=height>7.65f ? 3 : 2;
            float floor=(height-3.11f)/floors;
            for (int level=0;level<floors;level++)
            for (int bay=0;bay<bays;bay++)
            {
                float x=(bay+.5f-bays/2f)*bayWidth;
                float y=3.06f+(level+.49f)*floor;
                float w=bayWidth*.46f,h=floor*.65f;
                Box(district.Cornice,new Vector3(x,y,front-.044f),new Vector3(w+.16f,h+.14f,.16f));
                Box((index+bay+level)%6==0?shopLights:district.DarkWindow,new Vector3(x,y,front-.149f),new Vector3(w,h,.082f));
                Box(tiles,new Vector3(x-w*.31f,y,front-.207f),new Vector3(w*.16f,h,.046f));
                Box(tiles,new Vector3(x+w*.31f,y,front-.207f),new Vector3(w*.16f,h,.046f));
                Box(district.Cornice,new Vector3(x,y-h*.56f,front-.163f),new Vector3(w+.23f,.084f,.27f));
                if(level==0 && (index+bay)%4==0)
                    Box(district.Cornice,new Vector3(x,y-h*.65f,front-.275f),new Vector3(w+.36f,.093f,.45f));
            }

            void Box(Material material,Vector3 at,Vector3 scale) => Add(cube,material,at,scale);
            void Add(Mesh mesh,Material material,Vector3 at,Vector3 scale) => pieces.Add(mesh,material,point+orientation*at,scale,orientation);
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
                if (layer != 0 && floor == 0) continue;
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
                var door = new Vector3(0,floorHeight*.28f,front-.12f);
                AddBox(district.DarkWindow,door,new Vector3(width*.17f,floorHeight*.56f,.12f));
                for (int side = -1; side <= 1; side += 2)
                    AddBox(district.Cornice,new Vector3(side*(width*.5f-.09f),height*.49f,front-.03f),new Vector3(.19f,height*.98f,.20f));
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
            internal readonly string Name;
            internal readonly Pieces West = new Pieces(), East = new Pieces();
            internal readonly Material[] Facades, Tiles;
            internal readonly Material Snow, Cornice, DarkWindow, WarmWindow;

            internal District(string name, float haze)
            {
                Name = name;
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
