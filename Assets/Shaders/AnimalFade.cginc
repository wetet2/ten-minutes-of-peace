#ifndef PEACE_PARK_ANIMAL_FADE
#define PEACE_PARK_ANIMAL_FADE

float _AnimalVisibility;

void ApplyAnimalFade(float4 screen)
{
    // Interleaved gradient noise keeps an opaque depth pass for the skinned surfaces.
    float2 pixel = floor(screen.xy / max(screen.w, .0001) * _ScreenParams.xy);
    float threshold = frac(52.9829189 * frac(dot(pixel, float2(.06711056, .00583715))));
    clip(_AnimalVisibility - threshold - .00001);
}

#endif
