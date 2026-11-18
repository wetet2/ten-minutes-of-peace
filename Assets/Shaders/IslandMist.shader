Shader "PeacePark/Island Mist"
{
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=float2(i.world.x*.13-_Time.y*.015,i.uv.y*3+i.world.z*.31);
                float n=noise(p)*.65+noise(p*2.13)*.35;
                float center=.48+.15*sin(i.uv.x*14+i.world.z)+.11*(n-.5);
                float ribbon=pow(saturate(1-abs(i.uv.y-center)*2.8),3);
                float edges=smoothstep(0,.15,i.uv.x)*smoothstep(0,.15,1-i.uv.x);
                return fixed4(.83,.92,.89,ribbon*edges*smoothstep(.2,.8,n)*.52);
            }
            ENDCG
        }
    }
}
