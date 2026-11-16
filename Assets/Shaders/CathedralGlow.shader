Shader "PeacePark/Cathedral Glow"
{
    Properties
    {
        _Color("Glow tint", Color) = (1,.53,.16,1)
        _Strength("Glow strength", Range(0,1)) = .25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Strength;
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(AppData v)
            {
                Varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2 - 1;
                return o;
            }

            fixed4 frag(Varyings i) : SV_Target
            {
                half falloff = saturate(1 - dot(i.uv, i.uv));
                return fixed4(_Color.rgb, falloff * falloff * falloff * _Strength);
            }
            ENDCG
        }
    }
}
