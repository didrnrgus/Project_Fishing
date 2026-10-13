using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 낚시터 목록 화면의 한반도 지도를 코드로 그린다 (대략적인 해안선 다각형 + 제주도).
    /// 지도 좌표: 경도 123.8~131.0, 위도 33.0~42.6.
    /// </summary>
    public static class KoreaMap
    {
        public const float MinLon = 123.8f, MaxLon = 131.0f, MinLat = 33.0f, MaxLat = 42.6f;

        // 대략적인 한반도 해안선 (경도, 위도), 시계 방향
        static readonly Vector2[] Coast =
        {
            new Vector2(124.4f, 40.1f), new Vector2(124.8f, 39.6f), new Vector2(125.3f, 39.5f), new Vector2(125.6f, 39.1f),
            new Vector2(125.2f, 38.7f), new Vector2(124.9f, 38.2f), new Vector2(125.6f, 37.9f), new Vector2(126.1f, 37.7f),
            new Vector2(126.6f, 37.4f), new Vector2(126.4f, 37.0f), new Vector2(126.2f, 36.7f), new Vector2(126.5f, 36.3f),
            new Vector2(126.6f, 35.9f), new Vector2(126.4f, 35.5f), new Vector2(126.3f, 35.0f), new Vector2(126.4f, 34.5f),
            new Vector2(126.9f, 34.4f), new Vector2(127.4f, 34.6f), new Vector2(127.8f, 34.7f), new Vector2(128.3f, 34.8f),
            new Vector2(128.7f, 35.0f), new Vector2(129.1f, 35.1f), new Vector2(129.4f, 35.5f), new Vector2(129.5f, 36.0f),
            new Vector2(129.4f, 36.5f), new Vector2(129.3f, 37.1f), new Vector2(129.0f, 37.6f), new Vector2(128.6f, 38.1f),
            new Vector2(128.3f, 38.6f), new Vector2(127.9f, 39.1f), new Vector2(127.5f, 39.4f), new Vector2(127.6f, 39.8f),
            new Vector2(128.2f, 40.0f), new Vector2(129.0f, 40.6f), new Vector2(129.7f, 40.9f), new Vector2(129.8f, 41.5f),
            new Vector2(130.1f, 42.3f), new Vector2(130.7f, 42.6f), new Vector2(129.0f, 42.6f), new Vector2(128.0f, 42.0f),
            new Vector2(126.9f, 41.8f), new Vector2(126.0f, 41.0f), new Vector2(125.0f, 40.5f),
        };

        static Texture2D _tex;

        public static Texture2D Texture => _tex ??= Render(420, 560);

        public static Vector2 ToUV(float lon, float lat) =>
            new Vector2((lon - MinLon) / (MaxLon - MinLon), (lat - MinLat) / (MaxLat - MinLat));

        static bool InCoast(Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = Coast.Length - 1; i < Coast.Length; j = i++)
            {
                var a = Coast[i];
                var b = Coast[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        static bool InJeju(Vector2 p)
        {
            var d = new Vector2((p.x - 126.55f) / 0.32f, (p.y - 33.38f) / 0.14f);
            return d.sqrMagnitude < 1f;
        }

        static bool IsLand(float lon, float lat) => InCoast(new Vector2(lon, lat)) || InJeju(new Vector2(lon, lat));

        static Texture2D Render(int w, int h)
        {
            var land = new bool[w, h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float lon = Mathf.Lerp(MinLon, MaxLon, (x + 0.5f) / w);
                float lat = Mathf.Lerp(MinLat, MaxLat, (y + 0.5f) / h);
                land[x, y] = IsLand(lon, lat);
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // 해안까지 거리(최대 6px)로 해안선과 얕은 바다를 표현
                int dist = 99;
                for (int r = 1; r <= 6 && dist == 99; r++)
                    for (int k = -r; k <= r && dist == 99; k++)
                    {
                        if (Diff(land, x, y, x + k, y + r, w, h) || Diff(land, x, y, x + k, y - r, w, h) ||
                            Diff(land, x, y, x + r, y + k, w, h) || Diff(land, x, y, x - r, y + k, w, h)) dist = r;
                    }

                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                Color c;
                if (land[x, y])
                {
                    var low = new Color(0.36f, 0.42f, 0.24f);
                    var high = new Color(0.5f, 0.48f, 0.3f);
                    c = Color.Lerp(low, high, noise);
                    if (dist <= 1) c = Color.Lerp(c, new Color(0.85f, 0.8f, 0.6f), 0.6f);
                }
                else
                {
                    var deep = new Color(0.06f, 0.14f, 0.22f);
                    var shallow = new Color(0.12f, 0.3f, 0.36f);
                    c = Color.Lerp(deep, shallow, dist <= 6 ? 1f - dist / 7f : 0f);
                    c = Color.Lerp(c, deep * 1.2f, noise * 0.2f);
                }
                // 격자
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
