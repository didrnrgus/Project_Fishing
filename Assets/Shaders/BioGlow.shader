// 야광충 빛 알갱이 파티클 (BioluminescenceFx.cs). 더하기 블렌드라 겹칠수록 밝고, 밝기 배율(_Boost)로 1을 넘겨 블룸이 번지게 한다.
//  - 색 = 파티클 색 rgb × 알파 (알파로 깜빡임·사라짐을 준다) × 둥근 점 텍스처
//  - 안개 속에서는 검게 (더하기라 안개색을 더하면 뿌옇게 뜬다)
Shader "Fishing/BioGlow"
{
    Properties
    {
        _MainTex ("Dot (A)", 2D) = "white" {}
        _Boost ("Boost", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Boost;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a;
                float4 c = float4(i.color.rgb * (i.color.a * a * _Boost), 1);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, c, float4(0, 0, 0, 0));
                return c;
            }
            ENDCG
        }
    }
}
