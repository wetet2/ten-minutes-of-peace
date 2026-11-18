Shader "PeacePark/Waterfall Mist"
{
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;UNITY_TRANSFER_FOG(o,o.pos);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float radius=length((i.uv-.5)*2);
                fixed4 result=i.color;result.a*=pow(saturate(1-radius*radius),2);
                UNITY_APPLY_FOG(i.fogCoord,result);return result;
            }
            ENDCG
        }
    }
}
