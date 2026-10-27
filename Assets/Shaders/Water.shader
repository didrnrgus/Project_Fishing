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
        _SeabedTex ("Seabed Height (R, world y) + Breakwater (G) + Shore Proximity (B)", 2D) = "black" {}
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
        // 사용자 참고 사진(2026-10-27): 빛은 물가를 따라 겹겹이 밀려오는 가는 레이스 같은 파도 앞줄에 몰리고(맨 앞이 가장 밝다),
        // 줄 뒤(바다 쪽)는 모래알처럼 자글자글한 빛 점이 깔렸다가 옅어진다. 바위·벽 앞은 부서진 물이 구름처럼 뿌옇게 번진다.
        // 파도 앞 위상: 물가까지 거리 dist와 시간으로 물가 쪽으로 밀려오고, 사인 둘 + 값 노이즈 둘(레이스처럼 구불구불)로 앞줄이 굽는다.
        // BioluminescenceFx.cs의 FrontGlow와 같은 식이어야 파티클이 빛나는 파도 앞에서 튄다
        float BioSaw(float2 p, float dist, float t)
        {
            float wob = 2.2 * sin(p.x * 0.31 + p.y * 0.13 + t * 0.21) + 1.4 * sin(p.y * 0.41 - p.x * 0.53 - t * 0.33)
                      + (FishNoise(p * 0.45) - 0.5) * 2.6 + (FishNoise(p * 1.7 + 7.0) - 0.5) * 0.8;
            return frac((dist * 1.4 + t * 1.1 + wob) / 6.2831);
        }

        // 파도 앞 밝기가 군데군데 다르다 (10m쯤 되는 덩어리가 천천히 옮겨 간다). 꺼지지는 않는다 (사진은 앞줄이 길게 이어진다). 0.35~1
        float BioPatch(float2 p, float t)
        {
            return 0.35 + 0.65 * smoothstep(0.25, 0.75, FishNoise(p * 0.11 + float2(t * 0.04, -t * 0.025)));
        }

        // 자글자글한 빛 점 (사진: 모래알처럼 빽빽한 반짝임). 멀어서 한 픽셀에 여러 점이 들면 평균값으로 (반짝이는 계단 무늬 없이). 0~1
        float BioSpeckle(float2 p, float t, float fw)
        {
            float a = FishNoise(p * 7.0 + float2(t * 0.35, 0));
            float b = FishNoise(p * 15.0 + float2(3.1, -t * 0.5));
            float sp = saturate((a * 0.5 + b * 0.5 - 0.44) * 3.4);

            return lerp(sp, 0.38, saturate(fw * 9.0 - 0.25));
        }

        // 한 파도: 맨 앞(saw 0) 가는 밝은 줄 + 바로 뒤 겹줄(앞 파도가 남긴 레이스) + 바다 쪽으로 옅어지는 자글자글한 빛 판.
        // 앞쪽 끝은 1cm 남짓 부드럽게. C#도 같은 식 (speck = 평균 0.38)
        float BioFront(float2 p, float dist, float t, float speck)
        {
            float s = BioSaw(p, dist, t);
            float k = 1 - s;
            float on = smoothstep(0.0, 0.012, s);
            float edge = pow(k, 45) * (0.75 + 0.25 * speck);
            float d2 = (s - 0.22) / 0.03;
            float lace = exp(-d2 * d2) * 0.4 * smoothstep(0.35, 0.7, FishNoise(p * 0.6 + 11.0));
            float sheet = pow(k, 2.5) * (0.1 + speck * 0.9) * 0.6;
            return on * (edge + sheet) + lace * (0.5 + speck * 0.5);
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
            float pixFw = length(fwidth(p));   // 한 픽셀이 덮는 물 넓이 (자글자글한 빛 점을 멀면 평균으로) — 분기 밖에서 구한다
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
                    float2 sb = tex2D(_SeabedTex, uv).rg;
                    float bed = sb.r;
                    float depth = IN.worldPos.y - bed;
                    // 방파제 둘레 (G = 방파제 자리): 1m 칸 해저가 벽 둘레 1~2m를 얕은 비탈처럼 만들어 밝은 물·흰 거품 띠가 벽과 물 사이에 끼었다
                    // (2026-10-27 사용자: "방파제가 벽에서 떨어져 보인다"). 가운데·네 이웃 칸 중 최댓값으로 2m 안을 벽 앞으로 본다
                    float built = max(sb.g, max(max(tex2D(_SeabedTex, uv + float2(_SeabedStep.x, 0)).g, tex2D(_SeabedTex, uv - float2(_SeabedStep.x, 0)).g),
                                                max(tex2D(_SeabedTex, uv + float2(0, _SeabedStep.y)).g, tex2D(_SeabedTex, uv - float2(0, _SeabedStep.y)).g)));
                    built = smoothstep(0.02, 0.3, built);
                    // 해저 경사 (m/m) → 물가까지 수평 거리 어림. 벽(호안·방파제)은 경사가 커서 띠가 얇다
                    float bx = (tex2D(_SeabedTex, uv + float2(_SeabedStep.x, 0)).r - tex2D(_SeabedTex, uv - float2(_SeabedStep.x, 0)).r) / (2 * _SeabedStep.z);
                    float bz = (tex2D(_SeabedTex, uv + float2(0, _SeabedStep.y)).r - tex2D(_SeabedTex, uv - float2(0, _SeabedStep.y)).r) / (2 * _SeabedStep.w);
                    float slope = max(length(float2(bx, bz)), 0.03);
                    float dist = max(depth, 0) / slope;

                    // 바닥이 비치는 얕은 물: 수심 2.5m보다 얕으면 밝고 맑게
                    float shallow = (1 - saturate(depth / 2.5)) * (1 - built);
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
                    foam *= 1 - built * 0.92;                            // 방파제 앞은 깊은 물이 벽에 바로 닿는다
                    o.Albedo = lerp(o.Albedo, _FoamColor.rgb, foam);
                    o.Smoothness = lerp(o.Smoothness, 0.35, foam);
                    o.Normal = normalize(lerp(n, float3(0, 0, 1), foam * 0.8));

                    // 야광충: 부서지는 파도 앞이 날카롭게 켜지고 바다 쪽으로 꼬리를 끌며 꺼진다. 거품에도 옅게 남고, 물가 모래 선은 반짝인다
                    float bio = _BioGlow * _GlintNight;
                    if (bio > 0.01)
                    {
                        float speck = BioSpeckle(p, t, pixFw);
                        // 벽(호안·방파제) 앞 깊은 물은 몇십 cm 사이에 dist가 0→십몇 m로 뛰어 벽을 따라 날카로운 띠가 생겼다 (2026-10-27 사용자: "인위적").
                        // 그래서 벽 앞은 벽 가까움(prox)으로 가짜 거리를 만들어 파도 앞이 벽 쪽으로 넓게 밀려오게 한다.
                        // 물가에서 먼 곳(dist가 거품 폭 2~4배 넘게)도 벽 앞 방식으로: 평평한 항구 바닥은 1m 칸 해저 비탈로 구한 dist의 등고선이 칸을 따라 꺾여
                        // 파도 앞이 네모난 조각으로 끊겼다. 거리 어림이 믿을 만한 곳은 물가 바로 앞뿐
                        float wallK = max(max(smoothstep(0.8, 2.0, depth), built), smoothstep(_FoamWidth * 2.0, _FoamWidth * 4.0, dist));
                        // 벽 가까움(prox): 맵을 읽을 때 물 위로 나온 땅을 σ 4.5m쯤 흐려 B 채널에 구워 둔 값 (WaterSurface.SetSeabed) → 벽에서 10m 남짓까지 매끈하게 옅어진다.
                        // (예전엔 픽셀마다 둘레 16곳 해저를 찍어 구해서 1m 칸 경계마다 꺾인 각진 빛 조각이 보였다)
                        float prox = smoothstep(0.0, 0.42, tex2D(_SeabedTex, uv).b);
                        // 파도 위상은 매끈한 가까움으로만 (예전엔 덩어리 노이즈를 곱한 가까움으로 위상을 만들어, 톱니가 끊기는 등고선이
                        // 구멍 난 얼룩 테두리로 또렷하게 보였다). 들쭉날쭉함은 밝기에만 (proxAmt)
                        float proxAmt = prox * (0.55 + 0.45 * FishNoise(p * 0.08 + t * 0.02));
                        float pseudo = (1 - prox) * _FoamWidth * 3.0;
                        float front = BioFront(p, lerp(dist, pseudo, wallK), t, speck);
                        // 벽·바위 앞은 날카로운 파도 앞 대신 넓고 완만하게 밀려와 구름처럼 뿌옇게 번지는 빛 (사진: 갯바위에 부서진 물):
                        // 앞줄도 12%에 걸쳐 차오르고(톱니가 넘어가는 자리에 경계 없이), 두 겹 흐르는 노이즈로 뭉게뭉게, 위에 자글자글한 점
                        float saw = BioSaw(p, pseudo, t);
                        float cloud = FishNoise(p * 0.5 + float2(t * 0.15, 0)) * 0.6 + FishNoise(p * 0.19 - float2(0, t * 0.06)) * 0.8;
                        float soft = smoothstep(0.0, 0.12, saw) * pow(saturate(1 - saw), 2.0) * (0.25 + cloud * 0.7) * (0.55 + speck * 0.6);
                        front = lerp(front, soft, wallK);
                        // 흰 하이라이트: 밀려오는 빛의 맨 앞줄만 가늘게, 덩어리 노이즈로 군데군데 끊긴 흰빛 물마루 (벽 앞은 앞줄도 부드럽게 차오른다)
                        float crest = smoothstep(0.0, lerp(0.012, 0.08, wallK), saw) * pow(saturate(1 - saw), lerp(60, 22, wallK)) * smoothstep(0.25, 0.7, FishNoise(p * 0.35 + t * 0.1));
                        prox = proxAmt;
                        float zone = lerp(smoothstep(_FoamWidth * 4.0, _FoamWidth * 0.3, dist), prox, wallK);
                        float wave = front * zone * BioPatch(p, t);
                        float grains = BioGrains(p, 3.2, t) * saturate(foam * 0.2 + zone * 0.5) + BioGrains(p * 0.61 + 17.3, 2.4, t) * step(depth, 0.35) * 0.6;
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
