Shader "PeacePark/Christmas Ice"
{
    Properties
    {
        _Color ("Blue ice", Color) = (.53,.66,.81,1)
        _EdgeColor ("Frosted edges", Color) = (.76,.85,.95,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            fixed4 _Color, _EdgeColor;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float2 uv:TEXCOORD0;
                float3 world:TEXCOORD1;
                SHADOW_COORDS(2)
                float depth:TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.depth=-UnityObjectToViewPos(v.vertex.xyz).z;
                TRANSFER_SHADOW(o);
                return o;
            }

            float TraceEight(float2 uv,float2 radii)
            {
                float2 p=(uv-.5)/radii;
                float x2=p.x*p.x;
                float curve=p.y*p.y-4*x2*(1-x2);
                float gradient=length(float2(8*p.x*(2*x2-1),2*p.y));
                float edge=abs(curve)/max(gradient,.14);
                return 1-smoothstep(.002,.010+fwidth(edge),edge);
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float2 xy=i.world.xz;
                float frost=sin(xy.x*.90+sin(xy.y*.66)*1.3)*sin(xy.y*1.13+xy.x*.18);
                float sheen=pow(1-saturate(normalize(_WorldSpaceCameraPos-i.world).y),2.5);
                float row=abs(uv.y-.5)*2;
                float edge=pow(saturate(max(abs(uv.x-.5)*2,row)),5);
                float3 ice=lerp(_Color.rgb,_EdgeColor.rgb,edge*.63+sheen*.29)+frost*.024;
                float bulb=pow(1-abs(frac(uv.x*14-.5)-.5)*2,3);
                float reflection=bulb*pow(row,5)*(.60+.40*sheen);
                float trails=max(TraceEight(uv,float2(.42,.385)),TraceEight(uv,float2(.402,.363))*.48);
                ice*=lerp(.74,1,SHADOW_ATTENUATION(i));
                ice=lerp(ice,float3(.96,.67,.34),reflection*.26);
                fixed4 result=fixed4(ice+trails*.077,1);
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    UNITY_CALC_FOG_FACTOR_RAW(i.depth);
                    UNITY_FOG_LERP_COLOR(result,unity_FogColor,unityFogFactor);
                #endif
                return result;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
