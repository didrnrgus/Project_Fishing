using UnityEditor;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 외부 에셋 임포트 규칙. (캐릭터 모델은 에셋이 아니라 실행 중에 읽는 VRM — Characters/VrmCharacters)
    ///  - Resources/Animations: 휴머노이드 리그 (애니메이션 리타깃, 동작 클립만). FBX 안의 "Armature|이름" 테이크가 클립 "이름"
    ///    (UAL1_Standard = Quaternius CC0, Motions = tools/blender/build_motions.py로 만든 기본·반응 동작)
    ///  - 이름에 "Loop"가 들어간 클립은 반복 재생
    ///  - Resources/Fish: 레거시 애니메이션 (Animation 컴포넌트). Fish/Species는 build_fish.py가 만든 어종별 모델(클립 5개)
    ///  - Resources/Fish/Textures: *_n = 노멀 맵, *_m = 금속성·매끈함(선형), 나머지 = 알베도(알파 = 지느러미)
    ///  - Resources/Icons: UI 스프라이트 (투명 배경, 밉맵 없음). Items·Junk = build_icons.py, Fish = build_fish.py
    ///  - Resources/Junk: 잡동사니 정적 모델 (build_icons.py)
    ///  - Assets/Vegetation: 나무·덤불·바위·풀 카드 (build_vegetation.py, 재질·프리팹은 VegetationBake)
    /// </summary>
    public class AssetImportRules : AssetPostprocessor
    {
        static bool IsHumanoid(string path) => path.Contains("Resources/Animations/");

        void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;
            if (IsHumanoid(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.isReadable = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = true;
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
            else if (assetPath.Contains("Resources/Junk/") || assetPath.Contains("Resources/Props/"))
            {
                // 잡동사니 (tools/blender/build_icons.py): 정적 모델, 머티리얼은 FBX 색 그대로
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.addCollider = false;
            }
            else if (assetPath.StartsWith("Assets/Vegetation/"))
            {
                // 나무·덤불·바위 (tools/blender/build_vegetation.py): 재질·LOD는 Editor/VegetationBake가 오브젝트 이름(LOD0_재질)으로 붙인다
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.addCollider = false;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.isReadable = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
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
                // 텍스처는 2048×1024 (fish_tex.S = 2). 비늘 결·나이테가 노멀·금속성 맵에 있어서 줄이면 뭉개진다:
                // 알베도·노멀 2048, 금속성·매끈함 1024. 어종마다 쓸 때만 불러오므로(Resources) 메모리 부담은 그때뿐이다
                importer.maxTextureSize = assetPath.EndsWith("_m.png") ? 1024 : 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (assetPath.StartsWith("Assets/Vegetation/"))
            {
                // *_nor = 법선, 잎·십자 판·풀 카드 = 알파로 자르는 그림 (밉맵에서 잎이 줄지 않게 알파 덮개 보존)
                bool cutout = assetPath.Contains("/Leaves_") || assetPath.Contains("_billboard_") || assetPath.Contains("/Grass/");
                importer.textureType = assetPath.Contains("_nor") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.alphaIsTransparency = cutout;
                importer.mipMapsPreserveCoverage = cutout;
                importer.alphaTestReferenceValue = 0.4f;
                importer.wrapMode = cutout ? UnityEngine.TextureWrapMode.Clamp : UnityEngine.TextureWrapMode.Repeat;
                importer.maxTextureSize = assetPath.Contains("/Grass/") ? 512 : 1024;
            }
            else if (assetPath.Contains("Resources/Terrain/"))
            {
                // 지형 텍스처 (Poly Haven CC0): *_nor = 노멀(OpenGL)
                importer.textureType = assetPath.Contains("_nor") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
                importer.maxTextureSize = 1024;
                importer.anisoLevel = 4;
            }
            else if (assetPath.Contains("Resources/Intro/"))
            {
                // 인트로 그림 (tools/intro/build_intro_art.py). 화면 크기로 그리므로 줄이지 않고, 하늘 그러데이션은 압축하면 띠가 져서 그대로 둔다
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = assetPath.Contains("_mist") || assetPath.Contains("_wisps") ? UnityEngine.TextureWrapMode.Repeat : UnityEngine.TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = assetPath.Contains("bg_sky") || assetPath.Contains("company_paper") || assetPath.Contains("bg_water")
                    ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
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
