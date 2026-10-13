using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>코드로 UGUI를 조립하기 위한 헬퍼. 기준 해상도는 1920x1080. 유리·도형 스타일은 UIStyle.cs.</summary>
    public static partial class UIFactory
    {
        public static readonly Color Accent = new Color(0.36f, 0.88f, 0.8f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.35f);
        public static readonly Color Warn = new Color(1f, 0.62f, 0.25f);
        public static readonly Color Danger = new Color(0.95f, 0.3f, 0.28f);
        public static readonly Color Good = new Color(0.45f, 0.9f, 0.45f);
        public static readonly Color TextMain = new Color(0.95f, 0.97f, 0.99f);
        public static readonly Color TextDim = new Color(0.62f, 0.69f, 0.76f);
        public static Color ButtonBg => BtnNeutral;

        static TMP_FontAsset _font;
        static Material _outlineBase;
        static readonly Dictionary<(Color, float), Material> _outlines = new Dictionary<(Color, float), Material>();
        static Sprite _white, _circle, _ring, _fish;

        /// <summary>TMP 다이나믹 글꼴 Jalnan2. 에셋은 Editor/FontSetup.cs가 Resources/Fonts에 만든다.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (_font == null) _font = Resources.Load<TMP_FontAsset>("Fonts/Jalnan2 SDF");
                if (_font == null) _font = TMP_Settings.defaultFontAsset;
                return _font;
            }
        }

        public static Sprite White => _white ??= MakeSprite(4, (x, y) => 1f);

        /// <summary>가장자리로 갈수록 진해지는 둥근 사각형 테두리. 전체 화면에 늘려 쓴다.</summary>
        public static Sprite Vignette => _vignette ??= MakeSprite(256, (x, y) =>
        {
            float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 5f) + Mathf.Pow(Mathf.Abs(y), 5f), 0.2f);
            float a = Mathf.Clamp01((d - 0.62f) / 0.4f);
            return a * a;
        });

        static Sprite _vignette;

        public static Sprite Circle => _circle ??= MakeSprite(128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01((1f - d) * 64f);
        });

        public static Sprite Ring => _ring ??= MakeSprite(128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01((1f - d) * 64f) * Mathf.Clamp01((d - 0.74f) * 64f);
        });

        /// <summary>
        /// 오른쪽을 보는 물고기 (몸통, 꼬리, 등·배지느러미, 아가미, 눈).
        /// 회색조로 음영을 넣어 두고 Image.color로 어종 색을 입힌다.
        /// </summary>
        public static Sprite Fish => _fish ??= MakeColorSprite(256, (x, y) =>
        {
            float body = 1f - ((x + 0.1f) * (x + 0.1f) / 0.55f + y * y / 0.16f);
            float tailX = -x - 0.62f;
            bool tail = tailX > 0f && tailX < 0.36f && Mathf.Abs(y) < tailX * 1.3f;
            bool dorsal = y > 0.25f && y < 0.25f + (0.2f - Mathf.Abs(x + 0.05f)) * 0.9f && Mathf.Abs(x + 0.05f) < 0.2f;
            bool belly = y < -0.25f && y > -0.25f - (0.12f - Mathf.Abs(x + 0.2f)) * 0.9f && Mathf.Abs(x + 0.2f) < 0.12f;
            float eyeD = (x - 0.36f) * (x - 0.36f) + (y - 0.07f) * (y - 0.07f);
            if (body > 0f)
            {
                if (eyeD < 0.0016f) return new Color(0.05f, 0.05f, 0.05f, 1f);
                if (eyeD < 0.0045f) return new Color(1f, 1f, 1f, 1f);
                // 등(위)은 어둡고 배(아래)는 밝게 + 비늘 무늬
                float shade = Mathf.Lerp(1.05f, 0.62f, (y + 0.4f) / 0.8f);
                float scales = 0.94f + 0.06f * Mathf.Sin(x * 70f) * Mathf.Sin(y * 70f);
                bool gill = Mathf.Abs(x - 0.22f - y * y * 0.8f) < 0.012f && Mathf.Abs(y) < 0.3f;
                float v = Mathf.Clamp01(shade * scales * (gill ? 0.7f : 1f));
                float edge = Mathf.Clamp01(body * 30f);
                return new Color(v, v, v, edge);
            }
            if (tail || dorsal || belly) return new Color(0.75f, 0.75f, 0.75f, 0.92f);
            return Color.clear;
        });

        static Sprite _mouseIdle, _mousePressed, _arrow, _chevron, _speedLines, _hpGradient, _tensionGradient;
        static Texture2D _ticks;

        /// <summary>원작 안내 화면의 마우스 아이콘. pressed면 왼쪽 버튼이 주황색.</summary>
        public static Sprite Mouse(bool pressed)
        {
            if (pressed) return _mousePressed ??= MakeMouse(true);
            return _mouseIdle ??= MakeMouse(false);
        }

        static Sprite MakeMouse(bool pressed) => MakeColorSprite(128, (x, y) =>
        {
            float shape = Mathf.Pow(Mathf.Abs(x / 0.6f), 3f) + Mathf.Pow(Mathf.Abs(y / 0.92f), 3f);
            if (shape > 1f) return Color.clear;
            if (shape > 0.86f) return new Color(0.55f, 0.58f, 0.62f, 1f); // 테두리
            bool topHalf = y > 0.12f;
            if (topHalf && Mathf.Abs(x) < 0.03f) return new Color(0.6f, 0.62f, 0.66f, 1f);
            if (Mathf.Abs(y - 0.12f) < 0.025f) return new Color(0.6f, 0.62f, 0.66f, 1f);
            if (pressed && topHalf && x < 0f) return new Color(1f, 0.45f, 0.3f, 1f);
            float shade = Mathf.Lerp(1f, 0.85f, (x + 0.6f) / 1.2f * 0.5f + (0.92f - y) / 1.84f * 0.5f);
            return new Color(shade, shade, shade, 1f);
        });

        /// <summary>오른쪽을 가리키는 굵은 화살표 (카운터 방향 안내).</summary>
        public static Sprite Arrow => _arrow ??= MakeSprite(256, (x, y) =>
        {
            float shaft = x > -0.9f && x < 0.15f && Mathf.Abs(y) < 0.16f ? 1f : 0f;
            float head = x > 0.05f && x < 0.92f && Mathf.Abs(y) < (0.92f - x) * 0.8f ? 1f : 0f;
            return Mathf.Max(shaft * Mathf.Clamp01((x + 0.9f) * 2f), head);
        });

        /// <summary>왼쪽을 가리키는 꺾쇠 '&lt;' (물고기가 도망가는 방향 표시).</summary>
        public static Sprite Chevron => _chevron ??= MakeSprite(256, (x, y) =>
        {
            var tip = new Vector2(-0.55f, 0f);
            float d1 = DistToSegment(new Vector2(x, y), tip, new Vector2(0.45f, 0.92f));
            float d2 = DistToSegment(new Vector2(x, y), tip, new Vector2(0.45f, -0.92f));
            float d = Mathf.Min(d1, d2);
            return Mathf.Clamp01((0.13f - d) * 40f) + Mathf.Clamp01((0.22f - d) * 3f) * 0.35f;
        });

        /// <summary>
        /// 카운터 찬스 때 화면을 덮는 만화식 집중선. 가운데는 비워 두고 바깥으로 갈수록 진해진다.
        /// 선마다 굵기·시작 거리·밝기가 다르고, 가장자리는 부드럽게 깎아 계단 현상이 없다.
        /// 정사각형 원 안에 그리므로 화면 대각선보다 크게 깔고 돌려야 모서리가 비지 않는다.
        /// </summary>
        public static Sprite SpeedLines => _speedLines ??= MakeSprite(1024, (x, y) =>
        {
            const int Count = 150;
            float r = Mathf.Sqrt(x * x + y * y);
            float a = (Mathf.Atan2(y, x) / (Mathf.PI * 2f) + 0.5f) * Count;
            int i = Mathf.FloorToInt(a);
            float u = a - i;                                       // 선 하나의 각도 칸 안에서 0~1
            float h1 = Hash(i), h2 = Hash(i + 1000), h3 = Hash(i + 2000);
            if (h1 < 0.35f) return 0f;                              // 빈 칸
            float center = 0.5f + (h2 - 0.5f) * 0.4f;
            float width = Mathf.Lerp(0.08f, 0.42f, h3) * 0.5f;      // 칸 대비 반폭
            float edge = Mathf.Abs(u - center);
            float px = 1.5f / (r * 512f * Mathf.PI * 2f / Count + 1e-3f); // 1픽셀만큼의 u
            float body = Mathf.Clamp01((width - edge) / Mathf.Max(px, 0.02f) + 0.5f);
            float start = Mathf.Lerp(0.38f, 0.7f, Hash(i + 3000));  // 선이 시작하는 반지름
            float radial = Mathf.SmoothStep(0f, 1f, (r - start) / 0.25f);
            float bright = Mathf.Lerp(0.45f, 1f, h1);
            return body * radial * bright;
        });

        static float Hash(int n) => Mathf.Abs(Mathf.Sin(n * 12.9898f + 78.233f) * 43758.5453f) % 1f;

        public static Sprite HpGradient => _hpGradient ??= MakeColorSprite(64, (x, y) =>
            Color.Lerp(new Color(0.96f, 0.3f, 0.22f), new Color(1f, 0.78f, 0.3f), (x + 1f) * 0.5f));

        /// <summary>장력 바 배경: LOW(파랑) → HIGH(주황) → 빨강.</summary>
        public static Sprite TensionGradient => _tensionGradient ??= MakeColorSprite(128, (x, y) =>
        {
            float t = (x + 1f) * 0.5f;
            var c = t < 0.6f
                ? Color.Lerp(new Color(0.08f, 0.16f, 0.26f), new Color(0.16f, 0.2f, 0.24f), t / 0.6f)
                : t < 0.9f
                    ? Color.Lerp(new Color(0.3f, 0.24f, 0.12f), new Color(0.45f, 0.28f, 0.1f), (t - 0.6f) / 0.3f)
                    : new Color(0.5f, 0.12f, 0.1f);
            c.a = 0.95f;
            return c;
        });

        /// <summary>눈금자용 반복 텍스처. RawImage.uvRect로 반복시킨다.</summary>
        public static Texture2D Ticks
        {
            get
            {
                if (_ticks != null) return _ticks;
                _ticks = new Texture2D(8, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                for (int y = 0; y < 4; y++)
                for (int x = 0; x < 8; x++)
                    _ticks.SetPixel(x, y, x == 0 ? new Color(1f, 1f, 1f, 0.7f) : Color.clear);
                _ticks.Apply();
                return _ticks;
            }
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static Sprite MakeColorSprite(int size, Func<float, float, Color> color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = color((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>f(x, y)는 -1~1 좌표에서 알파값을 돌려준다.</summary>
        static Sprite MakeSprite(int size, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha(u, v));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            // Paraform(둥근 유리 도형)이 쓰는 정점 채널
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>앵커 한 점 기준 배치. pivot을 생략하면 anchor와 같게 둔다.</summary>
        public static RectTransform At(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Image(Transform parent, Color color, Sprite sprite = null, string name = "Image")
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>HUD·창의 유리 판 (raycast 막음). 예전 이름을 그대로 둔다.</summary>
        public static Graphic Panel(Transform parent, string name, float radius = RadiusHud, float opacity = 0.62f)
            => Glass(parent, name, radius, null, opacity);

        /// <summary>
        /// TMP 텍스트. 가로는 줄바꿈, 세로는 넘쳐도 그린다. 그림자는 글꼴 기본 머티리얼(underlay)에 들어 있다.
        /// 정렬·스타일은 예전 uGUI Text처럼 TextAnchor/FontStyle로 받는다.
        /// </summary>
        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal, string name = "Text")
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = Align(anchor);
            t.fontStyle = Style(style);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        /// <summary>월드 공간 TMP 글자(머리 위 이름표·간판). 가운데 정렬, 줄바꿈 없음. fontSize 10 = 글자 높이 1m.</summary>
        public static TextMeshPro WorldText(Transform parent, string text, Vector3 localPos, Color color, float fontSize, string name = "Text")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<TextMeshPro>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.rectTransform.sizeDelta = Vector2.zero;
            t.fontSharedMaterial = WorldMaterial(t.fontSharedMaterial);
            return t;
        }

        static Material _worldMat;

        /// <summary>
        /// 월드 글자용 머티리얼: 기본 그림자는 글자 크기에 비례해서 가까이서 보면 두껍게 번지므로, 짧고 또렷한 그림자로 바꾼다.
        /// UNDERLAY_ON 키워드는 그대로 둬서 빌드에 이미 들어간 셰이더 변형을 쓴다.
        /// </summary>
        static Material WorldMaterial(Material source)
        {
            if (_worldMat != null) return _worldMat;
            _worldMat = new Material(source) { name = source.name + " (World)" };
            _worldMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.45f));
            _worldMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.1f);
            _worldMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.15f);
            _worldMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            ShaderUtilities.UpdateShaderRatios(_worldMat);
            return _worldMat;
        }

        public static TextAlignmentOptions Align(TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center,
        };

        public static FontStyles Style(FontStyle style) => style switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal,
        };

        /// <summary>
        /// uGUI Outline 대신 쓰는 TMP 외곽선 (uGUI Shadow/Outline 컴포넌트는 TMP에 안 먹는다).
        /// px는 화면 픽셀 두께로, 글자 크기에 맞춰 SDF 두께로 바꾼다. 같은 색·두께는 머티리얼을 같이 쓴다.
        /// 글자 크기를 정한 뒤에 부른다.
        /// </summary>
        public static void Outline(TMP_Text t, Color color, float px = 1.5f)
        {
            if (_outlineBase == null) _outlineBase = Resources.Load<Material>("Fonts/Jalnan2 SDF - Outline");
            var font = Font;
            if (_outlineBase == null || font == null) return;
            // SDF 두께 1 = 아틀라스에서 (패딩+1)/2 픽셀, 화면에선 거기에 글자크기/샘플크기를 곱한 만큼
            float width = px * font.faceInfo.pointSize / (Mathf.Max(1f, t.fontSize) * (font.atlasPadding + 1) * 0.5f);
            width = Mathf.Round(Mathf.Clamp(width, 0.05f, 0.9f) * 20f) / 20f;
            if (!_outlines.TryGetValue((color, width), out var mat) || mat == null)
            {
                mat = new Material(_outlineBase) { name = $"{_outlineBase.name} {ColorUtility.ToHtmlStringRGBA(color)} {width}" };
                mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
                ShaderUtilities.UpdateShaderRatios(mat);
                _outlines[(color, width)] = mat;
            }
            t.fontSharedMaterial = mat;
        }

        /// <summary>둥근 버튼. 배경색은 BtnNeutral/BtnPrimary/BtnGold … 팔레트를 쓴다.</summary>
        public static Button Button(Transform parent, string label, Action onClick, Color? bg = null, int fontSize = 20)
        {
            var img = Shape(parent, bg ?? ButtonBg, RadiusButton, "Button", 5f);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.55f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = Text(img.transform, label, fontSize, TextMain, TextAnchor.MiddleCenter);
            t.rectTransform.Fill(6, 6, 2, 2);
            return btn;
        }

        /// <summary>알약 모양 트랙 + 채움 막대. 반환값은 채움 이미지(fillAmount로 조절, 부모가 트랙).</summary>
        public static Image Bar(Transform parent, Color bg, Color fill, string name = "Bar")
        {
            var back = Shape(parent, bg, Pill, name, 0f);
            var front = Shape(back.transform, fill, Pill, "Fill", 0f);
            front.rectTransform.Fill();
            front.type = UnityEngine.UI.Image.Type.Filled;
            front.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            front.fillAmount = 1f;
            return front;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
