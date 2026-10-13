using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    public class FishSchool
    {
        public string Name;
        public string[] SpeciesIds;
        public TimeBand[] PreferredTimes = new TimeBand[0];
        public int Col;
        public int Row;
        /// <summary>돌아다닐 수 있는 행 범위.</summary>
        public int MinRow;
        public int MaxRow = 2;
        public float NextRippleAt;
        /// <summary>어군이 실제로 머무는 자리(칸 안의 한 점). 여기에 하얀 소용돌이가 생긴다.</summary>
        public Vector3 Spot;
        /// <summary>이 시각(Time.time)이 되면 다른 자리로 옮겨 간다.</summary>
        public float MoveAt;
        public SchoolSwirlFx Fx;

        /// <summary>
        /// 정해진 시간에만 정해진 칸에 나타나는 특별 어군 (원작 자이언트 그루퍼: 저녁 C4, 아침 E3).
        /// 해당 시간대가 아니면 숨는다(<see cref="Hidden"/>).
        /// </summary>
        public (TimeBand band, int col, int row)[] Schedule;
        public bool Special;
        public bool Hidden;

        /// <summary>큰 어군이면 true. 큰 어군에서 <see cref="FishSchoolManager.CatchesToShrink"/>마리를 낚으면 작은 어군이 된다.</summary>
        public bool Big = true;
        public int CatchesLeft = FishSchoolManager.CatchesToShrink;
        /// <summary>작은 어군이 된 게임 시각(TotalMinutes). 일정 시간이 지나면 다시 큰 어군으로 모인다.</summary>
        public float ShrankAt;

        public bool Contains(string speciesId) => System.Array.IndexOf(SpeciesIds, speciesId) >= 0;

        /// <summary>해당 어종의 입질 가중치 배율.</summary>
        public float BiteBoost => Big ? 4f : 2f;
        /// <summary>입질까지 기다리는 시간 배율.</summary>
        public float WaitFactor => Big ? 0.5f : 0.75f;
        /// <summary>길이 분포 지수 (작을수록 큰 고기가 잘 나온다).</summary>
        public float LengthSkew => Big ? 1.7f : 2f;
        public string SizeLabel => Special ? "대물 어군" : Big ? "큰 어군" : "작은 어군";
        /// <summary>소용돌이 반지름(m).</summary>
        public float SpotRadius => Special ? 4.5f : Big ? 3.5f : 2.2f;

        /// <summary>찌가 소용돌이 안(어군 한가운데)에 떨어졌는지.</summary>
        public bool InSpot(Vector3 p)
        {
            var d = p - Spot;
            d.y = 0f;
            return d.magnitude <= SpotRadius * 1.3f;
        }
    }

    /// <summary>
    /// 어군. 맵마다 다르다(<see cref="FieldMapDef.CreateSchools"/>).
    /// 원작처럼 한 자리에 5~10분(실제 시간) 머물며 물 위에 하얀 소용돌이를 남기고,
    /// 그다음 이웃 칸으로 옮겨 간다. 선호 시간대에는 해안·둔치 쪽으로 붙는다.
    /// 어군이 있는 칸에 찌를 던지면 입질이 빠르고 해당 어종이 잘 낚인다. 소용돌이 한가운데면 더 빠르다.
    /// 원작처럼 큰 어군에서 3마리를 낚으면 작은 어군으로 흩어지고, 6시간 뒤 다시 모인다.
    /// 시간표가 있는 특별 어군은 정해진 시간·칸에만 나타난다.
    /// </summary>
    public class FishSchoolManager : MonoBehaviour
    {
        public const int CatchesToShrink = 3;
        public const float RegroupMinutes = 6 * 60f;
        /// <summary>한 자리에 머무는 실제 시간(초). 원작처럼 5~10분.</summary>
        public const float StayMinSeconds = 5 * 60f, StayMaxSeconds = 10 * 60f;

        public readonly List<FishSchool> Schools = new List<FishSchool>();
        TimeBand _lastBand;

        /// <summary>낚시터가 바뀌면 그 맵의 어군으로 바꾼다.</summary>
        public void Reset(List<FishSchool> schools)
        {
            foreach (var s in Schools)
                if (s.Fx != null) Destroy(s.Fx.gameObject);
            Schools.Clear();
            Schools.AddRange(schools);
            if (Game.Clock != null) ApplySchedules(Game.Clock.Band);
        }

        void Start()
        {
            if (Schools.Count == 0) Reset(FieldMap.Current.CreateSchools());
            Game.Clock.HourChanged += OnHourChanged;
        }

        void OnDestroy()
        {
            if (Game.Clock != null) Game.Clock.HourChanged -= OnHourChanged;
        }

        void ApplySchedules(TimeBand band)
        {
            _lastBand = band;
            foreach (var s in Schools)
            {
                if (s.Schedule == null) continue;
                s.Hidden = true;
                int oldCol = s.Col, oldRow = s.Row;
                foreach (var (b, col, row) in s.Schedule)
                    if (b == band) { s.Col = col; s.Row = row; s.Hidden = false; }
                if (!s.Hidden && (s.Col != oldCol || s.Row != oldRow)) PickSpot(s);
            }
        }

        void OnHourChanged()
        {
            var band = Game.Clock.Band;
            if (band != _lastBand)
            {
                ApplySchedules(band);
                foreach (var s in Schools)
                    if (s.Schedule != null && !s.Hidden && Game.Mode == GameMode.Field)
                        Game.Toast($"{s.Name} 어군이 {FieldMap.CellName(s.Col, s.Row)} 근처에 나타났다는 소문이 돈다!", ToastKind.Great);
            }
            if (Game.Clock.Hour % 2 != 0) return;
            foreach (var s in Schools)
            {
                if (!s.Big && Game.Clock.TotalMinutes - s.ShrankAt >= RegroupMinutes)
                {
                    s.Big = true;
                    s.CatchesLeft = CatchesToShrink;
                    if (Game.Mode == GameMode.Field) Game.Toast($"{s.Name}가 다시 큰 어군으로 모였습니다.", ToastKind.Info);
                }
            }
        }

        /// <summary>머무는 시간이 끝나면 이웃 칸으로 옮긴다. 활동 시간대엔 해안 쪽, 아니면 깊은 쪽으로.</summary>
        void Move(FishSchool s)
        {
            bool active = System.Array.IndexOf(s.PreferredTimes, Game.Clock.Band) >= 0;
            s.Col = Mathf.Clamp(s.Col + Random.Range(-1, 2), 0, FieldMap.Cols - 1);
            int span = Mathf.Max(1, s.MaxRow - s.MinRow);
            int targetRow = active ? s.MinRow + Random.Range(0, Mathf.Max(1, span / 2 + 1)) : s.MinRow + span / 2 + Random.Range(0, span / 2 + 1);
            s.Row = Mathf.Clamp(s.Row + (int)Mathf.Sign(targetRow - s.Row), s.MinRow, s.MaxRow);
            PickSpot(s);
        }

        /// <summary>칸 안에서 물 위의 한 점을 골라 머물 자리로 정하고, 머무는 시간을 새로 잰다.</summary>
        static void PickSpot(FishSchool s)
        {
            var center = FieldMap.CellCenter(s.Col, s.Row);
            s.Spot = center;
            for (int i = 0; i < 12; i++)
            {
                var p = center + new Vector3(Random.Range(-FieldMap.CellW * 0.3f, FieldMap.CellW * 0.3f), 0f,
                    Random.Range(-FieldMap.CellH * 0.28f, FieldMap.CellH * 0.28f));
                if (FieldMap.IsWater(p)) { s.Spot = p; break; }
            }
            s.Spot.y = FieldMap.WaterY;
            s.MoveAt = Time.time + Random.Range(StayMinSeconds, StayMaxSeconds);
        }

        /// <summary>어군 칸에서 그 어군의 어종을 낚았을 때. 큰 어군은 3마리째에 작은 어군이 된다.</summary>
        public void OnCatch(FishSchool school, string speciesId)
        {
            if (school == null || school.Special || !school.Big || !school.Contains(speciesId)) return;
            school.CatchesLeft--;
            if (school.CatchesLeft > 0) return;
            school.Big = false;
            school.ShrankAt = Game.Clock.TotalMinutes;
            Game.Toast($"{school.Name}가 흩어져 작은 어군이 되었습니다. 6시간 뒤 다시 모입니다.", ToastKind.Warn);
        }

        public FishSchool At(int col, int row)
        {
            foreach (var s in Schools)
                if (!s.Hidden && s.Col == col && s.Row == row) return s;
            return null;
        }

        void Update()
        {
            if (Game.Clock != null && Game.Clock.Band != _lastBand) ApplySchedules(Game.Clock.Band);
            bool field = Game.Mode == GameMode.Field;
            foreach (var s in Schools)
            {
                if (s.MoveAt <= 0f) PickSpot(s);
                else if (s.Schedule == null && Time.time >= s.MoveAt) Move(s);
                if (s.Fx == null) s.Fx = SchoolSwirlFx.Create();
                s.Fx.Show(field && !s.Hidden ? s.Spot : (Vector3?)null, s.SpotRadius);

                // 소용돌이 안에서 가끔 물고기가 튀어 올라 파문이 생긴다. 작은 어군은 덜 자주, 작게 튄다
                if (!field || s.Hidden || Time.time < s.NextRippleAt) continue;
                s.NextRippleAt = Time.time + (s.Big ? Random.Range(2.5f, 7f) : Random.Range(6f, 14f));
                var off = Random.insideUnitCircle * s.SpotRadius * 0.8f;
                var pos = s.Spot + new Vector3(off.x, 0f, off.y);
                if (!FieldMap.IsWater(pos)) continue;
                float big = s.Special ? 1.6f : 1f;
                Effects.Ripple(pos, (s.Big ? Random.Range(1.5f, 3f) : Random.Range(0.8f, 1.6f)) * big, 2f);
                Effects.Splash(pos, s.Big ? 6 : 3, (s.Big ? 0.6f : 0.4f) * big);
            }
        }
    }

    /// <summary>
    /// 어군 자리에 떠 있는 하얀 소용돌이. 두 겹이 서로 반대로 천천히 돌며 숨 쉬듯 진해졌다 옅어진다.
    /// 자리가 바뀌면 3초에 걸쳐 사라졌다가 새 자리에서 다시 떠오른다.
    /// </summary>
    public class SchoolSwirlFx : MonoBehaviour
    {
        const float FadeSeconds = 3f;

        Transform _swirl, _foam;
        Material _swirlMat, _foamMat;
        Vector3 _target;
        bool _visible;
        float _alpha, _radius = 3f, _targetRadius = 3f;
        float _seed;

        public static SchoolSwirlFx Create()
        {
            var fx = new GameObject("SchoolSwirl").AddComponent<SchoolSwirlFx>();
            fx._swirl = fx.Layer("Swirl", out fx._swirlMat);
            fx._foam = fx.Layer("Foam", out fx._foamMat);
            fx._seed = Random.value * 100f;
            return fx;
        }

        Transform Layer(string name, out Material mat)
        {
            mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = ProceduralTextures.SchoolSwirl };
            mat.SetFloat("_InvFade", 3f); // 소프트 파티클이 수면 바로 위에서 거의 투명해지지 않게
            var go = Mats.Prim(PrimitiveType.Quad, name, transform, Vector3.zero, Vector3.one, mat, collider: false);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>spot이 null이면 사라진다.</summary>
        public void Show(Vector3? spot, float radius)
        {
            _visible = spot.HasValue;
            if (_visible) _target = new Vector3(spot.Value.x, FieldMap.WaterY + 0.12f, spot.Value.z);
            _targetRadius = radius;
        }

        void Update()
        {
            bool moving = (transform.position - _target).sqrMagnitude > 0.01f;
            _alpha = Mathf.MoveTowards(_alpha, _visible && !moving ? 1f : 0f, Time.deltaTime / FadeSeconds);
            if (moving && _alpha <= 0f) transform.position = _target;
            _radius = Mathf.Lerp(_radius, _targetRadius, Time.deltaTime);

            float t = Time.time + _seed;
            float breathe = 0.8f + 0.2f * Mathf.Sin(t * 0.9f);
            _swirl.localRotation = Quaternion.Euler(90f, t * 14f, 0f);
            _swirl.localScale = Vector3.one * _radius * 2f * (0.97f + 0.03f * Mathf.Sin(t * 1.3f));
            _foam.localRotation = Quaternion.Euler(90f, 60f - t * 9f, 0f);
            _foam.localScale = Vector3.one * _radius * 2.5f;
            // 파티클 셰이더는 색을 2배 하므로 0.5가 흰색
            _swirlMat.SetColor("_TintColor", new Color(0.5f, 0.52f, 0.55f, 0.5f * _alpha * breathe));
            _foamMat.SetColor("_TintColor", new Color(0.5f, 0.52f, 0.55f, 0.3f * _alpha * (1.4f - breathe)));
        }

        void OnDestroy()
        {
            if (_swirlMat != null) Destroy(_swirlMat);
            if (_foamMat != null) Destroy(_foamMat);
        }
    }
}
