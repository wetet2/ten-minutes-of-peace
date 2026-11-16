Shader "PeacePark/AnimalContactShadow"
{
    Properties { _Opacity ("Opacity", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Opacity;

            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(appdata_base input)
            {
                Varyings output;
                output.position=UnityObjectToClipPos(input.vertex);
                output.uv=input.texcoord.xy*2-1;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radiusSquared=dot(input.uv,input.uv);
                float falloff=1-smoothstep(.10,1,radiusSquared);
                return half4(.16,.19,.13,falloff*falloff*.32*_Opacity);
            }
            ENDCG
        }
    }
}
