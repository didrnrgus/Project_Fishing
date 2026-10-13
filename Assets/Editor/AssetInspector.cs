using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>외부 에셋 임포트 결과(크기, 뼈 이름, 클립 이름)를 로그로 확인하는 도구.</summary>
    public static class AssetInspector
    {
        [MenuItem("Fishing/Inspect Imported Assets")]
        public static void Run()
        {
            foreach (var folder in new[] { "Assets/Resources/Characters", "Assets/Resources/Animations", "Assets/Resources/Fish", "Assets/Resources/Kenney" })
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            var sb = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Resources" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                sb.AppendLine($"== {path}  type={importer.animationType}  readable={importer.isReadable}");
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    var b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    sb.AppendLine($"   bounds size={b.size} center={b.center} renderers={renderers.Length}");
                    var mats = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct();
                    sb.AppendLine($"   mats: {string.Join(", ", mats.Select(m => $"{m.name}({m.shader.name}, tex={(m.mainTexture ? m.mainTexture.name : "-")}, col={m.color})"))}");
                }
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr != null)
                    sb.AppendLine($"   bones({smr.bones.Length}): {string.Join(",", smr.bones.Take(80).Select(t => t ? t.name : "null"))}  mats={smr.sharedMaterials.Length}");
                var animator = go.GetComponent<Animator>();
                if (animator != null) sb.AppendLine($"   avatar={(animator.avatar ? animator.avatar.name + " human=" + animator.avatar.isHuman + " valid=" + animator.avatar.isValid : "none")}");
                var clips = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<AnimationClip>().ToArray();
                if (clips.Length > 0)
                    sb.AppendLine($"   clips({clips.Length}): {string.Join(", ", clips.Select(c => $"{c.name}[{c.length:0.0}s{(c.isLooping ? ",loop" : "")}]"))}");
            }
            Debug.Log("[Inspect]\n" + sb);
            System.IO.File.WriteAllText("Logs/asset-inspect.txt", sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
