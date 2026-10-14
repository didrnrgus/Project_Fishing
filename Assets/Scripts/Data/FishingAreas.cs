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
    }

    /// <summary>
    /// 원작 "낚시터 목록(KOREA)" 화면의 국내 낚시터 11곳 (원작 한국 코스 1~24레벨). 모두 열려 있고 레벨로 입장을 막는다.
    /// 한강(Lv.1), 홍현 앞바다(Lv.5, 배를 받는 레벨), 서천 앞바다(Lv.6, 대회 장소)는 맵마다 따로 만든 월드,
    /// 나머지 8곳은 해안 맵 틀(World/CoastMap.cs + KoreaCoasts.cs)로 만든다. 위치는 지도에 찍을 대략적인 좌표다.
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
            new FishingArea { Id = "geomun", Name = "거문도", Lon = 127.3f, Lat = 34.03f, Open = true, Level = 24, MapId = "geomun",
                Desc = "대물을 꿈꾸는 조사들의 성지. 거문도 등대와 백도, 부시리·돌돔·갈치, 먼바다엔 다금바리·돗돔." },
        };

        public static int EntryLevel(string mapId)
        {
            foreach (var a in Korea)
                if (a.MapId == mapId) return a.Level;
            return 1;
        }
    }
}
