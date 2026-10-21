// 폭포 물줄기 (듀덴 폭포, 사용자 요청 "폭포에 오프셋 스크롤해서 흐르는 것처럼").
//  - 결 텍스처(MapKit.FallTexture)를 서로 다른 속도·배율로 두 번 내려 흘려 곱한다 → 겹친 물줄기가 따로 미끄러진다
//  - uv2.x = 물줄기 가로 0~1(가장자리 알파가 들쭉날쭉 줄어든다), uv2.y = 0 위(절벽 끝) ~ 1 아래(수면)
//  - 위 끝은 아직 덩어리진 맑은 물(청록 비침), 떨어질수록 공기를 머금어 하얗게, 아래 1/4은 부서져 물보라로 흩어진다
//  - 빛: 반투명 물이라 뒤에서 오는 해도 비친다(역광 투과) + 하늘 반사. 밤엔 둘 다 줄어든다
Shader "Fishing/Waterfall"
{
    Properties
    {
        _MainTex ("Streaks (RGBA)", 2D) = "white" {}
        _Color ("Foam Color", Color) = (0.96,0.98,1,1)
        _WaterColor ("Lip Water Color", Color) = (0.35,0.62,0.6,1)
        _Speed ("Fall Speed (uv/s)", Float) = 0.55
        _Opacity ("Opacity", Range(0,1)) = 0.92
        _EdgeSoft ("Edge Softness", Range(0.02,0.5)) = 0.18
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade vertex:vert
        #pragma target 3.0
        #include "FishingNoise.cginc"

        sampler2D _MainTex;
        fixed4 _Color, _WaterColor;
        half _Speed, _Opacity, _EdgeSoft;

        struct Input
        {
            float2 uv_MainTex;
            float2 fall;      // uv2: 가로 0~1, 세로 0(위)~1(아래)
            float3 worldPos;
            float3 viewDir;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.fall = v.texcoord1.xy;
            // 아래로 갈수록 바람에 살짝 흔들린다
            float sway = sin(_Time.y * 1.7 + v.vertex.y * 0.35) * 0.18 * v.texcoord1.y;
            v.vertex.z += sway;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float t = _Time.y;
            float2 uv = IN.uv_MainTex;
            float down = IN.fall.y;
            // 두 겹: 굵은 줄기는 빠르게, 잔 결은 더 빠르고 촘촘하게 (떨어질수록 빨라진다 = 가속)
            float accel = 1.0 + down * 0.8;
            half4 a = tex2D(_MainTex, float2(uv.x, uv.y * 0.6 + t * _Speed * accel));
            half4 b = tex2D(_MainTex, float2(uv.x * 1.7 + 0.37, uv.y * 1.3 + t * _Speed * 1.6 * accel));
            half streak = saturate(a.a * 0.65 + b.a * 0.55);

            // 가장자리: 노이즈로 들쭉날쭉하게 줄어드는 폭
            float edgeN = FishNoise(float2(IN.fall.y * 6.0 - t * 0.9, IN.fall.x * 3.0)) * 0.5;
            float edge = smoothstep(0.0, _EdgeSoft + edgeN * 0.12, IN.fall.x) * smoothstep(0.0, _EdgeSoft + edgeN * 0.12, 1.0 - IN.fall.x);

            // 아래쪽은 부서져 물보라: 알파가 성기고 둥근 덩어리로
            float spray = smoothstep(0.72, 1.0, down);
            float clumps = FishNoise(float2(uv.x * 9.0, uv.y * 4.0 + t * _Speed * 2.2));
            float bottom = lerp(1.0, smoothstep(0.35, 0.75, clumps), spray);

            // 위 끝(0~12%)은 아직 덩어리진 맑은 물: 청록이 비친다
            float lip = 1.0 - smoothstep(0.0, 0.14, down);
            half3 white = _Color.rgb * (0.86 + 0.14 * b.r);
            half3 col = lerp(white, _WaterColor.rgb, lip * (1.0 - streak * 0.5));

            o.Albedo = col;
            // 역광 투과 + 하늘빛: 물이 해를 등지고도 하얗게 빛난다
            float3 L = normalize(_WorldSpaceLightPos0.xyz);
            float back = pow(saturate(dot(-normalize(IN.viewDir), L)), 3.0);
            half day = saturate(L.y * 4.0 + 0.2);
            o.Emission = col * (0.22 + 0.35 * back) * day + unity_AmbientSky.rgb * 0.25 * col;
            o.Alpha = saturate(streak * 1.25 * edge * bottom) * _Opacity * lerp(0.8, 1.0, 1.0 - lip);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Diffuse"
}
