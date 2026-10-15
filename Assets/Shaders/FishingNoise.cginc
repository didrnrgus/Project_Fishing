// 반복 무늬 깨기용 값 노이즈 (Ground.shader, FishingTerrain.cginc)
#ifndef FISHING_NOISE_INCLUDED
#define FISHING_NOISE_INCLUDED

float FishHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// 부드러운 값 노이즈 (0~1)
float FishNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(FishHash(i), FishHash(i + float2(1, 0)), u.x),
                lerp(FishHash(i + float2(0, 1)), FishHash(i + float2(1, 1)), u.x), u.y);
}

#endif
