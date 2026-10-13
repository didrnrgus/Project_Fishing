using UnityEditor;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 외부 에셋(Quaternius, CC0) 임포트 규칙.
    ///  - Resources/Characters, Resources/Animations: 휴머노이드 리그 (애니메이션 리타깃)
    ///  - Resources/Characters/Angler_*.fbx: tools/blender/build_characters.py가 만든 낚시꾼(몸 + 옷 메쉬)
    ///  - ThirdParty/Quaternius/Source: 그 원본 Superhero FBX. 런타임에는 안 쓰므로 가볍게(애니메이션·머티리얼 없음)
    ///  - 이름에 "Loop"가 들어간 클립은 반복 재생
    ///  - Resources/Fish: 레거시 애니메이션 (헤엄 클립 하나라서 Animation 컴포넌트로 충분)
    /// </summary>
    public class AssetImportRules : AssetPostprocessor
    {
        static bool IsHumanoid(string path) =>
            path.Contains("Resources/Characters/") && !path.Contains("/Hair/") || path.Contains("Resources/Animations/");

        void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;
            if (IsHumanoid(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.isReadable = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = assetPath.Contains("Resources/Animations/");
            }
            else if (assetPath.Contains("Resources/Characters/Hair/"))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
            }
            else if (assetPath.Contains("ThirdParty/Quaternius/Source/"))
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
            }
            else if (assetPath.Contains("Resources/Fish/"))
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
            }
            else if (assetPath.Contains("Resources/Kenney/"))
            {
                // Kenney CC0 정적 모델: 애니메이션 없음, 머티리얼은 FBX 설명대로(colormap 텍스처 또는 단색)
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.addCollider = false;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.Contains("Resources/Animations/") && !assetPath.Contains("Resources/Fish/")) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                bool loop = clip.name.Contains("Loop") || assetPath.Contains("Resources/Fish/");
                clip.loopTime = loop;
                clip.loopPose = loop;
                if (clip.name.Contains("|")) clip.name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
