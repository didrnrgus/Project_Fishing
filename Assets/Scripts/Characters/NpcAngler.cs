using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
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
        TelescopicRod _rodModel;
        Role _role;
        float _nextCast;

        /// <summary>true면 다시 던지지 않고 낚싯대만 들고 서 있다 (대기실).</summary>
        public bool Idle;

        public static NpcAngler Spawn(Transform parent, Vector3 localPos, float yaw, CharacterLook look, Role role, bool showNameTag = true)
        {
            var root = new GameObject("NPC_" + look.Name).transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var npc = root.gameObject.AddComponent<NpcAngler>();
            npc.Build(look, role, showNameTag);
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

        void Update()
        {
            if (_anim != null)
            {
                _anim.SetLoop(_role switch
                {
                    Role.Angler => CharacterAnimator.HoldRod,
                    Role.Talker => CharacterAnimator.Talk,
                    _ => CharacterAnimator.Sit
                });
                if (_role == Role.Angler && !Idle && Time.time > _nextCast)
                {
                    _nextCast = Time.time + Random.Range(20f, 45f);
                    _anim.PlayOnce(CharacterAnimator.Cast, 1.6f, 0.75f);
                }
            }

            if (_nameTag != null && Game.CameraRig != null)
            {
                var cam = Game.CameraRig.transform.position;
                bool near = (cam - _nameTag.position).sqrMagnitude < 45f * 45f;
                _nameTag.gameObject.SetActive(near);
                if (near) _nameTag.rotation = Quaternion.LookRotation(_nameTag.position - cam);
            }
        }

        /// <summary>대기실처럼 낚싯대를 접어 들고 있게 한다 (빗자루 길이).</summary>
        public void CollapseRod()
        {
            if (_rodModel != null) _rodModel.SetExtension(0f);
        }

        void LateUpdate()
        {
            if (_rod != null && _hand != null) _rod.position = _hand.position;
            // 줄 무게로 초릿대가 살짝 숙여지고, 바람에 조금 흔들린다
            if (_rodModel != null)
                _rodModel.Drive(6f + Mathf.PerlinNoise(Time.time * 0.7f, transform.position.x) * 6f, transform.forward - Vector3.up, 0f, Time.deltaTime);
        }
    }
}
