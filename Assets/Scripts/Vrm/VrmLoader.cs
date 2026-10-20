using System;
using System.Threading.Tasks;
using UniGLTF;
using UniGLTF.Extensions.VRMC_vrm;
using UniVRM10;
using UnityEngine;

namespace Fishing.Vrm
{
    /// <summary>VRM 메타 "아바타 사용 허락".</summary>
    public enum VrmPermission { OnlyAuthor, OnlySeparatelyLicensed, Everyone }

    public sealed class VrmMeta
    {
        public string Title, Author;
        public Texture2D Thumbnail;
        public VrmPermission Permission = VrmPermission.Everyone;
        public bool CreditRequired;
    }

    /// <summary>
    /// UniVRM(MIT)을 감싼 얇은 층. UniVRM 일부 어셈블리(UniGLTF.Utils)가 자동 참조되지 않아 게임 코드(Assembly-CSharp)에서 바로 부를 수 없어서
    /// 이 어셈블리(Fishing.Vrm)만 UniVRM을 참조하고, 게임 코드는 GameObject·VrmMeta만 주고받는다 (Characters/VrmCharacters).
    /// </summary>
    public static class VrmLoader
    {
        /// <summary>
        /// .vrm(0.x·1.0)을 읽는다. 컨트롤 리그 없이(휴머노이드 Animator가 뼈를 바로 움직인다) → 우리 동작 클립·팔 IK가 그대로 붙는다.
        /// 반환한 물체는 켜진 채라 부르는 쪽이 같은 프레임에 끄거나 옮긴다.
        /// </summary>
        public static async Task<(GameObject root, VrmMeta meta)> LoadAsync(string path)
        {
            var inst = await Vrm10.LoadPathAsync(path, canLoadVrm0X: true, controlRigGenerationOption: ControlRigGenerationOption.None);
            if (inst == null) throw new Exception("VRM을 읽지 못했습니다");
            var meta = new VrmMeta();
            var m = inst.Vrm != null ? inst.Vrm.Meta : null;
            if (m != null)
            {
                meta.Title = string.IsNullOrWhiteSpace(m.Name) ? null : m.Name.Trim();
                meta.Author = m.Authors != null && m.Authors.Count > 0 ? string.Join(", ", m.Authors) : null;
                meta.Thumbnail = m.Thumbnail;
                meta.Permission = m.AvatarPermission switch
                {
                    AvatarPermissionType.onlyAuthor => VrmPermission.OnlyAuthor,
                    AvatarPermissionType.onlySeparatelyLicensedPerson => VrmPermission.OnlySeparatelyLicensed,
                    _ => VrmPermission.Everyone,
                };
                meta.CreditRequired = m.CreditNotation == CreditNotationType.required;
            }
            return (inst.gameObject, meta);
        }

        /// <summary>
        /// 꺼진 원본을 복제한 직후(아직 꺼진 채) 부른다. RuntimeGltfInstance를 떼서 UniVRM이 "씬에 놓인 프리팹"처럼 다루게 한다
        /// (초기 자세 = 원본의 T 자세, 텍스처·메쉬는 원본이 가진다). 눈 깜빡임을 붙인다.
        /// </summary>
        public static void PrepareClone(GameObject clone)
        {
            var gltf = clone.GetComponent<RuntimeGltfInstance>();
            if (gltf != null) UnityEngine.Object.DestroyImmediate(gltf);
            if (clone.GetComponent<Vrm10Instance>() != null) clone.AddComponent<VrmBlink>();
        }
    }

    /// <summary>VRM 눈 깜빡임 (2~6초마다, 0.15초).</summary>
    public class VrmBlink : MonoBehaviour
    {
        Vrm10Instance _vrm;
        float _next, _t = -1f;

        void Start()
        {
            _vrm = GetComponent<Vrm10Instance>();
            _next = Time.time + UnityEngine.Random.Range(1f, 4f);
        }

        void Update()
        {
            if (_vrm == null) return;
            if (_t < 0f && Time.time >= _next) _t = 0f;
            if (_t < 0f) return;
            _t += Time.deltaTime;
            float w = 1f - Mathf.Abs(_t / 0.075f - 1f);
            _vrm.Runtime.Expression.SetWeight(ExpressionKey.Blink, Mathf.Clamp01(w));
            if (_t >= 0.15f)
            {
                _vrm.Runtime.Expression.SetWeight(ExpressionKey.Blink, 0f);
                _t = -1f;
                _next = Time.time + UnityEngine.Random.Range(2f, 6f);
            }
        }
    }
}
