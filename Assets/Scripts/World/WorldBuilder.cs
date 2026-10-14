using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>프리미티브로 한강 반포지구 맵을 만든다. 아트 에셋이 생기면 이 클래스만 교체하면 된다.</summary>
    public class WorldBuilder
    {
        public readonly List<Light> Lamps = new List<Light>();
        public readonly List<Renderer> LampBulbs = new List<Renderer>();

        Transform _root;
        System.Random _rng;

        static readonly Color Grass = new Color(0.27f, 0.42f, 0.2f);
        static readonly Color GrassDark = new Color(0.27f, 0.42f, 0.22f);
        static readonly Color Path = new Color(0.78f, 0.72f, 0.6f);
        static readonly Color BikeRoad = new Color(0.32f, 0.33f, 0.36f);
        static readonly Color Stone = new Color(0.55f, 0.55f, 0.53f);
        static readonly Color Concrete = new Color(0.68f, 0.68f, 0.66f);
        static readonly Color Wood = new Color(0.55f, 0.4f, 0.26f);

        float R(float min, float max) => (float)(min + _rng.NextDouble() * (max - min));

        public Transform Build()
        {
            _root = new GameObject("World").transform;
            _rng = new System.Random(20120328); // 오픈일을 시드로

            BuildWater();
            BuildBanks();
            BuildBridge();
            BuildPier();
            BuildShop();
            BuildWeeds();
            BuildRocks();
            BuildTrees();
            BuildLamps();
            BuildCity();
            BuildProps();
            BuildBankAnglers();
            return _root;
        }

        void BuildWater()
        {
            // 잔물결 수면 (버텍스 파도 + 흐르는 노멀맵). 강바닥 대신 아래에 어두운 판을 깐다
            WaterSurface.Create(_root, new Vector3(0f, HanRiverMap.WaterY, 0f), new Vector2(600f, 132f), 2f);
            Mats.Prim(PrimitiveType.Cube, "RiverBed", _root, new Vector3(0f, HanRiverMap.WaterY - 3f, 0f),
                new Vector3(600f, 1f, 132f), Mats.Lit(new Color(0.05f, 0.08f, 0.08f)), false);
        }

        void BuildBanks()
        {
            float landH = 6f;
            float depth = 100f; // 걸을 수 있는 범위(LandFarZ)보다 넓게 깔아서 뒤편 건물 아래도 땅이 보이게 한다
            float centerY = HanRiverMap.LandY - landH * 0.5f;
            float southZ = HanRiverMap.RiverMinZ - depth * 0.5f;
            float northZ = HanRiverMap.RiverMaxZ + depth * 0.5f;

            Mats.Prim(PrimitiveType.Cube, "SouthBank", _root, new Vector3(0f, centerY, southZ), new Vector3(600f, landH, depth), Mats.TexturedBox("grass", new Color(0.8f, 0.95f, 0.75f), new Vector2(600f, depth), 7f, 0.1f));
            Mats.Prim(PrimitiveType.Cube, "NorthBank", _root, new Vector3(0f, centerY, northZ), new Vector3(600f, landH, depth), Mats.TexturedBox("grass", new Color(0.62f, 0.75f, 0.58f), new Vector2(600f, depth), 7f, 0.1f));

            // 둔치 산책로, 자전거도로, 호안 석축
            Mats.Prim(PrimitiveType.Cube, "Walkway", _root, new Vector3(0f, HanRiverMap.LandY + 0.02f, -66f), new Vector3(600f, 0.08f, 4f), Mats.TexturedBox("concrete", new Color(0.95f, 0.92f, 0.86f), new Vector2(600f, 4f), 4f, 0.2f), false);
            Mats.Prim(PrimitiveType.Cube, "BikeRoad", _root, new Vector3(0f, HanRiverMap.LandY + 0.02f, -84f), new Vector3(600f, 0.08f, 6f), Mats.TexturedBox("asphalt", new Color(0.75f, 0.62f, 0.6f), new Vector2(600f, 6f), 6f, 0.15f), false);
            Mats.Prim(PrimitiveType.Cube, "BikeLine", _root, new Vector3(0f, HanRiverMap.LandY + 0.07f, -84f), new Vector3(600f, 0.02f, 0.15f), Mats.Lit(new Color(0.95f, 0.8f, 0.2f)), false);
            Mats.Prim(PrimitiveType.Cube, "SouthEdge", _root, new Vector3(0f, HanRiverMap.LandY - 0.4f, HanRiverMap.RiverMinZ - 0.3f), new Vector3(600f, 1.2f, 0.8f), Mats.TexturedBox("concrete", new Color(0.8f, 0.8f, 0.78f), new Vector2(600f, 1.2f), 2f, 0.2f));
            Mats.Prim(PrimitiveType.Cube, "NorthEdge", _root, new Vector3(0f, HanRiverMap.LandY - 0.4f, HanRiverMap.RiverMaxZ + 0.3f), new Vector3(600f, 1.2f, 0.8f), Mats.TexturedBox("concrete", new Color(0.8f, 0.8f, 0.78f), new Vector2(600f, 1.2f), 2f, 0.2f));

            // 북쪽 강변북로
            Mats.Prim(PrimitiveType.Cube, "Expressway", _root, new Vector3(0f, HanRiverMap.LandY + 0.02f, 74f), new Vector3(600f, 0.08f, 10f), Mats.TexturedBox("asphalt", Color.white, new Vector2(600f, 10f), 6f, 0.15f), false);
        }

        void BuildBridge()
        {
            var deckMat = Mats.TexturedBox("concrete", new Color(0.9f, 0.9f, 0.88f), new Vector2(18f, 240f), 6f, 0.2f);
            var steel = Mats.Lit(new Color(0.25f, 0.42f, 0.55f), 0.4f, 0.3f);
            float x = HanRiverMap.BridgeX;
            float deckY = 12f;

            var bridge = new GameObject("Bridge").transform;
            bridge.SetParent(_root, false);
            Mats.Prim(PrimitiveType.Cube, "Deck", bridge, new Vector3(x, deckY, 0f), new Vector3(18f, 1.6f, 240f), deckMat);
            Mats.Prim(PrimitiveType.Cube, "RailL", bridge, new Vector3(x - 8.8f, deckY + 1.2f, 0f), new Vector3(0.3f, 0.9f, 240f), steel, false);
            Mats.Prim(PrimitiveType.Cube, "RailR", bridge, new Vector3(x + 8.8f, deckY + 1.2f, 0f), new Vector3(0.3f, 0.9f, 240f), steel, false);

            foreach (var z in HanRiverMap.BridgePierZ)
            {
                Mats.Prim(PrimitiveType.Cube, "Pier", bridge, new Vector3(x, deckY * 0.5f - 1f, z), new Vector3(5f, deckY + 2f, 6f), deckMat);
            }
        }

        void BuildPier()
        {
            var r = HanRiverMap.Pier;
            var pier = new GameObject("FishingPier").transform;
            pier.SetParent(_root, false);
            var deck = Mats.TexturedBox("deck", new Color(1f, 0.92f, 0.85f), new Vector2(r.width, r.height), 3f, 0.3f);
            Mats.Prim(PrimitiveType.Cube, "Deck", pier, new Vector3(r.center.x, HanRiverMap.LandY - 0.15f, r.center.y),
                new Vector3(r.width, 0.3f, r.height), deck);
            for (float z = r.yMin + 2f; z < r.yMax; z += 4f)
            {
                Mats.Prim(PrimitiveType.Cylinder, "Post", pier, new Vector3(r.xMin + 0.2f, 0.3f, z), new Vector3(0.25f, 0.9f, 0.25f), deck, false);
                Mats.Prim(PrimitiveType.Cylinder, "Post", pier, new Vector3(r.xMax - 0.2f, 0.3f, z), new Vector3(0.25f, 0.9f, 0.25f), deck, false);
            }
            var rail = Mats.Lit(new Color(0.9f, 0.9f, 0.88f));
            Mats.Prim(PrimitiveType.Cube, "RailL", pier, new Vector3(r.xMin + 0.1f, HanRiverMap.LandY + 0.5f, r.center.y), new Vector3(0.08f, 0.08f, r.height), rail, false);
            Mats.Prim(PrimitiveType.Cube, "RailR", pier, new Vector3(r.xMax - 0.1f, HanRiverMap.LandY + 0.5f, r.center.y), new Vector3(0.08f, 0.08f, r.height), rail, false);
            Mats.Prim(PrimitiveType.Cube, "RailEnd", pier, new Vector3(r.center.x, HanRiverMap.LandY + 0.5f, r.yMax - 0.1f), new Vector3(r.width, 0.08f, 0.08f), rail, false);
        }

        void BuildShop()
        {
            var p = HanRiverMap.ShopPos;
            var shop = new GameObject("TackleShop").transform;
            shop.SetParent(_root, false);
            shop.localPosition = p;

            Mats.Prim(PrimitiveType.Cube, "Body", shop, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 4.5f), Mats.Lit(new Color(0.93f, 0.9f, 0.82f)));
            Mats.Prim(PrimitiveType.Cube, "Roof", shop, new Vector3(0f, 3.2f, 0f), new Vector3(7.6f, 0.4f, 5.2f), Mats.Lit(new Color(0.2f, 0.42f, 0.62f)));
            for (int i = 0; i < 6; i++)
            {
                var c = i % 2 == 0 ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.97f, 0.97f, 0.95f);
                Mats.Prim(PrimitiveType.Cube, "Awning", shop, new Vector3(-3f + i * 1.2f, 2.6f, 2.75f), new Vector3(1.2f, 0.12f, 1.4f),
                    Mats.Lit(c), false, Quaternion.Euler(-18f, 0f, 0f));
            }
            Mats.Prim(PrimitiveType.Cube, "Counter", shop, new Vector3(0f, 0.55f, 3.5f), new Vector3(5f, 1.1f, 0.6f), Mats.Lit(Wood));

            // 처마 조명: 밤에도 카운터와 주인이 보이게
            var shopLight = new GameObject("ShopLight").AddComponent<Light>();
            shopLight.transform.SetParent(shop, false);
            shopLight.transform.localPosition = new Vector3(0f, 2.4f, 3.4f);
            shopLight.type = LightType.Point;
            shopLight.range = 7f;
            shopLight.intensity = 1.3f;
            shopLight.color = new Color(1f, 0.85f, 0.6f);
            shopLight.shadows = LightShadows.None;

            // 낚시점 주인: 벽과 카운터 사이에 선다 (건물 상자는 z ±2.25)
            var owner = NpcAngler.Spawn(shop, new Vector3(1.2f, 0f, 2.75f), 0f, new CharacterLook
            {
                Name = "낚시점 김사장", Title = "반포 낚시점", TopModel = "Top_Shirt", Hair = "Hair_Buzzed", Beard = true, HairColor = new Color(0.3f, 0.3f, 0.3f),
                Shirt = new Color(0.85f, 0.85f, 0.8f), VestColor = new Color(0.55f, 0.35f, 0.2f), Pants = new Color(0.3f, 0.28f, 0.25f), Hat = false
            }, NpcAngler.Role.Talker);
            owner.NpcId = "shop";

            AddSign(shop, "낚시점", new Vector3(0f, 3.9f, 2.3f), new Color(1f, 0.9f, 0.4f));
        }

        public static void AddSign(Transform parent, string text, Vector3 localPos, Color color)
        {
            var t = UIFactory.WorldText(parent, text, localPos, color, 5f, "Sign");
            t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            UIFactory.OneSided(t);
        }

        /// <summary>앞뒤 어느 쪽에서 봐도 읽히는 표지 (길가처럼 양쪽에서 지나다니는 곳).</summary>
        public static void AddSignBothSides(Transform parent, string text, Vector3 localPos, Color color)
        {
            AddSign(parent, text, localPos, color);
            var back = UIFactory.WorldText(parent, text, localPos, color, 5f, "SignBack");
            back.transform.localRotation = Quaternion.identity;
            UIFactory.OneSided(back);
        }

        void BuildWeeds()
        {
            // E1, F1 칸: 수초 지대
            var pad = Mats.Lit(new Color(0.24f, 0.45f, 0.2f), 0.3f);
            var reed = Mats.Lit(new Color(0.55f, 0.6f, 0.3f));
            var weeds = new GameObject("Weeds").transform;
            weeds.SetParent(_root, false);
            for (int i = 0; i < 140; i++)
            {
                // 잎끼리 겹쳐도 깜빡이지 않게 높이를 조금씩 다르게
                var pos = new Vector3(R(55f, 150f), HanRiverMap.WaterY + 0.02f + (i % 6) * 0.012f, R(-59f, -38f));
                float s = R(0.6f, 1.6f);
                Mats.Prim(PrimitiveType.Cylinder, "Pad", weeds, pos, new Vector3(s, 0.01f, s), pad, false);
            }
            for (int i = 0; i < 220; i++)
            {
                var pos = new Vector3(R(52f, 155f), HanRiverMap.WaterY + 0.6f, R(-60f, -55f));
                Mats.Prim(PrimitiveType.Cylinder, "Reed", weeds, pos, new Vector3(0.05f, R(0.6f, 1.2f), 0.05f), reed, false,
                    Quaternion.Euler(R(-8f, 8f), 0f, R(-8f, 8f)));
            }
        }

        void BuildRocks()
        {
            var rocks = new GameObject("Rocks").transform;
            rocks.SetParent(_root, false);
            var mat = Mats.Lit(Stone, 0.25f);
            // B열 석축 호안
            for (int i = 0; i < 70; i++)
            {
                var pos = new Vector3(R(-100f, -50f), HanRiverMap.WaterY + R(-0.1f, 0.25f), R(-60f, -57f));
                Mats.Prim(PrimitiveType.Sphere, "Rock", rocks, pos, new Vector3(R(0.6f, 1.6f), R(0.4f, 0.9f), R(0.6f, 1.4f)), mat, false);
            }
            // 교각 주변 사석
            foreach (var z in HanRiverMap.BridgePierZ)
                for (int i = 0; i < 10; i++)
                {
                    var pos = new Vector3(HanRiverMap.BridgeX + R(-4f, 4f), HanRiverMap.WaterY, z + R(-5f, 5f));
                    Mats.Prim(PrimitiveType.Sphere, "Rock", rocks, pos, new Vector3(R(0.8f, 1.8f), R(0.4f, 0.8f), R(0.8f, 1.6f)), mat, false);
                }
        }

        void BuildTrees()
        {
            var trees = new GameObject("Trees").transform;
            trees.SetParent(_root, false);
            var trunk = Mats.Lit(new Color(0.42f, 0.3f, 0.2f));
            var leaves = new[]
            {
                Mats.Lit(new Color(0.24f, 0.45f, 0.2f)), Mats.Lit(new Color(0.3f, 0.52f, 0.24f)), Mats.Lit(new Color(0.2f, 0.38f, 0.18f))
            };
            for (int i = 0; i < 90; i++)
            {
                float x = R(-155f, 155f);
                float z = i % 3 == 0 ? R(-104f, -90f) : R(-80f, -70f);
                if (Mathf.Abs(x - HanRiverMap.ShopPos.x) < 7f && z > -82f) continue;
                PlaceTree(trees, new Vector3(x, HanRiverMap.LandY, z), trunk, leaves[i % leaves.Length]);
            }
            for (int i = 0; i < 50; i++)
                PlaceTree(trees, new Vector3(R(-155f, 155f), HanRiverMap.LandY, R(62f, 68f)), trunk, leaves[i % leaves.Length]);
        }

        static readonly string[] TreeModels =
        {
            "tree_default", "tree_oak", "tree_fat", "tree_detailed", "tree_tall", "tree_simple", "tree_plateau",
            "tree_pineRoundA", "tree_pineRoundB", "tree_pineRoundC"
        };
        static readonly string[] BushModels = { "plant_bush", "plant_bushLarge", "plant_bushDetailed" };

        void PlaceTree(Transform parent, Vector3 basePos, Material trunk, Material leaf)
        {
            if (KenneyProps.Available)
            {
                // Kenney Nature Kit 나무 (CC0), 높이 5~9m. 둘레에 덤불을 조금 심는다
                var model = TreeModels[_rng.Next(TreeModels.Length)];
                KenneyProps.Spawn("Kenney/Nature/" + model, parent, basePos, R(0f, 360f), R(5f, 9f));
                if (_rng.NextDouble() < 0.4)
                    KenneyProps.Spawn("Kenney/Nature/" + BushModels[_rng.Next(BushModels.Length)], parent,
                        basePos + new Vector3(R(-2f, 2f), 0f, R(-2f, 2f)), R(0f, 360f), R(0.7f, 1.3f));
                return;
            }
            float h = R(2.5f, 4f);
            var t = new GameObject("Tree").transform;
            t.SetParent(parent, false);
            t.localPosition = basePos;
            Mats.Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.3f, h * 0.5f, 0.3f), trunk);
            float s = R(2.2f, 3.4f);
            Mats.Prim(PrimitiveType.Sphere, "Leaves", t, new Vector3(0f, h + s * 0.3f, 0f), new Vector3(s, s * 0.9f, s), leaf, false);
            Mats.Prim(PrimitiveType.Sphere, "Leaves2", t, new Vector3(R(-0.6f, 0.6f), h + s * 0.7f, R(-0.6f, 0.6f)), Vector3.one * s * 0.65f, leaf, false);
        }

        void BuildLamps()
        {
            var lamps = new GameObject("Lamps").transform;
            lamps.SetParent(_root, false);
            var pole = Mats.Lit(new Color(0.2f, 0.22f, 0.25f), 0.5f, 0.6f);
            for (float x = -150f; x <= 150f; x += 25f)
            {
                var basePos = new Vector3(x, HanRiverMap.LandY, -63.5f);
                Vector3 head;
                if (KenneyProps.Available)
                {
                    // Kenney City Kit Roads 가로등: 높이 5.5m, 팔이 강 쪽(+Z)으로 뻗는다 (모델 팔은 -Z 방향)
                    const float lampH = 5.5f;
                    var lamp = KenneyProps.Spawn("Kenney/Roads/light-curved", lamps, basePos, 180f, lampH);
                    float s = lampH / 0.67f;
                    head = basePos + new Vector3(0f, 0.62f * s, 0.19f * s);
                    var bulb = Mats.Prim(PrimitiveType.Sphere, "Bulb", lamps, head, new Vector3(0.45f, 0.18f, 0.45f), pole, false);
                    LampBulbs.Add(bulb.GetComponent<Renderer>());
                }
                else
                {
                    Mats.Prim(PrimitiveType.Cylinder, "Pole", lamps, basePos + Vector3.up * 2f, new Vector3(0.12f, 2f, 0.12f), pole, false);
                    var bulb = Mats.Prim(PrimitiveType.Sphere, "Bulb", lamps, basePos + Vector3.up * 4.1f, Vector3.one * 0.45f, pole, false);
                    LampBulbs.Add(bulb.GetComponent<Renderer>());
                    head = basePos + Vector3.up * 4.1f;
                }

                var lightGo = new GameObject("LampLight");
                lightGo.transform.SetParent(lamps, false);
                lightGo.transform.localPosition = head - Vector3.up * 0.2f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 14f;
                light.intensity = 1.6f;
                light.color = new Color(1f, 0.82f, 0.55f);
                Lamps.Add(light);
            }
        }

        /// <summary>원작 영상의 한강 소품: 계류 말뚝, 노란 부표, 드럼통 뗏목과 다른 낚시꾼, 요트, 다리 조명.</summary>
        void BuildProps()
        {
            var props = new GameObject("Props").transform;
            props.SetParent(_root, false);

            var bollard = Mats.Lit(new Color(0.18f, 0.19f, 0.2f), 0.4f, 0.5f);
            for (float x = -150f; x <= 150f; x += 14f)
            {
                if (Mathf.Abs(x + 6f - HanRiverMap.Pier.center.x) < 4f) continue;
                Mats.Prim(PrimitiveType.Cylinder, "Bollard", props, new Vector3(x + 6f, HanRiverMap.LandY + 0.35f, HanRiverMap.RiverMinZ - 1.2f),
                    new Vector3(0.45f, 0.35f, 0.45f), bollard);
            }

            var buoy = Mats.Lit(new Color(0.95f, 0.8f, 0.15f), 0.5f);
            for (int i = 0; i < 14; i++)
            {
                var pos = new Vector3(R(-140f, 140f), HanRiverMap.WaterY + 0.15f, R(-35f, 20f));
                Mats.Prim(PrimitiveType.Cylinder, "Buoy", props, pos, new Vector3(0.7f, 0.45f, 0.7f), buoy, false,
                    Quaternion.Euler(90f, R(0f, 180f), 0f));
            }

            // 드럼통 뗏목 (C1 칸) 위의 다른 낚시꾼들
            var raft = new GameObject("Raft").transform;
            raft.SetParent(props, false);
            raft.localPosition = new Vector3(-30f, HanRiverMap.WaterY, -48f);
            raft.localRotation = Quaternion.Euler(0f, 25f, 0f);
            var barrel = Mats.Lit(new Color(0.55f, 0.22f, 0.18f), 0.4f);
            for (int bx = 0; bx < 4; bx++)
            for (int bz = 0; bz < 2; bz++)
                Mats.Prim(PrimitiveType.Cylinder, "Barrel", raft, new Vector3(-2.4f + bx * 1.6f, 0.2f, -0.8f + bz * 1.6f),
                    new Vector3(1.1f, 0.75f, 1.1f), barrel, false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Deck", raft, new Vector3(0f, 0.75f, 0f), new Vector3(6.4f, 0.12f, 3.4f), Mats.Lit(Wood), false);
            // 뗏목 위의 다른 낚시꾼들 (원작 영상의 "월척대소녀", "낚자마자회친다" 같은 유저들 느낌)
            NpcAngler.Spawn(raft, new Vector3(-1.4f, 0.81f, 0.6f), 0f, new CharacterLook
            {
                Female = true, Name = "월척대소녀", Title = "붕어 사냥꾼", BottomModel = "Pants_Jeans", HatStyle = 2, Hair = "Hair_Long", HairColor = new Color(0.35f, 0.22f, 0.15f),
                Shirt = new Color(0.9f, 0.35f, 0.45f), Pants = new Color(0.9f, 0.9f, 0.88f), Vest = false, HatColor = new Color(0.95f, 0.95f, 0.95f)
            }, NpcAngler.Role.Angler);
            NpcAngler.Spawn(raft, new Vector3(1.2f, 0.81f, 0.6f), 15f, new CharacterLook
            {
                Name = "낚자마자회친다", Title = "한강 터줏대감", TopModel = "Top_Long", ShoeModel = "Shoes_Boots", HatStyle = 1, Hair = "Hair_SimpleParted", Shirt = new Color(0.3f, 0.45f, 0.8f),
                VestColor = new Color(0.85f, 0.6f, 0.15f), Pants = new Color(0.2f, 0.22f, 0.26f), HatColor = new Color(0.2f, 0.3f, 0.25f)
            }, NpcAngler.Role.Angler);

            // 요트
            var hull = Mats.Lit(new Color(0.92f, 0.92f, 0.94f), 0.6f);
            var sail = Mats.Lit(new Color(0.85f, 0.88f, 0.9f));
            if (KenneyProps.Available)
            {
                // Kenney Watercraft Kit: 요트 3척, 정박한 작은 어선 2척, 나룻배 1척
                string[] yachts = { "boat-sail-a", "boat-sail-b", "boat-sail-a" };
                for (int i = 0; i < 3; i++)
                    KenneyProps.Spawn("Kenney/Watercraft/" + yachts[i], props,
                        new Vector3(-120f + i * 95f + R(-10f, 10f), HanRiverMap.WaterY - 0.35f, R(34f, 52f)), R(0f, 360f), 0f, 9f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", props, new Vector3(120f, HanRiverMap.WaterY - 0.3f, 50f), 80f, 0f, 7f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-fishing-small", props, new Vector3(-140f, HanRiverMap.WaterY - 0.3f, 48f), -100f, 0f, 7f);
                KenneyProps.Spawn("Kenney/Watercraft/boat-row-small", props, new Vector3(45f, HanRiverMap.WaterY - 0.15f, -54f), 15f, 0f, 3.6f);
            }
            for (int i = 0; i < (KenneyProps.Available ? 0 : 3); i++)
            {
                var boat = new GameObject("Yacht").transform;
                boat.SetParent(props, false);
                boat.localPosition = new Vector3(-120f + i * 95f + R(-10f, 10f), HanRiverMap.WaterY, R(30f, 52f));
                boat.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                Mats.Prim(PrimitiveType.Capsule, "Hull", boat, new Vector3(0f, 0.3f, 0f), new Vector3(2.4f, 4.5f, 1.2f), hull, false, Quaternion.Euler(90f, 0f, 0f));
                Mats.Prim(PrimitiveType.Cylinder, "Mast", boat, new Vector3(0f, 6f, 0.5f), new Vector3(0.15f, 6f, 0.15f), hull, false);
                Mats.Prim(PrimitiveType.Cube, "Sail", boat, new Vector3(0f, 5.5f, -1.4f), new Vector3(0.05f, 9f, 3.6f), sail, false, Quaternion.Euler(-12f, 0f, 0f));
            }

            // 다리 조명 (밤에 강물에 비친다)
            var bridgeLight = Mats.Emissive(new Color(1f, 0.85f, 0.55f), 2.5f);
            for (float z = -110f; z <= 110f; z += 10f)
            {
                Mats.Prim(PrimitiveType.Sphere, "BridgeLight", props, new Vector3(HanRiverMap.BridgeX - 8.8f, 13.9f, z), Vector3.one * 0.5f, bridgeLight, false);
                Mats.Prim(PrimitiveType.Sphere, "BridgeLight", props, new Vector3(HanRiverMap.BridgeX + 8.8f, 13.9f, z), Vector3.one * 0.5f, bridgeLight, false);
                // 강 위에 있는 다리 조명만 물에 비친다 (데크 쪽 = 플레이어가 보는 쪽 난간)
                if (z > HanRiverMap.RiverMinZ && z < HanRiverMap.RiverMaxZ)
                    WaterGlints.Add(new Vector3(HanRiverMap.BridgeX + 8.8f, 13.9f, z), new Color(1f, 0.85f, 0.55f), 0.7f);
            }
            // 건너편(북쪽) 도시 불빛: 아파트 창문 불빛이 강물에 길게 비친다
            for (int i = 0; i < 9; i++)
            {
                float x = -130f + i * 32f + (i % 3) * 5f;
                var c = i % 3 == 0 ? new Color(0.75f, 0.85f, 1f) : new Color(1f, 0.78f, 0.5f);
                WaterGlints.Add(new Vector3(x, 18f + (i % 4) * 6f, HanRiverMap.LandFarZ - 10f), c, 0.55f);
            }
        }

        /// <summary>둔치에서 낚시하는 NPC들. 플레이어가 혼자가 아닌 느낌을 준다.</summary>
        void BuildBankAnglers()
        {
            var parent = new GameObject("BankAnglers").transform;
            parent.SetParent(_root, false);
            // 한국 낚시 조합 연락관: 데크 입구에서 퀘스트를 주고 보고를 받는다
            var union = NpcAngler.Spawn(parent, new Vector3(HanRiverMap.Pier.center.x - 4f, HanRiverMap.LandY, HanRiverMap.RiverMinZ - 5f), 200f, new CharacterLook
            {
                Name = "박주임", Title = "한국 낚시 조합 연락관", TopModel = "Top_Long", BottomModel = "Pants_Jeans", Hair = "Hair_SimpleParted", HairColor = new Color(0.1f, 0.08f, 0.07f),
                Shirt = new Color(0.92f, 0.93f, 0.95f), Vest = true, VestColor = new Color(0.12f, 0.2f, 0.42f), Pants = new Color(0.15f, 0.16f, 0.2f),
                Shoes = new Color(0.08f, 0.08f, 0.08f), Hat = false
            }, NpcAngler.Role.Talker);
            union.NpcId = "union";
            float z = HanRiverMap.RiverMinZ - 1.6f;
            NpcAngler.Spawn(parent, new Vector3(-46f, HanRiverMap.LandY, z), 0f, new CharacterLook
            {
                Name = "한강도사", Title = "대물 전문", TopModel = "Top_Long", BottomModel = "Pants_Wader", ShoeModel = "Shoes_Boots", Hair = "Hair_Buzzed", Beard = true, HairColor = new Color(0.15f, 0.12f, 0.1f),
                Shirt = new Color(0.2f, 0.2f, 0.22f), VestColor = new Color(0.6f, 0.55f, 0.4f), Pants = new Color(0.35f, 0.33f, 0.28f)
            }, NpcAngler.Role.Angler);
            NpcAngler.Spawn(parent, new Vector3(72f, HanRiverMap.LandY, z), -10f, new CharacterLook
            {
                Female = true, DarkSkin = true, Name = "수초요정", Title = "가물치 킬러", BottomModel = "Pants_Jeans", BottomPattern = 2, Hair = "Hair_Buns", HairColor = new Color(0.1f, 0.08f, 0.07f),
                Shirt = new Color(0.95f, 0.85f, 0.3f), Vest = false, Pants = new Color(0.25f, 0.3f, 0.45f), Hat = false
            }, NpcAngler.Role.Angler);
            NpcAngler.Spawn(parent, new Vector3(110f, HanRiverMap.LandY, z), 5f, new CharacterLook
            {
                Name = "새벽조사", Title = "월척 3회", TopModel = "Top_Long", HatStyle = 1, DarkSkin = true, Hair = "Hair_SimpleParted", Shirt = new Color(0.4f, 0.6f, 0.4f),
                VestColor = new Color(0.2f, 0.25f, 0.35f), Pants = new Color(0.2f, 0.2f, 0.2f), HatColor = new Color(0.85f, 0.85f, 0.8f)
            }, NpcAngler.Role.Angler);
        }

        void BuildCity()
        {
            var city = new GameObject("City").transform;
            city.SetParent(_root, false);
            var walls = new[]
            {
                Mats.Lit(new Color(0.85f, 0.84f, 0.8f)), Mats.Lit(new Color(0.78f, 0.8f, 0.82f)),
                Mats.Lit(new Color(0.88f, 0.82f, 0.72f)), Mats.Lit(new Color(0.7f, 0.72f, 0.76f))
            };
            var windows = Mats.Emissive(new Color(0.95f, 0.85f, 0.6f), 0.6f);

            if (KenneyProps.Available)
            {
                BuildKenneyCity(city);
                BuildNamsan(city);
                return;
            }

            // 북쪽 강변 아파트 단지
            for (float x = -170f; x <= 170f; x += 22f)
            {
                float h = R(20f, 36f);
                float z = R(98f, 110f);
                var b = Mats.Prim(PrimitiveType.Cube, "Apartment", city, new Vector3(x, HanRiverMap.LandY + h * 0.5f, z),
                    new Vector3(16f, h, 9f), walls[_rng.Next(walls.Length)]);
                for (float y = 3f; y < h - 2f; y += 6f)
                    Mats.Prim(PrimitiveType.Cube, "Windows", b.transform, new Vector3(0f, (y - h * 0.5f) / h, -0.51f),
                        new Vector3(0.9f, 0.6f / h, 0.01f), windows, false);
            }

            BuildNamsan(city);

            // 남쪽 뒤편 저층 건물
            for (float x = -170f; x <= 170f; x += 30f)
            {
                float h = R(10f, 20f);
                Mats.Prim(PrimitiveType.Cube, "Building", city, new Vector3(x, HanRiverMap.LandY + h * 0.5f, -125f),
                    new Vector3(20f, h, 12f), walls[_rng.Next(walls.Length)], false);
            }
        }

        static readonly string[] TallBuildings =
        {
            "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e",
            "building-l", "building-m", "building-n", "building-f", "building-g", "building-i"
        };
        static readonly string[] LowBuildings =
            { "building-a", "building-b", "building-c", "building-d", "building-e", "building-h", "building-j", "building-k" };

        /// <summary>Kenney City Kit Commercial로 강 건너 빌딩 숲과 남쪽 뒤편 상가를 세운다.</summary>
        void BuildKenneyCity(Transform city)
        {
            // 북쪽: 두 줄로 겹쳐 세워 스카이라인을 만든다 (뒷줄이 더 높다)
            for (int row = 0; row < 2; row++)
            for (float x = -185f; x <= 185f; x += row == 0 ? 17f : 23f)
            {
                var model = TallBuildings[_rng.Next(TallBuildings.Length)];
                float h = row == 0 ? R(18f, 32f) : R(30f, 52f);
                float z = row == 0 ? R(96f, 104f) : R(118f, 130f);
                KenneyProps.Spawn("Kenney/City/" + model, city, new Vector3(x + R(-3f, 3f), HanRiverMap.LandY, z),
                    180f + (_rng.Next(4) * 90f) * (row == 0 ? 0f : 1f), h);
            }
            // 남쪽 뒤편 저층 상가
            for (float x = -175f; x <= 175f; x += 19f)
            {
                var model = LowBuildings[_rng.Next(LowBuildings.Length)];
                KenneyProps.Spawn("Kenney/City/" + model, city, new Vector3(x + R(-2f, 2f), HanRiverMap.LandY, R(-128f, -120f)), 0f, R(10f, 18f));
            }
        }

        void BuildNamsan(Transform city)
        {
            // 멀리 보이는 남산과 타워
            var hill = Mats.Lit(new Color(0.22f, 0.36f, 0.22f));
            Mats.Prim(PrimitiveType.Sphere, "Namsan", city, new Vector3(60f, -40f, 260f), new Vector3(260f, 150f, 160f), hill, false);
            var tower = Mats.Lit(new Color(0.9f, 0.9f, 0.92f));
            Mats.Prim(PrimitiveType.Cylinder, "Tower", city, new Vector3(60f, 55f, 250f), new Vector3(3f, 25f, 3f), tower, false);
            Mats.Prim(PrimitiveType.Sphere, "TowerTop", city, new Vector3(60f, 78f, 250f), new Vector3(9f, 5f, 9f), tower, false);
            Mats.Prim(PrimitiveType.Cylinder, "Antenna", city, new Vector3(60f, 90f, 250f), new Vector3(0.6f, 10f, 0.6f), Mats.Emissive(new Color(1f, 0.3f, 0.3f), 1f), false);
        }
    }
}
