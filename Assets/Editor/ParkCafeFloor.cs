using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkCafeFloor
    {
        const string TilePath = ParkNaturePack.PandazoleFolder + "Prefabs/TileHex_07.prefab";
        const string FloorName = "Rectangular play floor";

        internal static void Create(Transform root,float width,float depth,Material fallback)
        {
            var tile=AssetDatabase.LoadAssetAtPath<GameObject>(TilePath);
            if(tile&&tile.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh)
            {
                var deck=FitTiles(filter.sharedMesh,width,depth);
                if(deck.vertexCount>0)
                {
                    var surface=Group(FloorName,root,Vector3.zero);
                    var groove=Paint("Cafe oak grooves","#CAA582");
                    var boards=Paint("Cafe honey oak boards","#D9B491");
                    groove.SetFloat("_Variation",.022f);
                    boards.SetFloat("_Variation",.031f);
                    boards.SetFloat("_GrassDetail",.04f);
                    surface.gameObject.AddComponent<MeshFilter>().sharedMesh=deck;
                    surface.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{groove,boards};
                    Debug.Log("CAFE FLOOR: Pandazole TileHex_07 fitted to the terrace.");
                    return;
                }
            }
            ParkGeometry.Place(FloorName,PrimitiveMesh(PrimitiveType.Cube),root,
                new Vector3(0,-.115f,0),new Vector3(width,.23f,depth),fallback);
        }

        static Mesh FitTiles(Mesh tile,float width,float depth)
        {
            using var snapshot=MeshUtility.AcquireReadOnlyMeshData(tile);
            var data=snapshot[0];
            using var positions=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp);
            using var sourceIndices=new NativeArray<int>(data.GetSubMesh(0).indexCount,Allocator.Temp);
            data.GetVertices(positions);
            data.GetIndices(sourceIndices,0);
            var bounds=tile.bounds;
            const float scale=.78f;
            float tileWidth=bounds.size.x*scale,tileDepth=bounds.size.z*scale,step=tileDepth*.75f;
            float minimumTop=bounds.max.y-bounds.size.y*.07f;
            float raisedTop=bounds.max.y-bounds.size.y*.012f;
            var topIndices=new List<int>();
            for(int i=0;i<sourceIndices.Length;i+=3)
            {
                int a=sourceIndices[i],b=sourceIndices[i+1],c=sourceIndices[i+2];
                if(positions[a].y>=minimumTop&&positions[b].y>=minimumTop&&positions[c].y>=minimumTop)
                    topIndices.AddRange(new[]{a,b,c});
            }
            var vertices=new List<Vector3>();
            var baseTriangles=new List<int>();
            var boardTriangles=new List<int>();
            var polygon=new List<Vector3>(8);
            var clipped=new List<Vector3>(8);
            int rows=Mathf.CeilToInt((depth+tileDepth)/(2*step));
            int columns=Mathf.CeilToInt(width/(2*tileWidth))+1;
            for(int row=-rows;row<=rows;row++)
            for(int column=-columns;column<=columns;column++)
            {
                float x=(column+(row&1)*.5f)*tileWidth,z=row*step;
                if(Mathf.Abs(x)>(width+tileWidth)/2||Mathf.Abs(z)>(depth+tileDepth)/2)continue;
                for(int i=0;i<topIndices.Count;i+=3)
                {
                    polygon.Clear();
                    bool board=true;
                    for(int corner=0;corner<3;corner++)
                    {
                        int index=topIndices[i+corner];
                        var point=positions[index]-bounds.center;
                        polygon.Add(new Vector3(point.x*scale+x,(positions[index].y-bounds.max.y)*.55f,point.z*scale+z));
                        board&=positions[index].y>=raisedTop;
                    }
                    Clip(polygon,clipped,Vector3.right,width/2);
                    Clip(clipped,polygon,Vector3.left,width/2);
                    Clip(polygon,clipped,Vector3.forward,depth/2);
                    Clip(clipped,polygon,Vector3.back,depth/2);
                    if(polygon.Count<3)continue;
                    int start=vertices.Count;
                    vertices.AddRange(polygon);
                    var triangles=board?boardTriangles:baseTriangles;
                    for(int j=1;j<polygon.Count-1;j++)
                        triangles.AddRange(new[]{start,start+j,start+j+1});
                }
            }
            var mesh=new Mesh{name="Pandazole terrace deck"};
            if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.subMeshCount=2;
            mesh.SetTriangles(baseTriangles,0);
            mesh.SetTriangles(boardTriangles,1);
            mesh.RecalculateNormals();
            return mesh;
        }

        // Clip the wooden tile geometry to the rectangular opening in the meadow.
        static void Clip(List<Vector3> input,List<Vector3> output,Vector3 direction,float limit)
        {
            output.Clear();
            for(int i=0;i<input.Count;i++)
            {
                var from=input[(i+input.Count-1)%input.Count];
                var to=input[i];
                float a=limit-Vector3.Dot(direction,from),b=limit-Vector3.Dot(direction,to);
                if((a>=0)!=(b>=0))
                {
                    float t=a/(a-b);
                    output.Add(Vector3.LerpUnclamped(from,to,t));
                }
                if(b>=0)output.Add(to);
            }
        }
    }
}
