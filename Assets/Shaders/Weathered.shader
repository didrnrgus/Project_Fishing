// 풍화 표면 셰이더 (Standard 조명): 호안·방파제·테트라포드·석상·회벽 집·바위 봉우리.
//  - 트라이플래너: 월드 좌표로 세 방향에서 텍스처를 읽어 면 방향으로 섞는다. 그래서 UV가 0~1뿐인 상자 프리미티브나 UV 없는 Blender 소품도
//    늘어나지 않고 _Tile(m)마다 한 장씩 고르게 깔린다. 정적 배칭(월드 좌표로 굳힌 메쉬)해도 결과가 같다.
//  - 물 자국: 전역 _FishingWetY(맵 만조선) 아래는 젖어서 어둡고 반들거리며, 그 아래 띠는 따개비·물때로 녹갈색 (_Wet)
//  - 빗물 자국: 세로 면에 위에서 아래로 흘러내린 거뭇한 줄 (_Streaks)
//  - 이끼·먼지: 위를 보는 면과 틈에 _MossColor 얼룩 (_Moss)
//  - 큰 얼룩: Ground 셰이더처럼 수 m 단위 밝기 흔들림으로 반복 무늬를 감춘다
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
        half _BumpScale, _Glossiness, _Metallic, _Desat, _Wet, _Streaks, _Moss, _Variation;
        float _Tile;
        float _FishingWetY; // 전역: 맵마다 만조선 높이 (CoastWorldBuilder 등이 넣는다). 0이면 물가가 y=0 근처

        struct Input
        {
            float2 uv_PaletteTex;
            float3 worldPos;
            float3 wNormal;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.wNormal = UnityObjectToWorldNormal(v.normal);
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
                fixed3 pal = tex2D(_PaletteTex, IN.uv_PaletteTex).rgb;
                paint *= lerp(pal, dot(pal, fixed3(0.299, 0.587, 0.114)).xxx, _PaletteDesat);
            }
            fixed3 colTex = c * paint;
            colTex = lerp(colTex, colTex * 0.55, wet);
            colTex = lerp(colTex, fixed3(0.2, 0.22, 0.15) * (0.6 + lum0), slime * 0.7);
            colTex = lerp(colTex, fixed3(0.62, 0.6, 0.55), barn * 0.6);
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
            o.Normal = normalize(float3(dot(nB, T), dot(nB, B), dot(nB, nW)));
        #endif
            o.Smoothness = saturate(lerp(_Glossiness * lerp(0.7, 1.2, macro), 0.62, wet * 0.8) * (1 - moss * 0.6));
            o.Metallic = _Metallic;
            o.Emission = tex2D(_EmissionMap, IN.uv_PaletteTex).rgb * _EmissionColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
