using System;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>게임 시간 3~7시간마다 날씨가 바뀐다. 비가 오면 카메라 주변에 빗방울을 뿌린다.</summary>
    public class WeatherSystem : MonoBehaviour
    {
        public Weather Current { get; private set; } = Weather.Clear;

        /// <summary>바람이 불어 가는 방향 (도, 0 = +Z). 게임 시각에 따라 천천히 돈다.</summary>
        public float WindYaw => Mathf.PerlinNoise(Game.Clock.TotalMinutes / 900f, 3.7f) * 540f;
        /// <summary>바람 세기 (m/s). 비 오면 세다.</summary>
        public float WindSpeed => 2f + Mathf.PerlinNoise(Game.Clock.TotalMinutes / 400f, 8.1f) * 5f + (Current == Weather.Rain ? 4f : Current == Weather.Cloudy ? 1.5f : 0f);
        /// <summary>파도 높이 배율: 바다가 강보다, 바람·비가 셀수록 높다.</summary>
        public float WaveFactor => (Fishing.World.FieldMap.IsSea ? 1f : 0.45f) * (0.7f + WindSpeed / 8f + (Current == Weather.Rain ? 0.4f : 0f));
        public event Action Changed;

        float _nextChangeAt;
        ParticleSystem _rain;

        void Start()
        {
            BuildRain();
            _nextChangeAt = Game.Clock.TotalMinutes + UnityEngine.Random.Range(180f, 420f);
            Apply();
        }

        void Update()
        {
            if (Game.Clock.TotalMinutes >= _nextChangeAt) Roll();
            Apply(); // 대기실 실내에서는 비를 끈다

            var cam = Game.CameraRig != null ? Game.CameraRig.Cam : null;
            if (cam != null) _rain.transform.position = cam.transform.position + Vector3.up * 14f + cam.transform.forward * 6f;
        }

        void Roll()
        {
            float r = UnityEngine.Random.value;
            var next = r < 0.55f ? Weather.Clear : r < 0.82f ? Weather.Cloudy : Weather.Rain;
            _nextChangeAt = Game.Clock.TotalMinutes + UnityEngine.Random.Range(180f, 420f);
            if (next == Current) return;
            Force(next);
            Game.Toast($"날씨가 바뀌었습니다: {Names.Of(Current)}");
        }

        public void Force(Weather weather)
        {
            Current = weather;
            Apply();
            Changed?.Invoke();
        }

        void Apply()
        {
            if (_rain == null) return;
            var emission = _rain.emission;
            bool snow = Seasons.IsWinter;
            if (snow != _snowMode) SetSnow(snow);
            emission.rateOverTime = Current == Weather.Rain && Game.Mode == GameMode.Field ? (snow ? 900f : 1800f) : 0f;
        }

        bool _snowMode;

        /// <summary>겨울이면 빗줄기 대신 천천히 날리는 눈송이.</summary>
        void SetSnow(bool snow)
        {
            _snowMode = snow;
            var main = _rain.main;
            main.startLifetime = snow ? 7f : 1.1f;
            main.startSpeed = snow ? 2.2f : 20f;
            main.startSize = snow ? 0.07f : 0.018f;
            main.startColor = snow ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.75f, 0.82f, 0.9f, 0.55f);
            var r = _rain.GetComponent<ParticleSystemRenderer>();
            r.renderMode = snow ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            var noise = _rain.noise;
            noise.enabled = snow;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
        }

        void BuildRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            _rain = go.AddComponent<ParticleSystem>();
            _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _rain.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 20f;
            main.startSize = 0.018f;
            main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.75f, 0.82f, 0.9f, 0.55f);

            var shape = _rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(45f, 45f, 1f); // 회전 후 로컬 Z가 아래를 향하므로 Z가 두께
            shape.rotation = new Vector3(90f, 0f, 0f);

            var emission = _rain.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3f;
            renderer.velocityScale = 0.02f;
            renderer.sharedMaterial = Mats.Particle(new Color(0.8f, 0.85f, 0.95f, 0.5f));

            _rain.Play();
        }
    }
}
