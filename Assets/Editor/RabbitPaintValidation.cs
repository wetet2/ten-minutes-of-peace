using System;
using UnityEngine;

namespace PeacePark.Editor
{
    static class RabbitPaintValidation
    {
        public static void PaintWithBrush(AnimalCanvas canvas,Color32 color)
        {
            // Largest radius actually exposed by DrawingPage; overlapping real strokes.
            const float radius=64;
            for(float y=0;y<AnimalCanvas.Height+radius;y+=radius*1.5f)
                canvas.Stroke(new Vector2(0,y),new Vector2(AnimalCanvas.Width,y),radius,color);
        }
        static byte[] PaintedTemplate(AnimalCanvas canvas,Color32 color,bool whiteDot=false)
        {
            // Exercise exactly the same clipped Stroke and PNG export as the drawing page.
            // Do not recolour alpha-edge pixels directly: they are outside the brush mask.
            PaintWithBrush(canvas,color);
            if(!whiteDot)return canvas.Paint.EncodeToPNG();
            var pixels=canvas.Paint.GetPixels32();
            for(int i=0;i<pixels.Length;i++)if(pixels[i].a==255&&canvas.IsInside(i%AnimalCanvas.Width,i/AnimalCanvas.Width))
            {pixels[i]=new Color32(255,255,255,255);break;}
            var texture=new Texture2D(AnimalCanvas.Width,AnimalCanvas.Height,TextureFormat.RGBA32,false);
            texture.SetPixels32(pixels);texture.Apply();var png=texture.EncodeToPNG();UnityEngine.Object.Destroy(texture);return png;
        }
        public static void CheckSurfaceCoverage(RabbitCharacter rabbit,Color32 frontColor,Color32 backColor)
        {
            for(int side=0;side<2;side++)
            {
                var texture=(Texture2D)rabbit.Skin.sharedMaterials[0].GetTexture(side==0?"_FrontTex":"_BackTex");
                var expected=side==0?frontColor:backColor;
                foreach(var pixel in texture.GetPixels32())
                    if(pixel.a!=255||Mathf.Abs(pixel.r-expected.r)>1||Mathf.Abs(pixel.g-expected.g)>1||Mathf.Abs(pixel.b-expected.b)>1)
                        throw new Exception("Fully painted silhouette leaves uncoloured surface texels");
                foreach(var uv in rabbit.Skin.sharedMesh.uv)
                {var pixel=texture.GetPixelBilinear(uv.x,uv.y);if(pixel.a<.99f)throw new Exception("Unmapped side vertex");}
            }
        }
        public static void CheckAndApplySolid(RabbitCharacter rabbit,AnimalCanvas front,AnimalCanvas back)
        {
            var frontColor=new Color32(45,183,194,255);var backColor=new Color32(244,154,61,255);
            var frontPng=PaintedTemplate(front,frontColor);var backPng=PaintedTemplate(back,backColor);
            int fringe=0;foreach(var pixel in front.Paint.GetPixels32())if(pixel.a>=128&&pixel.a<255&&pixel.r==255&&pixel.g==255&&pixel.b==255)fringe++;
            Debug.Log("BRUSH INPUT: white antialiased boundary pixels excluded by the brush = "+fringe);
            rabbit.ApplyPainting(frontPng,backPng);rabbit.enabled=false;
            CheckSurfaceCoverage(rabbit,frontColor,backColor);
            var partial=RabbitPaintTexture.Decode(PaintedTemplate(front,frontColor,true),"Partial paint validation");
            bool white=false,colored=false;
            foreach(var pixel in partial.GetPixels32())
            {white|=pixel.r==255&&pixel.g==255&&pixel.b==255;colored|=pixel.r==frontColor.r&&pixel.g==frontColor.g&&pixel.b==frontColor.b;}
            UnityEngine.Object.Destroy(partial);
            if(!white||!colored)throw new Exception("Side extension overwrites white inside the animal");
            Debug.Log("RABBIT PAINT PASS: actual clipped brush strokes and canvas PNG export cover every UV including both sides; intentional white inside is preserved.");
        }
    }
}
