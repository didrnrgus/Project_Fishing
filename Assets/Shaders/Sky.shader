// 하늘 장식(달, 별, 구름) 공용 셰이더. 안개를 받지 않고 깊이를 쓰지 않는다.
//  _Mode 0: 텍스처 × 색 × 버텍스 컬러 (달)
//  _Mode 2: 별 — _Mode 0에 별마다 다른 박자의 반짝임(uv2.x = 위상)
//  _Mode 1: 구름 — 텍스처 알파를 흘리고, 버텍스 컬러 알파(지평선 근처 페이드)를 곱한다. _Coverage가 0.6을 넘으면 층구름이 하늘을 덮는다
Shader "Fishing/Sky"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mode ("Mode", Float) = 0
        _Scroll ("Scroll (xy)", Vector) = (0.003, 0.001, 0, 0)
        _Coverage ("Cloud Coverage", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent-200" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Mode, _Coverage;
            float4 _Scroll;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                if (_Mode > 1.5)
                {
                    // 별 반짝임: 위상마다 다른 속도로, 0.55~1 사이를 오간다
                    float p = v.uv2.x;
                    float tw = sin(_Time.y * (1.3 + p * 2.4) + p * 40.0) * sin(_Time.y * (0.7 + p) + p * 17.0);
                    o.color.a *= 0.78 + 0.22 * tw;
                }
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Mode < 0.5 || _Mode > 1.5)
                    return tex2D(_MainTex, i.uv) * _Color * i.color;

                // 구름: 두 겹을 다른 속도로 흘려서 뭉게구름 느낌
                float a1 = tex2D(_MainTex, i.uv * 3.0 + _Time.y * _Scroll.xy).a;
                float a2 = tex2D(_MainTex, i.uv * 5.3 - _Time.y * _Scroll.xy * 0.7).a;
                float d = (a1 * 0.65 + a2 * 0.35);
                // 노이즈 값은 0.3~0.7에 몰려 있어서 임계값도 그 안에서 움직인다
                float threshold = 0.66 - _Coverage * 0.3;
                float cloud = smoothstep(threshold, threshold + 0.12, d);
                // 흐림·비: 뭉게구름 사이를 회색 층구름(stratus)이 메운다 (흐린 날 하늘 큐브맵을 섞는 대신 절차적으로)
                float stratus = smoothstep(0.6, 1.0, _Coverage);
                float layer = stratus * (0.7 + 0.3 * smoothstep(0.35, 0.65, a2));
                fixed4 c = _Color;
                c.rgb *= lerp(1.0, 0.82, saturate((d - 0.5) * 2.0)); // 두꺼운 곳은 살짝 어둡게
                c.rgb *= lerp(1.0, 0.86, layer * (1.0 - cloud));      // 층구름은 뭉게구름보다 조금 어둡고 평평하게
                cloud = max(cloud, layer);
                c.a *= cloud * i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
