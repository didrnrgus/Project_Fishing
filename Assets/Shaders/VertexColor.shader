// 꼭짓점 색 그대로 칠하는 반질한 면 (탄천 카약·부력 큐브 선착장·사람: 절차 메쉬, KayakKit).
// Fishing/Weathered의 _VertexAO는 꼭짓점 색을 밝기 + 따뜻함/차가움 두 값으로만 받아 주황이 갈색, 분홍이 베이지가 됐다 → RGB 그대로.
Shader "Fishing/VertexColor"
{
    Properties
    {
        _MainTex ("Unused (batching asks for it)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off   // 얇은 판(노 날)·안쪽이 보일 수 있는 절차 메쉬도 빠지지 않게

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _Color;
        half _Glossiness;

        struct Input
        {
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
