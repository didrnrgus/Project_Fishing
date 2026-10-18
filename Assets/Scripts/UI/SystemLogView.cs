using Fishing.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 원작 화면의 두 가지 글씨 알림 (2013 홍현 대기실·배 영상 0:22~4:58에서 늘 보인다):
    /// - 왼쪽 아래 시스템 채팅: "한국 채널에 입장 했습니다." "6000 EXP 를 획득했습니다." "[강대표의 추측] 퀘스트를 수락하셨습니다." 최근 6줄, 오래되면 흐려진다.
    /// - 화면 가운데 위 흰 글씨: "[업적] 동갈자돔을 홍현 앞바다에서 납품하기 1/1 [완 료]" 같은 진행 알림. 줄마다 3초.
    /// </summary>
    public class SystemLogView
    {
        const int MaxLines = 6;
        const float LineLife = 40f;
        readonly TMP_Text _log;
        readonly RectTransform _noticeRoot;
        readonly List<(string text, float time)> _lines = new List<(string, float)>();
        readonly List<(RectTransform root, CanvasGroup group, float until)> _notices = new List<(RectTransform, CanvasGroup, float)>();
        readonly UnityEngine.UI.Image _logBg;

        public SystemLogView(Transform canvas)
        {
            var root = UIFactory.Node("SystemLog", canvas).Fill();
            // 원작 채팅 창처럼 옅은 검은 판 위에 글씨. 작은 글씨에 외곽선(Outline)을 걸면 SDF 두께가 커져 글자가 검게 뭉개진다
            _logBg = UIFactory.Image(root, new Color(0f, 0f, 0f, 0.4f), null, "LogBg");
            _logBg.raycastTarget = false;
            _logBg.rectTransform.At(new Vector2(0, 0), new Vector2(14, 120), new Vector2(580, 0), Vector2.zero);
            _log = UIFactory.Text(root, "", 16, UIFactory.TextMain, TextAnchor.LowerLeft);
            _log.rectTransform.At(new Vector2(0, 0), new Vector2(24, 127), new Vector2(560, 160), Vector2.zero);
            _log.raycastTarget = false;
            _log.lineSpacing = 4f;

            // 시스템 줄은 창 밑에 깔리게 (원작: 창이 채팅을 덮는다). 가운데 알림만 맨 위에
            root.SetAsFirstSibling();
            _noticeRoot = UIFactory.Node("Notices", canvas).At(new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(1000, 120), new Vector2(0.5f, 1));
            Game.Logged += Add;
            Game.Noticed += Notice;
        }

        public void Dispose()
        {
            Game.Logged -= Add;
            Game.Noticed -= Notice;
        }

        void Add(string message, string color)
        {
            _lines.Add(($"<color={color}>{message}</color>", Time.unscaledTime));
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);
            Rebuild();
        }

        void Notice(string message)
        {
            // 외곽선 대신 글자 뒤 옅은 검은 띠 (밝은 하늘 위에서도 흰 글씨가 읽히게)
            _noticeRoot.SetAsLastSibling(); // 나중에 연 창보다 위에
            var band = UIFactory.Image(_noticeRoot, new Color(0f, 0f, 0f, 0.42f), null, "Notice");
            band.raycastTarget = false;
            var t = UIFactory.Text(band.transform, message, 19, Color.white, TextAnchor.MiddleCenter);
            t.raycastTarget = false;
            t.rectTransform.Fill();
            t.textWrappingMode = TextWrappingModes.NoWrap;
            float w = Mathf.Min(1000f, t.preferredWidth + 40f);
            band.rectTransform.sizeDelta = new Vector2(w, 30f);
            var group = band.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            _notices.Add((band.rectTransform, group, Time.unscaledTime + 3f));
            while (_notices.Count > 4)
            {
                Object.Destroy(_notices[0].root.gameObject);
                _notices.RemoveAt(0);
            }
            Layout();
        }

        void Layout()
        {
            for (int i = 0; i < _notices.Count; i++)
            {
                var rt = _notices[i].root;
                rt.At(new Vector2(0.5f, 1), new Vector2(0, -i * 34), rt.sizeDelta, new Vector2(0.5f, 1));
            }
        }

        void Rebuild()
        {
            var sb = new System.Text.StringBuilder();
            float now = Time.unscaledTime;
            foreach (var (text, time) in _lines)
            {
                float age = now - time;
                // 오래된 줄은 흐리게 (원작 채팅 창처럼 사라지지는 않는다)
                int a = age < LineLife ? 255 : 120;
                sb.Append($"<alpha=#{a:X2}>").Append(text).Append('\n');
            }
            _log.text = sb.ToString().TrimEnd('\n');
            // 판은 줄 수만큼만
            float h = _lines.Count == 0 ? 0f : _log.GetPreferredValues(_log.text, 560f, 0f).y + 14f;
            _logBg.rectTransform.sizeDelta = new Vector2(580f, h);
            _logBg.enabled = h > 0f;
        }

        float _nextRebuild;

        public void Update()
        {
            float now = Time.unscaledTime;
            bool changed = false;
            for (int i = _notices.Count - 1; i >= 0; i--)
            {
                var (rt, group, until) = _notices[i];
                float left = until - now;
                if (left <= 0f) { Object.Destroy(rt.gameObject); _notices.RemoveAt(i); changed = true; continue; }
                group.alpha = Mathf.Clamp01(left / 0.5f);
            }
            if (changed) Layout();
            // 챔질·1인칭 파이팅·결과 화면에서는 왼쪽 아래 콤보·포커싱 칸과 겹치므로 시스템 줄을 숨긴다 (원작도 파이팅 중엔 HUD를 걷는다)
            var st = Game.Fishing != null ? Game.Fishing.State : Fishing.Angling.FishingState.Idle;
            bool hide = st == Fishing.Angling.FishingState.Bite || st == Fishing.Angling.FishingState.Fighting || st == Fishing.Angling.FishingState.Result
                        || (Game.UI != null && Game.UI.Loading != null && Game.UI.Loading.Showing);
            _log.enabled = !hide;
            _logBg.gameObject.SetActive(!hide && _lines.Count > 0);
            if (now > _nextRebuild) { _nextRebuild = now + 2f; Rebuild(); }
        }
    }
}
