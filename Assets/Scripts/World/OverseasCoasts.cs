using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 해외 거점 낚시터 (원작: 2012-04 동남아 거점, 지중해 산토리니, 2013-03 나일강 하류). 국내 해안 맵과 같은 <see cref="CoastSpec"/> 틀.
    ///  - 하롱베이(베트남, Lv.25): 에메랄드 바다에 솟은 석회암 봉우리, 티톱섬, 수상 마을, 주황 돛 정크선, 좁고 높은 색깔 집.
    ///  - 산토리니(그리스, Lv.28): 칼데라 절벽 아래 암모우디 항구, 절벽 위 하얀 집·파란 돔 교회·풍차, 바다 건너 칼데라 테와 검은 화산섬.
    ///  - 나일강 하류(이집트, Lv.31): 강(맞은편 둑), 야자수 강가, 흙벽돌 집과 비둘기탑, 펠루카 돛배, 사막 너머 피라미드. 민물.
    /// 좌표는 국내 해안 맵과 같다: 해안선 z ≈ -40, 물이 +Z, 격자 A~H × 1~8.
    /// </summary>
    public static partial class OverseasCoasts
    {
        const float NaN = float.NaN;
        static Habitat S => Habitat.Shallow;
        static Habitat D => Habitat.Deep;
        static Habitat R => Habitat.Rocky;
        static Habitat W => Habitat.Weed;

        static readonly TimeBand[] DawnEve = { TimeBand.Dawn, TimeBand.Evening };
        static readonly TimeBand[] MornDay = { TimeBand.Morning, TimeBand.Day };
        static readonly TimeBand[] EveNight = { TimeBand.Evening, TimeBand.Night };
        static readonly TimeBand[] DayLong = { TimeBand.Morning, TimeBand.Day, TimeBand.Evening };

        static FishSchool School(string name, int col, int row, int maxRow, TimeBand[] times, params string[] ids) =>
            new FishSchool { Name = name, SpeciesIds = ids, PreferredTimes = times, Col = col, Row = row, MinRow = Mathf.Max(0, row - 1), MaxRow = maxRow };

        static FishSchool Special(string name, string id, params (TimeBand, int, int)[] schedule) =>
            new FishSchool { Name = name, SpeciesIds = new[] { id }, Special = true, Schedule = schedule };

        static CharacterLook Local(string name, string title, bool female = false) => new CharacterLook { Name = name, Title = title, Female = female };

        static CharacterLook Union(string name, string title, bool female) => new CharacterLook { Name = name, Title = title, Female = female };

        static NpcSpec Npc(float x, float z, float yaw, CharacterLook look, NpcAngler.Role role, string id = null, float y = NaN) =>
            new NpcSpec { Pos = new Vector3(x, y, z), Yaw = yaw, Look = look, Role = role, Id = id };

        static readonly Color SignBlue = new Color(0.6f, 0.85f, 1f), SignGold = new Color(1f, 0.88f, 0.6f);

        /// <summary>하롱베이 (Lv.25): 뚜언쩌우 선착장 앞 에메랄드 바다. 투계바위·향로바위·티톱섬, 멀리 수천 개 석회암 봉우리, 수상 마을과 정크선.</summary>
        public static CoastSpec HaLong() => new CoastSpec
        {
            Id = "halong", MapName = "하롱베이 · 베트남", ShortName = "하롱베이", ShopName = "바이짜이 낚시점", UnionContact = "뚜언쩌우 선착장의 응우옌 지부장",
            LandName = "바이짜이 마을", SeaName = "하롱베이", Seed = 31,
            WaterDeep = new Color(0.03f, 0.2f, 0.22f), WaterShallow = new Color(0.12f, 0.5f, 0.42f),
            Shores = new[] { new ShoreSeg(-999f, -70f, ShoreKind.Beach), new ShoreSeg(-70f, 30f, ShoreKind.Quay), new ShoreSeg(30f, 999f, ShoreKind.Beach) },
            NearDepth = 3.5f, DepthSlope = 0.05f, FlatWidth = 30f, CliffHeight = 16f, CliffWidth = 40f, RoadOffset = 16f, VillageDensity = 1.1f,
            Houses = HouseStyle.Vietnam, TreeKind = TreeStyle.Tropical,
            Platforms = new[] { new PlatformSpec { Area = new Rect(-44f, -41f, 6f, 46f), Kind = PlatformKind.Pier, Name = "뚜언쩌우 잔교" } },
            Stacks = new[]
            {
                new StackSpec { Pos = new Vector3(-118f, 0f, 70f), Radius = 7f, Height = 24f, Pines = true, Name = "투계바위" },
                new StackSpec { Pos = new Vector3(-104f, 0f, 76f), Radius = 6f, Height = 21f, Pines = true, Name = "투계바위" },
                new StackSpec { Pos = new Vector3(122f, 0f, 92f), Radius = 6f, Height = 17f, Pines = true, Name = "향로바위" },
                new StackSpec { Pos = new Vector3(-60f, 0f, 168f), Radius = 22f, Height = 66f, Pines = true, Name = "석회암 봉우리" },
                new StackSpec { Pos = new Vector3(150f, 0f, 178f), Radius = 24f, Height = 78f, Pines = true, Name = "석회암 봉우리" },
                new StackSpec { Pos = new Vector3(-172f, 0f, 118f), Radius = 16f, Height = 52f, Pines = true, Name = "석회암 봉우리" },
            },
            Islets = new[] { new IsletSpec { Center = new Vector3(42f, 0f, 128f), Radius = 22f, Height = 26f, Walkable = true, Trees = true, Name = "티톱섬" } },
            Features = CoastFeature.Karst | CoastFeature.Junks | CoastFeature.FloatingVillage | CoastFeature.Mountains,
            Grid = new[,]
            {
                { S, S, W, S, S, S, S, S },
                { R, S, W, D, S, W, S, S },
                { R, D, D, D, D, D, D, R },
                { D, D, R, D, R, D, D, R },
                { R, D, D, D, D, D, D, D },
                { D, R, D, D, D, D, R, D },
                { D, R, R, D, D, D, R, R },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(4f, 1.1f, -47f), ShopPos = new Vector3(-16f, 1f, -52f),
            ShopRoof = new Color(0.8f, 0.3f, 0.18f),
            ShopOwner = Local("바이짜이 낚시점 투 사장", "바이짜이 낚시점"),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("응우옌 지부장", "한국 낚시 조합 하롱베이 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(-41f, 2f, 0f, Local("논라 쓴 아주머니", "수상 마을 어부", true), NpcAngler.Role.Angler, y: 1f),
                Npc(-90f, -46f, 20f, Local("쯔엉 아저씨", "GT 낚시 가이드", false), NpcAngler.Role.Talker),
                Npc(60f, -43f, 0f, Local("한국인 여행객", "패키지 투어 중", true), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("뚜언쩌우 선착장", new Vector3(-30f, 6.5f, -60f), SignBlue), ("HA LONG BAY", new Vector3(30f, 7f, -62f), SignGold) },
            FarIslands = new[] { new Vector4(-260f, 330f, 60f, 40f), new Vector4(300f, 380f, 70f, 46f) },
            Schools = () => new List<FishSchool>
            {
                School("바라문디 떼", 1, 0, 2, new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, "barramundi", "mangrovejack"),
                School("맹그로브잭", 5, 1, 3, EveNight, "mangrovejack", "orangegrouper"),
                School("얼룩바리 굴", 6, 2, 5, MornDay, "orangegrouper"),
                School("꼬치고기 떼", 3, 3, 6, MornDay, "barracuda", "gianttrevally"),
                // GT: 새벽 F5, 저녁 C7 (봉우리 사이 물살)
                Special("GT 어군", "gianttrevally", (TimeBand.Dawn, 5, 4), (TimeBand.Evening, 2, 6)),
            },
        };

        /// <summary>산토리니 (Lv.28): 이아 절벽 아래 암모우디 항구. 절벽 위 하얀 마을·파란 돔·풍차, 바다 건너 칼데라 테, 가운데 검은 화산섬. 칼데라라 물이 아주 깊다.</summary>
        public static CoastSpec Santorini() => new CoastSpec
        {
            Id = "santorini", MapName = "산토리니 · 에게해", ShortName = "산토리니", ShopName = "암모우디 낚시점", UnionContact = "암모우디 항구의 야니스 지부장",
            LandName = "암모우디 항구", SeaName = "칼데라 바다", Seed = 34, DarkRock = true,
            WaterDeep = new Color(0.01f, 0.1f, 0.3f), WaterShallow = new Color(0.04f, 0.38f, 0.55f),
            Shores = new[] { new ShoreSeg(-999f, -50f, ShoreKind.Rock), new ShoreSeg(-50f, 42f, ShoreKind.Quay), new ShoreSeg(42f, 999f, ShoreKind.Rock) },
            NearDepth = 7f, DepthSlope = 0.12f, FlatWidth = 13f, CliffHeight = 46f, CliffWidth = 34f, RoadOffset = 0f, VillageDensity = 0.35f,
            Houses = HouseStyle.Cyclades, TreeKind = TreeStyle.Mediterranean,
            Breakwaters = new[] { new BreakwaterSpec { Name = "암모우디 방파제", Stem = new Rect(36f, -40f, 6f, 30f), Arm = new Rect(12f, -14f, 28f, 5f), Light = new Color(0.95f, 0.95f, 0.93f) } },
            Islets = new[] { new IsletSpec { Center = new Vector3(-84f, 0f, 14f), Radius = 9f, Height = 5f, Walkable = true, Name = "성 니콜라오스 바위섬" } },
            Stacks = new[] { new StackSpec { Pos = new Vector3(104f, 0f, 58f), Radius = 5f, Height = 14f, Name = "검은 바위기둥" } },
            Features = CoastFeature.Caldera | CoastFeature.Windmills,
            Grid = new[,]
            {
                { R, R, S, S, S, R, R, R },
                { R, R, D, D, D, D, R, R },
                { D, D, D, D, D, D, D, R },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            },
            Spawn = new Vector3(8f, 1.1f, -45f), ShopPos = new Vector3(-20f, 1f, -47f),
            ShopRoof = new Color(0.1f, 0.35f, 0.72f),
            ShopOwner = Local("암모우디 낚시점 스타브로스", "암모우디 낚시점"),
            Npcs = new[]
            {
                Npc(24f, -46f, 200f, Union("야니스 지부장", "한국 낚시 조합 산토리니 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(38f, -18f, 0f, Local("마리아 할머니", "문어 말리는 어부", true), NpcAngler.Role.Angler, y: 1f),
                Npc(-36f, -46f, 30f, Local("요르고스", "타베르나 주인", false), NpcAngler.Role.Talker),
                Npc(-84f, 14f, 0f, Local("다이빙하는 여행객", "바위섬에서 뛰어든다", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("AMMOUDI", new Vector3(10f, 6.5f, -56f), SignBlue), ("OIA · 이아 마을", new Vector3(-60f, 52f, -96f), SignGold) },
            FarIslands = new[] { new Vector4(380f, 420f, 160f, 50f) },
            Schools = () => new List<FishSchool>
            {
                School("귀족도미 떼", 2, 0, 2, DayLong, "giltheadbream", "europeanseabass"),
                School("유럽농어", 5, 0, 2, DawnEve, "europeanseabass", "giltheadbream"),
                School("곰치 바위", 0, 1, 3, EveNight, "moray"),
                School("유럽대구", 4, 3, 6, EveNight, "hake"),
                // 참다랑어: 새벽 D7, 아침 G8 (먼바다, 배로)
                Special("참다랑어 떼", "bluefintuna", (TimeBand.Dawn, 3, 6), (TimeBand.Morning, 6, 7)),
            },
        };

        /// <summary>나일강 하류 (Lv.31): 카이로 남쪽 강변. 맞은편 둑까지 180m 남짓, 야자수 강가와 흙벽돌 마을, 펠루카, 사막 너머 기자 피라미드. 민물.</summary>
        public static CoastSpec Nile() => new CoastSpec
        {
            Id = "nile", MapName = "나일강 하류 · 이집트", ShortName = "나일강", ShopName = "나일 강변 낚시점", UnionContact = "강변 선착장의 하산 지부장",
            LandName = "강변 마을", SeaName = "나일강", Seed = 37, River = true, FarShoreZ = 140f, SandLand = true,
            WaterDeep = new Color(0.08f, 0.19f, 0.15f), WaterShallow = new Color(0.27f, 0.33f, 0.21f),
            Shores = new[] { new ShoreSeg(-999f, -60f, ShoreKind.Beach), new ShoreSeg(-60f, 50f, ShoreKind.Quay), new ShoreSeg(50f, 999f, ShoreKind.Beach) },
            NearDepth = 2.5f, DepthSlope = 0.035f, FlatWidth = 42f, CliffHeight = 3f, CliffWidth = 40f, RoadOffset = 16f, VillageDensity = 0.9f,
            Houses = HouseStyle.Egypt, TreeKind = TreeStyle.Palm,
            Platforms = new[] { new PlatformSpec { Area = new Rect(30f, -41f, 5f, 26f), Kind = PlatformKind.Pier, Name = "펠루카 선착장" } },
            Islets = new[] { new IsletSpec { Center = new Vector3(-104f, 0f, 58f), Radius = 24f, Height = 2.5f, Walkable = true, Trees = true, Name = "강 가운데 섬" } },
            Features = CoastFeature.Feluccas | CoastFeature.Pyramids,
            Grid = new[,]
            {
                { W, S, S, S, S, S, W, W },
                { W, S, D, D, D, D, S, W },
                { S, D, D, D, D, D, D, S },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, W },
                { W, S, W, S, W, S, W, W },
                { W, W, W, W, W, W, W, W },
                { W, W, W, W, W, W, W, W },
            },
            Spawn = new Vector3(6f, 1.1f, -47f), ShopPos = new Vector3(-18f, 1f, -52f),
            ShopRoof = new Color(0.25f, 0.5f, 0.6f),
            ShopOwner = Local("나일 강변 낚시점 마흐무드", "나일 강변 낚시점", false),
            Npcs = new[]
            {
                Npc(22f, -47f, 200f, Union("하산 지부장", "한국 낚시 조합 이집트 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(32f, -22f, 0f, Local("펠루카 사공", "돛 하나로 강을 건넌다", false), NpcAngler.Role.Angler, y: 1f),
                Npc(-50f, -46f, 10f, Local("아마드 할아버지", "나일농어 60년", false), NpcAngler.Role.Angler),
                Npc(70f, -44f, 340f, Local("고고학 유학생", "주말엔 낚시", true), NpcAngler.Role.Talker),
            },
            Signs = new[] { ("나일강 · NILE", new Vector3(6f, 6.5f, -62f), SignGold) },
            Schools = () => new List<FishSchool>
            {
                School("틸라피아 떼", 2, 0, 2, DayLong, "tilapia"),
                School("아프리카메기", 5, 1, 3, EveNight, "africancatfish", "tilapia"),
                School("타이거피시 떼", 3, 2, 4, MornDay, "tigerfish", "tilapia"),
                // 나일농어: 새벽 B4, 저녁 G5 (강 가운데 깊은 골)
                Special("나일농어 어군", "nileperch", (TimeBand.Dawn, 1, 3), (TimeBand.Evening, 6, 4)),
            },
        };
    }
}
