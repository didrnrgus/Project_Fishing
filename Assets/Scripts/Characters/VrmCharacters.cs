using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fishing.Vrm;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// VRM 캐릭터 (VRoid Studio 등에서 만든 .vrm, 0.x·1.0 모두). 빌드를 다시 하지 않아도 폴더에 넣으면 캐릭터 창에서 골라 쓸 수 있다.
    /// - 폴더: 문서/짭풍명월/Characters (유저), StreamingAssets/Characters (게임에 넣어 배포하는 기본 캐릭터)
    /// - UniVRM(MIT, Packages/manifest.json)으로 실행 중에 읽는다 (Vrm/VrmLoader, 따로 어셈블리). 한 번 읽은 것은 숨긴 선반에 원본으로 두고 복제해 쓴다
    ///   (필드 캐릭터·대기실 캐릭터·NPC·초상화가 한 파일을 함께 쓴다). 맵을 지을 때는 <see cref="LoadNow"/>로 그 자리에서 읽고,
    ///   캐릭터 창 목록은 <see cref="Request"/>로 뒤에서 읽어 다 읽으면 <see cref="Loaded"/>로 알린다.
    /// - 어떤 파일을 누구에게 입히는지는 <see cref="CharacterRoster"/> (characters.json).
    /// - 복제본은 RuntimeGltfInstance를 떼서 UniVRM이 "씬에 놓인 프리팹"처럼 다룬다(초기 자세 = 원본의 T 자세, 자원은 원본이 가진다).
    /// - 컨트롤 리그 없이(ControlRigGenerationOption.None) 휴머노이드 Animator가 뼈를 바로 움직여서, 우리 동작 클립·팔 IK·손가락 쥐기가 그대로 붙는다.
    /// </summary>
    public static class VrmCharacters
    {
        public sealed class Info
        {
            public string Id, Path, Title, Author, Error;
            public Texture2D Thumbnail;
            /// <summary>VRM 메타 "아바타 사용 허락": 만든 사람만 / 따로 허락받은 사람만이면 경고를 보여 준다.</summary>
            public VrmPermission Permission = VrmPermission.Everyone;
            public bool CreditRequired;
            public float Height;
            public bool Loading;
        }

        public static string UserFolder =>
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "짭풍명월", "Characters");

        /// <summary>[옷 바꾸기]가 웹에서 받은 VRM (OutfitChanger).</summary>
        public static string OutfitFolder => System.IO.Path.Combine(UserFolder, "outfits");

        public static string BuiltInFolder => System.IO.Path.Combine(Application.streamingAssetsPath, "Characters");

        /// <summary>읽은 VRM이 이 키 밖이면(잘못 만든 모델) 1.6m로 맞춘다.</summary>
        const float MinHeight = 1.0f, MaxHeight = 2.2f, FallbackHeight = 1.6f;

        static readonly Dictionary<string, GameObject> _templates = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Info> _info = new Dictionary<string, Info>();
        static Transform _shelf;

        /// <summary>VRM 하나를 다 읽었을 때 (실패해도 알린다, Info.Error).</summary>
        public static event Action<string> Loaded;

        /// <summary>쓸 수 있는 VRM id 목록: "builtin/파일.vrm", "user/파일.vrm".</summary>
        public static List<string> List()
        {
            var ids = new List<string>();
            void Scan(string folder, string prefix)
            {
                try
                {
                    if (!Directory.Exists(folder)) return;
                    var files = Directory.GetFiles(folder, "*.vrm");
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                    foreach (var f in files) ids.Add(prefix + System.IO.Path.GetFileName(f));
                }
                catch (Exception e) { Debug.LogWarning($"[Dev] VRM 폴더를 읽지 못함: {folder} ({e.Message})"); }
            }
            Scan(BuiltInFolder, "builtin/");
            Scan(UserFolder, "user/");
            Scan(OutfitFolder, "user/outfits/");
            return ids;
        }

        public static string PathOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id.StartsWith("builtin/")) return System.IO.Path.Combine(BuiltInFolder, id.Substring(8));
            if (id.StartsWith("user/")) return System.IO.Path.Combine(UserFolder, id.Substring(5));
            return null;
        }

        public static bool Exists(string id)
        {
            var p = PathOf(id);
            return p != null && File.Exists(p);
        }

        /// <summary>유저 폴더를 만든다 (캐릭터 창 "폴더 열기").</summary>
        public static string EnsureUserFolder()
        {
            try { Directory.CreateDirectory(UserFolder); } catch (Exception e) { Debug.LogWarning($"[Dev] VRM 폴더를 만들지 못함: {e.Message}"); }
            return UserFolder;
        }

        public static Info GetInfo(string id) => id != null && _info.TryGetValue(id, out var i) ? i : null;

        /// <summary>읽어 둔 원본 (꺼진 채 선반에 있다, T 자세). 옷 입히기(VrmOutfit)가 옷 조각을 떼어 간다.</summary>
        public static GameObject Template(string id) => id != null && _templates.TryGetValue(id, out var t) ? t : null;

        public static bool IsReady(string id) => id != null && _templates.TryGetValue(id, out var t) && t != null;

        /// <summary>아직 안 읽었으면 읽기 시작한다.</summary>
        public static void Request(string id)
        {
            if (string.IsNullOrEmpty(id) || _info.ContainsKey(id)) return;
            var info = new Info { Id = id, Path = PathOf(id), Title = System.IO.Path.GetFileNameWithoutExtension(id), Loading = true };
            _info[id] = info;
            _ = LoadAsync(info);
        }

        /// <summary>파일을 바꿨을 때 (캐릭터 창 "새로고침"): 읽은 것을 버리고 다시 읽게 한다.</summary>
        public static void Forget(string id)
        {
            if (_templates.TryGetValue(id, out var t) && t != null) UnityEngine.Object.Destroy(t);
            _templates.Remove(id);
            _info.Remove(id);
        }

        static Transform Shelf
        {
            get
            {
                if (_shelf != null) return _shelf;
                var go = new GameObject("VrmShelf");
                go.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(go);
                _shelf = go.transform;
                return _shelf;
            }
        }

        static async Task LoadAsync(Info info)
        {
            try
            {
                if (info.Path == null || !File.Exists(info.Path)) throw new FileNotFoundException("파일이 없습니다", info.Path);
                var (go, meta) = await VrmLoader.LoadAsync(info.Path);
                Store(info, go, meta);
            }
            catch (Exception e) { Fail(info, e); }
            info.Loading = false;
            Loaded?.Invoke(info.Id);
        }

        /// <summary>
        /// 아직 안 읽었으면 지금 이 프레임에 읽는다 (맵을 지을 때 NPC·내 캐릭터용, <see cref="CharacterFactory.Create"/>).
        /// 다른 곳에서 이미 비동기로 읽는 중이면 기다리지 않고 false (그 사이엔 대신 서 있을 모양을 쓴다).
        /// </summary>
        public static bool LoadNow(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (IsReady(id)) return true;
            if (_info.TryGetValue(id, out var old) && (old.Loading || old.Error != null)) return false;
            var info = new Info { Id = id, Path = PathOf(id), Title = System.IO.Path.GetFileNameWithoutExtension(id) };
            _info[id] = info;
            var t0 = Time.realtimeSinceStartup;
            try
            {
                if (info.Path == null || !File.Exists(info.Path)) throw new FileNotFoundException("파일이 없습니다", info.Path);
                var (go, meta) = VrmLoader.Load(info.Path);
                Store(info, go, meta);
                Debug.Log($"[Dev] VRM 바로 읽기 {id}: {(Time.realtimeSinceStartup - t0) * 1000f:0}ms");
            }
            catch (Exception e) { Fail(info, e); }
            // Loaded는 내지 않는다: 부른 쪽이 바로 입히는 중이라, GameUI가 또 입히면 캐릭터가 둘 생긴다
            return IsReady(id);
        }

        static void Fail(Info info, Exception e)
        {
            info.Error = e.Message;
            Debug.LogWarning($"[Dev] VRM 읽기 실패: {info.Id} ({e.GetType().Name}: {e.Message})");
        }

        static void Store(Info info, GameObject go, VrmMeta meta)
        {
            // 아직 켜져 있을 때 키를 잰다 (꺼지면 렌더러 bounds가 비어 있다)
            var b = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            info.Height = any ? b.max.y - go.transform.position.y : 0f;
            float bright = CharacterRoster.BrightnessOf(System.IO.Path.GetFileName(info.Path));
            if (bright != 1f) VrmLoader.ScaleBrightness(go, bright);
            go.SetActive(false);
            go.transform.SetParent(Shelf, false);
            go.name = "VRM_" + info.Title;

            if (meta.Title != null) info.Title = meta.Title;
            info.Author = meta.Author;
            info.Thumbnail = meta.Thumbnail;
            info.Permission = meta.Permission;
            info.CreditRequired = meta.CreditRequired;
            _templates[info.Id] = go;
            Debug.Log($"[Dev] VRM 읽음: {info.Id} \"{info.Title}\" ({info.Author}), 키 {info.Height:0.00}m");
        }

        /// <summary>
        /// 읽어 둔 VRM을 복제해 게임 캐릭터로 만든다 (CharacterFactory.Create와 같은 약속: 루트에 Animator + CharacterAnimator, 발이 원점, 게임 정면 +Z).
        /// 아직 안 읽었으면 읽기 시작하고 null.
        /// </summary>
        public static GameObject Create(string id, Transform parent, string name, bool female)
        {
            if (!IsReady(id)) { Request(id); return null; }
            var info = _info[id];
            var go = UnityEngine.Object.Instantiate(_templates[id], parent, false); // 원본이 꺼져 있어 복제본도 꺼진 채
            go.name = "Character_" + (name ?? info.Title);
            VrmLoader.PrepareClone(go);

            go.transform.localPosition = Vector3.zero;
            // VRM은 +Z를 보지만 우리 동작 클립은 −Z를 보게 맞춰 두었다
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            float s = info.Height >= MinHeight && info.Height <= MaxHeight ? 1f : info.Height > 0.1f ? FallbackHeight / info.Height : 1f;
            go.transform.localScale = Vector3.one * s;

            var anim = go.GetComponent<Animator>();
            if (anim == null) { UnityEngine.Object.Destroy(go); return null; }
            anim.runtimeAnimatorController = null;
            anim.applyRootMotion = false;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            go.SetActive(true);
            go.AddComponent<CharacterAnimator>().Female = female;
            return go;
        }
    }
}
