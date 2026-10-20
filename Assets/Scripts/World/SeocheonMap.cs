using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 서천 앞바다 (서해, Lv.6). 원작 국내 낚시터이자 조합 낚시 대회가 열리던 곳.
    /// 실제 서천 바닷가를 참고했다: 홍원항(바다로 뻗은 방파제와 흰·빨간 등대, 국가어항), 춘장대 해변과 갯벌,
    /// 마량리 동백나무숲의 동백정(서해 낙조를 보는 정자), 앞바다의 작은 바위섬. 주꾸미·갑오징어·광어·우럭·백조기, 가을 전어.
    ///  - 물때: 서해는 밀물·썰물이 크다. 수면이 ±0.6m 오르내리고, 썰물이면 서쪽 갯벌이 드러나 걸어 들어갈 수 있다(<see cref="Tide"/>)
    ///  - 물빛: 탁한 황록색 (서해)
    ///  - 해: 바다 쪽(북쪽 +Z)으로 진다 → 방파제·동백정에서 바다 위 노을
    /// 격자 A~H × 1~8 (홍현 앞바다와 같은 크기), 1행이 해안 쪽.
    /// </summary>
    public class SeocheonDef : FieldMapDef
    {
        public override string Id => "seocheon";
        public override string MapName => "서천 앞바다 · 서해";
        public override string ShortName => "서천 앞바다";
        public override string ShopName => "서천 낚시점";
        public override string UnionContact => "홍원항 입구의 어촌계 이계장";
        public override bool IsSea => true;

        public const float ShoreZ = -40f;
        /// <summary>빨간 등대 방파제 (ㄴ자: 해안에서 북쪽으로, 끝에서 서쪽으로 꺾인다).</summary>
        public static readonly Rect RedStem = new Rect(56f, -40f, 7f, 68f);
        public static readonly Rect RedArm = new Rect(28f, 22f, 35f, 6f);
        /// <summary>흰 등대 방파제 (짧게 북쪽으로).</summary>
        public static readonly Rect WhiteBw = new Rect(-22f, -40f, 6f, 44f);
        /// <summary>동백정 곶: 남쪽 육지에서 바다로 튀어나온 언덕 (등뼈 선분, 폭).</summary>
        public static readonly Vector2 CapeBase = new Vector2(150f, -60f), CapeTip = new Vector2(150f, 22f);
        public const float CapeWidthBase = 38f, CapeWidthTip = 20f;
        /// <summary>앞바다 바위섬.</summary>
        public static readonly Vector3 Islet = new Vector3(105f, 0f, 88f);
        public const float IsletRadius = 10f;
        /// <summary>갯벌: 이 x보다 서쪽.</summary>
        public const float FlatsX = -45f;

        public SeocheonDef()
        {
            WaterY = 0.2f;
            BaseWaterY = 0.2f;
            TideAmplitude = 0.6f;
            LandY = 1.0f;
            WaterDeep = new Color(0.1f, 0.15f, 0.12f);
            WaterShallow = new Color(0.27f, 0.3f, 0.2f);
            GridMinX = -160f;
            GridMinZ = ShoreZ;
            CellW = 40f;
            CellH = 30f;
            Cols = 8;
            Rows = 8;
            MinX = -200f;
            MaxX = 200f;
            View = new Rect(-180f, -70f, 360f, 300f);
            Spawn = new Vector3(20f, LandY + 0.1f, -52f);
            ShopPos = new Vector3(-6f, LandY, -58f);
            ShopRadius = 10f;

            var S = Habitat.Shallow; var D = Habitat.Deep; var R = Habitat.Rocky; var W = Habitat.Weed;
            HabitatGrid = new[,]
            {
                { S, S, S, R, D, R, R, R },
                { S, S, W, R, D, R, R, R },
                { W, S, S, D, D, D, R, R },
                { D, W, S, D, D, D, R, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, R, D, D, D },
                { D, D, D, D, D, D, D, D },
                { D, D, D, D, D, D, D, D },
            };

            Mark(MapMark.Shape.Rect, 0f, (ShoreZ + 260f) * 0.5f, 400f, 260f - ShoreZ, new Color(0.14f, 0.24f, 0.24f));
            Mark(MapMark.Shape.Rect, -120f, -5f, 150f, 70f, new Color(0.36f, 0.33f, 0.26f), "춘장대 갯벌");
            Mark(MapMark.Shape.Rect, RedStem.center.x, RedStem.center.y, RedStem.width, RedStem.height, new Color(0.75f, 0.75f, 0.72f), "홍원항 방파제");
            Mark(MapMark.Shape.Rect, RedArm.center.x, RedArm.center.y, RedArm.width, RedArm.height, new Color(0.75f, 0.75f, 0.72f));
            Mark(MapMark.Shape.Circle, RedArm.xMin + 3f, RedArm.center.y, 5f, 5f, new Color(0.95f, 0.25f, 0.2f), "빨간 등대");
            Mark(MapMark.Shape.Rect, WhiteBw.center.x, WhiteBw.center.y, WhiteBw.width, WhiteBw.height, new Color(0.75f, 0.75f, 0.72f));
            Mark(MapMark.Shape.Circle, WhiteBw.center.x, WhiteBw.yMax - 3f, 5f, 5f, new Color(0.95f, 0.95f, 0.95f), "흰 등대");
            Mark(MapMark.Shape.Circle, CapeBase.x, -5f, 50f, 70f, new Color(0.25f, 0.38f, 0.22f), "동백정");
            Mark(MapMark.Shape.Circle, Islet.x, Islet.z, IsletRadius * 2f, IsletRadius * 2f, new Color(0.42f, 0.4f, 0.33f), "바위섬");
            Mark(MapMark.Shape.Circle, ShopPos.x, ShopPos.z, 8f, 8f, new Color(1f, 0.82f, 0.35f), "낚시점");
        }

        public static bool OnBreakwater(Vector3 p, float margin = 0f) =>
            In(RedStem, p, margin) || In(RedArm, p, margin) || In(WhiteBw, p, margin);

        static bool In(Rect r, Vector3 p, float m) => p.x > r.xMin + m && p.x < r.xMax - m && p.z > r.yMin - 1f && p.z < r.yMax - m;

        /// <summary>동백정 곶 등뼈까지 거리와 그 위치의 폭.</summary>
        public static (float dist, float width, float t) Cape(float x, float z)
        {
            var ab = CapeTip - CapeBase;
            float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, z) - CapeBase, ab) / ab.sqrMagnitude);
            var c = CapeBase + ab * t;
            return (Vector2.Distance(new Vector2(x, z), c), Mathf.Lerp(CapeWidthBase, CapeWidthTip, t), t);
        }

        public override bool IsWater(Vector3 p) =>
            p.x > MinX && p.x < MaxX && p.z > ShoreZ && p.z < 260f && !OnBreakwater(p) && SeocheonTerrain.HeightAt(p.x, p.z) < WaterY - 0.12f;

        public override float SeabedAt(float x, float z) => OnBreakwater(new Vector3(x, 0f, z)) ? LandY : SeocheonTerrain.HeightAt(x, z);
        public override Rect SeabedArea => new Rect(-205f, -70f, 410f, 330f);

        public override bool IsWalkable(Vector3 p)
        {
            if (p.x < MinX + 2f || p.x > MaxX - 2f) return false;
            if (p.z < ShoreZ - 0.6f && p.z > -150f) return true;          // 해안·마을
            if (OnBreakwater(p, 0.5f)) return true;
            float h = SeocheonTerrain.HeightAt(p.x, p.z);
            var (d, w, _) = Cape(p.x, p.z);
            if (d < w + 1f && h > WaterY + 0.3f) return true;              // 동백정 곶
            if (new Vector2(p.x - Islet.x, p.z - Islet.z).magnitude < IsletRadius - 2.5f) return true;
            return p.x < FlatsX && p.z < 40f && h > WaterY + 0.08f;        // 썰물에 드러난 갯벌
        }

        public override string AreaName(Vector3 p)
        {
            if (In(RedArm, p, 0f)) return "빨간 등대";
            if (In(RedStem, p, 0f)) return "홍원항 방파제";
            if (In(WhiteBw, p, 0f)) return p.z > WhiteBw.yMax - 8f ? "흰 등대" : "홍원항 작은 방파제";
            var (d, w, _) = Cape(p.x, p.z);
            if (d < w + 6f) return "마량리 동백정";
            if (new Vector2(p.x - Islet.x, p.z - Islet.z).magnitude < IsletRadius + 6f) return "앞바다 바위섬";
            if (p.x < FlatsX && p.z < 40f) return p.z < ShoreZ ? "춘장대 해변" : "춘장대 갯벌";
            if (p.z < ShoreZ) return p.x < 0f ? "서천 바닷가 마을" : "홍원항";
            return p.z > 110f ? "먼바다" : "서천 앞바다";
        }

        public override List<FishSchool> CreateSchools() => new List<FishSchool>
        {
            new FishSchool { Name = "망둥어 떼", SpeciesIds = new[] { "goby", "marbledflounder" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Day, TimeBand.Evening }, Col = 1, Row = 1, MaxRow = 2 },
            new FishSchool { Name = "주꾸미 밭", SpeciesIds = new[] { "octopus", "flounder" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Day }, Col = 2, Row = 2, MaxRow = 3 },
            new FishSchool { Name = "전어 떼", SpeciesIds = new[] { "shad", "seabass" },
                PreferredTimes = new[] { TimeBand.Morning, TimeBand.Evening }, Col = 4, Row = 2, MaxRow = 4 },
            new FishSchool { Name = "갑오징어·우럭", SpeciesIds = new[] { "cuttlefish", "jacopever", "greenling" },
                PreferredTimes = new[] { TimeBand.Evening, TimeBand.Night, TimeBand.Morning }, Col = 6, Row = 1, MaxRow = 4 },
            new FishSchool { Name = "백조기 어군", SpeciesIds = new[] { "whitecroaker", "blackseabream", "conger" },
                PreferredTimes = new[] { TimeBand.Evening, TimeBand.Night }, Col = 4, Row = 4, MinRow = 3, MaxRow = 6 },
            // 서해의 왕: 저녁 C5, 밤 F6 (원작풍 대물 어군)
            new FishSchool { Name = "민어", SpeciesIds = new[] { "croaker" }, Special = true,
                Schedule = new[] { (TimeBand.Evening, 2, 4), (TimeBand.Night, 5, 5) } },
        };

        public override Transform BuildWorld(List<Light> lamps, List<Renderer> bulbs) => new SeocheonWorldBuilder(this).Build(lamps, bulbs);
    }

    /// <summary>서천 앞바다 지형: 마을 비탈, 춘장대 해변·갯벌(물골), 홍원항 포구(호안·깊은 물), 동백정 곶(절벽), 바위섬, 바다 밑.</summary>
    public static class SeocheonTerrain
    {
        const float MinX = -210f, MinZ = -200f, SizeX = 420f, SizeZ = 470f, BaseY = -12f, Height = 44f;
        const float LandY = 1f;

        static float Smooth(float a, float b, float x) => TerrainBuilder.Smooth(a, b, x);

        public static float HeightAt(float x, float z)
        {
            const float shore = SeocheonDef.ShoreZ;
            float y;
            // 서쪽(갯벌)과 동쪽(포구·깊은 물)을 x로 섞는다
            float flats = Smooth(-30f, -55f, x);
            if (z < shore - 6f)
            {
                // 육지: 마을 뒤로 완만한 언덕
                y = LandY;
                if (z < -78f) y += (-78f - z) * 0.16f + Mathf.PerlinNoise(x * 0.02f, z * 0.02f) * 4f * Smooth(-78f, -110f, z);
            }
            else
            {
                // 갯벌 쪽: 모래 해변 → 0.7m 갯벌 → 물골을 따라 천천히 깊어진다
                float beach = z < shore + 2f ? Mathf.Lerp(LandY, 0.75f, Smooth(shore - 6f, shore + 2f, z)) :
                    Mathf.Lerp(0.75f, -1.7f, Mathf.Clamp01((z - shore - 2f) / 80f)) - Mathf.Max(0f, z - 42f) * 0.08f;
                float creekX = -110f + Mathf.Sin(z * 0.045f) * 18f;
                float creek = 0.7f * Mathf.Exp(-(x - creekX) * (x - creekX) / 70f) * Smooth(shore, shore + 15f, z);
                float ripples = (Mathf.PerlinNoise(x * 0.12f, z * 0.12f) - 0.5f) * 0.3f;
                float flatY = beach - creek + ripples;
                // 포구 쪽: 호안(콘크리트 벽) 바로 앞부터 깊다
                float harborY = z < shore ? LandY : -3f - (z - shore) * 0.06f + (Mathf.PerlinNoise(x * 0.03f, z * 0.03f) - 0.5f) * 1.5f;
                y = Mathf.Lerp(harborY, flatY, flats);
                y = Mathf.Max(y, BaseY + 1f);
            }

            // 동백정 곶: 등뼈를 따라 솟은 언덕, 바다 쪽 끝은 절벽
            var (d, w, t) = SeocheonDef.Cape(x, z);
            if (d < w + 12f)
            {
                float top = Mathf.Lerp(14f, 7f, t) + (Mathf.PerlinNoise(x * 0.05f, z * 0.05f) - 0.5f) * 2f;
                float cape = d < w
                    ? Mathf.Lerp(top, LandY + 1.2f, Mathf.Pow(d / w, 2.2f))
                    : Mathf.Lerp(LandY + 1.2f, -6f, Smooth(w, w + 9f, d + (Mathf.PerlinNoise(x * 0.2f, z * 0.2f) - 0.5f) * 4f));
                // 곶 끝도 둥글게 절벽
                if (t >= 0.999f)
                {
                    float over = Vector2.Distance(new Vector2(x, z), SeocheonDef.CapeTip);
                    cape = over < w ? Mathf.Lerp(top, LandY + 1.2f, Mathf.Pow(over / w, 2.2f)) : Mathf.Lerp(LandY + 1.2f, -6f, Smooth(w, w + 9f, over));
                }
                y = Mathf.Max(y, cape);
            }

            // 앞바다 바위섬
            var isl = SeocheonDef.Islet;
            float id = new Vector2(x - isl.x, z - isl.z).magnitude;
            if (id < SeocheonDef.IsletRadius + 10f)
            {
                float rough = Mathf.PerlinNoise(x * 0.2f, z * 0.2f) - 0.5f;
                float islet = id < SeocheonDef.IsletRadius - 2.5f ? LandY + rough * 0.3f + Mathf.Exp(-id * id / 18f) * 3f
                    : Mathf.Lerp(LandY, -5f, Smooth(SeocheonDef.IsletRadius - 2.5f, SeocheonDef.IsletRadius + 7f, id + rough * 3f));
                y = Mathf.Max(y, islet);
            }
            return y;
        }

        public static Terrain Build(Transform parent, float waterY)
        {
            var layers = new[]
            {
                new TerrainBuilder.LayerSpec("grass", 9f, 0.05f),
                new TerrainBuilder.LayerSpec("sand", 6f, 0.15f),
                new TerrainBuilder.LayerSpec("rock", 11f, 0.2f),
                new TerrainBuilder.LayerSpec("shore", 7f, 0.3f),
                // 갯벌 진흙: 모래 텍스처를 어둡게 바꾼 mud (축축하게 반짝)
                new TerrainBuilder.LayerSpec("mud", 5f, 0.7f),
            };
            return TerrainBuilder.Build(parent, "SeocheonTerrain", new Rect(MinX, MinZ, SizeX, SizeZ), BaseY, Height, 513, HeightAt, layers,
                (x, z, y, steep, wts) =>
                {
                    float n = Mathf.PerlinNoise(x * 0.07f, z * 0.07f);
                    float rock = Smooth(24f, 36f, steep);
                    var (d, cw, _) = SeocheonDef.Cape(x, z);
                    bool cape = d < cw + 12f;
                    bool islet = new Vector2(x - SeocheonDef.Islet.x, z - SeocheonDef.Islet.z).magnitude < SeocheonDef.IsletRadius + 8f;
                    float flats = Smooth(-30f, -55f, x);
                    float sand = 0f, shore = 0f, mud = 0f;
                    if (cape || islet)
                    {
                        rock = Mathf.Max(rock, islet ? 0.7f : 0f);
                        shore = Smooth(LandY + 0.8f, waterY, y) * 0.9f;
                    }
                    else if (z > SeocheonDef.ShoreZ - 7f)
                    {
                        // 해변 모래 → 갯벌 진흙 (서쪽), 포구 쪽은 자갈
                        float beach = Smooth(SeocheonDef.ShoreZ - 7f, SeocheonDef.ShoreZ - 3f, z) * Smooth(SeocheonDef.ShoreZ + 4f + n * 6f, SeocheonDef.ShoreZ, z);
                        sand = beach * flats + (1f - flats) * 0.3f;
                        mud = flats * Smooth(SeocheonDef.ShoreZ - 1f, SeocheonDef.ShoreZ + 5f + n * 8f, z);
                        shore = (1f - flats) * 0.7f;
                    }
                    wts[0] = Mathf.Max(0f, 1f - rock - sand - shore - mud);
                    wts[1] = sand;
                    wts[2] = rock;
                    wts[3] = shore;
                    wts[4] = mud;
                });
        }
    }

    /// <summary>서천 앞바다 월드.</summary>
    public class SeocheonWorldBuilder
    {
        readonly SeocheonDef _map;
        Transform _root;
        System.Random _rng;
        CoastProps _props;

        public SeocheonWorldBuilder(SeocheonDef map) { _map = map; }

        float R(float min, float max) => (float)(min + _rng.NextDouble() * (max - min));

        public Transform Build(List<Light> lamps, List<Renderer> bulbs)
        {
            _root = new GameObject("World_Seocheon").transform;
            _rng = new System.Random(20120921);
            _props = new CoastProps(_root, _rng, lamps, bulbs);

            WaterSurface.Create(_root, new Vector3(0f, _map.WaterY, 180f), new Vector2(900f, 460f), 4f);
            SeocheonTerrain.Build(_root, _map.BaseWaterY);
            BuildShore();
            BuildBreakwaters();
            BuildCape();
            BuildIslet();
            BuildHarbor();
            BuildVillage();
            BuildShop();
            BuildFarIslands();
            BuildNpcs();
            return _root;
        }

        // ---------------- 해안 ----------------

        void BuildShore()
        {
            Mats.Prim(PrimitiveType.Cube, "CoastRoad", _root, new Vector3(0f, _map.LandY + 0.03f, -62f), new Vector3(420f, 0.14f, 7f),
                Mats.TexturedBox("asphalt", Color.white, new Vector2(420f, 7f), 6f, 0.15f), false);
            // 홍원항 호안: 포구 앞 콘크리트 벽 (갯벌 쪽은 모래 해변)
            Mats.Prim(PrimitiveType.Cube, "Quay", _root, new Vector3(45f, _map.LandY - 2.2f, SeocheonDef.ShoreZ - 0.4f), new Vector3(150f, 6.4f, 1f),
                Mats.Quay(new Color(0.85f, 0.85f, 0.82f)));
            Mats.Prim(PrimitiveType.Cube, "QuayApron", _root, new Vector3(45f, _map.LandY + 0.03f, -46f), new Vector3(150f, 0.14f, 12f),
                Mats.TexturedBox("concrete", new Color(0.8f, 0.8f, 0.78f), new Vector2(150f, 12f), 4f, 0.2f), false);
            // 춘장대 해변의 작은 소나무 숲 (해송)
            if (KenneyProps.Available)
                for (float x = -195f; x < -60f; x += R(9f, 16f))
                {
                    float z = R(-70f, -50f);
                    KenneyProps.Spawn("Kenney/Nature/" + (_rng.NextDouble() < 0.5 ? "tree_pineRoundA" : "tree_pineRoundB"), _root,
                        new Vector3(x, SeocheonTerrain.HeightAt(x, z) - 0.05f, z), R(0f, 360f), R(6f, 9f));
                }
            // 갯벌 위 말뚝과 바구니 (갯벌 체험장 느낌)
            var pole = Mats.Wood(new Color(0.35f, 0.28f, 0.2f), 0.3f);
            for (int i = 0; i < 14; i++)
            {
                float x = -170f + i * 9f + R(-2f, 2f), z = -20f + R(-6f, 18f);
                float y = SeocheonTerrain.HeightAt(x, z);
                Mats.Prim(PrimitiveType.Cylinder, "FlatPole", _root, new Vector3(x, y + 0.6f, z), new Vector3(0.12f, 0.9f, 0.12f), pole, false);
            }
        }

        // ---------------- 방파제와 등대 ----------------

        void BuildBreakwaters()
        {
            var concrete = Mats.Quay(new Color(0.92f, 0.92f, 0.9f));
            var parapet = Mats.Quay(new Color(0.78f, 0.78f, 0.76f));
            var tetra = Mats.Tetrapod(new Color(0.92f, 0.9f, 0.86f));
            float bottom = _map.BaseWaterY - 5f;
            void Body(Rect r, string name, bool parapetWest)
            {
                float h = _map.LandY - bottom;
                Mats.Prim(PrimitiveType.Cube, name, _root, new Vector3(r.center.x, bottom + h * 0.5f, r.center.y), new Vector3(r.width, h, r.height), concrete);
                if (r.height > r.width)
                    Mats.Prim(PrimitiveType.Cube, name + "Parapet", _root, new Vector3(parapetWest ? r.xMin + 0.3f : r.xMax - 0.3f, _map.LandY + 0.4f, r.center.y),
                        new Vector3(0.6f, 0.8f, r.height), parapet, false);
                else
                    Mats.Prim(PrimitiveType.Cube, name + "Parapet", _root, new Vector3(r.center.x, _map.LandY + 0.4f, r.yMax - 0.3f),
                        new Vector3(r.width, 0.8f, 0.6f), parapet, false);
            }
            Body(SeocheonDef.RedStem, "RedBreakwater", false);
            Body(SeocheonDef.RedArm, "RedBreakwaterArm", false);
            Body(SeocheonDef.WhiteBw, "WhiteBreakwater", true);

            // 테트라포드: 큰 방파제 바깥(동쪽)과 꺾인 팔 바깥(북쪽)
            var st = SeocheonDef.RedStem;
            _props.TetrapodBank(new Vector3(st.xMax + 0.5f, 0f, st.yMin + 6f), new Vector3(st.xMax + 0.5f, 0f, st.yMax + 3f), Vector3.right, _map.BaseWaterY, tetra);
            var arm = SeocheonDef.RedArm;
            _props.TetrapodBank(new Vector3(arm.xMin, 0f, arm.yMax + 0.5f), new Vector3(arm.xMax, 0f, arm.yMax + 0.5f), Vector3.forward, _map.BaseWaterY, tetra);

            _props.Lighthouse(new Vector3(SeocheonDef.RedArm.xMin + 3f, _map.LandY, SeocheonDef.RedArm.center.y), new Color(0.85f, 0.15f, 0.12f), new Color(1f, 0.45f, 0.4f));
            _props.Lighthouse(new Vector3(SeocheonDef.WhiteBw.center.x, _map.LandY, SeocheonDef.WhiteBw.yMax - 3f), new Color(0.95f, 0.95f, 0.93f), new Color(0.6f, 1f, 0.6f));
        }

        // ---------------- 동백정 ----------------

        void BuildCape()
        {
            // 정자: 곶 꼭대기 근처, 바다(북쪽)를 보고 선다
            var top = new Vector2(SeocheonDef.CapeBase.x, -8f);
            float y = SeocheonTerrain.HeightAt(top.x, top.y);
            _props.Pavilion(new Vector3(top.x, y, top.y), 0f);
            WorldBuilder.AddSign(_root, "동백정", new Vector3(top.x, y + 6.4f, top.y - 3.6f), new Color(1f, 0.85f, 0.6f));

            // 동백나무숲: 윤기 나는 짙은 잎 + 붉은 꽃
            var leaf = Mats.Lit(new Color(0.08f, 0.24f, 0.12f), 0.55f);
            var leaf2 = Mats.Lit(new Color(0.1f, 0.3f, 0.15f), 0.5f);
            var flower = Mats.Lit(new Color(0.85f, 0.08f, 0.12f), 0.4f);
            var stamen = Mats.Lit(new Color(1f, 0.85f, 0.2f), 0.3f);
            var trunk = Mats.Lit(new Color(0.35f, 0.3f, 0.26f), 0.2f);
            for (int i = 0; i < 46; i++)
            {
                float a = R(0f, Mathf.PI * 2f);
                float x = SeocheonDef.CapeBase.x + R(-30f, 30f), z = R(-62f, -16f);
                var (d, w, _) = SeocheonDef.Cape(x, z);
                if (d > w - 3f) continue;
                // 정자 주변과 곶 끝(바다를 보는 쪽)은 비워 둔다
                if (new Vector2(x - top.x, z - top.y).magnitude < 11f) continue;
                float gy = SeocheonTerrain.HeightAt(x, z);
                var tree = new GameObject("Camellia").transform;
                tree.SetParent(_root, false);
                tree.localPosition = new Vector3(x, gy, z);
                float s = R(0.8f, 1.4f);
                Mats.Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.2f * s, 0f), new Vector3(0.35f * s, 1.2f * s, 0.35f * s), trunk, false);
                for (int k = 0; k < 4; k++)
                {
                    var c = new Vector3(R(-1.2f, 1.2f), 2.6f + R(0f, 1.4f), R(-1.2f, 1.2f)) * s;
                    Mats.Prim(PrimitiveType.Sphere, "Leaves", tree, c, Vector3.one * R(2.2f, 3.2f) * s, k % 2 == 0 ? leaf : leaf2, k == 0);
                }
                for (int k = 0; k < 7; k++)
                {
                    var dir = Random.onUnitSphere;
                    dir.y = Mathf.Abs(dir.y) * 0.7f + 0.2f;
                    var p = new Vector3(0f, 3.2f * s, 0f) + dir.normalized * 1.6f * s;
                    Mats.Prim(PrimitiveType.Sphere, "Flower", tree, p, Vector3.one * 0.32f, flower, false);
                    Mats.Prim(PrimitiveType.Sphere, "Stamen", tree, p + dir.normalized * 0.12f, Vector3.one * 0.1f, stamen, false);
                }
            }
            // 절벽 아래 바위
            var rock = Mats.Textured("rock", new Color(0.8f, 0.78f, 0.74f), 1.2f, 0.3f);
            for (int i = 0; i < 26; i++)
            {
                float z = R(-36f, 28f);
                float side = i % 2 == 0 ? -1f : 1f;
                var (_, w, t) = SeocheonDef.Cape(SeocheonDef.CapeBase.x, z);
                float x = SeocheonDef.CapeBase.x + side * (w + R(4f, 8f));
                if (z > SeocheonDef.CapeTip.y - 2f) { x = SeocheonDef.CapeBase.x + R(-14f, 14f); z = SeocheonDef.CapeTip.y + w + R(3f, 7f); }
                float s = R(2.5f, 5.5f);
                Rocks.Prim("CliffRock", _root, new Vector3(x, _map.BaseWaterY + R(-0.6f, 0.8f), z),
                    new Vector3(s * R(1f, 1.5f), s * 0.7f, s), rock, Quaternion.Euler(R(-15f, 15f), R(0f, 360f), R(-15f, 15f)));
            }
        }

        void BuildIslet()
        {
            var rock = Mats.Textured("rock", new Color(0.78f, 0.76f, 0.72f), 1.2f, 0.3f);
            var c = SeocheonDef.Islet;
            for (int k = 0; k < 9; k++)
            {
                float a = k / 9f * Mathf.PI * 2f + R(-0.2f, 0.2f);
                float s = R(2.5f, 5f);
                Rocks.Prim("IsletRock", _root, new Vector3(c.x + Mathf.Cos(a) * (SeocheonDef.IsletRadius + 2f), _map.BaseWaterY + R(-0.8f, 0.5f),
                    c.z + Mathf.Sin(a) * (SeocheonDef.IsletRadius + 2f)), new Vector3(s, s * R(0.6f, 1f), s * R(0.8f, 1.2f)), rock,
                    Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-20f, 20f)));
            }
        }

        // ---------------- 포구 · 마을 ----------------

        void BuildHarbor()
        {
            if (KenneyProps.Available)
            {
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", _root, new Vector3(18f, _map.BaseWaterY - 0.3f, -28f), 95f, 0f, 7f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", _root, new Vector3(30f, _map.BaseWaterY - 0.3f, -22f), 80f, 0f, 7f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", _root, new Vector3(42f, _map.BaseWaterY - 0.3f, -30f), 100f, 0f, 7.5f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-row-large", _root, new Vector3(6f, _map.BaseWaterY - 0.15f, -34f), 10f, 0f, 5f);
                for (int i = 0; i < 5; i++)
                    KenneyProps.Spawn("Kenney/Watercraft/buoy-flag", _root, new Vector3(-80f + i * 45f, _map.BaseWaterY - 0.6f, 50f + (i % 2) * 35f), 0f, 1.8f);
            }
            // 포구 가로등 (호안을 따라, 방파제 위)
            for (float x = -30f; x <= 100f; x += 26f) _props.Streetlight(new Vector3(x, _map.LandY, -45.5f));
            for (float z = -28f; z <= 18f; z += 22f) _props.Streetlight(new Vector3(SeocheonDef.RedStem.xMax - 0.8f, _map.LandY, z));
            for (float x = -170f; x <= -70f; x += 33f) _props.Streetlight(new Vector3(x, _map.LandY, -58.5f));
            WorldBuilder.AddSign(_root, "홍원항", new Vector3(30f, _map.LandY + 5.5f, -49f), new Color(0.6f, 0.85f, 1f));
        }

        void BuildVillage()
        {
            if (!KenneyProps.Available) return;
            // 해안도로 남쪽: 횟집·민박 거리와 언덕 위 집들
            for (float x = -190f; x <= 120f; x += R(14f, 24f))
            {
                float z = R(-74f, -68f);
                if (x > -30f && x < 10f) continue; // 낚시점 자리
                KenneyProps.Spawn("Kenney/City/building-" + "abcdefgh"[_rng.Next(8)], _root, new Vector3(x, SeocheonTerrain.HeightAt(x, z) - 0.05f, z), 180f + R(-6f, 6f), R(5f, 8f));
            }
            for (int i = 0; i < 40; i++)
            {
                float x = R(-200f, 110f), z = R(-140f, -85f);
                float y = SeocheonTerrain.HeightAt(x, z) - 0.05f;
                if (_rng.NextDouble() < 0.35)
                    KenneyProps.Spawn("Kenney/City/building-" + "abcdeh"[_rng.Next(6)], _root, new Vector3(x, y, z), 180f + R(-20f, 20f), R(5f, 7f));
                else
                    KenneyProps.Spawn("Kenney/Nature/" + new[] { "tree_pineRoundA", "tree_pineRoundC", "tree_default", "tree_oak" }[_rng.Next(4)], _root,
                        new Vector3(x, y, z), R(0f, 360f), R(5f, 9f));
            }
            WorldBuilder.AddSign(_root, "춘장대 해수욕장", new Vector3(-120f, _map.LandY + 4.5f, -56f), new Color(1f, 0.9f, 0.6f));
            WorldBuilder.AddSign(_root, "서천 주꾸미 축제", new Vector3(60f, _map.LandY + 6.5f, -72f), new Color(1f, 0.55f, 0.45f));
        }

        void BuildShop()
        {
            _props.TackleShop(_map.ShopPos, 0f, "서천 낚시점", new CharacterLook
            {
                Name = "서천 낚시점 박사장", Title = "서천 낚시점", TopModel = "Top_Shirt", Hair = "Hair_Buzzed", HairColor = new Color(0.3f, 0.3f, 0.3f),
                Shirt = new Color(0.35f, 0.5f, 0.65f), VestColor = new Color(0.7f, 0.6f, 0.4f), Pants = new Color(0.25f, 0.25f, 0.28f), Hat = true, HatStyle = 1,
                HatColor = new Color(0.85f, 0.3f, 0.2f)
            }, new Color(0.2f, 0.45f, 0.35f));
        }

        void BuildFarIslands()
        {
            // 서해 섬들 (수평선 너머로 해가 진다)
            var near = Mats.Lit(new Color(0.22f, 0.3f, 0.24f));
            var far = Mats.Lit(new Color(0.35f, 0.4f, 0.4f));
            float[,] isl = { { -300f, 470f, 140f, 35f }, { -60f, 560f, 200f, 45f }, { 260f, 500f, 160f, 40f }, { 80f, 680f, 300f, 55f } };
            for (int i = 0; i < isl.GetLength(0); i++)
                Mats.Prim(PrimitiveType.Sphere, "FarIsland", _root, new Vector3(isl[i, 0], -isl[i, 3] * 0.35f, isl[i, 1]),
                    new Vector3(isl[i, 2], isl[i, 3], isl[i, 2] * 0.45f), i >= 3 ? far : near, false);
        }

        void BuildNpcs()
        {
            var parent = new GameObject("SeocheonNpcs").transform;
            parent.SetParent(_root, false);
            var union = NpcAngler.Spawn(parent, new Vector3(SeocheonDef.RedStem.xMin - 4f, _map.LandY, SeocheonDef.ShoreZ - 5f), 200f, new CharacterLook
            {
                Name = "어촌계 이계장", Title = "한국 낚시 조합 서천 지부", VestModel = "Vest_Life", ShoeModel = "Shoes_Boots", Hair = "Hair_SimpleParted", Female = true,
                HairColor = new Color(0.15f, 0.12f, 0.1f), Shirt = new Color(0.9f, 0.9f, 0.88f), Vest = true, VestColor = new Color(0.95f, 0.5f, 0.12f),
                Pants = new Color(0.2f, 0.25f, 0.35f), Hat = true, HatStyle = 1, HatColor = new Color(0.2f, 0.3f, 0.55f)
            }, NpcAngler.Role.Talker);
            union.NpcId = "union";

            NpcAngler.Spawn(parent, new Vector3(SeocheonDef.RedArm.center.x + 6f, _map.LandY, SeocheonDef.RedArm.yMax - 1.2f), 0f, new CharacterLook
            {
                Name = "주꾸미 아줌마", Title = "에기 30년", TopModel = "Top_Long", Female = true, Hair = "Hair_Buns", Shirt = new Color(0.8f, 0.45f, 0.55f),
                VestColor = new Color(0.3f, 0.35f, 0.3f), Pants = new Color(0.25f, 0.22f, 0.25f), HatStyle = 2, HatColor = new Color(0.85f, 0.8f, 0.6f)
            }, NpcAngler.Role.Angler);
            NpcAngler.Spawn(parent, new Vector3(SeocheonDef.WhiteBw.xMin + 1.2f, _map.LandY, SeocheonDef.WhiteBw.yMax - 8f), -60f, new CharacterLook
            {
                Name = "갑오징어 헌터", Title = "먹물 세례 100회", TopModel = "Top_Rash", BottomModel = "Pants_Wader", ShoeModel = "Shoes_Boots", Hair = "Hair_Buzzed",
                Shirt = new Color(0.15f, 0.15f, 0.2f), Vest = false, Pants = new Color(0.15f, 0.2f, 0.18f), HatStyle = 1, HatColor = new Color(0.1f, 0.1f, 0.12f)
            }, NpcAngler.Role.Angler);
            var top = new Vector2(SeocheonDef.CapeBase.x, -8f);
            NpcAngler.Spawn(parent, new Vector3(top.x + 5f, SeocheonTerrain.HeightAt(top.x + 5f, top.y + 9f), top.y + 9f), 0f, new CharacterLook
            {
                Name = "동백정 할아버지", Title = "서해 노을 지킴이", TopModel = "Top_Long", Hair = "Hair_Buzzed", Beard = true, HairColor = new Color(0.85f, 0.85f, 0.82f),
                Shirt = new Color(0.7f, 0.65f, 0.55f), Vest = false, Pants = new Color(0.3f, 0.3f, 0.28f), Hat = false
            }, NpcAngler.Role.Talker);
        }
    }
}
