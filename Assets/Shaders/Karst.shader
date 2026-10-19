// 석회암 봉우리(하롱베이 karst) 셰이더 (Standard 조명).
// 예전에는 바닥용 rock 텍스처(이끼 낀 화강암 항공사진)를 Fishing/Ground로 4m마다 반복해, 수직 절벽에 같은 무늬가 줄지어 보였다.
// 여기서는 실사 석회암 두 장(Rocks and Boulders 2: 지의류 낀 흰 석회암 _MainTex, 층이 진 회색 석회암 _LayerTex)을
// (1) 큰 배율 두 벌(약 9m·14m, 하나는 37도 회전)로 읽어 월드 3D 노이즈로 섞고, (2) 회색 층 바위를 큰 덩어리로 섞고,
// (3) 2.5m 잔무늬는 밝기로만 얹고, (4) 빗물이 흘러내린 검은 세로 얼룩과 주황빛 녹물 얼룩,
// (5) 턱·위를 보는 면에 몰리는 덤불 덩어리, (6) 수십 m 크기 명암으로 반복을 지운다.
// 노이즈는 모두 월드 좌표라 정적 배칭해도 같고, 메쉬 UV 이음매(둘레 한 바퀴)에 걸리지 않는다.
Shader "Fishing/Karst"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _LayerTex ("Layer Albedo", 2D) = "white" {}
        [Normal] _LayerBump ("Layer Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.15
        _Saturation ("Rock Saturation", Range(0,1)) = 0.8
        _StreakColor ("Streak Color", Color) = (0.45,0.45,0.42,1)
        _Streaks ("Streak Amount", Range(0,1)) = 0.7
        _MossColor ("Moss Color", Color) = (0.3,0.42,0.22,1)
        _Moss ("Moss Amount", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow
        #pragma target 3.0
        #pragma multi_compile_local __ _NORMALMAP

        #include "FishingNoise.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _LayerTex, _LayerBump;
        fixed4 _Color, _StreakColor, _MossColor;
        half _BumpScale, _Glossiness, _Saturation, _Streaks, _Moss;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 wn;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.wn = UnityObjectToWorldNormal(v.normal);
        }

        // 수직 절벽용 3D 노이즈 흉내: x-y 평면과 z-y 평면 노이즈의 평균 (둘레 어느 방향 면에서도 이어진다)
        float WallNoise(float3 p, float2 k)
        {
            return 0.5 * (FishNoise(float2(p.x * k.x, p.y * k.y)) + FishNoise(float2(p.z * k.x + 17.3, p.y * k.y + 5.1)));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 wp = IN.worldPos;
            float2 uv = IN.uv_MainTex; // 메쉬 UV는 1 = 4m
            const float2x2 rot = float2x2(0.7986, -0.6018, 0.6018, 0.7986);
            float2 uvA = uv * 0.45;                                  // 약 9m 한 장
            float2 uvB = mul(rot, uv) * 0.29 + float2(0.37, 0.61);   // 약 14m, 회전
            float2 uvL = uv * float2(0.38, 0.55) + float2(0.53, 0.11); // 층 바위: 가로로 조금 길게 (퇴적층 방향)
            float2 uvD = uv * 1.6 + float2(0.21, 0.83);              // 2.5m 잔무늬

            float n = WallNoise(wp, float2(0.045, 0.035)) * 0.65 + WallNoise(wp + 31.0, float2(0.16, 0.12)) * 0.35;
            float m = smoothstep(0.38, 0.62, n);
            fixed3 c = lerp(tex2D(_MainTex, uvA).rgb, tex2D(_MainTex, uvB).rgb, m);
            // 회색 층 바위 덩어리 (봉우리 면의 30~40%)
            float ln = WallNoise(wp + 13.0, float2(0.03, 0.05)) * 0.7 + WallNoise(wp + 41.0, float2(0.12, 0.1)) * 0.3;
            float layerMask = smoothstep(0.5, 0.62, ln);
            c = lerp(c, tex2D(_LayerTex, uvL).rgb * 1.05, layerMask);
            float detail = dot(tex2D(_LayerTex, uvD).rgb, fixed3(0.299, 0.587, 0.114));
            c *= lerp(0.82, 1.18, saturate(detail * 1.7));

            float lum = dot(c, fixed3(0.299, 0.587, 0.114));
            fixed3 col = lerp(lum.xxx, c, _Saturation);

            // 큰 명암 (수십 m): 봉우리마다·높이마다 색이 다르게
            float macro = WallNoise(wp, float2(0.012, 0.01)) * 0.7 + FishNoise(wp.xz * 0.004 + 3.3) * 0.3;
            col *= lerp(0.8, 1.12, macro);

            // 주황빛 녹물 얼룩 (하롱베이 석회암 절벽의 황토색 띠)
            float rust = smoothstep(0.62, 0.8, WallNoise(wp + 87.0, float2(0.06, 0.03))) * 0.55;
            col = lerp(col, col * fixed3(1.12, 0.86, 0.62), rust);

            // 빗물 얼룩: 가로로 좁고 세로로 긴 검은 줄
            float s = WallNoise(wp, float2(0.28, 0.018)) * 0.65 + WallNoise(wp + 9.0, float2(0.9, 0.05)) * 0.35;
            float streak = smoothstep(0.5, 0.78, s) * _Streaks;
            col = lerp(col, col * _StreakColor.rgb, streak);

            // 덤불: 위를 보는 면과 턱에 몰리고, 깎아지른 면에는 드문드문 덩어리로
            float up = saturate(IN.wn.y * 1.6 + 0.25);
            float mossN = WallNoise(wp + 53.0, float2(0.09, 0.07)) * 0.6 + WallNoise(wp + 71.0, float2(0.35, 0.3)) * 0.4;
            float moss = smoothstep(0.66, 0.74, mossN + up * 0.32) * _Moss;
            fixed3 mossCol = _MossColor.rgb * lerp(0.6, 1.35, saturate(detail * 1.8));
            col = lerp(col, mossCol, moss);

            o.Albedo = col * _Color.rgb;

        #if defined(_NORMALMAP)
            half3 na = UnpackScaleNormal(tex2D(_BumpMap, uvA), _BumpScale);
            half3 nb = UnpackScaleNormal(tex2D(_BumpMap, uvB), _BumpScale);
            nb.xy = mul(nb.xy, rot);
            half3 nl = UnpackScaleNormal(tex2D(_LayerBump, uvL), _BumpScale);
            o.Normal = normalize(lerp(lerp(na, nb, m), nl, layerMask));
        #endif
            o.Smoothness = saturate(_Glossiness * lerp(1.2, 0.5, max(moss, streak * 0.5)));
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
