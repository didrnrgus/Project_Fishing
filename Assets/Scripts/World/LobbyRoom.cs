using Fishing.Angling;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 대기실 "한국 낚시 조합". 원작 대기실(cpmw_lobby_ship.mp4 0:00~0:10, clips/01 로비)을 같은 구도로 옮긴다:
    /// 카메라는 허리 높이에서 수평으로 캐릭터 정면을 보고(캐릭터가 화면 가운데 세로 70%), 뒤로 1.7m에 벽이 있는 얕은 방.
    ///  - 뒷벽: 아래 1.1m는 세로 판재 징두리(위에 걸레받이 같은 몰딩), 위는 갈색 벽돌. 천장은 어두운 나무 들보, 왼쪽 위에 매달린 갓 전등.
    ///  - 왼쪽: 잉어 수묵화 족자, 한반도 지도 액자, 낮은 책장(위에 금빛·은빛 트로피, 둥근 사진 액자), 맨 왼쪽 산세베리아 화분.
    ///  - 가운데: 박제 물고기(큰 검은 볼락 위, 흰 물고기 둘 아래), 그 오른쪽 사진을 꽂은 세계지도. 벽 앞에 둥근 카페 탁자(찻잔 둘) + 살대 의자 둘.
    ///  - 오른쪽: 벽 선반 세 단(책·고양이 장식·트로피·접시·액자·금붕어·릴), 책상 위 모니터·연필꽂이, 책상 의자(지도 두루마리),
    ///    큰 앤티크 지구본, 맨 오른쪽 세로로 늘어진 태극기.
    ///  - 바닥: 어두운 마루 + 캐릭터 발밑 페르시아 양탄자.
    /// 수족관은 카메라 뒤쪽 벽에 두어 대기실 화면에는 안 보이고, 수족관 창을 열면 그쪽을 비춘다. 필드에서 멀리 떨어진 곳에 만들어 두고 카메라만 옮긴다.
    /// </summary>
    public class LobbyRoom : MonoBehaviour
    {
        public static readonly Vector3 Origin = new Vector3(0f, 0f, -420f);
        // 방 크기: 왼쪽 벽 x, 오른쪽 벽 x, 뒷벽 z, 앞벽 z(카메라 뒤), 천장 높이
        const float XL = -3.6f, XR = 4.2f, ZB = -2.4f, ZF = 2.6f, H = 3.05f;
        const float Wainscot = 1.12f;

        public Aquarium Tank { get; private set; }

        // 원작 구도: 눈높이 1m에서 수평으로, 캐릭터(0.2, 0, -0.6)에서 2.05m 앞
        public Vector3 CameraPos => Origin + new Vector3(0.2f, 1.0f, 1.45f);
        public Quaternion CameraRot => Quaternion.LookRotation((Origin + new Vector3(0.2f, 0.94f, -0.6f)) - CameraPos);

        static readonly Color Wood = new Color(0.42f, 0.28f, 0.17f);
        static readonly Color WoodDark = new Color(0.24f, 0.15f, 0.09f);
        static readonly Color WoodLight = new Color(0.66f, 0.48f, 0.3f);

        public static LobbyRoom Build()
        {
            var go = new GameObject("LobbyRoom");
            go.transform.position = Origin;
            var room = go.AddComponent<LobbyRoom>();
            room.BuildRoom();
            return room;
        }

        void BuildRoom()
        {
            var t = transform;
            // 실내라 물 자국·이끼 없는 판재 결
            var floor = Mats.Weathered("planks", new Color(0.3f, 0.19f, 0.12f), 1.6f, 0.35f, 0f, 0f, 0f, null, 0.6f);
            var panel = Mats.Weathered("planks", new Color(0.5f, 0.33f, 0.2f), 1.2f, 0.3f, 0f, 0f, 0f, null, 0.6f);
            var dark = Mats.Lit(WoodDark, 0.2f);
            var wood = Mats.Lit(Wood, 0.35f);
            var light = Mats.Lit(WoodLight, 0.3f);
            float W = XR - XL, D = ZF - ZB, cx = (XL + XR) / 2f, cz = (ZB + ZF) / 2f;

            // 바닥, 천장, 벽 (벽 안쪽 면: 뒷벽 ZB+0.1)
            float back = ZB + 0.1f;
            Mats.Prim(PrimitiveType.Cube, "Floor", t, new Vector3(cx, -0.05f, cz), new Vector3(W, 0.1f, D), floor, false);
            Mats.Prim(PrimitiveType.Cube, "Ceiling", t, new Vector3(cx, H + 0.1f, cz), new Vector3(W, 0.2f, D), dark, false);
            for (float x = XL + 0.6f; x < XR; x += 1.5f)
                Mats.Prim(PrimitiveType.Cube, "Beam", t, new Vector3(x, H - 0.12f, cz), new Vector3(0.22f, 0.24f, D), dark, false);
            Mats.Prim(PrimitiveType.Cube, "Joist", t, new Vector3(cx, H - 0.16f, back + 0.15f), new Vector3(W, 0.3f, 0.3f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "BackWall", t, new Vector3(cx, H / 2f, ZB), new Vector3(W, H, 0.2f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "LeftWall", t, new Vector3(XL, H / 2f, cz), new Vector3(0.2f, H, D), dark, false);
            Mats.Prim(PrimitiveType.Cube, "RightWall", t, new Vector3(XR, H / 2f, cz), new Vector3(0.2f, H, D), dark, false);
            Mats.Prim(PrimitiveType.Cube, "FrontWall", t, new Vector3(cx, H / 2f, ZF), new Vector3(W, H, 0.2f), dark, false);

            // 벽돌 (징두리 위로 천장까지) + 징두리 판재 + 몰딩
            float brickH = H - Wainscot;
            var brick = new Material(Shader.Find("Standard")) { mainTexture = BrickTexture, mainTextureScale = new Vector2(W / 0.9f, brickH / 0.6f) };
            brick.SetFloat("_Glossiness", 0.05f);
            Mats.Prim(PrimitiveType.Quad, "Bricks", t, new Vector3(cx, Wainscot + brickH / 2f, back + 0.005f), new Vector3(W, brickH, 1f), brick, false,
                Quaternion.Euler(0f, 180f, 0f));
            var brickSide = new Material(brick) { mainTextureScale = new Vector2(D / 0.9f, brickH / 0.6f) };
            Mats.Prim(PrimitiveType.Quad, "BricksL", t, new Vector3(XL + 0.105f, Wainscot + brickH / 2f, cz), new Vector3(D, brickH, 1f), brickSide, false,
                Quaternion.Euler(0f, -90f, 0f));
            Mats.Prim(PrimitiveType.Quad, "BricksR", t, new Vector3(XR - 0.105f, Wainscot + brickH / 2f, cz), new Vector3(D, brickH, 1f), brickSide, false,
                Quaternion.Euler(0f, 90f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Wainscot", t, new Vector3(cx, Wainscot / 2f, back + 0.02f), new Vector3(W, Wainscot, 0.04f), panel, false);
            for (float x = XL + 0.18f; x < XR; x += 0.18f)
                Mats.Prim(PrimitiveType.Cube, "Seam", t, new Vector3(x, Wainscot / 2f, back + 0.042f), new Vector3(0.012f, Wainscot - 0.06f, 0.004f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "Rail", t, new Vector3(cx, Wainscot, back + 0.05f), new Vector3(W, 0.07f, 0.1f), wood, false);
            Mats.Prim(PrimitiveType.Cube, "Skirting", t, new Vector3(cx, 0.06f, back + 0.05f), new Vector3(W, 0.12f, 0.06f), dark, false);
            foreach (float x in new[] { XL + 0.12f, XR - 0.12f })
                Mats.Prim(PrimitiveType.Cube, "SideWainscot", t, new Vector3(x, Wainscot / 2f, cz), new Vector3(0.04f, Wainscot, D), panel, false);

            // 양탄자 (캐릭터 발밑, 원작: 베이지 바탕 + 붉은 테두리 + 가운데 메달리온)
            var rugMat = new Material(Shader.Find("Standard")) { mainTexture = RugTexture };
            rugMat.SetFloat("_Glossiness", 0f);
            Mats.Prim(PrimitiveType.Quad, "Rug", t, new Vector3(0.2f, 0.006f, -0.45f), new Vector3(3.4f, 2.1f, 1f), rugMat, false, Quaternion.Euler(90f, 0f, 0f));

            // ── 뒷벽 왼쪽: 잉어 수묵화 족자, 한반도 지도 ──
            Framed(t, "CarpPainting", PaintingTexture, new Vector3(-2.85f, 2.2f, back), new Vector2(0.72f, 0.98f), dark);
            Framed(t, "KoreaMap", KoreaMap.Texture, new Vector3(-1.75f, 1.9f, back), new Vector2(0.5f, 0.74f), Mats.Lit(new Color(0.85f, 0.82f, 0.75f), 0.2f));

            // 낮은 책장 (위에 트로피·사진 액자)
            var shelf = new GameObject("Bookshelf").transform;
            shelf.SetParent(t, false);
            shelf.localPosition = new Vector3(-2.2f, 0f, back + 0.18f);
            Mats.Prim(PrimitiveType.Cube, "Back", shelf, new Vector3(0f, 0.62f, -0.15f), new Vector3(1.3f, 1.24f, 0.03f), dark, false);
            foreach (float sx in new[] { -0.64f, 0.64f })
                Mats.Prim(PrimitiveType.Cube, "Side", shelf, new Vector3(sx, 0.62f, 0f), new Vector3(0.04f, 1.24f, 0.32f), wood, false);
            var rng = new System.Random(7);
            for (int row = 0; row < 3; row++)
            {
                float y = 0.05f + row * 0.4f;
                Mats.Prim(PrimitiveType.Cube, "Board", shelf, new Vector3(0f, y, 0f), new Vector3(1.28f, 0.035f, 0.32f), wood, false);
                Books(shelf, rng, -0.6f, 0.6f, y + 0.02f, 0f, 0.26f, 0.32f);
            }
            Mats.Prim(PrimitiveType.Cube, "Top", shelf, new Vector3(0f, 1.24f, 0f), new Vector3(1.34f, 0.04f, 0.34f), wood, false);
            Trophy(shelf, new Vector3(0.12f, 1.26f, 0f), 0.48f, Gold);
            Trophy(shelf, new Vector3(0.42f, 1.26f, -0.02f), 0.62f, Mats.Lit(new Color(0.78f, 0.8f, 0.84f), 0.75f, 0.85f));
            Trophy(shelf, new Vector3(0.55f, 1.26f, 0.06f), 0.32f, Gold);
            // 둥근 사진 액자
            Mats.Prim(PrimitiveType.Cylinder, "PhotoRing", shelf, new Vector3(-0.3f, 1.42f, -0.05f), new Vector3(0.32f, 0.015f, 0.32f), wood, false, Quaternion.Euler(80f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, "Photo", shelf, new Vector3(-0.3f, 1.42f, -0.035f), new Vector3(0.22f, 0.012f, 0.22f), Mats.Lit(new Color(0.45f, 0.55f, 0.6f)), false, Quaternion.Euler(80f, 0f, 0f));
            // 은빛 돌고래 조각 대신: 은빛 물고기 조각 (받침 + 비스듬히 선 물고기)
            Mats.Prim(PrimitiveType.Cube, "Pedestal", shelf, new Vector3(-0.52f, 1.29f, 0f), new Vector3(0.14f, 0.06f, 0.14f), dark, false);
            Mats.Prim(PrimitiveType.Sphere, "Sculpture", shelf, new Vector3(-0.52f, 1.5f, 0f), new Vector3(0.09f, 0.38f, 0.12f), Mats.Lit(new Color(0.7f, 0.74f, 0.8f), 0.8f, 0.9f), false,
                Quaternion.Euler(0f, 0f, 20f));

            // 산세베리아 화분 (맨 왼쪽 앞)
            var plant = new GameObject("Plant").transform;
            plant.SetParent(t, false);
            plant.localPosition = new Vector3(-3.05f, 0f, -1.35f);
            Mats.Prim(PrimitiveType.Cylinder, "Pot", plant, new Vector3(0f, 0.22f, 0f), new Vector3(0.42f, 0.22f, 0.42f), Mats.Lit(new Color(0.3f, 0.3f, 0.28f), 0.3f), false);
            var leaf = Mats.Lit(new Color(0.28f, 0.42f, 0.2f), 0.25f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f, tilt = 6f + (i % 3) * 7f, h = 0.75f + (i % 4) * 0.18f;
                var dir = Quaternion.Euler(tilt, a, 0f);
                Mats.Prim(PrimitiveType.Cube, "Leaf", plant, new Vector3(0f, 0.42f, 0f) + dir * new Vector3(0f, h / 2f, 0f), new Vector3(0.07f, h, 0.012f), leaf, false, dir);
            }

            // ── 뒷벽 가운데: 박제 물고기 (큰 검은 볼락 위, 흰 물고기 둘 아래) + 세계지도 ──
            MountFish(t, "rockfish", 84f, new Vector3(-0.78f, 2.23f, back + 0.12f));
            MountFish(t, "mullet", 58f, new Vector3(-1.17f, 1.68f, back + 0.12f));
            MountFish(t, "seabass", 50f, new Vector3(-0.3f, 1.62f, back + 0.12f));
            var worldMat = new Material(Shader.Find("Standard")) { mainTexture = WorldMapTexture };
            worldMat.SetFloat("_Glossiness", 0.15f);
            Mats.Prim(PrimitiveType.Quad, "WorldMap", t, new Vector3(0.45f, 2.08f, back + 0.01f), new Vector3(1.28f, 0.92f, 1f), worldMat, false, Quaternion.Euler(0f, 180f, 0f));
            // 지도에 꽂은 사진 (폴라로이드)
            var pr = new System.Random(3);
            var pins = new[] { new Vector2(-0.5f, 0.38f), new Vector2(0.12f, 0.46f), new Vector2(0.58f, 0.18f), new Vector2(-0.05f, -0.05f), new Vector2(0.5f, -0.4f), new Vector2(-0.42f, -0.46f) };
            foreach (var p in pins)
            {
                var rot = Quaternion.Euler(0f, 180f, (float)(pr.NextDouble() - 0.5) * 24f);
                var pos = new Vector3(0.45f + p.x, 2.08f + p.y, back + 0.016f);
                Mats.Prim(PrimitiveType.Quad, "Polaroid", t, pos, new Vector3(0.15f, 0.18f, 1f), Mats.Lit(new Color(0.92f, 0.92f, 0.88f)), false, rot);
                var c = Color.HSVToRGB(0.45f + (float)pr.NextDouble() * 0.2f, 0.35f, 0.35f + (float)pr.NextDouble() * 0.25f);
                Mats.Prim(PrimitiveType.Quad, "Picture", t, pos + rot * new Vector3(0f, 0.015f, -0.002f), new Vector3(0.12f, 0.12f, 1f), Mats.Lit(c), false, rot);
            }

            // 카페 탁자 + 찻잔 둘 + 살대 의자 둘 (원작: 캐릭터 왼쪽 뒤)
            var table = new GameObject("CafeTable").transform;
            table.SetParent(t, false);
            table.localPosition = new Vector3(-0.8f, 0f, -1.75f);
            Mats.Prim(PrimitiveType.Cylinder, "Top", table, new Vector3(0f, 0.68f, 0f), new Vector3(0.86f, 0.02f, 0.86f), Mats.Lit(new Color(0.36f, 0.22f, 0.13f), 0.45f), false);
            Mats.Prim(PrimitiveType.Cylinder, "Apron", table, new Vector3(0f, 0.63f, 0f), new Vector3(0.7f, 0.04f, 0.7f), dark, false);
            for (int i = 0; i < 4; i++)
            {
                var off = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0.26f, 0f, 0f);
                Mats.Prim(PrimitiveType.Cylinder, "Leg", table, off + new Vector3(0f, 0.31f, 0f), new Vector3(0.05f, 0.31f, 0.05f), dark, false);
                Mats.Prim(PrimitiveType.Sphere, "Turn", table, off + new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.1f, 0.08f), dark, false);
            }
            foreach (float x in new[] { -0.2f, 0.2f })
            {
                Mats.Prim(PrimitiveType.Cylinder, "Saucer", table, new Vector3(x, 0.695f, 0.05f), new Vector3(0.15f, 0.006f, 0.15f), Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0.6f), false);
                Mats.Prim(PrimitiveType.Cylinder, "Cup", table, new Vector3(x, 0.735f, 0.05f), new Vector3(0.08f, 0.035f, 0.08f), Mats.Lit(new Color(0.25f, 0.22f, 0.2f), 0.6f), false);
            }
            Chair(t, new Vector3(-1.5f, 0f, -1.95f), 0f, light);
            Chair(t, new Vector3(-0.15f, 0f, -2.0f), 0f, light);

            // ── 오른쪽: 벽 선반 세 단 ──
            var shelves = new GameObject("WallShelves").transform;
            shelves.SetParent(t, false);
            shelves.localPosition = new Vector3(0f, 0f, back);
            float[] boardY = { 1.68f, 2.1f, 2.52f };
            foreach (float y in boardY)
            {
                Mats.Prim(PrimitiveType.Cube, "Shelf", shelves, new Vector3(2.75f, y, 0.15f), new Vector3(3.0f, 0.04f, 0.3f), wood, false);
                for (float x = 1.5f; x < 4.1f; x += 1.2f)
                    Mats.Prim(PrimitiveType.Cube, "Bracket", shelves, new Vector3(x, y - 0.08f, 0.08f), new Vector3(0.03f, 0.14f, 0.14f), dark, false);
            }
            var srng = new System.Random(11);
            // 맨 위 단: 고양이 장식(흰·검정), 트로피, 기대 놓은 접시, 책
            Cat(shelves, new Vector3(1.55f, boardY[2] + 0.02f, 0.15f), Mats.Lit(new Color(0.92f, 0.9f, 0.86f), 0.5f));
            Cat(shelves, new Vector3(1.78f, boardY[2] + 0.02f, 0.15f), Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.6f));
            Trophy(shelves, new Vector3(2.15f, boardY[2] + 0.02f, 0.15f), 0.4f, Gold);
            Mats.Prim(PrimitiveType.Cylinder, "Plate", shelves, new Vector3(2.62f, boardY[2] + 0.2f, 0.08f), new Vector3(0.36f, 0.01f, 0.36f), Mats.Lit(new Color(0.9f, 0.88f, 0.82f), 0.6f), false,
                Quaternion.Euler(75f, 0f, 0f));
            Books(shelves, srng, 3.1f, 4.05f, boardY[2] + 0.02f, 0.15f, 0.26f, 0.38f);
            // 가운데 단: 금빛 물고기 장식, 사진 액자, 둥근 접시 그림, 눕힌 책 더미, 책
            var gold = new GameObject("GoldFish").transform;
            gold.SetParent(shelves, false);
            gold.localPosition = new Vector3(1.6f, boardY[1] + 0.2f, 0.15f);
            gold.localRotation = Quaternion.Euler(0f, 90f, 25f);
            var gf = FishModels.Create(FishDatabase.Get("goldkoi"), 34f, gold);
            if (gf != null)
            {
                FishModels.Freeze(gf, FishModels.Flop, 0.2f);
                foreach (var r in gf.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Gold;
            }
            Framed(shelves, "Photo", null, new Vector3(2.05f, boardY[1] + 0.17f, 0.1f), new Vector2(0.24f, 0.3f), wood, Quaternion.Euler(-8f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, "DecoPlate", shelves, new Vector3(2.5f, boardY[1] + 0.17f, 0.06f), new Vector3(0.3f, 0.01f, 0.3f), Mats.Lit(new Color(0.88f, 0.86f, 0.8f), 0.5f), false,
                Quaternion.Euler(78f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                Mats.Prim(PrimitiveType.Cube, "Stack", shelves, new Vector3(2.95f, boardY[1] + 0.035f + i * 0.045f, 0.15f), new Vector3(0.34f - i * 0.02f, 0.04f, 0.24f),
                    Mats.Lit(Color.HSVToRGB((float)srng.NextDouble(), 0.4f, 0.6f)), false, Quaternion.Euler(0f, i * 7f, 0f));
            Books(shelves, srng, 3.35f, 4.05f, boardY[1] + 0.02f, 0.15f, 0.26f, 0.36f);
            // 아래 단: 릴 여섯 개
            for (int i = 0; i < 6; i++) Reel(shelves, new Vector3(1.5f + i * 0.45f, boardY[0] + 0.02f, 0.15f), srng);

            // 책상 + 모니터 + 연필꽂이 + 의자(지도 두루마리)
            var desk = new GameObject("Desk").transform;
            desk.SetParent(t, false);
            desk.localPosition = new Vector3(2.1f, 0f, back + 0.42f);
            Mats.Prim(PrimitiveType.Cube, "Top", desk, new Vector3(0f, 0.76f, 0f), new Vector3(2.3f, 0.05f, 0.75f), wood, false);
            Mats.Prim(PrimitiveType.Cube, "Apron", desk, new Vector3(0f, 0.68f, 0.33f), new Vector3(2.2f, 0.12f, 0.03f), dark, false);
            foreach (float x in new[] { -1.08f, 1.08f })
            foreach (float z in new[] { -0.32f, 0.32f })
                Mats.Prim(PrimitiveType.Cube, "Leg", desk, new Vector3(x, 0.37f, z), new Vector3(0.07f, 0.74f, 0.07f), dark, false);
            var mon = new GameObject("Monitor").transform;
            mon.SetParent(desk, false);
            mon.localPosition = new Vector3(-0.55f, 0.79f, -0.1f);
            mon.localRotation = Quaternion.Euler(0f, -55f, 0f);
            var alu = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.7f, 0.6f);
            Mats.Prim(PrimitiveType.Cube, "Foot", mon, new Vector3(0f, 0.005f, -0.05f), new Vector3(0.2f, 0.01f, 0.18f), alu, false);
            Mats.Prim(PrimitiveType.Cube, "Neck", mon, new Vector3(0f, 0.17f, -0.1f), new Vector3(0.16f, 0.34f, 0.015f), alu, false, Quaternion.Euler(-12f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Body", mon, new Vector3(0f, 0.52f, -0.06f), new Vector3(0.66f, 0.52f, 0.03f), alu, false);
            Mats.Prim(PrimitiveType.Cube, "Screen", mon, new Vector3(0f, 0.56f, -0.044f), new Vector3(0.62f, 0.38f, 0.004f), Mats.Lit(new Color(0.02f, 0.02f, 0.03f), 0.9f), false);
            Mats.Prim(PrimitiveType.Cylinder, "PenCup", desk, new Vector3(-0.12f, 0.85f, -0.05f), new Vector3(0.08f, 0.08f, 0.08f), Mats.Lit(new Color(0.45f, 0.12f, 0.1f), 0.4f), false);
            for (int i = 0; i < 4; i++)
                Mats.Prim(PrimitiveType.Cylinder, "Pen", desk, new Vector3(-0.12f + (i - 1.5f) * 0.015f, 0.97f, -0.05f), new Vector3(0.008f, 0.1f, 0.008f),
                    Mats.Lit(i % 2 == 0 ? new Color(0.15f, 0.15f, 0.18f) : new Color(0.75f, 0.6f, 0.3f)), false, Quaternion.Euler(0f, 0f, (i - 1.5f) * 6f));
            Chair(t, new Vector3(2.0f, 0f, -1.62f), 180f, light);
            Mats.Prim(PrimitiveType.Cylinder, "MapRoll", t, new Vector3(2.0f, 0.55f, -1.6f), new Vector3(0.14f, 0.27f, 0.14f), Mats.Lit(new Color(0.85f, 0.8f, 0.68f), 0.1f), false,
                Quaternion.Euler(0f, 30f, 90f));

            // 큰 앤티크 지구본 (원작: 화면 오른쪽, 의자 앞 높이 1.3m 남짓)
            var globe = new GameObject("Globe").transform;
            globe.SetParent(t, false);
            globe.localPosition = new Vector3(2.17f, 0f, -1.2f);
            Mats.Prim(PrimitiveType.Cylinder, "Base", globe, new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), dark, false);
            Mats.Prim(PrimitiveType.Cylinder, "Post", globe, new Vector3(0f, 0.5f, 0f), new Vector3(0.05f, 0.5f, 0.05f), dark, false);
            var globeMat = new Material(Shader.Find("Standard")) { mainTexture = WorldMapTexture, color = new Color(0.95f, 0.88f, 0.72f) };
            globeMat.SetFloat("_Glossiness", 0.35f);
            Mats.Prim(PrimitiveType.Sphere, "Ball", globe, new Vector3(0f, 1.32f, 0f), Vector3.one * 0.54f, globeMat, false, Quaternion.Euler(0f, 140f, 23f));
            Mats.Prim(PrimitiveType.Cylinder, "Meridian", globe, new Vector3(0f, 1.32f, 0f), new Vector3(0.6f, 0.004f, 0.6f), Gold, false, Quaternion.Euler(90f, 0f, 23f));

            // 태극기 (원작: 화면 오른쪽 끝, 깃대에서 세로로 늘어진 깃발)
            Mats.Prim(PrimitiveType.Cylinder, "FlagPole", t, new Vector3(2.62f, 1.1f, -0.9f), new Vector3(0.035f, 1.1f, 0.035f), Mats.Lit(new Color(0.8f, 0.7f, 0.4f), 0.6f, 0.6f), false);
            Mats.Prim(PrimitiveType.Sphere, "FlagTop", t, new Vector3(2.62f, 2.22f, -0.9f), Vector3.one * 0.06f, Gold, false);
            var flagMat = new Material(Shader.Find("Standard")) { mainTexture = Taegukgi(240, 160) };
            // 세로로 늘어진 깃발: 판을 90도 돌려 태극 문양이 세로로 보이게, 벽 쪽으로 살짝 비스듬히
            Mats.Prim(PrimitiveType.Quad, "Flag", t, new Vector3(2.38f, 1.6f, -0.92f), new Vector3(0.78f, 0.52f, 1f), flagMat, false, Quaternion.Euler(0f, 205f, 90f));

            // 매달린 갓 전등 (원작: 화면 위 가운데 왼쪽)
            Mats.Prim(PrimitiveType.Cylinder, "Cord", t, new Vector3(-1.3f, (H + 2.62f) / 2f, -1.2f), new Vector3(0.01f, (H - 2.62f) / 2f, 0.01f), dark, false);
            Mats.Prim(PrimitiveType.Sphere, "Shade", t, new Vector3(-1.3f, 2.56f, -1.2f), new Vector3(0.5f, 0.16f, 0.5f), Mats.Lit(new Color(0.85f, 0.82f, 0.75f), 0.4f), false);
            Mats.Prim(PrimitiveType.Sphere, "Bulb", t, new Vector3(-1.3f, 2.48f, -1.2f), Vector3.one * 0.12f, Mats.Emissive(new Color(1f, 0.86f, 0.62f), 2.2f), false);

            // 수족관 (원작: 낚은 물고기·산호·해초로 꾸미는 수족관). 카메라 뒤 앞벽에 붙여, 수족관 창을 열 때만 비춘다
            Tank = Aquarium.Build(t, new Vector3(-1.6f, 0f, ZF - 0.58f));
            Tank.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // 조명: 갓 전등 아래 따뜻한 빛 + 캐릭터 정면 보조광 + 선반·책장 쪽 은은한 빛 (원작: 어둑한 세피아 톤)
            AddLight(new Vector3(-1.3f, 2.35f, -1.2f), 1.7f, 6f);
            AddLight(new Vector3(0.2f, 1.9f, 0.9f), 1.25f, 4.5f);
            AddLight(new Vector3(2.6f, 2.6f, -1.3f), 1.2f, 4.5f);
            AddLight(new Vector3(-2.6f, 2.2f, -1.2f), 0.9f, 4f);

            // 가운데 캐릭터 (입은 의상 그대로)
            _room = t;
            RefreshPlayer();
        }

        static Material _gold;
        static Material Gold => _gold != null ? _gold : (_gold = Mats.Lit(new Color(0.85f, 0.65f, 0.22f), 0.75f, 0.85f));

        /// <summary>벽에 거는 액자: 어두운 테 + 그림 판 (tex가 null이면 흐린 사진 색).</summary>
        static void Framed(Transform parent, string name, Texture tex, Vector3 pos, Vector2 size, Material frame, Quaternion? tilt = null)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = tilt ?? Quaternion.identity;
            Mats.Prim(PrimitiveType.Cube, "Frame", root, new Vector3(0f, 0f, 0.02f), new Vector3(size.x + 0.08f, size.y + 0.08f, 0.04f), frame, false);
            Material pic;
            if (tex != null)
            {
                pic = new Material(Shader.Find("Standard")) { mainTexture = tex };
                pic.SetFloat("_Glossiness", 0.1f);
            }
            else pic = Mats.Lit(new Color(0.5f, 0.58f, 0.6f), 0.3f);
            Mats.Prim(PrimitiveType.Quad, "Picture", root, new Vector3(0f, 0f, 0.041f), new Vector3(size.x, size.y, 1f), pic, false, Quaternion.Euler(0f, 180f, 0f));
        }

        /// <summary>선반 위에 책을 세워 꽂는다 (x0~x1, 바닥 y, 깊이 z, 높이 범위).</summary>
        static void Books(Transform parent, System.Random rng, float x0, float x1, float y, float z, float hMin, float hMax)
        {
            for (float x = x0; x < x1;)
            {
                float w = 0.035f + (float)rng.NextDouble() * 0.035f;
                float h = hMin + (float)rng.NextDouble() * (hMax - hMin);
                var c = Color.HSVToRGB((float)rng.NextDouble(), 0.3f + (float)rng.NextDouble() * 0.3f, 0.35f + (float)rng.NextDouble() * 0.35f);
                Mats.Prim(PrimitiveType.Cube, "Book", parent, new Vector3(x + w / 2f, y + h / 2f, z), new Vector3(w, h, 0.2f), Mats.Lit(c, 0.2f), false,
                    Quaternion.Euler(0f, 0f, rng.NextDouble() < 0.1 ? 8f : 0f));
                x += w + 0.004f;
            }
        }

        /// <summary>트로피: 받침 + 기둥 + 잔.</summary>
        static void Trophy(Transform parent, Vector3 pos, float h, Material mat)
        {
            var root = new GameObject("Trophy").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            Mats.Prim(PrimitiveType.Cube, "Base", root, new Vector3(0f, h * 0.06f, 0f), new Vector3(h * 0.32f, h * 0.12f, h * 0.32f), Mats.Lit(new Color(0.15f, 0.1f, 0.06f), 0.4f), false);
            Mats.Prim(PrimitiveType.Cylinder, "Stem", root, new Vector3(0f, h * 0.35f, 0f), new Vector3(h * 0.06f, h * 0.2f, h * 0.06f), mat, false);
            Mats.Prim(PrimitiveType.Sphere, "Cup", root, new Vector3(0f, h * 0.72f, 0f), new Vector3(h * 0.36f, h * 0.4f, h * 0.36f), mat, false);
            foreach (int s in new[] { -1, 1 })
                Mats.Prim(PrimitiveType.Sphere, "Handle", root, new Vector3(s * h * 0.2f, h * 0.75f, 0f), new Vector3(h * 0.06f, h * 0.2f, h * 0.06f), mat, false);
        }

        /// <summary>앉은 고양이 장식: 몸 + 머리 + 귀 둘.</summary>
        static void Cat(Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject("Cat").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            Mats.Prim(PrimitiveType.Sphere, "Body", root, new Vector3(0f, 0.1f, 0f), new Vector3(0.13f, 0.2f, 0.12f), mat, false);
            Mats.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 0.23f, 0.02f), new Vector3(0.1f, 0.09f, 0.09f), mat, false);
            foreach (int s in new[] { -1, 1 })
                Mats.Prim(PrimitiveType.Cube, "Ear", root, new Vector3(s * 0.03f, 0.28f, 0.02f), new Vector3(0.03f, 0.04f, 0.015f), mat, false, Quaternion.Euler(0f, 0f, s * -20f));
        }

        /// <summary>스피닝 릴: 스풀 + 몸통 + 손잡이 (색은 제각각).</summary>
        static void Reel(Transform parent, Vector3 pos, System.Random rng)
        {
            var root = new GameObject("Reel").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 40f - 20f, 0f);
            var body = Mats.Lit(Color.HSVToRGB((float)rng.NextDouble(), 0.5f, 0.45f), 0.6f, 0.5f);
            var silver = Mats.Lit(new Color(0.72f, 0.74f, 0.78f), 0.75f, 0.8f);
            Mats.Prim(PrimitiveType.Cube, "Foot", root, new Vector3(0f, 0.01f, 0f), new Vector3(0.04f, 0.02f, 0.12f), silver, false);
            Mats.Prim(PrimitiveType.Cylinder, "Body", root, new Vector3(0f, 0.09f, 0f), new Vector3(0.09f, 0.04f, 0.09f), body, false, Quaternion.Euler(0f, 0f, 90f));
            Mats.Prim(PrimitiveType.Cylinder, "Spool", root, new Vector3(0f, 0.09f, 0.07f), new Vector3(0.07f, 0.04f, 0.07f), silver, false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Handle", root, new Vector3(0.06f, 0.12f, 0f), new Vector3(0.01f, 0.08f, 0.01f), silver, false);
            Mats.Prim(PrimitiveType.Sphere, "Knob", root, new Vector3(0.07f, 0.16f, 0f), Vector3.one * 0.025f, body, false);
        }

        /// <summary>살대 의자 (원작 카페 의자): 앉는 판 + 다리 넷 + 등받이 기둥 둘·윗살·세로 살. yaw 0이면 카메라(+z) 쪽을 본다.</summary>
        static void Chair(Transform parent, Vector3 pos, float yaw, Material mat)
        {
            var root = new GameObject("Chair").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Mats.Prim(PrimitiveType.Cube, "Seat", root, new Vector3(0f, 0.46f, 0f), new Vector3(0.42f, 0.035f, 0.4f), mat, false);
            foreach (float x in new[] { -0.18f, 0.18f })
            foreach (float z in new[] { -0.17f, 0.17f })
                Mats.Prim(PrimitiveType.Cylinder, "Leg", root, new Vector3(x, 0.23f, z), new Vector3(0.035f, 0.23f, 0.035f), mat, false);
            foreach (float x in new[] { -0.18f, 0.18f })
                Mats.Prim(PrimitiveType.Cylinder, "Post", root, new Vector3(x, 0.72f, -0.18f), new Vector3(0.035f, 0.27f, 0.035f), mat, false);
            Mats.Prim(PrimitiveType.Cube, "TopRail", root, new Vector3(0f, 0.97f, -0.18f), new Vector3(0.44f, 0.09f, 0.03f), mat, false);
            for (int i = 0; i < 4; i++)
                Mats.Prim(PrimitiveType.Cylinder, "Spindle", root, new Vector3(-0.105f + i * 0.07f, 0.71f, -0.18f), new Vector3(0.018f, 0.22f, 0.018f), mat, false);
        }

        Transform _room;
        NpcAngler _me;

        /// <summary>내 캐릭터를 다시 세운다 (의상을 바꿨을 때).</summary>
        public void RefreshPlayer()
        {
            if (_me != null) Destroy(_me.gameObject);
            var look = Game.Profile != null ? Game.Profile.BuildLook() : CharacterLook.Player;
            // 채널에 들어가기 전이라 낚싯대 없이 맨손으로 서 있는다 (2026-10-21 사용자 요청)
            _me = NpcAngler.Spawn(_room, new Vector3(0.2f, 0f, -0.6f), 0f, look, NpcAngler.Role.Stand, showNameTag: false);
            _me.Idle = true;
        }


        void MountFish(Transform parent, string speciesId, float length, Vector3 pos)
        {
            var species = FishDatabase.Get(speciesId);
            Mats.Prim(PrimitiveType.Cube, "Plaque", parent, pos + new Vector3(0f, 0f, -0.06f), new Vector3(length / 100f + 0.25f, 0.42f, 0.04f),
                Mats.Lit(WoodDark, 0.4f), false);
            var holder = new GameObject("Trophy_" + speciesId).transform;
            holder.SetParent(parent, false);
            holder.localPosition = pos;
            holder.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var fish = FishModels.Create(species, length, holder);
            FishModels.Freeze(fish, FishModels.Flop, 0.15f); // 박제: 몸을 살짝 튼 자세
        }

        void AddLight(Vector3 local, float intensity, float range)
        {
            var go = new GameObject("RoomLight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.85f, 0.65f);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        /// <summary>태극기 (대략적인 태극 문양 + 4괘).</summary>
        static Texture2D Taegukgi(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var red = new Color(0.8f, 0.1f, 0.18f);
            var blue = new Color(0f, 0.2f, 0.55f);
            float r = h * 0.25f;
            var center = new Vector2(w / 2f, h / 2f);
            float ang = -33.7f * Mathf.Deg2Rad;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x, y) - center;
                var q = new Vector2(p.x * Mathf.Cos(ang) - p.y * Mathf.Sin(ang), p.x * Mathf.Sin(ang) + p.y * Mathf.Cos(ang));
                var c = Color.white;
                if (q.magnitude < r)
                {
                    bool upper = q.y > 0f;
                    bool leftSmall = (q - new Vector2(-r / 2f, 0f)).magnitude < r / 2f;
                    bool rightSmall = (q - new Vector2(r / 2f, 0f)).magnitude < r / 2f;
                    bool isRed = (upper && !rightSmall) || leftSmall;
                    c = isRed ? red : blue;
                }
                else
                {
                    // 4괘: 모서리 대각선 방향의 검은 막대 세 줄
                    for (int k = 0; k < 4; k++)
                    {
                        float sx = k % 2 == 0 ? -1f : 1f, sy = k < 2 ? 1f : -1f;
                        var gc = center + new Vector2(sx * r * 2.05f, sy * r * 1.35f);
                        var d = new Vector2(x, y) - gc;
                        float a = Mathf.Atan2(sy, sx);
                        var local = new Vector2(d.x * Mathf.Cos(-a) - d.y * Mathf.Sin(-a), d.x * Mathf.Sin(-a) + d.y * Mathf.Cos(-a));
                        if (Mathf.Abs(local.y) < r * 0.5f && Mathf.Abs(local.x) < r * 0.36f)
                        {
                            float band = (local.x + r * 0.36f) / (r * 0.72f) * 5f;
                            if ((int)band % 2 == 0) c = Color.black;
                        }
                    }
                }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        // ---- 대기실 그림 텍스처 (코드로 그린다) ----
        static Texture2D _brick, _rug, _painting, _worldMap;

        static Texture2D NewTex(int w, int h, bool mips = true) =>
            new Texture2D(w, h, TextureFormat.RGBA32, mips) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };

        static float Hash(int x, int y) => Mathf.Abs(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f) % 1f;

        /// <summary>
        /// 갈색 벽돌 (원작 대기실 뒷벽): 가로 0.9m × 세로 0.6m 한 장에 벽돌 4장 × 8줄, 줄마다 반 장씩 어긋나고
        /// 장마다 밝기·붉은 기가 조금씩 다르다. 줄눈은 어두운 회갈색.
        /// </summary>
        static Texture2D BrickTexture
        {
            get
            {
                if (_brick != null) return _brick;
                const int w = 256, h = 192, rows = 8, cols = 4;
                var tex = NewTex(w, h);
                var px = new Color32[w * h];
                var mortar = new Color(0.2f, 0.16f, 0.13f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fy = (float)y / h * rows;
                    int row = (int)fy;
                    float fx = (float)x / w * cols + (row % 2 == 0 ? 0f : 0.5f);
                    int col = (int)Mathf.Floor(fx) % cols;
                    float ux = fx - Mathf.Floor(fx), uy = fy - row;
                    // 줄눈 두께: 세로 줄눈 2.5%, 가로 줄눈 9%
                    bool joint = ux < 0.025f || ux > 0.975f || uy < 0.09f;
                    float n = Hash(col, row);
                    var b = Color.Lerp(new Color(0.5f, 0.33f, 0.24f), new Color(0.66f, 0.47f, 0.34f), n);
                    if (Hash(col + 7, row + 3) > 0.82f) b *= 0.78f; // 그을린 벽돌
                    float grain = 0.92f + 0.16f * Hash(x / 3, y / 3);
                    var c = joint ? mortar : b * grain;
                    c.a = 1f;
                    px[y * w + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _brick = tex;
            }
        }

        /// <summary>
        /// 페르시아 양탄자 (원작: 캐릭터 발밑): 짙은 적갈색 바깥 테두리 + 잔무늬 띠, 베이지 바탕, 가운데 붉은 마름모 메달리온과 네 귀퉁이 무늬.
        /// </summary>
        static Texture2D RugTexture
        {
            get
            {
                if (_rug != null) return _rug;
                const int w = 320, h = 200;
                var tex = NewTex(w, h);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color32[w * h];
                var red = new Color(0.42f, 0.12f, 0.09f);
                var deep = new Color(0.2f, 0.07f, 0.06f);
                var beige = new Color(0.76f, 0.64f, 0.48f);
                var navy = new Color(0.16f, 0.17f, 0.24f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // 가장자리까지 거리(px)
                    int e = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
                    Color c;
                    if (e < 4) c = deep;
                    else if (e < 22)
                    {
                        // 테두리 띠: 반복 꽃무늬 (사인 격자)
                        float along = (x < 22 || x > w - 23) ? y : x;
                        float m = Mathf.Sin(along * 0.45f) * Mathf.Sin((e - 4) * 0.35f);
                        c = m > 0.25f ? beige * 0.85f : red;
                    }
                    else if (e < 26) c = deep;
                    else
                    {
                        // 가운데 메달리온: 마름모 거리
                        float dm = Mathf.Abs(u) * 1.15f + Mathf.Abs(v) * 1.6f;
                        float ring = Mathf.Sin(dm * 26f);
                        if (dm < 0.18f) c = navy;
                        else if (dm < 0.62f) c = ring > 0.55f ? beige * 0.9f : red;
                        else if (dm < 0.68f) c = deep;
                        else
                        {
                            c = beige;
                            // 바탕 잔무늬 + 네 귀퉁이 붉은 무늬
                            float corner = Mathf.Min(Vector2.Distance(new Vector2(Mathf.Abs(u), Mathf.Abs(v)), new Vector2(0.86f, 0.8f)), 1f);
                            if (corner < 0.14f) c = Mathf.Sin(corner * 60f) > 0f ? red : deep;
                            else if (Mathf.Sin(x * 0.6f) * Mathf.Sin(y * 0.6f) > 0.85f) c = red * 1.2f;
                        }
                    }
                    // 세월에 바랜 얼룩
                    c *= 0.88f + 0.12f * Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _rug = tex;
            }
        }

        /// <summary>
        /// 잉어 수묵화 족자 (원작: 왼쪽 벽 흰 종이에 먹으로 그린 잉어 + 오른쪽 세로 글씨): 미색 종이, 위·아래 갈색 족자 띠,
        /// 먹색 잉어 한 마리(몸통 타원 + 꼬리 + 지느러미, 비늘 결), 오른쪽 위 세로 획 몇 줄과 붉은 낙관.
        /// </summary>
        static Texture2D PaintingTexture
        {
            get
            {
                if (_painting != null) return _painting;
                const int w = 180, h = 245;
                var tex = NewTex(w, h);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color32[w * h];
                var paper = new Color(0.9f, 0.87f, 0.8f);
                var ink = new Color(0.12f, 0.12f, 0.13f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = paper * (0.94f + 0.06f * Mathf.PerlinNoise(x * 0.08f, y * 0.08f));
                    if (y < 14 || y > h - 15) c = new Color(0.36f, 0.25f, 0.15f);
                    else
                    {
                        // 잉어: 왼쪽 아래에서 오른쪽 위로 비스듬히 (머리 오른쪽)
                        var p = new Vector2(x - 78f, y - 112f);
                        float a = 18f * Mathf.Deg2Rad;
                        var q = new Vector2(p.x * Mathf.Cos(a) + p.y * Mathf.Sin(a), -p.x * Mathf.Sin(a) + p.y * Mathf.Cos(a));
                        float body = (q.x / 52f) * (q.x / 52f) + (q.y / 20f) * (q.y / 20f);
                        // 꼬리: 몸통 왼쪽 끝에서 벌어지는 삼각형
                        float tx = -q.x - 46f;
                        bool tail = tx > 0f && tx < 26f && Mathf.Abs(q.y) < tx * 0.75f + 2f;
                        bool dorsal = q.y > 14f && q.y < 14f + (22f - Mathf.Abs(q.x + 6f)) * 0.5f && Mathf.Abs(q.x + 6f) < 22f;
                        if (body < 1f)
                        {
                            float shade = 0.35f + 0.5f * Mathf.Clamp01(1f - body) * (q.y > 0 ? 1f : 0.55f);
                            float scale = Mathf.Sin(q.x * 0.55f + Mathf.Abs(q.y) * 0.3f) > 0.6f ? 0.15f : 0f;
                            c = Color.Lerp(paper, ink, Mathf.Clamp01(shade + scale));
                            if (q.x > 38f && q.x < 43f && q.y > 2f && q.y < 7f) c = paper; // 눈
                        }
                        else if (tail || dorsal) c = Color.Lerp(paper, ink, 0.55f);
                        // 세로 글씨: 오른쪽 위 세 줄
                        for (int k = 0; k < 3; k++)
                        {
                            float cx0 = w - 22f - k * 14f;
                            if (Mathf.Abs(x - cx0) < 3.2f && y > h - 30 - k * 8 - 120 && y < h - 30 - k * 8 && Mathf.Sin(y * 0.55f + k) > -0.3f) c = ink;
                        }
                        // 붉은 낙관
                        if (x > 22 && x < 34 && y > 28 && y < 40) c = new Color(0.7f, 0.15f, 0.12f);
                    }
                    c.a = 1f;
                    px[y * w + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _painting = tex;
            }
        }

        /// <summary>
        /// 세계지도 (원작: 사진을 꽂아 둔 벽 지도, 지구본에도 쓴다): 청록 바다 + 위도·경도선, 대륙은 타원 덩어리에 해안선을 흔들어 그린다.
        /// </summary>
        static Texture2D WorldMapTexture
        {
            get
            {
                if (_worldMap != null) return _worldMap;
                const int w = 256, h = 128;
                var tex = NewTex(w, h);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color32[w * h];
                // (가운데 u, v, 반지름 u, v) — 경도 -180~180 → u 0~1, 위도 -90~90 → v 0~1
                var lands = new[]
                {
                    new Vector4(0.2f, 0.72f, 0.1f, 0.13f), new Vector4(0.27f, 0.6f, 0.05f, 0.06f), // 북미
                    new Vector4(0.33f, 0.36f, 0.05f, 0.14f), new Vector4(0.3f, 0.47f, 0.04f, 0.05f), // 남미
                    new Vector4(0.39f, 0.88f, 0.04f, 0.06f), // 그린란드
                    new Vector4(0.52f, 0.76f, 0.05f, 0.06f), // 유럽
                    new Vector4(0.54f, 0.48f, 0.07f, 0.14f), new Vector4(0.58f, 0.58f, 0.06f, 0.05f), // 아프리카
                    new Vector4(0.7f, 0.74f, 0.15f, 0.11f), new Vector4(0.73f, 0.6f, 0.06f, 0.07f), new Vector4(0.8f, 0.6f, 0.04f, 0.06f), // 아시아·인도·동남아
                    new Vector4(0.86f, 0.32f, 0.06f, 0.05f), // 호주
                };
                var sea = new Color(0.46f, 0.66f, 0.66f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float wobble = (Mathf.PerlinNoise(u * 14f, v * 14f) - 0.5f) * 0.45f;
                    bool land = false;
                    foreach (var l in lands)
                    {
                        float du = (u - l.x) / l.z, dv = (v - l.y) / l.w;
                        if (du * du + dv * dv < 1f + wobble) { land = true; break; }
                    }
                    Color c = land ? Color.Lerp(new Color(0.83f, 0.72f, 0.48f), new Color(0.62f, 0.66f, 0.42f), Mathf.PerlinNoise(u * 9f, v * 9f)) : sea;
                    if (!land && (x % 21 == 0 || y % 21 == 0)) c *= 0.88f; // 위도·경도선
                    if (x < 2 || x > w - 3 || y < 2 || y > h - 3) c = new Color(0.9f, 0.88f, 0.8f); // 흰 테두리
                    c.a = 1f;
                    px[y * w + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _worldMap = tex;
            }
        }
    }
}
