// 낚시꾼 몸(피부). 옷 메쉬 속에 가려지는 피부는 그리지 않는다 (애니메이션 중 살이 옷을 뚫고 나오지 않게).
// 버텍스 컬러 = 덮임 마스크 (tools/blender/build_characters.py가 굽는다): R 상의, G 하의, B 신발, A 긴소매.
// _Clip 채널이 1이면 그 옷을 입은 것 → 마스크 > 0.5인 피부를 잘라낸다.
Shader "Fishing/Skin"
{
    Properties
    {
        _MainTex ("Skin (RGB)", 2D) = "white" {}
        _Color ("Skin Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.32
        _Clip ("Clip (R top, G bottom, B shoes, A long sleeve)", Vector) = (1,1,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        float4 _Clip;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            clip(0.5 - dot(IN.color, _Clip));
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
