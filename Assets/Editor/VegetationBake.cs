using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fishing.World;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// tools/blender/build_vegetation.py가 만든 Assets/Vegetation(나무·덤불 = ez-tree, 바위·풀 = Poly Haven)으로
    /// 재질·LOD 프리팹을 만들고 Resources/VegetationLibrary.asset에 묶는다 (런타임이 Resources.Load로 쓰고, 참조라서 빌드에 들어간다).
    /// 덤불·작은 나무는 KenneyProps가 이름으로 찾도록 Resources/Plants/bush01~06·tree01~04에도 둔다.
    /// BuildWindows가 빌드 전에 부른다. 메뉴: Fishing > Build Vegetation Library
    /// FBX 오브젝트 이름 "LOD0_재질"·"LOD1_재질"로 LODGroup과 재질을 정한다. 2026-10-21까지는 에셋 스토어 식생(Conifers [BOTD]·Realistic Tree 9·10·
    /// Nature Starter Kit 2·Rock and Boulders 2·Grass Flowers Pack Free)이었다.
    /// </summary>
    public static class VegetationBake
    {
        const string LibPath = "Assets/Resources/VegetationLibrary.asset";
        const string Root = "Assets/Vegetation";
        const string PrefabDir = Root + "/Prefabs";
        const string MatDir = Root + "/Materials";
        const string TexDir = Root + "/Textures";
        const string PlantsDir = "Assets/Resources/Plants";

        /// <summary>LOD 바뀌는 화면 높이 비율: (LOD0 → 십자 판·낮은 LOD, 사라짐).</summary>
        static readonly Dictionary<string, (float lod0, float cull)> Lod = new Dictionary<string, (float, float)>
        {
            { "Conifers", (0.12f, 0.012f) }, { "Broadleaf", (0.12f, 0.012f) }, { "Bushes", (0.07f, 0.008f) }, { "Rocks", (0.035f, 0.004f) },
        };

        [MenuItem("Fishing/Build Vegetation Library")]
        public static void Run() => Ensure();

        public static void Ensure()
        {
            if (!Directory.Exists(Root))
            {
                Debug.LogWarning("[Vegetation] Assets/Vegetation 없음 — 먼저 tools/blender/build_vegetation.py");
                return;
            }
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PlantsDir);
            var lib = AssetDatabase.LoadAssetAtPath<VegetationLibrary>(LibPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VegetationLibrary>();
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            var conifers = Build("Conifers", false);
            var broadleaf = Build("Broadleaf", false);
            var bushes = Build("Bushes", false);
            lib.conifers = conifers.ToArray();
            lib.broadleaf = broadleaf.Where(p => p.name.StartsWith("Broadleaf")).ToArray();
            lib.rocks = Build("Rocks", false).ToArray();
            lib.snowRocks = Build("Rocks", true).ToArray();
            // KenneyProps.Plant*: 덤불 6종(bush01~06), 꽃·열매를 다는 작은 나무 4종(tree01~04)
            Copy(bushes.OrderBy(p => p.name).ToList(), "bush");
            Copy(broadleaf.Where(p => p.name.StartsWith("Small")).OrderBy(p => p.name).ToList(), "tree");
            lib.grass = Cards("grass0");
            lib.flowers = Cards("grassFlower");
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vegetation] 침엽수 {lib.conifers.Length}, 활엽수 {lib.broadleaf.Length}, 덤불 {bushes.Count}, 바위 {lib.rocks.Length}(눈 {lib.snowRocks.Length}), 풀 {lib.grass.Length}, 들꽃 {lib.flowers.Length}");
        }

        static List<GameObject> Build(string folder, bool snow)
        {
            var list = new List<GameObject>();
            string dir = $"{Root}/{folder}";
            if (!Directory.Exists(dir)) return list;
            foreach (var fbx in Directory.GetFiles(dir, "*.fbx").Select(f => f.Replace('\\', '/')).OrderBy(f => f))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (model == null) continue;
                string name = Path.GetFileNameWithoutExtension(fbx);
                var meta = ReadMeta($"{dir}/{name}.json");
                var go = Object.Instantiate(model);
                go.name = name + (snow ? "_snow" : "");
                float height = Mathf.Max(0.01f, Bounds(go).size.y);
                var lod0 = new List<Renderer>();
                var lod1 = new List<Renderer>();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    string n = r.gameObject.name;
                    string role = n.Length > 5 ? n.Substring(5) : n;
                    r.sharedMaterial = Material(folder, name, role, meta, height, snow);
                    (n.StartsWith("LOD1") ? lod1 : lod0).Add(r);
                }
                var (h0, cull) = Lod[folder];
                var group = go.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(h0, lod0.ToArray()), new LOD(cull, lod1.ToArray()) });
                group.RecalculateBounds();
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{go.name}.prefab");
                Object.DestroyImmediate(go);
                if (prefab != null) list.Add(prefab);
            }
            return list;
        }

        static void Copy(List<GameObject> src, string prefix)
        {
            for (int i = 0; i < src.Count; i++)
            {
                var go = Object.Instantiate(src[i]);
                PrefabUtility.SaveAsPrefabAsset(go, $"{PlantsDir}/{prefix}{i + 1:00}.prefab");
                Object.DestroyImmediate(go);
            }
        }

        class Meta { public string bark, leaves; }

        static Meta ReadMeta(string path) => File.Exists(path) ? JsonUtility.FromJson<Meta>(File.ReadAllText(path)) : new Meta();

        /// <summary>재질: 껍질·잎은 나무 종류마다(잎은 바람 높이 때문에 나무마다), 십자 판·바위는 모델마다. 눈 바위는 따로.</summary>
        static Material Material(string folder, string name, string role, Meta meta, float height, bool snow)
        {
            string dir = $"{Root}/{folder}";
            switch (role)
            {
                case "bark":
                    return Mat($"Bark_{meta.bark}", "Fishing/Rock", Tex($"{TexDir}/Bark_{meta.bark}_diff.png"), Tex($"{TexDir}/Bark_{meta.bark}_nor.png"),
                        m => m.SetFloat("_Smoothness", 0.08f));
                case "leaves":
                    return Mat($"{name}_leaves", "Fishing/Foliage", Tex($"{TexDir}/Leaves_{meta.leaves}.png"), null, m =>
                    {
                        m.SetFloat("_WindHeight", height);
                        m.SetFloat("_Wind", height * (folder == "Bushes" ? 0.012f : 0.006f));
                    });
                case "billboard":
                    return Mat($"{name}_billboard", "Fishing/Foliage", Tex($"{dir}/{name}_billboard_diff.png"), null, m => m.SetFloat("_Wind", 0f));
                default: // rock
                    return Mat($"{name}_rock" + (snow ? "_snow" : ""), "Fishing/Rock", Tex($"{dir}/{name}_rock_diff.png"), Tex($"{dir}/{name}_rock_nor.png"),
                        m => m.SetFloat("_Snow", snow ? 0.85f : 0f));
            }
        }

        static Material Mat(string name, string shader, Texture2D albedo, Texture2D normal, System.Action<Material> setup)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find(shader);
            mat.mainTexture = albedo;
            if (normal != null) mat.SetTexture("_BumpMap", normal);
            setup?.Invoke(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        static Texture2D[] Cards(string prefix)
        {
            string dir = $"{Root}/Grass";
            if (!Directory.Exists(dir)) return new Texture2D[0];
            return Directory.GetFiles(dir, prefix + "*.png").OrderBy(f => f).Select(f => Tex(f.Replace('\\', '/'))).Where(t => t != null).ToArray();
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
