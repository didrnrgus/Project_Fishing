// 멀리 보이는 랜드마크(롯데월드타워·63빌딩·남산타워) 셰이더 (Standard 조명).
//  - 안개를 맵 안개의 _FogScale배만 받는다: 수백 m 밖이라 맵 안개(지수 0.0028)를 다 받으면 회색 덩어리가 된다. 대신 옅게 뿌옇게
//  - 밤 창문 불빛: _EmissionMap(창마다 켜짐/꺼짐) × _EmissionColor × 전역 _FishingNight (DayNightCycle이 0 낮 ~ 1 밤으로 넣는다)
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
        _DayEmission ("Day Emission", Range(0,1)) = 0
        _FogScale ("Fog Scale", Range(0,1)) = 0.35
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull [_Cull]

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow nofog finalcolor:landmarkFog vertex:vert
        #pragma target 3.0

        sampler2D _MainTex, _EmissionMap;
        fixed4 _Color, _EmissionColor;
        half _Glossiness, _Metallic, _DayEmission, _FogScale, _Cutoff;
        float _FishingNight;

        struct Input
        {
            float2 uv_MainTex;
            float fogDist;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.fogDist = length(UnityObjectToViewPos(v.vertex));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            if (_Cutoff > 0) clip(c.a - _Cutoff);
            o.Albedo = c.rgb;
            o.Smoothness = _Glossiness * c.a;
            o.Metallic = _Metallic;
            fixed3 win = tex2D(_EmissionMap, IN.uv_MainTex).rgb;
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
