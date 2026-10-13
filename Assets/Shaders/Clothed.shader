// 속옷 차림 베이스 캐릭터에 옷을 입히는 셰이더.
// 버텍스 컬러 R=셔츠, G=바지, B=신발, A=조끼(몸통) 가중치 (CharacterFactory가 뼈 가중치로 굽는다).
Shader "Fishing/Clothed"
{
    Properties
    {
        _MainTex ("Skin (RGB)", 2D) = "white" {}
        _Color ("Skin Tint", Color) = (1,1,1,1)
        _ShirtColor ("Shirt", Color) = (0.9,0.9,0.92,1)
        _PantsColor ("Pants", Color) = (0.25,0.3,0.45,1)
        _ShoeColor ("Shoes", Color) = (0.15,0.15,0.17,1)
        _VestColor ("Vest", Color) = (0.25,0.4,0.3,1)
        _VestAmount ("Vest On", Range(0,1)) = 1
        _Glossiness ("Skin Smoothness", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color, _ShirtColor, _PantsColor, _ShoeColor, _VestColor;
        half _VestAmount;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        // 천 질감용 작은 격자 노이즈
        float Weave(float2 uv)
        {
            float2 g = frac(uv * 420.0);
            return 0.94 + 0.06 * step(0.5, g.x) * step(0.5, g.y);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 skin = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            float shirt = smoothstep(0.35, 0.65, IN.color.r);
            float pants = smoothstep(0.35, 0.65, IN.color.g);
            float shoes = smoothstep(0.35, 0.65, IN.color.b);
            float vest = smoothstep(0.5, 0.8, IN.color.a) * _VestAmount;
            float weave = Weave(IN.uv_MainTex);

            fixed3 c = skin;
            c = lerp(c, _PantsColor.rgb * weave, pants);
            c = lerp(c, _ShirtColor.rgb * weave, shirt);
            c = lerp(c, _VestColor.rgb * (0.9 + 0.1 * weave), vest * shirt);
            c = lerp(c, _ShoeColor.rgb, shoes);

            float cloth = saturate(shirt + pants + shoes);
            o.Albedo = c;
            o.Metallic = 0;
            o.Smoothness = lerp(_Glossiness, 0.08 + shoes * 0.3, cloth);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
