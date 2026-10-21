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
    /// 원작풍 UI (clips 07 인벤토리·내 캐릭터 창, 04 HUD 명세 §4): 짙은 남색 반투명 판 + 청록·파랑 빛 테두리, 모서리는 거의 각지게,
    /// 탭·버튼은 파란 판. Translucent Image(배경 블러)와 Paraform(도형·그림자·테두리·가장자리 광택)으로
    /// 창·HUD 판·버튼·바를 그린다.
    ///  - <see cref="Glass"/>: 뒤의 3D 화면이 흐리게 비치는 어두운 유리 판 (창, HUD 패널)
    ///  - <see cref="Shape"/>: 블러 없는 둥근 도형 (버튼, 칩, 바, 배지)
    /// 재질은 Resources/UI/UIGlass·UISolid (빌드에서 셰이더 키워드가 빠지지 않도록 에셋으로 둔다).
    /// </summary>
    public static partial class UIFactory
    {
        // ---------------- 팔레트 ----------------
        public static readonly Color GlassTint = new Color(0.02f, 0.045f, 0.08f, 1f);
        public static readonly Color TextFaint = new Color(0.45f, 0.52f, 0.6f);
        /// <summary>유리 판 안의 카드·행 배경.</summary>
        public static readonly Color Surface = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color SurfaceHi = new Color(1f, 1f, 1f, 0.1f);
        public static readonly Color Hairline = new Color(0.3f, 0.65f, 1f, 0.22f);
        /// <summary>원작 창 테두리의 파란 빛.</summary>
        public static readonly Color FrameBlue = new Color(0.16f, 0.58f, 1f, 0.75f);
        /// <summary>원작 창 바깥 테두리 (밝은 하늘색 선 + 번지는 빛, clips 07 수리하기·내 캐릭터 창).</summary>
        public static readonly Color FrameEdge = new Color(0.3f, 0.72f, 1f, 1f);
        /// <summary>창 안쪽 판: 거의 검은 남색. 원작 창 안은 뒤가 거의 안 비친다.</summary>
        public static readonly Color WindowInner = new Color(0.012f, 0.02f, 0.032f, 0.94f);
        /// <summary>창 바깥 띠(테두리 판)의 푸른 유리.</summary>
        public static readonly Color WindowBezel = new Color(0.03f, 0.1f, 0.2f, 1f);
        /// <summary>제목 옆 작은 영문 (원작 "내 캐릭터 CHARACTER").</summary>
        public const string CaptionHex = "#4aa8ff";
        public static readonly Color TitleColor = new Color(0.82f, 0.93f, 1f);
        /// <summary>원작 HUD 판: 검은 반투명 + 회색 금속 테두리.</summary>
        public static readonly Color HudTint = new Color(0.03f, 0.035f, 0.04f, 1f);
        public static readonly Color HudEdge = new Color(0.52f, 0.57f, 0.62f, 0.85f);
        public static readonly Color CloseRed = new Color(0.78f, 0.13f, 0.1f, 1f);

        public static readonly Color BtnNeutral = new Color(0.1f, 0.16f, 0.25f, 0.96f);
        public static readonly Color BtnPrimary = new Color(0.09f, 0.36f, 0.68f, 1f);
        public static readonly Color BtnGold = new Color(0.6f, 0.43f, 0.13f, 1f);
        public static readonly Color BtnGood = new Color(0.17f, 0.48f, 0.29f, 1f);
        public static readonly Color BtnDanger = new Color(0.56f, 0.18f, 0.16f, 1f);
        public static readonly Color BtnPurple = new Color(0.4f, 0.29f, 0.58f, 1f);
        public static readonly Color BtnBlue = new Color(0.15f, 0.35f, 0.55f, 1f);
        public static readonly Color BtnGhost = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color TabOn = BtnPrimary;
        public static readonly Color TabOff = new Color(0.12f, 0.22f, 0.36f, 0.75f);

        public const float RadiusPanel = 8f;
        public const float RadiusHud = 6f;
        public const float RadiusCard = 5f;
        public const float RadiusButton = 5f;
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
            var img = NewTranslucent(parent, name, GlassMaterial, tint ?? GlassTint, radius, 3f);
            img.foregroundOpacity = opacity;
            img.vibrancy = 0.9f;
            img.brightness = -0.06f;
            img.flatten = 0.3f;
            img.raycastTarget = raycast;
            if (shadow) AddShadow(img, 28f, new Vector2(0, -8), 0.45f);
            // 원작 창: 파란 빛 테두리 (안쪽 선 + 바깥으로 번지는 빛)
            AddBorder(img, FrameBlue, 0.8f);
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

        /// <summary>안쪽 가는 테두리 선 + 바깥으로 번지는 빛 (유리 가장자리, 선택 표시, 빛나는 테두리). 다시 부르면 바꾼다.</summary>
        public static void AddBorder(TranslucentImage img, Color color, float width, float glow = 0f)
        {
            using (var scope = img.paraformConfig.EditEffects())
            {
                scope.effects.RemoveAll(e => e is PfOutline && e.name == "Border");
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
            }
            img.SetAllDirty();
        }

        /// <summary>
        /// 원작 창 틀 (clips 07 수리하기·내 캐릭터·인벤토리 창): 밝은 하늘색 빛 테두리를 두른 푸른 유리 띠 안에,
        /// 가는 파란 선을 두른 거의 검은 안쪽 판이 7px 들어가 있다. 띠는 Translucent Image라 뒤 화면이 흐리게 비친다.
        /// 반환값은 바깥 틀 (내용은 여기에 붙인다).
        /// </summary>
        public static TranslucentImage Window(Transform parent, string name, Vector2 size)
        {
            var frame = NewTranslucent(parent, name, GlassMaterial, WindowBezel, 9f, 2.5f);
            frame.foregroundOpacity = 0.78f;
            frame.vibrancy = 0.6f;
            frame.brightness = -0.04f;
            frame.flatten = 0.3f;
            frame.raycastTarget = true;
            frame.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            AddShadow(frame, 30f, new Vector2(0, -10), 0.6f);
            AddBorder(frame, FrameEdge, 1f, 1.5f);
            var inner = Shape(frame.transform, WindowInner, 5f, "Inner", 0f);
            inner.rectTransform.Fill(7, 7, 7, 7);
            AddBorder(inner, new Color(0.16f, 0.5f, 0.92f, 0.85f), 0.75f);
            return frame;
        }

        /// <summary>제목 + 작은 파란 영문 (원작 "인벤토리 INVENTORY"). en이 비면 제목만.</summary>
        public static string Caption(string ko, string en, int size = 15)
            => string.IsNullOrEmpty(en) ? ko : $"{ko} <size={size}><color={CaptionHex}>{en}</color></size>";

        /// <summary>원작 창 닫기 버튼: 빨간 네모 + 흰 ×, 밝은 테두리.</summary>
        public static Button CloseButton(Transform parent, System.Action onClick)
        {
            var b = Button(parent, "×", onClick, CloseRed, 24);
            var img = (TranslucentImage)b.targetGraphic;
            img.paraformConfig.CornerRadii = Vector4.one * 3f;
            AddBorder(img, new Color(1f, 0.82f, 0.78f, 0.95f), 1f);
            var t = img.GetComponentInChildren<TMP_Text>();
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.Fill(0, 0, 0, 3);
            return b;
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

        /// <summary>표 머리 줄: 조금 밝은 회청색 띠 가운데에 제목 (원작 내 캐릭터 스탯 표 "데미지 정보").</summary>
        public static TMP_Text TableHeader(RectTransform row, string text, int size = 17)
        {
            var bg = Shape(row, new Color(0.2f, 0.25f, 0.31f, 0.75f), 2f, "Bg", 2f);
            bg.rectTransform.Fill();
            var t = Text(row, text, size, new Color(0.86f, 0.9f, 0.94f), TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Fill();
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

        /// <summary>
        /// 화면 전체를 흐리게 덮는 막 (테두리·그림자 없는 유리). 원작 ESC 메뉴처럼 뒤 게임 화면이 뿌옇게 보인다. 클릭을 막는다.
        /// </summary>
        public static TranslucentImage BlurBackdrop(Transform parent, string name, float opacity = 0.25f)
        {
            var img = NewTranslucent(parent, name, GlassMaterial, new Color(0f, 0f, 0f, 1f), 0f, 0f);
            img.foregroundOpacity = opacity;
            img.vibrancy = 0.6f;
            img.brightness = -0.08f;
            img.flatten = 0.2f;
            img.raycastTarget = true;
            img.rectTransform.Fill();
            return img;
        }

        // ---- 원작 메뉴 버튼 (대기실 오른쪽 아래 4개·ESC 메뉴, 2013 홍현 영상 0:10·2:50) ----
        // 짙은 남색 금속판: 위 반은 조금 밝고 맨 위에 가는 빛 줄, 가운데가 가장 어둡고 아래로 갈수록 푸른 반사광이 올라온다.
        // 둘레는 하늘색 빛 테두리, 글씨는 흰 굵은 한글 + 그 아래 작은 영문 대문자. 게임 종료는 같은 모양의 주황·적갈색 판.
        static Texture2D _classicBlue, _classicRed;
        static Texture2D ClassicBlue => _classicBlue ??= ClassicPlate(new Color(0.36f, 0.6f, 0.78f), new Color(0.17f, 0.34f, 0.48f), new Color(0.05f, 0.13f, 0.21f), new Color(0.1f, 0.27f, 0.39f));
        static Texture2D ClassicRed => _classicRed ??= ClassicPlate(new Color(1f, 0.6f, 0.32f), new Color(0.82f, 0.36f, 0.14f), new Color(0.5f, 0.14f, 0.05f), new Color(0.62f, 0.2f, 0.07f));

        /// <summary>세로 그라데이션 판 텍스처: top = 맨 위 빛 줄, upper = 위 반, mid = 가운데(가장 어둡다), bottom = 아래 반사광.</summary>
        static Texture2D ClassicPlate(Color top, Color upper, Color mid, Color bottom)
        {
            const int h = 64;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[4 * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h; // 0 = 아래, 1 = 위
                Color c = v > 0.5f ? Color.Lerp(mid, upper, Mathf.SmoothStep(0f, 1f, (v - 0.5f) / 0.42f)) : Color.Lerp(bottom, mid, Mathf.SmoothStep(0f, 1f, v / 0.5f));
                if (v > 0.9f) c = Color.Lerp(c, top, (v - 0.9f) / 0.1f);
                c.a = 1f;
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 원작 메뉴 버튼. ko = 굵은 한글, en = 아래 작은 영문(대문자로 쓴다). red면 게임 종료 같은 주황 판.
        /// 크기는 부르는 쪽에서 At으로 정한다 (원작: 대기실 300×56, ESC 228×48).
        /// </summary>
        public static Button ClassicButton(Transform parent, string ko, string en, System.Action onClick, bool red = false, int koSize = 22)
        {
            var img = RoundedTexture(parent, red ? ClassicRed : ClassicBlue, 3f, "ClassicButton");
            img.raycastTarget = true;
            AddShadow(img, 10f, new Vector2(0, -3), 0.55f);
            AddBorder(img, red ? new Color(1f, 0.7f, 0.42f, 0.95f) : new Color(0.42f, 0.78f, 1f, 0.9f), 1.5f, 2.5f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.52f, 0.6f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => { if (Fishing.Core.Game.Sound != null) Fishing.Core.Game.Sound.UiClick(); onClick?.Invoke(); });
            int enSize = Mathf.Max(10, Mathf.RoundToInt(koSize * 0.55f));
            var t = Text(img.transform, string.IsNullOrEmpty(en) ? ko : $"{ko}\n<size={enSize}><color=#e4f0f8>{en}</color></size>", koSize, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.lineSpacing = -14f;
            t.rectTransform.Fill(4, 4, 0, 2);
            return btn;
        }
    }
}
