using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fishing.World;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 받은 실사풍 식생·바위 에셋을 Resources/VegetationLibrary.asset에 묶는다 (런타임이 Resources.Load로 쓰고, 참조라서 빌드에 들어간다).
    /// BuildWindows가 NatureKitBake 뒤에 부른다. 메뉴: Fishing > Build Vegetation Library
    /// 풀 그림은 임포트 설정도 맞춘다 (투명 가장자리, Clamp, 밉맵 알파 보존, 512px).
    /// </summary>
    public static class VegetationBake
    {
        const string LibPath = "Assets/Resources/VegetationLibrary.asset";
        const string ConiferDir = "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/BIRP/Prefabs";
        const string RockDir = "Assets/Rocks and Boulders 2/Rocks/Prefabs";
        const string SnowRockDir = "Assets/Rocks and Boulders 2/Rocks/Prefabs_snow";
        const string BakedDir = "Assets/Resources/NatureKit/Baked";
        const string GrassDir = "Assets/ALP_Assets/GrassFlowersFREE/Textures/GrassFlowers";
        // 단일 바위 (무리·LOD 무리 프리팹은 뺀다)
        static readonly string[] RockNames = { "Rock1A", "Rock1B", "Rock1C", "Rock1D", "Rock1E", "Rock2", "Rock3", "Rock4A", "Rock4B", "Rock5A", "Rock5B", "Rock6A", "Rock6B", "Rock6C" };

        [MenuItem("Fishing/Build Vegetation Library")]
        public static void Run() => Ensure();

        public static void Ensure()
        {
            var lib = AssetDatabase.LoadAssetAtPath<VegetationLibrary>(LibPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VegetationLibrary>();
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            // 소나무: 키 큰·중간·작은 (Bare는 죽은 나무라 뺀다)
            lib.conifers = new[] { "Tall", "Medium", "Small" }
                .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"{ConiferDir}/PF Conifer {n} BOTD.prefab")).Where(g => g != null).ToArray();
            lib.broadleaf = Directory.Exists(BakedDir)
                ? Directory.GetFiles(BakedDir, "Tree*.prefab").Select(Path.GetFileName).Where(f => f.StartsWith("Tree9") || f.StartsWith("Tree10"))
                    .OrderBy(f => f).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>($"{BakedDir}/{f}")).Where(g => g != null).ToArray()
                : new GameObject[0];
            lib.rocks = RockNames.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"{RockDir}/{n}.prefab")).Where(g => g != null).ToArray();
            lib.snowRocks = RockNames.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"{SnowRockDir}/{n}.prefab")).Where(g => g != null).ToArray();
            lib.grass = LoadGrass("grass0");
            lib.flowers = LoadGrass("grassFlower");
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vegetation] 소나무 {lib.conifers.Length}, 활엽수 {lib.broadleaf.Length}, 바위 {lib.rocks.Length}(눈 {lib.snowRocks.Length}), 풀 {lib.grass.Length}, 들꽃 {lib.flowers.Length}");
        }

        static Texture2D[] LoadGrass(string prefix)
        {
            if (!Directory.Exists(GrassDir)) return new Texture2D[0];
            var list = new List<Texture2D>();
            foreach (var f in Directory.GetFiles(GrassDir, prefix + "*.tga").OrderBy(f => f))
            {
                string path = f.Replace('\\', '/');
                if (AssetImporter.GetAtPath(path) is TextureImporter ti)
                {
                    bool changed = !ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp || !ti.mipMapsPreserveCoverage || ti.maxTextureSize != 512;
                    if (changed)
                    {
                        ti.alphaIsTransparency = true;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.mipMapsPreserveCoverage = true;
                        ti.alphaTestReferenceValue = 0.45f;
                        ti.maxTextureSize = 512;
                        ti.SaveAndReimport();
                    }
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null) list.Add(tex);
            }
            return list.ToArray();
        }
    }
}
