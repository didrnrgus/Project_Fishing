using System.Collections.Generic;

namespace Fishing.Data
{
    /// <summary>지중해 거점 어종 뒷부분 (docs/fish-db/03_지중해_어종DB.md MED-M23~M44).</summary>
    /// <remarks>
    /// 베네치아에만 사는 유럽 뱀장어(M37)·서대(M42)·아드리아 철갑상어(M43)는 베네치아 낚시터를 만들지 않아 뺐다.
    /// 만새기(M32)는 <c>mahimahi</c>, 숭어(M36)는 원래 있던 <c>mullet</c>이라 낚시터만 더한다.
    /// </remarks>
    public static partial class FishDatabase
    {
        static readonly List<FishSpecies> Mediterranean2Species = new List<FishSpecies>
        {
            // ---------- 암초·난파선 바닥 ----------
            // 유럽 붕장어 (Conger conger): 2m·50kg 넘게 자라는 대형 붕장어. 난파선·바위굴에 살고 밤에 나온다
            new FishSpecies { Id = "europeanconger", Name = "유럽붕장어", Latin = "Conger conger", Temper = "난파선 속에 사는 잿빛 거인 붕장어. 걸리면 꼬리를 쇳덩이에 감고 버틴다",
                Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 60, MaxLen = 230, TrophyLen = 180, TrophyName = "난파선의 주인", WeightCoef = 0.0055f, Rarity = 4,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.55f, Speed = 0.7f, Stamina = 2.0f, Aggression = 0.55f, Exp = 180, Price = 14000 },
            // 붉은 쏨뱅이 (Scorpaena scrofa): 지중해에서 가장 큰 쏨뱅이. 부야베스·카카비아 국거리로 비싸다. 등가시에 독
            new FishSpecies { Id = "redscorpionfish", Name = "붉은쏨뱅이", Latin = "Scorpaena scrofa", Temper = "붉은 바위인 척 엎드린 부야베스의 주인공. 등가시에 독이 있어 맨손으로 쥐면 안 된다",
                Maps = M("santorini", "crete", "duden"),
                MinLen = 20, MaxLen = 50, TrophyLen = 40, TrophyName = "대형 붉은쏨뱅이", WeightCoef = 0.025f, Rarity = 3,
                Habitats = new[] { Habitat.Rocky, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Squid, BaitKind.Shrimp, BaitKind.Minnow },
                Times = new[] { TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.7f, Speed = 0.55f, Stamina = 1.0f, Aggression = 0.4f, Exp = 60, Price = 4200 },
            new FishSpecies { Id = "paintedcomber", Name = "페인티드콤버", Latin = "Serranus scriba", Temper = "얼굴에 파란 낙서 같은 무늬, 배에 푸른 얼룩. 작아도 미끼에 겁 없이 덤빈다",
                Maps = M("santorini", "crete"),
                MinLen = 10, MaxLen = 36, TrophyLen = 28, TrophyName = "대형 콤버", WeightCoef = 0.011f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.3f, Speed = 0.8f, Stamina = 0.6f, Aggression = 0.75f, Exp = 20, Price = 700 },
            new FishSpecies { Id = "rainbowwrasse", Name = "무지개놀래기", Latin = "Coris julis", Temper = "주황 지그재그 띠를 두른 바위밭의 무지개. 밤이면 모래 속에 파묻혀 잔다",
                Maps = M("santorini", "crete"),
                MinLen = 8, MaxLen = 25, TrophyLen = 20, TrophyName = "왕무지개놀래기", WeightCoef = 0.01f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Krill, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.22f, Speed = 1.1f, Stamina = 0.5f, Aggression = 0.7f, Exp = 18, Price = 600 },

            // ---------- 모래 바닥 ----------
            // 일반 노랑가오리 (Dasyatis pastinaca): 길이는 꼬리 끝까지. 꼬리에 독가시
            new FishSpecies { Id = "commonstingray", Name = "지중해노랑가오리", Latin = "Dasyatis pastinaca", Temper = "모래 속에 납작 숨어 눈만 내놓는다. 꼬리 독가시를 조심, 바닥에 들러붙으면 들어 올리기 힘들다",
                Maps = M("zakynthos"),
                MinLen = 50, MaxLen = 140, TrophyLen = 115, TrophyName = "대형 노랑가오리", WeightCoef = 0.0044f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Squid, BaitKind.CutBait, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.0f, Speed = 0.6f, Stamina = 1.5f, Aggression = 0.35f, Exp = 80, Price = 3800 },
            new FishSpecies { Id = "redmullet", Name = "붉은촉수", Latin = "Mullus barbatus", Temper = "턱 밑 두 가닥 수염으로 모래를 더듬어 먹이를 찾는다. 고대 로마 귀족이 금값을 치르던 생선",
                Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 10, MaxLen = 32, TrophyLen = 26, TrophyName = "왕촉수", WeightCoef = 0.012f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.SeaWorm, BaitKind.Shrimp, BaitKind.Krill },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.3f, Speed = 0.8f, Stamina = 0.65f, Aggression = 0.5f, Exp = 22, Price = 1100 },

            // ---------- 상어류 (자킨토스·산토리니) ----------
            new FishSpecies { Id = "smoothhound", Name = "유럽별상어", Latin = "Mustelus mustelus", Temper = "이빨 대신 맷돌 같은 판으로 게를 으깨 먹는 순한 상어. 상어 낚시 입문용이지만 힘은 진짜 상어다",
                Maps = M("zakynthos"),
                MinLen = 60, MaxLen = 160, TrophyLen = 130, TrophyName = "대형 별상어", WeightCoef = 0.0048f, Rarity = 3,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.05f, Speed = 1.05f, Stamina = 1.4f, Aggression = 0.6f, Exp = 85, Price = 4000 },
            // 청새리상어 (Prionace glauca): 먼바다 표층을 떠도는 날씬한 상어. 배로 먼바다에 나가야 문다
            new FishSpecies { Id = "blueshark", Name = "청새리상어", Latin = "Prionace glauca", Temper = "쪽빛 등에 긴 가슴지느러미, 먼바다를 떼 지어 떠도는 상어. 피 냄새를 맡고 배 곁까지 따라온다",
                Maps = M("santorini", "zakynthos"), FarSeaOnly = true,
                MinLen = 150, MaxLen = 340, TrophyLen = 280, TrophyName = "대형 청새리상어", WeightCoef = 0.0042f, Rarity = 4,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 1.5f, Speed = 1.4f, Stamina = 2.0f, Aggression = 0.8f, Exp = 200, Price = 16000 },
            // 청상아리 (Isurus oxyrinchus): 가장 빠른 상어. 걸리면 수면 위로 크게 뛴다. 참다랑어보다 빠르고 사납게
            new FishSpecies { Id = "mako", Name = "청상아리", Latin = "Isurus oxyrinchus", Temper = "가장 빠른 상어. 걸리는 순간 수면 위로 몇 미터씩 솟구치며 바늘을 털어 낸다",
                Maps = M("santorini"), FarSeaOnly = true,
                MinLen = 150, MaxLen = 340, TrophyLen = 290, TrophyName = "바다의 탄환", WeightCoef = 0.0085f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Lure, BaitKind.Squid, BaitKind.Minnow },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 1.8f, Speed = 2.0f, Stamina = 2.3f, Aggression = 0.97f, Exp = 420, Price = 42000 },
            // 환도상어 (Alopias vulpinus): 몸 절반이 위 꼬리. 길이는 꼬리 끝까지라 무게 계수가 작다
            new FishSpecies { Id = "thresher", Name = "환도상어", Latin = "Alopias vulpinus", Temper = "몸길이만 한 낫 같은 꼬리로 정어리 떼를 후려쳐 기절시킨다. 꼬리를 휘두를 때마다 줄이 비명을 지른다",
                Maps = M("zakynthos"), FarSeaOnly = true,
                MinLen = 200, MaxLen = 480, TrophyLen = 400, TrophyName = "칼꼬리의 왕", WeightCoef = 0.0025f, Rarity = 5,
                Habitats = new[] { Habitat.Deep }, Baits = new[] { BaitKind.Minnow, BaitKind.Squid, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 1.75f, Speed = 1.5f, Stamina = 2.5f, Aggression = 0.85f, Exp = 400, Price = 40000 },

            // ---------- 떼고기 (입문 잡어) ----------
            new FishSpecies { Id = "atlanticmackerel", Name = "대서양고등어", Latin = "Scomber colias", Temper = "배 쪽 옆구리에 잿빛 점이 흩뿌려진 지중해 고등어. 떼가 들어오면 바늘마다 줄줄이 달린다",
                Maps = M("santorini", "crete", "zakynthos", "duden"),
                MinLen = 18, MaxLen = 45, TrophyLen = 38, TrophyName = "대고등어", WeightCoef = 0.011f, Rarity = 1,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.Squid, BaitKind.CutBait, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.45f, Speed = 1.5f, Stamina = 0.8f, Aggression = 0.85f, Exp = 22, Price = 700 },
            new FishSpecies { Id = "atlantichorsemackerel", Name = "유럽전갱이", Latin = "Trachurus trachurus", Temper = "옆줄을 따라 방패 비늘이 한 줄로 늘어선 전갱이. 항구 불빛 아래로 밤마다 몰려든다",
                Maps = M("santorini", "crete", "zakynthos"),
                MinLen = 15, MaxLen = 50, TrophyLen = 40, TrophyName = "대전갱이", WeightCoef = 0.011f, Rarity = 1,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow }, Baits = new[] { BaitKind.Krill, BaitKind.CutBait, BaitKind.SeaWorm, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Weathers = AnyWeather,
                Power = 0.38f, Speed = 1.3f, Stamina = 0.75f, Aggression = 0.7f, Exp = 20, Price = 650 },
            // 보그 (Boops boops): 이름이 '소의 눈'. 연안 표층 떼고기
            new FishSpecies { Id = "bogue", Name = "보그", Latin = "Boops boops", Temper = "눈이 커서 '소눈'이라는 이름이 붙은 은빛 도미. 방파제 밑에서 떼로 미끼를 쪼아 댄다",
                Maps = M("crete", "duden"),
                MinLen = 12, MaxLen = 36, TrophyLen = 30, TrophyName = "대형 보그", WeightCoef = 0.008f, Rarity = 1,
                Habitats = new[] { Habitat.Shallow, Habitat.Deep }, Baits = new[] { BaitKind.Krill, BaitKind.SeaWorm, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.3f, Speed = 1.2f, Stamina = 0.6f, Aggression = 0.75f, Exp = 18, Price = 500 },

            // ---------- 홍해에서 수에즈 운하로 건너온 외래종 (듀덴폭포 · 안탈리아 만) ----------
            // 은빛뺨복어 (Lagocephalus sceleratus): 살·내장에 테트로도톡신. 먹지 못하지만 퇴치 수매값을 쳐 준다
            new FishSpecies { Id = "toadfish", Name = "은빛뺨복어", Latin = "Lagocephalus sceleratus", Temper = "홍해에서 건너온 침입자. 살에도 맹독이 있어 먹으면 안 되고, 앵무새 부리 같은 이빨로 낚싯줄을 끊는다",
                Maps = M("duden"),
                MinLen = 20, MaxLen = 100, TrophyLen = 80, TrophyName = "괴물 복어", WeightCoef = 0.006f, Rarity = 2,
                Habitats = new[] { Habitat.Deep, Habitat.Shallow, Habitat.Rocky }, Baits = new[] { BaitKind.Squid, BaitKind.CutBait, BaitKind.Shrimp, BaitKind.Lure },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.65f, Speed = 1.0f, Stamina = 0.9f, Aggression = 0.85f, Exp = 40, Price = 1200 },
            new FishSpecies { Id = "lionfish", Name = "라이언피시", Latin = "Pterois miles", Temper = "부채 같은 가슴지느러미에 줄무늬 갈기. 등가시마다 독이 있는 홍해 침입자, 잡을수록 바다가 산다",
                Maps = M("duden"),
                MinLen = 10, MaxLen = 35, TrophyLen = 30, TrophyName = "대형 라이언피시", WeightCoef = 0.014f, Rarity = 2,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Minnow, BaitKind.Shrimp, BaitKind.Lure },
                Times = new[] { TimeBand.Dawn, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.3f, Speed = 0.5f, Stamina = 0.6f, Aggression = 0.45f, Exp = 36, Price = 1600 },
            // 청대치 (Fistularia commersonii): 1.5m까지 자라지만 몸이 가늘어 1kg 남짓
            new FishSpecies { Id = "cornetfish", Name = "청대치", Latin = "Fistularia commersonii", Temper = "피리처럼 긴 주둥이로 작은 물고기를 빨아들인다. 몸에 파란 점선이 반짝이는 수에즈 운하 너머의 손님",
                Maps = M("duden"),
                MinLen = 50, MaxLen = 160, TrophyLen = 130, TrophyName = "미터급 청대치", WeightCoef = 0.0004f, Rarity = 2,
                Habitats = new[] { Habitat.Shallow, Habitat.Weed, Habitat.Rocky }, Baits = new[] { BaitKind.Minnow, BaitKind.Lure, BaitKind.Shrimp },
                Times = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Weathers = AnyWeather,
                Power = 0.45f, Speed = 1.2f, Stamina = 0.8f, Aggression = 0.6f, Exp = 34, Price = 1300 },
            // 물결독가시치 (Siganus rivulatus): 독가시치(Siganus fuscescens)의 홍해 친척. 해초를 뜯는 떼고기, 지느러미 가시에 독
            new FishSpecies { Id = "marbledspinefoot", Name = "물결독가시치", Latin = "Siganus rivulatus", Temper = "해초를 뜯어 먹는 홍해 출신 떼고기. 지느러미 가시에 독이 있어 쏘이면 한참 욱신거린다",
                Maps = M("duden"),
                MinLen = 12, MaxLen = 30, TrophyLen = 25, TrophyName = "대형 독가시치", WeightCoef = 0.016f, Rarity = 1,
                Habitats = new[] { Habitat.Rocky, Habitat.Weed }, Baits = new[] { BaitKind.Krill, BaitKind.Shrimp, BaitKind.SeaWorm },
                Times = new[] { TimeBand.Morning, TimeBand.Day }, Weathers = AnyWeather,
                Power = 0.4f, Speed = 1.0f, Stamina = 0.75f, Aggression = 0.55f, Exp = 24, Price = 800 },
        };

        /// <summary>이미 있는 어종이 새로 사는 낚시터 (id, 낚시터들).</summary>
        static readonly (string id, string[] maps)[] Mediterranean2MapAdds =
        {
            ("mahimahi", M("santorini", "crete", "duden")),   // 만새기 (MED-M32, 다른 지중해 파일에서 정의)
            ("mullet", M("zakynthos", "duden")),               // 숭어 (MED-M36): 자킨토스 · 듀덴폭포 하구
        };
    }
}
