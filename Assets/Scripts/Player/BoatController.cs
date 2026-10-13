using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.PlayerSystems
{
    /// <summary>
    /// 배. 원작처럼 물가에서 꺼내(B) 타고 나가서, 멈추면 배 위에서 낚시한다.
    ///  - 물가(둔치·데크 끝)에서 B: 앞쪽 물 위에 배를 띄우고 탄다
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
        public float MaxSpeed => Def == null ? 0f : Game.Profile.Data.boatFuel > 0f ? Def.BoatSpeed : PaddleSpeed;
        public bool Seated { get; private set; }

        /// <summary>자동 연출용 입력 (NaN이면 키보드).</summary>
        public float DebugThrottle = float.NaN;
        public float DebugSteer;

        const float PaddleSpeed = 1.2f;

        Transform _boat, _seat, _bow, _stern;
        float _halfLen, _halfWidth;
        float _yaw;
        Vector3 _pos;
        TrailRenderer _wake;
        float _nextRipple, _nextBowSplash, _bumpCooldown;
        bool _fuelWarned;

        public void Toggle()
        {
            if (Riding) TryDisembark();
            else TryLaunch();
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
            Riding = true;
            Seated = false;
            _fuelWarned = false;
            Game.Player.Mount(_bow, false);
            UpdateTransform(0f, 0f);
            Game.Player.SnapToMount();
            Effects.Splash(at, 30, 1.2f);
            Effects.Ripple(at, 3f, 1.5f);
            Game.Sound.Splash(1f);
            Game.Toast($"{Def.Name}를 띄웠습니다. W/S 속도 · A/D 방향 · 멈추면 낚시 · 물가에서 B로 내리기", ToastKind.Good);
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
            Game.Player.Dismount(landAt, yaw);
            if (_boat != null) Destroy(_boat.gameObject);
            if (_wake != null) Destroy(_wake.gameObject);
            _boat = null;
            Game.Save();
        }

        // ---------------- 운전 ----------------

        void Update()
        {
            if (!Riding) return;
            if (Game.Mode != GameMode.Field) { StowToLand(); return; }
            float dt = Time.deltaTime;

            bool canDrive = Game.Fishing.State == FishingState.Idle && !(Game.UI != null && Game.UI.BlockingPanelOpen);
            float throttleIn = 0f, steer = 0f;
            if (canDrive)
            {
                if (!float.IsNaN(DebugThrottle))
                {
                    throttleIn = DebugThrottle;
                    steer = DebugSteer;
                }
                else
                {
                    if (Settings.Held(KeyAction.MoveForward)) throttleIn += 1f;
                    if (Settings.Held(KeyAction.MoveBack)) throttleIn -= 0.4f;
                    if (Settings.Held(KeyAction.MoveRight)) steer += 1f;
                    if (Settings.Held(KeyAction.MoveLeft)) steer -= 1f;
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
                if (Game.Profile.Data.boatFuel > 0f && Mathf.Abs(Speed) > PaddleSpeed)
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

            if (Game.Profile.Data.boatFuel <= 0f && !_fuelWarned)
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
            Game.Sound.SetEngine(Game.Profile.Data.boatFuel > 0f ? 0.15f + Throttle * 0.85f : 0f, Mathf.Abs(Speed) / Mathf.Max(1f, Def.BoatSpeed));

            // 달리는 동안 카메라가 배 뒤로 천천히 돈다 (오른쪽 드래그 중이면 그대로)
            if (Seated && Mathf.Abs(Speed) > 1f && !Input.GetMouseButton(1))
                Game.CameraRig.Yaw = Mathf.LerpAngle(Game.CameraRig.Yaw, _yaw, dt * 2f);
        }

        void UpdateTransform(float steer, float dt)
        {
            if (_boat == null) return;
            float t = Time.time;
            float s01 = Mathf.Clamp01(Mathf.Abs(Speed) / Mathf.Max(1f, Def.BoatSpeed));
            float y = FieldMap.WaterY + Mathf.Sin(t * 1.6f) * 0.04f + s01 * 0.08f;
            float pitch = -s01 * 7f + Mathf.Sin(t * 1.25f) * 1.2f + (s01 > 0.3f ? Mathf.Sin(t * 7f) * 0.6f * s01 : 0f);
            float roll = Mathf.Sin(t * 1.05f) * 1.8f - steer * s01 * 8f;
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

        /// <summary>배 몸통(앞·가운데·뒤, 양옆)이 모두 물 위인지.</summary>
        bool HullClear(Vector3 c, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j += 2)
            {
                var p = c + rot * new Vector3(j * _halfWidth, 0f, i * _halfLen);
                if (!FieldMap.IsWater(p)) return false;
            }
            return FieldMap.IsWater(c + rot * new Vector3(0f, 0f, _halfLen + 0.3f));
        }

        // ---------------- 모델 ----------------

        void SetSize()
        {
            bool speedBoat = Def.Id == "boat_speed";
            _halfLen = speedBoat ? 2.6f : 1.65f;
            _halfWidth = speedBoat ? 1.0f : 0.8f;
        }

        void BuildModel()
        {
            if (_boat != null) Destroy(_boat.gameObject);
            _boat = new GameObject("Boat_" + Def.Id).transform;
            if (Def.Id == "boat_speed") BuildSpeedBoat();
            else BuildRubberBoat();

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

        void BuildRubberBoat()
        {
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
            Mats.Prim(PrimitiveType.Cylinder, "Shaft", _boat, new Vector3(0f, 0.12f, -1.72f), new Vector3(0.06f, 0.32f, 0.06f), black, false);
            Mats.Prim(PrimitiveType.Cylinder, "Tiller", _boat, new Vector3(0f, 0.62f, -1.3f), new Vector3(0.035f, 0.25f, 0.035f), black, false,
                Quaternion.Euler(80f, 0f, 0f));
            _seat = Point("Seat", new Vector3(0f, 0.05f, -0.75f));
            _bow = Point("Bow", new Vector3(0f, 0.06f, 0.75f));
            _stern = Point("Stern", new Vector3(0f, 0f, -1.7f));
        }

        void BuildSpeedBoat()
        {
            if (KenneyProps.Available)
            {
                // Kenney Watercraft Kit 모터보트 (CC0). 길이 5.2m, 바닥이 물에 0.4m 잠긴다
                // 뒤쪽에 수상스키 견인 아치가 있는 모터보트. 뱃머리가 +Z
                KenneyProps.Spawn("Kenney/Watercraft/boat-speed-a", _boat, new Vector3(0f, -0.4f, 0f), 0f, 0f, 5.2f);
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
