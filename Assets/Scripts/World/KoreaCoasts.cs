using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 원작 "낚시터 목록(KOREA)"의 나머지 국내 낚시터 8곳. 모두 <see cref="CoastSpec"/> 사양 하나로 정의한다.
    /// 실제 바닷가를 참고한 풍경: 영흥도(목섬 모래톱·영흥대교·해무), 태안(만리포·신두리 사구·할미·할아비바위),
    /// 삼척(삼척항·추암 촛대바위·오징어배 집어등), 동해 등대섬(큰 등대·섬), 헌화로(절벽 아래 해안도로·바다부채길·진달래),
    /// 욕지도(가두리 낚시터·출렁다리·삼여), 청진포구(눈 덮인 큰 항구·명태 덕장·얼음 조각), 거문도(거문도 등대·백도).
    /// 좌표: 해안선 z ≈ -40, 바다 +Z, 격자 A~H(x -160~160) × 1~8(z -40~200).
    /// </summary>
    public static partial class KoreaCoasts
    {
        const float NaN = float.NaN;
        static Habitat S => Habitat.Shallow;
        static Habitat D => Habitat.Deep;
        static Habitat R => Habitat.Rocky;
        static Habitat W => Habitat.Weed;

        static readonly TimeBand[] DawnEve = { TimeBand.Dawn, TimeBand.Evening };
        static readonly TimeBand[] MornDay = { TimeBand.Morning, TimeBand.Day };
        static readonly TimeBand[] EveNight = { TimeBand.Evening, TimeBand.Night };
        static readonly TimeBand[] MornEve = { TimeBand.Morning, TimeBand.Evening };

        static FishSchool School(string name, int col, int row, int maxRow, TimeBand[] times, params string[] ids) =>
            new FishSchool { Name = name, SpeciesIds = ids, PreferredTimes = times, Col = col, Row = row, MinRow = Mathf.Max(0, row - 1), MaxRow = maxRow };

        static FishSchool Special(string name, string id, params (TimeBand, int, int)[] schedule) =>
            new FishSchool { Name = name, SpeciesIds = new[] { id }, Special = true, Schedule = schedule };

        static CharacterLook Union(string name, string title, bool female) => new CharacterLook { Name = name, Title = title, Female = female };

        static CharacterLook Angler(string name, string title, bool female = false) => new CharacterLook { Name = name, Title = title, Female = female };

        static CharacterLook Owner(string name, string title) => new CharacterLook { Name = name, Title = title };

        static NpcSpec Npc(float x, float z, float yaw, CharacterLook look, NpcAngler.Role role, string id = null, float y = NaN) =>
            new NpcSpec { Pos = new Vector3(x, y, z), Yaw = yaw, Look = look, Role = role, Id = id };

        static readonly Color SignBlue = new Color(0.6f, 0.85f, 1f), SignGold = new Color(1f, 0.88f, 0.6f);

        // ===================================================== 서해 =====================================================

        /// <summary>영흥도 (Lv.9): 진두항 방파제, 십리포 해변 앞 목섬(썰물이면 모래톱으로 걸어 들어간다), 서쪽 갯벌, 영흥대교. 흐린 날 해무.</summary>
        public static CoastSpec Yeongheung() => new CoastSpec
        {
            Id = "yeongheung", MapName = "영흥도 · 서해", ShortName = "영흥도", ShopName = "영흥 낚시점", UnionContact = "진두항 입구의 어촌계 최계장",
            LandName = "영흥도 마을", SeaName = "영흥도 앞바다", Seed = 9,
            WaterDeep = new Color(0.1f, 0.16f, 0.15f), WaterShallow = new Color(0.28f, 0.32f, 0.25f), Tide = 0.55f, FogBoost = 0.014f,
            Shores = new[] { new ShoreSeg(-999f, -65f, ShoreKind.Mud), new ShoreSeg(-65f, 18f, ShoreKind.Beach), new ShoreSeg(25f, 112f, ShoreKind.Quay), new ShoreSeg(112f, 999f, ShoreKind.Rock) },
            NearDepth = 3f, DepthSlope = 0.05f, FlatWidth = 40f, CliffHeight = 12f, CliffWidth = 45f, RoadOffset = 22f,
            Breakwaters = new[]
            {
                new BreakwaterSpec { Name = "진두항 방파제", Stem = new Rect(98f, -40f, 7f, 60f), Arm = new Rect(68f, 14f, 37f, 6f), Light = new Color(0.85f, 0.15f, 0.12f) },
                new BreakwaterSpec { Name = "진두항 작은 방파제", Stem = new Rect(30f, -40f, 6f, 34f), Light = new Color(0.95f, 0.95f, 0.93f), ParapetWest = true },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(-22f, 0f, 48f), Radius = 11f, Height = 5f, Walkable = true, SandBar = true, Trees = true, Name = "목섬" } },
            Capes = new[] { new CapeSpec { Base = new Vector2(168f, -62f), Tip = new Vector2(166f, 8f), WidthBase = 30f, WidthTip = 15f, Height = 13f, Forest = true, Name = "선재 갯바위" } },
            Features = CoastFeature.SeaBridge,
            Grid = new[,]
            {
                { S, S, S, S, D, D, R, R },
                { W, S, S, S, D, D, R, R },
                { W, S, S, R, D, D, R, R },
                { S, W, S, D, D, D, D, R },
                { D, D, D, D, D, R, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(62f, 1.1f, -47f), ShopPos = new Vector3(48f, 1f, -52f),
            ShopOwner = Owner("영흥 낚시점 노사장", "영흥 낚시점"),
            Npcs = new[]
            {
                Npc(90f, -47f, 200f, Union("어촌계 최계장", "한국 낚시 조합 영흥 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(80f, 18.8f, 0f, Angler("우럭 삼촌", "선상 20년"), NpcAngler.Role.Angler, y: 1f),
                Npc(-40f, -44f, 0f, Angler("해변 꼬마 낚시꾼", "망둥어 전문"), NpcAngler.Role.Angler),
                Npc(-110f, -50f, 20f, Angler("갯벌 할머니", "바지락 캐는 법 알려 줌", true), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("진두항", new Vector3(62f, 6.5f, -60f), SignBlue), ("십리포 해수욕장", new Vector3(-25f, 5f, -60f), SignGold), ("영흥대교", new Vector3(-190f, 6f, -62f), SignGold) },
            Areas = new[] { (new Rect(-65f, -52f, 83f, 16f), "십리포 해변"), (new Rect(-200f, -45f, 135f, 120f), "영흥 갯벌") },
            FarIslands = new[] { new Vector4(-150f, 480f, 150f, 36f), new Vector4(80f, 560f, 220f, 46f), new Vector4(300f, 470f, 140f, 30f), new Vector4(20f, 700f, 320f, 60f) },
            Schools = () => new List<FishSchool>
            {
                School("망둥어 떼", 1, 1, 2, new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, "goby", "marbledflounder"),
                School("점농어 무리", 2, 1, 3, DawnEve, "spottedseabass", "seabass"),
                School("광어·붕장어", 3, 2, 4, new[] { TimeBand.Day, TimeBand.Evening, TimeBand.Night }, "flounder", "conger"),
                School("우럭·노래미", 6, 2, 4, MornEve, "jacopever", "greenling", "blackseabream"),
                School("백조기·갑오징어", 4, 4, 6, EveNight, "whitecroaker", "cuttlefish", "shad"),
                Special("점농어 대물", "spottedseabass", (TimeBand.Dawn, 1, 2), (TimeBand.Evening, 5, 1)),
            },
        };

        /// <summary>태안반도 연안 (Lv.11): 만리포의 긴 모래 해변과 신두리 해안사구, 해변 앞 할미·할아비바위, 만리포항. 서해답지 않게 맑고 푸르다.</summary>
        public static CoastSpec Taean() => new CoastSpec
        {
            Id = "taean", MapName = "태안반도 연안 · 서해", ShortName = "태안반도", ShopName = "만리포 낚시점", UnionContact = "만리포항 입구의 어촌계 한계장",
            LandName = "만리포 마을", SeaName = "태안 앞바다", Seed = 11,
            WaterDeep = new Color(0.05f, 0.17f, 0.24f), WaterShallow = new Color(0.2f, 0.42f, 0.42f), Tide = 0.6f,
            Shores = new[] { new ShoreSeg(-999f, -22f, ShoreKind.Beach), new ShoreSeg(-22f, 32f, ShoreKind.Rock), new ShoreSeg(32f, 122f, ShoreKind.Quay), new ShoreSeg(122f, 999f, ShoreKind.Rock) },
            NearDepth = 3f, DepthSlope = 0.06f, FlatWidth = 48f, CliffHeight = 10f, CliffWidth = 45f, RoadOffset = 22f,
            Features = CoastFeature.Dunes, DuneArea = new Rect(-195f, -128f, 150f, 58f),
            Breakwaters = new[]
            {
                new BreakwaterSpec { Name = "만리포항 방파제", Stem = new Rect(104f, -40f, 7f, 56f), Arm = new Rect(74f, 10f, 37f, 6f), Light = new Color(0.85f, 0.15f, 0.12f) },
                new BreakwaterSpec { Name = "만리포항 작은 방파제", Stem = new Rect(40f, -40f, 6f, 30f), Light = new Color(0.95f, 0.95f, 0.93f), ParapetWest = true },
            },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(-62f, 0f, 22f), Radius = 6.5f, Height = 15f, Pines = true, Name = "할미바위" },
                new StackSpec { Pos = new Vector3(-88f, 0f, 38f), Radius = 8.5f, Height = 19f, Pines = true, Name = "할아비바위" },
            },
            Capes = new[] { new CapeSpec { Base = new Vector2(172f, -62f), Tip = new Vector2(160f, 34f), WidthBase = 34f, WidthTip = 17f, Height = 16f, Forest = true, Name = "모항 갯바위" } },
            Grid = new[,]
            {
                { S, S, S, R, S, D, R, R },
                { S, S, R, R, S, D, R, R },
                { S, S, S, S, D, D, D, R },
                { S, W, S, D, D, D, D, R },
                { D, D, D, D, D, R, D, D },
                { D, D, D, D, D, D, D, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(78f, 1.1f, -47f), ShopPos = new Vector3(62f, 1f, -52f),
            ShopOwner = Owner("만리포 낚시점 윤사장", "만리포 낚시점"),
            Npcs = new[]
            {
                Npc(96f, -47f, 200f, Union("어촌계 한계장", "한국 낚시 조합 태안 지부", true), NpcAngler.Role.Talker, "union"),
                Npc(85f, 14.8f, 0f, Angler("쭈갑 전문가", "에기 색깔 30종 보유", true), NpcAngler.Role.Angler, y: 1f),
                Npc(-130f, -46f, 10f, Angler("원투 아저씨", "만리포 광어 지킴이"), NpcAngler.Role.Angler),
                Npc(-60f, -54f, 30f, Angler("노을 사진가", "꽃지 노을 1000장"), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("만리포항", new Vector3(78f, 6.5f, -60f), SignBlue), ("만리포 해수욕장", new Vector3(-100f, 5f, -62f), SignGold), ("신두리 해안사구", new Vector3(-120f, 9f, -96f), SignGold) },
            Areas = new[] { (new Rect(-200f, -54f, 178f, 22f), "만리포 해수욕장"), (new Rect(-195f, -128f, 150f, 58f), "신두리 해안사구") },
            FarIslands = new[] { new Vector4(-260f, 450f, 160f, 34f), new Vector4(40f, 600f, 240f, 50f), new Vector4(280f, 520f, 150f, 36f) },
            Schools = () => new List<FishSchool>
            {
                School("주꾸미 밭", 2, 1, 2, MornDay, "octopus", "marbledflounder"),
                School("광어 무리", 1, 1, 3, new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, "flounder", "goby"),
                School("갑오징어·우럭", 6, 1, 4, new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Morning }, "cuttlefish", "jacopever", "greenling"),
                School("전어·백조기", 3, 2, 4, MornEve, "shad", "whitecroaker"),
                School("참돔 어군", 5, 4, 7, MornDay, "redseabream", "conger", "blackseabream"),
                Special("점농어 떼", "spottedseabass", (TimeBand.Dawn, 1, 1), (TimeBand.Night, 6, 2)),
            },
        };

        // ===================================================== 동해 =====================================================

        /// <summary>삼척 앞바다 (Lv.14): 삼척항 두 방파제, 새천년해안도로, 추암 촛대바위와 해암정, 밤바다 오징어배 집어등. 차고 깊은 쪽빛 바다.</summary>
        public static CoastSpec Samcheok() => new CoastSpec
        {
            Id = "samcheok", MapName = "삼척 앞바다 · 동해", ShortName = "삼척 앞바다", ShopName = "삼척 낚시점", UnionContact = "삼척항의 수협 강과장",
            LandName = "삼척 해안 마을", SeaName = "삼척 앞바다", Seed = 14,
            WaterDeep = new Color(0.02f, 0.09f, 0.22f), WaterShallow = new Color(0.08f, 0.3f, 0.42f),
            Shores = new[] { new ShoreSeg(-999f, -92f, ShoreKind.Rock), new ShoreSeg(-92f, -22f, ShoreKind.Beach), new ShoreSeg(-14f, 82f, ShoreKind.Quay), new ShoreSeg(82f, 999f, ShoreKind.Rock) },
            NearDepth = 5f, DepthSlope = 0.09f, FlatWidth = 36f, CliffHeight = 16f, CliffWidth = 36f, RoadOffset = 22f,
            Breakwaters = new[]
            {
                new BreakwaterSpec { Name = "삼척항 방파제", Stem = new Rect(72f, -40f, 7f, 70f), Arm = new Rect(40f, 24f, 39f, 6f), Light = new Color(0.85f, 0.15f, 0.12f) },
                new BreakwaterSpec { Name = "삼척항 흰 등대", Stem = new Rect(-12f, -40f, 6f, 46f), Light = new Color(0.95f, 0.95f, 0.93f), ParapetWest = true },
            },
            Capes = new[] { new CapeSpec { Base = new Vector2(142f, -72f), Tip = new Vector2(136f, -24f), WidthBase = 26f, WidthTip = 14f, Height = 11f, Forest = true, Landmark = Landmark.Pavilion, Name = "추암 해암정" } },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(150f, 0f, -12f), Radius = 2.4f, Height = 15f, Name = "추암 촛대바위" },
                new StackSpec { Pos = new Vector3(161f, 0f, -4f), Radius = 3.6f, Height = 9f, Name = "형제바위" },
            },
            Features = CoastFeature.SquidBoats | CoastFeature.Mountains | CoastFeature.SeaRocks,
            Bioluminescence = 1f, // 삼척해수욕장 밤 파도가 야광충으로 파랗게 (사용자 참고 사진)
            Grid = new[,]
            {
                { R, R, S, S, D, D, R, R },
                { R, W, S, S, D, D, R, R },
                { R, R, S, D, D, D, R, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, R, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, R, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(26f, 1.1f, -47f), ShopPos = new Vector3(10f, 1f, -52f),
            ShopOwner = Owner("삼척 낚시점 정사장", "삼척 낚시점"),
            Npcs = new[]
            {
                Npc(58f, -47f, 200f, Union("수협 강과장", "한국 낚시 조합 삼척 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(52f, 28.8f, 0f, Angler("임연수 형님", "동해 원투 30년", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-9.6f, 0f, -80f, Angler("등대 낚시꾼", "학꽁치 채비 장인"), NpcAngler.Role.Angler, y: 1f),
                Npc(30f, -46f, 160f, Angler("오징어배 선장", "집어등 20년", false), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("삼척항", new Vector3(26f, 6.5f, -60f), SignBlue), ("새천년해안도로", new Vector3(-150f, 5.5f, -64f), SignGold), ("추암 촛대바위", new Vector3(125f, 6f, -66f), SignGold) },
            Areas = new[] { (new Rect(-92f, -52f, 70f, 16f), "삼척해수욕장") },
            Schools = () => new List<FishSchool>
            {
                School("임연수어 떼", 1, 1, 3, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, "atkamackerel", "greenling"),
                School("가자미 밭", 3, 1, 3, MornDay, "flatfish", "halfbeak"),
                School("도루묵 떼", 2, 0, 1, EveNight, "sandfish", "jacopever"),
                School("고등어 떼", 4, 2, 6, MornEve, "mackerel", "halfbeak", "blackseabream"),
                School("대구·대문어", 6, 4, 7, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, "cod", "giantoctopus"),
                Special("오징어 집어등", "squid", (TimeBand.Evening, 5, 4), (TimeBand.Night, 3, 6)),
            },
        };

        /// <summary>동해 등대섬 (Lv.16): 작은 섬. 언덕 위 큰 등대와 등대지기 숙소, 선착장, 양옆 갯바위섬. 물이 아주 맑다.</summary>
        public static CoastSpec Deungdae() => new CoastSpec
        {
            Id = "deungdae", MapName = "동해 등대섬 · 동해", ShortName = "등대섬", ShopName = "등대섬 매점", UnionContact = "선착장의 등대섬 이장님",
            LandName = "등대섬", SeaName = "등대섬 앞바다", Seed = 16, LandHalfWidth = 98f,
            WaterDeep = new Color(0.02f, 0.1f, 0.25f), WaterShallow = new Color(0.05f, 0.38f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, 18f, ShoreKind.Rock), new ShoreSeg(18f, 72f, ShoreKind.Quay), new ShoreSeg(72f, 999f, ShoreKind.Rock) },
            ShoreBumps = new[] { new Vector3(-60f, 18f, 6f) },
            NearDepth = 6f, DepthSlope = 0.1f, FlatWidth = 30f, CliffHeight = 20f, CliffWidth = 30f, RoadOffset = 20f, VillageDensity = 0.3f,
            Breakwaters = new[] { new BreakwaterSpec { Name = "등대섬 선착장", Stem = new Rect(62f, -40f, 6f, 40f), Arm = new Rect(38f, -6f, 30f, 6f), Light = new Color(0.95f, 0.95f, 0.93f) } },
            Capes = new[] { new CapeSpec { Base = new Vector2(-45f, -75f), Tip = new Vector2(-58f, -4f), WidthBase = 32f, WidthTip = 20f, Height = 24f, Forest = true, Landmark = Landmark.MukhoLighthouse, Name = "등대 언덕" } },
            Islets = new[]
            {
                new IsletSpec { Center = new Vector3(-118f, 0f, 92f), Radius = 14f, Height = 6f, Walkable = true, Name = "서쪽 갯바위섬" },
                new IsletSpec { Center = new Vector3(128f, 0f, 150f), Radius = 12f, Height = 4f, Walkable = true, Name = "동쪽 여" },
            },
            Stacks = new[] { new StackSpec { Pos = new Vector3(112f, 0f, 30f), Radius = 5f, Height = 14f, Pines = true, Name = "삼형제굴바위" } },
            Features = CoastFeature.SquidBoats | CoastFeature.SeaRocks,
            Grid = new[,]
            {
                { D, R, R, R, S, S, R, D },
                { D, R, W, R, D, S, R, D },
                { D, R, D, D, D, D, R, D },
                { R, D, D, D, D, D, D, D },
                { R, D, D, D, R, D, D, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(48f, 1.1f, -47f), ShopPos = new Vector3(30f, 1f, -52f),
            ShopOwner = Owner("등대섬 매점 할머니", "등대섬 매점"),
            Npcs = new[]
            {
                Npc(70f, -47f, 220f, Union("등대섬 이장님", "한국 낚시 조합 등대섬 연락소", false), NpcAngler.Role.Talker, "union"),
                Npc(-55f, -20f, 0f, Angler("등대지기 윤씨", "등대를 지킨 지 25년", false), NpcAngler.Role.Talker),
                Npc(-118f, 100f, 0f, Angler("갯바위 독조사", "혼자가 편하다"), NpcAngler.Role.Angler),
                Npc(52f, -2f, 0f, Angler("열기 할아버지", "카드채비 10단"), NpcAngler.Role.Angler, y: 1f),
            },
            Signs = new[] { ("등대섬 선착장", new Vector3(48f, 6.5f, -60f), SignBlue) },
            FarIslands = new[] { new Vector4(-380f, 520f, 200f, 70f), new Vector4(260f, 640f, 160f, 40f) },
            Schools = () => new List<FishSchool>
            {
                School("가자미·임연수", 4, 0, 2, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, "flatfish", "atkamackerel"),
                School("열기 떼", 2, 2, 5, MornDay, "redrockfish", "rockfish"),
                School("우럭·노래미", 6, 1, 4, MornEve, "jacopever", "greenling", "blackseabream"),
                School("고등어·전갱이", 3, 3, 6, MornEve, "mackerel", "horsemackerel"),
                School("방어 떼", 5, 3, 7, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, "yellowtail", "mackerel"),
                Special("오징어 집어등", "squid", (TimeBand.Evening, 4, 5), (TimeBand.Night, 2, 6)),
            },
        };

        /// <summary>헌화로 (Lv.18): 절벽 바로 아래 바다를 끼고 달리는 해안도로, 갯바위 곶, 바다부채길 데크, 심곡항. 절벽엔 진달래 (헌화가).</summary>
        public static CoastSpec Heonhwa() => new CoastSpec
        {
            Id = "heonhwa", MapName = "헌화로 · 동해", ShortName = "헌화로", ShopName = "심곡 낚시점", UnionContact = "심곡항의 어촌계 김계장",
            LandName = "헌화로", SeaName = "헌화로 앞바다", Seed = 18,
            WaterDeep = new Color(0.02f, 0.1f, 0.24f), WaterShallow = new Color(0.06f, 0.36f, 0.46f),
            Shores = new[] { new ShoreSeg(-999f, -128f, ShoreKind.Rock), new ShoreSeg(-128f, -62f, ShoreKind.Quay), new ShoreSeg(-62f, 999f, ShoreKind.Rock) },
            ShoreBumps = new[] { new Vector3(40f, 30f, -6f), new Vector3(150f, 25f, -5f) },
            NearDepth = 4f, DepthSlope = 0.09f, FlatWidth = 18f, CliffHeight = 30f, CliffWidth = 22f, RoadOffset = 9f, VillageDensity = 0.25f,
            Breakwaters = new[] { new BreakwaterSpec { Name = "심곡항 방파제", Stem = new Rect(-78f, -40f, 6f, 34f), Arm = new Rect(-112f, -12f, 40f, 6f), Light = new Color(0.95f, 0.95f, 0.93f) } },
            Capes = new[]
            {
                new CapeSpec { Base = new Vector2(-10f, -52f), Tip = new Vector2(-2f, 8f), WidthBase = 15f, WidthTip = 8f, Height = 6f, Landmark = Landmark.Shrine, Name = "합궁골 갯바위" },
                new CapeSpec { Base = new Vector2(118f, -52f), Tip = new Vector2(128f, 14f), WidthBase = 17f, WidthTip = 9f, Height = 8f, Name = "헌화로 갯바위" },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(60f, 0f, 46f), Radius = 9f, Height = 3f, Walkable = true, Name = "앞 여" } },
            Stacks = new[] { new StackSpec { Pos = new Vector3(-38f, 0f, 22f), Radius = 3f, Height = 11f, Name = "부채바위" } },
            Platforms = new[] { new PlatformSpec { Area = new Rect(20f, -42.2f, 86f, 3.2f), Kind = PlatformKind.Boardwalk, Name = "바다부채길" } },
            Features = CoastFeature.Mountains | CoastFeature.Azaleas | CoastFeature.SquidBoats | CoastFeature.SeaRocks,
            Grid = new[,]
            {
                { R, R, R, R, R, R, R, R },
                { R, D, S, R, D, R, S, R },
                { D, D, D, D, R, D, D, R },
                { D, R, D, D, D, D, D, D },
                { D, D, D, R, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, D, R, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(-78f, 1.1f, -46f), ShopPos = new Vector3(-96f, 1f, -57f),
            ShopOwner = Owner("심곡 낚시점 조사장", "심곡 낚시점"),
            Npcs = new[]
            {
                Npc(-66f, -46f, 200f, Union("어촌계 김계장", "한국 낚시 조합 강릉 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(-2f, 4f, 0f, Angler("갯바위 감생이꾼", "새벽 4시 출조", false), NpcAngler.Role.Angler),
                Npc(128f, 10f, 10f, Angler("문어 할아버지", "대문어 30kg 기록"), NpcAngler.Role.Angler),
                Npc(60f, -41f, 20f, Angler("부채길 산책객", "헌화가를 외운다", true), NpcAngler.Role.Talker, y: 1.4f),
            },
            Signs = new[] { ("헌화로", new Vector3(-160f, 5.5f, -52f), SignGold), ("심곡항", new Vector3(-92f, 6f, -50f), SignBlue), ("바다부채길", new Vector3(60f, 5f, -47f), SignGold) },
            Schools = () => new List<FishSchool>
            {
                School("감성돔 무리", 2, 0, 2, DawnEve, "blackseabream", "seabass"),
                School("가자미·학꽁치", 1, 1, 3, MornDay, "flatfish", "halfbeak"),
                School("우럭·노래미", 6, 0, 3, MornEve, "jacopever", "greenling"),
                School("열기·볼락", 5, 2, 5, new[] { TimeBand.Morning, TimeBand.Evening, TimeBand.Night }, "redrockfish", "rockfish"),
                School("오징어·고등어", 4, 4, 7, EveNight, "squid", "mackerel"),
                Special("대문어 굴", "giantoctopus", (TimeBand.Dawn, 3, 1), (TimeBand.Evening, 6, 1)),
            },
        };

        /// <summary>청진포구 (Lv.22): 북쪽 끝 눈 덮인 큰 항구. 긴 방파제 두 개, 크레인과 창고, 명태 덕장, 물 위의 얼음 조각. 명태의 고향.</summary>
        public static CoastSpec Cheongjin() => new CoastSpec
        {
            Id = "cheongjin", MapName = "청진포구 · 동해 북부", ShortName = "청진포구", ShopName = "청진 낚시점", UnionContact = "포구의 어로반장 박씨",
            LandName = "청진포구", SeaName = "청진 앞바다", Seed = 22, Snow = true, NorthPort = true, FogBoost = 0.006f,
            WaterDeep = new Color(0.03f, 0.08f, 0.14f), WaterShallow = new Color(0.12f, 0.22f, 0.28f),
            Shores = new[] { new ShoreSeg(-999f, -42f, ShoreKind.Rock), new ShoreSeg(-42f, 142f, ShoreKind.Quay), new ShoreSeg(142f, 999f, ShoreKind.Rock) },
            NearDepth = 5f, DepthSlope = 0.08f, FlatWidth = 44f, CliffHeight = 14f, CliffWidth = 40f, RoadOffset = 24f, VillageDensity = 1.1f,
            Breakwaters = new[]
            {
                new BreakwaterSpec { Name = "청진 동방파제", Stem = new Rect(150f, -40f, 8f, 90f), Arm = new Rect(92f, 44f, 66f, 7f), Light = new Color(0.85f, 0.15f, 0.12f) },
                new BreakwaterSpec { Name = "청진 서방파제", Stem = new Rect(-58f, -40f, 7f, 60f), Arm = new Rect(-58f, 14f, 42f, 7f), Light = new Color(0.95f, 0.95f, 0.93f), ParapetWest = true },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(-125f, 0f, 120f), Radius = 12f, Height = 4f, Walkable = true, Name = "북쪽 갯바위" } },
            Features = CoastFeature.Cranes | CoastFeature.DryingRacks | CoastFeature.Mountains | CoastFeature.IceFloes,
            Grid = new[,]
            {
                { R, W, S, D, D, D, D, R },
                { R, W, S, D, D, D, D, R },
                { R, R, D, D, D, D, D, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, R, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, R, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(28f, 1.1f, -47f), ShopPos = new Vector3(10f, 1f, -54f),
            ShopRoof = new Color(0.55f, 0.2f, 0.18f),
            ShopOwner = Owner("청진 낚시점 리사장", "청진 낚시점"),
            Npcs = new[]
            {
                Npc(40f, -47f, 200f, Union("어로반장 박씨", "한국 낚시 조합 청진 연락소", false), NpcAngler.Role.Talker, "union"),
                Npc(120f, 47.8f, 0f, Angler("명태잡이 노인", "명태 3만 마리", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-40f, 17.8f, 0f, Angler("도루묵 아주머니", "알배기만 노린다", true), NpcAngler.Role.Angler, y: 1f),
                Npc(-60f, -70f, 160f, Angler("덕장 주인", "황태 만드는 사람", false), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("청진포구", new Vector3(28f, 7f, -62f), SignBlue) },
            FarIslands = new[] { new Vector4(-300f, 560f, 220f, 50f), new Vector4(200f, 620f, 260f, 60f) },
            Schools = () => new List<FishSchool>
            {
                School("도루묵 떼", 2, 0, 2, EveNight, "sandfish", "flatfish"),
                School("임연수어 떼", 1, 1, 4, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, "atkamackerel", "greenling"),
                School("가자미 밭", 4, 1, 3, MornDay, "flatfish", "jacopever"),
                School("대구 어군", 5, 3, 7, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, "cod", "giantoctopus"),
                // 명태 어군: 새벽 B5, 밤 F6 (도감 설명과 같다)
                Special("명태 어군", "pollock", (TimeBand.Dawn, 1, 4), (TimeBand.Night, 5, 5)),
            },
        };

        // ===================================================== 남해 =====================================================

        /// <summary>욕지도 (Lv.19): 욕지항, 바다 위 가두리 낚시터와 잔교, 펠리컨바위 절벽과 출렁다리, 삼여 바위. 참돔·방어·갈치.</summary>
        public static CoastSpec Yokji() => new CoastSpec
        {
            Id = "yokji", MapName = "욕지도 · 남해", ShortName = "욕지도", ShopName = "욕지 낚시점", UnionContact = "욕지항의 어촌계 서계장",
            LandName = "욕지도 마을", SeaName = "욕지도 앞바다", Seed = 19, LandHalfWidth = 150f,
            WaterDeep = new Color(0.05f, 0.2f, 0.28f), WaterShallow = new Color(0.12f, 0.42f, 0.42f),
            Shores = new[] { new ShoreSeg(-999f, -42f, ShoreKind.Rock), new ShoreSeg(-42f, 62f, ShoreKind.Quay), new ShoreSeg(70f, 112f, ShoreKind.Beach), new ShoreSeg(112f, 999f, ShoreKind.Rock) },
            NearDepth = 4f, DepthSlope = 0.07f, FlatWidth = 36f, CliffHeight = 18f, CliffWidth = 40f, RoadOffset = 22f, VillageDensity = 0.7f,
            Breakwaters = new[]
            {
                new BreakwaterSpec { Name = "욕지항 방파제", Stem = new Rect(56f, -40f, 7f, 60f), Arm = new Rect(20f, 14f, 43f, 6f), Light = new Color(0.85f, 0.15f, 0.12f) },
                new BreakwaterSpec { Name = "욕지항 작은 방파제", Stem = new Rect(-46f, -40f, 6f, 40f), Light = new Color(0.95f, 0.95f, 0.93f), ParapetWest = true },
            },
            Platforms = new[]
            {
                new PlatformSpec { Area = new Rect(-150f, 16f, 48f, 36.4f), Kind = PlatformKind.FishFarm, Name = "가두리 낚시터" },
                new PlatformSpec { Area = new Rect(-128f, -42f, 3f, 59f), Kind = PlatformKind.Pier, Name = "가두리 잔교" },
            },
            Capes = new[] { new CapeSpec { Base = new Vector2(128f, -72f), Tip = new Vector2(146f, -6f), WidthBase = 28f, WidthTip = 13f, Height = 22f, Forest = true, Name = "펠리컨바위 절벽" } },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(166f, 0f, 18f), Radius = 4f, Height = 17f, Pines = true, Name = "펠리컨바위" },
                new StackSpec { Pos = new Vector3(-62f, 0f, 120f), Radius = 4f, Height = 14f, Name = "삼여" },
                new StackSpec { Pos = new Vector3(-48f, 0f, 129f), Radius = 3.2f, Height = 11f, Name = "삼여" },
                new StackSpec { Pos = new Vector3(-72f, 0f, 134f), Radius = 3f, Height = 9f, Name = "삼여" },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(84f, 0f, 150f), Radius = 14f, Height = 5f, Walkable = true, Name = "좌사리 갯바위" } },
            Features = CoastFeature.SuspensionBridge,
            Grid = new[,]
            {
                { R, W, R, S, S, S, R, R },
                { W, W, R, S, D, S, R, R },
                { W, R, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, R, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, R },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(26f, 1.1f, -47f), ShopPos = new Vector3(10f, 1f, -52f),
            ShopOwner = Owner("욕지 낚시점 하사장", "욕지 낚시점"),
            Npcs = new[]
            {
                Npc(40f, -47f, 200f, Union("어촌계 서계장", "한국 낚시 조합 통영 지부", true), NpcAngler.Role.Talker, "union"),
                Npc(-149.2f, 30f, 270f, Angler("가두리 단골", "참돔 손맛 중독"), NpcAngler.Role.Angler, y: 0.65f),
                Npc(-105f, 49f, 180f, Angler("가두리 사장", "고등어 키운 지 15년", false), NpcAngler.Role.Talker, y: 0.75f),
                Npc(40f, 18.8f, 0f, Angler("방어 사냥꾼", "지깅 전문"), NpcAngler.Role.Angler, y: 1f),
            },
            Signs = new[] { ("욕지항", new Vector3(26f, 6.5f, -60f), SignBlue), ("욕지 고구마", new Vector3(-20f, 6f, -76f), SignGold), ("가두리 낚시터", new Vector3(-126f, 4f, -46f), SignBlue) },
            FarIslands = new[] { new Vector4(-280f, 480f, 180f, 50f), new Vector4(120f, 600f, 220f, 55f), new Vector4(330f, 430f, 120f, 40f) },
            Schools = () => new List<FishSchool>
            {
                School("고등어·전갱이", 1, 1, 3, MornEve, "mackerel", "horsemackerel", "halfbeak"),
                School("감성돔·벵에돔", 6, 0, 2, DawnEve, "blackseabream", "opaleye", "rockfish"),
                School("참돔 어군", 3, 3, 6, MornDay, "redseabream", "seabass"),
                School("방어·삼치", 5, 3, 7, new[] { TimeBand.Dawn, TimeBand.Morning }, "yellowtail", "spanishmackerel"),
                School("갈치 떼", 4, 4, 7, EveNight, "hairtail", "conger"),
                Special("대물 참돔", "redseabream", (TimeBand.Morning, 2, 5), (TimeBand.Evening, 5, 4)),
            },
        };

        /// <summary>
        /// 제주 서귀포 (Lv.21, 원작 이벤트 지역): 서귀포항 두 방파제, 검은 현무암 갯바위, 바다에 선 외돌개, 앞바다의 범섬,
        /// 주상절리 곶(기둥 벽·너럭 단·전망 데크), 동쪽 바다 너머 성산일출봉, 서쪽 갯바위 앞 용두암, 뒤로 한라산과 오름, 돌담 두른 귤밭, 돌하르방, 해녀 할망. 맑은 쪽빛 물, 자리돔·옥돔·무늬오징어, 밤 먼바다엔 다금바리 어군.
        /// </summary>
        public static CoastSpec Jeju() => new CoastSpec
        {
            Id = "jeju", MapName = "제주 서귀포 · 남해", ShortName = "제주", ShopName = "서귀포 낚시점", UnionContact = "서귀포항의 어촌계 고계장",
            LandName = "서귀포 마을", SeaName = "서귀포 앞바다", Seed = 21, DarkRock = true, CliffTexture = "darkrock", CliffTint = new Color(1.35f, 1.35f, 1.4f, 1f),
            WaterDeep = new Color(0.02f, 0.14f, 0.26f), WaterShallow = new Color(0.04f, 0.45f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -132f, ShoreKind.Rock), new ShoreSeg(-132f, -112f, ShoreKind.Beach), new ShoreSeg(-112f, -12f, ShoreKind.Rock),
                             new ShoreSeg(-12f, 82f, ShoreKind.Quay), new ShoreSeg(82f, 999f, ShoreKind.Rock) },
            NearDepth = 5f, DepthSlope = 0.09f, FlatWidth = 40f, CliffHeight = 16f, CliffWidth = 50f, RoadOffset = 22f, VillageDensity = 0.7f,
            Breakwaters = new[]
            {
                new BreakwaterSpec { Name = "서귀포항 방파제", Stem = new Rect(74f, -40f, 7f, 58f), Arm = new Rect(42f, 12f, 39f, 6f), Light = new Color(0.85f, 0.15f, 0.12f) },
                new BreakwaterSpec { Name = "서귀포항 흰 등대", Stem = new Rect(-4f, -40f, 6f, 36f), Light = new Color(0.95f, 0.95f, 0.93f), ParapetWest = true },
            },
            Capes = new[] { new CapeSpec { Base = new Vector2(-70f, -72f), Tip = new Vector2(-82f, -6f), WidthBase = 30f, WidthTip = 16f, Height = 19f, Forest = true, Name = "주상절리" } },   // 주상절리대 절벽 높이 (사진: 20m 안팎)
            Stacks = new[] { new StackSpec { Pos = new Vector3(-40f, 0f, 16f), Radius = 3.6f, Height = 20f, Pines = true, Name = "외돌개" } },
            Islets = new[] { new IsletSpec { Center = new Vector3(118f, 0f, 160f), Radius = 30f, Height = 26f, Walkable = true, Trees = true, Name = "범섬" } },
            Features = CoastFeature.Hallasan | CoastFeature.DolHareubang | CoastFeature.Tangerines | CoastFeature.Columnar | CoastFeature.Seongsan | CoastFeature.Yongduam,
            Grid = new[,]
            {
                { R, R, R, W, S, S, R, R },
                { R, W, R, R, D, D, R, R },
                { W, R, D, D, D, D, D, R },
                { R, D, D, D, D, D, R, D },
                { D, D, D, D, R, D, D, D },
                { D, D, D, D, D, D, R, R },
                { D, D, D, D, D, D, R, R },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(26f, 1.1f, -47f), ShopPos = new Vector3(10f, 1f, -52f),
            ShopRoof = new Color(0.9f, 0.5f, 0.15f),
            ShopOwner = Owner("서귀포 낚시점 오사장", "서귀포 낚시점"),
            Npcs = new[]
            {
                Npc(50f, -47f, 200f, Union("어촌계 고계장", "한국 낚시 조합 제주 지부", true), NpcAngler.Role.Talker, "union"),
                Npc(-30f, -46f, 20f, new CharacterLook { Name = "해녀 할망", Title = "물질 50년", Female = true }, NpcAngler.Role.Talker),
                Npc(60f, 16.8f, 0f, Angler("벵에돔 꾼", "크릴 밑밥 장인"), NpcAngler.Role.Angler, y: 1f),
                Npc(-82f, -4f, 0f, Angler("무늬오징어 에깅러", "가을엔 서귀포", true), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("서귀포항", new Vector3(26f, 6.5f, -60f), SignBlue), ("외돌개", new Vector3(-40f, 5.5f, -60f), SignGold), ("주상절리대", new Vector3(-70f, 9f, -78f), SignGold), ("용두암", new Vector3(-160f, 5.5f, -60f), SignGold) },
            FarIslands = new[] { new Vector4(-200f, 330f, 60f, 26f), new Vector4(-90f, 380f, 40f, 18f), new Vector4(260f, 520f, 120f, 30f) },
            Schools = () => new List<FishSchool>
            {
                School("자리돔 떼", 2, 0, 2, new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, "damselfish", "opaleye"),
                School("벵에돔·감성돔", 5, 0, 2, DawnEve, "opaleye", "blackseabream", "beakfish"),
                School("무늬오징어", 1, 1, 3, new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, "bigfinsquid", "rockfish"),
                School("옥돔 밭", 3, 3, 6, MornDay, "tilefish", "redseabream"),
                School("갈치·방어", 6, 3, 7, EveNight, "hairtail", "yellowtail", "amberjack"),
                // 제주 다금바리: 저녁 G6, 밤 D7 (먼바다)
                Special("다금바리 어군", "dageumbari", (TimeBand.Evening, 6, 5), (TimeBand.Night, 3, 6)),
            },
        };

        /// <summary>거문도 (Lv.24): 남해 먼 섬. 수월산 끝 거문도 등대(1905)와 동백숲, 거문항, 바위섬들, 수평선의 백도. 부시리·갈치·돌돔, 먼바다엔 다금바리·돗돔.</summary>
        public static CoastSpec Geomun() => new CoastSpec
        {
            Id = "geomun", MapName = "거문도 · 남해 먼바다", ShortName = "거문도", ShopName = "거문도 낚시점", UnionContact = "거문항의 어촌계 문계장",
            LandName = "거문도 마을", SeaName = "거문도 앞바다", Seed = 24, LandHalfWidth = 125f,
            WaterDeep = new Color(0.02f, 0.12f, 0.26f), WaterShallow = new Color(0.05f, 0.42f, 0.5f),
            Shores = new[] { new ShoreSeg(-999f, -22f, ShoreKind.Rock), new ShoreSeg(-22f, 72f, ShoreKind.Quay), new ShoreSeg(72f, 999f, ShoreKind.Rock) },
            NearDepth = 6f, DepthSlope = 0.1f, FlatWidth = 34f, CliffHeight = 22f, CliffWidth = 34f, RoadOffset = 22f, VillageDensity = 0.55f,
            Breakwaters = new[] { new BreakwaterSpec { Name = "거문항 방파제", Stem = new Rect(66f, -40f, 7f, 55f), Arm = new Rect(34f, 9f, 39f, 6f), Light = new Color(0.85f, 0.15f, 0.12f) } },
            Capes = new[] { new CapeSpec { Base = new Vector2(-100f, -72f), Tip = new Vector2(-138f, 12f), WidthBase = 34f, WidthTip = 16f, Height = 26f, Forest = true, Landmark = Landmark.GeomunLighthouse, Name = "거문도 등대" } },
            Islets = new[]
            {
                new IsletSpec { Center = new Vector3(60f, 0f, 124f), Radius = 16f, Height = 6f, Walkable = true, Trees = true, Name = "서도 갯바위" },
                new IsletSpec { Center = new Vector3(150f, 0f, 62f), Radius = 10f, Height = 4f, Walkable = true, Name = "동도 여" },
            },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(-58f, 0f, 62f), Radius = 4f, Height = 18f, Name = "각시바위" },
                new StackSpec { Pos = new Vector3(-40f, 0f, 76f), Radius = 3f, Height = 11f, Name = "서방바위" },
            },
            Features = CoastFeature.FarRocks | CoastFeature.SeaRocks,
            Grid = new[,]
            {
                { R, R, R, S, S, D, R, R },
                { R, R, R, D, D, D, R, R },
                { D, R, D, D, D, D, D, R },
                { D, D, R, D, D, D, D, D },
                { D, D, D, D, R, D, D, D },
                { D, D, D, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(26f, 1.1f, -47f), ShopPos = new Vector3(8f, 1f, -52f),
            ShopOwner = Owner("거문도 낚시점 문사장", "거문도 낚시점"),
            Npcs = new[]
            {
                Npc(48f, -47f, 200f, Union("어촌계 문계장", "한국 낚시 조합 거문도 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(60f, 134f, 0f, Angler("부시리 헌터", "미터급 7마리"), NpcAngler.Role.Angler),
                Npc(40f, 13.8f, 0f, Angler("갈치 아저씨", "밤낚시 전문", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-118f, -20f, 300f, Angler("등대 해설사", "거문도 등대는 1905년생", true), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("거문항", new Vector3(26f, 6.5f, -60f), SignBlue), ("백도 →", new Vector3(150f, 6f, -48f), SignGold) },
            FarIslands = new[] { new Vector4(-330f, 420f, 180f, 60f), new Vector4(-80f, 640f, 240f, 50f) },
            Schools = () => new List<FishSchool>
            {
                School("돌돔·벵에돔", 1, 1, 3, MornDay, "beakfish", "opaleye"),
                School("감성돔·쏨뱅이", 6, 0, 2, DawnEve, "blackseabream", "scorpionfish", "rockfish"),
                School("참돔·열기", 3, 4, 7, MornDay, "redseabream", "redrockfish"),
                School("삼치·방어", 5, 2, 6, new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, "spanishmackerel", "yellowtail"),
                School("갈치 떼", 4, 3, 7, EveNight, "hairtail", "conger"),
                Special("부시리 무리", "amberjack", (TimeBand.Morning, 6, 3), (TimeBand.Day, 2, 5)),
            },
        };
    }
}
