using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// NPC를 덜 똑같아 보이게: 같은 VRM이라도 이름(해시)에 따라 머리색·상의·하의 색을 바꾼다 (16 문서 3장).
    /// VRoid 재질 이름 규칙(…_HAIR, …_Tops_…_CLOTH, …_Onepice_…, …_Bottoms_…)으로 부위를 찾고,
    /// 텍스처를 밝기만 남긴 채 새 색을 입힌 렌더 텍스처(Resources/Shaders/Recolor)로 바꾼다 — 명암·머리결·옷 주름은 그대로.
    /// 같은 텍스처 + 같은 색은 한 번만 만들어 함께 쓴다 (색 표가 짧아 개수가 묶인다).
    /// 고른 사람(characters.json의 npc 이름 지정, 내 캐릭터)은 바꾸지 않는다.
    /// </summary>
    public static class NpcLooks
    {
        static readonly Color[] Hair =
        {
            new Color(0.10f, 0.09f, 0.09f), // 검정
            new Color(0.24f, 0.16f, 0.11f), // 짙은 갈색
            new Color(0.42f, 0.28f, 0.17f), // 갈색
            new Color(0.52f, 0.25f, 0.14f), // 밤색
            new Color(0.50f, 0.50f, 0.52f), // 잿빛
            new Color(0.82f, 0.80f, 0.77f), // 흰머리
            new Color(0.88f, 0.72f, 0.42f), // 금발
            new Color(0.16f, 0.20f, 0.36f), // 남색
            new Color(0.42f, 0.14f, 0.18f), // 와인
        };

        static readonly Color[] Tops =
        {
            new Color(0.18f, 0.24f, 0.42f), // 남색
            new Color(0.40f, 0.42f, 0.25f), // 올리브
            new Color(0.66f, 0.60f, 0.42f), // 카키
            new Color(0.70f, 0.18f, 0.15f), // 빨강
            new Color(0.88f, 0.88f, 0.86f), // 흰색
            new Color(0.90f, 0.50f, 0.15f), // 주황 (구명조끼 색)
            new Color(0.15f, 0.45f, 0.45f), // 청록
            new Color(0.45f, 0.46f, 0.48f), // 회색
            new Color(0.45f, 0.65f, 0.85f), // 하늘
            new Color(0.80f, 0.65f, 0.20f), // 겨자
        };

        static readonly Color[] Bottoms =
        {
            new Color(0.15f, 0.17f, 0.25f), // 짙은 남색
            new Color(0.20f, 0.20f, 0.22f), // 숯색
            new Color(0.50f, 0.45f, 0.32f), // 카키
            new Color(0.25f, 0.33f, 0.50f), // 청바지
            new Color(0.10f, 0.10f, 0.11f), // 검정
            new Color(0.30f, 0.32f, 0.20f), // 올리브
        };

        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        static readonly int ShadeTex = Shader.PropertyToID("_ShadeTex");
        static readonly int ShadeColor = Shader.PropertyToID("_ShadeColor");
        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int AvgLumId = Shader.PropertyToID("_AvgLum");

        static Material _blit;
        static readonly Dictionary<(Texture, int), RenderTexture> _cache = new Dictionary<(Texture, int), RenderTexture>();
        static readonly Dictionary<Texture, float> _avg = new Dictionary<Texture, float>();

        /// <summary>렌더 텍스처 한 변 최대 (VRoid는 2048 — NPC는 멀리서 보니 반으로).</summary>
        const int MaxSize = 1024;

        enum Part { None, Hair, Tops, Bottoms }

        static Part PartOf(string matName)
        {
            if (matName.Contains("_HAIR")) return Part.Hair;
            if (!matName.Contains("_CLOTH")) return Part.None; // 얼굴·피부·눈은 그대로
            if (matName.Contains("_Tops_") || matName.Contains("_Onepice_") || matName.Contains("_Onepiece_")) return Part.Tops;
            if (matName.Contains("_Bottoms_")) return Part.Bottoms;
            return Part.None; // 신발·장신구(리본·넥타이)는 원래 색
        }

        public static void Apply(GameObject character, string name)
        {
            if (_blit == null)
            {
                var shader = Resources.Load<Shader>("Shaders/Recolor");
                if (shader == null) { Debug.LogWarning("[Dev] Recolor 셰이더 없음"); return; }
                _blit = new Material(shader);
            }
            uint h = CharacterRoster.StableHash(name ?? "");
            // 부위마다 다른 해시 조각 → 머리·상의·하의가 따로 고른다. 1/6은 원래 색 그대로 (모델 본래 모습도 가끔 보이게)
            int hair = Pick(h, 0, Hair.Length), tops = Pick(h, 8, Tops.Length), bottoms = Pick(h, 16, Bottoms.Length);
            foreach (var r in character.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    var part = PartOf(m.name);
                    int idx = part switch { Part.Hair => hair, Part.Tops => tops, Part.Bottoms => bottoms, _ => -1 };
                    if (idx < 0) continue;
                    var color = part switch { Part.Hair => Hair[idx], Part.Tops => Tops[idx], _ => Bottoms[idx] };
                    int key = (int)part * 100 + idx;
                    var copy = new Material(m) { name = m.name + "_npc" };
                    if (m.HasProperty(MainTex)) copy.SetTexture(MainTex, Recolored(m.GetTexture(MainTex), color, key));
                    if (m.HasProperty(ShadeTex)) copy.SetTexture(ShadeTex, Recolored(m.GetTexture(ShadeTex), color, key));
                    if (m.HasProperty(ShadeColor))
                    {
                        // 그림자 색의 색조(분홍·보라 등)를 빼고 밝기만 남긴다
                        var sc = m.GetColor(ShadeColor);
                        float l = sc.grayscale;
                        copy.SetColor(ShadeColor, new Color(l, l, l, sc.a));
                    }
                    mats[i] = copy;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>해시의 shift 비트 자리에서 0..n-1, 6분의 1은 -1(원래 색).</summary>
        static int Pick(uint h, int shift, int n)
        {
            uint v = (h >> shift) * 2654435761u;
            if ((v >> 7) % 6u == 0u) return -1;
            return (int)((v >> 11) % (uint)n);
        }

        static Texture Recolored(Texture src, Color tint, int key)
        {
            if (src == null) return null;
            if (_cache.TryGetValue((src, key), out var rt) && rt != null) return rt;
            float scale = Mathf.Min(1f, MaxSize / (float)Mathf.Max(src.width, src.height));
            int w = Mathf.Max(4, Mathf.RoundToInt(src.width * scale)), h = Mathf.Max(4, Mathf.RoundToInt(src.height * scale));
            rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = src.name + "_recolor",
                useMipMap = true,
                autoGenerateMips = true,
                wrapMode = src.wrapMode,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2,
            };
            rt.Create();
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            _blit.SetColor(TintId, linear ? tint.linear : tint);
            _blit.SetFloat(AvgLumId, AverageLuminance(src));
            Graphics.Blit(src, rt, _blit);
            _cache[(src, key)] = rt;
            return rt;
        }

        /// <summary>텍스처의 평균 밝기 (불투명한 곳만, 셰이더가 보는 값과 같은 공간). 16×16으로 줄여 한 번 읽는다.</summary>
        static float AverageLuminance(Texture src)
        {
            if (_avg.TryGetValue(src, out var a)) return a;
            var small = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(src, small);
            var prev = RenderTexture.active;
            RenderTexture.active = small;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false, true);
            tex.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(small);
            float sum = 0f, wsum = 0f;
            foreach (var c in tex.GetPixels())
            {
                float l = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                sum += l * c.a;
                wsum += c.a;
            }
            Object.Destroy(tex);
            a = wsum > 0.01f ? Mathf.Clamp(sum / wsum, 0.02f, 1f) : 0.5f;
            _avg[src] = a;
            return a;
        }
    }
}
