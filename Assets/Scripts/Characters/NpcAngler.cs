using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 배경 NPC. 원작 필드에는 다른 유저들이 줄지어 낚시하고 있었는데, 솔로판에서는 NPC가 그 분위기를 낸다.
    ///  - Angler: 낚싯대를 들고 서 있다가 가끔 다시 던진다 (두 팔 IK, 몸은 UAL 대기)
    ///  - Talker: 낚시점 주인처럼 말하는 동작
    /// </summary>
    public class NpcAngler : MonoBehaviour
    {
        /// <summary>Stand: 맨손으로 서 있기 (대기실의 내 캐릭터).</summary>
        public enum Role { Angler, Talker, Sitter, Stand }

        CharacterAnimator _anim;
        Transform _hand, _rod, _nameTag;
        RodGrip _grip, _gripL;
        Transform _upperArmL, _lowerArmL, _handL;
        Transform _upperArmR, _lowerArmR;
        float _armScale = 1f;
        /// <summary>다시 던지기 시작한 시각 (-1 = 던지는 중 아님). 낚싯대 각도를 CastPitch 곡선대로 움직이고 오른손이 따라간다.</summary>
        float _castStart = -1f;
        /// <summary>릴을 감는 동안 (그 시각까지). 왼손이 릴 손잡이를 돌린다.</summary>
        float _reelUntil, _reelAt = -1f;
        float _leftWeight, _crankWeight;
        TelescopicRod _rodModel;
        // 낚싯줄과 물에 뜬 찌 (앞이 트인 물이 있을 때만)
        LineRenderer _line;
        Transform _float;
        Vector3 _floatPos;
        float _floatPhase;
        Role _role;
        float _nextCast;

        /// <summary>true면 다시 던지지 않고 낚싯대만 들고 서 있다 (대기실).</summary>
        public bool Idle;

        /// <summary>말을 걸 수 있는 NPC 목록 (이름표가 있는 NPC).</summary>
        public static readonly System.Collections.Generic.List<NpcAngler> Talkable = new System.Collections.Generic.List<NpcAngler>();
        /// <summary>대화 종류: shop(낚시점), union(조합 연락관), angler(낚시꾼).</summary>
        public string NpcId = "angler";
        public CharacterLook Look { get; private set; }
        Transform _faceTarget;
        /// <summary>몸을 돌리는 중 (-1 왼쪽, 1 오른쪽, 0 아님): 제자리 돌기 클립을 튼다.</summary>
        int _turning;
        float _nextRally;
        Quaternion _homeRot;

        public static NpcAngler Spawn(Transform parent, Vector3 localPos, float yaw, CharacterLook look, Role role, bool showNameTag = true)
        {
            var root = new GameObject("NPC_" + look.Name).transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var npc = root.gameObject.AddComponent<NpcAngler>();
            // 사람 크기 몸: 플레이어가 뚫고 지나가지 않고, 찌가 사람을 뚫고 날아가지 않게
            var body = root.gameObject.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.9f, 0f);
            body.height = 1.8f;
            body.radius = 0.32f;
            npc.Look = look;
            npc._homeRot = root.localRotation;
            npc.Build(look, role, showNameTag);
            if (showNameTag) Talkable.Add(npc);
            return npc;
        }

        void Build(CharacterLook look, Role role, bool showNameTag)
        {
            _role = role;
            var character = CharacterFactory.Create(look, transform);
            if (character != null)
            {
                _anim = character.GetComponentInChildren<CharacterAnimator>();
                var an = character.GetComponentInChildren<Animator>();
                if (an != null && an.isHuman)
                {
                    _hand = an.GetBoneTransform(HumanBodyBones.RightHand);
                    _upperArmR = an.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    _lowerArmR = an.GetBoneTransform(HumanBodyBones.RightLowerArm);
                    _upperArmL = an.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    _lowerArmL = an.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                    _handL = an.GetBoneTransform(HumanBodyBones.LeftHand);
                    _armScale = RodArm.ArmScale(_upperArmR, _lowerArmR, _hand);
                }
                if (role == Role.Angler)
                {
                    _grip = RodGrip.Find(character.transform);
                    _gripL = RodGrip.Find(character.transform, true);
                }
                // 앉아 있는 NPC(크루저 선장 등)는 엉덩이 밑에 아이스박스
                if (role == Role.Sitter) { var seat = SitSeat.Attach(character.transform); if (seat != null) seat.Active = true; }
            }
            else CharacterFactory.Placeholder(transform, look.Name);

            if (role == Role.Angler)
            {
                _rod = new GameObject("Rod").transform;
                _rod.SetParent(transform, false);
                _rod.localPosition = new Vector3(0.3f, 1.05f, 0.25f);
                _rod.localRotation = Quaternion.Euler(-RestPitch, 0f, 0f);
                // NPC마다 장비가 다르게 보이도록 이름으로 고른다
                string[] rods = { "rod_basic", "rod_carbon", "rod_bamboo", "rod_heavy" };
                string[] reels = { "reel_basic", "reel_power", "reel_hispeed" };
                int h = Mathf.Abs(look.Name?.GetHashCode() ?? 0);
                var axis = new GameObject("RodAxis").transform;
                axis.SetParent(_rod, false);
                axis.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _rodModel = TelescopicRod.Create(axis, GearDatabase.GetGear(rods[h % rods.Length]), GearDatabase.GetGear(reels[h % reels.Length]));
            }
            if (showNameTag) _nameTag = CharacterFactory.AddNameTag(transform, look.Name, look.Title);
            _nextCast = Time.time + Random.Range(5f, 25f);
        }

        void Start()
        {
            if (_rodModel == null || _role != Role.Angler) return;
            // 바라보는 쪽 물 위 4~13m에 찌를 띄운다. 물이 아니거나 무언가에 막히면 더 가깝게, 끝내 없으면 줄을 안 단다
            for (float d = 13f; d >= 4f; d -= 1.5f)
            {
                var p = transform.position + transform.forward * d;
                p.y = FieldMap.WaterY;
                if (!FieldMap.IsWater(p) || Fishing.Angling.FishingController.CastBlocked(_rodModel.Tip.position, p, out _)) continue;
                _floatPos = p;
                _floatPhase = Random.Range(0f, 6f);
                _float = new GameObject("NpcFloat").transform;
                _float.SetParent(transform.parent, true);
                Mats.Prim(PrimitiveType.Sphere, "FloatTop", _float, new Vector3(0f, 0.06f, 0f), new Vector3(0.07f, 0.09f, 0.07f), Mats.Lit(new Color(0.95f, 0.25f, 0.15f), 0.6f), false);
                Mats.Prim(PrimitiveType.Sphere, "FloatBody", _float, Vector3.zero, new Vector3(0.09f, 0.06f, 0.09f), Mats.Lit(new Color(0.95f, 0.95f, 0.9f), 0.6f), false);
                _line = new GameObject("NpcLine").AddComponent<LineRenderer>();
                _line.transform.SetParent(transform, false);
                _line.positionCount = 12;
                _line.widthMultiplier = 0.008f;
                _line.sharedMaterial = Mats.Unlit(new Color(0.85f, 0.88f, 0.9f, 1f));
                _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _line.useWorldSpace = true;
                break;
            }
        }

        void UpdateLine()
        {
            if (_line == null) return;
            // 찌: 수면(물때 포함) 위에서 잔물결 따라 까딱, 가끔 살짝 잠긴다(입질)
            float bob = Mathf.Sin(Time.time * 1.7f + _floatPhase) * 0.025f - (Mathf.PerlinNoise(Time.time * 0.3f, _floatPhase) > 0.78f ? 0.05f : 0f);
            _float.position = new Vector3(_floatPos.x, FieldMap.WaterY + 0.02f + bob, _floatPos.z);
            var a = _rodModel.Tip.position;
            var b = _float.position + Vector3.up * 0.1f;
            float sag = Vector3.Distance(a, b) * 0.06f;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                _line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * sag);
            }
        }

        void OnDisable()
        {
            if (_float != null) Destroy(_float.gameObject);
        }

        void Update()
        {
            if (_anim != null)
            {
                // 몸을 크게 돌리는 중이면 발을 옮기며 돈다 (Motions Basic_Turn), 앉은 사람은 그대로
                if (_turning != 0 && _role != Role.Sitter)
                    _anim.SetLoop(_turning < 0 ? CharacterAnimator.TurnLeft : CharacterAnimator.TurnRight, 1.2f);
                else
                    _anim.SetLoop(_role switch
                    {
                        Role.Angler => CharacterAnimator.HoldRod,
                        Role.Talker => CharacterAnimator.Talk,
                        Role.Stand => CharacterAnimator.Idle,
                        _ => CharacterAnimator.Sit
                    });
                // 조합 연락관: 보고할 퀘스트가 있고 내가 가까이 오면 손을 흔들어 부른다 (Motions Wave_Call)
                if (_mark == Fishing.Core.QuestSystem.Mark.Ready && _faceTarget == null && Game.Player != null && Time.time > _nextRally &&
                    (Game.Player.transform.position - transform.position).sqrMagnitude < 15f * 15f)
                {
                    _nextRally = Time.time + Random.Range(9f, 14f);
                    _anim.PlayOnce(CharacterAnimator.Wave, 1f);
                }
                if (_role == Role.Angler && !Idle && Time.time > _nextCast)
                {
                    _nextCast = Time.time + Random.Range(20f, 45f);
                    // 낚싯대를 어깨 너머로 젖혔다가 앞으로 휘둘러 던진다 (오른손은 낚싯대 각도를 따라가는 IK, 내 캐릭터와 같은 손 자리 표).
                    // 몇 초 기다렸다가 왼손 IK로 릴 손잡이를 돌린다 (내 캐릭터 UpdateCrank와 같다)
                    _castStart = Time.time;
                    _reelAt = Time.time + CastTime + Random.Range(2f, 6f);
                }
                if (_reelAt > 0f && Time.time >= _reelAt)
                {
                    _reelAt = -1f;
                    _reelUntil = Time.time + Random.Range(3f, 5f);
                }
            }

            if (_nameTag != null && Game.CameraRig != null)
            {
                var cam = Game.CameraRig.transform.position;
                // 대화 중에는 이름표를 숨긴다 (대화창에 이름이 나온다)
                bool near = (cam - _nameTag.position).sqrMagnitude < 45f * 45f && _faceTarget == null;
                _nameTag.gameObject.SetActive(near);
                if (near) _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - cam);
                // 퀘스트 NPC(조합 연락관·낚시점 주인). 어부는 통발 표시(TrapFisherMarker)가 따로 있다
                if (near && NpcId != "fisher" && QuestKey != null) UpdateQuestMarker();
            }
        }

        TMPro.TextMeshPro _questMark;
        Fishing.Core.QuestSystem.Mark _mark;
        float _nextMarkCheck;

        /// <summary>이 NPC의 퀘스트 키 (QuestNpcs: union@맵 · shop@맵 · fisher@맵). 퀘스트 NPC가 아니면 null.</summary>
        public string QuestKey => QuestNpcs.KeyFor(NpcId, FieldMap.Id);

        /// <summary>
        /// 원작 NPC 머리 위 퀘스트 표시: 노란 ? = 완료할 퀘스트(보고하면 보상), 노란 ! = 받을 이야기 퀘스트,
        /// 하늘색 ! = 받을 일일반복·가이드·일반 퀘스트, 회색 ? = 진행 중. 이름표 위에 떠서 위아래로 살짝 흔들린다.
        /// </summary>
        void UpdateQuestMarker()
        {
            if (_questMark == null)
            {
                _questMark = Fishing.UI.UIFactory.WorldText(_nameTag, "", new Vector3(0f, 0.62f, 0f), Color.white, 5f, "QuestMark");
                _questMark.fontStyle = TMPro.FontStyles.Bold;
                _questMark.outlineWidth = 0.2f;
                _questMark.outlineColor = new Color32(30, 20, 0, 255);
            }
            if (Time.time >= _nextMarkCheck && Game.Quests != null)
            {
                _nextMarkCheck = Time.time + 0.4f;
                _mark = Game.Quests.MarkFor(QuestKey);
            }
            var (text, color) = Fishing.UI.QuestTalkPanel.MarkGlyph(_mark);
            bool pop = _mark == Fishing.Core.QuestSystem.Mark.Ready || _mark == Fishing.Core.QuestSystem.Mark.AvailableStory;
            _questMark.text = text;
            _questMark.color = color;
            _questMark.transform.localPosition = new Vector3(0f, 0.62f + (pop ? Mathf.Sin(Time.time * 4f) * 0.08f : 0f), 0f);
            _questMark.transform.localScale = Vector3.one * (pop ? 1.15f : 0.9f);
        }

        /// <summary>평소 낚싯대 각도 (RodArm 각도, 허리 앞에 비스듬히 든 대기).</summary>
        const float RestPitch = 40f;
        /// <summary>다시 던지기 전체 길이 (젖히기 1초 → 멈춤 → 휘두르기 0.25초 → 앞으로 뻗은 채 → 1초에 걸쳐 원래 각도).</summary>
        const float CastTime = 3.4f;
        /// <summary>왼손이 쥐는 자리: 릴 시트(오른손)에서 이만큼 뒤 — 오른손 바로 아래 (PlayerController.LeftGripBehind와 같다).</summary>
        const float LeftGripBehind = 0.12f;

        /// <summary>던지기 시작하고 t초 뒤 낚싯대 각도.</summary>
        static float CastPitch(float t)
        {
            if (t < 1.0f) return Mathf.Lerp(RestPitch, 155f, Mathf.SmoothStep(0f, 1f, t / 1.0f));
            if (t < 1.3f) return 155f;
            if (t < 1.55f) return Mathf.Lerp(155f, 2f, Mathf.SmoothStep(0f, 1f, (t - 1.3f) / 0.25f));
            if (t < 2.4f) return 2f;
            return Mathf.Lerp(2f, RestPitch, Mathf.SmoothStep(0f, 1f, (t - 2.4f) / (CastTime - 2.4f)));
        }

        /// <summary>오른손을 낚싯대 각도에 맞는 자리로 (낚싯대를 손에 붙이기 전에). 내 캐릭터 UpdateRodArmRight와 같은 표.</summary>
        void UpdateRightHand()
        {
            if (_rod == null) return;
            float pitch = RestPitch;
            if (_castStart >= 0f)
            {
                float t = Time.time - _castStart;
                if (t >= CastTime) _castStart = -1f;
                else pitch = CastPitch(t);
            }
            _rod.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
            // 한 번짜리 클립(손짓·박수) 중에는 팔을 클립에 맡긴다 (왼손 UpdateLeftHand와 같다)
            bool once = _anim != null && _anim.PlayingOnce;
            _rightWeight = Mathf.MoveTowards(_rightWeight, once ? 0f : 1f, Time.deltaTime * 4f);
            if (_rightWeight <= 0.001f || _upperArmR == null || _lowerArmR == null || _hand == null) return;
            float w = Mathf.SmoothStep(0f, 1f, _rightWeight);
            var want = _upperArmR.position + transform.rotation * (RodArm.GripOffset(pitch) * _armScale);
            ArmIK.Solve(_upperArmR, _lowerArmR, _hand, Vector3.Lerp(_hand.position, want, w), transform.rotation * RodArm.ElbowPole(pitch), w);
        }

        float _rightWeight = 1f;

        /// <summary>
        /// 왼손: 평소엔 손잡이 끝을 감싸 쥐고, 릴을 감을 땐 릴 손잡이(노브)를 돌린다. 한 번짜리 동작(손짓·박수) 중에는 클립 그대로.
        /// 낚싯대는 오른손(_grip)에 붙어 있다 — 원작·내 캐릭터와 같은 손.
        /// </summary>
        void UpdateLeftHand()
        {
            if (_rodModel == null || _upperArmL == null || _lowerArmL == null || _handL == null || _rod == null) return;
            bool casting = _anim != null && _anim.PlayingOnce;
            bool reeling = !casting && Time.time < _reelUntil;
            _leftWeight = Mathf.MoveTowards(_leftWeight, casting ? 0f : 1f, Time.deltaTime * 3f);
            _crankWeight = Mathf.MoveTowards(_crankWeight, reeling ? 1f : 0f, Time.deltaTime * 5f);
            if (reeling) _rodModel.SpinReel(2.2f, 0f, Time.deltaTime);
            if (_leftWeight <= 0.001f) return;

            var fwd = _rod.forward;
            var knob = _rodModel.HandleKnob;
            var butt = _rod.position - fwd * LeftGripBehind;
            var pole = transform.rotation * new Vector3(-0.7f, -0.7f, -0.1f);
            var target = knob != null ? Vector3.Lerp(butt, knob.position, _crankWeight) : butt;
            var axis = knob != null ? Vector3.Slerp(fwd, knob.up, _crankWeight) : fwd;
            float w = Mathf.SmoothStep(0f, 1f, _leftWeight);
            ArmIK.Solve(_upperArmL, _lowerArmL, _handL, Vector3.Lerp(_handL.position, target, w), pole, w);
            if (_gripL == null) return;
            // 손목이 아니라 손바닥 앞(쥐는 자리)이 손잡이에 오게
            // 손잡이를 쥘 땐 손바닥이 낚싯대(몸 오른쪽)를 보게 (내 캐릭터와 같다), 릴을 감을 땐 노브를 감싸기만
            var palm = transform.right;
            float roll = w * (1f - _crankWeight);
            _gripL.AlignTo(axis, w);
            _gripL.RollPalm(axis, palm, roll);
            ArmIK.Solve(_upperArmL, _lowerArmL, _handL, _handL.position + (target - _gripL.Grip) * w, pole, w);
            _gripL.AlignTo(axis, w);
            _gripL.RollPalm(axis, palm, roll);
            _gripL.Wrap(target, axis, _crankWeight > 0.5f ? 0.016f : 0.032f);
        }

        /// <summary>자동 촬영: reel = 지금부터 10초 릴을 감는다, cast = 지금 다시 던진다, wave = 손짓, clap = 박수.</summary>
        public void DebugAct(string act)
        {
            _nextCast = Time.time + 60f;
            switch (act)
            {
                case "reel": _reelUntil = Time.time + 10f; break;
                case "cast": _castStart = Time.time; break;
                case "wave": _anim?.PlayOnce(CharacterAnimator.Wave, 1f); break;
                case "clap": _anim?.PlayOnce(CharacterAnimator.Clap, 1f, 1.2f); break;
            }
        }

        void OnDestroy() => Talkable.Remove(this);

        /// <summary>
        /// 근처에서 누가 대물을 낚았을 때: 대화 NPC는 돌아보며 3초쯤 박수, 낚시꾼은 낚싯대를 들고 있으니 돌아보기만.
        /// </summary>
        public static void Applaud(Vector3 where, Transform who, float radius = 22f)
        {
            foreach (var npc in Talkable)
            {
                if (npc == null || (npc.transform.position - where).sqrMagnitude > radius * radius) continue;
                npc.StartCoroutine(npc.ApplaudRoutine(who));
            }
        }

        System.Collections.IEnumerator ApplaudRoutine(Transform who)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.8f));
            var before = _faceTarget;
            _faceTarget = who;
            if (_role == Role.Talker && _anim != null) _anim.PlayOnce(CharacterAnimator.Clap, 1f, 1.2f); // 2.5초 반복 클립 × 1.2 = 3초쯤
            yield return new WaitForSeconds(3.2f);
            if (_faceTarget == who) _faceTarget = before;
        }

        /// <summary>대화할 때 상대를 돌아본다. null이면 원래 방향으로.</summary>
        public void FaceTarget(Transform target) => _faceTarget = target;

        void LateUpdate()
        {
            var want = _homeRot;
            if (_faceTarget != null)
            {
                var d = _faceTarget.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f)
                    want = Quaternion.Inverse(transform.parent != null ? transform.parent.rotation : Quaternion.identity) * Quaternion.LookRotation(d);
            }
            float diff = Mathf.DeltaAngle(transform.localEulerAngles.y, want.eulerAngles.y);
            _turning = Mathf.Abs(diff) > 25f ? (diff < 0f ? -1 : 1) : (Mathf.Abs(diff) < 8f ? 0 : _turning);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, want, Time.deltaTime * 6f);

            UpdateRightHand();
            if (_rod != null && _grip != null)
            {
                _grip.AlignTo(_rod.forward);
                _rod.position = _grip.Grip;
                _grip.Wrap(_rod.position, _rod.forward);
            }
            else if (_rod != null && _hand != null) _rod.position = _hand.position;
            UpdateLeftHand();
            // 줄 무게로 초릿대가 살짝 숙여지고, 바람에 조금 흔들린다
            if (_rodModel != null)
                _rodModel.Drive(6f + Mathf.PerlinNoise(Time.time * 0.7f, transform.position.x) * 6f, transform.forward - Vector3.up, 0f, Time.deltaTime);
            UpdateLine();
        }
    }
}
