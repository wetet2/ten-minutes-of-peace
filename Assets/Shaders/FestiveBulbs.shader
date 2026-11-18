Shader "PeacePark/Festive Bulbs"
{
    Properties { _Color ("Bulb color",Color)=(1,.7,.26,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Varyings { float4 pos:SV_POSITION; half glow:TEXCOORD0; UNITY_FOG_COORDS(1) };
            Varyings vert(appdata_base v)
            {
                Varyings o;
                o.pos=UnityObjectToClipPos(v.vertex);
                float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
                float phase=dot(p,float3(1.38,2.13,-.91));
                float wave=sin(_Time.y*2.12+phase)*.68+sin(_Time.y*1.39-phase*.47)*.32;
                o.glow=smoothstep(-.45,.92,wave);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            fixed4 frag(Varyings i):SV_Target
            {
                fixed4 col=fixed4(lerp(_Color.rgb*.48,lerp(_Color.rgb,1,.38),i.glow),1);
                UNITY_APPLY_FOG(i.fogCoord,col);
                return col;
            }
            ENDCG
        }
    }
}
