using UnityEditor;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 외부 에셋(Quaternius, CC0) 임포트 규칙.
    ///  - Resources/Characters, Resources/Animations: 휴머노이드 리그 (애니메이션 리타깃)
    ///  - Resources/Characters/Angler_*.fbx: tools/blender/build_characters.py가 만든 낚시꾼(몸 + 옷 메쉬)
    ///  - ThirdParty/Quaternius/Source: 그 원본 Superhero FBX. 런타임에는 안 쓰므로 가볍게(애니메이션·머티리얼 없음)
    ///  - 이름에 "Loop"가 들어간 클립은 반복 재생
    ///  - Resources/Fish: 레거시 애니메이션 (Animation 컴포넌트). Fish/Species는 build_fish.py가 만든 어종별 모델(클립 5개)
    ///  - Resources/Fish/Textures: *_n = 노멀 맵, *_m = 금속성·매끈함(선형), 나머지 = 알베도(알파 = 지느러미)
    ///  - Resources/Icons: UI 스프라이트 (투명 배경, 밉맵 없음). Items·Junk = build_icons.py, Fish = build_fish.py
    ///  - Resources/Junk: 잡동사니 정적 모델 (build_icons.py)
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
                if (assetPath.Contains("Resources/Fish/Species/"))
                {
                    importer.importBlendShapes = false;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.isReadable = false;
                    importer.optimizeMeshVertices = true;
                }
            }
            else if (assetPath.Contains("Resources/Junk/"))
            {
                // 잡동사니 (tools/blender/build_icons.py): 정적 모델, 머티리얼은 FBX 색 그대로
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.addCollider = false;
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

        void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (assetPath.Contains("Resources/Fish/Textures/"))
            {
                if (assetPath.EndsWith("_n.png"))
                    importer.textureType = TextureImporterType.NormalMap;
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = !assetPath.EndsWith("_m.png");
                    importer.alphaIsTransparency = false;
                }
                importer.mipmapEnabled = true;
                importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
                // 노멀·금속성은 작게 (빌드 크기): 알베도 1024, 노멀 512, 금속성 256
                importer.maxTextureSize = assetPath.EndsWith("_n.png") ? 512 : assetPath.EndsWith("_m.png") ? 256 : 1024;
            }
            else if (assetPath.Contains("Resources/Icons/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
                importer.maxTextureSize = 256;
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
