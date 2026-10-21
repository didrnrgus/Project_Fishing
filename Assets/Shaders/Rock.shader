// 실사 바위·나무껍질 (Assets/Vegetation: Poly Haven 바위, ez-tree 껍질). 알베도 + 법선 맵, 거친 표면.
// _Snow > 0이면 위를 보는 면부터 눈이 덮인다 (눈 맵 청진포구의 바위 — 예전 Rock and Boulders 2의 눈 판 대신).
Shader "Fishing/Rock"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.12
        _Snow ("Snow", Range(0,1)) = 0
        _SnowColor ("Snow Color", Color) = (0.93, 0.95, 0.98, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _BumpMap;
        fixed4 _Color, _SnowColor;
        half _Smoothness, _Snow;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Albedo = c.rgb;
            o.Smoothness = _Smoothness;
            if (_Snow > 0.001)
            {
                float up = WorldNormalVector(IN, o.Normal).y;
                float s = smoothstep(0.55 - _Snow * 0.35, 0.8 - _Snow * 0.3, up) * _Snow;
                o.Albedo = lerp(o.Albedo, _SnowColor.rgb, s);
                o.Smoothness = lerp(o.Smoothness, 0.35, s);
                o.Normal = normalize(lerp(o.Normal, float3(0, 0, 1), s * 0.7));
            }
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
