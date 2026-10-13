using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.PlayerSystems
{
    /// <summary>
    /// 평소에는 마우스 오른쪽 드래그로 도는 3인칭 카메라,
    /// 파이팅 중에는 원작처럼 1인칭 시점으로 전환한다.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public float Yaw;
        public float Pitch = 16f;
        public float Distance = 7f;

        bool _fight;
        Vector3 _fightPos;
        Vector3 _fightDir;
        float _fightLook;
        float _shake;

        void Awake()
        {
            Cam = gameObject.AddComponent<Camera>();
            Cam.nearClipPlane = 0.05f;
            Cam.farClipPlane = 1200f;
            Cam.fieldOfView = 60f;
            gameObject.AddComponent<AudioListener>();
            tag = "MainCamera";
        }

        public void EnterFight(Vector3 eye, Vector3 dir)
        {
            _fight = true;
            _fightPos = eye;
            _fightDir = new Vector3(dir.x, 0f, dir.z).normalized;
            _fightLook = 0f;
        }

        public void ExitFight()
        {
            _fight = false;
            Yaw = Quaternion.LookRotation(_fightDir).eulerAngles.y;
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        void LateUpdate()
        {
            if (Game.Player == null) return;
            _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 1.5f);
            var shakeOffset = Random.insideUnitSphere * _shake * 0.08f;

            if (_fight)
            {
                // 물고기가 좌우로 달리면 시선이 살짝 따라간다
                var fight = Game.Fishing != null ? Game.Fishing.Fight : null;
                float targetLook = fight != null ? fight.FishX * 9f + fight.RodX * 3f : 0f;
                _fightLook = Mathf.Lerp(_fightLook, targetLook, Time.deltaTime * 3f);
                var rot = Quaternion.LookRotation(_fightDir) * Quaternion.Euler(9f, _fightLook, 0f);
                transform.SetPositionAndRotation(_fightPos + shakeOffset, rot);
                return;
            }

            bool uiBlocking = Game.UI != null && Game.UI.BlockingPanelOpen;
            if (Input.GetMouseButton(1) && !uiBlocking)
            {
                Yaw += Input.GetAxis("Mouse X") * 3f;
                Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 2f, -5f, 70f);
            }
            if (!uiBlocking)
                Distance = Mathf.Clamp(Distance - Input.mouseScrollDelta.y * 0.8f, 3f, 16f);

            // 어깨 너머 시점: 캐릭터 머리가 찌를 가리지 않도록 살짝 오른쪽을 본다
            var orbit = Quaternion.Euler(Pitch, Yaw, 0f);
            var target = Game.Player.transform.position + Vector3.up * 1.6f + (orbit * Vector3.right) * 0.75f;
            var desired = target - orbit * Vector3.forward * Distance;
            desired.y = Mathf.Max(desired.y, HanRiverMap.WaterY + 0.5f);

            // 나무·건물에 가리면 카메라를 앞으로 당긴다 (플레이어 자신은 콜라이더가 CharacterController뿐)
            var toCam = desired - target;
            if (Physics.SphereCast(target, 0.3f, toCam.normalized, out var hit, toCam.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.gameObject != Game.Player.gameObject)
                desired = target + toCam.normalized * Mathf.Max(1.2f, hit.distance - 0.2f);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 12f) + shakeOffset;
            transform.rotation = Quaternion.LookRotation(target - transform.position);
        }
    }
}
