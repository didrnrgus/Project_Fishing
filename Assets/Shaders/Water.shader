// 강물/바다 셰이더.
//  - 버텍스: 방향이 다른 사인파 3개로 잔물결 (원작 영상의 일렁이는 수면)
//  - 픽셀: 절차적 노멀맵 2장을 서로 다른 방향으로 흘려서 잔물결 반짝임
//  - 반사: Standard BRDF + 하늘만 찍는 실시간 반사 프로브 (DayNightCycle)
//  - 밤 불빛 기둥: WaterGlints가 넘긴 광원(가로등·다리 조명·도시 불빛)마다, 잔물결로 흔들린 반사 방향과 광원 방향의 차이를
//    가로(좌우)는 좁게 세로(보는 쪽)는 넓게 받아 물 위에 세로로 길게 늘어진 빛을 그린다
Shader "Fishing/Water"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.05,0.15,0.17,1)
        _ShallowColor ("Shallow Color", Color) = (0.12,0.3,0.3,1)
        _NormalTex ("Normal (RGB, linear)", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Range(0,2)) = 0.6
        _Tiling1 ("Tiling 1", Float) = 0.06
        _Tiling2 ("Tiling 2", Float) = 0.17
        _Speed1 ("Speed 1", Vector) = (0.015, 0.008, 0, 0)
        _Speed2 ("Speed 2", Vector) = (-0.01, 0.02, 0, 0)
        _WaveAmp ("Wave Amplitude", Float) = 0.05
        _WaveLen ("Wave Length", Float) = 9
        _Smoothness ("Smoothness", Range(0,1)) = 0.93
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma target 3.0

        sampler2D _NormalTex;
        fixed4 _DeepColor, _ShallowColor;
        half _NormalScale, _Tiling1, _Tiling2, _WaveAmp, _WaveLen, _Smoothness;
        float4 _Speed1, _Speed2;
        float4 _GlintPos[48];
        float4 _GlintCol[48];
        float _GlintCount, _GlintNight;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        float Wave(float2 p, float2 dir, float len, float speed)
        {
            return sin(dot(p, dir) * 6.2831 / len + _Time.y * speed);
        }

        void vert(inout appdata_full v)
        {
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float h = Wave(wp.xz, float2(0.8, 0.6), _WaveLen, 1.3)
                    + 0.6 * Wave(wp.xz, float2(-0.4, 0.92), _WaveLen * 0.63, 1.7)
                    + 0.35 * Wave(wp.xz, float2(0.95, -0.3), _WaveLen * 0.37, 2.3);
            v.vertex.y += h * _WaveAmp;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 p = IN.worldPos.xz;
            float3 n1 = tex2D(_NormalTex, p * _Tiling1 + _Time.y * _Speed1.xy).rgb * 2 - 1;
            float3 n2 = tex2D(_NormalTex, p * _Tiling2 + _Time.y * _Speed2.xy).rgb * 2 - 1;
            float3 n = normalize(float3((n1.xy + n2.xy) * _NormalScale, 1));
            o.Normal = n;

            // 비스듬히 볼수록 깊은 색, 내려다볼수록 얕은 색
            float facing = saturate(dot(normalize(IN.viewDir), n));
            o.Albedo = lerp(_DeepColor.rgb, _ShallowColor.rgb, facing * facing);
            o.Metallic = 0;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;

            // 밤 불빛 기둥
            if (_GlintNight > 0.01)
            {
                float3 V = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float3 Nw = normalize(float3(n.x, n.z, n.y));          // 평평한 수면의 탄젠트 공간 → 월드 (T=+x, B=+z, N=+y)
                float3 R = reflect(-V, Nw);
                float3 right = normalize(cross(float3(0, 1, 0), V));
                float3 glow = 0;
                int count = (int)_GlintCount;
                for (int i = 0; i < 48; i++)
                {
                    if (i >= count) break;
                    float3 toL = _GlintPos[i].xyz - IN.worldPos;
                    float dist = length(toL);
                    float3 d = R - toL / dist;
                    float dx = dot(d, right);
                    float dy = d.y;
                    float g = exp(-(dx * dx * 5000.0 + dy * dy * 70.0));
                    glow += _GlintCol[i].rgb * (_GlintCol[i].a * g / (1.0 + dist * 0.004));
                }
                o.Emission = glow * _GlintNight * 0.9;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
