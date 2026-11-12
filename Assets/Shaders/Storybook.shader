Shader "PeacePark/Storybook"
{
    Properties
    {
        _Color("Paint color",Color)=(1,1,1,1)
        _Shade("Shade tint",Color)=(.62,.76,.69,1)
        _Variation("World variation",Range(0,.1))=0
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
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            fixed4 _Color,_Shade;float _Variation;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;SHADOW_COORDS(2) UNITY_FOG_COORDS(3)};
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;TRANSFER_SHADOW(o);UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float diffuse=dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz));
                float wrap=smoothstep(-.15,.95,diffuse);
                fixed3 lighting=lerp(_Shade.rgb,fixed3(1.05,1.035,1),wrap);
                lighting*=lerp(.7,1,SHADOW_ATTENUATION(i));
                float variation=sin(i.world.x*.19+sin(i.world.z*.17))*sin(i.world.z*.23+i.world.x*.07)*_Variation;
                fixed4 color=fixed4(_Color.rgb*lighting*(1+variation),1);UNITY_APPLY_FOG(i.fogCoord,color);return color;
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
