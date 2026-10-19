using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 덕장에 거는 명태 한 쌍 (청진포구 명태 덕장). 예전에는 캡슐 + 구(머리가 아래)였다.
    /// 실제 덕장처럼 두 마리를 코에서 새끼줄로 엮어 장대 양쪽에 걸치고, 머리가 위·꼬리가 아래로 늘어진다.
    /// 배는 갈라 속이 비었고(검붉은 속살), 입은 벌어졌고, 눈은 꺼졌다. 바닷바람에 마르며 몸이 납작하고 쭈글쭈글해진다.
    /// 메쉬 한 개에 두 마리 + 줄을 모두 담는다(원점 = 장대 위 줄이 걸치는 점, 장대는 x축).
    /// 덕장 앞(±z)에서 보면 옆모습이 보이게 납작한 옆면이 z를 보고, 등은 장대 방향(±x)을 본다. 텍스처는 코드로 만든다.
    /// </summary>
    public static class DriedPollock
    {
        const float Length = 0.52f;   // 명태 한 마리 길이 (m)
        const int Seg = 10;           // 둘레
        const int Rings = 14;         // 길이 방향

        static Mesh _pair;
        static readonly Dictionary<int, Material> _mats = new Dictionary<int, Material>();

        /// <summary>명태 한 쌍 메쉬 (공유).</summary>
        public static Mesh Pair => _pair ??= BuildPair();

        /// <summary>마른 정도가 다른 명태 재질 3가지 (0 = 덜 마른 회색빛, 2 = 노랗게 마른 황태).</summary>
        public static Material Material(int variant)
        {
            variant = Mathf.Clamp(variant, 0, 2);
            if (_mats.TryGetValue(variant, out var m)) return m;
            var tint = variant switch
            {
                0 => new Color(0.86f, 0.84f, 0.8f),
                1 => new Color(1f, 0.93f, 0.8f),
                _ => new Color(1.05f, 0.9f, 0.68f),
            };
            m = new Material(Shader.Find("Standard")) { color = tint, mainTexture = Texture };
            m.SetFloat("_Glossiness", variant == 0 ? 0.38f : 0.22f);
            _mats[variant] = m;
            return m;
        }

        // ---- 메쉬 ----

        /// <summary>몸 단면 크기: t = 0(주둥이) ~ 1(꼬리자루). (등배 높이, 좌우 두께) 반지름 m.</summary>
        static Vector2 Profile(float t)
        {
            float h = t < 0.08f ? Mathf.Lerp(0.018f, 0.045f, t / 0.08f)          // 벌린 입
                    : t < 0.3f ? Mathf.Lerp(0.045f, 0.058f, (t - 0.08f) / 0.22f) // 머리 → 어깨
                    : Mathf.Lerp(0.058f, 0.012f, Mathf.Pow((t - 0.3f) / 0.7f, 1.3f));
            float w = h * (t < 0.25f ? 0.62f : 0.42f); // 마르면서 납작해진 몸통 (머리는 덜 납작)
            return new Vector2(h, w);
        }

        static Mesh BuildPair()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            // 두 마리: 장대(x축) 앞뒤로 하나씩, 등은 서로 반대쪽(±x)
            AddFish(verts, uvs, tris, new Vector3(0f, -0.06f, 0.035f), +1f, 0.4f);
            AddFish(verts, uvs, tris, new Vector3(0.01f, -0.065f, -0.035f), -1f, -0.3f);
            // 새끼줄: 두 코를 잇고 장대 위로 걸친다 (가는 상자 세 개)
            AddRope(verts, uvs, tris, new Vector3(0f, -0.07f, 0.035f), new Vector3(0f, 0.03f, 0.02f));
            AddRope(verts, uvs, tris, new Vector3(0f, 0.03f, 0.02f), new Vector3(0f, 0.03f, -0.02f));
            AddRope(verts, uvs, tris, new Vector3(0f, 0.03f, -0.02f), new Vector3(0.01f, -0.075f, -0.035f));

            var mesh = new Mesh { name = "DriedPollockPair" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <param name="nose">주둥이(맨 위) 위치</param>
        /// <param name="facing">+1이면 등이 +x, -1이면 -x</param>
        /// <param name="sway">아래로 갈수록 살짝 휘는 정도</param>
        static void AddFish(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 nose, float facing, float sway)
        {
            int start = verts.Count;
            for (int r = 0; r <= Rings; r++)
            {
                float t = r / (float)Rings;
                var p = Profile(t);
                // 배를 가른 자리(t 0.28~0.62): 배쪽 단면이 안으로 꺼진다
                float slit = Mathf.Clamp01(Mathf.Min((t - 0.28f) / 0.05f, (0.62f - t) / 0.05f));
                // 말라 비틀린 몸: 길이 방향으로 등이 살짝 굽고 옆으로 휜다
                float y = -t * Length;
                float bend = Mathf.Sin(t * Mathf.PI) * 0.025f;
                for (int s = 0; s <= Seg; s++)
                {
                    float a = s / (float)Seg * Mathf.PI * 2f;  // 0 = 등, π = 배
                    float dorsal = Mathf.Cos(a), side = Mathf.Sin(a);
                    float hr = p.x;
                    if (dorsal < -0.3f) hr *= 1f - slit * 0.75f * Mathf.InverseLerp(-0.3f, -1f, dorsal); // 배 쪽이 꺼짐
                    // 쭈글쭈글한 주름 (길이 방향으로 잔 물결)
                    float wrinkle = 1f + 0.06f * Mathf.Sin(t * 70f + side * 3f) * Mathf.Clamp01((t - 0.1f) * 4f);
                    var v = new Vector3((dorsal * hr * wrinkle + bend * 0.6f) * facing, y, side * p.y * wrinkle + sway * bend);
                    verts.Add(nose + v);
                    uvs.Add(new Vector2(s / (float)Seg, 1f - t * 0.82f)); // 몸은 텍스처 위 82%
                }
            }
            int w = Seg + 1;
            for (int r = 0; r < Rings; r++)
            for (int s = 0; s < Seg; s++)
            {
                int i = start + r * w + s;
                if (facing > 0) { tris.Add(i); tris.Add(i + 1); tris.Add(i + w); tris.Add(i + 1); tris.Add(i + w + 1); tris.Add(i + w); }
                else { tris.Add(i); tris.Add(i + w); tris.Add(i + 1); tris.Add(i + 1); tris.Add(i + w); tris.Add(i + w + 1); }
            }
            // 주둥이 끝 닫기
            int cap = verts.Count;
            verts.Add(nose + new Vector3(0f, 0.012f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));
            for (int s = 0; s < Seg; s++)
            {
                if (facing > 0) { tris.Add(cap); tris.Add(start + s + 1); tris.Add(start + s); }
                else { tris.Add(cap); tris.Add(start + s); tris.Add(start + s + 1); }
            }

            var tail = nose + new Vector3(0f, -Length, 0f);
            // 꼬리지느러미: 등배로 갈라진 V자 (양면)
            Fin(verts, uvs, tris, tail + new Vector3(0f, 0.01f, 0f),
                tail + new Vector3(0.045f * facing, -0.075f, 0f), tail + new Vector3(0f, -0.045f, 0f), tail + new Vector3(-0.045f * facing, -0.075f, 0f));
            // 등지느러미 세 개 (명태는 등지느러미가 셋), 뒷지느러미 두 개
            foreach (var (t0, t1, hgt) in new[] { (0.3f, 0.4f, 0.03f), (0.45f, 0.57f, 0.028f), (0.62f, 0.74f, 0.022f) })
                FinOnBody(verts, uvs, tris, nose, t0, t1, hgt, facing, +1f);
            foreach (var (t0, t1, hgt) in new[] { (0.5f, 0.6f, 0.02f), (0.64f, 0.75f, 0.018f) })
                FinOnBody(verts, uvs, tris, nose, t0, t1, hgt, facing, -1f);
        }

        static void FinOnBody(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 nose, float t0, float t1, float hgt, float facing, float dir)
        {
            float r0 = Profile(t0).x, r1 = Profile(t1).x;
            float bend0 = Mathf.Sin(t0 * Mathf.PI) * 0.025f * 0.6f, bend1 = Mathf.Sin(t1 * Mathf.PI) * 0.025f * 0.6f;
            var a = nose + new Vector3((dir * r0 * 0.9f + bend0) * facing, -t0 * Length, 0f);
            var b = nose + new Vector3((dir * r1 * 0.9f + bend1) * facing, -t1 * Length, 0f);
            var tip = nose + new Vector3((dir * (r0 + hgt) + bend0) * facing, -(t0 + (t1 - t0) * 0.35f) * Length, 0f);
            Fin(verts, uvs, tris, a, tip, b, b);
        }

        /// <summary>얇은 지느러미 (양면 삼각형 두 개). 텍스처 아래쪽 지느러미 칸(v 0~0.15)을 쓴다.</summary>
        static void Fin(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 root, Vector3 a, Vector3 mid, Vector3 b)
        {
            int i = verts.Count;
            verts.Add(root); verts.Add(a); verts.Add(mid); verts.Add(b);
            uvs.Add(new Vector2(0.5f, 0.15f)); uvs.Add(new Vector2(0.1f, 0.02f)); uvs.Add(new Vector2(0.5f, 0.05f)); uvs.Add(new Vector2(0.9f, 0.02f));
            tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1, i, i + 2, i + 3, i, i + 3, i + 2 });
        }

        static void AddRope(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 from, Vector3 to)
        {
            const float r = 0.006f;
            var dir = (to - from).normalized;
            var side = Vector3.Cross(dir, Vector3.right).sqrMagnitude > 1e-4f ? Vector3.Cross(dir, Vector3.right).normalized : Vector3.forward;
            var up = Vector3.Cross(side, dir).normalized;
            int i = verts.Count;
            foreach (var end in new[] { from, to })
            {
                verts.Add(end + (side + up) * r); verts.Add(end + (side - up) * r);
                verts.Add(end + (-side - up) * r); verts.Add(end + (-side + up) * r);
                for (int k = 0; k < 4; k++) uvs.Add(new Vector2(0.95f, 0.08f)); // 텍스처 오른쪽 아래 새끼줄 색
            }
            for (int k = 0; k < 4; k++)
            {
                int a = i + k, b = i + (k + 1) % 4, c = a + 4, d = b + 4;
                tris.AddRange(new[] { a, c, b, b, c, d, a, b, c, b, d, c });
            }
        }

        // ---- 텍스처 (u = 둘레 0 등 → 0.5 배 → 1 등, v = 1 주둥이 → 0.18 꼬리, 0~0.15 지느러미) ----

        static Texture2D _tex;
        static Texture2D Texture => _tex ??= MakeTexture(128, 256);

        static Texture2D MakeTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "DriedPollock" };
            var px = new Color[w * h];
            var back = new Color(0.42f, 0.36f, 0.27f);    // 등: 마른 갈색
            var flank = new Color(0.78f, 0.66f, 0.45f);   // 옆구리: 누런 황태 빛
            var belly = new Color(0.86f, 0.8f, 0.66f);    // 배 가장자리: 희끄무레
            var cavity = new Color(0.45f, 0.24f, 0.18f);  // 가른 배 속: 검붉은 살
            var fin = new Color(0.36f, 0.3f, 0.22f);
            var rope = new Color(0.62f, 0.52f, 0.34f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                Color c;
                if (v < 0.16f)
                {
                    // 지느러미 칸 (+ 오른쪽 아래 새끼줄 색)
                    c = u > 0.9f ? rope : Color.Lerp(fin, flank, Mathf.PerlinNoise(u * 30f, v * 40f) * 0.4f);
                    float rays = 0.85f + 0.15f * Mathf.Sin(u * 90f);
                    c *= rays;
                }
                else
                {
                    float t = (1f - v) / 0.82f;                 // 0 주둥이 ~ 1 꼬리
                    float ang = Mathf.Abs(u - 0.5f) * 2f;        // 1 = 등, 0 = 배
                    c = ang > 0.55f ? Color.Lerp(flank, back, Mathf.SmoothStep(0f, 1f, (ang - 0.55f) / 0.45f))
                                    : Color.Lerp(belly, flank, Mathf.SmoothStep(0f, 1f, ang / 0.55f));
                    // 옆줄: 등 쪽에 가는 어두운 줄
                    float lateral = Mathf.Abs(ang - 0.7f);
                    if (lateral < 0.02f && t > 0.25f) c *= 0.8f;
                    // 배를 가른 자리: 배 가운데가 검붉은 속살
                    if (t > 0.28f && t < 0.62f && ang < 0.22f)
                        c = Color.Lerp(cavity, c, Mathf.SmoothStep(0f, 1f, ang / 0.22f) * 0.6f);
                    // 아가미 뚜껑 경계 (머리와 몸 사이 어두운 곡선)
                    if (Mathf.Abs(t - (0.22f + 0.03f * ang)) < 0.008f) c *= 0.65f;
                    // 벌린 입 안
                    if (t < 0.05f) c = Color.Lerp(new Color(0.25f, 0.16f, 0.12f), c, t / 0.05f);
                    // 꺼진 눈 (좌우 옆면 u = 0.25·0.75)
                    float eu = Mathf.Min(Mathf.Abs(u - 0.25f), Mathf.Abs(u - 0.75f));
                    float er = Mathf.Sqrt(eu * eu * 4f + Mathf.Pow((t - 0.1f) * 1.2f, 2f));
                    if (er < 0.05f) c = Color.Lerp(new Color(0.12f, 0.1f, 0.08f), new Color(0.55f, 0.5f, 0.42f), Mathf.SmoothStep(0.02f, 0.05f, er));
                    // 마른 주름·비늘 자국·소금기
                    float wrinkle = 0.9f + 0.1f * Mathf.Sin(t * 260f + Mathf.PerlinNoise(u * 8f, t * 6f) * 6f);
                    float speck = Mathf.PerlinNoise(u * 60f, t * 120f);
                    c *= wrinkle * (0.92f + speck * 0.16f);
                    if (speck > 0.78f) c = Color.Lerp(c, new Color(0.92f, 0.9f, 0.85f), 0.35f); // 하얗게 일어난 소금기
                }
                c.a = 1f;
                px[y * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }
    }
}
