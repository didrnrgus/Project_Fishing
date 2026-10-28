using System.Collections.Generic;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 대기실(한국 낚시 조합)의 수족관. 원작: 낚은 물고기·산호·해초로 꾸미고, 시간이 지나면 배가 고파져 미끼를 주면 경험치를 조금 얻는다.
    /// 수조 안에서 넣은 물고기가 실제로 헤엄친다(작게 줄여서). 장식은 정해진 자리에 놓인다.
    /// </summary>
    public class Aquarium : MonoBehaviour
    {
        public const int Capacity = 6;
        public const int MaxDecor = 8;
        /// <summary>배가 다 고파지는 데 걸리는 게임 시간(분).</summary>
        public const float HungryMinutes = 8 * 60f;

        public static readonly (string id, string name, int price, Color color)[] DecorShop =
        {
            ("coral_red", "붉은 산호", 1500, new Color(0.85f, 0.2f, 0.18f)),
            ("coral_pink", "분홍 산호", 2000, new Color(0.95f, 0.5f, 0.6f)),
            ("seaweed", "해초 다발", 800, new Color(0.2f, 0.55f, 0.25f)),
            ("shell", "큰 조개", 1200, new Color(0.92f, 0.85f, 0.75f)),
            ("rock", "수석", 1000, new Color(0.45f, 0.45f, 0.48f)),
        };

        // 수조 안쪽 (수족관 루트 기준)
        static readonly Vector3 Inner = new Vector3(2.2f, 1.0f, 0.75f);
        const float BaseY = 0.85f;

        Transform _fishRoot, _decorRoot;
        /// <summary>
        /// 수조 속 한 마리. 일정한 속도로 목표점까지 직선으로 미끄러지면(예전) 꼬리만 흔드는 장난감처럼 보여서,
        /// 진짜 물고기처럼 꼬리를 몇 번 쳐서 나아갔다가 미끄러지고(burst-and-glide), 방향은 천천히 틀며 몸을 기울이고,
        /// 위아래로는 비스듬히만 움직이며, 가끔 멈춰 지느러미로 제자리를 지킨다.
        /// </summary>
        class Swimmer
        {
            public Transform T;
            public GameObject Model;
            public Vector3 Target;
            public float Cruise, Speed, Phase, PhaseLeft, Yaw, Pitch, Bank, YawRate;
            public bool Beat, Hover;
            public string Clip;
        }
        readonly List<Swimmer> _swimmers = new List<Swimmer>();
        readonly List<Transform> _bubbles = new List<Transform>();
        readonly List<Transform> _weeds = new List<Transform>();

        // 수족관 창이 화면 오른쪽에 뜨므로, 수조가 화면 왼쪽에 오도록 오른쪽 앞에서 비춘다
        public Vector3 ViewPos => transform.position + transform.rotation * new Vector3(-0.6f, 1.45f, 2.4f);
        public Quaternion ViewRot => Quaternion.LookRotation(transform.position + transform.rotation * new Vector3(-1.2f, 1.25f, 0f) - ViewPos);

        public static Aquarium Build(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Aquarium");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var a = go.AddComponent<Aquarium>();
            a.BuildTank();
            a.Rebuild();
            return a;
        }

        void BuildTank()
        {
            var t = transform;
            var wood = Mats.Lit(new Color(0.28f, 0.18f, 0.11f), 0.3f);
            Mats.Prim(PrimitiveType.Cube, "Cabinet", t, new Vector3(0f, 0.4f, 0f), new Vector3(Inner.x + 0.2f, 0.8f, Inner.z + 0.15f), wood, false);
            Mats.Prim(PrimitiveType.Cube, "Lid", t, new Vector3(0f, BaseY + Inner.y + 0.06f, 0f), new Vector3(Inner.x + 0.12f, 0.1f, Inner.z + 0.08f), wood, false);
            // 물과 유리 (알파 블렌드), 바닥 모래, 뒤판
            Mats.Prim(PrimitiveType.Cube, "Water", t, new Vector3(0f, BaseY + Inner.y * 0.47f, 0f), new Vector3(Inner.x, Inner.y * 0.94f, Inner.z),
                Mats.Particle(new Color(0.25f, 0.55f, 0.75f, 0.18f)), false);
            Mats.Prim(PrimitiveType.Cube, "Glass", t, new Vector3(0f, BaseY + Inner.y * 0.5f, Inner.z * 0.5f + 0.01f), new Vector3(Inner.x + 0.04f, Inner.y, 0.01f),
                Mats.Particle(new Color(0.8f, 0.95f, 1f, 0.08f)), false);
            Mats.Prim(PrimitiveType.Cube, "Back", t, new Vector3(0f, BaseY + Inner.y * 0.5f, -Inner.z * 0.5f), new Vector3(Inner.x, Inner.y, 0.02f),
                Mats.Lit(new Color(0.08f, 0.22f, 0.32f), 0.4f), false);
            Mats.Prim(PrimitiveType.Cube, "Sand", t, new Vector3(0f, BaseY + 0.04f, 0f), new Vector3(Inner.x, 0.08f, Inner.z), Mats.Lit(new Color(0.85f, 0.78f, 0.6f)), false);
            foreach (int side in new[] { -1, 1 })
                Mats.Prim(PrimitiveType.Cube, "Frame", t, new Vector3(side * (Inner.x * 0.5f + 0.03f), BaseY + Inner.y * 0.5f, 0f), new Vector3(0.05f, Inner.y + 0.05f, Inner.z + 0.05f), wood, false);

            var light = new GameObject("TankLight").AddComponent<Light>();
            light.transform.SetParent(t, false);
            light.transform.localPosition = new Vector3(0f, BaseY + Inner.y - 0.1f, 0.2f);
            light.type = LightType.Point;
            light.color = new Color(0.6f, 0.85f, 1f);
            light.range = 2.6f;
            light.intensity = 1.6f;

            _fishRoot = new GameObject("Fish").transform;
            _fishRoot.SetParent(t, false);
            _decorRoot = new GameObject("Decor").transform;
            _decorRoot.SetParent(t, false);

            // 기포
            var bubble = Mats.Particle(new Color(0.9f, 0.97f, 1f, 0.6f));
            for (int i = 0; i < 10; i++)
                _bubbles.Add(Mats.Prim(PrimitiveType.Sphere, "Bubble", t, new Vector3(Inner.x * 0.4f, BaseY + Random.value * Inner.y, -0.2f),
                    Vector3.one * Random.Range(0.02f, 0.045f), bubble, false).transform);
        }

        static Vector3 RandomInside() => new Vector3(Random.Range(-Inner.x * 0.4f, Inner.x * 0.4f), BaseY + Random.Range(0.25f, Inner.y * 0.85f),
            Random.Range(-Inner.z * 0.3f, Inner.z * 0.3f));

        /// <summary>저장된 물고기·장식으로 수조 안을 다시 꾸민다.</summary>
        public void Rebuild()
        {
            var d = Game.Profile?.Data;
            if (d == null) return;
            for (int i = _fishRoot.childCount - 1; i >= 0; i--) Destroy(_fishRoot.GetChild(i).gameObject);
            for (int i = _decorRoot.childCount - 1; i >= 0; i--) Destroy(_decorRoot.GetChild(i).gameObject);
            _swimmers.Clear();
            _weeds.Clear();

            foreach (var f in d.aquarium)
            {
                var s = FishDatabase.Get(f.speciesId);
                if (s == null) continue;
                var holder = new GameObject("Swimmer").transform;
                holder.SetParent(_fishRoot, false);
                holder.localPosition = RandomInside();
                // 수조에 맞게 줄인다 (실제 길이 비율은 남긴다)
                float shown = Mathf.Lerp(12f, 45f, Mathf.InverseLerp(s.MinLen, Mathf.Max(s.MaxLen, s.MinLen + 1f), f.length));
                var model = FishModels.Create(s, shown, holder);
                model.transform.localRotation = Quaternion.identity;
                FishModels.Play(model, FishModels.SwimSlow, 1f, 0f);
                float yaw = Random.Range(0f, 360f);
                holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
                _swimmers.Add(new Swimmer { T = holder, Model = model, Target = NextTarget(holder.localPosition), Cruise = Random.Range(0.18f, 0.3f),
                    Speed = 0.1f, Yaw = yaw, Beat = true, PhaseLeft = Random.Range(0.3f, 1f), Clip = FishModels.SwimSlow });
            }

            for (int i = 0; i < d.aquariumDecor.Count && i < MaxDecor; i++)
            {
                string id = d.aquariumDecor[i];
                var spot = new Vector3(-Inner.x * 0.42f + (i % 4) * Inner.x * 0.28f, BaseY + 0.08f, i < 4 ? -Inner.z * 0.25f : Inner.z * 0.12f);
                BuildDecor(id, spot);
            }
        }

        void BuildDecor(string id, Vector3 at)
        {
            Color color = Color.white;
            foreach (var e in DecorShop) if (e.id == id) color = e.color;
            var mat = Mats.Lit(color, 0.3f);
            var d = new GameObject(id).transform;
            d.SetParent(_decorRoot, false);
            d.localPosition = at;
            switch (id)
            {
                case "coral_red":
                case "coral_pink":
                    for (int k = 0; k < 5; k++)
                        Mats.Prim(PrimitiveType.Capsule, "Branch", d, new Vector3(Random.Range(-0.06f, 0.06f), 0.12f + k * 0.03f, Random.Range(-0.05f, 0.05f)),
                            new Vector3(0.05f, 0.14f + k * 0.02f, 0.05f), mat, false, Quaternion.Euler(Random.Range(-35f, 35f), 0f, Random.Range(-35f, 35f)));
                    break;
                case "seaweed":
                    for (int k = 0; k < 4; k++)
                    {
                        var w = Mats.Prim(PrimitiveType.Capsule, "Weed", d, new Vector3(k * 0.05f - 0.08f, 0.25f, 0f), new Vector3(0.03f, 0.25f, 0.015f), mat, false).transform;
                        _weeds.Add(w);
                    }
                    break;
                case "shell":
                    Mats.Prim(PrimitiveType.Sphere, "Shell", d, new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.06f, 0.16f), mat, false);
                    Mats.Prim(PrimitiveType.Sphere, "Pearl", d, new Vector3(0f, 0.07f, 0.02f), Vector3.one * 0.04f, Mats.Lit(Color.white, 0.95f, 0.3f), false);
                    break;
                default:
                    Mats.Prim(PrimitiveType.Sphere, "Rock", d, new Vector3(0f, 0.08f, 0f), new Vector3(0.24f, 0.18f, 0.2f), mat, false);
                    break;
            }
        }

        /// <summary>다음 목표점: 수평으로는 충분히 떨어진 곳, 높이는 조금만 바꾼다 (물고기는 수직으로 솟구치지 않는다).</summary>
        static Vector3 NextTarget(Vector3 from)
        {
            for (int k = 0; k < 8; k++)
            {
                var p = RandomInside();
                p.y = Mathf.Clamp(from.y + Random.Range(-0.18f, 0.18f), BaseY + 0.25f, BaseY + Inner.y * 0.85f);
                if (new Vector2(p.x - from.x, p.z - from.z).magnitude > 0.5f) return p;
            }
            return RandomInside();
        }

        void Update()
        {
            var d = Game.Profile?.Data;
            float dt = Time.deltaTime;
            for (int i = 0; i < _swimmers.Count; i++)
            {
                var f = _swimmers[i];
                if (f.T == null) continue;
                // 배고픈 고기는 느릿느릿
                float hunger = d != null && i < d.aquarium.Count ? Hunger(d.aquarium[i]) : 0f;
                float cruise = f.Cruise * (hunger > 60f ? 0.5f : 1f);
                var to = f.Target - f.T.localPosition;
                float dist = to.magnitude;

                // 박자: 꼬리 치기 → 미끄러지기를 번갈아, 가끔 목표에서 멈춰 제자리 (지느러미만 살랑)
                f.PhaseLeft -= dt;
                if (f.Hover)
                {
                    if (f.PhaseLeft <= 0f) { f.Hover = false; f.Beat = true; f.PhaseLeft = Random.Range(0.6f, 1.2f); f.Target = NextTarget(f.T.localPosition); }
                }
                else if (dist < 0.12f)
                {
                    if (Random.value < 0.35f) { f.Hover = true; f.PhaseLeft = Random.Range(1.5f, 4f); }
                    else f.Target = NextTarget(f.T.localPosition);
                }
                else if (f.PhaseLeft <= 0f)
                {
                    f.Beat = !f.Beat;
                    f.PhaseLeft = f.Beat ? Random.Range(0.6f, 1.4f) : Random.Range(0.4f, 1.2f);
                }
                float want = f.Hover ? 0f : f.Beat ? cruise * 1.25f : 0f;
                // 꼬리를 칠 땐 빨리 붙고, 미끄러질 땐 물의 저항으로 천천히 준다
                f.Speed = Mathf.MoveTowards(f.Speed, want, (want > f.Speed ? 0.6f : 0.12f) * dt);

                // 방향: 한 번에 돌 수 있는 각속도 제한, 위아래는 ±25°까지, 돌 때는 안쪽으로 몸을 기울인다
                if (dist > 0.02f && !f.Hover)
                {
                    float wantYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    float wantPitch = Mathf.Clamp(-Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg, -25f, 25f);
                    float turn = (f.Beat ? 75f : 40f) * dt;
                    float ny = Mathf.MoveTowardsAngle(f.Yaw, wantYaw, turn);
                    f.YawRate = Mathf.Lerp(f.YawRate, Mathf.DeltaAngle(f.Yaw, ny) / Mathf.Max(dt, 1e-4f), dt * 4f);
                    f.Yaw = ny;
                    f.Pitch = Mathf.MoveTowards(f.Pitch, wantPitch, 30f * dt);
                }
                else
                {
                    f.YawRate = Mathf.Lerp(f.YawRate, 0f, dt * 3f);
                    f.Pitch = Mathf.MoveTowards(f.Pitch, 0f, 15f * dt);   // 멈추면 몸이 수평으로
                }
                f.Bank = Mathf.Lerp(f.Bank, Mathf.Clamp(-f.YawRate * 0.25f, -20f, 20f), dt * 3f);
                f.T.localRotation = Quaternion.Euler(f.Pitch, f.Yaw, f.Bank);
                // 모델 머리 = +Z, 머리가 보는 쪽으로만 나아간다 (돌면서 유리 밖으로 나가지 않게 수조 안에 가둔다)
                var np = f.T.localPosition + f.T.localRotation * Vector3.forward * f.Speed * dt;
                f.T.localPosition = new Vector3(Mathf.Clamp(np.x, -Inner.x * 0.44f, Inner.x * 0.44f),
                    Mathf.Clamp(np.y, BaseY + 0.2f, BaseY + Inner.y * 0.88f), Mathf.Clamp(np.z, -Inner.z * 0.36f, Inner.z * 0.36f));

                // 꼬리 박자는 실제로 미는 힘에 맞춘다: 칠 때 빠르게, 미끄러질 땐 거의 멈춤, 멈추면 제자리 맴돌기 클립
                string clip = f.Hover && f.Speed < 0.03f ? FishModels.IdleHover : FishModels.SwimSlow;
                if (clip != f.Clip) { f.Clip = clip; FishModels.Play(f.Model, clip, 1f, 0.4f); }
                if (clip == FishModels.SwimSlow)
                    FishModels.SetSpeed(f.Model, clip, f.Beat && !f.Hover ? Mathf.Lerp(0.7f, 1.6f, f.Speed / Mathf.Max(0.01f, cruise * 1.25f)) : 0.3f);
            }
            foreach (var b in _bubbles)
            {
                var p = b.localPosition + Vector3.up * 0.25f * Time.deltaTime;
                if (p.y > BaseY + Inner.y * 0.92f) p = new Vector3(Inner.x * 0.4f + Random.Range(-0.05f, 0.05f), BaseY + 0.1f, Random.Range(-0.25f, 0f));
                b.localPosition = p;
            }
            float tt = Time.time;
            for (int i = 0; i < _weeds.Count; i++)
                if (_weeds[i] != null) _weeds[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(tt * 1.3f + i) * 12f);
        }

        /// <summary>0(배부름) ~ 100(배고픔).</summary>
        public static float Hunger(AquaFish f)
        {
            if (Game.Clock == null) return 0f;
            return Mathf.Clamp((Game.Clock.TotalMinutes - f.fedAt) / HungryMinutes * 100f, 0f, 100f);
        }
    }
}
