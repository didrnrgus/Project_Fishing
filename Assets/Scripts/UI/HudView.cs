using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>필드 HUD: 캐릭터 정보, 시계/날씨, 미니맵, 퀘스트 추적, 미끼 슬롯, 상태 안내, 토스트.</summary>
    public class HudView
    {
        readonly RectTransform _root;

        Text _levelText, _goldText, _staminaText, _expText, _gearText;
        Image _staminaFill, _expFill;
        Text _clockText, _areaText;
        Text _questTitle, _questGoal;
        Text _hint, _prompt;
        RectTransform _chargeRoot;
        Image _chargeFill;
        Text _chargeText;
        Text _biteMark, _heartText;
        readonly List<(Image bg, Text label, BaitDef bait)> _baitSlots = new List<(Image, Text, BaitDef)>();

        RectTransform _mapRoot;
        RectTransform _playerArrow, _bobberDot;
        readonly List<RectTransform> _schoolMarks = new List<RectTransform>();

        readonly List<(Text text, float until)> _toasts = new List<(Text, float)>();
        RectTransform _toastRoot;

        const float MapScale = 1f; // 1px = 1m

        public GameObject Root => _root.gameObject;

        public HudView(Transform canvas)
        {
            _root = UIFactory.Node("Hud", canvas).Fill();
            BuildPlayerPanel();
            BuildClockAndMap();
            BuildQuestTracker();
            BuildBaitBar();
            BuildCenter();
            BuildToasts();
            Game.Toasted += Toast;
        }

        // ---------------- 빌드 ----------------

        void BuildPlayerPanel()
        {
            var panel = UIFactory.Panel(_root, "PlayerPanel").rectTransform.At(new Vector2(0, 1), new Vector2(20, -20), new Vector2(380, 150));
            var portrait = UIFactory.Image(panel, UIFactory.Accent, UIFactory.Circle, "Portrait").rectTransform.At(new Vector2(0, 1), new Vector2(14, -14), new Vector2(72, 72));
            UIFactory.Text(portrait, "강", 34, new Color(0.05f, 0.15f, 0.15f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();

            _levelText = UIFactory.Text(panel, "", 24, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _levelText.rectTransform.At(new Vector2(0, 1), new Vector2(98, -14), new Vector2(270, 30));
            _goldText = UIFactory.Text(panel, "", 19, UIFactory.Gold);
            _goldText.rectTransform.At(new Vector2(0, 1), new Vector2(98, -46), new Vector2(270, 26));

            UIFactory.Text(panel, "체력", 16, UIFactory.TextDim).rectTransform.At(new Vector2(0, 1), new Vector2(14, -94), new Vector2(50, 20));
            _staminaFill = UIFactory.Bar(panel, new Color(0, 0, 0, 0.5f), UIFactory.Good, "Stamina");
            ((RectTransform)_staminaFill.transform.parent).At(new Vector2(0, 1), new Vector2(64, -96), new Vector2(240, 16));
            _staminaText = UIFactory.Text(panel, "", 15, UIFactory.TextMain, TextAnchor.MiddleRight);
            _staminaText.rectTransform.At(new Vector2(0, 1), new Vector2(306, -94), new Vector2(64, 20));

            UIFactory.Text(panel, "EXP", 16, UIFactory.TextDim).rectTransform.At(new Vector2(0, 1), new Vector2(14, -120), new Vector2(50, 20));
            _expFill = UIFactory.Bar(panel, new Color(0, 0, 0, 0.5f), new Color(0.45f, 0.7f, 1f), "Exp");
            ((RectTransform)_expFill.transform.parent).At(new Vector2(0, 1), new Vector2(64, -122), new Vector2(240, 12));
            _expText = UIFactory.Text(panel, "", 15, UIFactory.TextMain, TextAnchor.MiddleRight);
            _expText.rectTransform.At(new Vector2(0, 1), new Vector2(306, -120), new Vector2(64, 20));

            var keys = UIFactory.Text(_root, "[I] 살림망   [B] 도감   [Q] 퀘스트   [P] 낚시점   [F1] 도움말   [F5] 저장", 16, UIFactory.TextDim);
            keys.rectTransform.At(new Vector2(0, 1), new Vector2(22, -176), new Vector2(700, 24));
        }

        void BuildClockAndMap()
        {
            var clock = UIFactory.Panel(_root, "Clock").rectTransform.At(new Vector2(1, 1), new Vector2(-20, -20), new Vector2(340, 74));
            _clockText = UIFactory.Text(clock, "", 22, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _clockText.rectTransform.At(new Vector2(0, 1), new Vector2(14, -8), new Vector2(320, 30));
            _areaText = UIFactory.Text(clock, "", 16, UIFactory.TextDim);
            _areaText.rectTransform.At(new Vector2(0, 1), new Vector2(14, -42), new Vector2(320, 24));

            var frame = UIFactory.Panel(_root, "Minimap").rectTransform.At(new Vector2(1, 1), new Vector2(-20, -100), new Vector2(340, 240));
            _mapRoot = UIFactory.Node("Map", frame).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 220));
            var mask = _mapRoot.gameObject.AddComponent<RectMask2D>();
            mask.enabled = true;

            UIFactory.Image(_mapRoot, new Color(0.3f, 0.42f, 0.26f), null, "Land").rectTransform.Fill();
            var water = UIFactory.Image(_mapRoot, new Color(0.2f, 0.4f, 0.55f), null, "Water").rectTransform;
            water.At(new Vector2(0.5f, 0.5f), MapPos(0f, 0f), new Vector2(320, 120 * MapScale));

            // 격자와 이름
            for (int c = 0; c <= HanRiverMap.Cols; c++)
            {
                float x = HanRiverMap.GridMinX + c * HanRiverMap.CellW;
                UIFactory.Image(_mapRoot, new Color(1, 1, 1, 0.18f), null, "GridV").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(x, 0f), new Vector2(1, 120));
                if (c < HanRiverMap.Cols)
                    UIFactory.Text(_mapRoot, ((char)('A' + c)).ToString(), 13, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter).rectTransform
                        .At(new Vector2(0.5f, 0.5f), MapPos(x + 25f, HanRiverMap.RiverMaxZ - 7f), new Vector2(30, 16));
            }
            for (int r = 0; r <= HanRiverMap.Rows; r++)
            {
                float z = HanRiverMap.RiverMinZ + r * HanRiverMap.CellH;
                UIFactory.Image(_mapRoot, new Color(1, 1, 1, 0.18f), null, "GridH").rectTransform
                    .At(new Vector2(0.5f, 0.5f), MapPos(0f, z), new Vector2(300, 1));
                if (r < HanRiverMap.Rows)
                    UIFactory.Text(_mapRoot, (r + 1).ToString(), 13, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter).rectTransform
                        .At(new Vector2(0.5f, 0.5f), MapPos(-156f, z + 15f), new Vector2(16, 16));
            }

            // 수초 지대 표시
            UIFactory.Image(_mapRoot, new Color(0.3f, 0.6f, 0.25f, 0.6f), null, "WeedArea").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(102f, -49f), new Vector2(98, 20));
            UIFactory.Image(_mapRoot, new Color(0.85f, 0.85f, 0.82f), null, "Bridge").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(HanRiverMap.BridgeX, 0f), new Vector2(6, 220));
            var pier = HanRiverMap.Pier;
            UIFactory.Image(_mapRoot, new Color(0.7f, 0.5f, 0.3f), null, "Pier").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(pier.center.x, pier.center.y), new Vector2(4, pier.height));
            var shop = UIFactory.Image(_mapRoot, UIFactory.Gold, UIFactory.Circle, "Shop").rectTransform
                .At(new Vector2(0.5f, 0.5f), MapPos(HanRiverMap.ShopPos.x, HanRiverMap.ShopPos.z), new Vector2(10, 10));
            UIFactory.Text(shop, "낚시점", 11, UIFactory.Gold, TextAnchor.MiddleLeft).rectTransform.At(new Vector2(1, 0.5f), new Vector2(4, 0), new Vector2(50, 14), new Vector2(0, 0.5f));

            _bobberDot = UIFactory.Image(_mapRoot, new Color(1f, 0.3f, 0.2f), UIFactory.Circle, "Bobber").rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
            _playerArrow = UIFactory.Text(_mapRoot, "▲", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Player").rectTransform
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
        }

        static Vector2 MapPos(float x, float z) => new Vector2(x * MapScale, z * MapScale);

        void BuildQuestTracker()
        {
            var panel = UIFactory.Panel(_root, "QuestTracker").rectTransform.At(new Vector2(1, 1), new Vector2(-20, -352), new Vector2(340, 96));
            UIFactory.Text(panel, "조합 퀘스트", 14, UIFactory.Accent).rectTransform.At(new Vector2(0, 1), new Vector2(14, -6), new Vector2(200, 20));
            _questTitle = UIFactory.Text(panel, "", 19, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            _questTitle.rectTransform.At(new Vector2(0, 1), new Vector2(14, -28), new Vector2(316, 26));
            _questGoal = UIFactory.Text(panel, "", 16, UIFactory.TextDim, TextAnchor.UpperLeft);
            _questGoal.rectTransform.At(new Vector2(0, 1), new Vector2(14, -56), new Vector2(316, 36));
        }

        void BuildBaitBar()
        {
            var bar = UIFactory.Node("BaitBar", _root).At(new Vector2(0, 0), new Vector2(20, 20), new Vector2(6 * 104, 72));
            for (int i = 0; i < GearDatabase.Baits.Count; i++)
            {
                var bait = GearDatabase.Baits[i];
                int index = i;
                var btn = UIFactory.Button(bar, "", () => SelectBait(index), UIFactory.PanelBg);
                var rt = (RectTransform)btn.transform;
                rt.At(new Vector2(0, 0), new Vector2(i * 104, 0), new Vector2(98, 72));
                var label = btn.GetComponentInChildren<Text>();
                label.fontSize = 15;
                _baitSlots.Add((btn.GetComponent<Image>(), label, bait));
            }
            _gearText = UIFactory.Text(_root, "", 16, UIFactory.TextDim);
            _gearText.rectTransform.At(new Vector2(0, 0), new Vector2(22, 98), new Vector2(900, 24));
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

        void BuildCenter()
        {
            _hint = UIFactory.Text(_root, "", 20, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _hint.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1000, 32));

            _prompt = UIFactory.Text(_root, "", 22, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 180), new Vector2(600, 32));

            _chargeRoot = UIFactory.Node("Charge", _root).At(new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(420, 50));
            _chargeFill = UIFactory.Bar(_chargeRoot, new Color(0, 0, 0, 0.6f), UIFactory.Accent, "ChargeBar");
            ((RectTransform)_chargeFill.transform.parent).At(new Vector2(0.5f, 0), Vector2.zero, new Vector2(420, 20));
            _chargeText = UIFactory.Text(_chargeRoot, "", 18, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _chargeText.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(420, 26));

            _biteMark = UIFactory.Text(_root, "!", 120, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _biteMark.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(200, 160));
            _heartText = UIFactory.Text(_root, "♥ 두근두근 ♥", 40, new Color(1f, 0.4f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _heartText.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(500, 60));
        }

        void BuildToasts()
        {
            _toastRoot = UIFactory.Node("Toasts", _root).At(new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(900, 200));
        }

        // ---------------- 갱신 ----------------

        public void Toast(string message, ToastKind kind)
        {
            var color = kind switch
            {
                ToastKind.Good => UIFactory.Good,
                ToastKind.Warn => UIFactory.Warn,
                ToastKind.Great => UIFactory.Gold,
                _ => UIFactory.TextMain
            };
            var t = UIFactory.Text(_toastRoot, message, kind == ToastKind.Great ? 26 : 22, color, TextAnchor.MiddleCenter,
                kind == ToastKind.Great ? FontStyle.Bold : FontStyle.Normal);
            t.rectTransform.At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(900, 34));
            _toasts.Add((t, Time.time + (kind == ToastKind.Great ? 5f : 3.2f)));
            while (_toasts.Count > 5)
            {
                Object.Destroy(_toasts[0].text.gameObject);
                _toasts.RemoveAt(0);
            }
        }

        public void Update()
        {
            var p = Game.Profile;
            var d = p.Data;
            var fishing = Game.Fishing;
            bool fighting = fishing.State == FishingState.Fighting;

            // 파이팅 중에는 필드 HUD를 숨긴다
            foreach (Transform child in _root)
                if (child != _toastRoot) child.gameObject.SetActive(!fighting);
            UpdateToasts();
            if (fighting) return;

            _levelText.text = $"Lv.{d.level}  강태공";
            _goldText.text = $"{d.gold:N0} 골드   ·   총 {d.totalCatches}마리";
            _staminaFill.fillAmount = d.stamina / PlayerProfile.MaxStamina;
            _staminaFill.color = d.stamina < 30f ? UIFactory.Danger : d.stamina < 60f ? UIFactory.Warn : UIFactory.Good;
            _staminaText.text = $"{d.stamina:0}";
            bool maxLevel = d.level >= PlayerProfile.MaxLevel;
            _expFill.fillAmount = maxLevel ? 1f : d.exp / (float)PlayerProfile.ExpToNext(d.level);
            _expText.text = maxLevel ? "MAX" : $"{_expFill.fillAmount * 100f:0}%";
            _gearText.text = $"낚싯대 {p.Rod.Name} (내구 {d.rodDurability:0})   ·   릴 {p.Reel.Name}   ·   줄 {p.Line.Name}";

            var clock = Game.Clock;
            _clockText.text = $"{clock.Day}일차  {clock.TimeText}  {Names.Of(clock.Band)} · {Names.Of(Game.Weather.Current)}";
            var playerPos = Game.Player.transform.position;
            _areaText.text = $"{HanRiverMap.MapName} · {HanRiverMap.AreaName(playerPos)}";

            var quest = Game.Quests.Current;
            _questTitle.text = quest?.Title ?? "모든 퀘스트 완료";
            _questGoal.text = quest != null ? $"{quest.GoalText}  ({Mathf.Min(Game.Quests.Progress, quest.Count)}/{quest.Count})" : "자유롭게 대물을 노려보세요!";

            for (int i = 0; i < _baitSlots.Count; i++)
            {
                var (bg, label, bait) = _baitSlots[i];
                bool selected = d.baitId == bait.Id;
                int count = p.Count(bait.Id);
                label.text = $"{i + 1}  {bait.Name}\n<size=20><b>{count}</b></size>";
                label.color = count > 0 ? UIFactory.TextMain : UIFactory.TextDim;
                bg.color = selected ? new Color(0.15f, 0.45f, 0.42f, 0.95f) : UIFactory.PanelBg;
            }

            UpdateMap(playerPos);
            UpdateCenter(playerPos);
        }

        void UpdateMap(Vector3 playerPos)
        {
            // 플레이어를 중심으로 지도를 움직이지 않고, 전체 맵(320x220m)을 그대로 보여준다
            _playerArrow.anchoredPosition = MapPos(playerPos.x, playerPos.z);
            _playerArrow.localRotation = Quaternion.Euler(0, 0, -Game.Player.Yaw);

            var state = Game.Fishing.State;
            bool showBobber = state == FishingState.Waiting || state == FishingState.Bite || state == FishingState.Casting;
            _bobberDot.gameObject.SetActive(showBobber);
            if (showBobber) _bobberDot.anchoredPosition = MapPos(Game.Fishing.BobberPosition.x, Game.Fishing.BobberPosition.z);

            var schools = Game.Schools.Schools;
            while (_schoolMarks.Count < schools.Count)
            {
                var mark = UIFactory.Text(_mapRoot, "◆", 14, new Color(1f, 0.75f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold, "School").rectTransform
                    .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
                _schoolMarks.Add(mark);
            }
            for (int i = 0; i < schools.Count; i++)
            {
                var c = HanRiverMap.CellCenter(schools[i].Col, schools[i].Row);
                _schoolMarks[i].anchoredPosition = MapPos(c.x, c.z) + new Vector2(Mathf.Sin(Time.time * 1.5f + i) * 3f, 0f);
            }
        }

        void UpdateCenter(Vector3 playerPos)
        {
            var f = Game.Fishing;
            _chargeRoot.gameObject.SetActive(f.State == FishingState.Charging);
            if (f.State == FishingState.Charging)
            {
                _chargeFill.fillAmount = f.Charge;
                _chargeFill.color = f.CastTargetIsWater ? UIFactory.Accent : UIFactory.Danger;
                _chargeText.text = $"캐스팅 {f.CastDistance:0.0}m" + (f.CastTargetIsWater ? "" : "  (물이 아닙니다)");
            }

            bool bite = f.State == FishingState.Bite;
            _biteMark.gameObject.SetActive(bite);
            if (bite) _biteMark.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 30f) * 0.08f);
            _heartText.gameObject.SetActive(bite && f.BigBite);
            if (bite && f.BigBite) _heartText.rectTransform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.15f);

            _hint.text = f.State switch
            {
                FishingState.Idle => "WASD 이동 · Shift 달리기 · 우클릭 드래그 시점 · 좌클릭 꾹 눌러 캐스팅",
                FishingState.Charging => "놓으면 던집니다 (거리 게이지가 왕복합니다)",
                FishingState.Casting => "",
                FishingState.Waiting => $"{f.CastCellLabel}  ·  찌가 쭉 빨려들면 좌클릭으로 챔질!  (R/이동: 회수)",
                FishingState.Bite => "지금! 좌클릭!",
                _ => ""
            };

            bool nearShop = Vector3.Distance(playerPos, HanRiverMap.ShopPos) < HanRiverMap.ShopRadius;
            _prompt.text = nearShop && f.State == FishingState.Idle ? "[P] 낚시점 이용" : "";
            if (Game.Profile.Data.stamina < 30f && f.State == FishingState.Idle)
                _prompt.text = "지쳤습니다... 살림망(I)에서 요리하거나 김밥을 드세요";
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
                _toasts[i].text.rectTransform.anchoredPosition = new Vector2(0, -(_toasts.Count - 1 - i) * 36f);
        }
    }
}
