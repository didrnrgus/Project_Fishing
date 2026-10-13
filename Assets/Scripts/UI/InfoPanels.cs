using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>조합 퀘스트 창: 현재 퀘스트와 완료한 퀘스트 목록.</summary>
    public class QuestPanel : UIPanel
    {
        public QuestPanel(Transform canvas)
        {
            BuildFrame(canvas, "QuestPanel", "조합 퀘스트", new Vector2(760, 640));
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);
            var quests = Game.Quests;
            var q = quests.Current;

            Cell(rows.Next(24), QuestDatabase.Giver, 4, 600, 16, UIFactory.Accent);
            if (q != null)
            {
                Cell(rows.Next(40), q.Title, 4, 700, 28, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
                var desc = Cell(rows.Next(70), $"\"{q.Desc}\"", 4, 700, 18, UIFactory.TextDim, TextAnchor.UpperLeft);
                desc.fontStyle = FontStyle.Italic;
                Cell(rows.Next(30), $"목표: {q.GoalText}   ({Mathf.Min(quests.Progress, q.Count)}/{q.Count})", 4, 700, 20);
                Cell(rows.Next(30), $"보상: {q.RewardText}", 4, 700, 20, UIFactory.Gold);
                if (quests.Ready)
                {
                    // 대기실(조합)에서는 바로, 필드에서는 박주임과 대화 중일 때 보고할 수 있다
                    bool canReport = Game.Mode == GameMode.Lobby || (Game.UI.Dialog.Npc != null && Game.UI.Dialog.Npc.NpcId == "union");
                    var row = rows.Next(52, 8);
                    if (canReport) RowButton(row, "목표 달성! 보고하고 보상 받기", () => { quests.Claim(); Refresh(); }, 4, 420, new Color(0.2f, 0.5f, 0.3f), true, 20);
                    else Cell(row, "목표 달성! 데크 입구의 조합 연락관 박주임이나 한국 낚시 조합(대기실)에서 보고하세요.", 4, 700, 17, UIFactory.Good);
                }
            }
            else
            {
                Cell(rows.Next(40), "모든 이야기 퀘스트를 마쳤습니다!", 4, 700, 26, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            rows.Next(16);
            Cell(rows.Next(26), $"완료한 퀘스트 ({quests.CompletedCount}/{QuestDatabase.Story.Count})", 4, 600, 18, UIFactory.Accent);
            for (int i = 0; i < QuestDatabase.Story.Count; i++)
            {
                var def = QuestDatabase.Story[i];
                bool done = i < quests.CompletedCount;
                bool current = i == quests.CompletedCount;
                string mark = done ? "✔" : current ? "▶" : "·";
                var color = done ? UIFactory.Good : current ? UIFactory.TextMain : UIFactory.TextDim;
                Cell(rows.Next(26, 2), $"{mark}  {def.Title}" + (done || current ? $"  <size=14><color=#8090a0>{def.GoalText}</color></size>" : ""),
                    10, 700, 18, color);
            }
        }
    }

    /// <summary>조작법 / 게임 규칙 요약.</summary>
    public class HelpPanel : UIPanel
    {
        public HelpPanel(Transform canvas)
        {
            BuildFrame(canvas, "HelpPanel", "청풍명월 솔로 · 도움말", new Vector2(1180, 800));
            var text = UIFactory.Text(Body,
                "<b><color=#55dcc8>필드</color></b>\n" +
                "  WASD 이동 · Shift 달리기 · Space 점프 · B 배 꺼내기/내리기 · 오른쪽 버튼 드래그 시점 회전 · 휠 줌 · 숫자 1~6 미끼 선택\n" +
                "  F NPC와 대화 · L 퀘스트 · P 도감 · Z 살림망 · M 지역 지도 · F12 상점 · U 설명 켜기/끄기 · F1 도움말 · F5 저장 · Esc 창 닫기\n\n" +
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
            text.lineSpacing = 1.15f;
        }

        public override void Refresh() { }
    }
}
