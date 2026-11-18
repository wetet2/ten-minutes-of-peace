Shader "PeacePark/Island Foliage"
{
    Properties { _Color("Canopy color",Color)=(.22,.36,.10,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        struct Input { float3 worldPos; float3 worldNormal; };
        float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
        float noise(float3 p)
        {
            float3 q=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash(q),hash(q+float3(1,0,0)),f.x),lerp(hash(q+float3(0,1,0)),hash(q+float3(1,1,0)),f.x),f.y),
                lerp(lerp(hash(q+float3(0,0,1)),hash(q+float3(1,0,1)),f.x),lerp(hash(q+float3(0,1,1)),hash(q+1),f.x),f.y),f.z);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos;
            float clumps=noise(p*2.6),leaves=noise(p*14);
            leaves=lerp(.5,leaves,1-smoothstep(.05,.3,length(fwidth(p))));
            float shade=.61+.29*saturate(IN.worldNormal.y)+clumps*.28;
            o.Albedo=_Color.rgb*shade*(.87+leaves*.3);
            o.Albedo=lerp(o.Albedo,o.Albedo*float3(1.18,1.13,.8),smoothstep(.62,.85,clumps)*saturate(IN.worldNormal.y));
            o.Smoothness=.12;o.Occlusion=.78+.22*clumps;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
