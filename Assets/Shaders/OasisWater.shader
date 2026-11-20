Shader "PeacePark/Oasis Water"
{
    Properties
    {
        _Shallow("Shallow tint", Color) = (.38,.78,.74,.41)
        _Deep("Deep tint", Color) = (.04,.6,.69,.68)
    }
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Shallow, _Deep;
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            Varyings vert(AppData v)
            {
                Varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(Varyings i) : SV_Target
            {
                half depth = 1 - smoothstep(.28,1.12,length(i.uv));
                fixed4 color = lerp(_Shallow,_Deep,depth);
                float2 p = i.world.xz;
                float time = _Time.y;
                half wave = sin(dot(p,float2(.85,3.45)) + sin(p.x * 1.25 + time * .55) - time * 1.2);
                half swell = sin(dot(p,float2(-1.45,2.8)) - time * .85);
                half ripple = smoothstep(.57,.96,wave * .63 + swell * .37);
                half reflection = 1 - saturate(normalize(_WorldSpaceCameraPos - i.world).y);
                color.rgb = lerp(color.rgb, fixed3(.8,.96,.93), ripple * .20 + reflection * reflection * .11);
                UNITY_APPLY_FOG(i.fogCoord,color);
                return color;
            }
            ENDCG
        }
    }
}
