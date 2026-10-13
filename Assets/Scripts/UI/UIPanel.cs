using TMPro;
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
        protected TMP_Text Title;

        /// <summary>BuildFrame 대신 전체 화면 등 직접 만든 루트를 쓸 때.</summary>
        protected void SetRoot(GameObject root) => Root = root;

        protected void BuildFrame(Transform canvas, string name, string title, Vector2 size, bool closable = true)
        {
            Root = UIFactory.Dim(canvas, name, 0.42f).gameObject;

            // 유리 창: 뒤 화면이 흐리게 비친다. 제목 줄 아래에 가는 구분선
            Frame = UIFactory.Glass(Root.transform, "Frame", UIFactory.RadiusPanel, null, 0.7f).rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UIFactory.Shape(Frame, UIFactory.Accent, UIFactory.Pill, "TitleMark", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(26, -24), new Vector2(5, 28));
            Title = UIFactory.Text(Frame, title, 27, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            Title.rectTransform.At(new Vector2(0, 1), new Vector2(42, -14), new Vector2(size.x - 160, 46));
            UIFactory.Divider(Frame, "TitleLine").rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(size.x - 48, 1));
            if (closable)
            {
                var close = UIFactory.IconButton(Frame, "×", Close, UIFactory.BtnGhost, 20);
                ((RectTransform)close.transform).At(new Vector2(1, 1), new Vector2(-18, -16), new Vector2(40, 40));
            }
            Body = UIFactory.Node("Body", Frame).Fill(28, 28, 82, 24);
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

        protected static TMP_Text Cell(RectTransform row, string text, float x, float width, int size = 18, Color? color = null,
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
            rt.sizeDelta = new Vector2(width, -8);
            b.interactable = interactable;
            return b;
        }
    }
}
