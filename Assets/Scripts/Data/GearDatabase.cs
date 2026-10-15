using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fishing.Data
{
    public enum GearSlot { Rod, Reel, Line, Boat }

    /// <summary>음식: 즉시 회복(Instant) + 분당 회복(PerMinute × Minutes, 게임 분).</summary>
    public sealed class FoodDef
    {
        public string Id, Name, Desc;
        public int Price;
        public float Instant;
        public float PerMinute;
        public float Minutes;
        public string EffectText => (Instant > 0f ? $"즉시 +{Instant:0}" : "") + (Instant > 0f && PerMinute > 0f ? " · " : "") +
                                    (PerMinute > 0f ? $"{Minutes / 60f:0.#}시간 동안 +{PerMinute * Minutes:0}" : "");
    }

    public sealed class GearDef
    {
        public string Id;
        public string Name;
        public GearSlot Slot;
        public int Price;
        public int Level;
        public string Desc;
        /// <summary>상점에서 팔지 않는다 (대회 보상 장비).</summary>
        public bool NotSold;

        // 낚싯대
        public float Power = 1f;      // 데미지 배율
        public float Flex = 0.5f;     // 0 = 경조(빳빳), 1 = 연조(낭창)
        public float CastRange = 18f; // m

        // 릴
        public float ReelSpeed = 3f;  // m/s
        public float Capacity = 60f;  // 감을 수 있는 줄 길이 m

        // 줄
        public float Strength = 1f;   // 끊어지는 장력 배율

        // 배
        public float BoatSpeed = 6f;      // 최고 속도 m/s
        public float FuelCapacity = 20f;  // 연료통 L
        public float FuelPerKm = 10f;     // 1km 달릴 때 드는 연료 L
        /// <summary>파도에 덜 흔들리는 정도 0~1 (고무보트 0.3 ~ 크루저 0.9).</summary>
        public float Stability = 0.5f;
        /// <summary>돛단배: 연료 없이 바람으로 달린다 (바람을 거스르면 느리다).</summary>
        public bool Sail;
        /// <summary>먼바다(파도 높은 곳)에 나갈 수 있는지.</summary>
        public bool OpenSea = true;

        static string Stars(float v) => new string('★', Mathf.Clamp(Mathf.RoundToInt(v * 5f), 1, 5));

        public string StatLine => Slot switch
        {
            GearSlot.Rod => $"힘 {Power:0.0} · {(Flex >= 0.6f ? "연조" : Flex <= 0.35f ? "경조" : "중경조")} · 비거리 {CastRange:0}m",
            GearSlot.Reel => $"감기 {ReelSpeed:0.0}m/s · 권사량 {Capacity:0}m",
            GearSlot.Line => $"강도 {Strength:0.0}",
            GearSlot.Boat => Sail
                ? $"최고 {BoatSpeed * 3.6f:0}km/h (바람) · 연료 없음 · 안정성 {Stars(Stability)}"
                : $"최고 {BoatSpeed * 3.6f:0}km/h · 연료통 {FuelCapacity:0}L · 연비 {1000f / FuelPerKm:0}m/L · 안정성 {Stars(Stability)}" + (OpenSea ? "" : " · 먼바다 불가"),
            _ => ""
        };
    }

    public sealed class BaitDef
    {
        public string Id;
        public string Name;
        public BaitKind Kind;
        public int PackSize;
        public int Price;
        /// <summary>파는 맵 (null이면 어디서나). 하단 퀵슬롯도 맵에 맞는 미끼만 보여 준다.</summary>
        public string[] Maps;
        public bool SoldIn(string mapId) => Maps == null || System.Array.IndexOf(Maps, mapId) >= 0;
        /// <summary>사용 레벨 (원작 "습득 레벨 미끼").</summary>
        public int Level = 1;
        /// <summary>입질 배율. 좋은 해체 미끼일수록 크고, 큰 고기도 잘 문다.</summary>
        public float Potency = 1f;
        /// <summary>상점에서 팔지 않는다 (해체로만 얻는 미끼).</summary>
        public bool NotSold;
    }

    /// <summary>
    /// 레벨 보상 (원작 2013 공략: Lv.4 Victory5 릴, Lv.5 강바람 낚시대, Lv.6 미끄러지지 않는 목장갑).
    /// Lv.5 고무보트는 GameBootstrap이 따로 준다. 음식·미끼 꾸러미는 솔로판에서 더한 것.
    /// </summary>
    public static class LevelRewards
    {
        public static readonly (int level, string id, int count, string text)[] All =
        {
            (2, "bait_worm", 20, "지렁이 20마리"),
            (3, "food_fishcake", 2, "살살녹는어묵 2개"),
            (4, "reel_victory5", 1, "Victory5 릴"),
            (5, "rod_gangbaram", 1, "강바람 낚시대"),
            (6, Fishing.Core.PlayerProfile.CottonGlovesId, 1, "미끄러지지 않는 목장갑 (파이팅 힘 +5% · 릴 속도 +5%)"),
            (8, "food_dosirak", 3, "바다 도시락 3개"),
            (10, "bait_lure", 10, "루어 10개"),
        };
    }

    public static class GearDatabase
    {
        public static readonly List<GearDef> Gear = new List<GearDef>
        {
            new GearDef { Id = "rod_basic", Name = "입문용 민물대", Slot = GearSlot.Rod, Price = 0, Level = 1,
                Power = 1.0f, Flex = 0.8f, CastRange = 18f, Desc = "낭창한 연조대. 장력 변화가 부드럽다." },
            new GearDef { Id = "rod_carbon", Name = "카본 경질대", Slot = GearSlot.Rod, Price = 3000, Level = 4,
                Power = 1.4f, Flex = 0.3f, CastRange = 22f, Desc = "빳빳해서 힘을 그대로 전달하지만 장력이 튄다." },
            new GearDef { Id = "rod_bamboo", Name = "명품 대나무 연질대", Slot = GearSlot.Rod, Price = 12000, Level = 8,
                Power = 1.5f, Flex = 0.75f, CastRange = 20f, Desc = "손맛의 정점. 대물의 돌진도 부드럽게 받아낸다." },
            new GearDef { Id = "rod_fly", Name = "플라이 낚싯대", Slot = GearSlot.Rod, Price = 9000, Level = 6,
                Power = 0.9f, Flex = 0.95f, CastRange = 16f, Desc = "가볍고 낭창한 플라이대. 플라이(털바늘)를 흘리면 입질이 1.3배 빠르다." },
            new GearDef { Id = "rod_heavy", Name = "헤비 원투대", Slot = GearSlot.Rod, Price = 25000, Level = 12,
                Power = 2.0f, Flex = 0.35f, CastRange = 32f, Desc = "멀리, 강하게. 대물 전용." },
            // 해외 거점용 (원작: 동남아 업데이트마다 "채비 10여 종" 추가)
            new GearDef { Id = "rod_gt", Name = "GT 파핑대", Slot = GearSlot.Rod, Price = 60000, Level = 25,
                Power = 2.2f, Flex = 0.3f, CastRange = 34f, Desc = "열대 바다 대물용 초경질 루어대. 무거운 파핑 루어를 멀리 던진다." },
            new GearDef { Id = "rod_nile", Name = "나일 원투대", Slot = GearSlot.Rod, Price = 90000, Level = 31,
                Power = 2.4f, Flex = 0.45f, CastRange = 33f, Desc = "2m 나일농어와 힘겨루기하는 굵은 원투대. 경질이지만 끝이 잘 휜다." },
            // 원작 대회 1~3위 보상 「거거 낚시대」 (상점에서 팔지 않음)
            new GearDef { Id = "rod_gege", Name = "거거 낚시대", Slot = GearSlot.Rod, Price = 0, Level = 10, NotSold = true,
                Power = 1.85f, Flex = 0.6f, CastRange = 28f, Desc = "조합 낚시 대회 우승 상품. 가볍고 질겨서 손맛과 힘을 모두 잡았다." },

            // 원작 레벨 보상 (2013 공략: Lv.5 「강바람 낚시대」, Lv.4 「Victory5 릴」). 상점에서 팔지 않는다
            new GearDef { Id = "rod_gangbaram", Name = "강바람 낚시대", Slot = GearSlot.Rod, Price = 0, Level = 5, NotSold = true,
                Power = 1.25f, Flex = 0.65f, CastRange = 21f, Desc = "Lv.5 달성 보상. 강바람에도 흔들리지 않는 중경질대. 카본대보다 장력이 덜 튄다." },

            // 빙고 3줄 보상 (원작: 빙고 보상 「황금거거」)
            new GearDef { Id = "rod_goldgege", Name = "황금거거 낚싯대", Slot = GearSlot.Rod, Price = 0, Level = 10, NotSold = true,
                Power = 2.0f, Flex = 0.62f, CastRange = 29f, Desc = "빙고 3줄 보상. 거거 낚시대를 금빛으로 다시 칠하고 블랭크를 보강했다." },
            // 괴물 어종 대물 코인으로 바꾸는 이벤트 채비 (원작: 이벤트 코인 → 풍림화산 채비)
            new GearDef { Id = "rod_pungrim", Name = "풍림화산 낚싯대", Slot = GearSlot.Rod, Price = 0, Level = 12, NotSold = true,
                Power = 2.3f, Flex = 0.55f, CastRange = 31f, Desc = "대물 코인 교환품. 검은 옻칠에 붉은 띠와 금박 — 바람처럼 빠르고 산처럼 버틴다." },

            new GearDef { Id = "reel_basic", Name = "기본 스피닝 릴", Slot = GearSlot.Reel, Price = 0, Level = 1,
                ReelSpeed = 3f, Capacity = 60f, Desc = "무난한 입문용 릴." },
            new GearDef { Id = "reel_victory5", Name = "Victory5 릴", Slot = GearSlot.Reel, Price = 0, Level = 4, NotSold = true,
                ReelSpeed = 3.7f, Capacity = 80f, Desc = "Lv.4 달성 보상. 가볍고 잘 감기는 입문 다음 단계 릴." },
            new GearDef { Id = "reel_power", Name = "파워 릴", Slot = GearSlot.Reel, Price = 5000, Level = 5,
                ReelSpeed = 4.2f, Capacity = 90f, Desc = "감는 힘이 좋다." },
            new GearDef { Id = "reel_hispeed", Name = "하이기어 릴", Slot = GearSlot.Reel, Price = 15000, Level = 10,
                ReelSpeed = 5.5f, Capacity = 120f, Desc = "빠르게 거리를 좁힌다." },
            new GearDef { Id = "reel_jigging", Name = "대형 지깅 릴", Slot = GearSlot.Reel, Price = 70000, Level = 25,
                ReelSpeed = 6.2f, Capacity = 200f, Desc = "참다랑어·GT용 대형 릴. 권사량 200m, 드랙이 묵직하다." },
            // 원작 대회 보상 「나팔고둥」 (나팔고둥 껍데기 같은 무늬의 릴)
            new GearDef { Id = "reel_napal", Name = "나팔고둥 릴", Slot = GearSlot.Reel, Price = 0, Level = 10, NotSold = true,
                ReelSpeed = 5.0f, Capacity = 140f, Desc = "조합 낚시 대회 입상 상품. 권사량이 넉넉하고 드랙이 부드럽다." },

            new GearDef { Id = "reel_pungrim", Name = "풍림화산 릴", Slot = GearSlot.Reel, Price = 0, Level = 12, NotSold = true,
                ReelSpeed = 6.0f, Capacity = 160f, Desc = "대물 코인 교환품. 옻칠한 검은 몸통에 붉은 핸들 — 괴물 어종도 끌어낸다." },

            new GearDef { Id = "line_nylon", Name = "나일론 2호", Slot = GearSlot.Line, Price = 0, Level = 1,
                Strength = 1.0f, Desc = "기본 원줄." },
            new GearDef { Id = "line_carbon", Name = "카본 3호", Slot = GearSlot.Line, Price = 1500, Level = 3,
                Strength = 1.3f, Desc = "잘 끊어지지 않는다." },
            new GearDef { Id = "line_pe", Name = "강사 줄 (PE 4호)", Slot = GearSlot.Line, Price = 6000, Level = 8,
                Strength = 1.7f, Desc = "대물과의 힘싸움을 버틴다." },
            new GearDef { Id = "line_pe8", Name = "PE 8호 + 쇼크리더", Slot = GearSlot.Line, Price = 25000, Level = 25,
                Strength = 2.2f, Desc = "GT·참다랑어·나일농어용. 이빨과 바위에 쓸려도 버티는 굵은 목줄을 달았다." },

            new GearDef { Id = "boat_rubber", Name = "고무보트", Slot = GearSlot.Boat, Price = 4000, Level = 3,
                BoatSpeed = 6f, FuelCapacity = 20f, FuelPerKm = 12f, Stability = 0.3f, OpenSea = false,
                Desc = "물가에서 꺼내 타는 2인용 고무보트. 느리지만 강 건너편 깊은 칸까지 갈 수 있다. 파도가 높은 먼바다에는 못 나간다." },
            new GearDef { Id = "boat_sail", Name = "돛단배", Slot = GearSlot.Boat, Price = 18000, Level = 8,
                BoatSpeed = 9f, FuelCapacity = 0f, FuelPerKm = 0f, Stability = 0.5f, Sail = true,
                Desc = "바람을 받아 달리는 작은 요트. 연료가 필요 없지만 바람을 거슬러서는 느리다 (옆바람이 가장 빠르다)." },
            new GearDef { Id = "boat_speed", Name = "고속보트", Slot = GearSlot.Boat, Price = 30000, Level = 10,
                BoatSpeed = 14f, FuelCapacity = 40f, FuelPerKm = 20f, Stability = 0.55f, Desc = "물보라를 가르며 달리는 FRP 고속보트." },
            // 출석 7일 보상 (원작: 출석 이벤트 특수 배 「블랙스피어」)
            new GearDef { Id = "boat_blackspear", Name = "블랙스피어", Slot = GearSlot.Boat, Price = 0, Level = 5, NotSold = true,
                BoatSpeed = 15f, FuelCapacity = 45f, FuelPerKm = 16f, Stability = 0.6f,
                Desc = "출석 7일 보상. 창끝처럼 날렵한 검은 고속정. 고속보트보다 빠르고 연료를 덜 먹는다." },
            new GearDef { Id = "boat_cruiser", Name = "낚시 크루저", Slot = GearSlot.Boat, Price = 60000, Level = 14,
                BoatSpeed = 11f, FuelCapacity = 80f, FuelPerKm = 25f, Stability = 0.9f,
                Desc = "선장과 동행 꾼이 함께 타는 큰 낚싯배 (원작: 최대 10명 승선). 파도에도 거의 흔들리지 않아 먼바다 대물 포인트까지 간다." },
        };

        public static readonly List<BaitDef> Baits = new List<BaitDef>
        {
            new BaitDef { Id = "bait_worm", Name = "지렁이", Kind = BaitKind.Worm, PackSize = 30, Price = 300, Maps = FishSpecies.FreshMaps },
            new BaitDef { Id = "bait_paste", Name = "떡밥", Kind = BaitKind.Paste, PackSize = 30, Price = 400, Maps = FishSpecies.FreshMaps },
            new BaitDef { Id = "bait_corn", Name = "옥수수", Kind = BaitKind.Corn, PackSize = 30, Price = 300, Maps = FishSpecies.FreshMaps },
            new BaitDef { Id = "bait_krill", Name = "크릴", Kind = BaitKind.Krill, PackSize = 30, Price = 400, Maps = FishSpecies.SeaMaps },
            new BaitDef { Id = "bait_seaworm", Name = "청갯지렁이", Kind = BaitKind.SeaWorm, PackSize = 30, Price = 500, Maps = FishSpecies.SeaMaps },
            new BaitDef { Id = "bait_squid", Name = "오징어살", Kind = BaitKind.Squid, PackSize = 20, Price = 600, Maps = FishSpecies.SeaMaps },
            new BaitDef { Id = "bait_shrimp", Name = "새우", Kind = BaitKind.Shrimp, PackSize = 20, Price = 600 },
            new BaitDef { Id = "bait_minnow", Name = "생미끼", Kind = BaitKind.Minnow, PackSize = 10, Price = 800 },
            new BaitDef { Id = "bait_lure", Name = "스푼 루어", Kind = BaitKind.Lure, PackSize = 10, Price = 1500 },
            // 서천 앞바다: 주꾸미·갑오징어용 새우 모양 가짜 미끼 (원작풍)
            new BaitDef { Id = "bait_egi", Name = "에기", Kind = BaitKind.Egi, PackSize = 5, Price = 1800, Maps = FishSpecies.EgiMaps },
            // 한강: 수면에 띄워 흘리는 제물낚시 털바늘. 피라미·끄리·강준치·블루길
            new BaitDef { Id = "bait_fly", Name = "플라이", Kind = BaitKind.Fly, PackSize = 10, Price = 1200, Maps = FishSpecies.FreshMaps, Level = 4 },

            // 해체 미끼 (원작풍 이름: 원작에도 "촉촉한이리" 같은 형용사+부위 미끼가 있었다). 살림망에서 고기를 해체해 얻는다
            new BaitDef { Id = "bait_cut1", Name = "어분덩이", Kind = BaitKind.CutBait, NotSold = true, Level = 1, Potency = 1.0f },
            new BaitDef { Id = "bait_cut2", Name = "악취나는살점", Kind = BaitKind.CutBait, NotSold = true, Level = 5, Potency = 1.15f },
            new BaitDef { Id = "bait_cut3", Name = "덩어리진생선내장", Kind = BaitKind.CutBait, NotSold = true, Level = 8, Potency = 1.3f },
            new BaitDef { Id = "bait_cut4", Name = "촉촉한이리", Kind = BaitKind.CutBait, NotSold = true, Level = 12, Potency = 1.5f },
            new BaitDef { Id = "bait_cut5", Name = "비릿한볼살", Kind = BaitKind.CutBait, NotSold = true, Level = 16, Potency = 1.7f },
        };

        /// <summary>어종 등급(★1~5)에 맞는 해체 미끼.</summary>
        public static BaitDef CutBaitFor(int rarity) => GetBait("bait_cut" + UnityEngine.Mathf.Clamp(rarity, 1, 5));

        public const string KimbapId = "food_kimbap";

        /// <summary>
        /// 음식 (원작: 상점 F12에서 산다. 즉시 회복형과 분당 회복형(10분간 회복량 증가)이 있었다).
        /// 분당 회복형은 게임 시간(1초 = 2분) 기준이라 솔로판은 1시간(현실 30초)으로 늘렸다.
        /// "살살녹는어묵"은 원작 유저 영상 제목에 나온 이름.
        /// </summary>
        public static readonly List<FoodDef> Foods = new List<FoodDef>
        {
            new FoodDef { Id = KimbapId, Name = "김밥", Price = 500, Instant = 40f, Desc = "출조의 든든한 동반자. 바로 체력 +40." },
            new FoodDef { Id = "food_ramen", Name = "컵라면", Price = 250, Instant = 20f, Desc = "물만 부으면 끝. 바로 체력 +20." },
            new FoodDef { Id = "food_fishcake", Name = "살살녹는어묵", Price = 400, PerMinute = 1f, Minutes = 60f, Desc = "뜨끈한 어묵 국물. 게임 1시간 동안 분마다 체력 +1 (모두 +60)." },
            new FoodDef { Id = "food_coffee", Name = "보온병 믹스커피", Price = 300, PerMinute = 0.5f, Minutes = 120f, Desc = "새벽 출조의 필수품. 게임 2시간 동안 분마다 체력 +0.5 (모두 +60)." },
            new FoodDef { Id = "food_dosirak", Name = "바다 도시락", Price = 900, Instant = 30f, PerMinute = 0.6f, Minutes = 60f, Desc = "회와 매운탕이 든 도시락. 바로 +30, 게임 1시간 동안 분마다 +0.6." },
        };

        public static FoodDef GetFood(string id) => Foods.FirstOrDefault(f => f.Id == id);
        public const int FuelPricePerLiter = 30;
        public const int KimbapPrice = 500;
        public const float KimbapStamina = 40f;

        static Dictionary<string, GearDef> _gear;
        static Dictionary<string, BaitDef> _bait;

        public static GearDef GetGear(string id)
        {
            _gear ??= Gear.ToDictionary(g => g.Id);
            return id != null && _gear.TryGetValue(id, out var g) ? g : null;
        }

        public static BaitDef GetBait(string id)
        {
            _bait ??= Baits.ToDictionary(b => b.Id);
            return id != null && _bait.TryGetValue(id, out var b) ? b : null;
        }
    }
}
