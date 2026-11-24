Shader "PeacePark/Resort Water"
{
    Properties
    {
        _Tint("Clear turquoise", Color) = (.075,.58,.67,.44)
        _Current("Flow on the river", Float) = 0
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
            fixed4 _Tint;
            half _Current;
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float2 uv : TEXCOORD1; UNITY_FOG_COORDS(2) };

            Varyings vert(AppData v)
            {
                Varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            fixed4 frag(Varyings i) : SV_Target
            {
                float2 p = i.world.xz * 1.20;
                float t = _Time.y;
                float2 warp = p + float2(sin(p.y * 1.35 + t * .75), cos(p.x * 1.22 - t * .8)) * .59;
                float2 cells = abs(sin(warp * float2(2.55,2.93) + float2(t * .35,-t * .22)));
                half caustics = (1-smoothstep(.035,.24,min(cells.x,cells.y))) * (.64+.36*cos(warp.x+warp.y));
                half reflection = 1-saturate(normalize(_WorldSpaceCameraPos-i.world).y);
                half flow = _Current * smoothstep(.84,1,sin(i.uv.x*113.0973-t*2.2+sin(i.uv.y*9)*.40));
                fixed4 color = _Tint;
                color.rgb = lerp(color.rgb,fixed3(.82,1,.96),caustics*.60+reflection*reflection*.28+flow*.15);
                color.a += caustics*.14 + reflection*.13;
                UNITY_APPLY_FOG(i.fogCoord,color);
                return color;
            }
            ENDCG
        }
    }
}
