using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 캐릭터 한 명. 생김새는 VRM 모델 하나로 정해진다 (옷·모자를 따로 입히지 않는다 — 의상은 능력치만, 몸에 붙여 보이는 건 낚싯대뿐).
    /// Vrm이 비어 있으면 <see cref="CharacterRoster"/>가 이름·성별로 고른다 (characters.json).
    /// </summary>
    public class CharacterLook
    {
        public string Name;
        public string Title;
        /// <summary>여자: 대기·걷기 동작을 여자 클립(_F)으로, NPC면 여자 모델 목록에서 고른다.</summary>
        public bool Female;
        /// <summary>VRM id ("builtin/파일.vrm", "user/파일.vrm"). 비우면 CharacterRoster가 고른다.</summary>
        public string Vrm;

        public static CharacterLook Player => new CharacterLook { Name = "강태공", Title = "초보 조사" };
    }

    /// <summary>
    /// 캐릭터를 만든다: VRM(VrmCharacters)을 복제하고 동작(CharacterAnimator)을 붙인다.
    /// 처음 쓰는 파일은 그 자리에서 읽는다 (맵을 지을 때 = 로딩 화면 뒤). 파일이 없거나 깨졌으면 null → 부르는 쪽이 대신 서 있을 모양을 만든다.
    /// </summary>
    public static class CharacterFactory
    {
        /// <summary>NPC 키를 이름으로 조금씩 다르게 (같은 모델이 여럿이어도 덜 똑같아 보이게).</summary>
        const float NpcScaleJitter = 0.06f;

        /// <summary>캐릭터를 만든다. 반환값의 루트에 Animator와 CharacterAnimator가 붙어 있다. 발이 원점, 게임 정면(+Z)을 본다. 실패하면 null.</summary>
        public static GameObject Create(CharacterLook look, Transform parent, bool npc = true)
        {
            var id = !string.IsNullOrEmpty(look.Vrm) ? look.Vrm : CharacterRoster.ForNpc(look.Name, look.Female);
            if (id == null || !VrmCharacters.LoadNow(id)) return null;
            var go = VrmCharacters.Create(id, parent, look.Name, look.Female);
            if (go != null && npc && string.IsNullOrEmpty(look.Vrm))
            {
                float j = (CharacterRoster.StableHash(look.Name ?? "") % 1000u) / 999f * 2f - 1f;
                go.transform.localScale *= 1f + j * NpcScaleJitter;
                // 목록에서 고른 NPC는 머리·옷 색도 이름으로 바꾼다 (characters.json에서 이름으로 정한 사람은 그대로)
                if (!CharacterRoster.IsAssigned(look.Name)) NpcLooks.Apply(go, look.Name);
            }
            return go;
        }

        /// <summary>VRM을 못 읽었을 때 대신 서 있는 마네킹 (회색 캡슐, 키 1.7m).</summary>
        public static GameObject Placeholder(Transform parent, string name)
        {
            var go = new GameObject("Placeholder_" + name);
            go.transform.SetParent(parent, false);
            var mat = Mats.Lit(new Color(0.62f, 0.64f, 0.68f));
            Mats.Prim(PrimitiveType.Capsule, "Body", go.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.5f, 0.85f, 0.4f), mat, false);
            Mats.Prim(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0f, 1.58f, 0f), Vector3.one * 0.26f, mat, false);
            return go;
        }

        /// <summary>머리 위 이름표(+칭호). 카메라를 향하게는 사용하는 쪽에서 돌린다.</summary>
        public static Transform AddNameTag(Transform parent, string name, string title, float height = 2.15f)
        {
            var tag = new GameObject("NameTag").transform;
            tag.SetParent(parent, false);
            tag.localPosition = new Vector3(0f, height, 0f);
            if (!string.IsNullOrEmpty(title))
                UIFactory.WorldText(tag, title, new Vector3(0f, 0.2f, 0f), new Color(0.55f, 0.85f, 1f), 1.5f);
            UIFactory.WorldText(tag, name, Vector3.zero, Color.white, 2f);
            tag.gameObject.AddComponent<NameTagFade>();
            return tag;
        }
    }

    /// <summary>
    /// 이름표 마무리 (주인 스크립트의 LateUpdate 뒤에 돈다, 켜고 끄기는 주인이 정한다):
    /// - 화면과 나란하게 세운다. 주인들은 카메라 '위치'를 보게 돌려서 화면 가장자리의 이름표가 비스듬히 기울어 커 보였다.
    /// - 카메라가 가까우면 흐리게 → 숨긴다. 3인칭 카메라가 NPC 옆을 지나면 머리 위 글자가 화면 위쪽을 덮었다(낚시점 앞 항만 노동자 등).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class NameTagFade : MonoBehaviour
    {
        const float Hide = 3f, Full = 6.5f;
        TMPro.TMP_Text[] _texts;
        float _alpha = -1f;

        void Awake() => _texts = GetComponentsInChildren<TMPro.TMP_Text>(true);

        void LateUpdate()
        {
            if (Fishing.Core.Game.CameraRig == null) return;
            var cam = Fishing.Core.Game.CameraRig.transform;
            transform.rotation = cam.rotation;
            float d = Vector3.Distance(cam.position, transform.position);
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Hide, Full, d));
            if (Mathf.Abs(a - _alpha) < 0.01f) return;
            _alpha = a;
            foreach (var t in _texts)
                if (t != null) t.alpha = a;
        }
    }
}
