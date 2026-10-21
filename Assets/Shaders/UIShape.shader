// 둥근 상자 UI 도형 (UIShape): 채움(색·텍스처, 유리면 흐린 배경) + 가장자리 광택 + 안쪽 테두리 + 바깥 빛 + 그림자를 한 번에 그린다.
// 정점 채널 (UIShape.OnPopulateMesh): uv0 = 텍스처 UV, uv1 = (중심 기준 위치 xy, 반폭·반높이), uv2 = (모서리 반지름, 광택 폭, 테두리 폭, 바깥 빛 폭),
// uv3 = (그림자 오프셋 xy, 그림자 번짐, 그림자 진하기), tangent = 테두리 색, normal.x = 유리 덮는 색 진하기.
// 2026-10-21까지는 Translucent Image + Paraform(에셋 스토어)이었다.
Shader "Fishing/UIShape"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Glass ("Glass", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 params : TEXCOORD2;
                float4 shadow : TEXCOORD3;
                float4 border : TEXCOORD4;
                float4 screen : TEXCOORD5;
                float4 world : TEXCOORD6;
                float glass : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _UIBlurTex;
            float _Glass;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv0.xy, _MainTex);
                o.shape = v.uv1;
                o.params = v.uv2;
                o.shadow = v.uv3;
                o.border = v.tangent;
                o.screen = ComputeScreenPos(o.pos);
                o.glass = v.normal.x;
                return o;
            }

            // 둥근 상자 거리 (안쪽 음수)
            float sdRoundBox(float2 p, float2 b, float r)
            {
                r = min(r, min(b.x, b.y));
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            float4 over(float4 top, float4 bottom) { return top + bottom * (1.0 - top.a); }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.shape.xy;
                float2 hb = i.shape.zw;
                float radius = i.params.x, bevel = i.params.y, bw = i.params.z, glow = i.params.w;
                float d = sdRoundBox(p, hb, radius);
                float aa = max(fwidth(d), 1e-4);
                float inside = saturate(0.5 - d / aa);

                float3 rgb;
                float a;
                if (_Glass > 0.5)
                {
                    // 뒤 화면을 흐리게(UIBlurSource) → 채도를 올리고 대비를 줄이고 조금 어둡게 → 판 색으로 덮는다
                    float3 b = tex2D(_UIBlurTex, i.screen.xy / i.screen.w).rgb;
                    float l = dot(b, float3(0.299, 0.587, 0.114));
                    b = lerp(l.xxx, b, 1.8);
                    b = lerp(b, l.xxx * 0.5 + 0.25, 0.25) - 0.07;
                    rgb = lerp(saturate(b), i.color.rgb, saturate(i.glass));
                    a = 1.0;
                }
                else
                {
                    float4 t = tex2D(_MainTex, i.uv);
                    rgb = t.rgb * i.color.rgb;
                    a = t.a;
                }
                // 가장자리 광택: 테두리 안쪽 bevel 폭에서 밝아진다 (위쪽이 더)
                if (bevel > 0.01)
                {
                    float edge = saturate(1.0 + d / bevel);
                    float top = saturate(0.5 + 0.5 * p.y / max(hb.y, 1.0));
                    rgb += edge * edge * (0.03 + 0.09 * top);
                }
                float4 col = float4(rgb, 1.0) * (a * inside);

                // 그림자 (도형 아래)
                if (i.shadow.w > 0.001)
                {
                    float soft = max(i.shadow.z, 1.0);
                    float ds = sdRoundBox(p - i.shadow.xy, hb, radius);
                    float s = i.shadow.w * (1.0 - smoothstep(-soft * 0.35, soft, ds));
                    col = over(col, float4(0, 0, 0, s));
                }
                // 안쪽 테두리 선 + 바깥으로 번지는 빛
                float4 bc = i.border;
                if (bc.a > 0.001)
                {
                    if (bw > 0.001)
                    {
                        float band = inside * saturate((d + bw) / aa + 0.5);
                        col = over(float4(bc.rgb, 1) * (bc.a * band), col);
                    }
                    if (glow > 0.001 && d > 0)
                    {
                        float g = 1.0 - saturate(d / glow);
                        col = over(float4(bc.rgb, 1) * (bc.a * 0.65 * g * g), col);
                    }
                }
                col *= i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
