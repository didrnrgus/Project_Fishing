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
        Text _nameText;
        // 배 계기판
        RectTransform _boatGauges;
        Image _speedFill, _fuelFill;
        Text _speedText, _fuelText, _boatHint;
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
            BuildBoatGauges();
            _toastRoot = UIFactory.Node("Toasts", _root).At(new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(1000, 200));
            Game.Toasted += Toast;
        }

        // ---------------- 빌드 ----------------

        void BuildPlayerPanel()
        {
            var panel = UIFactory.Panel(_root, "PlayerPanel", new Color(0.03f, 0.05f, 0.07f, 0.88f)).rectTransform
                .At(new Vector2(0, 1), new Vector2(10, -8), new Vector2(430, 122));
            UIFactory.Image(panel, UIFactory.Accent, null, "TopLine").rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(430, 3));

            // 초상화: 내 캐릭터를 비추는 카메라의 렌더 텍스처 (PortraitCamera)
            var frame = UIFactory.Image(panel, new Color(0.55f, 0.75f, 0.8f, 0.9f), null, "PortraitFrame").rectTransform
                .At(new Vector2(0, 1), new Vector2(8, -10), new Vector2(106, 106));
            var portrait = UIFactory.Node("Portrait", frame).Fill(3, 3, 3, 3);
            var raw = portrait.gameObject.AddComponent<RawImage>();
            raw.texture = PortraitCamera.Texture;
            raw.raycastTarget = false;
            var lvBadge = UIFactory.Image(frame, new Color(0.05f, 0.08f, 0.1f, 0.92f), null, "LevelBadge").rectTransform
                .At(new Vector2(0, 0), new Vector2(0, 0), new Vector2(54, 24), new Vector2(0, 0));
            _levelText = UIFactory.Text(lvBadge, "", 16, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _levelText.rectTransform.Fill();

            var look = Fishing.Characters.CharacterLook.Player;
            _nameText = UIFactory.Text(panel, $"{look.Name}  <size=13><color=#7fd8ff>{look.Title}</color></size>", 20, UIFactory.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            _nameText.rectTransform.At(new Vector2(0, 1), new Vector2(126, -10), new Vector2(290, 26));

            UIFactory.Text(panel, "HP", 13, new Color(0.5f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(126, -42), new Vector2(40, 18));
            _staminaFill = UIFactory.Bar(panel, new Color(0, 0, 0, 0.6f), new Color(0.4f, 0.8f, 1f), "HP");
            ((RectTransform)_staminaFill.transform.parent).At(new Vector2(0, 1), new Vector2(160, -45), new Vector2(190, 12));
            _staminaText = UIFactory.Text(panel, "", 13, UIFactory.TextMain, TextAnchor.MiddleRight);
            _staminaText.rectTransform.At(new Vector2(0, 1), new Vector2(352, -42), new Vector2(68, 18));

            UIFactory.Text(panel, "EXP", 13, new Color(0.55f, 0.95f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(126, -64), new Vector2(40, 18));
            _expFill = UIFactory.Bar(panel, new Color(0, 0, 0, 0.6f), new Color(0.5f, 0.9f, 0.4f), "Exp");
            ((RectTransform)_expFill.transform.parent).At(new Vector2(0, 1), new Vector2(160, -67), new Vector2(190, 12));
            _expText = UIFactory.Text(panel, "", 13, UIFactory.TextMain, TextAnchor.MiddleRight);
            _expText.rectTransform.At(new Vector2(0, 1), new Vector2(352, -64), new Vector2(68, 18));

            _goldText = UIFactory.Text(panel, "", 14, UIFactory.Gold);
            _goldText.rectTransform.At(new Vector2(0, 1), new Vector2(126, -90), new Vector2(290, 22));

            // 장착 미끼 (원작: 초상화 아래 물고기 아이콘 칸)
            var bait = UIFactory.Panel(_root, "BaitBox", new Color(0.05f, 0.12f, 0.16f, 0.9f)).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -138), new Vector2(150, 40));
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
            var exit = UIFactory.Button(clock, "↩ 조합", () => Game.UI.EnterLobby(), new Color(0.6f, 0.15f, 0.12f, 1f), 13);
            ((RectTransform)exit.transform).At(new Vector2(1, 1), new Vector2(-6, -6), new Vector2(64, 22));
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
            UIFactory.Text(frame, "[M] 지역 지도", 11, new Color(1, 1, 1, 0.55f), TextAnchor.LowerRight).rectTransform
                .At(new Vector2(1, 0), new Vector2(-8, 10), new Vector2(120, 16), new Vector2(1, 0));
            BuildMinimapContent();
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
                ("I", "가방", () => Game.UI.OpenExclusive(Game.UI.Inventory)),
                ("C", "의상", () => Game.UI.OpenExclusive(Game.UI.Costume)),
                ("F12", "상점", () => Game.UI.OpenShop()),
                ("U", "설명", () => Game.UI.ToggleTips()),
                ("F1", "도움말", () => Game.UI.OpenExclusive(Game.UI.Help)),
                ("F5", "저장", () => Game.UI.SaveNow()),
                ("B", "배", () => Game.Boat.Toggle()),
            };
            for (int i = 0; i < menu.Length; i++)
            {
                var (key, label, action) = menu[i];
                var btn = UIFactory.Button(rt, "", () => action(), new Color(0.1f, 0.13f, 0.16f, 1f), 15);
                ((RectTransform)btn.transform).At(new Vector2(0, 0.5f), new Vector2(8 + i * 98, 0), new Vector2(94, 38));
                btn.GetComponentInChildren<Text>().text = $"<size=11><color=#80c8ff>{key}</color></size>\n{label}";
            }

            // 미끼 퀵슬롯 (숫자키 1~6): 지금 낚시터에서 쓰는 미끼
            _baitBar = rt;
            BuildBaitSlots();
            var net = UIFactory.Button(rt, "", () => Game.UI.OpenExclusive(Game.UI.KeepNet), new Color(0.1f, 0.13f, 0.16f, 1f), 14);
            ((RectTransform)net.transform).At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(110, 40));
            _netSlot = net.GetComponentInChildren<Text>();
        }

        /// <summary>배 계기판 (원작: 오른쪽 아래 속도계·연료계).</summary>
        void BuildBoatGauges()
        {
            _boatGauges = UIFactory.Node("BoatGauges", _root).At(new Vector2(1, 0), new Vector2(-20, 70), new Vector2(300, 170), new Vector2(1, 0));
            (_speedFill, _speedText) = Gauge(_boatGauges, new Vector2(-220, 0), "SPEED", new Color(0.35f, 0.85f, 1f));
            (_fuelFill, _fuelText) = Gauge(_boatGauges, new Vector2(-70, 0), "FUEL", new Color(1f, 0.75f, 0.3f));
            _boatHint = UIFactory.Text(_boatGauges, "", 14, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleRight);
            _boatHint.rectTransform.At(new Vector2(1, 1), new Vector2(0, 26), new Vector2(560, 22), new Vector2(1, 1));
            _boatHint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        }

        static (Image fill, Text value) Gauge(RectTransform parent, Vector2 pos, string label, Color color)
        {
            var bg = UIFactory.Image(parent, new Color(0.03f, 0.05f, 0.07f, 0.85f), UIFactory.Circle, label + "Bg").rectTransform
                .At(new Vector2(1, 0), pos, new Vector2(140, 140), new Vector2(0.5f, 0f));
            var track = UIFactory.Image(bg, new Color(1, 1, 1, 0.12f), UIFactory.Ring, "Track");
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
            _fuelFill.fillAmount = Mathf.Clamp01(fuel / def.FuelCapacity) * 0.8f;
            _fuelFill.color = fuel < def.FuelCapacity * 0.2f ? UIFactory.Danger : new Color(1f, 0.75f, 0.3f);
            _fuelText.text = $"{fuel:0.0}<size=13>L</size>";
            _boatHint.text = boat.Seated
                ? $"{def.Name}  ·  W/S 속도  ·  A/D 방향  ·  멈추면 낚시"
                : $"{def.Name}  ·  뱃머리에서 낚시할 수 있습니다  ·  물가에서 B로 내리기";
        }

        RectTransform _baitBar;
        readonly List<GameObject> _baitSlotObjects = new List<GameObject>();
        string _baitsBuiltFor;

        /// <summary>지금 낚시터의 미끼 (최대 6개).</summary>
        public static List<BaitDef> MapBaits()
        {
            // 쓸 수 있는 해체 미끼 중 가장 좋은 것 하나를 마지막 칸에
            BaitDef cut = null;
            foreach (var b in GearDatabase.Baits)
                if (b.NotSold && Game.Profile.Count(b.Id) > 0 && b.Level <= Game.Profile.Data.level && (cut == null || b.Potency > cut.Potency)) cut = b;
            var list = new List<BaitDef>();
            int sold = cut != null ? 5 : 6;
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
            for (int i = 0; i < baits.Count; i++)
            {
                var bait = baits[i];
                int index = i;
                var btn = UIFactory.Button(_baitBar, "", () => SelectBait(index), new Color(0.1f, 0.13f, 0.16f, 1f), 13);
                ((RectTransform)btn.transform).At(new Vector2(1, 0.5f), new Vector2(-210 - (baits.Count - 1 - i) * 82, 0), new Vector2(78, 40));
                var label = btn.GetComponentInChildren<Text>();
                _baitSlots.Add((btn.GetComponent<Image>(), label, bait));
                _baitSlotObjects.Add(btn.gameObject);
            }
        }

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
            // 파이팅 중이거나 대기실이면 필드 HUD를 숨긴다 (토스트는 남긴다)
            bool fighting = fishing.State == FishingState.Fighting || Game.Mode == GameMode.Lobby;

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

            _levelText.text = $"Lv.{d.level}";
            _staminaFill.fillAmount = d.stamina / p.StaminaCap;
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
            _channelText.text = $"No. {Game.Channel:000}  {FieldMap.ShortName} CH.{Game.Channel}    ·    {Names.Of(Game.Weather.Current)}";
            _clockText.text = $"{sky}  {month:00}/{day:00}  <size=14>{ampm}</size> {h12:00}:{clock.Minute:00}";

            var playerPos = Game.Player.transform.position;
            var lookPos = fishing.State == FishingState.Waiting || fishing.State == FishingState.Bite ? fishing.BobberPosition : playerPos;
            _locationText.text = FieldMap.TryCell(lookPos, out int col, out int row) ? $"현재 위치 : {FieldMap.CellName(col, row)}" : "현재 위치 : 둔치";
            _areaText.text = FieldMap.AreaName(playerPos);

            var quest = Game.Quests.Current;
            _questTitle.text = quest != null ? $"① {quest.Title}" : "모든 조합 퀘스트 완료";
            _questGoal.text = quest == null ? "자유롭게 대물을 노려보세요!"
                : Game.Quests.Ready ? $"<color=#70e070>목표 달성! {FieldMap.UnionContact}에게 보고</color>"
                : $"{quest.GoalText}  {Mathf.Min(Game.Quests.Progress, quest.Count)} / {quest.Count}";

            if (Time.frameCount % 20 == 0 && _baitsBuiltFor != BaitKey()) BuildBaitSlots();
            for (int i = 0; i < _baitSlots.Count; i++)
            {
                var (bg, label, bait) = _baitSlots[i];
                bool selected = d.baitId == bait.Id;
                int count = p.Count(bait.Id);
                label.text = $"<size=10><color=#80c8ff>{i + 1}</color></size> {bait.Name}\n<b>x{count}</b>";
                label.color = count > 0 ? UIFactory.TextMain : UIFactory.TextDim;
                bg.color = selected ? new Color(0.12f, 0.4f, 0.45f, 1f) : new Color(0.1f, 0.13f, 0.16f, 1f);
            }
            _netSlot.text = $"<size=11><color=#80c8ff>Z</color></size> 살림망\n<b>{d.keepNet.Count}/{Game.Profile.KeepNetSize}</b>";

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
                var c = FieldMap.CellCenter(schools[i].Col, schools[i].Row);
                _schoolMarks[i].anchoredPosition = MapPos(c.x, c.z) + new Vector2(Mathf.Sin(Time.time * 1.5f + i) * 3f, 0f);
                _schoolMarks[i].localScale = Vector3.one * (schools[i].Special ? 1.6f : schools[i].Big ? 1.15f : 0.65f);
                _schoolMarks[i].gameObject.SetActive(!schools[i].Hidden);
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
                FishingState.Idle when Game.Player.OnBoat => Game.Boat.Seated
                    ? "W/S 속도 · A/D 방향 · 멈추면 뱃머리에서 낚시 · 물가에서 B = 내리기"
                    : "왼쪽 버튼 당겼다 밀기 = 캐스팅 · W = 출발 · 물가에서 B = 내리기",
                FishingState.Idle => "WASD 이동 · Shift 달리기 · Space 점프 · 물가에서 B = 배 · 왼쪽 버튼 당겼다 밀기 = 캐스팅",
                FishingState.Waiting => $"{f.CastCellLabel}   ·   R / 이동: 찌 회수",
                FishingState.Bite => $"{f.CastCellLabel}",
                _ => ""
            };

            var npc = Game.UI.NearbyNpc;
            bool nearShop = Vector3.Distance(playerPos, FieldMap.ShopPos) < FieldMap.ShopRadius;
            if (Game.UI.BlockingPanelOpen) { _prompt.text = ""; return; }
            _prompt.text = npc != null ? $"<color=#ffd060>[F]</color> {npc.Look.Name}{GameUI.Josa(npc.Look.Name, "과", "와")} 대화" + (npc.NpcId == "union" && Game.Quests.Ready ? "  <color=#70e070>· 퀘스트 보고</color>" : "")
                : nearShop && f.State == FishingState.Idle ? $"{FieldMap.ShopName}  [F12] 상점 이용" : "";
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
