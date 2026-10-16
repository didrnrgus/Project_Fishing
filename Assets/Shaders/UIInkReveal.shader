// 인트로 글자·그림용 UI 셰이더 (IntroSequence).
//  _Progress: 0이면 안 보이고 1이면 다 보인다. 왼쪽부터 붓으로 쓰듯 번지며 드러난다 (가장자리는 노이즈로 들쭉날쭉, 먹이 막 묻은 곳은 살짝 진하게).
//  _Shine: 왼쪽(-0.3)에서 오른쪽(1.3)으로 지나가는 빛줄기 위치. 0 미만·1 초과면 안 보인다.
//  _SrcBlend/_DstBlend: 보통(SrcAlpha, OneMinusSrcAlpha) / 더하기(SrcAlpha, One — 물에 비친 달빛)
Shader "Fishing/UIInkReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Reveal", Range(0, 1)) = 1
        _Soft ("Reveal Softness", Range(0.001, 0.5)) = 0.08
        _Ragged ("Ragged Edge", Range(0, 0.3)) = 0.06
        _Shine ("Shine Position", Float) = -1
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.12
        _ShineColor ("Shine Color", Color) = (1, 0.95, 0.8, 0.8)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "FishingNoise.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Progress, _Soft, _Ragged, _Shine, _ShineWidth;
            fixed4 _ShineColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // RawImage의 uvRect로 옮겨도 드러나는 방향이 그림 기준이 되도록 0~1 uv를 쓴다
                float x = saturate(i.uv.x);
                float n = FishNoise(i.uv * float2(9, 22)) * 0.65 + FishNoise(i.uv * float2(31, 70)) * 0.35;
                float edge = lerp(-_Soft - _Ragged, 1 + _Soft + _Ragged, _Progress);   // 0 → 완전히 가림, 1 → 완전히 보임
                float d = edge - (x + (n - 0.5) * _Ragged * 2);
                float reveal = smoothstep(0, _Soft, d);
                // 막 그어진 자리는 먹이 짙다
                float wet = (1 - smoothstep(0, _Soft * 2.5, d)) * reveal * step(_Progress, 0.999);
                c.rgb *= 1 - wet * 0.25;
                c.a *= reveal;
                // 빛줄기 (살짝 기울어진 띠)
                float s = (x + (i.uv.y - 0.5) * 0.25) - _Shine;
                float shine = exp(-s * s / (_ShineWidth * _ShineWidth)) * _ShineColor.a;
                c.rgb += _ShineColor.rgb * shine * c.a;
                return c;
            }
            ENDCG
        }
    }
}
