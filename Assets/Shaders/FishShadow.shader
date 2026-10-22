// 물속 그림자 (퀘스트 고기·귀한 고기가 수면 아래 검은 실루엣으로 어슬렁거린다).
//  - 물(Fishing/Water)이 불투명이라 물속 메쉬는 보이지 않는다 → 수면 바로 위에 납작한 띠(FishShadowFx)를 깔고 그린다
//  - 모양은 텍스처 알파(ProceduralTextures.FishShadow: 위에서 본 물고기, 가장자리가 흐릿하다), 꼬리 흔들기는 C#이 띠를 휜다
//  - 물결 노이즈로 가장자리를 일렁이게 해 물 너머로 비쳐 보이는 느낌을 낸다. 안개를 받는다
Shader "Fishing/FishShadow"
{
    Properties
    {
        _MainTex ("Silhouette (A)", 2D) = "white" {}
        _Color ("Color", Color) = (0,0.015,0.025,0.86)
    }
    SubShader
    {
        // 어군 소용돌이(Transparent)보다 먼저 그려 거품이 그림자 위에 얹히게
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 wp : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.wp = mul(unity_ObjectToWorld, v.vertex).xz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 수면 굴절처럼 uv를 살짝 일렁인다
                float t = _Time.y;
                float2 wob = float2(sin(i.wp.y * 2.3 + t * 1.7), cos(i.wp.x * 2.1 - t * 1.3)) * 0.012;
                fixed a = tex2D(_MainTex, i.uv + wob).a;
                fixed4 c = fixed4(_Color.rgb, _Color.a * a);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0, 0, 0, 0));
                return c;
            }
            ENDCG
        }
    }
}
