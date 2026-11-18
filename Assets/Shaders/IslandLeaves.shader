Shader "PeacePark/Island Leaves"
{
    Properties { _MainTex("Leaf cluster",2D)="white" {} _Color("Leaf tint",Color)=(1,1,1,1) _Cutoff("Leaf edge",Range(0,1))=.45 }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;fixed4 _Color;
        struct Input { float2 uv_MainTex; float3 worldPos; };
        void vert(inout appdata_full v)
        {
            float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
            v.vertex.x+=sin(_Time.y*.95+p.x*.37+p.z*.29)*.035*v.texcoord.y;
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            fixed4 leafColor=tex2D(_MainTex,IN.uv_MainTex);
            o.Albedo=leafColor.rgb*_Color.rgb;o.Alpha=leafColor.a;o.Smoothness=.13;
            o.Emission=o.Albedo*.12;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
