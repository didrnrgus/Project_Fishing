// 물고기 (tools/blender/build_fish.py가 만든 어종별 모델).
// 몸과 지느러미가 같은 아틀라스 텍스처를 쓴다: 알베도(A = 지느러미 가장자리), 노멀(비늘), 금속성(R)·매끈함(A).
// 지느러미 머티리얼은 _Cutoff로 해진 가장자리를 잘라 내고, 얇은 막이라 빛이 뒤에서 비치는 느낌(_Translucency)을 더한다.
// 은빛 고기는 시선이 비스듬할수록 비늘이 반짝이고(_Sheen), 금속성이 높은 비늘에는 보는 각도에 따라 색이 도는
// 얇은 막 간섭(무지갯빛, _Iridescence)이 은은하게 얹힌다.
// 점액 코팅(_Coat): 비늘(노멀 맵) 위에 덮인 매끈한 점액 막이 따로 빛을 받는다. 비늘마다 조금씩 다른 각도로 반짝이는
// 바탕 반사(Standard)에 더해, 거의 매끈한 막에서 좁고 밝은 하이라이트와 가장자리 반사(프레넬)가 얹혀 젖은 몸처럼 보인다.
// 막의 노멀은 비늘 노멀을 많이 눌러 편 것 (점액이 비늘 틈을 메운다).
// 잘라내기는 alphatest: 지시어 대신 clip()으로 한다. alphatest:를 쓰면 렌더 텍스처에 알파 0이 써져서
// 결과 화면·도감(투명 렌더 텍스처 → UI)에서 물고기가 사라진다.
Shader "Fishing/Fish"
{
    Properties
    {
        _MainTex ("Albedo (A = 지느러미 알파)", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MetalTex ("Metallic (R) Smoothness (A)", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.0
        _Sheen ("Sheen", Range(0,1)) = 0.3
        _Translucency ("Translucency", Range(0,1)) = 0.0
        _Wet ("Wet (물 밖에서 번들거림)", Range(0,1)) = 0.0
        _Iridescence ("Iridescence (은빛 비늘 무지갯빛)", Range(0,1)) = 0.35
        _Coat ("Coat (점액 막 반사)", Range(0,2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Fish fullforwardshadows addshadow
        #pragma target 3.0
        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex, _BumpMap, _MetalTex;
        fixed4 _Tint;
        half _Sheen, _Translucency, _Wet, _Cutoff, _Iridescence, _Coat;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        struct SurfaceOutputFish
        {
            fixed3 Albedo;
            float3 Normal;
            half3 Emission;
            half Metallic;
            half Smoothness;
            half Occlusion;
            fixed Alpha;
            float3 CoatNormal;   // 월드 공간 (점액 막)
            half Coat;
        };

        SurfaceOutputStandard ToStandard(SurfaceOutputFish s)
        {
            SurfaceOutputStandard o;
            o.Albedo = s.Albedo; o.Normal = s.Normal; o.Emission = s.Emission; o.Metallic = s.Metallic;
            o.Smoothness = s.Smoothness; o.Occlusion = s.Occlusion; o.Alpha = s.Alpha;
            return o;
        }

        inline half4 LightingFish(SurfaceOutputFish s, half3 viewDir, UnityGI gi)
        {
            half4 c = LightingStandard(ToStandard(s), viewDir, gi);
            half3 n = normalize(s.CoatNormal);
            half nv = saturate(dot(n, viewDir));
            half fres = 0.04 + 0.96 * pow(1.0 - nv, 5.0);
            // 직사광: 좁고 밝은 점 하이라이트 (블린-퐁, 에너지 보정)
            half3 h = normalize(gi.light.dir + viewDir);
            half nl = saturate(dot(n, gi.light.dir));
            half spec = pow(saturate(dot(n, h)), 220.0) * 9.0 * nl;
            // 주변 반사: 매끈한 막이 하늘·물빛을 비춘다 (가장자리일수록)
            c.rgb += s.Coat * (gi.light.color * spec * (0.06 + fres) + gi.indirect.specular * fres * 0.6);
            return c;
        }

        inline void LightingFish_GI(SurfaceOutputFish s, UnityGIInput data, inout UnityGI gi)
        {
            // 주변 반사는 점액 막(매끈함 0.92)의 노멀로 가져온다 → 바탕 반사(비늘)와 막 반사가 같은 큐브맵을 다른 흐림으로 쓴다
            SurfaceOutputStandard o = ToStandard(s);
            LightingStandard_GI(o, data, gi);
            Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(0.92, data.worldViewDir, s.CoatNormal, half3(0.04, 0.04, 0.04));
            half3 coatEnv = UnityGI_IndirectSpecular(data, 1.0, g);
            gi.indirect.specular = lerp(gi.indirect.specular, coatEnv, 0.5);
        }

        void surf (Input IN, inout SurfaceOutputFish o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Tint;
            clip(c.a - _Cutoff); // 지느러미 해진 가장자리 (alphatest: 대신 직접 잘라야 불투명 셰이더로 알파 1이 써진다)
            fixed4 m = tex2D(_MetalTex, IN.uv_MainTex);
            half3 nTex = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Normal = nTex;
            o.CoatNormal = WorldNormalVector(IN, normalize(lerp(half3(0, 0, 1), nTex, 0.25)));
            o.Coat = _Coat * (0.55 + 0.45 * m.a + _Wet * 0.5);   // 매끈한 곳(눈·점액)일수록, 물 밖에서 젖었을수록
            o.Occlusion = 1.0;
            o.Albedo = c.rgb;
            o.Metallic = m.r;
            o.Smoothness = saturate(m.a + _Wet * 0.3);
            o.Alpha = c.a;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            // 은빛 비늘: 비스듬히 볼 때 하늘빛이 살짝 번진다
            o.Emission = c.rgb * (_Sheen * m.r * pow(rim, 3.0) * 1.5 + _Translucency * 0.35);
            // 얇은 막 간섭: 시선 각도에 따라 파랑 → 보라 → 금빛으로 돈다 (금속성 비늘에만, 가장자리일수록)
            half3 irid = 0.5 + 0.5 * cos(6.2831853 * (rim * 1.4 + half3(0.0, 0.33, 0.67)));
            o.Emission += irid * m.r * rim * rim * _Iridescence * 0.22;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
