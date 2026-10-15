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
        // Kenney 나무(원뿔·공을 쌓은 도형 나무) 자리에 쓰는 Nature Starter Kit 2 나무 (Tree Creator, Resources/NatureKit).
        // 예전에는 활엽수만 바꾸고 소나무는 Kenney 그대로였는데, 원뿔을 쌓은 소나무가 "도형 올려놓은 것" 같다는 지적으로 모두 바꾼다.
        // 실사 소나무 에셋(WORK-IN-PROGRESS "사용자가 해야 할 일")이 들어오면 소나무 자리만 그쪽으로 돌린다
        static readonly string[] BroadleafKenney =
        {
            "tree_default", "tree_oak", "tree_fat", "tree_detailed", "tree_simple", "tree_tall", "tree_plateau",
            "tree_pineRoundA", "tree_pineRoundB", "tree_pineRoundC", "tree_pineTallA", "tree_pineTallB", "tree_pineDefaultA", "tree_pineDefaultB",
        };
        // Tree Creator 나무는 빌드에서 메쉬가 빠지므로 에디터가 구워 둔 것(Editor/NatureKitBake → Resources/NatureKit/Baked)을 쓴다
        static readonly string[] NatureKitTrees = { "NatureKit/Baked/tree01", "NatureKit/Baked/tree02", "NatureKit/Baked/tree03", "NatureKit/Baked/tree04" };
        static bool? _natureKit;
        public static bool NatureKitAvailable => _natureKit ??= Resources.Load<GameObject>(NatureKitTrees[0]) != null;

        public static GameObject Spawn(string path, Transform parent, Vector3 localPos, float yaw, float height = 0f, float length = 0f, bool solid = true)
        {
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            // 실사 에셋이 있으면 먼저: 소나무 자리 → Conifers [BOTD], 활엽수 자리 → Realistic Tree 9·10
            if (leaf.StartsWith("tree_pine") && VegetationLibrary.HasConifers)
                return SpawnRealTree(VegetationLibrary.Lib.conifers, parent, localPos, yaw, height > 0f ? height * 1.25f : 9f, solid);
            if (System.Array.IndexOf(BroadleafKenney, leaf) >= 0 && VegetationLibrary.HasBroadleaf)
                return SpawnRealTree(VegetationLibrary.Lib.broadleaf, parent, localPos, yaw, height > 0f ? height : 7f, solid);
            if (System.Array.IndexOf(BroadleafKenney, leaf) >= 0 && NatureKitAvailable)
                return SpawnNatureKitTree(parent, localPos, yaw, height > 0f ? height : 7f, solid);
            // Kenney 덤불(각진 초록 덩어리) → Nature Starter Kit 2 덤불
            if (leaf.StartsWith("plant_bush") && NatureKitAvailable)
                return SpawnBush(parent, localPos, yaw, height > 0f ? height : 1.2f);
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var size = LocalBounds(go).size;
            float s = 1f;
            if (height > 0f && size.y > 0.001f) s = height / size.y;
            else if (length > 0f) s = length / Mathf.Max(0.001f, Mathf.Max(size.x, size.z));
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            bool nature = path.Contains("/Nature/");
            bool city = path.Contains("/City/");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (nature) mats[i] = Recolor(mats[i]);
                    else if (city) mats[i] = CityGlow(mats[i]);
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = city ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
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

        /// <summary>Nature Starter Kit 2 활엽수 (위치로 종류를 고른다). 높이를 맞추고 줄기 콜라이더를 붙인다.</summary>
        static readonly string[] NatureKitBushes = { "bush01", "bush02", "bush03", "bush04", "bush05", "bush06" };

        /// <summary>
        /// Nature Starter Kit 2 덤불(tree면 나무)을 높이에 맞춰 놓는다 (구 프리미티브 덤불·나무 자리). 없으면 null.
        /// blossoms > 0이면 잎 덩어리 겉면에 작은 꽃·열매(blossom 재질, 지름 size m)를 흩어 붙인다 (진달래·동백·귤).
        /// </summary>
        public static GameObject SpawnBush(Transform parent, Vector3 localPos, float yaw, float height, int blossoms = 0, Material blossom = null, float size = 0.2f, bool tree = false)
        {
            if (!NatureKitAvailable) return null;
            if (tree && blossoms == 0 && VegetationLibrary.HasBroadleaf)
                return SpawnRealTree(VegetationLibrary.Lib.broadleaf, parent, localPos, yaw, height, false);
            var list = tree ? NatureKitTrees : System.Array.ConvertAll(NatureKitBushes, n => "NatureKit/Baked/" + n);
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

        static GameObject SpawnNatureKitTree(Transform parent, Vector3 localPos, float yaw, float height, bool solid)
        {
            int k = Mathf.Abs(Mathf.RoundToInt(localPos.x * 7.31f + localPos.z * 3.17f)) % NatureKitTrees.Length;
            var prefab = Resources.Load<GameObject>(NatureKitTrees[k]);
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
                cap.radius = b.size.y * 0.04f;
                cap.height = b.size.y * 0.6f;
                cap.center = new Vector3(0f, b.min.y + cap.height * 0.5f, 0f);
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

        static Material CityGlow(Material src)
        {
            if (_cityGlow != null) return _cityGlow;
            _cityGlow = new Material(src) { name = "KenneyCityGlow" };
            _cityGlow.EnableKeyword("_EMISSION");
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
