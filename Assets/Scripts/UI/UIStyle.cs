using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 원작풍 UI (clips 07 인벤토리·내 캐릭터 창, 04 HUD 명세 §4): 짙은 남색 반투명 판 + 청록·파랑 빛 테두리, 모서리는 거의 각지게,
    /// 탭·버튼은 파란 판. <see cref="UIShape"/>(둥근 도형·그림자·테두리·가장자리 광택, 셰이더 Fishing/UIShape)와
    /// <see cref="UIBlurSource"/>(뒤 화면 흐림)로 창·HUD 판·버튼·바를 그린다 (2026-10-21까지는 Translucent Image + Paraform).
    ///  - <see cref="Glass"/>: 뒤의 3D 화면이 흐리게 비치는 어두운 유리 판 (창, HUD 패널)
    ///  - <see cref="Shape"/>: 블러 없는 둥근 도형 (버튼, 칩, 바, 배지)
    /// 셰이더는 그래픽 설정의 Always Included에 들어 있다.
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
        static UIBlurSource _blurSource;

        /// <summary>메인 카메라에 배경 블러 소스를 붙인다. UI를 만들기 전에 한 번 부른다.</summary>
        public static void InitStyle(Camera cam)
        {
            if (_blurSource != null || cam == null) return;
            _blurSource = cam.gameObject.AddComponent<UIBlurSource>();
        }

        static Material GlassMaterial => _glassMat ??= ShapeMaterial(true);
        static Material SolidMaterial => _solidMat ??= ShapeMaterial(false);

        static Material ShapeMaterial(bool glass)
        {
            var mat = new Material(Shader.Find("Fishing/UIShape")) { name = glass ? "UIGlass" : "UISolid" };
            mat.SetFloat("_Glass", glass ? 1f : 0f);
            return mat;
        }

        static UIShape NewShape(Transform parent, string name, Material mat, Color color, float radius, float bevel, bool glass)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<UIShape>();
            img.material = mat;
            img.sprite = White;
            img.color = color;
            img.raycastTarget = false;
            img.IsGlass = glass;
            img.Radius = radius;
            img.Bevel = bevel;
            return img;
        }

        /// <summary>
        /// 어두운 유리 판. 뒤 화면이 흐리게 비치고, 둥근 모서리·은은한 테두리·그림자가 붙는다.
        /// opacity가 클수록 tint 색이 진하게 덮인다(글씨가 많은 창은 0.6 이상).
        /// </summary>
        public static UIShape Glass(Transform parent, string name = "Glass", float radius = RadiusPanel, Color? tint = null,
            float opacity = 0.62f, bool shadow = true, bool raycast = true)
        {
            var img = NewShape(parent, name, GlassMaterial, tint ?? GlassTint, radius, 14f, true);
            img.foregroundOpacity = opacity;
            img.raycastTarget = raycast;
            if (shadow) AddShadow(img, 28f, new Vector2(0, -8), 0.45f);
            // 원작 창: 파란 빛 테두리 (안쪽 선 + 바깥으로 번지는 빛)
            AddBorder(img, FrameBlue, 1.2f, 1.6f);
            return img;
        }

        /// <summary>블러 없는 둥근 도형 (버튼·칩·바·배지). radius에 <see cref="Pill"/>을 주면 알약/원 모양.</summary>
        public static UIShape Shape(Transform parent, Color color, float radius = RadiusButton, string name = "Shape", float bevel = 6f)
        {
            return NewShape(parent, name, SolidMaterial, color, radius, bevel, false);
        }

        /// <summary>그림자(아래 깔림). 둥근 모양을 그대로 따른다.</summary>
        public static void AddShadow(UIShape img, float softness, Vector2 offset, float alpha) => img.SetShadow(softness, offset, alpha);

        /// <summary>안쪽 가는 테두리 선 + 바깥으로 번지는 빛(glow) (유리 가장자리, 선택 표시, 빛나는 테두리). 다시 부르면 바꾼다.</summary>
        public static void AddBorder(UIShape img, Color color, float width, float glow = 0f) => img.SetBorder(color, width, glow);

        /// <summary>창 뒤를 덮는 반투명 막 (클릭을 막는다).</summary>
        public static Image Dim(Transform parent, string name, float alpha = 0.45f)
        {
            var img = Image(parent, new Color(0.01f, 0.02f, 0.03f, alpha), White, name);
            img.raycastTarget = true;
            img.rectTransform.Fill();
            return img;
        }

        /// <summary>행·카드 배경 (유리 판 안). highlight면 조금 더 밝게.</summary>
        public static UIShape RowBg(RectTransform row, bool highlight = false, float radius = RadiusCard)
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
            ((UIShape)b.targetGraphic).Radius = Pill;
            return b;
        }

        /// <summary>렌더 텍스처를 둥근 모서리로 보여 준다 (초상화, 전신 미리보기).</summary>
        public static UIShape RoundedTexture(Transform parent, Texture texture, float radius, string name = "Texture")
        {
            var img = Shape(parent, Color.white, radius, name, 0f);
            img.rawTexture = texture;
            return img;
        }

        /// <summary>도형의 모서리 반지름을 바꾼다.</summary>
        public static void SetRadius(Graphic g, float radius)
        {
            if (g is UIShape s) s.Radius = radius;
        }

        /// <summary>
        /// 화면 전체를 흐리게 덮는 막 (테두리·그림자 없는 유리). 원작 ESC 메뉴처럼 뒤 게임 화면이 뿌옇게 보인다. 클릭을 막는다.
        /// </summary>
        public static UIShape BlurBackdrop(Transform parent, string name, float opacity = 0.25f)
        {
            var img = NewShape(parent, name, GlassMaterial, new Color(0f, 0f, 0f, 1f), 0f, 0f, true);
            img.foregroundOpacity = opacity;
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
