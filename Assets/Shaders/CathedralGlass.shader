Shader "PeacePark/Cathedral Glass"
{
    Properties
    {
        _Color("Glass light", Color) = (1,.83,.42,1)
        _EdgeColor("Glass edge", Color) = (.78,.30,.09,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color, _EdgeColor;
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(AppData v)
            {
                Varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(Varyings i) : SV_Target
            {
                half2 q = i.uv - half2(.50,.47);
                half light = saturate(1 - length(q * half2(1.32,.98)));
                half2 diamond = abs(frac(half2(i.uv.x * 2.5 + i.uv.y * 8, i.uv.x * 2.5 - i.uv.y * 8)) - .5);
                half leading = smoothstep(.445,.50,max(diamond.x,diamond.y));
                return fixed4(lerp(_EdgeColor.rgb,_Color.rgb,light) * lerp(1,.89,leading),1);
            }
            ENDCG
        }
    }
}
