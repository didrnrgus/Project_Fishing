// 풀·들꽃 덤불 (GrassField가 만든 교차 판 메쉬, Grass Flowers Pack Free 그림).
// 메쉬는 월드 좌표로 구워 두므로 정점 좌표가 곧 월드 좌표다. 위쪽 정점(uv.y)일수록 바람에 흔들린다.
// 법선은 모두 위(0,1,0)로 둬서 교차 판이 어느 쪽에서 봐도 땅처럼 고르게 밝다. 아래쪽은 그늘(포기 안쪽)로 살짝 어둡게.
Shader "Fishing/Grass"
{
    Properties
    {
        _MainTex ("Grass (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45
        _Wind ("Wind Sway (m)", Float) = 0.07
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Cutoff, _Wind;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            float3 wp = v.vertex.xyz;
            float t = _Time.y;
            float sway = sin(t * 1.7 + wp.x * 0.35 + wp.z * 0.27) + 0.45 * sin(t * 3.3 + wp.x * 1.1 - wp.z * 0.8);
            float k = v.texcoord.y * v.texcoord.y;
            v.vertex.x += sway * _Wind * k;
            v.vertex.z += sway * _Wind * 0.6 * k;
            v.normal = float3(0, 1, 0);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            clip(c.a - _Cutoff);
            o.Albedo = c.rgb * IN.color.rgb * _Color.rgb * lerp(0.55, 1.0, saturate(IN.uv_MainTex.y * 1.6));
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack Off
}
