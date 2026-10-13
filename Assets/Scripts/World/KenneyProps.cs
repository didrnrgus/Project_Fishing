using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// Kenney CC0 모델(City Kit Commercial, City Kit Roads, Nature Kit, Watercraft Kit)을 배치하는 헬퍼.
    ///  - 원본은 1~5 단위 크기라서 원하는 높이(또는 길이)에 맞춰 균일하게 키운다
    ///  - Nature Kit의 청록색 잎·풀은 한강 풍경에 맞는 색으로 다시 칠한다
    ///  - 도시 건물은 밤에 창문 불빛처럼 은은하게 빛나도록 발광 머티리얼로 바꾼다 (<see cref="SetNightGlow"/>)
    /// 에셋이 없으면 <see cref="Available"/>이 false이고, WorldBuilder는 예전 프리미티브로 그린다.
    /// </summary>
    public static class KenneyProps
    {
        static bool? _available;
        public static bool Available => _available ??= Resources.Load<GameObject>("Kenney/Nature/tree_default") != null;

        static readonly Dictionary<string, Material> Recolored = new Dictionary<string, Material>();
        static Material _cityGlow;

        // Nature Kit 머티리얼 이름 → 한강 팔레트
        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "leafsGreen", new Color(0.27f, 0.47f, 0.2f) },
            { "leafsDark", new Color(0.17f, 0.34f, 0.19f) },
            { "woodBark", new Color(0.4f, 0.29f, 0.2f) },
            { "woodBarkDark", new Color(0.32f, 0.24f, 0.18f) },
            { "grass", new Color(0.3f, 0.48f, 0.21f) },
            { "dirt", new Color(0.48f, 0.43f, 0.37f) },
            { "_defaultMat", new Color(0.55f, 0.55f, 0.53f) },
        };

        /// <summary>
        /// 모델을 만든다. height > 0이면 높이를, length > 0이면 가장 긴 수평 길이를 그 값에 맞춘다.
        /// </summary>
        public static GameObject Spawn(string path, Transform parent, Vector3 localPos, float yaw, float height = 0f, float length = 0f)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var size = LocalBounds(go).size;
            float s = 1f;
            if (height > 0f && size.y > 0.001f) s = height / size.y;
            else if (length > 0f) s = length / Mathf.Max(0.001f, Mathf.Max(size.x, size.z));
            go.transform.localScale = Vector3.one * s;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            bool nature = path.Contains("/Nature/");
            bool city = path.Contains("/City/");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (nature) mats[i] = Recolor(mats[i]);
                    else if (city) mats[i] = CityGlow(mats[i]);
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = city ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            return go;
        }

        /// <summary>스케일 1 기준 렌더러 경계 (부모 기준 좌표).</summary>
        public static Bounds LocalBounds(GameObject go)
        {
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                // 메쉬 경계의 8 꼭짓점을 루트 기준으로 옮겨 합친다
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static Material Recolor(Material src)
        {
            var key = src.name.Replace(" (Instance)", "");
            if (Recolored.TryGetValue(key, out var m)) return m;
            if (!Palette.TryGetValue(key, out var color)) { Recolored[key] = src; return src; }
            m = Mats.Lit(color, key.StartsWith("leafs") ? 0.25f : 0.1f);
            Recolored[key] = m;
            return m;
        }

        static Material CityGlow(Material src)
        {
            if (_cityGlow != null) return _cityGlow;
            _cityGlow = new Material(src) { name = "KenneyCityGlow" };
            _cityGlow.EnableKeyword("_EMISSION");
            _cityGlow.SetTexture("_EmissionMap", src.mainTexture);
            _cityGlow.SetColor("_EmissionColor", Color.black);
            _cityGlow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return _cityGlow;
        }

        /// <summary>밤(0~1)에 도시 건물이 창문 불빛처럼 은은하게 빛난다. DayNightCycle이 부른다.</summary>
        public static void SetNightGlow(float night)
        {
            if (_cityGlow == null) return;
            _cityGlow.SetColor("_EmissionColor", new Color(0.55f, 0.47f, 0.32f) * Mathf.Clamp01(night) * 0.55f);
        }
    }
}
