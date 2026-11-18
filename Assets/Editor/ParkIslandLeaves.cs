using UnityEditor;
using UnityEngine;
using static PeacePark.Editor.ParkScenery;

namespace PeacePark.Editor
{
    internal static class ParkIslandLeaves
    {
        const string TexturePath="Assets/Textures/Island/RainforestBough.png";
        internal static Material Material(string name,Color tint)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if(!texture)throw new System.IO.FileNotFoundException("Island foliage texture missing",TexturePath);
            return new Material(Shader.Find("PeacePark/Island Leaves")){name=name,mainTexture=texture,color=tint};
        }
        internal static Mesh Bough()
        {
            // Three intersecting, slightly curved leaf cards form a volume around every branch tip.
            var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
            for(int plane=0;plane<3;plane++)
            {
                var rotation=Quaternion.Euler(plane==0?25:plane==1?65:105,plane*61,plane*17);
                int first=vertices.Count;
                for(int row=0;row<3;row++)
                for(int col=0;col<3;col++)
                {
                    float u=col*.5f,v=row*.5f;
                    vertices.Add(rotation*new Vector3((u-.5f)*2,(v-.5f)*1.5f,.20f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI)));
                    uv.Add(new Vector2(u,v));
                    if(row==2||col==2)continue;
                    int p=first+row*3+col;triangles.AddRange(new[]{p,p+3,p+1,p+1,p+3,p+4});
                }
            }
            var mesh=new Mesh{name="Volumetric tropical leaf bough"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
    }

    internal sealed class ParkIslandLeafImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            bool isLeaf=assetPath=="Assets/Textures/Island/RainforestBough.png";
            if(!isLeaf&&assetPath!="Assets/Textures/Island/CliffRock.png")return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=isLeaf;
            importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=isLeaf;importer.alphaTestReferenceValue=.45f;
            importer.maxTextureSize=1024;importer.wrapMode=isLeaf?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;
            importer.textureCompression=TextureImporterCompression.Compressed;importer.compressionQuality=80;
        }
    }
}
