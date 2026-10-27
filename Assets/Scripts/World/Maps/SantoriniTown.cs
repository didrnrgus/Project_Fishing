using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 산토리니(암모우디 항구) 마을 (사용자 원작 스크린샷 2026-10-27: 비탈을 따라 층층이 쌓인 흰 집·파란 돔, 베이지 포석 산책로,
    /// 검은 철제 가로등, 파란 아치문, 흰 화분 꽃, 나무 상자, 파라솔 카페, 돌 부두 끝 파란 돔 정자).
    /// 예전엔 부두 뒤가 맨 붉은 절벽이고 흰 집은 절벽 위 평지에만 있었다.
    /// </summary>
    public static class SantoriniTown
    {
        static readonly Color Blue = new Color(0.1f, 0.35f, 0.72f);

        public static void Build(CoastWorldBuilder b)
        {
            var root = new GameObject("SantoriniTown").transform;
            root.SetParent(b.Root, false);
            HillVillage(b);
            Promenade(b, root);
            Pavilion(b, root, new Vector3(15f, b.Map.LandY, -11.5f));
        }

        /// <summary>
        /// 비탈 마을: 산책로 뒤 비탈에 줄마다 7m씩 물러나며 흰 집을 놓는다. 비탈(경사 ≈ 0.75)이라 뒷줄 집 바닥이 앞줄 지붕보다 높아
        /// 이아 마을처럼 계단식으로 쌓인다. 집 뒤쪽은 비탈에 박힌다(동굴집). 줄마다 x를 엇갈리고 가끔 비워 골목·계단이 보이게.
        /// </summary>
        static void HillVillage(CoastWorldBuilder b)
        {
            var map = b.Map;
            var terrace = Mats.Wall(new Color(0.93f, 0.92f, 0.89f), 0.1f);
            var floor = Mats.Stone(new Color(0.78f, 0.72f, 0.64f));
            for (int row = 0; row < 7; row++)
            {
                float back = b.Spec.FlatWidth + 5f + row * 7f;
                for (float x = -135f + (row % 2) * 4.5f; x < 135f; x += b.Rand(7f, 9f))
                {
                    if (b.Rand(0f, 1f) < 0.08f) continue;   // 골목
                    float z = map.ShoreAt(x) - back + b.Rand(-1.2f, 1.2f);
                    if (z < CoastMapDef.TerrainMinZ + 8f || b.IsReservedOrFeature(x, z)) continue;
                    // 앞(바다 쪽) 땅 높이에 바닥을 맞춘다 — 뒤는 비탈에 묻힌다
                    float y = b.Height(x, z + 3f);
                    if (y < map.LandY + 1.5f) continue;   // 산책로 높이는 비운다
                    bool church = b.Rand(0f, 1f) < 0.09f;
                    b.PlaceCycladic(new Vector3(x, y - 0.1f, z), b.Rand(-6f, 6f), church, 9f);
                    // 흰 축대: 집 앞 테라스 바닥 + 아래 줄 지붕까지 내려가는 회벽 옹벽 (집 사이 흙이 덜 드러나 흰 마을이 이어진다)
                    float tw = b.Rand(7f, 10f);
                    Mats.Prim(PrimitiveType.Cube, "TerraceWall", b.Root, new Vector3(x, y - 3.2f, z + 4.1f), new Vector3(tw, 6.4f, 1.2f), terrace, false);
                    Mats.Prim(PrimitiveType.Cube, "TerraceFloor", b.Root, new Vector3(x, y - 0.05f, z + 3.6f), new Vector3(tw, 0.12f, 1.8f), floor, false);
                    Mats.Prim(PrimitiveType.Cube, "TerraceParapet", b.Root, new Vector3(x, y + 0.45f, z + 4.55f), new Vector3(tw, 0.9f, 0.3f), terrace, false);
                }
            }
            // 비탈 사이 덤불·올리브 (회색 바위 틈 초록)
            for (int i = 0; i < 70; i++)
            {
                float x = b.Rand(-140f, 140f);
                float z = map.ShoreAt(x) - b.Rand(b.Spec.FlatWidth + 3f, b.Spec.FlatWidth + 55f);
                if (b.IsReservedOrFeature(x, z)) continue;
                b.PlaceTree(new Vector3(x, b.Height(x, z) - 0.1f, z));
            }
        }

        /// <summary>산책로 꾸밈: 바다 쪽 흰 턱, 12m마다 검은 철제 가로등, 파란 아치문, 흰 화분 꽃, 나무 상자 더미, 파라솔 카페.</summary>
        static void Promenade(CoastWorldBuilder b, Transform root)
        {
            var map = b.Map;
            float y = map.LandY;
            var white = Mats.Wall(new Color(0.97f, 0.97f, 0.95f), 0.12f);
            var iron = Mats.Lit(new Color(0.12f, 0.11f, 0.1f), 0.35f, 0.4f);
            var glass = HanRiverLandmarks.Mat(new Color(1f, 0.92f, 0.7f), null, Texture2D.whiteTexture, new Color(1f, 0.78f, 0.45f) * 3f, 0.3f, 0f, Vector2.one);
            var blue = Mats.Wood(Blue, 0.35f);
            var wood = Mats.Wood(new Color(0.55f, 0.4f, 0.25f));
            var leaf = Mats.Lit(new Color(0.22f, 0.42f, 0.16f), 0.2f);
            var flowers = new[] { Mats.Lit(new Color(0.9f, 0.2f, 0.45f), 0.2f), Mats.Lit(new Color(0.95f, 0.85f, 0.3f), 0.2f), Mats.Lit(new Color(0.75f, 0.3f, 0.8f), 0.2f) };
            const float x0 = -48f, x1 = 34f;   // 부두(Quay) 구간

            // 바다 쪽 흰 턱 (원작: 포석 끝을 흰 회벽 턱이 두른다). 낚시를 막지 않게 낮고 콜라이더 없이
            for (float x = x0; x < x1; x += 4f)
            {
                float zs = map.ShoreAt(x + 2f);
                Mats.Prim(PrimitiveType.Cube, "QuayCurb", root, new Vector3(x + 2f, y + 0.12f, zs - 0.35f), new Vector3(4.02f, 0.28f, 0.5f), white, false);
            }

            // 가로등: 산책로 뒤쪽을 따라. 원작의 검은 철 기둥 + 등롱 두 개
            for (float x = x0 + 4f; x < x1; x += 12f)
            {
                var p = new Vector3(x, y, map.ShoreAt(x) - b.Spec.FlatWidth + 2.5f);
                Mats.Prim(PrimitiveType.Cylinder, "LampBase", root, p + Vector3.up * 0.3f, new Vector3(0.45f, 0.3f, 0.45f), iron, false);
                Mats.Prim(PrimitiveType.Cylinder, "LampPole", root, p + Vector3.up * 2.2f, new Vector3(0.14f, 2f, 0.14f), iron, false);
                Mats.Prim(PrimitiveType.Cube, "LampArm", root, p + Vector3.up * 4.1f, new Vector3(1.6f, 0.08f, 0.08f), iron, false);
                foreach (float s in new[] { -0.75f, 0.75f })
                {
                    var head = p + new Vector3(s, 3.75f, 0f);
                    Mats.Prim(PrimitiveType.Cube, "LampCage", root, head, new Vector3(0.36f, 0.5f, 0.36f), iron, false);
                    var g = Mats.Prim(PrimitiveType.Cube, "LampGlass", root, head, new Vector3(0.3f, 0.42f, 0.3f), glass, false);
                    b.AddBulb(g.GetComponent<Renderer>());
                }
                b.AddLamp(p + Vector3.up * 3.6f, new Color(1f, 0.8f, 0.55f), 11f, 1.3f);
            }

            // 파란 아치문: 산책로 가운데, 바다를 향해 (흰 기둥 둘 + 파란 아치 테)
            {
                float x = -4f;
                var p = new Vector3(x, y, map.ShoreAt(x) - 6f);
                var gate = new GameObject("BlueArch").transform;
                gate.SetParent(root, false);
                gate.localPosition = p;
                foreach (float s in new[] { -1f, 1f })
                {
                    Mats.Prim(PrimitiveType.Cube, "ArchPillar", gate, new Vector3(s * 1.9f, 1.4f, 0f), new Vector3(0.75f, 2.8f, 0.75f), white, false);
                    Mats.Prim(PrimitiveType.Cube, "ArchPillarCap", gate, new Vector3(s * 1.9f, 2.86f, 0f), new Vector3(0.9f, 0.12f, 0.9f), white, false);
                }
                // 반원 아치: 마디 12개
                for (int i = 0; i < 12; i++)
                {
                    float a0 = Mathf.PI * i / 12f, a1 = Mathf.PI * (i + 1) / 12f;
                    var q0 = new Vector3(Mathf.Cos(a0) * 1.9f, 2.92f + Mathf.Sin(a0) * 1.9f, 0f);
                    var q1 = new Vector3(Mathf.Cos(a1) * 1.9f, 2.92f + Mathf.Sin(a1) * 1.9f, 0f);
                    var d = q1 - q0;
                    Mats.Prim(PrimitiveType.Cube, "ArchRing", gate, (q0 + q1) * 0.5f, new Vector3(0.8f, 0.5f, d.magnitude + 0.05f), i % 2 == 0 ? blue : white, false,
                        Quaternion.LookRotation(d.normalized, Vector3.forward));
                }
                Mats.Prim(PrimitiveType.Cube, "ArchBell", gate, new Vector3(0f, 5.3f, 0f), new Vector3(0.5f, 0.6f, 0.4f), white, false);
            }

            // 흰 화분 (꽃이 핀 덤불)과 나무 상자 더미
            for (int i = 0; i < 9; i++)
            {
                float x = x0 + 6f + i * 9f + b.Rand(-1.5f, 1.5f);
                if (Mathf.Abs(x - -4f) < 4f || Mathf.Abs(x - -20f) < 7f) continue;   // 아치문·낚시점 앞
                var p = new Vector3(x, y, map.ShoreAt(x) - b.Spec.FlatWidth + 1.2f);
                Mats.Prim(PrimitiveType.Cube, "Planter", root, p + Vector3.up * 0.35f, new Vector3(1.6f, 0.7f, 0.8f), white, false);
                for (int k = 0; k < 5; k++)
                    Mats.Prim(PrimitiveType.Sphere, "PlanterBush", root, p + new Vector3(b.Rand(-0.6f, 0.6f), 0.85f + b.Rand(0f, 0.25f), b.Rand(-0.2f, 0.2f)),
                        Vector3.one * b.Rand(0.45f, 0.7f), k < 3 ? leaf : flowers[(i + k) % flowers.Length], false);
                if (i % 3 == 1)
                    for (int k = 0; k < 3; k++)
                        Mats.Prim(PrimitiveType.Cube, "Crate", root, p + new Vector3(2.2f + (k % 2) * 0.95f, 0.4f + (k / 2) * 0.82f, b.Rand(-0.1f, 0.1f)),
                            new Vector3(0.85f, 0.8f, 0.8f), wood, false, Quaternion.Euler(0f, b.Rand(-8f, 8f), 0f));
            }

            // 타베르나 카페: 요르고스(-36, -46) 앞 흰 파라솔 탁자 넷
            var cloth = Mats.Lit(new Color(0.96f, 0.95f, 0.9f), 0.15f);
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(-44f + (i % 2) * 6f, y, -42f + (i / 2) * 4.5f);
                Mats.Prim(PrimitiveType.Cylinder, "CafeTable", root, p + Vector3.up * 0.74f, new Vector3(0.9f, 0.03f, 0.9f), white, false);
                Mats.Prim(PrimitiveType.Cylinder, "CafeLeg", root, p + Vector3.up * 0.37f, new Vector3(0.08f, 0.37f, 0.08f), iron, false);
                Mats.Prim(PrimitiveType.Cylinder, "ParasolPole", root, p + Vector3.up * 1.4f, new Vector3(0.05f, 1.4f, 0.05f), wood, false);
                MapKit.Place(root, "Parasol", MapKit.Lathe(new[] { new Vector2(1.7f, 0f), new Vector2(0.05f, 0.55f) }, 8), cloth, p + Vector3.up * 2.35f);
                foreach (float s in new[] { -1f, 1f })
                {
                    Mats.Prim(PrimitiveType.Cube, "CafeChair", root, p + new Vector3(s * 0.75f, 0.45f, 0f), new Vector3(0.45f, 0.06f, 0.45f), blue, false);
                    Mats.Prim(PrimitiveType.Cube, "CafeChairBack", root, p + new Vector3(s * 0.98f, 0.75f, 0f), new Vector3(0.06f, 0.6f, 0.45f), blue, false);
                }
            }
        }

        /// <summary>
        /// 돌 부두 끝 정자 (원작 산토리니의 상징): 계단 단 위 네 모서리 흰 기둥, 기둥 사이 아치, 위 흰 드럼에 파란 돔과 십자가, 둘레 낮은 난간.
        /// </summary>
        static void Pavilion(CoastWorldBuilder b, Transform root, Vector3 at)
        {
            var t = new GameObject("BluePavilion").transform;
            t.SetParent(root, false);
            t.localPosition = at;
            var white = Mats.Wall(new Color(0.97f, 0.97f, 0.95f), 0.12f);
            var stone = Mats.Stone(new Color(0.84f, 0.78f, 0.68f));
            var blue = Mats.Wood(Blue, 0.4f);
            var gold = Mats.Lit(new Color(0.9f, 0.75f, 0.35f), 0.6f, 0.6f);
            const float half = 2.3f, colH = 3.4f;
            Mats.Prim(PrimitiveType.Cube, "PavilionStep", t, new Vector3(0f, 0.12f, 0f), new Vector3(half * 2f + 1.4f, 0.24f, half * 2f + 1.4f), stone, false);
            Mats.Prim(PrimitiveType.Cube, "PavilionFloor", t, new Vector3(0f, 0.32f, 0f), new Vector3(half * 2f + 0.6f, 0.2f, half * 2f + 0.6f), stone, false);
            float baseY = 0.42f;
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                Mats.Prim(PrimitiveType.Cube, "PavilionColumn", t, new Vector3(sx * half, baseY + colH * 0.5f, sz * half), new Vector3(0.55f, colH, 0.55f), white, false);
                Mats.Prim(PrimitiveType.Cube, "PavilionColumnFoot", t, new Vector3(sx * half, baseY + 0.15f, sz * half), new Vector3(0.75f, 0.3f, 0.75f), white, false);
            }
            // 네 면 아치 (기둥 사이를 잇는 반원 테) + 위 보
            for (int f = 0; f < 4; f++)
            {
                var rot = Quaternion.Euler(0f, f * 90f, 0f);
                for (int i = 0; i < 10; i++)
                {
                    float a0 = Mathf.PI * i / 10f, a1 = Mathf.PI * (i + 1) / 10f;
                    var q0 = rot * new Vector3(Mathf.Cos(a0) * (half - 0.27f), baseY + colH - 1.6f + Mathf.Sin(a0) * 1.2f, half);
                    var q1 = rot * new Vector3(Mathf.Cos(a1) * (half - 0.27f), baseY + colH - 1.6f + Mathf.Sin(a1) * 1.2f, half);
                    var d = q1 - q0;
                    Mats.Prim(PrimitiveType.Cube, "PavilionArch", t, (q0 + q1) * 0.5f, new Vector3(0.5f, 0.25f, d.magnitude + 0.04f), white, false,
                        Quaternion.LookRotation(d.normalized, rot * Vector3.forward));
                }
                Mats.Prim(PrimitiveType.Cube, "PavilionBeam", t, rot * new Vector3(0f, baseY + colH + 0.25f, half), new Vector3(half * 2f + 0.6f, 0.5f, 0.6f), white, false, rot);
                // 낮은 난간 (바다 쪽 세 면, 육지 쪽은 입구)
                if (f != 2)
                    Mats.Prim(PrimitiveType.Cube, "PavilionRail", t, rot * new Vector3(0f, baseY + 0.45f, half), new Vector3(half * 2f - 0.5f, 0.12f, 0.12f), white, false, rot);
            }
            Mats.Prim(PrimitiveType.Cube, "PavilionRoof", t, new Vector3(0f, baseY + colH + 0.6f, 0f), new Vector3(half * 2f + 0.6f, 0.2f, half * 2f + 0.6f), white, false);
            Mats.Prim(PrimitiveType.Cylinder, "PavilionDrum", t, new Vector3(0f, baseY + colH + 1.2f, 0f), new Vector3(3.2f, 0.5f, 3.2f), white, false);
            MapKit.Place(t, "PavilionDome", MapKit.Lathe(new[] { new Vector2(1.7f, 0f), new Vector2(1.62f, 0.55f), new Vector2(1.35f, 1.1f), new Vector2(0.9f, 1.55f), new Vector2(0.35f, 1.8f), new Vector2(0.02f, 1.86f) }, 20),
                blue, new Vector3(0f, baseY + colH + 1.7f, 0f));
            Mats.Prim(PrimitiveType.Cube, "PavilionCross", t, new Vector3(0f, baseY + colH + 4.0f, 0f), new Vector3(0.08f, 0.8f, 0.08f), gold, false);
            Mats.Prim(PrimitiveType.Cube, "PavilionCrossBar", t, new Vector3(0f, baseY + colH + 4.15f, 0f), new Vector3(0.4f, 0.08f, 0.08f), gold, false);
            b.AddLamp(at + Vector3.up * (baseY + colH - 0.2f), new Color(1f, 0.85f, 0.6f), 9f, 1.2f);
        }
    }
}
