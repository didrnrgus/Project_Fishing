// 귀신: 희미하게 비치는 푸르스름한 흰 몸. 가장자리(시선과 비스듬한 면)가 밝게 빛나고, 발 쪽으로 사라지며, 천천히 일렁인다.
// 깊이는 먼저 한 번 써서(ColorMask 0) 자기 몸 뒤쪽 면이 겹쳐 보이지 않게 한다. _Fade로 나타나고 사라진다(NightVisitor).
Shader "Fishing/Ghost"
{
    Properties
    {
        _Color ("Color", Color) = (0.82, 0.9, 1, 1)
        _Rim ("Rim Glow", Range(0, 4)) = 2.2
        _Fade ("Fade", Range(0, 1)) = 1
        _FeetY ("Feet Fade Height (m)", Float) = 0.6
        _BaseY ("Feet World Y", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            ZWrite On
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return 0; }
            ENDCG
        }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Rim, _Fade, _FeetY;
            float _BaseY;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 v : TEXCOORD1;
                float3 wp : TEXCOORD2;
                float h : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            v2f vert(appdata_base i)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, i.vertex);
                // 천천히 일렁임
                wp.xz += sin(_Time.y * 1.3 + wp.y * 3.0) * 0.015;
                o.pos = mul(UNITY_MATRIX_VP, wp);
                o.wp = wp.xyz;
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = _WorldSpaceCameraPos - wp.xyz;
                o.h = wp.y - _BaseY; // 발(NightVisitor가 매 프레임 넣는 높이)에서 잰 높이
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.n), v = normalize(i.v);
                float rim = pow(1.0 - saturate(abs(dot(n, v))), 2.0);
                float feet = smoothstep(0.0, _FeetY, i.h);
                float flicker = 0.85 + 0.15 * sin(_Time.y * 7.0 + i.wp.y * 5.0) * sin(_Time.y * 2.3);
                float a = (0.5 + rim * _Rim * 0.35) * feet * flicker * _Fade;
                fixed4 c = fixed4(_Color.rgb * (0.9 + rim * _Rim), saturate(a));
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0, 0, 0, 0));
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
