// 숲 덮인 먼 섬·산 (FarLand). 예전에는 풀 텍스처를 늘여 붙인 매끈한 초록 원뿔이라 장난감처럼 보였다.
//  - 수관(나무 머리): FarLand가 코드로 구운 수관 높이·법선 텍스처를 월드 트라이플래너로 → 해를 받는 쪽이 밝고 나무 사이 틈이 어두운 숲 결
//  - 색: 수관마다 조금씩 다른 초록 + 수십 m 얼룩(활엽수 연두·소나무 짙은 초록), _Sparse > 0이면 나무 사이 맨땅(_Ground)이 드러난다 (덤불 산·눈 덮인 산)
//  - 바위: 가파른 면(1 - 법선.y > _RockSlope)과 물가 띠(전역 _FishingWetY 위 _ShoreBand m)는 바위 결(_RockTex 트라이플래너), 만조선 아래는 젖어 어둡다
//  - 안개: Landmark 셰이더처럼 맵 안개를 _FogScale배만 받는다 (공기 원근감: 멀수록 푸른 안개빛)
Shader "Fishing/Forest"
{
    Properties
    {
        _Canopy ("Canopy (R tint, G height)", 2D) = "gray" {}
        _CanopyNormal ("Canopy Normal", 2D) = "bump" {}
        _CanopyTile ("Canopy Meters Per Tile", Float) = 64
        _CanopyBump ("Canopy Bump", Range(0,2)) = 1
        _Dark ("Dark Green", Color) = (0.06,0.11,0.06,1)
        _Light ("Light Green", Color) = (0.2,0.29,0.11,1)
        _Ground ("Ground Between Trees", Color) = (0.36,0.32,0.24,1)
        _Sparse ("Sparse", Range(0,1)) = 0
        _RockTex ("Rock", 2D) = "gray" {}
        _RockNor ("Rock Normal", 2D) = "bump" {}
        _RockAvg ("Rock Average", Color) = (0.5,0.5,0.5,1)
        _RockTint ("Rock Tint", Color) = (0.46,0.45,0.42,1)
        _RockTile ("Rock Meters Per Tile", Float) = 16
        _RockSlope ("Rock From Slope", Range(0,1)) = 0.5
        _ShoreBand ("Shore Rock Band (m)", Float) = 3
        _Snow ("Snow", Range(0,1)) = 0
        _FogScale ("Fog Scale", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow nofog finalcolor:forestFog vertex:vert
        #pragma target 3.0
        #include "FishingNoise.cginc"

        sampler2D _Canopy, _CanopyNormal, _RockTex, _RockNor;
        fixed4 _Dark, _Light, _Ground, _RockAvg, _RockTint;
        float _CanopyTile, _RockTile, _ShoreBand;
        half _CanopyBump, _Sparse, _RockSlope, _Snow, _FogScale;
        float _FishingWetY;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float fogDist;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // 메쉬에 접선이 없어도 되게 아무 직교 축이나 준다 (surf에서 WorldNormalVector로 같은 축을 되찾아 월드 법선을 접선 공간으로 바꾼다)
            float3 n = v.normal;
            v.tangent = float4(normalize(cross(n, abs(n.z) < 0.9 ? float3(0, 0, 1) : float3(1, 0, 0))), 1);
            o.fogDist = length(UnityObjectToViewPos(v.vertex));
        }

        // 화이트아웃 트라이플래너 법선 (월드)
        float3 TriNormal(sampler2D tex, float3 p, float3 n, float3 bw, half scale)
        {
            half3 tx = UnpackScaleNormal(tex2D(tex, p.zy), scale);
            half3 ty = UnpackScaleNormal(tex2D(tex, p.xz), scale);
            half3 tz = UnpackScaleNormal(tex2D(tex, p.xy), scale);
            tx = half3(tx.xy + n.zy, abs(tx.z) * n.x);
            ty = half3(ty.xy + n.xz, abs(ty.z) * n.y);
            tz = half3(tz.xy + n.xy, abs(tz.z) * n.z);
            return normalize(tx.zyx * bw.x + ty.xzy * bw.y + tz.xyz * bw.z);
        }

        half4 Tri(sampler2D tex, float3 p, float3 bw)
        {
            return tex2D(tex, p.zy) * bw.x + tex2D(tex, p.xz) * bw.y + tex2D(tex, p.xy) * bw.z;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Normal = float3(0, 0, 1);
            float3 N = normalize(WorldNormalVector(IN, float3(0, 0, 1)));
            float3 T = WorldNormalVector(IN, float3(1, 0, 0));
            float3 B = WorldNormalVector(IN, float3(0, 1, 0));
            float3 wp = IN.worldPos;
            float3 bw = pow(abs(N), 4);
            bw /= (bw.x + bw.y + bw.z);

            // 수관: 위 투영은 두 번(회전·다른 배율) 읽어 반복 무늬를 깬다
            float3 p = wp / _CanopyTile;
            half4 cv = Tri(_Canopy, p, bw);
            float2 q = float2(p.x * 0.799 - p.z * 0.602, p.x * 0.602 + p.z * 0.799) * 0.71 + 0.37;
            float mixN = smoothstep(0.35, 0.65, FishNoise(wp.xz * 0.011));
            cv = lerp(cv, tex2D(_Canopy, q), mixN * bw.y);
            half h = cv.g;
            float patch = FishNoise(wp.xz * 0.013 + 3.1) * 0.6 + FishNoise(wp.xz * 0.05 + 7.7) * 0.4;
            half3 green = lerp(_Dark.rgb, _Light.rgb, saturate(cv.r * 0.55 + patch * 0.75 - 0.2));
            half3 canopy = green * lerp(0.32, 1.12, h);
            if (_Sparse > 0.001)
            {
                half ground = 1 - smoothstep(_Sparse - 0.06, _Sparse + 0.08, h + (patch - 0.5) * 0.3);
                canopy = lerp(canopy, _Ground.rgb * lerp(0.8, 1.1, patch), ground);
            }
            if (_Snow > 0.001)
                canopy = lerp(canopy, half3(0.86, 0.89, 0.93), saturate((1 - h * 1.3) * smoothstep(0.4, 0.8, N.y)) * _Snow);
            float3 nCanopy = TriNormal(_CanopyNormal, p, N, bw, _CanopyBump);

            // 바위: 가파른 면 + 물가 띠. 수관 높이로 경계를 들쭉날쭉하게 (높은 나무가 바위 끝에 매달린다)
            float rn = FishNoise(wp.xz * 0.045 + wp.y * 0.06) * 0.7 + FishNoise(wp.xz * 0.21) * 0.3;
            float slope = 1 - N.y;
            float rock = smoothstep(_RockSlope - 0.1, _RockSlope + 0.1, slope + (rn - 0.5) * 0.35);
            float shore = 1 - smoothstep(_FishingWetY + _ShoreBand * 0.4, _FishingWetY + _ShoreBand * (0.8 + rn * 0.6), wp.y);
            rock = saturate(max(rock, shore) * 1.3 - h * 0.35);

            float3 pr = wp / _RockTile;
            half3 rc = Tri(_RockTex, pr, bw).rgb;
            half3 rc2 = Tri(_RockTex, pr * 0.27, bw).rgb;
            half3 k = (rc * 0.6 + rc2 * 0.4) / max(_RockAvg.rgb, 0.05);
            half3 rockCol = _RockTint.rgb * lerp(dot(k, half3(0.3, 0.59, 0.11)).xxx, k, 0.5);
            // 만조선 아래는 젖어 어둡고, 바로 위는 소금기로 살짝 바랜다
            float wet = 1 - smoothstep(_FishingWetY - 0.2, _FishingWetY + 0.5, wp.y + (rn - 0.5) * 0.6);
            rockCol *= lerp(1, 0.5, wet);
            float3 nRock = TriNormal(_RockNor, pr, N, bw, 1);

            half3 col = lerp(canopy, rockCol, rock);
            float3 W = normalize(lerp(nCanopy, nRock, rock));
            o.Normal = normalize(float3(dot(W, T), dot(W, B), dot(W, N)));
            o.Albedo = col;
            o.Smoothness = lerp(0.06, lerp(0.12, 0.55, wet), rock);
            o.Metallic = 0;
            o.Alpha = 1;
        }

        void forestFog(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            float f = saturate(exp2(-unity_FogParams.y * IN.fogDist * _FogScale));
        #ifdef UNITY_PASS_FORWARDADD
            color.rgb = lerp(fixed3(0, 0, 0), color.rgb, f);
        #else
            color.rgb = lerp(unity_FogColor.rgb, color.rgb, f);
        #endif
        }
        ENDCG
    }
    FallBack "Diffuse"
}
