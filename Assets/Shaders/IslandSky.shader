Shader "PeacePark/Island Sky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata{float4 vertex:POSITION;};
            struct v2f{float4 pos:SV_POSITION;float3 direction:TEXCOORD0;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);
                // Match the distant ocean fog so the far clip cannot draw a colored horizon strip.
                float3 sky=lerp(unity_FogColor.rgb,float3(.13,.52,.82),pow(saturate(d.y*1.9),.55));
                float2 p=d.xz/(max(d.y,.02)+.22)*3.5+float2(_Time.y*.001,0);
                float n=noise(p)*.53+noise(p*2.03)*.27+noise(p*4.1)*.13+noise(p*8.3)*.07;
                float cloud=smoothstep(.53,.70,n)*smoothstep(.015,.12,d.y);
                float3 white=lerp(float3(.77,.85,.87),float3(1,.99,.92),smoothstep(.51,.72,n));
                return fixed4(lerp(sky,white,cloud*.96),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
