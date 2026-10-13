using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 여기어때 잘난체2(Jalnan2) TTF로 TextMeshPro 다이나믹 폰트 에셋을 만들고 TMP 기본 글꼴로 지정한다.
    /// 다이나믹이라 실제로 화면에 나온 글자만 아틀라스에 채워진다(빌드 때는 비워서 포함).
    /// 에셋이 없으면 에디터가 켜질 때 자동으로 만들고, 메뉴로 다시 만들 수도 있다.
    ///   Resources/Fonts/Jalnan2 SDF.asset            기본 머티리얼 = 옅은 그림자(underlay)
    ///   Resources/Fonts/Jalnan2 SDF - Outline.mat    외곽선 프리셋 (UIFactory.Outline이 복사해 색·두께만 바꿈)
    /// </summary>
    [InitializeOnLoad]
    public static class FontSetup
    {
        const string SourcePath = "Assets/TextMesh Pro/Fonts/Jalnan2TTF.ttf";
        const string Dir = "Assets/Resources/Fonts";
        public const string FontPath = Dir + "/Jalnan2 SDF.asset";
        const string OutlinePath = Dir + "/Jalnan2 SDF - Outline.mat";
        const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static FontSetup()
        {
            EditorApplication.delayCall += Ensure;
        }

        /// <summary>폰트 에셋이 없으면 만든다. 배치모드 빌드는 delayCall 전에 끝나므로 빌드 메서드에서도 부른다.</summary>
        public static void Ensure()
        {
            if (!File.Exists(FontPath) || !File.Exists(OutlinePath)) Build();
        }

        [MenuItem("Fishing/Rebuild Jalnan2 TMP Font")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (font == null) { Debug.LogWarning("[FontSetup] 원본 폰트 없음: " + SourcePath); return; }

            Directory.CreateDirectory(Dir);
            AssetDatabase.DeleteAsset(FontPath);
            AssetDatabase.DeleteAsset(OutlinePath);

            // 56pt 샘플·패딩 8: 작은 글씨 외곽선(1~1.5px)까지 낼 수 있는 여유. 2048 아틀라스 한 장에 약 780자, 넘치면 다음 장.
            var fa = TMP_FontAsset.CreateFontAsset(font, 56, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            fa.name = "Jalnan2 SDF";
            // 원래 굵은 글꼴이라 가짜 볼드는 살짝만
            fa.boldStyle = 0.35f;
            fa.boldSpacing = 2f;
            AssetDatabase.CreateAsset(fa, FontPath);

            var atlas = fa.atlasTexture;
            atlas.name = "Jalnan2 SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fa);

            var mat = fa.material;
            mat.name = "Jalnan2 SDF Material";
            mat.SetFloat(ShaderUtilities.ID_WeightBold, fa.boldStyle);
            // 예전 uGUI Shadow(1~1.5px 아래로) 느낌의 그림자: 20pt에서 약 (1, -1.4)px
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.55f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.3f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.45f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
            ShaderUtilities.UpdateShaderRatios(mat);
            AssetDatabase.AddObjectToAsset(mat, fa);

            var outline = new Material(mat) { name = "Jalnan2 SDF - Outline" };
            outline.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outline.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            outline.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
            ShaderUtilities.UpdateShaderRatios(outline);
            AssetDatabase.CreateAsset(outline, OutlinePath);

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
                // 한글을 글자 단위가 아니라 띄어쓰기 단위로 줄바꿈
                so.FindProperty("m_UseModernHangulLineBreakingRules").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FontSetup] TMP 다이나믹 폰트 생성: " + FontPath);
        }
    }
}
