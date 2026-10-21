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
        const string AvatarFbx = "Assets/ThirdParty/StarterAssets/Armature.fbx";

        [MenuItem("Fishing/Bake Starter Assets Clips")]
        public static void Run() => Ensure(force: true);

        public static void Ensure(bool force = false)
        {
            if (!Directory.Exists(SrcDir)) return;
            Directory.CreateDirectory(DstDir);
            FixImportSettings();
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

        /// <summary>
        /// 클립 FBX가 Armature.fbx 아바타를 빌려 쓰고(원본 패키지와 같다), 점프 말고는 반복하게 맞춘다.
        /// 기본값(클립 FBX마다 자기 첫 프레임 뼈대로 아바타 생성)이면 근육값 기준이 T포즈가 아니어서 무릎이 최대 36° 뒤로 꺾이고 발끝이 옆을 본다.
        /// 반복이 꺼져 있으면 CharacterAnimator가 길이에서 멈춰 한 바퀴 뒤 굳는다.
        /// </summary>
        static void FixImportSettings()
        {
            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AvatarFbx))
                if (o is Avatar a) avatar = a;
            if (avatar == null) { Debug.LogError($"[StarterClip] {AvatarFbx} 아바타 없음"); return; }
            foreach (var fbx in Directory.GetFiles(SrcDir, "*.fbx"))
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(fbx.Replace('\\', '/'));
                bool loop = !fbx.Contains("Jump--Jump");
                var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
                bool ok = imp.avatarSetup == ModelImporterAvatarSetup.CopyFromOther && imp.sourceAvatar == avatar;
                foreach (var c in clips) ok &= c.loopTime == loop;
                if (ok) continue;
                imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                imp.sourceAvatar = avatar;
                foreach (var c in clips) c.loopTime = loop;
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
                Debug.Log($"[StarterClip] 임포트 설정 고침: {fbx} (아바타 {avatar.name}, 반복 {loop})");
            }
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
