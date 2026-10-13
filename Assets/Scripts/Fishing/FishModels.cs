using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// Quaternius Animated Fish Pack(CC0)의 Fish1~3으로 어종별 물고기를 만든다.
    /// 원본은 cm 단위(길이 약 1.7~3.3m로 읽힘)라 실제 길이에 맞춰 줄이고, 어종 색을 입힌다.
    /// 에셋이 없으면 캡슐로 대신한다.
    /// </summary>
    public static class FishModels
    {
        // 모델별 원본 길이(Z축, 임포트 단위)
        static readonly (string path, float rawLength)[] Models =
        {
            ("Fish/Fish1", 326.5f), // 몸통이 두툼한 고기: 잉어, 붕어, 메기 …
            ("Fish/Fish2", 215.7f), // 지느러미가 큰 고기: 쏘가리, 배스, 블루길 …
            ("Fish/Fish3", 167.6f), // 날씬한 고기: 피라미, 끄리, 강준치, 숭어, 뱀장어 …
        };

        public static int ModelIndex(FishSpecies s) => s.Id switch
        {
            "mandarin" or "bass" or "bluegill" or "nuchi" => 1,
            "zacco" or "kkeuri" or "skygager" or "mullet" or "eel" or "bagrid" => 2,
            _ => 0
        };

        public static Color BodyColor(FishSpecies s) => s.Id switch
        {
            "crucian" => new Color(0.62f, 0.56f, 0.32f),
            "carp" => new Color(0.7f, 0.52f, 0.25f),
            "goldkoi" => new Color(1f, 0.72f, 0.15f),
            "catfish" => new Color(0.32f, 0.3f, 0.26f),
            "mandarin" => new Color(0.72f, 0.62f, 0.3f),
            "snakehead" => new Color(0.3f, 0.3f, 0.24f),
            "eel" => new Color(0.36f, 0.38f, 0.28f),
            "bass" => new Color(0.45f, 0.55f, 0.32f),
            "bluegill" => new Color(0.4f, 0.5f, 0.55f),
            "nuchi" => new Color(0.7f, 0.68f, 0.6f),
            "bagrid" => new Color(0.75f, 0.6f, 0.3f),
            _ => new Color(0.75f, 0.78f, 0.82f)
        };

        public static GameObject Create(FishSpecies species, float lengthCm, Transform parent)
        {
            var (path, raw) = Models[ModelIndex(species)];
            var prefab = Resources.Load<GameObject>(path);
            GameObject go;
            if (prefab == null || species.IsJunk)
            {
                go = Mats.Prim(PrimitiveType.Capsule, "Fish", parent, Vector3.zero, new Vector3(0.1f, 0.15f, 0.1f),
                    Mats.Lit(BodyColor(species)), false, Quaternion.Euler(90f, 0f, 0f));
                return go;
            }

            go = Object.Instantiate(prefab, parent, false);
            go.name = "Fish_" + species.Id;
            float meters = lengthCm / 100f;
            go.transform.localScale = Vector3.one * (meters / raw);

            var body = BodyColor(species);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    // 원본 머티리얼 순서: 몸통, 배·지느러미, 눈(대략). 몸통은 어종 색, 나머지는 밝게/어둡게
                    var c = i == 0 ? body : i == 1 ? Color.Lerp(body, Color.white, 0.45f) : new Color(0.08f, 0.08f, 0.08f);
                    mats[i] = Mats.Lit(c, 0.65f);
                }
                r.sharedMaterials = mats;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            var anim = go.GetComponent<Animation>();
            if (anim != null && anim.clip != null)
            {
                anim.wrapMode = WrapMode.Loop;
                anim.Play();
            }
            return go;
        }
    }
}
