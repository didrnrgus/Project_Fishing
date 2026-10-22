using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 원작 어군 3종 (게임메카 동남아 업데이트 기사): 고기떼(입질이 빠르지만 횟수 제한) /
    /// 히든 어군(고기떼를 모두 낚으면 나타난다) / 포식자 어군(그 지역의 어군을 모두 흩어 놓고, 포식자를 낚으면 어군이 다시 나타난다).
    /// 그림자: 원작처럼 퀘스트 고기나 귀한 고기 한 마리가 물속에 검은 실루엣으로 보이며 천천히 헤엄쳐 다닌다.
    /// </summary>
    public enum SchoolKind { Normal, Hidden, Predator, Shadow }

    public class FishSchool
    {
        public SchoolKind Kind;
        /// <summary>히든·포식자 어군이 사라지는 게임 시각(TotalMinutes).</summary>
        public float ExpiresAt;
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

        /// <summary>그림자: 지금 헤엄치는 방향(수평 단위 벡터)·향하는 곳·헤엄 속도(m/s).</summary>
        public Vector3 Heading = Vector3.forward;
        public Vector3 Waypoint;
        public float Speed;
        /// <summary>그림자 고기가 진행 중인 퀘스트의 목표 어종이다 (이름을 밝히고 미니맵에 퀘스트 표시).</summary>
        public bool Quest;
        /// <summary>그림자 고기가 바늘에 걸려 파이팅 중이다 (실루엣·소용돌이를 감춘다).</summary>
        public bool Hooked;
        public FishShadowFx Silhouette;

        public bool Shadow => Kind == SchoolKind.Shadow;

        public bool Contains(string speciesId) => System.Array.IndexOf(SpeciesIds, speciesId) >= 0;

        /// <summary>해당 어종의 입질 가중치 배율.</summary>
        public float BiteBoost => Kind == SchoolKind.Shadow ? 1f : Kind == SchoolKind.Hidden ? 6f : Kind == SchoolKind.Predator ? 3f : Big ? 4f : 2f;
        /// <summary>입질까지 기다리는 시간 배율.</summary>
        public float WaitFactor => Kind == SchoolKind.Shadow ? 0.45f : Kind == SchoolKind.Hidden ? 0.45f : Kind == SchoolKind.Predator ? 0.65f : Big ? 0.5f : 0.75f;
        /// <summary>길이 분포 지수 (작을수록 큰 고기가 잘 나온다).</summary>
        public float LengthSkew => Kind == SchoolKind.Shadow ? 1.2f : Kind == SchoolKind.Hidden ? 1.4f : Kind == SchoolKind.Predator ? 1.3f : Big ? 1.7f : 2f;
        public string SizeLabel => Kind == SchoolKind.Shadow ? (Quest ? "퀘스트 고기" : "정체 모를 고기") : Kind == SchoolKind.Hidden ? "히든 어군" : Kind == SchoolKind.Predator ? "포식자 어군" : Special ? "대물 어군" : Big ? "큰 어군" : "작은 어군";
        /// <summary>지도 표시용 짧은 이름.</summary>
        public string ShortLabel => Kind == SchoolKind.Shadow ? "그림자" : Kind == SchoolKind.Hidden ? "히든" : Kind == SchoolKind.Predator ? "포식자" : Special ? "대물" : Big ? "대" : "소";
        /// <summary>소용돌이 반지름(m).</summary>
        public float SpotRadius => Kind == SchoolKind.Shadow ? 3f : Kind != SchoolKind.Normal ? 4f : Special ? 4.5f : Big ? 3.5f : 2.2f;
        /// <summary>소용돌이·지도 색: 히든은 금빛, 포식자는 붉은빛, 그림자는 물빛.</summary>
        public Color Tint => Kind == SchoolKind.Shadow ? new Color(0.6f, 0.88f, 1f) : Kind == SchoolKind.Hidden ? new Color(1f, 0.85f, 0.35f) : Kind == SchoolKind.Predator ? new Color(1f, 0.4f, 0.35f) : Color.white;

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
    /// 가끔 퀘스트 고기·귀한 고기 한 마리가 물속 검은 그림자로 나타나 천천히 헤엄쳐 다닌다(<see cref="SchoolKind.Shadow"/>).
    /// </summary>
    public class FishSchoolManager : MonoBehaviour
    {
        public const int CatchesToShrink = 3;
        public const float RegroupMinutes = 6 * 60f;
        /// <summary>한 자리에 머무는 실제 시간(초). 원작처럼 5~10분.</summary>
        public const float StayMinSeconds = 5 * 60f, StayMaxSeconds = 10 * 60f;

        public readonly List<FishSchool> Schools = new List<FishSchool>();
        TimeBand _lastBand;

        /// <summary>히든 어군이 머무는 게임 시간(분) · 포식자 어군이 머무는 게임 시간(분).</summary>
        public const float HiddenMinutes = 120f, PredatorMinutes = 180f;
        /// <summary>포식자가 나타날 확률 (게임 2시간마다 굴린다).</summary>
        public const float PredatorChance = 0.18f;

        /// <summary>그림자가 머무는 게임 시간(분) · 사라진 뒤 다시 나타날 수 있기까지(분).</summary>
        public const float ShadowMinutes = 300f, ShadowCooldownMinutes = 60f;
        /// <summary>그림자가 나타날 확률 (게임 1시간마다): 진행 중인 퀘스트 고기가 이 낚시터에 살면 높고, 아니면 귀한 고기가 가끔.</summary>
        public const float ShadowQuestChance = 0.3f, ShadowRareChance = 0.08f;
        /// <summary>그림자 헤엄 속도(m/s). 원작처럼 천천히 어슬렁거린다.</summary>
        public const float ShadowSpeed = 0.55f;
        float _shadowReadyAt;

        /// <summary>낚시터가 바뀌면 그 맵의 어군으로 바꾼다. 히든·포식자 어군 자리도 하나씩 만들어 둔다 (처음엔 숨어 있음).</summary>
        public void Reset(List<FishSchool> schools)
        {
            foreach (var s in Schools)
            {
                if (s.Fx != null) Destroy(s.Fx.gameObject);
                if (s.Silhouette != null) Destroy(s.Silhouette.gameObject);
            }
            Schools.Clear();
            Schools.AddRange(schools);
            var hidden = MakeKind(SchoolKind.Hidden, schools);
            if (hidden != null) Schools.Add(hidden);
            var predator = MakeKind(SchoolKind.Predator, schools);
            if (predator != null) Schools.Add(predator);
            if (schools.Count > 0)
                Schools.Add(new FishSchool { Kind = SchoolKind.Shadow, Hidden = true, Name = "검은 그림자", SpeciesIds = new string[0],
                    MinRow = schools[0].MinRow, MaxRow = schools[0].MaxRow, Col = schools[0].Col, Row = schools[0].Row, CatchesLeft = 1 });
            _shadowReadyAt = 0f;
            if (Game.Clock != null) ApplySchedules(Game.Clock.Band);
        }

        /// <summary>
        /// 히든: 그 낚시터의 가장 귀한 어종 둘(희귀도 3 이상). 포식자: 가장 힘센 큰 고기 하나.
        /// 대물 퀘스트 전용(SchoolOnly)·먼바다 전용 어종은 뺀다.
        /// </summary>
        static FishSchool MakeKind(SchoolKind kind, List<FishSchool> normal)
        {
            var dex = new List<FishSpecies>();
            foreach (var sp in FishDatabase.DexOf(FieldMap.Id))
                if (!sp.SchoolOnly && !sp.FarSeaOnly && !sp.Boss && sp.Rarity <= 4 && sp.Id != "smelt") dex.Add(sp);
            if (dex.Count == 0 || normal.Count == 0) return null;
            string[] ids;
            if (kind == SchoolKind.Hidden)
            {
                dex.RemoveAll(sp => sp.Rarity < 3);
                if (dex.Count == 0) return null;
                dex.Sort((a, b) => b.Rarity != a.Rarity ? b.Rarity.CompareTo(a.Rarity) : b.MaxLen.CompareTo(a.MaxLen));
                ids = dex.Count >= 2 ? new[] { dex[0].Id, dex[1].Id } : new[] { dex[0].Id };
            }
            else
            {
                dex.RemoveAll(sp => sp.Rarity < 2 || sp.MaxLen < 50f);
                if (dex.Count == 0) return null;
                dex.Sort((a, b) => (b.Power * b.MaxLen).CompareTo(a.Power * a.MaxLen));
                ids = new[] { dex[0].Id };
            }
            var first = normal[0];
            return new FishSchool
            {
                Kind = kind, Hidden = true,
                Name = kind == SchoolKind.Hidden ? "히든 어군" : FishDatabase.Get(ids[0]).Name + " 포식자",
                SpeciesIds = ids, MinRow = first.MinRow, MaxRow = first.MaxRow, Col = first.Col, Row = first.Row,
                CatchesLeft = kind == SchoolKind.Hidden ? 3 : 1,
            };
        }

        FishSchool OfKind(SchoolKind kind) => Schools.Find(s => s.Kind == kind);

        /// <summary>포식자가 나타나면 고기떼(일반 어군)가 모두 작은 어군으로 흩어진다.</summary>
        void PredatorArrives(FishSchool pred, FishSchool near)
        {
            Appear(pred, near, PredatorMinutes);
            foreach (var s in Schools)
            {
                if (s.Kind != SchoolKind.Normal || s.Special || !s.Big) continue;
                s.Big = false;
                s.CatchesLeft = 0;
                s.ShrankAt = Game.Clock.TotalMinutes;
            }
        }

        /// <summary>포식자를 낚아 쫓아내면 흩어졌던 고기떼가 바로 다시 모인다.</summary>
        void RegroupAll()
        {
            foreach (var s in Schools)
            {
                if (s.Kind != SchoolKind.Normal || s.Big) continue;
                s.Big = true;
                s.CatchesLeft = CatchesToShrink;
            }
        }

        /// <summary>히든·포식자 어군을 나타나게 한다. near가 있으면 그 옆 칸에.</summary>
        void Appear(FishSchool s, FishSchool near, float minutes)
        {
            s.Hidden = false;
            s.Big = true;
            s.CatchesLeft = s.Kind == SchoolKind.Hidden ? 3 : 1;
            s.ExpiresAt = Game.Clock.TotalMinutes + minutes;
            for (int i = 0; i < 16; i++)
            {
                int col = near != null ? Mathf.Clamp(near.Col + Random.Range(-1, 2), 0, FieldMap.Cols - 1) : Random.Range(0, FieldMap.Cols);
                int row = near != null ? Mathf.Clamp(near.Row + Random.Range(-1, 2), s.MinRow, s.MaxRow) : Random.Range(s.MinRow, s.MaxRow + 1);
                if (!FieldMap.IsWater(FieldMap.CellCenter(col, row))) continue;
                s.Col = col; s.Row = row;
                if (near == null || col != near.Col || row != near.Row) break;
            }
            PickSpot(s);
        }

        void Vanish(FishSchool s, string why)
        {
            s.Hidden = true;
            s.Hooked = false;
            if (s.Shadow && Game.Clock != null) _shadowReadyAt = Game.Clock.TotalMinutes + ShadowCooldownMinutes;
            if (why != null && Game.Mode == GameMode.Field) Game.Toast(why, ToastKind.Info);
        }

        /// <summary>테스트·자동 촬영용: 히든/포식자 어군·그림자를 바로 띄운다. 그림자는 near가 있으면 그 둘레에.</summary>
        public FishSchool DebugAppear(SchoolKind kind, Vector3? near = null)
        {
            var s = OfKind(kind);
            if (s == null) return null;
            if (kind == SchoolKind.Shadow) return !s.Hidden || TryShadow(true, near) ? s : null;
            if (kind == SchoolKind.Predator) PredatorArrives(s, Schools.Find(x => x.Kind == SchoolKind.Normal && !x.Special));
            else Appear(s, null, HiddenMinutes);
            return s;
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
            float now = Game.Clock.TotalMinutes;
            foreach (var s in Schools)
                if (s.Kind != SchoolKind.Normal && !s.Hidden && !s.Hooked && now >= s.ExpiresAt)
                    Vanish(s, s.Kind == SchoolKind.Hidden ? "히든 어군이 깊은 곳으로 사라졌습니다."
                        : s.Shadow ? "물속을 어슬렁거리던 검은 그림자가 깊은 곳으로 사라졌습니다." : $"{s.Name}가 먼바다로 떠났습니다.");
            TryShadow(false, null);
            if (Game.Clock.Hour % 2 != 0) return;

            // 포식자: 가끔 큰 고기가 나타나 고기떼를 모두 흩어 놓는다 (원작: 그 지역 어군이 모두 사라진다)
            var pred = OfKind(SchoolKind.Predator);
            if (pred != null && pred.Hidden && Random.value < PredatorChance)
            {
                var prey = Schools.FindAll(x => x.Kind == SchoolKind.Normal && !x.Special && !x.Hidden && x.Big);
                if (prey.Count > 0)
                {
                    PredatorArrives(pred, prey[Random.Range(0, prey.Count)]);
                    if (Game.Mode == GameMode.Field)
                        Game.Toast($"{pred.Name}가 {FieldMap.CellName(pred.Col, pred.Row)}에 나타나 고기떼가 모두 흩어졌습니다! 포식자를 낚으면 어군이 돌아옵니다.", ToastKind.Great);
                }
            }

            foreach (var s in Schools)
            {
                if (s.Kind != SchoolKind.Normal) continue;
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

        // ---------- 물속 그림자 ----------

        /// <summary>그림자가 될 수 있는 어종: 그 낚시터에 살고, 어군 전용·먼바다 전용·괴물이 아닌 고기.</summary>
        static bool ShadowCandidate(FishSpecies sp) => !sp.SchoolOnly && !sp.FarSeaOnly && !sp.Boss && sp.Id != "smelt";

        /// <summary>
        /// 원작: 퀘스트 고기나 귀한 고기는 어군과 함께 물속에 검은 실루엣이 보인다.
        /// 진행 중인 퀘스트가 요구하는 어종이 이 낚시터에 살면 그 고기가, 아니면 귀한 고기(희귀도 3 이상)가 나타난다.
        /// 나타나면 공지(토스트)와 시스템 줄로 알리고, 미니맵에 실루엣을 띄운다. force면 확률·쿨타임을 무시한다.
        /// </summary>
        bool TryShadow(bool force, Vector3? near)
        {
            var s = OfKind(SchoolKind.Shadow);
            if (s == null || !s.Hidden) return false;
            if (!force && (Game.Mode != GameMode.Field || Game.Clock.TotalMinutes < _shadowReadyAt)) return false;
            var quest = new List<FishSpecies>();
            var rare = new List<FishSpecies>();
            foreach (var sp in FishDatabase.DexOf(FieldMap.Id))
            {
                if (!ShadowCandidate(sp)) continue;
                if (Game.Quests != null && Game.Quests.IsQuestSpecies(sp.Id)) quest.Add(sp);
                else if (sp.Rarity >= 3) rare.Add(sp);
            }
            bool isQuest = quest.Count > 0;
            var pool = isQuest ? quest : rare;
            if (pool.Count == 0 || (!force && Random.value >= (isQuest ? ShadowQuestChance : ShadowRareChance))) return false;
            var pick = pool[Random.Range(0, pool.Count)];
            if (!ShadowStart(near ?? (Game.Player != null ? Game.Player.transform.position : FieldMap.Spawn), out var start)) return false;

            s.SpeciesIds = new[] { pick.Id };
            s.Quest = isQuest;
            s.Name = isQuest ? $"{pick.Name}의 그림자" : "검은 그림자";
            s.Hidden = false;
            s.Hooked = false;
            s.Big = true;
            s.CatchesLeft = 1;
            s.ExpiresAt = Game.Clock.TotalMinutes + ShadowMinutes;
            s.Spot = start;
            s.Waypoint = start;
            s.Speed = 0f;
            var a = Random.Range(0f, Mathf.PI * 2f);
            s.Heading = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            s.MoveAt = Time.time;
            FieldMap.TryCell(start, out s.Col, out s.Row);

            string cell = FieldMap.CellName(s.Col, s.Row);
            string msg = isQuest
                ? $"{cell} 근처 물속에 커다란 검은 그림자가 어른거립니다! 퀘스트 목표 [{pick.Name}]인 것 같습니다."
                : $"{cell} 근처 물속에 커다란 검은 그림자가 어른거립니다… 귀한 고기일지도 모릅니다!";
            if (Game.Mode == GameMode.Field)
            {
                Game.Toast(msg, ToastKind.Great);
                Game.SysLog(msg, ShadowLogColor);
            }
            return true;
        }

        const string ShadowLogColor = "#9ae0ff";

        /// <summary>그림자가 헤엄칠 수 있는 곳: 격자 안의 물. 먼바다 포인트는 빼서 물가나 배에서 닿게 한다.</summary>
        static bool ShadowWater(Vector3 p) => FieldMap.TryCell(p, out _, out _) && FieldMap.IsWater(p) && !FieldMap.IsFarSea(p);

        /// <summary>나타날 자리: 눈에 띄게 플레이어(또는 near)에서 14~34m 떨어진 물 위, 안 되면 어군이 다니는 칸 어디든.</summary>
        bool ShadowStart(Vector3 around, out Vector3 p)
        {
            for (int i = 0; i < 40; i++)
            {
                var d = Random.insideUnitCircle.normalized * Random.Range(14f, 34f);
                p = new Vector3(around.x + d.x, FieldMap.WaterY, around.z + d.y);
                if (ShadowWater(p)) return true;
            }
            var first = Schools.Find(x => x.Kind == SchoolKind.Normal);
            int minRow = first != null ? first.MinRow : 0, maxRow = first != null ? first.MaxRow : FieldMap.Rows - 1;
            for (int i = 0; i < 40; i++)
            {
                var c = FieldMap.CellCenter(Random.Range(0, FieldMap.Cols), Random.Range(minRow, maxRow + 1));
                p = c + new Vector3(Random.Range(-0.4f, 0.4f) * FieldMap.CellW, 0f, Random.Range(-0.4f, 0.4f) * FieldMap.CellH);
                p.y = FieldMap.WaterY;
                if (ShadowWater(p)) return true;
            }
            p = default;
            return false;
        }

        /// <summary>
        /// 그림자는 소용돌이처럼 자리를 옮겨 다니지 않고 계속 천천히 헤엄친다: 6~18m 앞의 물 위 한 점을 골라 고개를 돌려 가며 다가가고,
        /// 닿거나 20~40초가 지나면 다음 점을 고른다. 앞이 땅이면 바로 돌아선다. 가끔 멈칫하듯 느려진다.
        /// </summary>
        static void SwimShadow(FishSchool s, float dt)
        {
            var to = s.Waypoint - s.Spot;
            to.y = 0f;
            bool blocked = !ShadowWater(s.Spot + s.Heading * 2.5f);
            if (to.magnitude < 1.5f || Time.time >= s.MoveAt || blocked)
            {
                for (int i = 0; i < 16; i++)
                {
                    // 막혔으면 아무 쪽으로, 아니면 대체로 가던 쪽(앞 ±100°)으로
                    float yaw = Mathf.Atan2(s.Heading.z, s.Heading.x) + (blocked ? Random.Range(-Mathf.PI, Mathf.PI) : Random.Range(-1.75f, 1.75f));
                    var p = s.Spot + new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * Random.Range(6f, 18f);
                    if (!ShadowWater(p) || !ShadowWater((s.Spot + p) * 0.5f)) continue;
                    s.Waypoint = p;
                    break;
                }
                s.MoveAt = Time.time + Random.Range(20f, 40f);
                to = s.Waypoint - s.Spot;
                to.y = 0f;
            }
            if (to.sqrMagnitude > 0.01f)
                s.Heading = Vector3.RotateTowards(s.Heading, to.normalized, (blocked ? 1.6f : 0.45f) * dt, 0f).normalized;
            // 0.15~1배 사이로 느려졌다 빨라졌다 (꼬리를 몇 번 치고 미끄러지는 느낌)
            float pace = 0.15f + 0.85f * Mathf.SmoothStep(0f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 0.23f + s.Waypoint.x));
            s.Speed = Mathf.MoveTowards(s.Speed, blocked ? ShadowSpeed * 0.3f : ShadowSpeed * pace, dt * 0.5f);
            var next = s.Spot + s.Heading * s.Speed * dt;
            if (ShadowWater(next)) s.Spot = next;
            s.Spot.y = FieldMap.WaterY;
            FieldMap.TryCell(s.Spot, out s.Col, out s.Row);
        }

        /// <summary>그림자 고기가 바늘에 걸렸다: 파이팅 동안 실루엣을 감춘다.</summary>
        public void OnHooked(FishSchool school, string speciesId)
        {
            if (school != null && school.Shadow && !school.Hidden && school.Contains(speciesId)) school.Hooked = true;
        }

        /// <summary>그림자 고기를 놓쳤다: 다시 물속에서 어슬렁거린다 (놀라서 반대쪽으로).</summary>
        public void OnLost(FishSchool school)
        {
            if (school == null || !school.Shadow || !school.Hooked) return;
            school.Hooked = false;
            school.Heading = -school.Heading;
            school.MoveAt = 0f;
            if (Game.Mode == GameMode.Field) Game.Toast("놓친 그림자가 다시 물속을 어슬렁거립니다. 한 번 더 노려 보세요!", ToastKind.Info);
        }

        /// <summary>어군 칸에서 그 어군의 어종을 낚았을 때. 큰 어군은 3마리째에 작은 어군이 된다.</summary>
        public void OnCatch(FishSchool school, string speciesId)
        {
            if (school == null || school.Special || !school.Big || !school.Contains(speciesId)) return;
            school.CatchesLeft--;
            if (school.CatchesLeft > 0) return;
            if (school.Shadow)
            {
                Vanish(school, null);
                var sp = FishDatabase.Get(speciesId);
                Game.Toast($"물속 그림자의 정체는 [{sp.Name}]{QuestNpcs.Josa(sp.Name, "이었", "였")}습니다!", ToastKind.Great);
                return;
            }
            if (school.Kind == SchoolKind.Predator)
            {
                Vanish(school, null);
                RegroupAll();
                Game.Toast($"{school.Name}를 쫓아냈습니다! 흩어졌던 고기떼가 다시 큰 어군으로 모였습니다.", ToastKind.Great);
                return;
            }
            if (school.Kind == SchoolKind.Hidden)
            {
                Vanish(school, "히든 어군을 모두 낚아 흩어졌습니다!");
                return;
            }
            school.Big = false;
            school.ShrankAt = Game.Clock.TotalMinutes;
            Game.Toast($"{school.Name}가 흩어져 작은 어군이 되었습니다. 6시간 뒤 다시 모입니다.", ToastKind.Warn);

            // 원작: 큰 어군이 모두 흩어지면 숨어 있던 히든 어군이 나타난다
            var hidden = OfKind(SchoolKind.Hidden);
            if (hidden != null && hidden.Hidden && Schools.TrueForAll(s => s.Kind != SchoolKind.Normal || s.Special || !s.Big))
            {
                Appear(hidden, null, HiddenMinutes);
                var names = new List<string>();
                foreach (var id in hidden.SpeciesIds) names.Add(FishDatabase.Get(id).Name);
                Game.Toast($"큰 어군이 모두 흩어지자 숨어 있던 히든 어군({string.Join("·", names)})이 {FieldMap.CellName(hidden.Col, hidden.Row)}에 나타났습니다!", ToastKind.Great);
            }
        }

        /// <summary>그 칸의 어군 (그림자는 칸이 아니라 실루엣 둘레에서만 치므로 뺀다).</summary>
        public FishSchool At(int col, int row)
        {
            foreach (var s in Schools)
                if (!s.Hidden && !s.Shadow && s.Col == col && s.Row == row) return s;
            return null;
        }

        /// <summary>찌가 떨어진 자리의 어군: 물속 그림자 바로 둘레면 그림자, 아니면 그 칸의 어군.</summary>
        public FishSchool At(Vector3 p, int col, int row)
        {
            foreach (var s in Schools)
                if (s.Shadow && !s.Hidden && !s.Hooked && s.InSpot(p)) return s;
            return At(col, row);
        }

        void Update()
        {
            if (Game.Clock != null && Game.Clock.Band != _lastBand) ApplySchedules(Game.Clock.Band);
            bool field = Game.Mode == GameMode.Field;
            foreach (var s in Schools)
            {
                if (s.Shadow)
                {
                    if (!s.Hidden && !s.Hooked) SwimShadow(s, Time.deltaTime);
                    bool show = field && !s.Hidden && !s.Hooked;
                    if (s.Fx == null) s.Fx = SchoolSwirlFx.Create();
                    s.Fx.Show(show ? s.Spot : (Vector3?)null, s.SpotRadius, s.Tint, follow: true);
                    if (s.Silhouette == null) s.Silhouette = FishShadowFx.Create();
                    var sp = s.SpeciesIds.Length > 0 ? FishDatabase.Get(s.SpeciesIds[0]) : null;
                    s.Silhouette.Show(show ? s.Spot : (Vector3?)null, s.Heading, s.Speed, sp != null ? FishShadowFx.LengthFor(sp) : 1.5f);
                    continue;
                }
                if (s.MoveAt <= 0f) PickSpot(s);
                else if (s.Schedule == null && Time.time >= s.MoveAt) Move(s);
                if (s.Fx == null) s.Fx = SchoolSwirlFx.Create();
                s.Fx.Show(field && !s.Hidden ? s.Spot : (Vector3?)null, s.SpotRadius, s.Tint);

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
        Color _tint = Color.white;
        bool _visible, _follow;
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

        /// <summary>spot이 null이면 사라진다. follow면 자리가 바뀌어도 사라지지 않고 따라간다 (헤엄치는 그림자).</summary>
        public void Show(Vector3? spot, float radius, Color tint, bool follow = false)
        {
            _tint = tint;
            _follow = follow;
            _visible = spot.HasValue;
            if (_visible) _target = new Vector3(spot.Value.x, FieldMap.WaterY + 0.12f, spot.Value.z);
            _targetRadius = radius;
        }

        void Update()
        {
            bool moving = (transform.position - _target).sqrMagnitude > 0.01f;
            // 따라가는 소용돌이: 보이는 동안엔 그대로 붙어 가고, 새로 나타날 때만 옛 자리에서 사라졌다가 옮겨 온다
            if (_follow && _visible && _alpha > 0f) { transform.position = _target; moving = false; }
            _alpha = Mathf.MoveTowards(_alpha, _visible && !moving ? (_follow ? 0.55f : 1f) : 0f, Time.deltaTime / FadeSeconds);
            if (moving && _alpha <= 0f) transform.position = _target;
            _radius = Mathf.Lerp(_radius, _targetRadius, Time.deltaTime);

            float t = Time.time + _seed;
            float breathe = 0.8f + 0.2f * Mathf.Sin(t * 0.9f);
            _swirl.localRotation = Quaternion.Euler(90f, t * 14f, 0f);
            _swirl.localScale = Vector3.one * _radius * 2f * (0.97f + 0.03f * Mathf.Sin(t * 1.3f));
            _foam.localRotation = Quaternion.Euler(90f, 60f - t * 9f, 0f);
            _foam.localScale = Vector3.one * _radius * 2.5f;
            // 파티클 셰이더는 색을 2배 하므로 0.5가 흰색
            _swirlMat.SetColor("_TintColor", new Color(0.5f * _tint.r, 0.52f * _tint.g, 0.55f * _tint.b, 0.5f * _alpha * breathe));
            _foamMat.SetColor("_TintColor", new Color(0.5f * _tint.r, 0.52f * _tint.g, 0.55f * _tint.b, 0.3f * _alpha * (1.4f - breathe)));
        }

        void OnDestroy()
        {
            if (_swirlMat != null) Destroy(_swirlMat);
            if (_foamMat != null) Destroy(_foamMat);
        }
    }
}
