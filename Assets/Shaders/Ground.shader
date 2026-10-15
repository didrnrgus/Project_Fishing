// 반복 무늬 깨기 바닥·바위 셰이더 (Standard 조명).
// 같은 텍스처를 (1) 원래 UV와 (2) 회전·다른 배율 UV로 두 번 읽어 저주파 노이즈로 섞고,
// 월드 좌표 큰 얼룩(수 m ~ 수십 m)으로 밝기·광택을 흔든다. 그래서 1K 텍스처를 넓게 반복해도 격자 무늬가 안 보인다.
// 월드 좌표를 쓰므로 정적 배칭(월드 좌표로 굳힌 메쉬)해도 결과가 같다.
Shader "Fishing/Ground"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0
        _Variation ("Macro Variation", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        // 머티리얼을 런타임에 만들어서 shader_feature면 빌드에서 빠진다
        #pragma multi_compile_local __ _NORMALMAP

        #include "FishingNoise.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _BumpScale, _Glossiness, _Metallic, _Variation;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            // 37도 회전 + 0.73배 + 어긋남: 원래 타일과 격자가 맞지 않는 두 번째 샘플
            const float2x2 rot = float2x2(0.7986, -0.6018, 0.6018, 0.7986);
            float2 uv2 = mul(rot, uv) * 0.73 + float2(0.37, 0.61);
            // 섞는 비율: 타일 크기보다 큰 얼룩 (UV 기준) + 월드 기준 얼룩을 더해 반복 단위마다 다르게
            float n = FishNoise(uv * 0.55) * 0.6 + FishNoise(IN.worldPos.xz * 0.11 + 7.1) * 0.4;
            float m = smoothstep(0.35, 0.65, n);

            fixed4 a = tex2D(_MainTex, uv);
            fixed4 b = tex2D(_MainTex, uv2);
            fixed4 c = lerp(a, b, m);

            // 큰 얼룩: 밝기·채도 살짝, 물기·먼지 느낌의 광택 변화
            float macro = FishNoise(IN.worldPos.xz * 0.045) * 0.65 + FishNoise(IN.worldPos.xz * 0.19 + 3.3) * 0.35;
            float bright = lerp(1.0 - 0.28 * _Variation, 1.0 + 0.14 * _Variation, macro);
            fixed3 col = c.rgb * bright;
            float lum = dot(col, fixed3(0.299, 0.587, 0.114));
            col = lerp(lum.xxx, col, lerp(0.85, 1.08, macro));
            o.Albedo = col * _Color.rgb;

        #if defined(_NORMALMAP)
            half3 na = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            half3 nb = UnpackScaleNormal(tex2D(_BumpMap, uv2), _BumpScale);
            nb.xy = mul(nb.xy, rot); // 회전해서 읽은 법선을 원래 UV 방향으로 되돌린다
            o.Normal = normalize(lerp(na, nb, m));
        #endif
            o.Smoothness = saturate(_Glossiness * lerp(0.7, 1.25, macro));
            o.Metallic = _Metallic;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
