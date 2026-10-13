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
            Mats.Prim(PrimitiveType.Sphere, "Nose", Model, new Vector3(0f, 1.63f, 0.2f), Vector3.one * 0.07f, skin, false);

            // 오른손에 든 낚싯대. 피벗을 기준으로 위아래로 기울인다.
            _rodPivot = new GameObject("RodPivot").transform;
            _rodPivot.SetParent(Model, false);
            _rodPivot.localPosition = new Vector3(0.3f, 1.05f, 0.25f);
            Mats.Prim(PrimitiveType.Sphere, "Hand", _rodPivot, Vector3.zero, Vector3.one * 0.13f, skin, false);

            var rodMat = Mats.Lit(new Color(0.75f, 0.15f, 0.2f), 0.6f);
            const float rodLen = 2.8f;
            Mats.Prim(PrimitiveType.Cylinder, "Rod", _rodPivot, new Vector3(0f, 0f, rodLen * 0.5f - 0.3f),
                new Vector3(0.035f, rodLen * 0.5f, 0.035f), rodMat, false, Quaternion.Euler(90f, 0f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, "Reel", _rodPivot, new Vector3(0f, -0.08f, 0.1f), new Vector3(0.12f, 0.03f, 0.12f),
                Mats.Lit(new Color(0.2f, 0.2f, 0.22f), 0.7f, 0.6f), false, Quaternion.Euler(0f, 0f, 90f));

            // 머리 위 이름표 (원작처럼 캐릭터 이름이 떠 있다)
            var tag = new GameObject("NameTag");
            tag.transform.SetParent(transform, false);
            tag.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            var tm = tag.AddComponent<TextMesh>();
            tm.text = "강태공";
            tm.font = Fishing.UI.UIFactory.Font;
            tm.fontSize = 48;
            tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tag.GetComponent<MeshRenderer>().sharedMaterial = Fishing.UI.UIFactory.Font.material;
            _nameTag = tag.transform;

            RodTip = new GameObject("RodTip").transform;
            RodTip.SetParent(_rodPivot, false);
            RodTip.localPosition = new Vector3(0f, 0f, rodLen - 0.3f);
        }

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

            // 걸을 때 살짝 들썩이는 모션
            float bobOffset = motion.sqrMagnitude > 1f ? Mathf.Abs(Mathf.Sin(_bob)) * 0.06f : 0f;
            Model.localPosition = new Vector3(0f, bobOffset, 0f);

            if (_nameTag != null && Game.CameraRig != null)
                _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - Game.CameraRig.transform.position);

            _currentRodPitch = Mathf.Lerp(_currentRodPitch, RodPitch, Time.deltaTime * 10f);
            _rodPivot.localRotation = Quaternion.Euler(-_currentRodPitch, 0f, 0f);
        }
    }
}
