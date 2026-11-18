Shader "PeacePark/Beach Mountain"
{
    Properties { _Color("Rock color",Color)=(.90,.94,.85,1) _RockTexture("Cliff stone albedo",2D)="white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        sampler2D _RockTexture;
        struct Input { float3 worldPos; float3 worldNormal; };
        float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        float noise(float2 p)
        {
            float2 a=floor(p),t=frac(p);t=t*t*(3-2*t);
            return lerp(lerp(hash(a),hash(a+float2(1,0)),t.x),lerp(hash(a+float2(0,1)),hash(a+1),t.x),t.y);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos;
            float variation=noise(p.xz*.23+p.y*.06);
            float mineral=noise(float2(p.x+p.z*.41,p.y)*1.8);
            float strata=pow(.5+.5*sin(p.y*1.4+noise(p.xz*.32)*5),18);
            float cracks=pow(.5+.5*sin(p.x*2.7+noise(p.zy*.12)*8),24);
            float wet=smoothstep(.57,.79,noise(float2(p.x*.5,p.y*.045)));
            float3 weights=pow(abs(IN.worldNormal),4);weights/=max(.001,weights.x+weights.y+weights.z);
            float3 stone=tex2D(_RockTexture,p.zy*.12).rgb*weights.x+tex2D(_RockTexture,p.xz*.12).rgb*weights.y+tex2D(_RockTexture,p.xy*.12).rgb*weights.z;
            float3 rock=stone*_Color.rgb*(.83+variation*.34+mineral*.12-strata*.05-cracks*.07-wet*.10);
            float moss=smoothstep(.32,.78,IN.worldNormal.y+noise(p.xz*.20)*.15)*(.80+.20*variation);
            o.Albedo=lerp(rock,stone*float3(.49,.80,.22)*(.72+variation*.45),moss*.88);
            o.Smoothness=.08;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
