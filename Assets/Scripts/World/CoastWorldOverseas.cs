using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 해외 낚시터 풍경 (원작 동남아·지중해·나일강 거점): 지역 양식 집과 나무, 강 맞은편 둑,
    /// 하롱베이 석회암 봉우리·정크선·수상 가옥, 산토리니 칼데라·하얀 마을·풍차, 나일강 펠루카·피라미드.
    /// 야자수·펠루카·정크선 모델은 tools/blender/build_props.py (Resources/Props).
    /// </summary>
    public partial class CoastWorldBuilder
    {
        // ---------------- 집 ----------------

        void House(Vector3 at, float yaw, bool sign)
        {
            switch (S.Houses)
            {
                case HouseStyle.Cyclades: CycladicHouse(at, yaw, _rng.NextDouble() < 0.12); break;
                case HouseStyle.Egypt: MudHouse(at, yaw); break;
                case HouseStyle.Vietnam: TubeHouse(at, yaw, sign); break;
                case HouseStyle.Stilt: StiltHouse(at, yaw); break;
                case HouseStyle.Resort: ResortVilla(at, yaw); break;
                case HouseStyle.Thai: ThaiHouse(at, yaw); break;
                case HouseStyle.Venetian: VenetianHouse(at, yaw, sign); break;
                default: _props.KoreanHouse(at, yaw, sign); break;
            }
        }

        static readonly Color CycladicBlue = new Color(0.1f, 0.35f, 0.72f);
        static readonly Dictionary<long, Mesh> ArchCache = new Dictionary<long, Mesh>();

        /// <summary>위가 반원인 판 (아치 문·창): 바닥 가운데 원점, 앞(+Z)을 본다. 두께 0.04.</summary>
        static Mesh ArchPanel(float width, float height)
        {
            long key = Mathf.RoundToInt(width * 100f) * 100000L + Mathf.RoundToInt(height * 100f);
            if (ArchCache.TryGetValue(key, out var m)) return m;
            float r = width * 0.5f, straight = Mathf.Max(0f, height - r);
            var outline = new List<Vector2> { new Vector2(-r, 0f), new Vector2(r, 0f) };
            const int arcN = 14;
            for (int i = 0; i <= arcN; i++)
            {
                float a = i / (float)arcN * Mathf.PI;
                outline.Add(new Vector2(Mathf.Cos(a) * r, straight + Mathf.Sin(a) * r));
            }
            var v = new List<Vector3>();
            var t = new List<int>();
            // 앞면: 가운데 점에서 부채꼴
            int c0 = v.Count;
            v.Add(new Vector3(0f, height * 0.45f, 0.02f));
            foreach (var p in outline) v.Add(new Vector3(p.x, p.y, 0.02f));
            // 윤곽은 +Z에서 보면 시계 방향으로 돈다 → 그 순서 그대로가 앞면
            for (int i = 0; i < outline.Count; i++) t.AddRange(new[] { c0, c0 + 1 + i, c0 + 1 + (i + 1) % outline.Count });
            // 옆 두께
            int s0 = v.Count;
            foreach (var p in outline) { v.Add(new Vector3(p.x, p.y, 0.02f)); v.Add(new Vector3(p.x, p.y, -0.02f)); }
            for (int i = 0; i < outline.Count; i++)
            {
                int j = (i + 1) % outline.Count;
                // 두께 면은 양면으로 (0.04m라 감는 방향을 따지지 않는다)
                t.AddRange(new[] { s0 + i * 2, s0 + j * 2, s0 + i * 2 + 1, s0 + j * 2, s0 + j * 2 + 1, s0 + i * 2 + 1 });
                t.AddRange(new[] { s0 + i * 2, s0 + i * 2 + 1, s0 + j * 2, s0 + j * 2, s0 + i * 2 + 1, s0 + j * 2 + 1 });
            }
            m = new Mesh { name = "Arch" };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            ArchCache[key] = m;
            return m;
        }

        /// <summary>
        /// 키클라데스 집: 하얀 회벽 상자(모서리가 둥근 느낌으로 처마 턱), 파란 문·덧창, 가끔 2단.
        /// church면 파란 돔을 얹은 작은 교회 (산토리니 이아 마을). 비탈에 서도 뜨지 않게 벽이 땅속 3m까지 내려간다.
        /// </summary>
        void CycladicHouse(Vector3 at, float yaw, bool church, float below = 3f)
        {
            var h = new GameObject(church ? "CycladicChurch" : "CycladicHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            // 사진(이아 마을): 흰 회벽(가끔 황토·분홍 파스텔), 두꺼운 벽에 깊이 들어간 아치 문·창(파란 칠), 둥글게 덧바른 모서리와 처마 턱,
            // 원통 지붕(카마라), 바깥 계단, 부겐빌레아 (HouseKit: 벽을 비운 아치 + 귀퉁이 채움)
            var wallCol = _rng.NextDouble() < 0.15 ? (_rng.NextDouble() < 0.5 ? new Color(0.93f, 0.8f, 0.55f) : new Color(0.93f, 0.72f, 0.68f)) : new Color(0.97f, 0.97f, 0.95f);
            var white = Mats.Wall(wallCol, 0.1f);
            var trim = Mats.Wall(new Color(0.98f, 0.98f, 0.97f), 0.1f);
            var blue = Mats.Wood(CycladicBlue, 0.35f);
            var dark = Mats.Lit(new Color(0.18f, 0.2f, 0.24f), 0.6f);
            float w = R(5f, 8f), d = R(5f, 7f), hgt = R(3f, 3.6f);   // below = 땅속으로 내린 벽 (비탈에 박힌 동굴집이면 깊게)
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            var p = new HouseKit.Parts { WallMat = white, TrimMat = trim, FrameMat = blue, GlassMat = dark, DoorMat = blue, MetalMat = dark };
            float dx = R(-w * 0.2f, w * 0.2f);
            var front = new List<HouseKit.Opening> { new HouseKit.Opening(dx, 0f, 0.95f, 2.15f, HouseKit.Hole.Arch) };
            var winX = new List<float>();
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -1f : 1f) * w * 0.32f;
                if (Mathf.Abs(x - dx) < 1.3f) continue;
                winX.Add(x);
                front.Add(new HouseKit.Opening(x, 1.2f, 0.76f, 1.12f, HouseKit.Hole.ArchWindow));
            }
            HouseKit.Box(p, w, d, -below, hgt, 0.32f, front, null, null, null, HouseKit.WallShade(0f, hgt), local, 0.4f, 0.18f);
            // 둥근 처마 턱: 가로 원통을 둘레에 (모서리가 각지지 않게 회벽을 덧바른 모양)
            foreach (var (pos, len, rot) in new[] { (new Vector3(0f, hgt + 0.05f, d * 0.5f), w + 0.1f, 90f), (new Vector3(0f, hgt + 0.05f, -d * 0.5f), w + 0.1f, 90f),
                         (new Vector3(w * 0.5f, hgt + 0.05f, 0f), d + 0.1f, 0f), (new Vector3(-w * 0.5f, hgt + 0.05f, 0f), d + 0.1f, 0f) })
                Mats.Prim(PrimitiveType.Capsule, "Lip", h, pos, new Vector3(0.28f, len * 0.5f, 0.28f), trim, false, Quaternion.Euler(90f, rot, 0f));
            p.Trim.Block(new Vector3(0f, hgt + 0.05f, 0f), new Vector3(w, 0.2f, d), 0.05f, Quaternion.identity, Color.white, Color.white * 0.85f);
            p.Trim.Block(new Vector3(dx, 0.08f, d * 0.5f + 0.35f), new Vector3(1.5f, 0.16f, 0.6f), 0.05f, Quaternion.identity, Color.white, Color.white * 0.8f); // 문 앞 디딤
            // 아치 창 양옆 파란 덧창
            foreach (float x in winX)
                foreach (float sx in new[] { -0.58f, 0.58f })
                    p.Door.Block(new Vector3(x + sx, 1.72f, d * 0.5f + 0.03f), new Vector3(0.36f, 1.05f, 0.04f), 0.01f, Quaternion.identity, Color.white, Color.white);
            // 원통 지붕 (동굴집 카마라): 앞뒤 방향 반원통
            if (!church && _rng.NextDouble() < 0.3)
                Mats.Prim(PrimitiveType.Cylinder, "Vault", h, new Vector3(0f, hgt + 0.1f, 0f), new Vector3(w * 0.9f, d * 0.5f, w * 0.55f), trim, false, Quaternion.Euler(90f, 0f, 0f));
            // 옆벽 바깥 계단 (옥상 테라스로): 디딤판마다 모따기, 바깥 난간벽
            if (_rng.NextDouble() < 0.35)
            {
                float sx = _rng.NextDouble() < 0.5 ? -1f : 1f;
                for (int k = 0; k < 9; k++)
                    p.Trim.Block(new Vector3(sx * (w * 0.5f + 0.45f), k * hgt / 9f + 0.15f, d * 0.4f - k * d * 0.8f / 9f), new Vector3(0.9f, 0.3f, d * 0.8f / 9f + 0.02f), 0.04f,
                        Quaternion.identity, Color.white, Color.white * 0.85f);
                p.Wall.Block(new Vector3(sx * (w * 0.5f + 0.88f), hgt * 0.5f, 0f), new Vector3(0.14f, hgt, d * 0.85f), 0.06f, Quaternion.identity, HouseKit.WallShade(0f, hgt));
            }
            // 부겐빌레아: 문 옆 벽을 타고 오른 분홍·자주 꽃 덩굴
            if (_rng.NextDouble() < 0.2)
            {
                var pink = Mats.Lit(_rng.NextDouble() < 0.6 ? new Color(0.85f, 0.2f, 0.55f) : new Color(0.65f, 0.2f, 0.6f), 0.2f);
                var leaf = Mats.Lit(new Color(0.2f, 0.38f, 0.16f), 0.2f);
                float bx = dx + (dx > 0 ? -1.3f : 1.3f);
                // 벽에 붙은 납작한 꽃송이 덩어리 (위로 갈수록 처마를 따라 옆으로 번진다)
                for (int k = 0; k < 16; k++)
                {
                    float yy = R(0.6f, hgt + 0.2f);
                    float spread = 0.4f + yy / hgt * 1.2f;
                    Mats.Prim(PrimitiveType.Sphere, "Bougainvillea", h, new Vector3(bx + R(-spread, spread), yy, d * 0.5f + 0.12f),
                        new Vector3(R(0.35f, 0.6f), R(0.3f, 0.45f), 0.22f), k % 4 == 0 ? leaf : pink, false);
                }
            }
            float top = hgt;
            if (!church && _rng.NextDouble() < 0.35)
            {
                // 2단: 뒤쪽에 작은 상자 하나 더 (테라스가 앞에 남는다), 아치 창 하나
                float w2 = w * R(0.5f, 0.7f), d2 = d * 0.5f;
                var rig = HouseKit.Rig(h, new Vector3(R(-0.5f, 0.5f), hgt, -d * 0.25f), 0f, "Upper");
                var up = new HouseKit.Parts { WallMat = white, TrimMat = trim, FrameMat = blue, GlassMat = blue, DoorMat = blue };
                HouseKit.Box(up, w2, d2, 0f, 3f, 0.3f, new List<HouseKit.Opening> { new HouseKit.Opening(0f, 1.0f, 0.8f, 1.2f, HouseKit.Hole.ArchWindow) }, null, null, null,
                    HouseKit.WallShade(0f, 3f), local, 0.35f, 0.16f);
                up.Trim.Block(new Vector3(0f, 3.05f, 0f), new Vector3(w2 + 0.1f, 0.18f, d2 + 0.1f), 0.06f, Quaternion.identity, Color.white, Color.white * 0.85f);
                up.Emit(rig);
                top = hgt + 3f;
            }
            p.Emit(h);
            if (church)
            {
                // 둥근 드럼 + 파란 돔 + 꼭대기 십자가, 앞쪽에 종탑
                Mats.Prim(PrimitiveType.Cylinder, "Drum", h, new Vector3(0f, hgt + 0.7f, 0f), new Vector3(w * 0.55f, 0.7f, w * 0.55f), white, false);
                Mats.Prim(PrimitiveType.Sphere, "Dome", h, new Vector3(0f, hgt + 1.4f, 0f), new Vector3(w * 0.56f, w * 0.5f, w * 0.56f), blue, false);
                var gold = Mats.Lit(new Color(0.9f, 0.75f, 0.35f), 0.6f, 0.6f);
                Mats.Prim(PrimitiveType.Cube, "Cross", h, new Vector3(0f, hgt + 1.4f + w * 0.25f + 0.5f, 0f), new Vector3(0.08f, 0.9f, 0.08f), gold, false);
                Mats.Prim(PrimitiveType.Cube, "CrossBar", h, new Vector3(0f, hgt + 1.4f + w * 0.25f + 0.65f, 0f), new Vector3(0.45f, 0.08f, 0.08f), gold, false);
                // 종탑: 흰 벽에 아치 구멍 (뒤가 비친다)
                var bell = new HouseKit.Parts { WallMat = white, DoorMat = dark };
                HouseKit.Wall(bell.Wall, new Vector3(w * 0.3f, hgt, d * 0.5f), Quaternion.identity, 1.6f, 0f, 2.2f, 0.4f,
                    new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0.5f, 0.6f, 1.1f, HouseKit.Hole.Arch) }, HouseKit.WallShade(hgt, hgt + 2.2f));
                bell.Emit(h);
                top = hgt + 1.4f + w * 0.25f;
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, top * 0.5f, 0f);
            col.size = new Vector3(w, top + 0.5f, d);
        }

        /// <summary>이집트 시골집: 두꺼운 흙벽돌 평지붕 (모서리가 둥글게 닳음), 낮은 난간, 깊이 들어간 작은 창과 문. 가끔 비둘기탑(위가 둥근 흙탑).</summary>
        void MudHouse(Vector3 at, float yaw)
        {
            var h = new GameObject("MudHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var tones = new[] { new Color(0.78f, 0.66f, 0.5f), new Color(0.72f, 0.58f, 0.42f), new Color(0.86f, 0.78f, 0.62f) };
            var mud = Mats.Wall(tones[_rng.Next(tones.Length)], 0.05f);
            var dark = Mats.Lit(new Color(0.15f, 0.12f, 0.1f), 0.2f);
            var door = Mats.Lit(_rng.NextDouble() < 0.5 ? new Color(0.2f, 0.45f, 0.55f) : new Color(0.45f, 0.3f, 0.18f), 0.2f);
            float w = R(6f, 9f), d = R(5f, 7f), hgt = R(3f, 4.2f);
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            var p = new HouseKit.Parts { WallMat = mud, TrimMat = mud, FrameMat = door, GlassMat = dark, DoorMat = door };
            var shade = HouseKit.WallShade(0f, hgt);
            var front = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0f, 1f, 2f, HouseKit.Hole.Door) };
            for (int i = -1; i <= 1; i += 2) front.Add(new HouseKit.Opening(i * w * 0.3f, 1.8f, 0.55f, 0.55f));
            var side = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 1.9f, 0.45f, 0.45f) };
            HouseKit.Box(p, w, d, -1.5f, hgt, 0.4f, front, null, side, side, shade, local, 0.5f, 0.22f);
            // 낮은 난간 (앞뒤), 둥글게 닳은 흙
            foreach (float sz in new[] { 1f, -1f })
                p.Wall.Block(new Vector3(0f, hgt + 0.28f, sz * (d * 0.5f - 0.16f)), new Vector3(w + 0.02f, 0.6f, 0.34f), 0.12f, Quaternion.identity, shade);
            p.Trim.Block(new Vector3(0f, hgt - 0.05f, 0f), new Vector3(w - 0.1f, 0.12f, d - 0.1f), 0.04f, Quaternion.identity, Color.white, Color.white);
            // 지붕 위 짚단·장작
            Mats.Prim(PrimitiveType.Cube, "Straw", h, new Vector3(R(-w * 0.2f, w * 0.2f), hgt + 0.35f, R(-1f, 0.5f)), new Vector3(R(1.5f, 2.5f), 0.5f, R(1f, 1.8f)),
                Mats.Lit(new Color(0.8f, 0.7f, 0.4f), 0.05f), false);
            p.Emit(h);
            float top = hgt;
            if (_rng.NextDouble() < 0.25)
            {
                Mats.Prim(PrimitiveType.Cylinder, "Dovecote", h, new Vector3(w * 0.5f + 1.4f, 2.6f, -d * 0.2f), new Vector3(2f, 2.6f, 2f), mud, false);
                Mats.Prim(PrimitiveType.Sphere, "DovecoteTop", h, new Vector3(w * 0.5f + 1.4f, 5.2f, -d * 0.2f), new Vector3(2f, 2.4f, 2f), mud, false);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * 60f * Mathf.Deg2Rad;
                    Mats.Prim(PrimitiveType.Cube, "Hole", h, new Vector3(w * 0.5f + 1.4f + Mathf.Cos(a) * 1f, 3.6f + (k % 2) * 0.6f, -d * 0.2f + Mathf.Sin(a) * 1f),
                        new Vector3(0.2f, 0.2f, 0.2f), dark, false);
                }
                top = 6.4f;
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, hgt * 0.5f, 0f);
            col.size = new Vector3(w, hgt + 0.6f, d);
        }

        static readonly Color[] TubeColors =
        {
            new Color(0.96f, 0.82f, 0.35f), new Color(0.93f, 0.62f, 0.6f), new Color(0.6f, 0.85f, 0.72f), new Color(0.55f, 0.72f, 0.92f), new Color(0.97f, 0.95f, 0.88f),
        };
        static readonly string[] TubeSigns = { "PHO", "CAFE", "HAI SAN", "NHA NGHI", "BIA" };

        /// <summary>
        /// 베트남 좁고 높은 집 (낮 하롱 시내의 nhà ống): 폭 4~5m, 3~4층, 파스텔 벽, 1층 셔터 가게, 층마다 발코니와 들어간 큰 창,
        /// 꼭대기 앞은 옥상 테라스, 뒤는 박공을 앞으로 보인 빨간 기와 지붕.
        /// </summary>
        void TubeHouse(Vector3 at, float yaw, bool sign)
        {
            var h = new GameObject("TubeHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wall = Mats.Wall(TubeColors[_rng.Next(TubeColors.Length)], 0.15f);
            var rail = Mats.Lit(new Color(0.2f, 0.22f, 0.24f), 0.4f);
            var glass = Mats.Lit(new Color(0.2f, 0.28f, 0.32f), 0.8f);
            var tile = Mats.Tile(new Color(0.72f, 0.3f, 0.18f), 0.2f);
            float w = R(4f, 5.2f), d = R(8f, 11f);
            int floors = _rng.Next(3, 5);
            const float fh = 3.2f;
            float hgt = floors * fh;
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            var trim = Mats.Weathered("concrete", new Color(0.8f, 0.79f, 0.76f), 2f, 0.12f, 0f, 0.5f, 0.15f, null, 0.7f, 0f, "wallconcrete");
            var p = new HouseKit.Parts
            {
                WallMat = wall, TrimMat = trim, FrameMat = Mats.Lit(new Color(0.75f, 0.76f, 0.78f), 0.55f, 0.5f), GlassMat = glass,
                Glass2Mat = Mats.Lit(new Color(0.42f, 0.36f, 0.3f), 0.8f), DoorMat = rail, MetalMat = Mats.Lit(new Color(0.62f, 0.64f, 0.66f), 0.45f, 0.5f), RoofMat = tile, UnderMat = trim,
            };
            var shade = HouseKit.WallShade(0f, hgt);
            var front = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0.05f, w * 0.78f, 2.6f, HouseKit.Hole.Shop) };
            var back = new List<HouseKit.Opening>();
            for (int f = 1; f < floors; f++)
            {
                front.Add(new HouseKit.Opening(0f, f * fh + 0.25f, w * 0.55f, 2.1f));
                back.Add(new HouseKit.Opening(0f, f * fh + 1f, 1.1f, 1.1f));
            }
            HouseKit.Box(p, w, d, -0.8f, hgt, 0.22f, front, back, null, null, shade, local);
            for (int f = 1; f < floors; f++)
            {
                // 발코니: 모따기 슬래브 + 쇠 난간 (위 손잡이·세로 살)
                float y = f * fh;
                p.Trim.Block(new Vector3(0f, y + 0.05f, d * 0.5f + 0.5f), new Vector3(w, 0.15f, 1f), 0.03f, Quaternion.identity, Color.white, Color.white * 0.75f);
                p.Door.Block(new Vector3(0f, y + 1.05f, d * 0.5f + 0.98f), new Vector3(w, 0.04f, 0.05f), 0f, Quaternion.identity, Color.white, Color.white);
                for (float x = -w * 0.5f + 0.1f; x < w * 0.5f; x += 0.14f)
                    p.Door.Block(new Vector3(x, y + 0.6f, d * 0.5f + 0.98f), new Vector3(0.02f, 0.9f, 0.02f), 0f, Quaternion.identity, Color.white, Color.white);
            }
            // 꼭대기: 슬래브 + 앞 옥상 테라스 난간, 뒤 60%는 기와 박공 (박공을 앞으로)
            HouseKit.FlatRoof(p, hgt, w, d, 0f, false, shade);
            p.Wall.Block(new Vector3(0f, hgt + 0.65f, d * 0.5f - 0.09f), new Vector3(w, 0.9f, 0.18f), 0f, Quaternion.identity, shade);
            p.Trim.Block(new Vector3(0f, hgt + 1.13f, d * 0.5f - 0.09f), new Vector3(w + 0.06f, 0.06f, 0.24f), 0.015f, Quaternion.identity, Color.white, Color.white * 0.8f);
            p.Emit(h);
            var rig = HouseKit.Rig(h, new Vector3(0f, hgt + 0.2f, -d * 0.18f), 90f);
            var rp = new HouseKit.Parts { WallMat = wall, TrimMat = trim, RoofMat = tile, UnderMat = trim, PipeMat = rail };
            HouseKit.GableRoof(rp, 0f, d * 0.6f, w - 0.36f, 1.4f, 0.25f, 0.35f, HouseKit.Skin.Barrel, rp.Wall, 0.18f, 0.1f, false, shade);
            rp.Emit(rig);
            if (sign)
            {
                var board = Mats.Lit(new Color(0.85f, 0.15f, 0.12f), 0.3f);
                Mats.Prim(PrimitiveType.Cube, "SignBoard", h, new Vector3(0f, fh - 0.2f, d * 0.5f + 0.12f), new Vector3(w - 0.4f, 0.7f, 0.1f), board, false);
                var t = UIFactory.WorldText(h, TubeSigns[_rng.Next(TubeSigns.Length)], new Vector3(0f, fh - 0.2f, d * 0.5f + 0.19f), new Color(1f, 0.9f, 0.3f), 5f, "Sign");
                UIFactory.OneSided(t);
                t.transform.localScale = Vector3.one * 0.8f;
                t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, hgt * 0.5f, 0f);
            col.size = new Vector3(w, hgt + 1f, d);
        }

        internal static Mesh _gableMesh;

        /// <summary>박공지붕 단위 메쉬 (CoastProps와 같은 모양): 바닥 -0.5~0.5, 용마루 높이 1.</summary>
        internal static Mesh GableMesh()
        {
            if (_gableMesh != null) return _gableMesh;
            var a = new Vector3(-0.5f, 0f, -0.5f); var b = new Vector3(0.5f, 0f, -0.5f);
            var c = new Vector3(0.5f, 0f, 0.5f); var e = new Vector3(-0.5f, 0f, 0.5f);
            var r0 = new Vector3(-0.5f, 1f, 0f); var r1 = new Vector3(0.5f, 1f, 0f);
            var v = new[] { a, b, r1, r0, e, r0, r1, c, a, r0, e, b, c, r1, a, e, c, b };
            var t = new[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6, 8, 10, 9, 11, 13, 12, 14, 15, 16, 14, 16, 17 };
            _gableMesh = new Mesh { name = "Gable", vertices = v, triangles = t };
            _gableMesh.RecalculateNormals();
            _gableMesh.RecalculateBounds();
            return _gableMesh;
        }

        /// <summary>
        /// 산토리니 이아처럼 절벽 꼭대기 가장자리를 따라 하얀 집이 층층이 매달린 마을.
        /// 절벽 위(뒤쪽 높은 곳)에서 비탈이 너무 가파르지 않은 자리를 골라 촘촘히 놓는다.
        /// </summary>
        void CliffVillage()
        {
            float halfW = S.LandHalfWidth > 0f ? S.LandHalfWidth - 25f : 190f;
            float edge = S.FlatWidth + S.CliffWidth * 0.55f;
            for (int i = 0; i < 260; i++)
            {
                float x = R(-halfW, halfW);
                float back = R(edge, edge + 55f);
                float z = _map.ShoreAt(x) - back;
                if (z < CoastMapDef.TerrainMinZ + 8f || InFeature(x, z)) continue;
                var (y, _) = Footing(x, z, 2.5f);
                float hi = H(x, z);
                if (hi - y > 3f || y < _map.LandY + S.CliffHeight * 0.55f) continue; // 가파른 벽면·아래쪽은 건너뛴다
                bool church = _rng.NextDouble() < 0.07;
                CycladicHouse(new Vector3(x, y - 0.1f, z), R(-8f, 8f), church);
            }
            if ((S.Features & CoastFeature.Windmills) != 0)
                for (int i = 0; i < 4; i++)
                {
                    float x = -halfW * 0.8f + i * 22f + R(-4f, 4f);
                    float z = _map.ShoreAt(x) - (edge + 30f);
                    var (y, flat) = Footing(x, z, 2.5f);
                    if (!flat) continue;
                    Windmill(new Vector3(x, y, z));
                }
        }

        /// <summary>키클라데스 풍차: 하얀 원통 몸, 원뿔 짚지붕, 삼각 돛 8장.</summary>
        void Windmill(Vector3 at)
        {
            var t = new GameObject("Windmill").transform;
            t.SetParent(_root, false);
            t.localPosition = at;
            t.localRotation = Quaternion.Euler(0f, R(150f, 210f), 0f);
            var white = Mats.Wall(new Color(0.96f, 0.96f, 0.94f), 0.1f);
            var thatch = Mats.Thatch(new Color(0.45f, 0.36f, 0.25f));
            var wood = Mats.Wood(new Color(0.4f, 0.3f, 0.2f), 0.2f);
            var cloth = Mats.Lit(new Color(0.95f, 0.93f, 0.86f), 0.1f);
            Mats.Prim(PrimitiveType.Cylinder, "Tower", t, new Vector3(0f, 3.5f, 0f), new Vector3(4.2f, 3.5f, 4.2f), white);
            Mats.Prim(PrimitiveType.Sphere, "Cap", t, new Vector3(0f, 7.2f, 0f), new Vector3(4.4f, 2.6f, 4.4f), thatch, false);
            var hub = new Vector3(0f, 6.6f, 2.3f);
            for (int k = 0; k < 8; k++)
            {
                var rot = Quaternion.Euler(0f, 0f, k * 45f + 10f);
                Mats.Prim(PrimitiveType.Cube, "Spar", t, hub + rot * new Vector3(0f, 2.6f, 0f), new Vector3(0.12f, 5.2f, 0.12f), wood, false, rot);
                Mats.Prim(PrimitiveType.Cube, "Sail", t, hub + rot * new Vector3(0.5f, 3f, 0.05f), new Vector3(0.9f, 3.4f, 0.03f), cloth, false, rot);
            }
        }

        // ---------------- 나무 ----------------

        void Tree(Vector3 at)
        {
            if (!KenneyProps.Available) return;
            switch (S.TreeKind)
            {
                case TreeStyle.Palm:
                    KenneyProps.Spawn(_rng.NextDouble() < 0.5 ? "Props/palm_a" : "Props/palm_b", _root, at, R(0f, 360f), R(6f, 9.5f));
                    break;
                case TreeStyle.Tropical:
                    if (_rng.NextDouble() < 0.55) KenneyProps.Spawn(_rng.NextDouble() < 0.5 ? "Props/palm_a" : "Props/palm_b", _root, at, R(0f, 360f), R(6f, 9f));
                    else KenneyProps.Spawn("Kenney/Nature/tree_default", _root, at, R(0f, 360f), R(6f, 10f));
                    break;
                case TreeStyle.Mediterranean:
                    // 올리브(작고 둥근 나무)와 사이프러스(가늘고 높은 나무)
                    if (_rng.NextDouble() < 0.7) KenneyProps.Spawn("Kenney/Nature/tree_plateau", _root, at, R(0f, 360f), R(3f, 4.5f));
                    else KenneyProps.Spawn("Kenney/Nature/tree_tall", _root, at, R(0f, 360f), R(7f, 10f));
                    break;
                default:
                    KenneyProps.Spawn("Kenney/Nature/" + (S.Snow ? "tree_pineRoundC" : new[] { "tree_pineRoundA", "tree_pineRoundC", "tree_default", "tree_oak", "tree_pineRoundB" }[_rng.Next(5)]), _root,
                        at, R(0f, 360f), R(5f, 9f));
                    break;
            }
        }

        // ---------------- 강 맞은편 둑 ----------------

        /// <summary>강 건너: 물가를 따라 야자수 줄, 드문드문 흙집, 밭 (사막 쪽은 비워 둔다).</summary>
        void BuildFarBank()
        {
            if (S.FarShoreZ <= 0f) return;
            // 지형(x ±210, z -200~270) 밖으로도 양쪽 둑 땅이 이어져 보이게 사막(또는 들판) 판을 깐다.
            // 높이는 강가 평지(LandY)보다 0.3m 낮게: 지형 안에서는 땅 밑에 묻히고, 밖에서는 물(0.2)보다 높다
            var plain = S.SandLand ? Mats.Textured("sand", new Color(0.95f, 0.85f, 0.65f), 120f, 0.05f) : Mats.Textured("grass", Color.white, 120f, 0.05f);
            float py = _map.LandY - 0.3f - 0.2f;
            Mats.Prim(PrimitiveType.Cube, "FarPlain", _root, new Vector3(0f, py, (S.FarShoreZ + 30f + 1600f) * 0.5f), new Vector3(3200f, 0.4f, 1600f - S.FarShoreZ - 30f), plain, false);
            Mats.Prim(PrimitiveType.Cube, "NearPlain", _root, new Vector3(0f, py, (S.ShoreZ - 30f - 1000f) * 0.5f), new Vector3(3200f, 0.4f, 1000f + S.ShoreZ - 30f), plain, false);
            for (float x = -195f; x < 195f; x += R(6f, 13f))
            {
                float z = _map.FarShoreAt(x) + R(4f, 16f);
                float y = H(x, z);
                if (y < _map.LandY - 0.1f || InFeature(x, z)) continue;
                Tree(new Vector3(x, y - 0.05f, z));
            }
            for (float x = -180f; x < 180f; x += R(28f, 50f))
            {
                float z = _map.FarShoreAt(x) + R(24f, 34f);
                var (y, flat) = Footing(x, z, 4f);
                if (!flat || InFeature(x, z)) continue;
                House(new Vector3(x, y - 0.05f, z), 180f + R(-15f, 15f), false);
            }
        }

        // ---------------- 특별 풍경 ----------------

        void BuildOverseasFeatures()
        {
            if ((S.Features & CoastFeature.Karst) != 0) KarstHorizon();
            if ((S.Features & CoastFeature.Junks) != 0) Boats("Props/junk", 5, 13f);
            if ((S.Features & CoastFeature.Feluccas) != 0) Boats("Props/felucca", 6, 7.5f);
            if ((S.Features & CoastFeature.FloatingVillage) != 0) FloatingVillage();
            if ((S.Features & CoastFeature.Caldera) != 0) Caldera();
            if ((S.Features & CoastFeature.Pyramids) != 0) Pyramids();
        }

        /// <summary>하롱베이 수평선: 녹음 덮인 석회암 봉우리 수십 개 (먼 것은 안개에 묻힌다).</summary>
        void KarstHorizon()
        {
            // 회백색 석회암 절벽 + 젖은 아랫단 + 턱·꼭대기를 덮은 짙은 숲 (먼 것은 안개에 묻히게 엷게)
            var limestone = Mats.Karst(new Color(0.92f, 0.92f, 0.88f));
            var wet = Mats.Karst(new Color(0.42f, 0.42f, 0.4f), 0.1f);
            var jungle = Mats.Karst(new Color(0.92f, 0.92f, 0.88f), 1f);
            var near = new[] { limestone, wet, jungle };
            var far = new[] { Mats.Lit(new Color(0.5f, 0.56f, 0.55f), 0.02f), Mats.Lit(new Color(0.42f, 0.48f, 0.48f), 0.02f), Mats.Lit(new Color(0.4f, 0.5f, 0.46f), 0.02f) };
            for (int i = 0; i < 46; i++)
            {
                float x = R(-650f, 650f), z = R(300f, 900f);
                float r = R(14f, 40f), h = R(40f, 120f);
                var go = new GameObject("Karst");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = new Vector3(x, _map.BaseWaterY - 2f, z);
                go.AddComponent<MeshFilter>().sharedMesh = StackMesh(r, h, _rng.Next(1000), true, 4f);
                go.AddComponent<MeshRenderer>().sharedMaterials = z > 600f ? far : near;
            }
        }

        /// <summary>물 위에 떠 있는 배들 (정크선·펠루카). 낚시 칸 가운데는 피하고 가장자리·먼 쪽에 둔다.</summary>
        void Boats(string path, int count, float length)
        {
            // 지중해 마을(키클라데스·베네치아풍)은 한국 어선 대신 그리스 카이크 (사진: 크레타·산토리니 항구의 칠한 나무배)
            if (path.EndsWith("boat-fishing-small") && (S.Houses == HouseStyle.Cyclades || S.Houses == HouseStyle.Venetian)) path = "Props/boat_caique";
            int placed = 0;
            for (int i = 0; i < count * 6 && placed < count; i++)
            {
                float x = R(-190f, 190f);
                float z = S.FarShoreZ > 0f ? R(_map.ShoreAt(x) + 40f, _map.FarShoreAt(x) - 20f) : R(150f, 240f);
                var p = new Vector3(x, _map.BaseWaterY, z);
                if (!_map.IsWater(p) || _map.HeightAt(x, z) > _map.BaseWaterY - 1.5f) continue;
                if (new Vector2(x - S.Spawn.x, z - S.Spawn.z).magnitude < 50f) continue;
                KenneyProps.Spawn(path, _root, p + Vector3.down * 0.25f, R(0f, 360f), 0f, length);
                placed++;
            }
        }

        /// <summary>하롱베이 수상 마을: 플라스틱 통 위에 뜬 판잣집들과 그물 가두리 (멀리서 보는 풍경, 콜라이더 있음).</summary>
        void FloatingVillage()
        {
            var plank = Mats.Wood(new Color(0.55f, 0.42f, 0.3f), 0.1f);
            var drum = Mats.Lit(new Color(0.2f, 0.35f, 0.65f), 0.4f);
            var tin = Mats.Lit(new Color(0.6f, 0.62f, 0.62f), 0.4f, 0.3f);
            var colors = new[] { new Color(0.25f, 0.55f, 0.75f), new Color(0.85f, 0.75f, 0.55f), new Color(0.6f, 0.75f, 0.55f), new Color(0.95f, 0.95f, 0.9f) };
            float cx = 95f, cz = 205f;
            for (int i = 0; i < 9; i++)
            {
                var c = new Vector3(cx + (i % 3) * 16f + R(-3f, 3f), _map.BaseWaterY, cz + (i / 3) * 14f + R(-3f, 3f));
                if (!_map.IsWater(c)) continue;
                var t = new GameObject("FloatHouse").transform;
                t.SetParent(_root, false);
                t.localPosition = c;
                t.localRotation = Quaternion.Euler(0f, R(-10f, 10f), 0f);
                Mats.Prim(PrimitiveType.Cube, "Deck", t, new Vector3(0f, 0.35f, 0f), new Vector3(10f, 0.2f, 8f), plank);
                for (int k = 0; k < 6; k++)
                    Mats.Prim(PrimitiveType.Cylinder, "Drum", t, new Vector3(-4f + (k % 3) * 4f, 0.05f, -3f + (k / 3) * 6f), new Vector3(0.9f, 0.3f, 0.9f), drum, false);
                Mats.Prim(PrimitiveType.Cube, "Hut", t, new Vector3(0f, 1.7f, -1.2f), new Vector3(6f, 2.6f, 4.5f), Mats.Lit(colors[_rng.Next(colors.Length)], 0.1f));
                Mats.Prim(PrimitiveType.Cube, "HutRoof", t, new Vector3(0f, 3.1f, -1.2f), new Vector3(6.6f, 0.15f, 5.2f), tin, false, Quaternion.Euler(6f, 0f, 0f));
            }
        }

        /// <summary>
        /// 산토리니 칼데라: 바다 건너 멀리 높이 200m 넘는 붉은·검은 줄무늬 절벽이 활처럼 둘러 서 있고,
        /// 그 위로 하얀 마을 띠. 칼데라 가운데엔 검은 용암 섬 네아 카메니.
        /// </summary>
        /// <summary>
        /// 산토리니 칼데라: 바다 건너 반원으로 둘러선 절벽. 예전에는 거대한 구 9개를 줄지어 놓아 "반구가 늘어선" 모양이었다.
        /// 한 덩어리 절벽 메쉬(<see cref="CalderaMesh"/>)로 잇고, 붉은 화산암 / 짙은 용암층 띠 / 윗면 평지를 서브메쉬로 나누고,
        /// 절벽 위에 하얀 집·파란 지붕을 촘촘히 얹는다 (이아·피라 마을처럼).
        /// </summary>
        void Caldera()
        {
            // 사용자 참고 사진(산토리니): 붉은 갈색·검은 화산암 층이 켜켜이 쌓인 깎아지른 칼데라 벽, 위에 흰 마을이 쏟아지듯 걸려 있다
            var cliff = MapKit.FarRock(new Color(0.66f, 0.42f, 0.34f), "canyon", 45f, 0.95f);
            var band = MapKit.FarRock(new Color(0.24f, 0.21f, 0.2f), "darkrock", 30f, 0.9f);
            var top = MapKit.FarRock(new Color(0.74f, 0.64f, 0.5f), "sand", 30f, 0.6f);
            var go = new GameObject("Caldera");
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(0f, _map.BaseWaterY - 25f, 260f);
            go.AddComponent<MeshFilter>().sharedMesh = CalderaMesh(S.Seed, out var rim);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { cliff, band, top };
            // 절벽 위 마을: 하얀 집이 가장자리를 따라 빽빽하고, 절벽 위쪽으로 계단처럼 흘러내린다. 파란 돔 교회·종탑, 파스텔 집 몇 채
            var white = MapKit.Far(new Color(0.98f, 0.97f, 0.94f), 0.2f);
            var pastel = new[] { MapKit.Far(new Color(0.95f, 0.82f, 0.62f)), MapKit.Far(new Color(0.92f, 0.7f, 0.62f)), MapKit.Far(new Color(0.96f, 0.9f, 0.75f)) };
            var blue = MapKit.Far(new Color(0.1f, 0.3f, 0.72f), 0.45f);
            var window = MapKit.Far(new Color(0.12f, 0.22f, 0.38f), 0.6f);
            var origin = go.transform.localPosition;
            for (int k = 0; k < rim.Count; k++)
            {
                var (p, inward) = rim[k];
                // 마을이 모인 구간과 빈 구간 (이아·피라·이메로비글리처럼 띄엄띄엄)
                float town = Mathf.PerlinNoise(k * 0.06f + S.Seed, 2.3f);
                if (town < 0.42f) continue;
                int count = town > 0.6f ? 6 : 3;
                for (int j = 0; j < count; j++)
                {
                    // 가장자리 안쪽(평지)부터 절벽 쪽으로 흘러내린다: 음수 = 절벽면 쪽(바다 쪽으로 내려간다)
                    float back = R(-26f, 34f);
                    float drop = back < 0f ? -back * 0.9f : 0f;
                    var at = origin + p + inward * back + Vector3.down * drop;
                    float w = R(6f, 12f), h = R(4f, 8f);
                    var mat = _rng.NextDouble() < 0.15 ? pastel[_rng.Next(pastel.Length)] : white;
                    var rot = Quaternion.LookRotation(-inward) * Quaternion.Euler(0f, R(-12f, 12f), 0f);
                    Mats.Prim(PrimitiveType.Cube, "CliffHouse", _root, at + Vector3.up * h * 0.45f, new Vector3(w, h, w * R(0.7f, 1.1f)), mat, false, rot);
                    // 바다 쪽 면의 창 + 가끔 원통 지붕
                    Mats.Prim(PrimitiveType.Cube, "CliffWindow", _root, at + Vector3.up * h * 0.5f - inward * (w * 0.4f), new Vector3(w * 0.5f, h * 0.3f, 0.4f), window, false, rot);
                    if (_rng.NextDouble() < 0.25)
                        Mats.Prim(PrimitiveType.Cylinder, "Vault", _root, at + Vector3.up * h * 0.95f, new Vector3(w * 0.9f, w * 0.3f, w * 0.5f), mat, false, rot * Quaternion.Euler(0f, 0f, 90f));
                    if (_rng.NextDouble() < 0.07)
                    {
                        // 파란 돔 교회 + 종탑
                        Mats.Prim(PrimitiveType.Sphere, "CliffDome", _root, at + Vector3.up * (h * 0.95f), Vector3.one * w * 0.65f, blue, false);
                        Mats.Prim(PrimitiveType.Cube, "BellTower", _root, at + Vector3.up * (h + 3f) + Vector3.Cross(Vector3.up, inward) * w * 0.6f, new Vector3(2.4f, 6f, 2.4f), white, false, rot);
                    }
                }
            }
            var lava = Mats.Textured("rock", new Color(0.22f, 0.2f, 0.2f), new Vector2(8f, 4f), 0.1f);
            Rocks.Prim("NeaKameni", _root, new Vector3(-80f, _map.BaseWaterY + 6f, 520f), new Vector3(260f, 55f, 180f), lava);
        }

        /// <summary>
        /// 칼데라 절벽 메쉬: 각도 -78°~78°의 타원 호(가로 900, 세로 700) 위로 바다(-y)에서 절벽 꼭대기까지 올라가 안쪽 평지로 이어진다.
        /// 높이는 노이즈로 오르내리고(150~230m), 절벽면은 세로 협곡·층 굴곡이 있다. 서브메쉬 0 = 붉은 절벽, 1 = 짙은 용암층 띠, 2 = 윗면.
        /// rim: 꼭대기 가장자리 점과 안쪽(마을 놓을 쪽) 방향.
        /// </summary>
        static Mesh CalderaMesh(int seed, out List<(Vector3 p, Vector3 inward)> rim)
        {
            const int seg = 140, rows = 16;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            rim = new List<(Vector3, Vector3)>();
            float sd = seed * 0.71f;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-78f, 78f, i / (float)seg) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a) * 900f, 0f, Mathf.Cos(a) * 700f);
                var outward = new Vector3(Mathf.Sin(a) / 900f, 0f, Mathf.Cos(a) / 700f).normalized;
                float hgt = 150f + 80f * Mathf.PerlinNoise(sd + i * 0.045f, 3.1f) + 25f * (Mathf.PerlinNoise(sd + i * 0.21f, 8.7f) - 0.5f);
                // 세로 협곡: 몇 군데 절벽이 안으로 파였다
                float gully = Mathf.Pow(Mathf.Abs(Mathf.Sin(i * 0.31f + sd)), 14f) * 18f;
                for (int r = 0; r <= rows; r++)
                {
                    float t = r / (float)rows;
                    Vector3 p;
                    if (r < rows - 2)
                    {
                        // 절벽면: 아래는 바다 밑에서 시작, 위로 갈수록 살짝 뒤로 기운다. 층마다 튀어나오고 들어간다
                        float ft = t / ((rows - 2f) / rows);
                        float y = Mathf.Lerp(-10f, hgt, ft);
                        float lean = ft * 55f + Mathf.Sin(ft * 9f + i * 0.17f) * 6f + (Mathf.PerlinNoise(sd + i * 0.3f, ft * 6f) - 0.5f) * 22f + gully * (1f - ft * 0.5f);
                        p = dir + outward * lean + Vector3.up * y;
                    }
                    else
                    {
                        // 윗면: 가장자리에서 바깥(섬 안쪽)으로 평지
                        float back = r == rows - 1 ? 70f : 260f;
                        p = dir + outward * (55f + back) + Vector3.up * (hgt + (r == rows - 1 ? 3f : 12f));
                    }
                    v.Add(p);
                    uv.Add(new Vector2(i / (float)seg * 30f, p.y / 40f));
                    if (r == rows - 2) rim.Add((p, outward));
                }
            }
            var cliffT = new List<int>();
            var bandT = new List<int>();
            var topT = new List<int>();
            for (int i = 0; i < seg; i++)
            for (int r = 0; r < rows; r++)
            {
                int a0 = i * (rows + 1) + r, a1 = a0 + 1, b0 = a0 + rows + 1, b1 = b0 + 1;
                // 바다 쪽(-outward, 원점 쪽)에서 보이게: 호의 안쪽이 앞면
                var tri = new[] { a0, a1, b0, b0, a1, b1 };
                float yMid = (v[a0].y + v[a1].y) * 0.5f;
                float hgtHere = v[i * (rows + 1) + rows - 2].y;
                var list = r >= rows - 2 ? topT : (yMid > hgtHere * 0.42f && yMid < hgtHere * 0.55f) ? bandT : cliffT;
                list.AddRange(tri);
            }
            var m = new Mesh { name = "Caldera", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.subMeshCount = 3;
            m.SetTriangles(cliffT, 0);
            m.SetTriangles(bandT, 1);
            m.SetTriangles(topT, 2);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// 나일강 하류 서쪽 사막 너머 기자 피라미드 셋 (사용자 참고 사진: 노을에 물든 계단진 석회암 면, 카프레 피라미드 꼭대기에만 매끈한 외장석이 남았다).
        /// </summary>
        void Pyramids()
        {
            var stone = MapKit.FarRock(new Color(0.84f, 0.7f, 0.5f), "sandstone", 6f, 0.9f);
            var casing = MapKit.FarRock(new Color(0.9f, 0.82f, 0.66f), "whitestone", 10f, 0.4f);
            var pts = new[] { (new Vector3(-260f, 0f, 700f), 146f), (new Vector3(-80f, 0f, 760f), 136f), (new Vector3(70f, 0f, 820f), 65f) };
            foreach (var (p, h) in pts)
            {
                var go = new GameObject("Pyramid");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = p + Vector3.up * (_map.LandY - 2f);
                go.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                // 카프레(가운데)만 꼭대기 1/4에 외장석이 남았다
                float cap = h == 136f ? 0.24f : 0f;
                go.AddComponent<MeshFilter>().sharedMesh = PyramidMesh(h * 1.57f, h, 46, cap);
                go.AddComponent<MeshRenderer>().sharedMaterials = new[] { stone, casing };
            }
        }

        /// <summary>
        /// 계단진 피라미드: courses 단을 층마다 안으로 들여 쌓는다 (단마다 세운 면 + 디딤 면, 모서리는 닳아 높이가 조금씩 다르다).
        /// capFrac > 0이면 꼭대기 그 비율만큼은 매끈한 외장석 사면(서브메쉬 1). 재질이 트라이플래너라 UV는 없다.
        /// </summary>
        static Mesh PyramidMesh(float baseW, float height, int courses, float capFrac)
        {
            var v = new List<Vector3>();
            var t0 = new List<int>();
            var t1 = new List<int>();
            float b = baseW * 0.5f;
            int smoothFrom = capFrac > 0f ? Mathf.FloorToInt(courses * (1f - capFrac)) : courses;
            float step = height / courses;
            void Quad(List<int> t, Vector3 a, Vector3 b2, Vector3 c, Vector3 d)
            {
                int i = v.Count;
                v.Add(a); v.Add(b2); v.Add(c); v.Add(d);
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            var rng = new System.Random(courses * 31 + (int)height);
            for (int k = 0; k < smoothFrom; k++)
            {
                float y0 = k * step, y1 = (k + 1) * step;
                float r0 = b * (1f - k / (float)courses), r1 = b * (1f - (k + 1) / (float)courses);
                float rr = r0 - (r0 - r1) * 0.25f; // 세운 면은 디딤 앞쪽에서 조금 들어가 있다
                for (int s = 0; s < 4; s++)
                {
                    var rot = Quaternion.Euler(0f, s * 90f, 0f);
                    float wear = (float)rng.NextDouble() * step * 0.15f;
                    Quad(t0, rot * new Vector3(-rr, y0, -rr), rot * new Vector3(-rr, y1 - wear, -rr), rot * new Vector3(rr, y1 - wear, -rr), rot * new Vector3(rr, y0, -rr));
                    Quad(t0, rot * new Vector3(-rr, y1 - wear, -rr), rot * new Vector3(-r1, y1, -r1), rot * new Vector3(r1, y1, -r1), rot * new Vector3(rr, y1 - wear, -rr));
                }
            }
            if (smoothFrom < courses)
            {
                float y0 = smoothFrom * step;
                float r0 = b * (1f - smoothFrom / (float)courses);
                var apex = new Vector3(0f, height, 0f);
                for (int s = 0; s < 4; s++)
                {
                    var rot = Quaternion.Euler(0f, s * 90f, 0f);
                    int i = v.Count;
                    v.Add(rot * new Vector3(-r0, y0, -r0)); v.Add(apex); v.Add(rot * new Vector3(r0, y0, -r0));
                    t1.AddRange(new[] { i, i + 1, i + 2 });
                }
            }
            var m = new Mesh { name = "Pyramid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.subMeshCount = 2;
            m.SetTriangles(t0, 0);
            m.SetTriangles(t1, 1);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
