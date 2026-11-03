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
    ///  - 배에 타면 같이 탄다. 대기실에서는 숨는다 (대화는 그대로 된다).
    ///  - 길 안내(NPC·낚시 포인트·배 띄울 곳)와 손으로 가리키기 (아래 "길 안내" 이하).
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
            SetVisible(Game.Mode == GameMode.Field);
            if (!_visible) { StopLead(); EndRide(); return; }
            var pp = Game.Player.transform.position;
            var vel = pp - _playerLast;
            vel.y = 0f;
            _playerVel = Vector3.Lerp(_playerVel, vel / Mathf.Max(Time.deltaTime, 1e-4f), Time.deltaTime * 6f);
            _playerSpeed = _playerVel.magnitude;
            _playerLast = pp;
            // 배: 같이 탄다
            if (Game.Player.OnBoat && Game.Boat != null && Game.Boat.BowPoint != null) { RideUpdate(); return; }
            EndRide();
            if (_goal != null && LeadUpdate()) return;

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
                motion = Slide(to / dist * speed);
            }

            float speedNow = Step(motion);

            // 막혀서 못 오면 (난간·벽 뒤) 잠시 뒤 옆으로 옮긴다
            _stuckFor = dist > 3f && speedNow < 0.3f ? _stuckFor + Time.deltaTime : 0f;
            if (_stuckFor > 1.5f) { TeleportNearPlayer(); return; }

            // 바라보기: 움직이면 가는 쪽, 서 있으면 플레이어가 보는 쪽(낚시 중이면 물)이나 플레이어
            Animate(speedNow, speedNow > 0.3f ? motion : fishing ? Quaternion.Euler(0f, Game.Player.Yaw, 0f) * Vector3.forward : toPlayer);
        }

        /// <summary>물·못 가는 곳으로 가려 하면 한 축만 남겨 미끄러지듯 비켜 간다.</summary>
        Vector3 Slide(Vector3 motion)
        {
            var next = transform.position + motion * Time.deltaTime;
            if (FieldMap.IsWalkable(next)) return motion;
            var nx = transform.position + new Vector3(motion.x, 0f, 0f) * Time.deltaTime;
            var nz = transform.position + new Vector3(0f, 0f, motion.z) * Time.deltaTime;
            if (FieldMap.IsWalkable(nx)) return new Vector3(motion.x, 0f, 0f);
            if (FieldMap.IsWalkable(nz)) return new Vector3(0f, 0f, motion.z);
            return Vector3.zero;
        }

        /// <summary>중력과 함께 움직이고, 이번 프레임에 실제로 간 수평 속도를 돌려준다.</summary>
        float Step(Vector3 motion)
        {
            if (_cc.isGrounded && _vy < 0f) _vy = -1f;
            _vy -= 20f * Time.deltaTime;
            var move = motion;
            move.y = _vy;
            _cc.Move(move * Time.deltaTime);
            float speedNow = Flat(transform.position - _lastPos) / Mathf.Max(Time.deltaTime, 1e-4f);
            _lastPos = transform.position;
            return speedNow;
        }

        void Animate(float speedNow, Vector3 face)
        {
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

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        // ───────────── 길 안내 ─────────────
        // 플레이어가 "데려다줘"·"퀘스트 어디서 받아?"·"붕어 어디서 잡아?" 하면(BuddyBrain.TryGuide) 그곳으로 앞장서 걷는다.
        //  - 처지면 서서 돌아보며 손으로 가리키고 부른다. 딴 데로 가면 "아니야, 저기야!" 하고 가리킨다.
        //  - NPC: 닿으면 "여기야!", 플레이어가 옆에 오면 다시 따라다닌다.
        //  - 낚시 포인트: 물가에 닿으면 던질 물을 가리킨다. 배를 타야 하는 포인트면 배 띄울 물가로 데려가고, 플레이어가 배에 타면 같이 타서 포인트를 가리킨다.

        /// <summary>안내할 곳.</summary>
        public sealed class Goal
        {
            /// <summary>따라갈 대상 (NPC). 없으면 <see cref="Spot"/>.</summary>
            public Transform Target;
            /// <summary>걸어갈 물가 자리.</summary>
            public Vector3 Spot;
            /// <summary>닿으면 손으로 가리킬 곳 (낚시 포인트 물). 없으면 null.</summary>
            public Vector3? PointAt;
            /// <summary>여기서 배를 타야 한다: 닿은 뒤 플레이어가 배에 타면 같이 타고 <see cref="PointAt"/>까지 가리킨다.</summary>
            public bool Boat;
            public string Name;
            /// <summary>닿았을 때 (BuddyBrain이 말한다).</summary>
            public System.Action Arrived;
            /// <summary>배로 포인트에 닿았을 때.</summary>
            public System.Action Reached;
            public Vector3 Position => Target != null ? Target.position : Spot;
        }

        const float ArriveRadius = 2.3f, WaitDistance = 8f, BoatReach = 14f;
        Goal _goal;
        System.Collections.Generic.List<Vector3> _path;
        int _pathIndex, _repaths, _calls;
        bool _arrived;
        float _leadStarted, _nextCall, _arrivedAt, _playerSpeed, _wrongFor, _nextWrong;
        Vector3 _playerLast, _playerVel;

        /// <summary>길 안내 중 (가는 중이거나 닿아서 플레이어를 기다리는 중).</summary>
        public bool Leading => _goal != null;
        /// <summary>안내하던 곳에 닿아 플레이어를 기다리는 중.</summary>
        public bool LeadArrived => _goal != null && _arrived;

        public bool Lead(Goal goal)
        {
            if (goal == null || !_visible) return false;
            _goal = goal;
            _arrived = false;
            _leadStarted = Time.time;
            _nextCall = Time.time + 5f;
            _nextWrong = Time.time + 4f;
            _repaths = _calls = 0;
            _stuckFor = _wrongFor = 0f;
            float t0 = Time.realtimeSinceStartup;
            if (!_riding) Repath();
            Debug.Log($"[Buddy] 길 안내 → {goal.Name}{(goal.Boat ? " (배)" : "")}: {(_path != null ? $"꺾는 점 {_path.Count}개" : "길 없음 (곧장)")} ({(Time.realtimeSinceStartup - t0) * 1000f:0}ms)");
            // 먼저 그쪽을 가리킨다 ("저쪽이야")
            PointTo(goal.PointAt ?? goal.Position, 2.2f);
            return true;
        }

        public void StopLead()
        {
            _goal = null;
            _path = null;
        }

        void Repath()
        {
            _path = BuddyPath.Find(transform.position, _goal.Position, ArriveRadius);
            _pathIndex = 0;
        }

        /// <summary>플레이어가 목적지와 반대로 1.2초 넘게 가면 true (한 번 알리면 6초 쉰다).</summary>
        bool WrongWay(Vector3 goal)
        {
            var toGoal = goal - Game.Player.transform.position;
            toGoal.y = 0f;
            bool wrong = _playerVel.magnitude > 1.5f && toGoal.magnitude > 6f && Vector3.Dot(_playerVel.normalized, toGoal.normalized) < -0.3f;
            _wrongFor = wrong ? _wrongFor + Time.deltaTime : 0f;
            if (_wrongFor < 1.2f || Time.time < _nextWrong) return false;
            _wrongFor = 0f;
            _nextWrong = Time.time + 6f;
            return true;
        }

        /// <summary>안내 한 프레임 (걸어서). 안내가 끝났으면 false (따라다니기로 돌아간다).</summary>
        bool LeadUpdate()
        {
            if (_goal.Target != null && !_goal.Target.gameObject.activeInHierarchy) { StopLead(); return false; }
            var goalPos = _goal.Position;
            var player = Game.Player.transform.position;
            float toPlayer = Flat(player - transform.position);
            float playerToGoal = Flat(goalPos - player);
            bool fishing = Game.Fishing != null && Game.Fishing.State != FishingState.Idle;
            // 끝: 플레이어가 그곳에 왔다(배를 타야 하는 곳은 배에 탈 때까지 기다린다) / 낚시를 시작했다 / 딴 데로 멀리 갔다 / 너무 오래 걸렸다
            if ((_arrived && !_goal.Boat && playerToGoal < 3.5f) || fishing || toPlayer > 45f || Time.time - _leadStarted > 180f || (_arrived && Time.time - _arrivedAt > 40f))
            {
                StopLead();
                return false;
            }
            if (!_arrived && (Flat(goalPos - transform.position) < ArriveRadius || playerToGoal < 2.5f))
            {
                _arrived = true;
                _arrivedAt = Time.time;
                _goal.Arrived?.Invoke();
                PointTo(_goal.PointAt ?? goalPos, 3f);
            }

            var motion = Vector3.zero;
            Vector3 face;
            if (_arrived) face = player - transform.position;
            else if (WrongWay(goalPos))
            {
                face = goalPos - transform.position;
                BuddyBrain.Instance?.WrongWay(_goal.Name);
                PointTo(goalPos, 2.5f);
            }
            else if (toPlayer > WaitDistance)
            {
                // 처졌다: 서서 돌아보고, 가끔 가는 쪽을 가리키며 부른다 (세 번까지)
                face = player - transform.position;
                if (Time.time >= _nextCall && _calls < 3)
                {
                    _nextCall = Time.time + 8f;
                    _calls++;
                    BuddyBrain.Instance?.CallPlayer(_goal.Name);
                    PointTo(goalPos, 2.5f);
                }
            }
            else
            {
                var to = NextWaypoint(goalPos) - transform.position;
                to.y = 0f;
                // 플레이어가 뛰면 같이 뛰고, 조금 처지면 늦춘다
                float speed = _playerSpeed > 5.5f ? RunSpeed - 1f : toPlayer > 5f ? 2.8f : WalkSpeed - 0.5f;
                motion = to.sqrMagnitude > 0.01f ? Slide(to.normalized * speed) : Vector3.zero;
                face = motion.sqrMagnitude > 0.01f ? motion : goalPos - transform.position;
            }
            if (Pointing) face = _pointAt - transform.position; // 가리키는 동안은 그쪽으로 몸을 돌린다

            float speedNow = Step(motion);
            // 막혔다: 길을 다시 찾고, 그래도 막히면 앞의 꺾는 점으로 옮긴다
            _stuckFor = motion.sqrMagnitude > 0.5f && speedNow < 0.3f ? _stuckFor + Time.deltaTime : 0f;
            if (_stuckFor > 1.2f)
            {
                _stuckFor = 0f;
                if (_repaths++ < 2) Repath();
                else if (_path != null && _pathIndex < _path.Count) TeleportTo(_path[Mathf.Min(_pathIndex + 1, _path.Count - 1)]);
            }
            Animate(speedNow, face);
            return true;
        }

        Vector3 NextWaypoint(Vector3 goalPos)
        {
            if (_path == null) return goalPos;
            while (_pathIndex < _path.Count && Flat(_path[_pathIndex] - transform.position) < 0.8f) _pathIndex++;
            return _pathIndex < _path.Count ? _path[_pathIndex] : goalPos;
        }

        void TeleportTo(Vector3 spot)
        {
            bool was = _cc.enabled;
            _cc.enabled = false;
            transform.position = spot;
            _cc.enabled = was;
            _vy = 0f;
            _lastPos = spot;
        }

        // ───────────── 배 같이 타기 ─────────────
        // 플레이어가 배에 타면 윤슬도 탄다: 플레이어가 운전석에 앉아 몰면 뱃머리에 서고, 뱃머리에 서서 낚시하면 운전석에 앉는다.
        // 배로 포인트를 안내하는 중이면 뱃머리에서 포인트를 가리키고, 엉뚱한 데로 몰면 "아니야, 저기야!".

        bool _riding;
        Transform _ridePoint;
        bool _rideSeated;
        SitSeat _sitSeat;

        void RideUpdate()
        {
            var boat = Game.Boat;
            bool playerSeated = boat.Seated;
            var point = playerSeated ? boat.BowPoint : boat.SeatPoint;
            if (!_riding)
            {
                _riding = true;
                _cc.enabled = false;
                _path = null;
                if (_goal != null && !_goal.Boat) StopLead(); // 걸어서 가던 안내는 배에 타면 끝
            }
            if (point != _ridePoint)
            {
                _ridePoint = point;
                _rideSeated = !playerSeated;
                if (_sitSeat == null && _anim != null) _sitSeat = SitSeat.Attach(_anim.transform);
            }
            if (_sitSeat != null) _sitSeat.Active = _rideSeated;

            var face = Quaternion.Euler(0f, point.eulerAngles.y, 0f) * Vector3.forward;
            if (_goal != null && _goal.Boat && _goal.PointAt.HasValue)
            {
                var target = _goal.PointAt.Value;
                float left = Flat(target - boat.Hull.position);
                if (left < BoatReach)
                {
                    _goal.Reached?.Invoke();
                    PointTo(target, 3f);
                    StopLead();
                }
                else if (WrongWay(target))
                {
                    BuddyBrain.Instance?.WrongWay(_goal.Name);
                    PointTo(target, 3f);
                }
                else if (Time.time >= _nextCall)
                {
                    // 가끔 가리켜 준다
                    _nextCall = Time.time + 9f;
                    PointTo(target, 2.5f);
                }
            }
            if (Pointing && !_rideSeated) face = _pointAt - transform.position;
            if (_anim != null && !_anim.PlayingOnce) _anim.SetLoop(_rideSeated ? CharacterAnimator.Sit : CharacterAnimator.Idle);
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f) _model.rotation = Quaternion.Slerp(_model.rotation, Quaternion.LookRotation(face), Time.deltaTime * 6f);
            if (_nameTag != null && Game.CameraRig != null)
                _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - Game.CameraRig.transform.position);
        }

        void EndRide()
        {
            if (!_riding) return;
            _riding = false;
            _ridePoint = null;
            if (_sitSeat != null) _sitSeat.Active = false;
            _cc.enabled = _visible;
            TeleportNearPlayer();
        }

        // ───────────── 손으로 가리키기 (IK) ─────────────
        // 오른팔을 곧게 펴 그쪽을 가리킨다: 애니메이션이 자세를 잡은 뒤(LateUpdate) 위팔·아래팔·손을 차례로 그 방향으로 돌린다. 0.25초에 걸쳐 들고 내린다.

        Animator _animator;
        Transform _upperArm, _lowerArm, _hand;
        Vector3 _pointAt;
        float _pointUntil, _pointWeight;

        bool Pointing => Time.time < _pointUntil;

        public void PointTo(Vector3 where, float seconds)
        {
            _pointAt = where;
            _pointUntil = Time.time + seconds;
        }

        void FindArm()
        {
            if (_animator != null || _anim == null) return;
            _animator = _anim.GetComponentInChildren<Animator>();
            if (_animator == null || !_animator.isHuman) return;
            _upperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _lowerArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        void LateUpdate()
        {
            if (_riding && _ridePoint != null)
            {
                transform.position = _ridePoint.position;
                _lastPos = transform.position;
            }
            FindArm();
            _pointWeight = Mathf.MoveTowards(_pointWeight, Pointing && _visible ? 1f : 0f, Time.deltaTime * 4f);
            if (_pointWeight <= 0f || _upperArm == null || _lowerArm == null || _hand == null) return;
            var dir = _pointAt - _upperArm.position;
            // 먼 곳은 조금 위로 (팔을 수평보다 살짝 들어 "저 멀리")
            dir.y = Mathf.Max(dir.y, new Vector2(dir.x, dir.z).magnitude * 0.12f);
            dir.Normalize();
            float w = Mathf.SmoothStep(0f, 1f, _pointWeight);
            Aim(_upperArm, _lowerArm, dir, w);
            Aim(_lowerArm, _hand, dir, w);
        }

        /// <summary>bone에서 child로 가는 방향을 dir 쪽으로 w만큼 돌린다.</summary>
        static void Aim(Transform bone, Transform child, Vector3 dir, float w)
        {
            var cur = child.position - bone.position;
            if (cur.sqrMagnitude < 1e-6f) return;
            bone.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, dir), w) * bone.rotation;
        }
    }
}
