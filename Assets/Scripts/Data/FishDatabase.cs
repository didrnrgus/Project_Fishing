using System.Collections.Generic;
using System.Linq;

namespace Fishing.Data
{
    public sealed class FishSpecies
    {
        public string Id;
        public string Name;
        public string Latin;
        public string Temper;       // 도감에 표시되는 성질
        public float MinLen;        // cm
        public float MaxLen;        // cm
        public float TrophyLen;     // 이 길이 이상이면 대물
        public string TrophyName;
        public float WeightCoef;    // g = coef * len^3
        public int Rarity;          // 0 = 잡동사니, 1~5
        public Habitat[] Habitats;
        public BaitKind[] Baits;
        public TimeBand[] Times;
        public Weather[] Weathers;
        public float Power;         // 당기는 힘
        public float Speed;         // 좌우로 내달리는 속도
        public float Stamina;       // 체력 배율
        public float Aggression;    // 방향 전환/돌진 빈도
        public int Exp;
        public int Price;
        /// <summary>사는 맵 (null이면 모든 맵, 잡동사니용).</summary>
        public string[] Maps = HanOnly;
        /// <summary>true면 어군 안에서만 문다 (자이언트 그루퍼 같은 대물 퀘스트 고기).</summary>
        public bool SchoolOnly;
        /// <summary>먼바다 포인트(배로 나가는 맵 앞쪽 38%)에서만 문다: 수심 100m 넘는 곳에 사는 다금바리·돗돔.</summary>
        public bool FarSeaOnly;
        /// <summary>
        /// 괴물 어종 (원작 이벤트 몬스터: 괴물잉어·괴물가물치·폭군삼치·황새치). 낚으면 대물 코인을 주고,
        /// 코인은 조합 교환소에서 풍림화산 채비로 바꾼다. 히든·포식자 어군에는 끼지 않는다.
        /// </summary>
        public int BossCoins;
        public bool Boss => BossCoins > 0;
        /// <summary>제철 (null이면 사계절). 제철이면 잘 물고 아니면 덜 문다 (<see cref="Seasons.BiteFactor"/>).</summary>
        public Season[] Seasons;
        /// <summary>잡동사니끼리의 상대 빈도 (잡동사니가 걸릴 확률 자체는 BiteSelector.JunkChance).</summary>
        public float JunkWeight = 1f;
        public bool IsJunk => Rarity == 0;
        public bool LivesIn(string mapId) => Maps == null || mapId == null || System.Array.IndexOf(Maps, mapId) >= 0;

        public static readonly string[] HanOnly = { "hangang" };
        public static readonly string[] SeaOnly = { "honghyeon" };
        public static readonly string[] Both = { "hangang", "honghyeon" };
        /// <summary>서천 앞바다(서해)에만.</summary>
        public static readonly string[] WestOnly = { "seocheon" };

        // 바다 지역 (낚시터 id)
        public static readonly string[] SouthSea = { "honghyeon", "yokji", "geomun", "jeju" };
        public static readonly string[] WestSea = { "seocheon", "yeongheung", "taean" };
        public static readonly string[] EastSea = { "heonhwa", "samcheok", "deungdae", "cheongjin" };
        /// <summary>모든 바다 낚시터 (바다 미끼를 판다).</summary>
        public static readonly string[] AllSeas = { "honghyeon", "yokji", "geomun", "jeju", "seocheon", "yeongheung", "taean", "heonhwa", "samcheok", "deungdae", "cheongjin" };
        /// <summary>남해·서해 모두.</summary>
        public static readonly string[] Seas = { "honghyeon", "yokji", "geomun", "jeju", "seocheon", "yeongheung", "taean" };
        /// <summary>바다 미끼를 파는 낚시터.</summary>
        /// <summary>바다 미끼를 파는 곳: 국내 바다 + 해외 바다(하롱베이·산토리니).</summary>
        public static readonly string[] SeaMaps = { "honghyeon", "yokji", "geomun", "jeju", "seocheon", "yeongheung", "taean", "heonhwa", "samcheok", "deungdae", "cheongjin", "halong", "santorini",
            "vinpearl", "phuket", "kohsamui", "langkawi", "sentosa", "tioman", "telokbatek", "tenggol", "crete", "zakynthos", "duden" };
        /// <summary>민물 미끼(지렁이·떡밥·옥수수·플라이)를 파는 곳: 한강 · 탄천 · 나일강 · 앙코르와트(톤레삽) · 돈콘(메콩) · 방콕(짜오프라야).</summary>
        public static readonly string[] FreshMaps = { "hangang", "tancheon", "nile", "angkor", "donkhone", "bangkok" };
        /// <summary>에기(가짜 새우 미끼)를 파는 곳: 서해(주꾸미·갑오징어), 동해(오징어·대문어), 제주·거문도(무늬오징어).</summary>
        public static readonly string[] EgiMaps = { "seocheon", "yeongheung", "taean", "heonhwa", "samcheok", "deungdae", "cheongjin", "jeju", "geomun" };

        public float WeightGrams(float len) => WeightCoef * len * len * len;
        public bool IsTrophy(float len) => !IsJunk && len >= TrophyLen;
    }

    /// <summary>
    /// 어종 표. 이 파일의 <see cref="All"/>이 처음 만든 77종 + 잡동사니이고, 2026-10-19부터 늘어난 어종은 지역별 partial 파일에 있다
    /// (<c>FishDatabase.Korea2.cs</c>, <c>.SeaAsiaFresh.cs</c>(동남아 민물), <c>.SeaAsia.cs</c>(동남아 바다), <c>.Mediterranean.cs</c>·<c>.Mediterranean2.cs</c> — docs/fish-db 기준).
    /// 지역 파일은 새 어종 목록(<c>…Species</c>)과, 이미 있는 어종이 새 낚시터에도 살게 하는 목록(<c>…MapAdds</c>)을 내놓고
    /// 정적 생성자가 합친다. 정적 생성자는 모든 partial 파일의 필드 초기화가 끝난 뒤에 돈다.
    /// </summary>
    public static partial class FishDatabase
    {
        static readonly Habitat[] AnyHabitat = { Habitat.Shallow, Habitat.Deep, Habitat.Rocky, Habitat.Weed };
        static readonly TimeBand[] AnyTime = { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day, TimeBand.Evening, TimeBand.Night };
        static readonly Weather[] AnyWeather = { Weather.Clear, Weather.Cloudy, Weather.Rain };
        static readonly BaitKind[] AnyBait = { BaitKind.Worm, BaitKind.Paste, BaitKind.Corn, BaitKind.Shrimp, BaitKind.Minnow, BaitKind.Lure,
            BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Squid, BaitKind.CutBait, BaitKind.Egi, BaitKind.Fly };
        static readonly string[] Sea = FishSpecies.SeaOnly;
        static readonly string[] Seas = FishSpecies.Seas;
        static readonly string[] West = FishSpecies.WestOnly;
        static readonly string[] South = FishSpecies.SouthSea;
        static readonly string[] WestSea = FishSpecies.WestSea;
        static readonly string[] East = FishSpecies.EastSea;
        static readonly string[] AllSeas = FishSpecies.AllSeas;
        static string[] M(params string[] ids) => ids;

        public static readonly List<FishSpecies> All = new List<FishSpecies>
        {
            new FishSpecies { Id = "crucian", Name = "붕어", Latin = "Carassius auratus", Temper = "온순하지만 입질이 예민하다",
                MinLen = 8, MaxLen = 42, TrophyLen = 30.3f, TrophyName = "월척", WeightCoef = 0.018f, Rarity = 1,
                Seasons = new[] { Season.Spring, Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Corn },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.45f, Speed = 0.8f, Stamina = 1.0f, Aggression = 0.5f, Exp = 12, Price = 300 },

            new FishSpecies { Id = "zacco", Name = "피라미", Latin = "Zacco platypus", Temper = "작지만 재빠르다",
                MinLen = 7, MaxLen = 17, TrophyLen = 15, TrophyName = "대형", WeightCoef = 0.009f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste , BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.25f, Speed = 1.4f, Stamina = 0.6f, Aggression = 0.7f, Exp = 5, Price = 80 },

            new FishSpecies { Id = "bluegill", Name = "블루길", Latin = "Lepomis macrochirus", Temper = "먹성이 좋아 미끼를 가리지 않는다",
                MinLen = 8, MaxLen = 25, TrophyLen = 22, TrophyName = "대형", WeightCoef = 0.025f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Shrimp , BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.35f, Speed = 1.0f, Stamina = 0.7f, Aggression = 0.6f, Exp = 7, Price = 60 },

            new FishSpecies { Id = "kkeuri", Name = "끄리", Latin = "Opsariichthys uncirostris", Temper = "사납게 먹이를 쫓는 육식어",
                MinLen = 15, MaxLen = 38, TrophyLen = 30, TrophyName = "대형", WeightCoef = 0.009f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Worm , BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.55f, Speed = 1.3f, Stamina = 0.8f, Aggression = 0.9f, Exp = 14, Price = 250 },

            new FishSpecies { Id = "nuchi", Name = "누치", Latin = "Hemibarbus labeo", Temper = "바닥을 훑으며 끈질기게 버틴다",
                MinLen = 20, MaxLen = 60, TrophyLen = 50, TrophyName = "대물", WeightCoef = 0.010f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.75f, Speed = 0.9f, Stamina = 1.1f, Aggression = 0.6f, Exp = 25, Price = 500 },

            new FishSpecies { Id = "skygager", Name = "강준치", Latin = "Chanodichthys erythropterus", Temper = "수면 가까이에서 먹이를 덮친다",
                MinLen = 30, MaxLen = 90, TrophyLen = 70, TrophyName = "대물", WeightCoef = 0.006f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure , BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear },
                Power = 0.8f, Speed = 1.2f, Stamina = 0.9f, Aggression = 0.8f, Exp = 30, Price = 400 },

            new FishSpecies { Id = "mullet", Name = "숭어", Latin = "Mugil cephalus", Temper = "바다에서 거슬러 올라온 힘 좋은 고기", Maps = FishSpecies.Both,
                MinLen = 30, MaxLen = 80, TrophyLen = 65, TrophyName = "대물", WeightCoef = 0.010f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Paste, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.85f, Speed = 1.3f, Stamina = 1.0f, Aggression = 0.75f, Exp = 30, Price = 600 },

            new FishSpecies { Id = "bass", Name = "배스", Latin = "Micropterus salmoides", Temper = "공격적인 외래 육식어",
                MinLen = 20, MaxLen = 62, TrophyLen = 50, TrophyName = "런커", WeightCoef = 0.016f, Rarity = 2,
                Habitats = new[] { Habitat.Weed, Habitat.Rocky }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Morning, TimeBand.Evening }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.8f, Speed = 1.1f, Stamina = 1.0f, Aggression = 0.85f, Exp = 28, Price = 350 },

            new FishSpecies { Id = "bagrid", Name = "동자개", Latin = "Tachysurus fulvidraco", Temper = "'빠가빠가' 소리를 내는 야행성 메기류",
                MinLen = 12, MaxLen = 34, TrophyLen = 28, TrophyName = "대형", WeightCoef = 0.010f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Rain, Weather.Cloudy },
                Power = 0.5f, Speed = 0.7f, Stamina = 0.9f, Aggression = 0.5f, Exp = 16, Price = 450 },

            new FishSpecies { Id = "catfish", Name = "메기", Latin = "Silurus asotus", Temper = "밤이면 깨어나는 묵직한 사냥꾼",
                MinLen = 25, MaxLen = 90, TrophyLen = 70, TrophyName = "대물", WeightCoef = 0.008f, Rarity = 2,
                Seasons = new[] { Season.Summer }, Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.Minnow, BaitKind.Shrimp , BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Rain, Weather.Cloudy },
                Power = 0.9f, Speed = 0.6f, Stamina = 1.3f, Aggression = 0.5f, Exp = 32, Price = 700 },

            new FishSpecies { Id = "carp", Name = "잉어", Latin = "Cyprinus carpio", Temper = "힘이 장사, 한강의 터줏대감",
                MinLen = 25, MaxLen = 105, TrophyLen = 80, TrophyName = "대물", WeightCoef = 0.014f, Rarity = 3,
                Seasons = new[] { Season.Spring, Season.Autumn }, Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.95f, Speed = 0.7f, Stamina = 1.3f, Aggression = 0.45f, Exp = 40, Price = 900 },

            new FishSpecies { Id = "mandarin", Name = "쏘가리", Latin = "Siniperca scherzeri", Temper = "바위틈에 숨어 사는 민물의 제왕",
                MinLen = 15, MaxLen = 55, TrophyLen = 45, TrophyName = "대물", WeightCoef = 0.014f, Rarity = 3,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy },
                Power = 0.85f, Speed = 1.0f, Stamina = 1.1f, Aggression = 0.8f, Exp = 55, Price = 2500 },

            new FishSpecies { Id = "snakehead", Name = "가물치", Latin = "Channa argus", Temper = "수초 속에 숨어 있다가 폭발적으로 덮친다",
                MinLen = 35, MaxLen = 100, TrophyLen = 80, TrophyName = "대물", WeightCoef = 0.009f, Rarity = 3,
                Seasons = new[] { Season.Summer }, Habitats = new[] { Habitat.Weed }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow , BaitKind.CutBait },
                Times = new[] { TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear },
                Power = 1.1f, Speed = 0.8f, Stamina = 1.4f, Aggression = 0.7f, Exp = 60, Price = 1500 },

            new FishSpecies { Id = "eel", Name = "뱀장어", Latin = "Anguilla japonica", Temper = "비 오는 밤에만 모습을 드러낸다",
                MinLen = 40, MaxLen = 100, TrophyLen = 85, TrophyName = "대물", WeightCoef = 0.0018f, Rarity = 4,
                Seasons = new[] { Season.Summer }, Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Worm, BaitKind.Shrimp, BaitKind.Minnow , BaitKind.CutBait },
                Times = new[] { TimeBand.Night }, Weathers = new[] { Weather.Rain },
                Power = 0.8f, Speed = 0.8f, Stamina = 1.4f, Aggression = 0.6f, Exp = 70, Price = 4000 },

            new FishSpecies { Id = "goldkoi", Name = "황금 비단잉어", Latin = "Cyprinus rubrofuscus", Temper = "새벽 물안개 속에서만 보인다는 한강의 전설",
                MinLen = 60, MaxLen = 110, TrophyLen = 100, TrophyName = "전설", WeightCoef = 0.014f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Corn, BaitKind.Paste },
                Times = new[] { TimeBand.Dawn }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.25f, Speed = 0.8f, Stamina = 1.8f, Aggression = 0.55f, Exp = 300, Price = 30000 },

            // 원작 2013 공략의 한강 어종 은어·떡붕어
            new FishSpecies { Id = "sweetfish", Name = "은어", Latin = "Plecoglossus altivelis", Temper = "맑은 여울에서 이끼를 뜯는다. 수박 향이 난다는 1년살이 물고기",
                MinLen = 10, MaxLen = 28, TrophyLen = 23, TrophyName = "대은어", WeightCoef = 0.008f, Rarity = 2,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Fly, BaitKind.Lure, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.35f, Speed = 1.5f, Stamina = 0.8f, Aggression = 0.8f, Exp = 16, Price = 700 },
            new FishSpecies { Id = "gengorobuna", Name = "떡붕어", Latin = "Carassius cuvieri", Temper = "붕어보다 체고가 높은 일본 원산 붕어. 떡밥을 좋아하고 중층에 뜬다",
                MinLen = 12, MaxLen = 50, TrophyLen = 40, TrophyName = "대형 떡붕어", WeightCoef = 0.02f, Rarity = 1,
                Seasons = new[] { Season.Spring, Season.Summer }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Paste, BaitKind.Corn },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 0.7f, Stamina = 1.0f, Aggression = 0.4f, Exp = 14, Price = 350 },

            // ---------- 괴물 어종 (원작 이벤트 몬스터) ----------
            // 원작: 한강 부표 지점에 괴물잉어가 나타났다 (이벤트). 솔로판은 비·흐린 밤에 깊은 곳에서 아주 드물게 문다
            new FishSpecies { Id = "monstercarp", Name = "괴물잉어", Latin = "Cyprinus carpio (거대 개체)", Temper = "한강 부표 아래 산다는 전설의 잉어. 이끼 낀 비늘 하나가 손바닥만 하다",
                MinLen = 110, MaxLen = 150, TrophyLen = 130, TrophyName = "한강의 주인", WeightCoef = 0.014f, Rarity = 5, BossCoins = 3,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Corn, BaitKind.Paste },
                Times = new[] { TimeBand.Night }, Weathers = new[] { Weather.Rain, Weather.Cloudy },
                Power = 1.5f, Speed = 0.75f, Stamina = 2.4f, Aggression = 0.5f, Exp = 400, Price = 20000 },
            new FishSpecies { Id = "monstersnakehead", Name = "괴물가물치", Latin = "Channa argus (거대 개체)", Temper = "수초 숲의 폭군. 개구리도 오리 새끼도 한입에 삼킨다는 소문",
                MinLen = 100, MaxLen = 135, TrophyLen = 120, TrophyName = "수초의 폭군", WeightCoef = 0.009f, Rarity = 5, BossCoins = 3,
                Habitats = new[] { Habitat.Weed }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Rain },
                Power = 1.6f, Speed = 0.9f, Stamina = 2.2f, Aggression = 0.85f, Exp = 380, Price = 18000 },

            // ================= 홍현 앞바다 (남해) =================
            // 원작: 홍현 앞바다 인기 어종은 감성돔. 대물 퀘스트 "자이언트 그루퍼"는 저녁 C4 / 아침 E3 어군에서만.
            new FishSpecies { Id = "mackerel", Name = "고등어", Latin = "Scomber japonicus", Temper = "떼로 몰려다니며 미끼를 순식간에 채 간다", Maps = M("honghyeon", "yokji", "geomun", "heonhwa", "samcheok", "deungdae", "jeju"),
                MinLen = 20, MaxLen = 45, TrophyLen = 40, TrophyName = "대고등어", WeightCoef = 0.012f, Rarity = 1,
                Seasons = new[] { Season.Autumn }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.Squid, BaitKind.CutBait , BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.45f, Speed = 1.5f, Stamina = 0.8f, Aggression = 0.85f, Exp = 10, Price = 250 },
            new FishSpecies { Id = "horsemackerel", Name = "전갱이", Latin = "Trachurus japonicus", Temper = "해 질 녘 방파제로 몰려든다", Maps = M("honghyeon", "yokji", "geomun", "deungdae", "jeju"),
                MinLen = 15, MaxLen = 40, TrophyLen = 35, TrophyName = "대전갱이", WeightCoef = 0.011f, Rarity = 1,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.35f, Speed = 1.3f, Stamina = 0.7f, Aggression = 0.7f, Exp = 8, Price = 200 },
            new FishSpecies { Id = "halfbeak", Name = "학꽁치", Latin = "Hyporhamphus sajori", Temper = "가느다란 부리로 수면을 스친다", Maps = M("honghyeon", "yokji", "heonhwa", "samcheok"),
                MinLen = 20, MaxLen = 40, TrophyLen = 35, TrophyName = "대학꽁치", WeightCoef = 0.004f, Rarity = 1,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.2f, Speed = 1.4f, Stamina = 0.5f, Aggression = 0.6f, Exp = 6, Price = 150 },
            new FishSpecies { Id = "rockfish", Name = "볼락", Latin = "Sebastes inermis", Temper = "큰 눈으로 밤바다를 살핀다", Maps = M("honghyeon", "yokji", "geomun", "heonhwa", "deungdae", "jeju"),
                MinLen = 12, MaxLen = 32, TrophyLen = 27, TrophyName = "왕볼락", WeightCoef = 0.02f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill },
                Times = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Dawn }, Weathers = AnyWeather,
                Power = 0.35f, Speed = 0.8f, Stamina = 0.7f, Aggression = 0.5f, Exp = 9, Price = 400 },
            new FishSpecies { Id = "filefish", Name = "쥐치", Latin = "Stephanolepis cirrhifer", Temper = "작은 입으로 미끼만 쏙 따먹는 얌체", Maps = M("honghyeon", "yokji"),
                MinLen = 15, MaxLen = 32, TrophyLen = 28, TrophyName = "대쥐치", WeightCoef = 0.03f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Rocky }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp },
                Times = new[] { TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.35f, Speed = 0.7f, Stamina = 0.7f, Aggression = 0.5f, Exp = 9, Price = 500 },
            new FishSpecies { Id = "marbledflounder", Name = "문치가자미", Latin = "Pseudopleuronectes yokohamae", Temper = "모래 바닥에 납작 엎드려 있다", Maps = M("honghyeon", "yokji", "seocheon", "yeongheung", "taean"),
                MinLen = 15, MaxLen = 42, TrophyLen = 35, TrophyName = "대가자미", WeightCoef = 0.015f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.4f, Speed = 0.5f, Stamina = 0.8f, Aggression = 0.3f, Exp = 10, Price = 600 },
            new FishSpecies { Id = "scorpionfish", Name = "쏨뱅이", Latin = "Sebastiscus marmoratus", Temper = "가시가 억센 암초 지킴이", Maps = M("honghyeon", "yokji", "geomun", "jeju"),
                MinLen = 15, MaxLen = 32, TrophyLen = 28, TrophyName = "대쏨뱅이", WeightCoef = 0.025f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.SeaWorm, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.45f, Speed = 0.6f, Stamina = 0.8f, Aggression = 0.4f, Exp = 14, Price = 800 },
            new FishSpecies { Id = "jacopever", Name = "우럭", Latin = "Sebastes schlegelii", Temper = "바위 틈에 박혀 버티는 남해의 단골", Maps = AllSeas,
                MinLen = 20, MaxLen = 60, TrophyLen = 50, TrophyName = "대우럭", WeightCoef = 0.022f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Squid, BaitKind.Minnow },
                Times = AnyTime, Weathers = AnyWeather,
                Power = 0.7f, Speed = 0.6f, Stamina = 1.1f, Aggression = 0.45f, Exp = 20, Price = 900 },
            new FishSpecies { Id = "greenling", Name = "쥐노래미", Latin = "Hexagrammos otakii", Temper = "해초밭을 누비는 얼룩무늬", Maps = AllSeas,
                MinLen = 20, MaxLen = 55, TrophyLen = 45, TrophyName = "대노래미", WeightCoef = 0.015f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.6f, Speed = 0.8f, Stamina = 1.0f, Aggression = 0.5f, Exp = 18, Price = 700 },
            new FishSpecies { Id = "flounder", Name = "광어", Latin = "Paralichthys olivaceus", Temper = "모래에 숨어 있다가 덥석 문다", Maps = Seas,
                MinLen = 35, MaxLen = 95, TrophyLen = 70, TrophyName = "대광어", WeightCoef = 0.012f, Rarity = 2,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.Lure },
                Times = AnyTime, Weathers = AnyWeather,
                Power = 0.85f, Speed = 0.6f, Stamina = 1.3f, Aggression = 0.4f, Exp = 35, Price = 3000 },
            new FishSpecies { Id = "conger", Name = "붕장어", Latin = "Conger myriaster", Temper = "밤이면 굴에서 나와 몸을 비튼다", Maps = Seas,
                MinLen = 40, MaxLen = 110, TrophyLen = 90, TrophyName = "대붕장어", WeightCoef = 0.0022f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.8f, Speed = 0.9f, Stamina = 1.3f, Aggression = 0.6f, Exp = 30, Price = 1500 },
            new FishSpecies { Id = "blackseabream", Name = "감성돔", Latin = "Acanthopagrus schlegelii", Temper = "갯바위 꾼들의 로망, 홍현 앞바다의 인기 어종", Maps = M("honghyeon", "yokji", "geomun", "seocheon", "yeongheung", "taean", "heonhwa", "samcheok", "deungdae", "jeju"),
                MinLen = 20, MaxLen = 65, TrophyLen = 50, TrophyName = "5짜 감성돔", WeightCoef = 0.025f, Rarity = 3,
                Seasons = new[] { Season.Autumn, Season.Winter }, Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.95f, Speed = 1.0f, Stamina = 1.4f, Aggression = 0.65f, Exp = 45, Price = 3500 },
            new FishSpecies { Id = "opaleye", Name = "벵에돔", Latin = "Girella punctata", Temper = "물살 센 갯바위에서 찌를 확 끌고 들어간다", Maps = South,
                MinLen = 20, MaxLen = 50, TrophyLen = 40, TrophyName = "4짜 벵에돔", WeightCoef = 0.024f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.85f, Speed = 1.2f, Stamina = 1.2f, Aggression = 0.7f, Exp = 40, Price = 3000 },
            new FishSpecies { Id = "seabass", Name = "농어", Latin = "Lateolabrax japonicus", Temper = "파도치는 밤, 수면 위로 바늘털이를 한다", Maps = M("honghyeon", "yokji", "geomun", "seocheon", "yeongheung", "taean", "heonhwa", "jeju"),
                MinLen = 40, MaxLen = 105, TrophyLen = 80, TrophyName = "대농어", WeightCoef = 0.011f, Rarity = 3,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Dawn }, Weathers = new[] { Weather.Rain, Weather.Cloudy },
                Power = 1.0f, Speed = 1.3f, Stamina = 1.3f, Aggression = 0.8f, Exp = 55, Price = 4000 },
            new FishSpecies { Id = "redseabream", Name = "참돔", Latin = "Pagrus major", Temper = "먼바다 깊은 곳의 붉은 여왕", Maps = M("honghyeon", "yokji", "geomun", "seocheon", "taean", "jeju"),
                MinLen = 30, MaxLen = 95, TrophyLen = 70, TrophyName = "대물 참돔", WeightCoef = 0.018f, Rarity = 3,
                Seasons = new[] { Season.Spring, Season.Autumn }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.Squid, BaitKind.SeaWorm , BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.0f, Speed = 1.1f, Stamina = 1.4f, Aggression = 0.6f, Exp = 60, Price = 5000 },
            new FishSpecies { Id = "beakfish", Name = "돌돔", Latin = "Oplegnathus fasciatus", Temper = "단단한 이빨로 성게도 부순다, 갯바위의 제왕", Maps = South,
                MinLen = 25, MaxLen = 65, TrophyLen = 50, TrophyName = "돌돔 5짜", WeightCoef = 0.028f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 1.2f, Speed = 1.0f, Stamina = 1.6f, Aggression = 0.75f, Exp = 90, Price = 9000 },
            new FishSpecies { Id = "giantgrouper", Name = "자이언트 그루퍼", Latin = "Epinephelus bruneus", Temper = "홍현 앞바다 암초의 주인 (자바리). 저녁 C4, 아침 E3 어군에서만 나타난다", Maps = Sea,
                MinLen = 60, MaxLen = 130, TrophyLen = 100, TrophyName = "바다의 주인", WeightCoef = 0.02f, Rarity = 5, SchoolOnly = true,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Morning }, Weathers = AnyWeather,
                Power = 1.6f, Speed = 0.9f, Stamina = 2.2f, Aggression = 0.6f, Exp = 350, Price = 40000 },

            // 남해 깊은 바다의 전설 (먼바다 포인트에서만). 언론 보도: 다금바리는 제주·남해 수심 100~200m, 15~20kg이면 대형.
            // 돗돔은 400~500m 심해 암초에 살다 5~7월 산란하러 100m 안팎으로 올라올 때만 낚인다. 국내 기록 191cm, 한 해 30마리 안팎.
            new FishSpecies { Id = "dageumbari", Name = "다금바리", Latin = "Niphon spinosus", Temper = "제주·남해 깊은 암초의 최고급 어종. 먼바다 포인트에서 밤·저녁에 아주 드물게 문다", Maps = M("honghyeon", "geomun", "jeju"),
                MinLen = 45, MaxLen = 120, TrophyLen = 100, TrophyName = "전설의 다금바리", WeightCoef = 0.02f, Rarity = 5, FarSeaOnly = true,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.5f, Speed = 0.8f, Stamina = 2.0f, Aggression = 0.55f, Exp = 300, Price = 36000 },
            new FishSpecies { Id = "dotdom", Name = "돗돔", Latin = "Stereolepis doederleini", Temper = "심해의 거인. 봄·여름 산란철에만 먼바다 포인트로 올라온다. 사람 키만 한 괴력",
                Maps = M("honghyeon", "geomun", "jeju"), Seasons = new[] { Season.Spring, Season.Summer },
                MinLen = 90, MaxLen = 190, TrophyLen = 165, TrophyName = "전설의 돗돔", WeightCoef = 0.02f, Rarity = 5, FarSeaOnly = true,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait },
                Times = AnyTime, Weathers = AnyWeather,
                Power = 1.9f, Speed = 0.6f, Stamina = 2.6f, Aggression = 0.45f, Exp = 450, Price = 60000 },

            // 겨울 한강 얼음낚시 (얼음 구멍에서만)
            new FishSpecies { Id = "smelt", Name = "빙어", Latin = "Hypomesus nipponensis", Temper = "얼음 구멍 아래 떼 지어 다니는 겨울 강의 요정. 겨울에만, 얼음 위에서만 낚인다",
                MinLen = 6, MaxLen = 15, TrophyLen = 13, TrophyName = "왕빙어", WeightCoef = 0.005f, Rarity = 1, Seasons = new[] { Season.Winter },
                Habitats = AnyHabitat, Baits = new[] { BaitKind.Worm, BaitKind.Paste },
                Times = AnyTime, Weathers = AnyWeather,
                Power = 0.15f, Speed = 0.9f, Stamina = 0.4f, Aggression = 0.7f, Exp = 5, Price = 200 },

            // ---------- 서천 앞바다 (서해): 홍원항·마량포구 갯벌과 방파제 ----------
            new FishSpecies { Id = "goby", Name = "망둥어", Latin = "Acanthogobius flavimanus", Temper = "갯벌의 먹보. 뭐든 덥석 무는 서해 아이들의 첫 고기", Maps = WestSea,
                MinLen = 8, MaxLen = 30, TrophyLen = 25, TrophyName = "대망둥어", WeightCoef = 0.012f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.CutBait },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.3f, Speed = 0.6f, Stamina = 0.7f, Aggression = 0.4f, Exp = 6, Price = 150 },
            new FishSpecies { Id = "shad", Name = "전어", Latin = "Konosirus punctatus", Temper = "가을 전어 굽는 냄새에 집 나간 며느리도 돌아온다는 은빛 떼고기", Maps = WestSea,
                MinLen = 15, MaxLen = 30, TrophyLen = 26, TrophyName = "대전어", WeightCoef = 0.011f, Rarity = 1,
                Seasons = new[] { Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.3f, Speed = 1.2f, Stamina = 0.6f, Aggression = 0.7f, Exp = 7, Price = 300 },
            new FishSpecies { Id = "whitecroaker", Name = "백조기", Latin = "Pennahia argentata", Temper = "부레로 '구구' 우는 서해의 은빛 조기 (보구치)", Maps = WestSea,
                MinLen = 20, MaxLen = 40, TrophyLen = 33, TrophyName = "대백조기", WeightCoef = 0.014f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 0.8f, Stamina = 0.9f, Aggression = 0.5f, Exp = 16, Price = 900 },
            new FishSpecies { Id = "octopus", Name = "주꾸미", Latin = "Amphioctopus fangsiao", Temper = "에기를 끌어안고 놓지 않는 서천의 봄 명물. 묵직하게 버틴다", Maps = M("seocheon", "taean"),
                MinLen = 10, MaxLen = 32, TrophyLen = 26, TrophyName = "왕주꾸미", WeightCoef = 0.012f, Rarity = 2,
                Seasons = new[] { Season.Spring }, Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Egi, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.45f, Speed = 0.3f, Stamina = 0.9f, Aggression = 0.2f, Exp = 15, Price = 1200 },
            new FishSpecies { Id = "cuttlefish", Name = "갑오징어", Latin = "Sepia esculenta", Temper = "먹물을 뿜으며 뒤로 내빼는 봄 바다 별미", Maps = WestSea,
                MinLen = 18, MaxLen = 45, TrophyLen = 35, TrophyName = "대갑", WeightCoef = 0.014f, Rarity = 3,
                Seasons = new[] { Season.Spring }, Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Egi },
                Times = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Morning }, Weathers = AnyWeather,
                Power = 0.65f, Speed = 0.9f, Stamina = 1.0f, Aggression = 0.55f, Exp = 40, Price = 3500 },
            new FishSpecies { Id = "croaker", Name = "민어", Latin = "Miichthys miiuy", Temper = "서해 여름 보양식의 왕. 들물 때 정해진 어군(저녁 C5, 밤 F6)에만 나타난다", Maps = West,
                MinLen = 60, MaxLen = 140, TrophyLen = 100, TrophyName = "민어 1m", WeightCoef = 0.009f, Rarity = 5, SchoolOnly = true,
                Seasons = new[] { Season.Summer }, Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.5f, Speed = 0.8f, Stamina = 2.1f, Aggression = 0.55f, Exp = 320, Price = 45000 },

            // ---------- 동해 (헌화로·삼척 앞바다·동해 등대섬·청진포구): 차고 맑은 바다 ----------
            new FishSpecies { Id = "atkamackerel", Name = "임연수어", Latin = "Pleurogrammus azonus", Temper = "누런 몸에 검은 띠. '임연수 껍질 쌈밥에 천석꾼 망한다'는 동해 겨울 별미", Maps = M("samcheok", "deungdae", "cheongjin"),
                MinLen = 25, MaxLen = 50, TrophyLen = 42, TrophyName = "대임연수", WeightCoef = 0.009f, Rarity = 1,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 1.1f, Stamina = 0.9f, Aggression = 0.6f, Exp = 18, Price = 600 },
            new FishSpecies { Id = "sandfish", Name = "도루묵", Latin = "Arctoscopus japonicus", Temper = "'말짱 도루묵'의 그 고기. 초겨울 알을 낳으러 방파제 해초밭으로 몰려든다", Maps = M("samcheok", "cheongjin"),
                MinLen = 12, MaxLen = 26, TrophyLen = 22, TrophyName = "왕도루묵", WeightCoef = 0.009f, Rarity = 1,
                Seasons = new[] { Season.Winter }, Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.25f, Speed = 0.7f, Stamina = 0.5f, Aggression = 0.5f, Exp = 9, Price = 350 },
            new FishSpecies { Id = "flatfish", Name = "참가자미", Latin = "Pseudopleuronectes herzensteini", Temper = "동해 모래밭의 노란 테두리 가자미. 바닥을 톡톡 건드리면 문다", Maps = East,
                MinLen = 15, MaxLen = 40, TrophyLen = 33, TrophyName = "대가자미", WeightCoef = 0.016f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.4f, Speed = 0.5f, Stamina = 0.8f, Aggression = 0.3f, Exp = 12, Price = 700 },
            new FishSpecies { Id = "squid", Name = "오징어", Latin = "Todarodes pacificus", Temper = "밤바다 집어등 불빛으로 모여드는 동해의 명물. 에기를 덮치고 먹물을 쏜다", Maps = M("heonhwa", "samcheok", "deungdae"),
                MinLen = 18, MaxLen = 40, TrophyLen = 33, TrophyName = "대오징어", WeightCoef = 0.01f, Rarity = 2,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Egi, BaitKind.Lure },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.55f, Speed = 1.0f, Stamina = 0.9f, Aggression = 0.6f, Exp = 22, Price = 1400 },
            new FishSpecies { Id = "giantoctopus", Name = "대문어", Latin = "Enteroctopus dofleini", Temper = "동해 바위굴의 거인. 바닥에 달라붙어 낚싯대를 활처럼 휘게 만든다", Maps = M("heonhwa", "samcheok", "cheongjin"),
                MinLen = 50, MaxLen = 200, TrophyLen = 150, TrophyName = "왕문어", WeightCoef = 0.0016f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Egi, BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.4f, Speed = 0.3f, Stamina = 1.55f, Aggression = 0.25f, Exp = 110, Price = 12000 },
            new FishSpecies { Id = "cod", Name = "대구", Latin = "Gadus macrocephalus", Temper = "입이 커서 대구(大口). 한겨울 깊은 바다에서 묵직하게 올라온다", Maps = M("samcheok", "cheongjin"),
                MinLen = 40, MaxLen = 100, TrophyLen = 80, TrophyName = "대물 대구", WeightCoef = 0.009f, Rarity = 3,
                Seasons = new[] { Season.Winter }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.0f, Speed = 0.7f, Stamina = 1.5f, Aggression = 0.45f, Exp = 60, Price = 4500 },
            new FishSpecies { Id = "pollock", Name = "명태", Latin = "Gadus chalcogrammus", Temper = "국민 생선의 고향 청진. 남쪽 바다에선 사라진 지 오래, 새벽 B5·밤 F6 어군에서만 나타난다", Maps = M("cheongjin"),
                MinLen = 35, MaxLen = 90, TrophyLen = 70, TrophyName = "왕명태", WeightCoef = 0.007f, Rarity = 5, SchoolOnly = true,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.25f, Speed = 1.1f, Stamina = 1.9f, Aggression = 0.7f, Exp = 300, Price = 30000 },
            new FishSpecies { Id = "redrockfish", Name = "불볼락", Latin = "Sebastes thompsoni", Temper = "꾼들이 '열기'라 부르는 붉은 볼락. 줄줄이 달려 올라온다", Maps = M("heonhwa", "deungdae", "geomun", "jeju"),
                MinLen = 15, MaxLen = 35, TrophyLen = 29, TrophyName = "왕열기", WeightCoef = 0.019f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.45f, Speed = 0.8f, Stamina = 0.8f, Aggression = 0.55f, Exp = 16, Price = 900 },

            // ---------- 남해 섬 (욕지도·거문도): 따뜻한 물, 회유어 ----------
            new FishSpecies { Id = "yellowtail", Name = "방어", Latin = "Seriola quinqueradiata", Temper = "겨울 방어는 기름이 꽉 찬다. 떼로 몰려와 릴을 비명 지르게 한다", Maps = M("yokji", "geomun", "deungdae", "jeju"),
                MinLen = 40, MaxLen = 100, TrophyLen = 80, TrophyName = "대방어", WeightCoef = 0.012f, Rarity = 3,
                Seasons = new[] { Season.Autumn, Season.Winter }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.15f, Speed = 1.5f, Stamina = 1.5f, Aggression = 0.85f, Exp = 70, Price = 5500 },
            new FishSpecies { Id = "amberjack", Name = "부시리", Latin = "Seriola lalandi", Temper = "갯바위 꾼의 팔을 저리게 하는 은빛 어뢰. 거문도 여름 바다의 주인공", Maps = M("yokji", "geomun", "jeju"),
                MinLen = 50, MaxLen = 140, TrophyLen = 110, TrophyName = "미터급 부시리", WeightCoef = 0.011f, Rarity = 4,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.45f, Speed = 1.7f, Stamina = 1.8f, Aggression = 0.9f, Exp = 140, Price = 14000 },
            new FishSpecies { Id = "hairtail", Name = "갈치", Latin = "Trichiurus lepturus", Temper = "칼처럼 번쩍이는 은빛 몸. 밤이면 몸을 세운 채 수면 가까이 올라온다", Maps = M("yokji", "geomun", "jeju"),
                MinLen = 50, MaxLen = 140, TrophyLen = 110, TrophyName = "5지 갈치", WeightCoef = 0.0007f, Rarity = 2,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.CutBait, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.6f, Speed = 0.9f, Stamina = 1.0f, Aggression = 0.5f, Exp = 26, Price = 2200 },
            new FishSpecies { Id = "spanishmackerel", Name = "삼치", Latin = "Scomberomorus niphonius", Temper = "날카로운 이빨로 루어를 덮치는 가을 남해의 사냥꾼", Maps = M("yokji", "geomun", "jeju"),
                MinLen = 40, MaxLen = 100, TrophyLen = 80, TrophyName = "대삼치", WeightCoef = 0.006f, Rarity = 2,
                Seasons = new[] { Season.Autumn }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning }, Weathers = AnyWeather,
                Power = 0.8f, Speed = 1.6f, Stamina = 1.1f, Aggression = 0.85f, Exp = 34, Price = 2600 },

            new FishSpecies { Id = "tyrantmackerel", Name = "폭군삼치", Latin = "Scomberomorus niphonius (거대 개체)", Temper = "삼치 떼를 거느린다는 괴물 삼치. 이빨에 줄이 스치면 끝이다", Maps = M("yokji", "geomun", "jeju"),
                MinLen = 110, MaxLen = 150, TrophyLen = 135, TrophyName = "남해의 폭군", WeightCoef = 0.006f, Rarity = 5, BossCoins = 4,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.55f, Speed = 1.9f, Stamina = 2.2f, Aggression = 0.95f, Exp = 450, Price = 24000 },
            // 황새치 (Xiphias gladius): 밤이면 먼바다 수면 가까이 올라온다. 배를 타고 먼바다에서만
            new FishSpecies { Id = "swordfish", Name = "황새치", Latin = "Xiphias gladius", Temper = "칼 같은 위턱을 휘두르는 먼바다의 검객. 밤에 수면 가까이 올라온다", Maps = M("geomun", "jeju", "deungdae"),
                MinLen = 150, MaxLen = 300, TrophyLen = 250, TrophyName = "바다의 검객", WeightCoef = 0.0065f, Rarity = 5, BossCoins = 5, FarSeaOnly = true,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.Lure },
                Times = new[] { TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.8f, Speed = 1.7f, Stamina = 2.6f, Aggression = 0.9f, Exp = 600, Price = 40000 },

            // ================= 해외 거점 (원작: 동남아 2012-04 · 지중해 · 나일강 하류 2013-03) =================
            // ---------- 하롱베이 (베트남) ----------
            new FishSpecies { Id = "barramundi", Name = "바라문디", Latin = "Lates calcarifer", Temper = "맹그로브와 기수역의 은빛 사냥꾼. 걸리면 수면 위로 머리를 흔들며 뛴다", Maps = M("halong"),
                MinLen = 35, MaxLen = 120, TrophyLen = 95, TrophyName = "미터급 바라문디", WeightCoef = 0.011f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.2f, Speed = 1.1f, Stamina = 1.5f, Aggression = 0.8f, Exp = 80, Price = 5200 },
            new FishSpecies { Id = "orangegrouper", Name = "얼룩바리", Latin = "Epinephelus coioides", Temper = "석회암 봉우리 밑 바위굴의 주인. 물면 굴로 처박는다", Maps = M("halong"),
                MinLen = 25, MaxLen = 100, TrophyLen = 75, TrophyName = "대형 얼룩바리", WeightCoef = 0.016f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.Minnow, BaitKind.Shrimp, BaitKind.CutBait },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.25f, Speed = 0.7f, Stamina = 1.5f, Aggression = 0.55f, Exp = 75, Price = 4800 },
            new FishSpecies { Id = "mangrovejack", Name = "맹그로브잭", Latin = "Lutjanus argentimaculatus", Temper = "붉은 몸의 퉁돔. 수상 마을 기둥과 그물 밑에 숨어 산다", Maps = M("halong"),
                MinLen = 20, MaxLen = 80, TrophyLen = 60, TrophyName = "대형 맹그로브잭", WeightCoef = 0.016f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Shrimp, BaitKind.Squid, BaitKind.Lure },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 1.0f, Stamina = 1.2f, Aggression = 0.75f, Exp = 45, Price = 2400 },
            new FishSpecies { Id = "gianttrevally", Name = "자이언트 트레발리", Latin = "Caranx ignobilis", Temper = "GT라 불리는 열대 바다의 깡패. 릴이 비명을 지른다", Maps = M("halong"),
                MinLen = 50, MaxLen = 150, TrophyLen = 120, TrophyName = "괴물 GT", WeightCoef = 0.016f, Rarity = 4,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.6f, Speed = 1.6f, Stamina = 1.9f, Aggression = 0.9f, Exp = 160, Price = 12000 },
            new FishSpecies { Id = "barracuda", Name = "꼬치고기", Latin = "Sphyraena jello", Temper = "날카로운 이빨의 은빛 창. 수면 가까이서 루어를 낚아챈다", Maps = M("halong"),
                MinLen = 40, MaxLen = 140, TrophyLen = 110, TrophyName = "대형 꼬치고기", WeightCoef = 0.0045f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.9f, Speed = 1.7f, Stamina = 1.1f, Aggression = 0.85f, Exp = 50, Price = 2600 },

            // ---------- 산토리니 (그리스 · 에게해) ----------
            // 원작 영상: "지중해 산토리니 어종, 유럽대구 미끼"
            new FishSpecies { Id = "giltheadbream", Name = "귀족도미", Latin = "Sparus aurata", Temper = "눈 사이 금빛 띠가 왕관 같다. 조개를 깨 먹는 단단한 이빨", Maps = M("santorini"),
                MinLen = 18, MaxLen = 70, TrophyLen = 55, TrophyName = "대형 귀족도미", WeightCoef = 0.018f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.85f, Speed = 0.9f, Stamina = 1.2f, Aggression = 0.6f, Exp = 40, Price = 2200 },
            new FishSpecies { Id = "europeanseabass", Name = "유럽농어", Latin = "Dicentrarchus labrax", Temper = "파도가 부서지는 갯바위의 은빛 농어. 그리스 사람들은 '라브라키'라 부른다", Maps = M("santorini"),
                MinLen = 25, MaxLen = 100, TrophyLen = 75, TrophyName = "대형 유럽농어", WeightCoef = 0.0105f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.95f, Speed = 1.1f, Stamina = 1.2f, Aggression = 0.7f, Exp = 45, Price = 2500 },
            new FishSpecies { Id = "hake", Name = "유럽대구", Latin = "Merluccius merluccius", Temper = "깊은 칼데라 바닥의 대구. 밤이면 떠올라 정어리를 쫓는다", Maps = M("santorini"),
                MinLen = 25, MaxLen = 100, TrophyLen = 80, TrophyName = "대형 유럽대구", WeightCoef = 0.0075f, Rarity = 2,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.Minnow, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Dawn }, Weathers = AnyWeather,
                Power = 0.8f, Speed = 0.8f, Stamina = 1.1f, Aggression = 0.5f, Exp = 40, Price = 2000 },
            new FishSpecies { Id = "moray", Name = "지중해곰치", Latin = "Muraena helena", Temper = "현무암 틈에서 입을 벌리고 있는 얼룩 곰치. 바위에 몸을 감아 버틴다", Maps = M("santorini"),
                MinLen = 50, MaxLen = 150, TrophyLen = 120, TrophyName = "대형 곰치", WeightCoef = 0.0028f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.15f, Speed = 0.6f, Stamina = 1.6f, Aggression = 0.6f, Exp = 70, Price = 3600 },
            new FishSpecies { Id = "bluefintuna", Name = "참다랑어", Latin = "Thunnus thynnus", Temper = "에게해 먼바다를 가로지르는 바다의 탄환. 배를 끌고 다닐 만큼 세다", Maps = M("santorini"), FarSeaOnly = true,
                MinLen = 90, MaxLen = 260, TrophyLen = 200, TrophyName = "200급 참치", WeightCoef = 0.0155f, Rarity = 4,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning }, Weathers = AnyWeather,
                Power = 1.85f, Speed = 1.8f, Stamina = 2.4f, Aggression = 0.85f, Exp = 260, Price = 30000 },

            // ---------- 나일강 하류 (이집트) ----------
            new FishSpecies { Id = "nileperch", Name = "나일농어", Latin = "Lates niloticus", Temper = "나일강의 거인. 2m까지 자라고, 고대 이집트 벽화에도 그려졌다", Maps = M("nile"),
                MinLen = 50, MaxLen = 190, TrophyLen = 150, TrophyName = "나일의 거인", WeightCoef = 0.011f, Rarity = 4,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.7f, Speed = 1.0f, Stamina = 2.2f, Aggression = 0.65f, Exp = 220, Price = 16000 },
            new FishSpecies { Id = "tilapia", Name = "나일틸라피아", Latin = "Oreochromis niloticus", Temper = "입속에 알을 품어 기르는 나일의 붕어. 강변 사람들의 저녁거리", Maps = M("nile"),
                MinLen = 10, MaxLen = 50, TrophyLen = 40, TrophyName = "대형 틸라피아", WeightCoef = 0.019f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Corn },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 0.8f, Stamina = 1.0f, Aggression = 0.55f, Exp = 18, Price = 500 },
            new FishSpecies { Id = "africancatfish", Name = "아프리카메기", Latin = "Clarias gariepinus", Temper = "가물어도 진흙 속에서 버티는 질긴 메기. 밤에 수염으로 먹이를 찾는다", Maps = M("nile"),
                MinLen = 40, MaxLen = 150, TrophyLen = 110, TrophyName = "대형 아프리카메기", WeightCoef = 0.008f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.CutBait, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.15f, Speed = 0.6f, Stamina = 1.7f, Aggression = 0.45f, Exp = 60, Price = 2800 },
            new FishSpecies { Id = "tigerfish", Name = "타이거피시", Latin = "Hydrocynus vittatus", Temper = "줄무늬 몸에 송곳니가 밖으로 드러난 아프리카 강의 호랑이. 바늘을 뱉어 내기 일쑤다", Maps = M("nile"),
                MinLen = 25, MaxLen = 90, TrophyLen = 70, TrophyName = "대형 타이거피시", WeightCoef = 0.012f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 1.1f, Speed = 1.7f, Stamina = 1.3f, Aggression = 0.95f, Exp = 90, Price = 5400 },

            // ---------- 제주 (서귀포) ----------
            new FishSpecies { Id = "damselfish", Name = "자리돔", Latin = "Chromis notata", Temper = "제주 사람들의 여름 자리물회. 갯바위 앞 물속에 구름처럼 떼 지어 산다", Maps = M("jeju"),
                MinLen = 8, MaxLen = 18, TrophyLen = 15, TrophyName = "왕자리", WeightCoef = 0.03f, Rarity = 1,
                Seasons = new[] { Season.Spring, Season.Summer }, Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.25f, Speed = 1.2f, Stamina = 0.5f, Aggression = 0.7f, Exp = 9, Price = 300 },
            new FishSpecies { Id = "tilefish", Name = "옥돔", Latin = "Branchiostegus japonicus", Temper = "제주 진상품이던 귀한 생선. 깊은 모래펄 굴 앞에서 미끼를 기다린다", Maps = M("jeju"),
                MinLen = 25, MaxLen = 55, TrophyLen = 45, TrophyName = "대옥돔", WeightCoef = 0.012f, Rarity = 3,
                Seasons = new[] { Season.Autumn, Season.Winter }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.7f, Speed = 0.7f, Stamina = 1.1f, Aggression = 0.45f, Exp = 45, Price = 4500 },
            new FishSpecies { Id = "bigfinsquid", Name = "무늬오징어", Latin = "Sepioteuthis lessoniana", Temper = "에깅 꾼들의 로망. 가을 갯바위 앞 해초밭에서 에기를 덮치고 드랙을 차고 나간다", Maps = M("jeju", "geomun"),
                MinLen = 20, MaxLen = 50, TrophyLen = 40, TrophyName = "킬로급", WeightCoef = 0.016f, Rarity = 3,
                Seasons = new[] { Season.Autumn }, Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Egi },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.7f, Speed = 1.1f, Stamina = 1.0f, Aggression = 0.6f, Exp = 40, Price = 4000 },

            // ---------- 서해 섬 (영흥도·태안) ----------
            new FishSpecies { Id = "spottedseabass", Name = "점농어", Latin = "Lateolabrax maculatus", Temper = "몸에 검은 점이 박힌 서해 농어. 물골과 갯바위 사이를 오가며 먹이를 덮친다", Maps = WestSea,
                MinLen = 35, MaxLen = 95, TrophyLen = 75, TrophyName = "대점농어", WeightCoef = 0.011f, Rarity = 3,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain, Weather.Clear },
                Power = 0.95f, Speed = 1.25f, Stamina = 1.25f, Aggression = 0.75f, Exp = 50, Price = 3800 },

            // 잡동사니: 도감에는 기록되지 않는다
            new FishSpecies { Id = "can", Name = "빈 깡통", Latin = "-", Temper = "누가 버렸을까", Maps = null,
                MinLen = 12, MaxLen = 12, TrophyLen = 999, TrophyName = "", WeightCoef = 0.0001f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.1f, Speed = 0.1f, Stamina = 0.3f, Aggression = 0.1f, Exp = 1, Price = 0 },

            new FishSpecies { Id = "trap", Name = "통발", Latin = "-", Temper = "누군가 놓고 간 통발 (원작에도 낚였다)", Maps = null, JunkWeight = 0.6f,
                MinLen = 45, MaxLen = 45, TrophyLen = 999, TrophyName = "", WeightCoef = 0.00001f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.25f, Speed = 0.1f, Stamina = 0.5f, Aggression = 0.1f, Exp = 1, Price = 50 },
            new FishSpecies { Id = "box", Name = "나무 상자", Latin = "-", Temper = "안에 뭐가 들었을까? ...비었다", Maps = null, JunkWeight = 0.5f,
                MinLen = 40, MaxLen = 40, TrophyLen = 999, TrophyName = "", WeightCoef = 0.00002f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.3f, Speed = 0.1f, Stamina = 0.5f, Aggression = 0.1f, Exp = 1, Price = 30 },
            new FishSpecies { Id = "fishcan", Name = "생선 캔", Latin = "-", Temper = "CBT 이벤트의 그 생선 캔! 낚시점에서 비싸게 사 준다", Maps = null, JunkWeight = 0.45f,
                MinLen = 10, MaxLen = 10, TrophyLen = 999, TrophyName = "", WeightCoef = 0.0003f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.1f, Speed = 0.1f, Stamina = 0.3f, Aggression = 0.1f, Exp = 5, Price = 3000 },
            new FishSpecies { Id = "robotfish", Name = "로봇 물고기", Latin = "-", Temper = "한강 수질을 감시하던 로봇 물고기 (원작 한강 명물)", Maps = FishSpecies.HanOnly, JunkWeight = 0.45f,
                MinLen = 50, MaxLen = 50, TrophyLen = 999, TrophyName = "", WeightCoef = 0.00003f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.5f, Speed = 0.6f, Stamina = 0.6f, Aggression = 0.3f, Exp = 30, Price = 8000 },
            new FishSpecies { Id = "shoe", Name = "낡은 운동화", Latin = "-", Temper = "한 짝만 있다", Maps = null,
                MinLen = 27, MaxLen = 27, TrophyLen = 999, TrophyName = "", WeightCoef = 0.00002f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.15f, Speed = 0.1f, Stamina = 0.4f, Aggression = 0.1f, Exp = 1, Price = 0 },
        };

        static FishDatabase()
        {
            foreach (var list in new[] { Korea2Species, SeaAsiaFreshSpecies, SeaAsiaSpecies, MediterraneanSpecies, Mediterranean2Species })
                if (list != null) All.AddRange(list);
            foreach (var adds in new[] { Korea2MapAdds, SeaAsiaFreshMapAdds, SeaAsiaMapAdds, MediterraneanMapAdds, Mediterranean2MapAdds })
            {
                if (adds == null) continue;
                foreach (var (id, maps) in adds)
                {
                    var s = All.Find(f => f.Id == id);
                    if (s == null || s.Maps == null) continue;
                    s.Maps = s.Maps.Concat(maps).Distinct().ToArray(); // 공유 배열(SouthSea 등)은 건드리지 않고 새 배열로
                }
            }
        }

        static Dictionary<string, FishSpecies> _byId;

        public static FishSpecies Get(string id)
        {
            _byId ??= All.ToDictionary(s => s.Id);
            return id != null && _byId.TryGetValue(id, out var s) ? s : null;
        }

        public static IEnumerable<FishSpecies> DexSpecies => All.Where(s => !s.IsJunk);

        /// <summary>그 맵의 도감 어종.</summary>
        public static IEnumerable<FishSpecies> DexOf(string mapId) => All.Where(s => !s.IsJunk && s.Maps != null && System.Array.IndexOf(s.Maps, mapId) >= 0);
    }
}
