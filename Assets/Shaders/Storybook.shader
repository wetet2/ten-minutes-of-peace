Shader "PeacePark/Storybook"
{
    Properties
    {
        _Color("Paint color",Color)=(1,1,1,1)
        _Shade("Shade tint",Color)=(.62,.76,.69,1)
        _Variation("World variation",Range(0,.1))=0
        _GrassDetail("Grass detail",Range(0,.3))=0
        [HideInInspector] _AnimalVisibility("Animal visibility",Range(0,1))=1
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_local_fragment _ PEACE_PARK_ANIMAL_FADING
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "AnimalFade.cginc"
            fixed4 _Color,_Shade;float _Variation,_GrassDetail;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;SHADOW_COORDS(2) float viewDepth:TEXCOORD3;float4 screen:TEXCOORD4;};
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.viewDepth=-UnityObjectToViewPos(v.vertex.xyz).z;
                o.screen=ComputeScreenPos(o.pos);
                TRANSFER_SHADOW(o);
                return o;
            }
            float grassHash(float2 p)
            {
                float3 q=frac(p.xyx*float3(.1031,.1030,.0973));
                q+=dot(q,q.yzx+33.33);
                return frac((q.x+q.y)*q.z);
            }
            float grassNoise(float2 p)
            {
                float2 cell=floor(p), t=frac(p);
                t=t*t*(3-2*t);
                return lerp(lerp(grassHash(cell),grassHash(cell+float2(1,0)),t.x),
                    lerp(grassHash(cell+float2(0,1)),grassHash(cell+1),t.x),t.y)-.5;
            }
            fixed4 frag(v2f i):SV_Target
            {
                #if defined(PEACE_PARK_ANIMAL_FADING)
                    ApplyAnimalFade(i.screen);
                #endif
                float diffuse=dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz));
                float wrap=smoothstep(-.15,.95,diffuse);
                fixed3 lighting=lerp(_Shade.rgb,fixed3(1.05,1.035,1),wrap);
                lighting*=lerp(.7,1,SHADOW_ATTENUATION(i));
                float variation=sin(i.world.x*.19+sin(i.world.z*.17))*sin(i.world.z*.23+i.world.x*.07)*_Variation;
                if(_GrassDetail>0)
                {
                    float2 p=i.world.xz;
                    float footprint=length(fwidth(p));
                    float broad=grassNoise(p*.63)*.42;
                    float clumps=grassNoise(p*float2(4.3,6.9))*(1-smoothstep(.05,.2,footprint));
                    float flecks=grassNoise(p*float2(16,22))*(1-smoothstep(.02,.07,footprint));
                    variation+=_GrassDetail*(broad+clumps*.75+flecks*.65);
                }
                fixed4 color=fixed4(_Color.rgb*lighting*(1+variation),1);
                // Interpolate distance, not the clamped vertex fog factor, on large ground triangles.
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    UNITY_CALC_FOG_FACTOR_RAW(i.viewDepth);
                    UNITY_FOG_LERP_COLOR(color,unity_FogColor,unityFogFactor);
                #endif
                return color;
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
