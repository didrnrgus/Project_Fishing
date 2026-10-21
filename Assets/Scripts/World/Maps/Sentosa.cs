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
        /// 센토사 (Lv.35): 동남아 여행의 종착역. 해변 언덕의 거대한 머라이언, 해협 건너 마리나 스카이라인(세 동을 잇는 배 모양 옥상 마리나 베이 샌즈),
        /// 하늘에 뜬 열기구, 요트 마리나 폰툰, 돌 포장 산책로와 아치 지붕 건물 (원작 3차 영상 0:08~0:22).
        /// </summary>
        public static CoastSpec Sentosa() => new CoastSpec
        {
            Id = "sentosa", MapName = "센토사 · 싱가포르", ShortName = "센토사", ShopName = "센토사 마리나 낚시점", UnionContact = "머라이언 광장의 탄 지부장",
            LandName = "머라이언 광장", SeaName = "싱가포르 해협", Seed = 57, SandTint = new Color(1.5f, 1.45f, 1.32f), StoneQuay = true, StoneTint = new Color(0.8f, 0.76f, 0.7f),
            WaterDeep = new Color(0.03f, 0.18f, 0.24f), WaterShallow = new Color(0.12f, 0.42f, 0.42f),
            Shores = new[] { new ShoreSeg(-999f, -110f, ShoreKind.Beach), new ShoreSeg(-110f, 60f, ShoreKind.Quay), new ShoreSeg(60f, 999f, ShoreKind.Beach) },
            NearDepth = 4f, DepthSlope = 0.05f, FlatWidth = 30f, CliffHeight = 20f, CliffWidth = 50f, RoadOffset = 16f, VillageDensity = 0.5f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Tropical,
            Platforms = new[] { new PlatformSpec { Area = new Rect(36f, -41f, 4f, 44f), Kind = PlatformKind.Pier, Name = "마리나 폰툰" } },
            // 사용자 참고 사진: 팔라완 해변 앞 출렁다리로 건너는 작은 섬 (야자수, 전망 탑)
            Islets = new[] { new IsletSpec { Center = new Vector3(-150f, 0f, 2f), Radius = 12f, Height = 5f, Walkable = true, Trees = true, Name = "팔라완 작은 섬" } },
            Grid = new[,]
            {
                { S, S, W, S, S, S, S, S },
                { W, D, D, D, D, D, R, S },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(4f, 1.1f, -47f), ShopPos = new Vector3(-24f, 1f, -56f),
            ShopRoof = new Color(0.15f, 0.48f, 0.5f),
            ShopOwner = Local("마리나 낚시점 림 사장", "센토사 마리나 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("탄 지부장", "한국 낚시 조합 싱가포르 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(38f, -2f, 0f, Local("요트 선장 라즈", "주말엔 GT 원정", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-58f, -48f, 300f, Local("머라이언 기념사진 작가", "한 장에 10달러", true), NpcAngler.Role.Talker),
                Npc(-140f, -44f, 20f, Local("해양 스포츠 강사", "카이트서핑 하러 왔다", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("SENTOSA", new Vector3(10f, 6.5f, -64f), SignGold) },
            // 머라이언 언덕
            Reserved = new[] { new Rect(-112f, -105f, 50f, 50f) },
            Schools = () => new List<FishSchool>
            {
                School("스내퍼 바위", 6, 1, 3, DayLong, "russellsnapper", "goldensnapper", "spangledemperor"),
                School("독가시치 수초", 2, 0, 1, DayLong, "rabbitfish", "archerfish"),
                School("스레드핀 얕은 물", 4, 0, 1, EveNight, "threadfin", "ladyfish"),
                School("타폰 물길", 1, 1, 2, DawnEve, "tarpon", "mangrovejack"),
                School("퀸피시 떼", 3, 2, 4, MornDay, "queenfish", "orangegrouper"),
                // GT: 새벽 D5, 저녁 G4 (머라이언 앞 물살)
                Special("GT 어군", "gianttrevally", (TimeBand.Dawn, 3, 4), (TimeBand.Evening, 6, 3)),
            },
            Extra = SentosaExtra,
        };

        /// <summary>센토사만의 풍경: 머라이언, 해협 건너 스카이라인과 마리나 베이 샌즈, 열기구, 요트, 아치 지붕 건물.</summary>
        static void SentosaExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Sentosa").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY, water = b.Map.BaseWaterY;

            // --- 머라이언: 언덕 위 돌 단에 서서 바다(해협 건너 도시)를 본다 ---
            var ml = new Vector3(-88f, 0f, -80f);
            ml.y = b.Height(ml.x, ml.z) - 0.3f;
            var stone = Mats.Textured("concrete", new Color(0.86f, 0.84f, 0.8f), 2f, 0.2f);
            Mats.Prim(PrimitiveType.Cylinder, "MerlionPlinth", root, ml + new Vector3(0f, 1f, 0f), new Vector3(20f, 1.4f, 20f), stone, true);
            MapKit.Prop(root, "merlion", ml + new Vector3(0f, 2.3f, 0f), MapKit.Face(ml, new Vector3(-10f, 0f, 120f)), 34f);
            // 머라이언까지 오르는 계단 길
            Mats.Prim(PrimitiveType.Cube, "MerlionSteps", root, Vector3.Lerp(ml, new Vector3(-70f, land, -52f), 0.5f) + Vector3.up * 0.5f, new Vector3(6f, 1f, 30f), stone, false,
                Quaternion.Euler(-Mathf.Atan2(ml.y - land, 30f) * Mathf.Rad2Deg, MapKit.Face(new Vector3(-70f, 0f, -52f), ml), 0f));

            // --- 해협 건너 마리나: 땅 띠 + 빌딩숲 + 마리나 베이 샌즈 ---
            var shoreMat = MapKit.Far(new Color(0.46f, 0.5f, 0.42f), 0.05f);
            Mats.Prim(PrimitiveType.Cube, "FarShoreLand", root, new Vector3(0f, water + 0.6f, 520f), new Vector3(1400f, 1.6f, 180f), shoreMat, false);
            var rng = new System.Random(57);
            for (int i = 0; i < 26; i++)
            {
                float x = -520f + i * 40f + (float)rng.NextDouble() * 16f;
                if (x > -80f && x < 120f) continue; // 샌즈 자리
                float h = 50f + (float)rng.NextDouble() * (Mathf.Abs(x) < 300f ? 150f : 70f), w = 20f + (float)rng.NextDouble() * 14f;
                float z = 540f + (float)rng.NextDouble() * 60f;
                var tex = HanRiverLandmarks.Facade(Mathf.RoundToInt(h / 3.6f), 8, new Color(0.62f, 0.75f, 0.86f), new Color(0.2f, 0.28f, 0.36f), new Color(0.82f, 0.84f, 0.86f), 0.22f, 0.1f, 500 + i, out var night, 0.45f);
                var m = HanRiverLandmarks.Mat(Color.white, tex, night, new Color(1f, 0.88f, 0.65f), 0.6f, 0.3f, new Vector2(w / 22f, 1f));
                var go = Mats.Prim(PrimitiveType.Cube, "MarinaTower", root, new Vector3(x, water + h * 0.5f, z), new Vector3(w, h, w), m, false);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // 마리나 베이 샌즈: 살짝 벌어진 세 동 + 꼭대기를 잇는 배 모양 스카이파크
            var sandsTex = HanRiverLandmarks.Facade(55, 10, new Color(0.7f, 0.8f, 0.88f), new Color(0.3f, 0.38f, 0.45f), new Color(0.9f, 0.9f, 0.9f), 0.18f, 0.08f, 777, out var sandsNight, 0.5f);
            var sandsMat = HanRiverLandmarks.Mat(Color.white, sandsTex, sandsNight, new Color(1f, 0.9f, 0.7f), 0.7f, 0.3f, new Vector2(1f, 1f));
            const float sandsH = 170f;
            for (int k = -1; k <= 1; k++)
            {
                var t = Mats.Prim(PrimitiveType.Cube, "SandsTower", root, new Vector3(20f + k * 46f, water + sandsH * 0.5f, 560f), new Vector3(24f, sandsH, 34f), sandsMat, false);
                t.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // 두 다리가 위로 갈수록 붙는 모양 흉내: 앞면에 비스듬한 판
                Mats.Prim(PrimitiveType.Cube, "SandsLeg", root, new Vector3(20f + k * 46f, water + sandsH * 0.45f, 543f), new Vector3(22f, sandsH * 0.9f, 2f), sandsMat, false, Quaternion.Euler(-4f, 0f, 0f));
            }
            var park = MapKit.Far(new Color(0.88f, 0.9f, 0.9f), 0.5f, 0.3f);
            Mats.Prim(PrimitiveType.Cube, "SandsSkyPark", root, new Vector3(34f, water + sandsH + 3f, 560f), new Vector3(190f, 6f, 30f), park, false);
            Mats.Prim(PrimitiveType.Cube, "SandsSkyParkBow", root, new Vector3(-72f, water + sandsH + 2.5f, 560f), new Vector3(30f, 4f, 22f), park, false, Quaternion.Euler(0f, 0f, 4f));

            // --- 하늘의 열기구 (해협 위·도시 위) ---
            foreach (var (x, y, z, id) in new[] { (-60f, 60f, 260f, "balloon_a"), (40f, 85f, 330f, "balloon_b"), (130f, 50f, 200f, "balloon_a"), (-180f, 95f, 380f, "balloon_b"), (220f, 75f, 420f, "balloon_a") })
                MapKit.Prop(root, id, new Vector3(x, y, z), b.Rand(0f, 360f), 22f, 0f, false);

            // --- 요트: 폰툰 양옆에 매어 둔 돛배·모터보트, 해협의 배 ---
            foreach (var (x, z, yaw, path) in new[] { (32f, -24f, 0f, "boat-sail-a"), (32f, -8f, 0f, "boat-sail-b"), (44f, -18f, 180f, "boat-speed-a"), (44f, -2f, 180f, "boat-sail-a"), (32f, 8f, 0f, "boat-speed-c") })
                KenneyProps.Spawn("Kenney/Watercraft/" + path, root, new Vector3(x, water - 0.2f, z), yaw, 0f, 10f);
            b.PlaceBoats("Kenney/Watercraft/boat-sail-b", 4, 12f);

            // --- 아치 지붕 건물 (해변 쪽 긴 원통 지붕 홀) ---
            var hallAt = new Vector3(110f, 0f, -68f);
            hallAt.y = b.Height(hallAt.x, hallAt.z);
            var glass = Mats.Lit(new Color(0.55f, 0.7f, 0.78f), 0.85f, 0.2f);
            Mats.Prim(PrimitiveType.Cube, "HallBase", root, hallAt + new Vector3(0f, 1.5f, 0f), new Vector3(36f, 3f, 16f), Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.2f), true);
            Mats.Prim(PrimitiveType.Cylinder, "HallVault", root, hallAt + new Vector3(0f, 3f, 0f), new Vector3(16f, 18f, 12f), glass, false, Quaternion.Euler(0f, 0f, 90f));

            // --- 팔라완 출렁다리 (사진): 해변에서 작은 섬까지 밧줄 난간 출렁다리, 양끝 나무 전망 탑 ---
            {
                var from = new Vector3(-150f, 0f, b.Map.ShoreAt(-150f) - 2f);
                var to = new Vector3(-150f, 0f, 2f - 9f);
                from.y = b.Height(from.x, from.z) + 0.2f;
                to.y = b.Height(to.x, to.z) + 0.2f;
                var plank = Mats.Wood(new Color(0.52f, 0.38f, 0.26f), 0.25f);
                var rope = Mats.Lit(new Color(0.7f, 0.62f, 0.48f), 0.2f);
                int n = 28;
                for (int i = 0; i <= n; i++)
                {
                    float f = i / (float)n;
                    var p = Vector3.Lerp(from, to, f) + Vector3.down * Mathf.Sin(f * Mathf.PI) * 1.4f;
                    Mats.Prim(PrimitiveType.Cube, "BridgePlank", root, p, new Vector3(1.8f, 0.08f, 0.35f), plank, false);
                    foreach (float sx in new[] { -0.95f, 0.95f })
                    {
                        Mats.Prim(PrimitiveType.Cylinder, "BridgeHanger", root, p + new Vector3(sx, 0.55f, 0f), new Vector3(0.03f, 0.55f, 0.03f), rope, false);
                        if (i < n)
                        {
                            var q = Vector3.Lerp(from, to, (i + 1f) / n) + Vector3.down * Mathf.Sin((i + 1f) / n * Mathf.PI) * 1.4f;
                            MapKit.Beam(root, p + new Vector3(sx, 1.1f, 0f), q + new Vector3(sx, 1.1f, 0f), 0.05f, rope, false, "BridgeRope");
                            MapKit.Beam(root, p + new Vector3(sx, 0.05f, 0f), q + new Vector3(sx, 0.05f, 0f), 0.06f, rope, false, "BridgeRope");
                        }
                    }
                }
                // 양끝 나무 전망 탑 (네 기둥 + 두 층 데크 + 지붕)
                foreach (var end in new[] { from + Vector3.back * 1.5f, to + Vector3.forward * 1.5f })
                {
                    foreach (var (dx, dz) in new[] { (-1.4f, -1.4f), (1.4f, -1.4f), (-1.4f, 1.4f), (1.4f, 1.4f) })
                        Mats.Prim(PrimitiveType.Cube, "TowerPost", root, end + new Vector3(dx, 3.5f, dz), new Vector3(0.3f, 7f, 0.3f), plank, false);
                    Mats.Prim(PrimitiveType.Cube, "TowerDeck", root, end + new Vector3(0f, 3.5f, 0f), new Vector3(3.4f, 0.2f, 3.4f), plank, false);
                    Mats.Prim(PrimitiveType.Cube, "TowerDeck2", root, end + new Vector3(0f, 6.6f, 0f), new Vector3(3.4f, 0.2f, 3.4f), plank, false);
                    MapKit.Place(root, "TowerRoof", CoastWorldBuilder.Gable(), Mats.Thatch(new Color(0.55f, 0.45f, 0.3f)), end + new Vector3(0f, 7f, 0f)).transform.localScale = new Vector3(4.2f, 1.8f, 4.2f);
                }
            }

            // 돌 포장 산책로 횃불 (정글 길)
            var pole = Mats.Wood(new Color(0.28f, 0.2f, 0.14f), 0.2f);
            var flame = Mats.Emissive(new Color(1f, 0.55f, 0.15f), 2.5f);
            for (float x = -150f; x < -100f; x += 10f)
            {
                float z = b.Map.ShoreAt(x) - 16f, y = b.Height(x, z);
                Mats.Prim(PrimitiveType.Cylinder, "TorchPole", root, new Vector3(x, y + 1.1f, z), new Vector3(0.12f, 1.1f, 0.12f), pole, false);
                var f = Mats.Prim(PrimitiveType.Sphere, "TorchFlame", root, new Vector3(x, y + 2.35f, z), new Vector3(0.3f, 0.45f, 0.3f), flame, false);
                b.AddBulb(f.GetComponent<Renderer>());
                b.AddLamp(new Vector3(x, y + 2.6f, z), new Color(1f, 0.6f, 0.3f), 9f, 1.2f);
            }
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>센토사 이야기 (Lv.35~). 첫 퀘스트는 그 낚시터 연락관(탄 지부장)이 준다.</summary>
        static QuestDef[] SentosaChapter() => new[]
        {
            new QuestDef { Id = "sentosa_1", Title = "종착역의 붉은 손님", Desc = "웰컴 투 센토사! 동쪽 바위엔 러셀 스내퍼가 떼로 살아요. 새우나 크릴로 다섯 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.35부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "russellsnapper", Count = 5, RewardGold = 60000, RewardExp = 7000 },
            new QuestDef { Id = "sentosa_2", Title = "네 가닥 수염", Desc = "가슴지느러미 아래 실 같은 수염 네 가닥, 포핑거 스레드핀은 저녁·밤 얕은 물에 들어온다네. 두 마리!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "threadfin", Count = 2, RewardGold = 70000, RewardExp = 8000 },
            new QuestDef { Id = "sentosa_3", Title = "머라이언 앞의 GT", Desc = "새벽 D5, 저녁 G4, 머라이언이 바라보는 물살에 GT가 들어온다오. 1.2m가 넘는 놈을 낚아 동남아 여행의 끝을 장식하시게!",
                Goal = QuestGoal.CatchMinLength, SpeciesId = "gianttrevally", MinLen = 120f, Count = 1, RewardGold = 140000, RewardExp = 13000, RewardKeepNet = 3 },
        };
    }
}
