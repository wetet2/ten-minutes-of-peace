#ifndef PEACE_BEACH_COAST_INCLUDED
#define PEACE_BEACH_COAST_INCLUDED
// Keep the same signed distance as ParkBeachMeshes.LandDistance.
float PeaceBeachLandDistance(float2 p)
{
    float shore=-7-.014*p.x*p.x+.25*sin(p.x*.4);
    float slope=-.028*p.x+.1*cos(p.x*.4);
    float bay=(p.y-shore)/sqrt(1+slope*slope);
    float edge=(1-length(float2((p.x+8)/62,(p.y-100)/120)))*55;
    float blend=saturate(.5+.5*(edge-bay)/4);
    return lerp(edge,bay,blend)-4*blend*(1-blend);
}
#endif
