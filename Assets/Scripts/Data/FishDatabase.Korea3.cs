using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>한국 거점 추가 어종 3차 — 얇은 낚시터(탄천·동해·서해)를 채우는 실제 한국 어종 (모델: tools/blender/species_korea3.py).</summary>
    public static partial class FishDatabase
    {
        // partial 파일끼리는 정적 필드 초기화 순서가 정해져 있지 않다 (FishDatabase.cs의 AnyWeather가 아직 null일 수 있다).
        // 그래서 이 파일의 목록은 다른 파일의 정적 필드 대신 매번 새 배열을 돌려주는 속성을 쓴다
        static Weather[] K3AnyWeather => new[] { Weather.Clear, Weather.Cloudy, Weather.Rain };

        static readonly List<FishSpecies> Korea3Species = new List<FishSpecies>
        {
            // ---------- 탄천·한강 (토종 민물고기) ----------
            // 꺽지: 한국 고유종. 바위 밑에 숨어 지나가는 고기를 덮치는 작은 육식어. 아가미 뚜껑 끝 청록 테 검은 점이 표지
            new FishSpecies { Id = "auchaperch", Name = "꺽지", Latin = "Coreoperca herzi", Temper = "바위 밑에 숨어 지나가는 고기를 덮치는 한국 고유종. 아가미 뚜껑 끝에 청록 테를 두른 검은 점이 박혀 있고, 텃세가 세서 작은 루어에도 덤빈다",
                Maps = M("tancheon", "hangang"),
                MinLen = 8, MaxLen = 30, TrophyLen = 22, TrophyName = "대꺽지", WeightCoef = 0.016f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Worm, BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.55f, Speed = 0.9f, Stamina = 0.85f, Aggression = 0.8f, Exp = 30, Price = 900 },
            // 쉬리: 한국 고유종. 맑고 빠른 여울에만 사는 2급수 지표종. 금빛·남보라 줄무늬
            new FishSpecies { Id = "splendiddace", Name = "쉬리", Latin = "Coreoleuciscus splendidus", Temper = "맑고 물살 빠른 여울에만 사는 한국 고유종. 주둥이부터 꼬리까지 금빛과 남보라 줄이 나란히 지나고, 바닥을 기듯 무리 지어 다닌다",
                Maps = M("tancheon"),
                MinLen = 6, MaxLen = 15, TrophyLen = 13, TrophyName = "대형", WeightCoef = 0.009f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Worm, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.2f, Speed = 1.35f, Stamina = 0.55f, Aggression = 0.65f, Exp = 22, Price = 500 },
            // 모래무지: 모래 속에 몸을 묻고 바닥 벌레를 빨아 먹는다. 깨끗한 모래 하천의 지표
            new FishSpecies { Id = "pikegudgeon", Name = "모래무지", Latin = "Pseudogobio esocinus", Temper = "모래 속에 몸을 묻고 눈만 내놓는 모래 바닥의 숨바꼭질 선수. 긴 주둥이로 모래를 머금었다 뱉으며 벌레를 골라 먹는다",
                Maps = M("tancheon", "hangang"),
                MinLen = 10, MaxLen = 25, TrophyLen = 21, TrophyName = "대형", WeightCoef = 0.008f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = K3AnyWeather,
                Power = 0.35f, Speed = 0.85f, Stamina = 0.75f, Aggression = 0.45f, Exp = 14, Price = 350 },
            // 동사리: 한국 고유종. 바닥에 엎드려 꼼짝 않다가 입 앞의 먹이를 덥석 무는 야행성 '멍텅구리'
            new FishSpecies { Id = "darksleeper", Name = "동사리", Latin = "Odontobutis platycephala", Temper = "돌 밑에 엎드려 꼼짝 않아 '멍텅구리'라 불리는 한국 고유종. 밤이면 넓적한 머리의 큰 입으로 지나가는 먹이를 덥석 문다",
                Maps = M("tancheon", "hangang"),
                MinLen = 8, MaxLen = 20, TrophyLen = 17, TrophyName = "대형", WeightCoef = 0.013f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.35f, Speed = 0.6f, Stamina = 0.7f, Aggression = 0.55f, Exp = 14, Price = 350 },
            // 미꾸라지: 흙탕물 바닥을 파고드는 수염 다섯 쌍의 미꾸리. 창자로도 숨을 쉰다
            new FishSpecies { Id = "pondloach", Name = "미꾸라지", Latin = "Misgurnus mizolepis", Temper = "진흙 바닥을 파고드는 수염 다섯 쌍의 개구쟁이. 물이 탁해도 수면에 올라와 공기를 삼켜 창자로 숨을 쉰다",
                Maps = M("tancheon", "hangang"),
                MinLen = 8, MaxLen = 20, TrophyLen = 17, TrophyName = "왕미꾸라지", WeightCoef = 0.0045f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 0.2f, Speed = 0.75f, Stamina = 0.55f, Aggression = 0.5f, Exp = 6, Price = 200 },

            // ---------- 동해 (청진·삼척·헌화로·등대섬) ----------
            // 청어: 겨울 동해의 등 푸른 생선. 과메기의 원조
            new FishSpecies { Id = "pacificherring", Name = "청어", Latin = "Clupea pallasii", Temper = "겨울이면 알을 낳으러 동해 연안으로 몰려드는 등 푸른 은빛 떼고기. 원래 과메기는 이 청어로 만들었다",
                Maps = M("cheongjin", "samcheok", "heonhwa"),
                MinLen = 20, MaxLen = 45, TrophyLen = 38, TrophyName = "대청어", WeightCoef = 0.008f, Rarity = 1,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = K3AnyWeather,
                Power = 0.4f, Speed = 1.25f, Stamina = 0.7f, Aggression = 0.7f, Exp = 10, Price = 400 },
            // 꽁치: 가을 동해를 내려오는 뾰족 주둥이 은빛 고기. 밤 불빛에 모인다
            new FishSpecies { Id = "saury", Name = "꽁치", Latin = "Cololabis saira", Temper = "가을이면 찬 바다를 따라 내려오는 뾰족 주둥이 은빛 고기. 밤 불빛에 떼로 모여들어 수면을 튀어 오른다",
                Maps = M("deungdae", "samcheok", "cheongjin"),
                MinLen = 20, MaxLen = 40, TrophyLen = 34, TrophyName = "대꽁치", WeightCoef = 0.0045f, Rarity = 2,
                Seasons = new[] { Season.Autumn }, Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.Squid, BaitKind.Lure },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.3f, Speed = 1.45f, Stamina = 0.6f, Aggression = 0.65f, Exp = 12, Price = 400 },
            // 도치(뚝지): 공처럼 둥글고 물렁한 몸, 배의 빨판으로 바위에 붙는다. 겨울 강원도 별미 (도치 숙회·알탕)
            new FishSpecies { Id = "lumpsucker", Name = "도치", Latin = "Aptocyclus ventricosus", Temper = "공처럼 둥글고 말랑한 몸으로 배의 빨판을 바위에 붙이고 버틴다. 한겨울 알을 낳으러 얕은 바다로 오르는 강원도 겨울 별미",
                Maps = M("cheongjin", "samcheok", "heonhwa"),
                MinLen = 20, MaxLen = 40, TrophyLen = 33, TrophyName = "왕도치", WeightCoef = 0.04f, Rarity = 3,
                Seasons = new[] { Season.Winter }, Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = K3AnyWeather,
                Power = 0.5f, Speed = 0.35f, Stamina = 0.9f, Aggression = 0.25f, Exp = 45, Price = 2500 },
            // 망상어: 알 대신 새끼를 낳는 바다 붕어. 방파제 어디서나 크릴에 덤빈다
            new FishSpecies { Id = "surfperch", Name = "망상어", Latin = "Ditrema temminckii", Temper = "알 대신 새끼를 낳는 바다의 붕어. 방파제 해초 사이에서 크릴만 보면 떼로 달려든다",
                Maps = M("heonhwa", "deungdae", "samcheok", "honghyeon"),
                MinLen = 12, MaxLen = 28, TrophyLen = 24, TrophyName = "대망상어", WeightCoef = 0.022f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = K3AnyWeather,
                Power = 0.35f, Speed = 0.85f, Stamina = 0.7f, Aggression = 0.55f, Exp = 8, Price = 300 },
            // 삼세기(삼숙이): 울퉁불퉁한 머리, 가시 돌기 피부의 둑중개류. 겨울 연안에 알을 낳고 꼼짝 않고 지킨다
            new FishSpecies { Id = "searaven", Name = "삼세기", Latin = "Hemitripterus villosus", Temper = "울퉁불퉁한 얼굴과 가시 돋친 피부로 바닥 돌처럼 숨는 못난이. 강릉에선 '삼숙이'라 부르며 겨울 매운탕으로 친다",
                Maps = M("cheongjin", "samcheok", "heonhwa"),
                MinLen = 20, MaxLen = 40, TrophyLen = 34, TrophyName = "왕삼세기", WeightCoef = 0.022f, Rarity = 3,
                Seasons = new[] { Season.Winter }, Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.CutBait, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.55f, Speed = 0.45f, Stamina = 0.9f, Aggression = 0.4f, Exp = 35, Price = 1500 },

            // ---------- 서해 (서천·영흥도·태안) ----------
            // 참조기: 굴비의 그 고기. 봄 산란철 서해를 거슬러 오르며 부레로 '구구' 운다
            new FishSpecies { Id = "yellowcroaker", Name = "참조기", Latin = "Larimichthys polyactis", Temper = "말려서 굴비가 되는 서해의 황금 생선. 봄이면 떼 지어 서해를 거슬러 오르며 부레로 '구구' 운다",
                Maps = M("seocheon", "yeongheung", "taean"),
                MinLen = 18, MaxLen = 40, TrophyLen = 32, TrophyName = "대조기", WeightCoef = 0.011f, Rarity = 3,
                Seasons = new[] { Season.Spring, Season.Autumn }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.45f, Speed = 0.85f, Stamina = 0.85f, Aggression = 0.55f, Exp = 30, Price = 2500 },
            // 박대: 혀처럼 길쭉한 참서대과 가자미. 서천 박대묵의 그 고기
            new FishSpecies { Id = "tonguesole", Name = "박대", Latin = "Cynoglossus semilaevis", Temper = "혀처럼 길쭉한 몸으로 서해 갯벌 진흙 바닥에 납작 붙어 산다. 껍질로 쑨 서천 박대묵이 별미",
                Maps = M("seocheon", "yeongheung"),
                MinLen = 25, MaxLen = 70, TrophyLen = 55, TrophyName = "대박대", WeightCoef = 0.005f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.45f, Speed = 0.45f, Stamina = 0.85f, Aggression = 0.3f, Exp = 22, Price = 1500 },
            // 양태: 납작한 머리로 모래에 숨어 있다가 큰 입으로 덮치는 매복꾼
            new FishSpecies { Id = "flathead", Name = "양태", Latin = "Platycephalus indicus", Temper = "넓적한 머리를 모래에 묻고 눈만 내놓은 매복꾼. 지나가는 고기를 큰 입으로 덮치고, 걸리면 머리를 흔들며 버틴다",
                Maps = M("taean", "seocheon", "honghyeon", "yokji"),
                MinLen = 30, MaxLen = 90, TrophyLen = 65, TrophyName = "대양태", WeightCoef = 0.007f, Rarity = 2,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.Minnow, BaitKind.SeaWorm, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = K3AnyWeather,
                Power = 0.7f, Speed = 0.6f, Stamina = 1.1f, Aggression = 0.6f, Exp = 30, Price = 2000 },
            // 성대: 날개 같은 청록 가슴지느러미를 펼치고 다리 같은 줄기로 바닥을 걷는다. 부레로 '꾹꾹' 운다
            new FishSpecies { Id = "gurnard", Name = "성대", Latin = "Chelidonichthys spinosus", Temper = "가슴지느러미 아래 세 가닥으로 바닥을 걸어 다니고, 펼치면 청록 날개에 파란 점이 빛난다. 낚아 올리면 부레로 '꾹꾹' 운다",
                Maps = M("taean", "seocheon", "yeongheung", "yokji"),
                MinLen = 20, MaxLen = 40, TrophyLen = 34, TrophyName = "대성대", WeightCoef = 0.011f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = K3AnyWeather,
                Power = 0.45f, Speed = 0.7f, Stamina = 0.8f, Aggression = 0.45f, Exp = 20, Price = 1200 },
            // 홍어: 마름모 원반에 뾰족한 주둥이. 모래펄 바닥에 납작 붙어 있다 끌려 나온다
            new FishSpecies { Id = "skate", Name = "홍어", Latin = "Okamejei kenojei", Temper = "마름모 원반을 펼치고 모래펄 바닥에 납작 붙는다. 걸리면 바닥에 들러붙어 판때기처럼 버텨서 좀처럼 떨어지지 않는다",
                Maps = M("taean", "seocheon"),
                MinLen = 30, MaxLen = 60, TrophyLen = 50, TrophyName = "대홍어", WeightCoef = 0.012f, Rarity = 3,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Squid, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.8f, Speed = 0.5f, Stamina = 1.3f, Aggression = 0.35f, Exp = 50, Price = 4000 },
            // 아귀: 머리 위 낚싯대로 먹이를 꾀는 바다의 낚시꾼. 몸의 반이 입
            new FishSpecies { Id = "anglerfish", Name = "아귀", Latin = "Lophiomus setigerus", Temper = "머리 위 낚싯대를 흔들어 먹이를 꾀는 바다의 낚시꾼. 몸의 반이 입이라 제 몸만 한 고기도 통째로 삼킨다",
                Maps = M("taean", "seocheon", "samcheok", "cheongjin"),
                MinLen = 30, MaxLen = 100, TrophyLen = 75, TrophyName = "대왕 아귀", WeightCoef = 0.016f, Rarity = 3,
                Seasons = new[] { Season.Winter, Season.Spring }, Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.CutBait, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 0.9f, Speed = 0.35f, Stamina = 1.4f, Aggression = 0.3f, Exp = 60, Price = 5000 },

            // ---------- 남해·제주 ----------
            // 보리멸: 여름 모래 해변 원투낚시의 단골. 바닥을 훑는 연한 모랫빛 은색 고기
            new FishSpecies { Id = "sillago", Name = "보리멸", Latin = "Sillago japonica", Temper = "여름 모래 해변의 원투낚시 단골. 뾰족한 주둥이로 모래 속 갯지렁이를 쪼아 먹고, 작아도 손맛이 경쾌하다",
                Maps = M("honghyeon", "yokji", "heonhwa", "taean"),
                MinLen = 12, MaxLen = 30, TrophyLen = 25, TrophyName = "대보리멸", WeightCoef = 0.007f, Rarity = 1,
                Seasons = new[] { Season.Summer }, Habitats = new[] { Habitat.Shallow }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = K3AnyWeather,
                Power = 0.25f, Speed = 1.1f, Stamina = 0.55f, Aggression = 0.6f, Exp = 8, Price = 400 },
            // 졸복: 미끼만 뜯어 가는 얌체 복어. 독이 있어 함부로 먹으면 안 된다
            new FishSpecies { Id = "pantherpuffer", Name = "졸복", Latin = "Takifugu pardalis", Temper = "날카로운 이빨로 미끼만 쏙쏙 뜯어 가는 얌체 복어. 걸려 나오면 배를 빵빵하게 부풀린다. 독이 있어 함부로 먹으면 안 된다",
                Maps = M("honghyeon", "yokji", "geomun", "taean"),
                MinLen = 12, MaxLen = 35, TrophyLen = 28, TrophyName = "대졸복", WeightCoef = 0.025f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Squid },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = K3AnyWeather,
                Power = 0.35f, Speed = 0.6f, Stamina = 0.7f, Aggression = 0.75f, Exp = 7, Price = 300 },
            // 붉바리: 제주·남해 암초의 귀한 바리. 다금바리와 함께 최고급 횟감으로 꼽힌다
            new FishSpecies { Id = "redspottedgrouper", Name = "붉바리", Latin = "Epinephelus akaara", Temper = "적갈색 몸에 주황 점이 촘촘한 귀한 바리. 암초 굴에 숨어 살아 걸리면 굴로 파고들고, 횟감으로는 다금바리와 함께 으뜸",
                Maps = M("jeju", "geomun", "yokji"),
                MinLen = 25, MaxLen = 55, TrophyLen = 45, TrophyName = "왕붉바리", WeightCoef = 0.017f, Rarity = 4,
                Seasons = new[] { Season.Summer, Season.Autumn }, Habitats = new[] { Habitat.Rocky }, Baits = new[] { BaitKind.Shrimp, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = K3AnyWeather,
                Power = 1.0f, Speed = 0.8f, Stamina = 1.4f, Aggression = 0.65f, Exp = 160, Price = 18000 },
            // 날치: 날개 같은 가슴지느러미로 물 위를 수십 미터 활공한다. 여름 제주·남해 먼바다
            new FishSpecies { Id = "flyingfish", Name = "날치", Latin = "Cypselurus agoo", Temper = "날개 같은 가슴지느러미를 펴고 물 위를 수십 미터나 활공한다. 여름 난류를 타고 제주와 남해 섬 앞바다로 몰려든다",
                Maps = M("jeju", "geomun"),
                MinLen = 20, MaxLen = 35, TrophyLen = 31, TrophyName = "대날치", WeightCoef = 0.008f, Rarity = 3,
                Seasons = new[] { Season.Summer }, Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.35f, Speed = 1.6f, Stamina = 0.6f, Aggression = 0.7f, Exp = 30, Price = 800 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] Korea3MapAdds =
        {
            // 청진: 여름 난류를 따라 고등어가 올라오고, 청진 앞바다는 살오징어 어장
            ("mackerel", new[] { "cheongjin" }), ("squid", new[] { "cheongjin" }),
            // 헌화로: 임연수어는 강릉 앞바다 겨울 갯바위 단골
            ("atkamackerel", new[] { "heonhwa" }),
            // 영흥도: 주꾸미는 영흥도 봄·가을 선상낚시의 대표 어종
            ("octopus", new[] { "yeongheung" }),
        };
    }
}
