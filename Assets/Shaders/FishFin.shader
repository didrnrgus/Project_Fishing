// 물고기 지느러미: 얇은 막이라 반투명(알파 블렌딩)으로 그린다. 예전에는 Fishing/Fish의 잘라내기(clip)라
// 막이 불투명한 판처럼 보이고 가장자리가 구멍 난 종이처럼 해져 보였다.
// - 알파 채널은 따로 섞는다(One, OneMinusSrcAlpha): 결과 화면·도감처럼 투명 렌더 텍스처에 그린 뒤 UI에 얹을 때
//   지느러미 뒤 몸이 비쳐 사라지거나 막이 흐려지지 않게.
// - noforwardadd: 점광원(가로등)은 정점·SH로만 받는다. 추가 패스(Blend One One)는 알파를 무시해 막이 밤에 빛나 보인다.
// - 그림자는 FallBack(잘라내기 VertexLit)의 그림자 패스가 _Cutoff로 그린다.
Shader "Fishing/FishFin"
{
    Properties
    {
        _MainTex ("Albedo (A = 막 두께)", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MetalTex ("Metallic (R) Smoothness (A)", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Shadow Cutoff", Range(0,1)) = 0.5
        _Opacity ("Opacity", Range(0,2)) = 1.0
        _Translucency ("Translucency", Range(0,1)) = 0.6
        _Wet ("Wet", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 300
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard keepalpha noforwardadd
        #pragma target 3.0

        sampler2D _MainTex, _BumpMap, _MetalTex;
        fixed4 _Tint;
        half _Opacity, _Translucency, _Wet;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Tint;
            fixed4 m = tex2D(_MetalTex, IN.uv_MainTex);
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = saturate(m.a + _Wet * 0.3);
            // 비스듬히 볼수록 막이 두껍게 겹쳐 보인다 (프레넬)
            half facing = saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Alpha = saturate(c.a * _Opacity * lerp(1.35, 1.0, facing));
            // 뒤에서 빛이 비치는 얇은 막
            o.Emission = c.rgb * _Translucency * 0.3;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
