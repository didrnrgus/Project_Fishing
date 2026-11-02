using System.Text;
using Fishing.Buddy;
using Fishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 윤슬과의 대화: 왼쪽 가운데(시스템 줄 위) 대화창 + 윤슬 머리 위 말풍선 + 닫혀 있을 때 "Enter 윤슬과 대화" 알약.
    /// 다른 창과 달리 게임을 막지 않는다 (열어 둔 채 걷고 낚시한다). 입력칸에 글을 쓰는 동안만 단축키·이동을 막는다(<see cref="Settings.Typing"/>).
    /// Enter: 열고 입력칸으로 → 쓰고 Enter로 보내기. Esc: 닫기.
    /// </summary>
    public class BuddyChatView
    {
        const float BubbleSeconds = 7f;
        const float BubbleWidth = 420f;

        readonly RectTransform _window, _hint, _bubble;
        readonly TMP_Text _log, _status, _bubbleText, _hintText;
        readonly TMP_InputField _input;
        readonly Button _modelButton;
        float _bubbleUntil;
        string _bubbleLine;
        bool _dirty = true;

        public bool IsOpen => _window.gameObject.activeSelf;
        /// <summary>입력칸에서 Enter를 눌러 막 닫았거나 보낸 프레임: 같은 Enter로 다시 열지 않게 (GameUI 단축키).</summary>
        public bool JustSubmitted => Time.frameCount - _submitFrame <= 1;
        int _submitFrame = -10;

        public BuddyChatView(Transform canvas)
        {
            var root = UIFactory.Node("BuddyChat", canvas).Fill();

            // 말풍선 (화면 공간, 윤슬 머리 위를 따라간다)
            _bubble = UIFactory.Panel(root, "Bubble", 14f, 0.8f).rectTransform;
            _bubble.pivot = new Vector2(0.5f, 0f);
            _bubble.anchorMin = _bubble.anchorMax = Vector2.zero;
            _bubble.GetComponent<Graphic>().raycastTarget = false;
            _bubbleText = UIFactory.Text(_bubble, "", 18, UIFactory.TextMain, TextAnchor.MiddleLeft);
            _bubbleText.rectTransform.Fill(14, 14, 8, 8);
            _bubble.gameObject.SetActive(false);

            // 닫혀 있을 때의 알약
            _hint = UIFactory.Panel(root, "Hint", UIFactory.Pill, 0.6f).rectTransform;
            _hint.At(new Vector2(0, 0), new Vector2(14, 292), new Vector2(230, 34), Vector2.zero);
            _hint.GetComponent<Graphic>().raycastTarget = false;
            _hintText = UIFactory.Text(_hint, "", 16, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _hintText.rectTransform.Fill(10, 10, 0, 0);

            // 대화창
            _window = UIFactory.Panel(root, "Window", 10f, 0.78f).rectTransform;
            _window.At(new Vector2(0, 0), new Vector2(14, 292), new Vector2(600, 420), Vector2.zero);
            var title = UIFactory.Text(_window, $"{BuddyBrain.BuddyName} <size=15><color=#9fb0c0>낚시 친구</color></size>", 22, UIFactory.TitleColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(220, 34), new Vector2(0, 1));
            _status = UIFactory.Text(_window, "", 14, UIFactory.TextDim, TextAnchor.MiddleRight);
            _status.rectTransform.At(new Vector2(1, 1), new Vector2(-50, -8), new Vector2(330, 34), new Vector2(1, 1));
            var close = UIFactory.CloseButton(_window, Close);
            ((RectTransform)close.transform).At(new Vector2(1, 1), new Vector2(-12, -10), new Vector2(28, 28), new Vector2(1, 1));

            // 대화 줄: 아래부터 쌓이고 넘치면 위가 잘린다
            var view = UIFactory.Node("Log", _window).Fill(16, 16, 48, 96);
            view.gameObject.AddComponent<RectMask2D>();
            _log = UIFactory.Text(view, "", 17, UIFactory.TextMain, TextAnchor.LowerLeft);
            _log.rectTransform.Fill();
            _log.lineSpacing = 6f;
            _log.paragraphSpacing = 10f;

            // 입력칸 + 보내기
            _input = CreateInput(_window);
            ((RectTransform)_input.transform).At(new Vector2(0, 0), new Vector2(16, 50), new Vector2(476, 38), Vector2.zero);
            _input.onSubmit.AddListener(Send);
            var send = UIFactory.Button(_window, "보내기", () => Send(_input.text), null, 17);
            ((RectTransform)send.transform).At(new Vector2(1, 0), new Vector2(-16, 50), new Vector2(84, 38), new Vector2(1, 0));

            // 아래 줄: 모델 바꾸기 · 설치 안내 · 다시 확인
            _modelButton = UIFactory.Button(_window, "모델", () => BuddyBrain.Instance?.NextModel(), null, 14);
            ((RectTransform)_modelButton.transform).At(new Vector2(0, 0), new Vector2(16, 12), new Vector2(200, 30), Vector2.zero);
            var guide = UIFactory.Button(_window, "설치 안내 (브라우저)", () => Application.OpenURL(BuddyConfig.DownloadUrl), null, 14);
            ((RectTransform)guide.transform).At(new Vector2(0, 0), new Vector2(224, 12), new Vector2(180, 30), Vector2.zero);
            var retry = UIFactory.Button(_window, "다시 연결", () => BuddyBrain.Instance?.CheckOllama(), null, 14);
            ((RectTransform)retry.transform).At(new Vector2(0, 0), new Vector2(412, 12), new Vector2(110, 30), Vector2.zero);

            _window.gameObject.SetActive(false);
            if (BuddyBrain.Instance != null)
            {
                BuddyBrain.Instance.Changed += () => _dirty = true;
                BuddyBrain.Instance.Said += (text, _) => ShowBubble(text);
            }
        }

        static TMP_InputField CreateInput(Transform parent)
        {
            var bg = UIFactory.Image(parent, new Color(0f, 0f, 0f, 0.55f), null, "Input");
            bg.raycastTarget = true;
            var field = bg.gameObject.AddComponent<TMP_InputField>();
            var area = UIFactory.Node("TextArea", bg.transform).Fill(12, 12, 4, 4);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UIFactory.Text(area, "물어보고 싶은 걸 써 봐 (예: 붕어는 어디서 잡아?)", 17, UIFactory.TextDim, TextAnchor.MiddleLeft);
            placeholder.rectTransform.Fill();
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            var text = UIFactory.Text(area, "", 17, UIFactory.TextMain, TextAnchor.MiddleLeft);
            text.rectTransform.Fill();
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.richText = false;
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = UIFactory.Font;
            field.pointSize = 17;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 300;
            field.caretColor = UIFactory.TextMain;
            field.customCaretColor = true;
            field.selectionColor = new Color(0.3f, 0.6f, 1f, 0.45f);
            field.richText = false;
            return field;
        }

        public void Open()
        {
            _window.gameObject.SetActive(true);
            _window.SetAsLastSibling();
            BuddyBrain.Instance?.CheckOllama(warm: true);
            Focus();
            _dirty = true;
        }

        public void Close()
        {
            if (_input.isFocused) _input.DeactivateInputField();
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _input.gameObject) EventSystem.current.SetSelectedGameObject(null);
            _window.gameObject.SetActive(false);
        }

        public void Focus()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_input.gameObject);
            _input.ActivateInputField();
        }

        /// <summary>자동 촬영용: 입력칸에 쓰고 보낸 것처럼.</summary>
        public void DebugAsk(string question)
        {
            if (!IsOpen) Open();
            Send(question);
        }

        void Send(string text)
        {
            _submitFrame = Time.frameCount;
            if (string.IsNullOrWhiteSpace(text)) { Close(); return; } // 빈칸에서 Enter = 닫기 (MMO 채팅처럼)
            BuddyBrain.Instance?.Ask(text);
            _input.text = "";
            Focus();
        }

        void ShowBubble(string text)
        {
            _bubbleLine = text;
            _bubbleUntil = Time.unscaledTime + BubbleSeconds + text.Length * 0.04f;
        }

        public void Update()
        {
            var brain = BuddyBrain.Instance;
            // 입력칸에 쓰는 동안: 이동·단축키 막기 (Settings.Down/Held가 false)
            Settings.Typing = IsOpen && _input.isFocused;

            bool field = Game.Mode == GameMode.Field;
            _hint.gameObject.SetActive(!IsOpen && brain != null && !(Game.UI != null && Game.UI.BlockingPanelOpen));
            if (_hint.gameObject.activeSelf)
                _hintText.text = $"<color=#ffd75a>Enter</color> {BuddyBrain.BuddyName}{GameUI.Josa(BuddyBrain.BuddyName, "과", "와")} 대화" +
                                 (brain.State == BuddyBrain.Status.Thinking ? " …" : "");

            if (_dirty && brain != null) { _dirty = false; Redraw(brain); }
            UpdateBubble(brain, field);
        }

        void Redraw(BuddyBrain brain)
        {
            _status.text = brain.State switch
            {
                BuddyBrain.Status.Checking => "연결 확인 중…",
                BuddyBrain.Status.Offline => "<color=#ff7a6a>●</color> Ollama 꺼짐 — 정해진 말만 해요",
                BuddyBrain.Status.NoModel => $"<color=#ffb050>●</color> 모델 없음 (ollama pull {BuddyConfig.DefaultModel})",
                BuddyBrain.Status.Thinking => $"<color=#7fd0ff>●</color> {(brain.Partial != null ? "말하는 중…" : brain.Working ?? "생각 중…")}",
                _ => $"<color=#7fe07f>●</color> 이 PC의 AI 연결됨",
            };
            _modelButton.GetComponentInChildren<TMP_Text>().text = brain.Model != null ? $"모델: {brain.Model}" : "모델: 없음";
            _modelButton.interactable = brain.Models.Count > 1 && brain.State != BuddyBrain.Status.Thinking;

            var sb = new StringBuilder();
            int from = Mathf.Max(0, brain.Log.Count - 14);
            for (int i = from; i < brain.Log.Count; i++)
            {
                var (me, text) = brain.Log[i];
                string safe = text.Replace("<", "&lt;");
                sb.Append(me ? $"<color=#ffd75a>나</color>  {safe}" : $"<color=#7fd0ff>{BuddyBrain.BuddyName}</color>  {safe}");
                if (i < brain.Log.Count - 1) sb.Append('\n');
            }
            if (brain.State == BuddyBrain.Status.Thinking)
                sb.Append(brain.Partial != null
                    ? $"\n<color=#7fd0ff>{BuddyBrain.BuddyName}</color>  {brain.Partial.Replace("<", "&lt;")}"
                    : $"\n<color=#7fd0ff>{BuddyBrain.BuddyName}</color>  <color=#9fb0c0>{brain.Working ?? "…"}</color>");
            _log.text = sb.ToString();
        }

        void UpdateBubble(BuddyBrain brain, bool field)
        {
            var anchor = BuddyBody.Instance != null ? BuddyBody.Instance.BubbleAnchor : null;
            string line = brain != null && brain.State == BuddyBrain.Status.Thinking ? brain.Partial ?? brain.Working ?? "…"
                : Time.unscaledTime < _bubbleUntil ? _bubbleLine : null;
            var cam = Game.CameraRig != null ? Game.CameraRig.Cam : null;
            bool show = field && line != null && anchor.HasValue && cam != null && !(Game.UI != null && Game.UI.BlockingPanelOpen);
            Vector3 sp = default;
            if (show)
            {
                sp = cam.WorldToScreenPoint(anchor.Value);
                show = sp.z > 0.5f && sp.x > 0f && sp.x < Screen.width; // 화면 밖이면 (가장자리에 붙이면 누가 말하는지 모른다) 숨긴다
            }
            _bubble.gameObject.SetActive(show);
            if (!show) return;
            if (_bubbleText.text != line)
            {
                _bubbleText.text = line.Replace("<", "&lt;");
                var size = _bubbleText.GetPreferredValues(_bubbleText.text, BubbleWidth - 28f, 0f);
                _bubble.sizeDelta = new Vector2(Mathf.Min(BubbleWidth, size.x + 30f), size.y + 18f);
            }
            // 화면 좌표 → 캔버스 좌표 (캔버스는 1920×1080 기준으로 늘어난다)
            var canvas = (RectTransform)_bubble.parent;
            float scale = canvas.rect.width > 0f ? canvas.rect.width / Screen.width : 1f;
            var pos = new Vector2(sp.x * scale, sp.y * scale + 8f);
            float half = _bubble.sizeDelta.x * 0.5f;
            // 대화창이 열려 있으면 창 오른쪽으로 비켜 선다 (창 뒤에 가리지 않게)
            float minX = IsOpen ? _window.anchoredPosition.x + _window.sizeDelta.x + 10f : 10f;
            pos.x = Mathf.Clamp(pos.x, minX + half, canvas.rect.width - half - 10f);
            pos.y = Mathf.Clamp(pos.y, 10f, canvas.rect.height - _bubble.sizeDelta.y - 140f);
            _bubble.anchoredPosition = pos;
        }
    }
}
