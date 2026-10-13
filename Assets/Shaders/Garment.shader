// 낚시꾼 옷·모자·안경 (tools/blender/build_characters.py가 만든 메쉬).
// 버텍스 컬러: R 트림(목 리브·소매단·밑창·지퍼·주머니 덮개) → _TrimColor
//              G 음영(1 = 밝음, 주머니 몸통은 조금 어둡게)
//              B 보조 원단(조끼 등판 망사, 밀짚 엮음) → _PatternColor 망사 무늬
//              A 휴식 자세 높이/2 → _ClipBelow(m)보다 낮으면 잘라낸다 (장화를 신으면 바지 아랫단을 장화 속에 넣는다)
// _Pattern: 0 무지, 1 체크(남방), 2 데님, 3 줄무늬, 4 엮음(밀짚)
Shader "Fishing/Garment"
{
    Properties
    {
        _MainTex ("Unused (UV)", 2D) = "white" {}
        _Color ("Main", Color) = (0.8,0.8,0.8,1)
        _TrimColor ("Trim", Color) = (0.6,0.6,0.6,1)
        _PatternColor ("Pattern", Color) = (0.3,0.3,0.3,1)
        _Pattern ("Pattern (0 none 1 plaid 2 denim 3 stripe 4 weave)", Float) = 0
        _PatternScale ("Pattern Scale", Float) = 24
        _ClipBelow ("Clip Below Height (m)", Float) = -1
        _Glossiness ("Smoothness", Range(0,1)) = 0.12
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color, _TrimColor, _PatternColor;
        float _Pattern, _PatternScale, _ClipBelow;
        half _Glossiness, _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 m = IN.color;
            clip(m.a * 2.0 - _ClipBelow);

            float2 uv = IN.uv_MainTex * _PatternScale;
            float3 c = _Color.rgb;
            if (_Pattern > 0.5 && _Pattern < 1.5)
            {
                // 체크: 굵은 띠 두 방향 + 가는 선
                float2 g = frac(uv);
                float band = step(g.x, 0.3) + step(g.y, 0.3);
                float lineA = step(abs(g.x - 0.65), 0.03) + step(abs(g.y - 0.65), 0.03);
                c = lerp(c, _PatternColor.rgb, saturate(band * 0.45));
                c = lerp(c, float3(0.95, 0.92, 0.85), saturate(lineA) * 0.5);
            }
            else if (_Pattern > 1.5 && _Pattern < 2.5)
            {
                // 데님: 사선 능직 + 실 얼룩
                float twill = frac((uv.x + uv.y) * 6.0);
                c *= 0.9 + 0.1 * step(0.5, twill);
                c *= 0.92 + 0.16 * Hash(floor(uv * 10.0));
            }
            else if (_Pattern > 2.5 && _Pattern < 3.5)
            {
                c = lerp(c, _PatternColor.rgb, step(frac(uv.y * 0.8), 0.35));
            }
            else if (_Pattern > 3.5)
            {
                float2 g = frac(uv * 1.5);
                float weave = step(0.5, g.x) == step(0.5, g.y) ? 1.0 : 0.82;
                c *= weave * (0.92 + 0.12 * Hash(floor(uv * 3.0)));
            }
            // 보조 원단(망사): 작은 구멍 무늬
            float2 h = frac(uv * 3.0) - 0.5;
            float holes = step(dot(h, h), 0.08);
            c = lerp(c, lerp(_PatternColor.rgb, _PatternColor.rgb * 0.5, holes), saturate(m.b) * step(_Pattern, 3.5));
            if (_Pattern > 3.5) c *= lerp(1.0, 0.9 + 0.1 * holes, m.b);

            c = lerp(c, _TrimColor.rgb, saturate(m.r));
            c *= m.g;
            // 천 결: 아주 옅은 잔무늬
            c *= 0.97 + 0.06 * Hash(floor(IN.uv_MainTex * 380.0));

            o.Albedo = c;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
