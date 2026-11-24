Shader "PeacePark/DeerCloth"
{
    Properties
    {
        _Color("Deer tint", Color) = (1, 1, 1, 1)
        _MainTex("Deer coat", 2D) = "white" {}
        _ClothColor("Riding cloth", Color) = (.72, .025, .035, 1)
        _StitchColor("Cloth stitching", Color) = (.95, .72, .34, 1)
        _Glossiness("Coat smoothness", Range(0, 1)) = .45
        _Metallic("Metallic", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert nolightmap
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color, _ClothColor, _StitchColor;
        half _Glossiness, _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float2 cloth;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.cloth = v.texcoord1.xy;
        }

        void surf(Input i, inout SurfaceOutputStandard o)
        {
            float2 bounds = abs(i.cloth);
            float2 corner = bounds - .94;
            float edge = length(max(corner, 0)) + min(max(corner.x, corner.y), 0) - .06;
            float width = max(.001, .5 * fwidth(edge));
            half cloth = 1 - smoothstep(-width, width, edge);
            half hem = 1 - smoothstep(.015, .05, -edge);

            float2 threadPosition = i.cloth * float2(140, 110);
            half weaveVisibility = saturate(1 - max(fwidth(threadPosition.x), fwidth(threadPosition.y)));
            half weave = sin(threadPosition.x * 6.283185) * sin(threadPosition.y * 6.283185) * .022 * weaveVisibility;
            fixed3 fabric = _ClothColor.rgb * (1 + weave - hem * .18);

            float2 stitchWidth = max(float2(.004, .004), fwidth(i.cloth));
            half sideSeam = 1 - smoothstep(.012, .012 + stitchWidth.x, abs(bounds.x - .89));
            half bottomSeam = 1 - smoothstep(.012, .012 + stitchWidth.y, abs(i.cloth.y + .89));
            half sideDashes = 1 - smoothstep(.30, .39, abs(frac(i.cloth.y * 13) - .5));
            half bottomDashes = 1 - smoothstep(.30, .39, abs(frac(i.cloth.x * 16) - .5));
            half stitching = saturate(sideSeam * sideDashes * (1 - smoothstep(.86, .92, bounds.y)) +
                bottomSeam * bottomDashes * (1 - smoothstep(.86, .92, bounds.x)));
            fabric = lerp(fabric, _StitchColor.rgb, stitching);

            o.Albedo = lerp(tex2D(_MainTex, i.uv_MainTex).rgb * _Color.rgb, fabric, cloth);
            o.Smoothness = lerp(_Glossiness, .13, cloth);
            o.Metallic = _Metallic * (1 - cloth);
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}
