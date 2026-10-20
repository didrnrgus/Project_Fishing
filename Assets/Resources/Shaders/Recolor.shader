// NPC 머리·옷 색 바꾸기 (Characters/NpcLooks): 텍스처를 밝기만 남기고 _Tint 색을 입힌다. 명암·결은 그대로, 평균 밝기(_AvgLum)가 _Tint가 된다.
Shader "Hidden/Fishing/Recolor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _AvgLum ("Average luminance", Float) = 0.5
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Tint;
            float _AvgLum;

            float4 frag (v2f_img i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float l = dot(c.rgb, float3(0.2126, 0.7152, 0.0722));
                // 평균보다 밝은 곳(하이라이트)은 덜 키워 하얗게 타지 않게
                float k = l / max(_AvgLum, 0.02);
                k = k < 1 ? k : 1 + (k - 1) * 0.35;
                return float4(saturate(_Tint.rgb * k), c.a);
            }
            ENDCG
        }
    }
}
