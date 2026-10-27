using System;
using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 집 부품 (한국 바닷가 집·해외 집 양식이 같이 쓴다). 상자에 판을 붙이던 것을 실제 짓는 방식으로:
    /// - 벽: 창·문 자리를 진짜로 비우고 나머지를 블록으로 쌓는다 → 창이 벽 안으로 들어가 옆면(문설주)에 그늘이 진다. 아치는 위 귀퉁이를 채운다
    /// - 창·문: 새시 테 + 미서기 유리 두 장(앞뒤로 어긋남) + 창턱, 문짝·손잡이·문턱, 가게 셔터
    /// - 박공지붕: 골강판(물결 단면이 처마 끝에 보인다)·기와(줄마다 아래 끝이 들린 반원 기와)·평기와(층층) 곡면 + 밑판 + 처마 판·박공 마감판·용마루 + 박공 벽 삼각형
    /// - 평지붕: 슬래브 + 초록 우레탄 방수칠 + 난간 턱 + 갓돌, 시멘트 블록 담장
    /// 모든 조각은 재질별 <see cref="BlockMesh"/>에 모아 집 하나에 렌더러 몇 개로 굽는다 (예전엔 프리미티브 수십 개).
    /// </summary>
    public static class HouseKit
    {
        public enum Hole { Window, Door, Arch, ArchWindow, Shop }

        /// <summary>벽에 낸 구멍: X = 벽 가운데 기준 가로 위치, Y = 아랫변 높이, W·H = 너비·높이.</summary>
        public struct Opening
        {
            public float X, Y, W, H;
            public Hole Kind;
            public Opening(float x, float y, float w, float h, Hole kind = Hole.Window)
            {
                X = x; Y = y; W = w; H = h; Kind = kind;
            }
        }

        /// <summary>집 한 채의 재질별 버퍼. 다 지은 뒤 <see cref="Emit"/>.</summary>
        public sealed class Parts
        {
            public readonly BlockMesh Wall = new BlockMesh(), Trim = new BlockMesh(), Frame = new BlockMesh(), Glass = new BlockMesh(), Glass2 = new BlockMesh(),
                Door = new BlockMesh(), Roof = new BlockMesh(), Under = new BlockMesh(), Metal = new BlockMesh(), Accent = new BlockMesh();
            public readonly List<Vector3[]> Pipes = new List<Vector3[]>();
            public Material WallMat, TrimMat, FrameMat, GlassMat, Glass2Mat, DoorMat, RoofMat, UnderMat, MetalMat, AccentMat, PipeMat;

            public void Emit(Transform parent)
            {
                Wall.Emit(parent, "Walls", Mats.VertexAO(WallMat));
                Trim.Emit(parent, "Trim", Mats.VertexAO(TrimMat ?? WallMat));
                Frame.Emit(parent, "Frames", FrameMat);
                Glass.Emit(parent, "Glass", GlassMat);
                Glass2.Emit(parent, "Glass2", Glass2Mat ?? GlassMat);
                Door.Emit(parent, "Doors", DoorMat);
                Roof.Emit(parent, "Roof", Mats.VertexAO(RoofMat));
                Under.Emit(parent, "RoofUnder", Mats.VertexAO(UnderMat ?? TrimMat ?? WallMat));
                Metal.Emit(parent, "Metal", MetalMat ?? FrameMat);
                Accent.Emit(parent, "Accent", Mats.VertexAO(AccentMat ?? WallMat));
                if (Pipes.Count > 0)
                {
                    var go = new GameObject("Pipes");
                    go.transform.SetParent(parent, false);
                    go.AddComponent<MeshFilter>().sharedMesh = MapKit.Tubes(Pipes, _ => 0.045f, 6);
                    go.AddComponent<MeshRenderer>().sharedMaterial = PipeMat ?? MetalMat ?? FrameMat;
                }
            }
        }

        /// <summary>벽 그늘: 땅에서 1.2m까지 어둡다가(튄 흙·습기) 위로 밝아진다. 처마 밑 0.4m도 살짝 어둡다.</summary>
        public static Func<Vector3, Color> WallShade(float groundY, float topY) => p =>
        {
            float g = Mathf.Lerp(0.72f, 1f, Mathf.InverseLerp(groundY, groundY + 1.2f, p.y));
            g *= Mathf.Lerp(1f, 0.86f, Mathf.InverseLerp(topY - 0.45f, topY, p.y));
            return new Color(g, g, g, 1f);
        };

        // ---------------- 벽 ----------------

        /// <summary>
        /// 벽 한 면 (집 로컬): origin = 바깥 면 가운데 아래, rot의 +z = 바깥, len = 가로, y0~y1, t = 두께(안쪽 -z로).
        /// 구멍 자리를 비우고 나머지를 가로줄·세로칸 블록으로 쌓는다 (맞닿은 면은 벽 속이라 안 보인다). 아치 구멍은 위 두 귀퉁이를 채운다.
        /// </summary>
        public static void Wall(BlockMesh buf, Vector3 origin, Quaternion rot, float len, float y0, float y1, float t, IList<Opening> holes, Func<Vector3, Color> shade)
        {
            // 창 밑 빗물 얼룩: 창턱 높이 꼭짓점만 어둡게 → 그 아래 블록이 위에서 아래로 옅어지는 얼룩이 된다
            if (holes != null && holes.Count > 0)
            {
                var baseShade = shade;
                var inv = Quaternion.Inverse(rot);
                shade = wp =>
                {
                    var c = baseShade(wp);
                    var l = inv * (wp - origin);
                    foreach (var o in holes)
                        if ((o.Kind == Hole.Window || o.Kind == Hole.ArchWindow) && Mathf.Abs(l.x - o.X) < o.W * 0.5f + 0.01f && Mathf.Abs(l.y - o.Y) < 0.01f)
                            return c * 0.78f;
                    return c;
                };
            }
            var xs = new List<float> { -len * 0.5f, len * 0.5f };
            var ys = new List<float> { y0, y1 };
            if (holes != null)
                foreach (var o in holes)
                {
                    xs.Add(Mathf.Clamp(o.X - o.W * 0.5f, -len * 0.5f, len * 0.5f));
                    xs.Add(Mathf.Clamp(o.X + o.W * 0.5f, -len * 0.5f, len * 0.5f));
                    ys.Add(Mathf.Clamp(o.Y, y0, y1));
                    ys.Add(Mathf.Clamp(o.Y + o.H, y0, y1));
                }
            Dedup(xs);
            Dedup(ys);
            bool Solid(float x, float y)
            {
                if (holes == null) return true;
                foreach (var o in holes)
                    if (x > o.X - o.W * 0.5f && x < o.X + o.W * 0.5f && y > o.Y && y < o.Y + o.H) return false;
                return true;
            }
            for (int j = 0; j < ys.Count - 1; j++)
            {
                float ya = ys[j], yb = ys[j + 1], cy = (ya + yb) * 0.5f;
                int i = 0;
                while (i < xs.Count - 1)
                {
                    if (!Solid((xs[i] + xs[i + 1]) * 0.5f, cy)) { i++; continue; }
                    int k = i;
                    while (k < xs.Count - 1 && Solid((xs[k] + xs[k + 1]) * 0.5f, cy)) k++;
                    float xa = xs[i], xb = xs[k];
                    buf.Block(origin + rot * new Vector3((xa + xb) * 0.5f, cy, -t * 0.5f), new Vector3(xb - xa, yb - ya, t), 0f, rot, shade);
                    i = k;
                }
            }
            if (holes == null) return;
            // 아치: 반원 위 두 귀퉁이를 벽으로 채운다 (귀퉁이 꼭짓점에서 부채꼴로 다 보이는 모양)
            foreach (var o in holes)
            {
                if (o.Kind != Hole.Arch) continue;
                float r = o.W * 0.5f, cyA = o.Y + o.H - r;
                var c = shade(origin + rot * new Vector3(o.X, o.Y + o.H, 0f));
                foreach (float side in new[] { -1f, 1f })
                {
                    var prof = new List<Vector2> { new Vector2(o.X + side * r, o.Y + o.H) };
                    const int n = 7;
                    for (int k = 0; k <= n; k++)
                    {
                        float a = Mathf.PI * 0.5f * k / n; // 꼭대기(0) → 옆(90°)
                        prof.Add(new Vector2(o.X + side * Mathf.Sin(a) * r, cyA + Mathf.Cos(a) * r));
                    }
                    if (side > 0f) prof.Reverse(1, prof.Count - 1);
                    buf.Extrude(prof, -t, 0f, origin, rot, c, c, true);
                }
            }
        }

        static void Dedup(List<float> v)
        {
            v.Sort();
            for (int i = v.Count - 1; i > 0; i--)
                if (v[i] - v[i - 1] < 0.005f) v.RemoveAt(i);
        }

        /// <summary>
        /// 구멍 채우기: 창 = 새시 테 + 미서기 유리 두 장(앞뒤로 어긋남, 하나는 커튼 친 색) + 바깥 창턱, 문 = 테 + 문짝(위 작은 유리) + 손잡이 + 문턱,
        /// 아치 = 안쪽에 문짝(귀퉁이는 벽에 가려 아치 모양으로 보인다), 가게 = 셔터 골판. inset = 바깥 면에서 들어간 깊이.
        /// </summary>
        public static void Fill(Parts p, Vector3 origin, Quaternion rot, Opening o, float t, System.Random rng, float inset = 0.12f, bool sill = true)
        {
            inset = Mathf.Min(inset, t - 0.03f);
            Vector3 L(float x, float y, float z) => origin + rot * new Vector3(x, y, z);
            var white = Color.white;
            const float f = 0.05f; // 새시 굵기
            float z = -inset;
            void Ring(BlockMesh b, bool bottom)
            {
                b.Block(L(o.X, o.Y + o.H - f * 0.5f, z), new Vector3(o.W, f, 0.07f), 0f, rot, white, white);
                if (bottom) b.Block(L(o.X, o.Y + f * 0.5f, z), new Vector3(o.W, f, 0.07f), 0f, rot, white, white);
                b.Block(L(o.X - o.W * 0.5f + f * 0.5f, o.Y + o.H * 0.5f, z), new Vector3(f, o.H, 0.07f), 0f, rot, white, white);
                b.Block(L(o.X + o.W * 0.5f - f * 0.5f, o.Y + o.H * 0.5f, z), new Vector3(f, o.H, 0.07f), 0f, rot, white, white);
            }
            switch (o.Kind)
            {
                case Hole.Window:
                {
                    Ring(p.Frame, true);
                    // 미서기 두 짝: 왼쪽 짝이 앞, 오른쪽 짝이 뒤 (겹친 가운데 새시)
                    float hw = (o.W - f * 2f) * 0.5f, gh = o.H - f * 2f;
                    bool curtainLeft = rng.NextDouble() < 0.5;
                    (curtainLeft ? p.Glass2 : p.Glass).Block(L(o.X - hw * 0.5f, o.Y + o.H * 0.5f, z + 0.012f), new Vector3(hw + 0.02f, gh, 0.012f), 0f, rot, white, white);
                    (curtainLeft ? p.Glass : p.Glass2).Block(L(o.X + hw * 0.5f, o.Y + o.H * 0.5f, z - 0.012f), new Vector3(hw + 0.02f, gh, 0.012f), 0f, rot, white, white);
                    p.Frame.Block(L(o.X + 0.01f, o.Y + o.H * 0.5f, z + 0.005f), new Vector3(0.045f, gh, 0.05f), 0f, rot, white, white);
                    if (sill)
                    {
                        var c = new Color(0.9f, 0.9f, 0.9f);
                        // 윗면을 구멍 아랫변보다 5mm 올려 문설주 바닥 면과 겹쳐 깜빡이지 않게
                        p.Trim.Block(L(o.X, o.Y - 0.03f, 0.04f), new Vector3(o.W + 0.14f, 0.07f, inset + 0.1f), 0.015f, rot, c, c * 0.8f);
                    }
                    break;
                }
                case Hole.Door:
                {
                    Ring(p.Frame, false);
                    float dw = o.W - f * 2f, dh = o.H - f;
                    p.Door.Block(L(o.X, o.Y + dh * 0.5f, z - 0.01f), new Vector3(dw, dh, 0.045f), 0.008f, rot, white, white);
                    // 문 위쪽 작은 유리 (현관 새시 문 느낌)
                    p.Glass.Block(L(o.X, o.Y + dh * 0.72f, z + 0.015f), new Vector3(dw * 0.62f, dh * 0.3f, 0.01f), 0f, rot, white, white);
                    p.Metal.Block(L(o.X + dw * 0.36f, o.Y + 1.0f, z + 0.04f), new Vector3(0.12f, 0.035f, 0.05f), 0.01f, rot, white, white);
                    var c = new Color(0.85f, 0.85f, 0.85f);
                    p.Trim.Block(L(o.X, o.Y + 0.06f, 0.25f - inset * 0.5f), new Vector3(o.W + 0.5f, 0.12f, 0.5f + inset), 0.02f, rot, c, c * 0.75f);
                    break;
                }
                case Hole.Arch:
                {
                    p.Door.Block(L(o.X, o.Y + o.H * 0.5f, z), new Vector3(o.W + 0.02f, o.H, 0.05f), 0f, rot, white, white);
                    break;
                }
                case Hole.ArchWindow:
                {
                    // 아치 창: 안쪽 유리 판(귀퉁이는 벽에 가려 아치로 보인다) + 가운데 살 + 바깥 창턱
                    p.Glass.Block(L(o.X, o.Y + o.H * 0.5f, z), new Vector3(o.W + 0.02f, o.H, 0.02f), 0f, rot, white, white);
                    p.Frame.Block(L(o.X, o.Y + (o.H - o.W * 0.5f) * 0.5f, z + 0.02f), new Vector3(0.04f, o.H - o.W * 0.5f, 0.03f), 0f, rot, white, white);
                    if (sill)
                    {
                        var c = new Color(0.92f, 0.92f, 0.92f);
                        p.Trim.Block(L(o.X, o.Y - 0.03f, 0.04f), new Vector3(o.W + 0.14f, 0.07f, inset + 0.1f), 0.015f, rot, c, c * 0.8f);
                    }
                    break;
                }
                case Hole.Shop:
                {
                    // 셔터: 가로 골판 (반쯤 올라간 곳은 어두운 가게 안)
                    float up = (float)rng.NextDouble() < 0.5f ? 0.55f : 0f;
                    float sh = o.H * (1f - up);
                    p.Metal.Surface(1, Mathf.Max(2, Mathf.RoundToInt(sh / 0.04f)), (u, v) =>
                        L(o.X - o.W * 0.5f + u * o.W, o.Y + o.H - v * sh, z + 0.012f * Mathf.Sin(v * sh / 0.08f * Mathf.PI * 2f)), rot * Vector3.forward, (u, v) => white);
                    if (up > 0f) p.Glass.Block(L(o.X, o.Y + (o.H - sh) * 0.5f, z - 0.15f), new Vector3(o.W, o.H - sh, 0.02f), 0f, rot, white, white);
                    p.Metal.Block(L(o.X, o.Y + o.H + 0.12f, 0.05f), new Vector3(o.W + 0.1f, 0.24f, 0.18f + inset), 0.02f, rot, white, white); // 셔터 통
                    break;
                }
            }
        }

        /// <summary>
        /// 네 벽 상자 (집 로컬, 가운데 바닥 원점): 앞(+z)·뒤(-z)는 가로 w 전체, 오른쪽(+x)·왼쪽(-x)은 그 사이. 각 벽의 구멍 목록(null = 막힌 벽)을 비우고 채운다.
        /// round > 0이면 네 세로 모서리에 둥글게 깎은 기둥을 덧대 회벽 덧바른 모서리처럼 (키클라데스·흙집).
        /// </summary>
        public static void Box(Parts p, float w, float d, float y0, float y1, float t, IList<Opening> front, IList<Opening> back, IList<Opening> right, IList<Opening> left,
            Func<Vector3, Color> shade, System.Random rng, float round = 0f, float inset = 0.12f)
        {
            var sides = new[]
            {
                (new Vector3(0f, 0f, d * 0.5f), Quaternion.identity, w, front), (new Vector3(0f, 0f, -d * 0.5f), Quaternion.Euler(0f, 180f, 0f), w, back),
                (new Vector3(w * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), d - t * 2f, right), (new Vector3(-w * 0.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), d - t * 2f, left),
            };
            foreach (var (o, r, len, holes) in sides)
            {
                Wall(p.Wall, o, r, len, y0, y1, t, holes, shade);
                if (holes != null) foreach (var h in holes) Fill(p, o, r, h, t, rng, inset);
            }
            if (round <= 0f) return;
            foreach (var (sx, sz) in new[] { (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, -1f) })
                p.Wall.Block(new Vector3(sx * (w * 0.5f - round * 0.5f + 0.02f), (y0 + y1) * 0.5f, sz * (d * 0.5f - round * 0.5f + 0.02f)),
                    new Vector3(round, y1 - y0, round), round * 0.45f, Quaternion.identity, shade);
        }

        /// <summary>집 아래 자식 (돌리거나 옮겨 얹는 지붕용): 그 안에 따로 Parts를 굽는다.</summary>
        public static Transform Rig(Transform parent, Vector3 pos, float yaw, string name = "RoofRig")
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        // ---------------- 지붕 ----------------

        public enum Skin { Corrugated, Barrel, FlatTile, Thatch, Smooth }

        /// <summary>
        /// 박공지붕 (집 로컬, 용마루 = x 방향). 벽 윗면 wallTop, 벽 가로 wallW·깊이 wallD, 용마루까지 높이 rise, 처마 내밀기 ox(박공 쪽)·oz(처마 쪽).
        /// 지붕판은 벽 위 thick 두께로 얹히고 처마 끝까지 같은 기울기. 피복:
        ///  - Corrugated 골강판: 20cm 골(사다리꼴에 가까운 물결), 처마 끝에 물결 단면이 보인다
        ///  - Barrel 기와: 26cm 반원 기와를 32cm 줄마다 아래 끝을 들어 층이 지게
        ///  - FlatTile 평기와: 줄마다 층만 (타이·초가 얹을 판)
        /// 밑판(처마 밑), 처마 판·박공 마감판, 용마루 덮개, 박공 벽 삼각형(gableWall 버퍼, 벽 두께 t), 물받이 홈통.
        /// </summary>
        public static void GableRoof(Parts p, float wallTop, float wallW, float wallD, float rise, float ox, float oz, Skin skin,
            BlockMesh gableWall = null, float t = 0.25f, float thick = 0.1f, bool gutter = true, Func<Vector3, Color> wallShade = null)
        {
            float k = rise / (wallD * 0.5f);                 // 기울기
            float len = wallW + ox * 2f, half = wallD * 0.5f + oz;
            float slopeLen = Mathf.Sqrt(half * half + (k * half) * (k * half));
            var white = Color.white;
            float Y(float z) => wallTop + thick + rise - k * Mathf.Abs(z);
            foreach (float s in new[] { 1f, -1f })
            {
                var n = new Vector3(0f, half, s * k * half).normalized;  // 바깥(위) 법선
                Vector3 Base(float u, float v) => new Vector3(-len * 0.5f + u * len, Y(s * v * half), s * v * half);
                Color Col(float u, float v) { float g = Mathf.Lerp(1f, 0.88f, v); return new Color(g, g, g, 1f); }
                switch (skin)
                {
                    case Skin.Corrugated:
                    {
                        const float pitch = 0.2f, amp = 0.018f;
                        int nu = Mathf.Max(8, Mathf.RoundToInt(len / pitch * 6f));
                        float Wave(float u) => Mathf.Clamp(Mathf.Sin(u * len / pitch * Mathf.PI * 2f) * 1.6f, -1f, 1f);
                        // 음영은 물결 깊이 30%로: 해 반대쪽 골 옆면이 새까만 줄무늬가 되지 않게 (실제 강판은 하늘이 비친다)
                        p.Roof.Surface(nu, 1, (u, v) => Base(u, v) + n * (amp * Wave(u)), n, Col, (u, v) => Base(u, v) + n * (amp * 0.3f * Wave(u)));
                        break;
                    }
                    case Skin.Barrel:
                    case Skin.FlatTile:
                    {
                        const float course = 0.32f;
                        int rows = Mathf.Max(2, Mathf.CeilToInt(slopeLen / course));
                        float pitch = 0.26f;
                        int nu = skin == Skin.Barrel ? Mathf.Max(8, Mathf.RoundToInt(len / pitch * 4f)) : 1;
                        for (int r = 0; r < rows; r++)
                        {
                            float v0 = r / (float)rows, v1 = (r + 1f) / rows + 0.02f;
                            Vector3 Tile(float u, float vv, float depth)
                            {
                                float v = Mathf.Lerp(v0, Mathf.Min(1f, v1), vv);
                                float bump = skin == Skin.Barrel ? 0.045f * depth * (Mathf.Abs(Mathf.Sin(u * len / pitch * Mathf.PI)) * 2f - 1f) : 0f;
                                return Base(u, v) + n * (bump + 0.035f * vv); // 줄 아래 끝이 들린다
                            }
                            p.Roof.Surface(nu, 1, (u, vv) => Tile(u, vv, 1f), n, (u, vv) => Col(u, Mathf.Lerp(v0, v1, vv)) * Mathf.Lerp(0.8f, 1f, vv),
                                (u, vv) => Tile(u, vv, 0.5f));
                        }
                        break;
                    }
                    case Skin.Thatch:
                    {
                        // 초가: 45cm 줄마다 짚 다발 층 (아래 끝이 들쭉날쭉, 겉이 울퉁불퉁), 처마 끝에 두꺼운 짚 단면
                        const float course = 0.45f;
                        int rows = Mathf.Max(2, Mathf.CeilToInt(slopeLen / course));
                        int nu = Mathf.Max(8, Mathf.RoundToInt(len / 0.12f));
                        float seed = s > 0f ? 3.7f : 9.1f;
                        for (int r = 0; r < rows; r++)
                        {
                            float v0 = r / (float)rows, v1 = (r + 1f) / rows + 0.05f;
                            int rr = r;
                            Vector3 Straw(float u, float vv, float depth)
                            {
                                float jag = (Mathf.PerlinNoise(u * len * 2.6f, rr * 7.3f + seed) - 0.5f) * 0.16f + (Mathf.PerlinNoise(u * len * 9f, rr * 3.1f) - 0.5f) * 0.08f;
                                float v = Mathf.Lerp(v0, v1 + jag / slopeLen * 2f, vv);
                                float lump = (Mathf.PerlinNoise(u * len * 1.4f + seed, v * slopeLen * 1.1f + rr) - 0.5f) * 0.1f * depth;
                                return Base(u, v) + n * (lump + 0.11f * vv);
                            }
                            p.Roof.Surface(nu, 2, (u, vv) => Straw(u, vv, 1f), n, (u, vv) => Col(u, Mathf.Lerp(v0, v1, vv)) * Mathf.Lerp(0.72f, 1f, vv),
                                (u, vv) => Straw(u, vv, 0.4f));
                        }
                        // 처마 끝 단면 (짚 두께가 보인다)
                        var outDir = new Vector3(0f, -k, s).normalized;
                        p.Roof.Surface(nu, 1, (u, w2) =>
                        {
                            float jag = (Mathf.PerlinNoise(u * len * 9f, 77f + seed) - 0.5f) * 0.1f;
                            return Base(u, 1.02f) + outDir * jag + n * Mathf.Lerp(0.11f, -thick - 0.02f, w2);
                        }, outDir, (u, w2) => new Color(0.62f, 0.62f, 0.62f) * Mathf.Lerp(1f, 0.7f, w2));
                        break;
                    }
                    default:
                        p.Roof.Surface(1, 1, Base, n, Col);
                        break;
                }
                // 밑판 (처마 밑에서 보이는 면)
                p.Under.Surface(1, 1, (u, v) => Base(u, v) - n * (thick + 0.01f), -n, (u, v) => new Color(0.7f, 0.7f, 0.7f));
                // 처마 판: 지붕판 끝보다 6cm 안쪽 밑에 세워서 처마 끝 물결·기와 단면이 그대로 보인다
                float fz = s * (half - 0.06f), fTop = Y(fz) - thick - 0.012f, fh = 0.16f;
                p.Trim.Block(new Vector3(0f, fTop - fh * 0.5f, fz), new Vector3(len - 0.02f, fh, 0.035f), 0.008f, Quaternion.identity, white, white * 0.8f);
                // 박공 마감판 (박공 끝을 따라 비스듬히)
                var dir = new Vector3(0f, -k * half, s * half).normalized;
                foreach (float sx in new[] { -1f, 1f })
                {
                    var mid = new Vector3(sx * (len * 0.5f + 0.02f), Y(s * half * 0.5f) - thick * 0.4f, s * half * 0.5f);
                    p.Trim.Block(mid, new Vector3(0.04f, thick + 0.12f, slopeLen + 0.06f), 0.008f, Quaternion.LookRotation(dir, n), white, white * 0.8f);
                }
                if (gutter)
                {
                    float gy = Y(s * half) - thick - 0.06f, gz = s * (half + 0.08f);
                    p.Pipes.Add(new[] { new Vector3(-len * 0.5f + 0.1f, gy, gz), new Vector3(len * 0.5f - 0.1f, gy, gz) });
                }
            }
            // 용마루 덮개: 골강판은 ∧ 덮개판, 기와는 반원 용마루 기와
            float ry = wallTop + thick + rise;
            if (skin == Skin.Thatch)
                p.Roof.Surface(Mathf.Max(6, Mathf.RoundToInt(len / 0.3f)), 8, (u, v) =>
                    new Vector3(-len * 0.5f - 0.05f + u * (len + 0.1f), ry + 0.05f + Mathf.Sin(v * Mathf.PI) * 0.26f, Mathf.Cos(v * Mathf.PI) * 0.34f),
                    Vector3.up, (u, v) => new Color(0.85f, 0.85f, 0.85f));
            else if (skin == Skin.Barrel || skin == Skin.FlatTile)
                p.Roof.Surface(Mathf.Max(6, Mathf.RoundToInt(len / 0.3f)), 6, (u, v) =>
                    new Vector3(-len * 0.5f - 0.02f + u * (len + 0.04f), ry + 0.02f + Mathf.Sin(v * Mathf.PI) * 0.11f, Mathf.Cos(v * Mathf.PI) * 0.13f),
                    Vector3.up, (u, v) => white);
            else
                foreach (float s in new[] { 1f, -1f })
                {
                    var n = new Vector3(0f, half, s * k * half).normalized;
                    var dir = new Vector3(0f, -k, s).normalized;
                    p.Roof.Block(new Vector3(0f, ry + 0.03f, 0f) + dir * 0.1f, new Vector3(len + 0.06f, 0.02f, 0.22f), 0f, Quaternion.LookRotation(dir, n), white, white);
                }
            // 박공 벽 삼각형 (벽 위 → 지붕 밑)
            if (gableWall != null)
            {
                var prof = new List<Vector2> { new Vector2(-wallD * 0.5f, wallTop), new Vector2(wallD * 0.5f, wallTop), new Vector2(0f, wallTop + rise - 0.02f) };
                var rotX = Quaternion.Euler(0f, -90f, 0f); // 단면 x → 집 z, 밀기 z → 집 -x
                var c = wallShade != null ? wallShade(new Vector3(0f, wallTop + rise * 0.5f, 0f)) : white;
                gableWall.Extrude(prof, -wallW * 0.5f, -wallW * 0.5f + t, Vector3.zero, rotX, c, c);
                gableWall.Extrude(prof, wallW * 0.5f - t, wallW * 0.5f, Vector3.zero, rotX, c, c);
            }
        }

        /// <summary>
        /// 외쪽지붕 (창고·덧댄 칸): 벽 윗면 높은 쪽 hiTop(z = +d/2 쪽)에서 낮은 쪽 loTop(-d/2 쪽)으로 한 면, 골강판. 처마 내밀기 o, 밑판·처마 판·물받이.
        /// 옆 벽 위 사다리꼴 채움(sideWall 버퍼, 두께 t). 로컬: 가로 x = w, 깊이 z = d.
        /// </summary>
        public static void ShedRoof(Parts p, float w, float d, float hiTop, float loTop, float o, BlockMesh sideWall = null, float t = 0.2f, float thick = 0.08f)
        {
            float k = (hiTop - loTop) / d;
            float Y(float z) => hiTop + thick - k * (d * 0.5f - z);
            float len = w + o * 2f, z0 = d * 0.5f + o, z1 = -d * 0.5f - o;
            var n = new Vector3(0f, 1f, k).normalized;
            Vector3 Base(float u, float v) { float z = Mathf.Lerp(z0, z1, v); return new Vector3(-len * 0.5f + u * len, Y(z), z); }
            const float pitch = 0.2f, amp = 0.018f;
            float Wave(float u) => Mathf.Clamp(Mathf.Sin(u * len / pitch * Mathf.PI * 2f) * 1.6f, -1f, 1f);
            p.Roof.Surface(Mathf.Max(8, Mathf.RoundToInt(len / pitch * 6f)), 1, (u, v) => Base(u, v) + n * (amp * Wave(u)), n,
                (u, v) => new Color(1f, 1f, 1f) * Mathf.Lerp(1f, 0.9f, v), (u, v) => Base(u, v) + n * (amp * 0.3f * Wave(u)));
            p.Under.Surface(1, 1, (u, v) => Base(u, v) - n * (thick + 0.01f), -n, (u, v) => new Color(0.7f, 0.7f, 0.7f));
            var white = Color.white;
            p.Trim.Block(new Vector3(0f, Y(z1 + 0.06f) - thick - 0.09f, z1 + 0.06f), new Vector3(len - 0.02f, 0.16f, 0.035f), 0.008f, Quaternion.identity, white, white * 0.8f);
            p.Pipes.Add(new[] { new Vector3(-len * 0.5f + 0.1f, Y(z1) - thick - 0.06f, z1 - 0.08f), new Vector3(len * 0.5f - 0.1f, Y(z1) - thick - 0.06f, z1 - 0.08f) });
            if (sideWall == null) return;
            var prof = new List<Vector2> { new Vector2(-d * 0.5f, loTop), new Vector2(d * 0.5f, loTop), new Vector2(d * 0.5f, hiTop), new Vector2(-d * 0.5f, loTop + 0.001f) };
            var rotX = Quaternion.Euler(0f, -90f, 0f);
            sideWall.Extrude(prof, -w * 0.5f, -w * 0.5f + t, Vector3.zero, rotX, white, white);
            sideWall.Extrude(prof, w * 0.5f - t, w * 0.5f, Vector3.zero, rotX, white, white);
        }

        /// <summary>
        /// 평지붕 (슬래브): 벽 위 0.2m 슬래브(사방 5cm 내밂) + 난간 턱(parapetH) + 갓돌, 위는 초록 우레탄 방수칠(coat, Accent 버퍼) — 한국 옥상.
        /// 난간 한쪽에 배수 꼭지(Metal).
        /// </summary>
        public static void FlatRoof(Parts p, float wallTop, float w, float d, float parapetH, bool coat, Func<Vector3, Color> shade)
        {
            var white = Color.white;
            p.Trim.Block(new Vector3(0f, wallTop + 0.1f, 0f), new Vector3(w + 0.1f, 0.2f, d + 0.1f), 0.02f, Quaternion.identity, white, white * 0.75f);
            float top = wallTop + 0.2f, pt = 0.18f;
            if (coat)
                p.Accent.Face(new Vector3(-w * 0.5f + pt, top + 0.004f, -d * 0.5f + pt), new Vector3(-w * 0.5f + pt, top + 0.004f, d * 0.5f - pt),
                    new Vector3(w * 0.5f - pt, top + 0.004f, d * 0.5f - pt), new Vector3(w * 0.5f - pt, top + 0.004f, -d * 0.5f + pt), Vector3.up, white);
            if (parapetH <= 0f) return;
            foreach (var (c, size) in new[]
                     {
                         (new Vector3(0f, 0f, d * 0.5f - pt * 0.5f), new Vector3(w, parapetH, pt)), (new Vector3(0f, 0f, -d * 0.5f + pt * 0.5f), new Vector3(w, parapetH, pt)),
                         (new Vector3(w * 0.5f - pt * 0.5f, 0f, 0f), new Vector3(pt, parapetH, d - pt * 2f)), (new Vector3(-w * 0.5f + pt * 0.5f, 0f, 0f), new Vector3(pt, parapetH, d - pt * 2f)),
                     })
            {
                p.Wall.Block(c + Vector3.up * (top + parapetH * 0.5f), size, 0f, Quaternion.identity, shade);
                p.Trim.Block(c + Vector3.up * (top + parapetH + 0.03f), size + new Vector3(0.06f, 0.06f - parapetH, 0.06f), 0.015f, Quaternion.identity, white, white * 0.8f);
            }
            p.Pipes.Add(new[] { new Vector3(w * 0.5f - 0.1f, top + 0.06f, d * 0.3f), new Vector3(w * 0.5f + 0.25f, top + 0.04f, d * 0.3f) });
        }

        /// <summary>
        /// 시멘트 블록 담장 (a→b, 높이 h): 20cm 줄마다 모따기 블록(가로 홈이 진다, blocks) + 40cm마다 세로 줄눈(어긋나게, joints), 위 갓돌(cap).
        /// </summary>
        public static void BlockWall(BlockMesh blocks, BlockMesh joints, BlockMesh cap, Vector3 a, Vector3 b, float h, System.Random rng)
        {
            var dir = b - a;
            dir.y = 0f;
            float len = dir.magnitude;
            if (len < 0.3f) return;
            var rot = Quaternion.LookRotation(Vector3.Cross(dir / len, Vector3.up));
            var along = dir / len;
            int courses = Mathf.Max(1, Mathf.RoundToInt(h / 0.2f));
            for (int c = 0; c < courses; c++)
            {
                float y = a.y + (c + 0.5f) * 0.2f;
                float g = 0.92f + (float)rng.NextDouble() * 0.1f;
                var col = new Color(g, g, g);
                blocks.Block(a + along * (len * 0.5f) + Vector3.up * (y - a.y), new Vector3(0.19f, 0.195f, len), 0.012f, Quaternion.LookRotation(along), col, col * 0.85f);
                // 세로 줄눈 (짙은 선, 담 양면)
                for (float s = (c % 2) * 0.2f + 0.2f; s < len - 0.1f; s += 0.4f)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var n = rot * Vector3.forward * side;
                        var q = a + along * s + Vector3.up * (y - a.y - 0.095f) + n * 0.097f;
                        joints.Face(q - along * 0.006f, q + along * 0.006f, q + along * 0.006f + Vector3.up * 0.19f, q - along * 0.006f + Vector3.up * 0.19f, n, new Color(0.45f, 0.45f, 0.45f));
                    }
            }
            var top = new Color(0.95f, 0.95f, 0.95f);
            cap.Block(a + along * (len * 0.5f) + Vector3.up * (courses * 0.2f + 0.03f), new Vector3(0.26f, 0.06f, len + 0.04f), 0.012f, Quaternion.LookRotation(along), top, top * 0.8f);
        }
    }
}
