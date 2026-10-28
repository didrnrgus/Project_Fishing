using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public static partial class KoreaCoasts
    {
        // 탄천 단면 (z, 사용자 지도: 야탑 탄천종합운동장 앞 37.4113, 127.1230 — 물길 60m, 양쪽 둔치·제방, 제방 위 양현로·탄천로 사이 약 180m).
        // 실제 동쪽(야탑 시내) = 게임 남쪽(-z, 시작하는 둔치), 실제 서쪽(탄천종합운동장) = 게임 북쪽(+z), 하류(북쪽, 한강·잠실 롯데월드타워) = +x.
        //   물가 ShoreAt(x) ≈ -40 ← 둔치 38m(갈대 물가·산책로·자전거도로) ← 제방 비탈 14m(벚나무) ← 제방 위 양현로 ← 시가지
        //   맞은편 물가 FarShoreAt(x) ≈ +20 → 둔치 → 제방 → 탄천로 → 탄천종합운동장·야구장
        internal const float TcCityY = 6.6f, TcPlain = 38f, TcLevee = 14f;
        /// <summary>보(洑) · 징검다리 · 보행교 · 차도교(야탑교 -x, 사송교 +x) 자리 (x).</summary>
        // 사용자 위성사진: 사송교 바로 위 길쭉한 섬 둘, 그 아래 지그재그 징검다리, 더 아래 곧은 돌 보, 가운데 보행교
        internal const float TcWeirX = 66f, TcStonesX = 106f, TcFootX = 12f, TcBridgeX = 165f;

        /// <summary>
        /// 탄천 (Lv.3): 성남 야탑동 탄천종합운동장 앞. 폭 60m 남짓한 도심 하천 — 갈대·억새 물가, 붉은 산책로와 청록 자전거도로, 벚나무 제방,
        /// 보 아래 여울과 징검다리, 강 건너 탄천종합운동장·야구장, 양쪽 아파트 숲, 하류 너머 멀리 롯데월드타워.
        /// 생태하천 복원으로 돌아온 토종 민물고기(버들치·갈겨니·참마자·돌고기·납자루·각시붕어·참중고기)와 봄이면 한강에서 올라오는 잉어 떼.
        /// 실제 탄천은 낚시 금지라, 게임에서는 조합이 생태조사 허가를 받은 구간으로 둔다 (연락관 대사).
        /// </summary>
        public static CoastSpec Tancheon() => new CoastSpec
        {
            Id = "tancheon", MapName = "탄천 · 성남 야탑", ShortName = "탄천", ShopName = "탄천 낚시점", UnionContact = "보행교 앞 둔치의 강반장",
            LandName = "야탑 둔치", SeaName = "탄천", Seed = 61, River = true, FarShoreZ = 20f, FarShoreAmp = 0.22f, CellH = 7.5f,
            MapView = new Rect(-175f, -105f, 350f, 200f),
            WaterDeep = new Color(0.06f, 0.13f, 0.1f), WaterShallow = new Color(0.22f, 0.28f, 0.19f),
            ShoreBumps = new[] { new Vector3(-125f, 45f, 2.5f), new Vector3(105f, 55f, -2f) },
            SandTexture = "pebbles", CliffTexture = "stonewall",
            NearDepth = 1.7f, FlatWidth = TcPlain, CliffHeight = TcCityY - 1f, CliffWidth = TcLevee, RoadOffset = 0f,
            Houses = HouseStyle.Korean, TreeKind = TreeStyle.Korean, CustomBanks = true,
            Terrain = TancheonHeight, Splat = TancheonSplat,
            // 길쭉한 갈대섬 둘 (물 흐름 방향으로 늘어선 원 여러 개)
            Islets = new[]
            {
                new IsletSpec { Center = new Vector3(122f, 0f, -14f), Radius = 3.6f, Height = 0f, Walkable = true, Name = "갈대섬" },
                new IsletSpec { Center = new Vector3(129f, 0f, -13f), Radius = 4.2f, Height = 0f, Walkable = true, Name = "갈대섬" },
                new IsletSpec { Center = new Vector3(137f, 0f, -12f), Radius = 4.2f, Height = 0f, Walkable = true, Name = "갈대섬" },
                new IsletSpec { Center = new Vector3(145f, 0f, -11.5f), Radius = 3.4f, Height = 0f, Walkable = true, Name = "갈대섬" },
                new IsletSpec { Center = new Vector3(126f, 0f, 2f), Radius = 3.2f, Height = 0f, Walkable = true, Name = "갈대섬" },
                new IsletSpec { Center = new Vector3(133f, 0f, 3f), Radius = 3.8f, Height = 0f, Walkable = true, Name = "갈대섬" },
                new IsletSpec { Center = new Vector3(140f, 0f, 3.5f), Radius = 3.2f, Height = 0f, Walkable = true, Name = "갈대섬" },
            },
            Platforms = new[]
            {
                new PlatformSpec { Area = new Rect(TcStonesX - 2.6f, -44f, 5.2f, 70f), Kind = PlatformKind.Custom, Name = "탄천 징검다리" },
                new PlatformSpec { Area = new Rect(TcFootX - 1.5f, -51f, 3f, 83f), Kind = PlatformKind.Custom, Name = "탄천 보행교" },
            },
            Areas = new[]
            {
                (new Rect(TcWeirX - 4f, -46f, 8f, 72f), "탄천 보"),
                (new Rect(TcWeirX + 4f, -40f, 44f, 60f), "보 아래 여울"),
                (new Rect(TcBridgeX - 16f, -120f, 32f, 220f), "사송교 아래"),
                (new Rect(-TcBridgeX - 16f, -120f, 32f, 220f), "야탑교 아래"),
                (new Rect(-210f, 58f, 420f, 60f), "탄천로 · 탄천종합운동장"),
                (new Rect(-210f, -115f, 420f, 37f), "양현로 제방"),
            },
            // 열: A~D 야탑교~보행교 (깊은 물골), E 보행교~보 위 소, F 보 아래 여울, G 징검다리 여울, H 갈대섬
            Grid = new[,]
            {
                { W, W, S, W, S, S, R, W },
                { S, S, S, S, D, R, R, W },
                { D, D, D, D, D, R, R, S },
                { D, D, D, D, D, R, R, W },
                { D, D, D, D, D, R, R, W },
                { D, S, D, D, D, R, R, S },
                { S, S, S, S, D, R, R, W },
                { W, W, W, S, W, S, R, W },
            },
            Spawn = new Vector3(-6f, 1.1f, -45f), ShopPos = new Vector3(-30f, 1f, -68f),
            ShopRoof = new Color(0.16f, 0.5f, 0.56f),
            ShopOwner = Owner("탄천 낚시점 이사장", "탄천 낚시점"),
            Npcs = new[]
            {
                Npc(4f, -49f, 250f, Union("강반장", "한국 낚시 조합 성남 지부", false), NpcAngler.Role.Talker, "union"),
                Npc(TcStonesX - 6f, -43f, 10f, Angler("생태조사 대학생", "버들치 개체 수를 센다", true), NpcAngler.Role.Angler),
                Npc(54f, -48f, 40f, Angler("수달 해설사", "밤엔 수달이 나와요", true), NpcAngler.Role.Talker),
                Npc(-58f, -52f, 160f, Angler("자전거 동호회 아저씨", "한강까지 왕복 30km", false), NpcAngler.Role.Talker),
                Npc(46f, 25f, 190f, Angler("산책 나온 할아버지", "탄천 50년 토박이", false), NpcAngler.Role.Angler),
            },
            Signs = new[] { ("탄천 · TANCHEON", new Vector3(-16f, 4.2f, -55f), SignBlue), ("수달 서식지 · 조용히", new Vector3(48f, 3.4f, -53f), SignGold) },
            Schools = () => new List<FishSchool>
            {
                School("버들치 떼", 6, 2, 5, DayLong, "chineseminnow", "darkchub"),
                School("피라미·갈겨니 여울", 5, 3, 6, DayLong, "darkchub", "zacco"),
                School("참마자·돌고기 자갈 바닥", 6, 1, 4, MornDay, "skinnybarbel", "stripedshiner", "korsarcocheilichthys"),
                School("납자루·각시붕어 수초", 7, 1, 4, MornEve, "bitterling", "rosybitterling", "crucian"),
                School("메기 다리 그늘", 4, 4, 6, EveNight, "catfish", "nuchi"),
                // 잉어 떼: 봄이면 한강에서 수천 마리가 거슬러 올라온다 — 새벽 E4(보 위 소), 저녁 C5
                Special("한강에서 올라온 잉어 떼", "carp", (TimeBand.Dawn, 4, 3), (TimeBand.Evening, 2, 4)),
            },
            Extra = b => new TancheonWorld(b).Build(),
        };

        static readonly TimeBand[] DayLong = { TimeBand.Morning, TimeBand.Day, TimeBand.Evening };

        static float TcSmooth(float a, float b, float x) => TerrainBuilder.Smooth(a, b, x);
        static float TcGauss(float x, float c, float w) => Mathf.Exp(-(x - c) * (x - c) / (2f * w * w));

        /// <summary>
        /// 물가에서 뭍 쪽으로 d(m)인 곳의 둑 높이 (d &lt; 0이면 물속 비탈). n = 둔치 결 (±0.12m, 제방 위로 갈수록 사라진다).
        /// 제방은 곧은 비탈과 부드러운 비탈을 섞어 가장 가파른 곳이 24° (풀이 덮고, 걸어서 오른다).
        /// </summary>
        internal static float TcBank(float d, float n)
        {
            float y = Mathf.Lerp(-3f, 0.95f, TcSmooth(-6f, 3.5f, d)) + 0.08f * TcSmooth(3.5f, 10f, d);
            float t = Mathf.Clamp01((d - TcPlain) / TcLevee);
            float levee = Mathf.Lerp(t, t * t * (3f - 2f * t), 0.45f);
            return y + n * (1f - levee) * TcSmooth(1f, 4f, d) + (TcCityY - 1.03f) * levee;
        }

        /// <summary>물길 가운데 수심 (m): 보 위 소는 깊고, 보 아래·징검다리 여울은 얕다 (자갈톱이 수면 위로 드러난다).</summary>
        internal static float TcDepth(float x)
        {
            float d = 1.7f + 0.3f * Mathf.Sin(x * 0.045f + 1.3f);
            d += 0.8f * TcGauss(x, 52f, 20f);
            d -= 0.85f * TcSmooth(TcWeirX + 2f, TcWeirX + 8f, x) * TcSmooth(TcWeirX + 62f, TcWeirX + 40f, x);
            d += 0.45f * (TcGauss(x, TcBridgeX, 12f) + TcGauss(x, -TcBridgeX, 12f));
            return d;
        }

        internal static float TancheonHeight(CoastMapDef m, float x, float z)
        {
            float zn = m.ShoreAt(x), zf = m.FarShoreAt(x);
            float n = (Mathf.PerlinNoise(x * 0.05f + 3f, z * 0.05f) - 0.5f) * 0.24f;
            float u = Mathf.Clamp01((z - zn) / (zf - zn));
            // 물골이 한쪽으로 치우쳐 굽이친다
            float skew = Mathf.Pow(u, 0.85f + 0.3f * Mathf.Sin(x * 0.018f + 0.6f));
            float bed = 0.1f - TcDepth(x) * Mathf.Pow(Mathf.Sin(Mathf.PI * skew), 0.6f)
                        + (Mathf.PerlinNoise(x * 0.16f + 7f, z * 0.16f) - 0.5f) * 0.55f;
            return Mathf.Max(bed, Mathf.Max(TcBank(zn - z, n), TcBank(z - zf, n)));
        }

        /// <summary>칠하기: 물가 자갈·조약돌 띠, 물속 자갈·모래(웅덩이엔 진흙), 나머지는 풀 (둔치·제방·시가지 잔디).</summary>
        static void TancheonSplat(CoastMapDef m, float x, float z, float y, float steep, float[] w)
        {
            float n = Mathf.PerlinNoise(x * 0.09f, z * 0.09f);
            float water = m.BaseWaterY;
            float gravel = TcSmooth(water + 0.72f + n * 0.25f, water + 0.3f, y);
            float under = TcSmooth(water - 0.1f, water - 0.6f, y);
            float mud = under * TcSmooth(0.45f, 0.7f, Mathf.PerlinNoise(x * 0.03f + 9f, z * 0.05f)) * 0.8f;
            float rock = TcSmooth(30f, 42f, steep);
            w[0] = Mathf.Max(0f, 1f - gravel - rock);
            w[1] = gravel * (1f - under) * 0.85f + under * 0.35f;
            w[2] = rock;
            w[3] = gravel * 0.3f + under * 0.5f;
            w[4] = mud;
        }
    }

    /// <summary>면마다 UV 0~1인 상자 모음 (BlockMesh는 UV가 없어 무늬 천을 못 입힌다). 옆면은 세로 = 텍스처 v.</summary>
    sealed class UvBoxes
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector2> _u = new List<Vector2>();
        readonly List<int> _t = new List<int>();

        static readonly (Vector3 n, Vector3 a, Vector3 b)[] Faces =
        {
            (Vector3.right, Vector3.back, Vector3.up), (Vector3.left, Vector3.forward, Vector3.up),
            (Vector3.forward, Vector3.right, Vector3.up), (Vector3.back, Vector3.left, Vector3.up),
            (Vector3.up, Vector3.right, Vector3.back), (Vector3.down, Vector3.right, Vector3.forward),
        };

        public void Box(Vector3 c, Vector3 size, Quaternion q)
        {
            var h = size * 0.5f;
            float E(Vector3 axis) => Mathf.Abs(Vector3.Dot(axis, h));
            foreach (var (n, a, b) in Faces)
            {
                var o = c + q * (n * E(n));
                var A = q * (a * E(a));
                var B = q * (b * E(b));
                int i = _v.Count;
                _v.Add(o - A - B); _v.Add(o + A - B); _v.Add(o + A + B); _v.Add(o - A + B);
                _u.Add(new Vector2(0f, 0f)); _u.Add(new Vector2(1f, 0f)); _u.Add(new Vector2(1f, 1f)); _u.Add(new Vector2(0f, 1f));
                _t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
        }

        public GameObject Emit(Transform parent, string name, Material mat)
        {
            if (_t.Count == 0) return null;
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(_v);
            m.SetUVs(0, _u);
            m.SetTriangles(_t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }

    /// <summary>
    /// 탄천 풍경 (<see cref="CoastSpec.Extra"/>). 사용자 사진(탄천 둔치·보·벚나무 제방·야경·억새)과 지도(야탑 탄천종합운동장 앞)를 보고 상상해서 지었다.
    /// </summary>
    sealed class TancheonWorld
    {
        const float CityY = KoreaCoasts.TcCityY;
        const float Ext = 520f;   // 지형(x ±210) 밖으로 물길·둑·길을 이어 그리는 끝
        readonly CoastWorldBuilder _b;
        readonly CoastMapDef _m;
        readonly Transform _root;
        readonly System.Random _rng;
        readonly Season _season;

        public TancheonWorld(CoastWorldBuilder b)
        {
            _b = b;
            _m = b.Map;
            _root = new GameObject("Tancheon").transform;
            _root.SetParent(b.Root, false);
            _rng = new System.Random(6161);
            _season = Seasons.Current;
        }

        float R(float a, float b) => (float)(a + _rng.NextDouble() * (b - a));
        float H(float x, float z) => x > -209f && x < 209f && z > -199f && z < 269f ? _b.Height(x, z) : KoreaCoasts.TancheonHeight(_m, x, z);
        float Water => _m.BaseWaterY;
        float NearZ(float x, float d) => _m.ShoreAt(x) - d;
        float FarZ(float x, float e) => _m.FarShoreAt(x) + e;
        // 제방 위 도로 가운데 (양현로 / 탄천로)
        float NearRoadZ(float x) => _m.ShoreAt(x) - (KoreaCoasts.TcPlain + KoreaCoasts.TcLevee) - 9f;
        float FarRoadZ(float x) => _m.FarShoreAt(x) + KoreaCoasts.TcPlain + KoreaCoasts.TcLevee + 9f;

        public void Build()
        {
            Flow();
            CityGround();
            Corridor();
            Paths();
            Roads();
            Riprap();
            Reeds();
            Trees();
            ParkProps();
            SteppingStones();
            Weir();
            Footbridge();
            Festival();
            foreach (var (x, name) in new[] { (-KoreaCoasts.TcBridgeX, "야 탑 교"), (KoreaCoasts.TcBridgeX, "사 송 교"), (-440f, "서 현 교"), (440f, "탄 천 교") })
                CarBridge(x, name, Mathf.Abs(x) < 300f);
            Stadium(new Vector3(-48f, CityY, 172f));
            DroneShow.Create(_root, new Vector3(-48f, CityY + 82f, 172f), 0f).Height = 76f;   // 운동장 위 밤하늘 드론쇼 (사용자 요청)
            Ballpark(new Vector3(84f, CityY, 112f));
            Gymnasium(new Vector3(-188f, CityY, 160f));
            City();
            Skyline();
        }

        // ---------------------------------------------------------------- 흐름

        /// <summary>
        /// 물이 한강 쪽(하류 +x)으로 흐른다 (사용자: "한강 쪽으로 흐르는 느낌이 있어야"): 수면 물결 무늬 두 겹을 하류로 초속 0.5~0.8m로 밀고,
        /// 수면에 꽃잎·잎(봄 벚꽃잎, 가을 낙엽, 그 밖엔 버들잎) 몇십 장이 떠서 천천히 떠내려간다 (납작한 점, 물보라 같은 덩어리 파티클이 아니다).
        /// </summary>
        void Flow()
        {
            // Water.shader: tex(p * tiling + t * speed) → 무늬는 speed 반대쪽으로 움직인다. 하류(+x)로 흐르게 x를 음수로
            WaterSurface.Material?.SetVector("_Speed1", new Vector4(-0.05f, 0.004f, 0f, 0f));
            WaterSurface.Material?.SetVector("_Speed2", new Vector4(-0.09f, -0.01f, 0f, 0f));

            var go = new GameObject("FloatingPetals");
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(0f, Water + 0.03f, -10f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 70f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            Color a, b;
            switch (_season)
            {
                case Season.Spring: a = new Color(1f, 0.88f, 0.9f); b = new Color(0.98f, 0.76f, 0.82f); break;
                case Season.Autumn: a = new Color(0.85f, 0.5f, 0.2f); b = new Color(0.75f, 0.62f, 0.25f); break;
                default: a = new Color(0.55f, 0.65f, 0.3f); b = new Color(0.7f, 0.72f, 0.4f); break;
            }
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            var em = ps.emission;
            em.rateOverTime = 3.6f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(420f, 0.01f, 40f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            r.sharedMaterial = Mats.Particle(Color.white);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        // ---------------------------------------------------------------- 땅

        /// <summary>지형(x ±210, z -200~270) 밖 시가지 바닥: 지형과 겹치지 않게 여섯 조각, 높이는 시가지와 같다.</summary>
        void CityGround()
        {
            var ground = Mats.Textured("aggregate", new Color(0.62f, 0.62f, 0.6f), 60f, 0.05f);
            float y = CityY - 0.2f;
            void Slab(float x0, float x1, float z0, float z1) =>
                Mats.Prim(PrimitiveType.Cube, "CityGround", _root, new Vector3((x0 + x1) * 0.5f, y - 0.01f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.4f, z1 - z0), ground, false);
            Slab(-1600f, -209f, -1600f, -110f);
            Slab(209f, 1600f, -1600f, -110f);
            Slab(-209f, 209f, -1600f, -199f);
            Slab(-1600f, -209f, 91f, 1600f);
            Slab(209f, 1600f, 91f, 1600f);
            Slab(-209f, 209f, 269f, 1600f);
        }

        /// <summary>지형 밖 물길·둔치·제방 (x 205~520 양쪽): 같은 단면 함수로 격자 메쉬. 물은 WaterSurface가 x ±500까지 덮는다.</summary>
        void Corridor()
        {
            var grass = Mats.Textured("grass", new Color(0.78f, 0.86f, 0.7f), 9f, 0.05f);
            foreach (float side in new[] { -1f, 1f })
            {
                const int nx = 80, nz = 100;
                var v = new List<Vector3>();
                var t = new List<int>();
                for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                {
                    float x = side * Mathf.Lerp(205f, Ext, i / (float)nx);
                    float z = Mathf.Lerp(-111f, 92f, j / (float)nz);
                    v.Add(new Vector3(x, Mathf.Min(KoreaCoasts.TancheonHeight(_m, x, z), CityY), z));
                }
                for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    int a = i * (nz + 1) + j, b = a + 1, c = a + nz + 1, d = c + 1;
                    if (side > 0f) t.AddRange(new[] { a, b, c, b, d, c });
                    else t.AddRange(new[] { a, c, b, b, c, d });
                }
                var mesh = new Mesh { name = "TancheonCorridor", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetTriangles(t, 0);
                var uv = new Vector2[v.Count];
                for (int k = 0; k < v.Count; k++) uv[k] = new Vector2(v[k].x, v[k].z) / 9f;
                mesh.uv = uv;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go = new GameObject("Corridor");
                go.transform.SetParent(_root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = grass;
            }
        }

        // ---------------------------------------------------------------- 길

        /// <summary>
        /// z(x)를 따라가는 띠 길 (2m 마디, 땅에서 4cm 위, 옆면은 땅속으로). 콜라이더가 있어 그 위엔 풀이 안 난다(GrassField).
        /// skip(x)가 참인 마디는 건너뛴다 (보행교 경사로·낚시점 자리).
        /// </summary>
        GameObject Strip(string name, System.Func<float, float> zOf, float width, float x0, float x1, Material mat, float lift = 0.04f,
            System.Func<float, bool> skip = null, bool collider = true, float flatY = float.NaN)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            var uv = new List<Vector2>();
            float hw = width * 0.5f;
            int prev = -1;
            for (float x = x0; x <= x1 + 0.01f; x += 2f)
            {
                if (skip != null && skip(x)) { prev = -1; continue; }
                float z = zOf(x);
                float y0 = float.IsNaN(flatY) ? H(x, z - hw) : flatY, y1 = float.IsNaN(flatY) ? H(x, z + hw) : flatY, ym = float.IsNaN(flatY) ? H(x, z) : flatY;
                float top = Mathf.Max(ym, Mathf.Max(y0, y1)) + lift;
                int b = v.Count;
                v.Add(new Vector3(x, top - 0.4f, z - hw - 0.05f));
                v.Add(new Vector3(x, top, z - hw));
                v.Add(new Vector3(x, top, z + hw));
                v.Add(new Vector3(x, top - 0.4f, z + hw + 0.05f));
                uv.Add(new Vector2(x, z - hw)); uv.Add(new Vector2(x, z - hw)); uv.Add(new Vector2(x, z + hw)); uv.Add(new Vector2(x, z + hw));
                if (prev >= 0)
                {
                    t.AddRange(new[] { prev + 1, b + 1, prev + 2, prev + 2, b + 1, b + 2 });   // 윗면
                    t.AddRange(new[] { prev + 0, b + 0, prev + 1, prev + 1, b + 0, b + 1 });   // 옆면
                    t.AddRange(new[] { prev + 2, b + 2, prev + 3, prev + 3, b + 2, b + 3 });
                }
                prev = b;
            }
            if (t.Count == 0) return null;
            // 윗면이 위(+y)를 보도록 감는 방향 맞추기
            for (int i = 0; i < t.Count; i += 3)
            {
                var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                var outward = new Vector3(0f, 1f, 0f);
                if (Mathf.Abs(n.y) < Mathf.Abs(n.z) * 0.5f) outward = new Vector3(0f, 0f, c.z - zOf(c.x));
                if (Vector3.Dot(n, outward) < 0f) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
            }
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>띠 길 위 칠한 선 (offset = 길 가운데에서 옆으로, dash = 칠한 길이·빈 길이, 0이면 실선).</summary>
        void Line(System.Func<float, float> zOf, float offset, float width, float x0, float x1, Material mat, float dash = 0f, float gap = 0f,
            System.Func<float, bool> skip = null, float flatY = float.NaN)
        {
            var bm = new BlockMesh();
            float step = dash > 0f ? dash + gap : 2f;
            for (float x = x0; x < x1; x += step)
            {
                float xb = Mathf.Min(x + (dash > 0f ? dash : 2f), x1);
                if (skip != null && (skip(x) || skip(xb))) continue;
                float za = zOf(x) + offset, zb = zOf(xb) + offset;
                float ya = (float.IsNaN(flatY) ? H(x, za) : flatY) + 0.05f, yb = (float.IsNaN(flatY) ? H(xb, zb) : flatY) + 0.05f;
                float hw = width * 0.5f;
                bm.Face(new Vector3(x, ya, za - hw), new Vector3(xb, yb, zb - hw), new Vector3(xb, yb, zb + hw), new Vector3(x, ya, za + hw), Vector3.up, Color.white);
            }
            var go = bm.Emit(_root, "PaintLine", mat);
            if (go != null) go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        bool UnderFootRamp(float x, float z) =>
            Mathf.Abs(x - KoreaCoasts.TcFootX) < 3.2f && z > -53f && z < 34f;

        /// <summary>
        /// 둔치 길 (사용자 사진: 물가 쪽 붉은 탄성포장 산책로, 그 뒤 청록 자전거도로(흰 가운데 점선·가장자리 선), 맞은편은 마사토 산책로).
        /// 지형 밖으로도 x ±520까지 이어 그린다.
        /// </summary>
        void Paths()
        {
            var red = Mats.Weathered("asphalt", new Color(0.62f, 0.3f, 0.26f), 3f, 0.1f, 0f, 0f, 0.05f, null, 0.2f);
            var masa = Mats.Weathered("aggregate", new Color(0.78f, 0.7f, 0.56f), 2.5f, 0.05f, 0f, 0f, 0.1f, null, 0.3f);
            var bike = Mats.Weathered("asphalt", new Color(0.22f, 0.5f, 0.52f), 4f, 0.12f, 0f, 0f, 0.05f, null, 0.15f);
            var white = Mats.Lit(new Color(0.9f, 0.9f, 0.86f), 0.3f);
            var yellow = Mats.Lit(new Color(0.92f, 0.72f, 0.18f), 0.3f);
            float nWalk(float x) => NearZ(x, 9f);
            float nBike(float x) => NearZ(x, 18f);
            float fWalk(float x) => FarZ(x, 9f);
            float fBike(float x) => FarZ(x, 18f);
            Strip("WalkNear", nWalk, 2.6f, -Ext, Ext, red, 0.04f);
            Strip("BikeNear", nBike, 4.6f, -Ext, Ext, bike, 0.05f);
            Strip("WalkFar", fWalk, 2.6f, -Ext, Ext, masa, 0.04f);
            Strip("BikeFar", fBike, 4.6f, -Ext, Ext, bike, 0.05f);
            foreach (var zf in new System.Func<float, float>[] { nBike, fBike })
            {
                Line(zf, 0f, 0.12f, -Ext, Ext, yellow, 3f, 3f);
                Line(zf, -2.05f, 0.1f, -Ext, Ext, white);
                Line(zf, 2.05f, 0.1f, -Ext, Ext, white);
            }
            // 길 사이를 잇는 샛길 (둔치를 가로질러 물가로)
            for (float x = -190f; x < 200f; x += R(45f, 70f))
            {
                if (Mathf.Abs(x - KoreaCoasts.TcFootX) < 30f || Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 18f) continue;
                foreach (bool near in new[] { true, false })
                {
                    float za = near ? NearZ(x, 6f) : FarZ(x, 6f), zb = near ? NearZ(x, 21f) : FarZ(x, 21f);
                    var pts = new BlockMesh();
                    for (float z = Mathf.Min(za, zb); z < Mathf.Max(za, zb); z += 1.5f)
                    {
                        float y = H(x, z) + 0.03f;
                        pts.Face(new Vector3(x - 0.7f, y, z), new Vector3(x + 0.7f, y, z), new Vector3(x + 0.7f, H(x, z + 1.5f) + 0.03f, z + 1.5f),
                            new Vector3(x - 0.7f, H(x, z + 1.5f) + 0.03f, z + 1.5f), Vector3.up, Color.white);
                    }
                    var g = pts.Emit(_root, "SidePath", near ? red : masa);
                    if (g != null) g.AddComponent<MeshCollider>().sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
                }
            }
        }

        /// <summary>제방 위 도로 (양현로·탄천로): 4차로 아스팔트 + 노란 겹 중앙선·흰 차선, 양쪽 보도블록, 연석. 강 쪽 보도는 제방 산책로.</summary>
        void Roads()
        {
            var asphalt = Mats.Weathered("asphalt", new Color(0.3f, 0.31f, 0.32f), 5f, 0.12f, 0f, 0f, 0f, null, 0.6f);
            var walk = Mats.Weathered("greytile", new Color(0.72f, 0.66f, 0.6f), 2f, 0.1f, 0f, 0f, 0.05f, null, 0.4f);
            var curb = Mats.Weathered("concrete", new Color(0.74f, 0.74f, 0.72f), 2f, 0.1f, 0f, 0.2f, 0f, null, 0.6f);
            var white = Mats.Lit(new Color(0.88f, 0.88f, 0.85f), 0.3f);
            var yellow = Mats.Lit(new Color(0.9f, 0.68f, 0.12f), 0.3f);
            float y = CityY;
            foreach (bool near in new[] { true, false })
            {
                float sg = near ? -1f : 1f;
                System.Func<float, float> road = near ? NearRoadZ : FarRoadZ;
                Strip("Road", road, 13f, -Ext - 300f, Ext + 300f, asphalt, 0.02f, null, true, y);
                Strip("Sidewalk", x => road(x) - sg * 8.5f, 4f, -Ext - 300f, Ext + 300f, walk, 0.16f, null, true, y);
                Strip("LeveeWalk", x => road(x) + sg * 8.5f, 4f, -Ext - 300f, Ext + 300f, walk, 0.16f, null, true, y);
                foreach (float o in new[] { -6.6f, 6.6f })
                    Strip("Curb", x => road(x) + o, 0.22f, -Ext - 300f, Ext + 300f, curb, 0.18f, null, false, y);
                Line(road, -0.12f, 0.12f, -Ext - 300f, Ext + 300f, yellow, 0f, 0f, null, y);
                Line(road, 0.12f, 0.12f, -Ext - 300f, Ext + 300f, yellow, 0f, 0f, null, y);
                foreach (float o in new[] { -3.2f, 3.2f })
                    Line(road, o, 0.13f, -Ext - 300f, Ext + 300f, white, 3f, 5f, null, y);
                // 가로등 (도로 강 쪽 보도, 팔이 도로 위로)
                for (float x = -200f; x < 205f; x += 32f)
                    _b.Streetlight(new Vector3(x, y + 0.16f, road(x) + sg * 7.4f), near ? 0f : 180f);
                // 제방 위 보도 난간 (강 쪽 가장자리)
                Railing(x => road(x) + sg * 10.6f, -Ext - 300f, Ext + 300f, y + 0.16f, 1.1f);
            }
            // 다리로 이어지는 시가지 길 (야탑로·장미로)
            foreach (float bx in new[] { -KoreaCoasts.TcBridgeX, KoreaCoasts.TcBridgeX })
            {
                Mats.Prim(PrimitiveType.Cube, "CrossRoad", _root, new Vector3(bx, y + 0.01f, -900f), new Vector3(20f, 0.04f, 1580f), asphalt, false);
                Mats.Prim(PrimitiveType.Cube, "CrossRoad", _root, new Vector3(bx, y + 0.01f, 900f), new Vector3(20f, 0.04f, 1580f), asphalt, false);
            }
        }

        /// <summary>제방 보도 난간: 2m마다 각관 기둥 + 위·가운데 가로대 (짙은 녹색 도장).</summary>
        void Railing(System.Func<float, float> zOf, float x0, float x1, float y, float h)
        {
            var bm = new BlockMesh();
            var col = Color.white;
            for (float x = Mathf.Max(x0, -260f); x < Mathf.Min(x1, 260f); x += 2f)
            {
                var a = new Vector3(x, y, zOf(x));
                var b = new Vector3(x + 2f, y, zOf(x + 2f));
                var d = b - a;
                var rot = Quaternion.LookRotation(d.normalized);
                bm.Block(a + Vector3.up * h * 0.5f, new Vector3(0.07f, h, 0.07f), 0.01f, Quaternion.identity, col, col * 0.7f);
                bm.Block((a + b) * 0.5f + Vector3.up * h, new Vector3(0.06f, 0.06f, d.magnitude), 0.01f, rot, col, col);
                bm.Block((a + b) * 0.5f + Vector3.up * h * 0.5f, new Vector3(0.04f, 0.04f, d.magnitude), 0.01f, rot, col, col);
            }
            bm.Emit(_root, "LeveeRail", Mats.VertexAO(Mats.Lit(new Color(0.18f, 0.32f, 0.26f), 0.4f, 0.3f)));
        }

        // ---------------------------------------------------------------- 물가

        /// <summary>자연석 호안: 물가를 따라 군데군데 둥근 돌 무더기 (나머지는 갈대). 징검다리·보·보행교 곁은 돌을 더 촘촘히.</summary>
        void Riprap()
        {
            var rock = Mats.Textured("rock", new Color(0.78f, 0.76f, 0.72f), 1.2f, 0.25f);
            for (float x = -205f; x < 205f; x += R(1.6f, 3f))
            {
                float k = Mathf.PerlinNoise(x * 0.025f + 4f, 1.3f);
                bool busy = Mathf.Abs(x - KoreaCoasts.TcStonesX) < 14f || Mathf.Abs(x - KoreaCoasts.TcWeirX) < 16f || Mathf.Abs(x - KoreaCoasts.TcFootX) < 10f;
                foreach (bool near in new[] { true, false })
                {
                    if (!busy && k < (near ? 0.5f : 0.55f)) continue;
                    float z = near ? NearZ(x, R(-0.5f, 1.6f)) : FarZ(x, R(-0.5f, 1.6f));
                    if (UnderFootRamp(x, z)) continue;
                    _b.Props.Boulder(new Vector3(x, Water + R(-0.25f, 0.2f), z), R(0.55f, 1.3f), rock);
                }
            }
            // 보 아래 여울: 물 위로 머리를 내민 돌
            for (int i = 0; i < 46; i++)
            {
                float x = R(KoreaCoasts.TcWeirX + 5f, KoreaCoasts.TcWeirX + 50f);
                float z = R(NearZ(x, -3f), FarZ(x, -3f));
                _b.Props.Boulder(new Vector3(x, Mathf.Max(H(x, z), Water - 0.5f) + R(-0.2f, 0.1f), z), R(0.4f, 1.0f), rock);
            }
        }

        // ---------------------------------------------------------------- 갈대·억새

        static Texture2D _reedTex;
        static Season _reedSeason;

        /// <summary>
        /// 갈대·억새 포기 그림 (128 × 256, 알파): 바닥에서 휘어 오르는 잎 20여 장 + 키 큰 줄기 끝 깃털 이삭.
        /// 가을·겨울엔 잎이 짚빛, 이삭이 은빛으로 부풀고(사용자 사진: 가을 억새밭), 봄·여름엔 초록 잎에 이삭은 적고 보랏빛.
        /// </summary>
        Texture2D ReedTexture()
        {
            if (_reedTex != null && _reedSeason == _season) return _reedTex;
            _reedSeason = _season;
            const int Wd = 128, Ht = 256;
            var px = new Color[Wd * Ht];
            var rng = new System.Random(77);
            float Rr(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
            bool autumn = _season != Season.Summer;   // 봄에도 지난해 마른 갈대가 서 있다 (사용자 사진: 4월 탄천)
            var leafLo = autumn ? new Color(0.5f, 0.43f, 0.3f) : new Color(0.26f, 0.38f, 0.16f);
            var leafHi = autumn ? new Color(0.86f, 0.77f, 0.58f) : new Color(0.55f, 0.68f, 0.34f);
            var plume = autumn ? new Color(0.93f, 0.9f, 0.84f) : new Color(0.62f, 0.52f, 0.5f);
            void Dot(int x, int y, Color c, float a)
            {
                if (x < 0 || x >= Wd || y < 0 || y >= Ht) return;
                var p = px[y * Wd + x];
                float na = Mathf.Max(p.a, a);
                px[y * Wd + x] = new Color(Mathf.Lerp(p.r, c.r, a), Mathf.Lerp(p.g, c.g, a), Mathf.Lerp(p.b, c.b, a), na);
            }
            int blades = 26;
            for (int k = 0; k < blades; k++)
            {
                float x0 = Rr(Wd * 0.3f, Wd * 0.7f);
                float lean = Rr(-0.9f, 0.9f);
                float top = Rr(0.35f, 0.85f) * Ht;
                bool stem = k < (autumn ? 9 : 4);
                if (stem) { top = Rr(0.8f, 0.97f) * Ht; lean *= 0.4f; }
                for (int y = 0; y < top; y++)
                {
                    float f = y / top;
                    float x = x0 + lean * Wd * 0.5f * f * f;
                    float w = (stem ? 1.2f : 2.0f) * (1f - f * 0.8f);
                    var c = Color.Lerp(leafLo, leafHi, f * 0.9f + Rr(-0.05f, 0.05f));
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        float a = Mathf.Clamp01(w - Mathf.Abs(dx));
                        if (a > 0f) Dot(Mathf.RoundToInt(x) + dx, y, c, a);
                    }
                }
                if (!stem) continue;
                // 이삭: 줄기 끝에서 아래·옆으로 흩어지는 가는 깃털 가닥
                float tx = x0 + lean * Wd * 0.5f;
                int plumeLen = Mathf.RoundToInt(Ht * (autumn ? 0.2f : 0.12f));
                for (int s = 0; s < (autumn ? 70 : 30); s++)
                {
                    float sy = top - Rr(0f, plumeLen);
                    float ang = Rr(-1.1f, 1.1f);
                    float len = Rr(4f, autumn ? 16f : 9f);
                    for (float u = 0f; u < len; u += 0.7f)
                        Dot(Mathf.RoundToInt(tx + Mathf.Sin(ang) * u), Mathf.RoundToInt(sy - Mathf.Cos(ang) * u * 0.6f + u * 0.25f), plume * Rr(0.85f, 1.05f), Rr(0.55f, 0.95f));
                }
            }
            _reedTex = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "reeds" };
            _reedTex.SetPixels(px);
            _reedTex.Apply(true);
            return _reedTex;
        }

        /// <summary>
        /// 물가 갈대·억새 띠 (물가에서 -1~6m, 덩어리 노이즈로 끊긴다). 시작 둔치 앞·NPC 자리·보행교·징검다리 끝은 비워 물에 닿게.
        /// 포기마다 세 장 교차 판, 60m 칸마다 메쉬 하나 (Fishing/Grass, 바람에 흔들림).
        /// </summary>
        void Reeds()
        {
            var shader = Shader.Find("Fishing/Grass");
            if (shader == null) return;
            var mat = new Material(shader) { mainTexture = ReedTexture(), name = "Reeds" };
            mat.SetFloat("_Cutoff", 0.38f);
            mat.SetFloat("_Wind", 0.16f);
            var clumps = new List<(Vector3 p, float w, float h, float yaw, Color tint)>();
            bool Clear(float x, float z)
            {
                if (x > -24f && x < 18f && z < 0f) return true;   // 시작 둔치 앞 물가
                if (Mathf.Abs(x - KoreaCoasts.TcStonesX) < 3f || Mathf.Abs(x - KoreaCoasts.TcWeirX) < 4f) return true;
                if (UnderFootRamp(x, z)) return true;
                foreach (var n in _b.Spec.Npcs) if (new Vector2(n.Pos.x - x, n.Pos.z - z).magnitude < 4f) return true;
                return false;
            }
            void Add(float x, float z, float hScale)
            {
                float y = H(x, z);
                if (y < Water - 0.35f || Clear(x, z)) return;
                float h = R(1.3f, 2.2f) * hScale;
                var tint = Color.Lerp(new Color(0.85f, 0.85f, 0.8f), new Color(1.08f, 1.05f, 1f), Mathf.PerlinNoise(x * 0.2f, z * 0.2f)) * R(0.9f, 1.05f);
                clumps.Add((new Vector3(x, Mathf.Max(y, Water - 0.1f) - 0.05f, z), h * R(0.8f, 1.1f), h, R(0f, Mathf.PI), tint));
            }
            for (float x = -260f; x < 260f; x += 0.85f)
            {
                foreach (bool near in new[] { true, false })
                {
                    float k = Mathf.PerlinNoise(x * 0.03f + (near ? 0f : 40f), 2.7f);
                    if (k < 0.36f) continue;
                    int n = 1 + (k > 0.55f ? 1 : 0) + (k > 0.7f ? 1 : 0);
                    for (int i = 0; i < n; i++)
                    {
                        float d = R(-1.2f, 2.5f + 4f * (k - 0.36f));
                        float xx = x + R(-0.4f, 0.4f);
                        Add(xx, near ? NearZ(xx, d) : FarZ(xx, d), 0.8f + k * 0.5f);
                    }
                }
            }
            // 모래섬·버드나무 섬은 통째로 갈대
            foreach (var isl in _b.Spec.Islets)
                for (int i = 0; i < 26; i++)
                {
                    float a = R(0f, Mathf.PI * 2f), r = Mathf.Sqrt(R(0f, 1f)) * (isl.Radius + 0.6f);
                    Add(isl.Center.x + Mathf.Cos(a) * r, isl.Center.z + Mathf.Sin(a) * r, 1.1f);
                }
            // 둔치 억새밭 두 군데 (사용자 사진: 가을 억새 들판)
            foreach (var (cx, near) in new[] { (-150f, true), (120f, false) })
                for (int i = 0; i < 260; i++)
                {
                    float x = cx + R(-22f, 22f), d = R(20f, 34f);
                    Add(x, near ? NearZ(x, d) : FarZ(x, d), 0.75f);
                }

            var cells = new Dictionary<int, List<(Vector3 p, float w, float h, float yaw, Color tint)>>();
            foreach (var c in clumps)
            {
                int key = Mathf.FloorToInt(c.p.x / 60f);
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<(Vector3, float, float, float, Color)>();
                list.Add(c);
            }
            foreach (var kv in cells)
            {
                var v = new List<Vector3>();
                var uv = new List<Vector2>();
                var col = new List<Color>();
                var nrm = new List<Vector3>();
                var tri = new List<int>();
                foreach (var c in kv.Value)
                    for (int q = 0; q < 3; q++)
                    {
                        float a = c.yaw + q * Mathf.PI / 3f;
                        var dx = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (c.w * 0.5f);
                        int b = v.Count;
                        v.Add(c.p - dx); v.Add(c.p + dx); v.Add(c.p + dx + Vector3.up * c.h); v.Add(c.p - dx + Vector3.up * c.h);
                        uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
                        for (int k = 0; k < 4; k++) { col.Add(c.tint); nrm.Add(Vector3.up); }
                        tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                    }
                var mesh = new Mesh { name = "Reeds", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetUVs(0, uv);
                mesh.SetColors(col);
                mesh.SetNormals(nrm);
                mesh.SetTriangles(tri, 0);
                mesh.RecalculateBounds();
                var go = new GameObject("Reeds");
                go.transform.SetParent(_root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        // ---------------------------------------------------------------- 나무

        /// <summary>
        /// 제방 벚나무 줄(사용자 사진: 봄이면 제방 길이 분홍 터널) — 봄엔 꽃을 박고, 나머지 계절엔 그냥 활엽수.
        /// 물가 버드나무 무리, 둔치 느티나무 무리, 도로 가로수.
        /// </summary>
        void Trees()
        {
            var blossom = Mats.Lit(new Color(1f, 0.86f, 0.86f), 0.1f);
            bool spring = _season == Season.Spring;
            foreach (bool near in new[] { true, false })
            {
                for (float x = -205f; x < 205f; x += R(10f, 14f))
                {
                    if (Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 16f) continue;
                    // 제방 비탈 중턱과 제방 위 길가, 두 줄
                    foreach (float d in new[] { KoreaCoasts.TcPlain + 6.5f, KoreaCoasts.TcPlain + KoreaCoasts.TcLevee - 1f })
                    {
                        float z = near ? NearZ(x, d) : FarZ(x, d);
                        var at = new Vector3(x + R(-1.5f, 1.5f), H(x, z) - 0.1f, z);
                        if (spring) KenneyProps.SpawnBush(_root, at, R(0f, 360f), R(6.5f, 8.5f), 90, blossom, 0.34f, true);
                        else KenneyProps.Spawn("Kenney/Nature/tree_default", _root, at, R(0f, 360f), R(6.5f, 8.5f));
                    }
                }
            }
            // 개나리: 제방 비탈 아래쪽 (사용자 사진: 벚꽃 밑 노란 개나리 덤불). 봄이 아니면 그냥 초록 덤불
            var yellow = Mats.Lit(new Color(1f, 0.82f, 0.12f), 0.15f);
            foreach (bool near in new[] { true, false })
                for (float x = -204f; x < 205f; x += R(3.5f, 7f))
                {
                    if (Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 16f) continue;
                    float d = KoreaCoasts.TcPlain + R(1.5f, 5f);
                    float z = near ? NearZ(x, d) : FarZ(x, d);
                    var at = new Vector3(x, H(x, z) - 0.05f, z);
                    if (spring) KenneyProps.SpawnBush(_root, at, R(0f, 360f), R(1.2f, 1.8f), 70, yellow, 0.1f);
                    else if (_rng.NextDouble() < 0.4) KenneyProps.SpawnBush(_root, at, R(0f, 360f), R(1.1f, 1.6f));
                }
            Willows();
            // 물가 버드나무·둔치 나무 무리 (길과 길 사이 잔디, 산책로 바깥 물가)
            for (int i = 0; i < 55; i++)
            {
                float x = R(-200f, 200f);
                bool near = _rng.NextDouble() < 0.5;
                float d = _rng.NextDouble() < 0.35 ? R(3f, 6f) : R(22f, 35f);
                float z = near ? NearZ(x, d) : FarZ(x, d);
                if (UnderFootRamp(x, z) || Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 16f) continue;
                if (near && x > -45f && x < 20f && d < 30f) continue;   // 시작 둔치·낚시점 앞은 트인 채로
                if (Vector2.Distance(new Vector2(x, z), new Vector2(_b.Spec.ShopPos.x, _b.Spec.ShopPos.z)) < 10f) continue;
                if (OnCourt(x, z) || InFestival(x, z)) continue;
                KenneyProps.Spawn("Kenney/Nature/" + (_rng.NextDouble() < 0.5 ? "tree_oak" : "tree_fat"), _root, new Vector3(x, H(x, z) - 0.1f, z), R(0f, 360f), R(7f, 12f));
            }
            // 가로수 (도로 바깥 보도)
            foreach (bool near in new[] { true, false })
                for (float x = -204f; x < 205f; x += 16f)
                {
                    if (Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 14f) continue;
                    float z = near ? NearRoadZ(x) - 8.6f : FarRoadZ(x) + 8.6f;
                    KenneyProps.Spawn("Kenney/Nature/tree_default", _root, new Vector3(x, CityY + 0.1f, z), R(0f, 360f), R(8f, 10f));
                }
        }

        // ---------------------------------------------------------------- 버드나무

        static Texture2D _willowTex;

        /// <summary>
        /// 버들 가지 그림 (128 × 512, 알파): 아래(v 0)가 매달린 곳, 위(v 1)가 늘어진 끝. 가는 가지 22가닥이 길이도 시작 높이도 제각각이라
        /// 한 장 안에서도 커튼이 여러 겹으로 보인다. 잎은 좁고 짧게 아래로 비스듬히 촘촘히, 끝으로 갈수록 연두빛이 밝다 (사용자 참고 사진).
        /// </summary>
        static Texture2D WillowTexture()
        {
            if (_willowTex != null) return _willowTex;
            const int Wd = 128, Ht = 512;
            var px = new Color[Wd * Ht];
            var rng = new System.Random(31);
            float Rr(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
            for (int k = 0; k < 22; k++)
            {
                float x0 = Rr(5f, Wd - 5f), drift = Rr(-8f, 8f);
                int start = rng.Next(0, Ht / 4);
                float len = Rr(0.45f, 1f) * (Ht - start);
                for (int i = 0; i < len; i++)
                {
                    int y = start + i;
                    float f = i / len;
                    int x = Mathf.RoundToInt(x0 + drift * f * f + Mathf.Sin(y * 0.04f + k) * 1.5f);
                    if (x >= 0 && x < Wd) px[y * Wd + x] = new Color(0.38f, 0.42f, 0.2f, 1f);
                    if (i % 3 != 0) continue;
                    int side = (i / 3) % 2 == 0 ? 1 : -1;
                    var leaf = Color.Lerp(new Color(0.36f, 0.5f, 0.16f), new Color(0.78f, 0.88f, 0.4f), Mathf.Clamp01(f * 0.8f + Rr(-0.15f, 0.25f)));
                    int ll = rng.Next(4, 7);
                    for (int l = 1; l <= ll; l++)
                    {
                        int lx = x + side * Mathf.RoundToInt(l * 0.4f), ly = y + l;
                        if (lx >= 0 && lx < Wd && ly < Ht) px[ly * Wd + lx] = leaf;
                    }
                }
            }
            _willowTex = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "willow" };
            _willowTex.SetPixels(px);
            _willowTex.Apply(true);
            return _willowTex;
        }

        /// <summary>
        /// 물가 수양버들 (사용자 참고 사진 넷: 키 10~13m, 둥근 분수 모양 수관에서 가는 가지 커튼이 여러 겹으로 땅·물 가까이까지 늘어진다).
        /// 굵은 갈색 줄기 → 위·바깥으로 뻗는 큰 가지 넷(커튼 틈으로 보인다) → 수관(납작한 반구) 겉면 곳곳에서 가지 판 280장이 늘어진다:
        /// 가장자리(낮은 곳)는 땅 위 0.4~1.5m까지 길게, 꼭대기 쪽은 짧게 겹을 이룬다. 판은 바깥을 보게 둘레 방향으로 돌린다 (Fishing/Grass, 끝이 흔들린다).
        /// 봄엔 밝은 연두, 여름엔 짙은 초록, 가을엔 누런 초록.
        /// </summary>
        void Willows()
        {
            var shader = Shader.Find("Fishing/Grass");
            if (shader == null) return;
            var mat = new Material(shader) { mainTexture = WillowTexture(), name = "Willow" };
            mat.SetFloat("_Cutoff", 0.3f);
            mat.SetFloat("_Wind", 0.22f);
            var seasonTint = _season switch
            {
                Season.Spring => new Color(1.25f, 1.3f, 0.85f),
                Season.Summer => new Color(0.82f, 0.92f, 0.78f),
                Season.Autumn => new Color(1.05f, 1f, 0.7f),
                _ => new Color(0.9f, 0.9f, 0.75f),
            };
            var bark = Mats.Weathered("planks", new Color(0.26f, 0.21f, 0.16f), 1.2f, 0.1f, 0f, 0.2f, 0.3f, null, 0.5f);
            var spots = new List<Vector3> { new Vector3(-30f, 0f, NearZ(-30f, 4f)) };
            for (int i = 0; i < 40 && spots.Count < 16; i++)
            {
                float x = R(-200f, 200f);
                bool near = _rng.NextDouble() < 0.5;
                float z = near ? NearZ(x, R(3f, 5.5f)) : FarZ(x, R(3f, 5.5f));
                if (near && ((x > -25f && x < 20f) || (x > FestX0 - 8f && x < FestX1 + 8f))) continue;   // 시작 둔치 앞·축제 마당 앞은 트이게
                if (UnderFootRamp(x, z) || Mathf.Abs(x - KoreaCoasts.TcStonesX) < 9f || Mathf.Abs(x - KoreaCoasts.TcWeirX) < 9f || Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 20f) continue;
                bool crowded = false;
                foreach (var o in spots) if (new Vector2(o.x - x, o.z - z).magnitude < 13f) crowded = true;
                if (!crowded) spots.Add(new Vector3(x, 0f, z));
            }
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var col = new List<Color>();
            var nrm = new List<Vector3>();
            var tri = new List<int>();
            foreach (var sp in spots)
            {
                var g = new Vector3(sp.x, H(sp.x, sp.z) - 0.1f, sp.z);
                float scale = R(0.85f, 1.15f);
                float trunkH = R(3.6f, 4.6f) * scale;
                var lean = Quaternion.Euler(R(-8f, 8f), R(0f, 360f), 0f);
                var top = g + lean * Vector3.up * trunkH;
                MapKit.Beam(_root, g, top, 0.62f * scale, bark, true, "WillowTrunk");
                float crR = R(5.2f, 6.4f) * scale, crH = R(4.2f, 5.2f) * scale;
                var c = top + Vector3.up * (crH * 0.45f);
                // 큰 가지: 줄기 머리에서 수관 안쪽 위·바깥으로
                for (int k = 0; k < 4; k++)
                {
                    float a = k * Mathf.PI * 0.5f + R(-0.4f, 0.4f);
                    var tip = c + new Vector3(Mathf.Cos(a) * crR * 0.55f, crH * R(0.35f, 0.65f), Mathf.Sin(a) * crR * 0.55f);
                    MapKit.Beam(_root, top, tip, 0.28f * scale, bark, false, "WillowLimb");
                    MapKit.Beam(_root, tip, tip + new Vector3(Mathf.Cos(a) * 1.4f, R(0.3f, 1f), Mathf.Sin(a) * 1.4f), 0.14f * scale, bark, false, "WillowTwig");
                }
                var tint = Color.Lerp(new Color(0.9f, 0.94f, 0.86f), Color.white, R(0f, 1f)) * seasonTint;
                for (int k = 0; k < 280; k++)
                {
                    // 수관 겉면: 높이각 0~80°, 낮은 곳일수록 많이 (커튼 가장자리)
                    float th = R(0f, Mathf.PI * 2f);
                    float ph = Mathf.Pow(R(0f, 1f), 1.6f) * 80f * Mathf.Deg2Rad;
                    float rr = ph < 0.5f ? R(0.85f, 1.05f) : R(0.5f, 1.02f);   // 꼭대기 쪽은 안쪽 겹도
                    var hang = c + new Vector3(Mathf.Cos(ph) * Mathf.Cos(th) * crR * rr, Mathf.Sin(ph) * crH, Mathf.Cos(ph) * Mathf.Sin(th) * crR * rr);
                    float toGround = hang.y - g.y;
                    float drop = ph < 0.5f ? toGround - R(0.4f, 1.6f) : R(2.5f, Mathf.Max(2.6f, toGround * 0.75f));
                    float yaw = th + Mathf.PI * 0.5f + R(-0.35f, 0.35f);
                    var dx = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * R(0.35f, 0.6f);
                    var top0 = hang + Vector3.up * 0.25f;
                    int b = v.Count;
                    var flare = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th)) * drop * R(0.08f, 0.2f);   // 아래로 갈수록 바깥으로 퍼진다 (분수 모양)
                    v.Add(top0 - dx); v.Add(top0 + dx); v.Add(top0 + dx + Vector3.down * drop + flare); v.Add(top0 - dx + Vector3.down * drop + flare);
                    uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
                    var ct = tint * R(0.9f, 1.05f);
                    for (int q = 0; q < 4; q++) { col.Add(ct); nrm.Add(Vector3.up); }
                    tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                }
            }
            var mesh = new Mesh { name = "Willows", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetColors(col);
            mesh.SetNormals(nrm);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            var go = new GameObject("Willows");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ---------------------------------------------------------------- 연등 축제

        // 축제 마당: 이쪽 둔치 상류 쪽 (야탑교와 시작 둔치 사이), 자전거도로와 제방 사이
        const float FestX0 = -150f, FestX1 = -62f;

        bool InFestival(float x, float z) => x > FestX0 - 4f && x < FestX1 + 4f && z < NearZ(x, 19f) && z > NearZ(x, 38f);

        static Material Glow(Color c, float k = 1.8f) => HanRiverLandmarks.Mat(c, null, Texture2D.whiteTexture, c * k, 0.25f, 0f, Vector2.one);

        /// <summary>
        /// 비단 등 천 (64 × 96): 연한 바탕색에 매화·국화 꽃무늬(진한 색 꽃잎 점 + 가지 선) 또는 색동(분홍·하늘·노랑·연보라 가로띠), 위아래 진한 테.
        /// 밤엔 같은 그림이 빛 맵이 되어 속에서 켠 듯 무늬가 비친다 (사용자 사진: 탄천 등 축제의 비단 등).
        /// </summary>
        static Texture2D SilkTexture(Color baseCol, int seed, bool saekdong)
        {
            const int Wd = 64, Ht = 96;
            var t = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "silk" + seed };
            var rng = new System.Random(seed);
            var px = new Color[Wd * Ht];
            var bands = new[] { new Color(1f, 0.62f, 0.72f), new Color(0.6f, 0.85f, 0.98f), new Color(1f, 0.95f, 0.55f), new Color(0.82f, 0.7f, 0.95f), new Color(0.98f, 0.98f, 0.95f) };
            var bg = Color.Lerp(baseCol, Color.white, 0.25f);
            for (int y = 0; y < Ht; y++)
            for (int x = 0; x < Wd; x++)
            {
                var c = saekdong ? bands[(y / 9) % bands.Length] : bg;
                if (y < 4 || y > Ht - 5) c = baseCol * 0.7f;
                px[y * Wd + x] = c;
            }
            if (!saekdong)
            {
                var ink = Color.Lerp(baseCol, new Color(0.5f, 0.15f, 0.35f), 0.55f);
                var ink2 = new Color(0.25f, 0.45f, 0.75f);
                for (int k = 0; k < 9; k++)
                {
                    int cx = rng.Next(Wd), cy = rng.Next(8, Ht - 8);
                    var pc = rng.NextDouble() < 0.5 ? ink : ink2;
                    for (int pet = 0; pet < 5; pet++)
                    {
                        float a = pet * 1.2566f + k;
                        for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int x = (cx + Mathf.RoundToInt(Mathf.Cos(a) * 3f) + dx + Wd) % Wd, y = cy + Mathf.RoundToInt(Mathf.Sin(a) * 3f) + dy;
                            if (y >= 0 && y < Ht && dx * dx + dy * dy <= 4) px[y * Wd + x] = pc;
                        }
                    }
                    // 가지
                    for (int l = 0; l < 14; l++)
                    {
                        int x = (cx - l + Wd) % Wd, y = cy - l / 2 - 3;
                        if (y >= 0) px[y * Wd + x] = new Color(0.35f, 0.25f, 0.2f);
                    }
                }
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        /// <summary>
        /// 탄천 등 축제 (사용자 사진): 흰 뾰족 천막 줄, 알루미늄 걸대에 매단 비단 등(두세 색 이어 붙인 사각·원통 등), 물가 쪽 줄에 매단 붉은 연꽃등,
        /// 대나무 장대에 매단 긴 원통 등, 받침 위 큰 물고기 등(잉어·각시붕어·버들치 모양, 밤에 속에서 빛난다), 야탑교 앞 물 위 LED 무대(비계 + 파란 화면),
        /// 산책로 가장자리 초록 LED 점등, 자전거도로 위 별빛 조명(색색 빛 점). 밤에만 빛나고 낮엔 그냥 색 천·판.
        /// </summary>
        void Festival()
        {
            var root = new GameObject("LanternFestival").transform;
            root.SetParent(_root, false);
            var silkCols = new[] { new Color(1f, 0.55f, 0.6f), new Color(0.98f, 0.92f, 0.35f), new Color(0.4f, 0.8f, 0.95f), new Color(0.55f, 0.9f, 0.75f),
                                   new Color(0.85f, 0.45f, 0.85f), new Color(1f, 0.7f, 0.45f), new Color(0.95f, 0.95f, 0.92f), new Color(0.95f, 0.25f, 0.25f) };
            // 천 그림 열두 가지 (색마다 꽃무늬, 그중 넷은 색동)
            var silk = new Material[12];
            var bms = new UvBoxes[silk.Length];
            for (int i = 0; i < silk.Length; i++)
            {
                var tex = SilkTexture(silkCols[i % silkCols.Length], 700 + i, i >= 8);
                silk[i] = HanRiverLandmarks.Mat(Color.white, tex, tex, Color.white * 0.95f, 0.35f, 0f, Vector2.one);
                bms[i] = new UvBoxes();
            }
            var frame = new BlockMesh();
            var tent = new BlockMesh();
            var c = Color.white;
            float Gy(float x, float z) => H(x, z);
            void Lantern(Vector3 hang, bool round)
            {
                // 위아래 두세 칸 다른 색 (사진: 위 파랑·아래 분홍처럼 이어 붙인 비단 등)
                float y = hang.y;
                int parts = _rng.Next(2, 4);
                var q = Quaternion.Euler(0f, R(-10f, 10f), 0f);
                for (int p = 0; p < parts; p++)
                {
                    float h = R(0.45f, 0.7f);
                    float w = round ? 0.5f : 0.46f;
                    bms[_rng.Next(bms.Length)].Box(new Vector3(hang.x, y - h * 0.5f, hang.z), new Vector3(w, h, w), q);
                    y -= h + 0.02f;
                }
                frame.Block(new Vector3(hang.x, hang.y + 0.02f, hang.z), new Vector3(0.52f, 0.03f, 0.52f), 0f, q, c, c);
            }
            // 흰 천막 (뾰족 지붕) 줄
            for (float x = FestX0 + 4f; x < FestX1; x += 6.5f)
            {
                float z = NearZ(x, 33f), g = Gy(x, z);
                var b = new Vector3(x, g, z);
                foreach (var (dx, dz) in new[] { (-2.4f, -2.4f), (2.4f, -2.4f), (-2.4f, 2.4f), (2.4f, 2.4f) })
                    frame.Block(b + new Vector3(dx, 1.2f, dz), new Vector3(0.07f, 2.4f, 0.07f), 0f, Quaternion.identity, c, c);
                var apex = b + Vector3.up * 4.4f;
                var p0 = b + new Vector3(-2.6f, 2.4f, -2.6f); var p1 = b + new Vector3(2.6f, 2.4f, -2.6f);
                var p2 = b + new Vector3(2.6f, 2.4f, 2.6f); var p3 = b + new Vector3(-2.6f, 2.4f, 2.6f);
                foreach (var (a, d) in new[] { (p0, p1), (p1, p2), (p2, p3), (p3, p0) })
                {
                    var n = Vector3.Cross(d - a, apex - a).normalized;
                    if (n.y < 0f) n = -n;
                    tent.Face(a, d, apex, apex, n, c);
                    tent.Face(a, d, a + Vector3.up * 0.0f + Vector3.down * 0.25f, a + Vector3.down * 0.25f, n, c);   // 처마 띠
                }
                // 뒷벽 천
                tent.Face(p3 + Vector3.down * 2.4f, p2 + Vector3.down * 2.4f, p2, p3, Vector3.back, c * 0.95f);
            }
            // 등 걸대 (알루미늄 각관 틀 세 단에 비단 등)
            for (float x = FestX0 + 8f; x < FestX1 - 6f; x += 16f)
            {
                float z = NearZ(x, 25.5f), g = Gy(x, z);
                const float L = 7f;
                foreach (float dx in new[] { -L * 0.5f, 0f, L * 0.5f })
                    frame.Block(new Vector3(x + dx, g + 2.1f, z), new Vector3(0.08f, 4.2f, 0.08f), 0f, Quaternion.identity, c, c);
                for (int row = 0; row < 3; row++)
                {
                    float by = g + 1.6f + row * 1.25f;
                    frame.Block(new Vector3(x, by, z), new Vector3(L, 0.06f, 0.06f), 0f, Quaternion.identity, c, c);
                    for (float lx = -L * 0.5f + 0.5f; lx < L * 0.5f; lx += 0.78f)
                        Lantern(new Vector3(x + lx, by - 0.05f, z + R(-0.08f, 0.08f)), _rng.NextDouble() < 0.3);
                }
            }
            // 대나무 장대 + 매단 긴 등 (산책로 둔치 쪽)
            var bamboo = Mats.Lit(new Color(0.72f, 0.6f, 0.3f), 0.3f);
            for (float x = FestX0; x < FestX1 + 40f; x += 11f)
            {
                float z = NearZ(x, 11f), g = Gy(x, z);
                if (UnderFootRamp(x, z)) continue;
                MapKit.Beam(root, new Vector3(x, g, z), new Vector3(x + 0.3f, g + 7.5f, z - 0.6f), 0.1f, bamboo);
                var hang = new Vector3(x + 0.3f, g + 7f, z - 1.3f);
                frame.Block(hang + Vector3.up * 0.3f, new Vector3(0.02f, 0.6f, 0.02f), 0f, Quaternion.identity, c, c);
                for (int p = 0; p < 3; p++)
                    bms[_rng.Next(bms.Length)].Box(hang + Vector3.down * (0.35f + p * 0.64f), new Vector3(0.42f, 0.6f, 0.42f), Quaternion.Euler(0f, 45f, 0f));
            }
            // 물가 쪽 줄과 붉은 연꽃등 (산책로 물가 쪽 가장자리)
            var lotus = Glow(new Color(1f, 0.32f, 0.36f), 1.7f);
            var leaf = Mats.Lit(new Color(0.25f, 0.6f, 0.25f), 0.3f);
            var rope = new BlockMesh();
            var lotusBm = new BlockMesh();
            var leafBm = new BlockMesh();
            for (float x = FestX0; x < FestX1 + 40f; x += 2.6f)
            {
                float z = NearZ(x, 6.8f), g = Gy(x, z);
                if (UnderFootRamp(x, z)) continue;
                frame.Block(new Vector3(x, g + 0.55f, z), new Vector3(0.08f, 1.1f, 0.08f), 0f, Quaternion.identity, c, c);
                rope.Block(new Vector3(x + 1.3f, g + 1.0f, z), new Vector3(2.6f, 0.025f, 0.025f), 0f, Quaternion.identity, c, c);
                var lc = new Vector3(x + 1.3f, g + 0.82f, z);
                for (int k = 0; k < 6; k++)
                {
                    var q = Quaternion.Euler(R(-20f, 20f), k * 60f, 0f);
                    lotusBm.Block(lc + q * new Vector3(0f, 0.04f, 0.13f), new Vector3(0.2f, 0.32f, 0.08f), 0.03f, q * Quaternion.Euler(-25f, 0f, 0f), c, c);
                }
                lotusBm.Block(lc + Vector3.up * 0.05f, new Vector3(0.22f, 0.3f, 0.22f), 0.08f, Quaternion.identity, c, c);
                leafBm.Block(lc + Vector3.down * 0.13f, new Vector3(0.42f, 0.04f, 0.42f), 0.02f, Quaternion.Euler(0f, 45f, 0f), c, c);
            }
            // 큰 물고기 등: 받침 위에 탄천 물고기 모양 (밤에 속이 빛나는 종이 등)
            var plinth = Mats.Lit(new Color(0.12f, 0.12f, 0.13f), 0.4f);
            var fishLamps = new[] { ("carp", 420f, new Color(1f, 0.62f, 0.25f)), ("rosybitterling", 260f, new Color(1f, 0.55f, 0.65f)), ("chineseminnow", 300f, new Color(0.95f, 0.9f, 0.55f)),
                                    ("darkchub", 280f, new Color(0.45f, 0.75f, 1f)), ("catfish", 380f, new Color(0.7f, 0.95f, 0.6f)) };
            int fi = 0;
            for (float x = FestX0 + 10f; x < FestX1 - 4f && fi < fishLamps.Length; x += 17f, fi++)
            {
                var (id, cm, col) = fishLamps[fi];
                float z = NearZ(x, 19.5f + 2f), g = Gy(x, z);
                Mats.Prim(PrimitiveType.Cube, "LanternPlinth", root, new Vector3(x, g + 0.4f, z - 1.2f), new Vector3(4.6f, 0.8f, 2.4f), plinth);
                var sp = FishDatabase.Get(id);
                if (sp == null) continue;
                var fish = Fishing.Angling.FishModels.Create(sp, cm, root);
                fish.transform.position = new Vector3(x, g + 2.1f, z - 1.2f);
                fish.transform.rotation = Quaternion.Euler(R(-8f, 8f), 90f + R(-20f, 20f), R(-6f, 6f));
                Fishing.Angling.FishModels.Freeze(fish, Fishing.Angling.FishModels.SwimSlow, R(0f, 0.6f));
                var glow = Glow(col, 1.1f);
                foreach (var r in fish.GetComponentsInChildren<Renderer>())
                {
                    var ms = r.sharedMaterials;
                    for (int m = 0; m < ms.Length; m++) ms[m] = glow;
                    r.sharedMaterials = ms;
                }
                frame.Block(new Vector3(x, g + 1.45f, z - 1.2f), new Vector3(0.06f, 1.3f, 0.06f), 0f, Quaternion.identity, c, c);
                _b.AddLamp(new Vector3(x, g + 2.4f, z - 1.2f), col, 7f, 0.8f);
            }
            // 물 위 LED 무대 (야탑교 하류 앞): 비계 틀 + 크고 작은 파란 화면
            {
                float x = -KoreaCoasts.TcBridgeX + 22f, z = -10f;
                var scaff = new BlockMesh();
                for (int i = 0; i <= 4; i++)
                    for (int j = 0; j <= 3; j++)
                        scaff.Block(new Vector3(x + j * 0.01f, Water + 3f, z - 6f + i * 3f), new Vector3(0.06f, 6.5f, 0.06f), 0f, Quaternion.identity, c, c);
                for (int r = 0; r < 4; r++)
                    scaff.Block(new Vector3(x + 0.6f, Water + 0.4f + r * 1.8f, z), new Vector3(0.05f, 0.05f, 12f), 0f, Quaternion.identity, c, c);
                scaff.Emit(root, "StageScaffold", Mats.Lit(new Color(0.55f, 0.57f, 0.6f), 0.4f, 0.5f));
                var screen = HanRiverLandmarks.Mat(new Color(0.08f, 0.1f, 0.16f), null, Texture2D.whiteTexture, new Color(0.35f, 0.5f, 1f) * 2.6f, 0.6f, 0f, Vector2.one);
                var screen2 = HanRiverLandmarks.Mat(new Color(0.08f, 0.1f, 0.16f), null, Texture2D.whiteTexture, new Color(0.75f, 0.85f, 1f) * 2.8f, 0.6f, 0f, Vector2.one);
                foreach (var (dz, y, w, h, m) in new[] { (0f, 3.2f, 3f, 4.6f, screen2), (-2.6f, 4.4f, 1.6f, 2.2f, screen), (2.4f, 4.6f, 1.4f, 1.6f, screen), (-2.2f, 2.0f, 1.2f, 2f, screen2), (2.6f, 1.8f, 1.2f, 1.4f, screen2), (-4.4f, 1.6f, 0.8f, 1.2f, screen) })
                    Mats.Prim(PrimitiveType.Cube, "StageScreen", root, new Vector3(x - 0.1f, Water + y, z + dz), new Vector3(0.15f, h, w), m, false)
                        .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Mats.Prim(PrimitiveType.Cube, "StageFloat", root, new Vector3(x + 0.4f, Water + 0.05f, z), new Vector3(2.4f, 0.4f, 12.5f), plinth, false);
                _b.AddLamp(new Vector3(x - 3f, Water + 3f, z), new Color(0.45f, 0.6f, 1f), 22f, 1.6f);
            }
            // 산책로 가장자리 초록 LED (사용자 사진: 길 따라 점점이) — 양쪽 둔치 산책로
            var green = HanRiverLandmarks.Mat(new Color(0.3f, 0.35f, 0.3f), null, Texture2D.whiteTexture, new Color(0.2f, 1f, 0.5f) * 3f, 0.4f, 0f, Vector2.one);
            var dots = new BlockMesh();
            foreach (bool near in new[] { true, false })
                for (float x = -205f; x < 205f; x += 4f)
                    foreach (float o in new[] { -1.4f, 1.4f })
                    {
                        float z = (near ? NearZ(x, 9f) : FarZ(x, 9f)) + o;
                        if (UnderFootRamp(x, z)) continue;
                        dots.Block(new Vector3(x, H(x, z) + 0.06f, z), new Vector3(0.08f, 0.04f, 0.08f), 0.01f, Quaternion.identity, c, c);
                    }
            dots.Emit(root, "PathLed", green).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 자전거도로 위 별빛 조명 (빛을 쏘아 바닥에 맺힌 색색 점) — 축제 마당 앞
            var starCols = new[] { new Color(0.4f, 0.5f, 1f), new Color(0.75f, 0.4f, 1f), new Color(1f, 0.9f, 0.5f), new Color(0.4f, 1f, 0.9f), new Color(1f, 1f, 1f) };
            var starBm = new BlockMesh[starCols.Length];
            for (int i = 0; i < starBm.Length; i++) starBm[i] = new BlockMesh();
            for (int i = 0; i < 900; i++)
            {
                float x = R(FestX0, FestX1 + 30f);
                float z = NearZ(x, 18f) + R(-2.2f, 2.2f);
                float y = H(x, z) + 0.06f, r = R(0.04f, 0.1f);
                starBm[_rng.Next(starBm.Length)].Face(new Vector3(x - r, y, z - r), new Vector3(x + r, y, z - r), new Vector3(x + r, y, z + r), new Vector3(x - r, y, z + r), Vector3.up, c);
            }
            for (int i = 0; i < starBm.Length; i++)
            {
                var sm = HanRiverLandmarks.Mat(new Color(0.22f, 0.5f, 0.52f), null, Texture2D.whiteTexture, starCols[i] * 3f, 0.2f, 0f, Vector2.one);
                var sg = starBm[i].Emit(root, "PathStars", sm);
                if (sg != null) sg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            frame.Emit(root, "LanternFrames", Mats.Lit(new Color(0.75f, 0.77f, 0.8f), 0.5f, 0.6f));
            tent.Emit(root, "FestivalTents", Mats.Lit(new Color(0.95f, 0.94f, 0.9f), 0.2f));
            rope.Emit(root, "LotusRope", Mats.Lit(new Color(0.85f, 0.6f, 0.3f), 0.2f));
            lotusBm.Emit(root, "LotusLanterns", lotus).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            leafBm.Emit(root, "LotusLeaves", leaf);
            for (int i = 0; i < bms.Length; i++) bms[i].Emit(root, "SilkLanterns", silk[i]);
            // 밤 축제 불빛 (천막·걸대 둘레 따뜻한 빛)
            for (float x = FestX0 + 10f; x < FestX1; x += 30f)
                _b.AddLamp(new Vector3(x, Gy(x, NearZ(x, 27f)) + 3.5f, NearZ(x, 27f)), new Color(1f, 0.75f, 0.5f), 18f, 1.2f);
        }

        // ---------------------------------------------------------------- 둔치 소품

        /// <summary>벤치·파고라(그늘 쉼터)·휴지통·자전거도로 가로등·생태 안내판·수위표.</summary>
        void ParkProps()
        {
            var wood = Mats.Wood(new Color(0.62f, 0.44f, 0.28f));
            var steel = Mats.Lit(new Color(0.24f, 0.26f, 0.28f), 0.4f, 0.4f);
            var bm = new BlockMesh();
            var bmSteel = new BlockMesh();
            var c = Color.white;
            void Bench(Vector3 at, float yaw)
            {
                var q = Quaternion.Euler(0f, yaw, 0f);
                for (int s = 0; s < 4; s++)
                    bm.Block(at + q * new Vector3(0f, 0.45f, -0.18f + s * 0.12f), new Vector3(1.8f, 0.04f, 0.09f), 0.01f, q, c, c * 0.8f);
                for (int s = 0; s < 2; s++)
                    bm.Block(at + q * new Vector3(0f, 0.62f + s * 0.14f, 0.27f), new Vector3(1.8f, 0.1f, 0.03f), 0.01f, q * Quaternion.Euler(-12f, 0f, 0f), c, c * 0.8f);
                foreach (float lx in new[] { -0.75f, 0.75f })
                {
                    bmSteel.Block(at + q * new Vector3(lx, 0.22f, 0f), new Vector3(0.05f, 0.44f, 0.55f), 0.01f, q, c, c);
                    bmSteel.Block(at + q * new Vector3(lx, 0.62f, 0.27f), new Vector3(0.05f, 0.4f, 0.05f), 0.01f, q, c, c);
                }
            }
            // 산책로 물가 쪽 벤치 (물을 본다)
            for (float x = -200f; x < 200f; x += R(22f, 34f))
            {
                if (UnderFootRamp(x, -44f) || Mathf.Abs(x - KoreaCoasts.TcStonesX) < 5f) continue;
                float z = NearZ(x, 7f);
                Bench(new Vector3(x, H(x, z), z), 180f);
                float zf = FarZ(x + 9f, 7f);
                Bench(new Vector3(x + 9f, H(x + 9f, zf), zf), 0f);
            }
            // 파고라 (등나무 그늘 쉼터): 기둥 6 + 각재 지붕살
            foreach (var (px, near) in new[] { (-48f, true), (40f, true), (-60f, false), (150f, false) })
            {
                float pz = near ? NearZ(px, 27f) : FarZ(px, 27f);
                var at = new Vector3(px, H(px, pz), pz);
                for (int i = 0; i < 3; i++)
                foreach (float sz in new[] { -1.8f, 1.8f })
                    bm.Block(at + new Vector3(-3f + i * 3f, 1.3f, sz), new Vector3(0.22f, 2.6f, 0.22f), 0.02f, Quaternion.identity, c, c * 0.6f);
                foreach (float sz in new[] { -1.8f, 1.8f })
                    bm.Block(at + new Vector3(0f, 2.68f, sz), new Vector3(6.8f, 0.18f, 0.16f), 0.02f, Quaternion.identity, c, c);
                for (int i = 0; i < 12; i++)
                    bm.Block(at + new Vector3(-3.3f + i * 0.6f, 2.84f, 0f), new Vector3(0.08f, 0.14f, 4.6f), 0.01f, Quaternion.identity, c, c);
                Bench(at + new Vector3(-1.5f, 0f, -0.9f), 0f);
                Bench(at + new Vector3(1.5f, 0f, 0.9f), 180f);
                // 등나무 덩굴 (지붕살 위 잎 덩어리)
                KenneyProps.SpawnBush(_root, at + new Vector3(-1.6f, 2.7f, 0f), R(0f, 360f), 1.3f);
                KenneyProps.SpawnBush(_root, at + new Vector3(1.8f, 2.7f, 0.4f), R(0f, 360f), 1.2f);
            }
            Courts();
            bm.Emit(_root, "ParkWood", Mats.VertexAO(wood));
            bmSteel.Emit(_root, "ParkSteel", steel);
            // 자전거도로 가로등 (둔치 쪽, 40m마다)
            foreach (bool near in new[] { true, false })
                for (float x = -190f + (near ? 0f : 20f); x < 200f; x += 40f)
                {
                    if (UnderFootRamp(x, near ? -50f : 30f) || Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 14f) continue;
                    float z = near ? NearZ(x, 21f) : FarZ(x, 21f);
                    _b.Streetlight(new Vector3(x, H(x, z) - 0.05f, z), near ? 0f : 180f);
                }
        }

        /// <summary>둔치 운동장 자리 (x 가운데, 이쪽 둔치인지, 길이, 종류 0 농구 · 1 풋살 · 2 테니스 · 3 스케이트장): 이 둘레엔 나무를 두지 않는다.</summary>
        static readonly (float x, bool near, float len, int kind)[] CourtSpots =
        {
            (86f, true, 28f, 0), (118f, true, 30f, 1), (36f, false, 34f, 2), (74f, false, 34f, 2), (-110f, false, 60f, 3),
        };

        bool OnCourt(float x, float z)
        {
            foreach (var (cx, near, len, _) in CourtSpots)
            {
                float zc = near ? NearZ(cx, 28.5f) : FarZ(cx, 28.5f);
                if (Mathf.Abs(x - cx) < len * 0.5f + 3f && Mathf.Abs(z - zc) < 10f) return true;
            }
            return false;
        }

        static Texture2D CourtTex(int kind)
        {
            const int Wd = 256, Ht = 128;
            var t = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { name = "court" + kind };
            Color bg = kind switch { 0 => new Color(0.25f, 0.5f, 0.82f), 1 => new Color(0.3f, 0.62f, 0.6f), 2 => new Color(0.32f, 0.55f, 0.35f), _ => new Color(0.66f, 0.33f, 0.26f) };
            Color rim = kind == 2 ? new Color(0.8f, 0.42f, 0.28f) : kind == 0 ? new Color(0.92f, 0.94f, 0.96f) : bg * 0.9f;
            var white = new Color(0.95f, 0.95f, 0.93f);
            for (int y = 0; y < Ht; y++)
            for (int x = 0; x < Wd; x++)
            {
                float u = x / (float)Wd, v = y / (float)Ht;
                bool edge = u < 0.08f || u > 0.92f || v < 0.12f || v > 0.88f;
                var c = edge ? rim : bg;
                bool line = false;
                if (kind == 0)
                {
                    // 농구장 (위성사진의 파란 농구장): 가운데 원, 양쪽 흰 반원 구역과 붉은 제한 구역
                    float dx = (u - 0.5f) * 4f, dy = (v - 0.5f) * 2f;
                    line |= Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.22f) < 0.012f || Mathf.Abs(u - 0.5f) < 0.004f;
                    foreach (float ex in new[] { 0.08f, 0.92f })
                    {
                        float rx = (u - ex) * 2f, ry = v - 0.5f;
                        if (!edge && Mathf.Sqrt(rx * rx + ry * ry) < 0.34f) c = white;
                        if (!edge && Mathf.Abs(u - ex) < 0.12f && Mathf.Abs(ry) < 0.1f) c = new Color(0.8f, 0.25f, 0.2f);
                    }
                }
                else if (kind == 2)
                {
                    // 테니스장: 주황 테두리 안 초록 코트, 단식·복식·서비스 선, 네트
                    if (u < 0.15f || u > 0.85f || v < 0.2f || v > 0.8f) c = rim;
                    line |= Mathf.Abs(u - 0.5f) < 0.004f && v > 0.2f && v < 0.8f;
                    line |= (Mathf.Abs(v - 0.2f) < 0.006f || Mathf.Abs(v - 0.8f) < 0.006f || Mathf.Abs(v - 0.27f) < 0.006f || Mathf.Abs(v - 0.73f) < 0.006f) && u > 0.15f && u < 0.85f;
                    line |= (Mathf.Abs(u - 0.15f) < 0.004f || Mathf.Abs(u - 0.85f) < 0.004f || Mathf.Abs(u - 0.3f) < 0.004f || Mathf.Abs(u - 0.7f) < 0.004f) && v > 0.2f && v < 0.8f;
                    line |= Mathf.Abs(v - 0.5f) < 0.004f && u > 0.3f && u < 0.7f;
                }
                else if (kind == 1)
                {
                    float dx = (u - 0.5f) * 2f, dy = v - 0.5f;
                    line |= Mathf.Abs(u - 0.5f) < 0.004f || Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.12f) < 0.008f;
                }
                if (kind != 2 && kind != 3 && !edge && (Mathf.Abs(u - 0.08f) < 0.005f || Mathf.Abs(u - 0.92f) < 0.005f || Mathf.Abs(v - 0.12f) < 0.008f || Mathf.Abs(v - 0.88f) < 0.008f)) line = true;
                if (line) c = white;
                t.SetPixel(x, y, c * (0.97f + 0.03f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f)));
            }
            t.Apply(true);
            return t;
        }

        /// <summary>
        /// 둔치 운동장 (사용자 위성사진): 이쪽 둔치엔 파란 농구장·청록 풋살장, 맞은편엔 초록 테니스장 둘(주황 테두리, 철망 울타리),
        /// 상류 쪽 맞은편엔 붉은 바닥 야외스케이트장(낮은 울타리). 바닥은 둔치 높이에 평평하게.
        /// </summary>
        void Courts()
        {
            var fence = Mats.Lit(new Color(0.2f, 0.3f, 0.24f), 0.3f, 0.3f);
            var bm = new BlockMesh();
            var c = Color.white;
            foreach (var (cx, near, len, kind) in CourtSpots)
            {
                float zc = near ? NearZ(cx, 28.5f) : FarZ(cx, 28.5f);
                const float wid = 15f;
                float y = Mathf.Max(H(cx - len * 0.5f, zc), Mathf.Max(H(cx + len * 0.5f, zc), H(cx, zc))) + 0.06f;
                Mats.Prim(PrimitiveType.Cube, "Court", _root, new Vector3(cx, y - 0.15f, zc), new Vector3(len, 0.3f, wid), TexMat(CourtTex(kind), Vector2.one, 0.25f));
                if (kind == 0)
                    foreach (float s in new[] { -1f, 1f })
                    {
                        bm.Block(new Vector3(cx + s * (len * 0.5f - 0.6f), y + 1.6f, zc), new Vector3(0.15f, 3.2f, 0.15f), 0f, Quaternion.identity, c, c);
                        bm.Block(new Vector3(cx + s * (len * 0.5f - 1.2f), y + 3.3f, zc), new Vector3(0.08f, 1.05f, 1.8f), 0f, Quaternion.identity, c, c);
                    }
                if (kind != 2 && kind != 3) continue;
                float hx = len * 0.5f, hz = wid * 0.5f, fh = kind == 2 ? 4f : 1.2f;
                for (float t = -hx; t <= hx + 0.01f; t += 3f)
                    foreach (float s in new[] { -1f, 1f })
                        bm.Block(new Vector3(cx + t, y + fh * 0.5f, zc + s * hz), new Vector3(0.08f, fh, 0.08f), 0f, Quaternion.identity, c, c);
                foreach (float s in new[] { -1f, 1f })
                    for (float k = 0.4f; k <= fh; k += kind == 2 ? 0.5f : 0.4f)
                    {
                        bm.Block(new Vector3(cx, y + k, zc + s * hz), new Vector3(len, 0.02f, 0.02f), 0f, Quaternion.identity, c, c);
                        bm.Block(new Vector3(cx + s * hx, y + k, zc), new Vector3(0.02f, 0.02f, wid), 0f, Quaternion.identity, c, c);
                    }
                if (kind == 2) bm.Block(new Vector3(cx, y + 0.5f, zc), new Vector3(0.04f, 1f, wid * 0.62f), 0f, Quaternion.identity, c, c);
            }
            var g = bm.Emit(_root, "CourtFence", fence);
            if (g != null) g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- 징검다리

        /// <summary>
        /// 징검다리 (사용자 참고 사진 다섯 장 + "너무 규칙적이라 기괴하다, 사람 손맛·울퉁불퉁·비켜설 바위"):
        /// 줄이 물을 건너며 살짝 굽이치고(사인 둘), 돌마다 크기·높이·기울기·방향이 다르고, 엇갈림 폭도 들쭉날쭉 — 가끔 두 개가 나란히, 가끔 하나가 가운데.
        /// 다듬은 화강석 블록 사이에 모서리 둥근 자연석이 섞이고, 물 가운데 두 곳엔 넓적한 너럭돌을 놓아 마주 오는 사람과 비켜선다.
        /// 돌 사이 틈으로 빠지는 물살은 얇은 흐르는 물 판(Fishing/Waterfall, 파티클 없음)으로. 양 끝은 콘크리트 계단 참, 바닥엔 막돌.
        /// 걷는 받침은 돌과 돌 사이를 잇는 보이지 않는 판 (돌 위를 디디듯 지나간다).
        /// </summary>
        void SteppingStones()
        {
            float x0 = KoreaCoasts.TcStonesX;
            var granite = Mats.VertexAO(Mats.Weathered("granite", new Color(0.8f, 0.79f, 0.76f), 1.2f, 0.12f, 1f, 0.15f, 0.25f, null, 0.55f, 0f, null, 1.25f));
            var field = Mats.VertexAO(Mats.Weathered("granite", new Color(0.66f, 0.64f, 0.6f), 1.4f, 0.1f, 1f, 0.1f, 0.45f, new Color(0.3f, 0.36f, 0.2f), 0.5f, 0f, null, 1.35f));
            var conc = Mats.VertexAO(Mats.Weathered("concrete", new Color(0.8f, 0.79f, 0.75f), 2f, 0.12f, 1f, 0.3f, 0.2f, null, 0.5f));
            var bmCut = new BlockMesh();
            var bmField = new BlockMesh();
            var bmLand = new BlockMesh();
            var bmWalk = new BlockMesh();
            var wet = new Color(0.42f, 0.44f, 0.4f);
            var mossy = new Color(0.55f, 0.58f, 0.48f);
            float z0 = NearZ(x0, 1.2f), z1 = FarZ(x0, 1.2f);
            float top = Water + 0.4f;
            // 줄 가운데 x: 두 사인을 겹쳐 굽이친다 (최대 ±0.9m)
            float Mid(float z) => x0 + 0.55f * Mathf.Sin(z * 0.11f + 0.7f) + 0.3f * Mathf.Sin(z * 0.29f + 2.1f);
            // 비켜설 너럭돌 자리 (물길 1/3, 2/3쯤)
            float zw1 = Mathf.Lerp(z0, z1, 0.36f), zw2 = Mathf.Lerp(z0, z1, 0.7f);
            var steps = new List<Vector3>();   // 디딤 자리 (윗면 가운데)
            float z = z0 + 0.45f;
            int side = 1;
            while (z < z1 - 0.4f)
            {
                if (Mathf.Abs(z - zw1) < 1.2f || Mathf.Abs(z - zw2) < 1.2f)
                {
                    // 너럭돌: 넓적하고 낮게 깔린 큰 돌 + 곁에 작은 돌 둘
                    float zc = Mathf.Abs(z - zw1) < 1.2f ? zw1 : zw2;
                    var c = new Vector3(Mid(zc) + R(-0.3f, 0.3f), top - 0.05f + R(-0.03f, 0.03f), zc);
                    bmField.Block(c + Vector3.down * 0.9f, new Vector3(R(2.6f, 3.1f), 1.8f, R(1.9f, 2.3f)), 0.32f, Quaternion.Euler(R(-2f, 2f), R(-25f, 25f), R(-2f, 2f)), mossy, wet);
                    for (int k = 0; k < 2; k++)
                    {
                        var sc = c + new Vector3(R(1.4f, 2.2f) * (k == 0 ? 1f : -1f), R(-0.25f, -0.1f), R(-0.8f, 0.8f));
                        bmField.Block(sc + Vector3.down * 0.8f, new Vector3(R(0.7f, 1.1f), 1.6f, R(0.6f, 0.9f)), 0.22f, Quaternion.Euler(R(-6f, 6f), R(0f, 180f), R(-6f, 6f)), mossy, wet);
                    }
                    steps.Add(c);
                    z = zc + 1.35f;
                    continue;
                }
                float mx = Mid(z);
                float r = (float)_rng.NextDouble();
                if (r < 0.12f)
                {
                    // 두 개가 나란히 (조금 엇갈려)
                    foreach (float sg in new[] { -1f, 1f })
                    {
                        var c = new Vector3(mx + sg * R(0.5f, 0.65f), top + R(-0.06f, 0.05f), z + sg * R(0f, 0.12f));
                        bmCut.Block(c + Vector3.down * 0.85f, new Vector3(R(0.95f, 1.15f), 1.7f, R(0.6f, 0.75f)), R(0.05f, 0.1f),
                            Quaternion.Euler(R(-2.5f, 2.5f), R(-10f, 10f), R(-2.5f, 2.5f)), Color.white, wet);
                    }
                    steps.Add(new Vector3(mx, top, z));
                }
                else
                {
                    bool natural = r > 0.82f;
                    float off = side * R(0.18f, 0.55f) * (r < 0.22f ? 0.2f : 1f);   // 가끔은 가운데
                    var c = new Vector3(mx + off, top + R(-0.07f, 0.06f) - (natural ? 0.04f : 0f), z);
                    var size = natural ? new Vector3(R(1.0f, 1.35f), 1.7f, R(0.8f, 1.05f)) : new Vector3(R(1.05f, 1.45f), 1.7f, R(0.62f, 0.82f));
                    var q = Quaternion.Euler(R(-3f, 3f), natural ? R(0f, 180f) : R(-14f, 14f), R(-3f, 3f));
                    if (natural) bmField.Block(c + Vector3.down * 0.85f, size, R(0.22f, 0.32f), q, mossy, wet);
                    else bmCut.Block(c + Vector3.down * 0.85f, size, R(0.04f, 0.11f), q, Color.white, wet);
                    steps.Add(c);
                    side = -side;
                    if (_rng.NextDouble() < 0.15) side = -side;   // 같은 쪽이 두 번
                }
                z += R(0.72f, 1.02f);
            }
            // 줄 밖으로 흩어진 돌 (물가 쪽은 많이, 가운데는 몇 개만 머리를 내민다)
            for (int k = 0; k < 18; k++)
            {
                float zz = R(z0, z1);
                float edge = Mathf.Min(zz - z0, z1 - zz);
                if (edge > 8f && _rng.NextDouble() < 0.6) continue;
                var c = new Vector3(Mid(zz) + R(1.4f, 3.2f) * (_rng.NextDouble() < 0.6 ? 1f : -1f), Water + R(-0.25f, 0.15f), zz);
                bmField.Block(c + Vector3.down * 0.7f, new Vector3(R(0.6f, 1.2f), 1.4f, R(0.5f, 1f)), 0.25f, Quaternion.Euler(R(-8f, 8f), R(0f, 180f), R(-8f, 8f)), mossy, wet);
            }
            for (int k = 0; k < 70; k++)
            {
                float zz = R(z0, z1);
                _b.Props.Boulder(new Vector3(Mid(zz) + R(-1.8f, 3.5f), Mathf.Min(H(x0, zz), Water - 0.25f) + R(-0.1f, 0.15f), zz), R(0.25f, 0.6f), field);
            }
            // 걷는 받침: 디딤 자리끼리 잇는 보이지 않는 판 (윗면보다 조금 낮게)
            for (int i = 0; i + 1 < steps.Count; i++)
            {
                var a = steps[i];
                var b = steps[i + 1];
                var d = b - a;
                d.y = 0f;
                if (d.sqrMagnitude < 0.01f) continue;
                bmWalk.Block((a + b) * 0.5f + Vector3.down * 0.08f, new Vector3(1.0f, 0.1f, d.magnitude + 0.5f), 0f, Quaternion.LookRotation(d.normalized), Color.white, Color.white);
            }
            // 양쪽 둑 끝 콘크리트 계단 참 (물가 → 둔치), 줄 끝 자리에 맞춰
            foreach (var (zEnd, dir) in new[] { (z0, -1f), (z1, 1f) })
            {
                float mx = Mid(zEnd);
                for (int st = 0; st < 4; st++)
                {
                    float zz = zEnd + dir * (0.2f + st * 0.55f);
                    float yy = Mathf.Lerp(top, H(mx, zEnd + dir * 3.2f) + 0.05f, st / 3f);
                    bmLand.Block(new Vector3(mx, yy - 0.5f, zz), new Vector3(3.2f, 1f, 0.6f), 0.03f, Quaternion.Euler(0f, R(-3f, 3f), 0f), Color.white, wet);
                }
            }
            foreach (var (bm, mat, name) in new[] { (bmCut, granite, "SteppingStones"), (bmField, field, "SteppingFieldStones"), (bmLand, conc, "StoneLanding") })
            {
                var go = bm.Emit(_root, name, mat);
                if (go != null) go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            var walk = bmWalk.Emit(_root, "StoneWalkway", granite);
            walk.AddComponent<MeshCollider>().sharedMesh = walk.GetComponent<MeshFilter>().sharedMesh;
            walk.GetComponent<Renderer>().enabled = false;
            // 돌 사이 틈으로 빠져 하류로 번지는 물살: 디딤돌마다 하류 쪽 수면에 얇은 흐르는 물 판 (uv v가 하류로 흐른다)
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            foreach (var st in steps)
            {
                float w = R(0.6f, 1.1f), l = R(1.6f, 3.2f), zc = st.z + R(-0.45f, 0.45f);
                float xs = st.x + 0.55f, y = Water + 0.012f;
                int b = v.Count;
                v.Add(new Vector3(xs, y, zc - w * 0.5f)); v.Add(new Vector3(xs, y, zc + w * 0.5f));
                v.Add(new Vector3(xs + l, y, zc + w * 0.8f)); v.Add(new Vector3(xs + l, y, zc - w * 0.8f));
                float u0 = R(0f, 1f);
                uv.Add(new Vector2(u0, 0f)); uv.Add(new Vector2(u0 + 0.4f, 0f)); uv.Add(new Vector2(u0 + 0.4f, -l * 0.6f)); uv.Add(new Vector2(u0, -l * 0.6f));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "StoneRunoff" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var flow = new GameObject("StoneRunoff");
            flow.transform.SetParent(_root, false);
            flow.AddComponent<MeshFilter>().sharedMesh = mesh;
            var fr = flow.AddComponent<MeshRenderer>();
            fr.sharedMaterial = MapKit.WaterfallMat(0.35f, 0.3f, new Color(0.85f, 0.9f, 0.88f));
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- 보

        /// <summary>
        /// 보(洑): 물길을 가로지르는 낮은 콘크리트 턱 + 하류 쪽 두 단 계단 비탈 (사용자 사진: 물이 하얗게 넘쳐흐르는 계단식 보).
        /// 턱 위를 넘는 얇은 물 막(Fishing/Waterfall 흐르는 텍스처), 아래는 돌 많은 여울. 물보라 파티클은 과해서 뺐다 (사용자 지적).
        /// </summary>
        void Weir()
        {
            float x = KoreaCoasts.TcWeirX;
            float z0 = NearZ(x, 2.5f), z1 = FarZ(x, 2.5f);
            var conc = Mats.VertexAO(Mats.Weathered("concrete", new Color(0.82f, 0.8f, 0.76f), 2.5f, 0.2f, 1f, 0.4f, 0.35f, new Color(0.3f, 0.36f, 0.22f), 0.5f));
            var bm = new BlockMesh();
            var wet = new Color(0.55f, 0.56f, 0.5f);
            float crest = Water + 0.24f;
            for (float z = z0; z < z1; z += 6f)
            {
                float len = Mathf.Min(6f, z1 - z) - 0.02f, zc = z + len * 0.5f;
                bm.Block(new Vector3(x, crest - 1.2f, zc), new Vector3(2.2f, 2.4f, len), 0.05f, Quaternion.identity, Color.white, wet);
                bm.Block(new Vector3(x + 2.1f, crest - 0.25f - 1.2f, zc), new Vector3(2.0f, 2.4f, len), 0.05f, Quaternion.identity, Color.white, wet);
                bm.Block(new Vector3(x + 4f, crest - 0.5f - 1.2f, zc), new Vector3(1.8f, 2.4f, len), 0.05f, Quaternion.identity, Color.white, wet);
            }
            // 양쪽 둑 날개벽
            foreach (float z in new[] { z0 - 1.2f, z1 + 1.2f })
                bm.Block(new Vector3(x + 1.5f, Water + 0.2f, z), new Vector3(6.5f, 2f, 1.2f), 0.06f, Quaternion.identity, Color.white, wet);
            var go = bm.Emit(_root, "Weir", conc);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;

            // 넘쳐흐르는 물 막: 턱 위(위류 쪽 가장자리) → 두 계단을 타고 하류 수면으로
            var prof = new[] { new Vector2(-1.15f, crest + 0.06f), new Vector2(1.0f, crest + 0.05f), new Vector2(1.3f, crest - 0.12f), new Vector2(3.0f, crest - 0.2f),
                               new Vector2(3.25f, crest - 0.38f), new Vector2(4.8f, crest - 0.45f), new Vector2(5.4f, Water + 0.01f) };
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            int nz = Mathf.CeilToInt((z1 - z0) / 2f);
            float vAcc = 0f;
            var vs = new float[prof.Length];
            for (int i = 1; i < prof.Length; i++) { vAcc += Vector2.Distance(prof[i], prof[i - 1]); vs[i] = vAcc; }
            for (int j = 0; j <= nz; j++)
            {
                float z = Mathf.Lerp(z0, z1, j / (float)nz);
                for (int i = 0; i < prof.Length; i++)
                {
                    v.Add(new Vector3(x + prof[i].x, prof[i].y, z));
                    uv.Add(new Vector2(z / 6f, -vs[i] / 3f));
                }
            }
            int row = prof.Length;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < row - 1; i++)
            {
                int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                t.AddRange(new[] { a, c, b, b, c, d });
            }
            var mesh = new Mesh { name = "WeirFlow" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var flow = new GameObject("WeirFlow");
            flow.transform.SetParent(_root, false);
            flow.AddComponent<MeshFilter>().sharedMesh = mesh;
            var fr = flow.AddComponent<MeshRenderer>();
            fr.sharedMaterial = MapKit.WaterfallMat(0.9f, 0.75f, new Color(0.85f, 0.9f, 0.88f));
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- 보행교

        /// <summary>
        /// 탄천 보행교 (사용자 사진: 조깅하며 건너는 낮은 다리): 콘크리트 상판이 물가에선 수면 위 1.3m, 가운데로 갈수록 살짝 솟아 1m 더 높은 완만한 아치
        /// (사용자: "살짝 아치 형태면 좋겠다"). 교각 사이 밑면은 아치로 파였다.
        /// 난간은 빨간 강관 기둥 사이에 초록 와이어 네 가닥, 기둥 하나엔 주황 구명환. 상판 바깥 옆면엔 검은 전선관 두 줄 — 모두 상판 곡선을 따른다.
        /// 양쪽 끝은 둔치 산책로로 이어지는 짧은 경사. 밤엔 상판 옆 아래 LED가 물빛으로 켜진다.
        /// </summary>
        void Footbridge()
        {
            float x = KoreaCoasts.TcFootX;
            const float W = 3f, Thick = 0.45f, Rise = 1.0f;
            float zA = NearZ(x, 3.5f), zB = FarZ(x, 3.5f);
            float deckEnd = Water + 1.3f;
            // 상판 윗면 높이: 양 끝 deckEnd → 가운데 deckEnd + Rise (원호에 가까운 사인 곡선)
            float Deck(float z) => deckEnd + Rise * Mathf.Sin(Mathf.PI * Mathf.Clamp01((z - zA) / (zB - zA)));
            var conc = Mats.VertexAO(Mats.Weathered("concrete", new Color(0.72f, 0.72f, 0.68f), 2.5f, 0.12f, 1f, 0.55f, 0.25f, new Color(0.3f, 0.34f, 0.22f), 0.55f));
            var red = Mats.Weathered("rust", new Color(0.62f, 0.18f, 0.14f), 1.2f, 0.35f, 0f, 0.3f, 0f, null, 0.3f, 0.3f);
            var wire = Mats.Lit(new Color(0.22f, 0.62f, 0.42f), 0.5f, 0.3f);
            var pipe = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.4f);
            var led = HanRiverLandmarks.Mat(new Color(0.4f, 0.42f, 0.44f), null, Texture2D.whiteTexture, new Color(0.3f, 0.8f, 1f) * 2f, 0.3f, 0f, Vector2.one);
            var root = new GameObject("Footbridge").transform;
            root.SetParent(_root, false);
            var bm = new BlockMesh();
            var c = Color.white;
            var wet = new Color(0.5f, 0.52f, 0.46f);
            float hw = W * 0.5f;
            // 경간: 교각 사이 10~11m, 밑면은 교각 머리(수면 위 30cm)에서 경간 가운데 상판 밑까지 솟는 아치
            int spans = Mathf.Max(3, Mathf.RoundToInt((zB - zA) / 10.5f));
            float sl = (zB - zA) / spans;
            for (int k = 0; k < spans; k++)
            {
                float z0 = zA + k * sl;
                const int n = 12;
                for (int i = 0; i < n; i++)
                {
                    float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                    float za = z0 + t0 * sl, zb = z0 + t1 * sl;
                    float ta = Deck(za), tb = Deck(zb);
                    float ba = Mathf.Lerp(Water + 0.3f, ta - Thick, Mathf.Pow(Mathf.Sin(Mathf.PI * t0), 0.45f));
                    float bb = Mathf.Lerp(Water + 0.3f, tb - Thick, Mathf.Pow(Mathf.Sin(Mathf.PI * t1), 0.45f));
                    foreach (float sg in new[] { -1f, 1f })
                        bm.Face(new Vector3(x + sg * hw, ta, za), new Vector3(x + sg * hw, tb, zb), new Vector3(x + sg * hw, bb, zb), new Vector3(x + sg * hw, ba, za),
                            new Vector3(sg, 0f, 0f), c);
                    bm.Face(new Vector3(x - hw, ba, za), new Vector3(x + hw, ba, za), new Vector3(x + hw, bb, zb), new Vector3(x - hw, bb, zb), Vector3.down, wet);
                    // 윗면 (곡선을 따라 마디마다)
                    bm.Face(new Vector3(x - hw, ta, za), new Vector3(x + hw, ta, za), new Vector3(x + hw, tb, zb), new Vector3(x - hw, tb, zb), Vector3.up, c);
                }
                if (k > 0)
                {
                    float bed = Mathf.Min(H(x, z0), Water - 0.5f) - 0.5f;
                    bm.Block(new Vector3(x, (Water + 0.35f + bed) * 0.5f, z0), new Vector3(W + 0.6f, Water + 0.35f - bed, 1.1f), 0.25f, Quaternion.identity, c, wet);
                }
            }
            // 양 끝 경사 (둔치 산책로 높이로)
            foreach (var (zEnd, dir) in new[] { (zA, -1f), (zB, 1f) })
            {
                float zFar = zEnd + dir * 6.5f;
                float g = H(x, zFar) + 0.04f;
                var a = new Vector3(x, deckEnd, zEnd);
                var b = new Vector3(x, g, zFar);
                var d = b - a;
                bm.Block((a + b) * 0.5f + Vector3.down * 0.35f, new Vector3(W, 0.7f, d.magnitude + 0.1f), 0.03f, Quaternion.LookRotation(d.normalized), c, wet);
            }
            var go = bm.Emit(root, "FootBridge", conc);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            // 난간·전선관·LED: 1.2m 마디로 곡선을 따라 잇는다
            var bmRed = new BlockMesh();
            var bmWire = new BlockMesh();
            var bmPipe = new BlockMesh();
            var bmLed = new BlockMesh();
            var bmWall = new BlockMesh();
            const float step = 1.2f;
            foreach (float sg in new[] { -1f, 1f })
            {
                float px = x + sg * (hw - 0.12f);
                for (float z = zA; z <= zB + 0.01f; z += 2.4f)
                    bmRed.Block(new Vector3(px, Deck(z) + 0.6f, z), new Vector3(0.16f, 1.2f, 0.16f), 0.03f, Quaternion.identity, c, c);
                for (float z = zA; z < zB - 0.01f; z += step)
                {
                    float z1 = Mathf.Min(z + step, zB);
                    var a = new Vector3(0f, Deck(z), z);
                    var b = new Vector3(0f, Deck(z1), z1);
                    var mid = (a + b) * 0.5f;
                    var d = b - a;
                    var q = Quaternion.LookRotation(d.normalized);
                    float L = d.magnitude + 0.02f;
                    for (int w = 0; w < 4; w++)
                        bmWire.Block(new Vector3(px, mid.y + 0.3f + w * 0.25f, mid.z), new Vector3(0.025f, 0.025f, L), 0f, q, c, c);
                    for (int w = 0; w < 2; w++)
                        bmPipe.Block(new Vector3(x + sg * (hw + 0.05f), mid.y - 0.15f - w * 0.12f, mid.z), new Vector3(0.08f, 0.08f, L), 0.02f, q, c, c);
                    bmLed.Block(new Vector3(x + sg * (hw + 0.02f), mid.y - Thick + 0.04f, mid.z), new Vector3(0.03f, 0.05f, L), 0f, q, c, c);
                    bmWall.Block(new Vector3(x + sg * hw, mid.y + 0.9f, mid.z), new Vector3(0.1f, 1.8f, L), 0f, q, c, c);
                }
            }
            bmRed.Emit(root, "FootRailPost", red);
            bmWire.Emit(root, "FootRailWire", wire).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bmPipe.Emit(root, "FootPipe", pipe);
            bmLed.Emit(root, "FootLed", led).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 난간 옆으로 떨어지지 않게 (보이지 않는 벽, 곡선을 따라)
            var wallGo = bmWall.Emit(root, "FootRailCollider", wire);
            wallGo.AddComponent<MeshCollider>().sharedMesh = wallGo.GetComponent<MeshFilter>().sharedMesh;
            wallGo.GetComponent<Renderer>().enabled = false;
            float zr = zA + 2.4f * 3f;
            KenneyProps.Spawn("Props/harbor_lifering", root, new Vector3(x - hw - 0.12f, Deck(zr) + 0.55f, zr), 90f, 0.75f);
            _b.AddLamp(new Vector3(x, deckEnd + Rise + 4f, (zA + zB) * 0.5f), new Color(0.7f, 0.85f, 1f), 16f, 0.9f);
        }

        // ---------------------------------------------------------------- 차도교

        /// <summary>
        /// 차도교 (야탑교·사송교, 멀리 서현교·탄천교): 제방 위 도로 높이로 물길·둔치를 건너는 6차로 다리.
        /// 상판 + PSC 거더 여섯 줄, 물 흐름 방향으로 길쭉한 벽식 교각(둥근 끝) + 머리 보, 콘크리트 방호벽 + 위 철제 난간, 차선, 가로등.
        /// 밤엔 상판 옆 LED 띠가 보랏빛·푸른빛으로 켜지고 교각이 아래서 비춘다 (사용자 사진: 탄천 다리 야경).
        /// </summary>
        void CarBridge(float x, string name, bool detail)
        {
            const float W = 26f;
            float zA = NearRoadZ(x) + 7f, zB = FarRoadZ(x) - 7f;
            float top = CityY + 0.02f;
            var root = new GameObject("Bridge_" + name.Replace(" ", "")).transform;
            root.SetParent(_root, false);
            var concrete = Resources.Load<Texture2D>("Terrain/concrete_diff");
            var deckMat = Mats.Weathered("asphalt", new Color(0.3f, 0.31f, 0.32f), 5f, 0.12f, 0f, 0f, 0f, null, 0.6f);
            var conc = Mats.VertexAO(Mats.Weathered("concrete", new Color(0.8f, 0.79f, 0.76f), 2.5f, 0.15f, 1f, 0.45f, 0.15f, null, 0.5f));
            var pierLit = HanRiverLandmarks.Mat(new Color(0.78f, 0.77f, 0.74f), concrete, UplightTex(), new Color(1f, 0.85f, 0.65f) * 1.2f, 0.15f, 0f, Vector2.one);
            var led = HanRiverLandmarks.Mat(new Color(0.4f, 0.4f, 0.42f), null, Texture2D.whiteTexture, new Color(1f, 0.78f, 0.5f) * 2.2f, 0.3f, 0f, Vector2.one);
            var ledA = HanRiverLandmarks.Mat(new Color(0.45f, 0.42f, 0.5f), null, Texture2D.whiteTexture, new Color(0.7f, 0.3f, 1f) * 2.6f, 0.3f, 0f, Vector2.one);
            var ledB = HanRiverLandmarks.Mat(new Color(0.42f, 0.48f, 0.44f), null, Texture2D.whiteTexture, new Color(0.25f, 1f, 0.45f) * 2.4f, 0.3f, 0f, Vector2.one);
            var rail = Mats.Lit(new Color(0.55f, 0.58f, 0.6f), 0.5f, 0.5f);
            float len = zB - zA, zc = (zA + zB) * 0.5f;
            // 상판 + 보도 (양쪽 3m, 15cm 높게)
            Mats.Prim(PrimitiveType.Cube, "BridgeDeck", root, new Vector3(x, top - 0.25f, zc), new Vector3(W - 6f, 0.5f, len), deckMat);
            foreach (float s in new[] { -1f, 1f })
                Mats.Prim(PrimitiveType.Cube, "BridgeWalk", root, new Vector3(x + s * (W * 0.5f - 1.5f), top - 0.1f, zc), new Vector3(3f, 0.5f, len), conc);
            var bm = new BlockMesh();
            var c = Color.white;
            // 거더 여섯 줄 + 가로보
            for (int g = 0; g < 6; g++)
                bm.Block(new Vector3(x - W * 0.5f + 2.2f + g * (W - 4.4f) / 5f, top - 1.45f, zc), new Vector3(0.8f, 1.9f, len), 0.05f, Quaternion.identity, c, c * 0.6f);
            for (float z = zA + 2f; z < zB; z += 12f)
                bm.Block(new Vector3(x, top - 1.3f, z), new Vector3(W - 4f, 1.4f, 0.3f), 0.03f, Quaternion.identity, c, c * 0.6f);
            // 방호벽 (밖으로 살짝 기운 단면)
            foreach (float s in new[] { -1f, 1f })
            {
                bm.Block(new Vector3(x + s * (W * 0.5f - 0.2f), top + 0.45f, zc), new Vector3(0.4f, 1.0f, len), 0.06f, Quaternion.identity, c, c * 0.75f);
                bm.Block(new Vector3(x + s * (W * 0.5f + 0.05f), top - 0.55f, zc), new Vector3(0.3f, 1.3f, len), 0.04f, Quaternion.identity, c, c * 0.7f);
            }
            // 교각: 물·둔치에 50m 남짓마다, 흐름 방향(x)으로 길쭉한 벽 + 둥근 끝, 위에 머리 보
            var piers = new List<float>();
            for (float z = zA + 28f; z < zB - 20f; z += 34f) piers.Add(z);
            foreach (float pz in piers)
            {
                float ground = Mathf.Min(H(x, pz), Water) - 1.2f;
                float h = top - 2.4f - ground;
                bm.Block(new Vector3(x, top - 2.7f, pz), new Vector3(W - 3f, 0.8f, 2.4f), 0.06f, Quaternion.identity, c, c * 0.7f);
                var body = Mats.Prim(PrimitiveType.Cube, "Pier", root, new Vector3(x, ground + h * 0.5f, pz), new Vector3(9f, h, 1.8f), pierLit);
                foreach (float s in new[] { -1f, 1f })
                    Mats.Prim(PrimitiveType.Cylinder, "PierNose", root, new Vector3(x + s * 4.5f, ground + h * 0.5f, pz), new Vector3(1.8f, h * 0.5f, 1.8f), pierLit, false);
                if (detail && H(x, pz) < Water)
                {
                    // 수위표 (빨강·흰 눈금 판)
                    var gauge = Mats.Prim(PrimitiveType.Cube, "WaterGauge", root, new Vector3(x - 5.45f, Water + 1.2f, pz), new Vector3(0.05f, 2.6f, 0.35f), Mats.Lit(new Color(0.92f, 0.92f, 0.9f)), false);
                    for (int k = 0; k < 6; k++)
                        Mats.Prim(PrimitiveType.Cube, "GaugeMark", gauge.transform.parent, new Vector3(x - 5.48f, Water + 0.1f + k * 0.42f, pz), new Vector3(0.02f, 0.2f, 0.36f), Mats.Lit(new Color(0.8f, 0.15f, 0.12f)), false);
                }
            }
            var bcg = bm.Emit(root, "BridgeConcrete", conc);
            bcg.AddComponent<MeshCollider>().sharedMesh = bcg.GetComponent<MeshFilter>().sharedMesh;
            // 난간 (방호벽 위 철제), LED 띠 (상판 옆 아래 모서리)
            var bmRail = new BlockMesh();
            var bmLed = new BlockMesh();
            foreach (float s in new[] { -1f, 1f })
            {
                float rx = x + s * (W * 0.5f - 0.2f);
                bmRail.Block(new Vector3(rx, top + 1.3f, zc), new Vector3(0.08f, 0.08f, len), 0.01f, Quaternion.identity, c, c);
                for (float z = zA; z < zB; z += 2.5f)
                    bmRail.Block(new Vector3(rx, top + 1.1f, z), new Vector3(0.06f, 0.4f, 0.06f), 0f, Quaternion.identity, c, c);
                bmLed.Block(new Vector3(x + s * (W * 0.5f + 0.22f), top - 1.15f, zc), new Vector3(0.06f, 0.12f, len), 0f, Quaternion.identity, c, c);
            }
            bmRail.Emit(root, "BridgeRail", rail);
            // 난간 세로 LED 막대: 다리 반쪽은 보라, 반쪽은 초록 (사용자 야경 사진)
            var barA = new BlockMesh();
            var barB = new BlockMesh();
            foreach (float s in new[] { -1f, 1f })
                for (float z = zA + 1.25f; z < zB; z += 2.5f)
                    (z < zc ? barA : barB).Block(new Vector3(x + s * (W * 0.5f - 0.12f), top + 1.12f, z), new Vector3(0.07f, 0.42f, 0.07f), 0f, Quaternion.identity, c, c);
            foreach (var (bmb, m) in new[] { (barA, ledA), (barB, ledB) })
            {
                var bg = bmb.Emit(root, "BridgeLedBar", m);
                if (bg != null) bg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var lg = bmLed.Emit(root, "BridgeLed", led);
            if (lg != null) lg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 차선
            var white = Mats.Lit(new Color(0.88f, 0.88f, 0.85f), 0.3f);
            var yellow = Mats.Lit(new Color(0.9f, 0.68f, 0.12f), 0.3f);
            foreach (float o in new[] { -0.12f, 0.12f })
                Mats.Prim(PrimitiveType.Cube, "BridgeYellow", root, new Vector3(x + o, top + 0.005f, zc), new Vector3(0.12f, 0.01f, len), yellow, false);
            foreach (float o in new[] { -6.4f, -3.2f, 3.2f, 6.4f })
                for (float z = zA + 1f; z < zB - 3f; z += 8f)
                    Mats.Prim(PrimitiveType.Cube, "BridgeLane", root, new Vector3(x + o, top + 0.005f, z + 1.5f), new Vector3(0.13f, 0.01f, 3f), white, false);
            if (!detail) return;
            // 가로등 (양쪽 보도, 엇갈려)
            for (float z = zA + 10f; z < zB; z += 26f)
            {
                _b.Streetlight(new Vector3(x - W * 0.5f + 0.9f, top + 0.15f, z), 270f);
                _b.Streetlight(new Vector3(x + W * 0.5f - 0.9f, top + 0.15f, z + 13f), 90f);
            }
            // 다리 이름 판 (방호벽 바깥, 양쪽 강 위)
            foreach (float s in new[] { -1f, 1f })
            {
                var t = Fishing.UI.UIFactory.WorldText(root, name, new Vector3(x + s * (W * 0.5f + 0.24f), top - 0.6f, (zA + zB) * 0.5f - 6f), new Color(0.2f, 0.22f, 0.25f), 9f, "BridgeName");
                t.transform.localRotation = Quaternion.Euler(0f, s > 0f ? -90f : 90f, 0f);
                Fishing.UI.UIFactory.OneSided(t);
            }
            // 교각 투광등 (밤에 교각을 아래서 비춘다)
            foreach (float pz in piers)
                if (H(x, pz) < Water) _b.AddLamp(new Vector3(x - 7f, Water + 0.6f, pz), new Color(1f, 0.78f, 0.55f), 14f, 1.4f);
        }

        static Texture2D _uplight;

        /// <summary>올려 비추는 투광등 빛 (밤 빛 맵): 아래가 밝고 위로 어두워진다 (한강 동작대교와 같은 식).</summary>
        static Texture2D UplightTex()
        {
            if (_uplight != null) return _uplight;
            _uplight = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
            {
                float k = Mathf.Lerp(1f, 0.1f, Mathf.Pow(y / 63f, 0.7f));
                for (int x = 0; x < 4; x++) _uplight.SetPixel(x, y, new Color(k, k, k, 1f));
            }
            _uplight.Apply();
            return _uplight;
        }

        // ---------------------------------------------------------------- 탄천종합운동장

        static Mesh Grid(int nu, int nv, System.Func<float, float, Vector3> P, System.Func<float, float, Vector2> uv, bool flip = false)
        {
            var v = new List<Vector3>();
            var u2 = new List<Vector2>();
            var t = new List<int>();
            for (int j = 0; j <= nv; j++)
            for (int i = 0; i <= nu; i++)
            {
                float u = i / (float)nu, w = j / (float)nv;
                v.Add(P(u, w));
                u2.Add(uv(u, w));
            }
            int row = nu + 1;
            for (int j = 0; j < nv; j++)
            for (int i = 0; i < nu; i++)
            {
                int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                if (flip) t.AddRange(new[] { a, b, c, b, d, c });
                else t.AddRange(new[] { a, c, b, b, c, d });
            }
            var m = new Mesh { name = "Grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, u2);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>둥근 네모(초타원, 지수 p) 둘레 점: 반지름 (a, b), 각 2πu.</summary>
        static Vector2 Oval(float u, float a, float b, float p = 2.8f)
        {
            float th = u * Mathf.PI * 2f;
            float c = Mathf.Cos(th), s = Mathf.Sin(th);
            return new Vector2(Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / p) * a, Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / p) * b);
        }

        static Texture2D _seats, _track, _pitch;

        /// <summary>관람석: 가로 = 둘레 한 바퀴(구역마다 파랑·하늘·초록·주황 의자), 세로 = 한 줄(의자 등받이 + 앉는 판 + 회색 계단).</summary>
        static Texture2D SeatTexture()
        {
            if (_seats != null) return _seats;
            const int Wd = 1024, Ht = 32;
            var cols = new[] { new Color(0.16f, 0.36f, 0.7f), new Color(0.3f, 0.6f, 0.82f), new Color(0.2f, 0.56f, 0.36f), new Color(0.9f, 0.5f, 0.18f), new Color(0.16f, 0.36f, 0.7f), new Color(0.86f, 0.2f, 0.2f) };
            _seats = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "seats" };
            var px = new Color[Wd * Ht];
            for (int x = 0; x < Wd; x++)
            {
                var sec = cols[(x * 18 / Wd) % cols.Length];
                bool aisle = (x % (Wd / 18)) < 3;
                for (int y = 0; y < Ht; y++)
                {
                    bool seat = y > 10 && y < 26 && (x % 6) != 0;
                    var c = aisle || !seat ? new Color(0.62f, 0.62f, 0.6f) * (y < 10 ? 0.85f : 1f) : sec * (y > 20 ? 1.1f : 0.85f);
                    px[y * Wd + x] = c;
                }
            }
            _seats.SetPixels(px);
            _seats.Apply(true);
            return _seats;
        }

        /// <summary>트랙: 가로 = 둘레, 세로 = 레인 8개를 가르는 흰 선이 있는 파란 우레탄 (사용자 위성사진).</summary>
        static Texture2D TrackTexture()
        {
            if (_track != null) return _track;
            const int Wd = 16, Ht = 256;
            _track = new Texture2D(Wd, Ht, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "track" };
            for (int y = 0; y < Ht; y++)
            {
                bool line = (y % 32) < 2 || y > Ht - 3;
                for (int x = 0; x < Wd; x++)
                    _track.SetPixel(x, y, line ? new Color(0.95f, 0.95f, 0.92f) : new Color(0.2f, 0.42f, 0.72f) * (0.95f + 0.05f * Mathf.PerlinNoise(x * 0.7f, y * 0.3f)));
            }
            _track.Apply(true);
            return _track;
        }

        /// <summary>잔디 구장: 깎은 줄무늬 (짙고 옅은 초록이 번갈아).</summary>
        static Texture2D PitchTexture()
        {
            if (_pitch != null) return _pitch;
            const int N = 128;
            _pitch = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "pitch" };
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool band = (x / 16) % 2 == 0;
                float n = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.08f;
                _pitch.SetPixel(x, y, (band ? new Color(0.28f, 0.52f, 0.22f) : new Color(0.34f, 0.6f, 0.26f)) * (0.96f + n));
            }
            _pitch.Apply(true);
            return _pitch;
        }

        static Material TexMat(Texture2D tex, Vector2 tiling, float smooth = 0.15f)
        {
            var m = new Material(Shader.Find("Standard")) { color = Color.white, mainTexture = tex };
            m.mainTextureScale = tiling;
            m.SetFloat("_Glossiness", smooth);
            return m;
        }

        GameObject Put(string name, Mesh mesh, Material mat, Transform parent, bool collider = false, bool shadow = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            if (!shadow) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>
        /// 탄천종합운동장: 둥근 네모 그릇 관람석(구역별 색 의자), 바깥 기둥 벽, 강 반대쪽 본부석 위 흰 지붕막(뒤 기둥에서 매단 듯),
        /// 안쪽 붉은 8레인 트랙과 줄무늬 잔디 축구장(흰 선), 네 귀퉁이 조명탑(밤에 켜짐), 강 쪽 벽에 이름.
        /// </summary>
        void Stadium(Vector3 c)
        {
            var root = new GameObject("TancheonStadium").transform;
            root.SetParent(_root, false);
            root.localPosition = c;
            const float fa = 62f, fb = 40f;       // 잔디 반지름 (x, z)
            const float ta = 74f, tb = 50f;       // 트랙 바깥
            const float sa0 = 76f, sb0 = 52f, sa1 = 96f, sb1 = 68f, sh0 = 1.6f, sh1 = 17f;
            // 잔디·트랙
            Put("Pitch", Grid(64, 8, (u, w) => { var p = Oval(u, fa * w, fb * w); return new Vector3(p.x, 0.06f, p.y); }, (u, w) => { var p = Oval(u, fa * w, fb * w); return p / 9f; }, true),
                TexMat(PitchTexture(), Vector2.one), root, true);
            Put("Track", Grid(96, 2, (u, w) => { var p = Oval(u, Mathf.Lerp(fa, ta, w), Mathf.Lerp(fb, tb, w)); return new Vector3(p.x, 0.07f, p.y); }, (u, w) => new Vector2(u * 60f, w), true),
                TexMat(TrackTexture(), Vector2.one), root, true);
            // 축구장 선 (105 × 68)
            var white = Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.2f);
            var lines = new BlockMesh();
            var lc = Color.white;
            void L(float x0, float z0, float x1, float z1) =>
                lines.Block(new Vector3((x0 + x1) * 0.5f, 0.09f, (z0 + z1) * 0.5f), new Vector3(Mathf.Max(0.12f, Mathf.Abs(x1 - x0)), 0.01f, Mathf.Max(0.12f, Mathf.Abs(z1 - z0))), 0f, Quaternion.identity, lc, lc);
            L(-52.5f, -34f, 52.5f, -34f); L(-52.5f, 34f, 52.5f, 34f); L(-52.5f, -34f, -52.5f, 34f); L(52.5f, -34f, 52.5f, 34f); L(0f, -34f, 0f, 34f);
            foreach (float s in new[] { -1f, 1f })
            {
                L(s * 52.5f, -20f, s * 36f, -20f); L(s * 52.5f, 20f, s * 36f, 20f); L(s * 36f, -20f, s * 36f, 20f);
                L(s * 52.5f, -9f, s * 47f, -9f); L(s * 52.5f, 9f, s * 47f, 9f); L(s * 47f, -9f, s * 47f, 9f);
            }
            for (int k = 0; k < 24; k++)
            {
                float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f;
                L(Mathf.Cos(a0) * 9.15f, Mathf.Sin(a0) * 9.15f, Mathf.Cos(a1) * 9.15f, Mathf.Sin(a1) * 9.15f);
            }
            var lg = lines.Emit(root, "PitchLines", white);
            lg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 골대
            var bmGoal = new BlockMesh();
            foreach (float s in new[] { -1f, 1f })
            {
                foreach (float gz in new[] { -3.66f, 3.66f })
                    bmGoal.Block(new Vector3(s * 52.5f, 1.22f, gz), new Vector3(0.12f, 2.44f, 0.12f), 0f, Quaternion.identity, lc, lc);
                bmGoal.Block(new Vector3(s * 52.5f, 2.44f, 0f), new Vector3(0.12f, 0.12f, 7.44f), 0f, Quaternion.identity, lc, lc);
            }
            bmGoal.Emit(root, "Goals", white);
            // 관람석 그릇 (안쪽 담 + 계단식 의자면)
            Put("Stands", Grid(160, 1, (u, w) => { var p = Oval(u, Mathf.Lerp(sa0, sa1, w), Mathf.Lerp(sb0, sb1, w)); return new Vector3(p.x, Mathf.Lerp(sh0, sh1, w), p.y); },
                    (u, w) => new Vector2(u, w * 26f), true), TexMat(SeatTexture(), Vector2.one, 0.25f), root, true);
            var concMat = Mats.VertexAO(Mats.Weathered("wallconcrete", new Color(0.86f, 0.85f, 0.82f), 3f, 0.15f, 0f, 0.4f, 0.05f, null, 0.5f));
            Put("InnerWall", Grid(160, 1, (u, w) => { var p = Oval(u, sa0, sb0); return new Vector3(p.x, Mathf.Lerp(0.05f, sh0, w), p.y); },
                    (u, w) => new Vector2(u * 40f, w), true), concMat, root, true);
            // 바깥 벽 (그릇 뒤 끝 → 땅) + 기둥
            Put("OuterWall", Grid(160, 1, (u, w) => { var p = Oval(u, sa1 + 0.3f, sb1 + 0.3f); return new Vector3(p.x, Mathf.Lerp(sh1 + 1.2f, 0f, w), p.y); },
                    (u, w) => new Vector2(u * 60f, w * 4f), true), concMat, root, true);
            var bmCol = new BlockMesh();
            for (int k = 0; k < 64; k++)
            {
                float u = k / 64f;
                var p = Oval(u, sa1 + 1.1f, sb1 + 1.1f);
                var p2 = Oval(u + 0.002f, sa1 + 1.1f, sb1 + 1.1f);
                var tan = new Vector3(p2.x - p.x, 0f, p2.y - p.y).normalized;
                bmCol.Block(new Vector3(p.x, (sh1 + 1.6f) * 0.5f, p.y), new Vector3(1.0f, sh1 + 1.6f, 1.5f), 0.06f, Quaternion.LookRotation(tan), lc, lc * 0.7f);
            }
            // 바깥 테 (그릇 위 끝 보)
            for (int k = 0; k < 128; k++)
            {
                var p = Oval(k / 128f, sa1 + 0.6f, sb1 + 0.6f);
                var p2 = Oval((k + 1) / 128f, sa1 + 0.6f, sb1 + 0.6f);
                var d = new Vector3(p2.x - p.x, 0f, p2.y - p.y);
                bmCol.Block(new Vector3((p.x + p2.x) * 0.5f, sh1 + 1.4f, (p.y + p2.y) * 0.5f), new Vector3(1.6f, 0.9f, d.magnitude + 0.05f), 0.04f, Quaternion.LookRotation(d.normalized), lc, lc);
            }
            bmCol.Emit(root, "StadiumColumns", concMat);
            // 지붕막 (사용자 위성사진: 흰 막 지붕이 관람석을 빙 둘러 덮고 잔디·트랙만 열려 있다): 바깥 테에서 안으로 솟으며 뻗는 고리,
            // 막을 당긴 골(물결)이 둘레를 따라 진다. 위아래 두 장 (아래서 올려다봐도 보이게)
            var roofMat = Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0.4f);
            System.Func<float, float, Vector3> roofP = (u, w) =>
            {
                var p = Oval(u, Mathf.Lerp(sa1 + 1f, sa0 + 3f, w), Mathf.Lerp(sb1 + 1f, sb0 + 3f, w));
                float rib = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 36f));
                return new Vector3(p.x, sh1 + 4.5f + 4f * w - 0.9f * (1f - rib) * Mathf.Sin(w * Mathf.PI), p.y);
            };
            Put("RoofTop", Grid(288, 4, roofP, (u, w) => new Vector2(u, w)), roofMat, root, false);
            Put("RoofBottom", Grid(288, 4, roofP, (u, w) => new Vector2(u, w), true), roofMat, root, false, false);
            // 막을 받치는 갈비 보: 바깥 기둥 머리 → 안쪽 끝 (36개)
            var steel = Mats.Lit(new Color(0.85f, 0.86f, 0.87f), 0.5f, 0.6f);
            for (int k = 0; k < 36; k++)
            {
                float uu = k / 36f;
                var p0 = roofP(uu, 0f);
                var p1 = roofP(uu, 1f);
                MapKit.Beam(root, new Vector3(p0.x, sh1 + 1.6f, p0.z), p0 + Vector3.down * 0.3f, 0.6f, steel);
                MapKit.Beam(root, p0 + Vector3.down * 0.35f, p1 + Vector3.down * 0.35f, 0.4f, steel);
            }
            // 서쪽(강 반대쪽) 본부석 뒤 흰 지붕 건물 (위성사진의 큰 흰 직사각형)
            var annexWall = Mats.Wall(new Color(0.9f, 0.89f, 0.86f));
            Mats.Prim(PrimitiveType.Cube, "StadiumAnnex", root, new Vector3(-6f, 8f, sb1 + 13f), new Vector3(96f, 16f, 22f), annexWall);
            Mats.Prim(PrimitiveType.Cube, "StadiumAnnexRoof", root, new Vector3(-6f, 16.4f, sb1 + 13f), new Vector3(98f, 0.8f, 24f), roofMat, false);
            // 조명탑 넷 (밤에 켜짐)
            var lampHead = Mats.Lit(new Color(0.8f, 0.8f, 0.78f), 0.3f);
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var p = new Vector3(sx * (sa1 - 6f), 0f, sz * (sb1 - 4f));
                MapKit.Beam(root, p, p + Vector3.up * 42f, 1.1f, steel);
                var head = Mats.Prim(PrimitiveType.Cube, "FloodLight", root, p + Vector3.up * 43f, new Vector3(7f, 4f, 0.8f), lampHead, false,
                    Quaternion.LookRotation(new Vector3(-p.x, -20f, -p.z).normalized));
                _b.AddBulb(head.GetComponent<Renderer>());
                _b.AddLamp(root.TransformPoint(p * 0.8f + Vector3.up * 38f), new Color(0.95f, 0.97f, 1f), 70f, 1.1f);
            }
            // 강 쪽 벽 이름
            var sign = Fishing.UI.UIFactory.WorldText(root, "탄천종합운동장", new Vector3(-20f, sh1 - 3f, -(sb1 + 2.1f)), new Color(0.15f, 0.3f, 0.55f), 40f, "StadiumName");
            Fishing.UI.UIFactory.OneSided(sign);
            // 광장 (강 쪽 앞) + 나무
            var plaza = Mats.Weathered("greytile", new Color(0.78f, 0.74f, 0.68f), 2f, 0.1f, 0f, 0f, 0.05f, null, 0.4f);
            Mats.Prim(PrimitiveType.Cube, "StadiumPlaza", root, new Vector3(0f, 0.02f, -(sb1 + 10f)), new Vector3(150f, 0.1f, 14f), plaza);
            for (float x = -70f; x <= 70f; x += 14f)
                KenneyProps.Spawn("Kenney/Nature/tree_default", root, new Vector3(x, 0.05f, -(sb1 + 12f)), R(0f, 360f), R(7f, 9f));
        }

        /// <summary>탄천 야구장: 부채꼴 외야 잔디·흙 내야·다이아몬드, 초록 외야 펜스, 홈 뒤 그물과 작은 관람석, 조명탑 여섯.</summary>
        void Ballpark(Vector3 home)
        {
            var root = new GameObject("Ballpark").transform;
            root.SetParent(_root, false);
            root.localPosition = home;
            root.localRotation = Quaternion.Euler(0f, 45f, 0f);   // 외야가 +z(강 반대쪽)·+x(하류) 사이를 본다
            const float R0 = 92f;
            Put("Outfield", Grid(32, 8, (u, w) => { float a = Mathf.Lerp(-45f, 45f, u) * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a) * R0 * w, 0.06f, Mathf.Cos(a) * R0 * w); },
                (u, w) => { float a = Mathf.Lerp(-45f, 45f, u) * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * R0 * w / 9f; }),
                TexMat(PitchTexture(), Vector2.one), root, true);
            var dirt = Mats.Textured("sand", new Color(0.78f, 0.55f, 0.38f), 4f, 0.05f);
            Put("Infield", Grid(24, 4, (u, w) => { float a = Mathf.Lerp(-45f, 45f, u) * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a) * 44f * w, 0.08f, Mathf.Cos(a) * 44f * w); },
                (u, w) => new Vector2(u, w)), dirt, root, false);
            // 내야 잔디 (다이아몬드 안)
            Mats.Prim(PrimitiveType.Cube, "InfieldGrass", root, new Vector3(0f, 0.09f, 19.4f), new Vector3(23f, 0.02f, 23f), TexMat(PitchTexture(), new Vector2(2f, 2f)), false, Quaternion.Euler(0f, 45f, 0f));
            var white = Mats.Lit(new Color(0.95f, 0.95f, 0.92f), 0.2f);
            foreach (var p in new[] { new Vector3(0f, 0.12f, 0f), new Vector3(19.4f, 0.12f, 19.4f), new Vector3(0f, 0.12f, 38.8f), new Vector3(-19.4f, 0.12f, 19.4f) })
                Mats.Prim(PrimitiveType.Cube, "Base", root, p, new Vector3(0.45f, 0.06f, 0.45f), white, false, Quaternion.Euler(0f, 45f, 0f));
            foreach (float s in new[] { -1f, 1f })
                Mats.Prim(PrimitiveType.Cube, "FoulLine", root, RotY(new Vector3(0f, 0.1f, R0 * 0.5f), s * 45f), new Vector3(0.1f, 0.01f, R0), white, false, Quaternion.Euler(0f, s * 45f, 0f));
            // 외야 펜스
            var fence = Mats.Lit(new Color(0.12f, 0.36f, 0.24f), 0.2f);
            var bm = new BlockMesh();
            var c = Color.white;
            for (int k = 0; k < 24; k++)
            {
                float a0 = Mathf.Lerp(-45f, 45f, k / 24f) * Mathf.Deg2Rad, a1 = Mathf.Lerp(-45f, 45f, (k + 1) / 24f) * Mathf.Deg2Rad;
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * R0;
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * R0;
                var d = p1 - p0;
                bm.Block((p0 + p1) * 0.5f + Vector3.up * 1.6f, new Vector3(0.3f, 3.2f, d.magnitude + 0.05f), 0.02f, Quaternion.LookRotation(d.normalized), c, c * 0.8f);
            }
            bm.Emit(root, "OutfieldFence", fence).AddComponent<BoxCollider>();
            // 홈 뒤 그물 기둥 + 관람석 계단
            var steel = Mats.Lit(new Color(0.5f, 0.52f, 0.55f), 0.4f, 0.4f);
            var net = Mats.Lit(new Color(0.3f, 0.32f, 0.3f), 0.1f);
            for (int k = -3; k <= 3; k++)
            {
                var p = RotY(new Vector3(0f, 0f, -16f), k * 18f);
                MapKit.Beam(root, p, p + Vector3.up * 12f, 0.3f, steel);
            }
            var stands = new BlockMesh();
            for (int r = 0; r < 6; r++)
                for (int k = -3; k < 3; k++)
                {
                    var p = RotY(new Vector3(0f, 0.4f + r * 0.5f, -18f - r * 0.9f), (k + 0.5f) * 18f);
                    stands.Block(p, new Vector3(10f, 0.8f + r * 0.0f, 0.9f), 0.02f, Quaternion.Euler(0f, (k + 0.5f) * 18f, 0f), c, c * 0.7f);
                }
            stands.Emit(root, "BallparkStands", Mats.Lit(new Color(0.2f, 0.4f, 0.72f), 0.3f));
            var netPanel = Mats.Prim(PrimitiveType.Cube, "BackNet", root, new Vector3(0f, 6f, -16.2f), new Vector3(34f, 12f, 0.02f), net, false);
            netPanel.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 조명탑 여섯
            var head = Mats.Lit(new Color(0.8f, 0.8f, 0.78f), 0.3f);
            foreach (var (ang, rr) in new[] { (-60f, 30f), (60f, 30f), (-40f, 98f), (40f, 98f), (-15f, 100f), (15f, 100f) })
            {
                var p = RotY(new Vector3(0f, 0f, rr), ang);
                MapKit.Beam(root, p, p + Vector3.up * 30f, 0.6f, steel);
                var h = Mats.Prim(PrimitiveType.Cube, "BallLight", root, p + Vector3.up * 31f, new Vector3(4f, 2.4f, 0.6f), head, false, Quaternion.LookRotation(new Vector3(-p.x, -12f, -p.z + 40f).normalized));
                _b.AddBulb(h.GetComponent<Renderer>());
            }
        }

        static Vector3 RotY(Vector3 p, float deg) => Quaternion.Euler(0f, deg, 0f) * p;

        /// <summary>탄천체육관: 낮은 아치 지붕 상자 (은빛 금속 지붕, 유리 띠 창).</summary>
        void Gymnasium(Vector3 c)
        {
            var root = new GameObject("Gymnasium").transform;
            root.SetParent(_root, false);
            root.localPosition = c;
            var wall = Mats.Wall(new Color(0.88f, 0.86f, 0.82f));
            var glass = HanRiverLandmarks.Mat(Color.white, HanRiverLandmarks.Facade(2, 10, new Color(0.55f, 0.68f, 0.76f), new Color(0.3f, 0.38f, 0.45f), new Color(0.85f, 0.86f, 0.86f), 0.3f, 0.1f, 61, out var night, 0.5f),
                night, new Color(1f, 0.92f, 0.75f), 0.6f, 0.2f, new Vector2(4f, 1f));
            const float Wd = 46f, Dp = 60f, Ht = 12f;
            Mats.Prim(PrimitiveType.Cube, "GymBody", root, new Vector3(0f, Ht * 0.5f, 0f), new Vector3(Wd, Ht, Dp), wall);
            Mats.Prim(PrimitiveType.Cube, "GymGlass", root, new Vector3(0f, 4.5f, 0f), new Vector3(Wd + 0.1f, 4f, Dp + 0.1f), glass, false);
            var roof = Mats.Lit(new Color(0.72f, 0.75f, 0.78f), 0.55f, 0.6f);
            Put("GymRoof", Grid(24, 1, (u, w) => { float a = Mathf.Lerp(-1f, 1f, u); return new Vector3(a * (Wd * 0.5f + 1.5f), Ht + 7f * (1f - a * a), Mathf.Lerp(-Dp * 0.5f - 1.5f, Dp * 0.5f + 1.5f, w)); },
                (u, w) => new Vector2(u, w)), roof, root, false);
            var sign = Fishing.UI.UIFactory.WorldText(root, "탄천체육관", new Vector3(0f, Ht - 2f, -Dp * 0.5f - 0.1f), new Color(0.2f, 0.25f, 0.3f), 22f, "GymName");
            Fishing.UI.UIFactory.OneSided(sign);
        }

        // ---------------------------------------------------------------- 시가지

        Material[] _aptMats, _towerMats, _officeMats;
        Material _endWall, _roofMat;

        void CityMats()
        {
            int nt = HanRiverLandmarks.NightTiles;
            var warm = new Color(1f, 0.85f, 0.6f) * 0.8f;
            Material M(Texture2D tex, Texture2D night, Color tint, Color glow, float smooth = 0.2f) => HanRiverLandmarks.Mat(tint, tex, night, glow, smooth, 0f, Vector2.one, nt);
            var a1 = HanRiverLandmarks.Facade(5, 8, new Color(0.56f, 0.62f, 0.66f), new Color(0.36f, 0.41f, 0.45f), new Color(0.95f, 0.94f, 0.92f), 0.3f, 0.14f, 611, out var n1, 0.4f, nt);
            var a2 = HanRiverLandmarks.Facade(5, 8, new Color(0.6f, 0.62f, 0.6f), new Color(0.38f, 0.4f, 0.4f), new Color(0.93f, 0.9f, 0.84f), 0.32f, 0.14f, 612, out var n2, 0.36f, nt);
            _aptMats = new[] { M(a1, n1, Color.white, warm), M(a2, n2, Color.white, warm), M(a1, n1, new Color(0.96f, 0.93f, 0.88f), warm) };
            var t1 = HanRiverLandmarks.Facade(5, 8, new Color(0.62f, 0.78f, 0.76f), new Color(0.38f, 0.52f, 0.52f), new Color(0.93f, 0.95f, 0.94f), 0.14f, 0.1f, 613, out var n3, 0.42f, nt);
            var t2 = HanRiverLandmarks.Facade(5, 8, new Color(0.7f, 0.76f, 0.82f), new Color(0.42f, 0.48f, 0.56f), new Color(0.92f, 0.93f, 0.95f), 0.14f, 0.1f, 614, out var n4, 0.42f, nt);
            _towerMats = new[] { M(t1, n3, Color.white, warm, 0.55f), M(t2, n4, Color.white, warm, 0.55f) };
            var o1 = HanRiverLandmarks.Facade(5, 8, new Color(0.6f, 0.68f, 0.76f), new Color(0.3f, 0.36f, 0.42f), new Color(0.72f, 0.74f, 0.76f), 0.15f, 0.08f, 615, out var n5, 0.45f, nt);
            var o2 = HanRiverLandmarks.Facade(5, 8, new Color(0.82f, 0.78f, 0.7f), new Color(0.5f, 0.5f, 0.48f), new Color(0.9f, 0.86f, 0.78f), 0.3f, 0.1f, 616, out var n6, 0.45f, nt);
            var cool = new Color(0.9f, 0.95f, 1f) * 0.8f;
            _officeMats = new[] { M(o1, n5, Color.white, cool, 0.65f), M(o2, n6, Color.white, cool, 0.3f) };
            _endWall = Mats.Wall(new Color(0.93f, 0.92f, 0.89f));
            _roofMat = Mats.Lit(new Color(0.78f, 0.78f, 0.76f), 0.2f);
        }

        GameObject Box(string name, Vector3 basePos, Vector3 size, float yaw, Material mat, int variant, bool collider, bool shadow)
        {
            var go = Mats.Prim(PrimitiveType.Cube, name, _root, basePos + Vector3.up * size.y * 0.5f, size, mat, collider, Quaternion.Euler(0f, yaw, 0f));
            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = HanRiverLandmarks.TiledCube(mf.sharedMesh, size.x, size.y, size.z, variant);
            if (!shadow) go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>
        /// 판상형 아파트 한 동: 창 띠 벽(밤엔 띄엄띄엄 불빛), 창 없는 양 끝 벽(가까운 동은 큰 동 번호와 브랜드 색 띠), 옥상 기계실·물탱크실.
        /// </summary>
        void Apartment(Vector3 at, float len, int floors, float yaw, int seed, bool detail, string number = null)
        {
            const float depth = 13f;
            float h = floors * 2.8f + 1.4f;
            Box("Apt", at, new Vector3(len, h, depth), yaw, _aptMats[seed % _aptMats.Length], seed, detail, detail);
            var q = Quaternion.Euler(0f, yaw, 0f);
            var crown = Mats.Prim(PrimitiveType.Cube, "AptCrown", _root, at + Vector3.up * (h + 2.2f), new Vector3(len * 0.22f, 4.4f, depth * 0.7f), _endWall, false, q);
            if (!detail) { crown.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return; }
            var brand = new[] { new Color(0.1f, 0.42f, 0.42f), new Color(0.45f, 0.28f, 0.16f), new Color(0.15f, 0.3f, 0.6f), new Color(0.6f, 0.15f, 0.2f) }[seed % 4];
            var brandMat = Mats.Lit(brand, 0.3f);
            foreach (float s in new[] { -1f, 1f })
            {
                // 끝 벽 (창을 덮는 민 벽) + 꼭대기 브랜드 띠
                var wallPos = at + q * new Vector3(s * (len * 0.5f + 0.12f), h * 0.5f, 0f);
                Mats.Prim(PrimitiveType.Cube, "AptEnd", _root, wallPos, new Vector3(0.24f, h, depth + 0.1f), _endWall, false, q);
                Mats.Prim(PrimitiveType.Cube, "AptBrand", _root, at + q * new Vector3(s * (len * 0.5f + 0.26f), h - 4f, 0f), new Vector3(0.05f, 5f, depth * 0.55f), brandMat, false, q);
                if (number == null) continue;
                var t = Fishing.UI.UIFactory.WorldText(_root, number, at + q * new Vector3(s * (len * 0.5f + 0.27f), h - 12f, 0f), new Color(0.25f, 0.27f, 0.3f), 60f, "AptNumber");
                t.transform.localRotation = q * Quaternion.Euler(0f, s > 0f ? -90f : 90f, 0f);
                Fishing.UI.UIFactory.OneSided(t);
            }
        }

        /// <summary>탑상형 고층 아파트 (사용자 사진: 탄천 옆 30~40층 청록 유리 탑): 십자 평면(긴 상자 둘), 위 두 층은 물러난 왕관 + 꼭대기 테.</summary>
        void Tower(Vector3 at, int floors, float yaw, int seed, bool shadow)
        {
            float h = floors * 2.9f;
            var m = _towerMats[seed % _towerMats.Length];
            Box("Tower", at, new Vector3(30f, h, 14f), yaw, m, seed, shadow, shadow);
            Box("Tower", at, new Vector3(14f, h - 3f, 26f), yaw, m, seed + 3, false, shadow);
            var crown = Mats.Prim(PrimitiveType.Cube, "TowerCrown", _root, at + Vector3.up * (h + 3f), new Vector3(20f, 6f, 10f), _endWall, false, Quaternion.Euler(0f, yaw, 0f));
            crown.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var frame = Mats.Prim(PrimitiveType.Cube, "TowerFrame", _root, at + Vector3.up * (h + 7f), new Vector3(24f, 1.2f, 16f), _endWall, false, Quaternion.Euler(0f, yaw, 0f));
            frame.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// 양쪽 시가지. 가까운 쪽(시작 둔치 뒤, 실제 야탑 시내): 도로변 업무 빌딩·병원, 그 뒤 판상형 아파트 단지.
        /// 맞은편(운동장 뒤): 아파트 단지. 하류 양쪽: 탑상형 고층 아파트 무리. 지형 밖은 그림자 없는 먼 상자.
        /// </summary>
        void City()
        {
            CityMats();
            int seed = 0;
            // 가까운 쪽 도로변 빌딩 (도로 뒤 7m부터)
            for (float x = -200f; x < 200f; x += R(36f, 50f))
            {
                if (Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 22f) continue;
                float w = R(22f, 34f), d = R(16f, 24f), h = R(18f, 52f);
                float z = NearRoadZ(x) - 13f - d * 0.5f;
                Box("Office", new Vector3(x, CityY, z), new Vector3(w, h, d), 0f, _officeMats[seed % 2], seed++, true, true);
                Mats.Prim(PrimitiveType.Cube, "OfficeRoof", _root, new Vector3(x, CityY + h + 1.2f, z), new Vector3(w * 0.4f, 2.4f, d * 0.4f), _roofMat, false);
            }
            // 가까운 쪽 아파트 단지 (지형 안: 그림자·콜라이더·동 번호)
            int no = 101;
            for (float z = -160f; z > -195f; z -= 34f)
                for (float x = -190f; x < 200f; x += R(70f, 85f))
                {
                    if (Mathf.Abs(Mathf.Abs(x) - KoreaCoasts.TcBridgeX) < 40f) continue;
                    Apartment(new Vector3(x, CityY, z), R(48f, 62f), _rng.Next(15, 25), R(-4f, 4f), seed++, true, (no++).ToString());
                }
            // 맞은편 운동장 뒤 아파트 (지형 끝 언저리)
            no = 301;
            for (float x = -200f; x < 210f; x += R(68f, 82f))
                Apartment(new Vector3(x, CityY, R(256f, 262f)), R(48f, 60f), _rng.Next(15, 23), R(-5f, 5f), seed++, true, (no++).ToString());
            // 하류 탑상형 무리 (사용자 사진의 고층 탑)
            foreach (var (x, z) in new[] { (250f, -150f), (300f, -170f), (360f, -150f), (330f, -230f), (420f, -190f), (270f, 140f), (330f, 170f), (400f, 140f), (-260f, -160f), (-330f, -190f) })
                Tower(new Vector3(x, CityY, z), _rng.Next(30, 44), R(-10f, 10f), seed++, Mathf.Abs(x) < 300f);
            // 먼 시가지 (지형 밖, 그림자·콜라이더 없음)
            int placed = 0;
            for (int i = 0; i < 900 && placed < 260; i++)
            {
                float x = R(-1150f, 1150f);
                bool near = _rng.NextDouble() < 0.5;
                float z = near ? R(-1100f, -205f) : R(280f, 1100f);
                if (Mathf.Abs(x) < 215f && z > -215f && z < 280f) continue;
                if (Mathf.Abs(x) < 470f && z > 100f && z < 230f && x > 230f) continue;   // 하류 탑 무리 자리
                if (new Vector2(x - 1060f, z - 140f).magnitude < 120f) continue;         // 롯데월드타워
                float far = Mathf.InverseLerp(200f, 1100f, Mathf.Abs(z));
                bool apt = _rng.NextDouble() < 0.7;
                if (apt) Apartment(new Vector3(x, CityY, z), R(45f, 65f), _rng.Next(12, 26) + Mathf.RoundToInt(far * 6f), R(-12f, 12f) + (_rng.NextDouble() < 0.2 ? 90f : 0f), seed++, false);
                else Box("FarOffice", new Vector3(x, CityY, z), new Vector3(R(18f, 32f), R(25f, 70f), R(18f, 30f)), R(0f, 90f), _officeMats[seed % 2], seed++, false, false);
                placed++;
            }
        }

        // ---------------------------------------------------------------- 먼 풍경

        /// <summary>하류(북쪽) 너머 롯데월드타워 (실제 11km — 조금 보이게 줄여서 가깝게), 양쪽 멀리 숲 덮인 산줄기.</summary>
        void Skyline()
        {
            HanRiverLandmarks.Lotte(_root, new Vector3(1060f, CityY - 3f, 140f), 0.42f, 40f);
            var near = FarLand.KoreaForest(0.6f);
            var far = FarLand.KoreaForest(0.8f);
            MapKit.Place(_root, "Ridge", MapKit.Mountain(420f, 140f, 120f, 611, 2f, 3), new[] { near, near }, new Vector3(-250f, CityY - 4f, -1050f));
            MapKit.Place(_root, "Ridge", MapKit.Mountain(380f, 120f, 150f, 612, 2f, 3), new[] { far, far }, new Vector3(450f, CityY - 4f, -1100f));
            MapKit.Place(_root, "Ridge", MapKit.Mountain(460f, 150f, 110f, 613, 2f, 3), new[] { near, near }, new Vector3(-350f, CityY - 4f, 1050f));
            MapKit.Place(_root, "Ridge", MapKit.Mountain(300f, 120f, 90f, 614, 2f, 2), new[] { far, far }, new Vector3(-1100f, CityY - 4f, 0f));
        }
    }
}
