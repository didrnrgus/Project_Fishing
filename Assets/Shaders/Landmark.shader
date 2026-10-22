// 멀리 보이는 랜드마크(롯데월드타워·63빌딩·남산타워) 셰이더 (Standard 조명).
//  - 안개를 맵 안개의 _FogScale배만 받는다: 수백 m 밖이라 맵 안개(지수 0.0028)를 다 받으면 회색 덩어리가 된다. 대신 옅게 뿌옇게
//  - 밤 창문 불빛: _EmissionMap(창마다 켜짐/꺼짐, _NightTiles장 아틀라스) × _EmissionColor × 전역 _FishingNight (DayNightCycle이 0 낮 ~ 1 밤으로 넣는다)
//  - _Cutoff > 0이면 알파로 구멍을 뚫는다 (롯데월드타워 꼭대기 다이아그리드 격자, 양면)
Shader "Fishing/Landmark"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.6
        _Metallic ("Metallic", Range(0,1)) = 0.2
        _EmissionMap ("Night Windows", 2D) = "black" {}
        _EmissionColor ("Night Color", Color) = (1,0.85,0.6,1)
        _NightTiles ("Night Atlas Tiles", Float) = 1
        _DayEmission ("Day Emission", Range(0,1)) = 0
        _FogScale ("Fog Scale", Range(0,1)) = 0.35
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _DetailTex ("World Detail (triplanar)", 2D) = "gray" {}
        _DetailAvg ("Detail Average", Color) = (0.5,0.5,0.5,1)
        _DetailTile ("Detail Meters Per Tile", Float) = 20
        _DetailStrength ("Detail Strength", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull [_Cull]

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow nofog finalcolor:landmarkFog vertex:vert
        #pragma target 3.0

        sampler2D _MainTex, _EmissionMap, _DetailTex;
        fixed4 _DetailAvg;
        float _DetailTile, _NightTiles;
        half _DetailStrength;
        fixed4 _Color, _EmissionColor;
        half _Glossiness, _Metallic, _DayEmission, _FogScale, _Cutoff;
        float _FishingNight;

        struct Input
        {
            float2 uv_MainTex;
            float fogDist;
            float3 wPos;
            float3 wNrm;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.fogDist = length(UnityObjectToViewPos(v.vertex));
            o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.wNrm = UnityObjectToWorldNormal(v.normal);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            if (_Cutoff > 0) clip(c.a - _Cutoff);
            // 먼 산·절벽: 월드 좌표 트라이플래너 바위 결 (두 배율을 겹쳐 수백 m 산에서도 반복이 덜 보인다)
            if (_DetailStrength > 0)
            {
                float3 w = pow(abs(normalize(IN.wNrm)), 4);
                w /= (w.x + w.y + w.z);
                float3 p = IN.wPos / _DetailTile;
                fixed3 d = tex2D(_DetailTex, p.zy).rgb * w.x + tex2D(_DetailTex, p.xz).rgb * w.y + tex2D(_DetailTex, p.xy).rgb * w.z;
                fixed3 d2 = tex2D(_DetailTex, p.zy * 0.23).rgb * w.x + tex2D(_DetailTex, p.xz * 0.23).rgb * w.y + tex2D(_DetailTex, p.xy * 0.23).rgb * w.z;
                fixed3 k = (d * 0.55 + d2 * 0.45) / max(_DetailAvg.rgb, 0.05);
                k = lerp(dot(k, fixed3(0.3, 0.59, 0.11)).xxx, k, 0.35);
                c.rgb *= lerp(fixed3(1, 1, 1), k, _DetailStrength);
            }
            o.Albedo = c.rgb;
            o.Smoothness = _Glossiness * c.a;
            o.Metallic = _Metallic;
            // 밤 불빛 아틀라스는 낮 텍스처 _NightTiles × _NightTiles장을 한 장에 담아(창마다 다름) 낮 무늬가 반복돼도 불빛은 반복되지 않는다
            fixed3 win = tex2D(_EmissionMap, IN.uv_MainTex / _NightTiles).rgb;
            o.Emission = win * _EmissionColor.rgb * max(saturate(_FishingNight), _DayEmission);
        }

        void landmarkFog(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            // 지수 안개(맵 설정)를 거리 × _FogScale로 계산. 추가 조명 패스(forward add)는 안개색 대신 검정으로 섞는다
            float f = saturate(exp2(-unity_FogParams.y * IN.fogDist * _FogScale)); // = exp(-밀도 × 거리 × 배율)
        #ifdef UNITY_PASS_FORWARDADD
            color.rgb = lerp(fixed3(0, 0, 0), color.rgb, f);
        #else
            color.rgb = lerp(unity_FogColor.rgb, color.rgb, f);
        #endif
        }
        ENDCG
    }
    FallBack "Diffuse"
}
