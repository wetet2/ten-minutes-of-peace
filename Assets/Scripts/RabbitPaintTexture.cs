using UnityEngine;

namespace PeacePark
{
    public static class RabbitPaintTexture
    {
        // Extend the silhouette's nearest boundary colour into transparent texels.
        // Unpainted white INSIDE the animal remains white; this is not a flood fill tool.
        public static Texture2D Decode(byte[] png,string name)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            if(!texture.LoadImage(png,false))
            {texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();return texture;}
            return ExtendEdges(texture);
        }

        public static Texture2D FromRaw(byte[] rgba, string name)
        {
            var texture = new Texture2D(AnimalCanvas.Width, AnimalCanvas.Height, TextureFormat.RGBA32, false)
            {
                name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.LoadRawTextureData(rgba);
            return ExtendEdges(texture);
        }

        static Texture2D ExtendEdges(Texture2D texture)
        {
            int width=texture.width,height=texture.height;
            var pixels=texture.GetPixels32();var queue=new int[pixels.Length];int read=0,write=0;
            for(int i=0;i<pixels.Length;i++)
            {
                // Half-covered outline pixels can be white even after every brush-mask
                // pixel was painted. They must not seed a white halo on the side UVs.
                if(pixels[i].a==255){queue[write++]=i;}
                else pixels[i]=new Color32(0,0,0,0);
            }
            if(write==0)
                for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,255);
            void Visit(int source,int next)
            {if(pixels[next].a!=0)return;pixels[next]=pixels[source];queue[write++]=next;}
            while(read<write)
            {
                int id=queue[read++],x=id%width;
                if(x>0)Visit(id,id-1);if(x<width-1)Visit(id,id+1);
                if(id>=width)Visit(id,id-width);if(id<pixels.Length-width)Visit(id,id+width);
            }
            texture.SetPixels32(pixels);texture.Apply();return texture;
        }
    }
}
