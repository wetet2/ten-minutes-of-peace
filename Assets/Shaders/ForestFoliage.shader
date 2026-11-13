Shader "PeacePark/Forest foliage"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull [_Cull]
        CGPROGRAM
        #pragma surface surf Lambert addshadow fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Cutoff;
        struct Input { float2 uv_MainTex; };
        void surf(Input input, inout SurfaceOutput output)
        {
            fixed4 texel = tex2D(_MainTex, input.uv_MainTex);
            clip(texel.a - _Cutoff);
            output.Albedo = texel.rgb * _Color.rgb;
            output.Alpha = 1;
        }
        ENDCG
    }
    FallBack Off
}
