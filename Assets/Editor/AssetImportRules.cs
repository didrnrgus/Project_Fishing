using UnityEditor;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 외부 에셋(Quaternius, CC0) 임포트 규칙.
    ///  - Resources/Characters, Resources/Animations: 휴머노이드 리그 (애니메이션 리타깃)
    ///  - Resources/Characters/Angler_*.fbx: tools/blender/build_characters.py가 만든 낚시꾼(몸 + 옷 메쉬)
    ///  - ThirdParty/Quaternius/Source: 그 원본 Superhero FBX. 런타임에는 안 쓰므로 가볍게(애니메이션·머티리얼 없음)
    ///  - Resources/Animations/Mixamo: Mixamo FBX를 MixamoClips 표대로 쪼갠다 (낚시 동작)
    ///  - Resources/Animations/KevinIglesias: Human Basic Motions FREE의 대기·걷기·뛰기 (남·녀). 아바타는 패키지 모델(HumanM/F_Model)에서 복사
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
            else if (assetPath.Contains("Resources/Terrain/"))
            {
                // 지형 텍스처 (Poly Haven CC0): *_nor = 노멀(OpenGL)
                importer.textureType = assetPath.Contains("_nor") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
                importer.maxTextureSize = 1024;
                importer.anisoLevel = 4;
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
        /// Mixamo FBX(Resources/Animations/Mixamo, 30fps)를 쪼갠 클립. 프레임은 Unity 기준(0부터).
        /// Fishing Cast 한 개에 "들기 → 던지기 → 내리며 기다리기 → 릴 감기 → 처음 자세" 가 이어져 있다(docs/03 "애니메이션").
        /// </summary>
        static readonly (string file, string clip, int first, int last)[] MixamoClips =
        {
            ("FishingIdle", "Fishing_Idle_Loop", 0, 105),   // 낚싯대를 허리 앞에 든 대기 (전체)
            ("FishingCast", "Fishing_Windup", 0, 45),       // 어깨 너머로 들어 올리기 (끝 자세에서 멈춤)
            ("FishingCast", "Fishing_Hook", 0, 30),         // 챔질: 홱 들어 올리기 (빠르게 재생)
            ("FishingCast", "Fishing_Cast", 45, 95),        // 앞으로 던지고 팔을 뻗은 채 내리기
            ("FishingCast", "Fishing_Reel_Loop", 207, 238), // 릴 감기 한 바퀴 (포즈 차 2cm 구간)
            ("FishingCast", "Fishing_Sequence", 0, 263),    // 전체 (NPC가 가끔 통째로)
            ("Cheering", "Cheer_ArmsUp", 0, 87),            // 랜딩 환호: 두 팔 들고 흔들기
            ("Clapping", "Clap_Loop", 0, 35),               // 박수 (근처에서 대물을 낚으면 NPC가 친다)
        };

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
        };

        void OnPreprocessAnimation()
        {
            if (!assetPath.Contains("Resources/Animations/") && !assetPath.Contains("Resources/Fish/")) return;
            var importer = (ModelImporter)assetImporter;
            if (assetPath.Contains("Resources/Animations/Mixamo/"))
            {
                PreprocessMixamo(importer);
                return;
            }
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
                // Kevin Iglesias도 Mixamo처럼 +Z를 본다 → UAL(-Z)에 맞춘다
                c.rotationOffset = 180f;
                importer.clipAnimations = new[] { c };
            }
        }

        static void PreprocessMixamo(ModelImporter importer)
        {
            var defaults = importer.defaultClipAnimations;
            if (defaults.Length == 0) return;
            string file = System.IO.Path.GetFileNameWithoutExtension(importer.assetPath);
            var clips = new System.Collections.Generic.List<ModelImporterClipAnimation>();
            foreach (var (f, name, first, last) in MixamoClips)
            {
                if (f != file) continue;
                bool loop = name.Contains("Loop");
                clips.Add(new ModelImporterClipAnimation
                {
                    name = name,
                    takeName = defaults[0].takeName,
                    firstFrame = first,
                    lastFrame = last,
                    loopTime = loop,
                    loopPose = loop,
                    lockRootRotation = true,
                    lockRootHeightY = true,
                    lockRootPositionXZ = true,
                    keepOriginalOrientation = true,
                    // Mixamo는 +Z, UAL은 -Z를 본다(CharacterFactory가 모델을 180° 돌려 둠) → UAL 쪽에 맞춘다
                    rotationOffset = 180f,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true,
                });
            }
            if (clips.Count > 0) importer.clipAnimations = clips.ToArray();
        }
    }
}
