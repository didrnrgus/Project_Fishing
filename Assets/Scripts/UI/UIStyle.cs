using LeTai.Asset.TranslucentImage;
using LeTai.Paraform.Scaffold;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PfShadow = LeTai.Paraform.Scaffold.Shadow;
using PfOutline = LeTai.Paraform.Scaffold.Outline;

namespace Fishing.UI
{
    /// <summary>
    /// 다크 유리 스타일. Translucent Image(배경 블러)와 Paraform(둥근 도형·그림자·테두리·가장자리 광택)으로
    /// 창·HUD 판·버튼·바를 그린다.
    ///  - <see cref="Glass"/>: 뒤의 3D 화면이 흐리게 비치는 어두운 유리 판 (창, HUD 패널)
    ///  - <see cref="Shape"/>: 블러 없는 둥근 도형 (버튼, 칩, 바, 배지)
    /// 재질은 Resources/UI/UIGlass·UISolid (빌드에서 셰이더 키워드가 빠지지 않도록 에셋으로 둔다).
    /// </summary>
    public static partial class UIFactory
    {
        // ---------------- 팔레트 ----------------
        public static readonly Color GlassTint = new Color(0.035f, 0.06f, 0.085f, 1f);
        public static readonly Color TextFaint = new Color(0.45f, 0.52f, 0.6f);
        /// <summary>유리 판 안의 카드·행 배경.</summary>
        public static readonly Color Surface = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color SurfaceHi = new Color(1f, 1f, 1f, 0.1f);
        public static readonly Color Hairline = new Color(1f, 1f, 1f, 0.1f);

        public static readonly Color BtnNeutral = new Color(0.17f, 0.22f, 0.27f, 0.96f);
        public static readonly Color BtnPrimary = new Color(0.1f, 0.46f, 0.45f, 1f);
        public static readonly Color BtnGold = new Color(0.6f, 0.43f, 0.13f, 1f);
        public static readonly Color BtnGood = new Color(0.17f, 0.48f, 0.29f, 1f);
        public static readonly Color BtnDanger = new Color(0.56f, 0.18f, 0.16f, 1f);
        public static readonly Color BtnPurple = new Color(0.4f, 0.29f, 0.58f, 1f);
        public static readonly Color BtnBlue = new Color(0.15f, 0.35f, 0.55f, 1f);
        public static readonly Color BtnGhost = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color TabOn = BtnPrimary;
        public static readonly Color TabOff = new Color(1f, 1f, 1f, 0.07f);

        public const float RadiusPanel = 22f;
        public const float RadiusHud = 16f;
        public const float RadiusCard = 12f;
        public const float RadiusButton = 10f;
        public const float Pill = 999f;

        static Material _glassMat, _solidMat;
        static TranslucentImageSource _blurSource;

        /// <summary>메인 카메라에 배경 블러 소스를 붙인다. UI를 만들기 전에 한 번 부른다.</summary>
        public static void InitStyle(Camera cam)
        {
            if (_blurSource != null || cam == null) return;
            var config = ScriptableObject.CreateInstance<ScalableBlurConfig>();
            config.Mode = ScalableBlurConfig.BlurMode.Balanced;
            config.Strength = 26f;
            _blurSource = cam.gameObject.AddComponent<TranslucentImageSource>();
            _blurSource.BlurConfig = config;
            _blurSource.Downsample = 1;
        }

        static Material GlassMaterial => _glassMat ??= LoadMaterial("UI/UIGlass", false);
        static Material SolidMaterial => _solidMat ??= LoadMaterial("UI/UISolid", true);

        static Material LoadMaterial(string path, bool opaque)
        {
            // 복사본을 쓴다: 블러 텍스처를 재질에 넣으므로 에디터에서 원본 에셋이 바뀌지 않게
            var asset = Resources.Load<Material>(path);
            if (asset != null) return new Material(asset) { name = asset.name };
            Material mat;
            // 에셋이 아직 임포트되지 않았으면 기본 Paraform 재질에서 키워드만 바꿔 만든다 (에디터용 대비책)
            mat = new Material(DefaultResources.Instance.paraformMaterial) { name = path };
            foreach (var kw in new[] { "_BACKGROUND_MODE_NORMAL", "_BACKGROUND_MODE_COLORFUL", "_BACKGROUND_MODE_OPAQUE", "_REFRACTION_MODE_OFF", "_REFRACTION_MODE_ON", "_REFRACTION_MODE_CHROMATIC" })
                mat.DisableKeyword(kw);
            mat.EnableKeyword(opaque ? "_BACKGROUND_MODE_OPAQUE" : "_BACKGROUND_MODE_NORMAL");
            mat.EnableKeyword(opaque ? "_REFRACTION_MODE_OFF" : "_REFRACTION_MODE_ON");
            mat.EnableKeyword("_USE_EDGE_GLINT");
            return mat;
        }

        static TranslucentImage NewTranslucent(Transform parent, string name, Material mat, Color color, float radius, float bevel)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<TranslucentImage>();
            img.material = mat;
            img.sprite = White;
            img.color = color;
            img.raycastTarget = false;
            if (_blurSource != null) img.source = _blurSource;
            img.paraformConfig.FormMode = FormMode.RoundedBox;
            img.paraformConfig.CornerCurvature = 1.6f;
            img.paraformConfig.CornerRadii = Vector4.one * radius;
            img.paraformConfig.BevelWidth = bevel;
            img.paraformConfig.Elevation = 40f;
            return img;
        }

        /// <summary>
        /// 어두운 유리 판. 뒤 화면이 흐리게 비치고, 둥근 모서리·은은한 테두리·그림자가 붙는다.
        /// opacity가 클수록 tint 색이 진하게 덮인다(글씨가 많은 창은 0.6 이상).
        /// </summary>
        public static TranslucentImage Glass(Transform parent, string name = "Glass", float radius = RadiusPanel, Color? tint = null,
            float opacity = 0.62f, bool shadow = true, bool raycast = true)
        {
            var img = NewTranslucent(parent, name, GlassMaterial, tint ?? GlassTint, radius, 14f);
            img.foregroundOpacity = opacity;
            img.vibrancy = 0.9f;
            img.brightness = -0.06f;
            img.flatten = 0.3f;
            img.raycastTarget = raycast;
            if (shadow) AddShadow(img, 28f, new Vector2(0, -8), 0.45f);
            AddBorder(img, new Color(1f, 1f, 1f, 0.09f), 1.2f);
            return img;
        }

        /// <summary>블러 없는 둥근 도형 (버튼·칩·바·배지). radius에 <see cref="Pill"/>을 주면 알약/원 모양.</summary>
        public static TranslucentImage Shape(Transform parent, Color color, float radius = RadiusButton, string name = "Shape", float bevel = 6f)
        {
            return NewTranslucent(parent, name, SolidMaterial, color, radius, bevel);
        }

        /// <summary>그림자(아래 깔림). Paraform 효과라 둥근 모양을 그대로 따른다.</summary>
        public static void AddShadow(TranslucentImage img, float softness, Vector2 offset, float alpha)
        {
            using (var scope = img.paraformConfig.EditEffects())
                scope.effects.Add(new PfShadow
                {
                    name = "Shadow",
                    color = new Color(0f, 0f, 0f, alpha),
                    useGraphicColor = false,
                    softness = softness,
                    offset = offset,
                });
            img.SetAllDirty();
        }

        /// <summary>안쪽 가는 테두리 선 (유리 가장자리, 선택 표시, 빛나는 테두리).</summary>
        public static void AddBorder(TranslucentImage img, Color color, float width, float glow = 0f)
        {
            using (var scope = img.paraformConfig.EditEffects())
                scope.effects.Add(new PfOutline
                {
                    name = "Border",
                    color = color,
                    useGraphicColor = false,
                    useGraphicAlpha = true,
                    innerWidth = width,
                    outerWidth = glow,
                    outerSoftness = glow,
                });
            img.SetAllDirty();
        }

        /// <summary>창 뒤를 덮는 반투명 막 (클릭을 막는다).</summary>
        public static Image Dim(Transform parent, string name, float alpha = 0.45f)
        {
            var img = Image(parent, new Color(0.01f, 0.02f, 0.03f, alpha), White, name);
            img.raycastTarget = true;
            img.rectTransform.Fill();
            return img;
        }

        /// <summary>행·카드 배경 (유리 판 안). highlight면 조금 더 밝게.</summary>
        public static TranslucentImage RowBg(RectTransform row, bool highlight = false, float radius = RadiusCard)
        {
            var bg = Shape(row, highlight ? SurfaceHi : Surface, radius, "Bg", 0f);
            bg.rectTransform.Fill();
            bg.transform.SetAsFirstSibling();
            return bg;
        }

        /// <summary>작은 알약 라벨 (단축키, 등급, 상태 표시).</summary>
        public static TMP_Text Chip(Transform parent, string text, Color bg, Color fg, int size = 13, string name = "Chip")
        {
            var shape = Shape(parent, bg, Pill, name, 0f);
            var t = Text(shape.transform, text, size, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Fill(6, 6, 0, 0);
            return t;
        }

        /// <summary>섹션 제목: 왼쪽 강조 막대 + 굵은 글씨.</summary>
        public static TMP_Text SectionTitle(RectTransform row, string text, float x = 4f, int size = 19)
        {
            var bar = Shape(row, Accent, Pill, "Mark", 0f).rectTransform;
            bar.anchorMin = bar.anchorMax = new Vector2(0, 0.5f);
            bar.pivot = new Vector2(0, 0.5f);
            bar.anchoredPosition = new Vector2(x, 0);
            bar.sizeDelta = new Vector2(4, size + 2);
            var t = Text(row, text, size, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.anchorMin = new Vector2(0, 0);
            t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.offsetMin = new Vector2(x + 14, 0);
            t.rectTransform.offsetMax = Vector2.zero;
            return t;
        }

        /// <summary>가는 구분선.</summary>
        public static Image Divider(Transform parent, string name = "Divider") => Image(parent, Hairline, null, name);

        /// <summary>둥근 원 모양 아이콘 버튼 (닫기, 화살표).</summary>
        public static Button IconButton(Transform parent, string label, System.Action onClick, Color? bg = null, int fontSize = 20)
        {
            var b = Button(parent, label, onClick, bg ?? BtnGhost, fontSize);
            var img = (TranslucentImage)b.targetGraphic;
            img.paraformConfig.CornerRadii = Vector4.one * Pill;
            return b;
        }

        /// <summary>렌더 텍스처를 둥근 모서리로 보여 준다 (초상화, 전신 미리보기).</summary>
        public static TranslucentImage RoundedTexture(Transform parent, Texture texture, float radius, string name = "Texture")
        {
            var img = Shape(parent, Color.white, radius, name, 0f);
            img.imageMode = TranslucentImage.ImageMode.Texture;
            img.rawTexture = texture;
            return img;
        }

        /// <summary>도형의 모서리 반지름을 바꾼다.</summary>
        public static void SetRadius(Graphic g, float radius)
        {
            if (g is TranslucentImage ti) ti.paraformConfig.CornerRadii = Vector4.one * radius;
        }
    }
}
