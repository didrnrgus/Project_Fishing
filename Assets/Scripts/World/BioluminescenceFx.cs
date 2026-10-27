using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 야광충 밤바다 (서천·삼척, <see cref="FieldMapDef.Bioluminescence"/>). 물 셰이더가 파도 앞 빛을 그리고, 여기서는 캐릭터 둘레를 맡는다.
    ///  - 캐릭터 근처 물가: 빛나는 파도 앞(셰이더 BioSaw와 같은 식)에서 파란 빛 알갱이가 튀고 깜빡이다 사라진다. 물가에서 멀수록 드물다
    ///  - 건드린 물: 찌가 떨어지거나 물보라·파문이 일면(<see cref="Effects"/>) 그 자리 물이 고리로 퍼지며 빛나고(_BioTouch) 빛 알갱이가 흩어진다.
    ///    물보라 물방울도 파랗게 빛난다
    ///  - 발걸음: 물가 젖은 모래를 걸으면 발밑이 반짝인다 (실제 야광충 해변)
    /// 밤 정도는 물 불빛 기둥과 같은 _GlintNight (DayNightCycle).
    /// </summary>
    public class BioluminescenceFx : MonoBehaviour
    {
        const int MaxTouch = 8;            // Water.shader의 _BioTouch 배열 크기와 같아야 한다
        const float FoamWidth = 4.5f;      // Water.shader _FoamWidth 기본값
        const float NearRadius = 22f;

        struct Touch { public Vector2 Pos; public float Radius, Strength, Age, Life; }

        static BioluminescenceFx _inst;
        static readonly List<Touch> Touches = new List<Touch>();
        static readonly Vector4[] TouchBuf = new Vector4[MaxTouch];

        ParticleSystem _glints, _drops;
        float _emitDebt;
        Vector3 _lastStep;
        bool _uploadedEmpty;

        static readonly Color[] Blues =
        {
            new Color(0.05f, 0.45f, 1f), new Color(0.1f, 0.6f, 1f), new Color(0.3f, 0.8f, 1f), new Color(0.6f, 0.92f, 1f),
        };

        /// <summary>물보라 색: 야광충 밤바다에서는 파랗게 빛난다 (<see cref="Effects.FightSpray"/>·<see cref="Effects.BigSplash"/>).</summary>
        public static Color SprayTint => Color.Lerp(Color.white, new Color(0.3f, 0.65f, 1f), Level * 0.85f);

        /// <summary>0(없음·낮) ~ 1(야광충 맵의 한밤).</summary>
        public static float Level
        {
            get
            {
                var map = FieldMap.Current;
                if (map == null || map.Bioluminescence <= 0f || Game.Mode != GameMode.Field) return 0f;
                return map.Bioluminescence * Shader.GetGlobalFloat("_GlintNight");
            }
        }

        void Awake()
        {
            _inst = this;
            _glints = MakeSystem("BioGlints", 0f, 4000);
            _drops = MakeSystem("BioDrops", 1.6f, 800);
        }

        ParticleSystem MakeSystem(string name, float gravity, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = max;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            // 켜졌다 → 한 번 죽었다 → 다시 반짝 → 꺼짐 (수명이 저마다 달라 박자가 엇갈린다)
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.35f, 0.3f), new GradientAlphaKey(0.9f, 0.45f), new GradientAlphaKey(0.25f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Fishing/BioGlow");
            r.sharedMaterial = shader != null
                ? new Material(shader) { mainTexture = ProceduralTextures.Star }
                : Mats.Particle(new Color(0.1f, 0.5f, 1f, 1f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        // ---------------------------------------------------------------- 건드린 물

        /// <summary>물을 건드렸다: 그 자리가 반경 radius(m)로 퍼지며 life초 동안 빛나고, 빛 알갱이가 흩어진다.</summary>
        public static void Disturb(Vector3 pos, float radius, float strength, float life = 1.6f)
        {
            if (_inst == null) return;
            float level = Level;
            if (level < 0.02f) return;
            var t = new Touch { Pos = new Vector2(pos.x, pos.z), Radius = radius, Strength = Mathf.Clamp01(strength), Life = life };
            if (Touches.Count < MaxTouch) Touches.Add(t);
            else
            {
                // 가장 많이 꺼진 것을 바꾼다
                int weakest = 0;
                float min = float.MaxValue;
                for (int i = 0; i < Touches.Count; i++)
                {
                    float left = Touches[i].Strength * (1f - Touches[i].Age / Touches[i].Life);
                    if (left < min) { min = left; weakest = i; }
                }
                Touches[weakest] = t;
            }
            int n = Mathf.RoundToInt((6f + radius * 10f) * strength * level);
            for (int i = 0; i < n; i++)
            {
                var d = Random.insideUnitCircle * radius;
                _inst.Glint(new Vector3(pos.x + d.x, 0f, pos.z + d.y), Random.Range(0.1f, 0.5f), level, Random.Range(0.6f, 1.8f));
            }
        }

        /// <summary>물보라 물방울이 파랗게 빛난다 (<see cref="Effects.Splash"/>).</summary>
        public static void Splash(Vector3 pos, int count, float strength)
        {
            if (_inst == null) return;
            float level = Level;
            if (level < 0.02f) return;
            var p = new ParticleSystem.EmitParams();
            int n = Mathf.RoundToInt(count * 0.7f * level);
            for (int i = 0; i < n; i++)
            {
                var dir = Random.insideUnitCircle * 1.6f;
                p.position = pos + new Vector3(dir.x * 0.15f, 0.05f, dir.y * 0.15f);
                p.velocity = new Vector3(dir.x, Random.Range(2.5f, 5f) * strength, dir.y);
                p.startSize = Random.Range(0.08f, 0.2f) * Mathf.Sqrt(strength);
                p.startLifetime = Random.Range(0.5f, 1f);
                p.startColor = Blues[Random.Range(0, Blues.Length)] * new Color(1f, 1f, 1f, level);
                _inst._drops.Emit(p, 1);
            }
            Disturb(pos, 0.5f + 0.5f * strength, Mathf.Clamp01(count / 16f));
        }

        // ---------------------------------------------------------------- 매 프레임

        void Update()
        {
            float level = Level;
            if (level < 0.02f)
            {
                if (Touches.Count > 0) Touches.Clear();
                if (!_uploadedEmpty) { UploadTouches(); _uploadedEmpty = true; }
                return;
            }
            _uploadedEmpty = false;
            float dt = Time.deltaTime;
            for (int i = Touches.Count - 1; i >= 0; i--)
            {
                var t = Touches[i];
                t.Age += dt;
                if (t.Age >= t.Life) Touches.RemoveAt(i);
                else Touches[i] = t;
            }
            UploadTouches();

            if (Game.Player == null) return;
            var player = Game.Player.transform.position;
            NearShore(player, level, dt);
            Footsteps(player, level);
        }

        static void UploadTouches()
        {
            for (int i = 0; i < MaxTouch; i++)
            {
                if (i >= Touches.Count) { TouchBuf[i] = Vector4.zero; continue; }
                var t = Touches[i];
                float k = t.Age / t.Life;
                // 반경은 빠르게 퍼지다 느려지고, 세기는 처음에 확 밝았다가 서서히 꺼진다
                TouchBuf[i] = new Vector4(t.Pos.x, t.Pos.y, t.Radius * (0.35f + 0.65f * Mathf.Sqrt(k)), t.Strength * Mathf.Pow(1f - k, 1.5f));
            }
            Shader.SetGlobalVectorArray("_BioTouch", TouchBuf);
            Shader.SetGlobalFloat("_BioTouchCount", Touches.Count);
        }

        /// <summary>캐릭터 둘레 물가: 빛나는 파도 앞에서 빛 알갱이가 튄다. 셰이더와 같은 식으로 파도 앞을 찾는다.</summary>
        void NearShore(Vector3 center, float level, float dt)
        {
            _emitDebt += 420f * level * dt;   // 사용자: 파티클 더 (160 → 420)
            int tries = Mathf.Min(Mathf.FloorToInt(_emitDebt) * 4, 700);
            _emitDebt -= Mathf.Floor(_emitDebt);
            float time = Time.timeSinceLevelLoad;
            for (int i = 0; i < tries; i++)
            {
                var d = Random.insideUnitCircle;
                float x = center.x + d.x * NearRadius, z = center.z + d.y * NearRadius;
                if (!WaterSurface.ShoreDistance(x, z, out float depth, out float dist)) continue;
                if (depth < -0.1f) continue;   // 물 밖 (젖은 모래 선은 조금 포함)
                // 벽 앞 깊은 물: 셰이더(Water.shader prox)처럼 둘레 땅을 살펴 벽 가까움으로 넓게 (벽에서 10m 남짓까지 옅어진다)
                float wallK = Mathf.Max(Mathf.Max(Smooth(0.8f, 2f, depth), WaterSurface.WallNear(x, z)), Smooth(FoamWidth * 2f, FoamWidth * 4f, dist));   // 방파제 둘레·물가에서 먼 곳도 벽 앞으로 (셰이더와 같게)
                float prox = wallK > 0.01f ? WaterSurface.Proximity(x, z) : 0f;
                float pseudo = (1f - prox) * FoamWidth * 3f;
                float zone = Mathf.Lerp(Smooth(FoamWidth * 4f, FoamWidth * 0.3f, dist), prox, wallK);
                float front = FrontGlow(x, z, Mathf.Lerp(dist, pseudo, wallK), time) * zone;
                float chance = front * 1.2f + zone * 0.22f + 0.02f;
                if (Random.value > chance) continue;
                // 파도 앞에서는 물방울처럼 살짝 튀어 오르고, 가끔 흰빛 알갱이
                Glint(new Vector3(x, 0f, z), front > 0.3f ? Random.Range(0.3f, 1.1f) : Random.Range(0.02f, 0.2f), level, Random.Range(0.6f, 1.8f), front > 0.45f && Random.value < 0.35f);
            }
        }

        /// <summary>물가 젖은 모래·얕은 물을 걸으면 발밑이 반짝이고 물이 빛난다.</summary>
        void Footsteps(Vector3 player, float level)
        {
            if ((player - _lastStep).sqrMagnitude < 0.7f * 0.7f) return;
            _lastStep = player;
            if (Game.Boat != null && Game.Boat.Riding) return;   // 배는 물보라(Effects)가 맡는다
            if (!WaterSurface.ShoreDistance(player.x, player.z, out float depth, out float dist)) return;
            if (depth < -0.35f || dist > 2.5f) return;
            for (int i = 0; i < 10; i++)
            {
                var d = Random.insideUnitCircle * 0.6f;
                Glint(new Vector3(player.x + d.x, 0f, player.z + d.y), Random.Range(0.05f, 0.4f), level, Random.Range(0.6f, 1.4f));
            }
            Disturb(player, 1.1f, 0.55f, 1.4f);
        }

        void Glint(Vector3 pos, float up, float level, float life, bool white = false)
        {
            float y = FieldMap.WaterY + 0.12f;
            if (WaterSurface.SeabedHeight(pos.x, pos.z, out float bed)) y = Mathf.Max(y, bed + 0.06f);
            var p = new ParticleSystem.EmitParams
            {
                position = new Vector3(pos.x, y, pos.z),
                velocity = new Vector3(Random.Range(-0.15f, 0.15f), up, Random.Range(-0.15f, 0.15f)),
                startSize = Random.Range(0.12f, 0.32f),
                startLifetime = life,
                startColor = (white ? new Color(0.85f, 0.97f, 1f) : Blues[Random.Range(0, Blues.Length)]) * new Color(1f, 1f, 1f, level),
            };
            _glints.Emit(p, 1);
        }

        // Water.shader BioSaw·BioPatch·BioFront와 같은 식 (p = 월드 x, z, 얼룩 churn은 평균값)
        static float FrontGlow(float x, float z, float dist, float t)
        {
            float wob = 2.2f * Mathf.Sin(x * 0.31f + z * 0.13f + t * 0.21f) + 1.4f * Mathf.Sin(z * 0.41f - x * 0.53f - t * 0.33f)
                      + (Noise(x * 0.45f, z * 0.45f) - 0.5f) * 2.6f + (Noise(x * 1.7f + 7f, z * 1.7f + 7f) - 0.5f) * 0.8f;
            float v = (dist * 1.4f + t * 1.1f + wob) / 6.2831f;
            float s = v - Mathf.Floor(v), k = 1f - s;
            const float speck = 0.38f;
            float d2 = (s - 0.22f) / 0.03f;
            float lace = Mathf.Exp(-d2 * d2) * 0.4f * Smooth(0.35f, 0.7f, Noise(x * 0.6f + 11f, z * 0.6f + 11f));
            float front = Smooth(0f, 0.012f, s) * (Mathf.Pow(k, 45f) * (0.75f + 0.25f * speck) + Mathf.Pow(k, 2.5f) * (0.1f + speck * 0.9f) * 0.6f) + lace * (0.5f + speck * 0.5f);
            return front * (0.35f + 0.65f * Smooth(0.25f, 0.75f, Noise(x * 0.11f + t * 0.04f, z * 0.11f - t * 0.025f)));
        }

        // FishingNoise.cginc FishHash·FishNoise
        static float Hash(float x, float y)
        {
            x = Frac(x * 123.34f); y = Frac(y * 456.21f);
            float d = x * (x + 45.32f) + y * (y + 45.32f);
            x += d; y += d;
            return Frac(x * y);
        }

        static float Noise(float x, float y)
        {
            float ix = Mathf.Floor(x), iy = Mathf.Floor(y), fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1f, iy), fx), Mathf.Lerp(Hash(ix, iy + 1f), Hash(ix + 1f, iy + 1f), fx), fy);
        }

        static float Frac(float v) => v - Mathf.Floor(v);

        /// <summary>HLSL smoothstep.</summary>
        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }
    }
}
