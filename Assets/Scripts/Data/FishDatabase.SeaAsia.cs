using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>동남아 거점 바다 어종 (docs/fish-db/02_동남아_어종DB.md 2-B, SEA-M).</summary>
    public static partial class FishDatabase
    {
        // 낚시터: 하롱베이(halong) · 빈펄(vinpearl) · 푸켓(phuket) · 코사무이(kohsamui) · 랑카위(langkawi) · 센토사(sentosa)
        //         · 플라우 티오만(tioman) · 텔록바텍(telokbatek) · 플라우 탱글(tenggol). 어종 배치는 DB 1장 표를 따른다
        static readonly List<FishSpecies> SeaAsiaSpecies = new List<FishSpecies>
        {
            // ---------- 외해 대형 회유어 (먼바다 포인트에서만) ----------
            // SEA-M02 돛새치: 푸켓·티오만 외해. 티오만에는 큰 군집이 온다
            new FishSpecies { Id = "sailfish", Name = "돛새치", Latin = "Istiophorus platypterus", Temper = "돛 같은 등지느러미를 활짝 펴고 정어리 떼를 몰아 사냥한다. 걸리면 연거푸 하늘로 뛰어오른다",
                Maps = M("phuket", "tioman"), FarSeaOnly = true,
                MinLen = 140, MaxLen = 300, TrophyLen = 250, TrophyName = "대형 돛새치", WeightCoef = 0.0033f, Rarity = 4,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.6f, Speed = 1.95f, Stamina = 1.9f, Aggression = 0.95f, Exp = 240, Price = 22000 },
            // SEA-M03 블랙마린 (우리말 백새치): 푸켓·티오만 최상위 보스. 황새치보다 크고 세다
            new FishSpecies { Id = "blackmarlin", Name = "블랙마린", Latin = "Istiompax indica", Temper = "우리말로 백새치. 가슴지느러미를 접지 못하는 외해의 왕. 500kg 넘는 몸으로 배를 끌고 다닌다",
                Maps = M("phuket", "tioman"), FarSeaOnly = true,
                MinLen = 180, MaxLen = 450, TrophyLen = 380, TrophyName = "그랜더", WeightCoef = 0.0077f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.95f, Speed = 1.85f, Stamina = 2.8f, Aggression = 0.95f, Exp = 700, Price = 50000 },
            // SEA-M04 황다랑어: 푸켓 인근 안다만해 외해, 코사무이 앞바다
            new FishSpecies { Id = "yellowfintuna", Name = "황다랑어", Latin = "Thunnus albacares", Temper = "낫처럼 길게 뻗은 노란 지느러미. 한번 물면 바닥 끝까지 내리꽂으며 버틴다",
                Maps = M("phuket", "kohsamui"), FarSeaOnly = true,
                MinLen = 60, MaxLen = 190, TrophyLen = 150, TrophyName = "대형 황다랑어", WeightCoef = 0.0155f, Rarity = 3,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.7f, Speed = 1.75f, Stamina = 2.1f, Aggression = 0.8f, Exp = 150, Price = 12000 },
            // SEA-M05 와후 (꼬치삼치): 부유물·FAD 주변. 이빨에 목줄이 잘린다
            new FishSpecies { Id = "wahoo", Name = "와후", Latin = "Acanthocybium solandri", Temper = "우리말로 꼬치삼치. 시속 80km로 루어를 덮치는 바다의 단거리 선수, 면도날 이빨에 목줄이 끊긴다",
                Maps = M("vinpearl", "phuket", "kohsamui", "tioman", "tenggol"), FarSeaOnly = true,
                MinLen = 70, MaxLen = 210, TrophyLen = 160, TrophyName = "대형 와후", WeightCoef = 0.0062f, Rarity = 3,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.35f, Speed = 2.0f, Stamina = 1.5f, Aggression = 0.9f, Exp = 120, Price = 8000 },
            // SEA-M11 도그투스 참치: 산호초 바깥 급경사의 포식자. 물면 암초로 처박는다
            new FishSpecies { Id = "dogtoothtuna", Name = "이빨다랑어", Latin = "Gymnosarda unicolor", Temper = "도그투스 참치. 다랑어인데 송곳니가 났다. 산호초 절벽을 따라 내리꽂아 줄을 암초에 쓸어 버린다",
                Maps = M("phuket", "tioman", "tenggol"), FarSeaOnly = true,
                MinLen = 60, MaxLen = 200, TrophyLen = 160, TrophyName = "대형 이빨다랑어", WeightCoef = 0.0125f, Rarity = 4,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning }, Weathers = AnyWeather,
                Power = 1.8f, Speed = 1.5f, Stamina = 2.1f, Aggression = 0.8f, Exp = 220, Price = 20000 },

            // ---------- 연안·외해 회유어 ----------
            // SEA-M06 만새기: 입문 외해 대상 (지중해 낚시터는 지중해 파일이 MapAdds로 더한다)
            new FishSpecies { Id = "mahimahi", Name = "만새기", Latin = "Coryphaena hippurus", Temper = "떠다니는 나무토막 그늘에 모이는 금빛·초록빛 물고기. 물 밖에 나오면 빛이 바래 간다",
                Maps = M("vinpearl", "phuket", "kohsamui", "tioman"),
                MinLen = 50, MaxLen = 160, TrophyLen = 120, TrophyName = "대형 만새기", WeightCoef = 0.0073f, Rarity = 2,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 1.15f, Speed = 1.5f, Stamina = 1.3f, Aggression = 0.9f, Exp = 55, Price = 3000 },
            // SEA-M07 코비아 (날쌔기): 난파선·구조물, 가오리를 따라다닌다
            new FishSpecies { Id = "cobia", Name = "날쌔기", Latin = "Rachycentron canadum", Temper = "코비아. 가오리 그림자를 따라다니다 난파선 밑에 자리 잡는다. 배 옆에 와서도 다시 처박는 끈질긴 힘",
                Maps = M("halong", "vinpearl", "phuket", "kohsamui", "langkawi", "tioman"),
                MinLen = 50, MaxLen = 180, TrophyLen = 140, TrophyName = "대형 날쌔기", WeightCoef = 0.0094f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.Shrimp, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.4f, Speed = 1.1f, Stamina = 1.8f, Aggression = 0.6f, Exp = 100, Price = 6500 },
            // SEA-M08 퀸피시: 연안·하구 포식자, 점프
            new FishSpecies { Id = "queenfish", Name = "퀸피시", Latin = "Scomberoides commersonnianus", Temper = "납작한 은빛 몸에 검은 점이 줄지어 박혔다. 하구 물살에서 루어를 쫓아 수면 위로 튀어 오른다",
                Maps = M("halong", "vinpearl", "phuket", "kohsamui", "langkawi", "sentosa", "telokbatek"),
                MinLen = 30, MaxLen = 120, TrophyLen = 90, TrophyName = "대형 퀸피시", WeightCoef = 0.0085f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 1.5f, Stamina = 1.1f, Aggression = 0.9f, Exp = 45, Price = 2200 },
            // SEA-M09 동갈삼치 (내로우바드 스패니시 마카렐)
            new FishSpecies { Id = "narrowbarredmackerel", Name = "동갈삼치", Latin = "Scomberomorus commerson", Temper = "몸에 물결 같은 가는 줄이 진 열대 삼치. 2m까지 자라고, 이빨로 루어째 끊어 간다",
                Maps = M("halong", "vinpearl", "phuket", "kohsamui", "tioman", "tenggol"),
                MinLen = 50, MaxLen = 200, TrophyLen = 150, TrophyName = "대형 동갈삼치", WeightCoef = 0.0055f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.15f, Speed = 1.75f, Stamina = 1.3f, Aggression = 0.9f, Exp = 60, Price = 3500 },
            // SEA-M30 백다랑어 (롱테일 참치): 타이만 연안을 도는 작은 다랑어 (연안형이라 먼바다 전용은 아니다)
            new FishSpecies { Id = "longtailtuna", Name = "백다랑어", Latin = "Thunnus tonggol", Temper = "롱테일 참치. 먼바다로 나가지 않고 타이만 연안을 떼 지어 도는 날씬한 다랑어",
                Maps = M("vinpearl", "kohsamui", "tioman"),
                MinLen = 40, MaxLen = 130, TrophyLen = 105, TrophyName = "대형 백다랑어", WeightCoef = 0.0146f, Rarity = 3,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.4f, Speed = 1.7f, Stamina = 1.7f, Aggression = 0.8f, Exp = 90, Price = 6000 },
            // SEA-M21 블루핀 트레발리: 산호초 가장자리
            new FishSpecies { Id = "bluefintrevally", Name = "블루핀 트레발리", Latin = "Caranx melampygus", Temper = "파란 지느러미에 몸 가득 검푸른 점. 산호초 가장자리를 무리 지어 돌며 작은 고기를 몰아붙인다",
                Maps = M("vinpearl", "phuket", "kohsamui", "langkawi", "tioman", "tenggol"),
                MinLen = 25, MaxLen = 100, TrophyLen = 80, TrophyName = "대형 블루핀", WeightCoef = 0.0125f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.1f, Speed = 1.5f, Stamina = 1.3f, Aggression = 0.85f, Exp = 55, Price = 3000 },
            // SEA-M27 스넛노즈 폼파노: 연안 파도 지대
            new FishSpecies { Id = "pompano", Name = "스넛노즈 폼파노", Latin = "Trachinotus blochii", Temper = "뭉툭한 주둥이에 노란 지느러미. 파도가 부서지는 모래톱에서 옆으로 몸을 눕혀 버틴다",
                Maps = M("halong", "vinpearl"),
                MinLen = 20, MaxLen = 90, TrophyLen = 70, TrophyName = "대형 폼파노", WeightCoef = 0.011f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 1.4f, Stamina = 1.2f, Aggression = 0.75f, Exp = 40, Price = 2600 },

            // ---------- 산호초·암초 ----------
            // SEA-M13 대왕바리 (진짜 자이언트 그루퍼). 게임의 giantgrouper는 홍현 앞바다 자바리(Epinephelus bruneus)라 따로 둔다
            new FishSpecies { Id = "daewangbari", Name = "대왕바리", Latin = "Epinephelus lanceolatus", Temper = "세상에서 가장 큰 바리, 영어 이름이 바로 자이언트 그루퍼. 난파선 속에 살며 상어도 통째로 삼킨다는 이야기가 전한다",
                Maps = M("langkawi", "tioman", "tenggol"),
                MinLen = 80, MaxLen = 240, TrophyLen = 200, TrophyName = "난파선의 왕", WeightCoef = 0.026f, Rarity = 5,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.85f, Speed = 0.7f, Stamina = 2.6f, Aggression = 0.5f, Exp = 480, Price = 45000 },
            // SEA-M12 코랄트라우트: 산호초 바리
            new FishSpecies { Id = "coraltrout", Name = "코랄트라우트", Latin = "Plectropomus leopardus", Temper = "붉은 몸에 파란 점이 별처럼 박힌 산호초 바리. 물자마자 산호 굴로 파고든다",
                Maps = M("vinpearl", "phuket", "kohsamui", "langkawi", "tioman", "tenggol"),
                MinLen = 25, MaxLen = 100, TrophyLen = 75, TrophyName = "대형 코랄트라우트", WeightCoef = 0.016f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.1f, Speed = 1.1f, Stamina = 1.3f, Aggression = 0.7f, Exp = 75, Price = 6000 },
            // SEA-M16 골든 스내퍼: 연안 암초 (모든 동남아 바다)
            new FishSpecies { Id = "goldensnapper", Name = "골든스내퍼", Latin = "Lutjanus johnii", Temper = "비늘마다 금빛 점이 찍힌 퉁돔. 등 뒤쪽 검은 점 하나가 표식, 밤이면 암초 밖으로 나와 새우를 찾는다",
                Maps = M("halong", "vinpearl", "phuket", "kohsamui", "langkawi", "sentosa", "tioman", "telokbatek", "tenggol"),
                MinLen = 20, MaxLen = 90, TrophyLen = 70, TrophyName = "대형 골든스내퍼", WeightCoef = 0.0144f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Shrimp, BaitKind.Squid, BaitKind.Minnow, BaitKind.CutBait },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.95f, Speed = 0.9f, Stamina = 1.2f, Aggression = 0.6f, Exp = 40, Price = 2200 },
            // SEA-M17 갈돔 (스팽글드 엠퍼러): 암초 둘레 모래 바닥 (모든 동남아 바다)
            new FishSpecies { Id = "spangledemperor", Name = "갈돔", Latin = "Lethrinus nebulosus", Temper = "스팽글드 엠퍼러. 비늘마다 파란 반짝이를 단 듯하고, 뺨에 파란 줄이 그어졌다. 모래 바닥을 뒤져 게와 조개를 먹는다",
                Maps = M("halong", "vinpearl", "phuket", "kohsamui", "langkawi", "sentosa", "tioman", "telokbatek", "tenggol"),
                MinLen = 20, MaxLen = 85, TrophyLen = 65, TrophyName = "대형 갈돔", WeightCoef = 0.0137f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Squid, BaitKind.Krill },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.8f, Speed = 0.9f, Stamina = 1.1f, Aggression = 0.55f, Exp = 35, Price = 1800 },
            // SEA-M18 범프헤드 패럿피시: 산호초 초식, 멸종위기 (보호종 컨셉)
            new FishSpecies { Id = "bumpheadparrot", Name = "범프헤드 패럿피시", Latin = "Bolbometopon muricatum", Temper = "혹처럼 튀어나온 이마로 산호를 들이받아 부숴 먹는 거대 비늘돔. 멸종위기 보호종, 경계심이 아주 많다",
                Maps = M("tioman", "tenggol"),
                MinLen = 50, MaxLen = 130, TrophyLen = 110, TrophyName = "산호초의 들소", WeightCoef = 0.021f, Rarity = 5,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 1.3f, Speed = 0.8f, Stamina = 1.8f, Aggression = 0.4f, Exp = 320, Price = 12000 },
            // SEA-M19 나폴레옹피시: 산호초 대형 놀래기, 멸종위기(EN) 보호종
            new FishSpecies { Id = "napoleonwrasse", Name = "나폴레옹피시", Latin = "Cheilinus undulatus", Temper = "나폴레옹 모자 같은 혹이 솟은 이마에 두툼한 입술. 얼굴에 미로 같은 무늬가 그어진 산호초의 거대 놀래기, 멸종위기 보호종",
                Maps = M("tioman", "tenggol"),
                MinLen = 60, MaxLen = 200, TrophyLen = 150, TrophyName = "산호초의 황제", WeightCoef = 0.019f, Rarity = 5,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Shrimp, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.6f, Speed = 0.75f, Stamina = 2.3f, Aggression = 0.45f, Exp = 380, Price = 20000 },

            // ---------- 하구·맹그로브 (랑카위·센토사·텔록바텍) ----------
            // SEA-M22 황소상어: 열대 연안에서 강 하구까지. 연안형이라 먼바다 전용으로 두지 않는다
            new FishSpecies { Id = "bullshark", Name = "황소상어", Latin = "Carcharhinus leucas", Temper = "강을 거슬러 민물까지 올라오는 사나운 상어. 흙탕물 하구에서 피 냄새를 쫓는다. 배에 올릴 때 조심",
                Maps = M("phuket", "langkawi"),
                MinLen = 120, MaxLen = 340, TrophyLen = 280, TrophyName = "하구의 황소", WeightCoef = 0.008f, Rarity = 4,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.CutBait, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.8f, Speed = 1.3f, Stamina = 2.3f, Aggression = 0.8f, Exp = 260, Price = 15000 },
            // SEA-M20 포핑거 스레드핀: 하구·연안 얕은 곳 흙탕물
            new FishSpecies { Id = "threadfin", Name = "스레드핀", Latin = "Eleutheronema tetradactylum", Temper = "가슴지느러미 아래 실 같은 네 가닥으로 흙탕물 바닥을 더듬어 새우를 찾는다. 말레이시아 사람들이 '쿠라우'라 부르는 귀한 생선",
                Maps = M("langkawi", "sentosa", "telokbatek"),
                MinLen = 30, MaxLen = 120, TrophyLen = 90, TrophyName = "대형 스레드핀", WeightCoef = 0.0087f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.0f, Speed = 1.4f, Stamina = 1.2f, Aggression = 0.8f, Exp = 70, Price = 4500 },
            // SEA-M23 레이디피시: 점프
            new FishSpecies { Id = "ladyfish", Name = "레이디피시", Latin = "Elops machnata", Temper = "자이언트 헤링. 걸리자마자 물 위로 몇 번이고 뛰어 바늘을 털어 낸다. 가시가 많아 먹기는 어렵다",
                Maps = M("langkawi", "sentosa", "telokbatek"),
                MinLen = 30, MaxLen = 100, TrophyLen = 80, TrophyName = "대형 레이디피시", WeightCoef = 0.006f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.85f, Speed = 1.6f, Stamina = 1.0f, Aggression = 0.95f, Exp = 40, Price = 1200 },
            // SEA-M25 인도태평양 타폰 (풀잉어): 맹그로브·하구, 점프
            new FishSpecies { Id = "tarpon", Name = "인도태평양 타폰", Latin = "Megalops cyprinoides", Temper = "우리말로 풀잉어. 큰 은빛 비늘이 거울처럼 번쩍인다. 물 위로 공중제비를 돌며 바늘을 턴다",
                Maps = M("langkawi", "sentosa", "telokbatek"),
                MinLen = 25, MaxLen = 110, TrophyLen = 85, TrophyName = "대형 타폰", WeightCoef = 0.009f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.0f, Speed = 1.4f, Stamina = 1.2f, Aggression = 1.0f, Exp = 75, Price = 2500 },
            // SEA-M24 물총고기 (아처피시): 맹그로브
            new FishSpecies { Id = "archerfish", Name = "물총고기", Latin = "Toxotes jaculatrix", Temper = "아처피시. 입으로 물총을 쏘아 맹그로브 잎 위의 벌레를 떨어뜨린다. 그림자만 비쳐도 숨는다",
                Maps = M("langkawi", "sentosa", "telokbatek"),
                MinLen = 8, MaxLen = 30, TrophyLen = 25, TrophyName = "명사수", WeightCoef = 0.02f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Shrimp, BaitKind.Krill, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear },
                Power = 0.3f, Speed = 1.0f, Stamina = 0.6f, Aggression = 0.5f, Exp = 25, Price = 1500 },

            // ---------- 베트남 연안 (하롱베이·빈펄) ----------
            // SEA-M26 병어
            new FishSpecies { Id = "silverpomfret", Name = "병어", Latin = "Pampus argenteus", Temper = "손바닥처럼 납작하고 둥근 은빛 몸. 입이 작아 입질이 아주 약하다",
                Maps = M("halong", "vinpearl"),
                MinLen = 12, MaxLen = 60, TrophyLen = 45, TrophyName = "대병어", WeightCoef = 0.02f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.5f, Speed = 0.8f, Stamina = 0.9f, Aggression = 0.4f, Exp = 25, Price = 2500 },
            // SEA-M28 옐로핀 시브림: 하구·연안의 감성돔 친척
            new FishSpecies { Id = "yellowfinseabream", Name = "옐로핀 시브림", Latin = "Acanthopagrus latus", Temper = "배지느러미와 뒷지느러미가 노란 감성돔 사촌. 물 흐린 날 석회암 섬 그늘에서 크릴을 노린다",
                Maps = M("halong"),
                MinLen = 15, MaxLen = 50, TrophyLen = 40, TrophyName = "4짜 시브림", WeightCoef = 0.022f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.7f, Speed = 0.9f, Stamina = 1.1f, Aggression = 0.55f, Exp = 30, Price = 1800 },
            // SEA-M29 러셀 스내퍼: 하롱베이 등 연안에 흔한 퉁돔
            new FishSpecies { Id = "russellsnapper", Name = "러셀 스내퍼", Latin = "Lutjanus russellii", Temper = "등 뒤쪽에 검은 점 하나를 단 흔한 퉁돔. 양식 그물 밑에 떼 지어 산다",
                Maps = M("halong", "vinpearl", "sentosa"),
                MinLen = 15, MaxLen = 50, TrophyLen = 40, TrophyName = "대형 러셀 스내퍼", WeightCoef = 0.017f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Shrimp, BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.55f, Speed = 0.8f, Stamina = 0.9f, Aggression = 0.55f, Exp = 18, Price = 700 },
            // SEA-M31 독가시치 (래빗피시): 연안 초식. DB가 '직접 배치'로 남긴 것을 연안 낚시터에 둔다
            new FishSpecies { Id = "rabbitfish", Name = "독가시치", Latin = "Siganus fuscescens", Temper = "토끼 같은 입으로 해초를 뜯는 래빗피시. 지느러미 가시에 독이 있어 찔리면 몇 시간 욱신거린다",
                Maps = M("halong", "vinpearl", "kohsamui", "sentosa", "telokbatek"),
                MinLen = 10, MaxLen = 40, TrophyLen = 33, TrophyName = "대형 독가시치", WeightCoef = 0.016f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.4f, Speed = 0.9f, Stamina = 0.8f, Aggression = 0.5f, Exp = 12, Price = 600 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] SeaAsiaMapAdds =
        {
            // SEA-M01 GT: 동남아 모든 바다 (하롱베이에는 이미 있다)
            ("gianttrevally", M("vinpearl", "phuket", "kohsamui", "langkawi", "sentosa", "tioman", "telokbatek", "tenggol")),
            // SEA-M10 꼬치고기
            ("barracuda", M("vinpearl", "phuket", "kohsamui", "tioman", "tenggol")),
            // SEA-M14 얼룩바리
            ("orangegrouper", M("vinpearl", "langkawi", "sentosa", "telokbatek")),
            // SEA-M15 맹그로브잭
            ("mangrovejack", M("vinpearl", "phuket", "kohsamui", "langkawi", "sentosa", "telokbatek")),
        };
    }
}
