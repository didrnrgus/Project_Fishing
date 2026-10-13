using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    public enum GearSlot { Rod, Reel, Line }

    public sealed class GearDef
    {
        public string Id;
        public string Name;
        public GearSlot Slot;
        public int Price;
        public int Level;
        public string Desc;

        // 낚싯대
        public float Power = 1f;      // 데미지 배율
        public float Flex = 0.5f;     // 0 = 경조(빳빳), 1 = 연조(낭창)
        public float CastRange = 18f; // m

        // 릴
        public float ReelSpeed = 3f;  // m/s
        public float Capacity = 60f;  // 감을 수 있는 줄 길이 m

        // 줄
        public float Strength = 1f;   // 끊어지는 장력 배율

        public string StatLine => Slot switch
        {
            GearSlot.Rod => $"힘 {Power:0.0} · {(Flex >= 0.6f ? "연조" : Flex <= 0.35f ? "경조" : "중경조")} · 비거리 {CastRange:0}m",
            GearSlot.Reel => $"감기 {ReelSpeed:0.0}m/s · 권사량 {Capacity:0}m",
            GearSlot.Line => $"강도 {Strength:0.0}",
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

            new GearDef { Id = "reel_basic", Name = "기본 스피닝 릴", Slot = GearSlot.Reel, Price = 0, Level = 1,
                ReelSpeed = 3f, Capacity = 60f, Desc = "무난한 입문용 릴." },
            new GearDef { Id = "reel_power", Name = "파워 릴", Slot = GearSlot.Reel, Price = 5000, Level = 5,
                ReelSpeed = 4.2f, Capacity = 90f, Desc = "감는 힘이 좋다." },
            new GearDef { Id = "reel_hispeed", Name = "하이기어 릴", Slot = GearSlot.Reel, Price = 15000, Level = 10,
                ReelSpeed = 5.5f, Capacity = 120f, Desc = "빠르게 거리를 좁힌다." },

            new GearDef { Id = "line_nylon", Name = "나일론 2호", Slot = GearSlot.Line, Price = 0, Level = 1,
                Strength = 1.0f, Desc = "기본 원줄." },
            new GearDef { Id = "line_carbon", Name = "카본 3호", Slot = GearSlot.Line, Price = 1500, Level = 3,
                Strength = 1.3f, Desc = "잘 끊어지지 않는다." },
            new GearDef { Id = "line_pe", Name = "강사 줄 (PE 4호)", Slot = GearSlot.Line, Price = 6000, Level = 8,
                Strength = 1.7f, Desc = "대물과의 힘싸움을 버틴다." },
        };

        public static readonly List<BaitDef> Baits = new List<BaitDef>
        {
            new BaitDef { Id = "bait_worm", Name = "지렁이", Kind = BaitKind.Worm, PackSize = 30, Price = 300 },
            new BaitDef { Id = "bait_paste", Name = "떡밥", Kind = BaitKind.Paste, PackSize = 30, Price = 400 },
            new BaitDef { Id = "bait_corn", Name = "옥수수", Kind = BaitKind.Corn, PackSize = 30, Price = 300 },
            new BaitDef { Id = "bait_shrimp", Name = "새우", Kind = BaitKind.Shrimp, PackSize = 20, Price = 600 },
            new BaitDef { Id = "bait_minnow", Name = "생미끼", Kind = BaitKind.Minnow, PackSize = 10, Price = 800 },
            new BaitDef { Id = "bait_lure", Name = "스푼 루어", Kind = BaitKind.Lure, PackSize = 10, Price = 1500 },
        };

        public const string KimbapId = "food_kimbap";
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
