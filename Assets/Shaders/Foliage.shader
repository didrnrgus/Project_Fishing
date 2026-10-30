// 나무·덤불 잎과 먼 거리 십자 판 (Assets/Vegetation, tools/blender/build_vegetation.py — ez-tree 잎 그림).
// 알파로 잘라 내고 양면을 그린다(뒷면은 법선을 뒤집는다). 잎 법선은 Blender에서 수관 가운데에서 바깥으로 넣어 두었다.
// 빛을 등진 쪽도 조금 비쳐 보이게(투과) 하고, 위쪽 정점일수록 바람에 살짝 흔들린다.
Shader "Fishing/Foliage"
{
    Properties
    {
        _MainTex ("Leaves (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
        _Wind ("Wind Sway (m)", Float) = 0.05
        _WindHeight ("Wind Full Height (m, object)", Float) = 8
        _Translucency ("Translucency", Range(0,1)) = 0.35
        _Blossom ("Blossom (0 = leaves)", Range(0,1)) = 0
        _BlossomColor ("Blossom Color", Color) = (1,0.84,0.88,1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Foliage vertex:vert alphatest:_Cutoff addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Wind, _WindHeight, _Translucency, _Blossom;
        fixed4 _BlossomColor;

        struct Input
        {
            float2 uv_MainTex;
            float facing : VFACE;
        };

        half4 LightingFoliage(SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir);
            half diff = saturate(ndl) + saturate(-ndl) * _Translucency;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten);
            c.a = s.Alpha;
            return c;
        }

        void vert(inout appdata_full v)
        {
            float3 wp = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float t = _Time.y;
            float k = saturate(v.vertex.y / max(_WindHeight, 0.01));
            float sway = sin(t * 1.3 + wp.x * 0.21 + wp.z * 0.17 + v.vertex.y * 0.4) + 0.35 * sin(t * 2.9 + v.vertex.x * 1.7);
            v.vertex.xz += sway * _Wind * k * k;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            // 꽃 핀 나무(벚나무): 잎 그림의 밝기 결만 남기고 꽃 색으로 칠한다 (잎 사이사이 그늘은 어둡게)
            half lum = dot(c.rgb, half3(0.3, 0.59, 0.11));
            o.Albedo = lerp(c.rgb, _BlossomColor.rgb * (0.55 + 1.2 * lum), _Blossom);
            o.Alpha = c.a;
            o.Normal = float3(0, 0, IN.facing > 0 ? 1 : -1);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
