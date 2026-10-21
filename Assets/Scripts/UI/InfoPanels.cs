using System.Linq;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>조합 퀘스트 창: 현재 퀘스트와 완료한 퀘스트 목록.</summary>
    /// <summary>
    /// 퀘스트(L). 첫 탭은 원작 퀘스트 책(13 문서 2:42): 왼쪽에 진행 중인 퀘스트(STORY/REPEAT/GUIDE 배지 + Lv, 일반은 Lv만),
    /// 오른쪽에 [보고자]·[임무]·보상·이야기 글·[힌트맵], 아래 [퀘스트 포기]. 퀘스트는 NPC에게 받고 [보고자]에게 가서 완료한다 (QuestTalkPanel).
    /// 나머지 탭: 이야기·의뢰 현황 / 대물 도전 / 업적 / 대회 / 빙고·출석 / 일지.
    /// </summary>
    public partial class QuestPanel : UIPanel
    {
        int _tab;

        public QuestPanel(Transform canvas)
        {
            BuildFrame(canvas, "QuestPanel", UIFactory.Caption("조합 퀘스트", "QUEST"), new Vector2(1000, 880));
        }

        public void ShowTab(int tab)
        {
            _tab = tab;
            if (IsOpen) Refresh();
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(Body);
            // 탭 줄은 고정, 그 아래 목록만 스크롤 (탭마다 스크롤 위치를 따로 기억)
            var tabs = UIFactory.Node("Tabs", Body);
            tabs.anchorMin = new Vector2(0, 1); tabs.anchorMax = new Vector2(1, 1); tabs.pivot = new Vector2(0.5f, 1);
            tabs.anchoredPosition = Vector2.zero; tabs.sizeDelta = new Vector2(0, 42);
            var rows = new RowCursor(UIFactory.Node("List", Body).Fill(0, 0, 52, 0), 0f, "quest" + _tab);
            // 원작 퀘스트 색: 이야기 보라 · 일일 파랑 · 일반 노랑 (탭 이름 앞 점)
            string[] names = { "<color=#c49aff>●</color> 퀘스트", "<color=#6ab8ff>●</color> 이야기·의뢰", "<color=#ffd060>●</color> 대물 도전", "업적 · 칭호", "대회", "빙고 · 출석", "일지" };
            int[] order = { 0, 1, 2, 5, 3, 4, 6 };
            for (int k = 0; k < order.Length; k++)
            {
                int t = order[k];
                var tb = RowButton(tabs, names[t], () => { _tab = t; Refresh(); }, k * 138f, 132,
                    _tab == t ? UIFactory.TabOn : UIFactory.TabOff, true, 16);
                UIFactory.SetRadius(tb.targetGraphic, UIFactory.Pill);
            }
            Title.text = $"{UIFactory.Caption("조합 퀘스트", "QUEST")}   <size=18><color=#ffd060>소라고동 {Game.Profile.Count(AchievementDatabase.ConchId)}개</color></size>";
            switch (_tab)
            {
                case 0: QuestBook(); break;         // 원작 퀘스트 책: 왼쪽 목록 + 오른쪽 상세
                case 1: Overview(rows); break;  // 이야기 줄기·오늘의 일일반복·가이드/일반 현황
                case 2: Trophy(rows); break;
                case 3: AchievementBook(rows); break;   // 기록 책 업적 분류 (QuestAchievementsTab.cs)
                case 5: BingoTab(rows); break;
                case 6: Journal(rows); break;
                default: Tournament(rows); break;
            }
        }

        static readonly Color StoryColor = new Color(0.77f, 0.6f, 1f), DailyColor = new Color(0.42f, 0.72f, 1f);

        void BingoTab(RowCursor rows)
        {
            var p = Game.Profile;
            Bingo.EnsureCard();
            var s = p.Data.bingo;
            int lines = Bingo.Lines();
            Cell(rows.Next(30), $"빙고 {s.card}번째 판  <size=14><color=#8090a0>칸에 적힌 고기를 낚으면 칠해진다 · 한 줄마다 소라고동 {Bingo.LineConch} + {Bingo.LineGold:N0}골드 · " +
                                $"{Bingo.LinesForBig}줄이면 {Bingo.BigRewardText(s)}</color></size>", 4, 960, 20, UIFactory.Accent);
            var head = rows.Next(34, 4);
            Cell(head, $"완성한 줄 <color=#ffd060>{lines}</color> / 12   ·   3줄 보상 {(s.bigPaid ? "<color=#70e070>받음</color>" : "아직")}   ·   3줄 채운 판 {s.cardsCleared}장",
                4, 640, 18);
            if (s.bigPaid) RowButton(head, "새 빙고판 받기", () => { Bingo.NewCard(); Refresh(); }, 720, 220, UIFactory.BtnGood, true, 17);

            const float cw = 176f, ch = 72f, gap = 6f;
            for (int r = 0; r < Bingo.Size; r++)
            {
                var row = rows.Next(ch, gap);
                for (int c = 0; c < Bingo.Size; c++)
                {
                    int i = r * Bingo.Size + c;
                    if (i >= s.cells.Count) break;
                    bool done = s.done[i];
                    string cell = s.cells[i];
                    var bg = UIFactory.Shape(row, done ? new Color(0.25f, 0.6f, 0.35f, 0.85f) : new Color(1f, 1f, 1f, 0.06f), 12f, "Cell", 0f);
                    bg.rectTransform.At(new Vector2(0, 0.5f), new Vector2(100 + c * (cw + gap), 0), new Vector2(cw, ch), new Vector2(0, 0.5f));
                    if (cell.StartsWith("sp:"))
                    {
                        var sp = FishDatabase.Get(cell.Substring(3));
                        var icon = sp != null ? FishModels.Icon(sp) : null;
                        if (icon != null)
                        {
                            var img = UIFactory.Image(bg.transform, done ? Color.white : new Color(1f, 1f, 1f, 0.55f), icon, "Icon");
                            img.preserveAspect = true;
                            img.rectTransform.At(new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(56, 56), new Vector2(0, 0.5f));
                        }
                    }
                    bool hasIcon = cell.StartsWith("sp:");
                    var t = UIFactory.Text(bg.transform, $"{(done ? "<color=#ffffff>" : "")}{Bingo.CellText(cell)}{(done ? "</color>" : "")}" +
                                                         (hasIcon ? $"\n<size=12><color=#9ab0c0>{Bingo.CellHint(cell)}</color></size>" : ""),
                        cell == "free" ? 22 : 15, done ? Color.white : UIFactory.TextMain, hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, FontStyle.Bold);
                    t.rectTransform.Fill(hasIcon ? 64 : 4, 4, 4, 4);
                    if (done)
                        UIFactory.Text(bg.transform, "★", 22, new Color(1f, 0.9f, 0.4f), TextAnchor.UpperRight, FontStyle.Bold).rectTransform.Fill(4, 2, 8, 2);
                }
            }

            // 출석 체크 (실제 날짜)
            rows.Next(10);
            UIFactory.SectionTitle(rows.Next(30), "출석 체크", 4, 18);
            var d = p.Data;
            int stamped = d.attendStamp % Attendance.Rewards.Length;
            if (stamped == 0 && d.attendStamp > 0 && Attendance.CheckedToday) stamped = Attendance.Rewards.Length;
            Cell(rows.Next(26), $"게임을 켠 날마다(실제 날짜) 도장 하나 · 7일째 특수 배 「블랙스피어」 · 모두 {d.attendTotal}일 출석" +
                                (Attendance.CheckedToday ? "  <color=#70e070>오늘 출석 완료</color>" : ""), 4, 960, 15, UIFactory.TextDim);
            var stampRow = rows.Next(70, 4);
            for (int k = 0; k < Attendance.Rewards.Length; k++)
            {
                bool got = k < stamped;
                var box = UIFactory.Shape(stampRow, got ? new Color(0.85f, 0.55f, 0.2f, 0.85f) : new Color(1f, 1f, 1f, 0.06f), 12f, "Stamp", 0f);
                box.rectTransform.At(new Vector2(0, 0.5f), new Vector2(4 + k * 136, 0), new Vector2(128, 66), new Vector2(0, 0.5f));
                UIFactory.Text(box.transform, $"<b>{k + 1}일째</b>{(got ? " ★" : "")}\n<size=12>{Attendance.Rewards[k].text}</size>", 15,
                    got ? Color.white : UIFactory.TextMain, TextAnchor.MiddleCenter).rectTransform.Fill(4, 4, 4, 4);
            }
        }

        void Journal(RowCursor rows)
        {
            var list = Game.Profile.Data.journal;
            Cell(rows.Next(30), $"낚시 일지  <size=14><color=#8090a0>처음 낚은 어종 · 대물 · 레벨 업 · 퀘스트 · 대회 · 출석을 날짜별로 적어 둔다 (최근 {PlayerProfile.JournalMax}줄)</color></size>",
                4, 960, 20, UIFactory.Accent);
            if (list.Count == 0) { Cell(rows.Next(30), "아직 적힌 것이 없습니다. 낚시를 시작해 보세요!", 4, 900, 17, UIFactory.TextDim); return; }
            int lastDay = -1;
            int shown = 0;
            for (int i = list.Count - 1; i >= 0 && shown < 80; i--, shown++)
            {
                var e = list[i];
                if (e.day != lastDay)
                {
                    lastDay = e.day;
                    rows.Next(4);
                    Cell(rows.Next(26), $"{e.day}일차", 4, 300, 17, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                }
                int h = Mathf.FloorToInt(e.minutes / 60f) % 24, m = Mathf.FloorToInt(e.minutes % 60f);
                Cell(rows.Next(24, 1), $"<color=#8090a0>{h:00}:{m:00}</color>   {e.text}", 20, 920, 16, UIFactory.TextMain);
            }
        }

        /// <summary>행 안에 알약 진행 바 + "n/m" 숫자.</summary>
        static void ProgressBar(RectTransform row, int value, int max, float x, float width)
        {
            float t = max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;
            var fill = UIFactory.Bar(row, new Color(0f, 0f, 0f, 0.35f), t >= 1f ? UIFactory.Good : UIFactory.Accent, "Progress");
            fill.fillAmount = t;
            var track = (RectTransform)fill.transform.parent;
            track.anchorMin = track.anchorMax = new Vector2(0, 0.5f);
            track.pivot = new Vector2(0, 0.5f);
            track.anchoredPosition = new Vector2(x, 0);
            track.sizeDelta = new Vector2(width - 70, 10);
            Cell(row, $"{value}/{max}", x + width - 64, 64, 16, t >= 1f ? UIFactory.Good : UIFactory.TextMain, TextAnchor.MiddleRight, FontStyle.Bold);
        }

        // ================= 퀘스트 책 (원작 L) =================

        string _sel;
        bool _confirmAbandon;

        /// <summary>책에서 고를 퀘스트 (자동 촬영 등).</summary>
        public void Select(QuestDef q)
        {
            _sel = q?.Id;
            _confirmAbandon = false;
            if (IsOpen) Refresh();
        }

        static RectTransform Pane(RectTransform parent, string name, float x, float width, bool right)
        {
            var rt = UIFactory.Shape(parent, UIFactory.Surface, UIFactory.RadiusCard, name, 0f).rectTransform;
            rt.anchorMin = new Vector2(right ? 1 : 0, 0);
            rt.anchorMax = new Vector2(right ? 1 : 0, 1);
            rt.pivot = new Vector2(right ? 1 : 0, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);
            rt.sizeDelta = new Vector2(width, 0);
            return rt;
        }

        static Color TypeColorOf(QuestDef q) => QuestTalkPanel.ColorOf(q);

        /// <summary>원작 배지: STORY(보라) · REPEAT(파랑) · GUIDE(연보라) 위에 작은 글씨, 아래 Lv.N. 일반은 배지 없이 "Lv. N".</summary>
        static void Badge(RectTransform row, QuestDef q)
        {
            if (q.Type == QuestType.Normal)
            {
                Cell(row, $"Lv. {q.Lv}", 10, 66, 14, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
                return;
            }
            var col = q.Type switch { QuestType.Story => new Color(0.48f, 0.3f, 0.78f), QuestType.Repeat => new Color(0.2f, 0.48f, 0.82f), _ => new Color(0.62f, 0.38f, 0.75f) };
            var b = UIFactory.Shape(row, col, 6f, "Badge", 0f).rectTransform;
            b.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(60, 32), new Vector2(0, 0.5f));
            UIFactory.Text(b, $"<size=9>{q.Badge}</size>\n<b>Lv.{q.Lv}</b>", 13, Color.white, TextAnchor.MiddleCenter).rectTransform.Fill(2, 2, 1, 1);
        }

        void QuestBook()
        {
            var quests = Game.Quests;
            var active = quests.Active();
            var area = UIFactory.Node("Book", Body).Fill(0, 0, 52, 0);
            var left = Pane(area, "Left", 0, 420, false);
            var right = Pane(area, "Right", 0, 516, true);
            var sel = active.Find(q => q.Id == _sel) ?? (active.Count > 0 ? active[0] : null);
            _sel = sel?.Id;

            // ---- 왼쪽: 진행 중인 퀘스트 ----
            var rows = new RowCursor(UIFactory.Node("ListArea", left).Fill(8, 14, 10, 66), 0f, "questbook");
            foreach (var q in active)
            {
                var row = rows.Next(42, 3);
                bool on = q == sel;
                var b = UIFactory.Button(row, "", () => { _sel = q.Id; _confirmAbandon = false; Refresh(); },
                    on ? new Color(1f, 1f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.03f), 16);
                ((RectTransform)b.transform).Fill();
                var label = b.GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.Left;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.rectTransform.Fill(76, q.Type == QuestType.Repeat ? 84 : 10, 0, 0);
                bool ready = quests.IsReady(q);
                label.text = (ready ? "<color=#7ee07e>완 료</color> " : "") + q.Title;
                Badge((RectTransform)b.transform, q);
                if (q.Type == QuestType.Repeat)
                    UIFactory.Text(b.transform, "(일일반복)", 13, TypeColorOf(q), TextAnchor.MiddleRight).rectTransform.Fill(0, 10, 0, 0);
            }
            if (active.Count == 0)
            {
                Cell(rows.Next(70), "진행중인 퀘스트가 없습니다.\n<size=14><color=#8090a0>머리 위에 <color=#ffd060>!</color> 가 뜬 NPC에게 말을 걸어\n[퀘스트 목록보기]에서 수락하세요.</color></size>",
                    10, 390, 17, UIFactory.TextDim);
            }
            var next = quests.Current;
            if (next != null && !quests.IsActive(next))
            {
                rows.Next(8);
                var giver = QuestNpcs.Get(next.Giver);
                Cell(rows.Next(60), $"<color=#c49aff>받을 이야기</color>  Lv.{next.Lv} {next.Title}\n<size=14><color=#8090a0>{giver?.Where ?? "조합"}에게서" +
                                    (giver != null && giver.InLobby ? " (대기실 [퀘스트])" : "") + "</color></size>", 10, 390, 16, UIFactory.TextMain);
            }

            var bottom = UIFactory.Node("Bottom", left);
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.anchoredPosition = new Vector2(0, 10); bottom.sizeDelta = new Vector2(0, 48);
            if (sel != null)
            {
                var give = RowButton(bottom, _confirmAbandon ? "정말 포기할까요?" : "퀘스트 포기", () =>
                {
                    if (!_confirmAbandon) { _confirmAbandon = true; Refresh(); return; }
                    _confirmAbandon = false;
                    quests.Abandon(sel);
                    _sel = null;
                    Refresh();
                }, 110, 200, _confirmAbandon ? UIFactory.BtnDanger : UIFactory.BtnNeutral, true, 17);
                UIFactory.SetRadius(give.targetGraphic, UIFactory.Pill);
            }

            // ---- 오른쪽: 상세 ----
            if (sel == null)
            {
                Cell(UIFactory.Node("Empty", right).Fill(20, 20, 20, 20), "퀘스트를 고르면 [보고자], [임무], 보상과 이야기가 여기에 나옵니다.", 0, 470, 16, UIFactory.TextDim);
                return;
            }
            var d = new RowCursor(UIFactory.Node("Detail", right).Fill(18, 14, 14, 78), 0f, "questbook_detail");
            Cell(d.Next(30), $"Lv. {sel.Lv}  {sel.Title}", 0, 470, 21, TypeColorOf(sel), TextAnchor.MiddleLeft, FontStyle.Bold);
            var rep = QuestNpcs.Get(sel.Reporter);
            Cell(d.Next(22, 2), $"[보고자] {rep?.Where ?? "-"}", 0, 470, 15, UIFactory.TextMain);
            d.Next(6);
            bool isReady = quests.IsReady(sel);
            Cell(d.Next(24, 2), $"[임무] {quests.ObjectiveLine(sel)}" + (isReady ? "  <color=#ffd060>완 료</color>" : ""), 0, 470, 16, new Color(0.5f, 0.9f, 0.45f));
            if (isReady) Cell(d.Next(22, 2), $"→ {QuestSystem.ReportLine(sel)}", 0, 470, 15, UIFactory.Good);
            Cell(d.Next(22, 2), $"[퀘스트 완료 보상] {sel.RewardLine}", 0, 470, 15, UIFactory.Gold);
            var items = sel.RewardItems;
            if (items.Count > 0) Cell(d.Next(22, 2), "[보상 아이템] " + string.Join(", ", items.Select(i => i.Replace("\n", " "))), 0, 470, 14, UIFactory.Gold);
            if (sel.Goal == QuestGoal.Deliver) Cell(d.Next(22, 2), $"[퀘스트 물건] {sel.ItemName} 1개", 0, 470, 14, UIFactory.TextMain);
            d.Next(8);
            var story = Cell(d.Next(10), sel.Desc ?? "", 2, 466, 15, new Color(0.86f, 0.89f, 0.92f), TextAnchor.UpperLeft);
            story.lineSpacing = 6f;
            float h = story.GetPreferredValues(story.text, 466, 0).y + 10f;
            ((RectTransform)story.transform.parent).sizeDelta = new Vector2(0, h);
            d.Next(h - 10f, 0f);

            // 아래: 사용 미끼 · [힌트맵]
            var foot = UIFactory.Node("Foot", right);
            foot.anchorMin = new Vector2(0, 0); foot.anchorMax = new Vector2(1, 0); foot.pivot = new Vector2(0.5f, 0);
            foot.anchoredPosition = new Vector2(0, 12); foot.sizeDelta = new Vector2(0, 52);
            var sp = FishDatabase.Get(sel.SpeciesId);
            string bait = sp != null && sp.Baits.Length > 0 ? string.Join(", ", sp.Baits.Take(3).Select(k => Names.Of(k))) : "-";
            Cell(foot, $"[사용미끼] {bait}", 18, 320, 14, UIFactory.TextDim);
            var hint = RowButton(foot, "[힌트맵]", () => QuestTalkPanel.ShowHint(sel), 516 - 18 - 140, 140, UIFactory.BtnBlue, true, 16);
            UIFactory.SetRadius(hint.targetGraphic, UIFactory.Pill);
        }

        // ================= 이야기·의뢰 현황 =================

        void Overview(RowCursor rows)
        {
            var quests = Game.Quests;
            var p = Game.Profile;

            // 이야기 줄기
            var storyHead = rows.Next(28);
            UIFactory.SectionTitle(storyHead, "이야기 (STORY)", 4, 18);
            ProgressBar(storyHead, quests.CompletedCount, QuestDatabase.Story.Count, 580, 300);
            var cur = quests.Current;
            if (cur != null)
            {
                var giver = QuestNpcs.Get(cur.Giver);
                string state = quests.IsActive(cur) ? (quests.IsReady(cur) ? $"<color=#7ee07e>완료 가능</color> — {QuestSystem.ReportLine(cur)}" : $"진행 중 — {quests.ObjectiveLine(cur)}")
                    : $"<color=#ffd060>받을 수 있음</color> — {giver?.Where ?? "조합"}" + (giver != null && giver.InLobby ? " (대기실 [퀘스트])" : "");
                Cell(rows.Next(26, 2), $"지금: <b>Lv.{cur.Lv} {cur.Title}</b>   <size=15>{state}</size>", 10, 900, 17, UIFactory.TextMain);
            }
            else Cell(rows.Next(30), "모든 이야기 퀘스트를 마쳤습니다!", 10, 900, 20, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < QuestDatabase.Story.Count; i++)
            {
                var def = QuestDatabase.Story[i];
                bool done = i < quests.CompletedCount;
                bool now = i == quests.CompletedCount;
                string mark = done ? "<color=#70e070>●</color>" : now ? "▶" : "○";
                var color = done ? UIFactory.Good : now ? UIFactory.TextMain : UIFactory.TextDim;
                string where = def.MapId != null ? Fishing.World.Maps.Get(def.MapId).ShortName : "대기실";
                Cell(rows.Next(24, 1), $"{mark}  <color=#8090a0>Lv.{def.Lv}</color> {def.Title}" + (done || now ? $"  <size=13><color=#8090a0>{def.Objective} · {where}</color></size>" : ""),
                    10, 900, 16, color);
            }

            // 오늘의 일일반복
            rows.Next(10);
            UIFactory.SectionTitle(rows.Next(28), "오늘의 일일반복 (REPEAT)", 4, 18);
            Cell(rows.Next(22), "게임 하루마다 새로 3개 · 그 낚시터 조합 연락관에게 수락하고, 채우면 다시 찾아가 완료 · 보상에 소라고동 1", 10, 900, 14, UIFactory.TextDim);
            foreach (var q in quests.DailyDefs)
            {
                var row = rows.Next(50, 4);
                bool done = quests.IsDone(q), active = quests.IsActive(q), ready = quests.IsReady(q);
                UIFactory.RowBg(row, active);
                var giver = QuestNpcs.Get(q.Giver);
                string state = done ? "<color=#70e070>완료</color>" : ready ? $"<color=#7ee07e>완료 가능</color> — {giver?.Name}에게" :
                    active ? $"진행 중 {quests.ProgressOf(q)}/{q.Count}" : $"<color=#ffd060>수락 전</color> — {giver?.Where}";
                Cell(row, $"<b><color=#7fc4ff>Lv.{q.Lv}</color> {q.Title}</b>  <size=13><color=#8090a0>{Fishing.World.Maps.Get(q.MapId).ShortName} · {q.Objective} {q.Count}</color></size>\n" +
                          $"<size=14>{state}   <color=#ffd060>{q.RewardLine} · 소라고동 1</color></size>", 12, 900, 16, done ? UIFactory.Good : UIFactory.TextMain);
            }

            // 가이드 · 일반
            rows.Next(10);
            UIFactory.SectionTitle(rows.Next(28), "가이드 · 일반 퀘스트", 4, 18);
            foreach (var q in QuestDatabase.Side)
            {
                bool done = quests.IsDone(q), active = quests.IsActive(q);
                var giver = QuestNpcs.Get(q.Giver);
                bool open = !done && !active && quests.AvailableAt(q.Giver).Contains(q);
                string state = done ? "<color=#70e070>완료</color>" : active ? (quests.IsReady(q) ? "<color=#7ee07e>완료 가능</color>" : $"진행 중 {quests.ProgressOf(q)}/{q.Count}")
                    : open ? $"<color=#ffd060>받을 수 있음</color> — {giver?.Where}" : "<color=#606c78>아직 (이야기를 더 진행하세요)</color>";
                string tag = q.Type == QuestType.Guide ? "<color=#e6a8ff>[가이드]</color>" : "[일반]";
                Cell(rows.Next(24, 1), $"{tag} <color=#8090a0>Lv.{q.Lv}</color> {q.Title}  <size=13>{state}</size>", 10, 900, 16, done ? UIFactory.Good : UIFactory.TextMain);
            }
            rows.Next(6);
            Cell(rows.Next(26), $"지금까지 완료한 퀘스트 {p.Data.quest.completedTotal}개 · 그중 일일반복 {p.Data.dailyCompleted}개", 4, 900, 15, UIFactory.TextDim);
        }

        void Trophy(RowCursor rows)
        {
            var p = Game.Profile;
            Cell(rows.Next(30), $"대물 도전  <size=14><color=#8090a0>어종마다 대물 기준을 처음 넘기면 소라고동 3개 + 경험치 · 달성 {p.Data.trophyDone.Count}종</color></size>",
                4, 900, 20, UIFactory.Accent);
            // 여러 바다에 사는 어종은 처음 나오는 낚시터에만 적는다
            var shown = new System.Collections.Generic.HashSet<string>();
            foreach (var map in Fishing.World.Maps.All)
            {
                var list = FishDatabase.DexOf(map.Id).Where(s => shown.Add(s.Id)).ToList();
                if (list.Count == 0) continue;
                Cell(rows.Next(26), map.ShortName, 4, 300, 17, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
                for (int i = 0; i < list.Count; i += 3)
                {
                    var row = rows.Next(24, 1);
                    for (int k = 0; k < 3 && i + k < list.Count; k++)
                    {
                        var s = list[i + k];
                        bool done = p.Data.trophyDone.Contains(s.Id);
                        var best = p.Dex(s.Id)?.maxLength ?? 0f;
                        Cell(row, $"{(done ? "●" : "○")} {s.Name} {s.TrophyLen:0.#}cm  <size=12><color=#8090a0>최고 {best:0.0}</color></size>",
                            12 + k * 300, 296, 15, done ? UIFactory.Good : UIFactory.TextDim);
                    }
                }
            }
        }

        void Tournament(RowCursor rows)
        {
            var t = Game.Tournament;
            var s = Game.Profile.Data.tournament;
            int day = Game.Clock.Day;
            Cell(rows.Next(30), $"조합 낚시 대회  <size=14><color=#8090a0>매일 {TournamentRules.TimeText} · 한강 → 서천 → 홍현, 이어서 갈 수 있는 해안 낚시터 지역 대회 · AI 낚시꾼 7명과 겨룬다</color></size>",
                4, 920, 20, UIFactory.Accent);

            // 오늘 대회 상태
            var head = rows.Next(46, 6);
            UIFactory.RowBg(head, true);
            string phase = t.Phase switch
            {
                TournamentRules.Phase.Before => "<color=#8090a0>참가 신청 전 (08:00부터)</color>",
                TournamentRules.Phase.Registration => "<color=#70e070>참가 신청 중</color>",
                TournamentRules.Phase.Running => $"<color=#ffd060>진행 중 · 남은 시간 {Mathf.FloorToInt(t.MinutesLeft / 60f)}시간 {Mathf.FloorToInt(t.MinutesLeft % 60f):00}분</color>",
                _ => "<color=#8090a0>오늘 대회 끝</color>",
            };
            Cell(head, $"<b>{TournamentRules.Title(day, t.MapId)}</b>   {phase}" + (t.Joined ? "   <color=#7fd8ff>· 참가 중</color>" : ""), 12, 660, 19);
            var why = t.CannotJoinReason();
            if (why == null) RowButton(head, "참가 신청", () => { t.Join(); Refresh(); }, 700, 190, UIFactory.BtnGood, true, 18);
            else if (!t.Joined) Cell(head, why, 660, 250, 14, UIFactory.TextDim, TextAnchor.MiddleRight);

            Cell(rows.Next(26), "미션: ① 가장 많이 낚기  ② 가장 긴 고기  ③ 길이 합계 — 미션마다 1위 10점 · 2위 6점 · 3위 4점 · 4위 2점 · 그 아래 1점, 합계로 순위",
                4, 920, 15, UIFactory.TextDim);
            Cell(rows.Next(26), "보상: 1위 8,000골드 · 소라고동 5 · <color=#ffd060>거거 낚시대</color>   2위 5,000골드 · 소라고동 3 · <color=#ffd060>나팔고둥 릴</color>   3위 3,000골드 · 소라고동 2",
                4, 920, 15, UIFactory.Gold);

            // 엠블럼
            var em = rows.Next(40, 8);
            int toNext = TournamentRules.ToNextEmblem(s.points);
            Cell(em, $"계급 엠블럼 <size=24>{TournamentRules.EmblemColored(s.points)}</size>   누적 {s.points}점" +
                     (toNext > 0 ? $"  <size=14><color=#8090a0>다음 단계까지 {toNext}점</color></size>" : ""), 4, 560, 19);
            Cell(em, $"참가 {s.played}회 · 우승 {s.wins}회 · 입상 {s.podiums}회 · 최고 {(s.bestRank > 0 ? s.bestRank + "위" : "-")}", 560, 360, 16, UIFactory.TextMain, TextAnchor.MiddleRight);

            // 순위표
            bool show = t.Joined || t.Phase == TournamentRules.Phase.Running || t.Phase == TournamentRules.Phase.Finished;
            if (!show)
            {
                Cell(rows.Next(30), "대회가 시작되면 여기에 순위표가 나옵니다.", 4, 900, 16, UIFactory.TextDim);
                return;
            }
            var entries = t.Entries();
            UIFactory.SectionTitle(rows.Next(30, 4), t.Joined && s.settled ? "최종 순위" : "현재 순위", 4, 18);
            var hdr = rows.Next(22, 2);
            string[] cols = { "순위", "이름", "마릿수", "최장", "가장 긴 고기", "길이 합계", "점수" };
            float[] xs = { 12, 80, 330, 430, 540, 700, 820 };
            for (int i = 0; i < cols.Length; i++) Cell(hdr, cols[i], xs[i], 120, 14, UIFactory.TextDim);
            foreach (var e in entries)
            {
                var row = rows.Next(30, 2);
                UIFactory.RowBg(row, e.IsPlayer);
                var col = e.IsPlayer ? UIFactory.Gold : e.Rank <= 3 ? UIFactory.TextMain : UIFactory.TextDim;
                string medal = e.Rank switch { 1 => "<color=#ffd060>1</color>", 2 => "<color=#d0d8e0>2</color>", 3 => "<color=#d09060>3</color>", _ => e.Rank.ToString() };
                Cell(row, medal, xs[0] + 8, 60, 18, col, TextAnchor.MiddleLeft, FontStyle.Bold);
                Cell(row, e.Name, xs[1], 240, 17, col);
                Cell(row, $"{e.Count}", xs[2], 90, 17, col);
                Cell(row, e.Longest > 0 ? $"{e.Longest:0.0}cm" : "-", xs[3], 100, 17, col);
                Cell(row, e.LongestFish ?? "-", xs[4], 150, 16, col);
                Cell(row, $"{e.TotalLength:0.0}cm", xs[5], 110, 17, col);
                Cell(row, $"{e.Points}", xs[6], 80, 18, col, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
        }

        void Achievements(RowCursor rows)
        {
            var p = Game.Profile;
            var head = rows.Next(30, 6);
            UIFactory.SectionTitle(head, "업적", 4, 18);
            ProgressBar(head, AchievementDatabase.All.Count(a => p.Data.achievements.Contains(a.Id)), AchievementDatabase.All.Count, 580, 300);
            var titleRow = rows.Next(40, 6);
            Cell(titleRow, $"지금 칭호: <color=#7fd8ff>「{p.CurrentTitle}」</color>", 4, 360, 20);
            var titles = p.Titles;
            for (int i = 0; i < titles.Count && i < 5; i++)
            {
                string t = titles[titles.Count - 1 - i];
                RowButton(titleRow, t, () => p.SetTitle(t), 370 + i * 112, 108, t == p.CurrentTitle ? UIFactory.TabOn : UIFactory.TabOff, true, 13);
            }
            foreach (var a in AchievementDatabase.All)
            {
                bool done = p.Data.achievements.Contains(a.Id);
                string prog = !done && a.Progress != null ? $"  ({a.Progress(p).Item1}/{a.Progress(p).Item2})" : "";
                Cell(rows.Next(24, 1), $"{(done ? "●" : "○")} <b>{a.Name}</b>  <size=13><color=#8090a0>{a.Desc}{prog}</color></size>" +
                                      $"  <size=13><color=#ffd060>소라고동 {a.Conch}</color>{(a.Title != null ? $" · 칭호 「{a.Title}」" : "")}</size>",
                    10, 920, 16, done ? UIFactory.Good : UIFactory.TextMain);
            }
        }
    }

    public static class TournamentView
    {
        /// <summary>순위표 한 줄 문자열 (HUD·퀘스트 창 공용).</summary>
        public static string Line(TournamentEntry e) =>
            $"{e.Rank}. {e.Name}  {e.Count}마리 · {e.Longest:0.0}cm · {e.Points}점";
    }

    public class HelpPanel : UIPanel
    {
        public HelpPanel(Transform canvas)
        {
            BuildFrame(canvas, "HelpPanel", UIFactory.Caption("청풍명월 솔로 · 도움말", "HELP"), new Vector2(1180, 800));
            var text = UIFactory.Text(Body,
                "<b><color=#55dcc8>필드</color></b>\n" +
                "  WASD 이동 · Shift 달리기 · Space 점프 · B 배 꺼내기/내리기 · 오른쪽 버튼 드래그 시점 회전 · 휠 줌 · 숫자 1~6 미끼 선택\n" +
                "  F NPC와 대화 · L 퀘스트 · P 도감 · Z 살림망 · I 가방 · C 내 캐릭터(장비·능력치·의상) · M 지역 지도 · F12 상점 · U 설명 켜기/끄기 · F1 도움말 · F5 저장 · O 옵션(키 바꾸기) · Esc 창 닫기\n\n" +
                "<b><color=#55dcc8>캐스팅</color></b>\n" +
                "  물가에서 <b>던지고 싶은 물을 클릭</b>하면 그쪽을 보고 낚싯대를 꺼내 듭니다 (다른 물을 클릭하면 방향을 바꿉니다).\n" +
                "  얼음 위에서는 아무 데나 클릭해 낚싯대를 꺼내 든 뒤, 누른 채 살짝 내리면 발 앞에 구멍을 뚫고 채비를 내립니다.\n" +
                "  왼쪽 버튼을 누른 채 마우스를 <b>아래로 내리면</b> 낚싯대가 올라가고, 그대로 <b>위로 쭉 올리면</b> 낚싯대가 내려오며 던집니다.\n" +
                "  빠르게 휘두를수록 멀리 날아갑니다. 5m가 안 되면 캐스팅 실패! 화면의 큰 숫자가 비거리입니다.\n" +
                "  던진 뒤에는 마우스로 낚싯대를 상하좌우로 움직여 줄을 늦추거나 당기고, 왼쪽 버튼을 꾹 누르면 릴을 감습니다.\n\n" +
                "<b><color=#55dcc8>챔질</color></b>\n" +
                "  찌가 살짝 깔짝이는 건 예신(가짜 입질)입니다. 이때 챔질하면 헛챔질!\n" +
                "  <color=#ff6050>!</color> 가 뜨고 찌가 빨려 들어가면 <b>왼쪽 버튼을 누른 채 좌우로 흔드세요</b>.\n" +
                "  찌가 많이 잠겼을수록 좋습니다: BAD · SO SO · GOOD · GREAT · <color=#ffd060>PERFECT</color>(완전히 잠겼을 때, 카운터 1개 보너스)\n\n" +
                "<b><color=#55dcc8>파이팅</color></b>\n" +
                "  왼쪽 버튼 = 줄 감기 · 오른쪽 버튼 = 줄 풀기 · 마우스 좌우 = 낚싯대 방향\n" +
                "  물고기가 도망가는 <b>반대 방향</b>으로 당기세요. 장력을 <color=#ffaa40>HIGH</color> 쪽에 두면 데미지가 크지만 빨강까지 가면 끊어집니다.\n" +
                "  마우스를 물고기 반대쪽으로 <b>계속 끌면</b> 장력이 쌓여 한계를 넘으면 줄이 끊어집니다. <b>물고기 쪽으로 끌면</b> 줄이 느슨해져요.\n" +
                "  <b>카운터</b>: 물고기가 질주하거나 한쪽에 몰리면 파란 화살표가 뜹니다. 그 방향으로 마우스를 빠르게 휙!\n" +
                "  체력이 높은 물고기를 5m 안으로 당기면 놀라서 도망갑니다. 먼저 HP를 깎으세요.\n" +
                "  HP가 20% 이하인 물고기를 5m 안으로 끌어오면 <color=#70e070>랜딩 찬스</color>! SPACE 또는 왼쪽 버튼 연타로 0m까지!\n\n" +
                "<b><color=#55dcc8>성장</color></b>\n" +
                "  체력(HP)은 시간과 하이텐션으로 줄어듭니다. 30 미만이면 힘이 빠집니다. 요리나 김밥으로 회복하세요.\n" +
                "  어종마다 좋아하는 서식지·미끼·시간·날씨가 있고, 도감에 많이 기록할수록 정보가 열립니다.\n" +
                "  미니맵의 <color=#ffbf4d>◆</color> 는 어군입니다. 어군이 있는 칸에 던지면 입질이 빠르고 대물 확률이 오릅니다.\n" +
                "  큰 어군에서 3마리를 낚으면 작은 어군으로 흩어집니다(6시간 뒤 다시 모임). 20cm 미만 작은 고기는 살림망에서 생미끼로 만들 수 있어요.",
                17, UIFactory.TextMain, TextAnchor.UpperLeft);
            text.rectTransform.Fill();
            text.lineSpacing = 15f;
        }

        public override void Refresh() { }
    }
}
