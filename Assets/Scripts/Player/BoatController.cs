using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.PlayerSystems
{
    /// <summary>
    /// 배. 원작처럼 물가에서 불러(B) 타고 나가서, 엔진을 끄면 배 위에서 낚시한다.
    ///  - 물가(둔치·데크 끝)에서 B: 원작 소환 연출 — 먼바다 쪽에서 배가 물보라를 끌며 달려와 물가에 돌아 선다. 그다음 탄다
    ///  - Y: 엔진 START/STOP (원작 계기판 "ENGINE START STOP [Y]"). 시동이 걸려 있으면 낚시를 못 한다 (돛단배는 엔진이 없다)
    ///  - W/S 속도, A/D 방향. 달릴 때는 운전석에 앉고, 멈추면 뱃머리에 서서 낚시할 수 있다
    ///  - 연료를 쓰며 달린다. 다 떨어지면 노를 저어 아주 천천히만 간다 (낚시점에서 채운다)
    ///  - 물가에 대고 B: 내려서 배를 접어 넣는다
    /// 강 건너편 깊은 칸(3~4행)은 둔치에서 던져서는 닿지 않아 배로만 노릴 수 있다.
    /// </summary>
    public class BoatController : MonoBehaviour
    {
        public bool Riding { get; private set; }
        public float Speed { get; private set; }
        public float Throttle { get; private set; }
        public GearDef Def { get; private set; }
        /// <summary>배를 띄운 둔치 위치. 배 위에서 저장하면 이 위치로 저장한다.</summary>
        public Vector3 LandPos { get; private set; }
        public float MaxSpeed => Def == null ? 0f : Def.Sail ? Def.BoatSpeed * WindFactor : Game.Profile.Data.boatFuel > 0f ? Def.BoatSpeed : PaddleSpeed;

        /// <summary>
        /// 돛단배 속도 배율 (0.25~1): 옆바람(바람과 90°)이 가장 빠르고, 뒤바람은 조금 느리고, 맞바람(바람을 거슬러 45° 안)은 거의 못 간다.
        /// 바람이 셀수록 빠르다.
        /// </summary>
        public float WindFactor
        {
            get
            {
                if (Game.Weather == null) return 0.6f;
                float rel = Mathf.Abs(Mathf.DeltaAngle(_yaw, Game.Weather.WindYaw)); // 0 = 뒤바람, 180 = 맞바람
                float shape = rel < 90f ? Mathf.Lerp(0.75f, 1f, rel / 90f) : rel < 135f ? Mathf.Lerp(1f, 0.55f, (rel - 90f) / 45f) : Mathf.Lerp(0.55f, 0.2f, (rel - 135f) / 45f);
                return Mathf.Clamp(shape * Mathf.Lerp(0.6f, 1.15f, Mathf.InverseLerp(2f, 9f, Game.Weather.WindSpeed)), 0.2f, 1.1f);
            }
        }
        public bool Seated { get; private set; }
        /// <summary>시동이 걸려 있다. 돛단배·연료가 떨어진 배는 엔진 없이 (돛·노).</summary>
        public bool EngineOn { get; private set; }
        /// <summary>엔진이 돌고 있어 낚싯대를 던질 수 없다 (원작: 정지해야 낚시).</summary>
        public bool BlocksCasting => Riding && Def != null && !Def.Sail && EngineOn;
        /// <summary>배가 물가로 달려오는 소환 연출 중.</summary>
        public bool Summoning => _summonT >= 0f;
        /// <summary>자동 촬영용: 소환 연출 없이 바로 띄운다.</summary>
        public bool InstantLaunch;
        float _summonT = -1f;
        Vector3 _summonFrom, _summonLand;
        float _engineWarnAt;
        const float SummonTime = 2.4f;
        /// <summary>배 꼬리 (트롤링 줄이 나가는 곳).</summary>
        public Vector3 SternPosition => _pos - Forward * _halfLen;
        public Vector3 Forward => Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;

        /// <summary>입질이 오면 배를 세운다 (트롤링).</summary>
        public void ForceStop()
        {
            Speed = 0f;
            Throttle = 0f;
        }

        /// <summary>자동 연출용 입력 (NaN이면 키보드).</summary>
        public float DebugThrottle = float.NaN;
        public float DebugSteer;

        const float PaddleSpeed = 1.2f;

        Transform _boat, _seat, _bow, _stern;
        /// <summary>떠 있는 배 몸체 (없으면 null). 캐스팅 장애물 검사에서 뺀다.</summary>
        public Transform Hull => _boat;
        /// <summary>뱃머리 서는 자리와 운전석 (윤슬이 같이 탈 때: 플레이어가 앉아 몰면 뱃머리, 뱃머리에 서면 운전석에).</summary>
        public Transform BowPoint => _bow;
        public Transform SeatPoint => _seat;
        float _halfLen, _halfWidth;
        float _yaw;
        Vector3 _pos;
        TrailRenderer _wake;
        float _nextRipple, _nextBowSplash, _bumpCooldown;
        bool _fuelWarned;

        public void Toggle()
        {
            if (Summoning) return;
            if (Riding) TryDisembark();
            else TryLaunch();
        }

        /// <summary>Y: 엔진 START / STOP.</summary>
        public void ToggleEngine()
        {
            if (!Riding || Def == null) return;
            if (Def.Sail) { Game.Toast("돛단배는 엔진이 없습니다. 바람을 받아 W로 나아갑니다.", ToastKind.Info); return; }
            if (!EngineOn)
            {
                if (Game.Profile.Data.boatFuel <= 0f) { Game.Toast("연료가 없어 시동이 걸리지 않습니다. 노를 저어 W로 천천히 갑니다.", ToastKind.Warn); return; }
                if (Game.Fishing.State != FishingState.Idle && !Game.Fishing.CanTroll)
                {
                    Game.Toast("낚시 중에는 시동을 걸 수 없습니다.", ToastKind.Warn);
                    return;
                }
                EngineOn = true;
                Game.Sound.Splash(0.3f);
                Game.CameraRig.Shake(0.25f);
                Game.SysLog($"{Def.Name} 시동을 걸었습니다.", "#7fd8ff");
            }
            else
            {
                if (Mathf.Abs(Speed) > 1.5f) { Game.Toast("속도를 줄인 뒤 엔진을 끄세요.", ToastKind.Warn); return; }
                EngineOn = false;
                Speed = 0f;
                Throttle = 0f;
                Game.Toast("엔진을 껐습니다. 뱃머리에서 낚시할 수 있습니다.", ToastKind.Info);
            }
        }

        // ---------------- 띄우기 / 내리기 ----------------

        void TryLaunch()
        {
            var p = Game.Profile;
            if (p.Boat == null)
            {
                Game.Toast("배가 없습니다. 낚시점(F12)에서 고무보트를 사 보세요. (Lv.3)", ToastKind.Warn);
                return;
            }
            if (Game.Fishing.State != FishingState.Idle)
            {
                Game.Toast("낚시 중에는 배를 꺼낼 수 없습니다.", ToastKind.Warn);
                return;
            }
            Def = p.Boat;
            SetSize();

            // 바라보는 방향을 먼저, 점점 옆으로 넓혀 가며 배가 들어갈 물을 찾는다
            var player = Game.Player;
            var origin = player.transform.position;
            float faceYaw = player.Yaw;
            for (float r = _halfWidth + 1.2f; r <= 9f; r += 0.5f)
            for (int k = 0; k <= 12; k++)
            {
                float yaw = faceYaw + (k % 2 == 0 ? 1 : -1) * ((k + 1) / 2) * 15f;
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var c = origin + dir * r;
                c.y = FieldMap.WaterY;
                if (!HullClear(c, yaw)) continue;
                Launch(c, yaw, origin);
                return;
            }
            Game.Toast("물가에서만 배를 꺼낼 수 있습니다. 둔치나 낚시 데크 끝에서 물 쪽을 보고 B를 누르세요.", ToastKind.Warn);
        }

        void Launch(Vector3 at, float yaw, Vector3 landPos)
        {
            LandPos = landPos;
            _pos = at;
            _yaw = yaw;
            Speed = 0f;
            BuildModel();
            EngineOn = false;
            if (!InstantLaunch && BeginSummon(at, yaw)) return;
            Board(at);
        }

        /// <summary>
        /// 원작 소환 (clips 03_배_소환_탑승_출항, 4:38~4:45): 빈 바다 쪽에서 배가 흰 물보라를 길게 끌며 달려와
        /// 물가 앞에서 옆으로 돌며 서고, 곧바로 뱃머리 위 3인칭으로 바뀐다. 달려올 물길이 없으면 바로 띄운다.
        /// </summary>
        bool BeginSummon(Vector3 at, float yaw)
        {
            var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            for (float d = 26f; d >= 10f; d -= 2f)
            {
                var from = at + dir * d;
                bool clear = true;
                for (float t = 0.15f; t < 1f && clear; t += 0.15f)
                    if (!FieldMap.IsWater(Vector3.Lerp(from, at, t))) clear = false;
                if (!clear || !FieldMap.IsWater(from)) continue;
                _summonFrom = from;
                _summonLand = at;
                _summonT = 0f;
                _pos = from;
                _yaw = yaw + 180f;    // 물가 쪽을 보고 달려온다
                Speed = Def.BoatSpeed * 0.8f;
                Game.Player.MovementLocked = true;
                UpdateTransform(0f, 0f);
                Game.Toast($"{Def.Name}{(Fishing.UI.GameUI.Josa(Def.Name, "을", "를"))} 불렀습니다!", ToastKind.Good);
                return true;
            }
            return false;
        }

        void UpdateSummon(float dt)
        {
            _summonT += dt;
            float k = Mathf.Clamp01(_summonT / SummonTime);
            // 앞 65%는 곧장 달려오고, 나머지는 속도를 줄이며 180° 돌아 물가에 옆으로 선다
            float travel = 1f - (1f - k) * (1f - k);
            var prev = _pos;
            _pos = Vector3.Lerp(_summonFrom, _summonLand, travel);
            float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, k));
            float baseYaw = Quaternion.LookRotation(_summonFrom - _summonLand).eulerAngles.y; // 띄울 방향 (먼바다 쪽)
            _yaw = baseYaw + 180f * (1f - turn);
            Speed = (prev - _pos).magnitude / Mathf.Max(dt, 1e-4f);
            var fwd = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            UpdateTransform(turn > 0f && turn < 1f ? 1f : 0f, dt);
            UpdateWake(fwd, dt);
            // 원작: 흰 물보라를 길게 끌며 달려온다 (뱃머리 양옆 물보라 + 꼬리 파문), 돌 때는 옆으로 크게 튄다
            if (Time.time >= _nextBowSplash)
            {
                _nextBowSplash = Time.time + 0.05f;
                var side = Vector3.Cross(Vector3.up, fwd);
                if (turn > 0.05f && turn < 0.95f) Effects.Splash(_pos - fwd * _halfLen * 0.4f + side * _halfWidth, 14, 1.2f);
                else if (k < 0.6f)
                {
                    Effects.Splash(_pos + fwd * _halfLen * 0.5f + side * _halfWidth * (Random.value < 0.5f ? -1f : 1f), 8, 1.1f);
                    Effects.Ripple(_pos - fwd * _halfLen, 1.6f, 1.8f);
                }
            }
            Game.Sound.SetEngine(0.25f + (1f - k) * 0.75f, Speed / Mathf.Max(1f, Def.BoatSpeed));
            if (k < 1f) return;
            _summonT = -1f;
            Speed = 0f;
            _yaw = baseYaw;
            Board(_summonLand);
        }

        void Board(Vector3 at)
        {
            _pos = at;
            Speed = 0f;
            Riding = true;
            Seated = false;
            _fuelWarned = false;
            Game.Player.Mount(_bow, false);
            UpdateTransform(0f, 0f);
            Game.Player.SnapToMount();
            Effects.Splash(at, 30, 1.2f);
            Effects.Ripple(at, 3f, 1.5f);
            Game.Sound.Splash(1f);
            Game.Player.MovementLocked = false;
            Game.Toast(Def.Sail ? $"{Def.Name}에 탔습니다. W/S 속도 · A/D 방향 · 멈추면 낚시 · 물가에서 B로 내리기"
                : $"{Def.Name}에 탔습니다. Y 시동 · W/S 속도 · A/D 방향 · 엔진을 끄면 낚시 · 물가에서 B로 내리기", ToastKind.Good);
        }

        void TryDisembark()
        {
            if (Game.Fishing.State != FishingState.Idle)
            {
                Game.Toast("낚시 중에는 내릴 수 없습니다.", ToastKind.Warn);
                return;
            }
            if (Mathf.Abs(Speed) > 1.5f)
            {
                Game.Toast("속도를 줄여야 내릴 수 있습니다.", ToastKind.Warn);
                return;
            }
            // 배 둘레에서 가장 가까운 걸을 수 있는 곳
            Vector3 best = Vector3.zero;
            float bestDist = float.MaxValue;
            for (float r = 0.5f; r <= 6f; r += 0.5f)
            for (int a = 0; a < 24; a++)
            {
                var q = _pos + Quaternion.Euler(0f, a * 15f, 0f) * Vector3.forward * (r + _halfWidth);
                if (!FieldMap.IsWalkable(q)) continue;
                float d = (q - _pos).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = q; }
            }
            if (bestDist == float.MaxValue)
            {
                Game.Toast("물가(둔치나 낚시 데크)에 가까이 대야 내릴 수 있습니다.", ToastKind.Warn);
                return;
            }
            best.y = FieldMap.LandY + 0.1f;
            EngineOn = false;
            var face = _pos - best;
            face.y = 0f;
            Stow(best, Quaternion.LookRotation(face.sqrMagnitude > 0.01f ? face : Vector3.forward).eulerAngles.y);
            Game.Toast("배에서 내렸습니다.", ToastKind.Info);
        }

        /// <summary>대기실로 나갈 때 등: 배를 띄운 둔치로 돌려보낸다.</summary>
        public void StowToLand()
        {
            if (!Riding) return;
            Stow(LandPos, Game.Player.Yaw);
        }

        void Stow(Vector3 landAt, float yaw)
        {
            Effects.Splash(_pos, 20, 1f);
            Game.Sound.Splash(0.7f);
            Riding = false;
            Speed = 0f;
            EngineOn = false;
            _summonT = -1f;
            Game.Player.Dismount(landAt, yaw);
            if (_boat != null) Destroy(_boat.gameObject);
            if (_wake != null) Destroy(_wake.gameObject);
            _boat = null;
            Game.Save();
        }

        // ---------------- 운전 ----------------

        void Update()
        {
            if (Summoning)
            {
                if (Game.Mode != GameMode.Field) { _summonT = -1f; if (_boat != null) Destroy(_boat.gameObject); _boat = null; Game.Player.MovementLocked = false; return; }
                UpdateSummon(Time.deltaTime);
                return;
            }
            if (!Riding) return;
            if (Game.Mode != GameMode.Field) { StowToLand(); return; }
            float dt = Time.deltaTime;

            // 트롤링: 루어·생미끼를 던져 둔 채로 천천히 몰 수 있다
            bool canDrive = (Game.Fishing.State == FishingState.Idle || Game.Fishing.CanTroll) && !(Game.UI != null && Game.UI.BlockingPanelOpen);
            float throttleIn = 0f, steer = 0f;
            if (canDrive)
            {
                if (!float.IsNaN(DebugThrottle))
                {
                    if (!Def.Sail && DebugThrottle != 0f) EngineOn = true;
                    throttleIn = DebugThrottle;
                    steer = DebugSteer;
                }
                else
                {
                    if (Settings.Held(KeyAction.MoveForward)) throttleIn += 1f;
                    if (Settings.Held(KeyAction.MoveBack)) throttleIn -= 0.4f;
                    if (Settings.Held(KeyAction.MoveRight)) steer += 1f;
                    if (Settings.Held(KeyAction.MoveLeft)) steer -= 1f;
                    // 엔진 배는 시동을 걸어야 간다 (연료가 떨어졌으면 노)
                    bool needEngine = !Def.Sail && Game.Profile.Data.boatFuel > 0f && !EngineOn;
                    if (needEngine && (throttleIn != 0f || steer != 0f))
                    {
                        throttleIn = 0f;
                        steer = 0f;
                        if (Time.time >= _engineWarnAt)
                        {
                            _engineWarnAt = Time.time + 4f;
                            Game.Toast($"시동이 꺼져 있습니다. {Settings.KeyName(KeyAction.Engine)}를 눌러 시동을 거세요.", ToastKind.Warn);
                        }
                    }
                }
            }
            Throttle = Mathf.MoveTowards(Throttle, Mathf.Abs(throttleIn), dt * 2f);

            float max = MaxSpeed;
            float accel = throttleIn != 0f ? max * 0.4f : max * 0.25f + 0.5f;
            Speed = Mathf.MoveTowards(Speed, throttleIn * max, accel * dt);

            // 방향: 달릴수록 잘 돈다. 멈춰 있어도 선외기로 천천히 돈다
            float turnRate = Mathf.Lerp(22f, 55f, Mathf.Clamp01(Mathf.Abs(Speed) / Mathf.Max(1f, max))) * (Speed < -0.1f ? -1f : 1f);
            float newYaw = _yaw + steer * turnRate * dt;
            if (HullClear(_pos, newYaw)) _yaw = newYaw;

            var fwd = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            var next = _pos + fwd * Speed * dt;
            _bumpCooldown -= dt;
            if (HullClear(next, _yaw))
            {
                float moved = (next - _pos).magnitude;
                _pos = next;
                if (!Def.Sail && Game.Profile.Data.boatFuel > 0f && Mathf.Abs(Speed) > PaddleSpeed)
                    Game.Profile.Data.boatFuel = Mathf.Max(0f, Game.Profile.Data.boatFuel - moved / 1000f * Def.FuelPerKm);
            }
            else
            {
                // 둑·교각·데크에 부딪힘
                if (_bumpCooldown <= 0f && Mathf.Abs(Speed) > 1f)
                {
                    _bumpCooldown = 0.6f;
                    Effects.Splash(_pos + fwd * _halfLen, 20, 1f);
                    Game.Sound.Splash(0.8f);
                    Game.CameraRig.Shake(Mathf.Clamp01(Mathf.Abs(Speed) / 8f));
                }
                Speed = -Speed * 0.2f;
            }

            if (!Def.Sail && Game.Profile.Data.boatFuel <= 0f && !_fuelWarned)
            {
                _fuelWarned = true;
                Game.Toast("연료가 떨어졌습니다. 노를 저어 천천히 돌아가세요. (낚시점에서 연료 채우기)", ToastKind.Warn);
            }

            UpdateTransform(steer, dt);

            // 달릴 때는 운전석, 멈추면 뱃머리에 서서 낚시
            bool seated = Mathf.Abs(Speed) > 0.35f || throttleIn != 0f;
            if (seated != Seated)
            {
                Seated = seated;
                Game.Player.Mount(seated ? _seat : _bow, seated);
            }
            Game.Player.SnapToMount();

            UpdateWake(fwd, dt);
            if (!Def.Sail) Game.Sound.SetEngine(Game.Profile.Data.boatFuel > 0f && EngineOn ? 0.15f + Throttle * 0.85f : 0f, Mathf.Abs(Speed) / Mathf.Max(1f, Def.BoatSpeed));
            if (EngineOn && Game.Profile.Data.boatFuel <= 0f) EngineOn = false;

            // 달리는 동안 카메라가 배 뒤로 천천히 돈다 (오른쪽 드래그 중이면 그대로)
            if (Seated && Mathf.Abs(Speed) > 1f && !Input.GetMouseButton(1))
                Game.CameraRig.Yaw = Mathf.LerpAngle(Game.CameraRig.Yaw, _yaw, dt * 2f);
        }

        void UpdateTransform(float steer, float dt)
        {
            if (_boat == null) return;
            float t = Time.time;
            float s01 = Mathf.Clamp01(Mathf.Abs(Speed) / Mathf.Max(1f, Def.BoatSpeed));
            // 파도가 높고 배가 작을수록 크게 흔들린다 (고무보트 > 고속보트·돛단배 > 크루저)
            float wave = (Game.Weather != null ? Game.Weather.WaveFactor : 1f) * Mathf.Lerp(2.2f, 0.35f, Def.Stability)
                         * (FieldMap.IsFarSea(_pos) ? 1.4f : 1f);
            float y = FieldMap.WaterY + Mathf.Sin(t * 1.6f) * 0.04f * wave + s01 * 0.08f;
            float pitch = -s01 * 7f + Mathf.Sin(t * 1.25f) * 1.2f * wave + (s01 > 0.3f ? Mathf.Sin(t * 7f) * 0.6f * s01 * wave : 0f);
            float roll = Mathf.Sin(t * 1.05f) * 1.8f * wave + Mathf.Sin(t * 2.3f + 1f) * 0.6f * wave - steer * s01 * (Def.Sail ? 14f : 8f);
            if (Def.Sail && _sail != null)
            {
                // 돛이 바람 쪽으로 돈다 (붐 각도), 옆바람이면 배가 조금 기운다
                float rel = Mathf.DeltaAngle(_yaw, Game.Weather != null ? Game.Weather.WindYaw : 0f);
                _sail.localRotation = Quaternion.Euler(0f, Mathf.Clamp(rel * 0.45f, -70f, 70f), 0f);
                roll += Mathf.Sign(rel) * Mathf.Sin(Mathf.Abs(rel) * Mathf.Deg2Rad) * 6f * s01;
            }
            _boat.SetPositionAndRotation(new Vector3(_pos.x, y, _pos.z), Quaternion.Euler(pitch, _yaw, roll));
        }

        void UpdateWake(Vector3 fwd, float dt)
        {
            float speed = Mathf.Abs(Speed);
            var stern = _pos - fwd * _halfLen;
            stern.y = FieldMap.WaterY + 0.03f;
            if (_wake != null)
            {
                _wake.transform.position = stern;
                _wake.emitting = speed > 0.8f;
                _wake.widthMultiplier = Mathf.Lerp(0.8f, 1.6f, speed / 14f);
            }
            if (speed > 1.2f && Time.time >= _nextRipple)
            {
                _nextRipple = Time.time + Mathf.Lerp(0.25f, 0.08f, speed / 14f);
                Effects.Ripple(stern, 1f + speed * 0.12f, 1.8f);
            }
            if (speed > 4f && Time.time >= _nextBowSplash)
            {
                _nextBowSplash = Time.time + Random.Range(0.15f, 0.35f);
                var right = Vector3.Cross(Vector3.up, fwd);
                Effects.Splash(_pos + fwd * (_halfLen * 0.6f) + right * (Random.value < 0.5f ? -1f : 1f) * _halfWidth, 6, 0.5f + speed * 0.05f);
            }
        }

        float _farWarnAt;

        /// <summary>배 몸통(앞·가운데·뒤, 양옆)이 모두 물 위인지. 먼바다에 못 나가는 배(고무보트)는 거기서 막힌다.</summary>
        bool HullClear(Vector3 c, float yaw)
        {
            if (Def != null && !Def.OpenSea && FieldMap.IsFarSea(c))
            {
                if (Time.time >= _farWarnAt)
                {
                    _farWarnAt = Time.time + 6f;
                    Game.Toast("파도가 높아 고무보트로는 먼바다에 나갈 수 없습니다. 고속보트나 크루저가 필요합니다.", ToastKind.Warn);
                }
                return false;
            }
            var rot = Quaternion.Euler(0f, yaw, 0f);
            for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j += 2)
            {
                var p = c + rot * new Vector3(j * _halfWidth, 0f, i * _halfLen);
                if (!FieldMap.IsWater(p)) return false;
            }
            if (!FieldMap.IsWater(c + rot * new Vector3(0f, 0f, _halfLen + 0.3f))) return false;
            // 물 위에 있는 것(정박한 배·가두리·잔교·바위기둥 …)을 뚫고 지나가지 않게
            foreach (var col in Physics.OverlapBox(new Vector3(c.x, FieldMap.WaterY + 0.7f, c.z), new Vector3(_halfWidth, 0.5f, _halfLen), rot, ~0, QueryTriggerInteraction.Ignore))
            {
                if (col is TerrainCollider || (_boat != null && col.transform.IsChildOf(_boat))) continue;
                if (Game.Player != null && col.transform.IsChildOf(Game.Player.transform)) continue;
                return false;
            }
            return true;
        }

        // ---------------- 모델 ----------------

        Transform _sail;

        void SetSize()
        {
            (_halfLen, _halfWidth) = Def.Id switch
            {
                "boat_speed" => (2.6f, 1.0f),
                "boat_blackspear" => (2.7f, 1.0f),
                "boat_sail" => (2.5f, 0.95f),
                "boat_cruiser" => (4.4f, 1.55f),
                _ => (1.65f, 0.8f),
            };
        }

        void BuildModel()
        {
            if (_boat != null) Destroy(_boat.gameObject);
            _boat = new GameObject("Boat_" + Def.Id).transform;
            _sail = null;
            switch (Def.Id)
            {
                case "boat_speed": BuildSpeedBoat(); break;
                case "boat_blackspear": BuildBlackSpear(); break;
                case "boat_sail": BuildSailBoat(); break;
                case "boat_cruiser": BuildCruiser(); break;
                default: BuildRubberBoat(); break;
            }

            var wakeGo = new GameObject("BoatWake");
            wakeGo.transform.rotation = Quaternion.LookRotation(Vector3.up); // 수면에 눕힌다
            _wake = wakeGo.AddComponent<TrailRenderer>();
            _wake.alignment = LineAlignment.TransformZ;
            _wake.time = 2.6f;
            _wake.minVertexDistance = 0.4f;
            _wake.widthCurve = new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(1f, 4f));
            _wake.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.9f, 0.95f), 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            };
            var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            _wake.sharedMaterial = new Material(shader) { color = Color.white };
            _wake.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _wake.emitting = false;
        }

        Transform Point(string name, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_boat, false);
            t.localPosition = local;
            return t;
        }

        /// <summary>움직이는 배 모델 (tools/blender/boats.py). 월드 좌표 풍화 결은 미끄러져 보이니 입히지 않는다. 없으면 null.</summary>
        GameObject BoatModel(string id, Vector3 pos, float length)
        {
            KenneyProps.NoSurface = true;
            var go = KenneyProps.Spawn("Props/" + id, _boat, pos, 0f, 0f, length, solid: false);
            KenneyProps.NoSurface = false;
            return go;
        }

        /// <summary>고무보트 (boats.py boat_rib: 주황 U자 튜브·고물 원뿔·손잡이 줄·나무 가로대·선외기). 없으면 예전 도형 판.</summary>
        void BuildRubberBoat()
        {
            _seat = Point("Seat", new Vector3(0f, 0.05f, -0.75f));
            _bow = Point("Bow", new Vector3(0f, 0.06f, 0.75f));
            _stern = Point("Stern", new Vector3(0f, 0f, -1.7f));
            if (BoatModel("boat_rib", new Vector3(0f, -0.08f, 0.05f), 3.9f) != null) return;
            var rubber = Mats.Lit(new Color(0.85f, 0.36f, 0.14f), 0.35f);
            var grey = Mats.Lit(new Color(0.3f, 0.32f, 0.35f), 0.2f);
            var wood = Mats.Lit(new Color(0.55f, 0.4f, 0.25f), 0.2f);
            var black = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.5f);
            foreach (int side in new[] { -1, 1 })
                Mats.Prim(PrimitiveType.Capsule, "Tube", _boat, new Vector3(side * 0.58f, 0.16f, -0.05f), new Vector3(0.42f, 1.5f, 0.42f),
                    rubber, false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Sphere, "BowTube", _boat, new Vector3(0f, 0.18f, 1.45f), new Vector3(1.55f, 0.42f, 0.55f), rubber, false);
            Mats.Prim(PrimitiveType.Cube, "Floor", _boat, new Vector3(0f, 0.02f, 0f), new Vector3(0.95f, 0.05f, 2.9f), grey, false);
            Mats.Prim(PrimitiveType.Cube, "Seat", _boat, new Vector3(0f, 0.3f, -0.55f), new Vector3(1.05f, 0.06f, 0.32f), wood, false);
            Mats.Prim(PrimitiveType.Cube, "Transom", _boat, new Vector3(0f, 0.25f, -1.5f), new Vector3(1f, 0.35f, 0.08f), grey, false);
            Mats.Prim(PrimitiveType.Cube, "Motor", _boat, new Vector3(0f, 0.62f, -1.68f), new Vector3(0.26f, 0.36f, 0.3f), black, false);
        }

        /// <summary>돛단배: Kenney 요트 몸통 + 바람 따라 도는 돛 (붐). 뱃머리가 +Z.</summary>
        void BuildSailBoat()
        {
            var white = Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0.6f);
            var navy = Mats.Lit(new Color(0.12f, 0.22f, 0.45f), 0.5f);
            var wood = Mats.Lit(new Color(0.6f, 0.45f, 0.3f), 0.3f);
            var mast = Mats.Lit(new Color(0.85f, 0.85f, 0.88f), 0.7f, 0.6f);
            // 선체: boats.py boat_dinghy (흰 선체·남색 띠·나무 갑판·센터보드·키). 없으면 예전 상자
            if (BoatModel("boat_dinghy", new Vector3(0f, -0.2f, -0.3f), 4.6f) == null)
            {
                Mats.Prim(PrimitiveType.Cube, "Hull", _boat, new Vector3(0f, 0.1f, -0.35f), new Vector3(1.8f, 0.6f, 3.6f), white, false);
                Mats.Prim(PrimitiveType.Cube, "Bow", _boat, new Vector3(0f, 0.1f, 1.45f), new Vector3(1.28f, 0.6f, 1.28f), white, false, Quaternion.Euler(0f, 45f, 0f));
                Mats.Prim(PrimitiveType.Cube, "Stripe", _boat, new Vector3(0f, -0.14f, -0.35f), new Vector3(1.82f, 0.14f, 3.62f), navy, false);
                Mats.Prim(PrimitiveType.Cube, "Deck", _boat, new Vector3(0f, 0.41f, -0.2f), new Vector3(1.6f, 0.02f, 3.8f), wood, false);
                Mats.Prim(PrimitiveType.Cube, "Keel", _boat, new Vector3(0f, -0.55f, 0f), new Vector3(0.12f, 0.7f, 1.4f), navy, false);
            }
            Mats.Prim(PrimitiveType.Cylinder, "Mast", _boat, new Vector3(0f, 3.6f, 0.5f), new Vector3(0.1f, 3.2f, 0.1f), mast, false);
            _sail = Point("SailPivot", new Vector3(0f, 0.9f, 0.5f));
            Mats.Prim(PrimitiveType.Cylinder, "Boom", _sail, new Vector3(0f, 0.15f, -1.2f), new Vector3(0.07f, 1.2f, 0.07f), mast, false, Quaternion.Euler(90f, 0f, 0f));
            // 삼각 돛: 얇은 판을 기울여 세운다
            var sail = Mats.Lit(new Color(0.97f, 0.96f, 0.92f), 0.2f);
            Mats.Prim(PrimitiveType.Cube, "Sail", _sail, new Vector3(0f, 2.4f, -1.0f), new Vector3(0.03f, 4.4f, 2.0f), sail, false, Quaternion.Euler(-14f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "SailStripe", _sail, new Vector3(0.02f, 1.2f, -0.85f), new Vector3(0.03f, 0.3f, 1.6f), navy, false, Quaternion.Euler(-14f, 0f, 0f));
            _seat = Point("Seat", new Vector3(0f, 0.42f, -1.5f));
            _bow = Point("Bow", new Vector3(0f, 0.42f, 1.3f));
            _stern = Point("Stern", new Vector3(0f, 0f, -2.5f));
        }

        /// <summary>낚시 크루저: Kenney 어선 몸통(9m) + 선장·동행 꾼 NPC.</summary>
        void BuildCruiser()
        {
            if (BoatModel("boat_kr", new Vector3(0f, -0.5f, 0f), 9f) == null)
                Mats.Prim(PrimitiveType.Cube, "Hull", _boat, new Vector3(0f, 0.3f, 0f), new Vector3(3f, 1.2f, 8.6f), Mats.Lit(new Color(0.9f, 0.9f, 0.92f), 0.5f), false);
            Fishing.Characters.NpcAngler.Spawn(_boat, new Vector3(0.4f, 0.75f, -1.8f), 0f, new Fishing.Characters.CharacterLook { Name = "김선장", Title = "크루저 선장" }, Fishing.Characters.NpcAngler.Role.Sitter, false);
            Fishing.Characters.NpcAngler.Spawn(_boat, new Vector3(-1.1f, 0.75f, 0.5f), -90f, new Fishing.Characters.CharacterLook { Name = "동행 꾼", Title = "" }, Fishing.Characters.NpcAngler.Role.Angler, false);
            _seat = Point("Seat", new Vector3(0f, 0.75f, -1.2f));
            _bow = Point("Bow", new Vector3(0f, 0.85f, 3f));
            _stern = Point("Stern", new Vector3(0f, 0f, -4.4f));
        }

        /// <summary>출석 보상 블랙스피어: Kenney 모터보트(c형)를 검게 칠한다 (선실 안이 비치지 않게 덧대는 판은 두지 않는다).</summary>
        void BuildBlackSpear()
        {
            var go = BoatModel("boat_speed", new Vector3(0f, -0.3f, 0f), 5.4f);
            if (go == null) { BuildSpeedBoat(); return; }
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                foreach (var m in mats) if (m.HasProperty("_Color")) m.color *= new Color(0.22f, 0.22f, 0.26f);
                r.materials = mats;
            }
            _seat = Point("Seat", new Vector3(0f, 0.62f, -0.2f));
            _bow = Point("Bow", new Vector3(0f, 0.72f, 1.7f));
            _stern = Point("Stern", new Vector3(0f, 0f, -2.7f));
        }

        void BuildSpeedBoat()
        {
            if (KenneyProps.Available)
            {
                // Kenney Watercraft Kit 모터보트 (CC0). 길이 5.2m, 바닥이 물에 0.4m 잠긴다
                // 뒤쪽에 수상스키 견인 아치가 있는 모터보트. 뱃머리가 +Z
                BoatModel("boat_speed", new Vector3(0f, -0.3f, 0f), 5.2f);
                _seat = Point("Seat", new Vector3(0f, 0.62f, -0.2f));
                _bow = Point("Bow", new Vector3(0f, 0.72f, 1.6f));
                _stern = Point("Stern", new Vector3(0f, 0f, -2.6f));
                return;
            }
            var white = Mats.Lit(new Color(0.93f, 0.94f, 0.95f), 0.6f);
            var navy = Mats.Lit(new Color(0.1f, 0.2f, 0.45f), 0.5f);
            var deck = Mats.Lit(new Color(0.75f, 0.73f, 0.68f), 0.3f);
            var glass = Mats.Lit(new Color(0.15f, 0.25f, 0.3f), 0.9f, 0.3f);
            var black = Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.5f);
            Mats.Prim(PrimitiveType.Cube, "Hull", _boat, new Vector3(0f, 0.15f, -0.45f), new Vector3(2f, 0.7f, 3.9f), white, false);
            Mats.Prim(PrimitiveType.Cube, "Bow", _boat, new Vector3(0f, 0.15f, 1.5f), new Vector3(1.42f, 0.7f, 1.42f), white, false, Quaternion.Euler(0f, 45f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Stripe", _boat, new Vector3(0f, -0.12f, -0.45f), new Vector3(2.02f, 0.2f, 3.92f), navy, false);
            Mats.Prim(PrimitiveType.Cube, "BowStripe", _boat, new Vector3(0f, -0.12f, 1.5f), new Vector3(1.44f, 0.2f, 1.44f), navy, false, Quaternion.Euler(0f, 45f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Deck", _boat, new Vector3(0f, 0.505f, -0.3f), new Vector3(1.8f, 0.02f, 4f), deck, false);
            Mats.Prim(PrimitiveType.Cube, "Console", _boat, new Vector3(0.35f, 0.88f, -0.35f), new Vector3(0.7f, 0.75f, 0.55f), white, false);
            Mats.Prim(PrimitiveType.Cube, "Windshield", _boat, new Vector3(0.35f, 1.38f, -0.15f), new Vector3(0.8f, 0.36f, 0.04f), glass, false,
                Quaternion.Euler(-25f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, "Wheel", _boat, new Vector3(0.35f, 1.15f, -0.68f), new Vector3(0.28f, 0.015f, 0.28f), black, false,
                Quaternion.Euler(60f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "SeatBase", _boat, new Vector3(0.35f, 0.7f, -1.2f), new Vector3(0.55f, 0.38f, 0.5f), navy, false);
            Mats.Prim(PrimitiveType.Cube, "SeatBack", _boat, new Vector3(0.35f, 1.05f, -1.45f), new Vector3(0.55f, 0.45f, 0.1f), navy, false);
            foreach (int side in new[] { -1, 1 })
                Mats.Prim(PrimitiveType.Cylinder, "Rail", _boat, new Vector3(side * 0.85f, 0.75f, 0.8f), new Vector3(0.03f, 0.9f, 0.03f),
                    Mats.Lit(new Color(0.8f, 0.82f, 0.85f), 0.9f, 0.9f), false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cube, "Motor", _boat, new Vector3(0f, 0.75f, -2.55f), new Vector3(0.42f, 0.6f, 0.45f), black, false);
            Mats.Prim(PrimitiveType.Cube, "MotorCowl", _boat, new Vector3(0f, 1.08f, -2.55f), new Vector3(0.38f, 0.12f, 0.42f),
                Mats.Lit(new Color(0.85f, 0.15f, 0.12f), 0.6f), false);
            Mats.Prim(PrimitiveType.Cylinder, "Shaft", _boat, new Vector3(0f, 0.1f, -2.6f), new Vector3(0.08f, 0.35f, 0.08f), black, false);
            _seat = Point("Seat", new Vector3(0.35f, 0.5f, -1.15f));
            _bow = Point("Bow", new Vector3(0f, 0.52f, 1.3f));
            _stern = Point("Stern", new Vector3(0f, 0f, -2.6f));
        }
    }
}
