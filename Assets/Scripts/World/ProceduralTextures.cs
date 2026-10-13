using UnityEngine;

namespace Fishing.World
{
    /// <summary>물결 노멀맵, 구름 노이즈, 달 텍스처를 코드로 만든다. 모두 이음매 없이 반복된다.</summary>
    public static class ProceduralTextures
    {
        static Texture2D _waterNormal, _cloudNoise, _moon;

        public static Texture2D WaterNormal => _waterNormal ??= MakeWaterNormal(256);
        public static Texture2D CloudNoise => _cloudNoise ??= MakeCloudNoise(256);
        public static Texture2D Moon => _moon ??= MakeMoon(128);

        // ---- 반복되는 값 노이즈 ----
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 2147483647;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            int Wrap(int v) => ((v % period) + period) % period;
            float a = Hash(Wrap(x0), Wrap(y0), seed), b = Hash(Wrap(x0 + 1), Wrap(y0), seed);
            float c = Hash(Wrap(x0), Wrap(y0 + 1), seed), d = Hash(Wrap(x0 + 1), Wrap(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>u, v는 0~1. 주기가 정수라 텍스처 경계에서 이어진다.</summary>
        static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(u * period, v * period, period, seed + o * 31) * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }

        static Texture2D MakeWaterNormal(int size)
        {
            var h = new float[size, size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                // 길쭉한 잔물결 + 잘게 부서지는 물결
                h[x, y] = Fbm(u, v, 6, 4, 11) * 0.7f + Fbm(u, v, 16, 3, 23) * 0.3f;
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var px = new Color32[size * size];
            const float strength = 6f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (h[(x + 1) % size, y] - h[(x - 1 + size) % size, y]) * strength;
                float dy = (h[x, (y + 1) % size] - h[x, (y - 1 + size) % size]) * strength;
                var n = new Vector3(-dx, -dy, 1f).normalized;
                px[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        static Texture2D MakeCloudNoise(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float n = Fbm(u, v, 4, 5, 77);
                px[y * size + x] = new Color(1f, 1f, 1f, n);
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        static Texture2D MakeMoon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float disc = Mathf.Clamp01((0.62f - r) * 60f);
                float glow = Mathf.Clamp01(1f - r) * 0.35f;
                float crater = Fbm((u + 1f) * 0.5f, (v + 1f) * 0.5f, 4, 3, 5);
                float shade = Mathf.Lerp(0.78f, 1f, crater);
                var c = new Color(shade, shade, shade * 0.95f, Mathf.Max(disc, glow * glow));
                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }
    }
}
