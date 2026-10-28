using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 어종별 물고기 모델.
    ///  - 우선 <c>Resources/Fish/Species/&lt;어종 Id&gt;</c> (tools/blender/build_fish.py가 만든 모델: 몸·지느러미·LOD1,
    ///    클립 Swim_Slow · Swim_Fast · Thrash · Flop · Idle_Hover, 텍스처 Fish/Textures/&lt;Id&gt;(_n, _m))
    ///  - 없으면 Quaternius Animated Fish Pack(CC0) Fish1~3에 어종 색을 입힌다 (예전 방식, 하나씩 바꿔 나갈 수 있게)
    /// 모델의 머리는 +Z, 길이는 BakeMesh로 재서 실제 cm에 맞춘다.
    /// </summary>
    public static class FishModels
    {
        public const string SwimSlow = "Swim_Slow", SwimFast = "Swim_Fast", Thrash = "Thrash", Flop = "Flop", IdleHover = "Idle_Hover";

        static readonly System.Collections.Generic.Dictionary<string, (Material body, Material fin)> MatCache =
            new System.Collections.Generic.Dictionary<string, (Material, Material)>();
        static readonly System.Collections.Generic.Dictionary<string, Sprite> IconCache = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>잡동사니 3D 모델(Resources/Junk, tools/blender/build_icons.py)이 있는지.</summary>
        public static bool HasJunkModel(FishSpecies s) => s != null && s.IsJunk && Resources.Load<GameObject>("Junk/" + s.Id) != null;

        /// <summary>잡동사니: 길이 1m로 만든 정적 모델을 실제 크기로. 머티리얼은 FBX 색 그대로.</summary>
        static GameObject CreateJunk(FishSpecies species, GameObject prefab, float lengthCm, Transform parent)
        {
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Junk_" + species.Id;
            go.transform.localScale *= lengthCm / 100f;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>어종 전용 모델이 있는지.</summary>
        public static bool HasModel(FishSpecies s) => s != null && Resources.Load<GameObject>("Fish/Species/" + s.Id) != null;

        /// <summary>도감 목록 아이콘 (Icons/Fish/&lt;Id&gt;, 없으면 null). 못 낚은 어종은 UI에서 검게 칠해 실루엣으로 쓴다.</summary>
        public static Sprite Icon(FishSpecies s)
        {
            if (s == null) return null;
            if (IconCache.TryGetValue(s.Id, out var sp)) return sp;
            sp = Resources.Load<Sprite>("Icons/Fish/" + s.Id);
            IconCache[s.Id] = sp;
            return sp;
        }

        static (Material body, Material fin) SpeciesMats(string id)
        {
            if (MatCache.TryGetValue(id, out var m)) return m;
            var shader = Shader.Find("Fishing/Fish") ?? Shader.Find("Standard");
            var finShader = Shader.Find("Fishing/FishFin") ?? shader; // 지느러미는 반투명 막
            var albedo = Resources.Load<Texture2D>("Fish/Textures/" + id);
            var normal = Resources.Load<Texture2D>("Fish/Textures/" + id + "_n");
            var metal = Resources.Load<Texture2D>("Fish/Textures/" + id + "_m");
            Material Make(bool fin)
            {
                var mat = new Material(fin ? finShader : shader) { name = $"Fish_{id}_{(fin ? "Fin" : "Body")}" };
                if (albedo != null) mat.SetTexture("_MainTex", albedo);
                if (normal != null) mat.SetTexture("_BumpMap", normal);
                if (metal != null) mat.SetTexture("_MetalTex", metal);
                mat.SetFloat("_Cutoff", fin ? 0.5f : 0f); // 지느러미: 그림자만 잘라내기
                mat.SetFloat("_Translucency", fin ? 0.6f : 0f);
                mat.SetFloat("_Sheen", 0.6f);
                return mat;
            }
            m = (Make(false), Make(true));
            MatCache[id] = m;
            return m;
        }

        /// <summary>
        /// 지금 자세의 실제 월드 바운드 (스킨드 메쉬를 구워서 잰다). 만든 직후에는 SkinnedMeshRenderer.bounds가
        /// 아직 갱신되지 않아 엉뚱한 값이 나오므로, 화면에 맞춰 크기를 정할 때는 이걸 쓴다.
        /// </summary>
        public static Bounds BakedBounds(GameObject go)
        {
            bool any = false;
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || r.name.EndsWith("_LOD1")) continue;
                if (r is SkinnedMeshRenderer smr)
                {
                    var m = new Mesh();
                    // 실험으로 확인: BakeMesh(false)는 뼈에 실린 크기까지 들어간 정점을 준다 → 위치·회전만 곱한다
                    // (BakeMesh(true)는 크기가 빠지고, false + localToWorld는 크기가 두 번 들어간다)
                    smr.BakeMesh(m, false);
                    var mtx = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    foreach (var v in m.vertices)
                    {
                        var w = mtx.MultiplyPoint3x4(v);
                        if (!any) { b = new Bounds(w, Vector3.zero); any = true; }
                        else b.Encapsulate(w);
                    }
                    Object.Destroy(m);
                }
                else
                {
                    if (!any) { b = r.bounds; any = true; }
                    else b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        /// <summary>클립을 반복 재생한다 (fade초 동안 섞으며 바꾼다). 예전 모델(클립 하나)은 그대로 둔다.</summary>
        public static void Play(GameObject fish, string clip, float speed = 1f, float fade = 0.2f)
        {
            if (fish == null) return;
            var anim = fish.GetComponentInChildren<Animation>();
            if (anim == null || anim[clip] == null) return;
            anim[clip].wrapMode = WrapMode.Loop;
            anim[clip].speed = speed;
            if (fade > 0f && anim.isPlaying) anim.CrossFade(clip, fade);
            else anim.Play(clip);
        }

        /// <summary>재생 중인 클립의 속도만 바꾼다 (헤엄치는 속도에 꼬리 박자를 맞출 때, 매 프레임 불러도 된다).</summary>
        public static void SetSpeed(GameObject fish, string clip, float speed)
        {
            var anim = fish != null ? fish.GetComponentInChildren<Animation>() : null;
            if (anim != null && anim[clip] != null) anim[clip].speed = speed;
        }

        /// <summary>박제처럼 한 자세로 멈춘다 (대기실 트로피).</summary>
        public static void Freeze(GameObject fish, string clip = IdleHover, float time = 0f)
        {
            var anim = fish != null ? fish.GetComponentInChildren<Animation>() : null;
            if (anim == null) return;
            if (anim[clip] == null) { anim.Stop(); return; }
            anim.Play(clip);
            anim[clip].time = time;
            anim[clip].speed = 0f;
            anim.Sample();
        }

        static GameObject CreateSpecies(FishSpecies species, GameObject prefab, float lengthCm, Transform parent)
        {
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Fish_" + species.Id;
            // build_fish.py가 주둥이~꼬리 끝 1m로 만든다 (몸·지느러미를 따로 재면 각각 0.8m라 재지 않는다)
            go.transform.localScale = Vector3.one * (lengthCm / 100f);
            var (body, fin) = SpeciesMats(species.Id);
            Renderer lod0Body = null, lod0Fin = null, lod1Body = null, lod1Fin = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                bool isFin = r.name.StartsWith("Fins");
                r.sharedMaterial = isFin ? fin : body;
                r.shadowCastingMode = isFin ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                if (r.name == "Body") lod0Body = r;
                else if (r.name == "Fins") lod0Fin = r;
                else if (r.name == "Body_LOD1") lod1Body = r;
                else if (r.name == "Fins_LOD1") lod1Fin = r;
            }
            // 가까이서는 원본, 멀어지면(수족관 여러 마리) 줄인 LOD1
            if (lod0Body != null && lod1Body != null)
            {
                var group = go.AddComponent<LODGroup>();
                group.SetLODs(new[]
                {
                    new LOD(0.08f, new[] { lod0Body, lod0Fin }),
                    new LOD(0.004f, new[] { lod1Body, lod1Fin }),
                });
                group.RecalculateBounds();
            }
            var anim = go.GetComponentInChildren<Animation>();
            if (anim != null)
            {
                anim.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (AnimationState st in anim) st.wrapMode = WrapMode.Loop;
                if (anim[SwimSlow] != null) anim.Play(SwimSlow);
            }
            return go;
        }
        static readonly string[] Models =
        {
            "Fish/Fish1", // 몸통이 두툼한 고기: 잉어, 붕어, 메기 …
            "Fish/Fish2", // 지느러미가 큰 고기: 쏘가리, 배스, 블루길 …
            "Fish/Fish3", // 날씬한 고기: 피라미, 끄리, 강준치, 숭어, 뱀장어 …
        };

        public static int ModelIndex(FishSpecies s) => s.Id switch
        {
            "mandarin" or "bass" or "bluegill" or "nuchi" or "greenling" or "filefish" or "opaleye" => 1,
            "zacco" or "kkeuri" or "skygager" or "mullet" or "eel" or "bagrid"
                or "mackerel" or "horsemackerel" or "halfbeak" or "seabass" or "conger" => 2,
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
            "mackerel" => new Color(0.25f, 0.5f, 0.45f),
            "horsemackerel" => new Color(0.55f, 0.62f, 0.6f),
            "halfbeak" => new Color(0.6f, 0.75f, 0.8f),
            "rockfish" => new Color(0.45f, 0.38f, 0.3f),
            "filefish" => new Color(0.6f, 0.55f, 0.42f),
            "marbledflounder" => new Color(0.5f, 0.45f, 0.35f),
            "scorpionfish" => new Color(0.65f, 0.35f, 0.25f),
            "jacopever" => new Color(0.35f, 0.33f, 0.3f),
            "greenling" => new Color(0.55f, 0.45f, 0.3f),
            "flounder" => new Color(0.42f, 0.38f, 0.3f),
            "conger" => new Color(0.45f, 0.42f, 0.35f),
            "blackseabream" => new Color(0.32f, 0.33f, 0.35f),
            "opaleye" => new Color(0.25f, 0.28f, 0.27f),
            "seabass" => new Color(0.7f, 0.74f, 0.78f),
            "redseabream" => new Color(0.9f, 0.45f, 0.45f),
            "beakfish" => new Color(0.55f, 0.55f, 0.5f),
            "giantgrouper" => new Color(0.4f, 0.32f, 0.25f),
            "robotfish" => new Color(0.75f, 0.78f, 0.82f),
            "goby" => new Color(0.55f, 0.5f, 0.4f),
            "shad" => new Color(0.7f, 0.75f, 0.8f),
            "whitecroaker" => new Color(0.78f, 0.78f, 0.75f),
            "octopus" => new Color(0.6f, 0.45f, 0.4f),
            "cuttlefish" => new Color(0.65f, 0.55f, 0.45f),
            "croaker" => new Color(0.5f, 0.48f, 0.42f),
            _ => new Color(0.75f, 0.78f, 0.82f)
        };

        static readonly System.Collections.Generic.Dictionary<string, float> LengthCache = new System.Collections.Generic.Dictionary<string, float>();

        /// <summary>
        /// 스킨드 메쉬는 프리팹 bounds가 뼈 스케일을 반영하지 않을 수 있어서, 실제로 구워서(BakeMesh) 길이를 잰다.
        /// 스케일 1인 상태에서 한 번만 재고 캐시한다.
        /// </summary>
        static float MeasuredLength(string path, GameObject instance)
        {
            if (LengthCache.TryGetValue(path, out var cached)) return cached;
            float length = 0f;
            foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var size = Vector3.Scale(baked.bounds.size, smr.transform.lossyScale);
                length = Mathf.Max(length, Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
                Object.Destroy(baked);
            }
            if (length <= 0.0001f)
                foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    var size = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
                    length = Mathf.Max(length, Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
                }
            if (length <= 0.0001f) length = 1f;
            // 부모 스케일이 섞이지 않도록 부모 기준으로 환산
            if (instance.transform.parent != null) length /= Mathf.Max(0.0001f, instance.transform.parent.lossyScale.x);
            LengthCache[path] = length;
            return length;
        }

        public static GameObject Create(FishSpecies species, float lengthCm, Transform parent)
        {
            var own = species != null && !species.IsJunk ? Resources.Load<GameObject>("Fish/Species/" + species.Id) : null;
            if (own != null) return CreateSpecies(species, own, lengthCm, parent);
            var junk = species != null && species.IsJunk ? Resources.Load<GameObject>("Junk/" + species.Id) : null;
            if (junk != null) return CreateJunk(species, junk, lengthCm, parent);
            var path = Models[ModelIndex(species)];
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
            go.transform.localScale = Vector3.one * (meters / MeasuredLength(path, go));

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
