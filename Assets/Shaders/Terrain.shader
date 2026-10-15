// 반복 무늬 깨기 지형 셰이더 (내장 렌더 파이프라인 Terrain, 첫 4층). 5번째 층부터는 Terrain-AddPass.shader.
// 코드는 FishingTerrain.cginc.
Shader "Fishing/Terrain"
{
    Properties
    {
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector] _NormalScale0 ("", Float) = 1
        [HideInInspector] _NormalScale1 ("", Float) = 1
        [HideInInspector] _NormalScale2 ("", Float) = 1
        [HideInInspector] _NormalScale3 ("", Float) = 1
        [HideInInspector] _DiffuseRemapScale0 ("", Vector) = (1,1,1,1)
        [HideInInspector] _DiffuseRemapScale1 ("", Vector) = (1,1,1,1)
        [HideInInspector] _DiffuseRemapScale2 ("", Vector) = (1,1,1,1)
        [HideInInspector] _DiffuseRemapScale3 ("", Vector) = (1,1,1,1)
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
        _Variation ("Macro Variation", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "TerrainCompatible" = "True" }

        CGPROGRAM
        #pragma surface surf Standard vertex:TerrainVert finalcolor:TerrainFinal fullforwardshadows addshadow
        #pragma multi_compile_fog
        #pragma target 3.0
        #include "UnityPBSLighting.cginc"
        #include "FishingTerrain.cginc"

        void surf(Input IN, inout SurfaceOutputStandard o) { TerrainSurf(IN, o); }
        ENDCG
    }
    Dependency "AddPassShader" = "Hidden/Fishing/Terrain-AddPass"
    Fallback "Nature/Terrain/Diffuse"
}
