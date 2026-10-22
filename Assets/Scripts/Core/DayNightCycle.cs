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
        ReflectionProbe _skyProbe;
        Fishing.World.SkyDecor _decor;
        float _nextProbeAt;
        // 흐린 날·비 오는 날 하늘을 덮는 큐브맵 돔 (AllSky Free "Overcast Low")
        Transform _overcastDome;
        Material _overcastMat;
        float _overcast;
        // 맑은 낮 뭉게구름 하늘 (Fantasy Skybox FREE "Day 01·03·05 Sunless"): 게임 날짜마다 셋 중 하나를 옅게 덮는다
        Transform _fairDome;
        Material _fairMat;
        Cubemap[] _fairCubes;
        float _fair;

        /// <summary>낚시터가 바뀌면 그 맵의 가로등으로 바꾼다.</summary>
        public void SetLampSet(List<Light> lamps, List<Renderer> bulbs)
        {
            _lamps.Clear();
            _lampBulbs.Clear();
            _lamps.AddRange(lamps);
            _lampBulbs.AddRange(bulbs);
            if (_bulbOn != null) SetLamps(_lampsOn);
        }

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

            _decor = new GameObject("SkyDecor").AddComponent<Fishing.World.SkyDecor>();
            _decor.transform.SetParent(transform, false);
            BuildOvercastDome();

            // 기본 반사는 시작할 때의 하늘로 고정되므로, 하늘만 찍는 실시간 프로브를 주기적으로 갱신한다
            var probeGo = new GameObject("SkyReflection");
            probeGo.transform.SetParent(transform, false);
            _skyProbe = probeGo.AddComponent<ReflectionProbe>();
            _skyProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            _skyProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            _skyProbe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            _skyProbe.size = new Vector3(2000f, 400f, 2000f);
            _skyProbe.resolution = 64;
            _skyProbe.cullingMask = 0;
            _skyProbe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            _skyProbe.importance = 0;
        }

        void BuildOvercastDome()
        {
            var cube = Resources.Load<Cubemap>("Sky/Overcast");
            var shader = Shader.Find("Fishing/SkyCube");
            if (cube == null || shader == null) return;
            _overcastMat = new Material(shader);
            _overcastMat.SetTexture("_Cube", cube);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "OvercastDome";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 2300f; // 반지름 1150m (카메라 far 1200m 안, 먼 섬·한라산보다 바깥)
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = _overcastMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _overcastDome = go.transform;
            go.SetActive(false);

            _fairCubes = new[] { Resources.Load<Cubemap>("Sky/Fair01"), Resources.Load<Cubemap>("Sky/Fair02"), Resources.Load<Cubemap>("Sky/Fair03") };
            if (_fairCubes[0] == null) return;
            _fairMat = new Material(shader);
            var fair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fair.name = "FairSkyDome";
            Object.Destroy(fair.GetComponent<Collider>());
            fair.transform.SetParent(transform, false);
            fair.transform.localScale = Vector3.one * 2320f; // 흐림 돔보다 살짝 바깥 (둘이 함께 보일 때 흐림이 위)
            var fr = fair.GetComponent<MeshRenderer>();
            fr.sharedMaterial = _fairMat;
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr.receiveShadows = false;
            _fairMat.renderQueue = 3000 - 301; // 흐림 돔(Transparent-300)보다 먼저
            _fairDome = fair.transform;
            fair.SetActive(false);
        }

        /// <summary>
        /// 맑은 날 낮(해가 높을 때)에만 그림 같은 뭉게구름 하늘을 0.45만큼 덮는다. 흐려지면 흐림 돔에 자리를 내준다.
        /// 해 없는 큐브맵이라 절차적 하늘의 해가 비쳐 보인다. 노을·밤에는 사라진다.
        /// </summary>
        void UpdateFair(float day, float warm)
        {
            if (_fairDome == null) return;
            float target = Game.Weather.Current == Weather.Clear ? 0.45f * Mathf.Clamp01((day - 0.55f) / 0.35f) * (1f - warm * 0.8f) : 0f;
            _fair = Mathf.MoveTowards(_fair, target, Time.deltaTime * 0.12f);
            bool show = _fair > 0.01f && Game.Mode == GameMode.Field && Game.CameraRig != null;
            _fairDome.gameObject.SetActive(show);
            if (!show) return;
            _fairDome.position = Game.CameraRig.Cam.transform.position;
            var cube = _fairCubes[Mathf.Abs(Game.Clock.Day) % _fairCubes.Length];
            if (cube != null && _fairMat.GetTexture("_Cube") != cube) _fairMat.SetTexture("_Cube", cube);
            _fairMat.SetColor("_Tint", Color.Lerp(new Color(0.75f, 0.8f, 0.9f), Color.white, day));
            _fairMat.SetFloat("_Blend", _fair);
            _fairMat.SetFloat("_Yaw", Game.Clock.TotalMinutes * 0.03f + 40f);
        }

        /// <summary>흐림 0.75, 비 0.95만큼 큐브맵 하늘을 덮는다. 낮 밝기·노을색에 맞춰 어둡게, 밤엔 거의 검게.</summary>
        void UpdateOvercast(float day, float warm)
        {
            if (_overcastDome == null) return;
            float target = Game.Weather.Current switch { Weather.Cloudy => 0.75f, Weather.Rain => 0.95f, _ => 0f };
            _overcast = Mathf.MoveTowards(_overcast, target, Time.deltaTime * 0.15f);
            bool show = _overcast > 0.01f && Game.Mode == GameMode.Field && Game.CameraRig != null;
            _overcastDome.gameObject.SetActive(show);
            if (!show) return;
            _overcastDome.position = Game.CameraRig.Cam.transform.position;
            var tint = Color.Lerp(new Color(0.06f, 0.07f, 0.1f), Color.white, day) * Mathf.Lerp(0.75f, 1f, _weatherDim);
            tint = Color.Lerp(tint, tint * new Color(1.15f, 0.85f, 0.7f), warm * day * 0.6f);
            _overcastMat.SetColor("_Tint", tint);
            _overcastMat.SetFloat("_Blend", _overcast * Mathf.Lerp(0.6f, 1f, day));
            _overcastMat.SetFloat("_Yaw", Game.Clock.TotalMinutes * 0.02f); // 구름이 아주 천천히 흘러간다
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

        /// <summary>해·달이 가장 높을 때 고도 30° (sin, cos) — 수평으로 보는 3인칭 화면 위쪽에 걸린다.</summary>
        const float SunPathSin = 0.5f, SunPathCos = 0.866f;

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
            // 해 뜰 녘·질 녘(고도 0 근처)에도 하늘빛이 절반은 남게: 예전 0.2는 06시에 하늘은 주황인데 땅이 거의 검었다
            float day = Mathf.Clamp01(elevation * 3f + 0.42f);
            float warm = 1f - Mathf.Clamp01(elevation * 3.5f);

            // 해가 지나는 길: 동쪽(+X)에서 떠서 물 쪽(+Z, 낚시할 때 바라보는 쪽) 하늘 높이 30°를 지나 서쪽(-X)으로 진다.
            // 예전에는 정오에 머리 바로 위(90°)를 지나고 달은 그 반대(밤 10시에 60°)라 수평으로 보는 3인칭 화면에 해·달이 안 보였다.
            // 밝기(elevation)는 예전 그대로, 보이는 자리만 낮게 기울였다
            float s = sunAngle * Mathf.Deg2Rad;
            var toSun = new Vector3(Mathf.Cos(s), Mathf.Sin(s) * SunPathSin, Mathf.Sin(s) * SunPathCos).normalized;
            _sun.transform.rotation = Quaternion.LookRotation(-toSun);
            _sun.intensity = (elevation > -0.03f ? Mathf.Clamp01(elevation * 2.5f + 0.15f) : 0f) * 1.0f * _weatherDim; // 지평선의 낮은 햇빛
            _sun.color = Color.Lerp(new Color(1f, 0.96f, 0.88f), new Color(1f, 0.52f, 0.28f), warm);
            _sun.enabled = _sun.intensity > 0.01f;

            _moon.transform.rotation = Quaternion.LookRotation(toSun); // 달은 해의 정반대 (밤 12시에 물 쪽 30°)
            // 원작 밤(2013 홍현 영상): 검지 않고 보랏빛 남색으로 밝다. 바위·물결·캐릭터가 또렷하게 보이는 "달빛 낮"
            _moon.intensity = Mathf.Clamp01(-elevation * 3f) * 0.5f * Mathf.Lerp(0.5f, 1f, _weatherDim);
            _moon.enabled = _moon.intensity > 0.01f;

            var nightSky = new Color(0.12f, 0.13f, 0.26f);
            var daySky = Color.Lerp(new Color(0.36f, 0.38f, 0.42f), new Color(0.42f, 0.5f, 0.62f), _weatherDim);
            var sunsetSky = new Color(0.75f, 0.48f, 0.38f);
            var sky = Color.Lerp(nightSky, daySky, day);
            sky = Color.Lerp(sky, sunsetSky, warm * day * 0.45f * _weatherDim);

            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = sky * 0.8f;
            RenderSettings.ambientGroundColor = sky * 0.45f;
            // 공기 원근감: 맑은 낮 안개는 지평선 하늘빛(밝은 푸른 회색, 노을엔 주황). 예전에는 어두운 환경광 색이라 먼 섬이 뿌옇게 바래지 않고 거뭇하게 남았다
            var haze = Color.Lerp(sky, Color.Lerp(new Color(0.64f, 0.72f, 0.82f), new Color(0.9f, 0.66f, 0.5f), warm * 0.7f), day * 0.75f);
            RenderSettings.fogColor = Color.Lerp(haze, new Color(0.6f, 0.62f, 0.65f) * day, 1f - _weatherDim);
            // 해무가 잦은 곳(FogBoost)은 흐린 날·새벽에 훨씬 짙다
            var map = Fishing.World.FieldMap.Current;
            float seaFog = map != null && Game.Mode == GameMode.Field
                ? map.FogBoost * (Game.Weather.Current != Weather.Clear ? 1f : Game.Clock.Band == TimeBand.Dawn ? 0.7f : 0.12f) : 0f;
            RenderSettings.fogDensity = Mathf.Lerp(0.011f, 0.0028f, _weatherDim)
                                        + (Game.Clock.Band == TimeBand.Dawn ? 0.004f : 0f) // 새벽 물안개
                                        + seaFog;

            _sky.SetFloat("_Exposure", Mathf.Lerp(0.3f, 1.25f, day) * Mathf.Lerp(0.55f, 1f, _weatherDim));
            _sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.9f, 1.6f, warm));
            _sky.SetColor("_SkyTint", Color.Lerp(new Color(0.55f, 0.55f, 0.55f), new Color(0.5f, 0.55f, 0.65f), _weatherDim));
            _sky.SetColor("_GroundColor", Color.Lerp(new Color(0.12f, 0.12f, 0.14f), new Color(0.37f, 0.35f, 0.34f), day));

            // 구름·달·별
            float coverage = Game.Weather.Current switch { Weather.Cloudy => 0.78f, Weather.Rain => 0.95f, _ => 0.4f };
            var cloudDay = Color.Lerp(new Color(0.62f, 0.64f, 0.68f), Color.white, _weatherDim);
            var cloudSunset = new Color(1f, 0.62f, 0.45f);
            var cloudNight = new Color(0.24f, 0.25f, 0.38f);
            var cloudColor = Color.Lerp(cloudNight, Color.Lerp(cloudDay, cloudSunset, warm * 0.8f), day);
            cloudColor.a = Mathf.Lerp(0.55f, 0.95f, day);
            if (Game.CameraRig != null)
                _decor.Apply(Game.CameraRig.Cam, toSun, 1f - day, cloudColor, coverage, warm);

            if (Time.time >= _nextProbeAt)
            {
                _nextProbeAt = Time.time + 2f;
                _skyProbe.RenderProbe();
            }

            UpdateOvercast(day, warm);
            UpdateFair(day, warm);
            Fishing.World.KenneyProps.SetNightGlow(1f - day);
            bool wantLamps = day < 0.45f || _weatherDim < 0.45f;
            Fishing.World.WaterGlints.SetNight(Mathf.Clamp01((0.55f - day) / 0.35f));
            // 랜드마크(Fishing/Landmark) 창문 불빛: 해 질 녘부터 켜진다
            Shader.SetGlobalFloat("_FishingNight", Mathf.Clamp01((0.62f - day) / 0.35f));
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
