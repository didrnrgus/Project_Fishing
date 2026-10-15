using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// Nature Starter Kit 2 나무(Tree Creator)를 런타임에 쓸 수 있게 굽는다.
    /// Tree Creator 프리팹은 메쉬·머티리얼을 에디터 전용 TreeData가 붙여 주므로 빌드에서는 빈 MeshFilter만 남는다.
    /// 그래서 프리팹(NatureStarterKit2/Nature) 안의 메쉬·머티리얼 하위 에셋을 Resources/NatureKit/Baked/로 복사하고 MeshFilter·MeshRenderer만 있는 프리팹을 만든다.
    /// BuildWindows가 빌드 전에 부른다 (이미 있으면 건너뛴다). 메뉴: Fishing > Bake Nature Kit Trees
    /// </summary>
    public static class NatureKitBake
    {
        const string Src = "Assets/NatureStarterKit2/Nature";
        const string Dst = "Assets/Resources/NatureKit/Baked";

        [MenuItem("Fishing/Bake Nature Kit Trees")]
        public static void Run() => Bake(true);

        public static void Ensure() => Bake(false);

        static void Bake(bool force)
        {
            if (!Directory.Exists(Src)) return;
            Directory.CreateDirectory(Dst);
            foreach (var path in Directory.GetFiles(Src, "*.prefab"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string outPrefab = $"{Dst}/{name}.prefab";
                if (!force && File.Exists(outPrefab)) continue;
                var assets = AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/'));
                var mesh = assets.OfType<Mesh>().FirstOrDefault();
                var mats = assets.OfType<Material>().ToArray();
                if (mesh == null || mats.Length == 0) { Debug.LogWarning($"[NatureKitBake] {name}: 메쉬/머티리얼 없음"); continue; }

                var meshCopy = Object.Instantiate(mesh);
                AssetDatabase.CreateAsset(meshCopy, $"{Dst}/{name}_mesh.asset");
                // 서브메쉬 순서: 줄기(Bark) → 잎(Leaf)
                var ordered = mats.OrderBy(m => m.name.Contains("Leaf") ? 1 : 0).ToArray();
                var matCopies = new Material[ordered.Length];
                for (int i = 0; i < ordered.Length; i++)
                {
                    matCopies[i] = new Material(ordered[i]) { name = $"{name}_{(ordered[i].name.Contains("Leaf") ? "leaf" : "bark")}" };
                    AssetDatabase.CreateAsset(matCopies[i], $"{Dst}/{matCopies[i].name}.mat");
                }
                var go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = meshCopy;
                go.AddComponent<MeshRenderer>().sharedMaterials = matCopies.Take(Mathf.Max(1, meshCopy.subMeshCount)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(go, outPrefab);
                Object.DestroyImmediate(go);
                Debug.Log($"[NatureKitBake] {name}: {meshCopy.vertexCount} verts, submesh {meshCopy.subMeshCount}");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
