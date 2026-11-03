using System;
using System.Collections.Generic;
using Fishing.Audio;
using Fishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 게임을 켜면 나오는 인트로: (Unity 스플래시 →) 사업자 로고 「제쓰로네 게임공방」 → 타이틀 「청풍명월」 → 아무 키 → 대기실.
    /// 그림은 tools/intro/build_intro_art.py가 만든 Resources/Intro/*.png를 겹으로 쌓아 움직인다(시차·물안개·물빛·배·갈대·꽃잎).
    /// 소리는 IntroAudio(절차적). 모든 연출은 시간 t의 함수(Pose)라서 건너뛰기는 t를 앞으로 옮기기만 하면 된다.
    /// 무대 좌표는 1920×1080(가운데 원점)이고, 화면 비율이 달라도 무대를 덮도록(cover) 키운다.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        public enum Stage { Loading, Company, Title, Leaving, Revealing }

        /// <summary>인트로가 화면을 덮고 있는 동안 true (게임 단축키를 막는다).</summary>
        public static bool Playing => Instance != null && Instance.Current != Stage.Revealing;
        public static IntroSequence Instance { get; private set; }

        public Stage Current { get; private set; }
        /// <summary>현재 장면 안의 시간(초).</summary>
        public float T => _t;
        /// <summary>타이틀 글씨가 다 써지고 "아무 키" 안내가 뜬 상태.</summary>
        public bool Ready => Current == Stage.Title && _t >= TitleReadyAt;

        const float CompanyFadeOutAt = 4.6f, CompanyEnd = 5.4f;
        const float TitleReadyAt = 5.6f;
        const float LeaveTime = 1.1f, RevealTime = 0.9f;

        Action _onFinished;
        float _t, _clock;
        Vector2 _mouse;

        Canvas _canvas;
        RectTransform _stage, _company, _title, _world, _front;
        Image _curtain;
        readonly List<Material> _mats = new List<Material>();
        readonly List<Texture> _loaded = new List<Texture>();     // Resources에서 불러온 것 (끝나면 내린다)
        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();   // 여기서 만든 텍스처·스프라이트
        Shader _shader;

        // 사업자 화면
        RawImage _paper, _seal, _sealGlow, _ko, _en;
        Image _lineL, _lineR;
        RectTransform[] _specks;
        Vector2[] _speckDir;

        // 타이틀 화면
        RawImage _sky, _moon, _wisps, _far, _water, _pathA, _pathB, _mistA, _mistB, _near, _boat, _boatRefl, _reeds;
        RawImage _titleImg, _titleGlow, _titleEn, _titleTag, _copyright;
        Image _vignette;
        TextMeshProUGUI _prompt;
        Petal[] _petals;

        // 소리
        AudioSource _sfx, _music;
        System.Threading.Tasks.Task<IntroAudio.Bank> _audioTask;
        AudioClip _stamp, _chime, _brush, _gong, _theme;
        readonly HashSet<string> _fired = new HashSet<string>();
        float _musicTarget;

        struct Petal
        {
            public RectTransform Rt;
            public Image Img;
            public float X, Y, Vx, Phase, Spin, Size, Depth;
        }

        /// <summary>인트로를 시작한다. onFinished는 타이틀을 지나 대기실이 보이기 시작할 때 한 번 불린다.</summary>
        public static IntroSequence Play(Action onFinished)
        {
            var go = new GameObject("Intro");
            var intro = go.AddComponent<IntroSequence>();
            intro._onFinished = onFinished;
            return intro;
        }

        void Awake()
        {
            Instance = this;
            _shader = Shader.Find("Fishing/UIInkReveal");
            _canvas = UIFactory.CreateCanvas("IntroCanvas", 500);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;

            _stage = UIFactory.Node("Stage", root).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            BuildCompany();
            BuildTitle();
            _title.gameObject.SetActive(false);

            // 맨 위 검은 막: 장면 전환 + 아래(대기실) 클릭 막기
            _curtain = UIFactory.Image(root, Color.black, null, "Curtain");
            _curtain.rectTransform.Fill();
            _curtain.raycastTarget = true;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.loop = true;
            _music.volume = 0f;
            _audioTask = IntroAudio.BuildAsync();
            Current = Stage.Loading;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var m in _mats) Destroy(m);
            foreach (var c in new[] { _stamp, _chime, _brush, _gong, _theme }) if (c != null) Destroy(c);
            foreach (var o in _owned) if (o != null) Destroy(o);
            foreach (var t in _loaded) if (t != null) Resources.UnloadAsset(t);
        }

        // ------------------------------------------------------------ 만들기

        Texture2D Tex(string name, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = Resources.Load<Texture2D>("Intro/" + name);
            if (t != null) { t.wrapMode = wrap; _loaded.Add(t); }
            return t;
        }

        Material Mat(bool additive = false, float progress = 1f)
        {
            if (_shader == null) return null;
            var m = new Material(_shader);
            m.SetFloat("_Progress", progress);
            m.SetFloat("_Shine", -1f);
            if (additive) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _mats.Add(m);
            return m;
        }

        /// <summary>무대 좌표(가운데 원점, 위가 +)에 그림 한 장.</summary>
        RawImage Layer(Transform parent, string tex, Vector2 pos, Vector2 size, Material mat = null, Vector2? pivot = null, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var rt = UIFactory.Node(tex, parent).At(new Vector2(0.5f, 0.5f), pos, size, pivot ?? new Vector2(0.5f, 0.5f));
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = Tex(tex, wrap);
            img.raycastTarget = false;
            if (mat != null) img.material = mat;
            if (img.texture == null) img.color = Color.clear;
            return img;
        }

        /// <summary>미리보기 그림(1920×1080, 왼쪽 위 원점) 좌표 → 무대 좌표.</summary>
        static Vector2 P(float x, float y) => new Vector2(x - 960f, 540f - y);

        void BuildCompany()
        {
            _company = UIFactory.Node("Company", _stage).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            _paper = Layer(_company, "company_paper", Vector2.zero, new Vector2(1920, 1080) * 1.04f);
            _sealGlow = Layer(_company, "company_seal_glow", new Vector2(0, 125), new Vector2(273, 273), Mat(true));
            _seal = Layer(_company, "company_seal", new Vector2(0, 125), new Vector2(230, 230), Mat());

            _specks = new RectTransform[16];
            _speckDir = new Vector2[_specks.Length];
            var r = new System.Random(3);
            for (int i = 0; i < _specks.Length; i++)
            {
                var img = UIFactory.Image(_company, new Color(0.62f, 0.12f, 0.12f, 0f), UIFactory.Circle, "Speck");
                _specks[i] = img.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 125), Vector2.one * (3 + (float)r.NextDouble() * 6));
                float a = (float)(r.NextDouble() * Math.PI * 2);
                _speckDir[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (140 + (float)r.NextDouble() * 120);
            }

            _ko = Layer(_company, "company_ko", new Vector2(0, -82), new Vector2(677, 104), Mat(false, 0f));
            _en = Layer(_company, "company_en", new Vector2(0, -172), new Vector2(595, 44), Mat());
            var fade = FadeSprite();
            _lineL = UIFactory.Image(_company, UIFactory.Gold, fade, "LineL");
            _lineL.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-322, -172), new Vector2(0, 2), new Vector2(1, 0.5f));
            _lineL.rectTransform.localScale = new Vector3(-1, 1, 1);   // 바깥쪽으로 옅어지게
            _lineL.rectTransform.pivot = new Vector2(0, 0.5f);
            _lineR = UIFactory.Image(_company, UIFactory.Gold, fade, "LineR");
            _lineR.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(322, -172), new Vector2(0, 2), new Vector2(0, 0.5f));
        }

        void BuildTitle()
        {
            _title = UIFactory.Node("Title", _stage).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            _world = UIFactory.Node("World", _title).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
            var full = new Vector2(1920, 1080);
            _sky = Layer(_world, "bg_sky", Vector2.zero, full);
            _moon = Layer(_world, "bg_moon", P(1344, 254), new Vector2(480, 480));
            _wisps = Layer(_world, "bg_wisps", P(960, 278), new Vector2(1920, 480), null, null, TextureWrapMode.Repeat);
            _wisps.uvRect = new Rect(0, 0, 1920f / 2048f, 1);
            _far = Layer(_world, "bg_far", Vector2.zero, full);
            _water = Layer(_world, "bg_water", Vector2.zero, full);
            _pathA = Layer(_world, "bg_moonpath", P(1344, 883), new Vector2(630, 394), Mat(true));
            _pathB = Layer(_world, "bg_moonpath", P(1344, 883), new Vector2(630, 394), Mat(true));
            _pathB.uvRect = new Rect(1, 0, -1, 1);   // 좌우를 뒤집은 것과 번갈아 → 반짝임
            _mistA = Layer(_world, "bg_mist", P(960, 668), new Vector2(1920, 360), null, null, TextureWrapMode.Repeat);
            _near = Layer(_world, "bg_near", Vector2.zero, full);
            _boatRefl = Layer(_world, "bg_boat_refl", P(1267, 870), new Vector2(640, 336), null, new Vector2(0.5f, 1f));
            _boat = Layer(_world, "bg_boat", P(1267, 864), new Vector2(640, 400), null, new Vector2(0.5f, 70f / 400f));
            _mistB = Layer(_world, "bg_mist", P(960, 720), new Vector2(1920, 300), null, null, TextureWrapMode.Repeat);
            _mistB.color = new Color(1, 1, 1, 0.45f);

            // 바람에 날리는 꽃잎 (청풍)
            var petalTex = Tex("petal");
            var petalSprite = petalTex != null ? Own(Sprite.Create(petalTex, new Rect(0, 0, petalTex.width, petalTex.height), new Vector2(0.5f, 0.5f))) : UIFactory.Circle;
            _petals = new Petal[22];
            var r = new System.Random(11);
            for (int i = 0; i < _petals.Length; i++)
            {
                var rt = UIFactory.Node("Petal", _world);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = petalSprite;
                img.raycastTarget = false;
                float depth = 0.4f + (float)r.NextDouble() * 0.6f;
                _petals[i] = new Petal
                {
                    Rt = rt, Img = img, Depth = depth,
                    X = -1100f + (float)r.NextDouble() * 2200f,
                    Y = -250f + (float)r.NextDouble() * 650f,
                    Vx = (50f + (float)r.NextDouble() * 90f) * depth,
                    Phase = (float)r.NextDouble() * 10f,
                    Spin = 40f + (float)r.NextDouble() * 120f,
                    Size = (9f + (float)r.NextDouble() * 12f) * depth,
                };
                img.color = Color.Lerp(new Color(0.95f, 0.86f, 0.88f), new Color(0.98f, 0.96f, 0.9f), (float)r.NextDouble());
            }
            _reeds = Layer(_world, "bg_reeds", P(1695, 1100), new Vector2(630, 364), null, new Vector2(0.5f, 0f));

            _vignette = UIFactory.Image(_title, Color.black, RadialVignette(), "Vignette");
            _vignette.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, full * 1.12f);

            _front = UIFactory.Node("Front", _title).At(new Vector2(0.5f, 0.5f), Vector2.zero, full);
            _titleGlow = Layer(_front, "title_glow", P(576, 389), new Vector2(900, 430));
            _titleImg = Layer(_front, "title", P(576, 389), new Vector2(900, 430), Mat(false, 0f));
            _titleImg.material.SetFloat("_Soft", 0.06f);
            _titleImg.material.SetFloat("_Ragged", 0.05f);
            _titleEn = Layer(_front, "title_en", P(576, 595), new Vector2(705, 61), Mat());
            _titleTag = Layer(_front, "title_tag", P(576, 650), new Vector2(473, 51));
            _copyright = Layer(_front, "copyright", new Vector2(0, -505), new Vector2(370, 29));

            _prompt = UIFactory.Text(_front, "아무 키나 눌러 시작", 30, new Color(0.95f, 0.92f, 0.84f), TextAnchor.MiddleCenter);
            _prompt.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(800, 60));
            _prompt.characterSpacing = 12f;

            var ver = UIFactory.Text(_front, "v" + Application.version, 18, new Color(0.6f, 0.66f, 0.72f, 0.7f), TextAnchor.LowerRight);
            ver.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(930, -515), new Vector2(300, 30), new Vector2(1, 0));
        }

        /// <summary>끝으로 갈수록 옅어지는 가로 띠 (금빛 선).</summary>
        Sprite FadeSprite()
        {
            var tex = new Texture2D(128, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[128 * 4];
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 128; x++)
                px[y * 128 + x] = new Color(1, 1, 1, Mathf.Pow(1f - x / 127f, 1.4f));
            tex.SetPixels32(px);
            tex.Apply();
            return Own(Sprite.Create(Own(tex), new Rect(0, 0, 128, 4), new Vector2(0.5f, 0.5f)));
        }

        Sprite RadialVignette()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(u * u * 0.8f + v * v);
                px[y * S + x] = new Color(0, 0, 0, Mathf.Clamp01((d - 0.55f) / 0.7f) * 0.85f);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Own(Sprite.Create(Own(tex), new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f)));
        }

        T Own<T>(T o) where T : UnityEngine.Object
        {
            _owned.Add(o);
            return o;
        }

        // ------------------------------------------------------------ 진행

        static bool AnyInput() => Input.anyKeyDown;

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);   // 첫 프레임처럼 오래 걸린 프레임에 연출이 건너뛰지 않게
            _clock += dt;
            FitStage();

            switch (Current)
            {
                case Stage.Loading:
                    _curtain.color = Color.black;
                    if (_audioTask.IsCompleted || _clock > 3f)
                    {
                        if (_audioTask.IsCompleted && !_audioTask.IsFaulted)
                        {
                            var b = _audioTask.Result;
                            _stamp = b.Stamp.ToClip(); _chime = b.Chime.ToClip(); _brush = b.Brush.ToClip();
                            _gong = b.Gong.ToClip(); _theme = b.Theme.ToClip();
                        }
                        Current = Stage.Company;
                        _t = 0f;
                    }
                    break;

                case Stage.Company:
                    _t += dt;
                    if (AnyInput() && _t < CompanyFadeOutAt && _t > 0.2f) _t = CompanyFadeOutAt + 0.3f;   // 건너뛰기: 빨리 어두워진다
                    CompanyPose(_t);
                    if (_t >= CompanyEnd) StartTitle();
                    break;

                case Stage.Title:
                    _t += dt;
                    if (AnyInput())
                    {
                        if (_t < TitleReadyAt)
                        {
                            // 글씨가 써지는 중이면 다 쓴 상태로 (붓 소리는 끊고 징은 건너뛴다)
                            _t = TitleReadyAt;
                            _fired.Add("brush");
                            _fired.Add("gong");
                            _sfx.Stop();
                        }
                        else Continue();
                    }
                    TitlePose(_t);
                    break;

                case Stage.Leaving:
                    _t += dt;
                    TitlePose(TitleReadyAt + 1f + _t);
                    _curtain.color = new Color(0, 0, 0, Smooth(0f, LeaveTime, _t));
                    _music.volume = Mathf.Lerp(_musicTarget, 0f, Mathf.Clamp01(_t / LeaveTime));
                    if (_t >= LeaveTime)
                    {
                        _music.Stop();
                        _stage.gameObject.SetActive(false);
                        Current = Stage.Revealing;
                        _t = 0f;
                        _onFinished?.Invoke();
                    }
                    break;

                case Stage.Revealing:
                    _t += dt;
                    _curtain.raycastTarget = false;
                    _curtain.color = new Color(0, 0, 0, 1f - Smooth(0.1f, RevealTime, _t));
                    if (_t >= RevealTime) Destroy(gameObject);
                    break;
            }
        }

        /// <summary>타이틀에서 게임으로 (아무 키와 같다). 자동 촬영도 이걸 부른다.</summary>
        public void Continue()
        {
            if (Current != Stage.Title) return;
            if (_t < TitleReadyAt) _t = TitleReadyAt;
            Fire("start", _chime, 0.7f);
            _musicTarget = _music.volume;
            Current = Stage.Leaving;
            _t = 0f;
        }

        void StartTitle()
        {
            _company.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            Current = Stage.Title;
            _t = 0f;
            if (_theme != null)
            {
                _music.clip = _theme;
                _music.Play();
            }
        }

        void FitStage()
        {
            var size = ((RectTransform)_canvas.transform).rect.size;
            float cover = Mathf.Max(size.x / 1920f, size.y / 1080f);
            _stage.localScale = new Vector3(cover, cover, 1f);
        }

        void Fire(string key, AudioClip clip, float volume)
        {
            if (clip == null || !_fired.Add(key)) return;
            _sfx.PlayOneShot(clip, volume * Settings.EffectsVolume);
        }

        static float Smooth(float a, float b, float t)
        {
            float x = Mathf.Clamp01((t - a) / (b - a));
            return x * x * (3f - 2f * x);
        }

        static float EaseOutBack(float x)
        {
            const float c = 1.7f;
            x -= 1f;
            return 1f + (c + 1f) * x * x * x + c * x * x;
        }

        static void Alpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        // ------------------------------------------------------------ 사업자 화면

        void CompanyPose(float t)
        {
            float curtain = Mathf.Max(1f - Smooth(0f, 0.9f, t), Smooth(CompanyFadeOutAt, CompanyEnd - 0.1f, t));
            _curtain.color = new Color(0, 0, 0, curtain);
            _paper.rectTransform.localScale = Vector3.one * (1f + t * 0.006f);

            // 낙관: 위에서 내려찍는다 (커졌다가 쾅)
            const float StampStart = 0.45f, StampHit = 0.7f;
            float p = Mathf.Clamp01((t - StampStart) / (StampHit - StampStart));
            float scale = Mathf.Lerp(2.1f, 1f, p * p);
            float after = t - StampHit;
            if (after > 0f) scale = 1f - 0.05f * Mathf.Exp(-after * 9f) * Mathf.Cos(after * 30f);
            _seal.rectTransform.localScale = Vector3.one * scale;
            _seal.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-12f, -3f, p));
            Alpha(_seal, Mathf.Clamp01(p * 1.6f));
            if (after >= 0f) Fire("stamp", _stamp, 1f);

            // 쾅 할 때 흔들림, 붉은 빛, 인주 튀김
            float shake = after > 0f ? Mathf.Exp(-after * 14f) * 9f : 0f;
            _company.anchoredPosition = new Vector2(Mathf.Sin(t * 91f) * shake, Mathf.Cos(t * 77f) * shake);
            Alpha(_sealGlow, after > 0f ? 0.75f * Mathf.Exp(-after * 2.2f) : 0f);
            _sealGlow.rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0f, after) * 0.25f);
            _sealGlow.rectTransform.localEulerAngles = _seal.rectTransform.localEulerAngles;
            for (int i = 0; i < _specks.Length; i++)
            {
                float k = after > 0f ? 1f - Mathf.Exp(-after * 6f) : 0f;
                _specks[i].anchoredPosition = new Vector2(0, 125) + _speckDir[i] * k;
                Alpha(_specks[i].GetComponent<Image>(), after > 0f ? 0.8f * Mathf.Exp(-after * 3f) : 0f);
            }

            // 한글 이름: 붓으로 쓰듯 왼쪽부터
            float ko = Smooth(1.1f, 2.3f, t);
            if (_ko.material != null && _ko.material.shader == _shader) _ko.material.SetFloat("_Progress", Mathf.Clamp01((t - 1.1f) / 1.2f));
            else Alpha(_ko, ko);
            _ko.rectTransform.anchoredPosition = new Vector2(0, -82 - (1f - ko) * 8f);
            if (t >= 1.1f) Fire("ko", _brush, 0.35f);

            // 금빛 선이 펼쳐지고 영어 이름
            float line = EaseOutCubic(Mathf.Clamp01((t - 2.0f) / 0.8f));
            _lineL.rectTransform.sizeDelta = new Vector2(150f * line, 2f);
            _lineR.rectTransform.sizeDelta = new Vector2(150f * line, 2f);
            float en = Smooth(2.05f, 2.9f, t);
            Alpha(_en, en);
            Alpha(_lineL, en * 0.9f);
            Alpha(_lineR, en * 0.9f);
            _en.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.04f, 1f, en);
            if (t >= 2.05f) Fire("en", _chime, 0.45f);

            // 빛줄기가 한 번 지나간다
            float shine = Mathf.Lerp(-0.4f, 1.4f, Mathf.Clamp01((t - 2.9f) / 1.1f));
            SetShine(_ko, shine);
            SetShine(_seal, Mathf.Lerp(-0.4f, 1.4f, Mathf.Clamp01((t - 2.75f) / 0.9f)));
        }

        static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        void SetShine(RawImage img, float pos)
        {
            if (img.material != null && img.material.HasProperty("_Shine")) img.material.SetFloat("_Shine", pos);
        }

        // ------------------------------------------------------------ 타이틀 화면

        void TitlePose(float t)
        {
            if (Current == Stage.Title) _curtain.color = new Color(0, 0, 0, 1f - Smooth(0f, 2.2f, t));
            float time = _clock;

            // 음악은 천천히 들어온다
            if (Current == Stage.Title) _music.volume = Smooth(0f, 3f, t) * 0.8f * Settings.EffectsVolume;

            // 카메라가 천천히 물러나는 느낌 + 마우스 시차
            var m = new Vector2(Input.mousePosition.x / Mathf.Max(1, Screen.width) - 0.5f, Input.mousePosition.y / Mathf.Max(1, Screen.height) - 0.5f);
            if (Screen.width <= 0 || !Application.isFocused) m = Vector2.zero;
            _mouse = Vector2.Lerp(_mouse, new Vector2(Mathf.Clamp(m.x, -0.5f, 0.5f), Mathf.Clamp(m.y, -0.5f, 0.5f)), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 2.5f));
            float dolly = Mathf.Lerp(1.09f, 1.0f, EaseOutCubic(Mathf.Clamp01(t / 11f)));
            _world.localScale = Vector3.one * (1.06f * dolly);
            var drift = new Vector2(Mathf.Sin(time * 0.07f) * 10f, Mathf.Sin(time * 0.05f + 1f) * 4f);
            Vector2 Par(float depth) => -(_mouse * new Vector2(36f, 16f) + drift) * depth;

            _sky.rectTransform.anchoredPosition = Par(0.1f);
            _moon.rectTransform.anchoredPosition = P(1344, 254) + Par(0.12f);
            float breathe = 0.5f + 0.5f * Mathf.Sin(time * 0.6f);
            Alpha(_moon, 0.93f + breathe * 0.07f);
            _moon.rectTransform.localScale = Vector3.one * (1f + breathe * 0.01f);
            _wisps.rectTransform.anchoredPosition = P(960, 278) + Par(0.18f);
            _wisps.uvRect = new Rect(time * 0.004f, 0, 1920f / 2048f, 1);
            _far.rectTransform.anchoredPosition = Par(0.3f);
            _water.rectTransform.anchoredPosition = Par(0.45f);
            var pathPos = P(1344, 883) + Par(0.45f);
            _pathA.rectTransform.anchoredPosition = pathPos;
            _pathB.rectTransform.anchoredPosition = pathPos;
            float flick = 0.5f + 0.5f * Mathf.Sin(time * 2.3f) * Mathf.Sin(time * 1.37f + 0.6f);
            Alpha(_pathA, 0.85f * (0.55f + 0.45f * flick));
            Alpha(_pathB, 0.85f * (0.55f + 0.45f * (1f - flick)));
            _pathA.rectTransform.localScale = new Vector3(1f + Mathf.Sin(time * 0.8f) * 0.03f, 1f, 1f);
            _mistA.rectTransform.anchoredPosition = P(960, 668) + Par(0.5f);
            _mistA.uvRect = new Rect(time * 0.006f, 0, 0.9375f, 1);
            Alpha(_mistA, 0.85f + 0.15f * Mathf.Sin(time * 0.3f));
            _near.rectTransform.anchoredPosition = Par(0.65f);

            // 배: 물결에 흔들린다
            float bob = Mathf.Sin(time * 0.9f);
            _boat.rectTransform.anchoredPosition = P(1267, 864) + Par(0.6f) + new Vector2(Mathf.Sin(time * 0.05f) * 14f, bob * 2.5f);
            _boat.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(time * 0.9f + 0.7f) * 1.4f);
            _boatRefl.rectTransform.anchoredPosition = _boat.rectTransform.anchoredPosition + new Vector2(0, -6f - bob * 2.5f);
            _boatRefl.rectTransform.localScale = new Vector3(1f + Mathf.Sin(time * 1.7f) * 0.01f, 1f + Mathf.Sin(time * 1.3f) * 0.05f, 1f);
            _boatRefl.rectTransform.localEulerAngles = new Vector3(0, 0, -_boat.rectTransform.localEulerAngles.z);
            _mistB.rectTransform.anchoredPosition = P(960, 720) + Par(0.7f);
            _mistB.uvRect = new Rect(0.5f - time * 0.011f, 0, 0.9375f, 1);

            // 꽃잎 (바람)
            float gust = 1f + 0.6f * Mathf.Max(0f, Mathf.Sin(time * 0.35f));
            for (int i = 0; i < _petals.Length; i++)
            {
                ref var pt = ref _petals[i];
                pt.X += pt.Vx * gust * Time.unscaledDeltaTime;
                if (pt.X > 1100f) { pt.X -= 2200f; pt.Y = -250f + Mathf.Repeat(pt.Y * 7.3f + 211f, 650f); }
                float y = pt.Y + Mathf.Sin(time * 0.8f + pt.Phase) * 30f * pt.Depth;
                pt.Rt.anchoredPosition = new Vector2(pt.X, y) + Par(pt.Depth);
                pt.Rt.sizeDelta = new Vector2(pt.Size * 1.4f, pt.Size);
                pt.Rt.localEulerAngles = new Vector3(0, 0, time * pt.Spin + pt.Phase * 40f);
                pt.Rt.localScale = new Vector3(1f, Mathf.Sin(time * 2.2f + pt.Phase), 1f);   // 뒤집히며 날린다
                Alpha(pt.Img, (0.35f + 0.5f * pt.Depth) * Smooth(0.5f, 2.5f, t));
            }

            _reeds.rectTransform.anchoredPosition = P(1695, 1100) + Par(1f);
            _reeds.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(time * 0.7f) * 0.9f * gust + Mathf.Sin(time * 1.9f) * 0.3f);

            // 글씨
            _front.anchoredPosition = Par(0.15f);
            float write = Mathf.Clamp01((t - 1.6f) / 1.9f);
            if (_titleImg.material != null && _titleImg.material.shader == _shader) _titleImg.material.SetFloat("_Progress", write);
            else Alpha(_titleImg, write);
            if (t >= 1.6f) Fire("brush", _brush, 0.6f);
            float glow = Smooth(2.6f, 4.4f, t);
            Alpha(_titleGlow, glow * (0.5f + 0.18f * Mathf.Sin(time * 0.9f)));
            _titleImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.015f, 1f, Smooth(1.6f, 4f, t));
            SetShine(_titleImg, Mathf.Lerp(-0.4f, 1.4f, Mathf.Repeat((t - 6.5f) / 9f, 1f) * 4.5f - 1.75f));

            // 붓글씨를 다 쓰면 징 (예전엔 여기서 "짭" 낙관을 찍었다 — 사용자 요청으로 뺌)
            if (t >= 3.72f) Fire("gong", _gong, 0.9f);

            float en = Smooth(3.9f, 4.8f, t);
            Alpha(_titleEn, en);
            _titleEn.rectTransform.anchoredPosition = P(576, 595) + new Vector2(0, -(1f - en) * 10f);
            SetShine(_titleEn, Mathf.Lerp(-0.4f, 1.4f, Mathf.Clamp01((t - 4.6f) / 1.2f)));
            float tag = Smooth(4.4f, 5.3f, t);
            Alpha(_titleTag, tag * 0.92f);
            _titleTag.rectTransform.anchoredPosition = P(576, 650) + new Vector2(0, -(1f - tag) * 8f);
            Alpha(_copyright, Smooth(5f, 6f, t) * 0.85f);

            float prompt = Smooth(TitleReadyAt, TitleReadyAt + 0.6f, t);
            float blink = Current == Stage.Leaving ? 1f : 0.7f + 0.3f * Mathf.Cos((t - TitleReadyAt) * 2.6f);
            _prompt.alpha = prompt * blink;
        }
    }
}
