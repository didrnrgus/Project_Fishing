using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// <see cref="CoastSpec.Extra"/>(맵마다 다른 랜드마크)를 짓는 코드가 쓰는 공개 도우미, 그리고 새 해외 맵의 집 양식 4가지
    /// (수상·고상 가옥, 리조트 빌라, 타이 겹지붕 집, 베네치아풍 회벽 집).
    /// </summary>
    public partial class CoastWorldBuilder
    {
        // ---------------- 공개 도우미 (맵 고유 풍경용) ----------------

        public Transform Root => _root;
        public CoastMapDef Map => _map;
        public CoastSpec Spec => S;
        public System.Random Rng => _rng;
        public CoastProps Props => _props;
        /// <summary>바위·콘크리트·나무 데크 기본 재질 (맵 설정에 맞춘 것).</summary>
        public Material RockMat => _rock;
        public Material ConcreteMat => _concrete;
        public Material WoodMat => _wood;

        public float Rand(float min, float max) => R(min, max);
        /// <summary>지형 높이 (물 밑이면 수면보다 낮다).</summary>
        public float Height(float x, float z) => H(x, z);
        /// <summary>건물 발밑 높이(네 귀퉁이 중 가장 낮은 곳)와 평평한지.</summary>
        public (float y, bool flat) FootingAt(float x, float z, float half) => Footing(x, z, half);
        public bool IsReservedOrFeature(float x, float z) => InFeature(x, z);
        /// <summary>그 맵 양식의 집 한 채.</summary>
        public void PlaceHouse(Vector3 at, float yaw, bool sign = false) => House(at, yaw, sign);
        /// <summary>그 맵 양식의 나무 한 그루.</summary>
        public void PlaceTree(Vector3 at) => Tree(at);
        /// <summary>키클라데스 흰 집 (church = 파란 돔 교회). below = 땅속으로 내린 벽 깊이 (비탈에 박힌 집).</summary>
        public void PlaceCycladic(Vector3 at, float yaw, bool church, float below = 3f) => CycladicHouse(at, yaw, church, below);
        /// <summary>위가 둥근 아치 판 메쉬 (바닥 중심, 너비·높이 m).</summary>
        public static Mesh ArchMesh(float width, float height) => ArchPanel(width, height);
        /// <summary>물 위에 배 띄우기 (Props/junk·Props/felucca 또는 Kenney 배 경로, 길이 m).</summary>
        public void PlaceBoats(string path, int count, float length) => Boats(path, count, length);
        /// <summary>밤에 켜지는 불빛 (가로등 목록에 들어가 낮엔 꺼진다).</summary>
        public void AddLamp(Vector3 pos, Color color, float range, float intensity) => _props.AddLamp(pos, color, range, intensity);
        /// <summary>밤에 빛나는 전구 재질로 바뀌는 렌더러 (가로등 전구 목록).</summary>
        public void AddBulb(Renderer r) => _bulbs.Add(r);
        public void Streetlight(Vector3 basePos, float yaw = 180f) => _props.Streetlight(basePos, yaw);
        /// <summary>석회암 봉우리 하나 (하롱베이와 같은 메쉬·재질). 바닥은 수면 아래 2m.</summary>
        public Transform KarstPeak(Vector3 at, float radius, float height, bool jungleTop = true)
        {
            var go = new GameObject("Karst");
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(at.x, _map.BaseWaterY - 2f, at.z);
            go.AddComponent<MeshFilter>().sharedMesh = StackMesh(radius, height, _rng.Next(1000), true, 4f);
            var lime = Mats.Karst(new Color(0.92f, 0.92f, 0.88f), 0.8f, jungleTop ? 0.35f : 0f);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { lime, Mats.Karst(new Color(0.42f, 0.42f, 0.4f), 0.1f), jungleTop ? Mats.Karst(new Color(0.92f, 0.92f, 0.88f), 1f, 1f) : lime };
            if (jungleTop && KenneyProps.Available && radius > 5f)
                JungleTop(go.transform, go.GetComponent<MeshFilter>().sharedMesh, Mathf.RoundToInt(Mathf.Clamp(radius * 0.6f, 3f, 14f)), radius);
            return go.transform;
        }
        /// <summary>박공지붕 단위 메쉬 (바닥 -0.5~0.5, 용마루 높이 1, 용마루는 x축 방향).</summary>
        public static Mesh Gable() => GableMesh();
        /// <summary>글자 판 (양쪽에서 읽힌다).</summary>
        public void Sign(string text, Vector3 pos, Color color) => WorldBuilder.AddSignBothSides(_root, text, pos, color);

        /// <summary>박공지붕: scale = (가로 x, 높이, 깊이 z), yaw 0이면 용마루가 x(앞면과 나란히).</summary>
        GameObject Roof(Transform parent, Vector3 pos, Vector3 scale, Material mat, float yaw = 0f)
        {
            var roof = new GameObject("Roof");
            roof.transform.SetParent(parent, false);
            roof.transform.localPosition = pos;
            roof.transform.localScale = scale;
            roof.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            roof.AddComponent<MeshFilter>().sharedMesh = GableMesh();
            roof.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return roof;
        }

        // ---------------- 새 집 양식 ----------------

        static readonly Color[] WeatheredWood =
        {
            new Color(0.55f, 0.5f, 0.44f), new Color(0.48f, 0.42f, 0.36f), new Color(0.62f, 0.58f, 0.52f), new Color(0.42f, 0.33f, 0.25f),
        };

        /// <summary>
        /// 고상 가옥 (메콩 돈콘·말레이 어촌): 기둥 위 판잣집. 바닥 1.6~2.2m, 비바람에 바랜 회갈색 판자(가로 판 줄), 함석(푸르스름한 회색) 또는 초가 박공지붕,
        /// 앞 툇마루와 사다리, 덧창. 물가면 기둥이 물에 잠긴다.
        /// </summary>
        void StiltHouse(Vector3 at, float yaw)
        {
            var h = new GameObject("StiltHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = Mats.Wood(WeatheredWood[_rng.Next(WeatheredWood.Length)], 0.12f);
            var plankLine = Mats.Lit(new Color(0.25f, 0.22f, 0.2f), 0.1f);
            var post = Mats.Wood(new Color(0.35f, 0.28f, 0.22f), 0.15f);
            bool thatch = _rng.NextDouble() < 0.45;
            var roofMat = thatch ? Mats.Thatch(new Color(0.62f, 0.5f, 0.32f)) : Mats.Weathered("rust", new Color(0.58f, 0.62f, 0.64f), 1.4f, 0.45f, 0f, 0.5f, 0.05f, null, 0.9f, 0.35f, null, 0.6f);
            float w = R(5f, 7.5f), d = R(4.5f, 6f), lift = R(1.6f, 2.2f), wallH = 2.6f;
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            // 기둥 (땅·물 아래까지)
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
                Mats.Prim(PrimitiveType.Cylinder, "Stilt", h, new Vector3(-w * 0.45f + i * w * 0.45f, (lift - 1.5f) * 0.5f, -d * 0.4f + j * d * 0.8f), new Vector3(0.22f, (lift + 1.5f) * 0.5f, 0.22f), post, false);
            Mats.Prim(PrimitiveType.Cube, "Floor", h, new Vector3(0f, lift, 0.6f), new Vector3(w + 0.4f, 0.18f, d + 1.4f), wood, false);
            // 판벽 (HouseKit: 문·창 자리를 비운 얇은 판벽, 창은 덧창 판)
            var p = new HouseKit.Parts { WallMat = wood, TrimMat = post, FrameMat = post, GlassMat = plankLine, DoorMat = plankLine, RoofMat = roofMat, UnderMat = wood, PipeMat = post };
            var front = new List<HouseKit.Opening>
            {
                new HouseKit.Opening(R(-w * 0.2f, w * 0.2f), lift + 0.1f, 0.9f, 1.9f, HouseKit.Hole.Door),
                new HouseKit.Opening(w * 0.32f, lift + 1.1f, 0.9f, 0.8f),
            };
            if (Mathf.Abs(front[0].X - front[1].X) < 1.2f) front.RemoveAt(1);
            var side = new List<HouseKit.Opening> { new HouseKit.Opening(0f, lift + 1.1f, 0.8f, 0.7f) };
            HouseKit.Box(p, w, d, lift + 0.08f, lift + wallH, 0.08f, front, null, side, side, HouseKit.WallShade(lift, lift + wallH), local, 0f, 0.05f);
            foreach (var o in front)
                if (o.Kind == HouseKit.Hole.Window) // 위로 들어 올린 나무 덧창 (받침대로 괸다)
                    p.Trim.Block(new Vector3(o.X, o.Y + o.H + 0.25f, d * 0.5f + 0.28f), new Vector3(o.W + 0.1f, 0.04f, 0.6f), 0.01f, Quaternion.Euler(-40f, 0f, 0f), Color.white, Color.white);
            for (int k = 1; k < 5; k++) // 가로 판자 줄
                Mats.Prim(PrimitiveType.Cube, "Plank", h, new Vector3(0f, lift + k * wallH / 5f, d * 0.5f + 0.01f), new Vector3(w, 0.03f, 0.02f), plankLine, false);
            // 툇마루 난간 + 사다리
            Mats.Prim(PrimitiveType.Cube, "Rail", h, new Vector3(0f, lift + 0.55f, d * 0.5f + 1.25f), new Vector3(w + 0.4f, 0.06f, 0.06f), post, false);
            for (int k = 0; k < 6; k++)
                Mats.Prim(PrimitiveType.Cube, "Baluster", h, new Vector3(-w * 0.5f + k * (w / 5f), lift + 0.3f, d * 0.5f + 1.25f), new Vector3(0.05f, 0.55f, 0.05f), post, false);
            var ladder = Quaternion.Euler(-28f, 0f, 0f);
            Mats.Prim(PrimitiveType.Cube, "Ladder", h, new Vector3(-w * 0.35f, lift * 0.5f, d * 0.5f + 1.9f), new Vector3(0.6f, lift * 1.15f, 0.06f), post, false, ladder);
            // 초가(두꺼운 짚 층·들쭉날쭉한 처마 끝) 또는 함석 골지붕, 툇마루까지 덮게 처마를 길게
            if (thatch) HouseKit.GableRoof(p, lift + wallH, w, d, 2.1f, 0.6f, 1.2f, HouseKit.Skin.Thatch, p.Wall, 0.08f, 0.22f, false);
            else HouseKit.GableRoof(p, lift + wallH, w, d, 1.5f, 0.6f, 1.1f, HouseKit.Skin.Corrugated, p.Wall, 0.08f, 0.06f, false);
            p.Emit(h);
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, (lift + wallH) * 0.5f, 0f);
            col.size = new Vector3(w, lift + wallH, d);
        }

        static readonly Color[] ResortRoofs = { new Color(0.78f, 0.36f, 0.22f), new Color(0.15f, 0.48f, 0.5f), new Color(0.6f, 0.45f, 0.3f) };

        /// <summary>리조트 빌라: 하얀 회벽 2층(가끔 1층 초가 방갈로), 벽에 들어간 넓은 유리창·발코니, 주황·청록 기와 지붕(맞배 둘을 엇갈려), 테라스 화분. 해변 리조트 맵(빈펄·코사무이·티오만·탱글).</summary>
        void ResortVilla(Vector3 at, float yaw)
        {
            var h = new GameObject("ResortVilla").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var white = Mats.Wall(new Color(0.96f, 0.95f, 0.92f), 0.12f);
            var glass = Mats.Lit(new Color(0.35f, 0.5f, 0.58f), 0.85f, 0.1f);
            var wood = Mats.Wood(new Color(0.55f, 0.38f, 0.24f), 0.3f);
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            var frame = Mats.Lit(new Color(0.3f, 0.24f, 0.2f), 0.4f);
            if (_rng.NextDouble() < 0.4)
            {
                // 초가 방갈로: 나무 벽(큰 유리 미닫이) + 높은 초가 박공
                float bw = R(5f, 6.5f), bd = R(5f, 6f);
                Mats.Prim(PrimitiveType.Cube, "Deck", h, new Vector3(0f, 0.35f, 0.8f), new Vector3(bw + 1.5f, 0.2f, bd + 2.5f), wood, false);
                var bp = new HouseKit.Parts { WallMat = wood, TrimMat = wood, FrameMat = frame, GlassMat = glass, Glass2Mat = glass, DoorMat = frame };
                HouseKit.Box(bp, bw, bd, 0.45f, 3.05f, 0.1f, new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0.55f, bw * 0.7f, 1.9f) }, null, null, null,
                    HouseKit.WallShade(0.45f, 3.05f), local, 0f, 0.06f);
                bp.RoofMat = Mats.Thatch(new Color(0.66f, 0.54f, 0.34f));
                bp.UnderMat = wood;
                HouseKit.GableRoof(bp, 3.05f, bw, bd, 2.6f, 0.8f, 1.2f, HouseKit.Skin.Thatch, bp.Wall, 0.1f, 0.24f, false);
                bp.Emit(h);
                var bc = h.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 2f, 0f);
                bc.size = new Vector3(bw, 4f, bd);
                return;
            }
            float w = R(8f, 12f), d = R(7f, 9f);
            int floors = _rng.NextDouble() < 0.6 ? 2 : 1;
            const float fh = 3.2f;
            float hgt = floors * fh;
            var tile = Mats.Tile(ResortRoofs[_rng.Next(ResortRoofs.Length)], 0.3f);
            var p = new HouseKit.Parts
            {
                WallMat = white, TrimMat = white, FrameMat = frame, GlassMat = glass, Glass2Mat = Mats.Lit(new Color(0.55f, 0.5f, 0.42f), 0.6f), DoorMat = wood,
                RoofMat = tile, UnderMat = wood, PipeMat = frame,
            };
            var shade = HouseKit.WallShade(0f, hgt);
            var front = new List<HouseKit.Opening>();
            var side = new List<HouseKit.Opening>();
            for (int f = 0; f < floors; f++)
            {
                front.Add(new HouseKit.Opening(0f, f * fh + 0.45f, w * 0.72f, 2.1f));
                side.Add(new HouseKit.Opening(0f, f * fh + 1.1f, 1.4f, 1.2f));
            }
            HouseKit.Box(p, w, d, -1f, hgt, 0.25f, front, null, side, side, shade, local);
            for (int f = 1; f < floors; f++)
            {
                float y = f * fh;
                p.Trim.Block(new Vector3(0f, y + 0.05f, d * 0.5f + 0.7f), new Vector3(w * 0.85f, 0.18f, 1.4f), 0.04f, Quaternion.identity, Color.white, Color.white * 0.8f);
                p.Glass.Block(new Vector3(0f, y + 0.6f, d * 0.5f + 1.38f), new Vector3(w * 0.85f, 0.9f, 0.04f), 0f, Quaternion.identity, Color.white, Color.white);
                p.Frame.Block(new Vector3(0f, y + 1.07f, d * 0.5f + 1.38f), new Vector3(w * 0.86f, 0.05f, 0.07f), 0f, Quaternion.identity, Color.white, Color.white);
            }
            // 기와 맞배 둘을 엇갈려 (모임지붕 느낌): 길게 한 채 + 앞뒤로 가로지른 짧은 한 채
            HouseKit.GableRoof(p, hgt, w, d, 1.8f, 0.6f, 0.6f, HouseKit.Skin.Barrel, p.Wall, 0.25f, 0.1f, true, shade);
            p.Emit(h);
            var rig = HouseKit.Rig(h, Vector3.zero, 90f);
            var rp = new HouseKit.Parts { WallMat = white, TrimMat = white, RoofMat = tile, UnderMat = wood, PipeMat = frame };
            HouseKit.GableRoof(rp, hgt, d, w * 0.6f, 1.75f, 0.6f, 0.1f, HouseKit.Skin.Barrel, rp.Wall, 0.25f, 0.1f, false, shade);
            rp.Emit(rig);
            for (int k = -1; k <= 1; k += 2)
                Mats.Prim(PrimitiveType.Cylinder, "Planter", h, new Vector3(k * w * 0.42f, 0.4f, d * 0.5f + 1.2f), new Vector3(0.8f, 0.4f, 0.8f), Mats.Lit(new Color(0.7f, 0.45f, 0.3f), 0.2f), false);
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, hgt * 0.5f, 0f);
            col.size = new Vector3(w, hgt + 1f, d);
        }

        /// <summary>
        /// 타이 전통 가옥 (방콕 강변): 짧은 기둥 위 티크(적갈색) 판벽(문·창을 비운), 아주 가파른 평기와 박공을 두 겹 얹고 끝을 하늘로 휘게(깔래) 한 주황·초록 지붕.
        /// </summary>
        void ThaiHouse(Vector3 at, float yaw)
        {
            var h = new GameObject("ThaiHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var teak = Mats.Wood(new Color(0.42f, 0.24f, 0.14f), 0.25f);
            var post = Mats.Wood(new Color(0.3f, 0.2f, 0.14f), 0.2f);
            var tile = Mats.Tile(_rng.NextDouble() < 0.7 ? new Color(0.82f, 0.38f, 0.16f) : new Color(0.2f, 0.45f, 0.3f), 0.35f);
            var gold = Mats.Lit(new Color(0.9f, 0.72f, 0.3f), 0.6f, 0.6f);
            float w = R(6f, 8f), d = R(5f, 6.5f), lift = 1.2f, wallH = 2.8f;
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            for (int i = 0; i < 4; i++)
                Mats.Prim(PrimitiveType.Cylinder, "Post", h, new Vector3(i < 2 ? -w * 0.42f : w * 0.42f, (lift - 1f) * 0.5f, i % 2 == 0 ? -d * 0.4f : d * 0.4f), new Vector3(0.25f, (lift + 1f) * 0.5f, 0.25f), post, false);
            Mats.Prim(PrimitiveType.Cube, "Floor", h, new Vector3(0f, lift, 0.5f), new Vector3(w + 0.6f, 0.2f, d + 1.2f), teak, false);
            var dark = Mats.Lit(new Color(0.12f, 0.1f, 0.09f), 0.3f);
            var p = new HouseKit.Parts { WallMat = teak, TrimMat = post, FrameMat = post, GlassMat = dark, DoorMat = post, RoofMat = tile, UnderMat = teak, PipeMat = post };
            var front = new List<HouseKit.Opening> { new HouseKit.Opening(0f, lift + 0.1f, 1f, 2f, HouseKit.Hole.Door) };
            var side = new List<HouseKit.Opening> { new HouseKit.Opening(-d * 0.15f, lift + 1f, 0.8f, 1f), new HouseKit.Opening(d * 0.2f, lift + 1f, 0.8f, 1f) };
            front.Add(new HouseKit.Opening(-w * 0.3f, lift + 1f, 0.8f, 1f));
            front.Add(new HouseKit.Opening(w * 0.3f, lift + 1f, 0.8f, 1f));
            HouseKit.Box(p, w, d, lift + 0.1f, lift + wallH, 0.1f, front, null, side, side, HouseKit.WallShade(lift, lift + wallH), local, 0f, 0.06f);
            // 두 겹 가파른 지붕 (아래가 넓고 위가 좁다), 평기와가 줄마다 층진다
            HouseKit.GableRoof(p, lift + wallH, w, d, 2.6f, 0.8f, 0.8f, HouseKit.Skin.FlatTile, p.Wall, 0.1f, 0.1f, false);
            p.Emit(h);
            var rig = HouseKit.Rig(h, new Vector3(0f, 1.2f, 0f), 0f);
            var rp = new HouseKit.Parts { WallMat = teak, TrimMat = post, RoofMat = tile, UnderMat = teak };
            HouseKit.GableRoof(rp, lift + wallH, w - 1f, d * 0.6f, 2.8f * 0.6f, 0.8f, 0.25f, HouseKit.Skin.FlatTile, null, 0.1f, 0.1f, false);
            rp.Emit(rig);
            // 박공 끝 깔래 (하늘로 휜 금빛 뿔)
            foreach (float sx in new[] { -1f, 1f })
                Mats.Prim(PrimitiveType.Capsule, "Kalae", h, new Vector3(sx * (w * 0.5f + 0.6f), lift + wallH + 2.9f, 0f), new Vector3(0.12f, 0.7f, 0.12f), gold, false, Quaternion.Euler(0f, 0f, sx * -20f));
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, (lift + wallH) * 0.5f, 0f);
            col.size = new Vector3(w, lift + wallH, d);
        }

        static readonly Color[] VenetianWalls =
        {
            new Color(0.93f, 0.78f, 0.45f), new Color(0.88f, 0.55f, 0.38f), new Color(0.95f, 0.88f, 0.72f), new Color(0.8f, 0.6f, 0.55f),
            new Color(0.9f, 0.82f, 0.6f), new Color(0.72f, 0.78f, 0.7f),
        };
        static readonly string[] TavernSigns = { "TAVERNA", "PSAROTAVERNA", "CAFE", "LOKANTA", "BALIK" };

        /// <summary>
        /// 베네치아풍 항구 집 (크레타 하니아, 안탈리아 칼레이치): 2~4층 파스텔 회벽(황토·살구·크림), 벽에 깊이 들어간 창과 아치 문, 초록·갈색 비늘살 덧창,
        /// 박공을 항구로 보인 붉은 반원 기와 지붕, 가끔 철 난간 발코니와 간판.
        /// </summary>
        void VenetianHouse(Vector3 at, float yaw, bool sign)
        {
            var h = new GameObject("VenetianHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wall = Mats.Wall(VenetianWalls[_rng.Next(VenetianWalls.Length)], 0.1f);
            var shutter = Mats.Lit(_rng.NextDouble() < 0.6 ? new Color(0.2f, 0.42f, 0.3f) : new Color(0.42f, 0.28f, 0.18f), 0.25f);
            var dark = Mats.Lit(new Color(0.16f, 0.15f, 0.14f), 0.4f);
            var tile = Mats.Tile(new Color(0.72f, 0.32f, 0.2f), 0.2f);
            var stone = Mats.Stone(new Color(0.78f, 0.74f, 0.66f), 0.15f);
            float w = R(5f, 7f), d = R(7f, 9f);
            int floors = _rng.Next(2, 5);
            const float fh = 3f;
            float hgt = floors * fh;
            var local = new System.Random(Mathf.RoundToInt(at.x * 131f + at.z * 71f));
            var iron = Mats.Lit(new Color(0.12f, 0.12f, 0.13f), 0.35f, 0.4f);
            var slat = Mats.Lit(shutter.color * 0.7f, 0.25f);
            var p = new HouseKit.Parts
            {
                WallMat = wall, TrimMat = stone, FrameMat = Mats.Lit(new Color(0.85f, 0.83f, 0.78f), 0.4f), GlassMat = dark, Glass2Mat = Mats.Lit(new Color(0.3f, 0.26f, 0.22f), 0.6f),
                DoorMat = dark, MetalMat = iron, RoofMat = tile, UnderMat = wall, AccentMat = stone, PipeMat = Mats.Lit(new Color(0.5f, 0.38f, 0.3f), 0.3f, 0.3f),
            };
            var shade = HouseKit.WallShade(0f, hgt);
            // 사진(하니아 베네치아 항구): 깊이 들어간 아치 문, 창 + 비늘살 나무 덧창, 철 난간 발코니, 층 띠·처마 장식띠, 물받이 홈통, 꽃 화분
            float fz = d * 0.5f;
            var front = new List<HouseKit.Opening> { new HouseKit.Opening(0f, 0f, 1.4f, 2.6f, HouseKit.Hole.Arch) };
            for (int f = 1; f < floors; f++)
                for (int k = -1; k <= 1; k += 2)
                    front.Add(new HouseKit.Opening(k * w * 0.27f, f * fh + 0.7f, 0.8f, 1.4f));
            HouseKit.Box(p, w, d, -1.2f, hgt, 0.3f, front, null, null, null, shade, local, 0f, 0.16f);
            // 아치 문 돌 테 (문 둘레 덧댄 돌), 아래 돌 띠
            foreach (float sx in new[] { -1f, 1f }) // 문 양옆 아래 돌 띠
                p.Accent.Block(new Vector3(sx * (w * 0.25f + 0.47f), 0.4f, fz + 0.02f), new Vector3(w * 0.5f - 0.94f, 0.8f, 0.05f), 0.01f, Quaternion.identity, Color.white, Color.white * 0.85f);
            foreach (float sx in new[] { -1f, 1f })
                p.Accent.Block(new Vector3(sx * 0.82f, 1.3f, fz + 0.04f), new Vector3(0.24f, 2.6f, 0.08f), 0.015f, Quaternion.identity, Color.white, Color.white * 0.85f);
            p.Accent.Block(new Vector3(0f, 2.72f, fz + 0.04f), new Vector3(1.88f, 0.24f, 0.08f), 0.015f, Quaternion.identity, Color.white, Color.white * 0.85f);
            for (int f = 1; f < floors; f++)
            {
                // 층 띠 (얇은 돌 띠)
                p.Accent.Block(new Vector3(0f, f * fh + 0.05f, fz + 0.05f), new Vector3(w + 0.06f, 0.14f, 0.1f), 0.02f, Quaternion.identity, Color.white, Color.white * 0.85f);
                bool balcony = _rng.NextDouble() < 0.45;
                for (int k = -1; k <= 1; k += 2)
                {
                    float x = k * w * 0.27f, y = f * fh + 1.4f;
                    foreach (float sx in new[] { -0.6f, 0.6f })
                    {
                        Mats.Prim(PrimitiveType.Cube, "Shutter", h, new Vector3(x + sx, y, fz + 0.03f), new Vector3(0.38f, 1.45f, 0.04f), shutter, false);
                        for (int sl = 0; sl < 5; sl++)
                            Mats.Prim(PrimitiveType.Cube, "Louver", h, new Vector3(x + sx, y - 0.56f + sl * 0.28f, fz + 0.055f), new Vector3(0.32f, 0.035f, 0.02f), slat, false);
                    }
                    if (balcony)
                    {
                        // 철 난간 발코니: 돌 받침 판 + 위 손잡이 + 세로 살
                        p.Accent.Block(new Vector3(x, y - 0.75f, fz + 0.4f), new Vector3(1.5f, 0.1f, 0.75f), 0.02f, Quaternion.identity, Color.white, Color.white * 0.8f);
                        p.Metal.Block(new Vector3(x, y + 0.15f, fz + 0.76f), new Vector3(1.5f, 0.04f, 0.04f), 0f, Quaternion.identity, Color.white, Color.white);
                        for (int bb = 0; bb < 9; bb++)
                            p.Metal.Block(new Vector3(x - 0.7f + bb * 0.175f, y - 0.3f, fz + 0.76f), new Vector3(0.02f, 0.85f, 0.02f), 0f, Quaternion.identity, Color.white, Color.white);
                        if (_rng.NextDouble() < 0.6)
                            Mats.Prim(PrimitiveType.Sphere, "Geranium", h, new Vector3(x + 0.45f, y - 0.5f, fz + 0.5f), new Vector3(0.4f, 0.3f, 0.3f),
                                Mats.Lit(_rng.NextDouble() < 0.5 ? new Color(0.85f, 0.15f, 0.2f) : new Color(0.9f, 0.4f, 0.6f), 0.3f), false);
                    }
                }
            }
            // 처마 장식띠 (톱니 돌림띠)
            p.Accent.Block(new Vector3(0f, hgt - 0.12f, fz + 0.08f), new Vector3(w + 0.2f, 0.24f, 0.16f), 0.02f, Quaternion.identity, Color.white, Color.white * 0.85f);
            for (int k = 0; k < Mathf.RoundToInt(w / 0.4f); k++)
                p.Accent.Block(new Vector3(-w * 0.5f + 0.2f + k * 0.4f, hgt - 0.3f, fz + 0.1f), new Vector3(0.18f, 0.12f, 0.1f), 0.01f, Quaternion.identity, Color.white, Color.white * 0.85f);
            p.Pipes.Add(new[] { new Vector3(w * 0.5f - 0.15f, hgt, fz + 0.1f), new Vector3(w * 0.5f - 0.15f, 0.1f, fz + 0.1f) });
            p.Emit(h);
            // 박공이 앞(항구)을 본다: 지붕을 90° 돌려 용마루가 앞뒤로
            var rig = HouseKit.Rig(h, Vector3.zero, 90f);
            var rp = new HouseKit.Parts { WallMat = wall, TrimMat = stone, RoofMat = tile, UnderMat = wall, PipeMat = p.PipeMat };
            HouseKit.GableRoof(rp, hgt, d, w, 1.6f, 0.3f, 0.3f, HouseKit.Skin.Barrel, rp.Wall, 0.3f, 0.1f, false, shade);
            rp.Emit(rig);
            if (sign)
            {
                var board = Mats.Lit(new Color(0.15f, 0.25f, 0.45f), 0.3f);
                Mats.Prim(PrimitiveType.Cube, "SignBoard", h, new Vector3(0f, 2.75f, d * 0.5f + 0.1f), new Vector3(w - 1f, 0.6f, 0.08f), board, false);
                // 항구 타베르나: 크림색 천 차양 + 바깥 식탁·의자
                var awning = Mats.Lit(new Color(0.92f, 0.88f, 0.76f), 0.2f);
                Mats.Prim(PrimitiveType.Cube, "Awning", h, new Vector3(0f, 2.35f, d * 0.5f + 1.3f), new Vector3(w + 0.4f, 0.06f, 2.6f), awning, false, Quaternion.Euler(-12f, 0f, 0f));
                Mats.Prim(PrimitiveType.Cube, "AwningValance", h, new Vector3(0f, 1.95f, d * 0.5f + 2.55f), new Vector3(w + 0.4f, 0.3f, 0.03f), awning, false);
                var tabl = Mats.Wood(new Color(0.55f, 0.4f, 0.26f), 0.3f);
                for (int k = -1; k <= 1; k += 2)
                {
                    var tp = new Vector3(k * w * 0.25f, 0f, d * 0.5f + 1.6f);
                    Mats.Prim(PrimitiveType.Cylinder, "TableTop", h, tp + Vector3.up * 0.75f, new Vector3(0.8f, 0.03f, 0.8f), tabl, false);
                    Mats.Prim(PrimitiveType.Cylinder, "TableLeg", h, tp + Vector3.up * 0.37f, new Vector3(0.06f, 0.37f, 0.06f), Mats.Lit(new Color(0.15f, 0.15f, 0.15f), 0.4f), false);
                    foreach (float cx in new[] { -0.6f, 0.6f })
                        Mats.Prim(PrimitiveType.Cube, "Chair", h, tp + new Vector3(cx, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.4f), Mats.Lit(new Color(0.2f, 0.42f, 0.6f), 0.3f), false);
                }
                var t = UIFactory.WorldText(h, TavernSigns[_rng.Next(TavernSigns.Length)], new Vector3(0f, 2.75f, d * 0.5f + 0.16f), new Color(1f, 0.92f, 0.7f), 4f, "Sign");
                UIFactory.OneSided(t);
                t.transform.localScale = Vector3.one * 0.8f;
                t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, hgt * 0.5f, 0f);
            col.size = new Vector3(w, hgt + 1f, d);
        }
    }
}
