using System;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 높이 함수와 칠하기 함수로 Unity Terrain을 만든다 (홍현 <see cref="SeaTerrain"/>, 서천 <see cref="SeocheonTerrain"/>).
    /// 텍스처는 Resources/Terrain의 Poly Haven CC0 (SOURCES.md). 셰이더는 반복 무늬를 깨는 Fishing/Terrain (Assets/Shaders/Terrain.shader).
    /// </summary>
    public static class TerrainBuilder
    {
        public struct LayerSpec
        {
            public string Texture;   // Resources/Terrain/<Texture>_diff, _nor
            public float Tile;       // 텍스처 한 장 크기 (m)
            public float Smoothness;
            public Color Tint;       // (0,0,0,0)이면 그대로. 주의: 내장 Terrain 셰이더(Built-in)는 무시한다 — 색을 바꾸려면 텍스처를 따로 만든다

            public LayerSpec(string texture, float tile, float smoothness, Color tint = default)
            {
                Texture = texture;
                Tile = tile;
                Smoothness = smoothness;
                Tint = tint;
            }
        }

        /// <param name="area">x, z 시작과 크기 (월드)</param>
        /// <param name="heightAt">(x, z) → 월드 높이</param>
        /// <param name="splat">(x, z, 높이, 경사도°, 층 가중치 배열) → 가중치를 채운다 (합은 알아서 1로 맞춘다)</param>
        public static Terrain Build(Transform parent, string name, Rect area, float baseY, float heightRange, int res,
            Func<float, float, float> heightAt, LayerSpec[] layers, Action<float, float, float, float, float[]> splat, int splatRes = 512)
        {
            var data = new TerrainData { heightmapResolution = res };
            data.size = new Vector3(area.width, heightRange, area.height);
            var h = new float[res, res];
            for (int iz = 0; iz < res; iz++)
            {
                float z = area.yMin + area.height * iz / (res - 1);
                for (int ix = 0; ix < res; ix++)
                {
                    float x = area.xMin + area.width * ix / (res - 1);
                    h[iz, ix] = Mathf.Clamp01((heightAt(x, z) - baseY) / heightRange);
                }
            }
            data.SetHeights(0, 0, h);

            var tls = new TerrainLayer[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                var l = layers[i];
                tls[i] = new TerrainLayer
                {
                    diffuseTexture = Resources.Load<Texture2D>("Terrain/" + l.Texture + "_diff") ?? Texture2D.grayTexture,
                    normalMapTexture = Resources.Load<Texture2D>("Terrain/" + l.Texture + "_nor"),
                    tileSize = new Vector2(l.Tile, l.Tile),
                    smoothness = l.Smoothness,
                    normalScale = 1f,
                };
                if (l.Tint.a > 0f) tls[i].diffuseRemapMax = new Vector4(l.Tint.r, l.Tint.g, l.Tint.b, 1f);
            }
            data.terrainLayers = tls;
            data.alphamapResolution = splatRes;
            var a = new float[splatRes, splatRes, layers.Length];
            var w = new float[layers.Length];
            for (int iz = 0; iz < splatRes; iz++)
            for (int ix = 0; ix < splatRes; ix++)
            {
                float nx = (ix + 0.5f) / splatRes, nz = (iz + 0.5f) / splatRes;
                float x = area.xMin + area.width * nx, z = area.yMin + area.height * nz;
                float y = data.GetInterpolatedHeight(nx, nz) + baseY;
                Array.Clear(w, 0, w.Length);
                splat(x, z, y, data.GetSteepness(nx, nz), w);
                float sum = 1e-5f;
                for (int k = 0; k < w.Length; k++) sum += Mathf.Max(0f, w[k]);
                for (int k = 0; k < w.Length; k++) a[iz, ix, k] = Mathf.Max(0f, w[k]) / sum;
            }
            data.SetAlphamaps(0, 0, a);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(area.xMin, baseY, area.yMin);
            var terrain = go.GetComponent<Terrain>();
            // 반복 무늬 깨기 지형 셰이더 (층마다 두 번 샘플 + 큰 얼룩). 없으면 내장 Standard 지형
            var shader = Shader.Find("Fishing/Terrain") ?? Shader.Find("Nature/Terrain/Standard");
            if (shader != null) terrain.materialTemplate = new Material(shader);
            terrain.drawInstanced = false; // Fishing/Terrain은 인스턴싱 지형 경로를 쓰지 않는다
            terrain.heightmapPixelError = 2f; // 4면 멀리서 지형이 길·앞마당 위로 솟아 깜빡인다
            terrain.basemapDistance = 250f;
            terrain.drawTreesAndFoliage = false;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return terrain;
        }

        public static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }
    }
}
