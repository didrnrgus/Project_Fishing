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
    ///  캐스팅: 좌클릭을 누르고 마우스를 당긴 다음, 빠르게 밀면서 버튼을 놓는다. 미는 속도가 비거리.
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
        const float SinkTime = 1.1f;    // 찌가 완전히 잠기기까지
        const float SunkHold = 0.6f;    // 완전히 잠긴 뒤 고기가 뱉기까지

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
        /// <summary>찌가 잠긴 정도 0~1 (챔질 게이지 표시용).</summary>
        public float BobberSink => _bobber != null ? _bobber.SinkAmount : 0f;
        /// <summary>찌가 날아가는 동안 지금까지 날아간 거리(m). 화면의 큰 비거리 숫자가 따라 올라간다.</summary>
        public float FlyingDistance { get; private set; }
        /// <summary>챔질에 성공해 파이팅으로 넘어가기 직전(낚싯대를 홱 채는 연출 중).</summary>
        public bool Hooking => _hookAt > 0f;
        /// <summary>랜딩 직후 결과 화면 전 환호 컷.</summary>
        public bool Cheering => _cheering;
        public HookGrade LastGrade { get; private set; }
        public float LastGradeAt { get; private set; } = -10f;

        /// <summary>개발용: 파이팅을 자동으로 진행한다 (스크린샷/밸런스 확인).</summary>
        public bool Autoplay;
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
        Coroutine _hitStop;
        // 대기 중 릴 감기
        int _waitCol, _waitRow;
        bool _waitInSpot;
        bool _reeling;
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
                case FishingState.Fighting: UpdateFighting(); break;
            }
            UpdateLine();
            UpdateCameraShot();
        }

        // ---------- Idle / Charging(캐스팅 제스처) / Casting ----------

        void UpdateIdle()
        {
            Game.Player.MovementLocked = false;
            Game.Player.RodPitch = 35f;
            if (UiBlocking || PointerOverUI) return;
            if (!Input.GetMouseButtonDown(0)) return;

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

            State = FishingState.Charging;
            PullBack = 0f;
            CastPower = 0f;
            _pushSamples.Clear();
            Game.Player.MovementLocked = true;
            Game.Player.FaceYaw(Game.CameraRig.Yaw);
            _marker.SetActive(true);
            _aimArc.enabled = true;
            _playerRing.SetActive(true);
            Game.Player.RodPitchSpeed = 10f;
        }

        void UpdateCharging()
        {
            float dy = Input.GetAxis("Mouse Y");
            float now = Time.time;
            // 아래로 당긴 양(뒤로 젖히기)을 모으고, 위로 미는 순간 속도를 잰다
            if (dy < 0f && PushSum(now) <= 1f) PullBack += -dy;
            _pushSamples.Add((now, Mathf.Max(0f, dy)));
            _pushSamples.RemoveAll(s => now - s.t > PushWindow);

            float pullFactor = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(PullBack / PullFull));
            CastPower = Mathf.Clamp01(PushSum(now) / PushFull) * pullFactor;

            Game.Player.FaceYaw(Game.CameraRig.Yaw);
            // 원작 0:01~0:02: 당길수록 낚싯대를 어깨 너머 뒤로 젖히고 상체도 뒤로. 밀기 시작하면 앞으로 넘어온다
            float pull = Mathf.Clamp01(PullBack / PullFull);
            Game.Player.RodPitch = Mathf.Lerp(70f, 155f, pull) - CastPower * 110f;
            Game.Player.RodPitchSpeed = 16f;
            Game.Player.BodyLean = Mathf.Lerp(4f, 20f, pull) - CastPower * 26f;
            Game.Player.RodYaw = Mathf.Lerp(0f, -14f, pull);

            _castDir = Game.Player.Model.forward;
            var rod = Game.Profile.Rod;
            // 미리보기: 당긴 만큼 낼 수 있는 최대 비거리를 보여준다
            float preview = Mathf.Max(CastPower, pullFactor * Mathf.Clamp01(PullBack / PullFull));
            CastDistance = Mathf.Lerp(4f, rod.CastRange + Game.Profile.Costume.Cast, preview);
            UpdateAim(CastDistance);

            if (Input.GetMouseButtonUp(0) || UiBlocking)
            {
                HideAim();
                if (UiBlocking) { ToIdle(); return; }

                CastDistance = Mathf.Lerp(4f, rod.CastRange + Game.Profile.Costume.Cast, CastPower);
                _castTarget = Game.Player.transform.position + _castDir * CastDistance;
                _castTarget.y = FieldMap.WaterY + 0.01f;
                if (!FieldMap.IsWater(_castTarget))
                {
                    Game.Toast(CastPower < 0.05f
                        ? "왼쪽 버튼을 누르고 당긴 다음, 빠르게 밀면서 놓으세요."
                        : "물 위로 던져야 합니다.", ToastKind.Warn);
                    ToIdle();
                    return;
                }
                Game.Player.RodPitch = 2f;
                Game.Player.RodPitchSpeed = 40f;   // 채찍처럼 앞으로 휙
                Game.Player.BodyLean = -16f;
                Game.Player.RodYaw = 0f;
                Game.Player.Rod.Kick(-260f);       // 휘두르는 힘에 초릿대가 뒤로 휘었다 튕긴다
                Game.Player.PlayCast();
                _bobber.Launch(Game.Player.RodTip.position, _castTarget);
                Game.Sound.Whoosh();
                Game.CameraRig.Shake(0.25f);
                Game.CameraRig.Punch(-3f);
                FlyingDistance = 0f;
                Casted?.Invoke(CastDistance);
                State = FishingState.Casting;
            }
        }

        float PushSum(float now)
        {
            float sum = 0f;
            foreach (var s in _pushSamples)
                if (now - s.t <= PushWindow) sum += s.dy;
            return sum;
        }

        void UpdateAim(float distance)
        {
            var origin = Game.Player.transform.position;
            var target = origin + _castDir * distance;
            target.y = FieldMap.WaterY + 0.03f;
            CastTargetIsWater = FieldMap.IsWater(target);

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
            // 착수: 낚싯대를 거두며 숨 고르기. 카메라는 잠시 찌를 본 뒤 원래 자리로
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
                    break;
                case FishingState.Bite:
                    shot = Fishing.PlayerSystems.CameraRig.ShotKind.Bite;
                    rig.BiteLookAt = _bobber.Position;
                    break;
                case FishingState.Result:
                    if (_cheering) shot = Fishing.PlayerSystems.CameraRig.ShotKind.Cheer;
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
                School = Game.Schools.At(col, row),
                MapId = FieldMap.Id,
                BaitPotency = Game.Profile.Bait.Potency,
                BigFishBonus = Game.Profile.Costume.BigFish,
            };
            // 소용돌이 한가운데에 떨어뜨리면 입질이 더 빠르다
            _waitCol = col;
            _waitRow = row;
            _waitInSpot = _ctx.School != null && _ctx.School.InSpot(_bobber.Position);
            CastCellLabel = $"{_cellName} · {FieldMap.HabitatName(_ctx.Habitat)}" + (_ctx.School != null ? $" · {_ctx.School.Name}({_ctx.School.SizeLabel})" : "")
                + (_waitInSpot ? " · 어군 한가운데!" : "");
            PendingBite = BiteSelector.Pick(_ctx);
            BigBite = false;

            _waitTimer = BiteSelector.WaitSeconds(_ctx) * slowdown * (_waitInSpot ? 0.7f : 1f);
            _nibblesLeft = PendingBite.Species.Aggression > 0.75f ? Random.Range(0, 2) : Random.Range(0, 4);
            _nextNibbleAt = _waitTimer * Random.Range(0.3f, 0.7f);
            _nibbleActive = false;
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
                float bend = State == FishingState.Bite ? 22f + Mathf.Sin(Time.time * 9f) * 6f : State == FishingState.Waiting ? 5f : 2f;
                player.RodBendTarget = bend;
                player.RodPullDir = dir;
            }
        }

        bool WantsRetrieve => Input.GetKeyDown(KeyCode.R) || Game.Player.MovedThisFrame;

        /// <summary>챔질 준비: 버튼을 누른 채 마우스를 흔들면 낚싯대가 그대로 좌우로 흔들린다 (원작 0:30).</summary>
        void SwayRod(bool bite)
        {
            var player = Game.Player;
            float mx = Input.GetMouseButton(0) && !UiBlocking ? Input.GetAxis("Mouse X") : 0f;
            _swayYaw = Mathf.Clamp(_swayYaw * Mathf.Exp(-5f * Time.deltaTime) + mx * 7f, -32f, 32f);
            player.RodYaw = _swayYaw;
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
            _shake = _shake * Mathf.Exp(-6f * Time.deltaTime) + Mathf.Abs(Input.GetAxis("Mouse X"));
            if (_shake < ShakeThreshold) return false;
            _shakeSpent = true;
            _shake = 0f;
            return true;
        }

        void UpdateWaiting()
        {
            if (WantsRetrieve) { Retrieve("찌를 회수했습니다."); return; }
            if (Time.time >= _castShotUntil) SwayRod(false);
            if (UpdateReel()) { if (State == FishingState.Waiting && DetectShake()) FalseHook(); return; }

            _waitTimer -= Time.deltaTime;
            if (_nibbleActive && Time.time > _nibbleWindowEnd) _nibbleActive = false;

            if (_nibblesLeft > 0 && _waitTimer <= _nextNibbleAt)
            {
                _nibblesLeft--;
                _bobber.Nibble();
                Game.Sound.Tick();
                Game.Player.Rod.Kick(70f); // 예신: 초릿대가 톡
                _nibbleActive = true;
                _nibbleWindowEnd = Time.time + 0.3f;
                _nextNibbleAt = _waitTimer - Random.Range(0.8f, 2f);
            }

            if (_waitTimer <= 0f && _nibblesLeft <= 0)
            {
                State = FishingState.Bite;
                _biteTimer = 0f;
                Game.Player.Rod.Kick(160f); // 본신: 초릿대가 쭉 빨려 들어간다
                BigBite = PendingBite.IsTrophy || PendingBite.Species.Rarity >= 4;
                Game.Sound.Plop();
                if (BigBite) Game.Sound.Heartbeat();
                Game.CameraRig.Shake(0.35f);
                return;
            }

            if (DetectShake()) FalseHook();
        }

        /// <summary>입질 전에 챔질하면 헛챔질. 예신(깔짝)에 속은 경우도 여기.</summary>
        void FalseHook()
        {
            if (Random.value < 0.5f) Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
            ShowGrade(HookGrade.Bad);
            Game.Toast(_nibbleActive ? "헛챔질! 예신에 속았다..." : "헛챔질! 고기가 놀라 달아났다.", ToastKind.Warn);
            Game.Player.RodPitch = 60f;
            BeginWait(1.3f);
        }

        /// <summary>
        /// 원작처럼 대기 중 좌클릭을 누르고 있으면 릴을 감아 찌가 점점 가까이 온다 (좌우로 흔들면 챔질).
        /// 감는 동안은 입질 시계가 멈추고, 손을 뗐을 때 다른 칸이나 어군 한가운데로 옮겨졌으면 그 자리에서 다시 기다린다.
        /// 발밑까지 감으면 찌를 회수한다. 감고 있으면 true.
        /// </summary>
        bool UpdateReel()
        {
            bool hold = Input.GetMouseButton(0) && !_shakeSpent && !UiBlocking;
            if (!hold)
            {
                if (!_reeling) return false;
                _reeling = false;
                Game.Player.RodPitch = 28f;
                FieldMap.TryCell(_bobber.Position, out int col, out int row);
                col = Mathf.Clamp(col, 0, FieldMap.Cols - 1);
                row = Mathf.Clamp(row, 0, FieldMap.Rows - 1);
                var school = Game.Schools.At(col, row);
                bool inSpot = school != null && school.InSpot(_bobber.Position);
                if (col != _waitCol || row != _waitRow || inSpot != _waitInSpot) BeginWait(1f);
                return false;
            }

            _reeling = true;
            var player = Game.Player.transform.position;
            var flat = player - _bobber.Position;
            flat.y = 0f;
            float step = Game.Profile.Reel.ReelSpeed * Time.deltaTime;
            var next = _bobber.Position + flat.normalized * Mathf.Min(step, flat.magnitude);
            if (flat.magnitude - step <= 3f || !FieldMap.IsWater(next))
            {
                Retrieve("찌를 다 감았습니다.");
                return true;
            }
            _bobber.DragTo(next);
            _nibbleActive = false;
            Game.Player.RodPitch = 48f;
            Game.Player.RodPitchSpeed = 8f;
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
            float sink = Mathf.Clamp01(_biteTimer / SinkTime);
            _bobber.SetSink(sink);

            float shown = _bobber.SinkAmount; // 눈에 보이는 찌 위치로 판정해야 공정하다
            bool shook = Autoplay ? shown >= 0.97f : DetectShake();
            if (shook)
            {
                var grade = GradeFor(shown);
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
                Game.Player.PlayHook();
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
                Game.CameraRig.Punch(grade == HookGrade.Perfect ? -9f : -6f);
                HitStop(0.12f, 0.12f);
                return;
            }
            if (_biteTimer > SinkTime + SunkHold)
            {
                Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                Game.Toast("늦었다! 미끼만 따먹혔다. 다시 던지세요.", ToastKind.Warn);
                ToIdle();
            }
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

            State = FishingState.Fighting;
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
            _marker.SetActive(true);
            _aimArc.enabled = true;
            _playerRing.SetActive(true);
            UpdateAim(CastDistance);
            // 당긴 자세와 어깨 근접 구도
            Game.Player.RodPitch = Mathf.Lerp(70f, 155f, Mathf.Clamp01(pullBack / PullFull)) - power * 110f;
            Game.Player.BodyLean = 20f - power * 26f;
            Game.Player.RodYaw = -10f;
            Game.CameraRig.Shot = Fishing.PlayerSystems.CameraRig.ShotKind.Cast;
            Game.CameraRig.CastLookAt = null;
            enabled = false; // 다음 Update에서 마우스 입력으로 덮어쓰지 않게 잠시 멈춘다
        }

        public void DebugResume() => enabled = true;

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
            Game.CameraRig.Shake(1.2f);
            Game.CameraRig.Punch(-8f);
            HitStop(0.09f, 0.15f);
            _rodView.Kick(420f); // 낚싯대가 확 휘었다가 튕겨 돌아온다
            var pos = _rodView.FishWorldPos(Fight);
            Effects.Splash(pos, 30, 1.6f);
            Game.UI.Fight.ShowCounter(Fight.Combo);
            Game.UI.Fight.PopDamage(pos, Fight.LastCounterDamage, true);
        }

        /// <summary>물고기가 날뛸 때: 큰 물보라, 화면이 흔들리고, 낚싯대가 확 끌려간다.</summary>
        void OnBurst()
        {
            Game.Sound.Splash(1f);
            var pos = _rodView.FishWorldPos(Fight);
            Effects.Splash(pos, 22, 1.3f);
            Effects.Ripple(pos, 2.2f, 1f);
            Game.CameraRig.Shake(0.55f);
            Game.CameraRig.Punch(2.5f);
            _rodView.Kick(260f);
        }

        void OnEscapeRun()
        {
            Game.Sound.Splash(1.2f);
            Effects.Splash(_rodView.FishWorldPos(Fight), 24, 1.4f);
            Game.UI.Fight.ShowEscape();
        }

        void UpdateFighting()
        {
            var f = Fight;
            var p = Game.Profile;

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
                f.Tick(Time.deltaTime, default, p.StaminaFactor, p.DurabilityFactor * (1f + p.Costume.Damage));
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
                    MouseDX = Input.GetAxis("Mouse X"),
                };
                f.Tick(Time.deltaTime, input, p.StaminaFactor, p.DurabilityFactor * (1f + p.Costume.Damage));

                // 릴: 감으면 손잡이가 돌고(드르르륵), 줄이 풀려 나가면 스풀이 거꾸로 돌며 드랙이 운다(지이이익)
                float lineOut = Mathf.Max(0f, (f.Distance - _lastDistance) / Mathf.Max(Time.deltaTime, 1e-4f));
                _lastDistance = f.Distance;
                float turns = input.Reeling ? f.Reel.ReelSpeed * 0.9f * p.StaminaFactor * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(f.Tension)) : 0f;
                _rodView.SetReel(turns, lineOut);
                Game.Sound.SetReel(turns, f.Tension);
                Game.Sound.SetDrag(lineOut);
                Game.Sound.SetStrain(Mathf.InverseLerp(FishFight.HighTension, 1f, f.Tension));

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
            _rodView.Hide();
            Game.CameraRig.ExitFight();
            Game.Player.SetVisible(true);
            Game.Player.RodPitch = 35f;

            float wear = Mathf.Lerp(0.5f, 3f, f.Length / 100f) * (f.Tension > 0.9f ? 1.5f : 1f);
            p.Data.rodDurability = Mathf.Max(0f, p.Data.rodDurability - wear);
            if (p.Data.rodDurability <= 0f) Game.Toast("낚싯대가 망가졌습니다! 상점에서 수리하세요.", ToastKind.Warn);

            FightEnded?.Invoke(outcome);

            if (outcome != FightOutcome.Landed)
            {
                Game.Sound.Snap();
                Game.Toast(outcome switch
                {
                    FightOutcome.LineBroken => "툭! 줄이 끊어졌다... 장력을 조심하세요.",
                    FightOutcome.HookOut => "바늘이 빠졌다... 줄을 너무 느슨하게 했어요.",
                    FightOutcome.LineRunOut => "줄이 모두 풀려버렸다...",
                    _ => "놓쳤다..."
                }, ToastKind.Warn);
                Fight = null;
                ToIdle();
                p.NotifyChanged();
                return;
            }

            Game.Sound.Success();
            Game.Player.PlayCheer();
            Effects.Splash(fishPos, 40, 1.8f);
            // 원작 1:12: 결과 화면 전에 캐릭터 얼굴 클로즈업
            _cheering = true;
            Game.Player.RodPitch = 70f;
            Game.Player.BodyLean = 8f;
            var species = f.Species;
            var caught = new CaughtFish
            {
                speciesId = species.Id,
                length = f.Length,
                weight = Mathf.Round(species.WeightGrams(f.Length)),
                day = Game.Clock.Day,
                zone = _cellName,
            };
            var previous = p.Dex(species.Id);
            float prevBest = previous?.maxLength ?? 0f;
            int prevBestDay = previous?.firstDay ?? 0;
            bool newSpecies = !species.IsJunk && p.Dex(species.Id) == null;
            bool record = p.RecordDex(species, caught.length, caught.weight, caught.day);
            int exp = Mathf.RoundToInt(species.Exp * Mathf.Sqrt(f.Length / species.MinLen) * (species.IsTrophy(f.Length) ? 2f : 1f));
            p.Data.totalCatches++;
            p.AddExp(exp);
            Game.Quests.OnCatch(new CatchInfo { Species = species, Length = f.Length, Habitat = _ctx.Habitat, Band = Game.Clock.Band,
                MapId = FieldMap.Id, NewSpecies = newSpecies, Combo = f.Combo });
            Game.Schools.OnCatch(_ctx.School, species.Id);
            Game.Tournament.OnCatch(species, f.Length);

            State = FishingState.Result;
            Fight = null;
            StartCoroutine(ShowResultAfterCheer(caught, exp, record, prevBest, prevBestDay, species, p));
        }

        System.Collections.IEnumerator ShowResultAfterCheer(CaughtFish caught, int exp, bool record, float prevBest, int prevBestDay,
            FishSpecies species, PlayerProfile p)
        {
            yield return new WaitForSecondsRealtime(1.1f);
            _cheering = false;
            Game.UI.Result.Show(caught, exp, record, prevBest, prevBestDay, keep =>
            {
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
                }
                p.NotifyChanged();
                ToIdle();
                Game.Save();
            });
        }

        // ---------- 3인칭 낚싯줄 ----------

        void UpdateLine()
        {
            bool show = State == FishingState.Casting || State == FishingState.Waiting || State == FishingState.Bite;
            _line.enabled = show;
            if (!show) return;

            var a = Game.Player.RodTip.position;
            var b = _bobber.Position + Vector3.up * 0.3f;
            float sag = State == FishingState.Bite ? 0.1f : Vector3.Distance(a, b) * 0.06f;
            var mid = (a + b) * 0.5f + Vector3.down * sag;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
                _line.SetPosition(i, p);
            }
        }
    }
}
