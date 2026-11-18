Shader "PeacePark/Beach Surface"
{
    Properties
    {
        _Color("Base color", Color) = (1,1,1,1)
        _Kind("Sand 0 bark 1 stone 2 leaf 3", Float) = 0
        _Wind("Leaf motion", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #include "UnityCG.cginc"
        #include "BeachCoast.cginc"
        fixed4 _Color;
        float _Kind, _Wind;
        struct Input { float3 worldPos; float2 leafUV; float4 tint; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 a=floor(p), t=frac(p); t=t*t*(3-2*t);
            return lerp(lerp(hash(a),hash(a+float2(1,0)),t.x),lerp(hash(a+float2(0,1)),hash(a+1),t.x),t.y);
        }
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
            v.vertex.x+=sin(_Time.y*1.3+p.x*.47+p.z*.36)*_Wind*v.texcoord.y*v.texcoord.y;
            v.vertex.z+=cos(_Time.y*.94+p.x*.31)*_Wind*.5*v.texcoord.y;
            o.leafUV=v.texcoord.xy;
            o.tint=v.color;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float broad=noise(p*.32), fine=noise(p*63);
            fine=lerp(.5,fine,1-smoothstep(.015,.075,length(fwidth(p))));
            float3 color=_Color.rgb;
            float smoothness=.12;
            if (_Kind<.5)
            {
                float distance=PeaceBeachLandDistance(p);
                float wet=1-smoothstep(.0,1.35,distance);
                color*=lerp(.94,1.05,broad)*lerp(.93,1.04,fine);
                float ripple=sin(p.y*12+sin(p.x*1.5)*.8+noise(p*2)*.6);
                color*=1+.012*ripple;
                color=lerp(color,color*float3(.68,.72,.70),wet*.67);
                float forest=smoothstep(12,23,p.y)*smoothstep(1,5,distance)*smoothstep(.06,.17,IN.worldPos.y);
                color=lerp(color,float3(.18,.23,.10)*(.75+.35*broad),forest);
                float underwater=1-smoothstep(-.45,.05,IN.worldPos.y);
                float2 ca=p*2+float2(_Time.y*.13,_Time.y*.09);
                float veins=abs(sin(ca.x+sin(ca.y*1.13))) * abs(sin(ca.y+cos(ca.x*.87)));
                float light=pow(saturate(1-veins),18)*underwater*(1-smoothstep(.04,.40,length(fwidth(p))))*exp(min(0,IN.worldPos.y)*.25);
                o.Emission=color*light*.10;
                color*=1+light*.16;
                smoothness=lerp(.10,.43,wet);
            }
            else if (_Kind<1.5)
            {
                float rings=pow(.5+.5*sin(IN.worldPos.y*28+noise(p*5)*1.2),9);
                color*=.72+.29*noise(float2(IN.leafUV.x*50,IN.worldPos.y*2))-.21*rings;
            }
            else if (_Kind<2.5)
            {
                float mineral=noise(p*4.3+IN.worldPos.y*1.9);
                color*=.75+.38*noise(p*.8+IN.worldPos.y)+.13*mineral;
                float moss=smoothstep(.55,.75,noise(p*.7+IN.worldPos.y*.23))*smoothstep(.4,1.8,IN.worldPos.y);
                color=lerp(color,float3(.20,.30,.075),moss*.55);
                color*=lerp(.53,1,smoothstep(-.3,1.0,IN.worldPos.y));
            }
            else if (_Kind<3.5)
            {
                float midrib=exp(-abs(IN.leafUV.x-.5)*95);
                float vein=pow(.5+.5*cos((IN.leafUV.y-abs(IN.leafUV.x-.5)*.43)*110),14);
                color*=IN.tint.rgb*(.86+.18*IN.leafUV.y+.10*vein);
                color=lerp(color,color*1.38,midrib*.55);
                smoothness=.31;
                o.Emission=color*.055;
            }
            else
            {
                float grooves=noise(float2((p.x+p.y*.71)*14,IN.worldPos.y*.55));
                color*=.57+.59*grooves+.11*noise(p*27+IN.worldPos.y*3);
                smoothness=.08;
            }
            o.Albedo=color; o.Metallic=0; o.Smoothness=smoothness; o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
