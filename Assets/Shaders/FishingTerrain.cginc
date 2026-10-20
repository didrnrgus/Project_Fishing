// Fishing/Terrain 첫 패스와 추가 패스(5번째 층부터)가 함께 쓰는 코드.
// 내장 Nature/Terrain/Standard와 같은 엔진 속성(_Control, _Splat0..3, _Normal0..3, _Smoothness0..3 …)을 받고,
// 층마다 텍스처를 두 번(원래 UV + 회전·다른 배율 UV) 읽어 저주파 노이즈로 섞어 반복 무늬를 없앤다.
// 월드 좌표 큰 얼룩으로 밝기·광택도 흔든다 (Ground.shader와 같은 방식).
#ifndef FISHING_TERRAIN_INCLUDED
#define FISHING_TERRAIN_INCLUDED

#include "FishingNoise.cginc"

sampler2D _Control;
float4 _Control_ST;
sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
float4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
half _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
half _Metallic0, _Metallic1, _Metallic2, _Metallic3;
half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;
half4 _DiffuseRemapScale0, _DiffuseRemapScale1, _DiffuseRemapScale2, _DiffuseRemapScale3;
half _Variation;

struct Input
{
    float2 tc;
    float3 worldPos;
    float3 wn;
    UNITY_FOG_COORDS(0)
};

void TerrainVert(inout appdata_full v, out Input data)
{
    UNITY_INITIALIZE_OUTPUT(Input, data);
    data.tc = TRANSFORM_TEX(v.texcoord.xy, _Control);
    data.wn = UnityObjectToWorldNormal(v.normal);
    // 지형 메쉬에는 접선이 없다 (내장 지형 셰이더와 같은 방법으로 만든다)
    v.tangent.xyz = cross(v.normal, float3(0, 0, 1));
    v.tangent.w = -1;
    float4 pos = UnityObjectToClipPos(v.vertex);
    UNITY_TRANSFER_FOG(data, pos);
}

static const float2x2 FishRot = float2x2(0.7986, -0.6018, 0.6018, 0.7986);

void SampleLayer(sampler2D tex, sampler2D nor, float4 st, half nscale, float2 tc, float m, out half3 col, out half3 n)
{
    float2 uv = tc * st.xy + st.zw;
    float2 uv2 = mul(FishRot, uv) * 0.73 + float2(0.37, 0.61);
    col = lerp(tex2D(tex, uv).rgb, tex2D(tex, uv2).rgb, m);
    half3 na = UnpackScaleNormal(tex2D(nor, uv), nscale);
    half3 nb = UnpackScaleNormal(tex2D(nor, uv2), nscale);
    nb.xy = mul(nb.xy, FishRot);
    n = lerp(na, nb, m);
}

// 가파른 면(절벽): 위에서 내려 찍은 UV는 세로로 늘어나 줄무늬가 된다 → 옆 두 방향(월드 X·Z)에서 찍은 색을 면 방향 비율로 섞는다.
// perM = 지형 UV(tc)가 1m에 바뀌는 양 × 층 반복 횟수. side = 옆 투영 비중 (평지 0)
half3 SideColor(sampler2D tex, float4 st, float3 wp, float perM, half3 tw, half3 planar)
{
    float k = perM * st.x;
    half3 cx = tex2D(tex, wp.zy * k).rgb;
    half3 cz = tex2D(tex, wp.xy * k + 0.31).rgb;
    return planar * tw.y + cx * tw.x + cz * tw.z;
}

void TerrainSurf(Input IN, inout SurfaceOutputStandard o)
{
    half4 ctrl = tex2D(_Control, IN.tc);
    half weight = dot(ctrl, half4(1, 1, 1, 1));
#if defined(TERRAIN_SPLAT_ADDPASS)
    clip(weight <= 0.005h ? -1 : 1);
#endif
    ctrl /= (weight + 1e-3f);

    float2 w = IN.worldPos.xz;
    float m = smoothstep(0.35, 0.65, FishNoise(w * 0.09) * 0.6 + FishNoise(w * 0.031 + 5.7) * 0.4);
    half3 c0, c1, c2, c3, n0, n1, n2, n3;
    SampleLayer(_Splat0, _Normal0, _Splat0_ST, _NormalScale0, IN.tc, m, c0, n0);
    SampleLayer(_Splat1, _Normal1, _Splat1_ST, _NormalScale1, IN.tc, m, c1, n1);
    SampleLayer(_Splat2, _Normal2, _Splat2_ST, _NormalScale2, IN.tc, m, c2, n2);
    SampleLayer(_Splat3, _Normal3, _Splat3_ST, _NormalScale3, IN.tc, m, c3, n3);
    // 절벽 트라이플래너: 경사 40도부터 섞기 시작해 수직이면 옆 투영만
    float3 nw = normalize(IN.wn);
    half3 tw = pow(abs(nw), 4);
    tw /= (tw.x + tw.y + tw.z);
    if (tw.y < 0.97)
    {
        float perM = (length(ddx(IN.tc)) + length(ddy(IN.tc))) / max(length(ddx(IN.worldPos.xz)) + length(ddy(IN.worldPos.xz)), 1e-5);
        if (ctrl.r > 0.01) c0 = SideColor(_Splat0, _Splat0_ST, IN.worldPos, perM, tw, c0);
        if (ctrl.g > 0.01) c1 = SideColor(_Splat1, _Splat1_ST, IN.worldPos, perM, tw, c1);
        if (ctrl.b > 0.01) c2 = SideColor(_Splat2, _Splat2_ST, IN.worldPos, perM, tw, c2);
        if (ctrl.a > 0.01) c3 = SideColor(_Splat3, _Splat3_ST, IN.worldPos, perM, tw, c3);
    }
    half3 col = c0 * ctrl.r * _DiffuseRemapScale0.rgb + c1 * ctrl.g * _DiffuseRemapScale1.rgb
              + c2 * ctrl.b * _DiffuseRemapScale2.rgb + c3 * ctrl.a * _DiffuseRemapScale3.rgb;
    half3 n = n0 * ctrl.r + n1 * ctrl.g + n2 * ctrl.b + n3 * ctrl.a;

    float macro = FishNoise(w * 0.035) * 0.65 + FishNoise(w * 0.15 + 3.3) * 0.35;
    col *= lerp(1.0 - 0.3 * _Variation, 1.0 + 0.14 * _Variation, macro);
    half lum = dot(col, half3(0.299, 0.587, 0.114));
    col = lerp(lum.xxx, col, lerp(0.85, 1.08, macro));

    o.Albedo = col;
    o.Normal = normalize(n + half3(0, 0, 1e-4));
    half smooth = dot(ctrl, half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3));
    o.Smoothness = saturate(smooth * lerp(0.7, 1.25, macro));
    o.Metallic = dot(ctrl, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
    o.Alpha = weight;
}

void TerrainFinal(Input IN, SurfaceOutputStandard o, inout fixed4 color)
{
    color *= o.Alpha;
#if defined(TERRAIN_SPLAT_ADDPASS)
    UNITY_APPLY_FOG_COLOR(IN.fogCoord, color, fixed4(0, 0, 0, 0));
#else
    UNITY_APPLY_FOG(IN.fogCoord, color);
#endif
}

#endif
