using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 파이팅 HUD. 원작 플레이 영상의 배치를 따른다.
    ///  - 위: 파이팅 레벨 링(릴 손잡이) + HP 바 + 눈금자 위 거리 마커
    ///  - 가운데 위: [설명 끄기 : U] 안내문
    ///  - 카운터 찬스: 속도선 + 큰 파란 화살표 + 마우스 아이콘
    ///  - 물고기 위치에 데미지 숫자
    ///  - 랜딩 찬스: 5m → 0m 다이얼, 연타 안내
    ///  - 아래: 카운터 콤보 구슬 + LOW~HIGH 장력 바
    /// </summary>
    public class FightHudView
    {
        const float RulerWidth = 640f;

        readonly RectTransform _root;
        Image _hookRing, _ringBack;
        RectTransform _reelHandle;
        Image _hpFill;
        RectTransform _distMarker;
        Text _distLabel, _nameText;
        Text _tipTitle, _tipText;
        Image _speedLines;
        RectTransform _counterArrow;
        Image _counterArrowImg;
        RectTransform _counterMice;
        RectTransform _chevron;
        Image _chevronImg;
        RectTransform _runMouse;
        Text _bigDistance;
        RectTransform _tensionMarker;
        Image _tensionFill;
        Text _highLabel;
        readonly Image[] _pips = new Image[5];
        Text _counterText, _warnText, _staminaText;
        RectTransform _landingRoot;
        Image _landingFill;
        Text _landingValue;
        float _counterUntil, _escapeUntil, _landingFlashUntil;
        float _reelAngle;

        readonly List<(Text text, Vector3 world, float born, bool big)> _pops = new List<(Text, Vector3, float, bool)>();

        public FightHudView(Transform canvas)
        {
            _root = UIFactory.Node("FightHud", canvas).Fill();
            BuildOverlays();
            BuildTop();
            BuildBottom();
            BuildCenter();
            BuildLanding();
            _root.gameObject.SetActive(false);
        }

        // ---------------- 빌드 ----------------

        void BuildOverlays()
        {
            _speedLines = UIFactory.Image(_root, new Color(0.85f, 0.95f, 1f, 0.6f), UIFactory.SpeedLines, "SpeedLines");
            _speedLines.rectTransform.Fill(-200, -200, -200, -200);

            _chevron = UIFactory.Node("RunChevron", _root).At(new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(110, 260), new Vector2(0.5f, 0.5f));
            _chevronImg = _chevron.gameObject.AddComponent<Image>();
            _chevronImg.sprite = UIFactory.Chevron;
            _chevronImg.color = new Color(1f, 0.25f, 0.2f, 0.9f);
            _chevronImg.raycastTarget = false;
            _runMouse = UIFactory.Node("RunMouse", _root).At(new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(240, 110), new Vector2(0.5f, 0.5f));
            UIFactory.Image(_runMouse, new Color(1, 1, 1, 0.35f), UIFactory.Mouse(true), "Ghost").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-70, 0), new Vector2(70, 105));
            UIFactory.Image(_runMouse, new Color(0.3f, 0.8f, 1f), UIFactory.Arrow, "Arrow").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(110, 60));
            UIFactory.Image(_runMouse, Color.white, UIFactory.Mouse(true), "Mouse").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(75, 0), new Vector2(70, 105));

            _counterArrow = UIFactory.Node("CounterArrow", _root).At(new Vector2(1, 0.5f), new Vector2(-150, 30), new Vector2(220, 130), new Vector2(0.5f, 0.5f));
            _counterArrowImg = _counterArrow.gameObject.AddComponent<Image>();
            _counterArrowImg.sprite = UIFactory.Arrow;
            _counterArrowImg.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            _counterArrowImg.raycastTarget = false;
            _counterMice = UIFactory.Node("CounterMice", _root).At(new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(360, 110), new Vector2(0.5f, 0.5f));
            UIFactory.Image(_counterMice, new Color(0.35f, 0.75f, 0.95f), UIFactory.Arrow, "ArrowL").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-160, 0), new Vector2(80, 40)).localScale = new Vector3(-1, 1, 1);
            UIFactory.Image(_counterMice, Color.white, UIFactory.Mouse(true), "MouseL").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-80, 0), new Vector2(66, 100));
            UIFactory.Image(_counterMice, Color.white, UIFactory.Mouse(true), "MouseR").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(80, 0), new Vector2(66, 100));
            UIFactory.Image(_counterMice, new Color(0.35f, 0.75f, 0.95f), UIFactory.Arrow, "ArrowR").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(160, 0), new Vector2(80, 40));
        }

        void BuildTop()
        {
            var top = UIFactory.Node("Top", _root).At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(900, 110));
            // 원작처럼 비스듬히 잘린 어두운 띠
            UIFactory.Image(top, new Color(0, 0, 0, 0.55f), null, "Band").rectTransform.Fill(52, 20, 18, 30);

            _ringBack = UIFactory.Image(top, new Color(0.06f, 0.07f, 0.08f, 0.95f), UIFactory.Circle, "RingBack");
            _ringBack.rectTransform.At(new Vector2(0, 0.5f), new Vector2(0, 4), new Vector2(110, 110));
            _hookRing = UIFactory.Image(_ringBack.transform, UIFactory.Good, UIFactory.Ring, "Ring");
            _hookRing.rectTransform.Fill(6, 6, 6, 6);
            _hookRing.type = Image.Type.Filled;
            _hookRing.fillMethod = Image.FillMethod.Radial360;
            _hookRing.fillOrigin = (int)Image.Origin360.Top;
            // 릴 손잡이: 감을 때 돈다
            _reelHandle = UIFactory.Node("ReelHandle", _ringBack.transform).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            UIFactory.Image(_reelHandle, new Color(0.75f, 0.75f, 0.78f), null, "Arm").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(8, 34));
            UIFactory.Image(_reelHandle, new Color(0.2f, 0.2f, 0.22f), UIFactory.Circle, "Knob").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(18, 18));
            UIFactory.Image(_ringBack.transform, new Color(0.85f, 0.85f, 0.88f), UIFactory.Circle, "Hub").rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));

            UIFactory.Text(top, "HP", 18, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(122, -22), new Vector2(40, 24));
            _nameText = UIFactory.Text(top, "", 17, UIFactory.TextMain, TextAnchor.MiddleRight);
            _nameText.rectTransform.At(new Vector2(1, 1), new Vector2(-24, -20), new Vector2(420, 24));

            _hpFill = UIFactory.Bar(top, new Color(0.12f, 0.04f, 0.04f, 0.9f), Color.white, "HP");
            _hpFill.sprite = UIFactory.HpGradient;
            ((RectTransform)_hpFill.transform.parent).At(new Vector2(0, 1), new Vector2(160, -24), new Vector2(700, 22));

            // 거리 눈금자 + 마커
            var ruler = UIFactory.Node("Ruler", top).At(new Vector2(0, 1), new Vector2(160, -60), new Vector2(RulerWidth, 12));
            UIFactory.Image(ruler, new Color(1f, 0.75f, 0.3f, 0.8f), null, "Base").rectTransform.At(new Vector2(0, 1), Vector2.zero, new Vector2(RulerWidth, 2));
            var ticks = UIFactory.Node("Ticks", ruler).At(new Vector2(0, 1), new Vector2(0, -2), new Vector2(RulerWidth, 8));
            var raw = ticks.gameObject.AddComponent<RawImage>();
            raw.texture = UIFactory.Ticks;
            raw.uvRect = new Rect(0, 0, RulerWidth / 8f, 1);
            raw.color = new Color(1, 1, 1, 0.5f);
            raw.raycastTarget = false;
            _distMarker = UIFactory.Node("Marker", ruler).At(new Vector2(0, 1), Vector2.zero, new Vector2(16, 16), new Vector2(0.5f, 0.5f));
            UIFactory.Text(_distMarker, "◆", 16, UIFactory.Gold, TextAnchor.MiddleCenter).rectTransform.Fill();
            var labelBg = UIFactory.Image(_distMarker, new Color(0, 0, 0, 0.75f), null, "LabelBg");
            labelBg.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(46, 0), new Vector2(66, 24));
            _distLabel = UIFactory.Text(labelBg.transform, "", 17, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _distLabel.rectTransform.Fill();

            _tipTitle = UIFactory.Text(_root, "[설명 끄기 : U]", 18, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _tipTitle.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1200, 26));
            _tipText = UIFactory.Text(_root, "", 21, UIFactory.TextMain, TextAnchor.MiddleCenter);
            _tipText.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1400, 30));

            _bigDistance = UIFactory.Text(_root, "", 90, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _bigDistance.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(700, 120));
        }

        void BuildBottom()
        {
            var bottom = UIFactory.Node("Bottom", _root).At(new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1060, 34));

            var pipBox = UIFactory.Image(bottom, new Color(0, 0, 0, 0.7f), null, "PipBox");
            pipBox.rectTransform.At(new Vector2(0, 0.5f), new Vector2(30, 46), new Vector2(170, 40));
            for (int i = 0; i < _pips.Length; i++)
            {
                var ring = UIFactory.Image(pipBox.transform, new Color(1, 1, 1, 0.15f), UIFactory.Ring, $"Pip{i}");
                ring.rectTransform.At(new Vector2(0, 0.5f), new Vector2(8 + i * 31, 0), new Vector2(28, 28));
                _pips[i] = ring;
            }

            var barBack = UIFactory.Image(bottom, Color.white, UIFactory.TensionGradient, "TensionBack");
            barBack.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 22));
            _tensionFill = UIFactory.Image(barBack.transform, new Color(0.35f, 0.7f, 1f, 0.85f), null, "Fill");
            _tensionFill.rectTransform.Fill(2, 2, 3, 3);
            _tensionFill.type = Image.Type.Filled;
            _tensionFill.fillMethod = Image.FillMethod.Horizontal;
            UIFactory.Text(barBack.transform, "LOW", 18, new Color(0.45f, 0.7f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 0.5f), new Vector2(-56, 0), new Vector2(56, 24));
            _highLabel = UIFactory.Text(barBack.transform, "HIGH", 18, UIFactory.Warn, TextAnchor.MiddleLeft, FontStyle.Bold);
            _highLabel.rectTransform.At(new Vector2(1, 0.5f), new Vector2(8, 0), new Vector2(64, 24), new Vector2(0, 0.5f));
            _tensionMarker = UIFactory.Node("Marker", barBack.transform).At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(36, 36), new Vector2(0.5f, 0.5f));
            UIFactory.Text(_tensionMarker, "◆", 34, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
        }

        void BuildCenter()
        {
            _counterText = UIFactory.Text(_root, "COUNTER", 120, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _counterText.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1000, 170));
            var outline = _counterText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.15f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(4, -4);
            _counterText.gameObject.SetActive(false);

            _warnText = UIFactory.Text(_root, "", 28, UIFactory.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            _warnText.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1000, 40));
            _staminaText = UIFactory.Text(_root, "", 18, UIFactory.Warn, TextAnchor.MiddleLeft);
            _staminaText.rectTransform.At(new Vector2(0, 0), new Vector2(24, 24), new Vector2(500, 26));
        }

        void BuildLanding()
        {
            _landingRoot = UIFactory.Node("Landing", _root).At(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(560, 300));
            UIFactory.Image(_landingRoot, Color.white, UIFactory.Mouse(true), "Mouse").rectTransform.At(new Vector2(0.5f, 1), new Vector2(-150, 0), new Vector2(60, 90));
            UIFactory.Text(_landingRoot, "or", 26, UIFactory.TextMain, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 1), new Vector2(-70, -30), new Vector2(60, 40));
            var space = UIFactory.Image(_landingRoot, new Color(1f, 0.45f, 0.4f, 0.9f), null, "Space");
            space.rectTransform.At(new Vector2(0.5f, 1), new Vector2(90, -30), new Vector2(230, 40));
            UIFactory.Text(space.transform, "SPACE BAR", 16, Color.white, TextAnchor.MiddleRight).rectTransform.Fill(0, 10, 0, 0);

            var dial = UIFactory.Image(_landingRoot, new Color(0.08f, 0.06f, 0.05f, 0.9f), UIFactory.Circle, "Dial");
            dial.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(220, 220));
            var arcBack = UIFactory.Image(dial.transform, new Color(0.35f, 0.2f, 0.1f, 0.9f), UIFactory.Ring, "ArcBack");
            arcBack.rectTransform.Fill(10, 10, 10, 10);
            arcBack.type = Image.Type.Filled;
            arcBack.fillMethod = Image.FillMethod.Radial360;
            arcBack.fillOrigin = (int)Image.Origin360.Left;
            arcBack.fillClockwise = true;
            arcBack.fillAmount = 0.5f;
            _landingFill = UIFactory.Image(dial.transform, new Color(1f, 0.6f, 0.15f), UIFactory.Ring, "ArcFill");
            _landingFill.rectTransform.Fill(10, 10, 10, 10);
            _landingFill.type = Image.Type.Filled;
            _landingFill.fillMethod = Image.FillMethod.Radial360;
            _landingFill.fillOrigin = (int)Image.Origin360.Left;
            _landingFill.fillClockwise = true;
            string[] marks = { "5m", "4m", "2m", "1m", "0m" };
            float[] angles = { 180f, 144f, 72f, 36f, 0f };
            for (int i = 0; i < marks.Length; i++)
            {
                float a = angles[i] * Mathf.Deg2Rad;
                UIFactory.Text(dial.transform, marks[i], 15, UIFactory.TextDim, TextAnchor.MiddleCenter).rectTransform
                    .At(new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a) * 128f, Mathf.Sin(a) * 128f), new Vector2(44, 20));
            }
            UIFactory.Text(dial.transform, "LANDING", 16, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .At(new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(160, 24));
            _landingValue = UIFactory.Text(dial.transform, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _landingValue.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(180, 54));
            _landingRoot.gameObject.SetActive(false);
        }

        // ---------------- 이벤트 ----------------

        public void ShowCounter(int combo)
        {
            _counterUntil = Time.time + 0.9f;
            _counterText.text = combo > 1 ? $"COUNTER x{combo}" : "COUNTER";
            _counterText.gameObject.SetActive(true);
        }

        public void ShowEscape() => _escapeUntil = Time.time + 2.5f;

        public void ShowLandingChance() => _landingFlashUntil = Time.time + 1.2f;

        /// <summary>물고기 위치에 데미지 숫자를 띄운다. 카운터는 크고 금색.</summary>
        public void PopDamage(Vector3 world, float amount, bool big)
        {
            var t = UIFactory.Text(_root, Mathf.Max(1, Mathf.RoundToInt(amount)).ToString(), big ? 54 : 34,
                big ? new Color(1f, 0.8f, 0.35f) : new Color(1f, 0.9f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 70));
            _pops.Add((t, world + new Vector3(Random.Range(-0.6f, 0.6f), 0.3f, 0f), Time.time, big));
        }

        // ---------------- 갱신 ----------------

        public void Update()
        {
            var fishing = Game.Fishing;
            var f = fishing.Fight;
            bool show = fishing.State == FishingState.Fighting && f != null;
            _root.gameObject.SetActive(show);
            if (!show)
            {
                ClearPops();
                return;
            }

            bool tips = Game.Profile.Data.showTips;

            // 파이팅 레벨 링 + 릴 손잡이
            float hook = f.Hook / 100f;
            _hookRing.fillAmount = hook;
            bool redRing = f.IsDanger || f.CounterChance || hook < 0.3f;
            _hookRing.color = redRing ? UIFactory.Danger : hook > 0.6f ? UIFactory.Good : UIFactory.Warn;
            if (Input.GetMouseButton(0) || f.Landing) _reelAngle -= Time.deltaTime * 720f;
            _reelHandle.localRotation = Quaternion.Euler(0, 0, _reelAngle);

            _hpFill.fillAmount = f.HP / f.MaxHP;
            _nameText.text = fishing.BigBite ? "<color=#ff7090>♥ 두근두근 ♥</color>" : "";

            float shownDistance = f.Landing ? f.LandingDistance : f.Distance;
            float rulerT = Mathf.Clamp01(shownDistance / f.Reel.Capacity);
            _distMarker.anchoredPosition = new Vector2(rulerT * RulerWidth, -1);
            _distLabel.text = $"{shownDistance:0}m";

            float tension = Mathf.Clamp01(f.Tension);
            _tensionFill.fillAmount = tension;
            _tensionFill.color = f.IsDanger ? new Color(1f, 0.35f, 0.3f, 0.85f) : f.IsHighTension ? new Color(1f, 0.7f, 0.3f, 0.85f) : new Color(0.35f, 0.7f, 1f, 0.85f);
            _tensionMarker.anchorMin = _tensionMarker.anchorMax = new Vector2(tension, 0.5f);
            _tensionMarker.anchoredPosition = Vector2.zero;
            _tensionMarker.localScale = Vector3.one * (f.IsDanger ? 1.2f + Mathf.Sin(Time.time * 30f) * 0.15f : 1f);
            _highLabel.color = f.IsHighTension ? UIFactory.Warn : new Color(1, 1, 1, 0.35f);

            for (int i = 0; i < _pips.Length; i++)
                _pips[i].color = i < f.Combo ? new Color(0.3f, 0.75f, 1f) : new Color(1, 1, 1, 0.15f);

            // 카운터 찬스: 속도선 + 화살표
            bool counterChance = f.CounterChance && !f.Landing;
            float pulse = 0.75f + Mathf.Sin(Time.time * 12f) * 0.25f;
            _speedLines.gameObject.SetActive(counterChance);
            if (counterChance) _speedLines.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.time * 40f);
            _counterArrow.gameObject.SetActive(counterChance);
            if (counterChance)
            {
                int dir = f.CounterDirection;
                _counterArrow.anchorMin = _counterArrow.anchorMax = new Vector2(dir > 0 ? 1f : 0f, 0.5f);
                _counterArrow.anchoredPosition = new Vector2(dir > 0 ? -170f : 170f, 30f) + Vector2.right * dir * pulse * 20f;
                _counterArrow.localScale = new Vector3(dir, 1, 1) * (0.9f + pulse * 0.2f);
            }
            _counterMice.gameObject.SetActive(counterChance && tips);

            // 물고기가 한쪽으로 달릴 때: 그쪽 끝에 빨간 꺾쇠 + 반대로 당기라는 안내
            bool running = !counterChance && !f.Landing && Mathf.Abs(f.FishX) > 0.4f && f.Mood != FishMood.Rest;
            _chevron.gameObject.SetActive(running);
            if (running)
            {
                int side = f.FishX > 0 ? 1 : -1;
                _chevron.anchorMin = _chevron.anchorMax = new Vector2(side > 0 ? 1f : 0f, 0.5f);
                _chevron.anchoredPosition = new Vector2(side > 0 ? -60f : 60f, 0f);
                _chevron.localScale = new Vector3(side > 0 ? -1 : 1, 1, 1) * (0.9f + pulse * 0.1f);
                _chevronImg.color = new Color(1f, 0.25f, 0.2f, 0.5f + pulse * 0.4f);
                _runMouse.localScale = new Vector3(-side, 1, 1);
            }
            _runMouse.gameObject.SetActive(running && tips);

            bool far = !f.Landing && f.Distance > f.Reel.Capacity * 0.7f;
            _bigDistance.text = far ? $"{f.Distance:0.00}m" : "";

            // 안내문 (원작 문구)
            string tip;
            if (f.Landing) tip = "랜딩 찬스! SPACE BAR를 빠르게 누르거나, 마우스 왼쪽 버튼을 빠르게 클릭하세요.";
            else if (counterChance) tip = "화살표 방향으로 낚시대를 빠르게 당겨주세요! 카운터로 질주하는 물고기를 멈출 수 있어요.";
            else if (Time.time < _escapeUntil || (f.Distance < FishFight.LandDistance + 2f && !f.HpLow)) tip = "체력이 높은 물고기를 5m안으로 당겨오면 물고기가 도망가게 됩니다.";
            else if (far) tip = "물고기와의 거리가 멀어지면 놓치게 됩니다. 거리를 좁히세요.";
            else if (running) tip = "물고기가 도망가는 반대 방향으로 당겨주세요. 물고기가 한쪽으로 몰리면 도망가기 쉽답니다.";
            else if (f.HpLow) tip = "물고기가 지쳤습니다! 5m 안으로 끌어오면 랜딩 찬스!";
            else tip = "왼쪽 버튼으로 줄을 감고, 오른쪽 버튼으로 풀어주세요. 장력을 HIGH 쪽에 두면 데미지가 커집니다.";
            _tipTitle.gameObject.SetActive(tips);
            _tipText.gameObject.SetActive(tips);
            _tipText.text = tip;

            if (f.IsDanger) _warnText.text = "줄이 끊어지려 한다! 오른쪽 버튼으로 풀어주세요";
            else if (f.Hook < 30f) _warnText.text = "바늘이 빠지려 한다! 줄을 감아 팽팽하게";
            else _warnText.text = "";

            // 랜딩 다이얼
            _landingRoot.gameObject.SetActive(f.Landing);
            if (f.Landing)
            {
                float t = 1f - Mathf.Clamp01(f.LandingDistance / FishFight.LandDistance);
                _landingFill.fillAmount = t * 0.5f;
                _landingValue.text = f.LandingDistance.ToString("0.00");
                float s = Time.time < _landingFlashUntil ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.08f : 1f;
                _landingRoot.localScale = Vector3.one * s;
            }

            if (_counterText.gameObject.activeSelf)
            {
                float left = _counterUntil - Time.time;
                _counterText.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, left - 0.6f) * 1.5f);
                if (left <= 0f) _counterText.gameObject.SetActive(false);
            }

            UpdatePops();

            float stamina = Game.Profile.Data.stamina;
            _staminaText.text = stamina < 30f ? $"체력 {stamina:0} · 지쳐서 힘이 빠진다..." : $"체력 {stamina:0}";
            _staminaText.color = stamina < 30f ? UIFactory.Danger : UIFactory.TextDim;
        }

        void UpdatePops()
        {
            var cam = Game.CameraRig.Cam;
            for (int i = _pops.Count - 1; i >= 0; i--)
            {
                var (text, world, born, big) = _pops[i];
                float age = Time.time - born;
                if (age > 1.1f)
                {
                    Object.Destroy(text.gameObject);
                    _pops.RemoveAt(i);
                    continue;
                }
                var screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f)
                {
                    text.enabled = false;
                    continue;
                }
                text.enabled = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
                text.rectTransform.anchoredPosition = local + new Vector2(0, age * 60f);
                var c = text.color;
                c.a = Mathf.Clamp01((1.1f - age) / 0.4f);
                text.color = c;
                text.rectTransform.localScale = Vector3.one * (big ? 1f + Mathf.Max(0f, 0.25f - age) * 2f : 1f);
            }
        }

        void ClearPops()
        {
            foreach (var p in _pops) Object.Destroy(p.text.gameObject);
            _pops.Clear();
        }
    }
}
