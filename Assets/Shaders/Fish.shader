// 물고기 (tools/blender/build_fish.py가 만든 어종별 모델).
// 몸과 지느러미가 같은 아틀라스 텍스처를 쓴다: 알베도(A = 지느러미 가장자리), 노멀(비늘), 금속성(R)·매끈함(A).
// 지느러미 머티리얼은 _Cutoff로 해진 가장자리를 잘라 내고, 얇은 막이라 빛이 뒤에서 비치는 느낌(_Translucency)을 더한다.
// 은빛 고기는 시선이 비스듬할수록 비늘이 반짝인다(_Sheen).
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
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex, _BumpMap, _MetalTex;
        fixed4 _Tint;
        half _Sheen, _Translucency, _Wet, _Cutoff;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Tint;
            clip(c.a - _Cutoff); // 지느러미 해진 가장자리 (alphatest: 대신 직접 잘라야 불투명 셰이더로 알파 1이 써진다)
            fixed4 m = tex2D(_MetalTex, IN.uv_MainTex);
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Albedo = c.rgb;
            o.Metallic = m.r;
            o.Smoothness = saturate(m.a + _Wet * 0.3);
            o.Alpha = c.a;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            // 은빛 비늘: 비스듬히 볼 때 하늘빛이 살짝 번진다
            o.Emission = c.rgb * (_Sheen * m.r * pow(rim, 3.0) * 1.5 + _Translucency * 0.35);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
