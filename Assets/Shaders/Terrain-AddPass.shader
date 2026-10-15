// Fishing/Terrain의 추가 패스: 5~8번째 층을 더한다 (decal:add). 코드는 FishingTerrain.cginc.
Shader "Hidden/Fishing/Terrain-AddPass"
{
    Properties
    {
        [HideInInspector] _DiffuseRemapScale0 ("", Vector) = (1,1,1,1)
        [HideInInspector] _DiffuseRemapScale1 ("", Vector) = (1,1,1,1)
        [HideInInspector] _DiffuseRemapScale2 ("", Vector) = (1,1,1,1)
        [HideInInspector] _DiffuseRemapScale3 ("", Vector) = (1,1,1,1)
        [HideInInspector] _NormalScale0 ("", Float) = 1
        [HideInInspector] _NormalScale1 ("", Float) = 1
        [HideInInspector] _NormalScale2 ("", Float) = 1
        [HideInInspector] _NormalScale3 ("", Float) = 1
        _Variation ("Macro Variation", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue" = "Geometry-99" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Standard decal:add vertex:TerrainVert finalcolor:TerrainFinal fullforwardshadows
        #pragma multi_compile_fog
        #pragma target 3.0
        #define TERRAIN_SPLAT_ADDPASS
        #include "UnityPBSLighting.cginc"
        #include "FishingTerrain.cginc"

        void surf(Input IN, inout SurfaceOutputStandard o) { TerrainSurf(IN, o); }
        ENDCG
    }
    Fallback off
}
