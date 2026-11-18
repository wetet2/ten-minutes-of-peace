Shader "PeacePark/Winter Cobbles"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert
        #pragma target 3.0
        struct Input { float3 worldPos; };
        void surf(Input i,inout SurfaceOutput o)
        {
            float2 p=i.worldPos.xz/float2(.60,.36);
            p.x+=.5*fmod(abs(floor(p.y)),2);
            float2 edge=.5-abs(frac(p)-.5);
            float pixel=max(fwidth(p.x),fwidth(p.y));
            float face=smoothstep(.026,.065+pixel*.5,min(edge.x,edge.y));
            float noise=frac(sin(dot(floor(p),float2(19.19,37.71)))*4137.2);
            float tone=lerp(.76,.94+.12*noise,face);
            tone=lerp(tone,.97,smoothstep(.25,1,pixel));
            o.Albedo=float3(.51,.46,.49)*tone;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
