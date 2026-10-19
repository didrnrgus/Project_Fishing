using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>물보라/파문 효과. 월드 공간 파티클 하나를 공유해서 Emit한다.</summary>
    public static class Effects
    {
        static ParticleSystem _splash;

        static ParticleSystem SplashSystem
        {
            get
            {
                if (_splash != null) return _splash;
                var go = new GameObject("SplashFx");
                _splash = go.AddComponent<ParticleSystem>();
                _splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _splash.main;
                main.loop = false;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 1.6f;
                main.startLifetime = 0.9f;
                main.startSize = 0.12f;
                main.maxParticles = 2000;
                var emission = _splash.emission;
                emission.rateOverTime = 0f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = Mats.Particle(new Color(0.9f, 0.95f, 1f, 0.75f));
                return _splash;
            }
        }

        public static void Splash(Vector3 pos, int count, float strength = 1f)
        {
            var ps = SplashSystem;
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var dir = Random.insideUnitCircle * 1.6f;
                p.position = pos + new Vector3(dir.x * 0.15f, 0.05f, dir.y * 0.15f);
                p.velocity = new Vector3(dir.x, Random.Range(2.5f, 5f) * strength, dir.y);
                p.startSize = Random.Range(0.06f, 0.16f) * Mathf.Sqrt(strength);
                p.startLifetime = Random.Range(0.5f, 1f);
                ps.Emit(p, 1);
            }
        }

        static ParticleSystem _spray;

        /// <summary>파이팅 물보라: 크고 부드러운 흰 물방울 덩어리. 커지면서 옅어지고, 천천히 떨어진다.</summary>
        static ParticleSystem SpraySystem
        {
            get
            {
                if (_spray != null) return _spray;
                var go = new GameObject("FightSprayFx");
                _spray = go.AddComponent<ParticleSystem>();
                _spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _spray.main;
                main.loop = false;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 0.9f;
                main.maxParticles = 3000;
                var emission = _spray.emission;
                emission.rateOverTime = 0f;
                var size = _spray.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.35f, 1f), new Keyframe(1f, 1.5f)));
                var col = _spray.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.75f, 0.45f), new GradientAlphaKey(0f, 1f) });
                col.color = grad;
                var rot = _spray.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = Mats.Particle(new Color(0.46f, 0.48f, 0.5f, 0.3f)); // 레거시 파티클 셰이더는 색을 2배로 곱한다
                return _spray;
            }
        }

        /// <summary>
        /// 원작 1인칭 파이팅의 물보라 (2013 홍현 영상 0:44~): 물고기 자리에서 하얀 물보라가 돔처럼 솟고 바닥에 거품 고리.
        /// FightRodView가 0.07~0.16초마다 부른다. intensity 0.35(지침) ~ 1.4(폭주).
        /// </summary>
        public static void FightSpray(Vector3 pos, float intensity)
        {
            var ps = SpraySystem;
            var p = new ParticleSystem.EmitParams();
            int count = Mathf.RoundToInt(7 + 12 * intensity);
            for (int i = 0; i < count; i++)
            {
                var dir = Random.insideUnitCircle;
                float up = Random.value;
                p.position = pos + new Vector3(dir.x * 0.35f, 0.1f, dir.y * 0.35f);
                // 가운데는 높이, 가장자리는 옆으로 퍼진다 (돔 모양)
                p.velocity = new Vector3(dir.x * Mathf.Lerp(2.6f, 0.6f, up), Mathf.Lerp(1.2f, 5.2f, up) * (0.6f + intensity * 0.5f), dir.y * Mathf.Lerp(2.6f, 0.6f, up));
                p.startSize = Random.Range(0.18f, 0.6f) * (0.7f + intensity * 0.35f);
                p.startLifetime = Random.Range(0.55f, 1.05f);
                p.rotation = Random.Range(0f, 360f);
                ps.Emit(p, 1);
            }
            // 바닥의 낮고 넓은 물안개
            for (int i = 0; i < 2; i++)
            {
                var dir = Random.insideUnitCircle;
                p.position = pos + new Vector3(dir.x * 0.6f, 0.15f, dir.y * 0.6f);
                p.velocity = new Vector3(dir.x * 1.2f, 0.6f, dir.y * 1.2f);
                p.startSize = Random.Range(1.1f, 1.9f) * (0.7f + intensity * 0.3f);
                p.startLifetime = Random.Range(0.7f, 1.1f);
                ps.Emit(p, 1);
            }
            Splash(pos, Mathf.RoundToInt(3 + 5 * intensity), 0.8f + intensity * 0.5f);
        }

        /// <summary>랜딩 성공·결과 화면의 큰 물보라 (한 번에 크게 터진다).</summary>
        public static void BigSplash(Vector3 pos, float scale = 1f)
        {
            var ps = SpraySystem;
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < 70; i++)
            {
                var dir = Random.insideUnitCircle;
                float up = Random.value;
                p.position = pos + new Vector3(dir.x * 0.4f, 0.1f, dir.y * 0.4f) * scale;
                p.velocity = new Vector3(dir.x * Mathf.Lerp(4.5f, 1f, up), Mathf.Lerp(2f, 8f, up), dir.y * Mathf.Lerp(4.5f, 1f, up)) * Mathf.Sqrt(scale);
                p.startSize = Random.Range(0.4f, 1.1f) * scale;
                p.startLifetime = Random.Range(0.7f, 1.4f);
                p.rotation = Random.Range(0f, 360f);
                ps.Emit(p, 1);
            }
            Splash(pos, 60, 1.8f * scale);
            Ripple(pos, 4f * scale, 1.6f);
        }

        static ParticleSystem _sparkle;

        /// <summary>반짝이는 작은 빛 (중력 없음, 짧게 커졌다 사라진다). 조준 궤적의 별 파티클.</summary>
        static ParticleSystem SparkleSystem
        {
            get
            {
                if (_sparkle != null) return _sparkle;
                var go = new GameObject("SparkleFx");
                _sparkle = go.AddComponent<ParticleSystem>();
                _sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _sparkle.main;
                main.loop = false;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 0f;
                main.maxParticles = 600;
                var emission = _sparkle.emission;
                emission.rateOverTime = 0f;
                var size = _sparkle.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = Mats.Particle(new Color(1f, 1f, 1f, 0.9f));
                return _sparkle;
            }
        }

        /// <summary>원작 조준선: 캐릭터 → 목표 지점 곡선을 따라가는 흰 별 파티클.</summary>
        public static void Sparkle(Vector3 pos, float size = 0.22f, float life = 0.35f)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = Random.insideUnitSphere * 0.15f + Vector3.up * 0.1f,
                startSize = size * Random.Range(0.7f, 1.3f),
                startLifetime = life,
                startColor = Color.Lerp(Color.white, new Color(0.6f, 0.95f, 1f), Random.value * 0.5f),
            };
            SparkleSystem.Emit(p, 1);
        }

        public static void Ripple(Vector3 pos, float size = 1.5f, float duration = 1.4f)
        {
            var go = Mats.Prim(PrimitiveType.Cylinder, "Ripple", null,
                new Vector3(pos.x, FieldMap.WaterY + 0.02f, pos.z), new Vector3(0.2f, 0.002f, 0.2f),
                Mats.NewRipple(), collider: false);
            go.AddComponent<RippleFx>().Init(size, duration);
        }
    }

    public class RippleFx : MonoBehaviour
    {
        float _size, _duration, _t;
        Material _mat;

        public void Init(float size, float duration)
        {
            _size = size;
            _duration = duration;
            _mat = GetComponent<Renderer>().sharedMaterial; // Ripple()에서 인스턴스마다 새로 만든 머티리얼
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / _duration;
            float s = Mathf.Lerp(0.2f, _size, Mathf.Sqrt(k));
            transform.localScale = new Vector3(s, 0.002f, s);
            _mat.SetColor("_TintColor", new Color(0.85f, 0.92f, 1f, Mathf.Lerp(0.35f, 0f, k)));
            if (k >= 1f) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}
