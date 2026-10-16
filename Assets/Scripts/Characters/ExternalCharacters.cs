using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 받은 휴머노이드 캐릭터(<see cref="CharacterLibrary"/>)를 게임 캐릭터로 만든다. CharacterFactory.Create와 같은 약속:
    /// 반환값 루트에 Animator + CharacterAnimator, 발이 원점, 게임 정면(+Z)을 본다.
    /// - 모든 동작 클립(UAL·Mixamo·Kevin·MixamoExtra)은 휴머노이드라 그대로 입는다. 클립은 −Z를 보게 맞춰 두었으므로 모델을 180° 돌린다.
    /// - 프리팹의 데모 스크립트(버튼을 띄우는 IdleChanger, 표정 FaceUpdate, 오른손 IK 등)는 떼고, 머리카락·치마 흔들림(SpringBone)과
    ///   눈 깜빡임(AutoBlink)만 남긴다. 프리팹의 AnimatorController도 떼고 Playables(CharacterAnimator)로만 움직인다.
    /// - "ghost"는 X Bot 마네킹에 반투명 귀신 셰이더(Fishing/Ghost).
    /// </summary>
    public static class ExternalCharacters
    {
        static readonly HashSet<string> KeepScripts = new HashSet<string> { "SpringBone", "SpringManager", "SpringCollider", "AutoBlink" };

        /// <summary>id에 맞는 키(m)와 성별(대기·걷기 클립 남/녀).</summary>
        static (float height, bool female) Spec(string id) => id switch
        {
            "unitychan" => (1.55f, true),
            "zombie" => (1.78f, false),
            "ghost" => (1.68f, true),
            _ => (1.75f, false)
        };

        public static bool Available(string id) => CharacterLibrary.Get(id) != null;

        public static GameObject Create(string id, Transform parent, string name = null)
        {
            var prefab = CharacterLibrary.Get(id);
            if (prefab == null) return null;
            var (height, female) = Spec(id);

            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Character_" + (name ?? id);
            go.SetActive(false); // 스크립트 Awake 전에 정리
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (!KeepScripts.Contains(mb.GetType().Name)) Object.DestroyImmediate(mb);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var l in go.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            foreach (var cam in go.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(cam.gameObject);

            var anim = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = null;
            anim.applyRootMotion = false;
            // Animator가 자식에 있으면 그 자식을 기준으로 (CharacterAnimator는 Animator와 같은 물체에)
            var root = anim.gameObject;

            // 키 맞추기: 정면·크기 1에서 렌더러 높이를 잰다
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var b = Bounds(go);
            float s = b.size.y > 0.1f ? height / b.size.y : 1f;
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = new Vector3(0f, -b.min.y * s, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // 클립이 −Z를 보게 맞춰져 있다 → 게임 정면(+Z)

            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = id == "ghost" ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (id == "ghost") ApplyGhost(go);
            if (id == "zombie") Darken(go, new Color(0.82f, 0.86f, 0.8f)); // 밤에 하얗게 뜨지 않게 살짝 칙칙하게

            go.SetActive(true);
            var ca = root.GetComponent<CharacterAnimator>() ?? root.AddComponent<CharacterAnimator>();
            ca.Female = female;
            return go;
        }

        static void ApplyGhost(GameObject go)
        {
            var shader = Shader.Find("Fishing/Ghost");
            if (shader == null) return;
            var m = new Material(shader) { name = "Ghost" };
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        static void Darken(GameObject go, Color tint)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;
                foreach (var m in mats)
                    if (m != null && m.HasProperty("_Color")) m.color *= tint;
                r.materials = mats;
            }
        }

        /// <summary>
        /// 키 재기: 비활성 상태라 렌더러 bounds는 믿을 수 없어서, 스킨드 메쉬 뼈(바인드 자세)의 가장 높은·낮은 위치로 잰다.
        /// Mixamo는 HeadTop_End 뼈가 정수리라 거의 정확하다. 원점 기준 로컬 bounds (y만 쓴다).
        /// </summary>
        static Bounds Bounds(GameObject go)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var bone in smr.bones)
                {
                    if (bone == null) continue;
                    float y = bone.position.y - go.transform.position.y;
                    lo = Mathf.Min(lo, y);
                    hi = Mathf.Max(hi, y);
                }
            if (lo > hi) return new Bounds(Vector3.up * 0.9f, new Vector3(0.5f, 1.8f, 0.5f));
            hi += (hi - lo) * 0.04f; // 정수리 뼈가 없는 모델: 머리 뼈 위로 조금
            return new Bounds(new Vector3(0f, (lo + hi) * 0.5f, 0f), new Vector3(0.5f, hi - lo, 0.5f));
        }
    }
}
