using Fishing.Characters;
using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.PlayerSystems
{
    /// <summary>둔치와 낚시 데크 위를 걸어 다니는 3인칭 캐릭터.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 4.5f;
        public float RunSpeed = 7.5f;

        /// <summary>낚시 중에는 이동을 막는다.</summary>
        public bool MovementLocked;

        public Transform RodTip { get; private set; }
        public Transform Model { get; private set; }
        public Vector3 EyePos => transform.position + Vector3.up * 1.6f;
        public float Yaw => Model.eulerAngles.y;
        public bool MovedThisFrame { get; private set; }

        /// <summary>낚싯대 기울기(도). 0 = 수평, 90 = 수직.</summary>
        public float RodPitch = 35f;

        CharacterController _cc;
        Transform _nameTag;
        CharacterAnimator _anim;
        Transform _hand;
        float _speedNow;
        Transform _rodPivot;
        float _vy;
        float _currentRodPitch = 35f;
        float _bob;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = 0.35f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
            _cc.stepOffset = 0.3f;
            BuildModel();
        }

        void BuildModel()
        {
            Model = new GameObject("Model").transform;
            Model.SetParent(transform, false);

            if (CharacterFactory.Available)
            {
                // Quaternius 캐릭터 (CC0) + 코드로 입힌 옷
                var look = CharacterLook.Player;
                var character = CharacterFactory.Create(look, Model);
                _anim = character.GetComponent<CharacterAnimator>();
                foreach (var t in character.GetComponentsInChildren<Transform>(true))
                    if (t.name == "hand_r") _hand = t;
                _nameTag = CharacterFactory.AddNameTag(transform, look.Name, look.Title);
            }
            else
            {
                BuildPrimitiveModel();
                _nameTag = CharacterFactory.AddNameTag(transform, "강태공", "초보 조사", 2.3f);
            }

            // 낚싯대: 손 위치를 따라가고 기울기는 코드로 정한다 (손 뼈의 축 방향에 의존하지 않게)
            _rodPivot = new GameObject("RodPivot").transform;
            _rodPivot.SetParent(Model, false);
            _rodPivot.localPosition = new Vector3(0.3f, 1.05f, 0.25f);

            var rodMat = Mats.Lit(new Color(0.75f, 0.15f, 0.2f), 0.6f);
            const float rodLen = 2.8f;
            Mats.Prim(PrimitiveType.Cylinder, "Rod", _rodPivot, new Vector3(0f, 0f, rodLen * 0.5f - 0.3f),
                new Vector3(0.03f, rodLen * 0.5f, 0.03f), rodMat, false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, "Grip", _rodPivot, new Vector3(0f, 0f, -0.15f),
                new Vector3(0.045f, 0.18f, 0.045f), Mats.Lit(new Color(0.12f, 0.12f, 0.12f)), false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, "Reel", _rodPivot, new Vector3(0f, -0.07f, 0.05f), new Vector3(0.11f, 0.03f, 0.11f),
                Mats.Lit(new Color(0.75f, 0.75f, 0.78f), 0.8f, 0.7f), false, Quaternion.Euler(0f, 0f, 90f));

            RodTip = new GameObject("RodTip").transform;
            RodTip.SetParent(_rodPivot, false);
            RodTip.localPosition = new Vector3(0f, 0f, rodLen - 0.3f);
        }

        /// <summary>에셋이 없을 때의 프리미티브 캐릭터.</summary>
        void BuildPrimitiveModel()
        {
            var skin = Mats.Lit(new Color(0.98f, 0.82f, 0.7f));
            var shirt = Mats.Lit(new Color(0.95f, 0.95f, 0.97f));
            var vest = Mats.Lit(new Color(0.3f, 0.45f, 0.32f));
            var pants = Mats.Lit(new Color(0.25f, 0.3f, 0.45f));
            var hat = Mats.Lit(new Color(0.92f, 0.85f, 0.65f));
            Mats.Prim(PrimitiveType.Capsule, "Legs", Model, new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.45f), pants, false);
            Mats.Prim(PrimitiveType.Capsule, "Body", Model, new Vector3(0f, 1.1f, 0f), new Vector3(0.62f, 0.42f, 0.5f), shirt, false);
            Mats.Prim(PrimitiveType.Cube, "Vest", Model, new Vector3(0f, 1.15f, 0.02f), new Vector3(0.6f, 0.5f, 0.5f), vest, false);
            Mats.Prim(PrimitiveType.Sphere, "Head", Model, new Vector3(0f, 1.65f, 0f), Vector3.one * 0.42f, skin, false);
            Mats.Prim(PrimitiveType.Cylinder, "HatBrim", Model, new Vector3(0f, 1.82f, 0f), new Vector3(0.7f, 0.01f, 0.7f), hat, false);
            Mats.Prim(PrimitiveType.Cylinder, "HatTop", Model, new Vector3(0f, 1.9f, 0f), new Vector3(0.38f, 0.09f, 0.38f), hat, false);
        }

        /// <summary>캐스팅 동작 (휘두르기).</summary>
        public void PlayCast() => _anim?.PlayOnce(CharacterAnimator.Cast, 1.6f, 0.75f);

        /// <summary>챔질 동작 (낚싯대를 홱 들어 올리기).</summary>
        public void PlayHook() => _anim?.PlayOnce(CharacterAnimator.Hook, 1.2f);

        /// <summary>낚아 올렸을 때.</summary>
        public void PlayCheer() => _anim?.PlayOnce(CharacterAnimator.Interact, 1.2f);

        public void Teleport(Vector3 pos, float yaw)
        {
            _cc.enabled = false;
            transform.position = pos;
            Model.rotation = Quaternion.Euler(0f, yaw, 0f);
            _cc.enabled = true;
        }

        public void FaceYaw(float yaw) => Model.rotation = Quaternion.Euler(0f, yaw, 0f);

        public void SetVisible(bool visible)
        {
            foreach (var r in Model.GetComponentsInChildren<Renderer>()) r.enabled = visible;
        }

        void Update()
        {
            MovedThisFrame = false;
            if (Game.Mode == GameMode.Lobby) return;
            var input = Vector2.zero;
            bool uiBlocking = Game.UI != null && Game.UI.BlockingPanelOpen;
            if (!uiBlocking)
            {
                if (Input.GetKey(KeyCode.W)) input.y += 1f;
                if (Input.GetKey(KeyCode.S)) input.y -= 1f;
                if (Input.GetKey(KeyCode.D)) input.x += 1f;
                if (Input.GetKey(KeyCode.A)) input.x -= 1f;
            }
            bool wantsMove = input.sqrMagnitude > 0.01f;
            MovedThisFrame = wantsMove;

            var motion = Vector3.zero;
            if (wantsMove && !MovementLocked)
            {
                float camYaw = Game.CameraRig != null ? Game.CameraRig.Yaw : 0f;
                var rot = Quaternion.Euler(0f, camYaw, 0f);
                var dir = rot * new Vector3(input.x, 0f, input.y).normalized;
                float speed = Input.GetKey(KeyCode.LeftShift) ? RunSpeed : WalkSpeed;
                motion = dir * speed;
                Model.rotation = Quaternion.Slerp(Model.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);

                // 걸을 수 없는 곳(강물)으로는 못 가게 축별로 미끄러뜨린다
                var next = transform.position + motion * Time.deltaTime;
                if (!HanRiverMap.IsWalkable(next))
                {
                    var nx = transform.position + new Vector3(motion.x, 0f, 0f) * Time.deltaTime;
                    var nz = transform.position + new Vector3(0f, 0f, motion.z) * Time.deltaTime;
                    if (HanRiverMap.IsWalkable(nx)) motion = new Vector3(motion.x, 0f, 0f);
                    else if (HanRiverMap.IsWalkable(nz)) motion = new Vector3(0f, 0f, motion.z);
                    else motion = Vector3.zero;
                }
                _bob += Time.deltaTime * speed * 2.2f;
            }

            if (_cc.isGrounded && _vy < 0f) _vy = -1f;
            _vy -= 20f * Time.deltaTime;
            motion.y = _vy;
            _cc.Move(motion * Time.deltaTime);

            _speedNow = new Vector2(motion.x, motion.z).magnitude;
            if (_anim == null)
            {
                // 프리미티브 캐릭터는 걸을 때 살짝 들썩이는 것으로 대신한다
                float bobOffset = _speedNow > 1f ? Mathf.Abs(Mathf.Sin(_bob)) * 0.06f : 0f;
                Model.localPosition = new Vector3(0f, bobOffset, 0f);
            }
            else UpdateAnimation();

            if (_nameTag != null && Game.CameraRig != null)
                _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - Game.CameraRig.transform.position);
        }

        void UpdateAnimation()
        {
            bool fishing = Game.Fishing != null && Game.Fishing.State != Angling.FishingState.Idle;
            bool running = _speedNow > WalkSpeed + 0.5f;
            if (_speedNow > 0.5f) _anim.SetLoop(running ? CharacterAnimator.Jog : CharacterAnimator.Walk, running ? 1.1f : 1.25f);
            else _anim.SetLoop(fishing ? CharacterAnimator.HoldRod : CharacterAnimator.Idle);
        }

        void LateUpdate()
        {
            // 애니메이션이 손을 움직인 뒤에 낚싯대를 손에 붙인다
            if (_hand != null) _rodPivot.position = _hand.position;
            _currentRodPitch = Mathf.Lerp(_currentRodPitch, RodPitch, Time.deltaTime * 10f);
            _rodPivot.localRotation = Quaternion.Euler(-_currentRodPitch, 0f, 0f);
        }
    }
}
