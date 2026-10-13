using System.Collections.Generic;
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

        static readonly Color Grass = new Color(0.36f, 0.52f, 0.28f);
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
            return _root;
        }

        void BuildWater()
        {
            var water = Mats.Lit(new Color(0.13f, 0.3f, 0.38f), 0.93f);
            Mats.Prim(PrimitiveType.Cube, "River", _root, new Vector3(0f, HanRiverMap.WaterY - 0.5f, 0f),
                new Vector3(600f, 1f, 130f), water);
        }

        void BuildBanks()
        {
            float landH = 6f;
            float depth = 100f; // 걸을 수 있는 범위(LandFarZ)보다 넓게 깔아서 뒤편 건물 아래도 땅이 보이게 한다
            float centerY = HanRiverMap.LandY - landH * 0.5f;
            float southZ = HanRiverMap.RiverMinZ - depth * 0.5f;
            float northZ = HanRiverMap.RiverMaxZ + depth * 0.5f;

            Mats.Prim(PrimitiveType.Cube, "SouthBank", _root, new Vector3(0f, centerY, southZ), new Vector3(600f, landH, depth), Mats.Lit(Grass));
            Mats.Prim(PrimitiveType.Cube, "NorthBank", _root, new Vector3(0f, centerY, northZ), new Vector3(600f, landH, depth), Mats.Lit(GrassDark));

            // 둔치 산책로, 자전거도로, 호안 석축
            Mats.Prim(PrimitiveType.Cube, "Walkway", _root, new Vector3(0f, HanRiverMap.LandY + 0.01f, -66f), new Vector3(600f, 0.02f, 4f), Mats.Lit(Path), false);
            Mats.Prim(PrimitiveType.Cube, "BikeRoad", _root, new Vector3(0f, HanRiverMap.LandY + 0.01f, -84f), new Vector3(600f, 0.02f, 6f), Mats.Lit(BikeRoad), false);
            Mats.Prim(PrimitiveType.Cube, "BikeLine", _root, new Vector3(0f, HanRiverMap.LandY + 0.025f, -84f), new Vector3(600f, 0.02f, 0.15f), Mats.Lit(new Color(0.95f, 0.8f, 0.2f)), false);
            Mats.Prim(PrimitiveType.Cube, "SouthEdge", _root, new Vector3(0f, HanRiverMap.LandY - 0.4f, HanRiverMap.RiverMinZ - 0.3f), new Vector3(600f, 1.2f, 0.8f), Mats.Lit(Stone));
            Mats.Prim(PrimitiveType.Cube, "NorthEdge", _root, new Vector3(0f, HanRiverMap.LandY - 0.4f, HanRiverMap.RiverMaxZ + 0.3f), new Vector3(600f, 1.2f, 0.8f), Mats.Lit(Stone));

            // 북쪽 강변북로
            Mats.Prim(PrimitiveType.Cube, "Expressway", _root, new Vector3(0f, HanRiverMap.LandY + 0.02f, 74f), new Vector3(600f, 0.04f, 10f), Mats.Lit(BikeRoad), false);
        }

        void BuildBridge()
        {
            var deckMat = Mats.Lit(Concrete);
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
            var deck = Mats.Lit(Wood);
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
            Mats.Prim(PrimitiveType.Cube, "Counter", shop, new Vector3(0f, 0.6f, 2.6f), new Vector3(5f, 1.2f, 0.6f), Mats.Lit(Wood));

            // 주인 NPC
            Mats.Prim(PrimitiveType.Capsule, "Owner", shop, new Vector3(1.2f, 1f, 1.6f), new Vector3(0.7f, 0.85f, 0.7f), Mats.Lit(new Color(0.3f, 0.5f, 0.35f)), false);
            Mats.Prim(PrimitiveType.Sphere, "OwnerHead", shop, new Vector3(1.2f, 2.05f, 1.6f), Vector3.one * 0.45f, Mats.Lit(new Color(0.96f, 0.8f, 0.66f)), false);

            AddSign(shop, "낚시점", new Vector3(0f, 3.9f, 2.3f), new Color(1f, 0.9f, 0.4f));
        }

        static void AddSign(Transform parent, string text, Vector3 localPos, Color color)
        {
            var go = new GameObject("Sign");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UIFactory.Font;
            tm.fontSize = 64;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = UIFactory.Font.material;
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
                var pos = new Vector3(R(55f, 150f), HanRiverMap.WaterY + 0.01f, R(-59f, -38f));
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

        void PlaceTree(Transform parent, Vector3 basePos, Material trunk, Material leaf)
        {
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
                Mats.Prim(PrimitiveType.Cylinder, "Pole", lamps, basePos + Vector3.up * 2f, new Vector3(0.12f, 2f, 0.12f), pole, false);
                var bulb = Mats.Prim(PrimitiveType.Sphere, "Bulb", lamps, basePos + Vector3.up * 4.1f, Vector3.one * 0.45f, pole, false);
                LampBulbs.Add(bulb.GetComponent<Renderer>());

                var lightGo = new GameObject("LampLight");
                lightGo.transform.SetParent(lamps, false);
                lightGo.transform.localPosition = basePos + Vector3.up * 3.9f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 14f;
                light.intensity = 1.6f;
                light.color = new Color(1f, 0.82f, 0.55f);
                Lamps.Add(light);
            }
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

            // 멀리 보이는 남산과 타워
            var hill = Mats.Lit(new Color(0.22f, 0.36f, 0.22f));
            Mats.Prim(PrimitiveType.Sphere, "Namsan", city, new Vector3(60f, -40f, 260f), new Vector3(260f, 150f, 160f), hill, false);
            var tower = Mats.Lit(new Color(0.9f, 0.9f, 0.92f));
            Mats.Prim(PrimitiveType.Cylinder, "Tower", city, new Vector3(60f, 55f, 250f), new Vector3(3f, 25f, 3f), tower, false);
            Mats.Prim(PrimitiveType.Sphere, "TowerTop", city, new Vector3(60f, 78f, 250f), new Vector3(9f, 5f, 9f), tower, false);
            Mats.Prim(PrimitiveType.Cylinder, "Antenna", city, new Vector3(60f, 90f, 250f), new Vector3(0.6f, 10f, 0.6f), Mats.Emissive(new Color(1f, 0.3f, 0.3f), 1f), false);

            // 남쪽 뒤편 저층 건물
            for (float x = -170f; x <= 170f; x += 30f)
            {
                float h = R(10f, 20f);
                Mats.Prim(PrimitiveType.Cube, "Building", city, new Vector3(x, HanRiverMap.LandY + h * 0.5f, -125f),
                    new Vector3(20f, h, 12f), walls[_rng.Next(walls.Length)], false);
            }
        }
    }
}
