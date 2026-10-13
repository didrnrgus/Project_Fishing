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
    }

    /// <summary>
    /// 원작 "낚시터 목록(KOREA)" 화면의 국내 낚시터. 지금은 한강만 구현돼 있다.
    /// 위치는 지도에 찍을 대략적인 좌표다.
    /// </summary>
    public static class FishingAreas
    {
        public static readonly List<FishingArea> Korea = new List<FishingArea>
        {
            new FishingArea { Id = "hangang", Name = "한강", Lon = 126.98f, Lat = 37.53f, Open = true, Level = 1, Desc = "서울 한복판의 강. 붕어부터 잉어, 쏘가리까지." },
            new FishingArea { Id = "hyeonhwa", Name = "현화로", Lon = 126.25f, Lat = 33.45f, Level = 18, Desc = "제주 바다의 검은 현무암 지대." },
            new FishingArea { Id = "samcheok", Name = "삼척 앞바다", Lon = 129.3f, Lat = 37.42f, Level = 14, Desc = "동해의 차가운 바다." },
            new FishingArea { Id = "cheongjin", Name = "청진포구", Lon = 129.78f, Lat = 41.78f, Level = 22, Desc = "북쪽 끝 항구." },
            new FishingArea { Id = "deungdae", Name = "동해 등대섬", Lon = 129.55f, Lat = 36.95f, Level = 16, Desc = "등대가 서 있는 작은 섬." },
            new FishingArea { Id = "seocheon", Name = "서천 앞바다", Lon = 126.6f, Lat = 36.05f, Level = 6, Desc = "노을이 아름다운 서해 바다." },
            new FishingArea { Id = "yeongheung", Name = "영흥도", Lon = 126.45f, Lat = 37.25f, Level = 9, Desc = "흐린 날이면 안개가 짙게 깔리는 섬." },
            new FishingArea { Id = "taean", Name = "태안반도 연안", Lon = 126.15f, Lat = 36.75f, Level = 11, Desc = "맑은 날 바다가 유난히 푸르다." },
            new FishingArea { Id = "honghyeon", Name = "홍현 앞바다", Lon = 127.95f, Lat = 34.73f, Level = 12, Desc = "남해의 잔잔한 갯바위 바다." },
            new FishingArea { Id = "yokji", Name = "욕지도", Lon = 128.25f, Lat = 34.63f, Level = 19, Desc = "참돔과 감성돔의 섬." },
            new FishingArea { Id = "geomun", Name = "거문도", Lon = 127.3f, Lat = 34.03f, Level = 24, Desc = "대물을 꿈꾸는 조사들의 성지." },
        };
    }
}
