using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// Unity Starter Assets(Third Person) 로코모션 FBX(Assets/ThirdParty/StarterAssets/Animations)를
    /// Y축 180° 돌린 .anim으로 Resources/Animations/StarterAssets에 굽는다 (CharacterAnimator가 이름으로 찾는다).
    /// Starter 클립은 +Z를 보지만 우리 동작 클립은 −Z를 보고 VRM을 180° 돌려 둔다 (VrmCharacters) — 임포트 설정의
    /// Root Transform Rotation Offset은 Playables 재생에서 지워져서, 몸 곡선(RootQ·RootT) 자체를 돌린다.
    /// BuildWindows가 빌드 전에 부른다. 메뉴: Fishing > Bake Starter Assets Clips
    /// </summary>
    public static class StarterClipBake
    {
        const string SrcDir = "Assets/ThirdParty/StarterAssets/Animations";
        const string DstDir = "Assets/Resources/Animations/StarterAssets";

        [MenuItem("Fishing/Bake Starter Assets Clips")]
        public static void Run() => Ensure(force: true);

        public static void Ensure(bool force = false)
        {
            if (!Directory.Exists(SrcDir)) return;
            Directory.CreateDirectory(DstDir);
            foreach (var fbx in Directory.GetFiles(SrcDir, "*.fbx"))
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbx.Replace('\\', '/')))
                {
                    if (!(obj is AnimationClip src) || src.name.StartsWith("__preview__")) continue;
                    var path = $"{DstDir}/{src.name}.anim";
                    if (!force && File.Exists(path) && File.GetLastWriteTimeUtc(path) > File.GetLastWriteTimeUtc(fbx + ".meta")) continue;
                    var dst = Rotated(src);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(dst, path);
                    Debug.Log($"[StarterClip] {path} ({src.length:F2}s)");
                }
            AssetDatabase.SaveAssets();
        }

        /// <summary>q' = (Y 180°) · q → (x,y,z,w) = (z, w, −x, −y), 위치는 (−x, y, −z).</summary>
        static AnimationClip Rotated(AnimationClip src)
        {
            var dst = new AnimationClip { name = src.name, frameRate = src.frameRate };
            foreach (var b in AnimationUtility.GetCurveBindings(src))
            {
                var curve = AnimationUtility.GetEditorCurve(src, b);
                string prop = b.propertyName;
                if (b.path == "" && prop.StartsWith("RootQ."))
                {
                    string from = prop switch { "RootQ.x" => "RootQ.z", "RootQ.y" => "RootQ.w", "RootQ.z" => "RootQ.x", _ => "RootQ.y" };
                    curve = AnimationUtility.GetEditorCurve(src, EditorCurveBinding.FloatCurve("", b.type, from));
                    if (prop == "RootQ.z" || prop == "RootQ.w") curve = Negated(curve);
                }
                else if (b.path == "" && (prop == "RootT.x" || prop == "RootT.z")) curve = Negated(curve);
                AnimationUtility.SetEditorCurve(dst, b, curve);
            }
            AnimationUtility.SetAnimationClipSettings(dst, AnimationUtility.GetAnimationClipSettings(src));
            return dst;
        }

        static AnimationCurve Negated(AnimationCurve c)
        {
            var keys = c.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].value = -keys[i].value;
                keys[i].inTangent = -keys[i].inTangent;
                keys[i].outTangent = -keys[i].outTangent;
            }
            return new AnimationCurve(keys) { preWrapMode = c.preWrapMode, postWrapMode = c.postWrapMode };
        }
    }
}
