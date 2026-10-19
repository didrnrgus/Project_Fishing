using Fishing.Angling;
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

        /// <summary>자동 스크린샷용 이동 입력 (카메라 기준, 0이면 키보드). DebugRun이면 달리기.</summary>
        public Vector2 DebugMove;
        public bool DebugRun;

        public Transform RodTip => Rod != null ? Rod.Tip : transform;
        /// <summary>머리 뼈 (초상화 카메라가 비춘다).</summary>
        public Transform Head { get; private set; }
        /// <summary>배에 타고 있는지.</summary>
        public bool OnBoat => _mount != null;
        public TelescopicRod Rod { get; private set; }

        /// <summary>이번 프레임 낚싯대 휨 목표(도)와 줄 방향. FishingController가 매 프레임 넣는다.</summary>
        public float RodBendTarget;
        public Vector3 RodPullDir;
        public Transform Model { get; private set; }
        public Vector3 EyePos => transform.position + Vector3.up * 1.6f;
        public float Yaw => Model.eulerAngles.y;
        public bool MovedThisFrame { get; private set; }

        /// <summary>낚싯대 기울기(도). 0 = 수평, 90 = 수직, 90 넘으면 머리 뒤로 젖힘.</summary>
        public float RodPitch = 35f;
        /// <summary>낚싯대 좌우 기울기(도). 챔질할 때 마우스를 흔드는 대로 따라 흔들린다.</summary>
        public float RodYaw;
        /// <summary>낚싯대가 목표 각도를 따라가는 빠르기. 캐스팅 순간에는 크게 해서 '휙' 휘두른다.</summary>
        public float RodPitchSpeed = 10f;
        /// <summary>상체 기울기(도). +면 뒤로 젖힘(캐스팅 준비), -면 앞으로 숙임(던진 직후·챔질).</summary>
        public float BodyLean;

        CharacterController _cc;
        Transform _nameTag;
        CharacterAnimator _anim;
        Transform _hand;
        float _speedNow;
        Transform _rodPivot;
        float _vy;
        float _currentRodPitch = 35f;
        /// <summary>지금 실제로 보이는 낚싯대 각도 (RodPitch를 따라가는 중간값). 줄 처짐 계산에 쓴다.</summary>
        public float CurrentRodPitch => _currentRodPitch;
        float _currentRodYaw;
        float _currentLean;
        Transform _spine;
        float _bob;
        float _extendTarget = 1f;
        Transform _mount;
        bool _mountSeated;
        SitSeat _sitSeat;
        RodGrip _grip;
        float _airTime;
        const float JumpSpeed = 6.2f;
        float _stride;
        int _extendedSections;
        string _rodId, _reelId;

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
                var look = Game.Profile != null ? Game.Profile.BuildLook() : CharacterLook.Player;
                BuildCharacter(look);
                SetNameTag(look);
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

            // 뽑기대: 낚싯대 축(+Y)을 RodPivot의 앞(+Z)으로, 릴은 아래로
            var axis = new GameObject("RodAxis").transform;
            axis.SetParent(_rodPivot, false);
            axis.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var p = Game.Profile;
            Rod = TelescopicRod.Create(axis, p?.Rod, p?.Reel);
            _rodId = p?.Rod.Id;
            _reelId = p?.Reel.Id;

            // 초상화 카메라가 내 캐릭터만 비추도록 전용 레이어에 둔다
            foreach (var t in Model.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = Fishing.UI.PortraitCamera.Layer;
                if (t.name == "Head") Head = t;
            }
            if (Head == null) Head = Model;
            // 낚싯대는 초상화에 비치지 않게 (얼굴 앞을 가로지른다)
            foreach (var t in _rodPivot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
        }

        GameObject _character;

        void BuildCharacter(CharacterLook look)
        {
            if (_character != null) Destroy(_character);
            _character = CharacterFactory.Create(look, Model);
            _anim = _character.GetComponent<CharacterAnimator>();
            _sitSeat = SitSeat.Attach(_character.transform);
            _grip = RodGrip.Find(_character.transform);
            _gripL = RodGrip.Find(_character.transform, true);
            Head = null;
            foreach (var t in _character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "hand_r") _hand = t;
                if (t.name == "Head") Head = t;
                if (t.name == "spine_02") _spine = t;
                t.gameObject.layer = Fishing.UI.PortraitCamera.Layer;
            }
            if (Head == null) Head = Model;
            FindLeftArm();
        }

        // ---------------- 낚은 고기 들어 보이기 (원작 결과 화면 1:55~1:58) ----------------

        Transform _upperArmL, _lowerArmL, _handL;
        Transform _upperArmR, _lowerArmR, _handR;
        RodGrip _gripL;
        Transform _heldFish;
        /// <summary>고기 입(머리 끝)의 고기 로컬 위치. 그 자리를 주먹에 둔다.</summary>
        Vector3 _fishMouthLocal, _fishCenterLocal;
        /// <summary>손가락만 한 고기(빙어·피라미): 머리 위로 치켜들지 않고 가슴 앞에서 몸통을 옆으로 쥐어 보인다.</summary>
        bool _tinyFish;
        float _holdWeight, _holdTarget, _showWeight;
        Vector3 _holdWant;
        /// <summary>왼손을 올릴 자리 (월드). 없으면 왼쪽 어깨 앞 위.</summary>
        public Vector3? HoldPoint;
        /// <summary>
        /// 결과 화면에서 고기를 들어 보이는 자세 (원작 1:56~1:58): 왼팔을 머리 위로 비스듬히 뻗어 주먹으로 고기 입을 쥐고(몸통은 아래로 늘어진다),
        /// 오른손은 낚싯대를 몸 옆에 세워 든다.
        /// </summary>
        public bool ShowOff;

        void FindLeftArm()
        {
            _upperArmL = _lowerArmL = _handL = null;
            if (_character == null) return;
            var all = _character.GetComponentsInChildren<Transform>(true);
            Transform Bone(string n) => System.Array.Find(all, t => t.name == n);
            _upperArmL = Bone("upperarm_l");
            _lowerArmL = Bone("lowerarm_l");
            _handL = Bone("hand_l");
            _upperArmR = Bone("upperarm_r");
            _lowerArmR = Bone("lowerarm_r");
            _handR = Bone("hand_r");
            if (_upperArmL != null && _lowerArmL != null && _handL != null && _upperArmR != null && _lowerArmR != null && _handR != null) return;
            var an = _character.GetComponentInChildren<Animator>();
            if (an == null || !an.isHuman) return;
            _upperArmL = an.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _lowerArmL = an.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _handL = an.GetBoneTransform(HumanBodyBones.LeftHand);
            _upperArmR = an.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _lowerArmR = an.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _handR = an.GetBoneTransform(HumanBodyBones.RightHand);
        }

        /// <summary>
        /// 낚은 고기를 왼손에 쥐고 들어 보인다. 오른손은 그대로 낚싯대를 든다.
        /// 팔은 애니메이션 위에 2관절 IK로 덧입힌다 (LateUpdate). fish = null이면 내려놓는다.
        /// </summary>
        public void HoldFish(Transform fish, float lengthCm = 0f)
        {
            if (fish != null && fish != _heldFish)
            {
                // 입 자리: 머리를 위로 세웠을 때 가운데에서 몸길이 절반 위 (예전에는 고기 가운데를 손목 4cm 아래에 붙여 손등 뒤에 박혀 보였다).
                // 스킨드 메쉬 바운드는 여유가 붙어 실제보다 커서, 길이는 아는 값(cm)을 쓴다
                fish.rotation = FishHangRotation(0f);
                var b = new Bounds(fish.position, Vector3.zero);
                foreach (var r in fish.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                float len = lengthCm > 0f ? lengthCm / 100f : b.size.y;
                var inv = Quaternion.Inverse(fish.rotation);
                _fishMouthLocal = inv * (b.center + Vector3.up * len * 0.44f - fish.position);
                _fishCenterLocal = inv * (b.center - fish.position);
                _tinyFish = len < 0.2f;
            }
            _heldFish = fish;
            _holdTarget = fish != null ? 1f : 0f;
        }

        /// <summary>입을 위로 매단 고기: 머리(+Z)는 위, 옆구리(+X)는 캐릭터 앞(카메라 쪽). 살짝 흔들린다.</summary>
        Quaternion FishHangRotation(float sway) =>
            Quaternion.AngleAxis(sway, Model.forward) * Quaternion.LookRotation(Vector3.up, Model.right);

        public Transform HeldFish => _heldFish;

        /// <summary>왼손 자리 (IK를 풀기 전 손의 실제 위치). 손이 없으면 모델 앞.</summary>
        public Vector3 LeftHandPos => _handL != null ? _handL.position : Model.position + Vector3.up * 1.2f + Model.forward * 0.3f;

        void UpdateHold()
        {
            _holdWeight = Mathf.MoveTowards(_holdWeight, _holdTarget, Time.deltaTime * 2.5f);
            if (_holdWeight <= 0.001f || _upperArmL == null) return;
            var fwd = Model.forward;
            var left = -Model.right;
            var shoulder = _upperArmL.position;
            Vector3 want;
            if (ShowOff && _tinyFish)
            {
                // 작은 고기는 가슴 앞으로 손을 내밀어 손바닥에 올려 보인다
                want = shoulder + fwd * 0.4f + left * 0.02f + Vector3.up * 0.04f;
            }
            else if (ShowOff)
            {
                // 원작 1:57: 왼팔을 거의 펴서 머리 위 비스듬히 바깥으로, 주먹이 머리 꼭대기보다 높다
                float reach = (_lowerArmL.position - shoulder).magnitude + (_handL.position - _lowerArmL.position).magnitude;
                want = shoulder + (Vector3.up * 0.78f + left * 0.58f + fwd * 0.3f).normalized * reach * 0.96f;
            }
            else want = HoldPoint ?? shoulder + fwd * 0.32f + left * 0.28f + Vector3.up * 0.18f;
            // 자리가 바뀌면 (잡기 → 들어 보이기) 휙 튀지 않고 0.3초쯤에 걸쳐 옮긴다
            _holdWant = _holdWeight < 0.05f ? want : Vector3.Lerp(_holdWant, want, Time.deltaTime * 7f);
            var target = Vector3.Lerp(_handL.position, _holdWant, Mathf.SmoothStep(0f, 1f, _holdWeight));
            SolveTwoBone(_upperArmL, _lowerArmL, _handL, target);
            if (_heldFish == null) return;
            if (_tinyFish && _gripL != null)
            {
                // 손바닥을 위·카메라 쪽으로 펴고(손가락은 감지 않는다) 고기를 옆으로 눕혀 올린다. 머리는 검지 쪽, 옆구리는 카메라 쪽.
                // (주먹으로 쥐면 손가락만 한 고기는 주먹 속에 숨어 안 보였다)
                var palmUp = (Vector3.up * 0.55f + fwd * 0.85f).normalized;
                _gripL.AlignPalm(palmUp, _holdWeight);
                _gripL.Open(_holdWeight);
                var n = _gripL.PalmNormal;
                // 머리는 손가락 끝 쪽으로, 손바닥을 따라 눕힌다
                var across = Vector3.ProjectOnPlane(_gripL.FingerDir, n).normalized;
                var trot = Quaternion.LookRotation(across, Vector3.Cross(across, n));
                // 살짝 파닥인다
                trot = Quaternion.AngleAxis(Mathf.Sin(Time.time * 7f) * 4f, n) * trot;
                _heldFish.rotation = trot;
                _heldFish.position = _gripL.Palm + n * 0.05f - trot * _fishCenterLocal;
                return;
            }
            // 주먹을 세워(엄지 위) 고기 입을 쥔다. 몸통은 새끼손가락 쪽 아래로 늘어진다
            Vector3 mouth;
            if (_gripL != null)
            {
                _gripL.AlignTo(Vector3.up, _holdWeight);
                mouth = _gripL.Grip;
                _gripL.Wrap(mouth, _gripL.Across, 0.024f);
            }
            else mouth = _handL.position + Vector3.down * 0.04f;
            var rot = FishHangRotation(Mathf.Sin(Time.time * 2.6f) * 5f);
            _heldFish.rotation = rot;
            _heldFish.position = mouth - rot * _fishMouthLocal;
        }

        /// <summary>들어 보이는 동안 오른팔을 몸 옆으로 내려 낚싯대를 세워 든다 (낚싯대는 손에 붙으니 낚싯대보다 먼저).</summary>
        void UpdateRodArm()
        {
            _showWeight = Mathf.MoveTowards(_showWeight, ShowOff ? 1f : 0f, Time.deltaTime * 2.5f);
            if (_showWeight <= 0.001f || _upperArmR == null) return;
            var want = _upperArmR.position + Model.right * 0.2f - Vector3.up * 0.4f + Model.forward * 0.2f;
            SolveTwoBone(_upperArmR, _lowerArmR, _handR, Vector3.Lerp(_handR.position, want, Mathf.SmoothStep(0f, 1f, _showWeight)));
        }

        /// <summary>2관절 IK: 위팔·아래팔 길이는 그대로 두고 손이 target에 닿게 돌린다 (팔꿈치는 지금 굽은 면 그대로).</summary>
        static void SolveTwoBone(Transform a, Transform b, Transform c, Vector3 target)
        {
            var pa = a.position;
            var pb = b.position;
            var pc = c.position;
            float lab = (pb - pa).magnitude, lcb = (pc - pb).magnitude;
            float lat = Mathf.Clamp((target - pa).magnitude, 0.01f, lab + lcb - 0.002f);
            float acab0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((pc - pa).normalized, (pb - pa).normalized), -1f, 1f));
            float babc0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((pa - pb).normalized, (pc - pb).normalized), -1f, 1f));
            float acab1 = Mathf.Acos(Mathf.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2f * lab * lat), -1f, 1f));
            float babc1 = Mathf.Acos(Mathf.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2f * lab * lcb), -1f, 1f));
            var axis = Vector3.Cross(pc - pa, pb - pa);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(pc - pa, Vector3.up);
            axis.Normalize();
            a.rotation = Quaternion.AngleAxis((acab1 - acab0) * Mathf.Rad2Deg, axis) * a.rotation;
            b.rotation = Quaternion.AngleAxis((babc1 - babc0) * Mathf.Rad2Deg, axis) * b.rotation;
            a.rotation = Quaternion.FromToRotation(c.position - pa, target - pa) * a.rotation;
        }

        /// <summary>의상을 바꾸면 캐릭터를 다시 입힌다 (초상화도 따라 바뀐다).</summary>
        public void ApplyLook()
        {
            if (!CharacterFactory.Available || Game.Profile == null) return;
            var look = Game.Profile.BuildLook();
            BuildCharacter(look);
            SetNameTag(look);
        }

        string _tagTitle;

        void SetNameTag(CharacterLook look)
        {
            if (_nameTag != null) Destroy(_nameTag.gameObject);
            _nameTag = CharacterFactory.AddNameTag(transform, look.Name, look.Title);
            _tagTitle = look.Title;
        }

        void Start()
        {
            // 레벨 업·업적으로 칭호가 바뀌면 머리 위 이름표도 바꾼다 (HUD와 같은 칭호)
            if (Game.Profile != null)
                Game.Profile.Changed += () =>
                {
                    if (this == null || !CharacterFactory.Available || Game.Profile.CurrentTitle == _tagTitle) return;
                    SetNameTag(Game.Profile.BuildLook());
                };
        }

        // ---------------- 배 ----------------

        /// <summary>배의 자리(운전석/뱃머리)에 올라탄다. seated면 앉아서 배 방향을 본다.</summary>
        public void Mount(Transform point, bool seated)
        {
            _mount = point;
            _mountSeated = seated;
            _cc.enabled = false;
            _vy = 0f;
        }

        public void Dismount(Vector3 pos, float yaw)
        {
            _mount = null;
            Teleport(pos, yaw);
        }

        /// <summary>배가 움직인 뒤 자리로 옮긴다 (BoatController가 매 프레임 부른다).</summary>
        public void SnapToMount()
        {
            if (_mount == null) return;
            transform.position = _mount.position;
            if (_mountSeated) Model.rotation = Quaternion.Euler(0f, _mount.eulerAngles.y, 0f);
        }

        /// <summary>접어 둔 낚싯대를 끝 마디부터 하나씩 뽑아 펼친다 (낚시터에 들어왔을 때).</summary>
        public void ExtendRod()
        {
            Rod.SetExtension(0f);
            _extendedSections = 0;
            _extendTarget = 1f;
        }

        /// <summary>낚싯대를 접는다 (대기실로 나갈 때).</summary>
        public void CollapseRod()
        {
            _extendTarget = 0f;
            Rod.SetExtension(0f);
        }

        public bool RodExtended => Rod.Extension >= 0.999f;

        void UpdateRodGear()
        {
            var p = Game.Profile;
            if (p == null || (p.Rod.Id == _rodId && p.Reel.Id == _reelId)) return;
            _rodId = p.Rod.Id;
            _reelId = p.Reel.Id;
            Rod.Rebuild(p.Rod, p.Reel);
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

        /// <summary>캐스팅 동작: 어깨 너머(Windup 끝 자세)에서 앞으로 던지고 팔을 뻗은 채 내린다.</summary>
        public void PlayCast() => _anim?.PlayOnce(CharacterAnimator.Cast, 1.5f);

        /// <summary>챔질 동작 (낚싯대를 홱 들어 올리기).</summary>
        public void PlayHook() => _anim?.PlayOnce(CharacterAnimator.Hook, 2f);

        /// <summary>낚아 올렸을 때 (두 팔 들고 환호).</summary>
        /// <summary>랜딩 환호: 두 팔 흔들기, 가끔(40%) 깡충 뛰며 기뻐하기 (받은 Mixamo Joyful Jump).</summary>
        public void PlayCheer()
        {
            if (CharacterAnimator.Has(CharacterAnimator.JoyfulJump) && Random.value < 0.4f) _anim?.PlayOnce(CharacterAnimator.JoyfulJump, 1.1f);
            else _anim?.PlayOnce(CharacterAnimator.Cheer, 1.1f);
        }

        /// <summary>괴물·전설 어종을 낚았을 때: 펄쩍 뛰며 기뻐한다 (Mixamo Excited).</summary>
        public void PlayExcited() => _anim?.PlayOnce(CharacterAnimator.Excited, 1.2f, 0.7f);

        /// <summary>줄이 끊어졌을 때: 움찔 놀란다 (Mixamo Scared, 앞부분만).</summary>
        /// <summary>줄이 끊겼을 때: 움찔 놀라기, 가끔(35%) 발 구르며 화내기 (받은 Mixamo Angry).</summary>
        public void PlayScared()
        {
            if (CharacterAnimator.Has(CharacterAnimator.Angry) && Random.value < 0.35f) _anim?.PlayOnce(CharacterAnimator.Angry, 1.2f, 0.6f);
            else _anim?.PlayOnce(CharacterAnimator.Scared, 1.3f, 0.5f);
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            _cc.enabled = false;
            transform.position = pos;
            Model.rotation = Quaternion.Euler(0f, yaw, 0f);
            _cc.enabled = true;
        }

        public void FaceYaw(float yaw) => Model.rotation = Quaternion.Euler(0f, yaw, 0f);

        /// <summary>이름표 표시 여부 (NPC와 대화할 때 숨긴다).</summary>
        public bool NameTagVisible = true;

        public void SetVisible(bool visible)
        {
            foreach (var r in Model.GetComponentsInChildren<Renderer>()) r.enabled = visible;
        }

        float _titleCheckAt;

        void Update()
        {
            if (Time.time >= _titleCheckAt && Game.Profile != null && CharacterFactory.Available)
            {
                _titleCheckAt = Time.time + 1f;
                if (Game.Profile.CurrentTitle != _tagTitle) SetNameTag(Game.Profile.BuildLook());
            }
            MovedThisFrame = false;
            if (Game.Mode == GameMode.Lobby) return;
            var input = Vector2.zero;
            bool uiBlocking = Game.UI != null && Game.UI.BlockingPanelOpen;

            if (_mount != null)
            {
                // 배 위: 이동은 BoatController가 한다. WASD는 찌 회수 판정에만 쓴다
                MovedThisFrame = !uiBlocking && (Settings.Held(KeyAction.MoveForward) || Settings.Held(KeyAction.MoveBack) || Settings.Held(KeyAction.MoveLeft) || Settings.Held(KeyAction.MoveRight));
                _speedNow = 0f;
                _airTime = 0f;
                if (_anim != null) UpdateAnimation();
                if (_nameTag != null) _nameTag.gameObject.SetActive(NameTagVisible);
                if (_nameTag != null && NameTagVisible && Game.CameraRig != null)
                    _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - Game.CameraRig.transform.position);
                return;
            }
            if (!uiBlocking)
            {
                if (Settings.Held(KeyAction.MoveForward)) input.y += 1f;
                if (Settings.Held(KeyAction.MoveBack)) input.y -= 1f;
                if (Settings.Held(KeyAction.MoveRight)) input.x += 1f;
                if (Settings.Held(KeyAction.MoveLeft)) input.x -= 1f;
            }
            if (DebugMove != Vector2.zero) input = DebugMove;
            bool wantsMove = input.sqrMagnitude > 0.01f;
            MovedThisFrame = wantsMove;

            var motion = Vector3.zero;
            if (wantsMove && !MovementLocked)
            {
                float camYaw = Game.CameraRig != null ? Game.CameraRig.Yaw : 0f;
                var rot = Quaternion.Euler(0f, camYaw, 0f);
                var dir = rot * new Vector3(input.x, 0f, input.y).normalized;
                float speed = Settings.Held(KeyAction.Run) || DebugRun ? RunSpeed : WalkSpeed;
                motion = dir * speed;
                Model.rotation = Quaternion.Slerp(Model.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);

                // 걸을 수 없는 곳(강물)으로는 못 가게 축별로 미끄러뜨린다
                var next = transform.position + motion * Time.deltaTime;
                if (!FieldMap.IsWalkable(next))
                {
                    var nx = transform.position + new Vector3(motion.x, 0f, 0f) * Time.deltaTime;
                    var nz = transform.position + new Vector3(0f, 0f, motion.z) * Time.deltaTime;
                    if (FieldMap.IsWalkable(nx)) motion = new Vector3(motion.x, 0f, 0f);
                    else if (FieldMap.IsWalkable(nz)) motion = new Vector3(0f, 0f, motion.z);
                    else motion = Vector3.zero;
                }
                _bob += Time.deltaTime * speed * 2.2f;
            }

            if (_cc.isGrounded && _vy < 0f) _vy = -1f;
            // 점프 (원작: Space). 낚시 중이거나 창이 열려 있으면 안 된다
            bool fishingIdle = Game.Fishing == null || Game.Fishing.State == Angling.FishingState.Idle;
            if (Settings.Down(KeyAction.Jump) && _cc.isGrounded && !uiBlocking && !MovementLocked && fishingIdle)
            {
                _vy = JumpSpeed;
                _anim?.PlayOnce(CharacterAnimator.JumpStart, 1.4f, 0.6f);
            }
            _airTime = _cc.isGrounded ? 0f : _airTime + Time.deltaTime;
            _vy -= 20f * Time.deltaTime;
            motion.y = _vy;
            _cc.Move(motion * Time.deltaTime);

            _speedNow = new Vector2(motion.x, motion.z).magnitude;
            // 발소리: 애니메이션 한 걸음마다 (뛰기 클립 0.3초 × 4m/s = 1.2m, 질주 0.26초 × 6m/s = 1.56m)
            if (_cc.isGrounded && _speedNow > 0.5f)
            {
                bool running = _speedNow > WalkSpeed + 0.5f;
                _stride += _speedNow * Time.deltaTime;
                if (_stride >= (running ? 1.56f : 1.2f))
                {
                    _stride = 0f;
                    if (Game.Sound != null) Game.Sound.Footstep(transform.position, running);
                }
            }
            if (_anim == null)
            {
                // 프리미티브 캐릭터는 걸을 때 살짝 들썩이는 것으로 대신한다
                float bobOffset = _speedNow > 1f ? Mathf.Abs(Mathf.Sin(_bob)) * 0.06f : 0f;
                Model.localPosition = new Vector3(0f, bobOffset, 0f);
            }
            else UpdateAnimation();

            if (_nameTag != null) _nameTag.gameObject.SetActive(NameTagVisible);
            if (_nameTag != null && NameTagVisible && Game.CameraRig != null)
                _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - Game.CameraRig.transform.position);
        }

        /// <summary>
        /// 낚시 중 반복 자세: 던지려고 당기는 중 = 어깨 너머로 들기, 그 밖 = 낚싯대 들고 대기.
        /// 찌·루어를 감을 때도 대기 자세 그대로 두고 왼손만 릴 손잡이를 돌린다 (<see cref="UpdateCrank"/>).
        /// Mixamo 릴 감기 클립은 오른손이 원을 그려서, 낚싯대를 쥔 오른손이 낚싯대째 빙빙 돌았다.
        /// </summary>
        static string FishingPose()
        {
            var f = Game.Fishing;
            if (f.State == Angling.FishingState.Charging) return CharacterAnimator.Windup;
            return CharacterAnimator.HoldRod;
        }

        float _crankWeight;

        /// <summary>찌·루어를 감는 동안 릴 손잡이를 돌리고 왼손을 손잡이 노브에 붙여 쥔다 (낚싯대를 손에 붙인 뒤에).</summary>
        void UpdateCrank()
        {
            bool reeling = Game.Fishing != null && Game.Fishing.ReelingIn && _heldFish == null;
            _crankWeight = Mathf.MoveTowards(_crankWeight, reeling ? 1f : 0f, Time.deltaTime * 5f);
            if (reeling) Rod.SpinReel(2.2f, 0f, Time.deltaTime);
            var knob = Rod.HandleKnob;
            if (_crankWeight <= 0.001f || knob == null || _upperArmL == null) return;
            float w = Mathf.SmoothStep(0f, 1f, _crankWeight);
            SolveTwoBone(_upperArmL, _lowerArmL, _handL, Vector3.Lerp(_handL.position, knob.position, w));
            if (_gripL == null) return;
            // 손목이 아니라 손바닥 앞(쥐는 자리)이 노브에 오게: 손을 노브 축에 맞춰 돌리고, 남은 차이만큼 한 번 더 뻗는다
            var axis = knob.up; // 캡슐 노브의 길이 방향
            _gripL.AlignTo(axis, w);
            SolveTwoBone(_upperArmL, _lowerArmL, _handL, _handL.position + (knob.position - _gripL.Grip) * w);
            _gripL.AlignTo(axis, w);
            _gripL.Wrap(knob.position, axis, 0.016f);
        }

        void UpdateAnimation()
        {
            bool fishing = Game.Fishing != null && Game.Fishing.State != Angling.FishingState.Idle;
            if (_sitSeat != null) _sitSeat.Active = _mount != null && _mountSeated;
            if (_mount != null)
            {
                _anim.SetLoop(_mountSeated ? CharacterAnimator.Drive : fishing ? FishingPose() : CharacterAnimator.Idle);
                return;
            }
            if (_airTime > 0.15f)
            {
                _anim.SetLoop(CharacterAnimator.JumpLoop);
                return;
            }
            bool running = _speedNow > WalkSpeed + 0.5f;
            // 속도에 맞는 클립 (Human Basic Motions 원래 속도: 걷기 2, 뛰기 4, 질주 6m/s). 걷기 키(4.5m/s)도 뛰는 걸음
            if (running) _anim.SetLoop(CharacterAnimator.Sprint, _speedNow / CharacterAnimator.SprintClipSpeed);
            else if (_speedNow > 2.6f) _anim.SetLoop(CharacterAnimator.Jog, _speedNow / CharacterAnimator.JogClipSpeed);
            else if (_speedNow > 0.5f) _anim.SetLoop(CharacterAnimator.Walk, Mathf.Max(0.6f, _speedNow / CharacterAnimator.WalkClipSpeed));
            else if (fishing) _anim.SetLoop(FishingPose(), Game.Fishing.State == Angling.FishingState.Charging ? 1.5f : 1f);
            else _anim.SetLoop(CharacterAnimator.Idle);
        }

        void LateUpdate()
        {
            // 애니메이션이 손을 움직인 뒤에 낚싯대를 손에 붙인다
            // 상체를 젖히거나 숙인다 (애니메이션 위에 덧입힌다). 손 위치가 바뀌므로 낚싯대보다 먼저
            _currentLean = Mathf.Lerp(_currentLean, BodyLean, Time.deltaTime * 12f);
            if (_spine != null && Mathf.Abs(_currentLean) > 0.05f)
                _spine.rotation = Quaternion.AngleAxis(-_currentLean, Model.right) * _spine.rotation;
            UpdateRodArm();
            _currentRodPitch = Mathf.Lerp(_currentRodPitch, RodPitch, Time.deltaTime * RodPitchSpeed);
            _currentRodYaw = Mathf.Lerp(_currentRodYaw, RodYaw, Time.deltaTime * 14f);
            _rodPivot.localRotation = Quaternion.Euler(0f, _currentRodYaw, 0f) * Quaternion.Euler(-_currentRodPitch, 0f, 0f);
            // 손을 낚싯대 방향에 맞춰 돌리고, 릴 시트를 손바닥 앞에 두고 손가락으로 감싸 쥔다 (RodGrip). 뼈가 없는 단순 모델은 손목에
            if (_grip != null)
            {
                _grip.AlignTo(_rodPivot.forward);
                _rodPivot.position = _grip.Grip;
                _grip.Wrap(_rodPivot.position, _rodPivot.forward);
            }
            else if (_hand != null) _rodPivot.position = _hand.position;

            UpdateRodGear();
            // 펼치기: 1.2초에 걸쳐 끝 마디부터. 마디가 하나 빠질 때마다 '착'
            if (Rod.Extension < _extendTarget)
            {
                Rod.SetExtension(Mathf.MoveTowards(Rod.Extension, _extendTarget, Time.deltaTime / 1.2f));
                int n = Rod.ExtendedSections;
                if (n > _extendedSections && Game.Sound != null) Game.Sound.RodSnap(n);
                _extendedSections = n;
            }
            Rod.Drive(RodBendTarget, RodPullDir.sqrMagnitude > 0f ? RodPullDir : Model.forward, 0f, Time.deltaTime);
            RodBendTarget = 0f;
            UpdateCrank();
            UpdateHold();
        }
    }
}
