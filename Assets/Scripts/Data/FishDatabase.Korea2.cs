using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>한국 거점 추가 어종 (docs/fish-db/01_한국_어종DB.md).</summary>
    public static partial class FishDatabase
    {
        // partial 파일끼리는 정적 필드 초기화 순서가 정해져 있지 않다 (FishDatabase.cs의 AnyWeather가 아직 null일 수 있다).
        // 그래서 이 파일의 목록은 다른 파일의 정적 필드 대신 매번 새 배열을 돌려주는 속성을 쓴다
        static Weather[] KrAnyWeather => new[] { Weather.Clear, Weather.Cloudy, Weather.Rain };

        static readonly List<FishSpecies> Korea2Species = new List<FishSpecies>
        {
            // ---------- 한강 (KR-A08 황쏘가리, A15 웅어, A16 황복) ----------
            // 황쏘가리: 쏘가리의 황색 변이 (천연기념물). 쏘가리와 같은 바위 여울에 살지만 아주 드물다
            new FishSpecies { Id = "yellowmandarin", Name = "황쏘가리", Latin = "Siniperca scherzeri (황색 변이)", Temper = "온몸이 황금빛으로 빛나는 쏘가리. 나라가 지키는 귀한 몸이라 사진만 남기고 돌려보낸다",
                MinLen = 25, MaxLen = 60, TrophyLen = 50, TrophyName = "한강의 황금 제왕", WeightCoef = 0.014f, Rarity = 5,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.0f, Speed = 1.05f, Stamina = 1.5f, Aggression = 0.85f, Exp = 260, Price = 26000 },
            // 웅어: 봄에 바다에서 한강 하구(행주)로 올라오는 멸치류. 조선 때 임금님께 올리던 진상품
            new FishSpecies { Id = "grenadieranchovy", Name = "웅어", Latin = "Coilia nasus", Temper = "봄이면 바다에서 한강 하구로 거슬러 오르는 칼날 같은 은빛 멸치. 옛날엔 임금님 수라상에 올랐다",
                MinLen = 18, MaxLen = 40, TrophyLen = 34, TrophyName = "대웅어", WeightCoef = 0.003f, Rarity = 3,
                Seasons = new[] { Season.Spring }, Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Shrimp, BaitKind.Worm, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 0.3f, Speed = 1.35f, Stamina = 0.6f, Aggression = 0.65f, Exp = 28, Price = 1600 },
            // 황복: 봄 산란철에 한강을 거슬러 오르는 복어. 독이 있다
            new FishSpecies { Id = "obscurepuffer", Name = "황복", Latin = "Takifugu obscurus", Temper = "봄이면 알을 낳으러 강을 거슬러 오르는 노란 띠 복어. 맛은 으뜸이지만 독이 있어 함부로 먹으면 큰일 난다",
                MinLen = 18, MaxLen = 45, TrophyLen = 38, TrophyName = "대황복", WeightCoef = 0.02f, Rarity = 4,
                Seasons = new[] { Season.Spring }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.Worm, BaitKind.Minnow },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 0.6f, Speed = 0.7f, Stamina = 1.05f, Aggression = 0.55f, Exp = 75, Price = 6000 },

            // ---------- 동해 (KR-C05 황어, C06 연어) ----------
            // 황어: 바다에 살다 봄에 동해안 하천으로 올라가 알을 낳는다. 겨울~봄 하구·방파제에서 낚인다
            new FishSpecies { Id = "japanesedace", Name = "황어", Latin = "Tribolodon hakonensis", Temper = "바다에 살다 봄이면 강으로 오르는 잉어 무리. 산란철엔 옆구리에 붉은 혼인색 줄이 돋는다",
                Maps = M("heonhwa", "samcheok", "cheongjin"),
                MinLen = 20, MaxLen = 50, TrophyLen = 42, TrophyName = "대황어", WeightCoef = 0.011f, Rarity = 2,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = KrAnyWeather,
                Power = 0.55f, Speed = 1.1f, Stamina = 0.9f, Aggression = 0.6f, Exp = 22, Price = 900 },
            // 연어 (첨연어): 가을에 태어난 동해안 하천(남대천·오십천)으로 돌아온다
            new FishSpecies { Id = "chumsalmon", Name = "연어", Latin = "Oncorhynchus keta", Temper = "태어난 강 냄새를 기억해 가을이면 동해로 돌아온다. 물살을 거슬러 뛰어오르는 은빛 귀향자",
                Maps = M("heonhwa", "samcheok", "cheongjin"),
                MinLen = 55, MaxLen = 95, TrophyLen = 80, TrophyName = "대연어", WeightCoef = 0.011f, Rarity = 3,
                Seasons = new[] { Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 1.1f, Speed = 1.3f, Stamina = 1.45f, Aggression = 0.8f, Exp = 65, Price = 4500 },

            // ---------- 남해·제주 (KR-D06 참다랑어, D08 혹돔) ----------
            // 태평양 참다랑어: 난류를 타고 남해·동해로 올라온다. 배를 타고 먼바다에서만 (산토리니의 참다랑어는 대서양 종)
            new FishSpecies { Id = "pacificbluefin", Name = "태평양 참다랑어", Latin = "Thunnus orientalis", Temper = "난류를 타고 남해·동해까지 올라오는 태평양의 거대 참치. 한 번 걸리면 몇 시간이고 줄을 끌고 간다",
                Maps = M("geomun", "jeju", "yokji", "deungdae", "samcheok"), FarSeaOnly = true,
                MinLen = 100, MaxLen = 280, TrophyLen = 230, TrophyName = "대왕 참다랑어", WeightCoef = 0.0155f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 1.9f, Speed = 1.85f, Stamina = 2.6f, Aggression = 0.85f, Exp = 500, Price = 50000 },
            // 혹돔: 수컷은 이마에 큰 혹이 솟는 대형 놀래기. 남해 섬·제주 암초
            new FishSpecies { Id = "sheepsheadwrasse", Name = "혹돔", Latin = "Semicossyphus reticulatus", Temper = "수컷은 이마에 주먹만 한 혹이 솟는다. 억센 이빨로 소라와 성게를 깨 먹는 암초 밑 터줏대감",
                Maps = M("yokji", "geomun", "jeju"),
                MinLen = 30, MaxLen = 90, TrophyLen = 70, TrophyName = "왕혹돔", WeightCoef = 0.02f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = KrAnyWeather,
                Power = 1.25f, Speed = 0.8f, Stamina = 1.65f, Aggression = 0.6f, Exp = 100, Price = 9000 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] Korea2MapAdds =
        {
        };
    }
}
