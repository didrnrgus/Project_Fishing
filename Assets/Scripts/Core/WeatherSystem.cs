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
            Announce();
        }

        // ---------------- 날씨 효과 (원작 2013 홍현 영상 0:22·3:06: 입장할 때와 날씨가 바뀔 때 화면 가운데 알림) ----------------

        /// <summary>챔질 판정에 더하는 찌 잠김 보너스 (맑으면 챔질이 매우 쉬워진다).</summary>
        public float HookBonus => Current == Weather.Clear ? 0.07f : 0f;
        /// <summary>음식 회복 배율 (맑으면 조금 더 빠르다).</summary>
        public float RegenFactor => Current == Weather.Clear ? 1.25f : 1f;
        /// <summary>입질 기다리는 시간 배율 (흐리거나 비 오면 입질이 잦다).</summary>
        public float WaitFactor => Current switch { Weather.Cloudy => 0.88f, Weather.Rain => 0.75f, _ => 1f };
        /// <summary>시간에 따라 줄어드는 체력 배율 (비를 맞으면 빨리 지친다).</summary>
        public float DrainFactor => Current == Weather.Rain ? 1.6f : 1f;

        /// <summary>날씨가 바뀔 때·낚시터에 들어갈 때 원작 문구 그대로 화면 가운데에 띄운다.</summary>
        public static string EffectText(Weather w) => w switch
        {
            Weather.Clear => "날씨가 쾌청해집니다. 챔질이 매우 쉬워지고 회복이 조금 더 빨라집니다.",
            Weather.Cloudy => "구름이 끼기 시작합니다. 물고기의 경계심이 줄어 입질이 조금 잦아집니다.",
            _ => Seasons.IsWinter || (Game.Mode == GameMode.Field && Fishing.World.FieldMap.Current.SnowGround)
                ? "눈이 내립니다. 입질이 잦아지지만 체력이 더 빨리 줄어듭니다."
                : "비가 내립니다. 입질이 잦아지지만 체력이 더 빨리 줄어듭니다.",
        };

        public void Announce()
        {
            if (Game.Mode != GameMode.Field) return;
            Game.Notice(EffectText(Current));
            Game.SysLog(EffectText(Current), "#9fd8ff");
        }

        public void Force(Weather weather)
        {
            Current = weather;
            // 정해 준 날씨는 적어도 3시간은 간다 (시계를 앞으로 돌려도 바로 다시 굴리지 않게)
            _nextChangeAt = Mathf.Max(_nextChangeAt, Game.Clock.TotalMinutes + 180f);
            Apply();
            Changed?.Invoke();
        }

        void Apply()
        {
            if (_rain == null) return;
            var emission = _rain.emission;
            // 겨울이거나 늘 눈 덮인 낚시터(청진포구)면 비 대신 눈
            bool snow = Seasons.IsWinter || (Game.Mode == GameMode.Field && Fishing.World.FieldMap.Current.SnowGround);
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
