using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class OverseasCoasts
    {
        /// <summary>
        /// 방콕 (Lv.31): 짜오프라야 강가 선착장. 강 건너 새벽 사원 왓 아룬(가운데 높은 프랑 + 네 귀퉁이 작은 프랑, 도자기 조각 모자이크),
        /// 강가의 가파른 겹지붕 티크 집, 롱테일 보트와 수상 버스, 멀리 방콕 빌딩숲. 민물 (원작 2차 — 실제 장소를 따랐다).
        /// </summary>
        public static CoastSpec Bangkok() => new CoastSpec
        {
            Id = "bangkok", MapName = "방콕 · 짜오프라야강", ShortName = "방콕", ShopName = "짜오프라야 낚시점", UnionContact = "강변 선착장의 니란 지부장",
            LandName = "타 띠엔 선착장", SeaName = "짜오프라야강", Seed = 54, River = true, FarShoreZ = 165f,
            WaterDeep = new Color(0.14f, 0.15f, 0.1f), WaterShallow = new Color(0.38f, 0.33f, 0.22f),
            Shores = new[] { new ShoreSeg(-999f, -60f, ShoreKind.Beach), new ShoreSeg(-60f, 60f, ShoreKind.Quay), new ShoreSeg(60f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.04f, FlatWidth = 36f, CliffHeight = 3f, CliffWidth = 50f, RoadOffset = 18f, VillageDensity = 1.1f,
            Houses = HouseStyle.Thai, TreeKind = TreeStyle.Tropical,
            Platforms = new[] { new PlatformSpec { Area = new Rect(12f, -41f, 6f, 24f), Kind = PlatformKind.Pier, Name = "수상 버스 선착장" } },
            Grid = new[,]
            {
                { W, S, S, S, S, S, S, W },
                { S, D, D, D, D, D, D, S },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { S, W, S, S, S, S, W, S },
                { W, W, W, W, W, W, W, W },
                { W, W, W, W, W, W, W, W },
            },
            Spawn = new Vector3(4f, 1.1f, -47f), ShopPos = new Vector3(-22f, 1f, -54f),
            ShopRoof = new Color(0.82f, 0.38f, 0.16f),
            ShopOwner = Local("짜오프라야 낚시점 쏨삭", "짜오프라야 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("니란 지부장", "한국 낚시 조합 방콕 지부", true), NpcAngler.Role.Talker, "union"),
                Npc(15f, -22f, 0f, Local("수상 버스 차장", "다음 배는 10분 뒤", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-40f, -48f, 140f, Local("사원 가는 스님", "강을 건너려고", false), NpcAngler.Role.Talker),
                Npc(-72f, -44f, 10f, Local("낚시공원 단골", "피라루쿠 사진이 자랑", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("THA TIEN PIER", new Vector3(30f, 6.5f, -62f), SignGold) },
            // 왓 아룬 터
            Reserved = new[] { new Rect(-70f, 160f, 160f, 110f) },
            Schools = () => new List<FishSchool>
            {
                School("파쿠 떼", 2, 0, 1, DayLong, "pacu", "silverbarb"),
                School("구라미 수초", 0, 0, 1, MornDay, "giantgourami", "moonlightgourami"),
                School("가물치 수초", 7, 0, 1, MornDay, "giantsnakehead", "stripedsnakehead"),
                School("줄무늬 메기 떼", 3, 2, 4, DayLong, "stripedcatfish", "wallago"),
                School("앨리게이터 가 얕은 물", 6, 1, 2, DayLong, "alligatorgar", "hampalabarb"),
                School("나이프피시 그늘", 1, 1, 3, EveNight, "clownknifefish", "wallago"),
                // 피라루쿠: 아침 D4, 낮 F5 (숨 쉬러 수면에 오른다)
                Special("피라루쿠 어군", "arapaima", (TimeBand.Morning, 3, 3), (TimeBand.Day, 5, 4)),
            },
            Extra = BangkokExtra,
        };

        static Texture2D _mosaic;

        /// <summary>왓 아룬 겉면: 크림색 회벽에 박은 도자기 조각 꽃무늬 (분홍·파랑·노랑·초록 점, 꽃잎 고리).</summary>
        static Texture2D Mosaic()
        {
            if (_mosaic != null) return _mosaic;
            const int N = 128;
            _mosaic = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[N * N];
            var cols = new[] { new Color(0.85f, 0.45f, 0.5f), new Color(0.35f, 0.55f, 0.8f), new Color(0.92f, 0.78f, 0.35f), new Color(0.45f, 0.68f, 0.45f), new Color(0.95f, 0.95f, 0.92f) };
            var rng = new System.Random(77);
            for (int i = 0; i < px.Length; i++)
            {
                float n = Mathf.PerlinNoise(i % N * 0.15f, i / N * 0.15f);
                px[i] = new Color(0.86f, 0.82f, 0.74f) * (0.9f + 0.15f * n);
            }
            // 꽃: 가운데 점 + 꽃잎 6개
            for (int k = 0; k < 42; k++)
            {
                int cx = rng.Next(N), cy = rng.Next(N);
                var c = cols[rng.Next(cols.Length)];
                var c2 = cols[rng.Next(cols.Length)];
                for (int dy = -7; dy <= 7; dy++)
                for (int dx = -7; dx <= 7; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dy, dx);
                    float petal = 4.5f + 2f * Mathf.Cos(a * 6f);
                    if (d > petal) continue;
                    int x = (cx + dx + N) % N, y = (cy + dy + N) % N;
                    px[y * N + x] = d < 2f ? c2 : c * (0.85f + 0.15f * (petal - d) / petal);
                }
            }
            _mosaic.SetPixels(px);
            _mosaic.Apply(true);
            return _mosaic;
        }

        /// <summary>방콕만의 풍경: 강 건너 왓 아룬, 먼 빌딩숲, 롱테일 보트·수상 버스.</summary>
        static void BangkokExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Bangkok").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, water = b.Map.BaseWaterY;

            // --- 왓 아룬: 3단 기단 + 가운데 높은 프랑(톱니 단면) + 네 귀퉁이 작은 프랑 ---
            var mosaic = new Material(Shader.Find("Standard")) { color = Color.white, mainTexture = Mosaic() };
            mosaic.SetFloat("_Glossiness", 0.35f);
            var white = Mats.Wall(new Color(0.9f, 0.87f, 0.8f), 0.25f);
            var gold = Mats.Lit(new Color(0.9f, 0.72f, 0.3f), 0.6f, 0.6f);
            var c = new Vector3(10f, land, 215f);
            float y = land;
            foreach (var (w, hh) in new[] { (64f, 3f), (46f, 4f), (32f, 4f) })
            {
                Mats.Prim(PrimitiveType.Cube, "WatTerrace", root, new Vector3(c.x, y + hh * 0.5f, c.z), new Vector3(w, hh, w), white, false);
                Mats.Prim(PrimitiveType.Cube, "WatRail", root, new Vector3(c.x, y + hh + 0.4f, c.z - w * 0.5f + 0.3f), new Vector3(w, 0.8f, 0.4f), mosaic, false);
                // 앞 계단 (가파르게)
                Mats.Prim(PrimitiveType.Cube, "WatStair", root, new Vector3(c.x, y + hh * 0.5f, c.z - w * 0.5f - 1.2f), new Vector3(5f, hh, 2.4f), white, false);
                y += hh;
            }
            // 프랑: Blender wat_arun (모서리를 세 번 깎은 십자형 단면, 감실·수호신상, 옥수수 모양 층층 탑 — 예전 둥근 톱니 원뿔은 도형 같았다, 2026-10-27 사용자 지적)
            bool real = MapKit.Prop(root, "wat_arun", new Vector3(c.x, y, c.z), 0f, 62f, 0f, false) != null;
            if (!real)
                MapKit.Place(root, "WatArunPrang", MapKit.Lathe(new List<Vector2>(PrangProfile(62f, 9f, 16)), 40, 3f, 0.88f), mosaic, new Vector3(c.x, y, c.z), 45f);
            // 꼭대기 삼지창 (금빛 뾰족 끝)
            MapKit.Place(root, "WatArunSpire", MapKit.Lathe(new[] { new Vector2(0.45f, 0f), new Vector2(0.12f, 5f), new Vector2(0.01f, 8f) }, 8), gold, new Vector3(c.x, y + 61.6f, c.z));
            foreach (var (dx, dz) in new[] { (-26f, -26f), (26f, -26f), (-26f, 26f), (26f, 26f) })
            {
                var at = new Vector3(c.x + dx, land + 3f, c.z + dz);
                if (!real || MapKit.Prop(root, "wat_arun", at, 0f, 26f, 0f, false) == null)
                    MapKit.Place(root, "WatArunSatellite", MapKit.Lathe(PrangProfile(26f, 4.5f, 10), 24, 3f, 0.86f), mosaic, at, 45f);
            }
            // 강가 쪽 흰 담과 정자 둘
            Mats.Prim(PrimitiveType.Cube, "WatWall", root, new Vector3(c.x, land + 1.2f, c.z - 42f), new Vector3(110f, 2.4f, 0.8f), white, true);
            foreach (float sx in new[] { -30f, 50f })
            {
                var pav = new Vector3(sx, land, c.z - 48f);
                foreach (var (dx, dz) in new[] { (-2.5f, -2.5f), (2.5f, -2.5f), (-2.5f, 2.5f), (2.5f, 2.5f) })
                    Mats.Prim(PrimitiveType.Cylinder, "SalaPost", root, pav + new Vector3(dx, 1.6f, dz), new Vector3(0.35f, 1.6f, 0.35f), white, false);
                var tile = Mats.Tile(new Color(0.82f, 0.38f, 0.16f), 0.35f);
                MapKit.Place(root, "SalaRoof", CoastWorldBuilder.Gable(), tile, pav + new Vector3(0f, 3.2f, 0f)).transform.localScale = new Vector3(8f, 3f, 7f);
                MapKit.Place(root, "SalaRoof2", CoastWorldBuilder.Gable(), tile, pav + new Vector3(0f, 4.6f, 0f)).transform.localScale = new Vector3(6f, 2.6f, 4f);
            }

            // --- 먼 방콕 빌딩숲 (강 건너 오른쪽·왼쪽 멀리, 밤엔 창 불빛) ---
            // 사진(짜오프라야 강변 스카이라인): 유리 커튼월·베이지 콘크리트 탑이 섞이고, 위로 갈수록 좁아지는 계단형 탑, 원통 탑, 옥상 왕관·안테나
            var rng = new System.Random(54);
            float Rn(float a0, float a1) => a0 + (float)rng.NextDouble() * (a1 - a0);
            var glassSets = new[]
            {
                (new Color(0.55f, 0.7f, 0.82f), new Color(0.22f, 0.3f, 0.38f), new Color(0.82f, 0.84f, 0.85f)),   // 푸른 유리
                (new Color(0.62f, 0.66f, 0.62f), new Color(0.3f, 0.34f, 0.32f), new Color(0.9f, 0.86f, 0.76f)),   // 녹회색 유리 + 베이지 틀
                (new Color(0.78f, 0.74f, 0.66f), new Color(0.4f, 0.38f, 0.34f), new Color(0.93f, 0.9f, 0.82f)),   // 베이지 콘크리트 + 띠창
                (new Color(0.35f, 0.42f, 0.5f), new Color(0.12f, 0.16f, 0.2f), new Color(0.6f, 0.62f, 0.64f)),    // 짙은 유리
            };
            var crownMat = MapKit.Far(new Color(0.82f, 0.82f, 0.8f), 0.4f, 0.3f);
            for (int i = 0; i < 24; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * Rn(110f, 430f);
                float z = Rn(330f, 590f);
                float h = Rn(40f, 150f), w = Rn(16f, 32f);
                var (gt, gb, fr) = glassSets[rng.Next(glassSets.Length)];
                int kind = rng.Next(3); // 0 상자 · 1 계단형 · 2 원통
                var tex = HanRiverLandmarks.Facade(Mathf.RoundToInt(h / 3.5f), kind == 2 ? 14 : 8, gt, gb, fr, Rn(0.15f, 0.35f), Rn(0.08f, 0.18f), 300 + i, out var night, 0.4f);
                var m = HanRiverLandmarks.Mat(Color.white, tex, night, new Color(1f, 0.85f, 0.6f), 0.55f, 0.25f, new Vector2(w / 22f, 1f));
                var at = new Vector3(x, land - 1f, z);
                GameObject Seg(PrimitiveType t, float y0, float hh, float ww)
                {
                    var g = Mats.Prim(t, "BkkTower", root, at + Vector3.up * (y0 + hh * 0.5f), new Vector3(ww, t == PrimitiveType.Cylinder ? hh * 0.5f : hh, ww), m, false);
                    g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    return g;
                }
                float topY;
                if (kind == 1)
                {
                    Seg(PrimitiveType.Cube, 0f, h * 0.55f, w);
                    Seg(PrimitiveType.Cube, h * 0.55f, h * 0.27f, w * 0.78f);
                    Seg(PrimitiveType.Cube, h * 0.82f, h * 0.18f, w * 0.55f);
                    topY = h;
                }
                else
                {
                    Seg(kind == 2 ? PrimitiveType.Cylinder : PrimitiveType.Cube, 0f, h, w);
                    topY = h;
                }
                // 옥상: 기계실 상자 + 왕관 테 + 가끔 안테나
                var crown = Mats.Prim(PrimitiveType.Cube, "BkkCrown", root, at + Vector3.up * (topY + 1.5f), new Vector3(w * 0.5f, 3f, w * 0.5f), crownMat, false);
                crown.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (rng.NextDouble() < 0.4)
                    Mats.Prim(PrimitiveType.Cylinder, "BkkAntenna", root, at + Vector3.up * (topY + 3f + h * 0.08f), new Vector3(0.6f, h * 0.08f, 0.6f), crownMat, false)
                        .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // --- 강 위: 롱테일 보트, 수상 버스(Kenney 큰 배), 선착장에 매어 둔 배 ---
            foreach (var (x, z, yaw, id) in new[] { (8f, -26f, 90f, "longtail_a"), (21f, -32f, 270f, "longtail_b"), (-60f, 40f, 100f, "longtail_b"), (90f, 70f, 260f, "longtail_a") })
                MapKit.Prop(root, id, new Vector3(x, water - 0.25f, z), yaw, 0f, 9.5f);
            KenneyProps.Spawn("Kenney/Watercraft/boat-row-large", root, new Vector3(-30f, water - 0.2f, 85f), 95f, 0f, 16f);
            KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", root, new Vector3(120f, water - 0.2f, 30f), 260f, 0f, 9f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>방콕 이야기 (Lv.31~). 첫 퀘스트는 그 낚시터 연락관(니란 지부장)이 준다.</summary>
        static QuestDef[] BangkokChapter() => new[]
        {
            new QuestDef { Id = "bangkok_1", Title = "사람 이빨 물고기", Desc = "싸왓디 카! 파쿠는 사람처럼 생긴 이빨로 열매를 깨 먹어요. 옥수수나 떡밥으로 세 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.31부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "pacu", Count = 3, RewardGold = 50000, RewardExp = 6000 },
            new QuestDef { Id = "bangkok_2", Title = "강의 악어", Desc = "주둥이가 악어 같은 앨리게이터 가가 얕은 물에 떠 있다네. 미끼고기나 루어로 한 마리. 비늘이 갑옷이라 바늘이 잘 안 박히니 챔질을 세게!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "alligatorgar", Count = 1, RewardGold = 75000, RewardExp = 8000 },
            new QuestDef { Id = "bangkok_3", Title = "숨 쉬는 거인", Desc = "아침 D4, 낮 F5에 피라루쿠가 숨을 쉬러 물 위로 오른다오. 1.8m가 넘는 놈을 낚으면 낚시공원 단골들이 줄을 서서 사진을 찍을 걸세.",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "arapaima", MinLen = 180f, Count = 1, RewardGold = 120000, RewardExp = 11000, RewardKeepNet = 3 },
        };
    }
}
