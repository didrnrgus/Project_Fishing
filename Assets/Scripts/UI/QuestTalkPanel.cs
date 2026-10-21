using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// NPC 퀘스트 대화 (원작 13 문서 0:42·1:24·2:56): 왼쪽 세로 대화 카드 — 이름 &lt;별칭&gt;, 인사말,
    ///  · 목록: "수락가능한 퀘스트" / "진행중인 퀘스트" (Lv.N 제목 + (이야기)/(일일반복)/(가이드) 꼬리표)
    ///  · 상세: Lv. 제목, 녹색 목표, [보고자], 이야기 글, [퀘스트 완료 보상] N EXP / N GOLD + 보상 칸 5개, 수락/돌아가기 또는 완료/포기/돌아가기
    /// 대기실에서는 오른쪽 아래에 조합 직원 카드 줄(머리 위 ! / ?)이 함께 뜬다 (원작 대기실 [퀘스트] 버튼).
    /// 원작 그래픽(사진 액자·종이 질감)은 쓰지 않고 이 게임의 유리 UI로 같은 구조만 따른다.
    /// </summary>
    public class QuestTalkPanel : UIPanel
    {
        const float W = 540f, H = 720f;

        string _key;
        NpcAngler _npc;
        bool _lobby;
        QuestDef _detail;
        bool _confirmAbandon;

        readonly TranslucentAvatar _avatar;
        readonly TMP_Text _greet;
        readonly RectTransform _content, _staff;

        public string NpcKey => _key;

        public QuestTalkPanel(Transform canvas)
        {
            BuildFrame(canvas, "QuestTalkPanel", "", new Vector2(W, H));
            // 원작처럼 뒤 화면을 거의 가리지 않는다 (왼쪽 대화 카드)
            Root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            Frame.At(new Vector2(0, 0.5f), new Vector2(34, 30), new Vector2(W, H), new Vector2(0, 0.5f));
            var mark = Frame.Find("TitleMark");
            if (mark != null) mark.gameObject.SetActive(false);
            _avatar = new TranslucentAvatar(Frame, new Vector2(24, -10), 58f);
            Title.rectTransform.At(new Vector2(0, 1), new Vector2(96, -14), new Vector2(W - 170, 46));
            Title.fontSize = 24;

            _greet = UIFactory.Text(Body, "", 17, UIFactory.TextMain, TextAnchor.UpperLeft);
            _greet.rectTransform.At(new Vector2(0, 1), Vector2.zero, new Vector2(W - 56, 92), new Vector2(0, 1));
            _greet.lineSpacing = 8f;
            _content = UIFactory.Node("Content", Body).Fill(0, 0, 100, 0);

            // 대기실 조합 직원 카드 줄 (오른쪽 아래)
            _staff = UIFactory.Panel(Root.transform, "Staff", UIFactory.RadiusPanel, 0.6f).rectTransform
                .At(new Vector2(1, 0), new Vector2(-24, 24), new Vector2(600, 300), new Vector2(1, 0));

            QuestCard.Init();
        }

        // ================= 열기 =================

        /// <summary>필드 NPC와 대화하다 [퀘스트 목록보기].</summary>
        public void OpenFor(NpcAngler npc)
        {
            _npc = null;
            _lobby = false;
            Begin(QuestNpcs.KeyFor(npc.NpcId, FieldMap.Id));
            // 다른 창을 닫는 동안(OpenExclusive)에는 비워 두었다가, 연 뒤에 대화 상대를 기억하고 돌아보게 한다
            _npc = npc;
            if (Game.Player != null) npc.FaceTarget(Game.Player.transform);
            Refresh();
        }

        /// <summary>키로 바로 연다 (개발용 촬영, 다른 창에서).</summary>
        public void OpenFor(string key)
        {
            _npc = null;
            _lobby = QuestNpcs.Get(key)?.InLobby == true && Game.Mode == GameMode.Lobby;
            Begin(key);
        }

        /// <summary>대기실 [퀘스트]: 조합 직원 줄 + 대화 카드. 완료할 퀘스트나 새 퀘스트가 있는 직원을 먼저 고른다.</summary>
        public void OpenLobby(string key = null)
        {
            _npc = null;
            _lobby = true;
            if (key == null)
            {
                var best = QuestSystem.Mark.None;
                foreach (var n in QuestNpcs.Lobby)
                {
                    var m = Game.Quests.MarkFor(n.Key);
                    if (key == null || m > best) { best = m; key = n.Key; }
                }
                if (best == QuestSystem.Mark.None) key = QuestNpcs.Desk;
            }
            Begin(key);
        }

        void Begin(string key)
        {
            _key = key;
            _detail = null;
            _confirmAbandon = false;
            if (!IsOpen) Game.UI.OpenExclusive(this);
            // 그 사람을 만나는 퀘스트는 말을 거는 순간 완료 (편지 카드가 뜬다)
            Game.Quests.Talked(key);
            Refresh();
        }

        public void ShowDetail(QuestDef q)
        {
            _detail = q;
            _confirmAbandon = false;
            if (IsOpen) Refresh();
        }

        public override void Close()
        {
            if (_npc != null) _npc.FaceTarget(null);
            _npc = null;
            base.Close();
        }

        // ================= 그리기 =================

        public override void Refresh()
        {
            var n = QuestNpcs.Get(_key);
            string name = n?.Name ?? _npc?.Look.Name ?? "조합";
            string epithet = n?.Epithet ?? _npc?.Look.Title ?? "";
            Title.text = $"{name}  <size=16><color=#9fd8ff><noparse><{epithet}></noparse></color></size>";
            _avatar.Set(name, n != null ? n.Color : AvatarColor(_key));
            _greet.text = n?.Greeting ?? "무슨 일이신가요?";

            UIFactory.ClearChildren(_content);
            if (_detail != null) Detail(_detail);
            else List();
            _staff.gameObject.SetActive(_lobby);
            if (_lobby) Staff();
        }

        static Color AvatarColor(string key)
        {
            int h = Mathf.Abs((key ?? "").GetHashCode());
            return Color.HSVToRGB((h % 360) / 360f, 0.35f, 0.55f);
        }

        void List()
        {
            var quests = Game.Quests;
            var avail = quests.AvailableAt(_key);
            var going = quests.InProgressAt(_key);
            var list = UIFactory.Node("List", _content).Fill(0, 0, 0, 56);
            var rows = new RowCursor(list, 0f, "questtalk");
            if (avail.Count > 0)
            {
                Cell(rows.Next(28), "수락가능한 퀘스트", 6, 400, 17, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                foreach (var q in avail) QuestRow(rows, q, false);
                rows.Next(8);
            }
            if (going.Count > 0)
            {
                Cell(rows.Next(28), "진행중인 퀘스트", 6, 400, 17, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                foreach (var q in going) QuestRow(rows, q, true);
            }
            if (avail.Count == 0 && going.Count == 0)
                Cell(rows.Next(60), "지금은 부탁드릴 일이 없네요.\n<size=14><color=#8090a0>이야기를 진행하거나 레벨이 오르면 새 퀘스트가 생깁니다.</color></size>", 6, W - 70, 17, UIFactory.TextDim);

            var bottom = UIFactory.Node("Bottom", _content);
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.anchoredPosition = Vector2.zero; bottom.sizeDelta = new Vector2(0, 48);
            var back = RowButton(bottom, "돌아가기", Back, W - 56 - 170, 170, UIFactory.BtnNeutral, true, 18);
            UIFactory.SetRadius(back.targetGraphic, UIFactory.Pill);
        }

        void QuestRow(RowCursor rows, QuestDef q, bool going)
        {
            var quests = Game.Quests;
            bool ready = going && quests.IsReady(q);
            var row = rows.Next(36, 3);
            var b = UIFactory.Button(row, "", () => ShowDetail(q), new Color(1f, 1f, 1f, ready ? 0.12f : 0.05f), 16);
            ((RectTransform)b.transform).Fill();
            var label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Left;
            label.rectTransform.Fill(12, 110, 0, 0);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.text = (ready ? "<color=#7ee07e>완 료</color>  " : "") + $"<color=#9fb0c0>Lv.{q.Lv}</color>  {q.Title}";
            if (q.TypeTag.Length > 0)
            {
                var tag = UIFactory.Text(b.transform, $"({q.TypeTag})", 14, ColorOf(q), TextAnchor.MiddleRight);
                tag.rectTransform.Fill(0, 12, 0, 0);
            }
        }

        public static Color ColorOf(QuestDef q)
        {
            ColorUtility.TryParseHtmlString(q.TypeColor, out var c);
            return c;
        }

        void Detail(QuestDef q)
        {
            var quests = Game.Quests;
            bool active = quests.IsActive(q);
            bool ready = active && quests.IsReady(q);
            bool here = q.Reporter == _key;

            // 위: 이야기 글 (스크롤), 아래: 보상 + 버튼 (고정)
            var top = UIFactory.Node("Text", _content).Fill(0, 0, 0, 190);
            var rows = new RowCursor(top, 0f, "questtalk_detail");
            Cell(rows.Next(30), $"Lv. {q.Lv}  {q.Title}", 4, W - 70, 20, ColorOf(q), TextAnchor.MiddleLeft, FontStyle.Bold);
            string goal = active ? quests.ObjectiveLine(q) : q.Objective;
            Cell(rows.Next(24, 2), goal, 4, W - 70, 17, new Color(0.5f, 0.9f, 0.45f));
            var rep = QuestNpcs.Get(q.Reporter);
            Cell(rows.Next(24, 2), $"[보고자] {rep?.Where ?? "-"}", 4, W - 70, 16, UIFactory.TextMain);
            if (q.Goal == QuestGoal.Deliver)
                Cell(rows.Next(24, 2), $"[퀘스트 물건] {q.ItemName} 1개" + (active ? "  <color=#7ee07e>(가지고 있음)</color>" : " (수락하면 받습니다)"), 4, W - 70, 15, UIFactory.Gold);
            if (ready && !here)
                Cell(rows.Next(24, 2), $"목표 달성! {rep?.Where}에게 가면 완료할 수 있습니다.", 4, W - 70, 15, UIFactory.Good);
            rows.Next(6);
            var story = Cell(rows.Next(10), q.Desc ?? "", 6, W - 76, 16, new Color(0.86f, 0.89f, 0.92f), TextAnchor.UpperLeft);
            story.lineSpacing = 6f;
            float h = story.GetPreferredValues(story.text, W - 76, 0).y + 12f;
            ((RectTransform)story.transform.parent).sizeDelta = new Vector2(0, h);
            rows.Next(h - 10f, 0f);

            // 보상
            var reward = UIFactory.Node("Reward", _content);
            reward.anchorMin = new Vector2(0, 0); reward.anchorMax = new Vector2(1, 0); reward.pivot = new Vector2(0.5f, 0);
            reward.anchoredPosition = new Vector2(0, 56); reward.sizeDelta = new Vector2(0, 128);
            UIFactory.Divider(reward, "Line").rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(W - 60, 1));
            Cell(Strip(reward, -6, 26), $"[퀘스트 완료 보상] {q.RewardLine}", 4, W - 70, 16, UIFactory.Gold);
            var items = q.RewardItems;
            for (int i = 0; i < 5; i++)
            {
                var slot = UIFactory.Shape(reward, new Color(1f, 1f, 1f, i < items.Count ? 0.12f : 0.05f), 10f, "Slot", 0f).rectTransform;
                slot.At(new Vector2(0, 1), new Vector2(4 + i * 96, -38), new Vector2(88, 80), new Vector2(0, 1));
                if (i < items.Count)
                    UIFactory.Text(slot, items[i], 13, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill(4, 4, 4, 4);
            }

            // 버튼
            var bar = UIFactory.Node("Buttons", _content);
            bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(0.5f, 0);
            bar.anchoredPosition = Vector2.zero; bar.sizeDelta = new Vector2(0, 48);
            float bw = 150f, right = W - 56;
            var buttons = new List<(string, System.Action, Color)>();
            if (!active)
            {
                bool can = quests.AvailableAt(_key).Contains(q);
                if (can) buttons.Add(("수 락", () => { if (quests.Accept(q)) { _detail = null; Refresh(); } }, UIFactory.BtnPrimary));
            }
            else
            {
                if (ready && here) buttons.Add(("완 료", () => { if (quests.Finish(q, _key)) { _detail = null; Refresh(); } }, UIFactory.BtnGood));
                buttons.Add((_confirmAbandon ? "정말 포기?" : "포 기", () =>
                {
                    if (!_confirmAbandon) { _confirmAbandon = true; Refresh(); return; }
                    quests.Abandon(q);
                    _detail = null;
                    Refresh();
                }, UIFactory.BtnDanger));
            }
            buttons.Add(("돌아가기", () => { _detail = null; _confirmAbandon = false; Refresh(); }, UIFactory.BtnNeutral));
            for (int i = 0; i < buttons.Count; i++)
            {
                var (label, act, col) = buttons[i];
                float x = i == buttons.Count - 1 ? right - bw : i * (bw + 10f);
                var b = RowButton(bar, label, act, x, bw, col, true, 18);
                UIFactory.SetRadius(b.targetGraphic, UIFactory.Pill);
            }
        }

        static RectTransform Strip(RectTransform parent, float y, float h)
        {
            var rt = UIFactory.Node("Strip", parent);
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y); rt.sizeDelta = new Vector2(0, h);
            return rt;
        }

        void Back()
        {
            if (_detail != null) { _detail = null; Refresh(); return; }
            if (_lobby) { Close(); return; }
            var npc = _npc;
            Close();
            if (npc != null) Game.UI.TalkTo(npc);
        }

        /// <summary>대기실 조합 직원 카드 (원작: 초상화 4장, 퀘스트 있는 사람 머리 위 !).</summary>
        void Staff()
        {
            UIFactory.ClearChildren(_staff);
            UIFactory.Text(_staff, "한국 낚시 조합", 16, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(22, -10), new Vector2(300, 24), new Vector2(0, 1));
            const float cw = 128f, gap = 12f;
            for (int i = 0; i < QuestNpcs.Lobby.Count; i++)
            {
                var n = QuestNpcs.Lobby[i];
                bool sel = n.Key == _key;
                var card = UIFactory.Button(_staff, "", () => { if (_key != n.Key) Begin(n.Key); }, sel ? new Color(1f, 1f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.05f), 16);
                UIFactory.SetRadius(card.targetGraphic, 16f);
                var rt = (RectTransform)card.transform;
                rt.At(new Vector2(0, 1), new Vector2(22 + i * (cw + gap), -76), new Vector2(cw, 136), new Vector2(0, 1));
                if (sel) UIFactory.AddBorder((UIShape)card.targetGraphic, new Color(0.55f, 0.95f, 0.9f, 0.8f), 2f, 4f);
                var av = new TranslucentAvatar(rt, new Vector2((cw - 72) / 2f, -10), 72f);
                av.Set(n.Name, n.Color);
                var label = card.GetComponentInChildren<TMP_Text>();
                label.text = $"<b>{n.Name}</b>\n<size=12><color=#9fb0c0>{n.Epithet}</color></size>";
                label.alignment = TextAlignmentOptions.Bottom;
                label.rectTransform.Fill(4, 4, 0, 8);
                // 머리 위 표시
                var m = Game.Quests.MarkFor(n.Key);
                var (txt, col) = MarkGlyph(m);
                if (txt.Length > 0)
                {
                    var mk = UIFactory.Text(rt, txt, 30, col, TextAnchor.MiddleCenter, FontStyle.Bold);
                    mk.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, 26), new Vector2(40, 40));
                    UIFactory.Outline(mk, new Color(0.1f, 0.06f, 0f, 1f), 2f);
                }
            }
            var n0 = QuestNpcs.Get(_key);
            UIFactory.Text(_staff, n0 != null ? $"<b>{n0.Name}</b>\n<size=15><color=#c8d4de>- {LobbyLine(n0)}</color></size>" : "", 18, UIFactory.TextMain, TextAnchor.UpperLeft)
                .rectTransform.At(new Vector2(0, 0), new Vector2(22, 14), new Vector2(360, 66), new Vector2(0, 0));
            var exit = UIFactory.Button(_staff, "<b>돌아가기</b>\n<size=11><color=#9fc8e0>EXIT</color></size>", Close, UIFactory.BtnBlue, 17);
            UIFactory.SetRadius(exit.targetGraphic, 14f);
            ((RectTransform)exit.transform).At(new Vector2(1, 0), new Vector2(-20, 16), new Vector2(170, 56), new Vector2(1, 0));
        }

        static string LobbyLine(QuestNpc n) => n.Key switch
        {
            QuestNpcs.Desk => "어머, 무슨 일이시죠? 혹시 저 보러 오신 건가요?",
            QuestNpcs.Chief => "바다 소식은 언제 들어도 설레는군.",
            QuestNpcs.Doctor => "어서 오게. 오늘은 무슨 고기 이야기를 가져왔나?",
            _ => "서류가 산더미예요. 그래도 손님은 언제나 환영이에요.",
        };

        /// <summary>머리 위 표시 글자와 색 (원작: 노란 ! 이야기 · 하늘색 ! 그 밖 · 노란 ? 완료 가능 · 회색 ? 진행 중).</summary>
        public static (string, Color) MarkGlyph(QuestSystem.Mark m) => m switch
        {
            QuestSystem.Mark.Ready => ("?", new Color(1f, 0.82f, 0.2f)),
            QuestSystem.Mark.AvailableStory => ("!", new Color(1f, 0.82f, 0.2f)),
            QuestSystem.Mark.Available => ("!", new Color(0.45f, 0.85f, 1f)),
            QuestSystem.Mark.Working => ("?", new Color(0.7f, 0.72f, 0.75f)),
            _ => ("", Color.white),
        };

        // ================= 힌트맵 =================

        /// <summary>
        /// 원작 퀘스트 책 [힌트맵]: 갈 곳(보고자, 아직이면 퀘스트 낚시터)을 지역 지도(M)에 핀으로 보여 준다.
        /// 다른 낚시터나 대기실이면 어디로 가야 하는지 알려 준다.
        /// </summary>
        public static void ShowHint(QuestDef q)
        {
            var quests = Game.Quests;
            bool toReporter = quests.IsReady(q) || q.Goal == QuestGoal.Meet || q.Goal == QuestGoal.Deliver || q.MapId == null;
            var npc = QuestNpcs.Get(toReporter ? q.Reporter : (QuestNpcs.Get(q.Reporter)?.MapId == q.MapId ? q.Reporter : q.Giver));
            string map = toReporter ? npc?.MapId : q.MapId;
            if (map == null)
            {
                Game.Toast($"[힌트맵] {npc?.Name ?? "조합 직원"}{QuestNpcs.Josa(npc?.Name, "은", "는")} 대기실(한국 낚시 조합)에 있습니다. ◀ 조합 → [퀘스트]", ToastKind.Info);
                return;
            }
            if (Game.Mode != GameMode.Field || map != FieldMap.Id)
            {
                Game.Toast($"[힌트맵] {Maps.Get(map).ShortName}의 퀘스트입니다. 대기실 [낚시터 선택하기]에서 {Maps.Get(map).ShortName}에 입장하세요." +
                           (npc != null && npc.MapId == map ? $" ({npc.Name})" : ""), ToastKind.Info);
                return;
            }
            Vector3? pos = null;
            foreach (var a in NpcAngler.Talkable)
                if (a != null && npc != null && QuestNpcs.KeyFor(a.NpcId, FieldMap.Id) == npc.Key) { pos = a.transform.position; break; }
            Game.UI.OpenExclusive(Game.UI.AreaMap);
            Game.UI.AreaMap.SetHint(pos, npc != null ? npc.Name : q.Title);
        }
    }

    /// <summary>원형 이름 카드 (초상화 대신: 색 원 + 이름 첫 글자).</summary>
    public sealed class TranslucentAvatar
    {
        readonly Graphic _bg;
        readonly TMP_Text _letter;

        public TranslucentAvatar(RectTransform parent, Vector2 topLeft, float size)
        {
            var bg = UIFactory.Shape(parent, Color.gray, UIFactory.Pill, "Avatar", 0f);
            bg.rectTransform.At(new Vector2(0, 1), topLeft, new Vector2(size, size), new Vector2(0, 1));
            UIFactory.AddBorder(bg, new Color(1f, 1f, 1f, 0.35f), 2f);
            _bg = bg;
            _letter = UIFactory.Text(bg.transform, "", size * 0.45f, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _letter.rectTransform.Fill();
        }

        public void Set(string name, Color color)
        {
            _bg.color = color;
            // "어촌계 정계장"처럼 직함이 앞에 붙으면 마지막 낱말의 첫 글자
            var word = string.IsNullOrEmpty(name) ? "?" : name.Substring(name.LastIndexOf(' ') + 1);
            _letter.text = word.Substring(0, 1);
        }
    }

    /// <summary>
    /// 원작 퀘스트 완료 편지 카드 (13 문서 1:28): 화면 위쪽 가운데 어두운 가로 띠 위에 항공우편 봉투 모양 카드,
    /// "제목 퀘스트 완료!! / N Gold 를 얻었습니다. / N Exp 를 얻었습니다." 2.5초쯤 떠 있다 사라진다. 여러 개면 차례로.
    /// </summary>
    public static class QuestCard
    {
        static bool _inited;
        static QuestCardRunner _runner;
        internal static readonly Queue<string> Pending = new Queue<string>();

        public static void Init()
        {
            if (_inited) return;
            _inited = true;
            QuestSystem.QuestCompleted += (q, gold, exp) => Show(q.Title, gold, exp);
        }

        public static void Show(string title, int gold, int exp)
        {
            Ensure();
            Pending.Enqueue($"{title} 퀘스트 완료!!\n{gold} Gold 를 얻었습니다.\n{exp} Exp 를 얻었습니다.");
        }

        public static bool Showing => _runner != null && _runner.Busy;

        static void Ensure()
        {
            if (_runner != null) return;
            var canvas = UIFactory.CreateCanvas("QuestCardCanvas", 40);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            _runner = canvas.gameObject.AddComponent<QuestCardRunner>();
            _runner.Build((RectTransform)canvas.transform);
        }
    }

    public class QuestCardRunner : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _card;
        TMP_Text _text;
        float _t = -1f;
        const float FadeIn = 0.25f, Hold = 2.5f, FadeOut = 0.45f;

        public bool Busy => _t >= 0f || QuestCard.Pending.Count > 0;

        public void Build(RectTransform root)
        {
            var layer = UIFactory.Node("QuestCard", root).Fill();
            _group = layer.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            // 어두운 가로 띠 (위아래 가장자리는 옅게)
            var band = UIFactory.Image(layer, new Color(0f, 0f, 0f, 0.55f), null, "Band").rectTransform;
            band.anchorMin = new Vector2(0, 0.5f); band.anchorMax = new Vector2(1, 0.5f); band.pivot = new Vector2(0.5f, 0.5f);
            band.anchoredPosition = new Vector2(0, 220); band.sizeDelta = new Vector2(0, 200);
            foreach (float y in new[] { 104f, -104f })
            {
                var edge = UIFactory.Image(band, new Color(0f, 0f, 0f, 0.28f), null, "Edge").rectTransform;
                edge.anchorMin = new Vector2(0, 0.5f); edge.anchorMax = new Vector2(1, 0.5f);
                edge.anchoredPosition = new Vector2(0, y); edge.sizeDelta = new Vector2(0, 8);
            }

            // 편지 카드: 미색 봉투 + 위쪽 빨강·파랑 빗금 띠 + 오른쪽 위 우표
            var card = UIFactory.Shape(band, new Color(0.96f, 0.94f, 0.89f, 1f), 14f, "Card", 4f);
            UIFactory.AddShadow(card, 22f, new Vector2(0, -6), 0.5f);
            UIFactory.AddBorder(card, new Color(0.55f, 0.42f, 0.28f, 0.9f), 3f);
            _card = card.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 172));
            for (int i = 0; i < 12; i++)
            {
                var stripe = UIFactory.Image(_card, i % 2 == 0 ? new Color(0.8f, 0.22f, 0.2f) : new Color(0.18f, 0.32f, 0.65f), null, "Stripe").rectTransform;
                stripe.At(new Vector2(0, 1), new Vector2(26 + i * 40, -12), new Vector2(26, 5), new Vector2(0, 1));
                stripe.localRotation = Quaternion.Euler(0, 0, 0f);
            }
            var stamp = UIFactory.Shape(_card, new Color(0.55f, 0.75f, 0.78f, 1f), 4f, "Stamp", 0f);
            UIFactory.AddBorder(stamp, Color.white, 3f);
            var srt = stamp.rectTransform.At(new Vector2(1, 1), new Vector2(-14, -8), new Vector2(58, 66), new Vector2(1, 1));
            srt.localRotation = Quaternion.Euler(0, 0, -8f);
            UIFactory.Text(srt, "QUEST", 11, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();

            _text = UIFactory.Text(_card, "", 21, new Color(0.22f, 0.19f, 0.16f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _text.rectTransform.Fill(70, 70, 26, 10);
            _text.lineSpacing = 4f;
        }

        void Update()
        {
            if (_t < 0f)
            {
                if (QuestCard.Pending.Count == 0) return;
                _text.text = QuestCard.Pending.Dequeue();
                _t = 0f;
                if (Game.Sound != null) Game.Sound.Coin();
            }
            _t += Time.unscaledDeltaTime;
            float a = _t < FadeIn ? _t / FadeIn : _t < FadeIn + Hold ? 1f : 1f - (_t - FadeIn - Hold) / FadeOut;
            _group.alpha = Mathf.Clamp01(a);
            float s = _t < FadeIn ? Mathf.Lerp(0.88f, 1f, _t / FadeIn) : 1f;
            _card.localScale = new Vector3(s, s, 1f);
            if (_t >= FadeIn + Hold + FadeOut)
            {
                _t = -1f;
                _group.alpha = 0f;
            }
        }
    }
}
