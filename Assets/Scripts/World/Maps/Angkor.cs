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
        /// 앙코르와트 (Lv.26): 서쪽 둑길 끝 해자 선착장. 폭 190m 해자(민물) 건너 이끼 낀 바깥 회랑과 정문 탑, 그 너머 3단 기단 위 연꽃 봉오리 탑 다섯.
        /// 물가엔 연잎·연꽃, 계단 양옆 사자상, 횃불. 해자에 톤레삽에서 올라온 메콩 대형 어종이 산다 (원작 1차 영상 0:08~0:24).
        /// </summary>
        public static CoastSpec Angkor() => new CoastSpec
        {
            Id = "angkor", MapName = "앙코르와트 · 캄보디아", ShortName = "앙코르와트", ShopName = "시엠립 낚시점", UnionContact = "해자 둑길의 속 찬나 지부장",
            LandName = "서쪽 둑길", SeaName = "앙코르 해자", Seed = 50, River = true, FarShoreZ = 140f, StoneQuay = true,
            WaterDeep = new Color(0.05f, 0.17f, 0.12f), WaterShallow = new Color(0.2f, 0.36f, 0.22f),
            Shores = new[] { new ShoreSeg(-999f, -45f, ShoreKind.Beach), new ShoreSeg(-45f, 45f, ShoreKind.Quay), new ShoreSeg(45f, 999f, ShoreKind.Beach) },
            NearDepth = 3f, DepthSlope = 0.03f, FlatWidth = 36f, CliffHeight = 4f, CliffWidth = 60f, RoadOffset = 18f, VillageDensity = 0.45f,
            Houses = HouseStyle.Stilt, TreeKind = TreeStyle.Tropical,
            Platforms = new[] { new PlatformSpec { Area = new Rect(-3f, -41f, 6f, 16f), Kind = PlatformKind.Pier, Name = "해자 선착장" } },
            Grid = new[,]
            {
                { W, W, S, S, S, S, W, W },
                { W, S, D, D, D, D, S, W },
                { S, D, D, D, D, D, D, S },
                { D, D, D, D, D, D, D, D },
                { S, D, D, D, D, D, D, S },
                { W, W, S, S, S, S, W, W },
                { W, W, W, W, W, W, W, W },
                { W, W, W, W, W, W, W, W },
            },
            Spawn = new Vector3(8f, 1.1f, -47f), ShopPos = new Vector3(-22f, 1f, -54f),
            ShopRoof = new Color(0.62f, 0.5f, 0.32f),
            ShopOwner = Local("시엠립 낚시점 소피읍", "시엠립 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("속 찬나 지부장", "한국 낚시 조합 앙코르 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(-1.5f, -29f, 0f, Local("크로마 두른 아주머니", "해자 어부", true), NpcAngler.Role.Angler, y: 1f),
                Npc(-12f, -46f, 160f, Local("사원 스님", "새벽 공양을 마치고", false), NpcAngler.Role.Talker),
                Npc(62f, -43f, 10f, Local("사진 찍는 여행객", "일출 명당을 찾는 중", true), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("ANGKOR WAT", new Vector3(30f, 6.5f, -62f), SignGold) },
            // 건너편은 해자 안쪽 잔디밭 (회랑 앞에 나무·집을 두지 않는다, 양 끝만 숲), 중앙 사원 터도 비운다
            Reserved = new[] { new Rect(-120f, 135f, 240f, 70f), new Rect(-200f, 158f, 400f, 30f), new Rect(-90f, 245f, 180f, 120f) },
            Schools = () => new List<FishSchool>
            {
                School("실버 바브 떼", 2, 0, 2, DayLong, "silverbarb", "hampalabarb", "moonlightgourami"),
                School("가물치 연잎 밭", 0, 0, 1, MornDay, "giantsnakehead", "stripedsnakehead"),
                School("줄무늬 메기 떼", 4, 2, 4, DayLong, "stripedcatfish", "wallago"),
                School("나이프피시 그늘", 6, 1, 3, EveNight, "clownknifefish", "redtailcatfish"),
                School("가오리 모래톱", 3, 3, 4, EveNight, "giantstingray", "giantpangasius"),
                // 아로와나: 새벽 B2, 저녁 G1 (연잎 그늘)
                Special("아로와나 어군", "arowana", (TimeBand.Dawn, 1, 1), (TimeBand.Evening, 6, 0)),
            },
            Extra = AngkorExtra,
        };

        static readonly Color AngkorStone = new Color(0.66f, 0.64f, 0.56f);

        /// <summary>
        /// 크메르 프랑(연꽃 봉오리 탑) 단면: 기단 → 몸통 → 안으로 들어가며 다섯 층 꽃잎 띠 → 봉오리 끝. 높이 h, 몸통 반지름 r.
        /// 톱니 단면(notch)으로 돌리면 모서리가 층층이 꺾인 탑이 된다.
        /// </summary>
        /// <summary>
        /// 프랑(탑) 단면: 기단 → 곧은 몸 → 위로 좁아지는 층 tiers개 → 연꽃봉오리 끝. 층마다 튀어나온 처마 턱 + 들어간 띠(조각 벽감 자리) + 작은 연꽃잎 턱.
        /// 사진(왓 아룬 가운데 프랑, 앙코르와트 중앙 탑): 층이 촘촘해서 멀리서 가로 줄이 겹겹이 보인다.
        /// </summary>
        static List<Vector2> PrangProfile(float h, float r, int tiers = 9)
        {
            var p = new List<Vector2> { new Vector2(r * 1.25f, 0f), new Vector2(r * 1.25f, h * 0.035f), new Vector2(r * 1.1f, h * 0.045f), new Vector2(r * 1.1f, h * 0.06f),
                new Vector2(r, h * 0.07f), new Vector2(r, h * 0.27f), new Vector2(r * 1.07f, h * 0.29f), new Vector2(r * 1.07f, h * 0.31f) };
            float y = h * 0.31f, top = h * 0.88f;
            float step = (top - y) / tiers;
            for (int i = 0; i < tiers; i++)
            {
                float f = i / (float)tiers;
                float ri = r * Mathf.Lerp(0.95f, 0.28f, Mathf.Pow(f, 1.15f));
                p.Add(new Vector2(ri * 0.94f, y + step * 0.05f));       // 들어간 띠
                p.Add(new Vector2(ri * 0.94f, y + step * 0.55f));
                p.Add(new Vector2(ri * 1.04f, y + step * 0.62f));       // 연꽃잎 턱
                p.Add(new Vector2(ri * 1.1f, y + step * 0.82f));        // 처마
                p.Add(new Vector2(ri * 0.98f, y + step));
                y += step;
            }
            p.Add(new Vector2(r * 0.2f, h * 0.9f));
            p.Add(new Vector2(r * 0.15f, h * 0.93f));
            p.Add(new Vector2(r * 0.17f, h * 0.945f));
            p.Add(new Vector2(r * 0.08f, h * 0.975f));
            p.Add(new Vector2(0.01f, h));
            return p;
        }

        static void Prang(Transform root, Vector3 at, float h, float r, Material stone) =>
            MapKit.Place(root, "Prang", MapKit.Lathe(PrangProfile(h, r, 8), 24, 4f, 0.86f), stone, at, 45f);

        /// <summary>앙코르와트만의 풍경: 건너편 바깥 회랑·정문 탑·둑길·중앙 사원 탑 다섯, 물가 연잎·연꽃, 사자상, 횃불.</summary>
        static void AngkorExtra(CoastWorldBuilder b)
        {
            var root = new GameObject("Angkor").transform;
            root.SetParent(b.Root, false);
            var stone = Mats.Karst(AngkorStone, 0.55f);
            var stoneDark = Mats.Karst(new Color(0.5f, 0.5f, 0.44f), 0.75f);
            var roofStone = Mats.Karst(new Color(0.55f, 0.54f, 0.48f), 0.85f);
            float land = b.Map.LandY;

            // --- 건너편: 바깥 회랑 (낮은 돌벽 + 돌 박공지붕), 가운데 정문 (탑 셋) ---
            const float galleryZ = 192f;
            foreach (float sx in new[] { -1f, 1f })
            {
                float x0 = sx * 22f, x1 = sx * 200f, cx = (x0 + x1) * 0.5f, len = Mathf.Abs(x1 - x0);
                Mats.Prim(PrimitiveType.Cube, "Gallery", root, new Vector3(cx, land + 3f, galleryZ), new Vector3(len, 6.4f, 7f), stone, true);
                Mats.Prim(PrimitiveType.Cube, "GalleryPlinth", root, new Vector3(cx, land + 0.5f, galleryZ - 0.6f), new Vector3(len, 1.4f, 8.6f), stoneDark, false);
                MapKit.Place(root, "GalleryRoof", CoastWorldBuilder.Gable(), roofStone, new Vector3(cx, land + 6.2f, galleryZ), 0f).transform.localScale = new Vector3(len, 3.2f, 8.4f);
                // 회랑 앞 기둥 줄 (그늘진 벽 앞에 선 사각 기둥)
                for (float x = x0 + sx * 6f; Mathf.Abs(x) < 196f; x += sx * 9f)
                    Mats.Prim(PrimitiveType.Cube, "Pillar", root, new Vector3(x, land + 2.6f, galleryZ - 4f), new Vector3(0.9f, 5.2f, 0.9f), stone, false);
                Mats.Prim(PrimitiveType.Cube, "Lintel", root, new Vector3(cx, land + 5.4f, galleryZ - 4f), new Vector3(len, 0.6f, 1.1f), stone, false);
            }
            Mats.Prim(PrimitiveType.Cube, "Gopura", root, new Vector3(0f, land + 3.5f, galleryZ), new Vector3(44f, 7f, 11f), stone, true);
            Mats.Prim(PrimitiveType.Cube, "GopuraDoor", root, new Vector3(0f, land + 2.2f, galleryZ - 5.55f), new Vector3(3.2f, 4.4f, 0.1f), Mats.Lit(new Color(0.08f, 0.08f, 0.07f)), false);
            Prang(root, new Vector3(0f, land + 7f, galleryZ), 17f, 4.2f, stone);
            Prang(root, new Vector3(-15f, land + 7f, galleryZ), 11f, 3f, stone);
            Prang(root, new Vector3(15f, land + 7f, galleryZ), 11f, 3f, stone);

            // 건너편 둑길: 물가에서 정문까지 돌길 + 나가 난간 (낮은 돌 띠)
            float bankZ = b.Map.FarShoreAt(0f);
            float pathLen = galleryZ - 6f - bankZ;
            Mats.Prim(PrimitiveType.Cube, "Causeway", root, new Vector3(0f, land + 0.2f, bankZ + pathLen * 0.5f), new Vector3(9f, 0.8f, pathLen + 4f), stoneDark, false);
            foreach (float sx in new[] { -4.6f, 4.6f })
                Mats.Prim(PrimitiveType.Cube, "NagaRail", root, new Vector3(sx, land + 1f, bankZ + pathLen * 0.5f), new Vector3(0.6f, 0.7f, pathLen), stone, false);
            foreach (float sx in new[] { -6f, 6f })
                MapKit.Prop(root, "singha", new Vector3(sx, land + 0.4f, bankZ + 2f), 180f, 3.2f);

            // --- 중앙 사원: 3단 기단 + 네 귀퉁이 탑과 가운데 높은 탑 (실제보다 가까이·낮게) ---
            const float templeZ = 290f;
            float y = land;
            foreach (var (w, d, hh) in new[] { (130f, 96f, 4f), (96f, 72f, 6f), (62f, 48f, 7f) })
            {
                Mats.Prim(PrimitiveType.Cube, "Terrace", root, new Vector3(0f, y + hh * 0.5f, templeZ), new Vector3(w, hh, d), stone, false);
                MapKit.Place(root, "TerraceRoof", CoastWorldBuilder.Gable(), roofStone, new Vector3(0f, y + hh, templeZ - d * 0.5f), 0f).transform.localScale = new Vector3(w, 1.6f, 3f);
                // 앞 계단
                Mats.Prim(PrimitiveType.Cube, "Stair", root, new Vector3(0f, y + hh * 0.5f, templeZ - d * 0.5f - 2f), new Vector3(8f, hh, 4f), stoneDark, false, Quaternion.Euler(-20f, 0f, 0f));
                y += hh;
            }
            foreach (var (dx, dz) in new[] { (-22f, -16f), (22f, -16f), (-22f, 16f), (22f, 16f) })
                Prang(root, new Vector3(dx, y, templeZ + dz), 36f, 6.5f, stone);
            Prang(root, new Vector3(0f, y, templeZ), 56f, 9.5f, stone);

            // 사원 뒤 정글 (중앙 사원 양옆)
            for (int i = 0; i < 26; i++)
            {
                float x = b.Rand(-200f, 200f), z = b.Rand(205f, 268f);
                if (Mathf.Abs(x) < 80f && z > 250f) continue;
                if (Mathf.Abs(x) < 26f) continue;
                b.PlaceTree(new Vector3(x, b.Height(x, z) - 0.05f, z));
            }

            // --- 이쪽 물가: 계단 양옆 사자상, 연잎·연꽃, 횃불 ---
            foreach (float sx in new[] { -4.6f, 4.6f })
                MapKit.Prop(root, "singha", new Vector3(sx, land, -43.5f), 180f, 2.4f); // 둑길로 오는 사람을 본다

            float water = b.Map.BaseWaterY;
            var pads = new List<Vector4>();
            var flowers = new List<Vector4>();
            // 연꽃 군락: 물가 가까이 덩어리로 모여 핀다 (이쪽 물가 22곳, 건너편 10곳)
            for (int g = 0; g < 32; g++)
            {
                bool far = g >= 22;
                float gx = g < 6 ? (g % 2 == 0 ? -1f : 1f) * (16f + g * 5f) : b.Rand(-190f, 190f); // 앞 여섯 군락은 선착장 양옆
                if (!far && Mathf.Abs(gx) < 14f) gx += 30f;
                float gz = far ? b.Map.FarShoreAt(gx) - b.Rand(4f, 16f) : b.Map.ShoreAt(gx) + b.Rand(4f, 20f);
                int n = (int)b.Rand(18f, 34f);
                for (int i = 0; i < n; i++)
                {
                    float a = b.Rand(0f, Mathf.PI * 2f), d = b.Rand(0f, 1f) * b.Rand(2f, 9f);
                    float x = gx + Mathf.Cos(a) * d * 1.6f, z = gz + Mathf.Sin(a) * d;
                    if (Mathf.Abs(x) < 8f && !far) continue;
                    if (b.Height(x, z) > water - 0.4f) continue;
                    float r = b.Rand(0.5f, 1.05f);
                    // 물결(±0.1m)에 묻히지 않게 수면보다 조금 높이 띄운다
                    pads.Add(new Vector4(x, z, r, water + 0.12f + b.Rand(0f, 0.03f)));
                    if (b.Rand(0f, 1f) < 0.16f) flowers.Add(new Vector4(x + r * 0.3f, z, r * 0.4f, water + 0.5f));
                }
            }
            MapKit.Place(root, "LotusPads", MapKit.Discs(pads, 12, true), Mats.Lit(new Color(0.32f, 0.55f, 0.2f), 0.3f), Vector3.zero);
            MapKit.Place(root, "LotusFlowers", MapKit.Discs(flowers, 8), Mats.Lit(new Color(0.95f, 0.55f, 0.7f), 0.2f), Vector3.zero);
            // 꽃대 (연꽃 아래 가는 줄기)
            var stems = new List<Vector3[]>();
            foreach (var f in flowers) stems.Add(new[] { new Vector3(f.x, water - 0.2f, f.y), new Vector3(f.x, f.w, f.y) });
            MapKit.Place(root, "LotusStems", MapKit.Tubes(stems, u => 0.025f, 5), Mats.Lit(new Color(0.3f, 0.45f, 0.2f)), Vector3.zero);

            var pole = Mats.Wood(new Color(0.3f, 0.22f, 0.15f), 0.2f);
            var flame = Mats.Emissive(new Color(1f, 0.55f, 0.15f), 2.5f);
            foreach (float x in new[] { -38f, -20f, -7f, 7f, 20f, 38f })
            {
                Mats.Prim(PrimitiveType.Cylinder, "TorchPole", root, new Vector3(x, land + 1.1f, -42.5f), new Vector3(0.12f, 1.1f, 0.12f), pole, false);
                var f = Mats.Prim(PrimitiveType.Sphere, "TorchFlame", root, new Vector3(x, land + 2.35f, -42.5f), new Vector3(0.3f, 0.45f, 0.3f), flame, false);
                b.AddBulb(f.GetComponent<Renderer>());
                b.AddLamp(new Vector3(x, land + 2.6f, -42.5f), new Color(1f, 0.6f, 0.3f), 9f, 1.4f);
            }
        }
    }
}

namespace Fishing.Data
{
    public static partial class QuestDatabase
    {
        /// <summary>앙코르와트 이야기 (Lv.26~). 첫 퀘스트는 그 낚시터 연락관(속 찬나 지부장)이 준다.</summary>
        static QuestDef[] AngkorChapter() => new[]
        {
            new QuestDef { Id = "angkor_1", Title = "천 년 해자의 은빛 손님", Desc = "줌 리읍 쑤어! 해자 연잎 사이엔 실버 바브가 떼로 다닌답니다. 떡밥으로 세 마리만 낚아 보세요. (낚시터 목록 [동남아] 탭, Lv.26부터)",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "silverbarb", Count = 3, RewardGold = 35000, RewardExp = 4500 },
            new QuestDef { Id = "angkor_2", Title = "해자의 칼날", Desc = "등이 혹처럼 솟고 배가 칼날 같은 클라운 나이프피시는 해 질 녘에 나온다네. 두 마리를 낚아 스님께 보여 드리게.",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "clownknifefish", Count = 2, RewardGold = 45000, RewardExp = 5500 },
            new QuestDef { Id = "angkor_3", Title = "메콩의 그림자", Desc = "우기에 톤레삽에서 올라온 거대 담수 가오리가 해자 바닥 모래에 붙어 있다오. 저녁·밤·새벽에 새우나 잘린 미끼로. 꼬리 가시를 조심하시오!",
                Goal = QuestGoal.CatchSpecies, SpeciesId = "giantstingray", Count = 1, RewardGold = 90000, RewardExp = 9000 },
        };
    }
}
