using System.Collections.Generic;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>지도(미니맵·지역 지도)에 그릴 지형 표시.</summary>
    public struct MapMark
    {
        public enum Shape { Rect, Circle, Label }
        public Shape Kind;
        public Rect Area;      // 월드 x, z
        public Color Color;
        public string Label;   // Label 모양일 때 글자, 그 외에는 지역 지도에 붙일 이름(선택)
    }

    /// <summary>
    /// 낚시터 하나의 정의: 좌표계, A~H/1~8 같은 격자, 칸별 서식지, 걸을 수 있는 곳과 물, 어군, 월드 생성.
    /// 지금 들어가 있는 낚시터는 <see cref="FieldMap.Current"/>이고, 게임 코드는 <see cref="FieldMap"/>을 통해서만 쓴다.
    /// </summary>
    public abstract class FieldMapDef
    {
        public abstract string Id { get; }
        public abstract string MapName { get; }
        /// <summary>채널 표시용 짧은 이름 (한강, 홍현 앞바다).</summary>
        public abstract string ShortName { get; }
        public abstract string ShopName { get; }
        /// <summary>퀘스트를 보고받는 조합 연락관 안내 (예: "데크 입구의 박주임").</summary>
        public abstract string UnionContact { get; }
        public virtual bool IsSea => false;

        public float WaterY = 0.2f;
        public float LandY = 1.0f;
        /// <summary>물때 폭 (m). 0이면 물이 오르내리지 않는다. 서천 앞바다 0.6</summary>
        public float TideAmplitude;
        /// <summary>물때 가운데 수면 높이 (물때가 있으면 WaterY = BaseWaterY ± 폭).</summary>
        public float BaseWaterY = 0.2f;
        /// <summary>물 색 (깊은 곳, 얕은 곳). a가 0이면 셰이더 기본값 (한강·남해의 청록).</summary>
        public Color WaterDeep, WaterShallow;
        /// <summary>안개를 더 짙게 (흐림·비·새벽에 크게). 영흥도처럼 해무가 잦은 곳.</summary>
        public float FogBoost;
        /// <summary>밤바다 파도 끝 야광충 세기 (0 = 없음, 서천·삼척 1). 물 셰이더 _BioGlow + 캐릭터 둘레 빛 알갱이(BioluminescenceFx).</summary>
        public float Bioluminescence;
        /// <summary>땅이 늘 눈으로 덮여 있다 (발소리 = 눈).</summary>
        public bool SnowGround;

        // 격자 (row 0이 시작 둔치/해안에 가장 가깝다)
        public float GridMinX, GridMinZ, CellW, CellH;
        public int Cols, Rows;
        public float GridMaxZ => GridMinZ + Rows * CellH;
        public float GridMaxX => GridMinX + Cols * CellW;

        /// <summary>배가 다닐 수 있는 범위 (x: MinX~MaxX).</summary>
        public float MinX, MaxX;

        /// <summary>지도에 보이는 범위 (x, z, 너비, 높이).</summary>
        public Rect View;

        public Vector3 Spawn, ShopPos;
        public float ShopRadius = 10f;

        protected Habitat[,] HabitatGrid; // [row, col]
        public readonly List<MapMark> Marks = new List<MapMark>();

        public abstract bool IsWater(Vector3 p);

        /// <summary>
        /// 물가 거품용 해저(물 밑 땅) 높이. 물이 아닌 곳(둔치·방파제)은 땅 높이. 기본: 물이면 수심 3m.
        /// 지형이 있는 맵은 지형 높이를 돌려준다.
        /// </summary>
        public virtual float SeabedAt(float x, float z) => IsWater(new Vector3(x, WaterY, z)) ? BaseWaterY - 3f : LandY;

        /// <summary>
        /// 물속에서 바로 솟은 구조물(방파제) 자리. 해저 텍스처 G 채널로 구워 물 셰이더가 그 둘레의 얕은 물빛·거품을 지운다
        /// (1m 칸이라 벽 둘레 1~2m가 얕은 비탈처럼 보여 방파제가 물에서 떨어져 떠 보였다).
        /// </summary>
        public virtual bool IsBuiltWall(float x, float z) => false;

        /// <summary>해저 높이를 구울 범위 (월드 x, z, 너비, 높이).</summary>
        public virtual Rect SeabedArea => new Rect(MinX, GridMinZ - 40f, MaxX - MinX, GridMaxZ - GridMinZ + 80f);
        public abstract bool IsWalkable(Vector3 p);
        public abstract string AreaName(Vector3 p);

        /// <summary>이 낚시터의 어군을 새로 만든다.</summary>
        public abstract List<FishSchool> CreateSchools();

        /// <summary>월드를 짓는다. 가로등(밤에 켜짐) 목록을 채운다.</summary>
        public abstract Transform BuildWorld(List<Light> lamps, List<Renderer> bulbs);

        public bool TryCell(Vector3 p, out int col, out int row)
        {
            col = Mathf.FloorToInt((p.x - GridMinX) / CellW);
            row = Mathf.FloorToInt((p.z - GridMinZ) / CellH);
            return col >= 0 && col < Cols && row >= 0 && row < Rows && IsWater(p);
        }

        public string CellName(int col, int row) => ((char)('A' + col)).ToString() + (row + 1);

        /// <summary>먼바다: 바다 맵에서 격자 6행 이후. 파도가 높아 고무보트로는 못 가고, 대물이 잘 문다.</summary>
        public bool IsFarSea(Vector3 p) => IsSea && p.z > GridMinZ + CellH * Rows * 0.62f;
        public Habitat HabitatAt(int col, int row) => HabitatGrid[Mathf.Clamp(row, 0, Rows - 1), Mathf.Clamp(col, 0, Cols - 1)];
        public string HabitatName(Habitat h) => Names.Of(h, IsSea);

        public Vector3 CellCenter(int col, int row) =>
            new Vector3(GridMinX + (col + 0.5f) * CellW, WaterY, GridMinZ + (row + 0.5f) * CellH);

        protected void Mark(MapMark.Shape kind, float x, float z, float w, float h, Color color, string label = null) =>
            Marks.Add(new MapMark { Kind = kind, Area = new Rect(x - w * 0.5f, z - h * 0.5f, w, h), Color = color, Label = label });
    }

    /// <summary>지금 들어가 있는 낚시터. 예전 HanRiverMap 정적 API와 같은 이름으로 현재 맵에 위임한다.</summary>
    public static class FieldMap
    {
        public static FieldMapDef Current { get; private set; } = Maps.HanRiver;
        public static void Set(FieldMapDef map) => Current = map;

        public static string Id => Current.Id;
        public static string MapName => Current.MapName;
        public static string ShortName => Current.ShortName;
        public static string ShopName => Current.ShopName;
        public static string UnionContact => Current.UnionContact;
        public static bool IsSea => Current.IsSea;
        public static float WaterY => Current.WaterY;
        public static float LandY => Current.LandY;
        public static float RiverMinZ => Current.GridMinZ;
        public static float RiverMaxZ => Current.GridMaxZ;
        public static float GridMinX => Current.GridMinX;
        public static float CellW => Current.CellW;
        public static float CellH => Current.CellH;
        public static int Cols => Current.Cols;
        public static int Rows => Current.Rows;
        public static float MinX => Current.MinX;
        public static float MaxX => Current.MaxX;
        public static Vector3 Spawn => Current.Spawn;
        public static Vector3 ShopPos => Current.ShopPos;
        public static float ShopRadius => Current.ShopRadius;

        public static bool IsWater(Vector3 p) => Current.IsWater(p);
        public static bool IsWalkable(Vector3 p) => Current.IsWalkable(p);
        public static bool TryCell(Vector3 p, out int col, out int row) => Current.TryCell(p, out col, out row);
        public static string CellName(int col, int row) => Current.CellName(col, row);
        public static Habitat HabitatAt(int col, int row) => Current.HabitatAt(col, row);
        public static string HabitatName(Habitat h) => Current.HabitatName(h);
        public static Vector3 CellCenter(int col, int row) => Current.CellCenter(col, row);
        public static string AreaName(Vector3 p) => Current.AreaName(p);
        public static bool IsFarSea(Vector3 p) => Current.IsFarSea(p);
    }

    public static class Maps
    {
        public static readonly FieldMapDef HanRiver = new HanRiverDef();
        public static readonly FieldMapDef Honghyeon = new HonghyeonDef();
        public static readonly FieldMapDef Seocheon = new SeocheonDef();
        public static readonly FieldMapDef Tancheon = new CoastMapDef(KoreaCoasts.Tancheon());
        // 해안 맵 틀 (CoastMapDef + KoreaCoasts 사양)
        public static readonly FieldMapDef Yeongheung = new CoastMapDef(KoreaCoasts.Yeongheung());
        public static readonly FieldMapDef Taean = new CoastMapDef(KoreaCoasts.Taean());
        public static readonly FieldMapDef Samcheok = new CoastMapDef(KoreaCoasts.Samcheok());
        public static readonly FieldMapDef Deungdae = new CoastMapDef(KoreaCoasts.Deungdae());
        public static readonly FieldMapDef Heonhwa = new CoastMapDef(KoreaCoasts.Heonhwa());
        public static readonly FieldMapDef Yokji = new CoastMapDef(KoreaCoasts.Yokji());
        public static readonly FieldMapDef Jeju = new CoastMapDef(KoreaCoasts.Jeju());
        public static readonly FieldMapDef Cheongjin = new CoastMapDef(KoreaCoasts.Cheongjin());
        public static readonly FieldMapDef Geomun = new CoastMapDef(KoreaCoasts.Geomun());
        // 해외 거점 (OverseasCoasts)
        public static readonly FieldMapDef HaLong = new CoastMapDef(OverseasCoasts.HaLong());
        public static readonly FieldMapDef Santorini = new CoastMapDef(OverseasCoasts.Santorini());
        public static readonly FieldMapDef Nile = new CoastMapDef(OverseasCoasts.Nile());
        // 2026-10-19 원작 동남아 ①②③·지중해 나머지 (World/Maps/*.cs)
        public static readonly FieldMapDef Angkor = new CoastMapDef(OverseasCoasts.Angkor());
        public static readonly FieldMapDef Vinpearl = new CoastMapDef(OverseasCoasts.Vinpearl());
        public static readonly FieldMapDef DonKhone = new CoastMapDef(OverseasCoasts.DonKhone());
        public static readonly FieldMapDef Phuket = new CoastMapDef(OverseasCoasts.Phuket());
        public static readonly FieldMapDef Bangkok = new CoastMapDef(OverseasCoasts.Bangkok());
        public static readonly FieldMapDef KohSamui = new CoastMapDef(OverseasCoasts.KohSamui());
        public static readonly FieldMapDef Langkawi = new CoastMapDef(OverseasCoasts.Langkawi());
        public static readonly FieldMapDef Sentosa = new CoastMapDef(OverseasCoasts.Sentosa());
        public static readonly FieldMapDef TelokBatek = new CoastMapDef(OverseasCoasts.TelokBatek());
        public static readonly FieldMapDef Tioman = new CoastMapDef(OverseasCoasts.Tioman());
        public static readonly FieldMapDef Tenggol = new CoastMapDef(OverseasCoasts.Tenggol());
        public static readonly FieldMapDef Crete = new CoastMapDef(OverseasCoasts.Crete());
        public static readonly FieldMapDef Zakynthos = new CoastMapDef(OverseasCoasts.Zakynthos());
        public static readonly FieldMapDef Duden = new CoastMapDef(OverseasCoasts.Duden());

        /// <summary>열린 낚시터 (입장 레벨 순, 도감 탭·대물 도전 순서).</summary>
        public static readonly FieldMapDef[] All = { HanRiver, Tancheon, Honghyeon, Seocheon, Yeongheung, Taean, Samcheok, Deungdae, Heonhwa, Yokji, Jeju, Cheongjin, Geomun,
            HaLong, Angkor, Vinpearl, DonKhone, Santorini, Phuket, Crete, Bangkok, Nile, KohSamui, Langkawi, Zakynthos, Sentosa, TelokBatek, Duden, Tioman, Tenggol };

        public static FieldMapDef Get(string id)
        {
            foreach (var m in All) if (m.Id == id) return m;
            return HanRiver;
        }
    }

    /// <summary>한강 반포지구. 좌표·서식지는 <see cref="HanRiverMap"/>, 월드는 <see cref="WorldBuilder"/>.</summary>
    public class HanRiverDef : FieldMapDef
    {
        public override string Id => "hangang";
        public override string MapName => HanRiverMap.MapName;
        public override string ShortName => "한강";
        public override string ShopName => "반포 낚시점";
        public override string UnionContact => "데크 입구의 조합 연락관 박주임";

        public HanRiverDef()
        {
            WaterY = HanRiverMap.WaterY;
            LandY = HanRiverMap.LandY;
            GridMinX = HanRiverMap.GridMinX;
            GridMinZ = HanRiverMap.RiverMinZ;
            CellW = HanRiverMap.CellW;
            CellH = HanRiverMap.CellH;
            Cols = HanRiverMap.Cols;
            Rows = HanRiverMap.Rows;
            MinX = HanRiverMap.MinX;
            MaxX = HanRiverMap.MaxX;
            View = new Rect(-160f, -88f, 320f, 176f);
            Spawn = HanRiverMap.Spawn;
            ShopPos = HanRiverMap.ShopPos;
            ShopRadius = HanRiverMap.ShopRadius;
            HabitatGrid = new Habitat[Rows, Cols];
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                HabitatGrid[r, c] = HanRiverMap.HabitatAt(c, r);

            // 지도 표시: 강, 산책로, 수초 지대, 다리, 데크, 낚시점
            var water = new Color(0.1f, 0.22f, 0.33f);
            Mark(MapMark.Shape.Rect, 0f, 0f, 320f, HanRiverMap.RiverMaxZ - HanRiverMap.RiverMinZ, water);
            Mark(MapMark.Shape.Rect, 102f, -49f, 98f, 20f, new Color(0.3f, 0.55f, 0.25f, 0.6f));
            Mark(MapMark.Shape.Rect, HanRiverMap.BridgeX, 0f, 4f, 200f, new Color(0.82f, 0.82f, 0.8f), "동작대교");
            var pier = HanRiverMap.Pier;
            Mark(MapMark.Shape.Rect, pier.center.x, pier.center.y, pier.width, pier.height, new Color(0.72f, 0.52f, 0.3f), "낚시 데크");
            Mark(MapMark.Shape.Circle, ShopPos.x, ShopPos.z, 8f, 8f, new Color(1f, 0.82f, 0.35f), "낚시점");
        }

        // 겨울: 남쪽 둔치 앞 강물이 얼어 걸을 수 있고, 배·찌는 못 간다 (SeasonFx)
        public override bool IsWater(Vector3 p) => HanRiverMap.IsWater(p) && !SeasonFx.OnIce(p);

        /// <summary>낚시 데크는 기둥 위에 뜬 잔교라 밑이 깊은 물이다 (땅으로 보면 데크 둘레에 해변 같은 흰 거품 띠가 생겼다).</summary>
        public override float SeabedAt(float x, float z) =>
            HanRiverMap.OnPier(new Vector3(x, LandY, z)) && z > HanRiverMap.RiverMinZ ? BaseWaterY - 3f : base.SeabedAt(x, z);
        public override bool IsWalkable(Vector3 p) => HanRiverMap.IsWalkable(p) || SeasonFx.OnIce(p);
        public override string AreaName(Vector3 p) => HanRiverMap.AreaName(p);

        public override List<FishSchool> CreateSchools() => new List<FishSchool>
        {
            new FishSchool { Name = "붕어 떼", SpeciesIds = new[] { "crucian", "bluegill", "zacco" },
                PreferredTimes = new[] { TimeBand.Dawn, TimeBand.Morning, TimeBand.Evening }, Col = 4, Row = 0, MaxRow = 2 },
            new FishSchool { Name = "잉어 무리", SpeciesIds = new[] { "carp", "goldkoi" },
                PreferredTimes = new[] { TimeBand.Dawn, TimeBand.Evening, TimeBand.Night }, Col = 3, Row = 1, MaxRow = 2 },
            new FishSchool { Name = "끄리·강준치 떼", SpeciesIds = new[] { "kkeuri", "skygager", "mullet" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Day }, Col = 2, Row = 1, MaxRow = 2 },
            new FishSchool { Name = "메기·동자개", SpeciesIds = new[] { "catfish", "bagrid", "eel" },
                PreferredTimes = new[] { TimeBand.Evening, TimeBand.Night }, Col = 1, Row = 0, MaxRow = 2 },
        };

        public override Transform BuildWorld(List<Light> lamps, List<Renderer> bulbs)
        {
            var builder = new WorldBuilder();
            var root = builder.Build();
            lamps.AddRange(builder.Lamps);
            bulbs.AddRange(builder.LampBulbs);
            return root;
        }
    }
}
