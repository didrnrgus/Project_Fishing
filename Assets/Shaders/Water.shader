// 강물/바다 셰이더.
//  - 버텍스: 방향이 다른 사인파 3개로 잔물결 (원작 영상의 일렁이는 수면)
//  - 픽셀: 절차적 노멀맵 2장을 서로 다른 방향으로 흘려서 잔물결 반짝임
//  - 반사: Standard BRDF + 하늘만 찍는 실시간 반사 프로브 (DayNightCycle)
//  - 밤 불빛 기둥: WaterGlints가 넘긴 광원(가로등·다리 조명·도시 불빛)마다, 잔물결로 흔들린 반사 방향과 광원 방향의 차이를
//    가로(좌우)는 좁게 세로(보는 쪽)는 넓게 받아 물 위에 세로로 길게 늘어진 빛을 그린다
//  - 물가 거품: 맵이 구워 둔 해저 높이 텍스처(_SeabedTex, WaterSurface.SetSeabed)로 수심과 해저 경사를 구해
//    물가까지의 수평 거리를 어림하고, 그 안쪽 몇 m에 파도 따라 밀려오는 흰 거품 띠와 바닥이 비치는 밝은 얕은 물을 그린다.
//    수면 높이는 버텍스 파도를 포함하므로 거품 경계가 파도에 맞춰 들락날락한다 (물때가 오르내리면 거품도 따라간다)
//  - 야광충(_BioGlow, 서천 밤바다): 밤이면 부서지는 파도 끝·거품이 청록 형광으로 빛난다. 새로 부서지는 쪽(파도 앞)이 가장 밝고
//    노이즈로 끊겨 반짝이며, 물가에서 조금 떨어진 바다에도 드문드문 빛 점이 일렁인다
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
        _SeabedTex ("Seabed Height (R, world y)", 2D) = "black" {}
        _SeabedRect ("Seabed Rect (xmin, zmin, 1/w, 1/h)", Vector) = (0,0,0,0)
        _SeabedStep ("Seabed texel (u, v, meters x, meters z)", Vector) = (0,0,1,1)
        _HasSeabed ("Has Seabed", Float) = 0
        _FoamWidth ("Foam Width (m from waterline)", Float) = 4.5
        _FoamColor ("Foam Color", Color) = (0.92,0.95,0.96,1)
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
        float _GlintCount, _GlintNight, _BioGlow;
        sampler2D _SeabedTex;
        float4 _SeabedRect, _SeabedStep, _FoamColor;
        float _HasSeabed, _FoamWidth;

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

            // 얕은 물과 물가 거품
            if (_HasSeabed > 0.5)
            {
                float2 uv = (p - _SeabedRect.xy) * _SeabedRect.zw;
                if (uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1)
                {
                    float bed = tex2D(_SeabedTex, uv).r;
                    float depth = IN.worldPos.y - bed;
                    // 해저 경사 (m/m) → 물가까지 수평 거리 어림. 벽(호안·방파제)은 경사가 커서 띠가 얇다
                    float bx = (tex2D(_SeabedTex, uv + float2(_SeabedStep.x, 0)).r - tex2D(_SeabedTex, uv - float2(_SeabedStep.x, 0)).r) / (2 * _SeabedStep.z);
                    float bz = (tex2D(_SeabedTex, uv + float2(0, _SeabedStep.y)).r - tex2D(_SeabedTex, uv - float2(0, _SeabedStep.y)).r) / (2 * _SeabedStep.w);
                    float slope = max(length(float2(bx, bz)), 0.03);
                    float dist = max(depth, 0) / slope;

                    // 바닥이 비치는 얕은 물: 수심 2.5m보다 얕으면 밝고 맑게
                    float shallow = 1 - saturate(depth / 2.5);
                    o.Albedo = lerp(o.Albedo, o.Albedo * 1.3 + float3(0.07, 0.08, 0.06), shallow * shallow * 0.75);

                    // 거품: 물가 쪽으로 밀려오는 띠 + 노이즈로 끊긴 가장자리
                    float t = _Time.y;
                    float n1 = tex2D(_NormalTex, p * 0.21 + t * float2(0.021, 0.013)).r;
                    float n2 = tex2D(_NormalTex, p * 0.57 - t * float2(0.017, 0.024)).g;
                    float noise = saturate(n1 * 0.6 + n2 * 0.4);
                    float edge = saturate(1 - dist / _FoamWidth);
                    float lap = sin(dist * 2.1 + t * 1.7 + noise * 7.0) * 0.5 + 0.5;
                    float foam = saturate(edge * edge * 1.5 + edge * lap * 0.75);
                    foam *= smoothstep(0.32, 0.62, noise + edge * 0.45);
                    foam = saturate(foam + step(depth, 0.05) * 0.9);   // 물이 땅에 닿는 선은 늘 하얗게
                    o.Albedo = lerp(o.Albedo, _FoamColor.rgb, foam);
                    o.Smoothness = lerp(o.Smoothness, 0.35, foam);
                    o.Normal = normalize(lerp(n, float3(0, 0, 1), foam * 0.8));

                    // 야광충: 부서지는 파도 앞(lap가 막 차오르는 곳)이 가장 밝고, 노이즈 알갱이로 반짝인다
                    float bio = _BioGlow * _GlintNight;
                    if (bio > 0.01)
                    {
                        float crest = smoothstep(0.55, 0.95, lap) * edge;
                        float spark = tex2D(_NormalTex, p * 1.9 + t * float2(0.05, -0.04)).b;
                        float glowFoam = saturate(foam * 0.55 + crest * 1.3) * (0.45 + 0.9 * smoothstep(0.45, 0.8, spark));
                        // 물가에서 조금 떨어진 곳의 드문 빛 점
                        float dots = smoothstep(0.86, 0.97, spark) * saturate(1 - dist / (_FoamWidth * 4)) * (0.6 + 0.4 * sin(t * 3 + n1 * 20));
                        o.Albedo = lerp(o.Albedo, o.Albedo * 0.25, bio);   // 밤엔 흰 거품이 아니라 빛만 보이게
                        o.Emission += float3(0.1, 0.78, 1.0) * (glowFoam * 2.2 + dots * 0.9) * bio;
                    }
                }
            }

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
                o.Emission += glow * _GlintNight * 0.9;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
