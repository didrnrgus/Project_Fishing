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

        /// <summary>
        /// Resources/Terrain 텍스처(Poly Haven CC0)를 입힌 머티리얼 (바위·자갈·데크·콘크리트). 없으면 단색.
        /// 셰이더는 반복 무늬를 깨는 Fishing/Ground (두 번 샘플 섞기 + 큰 얼룩), 없으면 Standard.
        /// </summary>
        public static Material Textured(string name, Color tint, float tiling = 1f, float smoothness = 0.2f) =>
            Textured(name, tint, new Vector2(tiling, tiling), smoothness);

        /// <summary>
        /// 크기에 맞춰 반복: 상자 프리미티브는 면마다 UV가 0~1이라, 윗면 크기(가로 x, 세로 z)를 텍스처 한 장 크기(m)로 나눠 반복 횟수를 정한다.
        /// </summary>
        public static Material TexturedBox(string name, Color tint, Vector2 sizeXZ, float metersPerTile, float smoothness = 0.2f) =>
            Textured(name, tint, new Vector2(Mathf.Max(1f, Mathf.Round(sizeXZ.x / metersPerTile)), Mathf.Max(1f, Mathf.Round(sizeXZ.y / metersPerTile))), smoothness);

        public static Material Textured(string name, Color tint, Vector2 tiling, float smoothness = 0.2f)
        {
            string key = $"tex{name}{tint}{tiling}{smoothness}";
            if (Cache.TryGetValue(key, out var m)) return m;
            var diff = Resources.Load<Texture2D>("Terrain/" + name + "_diff");
            if (diff == null) return Lit(tint, smoothness);
            m = new Material(Shader.Find("Fishing/Ground") ?? Shader.Find("Standard")) { color = tint, mainTexture = diff, mainTextureScale = tiling };
            var nor = Resources.Load<Texture2D>("Terrain/" + name + "_nor");
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Glossiness", smoothness);
            Cache[key] = m;
            return m;
        }

        /// <summary>
        /// 석회암 봉우리(하롱베이) 절벽: Fishing/Karst에 실사 석회암 두 장(Resources/Terrain/karst·karst2)을 섞는다.
        /// 메쉬 UV는 1 = 4m(StackMesh). 덤불 양 moss 0~1. 셰이더·텍스처가 없으면 예전 rock 텍스처.
        /// </summary>
        public static Material Karst(Color tint, float moss = 0.6f, float jungle = 0f, float lift = 0f) => Karst(tint, moss, jungle, lift, "karst", "karst2");

        /// <summary>석회암 절벽 셰이더에 다른 바위 결 두 장 (듀덴 석회화 = canyon + cliff, 자킨토스 = limecliff + karst).</summary>
        public static Material Karst(Color tint, float moss, float jungle, float lift, string tex, string layerTex)
        {
            string key = $"karst{tint}{moss}{jungle}{lift}{tex}{layerTex}";
            if (Cache.TryGetValue(key, out var m)) return m;
            var shader = Shader.Find("Fishing/Karst");
            var diff = Resources.Load<Texture2D>("Terrain/" + tex + "_diff");
            var layer = Resources.Load<Texture2D>("Terrain/" + layerTex + "_diff");
            if (shader == null || diff == null || layer == null) return Textured("rock", tint, 1f, 0.2f);
            m = new Material(shader) { color = tint, mainTexture = diff };
            m.SetTexture("_LayerTex", layer);
            var nor = Resources.Load<Texture2D>("Terrain/" + tex + "_nor");
            var lnor = Resources.Load<Texture2D>("Terrain/" + layerTex + "_nor");
            if (nor != null && lnor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetTexture("_LayerBump", lnor);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Moss", moss);
            m.SetFloat("_Jungle", jungle);
            m.SetFloat("_Lift", lift);
            // 밀림 색: 사진의 짙은 열대림 초록 (예전 연두는 이끼처럼 보였다)
            if (jungle > 0f) m.SetColor("_MossColor", new Color(0.17f, 0.3f, 0.13f));
            Cache[key] = m;
            return m;
        }

        /// <summary>Resources/Terrain 디퓨즈 텍스처의 평균 색 (감마). 풍화 셰이더가 나눠서 결만 남긴다. 새 텍스처를 넣으면 여기에 더한다.</summary>
        static readonly Dictionary<string, Color> TexAvg = new Dictionary<string, Color>
        {
            { "aggregate", new Color(0.579f, 0.518f, 0.454f) }, { "asphalt", new Color(0.355f, 0.352f, 0.332f) }, { "basalt", new Color(0.765f, 0.612f, 0.463f) },
            { "cliff", new Color(0.513f, 0.412f, 0.315f) }, { "concrete", new Color(0.469f, 0.432f, 0.356f) }, { "deck", new Color(0.433f, 0.204f, 0.054f) },
            { "greytile", new Color(0.379f, 0.361f, 0.294f) }, { "karst", new Color(0.557f, 0.556f, 0.53f) }, { "karst2", new Color(0.531f, 0.52f, 0.5f) },
            { "marble", new Color(0.696f, 0.615f, 0.477f) }, { "planks", new Color(0.313f, 0.264f, 0.23f) }, { "plaster", new Color(0.453f, 0.439f, 0.295f) },
            { "rock", new Color(0.431f, 0.363f, 0.205f) }, { "rooftile", new Color(0.57f, 0.313f, 0.168f) }, { "rust", new Color(0.673f, 0.563f, 0.354f) },
            { "sand", new Color(0.509f, 0.449f, 0.359f) }, { "sandstone", new Color(0.536f, 0.409f, 0.244f) }, { "stonewall", new Color(0.436f, 0.403f, 0.371f) },
            { "thatch", new Color(0.331f, 0.313f, 0.286f) }, { "grass", new Color(0.33f, 0.45f, 0.16f) }, { "mud", new Color(0.287f, 0.261f, 0.221f) },
            { "snow", new Color(0.835f, 0.862f, 0.901f) }, { "shore", new Color(0.294f, 0.268f, 0.177f) }, { "wallconcrete", new Color(0.359f, 0.316f, 0.271f) }, { "whitestone", new Color(0.536f, 0.5f, 0.433f) },
            { "seacliff", new Color(0.584f, 0.549f, 0.518f) }, { "limecliff", new Color(0.761f, 0.639f, 0.545f) }, { "canyon", new Color(0.482f, 0.322f, 0.2f) },
            { "granite", new Color(0.443f, 0.322f, 0.259f) }, { "darkrock", new Color(0.133f, 0.11f, 0.102f) }, { "pebbles", new Color(0.502f, 0.455f, 0.412f) },
        };

        /// <summary>텍스처 평균 색 (풍화·랜드마크 셰이더가 결만 남길 때 쓴다).</summary>
        public static Color AvgOf(string name) => TexAvg.TryGetValue(name, out var c) ? c : new Color(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// 풍화 표면 (Fishing/Weathered): Resources/Terrain/{name} 텍스처를 월드 좌표 트라이플래너로 metersPerTile(m)마다 깔고,
        /// 만조선 아래 물 자국(wet), 세로 면 빗물 자국(streaks), 위를 보는 면 이끼(moss)를 얹는다. UV가 없는 메쉬에도 쓸 수 있다.
        /// tint = 칠한 색 그대로 (셰이더가 텍스처를 평균 색으로 나눠 결만 남긴 뒤 곱한다). FBX 소품의 원래 부위 색을 넘기면 색은 두고 결만 입는다.
        /// side = 세로 면에 쓸 다른 텍스처 (예: 윗면 바닥 콘크리트 + 옆면 벽 콘크리트). 셰이더·텍스처가 없으면 Lit.
        /// </summary>
        public static Material Weathered(string name, Color tint, float metersPerTile = 3f, float smoothness = 0.2f,
            float wet = 1f, float streaks = 0.5f, float moss = 0.3f, Color? mossColor = null, float desat = 0.3f, float metallic = 0f, string side = null, float contrast = 1f)
        {
            var mc = mossColor ?? new Color(0.32f, 0.36f, 0.2f);
            string key = $"wx{name}{side}{tint}{metersPerTile}{smoothness}{wet}{streaks}{moss}{mc}{desat}{metallic}{contrast}";
            if (Cache.TryGetValue(key, out var m)) return m;
            var shader = Shader.Find("Fishing/Weathered");
            var diff = Resources.Load<Texture2D>("Terrain/" + name + "_diff");
            if (shader == null || diff == null) return Lit(tint, smoothness, metallic);
            m = new Material(shader) { color = tint, mainTexture = diff };
            m.SetColor("_TexAvg", TexAvg.TryGetValue(name, out var avg) ? avg : new Color(0.5f, 0.5f, 0.5f));
            var nor = Resources.Load<Texture2D>("Terrain/" + name + "_nor");
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.EnableKeyword("_NORMALMAP");
            }
            // 세로 면 텍스처 (예: 윗면 바닥 콘크리트 + 옆면 거푸집 벽 콘크리트). 없으면 윗면과 같게
            var sdiff = side != null ? Resources.Load<Texture2D>("Terrain/" + side + "_diff") : null;
            var snor = side != null ? Resources.Load<Texture2D>("Terrain/" + side + "_nor") : null;
            m.SetTexture("_SideTex", sdiff != null ? sdiff : diff);
            m.SetColor("_SideAvg", TexAvg.TryGetValue(sdiff != null ? side : name, out var savg) ? savg : new Color(0.5f, 0.5f, 0.5f));
            if (nor != null || snor != null) m.SetTexture("_SideBump", snor != null ? snor : nor);
            m.SetFloat("_Tile", metersPerTile);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Wet", wet);
            m.SetFloat("_Streaks", streaks);
            m.SetFloat("_Moss", moss);
            m.SetColor("_MossColor", mc);
            m.SetFloat("_Desat", desat);
            m.SetFloat("_Contrast", contrast);
            Cache[key] = m;
            return m;
        }

        // ---- 용도별 풍화 재질 (색 = 칠한 색 그대로) ----
        /// <summary>회벽·미장 벽: 거친 회벽 결 + 처마 밑에서 흘러내린 빗물 자국.</summary>
        public static Material Wall(Color c, float smoothness = 0.12f) => Weathered("plaster", c, 2.5f, smoothness, 0f, 0.45f, 0.08f, new Color(0.4f, 0.42f, 0.3f), 0.85f, 0f, null, 0.45f);
        /// <summary>나무 판·기둥: 비바람에 닳은 판재 결. 물에 닿는 말뚝은 아래가 젖는다.</summary>
        public static Material Wood(Color c, float smoothness = 0.25f) => Weathered("planks", c, 1.8f, smoothness, 0.7f, 0.15f, 0.08f, null, 0.75f);
        /// <summary>점토 기와 (지중해·동남아 붉은 기와, 색만 바꿔 초록 기와도).</summary>
        public static Material Tile(Color c, float smoothness = 0.3f) => Weathered("rooftile", c, 1.6f, smoothness, 0f, 0.25f, 0.15f, null, 0.8f);
        /// <summary>초가·짚·갈대 지붕.</summary>
        public static Material Thatch(Color c) => Weathered("thatch", c, 1.5f, 0.05f, 0f, 0.1f, 0.1f, null, 0.7f);
        /// <summary>쌓은 돌·석재: 돌 쌓기 결, 물가는 젖고 위는 이끼.</summary>
        public static Material Stone(Color c, float smoothness = 0.15f) => Weathered("stonewall", c, 2f, smoothness, 1f, 0.4f, 0.3f, null, 0.8f);
        /// <summary>석상·조각 돌 (사암·화강암 느낌): 결이 고운 사암 텍스처, 빗물 줄과 이끼.</summary>
        public static Material Carved(Color c, float smoothness = 0.15f, float moss = 0.3f) => Weathered("sandstone", c, 2.5f, smoothness, 1f, 0.55f, moss, new Color(0.25f, 0.3f, 0.2f), 0.85f);

        /// <summary>호안·방파제·부두 콘크리트: 윗면은 바닥 콘크리트(금·기름 얼룩·물웅덩이), 옆면은 거푸집 자국 난 벽 콘크리트, 물 자국·빗물 줄·이끼.</summary>
        public static Material Quay(Color tint, float smoothness = 0.2f)
        {
            var m = Weathered("concrete", tint * 0.66f, 3.5f, smoothness, 1f, 0.6f, 0.25f, null, 0.6f, 0f, "wallconcrete");
            if (m.HasProperty("_Grime")) m.SetFloat("_Grime", 1f); // 윗면에 금·기름 얼룩·물웅덩이
            return m;
        }

        /// <summary>
        /// 풍화 재질에 꼭짓점 색(틈·밑동 그늘, 블록마다 다른 타설 색)을 곱하는 복사본 (HarborKit 절차 메쉬용).
        /// 꼭짓점 색이 없는 메쉬에 쓰면 안 된다. 풍화 셰이더가 아니면 그대로 돌려준다.
        /// </summary>
        public static Material VertexAO(Material src)
        {
            if (src == null || !src.HasProperty("_VertexAO")) return src;
            string key = "vao" + src.GetEntityId();
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(src) { name = src.name + "_VAO" };
            m.SetFloat("_VertexAO", 1f);
            Cache[key] = m;
            return m;
        }

        /// <summary>테트라포드·소파 블록: 자갈 박힌 거친 콘크리트, 아래쪽 물때·따개비.</summary>
        public static Material Tetrapod(Color tint) =>
            Weathered("aggregate", tint * 0.66f, 1.6f, 0.12f, 1f, 0.3f, 0.2f, null, 0.7f);

        /// <summary>
        /// 이미 놓은 물체(FBX 소품 등)의 머티리얼을 모두 풍화 표면으로 바꾼다. 부위마다 원래 색을 tint로 넘겨 색 구분은 그대로 둔다.
        /// skip에 걸리는 색(예: 눈·금박처럼 반짝여야 할 부위)은 그대로 둔다.
        /// </summary>
        public static void Weatherize(GameObject go, string name, float metersPerTile, float smoothness = 0.2f, float wet = 1f, float streaks = 0.5f,
            float moss = 0.3f, Color? mossColor = null, System.Func<Color, bool> skip = null, float desat = 0.6f, float contrast = 1f)
        {
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var c = mats[i].HasProperty("_Color") ? mats[i].color : Color.white;
                    if (skip != null && skip(c)) continue;
                    // Blender FBX 색은 선형 값으로 들어온다 (감마 프로젝트에서 그대로 쓰면 미리보기보다 한참 어둡다: 베이지 머라이언이 짙은 회색)
                    c = c.gamma;
                    mats[i] = Weathered(name, new Color(c.r, c.g, c.b, 1f), metersPerTile, smoothness, wet, streaks, moss, mossColor, desat, 0f, null, contrast);
                }
                r.sharedMaterials = mats;
            }
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
