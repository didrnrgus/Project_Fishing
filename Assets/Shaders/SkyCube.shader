// 흐린 날·비 오는 날 하늘에 덮는 큐브맵 (AllSky Free "Overcast Low", Resources/Sky/Overcast).
// 카메라를 따라다니는 큰 구(안쪽 면, 반지름 1150m)에 그린다. 스카이박스(Skybox/Procedural) 위, 구름(Fishing/Sky) 아래.
// Built-in 렌더러는 스카이박스를 불투명 물체 다음(큐 2500 무렵)에 그리므로 그보다 뒤(Transparent-300)에 그리고, 깊이 검사로 지형·건물 뒤에 가린다.
//  _Blend: 덮는 정도 (맑음 0 ~ 비 1), _Tint: 시간대 밝기·색 (밤이면 어둡게)
//  지평선 아래는 큐브맵의 땅 대신 안개색으로 흐린다
Shader "Fishing/SkyCube"
{
    Properties
    {
        _Cube ("Cubemap", CUBE) = "" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Blend ("Blend", Range(0,1)) = 0
        _Yaw ("Yaw (deg)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-300" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Skybox" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Cube;
            fixed4 _Tint;
            float _Blend, _Yaw;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 d = normalize(v.vertex.xyz);
                float s, c;
                sincos(radians(_Yaw), s, c);
                o.dir = float3(c * d.x - s * d.z, d.y, s * d.x + c * d.z);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                fixed3 col = texCUBE(_Cube, d).rgb * _Tint.rgb;
                // 지평선 아래(땅 그림)는 안개색으로
                float below = saturate(-d.y * 8.0);
                col = lerp(col, unity_FogColor.rgb, below);
                return fixed4(col, _Blend);
            }
            ENDCG
        }
    }
}
