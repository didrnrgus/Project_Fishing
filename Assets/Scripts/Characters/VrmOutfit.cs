using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 다른 VRM의 옷을 입힌다 (16 문서 3장). VRoid 모델은 몸 메쉬(Body) 하나에 피부·상의·하의·신발이 재질별 조각(서브메쉬)으로 들어 있고
    /// 뼈 이름(J_Bip_*)이 같다 → 입은 사람의 옷 조각(…_CLOTH)은 숨기고, 옷 주인의 옷 조각만 떼어 입은 사람 뼈에 이름으로 다시 붙인다.
    /// - 정점은 각자 뼈 기준 자리를 그대로 지키므로(바인드포즈는 옷 주인 것) 키·팔 길이가 달라도 소매·바짓단이 손목·발목을 따라간다.
    /// - 입은 사람에게 없는 뼈(후드·끈 같은 흔들림 뼈 J_Sec_*)는 옷 주인 뼈대에서 가장 가까운 있는 조상으로 붙인다 (그 부분은 흔들리지 않는다).
    /// 주인공 기본 캐릭터에 쓴다: characters.json의 playerFemaleOutfit / playerMaleOutfit.
    /// </summary>
    public static class VrmOutfit
    {
        static bool IsCloth(Material m) => m != null && m.name.Contains("_CLOTH");

        public static void Apply(GameObject wearer, GameObject donorTemplate)
        {
            var donorBody = FindBody(donorTemplate);
            var body = FindBody(wearer);
            if (donorBody == null || body == null) { Debug.LogWarning("[Dev] 옷 입히기: 몸 메쉬를 못 찾음"); return; }

            // 1) 입은 사람의 옷 조각을 숨긴다 (메쉬 복사본에서 그 조각의 삼각형을 비운다)
            var mesh = Object.Instantiate(body.sharedMesh);
            mesh.name = body.sharedMesh.name + "_noCloth";
            var mats = body.sharedMaterials;
            for (int i = 0; i < mesh.subMeshCount && i < mats.Length; i++)
                if (IsCloth(mats[i])) mesh.SetTriangles(System.Array.Empty<int>(), i);
            body.sharedMesh = mesh;

            // 2) 옷 주인의 옷 조각만 남긴 메쉬
            var src = donorBody.sharedMesh;
            var dmats = donorBody.sharedMaterials;
            var keep = new List<int>();
            for (int i = 0; i < src.subMeshCount && i < dmats.Length; i++)
                if (IsCloth(dmats[i])) keep.Add(i);
            if (keep.Count == 0) return;
            var cloth = Object.Instantiate(src);
            cloth.name = src.name + "_outfit";
            cloth.subMeshCount = keep.Count;
            var clothMats = new Material[keep.Count];
            for (int k = 0; k < keep.Count; k++)
            {
                cloth.SetTriangles(src.GetTriangles(keep[k]), k);
                clothMats[k] = dmats[keep[k]];
            }

            // 3) 뼈를 이름으로 잇는다. 없는 뼈는 옷 주인 뼈대의 조상 중 입은 사람에게 있는 것으로, 바인드포즈도 그 뼈 기준으로 다시
            var wearerBones = new Dictionary<string, Transform>();
            foreach (var t in wearer.GetComponentsInChildren<Transform>(true)) wearerBones[t.name] = t;
            var dBones = donorBody.bones;
            var bones = new Transform[dBones.Length];
            var binds = new Matrix4x4[dBones.Length];
            var meshToWorld = donorBody.transform.localToWorldMatrix;
            for (int i = 0; i < dBones.Length; i++)
            {
                var d = dBones[i];
                while (d != null && !wearerBones.ContainsKey(d.name)) d = d.parent;
                if (d == null) { bones[i] = body.rootBone; binds[i] = src.bindposes[i]; continue; }
                bones[i] = wearerBones[d.name];
                // 옷 주인 원본(선반, T 자세 그대로)에서 그 뼈의 바인드포즈
                binds[i] = d == dBones[i] ? src.bindposes[i] : d.worldToLocalMatrix * meshToWorld;
            }
            cloth.bindposes = binds;

            var go = new GameObject("Outfit");
            go.transform.SetParent(body.transform.parent, false);
            go.transform.localPosition = body.transform.localPosition;
            go.transform.localRotation = body.transform.localRotation;
            go.transform.localScale = body.transform.localScale;
            go.layer = body.gameObject.layer;
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = cloth;
            smr.sharedMaterials = clothMats;
            smr.bones = bones;
            smr.rootBone = donorBody.rootBone != null && wearerBones.TryGetValue(donorBody.rootBone.name, out var rb) ? rb : body.rootBone;
            smr.updateWhenOffscreen = true;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        /// <summary>옷 조각(…_CLOTH)이 있는 VRM인가 (VRoid가 아니면 입혀도 그대로라 옷 후보에서 뺀다).</summary>
        public static bool HasCloth(GameObject root) => root != null && FindBody(root) != null;

        /// <summary>옷 조각이 든 몸 메쉬 (VRoid: Body / Body.baked).</summary>
        static SkinnedMeshRenderer FindBody(GameObject root)
        {
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                foreach (var m in smr.sharedMaterials)
                    if (IsCloth(m)) return smr;
            }
            return null;
        }
    }
}
