using System.IO;
using System.Linq;
using Fishing.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 프로젝트 셋업/빌드 도구. 배치모드에서 -executeMethod로도 호출할 수 있다.
    ///   Fishing.EditorTools.ProjectTools.Setup     메인 씬 생성, 빌드 설정, 셰이더 포함
    ///   Fishing.EditorTools.ProjectTools.BuildWindows   Builds/Windows/ProjectFishing.exe
    /// </summary>
    public static class ProjectTools
    {
        public const string ScenePath = "Assets/Scenes/HanRiver.unity";

        static readonly string[] RuntimeShaders =
        {
            "Standard", "Unlit/Color", "Skybox/Procedural", "Legacy Shaders/Particles/Alpha Blended", "UI/Default", "Fishing/Water", "Fishing/Sky", "Fishing/SkyCube", "Fishing/Fish", "Fishing/FishFin", "Fishing/Ground", "Fishing/Weathered", "Fishing/Karst", "Fishing/Landmark", "Fishing/Forest", "Fishing/Waterfall", "Fishing/Grass", "Fishing/UIInkReveal", "Fishing/FishShadow", "Fishing/BioGlow", "Fishing/Terrain", "Hidden/Fishing/Terrain-AddPass", "Nature/Terrain/Standard",
            // VRM 캐릭터 (UniVRM이 실행 중에 Shader.Find로 머티리얼을 만든다)
            "VRM10/MToon10", "UniGLTF/UniUnlit"
        };

        [MenuItem("Fishing/Setup Project")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Game").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AlwaysIncludeShaders();
            EnsurePostFx();

            PlayerSettings.productName = "Project Fishing - 청풍명월 솔로";
            PlayerSettings.companyName = "didrnrgus";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            // Unity 스플래시: 검은 바탕에 흰 로고 → 이어지는 사업자 인트로(IntroSequence)도 검은 화면에서 시작한다
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.backgroundColor = Color.black;
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] 완료: " + ScenePath);
        }

        /// <summary>코드에서 Shader.Find로 쓰는 셰이더가 빌드에서 빠지지 않도록 Always Included에 넣는다.</summary>
        static void AlwaysIncludeShaders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in RuntimeShaders)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("[Setup] 셰이더 없음: " + name); continue; }
                bool exists = Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == shader);
                if (exists) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        const string PostFxConfigPath = "Assets/Resources/PostFxConfig.asset";

        /// <summary>후처리 리소스를 Resources에서 찾을 수 있게 참조 에셋을 만든다 (빌드에 셰이더가 들어가게).</summary>
        static void EnsurePostFx()
        {
            var res = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>(
                "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
            if (res == null) { Debug.LogWarning("[Setup] Post Processing 패키지가 없습니다."); return; }
            var cfg = AssetDatabase.LoadAssetAtPath<PostFxConfig>(PostFxConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<PostFxConfig>();
                AssetDatabase.CreateAsset(cfg, PostFxConfigPath);
            }
            if (cfg.resources != res)
            {
                cfg.resources = res;
                EditorUtility.SetDirty(cfg);
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Fishing/Build Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) Setup();
            else AlwaysIncludeShaders();
            EnsurePostFx();
            FontSetup.Ensure();
            VegetationBake.Ensure();
            StarterClipBake.Ensure();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/ProjectFishing.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) CopyGuide("Builds/Windows/ProjectFishing_Data/StreamingAssets/Guide");
            Debug.Log($"[Build] {report.summary.result} · {report.summary.totalSize / (1024 * 1024)}MB · 에러 {report.summary.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>플레이 가이드(docs/guide/*.md)를 빌드의 StreamingAssets/Guide로: 윤슬(Buddy.GameKnowledge)이 읽는다. 저장소에는 한 벌만 둔다.</summary>
        static void CopyGuide(string dest)
        {
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.CreateDirectory(dest);
            foreach (var f in Directory.GetFiles("docs/guide", "*.md"))
                File.Copy(f, Path.Combine(dest, Path.GetFileName(f)));
            Debug.Log($"[Build] 가이드 {Directory.GetFiles(dest).Length}개 → {dest}");
        }

        [MenuItem("Fishing/Delete Save")]
        public static void DeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log("[Save] 세이브 파일을 삭제했습니다.");
        }
    }
}
