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
            BuildFrame(canvas, "HelpPanel", "청풍명월 솔로 · 도움말", new Vector2(980, 760));
            var text = UIFactory.Text(Body,
                "<b><color=#55dcc8>필드</color></b>\n" +
                "  WASD 이동 · Shift 달리기 · 마우스 오른쪽 드래그 시점 회전 · 휠 줌\n" +
                "  숫자 1~6 미끼 선택 · I 살림망 · B 도감 · Q 퀘스트 · P 낚시점(근처에서) · F5 저장 · Esc 창 닫기\n\n" +
                "<b><color=#55dcc8>캐스팅 → 입질 → 챔질</color></b>\n" +
                "  좌클릭을 누르고 있으면 거리 게이지가 왕복합니다. 놓으면 그 거리로 찌를 던집니다.\n" +
                "  찌가 살짝 깔짝이는 건 예신(가짜 입질)입니다. 이때 당기면 헛챔질!\n" +
                "  찌가 쭉 빨려 들어가고 <color=#ffd060>!</color> 가 뜨면 바로 좌클릭으로 챔질하세요. 빠를수록 바늘이 단단히 박힙니다.\n" +
                "  <color=#ff7090>♥ 두근두근 ♥</color> 이 뜨면 대물이나 희귀어입니다.\n\n" +
                "<b><color=#55dcc8>파이팅 (1인칭)</color></b>\n" +
                "  좌클릭 = 줄 감기 (장력↑ 거리↓) · 우클릭 = 줄 풀기 (장력↓ 거리↑)\n" +
                "  마우스 좌우 = 낚싯대 방향. 물고기 반대쪽으로 버티면 데미지가 커집니다.\n" +
                "  <b>카운터</b>: 물고기가 화면 끝까지 달렸을 때 반대 방향으로 마우스를 휙! 큰 데미지 + 콤보.\n" +
                "  장력 바: 파랑(약함) · <color=#ffaa40>주황=하이텐션(데미지 큼, 체력 소모 큼)</color> · <color=#ff5040>빨강=곧 끊어짐</color>\n" +
                "  왼쪽 위 링은 바늘 걸림 정도입니다. 줄을 너무 느슨하게 두면 바늘이 빠집니다.\n" +
                "  물고기 HP를 20% 이하로 깎고 5m 안으로 끌어오면 <color=#70e070>[Space] 랜딩</color>!\n\n" +
                "<b><color=#55dcc8>성장</color></b>\n" +
                "  체력은 시간과 하이텐션 파이팅으로 줄어듭니다. 30 미만이면 지쳐서 힘이 빠집니다.\n" +
                "  잡은 물고기를 요리하거나 김밥을 먹어 회복하세요. 레벨업하면 체력이 가득 찹니다.\n" +
                "  어종마다 좋아하는 서식지·미끼·시간·날씨가 있습니다. 도감에 많이 기록할수록 정보가 열립니다.\n" +
                "  미니맵의 <color=#ffbf4d>◆</color> 는 어군입니다. 어군이 있는 칸에 던지면 입질이 빠르고 대물 확률이 오릅니다.",
                18, UIFactory.TextMain, TextAnchor.UpperLeft);
            text.rectTransform.Fill();
            text.lineSpacing = 1.15f;
        }

        public override void Refresh() { }
    }
}
