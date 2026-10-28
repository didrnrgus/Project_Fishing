using System;
using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>설치한 통발 하나 (세이브).</summary>
    [Serializable]
    public class TrapSave
    {
        public string kind = FishTraps.RoundTrapId;
        public string mapId;
        public int spot;                 // 0 = 앞바다(물가), 1 = 먼 바다(깊은 물)
        public string npcName;           // 설치해 준 어부 (원작: "홍현 앞바다(이지훈)")
        public float startMinutes;       // 설치한 게임 시각 (GameClock.TotalMinutes)
        public long startTicks;          // 설치한 실제 시각 (UTC ticks) — 게임을 꺼 둔 동안에도 시간이 간다
        public bool rolled;              // 수거할 물고기를 정했는지 (한 번 정하면 다시 열어도 같다)
        public List<CaughtFish> fish = new List<CaughtFish>();
        public List<ItemStack> items = new List<ItemStack>();
    }

    /// <summary>통발 설치 장소 (맵마다 둘: 앞바다 / 먼 바다).</summary>
    public struct TrapSpot
    {
        public string MapId;
        public int Index;
        public string Name;
        public int Level;
        public int Cost;
    }

    /// <summary>
    /// 통발 (원작 영상 0:44~0:48, 2:20~2:26): 바닷가 마을 어부에게 통발을 맡겨 두면 20시간 뒤 물고기 10마리를 거둔다.
    ///  - 통발 칸 2개 (원작 화면의 원형 통발 두 줄). 원형 통발: 어획량 10마리, 소요 시간 20시간
    ///  - 설치 장소: 그 맵의 "앞바다"(요구 레벨 = 낚시터 입장 레벨)와 "먼 바다"(+2). 한 장소에 통발 하나
    ///  - 설치는 무료, 수거할 때 수거비용(요구 레벨 × 150 GOLD, 원작 Lv.12 → 1,800 GOLD)
    ///  - 시간: 게임 20시간 = 현실 20분(게임 1초 = 1분). 게임을 꺼 둔 동안의 실제 시간도 같은 비율로 쳐 준다
    ///  - 거둔 물고기: 그 맵의 흔한~조금 귀한 어종(희귀도 1~3), 작은~중간 크기. 고른 것만 살림망으로, 나머지는 놓아준다
    ///  - 덤 아이템(해체 미끼·바다 미끼)은 아이템 획득 창(<see cref="Fishing.UI.ItemBagPopup"/>)으로 받는다
    /// </summary>
    public static class FishTraps
    {
        public const string RoundTrapId = "trap_round";
        public const string RoundTrapName = "원형 통발";
        public const int Slots = 2;
        public const int CatchCount = 10;
        public const float DurationHours = 20f;
        public static float DurationMinutes => DurationHours * 60f;
        public const int CostPerLevel = 150;
        public const string FisherId = "fisher";

        static List<TrapSave> Traps
        {
            get
            {
                var d = Game.Profile.Data;
                if (d.traps == null) d.traps = new List<TrapSave>();
                return d.traps;
            }
        }

        public static IReadOnlyList<TrapSave> All => Traps;
        public static int InstalledCount => Traps.Count;

        // ---------------- 장소 ----------------

        /// <summary>맵 이름 줄기 (설치 장소·어부 칭호용).</summary>
        static readonly Dictionary<string, (string stem, string npc)> MapInfo = new Dictionary<string, (string, string)>
        {
            { "hangang", ("한강", "어도사 박영감") },
            { "tancheon", ("탄천", "탄천지기 윤씨") },
            { "honghyeon", ("홍현", "이지훈") },
            { "seocheon", ("서천", "홍원항 김씨") },
            { "yeongheung", ("영흥도", "진두항 오씨") },
            { "taean", ("태안", "만리포 윤씨") },
            { "samcheok", ("삼척", "삼척항 남씨") },
            { "deungdae", ("등대섬", "섬마을 배씨") },
            { "heonhwa", ("헌화로", "심곡항 정씨") },
            { "cheongjin", ("청진", "청진포 리씨") },
            { "yokji", ("욕지도", "가두리 황씨") },
            { "jeju", ("서귀포", "법환포구 고씨") },
            { "geomun", ("거문도", "거문항 손씨") },
            { "halong", ("하롱베이", "어부 탄") },
            { "santorini", ("산토리니", "어부 니코스") },
            { "nile", ("나일강", "어부 카림") },
            { "angkor", ("앙코르와트", "어부 삼낭") },
            { "vinpearl", ("빈펄", "어부 꽝") },
            { "donkhone", ("돈콘", "어부 캄") },
            { "phuket", ("푸켓", "어부 쁘라윳") },
            { "bangkok", ("방콕", "어부 웃") },
            { "kohsamui", ("코사무이", "어부 쏨밧") },
            { "langkawi", ("랑카위", "어부 이스마일") },
            { "sentosa", ("센토사", "어부 림") },
            { "telokbatek", ("텔록 바텍", "어부 오스만") },
            { "tioman", ("티오만", "어부 유수프") },
            { "tenggol", ("탱글", "어부 다우드") },
            { "crete", ("크레타", "어부 마놀리스") },
            { "zakynthos", ("자킨토스", "어부 스피로스") },
            { "duden", ("듀덴 폭포", "어부 알리") },
        };

        static string Stem(string mapId) => MapInfo.TryGetValue(mapId ?? "", out var i) ? i.stem : (Maps.Get(mapId)?.ShortName ?? mapId);
        public static string FisherName(string mapId) => MapInfo.TryGetValue(mapId ?? "", out var i) ? i.npc : "마을 어부";
        public static string FisherTitle(string mapId) => $"{Stem(mapId)}{(IsFresh(mapId) ? " 강" : "")}마을 어부";
        static bool IsFresh(string mapId) => Array.IndexOf(FishSpecies.FreshMaps, mapId) >= 0;

        public static TrapSpot Spot(string mapId, int index)
        {
            int lv = FishingAreas.EntryLevel(mapId) + (index == 1 ? 2 : 0);
            string stem = Stem(mapId);
            string name = IsFresh(mapId) ? (index == 0 ? $"{stem} 물가" : $"{stem} 깊은 물") : (index == 0 ? $"{stem} 앞바다" : $"{stem} 먼 바다");
            return new TrapSpot { MapId = mapId, Index = index, Name = name, Level = lv, Cost = lv * CostPerLevel };
        }

        public static TrapSpot SpotOf(TrapSave t) => Spot(t.mapId, t.spot);

        public static List<TrapSpot> SpotsOf(string mapId) => new List<TrapSpot> { Spot(mapId, 0), Spot(mapId, 1) };

        public static bool Occupied(TrapSpot s) => Traps.Exists(t => t.mapId == s.MapId && t.spot == s.Index);

        public static bool LevelOk(TrapSpot s) => Game.Profile.Data.level >= s.Level;

        public static bool CanInstallAt(TrapSpot s) => LevelOk(s) && !Occupied(s) && Traps.Count < Slots;

        // ---------------- 시간 ----------------

        /// <summary>지난 시간(게임 분). 게임 시계와 실제 시각(× 게임 1초 = 1분) 중 큰 쪽.</summary>
        public static float Elapsed(TrapSave t)
        {
            float game = Game.Clock != null ? Game.Clock.TotalMinutes - t.startMinutes : 0f;
            double realSec = (DateTime.UtcNow.Ticks - t.startTicks) / (double)TimeSpan.TicksPerSecond;
            float real = (float)Math.Max(0.0, realSec) * GameClock.GameMinutesPerSecond;
            return Mathf.Max(0f, Mathf.Max(game, real));
        }

        public static float Remaining(TrapSave t) => Mathf.Max(0f, DurationMinutes - Elapsed(t));
        public static bool Ready(TrapSave t) => Remaining(t) <= 0f;
        public static float Progress01(TrapSave t) => Mathf.Clamp01(Elapsed(t) / DurationMinutes);
        public static bool AnyReady => Game.Profile != null && Game.Profile.Data.traps != null && Game.Profile.Data.traps.Exists(Ready);

        /// <summary>"13시간 20분" (게임 시간).</summary>
        public static string HoursText(float minutes)
        {
            int m = Mathf.CeilToInt(minutes);
            int h = m / 60, mm = m % 60;
            return h > 0 ? (mm > 0 ? $"{h}시간 {mm}분" : $"{h}시간") : $"{mm}분";
        }

        // ---------------- 설치 / 수거 ----------------

        public static bool Install(TrapSpot s, string npcName)
        {
            if (Traps.Count >= Slots) { Game.Toast("통발을 더 설치할 수 없습니다. (2/2)", ToastKind.Warn); return false; }
            if (!LevelOk(s)) { Game.Toast($"요구 레벨이 부족합니다. (Lv.{s.Level} 이상)", ToastKind.Warn); return false; }
            if (Occupied(s)) { Game.Toast("이미 통발을 설치한 장소입니다.", ToastKind.Warn); return false; }
            Traps.Add(new TrapSave
            {
                mapId = s.MapId, spot = s.Index, npcName = npcName,
                startMinutes = Game.Clock != null ? Game.Clock.TotalMinutes : 0f,
                startTicks = DateTime.UtcNow.Ticks,
            });
            Game.SysLog($"{s.Name}에 {RoundTrapName}을 설치했습니다. ({DurationHours:0}시간 뒤 수거)");
            Game.Profile.NotifyChanged();
            Game.Save();
            return true;
        }

        /// <summary>수거할 물고기·아이템을 정한다 (처음 한 번만).</summary>
        public static void EnsureRolled(TrapSave t)
        {
            if (t.rolled || !Ready(t)) return;
            t.rolled = true;
            t.fish = RollFish(t.mapId, t.spot == 1, CatchCount);
            t.items = RollItems(t.mapId);
        }

        static readonly float[] RarityWeight = { 0f, 1f, 0.55f, 0.22f };

        /// <summary>통발에 드는 고기: 그 맵의 희귀도 1~3 어종(어군 전용·괴물·먼바다 전용 제외, 먼 바다 칸은 먼바다 어종 허용), 작은~중간 크기.</summary>
        public static List<CaughtFish> RollFish(string mapId, bool far, int count)
        {
            var pool = new List<(FishSpecies s, float w)>();
            float total = 0f;
            var season = Seasons.Current;
            foreach (var s in FishDatabase.All)
            {
                if (s.IsJunk || s.Rarity < 1 || s.Rarity > 3 || s.SchoolOnly || s.Boss || s.Id == "smelt") continue;
                if (s.Maps == null || Array.IndexOf(s.Maps, mapId) < 0) continue;
                if (s.FarSeaOnly && !far) continue;
                float w = RarityWeight[s.Rarity] * Seasons.BiteFactor(s, season) * (far && s.Rarity >= 2 ? 1.5f : 1f);
                if (w <= 0f) continue;
                pool.Add((s, w));
                total += w;
            }
            var list = new List<CaughtFish>();
            if (pool.Count == 0) return list;
            int day = Game.Clock != null ? Game.Clock.Day : 1;
            for (int i = 0; i < count; i++)
            {
                float r = UnityEngine.Random.value * total;
                var pick = pool[pool.Count - 1].s;
                foreach (var (s, w) in pool) { r -= w; if (r <= 0f) { pick = s; break; } }
                // 대물 길이 아래, 최대 길이의 70%(먼 바다 85%)까지. 대부분 작다
                float cap = pick.TrophyLen > pick.MinLen ? Mathf.Min(pick.MaxLen, pick.TrophyLen - 0.5f) : pick.MaxLen;
                cap = Mathf.Lerp(pick.MinLen, cap, far ? 0.85f : 0.7f);
                float len = Mathf.Lerp(pick.MinLen, cap, Mathf.Pow(UnityEngine.Random.value, far ? 1.8f : 2.4f));
                len = Mathf.Round(len * 10f) / 10f;
                list.Add(new CaughtFish { speciesId = pick.Id, length = len, weight = Mathf.Round(pick.WeightGrams(len)), day = day, zone = "통발" });
            }
            // 원작처럼 이름순으로 모아 보여 준다
            list.Sort((a, b) => string.CompareOrdinal(a.speciesId, b.speciesId));
            return list;
        }

        /// <summary>덤: 해체 미끼(레벨에 맞는 가장 좋은 것) 60%, 그 맵 미끼 35%.</summary>
        public static List<ItemStack> RollItems(string mapId, bool force = false)
        {
            var items = new List<ItemStack>();
            if (force || UnityEngine.Random.value < 0.6f)
            {
                BaitDef best = null;
                foreach (var b in GearDatabase.Baits)
                    if (b.Kind == BaitKind.CutBait && b.NotSold && b.Level <= Game.Profile.Data.level && (best == null || b.Level > best.Level)) best = b;
                if (best != null) items.Add(new ItemStack { id = best.Id, count = UnityEngine.Random.Range(5, 11) });
            }
            if (force || UnityEngine.Random.value < 0.35f)
                items.Add(new ItemStack { id = IsFresh(mapId) ? "bait_worm" : (UnityEngine.Random.value < 0.5f ? "bait_krill" : "bait_seaworm"), count = 10 });
            return items;
        }

        /// <summary>
        /// 수거: 수거비용을 내고, 고른 물고기를 살림망에 넣는다(도감에도 기록). 덤 아이템은 돌려준다(아이템 획득 창으로).
        /// 실패하면 null.
        /// </summary>
        public static List<ItemStack> Harvest(TrapSave t, ICollection<int> selected)
        {
            var p = Game.Profile;
            if (!Traps.Contains(t) || !Ready(t)) return null;
            EnsureRolled(t);
            int keep = 0;
            foreach (int i in selected) if (i >= 0 && i < t.fish.Count) keep++;
            int free = p.KeepNetSize - p.Data.keepNet.Count;
            if (keep > free)
            {
                Game.Toast(free <= 0 ? "살림망이 가득 찼습니다. 살림망을 비우거나 거둘 물고기를 줄이세요."
                    : $"살림망에 빈 자리가 {free}칸뿐입니다. 거둘 물고기를 {free}마리 이하로 고르세요.", ToastKind.Warn);
                return null;
            }
            int cost = SpotOf(t).Cost;
            if (!p.TrySpend(cost))
            {
                Game.Toast($"수거비용 {cost:N0} GOLD가 부족합니다.", ToastKind.Warn);
                return null;
            }
            Game.SysLog($"{cost:N0} GOLD 를 지불했습니다.");
            for (int i = 0; i < t.fish.Count; i++)
            {
                if (!selected.Contains(i)) continue;
                var f = t.fish[i];
                var s = FishDatabase.Get(f.speciesId);
                if (s == null) continue;
                p.Data.keepNet.Add(f);
                // 도감: 통발 고기도 "만난 어종"으로 기록한다 (경험치·퀘스트·대회 집계는 낚시로 잡은 고기만)
                p.RecordDex(s, f.length, f.weight, f.day);
            }
            if (keep > 0) Game.SysLog($"물고기 {keep}마리를 살림망에 넣었습니다.");
            int released = t.fish.Count - keep;
            if (released > 0) Game.SysLog($"물고기 {released}마리를 놓아주었습니다.", "#a0c8e0");
            Game.Notice($"{RoundTrapName} 수거 완료  ·  {SpotOf(t).Name}");
            var items = t.items ?? new List<ItemStack>();
            Traps.Remove(t);
            p.NotifyChanged();
            Game.Save();
            return items;
        }

        // ---------------- 개발용 ----------------

        /// <summary>자동 스크린샷용: 지금 맵 앞바다에 다 된 통발을 놓는다 (덤 아이템 포함).</summary>
        public static TrapSave DebugInstallReady(string mapId)
        {
            Traps.RemoveAll(x => x.mapId == mapId && x.spot == 0);
            if (Traps.Count >= Slots) Traps.RemoveAt(0);
            var t = new TrapSave
            {
                mapId = mapId, spot = 0, npcName = FisherName(mapId),
                startMinutes = (Game.Clock != null ? Game.Clock.TotalMinutes : 0f) - DurationMinutes - 1f,
                startTicks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerHour,
            };
            t.rolled = true;
            t.fish = RollFish(mapId, false, CatchCount);
            t.items = RollItems(mapId, true);
            Traps.Add(t);
            Game.Profile.NotifyChanged();
            return t;
        }

        // ---------------- 어부 NPC ----------------

        /// <summary>
        /// 맵마다 어부 NPC를 조합 연락관 옆에 세운다 (MapLoader가 월드를 지은 뒤). 말을 걸면 퀘스트 목록보기 / 미끼 조합하기 / 통발 설치하기.
        /// 물 위·벽 속이 아닌 자리를 연락관 둘레에서 찾는다.
        /// </summary>
        public static NpcAngler SpawnFisher(FieldMapDef map, Transform world)
        {
            if (map == null || world == null) return null;
            NpcAngler union = null;
            foreach (var n in NpcAngler.Talkable)
                if (n != null && n.NpcId == "union" && n.transform.IsChildOf(world)) { union = n; break; }

            Vector3 basePos = union != null ? union.transform.position : map.Spawn;
            float yaw = union != null ? union.transform.eulerAngles.y : 0f;
            var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3? at = null;
            foreach (var off in new[] { right * 3.2f, -right * 3.2f, -fwd * 3f + right * 2f, -fwd * 3f - right * 2f, right * 5f, -right * 5f })
            {
                var p = basePos + off;
                if (map is CoastMapDef c) p.y = c.HeightAt(p.x, p.z);
                if (map.IsWater(new Vector3(p.x, map.BaseWaterY, p.z)) || !map.IsWalkable(p)) continue;
                if (Physics.CheckSphere(p + Vector3.up * 0.9f, 0.45f, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool crowded = false;
                foreach (var n in NpcAngler.Talkable)
                    if (n != null && n.transform.IsChildOf(world) && (n.transform.position - p).sqrMagnitude < 2f * 2f) { crowded = true; break; }
                if (crowded) continue;
                at = p;
                break;
            }
            var pos = at ?? basePos + right * 3.2f;
            var look = new CharacterLook { Name = FisherName(map.Id), Title = FisherTitle(map.Id) };
            var npc = NpcAngler.Spawn(world, world.InverseTransformPoint(pos), yaw - world.eulerAngles.y, look, NpcAngler.Role.Talker);
            npc.NpcId = FisherId;
            npc.gameObject.AddComponent<TrapFisherMarker>();
            // 설치한 통발 자리에 띄울 부표
            var buoys = new GameObject("TrapBuoys").AddComponent<TrapBuoys>();
            buoys.transform.SetParent(world, false);
            buoys.Init(map, pos);
            return npc;
        }
    }

    /// <summary>어부 머리 위 하늘색 "!" (원작 0:42). 수거할 통발이 있으면 노랗게 출렁인다.</summary>
    public class TrapFisherMarker : MonoBehaviour
    {
        TMPro.TextMeshPro _mark;

        void Start()
        {
            _mark = Fishing.UI.UIFactory.WorldText(transform, "!", new Vector3(0f, 2.85f, 0f), new Color(0.45f, 0.85f, 1f), 5f, "TrapMark");
            _mark.fontStyle = TMPro.FontStyles.Bold;
            _mark.outlineWidth = 0.2f;
            _mark.outlineColor = new Color32(0, 20, 40, 255);
        }

        void LateUpdate()
        {
            if (_mark == null || Game.CameraRig == null) return;
            var cam = Game.CameraRig.transform.position;
            bool talking = Game.UI != null && Game.UI.Dialog.IsOpen && Game.UI.Dialog.Npc != null && Game.UI.Dialog.Npc.transform == transform;
            bool show = !talking && (cam - transform.position).sqrMagnitude < 45f * 45f;
            _mark.gameObject.SetActive(show);
            if (!show) return;
            bool ready = FishTraps.AnyReady;
            _mark.color = ready ? new Color(1f, 0.82f, 0.2f) : new Color(0.45f, 0.85f, 1f);
            _mark.transform.localPosition = new Vector3(0f, 2.85f + (ready ? Mathf.Sin(Time.time * 4f) * 0.08f : 0f), 0f);
            _mark.transform.rotation = Quaternion.LookRotation(_mark.transform.position - cam);
        }
    }

    /// <summary>
    /// 이 맵에 설치한 통발 자리의 부표 (주황 공 + 깃대). 앞바다 칸은 어부 앞쪽 물에서 12m 더 나간 곳, 먼 바다 칸은 격자 70% 지점.
    /// 1초마다 세이브의 통발 목록과 맞춘다.
    /// </summary>
    public class TrapBuoys : MonoBehaviour
    {
        FieldMapDef _map;
        readonly Vector3?[] _spots = new Vector3?[2];
        readonly GameObject[] _buoys = new GameObject[2];
        float _next;

        public void Init(FieldMapDef map, Vector3 fisher)
        {
            _map = map;
            // 앞바다: 어부 자리에서 +z(물 쪽)로 가다 처음 만난 물 + 12m
            for (float d = 1f; d < 200f; d += 1f)
            {
                var p = new Vector3(fisher.x, map.BaseWaterY, fisher.z + d);
                if (!map.IsWater(p)) continue;
                var q = p + Vector3.forward * 12f;
                _spots[0] = map.IsWater(q) ? q : p + Vector3.forward * 3f;
                break;
            }
            // 먼 바다: 격자 깊이 70% 줄에서 어부와 가까운 물
            float z = map.GridMinZ + (map.GridMaxZ - map.GridMinZ) * 0.7f;
            for (float dx = 0f; dx < 150f && _spots[1] == null; dx += 6f)
                foreach (float x in new[] { fisher.x + dx, fisher.x - dx })
                {
                    var p = new Vector3(x, map.BaseWaterY, z);
                    if (map.IsWater(p)) { _spots[1] = p; break; }
                }
        }

        void Update()
        {
            if (_map == null || Game.Profile == null) return;
            if (Time.time >= _next)
            {
                _next = Time.time + 1f;
                var traps = Game.Profile.Data.traps;
                for (int i = 0; i < 2; i++)
                {
                    bool want = _spots[i] != null && traps != null && traps.Exists(t => t.mapId == _map.Id && t.spot == i);
                    if (want && _buoys[i] == null) _buoys[i] = MakeBuoy(i);
                    else if (!want && _buoys[i] != null) { Destroy(_buoys[i]); _buoys[i] = null; }
                }
            }
            for (int i = 0; i < 2; i++)
            {
                if (_buoys[i] == null) continue;
                var p = _spots[i].Value;
                float bob = Mathf.Sin(Time.time * 1.3f + i * 2f) * 0.06f;
                _buoys[i].transform.position = new Vector3(p.x, _map.WaterY + bob, p.z);
                _buoys[i].transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.9f + i) * 6f, i * 70f, Mathf.Cos(Time.time * 1.1f + i) * 6f);
            }
        }

        GameObject MakeBuoy(int i)
        {
            var root = new GameObject("TrapBuoy" + i);
            root.transform.SetParent(transform, true);
            var orange = Mats.Lit(new Color(1f, 0.45f, 0.1f), 0.5f);
            Mats.Prim(PrimitiveType.Sphere, "Float", root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.55f, 0.45f, 0.55f), orange, false);
            Mats.Prim(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.04f, 0.6f, 0.04f), Mats.Lit(new Color(0.2f, 0.2f, 0.2f)), false);
            Mats.Prim(PrimitiveType.Cube, "Flag", root.transform, new Vector3(0.16f, 1.2f, 0f), new Vector3(0.3f, 0.2f, 0.02f), Mats.Lit(new Color(0.15f, 0.55f, 0.95f)), false);
            return root;
        }
    }
}
