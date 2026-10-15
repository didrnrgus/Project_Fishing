using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 배경 NPC. 원작 필드에는 다른 유저들이 줄지어 낚시하고 있었는데, 솔로판에서는 NPC가 그 분위기를 낸다.
    ///  - Angler: 낚싯대를 들고 서 있다가 가끔 다시 던진다
    ///  - Talker: 낚시점 주인처럼 말하는 동작
    /// </summary>
    public class NpcAngler : MonoBehaviour
    {
        public enum Role { Angler, Talker, Sitter }

        CharacterAnimator _anim;
        Transform _hand, _rod, _nameTag;
        RodGrip _grip;
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
            if (CharacterFactory.Available)
            {
                var character = CharacterFactory.Create(look, transform);
                _anim = character.GetComponent<CharacterAnimator>();
                foreach (var t in character.GetComponentsInChildren<Transform>(true))
                    if (t.name == "hand_r") _hand = t;
                if (role == Role.Angler) _grip = RodGrip.Find(character.transform);
                // 앉아 있는 NPC(크루저 선장 등)는 엉덩이 밑에 아이스박스
                if (role == Role.Sitter) { var seat = SitSeat.Attach(character.transform); if (seat != null) seat.Active = true; }
            }
            else
            {
                Mats.Prim(PrimitiveType.Capsule, "Body", transform, Vector3.up * 0.9f, new Vector3(0.55f, 0.9f, 0.5f), Mats.Lit(look.Shirt), false);
            }

            if (role == Role.Angler)
            {
                _rod = new GameObject("Rod").transform;
                _rod.SetParent(transform, false);
                _rod.localPosition = new Vector3(0.3f, 1.05f, 0.25f);
                _rod.localRotation = Quaternion.Euler(-40f, 0f, 0f);
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
                // 몸을 크게 돌리는 중이면 발을 옮기며 돈다 (Human Basic Motions Turn), 앉은 사람은 그대로
                if (_turning != 0 && _role != Role.Sitter)
                    _anim.SetLoop(_turning < 0 ? CharacterAnimator.TurnLeft : CharacterAnimator.TurnRight, 1.2f);
                else
                    _anim.SetLoop(_role switch
                    {
                        Role.Angler => CharacterAnimator.HoldRod,
                        Role.Talker => CharacterAnimator.Talk,
                        _ => CharacterAnimator.Sit
                    });
                // 조합 연락관: 보고할 퀘스트가 있고 내가 가까이 오면 손짓해 부른다 (Mixamo Rallying)
                if (NpcId == "union" && Game.Quests != null && Game.Quests.Ready && _faceTarget == null && Game.Player != null && Time.time > _nextRally &&
                    (Game.Player.transform.position - transform.position).sqrMagnitude < 15f * 15f)
                {
                    _nextRally = Time.time + Random.Range(9f, 14f);
                    _anim.PlayOnce(CharacterAnimator.Rallying, 1f, 0.8f);
                }
                if (_role == Role.Angler && !Idle && Time.time > _nextCast)
                {
                    _nextCast = Time.time + Random.Range(20f, 45f);
                    // 들기 → 던지기 → 기다리기 → 릴 감기 → 처음 자세 (Mixamo 한 벌, 약 8.8초)
                    _anim.PlayOnce(CharacterAnimator.FishingSequence);
                }
            }

            if (_nameTag != null && Game.CameraRig != null)
            {
                var cam = Game.CameraRig.transform.position;
                // 대화 중에는 이름표를 숨긴다 (대화창에 이름이 나온다)
                bool near = (cam - _nameTag.position).sqrMagnitude < 45f * 45f && _faceTarget == null;
                _nameTag.gameObject.SetActive(near);
                if (near) _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - cam);
                if (near && NpcId == "union") UpdateQuestMarker();
            }
        }

        TMPro.TextMeshPro _questMark;

        /// <summary>
        /// 원작 NPC 퀘스트 표시: 노란 ? = 목표 달성(보고하면 보상), 회색 ? = 진행 중, 노란 ! = 받을 일일 의뢰가 남음.
        /// 이름표 위에 떠서 위아래로 살짝 흔들린다.
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
            var q = Game.Quests;
            bool ready = q != null && q.Ready;
            bool working = q != null && q.Current != null;
            bool daily = Game.Profile != null && Game.Profile.Data.daily.Exists(d => !d.done);
            _questMark.text = ready ? "?" : working ? "?" : daily ? "!" : "";
            _questMark.color = ready ? new Color(1f, 0.82f, 0.2f) : working ? new Color(0.7f, 0.72f, 0.75f) : new Color(1f, 0.82f, 0.2f);
            _questMark.transform.localPosition = new Vector3(0f, 0.62f + (ready ? Mathf.Sin(Time.time * 4f) * 0.08f : 0f), 0f);
            _questMark.transform.localScale = Vector3.one * (ready ? 1.15f : 0.9f);
        }

        /// <summary>대기실처럼 낚싯대를 접어 들고 있게 한다 (빗자루 길이).</summary>
        public void CollapseRod()
        {
            if (_rodModel != null) _rodModel.SetExtension(0f);
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
            if (_role == Role.Talker && _anim != null) _anim.PlayOnce(CharacterAnimator.Clap, 1f, 2.6f);
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

            if (_rod != null && _grip != null)
            {
                _rod.position = _grip.Palm;
                _grip.Wrap(_rod.position, _rod.forward);
            }
            else if (_rod != null && _hand != null) _rod.position = _hand.position;
            // 줄 무게로 초릿대가 살짝 숙여지고, 바람에 조금 흔들린다
            if (_rodModel != null)
                _rodModel.Drive(6f + Mathf.PerlinNoise(Time.time * 0.7f, transform.position.x) * 6f, transform.forward - Vector3.up, 0f, Time.deltaTime);
            UpdateLine();
        }
    }
}
