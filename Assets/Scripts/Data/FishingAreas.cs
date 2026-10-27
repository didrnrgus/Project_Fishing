using System.Collections.Generic;

namespace Fishing.Data
{
    public sealed class FishingArea
    {
        public string Id;
        public string Name;
        public float Lon;       // 지도 표시용 경도/위도 (대략)
        public float Lat;
        public bool Open;       // 이번 버전에서 들어갈 수 있는지
        public int Level;       // 원작 기준 대략적인 입장 레벨
        public string Desc;
        /// <summary>실제 맵 아이디 (열린 낚시터만).</summary>
        public string MapId;
        /// <summary>거점 (원작: 한국 → 동남아 → 지중해 → 나일강). <see cref="FishingAreas.Hubs"/>의 Id.</summary>
        public string Hub = "korea";
    }

    /// <summary>거점: 낚시터 목록 화면 왼쪽 위 탭. 지도 범위(경도·위도)는 <see cref="Fishing.UI.RegionMap"/>이 그린다.</summary>
    public sealed class FishingHub
    {
        public string Id, Name, English;
        public int Level;
    }

    /// <summary>
    /// 원작 "낚시터 목록(KOREA)" 화면의 국내 낚시터 11곳 + 원작 이벤트 지역 제주 (한국 코스 1~24레벨). 모두 열려 있고 레벨로 입장을 막는다.
    /// 한강(Lv.1), 홍현 앞바다(Lv.5, 배를 받는 레벨), 서천 앞바다(Lv.6, 대회 장소)는 맵마다 따로 만든 월드,
    /// 나머지 9곳(제주 포함)은 해안 맵 틀(World/CoastMap.cs + KoreaCoasts.cs)로 만든다. 위치는 지도에 찍을 대략적인 좌표다.
    /// </summary>
    public static class FishingAreas
    {
        public static readonly List<FishingArea> Korea = new List<FishingArea>
        {
            new FishingArea { Id = "hangang", Name = "한강", Lon = 126.98f, Lat = 37.53f, Open = true, Level = 1, MapId = "hangang", Desc = "서울 한복판의 강. 붕어부터 잉어, 쏘가리까지." },
            new FishingArea { Id = "heonhwa", Name = "헌화로", Lon = 129.05f, Lat = 37.7f, Open = true, Level = 18, MapId = "heonhwa",
                Desc = "강릉 절벽 바로 아래를 달리는 해안도로와 동해 갯바위. 바다부채길, 대문어 굴, 절벽엔 진달래." },
            new FishingArea { Id = "samcheok", Name = "삼척 앞바다", Lon = 129.3f, Lat = 37.42f, Open = true, Level = 14, MapId = "samcheok",
                Desc = "차고 깊은 쪽빛 동해. 삼척항 방파제, 추암 촛대바위, 밤이면 수평선에 오징어배 집어등. 임연수어·가자미·대구." },
            new FishingArea { Id = "cheongjin", Name = "청진포구", Lon = 129.78f, Lat = 41.78f, Open = true, Level = 22, MapId = "cheongjin",
                Desc = "북쪽 끝 눈 덮인 큰 항구. 명태 덕장과 얼음 조각, 도루묵·대구, 새벽·밤에만 나타나는 명태 어군." },
            new FishingArea { Id = "deungdae", Name = "동해 등대섬", Lon = 129.55f, Lat = 36.95f, Open = true, Level = 16, MapId = "deungdae",
                Desc = "언덕 위 큰 등대가 서 있는 작은 섬. 아주 맑은 물, 열기·방어·오징어, 양옆 갯바위섬." },
            new FishingArea { Id = "seocheon", Name = "서천 앞바다", Lon = 126.6f, Lat = 36.05f, Open = true, Level = 6, MapId = "seocheon",
                Desc = "노을이 아름다운 서해 바다. 홍원항 흰·빨간 등대, 춘장대 갯벌, 동백정. 물때(밀물·썰물)를 읽어야 한다. 조합 낚시 대회가 열리는 곳." },
            new FishingArea { Id = "yeongheung", Name = "영흥도", Lon = 126.45f, Lat = 37.25f, Open = true, Level = 9, MapId = "yeongheung",
                Desc = "흐린 날이면 해무가 짙게 깔리는 섬. 진두항 방파제, 썰물이면 모래톱으로 걸어 들어가는 목섬, 영흥대교." },
            new FishingArea { Id = "taean", Name = "태안반도 연안", Lon = 126.15f, Lat = 36.75f, Open = true, Level = 11, MapId = "taean",
                Desc = "맑은 날 바다가 유난히 푸르다. 만리포 해변과 신두리 사구, 할미·할아비바위. 주꾸미·갑오징어·광어." },
            new FishingArea { Id = "honghyeon", Name = "홍현 앞바다", Lon = 127.95f, Lat = 34.73f, Open = true, Level = 5, MapId = "honghyeon",
                Desc = "남해 다랭이마을 아래 갯바위 바다. 감성돔이 인기, 방파제 등대와 갯바위 섬, 먼바다엔 자이언트 그루퍼." },
            new FishingArea { Id = "yokji", Name = "욕지도", Lon = 128.25f, Lat = 34.63f, Open = true, Level = 19, MapId = "yokji",
                Desc = "참돔과 감성돔의 섬. 바다 위 가두리 낚시터, 펠리컨바위 출렁다리, 가을엔 방어·삼치·갈치." },
            new FishingArea { Id = "jeju", Name = "제주 서귀포", Lon = 126.56f, Lat = 33.24f, Open = true, Level = 21, MapId = "jeju",
                Desc = "원작 이벤트 지역. 검은 현무암 갯바위와 외돌개, 범섬, 주상절리대, 용두암, 동쪽 바다 너머 성산일출봉, 뒤로 한라산. 자리돔·옥돔·무늬오징어, 밤 먼바다엔 다금바리 어군." },
            new FishingArea { Id = "geomun", Name = "거문도", Lon = 127.3f, Lat = 34.03f, Open = true, Level = 24, MapId = "geomun",
                Desc = "대물을 꿈꾸는 조사들의 성지. 거문도 등대와 백도, 부시리·돌돔·갈치, 먼바다엔 다금바리·돗돔." },
        };

        /// <summary>
        /// 해외 거점 (원작: 2012-04 동남아 ① 앙코르와트·하롱베이·빈펄·돈콘, ② 푸켓·방콕·코사무이·랑카위, ③ 센토사·티오만·텔록바텍·탱글,
        /// 지중해 산토리니·크레타·자킨토스·듀덴폭포, 2013-03 나일강 하류). 2026-10-19 원작 전체 맵 목록(docs/14)에 맞춰 14곳을 더했다. 만렙 40.
        /// </summary>
        public static readonly List<FishingArea> Overseas = new List<FishingArea>
        {
            new FishingArea { Id = "halong", Hub = "sea", Name = "하롱베이", Lon = 107.08f, Lat = 20.91f, Open = true, Level = 25, MapId = "halong",
                Desc = "에메랄드 바다에 솟은 석회암 봉우리 수천 개. 수상 마을과 정크선, 바라문디·얼룩바리·GT." },
            new FishingArea { Id = "santorini", Hub = "med", Name = "산토리니", Lon = 25.37f, Lat = 36.46f, Open = true, Level = 28, MapId = "santorini",
                Desc = "칼데라 절벽 위 하얀 마을과 파란 돔. 깊은 바다의 유럽대구·귀족도미, 먼바다엔 참다랑어." },
            new FishingArea { Id = "angkor", Hub = "sea", Name = "앙코르와트", Lon = 103.87f, Lat = 13.41f, Open = true, Level = 26, MapId = "angkor",
                Desc = "천 년 사원의 해자와 톤레삽 호수. 이끼 낀 회랑과 연꽃 봉오리 탑, 메콩 대형 메기·자이언트 바브·거대 가오리." },
            new FishingArea { Id = "vinpearl", Hub = "sea", Name = "빈펄", Lon = 109.24f, Lat = 12.22f, Open = true, Level = 27, MapId = "vinpearl",
                Desc = "나트랑 앞바다 혼째 섬의 리조트 해변. 초가 파라솔과 바다 위 정자, 바다를 건너는 케이블카. 코비아·만새기·코랄 트라우트." },
            new FishingArea { Id = "donkhone", Hub = "sea", Name = "돈콘", Lon = 105.92f, Lat = 14.12f, Open = true, Level = 28, MapId = "donkhone",
                Desc = "메콩강 4천 개 섬 지대의 수상 가옥 마을. 물탱크 탑과 나무 선착장, 여울과 바위섬. 메콩 대형 메기·줄리엔 잉어·자이언트 파항가시우스." },
            new FishingArea { Id = "phuket", Hub = "sea", Name = "푸켓", Lon = 98.34f, Lat = 7.82f, Open = true, Level = 30, MapId = "phuket",
                Desc = "안다만해의 진주. 언덕 위 하얀 대불과 팡아만 석회암 섬, 롱테일 보트. 돛새치·청새치·황다랑어·GT." },
            new FishingArea { Id = "bangkok", Hub = "sea", Name = "방콕", Lon = 100.49f, Lat = 13.74f, Open = true, Level = 31, MapId = "bangkok",
                Desc = "짜오프라야 강가와 낚시공원. 강 건너 새벽 사원 왓 아룬, 롱테일 보트와 수상 버스. 자이언트 스네이크헤드·피라루쿠·앨리게이터 가." },
            new FishingArea { Id = "kohsamui", Hub = "sea", Name = "코사무이", Lon = 100.06f, Lat = 9.57f, Open = true, Level = 32, MapId = "kohsamui",
                Desc = "숲으로 둘러싸인 해변의 섬. 바다 위 섬에 앉은 황금 대불, 할아버지·할머니 바위, 야자수 해변. 와후·만새기·코비아." },
            new FishingArea { Id = "langkawi", Hub = "sea", Name = "랑카위", Lon = 99.85f, Lat = 6.32f, Open = true, Level = 33, MapId = "langkawi",
                Desc = "바다·늪·정글의 섬. 붉은 독수리 상과 산 위 스카이 브리지, 맹그로브 물길. 바라문디·맹그로브 잭·포핑거 스레드핀·GT." },
            new FishingArea { Id = "sentosa", Hub = "sea", Name = "센토사", Lon = 103.82f, Lat = 1.25f, Open = true, Level = 35, MapId = "sentosa",
                Desc = "동남아 여행의 종착역, 해양 스포츠의 중심지. 거대한 머라이언, 바다 건너 마천루, 하늘의 열기구와 요트 마리나. GT·바라문디·스내퍼." },
            new FishingArea { Id = "telokbatek", Hub = "sea", Name = "텔록 바텍", Lon = 103.0f, Lat = 5.1f, Open = true, Level = 36, MapId = "telokbatek",
                Desc = "고즈넉한 동남아 어촌 마을. 해변 망루와 초가 오두막, 색칠한 롱테일 보트, 금빛 노을. 퀸피시·레이디피시·아처피시." },
            new FishingArea { Id = "tioman", Hub = "sea", Name = "플라우 티오만", Lon = 104.16f, Lat = 2.79f, Open = true, Level = 37, MapId = "tioman",
                Desc = "한적하고 평화로운 리조트. 언덕의 둥근 리조트 호텔, 보라 파라솔 백사장, 용의 뿔 봉우리. 돛새치·와후·나폴레옹피시." },
            new FishingArea { Id = "tenggol", Hub = "sea", Name = "플라우 탱글", Lon = 103.68f, Lat = 4.81f, Open = true, Level = 39, MapId = "tenggol",
                Desc = "나만의 특별한 해양 별장. 바다 위 나무 데크 별장과 산호초, 정글 바위섬. 도그투스 참치·범프헤드 패럿피시·코랄 트라우트." },
            new FishingArea { Id = "crete", Hub = "med", Name = "크레타", Lon = 24.02f, Lat = 35.52f, Open = true, Level = 30, MapId = "crete",
                Desc = "지중해에서 가장 큰 섬, 하니아의 베네치아 항구. 돌 등대와 파스텔 집, 둥근 지붕 모스크, 뒤로 하얀 산. 덴텍스·그루퍼·만새기." },
            new FishingArea { Id = "zakynthos", Hub = "med", Name = "자킨토스", Lon = 20.62f, Lat = 37.86f, Open = true, Level = 33, MapId = "zakynthos",
                Desc = "이오니아해의 휴양섬. 하얀 절벽에 둘러싸인 난파선 해변 나바지오와 푸른 동굴. 참다랑어·청상아리·황새치." },
            new FishingArea { Id = "duden", Hub = "med", Name = "듀덴 폭포", Lon = 30.79f, Lat = 36.86f, Open = true, Level = 36, MapId = "duden",
                Desc = "도시 폭포가 절벽에서 바다로 곧장 떨어지는 안탈리아. 뒤로 토로스 산맥. 미거·블루피시, 홍해에서 들어온 은빛뺨복어·쏠배감펭." },
            new FishingArea { Id = "nile", Hub = "africa", Name = "나일강 하류", Lon = 31.23f, Lat = 29.9f, Open = true, Level = 31, MapId = "nile",
                Desc = "야자수 강가와 펠루카, 사막 너머 피라미드. 2m 나일농어와 송곳니 타이거피시가 사는 민물." },
        };

        public static readonly List<FishingHub> Hubs = new List<FishingHub>
        {
            new FishingHub { Id = "korea", Name = "한국", English = "KOREA", Level = 1 },
            new FishingHub { Id = "sea", Name = "동남아", English = "SOUTHEAST ASIA", Level = 25 },
            new FishingHub { Id = "med", Name = "지중해", English = "MEDITERRANEAN", Level = 28 },
            new FishingHub { Id = "africa", Name = "아프리카", English = "AFRICA · NILE", Level = 31 },
        };

        public static IEnumerable<FishingArea> All
        {
            get
            {
                foreach (var a in Korea) yield return a;
                foreach (var a in Overseas) yield return a;
            }
        }

        public static IEnumerable<FishingArea> InHub(string hub)
        {
            foreach (var a in All) if (a.Hub == hub) yield return a;
        }

        public static int EntryLevel(string mapId)
        {
            foreach (var a in All)
                if (a.MapId == mapId) return a.Level;
            return 1;
        }
    }
}
