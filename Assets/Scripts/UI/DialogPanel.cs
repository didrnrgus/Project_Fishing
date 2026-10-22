using System;
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
    /// NPC 대화창. 가까이 가서 F로 말을 건다.
    ///  - shop (낚시점 김사장): 물건 사고팔기, 낚싯대 강화, 오늘 조황
    ///  - union (조합 연락관: 한강 박주임, 홍현 정계장): [퀘스트 목록보기](QuestTalkPanel), 낚시 대회, 소라고동 교환
    ///  - angler (둔치·뗏목 낚시꾼): 실제 어군 위치를 알려 주는 조황 정보, 한마디
    /// </summary>
    public class DialogPanel : UIPanel
    {
        public NpcAngler Npc { get; private set; }

        TMP_Text _speaker, _line;
        RectTransform _options;

        public DialogPanel(Transform canvas)
        {
            BuildFrame(canvas, "DialogPanel", "", new Vector2(980, 330));
            Frame.anchoredPosition = new Vector2(0, -250);
            Title.gameObject.SetActive(false);
            _speaker = UIFactory.Text(Frame, "", 24, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _speaker.rectTransform.At(new Vector2(0, 1), new Vector2(42, -18), new Vector2(700, 40));
            _line = UIFactory.Text(Body, "", 21, UIFactory.TextMain, TextAnchor.UpperLeft);
            _line.rectTransform.At(new Vector2(0, 1), new Vector2(0, 0), new Vector2(560, 220), new Vector2(0, 1));
            _line.lineSpacing = 20f;
            _options = UIFactory.Node("Options", Body).At(new Vector2(1, 1), Vector2.zero, new Vector2(340, 230), new Vector2(1, 1));
        }

        public void Open(NpcAngler npc)
        {
            Npc = npc;
            npc.FaceTarget(Game.Player.transform);
            Open();
            // 이 사람을 만나는 퀘스트는 말을 걸면 완료 (원작 "[퀘스트] 최아린을 만나기 1/1 [완 료]")
            Game.Quests.Talked(QuestKey);
        }

        /// <summary>이 NPC의 퀘스트 키 (조합 연락관·낚시점 주인·어부). 퀘스트 NPC가 아니면 null.</summary>
        string QuestKey => Npc == null ? null : QuestNpcs.KeyFor(Npc.NpcId, FieldMap.Id);

        /// <summary>원작 [퀘스트 목록보기]: 왼쪽 퀘스트 대화 카드로 넘어간다.</summary>
        void OpenQuestList()
        {
            var npc = Npc;
            base.Close();
            Npc = null;
            Game.UI.QuestTalk.OpenFor(npc);
        }

        /// <summary>퀘스트 NPC 인사 끝에 붙는 안내 (완료할 것 / 받을 것).</summary>
        string QuestHint()
        {
            var m = Game.Quests.MarkFor(QuestKey);
            return m == QuestSystem.Mark.Ready ? "\n<color=#70e070>목표를 채운 퀘스트가 있군요! [퀘스트 목록보기]에서 완료하세요.</color>"
                : m == QuestSystem.Mark.AvailableStory || m == QuestSystem.Mark.Available ? "\n<color=#ffd060>부탁드릴 일이 있어요. [퀘스트 목록보기]를 눌러 보세요.</color>"
                : "";
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
                case FishTraps.FisherId: FisherGreeting(); break;
                default: AnglerGreeting(); break;
            }
        }

        void Show(string line, params (string label, Action action)[] options)
        {
            _speaker.text = $"{Npc.Look.Name}  <size=16><color=#7fd8ff>{Npc.Look.Title}</color></size>";
            _line.text = line;
            UIFactory.ClearChildren(_options);
            // 선택지가 많으면(퀘스트 목록보기가 붙은 낚시점) 간격을 좁혀 창 안에 넣는다
            float step = Mathf.Min(46f, 236f / Mathf.Max(1, options.Length));
            for (int i = 0; i < options.Length; i++)
            {
                var (label, action) = options[i];
                var b = UIFactory.Button(_options, label, () => action(), i == options.Length - 1 ? UIFactory.BtnGhost : (i == 0 ? UIFactory.BtnPrimary : UIFactory.BtnNeutral), 19);
                UIFactory.SetRadius(b.targetGraphic, UIFactory.Pill);
                ((RectTransform)b.transform).At(new Vector2(0, 1), new Vector2(0, -i * step), new Vector2(340, step - 4f), new Vector2(0, 1));
            }
        }

        (string, Action) Bye => ("대화 끝내기", Close);

        // ---------------- 어부 (통발, 원작 0:42: 퀘스트 목록보기 / 미끼 조합하기 / 통발 설치하기) ----------------

        void FisherGreeting()
        {
            string where = FieldMap.IsSea ? "바다" : "물";
            string line = FishTraps.AnyReady
                ? "통발 걷을 때가 됐구먼! 뭐가 들었나 같이 보세."
                : $"이 {where}는 내 손바닥이지. 통발 하나 놓아 두면 {FishTraps.DurationHours:0}시간 뒤엔 고기가 그득하다네.\n수거할 때 수거비용만 내면 돼.";
            Show(line,
                ("퀘스트 목록보기", OpenQuestList),
                ("미끼 조합하기", () => Show("미끼 조합은 아직 연구 중이라네. 큰 고기를 해체하면 좋은 미끼가 나오니 그걸 먼저 써 보게.", ("다른 이야기", FisherGreeting), Bye)),
                ("통발 설치하기", () => { string name = Npc.Look.Name; Close(); Game.UI.Traps.OpenFrom(name); }),
                Bye);
        }

        // ---------------- 김사장 ----------------

        void ShopGreeting()
        {
            var p = Game.Profile;
            int lv = p.RodLevel(p.Data.rodId);
            string greet = Game.Clock.Band switch
            {
                TimeBand.Dawn => "새벽부터 나왔구먼! 물안개 낄 때가 붕어 입질이 제일 좋지.",
                TimeBand.Night => "이 밤에도 낚시를? 메기랑 장어는 밤에 잘 무니까 지렁이 넉넉히 챙겨 가.",
                _ => $"어서 와요, {Fishing.World.FieldMap.ShopName}이요. 미끼, 장비, 수리, 다 돼."
            };
            if (Game.Weather.Current == Weather.Rain) greet += "\n비 오는 날엔 붕어가 얕은 데로 붙는다고.";
            if (lv > 0) greet += $"\n그 낚싯대, +{lv}까지 올렸네? 손맛이 다르지.";
            greet += QuestHint();
            if (Game.Quests.HasQuestsAt(QuestKey))
            {
                Show(greet,
                    ("퀘스트 목록보기", OpenQuestList),
                    ("물건 사고팔기", () => { Close(); Game.UI.OpenExclusive(Game.UI.Shop); }),
                    ("장비 강화 (낚싯대·릴)", () => { Close(); Game.UI.OpenExclusive(Game.UI.Upgrade); }),
                    ("의상 보기", () => { Close(); Game.UI.Character.ShowTab(true); Game.UI.OpenExclusive(Game.UI.Character); }),
                    ("오늘 조황 묻기", () => Show(Report(), ("다른 이야기", ShopGreeting), Bye)),
                    Bye);
                return;
            }
            Show(greet,
                ("물건 사고팔기", () => { Close(); Game.UI.OpenExclusive(Game.UI.Shop); }),
                ("장비 강화 (낚싯대·릴)", () => { Close(); Game.UI.OpenExclusive(Game.UI.Upgrade); }),
                ("의상 보기", () => { Close(); Game.UI.Character.ShowTab(true); Game.UI.OpenExclusive(Game.UI.Character); }),
                ("오늘 조황 묻기", () => Show(Report(), ("다른 이야기", ShopGreeting), Bye)),
                Bye);
        }

        // ---------------- 조합 연락관 ----------------

        void UnionGreeting()
        {
            // 원작: 인사말 + [퀘스트 목록보기] → 수락가능한 퀘스트 / 진행중인 퀘스트
            string line = (QuestNpcs.Get(QuestKey)?.Greeting ?? $"한국 낚시 조합 {Npc.Look.Name}입니다.") + QuestHint();
            Show(line,
                ("퀘스트 목록보기", OpenQuestList),
                ("낚시 대회", Tournament),
                ("소라고동 교환", () => { Close(); Game.UI.OpenExclusive(Game.UI.Exchange); }),
                ("조황 정보", () => Show(Report(), ("다른 이야기", UnionGreeting), Bye)),
                Bye);
        }

        /// <summary>낚시 대회 안내와 참가 신청 (원작: 대회는 NPC를 통해 참가).</summary>
        void Tournament()
        {
            var t = Game.Tournament;
            var s = Game.Profile.Data.tournament;
            string head = $"오늘은 [{TournamentRules.Title(t.Day, t.MapId)}]가 {TournamentRules.TimeText}에 열립니다.\n" +
                          "가장 많이, 가장 길게, 길이 합계 — 세 미션 점수로 순위를 매기고, 1위는 거거 낚시대, 2위는 나팔고둥 릴을 드려요.\n" +
                          $"지금 계급은 {TournamentRules.EmblemColored(s.points)} (누적 {s.points}점)이시네요.";
            if (t.Joined && !s.settled)
            {
                Show(head + "\n<color=#70e070>참가 등록이 되어 있습니다. 행운을 빌어요!</color>", ("순위표 보기", OpenTournamentTab), ("다른 이야기", UnionGreeting), Bye);
                return;
            }
            var why = t.CannotJoinReason();
            if (why != null)
            {
                Show(head + $"\n<color=#ff9a50>{why}</color>", ("대회 기록 보기", OpenTournamentTab), ("다른 이야기", UnionGreeting), Bye);
                return;
            }
            Show(head, ("참가 신청하기", () => { if (t.Join()) Show("등록했습니다! 대회 시간에 대회장에서 낚은 고기만 기록돼요. 시간은 천천히 흐르니 침착하게!",
                    ("순위표 보기", OpenTournamentTab), Bye); }),
                ("대회 기록 보기", OpenTournamentTab), ("다른 이야기", UnionGreeting), Bye);
        }

        void OpenTournamentTab()
        {
            Close();
            Game.UI.Quest.ShowTab(4);
            Game.UI.OpenExclusive(Game.UI.Quest);
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
                if (s.Shadow) continue;
                bool active = Array.IndexOf(s.PreferredTimes, band) >= 0;
                var lead = FishDatabase.Get(s.SpeciesIds[0]);
                string bait = lead != null && lead.Baits.Length > 0 ? Names.Of(lead.Baits[0]) : "지렁이";
                string cell = FieldMap.CellName(s.Col, s.Row);
                if (active)
                    lines.Add($"· {s.Name}가 {cell} 쪽에 {(s.Big ? "크게" : "조금")} 모여 있어. {bait}가 잘 먹혀.");
            }
            // 물속 그림자 소문: 퀘스트 고기면 이름까지
            var shadow = Game.Schools.Schools.Find(x => x.Shadow && !x.Hidden);
            if (shadow != null)
            {
                string who = shadow.Quest ? $"{FishDatabase.Get(shadow.SpeciesIds[0]).Name} 같던데" : "뭔지는 모르겠는데 엄청 커";
                lines.Insert(0, $"· {FieldMap.CellName(shadow.Col, shadow.Row)} 쪽 물속에 시커먼 그림자가 어슬렁거려. {who}. 바로 앞에 던져 봐!");
            }
            if (lines.Count == 0) return $"지금은 다들 깊은 데 가라앉았어. {Names.Of(band)}엔 입질이 뜸하니 자리 잘 골라 봐.";
            if (Game.Profile.Boat == null && lines.Count > 0) lines.Add("(3~4행은 둔치에서 안 닿아. 배를 사면 갈 수 있지.)");
            return string.Join("\n", lines);
        }
    }
}
