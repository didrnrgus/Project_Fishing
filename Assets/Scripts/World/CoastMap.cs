using System;
using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>해안선 한 구간의 모양: 모래 해변, 갯바위, 콘크리트 호안(포구), 갯벌.</summary>
    public enum ShoreKind { Beach, Rock, Quay, Mud }

    /// <summary>곶·섬 위에 세우는 것.</summary>
    public enum Landmark { None, Pavilion, Lighthouse, GrandLighthouse, Shrine, MukhoLighthouse, GeomunLighthouse }

    /// <summary>물 위 시설: 가두리 양식장(통로만 걷는다), 해안 데크길(부채길), 잔교.</summary>
    public enum PlatformKind { FishFarm, Boardwalk, Pier }

    /// <summary>맵마다 켜는 특별 풍경.</summary>
    [Flags]
    public enum CoastFeature
    {
        None = 0,
        SquidBoats = 1,       // 밤바다 수평선의 오징어잡이 배 집어등
        DryingRacks = 2,      // 명태 덕장
        SeaBridge = 4,        // 바다를 건너는 사장교 (영흥대교)
        Cranes = 8,           // 항구 크레인·창고·트롤선
        FarRocks = 16,        // 먼바다 바위섬 무리 (백도)
        SuspensionBridge = 32,// 곶과 바위기둥을 잇는 출렁다리
        Dunes = 64,           // 해변 뒤 모래 언덕 (신두리 사구)
        Mountains = 128,      // 육지 뒤 높은 산줄기 (동해안)
        Azaleas = 256,        // 절벽에 핀 진달래 (헌화가)
        IceFloes = 512,       // 물 위에 떠다니는 얼음 조각 (청진포구)
        Hallasan = 1024,      // 육지 뒤로 솟은 큰 화산 (한라산)
        DolHareubang = 2048,  // 돌하르방 (포구·낚시점 앞)
        Tangerines = 4096,    // 돌담 두른 귤밭
        Columnar = 8192,      // 첫 곶 둘레의 육각 주상절리 기둥
        Karst = 16384,        // 수평선까지 이어진 석회암 봉우리 (하롱베이)
        Junks = 32768,        // 주황 돛 정크선 (하롱베이)
        FloatingVillage = 65536, // 수상 가옥 마을 (하롱베이)
        Caldera = 131072,     // 바다 건너 칼데라 절벽 위 하얀 마을, 가운데 검은 화산섬 (산토리니)
        Windmills = 262144,   // 절벽 위 하얀 풍차 (키클라데스)
        Pyramids = 524288,    // 사막 너머 피라미드 (나일강 하류)
        Feluccas = 1048576,   // 큰 삼각돛 펠루카 (나일강)
    }

    /// <summary>
    /// 마을 집 양식: 한국 바닷가 / 키클라데스(하얀 집·파란 돔) / 이집트(흙벽돌 평지붕) / 베트남(좁고 높은 색깔 집) /
    /// 수상·고상 가옥(기둥 위 판잣집, 메콩·말레이 어촌) / 리조트(흰 빌라·초가 방갈로) / 타이(가파른 겹지붕 티크 집) / 베네치아풍(파스텔 회벽·붉은 기와, 크레타·안탈리아).
    /// </summary>
    public enum HouseStyle { Korean, Cyclades, Egypt, Vietnam, Stilt, Resort, Thai, Venetian }

    /// <summary>나무 양식: 한국(소나무·활엽수) / 야자수 / 열대(야자수 + 활엽수) / 지중해(작고 둥근 올리브).</summary>
    public enum TreeStyle { Korean, Palm, Tropical, Mediterranean }

    public struct ShoreSeg
    {
        public float X0, X1;
        public ShoreKind Kind;
        public ShoreSeg(float x0, float x1, ShoreKind kind) { X0 = x0; X1 = x1; Kind = kind; }
    }

    public struct BreakwaterSpec
    {
        /// <summary>해안에서 바다(+Z)로 뻗는 몸통 (x, z, 너비, 길이).</summary>
        public Rect Stem;
        /// <summary>끝에서 옆으로 꺾인 팔 (너비 0이면 없음).</summary>
        public Rect Arm;
        /// <summary>등대 몸통 색 (a = 0이면 등대 없음). 등대는 팔 끝(없으면 몸통 끝)에 선다.</summary>
        public Color Light;
        public string Name;
        /// <summary>파도막이 벽이 서쪽(true)/동쪽에. 테트라포드는 반대쪽(바깥).</summary>
        public bool ParapetWest;
    }

    public struct CapeSpec
    {
        public Vector2 Base, Tip;
        public float WidthBase, WidthTip, Height;
        public string Name;
        public Landmark Landmark;
        /// <summary>곶 위 숲 (동백·소나무).</summary>
        public bool Forest;
    }

    public struct IsletSpec
    {
        public Vector3 Center;
        public float Radius, Height;
        public bool Walkable;
        public string Name;
        public Landmark Landmark;
        /// <summary>해안과 섬을 잇는 모래톱: 썰물 때만 드러나 걸어 들어간다 (영흥도 목섬).</summary>
        public bool SandBar;
        public bool Trees;
    }

    /// <summary>걸어 올라갈 수 없는 바위기둥 (촛대바위, 할미·할아비바위).</summary>
    public struct StackSpec
    {
        public Vector3 Pos;
        public float Radius, Height;
        public string Name;
        public bool Pines;
    }

    public struct PlatformSpec
    {
        public Rect Area;
        public PlatformKind Kind;
        public string Name;
    }

    public struct NpcSpec
    {
        public Vector3 Pos;
        public float Yaw;
        public CharacterLook Look;
        public NpcAngler.Role Role;
        public string Id;
    }

    /// <summary>
    /// 바닷가 낚시터 한 곳의 사양. <see cref="CoastMapDef"/>가 이것으로 지형·판정·월드를 만든다 (<see cref="KoreaCoasts"/>).
    /// 좌표: 해안선이 z ≈ ShoreZ, 바다가 +Z. 격자 A~H × 1~8 (한 칸 40×30m), 1행이 해안 쪽.
    /// </summary>
    public sealed class CoastSpec
    {
        public string Id, MapName, ShortName, ShopName, UnionContact;
        /// <summary>육지·바다 기본 지역 이름 (지역 지도·HUD).</summary>
        public string LandName, SeaName;
        public Color WaterDeep, WaterShallow;
        /// <summary>물때 폭 (서해 0.5~0.6). 0이면 물이 오르내리지 않는다.</summary>
        public float Tide;
        /// <summary>흐린 날·새벽 안개를 더 짙게 (영흥도).</summary>
        public float FogBoost;
        /// <summary>땅이 눈으로 덮여 있다 (청진포구).</summary>
        public bool Snow;
        /// <summary>검은 현무암 갯바위 (제주).</summary>
        public bool DarkRock;
        /// <summary>
        /// 지형 절벽 층 텍스처 (Resources/Terrain). 기본 seacliff(회색 금 간 바위면). 제주 darkrock(현무암), 자킨토스 limecliff(흰 석회암),
        /// 듀덴 canyon(주황 석회화), 산토리니 canyon + 붉은 CliffTint. 예전 기본 rock은 위에서 찍은 풀 낀 바위라 절벽이 이끼 풀밭처럼 보였다.
        /// </summary>
        public string CliffTexture = "seacliff";
        public Color CliffTint;
        /// <summary>모래 층 색 보정 (0이면 그대로). 열대 산호 모래는 크림빛 흰색, 자킨토스 흰 자갈 해변은 더 하얗게.</summary>
        public Color SandTint;
        /// <summary>해변 층 텍스처 (기본 sand). 자킨토스 나바지오는 흰 조약돌(pebbles).</summary>
        public string SandTexture = "sand";
        /// <summary>포구 호안을 콘크리트 대신 돌(사암·석회암, StoneTint 색)로, 계류 말뚝 없이 (앙코르 해자 둑길, 하니아 베네치아 항구).</summary>
        public bool StoneQuay;
        public Color StoneTint = new Color(0.66f, 0.64f, 0.56f);

        public float ShoreZ = -40f;
        /// <summary>해안선 굴곡 (x 중심, 폭, z 이동). +면 바다로 튀어나오고, -면 만.</summary>
        public Vector3[] ShoreBumps = new Vector3[0];
        /// <summary>육지 가로 폭의 절반 (섬). 0이면 맵 끝까지 육지.</summary>
        public float LandHalfWidth;
        public ShoreSeg[] Shores = { new ShoreSeg(-999f, 999f, ShoreKind.Rock) };
        /// <summary>해안선 바로 앞 수심, 먼바다로 갈수록 깊어지는 비율.</summary>
        public float NearDepth = 3f, DepthSlope = 0.06f;
        /// <summary>해안 평지 폭 (길·마을), 그 뒤 언덕·절벽 높이와 오르막 폭.</summary>
        public float FlatWidth = 30f, CliffHeight = 10f, CliffWidth = 40f;
        /// <summary>해안도로 (해안선에서 육지 쪽으로 이만큼). 0이면 길이 없다.</summary>
        public float RoadOffset = 14f;

        public BreakwaterSpec[] Breakwaters = new BreakwaterSpec[0];
        public CapeSpec[] Capes = new CapeSpec[0];
        public IsletSpec[] Islets = new IsletSpec[0];
        public StackSpec[] Stacks = new StackSpec[0];
        public PlatformSpec[] Platforms = new PlatformSpec[0];
        public CoastFeature Features;
        /// <summary>모래 언덕 자리 (Dunes).</summary>
        public Rect DuneArea;

        /// <summary>[row, col], row 0 = 해안 쪽.</summary>
        public Habitat[,] Grid;
        public Vector3 Spawn, ShopPos;
        public float ShopYaw;
        public Color ShopRoof = new Color(0.2f, 0.45f, 0.35f);
        public CharacterLook ShopOwner;
        public NpcSpec[] Npcs = new NpcSpec[0];
        public (string text, Vector3 pos, Color color)[] Signs = new (string, Vector3, Color)[0];
        /// <summary>이름 붙은 자리 (먼저 맞는 것). 곶·섬·방파제·시설 이름은 따로 자동으로.</summary>
        public (Rect area, string name)[] Areas = new (Rect, string)[0];
        /// <summary>먼 섬 (x, z, 너비, 높이).</summary>
        public Vector4[] FarIslands = new Vector4[0];
        /// <summary>마을 건물 수 배율 (섬은 적게), 북쪽 항구풍(회색 큰 건물).</summary>
        public float VillageDensity = 1f;
        public bool NorthPort;
        /// <summary>강: 맞은편 둑이 시작하는 z (0이면 바다). 민물이라 <see cref="River"/>도 켠다 (나일강).</summary>
        public float FarShoreZ;
        public bool River;
        public HouseStyle Houses;
        public TreeStyle TreeKind;
        /// <summary>물가에서 45m 넘게 떨어진 땅은 사막 모래 (이집트).</summary>
        public bool SandLand;
        public Func<List<FishSchool>> Schools;
        public int Seed = 1;
        /// <summary>
        /// 그 맵만의 풍경을 짓는 함수 (2026-10-19 새 해외 맵: 앙코르와트 사원, 머라이언, 나바지오 난파선…). 지형·해안·마을·기본 풍경을 다 지은 뒤 부른다.
        /// <see cref="CoastWorldBuilder"/>의 공개 도우미(Root·Height·Rand·FootingAt·PlaceHouse·PlaceTree…)를 쓴다.
        /// </summary>
        public Action<CoastWorldBuilder> Extra;
        /// <summary>마을 집·나무를 놓지 않을 자리 (x, z, 너비, 깊이) — <see cref="Extra"/>가 지을 랜드마크 자리.</summary>
        public Rect[] Reserved = new Rect[0];
    }

    /// <summary>
    /// 사양(<see cref="CoastSpec"/>)으로 만드는 바닷가 낚시터. 헌화로·삼척·등대섬·청진포구(동해), 영흥도·태안(서해), 욕지도·거문도(남해).
    /// 지형은 해안선(굴곡·섬)과 구간별 해안 모양, 곶·섬·바위기둥을 합친 높이 함수 하나(<see cref="HeightAt"/>)이고,
    /// 걸을 수 있는 곳·물은 그 높이와 구조물(방파제·시설)로 정한다.
    /// </summary>
    public class CoastMapDef : FieldMapDef
    {
        public readonly CoastSpec S;
        public override string Id => S.Id;
        public override string MapName => S.MapName;
        public override string ShortName => S.ShortName;
        public override string ShopName => S.ShopName;
        public override string UnionContact => S.UnionContact;
        public override bool IsSea => !S.River;

        public const float TerrainMinX = -210f, TerrainMinZ = -200f, TerrainSizeX = 420f, TerrainSizeZ = 470f, TerrainBaseY = -14f, TerrainHeight = 64f;

        public CoastMapDef(CoastSpec spec)
        {
            S = spec;
            WaterY = 0.2f;
            BaseWaterY = 0.2f;
            TideAmplitude = spec.Tide;
            LandY = 1.0f;
            WaterDeep = spec.WaterDeep;
            WaterShallow = spec.WaterShallow;
            FogBoost = spec.FogBoost;
            SnowGround = spec.Snow;
            GridMinX = -160f;
            GridMinZ = spec.ShoreZ;
            CellW = 40f;
            CellH = 30f;
            Cols = 8;
            Rows = 8;
            MinX = -200f;
            MaxX = 200f;
            View = new Rect(-180f, -70f, 360f, 300f);
            Spawn = spec.Spawn;
            ShopPos = spec.ShopPos;
            ShopRadius = 10f;
            HabitatGrid = spec.Grid;

            // 지도 표시
            Mark(MapMark.Shape.Rect, 0f, (spec.ShoreZ + 260f) * 0.5f, 400f, 260f - spec.ShoreZ, new Color(0.1f, 0.22f, 0.32f));
            if (spec.LandHalfWidth > 0f)
            {
                Mark(MapMark.Shape.Rect, -spec.LandHalfWidth - 60f, spec.ShoreZ - 20f, 120f, 60f, new Color(0.1f, 0.22f, 0.32f));
                Mark(MapMark.Shape.Rect, spec.LandHalfWidth + 60f, spec.ShoreZ - 20f, 120f, 60f, new Color(0.1f, 0.22f, 0.32f));
            }
            foreach (var seg in spec.Shores)
            {
                if (seg.Kind != ShoreKind.Beach && seg.Kind != ShoreKind.Mud) continue;
                float x0 = Mathf.Max(seg.X0, -195f), x1 = Mathf.Min(seg.X1, 195f);
                Mark(MapMark.Shape.Rect, (x0 + x1) * 0.5f, spec.ShoreZ + (seg.Kind == ShoreKind.Mud ? 25f : 3f), x1 - x0, seg.Kind == ShoreKind.Mud ? 60f : 10f,
                    seg.Kind == ShoreKind.Mud ? new Color(0.36f, 0.33f, 0.26f) : new Color(0.75f, 0.68f, 0.5f));
            }
            var concrete = new Color(0.75f, 0.75f, 0.72f);
            foreach (var b in spec.Breakwaters)
            {
                Mark(MapMark.Shape.Rect, b.Stem.center.x, b.Stem.center.y, b.Stem.width, b.Stem.height, concrete, b.Arm.width > 0f ? null : b.Name);
                if (b.Arm.width > 0f) Mark(MapMark.Shape.Rect, b.Arm.center.x, b.Arm.center.y, b.Arm.width, b.Arm.height, concrete, b.Name);
                if (b.Light.a > 0f)
                {
                    var lp = LighthousePos(b);
                    Mark(MapMark.Shape.Circle, lp.x, lp.y, 5f, 5f, b.Light);
                }
            }
            foreach (var c in spec.Capes)
                Mark(MapMark.Shape.Circle, (c.Base.x + c.Tip.x) * 0.5f, (Mathf.Max(c.Base.y, spec.ShoreZ - 10f) + c.Tip.y) * 0.5f, c.WidthBase * 1.3f,
                    Mathf.Abs(c.Tip.y - Mathf.Max(c.Base.y, spec.ShoreZ - 10f)) + c.WidthTip * 2f, new Color(0.25f, 0.38f, 0.22f), c.Name);
            foreach (var i in spec.Islets)
                Mark(MapMark.Shape.Circle, i.Center.x, i.Center.z, i.Radius * 2f, i.Radius * 2f, new Color(0.42f, 0.4f, 0.33f), i.Name);
            foreach (var st in spec.Stacks)
                Mark(MapMark.Shape.Circle, st.Pos.x, st.Pos.z, st.Radius * 2.4f, st.Radius * 2.4f, new Color(0.5f, 0.46f, 0.4f), st.Name);
            foreach (var pf in spec.Platforms)
                Mark(MapMark.Shape.Rect, pf.Area.center.x, pf.Area.center.y, pf.Area.width, pf.Area.height,
                    pf.Kind == PlatformKind.FishFarm ? new Color(0.35f, 0.55f, 0.65f) : new Color(0.72f, 0.52f, 0.3f), pf.Name);
            if (spec.FarShoreZ > 0f)
                Mark(MapMark.Shape.Rect, 0f, spec.FarShoreZ + 70f, 400f, 140f, new Color(0.36f, 0.42f, 0.24f), "건너편 둑");
            Mark(MapMark.Shape.Circle, ShopPos.x, ShopPos.z, 8f, 8f, new Color(1f, 0.82f, 0.35f), "낚시점");
        }

        public static Vector2 LighthousePos(BreakwaterSpec b) =>
            b.Arm.width > 0f
                ? new Vector2(b.Arm.center.x < b.Stem.center.x ? b.Arm.xMin + 3f : b.Arm.xMax - 3f, b.Arm.center.y)
                : new Vector2(b.Stem.center.x, b.Stem.yMax - 3f);

        // ---------------- 해안선 ----------------

        static float Smooth(float a, float b, float x) => TerrainBuilder.Smooth(a, b, x);

        /// <summary>x에서의 해안선 z. 섬이면 양 끝에서 남쪽으로 크게 꺾여 바다가 육지 옆을 감싼다.</summary>
        public float ShoreAt(float x)
        {
            float z = S.ShoreZ;
            foreach (var b in S.ShoreBumps) z += b.z * Mathf.Exp(-(x - b.x) * (x - b.x) / (2f * b.y * b.y));
            if (S.LandHalfWidth > 0f) z -= 240f * Smooth(S.LandHalfWidth, S.LandHalfWidth + 50f, Mathf.Abs(x));
            return z;
        }

        float KindWeight(float x, ShoreKind kind)
        {
            float w = 0f;
            foreach (var s in S.Shores)
                if (s.Kind == kind) w = Mathf.Max(w, Smooth(s.X0 - 8f, s.X0 + 8f, x) * Smooth(s.X1 + 8f, s.X1 - 8f, x));
            return w;
        }

        /// <summary>강 맞은편 둑의 물가 z (살짝 굽이친다).</summary>
        public float FarShoreAt(float x) => S.FarShoreZ + 10f * Mathf.Sin(x * 0.011f + S.Seed) + 4f * Mathf.Sin(x * 0.031f + 1.7f);

        public bool IsMudAt(float x, float z) => KindWeight(x, ShoreKind.Mud) > 0.5f && z > ShoreAt(x) - 2f && z < ShoreAt(x) + 110f;

        float LandHeight(float x, float back)
        {
            float rise = Smooth(S.FlatWidth, S.FlatWidth + S.CliffWidth, back);
            float hills = (Mathf.PerlinNoise(x * 0.018f + S.Seed, back * 0.02f) - 0.4f) * S.CliffHeight * 0.5f * rise;
            float y = LandY + S.CliffHeight * rise + hills;
            if ((S.Features & CoastFeature.Mountains) != 0) y += Smooth(S.FlatWidth + S.CliffWidth, S.FlatWidth + S.CliffWidth + 70f, back) * 22f;
            return y;
        }

        /// <summary>해안 모양별 높이 (d = 해안선에서 바다 쪽으로 거리, 음수면 육지).</summary>
        float Profile(ShoreKind kind, float x, float z, float d)
        {
            float rough = Mathf.PerlinNoise(x * 0.11f + S.Seed * 3.1f, z * 0.11f) - 0.5f;
            float floor = -S.NearDepth - Mathf.Max(0f, d - 40f) * S.DepthSlope + (Mathf.PerlinNoise(x * 0.03f, z * 0.03f) - 0.5f) * 1.6f;
            switch (kind)
            {
                case ShoreKind.Beach:
                    if (d < -6f) return LandHeight(x, -d);
                    if (d < 2f) return Mathf.Lerp(LandY, 0.75f, Smooth(-6f, 2f, d));
                    return Mathf.Lerp(0.75f, floor, Smooth(2f, 70f, d));
                case ShoreKind.Mud:
                {
                    if (d < -6f) return LandHeight(x, -d);
                    if (d < 2f) return Mathf.Lerp(LandY, 0.72f, Smooth(-6f, 2f, d));
                    float creek = 0.6f * Mathf.Exp(-Mathf.Pow(Mathf.Sin(x * 0.035f + z * 0.02f), 2f) * 30f) * Smooth(0f, 15f, d);
                    float flat = Mathf.Lerp(0.72f, -1.6f, Mathf.Clamp01((d - 2f) / 85f)) - creek + rough * 0.25f;
                    return d < 95f ? flat : Mathf.Lerp(flat, floor, Smooth(95f, 140f, d));
                }
                case ShoreKind.Quay:
                    if (d < 0f) return d < -S.FlatWidth ? LandHeight(x, -d) : LandY;
                    return Mathf.Min(-S.NearDepth - 1f, floor);
                default: // Rock
                    if (d < -2f) return LandHeight(x, -d) + (d > -5f ? Mathf.Max(0f, rough) * 1.2f * Smooth(-5f, -2f, d) : 0f);
                    return Mathf.Lerp(LandY + rough * 0.8f, floor - 0.6f, Smooth(-2f, 12f, d + rough * 4f)) + Mathf.Max(0f, rough) * 1.5f * Smooth(14f, 0f, d);
            }
        }

        /// <summary>곶 등뼈까지 거리, 그 위치의 폭, 등뼈 위 위치(0~1).</summary>
        public static (float dist, float width, float t) CapeDist(CapeSpec c, float x, float z)
        {
            var ab = c.Tip - c.Base;
            float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, z) - c.Base, ab) / ab.sqrMagnitude);
            var p = c.Base + ab * t;
            return (Vector2.Distance(new Vector2(x, z), p), Mathf.Lerp(c.WidthBase, c.WidthTip, t), t);
        }

        public float HeightAt(float x, float z)
        {
            float shore = ShoreAt(x);
            float d = z - shore;
            // 해안 모양 섞기
            float wb = KindWeight(x, ShoreKind.Beach), wm = KindWeight(x, ShoreKind.Mud), wq = KindWeight(x, ShoreKind.Quay);
            float wr = Mathf.Max(0f, 1f - wb - wm - wq);
            float sum = wb + wm + wq + wr;
            float y = 0f;
            if (wb > 0f) y += wb * Profile(ShoreKind.Beach, x, z, d);
            if (wm > 0f) y += wm * Profile(ShoreKind.Mud, x, z, d);
            if (wq > 0f) y += wq * Profile(ShoreKind.Quay, x, z, d);
            if (wr > 0f) y += wr * Profile(ShoreKind.Rock, x, z, d);
            y /= sum;

            if ((S.Features & CoastFeature.Dunes) != 0 && S.DuneArea.Contains(new Vector2(x, z)))
            {
                float edge = Mathf.Min(Mathf.Min(x - S.DuneArea.xMin, S.DuneArea.xMax - x), Mathf.Min(z - S.DuneArea.yMin, S.DuneArea.yMax - z));
                y += (0.5f + 0.5f * Mathf.Sin(x * 0.09f + Mathf.PerlinNoise(x * 0.02f, z * 0.03f) * 4f)) * 3.2f * Smooth(0f, 14f, edge);
            }

            foreach (var c in S.Capes)
            {
                var (cd, w, t) = CapeDist(c, x, z);
                if (cd > w + 14f) continue;
                float top = Mathf.Lerp(c.Height, c.Height * 0.55f, t) + (Mathf.PerlinNoise(x * 0.05f, z * 0.05f) - 0.5f) * 2f;
                float distEdge = cd;
                if (t >= 0.999f) distEdge = Vector2.Distance(new Vector2(x, z), c.Tip);
                float cape = distEdge < w
                    ? Mathf.Lerp(top, LandY + 1.2f, Mathf.Pow(distEdge / w, 2.2f))
                    : Mathf.Lerp(LandY + 1.2f, -6f, Smooth(w, w + 10f, distEdge + (Mathf.PerlinNoise(x * 0.2f, z * 0.2f) - 0.5f) * 4f));
                y = Mathf.Max(y, cape);
            }

            foreach (var i in S.Islets)
            {
                float id = new Vector2(x - i.Center.x, z - i.Center.z).magnitude;
                if (i.SandBar)
                {
                    // 해안에서 섬까지 곧게 이어진 모래톱 (가운데가 BaseWaterY - 0.15: 물이 빠지면 드러난다)
                    var a = new Vector2(i.Center.x, ShoreAt(i.Center.x) + 2f);
                    var b = new Vector2(i.Center.x, i.Center.z);
                    float t = Mathf.Clamp01((z - a.y) / (b.y - a.y));
                    float bd = Mathf.Abs(x - Mathf.Lerp(a.x, b.x, t));
                    if (z > a.y - 4f && z < b.y)
                        y = Mathf.Max(y, Mathf.Lerp(BaseWaterY - 0.15f, -1.5f, Smooth(4f, 14f, bd)));
                }
                if (id > i.Radius + 14f) continue;
                float rough = Mathf.PerlinNoise(x * 0.2f, z * 0.2f) - 0.5f;
                float isl = id < i.Radius - 2.5f
                    ? LandY + rough * 0.4f + Mathf.Exp(-id * id / (i.Radius * i.Radius * 0.25f)) * i.Height
                    : Mathf.Lerp(LandY, -5f, Smooth(i.Radius - 2.5f, i.Radius + 8f, id + rough * 3f));
                y = Mathf.Max(y, isl);
            }

            foreach (var st in S.Stacks)
            {
                float sd = new Vector2(x - st.Pos.x, z - st.Pos.z).magnitude;
                if (sd < st.Radius + 6f) y = Mathf.Max(y, Mathf.Lerp(LandY + 0.5f, -3f, Smooth(st.Radius, st.Radius + 6f, sd)));
            }

            // 강: 맞은편 둑 (완만한 진흙 둑 → 둑 위 평지, 멀어질수록 살짝 높아진다)
            if (S.FarShoreZ > 0f)
            {
                float e = z - FarShoreAt(x);
                if (e > -30f)
                {
                    float bank = Mathf.Lerp(-S.NearDepth - 0.5f, LandY, Smooth(-26f, 0f, e));
                    if (e > 0f) bank += Smooth(0f, 50f, e) * 1.6f + (Mathf.PerlinNoise(x * 0.03f, z * 0.03f) - 0.5f) * 1.2f * Smooth(0f, 20f, e);
                    y = Mathf.Max(y, bank);
                }
            }
            return Mathf.Max(y, TerrainBaseY + 1f);
        }

        // ---------------- 구조물 판정 ----------------

        static bool InRect(Rect r, Vector3 p, float m) => p.x > r.xMin + m && p.x < r.xMax - m && p.z > r.yMin - 1f && p.z < r.yMax - m;

        public bool OnBreakwater(Vector3 p, float margin = 0f)
        {
            foreach (var b in S.Breakwaters)
                if (InRect(b.Stem, p, margin) || (b.Arm.width > 0f && InRect(b.Arm, p, margin))) return true;
            return false;
        }

        /// <summary>시설 위 걸을 수 있는 곳. 가두리는 테두리·가운데 통로만 (그물 안은 물).</summary>
        public bool OnPlatform(Vector3 p, float margin = 0f)
        {
            foreach (var pf in S.Platforms)
            {
                var r = pf.Area;
                if (!(p.x > r.xMin + margin && p.x < r.xMax - margin && p.z > r.yMin + margin && p.z < r.yMax - margin)) continue;
                if (pf.Kind != PlatformKind.FishFarm) return true;
                if (FarmWalkway(r, p.x, p.z, margin)) return true;
            }
            return false;
        }

        /// <summary>가두리 통로: 테두리 1.6m와 칸 사이 통로 (칸 한 변 10m).</summary>
        public static bool FarmWalkway(Rect r, float x, float z, float margin = 0f)
        {
            const float pen = 10f, walk = 1.6f;
            float lx = x - r.xMin, lz = z - r.yMin;
            float mx = Mathf.Repeat(lx, pen + walk), mz = Mathf.Repeat(lz, pen + walk);
            return mx < walk - margin * 0.5f || mz < walk - margin * 0.5f || lx > r.width - walk + margin * 0.5f || lz > r.height - walk + margin * 0.5f;
        }

        bool InStack(Vector3 p, float extra)
        {
            foreach (var st in S.Stacks)
                if (new Vector2(p.x - st.Pos.x, p.z - st.Pos.z).magnitude < st.Radius + extra) return true;
            return false;
        }

        public override bool IsWater(Vector3 p)
        {
            if (p.x <= MinX || p.x >= MaxX || p.z >= 260f || p.z <= TerrainMinZ + 5f) return false;
            if (OnBreakwater(p) || OnPlatform(p) || InStack(p, 0.5f)) return false;
            return HeightAt(p.x, p.z) < WaterY - 0.12f;
        }

        public override bool IsWalkable(Vector3 p)
        {
            if (p.x < MinX + 2f || p.x > MaxX - 2f || p.z < TerrainMinZ + 12f || p.z > 255f) return false;
            if (OnBreakwater(p, 0.5f) || OnPlatform(p, 0.4f)) return true;
            if (InStack(p, 0.3f)) return false;
            float h = HeightAt(p.x, p.z);
            if (TideAmplitude > 0f && h > WaterY + 0.08f && (IsMudAt(p.x, p.z) || SandBarAt(p))) return true; // 썰물에 드러난 갯벌·모래톱
            // 땅은 평균 수면 기준으로 판정한다 (만조에 수면이 0.8m까지 올라와도 마을·포구는 그대로 걷는다)
            return h > BaseWaterY + 0.3f && h > WaterY + 0.05f;
        }

        bool SandBarAt(Vector3 p)
        {
            foreach (var i in S.Islets)
                if (i.SandBar && Mathf.Abs(p.x - i.Center.x) < 6f && p.z > ShoreAt(i.Center.x) && p.z < i.Center.z) return true;
            return false;
        }

        public override string AreaName(Vector3 p)
        {
            foreach (var (area, name) in S.Areas)
                if (area.Contains(new Vector2(p.x, p.z))) return name;
            foreach (var b in S.Breakwaters)
            {
                if (b.Arm.width > 0f && InRect(b.Arm, p, 0f)) return b.Name;
                if (InRect(b.Stem, p, 0f)) return b.Name;
            }
            foreach (var pf in S.Platforms)
                if (pf.Area.Contains(new Vector2(p.x, p.z))) return pf.Name;
            foreach (var st in S.Stacks)
                if (new Vector2(p.x - st.Pos.x, p.z - st.Pos.z).magnitude < st.Radius + 12f) return st.Name;
            foreach (var i in S.Islets)
                if (new Vector2(p.x - i.Center.x, p.z - i.Center.z).magnitude < i.Radius + 6f) return i.Name;
            foreach (var c in S.Capes)
            {
                var (cd, w, _) = CapeDist(c, p.x, p.z);
                if (cd < w + 6f) return c.Name;
            }
            if (p.z < ShoreAt(p.x)) return S.LandName;
            if (S.FarShoreZ > 0f && p.z > FarShoreAt(p.x) - 2f) return "건너편 둑";
            return IsFarSea(p) ? "먼바다" : S.SeaName;
        }

        public override List<FishSchool> CreateSchools() => S.Schools();

        public override float SeabedAt(float x, float z) => OnBreakwater(new Vector3(x, 0f, z)) ? LandY : HeightAt(x, z);
        // 섬 맵은 남쪽까지 바다가 감싸므로 지형 전체
        public override Rect SeabedArea => S.LandHalfWidth > 0f ? new Rect(-205f, -195f, 410f, 455f) : new Rect(-205f, -70f, 410f, 330f);

        public override Transform BuildWorld(List<Light> lamps, List<Renderer> bulbs) => new CoastWorldBuilder(this).Build(lamps, bulbs);
    }

    /// <summary>해안 맵 월드: 바다·지형·해안 구조물·방파제·곶·섬·바위기둥·시설·마을·낚시점·NPC·특별 풍경.</summary>
    public partial class CoastWorldBuilder
    {
        readonly CoastMapDef _map;
        readonly CoastSpec S;
        Transform _root;
        System.Random _rng;
        CoastProps _props;
        List<Light> _lamps;
        List<Renderer> _bulbs;
        Material _rock, _concrete, _wood;

        public CoastWorldBuilder(CoastMapDef map)
        {
            _map = map;
            S = map.S;
        }

        float R(float min, float max) => (float)(min + _rng.NextDouble() * (max - min));
        float H(float x, float z) => _map.HeightAt(x, z);

        public Transform Build(List<Light> lamps, List<Renderer> bulbs)
        {
            _root = new GameObject("World_" + S.Id).transform;
            _rng = new System.Random(S.Seed * 7919 + 13);
            Rocks.Snowy = S.Snow;
            _lamps = lamps;
            _bulbs = bulbs;
            _props = new CoastProps(_root, _rng, lamps, bulbs);
            _rock = S.DarkRock ? Mats.Textured("rock", new Color(0.3f, 0.29f, 0.29f), 1.2f, 0.35f) : Mats.Textured("rock", new Color(0.8f, 0.78f, 0.74f), 1.2f, 0.3f);
            _concrete = Mats.Quay(new Color(0.9f, 0.9f, 0.88f));
            _wood = Mats.Textured("deck", new Color(0.85f, 0.72f, 0.58f), 2f, 0.3f);

            // 섬 맵은 남쪽까지 바다가 감싼다
            WaterSurface.Create(_root, new Vector3(0f, _map.WaterY, 60f), new Vector2(1000f, 760f), 4f);
            BuildTerrain();
            BuildShoreline();
            BuildBreakwaters();
            BuildCapes();
            BuildIslets();
            BuildStacks();
            BuildPlatforms();
            BuildVillage();
            BuildShop();
            BuildFeatures();
            BuildFarIslands();
            BuildFarBank();
            BuildNpcs();
            foreach (var (text, pos, color) in S.Signs) WorldBuilder.AddSignBothSides(_root, text, pos, color);
            Rocks.Snowy = false;
            return _root;
        }

        // ---------------- 지형 ----------------

        void BuildTerrain()
        {
            var layers = new List<TerrainBuilder.LayerSpec>
            {
                new TerrainBuilder.LayerSpec(S.Snow ? "snow" : "grass", 9f, S.Snow ? 0.35f : 0.05f),
                new TerrainBuilder.LayerSpec(S.SandTexture, S.SandTexture == "sand" ? 6f : 2.5f, 0.15f, S.SandTint),
                new TerrainBuilder.LayerSpec(S.CliffTexture, 11f, 0.2f, S.CliffTint),
                new TerrainBuilder.LayerSpec("shore", 7f, 0.3f),
                new TerrainBuilder.LayerSpec("mud", 5f, 0.7f),
            };
            float waterY = _map.BaseWaterY;
            TerrainBuilder.Build(_root, "CoastTerrain", new Rect(CoastMapDef.TerrainMinX, CoastMapDef.TerrainMinZ, CoastMapDef.TerrainSizeX, CoastMapDef.TerrainSizeZ),
                CoastMapDef.TerrainBaseY, CoastMapDef.TerrainHeight, 513, H, layers.ToArray(),
                (x, z, y, steep, w) =>
                {
                    float n = Mathf.PerlinNoise(x * 0.07f, z * 0.07f);
                    float d = z - _map.ShoreAt(x);
                    float rock = Smooth(26f, 38f, steep);
                    float sand = 0f, shore = 0f, mud = 0f;
                    bool mudZone = _map.IsMudAt(x, z);
                    bool beachZone = false;
                    foreach (var s in S.Shores)
                        if (s.Kind == ShoreKind.Beach && x > s.X0 - 4f && x < s.X1 + 4f) beachZone = true;
                    if (mudZone) mud = Smooth(-1f, 5f + n * 8f, d);
                    if (beachZone) sand = Smooth(-9f - n * 4f, -5f, d);
                    if ((S.Features & CoastFeature.Dunes) != 0 && S.DuneArea.Contains(new Vector2(x, z))) sand = 1f;
                    // 사막: 강가 초록 띠(45m) 밖은 모래. 맞은편 둑도 같다
                    if (S.SandLand)
                    {
                        float far = S.FarShoreZ > 0f ? z - _map.FarShoreAt(x) : -999f;
                        sand = Mathf.Max(sand, Mathf.Max(Smooth(-38f, -58f, d + n * 10f), Smooth(38f, 58f, far + n * 10f)));
                    }
                    // 물가와 물속: 자갈(바위 해안) 또는 모래
                    if (!beachZone && !mudZone) shore = Smooth(LandYPlus(0.9f), waterY, y) * 0.9f;
                    if (y < waterY - 0.6f && !mudZone) { sand = Mathf.Max(sand, 0.5f); shore = Mathf.Max(shore, 0.4f); }
                    // 곶·섬은 바위가 많다
                    foreach (var i in S.Islets)
                        if (new Vector2(x - i.Center.x, z - i.Center.z).magnitude < i.Radius + 6f) rock = Mathf.Max(rock, i.Trees ? 0.25f : 0.6f);
                    w[0] = Mathf.Max(0f, 1f - rock - sand - shore - mud);
                    w[1] = sand;
                    w[2] = rock;
                    w[3] = shore;
                    w[4] = mud;
                });
        }

        float LandYPlus(float dy) => _map.LandY + dy;
        static float Smooth(float a, float b, float x) => TerrainBuilder.Smooth(a, b, x);

        /// <summary>
        /// 해안선을 따라 x0~x1 구간에 길이 방향 상자를 이어 붙인다 (도로·호안).
        /// onGround면 높이를 지형에 맞추고, 언덕(곶)이나 물 위로 지나가는 토막은 건너뛴다.
        /// </summary>
        void AlongShore(string name, float x0, float x1, float offset, float y, Vector3 size, Material mat, bool collider, bool onGround = false)
        {
            float step = 8f;
            int seg = 0;
            for (float x = x0; x < x1; x += step, seg++)
            {
                float xa = x, xb = Mathf.Min(x + step, x1);
                // 이웃 토막과 0.6m 겹치는 이음매가 같은 높이면 깜빡이므로 토막마다 1.5cm씩 엇갈린다
                float stagger = (seg % 2) * 0.015f;
                var a = new Vector3(xa, y, _map.ShoreAt(xa) + offset);
                var b = new Vector3(xb, y, _map.ShoreAt(xb) + offset);
                if (onGround)
                {
                    float ha = H(a.x, a.z), hb = H(b.x, b.z), hm = H((a.x + b.x) * 0.5f, (a.z + b.z) * 0.5f);
                    if (Mathf.Abs(ha - hb) > 1.2f || hm > _map.LandY + 1.4f || Mathf.Min(ha, hb) < _map.LandY - 0.4f) continue;
                    a.y = ha + 0.06f;
                    b.y = hb + 0.06f;
                }
                a.y += stagger;
                b.y += stagger;
                var dir = b - a;
                if (dir.magnitude < 0.5f) continue;
                Mats.Prim(PrimitiveType.Cube, name, _root, (a + b) * 0.5f, new Vector3(size.x, size.y, dir.magnitude + 0.6f), mat, collider,
                    Quaternion.LookRotation(dir.normalized));
            }
        }

        void BuildShoreline()
        {
            float halfW = S.LandHalfWidth > 0f ? S.LandHalfWidth - 10f : 205f;
            // 해안도로
            if (S.RoadOffset > 0f)
            {
                var asphalt = Mats.TexturedBox("asphalt", Color.white, new Vector2(7f, 8f), 6f, 0.15f);
                AlongShore("CoastRoad", -halfW, halfW, -S.RoadOffset, _map.LandY + 0.03f, new Vector3(7f, 0.16f, 1f), asphalt, false, true);
                for (float x = -halfW + 10f; x < halfW - 5f; x += 34f)
                {
                    float z = _map.ShoreAt(x) - S.RoadOffset + 4.2f, gy = H(x, z);
                    if (Mathf.Abs(gy - _map.LandY) < 0.5f && !InFeature(x, z)) _props.Streetlight(new Vector3(x, gy, z));
                }
            }
            // 포구 호안 (콘크리트 벽 + 앞마당)
            foreach (var seg in S.Shores)
            {
                if (seg.Kind != ShoreKind.Quay) continue;
                float x0 = Mathf.Max(seg.X0, -halfW), x1 = Mathf.Min(seg.X1, halfW);
                // 돌 부두: 바위 텍스처는 이끼 낀 갈라진 바위 덩어리처럼 보여서, 포장석 결이 있는 콘크리트 텍스처에 돌 색을 입힌다
                var quay = S.StoneQuay ? Mats.Weathered("stonewall", S.StoneTint * 0.45f, 3f, 0.12f, 1f, 0.5f, 0.35f, null, 0.4f, 0f, "stonewall") : _concrete;
                AlongShore("QuayWall", x0, x1, -0.4f, _map.LandY - 2.6f, new Vector3(1f, 7.2f, 1f), quay, true);
                // 해안도로가 바로 뒤에 붙어 있으면 앞마당은 깔지 않는다 (겹쳐서 깜빡인다)
                if (S.RoadOffset <= 0f || S.RoadOffset >= 16f)
                    AlongShore("QuayApron", x0, x1, -5.5f, _map.LandY + 0.04f, new Vector3(10f, 0.14f, 1f), quay, false);
                if (S.StoneQuay) continue;
                for (float x = x0 + 6f; x < x1 - 4f; x += R(7f, 11f))
                    Mats.Prim(PrimitiveType.Cylinder, "Bollard", _root, new Vector3(x, _map.LandY + 0.3f, _map.ShoreAt(x) - 1.2f), new Vector3(0.45f, 0.3f, 0.45f),
                        Mats.Lit(new Color(0.2f, 0.22f, 0.24f), 0.4f), false);
            }
            // 갯바위 해안: 물가에 바위 덩어리
            for (float x = -195f; x < 195f; x += R(5f, 9f))
            {
                bool rocky = true;
                foreach (var seg in S.Shores) if (seg.Kind != ShoreKind.Rock && x > seg.X0 - 4f && x < seg.X1 + 4f) rocky = false;
                if (!rocky) continue;
                float z = _map.ShoreAt(x) + R(-1f, 6f);
                if (z < CoastMapDef.TerrainMinZ + 10f) continue;
                _props.Boulder(new Vector3(x, _map.BaseWaterY + R(-0.5f, 0.6f), z), R(1.8f, 4.2f), _rock);
            }
        }

        // ---------------- 방파제 ----------------

        void BuildBreakwaters()
        {
            var parapet = Mats.Quay(new Color(0.78f, 0.78f, 0.76f));
            var tetra = Mats.Tetrapod(new Color(0.92f, 0.9f, 0.86f));
            float bottom = _map.BaseWaterY - 6f;
            foreach (var b in S.Breakwaters)
            {
                var concrete = Mats.Quay(new Color(0.94f, 0.94f, 0.92f));
                void Body(Rect r, bool along)
                {
                    float h = _map.LandY - bottom;
                    Mats.Prim(PrimitiveType.Cube, "Breakwater", _root, new Vector3(r.center.x, bottom + h * 0.5f, r.center.y), new Vector3(r.width, h, r.height), concrete);
                    if (along)
                        Mats.Prim(PrimitiveType.Cube, "BreakwaterParapet", _root, new Vector3(b.ParapetWest ? r.xMin + 0.3f : r.xMax - 0.3f, _map.LandY + 0.4f, r.center.y),
                            new Vector3(0.6f, 0.8f, r.height), parapet, false);
                    else
                        Mats.Prim(PrimitiveType.Cube, "BreakwaterParapet", _root, new Vector3(r.center.x, _map.LandY + 0.4f, r.yMax - 0.3f),
                            new Vector3(r.width, 0.8f, 0.6f), parapet, false);
                }
                Body(b.Stem, true);
                if (b.Arm.width > 0f) Body(b.Arm, false);
                BreakwaterDetails(b);
                // 테트라포드: 파도막이 벽 반대쪽(바깥)과 팔의 북쪽
                float side = b.ParapetWest ? b.Stem.xMin - 1f : b.Stem.xMax + 1f;
                float sgn = b.ParapetWest ? -1f : 1f;
                _props.TetrapodBank(new Vector3(side, 0f, b.Stem.yMin + 6f), new Vector3(side, 0f, b.Stem.yMax + 2f), new Vector3(sgn, 0f, 0f), _map.BaseWaterY, tetra);
                if (b.Arm.width > 0f)
                    _props.TetrapodBank(new Vector3(b.Arm.xMin, 0f, b.Arm.yMax + 0.5f), new Vector3(b.Arm.xMax, 0f, b.Arm.yMax + 0.5f), Vector3.forward, _map.BaseWaterY, tetra);
                if (b.Light.a > 0f)
                {
                    var lp = CoastMapDef.LighthousePos(b);
                    bool white = b.Light.r > 0.9f && b.Light.g > 0.9f;
                    _props.Lighthouse(new Vector3(lp.x, _map.LandY, lp.y), b.Light, white ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f));
                }
                // 방파제 위 가로등
                for (float z = b.Stem.yMin + 8f; z < b.Stem.yMax - 8f; z += 22f)
                    _props.Streetlight(new Vector3(b.ParapetWest ? b.Stem.xMin + 1.2f : b.Stem.xMax - 1.2f, _map.LandY, z), b.ParapetWest ? 90f : 270f);
            }
        }

        // ---------------- 곶 · 섬 · 바위기둥 ----------------

        /// <summary>
        /// 방파제 윗면 디테일: 10m마다 이음매(짙은 줄), 안쪽 가장자리 노란 안전선, 계류 말뚝(비트)과 쇠고리, 안쪽 벽의 철제 사다리,
        /// 수면 근처 젖은 짙은 띠와 따개비 낀 아랫단.
        /// </summary>
        void BreakwaterDetails(BreakwaterSpec b)
        {
            var seam = Mats.Lit(new Color(0.55f, 0.55f, 0.53f), 0.1f);
            var yellow = Mats.Lit(new Color(0.95f, 0.78f, 0.15f), 0.3f);
            var bitt = Mats.Lit(new Color(0.15f, 0.16f, 0.18f), 0.45f, 0.3f);
            var ladder = Mats.Lit(new Color(0.55f, 0.35f, 0.18f), 0.4f, 0.6f);
            float top = _map.LandY + 0.012f;
            void Along(Rect r, bool stem)
            {
                // 이음매 (5mm 위로 띄워 z-fighting 방지)
                if (stem) for (float z = r.yMin + 10f; z < r.yMax - 1f; z += 10f)
                        Mats.Prim(PrimitiveType.Cube, "BreakwaterSeam", _root, new Vector3(r.center.x, top, z), new Vector3(r.width - 0.1f, 0.02f, 0.08f), seam, false);
                else for (float x = r.xMin + 10f; x < r.xMax - 1f; x += 10f)
                        Mats.Prim(PrimitiveType.Cube, "BreakwaterSeam", _root, new Vector3(x, top, r.center.y), new Vector3(0.08f, 0.02f, r.height - 0.1f), seam, false);
                // 안쪽(배가 대는 쪽) 가장자리 노란 선과 계류 말뚝
                bool westInner = !b.ParapetWest;
                if (stem)
                {
                    float ex = westInner ? r.xMin + 0.35f : r.xMax - 0.35f;
                    Mats.Prim(PrimitiveType.Cube, "SafetyLine", _root, new Vector3(ex, top + 0.004f, r.center.y), new Vector3(0.25f, 0.02f, r.height - 0.4f), yellow, false);
                    for (float z = r.yMin + 7f; z < r.yMax - 4f; z += 14f)
                    {
                        var p = new Vector3(westInner ? r.xMin + 0.9f : r.xMax - 0.9f, _map.LandY, z);
                        Mats.Prim(PrimitiveType.Cylinder, "Bitt", _root, p + Vector3.up * 0.3f, new Vector3(0.38f, 0.3f, 0.38f), bitt);
                        Mats.Prim(PrimitiveType.Cylinder, "BittCap", _root, p + Vector3.up * 0.62f, new Vector3(0.5f, 0.05f, 0.5f), bitt, false);
                    }
                    // 안쪽 벽 철제 사다리 (수면에서 윗면까지)
                    for (float z = r.yMin + 18f; z < r.yMax - 6f; z += 26f)
                    {
                        float wx = westInner ? r.xMin - 0.08f : r.xMax + 0.08f;
                        foreach (float s in new[] { -0.25f, 0.25f })
                            Mats.Prim(PrimitiveType.Cube, "LadderRail", _root, new Vector3(wx, (_map.LandY + _map.BaseWaterY - 1.2f) * 0.5f, z + s), new Vector3(0.05f, _map.LandY - _map.BaseWaterY + 1.2f, 0.05f), ladder, false);
                        for (float yy = _map.BaseWaterY - 1f; yy < _map.LandY; yy += 0.3f)
                            Mats.Prim(PrimitiveType.Cube, "LadderRung", _root, new Vector3(wx, yy, z), new Vector3(0.04f, 0.04f, 0.5f), ladder, false);
                    }
                }
                // 수면 근처 젖은 띠·물때는 Fishing/Weathered 셰이더가 그린다 (_FishingWetY)
            }
            Along(b.Stem, true);
            if (b.Arm.width > 0f) Along(b.Arm, false);
        }

        void PlaceLandmark(Landmark lm, Vector3 at, float yaw)
        {
            switch (lm)
            {
                case Landmark.Pavilion: _props.Pavilion(at, yaw); break;
                case Landmark.Lighthouse: _props.Lighthouse(at, new Color(0.95f, 0.95f, 0.93f), new Color(0.6f, 1f, 0.6f)); break;
                case Landmark.GrandLighthouse: _props.GrandLighthouse(at, yaw); break;
                case Landmark.Shrine: Shrine(at, yaw); break;
                case Landmark.MukhoLighthouse: _props.MukhoLighthouse(at, yaw); break;
                case Landmark.GeomunLighthouse: _props.GeomunLighthouse(at, yaw); break;
            }
        }

        /// <summary>바닷가 서낭당: 돌무더기와 오색 천을 두른 신목, 작은 기와 당집.</summary>
        void Shrine(Vector3 at, float yaw)
        {
            var t = new GameObject("Shrine").transform;
            t.SetParent(_root, false);
            t.localPosition = at;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            // 돌탑: 납작한 돌을 아래 넓게 위로 좁게 쌓는다 (서낭당 누석단)
            var stone = Mats.Weathered("rock", new Color(0.5f, 0.48f, 0.45f), 1.2f, 0.2f, 0f, 0.1f, 0.5f);
            // (실사 바위 에셋은 크기를 평균 쪽으로 당겨 납작해지지 않으므로 납작한 타원체로 쌓는다)
            float cy = 0f;
            for (int i = 0; i < 14; i++)
            {
                float ring = 1f - i / 15f;
                float th = R(0.16f, 0.24f) * (0.7f + ring * 0.5f);
                cy += th * 0.5f;
                Mats.Prim(PrimitiveType.Sphere, "Cairn", t, new Vector3(2.5f + R(-0.12f, 0.12f), cy, R(-0.12f, 0.12f)),
                    new Vector3(R(0.55f, 0.8f) * (0.45f + ring * 0.75f), th * 1.15f, R(0.5f, 0.7f) * (0.45f + ring * 0.75f)), stone, false, Quaternion.Euler(R(-8f, 8f), R(0f, 360f), R(-8f, 8f)));
                cy += th * 0.5f;
            }
            // 당산나무: 큰 활엽수 (없으면 예전 원기둥 + 구)
            if (KenneyProps.SpawnBush(t, new Vector3(-2f, 0f, 0f), 0f, 8.5f, 0, null, 0f, true) == null)
            {
                Mats.Prim(PrimitiveType.Cylinder, "SacredTree", t, new Vector3(-2f, 2.5f, 0f), new Vector3(0.7f, 2.5f, 0.7f), Mats.Lit(new Color(0.35f, 0.28f, 0.22f)), false);
                Mats.Prim(PrimitiveType.Sphere, "Crown", t, new Vector3(-2f, 5.6f, 0f), new Vector3(5f, 3.6f, 5f), Mats.Lit(new Color(0.18f, 0.34f, 0.18f)), false);
            }
            var ribbons = new[] { new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.2f), new Color(0.2f, 0.45f, 0.85f), new Color(0.95f, 0.95f, 0.95f), new Color(0.2f, 0.65f, 0.3f) };
            for (int i = 0; i < 5; i++)
                Mats.Prim(PrimitiveType.Cylinder, "Ribbon", t, new Vector3(-2f, 1.5f + i * 0.18f, 0f), new Vector3(0.78f, 0.05f, 0.78f), Mats.Lit(ribbons[i]), false);
            // 당집: 회벽 + 나무 기둥·문, 작은 팔작 기와지붕 (Hanok.Roof)
            var wood = Mats.Weathered("planks", new Color(0.4f, 0.18f, 0.12f), 1.2f, 0.3f, 0f, 0.2f, 0f, null, 0.7f);
            Mats.Prim(PrimitiveType.Cube, "HallBase", t, new Vector3(0f, 0.2f, -3f), new Vector3(3.2f, 0.4f, 2.8f), Mats.Weathered("stonewall", new Color(0.55f, 0.53f, 0.5f), 1.4f, 0.15f, 0f, 0.3f, 0.4f));
            Mats.Prim(PrimitiveType.Cube, "Hall", t, new Vector3(0f, 1.3f, -3f), new Vector3(2.6f, 1.8f, 2.2f), Mats.Weathered("plaster", new Color(0.86f, 0.83f, 0.76f), 2f, 0.15f, 0f, 0.6f, 0.15f));
            foreach (float sx in new[] { -1.3f, 1.3f })
            foreach (float sz in new[] { -1.1f, 1.1f })
                Mats.Prim(PrimitiveType.Cylinder, "HallPost", t, new Vector3(sx, 1.3f, -3f + sz), new Vector3(0.22f, 0.9f, 0.22f), wood, false);
            Mats.Prim(PrimitiveType.Cube, "HallDoor", t, new Vector3(0f, 1.15f, -4.11f), new Vector3(1.1f, 1.4f, 0.04f), wood, false);
            var roof = new GameObject("HallRoof");
            roof.transform.SetParent(t, false);
            roof.transform.localPosition = new Vector3(0f, 2.25f, -3f);
            roof.AddComponent<MeshFilter>().sharedMesh = Hanok.Roof(2.1f, 1.85f, 1.05f, 0.28f);
            roof.AddComponent<MeshRenderer>().sharedMaterials = Hanok.RoofMaterials();
        }

        void Trees(Vector3 c, float radius, int count, Func<float, float, bool> ok, bool camellia)
        {
            if (!KenneyProps.Available && !camellia) return;
            var leaf = Mats.Lit(new Color(0.08f, 0.24f, 0.12f), 0.55f);
            var flower = Mats.Lit(new Color(0.85f, 0.08f, 0.12f), 0.4f);
            var trunk = Mats.Lit(new Color(0.35f, 0.3f, 0.26f), 0.2f);
            for (int i = 0; i < count; i++)
            {
                float x = c.x + R(-radius, radius), z = c.z + R(-radius, radius);
                if (!ok(x, z)) continue;
                float y = H(x, z);
                if (y < _map.LandY + 0.3f) continue;
                if (camellia && _rng.NextDouble() < 0.5)
                {
                    // 동백나무: 잎이 짙은 작은 나무 + 잎 사이 붉은 꽃 (예전: 구 세 개를 얹은 도형 나무)
                    float s = R(0.8f, 1.3f);
                    if (KenneyProps.SpawnBush(_root, new Vector3(x, y - 0.05f, z), R(0f, 360f), 4.2f * s, 14, flower, 0.22f, true) == null)
                    {
                        var tree = new GameObject("Camellia").transform;
                        tree.SetParent(_root, false);
                        tree.localPosition = new Vector3(x, y, z);
                        Mats.Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.2f * s, 0f), new Vector3(0.35f * s, 1.2f * s, 0.35f * s), trunk, false);
                        Mats.Prim(PrimitiveType.Sphere, "Leaves", tree, new Vector3(0f, 2.9f, 0f) * s, Vector3.one * 2.8f * s, leaf, false);
                    }
                }
                else if (KenneyProps.Available)
                {
                    if (S.TreeKind != TreeStyle.Korean) { Tree(new Vector3(x, y - 0.05f, z)); continue; }
                    string kind = S.Snow ? "tree_pineRoundC" : new[] { "tree_pineRoundA", "tree_pineRoundB", "tree_pineRoundC", "tree_default" }[_rng.Next(4)];
                    KenneyProps.Spawn("Kenney/Nature/" + kind, _root, new Vector3(x, y - 0.05f, z), R(0f, 360f), R(5f, 9f));
                }
            }
        }

        void BuildCapes()
        {
            foreach (var c in S.Capes)
            {
                // 곶 꼭대기 (등뼈 70% 지점)에 랜드마크
                var top2 = Vector2.Lerp(c.Base, c.Tip, 0.72f);
                float ty = H(top2.x, top2.y);
                var dir = (c.Tip - c.Base).normalized;
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                PlaceLandmark(c.Landmark, new Vector3(top2.x, ty, top2.y), yaw);
                if (c.Forest)
                    Trees(new Vector3((c.Base.x + c.Tip.x) * 0.5f, 0f, (c.Base.y + c.Tip.y) * 0.5f), Mathf.Max(c.WidthBase, (c.Tip - c.Base).magnitude * 0.5f), 70,
                        (x, z) =>
                        {
                            var (cd, w, _) = CoastMapDef.CapeDist(c, x, z);
                            return cd < w - 3f && new Vector2(x - top2.x, z - top2.y).magnitude > 10f;
                        }, !S.Snow);
                // 절벽 아래 바위
                for (int i = 0; i < 18; i++)
                {
                    float t = R(0.15f, 1f);
                    var p = Vector2.Lerp(c.Base, c.Tip, t);
                    var n = new Vector2(dir.y, -dir.x) * (i % 2 == 0 ? 1f : -1f);
                    float w = Mathf.Lerp(c.WidthBase, c.WidthTip, t);
                    var at = t > 0.95f ? c.Tip + dir * (c.WidthTip + R(3f, 7f)) + n * R(-8f, 8f) : p + n * (w + R(4f, 8f));
                    if (at.y < _map.ShoreAt(at.x) + 2f) continue;
                    _props.Boulder(new Vector3(at.x, _map.BaseWaterY + R(-0.6f, 0.8f), at.y), R(2.5f, 5f), _rock);
                }
            }
        }

        void BuildIslets()
        {
            foreach (var i in S.Islets)
            {
                var c = i.Center;
                int n = Mathf.RoundToInt(i.Radius * 0.7f);
                for (int k = 0; k < n; k++)
                {
                    float a = k / (float)n * Mathf.PI * 2f + R(-0.2f, 0.2f);
                    float s = R(2.5f, 5f);
                    _props.Boulder(new Vector3(c.x + Mathf.Cos(a) * (i.Radius + 2f), _map.BaseWaterY + R(-0.8f, 0.5f), c.z + Mathf.Sin(a) * (i.Radius + 2f)), s, _rock);
                }
                if (i.Trees) Trees(c, i.Radius, Mathf.RoundToInt(i.Radius * 1.6f), (x, z) => new Vector2(x - c.x, z - c.z).magnitude < i.Radius - 4f, !S.Snow);
                if (i.Landmark != Landmark.None)
                {
                    var top = new Vector3(c.x, H(c.x, c.z), c.z);
                    PlaceLandmark(i.Landmark, top, 180f);
                }
            }
        }

        void BuildStacks()
        {
            foreach (var st in S.Stacks)
            {
                var t = new GameObject("SeaStack").transform;
                t.SetParent(_root, false);
                t.localPosition = new Vector3(st.Pos.x, _map.BaseWaterY - 2f, st.Pos.z);
                // 파도에 깎인 울퉁불퉁한 바위기둥 (아래가 넓고 위로 갈수록 가늘다)
                var go = new GameObject("StackRock");
                go.transform.SetParent(t, false);
                go.AddComponent<MeshFilter>().sharedMesh = StackMesh(st.Radius, st.Height + 2f, _rng.Next(1000), (S.Features & CoastFeature.Karst) != 0);
                // 재질 3개: 옆 절벽 / 파도에 젖은 아랫단(짙고 번들) / 위를 보는 턱·꼭대기(풀·이끼)
                bool karst = (S.Features & CoastFeature.Karst) != 0;
                var cliff = karst ? Mats.Karst(new Color(0.95f, 0.94f, 0.9f), 0.8f, 0.35f)
                    : S.DarkRock ? Mats.Textured("rock", new Color(0.36f, 0.34f, 0.33f), 1f, 0.3f) : Mats.Textured("rock", new Color(0.78f, 0.74f, 0.68f), 1f, 0.25f);
                go.AddComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    cliff, karst ? Mats.Karst(new Color(0.42f, 0.41f, 0.38f), 0.1f) : Mats.Textured("rock", new Color(0.22f, 0.21f, 0.2f), 1f, 0.75f),
                    st.Pines ? (karst ? Mats.Karst(new Color(0.95f, 0.94f, 0.9f), 1f, 1f) : Mats.Textured("grass", new Color(0.55f, 0.62f, 0.45f), 1f, 0.1f)) : cliff,
                };
                if (st.Pines && KenneyProps.Available && karst)
                    JungleTop(t, go.GetComponent<MeshFilter>().sharedMesh, Mathf.RoundToInt(Mathf.Clamp(st.Radius * 0.9f, 4f, 22f)), st.Radius);
                else if (st.Pines && KenneyProps.Available)
                    for (int k = 0; k < 3; k++)
                        KenneyProps.Spawn("Kenney/Nature/tree_pineRoundB", t, new Vector3(R(-st.Radius, st.Radius) * 0.3f, st.Height + 1.6f, R(-st.Radius, st.Radius) * 0.3f), R(0f, 360f), R(3f, 5f));
                // 걸어서 못 들어가게 (배도 부딪힌다)
                var col = t.gameObject.AddComponent<CapsuleCollider>();
                col.radius = st.Radius;
                col.height = st.Height + 6f;
                col.center = new Vector3(0f, st.Height * 0.5f, 0f);
            }
        }

        /// <summary>
        /// 석회암 봉우리 밀림 (사진: 하롱베이 봉우리 위와 비탈은 활엽수가 빽빽하다): 메쉬에서 위를 보는(법선 y > 0.45) 꼭짓점을 골라
        /// 활엽수를 심는다. 크기는 봉우리 반지름에 비례 (멀리서도 덩어리진 숲으로 보이게).
        /// </summary>
        public void JungleTop(Transform t, Mesh mesh, int count, float radius)
        {
            var vs = mesh.vertices;
            var ns = mesh.normals;
            var cand = new List<int>();
            for (int i = 0; i < vs.Length; i += 3) if (ns[i].y > 0.45f && vs[i].y > 6f) cand.Add(i);
            if (cand.Count == 0) return;
            for (int k = 0; k < count; k++)
            {
                int i = cand[_rng.Next(cand.Count)];
                KenneyProps.Spawn("Kenney/Nature/tree_default", t, vs[i] - ns[i] * 0.4f, R(0f, 360f), R(0.35f, 0.6f) * Mathf.Clamp(radius, 6f, 16f), 0f, false);
            }
        }

        /// <summary>
        /// 바위기둥 메쉬 (해안 바위기둥·하롱베이 석회암 봉우리): 높이에 따라 가늘어지는 기둥을 큰 굴곡 + 잔 굴곡 + 세로 균열 홈 +
        /// 층층이 튀어나온 퇴적층 턱으로 깎고, 꼭대기는 울퉁불퉁하게 닫는다. 예전(둘레 20칸, 굴곡 한 겹)은 매끈한 탄두처럼 보였다.
        /// 서브메쉬 0 = 옆 절벽, 1 = 물에 젖는 아랫단(wetH m까지), 2 = 위를 보는 면(턱·꼭대기, 풀·이끼). UV는 둘레·높이 m 단위(한 장 = 4m).
        /// karst면 옆이 더 곧게 서고(석회암 탑) 꼭대기가 넓다.
        /// </summary>
        public static Mesh StackMesh(float radius, float height, int seed, bool karst = false, float wetH = 3.1f)
        {
            const int seg = 40, rings = 40;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var quads = new List<int>();
            float circ = Mathf.PI * 2f * radius;
            float sd = seed * 0.37f;
            int layers = 5 + seed % 5;
            for (int r = 0; r <= rings; r++)
            {
                float f = r / (float)rings;
                // 석회암 봉우리(사진: 하롱베이): 곧게 선 절벽 위로 위쪽 1/3이 밀림 덮인 둥근 지붕·뾰족한 봉우리
                float capStart = karst ? 0.66f : 0.85f;
                float cf = f > capStart ? (f - capStart) / (1f - capStart) : 0f;
                float cap = f > capStart ? Mathf.Pow(Mathf.Max(0f, 1f - cf * cf), karst ? 0.75f : 0.5f) : 1f;
                float taper = karst ? Mathf.Lerp(1.05f, 0.9f, Mathf.Pow(f, 1.6f)) : Mathf.Lerp(1.12f, 0.62f, Mathf.Pow(f, 1.3f));
                taper *= Mathf.Max(0.04f, cap);
                // 물가 파식 홈 (노치): 수면 위 0~1.5m가 파도에 깎여 안으로 들어간다
                if (karst)
                {
                    float yy = f * height;
                    taper *= 1f - 0.16f * Mathf.Exp(-Mathf.Pow((yy - (wetH - 0.3f)) / 0.9f, 2f));
                }
                // 퇴적층 턱: 높이를 층으로 나눠 층 위쪽이 살짝 튀어나온다 (층 경계도 노이즈로 기울어진다)
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    float big = Mathf.PerlinNoise(sd + ca * 1.3f + 5f, f * 3.2f + sa * 1.3f) - 0.5f;
                    float fine = Mathf.PerlinNoise(sd * 2.1f + ca * 4f + 11f, f * 14f + sa * 4f) - 0.5f;
                    float layer = f * layers + (Mathf.PerlinNoise(sd + ca * 0.8f, sa * 0.8f + 3f) - 0.5f) * 0.8f;
                    float ledge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.98f, layer - Mathf.Floor(layer)));
                    float crack = Mathf.Pow(Mathf.Abs(Mathf.Sin(a * (7f + seed % 4) + big * 3f)), 10f);
                    float rr = radius * taper * (1f + big * (karst ? 0.32f : 0.45f) + fine * 0.1f + ledge * 0.07f - crack * 0.07f);
                    float y = f * height;
                    if (f > capStart) y += height * (karst ? 0.14f * cf : 0.035f) * (Mathf.PerlinNoise(sd + ca * 2f + 20f, sa * 2f) - 0.5f) * 2f; // 울퉁불퉁한(석회암은 뾰족뾰족한) 꼭대기
                    verts.Add(new Vector3(ca * rr, y, sa * rr));
                    uvs.Add(new Vector2(i / (float)seg * circ / 4f, y / 4f));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int i = 0; i < seg; i++)
            {
                int a = r * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
                quads.AddRange(new[] { a, c, b, b, c, d });
            }
            int top = verts.Count;
            verts.Add(new Vector3(0f, height + radius * (karst ? 0.02f : 0.05f), 0f));
            uvs.Add(new Vector2(0.5f, height / 4f));
            int last = rings * (seg + 1);
            for (int i = 0; i < seg; i++) quads.AddRange(new[] { last + i, top, last + i + 1 });
            // 면 방향·높이로 서브메쉬를 나눈다
            var side = new List<int>();
            var wet = new List<int>();
            var up = new List<int>();
            for (int k = 0; k < quads.Count; k += 3)
            {
                Vector3 p0 = verts[quads[k]], p1 = verts[quads[k + 1]], p2 = verts[quads[k + 2]];
                var n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                float cy = (p0.y + p1.y + p2.y) / 3f;
                var list = cy < wetH ? wet : n.y > 0.55f ? up : side;
                list.Add(quads[k]); list.Add(quads[k + 1]); list.Add(quads[k + 2]);
            }
            var mesh = new Mesh { name = karst ? "Karst" : "SeaStack" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(side, 0);
            mesh.SetTriangles(wet, 1);
            mesh.SetTriangles(up, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------- 물 위 시설 ----------------

        void BuildPlatforms()
        {
            foreach (var pf in S.Platforms)
            {
                switch (pf.Kind)
                {
                    case PlatformKind.FishFarm: FishFarm(pf.Area); break;
                    default: Boardwalk(pf.Area, pf.Kind == PlatformKind.Boardwalk); break;
                }
            }
        }

        /// <summary>가두리 양식장: 스티로폼 부표 위 나무 통로, 칸마다 그물 테두리, 작은 관리사.</summary>
        void FishFarm(Rect r)
        {
            const float pen = 10f, walk = 1.6f;
            float y = _map.BaseWaterY + 0.35f;
            var plank = Mats.Textured("deck", new Color(0.75f, 0.62f, 0.48f), 2f, 0.25f);
            var floatMat = Mats.Lit(new Color(0.92f, 0.92f, 0.88f), 0.2f);
            var net = Mats.Lit(new Color(0.15f, 0.2f, 0.22f), 0.2f);
            var root = new GameObject("FishFarm").transform;
            root.SetParent(_root, false);
            // 가로 통로 (z 방향으로 반복), 세로 통로 (x 방향으로 반복)
            for (float lz = 0f; lz <= r.height + 0.01f; lz += pen + walk)
            {
                float z = r.yMin + Mathf.Min(lz, r.height - walk) + walk * 0.5f;
                Mats.Prim(PrimitiveType.Cube, "FarmPlank", root, new Vector3(r.center.x, y, z), new Vector3(r.width, 0.18f, walk), plank);
                Mats.Prim(PrimitiveType.Cube, "FarmFloat", root, new Vector3(r.center.x, y - 0.25f, z), new Vector3(r.width, 0.35f, walk * 0.8f), floatMat, false);
            }
            for (float lx = 0f; lx <= r.width + 0.01f; lx += pen + walk)
            {
                float x = r.xMin + Mathf.Min(lx, r.width - walk) + walk * 0.5f;
                Mats.Prim(PrimitiveType.Cube, "FarmPlank", root, new Vector3(x, y + 0.03f, r.center.y), new Vector3(walk, 0.18f, r.height), plank);
                Mats.Prim(PrimitiveType.Cube, "FarmFloat", root, new Vector3(x, y - 0.25f, r.center.y), new Vector3(walk * 0.8f, 0.35f, r.height), floatMat, false);
            }
            // 그물 칸 테두리 (난간 높이 줄)
            for (float lx = walk; lx < r.width - walk; lx += pen + walk)
            for (float lz = walk; lz < r.height - walk; lz += pen + walk)
            {
                var c = new Vector3(r.xMin + lx + pen * 0.5f, y + 0.5f, r.yMin + lz + pen * 0.5f);
                foreach (var (o, s) in new[] { (new Vector3(0f, 0f, pen * 0.5f), new Vector3(pen, 0.05f, 0.05f)), (new Vector3(0f, 0f, -pen * 0.5f), new Vector3(pen, 0.05f, 0.05f)),
                             (new Vector3(pen * 0.5f, 0f, 0f), new Vector3(0.05f, 0.05f, pen)), (new Vector3(-pen * 0.5f, 0f, 0f), new Vector3(0.05f, 0.05f, pen)) })
                    Mats.Prim(PrimitiveType.Cube, "NetRail", root, c + o, s, net, false);
            }
            // 사용자 참고 사진(욕지도 낚시 가두리): 칸마다 수면 아래 비치는 그물 + 테두리 기둥에 맨 그물 줄, 통로 둘레 노랑·파랑 스티로폼 부표,
            // 칸 위로 친 차양 그늘막, 칸 사이 낚시 의자·아이스박스. 관리사는 파란 패널 + 흰 지붕 + 차양
            var netWater = Mats.Transparent(new Color(0.08f, 0.14f, 0.12f, 0.45f), 0.3f);
            var buoyY = Mats.Lit(new Color(0.95f, 0.78f, 0.12f), 0.3f);
            var buoyB = Mats.Lit(new Color(0.18f, 0.42f, 0.8f), 0.3f);
            var post = Mats.Lit(new Color(0.75f, 0.75f, 0.72f), 0.3f, 0.4f);
            var shade = Mats.Lit(new Color(0.2f, 0.55f, 0.35f), 0.2f);
            var chair = Mats.Lit(new Color(0.85f, 0.25f, 0.2f), 0.3f);
            var cooler = Mats.Lit(new Color(0.92f, 0.92f, 0.95f), 0.4f);
            int cell = 0;
            for (float lx = walk; lx < r.width - walk; lx += pen + walk)
            for (float lz = walk; lz < r.height - walk; lz += pen + walk)
            {
                var c = new Vector3(r.xMin + lx + pen * 0.5f, y, r.yMin + lz + pen * 0.5f);
                Mats.Prim(PrimitiveType.Cube, "NetUnder", root, c + Vector3.down * 0.45f, new Vector3(pen - 0.3f, 0.02f, pen - 0.3f), netWater, false);
                foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                    Mats.Prim(PrimitiveType.Cylinder, "NetPost", root, c + new Vector3(dx * pen * 0.5f, 0.45f, dz * pen * 0.5f), new Vector3(0.08f, 0.45f, 0.08f), post, false);
                if (cell % 3 == 0)
                {
                    // 차양 그늘막 (네 기둥 + 초록 천)
                    foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                        Mats.Prim(PrimitiveType.Cylinder, "ShadePole", root, c + new Vector3(dx * 2.2f, 1.2f, dz * 2.2f - pen * 0.5f - walk * 0.5f), new Vector3(0.06f, 1.2f, 0.06f), post, false);
                    Mats.Prim(PrimitiveType.Cube, "Shade", root, c + new Vector3(0f, 2.4f, -pen * 0.5f - walk * 0.5f), new Vector3(4.8f, 0.04f, 4.6f), shade, false);
                }
                // 통로에 낚시 의자·아이스박스
                var seat = c + new Vector3(-pen * 0.3f, 0.35f, -pen * 0.5f - walk * 0.5f);
                Mats.Prim(PrimitiveType.Cube, "Chair", root, seat, new Vector3(0.45f, 0.06f, 0.45f), chair, false);
                Mats.Prim(PrimitiveType.Cube, "Cooler", root, seat + new Vector3(0.8f, -0.05f, 0f), new Vector3(0.6f, 0.4f, 0.4f), cooler, false);
                cell++;
            }
            // 통로 바깥 둘레 부표 줄 (노랑·파랑 번갈아)
            int bi = 0;
            for (float x = r.xMin; x <= r.xMax; x += 1.6f)
                foreach (float z in new[] { r.yMin - 0.5f, r.yMax + 0.5f })
                    Mats.Prim(PrimitiveType.Capsule, "Buoy", root, new Vector3(x, _map.BaseWaterY + 0.1f, z), new Vector3(0.6f, 0.35f, 0.6f), (bi++ % 2 == 0) ? buoyY : buoyB, false, Quaternion.Euler(0f, 0f, 90f));
            for (float z = r.yMin; z <= r.yMax; z += 1.6f)
                foreach (float x in new[] { r.xMin - 0.5f, r.xMax + 0.5f })
                    Mats.Prim(PrimitiveType.Capsule, "Buoy", root, new Vector3(x, _map.BaseWaterY + 0.1f, z), new Vector3(0.6f, 0.35f, 0.6f), (bi++ % 2 == 0) ? buoyY : buoyB, false, Quaternion.Euler(90f, 0f, 0f));
            // 관리사 (모서리): 파란 패널 벽, 흰 지붕, 앞 차양, 창
            var hut = new Vector3(r.xMax - 3f, y + 0.1f, r.yMax - 3f);
            Mats.Prim(PrimitiveType.Cube, "FarmHutDeck", root, hut + new Vector3(0f, 0f, 0f), new Vector3(6f, 0.2f, 6f), plank);
            Mats.Prim(PrimitiveType.Cube, "FarmHut", root, hut + new Vector3(0f, 1.3f, 0.3f), new Vector3(4.6f, 2.4f, 3.4f), Mats.Lit(new Color(0.25f, 0.45f, 0.72f)));
            Mats.Prim(PrimitiveType.Cube, "FarmHutRoof", root, hut + new Vector3(0f, 2.6f, 0f), new Vector3(5.2f, 0.2f, 4.6f), Mats.Lit(new Color(0.9f, 0.9f, 0.88f)), false, Quaternion.Euler(-6f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "FarmHutWindow", root, hut + new Vector3(0.6f, 1.6f, -1.42f), new Vector3(1.6f, 0.8f, 0.05f), Mats.Lit(new Color(0.14f, 0.2f, 0.26f), 0.85f), false);
            Mats.Prim(PrimitiveType.Cube, "FarmHutDoor", root, hut + new Vector3(-1.3f, 1.1f, -1.42f), new Vector3(0.9f, 1.9f, 0.05f), Mats.Lit(new Color(0.85f, 0.85f, 0.82f)), false);
            Mats.Prim(PrimitiveType.Cube, "FarmHutAwning", root, hut + new Vector3(0f, 2.3f, -2.4f), new Vector3(4.6f, 0.05f, 1.6f), shade, false, Quaternion.Euler(12f, 0f, 0f));
            _props.AddLamp(hut + new Vector3(0f, 3f, -2.5f), new Color(1f, 0.85f, 0.6f), 16f, 1.4f);
        }

        /// <summary>해안 데크길·잔교: 기둥 위 나무 데크, 데크길은 바다 쪽 난간.</summary>
        /// <summary>
        /// 나무 데크길·잔교: 판자 상판 + 아래 세로 장선(그림자 단) + 4m마다 말뚝(물에 젖은 짙은 아랫단)과 X자 가새.
        /// 난간이면 2m마다 기둥, 손잡이와 중간대 두 줄. 잔교 끝에는 계류 말뚝 두 개.
        /// </summary>
        void Boardwalk(Rect r, bool railing)
        {
            float y = _map.LandY + 0.25f;
            var root = new GameObject("Boardwalk").transform;
            root.SetParent(_root, false);
            Mats.Prim(PrimitiveType.Cube, "BoardwalkDeck", root, new Vector3(r.center.x, y, r.center.y), new Vector3(r.width, 0.18f, r.height),
                Mats.TexturedBox("deck", new Color(0.8f, 0.66f, 0.5f), new Vector2(r.width, r.height), 2f, 0.3f));
            var post = Mats.Wood(new Color(0.35f, 0.28f, 0.2f), 0.3f);
            var joist = Mats.Wood(new Color(0.42f, 0.33f, 0.24f), 0.2f);
            var wetPile = Mats.Lit(new Color(0.16f, 0.15f, 0.13f), 0.55f);
            bool longX = r.width > r.height;
            float len = longX ? r.width : r.height;
            float wid = longX ? r.height : r.width;
            // 장선: 상판 아래 길이 방향 세 줄
            foreach (float off in new[] { -0.35f, 0f, 0.35f })
            {
                var c = longX ? new Vector3(r.center.x, y - 0.22f, r.center.y + off * wid) : new Vector3(r.center.x + off * wid, y - 0.22f, r.center.y);
                Mats.Prim(PrimitiveType.Cube, "BoardwalkJoist", root, c, longX ? new Vector3(len, 0.24f, 0.16f) : new Vector3(0.16f, 0.24f, len), joist, false);
            }
            for (float t = 1f; t < len; t += 4f)
            {
                float lowest = y;
                foreach (float side in new[] { -1f, 1f })
                {
                    var p = longX ? new Vector3(r.xMin + t, 0f, r.center.y + side * (wid * 0.5f - 0.3f)) : new Vector3(r.center.x + side * (wid * 0.5f - 0.3f), 0f, r.yMin + t);
                    float ground = Mathf.Min(H(p.x, p.z), y) - 1f;
                    lowest = Mathf.Min(lowest, ground);
                    Mats.Prim(PrimitiveType.Cylinder, "BoardwalkPile", root, new Vector3(p.x, (y + ground) * 0.5f, p.z), new Vector3(0.3f, (y - ground) * 0.5f, 0.3f), post, false);
                    // 물에 잠기는 아랫단은 짙고 젖은 색 (수면 위 0.4m까지)
                    float wetTop = Mathf.Min(y - 0.3f, _map.BaseWaterY + 0.4f);
                    if (wetTop > ground)
                        Mats.Prim(PrimitiveType.Cylinder, "BoardwalkPileWet", root, new Vector3(p.x, (wetTop + ground) * 0.5f, p.z), new Vector3(0.32f, (wetTop - ground) * 0.5f, 0.32f), wetPile, false);
                    if (railing) Mats.Prim(PrimitiveType.Cylinder, "BoardwalkPost", root, new Vector3(p.x, y + 0.55f, p.z), new Vector3(0.1f, 0.5f, 0.1f), post, false);
                }
                // 말뚝 사이 가로 보 (상판 바로 아래)
                var mid = longX ? new Vector3(r.xMin + t, y - 0.38f, r.center.y) : new Vector3(r.center.x, y - 0.38f, r.yMin + t);
                Mats.Prim(PrimitiveType.Cube, "BoardwalkBeam", root, mid, longX ? new Vector3(0.18f, 0.2f, wid - 0.4f) : new Vector3(wid - 0.4f, 0.2f, 0.18f), joist, false);
                // 물 위로 1m 넘게 떠 있으면 X자 가새
                if (y - lowest > 2.2f)
                {
                    float h = Mathf.Min(y - 0.5f - Mathf.Max(lowest, _map.BaseWaterY - 0.5f), 2.4f);
                    float diag = Mathf.Atan2(h, wid - 0.6f) * Mathf.Rad2Deg;
                    var c = mid + Vector3.down * (h * 0.5f + 0.1f);
                    float lenD = Mathf.Sqrt(h * h + (wid - 0.6f) * (wid - 0.6f));
                    foreach (float sg in new[] { -1f, 1f })
                        Mats.Prim(PrimitiveType.Cube, "BoardwalkBrace", root, c, longX ? new Vector3(0.08f, 0.12f, lenD) : new Vector3(lenD, 0.12f, 0.08f), joist, false,
                            longX ? Quaternion.Euler(sg * diag, 0f, 0f) : Quaternion.Euler(0f, 0f, sg * diag));
                }
            }
            if (railing)
                foreach (float side in new[] { -1f, 1f })
                {
                    var c = longX ? new Vector3(r.center.x, y + 1.05f, r.center.y + side * (wid * 0.5f - 0.3f)) : new Vector3(r.center.x + side * (wid * 0.5f - 0.3f), y + 1.05f, r.center.y);
                    Mats.Prim(PrimitiveType.Cube, "BoardwalkRail", root, c, longX ? new Vector3(r.width, 0.08f, 0.08f) : new Vector3(0.08f, 0.08f, r.height), post, false);
                    var m = c + Vector3.down * 0.45f;
                    Mats.Prim(PrimitiveType.Cube, "BoardwalkRailMid", root, m, longX ? new Vector3(r.width, 0.06f, 0.06f) : new Vector3(0.06f, 0.06f, r.height), post, false);
                }
            else
            {
                // 잔교 끝: 계류 말뚝 둘 (배를 묶는 굵은 말뚝)
                var end = longX ? new Vector3(r.xMax - 0.5f, 0f, r.center.y) : new Vector3(r.center.x, 0f, r.yMax - 0.5f);
                foreach (float side in new[] { -1f, 1f })
                {
                    var p = end + (longX ? new Vector3(0f, 0f, side * (wid * 0.5f - 0.4f)) : new Vector3(side * (wid * 0.5f - 0.4f), 0f, 0f));
                    Mats.Prim(PrimitiveType.Cylinder, "MooringPost", root, new Vector3(p.x, y + 0.4f, p.z), new Vector3(0.35f, 0.4f, 0.35f), wetPile, false);
                }
            }
        }

        // ---------------- 마을 · 낚시점 · NPC ----------------

        void BuildVillage()
        {
            if (!KenneyProps.Available) return;
            float halfW = S.LandHalfWidth > 0f ? S.LandHalfWidth - 25f : 195f;
            string[] small = S.NorthPort ? new[] { "building-i", "building-j", "building-k", "building-l" } : new[] { "building-a", "building-b", "building-c", "building-d", "building-e", "building-h" };
            // 길가 건물 (바다를 본다)
            for (float x = -halfW; x <= halfW; x += R(14f, 24f) / Mathf.Max(0.3f, S.VillageDensity))
            {
                if (Mathf.Abs(x - S.ShopPos.x) < 16f) continue;
                float z = _map.ShoreAt(x) - S.RoadOffset - R(9f, 13f);
                var (y, flat) = Footing(x, z, 3.5f);
                if (!flat || y > _map.LandY + 2.5f || y < _map.LandY - 0.2f) continue; // 비탈·언덕·물가는 건너뛴다
                if (InFeature(x, z)) continue;
                var tangent = new Vector2(1f, (_map.ShoreAt(x + 1f) - _map.ShoreAt(x - 1f)) * 0.5f).normalized;
                float yaw = 180f + Mathf.Atan2(-tangent.y, tangent.x) * Mathf.Rad2Deg;
                if (S.NorthPort)
                    KenneyProps.Spawn("Kenney/City/" + small[_rng.Next(small.Length)], _root, new Vector3(x, y - 0.05f, z), yaw + R(-4f, 4f), R(5f, 10f));
                else // 바닷가 마을 집: 길가는 횟집·민박 간판이 가끔
                    House(new Vector3(x, y - 0.05f, z), yaw - 180f + R(-3f, 3f), _rng.NextDouble() < 0.4);
            }
            // 언덕 위 집과 나무
            int count = Mathf.RoundToInt(70 * S.VillageDensity) + 30;
            for (int i = 0; i < count; i++)
            {
                float x = R(-halfW, halfW);
                float back = R(S.RoadOffset + 26f, S.FlatWidth + S.CliffWidth + 60f);
                float z = _map.ShoreAt(x) - back;
                if (z < CoastMapDef.TerrainMinZ + 8f) continue;
                float y = H(x, z);
                if (y < _map.LandY - 0.1f || InFeature(x, z)) continue;
                bool house = _rng.NextDouble() < 0.3 * S.VillageDensity && back < S.FlatWidth + 15f;
                if (house)
                {
                    var (hy, flat) = Footing(x, z, 3.5f);
                    if (flat && S.NorthPort) KenneyProps.Spawn("Kenney/City/" + small[_rng.Next(small.Length)], _root, new Vector3(x, hy - 0.05f, z), 180f + R(-20f, 20f), R(5f, 7f));
                    else if (flat) House(new Vector3(x, hy - 0.05f, z), R(-20f, 20f), false);
                }
                else
                    Tree(new Vector3(x, y - 0.05f, z));
            }
            if (S.Houses == HouseStyle.Cyclades) CliffVillage();
        }

        /// <summary>건물 발밑: 네 귀퉁이 중 가장 낮은 높이, 비탈이 완만한지 (높낮이 차 1.5m 이하).</summary>
        (float y, bool flat) Footing(float x, float z, float half)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var (dx, dz) in new[] { (-half, -half), (half, -half), (-half, half), (half, half), (0f, 0f) })
            {
                float h = H(x + dx, z + dz);
                lo = Mathf.Min(lo, h);
                hi = Mathf.Max(hi, h);
            }
            return (lo, hi - lo < 1.5f);
        }

        bool InFeature(float x, float z)
        {
            foreach (var c in S.Capes)
            {
                var (cd, w, _) = CoastMapDef.CapeDist(c, x, z);
                if (cd < w + 4f) return true;
            }
            if ((S.Features & CoastFeature.Dunes) != 0 && S.DuneArea.Contains(new Vector2(x, z))) return true;
            if ((S.Features & CoastFeature.DryingRacks) != 0 && Mathf.Abs(x - DryingRackX) < 22f && Mathf.Abs(z - DryingRackZ) < 12f) return true;
            if ((S.Features & CoastFeature.Cranes) != 0 && Mathf.Abs(x - CraneX) < 40f && z > _map.ShoreAt(x) - 30f) return true;
            foreach (var r in S.Reserved) if (r.Contains(new Vector2(x, z))) return true;
            return new Vector2(x - S.ShopPos.x, z - S.ShopPos.z).magnitude < 12f;
        }

        void BuildShop()
        {
            var owner = S.ShopOwner ?? new CharacterLook { Name = S.ShopName + " 사장", Title = S.ShopName };
            _props.TackleShop(S.ShopPos, S.ShopYaw, S.ShopName, owner, S.ShopRoof);
        }

        void BuildNpcs()
        {
            var parent = new GameObject("CoastNpcs").transform;
            parent.SetParent(_root, false);
            foreach (var n in S.Npcs)
            {
                var pos = n.Pos;
                if (float.IsNaN(pos.y)) pos.y = Mathf.Max(_map.LandY, H(pos.x, pos.z));
                // 잔교·데크 위 사람은 상판 윗면(LandY + 0.25 + 두께 절반)에 세운다 (y: 1f로 두면 무릎까지 상판에 묻혔다)
                foreach (var pf in S.Platforms)
                    if (pf.Kind != PlatformKind.FishFarm && pf.Area.Contains(new Vector2(pos.x, pos.z)))
                        pos.y = _map.LandY + 0.34f;
                var npc = NpcAngler.Spawn(parent, pos, n.Yaw, n.Look, n.Role);
                if (!string.IsNullOrEmpty(n.Id)) npc.NpcId = n.Id;
            }
        }

        void BuildFarIslands()
        {
            var near = Mats.Lit(S.Snow ? new Color(0.5f, 0.55f, 0.6f) : new Color(0.22f, 0.3f, 0.24f));
            var far = Mats.Lit(new Color(0.35f, 0.4f, 0.42f));
            for (int i = 0; i < S.FarIslands.Length; i++)
            {
                var v = S.FarIslands[i];
                Mats.Prim(PrimitiveType.Sphere, "FarIsland", _root, new Vector3(v.x, -v.w * 0.35f, v.y), new Vector3(v.z, v.w, v.z * 0.45f), v.y > 600f ? far : near, false);
            }
            if ((S.Features & CoastFeature.Mountains) != 0)
            {
                // 육지 뒤 산줄기 (지형 밖 남쪽): 바다에서 돌아보면 보인다
                var ridge = Mats.Lit(S.Snow ? new Color(0.78f, 0.8f, 0.84f) : new Color(0.16f, 0.26f, 0.18f));
                var ridgeFar = Mats.Lit(S.Snow ? new Color(0.62f, 0.66f, 0.72f) : new Color(0.26f, 0.33f, 0.32f));
                for (int i = 0; i < 9; i++)
                {
                    float x = -360f + i * 90f + R(-20f, 20f);
                    float h = R(70f, 120f);
                    Mats.Prim(PrimitiveType.Sphere, "Ridge", _root, new Vector3(x, h * 0.1f, -300f + R(-30f, 30f)), new Vector3(R(140f, 200f), h, 110f), i % 2 == 0 ? ridge : ridgeFar, false);
                }
            }
        }

        // ---------------- 특별 풍경 ----------------

        const float DryingRackX = -60f, CraneX = 70f;
        float DryingRackZ => _map.ShoreAt(DryingRackX) - S.RoadOffset - 20f;

        void BuildFeatures()
        {
            if ((S.Features & CoastFeature.SquidBoats) != 0) SquidBoats();
            if ((S.Features & CoastFeature.DryingRacks) != 0) DryingRacks();
            if ((S.Features & CoastFeature.SeaBridge) != 0) SeaBridge();
            if ((S.Features & CoastFeature.Cranes) != 0) Cranes();
            if ((S.Features & CoastFeature.FarRocks) != 0) FarRocks();
            if ((S.Features & CoastFeature.SuspensionBridge) != 0) SuspensionBridge();
            if ((S.Features & CoastFeature.Azaleas) != 0) Azaleas();
            if ((S.Features & CoastFeature.IceFloes) != 0) IceFloes();
            if ((S.Features & CoastFeature.Hallasan) != 0) Hallasan();
            if ((S.Features & CoastFeature.DolHareubang) != 0) DolHareubangs();
            if ((S.Features & CoastFeature.Tangerines) != 0) Tangerines();
            if ((S.Features & CoastFeature.Columnar) != 0) ColumnarJoints();
            BuildOverseasFeatures();
            S.Extra?.Invoke(this);
        }

        /// <summary>한라산: 육지 뒤로 완만하게 솟은 큰 방패 화산, 꼭대기에 백록담 분화구 테.</summary>
        void Hallasan()
        {
            var slope = Mats.Lit(new Color(0.2f, 0.3f, 0.24f), 0.1f);
            var far = Mats.Lit(new Color(0.3f, 0.38f, 0.4f), 0.05f);
            var rim = Mats.Lit(new Color(0.42f, 0.4f, 0.36f), 0.1f);
            Mats.Prim(PrimitiveType.Sphere, "Hallasan", _root, new Vector3(-40f, -40f, -700f), new Vector3(1600f, 330f, 700f), far, false);
            Mats.Prim(PrimitiveType.Sphere, "HallasanNear", _root, new Vector3(-60f, -30f, -560f), new Vector3(900f, 210f, 400f), slope, false);
            Mats.Prim(PrimitiveType.Cylinder, "Baengnokdam", _root, new Vector3(-40f, 122f, -700f), new Vector3(130f, 4f, 70f), rim, false);
            // 오름 (작은 기생 화산)
            for (int i = 0; i < 6; i++)
                Mats.Prim(PrimitiveType.Sphere, "Oreum", _root, new Vector3(-320f + i * 120f + R(-30f, 30f), -6f, -300f + R(-40f, 30f)), new Vector3(R(60f, 110f), R(30f, 50f), R(50f, 90f)), slope, false);
        }

        /// <summary>돌하르방: 구멍 숭숭 현무암, 벙거지 모자, 큰 코, 배에 얹은 두 손.</summary>
        void DolHareubang(Vector3 at, float yaw, float scale)
        {
            var t = new GameObject("DolHareubang").transform;
            t.SetParent(_root, false);
            t.localPosition = at;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            t.localScale = Vector3.one * scale;
            // Blender 조각(sculpt.py hareubang: 벙거지·왕방울 눈·주먹코·배에 얹은 두 손을 한 덩어리로) + 현무암 결. 없으면 예전 도형 판
            if (KenneyProps.Spawn("Props/hareubang", t, Vector3.zero, 0f, 2.55f) != null) return;
            var stone = Mats.Textured("rock", new Color(0.32f, 0.31f, 0.31f), 0.6f, 0.25f);
            var dark = Mats.Lit(new Color(0.15f, 0.15f, 0.15f), 0.2f);
            Mats.Prim(PrimitiveType.Cube, "Base", t, new Vector3(0f, 0.15f, 0f), new Vector3(0.9f, 0.3f, 0.9f), stone);
            // 몸은 위아래가 거의 같은 굵은 기둥, 머리는 몸만큼 크고 길쭉하다 (실물 비율: 머리 ≈ 몸의 0.8)
            Mats.Prim(PrimitiveType.Cylinder, "Body", t, new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.55f, 0.5f), stone, false);
            Mats.Prim(PrimitiveType.Capsule, "Head", t, new Vector3(0f, 1.78f, 0.02f), new Vector3(0.58f, 0.5f, 0.52f), stone, false);
            Mats.Prim(PrimitiveType.Cylinder, "HatBrim", t, new Vector3(0f, 2.2f, 0f), new Vector3(0.6f, 0.04f, 0.56f), stone, false);
            Mats.Prim(PrimitiveType.Capsule, "HatTop", t, new Vector3(0f, 2.33f, 0f), new Vector3(0.44f, 0.16f, 0.42f), stone, false);
            Mats.Prim(PrimitiveType.Capsule, "Nose", t, new Vector3(0f, 1.72f, 0.27f), new Vector3(0.16f, 0.14f, 0.14f), stone, false);
            Mats.Prim(PrimitiveType.Cube, "Mouth", t, new Vector3(0f, 1.56f, 0.25f), new Vector3(0.2f, 0.025f, 0.03f), dark, false);
            foreach (float s in new[] { -1f, 1f })
            {
                // 툭 튀어나온 왕방울 눈
                Mats.Prim(PrimitiveType.Sphere, "EyeBall", t, new Vector3(s * 0.13f, 1.9f, 0.22f), new Vector3(0.17f, 0.13f, 0.1f), stone, false);
                Mats.Prim(PrimitiveType.Sphere, "Eye", t, new Vector3(s * 0.13f, 1.9f, 0.265f), new Vector3(0.09f, 0.06f, 0.03f), dark, false);
                // 배에 얹은 두 손 (한 손이 조금 위)
                Mats.Prim(PrimitiveType.Capsule, "Hand", t, new Vector3(s * 0.07f, 0.95f + (s > 0 ? 0.14f : -0.06f), 0.27f), new Vector3(0.14f, 0.2f, 0.1f), stone, false,
                    Quaternion.Euler(0f, 0f, 90f));
            }
        }

        void DolHareubangs()
        {
            var shop = S.ShopPos;
            DolHareubang(new Vector3(shop.x - 4.8f, H(shop.x - 4.8f, shop.z + 3f), shop.z + 3f), 0f, 1f);
            DolHareubang(new Vector3(shop.x + 4.8f, H(shop.x + 4.8f, shop.z + 3f), shop.z + 3f), 0f, 1f);
            foreach (var b in S.Breakwaters)
                DolHareubang(new Vector3(b.Stem.center.x, _map.LandY, b.Stem.yMin - 4f), 0f, 1.2f);
        }

        /// <summary>귤밭: 검은 현무암 돌담으로 두른 밭에 주황 귤이 달린 낮은 나무.</summary>
        void Tangerines()
        {
            var wall = Mats.Textured("rock", new Color(0.26f, 0.25f, 0.25f), 0.5f, 0.2f);
            var leaf = Mats.Lit(new Color(0.12f, 0.32f, 0.14f), 0.45f);
            var fruit = Mats.Lit(new Color(1f, 0.55f, 0.08f), 0.5f);
            var trunk = Mats.Lit(new Color(0.35f, 0.28f, 0.2f), 0.2f);
            for (int g = 0; g < 5; g++)
            {
                float cx = -150f + g * 62f + R(-8f, 8f);
                float cz = _map.ShoreAt(cx) - S.RoadOffset - R(30f, 50f);
                var (gy, flat) = Footing(cx, cz, 10f);
                if (!flat || InFeature(cx, cz)) continue;
                const float half = 12f;
                // 돌담 (둘레 네 변, 군데군데 틈)
                foreach (var (o, size) in new[] { (new Vector3(0f, 0f, half), new Vector3(half * 2f, 0.9f, 0.6f)), (new Vector3(0f, 0f, -half), new Vector3(half * 2f, 0.9f, 0.6f)),
                             (new Vector3(half, 0f, 0f), new Vector3(0.6f, 0.9f, half * 2f)), (new Vector3(-half, 0f, 0f), new Vector3(0.6f, 0.9f, half * 2f)) })
                {
                    var p = new Vector3(cx, H(cx + o.x, cz + o.z) + 0.4f, cz) + o;
                    Mats.Prim(PrimitiveType.Cube, "StoneWall", _root, p, size, wall);
                }
                for (float x = -half + 2.5f; x < half - 1f; x += 3.2f)
                for (float z = -half + 2.5f; z < half - 1f; z += 3.2f)
                {
                    float tx = cx + x + R(-0.4f, 0.4f), tz = cz + z + R(-0.4f, 0.4f);
                    // 귤나무: 덤불 모양 낮은 나무 + 잎 겉면에 귤 (예전: 구 하나에 귤 구슬)
                    float s = R(0.8f, 1.1f);
                    if (KenneyProps.SpawnBush(_root, new Vector3(tx, H(tx, tz), tz), R(0f, 360f), 2.1f * s, 12, fruit, 0.15f) == null)
                    {
                        var tree = new GameObject("TangerineTree").transform;
                        tree.SetParent(_root, false);
                        tree.localPosition = new Vector3(tx, H(tx, tz), tz);
                        Mats.Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.4f * s, 0f), new Vector3(0.18f, 0.4f * s, 0.18f), trunk, false);
                        Mats.Prim(PrimitiveType.Sphere, "Leaves", tree, new Vector3(0f, 1.3f * s, 0f), new Vector3(2f, 1.6f, 2f) * s, leaf, false);
                    }
                }
            }
        }

        /// <summary>주상절리: 첫 곶 바다 쪽 둘레에 육각 기둥이 다발로 서 있다 (높이가 들쭉날쭉).</summary>
        void ColumnarJoints()
        {
            if (S.Capes.Length == 0) return;
            var c = S.Capes[0];
            var mesh = HexPrism();
            var mat = Mats.Textured("rock", new Color(0.3f, 0.29f, 0.29f), 0.5f, 0.3f);
            var dir = (c.Tip - c.Base).normalized;
            var n = new Vector2(dir.y, -dir.x);
            for (int i = 0; i < 260; i++)
            {
                float t = R(0.35f, 1.05f);
                float side = i % 3 == 0 ? 1f : i % 3 == 1 ? -1f : 0f;
                var along = Vector2.Lerp(c.Base, c.Tip, Mathf.Min(1f, t));
                float w = Mathf.Lerp(c.WidthBase, c.WidthTip, Mathf.Min(1f, t));
                Vector2 p = side == 0f ? c.Tip + dir * (c.WidthTip * R(0.6f, 1.05f)) + n * R(-w, w) : along + n * side * w * R(0.75f, 1.05f);
                float top = Mathf.Max(H(p.x, p.y) + R(-0.5f, 1.5f), _map.BaseWaterY + R(0.3f, 2.5f));
                float bottom = _map.BaseWaterY - 4f;
                float h = top - bottom;
                if (h < 1f) continue;
                var go = new GameObject("BasaltColumn");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = new Vector3(p.x, bottom, p.y);
                go.transform.localRotation = Quaternion.Euler(0f, R(0f, 60f), 0f);
                float r = R(0.55f, 0.85f);
                go.transform.localScale = new Vector3(r, h, r);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        static Mesh _hexPrism;

        /// <summary>반지름 1, 높이 1(바닥 0 → 위 1)인 육각 기둥 (옆면 6장 + 윗면).</summary>
        static Mesh HexPrism()
        {
            if (_hexPrism != null) return _hexPrism;
            var v = new List<Vector3>();
            var tri = new List<int>();
            var uv = new List<Vector2>();
            for (int k = 0; k < 6; k++)
            {
                float a0 = k * Mathf.PI / 3f, a1 = (k + 1) * Mathf.PI / 3f;
                var p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                int b = v.Count;
                v.AddRange(new[] { p0, p1, p1 + Vector3.up, p0 + Vector3.up });
                uv.AddRange(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 2f), new Vector2(0f, 2f) });
                tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            int c = v.Count;
            v.Add(Vector3.up);
            uv.Add(new Vector2(0.5f, 0.5f));
            for (int k = 0; k < 6; k++)
            {
                float a0 = k * Mathf.PI / 3f, a1 = (k + 1) * Mathf.PI / 3f;
                int b = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0), 1f, Mathf.Sin(a0)));
                v.Add(new Vector3(Mathf.Cos(a1), 1f, Mathf.Sin(a1)));
                uv.Add(new Vector2(0.5f + Mathf.Cos(a0) * 0.5f, 0.5f + Mathf.Sin(a0) * 0.5f));
                uv.Add(new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f));
                tri.AddRange(new[] { c, b + 1, b });
            }
            _hexPrism = new Mesh { name = "HexPrism" };
            _hexPrism.SetVertices(v);
            _hexPrism.SetUVs(0, uv);
            _hexPrism.SetTriangles(tri, 0);
            _hexPrism.RecalculateNormals();
            _hexPrism.RecalculateBounds();
            return _hexPrism;
        }

        /// <summary>절벽 비탈에 핀 분홍 진달래 덤불 (헌화가: 노인이 절벽의 꽃을 꺾어 수로부인에게 바쳤다).</summary>
        void Azaleas()
        {
            var pink = Mats.Lit(new Color(0.95f, 0.45f, 0.65f), 0.3f);
            var pink2 = Mats.Lit(new Color(0.85f, 0.3f, 0.55f), 0.3f);
            var leaf = Mats.Lit(new Color(0.2f, 0.35f, 0.18f), 0.2f);
            for (int i = 0; i < 160; i++)
            {
                float x = R(-195f, 195f);
                float back = R(S.FlatWidth + 2f, S.FlatWidth + S.CliffWidth);
                float z = _map.ShoreAt(x) - back;
                float y = H(x, z);
                // 경사 50° 넘는 절벽 면은 건너뛴다 (덤불이 공중에 뜬 것처럼 보인다)
                float slope = Mathf.Max(Mathf.Abs(H(x, z + 1.5f) - H(x, z - 1.5f)), Mathf.Abs(H(x + 1.5f, z) - H(x - 1.5f, z))) / 3f;
                if (slope > 1.2f) continue;
                // 진달래 덤불: 덤불 모델 + 겉면에 분홍 꽃 (예전: 구 덤불에 큰 분홍 구슬)
                float bs = R(0.7f, 1.3f);
                if (KenneyProps.SpawnBush(_root, new Vector3(x, y - 0.1f, z), R(0f, 360f), 1.4f * bs, 16, _rng.NextDouble() < 0.5 ? pink : pink2, 0.16f) == null)
                {
                    var b = new GameObject("Azalea").transform;
                    b.SetParent(_root, false);
                    b.localPosition = new Vector3(x, y - 0.35f, z);
                    Mats.Prim(PrimitiveType.Sphere, "Bush", b, new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 1.4f, 1.7f) * bs, leaf, false);
                }
            }
        }

        /// <summary>포구 안팎에 떠다니는 납작한 얼음 조각.</summary>
        void IceFloes()
        {
            var ice = Mats.Lit(new Color(0.88f, 0.93f, 0.97f), 0.75f);
            for (int i = 0; i < 70; i++)
            {
                float x = R(-190f, 190f), z = R(-30f, 240f);
                if (!_map.IsWater(new Vector3(x, 0f, z))) continue;
                float s = R(1.2f, 5f);
                Mats.Prim(PrimitiveType.Cylinder, "IceFloe", _root, new Vector3(x, _map.BaseWaterY + 0.04f, z), new Vector3(s, 0.08f, s * R(0.5f, 1f)), ice, false,
                    Quaternion.Euler(0f, R(0f, 360f), 0f));
            }
        }

        /// <summary>수평선의 오징어잡이 배: 밤이면 줄줄이 매단 집어등이 환하다.</summary>
        void SquidBoats()
        {
            var hull = Mats.Lit(new Color(0.85f, 0.85f, 0.82f), 0.3f);
            var cabin = Mats.Lit(new Color(0.3f, 0.45f, 0.65f), 0.3f);
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(-320f + i * 105f + R(-25f, 25f), _map.BaseWaterY, R(330f, 460f));
                var b = new GameObject("SquidBoat").transform;
                b.SetParent(_root, false);
                b.localPosition = at;
                b.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                Mats.Prim(PrimitiveType.Cube, "Hull", b, new Vector3(0f, 0.6f, 0f), new Vector3(4f, 1.6f, 16f), hull, false);
                Mats.Prim(PrimitiveType.Cube, "Cabin", b, new Vector3(0f, 2.2f, -3f), new Vector3(3f, 1.8f, 4f), cabin, false);
                Mats.Prim(PrimitiveType.Cube, "LampWire", b, new Vector3(0f, 4.2f, 1f), new Vector3(0.05f, 0.05f, 14f), cabin, false);
                for (int k = 0; k < 9; k++)
                {
                    var bulb = Mats.Prim(PrimitiveType.Sphere, "SquidLamp", b, new Vector3(0f, 3.9f, -6f + k * 1.6f), Vector3.one * 0.9f, hull, false);
                    _bulbs.Add(bulb.GetComponent<Renderer>());
                }
                _props.AddLamp(b.position + Vector3.up * 5f, new Color(0.85f, 1f, 0.9f), 70f, 3f);
            }
        }

        /// <summary>
        /// 명태 덕장: 통나무 틀(덕목)에 장대 두 단을 걸고, 코를 새끼줄로 엮은 명태 두 마리씩(<see cref="DriedPollock"/>)
        /// 장대에 걸쳐 바닷바람에 말린다. 마른 정도가 다른 세 빛깔이 섞이고, 군데군데 빈자리, 눈 오는 맵이면 장대 위에 눈.
        /// 기둥 끝은 X자 버팀목, 맨 위는 긴 들보.
        /// </summary>
        void DryingRacks()
        {
            var log = Mats.Wood(new Color(0.42f, 0.33f, 0.24f), 0.2f);
            var oldLog = Mats.Wood(new Color(0.33f, 0.28f, 0.23f), 0.15f);
            var snow = Mats.Lit(new Color(0.93f, 0.95f, 0.98f), 0.35f);
            var pair = DriedPollock.Pair;
            for (int r = 0; r < 3; r++)
            {
                float x0 = DryingRackX - 16f, z = DryingRackZ - 6f + r * 6f;
                float y = Mathf.Max(_map.LandY, H(DryingRackX, z));
                for (int p = 0; p <= 4; p++)
                {
                    float px = x0 + p * 8f;
                    Mats.Prim(PrimitiveType.Cylinder, "RackPost", _root, new Vector3(px, y + 1.75f, z), new Vector3(0.24f, 1.75f, 0.24f), log, false,
                        Quaternion.Euler(R(-2f, 2f), 0f, R(-2f, 2f)));
                    // 끝 기둥은 앞뒤로 비스듬한 버팀목
                    if (p == 0 || p == 4)
                        foreach (float dz in new[] { -1f, 1f })
                            Mats.Prim(PrimitiveType.Cylinder, "RackBrace", _root, new Vector3(px, y + 1.2f, z + dz * 0.75f), new Vector3(0.12f, 1.35f, 0.12f), oldLog, false,
                                Quaternion.Euler(dz * 34f, 0f, 0f));
                }
                // 맨 위 들보
                Mats.Prim(PrimitiveType.Cylinder, "RackBeam", _root, new Vector3(x0 + 16f, y + 3.45f, z), new Vector3(0.16f, 16.2f, 0.16f), oldLog, false, Quaternion.Euler(0f, 0f, 90f));
                if (S.Snow)
                    Mats.Prim(PrimitiveType.Cube, "RackSnow", _root, new Vector3(x0 + 16f, y + 3.55f, z), new Vector3(32.2f, 0.06f, 0.2f), snow, false);
                foreach (float hy in new[] { 2.25f, 3.05f })
                {
                    Mats.Prim(PrimitiveType.Cylinder, "RackBar", _root, new Vector3(x0 + 16f, y + hy, z), new Vector3(0.11f, 16f, 0.11f), log, false, Quaternion.Euler(0f, 0f, 90f));
                    for (float x = x0 + 0.4f; x < x0 + 31.8f; x += R(0.26f, 0.34f))
                    {
                        if (_rng.NextDouble() < 0.07) continue; // 걷어 간 빈자리
                        // 장대 위 줄이 걸치는 점에서 늘어진다. 바람에 조금씩 돌고 기울었다
                        var go = new GameObject("PollockPair");
                        go.transform.SetParent(_root, false);
                        go.transform.localPosition = new Vector3(x, y + hy + 0.055f, z);
                        go.transform.localRotation = Quaternion.Euler(R(-5f, 5f), R(-14f, 14f), R(-4f, 4f));
                        go.transform.localScale = Vector3.one * R(0.88f, 1.12f);
                        go.AddComponent<MeshFilter>().sharedMesh = pair;
                        var mr = go.AddComponent<MeshRenderer>();
                        double v = _rng.NextDouble();
                        mr.sharedMaterial = DriedPollock.Material(v < 0.25 ? 0 : v < 0.7 ? 1 : 2);
                    }
                }
            }
            WorldBuilder.AddSignBothSides(_root, "명태 덕장", new Vector3(DryingRackX, _map.LandY + 5.5f, DryingRackZ + 9f), new Color(1f, 0.85f, 0.6f));
        }

        /// <summary>바다를 건너는 사장교 (영흥대교): 맵 서쪽 너머로 지나간다.</summary>
        void SeaBridge()
        {
            var deckMat = Mats.Wood(new Color(0.78f, 0.78f, 0.76f), 0.3f);
            var pylonMat = Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0.3f);
            var cable = Mats.Lit(new Color(0.85f, 0.85f, 0.85f), 0.5f);
            float x = -262f, z0 = -60f, z1 = 520f, deckY = 16f;
            Mats.Prim(PrimitiveType.Cube, "BridgeDeck", _root, new Vector3(x, deckY, (z0 + z1) * 0.5f), new Vector3(12f, 1.4f, z1 - z0), deckMat, false);
            for (float z = z0 + 30f; z < z1; z += 60f)
                Mats.Prim(PrimitiveType.Cube, "BridgePier", _root, new Vector3(x, deckY * 0.5f - 2f, z), new Vector3(5f, deckY + 4f, 4f), deckMat, false);
            foreach (float pz in new[] { 150f, 330f })
            {
                Mats.Prim(PrimitiveType.Cube, "BridgePylon", _root, new Vector3(x, deckY + 22f, pz), new Vector3(3f, 44f, 3f), pylonMat, false);
                for (int k = 1; k <= 7; k++)
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var top = new Vector3(x, deckY + 40f - k * 1.5f, pz);
                        var bottom = new Vector3(x, deckY + 0.8f, pz + s * k * 11f);
                        var mid = (top + bottom) * 0.5f;
                        Mats.Prim(PrimitiveType.Cylinder, "BridgeCable", _root, mid, new Vector3(0.18f, (top - bottom).magnitude * 0.5f, 0.18f), cable, false,
                            Quaternion.FromToRotation(Vector3.up, (top - bottom).normalized));
                    }
                var bulb = Mats.Prim(PrimitiveType.Sphere, "PylonLight", _root, new Vector3(x, deckY + 44.5f, pz), Vector3.one * 1.4f, pylonMat, false);
                _bulbs.Add(bulb.GetComponent<Renderer>());
            }
            for (float z = z0 + 10f; z < z1; z += 40f)
            {
                var bulb = Mats.Prim(PrimitiveType.Sphere, "BridgeLight", _root, new Vector3(x + 5.5f, deckY + 2.2f, z), Vector3.one * 0.6f, pylonMat, false);
                _bulbs.Add(bulb.GetComponent<Renderer>());
            }
        }

        /// <summary>큰 항구: 갠트리 크레인, 창고, 정박한 트롤선.</summary>
        void Cranes()
        {
            var yellow = Mats.Lit(new Color(0.85f, 0.65f, 0.15f), 0.35f);
            var red = Mats.Lit(new Color(0.65f, 0.18f, 0.14f), 0.35f);
            var shed = Mats.Wall(S.Snow ? new Color(0.55f, 0.58f, 0.6f) : new Color(0.6f, 0.62f, 0.6f), 0.3f);
            var roof = Mats.Lit(S.Snow ? new Color(0.92f, 0.94f, 0.97f) : new Color(0.3f, 0.45f, 0.55f), 0.3f);
            for (int i = 0; i < 2; i++)
            {
                float x = CraneX - 14f + i * 28f, z = _map.ShoreAt(x) - 6f;
                var mat = i == 0 ? yellow : red;
                foreach (float s in new[] { -1f, 1f })
                    Mats.Prim(PrimitiveType.Cube, "CraneLeg", _root, new Vector3(x + s * 4f, _map.LandY + 7f, z), new Vector3(0.8f, 14f, 0.8f), mat, false);
                Mats.Prim(PrimitiveType.Cube, "CraneBeam", _root, new Vector3(x, _map.LandY + 14.5f, z + 6f), new Vector3(9f, 1.2f, 30f), mat, false);
                Mats.Prim(PrimitiveType.Cube, "CraneCab", _root, new Vector3(x, _map.LandY + 13f, z + 2f), new Vector3(3f, 2.2f, 3f), Mats.Lit(new Color(0.85f, 0.85f, 0.82f)), false);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = CraneX - 30f + i * 22f, z = _map.ShoreAt(x) - 24f;
                Mats.Prim(PrimitiveType.Cube, "Warehouse", _root, new Vector3(x, _map.LandY + 3.5f, z), new Vector3(18f, 7f, 12f), shed);
                Mats.Prim(PrimitiveType.Cube, "WarehouseRoof", _root, new Vector3(x, _map.LandY + 7.2f, z), new Vector3(19f, 0.5f, 13f), roof, false);
            }
            if (KenneyProps.Available)
                for (int i = 0; i < 3; i++)
                {
                    float x = CraneX - 20f + i * 18f;
                    KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", _root, new Vector3(x, _map.BaseWaterY - 0.3f, _map.ShoreAt(x) + 7f), 90f + R(-6f, 6f), 0f, 14f);
                }
        }

        /// <summary>먼바다 바위섬 무리 (거문도 백도): 뾰족한 바위들이 수평선에 솟아 있다.</summary>
        void FarRocks()
        {
            var rock = Mats.Lit(new Color(0.55f, 0.52f, 0.48f), 0.2f);
            var cx = 240f; var cz = 520f;
            for (int i = 0; i < 26; i++)
            {
                float h = R(12f, 45f);
                var p = new Vector3(cx + R(-120f, 120f), _map.BaseWaterY - 3f + h * 0.45f, cz + R(-50f, 60f));
                Mats.Prim(PrimitiveType.Capsule, "FarRock", _root, p, new Vector3(R(8f, 22f), h, R(8f, 18f)), rock, false, Quaternion.Euler(R(-12f, 12f), R(0f, 360f), R(-12f, 12f)));
            }
        }

        /// <summary>출렁다리: 첫 곶 끝에서 첫 바위기둥까지 늘어진 줄다리.</summary>
        void SuspensionBridge()
        {
            if (S.Capes.Length == 0 || S.Stacks.Length == 0) return;
            var c = S.Capes[0];
            var st = S.Stacks[0];
            var a = new Vector3(c.Tip.x, H(c.Tip.x, c.Tip.y) + 0.3f, c.Tip.y);
            var b = new Vector3(st.Pos.x, _map.BaseWaterY - 2f + st.Height + 1f, st.Pos.z);
            var rope = Mats.Lit(new Color(0.75f, 0.2f, 0.15f), 0.3f);
            var plank = Mats.Wood(new Color(0.55f, 0.42f, 0.3f), 0.3f);
            int n = 24;
            var dir = (b - a);
            var flat = new Vector3(dir.x, 0f, dir.z).normalized;
            var side = Vector3.Cross(Vector3.up, flat);
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                var p0 = Vector3.Lerp(a, b, t0) + Vector3.down * Mathf.Sin(t0 * Mathf.PI) * 2.2f;
                var p1 = Vector3.Lerp(a, b, t1) + Vector3.down * Mathf.Sin(t1 * Mathf.PI) * 2.2f;
                var mid = (p0 + p1) * 0.5f;
                var rot = Quaternion.LookRotation(p1 - p0, Vector3.up);
                Mats.Prim(PrimitiveType.Cube, "BridgePlank", _root, mid, new Vector3(1.6f, 0.08f, (p1 - p0).magnitude * 0.9f), plank, false, rot);
                foreach (float s in new[] { -1f, 1f })
                    Mats.Prim(PrimitiveType.Cube, "BridgeRope", _root, mid + side * s * 0.85f + Vector3.up * 1f, new Vector3(0.06f, 0.06f, (p1 - p0).magnitude), rope, false, rot);
            }
        }
    }
}
