using Fishing.Angling;
using Fishing.Characters;
using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Buddy
{
    /// <summary>
    /// 윤슬의 몸: 낚시터에서 플레이어 뒤 옆을 따라 걷고 뛴다 (VRM, 모델은 characters.json의 npc "윤슬").
    ///  - 멀어지면 뛰고, 너무 멀거나(배로 건넜을 때 등) 막혀서 못 오면 플레이어 옆으로 옮겨 온다.
    ///  - 플레이어가 낚시하는 동안은 옆에 서서 물 쪽을 본다. 반응 말을 하면 동작(환호·박수·손 흔들기)을 한다.
    ///  - 대기실과 배 위에서는 숨는다 (대화는 그대로 된다).
    /// 몸은 플레이어·찌를 막지 않게 Ignore Raycast 층이고, 플레이어와는 부딪히지 않는다.
    /// </summary>
    public class BuddyBody : MonoBehaviour
    {
        // 플레이어 오른쪽 조금 뒤 (바로 뒤에 서면 3인칭 카메라 앞을 가린다)
        const float FollowBack = 0.7f, FollowSide = 1.6f;
        // 플레이어 걷기 4.5 · 달리기 7.5보다 조금 빨라야 따라붙는다
        const float WalkSpeed = 5f, RunSpeed = 8.5f;
        const float TeleportDistance = 22f;

        public static BuddyBody Instance { get; private set; }
        /// <summary>말풍선을 띄울 머리 위 자리 (화면에 보이지 않으면 null).</summary>
        public Vector3? BubbleAnchor => _visible ? transform.position + Vector3.up * 2.05f : (Vector3?)null;

        CharacterController _cc;
        CharacterAnimator _anim;
        Transform _model, _nameTag;
        float _vy, _stuckFor;
        bool _visible = true;
        Vector3 _lastPos;

        public static BuddyBody Spawn()
        {
            var go = new GameObject("Buddy_" + BuddyBrain.BuddyName);
            go.layer = 2; // Ignore Raycast: 카메라 충돌·캐스팅 막힘 판정에 안 걸린다
            Instance = go.AddComponent<BuddyBody>();
            return Instance;
        }

        void Awake()
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.7f;
            _cc.radius = 0.3f;
            _cc.center = new Vector3(0f, 0.85f, 0f);
            _cc.stepOffset = 0.35f;
            _model = new GameObject("Model").transform;
            _model.SetParent(transform, false);
            var look = new CharacterLook { Name = BuddyBrain.BuddyName, Title = "낚시 친구", Female = true };
            var character = CharacterFactory.Create(look, _model);
            if (character != null) _anim = character.GetComponentInChildren<CharacterAnimator>();
            else CharacterFactory.Placeholder(_model, look.Name);
            _nameTag = CharacterFactory.AddNameTag(transform, look.Name, look.Title);
        }

        void Start()
        {
            var playerCc = Game.Player != null ? Game.Player.GetComponent<CharacterController>() : null;
            if (playerCc != null) Physics.IgnoreCollision(_cc, playerCc);
            if (BuddyBrain.Instance != null) BuddyBrain.Instance.Said += OnSaid;
            TeleportNearPlayer();
        }

        void OnDestroy()
        {
            if (BuddyBrain.Instance != null) BuddyBrain.Instance.Said -= OnSaid;
            if (Instance == this) Instance = null;
        }

        void OnSaid(string text, string motion)
        {
            if (_anim != null && _visible && !string.IsNullOrEmpty(motion) && CharacterAnimator.Has(motion)) _anim.PlayOnce(motion, 1f);
        }

        Vector3 FollowPoint()
        {
            var p = Game.Player.transform;
            var fwd = Quaternion.Euler(0f, Game.Player.Yaw, 0f);
            return p.position + fwd * new Vector3(FollowSide, 0f, -FollowBack);
        }

        void SetVisible(bool visible)
        {
            if (_visible == visible) return;
            _visible = visible;
            _model.gameObject.SetActive(visible);
            _nameTag.gameObject.SetActive(visible);
            _cc.enabled = visible;
            if (visible) TeleportNearPlayer();
        }

        /// <summary>플레이어 뒤 옆 → 반대쪽 → 바로 뒤 → 플레이어 자리 순으로 걸을 수 있는 곳에 세운다.</summary>
        public void TeleportNearPlayer()
        {
            if (Game.Player == null) return;
            var p = Game.Player.transform.position;
            var rot = Quaternion.Euler(0f, Game.Player.Yaw, 0f);
            Vector3[] offsets = { new Vector3(FollowSide, 0f, -FollowBack), new Vector3(-FollowSide, 0f, -FollowBack), new Vector3(0f, 0f, -1.4f), new Vector3(0.6f, 0f, 0f) };
            var spot = p;
            foreach (var o in offsets)
            {
                var c = p + rot * o;
                if (FieldMap.IsWalkable(c)) { spot = c; break; }
            }
            // 땅 높이: 위에서 아래로 쏜다 (다리·데크 위도)
            if (Physics.Raycast(spot + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~(1 << 2), QueryTriggerInteraction.Ignore)) spot.y = hit.point.y;
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = spot;
            _model.rotation = Quaternion.Euler(0f, Game.Player.Yaw, 0f);
            _cc.enabled = was;
            _vy = 0f;
            _stuckFor = 0f;
            _lastPos = spot;
        }

        void Update()
        {
            if (Game.Player == null) return;
            SetVisible(Game.Mode == GameMode.Field && !Game.Player.OnBoat);
            if (!_visible) return;

            var target = FollowPoint();
            var to = target - transform.position;
            float dy = to.y;
            to.y = 0f;
            float dist = to.magnitude;
            var toPlayer = Game.Player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude > TeleportDistance || Mathf.Abs(dy) > 5f) { TeleportNearPlayer(); return; }

            bool fishing = Game.Fishing != null && Game.Fishing.State != FishingState.Idle;
            // 낚시 중이면 덜 따라붙는다 (플레이어는 서 있고, 카메라 앞을 가리지 않게)
            float stopAt = fishing ? 1.2f : 0.45f;
            var motion = Vector3.zero;
            if (dist > stopAt)
            {
                float speed = Mathf.Clamp((dist - stopAt) * 2.2f, 0f, dist > 6f ? RunSpeed : WalkSpeed);
                motion = to / dist * speed;
                var next = transform.position + motion * Time.deltaTime;
                if (!FieldMap.IsWalkable(next))
                {
                    var nx = transform.position + new Vector3(motion.x, 0f, 0f) * Time.deltaTime;
                    var nz = transform.position + new Vector3(0f, 0f, motion.z) * Time.deltaTime;
                    if (FieldMap.IsWalkable(nx)) motion = new Vector3(motion.x, 0f, 0f);
                    else if (FieldMap.IsWalkable(nz)) motion = new Vector3(0f, 0f, motion.z);
                    else motion = Vector3.zero;
                }
            }

            if (_cc.isGrounded && _vy < 0f) _vy = -1f;
            _vy -= 20f * Time.deltaTime;
            var move = motion;
            move.y = _vy;
            _cc.Move(move * Time.deltaTime);

            // 막혀서 못 오면 (난간·벽 뒤) 잠시 뒤 옆으로 옮긴다
            var moved = transform.position - _lastPos;
            moved.y = 0f;
            _lastPos = transform.position;
            float speedNow = moved.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
            _stuckFor = dist > 3f && speedNow < 0.3f ? _stuckFor + Time.deltaTime : 0f;
            if (_stuckFor > 1.5f) { TeleportNearPlayer(); return; }

            // 바라보기: 움직이면 가는 쪽, 서 있으면 플레이어가 보는 쪽(낚시 중이면 물)이나 플레이어
            Vector3 face = speedNow > 0.3f ? motion : fishing ? Quaternion.Euler(0f, Game.Player.Yaw, 0f) * Vector3.forward : toPlayer;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f)
                _model.rotation = Quaternion.Slerp(_model.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8f);

            if (_anim != null && !_anim.PlayingOnce)
            {
                if (speedNow < 0.25f) _anim.SetLoop(CharacterAnimator.Idle);
                else if (speedNow < 3.6f) _anim.SetLoop(CharacterAnimator.Walk, Mathf.Clamp(speedNow / CharacterAnimator.WalkClipSpeed, 0.5f, 1.8f));
                else _anim.SetLoop(CharacterAnimator.Jog, Mathf.Clamp(speedNow / CharacterAnimator.JogClipSpeed, 0.7f, 1.5f));
            }
            if (_nameTag != null && Game.CameraRig != null)
                _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - Game.CameraRig.transform.position);
        }
    }
}
