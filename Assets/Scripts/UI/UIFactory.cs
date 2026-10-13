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

        /// <summary>오른쪽을 보는 물고기 실루엣 (몸통 타원 + 꼬리 + 눈).</summary>
        public static Sprite Fish => _fish ??= MakeSprite(256, (x, y) =>
        {
            float body = 1f - ((x + 0.1f) * (x + 0.1f) / 0.55f + y * y / 0.16f);
            float tailX = -x - 0.62f; // 꼬리는 왼쪽 끝 삼각형
            float tail = tailX > 0f && tailX < 0.36f && Mathf.Abs(y) < tailX * 1.3f ? 1f : 0f;
            float eye = (x - 0.38f) * (x - 0.38f) + (y - 0.06f) * (y - 0.06f) < 0.0025f ? 0f : 1f;
            return Mathf.Max(Mathf.Clamp01(body * 40f), tail) * eye;
        });

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
