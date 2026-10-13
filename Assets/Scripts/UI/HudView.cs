using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
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

        Text _levelText, _staminaText, _expText, _goldText, _baitName, _baitCount;
        Image _staminaFill, _expFill;
        Text _channelText, _clockText, _locationText, _areaText;
        Text _questTitle, _questGoal;
        Text _hint, _prompt;
        Text _netSlot;
        readonly List<(Image bg, Text label, BaitDef bait)> _baitSlots = new List<(Image, Text, BaitDef)>();

        RectTransform _mapRoot;
        RectTransform _playerArrow, _bobberDot;
        readonly List<RectTransform> _schoolMarks = new List<RectTransform>();

        // 캐스팅
        Text _castDistance;
        float _castShownUntil;
        float _castShownValue;
        RectTransform _castTip;
        Text _tipTitle, _tipText;
        // 챔질
        RectTransform _hookGauge;
        RectTransform _gaugeBobber;
        readonly Text[] _gradeLabels = new Text[5];
        RectTransform _shakeTip;
        Text _gradePopup;
        RectTransform _biteAlert;

        readonly List<(Text text, float until)> _toasts = new List<(Text, float)>();
        RectTransform _toastRoot;

        const float MapScale = 0.95f;
        const float GaugeHeight = 300f;

        public GameObject Root => _root.gameObject;

        public HudView(Transform canvas)
        {
            _root = UIFactory.Node("Hud", canvas).Fill();
            BuildPlayerPanel();
            BuildClockAndMap();
            BuildBottomBar();
            BuildCasting();
            BuildHookGauge();
            BuildCenter();
            _toastRoot = UIFactory.Node("Toasts", _root).At(new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(1000, 200));
            Game.Toasted += Toast;
        }

        // ---------------- 빌드 ----------------

        void BuildPlayerPanel()
        {
            var panel = UIFactory.Panel(_root, "PlayerPanel", new Color(0.03f, 0.05f, 0.07f, 0.85f)).rectTransform
                .At(new Vector2(0, 1), new Vector2(10, -8), new Vector2(380, 92));
            var portrait = UIFactory.Image(panel, new Color(0.25f, 0.35f, 0.45f), null, "Portrait").rectTransform
                .At(new Vector2(0, 1), new Vector2(6, -6), new Vector2(80, 80));
            UIFactory.Image(portrait, new Color(0.98f, 0.82f, 0.7f), UIFactory.Circle, "Face").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(50, 56));
            UIFactory.Image(portrait, new Color(0.92f, 0.85f, 0.65f), null, "Hat").rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(64, 14));
            UIFactory.Image(portrait, new Color(0.92f, 0.85f, 0.65f), null, "HatTop").rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(34, 12));

            _levelText = UIFactory.Text(panel, "", 18, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _levelText.rectTransform.At(new Vector2(0, 1), new Vector2(96, -6), new Vector2(280, 24));

            UIFactory.Text(panel, "HP", 14, new Color(0.5f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(96, -32), new Vector2(40, 18));
            _staminaFill = UIFactory.Bar(panel, new Color(0, 0, 0, 0.6f), new Color(0.4f, 0.8f, 1f), "HP");
            ((RectTransform)_staminaFill.transform.parent).At(new Vector2(0, 1), new Vector2(132, -36), new Vector2(170, 10));
            _staminaText = UIFactory.Text(panel, "", 14, UIFactory.TextMain, TextAnchor.MiddleRight);
            _staminaText.rectTransform.At(new Vector2(0, 1), new Vector2(304, -32), new Vector2(68, 18));

            UIFactory.Text(panel, "EXP", 14, new Color(0.55f, 0.95f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(96, -52), new Vector2(40, 18));
            _expFill = UIFactory.Bar(panel, new Color(0, 0, 0, 0.6f), new Color(0.5f, 0.9f, 0.4f), "Exp");
            ((RectTransform)_expFill.transform.parent).At(new Vector2(0, 1), new Vector2(132, -56), new Vector2(170, 10));
            _expText = UIFactory.Text(panel, "", 14, UIFactory.TextMain, TextAnchor.MiddleRight);
            _expText.rectTransform.At(new Vector2(0, 1), new Vector2(304, -52), new Vector2(68, 18));

            _goldText = UIFactory.Text(panel, "", 14, UIFactory.Gold);
            _goldText.rectTransform.At(new Vector2(0, 1), new Vector2(96, -70), new Vector2(280, 20));

            // 장착 미끼 (원작: 초상화 아래 물고기 아이콘 칸)
            var bait = UIFactory.Panel(_root, "BaitBox", new Color(0.05f, 0.12f, 0.16f, 0.9f)).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -106), new Vector2(150, 40));
            UIFactory.Image(bait, UIFactory.Accent, null, "Border").rectTransform.At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(3, 40));
            _baitName = UIFactory.Text(bait, "", 15, UIFactory.TextMain);
            _baitName.rectTransform.At(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(90, 30));
            _baitCount = UIFactory.Text(bait, "", 16, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            _baitCount.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(60, 30));
        }

        void BuildClockAndMap()
        {
            var clock = UIFactory.Panel(_root, "Clock", new Color(0.03f, 0.05f, 0.07f, 0.88f)).rectTransform
                .At(new Vector2(1, 1), new Vector2(-10, -8), new Vector2(340, 62));
            _channelText = UIFactory.Text(clock, "", 14, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _channelText.rectTransform.At(new Vector2(0, 1), new Vector2(10, -4), new Vector2(320, 20));
            _clockText = UIFactory.Text(clock, "", 22, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _clockText.rectTransform.At(new Vector2(0, 1), new Vector2(10, -26), new Vector2(320, 30));

            var frame = UIFactory.Panel(_root, "Minimap", new Color(0.03f, 0.05f, 0.07f, 0.88f)).rectTransform
                .At(new Vector2(1, 1), new Vector2(-10, -74), new Vector2(340, 258));
            UIFactory.Text(frame, "MINIMAP", 13, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(10, -2), new Vector2(120, 20));
            _locationText = UIFactory.Text(frame, "", 13, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleRight, FontStyle.Bold);
            _locationText.rectTransform.At(new Vector2(1, 1), new Vector2(-10, -2), new Vector2(200, 20));

            _mapRoot = UIFactory.Node("Map", frame).At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(320, 212));
            _mapRoot.gameObject.AddComponent<RectMask2D>();
            UIFactory.Image(_mapRoot, new Color(0.24f, 0.3f, 0.24f), null, "Land").rectTransform.Fill();
            UIFactory.Image(_mapRoot, new Color(0.14f, 0.25f, 0.34f), null, "Water").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(0f, 0f), new Vector2(320, 120 * MapScale));

            for (int c = 0; c <= HanRiverMap.Cols; c++)
            {
                float x = HanRiverMap.GridMinX + c * HanRiverMap.CellW;
                UIFactory.Image(_mapRoot, new Color(1, 1, 1, 0.14f), null, "GridV").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(x, 0f), new Vector2(1, 120 * MapScale));
                if (c < HanRiverMap.Cols)
                    UIFactory.Text(_mapRoot, ((char)('A' + c)).ToString(), 12, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter).rectTransform
                        .At(new Vector2(0.5f, 0.5f), MapPos(x + 25f, HanRiverMap.RiverMaxZ - 7f), new Vector2(30, 16));
            }
            for (int r = 0; r <= HanRiverMap.Rows; r++)
            {
                float z = HanRiverMap.RiverMinZ + r * HanRiverMap.CellH;
                UIFactory.Image(_mapRoot, new Color(1, 1, 1, 0.14f), null, "GridH").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(0f, z), new Vector2(300 * MapScale, 1));
                if (r < HanRiverMap.Rows)
                    UIFactory.Text(_mapRoot, (r + 1).ToString(), 12, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter).rectTransform
                        .At(new Vector2(0.5f, 0.5f), MapPos(-155f, z + 15f), new Vector2(16, 16));
            }
            UIFactory.Image(_mapRoot, new Color(0.3f, 0.55f, 0.25f, 0.6f), null, "WeedArea").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(102f, -49f), new Vector2(98 * MapScale, 20 * MapScale));
            UIFactory.Image(_mapRoot, new Color(0.8f, 0.8f, 0.78f), null, "Bridge").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(HanRiverMap.BridgeX, 0f), new Vector2(6, 220));
            var pier = HanRiverMap.Pier;
            UIFactory.Image(_mapRoot, new Color(0.7f, 0.5f, 0.3f), null, "Pier").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(pier.center.x, pier.center.y), new Vector2(4, pier.height * MapScale));
            var shop = UIFactory.Image(_mapRoot, UIFactory.Gold, UIFactory.Circle, "Shop").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(HanRiverMap.ShopPos.x, HanRiverMap.ShopPos.z), new Vector2(9, 9));
            UIFactory.Text(shop, "낚시점", 11, UIFactory.Gold, TextAnchor.MiddleLeft).rectTransform.At(new Vector2(1, 0.5f), new Vector2(3, 0), new Vector2(50, 14), new Vector2(0, 0.5f));

            _bobberDot = UIFactory.Image(_mapRoot, new Color(1f, 0.3f, 0.2f), UIFactory.Circle, "Bobber").rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
            _playerArrow = UIFactory.Text(_mapRoot, "▲", 16, new Color(0.45f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, "Player").rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));

            // QUEST LIST
            var quest = UIFactory.Panel(_root, "QuestList", new Color(0.03f, 0.05f, 0.07f, 0.88f)).rectTransform
                .At(new Vector2(1, 1), new Vector2(-10, -336), new Vector2(340, 104));
            UIFactory.Text(quest, "QUEST LIST", 13, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(10, -2), new Vector2(200, 20));
            _areaText = UIFactory.Text(quest, "", 12, UIFactory.TextDim, TextAnchor.MiddleRight);
            _areaText.rectTransform.At(new Vector2(1, 1), new Vector2(-10, -2), new Vector2(200, 20));
            _questTitle = UIFactory.Text(quest, "", 16, new Color(1f, 0.65f, 0.25f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _questTitle.rectTransform.At(new Vector2(0, 1), new Vector2(10, -26), new Vector2(320, 24));
            _questGoal = UIFactory.Text(quest, "", 15, UIFactory.TextMain, TextAnchor.UpperLeft);
            _questGoal.rectTransform.At(new Vector2(0, 1), new Vector2(18, -52), new Vector2(312, 46));
        }

        static Vector2 MapPos(float x, float z) => new Vector2(x * MapScale, z * MapScale);

        void BuildBottomBar()
        {
            var bar = UIFactory.Image(_root, new Color(0.02f, 0.03f, 0.04f, 0.92f), null, "MenuBar");
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 46);
            bar.raycastTarget = true;

            (string key, string label, System.Action action)[] menu =
            {
                ("L", "퀘스트", () => Game.UI.OpenExclusive(Game.UI.Quest)),
                ("P", "도감", () => Game.UI.OpenExclusive(Game.UI.Dex)),
                ("Z", "살림망", () => Game.UI.OpenExclusive(Game.UI.KeepNet)),
                ("F12", "상점", () => Game.UI.OpenShop()),
                ("U", "설명", () => Game.UI.ToggleTips()),
                ("F1", "도움말", () => Game.UI.OpenExclusive(Game.UI.Help)),
                ("F5", "저장", () => Game.UI.SaveNow()),
            };
            for (int i = 0; i < menu.Length; i++)
            {
                var (key, label, action) = menu[i];
                var btn = UIFactory.Button(rt, "", () => action(), new Color(0.1f, 0.13f, 0.16f, 1f), 15);
                ((RectTransform)btn.transform).At(new Vector2(0, 0.5f), new Vector2(8 + i * 98, 0), new Vector2(94, 38));
                btn.GetComponentInChildren<Text>().text = $"<size=11><color=#80c8ff>{key}</color></size>\n{label}";
            }

            // 미끼 퀵슬롯 (숫자키 1~6)
            for (int i = 0; i < GearDatabase.Baits.Count; i++)
            {
                var bait = GearDatabase.Baits[i];
                int index = i;
                var btn = UIFactory.Button(rt, "", () => SelectBait(index), new Color(0.1f, 0.13f, 0.16f, 1f), 13);
                ((RectTransform)btn.transform).At(new Vector2(1, 0.5f), new Vector2(-210 - (GearDatabase.Baits.Count - 1 - i) * 82, 0), new Vector2(78, 40));
                var label = btn.GetComponentInChildren<Text>();
                _baitSlots.Add((btn.GetComponent<Image>(), label, bait));
            }
            var net = UIFactory.Button(rt, "", () => Game.UI.OpenExclusive(Game.UI.KeepNet), new Color(0.1f, 0.13f, 0.16f, 1f), 14);
            ((RectTransform)net.transform).At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(110, 40));
            _netSlot = net.GetComponentInChildren<Text>();
        }

        public void SelectBait(int index)
        {
            if (index < 0 || index >= GearDatabase.Baits.Count) return;
            if (Game.Fishing.State != FishingState.Idle)
            {
                Game.Toast("낚시 중에는 미끼를 바꿀 수 없습니다.", ToastKind.Warn);
                return;
            }
            var bait = GearDatabase.Baits[index];
            Game.Profile.SelectBait(bait.Id);
            Game.Toast($"미끼 변경: {bait.Name}");
        }

        void BuildCasting()
        {
            _castDistance = UIFactory.Text(_root, "", 110, new Color(1, 1, 1, 0.92f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _castDistance.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(800, 140));
            var outline = _castDistance.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(3, -3);

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
            // 찌 클로즈업: 물(하늘색) 위로 찌톱이 떠 있고, 잠길수록 등급이 오른다
            _hookGauge = UIFactory.Node("HookGauge", _root).At(new Vector2(0.5f, 0.5f), new Vector2(330, 20), new Vector2(320, GaugeHeight + 40));
            var water = UIFactory.Image(_hookGauge, new Color(0.25f, 0.75f, 0.95f, 0.45f), null, "Water");
            water.rectTransform.At(new Vector2(0, 0), new Vector2(0, 0), new Vector2(200, GaugeHeight * 0.5f));
            UIFactory.Image(_hookGauge, new Color(0.7f, 0.95f, 1f, 0.9f), null, "Surface").rectTransform
                .At(new Vector2(0, 0), new Vector2(0, GaugeHeight * 0.5f), new Vector2(200, 3));

            _gaugeBobber = UIFactory.Node("Bobber", _hookGauge).At(new Vector2(0, 0), new Vector2(100, GaugeHeight * 0.5f), new Vector2(14, 160), new Vector2(0.5f, 0f));
            Color[] stripes = { new Color(0.1f, 0.1f, 0.1f), new Color(0.2f, 0.75f, 0.3f), new Color(1f, 0.85f, 0.1f), new Color(0.95f, 0.25f, 0.2f), new Color(0.2f, 0.75f, 0.3f), new Color(1f, 0.85f, 0.1f), new Color(0.95f, 0.25f, 0.2f) };
            for (int i = 0; i < stripes.Length; i++)
                UIFactory.Image(_gaugeBobber, stripes[i], null, "Stripe").rectTransform
                    .At(new Vector2(0.5f, 0), new Vector2(0, i * 160f / stripes.Length), new Vector2(10, 160f / stripes.Length - 2));

            string[] names = { "BAD", "SO SO", "GOOD", "GREAT", "PERFECT" };
            for (int i = 0; i < names.Length; i++)
            {
                var t = UIFactory.Text(_hookGauge, names[i], 16 + i * 2, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
                t.rectTransform.At(new Vector2(0, 0), new Vector2(210, GaugeHeight * 0.5f + 140 - i * 36), new Vector2(120, 30));
                _gradeLabels[i] = t;
            }

            _shakeTip = UIFactory.Node("ShakeTip", _root).At(new Vector2(0.5f, 0.5f), new Vector2(250, -230), new Vector2(260, 120));
            UIFactory.Image(_shakeTip, new Color(0.3f, 0.85f, 1f, 0.9f), UIFactory.Arrow, "L").rectTransform
                .At(new Vector2(0.5f, 0.5f), new Vector2(-80, 0), new Vector2(60, 34)).localScale = new Vector3(-1, 1, 1);
            UIFactory.Image(_shakeTip, Color.white, UIFactory.Mouse(true), "Mouse").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 105));
            UIFactory.Image(_shakeTip, new Color(0.3f, 0.85f, 1f, 0.9f), UIFactory.Arrow, "R").rectTransform
                .At(new Vector2(0.5f, 0.5f), new Vector2(80, 0), new Vector2(60, 34));

            _gradePopup = UIFactory.Text(_root, "", 80, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _gradePopup.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 120));
            var outline = _gradePopup.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.1f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3, -3);

            // 본신 경보: 낚싯대 끝 근처의 빨간 느낌표
            _biteAlert = UIFactory.Node("BiteAlert", _root).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            UIFactory.Image(_biteAlert, new Color(1f, 0.2f, 0.15f, 0.35f), UIFactory.Ring, "Rays").rectTransform.Fill(-20, -20, -20, -20);
            UIFactory.Image(_biteAlert, new Color(0.9f, 0.1f, 0.08f, 0.95f), UIFactory.Circle, "Dot").rectTransform.Fill(14, 14, 14, 14);
            UIFactory.Text(_biteAlert, "!", 48, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
        }

        void BuildCenter()
        {
            _hint = UIFactory.Text(_root, "", 18, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _hint.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(1200, 28));
            _prompt = UIFactory.Text(_root, "", 22, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(800, 32));
        }

        // ---------------- 이벤트 ----------------

        public void OnCasted(float distance)
        {
            _castShownValue = distance;
            _castShownUntil = Time.time + 1.6f;
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
            var t = UIFactory.Text(_toastRoot, message, kind == ToastKind.Great ? 24 : 20, color, TextAnchor.MiddleCenter,
                kind == ToastKind.Great ? FontStyle.Bold : FontStyle.Normal);
            t.rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(1000, 32));
            _toasts.Add((t, Time.time + (kind == ToastKind.Great ? 5f : 3.2f)));
            while (_toasts.Count > 5)
            {
                Object.Destroy(_toasts[0].text.gameObject);
                _toasts.RemoveAt(0);
            }
        }

        // ---------------- 갱신 ----------------

        public void Update()
        {
            var p = Game.Profile;
            var d = p.Data;
            var fishing = Game.Fishing;
            bool fighting = fishing.State == FishingState.Fighting;

            // 파이팅 중에는 필드 HUD를 숨기고 토스트만 남긴다
            foreach (Transform child in _root)
                if (child != _toastRoot && child != _gradePopup.transform) child.gameObject.SetActive(!fighting);
            _toastRoot.anchoredPosition = new Vector2(0, fighting ? -330 : -230);
            UpdateToasts();
            if (fighting)
            {
                UpdateGradePopup();
                return;
            }

            _levelText.text = $"Lv. {d.level}    강태공";
            _staminaFill.fillAmount = d.stamina / PlayerProfile.MaxStamina;
            _staminaFill.color = d.stamina < 30f ? UIFactory.Danger : new Color(0.4f, 0.8f, 1f);
            _staminaText.text = $"{d.stamina:0.0} %";
            bool maxLevel = d.level >= PlayerProfile.MaxLevel;
            _expFill.fillAmount = maxLevel ? 1f : d.exp / (float)PlayerProfile.ExpToNext(d.level);
            _expText.text = maxLevel ? "MAX" : $"{_expFill.fillAmount * 100f:0} %";
            _goldText.text = $"{d.gold:N0} G   ·   총 {d.totalCatches}마리";
            _baitName.text = p.Bait.Name;
            _baitCount.text = $"x{p.Count(p.Bait.Id)}";

            var clock = Game.Clock;
            int month = 4 + (clock.Day - 1) / 30, day = 1 + (clock.Day - 1) % 30;
            int h12 = clock.Hour % 12 == 0 ? 12 : clock.Hour % 12;
            string ampm = clock.Hour < 12 ? "AM" : "PM";
            string sky = clock.Band == TimeBand.Night || clock.Band == TimeBand.Dawn ? "<color=#c8d8ff>●</color>" : "<color=#ffd060>●</color>";
            _channelText.text = $"No. 001  한강 CH.1    ·    {Names.Of(Game.Weather.Current)}";
            _clockText.text = $"{sky}  {month:00}/{day:00}  <size=14>{ampm}</size> {h12:00}:{clock.Minute:00}";

            var playerPos = Game.Player.transform.position;
            var lookPos = fishing.State == FishingState.Waiting || fishing.State == FishingState.Bite ? fishing.BobberPosition : playerPos;
            _locationText.text = HanRiverMap.TryCell(lookPos, out int col, out int row) ? $"현재 위치 : {HanRiverMap.CellName(col, row)}" : "현재 위치 : 둔치";
            _areaText.text = HanRiverMap.AreaName(playerPos);

            var quest = Game.Quests.Current;
            _questTitle.text = quest != null ? $"① {quest.Title}" : "모든 조합 퀘스트 완료";
            _questGoal.text = quest != null ? $"{quest.GoalText}  {Mathf.Min(Game.Quests.Progress, quest.Count)} / {quest.Count}" : "자유롭게 대물을 노려보세요!";

            for (int i = 0; i < _baitSlots.Count; i++)
            {
                var (bg, label, bait) = _baitSlots[i];
                bool selected = d.baitId == bait.Id;
                int count = p.Count(bait.Id);
                label.text = $"<size=10><color=#80c8ff>{i + 1}</color></size> {bait.Name}\n<b>x{count}</b>";
                label.color = count > 0 ? UIFactory.TextMain : UIFactory.TextDim;
                bg.color = selected ? new Color(0.12f, 0.4f, 0.45f, 1f) : new Color(0.1f, 0.13f, 0.16f, 1f);
            }
            _netSlot.text = $"<size=11><color=#80c8ff>Z</color></size> 살림망\n<b>{d.keepNet.Count}/{PlayerProfile.KeepNetCapacity}</b>";

            UpdateMap(playerPos);
            UpdateCasting();
            UpdateHookGauge();
            UpdateCenter(playerPos);
        }

        void UpdateMap(Vector3 playerPos)
        {
            _playerArrow.anchoredPosition = MapPos(playerPos.x, playerPos.z);
            _playerArrow.localRotation = Quaternion.Euler(0, 0, -Game.Player.Yaw);

            var state = Game.Fishing.State;
            bool showBobber = state == FishingState.Waiting || state == FishingState.Bite || state == FishingState.Casting;
            _bobberDot.gameObject.SetActive(showBobber);
            if (showBobber) _bobberDot.anchoredPosition = MapPos(Game.Fishing.BobberPosition.x, Game.Fishing.BobberPosition.z);

            var schools = Game.Schools.Schools;
            while (_schoolMarks.Count < schools.Count)
            {
                var mark = UIFactory.Text(_mapRoot, "◆", 13, new Color(1f, 0.75f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold, "School").rectTransform
                    .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
                _schoolMarks.Add(mark);
            }
            for (int i = 0; i < schools.Count; i++)
            {
                var c = HanRiverMap.CellCenter(schools[i].Col, schools[i].Row);
                _schoolMarks[i].anchoredPosition = MapPos(c.x, c.z) + new Vector2(Mathf.Sin(Time.time * 1.5f + i) * 3f, 0f);
            }
        }

        void UpdateCasting()
        {
            var f = Game.Fishing;
            bool tips = Game.Profile.Data.showTips;
            bool charging = f.State == FishingState.Charging;
            bool showNumber = charging || Time.time < _castShownUntil;
            _castDistance.gameObject.SetActive(showNumber);
            if (charging)
            {
                _castDistance.text = $"{f.CastDistance:0.00}m";
                _castDistance.color = f.CastTargetIsWater ? new Color(1, 1, 1, 0.92f) : new Color(1f, 0.5f, 0.45f, 0.92f);
            }
            else if (showNumber)
            {
                _castDistance.text = $"{_castShownValue:0.00}m";
                var c = Color.white;
                c.a = Mathf.Clamp01((_castShownUntil - Time.time) / 0.6f) * 0.92f;
                _castDistance.color = c;
            }
            _castTip.gameObject.SetActive(charging && tips);
            if (charging) _castTip.anchoredPosition = new Vector2(0, 120 + Mathf.Repeat(Time.time * 1.6f, 1f) * 40f);
        }

        void UpdateHookGauge()
        {
            var f = Game.Fishing;
            bool tips = Game.Profile.Data.showTips;
            bool waiting = f.State == FishingState.Waiting || f.State == FishingState.Bite;
            _hookGauge.gameObject.SetActive(waiting);
            _shakeTip.gameObject.SetActive(f.State == FishingState.Bite && tips);
            if (waiting)
            {
                float sink = f.BobberSink;
                float bob = Mathf.Sin(Time.time * 2.1f) * 4f;
                _gaugeBobber.anchoredPosition = new Vector2(100, GaugeHeight * 0.5f - sink * 160f + bob);
                int current = f.State != FishingState.Bite ? -1 : sink < 0.2f ? 0 : sink < 0.45f ? 1 : sink < 0.7f ? 2 : sink < 0.97f ? 3 : 4;
                for (int i = 0; i < _gradeLabels.Length; i++)
                {
                    bool on = i == current;
                    _gradeLabels[i].color = on ? (i == 4 ? UIFactory.Gold : Color.white) : new Color(1, 1, 1, 0.35f);
                    _gradeLabels[i].rectTransform.localScale = Vector3.one * (on ? 1.25f : 1f);
                }
            }

            UpdateGradePopup();

            // 본신이 오면 낚싯대 끝에 빨간 느낌표
            bool bite = f.State == FishingState.Bite;
            _biteAlert.gameObject.SetActive(bite);
            if (bite)
            {
                var screen = Game.CameraRig.Cam.WorldToScreenPoint(Game.Player.RodTip.position + Vector3.up * 0.6f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
                _biteAlert.anchoredPosition = local;
                _biteAlert.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.2f);
            }

            UpdateTips();
        }

        void UpdateGradePopup()
        {
            var f = Game.Fishing;
            float age = Time.time - f.LastGradeAt;
            bool showGrade = age < 1.1f && f.LastGrade != HookGrade.None;
            _gradePopup.gameObject.SetActive(showGrade);
            if (showGrade)
            {
                _gradePopup.text = f.LastGrade switch
                {
                    HookGrade.Bad => "BAD",
                    HookGrade.SoSo => "SO SO",
                    HookGrade.Good => "GOOD",
                    HookGrade.Great => "GREAT!",
                    _ => "PERFECT!!"
                };
                _gradePopup.color = f.LastGrade switch
                {
                    HookGrade.Bad => new Color(0.7f, 0.7f, 0.75f, 1f - age),
                    HookGrade.Perfect => new Color(1f, 0.85f, 0.3f, 1f - age * 0.5f),
                    _ => new Color(0.6f, 0.9f, 1f, 1f - age * 0.7f)
                };
                _gradePopup.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.2f - age) * 2f);
            }
        }

        void UpdateTips()
        {
            var f = Game.Fishing;
            bool tips = Game.Profile.Data.showTips;
            // 안내문 (원작 문구)
            string tip = f.State switch
            {
                FishingState.Charging => "왼쪽 버튼을 누르고 당긴 다음 빠르게 밀면서 버튼을 놓으면 캐스팅 됩니다.",
                FishingState.Waiting => "왼쪽 버튼을 누른 상태로 좌우로 흔들어 챔질하세요. 찌가 깔짝이는 건 예신이니 조심!",
                FishingState.Bite => "왼쪽 버튼을 누른 상태로 좌우로 흔들어 챔질하세요. 물에 완전히 잠겼을 때 흔들면 퍼펙트",
                FishingState.Idle => "왼쪽 버튼을 누르고 당긴 다음 빠르게 밀면서 버튼을 놓으면 캐스팅 됩니다.",
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
                FishingState.Idle => "WASD 이동 · Shift 달리기 · 오른쪽 드래그 시점 · 왼쪽 버튼 당겼다 밀기 = 캐스팅",
                FishingState.Waiting => $"{f.CastCellLabel}   ·   R / 이동: 찌 회수",
                FishingState.Bite => $"{f.CastCellLabel}",
                _ => ""
            };

            bool nearShop = Vector3.Distance(playerPos, HanRiverMap.ShopPos) < HanRiverMap.ShopRadius;
            _prompt.text = nearShop && f.State == FishingState.Idle ? "반포 낚시점  [F12] 상점 이용" : "";
            if (Game.Profile.Data.stamina < 30f && f.State == FishingState.Idle)
                _prompt.text = "지쳤습니다... 살림망(Z)에서 요리하거나 김밥을 드세요";
            if (f.State == FishingState.Bite && f.BigBite)
                _prompt.text = "<color=#ff7090>♥ 두근두근 ♥</color>";
        }

        void UpdateToasts()
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var (text, until) = _toasts[i];
                float left = until - Time.time;
                if (left <= 0f)
                {
                    Object.Destroy(text.gameObject);
                    _toasts.RemoveAt(i);
                    continue;
                }
                var c = text.color;
                c.a = Mathf.Clamp01(left / 0.6f);
                text.color = c;
            }
            for (int i = 0; i < _toasts.Count; i++)
                _toasts[i].text.rectTransform.anchoredPosition = new Vector2(0, -(_toasts.Count - 1 - i) * 34f);
        }
    }
}
