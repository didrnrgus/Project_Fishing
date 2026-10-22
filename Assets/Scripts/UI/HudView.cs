using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 필드 HUD. 원작 플레이 영상의 배치를 따른다.
    ///  - 왼쪽 위: 초상화, 레벨/이름, HP·EXP 바, 장착 미끼
    ///  - 오른쪽 위: 채널/시계, MINIMAP(현재 위치), QUEST LIST
    ///  - 아래: 메뉴 바(퀘스트 L, 도감 P, 살림망 Z, 상점 F12 …)와 미끼 퀵슬롯
    ///  - 캐스팅: 큰 비거리 숫자, 마우스 안내 / 챔질: 찌 클로즈업 게이지(BAD~PERFECT)
    /// </summary>
    public class HudView
    {
        readonly RectTransform _root;

        TMP_Text _levelText, _staminaText, _expText, _goldText, _baitName, _baitCount;
        Image _staminaFill, _expFill;
        TMP_Text _channelText, _clockText, _locationText, _areaText;
        TMP_Text _questTitle, _questGoal;
        RectTransform _questBox;
        float _nextQuestList;
        // 대회 순위표 (참가했을 때만)
        RectTransform _tourBox;
        TMP_Text _tourTitle, _tourBody;
        TMP_Text _hint, _prompt;
        TMP_Text _netSlot;
        TMP_Text _nameText;
        // 배 계기판
        RectTransform _boatGauges;
        Image _speedFill, _fuelFill;
        TMP_Text _speedText, _fuelText, _boatHint;
        RectTransform _boatHintPill;
        readonly List<(Image bg, TMP_Text label, BaitDef bait)> _baitSlots = new List<(Image, TMP_Text, BaitDef)>();

        RectTransform _mapRoot;
        RectTransform _playerArrow, _bobberDot;
        readonly List<RectTransform> _schoolMarks = new List<RectTransform>();
        // 미니맵의 물속 그림자: 어종 아이콘을 검게 칠한 실루엣 + 빛나는 고리 + 퀘스트(!)·정체 모름(?) 표시
        RectTransform _shadowMark;
        Image _shadowHalo, _shadowIcon;
        TMP_Text _shadowTag;
        string _shadowSpecies;
        bool _shadowFacesRight;

        // 캐스팅
        TMP_Text _castDistance;
        float _castShownUntil;
        float _castShownValue;
        float _castLandedAt;
        RectTransform _castTip;
        TMP_Text _tipTitle, _tipText;
        // 챔질
        RectTransform _hookGauge;
        RectTransform _gaugeBobber;
        readonly TMP_Text[] _gradeLabels = new TMP_Text[5];
        RectTransform _shakeTip;
        TMP_Text _gradePopup;
        RectTransform _biteAlert;
        Image _biteMist, _gaugeGlow;

        readonly List<(RectTransform root, CanvasGroup group, float until)> _toasts = new List<(RectTransform, CanvasGroup, float)>();
        RectTransform _toastRoot;

        const float MapScale = 0.95f;
        const float GaugeHeight = 300f;

        public GameObject Root => _root.gameObject;

        /// <summary>토스트 묶음. GameUI가 창들보다 위로 옮겨 창이 열려 있어도 가려지지 않게 한다.</summary>
        public RectTransform ToastLayer => _toastRoot;

        public HudView(Transform canvas)
        {
            _root = UIFactory.Node("Hud", canvas).Fill();
            BuildPlayerPanel();
            BuildClockAndMap();
            BuildBottomBar();
            BuildCasting();
            BuildHookGauge();
            BuildCenter();
            BuildBoatGauges();
            BuildRetry();
            BuildAimLines();
            _toastRoot = UIFactory.Node("Toasts", _root).At(new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(1000, 200));
            Game.Toasted += Toast;
        }

        // ---------------- 빌드 ----------------

        void BuildPlayerPanel()
        {
            var panel = UIFactory.Panel(_root, "PlayerPanel", 20f).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, -12), new Vector2(436, 128));

            // 초상화: 내 캐릭터를 비추는 카메라의 렌더 텍스처 (PortraitCamera), 둥근 모서리
            var frame = UIFactory.Shape(panel, new Color(1f, 1f, 1f, 0.08f), 16f, "PortraitFrame", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(10, -10), new Vector2(108, 108));
            var portrait = UIFactory.RoundedTexture(frame, PortraitCamera.Texture, 14f, "Portrait");
            portrait.rectTransform.Fill(2, 2, 2, 2);
            UIFactory.AddBorder(portrait, new Color(0.55f, 0.9f, 0.95f, 0.35f), 1.5f);
            _levelText = UIFactory.Chip(frame, "", new Color(0.04f, 0.07f, 0.09f, 0.92f), UIFactory.Gold, 15, "LevelBadge");
            ((RectTransform)_levelText.transform.parent).At(new Vector2(0.5f, 0), new Vector2(0, -8), new Vector2(64, 24), new Vector2(0.5f, 0));

            var look = Fishing.Characters.CharacterLook.Player;
            _nameText = UIFactory.Text(panel, $"{look.Name}  <size=13><color=#7fd8ff>{look.Title}</color></size>", 20, UIFactory.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            _nameText.rectTransform.At(new Vector2(0, 1), new Vector2(132, -12), new Vector2(290, 26));

            UIFactory.Text(panel, "HP", 12, new Color(0.5f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(132, -44), new Vector2(40, 18));
            _staminaFill = UIFactory.Bar(panel, new Color(0f, 0f, 0f, 0.45f), new Color(0.4f, 0.8f, 1f), "HP");
            ((RectTransform)_staminaFill.transform.parent).At(new Vector2(0, 1), new Vector2(168, -48), new Vector2(184, 10));
            _staminaText = UIFactory.Text(panel, "", 13, UIFactory.TextMain, TextAnchor.MiddleRight);
            _staminaText.rectTransform.At(new Vector2(0, 1), new Vector2(356, -44), new Vector2(66, 18));

            UIFactory.Text(panel, "EXP", 12, new Color(0.55f, 0.95f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(132, -66), new Vector2(40, 18));
            _expFill = UIFactory.Bar(panel, new Color(0f, 0f, 0f, 0.45f), new Color(0.5f, 0.9f, 0.4f), "Exp");
            ((RectTransform)_expFill.transform.parent).At(new Vector2(0, 1), new Vector2(168, -70), new Vector2(184, 10));
            _expText = UIFactory.Text(panel, "", 13, UIFactory.TextMain, TextAnchor.MiddleRight);
            _expText.rectTransform.At(new Vector2(0, 1), new Vector2(356, -66), new Vector2(66, 18));

            UIFactory.Divider(panel).rectTransform.At(new Vector2(0, 1), new Vector2(132, -88), new Vector2(290, 1));
            _goldText = UIFactory.Text(panel, "", 14, UIFactory.Gold);
            _goldText.rectTransform.At(new Vector2(0, 1), new Vector2(132, -94), new Vector2(290, 24));

            // 장착 미끼 (원작: 초상화 아래 물고기 아이콘 칸)
            var bait = UIFactory.Panel(_root, "BaitBox", UIFactory.Pill, 0.55f).rectTransform
                .At(new Vector2(0, 1), new Vector2(14, -150), new Vector2(170, 38));
            UIFactory.Shape(bait, UIFactory.Accent, UIFactory.Pill, "Dot", 0f).rectTransform.At(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(8, 8));
            _baitName = UIFactory.Text(bait, "", 15, UIFactory.TextMain);
            _baitName.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            _baitName.enableAutoSizing = true;
            _baitName.fontSizeMin = 10f;
            _baitName.fontSizeMax = 15f;
            _baitName.rectTransform.At(new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(84, 30));
            _baitCount = UIFactory.Text(bait, "", 16, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            _baitCount.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(60, 30));
        }

        void BuildClockAndMap()
        {
            var clock = UIFactory.Panel(_root, "Clock").rectTransform
                .At(new Vector2(1, 1), new Vector2(-14, -12), new Vector2(344, 68));
            var exit = UIFactory.Button(clock, "◀ 조합", () => Game.UI.EnterLobby(), UIFactory.BtnDanger, 13);
            UIFactory.SetRadius(exit.targetGraphic, UIFactory.Pill);
            ((RectTransform)exit.transform).At(new Vector2(1, 1), new Vector2(-10, -10), new Vector2(70, 24));
            _channelText = UIFactory.Text(clock, "", 13, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _channelText.rectTransform.At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(250, 20));
            _clockText = UIFactory.Text(clock, "", 22, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _clockText.rectTransform.At(new Vector2(0, 1), new Vector2(16, -30), new Vector2(320, 30));

            var frame = UIFactory.Panel(_root, "Minimap").rectTransform
                .At(new Vector2(1, 1), new Vector2(-14, -88), new Vector2(344, 262));
            UIFactory.Text(frame, "MINIMAP", 12, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -6), new Vector2(120, 20));
            _locationText = UIFactory.Text(frame, "", 13, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleRight, FontStyle.Bold);
            _locationText.rectTransform.At(new Vector2(1, 1), new Vector2(-16, -6), new Vector2(200, 20));

            UIFactory.Shape(frame, new Color(0f, 0f, 0f, 0.35f), 10f, "MapWell", 0f).rectTransform
                .At(new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(324, 216));
            _mapRoot = UIFactory.Node("Map", frame).At(new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(320, 212));
            _mapRoot.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(6, 6);
            UIFactory.Text(frame, "[M] 지역 지도", 11, new Color(1, 1, 1, 0.6f), TextAnchor.LowerRight).rectTransform
                .At(new Vector2(1, 0), new Vector2(-14, 14), new Vector2(120, 16), new Vector2(1, 0));
            BuildMinimapContent();
            _bobberDot = UIFactory.Image(_mapRoot, new Color(1f, 0.3f, 0.2f), UIFactory.Circle, "Bobber").rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
            _playerArrow = UIFactory.Text(_mapRoot, "▲", 16, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, "Player").rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));

            // QUEST LIST
            var quest = UIFactory.Panel(_root, "QuestList").rectTransform
                .At(new Vector2(1, 1), new Vector2(-14, -358), new Vector2(344, 108));
            UIFactory.Text(quest, "QUEST LIST", 12, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -6), new Vector2(200, 20));
            _areaText = UIFactory.Text(quest, "", 12, UIFactory.TextDim, TextAnchor.MiddleRight);
            _areaText.rectTransform.At(new Vector2(1, 1), new Vector2(-16, -6), new Vector2(200, 20));
            _questBox = quest;
            // 원작: "[현재 낚시터]에서 가능한 퀘스트만 보기" 체크 + 번호 붙은 퀘스트 (제목 줄은 종류 색, 목표 줄은 오른쪽 정렬) 최대 6개
            var hereToggle = UIFactory.Button(quest, "", () => { var qp = Game.Profile.Data.quest; qp.hereOnly = !qp.hereOnly; _nextQuestList = 0f; },
                new Color(1f, 1f, 1f, 0f), 12);
            ((RectTransform)hereToggle.transform).At(new Vector2(0, 1), new Vector2(8, -26), new Vector2(328, 22), new Vector2(0, 1));
            _questTitle = hereToggle.GetComponentInChildren<TMP_Text>();
            _questTitle.alignment = TextAlignmentOptions.Left;
            _questTitle.color = new Color(0.55f, 0.85f, 1f);
            _questGoal = UIFactory.Text(quest, "", 14, UIFactory.TextMain, TextAnchor.UpperLeft);
            _questGoal.rectTransform.At(new Vector2(0, 1), new Vector2(16, -52), new Vector2(316, 300), new Vector2(0, 1));
            _questGoal.lineSpacing = 2f;

            // 대회 순위표: 참가한 날 퀘스트 목록 아래에 (원작 대회 HUD 자리는 확인되지 않아 솔로판 배치)
            _tourBox = UIFactory.Panel(_root, "Tournament").rectTransform
                .At(new Vector2(1, 1), new Vector2(-14, -474), new Vector2(344, 170));
            UIFactory.Text(_tourBox, "TOURNAMENT", 12, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -6), new Vector2(140, 20));
            _tourTitle = UIFactory.Text(_tourBox, "", 13, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            _tourTitle.rectTransform.At(new Vector2(1, 1), new Vector2(-16, -6), new Vector2(200, 20));
            _tourBody = UIFactory.Text(_tourBox, "", 14, UIFactory.TextMain, TextAnchor.UpperLeft);
            _tourBody.rectTransform.At(new Vector2(0, 1), new Vector2(16, -30), new Vector2(316, 136));
            _tourBody.lineSpacing = 4f;
        }

        /// <summary>
        /// 원작 QUEST LIST: "1. [13] [이야기] 강대표의 추측" (종류 색) + 오른쪽 정렬 목표 "나린 참돔 잡아서 가져오기 0 / 1".
        /// 목표를 채우면 "완 료"가 붙고 목표 줄이 "[홍현 앞바다]의 정계장에게 보고하기"로 바뀐다. 상자 높이는 줄 수에 맞추고, 대회 순위표는 그 아래로.
        /// </summary>
        void UpdateQuestList()
        {
            if (Time.unscaledTime < _nextQuestList) return;
            _nextQuestList = Time.unscaledTime + 0.25f;
            var quests = Game.Quests;
            bool hereOnly = Game.Profile.Data.quest.hereOnly;
            _questTitle.text = $"{(hereOnly ? "<color=#7fe0ff>●</color>" : "<color=#506070>○</color>")} [현재 낚시터]에서 가능한 퀘스트만 보기";
            var sb = new System.Text.StringBuilder();
            int n = 0;
            foreach (var q in quests.Active())
            {
                if (hereOnly && !quests.DoableOn(q, FieldMap.Id)) continue;
                if (++n > 6) break;
                bool ready = quests.IsReady(q);
                string tag = q.TypeTag.Length > 0 ? $"[{q.TypeTag}] " : "";
                string title = q.Title.Length > 15 ? q.Title.Substring(0, 14) + ".." : q.Title;
                sb.Append($"<color={q.TypeColor}>{n}. {(ready ? "<color=#7ee07e>완 료</color> " : "")}[{q.Lv}] {tag}{title}</color>\n");
                string goal = ready ? QuestSystem.ReportLine(q) : quests.ObjectiveLine(q);
                if (goal.Length > 26) goal = goal.Substring(0, 25) + "..";
                sb.Append($"<align=right>{goal}</align>\n");
            }
            if (n == 0)
                sb.Append(hereOnly ? "<color=#8090a0>이 낚시터에서 할 퀘스트가 없습니다.</color>"
                    : "<color=#8090a0>진행중인 퀘스트가 없습니다.\n머리 위 <color=#ffd060>!</color> NPC에게 말을 걸어 보세요.</color>");
            _questGoal.text = sb.ToString().TrimEnd('\n');
            float h = 62f + _questGoal.GetPreferredValues(_questGoal.text, 316f, 0f).y;
            _questBox.sizeDelta = new Vector2(344, h);
            _tourBox.anchoredPosition = new Vector2(-14, -358 - h - 8);
        }

        /// <summary>F 대화 안내 뒤에 붙는 퀘스트 표시 (완료할 것 / 새 퀘스트).</summary>
        static string PromptQuestTag(Fishing.Characters.NpcAngler npc)
        {
            var m = Game.Quests.MarkFor(npc.QuestKey);
            return m == QuestSystem.Mark.Ready ? "  <color=#70e070>· 퀘스트 완료</color>"
                : m == QuestSystem.Mark.AvailableStory || m == QuestSystem.Mark.Available ? "  <color=#ffd060>· 새 퀘스트</color>" : "";
        }

        void UpdateTournament()
        {
            var t = Game.Tournament;
            bool show = t != null && t.Joined && !Game.Profile.Data.tournament.settled;
            _tourBox.gameObject.SetActive(show);
            if (!show) return;
            if (t.Phase == TournamentRules.Phase.Registration)
            {
                _tourTitle.text = "09:00 시작";
                _tourBody.text = $"{TournamentRules.Title(t.Day, t.MapId)}\n<color=#8090a0>{TournamentRules.MapShortName(t.MapId)}에서 열립니다.\n많이 · 길게 · 길이 합계로 겨뤄요.</color>";
                return;
            }
            float left = t.MinutesLeft;
            _tourTitle.text = $"남은 시간 {Mathf.FloorToInt(left / 60f)}:{Mathf.FloorToInt(left % 60f):00}";
            if (Time.frameCount % 15 != 0 && !string.IsNullOrEmpty(_tourBody.text)) return;
            var entries = t.Entries();
            var sb = new System.Text.StringBuilder();
            if (FieldMap.Id != Game.Profile.Data.tournament.mapId)
                sb.Append($"<color=#ff9a50>대회장은 {TournamentRules.MapShortName(Game.Profile.Data.tournament.mapId)}입니다!</color>\n");
            int shown = 0;
            foreach (var e in entries)
            {
                if (shown >= 5 && !e.IsPlayer) continue;
                string line = $"{e.Rank}. {e.Name}  <color=#a0b0c0>{e.Count}마리 · {e.Longest:0.0}cm</color>  <b>{e.Points}</b>";
                sb.Append(e.IsPlayer ? $"<color=#ffd060>{line}</color>" : line).Append('\n');
                shown++;
            }
            _tourBody.text = sb.ToString();
        }

        // 미니맵 좌표: 맵의 View 범위를 320x212 칸에 맞춘다
        static float _mapScale = MapScale;
        static Vector2 _mapCenter;
        static Vector2 MapPos(float x, float z) => new Vector2((x - _mapCenter.x) * _mapScale, (z - _mapCenter.y) * _mapScale);

        RectTransform _mapContent;
        string _mapBuiltFor;

        /// <summary>현재 낚시터의 미니맵을 그린다 (땅, 물, 격자, 지형 표시, 낚시점). 맵이 바뀌면 다시 그린다.</summary>
        void BuildMinimapContent()
        {
            var map = FieldMap.Current;
            _mapBuiltFor = map.Id;
            if (_mapContent != null) Object.Destroy(_mapContent.gameObject);
            _schoolMarks.Clear();
            _shadowMark = null;
            _mapContent = UIFactory.Node("Content", _mapRoot).Fill();
            _mapContent.SetAsFirstSibling();
            _mapScale = Mathf.Min(320f / map.View.width, 212f / map.View.height);
            _mapCenter = map.View.center;

            UIFactory.Image(_mapContent, new Color(0.24f, 0.3f, 0.24f), null, "Land").rectTransform.Fill();
            foreach (var m in map.Marks)
            {
                var size = new Vector2(Mathf.Max(3f, m.Area.width * _mapScale), Mathf.Max(3f, m.Area.height * _mapScale));
                var img = UIFactory.Image(_mapContent, m.Color, m.Kind == MapMark.Shape.Circle ? UIFactory.Circle : null, "Mark").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(m.Area.center.x, m.Area.center.y), size);
                if (m.Label == "낚시점")
                    UIFactory.Text(img, m.Label, 11, UIFactory.Gold, TextAnchor.MiddleLeft).rectTransform
                        .At(new Vector2(1, 0.5f), new Vector2(3, 0), new Vector2(50, 14), new Vector2(0, 0.5f));
            }
            float gw = map.Cols * map.CellW, gh = map.Rows * map.CellH;
            var gridCenter = new Vector2(map.GridMinX + gw * 0.5f, map.GridMinZ + gh * 0.5f);
            for (int c = 0; c <= map.Cols; c++)
            {
                float x = map.GridMinX + c * map.CellW;
                UIFactory.Image(_mapContent, new Color(1, 1, 1, 0.14f), null, "GridV").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(x, gridCenter.y), new Vector2(1, gh * _mapScale));
                if (c < map.Cols)
                    UIFactory.Text(_mapContent, ((char)('A' + c)).ToString(), 12, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter).rectTransform
                        .At(new Vector2(0.5f, 0.5f), MapPos(x + map.CellW * 0.5f, map.GridMaxZ) + new Vector2(0, 9), new Vector2(30, 16));
            }
            for (int r = 0; r <= map.Rows; r++)
            {
                float z = map.GridMinZ + r * map.CellH;
                UIFactory.Image(_mapContent, new Color(1, 1, 1, 0.14f), null, "GridH").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(gridCenter.x, z), new Vector2(gw * _mapScale, 1));
                if (r < map.Rows)
                    UIFactory.Text(_mapContent, (r + 1).ToString(), 12, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter).rectTransform
                        .At(new Vector2(0.5f, 0.5f), MapPos(map.GridMinX, z + map.CellH * 0.5f) + new Vector2(8, 0), new Vector2(16, 16));
            }
        }

        // 메뉴 독 글자: 키를 바꾸면 다시 쓴다
        readonly List<(TMP_Text text, KeyAction key, string label)> _menuLabels = new List<(TMP_Text, KeyAction, string)>();
        int _menuVersion = -1;

        void UpdateMenuLabels()
        {
            if (_menuVersion == Settings.Version) return;
            _menuVersion = Settings.Version;
            foreach (var (text, key, label) in _menuLabels)
                text.text = $"<size=11><color=#7fd0ff>{Settings.KeyName(key)}</color></size>\n{label}";
        }

        void BuildBottomBar()
        {
            (KeyAction key, string label, System.Action action)[] menu =
            {
                (KeyAction.Quest, "퀘스트", () => Game.UI.OpenExclusive(Game.UI.Quest)),
                (KeyAction.Dex, "도감", () => Game.UI.OpenExclusive(Game.UI.Dex)),
                (KeyAction.KeepNet, "살림망", () => Game.UI.OpenExclusive(Game.UI.KeepNet)),
                (KeyAction.Inventory, "가방", () => Game.UI.OpenExclusive(Game.UI.Inventory)),
                (KeyAction.Costume, "캐릭터", () => Game.UI.OpenExclusive(Game.UI.Character)),
                (KeyAction.Shop, "상점", () => Game.UI.OpenShop()),
                (KeyAction.Tips, "설명", () => Game.UI.ToggleTips()),
                (KeyAction.Help, "도움말", () => Game.UI.OpenExclusive(Game.UI.Help)),
                (KeyAction.Save, "저장", () => Game.UI.SaveNow()),
                (KeyAction.Boat, "배", () => Game.Boat.Toggle()),
                (KeyAction.Options, "옵션", () => Game.UI.OpenExclusive(Game.UI.Options)),
            };
            // 왼쪽 아래 떠 있는 유리 메뉴 독
            const float slot = 86f;
            var dock = UIFactory.Panel(_root, "MenuDock", 18f, 0.58f).rectTransform
                .At(new Vector2(0, 0), new Vector2(14, 12), new Vector2(menu.Length * slot + 12, 58));
            for (int i = 0; i < menu.Length; i++)
            {
                var (key, label, action) = menu[i];
                var btn = UIFactory.Button(dock, "", () => action(), new Color(1f, 1f, 1f, 0.001f), 15);
                ((RectTransform)btn.transform).At(new Vector2(0, 0.5f), new Vector2(6 + i * slot, 0), new Vector2(slot - 4, 46));
                var text = btn.GetComponentInChildren<TMP_Text>();
                _menuLabels.Add((text, key, label));
                // 마우스를 올리면 은은하게 밝아진다
                var colors = btn.colors;
                colors.highlightedColor = new Color(1f, 1f, 1f, 90f);
                btn.colors = colors;
            }

            // 미끼 퀵슬롯 (숫자키 1~6): 오른쪽 아래 유리 독, 마지막 칸은 살림망
            _baitBar = UIFactory.Panel(_root, "BaitDock", 18f, 0.58f).rectTransform
                .At(new Vector2(1, 0), new Vector2(-14, 12), new Vector2(600, 58), new Vector2(1, 0));
            BuildBaitSlots();
            var net = UIFactory.Button(_baitBar, "", () => Game.UI.OpenExclusive(Game.UI.KeepNet), UIFactory.BtnGhost, 14);
            ((RectTransform)net.transform).At(new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(104, 46));
            _netSlot = net.GetComponentInChildren<TMP_Text>();
        }

        /// <summary>배 계기판 (원작: 오른쪽 아래 속도계·연료계).</summary>
        void BuildBoatGauges()
        {
            _boatGauges = UIFactory.Node("BoatGauges", _root).At(new Vector2(1, 0), new Vector2(-20, 86), new Vector2(300, 170), new Vector2(1, 0));
            (_speedFill, _speedText) = Gauge(_boatGauges, new Vector2(-220, 0), "SPEED", new Color(0.35f, 0.85f, 1f));
            (_fuelFill, _fuelText) = Gauge(_boatGauges, new Vector2(-70, 0), "FUEL", new Color(1f, 0.75f, 0.3f));
            _boatHintPill = UIFactory.Glass(_boatGauges, "BoatHint", UIFactory.Pill, null, 0.55f, false, false).rectTransform
                .At(new Vector2(1, 1), new Vector2(0, 40), new Vector2(560, 32), new Vector2(1, 1));
            _boatHint = UIFactory.Text(_boatHintPill, "", 14, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _boatHint.rectTransform.Fill();
            // 원작 계기판 오른쪽 아래 둥근 버튼 "ENGINE START STOP [Y]"
            var eng = UIFactory.Button(_boatGauges, "", () => Game.Boat.ToggleEngine(), new Color(0.1f, 0.14f, 0.2f, 0.95f), 12);
            UIFactory.SetRadius(eng.targetGraphic, UIFactory.Pill);
            ((RectTransform)eng.transform).At(new Vector2(1, 0), new Vector2(-340, 10), new Vector2(92, 92), new Vector2(0.5f, 0f));
            _engineRing = UIFactory.Image(eng.transform, new Color(0.4f, 0.9f, 1f), UIFactory.Ring, "Ring");
            _engineRing.rectTransform.Fill(4, 4, 4, 4);
            _engineRing.raycastTarget = false;
            _engineText = eng.GetComponentInChildren<TMP_Text>();
            _engineText.lineSpacing = -12f;
        }

        Image _engineRing;
        TMP_Text _engineText;

        static (Image fill, TMP_Text value) Gauge(RectTransform parent, Vector2 pos, string label, Color color)
        {
            var bg = UIFactory.Glass(parent, label + "Bg", UIFactory.Pill, null, 0.6f, true, false).rectTransform
                .At(new Vector2(1, 0), pos, new Vector2(140, 140), new Vector2(0.5f, 0f));
            var track = UIFactory.Image(bg, new Color(1, 1, 1, 0.1f), UIFactory.Ring, "Track");
            track.rectTransform.Fill(8, 8, 8, 8);
            var fill = UIFactory.Image(bg, color, UIFactory.Ring, "Fill");
            fill.rectTransform.Fill(8, 8, 8, 8);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Bottom;
            fill.fillClockwise = true;
            var value = UIFactory.Text(bg, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            value.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(130, 34));
            UIFactory.Text(bg, label, 12, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .At(new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(100, 18));
            return (fill, value);
        }

        void UpdateBoatGauges()
        {
            var boat = Game.Boat;
            bool show = boat != null && boat.Riding;
            _boatGauges.gameObject.SetActive(show);
            if (!show) return;
            var def = boat.Def;
            float kmh = Mathf.Abs(boat.Speed) * 3.6f;
            _speedFill.fillAmount = Mathf.Clamp01(Mathf.Abs(boat.Speed) / def.BoatSpeed) * 0.8f;
            _speedText.text = $"{kmh:0}<size=13>km/h</size>";
            float fuel = Game.Profile.Data.boatFuel;
            if (def.Sail)
            {
                // 돛단배: 연료 대신 바람 (세기, 지금 방향이 얼마나 좋은지)
                _fuelFill.fillAmount = Mathf.Clamp01(boat.WindFactor) * 0.8f;
                _fuelFill.color = new Color(0.6f, 0.95f, 0.75f);
                _fuelText.text = $"{Game.Weather.WindSpeed:0.0}<size=13>m/s</size>";
            }
            else
            {
                _fuelFill.fillAmount = Mathf.Clamp01(fuel / def.FuelCapacity) * 0.8f;
                _fuelFill.color = fuel < def.FuelCapacity * 0.2f ? UIFactory.Danger : new Color(1f, 0.75f, 0.3f);
                _fuelText.text = $"{fuel:0.0}<size=13>L</size>";
            }
            bool on = boat.EngineOn;
            _engineRing.transform.parent.gameObject.SetActive(!def.Sail);
            _engineRing.color = on ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.45f, 0.35f, 0.5f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.5f);
            _engineText.text = $"<size=11>ENGINE</size>\n<b><color={(on ? "#70ff90" : "#ff8070")}>{(on ? "STOP" : "START")}</color></b>\n<size=11><color=#80c8ff>[Y]</color></size>";
            _boatHint.text = boat.Seated
                ? $"{def.Name}  ·  W/S 속도  ·  A/D 방향  ·  멈추면 낚시" + (def.Sail ? $"  ·  바람 {Game.Weather.WindSpeed:0}m/s, 옆바람이 빠르다" : "")
                : def.Sail || !on ? $"{def.Name}  ·  뱃머리에서 낚시할 수 있습니다  ·  Y 시동 · 물가에서 B로 내리기"
                : $"{def.Name}  ·  시동이 걸려 있습니다 — Y로 엔진을 꺼야 낚시할 수 있습니다";
            FitPill(_boatHintPill, _boatHint, 40f, 900f);
        }

        public const int MaxBaitSlots = 8;
        RectTransform _baitBar;
        readonly List<GameObject> _baitSlotObjects = new List<GameObject>();
        string _baitsBuiltFor;

        /// <summary>지금 낚시터의 미끼 (최대 8개, 숫자키 1~8).</summary>
        public static List<BaitDef> MapBaits()
        {
            // 쓸 수 있는 해체 미끼 중 가장 좋은 것 하나를 마지막 칸에
            BaitDef cut = null;
            foreach (var b in GearDatabase.Baits)
                if (b.NotSold && Game.Profile.Count(b.Id) > 0 && b.Level <= Game.Profile.Data.level && (cut == null || b.Potency > cut.Potency)) cut = b;
            var list = new List<BaitDef>();
            int sold = cut != null ? MaxBaitSlots - 1 : MaxBaitSlots;
            foreach (var b in GearDatabase.Baits)
                if (!b.NotSold && b.SoldIn(FieldMap.Id) && list.Count < sold) list.Add(b);
            if (cut != null) list.Add(cut);
            return list;
        }

        static string BaitKey()
        {
            var sb = new System.Text.StringBuilder(FieldMap.Id);
            foreach (var b in MapBaits()) sb.Append('|').Append(b.Id);
            return sb.ToString();
        }

        void BuildBaitSlots()
        {
            _baitsBuiltFor = BaitKey();
            foreach (var go in _baitSlotObjects) Object.Destroy(go);
            _baitSlotObjects.Clear();
            _baitSlots.Clear();
            var baits = MapBaits();
            // 칸이 7개 이상이면 조금 좁게 (왼쪽 메뉴 줄과 겹치지 않게)
            float step = baits.Count > 6 ? 76f : 84f;
            for (int i = 0; i < baits.Count; i++)
            {
                var bait = baits[i];
                int index = i;
                var btn = UIFactory.Button(_baitBar, "", () => SelectBait(index), UIFactory.BtnGhost, 13);
                ((RectTransform)btn.transform).At(new Vector2(1, 0.5f), new Vector2(-122 - (baits.Count - 1 - i) * step, 0), new Vector2(step - 4f, 46));
                var label = btn.GetComponentInChildren<TMP_Text>();
                var icon = ItemIcons.Get(bait.Id);
                if (icon != null)
                {
                    var img = UIFactory.Image(btn.transform, Color.white, icon, "Icon");
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    img.rectTransform.At(new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2(34, 34), new Vector2(0, 0.5f));
                    label.rectTransform.offsetMin = new Vector2(30, label.rectTransform.offsetMin.y);
                }
                label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = 13f;
                _baitSlots.Add((btn.GetComponent<Image>(), label, bait));
                _baitSlotObjects.Add(btn.gameObject);
            }
            // 독 너비를 칸 수에 맞춘다 (미끼 칸 + 구분선 + 살림망)
            _baitBar.sizeDelta = new Vector2(122 + baits.Count * step + 6, 58);
            _baitDivider ??= UIFactory.Divider(_baitBar, "Sep").rectTransform.At(new Vector2(1, 0.5f), new Vector2(-116, 0), new Vector2(1, 34));
        }

        RectTransform _baitDivider;

        /// <summary>퀵슬롯 칸에 들어가게 긴 미끼 이름은 앞 4글자만 (전체 이름은 왼쪽 위 장착 미끼 칸·가방에).</summary>
        static string ShortName(string name) => name.Length > 5 ? name.Substring(0, 4) + ".." : name;

        public void SelectBait(int index)
        {
            var baits = MapBaits();
            if (index < 0 || index >= baits.Count) return;
            if (Game.Fishing.State != FishingState.Idle)
            {
                Game.Toast("낚시 중에는 미끼를 바꿀 수 없습니다.", ToastKind.Warn);
                return;
            }
            var bait = baits[index];
            Game.Profile.SelectBait(bait.Id);
            Game.Toast($"미끼 변경: {bait.Name}");
        }

        TMP_Text _aimLines;

        /// <summary>원작: 조준 중 큰 거리 숫자 바로 아래 업적 진행 3줄 (작은 흰 글씨, 가운데 정렬).</summary>
        void BuildAimLines()
        {
            _aimLines = UIFactory.Text(_root, "", 16, new Color(1f, 1f, 1f, 0.92f), TextAnchor.UpperCenter);
            _aimLines.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -258), new Vector2(900, 80));
            _aimLines.lineSpacing = 4f;
            UIFactory.Outline(_aimLines, new Color(0f, 0f, 0f, 0.6f), 0.2f);
            _aimLines.gameObject.SetActive(false);
        }

        void BuildCasting()
        {
            _castDistance = UIFactory.Text(_root, "", 110, new Color(1, 1, 1, 0.92f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _castDistance.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(800, 140));
            UIFactory.Outline(_castDistance, new Color(0, 0, 0, 0.45f), 1.8f);

            _tipTitle = UIFactory.Text(_root, "[설명 끄기 : U]", 18, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _tipTitle.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(1200, 26));
            _tipText = UIFactory.Text(_root, "", 21, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _tipText.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1400, 30));

            // 캐스팅 안내: 마우스 + 위로 미는 화살표
            _castTip = UIFactory.Node("CastTip", _root).At(new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(120, 200));
            UIFactory.Image(_castTip, Color.white, UIFactory.Mouse(true), "Mouse").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(70, 105));
            var up = UIFactory.Image(_castTip, new Color(0.3f, 0.85f, 1f, 0.9f), UIFactory.Arrow, "Up");
            up.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(90, 50));
            up.rectTransform.localRotation = Quaternion.Euler(0, 0, 90f);
        }

        void BuildHookGauge()
        {
            // 원작 챔질 클로즈업 (2013 홍현 영상 0:39.5~0:42.8): 화면 오른쪽에 카드 없이 가는 찌톱 막대가 떠 있고,
            // 오른쪽에 BAD~PERFECT 글자, 아래로는 물빛처럼 하늘색 빛기둥이 번진다. 찌가 잠길수록 등급이 내려간다(PERFECT가 맨 아래).
            // 화면 가장자리는 하얀 안개(물보라)처럼 번진다
            _biteMist = UIFactory.Image(_root, new Color(0.85f, 0.92f, 1f, 0f), UIFactory.Vignette, "BiteMist");
            _biteMist.rectTransform.Fill(-40, -40, -40, -40);

            _hookGauge = UIFactory.Node("HookGauge", _root).At(new Vector2(0.5f, 0.5f), new Vector2(330, 60), new Vector2(320, GaugeHeight + 40));
            var glow = UIFactory.Image(_hookGauge, new Color(0.3f, 0.85f, 1f, 0.75f), UIFactory.Glow, "WaterGlow");
            glow.rectTransform.At(new Vector2(0, 0), new Vector2(100, GaugeHeight * 0.5f - 70f), new Vector2(150, 190));
            _gaugeGlow = glow;
            var core = UIFactory.Image(_hookGauge, new Color(0.75f, 0.97f, 1f, 0.85f), UIFactory.Glow, "WaterCore");
            core.rectTransform.At(new Vector2(0, 0), new Vector2(100, GaugeHeight * 0.5f - 40f), new Vector2(54, 120));
            UIFactory.Image(_hookGauge, new Color(0.8f, 0.97f, 1f, 0.8f), null, "Surface").rectTransform
                .At(new Vector2(0, 0), new Vector2(100, GaugeHeight * 0.5f), new Vector2(90, 2));

            _gaugeBobber = UIFactory.Node("Bobber", _hookGauge).At(new Vector2(0, 0), new Vector2(100, GaugeHeight * 0.5f), new Vector2(8, 160), new Vector2(0.5f, 0f));
            Color[] stripes = { new Color(0.1f, 0.1f, 0.1f), new Color(0.2f, 0.75f, 0.3f), new Color(1f, 0.85f, 0.1f), new Color(0.95f, 0.25f, 0.2f), new Color(0.2f, 0.75f, 0.3f), new Color(1f, 0.85f, 0.1f), new Color(0.95f, 0.25f, 0.2f) };
            for (int i = 0; i < stripes.Length; i++)
                UIFactory.Image(_gaugeBobber, stripes[i], null, "Stripe").rectTransform
                    .At(new Vector2(0.5f, 0), new Vector2(0, i * 160f / stripes.Length), new Vector2(6, 160f / stripes.Length - 2));

            string[] names = { "BAD", "SO SO", "GOOD", "GREAT", "PERFECT" };
            for (int i = 0; i < names.Length; i++)
            {
                var t = UIFactory.Text(_hookGauge, names[i], 14 + i, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
                t.rectTransform.At(new Vector2(0, 0), new Vector2(150, GaugeHeight * 0.5f + 140 - i * 36), new Vector2(120, 30));
                _gradeLabels[i] = t;
            }

            // 마우스 + 네 방향 화살표 (원작: 가운데 위에서 깜빡인다)
            _shakeTip = UIFactory.Node("ShakeTip", _root).At(new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(200, 200));
            for (int i = 0; i < 4; i++)
            {
                var a = UIFactory.Image(_shakeTip, new Color(0.3f, 0.85f, 1f, i < 2 ? 0.95f : 0.55f), UIFactory.Arrow, "Arrow" + i);
                float ang = i switch { 0 => 0f, 1 => 180f, 2 => 90f, _ => 270f };
                var dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                a.rectTransform.At(new Vector2(0.5f, 0.5f), dir * 58f, new Vector2(46, 26));
                a.rectTransform.localRotation = Quaternion.Euler(0, 0, ang);
            }
            UIFactory.Image(_shakeTip, Color.white, UIFactory.Mouse(true), "Mouse").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 76));

            _gradePopup = UIFactory.Text(_root, "", 80, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _gradePopup.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 120));
            UIFactory.Outline(_gradePopup, new Color(0.2f, 0.1f, 0f, 0.85f), 2f);

            // 본신 경보 (원작 0:36.5~0:37.1): 캐릭터 머리 위에 빨간 느낌표가 팍 튀어나와 떨린다
            _biteAlert = UIFactory.Node("BiteAlert", _root).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 120));
            UIFactory.Image(_biteAlert, new Color(1f, 0.2f, 0.12f, 0.45f), UIFactory.Glow, "Burst").rectTransform.Fill(-10, -10, -10, -10);
            var bang = UIFactory.Text(_biteAlert, "!", 96, new Color(0.95f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            bang.rectTransform.Fill();
            UIFactory.Outline(bang, new Color(0.25f, 0f, 0f, 0.95f), 3f);
        }

        void BuildCenter()
        {
            // 아래 가운데 안내문: 글자 길이에 맞춘 유리 알약
            _hintPill = UIFactory.Glass(_root, "HintPill", UIFactory.Pill, null, 0.5f, false, false).rectTransform
                .At(new Vector2(0.5f, 0), new Vector2(0, 84), new Vector2(600, 34));
            _hint = UIFactory.Text(_hintPill, "", 16, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _hint.rectTransform.Fill();
            _promptPill = UIFactory.Glass(_root, "PromptPill", UIFactory.Pill, null, 0.62f, true, false).rectTransform
                .At(new Vector2(0.5f, 0), new Vector2(0, 128), new Vector2(500, 40));
            _prompt = UIFactory.Text(_promptPill, "", 20, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.rectTransform.Fill();
        }

        RectTransform _hintPill, _promptPill;

        /// <summary>알약 배경을 글자 너비에 맞춘다. 글자가 없으면 숨긴다.</summary>
        static void FitPill(RectTransform pill, TMP_Text text, float pad = 44f, float max = 1500f)
        {
            bool show = !string.IsNullOrEmpty(text.text);
            pill.gameObject.SetActive(show);
            if (show) pill.sizeDelta = new Vector2(Mathf.Min(max, text.preferredWidth + pad), pill.sizeDelta.y);
        }

        // ---------------- 이벤트 ----------------

        public void OnCasted(float distance)
        {
            _castShownValue = distance;
            _castShownUntil = Time.time + 1.6f;
        }

        const int MaxToasts = 3;

        GameObject _warningCard;

        /// <summary>
        /// 원작 실패 경고 (2013 홍현 영상 0:54.5, 3:20, 6:24): 화면 위쪽 가운데에 흰 카드, 위에 빨간 삼각형 WARNING 표지,
        /// 카드 안에 검은 글씨로 사유. 3초 뒤 사라진다.
        /// </summary>
        public void ShowWarning(string message, string penalty = null)
        {
            if (_warningCard != null) Object.Destroy(_warningCard);
            // 금속 테두리
            var frame = UIFactory.Shape(_root, new Color(0.62f, 0.64f, 0.66f, 0.98f), 10f, "WarningCard", 5f);
            UIFactory.AddShadow(frame, 22f, new Vector2(0, -6), 0.6f);
            var rt = frame.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(720, 128));
            var paper = UIFactory.Shape(rt, new Color(0.97f, 0.96f, 0.93f, 1f), 4f, "Paper", 1f);
            paper.rectTransform.Fill(8, 8, 8, 8);
            UIFactory.AddBorder(paper, new Color(0.8f, 0.12f, 0.1f, 0.95f), 2.5f);
            // 위아래 빗금 띠
            HazardBand(paper.rectTransform, true);
            HazardBand(paper.rectTransform, false);
            // 리벳
            foreach (var c in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) })
                UIFactory.Shape(rt, new Color(0.85f, 0.86f, 0.88f), UIFactory.Pill, "Rivet", 2f).rectTransform
                    .At(c, new Vector2(c.x < 0.5f ? 4 : -4, c.y < 0.5f ? 4 : -4), new Vector2(8, 8), new Vector2(0.5f, 0.5f));
            // 빨간 삼각 표지 (카드 위로 걸친다)
            var signRoot = UIFactory.Node("Sign", rt).At(new Vector2(0.5f, 1), new Vector2(0, 46), new Vector2(170, 120));
            var signBack = UIFactory.Text(signRoot, "▲", 190, new Color(1f, 1f, 1f, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
            signBack.rectTransform.Fill(-20, -20, -30, -10);
            var sign = UIFactory.Text(signRoot, "▲", 168, new Color(0.86f, 0.1f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
            sign.rectTransform.Fill(-20, -20, -24, -10);
            var bang = UIFactory.Text(signRoot, "!", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bang.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(60, 60));
            var label = UIFactory.Text(signRoot, "WARNING", 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(120, 20));
            UIFactory.Outline(label, new Color(0.5f, 0f, 0f, 0.9f), 0.25f);
            if (string.IsNullOrEmpty(penalty))
            {
                var text = UIFactory.Text(rt, message, 19, new Color(0.12f, 0.1f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                text.rectTransform.Fill(30, 30, 34, 22);
            }
            else
            {
                var text = UIFactory.Text(rt, penalty, 17, new Color(0.1f, 0.08f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(680, 30));
                var cause = UIFactory.Text(rt, message, 14, new Color(0.5f, 0.2f, 0.18f), TextAnchor.MiddleCenter);
                cause.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -28), new Vector2(660, 22));
            }
            _warningCard = frame.gameObject;
            _warningAt = Time.time;
            Object.Destroy(_warningCard, 3.5f);
        }

        float _warningAt;

        /// <summary>원작 경고 카드 위아래의 빨강·검정 빗금 띠.</summary>
        static void HazardBand(RectTransform paper, bool top)
        {
            var band = UIFactory.Node(top ? "BandTop" : "BandBottom", paper)
                .At(new Vector2(0.5f, top ? 1 : 0), new Vector2(0, top ? -6 : 6), new Vector2(680, 10), new Vector2(0.5f, top ? 1 : 0));
            band.gameObject.AddComponent<RectMask2D>();
            for (int i = 0; i < 52; i++)
            {
                var stripe = UIFactory.Image(band, i % 2 == 0 ? new Color(0.82f, 0.1f, 0.08f) : new Color(0.12f, 0.1f, 0.1f), null, "Stripe");
                stripe.rectTransform.At(new Vector2(0, 0.5f), new Vector2(i * 14f - 10f, 0), new Vector2(9, 30));
                stripe.rectTransform.localRotation = Quaternion.Euler(0, 0, -40f);
            }
        }

        // ---------------- 재도전 (원작: 실패 뒤 오른쪽 아래 "재도전 하시겠습니까?" + 오른쪽 위 "X2 재도전") ----------------

        RectTransform _retryBubble, _retryBadge;
        TMP_Text _retrySub, _retryBadgeText;
        float _retryUntil;

        void BuildRetry()
        {
            // 나무 액자 같은 갈색 말풍선 + 왼쪽 금테 별 아이콘 + 오른쪽 조합 안내원
            var bubble = UIFactory.Shape(_root, new Color(0.24f, 0.17f, 0.11f, 0.96f), 10f, "RetryBubble", 4f);
            UIFactory.AddBorder(bubble, new Color(0.78f, 0.6f, 0.35f, 0.95f), 3f);
            UIFactory.AddShadow(bubble, 18f, new Vector2(0, -5), 0.55f);
            _retryBubble = bubble.rectTransform.At(new Vector2(1, 0), new Vector2(-150, 90), new Vector2(430, 92), new Vector2(1, 0));
            var inner = UIFactory.Shape(_retryBubble, new Color(0.93f, 0.88f, 0.76f, 1f), 6f, "Paper", 1f);
            inner.rectTransform.Fill(6, 6, 6, 6);
            var iconBox = UIFactory.Shape(_retryBubble, new Color(0.2f, 0.14f, 0.08f), 8f, "IconBox", 2f);
            UIFactory.AddBorder(iconBox, UIFactory.Gold, 2.5f);
            iconBox.rectTransform.At(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(64, 64), new Vector2(0, 0.5f));
            UIFactory.Image(iconBox.rectTransform, new Color(1f, 0.85f, 0.35f, 0.6f), UIFactory.Glow, "Glow").rectTransform.Fill(-6, -6, -6, -6);
            var star = UIFactory.Text(iconBox.rectTransform, "★", 46, new Color(1f, 0.88f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold);
            star.rectTransform.Fill();
            UIFactory.Outline(star, new Color(0.45f, 0.25f, 0f, 0.9f), 0.25f);
            var q = UIFactory.Text(_retryBubble, "재도전 하시겠습니까?", 20, new Color(0.2f, 0.13f, 0.07f), TextAnchor.MiddleLeft, FontStyle.Bold);
            q.rectTransform.At(new Vector2(0, 1), new Vector2(92, -14), new Vector2(330, 30));
            _retrySub = UIFactory.Text(_retryBubble, "", 13, new Color(0.38f, 0.27f, 0.16f), TextAnchor.UpperLeft);
            _retrySub.rectTransform.At(new Vector2(0, 1), new Vector2(92, -46), new Vector2(330, 40));
            var btn = _retryBubble.gameObject.AddComponent<Button>();
            btn.targetGraphic = inner;
            btn.onClick.AddListener(AcceptRetry);
            // 조합 안내원 (원작: 밀짚모자 소녀 일러스트 → 색 원 + 이름)
            var face = UIFactory.Shape(_retryBubble, new Color(0.95f, 0.75f, 0.45f), UIFactory.Pill, "Guide", 3f);
            UIFactory.AddBorder(face, new Color(0.78f, 0.6f, 0.35f), 3f);
            face.rectTransform.At(new Vector2(1, 0.5f), new Vector2(120, 14), new Vector2(104, 104), new Vector2(1, 0.5f));
            UIFactory.Text(face.rectTransform, "서윤", 26, new Color(0.35f, 0.2f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
            UIFactory.Chip(_retryBubble, "한서윤 <조합 안내원>", new Color(0.2f, 0.14f, 0.08f, 0.95f), new Color(1f, 0.9f, 0.7f), 11, "GuideName")
                .transform.parent.GetComponent<RectTransform>().At(new Vector2(1, 0), new Vector2(124, -14), new Vector2(130, 20), new Vector2(1, 0));
            _retryBubble.gameObject.SetActive(false);

            // 오른쪽 위 "X2 재도전" 배지 (시계 왼쪽)
            var badge = UIFactory.Shape(_root, new Color(0.08f, 0.1f, 0.18f, 0.92f), 10f, "RetryBadge", 3f);
            UIFactory.AddBorder(badge, UIFactory.Gold, 2f);
            _retryBadge = badge.rectTransform.At(new Vector2(1, 1), new Vector2(-370, -14), new Vector2(128, 60), new Vector2(1, 1));
            _retryBadgeText = UIFactory.Text(_retryBadge, "", 30, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _retryBadgeText.rectTransform.Fill(4, 4, 2, 2);
            UIFactory.Outline(_retryBadgeText, new Color(0.3f, 0.15f, 0f, 0.9f), 0.25f);
            _retryBadge.gameObject.SetActive(false);
        }

        /// <summary>물고기를 놓친 뒤: 10초 동안 재도전 말풍선 (클릭 또는 Enter).</summary>
        public void OfferRetry(string fishName, int mult)
        {
            _retryUntil = Time.time + 10f;
            _retrySub.text = $"방금 놓친 <b>{fishName}</b>{GameUI.Josa(fishName, "이", "가")} 다시 옵니다\n다시 낚으면 <color=#b04010><b>EXP X{mult}</b></color>  ·  클릭 / Enter";
            _retryBubble.gameObject.SetActive(true);
            _retryBubble.localScale = Vector3.one * 0.6f;
        }

        public void AcceptRetry()
        {
            if (!_retryBubble.gameObject.activeSelf) return;
            _retryBubble.gameObject.SetActive(false);
            CatchRewards.Arm();
            Game.Sound.Coin();
            Game.Notice($"재도전! 다음 입질에 {CatchRewards.RevengeName}{GameUI.Josa(CatchRewards.RevengeName, "이", "가")} 다시 옵니다.");
        }

        void UpdateRetry()
        {
            if (_retryBubble == null) return;
            if (_retryBubble.gameObject.activeSelf)
            {
                float left = _retryUntil - Time.time;
                bool busy = Game.Fishing.State != FishingState.Idle && Game.Fishing.State != FishingState.Charging;
                if (left <= 0f || Game.Mode != GameMode.Field || busy)
                {
                    _retryBubble.gameObject.SetActive(false);
                    if (!busy) CatchRewards.Decline();
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) AcceptRetry();
                    _retryBubble.localScale = Vector3.one * Mathf.MoveTowards(_retryBubble.localScale.x, 1f, Time.deltaTime * 5f);
                    bool riding = Game.Boat != null && Game.Boat.Riding;
                    _retryBubble.anchoredPosition = new Vector2(-150, riding ? 264 : 90);
                }
            }
            bool armed = CatchRewards.Armed;
            _retryBadge.gameObject.SetActive(armed);
            if (armed)
            {
                _retryBadgeText.text = $"<color=#ffe070>★</color> X{CatchRewards.Mult}<size=55%> 재도전</size>";
                _retryBadge.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(Time.time * 3f)) * 0.05f);
            }
        }

        public void Toast(string message, ToastKind kind)
        {
            var color = kind switch
            {
                ToastKind.Good => UIFactory.Good,
                ToastKind.Warn => UIFactory.Warn,
                ToastKind.Great => UIFactory.Gold,
                _ => UIFactory.TextMain
            };
            // 메시지마다 글자 너비에 맞춘 유리 알약
            var pill = UIFactory.Glass(_toastRoot, "Toast", UIFactory.Pill, null, 0.62f, true, false);
            if (kind != ToastKind.Info) UIFactory.AddBorder(pill, new Color(color.r, color.g, color.b, 0.45f), 1.5f);
            var root = pill.rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(400, kind == ToastKind.Great ? 42 : 36));
            var t = UIFactory.Text(root, message, kind == ToastKind.Great ? 22 : 18, color, TextAnchor.MiddleCenter,
                kind == ToastKind.Great ? FontStyle.Bold : FontStyle.Normal);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            const float maxW = 980f;
            if (t.preferredWidth + 48f <= maxW)
            {
                t.rectTransform.Fill(24f, 24f);
                root.sizeDelta = new Vector2(t.preferredWidth + 48f, root.sizeDelta.y);
            }
            else
            {
                // 긴 알림은 두 줄 이상으로 감싼다 (한 줄로 두면 화면 밖으로 넘친다).
                // 글자를 알약 안쪽으로 들여 둥근 끝·테두리에 붙지 않게 한다
                const float padX = 48f, padY = 14f;
                t.rectTransform.Fill(padX, padX, padY, padY);
                t.textWrappingMode = TextWrappingModes.Normal;
                float h = t.GetPreferredValues(message, maxW - padX * 2f, 0f).y;
                root.sizeDelta = new Vector2(maxW, Mathf.Max(root.sizeDelta.y, h + padY * 2f));
            }
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            _toasts.Add((root, group, Time.time + (kind == ToastKind.Great ? 5f : 3.2f)));
            // 한꺼번에 많이 오면(레벨 보상·업적·퀘스트) 화면 가운데를 덮으므로 최근 3개만 남긴다
            while (_toasts.Count > MaxToasts)
            {
                Object.Destroy(_toasts[0].root.gameObject);
                _toasts.RemoveAt(0);
            }
        }

        // ---------------- 갱신 ----------------

        public void Update()
        {
            var p = Game.Profile;
            var d = p.Data;
            var fishing = Game.Fishing;
            // 파이팅 중이거나 대기실이면 필드 HUD를 숨긴다 (토스트는 남긴다)
            bool fighting = fishing.State == FishingState.Fighting || fishing.State == FishingState.Result || fishing.Hooking || Game.Mode == GameMode.Lobby
                || (Game.UI.Result != null && Game.UI.Result.IsOpen);

            // 파이팅 중에는 필드 HUD를 숨기고 토스트만 남긴다.
            // 본신(챔질)이 오면 원작처럼(0:37.8) 미니맵·퀘스트·메뉴를 걷어 내고 찌 게이지와 안내문만 남긴다
            bool biteMode = !fighting && fishing.State == FishingState.Bite;
            foreach (Transform child in _root)
            {
                if (child == _toastRoot || child == _gradePopup.transform) continue;
                bool keep = child == _hookGauge || child == _shakeTip || child == _biteAlert || child == _biteMist.transform
                            || child == _tipTitle.transform || child == _tipText.transform;
                child.gameObject.SetActive(!fighting && (!biteMode || keep));
            }

            UpdateToasts();
            UpdateRetry();
            if (fighting)
            {
                UpdateGradePopup();
                return;
            }
            if (biteMode)
            {
                UpdateHookGauge();
                return;
            }

            _levelText.text = $"Lv.{d.level}";
            _nameText.text = $"강태공  <size=13><color=#7fd8ff>{p.CurrentTitle}</color></size>";
            _staminaFill.fillAmount = d.stamina / p.StaminaCap;
            _staminaFill.color = p.StaminaTier > 0 ? UIFactory.Danger : p.Regenerating(Game.Clock.TotalMinutes) ? new Color(0.5f, 0.9f, 0.55f) : new Color(0.4f, 0.8f, 1f);
            _staminaText.text = $"{d.stamina:0.0} %" + (p.Regenerating(Game.Clock.TotalMinutes) ? " ▲" : "");
            bool maxLevel = d.level >= PlayerProfile.MaxLevel;
            _expFill.fillAmount = maxLevel ? 1f : d.exp / (float)PlayerProfile.ExpToNext(d.level);
            _expText.text = maxLevel ? "MAX" : $"{_expFill.fillAmount * 100f:0} %";
            _goldText.text = $"{d.gold:N0} G   ·   총 {d.totalCatches}마리";
            _baitName.text = p.Bait.Name;
            _baitCount.text = $"x{p.Count(p.Bait.Id)}";

            var clock = Game.Clock;
            int month = Seasons.MonthOf(clock.Day), day = Seasons.DayOfMonth(clock.Day);
            int h12 = clock.Hour % 12 == 0 ? 12 : clock.Hour % 12;
            string ampm = clock.Hour < 12 ? "AM" : "PM";
            string sky = clock.Band == TimeBand.Night || clock.Band == TimeBand.Dawn ? "<color=#c8d8ff>●</color>" : "<color=#ffd060>●</color>";
            _channelText.text = $"No. {Game.Channel:000}  {FieldMap.ShortName} CH.{Game.Channel}    ·    {Seasons.Name(Seasons.Current)} {Seasons.WeatherName(Game.Weather.Current)}" +
                                (Tide.Active ? $"  ·  <color=#7fe8ff>{Tide.Name}</color>" : "");
            _clockText.text = $"{sky}  {month:00}/{day:00}  <size=14>{ampm}</size> {h12:00}:{clock.Minute:00}";

            var playerPos = Game.Player.transform.position;
            var lookPos = fishing.State == FishingState.Waiting || fishing.State == FishingState.Bite ? fishing.BobberPosition : playerPos;
            _locationText.text = FieldMap.TryCell(lookPos, out int col, out int row) ? $"현재 위치 : {FieldMap.CellName(col, row)}" : "현재 위치 : 둔치";
            _areaText.text = FieldMap.AreaName(playerPos);

            UpdateQuestList();

            if (Time.frameCount % 20 == 0 && _baitsBuiltFor != BaitKey()) BuildBaitSlots();
            for (int i = 0; i < _baitSlots.Count; i++)
            {
                var (bg, label, bait) = _baitSlots[i];
                bool selected = d.baitId == bait.Id;
                int count = p.Count(bait.Id);
                label.text = $"<size=75%><color=#80c8ff>{i + 1}</color></size> {ShortName(bait.Name)}\n<b>x{count}</b>";
                label.color = count > 0 ? UIFactory.TextMain : UIFactory.TextDim;
                bg.color = selected ? UIFactory.BtnPrimary : UIFactory.BtnGhost;
            }
            _netSlot.text = $"<size=11><color=#80c8ff>Z</color></size> 살림망\n<b>{d.keepNet.Count}/{Game.Profile.KeepNetSize}</b>";

            UpdateTournament();
            UpdateMenuLabels();
            UpdateMap(playerPos);
            UpdateBoatGauges();
            UpdateCasting();
            UpdateHookGauge();
            UpdateCenter(playerPos);
        }

        void UpdateMap(Vector3 playerPos)
        {
            if (_mapBuiltFor != FieldMap.Current.Id) BuildMinimapContent();
            _playerArrow.anchoredPosition = MapPos(playerPos.x, playerPos.z);
            _playerArrow.localRotation = Quaternion.Euler(0, 0, -Game.Player.Yaw);

            var state = Game.Fishing.State;
            bool showBobber = state == FishingState.Waiting || state == FishingState.Bite || state == FishingState.Casting;
            _bobberDot.gameObject.SetActive(showBobber);
            if (showBobber) _bobberDot.anchoredPosition = MapPos(Game.Fishing.BobberPosition.x, Game.Fishing.BobberPosition.z);

            var schools = Game.Schools.Schools;
            while (_schoolMarks.Count < schools.Count)
            {
                var mark = UIFactory.Text(_mapContent, "◆", 13, new Color(1f, 0.75f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold, "School").rectTransform
                    .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
                _schoolMarks.Add(mark);
            }
            for (int i = 0; i < schools.Count; i++)
            {
                var c = schools[i].Spot;
                _schoolMarks[i].anchoredPosition = MapPos(c.x, c.z) + new Vector2(Mathf.Sin(Time.time * 1.5f + i) * 3f, 0f);
                _schoolMarks[i].localScale = Vector3.one * (schools[i].Special || schools[i].Kind != Fishing.World.SchoolKind.Normal ? 1.6f : schools[i].Big ? 1.15f : 0.65f);
                _schoolMarks[i].GetComponent<TMPro.TMP_Text>().color = schools[i].Kind == Fishing.World.SchoolKind.Normal ? new Color(1f, 0.75f, 0.3f) : schools[i].Tint;
                _schoolMarks[i].gameObject.SetActive(!schools[i].Hidden && !schools[i].Shadow);
                if (schools[i].Shadow) UpdateShadowMark(schools[i]);
            }
        }

        /// <summary>
        /// 원작에는 없지만 미니맵에도 물속 그림자를 띄운다: 그 어종 아이콘을 검게 칠한 실루엣이 헤엄치는 쪽을 보고 천천히 움직이고,
        /// 둘레에 고리가 숨 쉬듯 빛난다. 퀘스트 고기면 금빛 고리와 "!", 아니면 물빛 고리와 "?".
        /// </summary>
        void UpdateShadowMark(FishSchool s)
        {
            bool show = !s.Hidden && !s.Hooked && s.SpeciesIds.Length > 0;
            if (!show)
            {
                if (_shadowMark != null) _shadowMark.gameObject.SetActive(false);
                return;
            }
            if (_shadowMark == null)
            {
                _shadowMark = UIFactory.Node("Shadow", _mapContent).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                _shadowHalo = UIFactory.Image(_shadowMark, Color.white, UIFactory.Circle, "Halo");
                _shadowHalo.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
                _shadowIcon = UIFactory.Image(_shadowMark, new Color(0.03f, 0.05f, 0.07f, 0.95f), null, "Silhouette");
                _shadowIcon.preserveAspect = true;
                _shadowIcon.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 17));
                _shadowTag = UIFactory.Text(_shadowMark, "", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Tag");
                _shadowTag.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(13, 12), new Vector2(16, 16));
                _shadowSpecies = null;
            }
            _shadowMark.gameObject.SetActive(true);
            var sp = FishDatabase.Get(s.SpeciesIds[0]);
            var icon = FishModels.Icon(sp);
            if (_shadowSpecies != sp.Id)
            {
                _shadowSpecies = sp.Id;
                _shadowIcon.sprite = icon != null ? icon : UIFactory.Fish;
            }
            _shadowMark.anchoredPosition = MapPos(s.Spot.x, s.Spot.z);
            // 아이콘은 왼쪽을, 기본 물고기 그림은 오른쪽을 본다. 헤엄치는 쪽으로 돌려 세우고 위아래로 조금 기울인다
            if (Mathf.Abs(s.Heading.x) > 0.2f) _shadowFacesRight = s.Heading.x > 0f;
            float tilt = Mathf.Clamp(Mathf.Atan2(s.Heading.z, Mathf.Abs(s.Heading.x)) * Mathf.Rad2Deg, -35f, 35f);
            bool flip = _shadowFacesRight == (icon != null);
            _shadowIcon.rectTransform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
            _shadowIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _shadowFacesRight ? tilt : -tilt);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            var ring = s.Quest ? new Color(1f, 0.82f, 0.3f) : s.Tint;
            _shadowHalo.color = new Color(ring.r, ring.g, ring.b, 0.25f + 0.25f * pulse);
            _shadowHalo.rectTransform.localScale = Vector3.one * (0.9f + 0.2f * pulse);
            _shadowTag.text = s.Quest ? "!" : "?";
            _shadowTag.color = s.Quest ? UIFactory.Gold : new Color(0.7f, 0.92f, 1f);
        }

        void UpdateCasting()
        {
            var f = Game.Fishing;
            bool tips = Game.Profile.Data.showTips;
            bool charging = f.State == FishingState.Charging && f.CastDistance > 0f; // 휘두르는 동안만
            bool showNumber = charging || Time.time < _castShownUntil;
            _castDistance.gameObject.SetActive(showNumber);
            if (charging)
            {
                _castDistance.text = $"{f.CastDistance:0.00}m";
                _castDistance.color = f.CastDistance >= 5f ? new Color(1, 1, 1, 0.92f) : new Color(1f, 0.5f, 0.45f, 0.92f);
            }
            else if (f.State == FishingState.Casting)
            {
                // 원작: 찌가 날아가는 동안 숫자가 7.18 → 12.34 → 19.83 … 처럼 따라 올라간다
                _castDistance.gameObject.SetActive(true);
                _castDistance.text = $"{f.FlyingDistance:0.00}m";
                _castDistance.color = new Color(1, 1, 1, 0.95f);
                _castDistance.rectTransform.localScale = Vector3.one;
                _castShownUntil = Time.time + 1.4f;
                _castShownValue = f.CastDistance;
                _castLandedAt = Time.time;
            }
            else if (showNumber)
            {
                // 착수 순간 숫자가 '툭' 커졌다가 돌아온다
                float since = Time.time - _castLandedAt;
                _castDistance.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.18f - since) * 1.6f);
                _castDistance.text = $"{_castShownValue:0.00}m";
                var c = Color.white;
                c.a = Mathf.Clamp01((_castShownUntil - Time.time) / 0.6f) * 0.92f;
                _castDistance.color = c;
            }
            _castTip.gameObject.SetActive(charging && tips);
            _aimLines.gameObject.SetActive(charging);
            if (charging && Time.frameCount % 10 == 0) _aimLines.text = string.Join("\n", CatchRewards.AimLines());
            if (charging) _castTip.anchoredPosition = new Vector2(0, 120 + Mathf.Repeat(Time.time * 1.6f, 1f) * 40f);
        }

        void UpdateHookGauge()
        {
            var f = Game.Fishing;
            bool tips = Game.Profile.Data.showTips;
            // 루어 대기 중에는 찌가 없으니 찌 게이지도 없다 (입질이 오면 나온다)
            bool waiting = (f.State == FishingState.Waiting && !f.LureMode && !f.Trolling) || f.State == FishingState.Bite;
            _hookGauge.gameObject.SetActive(waiting);
            if (waiting)
            {
                float sink = f.BobberSink;
                float bob = Mathf.Sin(Time.time * 2.1f) * 4f;
                // 잠기면 내려가고, 찌올림이면 솟는다 (게이지 위로 덜 삐져나오게 솟는 쪽은 짧게)
                _gaugeBobber.anchoredPosition = new Vector2(100, GaugeHeight * 0.5f - sink * (sink < 0f ? 90f : 160f) + bob);
                // 등급은 찌 깊이가 아니라 입질 패턴의 챔질 점수로 (찌올림은 솟은 꼭대기, 오르내림은 깊이 내려간 순간이 높다)
                float q = f.BiteQuality;
                int current = f.State != FishingState.Bite ? -1 : q < 0.2f ? 0 : q < 0.45f ? 1 : q < 0.7f ? 2 : q < 0.97f ? 3 : 4;
                for (int i = 0; i < _gradeLabels.Length; i++)
                {
                    bool on = i == current;
                    _gradeLabels[i].color = on ? (i == 4 ? UIFactory.Gold : Color.white) : new Color(1, 1, 1, 0.35f);
                    _gradeLabels[i].rectTransform.localScale = Vector3.one * (on ? 1.25f : 1f);
                }
            }

            UpdateGradePopup();

            // 본신이 오면 캐릭터 머리 위에 빨간 느낌표 (원작 0:36.5: 0.6초쯤 떨다가 클로즈업으로 넘어간다)
            bool bite = f.State == FishingState.Bite;
            bool bang = bite && f.BiteTime < FishingController.BiteCutDelay;
            _biteAlert.gameObject.SetActive(bang);
            if (bang)
            {
                var head = Game.Player.Head != null ? Game.Player.Head.position : Game.Player.transform.position + Vector3.up * 1.7f;
                var screen = Game.CameraRig.Cam.WorldToScreenPoint(head + Vector3.up * 0.75f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
                float k = f.BiteTime / 0.12f;
                _biteAlert.anchoredPosition = local + new Vector2(Mathf.Sin(Time.time * 60f) * 3f, 0f);
                _biteAlert.localScale = Vector3.one * (k < 1f ? Mathf.Lerp(2.2f, 1f, k) : 1f + Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 0.12f);
            }

            // 챔질 클로즈업: 가장자리 하얀 안개, 마우스 아이콘은 0.5초마다 깜빡, 찌 물빛은 일렁인다
            float mist = bite && !bang ? 0.55f : 0f;
            var mc = _biteMist.color;
            mc.a = Mathf.MoveTowards(mc.a, mist, Time.deltaTime * 3f);
            _biteMist.color = mc;
            _shakeTip.gameObject.SetActive(bite && !bang && tips && Mathf.Repeat(Time.time, 1f) < 0.6f);
            if (waiting && _gaugeGlow != null)
            {
                var gc = _gaugeGlow.color;
                gc.a = 0.6f + Mathf.Sin(Time.time * 3f) * 0.15f + (bite ? 0.2f : 0f);
                _gaugeGlow.color = gc;
            }

            UpdateTips();
        }

        void UpdateGradePopup()
        {
            var f = Game.Fishing;
            float age = Time.time - f.LastGradeAt;
            // 성공 등급은 파이팅 화면이 HIT 다음에 크게 띄운다 (원작 0:43). 여기서는 실패(BAD)만
            bool showGrade = age < 1.1f && f.LastGrade == HookGrade.Bad;
            _gradePopup.gameObject.SetActive(showGrade);
            if (showGrade)
            {
                _gradePopup.text = f.LastGrade switch
                {
                    HookGrade.Bad => "BAD",
                    HookGrade.SoSo => "SO SO~",
                    HookGrade.Good => "GOOD",
                    HookGrade.Great => "GREAT!",
                    _ => "PERFECT!!"
                };
                _gradePopup.color = f.LastGrade switch
                {
                    HookGrade.Bad => new Color(0.7f, 0.7f, 0.75f, 1f - age),
                    HookGrade.Perfect => new Color(1f, 0.85f, 0.3f, 1f - age * 0.5f),
                    HookGrade.SoSo => new Color(0.62f, 0.95f, 0.3f, 1f - age * 0.7f),
                    HookGrade.Good => new Color(0.36f, 0.9f, 0.8f, 1f - age * 0.7f),
                    _ => new Color(0.6f, 0.9f, 1f, 1f - age * 0.7f)
                };
                // 파이팅 중에는 HIT 아래에 (원작: HIT → SOSO~)
                _gradePopup.rectTransform.anchoredPosition = new Vector2(0, f.State == FishingState.Fighting ? -130f : 40f);
                _gradePopup.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.2f - age) * 2f);
            }
        }

        TMP_Text _actionPopup;

        /// <summary>루어 저킹 "액션!" (루어 위에 잠깐).</summary>
        void UpdateActionPopup()
        {
            if (_actionPopup == null)
            {
                _actionPopup = UIFactory.Text(_root, "액션!", 34, new Color(0.45f, 1f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Bold);
                _actionPopup.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 50));
            }
            var f = Game.Fishing;
            float age = Time.time - f.LastTwitchAt;
            bool show = f.LureMode && age < 0.7f;
            _actionPopup.gameObject.SetActive(show);
            if (!show) return;
            var cam = Game.CameraRig.Cam;
            var sp = cam.WorldToScreenPoint(f.BobberPosition + Vector3.up * (1.2f + age * 1.5f));
            if (sp.z < 0f) { _actionPopup.gameObject.SetActive(false); return; }
            var canvas = (RectTransform)_root.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, sp, null, out var local);
            _actionPopup.rectTransform.anchoredPosition = local;
            _actionPopup.color = new Color(0.45f, 1f, 0.85f, 1f - age / 0.7f);
        }

        void UpdateTips()
        {
            UpdateActionPopup();
            var f = Game.Fishing;
            bool tips = Game.Profile.Data.showTips;
            // 안내문 (원작 문구)
            string tip = f.State switch
            {
                FishingState.Charging when f.IceReady => "얼음낚시: 왼쪽 버튼을 누른 채 마우스를 살짝 내리면 발 앞에 구멍을 뚫고 채비를 내립니다.",
                FishingState.Charging => "왼쪽 버튼을 누른 채 마우스를 아래로 내려 낚싯대를 들고, 위로 빠르게 쭉 올리면 캐스팅! 빠를수록 멀리 (5m 미만은 실패)",
                FishingState.Waiting when f.Trolling => "트롤링 중: 천천히 몰면 줄이 배 뒤로 끌려간다. 입질이 오면 배가 서고, 좌우로 흔들어 챔질!",
                FishingState.Waiting when f.LureMode => "루어: 왼쪽 버튼을 꾹 눌러 감고, 감으면서 마우스를 위로 툭 치면 액션! 가만히 두면 잘 안 문다. 입질이 오면 좌우로 흔들어 챔질",
                FishingState.Waiting when f.Trembling => "손 끝으로부터 미세한 떨림이 느껴집니다.",
                FishingState.Waiting => "왼쪽 버튼을 누른 상태로 좌우로 흔들어 챔질하세요. 찌가 깔짝이는 건 예신이니 조심!",
                FishingState.Bite => "왼쪽 버튼을 누른 상태로 좌우로 흔들어 챔질하세요. 물에 완전히 잠겼을 때 흔들면 퍼펙트",
                FishingState.Idle => "던지고 싶은 물을 왼쪽 버튼으로 클릭하면 낚싯대를 꺼내 듭니다.",
                _ => ""
            };
            bool showTip = tips && !string.IsNullOrEmpty(tip) && f.State != FishingState.Idle;
            _tipTitle.gameObject.SetActive(showTip);
            _tipText.gameObject.SetActive(showTip);
            _tipText.text = tip;
            // 캐스팅 숫자가 뜨는 동안은 안내문을 위로 비켜 준다
            _tipText.rectTransform.anchoredPosition = new Vector2(0, _castDistance.gameObject.activeSelf ? -100 : -150);
            _tipTitle.rectTransform.anchoredPosition = new Vector2(0, _castDistance.gameObject.activeSelf ? -72 : -122);
        }

        void UpdateCenter(Vector3 playerPos)
        {
            var f = Game.Fishing;
            _hint.text = f.State switch
            {
                FishingState.Idle when Game.Player.OnBoat => Game.Boat.Seated
                    ? "W/S 속도 · A/D 방향 · 멈추면 뱃머리에서 낚시 · 물가에서 B = 내리기"
                    : "물 클릭 = 낚싯대 꺼내기 · W = 출발 · 물가에서 B = 내리기",
                FishingState.Idle => "WASD 이동 · Shift 달리기 · Space 점프 · 물가에서 B = 배 · 물 클릭 = 낚싯대 꺼내기",
                FishingState.Charging when f.IceReady => "누른 채 살짝 내리기 = 구멍 뚫고 채비 내리기 · R / 이동: 낚싯대 넣기",
                FishingState.Charging => "아래로 내려 들고 → 위로 쭉 올려 던지기 · 물 클릭 = 방향 바꾸기 · R / 이동: 낚싯대 넣기",
                FishingState.Waiting => $"{f.CastCellLabel}   ·   마우스: 낚싯대 움직이기(줄 느슨/팽팽) · 왼쪽 버튼 꾹: 릴 감기 · R / 이동: 찌 회수",
                FishingState.Bite => $"{f.CastCellLabel}",
                _ => ""
            };
            FitPill(_hintPill, _hint);
            if (Game.UI.BlockingPanelOpen) _hintPill.gameObject.SetActive(false);

            var npc = Game.UI.NearbyNpc;
            bool nearShop = Vector3.Distance(playerPos, FieldMap.ShopPos) < FieldMap.ShopRadius;
            if (Game.UI.BlockingPanelOpen) { _prompt.text = ""; _promptPill.gameObject.SetActive(false); return; }
            _prompt.text = npc != null ? $"<color=#ffd060>[F]</color> {npc.Look.Name}{GameUI.Josa(npc.Look.Name, "과", "와")} 대화" + PromptQuestTag(npc)
                : Game.UI.NearbyKiosk != null ? "<color=#ffd060>[F]</color> 납품소 · 살림망 고기 납품하기"
                : nearShop && f.State == FishingState.Idle ? $"{FieldMap.ShopName}  [F12] 상점 이용" : "";
            if (Game.Profile.StaminaTier > 0 && f.State == FishingState.Idle)
                _prompt.text = "지쳤습니다... 살림망(Z)에서 요리하거나 음식을 드세요 (가방 I)";
            if (f.State == FishingState.Bite && f.BigBite)
                _prompt.text = "<color=#ff7090>♥ 두근두근 ♥</color>";
            FitPill(_promptPill, _prompt, 48f);
        }

        void UpdateToasts()
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var (root, group, until) = _toasts[i];
                float left = until - Time.time;
                if (left <= 0f)
                {
                    Object.Destroy(root.gameObject);
                    _toasts.RemoveAt(i);
                    continue;
                }
                group.alpha = Mathf.Clamp01(left / 0.6f);
            }
            // 알림은 늘 창보다 위에 (창을 열면 창이 맨 뒤 형제로 올라와 알림을 덮었다)
            if (_toastRoot.parent != null && _toastRoot.GetSiblingIndex() != _toastRoot.parent.childCount - 1) _toastRoot.SetAsLastSibling();
            // 자리: 결과 화면 = 오른쪽 위(대물 배지·물고기와 안 겹치게), 창이 열림 = 맨 위 가운데, 파이팅 = 조금 아래
            bool result = Game.UI.Result != null && Game.UI.Result.IsOpen;
            bool fight = Game.Fishing.State == FishingState.Fighting || Game.Fishing.Cheering;
            float ax = fight && !result ? 1f : 0.5f;
            _toastRoot.anchorMin = _toastRoot.anchorMax = _toastRoot.pivot = new Vector2(ax, 1f);
            _toastRoot.anchoredPosition = result ? new Vector2(0, -66)
                : fight ? new Vector2(-30, -200)
                : Game.UI.BlockingPanelOpen ? new Vector2(0, -16)
                : Game.Fishing.State == FishingState.Charging ? new Vector2(0, -350) // 조준 업적 3줄 아래
                : new Vector2(0, -284); // 캐스팅 비거리 숫자(위 180~260) 아래
            // 최근 것이 맨 위, 아래로 쌓는다 (두 줄 알림은 그만큼 더 내려간다).
            // 창이 열려 있으면 최신 하나만 화면 맨 위 가장자리에 (여러 개를 쌓으면 창 제목·탭을 덮는다)
            bool panelOpen = !result && !fight && Game.UI.BlockingPanelOpen;
            if (panelOpen) _toastRoot.anchoredPosition = new Vector2(0, -6);
            float y = 0f;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var rt = _toasts[i].root;
                if (panelOpen && i < _toasts.Count - 1) { _toasts[i].group.alpha = 0f; continue; }
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, 1f);
                rt.anchoredPosition = new Vector2(0, -y);
                y += rt.sizeDelta.y + 8f;
            }
        }
    }
}
