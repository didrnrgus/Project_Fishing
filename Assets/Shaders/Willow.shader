// 수양버들 늘어진 가지 (탄천, TancheonWorld.Willows). 가지 한 가닥 = 여러 마디로 나눈 띠 (uv.y 0 매달린 곳 ~ 1 끝).
// Fishing/Grass처럼 판 하나가 통째로 기울면 막대가 달린 것 같다 (사용자 지적) → 끝으로 갈수록 크게(k = s^1.6) 흔들리고,
// 흔들림이 아래로 늦게 전해져(위상 - s) 가지가 채찍처럼 휘며 물결친다. uv2.x = 가지마다 다른 박자, 느린 돌풍이 세기를 바꾼다.
// 메쉬는 월드 좌표로 구워 두므로 정점 좌표가 곧 월드 좌표다.
Shader "Fishing/Willow"
{
    Properties
    {
        _MainTex ("Branches (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.3
        _Wind ("Tip Sway (m)", Float) = 0.45
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Cutoff, _Wind;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            float3 wp = v.vertex.xyz;
            float t = _Time.y;
            float s = v.texcoord.y;
            float ph = v.texcoord1.x;
            float k = pow(s, 1.6);
            float gust = 0.55 + 0.45 * sin(t * 0.27 + wp.x * 0.04 + wp.z * 0.03);
            float a = sin(t * 1.1 + ph - s * 2.2);
            float b = 0.35 * sin(t * 2.4 + ph * 1.7 - s * 3.6);
            float c = 0.25 * sin(t * 0.8 + ph * 0.6 - s * 1.5);
            v.vertex.x += (a + b) * _Wind * gust * k;
            v.vertex.z += (c + b * 0.6) * _Wind * gust * k;
            v.vertex.y += abs(a) * _Wind * 0.12 * gust * k;   // 흔들려 나간 끝은 살짝 들린다 (진자)
            v.normal = float3(0, 1, 0);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            clip(c.a - _Cutoff);
            o.Albedo = c.rgb * IN.color.rgb * _Color.rgb * lerp(0.6, 1.0, saturate(IN.uv_MainTex.y * 1.6));
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack Off
}
