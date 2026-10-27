using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.UI;
using TMPro;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 바닷가 낚시점 (모든 맵이 같이 쓴다: 해안 맵 틀·서천·홍현·한강). 예전엔 큐브 몸통·지붕 판·줄무늬 차양 판·카운터 큐브였다.
    /// 한국 포구 낚시점 사진대로 (<see cref="HouseKit"/> 부품):
    /// - 단층 미장 건물, 앞은 통유리 알루미늄 새시 다섯 칸(아래 걸레받이 판·위 가로대 위 고정창), 문 칸은 미닫이 두 짝을 옆 유리 뒤로 밀어 열어 둬 가게 안이 보인다
    /// - 가게 안: 안쪽 벽 루어 벽(Blender), 낚싯대 거치대, 계산대, 천장 형광등
    /// - 앞 위 가로 간판(테두리 LED 줄, 가게 이름 + 생미끼·채비·얼음), 옆벽 돌출 간판 "낚시"
    /// - 가게 앞 철골 캐노피(앞으로 낮아지는 골강판, 각관 기둥·보·서까래, 물받이), 밑에 크릴 냉동 쇼케이스 둘(주인 앞 카운터 노릇)·음료 냉장고·낚싯대 거치대·
    ///   집어제 포대 팔레트·밑밥통·의자, 옆에 자판기, 앞에 깃발 배너 둘
    /// - 뒤로 낮아지는 골강판 지붕, 옆 창·포스터, 실외기·LPG, 뒷문, 홈통. 바닥은 균열·얼룩 콘크리트 마당
    /// 로컬: 원점 = 앞마당 바닥 가운데 땅, +Z = 앞(길·바다). 건물 앞면 z = 2.25 (예전과 같다 — 주인 NPC 자리 (1.2, 2.8)).
    /// </summary>
    public static class TackleShopKit
    {
        const float W = 7.4f, D = 5f, FrontZ = 2.25f, T = 0.22f;
        const float FrontTop = 3.3f, BackTop = 2.9f;          // 앞벽 위 / 뒤로 낮아지는 지붕
        const float PadTop = 0.06f, FloorY = 0.12f;
        const float StoreX = 0f, StoreW = 6.2f, StoreY = 0.12f, StoreH = 2.28f;
        const int Bays = 5, DoorBay = 3;                      // 문 칸 가운데 x = 1.24 (주인이 그 앞에 선다)
        const float CanopyD = 3.25f, CanopyHi = 2.6f, CanopyLo = 2.38f;
        const float SignBottom = 2.7f, SignH = 1.2f, SignD = 0.34f;

        /// <summary>낚시점 하나를 짓고 주인 NPC(NpcId "shop")를 세운다. accent = 간판·캐노피 색 (맵마다 다른 지붕 색을 이어받는다).</summary>
        public static NpcAngler Build(Transform parent, Vector3 pos, float yaw, string sign, CharacterLook owner, Color accent, List<Renderer> bulbs)
        {
            var shop = new GameObject("TackleShop").transform;
            shop.SetParent(parent, false);
            shop.localPosition = pos;
            shop.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var rng = new System.Random(Mathf.RoundToInt(pos.x * 97f + pos.z * 53f) ^ sign.Length);

            Yard(shop);
            Building(shop, accent, rng);
            Canopy(shop, accent, bulbs);
            Signs(shop, sign, accent);
            FrontGoods(shop, rng);
            Flags(shop);

            // 처마 조명: 밤에도 냉동고와 주인이 보이게
            var light = new GameObject("ShopLight").AddComponent<Light>();
            light.transform.SetParent(shop, false);
            light.transform.localPosition = new Vector3(0.6f, 2.2f, 3.6f);
            light.type = LightType.Point;
            light.range = 7.5f;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.88f, 0.68f);
            light.shadows = LightShadows.None;

            var npc = NpcAngler.Spawn(shop, new Vector3(1.24f, PadTop, 2.8f), 0f, owner, NpcAngler.Role.Talker);
            npc.NpcId = "shop";
            return npc;
        }

        // ---------------- 앞마당 ----------------

        /// <summary>콘크리트 마당: 건물 밑까지 깔고 비탈에서도 뜨지 않게 1m 아래까지 (금·얼룩·물웅덩이는 Mats.Quay).</summary>
        static void Yard(Transform shop)
        {
            var b = new BlockMesh();
            var c = Color.white;
            b.Block(new Vector3(0.4f, PadTop - 0.55f, 1.6f), new Vector3(W + 2.6f, 1.1f, D + CanopyD + 1.6f), 0.03f, Quaternion.identity, c, c * 0.6f);
            b.Emit(shop, "Yard", Mats.VertexAO(Mats.Quay(new Color(0.62f, 0.61f, 0.58f))));
        }

        // ---------------- 건물 ----------------

        static void Building(Transform shop, Color accent, System.Random rng)
        {
            var rig = HouseKit.Rig(shop, new Vector3(0f, 0f, FrontZ - D * 0.5f), 0f, "Building");
            var p = new HouseKit.Parts
            {
                WallMat = Mats.Wall(new Color(0.93f, 0.91f, 0.86f)),
                TrimMat = Mats.Weathered("concrete", new Color(0.6f, 0.6f, 0.58f), 2f, 0.1f, 0f, 0.4f, 0.3f, null, 0.7f, 0f, "wallconcrete"),
                FrameMat = Mats.Lit(new Color(0.78f, 0.79f, 0.8f), 0.6f, 0.55f),      // 알루미늄 새시
                GlassMat = Mats.Lit(new Color(0.15f, 0.2f, 0.25f), 0.93f),
                Glass2Mat = Mats.Lit(new Color(0.3f, 0.32f, 0.33f), 0.85f),
                DoorMat = Mats.Lit(new Color(0.42f, 0.44f, 0.46f), 0.4f, 0.4f),
                RoofMat = Mats.Weathered("rust", new Color(0.5f, 0.54f, 0.57f), 1.6f, 0.4f, 0f, 0.4f, 0.04f, null, 0.75f, 0.25f, null, 0.35f),
                UnderMat = Mats.Wall(new Color(0.85f, 0.85f, 0.83f)),
                MetalMat = Mats.Lit(new Color(0.2f, 0.21f, 0.22f), 0.4f, 0.4f),
                AccentMat = Mats.Weathered("concrete", new Color(0.66f, 0.67f, 0.66f), 2f, 0.3f, 0f, 0.2f, 0.05f, null, 0.8f), // 가게 안 바닥 (매끈한 칠)
                PipeMat = Mats.Lit(new Color(0.76f, 0.76f, 0.73f), 0.4f),
            };
            var shade = HouseKit.WallShade(0f, BackTop);
            var gray = new Color(0.85f, 0.85f, 0.85f);
            // 기단 (걸레받이 띠) + 가게 안 바닥
            p.Trim.Block(new Vector3(0f, FloorY - 0.75f, 0f), new Vector3(W + 0.06f, 1.5f, D + 0.06f), 0.02f, Quaternion.identity, gray, gray * 0.6f);
            p.Accent.Face(new Vector3(-W * 0.5f + T, FloorY + 0.003f, -D * 0.5f + T), new Vector3(-W * 0.5f + T, FloorY + 0.003f, D * 0.5f - 0.05f),
                new Vector3(W * 0.5f - T, FloorY + 0.003f, D * 0.5f - 0.05f), new Vector3(W * 0.5f - T, FloorY + 0.003f, -D * 0.5f + T), Vector3.up, Color.white);

            // 네 벽: 앞은 통유리 자리, 오른쪽(+x)은 포스터 붙은 창, 왼쪽은 작은 창, 뒤는 뒷문 + 작은 창
            var front = new List<HouseKit.Opening> { new HouseKit.Opening(StoreX, StoreY, StoreW, StoreH, HouseKit.Hole.Shop) };
            var right = new List<HouseKit.Opening> { new HouseKit.Opening(0.3f, 1.0f, 1.5f, 1.1f) };
            var left = new List<HouseKit.Opening> { new HouseKit.Opening(-0.6f, 1.5f, 0.8f, 0.6f) };
            var back = new List<HouseKit.Opening> { new HouseKit.Opening(-2.2f, FloorY, 0.9f, 2.05f, HouseKit.Hole.Door), new HouseKit.Opening(1.4f, 1.4f, 1f, 0.8f) };
            var fq = Quaternion.identity;
            var bq = Quaternion.Euler(0f, 180f, 0f);
            var rq = Quaternion.Euler(0f, 90f, 0f);
            var lq = Quaternion.Euler(0f, -90f, 0f);
            var fo = new Vector3(0f, 0f, D * 0.5f);
            var bo = new Vector3(0f, 0f, -D * 0.5f);
            var ro = new Vector3(W * 0.5f, 0f, 0f);
            var lo = new Vector3(-W * 0.5f, 0f, 0f);
            HouseKit.Wall(p.Wall, fo, fq, W, -0.3f, FrontTop, T, front, shade);
            HouseKit.Wall(p.Wall, bo, bq, W, -0.3f, BackTop, T, back, shade);
            HouseKit.Wall(p.Wall, ro, rq, D - T * 2f, -0.3f, BackTop, T, right, shade);
            HouseKit.Wall(p.Wall, lo, lq, D - T * 2f, -0.3f, BackTop, T, left, shade);
            foreach (var o in back) HouseKit.Fill(p, bo, bq, o, T, rng);
            HouseKit.Fill(p, ro, rq, right[0], T, rng);
            HouseKit.Fill(p, lo, lq, left[0], T, rng);
            var stickers = new BlockMesh();
            Storefront(p, stickers, fo);
            // 뒤로 낮아지는 골강판 지붕 (앞은 간판이 가린다), 옆벽 위 사다리꼴
            HouseKit.ShedRoof(p, W, D, FrontTop, BackTop, 0.25f, p.Wall, T);
            // 뒤 모서리 홈통
            p.Pipes.Add(new[] { new Vector3(W * 0.5f + 0.1f, BackTop - 0.05f, -D * 0.5f - 0.33f), new Vector3(W * 0.5f + 0.1f, BackTop - 0.3f, -D * 0.5f - 0.08f), new Vector3(W * 0.5f + 0.1f, 0.15f, -D * 0.5f - 0.08f) });
            // 가게 안: 형광등 둘 (낮에도 켜 둔다), 계산대
            var tubes = new BlockMesh();
            foreach (float z in new[] { -1.2f, 0.9f })
                tubes.Block(new Vector3(0.6f, 2.95f, z), new Vector3(1.25f, 0.05f, 0.1f), 0.01f, Quaternion.identity, Color.white, Color.white);
            tubes.Emit(rig, "Tubes", Mats.Emissive(new Color(0.95f, 0.97f, 1f), 1.3f));
            // 형광등 빛: 지붕 그늘에 가려 열린 문 칸 안이 새까맸다 → 가게 안을 밝힌다 (루어 벽·계산대가 보이게)
            var inner = new GameObject("InteriorLight").AddComponent<Light>();
            inner.transform.SetParent(rig, false);
            inner.transform.localPosition = new Vector3(0.9f, 2.6f, -0.5f);
            inner.type = LightType.Point;
            inner.range = 5.5f;
            inner.intensity = 1.5f;
            inner.color = new Color(0.93f, 0.97f, 1f);
            inner.shadows = LightShadows.None;
            p.Door.Block(new Vector3(2.75f, FloorY + 0.5f, -0.2f), new Vector3(0.6f, 1f, 1.6f), 0.01f, Quaternion.identity, Color.white, Color.white * 0.8f);
            p.Glass.Block(new Vector3(2.75f, FloorY + 1.01f, -0.2f), new Vector3(0.62f, 0.02f, 1.62f), 0f, Quaternion.identity, Color.white, Color.white);
            p.Metal.Block(new Vector3(2.8f, FloorY + 1.15f, -0.5f), new Vector3(0.35f, 0.25f, 0.3f), 0.02f, Quaternion.Euler(0f, -20f, 0f), Color.white, Color.white);
            p.Emit(rig);
            stickers.Emit(rig, "Stickers", Mats.Lit(new Color(0.98f, 0.86f, 0.22f), 0.3f));

            KenneyProps.Spawn("Props/shop_lurewall", rig, new Vector3(0.9f, FloorY, -D * 0.5f + T + 0.02f), 0f, 0f, 0f, false);
            KenneyProps.Spawn("Props/shop_rodrack", rig, new Vector3(-2.3f, FloorY, -D * 0.5f + T + 0.25f), 0f, 2.55f, 0f, false);
            KenneyProps.Spawn("Props/shop_rodrack", rig, new Vector3(-W * 0.5f + T + 0.25f, FloorY, -0.2f), 90f, 2.5f, 0f, false);
            // 바깥 살림: 왼쪽 벽 실외기·LPG
            KenneyProps.Spawn("Props/house_ac", rig, new Vector3(-W * 0.5f - 0.2f, PadTop, 0.9f), -90f, 0f, 0f, false);
            KenneyProps.Spawn("Props/house_lpg", rig, new Vector3(-W * 0.5f - 0.3f, PadTop, -1.4f), -90f, 0f, 0f, false);

            // 오른쪽 창 포스터 (유리에 붙인 글자, 창 가운데 = 리그 z -0.3)
            Text(rig, "선상낚시\n예약", new Vector3(W * 0.5f - 0.1f, 1.6f, -0.3f), new Color(0.95f, 0.85f, 0.25f), 1.6f, 90f + 180f);
            var col = rig.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.6f, 0f);
            col.size = new Vector3(W, 3.4f, D);
        }

        /// <summary>
        /// 앞 통유리 (알루미늄 새시): 바깥 테 + 다섯 칸 세로 살 + 가로대(위는 고정창) + 아래 걸레받이 판, 칸마다 유리.
        /// 문 칸은 미닫이 두 짝을 양옆 유리 뒤로 밀어 열어 뒀다 (가게 안이 보인다). 유리에 붙인 글자 띠 두 장.
        /// </summary>
        static void Storefront(HouseKit.Parts p, BlockMesh stickers, Vector3 o)
        {
            var w = Color.white;
            Vector3 L(float x, float y, float z) => o + new Vector3(x, y, z);
            const float f = 0.06f, z = -0.07f, transom = 2.0f;
            float x0 = StoreX - StoreW * 0.5f, bw = StoreW / Bays;
            // 바깥 테
            p.Frame.Block(L(StoreX, StoreY + StoreH - f * 0.5f, z), new Vector3(StoreW, f, 0.1f), 0.005f, Quaternion.identity, w, w);
            p.Frame.Block(L(x0 + f * 0.5f, StoreY + StoreH * 0.5f, z), new Vector3(f, StoreH, 0.1f), 0.005f, Quaternion.identity, w, w);
            p.Frame.Block(L(-x0 - f * 0.5f, StoreY + StoreH * 0.5f, z), new Vector3(f, StoreH, 0.1f), 0.005f, Quaternion.identity, w, w);
            // 가로대 (문 칸 위로도 이어진다)
            p.Frame.Block(L(StoreX, StoreY + transom, z), new Vector3(StoreW, f * 1.4f, 0.1f), 0.005f, Quaternion.identity, w, w);
            float gTop = StoreY + StoreH - f, gMid = StoreY + transom + f * 0.7f;
            for (int i = 0; i < Bays; i++)
            {
                float cx = x0 + bw * (i + 0.5f);
                if (i > 0) p.Frame.Block(L(x0 + bw * i, StoreY + StoreH * 0.5f, z), new Vector3(f, StoreH, 0.1f), 0.005f, Quaternion.identity, w, w);
                // 위 고정창
                p.Glass.Block(L(cx, (gTop + gMid) * 0.5f, z - 0.01f), new Vector3(bw - f, gTop - gMid, 0.012f), 0f, Quaternion.identity, w, w);
                if (i == DoorBay)
                {
                    // 문 칸: 문턱 레일, 미닫이 두 짝은 양옆 유리 뒤(안쪽)로 밀었다
                    p.Metal.Block(L(cx, StoreY + 0.01f, z), new Vector3(bw - f, 0.02f, 0.14f), 0f, Quaternion.identity, w, w);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        float lx = cx + s * (bw - 0.2f), lz = z - 0.09f - (s > 0f ? 0.035f : 0f);
                        float lh = transom - 0.05f;
                        p.Frame.Block(L(lx, StoreY + lh * 0.5f, lz), new Vector3(bw - f, lh, 0.035f), 0f, Quaternion.identity, w, w);
                        p.Glass2.Block(L(lx, StoreY + 0.15f + (lh - 0.3f) * 0.5f, lz + 0.02f), new Vector3(bw - f - 0.12f, lh - 0.3f, 0.01f), 0f, Quaternion.identity, w, w);
                    }
                    continue;
                }
                // 걸레받이 판 + 유리
                p.Frame.Block(L(cx, StoreY + 0.17f, z), new Vector3(bw - f, 0.34f, 0.06f), 0.005f, Quaternion.identity, w, w);
                float g0 = StoreY + 0.34f, g1 = StoreY + transom - f * 0.7f;
                p.Glass.Block(L(cx, (g0 + g1) * 0.5f, z - 0.01f), new Vector3(bw - f, g1 - g0, 0.012f), 0f, Quaternion.identity, w, w);
            }
            // 유리 띠 스티커 (빨강·노랑 띠, 글자는 Signs가 붙인다)
            float sy = StoreY + 1.62f;
            for (int i = 0; i < Bays; i++)
            {
                if (i == DoorBay) continue;
                float cx = x0 + bw * (i + 0.5f);
                stickers.Block(L(cx, sy, z + 0.002f), new Vector3(bw - f - 0.08f, 0.26f, 0.004f), 0f, Quaternion.identity, w, w);
            }
        }

        // ---------------- 캐노피 ----------------

        /// <summary>
        /// 가게 앞 철골 캐노피: 벽(간판 밑)에서 앞으로 낮아지는 골강판(가게 색), 각관 기둥 둘·앞 보·옆 보·서까래, 앞 물받이·홈통, 밑에 형광등 (밤에 켜진다).
        /// </summary>
        static void Canopy(Transform shop, Color accent, List<Renderer> bulbs)
        {
            float w = W + 0.4f;
            var rig = HouseKit.Rig(shop, new Vector3(0f, 0f, FrontZ + CanopyD * 0.5f), 180f, "Canopy"); // 리그 +z = 벽 쪽
            var p = new HouseKit.Parts
            {
                RoofMat = Mats.Weathered("rust", accent, 1.6f, 0.4f, 0f, 0.35f, 0.03f, null, 0.8f, 0.2f, null, 0.35f),
                UnderMat = Mats.Lit(Color.Lerp(accent, Color.white, 0.55f) * 0.85f, 0.3f),
                TrimMat = Mats.Lit(new Color(0.82f, 0.83f, 0.84f), 0.4f, 0.3f),
                MetalMat = Mats.Lit(new Color(0.3f, 0.32f, 0.34f), 0.45f, 0.5f),
                PipeMat = Mats.Lit(new Color(0.82f, 0.83f, 0.84f), 0.4f, 0.3f),
            };
            p.WallMat = p.TrimMat;
            HouseKit.ShedRoof(p, w, CanopyD, CanopyHi, CanopyLo, 0.12f, null, 0.1f, 0.06f);
            var wc = Color.white;
            float hz = CanopyD * 0.5f, k = (CanopyHi - CanopyLo) / CanopyD;
            float Y(float zz) => CanopyHi - k * (hz - zz);
            // 기둥 (앞 끝 안쪽), 밑판
            foreach (float sx in new[] { -1f, 1f })
            {
                var c = new Vector3(sx * (w * 0.5f - 0.15f), 0f, -hz + 0.12f);
                p.Metal.Block(c + Vector3.up * (CanopyLo * 0.5f + PadTop * 0.5f), new Vector3(0.1f, CanopyLo - PadTop, 0.1f), 0.008f, Quaternion.identity, wc, wc);
                p.Metal.Block(c + Vector3.up * (PadTop + 0.01f), new Vector3(0.22f, 0.02f, 0.22f), 0.004f, Quaternion.identity, wc, wc);
                // 옆 보 (벽까지 비스듬히)
                var a = new Vector3(c.x, Y(-hz + 0.12f) - 0.06f, -hz + 0.12f);
                var b = new Vector3(c.x, Y(hz) - 0.06f, hz);
                p.Metal.Block((a + b) * 0.5f, new Vector3(0.08f, 0.12f, (b - a).magnitude), 0.006f, Quaternion.LookRotation(b - a), wc, wc);
            }
            // 앞 보, 서까래
            p.Metal.Block(new Vector3(0f, Y(-hz + 0.12f) - 0.08f, -hz + 0.12f), new Vector3(w - 0.2f, 0.15f, 0.1f), 0.006f, Quaternion.identity, wc, wc);
            for (int i = 1; i < 5; i++)
            {
                float x = -w * 0.5f + w * i / 5f;
                var a = new Vector3(x, Y(-hz + 0.12f) - 0.045f, -hz + 0.12f);
                var b = new Vector3(x, Y(hz) - 0.045f, hz);
                p.Metal.Block((a + b) * 0.5f, new Vector3(0.05f, 0.06f, (b - a).magnitude), 0.004f, Quaternion.LookRotation(b - a), wc, wc);
            }
            // 벽에 붙인 받침 보
            p.Metal.Block(new Vector3(0f, CanopyHi - 0.06f, hz - 0.04f), new Vector3(w, 0.12f, 0.08f), 0.006f, Quaternion.identity, wc, wc);
            // 앞 모서리 홈통 (물받이에서 기둥 따라 내려온다)
            float gx = w * 0.5f - 0.1f;
            p.Pipes.Add(new[] { new Vector3(gx, CanopyLo - 0.02f, -hz - 0.2f), new Vector3(gx, CanopyLo - 0.25f, -hz + 0.05f), new Vector3(gx, 0.12f, -hz + 0.05f) });
            p.Emit(rig);
            // 형광등 둘 (밤에 켜진다)
            for (int i = 0; i < 2; i++)
            {
                var tube = Mats.Prim(PrimitiveType.Cube, "CanopyTube", rig, new Vector3(i == 0 ? -1.4f : 1.6f, Y(0.2f) - 0.12f, 0.2f), new Vector3(1.2f, 0.05f, 0.08f), Mats.Lit(new Color(0.75f, 0.72f, 0.65f)), false);
                tube.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bulbs?.Add(tube.GetComponent<Renderer>());
            }
        }

        // ---------------- 간판 ----------------

        /// <summary>
        /// 앞 위 가로 간판: 가게 색 판 + 흰 안쪽 테 + 위아래 LED 줄(은은히 빛난다), 큰 흰 글자 가게 이름 + 노란 작은 글자 줄.
        /// 옆벽 돌출 간판(흰 상자, 빨간 세로 "낚시", 양쪽에서 읽힌다). 유리 띠 글자, 냉동고 라벨.
        /// </summary>
        static void Signs(Transform shop, string sign, Color accent)
        {
            var b = new BlockMesh();
            var edge = new BlockMesh();
            var wc = Color.white;
            float zc = FrontZ + SignD * 0.5f, zf = FrontZ + SignD, cy = SignBottom + SignH * 0.5f, sw = W + 0.3f;
            b.Block(new Vector3(0f, cy, zc), new Vector3(sw, SignH, SignD), 0.02f, Quaternion.identity, wc, wc);
            // 흰 안쪽 테 (면보다 1cm 앞)
            foreach (var (c, s) in new[]
                     {
                         (new Vector3(0f, cy + SignH * 0.5f - 0.1f, zf + 0.005f), new Vector3(sw - 0.16f, 0.03f, 0.01f)),
                         (new Vector3(0f, cy - SignH * 0.5f + 0.1f, zf + 0.005f), new Vector3(sw - 0.16f, 0.03f, 0.01f)),
                         (new Vector3(-sw * 0.5f + 0.095f, cy, zf + 0.005f), new Vector3(0.03f, SignH - 0.17f, 0.01f)),
                         (new Vector3(sw * 0.5f - 0.095f, cy, zf + 0.005f), new Vector3(0.03f, SignH - 0.17f, 0.01f)),
                     })
                edge.Block(c, s, 0f, Quaternion.identity, wc, wc);
            // 위아래 LED 줄 (간판 모서리를 따라)
            var led = new BlockMesh();
            foreach (float y in new[] { cy + SignH * 0.5f + 0.012f, cy - SignH * 0.5f - 0.012f })
                led.Block(new Vector3(0f, y, zf - 0.02f), new Vector3(sw, 0.022f, 0.03f), 0f, Quaternion.identity, wc, wc);
            b.Emit(shop, "SignBox", Mats.Emissive(accent * 0.85f, 0.12f));
            edge.Emit(shop, "SignEdge", Mats.Lit(new Color(0.96f, 0.96f, 0.94f), 0.4f));
            led.Emit(shop, "SignLed", Mats.Emissive(new Color(0.95f, 0.97f, 1f), 0.9f));
            var name = Text(shop, sign, new Vector3(0f, cy + 0.14f, zf + 0.015f), Color.white, 6.2f, 180f);
            name.fontStyle = FontStyles.Bold;
            Text(shop, "바다낚시 · 생미끼 · 채비 · 얼음 · 선상낚시", new Vector3(0f, cy - 0.35f, zf + 0.015f), new Color(1f, 0.9f, 0.45f), 2.3f, 180f);

            // 옆벽 돌출 간판 (오른쪽 벽 앞쪽, 벽에서 직각으로 나와 앞뒤에서 읽힌다)
            var box = new BlockMesh();
            var face = new BlockMesh();
            var arm = new BlockMesh();
            var sc = new Vector3(W * 0.5f + 0.65f, 2.95f, FrontZ - 0.6f);
            box.Block(sc, new Vector3(0.62f, 1.5f, 0.2f), 0.03f, Quaternion.identity, wc, wc);
            foreach (float s in new[] { -1f, 1f })
                face.Block(sc + new Vector3(0f, 0f, s * 0.102f), new Vector3(0.5f, 1.38f, 0.004f), 0f, Quaternion.identity, wc, wc);
            foreach (float y in new[] { 0.38f, -0.45f })
                arm.Block(new Vector3(W * 0.5f + 0.17f, sc.y + y, sc.z), new Vector3(0.34f, 0.05f, 0.05f), 0.005f, Quaternion.identity, wc, wc);
            box.Emit(shop, "BladeSign", Mats.Emissive(new Color(0.75f, 0.12f, 0.1f), 0.15f));
            face.Emit(shop, "BladeFace", Mats.Emissive(new Color(0.97f, 0.97f, 0.95f), 0.2f));
            arm.Emit(shop, "BladeArm", Mats.Lit(new Color(0.25f, 0.26f, 0.27f), 0.4f, 0.5f));
            foreach (float s in new[] { -1f, 1f })
            {
                var t = Text(shop, "낚\n시", sc + new Vector3(0f, 0.05f, s * 0.11f), new Color(0.82f, 0.12f, 0.1f), 5.4f, s > 0f ? 180f : 0f);
                t.lineSpacing = -18f;
                t.fontStyle = FontStyles.Bold;
            }

            // 유리 띠 글자 (Storefront의 띠 위)
            string[] labels = { "생미끼", "채비 · 소품", "릴 · 낚싯대", "", "얼음 · 음료" };
            float x0 = StoreX - StoreW * 0.5f, bw = StoreW / Bays;
            for (int i = 0; i < Bays; i++)
            {
                if (labels[i].Length == 0) continue;
                Text(shop, labels[i], new Vector3(x0 + bw * (i + 0.5f), StoreY + 1.62f - 0.005f, FrontZ - 0.07f + 0.012f), new Color(0.85f, 0.15f, 0.12f), 1.45f, 180f);
            }
        }

        // ---------------- 가게 앞 살림 ----------------

        static void FrontGoods(Transform shop, System.Random rng)
        {
            float Rd(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
            // 크릴 냉동 쇼케이스 둘: 주인(문 칸 앞) 앞에 나란히 — 카운터 노릇, 부딪힌다
            string[] tags = { "냉동 크릴", "밑밥 · 집어제" };
            for (int i = 0; i < 2; i++)
            {
                float x = 0.55f + i * 1.42f;
                KenneyProps.Spawn("Props/shop_freezer", shop, new Vector3(x, PadTop, 3.9f), 0f, 0f, 0f, false);
                Text(shop, tags[i], new Vector3(x, PadTop + 0.63f, 3.9f + 0.36f + 0.012f), Color.white, 1.1f, 180f);
            }
            var col = new GameObject("FreezerCol").AddComponent<BoxCollider>();
            col.transform.SetParent(shop, false);
            col.center = new Vector3(1.26f, 0.5f, 3.9f);
            col.size = new Vector3(2.8f, 0.9f, 0.74f);
            // 왼쪽: 음료·미끼 냉장고, 낚싯대 거치대, 집어제 포대 팔레트, 밑밥통·아이스박스, 의자
            KenneyProps.Spawn("Props/shop_fridge", shop, new Vector3(-3.15f, PadTop, FrontZ + 0.4f), 0f, 0f, 0f, true);
            KenneyProps.Spawn("Props/shop_rodrack", shop, new Vector3(-1.95f, PadTop, FrontZ + 0.6f), 0f, 2.4f, 0f, false);
            KenneyProps.Spawn("Props/shop_baitbags", shop, new Vector3(-2.9f, PadTop, 4.05f), Rd(-6f, 6f), 0f, 0f, true);
            KenneyProps.Spawn("Props/harbor_bucket", shop, new Vector3(-1.75f, PadTop, 4.55f), Rd(0f, 360f), 0f, 0f, false);
            KenneyProps.Spawn("Props/harbor_cooler", shop, new Vector3(-0.85f, PadTop, 4.75f), Rd(-20f, 20f), 0f, 0f, false);
            KenneyProps.Spawn("Props/shop_stool", shop, new Vector3(0.0f, PadTop, 2.75f), Rd(0f, 90f), 0f, 0f, false);
            KenneyProps.Spawn("Props/shop_stool", shop, new Vector3(-0.8f, PadTop, 3.55f), Rd(0f, 90f), 0f, 0f, false);
            KenneyProps.Spawn("Props/shop_stool", shop, new Vector3(3.2f, PadTop, 4.6f), Rd(0f, 90f), 0f, 0f, false);
            // 오른쪽: 얼음 냉동고(벽 앞), 캐노피 밖 자판기
            KenneyProps.Spawn("Props/shop_freezer", shop, new Vector3(3.05f, PadTop, FrontZ + 0.5f), 0f, 0f, 0f, true);
            Text(shop, "얼음", new Vector3(3.05f, PadTop + 0.63f, FrontZ + 0.5f + 0.36f + 0.012f), Color.white, 1.4f, 180f);
            KenneyProps.Spawn("Props/shop_vending", shop, new Vector3(W * 0.5f + 0.75f, PadTop, FrontZ - 0.1f), 0f, 0f, 0f, true);
        }

        /// <summary>
        /// 깃발 배너 둘 (가게 앞 길가): 땅에 박은 받침 + 위에서 휘어 넘어가는 장대, 세로로 긴 천(위 바깥 모서리가 둥글다)에 세로 글자 — 앞뒤에서 읽힌다.
        /// </summary>
        static void Flags(Transform shop)
        {
            var specs = new[]
            {
                (pos: new Vector3(-4.35f, 0f, FrontZ + CanopyD + 0.6f), text: "생\n미\n끼", cloth: new Color(0.85f, 0.14f, 0.12f), ink: new Color(1f, 0.95f, 0.4f), yaw: 8f),
                (pos: new Vector3(4.55f, 0f, FrontZ + CanopyD + 0.3f), text: "선\n상\n낚\n시", cloth: new Color(0.12f, 0.32f, 0.72f), ink: Color.white, yaw: -10f),
            };
            var poles = new List<Vector3[]>();
            foreach (var s in specs)
            {
                var f = new GameObject("FlagBanner").transform;
                f.SetParent(shop, false);
                f.localPosition = s.pos;
                f.localRotation = Quaternion.Euler(0f, s.yaw, 0f);
                const float fw = 0.62f, h0 = 0.75f, h1 = 3.05f, r0 = 0.22f, r1 = 0.3f;
                // 천 테두리: 왼쪽 아래 → 오른쪽 아래 → 오른쪽 위 둥근 모서리 → 윗변 → 왼쪽 위 둥근 모서리 (장대가 왼쪽 변·윗변·오른쪽 모서리를 따라간다)
                var edge = new List<Vector2> { new Vector2(0.015f, h0), new Vector2(fw, h0) };
                for (int i = 0; i <= 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 0.5f;
                    edge.Add(new Vector2(fw - r1 + Mathf.Cos(a) * r1, h1 - r1 + Mathf.Sin(a) * r1));
                }
                for (int i = 0; i <= 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 0.5f;
                    edge.Add(new Vector2(r0 - Mathf.Sin(a) * r0 + 0.015f * (i / 6f), h1 - r0 + Mathf.Cos(a) * r0));
                }
                var q = Quaternion.Euler(0f, s.yaw, 0f);
                var pts = new List<Vector3> { s.pos + q * new Vector3(0f, PadTop, 0f) };
                for (int i = edge.Count - 1; i >= 2; i--) pts.Add(s.pos + q * new Vector3(edge[i].x - 0.01f, edge[i].y + 0.01f, 0f));
                poles.Add(pts.ToArray());
                var prof = edge;
                var cloth = new BlockMesh();
                cloth.Extrude(prof, -0.003f, 0.003f, Vector3.zero, Quaternion.identity, Color.white, Color.white);
                var go = cloth.Emit(f, "Cloth", Mats.Lit(s.cloth, 0.2f));
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                foreach (float side in new[] { -1f, 1f })
                {
                    var t = Text(f, s.text, new Vector3(fw * 0.5f + 0.01f, (h0 + h1) * 0.5f - 0.05f, side * 0.006f), s.ink, 3.6f, side > 0f ? 180f : 0f);
                    t.lineSpacing = -20f;
                    t.fontStyle = FontStyles.Bold;
                }
                // 받침: 물 채운 둥근 통
                Mats.Prim(PrimitiveType.Cylinder, "FlagBase", f, new Vector3(0f, PadTop + 0.07f, 0f), new Vector3(0.36f, 0.07f, 0.36f), Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0.3f), false);
            }
            var pole = new GameObject("FlagPoles");
            pole.transform.SetParent(shop, false);
            pole.AddComponent<MeshFilter>().sharedMesh = MapKit.Tubes(poles, _ => 0.014f, 6);
            pole.AddComponent<MeshRenderer>().sharedMaterial = Mats.Lit(new Color(0.8f, 0.82f, 0.84f), 0.5f, 0.6f);
        }

        /// <summary>월드 글자 (간판·라벨): yaw 180 = 앞(+Z)에서 읽힌다, 뒤에서는 안 보인다.</summary>
        static TextMeshPro Text(Transform parent, string s, Vector3 pos, Color c, float size, float yaw)
        {
            var t = UIFactory.WorldText(parent, s, pos, c, size, "ShopText");
            t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            UIFactory.OneSided(t);
            return t;
        }
    }
}
