using System.Linq;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>조합 퀘스트 창: 현재 퀘스트와 완료한 퀘스트 목록.</summary>
    /// <summary>
    /// 퀘스트(L). 원작처럼 여러 종류: 이야기(스토리) / 일일 의뢰 / 대물 도전 / 업적·칭호.
    /// 이야기 퀘스트는 조합 연락관이나 대기실에서 보고해야 보상을 받는다.
    /// </summary>
    public class QuestPanel : UIPanel
    {
        int _tab;

        public QuestPanel(Transform canvas)
        {
            BuildFrame(canvas, "QuestPanel", "조합 퀘스트", new Vector2(980, 880));
        }

        public void ShowTab(int tab)
        {
            _tab = tab;
            if (IsOpen) Refresh();
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);
            var tabs = rows.Next(42, 10);
            string[] names = { "이야기", "일일 의뢰", "대물 도전", "업적 · 칭호", "대회" };
            for (int i = 0; i < names.Length; i++)
            {
                int t = i;
                var tb = RowButton(tabs, names[i], () => { _tab = t; Refresh(); }, i * 184f, 176,
                    _tab == i ? UIFactory.TabOn : UIFactory.TabOff, true, 18);
                UIFactory.SetRadius(tb.targetGraphic, UIFactory.Pill);
            }
            Title.text = $"조합 퀘스트   <size=18><color=#ffd060>소라고동 {Game.Profile.Count(AchievementDatabase.ConchId)}개</color></size>";
            switch (_tab)
            {
                case 0: Story(rows); break;
                case 1: Daily(rows); break;
                case 2: Trophy(rows); break;
                case 3: Achievements(rows); break;
                default: Tournament(rows); break;
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

        void Story(RowCursor rows)
        {
            var quests = Game.Quests;
            var q = quests.Current;
            Cell(rows.Next(24), QuestDatabase.Giver, 4, 600, 16, UIFactory.Accent);
            if (q != null)
            {
                Cell(rows.Next(40), q.Title, 4, 900, 28, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
                var desc = Cell(rows.Next(60), $"\"{q.Desc}\"", 4, 900, 18, UIFactory.TextDim, TextAnchor.UpperLeft);
                desc.fontStyle = FontStyles.Italic;
                var goal = rows.Next(34);
                Cell(goal, $"목표: {q.GoalText}", 4, 560, 20);
                ProgressBar(goal, Mathf.Min(quests.Progress, q.Count), q.Count, 580, 300);
                Cell(rows.Next(30), $"보상: {q.RewardText}", 4, 900, 20, UIFactory.Gold);
                if (quests.Ready)
                {
                    // 대기실(조합)에서는 바로, 필드에서는 조합 연락관과 대화 중일 때 보고할 수 있다
                    bool canReport = Game.Mode == GameMode.Lobby || (Game.UI.Dialog.Npc != null && Game.UI.Dialog.Npc.NpcId == "union");
                    var row = rows.Next(52, 8);
                    if (canReport) RowButton(row, "목표 달성! 보고하고 보상 받기", () => { quests.Claim(); Refresh(); }, 4, 420, UIFactory.BtnGood, true, 20);
                    else Cell(row, $"목표 달성! {Fishing.World.FieldMap.UnionContact}이나 한국 낚시 조합(대기실)에서 보고하세요.", 4, 900, 17, UIFactory.Good);
                }
            }
            else Cell(rows.Next(40), "모든 이야기 퀘스트를 마쳤습니다!", 4, 900, 26, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

            rows.Next(10);
            var storyHead = rows.Next(28);
            UIFactory.SectionTitle(storyHead, "이야기", 4, 18);
            ProgressBar(storyHead, quests.CompletedCount, QuestDatabase.Story.Count, 580, 300);
            for (int i = 0; i < QuestDatabase.Story.Count; i++)
            {
                var def = QuestDatabase.Story[i];
                bool done = i < quests.CompletedCount;
                bool current = i == quests.CompletedCount;
                string mark = done ? "<color=#70e070>●</color>" : current ? "▶" : "○";
                var color = done ? UIFactory.Good : current ? UIFactory.TextMain : UIFactory.TextDim;
                Cell(rows.Next(25, 1), $"{mark}  {def.Title}" + (done || current ? $"  <size=14><color=#8090a0>{def.GoalText}</color></size>" : ""),
                    10, 900, 17, color);
            }
        }

        void Daily(RowCursor rows)
        {
            Cell(rows.Next(30), "오늘의 조합 의뢰  <size=14><color=#8090a0>게임 하루마다 새로 3개 · 가 본 낚시터에서 · 완료하면 바로 보상 + 소라고동</color></size>",
                4, 900, 20, UIFactory.Accent);
            foreach (var q in Game.Quests.Daily)
            {
                var row = rows.Next(56, 6);
                UIFactory.RowBg(row, !q.done);
                Cell(row, $"{(q.done ? "● " : "")}<b>{QuestSystem.DailyText(q)}</b>\n<size=14><color=#ffd060>{q.gold:N0}골드 · 경험치 {q.exp} · 소라고동 1</color></size>",
                    12, 640, 19, q.done ? UIFactory.Good : UIFactory.TextMain);
                if (q.done) UIFactory.Chip(row, "완료", UIFactory.BtnGood, UIFactory.TextMain, 15).transform.parent.GetComponent<RectTransform>()
                    .At(new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(90, 30), new Vector2(1, 0.5f));
                else ProgressBar(row, q.progress, q.count, 640, 230);
            }
            Cell(rows.Next(30), $"지금까지 완료한 의뢰 {Game.Profile.Data.dailyCompleted}개", 4, 900, 16, UIFactory.TextDim);
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
            Cell(rows.Next(30), $"조합 낚시 대회  <size=14><color=#8090a0>매일 {TournamentRules.TimeText} · 한강 → 서천 앞바다 → 홍현 앞바다 차례로 · AI 낚시꾼 7명과 겨룬다</color></size>",
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
            Cell(head, $"<b>{TournamentRules.Title(day)}</b>   {phase}" + (t.Joined ? "   <color=#7fd8ff>· 참가 중</color>" : ""), 12, 660, 19);
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
            BuildFrame(canvas, "HelpPanel", "청풍명월 솔로 · 도움말", new Vector2(1180, 800));
            var text = UIFactory.Text(Body,
                "<b><color=#55dcc8>필드</color></b>\n" +
                "  WASD 이동 · Shift 달리기 · Space 점프 · B 배 꺼내기/내리기 · 오른쪽 버튼 드래그 시점 회전 · 휠 줌 · 숫자 1~6 미끼 선택\n" +
                "  F NPC와 대화 · L 퀘스트 · P 도감 · Z 살림망 · I 가방 · C 의상 · M 지역 지도 · F12 상점 · U 설명 켜기/끄기 · F1 도움말 · F5 저장 · O 옵션(키 바꾸기) · Esc 창 닫기\n\n" +
                "<b><color=#55dcc8>캐스팅</color></b>\n" +
                "  왼쪽 버튼을 누르고 마우스를 <b>아래로 당긴 다음</b>, <b>빠르게 위로 밀면서</b> 버튼을 놓으세요.\n" +
                "  많이 당기고 빨리 밀수록 멀리 날아갑니다. 화면의 큰 숫자가 비거리입니다.\n\n" +
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
