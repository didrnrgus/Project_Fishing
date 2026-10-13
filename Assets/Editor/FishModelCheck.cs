using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 어종별 물고기 모델(Resources/Fish/Species) 임포트 결과 확인: 렌더러 이름, 크기·방향(머리 = +Z), 뼈, 클립.
    /// 메뉴 Fishing/Check Fish Models 또는 -executeMethod Fishing.EditorTools.FishModelCheck.Run → Logs/fish-check.txt
    /// </summary>
    public static class FishModelCheck
    {
        /// <summary>임포트 규칙(AssetImportRules)을 바꾼 뒤 물고기 모델·텍스처·아이콘을 다시 임포트한다.</summary>
        [MenuItem("Fishing/Reimport Fish Assets")]
        public static void Reimport()
        {
            foreach (var dir in new[] { "Assets/Resources/Fish/Species", "Assets/Resources/Fish/Textures", "Assets/Resources/Icons" })
                foreach (var guid in AssetDatabase.FindAssets("", new[] { dir }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            Debug.Log("[FishCheck] 다시 임포트 완료");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Fishing/Check Fish Models")]
        public static void Run()
        {
            var sb = new StringBuilder();
            foreach (var prefab in Resources.LoadAll<GameObject>("Fish/Species").OrderBy(p => p.name))
            {
                var go = Object.Instantiate(prefab);
                var anim = go.GetComponentInChildren<Animation>();
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var b = new Bounds();
                bool first = true;
                float headZ = 0f, tailZ = 0f;
                foreach (var smr in smrs)
                {
                    var m = new Mesh();
                    smr.BakeMesh(m, true);
                    var verts = m.vertices.Select(v => smr.transform.TransformPoint(v)).ToArray();
                    foreach (var v in verts)
                    {
                        if (first) { b = new Bounds(v, Vector3.zero); first = false; }
                        else b.Encapsulate(v);
                    }
                    if (smr.name == "Body" && verts.Length > 0)
                    {
                        // 눈은 머리 쪽에 있다: 가장 높은 곳 근처가 아니라 x가 가장 큰 버텍스들의 z 평균으로 머리 방향을 본다
                        headZ = verts.OrderBy(v => v.z).First().z;
                        tailZ = verts.OrderBy(v => v.z).Last().z;
                    }
                    Object.DestroyImmediate(m);
                }
                sb.AppendLine($"{prefab.name}: 렌더러 [{string.Join(", ", smrs.Select(s => $"{s.name}({s.sharedMesh?.triangles.Length / 3}tri, bones {s.bones.Length})"))}]");
                sb.AppendLine($"  크기 {b.size.x:0.000} × {b.size.y:0.000} × {b.size.z:0.000}, 중심 {b.center}, Body z {headZ:0.00}~{tailZ:0.00}, root rot {go.transform.GetChild(0).localEulerAngles}");
                sb.AppendLine($"  클립: {(anim != null ? string.Join(", ", anim.Cast<AnimationState>().Select(s => $"{s.name}({s.length:0.00}s)")) : "없음")}");
                Object.DestroyImmediate(go);
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/fish-check.txt", sb.ToString());
            Debug.Log("[FishCheck]\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
