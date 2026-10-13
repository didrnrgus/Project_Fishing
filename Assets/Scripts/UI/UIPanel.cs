using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>화면 가운데 뜨는 창의 공통 틀: 제목, 닫기 버튼, 본문 영역.</summary>
    public abstract class UIPanel
    {
        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;

        protected RectTransform Frame;
        protected RectTransform Body;
        protected Text Title;

        /// <summary>BuildFrame 대신 전체 화면 등 직접 만든 루트를 쓸 때.</summary>
        protected void SetRoot(GameObject root) => Root = root;

        protected void BuildFrame(Transform canvas, string name, string title, Vector2 size, bool closable = true)
        {
            var dim = UIFactory.Panel(canvas, name, new Color(0, 0, 0, 0.35f));
            dim.rectTransform.Fill();
            Root = dim.gameObject;

            Frame = UIFactory.Panel(Root.transform, "Frame").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UIFactory.Image(Frame, UIFactory.Accent, null, "TopLine").rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(size.x, 4));
            Title = UIFactory.Text(Frame, title, 28, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            Title.rectTransform.At(new Vector2(0, 1), new Vector2(24, -12), new Vector2(size.x - 120, 44));
            if (closable)
            {
                var close = UIFactory.Button(Frame, "✕", Close, new Color(0.3f, 0.15f, 0.15f), 22);
                ((RectTransform)close.transform).At(new Vector2(1, 1), new Vector2(-14, -14), new Vector2(40, 40));
            }
            Body = UIFactory.Node("Body", Frame).Fill(24, 24, 68, 20);
            Root.SetActive(false);
        }

        public virtual void Open()
        {
            Root.SetActive(true);
            Root.transform.SetAsLastSibling();
            Refresh();
        }

        public virtual void Close() => Root.SetActive(false);

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public abstract void Refresh();

        /// <summary>본문을 비우고 위에서부터 행을 쌓는 헬퍼.</summary>
        protected class RowCursor
        {
            readonly RectTransform _parent;
            float _y;

            public RowCursor(RectTransform parent, float startY = 0f)
            {
                _parent = parent;
                _y = startY;
            }

            public RectTransform Next(float height, float gap = 6f)
            {
                var rt = UIFactory.Node("Row", _parent);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -_y);
                rt.sizeDelta = new Vector2(0, height);
                _y += height + gap;
                return rt;
            }
        }

        protected static Text Cell(RectTransform row, string text, float x, float width, int size = 18, Color? color = null,
            TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var t = UIFactory.Text(row, text, size, color ?? UIFactory.TextMain, anchor, style);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);
            rt.sizeDelta = new Vector2(width, 0);
            return t;
        }

        protected static Button RowButton(RectTransform row, string label, System.Action onClick, float x, float width,
            Color? bg = null, bool interactable = true, int fontSize = 17)
        {
            var b = UIFactory.Button(row, label, onClick, bg, fontSize);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);
            rt.sizeDelta = new Vector2(width, -6);
            b.interactable = interactable;
            return b;
        }
    }
}
