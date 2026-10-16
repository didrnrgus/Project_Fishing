using System.IO;
using System.Linq;
using Fishing.Characters;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 받은 캐릭터를 Resources/CharacterLibrary.asset에 묶고(ExternalCharacters.Registry 순서), 필요한 준비를 한다. BuildWindows가 부른다.
    /// - Mixamo 캐릭터 FBX는 4K 텍스처가 FBX 안에 들어 있다 → Assets/MixamoCharacters/Textures/<이름>/로 꺼내면
    ///   재질이 꺼낸 텍스처를 쓰고 AssetImportRules가 1024로 줄인다 (꺼낸 텍스처는 .gitignore, 없으면 여기서 다시 꺼낸다).
    /// - Assets/MixamoAnimation/Extra의 동작은 클립만 Resources/Animations/MixamoExtra/<클립>.anim으로 복사한다
    ///   (원본 FBX에 몸·텍스처가 들어 있어 Resources에 두면 빌드가 무거워진다).
    /// 메뉴: Fishing > Build Character Library
    /// </summary>
    public static class CharacterLibraryBake
    {
        const string LibPath = "Assets/Resources/CharacterLibrary.asset";
        const string ClipSrc = "Assets/MixamoAnimation/Extra";
        const string ClipDst = "Assets/Resources/Animations/MixamoExtra";

        [MenuItem("Fishing/Build Character Library")]
        public static void Run() => Ensure();

        public static void Ensure()
        {
            BakeClips();
            var lib = AssetDatabase.LoadAssetAtPath<CharacterLibrary>(LibPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<CharacterLibrary>();
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            lib.entries.Clear();
            foreach (var r in ExternalCharacters.Registry)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(r.path);
                if (go == null && r.fallback != null) go = AssetDatabase.LoadAssetAtPath<GameObject>(r.fallback);
                if (go == null) continue;
                if (r.path.StartsWith("Assets/MixamoCharacters/") && !r.path.EndsWith("XBot.fbx")) ExtractTextures(r.path);
                lib.entries.Add(new CharacterLibrary.Entry { id = r.id, prefab = go });
                var an = go.GetComponentInChildren<Animator>();
                var av = an != null ? an.avatar : AssetDatabase.LoadAllAssetsAtPath(r.path).OfType<Avatar>().FirstOrDefault();
                if (av == null || !av.isValid || !av.isHuman) Debug.LogWarning($"[Characters] {r.id}: 휴머노이드 아바타 없음/잘못됨 (동작이 안 붙는다)");
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Characters] {lib.entries.Count}/{ExternalCharacters.Registry.Length}: {string.Join(", ", lib.entries.Select(e => e.id))}");
        }

        static void ExtractTextures(string fbx)
        {
            string name = Path.GetFileNameWithoutExtension(fbx).Replace(" ", "").Replace(".", "");
            string dir = $"Assets/MixamoCharacters/Textures/{name}";
            if (Directory.Exists(dir) && Directory.GetFiles(dir).Any(f => !f.EndsWith(".meta"))) return;
            if (!(AssetImporter.GetAtPath(fbx) is ModelImporter mi)) return;
            Directory.CreateDirectory(dir);
            if (mi.ExtractTextures(dir))
            {
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[Characters] 텍스처 꺼냄: {name}");
            }
        }

        static void BakeClips()
        {
            if (!Directory.Exists(ClipSrc)) return;
            Directory.CreateDirectory(ClipDst);
            foreach (var f in Directory.GetFiles(ClipSrc, "*.fbx"))
            {
                string path = f.Replace('\\', '/');
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    if (clip.name.StartsWith("__preview__")) continue;
                    string dst = $"{ClipDst}/{clip.name}.anim";
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(dst);
                    var copy = new AnimationClip();
                    EditorUtility.CopySerialized(clip, copy);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(copy, existing);
                        EditorUtility.SetDirty(existing);
                    }
                    else AssetDatabase.CreateAsset(copy, dst);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
