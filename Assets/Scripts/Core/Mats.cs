using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>런타임 머티리얼 캐시. 에셋 없이 프리미티브로 월드를 만들기 때문에 색상별로 공유한다.</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static Texture2D _softDot, _ring;

        /// <summary>파티클용 둥근 점 텍스처. 텍스처가 없으면 파티클이 네모로 보인다.</summary>
        static Texture2D SoftDot => _softDot ??= RadialTexture(d => Mathf.Pow(Mathf.Clamp01(1f - d), 2f));

        /// <summary>파문용 고리 텍스처.</summary>
        static Texture2D RingTex => _ring ??= RadialTexture(d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) * 8f));

        static Texture2D RadialTexture(System.Func<float, float> alphaByRadius)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaByRadius(Mathf.Sqrt(dx * dx + dy * dy))));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>파문 하나마다 알파를 따로 바꾸므로 공유하지 않는 인스턴스를 만든다. 사용 후 Destroy할 것.</summary>
        public static Material NewRipple()
        {
            var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            m.mainTexture = RingTex;
            return m;
        }

        public static Material Lit(Color color, float smoothness = 0.15f, float metallic = 0f)
        {
            string key = $"lit{color}{smoothness}{metallic}";
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            Cache[key] = m;
            return m;
        }

        public static Material Emissive(Color color, float intensity = 1.5f)
        {
            string key = $"emi{color}{intensity}";
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Shader.Find("Standard")) { color = color };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            Cache[key] = m;
            return m;
        }

        /// <summary>Standard 셰이더 Fade 모드. 인스턴스마다 알파를 바꿔야 하면 shared=false.</summary>
        public static Material Transparent(Color color, float smoothness = 0.9f, bool shared = true)
        {
            string key = $"tr{color}{smoothness}";
            if (shared && Cache.TryGetValue(key, out var cached)) return cached;
            var m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Mode", 2f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetFloat("_Glossiness", smoothness);
            m.renderQueue = 3000;
            if (shared) Cache[key] = m;
            return m;
        }

        public static Material Unlit(Color color)
        {
            string key = $"unlit{color}";
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Shader.Find("Unlit/Color")) { color = color };
            Cache[key] = m;
            return m;
        }

        public static Material Particle(Color color)
        {
            string key = $"pt{color}";
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            m.SetColor("_TintColor", color);
            m.mainTexture = SoftDot;
            Cache[key] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
            Material mat, bool collider = true, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
    }
}
