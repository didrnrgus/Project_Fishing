using UnityEditor;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 외부 에셋 임포트 규칙. (캐릭터 모델은 에셋이 아니라 실행 중에 읽는 VRM — Characters/VrmCharacters)
    ///  - Resources/Animations: 휴머노이드 리그 (애니메이션 리타깃, 동작 클립만). FBX 안의 "Armature|이름" 테이크가 클립 "이름"
    ///    (UAL1_Standard = Quaternius CC0, Reactions = tools/blender/build_reactions.py로 만든 반응 동작)
    ///  - Resources/Animations/KevinIglesias: Human Basic Motions FREE의 대기·걷기·뛰기 (남·녀). 아바타는 패키지 모델(HumanM/F_Model)에서 복사
    ///  - 이름에 "Loop"가 들어간 클립은 반복 재생
    ///  - Resources/Fish: 레거시 애니메이션 (Animation 컴포넌트). Fish/Species는 build_fish.py가 만든 어종별 모델(클립 5개)
    ///  - Resources/Fish/Textures: *_n = 노멀 맵, *_m = 금속성·매끈함(선형), 나머지 = 알베도(알파 = 지느러미)
    ///  - Resources/Icons: UI 스프라이트 (투명 배경, 밉맵 없음). Items·Junk = build_icons.py, Fish = build_fish.py
    ///  - Resources/Junk: 잡동사니 정적 모델 (build_icons.py)
    /// </summary>
    public class AssetImportRules : AssetPostprocessor
    {
        static bool IsHumanoid(string path) => path.Contains("Resources/Animations/");

        void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;
            if (assetPath.Contains("Resources/Animations/KevinIglesias/"))
            {
                // 아바타 설정(Copy From Other → Kevin Iglesias/Human Animations/Models)은 패키지 그대로 둔다
                importer.animationType = ModelImporterAnimationType.Human;
                importer.isReadable = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = true;
            }
            else if (IsHumanoid(assetPath))
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

        /// <summary>
        /// Human Basic Motions FREE(Kevin Iglesias, Resources/Animations/KevinIglesias) 파일 → 클립 이름. 파일 끝 _M/_F가 남·녀.
        /// CharacterAnimator가 "이름_M"/"이름_F"를 캐릭터 성별로 골라 쓴다.
        /// </summary>
        static readonly (string file, string clip)[] KevinClips =
        {
            ("Idle", "Basic_Idle_Loop"),     // HumanM/F@Idle01: 팔을 내린 자연스러운 대기
            ("Walk", "Basic_Walk_Loop"),     // Walk01_Forward: 0.8초 한 걸음 쌍, 원래 2.0m/s
            ("Run", "Basic_Run_Loop"),       // Run01_Forward: 0.6초, 원래 4.0m/s (게임 걷기 4.5m/s)
            ("Sprint", "Basic_Sprint_Loop"), // Sprint01_Forward: 0.52초, 원래 6.0m/s (게임 달리기 7.5m/s)
            ("Talk", "Basic_Talk_Loop"),     // Talk01: 손짓하며 말하기 (NPC 대화 상대)
            ("JumpBegin", "Basic_JumpBegin"),// Jump01 - Begin: 웅크렸다 뛰어오르기 (한 번)
            ("Fall", "Basic_Fall_Loop"),     // Fall01: 공중에서 떨어지는 자세
            ("TurnLeft", "Basic_TurnLeft_Loop"),   // Turn01_Left: 제자리에서 발을 옮기며 왼쪽으로 돌기 (몸 방향은 코드가 돌린다)
            ("TurnRight", "Basic_TurnRight_Loop"), // Turn01_Right
        };

        void OnPreprocessAnimation()
        {
            if (!assetPath.Contains("Resources/Animations/") && !assetPath.Contains("Resources/Fish/")) return;
            var importer = (ModelImporter)assetImporter;
            if (assetPath.Contains("Resources/Animations/KevinIglesias/"))
            {
                PreprocessKevin(importer);
                return;
            }
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

        static void PreprocessKevin(ModelImporter importer)
        {
            var defaults = importer.defaultClipAnimations;
            if (defaults.Length == 0) return;
            string file = System.IO.Path.GetFileNameWithoutExtension(importer.assetPath); // Walk_M
            int us = file.LastIndexOf('_');
            if (us < 0) return;
            string kind = file.Substring(0, us), gender = file.Substring(us);
            foreach (var (f, name) in KevinClips)
            {
                if (f != kind) continue;
                var c = defaults[0];
                c.name = name + gender;
                bool loop = name.Contains("Loop");
                c.loopTime = loop;
                c.loopPose = loop;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
                // Kevin Iglesias는 +Z를 본다 → UAL(-Z)에 맞춘다
                c.rotationOffset = 180f;
                importer.clipAnimations = new[] { c };
            }
        }
    }
}
