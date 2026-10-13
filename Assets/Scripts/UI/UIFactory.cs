using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>코드로 UGUI를 조립하기 위한 헬퍼. 기준 해상도는 1920x1080.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelBg = new Color(0.05f, 0.08f, 0.11f, 0.88f);
        public static readonly Color PanelBgLight = new Color(0.12f, 0.17f, 0.22f, 0.92f);
        public static readonly Color Accent = new Color(0.33f, 0.86f, 0.78f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.35f);
        public static readonly Color Warn = new Color(1f, 0.62f, 0.25f);
        public static readonly Color Danger = new Color(0.95f, 0.3f, 0.28f);
        public static readonly Color Good = new Color(0.45f, 0.9f, 0.45f);
        public static readonly Color TextMain = new Color(0.94f, 0.96f, 0.98f);
        public static readonly Color TextDim = new Color(0.64f, 0.7f, 0.76f);
        public static readonly Color ButtonBg = new Color(0.2f, 0.32f, 0.38f, 1f);

        static Font _font;
        static Sprite _white, _circle, _ring, _fish;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "NanumGothic", "Arial" }, 32);
                }
                return _font;
            }
        }

        public static Sprite White => _white ??= MakeSprite(4, (x, y) => 1f);

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

        /// <summary>카운터 찬스 때 화면을 덮는 속도선.</summary>
        public static Sprite SpeedLines => _speedLines ??= MakeSprite(512, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Atan2(y, x) / (Mathf.PI * 2f) + 0.5f;
            int bucket = Mathf.FloorToInt(a * 220f);
            float h = Mathf.Abs(Mathf.Sin(bucket * 12.9898f) * 43758.5453f) % 1f;
            float streak = h > 0.55f ? (h - 0.55f) / 0.45f : 0f;
            return streak * Mathf.Clamp01((r - 0.35f) / 0.65f) * 0.9f;
        });

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

        public static Image Panel(Transform parent, string name, Color? color = null)
        {
            var img = Image(parent, color ?? PanelBg, White, name);
            img.raycastTarget = true;
            return img;
        }

        public static Text Text(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal, string name = "Text")
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color? bg = null, int fontSize = 20)
        {
            var img = Image(parent, bg ?? ButtonBg, White, "Button");
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = Text(img.transform, label, fontSize, TextMain, TextAnchor.MiddleCenter);
            t.rectTransform.Fill(4, 4, 2, 2);
            return btn;
        }

        /// <summary>배경 + 채움 막대. 반환값은 채움 이미지(fillAmount로 조절).</summary>
        public static Image Bar(Transform parent, Color bg, Color fill, string name = "Bar")
        {
            var back = Image(parent, bg, White, name);
            var front = Image(back.transform, fill, White, "Fill");
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
