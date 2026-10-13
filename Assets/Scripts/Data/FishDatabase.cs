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
        public bool IsJunk => Rarity == 0;

        public float WeightGrams(float len) => WeightCoef * len * len * len;
        public bool IsTrophy(float len) => !IsJunk && len >= TrophyLen;
    }

    /// <summary>한강 맵에 등장하는 어종. 해외 맵은 아직 없다.</summary>
    public static class FishDatabase
    {
        static readonly Habitat[] AnyHabitat = { Habitat.Shallow, Habitat.Deep, Habitat.Rocky, Habitat.Weed };
        static readonly TimeBand[] AnyTime = { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day, TimeBand.Evening, TimeBand.Night };
        static readonly Weather[] AnyWeather = { Weather.Clear, Weather.Cloudy, Weather.Rain };
        static readonly BaitKind[] AnyBait = { BaitKind.Worm, BaitKind.Paste, BaitKind.Corn, BaitKind.Shrimp, BaitKind.Minnow, BaitKind.Lure };

        public static readonly List<FishSpecies> All = new List<FishSpecies>
        {
            new FishSpecies { Id = "crucian", Name = "붕어", Latin = "Carassius auratus", Temper = "온순하지만 입질이 예민하다",
                MinLen = 8, MaxLen = 42, TrophyLen = 30.3f, TrophyName = "월척", WeightCoef = 0.018f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Corn },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.45f, Speed = 0.8f, Stamina = 1.0f, Aggression = 0.5f, Exp = 12, Price = 300 },

            new FishSpecies { Id = "zacco", Name = "피라미", Latin = "Zacco platypus", Temper = "작지만 재빠르다",
                MinLen = 7, MaxLen = 17, TrophyLen = 15, TrophyName = "대형", WeightCoef = 0.009f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.25f, Speed = 1.4f, Stamina = 0.6f, Aggression = 0.7f, Exp = 5, Price = 80 },

            new FishSpecies { Id = "bluegill", Name = "블루길", Latin = "Lepomis macrochirus", Temper = "먹성이 좋아 미끼를 가리지 않는다",
                MinLen = 8, MaxLen = 25, TrophyLen = 22, TrophyName = "대형", WeightCoef = 0.025f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.35f, Speed = 1.0f, Stamina = 0.7f, Aggression = 0.6f, Exp = 7, Price = 60 },

            new FishSpecies { Id = "kkeuri", Name = "끄리", Latin = "Opsariichthys uncirostris", Temper = "사납게 먹이를 쫓는 육식어",
                MinLen = 15, MaxLen = 38, TrophyLen = 30, TrophyName = "대형", WeightCoef = 0.009f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.55f, Speed = 1.3f, Stamina = 0.8f, Aggression = 0.9f, Exp = 14, Price = 250 },

            new FishSpecies { Id = "nuchi", Name = "누치", Latin = "Hemibarbus labeo", Temper = "바닥을 훑으며 끈질기게 버틴다",
                MinLen = 20, MaxLen = 60, TrophyLen = 50, TrophyName = "대물", WeightCoef = 0.010f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.75f, Speed = 0.9f, Stamina = 1.1f, Aggression = 0.6f, Exp = 25, Price = 500 },

            new FishSpecies { Id = "skygager", Name = "강준치", Latin = "Chanodichthys erythropterus", Temper = "수면 가까이에서 먹이를 덮친다",
                MinLen = 30, MaxLen = 90, TrophyLen = 70, TrophyName = "대물", WeightCoef = 0.006f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear },
                Power = 0.8f, Speed = 1.2f, Stamina = 0.9f, Aggression = 0.8f, Exp = 30, Price = 400 },

            new FishSpecies { Id = "mullet", Name = "숭어", Latin = "Mugil cephalus", Temper = "바다에서 거슬러 올라온 힘 좋은 고기",
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
                Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Rain, Weather.Cloudy },
                Power = 0.9f, Speed = 0.6f, Stamina = 1.3f, Aggression = 0.5f, Exp = 32, Price = 700 },

            new FishSpecies { Id = "carp", Name = "잉어", Latin = "Cyprinus carpio", Temper = "힘이 장사, 한강의 터줏대감",
                MinLen = 25, MaxLen = 105, TrophyLen = 80, TrophyName = "대물", WeightCoef = 0.014f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.95f, Speed = 0.7f, Stamina = 1.3f, Aggression = 0.45f, Exp = 40, Price = 900 },

            new FishSpecies { Id = "mandarin", Name = "쏘가리", Latin = "Siniperca scherzeri", Temper = "바위틈에 숨어 사는 민물의 제왕",
                MinLen = 15, MaxLen = 55, TrophyLen = 45, TrophyName = "대물", WeightCoef = 0.014f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy },
                Power = 0.85f, Speed = 1.0f, Stamina = 1.1f, Aggression = 0.8f, Exp = 55, Price = 2500 },

            new FishSpecies { Id = "snakehead", Name = "가물치", Latin = "Channa argus", Temper = "수초 속에 숨어 있다가 폭발적으로 덮친다",
                MinLen = 35, MaxLen = 100, TrophyLen = 80, TrophyName = "대물", WeightCoef = 0.009f, Rarity = 3,
                Habitats = new[] { Habitat.Weed }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear },
                Power = 1.1f, Speed = 0.8f, Stamina = 1.4f, Aggression = 0.7f, Exp = 60, Price = 1500 },

            new FishSpecies { Id = "eel", Name = "뱀장어", Latin = "Anguilla japonica", Temper = "비 오는 밤에만 모습을 드러낸다",
                MinLen = 40, MaxLen = 100, TrophyLen = 85, TrophyName = "대물", WeightCoef = 0.0018f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Worm, BaitKind.Shrimp, BaitKind.Minnow },
                Times = new[] { TimeBand.Night }, Weathers = new[] { Weather.Rain },
                Power = 0.8f, Speed = 0.8f, Stamina = 1.4f, Aggression = 0.6f, Exp = 70, Price = 4000 },

            new FishSpecies { Id = "goldkoi", Name = "황금 비단잉어", Latin = "Cyprinus rubrofuscus", Temper = "새벽 물안개 속에서만 보인다는 한강의 전설",
                MinLen = 60, MaxLen = 110, TrophyLen = 100, TrophyName = "전설", WeightCoef = 0.014f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Corn, BaitKind.Paste },
                Times = new[] { TimeBand.Dawn }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.25f, Speed = 0.8f, Stamina = 1.8f, Aggression = 0.55f, Exp = 300, Price = 30000 },

            // 잡동사니: 도감에는 기록되지 않는다
            new FishSpecies { Id = "can", Name = "빈 깡통", Latin = "-", Temper = "누가 버렸을까",
                MinLen = 12, MaxLen = 12, TrophyLen = 999, TrophyName = "", WeightCoef = 0.0001f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.1f, Speed = 0.1f, Stamina = 0.3f, Aggression = 0.1f, Exp = 1, Price = 0 },

            new FishSpecies { Id = "shoe", Name = "낡은 운동화", Latin = "-", Temper = "한 짝만 있다",
                MinLen = 27, MaxLen = 27, TrophyLen = 999, TrophyName = "", WeightCoef = 0.00002f, Rarity = 0,
                Habitats = AnyHabitat, Baits = AnyBait, Times = AnyTime, Weathers = AnyWeather,
                Power = 0.15f, Speed = 0.1f, Stamina = 0.4f, Aggression = 0.1f, Exp = 1, Price = 0 },
        };

        static Dictionary<string, FishSpecies> _byId;

        public static FishSpecies Get(string id)
        {
            _byId ??= All.ToDictionary(s => s.Id);
            return id != null && _byId.TryGetValue(id, out var s) ? s : null;
        }

        public static IEnumerable<FishSpecies> DexSpecies => All.Where(s => !s.IsJunk);
    }
}
