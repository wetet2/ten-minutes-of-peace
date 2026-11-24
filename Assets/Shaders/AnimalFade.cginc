#ifndef PEACE_PARK_ANIMAL_FADE
#define PEACE_PARK_ANIMAL_FADE

float _AnimalVisibility;

void ApplyAnimalFadeAtPixel(float2 pixel)
{
    // Interleaved gradient noise keeps a depth pass for the visible surfaces.
    float threshold = frac(52.9829189 * frac(dot(pixel, float2(.06711056, .00583715))));
    clip(_AnimalVisibility - threshold - .00001);
}

void ApplyAnimalFade(float4 screen)
{
    ApplyAnimalFadeAtPixel(floor(screen.xy / max(screen.w, .0001) * _ScreenParams.xy));
}

#endif
