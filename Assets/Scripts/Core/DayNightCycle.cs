using System.Collections.Generic;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>시계에 맞춰 해/달, 하늘, 안개, 가로등을 바꾼다.</summary>
    public class DayNightCycle : MonoBehaviour
    {
        Light _sun;
        Light _moon;
        Material _sky;
        readonly List<Light> _lamps = new List<Light>();
        readonly List<Renderer> _lampBulbs = new List<Renderer>();
        Material _bulbOn, _bulbOff;
        float _weatherDim = 1f;
        bool _lampsOn = true;

        public void Init(List<Light> lamps, List<Renderer> bulbs)
        {
            _lamps.AddRange(lamps);
            _lampBulbs.AddRange(bulbs);
            _bulbOn = Mats.Emissive(new Color(1f, 0.85f, 0.55f), 2.2f);
            _bulbOff = Mats.Lit(new Color(0.75f, 0.72f, 0.65f));

            _sun = CreateLight("Sun", new Color(1f, 0.96f, 0.88f), LightShadows.Soft);
            _moon = CreateLight("Moon", new Color(0.55f, 0.65f, 0.95f), LightShadows.None);

            _sky = new Material(Shader.Find("Skybox/Procedural"));
            _sky.SetFloat("_SunSize", 0.045f);
            _sky.SetFloat("_SunSizeConvergence", 6f);
            RenderSettings.skybox = _sky;
            RenderSettings.sun = _sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;

            SetLamps(false);
        }

        Light CreateLight(string name, Color color, LightShadows shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.shadows = shadows;
            light.shadowStrength = 0.8f;
            return light;
        }

        void Update()
        {
            if (_sun == null) return;

            float targetDim = Game.Weather.Current switch
            {
                Weather.Cloudy => 0.6f,
                Weather.Rain => 0.38f,
                _ => 1f
            };
            _weatherDim = Mathf.MoveTowards(_weatherDim, targetDim, Time.deltaTime * 0.2f);

            // 06:00에 해가 뜨고 18:00에 진다
            float sunAngle = Game.Clock.Minutes / GameClock.MinutesPerDay * 360f - 90f;
            float elevation = Mathf.Sin(sunAngle * Mathf.Deg2Rad);
            float day = Mathf.Clamp01(elevation * 3f + 0.2f);
            float warm = 1f - Mathf.Clamp01(elevation * 3.5f);

            _sun.transform.rotation = Quaternion.Euler(sunAngle, -35f, 0f);
            _sun.intensity = Mathf.Clamp01(elevation * 2.5f) * 1.2f * _weatherDim;
            _sun.color = Color.Lerp(new Color(1f, 0.96f, 0.88f), new Color(1f, 0.52f, 0.28f), warm);
            _sun.enabled = _sun.intensity > 0.01f;

            _moon.transform.rotation = Quaternion.Euler(sunAngle + 180f, -35f, 0f);
            _moon.intensity = Mathf.Clamp01(-elevation * 3f) * 0.28f * Mathf.Lerp(0.5f, 1f, _weatherDim);
            _moon.enabled = _moon.intensity > 0.01f;

            var nightSky = new Color(0.04f, 0.06f, 0.13f);
            var daySky = Color.Lerp(new Color(0.42f, 0.45f, 0.5f), new Color(0.55f, 0.66f, 0.82f), _weatherDim);
            var sunsetSky = new Color(0.75f, 0.48f, 0.38f);
            var sky = Color.Lerp(nightSky, daySky, day);
            sky = Color.Lerp(sky, sunsetSky, warm * day * 0.45f * _weatherDim);

            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = sky * 0.8f;
            RenderSettings.ambientGroundColor = sky * 0.45f;
            RenderSettings.fogColor = Color.Lerp(sky, new Color(0.6f, 0.62f, 0.65f) * day, 1f - _weatherDim);
            RenderSettings.fogDensity = Mathf.Lerp(0.011f, 0.0028f, _weatherDim)
                                        + (Game.Clock.Band == TimeBand.Dawn ? 0.004f : 0f); // 새벽 물안개

            _sky.SetFloat("_Exposure", Mathf.Lerp(0.12f, 1.25f, day) * Mathf.Lerp(0.55f, 1f, _weatherDim));
            _sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.9f, 1.6f, warm));
            _sky.SetColor("_SkyTint", Color.Lerp(new Color(0.55f, 0.55f, 0.55f), new Color(0.5f, 0.55f, 0.65f), _weatherDim));
            _sky.SetColor("_GroundColor", Color.Lerp(new Color(0.12f, 0.12f, 0.14f), new Color(0.37f, 0.35f, 0.34f), day));

            bool wantLamps = day < 0.45f || _weatherDim < 0.45f;
            if (wantLamps != _lampsOn) SetLamps(wantLamps);
        }

        void SetLamps(bool on)
        {
            _lampsOn = on;
            foreach (var l in _lamps) l.enabled = on;
            foreach (var r in _lampBulbs) r.sharedMaterial = on ? _bulbOn : _bulbOff;
        }
    }
}
