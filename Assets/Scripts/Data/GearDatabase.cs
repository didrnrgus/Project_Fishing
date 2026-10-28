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
        /// <summary>
        /// 마을 교환 장비: 이 낚시터 마을 어부가 나팔고동(한국)·황금거거(해외)를 받고 내준다 (<see cref="VillageTrade"/>).
        /// 상점에서 안 파는 대신 같은 레벨 상점 장비보다 능력치가 좋다.
        /// </summary>
        public string TradeMap;
        public int TradeCost;
        /// <summary>낚싯대 블랭크·릴 몸통 / 링·테 색 (#rrggbb). 따로 정한 모양이 없는 장비만 쓴다 (TelescopicRod, build_icons.py와 같은 색).</summary>
        public string LookMain, LookAccent;

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

            // ---- 마을 교환 장비 (원작: 상점 골드로는 못 사고 마을 NPC에게 한국은 나팔고동, 해외는 황금거거로 바꾼다) ----
            // 낚시터마다 하나씩, 레벨 = 그 낚시터 입장 레벨. 같은 레벨 상점 장비보다 능력치가 한 단계 좋다 (VillageTrade).
            Trade("line_hangang", "한강 합사 3호", GearSlot.Line, 3, "hangang", 3, "#3a8fd0", "#2a3440",
                "한강 둔치 터줏대감이 쓰는 파란 합사. 카본 3호보다 질기다.", strength: 1.4f),
            Trade("rod_beodeul", "버들치 계류대", GearSlot.Rod, 3, "tancheon", 3, "#7aa86a", "#e8e0c8",
                "탄천지기 윤씨가 버드나무 색으로 칠한 가벼운 계류대. 입문용 민물대보다 힘이 있고 여울 잔챙이 손맛이 살아 있다.", power: 1.2f, flex: 0.85f, range: 19f),
            Trade("rod_dalaengi", "다랭이 갯바위대", GearSlot.Rod, 5, "honghyeon", 5, "#3d6b3a", "#d9c27a",
                "다랭이마을 어부가 계단 논 색으로 칠한 갯바위대. 중경조라 감성돔 처박기를 부드럽게 받는다.", power: 1.5f, flex: 0.55f, range: 23f),
            Trade("reel_hongwon", "홍원항 선상 릴", GearSlot.Reel, 6, "seocheon", 5, "#e8e8e8", "#c8302a",
                "홍원항 흰 등대·빨간 등대 색 릴. 주꾸미배 선장들이 쓰는 튼튼한 물건.", speed: 4.7f, capacity: 100f),
            Trade("line_haemu", "해무 PE 3호", GearSlot.Line, 9, "yeongheung", 6, "#c8d4dc", "#2a3440",
                "해무 낀 날에도 잘 보이는 은회색 합사. PE 4호보다 가늘고 질기다.", strength: 1.85f),
            Trade("reel_napal", "나팔고동 릴", GearSlot.Reel, 11, "taean", 8, null, null,
                "나팔고동 껍데기 무늬를 새긴 태안 어부의 릴. 권사량이 넉넉하고 드랙이 부드럽다.", speed: 5.8f, capacity: 150f),
            Trade("rod_chotdae", "촛대바위 원투대", GearSlot.Rod, 14, "samcheok", 10, "#1f3f6e", "#e0e4ea",
                "추암 촛대바위처럼 곧고 단단한 쪽빛 원투대. 깊은 동해 바닥까지 멀리 던진다.", power: 2.15f, flex: 0.45f, range: 33f),
            Trade("line_deungdae", "등대지기 PE 5호", GearSlot.Line, 16, "deungdae", 10, "#f0f0f0", "#d02828",
                "등대섬 이장님이 등대 색으로 물들인 흰·빨간 합사.", strength: 2.05f),
            Trade("reel_bucheae", "바다부채 릴", GearSlot.Reel, 18, "heonhwa", 12, "#2a7a8c", "#d86a9a",
                "바다부채길 물빛 몸통에 절벽 진달래색 테. 대문어도 바닥에서 떼어 낸다.", speed: 6.3f, capacity: 175f),
            Trade("rod_yokji", "욕지 참돔 연질대", GearSlot.Rod, 19, "yokji", 14, "#d8706a", "#e8d8a0",
                "참돔 빛깔의 연질대. 낭창하게 휘어 가두리 참돔의 질주를 받아 낸다.", power: 2.3f, flex: 0.72f, range: 30f),
            Trade("reel_halla", "한라 지깅 릴", GearSlot.Reel, 21, "jeju", 16, "#2a2a2c", "#e8a030",
                "현무암처럼 검은 몸통에 귤빛 테. 권사량 200m로 먼바다 다금바리도 끝까지 버틴다.", speed: 6.5f, capacity: 200f),
            Trade("line_myeongtae", "명태잡이 합사 6호", GearSlot.Line, 22, "cheongjin", 16, "#e8eef4", "#4a6a8a",
                "청진 어로반장이 겨울 명태잡이에 쓰던 눈빛 합사. 얼어도 굳지 않는다.", strength: 2.25f),
            Trade("rod_baekdo", "백도 대물 갯바위대", GearSlot.Rod, 24, "geomun", 20, "#f2f0ea", "#1a4c8c",
                "백도 바위처럼 흰 블랭크에 쪽빛 링. 거문도 대물꾼들이 돗돔을 노리는 갯바위대.", power: 2.35f, flex: 0.55f, range: 34f),

            Trade("rod_halong", "하롱 정크 루어대", GearSlot.Rod, 25, "halong", 5, "#2f8f7f", "#b04a2a",
                "에메랄드빛 블랭크에 정크선 돛 색 링. 석회암 봉우리 사이로 무거운 루어를 날린다.", power: 2.45f, flex: 0.4f, range: 35f),
            Trade("line_tonlesap", "톤레삽 PE 8호", GearSlot.Line, 26, "angkor", 5, "#8a7a4a", "#c09040",
                "사원 돌이끼 색 합사. 메콩 대형 메기의 이빨에도 잘 안 쓸린다.", strength: 2.3f),
            Trade("reel_honche", "혼째 지깅 릴", GearSlot.Reel, 27, "vinpearl", 6, "#f0e0b0", "#2aa0c0",
                "나트랑 모래빛 몸통에 바다색 테. 코비아·만새기를 빠르게 끌어올린다.", speed: 6.7f, capacity: 220f),
            Trade("reel_caldera", "칼데라 딥 릴", GearSlot.Reel, 28, "santorini", 7, "#f4f4f4", "#1f5fbf",
                "하얀 집과 파란 돔 색 심해 릴. 권사량 250m로 칼데라 깊은 바닥까지 닿는다.", speed: 6.8f, capacity: 250f),
            Trade("rod_mekong", "메콩 메기 원투대", GearSlot.Rod, 28, "donkhone", 7, "#6a4a2a", "#c8a050",
                "수상 가옥 티크 나무 색 연질 원투대. 여울의 대형 메기를 지치게 만든다.", power: 2.5f, flex: 0.7f, range: 32f),
            Trade("rod_goldgege", "황금거거 낚싯대", GearSlot.Rod, 30, "phuket", 9, null, null,
                "황금거거 껍데기처럼 금빛으로 칠한 거거 낚시대의 윗급. 손맛과 힘을 모두 잡았다.", power: 2.6f, flex: 0.6f, range: 35f),
            Trade("line_chania", "하니아 PE 10호", GearSlot.Line, 30, "crete", 8, "#e8a87a", "#3a6a8a",
                "베네치아 항구 파스텔 집 색 합사. 덴텍스·그루퍼가 바위로 처박아도 버틴다.", strength: 2.4f),
            Trade("reel_chao", "짜오프라야 릴", GearSlot.Reel, 31, "bangkok", 9, "#c8a030", "#c03030",
                "사원 금박 몸통에 붉은 테. 피라루쿠의 힘을 감아 이긴다.", speed: 7.0f, capacity: 240f),
            Trade("line_nile", "나일 쇼크리더 합사", GearSlot.Line, 31, "nile", 9, "#c8a873", "#26808c",
                "타이거피시 송곳니를 견디는 모래색 합사에 굵은 목줄을 달았다.", strength: 2.45f),
            Trade("rod_samui", "사무이 금불 루어대", GearSlot.Rod, 32, "kohsamui", 10, "#e0b040", "#f0f0f0",
                "바다 위 황금 대불 색 루어대. 와후의 질주에도 줄을 먼저 내주지 않는다.", power: 2.65f, flex: 0.45f, range: 36f),
            Trade("reel_langkawi", "랑카위 독수리 릴", GearSlot.Reel, 33, "langkawi", 11, "#7a3a1a", "#f0f0f0",
                "붉은 독수리 상 색 릴. 맹그로브 물길에서 GT를 단숨에 끌어낸다.", speed: 7.1f, capacity: 260f),
            Trade("rod_navagio", "나바지오 참치대", GearSlot.Rod, 33, "zakynthos", 12, "#3ab0c8", "#f4f0e0",
                "난파선 해변 물빛 경질대. 참다랑어·황새치를 힘으로 들어 올린다.", power: 2.7f, flex: 0.35f, range: 38f),
            Trade("line_merlion", "머라이언 PE 12호", GearSlot.Line, 35, "sentosa", 12, "#f0f0f0", "#d03030",
                "센토사 머라이언처럼 흰 몸에 빨간 표시 합사.", strength: 2.55f),
            Trade("reel_telok", "텔록 바텍 노을 릴", GearSlot.Reel, 36, "telokbatek", 14, "#e86a2a", "#6a2a7a",
                "어촌의 금빛 노을 색 릴. 권사량 280m.", speed: 7.3f, capacity: 280f),
            Trade("rod_duden", "듀덴 폭포 연질대", GearSlot.Rod, 36, "duden", 14, "#5aa0c8", "#e8e8e8",
                "폭포 물보라 색 연질대. 낭창하게 휘면서도 힘이 세다.", power: 2.8f, flex: 0.7f, range: 35f),
            Trade("line_tioman", "티오만 용뿔 합사", GearSlot.Line, 37, "tioman", 15, "#8a5ac8", "#2a8a4a",
                "용의 뿔 봉우리 아래 리조트 어부의 보랏빛 합사.", strength: 2.65f),
            Trade("rod_tenggol", "탱글 산호 대물대", GearSlot.Rod, 39, "tenggol", 18, "#e86a6a", "#f0d070",
                "산호빛 블랭크에 금빛 링. 도그투스 참치를 위한 마지막 낚싯대.", power: 2.95f, flex: 0.55f, range: 39f),
        };

        static GearDef Trade(string id, string name, GearSlot slot, int level, string map, int cost, string main, string accent, string desc,
            float power = 1f, float flex = 0.5f, float range = 18f, float speed = 3f, float capacity = 60f, float strength = 1f) =>
            new GearDef
            {
                Id = id, Name = name, Slot = slot, Level = level, NotSold = true, TradeMap = map, TradeCost = cost, LookMain = main, LookAccent = accent,
                Desc = desc, Power = power, Flex = flex, CastRange = range, ReelSpeed = speed, Capacity = capacity, Strength = strength,
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
        /// 분당 회복형은 게임 시간(1초 = 1분) 기준이라 솔로판은 1시간(현실 1분)으로 늘렸다.
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
