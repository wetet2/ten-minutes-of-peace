Shader "PeacePark/RabbitPaint"
{
    Properties
    {
        _FrontTex ("Front painting", 2D) = "white" {}
        _BackTex ("Back painting", 2D) = "white" {}
        _BaseColor ("Base color", Color) = (1,1,1,1)
        _Species ("Animal species", Float) = 2
        _BoundsMin ("Character bounds min", Vector) = (-1.7,0,0,0)
        _BoundsSize ("Character bounds size", Vector) = (3.4,5.2,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _FrontTex;
            sampler2D _BackTex;
            fixed4 _BaseColor;
            float _Species;
            float4 _BoundsMin;
            float4 _BoundsSize;

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 side:TEXCOORD1; float3 rest:TEXCOORD2; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float3 paint:TEXCOORD0;
                float3 worldNormal:TEXCOORD1;
                SHADOW_COORDS(2)
                float3 rest:TEXCOORD3;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.paint=float3(v.uv,v.side.x);
                o.rest=v.rest;
                o.worldNormal=UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o);
                return o;
            }
            float Marking(float3 p)
            {
                if (_Species == 1)
                {
                    if(p.z>.58 && p.y<1.58 && p.x>.15)
                        return smoothstep(.02,.31,sin((p.x*.67-p.y*.52+p.z*.55)*24))*.90;
                    if(p.y<2.59 || p.z>-.17)return 0;
                    float x=abs(p.x), cheek=0;
                    for(int j=0;j<3;j++)
                    {
                        float stripeCenter=3.22-j*.205+.31*(x-.64)+.045*sin(x*9+j);
                        cheek=max(cheek,(1-smoothstep(.035,.067,abs(p.y-stripeCenter)))*smoothstep(.57,.75,x));
                    }
                    float brow=(1-smoothstep(.035,.068,abs(p.y-3.48-.12*x)))*(1-smoothstep(.20,.44,x));
                    float crown=(1-smoothstep(.045,.089,abs(p.y-3.67)))*(1-smoothstep(.13,.33,x));
                    return max(cheek,max(brow,crown))*.92;
                }
                if(_Species == 4 || _Species == 5)
                {
                    if(p.z>.63 && p.y<(_Species==4?.91:1.01))return .83;
                    if(_Species != 4)return 0;
                    if(p.y>4.89)return smoothstep(4.89,4.95,p.y)*.95;
                    if(p.y<2.48 || p.y>3.64)return 0;
                    float vertical=(p.y-2.48)/.29;
                    float around=atan2(p.z-.08,p.x)*.795775+floor(vertical)*.5;
                    float2 spot=frac(float2(around,vertical))-.5;
                    return (1-smoothstep(.26,.39,length(spot)))*.60;
                }
                return 0;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=saturate(i.paint.xy);
                fixed4 front=tex2D(_FrontTex,uv);
                fixed4 back=tex2D(_BackTex,uv);
                float side=i.paint.z;
                fixed4 painting=lerp(front,back,side);
                fixed3 albedo=lerp(_BaseColor.rgb,painting.rgb,painting.a);
                albedo=lerp(albedo,albedo*fixed3(.31,.23,.19),Marking(i.rest));
                if(_Species==6)
                {
                    float3 p=i.rest;
                    float frill=smoothstep(.12,.42,p.z)*smoothstep(2.69,3.36,p.y);
                    albedo=lerp(albedo,albedo*fixed3(.69,.86,.81),frill*.78);
                    float brow=smoothstep(3.52,3.77,p.y)*(1-smoothstep(-.66,-.49,p.z))
                        *smoothstep(.19,.36,abs(p.x));
                    float nose=(1-smoothstep(.10,.22,abs(p.x)))*smoothstep(3.03,3.16,p.y)
                        *(1-smoothstep(-1.17,-.98,p.z));
                    albedo=lerp(albedo,fixed3(1,.92,.71),max(brow,nose)*.72);
                }
                float3 n=normalize(i.worldNormal);
                float3 l=normalize(_WorldSpaceLightPos0.xyz);
                float lambert=saturate(dot(n,l));
                float shadow=SHADOW_ATTENUATION(i);
                fixed3 light=lerp(fixed3(.65,.78,.73),fixed3(1.05,1.035,1),smoothstep(-.65,.9,dot(n,l)))*lerp(.7,1,shadow);
                return fixed4(albedo*light,1);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
