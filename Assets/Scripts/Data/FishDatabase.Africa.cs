using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>나일강 하류(아프리카 거점) 추가 어종 + 지중해 낚시터 추가 어종. 모델 설계는 tools/blender/species_africa.py.</summary>
    /// <remarks>
    /// 나일강은 민물이라 Worm · Paste · Corn · Fly · Shrimp · Minnow · Lure 미끼만 판다. 지중해 낚시터는 바다 미끼(Krill · SeaWorm · Squid · CutBait …).
    /// 이집트 나일에 실제로 사는 어종 위주로 골랐고, 고대 이집트 벽화·신화에 나오는 고기(코끼리주둥이고기 · 전기메기 · 빈니 바브 · 파하카 복어)가 많다.
    /// 나일 비키르는 이집트에서 거의 사라졌고(지역 절멸), 짐나르쿠스·아프리카 아로와나는 나일 상류 쪽에 많아 희귀하게 둔다.
    /// </remarks>
    public static partial class FishDatabase
    {
        // partial 파일끼리는 정적 필드 초기화 순서가 정해져 있지 않다 (FishDatabase.cs의 AnyWeather가 아직 null일 수 있다).
        // 그래서 다른 파일의 정적 필드 대신 매번 새 배열을 돌려주는 속성을 쓴다
        static Weather[] AfAnyWeather => new[] { Weather.Clear, Weather.Cloudy, Weather.Rain };
        static string[] AfNile => new[] { "nile" };

        static readonly List<FishSpecies> AfricaSpecies = new List<FishSpecies>
        {
            // ================= 나일강 하류 (민물, Lv.31) =================
            // 바야드 메기 (Bagrus bajad): 1m 넘게 자라는 나일의 육식 메기. 낮에는 바위틈에 숨고 해 질 녘부터 작은 고기를 사냥한다
            new FishSpecies { Id = "bagrusbajad", Name = "바야드 메기", Latin = "Bagrus bajad", Temper = "가슴지느러미까지 닿는 긴 수염을 휘날리는 나일의 사냥꾼 메기. 낮에는 바위틈에 숨었다가 저녁이면 작은 고기를 덮친다",
                Maps = AfNile,
                MinLen = 30, MaxLen = 110, TrophyLen = 85, TrophyName = "미터급 바야드", WeightCoef = 0.009f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Worm, BaitKind.Shrimp, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 1.3f, Speed = 1.0f, Stamina = 1.7f, Aggression = 0.75f, Exp = 95, Price = 6000 },
            // 나일 시노돈티스 (Synodontis schall): 나일에서 가장 흔한 시노돈티스. 놀라면 부레를 울려 '꾸룩' 소리를 낸다
            new FishSpecies { Id = "synodontis", Name = "나일 시노돈티스", Latin = "Synodontis schall", Temper = "뼈 투구를 쓴 작은 메기. 낚아 올리면 '꾸룩꾸룩' 투덜거리고, 가시를 세워 손을 찌른다",
                Maps = AfNile,
                MinLen = 12, MaxLen = 45, TrophyLen = 36, TrophyName = "대형 시노돈티스", WeightCoef = 0.015f, Rarity = 1,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 0.45f, Speed = 0.7f, Stamina = 0.9f, Aggression = 0.5f, Exp = 22, Price = 600 },
            // 코끼리주둥이고기 (Mormyrus kannume): 코끼리 코 같은 주둥이로 진흙 속 깔따구 애벌레를 찾는다. 약한 전기로 어둠 속을 본다
            new FishSpecies { Id = "elephantsnout", Name = "코끼리주둥이고기", Latin = "Mormyrus kannume", Temper = "코끼리 코처럼 휜 주둥이로 강바닥 진흙을 더듬는다. 약한 전기를 내서 흙탕물 속에서도 길을 잃지 않는다",
                Maps = AfNile,
                MinLen = 25, MaxLen = 100, TrophyLen = 75, TrophyName = "대형 코끼리주둥이", WeightCoef = 0.007f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 0.9f, Speed = 0.9f, Stamina = 1.3f, Aggression = 0.45f, Exp = 85, Price = 5200 },
            // 나일 비키르 (Polypterus bichir): '살아 있는 화석'. 허파로 공기를 마신다. 이집트에서는 거의 사라져 아주 드물다
            new FishSpecies { Id = "bichir", Name = "나일 비키르", Latin = "Polypterus bichir", Temper = "등에 작은 깃발 지느러미를 줄줄이 세운 살아 있는 화석. 갑옷 같은 비늘에 허파로 숨을 쉬며 밤에 수초 사이를 기어 다닌다",
                Maps = AfNile,
                MinLen = 25, MaxLen = 75, TrophyLen = 60, TrophyName = "공룡시대의 생존자", WeightCoef = 0.006f, Rarity = 4,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.95f, Speed = 0.8f, Stamina = 1.5f, Aggression = 0.55f, Exp = 180, Price = 12000 },
            // 나일 라베오 (Labeo niloticus): 두툼한 입술로 돌의 이끼를 긁어 먹는 나일 계곡의 흔한 잉어
            new FishSpecies { Id = "labeo", Name = "나일 라베오", Latin = "Labeo niloticus", Temper = "두툼한 빨판 입술로 돌에 낀 이끼를 긁어 먹는다. 고대 이집트 무덤 벽화의 그물 속에도 가득 그려진 고기",
                Maps = AfNile,
                MinLen = 15, MaxLen = 60, TrophyLen = 48, TrophyName = "대형 라베오", WeightCoef = 0.013f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AfAnyWeather,
                Power = 0.55f, Speed = 0.85f, Stamina = 1.0f, Aggression = 0.45f, Exp = 20, Price = 550 },
            // 빈니 바브 (Labeobarbus bynni): 금빛 큰 비늘의 대형 잉어. 고대 이집트의 신성한 고기 '레피도토스'
            new FishSpecies { Id = "bynni", Name = "빈니 바브", Latin = "Labeobarbus bynni", Temper = "금빛 큰 비늘을 두른 나일의 잉어. 고대 이집트 사람들은 오시리스 신화의 신성한 고기라 여겨 함부로 먹지 않았다",
                Maps = AfNile,
                MinLen = 20, MaxLen = 82, TrophyLen = 65, TrophyName = "황금비늘 바브", WeightCoef = 0.013f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AfAnyWeather,
                Power = 0.95f, Speed = 1.0f, Stamina = 1.4f, Aggression = 0.55f, Exp = 50, Price = 2600 },
            // 파하카 복어 (Tetraodon lineatus): 민물 복어. 단단한 이빨로 달팽이·조개를 깨 먹는다. 몸에 독이 있어 먹으면 안 된다
            new FishSpecies { Id = "fahaka", Name = "파하카 복어", Latin = "Tetraodon lineatus", Temper = "금빛 줄무늬의 민물 복어. 달팽이 껍데기도 깨는 이빨로 낚싯줄을 끊고, 건지면 공처럼 부풀어 화를 낸다. 독이 있어 먹으면 안 된다",
                Maps = AfNile,
                MinLen = 10, MaxLen = 43, TrophyLen = 35, TrophyName = "성난 파하카", WeightCoef = 0.03f, Rarity = 2,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.Worm, BaitKind.Minnow },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AfAnyWeather,
                Power = 0.55f, Speed = 0.6f, Stamina = 0.9f, Aggression = 0.85f, Exp = 40, Price = 1800 },
            // 전기메기 (Malapterurus electricus): 몸을 감싼 전기 기관으로 수백 볼트를 낸다. 5천 년 전 이집트 무덤 그림에도 나온다
            new FishSpecies { Id = "electriccatfish", Name = "전기메기", Latin = "Malapterurus electricus", Temper = "소시지 같은 몸에 수백 볼트 전기를 품은 밤의 메기. 고대 이집트 의사들은 그 찌릿함으로 관절통을 다스렸다고 한다",
                Maps = AfNile,
                MinLen = 25, MaxLen = 120, TrophyLen = 90, TrophyName = "번개 메기", WeightCoef = 0.013f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Worm },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 1.25f, Speed = 0.55f, Stamina = 1.8f, Aggression = 0.6f, Exp = 110, Price = 7000 },
            // 아프리카 아로와나 (Heterotis niloticus): 아가미 위 기관으로 플랑크톤을 걸러 먹는 순한 아로와나. 공기를 들이마신다
            new FishSpecies { Id = "heterotis", Name = "아프리카 아로와나", Latin = "Heterotis niloticus", Temper = "커다란 비늘의 순한 아로와나. 수초 사이 진흙을 오물거리며 걸러 먹고, 숨이 차면 물 위로 코를 내민다",
                Maps = AfNile,
                MinLen = 30, MaxLen = 100, TrophyLen = 80, TrophyName = "대형 헤테로티스", WeightCoef = 0.01f, Rarity = 3,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AfAnyWeather,
                Power = 1.1f, Speed = 1.05f, Stamina = 1.6f, Aggression = 0.4f, Exp = 100, Price = 6500 },
            // 짐나르쿠스 (Gymnarchus niloticus, 아바): 긴 등지느러미 하나만 물결쳐 앞뒤로 헤엄치는 1.6m 전기 물고기
            new FishSpecies { Id = "gymnarchus", Name = "짐나르쿠스", Latin = "Gymnarchus niloticus", Temper = "등지느러미 하나를 물결치며 앞으로도 뒤로도 미끄러지는 전기 물고기. 눈은 거의 안 보이지만 전기로 미끼를 먼저 알아챈다",
                Maps = AfNile,
                MinLen = 60, MaxLen = 160, TrophyLen = 135, TrophyName = "나일의 유령", WeightCoef = 0.0046f, Rarity = 5,
                Habitats = new[] { Habitat.Weed, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Night, TimeBand.Dawn }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.5f, Speed = 1.2f, Stamina = 2.2f, Aggression = 0.7f, Exp = 380, Price = 36000 },

            // ================= 지중해 (바다) =================
            // 달고기 (Zeus faber): 몸 가운데 검은 점은 '성 베드로의 엄지 자국'. 정면에서 보면 얇아 들키지 않고 다가가 입을 대포처럼 뻗는다
            new FishSpecies { Id = "johndory", Name = "달고기", Latin = "Zeus faber", Temper = "몸 한가운데 성 베드로의 엄지 자국. 종잇장처럼 얇은 몸으로 슬금슬금 다가가 입을 쭉 뻗어 작은 고기를 삼킨다",
                Maps = new[] { "santorini", "crete", "zakynthos" },
                MinLen = 20, MaxLen = 66, TrophyLen = 52, TrophyName = "대형 달고기", WeightCoef = 0.02f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AfAnyWeather,
                Power = 0.7f, Speed = 0.5f, Stamina = 1.1f, Aggression = 0.4f, Exp = 75, Price = 5500 },
            // 유럽성대 (Chelidonichthys lucerna): 가슴지느러미 아래 손가락 같은 줄기로 모래 바닥을 걸어 다닌다. 펼치면 공작 깃 같은 파란 날개
            new FishSpecies { Id = "tubgurnard", Name = "유럽성대", Latin = "Chelidonichthys lucerna", Temper = "모래 바닥을 손가락 같은 지느러미로 걸어 다니다, 놀라면 공작 깃처럼 파란 날개를 활짝 편다. 걸리면 '꾸룩' 하고 운다",
                Maps = new[] { "crete", "zakynthos", "duden" },
                MinLen = 20, MaxLen = 75, TrophyLen = 58, TrophyName = "대형 성대", WeightCoef = 0.011f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AfAnyWeather,
                Power = 0.6f, Speed = 0.8f, Stamina = 1.0f, Aggression = 0.55f, Exp = 40, Price = 2000 },
            // 유럽통구멍 (Uranoscopus scaber): 모래 속에 숨어 하늘을 보는 눈과 입만 내놓는다. 어깨 가시에 독, 눈 뒤에서 약한 전기
            new FishSpecies { Id = "stargazer", Name = "유럽통구멍", Latin = "Uranoscopus scaber", Temper = "모래에 몸을 묻고 하늘만 쳐다보는 눈과 입만 내놓고 기다린다. 어깨 가시에 독이 있으니 맨손으로 쥐지 말 것",
                Maps = new[] { "santorini", "crete", "zakynthos" },
                MinLen = 12, MaxLen = 40, TrophyLen = 32, TrophyName = "대형 통구멍", WeightCoef = 0.016f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 0.45f, Speed = 0.5f, Stamina = 0.8f, Aggression = 0.6f, Exp = 38, Price = 1700 },
            // 그레이터 위버 (Trachinus draco): 낮에는 모래 속에 눈과 첫 등지느러미 독가시만 내놓고 숨는다. 해수욕객이 밟아 쏘이는 고기
            new FishSpecies { Id = "greaterweever", Name = "그레이터 위버", Latin = "Trachinus draco", Temper = "모래 속에 숨어 검은 독가시만 내놓는 지중해 해변의 지뢰. 쏘이면 몇 시간이고 불에 덴 듯 아프다. 밤이면 나와 돌아다닌다",
                Maps = new[] { "crete", "zakynthos", "duden" },
                MinLen = 15, MaxLen = 40, TrophyLen = 33, TrophyName = "독가시 왕", WeightCoef = 0.012f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 0.45f, Speed = 0.9f, Stamina = 0.75f, Aggression = 0.8f, Exp = 36, Price = 1500 },
            // 움브리나 (Umbrina cirrosa, 쉬드럼): 턱 밑 짧은 수염 하나, 금빛 물결무늬. 파도 치는 모래 해변과 강 하구에서 저녁에 잘 문다
            new FishSpecies { Id = "shidrum", Name = "움브리나", Latin = "Umbrina cirrosa", Temper = "금빛 물결무늬에 턱 밑 짧은 수염 하나. 파도가 부서지는 해변과 강어귀에서 해 질 녘 갯지렁이를 노린다",
                Maps = new[] { "crete", "zakynthos", "duden" },
                MinLen = 25, MaxLen = 73, TrophyLen = 60, TrophyName = "대형 움브리나", WeightCoef = 0.016f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AfAnyWeather,
                Power = 0.95f, Speed = 0.85f, Stamina = 1.4f, Aggression = 0.55f, Exp = 70, Price = 4800 },
            // 지중해비늘돔 (Sparisoma cretense): 크레타의 이름을 딴 지중해 유일의 비늘돔. 붉은 암컷과 잿빛 수컷의 색이 다르다
            new FishSpecies { Id = "medparrotfish", Name = "지중해비늘돔", Latin = "Sparisoma cretense", Temper = "크레타의 이름이 붙은 지중해 유일의 비늘돔. 앵무새 부리 같은 이빨로 바위의 해조를 갉는다. 붉은 쪽이 암컷이다",
                Maps = new[] { "crete", "santorini", "zakynthos", "duden" },
                MinLen = 15, MaxLen = 50, TrophyLen = 40, TrophyName = "대형 비늘돔", WeightCoef = 0.02f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AfAnyWeather,
                Power = 0.5f, Speed = 0.9f, Stamina = 0.9f, Aggression = 0.5f, Exp = 26, Price = 900 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] AfricaMapAdds =
        {
            ("mullet", new[] { "nile" }),                          // 숭어: 나일 삼각주 호수와 하류 민물까지 거슬러 오른다
            ("lionfish", new[] { "crete" }),                       // 라이언피시: 2015년 무렵부터 크레타에 자리 잡았다
            ("toadfish", new[] { "crete" }),                       // 은빛뺨복어: 크레타 연안에 널리 퍼졌다
            ("marbledspinefoot", new[] { "crete" }),               // 물결독가시치: 크레타 남부 연안에 정착
            ("rainbowwrasse", new[] { "zakynthos", "duden" }),     // 무지개놀래기: 지중해 바위 해안 어디에나
            ("paintedcomber", new[] { "zakynthos", "duden" }),     // 페인티드콤버: 지중해 바위·해초밭 어디에나
            ("salema", new[] { "zakynthos", "duden" }),            // 살레마: 지중해 연안 해초밭 떼고기
        };
    }
}
