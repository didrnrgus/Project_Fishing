using System.Collections.Generic;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 낚시터 목록 화면의 거점 지도 (원작: 거점마다 지도 한 장). 한국은 <see cref="KoreaMap"/>, 해외 거점은 여기서
    /// 대략적인 해안선 다각형(경도, 위도)과 강 선으로 그린다: 동남아(인도차이나·말레이반도), 지중해(이탈리아·그리스·에게해), 아프리카(이집트·나일강).
    /// </summary>
    public static class RegionMap
    {
        sealed class Region
        {
            public float MinLon, MaxLon, MinLat, MaxLat;
            /// <summary>땅 다각형 (Sea면 바다 다각형: 나머지가 땅).</summary>
            public Vector2[][] Polys;
            public bool Sea;
            /// <summary>타원 섬 (경도, 위도, 반지름 경도, 반지름 위도).</summary>
            public Vector4[] Isles = new Vector4[0];
            /// <summary>강 (선을 따라 물 + 둘레 초록 띠).</summary>
            public Vector2[][] Rivers = new Vector2[0][];
            /// <summary>사막 (강가를 빼면 모래색).</summary>
            public bool Desert;
            public Texture2D Tex;
        }

        static readonly Dictionary<string, Region> Regions = new Dictionary<string, Region>
        {
            ["sea"] = new Region
            {
                MinLon = 95f, MaxLon = 112f, MinLat = -1f, MaxLat = 23.6f,
                Polys = new[]
                {
                    V(95f, 24f, 112f, 24f, 112f, 21.6f, 108.5f, 21.6f, 107.5f, 21f, 106.6f, 20.3f, 105.8f, 19f, 106.5f, 17.5f, 107.5f, 16.3f, 108.8f, 15f, 109.3f, 13f,
                      109.2f, 11.5f, 108f, 10.8f, 106.8f, 10.4f, 105.1f, 8.6f, 104.8f, 9.5f, 104.3f, 10.4f, 103f, 11f, 102.4f, 12.2f, 101f, 12.7f, 100.9f, 13.4f,
                      100.2f, 13.5f, 99.9f, 12f, 99.2f, 10.3f, 99.8f, 9.2f, 100.3f, 8.3f, 100.6f, 7f, 101.5f, 6.8f, 102.4f, 6.1f, 103.4f, 4.8f, 103.5f, 3f, 104.3f, 1.5f,
                      103.5f, 1.3f, 102.5f, 2f, 101.3f, 2.9f, 100.4f, 4.5f, 100.3f, 5.8f, 99.7f, 6.8f, 98.5f, 8f, 98.3f, 9f, 98.7f, 10.5f, 98.5f, 12.5f, 98.2f, 13.8f,
                      97.7f, 16.5f, 96.5f, 16.8f, 95f, 16f),
                    V(95.2f, 5.6f, 98f, 4f, 100.5f, 1.5f, 103.5f, -1f, 104.5f, -2f, 102f, -2f, 99f, 0f, 96f, 2.5f, 95.3f, 4.5f),
                    V(108.8f, 1.8f, 110.5f, 1.8f, 112f, 2.8f, 112f, -2f, 110f, -2f, 109f, 0.5f),
                },
                Isles = new[] { new Vector4(109.8f, 19.2f, 1.2f, 0.9f), new Vector4(103.9f, 10.2f, 0.25f, 0.3f) },
                Rivers = new[] { V(105.6f, 23f, 105.8f, 21.5f, 106.6f, 20.4f), V(102.5f, 23.5f, 101.8f, 20.2f, 102.6f, 17.9f, 104.7f, 16.6f, 105.8f, 15.1f, 105.9f, 13.5f, 105.0f, 11.6f, 106.2f, 10.2f) },
            },
            ["med"] = new Region
            {
                MinLon = 12f, MaxLon = 28f, MinLat = 29.5f, MaxLat = 50.8f,
                Polys = new[]
                {
                    V(12f, 50.8f, 28f, 50.8f, 28f, 41.2f, 26.4f, 40.9f, 26f, 40.6f, 24f, 40.7f, 23.7f, 40.2f, 23.3f, 39.9f, 22.6f, 40.3f, 22.9f, 39.3f, 23.3f, 38.6f,
                      24.1f, 38.2f, 23.2f, 37.9f, 23f, 37.5f, 22.8f, 36.5f, 22.4f, 36.4f, 21.7f, 36.8f, 21.1f, 37.8f, 21.3f, 38.4f, 20.7f, 38.9f, 20f, 39.6f, 19.4f, 40.4f,
                      19.5f, 41.8f, 18.5f, 42.4f, 17f, 43.2f, 15.2f, 44.4f, 13.6f, 45.1f, 13f, 45.7f, 12.3f, 45.3f, 12.4f, 44.2f, 13.6f, 43.5f, 14.7f, 42f, 16.1f, 41.4f,
                      18f, 40.6f, 18.5f, 40.1f, 17.2f, 40.4f, 16.6f, 39.6f, 17.1f, 39f, 16.1f, 38f, 15.6f, 38.2f, 15.8f, 39.5f, 15.1f, 40.2f, 14f, 40.9f, 12.5f, 41.7f, 12f, 42f),
                    V(26.2f, 40.4f, 28f, 40.4f, 28f, 36.6f, 27.4f, 37f, 27.2f, 37.9f, 26.4f, 38.4f, 26.8f, 39.4f),
                    V(23.5f, 35.5f, 24.3f, 35.6f, 25.2f, 35.4f, 26.3f, 35.3f, 26.1f, 35f, 24.7f, 34.9f, 23.5f, 35.2f),
                    V(12.4f, 38.1f, 13.4f, 38.2f, 15.6f, 38.3f, 15.1f, 36.7f, 14.3f, 37f, 12.4f, 37.7f),
                    V(12f, 32.9f, 15.2f, 32.3f, 15.7f, 31.4f, 17.5f, 30.8f, 19f, 30.3f, 20f, 31.2f, 20.1f, 32.2f, 21.5f, 32.9f, 23f, 32.6f, 25f, 31.7f, 27f, 31.4f,
                      28f, 31.1f, 28f, 29.5f, 12f, 29.5f),
                },
                // 키클라데스 섬들 (산토리니·낙소스·파로스·밀로스·미코노스)
                Isles = new[]
                {
                    new Vector4(25.4f, 36.42f, 0.13f, 0.09f), new Vector4(25.45f, 37.05f, 0.18f, 0.14f), new Vector4(25.15f, 37.05f, 0.1f, 0.08f),
                    new Vector4(24.42f, 36.7f, 0.14f, 0.08f), new Vector4(25.35f, 37.45f, 0.1f, 0.07f), new Vector4(23.9f, 38.6f, 0.6f, 0.35f),
                },
            },
            ["africa"] = new Region
            {
                MinLon = 28f, MaxLon = 34f, MinLat = 26f, MaxLat = 34f, Sea = true, Desert = true,
                Polys = new[]
                {
                    V(28f, 34f, 34f, 34f, 34f, 31.3f, 33f, 31.1f, 32.3f, 31.3f, 31.9f, 31.5f, 31f, 31.6f, 30.3f, 31.5f, 29.9f, 31.3f, 29f, 30.9f, 28f, 31f),
                    V(32.55f, 29.95f, 33.1f, 29.6f, 33.7f, 28.5f, 34f, 28f, 34f, 26f, 33.95f, 26f, 33.7f, 27.2f, 33f, 28.3f, 32.6f, 29.5f),
                },
                Isles = new[] { new Vector4(30.6f, 29.45f, 0.18f, 0.06f) },
                Rivers = new[]
                {
                    V(32f, 26f, 31.6f, 26.5f, 31.2f, 27.1f, 30.85f, 27.8f, 30.75f, 28.1f, 31.05f, 28.9f, 31.2f, 29.6f, 31.25f, 30.05f, 30.9f, 30.6f, 30.4f, 31.45f),
                    V(31.25f, 30.05f, 31.4f, 30.6f, 31.8f, 31.5f),
                },
            },
        };

        static Vector2[] V(params float[] a)
        {
            var v = new Vector2[a.Length / 2];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector2(a[i * 2], a[i * 2 + 1]);
            return v;
        }

        public static Texture2D Texture(string hub)
        {
            if (!Regions.TryGetValue(hub, out var r)) return KoreaMap.Texture;
            return r.Tex ??= Render(r, 420, 560);
        }

        public static Vector2 ToUV(string hub, float lon, float lat)
        {
            if (!Regions.TryGetValue(hub, out var r)) return KoreaMap.ToUV(lon, lat);
            return new Vector2((lon - r.MinLon) / (r.MaxLon - r.MinLon), (lat - r.MinLat) / (r.MaxLat - r.MinLat));
        }

        static bool InPoly(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                var a = poly[i];
                var b = poly[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        static float RiverDist(Region r, Vector2 p)
        {
            float best = 99f;
            foreach (var line in r.Rivers)
                for (int i = 0; i < line.Length - 1; i++)
                {
                    var a = line[i];
                    var ab = line[i + 1] - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
                }
            return best;
        }

        static Texture2D Render(Region r, int w, int h)
        {
            float degPerPx = (r.MaxLon - r.MinLon) / w;
            var land = new bool[w, h];
            var river = new float[w, h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(Mathf.Lerp(r.MinLon, r.MaxLon, (x + 0.5f) / w), Mathf.Lerp(r.MinLat, r.MaxLat, (y + 0.5f) / h));
                bool inPoly = false;
                foreach (var poly in r.Polys) if (InPoly(poly, p)) { inPoly = true; break; }
                bool isLand = r.Sea ? !inPoly : inPoly;
                // 타원: 바다 지도에서는 섬(땅), 아프리카(Sea) 지도에서는 오아시스 호수(물)
                foreach (var isle in r.Isles)
                {
                    var d = new Vector2((p.x - isle.x) / isle.z, (p.y - isle.y) / isle.w);
                    if (d.sqrMagnitude < 1f) isLand = !r.Sea;
                }
                float rd = r.Rivers.Length > 0 ? RiverDist(r, p) / degPerPx : 99f;
                river[x, y] = rd;
                if (isLand && rd < 1.6f) isLand = false;
                land[x, y] = isLand;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dist = 99;
                for (int k = 1; k <= 6 && dist == 99; k++)
                    for (int m = -k; m <= k && dist == 99; m++)
                        if (Diff(land, x, y, x + m, y + k, w, h) || Diff(land, x, y, x + m, y - k, w, h) ||
                            Diff(land, x, y, x + k, y + m, w, h) || Diff(land, x, y, x - k, y + m, w, h)) dist = k;
                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                Color c;
                if (land[x, y])
                {
                    var low = new Color(0.36f, 0.42f, 0.24f);
                    var high = new Color(0.5f, 0.48f, 0.3f);
                    c = Color.Lerp(low, high, noise);
                    // 사막: 강가 초록 띠 밖은 모래색
                    if (r.Desert) c = Color.Lerp(Color.Lerp(new Color(0.72f, 0.6f, 0.4f), new Color(0.82f, 0.7f, 0.48f), noise), c, Mathf.Clamp01(1f - (river[x, y] - 3f) / 6f));
                    if (dist <= 1) c = Color.Lerp(c, new Color(0.85f, 0.8f, 0.6f), 0.6f);
                }
                else
                {
                    var deep = new Color(0.06f, 0.14f, 0.22f);
                    var shallow = new Color(0.12f, 0.3f, 0.36f);
                    c = Color.Lerp(deep, shallow, dist <= 6 ? 1f - dist / 7f : 0f);
                    c = Color.Lerp(c, deep * 1.2f, noise * 0.2f);
                    if (river[x, y] < 1.6f) c = new Color(0.16f, 0.36f, 0.42f);
                }
                if (x % 42 == 0 || y % 42 == 0) c = Color.Lerp(c, Color.white, 0.07f);
                px[y * w + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static bool Diff(bool[,] land, int x0, int y0, int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            return land[x, y] != land[x0, y0];
        }
    }
}
