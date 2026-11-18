Shader "PeacePark/Waterfall"
{
    Properties { _FlowTime("Flow time",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float _FlowTime;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.x+=sin(v.uv.y*19-_FlowTime*4)*.045*v.uv.y;
                o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float x=i.uv.x,t=i.uv.y;
                // UV.y increases down the cliff; subtracting time moves highlights downwards.
                float streak=.5+.5*sin(x*39+sin(t*16-_FlowTime*6)*.65);
                float surge=.5+.5*sin(t*95-_FlowTime*26+x*9);
                float foam=pow(streak,3)*.55+pow(surge,7)*.40;
                float edge=smoothstep(0,.15,x)*smoothstep(0,.15,1-x);
                float3 color=lerp(float3(.24,.70,.78),float3(.96,1,1),.50+foam*.5);
                fixed4 result=fixed4(color,edge*(.70+foam*.27)*smoothstep(1,.88,t));
                UNITY_APPLY_FOG(i.fogCoord,result);return result;
            }
            ENDCG
        }
    }
}
