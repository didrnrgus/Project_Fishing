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

            // ---------- 탄천 (성남 야탑, 생태하천 복원으로 돌아온 토종 민물고기 — 사용자 자료) ----------
            // 버들치: 성남시 깃대종. 차고 맑은 1급수에만 사는 지표종
            new FishSpecies { Id = "chineseminnow", Name = "버들치", Latin = "Rhynchocypris oxycephalus", Temper = "성남시의 깃대종. 차고 맑은 물에만 사는 1급수 지표종으로, 버들잎처럼 가는 몸에 잔 검은 점이 흩어져 있다",
                Maps = M("tancheon"),
                MinLen = 6, MaxLen = 15, TrophyLen = 13, TrophyName = "대형", WeightCoef = 0.011f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.22f, Speed = 1.3f, Stamina = 0.55f, Aggression = 0.7f, Exp = 10, Price = 150 },
            // 갈겨니: 피라미와 닮았지만 눈이 크고 옆구리에 짙은 세로띠. 맑은 하천 중상류
            new FishSpecies { Id = "darkchub", Name = "갈겨니", Latin = "Zacco temminckii", Temper = "피라미를 닮은 맑은 물의 사촌. 눈이 크고 옆구리에 짙은 세로띠, 여름 수컷은 배가 붉게 물든다",
                Maps = M("tancheon"),
                MinLen = 9, MaxLen = 20, TrophyLen = 17, TrophyName = "대형", WeightCoef = 0.0095f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 0.28f, Speed = 1.35f, Stamina = 0.65f, Aggression = 0.75f, Exp = 8, Price = 120 },
            // 참마자: 한국 고유종. 모래·자갈 바닥을 긴 주둥이로 뒤진다
            new FishSpecies { Id = "skinnybarbel", Name = "참마자", Latin = "Hemibarbus longirostris", Temper = "모래·자갈 바닥을 긴 주둥이로 뒤지는 한국 고유종. 옆구리에 검은 점이 줄지어 박혀 있다",
                Maps = M("tancheon"),
                MinLen = 12, MaxLen = 30, TrophyLen = 25, TrophyName = "대형", WeightCoef = 0.01f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = KrAnyWeather,
                Power = 0.4f, Speed = 0.95f, Stamina = 0.85f, Aggression = 0.5f, Exp = 16, Price = 400 },
            // 돌고기: 돌 틈에 숨어 사는 통통한 물고기. 주둥이에서 꼬리까지 굵은 검은 띠
            new FishSpecies { Id = "stripedshiner", Name = "돌고기", Latin = "Pungtungia herzi", Temper = "돌 틈에 숨어 사는 통통한 물고기. 주둥이 끝에서 꼬리까지 굵은 검은 띠가 지난다",
                Maps = M("tancheon"),
                MinLen = 6, MaxLen = 14, TrophyLen = 12, TrophyName = "대형", WeightCoef = 0.014f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 0.25f, Speed = 0.9f, Stamina = 0.6f, Aggression = 0.45f, Exp = 10, Price = 200 },
            // 참중고기: 한국 고유종. 여울에 살며 몸에 얼룩무늬, 산란기 수컷은 뺨이 붉다
            new FishSpecies { Id = "korsarcocheilichthys", Name = "참중고기", Latin = "Sarcocheilichthys variegatus wakiyae", Temper = "여울을 좋아하는 한국 고유종. 몸에 얼룩무늬가 있고, 산란기 수컷은 뺨이 붉고 몸이 검어진다",
                Maps = M("tancheon"),
                MinLen = 7, MaxLen = 15, TrophyLen = 13, TrophyName = "대형", WeightCoef = 0.012f, Rarity = 3,
                Seasons = new[] { Season.Spring, Season.Summer }, Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = KrAnyWeather,
                Power = 0.3f, Speed = 1.0f, Stamina = 0.7f, Aggression = 0.55f, Exp = 22, Price = 600 },
            // 납자루: 민물조개 몸속에 알을 낳는다. 수컷 혼인색은 보랏빛·주황
            new FishSpecies { Id = "bitterling", Name = "납자루", Latin = "Acheilognathus lanceolatus", Temper = "민물조개 몸속에 알을 낳는 작은 물고기. 수컷은 혼인색으로 몸이 보랏빛, 지느러미 끝이 주황으로 반짝인다",
                Maps = M("tancheon"),
                MinLen = 5, MaxLen = 11, TrophyLen = 10, TrophyName = "대형", WeightCoef = 0.016f, Rarity = 2,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Paste, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = KrAnyWeather,
                Power = 0.2f, Speed = 0.9f, Stamina = 0.5f, Aggression = 0.4f, Exp = 12, Price = 250 },
            // 각시붕어: 한국 고유종. 손가락만 한 몸, 수컷은 붉은 혼인색 — 가장 예쁜 민물고기로 꼽힌다
            new FishSpecies { Id = "rosybitterling", Name = "각시붕어", Latin = "Rhodeus uyekii", Temper = "손가락만 한 한국 고유종. 봄이면 수컷이 붉고 푸른 혼인색으로 물들어 가장 예쁜 민물고기로 꼽힌다. 조개에 알을 낳는다",
                Maps = M("tancheon"),
                MinLen = 3.5f, MaxLen = 8, TrophyLen = 7, TrophyName = "대형", WeightCoef = 0.018f, Rarity = 3,
                Seasons = new[] { Season.Spring }, Habitats = new[] { Habitat.Weed }, Baits = new[] { BaitKind.Paste, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.15f, Speed = 0.8f, Stamina = 0.45f, Aggression = 0.35f, Exp = 20, Price = 500 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] Korea2MapAdds =
        {
            // 탄천: 한강과 이어져 있어 붕어·잉어·피라미·누치·메기가 올라오고, 외래종 배스·블루길도 산다
            ("crucian", new[] { "tancheon" }), ("carp", new[] { "tancheon" }), ("zacco", new[] { "tancheon" }), ("nuchi", new[] { "tancheon" }),
            ("catfish", new[] { "tancheon" }), ("bass", new[] { "tancheon" }), ("bluegill", new[] { "tancheon" }),
        };
    }
}
