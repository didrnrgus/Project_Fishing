// 강물/바다 셰이더.
//  - 버텍스: 방향이 다른 사인파 3개로 잔물결 (원작 영상의 일렁이는 수면)
//  - 픽셀: 절차적 노멀맵 2장을 서로 다른 방향으로 흘려서 잔물결 반짝임
//  - 반사: Standard BRDF + 하늘만 찍는 실시간 반사 프로브 (DayNightCycle)
//  - 밤 불빛 기둥: WaterGlints가 넘긴 광원(가로등·다리 조명·도시 불빛)마다, 잔물결로 흔들린 반사 방향과 광원 방향의 차이를
//    가로(좌우)는 좁게 세로(보는 쪽)는 넓게 받아 물 위에 세로로 길게 늘어진 빛을 그린다
//  - 물가 거품: 맵이 구워 둔 해저 높이 텍스처(_SeabedTex, WaterSurface.SetSeabed)로 수심과 해저 경사를 구해
//    물가까지의 수평 거리를 어림하고, 그 안쪽 몇 m에 파도 따라 밀려오는 흰 거품 띠와 바닥이 비치는 밝은 얕은 물을 그린다.
//    수면 높이는 버텍스 파도를 포함하므로 거품 경계가 파도에 맞춰 들락날락한다 (물때가 오르내리면 거품도 따라간다)
//  - 야광충(_BioGlow, 서천·삼척 밤바다): 밤이면 부서지는 파도 앞이 전기 파랑으로 확 켜지고(가운데는 흰빛) 바다 쪽으로 꼬리를 끌며 꺼진다.
//    거품·물가 모래 선에는 빛 알갱이가 저마다 깜빡이고, 물가에서 조금 떨어진 바다에도 드문 빛 점이 있다.
//    찌·물보라·발걸음·배가 건드린 물(_BioTouch, BioluminescenceFx.cs)은 고리로 퍼지며 잠깐 빛난다
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
        #include "FishingNoise.cginc"

        sampler2D _NormalTex;
        fixed4 _DeepColor, _ShallowColor;
        half _NormalScale, _Tiling1, _Tiling2, _WaveAmp, _WaveLen, _Smoothness;
        float4 _Speed1, _Speed2;
        float4 _GlintPos[48];
        float4 _GlintCol[48];
        float _GlintCount, _GlintNight, _BioGlow;
        float4 _BioTouch[8];
        float _BioTouchCount;
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

        // ---- 야광충 ----
        // 파도 앞 위상: 물가까지 거리 dist와 시간으로 물가 쪽으로 밀려오고, 사인 두 개로 앞줄이 굽는다.
        // BioluminescenceFx.cs의 FrontGlow와 같은 식이어야 파티클이 빛나는 파도 앞에서 튄다
        float BioSaw(float2 p, float dist, float t)
        {
            float wob = 2.2 * sin(p.x * 0.31 + p.y * 0.13 + t * 0.21) + 1.4 * sin(p.y * 0.41 - p.x * 0.53 - t * 0.33);
            return frac((dist * 1.4 + t * 1.1 + wob) / 6.2831);
        }

        // 파도 앞이 군데군데만 부서진다 (10m쯤 되는 덩어리가 천천히 옮겨 간다). 0~1
        float BioPatch(float2 p, float t)
        {
            return smoothstep(0.3, 0.72, FishNoise(p * 0.11 + float2(t * 0.04, -t * 0.025)));
        }

        // 막 지나간 파도 앞(saw 0)이 가장 밝고 바다 쪽(saw 1)으로 꼬리를 끌며 꺼진다. 앞쪽 끝은 몇 cm 부드럽게.
        // 꼬리는 부서진 물처럼 얼룩진다 (churn). C#도 같은 식 (churn 빼고)
        float BioFront(float2 p, float dist, float t, float churn)
        {
            float s = BioSaw(p, dist, t);
            float k = 1 - s;
            float tail = k * k * k;
            return smoothstep(0.0, 0.03, s) * (pow(k, 18) * 0.9 + tail * (0.25 + churn));
        }

        // 칸마다 하나씩 흩어진 빛 알갱이가 저마다의 박자로 깜빡인다 (density = 1m에 몇 칸)
        float BioGrains(float2 p, float density, float t)
        {
            float2 q = p * density;
            float2 cell = floor(q);
            float h = FishHash(cell);
            float2 c = float2(FishHash(cell + 31.7), FishHash(cell + 71.3)) * 0.7 + 0.15;
            float d = length(frac(q) - c);
            float blink = pow(saturate(sin(t * (1.3 + h * 2.4) + h * 40.0)), 6);
            return saturate(1 - d * 6.5) * blink * step(0.45, h);
        }

        // 파도 앞 한가운데는 흰빛이 도는 하늘색, 꼬리는 짙은 전기 파랑 (실제 야광충 사진)
        float3 BioColor(float w)
        {
            return lerp(float3(0.02, 0.32, 1.0), float3(0.55, 0.95, 1.0), saturate(w * w * 1.2));
        }

        // 건드린 물(찌·물보라·발걸음·배)이 잠깐 퍼지며 빛난다. BioluminescenceFx.cs가 위치·반경·세기를 넘긴다
        float BioTouch(float2 p, float t)
        {
            float g = 0;
            int count = (int)_BioTouchCount;
            for (int i = 0; i < 8; i++)
            {
                if (i >= count) break;
                float4 b = _BioTouch[i];   // x, z, 반경, 세기
                float r = length(p - b.xy) / max(b.z, 0.05);
                float e = (r - 0.75) * 3.2;
                float ring = exp(-e * e) * 0.7 + exp(-r * r * 2.2) * 0.5;   // 퍼지는 고리 + 가운데 남은 빛
                g += ring * b.w;
            }
            return g;
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

                    // 야광충: 부서지는 파도 앞이 날카롭게 켜지고 바다 쪽으로 꼬리를 끌며 꺼진다. 거품에도 옅게 남고, 물가 모래 선은 반짝인다
                    float bio = _BioGlow * _GlintNight;
                    if (bio > 0.01)
                    {
                        // 부서진 물 얼룩: 물결 노멀(fbm 기울기)이라 값 노이즈처럼 네모 칸이 보이지 않는다
                        float c1 = tex2D(_NormalTex, p * 0.33 + t * float2(0.045, -0.03)).r;
                        float c2 = tex2D(_NormalTex, p * 0.71 - t * float2(0.03, 0.05)).g;
                        float churn = smoothstep(0.42, 0.68, c1 * 0.6 + c2 * 0.4) * 0.9;
                        // 벽(호안·방파제) 앞 깊은 물은 몇십 cm 사이에 dist가 0→십몇 m로 뛰어 벽을 따라 날카로운 띠가 생겼다 (2026-10-27 사용자: "인위적").
                        // 벽 가까움(prox): 둘레 16방향 × 2~12m 해저를 살펴 물 위로 나온 땅의 몫을 거리 가중으로 더한다 → 벽에서 10m 남짓까지 부드럽게 옅어진다.
                        // 샘플 반지름을 픽셀마다 흔들어 고리 무늬를 지우고, 그 가까움으로 가짜 거리를 만들어 파도 앞이 벽 쪽으로 밀려오게 한다
                        float wallK = smoothstep(0.8, 2.0, depth);
                        float prox = 0;
                        if (wallK > 0.01)
                        {
                            float jit = 0.7 + 0.6 * FishNoise(p * 0.9);   // 연속 노이즈 (칸 해시는 네모 무늬가 보였다)
                            float wsum = 0;
                            for (int k = 0; k < 16; k++)
                            {
                                float a = k * 0.3927 + jit;
                                float2 dir = float2(cos(a), sin(a));
                                float r = (2.0 + (float)(k & 3) * 3.2) * jit;      // 2 ~ 12m
                                float2 uv2 = uv + dir * r * _SeabedRect.zw;
                                float land = smoothstep(IN.worldPos.y - 1.2, IN.worldPos.y + 0.3, tex2Dlod(_SeabedTex, float4(uv2, 0, 0)).r);
                                float w = 1.0 / (0.6 + r * 0.25);
                                prox += land * w;
                                wsum += w;
                            }
                            prox = smoothstep(0.0, 0.42, prox / wsum);
                            // 큰 덩어리 노이즈로 경계를 들쭉날쭉하게
                            prox *= 0.55 + 0.45 * FishNoise(p * 0.08 + t * 0.02);
                        }
                        float pseudo = (1 - prox) * _FoamWidth * 3.0;
                        float front = BioFront(p, lerp(dist, pseudo, wallK), t, churn);
                        // 벽 앞은 날카로운 파도 앞 대신 넓고 완만하게 밀려오는 빛 (덩어리 가장자리가 또렷하지 않게)
                        float saw = BioSaw(p, pseudo, t);
                        float soft = pow(saturate(1 - saw), 2.0) * (0.7 + churn * 0.4);
                        front = lerp(front, soft, wallK * 0.8);
                        // 흰 하이라이트: 밀려오는 빛의 맨 앞줄만 가늘게, 부서진 물 얼룩(churn)·덩어리 노이즈로 군데군데 끊긴 흰빛 물마루
                        float crest = smoothstep(0.0, 0.03, saw) * pow(saturate(1 - saw), 22) * smoothstep(0.2, 0.75, churn + FishNoise(p * 0.35 + t * 0.1) * 0.6);
                        float zone = lerp(smoothstep(_FoamWidth * 4.0, _FoamWidth * 0.3, dist), prox, wallK);
                        float wave = front * zone * BioPatch(p, t);
                        float grains = BioGrains(p, 3.2, t) * saturate(foam * 0.4 + zone * 0.5) + BioGrains(p * 0.61 + 17.3, 2.4, t) * step(depth, 0.35) * 0.6;
                        // 물가에서 조금 떨어진 곳의 드문 빛 점 (벽 앞도 가까움만큼)
                        float dots = BioGrains(p * 0.37 + 5.1, 1.6, t * 0.6) * max(saturate(1 - dist / (_FoamWidth * 6)) * (1 - wallK), prox);
                        o.Albedo = lerp(o.Albedo, o.Albedo * 0.2, bio);   // 밤엔 흰 거품이 아니라 빛만 보이게
                        float3 c = BioColor(wave);
                        // 넓게 깔린 푸른 기운 + 밀려오는 빛(경계 없이) + 맨 앞줄 흰 하이라이트
                        float haze = max(zone, prox) * 0.16;
                        o.Emission += (c * wave * 2.8 + float3(0.05, 0.42, 1.0) * (haze + foam * 0.06 + grains * 2.2 + dots * 1.3)
                                       + float3(0.78, 0.94, 1.0) * crest * zone * BioPatch(p, t) * 3.5) * bio;
                    }
                }
            }

            // 건드린 물이 퍼지며 빛난다 (야광충 맵의 밤, 물가에서 멀어도)
            float bioTouch = _BioGlow * _GlintNight;
            if (bioTouch > 0.01 && _BioTouchCount > 0.5)
            {
                float touch = BioTouch(p, _Time.y);
                float tg = BioGrains(p, 4.5, _Time.y * 1.7) + BioGrains(p * 1.37 + 9.1, 4.5, _Time.y * 2.1);
                o.Emission += float3(0.04, 0.45, 1.0) * touch * (0.45 + tg * 3.0) * bioTouch;
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
