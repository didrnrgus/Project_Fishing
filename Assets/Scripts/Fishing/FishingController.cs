using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fishing.Angling
{
    public enum FishingState { Idle, Charging, Casting, Waiting, Bite, Fighting, Result }

    /// <summary>챔질 판정. 원작처럼 찌가 물에 많이 잠겼을 때 흔들수록 좋다.</summary>
    public enum HookGrade { None, Bad, SoSo, Good, Great, Perfect }

    /// <summary>
    /// 낚시 한 번의 흐름 (원작 조작을 따른다).
    ///  캐스팅: 던질 물을 좌클릭해 낚싯대를 꺼내 들고, 좌클릭을 누른 채 아래로 내려 젖혔다가 위로 쭉 올려 휘두른다. 휘두르는 속도가 비거리 (5m 미만은 실패).
    ///  챔질:   좌클릭을 누른 채 마우스를 좌우로 흔든다. 찌가 완전히 잠겼을 때 흔들면 PERFECT.
    ///  파이팅: 좌클릭 감기 / 우클릭 풀기 / 마우스 좌우 낚싯대 방향·카운터
    ///  랜딩:   랜딩 찬스가 뜨면 Space 또는 좌클릭 연타.
    /// </summary>
    public class FishingController : MonoBehaviour
    {
        // 캐스팅 제스처
        const float PullFull = 10f;     // 이만큼 당기면 최대 위력 (Mouse Y 축 단위, 약 100px)
        const float PushFull = 32f;     // 최근 0.15초 동안 이만큼 밀면 최대 비거리 (약 320px)
        const float PushWindow = 0.15f;
        // 챔질 제스처
        const float ShakeThreshold = 14f;
        // 본신 찌 모양·챔질 시간은 어종 입질 패턴이 정한다 (FishPatterns.BiteAt / BiteEnd)

        public FishingState State { get; private set; }
        public float CastPower { get; private set; }
        public float PullBack { get; private set; }
        public float CastDistance { get; private set; }
        public bool CastTargetIsWater { get; private set; }
        public FishFight Fight { get; private set; }
        public BiteResult PendingBite { get; private set; }
        public bool BigBite { get; private set; }
        public string CastCellLabel { get; private set; } = "";
        public Vector3 BobberPosition => _bobber != null ? _bobber.Position : Vector3.zero;
        /// <summary>찌가 잠긴 정도 (챔질 게이지 표시용). 1 = 다 잠김, 음수 = 찌올림으로 솟음.</summary>
        public float BobberSink => _bobber != null ? _bobber.SinkAmount : 0f;
        /// <summary>지금 챔질하면 받을 점수 0~1 (본신 중에만). 등급은 이것으로 매긴다.</summary>
        public float BiteQuality => State == FishingState.Bite ? _biteFrame.Quality : 0f;
        /// <summary>이번 입질의 찌 패턴 (찌올림·톡톡·오르내림 …).</summary>
        public BiteStyle CurrentBiteStyle => _biteStyle;
        BiteStyle _biteStyle;
        int _biteSeed;
        BiteFrame _biteFrame;
        float _biteDipMark;
        /// <summary>찌가 날아가는 동안 지금까지 날아간 거리(m). 화면의 큰 비거리 숫자가 따라 올라간다.</summary>
        public float FlyingDistance { get; private set; }
        /// <summary>챔질에 성공해 파이팅으로 넘어가기 직전(낚싯대를 홱 채는 연출 중).</summary>
        public bool Hooking => _hookAt > 0f;
        /// <summary>본신이 온 뒤 흐른 시간 (본신이 아니면 0).</summary>
        public float BiteTime => State == FishingState.Bite ? _biteTimer : 0f;
        /// <summary>원작 0:36.5~0:39.5: 본신 느낌표가 먼저 뜨고, 잠깐 뒤 챔질 클로즈업으로 끊어 넘어간다.</summary>
        public const float BiteCutDelay = 0.45f;
        /// <summary>예신(찌 깔짝임)이 한 번이라도 왔다: 원작 "손 끝으로부터 미세한 떨림이 느껴집니다."</summary>
        public bool Trembling => State == FishingState.Waiting && _nibbleSeen;
        bool _nibbleSeen;
        bool _userOrbited;
        /// <summary>찌 대기 중 손으로 시점을 돌렸는지 (자동 촬영이 원하는 시점으로 찍을 때 켠다).</summary>
        public bool UserOrbited { get => _userOrbited; set => _userOrbited = value; }
        /// <summary>랜딩 직후 결과 화면 전 환호 컷.</summary>
        public bool Cheering => _cheering;
        /// <summary>대기 중 좌클릭으로 찌·루어를 감고 있다 (캐릭터 릴 감기 동작).</summary>
        public bool ReelingIn => State == FishingState.Waiting && (_reeling || _lureMode && _lureReeling);
        public HookGrade LastGrade { get; private set; }
        public float LastGradeAt { get; private set; } = -10f;

        /// <summary>개발용: 파이팅을 자동으로 진행한다 (스크린샷/밸런스 확인).</summary>
        public bool Autoplay;
        /// <summary>파이팅 중 ESC 확인 창(FightQuitPanel)이 떠 있는 동안: 파이팅을 멈춘다.</summary>
        public bool FightPaused;
        /// <summary>자동 촬영용: 좌클릭을 누르고 있는 것처럼 찌를 감는다.</summary>
        public bool DebugReeling;
        /// <summary>자동 촬영용: 마우스 대신 낚싯대 각도를 정한다 (AimMin~AimMax).</summary>
        public void DebugRodAim(float pitch) => _aimPitch = Mathf.Clamp(pitch, AimMin, AimMax);
        /// <summary>자동 조작이 카운터 찬스에서 카운터를 시도할 확률(프레임당).</summary>
        public float AutoplayCounterChance = 0.08f;

        public event System.Action FightStarted;
        public event System.Action<FightOutcome> FightEnded;
        /// <summary>찌를 던진 순간. 인자는 비거리(m).</summary>
        public event System.Action<float> Casted;

        Bobber _bobber;
        FightRodView _rodView;
        LineRenderer _line;
        LineRenderer _aimArc;
        GameObject _marker;
        Renderer _markerRenderer;
        GameObject _playerRing;

        Vector3 _castTarget;
        Vector3 _castDir;
        BiteContext _ctx;
        string _cellName;
        readonly List<(float t, float dy)> _pushSamples = new List<(float, float)>();
        float _waitTimer;
        float _nextNibbleAt;
        int _nibblesLeft;
        bool _nibbleActive;
        float _nibbleWindowEnd;
        float _biteTimer;
        float _shake;
        bool _shakeSpent;
        float _lastDistance;
        float _lastTapAt = -10f;
        float _damagePopTimer;
        float _autoTapTimer;
        bool _cursorWasLocked;
        // 연출
        float _castShotUntil;
        float _hookAt;
        BiteResult _hookBite;
        float _hookDistance, _hookQuality;
        int _hookCombo;
        bool _cheering;
        float _swayYaw;
        /// <summary>찌를 기다리며 마우스 좌우로 튼 낚싯대 방향(도). 위아래(_aimPitch)와 함께 원작처럼 사방으로 겨눈다.</summary>
        float _aimYaw;
        const float AimYawMax = 30f;
        // 마우스 위아래로 직접 세우고 눕히는 낚싯대 각도 (던지기 전·찌 기다리는 동안). 내리면 줄이 처진다
        float _aimPitch = 35f;
        const float AimMin = 6f, AimMax = 80f;
        Coroutine _hitStop;
        // 루어 낚시 (스푼 루어·에기·플라이): 감으면서 액션을 줘야 문다. 배 위에서는 트롤링
        bool _lureMode;
        float _lastTwitchAt = -10f;
        bool _trollWarned;
        /// <summary>마지막 액션(저킹) 시각 — HUD가 "액션!"을 띄운다.</summary>
        public float LastTwitchAt => _lastTwitchAt;
        /// <summary>지금 미끼가 루어 계열인지 (대기 방식이 다르다).</summary>
        public bool LureMode => State == FishingState.Waiting && _lureMode;
        /// <summary>배를 몰면서 줄을 끌 수 있는 상태 (루어나 생미끼를 던져 둔 채).</summary>
        public bool CanTroll => State == FishingState.Waiting && Game.Boat != null && Game.Boat.Riding
                                && (_lureMode || Game.Profile.Bait.Kind == BaitKind.Minnow);
        /// <summary>지금 트롤링 중인지 (배가 움직이는 중).</summary>
        public bool Trolling => CanTroll && Mathf.Abs(Game.Boat.Speed) > 0.6f;

        public static bool IsLureBait(BaitKind k) => k == BaitKind.Lure || k == BaitKind.Egi || k == BaitKind.Fly;

        // 대기 중 릴 감기
        int _waitCol, _waitRow;
        bool _waitInSpot;
        bool _reeling, _lureReeling;
        float _reelFxAt;

        void Start()
        {
            _bobber = Bobber.Create();
            _rodView = FightRodView.Create(Game.CameraRig.Cam);

            _line = new GameObject("FishingLine").AddComponent<LineRenderer>();
            _line.positionCount = 14;
            _line.widthMultiplier = 0.01f;
            _line.sharedMaterial = Mats.Unlit(new Color(0.9f, 0.95f, 0.9f));
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.enabled = false;

            // 원작의 하늘색 캐스팅 궤적 리본
            _aimArc = new GameObject("AimArc").AddComponent<LineRenderer>();
            _aimArc.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _aimArc.alignment = LineAlignment.TransformZ;
            _aimArc.positionCount = 16;
            _aimArc.widthCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.85f, 0.45f), new Keyframe(1f, 0.05f));
            _aimArc.sharedMaterial = Mats.Particle(Color.white);
            _aimArc.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(new Color(0.2f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.4f, 0.95f, 1f), 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.7f), new GradientAlphaKey(1f, 1f) }
            };
            _aimArc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _aimArc.enabled = false;

            _marker = Mats.Prim(PrimitiveType.Cylinder, "CastMarker", null, Vector3.zero, new Vector3(1.2f, 0.01f, 1.2f),
                Mats.Transparent(new Color(0.3f, 1f, 0.7f, 0.45f), 0.5f, shared: false), collider: false);
            _markerRenderer = _marker.GetComponent<Renderer>();
            _marker.SetActive(false);

            _playerRing = Mats.Prim(PrimitiveType.Cylinder, "PlayerRing", null, Vector3.zero, new Vector3(2.2f, 0.005f, 2.2f),
                Mats.NewRipple(), collider: false);
            _playerRing.GetComponent<Renderer>().sharedMaterial.SetColor("_TintColor", new Color(0.3f, 0.9f, 1f, 0.6f));
            _playerRing.SetActive(false);
        }

        bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool UiBlocking => Game.UI != null && Game.UI.BlockingPanelOpen;

        /// <summary>
        /// 마우스를 위로 올리면 낚싯대를 세우고, 내리면 눕힌다 (오른쪽 드래그로 시점을 돌리는 중이나 창이 열렸을 땐 그대로).
        /// 예전에는 던지기 전 35도, 찌를 기다릴 때 28도로 고정이라 갑갑했다. 약 300px이면 끝에서 끝까지.
        /// 찌를 기다릴 때는 마우스 좌우로 낚싯대를 옆으로도 튼다 (예전엔 위아래로만 움직였다). 좌클릭을 누른 채 흔드는 건 챔질 동작(SwayRod)이라 뺀다.
        /// </summary>
        float AimRod()
        {
            if (!UiBlocking && !Input.GetMouseButton(1))
            {
                _aimPitch = Mathf.Clamp(_aimPitch + Input.GetAxis("Mouse Y") * Settings.FightSensitivity * 2.5f, AimMin, AimMax);
                if (State == FishingState.Waiting && !Input.GetMouseButton(0))
                    _aimYaw = Mathf.Clamp(_aimYaw + Input.GetAxis("Mouse X") * Settings.FightSensitivity * 2.5f, -AimYawMax, AimYawMax);
            }
            Game.Player.RodPitch = _aimPitch;
            Game.Player.RodPitchSpeed = 14f;
            return _aimPitch;
        }

        void Update()
        {
            if (Game.Mode == GameMode.Lobby) return;
            UpdateThirdPersonRod();
            switch (State)
            {
                case FishingState.Idle: UpdateIdle(); break;
                case FishingState.Charging: UpdateCharging(); break;
                case FishingState.Casting: UpdateCasting(); break;
                case FishingState.Waiting: UpdateWaiting(); break;
                case FishingState.Bite: UpdateBite(); break;
                case FishingState.Fighting:
                    if (FightPaused) Game.Sound.SetReel(0f, 0f);
                    else UpdateFighting();
                    break;
            }
            UpdateLine();
            UpdateCameraShot();
        }

        // ---------- Idle / Charging(캐스팅 제스처) / Casting ----------

        void UpdateIdle()
        {
            Game.Player.MovementLocked = false;
            Game.Player.RodHidden = true; // 원작: 돌아다닐 때는 낚싯대를 들고 있지 않다
            if (UiBlocking || PointerOverUI) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (Game.Boat != null && Game.Boat.Summoning) return;
            if (Game.Boat != null && Game.Boat.BlocksCasting)
            {
                // 원작: 엔진을 멈춰야 낚시한다
                Game.Toast($"엔진이 켜져 있습니다. {Settings.KeyName(KeyAction.Engine)}로 엔진을 끄고 낚시하세요.", ToastKind.Warn);
                return;
            }

            var p = Game.Profile;
            if (p.Count(p.Bait.Id) <= 0)
            {
                Game.Toast($"{p.Bait.Name}이(가) 없습니다. 숫자키로 다른 미끼를 고르거나 상점(F12)에서 사세요.", ToastKind.Warn);
                return;
            }
            if (p.Data.stamina <= 0f)
            {
                Game.Toast("체력이 바닥났습니다. 살림망(Z)에서 요리를 하거나 김밥을 드세요.", ToastKind.Warn);
                return;
            }

            // 겨울 한강 얼음 위: 클릭하면 낚싯대만 꺼내 들고, 누른 채 살짝 내리면 발 앞에 구멍을 뚫고 채비를 내린다
            _iceReady = SeasonFx.OnIce(Game.Player.transform.position);
            Vector3 aimPoint;
            if (_iceReady)
            {
                Game.Player.FaceYaw(Game.CameraRig.Yaw);
                aimPoint = Game.Player.transform.position + Game.Player.Model.forward * 1.4f;
            }
            // 원작: 던지고 싶은 물을 클릭하면 그쪽을 보고 낚싯대를 꺼내 든다 (아직 던지지 않는다)
            else if (!PickWater(out aimPoint))
            {
                if (Time.time - _waterHintAt > 3f)
                {
                    _waterHintAt = Time.time;
                    Game.Toast("던지고 싶은 물 위를 클릭하세요.");
                }
                return;
            }
            _bobber.FloatOffset = 0f;
            State = FishingState.Charging;
            ResetGesture();
            _gestureArmed = false;   // 방향을 정한 이 클릭은 떼고 나서 다시 눌러야 제스처가 시작된다
            SetAimPoint(aimPoint);
            Game.Player.MovementLocked = true;
            Game.Player.RodHidden = false;
            Game.Player.ExtendRod(0.4f);
            Game.Player.RodPitch = ReadyPitch;
            Game.Player.RodPitchSpeed = 10f;
            Game.Player.RodYaw = 0f;
            Game.Player.BodyLean = 0f;
            _marker.SetActive(true);
            _aimArc.enabled = true;
            _playerRing.SetActive(true);
        }

        // ---------- 캐스팅 제스처 ----------
        // 낚싯대를 꺼낸 뒤 좌클릭을 누른 채 마우스를 아래로 내리면 낚싯대가 어깨 너머로 올라가고,
        // 그대로 위로 쭉 올리면 낚싯대가 앞으로 내려오며 던진다. 앞으로 휘두르는 속도가 곧 비거리 (5m가 안 되면 실패).
        const float ReadyPitch = 40f;   // 꺼내 든 자세
        const float MinPull = 2.5f;     // 이만큼은 젖혀야 휘두를 수 있다
        const float FailDistance = 5f;
        float _rodBack;                 // 지금 젖힌 양 (Mouse Y 단위)
        float _peakBack;                // 이번 제스처에서 가장 많이 젖힌 양
        float _peakPush;                // 앞으로 휘두르는 동안 가장 빨랐던 속도 (Mouse Y 단위/초)
        float _swingStart = -1f;        // 앞으로 넘어오기 시작한 시각
        const float PushSpeedFull = PushFull / PushWindow; // 이 속도(약 2100px/초)로 휘두르면 최대 비거리
        bool _gestureArmed;
        bool _gestureHeld;
        Vector3 _aimPoint;
        Vector3? _pressAim;
        bool _iceReady;                 // 얼음 위에서 꺼내 든 상태 (휘두르지 않고 살짝 내리면 구멍에 내린다)
        const float IceDrop = 1f;       // 이만큼(약 10px) 내리면 채비를 내린다
        float _waterHintAt = -10f;

        /// <summary>얼음 위에서 낚싯대를 꺼내 들고 구멍에 내리기를 기다린다.</summary>
        public bool IceReady => State == FishingState.Charging && _iceReady;

        /// <summary>좌클릭을 누른 채 낚싯대를 뒤로 젖히고 있다 (캐릭터가 어깨 너머로 드는 자세).</summary>
        public bool CastPulling => State == FishingState.Charging && !_iceReady && _gestureHeld && _peakBack >= MinPull;

        void ResetGesture()
        {
            _rodBack = _peakBack = _peakPush = 0f;
            _swingStart = -1f;
            _gestureHeld = false;
            _pressAim = null;
            PullBack = 0f;
            CastPower = 0f;
            CastDistance = 0f;
            _pushSamples.Clear();
        }

        void SetAimPoint(Vector3 p)
        {
            _aimPoint = p;
            var d = p - Game.Player.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) Game.Player.FaceYaw(Quaternion.LookRotation(d).eulerAngles.y);
            _castDir = Game.Player.Model.forward;
        }

        /// <summary>마우스 커서 아래가 물이면 그 수면 위치. 땅·건물을 클릭했거나 발밑이면 false.</summary>
        bool PickWater(out Vector3 point)
        {
            point = Vector3.zero;
            var cam = Game.CameraRig != null ? Game.CameraRig.Cam : null;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, FieldMap.WaterY, 0f));
            if (!plane.Raycast(ray, out float enter) || enter > 400f) return false;
            // 수면보다 앞에서 땅·물체에 먼저 닿으면 물을 클릭한 게 아니다
            foreach (var hit in Physics.RaycastAll(ray, enter - 0.2f, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignore(hit.collider) && hit.point.y > FieldMap.WaterY + 0.25f) return false;
            point = ray.GetPoint(enter);
            point.y = FieldMap.WaterY;
            var flat = point - Game.Player.transform.position;
            flat.y = 0f;
            return flat.magnitude >= 1.5f && FieldMap.IsWater(point);
        }

        void UpdateCharging()
        {
            var player = Game.Player;
            var rod = Game.Profile.Rod;
            float maxRange = rod.CastRange + Game.Profile.Costume.Cast;
            if (UiBlocking) { ToIdle(); return; }
            // 이동하거나 R: 낚싯대를 거두고 다시 돌아다닌다
            if (WantsRetrieve) { ToIdle(); return; }

            bool held = Input.GetMouseButton(0);
            if (!held) _gestureArmed = true;
            if (_gestureArmed && Input.GetMouseButtonDown(0) && !PointerOverUI)
            {
                ResetGesture();
                _gestureHeld = true;
                // 누르고 그냥 떼면 그 물로 방향을 다시 정한다
                if (PickWater(out var p)) _pressAim = p;
            }

            float dy = _gestureHeld ? Input.GetAxis("Mouse Y") * Settings.FightSensitivity : 0f;
            float now = Time.time;
            if (_gestureHeld)
            {
                // 아래로 내리면 젖히고, 위로 올리면 앞으로 넘어온다. 낚싯대가 마우스를 그대로 따라간다
                _rodBack = Mathf.Clamp(_rodBack - dy, 0f, PullFull * 1.2f);
                if (_rodBack >= _peakBack) { _peakBack = _rodBack; _swingStart = -1f; _peakPush = 0f; } // 아직 젖히는 중
                _pushSamples.Add((now, Mathf.Max(0f, dy)));
                _pushSamples.RemoveAll(s => now - s.t > PushWindow);
                if (_peakBack >= MinPull && _rodBack < _peakBack - 0.3f)
                {
                    // 앞으로 넘어오는 속도 = 최근(최대 0.15초) 위로 올린 양 / 걸린 시간. 젖힌 양과 상관없이 빠를수록 멀리
                    if (_swingStart < 0f) _swingStart = now - Time.deltaTime;
                    float span = Mathf.Clamp(now - _swingStart, Mathf.Max(Time.deltaTime, 0.008f), PushWindow);
                    _peakPush = Mathf.Max(_peakPush, PushSum(now) / span);
                }
            }
            else _rodBack = Mathf.MoveTowards(_rodBack, 0f, Time.deltaTime * 20f);

            PullBack = _peakBack;
            float pullFactor = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(_peakBack / PullFull));
            CastPower = Mathf.Clamp01(_peakPush / PushSpeedFull) * pullFactor;
            // 휘두르는 중일 때만 비거리 숫자가 따라 올라간다
            CastDistance = _peakPush > 0f ? maxRange * CastPower : 0f;

            // 원작 0:01~0:02: 젖힐수록 낚싯대를 어깨 너머 뒤로, 상체도 뒤로
            float back = Mathf.Clamp01(_rodBack / PullFull);
            player.RodPitch = Mathf.Lerp(ReadyPitch, 155f, back);
            player.RodPitchSpeed = 18f;
            player.BodyLean = Mathf.Lerp(0f, 20f, back);
            player.RodYaw = Mathf.Lerp(0f, -14f, back);
            UpdateAim(_aimPoint);

            if (!_gestureHeld) return;
            bool released = !held;
            if (_iceReady)
            {
                if (_rodBack >= IceDrop)
                {
                    _gestureHeld = false;
                    HideAim();
                    StartIceFishing();
                }
                else if (released) _gestureHeld = false;
                return;
            }
            // 젖혔다가 거의 다 넘어왔으면 그 순간 던진다. 휘두르다 놓아도 던진다
            bool swung = _peakBack >= MinPull && _rodBack <= _peakBack * 0.3f;
            bool flick = released && _peakBack >= MinPull && _peakPush > 5f;
            if (!swung && !flick)
            {
                if (released)
                {
                    if (_peakBack < MinPull && _pressAim.HasValue) SetAimPoint(_pressAim.Value);
                    _gestureHeld = false;
                    _pressAim = null;
                }
                return;
            }
            _gestureHeld = false;
            _gestureArmed = !held; // 던진 뒤에도 누르고 있으면 떼고 다시 눌러야 한다

            if (CastDistance < FailDistance)
            {
                // 너무 느리게 휘둘렀다: 찌가 발 앞에 툭. 낚싯대는 든 채로 다시 시도한다
                player.Rod.Kick(-120f);
                Game.Sound.Whoosh();
                Game.Toast($"캐스팅 실패! ({CastDistance:0.0}m) 낚싯대를 더 빠르게 휘두르세요.", ToastKind.Warn);
                ResetGesture();
                return;
            }
            {
                _castTarget = player.transform.position + _castDir * CastDistance;
                _castTarget.y = FieldMap.WaterY + 0.01f;
                if (!FieldMap.IsWater(_castTarget))
                {
                    Game.Toast("너무 멀리 던져 물 밖으로 나갑니다. 조금 약하게 휘두르세요.", ToastKind.Warn);
                    ResetGesture();
                    return;
                }
                if (CastBlocked(player.RodTip.position, _castTarget, out string blocker))
                {
                    Game.Toast($"{blocker}에 걸려서 던질 수 없습니다. 앞이 트인 쪽으로 던지세요.", ToastKind.Warn);
                    ResetGesture();
                    return;
                }
                HideAim();
                Game.Player.RodPitch = 2f;
                Game.Player.RodPitchSpeed = 40f;   // 채찍처럼 앞으로 휙
                Game.Player.BodyLean = -16f;
                Game.Player.RodYaw = 0f;
                Game.Player.Rod.Kick(-260f);       // 휘두르는 힘에 초릿대가 뒤로 휘었다 튕긴다
                _bobber.Launch(Game.Player.RodTip.position, _castTarget);
                Game.Sound.Whoosh();
                Game.CameraRig.Shake(0.25f);
                Game.CameraRig.Punch(-3f);
                FlyingDistance = 0f;
                Casted?.Invoke(CastDistance);
                State = FishingState.Casting;
                // 원작: 체력이 낮으면 "체력이 부족하여 능력치가 감소합니다" 경고
                if (Game.Profile.StaminaTier > 0)
                    Game.Toast(Game.Profile.StaminaTier == 2 ? "체력이 바닥나 능력치가 크게 떨어집니다! 음식을 드세요. (가방 I)"
                        : "체력이 부족하여 능력치가 감소합니다. 음식을 드세요. (가방 I)", ToastKind.Warn);
            }
        }

        bool _iceFishing;

        void StartIceFishing()
        {
            Game.Player.MovementLocked = true;
            Game.Player.RodHidden = false;
            _castDir = Game.Player.Model.forward; // 꺼내 들 때 본 방향 그대로
            var hole = Game.Player.transform.position + _castDir * 1.4f;
            hole.y = FieldMap.WaterY;
            SeasonFx.MakeHole(hole);
            _bobber.FloatOffset = 0.22f;
            CastDistance = 1.4f;
            _castTarget = hole;
            Game.Player.RodPitch = 25f;
            _bobber.Launch(Game.Player.RodTip.position, hole + Vector3.up * 0.25f);
            Game.Sound.Splash(0.4f);
            Game.Toast("얼음 구멍을 뚫고 채비를 내렸습니다. 빙어가 떼로 다녀요!", ToastKind.Info);
            FlyingDistance = 0f;
            State = FishingState.Casting;
        }

        /// <summary>개발/테스트용: 얼음 위에서 얼음낚시를 시작한다.</summary>
        public void DebugIceFish()
        {
            if (State != FishingState.Idle) ToIdle();
            Game.Player.FaceYaw(Game.CameraRig.Yaw);
            StartIceFishing();
        }

        float PushSum(float now)
        {
            float sum = 0f;
            foreach (var s in _pushSamples)
                if (now - s.t <= PushWindow) sum += s.dy;
            return sum;
        }

        /// <summary>클릭한 물 지점까지 하늘색 궤적과 표시. 실제 비거리는 휘두르는 속도로 정해진다.</summary>
        void UpdateAim(Vector3 target)
        {
            var origin = Game.Player.transform.position;
            target.y = FieldMap.WaterY + 0.03f;
            var flatT = target - origin;
            flatT.y = 0f;
            float distance = flatT.magnitude;
            CastTargetIsWater = FieldMap.IsWater(target) && !CastBlocked(Game.Player.RodTip.position, target, out _);

            _marker.transform.position = target;
            _markerRenderer.material.color = CastTargetIsWater ? new Color(0.3f, 1f, 0.9f, 0.45f) : new Color(1f, 0.3f, 0.3f, 0.45f);
            _playerRing.transform.position = origin + Vector3.up * 0.03f;

            var right = Vector3.Cross(Vector3.up, _castDir);
            var start = origin + _castDir * 1.2f;
            start.y = Mathf.Max(origin.y, FieldMap.WaterY) + 0.05f;
            for (int i = 0; i < _aimArc.positionCount; i++)
            {
                float t = i / (float)(_aimArc.positionCount - 1);
                var p = Vector3.Lerp(start, target, t) + right * Mathf.Sin(t * Mathf.PI) * distance * 0.06f;
                p.y = Mathf.Lerp(start.y, target.y, t);
                _aimArc.SetPosition(i, p);
            }
            // 원작: 궤적을 따라 흰 별 파티클이 목표 지점으로 흘러간다
            if (Time.time >= _nextSparkle)
            {
                _nextSparkle = Time.time + 0.03f;
                for (int k = 0; k < 3; k++)
                {
                    float t = Mathf.Repeat(Time.time * 0.8f + k / 3f, 1f);
                    var p = Vector3.Lerp(start, target, t) + right * Mathf.Sin(t * Mathf.PI) * distance * 0.06f + Vector3.up * 0.12f;
                    Effects.Sparkle(p, Mathf.Lerp(0.16f, 0.32f, t));
                }
            }
        }

        float _nextSparkle;

        /// <summary>
        /// 찌가 날아갈 궤적(Bobber.Launch와 같은 포물선)에 막는 것이 있는지. 등대·NPC·건물·나무·언덕 등.
        /// 내 캐릭터, 타고 있는 배, 찌는 빼고, 출발점 1.5m 안(낚싯대 바로 앞)도 뺀다.
        /// </summary>
        public static bool CastBlocked(Vector3 from, Vector3 to, out string what)
        {
            what = null;
            to.y = FieldMap.WaterY;
            float dist = Vector3.Distance(from, to);
            float arc = dist * 0.2f + 1f;
            const int steps = 14;
            var prev = from;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                var p = Vector3.Lerp(from, to, t);
                p.y += Mathf.Sin(t * Mathf.PI) * arc;
                if (i == steps) p.y = FieldMap.WaterY + 0.3f; // 수면에 닿기 직전까지만
                var seg = p - prev;
                foreach (var hit in Physics.RaycastAll(prev, seg.normalized, seg.magnitude, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (Ignore(hit.collider) || Vector3.Distance(hit.point, from) < 1.5f) continue;
                    what = Describe(hit.collider);
                    return true;
                }
                prev = p;
            }
            return false;
        }

        static bool Ignore(Collider c)
        {
            var t = c.transform;
            if (Game.Player != null && t.IsChildOf(Game.Player.transform)) return true;
            if (Game.Boat != null && Game.Boat.Hull != null && t.IsChildOf(Game.Boat.Hull)) return true;
            return c.name == "Water" || c.name.StartsWith("Bobber") || World.HarborKit.IsLowWall(c); // 파라펫 너머로 던진다
        }

        /// <summary>막은 물체의 이름 (안내 문구용, 받침 조사가 맞게 '…에 걸려서').</summary>
        static string Describe(Collider c)
        {
            if (c is TerrainCollider) return "언덕";
            var npc = c.GetComponentInParent<Fishing.Characters.NpcAngler>();
            if (npc != null) return npc.Look?.Name ?? "사람";
            for (var t = c.transform; t != null; t = t.parent)
            {
                string n = t.name;
                if (n.Contains("Lighthouse")) return "등대";
                if (n.Contains("Pavilion")) return "정자";
                if (n.Contains("TackleShop")) return "낚시점";
                if (n.StartsWith("tree_") || n.Contains("Camellia") || n.Contains("Tree")) return "나무";
                if (n.StartsWith("building")) return "건물";
                if (n.StartsWith("light-") || n.Contains("Pole") || n.Contains("Lamp")) return "가로등";
                if (n.StartsWith("boat") || n.Contains("Boat")) return "배";
                if (n.Contains("Stack") || n.Contains("Rock") || n.Contains("Boulder")) return "바위";
                if (n.Contains("Bridge") || n.Contains("Pier") || n.Contains("Pylon")) return "다리";
                if (n.Contains("Crane") || n.Contains("Warehouse")) return "항구 시설";
            }
            return "장애물";
        }

        void HideAim()
        {
            _marker.SetActive(false);
            _aimArc.enabled = false;
            _playerRing.SetActive(false);
        }

        void UpdateCasting()
        {
            var flat = _bobber.Position - Game.Player.transform.position;
            flat.y = 0f;
            FlyingDistance = Mathf.Max(FlyingDistance, flat.magnitude);
            if (!_bobber.Landed) return;
            FlyingDistance = CastDistance;
            _lineLen = -1f; // 줄 길이는 착수한 뒤 첫 프레임에 잰다
            // 착수: 낚싯대를 거두며 숨 고르기. 카메라는 잠시 찌를 본 뒤 원래 자리로 (그다음은 마우스로 세우고 눕힌다)
            _aimPitch = 28f;
            Game.Player.RodPitch = 28f;
            Game.Player.RodPitchSpeed = 6f;
            Game.Player.BodyLean = 0f;
            _castShotUntil = Time.time + 0.9f;
            BeginWait(1f);
        }

        /// <summary>낚시 단계마다 카메라 구도를 정한다 (원작 연출).</summary>
        void UpdateCameraShot()
        {
            var rig = Game.CameraRig;
            if (rig == null) return;
            var shot = Fishing.PlayerSystems.CameraRig.ShotKind.Follow;
            switch (State)
            {
                case FishingState.Charging:
                    shot = Fishing.PlayerSystems.CameraRig.ShotKind.Cast;
                    rig.CastLookAt = null;
                    break;
                case FishingState.Casting:
                    shot = Fishing.PlayerSystems.CameraRig.ShotKind.Cast;
                    rig.CastLookAt = _bobber.Position;
                    break;
                case FishingState.Waiting:
                    if (Time.time < _castShotUntil) { shot = Fishing.PlayerSystems.CameraRig.ShotKind.Cast; rig.CastLookAt = _bobber.Position; }
                    else
                    {
                        // 원작 구도로 찌를 기다린다. 오른쪽 버튼으로 시점을 돌리면 그때부터는 손으로 돌린 시점
                        if (Input.GetMouseButton(1) && !UiBlocking) _userOrbited = true;
                        if (!_userOrbited && !_lureMode && !Trolling)
                        {
                            shot = Fishing.PlayerSystems.CameraRig.ShotKind.Wait;
                            rig.BiteLookAt = _bobber.Position;
                        }
                    }
                    break;
                case FishingState.Bite:
                    // 느낌표가 뜨는 동안은 그대로, 그다음 어깨 뒤 낮은 클로즈업으로 끊어 넘어간다
                    if (_biteTimer >= BiteCutDelay || _hookAt > 0f) shot = Fishing.PlayerSystems.CameraRig.ShotKind.Bite;
                    rig.BiteLookAt = _bobber.Position;
                    break;
                case FishingState.Result:
                    if (_cheering) shot = _resultShot;
                    break;
            }
            rig.Shot = shot;
        }

        /// <summary>잠깐 시간을 느리게 해서 '쾅' 하는 손맛을 준다 (히트 스톱).</summary>
        public void HitStop(float seconds, float scale)
        {
            if (_hitStop != null) StopCoroutine(_hitStop);
            _hitStop = StartCoroutine(HitStopRoutine(seconds, scale));
        }

        System.Collections.IEnumerator HitStopRoutine(float seconds, float scale)
        {
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = 1f;
        }

        // ---------- Waiting / Bite (챔질 제스처) ----------

        void BeginWait(float slowdown)
        {
            FieldMap.TryCell(_bobber.Position, out int col, out int row);
            col = Mathf.Clamp(col, 0, FieldMap.Cols - 1);
            row = Mathf.Clamp(row, 0, FieldMap.Rows - 1);
            _cellName = FieldMap.CellName(col, row);
            _ctx = new BiteContext
            {
                Habitat = FieldMap.HabitatAt(col, row),
                Bait = Game.Profile.Bait.Kind,
                Band = Game.Clock.Band,
                Weather = Game.Weather.Current,
                School = Game.Schools.At(_bobber.Position, col, row),
                MapId = FieldMap.Id,
                BaitPotency = Game.Profile.Bait.Potency,
                IceFishing = _iceFishing = SeasonFx.OnIce(Game.Player.transform.position),
                Season = Seasons.Current,
                UseSeason = true,
                FarSea = FieldMap.IsFarSea(_bobber.Position),
                // 먼바다 포인트는 대물이 잘 문다
                BigFishBonus = Game.Profile.Costume.BigFish + (FieldMap.IsFarSea(_bobber.Position) ? 0.08f : 0f),
            };
            // 소용돌이 한가운데에 떨어뜨리면 입질이 더 빠르다
            _waitCol = col;
            _waitRow = row;
            _waitInSpot = _ctx.School != null && _ctx.School.InSpot(_bobber.Position);
            CastCellLabel = $"{_cellName} · {FieldMap.HabitatName(_ctx.Habitat)}" + (_ctx.School != null ? $" · {_ctx.School.Name}({_ctx.School.SizeLabel})" : "")
                + (_waitInSpot ? " · 어군 한가운데!" : "") + (FieldMap.IsFarSea(_bobber.Position) ? " · 먼바다 (대물↑)" : "");
            PendingBite = BiteSelector.Pick(_ctx);
            // 재도전(복수혈전): 놓친 그 고기가 다시 온다. 입질도 빠르다
            FishSpecies rs = null;
            float rl = 0f;
            bool revenge = !_ctx.IceFishing && CatchRewards.TryRevengeBite(FieldMap.Id, out rs, out rl);
            if (revenge)
            {
                PendingBite = new BiteResult { Species = rs, Length = rl };
                CastCellLabel += $" · <color=#7fd8ff>재도전 X{CatchRewards.Mult}</color>";
            }
            BigBite = false;

            _waitTimer = BiteSelector.WaitSeconds(_ctx) * slowdown * (_waitInSpot ? 0.7f : 1f) * Tide.WaitFactor * Game.Weather.WaitFactor; // 서해: 들물이면 빨리, 흐리거나 비 오면 빨리
            if (revenge) _waitTimer *= 0.5f;
            var (nibMin, nibMax) = FishPatterns.Nibbles(FishPatterns.BiteOf(PendingBite.Species));
            _nibblesLeft = Random.Range(nibMin, nibMax);
            _lureMode = IsLureBait(Game.Profile.Bait.Kind);
            _bobber.SetLureLook(_lureMode, Game.Profile.Bait.Kind);
            if (_lureMode)
            {
                _nibblesLeft = 0;          // 포식어는 깔짝이지 않고 덥석 문다
                _waitTimer *= 0.8f;
            }
            _nextNibbleAt = _waitTimer * Random.Range(0.3f, 0.7f);
            _nibbleActive = false;
            _nibbleSeen = false;
            _userOrbited = false;
            Game.Player.BodyLean = 0f;
            _shake = 0f;
            _shakeSpent = Input.GetMouseButton(0);
            _bobber.ResetFloat();
            State = FishingState.Waiting;
        }

        /// <summary>3인칭 낚싯대 휨: 줄이 걸린 무게, 본신 때 빨려 들어가는 힘.</summary>
        void UpdateThirdPersonRod()
        {
            var player = Game.Player;
            if (player == null || player.Rod == null) return;
            if (State == FishingState.Waiting || State == FishingState.Bite || State == FishingState.Casting)
            {
                var dir = _bobber.Position - player.RodTip.position;
                float bend = State == FishingState.Bite ? 22f + Mathf.Sin(Time.time * 9f) * 6f
                           : State == FishingState.Waiting ? 2f + LineTension * 4f + LineStrain * 16f : 2f;
                player.RodBendTarget = bend;
                player.RodPullDir = dir;
            }
        }

        // 배 위에서 트롤링할 수 있으면 WASD는 배 운전이라 찌 회수로 치지 않는다
        bool WantsRetrieve => Settings.Down(KeyAction.Retrieve) || (Game.Player.MovedThisFrame && !CanTroll);

        /// <summary>챔질 준비: 버튼을 누른 채 마우스를 흔들면 낚싯대가 그대로 좌우로 흔들린다 (원작 0:30).</summary>
        void SwayRod(bool bite)
        {
            var player = Game.Player;
            float mx = Input.GetMouseButton(0) && !UiBlocking ? Input.GetAxis("Mouse X") * Settings.FightSensitivity : 0f;
            _swayYaw = Mathf.Clamp(_swayYaw * Mathf.Exp(-5f * Time.deltaTime) + mx * 7f, -32f, 32f);
            // 가만히 들고 있어도 손이 조금씩 흔들려 초릿대가 좌우로 살랑인다. 입질이면 물고기가 찌 쪽으로 툭툭 당긴다
            float tremor = (Mathf.PerlinNoise(Time.time * 0.6f, 4.2f) - 0.5f) * 3f;
            if (bite) tremor += Mathf.Sin(Time.time * 13f) * Mathf.Sin(Time.time * 2.3f) * 4f;
            player.RodYaw = Mathf.Clamp(_aimYaw + _swayYaw + tremor, -45f, 45f);
            player.BodyLean = bite ? -6f + Mathf.Abs(_swayYaw) * 0.2f : 0f;
            if (bite) player.RodPitch = 40f + Mathf.Sin(Time.time * 7f) * 3f;
        }

        /// <summary>좌클릭을 누른 채 좌우로 흔들었는지. 한 번 판정되면 버튼을 뗄 때까지 다시 판정하지 않는다.</summary>
        bool DetectShake()
        {
            if (UiBlocking) return false;
            if (!Input.GetMouseButton(0))
            {
                _shakeSpent = false;
                _shake = 0f;
                return false;
            }
            if (_shakeSpent) return false;
            _shake = _shake * Mathf.Exp(-6f * Time.deltaTime) + Mathf.Abs(Input.GetAxis("Mouse X") * Settings.FightSensitivity);
            if (_shake < ShakeThreshold) return false;
            _shakeSpent = true;
            _shake = 0f;
            return true;
        }

        void UpdateWaiting()
        {
            if (WantsRetrieve) { Retrieve("찌를 회수했습니다."); return; }
            if (_lureMode || Trolling) { UpdateLureWait(); return; }
            if (Time.time >= _castShotUntil) { SwayRod(false); AimRod(); }
            // 얼음 구멍에서는 감을 줄이 없다 (누르고 흔들면 챔질만)
            if (!_iceFishing && UpdateReel()) { if (State == FishingState.Waiting && DetectShake()) FalseHook(); return; }

            _waitTimer -= Time.deltaTime;
            if (_nibbleActive && Time.time > _nibbleWindowEnd) _nibbleActive = false;

            if (_nibblesLeft > 0 && _waitTimer <= _nextNibbleAt)
            {
                _nibblesLeft--;
                _nibbleSeen = true;
                _bobber.Nibble();
                Game.Sound.Tick();
                Game.Player.Rod.Kick(70f); // 예신: 초릿대가 톡
                _nibbleActive = true;
                _nibbleWindowEnd = Time.time + 0.3f;
                _nextNibbleAt = _waitTimer - Random.Range(0.8f, 2f);
            }

            if (_waitTimer <= 0f && _nibblesLeft <= 0)
            {
                StartBite(FishPatterns.BiteOf(PendingBite.Species));
                Game.Player.BodyLean = 12f;  // 원작 챔질 클로즈업: 무릎을 굽히고 앞으로 숙여 버틴다
                Game.Player.Rod.Kick(160f); // 본신: 초릿대가 쭉 빨려 들어간다
                BigBite = PendingBite.IsTrophy || PendingBite.Species.Rarity >= 4;
                Game.Sound.Plop();
                if (BigBite) Game.Sound.Heartbeat();
                Rumble.Pulse(BigBite ? 0.45f : 0.25f, 0.35f, 0.18f);   // 본신: 톡
                Game.CameraRig.Shake(0.35f);
                return;
            }

            if (DetectShake()) FalseHook();
        }

        /// <summary>
        /// 루어 낚시 대기: 가만히 두면 거의 안 문다 (플라이는 흘려도 조금 문다). 좌클릭으로 감는 동안 입질 시계가 가고,
        /// 감으면서 마우스를 위로 툭 치면(저킹) "액션!" — 입질이 확 당겨진다. 발밑까지 감으면 다시 던진다.
        /// 배를 천천히(14km/h 이하) 몰면 줄이 배 뒤로 끌려가며 트롤링이 된다 (생미끼도 된다).
        /// </summary>
        void UpdateLureWait()
        {
            if (Time.time >= _castShotUntil) SwayRod(false);
            var kind = Game.Profile.Bait.Kind;
            float rate;
            if (Trolling)
            {
                _lureReeling = false;
                var boat = Game.Boat;
                float dist = Mathf.Clamp(CastDistance, 10f, 25f);
                var target = boat.SternPosition - boat.Forward * dist;
                if (FieldMap.IsWater(target)) _bobber.DragTo(target);
                float sp = Mathf.Abs(boat.Speed);
                rate = sp < 4f ? 1.6f : 0f;
                if (sp >= 4f && !_trollWarned)
                {
                    _trollWarned = true;
                    Game.Toast("트롤링은 천천히! 14km/h보다 빠르면 루어가 물 위로 튀어 고기가 안 문다.", ToastKind.Warn);
                }
                if (sp < 3f) _trollWarned = false;
            }
            else
            {
                bool hold = Input.GetMouseButton(0) && !UiBlocking;
                _lureReeling = hold;
                if (hold)
                {
                    var player = Game.Player.transform.position;
                    var flat = player - _bobber.Position;
                    flat.y = 0f;
                    float step = Game.Profile.Reel.ReelSpeed * 0.75f * Time.deltaTime;
                    var next = _bobber.Position + flat.normalized * Mathf.Min(step, flat.magnitude);
                    if (flat.magnitude - step <= 3f || !FieldMap.IsWater(next))
                    {
                        Retrieve("루어를 다 감았습니다. 다시 던지세요.");
                        return;
                    }
                    _bobber.DragTo(next);
                    Game.Player.RodPitch = 42f;
                    if (Time.time >= _reelFxAt)
                    {
                        _reelFxAt = Time.time + 0.14f;
                        Game.Sound.ReelClick(0.25f);
                    }
                    rate = 1f;
                    // 저킹: 감으면서 마우스를 위로 툭
                    float my = Input.GetAxis("Mouse Y") * Settings.FightSensitivity;
                    if (my > 5f && Time.time - _lastTwitchAt > 0.45f)
                    {
                        _lastTwitchAt = Time.time;
                        _waitTimer -= kind == BaitKind.Egi ? 1.6f : 1.2f;
                        Game.Player.Rod.Kick(120f);
                        Game.Player.RodPitch = 70f;
                        Effects.Splash(_bobber.Position, 5, 0.4f);
                        Effects.Ripple(_bobber.Position, 0.9f, 0.8f);
                        Game.Sound.ReelClick(0.7f);
                    }
                }
                else
                {
                    rate = kind == BaitKind.Fly ? 0.55f : 0.12f;
                    if (Time.time >= _castShotUntil) AimRod();
                }
            }
            if (kind == BaitKind.Fly && Game.Profile.Data.rodId == "rod_fly") rate *= 1.3f;
            _waitTimer -= Time.deltaTime * rate;

            if (_waitTimer <= 0f)
            {
                StartBite(BiteStyle.Snap);   // 루어는 덥석 문다
                if (Game.Boat != null && Game.Boat.Riding) Game.Boat.ForceStop();
                Game.Player.Rod.Kick(200f); // 루어는 확 낚아챈다
                BigBite = PendingBite.IsTrophy || PendingBite.Species.Rarity >= 4;
                Game.Sound.Plop();
                if (BigBite) Game.Sound.Heartbeat();
                Rumble.Pulse(0.5f, 0.45f, 0.2f);
                Game.CameraRig.Shake(0.45f);
                _shakeSpent = false;
                _shake = 0f;
            }
        }

        /// <summary>입질 전에 챔질하면 헛챔질. 예신(깔짝)에 속은 경우도 여기.</summary>
        void FalseHook()
        {
            if (Random.value < 0.5f) Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
            ShowGrade(HookGrade.Bad);
            Game.Toast(_nibbleActive ? "헛챔질! 예신에 속았다..." : "헛챔질! 고기가 놀라 달아났다.", ToastKind.Warn);
            Game.Player.RodPitch = _aimPitch = 60f;
            BeginWait(1.3f);
        }

        /// <summary>
        /// 원작처럼 대기 중 좌클릭을 누르고 있으면 릴을 감아 찌가 점점 가까이 온다 (좌우로 흔들면 챔질).
        /// 감는 동안은 입질 시계가 멈추고, 손을 뗐을 때 다른 칸이나 어군 한가운데로 옮겨졌으면 그 자리에서 다시 기다린다.
        /// 발밑까지 감으면 찌를 회수한다. 감고 있으면 true.
        /// </summary>
        bool UpdateReel()
        {
            bool hold = (Input.GetMouseButton(0) || DebugReeling) && !_shakeSpent && !UiBlocking;
            if (!hold)
            {
                if (!_reeling) return false;
                _reeling = false;
                FieldMap.TryCell(_bobber.Position, out int col, out int row);
                col = Mathf.Clamp(col, 0, FieldMap.Cols - 1);
                row = Mathf.Clamp(row, 0, FieldMap.Rows - 1);
                var school = Game.Schools.At(_bobber.Position, col, row);
                bool inSpot = school != null && school.InSpot(_bobber.Position);
                if (col != _waitCol || row != _waitRow || inSpot != _waitInSpot) BeginWait(1f);
                return false;
            }

            _reeling = true;
            var player = Game.Player.transform.position;
            var flat = player - _bobber.Position;
            flat.y = 0f;
            float step = Game.Profile.Reel.ReelSpeed * Time.deltaTime;
            // 먼저 느슨한 줄을 감아 들이고, 줄이 팽팽해진 다음부터 찌가 끌려온다
            if (_lineLen > 0f) _lineLen = Mathf.Max(0.5f, _lineLen - step);
            float excess = _lineLen > 0f ? LineSpan() - _lineLen : step;
            float pull = Mathf.Clamp(excess, 0f, step * 1.5f);
            var next = _bobber.Position + flat.normalized * Mathf.Min(pull, flat.magnitude);
            if (flat.magnitude - pull <= 3f || !FieldMap.IsWater(next))
            {
                Retrieve("찌를 다 감았습니다.");
                return true;
            }
            if (pull > 0f) _bobber.DragTo(next);
            _nibbleActive = false;
            if (Time.time >= _reelFxAt)
            {
                _reelFxAt = Time.time + 0.14f;
                Game.Sound.ReelClick(0.3f);
                if (Random.value < 0.4f) Effects.Ripple(next, 0.7f, 0.9f);
            }
            return true;
        }

        void UpdateBite()
        {
            if (_hookAt > 0f)
            {
                // 챔질 성공: 낚싯대를 홱 채고(0.35초) 파이팅 시점으로 넘어간다
                if (Time.unscaledTime - _hookAt < 0.35f) return;
                _hookAt = 0f;
                StartFight(_hookBite, _hookDistance, _hookQuality, _hookCombo);
                Game.CameraRig.Punch(-6f);
                Game.CameraRig.Shake(0.6f);
                return;
            }
            if (WantsRetrieve) { Retrieve("찌를 회수했습니다."); return; }
            SwayRod(true);

            _biteTimer += Time.deltaTime;
            _biteFrame = FishPatterns.BiteAt(_biteStyle, _biteTimer, _biteSeed);
            _bobber.SetBite(_biteFrame.Dip, _biteFrame.Side);
            // 찌가 툭 내려갈 때마다 (톡톡·오르내림·찌올림 첫 깜빡) 초릿대가 까딱하고 물결이 인다
            if (_biteFrame.Dip > _biteDipMark + 0.22f)
            {
                _biteDipMark = _biteFrame.Dip;
                Game.Player.Rod.Kick(60f);
                Game.Sound.Tick();
                Effects.Ripple(_bobber.Position, 0.9f, 0.9f);
            }
            else if (_biteFrame.Dip < _biteDipMark - 0.22f) _biteDipMark = _biteFrame.Dip;

            bool shook = Autoplay ? _biteFrame.Quality >= 0.97f : DetectShake();
            if (shook)
            {
                var grade = GradeFor(Mathf.Min(1f, _biteFrame.Quality + Game.Weather.HookBonus)); // 맑으면 챔질이 쉽다
                ShowGrade(grade);
                if (grade == HookGrade.Bad)
                {
                    if (Random.value < 0.5f) Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                    Game.Toast("BAD! 너무 일찍 챔질했다. 고기가 달아났다.", ToastKind.Warn);
                    Game.Player.RodPitch = 60f;
                    BeginWait(1.3f);
                    return;
                }
                Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                float quality = grade switch { HookGrade.SoSo => 0.25f, HookGrade.Good => 0.5f, HookGrade.Great => 0.75f, _ => 1f };
                // 홱! 낚싯대를 세우며 크게 휘고, 찌 자리에 물보라, 잠깐 시간이 멈춘 듯
                _hookAt = Time.unscaledTime;
                _hookBite = PendingBite;
                _hookDistance = Vector3.Distance(Game.Player.transform.position, _bobber.Position);
                _hookQuality = quality;
                _hookCombo = grade == HookGrade.Perfect ? 1 : 0;
                Game.Player.RodPitch = 82f;
                Game.Player.RodPitchSpeed = 30f;
                Game.Player.BodyLean = 14f;
                Game.Player.Rod.Kick(380f);
                Effects.Splash(_bobber.Position, 26, 1.4f);
                Effects.Ripple(_bobber.Position, 2.4f, 1.2f);
                Game.Sound.Splash(1f);
                Game.CameraRig.Shake(grade == HookGrade.Perfect ? 1.1f : 0.7f);
                Rumble.Pulse(grade == HookGrade.Perfect ? 0.9f : 0.6f, 0.5f, 0.3f);   // 챔질
                Game.CameraRig.Punch(grade == HookGrade.Perfect ? -9f : -6f);
                HitStop(0.12f, 0.12f);
                return;
            }
            if (_biteTimer > FishPatterns.BiteEnd(_biteStyle, _biteSeed))
            {
                Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                Game.Toast("늦었다! 미끼만 따먹혔다. 다시 던지세요.", ToastKind.Warn);
                ToIdle();
            }
        }

        /// <summary>본신을 시작한다: 찌 패턴을 정하고 시계를 0으로.</summary>
        void StartBite(BiteStyle style)
        {
            State = FishingState.Bite;
            _biteTimer = 0f;
            _biteStyle = style;
            _biteSeed = Random.Range(0, 1 << 20);
            _biteFrame = default;
            _biteDipMark = 0f;
        }

        static HookGrade GradeFor(float sink) =>
            sink < 0.2f ? HookGrade.Bad :
            sink < 0.45f ? HookGrade.SoSo :
            sink < 0.7f ? HookGrade.Good :
            sink < 0.97f ? HookGrade.Great : HookGrade.Perfect;

        void ShowGrade(HookGrade grade)
        {
            LastGrade = grade;
            LastGradeAt = Time.time;
        }

        void Retrieve(string message)
        {
            Game.Toast(message);
            ToIdle();
        }

        void ToIdle()
        {
            _hookAt = 0f;
            _cheering = false;
            _reeling = false;
            _swayYaw = 0f;
            _aimYaw = 0f;
            Game.Player.RodYaw = 0f;
            Game.Player.BodyLean = 0f;
            Game.Player.RodPitchSpeed = 10f;
            State = FishingState.Idle;
            CastPower = 0f;
            BigBite = false;
            _bobber.Hide();
            HideAim();
            Game.Player.MovementLocked = false;
            Game.Player.RodPitch = 35f;
        }

        // ---------- Fighting ----------

        public void StartFight(BiteResult bite, float distance, float hookQuality, int startCombo = 0)
        {
            var p = Game.Profile;
            PendingBite = bite;
            if (_castDir == Vector3.zero) _castDir = Game.Player.Model.forward;
            Fight = new FishFight(bite.Species, bite.Length, Mathf.Max(6f, distance), hookQuality, p.Rod, p.Reel, p.Line, startCombo);
            Fight.CounterBonus = 1f + p.Costume.Counter;
            Fight.Countered += OnCounter;
            Fight.Burst += OnBurst;
            Fight.EscapeRun += OnEscapeRun;
            Fight.Special += OnSpecial;
            Game.Schools.OnHooked(_ctx.School, bite.Species.Id); // 물속 그림자 고기가 걸리면 실루엣을 감춘다

            State = FishingState.Fighting;
            Game.Player.RodHidden = false;
            _bobber.Hide();
            _line.enabled = false;
            _damagePopTimer = 0f;
            Game.Player.MovementLocked = true;
            Game.Player.SetVisible(false);
            Game.CameraRig.EnterFight(Game.Player.EyePos, _castDir);
            _rodView.Show(Fight, Game.Player.transform.position, _castDir);
            _lastDistance = Fight.Distance;

            _cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FightStarted?.Invoke();
        }

        /// <summary>대기실로 나갈 때: 던져 둔 찌를 거둔다.</summary>
        public void ForceIdle()
        {
            if (_bobber == null) return; // 아직 Start 전 (게임 시작 직후)
            if (State == FishingState.Fighting || State == FishingState.Result) return;
            ToIdle();
        }

        /// <summary>파이팅 중 ESC → [그만두기]: 물고기를 놓친 것으로 끝낸다 (줄은 잃지 않는다).</summary>
        public void GiveUpFight()
        {
            if (State == FishingState.Fighting && Fight != null && Fight.Outcome == FightOutcome.None) EndFight(FightOutcome.GaveUp);
        }

        /// <summary>개발/테스트용: 캐스팅 없이 바로 파이팅을 시작한다.</summary>
        public void DebugStartFight(string speciesId, float length, float distance)
        {
            if (State != FishingState.Idle) ToIdle();
            _castDir = Game.Player.Model.forward;
            FieldMap.TryCell(Game.Player.transform.position + _castDir * distance, out int col, out int row);
            _cellName = FieldMap.CellName(Mathf.Clamp(col, 0, 5), Mathf.Clamp(row, 0, 3));
            _ctx = new BiteContext { Habitat = Habitat.Deep, Band = Game.Clock.Band };
            StartFight(new BiteResult { Species = FishDatabase.Get(speciesId), Length = length }, distance, 0.9f);
        }

        /// <summary>개발/테스트용: 바라보는 방향으로 찌를 던진다.</summary>
        public void DebugCast(float distance)
        {
            if (State != FishingState.Idle) ToIdle();
            _castDir = Game.Player.Model.forward;
            _castTarget = Game.Player.transform.position + _castDir * distance;
            Game.Player.MovementLocked = true;
            Game.Player.RodHidden = false;
            Game.Player.RodPitch = 20f;
            _bobber.Launch(Game.Player.RodTip.position, _castTarget);
            Casted?.Invoke(distance);
            State = FishingState.Casting;
        }

        /// <summary>개발/테스트용: 캐스팅 제스처 화면을 보여주기 위해 조준 상태로 만든다.</summary>
        public void DebugAim(float power, float pullBack)
        {
            if (State != FishingState.Idle) ToIdle();
            State = FishingState.Charging;
            _castDir = Game.Player.Model.forward;
            CastPower = power;
            PullBack = pullBack;
            CastDistance = Mathf.Lerp(4f, Game.Profile.Rod.CastRange, power);
            _iceReady = false;
            _peakBack = PullFull;
            _peakPush = power * PushSpeedFull;
            _gestureHeld = true;
            _aimPoint = Game.Player.transform.position + _castDir * CastDistance;
            Game.Player.RodHidden = false;
            _marker.SetActive(true);
            _aimArc.enabled = true;
            _playerRing.SetActive(true);
            UpdateAim(_aimPoint);
            // 당긴 자세와 어깨 근접 구도
            Game.Player.RodPitch = Mathf.Lerp(70f, 155f, Mathf.Clamp01(pullBack / PullFull)) - power * 110f;
            Game.Player.BodyLean = 20f - power * 26f;
            Game.Player.RodYaw = -10f;
            Game.CameraRig.Shot = Fishing.PlayerSystems.CameraRig.ShotKind.Cast;
            Game.CameraRig.CastLookAt = null;
            enabled = false; // 다음 Update에서 마우스 입력으로 덮어쓰지 않게 잠시 멈춘다
        }

        public void DebugResume() => enabled = true;

        /// <summary>개발/테스트용: 루어 저킹 연출 ("액션!").</summary>
        public void DebugTwitch()
        {
            _lastTwitchAt = Time.time;
            Game.Player.Rod.Kick(120f);
            Effects.Splash(_bobber.Position, 5, 0.4f);
        }

        /// <summary>개발/테스트용: 대기 중이면 바로 본신이 오게 한다. 어종을 지정할 수 있다.</summary>
        public void DebugForceBite(string speciesId = null, float length = 0f)
        {
            if (State != FishingState.Waiting) return;
            _waitTimer = 0f;
            _nibblesLeft = 0;
            if (speciesId != null) PendingBite = new BiteResult { Species = FishDatabase.Get(speciesId), Length = length };
        }

        void OnCounter()
        {
            Game.Sound.Counter(Fight.Combo);
            Rumble.Pulse(0.8f, 1f, 0.15f);
            Game.CameraRig.Shake(1.2f);
            Game.CameraRig.Punch(-8f);
            HitStop(0.09f, 0.15f);
            _rodView.Kick(420f); // 낚싯대가 확 휘었다가 튕겨 돌아온다
            var pos = _rodView.FishWorldPos(Fight);
            Effects.BigSplash(pos, 0.8f);
            Game.UI.Fight.ShowCounter(Fight.Combo);
            Game.UI.Fight.PopDamage(pos, Fight.LastCounterDamage, true);
        }

        /// <summary>물고기가 날뛸 때: 큰 물보라, 화면이 흔들리고, 낚싯대가 확 끌려간다.</summary>
        void OnBurst()
        {
            Rumble.Pulse(0.7f, 0.2f, 0.6f);   // 질주: 묵직하게 길게
            Game.Sound.Splash(1f);
            var pos = _rodView.FishWorldPos(Fight);
            Effects.BigSplash(pos, 0.6f);
            Effects.Ripple(pos, 2.2f, 1f);
            Game.CameraRig.Shake(0.55f);
            Game.CameraRig.Punch(2.5f);
            _rodView.Kick(260f);
        }

        /// <summary>어종 패턴 기술: 처박기는 묵직한 진동, 바늘털이는 잔떨림, 점프는 수면을 박차는 물보라(떨어질 때 한 번 더), 바닥 붙기는 낮은 울림.</summary>
        void OnSpecial(FishMood mood)
        {
            var pos = _rodView.FishWorldPos(Fight);
            switch (mood)
            {
                case FishMood.Dive:
                    Rumble.Pulse(0.8f, 0.35f, 0.5f);
                    Game.CameraRig.Shake(0.5f);
                    Game.CameraRig.Punch(3.5f);
                    _rodView.Kick(320f);
                    Effects.Ripple(pos, 2.4f, 1f);
                    break;
                case FishMood.Shake:
                    Rumble.Pulse(0.35f, 0.6f, 1.2f);
                    Game.Sound.Splash(0.7f);
                    Effects.Splash(pos, 14, 0.8f);
                    break;
                case FishMood.Jump:
                    Rumble.Pulse(0.5f, 0.4f, 0.4f);
                    Game.Sound.Splash(1.2f);
                    Effects.BigSplash(pos, 0.8f);
                    _jumpSplashAt = Time.time + 0.7f;   // FishFight 점프 체공 시간
                    break;
                case FishMood.Hold:
                    Rumble.Pulse(0.6f, 0.2f, 1.5f);
                    Game.CameraRig.Shake(0.25f);
                    break;
            }
        }

        float _jumpSplashAt;

        void OnEscapeRun()
        {
            Rumble.Pulse(0.6f, 0.3f, 0.5f);
            Game.Sound.Splash(1.2f);
            Effects.Splash(_rodView.FishWorldPos(Fight), 24, 1.4f);
            Game.UI.Fight.ShowEscape();
        }

        void UpdateFighting()
        {
            var f = Fight;
            var p = Game.Profile;

            // 점프한 고기가 떨어지는 순간: 철썩 (장력이 튄다)
            if (_jumpSplashAt > 0f && Time.time >= _jumpSplashAt)
            {
                _jumpSplashAt = 0f;
                Effects.BigSplash(_rodView.FishWorldPos(f), 1f);
                Game.Sound.Splash(1.3f);
                Game.CameraRig.Shake(0.4f);
                _rodView.Kick(280f);
            }

            if (f.Landing)
            {
                // 랜딩 찬스: Space 또는 좌클릭 연타
                bool tap = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
                if (Autoplay)
                {
                    _autoTapTimer -= Time.deltaTime;
                    tap = _autoTapTimer <= 0f;
                    if (tap) _autoTapTimer = 1f / FightBot.LandingTapsPerSecond;
                }
                if (tap)
                {
                    f.LandingTap(p.StaminaFactor);
                    Game.Sound.ReelClick(0.8f);
                    _lastTapAt = Time.time;
                }
                f.Tick(Time.deltaTime, default, p.StaminaFactor, p.FightPower, p.ReelFactor);
                // 연타할 때마다 릴을 힘차게 돌린다
                float tapTurns = Time.time - _lastTapAt < 0.18f ? 4.5f : 0f;
                _rodView.SetReel(tapTurns, 0f);
                Game.Sound.SetReel(tapTurns, 0.6f);
            }
            else
            {
                var input = Autoplay ? FightBot.Input(f, AutoplayCounterChance) : new FightInput
                {
                    Reeling = Input.GetMouseButton(0),
                    Releasing = Input.GetMouseButton(1),
                    MouseDX = Input.GetAxis("Mouse X") * Settings.FightSensitivity,
                };
                // 포커싱 (Q): 원작 파이팅 스킬, 접속 20분마다 1회
                if (Settings.Down(KeyAction.Focus) && !f.Focused)
                {
                    if (p.FocusReady)
                    {
                        f.Focus();
                        p.UseFocus();
                        Game.Sound.Counter(3);
                        Game.CameraRig.Punch(-4f);
                        Game.Toast($"포커싱! {FishFight.FocusDuration:0}초 동안 장력이 천천히 변하고 데미지가 커집니다.", ToastKind.Great);
                    }
                    else Game.Toast($"포커싱은 {Mathf.CeilToInt(p.Data.focusCooldown / 60f)}분 뒤에 다시 쓸 수 있습니다.", ToastKind.Warn);
                }
                f.Tick(Time.deltaTime, input, p.StaminaFactor, p.FightPower, p.ReelFactor);

                // 릴: 감으면 손잡이가 돌고(드르르륵), 줄이 풀려 나가면 스풀이 거꾸로 돌며 드랙이 운다(지이이익)
                float lineOut = Mathf.Max(0f, (f.Distance - _lastDistance) / Mathf.Max(Time.deltaTime, 1e-4f));
                _lastDistance = f.Distance;
                float turns = input.Reeling ? f.Reel.ReelSpeed * 0.9f * p.ReelFactor * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(f.Tension)) : 0f;
                _rodView.SetReel(turns, lineOut);
                Game.Sound.SetReel(turns, f.Tension);
                Game.Sound.SetDrag(lineOut);
                Game.Sound.SetStrain(Mathf.InverseLerp(FishFight.HighTension, 1f, f.Tension));
                Rumble.Hold(Mathf.InverseLerp(FishFight.HighTension, 1f, f.Tension));   // 장력이 높으면 계속 떨림

                if (f.LandingChance && f.StartLanding())
                {
                    Game.Sound.Heartbeat();
                    Game.UI.Fight.ShowLandingChance();
                }
            }

            // 체력 소모: 하이텐션일수록 많이 지친다
            float drain = 0.35f + (f.IsHighTension ? 1.8f * f.Tension : 0f);
            p.ChangeStamina(-drain * (1f - p.Costume.Drain) * Time.deltaTime);

            // 원작처럼 물고기 위치에 데미지 숫자를 띄운다
            _damagePopTimer -= Time.deltaTime;
            if (_damagePopTimer <= 0f)
            {
                _damagePopTimer = 0.55f;
                float dmg = f.TakeDamagePool();
                if (dmg >= 0.5f) Game.UI.Fight.PopDamage(_rodView.FishWorldPos(f), dmg, false);
            }

            if (f.Outcome != FightOutcome.None) EndFight(f.Outcome);
        }

        void EndFight(FightOutcome outcome)
        {
            var f = Fight;
            var p = Game.Profile;
            Cursor.lockState = _cursorWasLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = true;
            var fishPos = _rodView.FishWorldPos(f);
            if (outcome != FightOutcome.Landed)
            {
                _rodView.Hide();
                Game.CameraRig.ExitFight();
                Game.Player.SetVisible(true);
                Game.Player.RodPitch = 35f;
            }

            float wear = Mathf.Lerp(0.5f, 3f, f.Length / 100f) * (f.Tension > 0.9f ? 1.5f : 1f);
            p.Data.rodDurability = Mathf.Max(0f, p.Data.rodDurability - wear);
            if (p.Data.rodDurability <= 0f) Game.Toast("낚싯대가 망가졌습니다! 상점에서 수리하세요.", ToastKind.Warn);
            CatchRewards.OnFightEnded(f.CounterCount);

            FightEnded?.Invoke(outcome);

            if (outcome != FightOutcome.Landed)
            {
                Game.Sound.Snap();
                Rumble.Pulse(1f, 1f, 0.45f);   // 줄 끊김
                // 원작 실패 페널티 (경고 카드 문구 그대로): 낚싯대 내구도 1% · 릴 내구도 2% · 줄이 끊기면 줄 길이 7m
                const float rodLoss = 1f, reelLoss = 2f;
                float lineLoss = outcome == FightOutcome.LineBroken ? Mathf.Min(7f, p.LineCapacity - p.Reel.Capacity * 0.4f) : 0f;
                p.Data.rodDurability = Mathf.Max(0f, p.Data.rodDurability - rodLoss);
                p.Data.reelDurability = Mathf.Max(0f, p.Data.reelDurability - reelLoss);
                p.Data.lineLost += Mathf.Max(0f, lineLoss);
                string cause = outcome switch
                {
                    FightOutcome.LineBroken => "낚싯줄이 끊어졌습니다! 장력이 HIGH를 넘지 않게 조심하세요.",
                    FightOutcome.HookOut => "바늘이 빠졌습니다! 줄을 너무 느슨하게 두었어요.",
                    FightOutcome.LineRunOut => "낚싯줄이 모두 풀려 버렸습니다! 거리를 좁히세요.",
                    FightOutcome.GaveUp => "파이팅을 그만두었습니다. 물고기가 달아났어요.",
                    _ => "물고기를 놓쳤습니다."
                };
                string penalty = $"낚싯대 내구도 감소 : {rodLoss:0.00} %  릴 내구도 감소 : {reelLoss:0.00} %"
                                 + (lineLoss > 0f ? $"  줄 길이 감소 : {lineLoss:0.00}m" : "");
                // 원작처럼 화면 위쪽 흰 경고 카드 (빨간 WARNING 표지)
                Game.UI.Hud.ShowWarning(cause, penalty);
                Game.SysLog($"{f.Species.Name}{Fishing.Data.QuestNpcs.Josa(f.Species.Name, "을", "를")} 놓쳤습니다.");
                // 원작: 경고 뒤 오른쪽 아래 "재도전 하시겠습니까?" + 오른쪽 위 X2 재도전
                if (CatchRewards.OnLost(f.Species, f.Length)) Game.UI.Hud.OfferRetry(f.Species.Name, CatchRewards.Mult);
                Game.Schools.OnLost(_ctx.School);
                Fight = null;
                ToIdle();
                // 줄이 툭 끊기면 움찔 (ToIdle이 낚싯대 자세를 돌린 뒤에 한 번짜리 동작)
                if (outcome == FightOutcome.LineBroken) Game.Player.PlayScared();
                p.NotifyChanged();
                return;
            }

            Game.Sound.Success();
            Rumble.Pulse(0.5f, 0.8f, 0.35f);   // 랜딩
            // 대물이면 근처 NPC가 돌아보며 박수
            if (f.Species.IsTrophy(f.Length) || f.Species.Rarity >= 4)
                Fishing.Characters.NpcAngler.Applaud(Game.Player.transform.position, Game.Player.transform);
            // 원작 1:53: 물고기 자리에서 하얀 물보라가 화면 앞까지 터진다
            Effects.BigSplash(fishPos, 1.2f);
            Game.CameraRig.Shake(0.8f);
            var species = f.Species;
            var caught = new CaughtFish
            {
                speciesId = species.Id,
                length = f.Length,
                weight = Mathf.Round(species.WeightGrams(f.Length)),
                day = Game.Clock.Day,
                zone = _cellName,
                mapId = FieldMap.Id,
            };
            var previous = p.Dex(species.Id);
            float prevBest = previous?.maxLength ?? 0f;
            int prevBestDay = previous?.firstDay ?? 0;
            bool newSpecies = !species.IsJunk && p.Dex(species.Id) == null;
            bool record = p.RecordDex(species, caught.length, caught.weight, caught.day);
            int exp = Mathf.RoundToInt(species.Exp * Mathf.Sqrt(f.Length / species.MinLen) * (species.IsTrophy(f.Length) ? 2f : 1f));
            // 원작 RESULT 오른쪽 토스트·BONUS·이벤트 어종(EXP +200%)·복수혈전
            exp = CatchRewards.OnCatch(species, f.Length, exp, f.CounterCount, record, prevBest, f.Elapsed, out var rewards);
            p.Data.totalCatches++;
            p.AddExp(exp);
            // 원작 시스템 줄: "[꽃돔] 19.28 cm, 0.15 kg 을 잡았습니다." "337 EXP 를 획득했습니다."
            if (!species.IsJunk)
                Game.SysLog($"[{species.Name}] {caught.length:0.00} cm, {caught.weight / 1000f:0.00} kg 을 잡았습니다.");
            Game.SysLog($"{exp:N0} EXP 를 획득했습니다.", QuestSystem.ExpColor);
            Game.Quests.OnCatch(new CatchInfo { Species = species, Length = f.Length, Habitat = _ctx.Habitat, Band = Game.Clock.Band,
                MapId = FieldMap.Id, NewSpecies = newSpecies, Combo = f.Combo });
            Game.Schools.OnCatch(_ctx.School, species.Id);
            Game.Tournament.OnCatch(species, f.Length);

            State = FishingState.Result;
            Fight = null;
            StartCoroutine(LandedSequence(f, caught, exp, record, prevBest, prevBestDay, species, p, newSpecies, rewards));
        }

        /// <summary>결과 화면에서 캐릭터가 손에 들어 보일 만한 크기인가 (원작: 작은 고기는 손에 들고, 큰 고기는 물 위로 튀어 오른다).</summary>
        public static bool HoldableCatch(FishSpecies s, float length) => s != null && !s.IsJunk && length <= 55f;

        /// <summary>결과 화면 구도: Catch(줄 끝에 매단 고기) → Show(손에 든 고기) 또는 Follow(큰 고기·잡동사니).</summary>
        Fishing.PlayerSystems.CameraRig.ShotKind _resultShot;
        GameObject _caughtFish;
        Light _showLight;
        /// <summary>3인칭 줄 끝 (줄 끝에 매단 고기). null이면 이 줄은 안 그린다.</summary>
        Vector3? _catchLineEnd;

        /// <summary>
        /// 랜딩 성공 뒤 결과 화면까지 (원작 2013 홍현 영상 1:53.0~1:56.0, 프레임 단위로 맞춤):
        ///  1. 1인칭 그대로 낚싯대를 세워 들어 올리고 시선이 위로 따라간다 (1.3초). 고기가 물 밖으로 끌려 나온다
        ///  2. 하얗게 번쩍인 뒤 3인칭 옆모습으로 끊어 넘어간다: 치켜든 낚싯대 줄 끝에 고기가 대롱대롱, 왼손으로 낚아챈다 (1.25초)
        ///  3. RESULT: 작은 고기는 얼굴 앞 정면 구도에서 왼손으로 들어 보이고, 큰 고기는 물 위로 튀어 오르는 모습 (결과 창의 3D 물고기)
        /// </summary>
        System.Collections.IEnumerator LandedSequence(FishFight f, CaughtFish caught, int exp, bool record, float prevBest, int prevBestDay,
            FishSpecies species, PlayerProfile p, bool firstGet, CatchRewards.Outcome rewards)
        {
            var rig = Game.CameraRig;
            // 1. 들어 올리기
            const float liftTime = 1.3f;
            for (float t = 0f; t < liftTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / (liftTime * 0.85f));
                _rodView.Lift = k;
                rig.FightLift = k;
                yield return null;
            }
            Fishing.UI.ScreenFlash.Flash(new Color(1f, 1f, 1f, 0.95f), 0.05f, 0.35f);
            _rodView.Hide();
            rig.FightLift = 0f;
            rig.ExitFight();
            var player = Game.Player;
            player.SetVisible(true);

            bool holdable = HoldableCatch(species, f.Length);
            // 2. 줄 끝에 매단 고기 (옆모습)
            _caughtFish = species.IsJunk && !FishModels.HasJunkModel(species) ? null : FishModels.Create(species, f.Length, null);
            if (_caughtFish != null) FishModels.Play(_caughtFish, species.IsJunk ? FishModels.IdleHover : FishModels.Flop, 1.2f, 0f);
            player.RodPitch = 48f;
            player.RodPitchSpeed = 30f;
            player.BodyLean = -6f;
            _resultShot = Fishing.PlayerSystems.CameraRig.ShotKind.Catch;
            _cheering = true;
            const float catchTime = 1.25f;
            bool grabbed = false;
            for (float t = 0f; t < catchTime; t += Time.unscaledDeltaTime)
            {
                if (_caughtFish != null)
                {
                    var tip = player.RodTip.position;
                    float swing = Mathf.Sin(t * 7f) * 0.12f * (1f - t / catchTime);
                    var hang = tip + Vector3.down * Mathf.Lerp(0.9f, 1.05f, t / catchTime) + player.Model.right * swing;
                    // 0.55초쯤 왼손을 뻗어 고기를 잡고 몸 쪽으로 당긴다
                    if (holdable && t > 0.55f && !grabbed)
                    {
                        grabbed = true;
                        player.HoldPoint = null;
                        player.HoldFish(_caughtFish.transform, f.Length);
                    }
                    if (!grabbed)
                    {
                        _caughtFish.transform.position = hang;
                        _caughtFish.transform.rotation = Quaternion.LookRotation(Vector3.up, player.Model.right) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 20f);
                    }
                    _catchLineEnd = grabbed ? (Vector3?)null : hang + Vector3.up * (f.Length / 200f);
                }
                yield return null;
            }
            _catchLineEnd = null;

            // 3. RESULT
            bool held = holdable && _caughtFish != null;
            if (held)
            {
                _resultShot = Fishing.PlayerSystems.CameraRig.ShotKind.Show;
                // 원작 1:57: 왼팔을 머리 위로 뻗어 주먹으로 고기 입을 쥐고 들어 보인다 (몸통은 아래로 늘어지고 옆구리가 카메라 쪽).
                // 오른손은 낚싯대를 몸 옆에 거의 세워 든다 (얼굴을 가리지 않게)
                var m = player.Model;
                player.HoldPoint = null;
                player.ShowOff = true;
                player.HoldFish(_caughtFish.transform, f.Length);
                FishModels.Play(_caughtFish, FishModels.Flop, 0.6f, 0.2f);
                player.RodPitch = 84f;
                player.RodYaw = 6f;
                // 밤에도 얼굴과 고기가 보이게 앞에서 비추는 보조광 (결과 창을 닫으면 끈다)
                _showLight = new GameObject("ResultFill").AddComponent<Light>();
                _showLight.type = LightType.Point;
                _showLight.range = 4f;
                _showLight.intensity = 1.6f;
                _showLight.color = new Color(1f, 0.95f, 0.88f);
                _showLight.shadows = LightShadows.None;
                _showLight.transform.position = player.Head.position + m.forward * 1.3f + Vector3.up * 0.3f - m.right * 0.3f;
            }
            else
            {
                // 큰 고기·잡동사니: 결과 창의 3D 물고기가 물 위로 튀어 오른다. 캐릭터는 환호
                if (_caughtFish != null) Destroy(_caughtFish);
                _caughtFish = null;
                if (_showLight != null) Destroy(_showLight.gameObject);
                _showLight = null;
                Game.Player.RodYaw = 0f;
                _resultShot = Fishing.PlayerSystems.CameraRig.ShotKind.Follow;
                player.RodPitch = 70f;
                if (species.Boss || species.Rarity >= 5) player.PlayExcited();
                else player.PlayCheer();
            }
            ShowResult(caught, exp, record, prevBest, prevBestDay, species, p, firstGet, held, rewards);
        }

        void ShowResult(CaughtFish caught, int exp, bool record, float prevBest, int prevBestDay,
            FishSpecies species, PlayerProfile p, bool firstGet, bool held, CatchRewards.Outcome rewards)
        {
            if (rewards != null) CatchRewards.Pay(rewards);
            Game.UI.Result.Show(caught, exp, record, prevBest, prevBestDay, firstGet, held, rewards, keep =>
            {
                _cheering = false;
                Game.Player.HoldFish(null);
                Game.Player.HoldPoint = null;
                Game.Player.ShowOff = false;
                if (_caughtFish != null) Destroy(_caughtFish);
                _caughtFish = null;
                _resultShot = Fishing.PlayerSystems.CameraRig.ShotKind.Follow;
                if (keep && !species.IsJunk)
                {
                    if (p.KeepNetFull)
                        Game.Toast("살림망이 가득 찼습니다. 상점에서 판매하거나 요리하세요.", ToastKind.Warn);
                    else
                        p.Data.keepNet.Add(caught);
                }
                else if (!species.IsJunk)
                {
                    int bonus = Mathf.Max(1, Mathf.RoundToInt(exp * 0.3f));
                    p.AddExp(bonus);
                    Game.Toast($"방생했습니다. 경험치 +{bonus}", ToastKind.Good);
                    Bingo.OnAction("release");
                    p.Data.releasedTotal++;
                    EcoSurvey.OnRelease();
                    Game.Quests.OnReleased();   // 가이드 퀘스트 "물고기 방생해보기"
                }
                p.NotifyChanged();
                ToIdle();
                Game.Save();
            });
        }

        // ---------- 3인칭 낚싯줄 ----------

        float _lineLen = -1f;
        /// <summary>찌를 기다리는 동안 줄이 팽팽한 정도 0(축 늘어짐)~1(팽팽).</summary>
        public float LineTension { get; private set; }
        /// <summary>줄이 팽팽한 데서 더 당겨 낚싯대가 휘는 정도 0~1.</summary>
        public float LineStrain { get; private set; }

        float LineSpan() => Vector3.Distance(Game.Player.RodTip.position, _bobber.Position + Vector3.up * 0.3f);

        void UpdateLine()
        {
            if (State == FishingState.Result && _catchLineEnd.HasValue)
            {
                // 줄 끝에 매단 고기까지 곧게
                _line.enabled = true;
                var tip = Game.Player.RodTip.position;
                for (int i = 0; i < _line.positionCount; i++)
                    _line.SetPosition(i, Vector3.Lerp(tip, _catchLineEnd.Value, i / (float)(_line.positionCount - 1)));
                return;
            }
            bool show = State == FishingState.Casting || State == FishingState.Waiting || State == FishingState.Bite;
            _line.enabled = show;
            if (!show) return;

            var a = Game.Player.RodTip.position;
            var b = _bobber.Position + Vector3.up * 0.3f;
            float d = Vector3.Distance(a, b);
            // 찌는 그 자리에 있고 풀려 나간 줄 길이도 그대로라, 낚싯대를 찌 쪽으로 내리거나 숙이면 줄이 처지고
            // 세우거나 반대로 틀면 팽팽해진다. 더 당기면 낚싯대가 휘고, 그보다 더 당기면 드랙이 줄을 조금 내준다
            if (State == FishingState.Waiting || State == FishingState.Bite)
            {
                if (_lineLen < 0f) _lineLen = d * 1.05f + 0.6f;
                if (_lureReeling || Trolling) _lineLen = d;
                _lineLen = Mathf.Max(_lineLen, d - 1f);
            }
            float slack = Mathf.Max(0f, _lineLen - d);
            LineTension = State == FishingState.Waiting ? Mathf.Clamp01(1f - slack / (0.5f + d * 0.06f)) : 0f;
            LineStrain = State == FishingState.Waiting ? Mathf.Clamp01(d - _lineLen) : 0f;
            float sag = State == FishingState.Bite ? 0.1f
                      : State == FishingState.Casting ? d * 0.06f
                      : Mathf.Max(d * 0.006f, Mathf.Sqrt(3f * d * slack / 8f)); // 줄 길이 L, 거리 d인 포물선의 처짐
            var mid = (a + b) * 0.5f + Vector3.down * sag;
            float floor = FieldMap.WaterY + 0.02f;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
                if (State != FishingState.Casting) p.y = Mathf.Max(p.y, floor); // 처진 줄은 물에 닿으면 수면에 눕는다
                _line.SetPosition(i, p);
            }
        }
    }
}
