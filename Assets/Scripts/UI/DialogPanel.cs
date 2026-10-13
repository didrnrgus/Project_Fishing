using System;
using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// NPC 대화창. 가까이 가서 F로 말을 건다.
    ///  - shop (낚시점 김사장): 물건 사고팔기, 낚싯대 강화, 오늘 조황
    ///  - union (조합 연락관 박주임): 퀘스트 확인, 목표를 채웠으면 보고하고 보상 받기
    ///  - angler (둔치·뗏목 낚시꾼): 실제 어군 위치를 알려 주는 조황 정보, 한마디
    /// </summary>
    public class DialogPanel : UIPanel
    {
        public NpcAngler Npc { get; private set; }

        Text _speaker, _line;
        RectTransform _options;

        public DialogPanel(Transform canvas)
        {
            BuildFrame(canvas, "DialogPanel", "", new Vector2(980, 330));
            Frame.anchoredPosition = new Vector2(0, -250);
            Title.gameObject.SetActive(false);
            _speaker = UIFactory.Text(Frame, "", 24, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _speaker.rectTransform.At(new Vector2(0, 1), new Vector2(24, -16), new Vector2(700, 34));
            _line = UIFactory.Text(Body, "", 21, UIFactory.TextMain, TextAnchor.UpperLeft);
            _line.rectTransform.At(new Vector2(0, 1), new Vector2(0, 0), new Vector2(560, 220), new Vector2(0, 1));
            _line.lineSpacing = 1.2f;
            _options = UIFactory.Node("Options", Body).At(new Vector2(1, 1), Vector2.zero, new Vector2(340, 230), new Vector2(1, 1));
        }

        public void Open(NpcAngler npc)
        {
            Npc = npc;
            npc.FaceTarget(Game.Player.transform);
            Open();
        }

        public override void Close()
        {
            if (Npc != null) Npc.FaceTarget(null);
            Npc = null;
            base.Close();
        }

        public override void Refresh()
        {
            if (Npc == null) return;
            switch (Npc.NpcId)
            {
                case "shop": ShopGreeting(); break;
                case "union": UnionGreeting(); break;
                default: AnglerGreeting(); break;
            }
        }

        void Show(string line, params (string label, Action action)[] options)
        {
            _speaker.text = $"{Npc.Look.Name}  <size=16><color=#7fd8ff>{Npc.Look.Title}</color></size>";
            _line.text = line;
            UIFactory.ClearChildren(_options);
            for (int i = 0; i < options.Length; i++)
            {
                var (label, action) = options[i];
                var b = UIFactory.Button(_options, label, () => action(), i == options.Length - 1 ? new Color(0.25f, 0.27f, 0.3f) : UIFactory.ButtonBg, 19);
                ((RectTransform)b.transform).At(new Vector2(0, 1), new Vector2(0, -i * 54f), new Vector2(340, 48), new Vector2(0, 1));
            }
        }

        (string, Action) Bye => ("대화 끝내기", Close);

        // ---------------- 김사장 ----------------

        void ShopGreeting()
        {
            var p = Game.Profile;
            int lv = p.RodLevel(p.Data.rodId);
            string greet = Game.Clock.Band switch
            {
                TimeBand.Dawn => "새벽부터 나왔구먼! 물안개 낄 때가 붕어 입질이 제일 좋지.",
                TimeBand.Night => "이 밤에도 낚시를? 메기랑 장어는 밤에 잘 무니까 지렁이 넉넉히 챙겨 가.",
                _ => "어서 와요, 반포 낚시점이요. 미끼, 장비, 수리, 다 돼."
            };
            if (Game.Weather.Current == Weather.Rain) greet += "\n비 오는 날엔 붕어가 얕은 데로 붙는다고.";
            if (lv > 0) greet += $"\n그 낚싯대, +{lv}까지 올렸네? 손맛이 다르지.";
            Show(greet,
                ("물건 사고팔기", () => { Close(); Game.UI.OpenExclusive(Game.UI.Shop); }),
                ("낚싯대 강화", () => { Close(); Game.UI.OpenExclusive(Game.UI.Upgrade); }),
                ("오늘 조황 묻기", () => Show(Report(), ("다른 이야기", ShopGreeting), Bye)),
                Bye);
        }

        // ---------------- 박주임 (조합) ----------------

        void UnionGreeting()
        {
            var quests = Game.Quests;
            var q = quests.Current;
            if (q == null)
            {
                Show("조합의 이야기 퀘스트를 모두 마치셨습니다. 이제 한강의 진짜 강태공이시네요!", ("퀘스트 기록 보기", OpenQuest), Bye);
                return;
            }
            if (quests.Ready)
            {
                Show($"오, [{q.Title}] 목표를 채우셨군요! 조합에 보고하겠습니다.\n보상: {q.RewardText}",
                    ("보고하고 보상 받기", Claim), Bye);
                return;
            }
            Show($"한국 낚시 조합 연락관 박주임입니다.\n지금 맡으신 일은 [{q.Title}] — {q.GoalText} ({Mathf.Min(quests.Progress, q.Count)}/{q.Count}).\n\"{q.Desc}\"",
                ("퀘스트 자세히 보기", OpenQuest),
                ("조황 정보", () => Show(Report(), ("다른 이야기", UnionGreeting), Bye)),
                Bye);
        }

        void OpenQuest()
        {
            var npc = Npc;
            base.Close();
            Game.UI.OpenExclusive(Game.UI.Quest);
            Npc = npc; // 퀘스트 창에서 보고할 수 있게 대화 상대를 기억한다
        }

        void Claim()
        {
            var q = Game.Quests.Current;
            Game.Quests.Claim();
            var next = Game.Quests.Current;
            Show($"수고하셨습니다! [{q.Title}] 보상을 드렸습니다. ({q.RewardText})" +
                 (next != null ? $"\n\n다음 부탁은 [{next.Title}] — {next.GoalText} 입니다." : ""),
                ("다른 이야기", UnionGreeting), Bye);
        }

        // ---------------- 낚시꾼 ----------------

        void AnglerGreeting()
        {
            string[] lines =
            {
                "쉿, 찌 보고 있어요. ...깔짝거리는 건 예신이니까 참아야 돼요.",
                "줄이 팽팽할 때 반대로만 당기면 끊어져요. 물고기 쪽으로 살살 늦춰 줘야지.",
                "배 있으면 강 건너 깊은 데로 나가 봐요. 거기 대물이 숨어 있다니까.",
                "큰 어군에서 몇 마리 낚으면 금방 흩어지더라고요. 다른 어군 찾아다니는 게 요령이에요.",
                "작은 피라미는 살림망에서 생미끼로 만들어 써요. 쏘가리가 환장하거든.",
            };
            var line = lines[Mathf.Abs(Npc.Look.Name.GetHashCode() + Game.Clock.Day) % lines.Length];
            Show(line, ("요즘 어디서 잘 나와요?", () => Show(Report(), ("다른 이야기", AnglerGreeting), Bye)), Bye);
        }

        /// <summary>지금 시간대에 활발한 어군과 그 위치, 맞는 미끼를 실제 게임 상태에서 뽑아 말해 준다.</summary>
        static string Report()
        {
            var band = Game.Clock.Band;
            var lines = new List<string>();
            foreach (var s in Game.Schools.Schools)
            {
                bool active = Array.IndexOf(s.PreferredTimes, band) >= 0;
                var lead = FishDatabase.Get(s.SpeciesIds[0]);
                string bait = lead != null && lead.Baits.Length > 0 ? Names.Of(lead.Baits[0]) : "지렁이";
                string cell = HanRiverMap.CellName(s.Col, s.Row);
                if (active)
                    lines.Add($"· {s.Name}가 {cell} 쪽에 {(s.Big ? "크게" : "조금")} 모여 있어. {bait}가 잘 먹혀.");
            }
            if (lines.Count == 0) return $"지금은 다들 깊은 데 가라앉았어. {Names.Of(band)}엔 입질이 뜸하니 자리 잘 골라 봐.";
            if (Game.Profile.Boat == null && lines.Count > 0) lines.Add("(3~4행은 둔치에서 안 닿아. 배를 사면 갈 수 있지.)");
            return string.Join("\n", lines);
        }
    }
}
