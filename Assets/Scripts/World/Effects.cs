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

        public static void Ripple(Vector3 pos, float size = 1.5f, float duration = 1.4f)
        {
            var go = Mats.Prim(PrimitiveType.Cylinder, "Ripple", null,
                new Vector3(pos.x, HanRiverMap.WaterY + 0.02f, pos.z), new Vector3(0.2f, 0.002f, 0.2f),
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
