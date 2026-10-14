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
    public enum Landmark { None, Pavilion, Lighthouse, GrandLighthouse, Shrine }

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
    }

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
        public Func<List<FishSchool>> Schools;
        public int Seed = 1;
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
        public override bool IsSea => true;

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
            return IsFarSea(p) ? "먼바다" : S.SeaName;
        }

        public override List<FishSchool> CreateSchools() => S.Schools();

        public override float SeabedAt(float x, float z) => OnBreakwater(new Vector3(x, 0f, z)) ? LandY : HeightAt(x, z);
        // 섬 맵은 남쪽까지 바다가 감싸므로 지형 전체
        public override Rect SeabedArea => S.LandHalfWidth > 0f ? new Rect(-205f, -195f, 410f, 455f) : new Rect(-205f, -70f, 410f, 330f);

        public override Transform BuildWorld(List<Light> lamps, List<Renderer> bulbs) => new CoastWorldBuilder(this).Build(lamps, bulbs);
    }

    /// <summary>해안 맵 월드: 바다·지형·해안 구조물·방파제·곶·섬·바위기둥·시설·마을·낚시점·NPC·특별 풍경.</summary>
    public class CoastWorldBuilder
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
            _lamps = lamps;
            _bulbs = bulbs;
            _props = new CoastProps(_root, _rng, lamps, bulbs);
            _rock = Mats.Textured("rock", new Color(0.8f, 0.78f, 0.74f), 1.2f, 0.3f);
            _concrete = Mats.Textured("concrete", new Color(0.88f, 0.88f, 0.86f), 1f, 0.2f);
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
            BuildNpcs();
            foreach (var (text, pos, color) in S.Signs) WorldBuilder.AddSignBothSides(_root, text, pos, color);
            return _root;
        }

        // ---------------- 지형 ----------------

        void BuildTerrain()
        {
            var layers = new List<TerrainBuilder.LayerSpec>
            {
                new TerrainBuilder.LayerSpec(S.Snow ? "snow" : "grass", 9f, S.Snow ? 0.35f : 0.05f),
                new TerrainBuilder.LayerSpec("sand", 6f, 0.15f),
                new TerrainBuilder.LayerSpec("rock", 11f, 0.2f),
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
                AlongShore("QuayWall", x0, x1, -0.4f, _map.LandY - 2.6f, new Vector3(1f, 7.2f, 1f), _concrete, true);
                // 해안도로가 바로 뒤에 붙어 있으면 앞마당은 깔지 않는다 (겹쳐서 깜빡인다)
                if (S.RoadOffset <= 0f || S.RoadOffset >= 16f)
                    AlongShore("QuayApron", x0, x1, -5.5f, _map.LandY + 0.04f, new Vector3(10f, 0.14f, 1f), _concrete, false);
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
            var parapet = Mats.Lit(new Color(0.66f, 0.66f, 0.64f));
            var tetra = Mats.Lit(new Color(0.6f, 0.6f, 0.58f), 0.15f);
            float bottom = _map.BaseWaterY - 6f;
            foreach (var b in S.Breakwaters)
            {
                var concrete = Mats.TexturedBox("concrete", new Color(0.92f, 0.92f, 0.9f), new Vector2(b.Stem.width, b.Stem.height), 4f, 0.2f);
                void Body(Rect r, bool along)
                {
                    float h = _map.LandY - bottom;
                    Mats.Prim(PrimitiveType.Cube, "Breakwater", _root, new Vector3(r.center.x, bottom + h * 0.5f, r.center.y), new Vector3(r.width, h, r.height), concrete);
                    if (along)
                        Mats.Prim(PrimitiveType.Cube, "BreakwaterParapet", _root, new Vector3(b.ParapetWest ? r.xMin + 0.3f : r.xMax - 0.3f, _map.LandY + 0.6f, r.center.y),
                            new Vector3(0.6f, 1.2f, r.height), parapet, false);
                    else
                        Mats.Prim(PrimitiveType.Cube, "BreakwaterParapet", _root, new Vector3(r.center.x, _map.LandY + 0.6f, r.yMax - 0.3f),
                            new Vector3(r.width, 1.2f, 0.6f), parapet, false);
                }
                Body(b.Stem, true);
                if (b.Arm.width > 0f) Body(b.Arm, false);
                // 테트라포드: 파도막이 벽 반대쪽(바깥)과 팔의 북쪽
                float side = b.ParapetWest ? b.Stem.xMin - 1f : b.Stem.xMax + 1f;
                float sgn = b.ParapetWest ? -1f : 1f;
                for (float z = b.Stem.yMin + 6f; z < b.Stem.yMax + 2f; z += R(2.3f, 3f))
                    _props.Tetrapod(new Vector3(side + sgn * R(0.2f, 2.5f), _map.BaseWaterY + R(-0.6f, 0.4f), z + R(-0.8f, 0.8f)), tetra);
                if (b.Arm.width > 0f)
                    for (float x = b.Arm.xMin; x < b.Arm.xMax; x += R(2.3f, 3f))
                        _props.Tetrapod(new Vector3(x + R(-0.8f, 0.8f), _map.BaseWaterY + R(-0.6f, 0.4f), b.Arm.yMax + R(1f, 3.5f)), tetra);
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

        void PlaceLandmark(Landmark lm, Vector3 at, float yaw)
        {
            switch (lm)
            {
                case Landmark.Pavilion: _props.Pavilion(at, yaw); break;
                case Landmark.Lighthouse: _props.Lighthouse(at, new Color(0.95f, 0.95f, 0.93f), new Color(0.6f, 1f, 0.6f)); break;
                case Landmark.GrandLighthouse: _props.GrandLighthouse(at, yaw); break;
                case Landmark.Shrine: Shrine(at, yaw); break;
            }
        }

        /// <summary>바닷가 서낭당: 돌무더기와 오색 천을 두른 신목, 작은 기와 당집.</summary>
        void Shrine(Vector3 at, float yaw)
        {
            var t = new GameObject("Shrine").transform;
            t.SetParent(_root, false);
            t.localPosition = at;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var stone = Mats.Lit(new Color(0.55f, 0.53f, 0.5f), 0.2f);
            for (int i = 0; i < 9; i++)
                Mats.Prim(PrimitiveType.Sphere, "Cairn", t, new Vector3(R(-0.8f, 0.8f), 0.3f + i * 0.12f, R(-0.8f, 0.8f)) + new Vector3(2.5f, 0f, 0f),
                    Vector3.one * R(0.5f, 0.9f), stone, false);
            Mats.Prim(PrimitiveType.Cylinder, "SacredTree", t, new Vector3(-2f, 2.5f, 0f), new Vector3(0.7f, 2.5f, 0.7f), Mats.Lit(new Color(0.35f, 0.28f, 0.22f)), false);
            Mats.Prim(PrimitiveType.Sphere, "Crown", t, new Vector3(-2f, 5.6f, 0f), new Vector3(5f, 3.6f, 5f), Mats.Lit(new Color(0.18f, 0.34f, 0.18f)), false);
            var ribbons = new[] { new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.2f), new Color(0.2f, 0.45f, 0.85f), new Color(0.95f, 0.95f, 0.95f), new Color(0.2f, 0.65f, 0.3f) };
            for (int i = 0; i < 5; i++)
                Mats.Prim(PrimitiveType.Cylinder, "Ribbon", t, new Vector3(-2f, 1.5f + i * 0.18f, 0f), new Vector3(0.78f, 0.05f, 0.78f), Mats.Lit(ribbons[i]), false);
            Mats.Prim(PrimitiveType.Cube, "Hall", t, new Vector3(0f, 1.1f, -3f), new Vector3(2.6f, 2.2f, 2.2f), Mats.Lit(new Color(0.85f, 0.8f, 0.7f)));
            var roof = new GameObject("HallRoof");
            roof.transform.SetParent(t, false);
            roof.transform.localPosition = new Vector3(0f, 2.2f, -3f);
            roof.AddComponent<MeshFilter>().sharedMesh = CoastProps.CurvedRoof(1.9f, 1.1f, 0.3f);
            roof.AddComponent<MeshRenderer>().sharedMaterials = new[] { Mats.Lit(new Color(0.22f, 0.24f, 0.28f), 0.35f), Mats.Lit(new Color(0.55f, 0.22f, 0.16f), 0.3f) };
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
                    var tree = new GameObject("Camellia").transform;
                    tree.SetParent(_root, false);
                    tree.localPosition = new Vector3(x, y, z);
                    float s = R(0.8f, 1.3f);
                    Mats.Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.2f * s, 0f), new Vector3(0.35f * s, 1.2f * s, 0.35f * s), trunk, false);
                    for (int k = 0; k < 3; k++)
                        Mats.Prim(PrimitiveType.Sphere, "Leaves", tree, new Vector3(R(-1f, 1f), 2.6f + R(0f, 1.2f), R(-1f, 1f)) * s, Vector3.one * R(2.2f, 3f) * s, leaf, false);
                    for (int k = 0; k < 5; k++)
                    {
                        var dir = new Vector3(R(-1f, 1f), R(0.2f, 0.9f), R(-1f, 1f)).normalized;
                        Mats.Prim(PrimitiveType.Sphere, "Flower", tree, new Vector3(0f, 3.1f * s, 0f) + dir * 1.5f * s, Vector3.one * 0.3f, flower, false);
                    }
                }
                else if (KenneyProps.Available)
                {
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
                go.AddComponent<MeshFilter>().sharedMesh = StackMesh(st.Radius, st.Height + 2f, _rng.Next(1000));
                go.AddComponent<MeshRenderer>().sharedMaterial = Mats.Textured("rock", new Color(0.78f, 0.74f, 0.68f), 1f, 0.25f);
                // 아랫단: 파도에 젖은 짙은 띠
                Mats.Prim(PrimitiveType.Cylinder, "StackWet", t, new Vector3(0f, 2.4f, 0f), new Vector3(st.Radius * 2.32f, 0.7f, st.Radius * 2.32f),
                    Mats.Lit(new Color(0.18f, 0.17f, 0.16f), 0.6f), false);
                if (st.Pines && KenneyProps.Available)
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
        /// 바위기둥 메쉬: 높이에 따라 가늘어지는 원기둥에 각도·높이 노이즈로 울퉁불퉁한 면, 꼭대기는 둥글게 닫는다.
        /// UV는 둘레·높이 m 단위(텍스처 한 장 = 4m).
        /// </summary>
        static Mesh StackMesh(float radius, float height, int seed)
        {
            const int seg = 20, rings = 16;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float circ = Mathf.PI * 2f * radius;
            for (int r = 0; r <= rings; r++)
            {
                float f = r / (float)rings;
                float y = f * height;
                // 꼭대기 15%는 둥글게 오므린다
                float cap = f > 0.85f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((f - 0.85f) / 0.15f, 2f))) : 1f;
                float taper = Mathf.Lerp(1.12f, 0.62f, Mathf.Pow(f, 1.3f)) * Mathf.Max(0.05f, cap);
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    float n = Mathf.PerlinNoise(seed * 0.37f + Mathf.Cos(a) * 1.3f + 5f, f * 3.2f + Mathf.Sin(a) * 1.3f) - 0.5f;
                    float ledge = Mathf.PerlinNoise(seed * 0.11f, f * 9f) - 0.5f; // 층층이 깎인 결
                    float rr = radius * taper * (1f + n * 0.45f + ledge * 0.12f);
                    verts.Add(new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                    uvs.Add(new Vector2(i / (float)seg * circ / 4f, y / 4f));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int i = 0; i < seg; i++)
            {
                int a = r * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
            // 꼭대기 마개
            int top = verts.Count;
            verts.Add(new Vector3(0f, height + radius * 0.05f, 0f));
            uvs.Add(new Vector2(0.5f, height / 4f));
            int last = rings * (seg + 1);
            for (int i = 0; i < seg; i++) tris.AddRange(new[] { last + i, top, last + i + 1 });
            var mesh = new Mesh { name = "SeaStack" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
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
            // 관리사 (모서리)
            var hut = new Vector3(r.xMax - 3f, y + 0.1f, r.yMax - 3f);
            Mats.Prim(PrimitiveType.Cube, "FarmHutDeck", root, hut + new Vector3(0f, 0f, 0f), new Vector3(6f, 0.2f, 6f), plank);
            Mats.Prim(PrimitiveType.Cube, "FarmHut", root, hut + new Vector3(0f, 1.3f, 0f), new Vector3(4f, 2.4f, 3.4f), Mats.Lit(new Color(0.3f, 0.5f, 0.7f)));
            Mats.Prim(PrimitiveType.Cube, "FarmHutRoof", root, hut + new Vector3(0f, 2.6f, 0f), new Vector3(4.6f, 0.2f, 4f), Mats.Lit(new Color(0.85f, 0.85f, 0.82f)), false);
            _props.AddLamp(hut + new Vector3(0f, 3f, -2.5f), new Color(1f, 0.85f, 0.6f), 16f, 1.4f);
        }

        /// <summary>해안 데크길·잔교: 기둥 위 나무 데크, 데크길은 바다 쪽 난간.</summary>
        void Boardwalk(Rect r, bool railing)
        {
            float y = _map.LandY + 0.25f;
            var root = new GameObject("Boardwalk").transform;
            root.SetParent(_root, false);
            Mats.Prim(PrimitiveType.Cube, "BoardwalkDeck", root, new Vector3(r.center.x, y, r.center.y), new Vector3(r.width, 0.25f, r.height),
                Mats.TexturedBox("deck", new Color(0.8f, 0.66f, 0.5f), new Vector2(r.width, r.height), 2f, 0.3f));
            var post = Mats.Lit(new Color(0.35f, 0.28f, 0.2f), 0.3f);
            bool longX = r.width > r.height;
            float len = longX ? r.width : r.height;
            for (float t = 1f; t < len; t += 4f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var p = longX ? new Vector3(r.xMin + t, 0f, r.center.y + side * (r.height * 0.5f - 0.3f)) : new Vector3(r.center.x + side * (r.width * 0.5f - 0.3f), 0f, r.yMin + t);
                    float ground = Mathf.Min(H(p.x, p.z), y) - 1f;
                    Mats.Prim(PrimitiveType.Cylinder, "BoardwalkPile", root, new Vector3(p.x, (y + ground) * 0.5f, p.z), new Vector3(0.3f, (y - ground) * 0.5f, 0.3f), post, false);
                    if (railing) Mats.Prim(PrimitiveType.Cylinder, "BoardwalkPost", root, new Vector3(p.x, y + 0.55f, p.z), new Vector3(0.1f, 0.5f, 0.1f), post, false);
                }
            }
            if (railing)
                foreach (float side in new[] { -1f, 1f })
                {
                    var c = longX ? new Vector3(r.center.x, y + 1.05f, r.center.y + side * (r.height * 0.5f - 0.3f)) : new Vector3(r.center.x + side * (r.width * 0.5f - 0.3f), y + 1.05f, r.center.y);
                    Mats.Prim(PrimitiveType.Cube, "BoardwalkRail", root, c, longX ? new Vector3(r.width, 0.08f, 0.08f) : new Vector3(0.08f, 0.08f, r.height), post, false);
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
                KenneyProps.Spawn("Kenney/City/" + small[_rng.Next(small.Length)], _root, new Vector3(x, y - 0.05f, z), yaw + R(-4f, 4f), R(5f, S.NorthPort ? 10f : 8f));
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
                    if (flat) KenneyProps.Spawn("Kenney/City/" + small[_rng.Next(small.Length)], _root, new Vector3(x, hy - 0.05f, z), 180f + R(-20f, 20f), R(5f, 7f));
                }
                else
                    KenneyProps.Spawn("Kenney/Nature/" + (S.Snow ? "tree_pineRoundC" : new[] { "tree_pineRoundA", "tree_pineRoundC", "tree_default", "tree_oak", "tree_pineRoundB" }[_rng.Next(5)]), _root,
                        new Vector3(x, y - 0.05f, z), R(0f, 360f), R(5f, 9f));
            }
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
            return new Vector2(x - S.ShopPos.x, z - S.ShopPos.z).magnitude < 12f;
        }

        void BuildShop()
        {
            var owner = S.ShopOwner ?? new CharacterLook { Name = S.ShopName + " 사장", Title = S.ShopName, TopModel = "Top_Shirt", Hair = "Hair_Buzzed" };
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
                var b = new GameObject("Azalea").transform;
                b.SetParent(_root, false);
                b.localPosition = new Vector3(x, y - 0.35f, z);
                Mats.Prim(PrimitiveType.Sphere, "Bush", b, new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 1.4f, 1.7f) * R(0.7f, 1.3f), leaf, false);
                for (int k = 0; k < 6; k++)
                    Mats.Prim(PrimitiveType.Sphere, "Bloom", b, new Vector3(R(-0.9f, 0.9f), R(0.7f, 1.2f), R(-0.9f, 0.9f)), Vector3.one * R(0.4f, 0.7f), k % 2 == 0 ? pink : pink2, false);
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

        /// <summary>명태 덕장: 통나무 틀에 명태를 줄줄이 매달아 바닷바람에 말린다.</summary>
        void DryingRacks()
        {
            var log = Mats.Lit(new Color(0.42f, 0.33f, 0.24f), 0.2f);
            var fish = Mats.Lit(new Color(0.72f, 0.58f, 0.36f), 0.45f);
            var head = Mats.Lit(new Color(0.42f, 0.34f, 0.24f), 0.4f);
            for (int r = 0; r < 3; r++)
            {
                float x0 = DryingRackX - 16f, z = DryingRackZ - 6f + r * 6f;
                float y = Mathf.Max(_map.LandY, H(DryingRackX, z));
                for (int p = 0; p <= 4; p++)
                    Mats.Prim(PrimitiveType.Cylinder, "RackPost", _root, new Vector3(x0 + p * 8f, y + 1.6f, z), new Vector3(0.22f, 1.6f, 0.22f), log, false);
                foreach (float hy in new[] { 2.2f, 3f })
                {
                    Mats.Prim(PrimitiveType.Cylinder, "RackBar", _root, new Vector3(x0 + 16f, y + hy, z), new Vector3(0.12f, 16f, 0.12f), log, false, Quaternion.Euler(0f, 0f, 90f));
                    for (float x = x0 + 0.6f; x < x0 + 32f; x += 0.62f)
                        foreach (float side in new[] { -0.13f, 0.13f })
                        {
                            // 두 마리씩 줄에 엮어 걸쳐 둔다 (머리가 아래)
                            var c = new Vector3(x, y + hy - 0.62f, z + side);
                            var rot = Quaternion.Euler(R(-6f, 6f) + side * 40f, R(-12f, 12f), R(-4f, 4f));
                            Mats.Prim(PrimitiveType.Capsule, "Pollock", _root, c, new Vector3(0.2f, 0.5f, 0.11f), fish, false, rot);
                            Mats.Prim(PrimitiveType.Sphere, "PollockHead", _root, c + rot * new Vector3(0f, -0.5f, 0f), new Vector3(0.17f, 0.22f, 0.1f), head, false, rot);
                        }
                }
            }
            WorldBuilder.AddSignBothSides(_root, "명태 덕장", new Vector3(DryingRackX, _map.LandY + 5.5f, DryingRackZ + 9f), new Color(1f, 0.85f, 0.6f));
        }

        /// <summary>바다를 건너는 사장교 (영흥대교): 맵 서쪽 너머로 지나간다.</summary>
        void SeaBridge()
        {
            var deckMat = Mats.Lit(new Color(0.78f, 0.78f, 0.76f), 0.3f);
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
            var shed = Mats.Lit(S.Snow ? new Color(0.55f, 0.58f, 0.6f) : new Color(0.6f, 0.62f, 0.6f), 0.3f);
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
            var plank = Mats.Lit(new Color(0.55f, 0.42f, 0.3f), 0.3f);
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
