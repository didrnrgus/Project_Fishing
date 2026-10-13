using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    public enum GearSlot { Rod, Reel, Line, Boat }

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

        public string StatLine => Slot switch
        {
            GearSlot.Rod => $"힘 {Power:0.0} · {(Flex >= 0.6f ? "연조" : Flex <= 0.35f ? "경조" : "중경조")} · 비거리 {CastRange:0}m",
            GearSlot.Reel => $"감기 {ReelSpeed:0.0}m/s · 권사량 {Capacity:0}m",
            GearSlot.Line => $"강도 {Strength:0.0}",
            GearSlot.Boat => $"최고 {BoatSpeed * 3.6f:0}km/h · 연료통 {FuelCapacity:0}L · 연비 {1000f / FuelPerKm:0}m/L",
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
            new GearDef { Id = "rod_heavy", Name = "헤비 원투대", Slot = GearSlot.Rod, Price = 25000, Level = 12,
                Power = 2.0f, Flex = 0.35f, CastRange = 32f, Desc = "멀리, 강하게. 대물 전용." },
            // 원작 대회 1~3위 보상 「거거 낚시대」 (상점에서 팔지 않음)
            new GearDef { Id = "rod_gege", Name = "거거 낚시대", Slot = GearSlot.Rod, Price = 0, Level = 10, NotSold = true,
                Power = 1.85f, Flex = 0.6f, CastRange = 28f, Desc = "조합 낚시 대회 우승 상품. 가볍고 질겨서 손맛과 힘을 모두 잡았다." },

            new GearDef { Id = "reel_basic", Name = "기본 스피닝 릴", Slot = GearSlot.Reel, Price = 0, Level = 1,
                ReelSpeed = 3f, Capacity = 60f, Desc = "무난한 입문용 릴." },
            new GearDef { Id = "reel_power", Name = "파워 릴", Slot = GearSlot.Reel, Price = 5000, Level = 5,
                ReelSpeed = 4.2f, Capacity = 90f, Desc = "감는 힘이 좋다." },
            new GearDef { Id = "reel_hispeed", Name = "하이기어 릴", Slot = GearSlot.Reel, Price = 15000, Level = 10,
                ReelSpeed = 5.5f, Capacity = 120f, Desc = "빠르게 거리를 좁힌다." },
            // 원작 대회 보상 「나팔고둥」 (나팔고둥 껍데기 같은 무늬의 릴)
            new GearDef { Id = "reel_napal", Name = "나팔고둥 릴", Slot = GearSlot.Reel, Price = 0, Level = 10, NotSold = true,
                ReelSpeed = 5.0f, Capacity = 140f, Desc = "조합 낚시 대회 입상 상품. 권사량이 넉넉하고 드랙이 부드럽다." },

            new GearDef { Id = "line_nylon", Name = "나일론 2호", Slot = GearSlot.Line, Price = 0, Level = 1,
                Strength = 1.0f, Desc = "기본 원줄." },
            new GearDef { Id = "line_carbon", Name = "카본 3호", Slot = GearSlot.Line, Price = 1500, Level = 3,
                Strength = 1.3f, Desc = "잘 끊어지지 않는다." },
            new GearDef { Id = "line_pe", Name = "강사 줄 (PE 4호)", Slot = GearSlot.Line, Price = 6000, Level = 8,
                Strength = 1.7f, Desc = "대물과의 힘싸움을 버틴다." },

            new GearDef { Id = "boat_rubber", Name = "고무보트", Slot = GearSlot.Boat, Price = 4000, Level = 3,
                BoatSpeed = 6f, FuelCapacity = 20f, FuelPerKm = 12f, Desc = "물가에서 꺼내 타는 2인용 고무보트. 느리지만 강 건너편 깊은 칸까지 갈 수 있다." },
            new GearDef { Id = "boat_speed", Name = "고속보트", Slot = GearSlot.Boat, Price = 30000, Level = 10,
                BoatSpeed = 14f, FuelCapacity = 40f, FuelPerKm = 20f, Desc = "물보라를 가르며 달리는 FRP 고속보트." },
        };

        public static readonly List<BaitDef> Baits = new List<BaitDef>
        {
            new BaitDef { Id = "bait_worm", Name = "지렁이", Kind = BaitKind.Worm, PackSize = 30, Price = 300, Maps = FishSpecies.HanOnly },
            new BaitDef { Id = "bait_paste", Name = "떡밥", Kind = BaitKind.Paste, PackSize = 30, Price = 400, Maps = FishSpecies.HanOnly },
            new BaitDef { Id = "bait_corn", Name = "옥수수", Kind = BaitKind.Corn, PackSize = 30, Price = 300, Maps = FishSpecies.HanOnly },
            new BaitDef { Id = "bait_krill", Name = "크릴", Kind = BaitKind.Krill, PackSize = 30, Price = 400, Maps = FishSpecies.SeaOnly },
            new BaitDef { Id = "bait_seaworm", Name = "청갯지렁이", Kind = BaitKind.SeaWorm, PackSize = 30, Price = 500, Maps = FishSpecies.SeaOnly },
            new BaitDef { Id = "bait_squid", Name = "오징어살", Kind = BaitKind.Squid, PackSize = 20, Price = 600, Maps = FishSpecies.SeaOnly },
            new BaitDef { Id = "bait_shrimp", Name = "새우", Kind = BaitKind.Shrimp, PackSize = 20, Price = 600 },
            new BaitDef { Id = "bait_minnow", Name = "생미끼", Kind = BaitKind.Minnow, PackSize = 10, Price = 800 },
            new BaitDef { Id = "bait_lure", Name = "스푼 루어", Kind = BaitKind.Lure, PackSize = 10, Price = 1500 },

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
