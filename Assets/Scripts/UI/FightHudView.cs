using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using TMPro;
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
        const float RulerWidth = 560f;

        readonly RectTransform _root;
        Image _hookRing, _ringBack;
        RectTransform _reelHandle;
        Image _hpFill;
        RectTransform _distMarker;
        TMP_Text _distLabel, _nameText;
        TMP_Text _tipTitle, _tipText;
        Image _speedLines;
        /// <summary>개발용(자동 스크린샷): 카운터 찬스 연출을 강제로 띄운다.</summary>
        public static bool DebugForceCounterChance;
        float _speedLinesNext;
        RectTransform _counterArrow;
        Image _counterArrowImg;
        RectTransform _counterMice;
        RectTransform _chevron;
        Image _chevronImg;
        RectTransform _runMouse;
        TMP_Text _bigDistance;
        RectTransform _tensionMarker;
        Image _tensionFill;
        TMP_Text _highLabel, _tensionPercent;
        RectTransform _slackHint;
        TMP_Text _slackHintText;
        readonly Image[] _pips = new Image[5];
        TMP_Text _counterText, _warnText, _staminaText, _hitText;
        FishFight _hitShownFor;
        float _hitAt;
        RectTransform _warnPill;
        RectTransform _landingRoot;
        Image _landingFill;
        TMP_Text _landingValue;
        float _counterUntil, _escapeUntil, _landingFlashUntil;
        float _reelAngle;
        Image _vignette;
        float _vignetteAlpha;

        readonly List<(TMP_Text text, Vector3 world, float born, bool big)> _pops = new List<(TMP_Text, Vector3, float, bool)>();

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
            // 원작: 장력이 위험하면 화면 가장자리가 빨갛게 번쩍인다
            _vignette = UIFactory.Image(_root, new Color(0.9f, 0.05f, 0.03f, 0f), UIFactory.Vignette, "DangerVignette");
            _vignette.rectTransform.Fill();

            _speedLines = UIFactory.Image(_root, new Color(0.85f, 0.95f, 1f, 0.6f), UIFactory.SpeedLines, "SpeedLines");
            // 크기는 Update에서 화면 대각선보다 크게 맞춘다 (돌려도 모서리가 비지 않게)
            _speedLines.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 2400));

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
            UIFactory.Glass(top, "Band", UIFactory.RadiusHud, null, 0.55f, true, false).rectTransform.Fill(52, 10, 14, 24);

            _ringBack = UIFactory.Glass(top, "RingBack", UIFactory.Pill, null, 0.8f, true, false);
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
            _nameText = UIFactory.Text(top, "", 16, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            _nameText.rectTransform.At(new Vector2(1, 1), new Vector2(-18, -22), new Vector2(150, 26));

            _hpFill = UIFactory.Bar(top, new Color(0.12f, 0.04f, 0.04f, 0.9f), Color.white, "HP");
            _hpFill.sprite = UIFactory.HpGradient;
            ((RectTransform)_hpFill.transform.parent).At(new Vector2(0, 1), new Vector2(160, -24), new Vector2(560, 22));

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
            var labelBg = UIFactory.Shape(_distMarker, new Color(0.02f, 0.04f, 0.06f, 0.85f), UIFactory.Pill, "LabelBg", 0f);
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

            var pipBox = UIFactory.Glass(bottom, "PipBox", UIFactory.Pill, null, 0.6f, true, false);
            pipBox.rectTransform.At(new Vector2(0, 0.5f), new Vector2(30, 60), new Vector2(176, 42));
            for (int i = 0; i < _pips.Length; i++)
            {
                var ring = UIFactory.Image(pipBox.transform, new Color(1, 1, 1, 0.15f), UIFactory.Ring, $"Pip{i}");
                ring.rectTransform.At(new Vector2(0, 0.5f), new Vector2(11 + i * 31, 0), new Vector2(28, 28));
                _pips[i] = ring;
            }

            var barBack = UIFactory.Shape(bottom, Color.white, UIFactory.Pill, "TensionBack", 4f);
            barBack.sprite = UIFactory.TensionGradient;
            UIFactory.AddShadow(barBack, 16f, new Vector2(0, -4), 0.5f);
            UIFactory.AddBorder(barBack, new Color(1f, 1f, 1f, 0.18f), 1.2f);
            barBack.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 22));
            _tensionFill = UIFactory.Shape(barBack.transform, new Color(0.35f, 0.7f, 1f, 0.85f), UIFactory.Pill, "Fill", 0f);
            _tensionFill.rectTransform.Fill(3, 3, 3, 3);
            _tensionFill.type = Image.Type.Filled;
            _tensionFill.fillMethod = Image.FillMethod.Horizontal;
            UIFactory.Text(barBack.transform, "LOW", 18, new Color(0.45f, 0.7f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 0.5f), new Vector2(-56, 0), new Vector2(56, 24));
            _highLabel = UIFactory.Text(barBack.transform, "HIGH", 18, UIFactory.Warn, TextAnchor.MiddleLeft, FontStyle.Bold);
            _highLabel.rectTransform.At(new Vector2(1, 0.5f), new Vector2(8, 0), new Vector2(64, 24), new Vector2(0, 0.5f));
            _tensionMarker = UIFactory.Node("Marker", barBack.transform).At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(36, 36), new Vector2(0.5f, 0.5f));
            UIFactory.Text(_tensionMarker, "◆", 34, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
            var pctPill = UIFactory.Shape(_tensionMarker, new Color(0.02f, 0.04f, 0.06f, 0.88f), UIFactory.Pill, "PercentPill", 0f);
            UIFactory.AddBorder(pctPill, new Color(1f, 0.82f, 0.35f, 0.6f), 1.2f);
            pctPill.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0, 30), new Vector2(60, 24), new Vector2(0.5f, 0.5f));
            _tensionPercent = UIFactory.Text(pctPill.rectTransform, "", 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _tensionPercent.rectTransform.Fill();

            // 바 이름과 끝(한계) 표시: 오른쪽 끝을 넘으면 줄이 끊어진다
            UIFactory.Text(barBack.transform, "장력", 15, new Color(1, 1, 1, 0.75f), TextAnchor.LowerLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(0, 2), new Vector2(80, 20), new Vector2(0, 0));
            UIFactory.Image(barBack.transform, new Color(1f, 0.2f, 0.15f, 0.95f), null, "LimitLine").rectTransform
                .At(new Vector2(1, 0.5f), Vector2.zero, new Vector2(4, 34), new Vector2(0.5f, 0.5f));
            // 현재 % 표시(마커 위)와 겹치지 않게 한계 글자는 바 아래에 둔다
            UIFactory.Text(barBack.transform, "한계 · 줄 끊김", 13, new Color(1f, 0.45f, 0.4f), TextAnchor.UpperRight, FontStyle.Bold).rectTransform
                .At(new Vector2(1, 0), new Vector2(0, -4), new Vector2(140, 20), new Vector2(1, 1));

            // 위험할 때: 어느 쪽으로 마우스를 끌어야 줄이 늦춰지는지
            _slackHint = UIFactory.Glass(_root, "SlackHint", UIFactory.Pill, null, 0.7f, true, false).rectTransform
                .At(new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(380, 44));
            _slackHintText = UIFactory.Text(_slackHint, "", 21, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _slackHintText.rectTransform.Fill();
        }

        void BuildCenter()
        {
            _counterText = UIFactory.Text(_root, "COUNTER", 120, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _counterText.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1000, 170));
            UIFactory.Outline(_counterText, new Color(0.35f, 0.15f, 0.02f, 0.9f), 2.5f);
            _counterText.gameObject.SetActive(false);

            // 원작 0:31: 챔질하면 금속 느낌의 큰 HIT가 쾅 찍힌다
            _hitText = UIFactory.Text(_root, "HIT", 150, new Color(0.93f, 0.95f, 0.98f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            _hitText.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 220));
            _hitText.enableVertexGradient = true;
            _hitText.colorGradient = new TMPro.VertexGradient(Color.white, Color.white, new Color(0.55f, 0.6f, 0.68f), new Color(0.55f, 0.6f, 0.68f));
            UIFactory.Outline(_hitText, new Color(0.08f, 0.1f, 0.14f, 0.9f), 2.5f);
            _hitText.gameObject.SetActive(false);

            // 경고문: 빨간 화면 테두리 위에서도 읽히게 어두운 유리 알약
            _warnPill = UIFactory.Glass(_root, "WarnPill", UIFactory.Pill, new Color(0.12f, 0.02f, 0.02f, 1f), 0.75f, true, false).rectTransform
                .At(new Vector2(0.5f, 0), new Vector2(0, 156), new Vector2(800, 42));
            UIFactory.AddBorder((LeTai.Asset.TranslucentImage.TranslucentImage)_warnPill.GetComponent<Image>(), new Color(1f, 0.35f, 0.3f, 0.6f), 1.5f);
            _warnText = UIFactory.Text(_warnPill, "", 22, new Color(1f, 0.5f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _warnText.rectTransform.Fill();
            _staminaText = UIFactory.Text(_root, "", 18, UIFactory.Warn, TextAnchor.MiddleLeft);
            _staminaText.rectTransform.At(new Vector2(0, 0), new Vector2(24, 24), new Vector2(500, 26));
        }

        void BuildLanding()
        {
            _landingRoot = UIFactory.Node("Landing", _root).At(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(560, 300));
            UIFactory.Image(_landingRoot, Color.white, UIFactory.Mouse(true), "Mouse").rectTransform.At(new Vector2(0.5f, 1), new Vector2(-150, 0), new Vector2(60, 90));
            UIFactory.Text(_landingRoot, "or", 26, UIFactory.TextMain, TextAnchor.MiddleCenter).rectTransform.At(new Vector2(0.5f, 1), new Vector2(-70, -30), new Vector2(60, 40));
            var space = UIFactory.Shape(_landingRoot, new Color(1f, 0.45f, 0.4f, 0.92f), 10f, "Space", 5f);
            space.rectTransform.At(new Vector2(0.5f, 1), new Vector2(90, -30), new Vector2(230, 40));
            UIFactory.Text(space.transform, "SPACE BAR", 16, Color.white, TextAnchor.MiddleRight).rectTransform.Fill(0, 10, 0, 0);

            var dial = UIFactory.Glass(_landingRoot, "Dial", UIFactory.Pill, new Color(0.08f, 0.06f, 0.05f, 1f), 0.75f, true, false);
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
        /// <summary>새 파이팅이 시작되면 HIT를 크게 찍었다가(0.12초) 줄어들며 사라진다.</summary>
        void UpdateHit(FishFight f)
        {
            if (f != _hitShownFor)
            {
                _hitShownFor = f;
                _hitAt = Time.unscaledTime;
                _hitText.gameObject.SetActive(true);
            }
            if (!_hitText.gameObject.activeSelf) return;
            float age = Time.unscaledTime - _hitAt;
            if (age > 1.1f) { _hitText.gameObject.SetActive(false); return; }
            float slam = age < 0.12f ? Mathf.Lerp(2.6f, 1f, age / 0.12f) : 1f + Mathf.Max(0f, 0.25f - age) * 0.3f;
            _hitText.rectTransform.localScale = Vector3.one * slam;
            _hitText.alpha = age < 0.75f ? 1f : Mathf.Clamp01((1.1f - age) / 0.35f);
        }

        public void PopDamage(Vector3 world, float amount, bool big)
        {
            // 원작: 금색 숫자가 툭 튀어나왔다가 떠오른다
            var t = UIFactory.Text(_root, Mathf.Max(1, Mathf.RoundToInt(amount)).ToString(), big ? 58 : 38,
                big ? new Color(1f, 0.78f, 0.3f) : new Color(1f, 0.86f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Outline(t, new Color(0.3f, 0.15f, 0.02f, 0.85f), 1.5f);
            t.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 70));
            _pops.Add((t, world + new Vector3(Random.Range(-0.6f, 0.6f), 0.3f, 0f), Time.time, big));
        }

        // ---------------- 파이팅 기록 (결과 화면 차트) ----------------

        /// <summary>마지막 파이팅의 장력(%)·물고기 HP(%)를 일정 간격으로 남긴 기록. 결과 화면이 꺾은선으로 그린다.</summary>
        public static readonly List<float> TensionLog = new List<float>();
        public static readonly List<float> HpLog = new List<float>();
        public static float LogInterval = 0.25f;
        public static float LastFightSeconds;
        FishFight _logged;
        float _nextLog;

        void RecordLog(FishFight f)
        {
            if (f != _logged)
            {
                _logged = f;
                TensionLog.Clear();
                HpLog.Clear();
                _nextLog = 0f;
            }
            LastFightSeconds = f.Elapsed;
            if (f.Elapsed < _nextLog) return;
            _nextLog = f.Elapsed + LogInterval;
            TensionLog.Add(Mathf.Clamp(f.Tension * 100f, 0f, 120f));
            HpLog.Add(Mathf.Clamp01(f.HP / f.MaxHP) * 100f);
        }

        // ---------------- 갱신 ----------------

        void UpdateVignette(FishFight f)
        {
            float target = 0f;
            if (f.IsDanger) target = 0.6f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 9f));
            else if (f.IsHighTension) target = Mathf.InverseLerp(FishFight.HighTension, FishFight.DangerTension, f.Tension) * 0.3f;
            _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, target, Time.deltaTime * 4f);
            var c = _vignette.color;
            c.a = _vignetteAlpha;
            _vignette.color = c;
        }

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
            RecordLog(f);
            UpdateHit(f);

            bool tips = Game.Profile.Data.showTips;

            UpdateVignette(f);

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
            _tensionPercent.text = $"{f.Tension * 100f:0}%";
            _tensionPercent.color = f.IsDanger ? new Color(1f, 0.45f, 0.4f) : Color.white;
            _tensionFill.fillAmount = tension;
            _tensionFill.color = f.IsDanger ? new Color(1f, 0.35f, 0.3f, 0.85f) : f.IsHighTension ? new Color(1f, 0.7f, 0.3f, 0.85f) : new Color(0.35f, 0.7f, 1f, 0.85f);
            _tensionMarker.anchorMin = _tensionMarker.anchorMax = new Vector2(tension, 0.5f);
            _tensionMarker.anchoredPosition = Vector2.zero;
            _tensionMarker.localScale = Vector3.one * (f.IsDanger ? 1.2f + Mathf.Sin(Time.time * 30f) * 0.15f : 1f);
            _highLabel.color = f.IsHighTension ? UIFactory.Warn : new Color(1, 1, 1, 0.35f);

            for (int i = 0; i < _pips.Length; i++)
                _pips[i].color = i < f.Combo ? new Color(0.3f, 0.75f, 1f) : new Color(1, 1, 1, 0.15f);

            // 카운터 찬스: 속도선 + 화살표
            bool counterChance = (f.CounterChance || DebugForceCounterChance) && !f.Landing;
            float pulse = 0.75f + Mathf.Sin(Time.time * 12f) * 0.25f;
            _speedLines.gameObject.SetActive(counterChance);
            if (counterChance)
            {
                var size = _root.rect.size;
                float side = Mathf.Sqrt(size.x * size.x + size.y * size.y) * 1.08f;
                _speedLines.rectTransform.sizeDelta = new Vector2(side, side);
                // 만화 집중선처럼 짧은 간격으로 각도가 툭툭 바뀌며 번쩍인다
                if (Time.time >= _speedLinesNext)
                {
                    _speedLinesNext = Time.time + 0.07f;
                    _speedLines.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    _speedLines.rectTransform.localScale = Vector3.one * Random.Range(1f, 1.06f);
                    var c = _speedLines.color;
                    c.a = Random.Range(0.45f, 0.65f);
                    _speedLines.color = c;
                }
            }
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
            else if (f.IsHighTension && f.DragLoad > 0.15f) tip = "물고기 반대쪽으로 계속 당기면 장력이 한계를 넘어 줄이 끊어집니다. 물고기 쪽으로 끌면 줄이 느슨해져요.";
            else tip = "왼쪽 버튼으로 줄을 감고, 오른쪽 버튼으로 풀어주세요. 장력을 HIGH 쪽에 두면 데미지가 커집니다.";
            _tipTitle.gameObject.SetActive(tips);
            _tipText.gameObject.SetActive(tips);
            _tipText.text = tip;

            bool slack = f.IsDanger || (f.IsHighTension && f.DragLoad > 0.2f);
            _slackHint.gameObject.SetActive(slack);
            if (slack)
            {
                float blink = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 8f));
                _slackHintText.text = f.FishSide < 0 ? "◀◀  물고기 쪽으로 끌어 늦추기" : "물고기 쪽으로 끌어 늦추기  ▶▶";
                _slackHintText.color = new Color(1f, 0.85f, 0.4f, blink);
                _slackHint.anchoredPosition = new Vector2(f.FishSide * (60f + Mathf.Repeat(Time.time * 1.5f, 1f) * 40f), 220f);
            }

            if (f.IsDanger) _warnText.text = "줄이 끊어지려 한다! 물고기 쪽으로 끌거나 오른쪽 버튼으로 풀어주세요";
            else if (f.Hook < 30f) _warnText.text = "바늘이 빠지려 한다! 줄을 감아 팽팽하게";
            else _warnText.text = "";
            _warnPill.gameObject.SetActive(_warnText.text.Length > 0);
            if (_warnText.text.Length > 0) _warnPill.sizeDelta = new Vector2(_warnText.preferredWidth + 56f, 42f);

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
                text.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.15f - age) * (big ? 5f : 3.5f));
            }
        }

        void ClearPops()
        {
            foreach (var p in _pops) Object.Destroy(p.text.gameObject);
            _pops.Clear();
        }
    }
}
