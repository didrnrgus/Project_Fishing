using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>동남아 거점 민물 어종 (docs/fish-db/02_동남아_어종DB.md 2-A, SEA-F).</summary>
    public static partial class FishDatabase
    {
        // 낚시터: 앙코르와트(톤레삽 호수·강, Lv.26) · 돈콘(메콩강 4000개 섬, 폭포·여울, Lv.27) · 방콕(짜오프라야강 + 낚시공원, Lv.31)
        // partial 파일끼리는 정적 필드 초기화 순서가 정해져 있지 않다 (FishDatabase.cs의 AnyWeather가 아직 null일 수 있다).
        // 그래서 다른 파일의 정적 필드 대신 매번 새 배열을 돌려주는 속성을 쓴다 (맵 배열도 어종마다 따로 가지게 된다)
        static Weather[] SfAnyWeather => new[] { Weather.Clear, Weather.Cloudy, Weather.Rain };
        static string[] AngkorDon => new[] { "angkor", "donkhone" };
        static string[] AngkorBkk => new[] { "angkor", "bangkok" };
        static string[] SeaFreshAll => new[] { "angkor", "donkhone", "bangkok" };

        static readonly List<FishSpecies> SeaAsiaFreshSpecies = new List<FishSpecies>
        {
            // ================= 메콩의 거인들 (★5) =================
            // SEA-F01 메콩 대형 메기: 세계 최대 담수어. 이빨이 없고 물이끼·식물을 먹는다
            new FishSpecies { Id = "mekonggiantcatfish", Name = "메콩 대형 메기", Latin = "Pangasianodon gigas", Temper = "세상에서 가장 큰 민물고기. 이빨도 없이 물이끼를 먹는 온순한 거인이지만, 걸리면 배째로 끌고 간다",
                Maps = AngkorDon,
                MinLen = 120, MaxLen = 300, TrophyLen = 250, TrophyName = "메콩의 왕", WeightCoef = 0.011f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 1.95f, Speed = 0.9f, Stamina = 2.8f, Aggression = 0.55f, Exp = 560, Price = 62000 },
            // SEA-F02 자이언트 바브: 세계 최대급 잉어, 캄보디아의 나라 물고기
            new FishSpecies { Id = "giantbarb", Name = "자이언트 바브", Latin = "Catlocarpio siamensis", Temper = "캄보디아의 나라 물고기. 앙코르 사원 돌벽에도 새겨진, 머리가 몸의 3분의 1이나 되는 거대 잉어",
                Maps = AngkorDon,
                MinLen = 80, MaxLen = 250, TrophyLen = 200, TrophyName = "톤레삽의 수호신", WeightCoef = 0.016f, Rarity = 5,
                Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.75f, Speed = 0.75f, Stamina = 2.6f, Aggression = 0.45f, Exp = 480, Price = 50000 },
            // SEA-F03 거대 담수 가오리: 원반 폭 2m 넘는 메콩의 가오리. 꼬리에 독가시
            new FishSpecies { Id = "giantstingray", Name = "거대 담수 가오리", Latin = "Urogymnus polylepis", Temper = "수레바퀴만 한 몸으로 강바닥 모래에 납작 붙어 버틴다. 꼬리의 독가시를 조심할 것",
                Maps = M("angkor"),
                MinLen = 100, MaxLen = 240, TrophyLen = 200, TrophyName = "메콩의 그림자", WeightCoef = 0.022f, Rarity = 5,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.Minnow, BaitKind.Worm, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Dawn }, Weathers = SfAnyWeather,
                Power = 2.0f, Speed = 0.5f, Stamina = 3.0f, Aggression = 0.35f, Exp = 600, Price = 65000 },
            // SEA-F09 자이언트 판가시우스: 상어처럼 등지느러미가 높은 메콩의 초대형 육식 메기
            new FishSpecies { Id = "giantpangasius", Name = "자이언트 판가시우스", Latin = "Pangasius sanitwongsei", Temper = "상어처럼 높이 솟은 등지느러미를 단 메콩의 거대 메기. 밤이면 깊은 소에서 올라와 물고기를 사냥한다",
                Maps = AngkorDon,
                MinLen = 100, MaxLen = 280, TrophyLen = 230, TrophyName = "메콩의 밤 사냥꾼", WeightCoef = 0.012f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Shrimp, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 1.9f, Speed = 1.15f, Stamina = 2.6f, Aggression = 0.7f, Exp = 520, Price = 55000 },
            // SEA-F17 아시아 아로와나: 멸종위기 보호종. 톤레삽 서식은 확인되지 않아 앙코르와트의 전설 어종으로
            new FishSpecies { Id = "arowana", Name = "아시아 아로와나", Latin = "Scleropages formosus", Temper = "용의 비늘을 두른 물고기. 수면 위 벌레를 노려 물 밖으로 솟구친다. 행운을 부른다는 전설의 고기",
                Maps = M("angkor"),
                MinLen = 40, MaxLen = 90, TrophyLen = 75, TrophyName = "금룡", WeightCoef = 0.008f, Rarity = 5,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Fly },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening }, Weathers = new[] { Weather.Clear },
                Power = 1.05f, Speed = 1.4f, Stamina = 1.5f, Aggression = 0.9f, Exp = 320, Price = 35000 },

            // ================= 대물 (★4) =================
            // SEA-F08 줄리엔 황금 잉어 (시암 연어 잉어): 멸종위기. 산란철 메콩 여울 자갈밭
            new FishSpecies { Id = "julliencarp", Name = "줄리엔 황금 잉어", Latin = "Probarbus jullieni", Temper = "금빛 옆구리에 검은 세로줄 다섯 가닥. 산란철엔 메콩 여울 자갈밭으로 모여들어 힘차게 내달린다",
                Maps = AngkorDon,
                MinLen = 50, MaxLen = 150, TrophyLen = 120, TrophyName = "황금 대잉어", WeightCoef = 0.018f, Rarity = 4,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = SfAnyWeather,
                Power = 1.55f, Speed = 1.3f, Stamina = 2.0f, Aggression = 0.75f, Exp = 220, Price = 18000 },
            // SEA-F18 군치: 급류·바위 바닥의 야행성 대형 메기. 돈콘 폭포 아래
            new FishSpecies { Id = "goonch", Name = "군치", Latin = "Bagarius yarrelli", Temper = "폭포 아래 바위 틈에 숨은 악마 메기. 바늘 같은 이빨이 줄지어 났고, 급류에 몸을 박고 버틴다",
                Maps = M("donkhone"),
                MinLen = 50, MaxLen = 200, TrophyLen = 150, TrophyName = "폭포의 악마", WeightCoef = 0.011f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Shrimp, BaitKind.Worm, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 1.75f, Speed = 0.8f, Stamina = 2.2f, Aggression = 0.6f, Exp = 240, Price = 20000 },
            // SEA-F19 피라루쿠 (아라파이마): 태국 낚시공원의 외래 대형어 (원산지 아마존)
            new FishSpecies { Id = "arapaima", Name = "피라루쿠", Latin = "Arapaima gigas", Temper = "아마존에서 건너온 낚시공원의 거인. 숨을 쉬러 수면에 오를 때마다 '쿵' 하고 물이 울린다",
                Maps = M("bangkok"),
                MinLen = 90, MaxLen = 250, TrophyLen = 200, TrophyName = "아마존의 거인", WeightCoef = 0.011f, Rarity = 4,
                Habitats = new[] { Habitat.Deep, Habitat.Weed }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = SfAnyWeather,
                Power = 1.85f, Speed = 1.0f, Stamina = 2.4f, Aggression = 0.7f, Exp = 260, Price = 24000 },
            // SEA-F20 앨리게이터 가: 낚시공원 외래종. 이빨이 날카롭다
            new FishSpecies { Id = "alligatorgar", Name = "앨리게이터 가", Latin = "Atractosteus spatula", Temper = "악어 같은 주둥이에 이빨이 두 줄로 났다. 갑옷 같은 비늘이 단단해 바늘이 잘 박히지 않는다",
                Maps = M("bangkok"),
                MinLen = 80, MaxLen = 220, TrophyLen = 180, TrophyName = "악어 주둥이", WeightCoef = 0.0066f, Rarity = 4,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.CutBait },
                Times = new[] { TimeBand.Day, TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 1.55f, Speed = 1.1f, Stamina = 2.0f, Aggression = 0.8f, Exp = 230, Price = 20000 },

            // ================= 중간 (★3) =================
            // SEA-F04 자이언트 스네이크헤드 (톤): 침수림 수초대의 공격적인 포식자, 새끼를 지킨다
            new FishSpecies { Id = "giantsnakehead", Name = "자이언트 스네이크헤드", Latin = "Channa micropeltes", Temper = "새끼 떼를 지키는 어미는 다가오는 것이면 무엇이든 덮친다. 말레이 사람들은 '토만'이라 부른다",
                Maps = SeaFreshAll,
                MinLen = 40, MaxLen = 130, TrophyLen = 100, TrophyName = "미터급 토만", WeightCoef = 0.009f, Rarity = 3,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.35f, Speed = 1.0f, Stamina = 1.6f, Aggression = 0.9f, Exp = 95, Price = 6000 },
            // SEA-F06 줄무늬 메기: 회유성, 떼 지어 다닌다. 야생 개체는 급감
            new FishSpecies { Id = "stripedcatfish", Name = "줄무늬 메기", Latin = "Pangasianodon hypophthalmus", Temper = "상어처럼 생긴 은빛 메기. 떼 지어 다니며 수면에 떨어진 것을 쭉 빨아들인다",
                Maps = AngkorBkk,
                MinLen = 40, MaxLen = 130, TrophyLen = 100, TrophyName = "미터급 줄무늬 메기", WeightCoef = 0.014f, Rarity = 3,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 1.3f, Speed = 1.1f, Stamina = 1.8f, Aggression = 0.6f, Exp = 85, Price = 5000 },
            // SEA-F11 클라운 나이프피시: 야행성 포식, 수초·그늘
            new FishSpecies { Id = "clownknifefish", Name = "클라운 나이프피시", Latin = "Chitala ornata", Temper = "칼날 같은 몸 옆으로 흰 테를 두른 검은 눈알 무늬가 줄지어 박혔다. 긴 뒷지느러미를 물결치며 밤에 사냥한다",
                Maps = AngkorBkk,
                MinLen = 30, MaxLen = 100, TrophyLen = 80, TrophyName = "대형 나이프피시", WeightCoef = 0.006f, Rarity = 3,
                Habitats = new[] { Habitat.Weed, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Shrimp, BaitKind.Lure },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 0.95f, Speed = 0.9f, Stamina = 1.3f, Aggression = 0.65f, Exp = 85, Price = 5200 },
            // SEA-F12 월라고 (헬리콥터 메기): 야행성 대형 포식 메기
            new FishSpecies { Id = "wallago", Name = "월라고", Latin = "Wallago attu", Temper = "입이 눈 뒤까지 찢어진 사나운 메기. 걸리면 몸을 비틀며 헬리콥터처럼 빙글빙글 돈다",
                Maps = SeaFreshAll,
                MinLen = 50, MaxLen = 180, TrophyLen = 140, TrophyName = "대형 월라고", WeightCoef = 0.0075f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = new[] { Weather.Cloudy, Weather.Rain },
                Power = 1.35f, Speed = 1.0f, Stamina = 1.7f, Aggression = 0.85f, Exp = 110, Price = 7000 },
            // SEA-F14 붉은꼬리 메기: 메콩 본류 바닥 포식
            new FishSpecies { Id = "redtailcatfish", Name = "붉은꼬리 메기", Latin = "Hemibagrus wyckioides", Temper = "꼬리지느러미가 불꽃처럼 붉은 메콩의 바닥 사냥꾼. 여덟 가닥 수염으로 강바닥을 더듬는다",
                Maps = AngkorDon,
                MinLen = 40, MaxLen = 130, TrophyLen = 100, TrophyName = "대형 붉은꼬리 메기", WeightCoef = 0.015f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Shrimp, BaitKind.Worm, BaitKind.CutBait },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = SfAnyWeather,
                Power = 1.35f, Speed = 0.8f, Stamina = 1.8f, Aggression = 0.6f, Exp = 100, Price = 6500 },

            // ================= 흔한 고기 (★1~2) =================
            // SEA-F05 줄무늬 스네이크헤드: 톤레삽 등 수초 지대 상주. 입문 담수 포식자
            new FishSpecies { Id = "stripedsnakehead", Name = "줄무늬 스네이크헤드", Latin = "Channa striata", Temper = "논과 늪 어디에나 사는 동남아의 가물치. 가물어도 진흙 속에서 공기를 마시며 버틴다",
                Maps = SeaFreshAll,
                MinLen = 25, MaxLen = 75, TrophyLen = 60, TrophyName = "대형 스네이크헤드", WeightCoef = 0.009f, Rarity = 2,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Worm },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = SfAnyWeather,
                Power = 0.85f, Speed = 0.85f, Stamina = 1.2f, Aggression = 0.8f, Exp = 40, Price = 2200 },
            // SEA-F13 햄팔라 바브 (정글 퍼치): 여울·급류 포식자
            new FishSpecies { Id = "hampalabarb", Name = "햄팔라 바브", Latin = "Hampala macrolepidota", Temper = "옆구리에 검은 쐐기 무늬를 단 정글의 육식 잉어. 여울에서 루어를 보면 앞뒤 안 가리고 덮친다",
                Maps = SeaFreshAll,
                MinLen = 20, MaxLen = 65, TrophyLen = 50, TrophyName = "대형 햄팔라", WeightCoef = 0.013f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.85f, Speed = 1.3f, Stamina = 1.0f, Aggression = 0.9f, Exp = 45, Price = 2400 },
            // SEA-F15 자이언트 구라미: 호수·연못, 경계심 높음
            new FishSpecies { Id = "giantgourami", Name = "자이언트 구라미", Latin = "Osphronemus goramy", Temper = "두툼한 입술로 물 위에 뜬 풀잎을 오물거린다. 경계심이 많아 그림자만 비쳐도 숨는다",
                Maps = M("bangkok"),
                MinLen = 20, MaxLen = 65, TrophyLen = 50, TrophyName = "대형 구라미", WeightCoef = 0.025f, Rarity = 2,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.8f, Speed = 0.6f, Stamina = 1.2f, Aggression = 0.4f, Exp = 55, Price = 3000 },
            // SEA-F21 파쿠: 피라냐 근연종, 낚시공원 외래종
            new FishSpecies { Id = "pacu", Name = "파쿠", Latin = "Piaractus brachypomus", Temper = "피라냐의 사촌. 사람 이처럼 생긴 어금니로 물에 떨어진 열매와 씨앗을 깨 먹는다",
                Maps = M("bangkok"),
                MinLen = 25, MaxLen = 80, TrophyLen = 60, TrophyName = "대형 파쿠", WeightCoef = 0.035f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = SfAnyWeather,
                Power = 1.1f, Speed = 1.0f, Stamina = 1.4f, Aggression = 0.75f, Exp = 60, Price = 3200 },
            // SEA-F07 실버 바브: 톤레삽 대표 회유 잉어류 ('고기떼' 어군)
            new FishSpecies { Id = "silverbarb", Name = "실버 바브", Latin = "Barbonymus gonionotus", Temper = "톤레삽에 구름처럼 몰려다니는 은빛 잉어. 캄보디아 사람들 밥상의 단골",
                Maps = SeaFreshAll,
                MinLen = 10, MaxLen = 40, TrophyLen = 32, TrophyName = "대형 실버 바브", WeightCoef = 0.016f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = SfAnyWeather,
                Power = 0.45f, Speed = 1.1f, Stamina = 0.8f, Aggression = 0.6f, Exp = 18, Price = 500 },
            // SEA-F10 문라이트 구라미: 톤레삽 북쪽 침수림 주변 소형어
            new FishSpecies { Id = "moonlightgourami", Name = "문라이트 구라미", Latin = "Trichopodus microlepis", Temper = "달빛처럼 은은하게 빛나는 작은 구라미. 실 같은 배지느러미로 물풀 사이를 더듬는다",
                Maps = AngkorBkk,
                MinLen = 6, MaxLen = 15, TrophyLen = 13, TrophyName = "보름달 구라미", WeightCoef = 0.02f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = SfAnyWeather,
                Power = 0.15f, Speed = 0.7f, Stamina = 0.4f, Aggression = 0.4f, Exp = 12, Price = 450 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] SeaAsiaFreshMapAdds =
        {
            // SEA-F16 아시아 바라문디: 방콕 기수역, 랑카위·센토사·텔록바텍 맹그로브 연안
            ("barramundi", new[] { "bangkok", "langkawi", "sentosa", "telokbatek" }),
        };
    }
}
