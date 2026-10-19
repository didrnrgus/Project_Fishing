using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>지중해 거점 어종 앞부분 (docs/fish-db/03_지중해_어종DB.md MED-M01~M22).</summary>
    public static partial class FishDatabase
    {
        static readonly List<FishSpecies> MediterraneanSpecies = new List<FishSpecies>
        {
            // ---------- 외해 회유어 (다랑어·보니토) ----------
            // MED-M03 날개다랑어: 긴 가슴지느러미의 외해 다랑어. 여름~가을 수온을 따라 몰려온다
            new FishSpecies { Id = "albacore", Name = "날개다랑어", Latin = "Thunnus alalunga", Temper = "가슴지느러미가 날개처럼 긴 흰살 다랑어. 여름 끝물에 에게해 먼바다를 떼 지어 지난다", Maps = M("santorini", "crete", "zakynthos"), FarSeaOnly = true,
                MinLen = 50, MaxLen = 120, TrophyLen = 100, TrophyName = "대형 날개다랑어", WeightCoef = 0.0165f, Rarity = 3,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.3f, Speed = 1.65f, Stamina = 1.7f, Aggression = 0.8f, Exp = 110, Price = 9000 },
            // MED-M04 리틀 투니: 연안까지 들어와 정어리 떼를 몰아 먹는 소형 다랑어
            new FishSpecies { Id = "littletunny", Name = "대서양점다랑어", Latin = "Euthynnus alletteratus", Temper = "등의 물결무늬와 가슴 밑 검은 점. 정어리 떼를 수면으로 몰아붙여 바다를 끓게 만든다", Maps = M("crete", "zakynthos", "duden"),
                MinLen = 40, MaxLen = 95, TrophyLen = 80, TrophyName = "대형 점다랑어", WeightCoef = 0.014f, Rarity = 2,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.1f, Speed = 1.75f, Stamina = 1.3f, Aggression = 0.9f, Exp = 55, Price = 3000 },
            // MED-M05 대서양 보니토: 봄·가을 연안으로 들어오는 줄무늬 소형 다랑어 (튀르키예 '팔라무트')
            new FishSpecies { Id = "atlanticbonito", Name = "대서양보니토", Latin = "Sarda sarda", Temper = "등에 비스듬한 검은 줄이 그어진 작은 다랑어. 튀르키예 사람들은 '팔라무트'라 부르며 가을을 기다린다", Maps = M("crete", "zakynthos", "duden"),
                MinLen = 35, MaxLen = 85, TrophyLen = 70, TrophyName = "대형 보니토", WeightCoef = 0.013f, Rarity = 2,
                Seasons = new[] { Season.Spring, Season.Autumn }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 1.65f, Stamina = 1.2f, Aggression = 0.85f, Exp = 50, Price = 2800 },

            // ---------- 전갱이과 대물 ----------
            // MED-M06 그레이터 앰버잭(잿방어): 난파선·수중 산의 지깅 보스
            new FishSpecies { Id = "greateramberjack", Name = "잿방어", Latin = "Seriola dumerili", Temper = "난파선 위를 맴도는 방어 집안의 맏형. 걸리면 곧장 난파선 철골 속으로 처박는다", Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 50, MaxLen = 170, TrophyLen = 140, TrophyName = "괴력의 잿방어", WeightCoef = 0.013f, Rarity = 4,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.6f, Speed = 1.6f, Stamina = 2.0f, Aggression = 0.85f, Exp = 180, Price = 18000 },
            // MED-M07 리어피시: 파도 지대에서 숭어 떼를 덮치는 대형 전갱이과
            new FishSpecies { Id = "leerfish", Name = "리어피시", Latin = "Lichia amia", Temper = "파도가 부서지는 갯바위에서 숭어 떼를 들이받는 은빛 사냥꾼. 그리스 꾼들은 '리차'라 부른다", Maps = M("santorini", "crete"),
                MinLen = 40, MaxLen = 150, TrophyLen = 110, TrophyName = "대형 리어피시", WeightCoef = 0.0104f, Rarity = 4,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.45f, Speed = 1.75f, Stamina = 1.6f, Aggression = 0.95f, Exp = 150, Price = 12000 },

            // ---------- 암초의 고급 어종 ----------
            // MED-M08 커먼 덴텍스: 경계심 높은 지중해 도미의 왕
            new FishSpecies { Id = "dentex", Name = "덴텍스", Latin = "Dentex dentex", Temper = "송곳니가 삐죽 나온 지중해 도미의 왕. 그리스에서는 '시나그리다'라 부르는데, 눈치가 빨라 좀처럼 물지 않는다", Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 25, MaxLen = 100, TrophyLen = 80, TrophyName = "전설의 덴텍스", WeightCoef = 0.0145f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.25f, Speed = 1.1f, Stamina = 1.7f, Aggression = 0.7f, Exp = 150, Price = 15000 },
            // MED-M09 더스키 그루퍼: 바위굴의 보호종. 물면 굴로 파고든다
            new FishSpecies { Id = "duskygrouper", Name = "더스키 그루퍼", Latin = "Epinephelus marginatus", Temper = "바위굴에서 50년을 사는 지중해의 늙은 왕. 물면 굴속으로 파고들어 꿈쩍도 않는다", Maps = M("santorini", "crete"),
                MinLen = 40, MaxLen = 140, TrophyLen = 110, TrophyName = "굴의 왕", WeightCoef = 0.021f, Rarity = 5,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.Minnow, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.65f, Speed = 0.75f, Stamina = 2.2f, Aggression = 0.55f, Exp = 300, Price = 34000 },
            // MED-M10 미거: 밤 모래펄에서 우는 지중해의 민어
            new FishSpecies { Id = "meagre", Name = "지중해민어", Latin = "Argyrosomus regius", Temper = "밤바다 모래펄에서 '꾸룩꾸룩' 우는 거대한 민어. 입속이 노래 튀르키예에서는 '사르아으즈(노란 입)'라 부른다", Maps = M("zakynthos", "duden"),
                MinLen = 50, MaxLen = 180, TrophyLen = 140, TrophyName = "대형 민어", WeightCoef = 0.009f, Rarity = 4,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.SeaWorm, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.45f, Speed = 0.85f, Stamina = 2.0f, Aggression = 0.55f, Exp = 170, Price = 16000 },

            // ---------- 도미류 ----------
            // MED-M13 커먼 판도라: 입문 구간의 분홍 도미
            new FishSpecies { Id = "pandora", Name = "판도라", Latin = "Pagellus erythrinus", Temper = "분홍빛 몸에 하늘색 점이 뿌려진 작은 도미. 그리스 식탁에 흔히 오르는 '리스리니'", Maps = M("crete", "zakynthos"),
                MinLen = 12, MaxLen = 50, TrophyLen = 38, TrophyName = "대형 판도라", WeightCoef = 0.018f, Rarity = 1,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill, BaitKind.Shrimp, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 1.0f, Stamina = 0.8f, Aggression = 0.6f, Exp = 18, Price = 700 },
            // MED-M14 레드 포기: 참돔의 지중해 사촌
            new FishSpecies { Id = "redporgy", Name = "지중해참돔", Latin = "Pagrus pagrus", Temper = "참돔의 지중해 사촌. 그리스 사람들은 '팡그리'라 부르며 잔칫상에 통째로 굽는다", Maps = M("santorini", "crete"),
                MinLen = 20, MaxLen = 75, TrophyLen = 55, TrophyName = "대물 팡그리", WeightCoef = 0.017f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Squid, BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 1.0f, Stamina = 1.35f, Aggression = 0.6f, Exp = 60, Price = 4500 },
            // MED-M15 화이트 시브림: 미끼만 빼먹는 영리한 갯바위 도미 (벵에돔 포지션)
            new FishSpecies { Id = "whiteseabream", Name = "흰도미", Latin = "Diplodus sargus", Temper = "꼬리자루에 검은 안장 무늬를 단 갯바위 도미. 영리해서 미끼만 쏙 빼먹고 달아난다", Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 15, MaxLen = 45, TrophyLen = 38, TrophyName = "대형 흰도미", WeightCoef = 0.02f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.7f, Speed = 1.0f, Stamina = 1.0f, Aggression = 0.6f, Exp = 42, Price = 2300 },
            // MED-M16 투밴디드 시브림: 검은 띠 두 줄의 흔한 도미
            new FishSpecies { Id = "twobandedbream", Name = "두줄도미", Latin = "Diplodus vulgaris", Temper = "머리 뒤와 꼬리자루에 검은 띠를 두른 흔한 도미. 해초밭 앞에서 떼로 미끼를 쪼아 댄다", Maps = M("santorini", "crete", "duden"),
                MinLen = 12, MaxLen = 40, TrophyLen = 30, TrophyName = "대형 두줄도미", WeightCoef = 0.02f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 1.05f, Stamina = 0.8f, Aggression = 0.6f, Exp = 16, Price = 600 },
            // MED-M17 새들드 시브림: 항구·방파제 표층의 꼬리점 도미
            new FishSpecies { Id = "saddledbream", Name = "안장도미", Latin = "Oblada melanura", Temper = "꼬리자루에 흰 테 두른 검은 점. 방파제 그늘 밑 표층에서 떼 지어 밑밥을 받아먹는다", Maps = M("santorini", "crete", "duden"),
                MinLen = 12, MaxLen = 34, TrophyLen = 28, TrophyName = "대형 안장도미", WeightCoef = 0.016f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.4f, Speed = 1.25f, Stamina = 0.65f, Aggression = 0.7f, Exp = 14, Price = 500 },
            // MED-M18 살레마: 해초를 뜯는 금빛 줄무늬 떼
            new FishSpecies { Id = "salema", Name = "살레마", Latin = "Sarpa salpa", Temper = "해초를 뜯어 먹는 금빛 줄무늬 떼. 잘못 먹으면 환각을 본다는 소문이 섬마다 전해진다", Maps = M("santorini", "crete"),
                MinLen = 15, MaxLen = 45, TrophyLen = 36, TrophyName = "대형 살레마", WeightCoef = 0.016f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Rocky }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.4f, Speed = 0.9f, Stamina = 0.8f, Aggression = 0.4f, Exp = 12, Price = 450 },

            // ---------- 이빨 있는 표층 포식자 ----------
            // MED-M19 블루피시: 이빨로 줄을 끊는 표층의 도살자 (튀르키예 '뤼페르')
            new FishSpecies { Id = "bluefish", Name = "블루피시", Latin = "Pomatomus saltatrix", Temper = "날카로운 이빨로 미끼어 떼를 난도질하는 표층의 도살자. 튀르키예에서는 '뤼페르'라 부르는 가을 별미", Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 30, MaxLen = 110, TrophyLen = 85, TrophyName = "대형 블루피시", WeightCoef = 0.0095f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.1f, Speed = 1.6f, Stamina = 1.3f, Aggression = 1.0f, Exp = 70, Price = 5000 },
            // MED-M20 유럽 바라쿠다: 해 질 녘 암초 둘레를 도는 은빛 창
            new FishSpecies { Id = "europeanbarracuda", Name = "유럽꼬치고기", Latin = "Sphyraena sphyraena", Temper = "해 질 녘 암초 둘레를 떼 지어 도는 은빛 창. 아래턱이 튀어나온 입으로 루어를 낚아챈다", Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 30, MaxLen = 120, TrophyLen = 90, TrophyName = "대형 꼬치고기", WeightCoef = 0.0045f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 1.7f, Stamina = 1.1f, Aggression = 0.9f, Exp = 65, Price = 4200 },
            // MED-M21 가아피시: 봄 연안의 바늘 주둥이
            new FishSpecies { Id = "garfish", Name = "유럽동갈치", Latin = "Belone belone", Temper = "뼈까지 초록빛인 바늘 주둥이. 봄이면 수면을 통통 튀며 연안으로 몰려든다", Maps = M("crete", "zakynthos"),
                MinLen = 30, MaxLen = 85, TrophyLen = 70, TrophyName = "대형 동갈치", WeightCoef = 0.0021f, Rarity = 1,
                Seasons = new[] { Season.Spring }, Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.35f, Speed = 1.5f, Stamina = 0.6f, Aggression = 0.75f, Exp = 15, Price = 550 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] MediterraneanMapAdds =
        {
            ("bluefintuna", M("zakynthos")),                        // MED-M01
            ("swordfish", M("santorini", "crete", "zakynthos")),    // MED-M02
            ("giltheadbream", M("crete", "zakynthos", "duden")),    // MED-M11
            ("europeanseabass", M("crete", "duden")),               // MED-M12
            ("moray", M("crete", "zakynthos", "duden")),            // MED-M22
        };
    }
}
