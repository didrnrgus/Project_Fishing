using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>동남아 거점 어종 2차 (바다 11 · 민물 5). 얇던 돈콘·탱글·텔록바텍·센토사·방콕을 채운다 (모델: tools/blender/species_sea2.py).</summary>
    public static partial class FishDatabase
    {
        // partial 파일끼리는 정적 필드 초기화 순서가 정해져 있지 않다 (FishDatabase.cs의 AnyWeather가 아직 null일 수 있다).
        // 그래서 다른 파일의 정적 필드 대신 매번 새 배열을 돌려주는 속성을 쓴다
        static Weather[] S2AnyWeather => new[] { Weather.Clear, Weather.Cloudy, Weather.Rain };
        static string[] S2Reef => new[] { "phuket", "kohsamui", "tioman", "tenggol" };
        static string[] S2Mangrove => new[] { "langkawi", "sentosa", "telokbatek" };

        static readonly List<FishSpecies> SeaAsia2Species = new List<FishSpecies>
        {
            // ================= 산호초·암초 =================
            // 흑기흉상어 (블랙팁 리프 상어): 산호초 얕은 여울, 등지느러미를 물 밖에 내놓고 다닌다. 겁이 많다
            new FishSpecies { Id = "blacktipreefshark", Name = "흑기흉상어", Latin = "Carcharhinus melanopterus", Temper = "블랙팁 리프 상어. 지느러미 끝이 먹물에 담근 듯 새까맣다. 무릎 깊이 산호초 여울을 등지느러미를 내놓고 돌다가, 해 질 녘이면 사냥에 나선다",
                Maps = M("phuket", "kohsamui", "langkawi", "tioman", "tenggol"),
                MinLen = 70, MaxLen = 180, TrophyLen = 150, TrophyName = "산호초의 파수꾼", WeightCoef = 0.0042f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.CutBait, BaitKind.Squid, BaitKind.Minnow, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = S2AnyWeather,
                Power = 1.4f, Speed = 1.5f, Stamina = 1.6f, Aggression = 0.75f, Exp = 140, Price = 7000 },
            // 골든 트레발리: 맑은 물 산호초·모래 바닥. 이빨 없는 고무 입술로 모래를 빨아 먹는다
            new FishSpecies { Id = "goldentrevally", Name = "골든 트레발리", Latin = "Gnathanodon speciosus", Temper = "이빨 없는 고무 같은 입술을 쭉 내밀어 모래 속 게와 조개를 빨아들인다. 어릴 땐 샛노란 몸에 검은 줄무늬, 크면 은빛 금색이 된다",
                Maps = M("vinpearl", "phuket", "kohsamui", "tioman", "telokbatek", "tenggol"),
                MinLen = 30, MaxLen = 120, TrophyLen = 95, TrophyName = "대형 골든 트레발리", WeightCoef = 0.011f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.Squid, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.2f, Speed = 1.4f, Stamina = 1.4f, Aggression = 0.7f, Exp = 85, Price = 5500 },
            // 타이탄 트리거피시: 산호초의 가장 큰 쥐치류. 산란철 둥지를 지키며 다이버도 문다
            new FishSpecies { Id = "titantriggerfish", Name = "타이탄 트리거피시", Latin = "Balistoides viridescens", Temper = "성게도 깨 먹는 이빨로 낚싯바늘을 갉는다. 바위 틈에 숨어 등가시를 빗장처럼 세우면 손으로 당겨도 빠지지 않는다. 산란철엔 다이버도 쫓아와 문다",
                Maps = S2Reef,
                MinLen = 25, MaxLen = 75, TrophyLen = 60, TrophyName = "산호초의 깡패", WeightCoef = 0.024f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.Squid, BaitKind.SeaWorm, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.95f, Speed = 0.8f, Stamina = 1.3f, Aggression = 0.9f, Exp = 70, Price = 2500 },
            // 타이거 그루퍼 (갈색얼룩바리): 산호초·암초의 매복 사냥꾼. 활어 시장의 고급 바리
            new FishSpecies { Id = "tigergrouper", Name = "타이거 그루퍼", Latin = "Epinephelus fuscoguttatus", Temper = "누런 몸에 갈색 얼룩과 잔점이 호랑이 가죽처럼 얽혔다. 제 굴 앞을 지키다 해 뜰 무렵과 질 무렵에 덮친다. 걸리면 굴로 처박는다",
                Maps = M("halong", "vinpearl", "kohsamui", "telokbatek", "tenggol"),
                MinLen = 30, MaxLen = 120, TrophyLen = 90, TrophyName = "대형 타이거 그루퍼", WeightCoef = 0.017f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.CutBait, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = S2AnyWeather,
                Power = 1.3f, Speed = 0.8f, Stamina = 1.6f, Aggression = 0.6f, Exp = 110, Price = 9000 },
            // 엠퍼러 스내퍼 (레드 엠퍼러): 산호초 둘레 모래·자갈 바닥, 큰 것은 깊은 곳으로
            new FishSpecies { Id = "emperorsnapper", Name = "엠퍼러 스내퍼", Latin = "Lutjanus sebae", Temper = "분홍빛 몸에 붉은 띠 세 줄을 두른 퉁돔의 황제. 클수록 띠가 바래고, 깊은 암초 바닥에서 밤에 올라와 먹이를 찾는다",
                Maps = M("vinpearl", "phuket", "kohsamui", "tioman", "tenggol"),
                MinLen = 30, MaxLen = 110, TrophyLen = 85, TrophyName = "레드 엠퍼러", WeightCoef = 0.022f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Minnow, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = S2AnyWeather,
                Power = 1.4f, Speed = 0.95f, Stamina = 1.8f, Aggression = 0.6f, Exp = 200, Price = 16000 },
            // 말라바 스내퍼 (이칸 메라): 연안·외해 암초와 단단한 펄 바닥. 말레이시아 바닥낚시의 대표 붉은 퉁돔
            new FishSpecies { Id = "malabarsnapper", Name = "말라바 스내퍼", Latin = "Lutjanus malabaricus", Temper = "말레이시아 사람들이 '이칸 메라(붉은 고기)'라 부르는 바닥낚시의 꽃. 밤이면 산호와 해면 둘레로 떼 지어 나와 미끼를 덥석 문다",
                Maps = M("halong", "kohsamui", "langkawi", "sentosa", "telokbatek", "tenggol"),
                MinLen = 25, MaxLen = 100, TrophyLen = 75, TrophyName = "대형 이칸 메라", WeightCoef = 0.016f, Rarity = 3,
                Habitats = new[] { Habitat.Deep, Habitat.Rocky }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Shrimp, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Dawn }, Weathers = S2AnyWeather,
                Power = 1.15f, Speed = 0.95f, Stamina = 1.5f, Aggression = 0.6f, Exp = 110, Price = 8000 },
            // 오리엔탈 스위트립스: 산호초·석호, 낮엔 산호 그늘에서 쉬고 밤에 먹이를 찾는다
            new FishSpecies { Id = "orientalsweetlips", Name = "오리엔탈 스위트립스", Latin = "Plectorhinchus vittatus", Temper = "흰 몸에 검은 세로줄, 노란 지느러미와 두툼한 입술. 낮엔 산호 그늘에 둥둥 떠 쉬다가 밤이 되면 모래 바닥의 새우와 갯지렁이를 찾아 나선다",
                Maps = M("phuket", "langkawi", "tioman", "tenggol"),
                MinLen = 25, MaxLen = 72, TrophyLen = 60, TrophyName = "대형 스위트립스", WeightCoef = 0.015f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.Squid, BaitKind.Krill },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = S2AnyWeather,
                Power = 0.95f, Speed = 0.85f, Stamina = 1.2f, Aggression = 0.5f, Exp = 75, Price = 4500 },

            // ================= 외해 회유어 =================
            // 동갈방어 (레인보우 러너): 외해 표층, 떠다니는 집어 장치(FAD) 둘레에 떼로 머문다
            new FishSpecies { Id = "rainbowrunner", Name = "동갈방어", Latin = "Elagatis bipinnulata", Temper = "레인보우 러너. 짙은 청록 등에 하늘색·노란 줄이 무지개처럼 그어졌다. 부유물 밑에 떼로 머물다 루어를 보면 앞다투어 달려든다",
                Maps = M("vinpearl", "phuket", "kohsamui", "tioman", "tenggol"), FarSeaOnly = true,
                MinLen = 50, MaxLen = 120, TrophyLen = 100, TrophyName = "대형 동갈방어", WeightCoef = 0.0075f, Rarity = 3,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Minnow, BaitKind.Squid },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.2f, Speed = 1.8f, Stamina = 1.4f, Aggression = 0.85f, Exp = 100, Price = 5000 },
            // 인도고등어: 연안 탁한 물의 플랑크톤을 떼 지어 걸러 먹는다. 동남아 밥상의 흔한 생선
            new FishSpecies { Id = "indianmackerel", Name = "인도고등어", Latin = "Rastrelliger kanagurta", Temper = "태국 '쁠라투', 말레이시아 '이칸 켐붕'. 입을 쩍 벌린 채 떼 지어 헤엄치며 플랑크톤을 걸러 먹는다. 가슴지느러미 아래 검은 점이 표식",
                Maps = M("halong", "vinpearl", "kohsamui", "langkawi", "sentosa", "telokbatek", "tenggol"),
                MinLen = 15, MaxLen = 35, TrophyLen = 30, TrophyName = "대형 인도고등어", WeightCoef = 0.011f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp, BaitKind.SeaWorm, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = S2AnyWeather,
                Power = 0.35f, Speed = 1.3f, Stamina = 0.7f, Aggression = 0.7f, Exp = 14, Price = 500 },

            // ================= 맹그로브·갯벌 =================
            // 자이언트 머드스키퍼: 맹그로브 갯벌의 가장 큰 말뚝망둥어. 물 밖에서 피부로 숨쉰다
            new FishSpecies { Id = "giantmudskipper", Name = "자이언트 머드스키퍼", Latin = "Periophthalmodon schlosseri", Temper = "짱뚱어의 사촌. 머리 꼭대기로 튀어나온 두 눈을 굴리며 갯벌 위를 가슴지느러미로 기어 다닌다. 제 땅에 들어온 게를 펄쩍 뛰어 덮친다",
                Maps = S2Mangrove,
                MinLen = 12, MaxLen = 28, TrophyLen = 25, TrophyName = "갯벌의 왕", WeightCoef = 0.012f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 0.3f, Speed = 1.1f, Stamina = 0.6f, Aggression = 0.85f, Exp = 25, Price = 900 },
            // 점박이 스캣: 하구·맹그로브·항구, 어린 것은 강을 거슬러 민물까지 오른다 (짜오프라야 하류)
            new FishSpecies { Id = "spottedscat", Name = "점박이 스캣", Latin = "Scatophagus argus", Temper = "네모나게 납작한 몸에 갈색 점이 흩뿌려졌다. 바다와 강을 오가며 무엇이든 먹는다. 등가시에 독이 있어 찔리면 몇 시간을 앓는다",
                Maps = M("halong", "langkawi", "sentosa", "telokbatek", "bangkok"),
                MinLen = 10, MaxLen = 38, TrophyLen = 30, TrophyName = "대형 스캣", WeightCoef = 0.03f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Shrimp, BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Worm, BaitKind.Paste },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = S2AnyWeather,
                Power = 0.45f, Speed = 0.9f, Stamina = 0.9f, Aggression = 0.6f, Exp = 22, Price = 900 },

            // ================= 메콩·짜오프라야 민물 =================
            // 말레이 마시어 (켈라·엠푸라우): 맑은 급류의 왕. 숲에서 떨어진 열매를 먹는다. 말레이시아에서 가장 비싼 민물고기
            new FishSpecies { Id = "mahseer", Name = "말레이 마시어", Latin = "Tor tambroides", Temper = "말레이시아에서 '켈라', '엠푸라우'라 부르는 급류의 왕. 숲에서 떨어진 열매를 받아먹고 살아 1kg에 수십만 원을 부른다. 걸리면 여울을 거슬러 쏜살같이 내달린다",
                Maps = M("donkhone"),
                MinLen = 30, MaxLen = 100, TrophyLen = 80, TrophyName = "급류의 왕", WeightCoef = 0.012f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky, Habitat.Shallow }, Baits = new[] { BaitKind.Corn, BaitKind.Paste, BaitKind.Worm, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = new[] { Weather.Clear, Weather.Cloudy },
                Power = 1.5f, Speed = 1.45f, Stamina = 1.9f, Aggression = 0.75f, Exp = 230, Price = 26000 },
            // 솔저 리버 바브: 메콩·짜오프라야 본류의 흐르는 물. 태국 낚시꾼의 단골 손맛
            new FishSpecies { Id = "soldierriverbarb", Name = "솔저 리버 바브", Latin = "Cyclocheilichthys enoplos", Temper = "톱니 가시가 박힌 등지느러미를 군인처럼 꼿꼿이 세운 은빛 잉어. 큰 강 흐름 속에서 조개와 수초 뿌리를 뒤진다",
                Maps = M("angkor", "donkhone", "bangkok"),
                MinLen = 20, MaxLen = 74, TrophyLen = 55, TrophyName = "대형 솔저 바브", WeightCoef = 0.014f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Corn, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = S2AnyWeather,
                Power = 0.85f, Speed = 1.15f, Stamina = 1.1f, Aggression = 0.55f, Exp = 40, Price = 2200 },
            // 틴포일 바브: 강·수로의 센 물살을 좋아하는 떼고기. 지느러미가 불붙은 듯 붉다
            new FishSpecies { Id = "tinfoilbarb", Name = "틴포일 바브", Latin = "Barbonymus schwanenfeldii", Temper = "은박지처럼 번쩍이는 몸에 붉은 지느러미. 센 물살 속을 떼 지어 다니며 수초와 벌레를 가리지 않고 먹는 욕심쟁이",
                Maps = M("angkor", "donkhone", "bangkok"),
                MinLen = 10, MaxLen = 35, TrophyLen = 30, TrophyName = "대형 틴포일 바브", WeightCoef = 0.018f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed }, Baits = new[] { BaitKind.Paste, BaitKind.Corn, BaitKind.Worm, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = S2AnyWeather,
                Power = 0.5f, Speed = 1.2f, Stamina = 0.85f, Aggression = 0.65f, Exp = 18, Price = 600 },
            // 스네이크스킨 구라미 (쁠라 살릿): 논·늪·침수림의 고인 물. 태국에서 말려 먹는 생선
            new FishSpecies { Id = "snakeskingourami", Name = "스네이크스킨 구라미", Latin = "Trichopodus pectoralis", Temper = "태국 '쁠라 살릿'. 뱀 껍질 같은 얼룩 띠를 두르고 논과 늪의 고인 물에서 공기를 마시며 산다. 소금에 절여 말린 것이 별미",
                Maps = M("angkor", "bangkok"),
                MinLen = 8, MaxLen = 25, TrophyLen = 20, TrophyName = "대형 살릿", WeightCoef = 0.018f, Rarity = 1,
                Habitats = new[] { Habitat.Weed, Habitat.Shallow }, Baits = new[] { BaitKind.Worm, BaitKind.Paste, BaitKind.Fly },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = S2AnyWeather,
                Power = 0.2f, Speed = 0.7f, Stamina = 0.5f, Aggression = 0.45f, Exp = 13, Price = 500 },
            // 마블 고비 (순혹): 가장 큰 망둑어류. 바닥에 꼼짝 않고 붙어 있다 밤에 덮친다. 중국계 사람들이 귀하게 여기는 비싼 생선
            new FishSpecies { Id = "marblegoby", Name = "마블 고비", Latin = "Oxyeleotris marmorata", Temper = "대리석 무늬 몸으로 강바닥 돌과 하나가 되어 꼼짝 않는다. 밤이 되면 큰 입으로 지나가는 물고기를 한입에 삼킨다. 싱가포르에선 '순혹'이라 부르는 귀한 생선",
                Maps = M("angkor", "bangkok"),
                MinLen = 15, MaxLen = 65, TrophyLen = 50, TrophyName = "대형 순혹", WeightCoef = 0.011f, Rarity = 3,
                Habitats = new[] { Habitat.Weed, Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Shrimp, BaitKind.Worm },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = S2AnyWeather,
                Power = 0.8f, Speed = 0.6f, Stamina = 1.1f, Aggression = 0.75f, Exp = 70, Price = 6500 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] SeaAsia2MapAdds =
        {
            // 줄무늬 메기: 건기마다 콘 폭포의 물고기 길로 떼 지어 거슬러 오른다
            ("stripedcatfish", M("donkhone")),
            // 클라운 나이프피시: 메콩 본류 깊은 소
            ("clownknifefish", M("donkhone")),
            // 자이언트 구라미: 메콩 유역 토착종 (톤레삽 수초 지대)
            ("giantgourami", M("angkor")),
            // 날쌔기: 테렝가누 앞바다 섬 둘레
            ("cobia", M("tenggol")),
            // 러셀 스내퍼: 말레이시아·타이만 연안 어디에나 흔하다
            ("russellsnapper", M("kohsamui", "telokbatek")),
            // 동갈삼치 ('이칸 텡기리'): 말라카 해협·싱가포르 해협의 대표 회유어
            ("narrowbarredmackerel", M("sentosa", "telokbatek")),
        };
    }
}
