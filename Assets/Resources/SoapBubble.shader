Shader "PeacePark/Soap Bubble"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Vertex
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            Varyings vert(Vertex v)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = v.uv * 2 - 1;
                o.color = v.color;
                return o;
            }

            fixed4 frag(Varyings i) : SV_Target
            {
                float radius = length(i.uv);
                half film = 1 - smoothstep(.89, 1, radius);
                half rim = smoothstep(.66, .92, radius) * film;
                half gleam = 1 - smoothstep(.25, 1, length((i.uv - float2(-.34, .43)) * float2(4, 7)));
                half glint = 1 - smoothstep(.18, 1, length((i.uv - float2(-.59, .22)) * 15));
                half reflection = saturate(gleam + .72 * glint);
                half shimmer = saturate(.5 + .51 * i.uv.x - .44 * i.uv.y);
                fixed3 tint = lerp(fixed3(.65, .94, 1), fixed3(1, .69, .93), shimmer);
                return fixed4(lerp(tint, fixed3(1, 1, 1), reflection) * i.color.rgb,
                    saturate(.10 * film + .70 * rim + .80 * reflection) * i.color.a);
            }
            ENDCG
        }
    }
}
