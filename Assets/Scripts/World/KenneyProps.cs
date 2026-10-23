using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// Kenney CC0 모델(City Kit Commercial, City Kit Roads, Nature Kit, Watercraft Kit)을 배치하는 헬퍼.
    ///  - 원본은 1~5 단위 크기라서 원하는 높이(또는 길이)에 맞춰 균일하게 키운다
    ///  - Nature Kit의 청록색 잎·풀은 한강 풍경에 맞는 색으로 다시 칠한다
    ///  - 도시 건물은 밤에 창문 불빛처럼 은은하게 빛나도록 발광 머티리얼로 바꾼다 (<see cref="SetNightGlow"/>)
    /// 에셋이 없으면 <see cref="Available"/>이 false이고, WorldBuilder는 예전 프리미티브로 그린다.
    /// </summary>
    public static class KenneyProps
    {
        static bool? _available;
        public static bool Available => _available ??= Resources.Load<GameObject>("Kenney/Nature/tree_default") != null;

        static readonly Dictionary<string, Material> Recolored = new Dictionary<string, Material>();
        static Material _cityGlow;

        // Nature Kit 머티리얼 이름 → 한강 팔레트
        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "leafsGreen", new Color(0.27f, 0.47f, 0.2f) },
            { "leafsDark", new Color(0.17f, 0.34f, 0.19f) },
            { "woodBark", new Color(0.4f, 0.29f, 0.2f) },
            { "woodBarkDark", new Color(0.32f, 0.24f, 0.18f) },
            { "grass", new Color(0.3f, 0.48f, 0.21f) },
            { "dirt", new Color(0.48f, 0.43f, 0.37f) },
            { "_defaultMat", new Color(0.55f, 0.55f, 0.53f) },
        };

        /// <summary>
        /// 모델을 만든다. height > 0이면 높이를, length > 0이면 가장 긴 수평 길이를 그 값에 맞춘다.
        /// </summary>
        // Kenney 나무(원뿔·공을 쌓은 도형 나무) 자리에 쓰는 실사풍 나무 (VegetationLibrary: ez-tree로 만든 침엽수·활엽수).
        // 원뿔을 쌓은 소나무가 "도형 올려놓은 것" 같다는 지적으로 모두 바꾼다.
        static readonly string[] BroadleafKenney =
        {
            "tree_default", "tree_oak", "tree_fat", "tree_detailed", "tree_simple", "tree_tall", "tree_plateau",
            "tree_pineRoundA", "tree_pineRoundB", "tree_pineRoundC", "tree_pineTallA", "tree_pineTallB", "tree_pineDefaultA", "tree_pineDefaultB",
        };
        // 꽃·열매를 다는 작은 나무와 덤불 (Editor/VegetationBake → Resources/Plants, ez-tree)
        static readonly string[] PlantTrees = { "Plants/tree01", "Plants/tree02", "Plants/tree03", "Plants/tree04" };
        static bool? _plants;
        public static bool PlantsAvailable => _plants ??= Resources.Load<GameObject>(PlantTrees[0]) != null;

        /// <summary>
        /// Kenney 장난감 배 → tools/blender/boats.py 배 (사진 대조로 만든 한국 연안 어선·모터보트·나무배·요트). 둘 다 뱃머리 +Z, 기준점 용골 바닥이라 그대로 바꿔 끼운다.
        /// </summary>
        static readonly Dictionary<string, string> BoatSwap = new Dictionary<string, string>
        {
            { "Kenney/Watercraft/boat-fishing-small", "Props/boat_kr" }, { "Kenney/Watercraft/boat-speed-a", "Props/boat_speed" },
            { "Kenney/Watercraft/boat-speed-c", "Props/boat_speed" }, { "Kenney/Watercraft/boat-row-large", "Props/boat_row" },
            { "Kenney/Watercraft/boat-row-small", "Props/boat_row" }, { "Kenney/Watercraft/boat-sail-a", "Props/yacht" }, { "Kenney/Watercraft/boat-sail-b", "Props/yacht" },
        };

        /// <summary>true인 동안 놓는 소품은 풍화 결(월드 좌표)을 입히지 않는다 — 움직이는 플레이어 배는 결이 미끄러져 보인다.</summary>
        public static bool NoSurface;

        public static GameObject Spawn(string path, Transform parent, Vector3 localPos, float yaw, float height = 0f, float length = 0f, bool solid = true)
        {
            if (BoatSwap.TryGetValue(path, out var swap) && Resources.Load<GameObject>(swap) != null) path = swap;
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            // 실사풍 나무가 있으면 먼저: 소나무 자리 → 침엽수, 활엽수 자리 → 활엽수 (VegetationLibrary)
            if (leaf.StartsWith("tree_pine") && VegetationLibrary.HasConifers)
                return SpawnRealTree(VegetationLibrary.Lib.conifers, parent, localPos, yaw, height > 0f ? height * 1.25f : 9f, solid);
            if (System.Array.IndexOf(BroadleafKenney, leaf) >= 0 && VegetationLibrary.HasBroadleaf)
                return SpawnRealTree(VegetationLibrary.Lib.broadleaf, parent, localPos, yaw, height > 0f ? height : 7f, solid);
            if (System.Array.IndexOf(BroadleafKenney, leaf) >= 0 && PlantsAvailable)
                return SpawnPlantTree(parent, localPos, yaw, height > 0f ? height : 7f, solid);
            // Kenney 덤불(각진 초록 덩어리) → 실사풍 덤불 (Resources/Plants)
            if (leaf.StartsWith("plant_bush") && PlantsAvailable)
                return SpawnBush(parent, localPos, yaw, height > 0f ? height : 1.2f);
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            // Blender FBX(Z-up)는 루트에 축 변환 회전(-90° X)이 들어 있다. 그걸 지우면 배·야자수가 옆으로 눕거나 서 버린다
            // (예전엔 지워서 정크선·펠루카가 세로로 서 있었다). 원래 회전을 두고 그 위에 yaw만 더한다
            var baseRot = prefab.transform.localRotation;
            go.transform.localRotation = baseRot;
            go.transform.localScale = Vector3.one;
            var size = RotatedSize(LocalBounds(go), baseRot);
            float s = 1f;
            if (height > 0f && size.y > 0.001f) s = height / size.y;
            else if (length > 0f) s = length / Mathf.Max(0.001f, Mathf.Max(size.x, size.z));
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * baseRot;

            bool nature = path.Contains("/Nature/");
            bool city = path.Contains("/City/");
            bool lamp = path.Contains("/Roads/light");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (nature) mats[i] = Recolor(mats[i]);
                    else if (city) mats[i] = CityGlow(mats[i]);
                    // 가로등: 장난감 라벤더 색 → 짙은 회색 도장 철제 (실제 해안 가로등)
                    else if (lamp) mats[i] = Mats.Weathered("rust", new Color(0.36f, 0.38f, 0.4f), 1.2f, 0.45f, 0f, 0.35f, 0f, null, 1f, 0.35f);
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = city ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (path.StartsWith("Props/"))
            {
                if (NoSurface || !PropSurface(leaf, go)) GammaColors(go);
            }
            if (solid) AddCollider(go, path);
            return go;
        }

        /// <summary>
        /// 실사 나무 (VegetationLibrary): 위치로 종류를 고르고 높이를 맞춘다. 크기마다 조금씩 다르게(±12%), 줄기 콜라이더.
        /// LODGroup·빌보드가 있는 소나무도 통째로 늘리면 된다.
        /// </summary>
        public static GameObject SpawnRealTree(GameObject[] list, Transform parent, Vector3 localPos, float yaw, float height, bool solid)
        {
            int k = Mathf.Abs(Mathf.RoundToInt(localPos.x * 7.31f + localPos.z * 3.17f)) % list.Length;
            var prefab = list[k];
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "RealTree";
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var b = LocalBounds(go);
            float s = b.size.y > 0.01f ? height / b.size.y : 1f;
            s *= 0.88f + 0.24f * Mathf.PerlinNoise(localPos.x * 0.37f, localPos.z * 0.37f);
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            if (solid)
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = b.size.y * 0.025f;
                cap.height = b.size.y * 0.5f;
                cap.center = new Vector3(0f, b.min.y + cap.height * 0.5f, 0f);
            }
            return go;
        }

        /// <summary>덤불 6종 (Resources/Plants/bush01~06, 위치로 종류를 고른다).</summary>
        static readonly string[] PlantBushes = { "bush01", "bush02", "bush03", "bush04", "bush05", "bush06" };

        /// <summary>
        /// 덤불(tree면 작은 나무, Resources/Plants)을 높이에 맞춰 놓는다 (구 프리미티브 덤불·나무 자리). 없으면 null.
        /// blossoms > 0이면 잎 덩어리 겉면에 작은 꽃·열매(blossom 재질, 지름 size m)를 흩어 붙인다 (진달래·동백·귤).
        /// </summary>
        public static GameObject SpawnBush(Transform parent, Vector3 localPos, float yaw, float height, int blossoms = 0, Material blossom = null, float size = 0.2f, bool tree = false)
        {
            if (!PlantsAvailable) return null;
            if (tree && blossoms == 0 && VegetationLibrary.HasBroadleaf)
                return SpawnRealTree(VegetationLibrary.Lib.broadleaf, parent, localPos, yaw, height, false);
            var list = tree ? PlantTrees : System.Array.ConvertAll(PlantBushes, n => "Plants/" + n);
            int k = Mathf.Abs(Mathf.RoundToInt(localPos.x * 5.13f + localPos.z * 2.71f)) % list.Length;
            var prefab = Resources.Load<GameObject>(list[k]);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = tree ? "NatureTree" : "NatureBush";
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var b = LocalBounds(go);
            float s = b.size.y > 0.01f ? height / b.size.y : 1f;
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (blossoms > 0 && blossom != null)
            {
                // 잎 덩어리(나무면 위쪽)를 타원체로 보고 겉면 조금 안쪽에 꽃을 붙인다
                var c = b.center;
                var e = b.extents;
                if (tree) { c.y = b.min.y + b.size.y * 0.68f; e.y = b.size.y * 0.3f; }
                var rng = new System.Random(Mathf.RoundToInt(localPos.x * 31f + localPos.z * 17f));
                for (int i = 0; i < blossoms; i++)
                {
                    var d = new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 1.4f - 0.4f, (float)rng.NextDouble() * 2f - 1f).normalized;
                    var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(f.GetComponent<Collider>());
                    f.name = "Blossom";
                    f.transform.SetParent(go.transform, false);
                    f.transform.localPosition = c + Vector3.Scale(d, e) * 0.88f;
                    f.transform.localScale = Vector3.one * (size / s) * (0.8f + 0.4f * (float)rng.NextDouble());
                    f.GetComponent<Renderer>().sharedMaterial = blossom;
                }
            }
            return go;
        }

        static GameObject SpawnPlantTree(Transform parent, Vector3 localPos, float yaw, float height, bool solid)
        {
            int k = Mathf.Abs(Mathf.RoundToInt(localPos.x * 7.31f + localPos.z * 3.17f)) % PlantTrees.Length;
            var prefab = Resources.Load<GameObject>(PlantTrees[k]);
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "NatureTree";
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var b = LocalBounds(go);
            float s = b.size.y > 0.01f ? height * 1.15f / b.size.y : 1f;
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (solid)
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = b.size.y * 0.025f;
                cap.height = b.size.y * 0.5f;
                cap.center = new Vector3(0f, b.min.y + cap.height * 0.5f, 0f);
            }
            return go;
        }

        /// <summary>
        /// 부딪히는 몸: 건물·바위·배는 상자, 나무는 줄기, 가로등은 기둥 (풀·덤불·꽃·부표는 없음).
        /// 사람이 건물·나무를 뚫고 지나가거나 찌가 뚫고 날아가지 않게 한다.
        /// </summary>
        static void AddCollider(GameObject go, string path)
        {
            string n = path.Substring(path.LastIndexOf('/') + 1);
            var b = LocalBounds(go);
            if (path.Contains("/City/") || n.StartsWith("rock_") || (path.Contains("/Watercraft/") && !n.StartsWith("buoy")))
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }
            else if (path.StartsWith("Props/") && !n.StartsWith("palm"))
            {
                // 해외 소품 배 (펠루카·정크선): 상자
                var box = go.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }
            else if (n.StartsWith("tree_") || n.StartsWith("palm"))
            {
                var cap = go.AddComponent<CapsuleCollider>();
                // Blender 야자수(Props)는 로컬 Z가 위 (루트 축 변환 회전)
                bool zUp = Vector3.Dot(go.transform.localRotation * Vector3.forward, Vector3.up) > 0.7f;
                float tall = zUp ? b.size.z : b.size.y, low = zUp ? b.min.z : b.min.y;
                cap.direction = zUp ? 2 : 1;
                cap.radius = tall * 0.04f;
                cap.height = tall * 0.6f;
                cap.center = zUp ? new Vector3(0f, 0f, low + cap.height * 0.5f) : new Vector3(0f, low + cap.height * 0.5f, 0f);
            }
            else if (path.Contains("/Roads/light"))
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = b.size.y * 0.025f;
                cap.height = b.size.y;
                cap.center = new Vector3(0f, b.center.y, 0f);
            }
        }

        /// <summary>스케일 1 기준 렌더러 경계 (부모 기준 좌표).</summary>
        /// <summary>로컬 경계 상자를 rot만큼 돌린 뒤의 축 정렬 크기 (부모 기준 가로·높이·깊이).</summary>
        static Vector3 RotatedSize(Bounds b, Quaternion rot)
        {
            var size = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                var c = rot * Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                size = Vector3.Max(size, new Vector3(Mathf.Abs(c.x), Mathf.Abs(c.y), Mathf.Abs(c.z)));
            }
            return size * 2f;
        }

        public static Bounds LocalBounds(GameObject go)
        {
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                // 메쉬 경계의 8 꼭짓점을 루트 기준으로 옮겨 합친다
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static Material Recolor(Material src)
        {
            var key = src.name.Replace(" (Instance)", "");
            if (Recolored.TryGetValue(key, out var m)) return m;
            if (!Palette.TryGetValue(key, out var color)) { Recolored[key] = src; return src; }
            m = Mats.Lit(color, key.StartsWith("leafs") ? 0.25f : 0.1f);
            Recolored[key] = m;
            return m;
        }

        static readonly Dictionary<Material, Material> GammaCache = new Dictionary<Material, Material>();

        /// <summary>
        /// 결을 입히지 않는 Blender 소품(움직이는 배·야자수·열기구…): FBX 색이 선형 값으로 들어와 감마 프로젝트에서 어둡게 보이므로 색만 감마로 바꾼 복사 재질로.
        /// </summary>
        static void GammaColors(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_Color")) continue;
                    if (!GammaCache.TryGetValue(m, out var g))
                    {
                        g = new Material(m) { name = m.name + "_g", color = m.color.gamma };
                        GammaCache[m] = g;
                    }
                    mats[i] = g;
                }
                r.sharedMaterials = mats;
            }
        }

        /// <summary>소품에서 이름이 prefix로 시작하는 재질(Blender 재질 이름)을 m으로 바꾼다 — 텐트 겉감처럼 한 모델을 여러 색으로 쓸 때.</summary>
        public static void Tint(GameObject go, string prefix, Material m)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith(prefix)) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        static bool Bright(Color c) => c.grayscale > 0.78f;
        static bool Vivid(Color c)
        {
            Color.RGBToHSV(c, out _, out float sat, out float val);
            return sat > 0.55f && val > 0.35f;
        }

        /// <summary>
        /// Blender 소품(Resources/Props)은 FBX 색만 있어 매끈한 플라스틱처럼 보인다 → 재질 결을 트라이플래너로 입힌다 (부위 색은 그대로).
        /// 석상은 돌 결·빗물 줄·이끼, 배·망루는 판재, 난파선은 녹. 금박·돛·리본처럼 반짝이거나 천인 부위는 건너뛴다.
        /// </summary>
        static bool PropSurface(string id, GameObject go)
        {
            switch (id)
            {
                case "buddha_white": // 푸켓 빅 부다: 흰 버마 대리석
                    Mats.Weatherize(go, "marble", 3f, 0.35f, 0f, 0.45f, 0.12f, new Color(0.45f, 0.45f, 0.38f), null, 0.85f); return true;
                case "buddha_gold": // 금박은 그대로, 받침 돌만
                    Mats.Weatherize(go, "sandstone", 2.5f, 0.2f, 0f, 0.4f, 0.15f, null, c => c.r > c.b * 1.5f); return true; // (색은 선형 값이라 밝기로 거르면 금 받침이 빠졌다)
                case "merlion": // 센토사 머라이언: 회색 GRC(유리섬유 보강 콘크리트), 비늘 위로 빗물 줄
                    Mats.Weatherize(go, "concrete", 2.5f, 0.25f, 1f, 0.6f, 0.15f, null, null, 0.85f); return true;
                case "eagle": // 다타란 랑: 적갈색 칠한 조각 + 바위 받침
                    Mats.Weatherize(go, "concrete", 1.8f, 0.3f, 1f, 0.35f, 0.05f, null, c => c.r > 0.85f && c.g > 0.7f, 0.9f); return true;
                case "hareubang": // 제주 현무암: 구멍 숭숭한 거친 돌
                    Mats.Weatherize(go, "basalt", 0.6f, 0.15f, 0f, 0.25f, 0.2f, new Color(0.3f, 0.34f, 0.24f), null, 0.95f); return true;
                case "singha": // 앙코르 사자상: 사암
                    Mats.Weatherize(go, "sandstone", 1.6f, 0.15f, 0f, 0.5f, 0.45f, new Color(0.22f, 0.27f, 0.18f), null, 0.8f); return true;
                case "shipwreck": // 자킨토스 파나기오티스호: 녹슨 철
                    Mats.Weatherize(go, "rust", 2.5f, 0.3f, 1f, 0.6f, 0f, null, null, 0.4f); return true;
                case "boat_kr": case "boat_speed": case "yacht": // FRP 선체: 결은 옅게, 흘수선 물때·빗물 줄 (깃발·전구·유리는 그대로)
                    Mats.Weatherize(go, "concrete", 2f, 0.45f, 1f, 0.45f, 0.05f, null, c => Vivid(c) || c.grayscale < 0.03f, 1f, 0.3f); return true;
                case "boat_row": case "boat_caique":
                case "longtail_a": case "longtail_b": case "felucca": case "junk": case "lifeguard": case "sunbed":
                    Mats.Weatherize(go, "planks", 1.6f, 0.3f, 1f, 0.2f, 0.05f, null, c => Bright(c) || Vivid(c), 0.75f); return true;
                case "parasol_thatch":
                    Mats.Weatherize(go, "thatch", 1.2f, 0.05f, 0f, 0f, 0f, null, null, 0.7f); return true;
            }
            return false;
        }

        /// <summary>
        /// Kenney 도시 건물: 팔레트 텍스처(라벤더·하늘색 장난감 색)를 반쯤 무채색으로 빼고 따뜻한 회벽 색을 곱한 뒤,
        /// 회벽 결·빗물 자국을 트라이플래너로 입힌다 (Fishing/Weathered 팔레트 모드). 밤 창 불빛은 팔레트를 발광으로.
        /// </summary>
        static Material CityGlow(Material src)
        {
            if (_cityGlow != null) return _cityGlow;
            var wx = Mats.Wall(new Color(1.3f, 1.26f, 1.18f));
            _cityGlow = wx.shader.name == "Fishing/Weathered" ? new Material(wx) : new Material(src);
            _cityGlow.name = "KenneyCityGlow";
            if (wx.shader.name == "Fishing/Weathered")
            {
                _cityGlow.SetTexture("_PaletteTex", src.mainTexture);
                _cityGlow.SetFloat("_UsePalette", 1f);
                _cityGlow.SetFloat("_PaletteDesat", 0.55f);
                _cityGlow.SetFloat("_Tile", 2f);
            }
            else _cityGlow.EnableKeyword("_EMISSION");
            _cityGlow.SetTexture("_EmissionMap", src.mainTexture);
            _cityGlow.SetColor("_EmissionColor", Color.black);
            _cityGlow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return _cityGlow;
        }

        /// <summary>밤(0~1)에 도시 건물이 창문 불빛처럼 은은하게 빛난다. DayNightCycle이 부른다.</summary>
        public static void SetNightGlow(float night)
        {
            if (_cityGlow == null) return;
            _cityGlow.SetColor("_EmissionColor", new Color(0.55f, 0.47f, 0.32f) * Mathf.Clamp01(night) * 0.55f);
        }
    }
}
