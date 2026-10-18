using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.PlayerSystems
{
    /// <summary>
    /// 평소에는 마우스 오른쪽 드래그로 도는 3인칭 카메라,
    /// 파이팅 중에는 원작처럼 1인칭 시점으로 전환한다.
    /// 원작 연출을 따라 낚시 단계마다 구도를 바꾼다 (<see cref="Shot"/>):
    ///  - Cast: 당기기 시작하면 어깨 바로 뒤로 다가가고, 던지면 날아가는 찌를 바라본다
    ///  - Bite: 본신이 오면 캐릭터 옆모습 클로즈업 (낚싯대와 찌가 한 화면에)
    ///  - Cheer: 랜딩 직후 캐릭터 얼굴 앞
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public enum ShotKind { Follow, Cast, Bite, Cheer, Catch, Show, Wait }

        /// <summary>랜딩 직후 1인칭에서 낚싯대를 들어 올리며 시선이 위로 따라간다 (원작 1:53~1:54.8). 0~1.</summary>
        public float FightLift;

        public Camera Cam { get; private set; }
        public float Yaw;
        public float Pitch = 16f;
        public float Distance = 7f;

        /// <summary>지금 원하는 구도. FishingController가 매 프레임 정한다.</summary>
        public ShotKind Shot = ShotKind.Follow;
        /// <summary>Cast 구도에서 바라볼 곳 (날아가는 찌). 없으면 앞쪽.</summary>
        public Vector3? CastLookAt;
        /// <summary>Bite 구도에서 바라볼 곳 (찌).</summary>
        public Vector3 BiteLookAt;

        const float BaseFov = 60f;

        bool _fight;
        bool _fixed;
        Vector3 _fixedPos;
        Quaternion _fixedRot;
        Vector3 _fightPos;
        Vector3 _fightDir;
        float _fightLook;
        float _fightRoll;
        float _landingZoom;
        float _shake;
        float _fovPunch;
        Vector3 _kick;
        float _shotBlend;          // 0 = 평소 추적, 1 = 연출 구도
        ShotKind _lastShot, _prevShot;
        Vector3 _shotPos;
        Quaternion _shotRot;

        void Awake()
        {
            Cam = gameObject.AddComponent<Camera>();
            Cam.nearClipPlane = 0.05f;
            Cam.farClipPlane = 1200f;
            Cam.fieldOfView = BaseFov;
            gameObject.AddComponent<AudioListener>();
            tag = "MainCamera";
        }

        public void EnterFight(Vector3 eye, Vector3 dir)
        {
            _fight = true;
            _fightPos = eye;
            _fightDir = new Vector3(dir.x, 0f, dir.z).normalized;
            _fightLook = 0f;
            _shotBlend = 0f;
            Shot = ShotKind.Follow;
        }

        public void ExitFight()
        {
            _fight = false;
            Yaw = Quaternion.LookRotation(_fightDir).eulerAngles.y;
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        /// <summary>순간적으로 화각을 좁혔다 돌아오게 한다 (챔질·카운터의 '쾅'). 음수면 넓어진다.</summary>
        public void Punch(float fovDelta) => _fovPunch = Mathf.Abs(fovDelta) > Mathf.Abs(_fovPunch) ? fovDelta : _fovPunch;

        /// <summary>카메라를 한 방향으로 툭 민다 (월드 기준, 미터). 스프링처럼 돌아온다.</summary>
        public void Kick(Vector3 offset) => _kick += offset;

        /// <summary>대기실처럼 정해진 구도로 고정한다.</summary>
        public void SetFixed(Vector3 pos, Quaternion rot)
        {
            _fixed = true;
            _fixedPos = pos;
            _fixedRot = rot;
            transform.SetPositionAndRotation(pos, rot);
        }

        public void ClearFixed()
        {
            _fixed = false;
            if (Game.Player != null)
                transform.position = Game.Player.transform.position + new Vector3(0f, 3f, -6f);
        }

        void LateUpdate()
        {
            // near가 작을수록 먼 곳 깊이 정밀도가 떨어져 겹친 면이 깜빡인다 (z-fighting). 눈앞 낚싯대가 있는 1인칭만 가깝게
            Cam.nearClipPlane = _fight ? 0.05f : 0.2f;
            if (Game.Player == null) return;
            if (_fixed)
            {
                // 대기실: 아주 천천히 숨 쉬듯 흔들린다
                float t = Time.time * 0.3f;
                transform.SetPositionAndRotation(_fixedPos + new Vector3(Mathf.Sin(t) * 0.05f, Mathf.Sin(t * 1.3f) * 0.03f, 0f), _fixedRot);
                Cam.fieldOfView = BaseFov;
                return;
            }
            float dt = Time.unscaledDeltaTime;
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 1.6f);
            var shakeOffset = Random.insideUnitSphere * _shake * 0.08f;
            _kick = Vector3.Lerp(_kick, Vector3.zero, dt * 9f);
            _fovPunch = Mathf.Lerp(_fovPunch, 0f, dt * 7f);

            if (_fight)
            {
                // 물고기가 좌우로 달리면 시선이 따라가고, 낚싯대를 트는 쪽으로 화면이 살짝 기운다
                var fight = Game.Fishing != null ? Game.Fishing.Fight : null;
                float targetLook = fight != null ? fight.FishX * 12f + fight.RodX * 4f : 0f;
                float targetRoll = fight != null ? -fight.RodX * 3.5f - fight.FishX * 2f : 0f;
                _fightLook = Mathf.Lerp(_fightLook, targetLook, dt * 3f);
                _fightRoll = Mathf.Lerp(_fightRoll, targetRoll, dt * 4f);
                // 릴을 감을 때 몸이 앞뒤로 들썩인다
                float pump = fight != null && Input.GetMouseButton(0) ? Mathf.Sin(Time.time * 9f) * 0.012f : 0f;
                float lift = Mathf.SmoothStep(0f, 1f, FightLift);
                var rot = Quaternion.LookRotation(_fightDir) * Quaternion.Euler(9f - 34f * lift, _fightLook * (1f - lift), _fightRoll * (1f - lift));
                // 랜딩 찬스: 릴 쪽으로 다가가듯 화각을 좁힌다 (원작 1:52)
                bool landing = fight != null && fight.Landing;
                _landingZoom = Mathf.Lerp(_landingZoom, landing ? 1f : 0f, dt * 5f);
                transform.SetPositionAndRotation(_fightPos + shakeOffset + _kick + Vector3.up * (pump + lift * 0.3f), rot);
                Cam.fieldOfView = BaseFov - _landingZoom * 9f + _fovPunch;
                return;
            }

            bool uiBlocking = Game.UI != null && Game.UI.BlockingPanelOpen;
            if (Input.GetMouseButton(1) && !uiBlocking && Shot == ShotKind.Follow)
            {
                Yaw += Input.GetAxis("Mouse X") * 3f * Settings.LookSensitivity;
                Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 2f * Settings.LookSensitivity * (Settings.InvertY ? -1f : 1f), -5f, 70f);
            }
            if (!uiBlocking && Shot == ShotKind.Follow)
                Distance = Mathf.Clamp(Distance - Input.mouseScrollDelta.y * 0.8f, 3f, 16f);

            // 어깨 너머 시점: 캐릭터 머리가 찌를 가리지 않도록 살짝 오른쪽을 본다
            var orbit = Quaternion.Euler(Pitch, Yaw, 0f);
            var target = Game.Player.transform.position + Vector3.up * 1.6f + (orbit * Vector3.right) * 0.75f;
            var desired = target - orbit * Vector3.forward * Distance;
            desired.y = Mathf.Max(desired.y, FieldMap.WaterY + 0.5f);

            // 나무·건물에 가리면 카메라를 앞으로 당긴다 (플레이어 자신은 콜라이더가 CharacterController뿐).
            // 내가 탄 배와 그 위 사람(크루저 선장 등)은 빼야 한다: 걸리면 카메라가 머리 뒤 1.2m로 붙어 모자만 보인다
            var toCam = desired - target;
            float block = float.MaxValue;
            var boat = Game.Boat != null && Game.Player.OnBoat ? Game.Boat.Hull : null;
            foreach (var h in Physics.SphereCastAll(target, 0.3f, toCam.normalized, toCam.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.gameObject == Game.Player.gameObject || (boat != null && h.transform.IsChildOf(boat))) continue;
                block = Mathf.Min(block, h.distance);
            }
            if (block < float.MaxValue)
                desired = target + toCam.normalized * Mathf.Max(1.2f, block - 0.2f);
            var followPos = Vector3.Lerp(transform.position, desired, dt * 12f);
            var followRot = Quaternion.LookRotation(target - followPos);

            // 연출 구도 섞기: 들어갈 때는 빠르게, 빠질 때는 천천히
            // 챔질 클로즈업·고기 잡기·들어 보이기는 원작처럼 끊어서(컷) 들어간다 (0:39.5, 1:55, 1:55.5)
            bool cutIn = (Shot == ShotKind.Bite || Shot == ShotKind.Catch || Shot == ShotKind.Show) && _prevShot != Shot;
            _prevShot = Shot;
            if (Shot != ShotKind.Follow) _lastShot = Shot;
            if (cutIn)
            {
                ComputeShot(Shot, out _shotPos, out _shotRot, out _);
                _shotBlend = 1f;
            }
            _shotBlend = Mathf.MoveTowards(_shotBlend, Shot != ShotKind.Follow ? 1f : 0f, dt * (Shot != ShotKind.Follow ? 3.2f : 1.4f));
            float fov = BaseFov;
            if (_shotBlend > 0.001f)
            {
                ComputeShot(_lastShot, out var sp, out var sr, out float shotFov);
                // 연출 구도 자체도 부드럽게 따라간다 (찌가 날아가는 동안 고개가 따라가도록)
                _shotPos = _shotBlend < 0.02f ? sp : Vector3.Lerp(_shotPos, sp, dt * 8f);
                _shotRot = _shotBlend < 0.02f ? sr : Quaternion.Slerp(_shotRot, sr, dt * 7f);
                float e = _shotBlend * _shotBlend * (3f - 2f * _shotBlend);
                followPos = Vector3.Lerp(followPos, _shotPos, e);
                followRot = Quaternion.Slerp(followRot, _shotRot, e);
                fov = Mathf.Lerp(BaseFov, shotFov, e);
            }
            transform.SetPositionAndRotation(followPos + shakeOffset + _kick, followRot);
            Cam.fieldOfView = fov + _fovPunch;
        }

        void ComputeShot(ShotKind shot, out Vector3 pos, out Quaternion rot, out float fov)
        {
            var player = Game.Player;
            var fwd = player.Model.forward;
            fwd.y = 0f;
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            var root = player.transform.position;
            switch (shot)
            {
                case ShotKind.Cast:
                {
                    // 원작 0:02~0:04: 오른쪽 어깨 바로 뒤, 낮게. 던지면 찌를 따라 고개를 든다
                    pos = root + Vector3.up * 2.1f - fwd * 3.4f + right * 1.2f;
                    // 캐릭터 등과 날아갈 쪽이 함께 보이게 (원작: 캐릭터가 화면 왼쪽 아래 1/3)
                    var far = CastLookAt ?? root + fwd * 14f + Vector3.up * 0.4f;
                    var look = Vector3.Lerp(root + Vector3.up * 1.3f, far, CastLookAt.HasValue ? 0.45f : 0.55f);
                    // 너무 위를 보지 않게 (찌가 높이 떠도 수평선이 화면 안에)
                    var dir = look - pos;
                    dir.y = Mathf.Clamp(dir.y, -dir.magnitude * 0.35f, dir.magnitude * 0.22f);
                    rot = Quaternion.LookRotation(dir);
                    fov = 52f;
                    break;
                }
                case ShotKind.Bite:
                {
                    // 원작 챔질 클로즈업 (2013 홍현 영상 0:39.5~0:42.8): 엉덩이 높이에서 오른쪽 등 뒤에 바짝 붙어 올려다본다.
                    // 화면 왼쪽 아래를 캐릭터 등·어깨가 크게 채우고, 낚싯대는 오른쪽 앞 물로, 위 절반은 밤하늘
                    pos = root + Vector3.up * 1.25f - fwd * 1.6f + right * 1.0f;
                    var look = root + fwd * 10f + right * 2.0f + Vector3.up * 2.4f;
                    rot = Quaternion.LookRotation(look - pos);
                    fov = 56f;
                    break;
                }
                case ShotKind.Wait:
                {
                    // 원작 찌 대기 (2013 홍현 영상 0:24~0:36): 캐릭터 왼쪽 뒤에서 무릎 위로 잡아 화면 왼쪽 1/3에 세우고,
                    // 오른쪽으로 뻗은 낚싯줄 끝 찌까지 한 화면에
                    pos = root + Vector3.up * 1.95f - fwd * 3.3f - right * 1.05f;
                    var look = Vector3.Lerp(root + Vector3.up * 1.25f, BiteLookAt, 0.42f);
                    var dir = look - pos;
                    dir.y = Mathf.Clamp(dir.y, -dir.magnitude * 0.3f, dir.magnitude * 0.05f);
                    rot = Quaternion.LookRotation(dir);
                    fov = 50f;
                    break;
                }
                case ShotKind.Catch:
                {
                    // 원작 1:55: 낚싯대를 치켜든 캐릭터를 앞쪽 왼편에서 옆모습으로, 줄 끝에 고기가 대롱대롱
                    pos = root + Vector3.up * 1.5f + fwd * 1.5f - right * 2.7f;
                    var look = root + Vector3.up * 1.7f + fwd * 1.2f;
                    rot = Quaternion.LookRotation(look - pos);
                    fov = 50f;
                    break;
                }
                case ShotKind.Show:
                {
                    // 원작 1:56~1:58 RESULT: 정면 가까이 가슴 위로, 살짝 올려다본다. 머리 위로 치켜든 주먹과 늘어진 고기는 화면 오른쪽 위
                    var head = player.Head != null ? player.Head.position : root + Vector3.up * 1.65f;
                    pos = head + fwd * 1.8f + right * 0.12f - Vector3.up * 0.2f;
                    var look = head - Vector3.up * 0.08f - right * 0.3f;
                    rot = Quaternion.LookRotation(look - pos);
                    fov = 42f;
                    break;
                }
                default: // Cheer
                {
                    // 원작 1:12: 얼굴 앞 클로즈업
                    var head = player.Head != null ? player.Head.position : root + Vector3.up * 1.65f;
                    pos = head + fwd * 2.6f + right * 0.55f + Vector3.up * 0.1f;
                    rot = Quaternion.LookRotation(head + Vector3.down * 0.35f - pos);
                    fov = 42f;
                    break;
                }
            }
            if (pos.y < FieldMap.WaterY + 0.4f) pos.y = FieldMap.WaterY + 0.4f;
        }
    }
}
