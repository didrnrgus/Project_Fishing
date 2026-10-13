using Fishing.Data;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Fishing.Core
{
    /// <summary>
    /// 화면 후처리 (Post Processing Stack v2, Built-in 파이프라인). docs/08-quality-plan.md 5장.
    ///  - 블룸: 노을·가로등·다리 조명·별모래가 번진다 (밤에 조금 세게)
    ///  - 컬러 그레이딩: Neutral 톤매핑(ACES는 노을·밤 하늘이 검게 뭉개짐), 시간대별 색온도·채도 (새벽 푸르스름 → 낮 선명 → 노을 따뜻 → 밤 차갑고 차분), 비 오면 채도 낮게
    ///  - AO(MSVO), 비네트 약하게, SMAA
    /// 카메라에 PostProcessLayer, 전역 볼륨 하나(코드로 만든 프로필). 옵션 창에서 끌 수 있다(<see cref="Enabled"/>).
    /// </summary>
    public class PostFx : MonoBehaviour
    {
        PostProcessLayer _layer;
        PostProcessVolume _volume;
        Bloom _bloom;
        ColorGrading _grading;
        AmbientOcclusion _ao;
        Vignette _vignette;
        float _rain;

        public static PostFx Instance { get; private set; }

        /// <summary>후처리 켜기/끄기 (옵션).</summary>
        public bool Enabled
        {
            get => _layer != null && _layer.enabled;
            set
            {
                if (_layer != null) _layer.enabled = value;
                if (_volume != null) _volume.enabled = value;
            }
        }

        public static PostFx Attach(Camera cam)
        {
            var cfg = Resources.Load<PostFxConfig>("PostFxConfig");
            if (cfg == null || cfg.resources == null)
            {
                Debug.LogWarning("[PostFx] Resources/PostFxConfig가 없어 후처리를 끕니다 (메뉴 Fishing/Setup Project).");
                return null;
            }
            var fx = cam.gameObject.AddComponent<PostFx>();
            fx.Build(cam, cfg.resources);
            Instance = fx;
            return fx;
        }

        void Build(Camera cam, PostProcessResources res)
        {
            cam.allowHDR = true;
            _layer = cam.gameObject.AddComponent<PostProcessLayer>();
            _layer.Init(res);
            _layer.volumeTrigger = cam.transform;
            _layer.volumeLayer = 1 << gameObject.layer;
            _layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
            _layer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.Medium;

            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            _bloom = profile.AddSettings<Bloom>();
            _bloom.enabled.Override(true);
            _bloom.intensity.Override(1.1f);
            _bloom.threshold.Override(1.05f);
            _bloom.softKnee.Override(0.6f);
            _bloom.diffusion.Override(7f);

            _grading = profile.AddSettings<ColorGrading>();
            _grading.enabled.Override(true);
            _grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            _grading.tonemapper.Override(Tonemapper.Neutral); // ACES는 노을·밤의 어두운 하늘을 검게 눌러서 Neutral
            _grading.postExposure.Override(0.2f);
            _grading.lift.Override(Vector4.zero);
            _grading.temperature.Override(0f);
            _grading.saturation.Override(8f);
            _grading.contrast.Override(6f);

            _ao = profile.AddSettings<AmbientOcclusion>();
            _ao.enabled.Override(true);
            _ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
            _ao.intensity.Override(0.55f);
            _ao.thicknessModifier.Override(1.2f);

            _vignette = profile.AddSettings<Vignette>();
            _vignette.enabled.Override(true);
            _vignette.intensity.Override(0.22f);
            _vignette.smoothness.Override(0.45f);

            var volGo = new GameObject("PostFxVolume") { layer = gameObject.layer };
            volGo.transform.SetParent(transform, false);
            _volume = volGo.AddComponent<PostProcessVolume>();
            _volume.isGlobal = true;
            _volume.priority = 1;
            _volume.sharedProfile = profile;
        }

        // 시간대별 (분, 색온도, 채도, 노출, 블룸) — 그 사이는 섞는다
        static readonly (float min, float temp, float sat, float exp, float bloom)[] Keys =
        {
            (0, -18f, -12f, 0.45f, 1.6f),     // 한밤: 차갑고 차분, 불빛 번짐
            (4 * 60, -14f, -8f, 0.4f, 1.4f),    // 새벽: 푸르스름
            (6 * 60, -6f, 0f, 0.3f, 1.2f),      // 해 뜰 녘
            (8 * 60, 4f, 8f, 0.2f, 1.0f),       // 아침
            (12 * 60, 0f, 10f, 0.15f, 0.9f),    // 한낮: 선명
            (16 * 60, 8f, 10f, 0.2f, 1.0f),
            (18 * 60, 18f, 12f, 0.35f, 1.35f),  // 노을: 따뜻
            (19 * 60 + 30, 4f, 0f, 0.45f, 1.5f),
            (21 * 60, -14f, -10f, 0.45f, 1.6f),
            (24 * 60, -18f, -12f, 0.45f, 1.6f),
        };

        void Update()
        {
            if (_grading == null || Game.Clock == null) return;
            float m = Game.Clock.Minutes;
            int i = 0;
            while (i < Keys.Length - 2 && m >= Keys[i + 1].min) i++;
            var a = Keys[i];
            var b = Keys[i + 1];
            float t = Mathf.InverseLerp(a.min, b.min, m);
            t = t * t * (3f - 2f * t);
            float rainTarget = Game.Weather != null ? (Game.Weather.Current == Weather.Rain ? 1f : Game.Weather.Current == Weather.Cloudy ? 0.45f : 0f) : 0f;
            _rain = Mathf.MoveTowards(_rain, rainTarget, Time.deltaTime * 0.3f);
            _grading.temperature.value = Mathf.Lerp(a.temp, b.temp, t) * (1f - _rain * 0.5f);
            _grading.saturation.value = Mathf.Lerp(a.sat, b.sat, t) - 22f * _rain;
            _grading.postExposure.value = Mathf.Lerp(a.exp, b.exp, t) - 0.1f * _rain;
            _bloom.intensity.value = Mathf.Lerp(a.bloom, b.bloom, t);
            // 어두울 때는 그림자를 살짝 들어 올려 실루엣이 보이게
            float dark = Mathf.Clamp01((Mathf.Lerp(a.exp, b.exp, t) - 0.15f) / 0.3f);
            _grading.lift.value = new Vector4(0f, 0.01f, 0.03f, 0.05f * dark);
            // 대기실(실내)은 조명이 따뜻하니 고정
            if (Game.Mode == GameMode.Lobby)
            {
                _grading.temperature.value = 6f;
                _grading.saturation.value = 6f;
                _grading.postExposure.value = 0.2f;
                _grading.lift.value = Vector4.zero;
                _bloom.intensity.value = 1.0f;
            }
        }
    }
}
