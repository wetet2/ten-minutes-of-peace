Shader "PeacePark/Resort Paving"
{
    Properties
    {
        _Color("Stone or tile", Color) = (.88,.75,.58,1)
        _Grout("Joint color", Color) = (.97,.89,.76,1)
        _Cell("Tile size", Vector) = (1.25,.90,0,0)
        _Stagger("Staggered rows", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert
        #pragma target 3.0
        fixed4 _Color, _Grout;
        float4 _Cell;
        float _Stagger;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input i, inout SurfaceOutput o)
        {
            float3 normal = abs(i.worldNormal);
            float2 coordinates = normal.y > .55 ? i.worldPos.xz :
                float2(normal.z > normal.x ? i.worldPos.x : i.worldPos.z, i.worldPos.y);
            float2 p = coordinates / _Cell.xy;
            p.x += .5 * fmod(abs(floor(p.y)),2) * _Stagger;
            float2 edges = .5 - abs(frac(p) - .5);
            float footprint = max(fwidth(p.x), fwidth(p.y));
            float face = smoothstep(.015, .039 + footprint * .36, min(edges.x,edges.y));
            float variation = frac(sin(dot(floor(p),float2(38.7,19.3))) * 4378.2);
            fixed3 stone = _Color.rgb * lerp(.94,1.04,variation);
            o.Albedo = lerp(lerp(_Grout.rgb,stone,face),_Color.rgb,smoothstep(.3,1,footprint)) * .77;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
