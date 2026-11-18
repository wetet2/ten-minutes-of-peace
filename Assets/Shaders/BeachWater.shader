Shader "PeacePark/Beach Water"
{
    Properties { _Shallow("Shallow water",Color)=(.025,.60,.61,1) _Deep("Deep water",Color)=(.008,.24,.38,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "BeachCoast.cginc"
            float4 _Shallow,_Deep;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o; float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
                float nearby=1-smoothstep(70,180,length(_WorldSpaceCameraPos.xz-p.xz));
                p.y+=(sin(p.x*.53+p.z*.8+_Time.y*.9)*.028+sin(p.z*1.9-_Time.y*1.1)*.012)*nearby;
                o.world=p; o.pos=mul(UNITY_MATRIX_VP,float4(p,1)); UNITY_TRANSFER_FOG(o,o.pos); return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.world.xz;
                float tide=.28*sin(_Time.y*.62)+.12*sin(p.x*.65+_Time.y*.34);
                float landDistance=PeaceBeachLandDistance(p);
                float distance=-landDistance+tide;
                clip(distance);
                float depth=max(0,-landDistance)*.17;
                float rippleVisibility=1-smoothstep(.12,.85,length(fwidth(p)));
                float3 normal=normalize(float3(.065*cos(p.x*1.5+p.y*2.1+_Time.y)*rippleVisibility,1,
                    (.11*cos(p.y*2.2-_Time.y*1.1)+.045*sin(p.x*3.3+p.y*1.6+_Time.y))*rippleVisibility));
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float fresnel=pow(1-saturate(dot(view,normal)),4);
                float3 water=lerp(_Shallow.rgb,_Deep.rgb,1-exp(-depth*.25));
                water=lerp(water,float3(.51,.79,.87),fresnel*.62);
                float foam=(1-smoothstep(.10,.75,distance))*.94;
                float breaker=frac((distance+_Time.y*.7+sin(p.x*.43)*.17)/3.8);
                foam+=pow(saturate(1-abs(breaker-.5)*18),3)*exp(-distance*.22)*.57;
                float breakup=.65+.35*sin(p.x*19+sin(p.y*17)+_Time.y)*sin(p.y*24-p.x*3);
                foam*=breakup;
                float3 sunDirection=normalize(float3(-.45,.8,-.35));
                float sparkle=pow(saturate(dot(normal,normalize(view+sunDirection))),170)*.85;
                float3 color=lerp(water,float3(.94,.99,.94),saturate(foam))+sparkle;
                float opacity=saturate(.19+(1-exp(-depth*.55))*.81+fresnel*.25+foam*.78);
                fixed4 result=fixed4(color,opacity*smoothstep(0,.10,distance));
                UNITY_APPLY_FOG(i.fogCoord,result); return result;
            }
            ENDCG
        }
    }
}
