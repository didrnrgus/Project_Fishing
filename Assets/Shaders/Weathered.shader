// 풍화 표면 셰이더 (Standard 조명): 호안·방파제·테트라포드·석상·회벽 집·바위 봉우리.
//  - 트라이플래너: 월드 좌표로 세 방향에서 텍스처를 읽어 면 방향으로 섞는다. 그래서 UV가 0~1뿐인 상자 프리미티브나 UV 없는 Blender 소품도
//    늘어나지 않고 _Tile(m)마다 한 장씩 고르게 깔린다. 정적 배칭(월드 좌표로 굳힌 메쉬)해도 결과가 같다.
//  - 물 자국: 전역 _FishingWetY(맵 만조선) 아래는 젖어서 어둡고 반들거리며, 그 아래 띠는 따개비·물때로 녹갈색 (_Wet)
//  - 빗물 자국: 세로 면에 위에서 아래로 흘러내린 거뭇한 줄 (_Streaks)
//  - 이끼·먼지: 위를 보는 면과 틈에 _MossColor 얼룩 (_Moss)
//  - 큰 얼룩: Ground 셰이더처럼 수 m 단위 밝기 흔들림으로 반복 무늬를 감춘다
//  - 바닥 때 (_Grime): 위를 보는 면에 갈라진 금(보로노이 경계를 노이즈로 휘고 군데군데만), 기름·물 얼룩, 얕은 물웅덩이(번들, 법선 평평)
//  - 버텍스 AO (_VertexAO): 절차 메쉬(HarborKit)가 꼭짓점 색에 구운 틈·밑동 그늘과 블록마다 다른 타설 색을 곱한다 (기본 0 = 안 씀)
// 텍스처 색 × _Color(FBX 원래 색을 넘겨받는다)라서 Blender 소품의 부위별 색은 그대로 두고 결만 입힌다.
Shader "Fishing/Weathered"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _SideTex ("Side Albedo (walls)", 2D) = "white" {}
        [Normal] _SideBump ("Side Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _TexAvg ("Texture Average Color", Color) = (0.5,0.5,0.5,1)
        _SideAvg ("Side Texture Average Color", Color) = (0.5,0.5,0.5,1)
        _Tile ("Meters Per Tile", Float) = 3
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0
        _Desat ("Texture Desaturate", Range(0,1)) = 0.3
        _Contrast ("Texture Contrast", Range(0,1.5)) = 1
        _Wet ("Water Line", Range(0,1)) = 1
        _Streaks ("Rain Streaks", Range(0,1)) = 0.5
        _Moss ("Moss", Range(0,1)) = 0.3
        _MossColor ("Moss Color", Color) = (0.32,0.36,0.2,1)
        _Variation ("Macro Variation", Range(0,1)) = 0.6
        _VertexAO ("Vertex Color AO", Range(0,1)) = 0
        _Grime ("Floor Grime (cracks, stains, puddles)", Range(0,1)) = 0
        _PaletteTex ("Palette (mesh UV, Kenney)", 2D) = "white" {}
        _UsePalette ("Use Palette", Float) = 0
        _PaletteDesat ("Palette Desaturate", Range(0,1)) = 0
        _EmissionMap ("Emission (mesh UV)", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        #pragma multi_compile_local __ _NORMALMAP

        #include "FishingNoise.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _SideTex, _SideBump;
        sampler2D _PaletteTex, _EmissionMap; // 메쉬 UV로 읽는 색 (Kenney 팔레트 텍스처 → 풍화 결을 그 위에 곱한다)
        half _UsePalette, _PaletteDesat;
        fixed4 _EmissionColor; // 세로 면(X·Z 투영)용. Mats.Weathered가 따로 안 주면 윗면과 같은 텍스처를 넣는다
        fixed4 _Color, _MossColor, _TexAvg, _SideAvg;
        half _Contrast;
        half _BumpScale, _Glossiness, _Metallic, _Desat, _Wet, _Streaks, _Moss, _Variation, _VertexAO, _Grime;
        float _Tile;
        float _FishingWetY; // 전역: 맵마다 만조선 높이 (CoastWorldBuilder 등이 넣는다). 0이면 물가가 y=0 근처

        struct Input
        {
            // (이름이 uv로 시작하면 서피스 셰이더가 텍스처 좌표로 여긴다) xy = 메쉬 UV (팔레트·발광), z = 꼭짓점 색 밝기, w = 꼭짓점 색 r - b (누런/푸른 기운). 보간기 10개 한도라 한 칸에 묶는다
            float4 meshAO;
            float3 worldPos;
            float3 wNormal;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.wNormal = UnityObjectToWorldNormal(v.normal);
            o.meshAO = float4(v.texcoord.xy, v.color.g, v.color.r - v.color.b);
            // UV·탄젠트가 없는 메쉬도 법선맵이 깨지지 않게 탄젠트를 법선에서 만든다 (트라이플래너라 방향은 상관없다)
            // surf에서 같은 식으로 월드 탄젠트를 다시 만들므로, 월드에서 정한 뒤 물체 공간으로 되돌려 넣는다 (회전·크기와 무관하게 맞는다)
            float3 n = normalize(o.wNormal);
            float3 t = abs(n.y) < 0.99 ? cross(float3(0, 1, 0), n) : cross(float3(1, 0, 0), n);
            v.tangent = float4(normalize(mul((float3x3)unity_WorldToObject, normalize(t))), unity_WorldTransformParams.w);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float3 nW = normalize(IN.wNormal);
            float3 w = pow(abs(nW), 4);
            w /= (w.x + w.y + w.z);
            float s = 1.0 / max(_Tile, 0.05);
            float2 uvX = p.zy * s, uvY = p.xz * s + 0.37, uvZ = p.xy * s + 0.61;

            // 텍스처 평균 색으로 나눠 결(밝고 어두운 차이)만 남긴다 → _Color가 그대로 칠한 색이 된다
            fixed3 c = (tex2D(_SideTex, uvX).rgb * w.x + tex2D(_SideTex, uvZ).rgb * w.z) / max(_SideAvg.rgb, 0.05)
                     + tex2D(_MainTex, uvY).rgb * w.y / max(_TexAvg.rgb, 0.05);
            float lum0 = dot(c, fixed3(0.299, 0.587, 0.114));
            c = lerp(c, lum0.xxx, _Desat);
            c = lerp(fixed3(1, 1, 1), c, _Contrast); // 결의 세기 (회벽처럼 밝은 면은 약하게 — 너무 세면 지저분한 회색이 된다)
            lum0 = saturate(lum0 * 0.5);

            // 큰 얼룩
            float macro = FishNoise(p.xz * 0.06 + p.y * 0.04) * 0.6 + FishNoise(p.xz * 0.23 + p.yy * 0.2 + 3.3) * 0.4;
            c *= lerp(1.0 - 0.25 * _Variation, 1.0 + 0.12 * _Variation, macro);

            float vertical = 1 - abs(nW.y);
            float along = p.x * 0.7 + p.z * 0.7;

            // 빗물 자국: 가로로 촘촘하고 세로로 길게 늘인 노이즈 → 위에서 흘러내린 줄
            float streak = FishNoise(float2(along * 2.2, p.y * 0.12)) * FishNoise(float2(along * 0.6 + 5.1, p.y * 0.05));
            streak = smoothstep(0.18, 0.55, streak) * vertical * _Streaks;
            c *= 1 - 0.38 * streak;

            // 이끼·먼지: 위를 보는 면 + 노이즈 얼룩
            float mossN = FishNoise(p.xz * 0.9 + p.y * 0.5) * 0.65 + FishNoise(p.xz * 3.1 + p.yy) * 0.35;
            float moss = smoothstep(0.35, 0.8, nW.y + mossN * 0.6 - 0.25) * smoothstep(0.35, 0.7, mossN) * _Moss;
            c = lerp(c, _MossColor.rgb * (0.7 + 0.6 * lum0), moss);

            // 물 자국: 만조선 아래 젖은 띠 + 그 아래 물때·따개비 띠. 경계는 노이즈로 들쭉날쭉
            float edge = (FishNoise(float2(along * 1.3, 0.5)) - 0.5) * 0.5;
            float wet = (1 - smoothstep(_FishingWetY - 0.15, _FishingWetY + 0.35, p.y + edge)) * _Wet;
            float slime = (1 - smoothstep(_FishingWetY - 1.6, _FishingWetY - 0.2, p.y + edge * 2)) * _Wet;
            float barn = smoothstep(0.45, 0.75, FishNoise(p.xz * 6 + p.yy * 6)) * slime;
            fixed3 paint = _Color.rgb;
            if (_UsePalette > 0.5)
            {
                fixed3 pal = tex2D(_PaletteTex, IN.meshAO.xy).rgb;
                paint *= lerp(pal, dot(pal, fixed3(0.299, 0.587, 0.114)).xxx, _PaletteDesat);
            }
            fixed3 colTex = c * paint;
            colTex = lerp(colTex, colTex * 0.55, wet);
            colTex = lerp(colTex, fixed3(0.2, 0.22, 0.15) * (0.6 + lum0), slime * 0.7);
            colTex = lerp(colTex, fixed3(0.62, 0.6, 0.55), barn * 0.6);
            colTex *= lerp(fixed3(1, 1, 1), IN.meshAO.z + float3(0.5, 0, -0.5) * IN.meshAO.w, _VertexAO);

            // 바닥 때: 위를 보는 면만
            float floorUp = smoothstep(0.75, 0.95, nW.y) * _Grime;
            float puddle = 0, stain = 0;
            if (floorUp > 0.001)
            {
                // 갈라진 금: 보로노이 경계(F2 - F1, 칸 ≈ 2m). 좌표를 노이즈로 휘어 곧은 다각형이 안 보이게, 큰 노이즈로 군데군데만,
                // 선 위에서도 잔노이즈로 끊어 한 줄씩 갈라진 것처럼 (빈틈없는 칸 무늬면 보도블록처럼 보였다)
                float2 q = p.xz * 0.5 + (float2(FishNoise(p.xz * 2.3), FishNoise(p.xz * 2.3 + 7.1)) - 0.5) * 0.3;
                float2 ci = floor(q), cf = frac(q);
                float f1 = 8, f2 = 8;
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float2 cell = float2(i, j);
                    float2 r = cell + float2(FishHash(ci + cell), FishHash(ci + cell + 17.3)) - cf;
                    float d = dot(r, r);
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                }
                float crack = (1 - smoothstep(0.0, 0.016, sqrt(f2) - sqrt(f1)))
                            * smoothstep(0.6, 0.78, FishNoise(p.xz * 0.15 + 11.7))
                            * smoothstep(0.35, 0.6, FishNoise(p.xz * 3.7 + 2.2)) * floorUp;
                colTex *= 1 - 0.42 * crack;
                // 기름·물 얼룩 (짙고 살짝 번들)
                stain = smoothstep(0.62, 0.8, FishNoise(p.xz * 0.45 + 3.1) * 0.7 + FishNoise(p.xz * 1.7) * 0.3) * floorUp;
                colTex *= 1 - 0.3 * stain;
                // 얕은 물웅덩이: 수 m 노이즈의 높은 곳, 가장자리 좁게
                puddle = smoothstep(0.7, 0.75, FishNoise(p.xz * 0.12 + 5.5) * 0.75 + FishNoise(p.xz * 0.6) * 0.25) * floorUp;
                colTex = lerp(colTex, colTex * 0.6, puddle);
            }
            o.Albedo = colTex;

        #if defined(_NORMALMAP)
            // 화이트아웃 블렌드 (면 방향마다 읽은 법선을 월드 법선에 얹는다)
            half3 tX = UnpackScaleNormal(tex2D(_SideBump, uvX), _BumpScale);
            half3 tY = UnpackScaleNormal(tex2D(_BumpMap, uvY), _BumpScale);
            half3 tZ = UnpackScaleNormal(tex2D(_SideBump, uvZ), _BumpScale);
            tX = half3(tX.xy + nW.zy, abs(tX.z) * nW.x);
            tY = half3(tY.xy + nW.xz, abs(tY.z) * nW.y);
            tZ = half3(tZ.xy + nW.xy, abs(tZ.z) * nW.z);
            float3 nB = normalize(tX.zyx * w.x + tY.xzy * w.y + tZ.xyz * w.z);
            // 월드 법선 → 탄젠트 공간 (vert에서 만든 탄젠트와 같은 축)
            float3 T = abs(nW.y) < 0.99 ? normalize(cross(float3(0, 1, 0), nW)) : normalize(cross(float3(1, 0, 0), nW));
            float3 B = cross(nW, T);
            o.Normal = normalize(lerp(float3(dot(nB, T), dot(nB, B), dot(nB, nW)), float3(0, 0, 1), puddle));
        #endif
            o.Smoothness = max(saturate(lerp(_Glossiness * lerp(0.7, 1.2, macro), 0.62, wet * 0.8) * (1 - moss * 0.6)), max(puddle * 0.88, stain * 0.45));
            o.Metallic = _Metallic;
            o.Emission = tex2D(_EmissionMap, IN.meshAO.xy).rgb * _EmissionColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
