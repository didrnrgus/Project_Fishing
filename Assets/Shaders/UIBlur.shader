// 유리 판 뒤 흐린 화면 (UIBlurSource): 대각 4점 평균(Kawase)을 오프셋을 키워 가며 여러 번.
Shader "Hidden/Fishing/UIBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Offset (texels)", Float) = 1
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
            float4 _MainTex_TexelSize;
            float _Offset;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * (_Offset + 0.5);
                fixed4 c = tex2D(_MainTex, i.uv + float2(o.x, o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x, o.y));
                c += tex2D(_MainTex, i.uv + float2(o.x, -o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x, -o.y));
                c *= 0.25;
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
}
