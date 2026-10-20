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
        /// 플라우 티오만 (Lv.37): 한적하고 평화로운 리조트. 언덕에 활처럼 휜 대형 리조트 호텔(층층 발코니), 보라 파라솔이 줄 선 백사장,
        /// 해바라기 화단과 가로등 광장, 에메랄드 바다의 작은 섬, 뒤로 정글에 솟은 쌍둥이 봉우리 '용의 뿔' (원작 3차 영상 0:38~0:46).
        /// </summary>
        public static CoastSpec Tioman() => new CoastSpec
        {
            Id = "tioman", MapName = "플라우 티오만 · 남중국해", ShortName = "티오만", ShopName = "티오만 리조트 낚시점", UnionContact = "리조트 부두의 파리다 지부장",
            LandName = "리조트 해변", SeaName = "티오만 앞바다", Seed = 59,
            WaterDeep = new Color(0.02f, 0.24f, 0.3f), WaterShallow = new Color(0.08f, 0.62f, 0.52f),
            Shores = new[] { new ShoreSeg(-999f, -30f, ShoreKind.Beach), new ShoreSeg(-30f, 30f, ShoreKind.Quay), new ShoreSeg(30f, 999f, ShoreKind.Beach) },
            NearDepth = 3f, DepthSlope = 0.06f, FlatWidth = 42f, CliffHeight = 18f, CliffWidth = 60f, RoadOffset = 20f, VillageDensity = 0.4f,
            Houses = HouseStyle.Resort, TreeKind = TreeStyle.Tropical, Features = CoastFeature.Mountains,
            Platforms = new[] { new PlatformSpec { Area = new Rect(16f, -41f, 5f, 44f), Kind = PlatformKind.Pier, Name = "리조트 부두" } },
            Islets = new[] { new IsletSpec { Center = new Vector3(-96f, 0f, 92f), Radius = 15f, Height = 9f, Walkable = true, Trees = true, Name = "렝기스 섬" } },
            Grid = new[,]
            {
                { S, S, S, S, S, S, R, R },
                { R, S, D, D, D, D, R, R },
                { R, D, D, D, D, D, D, R },
                { D, D, D, D, D, D, D, R },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(6f, 1.1f, -47f), ShopPos = new Vector3(-22f, 1f, -55f),
            ShopRoof = new Color(0.55f, 0.3f, 0.65f),
            ShopOwner = Local("리조트 낚시점 누르", "티오만 리조트 낚시점", new Color(0.6f, 0.35f, 0.7f), true, false, 2),
            Npcs = new[]
            {
                Npc(26f, -47f, 200f, Union("파리다 지부장", "한국 낚시 조합 티오만 지부", true, false), NpcAngler.Role.Talker, "union"),
                Npc(19f, -2f, 0f, Local("다이빙 강사", "산호초 지킴이", new Color(0.2f, 0.65f, 0.75f), false, false, -1), NpcAngler.Role.Angler, y: 1f),
                Npc(-44f, -50f, 120f, Local("리조트 지배인", "보라 파라솔이 자랑", new Color(0.95f, 0.95f, 0.92f), false, false, -1), NpcAngler.Role.Talker),
                Npc(80f, -44f, 0f, Local("허니문 부부", "해먹에서 일몰을", new Color(0.95f, 0.75f, 0.85f), true, false, 2), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("PULAU TIOMAN", new Vector3(0f, 6.5f, -64f), SignGold) },
            FarIslands = new[] { new Vector4(-300f, 450f, 120f, 40f), new Vector4(280f, 480f, 90f, 30f) },
            Reserved = new[] { new Rect(-90f, -110f, 180f, 46f), new Rect(-60f, -64f, 30f, 16f) },
            Schools = () => new List<FishSchool>
            {
                School("범프헤드 산호초", 1, 0, 1, MornDay, "bumpheadparrot", "coraltrout"),
                School("블루핀 트레발리", 3, 1, 3, DayLong, "bluefintrevally", "gianttrevally"),
                School("산호 바위", 6, 1, 3, DayLong, "coraltrout", "goldensnapper", "spangledemperor"),
                School("삼치 떼", 4, 3, 5, DayLong, "narrowbarredmackerel", "longtailtuna", "mahimahi"),
                School("코비아 그늘", 2, 4, 6, DayLong, "cobia", "barracuda"),
                School("대왕바리 굴", 7, 2, 3, EveNight, "daewangbari"),
                // 나폴레옹피시: 아침 F3, 낮 A4
                Special("나폴레옹피시", "napoleonwrasse", (TimeBand.Morning, 5, 2), (TimeBand.Day, 0, 3)),
            },
            Extra = TiomanExtra,
        };

        /// <summary>티오만만의 풍경: 활처럼 휜 리조트 호텔, 보라 파라솔 줄, 해바라기 화단, 용의 뿔 쌍둥이 봉우리.</summary>
        static void TiomanExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Tioman").transform;
            root.SetParent(b.Root, false);
            float land = b.Map.LandY;

            // --- 활처럼 휜 리조트 호텔: 바다를 안듯이 오목한 아홉 동, 층층 발코니 ---
            var facade = HanRiverLandmarks.Facade(9, 5, new Color(0.55f, 0.66f, 0.72f), new Color(0.3f, 0.36f, 0.4f), new Color(0.97f, 0.96f, 0.93f), 0.38f, 0.14f, 590, out var night, 0.55f);
            var hotelMat = HanRiverLandmarks.Mat(Color.white, facade, night, new Color(1f, 0.85f, 0.6f), 0.35f, 0f, Vector2.one);
            var side = Mats.Wall(new Color(0.96f, 0.95f, 0.92f), 0.2f);
            var roof = Mats.Tile(new Color(0.75f, 0.35f, 0.22f), 0.25f);
            var slab = Mats.Wall(new Color(0.98f, 0.97f, 0.95f), 0.2f);
            var rail = Mats.Transparent(new Color(0.6f, 0.75f, 0.8f, 0.45f), 0.9f);
            var railTop = Mats.Lit(new Color(0.85f, 0.86f, 0.88f), 0.6f, 0.6f);
            var center = new Vector3(0f, 0f, -12f);
            const float radius = 78f, blockW = 16f, blockD = 12f, floorH = 3.1f;
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(-44f, 44f, i / 8f) * Mathf.Deg2Rad;
                var p = center + new Vector3(Mathf.Sin(a) * radius, 0f, -Mathf.Cos(a) * radius);
                int floors = 9 - Mathf.Abs(i - 4) / 2; // 가운데가 높다
                float h = floors * floorH;
                float gy = Mathf.Min(b.Height(p.x - 6f, p.z), b.Height(p.x + 6f, p.z), b.Height(p.x, p.z + 5f));
                var rot = Quaternion.Euler(0f, MapKit.Face(p, center), 0f);
                var block = new GameObject("HotelBlock").transform;
                block.SetParent(root, false);
                block.localPosition = new Vector3(p.x, gy, p.z);
                block.localRotation = rot;
                // 앞면(바다 쪽) = 발코니 정면, 나머지 = 흰 벽
                Mats.Prim(PrimitiveType.Cube, "HotelWall", block, new Vector3(0f, h * 0.5f - 2f, 0f), new Vector3(blockW, h + 4f, blockD), side, true);
                var front = Mats.Prim(PrimitiveType.Quad, "HotelFront", block, new Vector3(0f, h * 0.5f, blockD * 0.5f + 0.05f), new Vector3(blockW, h, 1f), hotelMat, false, Quaternion.Euler(0f, 180f, 0f));
                front.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Mats.Prim(PrimitiveType.Cube, "HotelRoof", block, new Vector3(0f, h + 0.5f, 0f), new Vector3(blockW + 1f, 1f, blockD + 1f), roof, false);
                // 사진(리조트 호텔 정면): 층마다 튀어나온 발코니 판 + 유리 난간, 칸마다 칸막이 벽 → 정면에 깊이가 생긴다
                for (int f = 1; f < floors; f++)
                {
                    float y = f * floorH;
                    Mats.Prim(PrimitiveType.Cube, "Balcony", block, new Vector3(0f, y - 0.08f, blockD * 0.5f + 0.75f), new Vector3(blockW, 0.18f, 1.5f), slab, false);
                    Mats.Prim(PrimitiveType.Cube, "BalconyGlass", block, new Vector3(0f, y + 0.5f, blockD * 0.5f + 1.48f), new Vector3(blockW, 1f, 0.03f), rail, false);
                    Mats.Prim(PrimitiveType.Cube, "BalconyRail", block, new Vector3(0f, y + 1.02f, blockD * 0.5f + 1.48f), new Vector3(blockW, 0.05f, 0.06f), railTop, false);
                }
                for (int k = 0; k <= 5; k++)
                    Mats.Prim(PrimitiveType.Cube, "BayWall", block, new Vector3(-blockW * 0.5f + k * blockW / 5f, h * 0.5f, blockD * 0.5f + 0.75f), new Vector3(0.22f, h, 1.5f), slab, false);
            }

            // --- 보라 파라솔 줄 + 선베드 ---
            for (float x = -160f; x < 160f; x += 11f)
            {
                if (x > -40f && x < 40f) continue;
                float z = b.Map.ShoreAt(x) - 10f, y = b.Height(x, z);
                if (y < land - 0.3f) continue;
                MapKit.Prop(root, "parasol_purple", new Vector3(x, y - 0.05f, z), 0f, 2.8f);
                MapKit.Prop(root, "sunbed", new Vector3(x + 1.1f, b.Height(x + 1.1f, z + 1.5f), z + 1.5f), 180f, 0f, 2f);
            }

            // --- 광장 해바라기 화단 (낚시점 옆): 줄기 + 노란 꽃잎 + 갈색 가운데 ---
            var stems = new List<Vector3[]>();
            var petals = new List<Vector4>();
            var hearts = new List<Vector4>();
            var bed = new Vector3(-45f, land, -56f);
            Mats.Prim(PrimitiveType.Cube, "FlowerBed", root, bed + new Vector3(0f, 0.25f, 0f), new Vector3(22f, 0.5f, 6f), Mats.Textured("mud", new Color(0.55f, 0.4f, 0.3f), 2f, 0.1f), true);
            var rng = new System.Random(59);
            for (int k = 0; k < 90; k++)
            {
                float x = bed.x + ((float)rng.NextDouble() - 0.5f) * 20f, z = bed.z + ((float)rng.NextDouble() - 0.5f) * 5f;
                float top = land + 0.5f + 0.9f + (float)rng.NextDouble() * 0.6f;
                stems.Add(new[] { new Vector3(x, land + 0.45f, z), new Vector3(x, top, z) });
                petals.Add(new Vector4(x, z, 0.26f, top));
                hearts.Add(new Vector4(x, z, 0.11f, top + 0.02f));
            }
            MapKit.Place(root, "SunflowerStems", MapKit.Tubes(stems, u => 0.025f, 5), Mats.Lit(new Color(0.3f, 0.5f, 0.2f)), Vector3.zero);
            MapKit.Place(root, "SunflowerPetals", MapKit.Discs(petals, 12), Mats.Lit(new Color(0.98f, 0.78f, 0.12f), 0.2f), Vector3.zero);
            MapKit.Place(root, "SunflowerHearts", MapKit.Discs(hearts, 8), Mats.Lit(new Color(0.35f, 0.22f, 0.1f), 0.2f), Vector3.zero);

            // --- 용의 뿔: 정글 위로 솟은 가는 쌍둥이 봉우리 (멀리 뒤) ---
            b.KarstPeak(new Vector3(140f, 0f, -270f), 13f, 175f);
            b.KarstPeak(new Vector3(172f, 0f, -258f), 10f, 150f);
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>플라우 티오만 이야기 (Lv.37~). 첫 퀘스트는 그 낚시터 연락관(파리다 지부장)이 준다.</summary>
        static QuestDef[] TiomanChapter() => new[]
        {
            new QuestDef { Id = "tioman_1", Title = "파란 지느러미 사냥꾼", Desc = "슬라맛 다탕! 산호초 가장자리엔 지느러미가 파란 블루핀 트레발리가 사냥을 해요. 루어로 세 마리 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.37부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "bluefintrevally", Count = 3, RewardGold = 65000, RewardExp = 7500 },
            new QuestDef { Id = "tioman_2", Title = "산호를 씹는 혹 머리", Desc = "범프헤드 패럿피시는 이마 혹으로 산호를 부숴 먹는다네. 아침·낮 B1 산호초에서 크릴로 한 마리. 다이빙 강사가 꼭 보고 싶어 하더군.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "bumpheadparrot", Count = 1, RewardGold = 90000, RewardExp = 9500 },
            new QuestDef { Id = "tioman_3", Title = "동쪽 바다의 돛", Desc = "배를 타고 동쪽 먼바다로 나가면 돛새치 떼가 몰려온다오. 아침이나 낮에 루어를 끌어 보시게.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "sailfish", Count = 1, RewardGold = 140000, RewardExp = 13000 },
        };
    }
}
