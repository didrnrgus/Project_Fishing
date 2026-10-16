using System.Collections.Generic;
using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 받은 휴머노이드 캐릭터로 만든 NPC를 맵마다 놓는다 (MapLoader가 월드를 지은 직후). 사람 목록은 ExternalCharacters.Registry.
    /// - 낮: 둔치를 걷는 의사, 벤치(아이스박스)에서 수다 떠는 두 사람, 낚시점 앞에서 음료를 마시는 항만 노동자,
    ///   아이돌 옆에서 닭 춤 추는 아이, 항구를 순찰하는 군인·특공대, 맵마다 코스프레 낚시꾼(기사·닌자·엘프 …), 태안 해수욕장에서 헤엄치는 사람,
    ///   하롱베이 해적, 산토리니 마을 아가씨
    /// - 밤(21~04시): 좀비 여럿(경찰·소녀·전쟁·해골 …, 가까이 가면 비명), 술 취해 비틀거리는 아저씨, 다리 밑 광대, 빛나는 댄서,
    ///   흡혈귀·그림자, 귀신(<see cref="NightVisitor"/>: X Bot + 반투명 셰이더, 다가가면 사라짐)
    /// - 유니티짱: 한강 낚시꾼 + 공원 "낚시 아이돌"(<see cref="IdolRoutine"/>)
    /// 에셋이 없으면(CharacterLibrary에 없음) 그 사람만 빠진다.
    /// </summary>
    public static class ExtraNpcs
    {
        static Transform _root;
        static FieldMapDef _map;

        static float H(float x, float z) => _map is CoastMapDef c ? c.HeightAt(x, z) : _map.LandY;
        static Vector3 At(float x, float z) => new Vector3(x, H(x, z), z);

        static void W(Wanderer.Spec s) => Wanderer.Spawn(_root, s);

        /// <summary>z 위에서 x0~x1을 오가는 길. 중간에 물이 끼면 가운데 쪽으로 줄인다 (너무 짧아지면 null).</summary>
        static Vector3[] Path(float x0, float x1, float z)
        {
            for (int k = 0; k < 6; k++)
            {
                bool ok = true;
                for (float x = x0; x <= x1 && ok; x += 1.5f)
                    if (_map.IsWater(new Vector3(x, _map.BaseWaterY, z))) ok = false;
                if (ok) return new[] { At(x0, z), At(x1, z) };
                float m = (x0 + x1) * 0.5f, h = (x1 - x0) * 0.35f;
                if (h < 2f) break;
                x0 = m - h; x1 = m + h;
            }
            return null;
        }

        /// <summary>x 줄을 따라 바다 쪽으로 가며 수심 1.5~4m인 첫 물 (헤엄치는 사람 자리).</summary>
        static Vector3? Shallow(float x)
        {
            if (!(_map is CoastMapDef c)) return null;
            for (float z = -60f; z < 120f; z += 1f)
            {
                var p = new Vector3(x, c.BaseWaterY, z);
                float depth = c.BaseWaterY - c.HeightAt(x, z);
                if (c.IsWater(p) && depth > 1.5f && depth < 4f) return p;
            }
            return null;
        }

        static void Angler(string id, string name, string title, float dx, float dz = 1f, float yaw = 0f)
        {
            if (!(_map is CoastMapDef c) || !ExternalCharacters.Available(id)) return;
            float x = c.S.Spawn.x + dx, z = c.S.Spawn.z + dz;
            // 물가 쪽으로 한 걸음씩 가며 아직 땅인 마지막 자리
            for (int i = 0; i < 6 && !c.IsWater(new Vector3(x, c.BaseWaterY, z + 1f)); i++) z += 1f;
            if (c.IsWater(new Vector3(x, c.BaseWaterY, z))) return;
            NpcAngler.Spawn(_root, At(x, z), yaw, new CharacterLook { External = id, Name = name, Title = title }, NpcAngler.Role.Angler);
        }

        static void Patrol(string id, string name, string title, float dx0, float dx1, float dz, Wanderer.When when = Wanderer.When.Day)
        {
            if (!(_map is CoastMapDef c)) return;
            var path = Path(c.S.Spawn.x + dx0, c.S.Spawn.x + dx1, c.S.Spawn.z + dz);
            if (path == null) { Debug.Log($"[Dev] 순찰 길 없음: {id}"); return; }
            W(new Wanderer.Spec { Id = id, Name = name, Title = title, Points = path, Walk = CharacterAnimator.Walk2, Idle = CharacterAnimator.Idle,
                React = CharacterAnimator.ThumbsUp, Speed = 1.25f, When = when, Tag = true, Rest = 6f });
        }

        static void NightWalker(string id, float dx0, float dx1, float dz, string toast = null)
        {
            if (!(_map is CoastMapDef c)) return;
            var path = Path(c.S.Spawn.x + dx0, c.S.Spawn.x + dx1, c.S.Spawn.z + dz);
            if (path != null) Zombie(id, path, toast);
            else Debug.Log($"[Dev] 밤 길 없음: {id}");
        }

        static void Zombie(string id, Vector3[] path, string toast = null) =>
            W(new Wanderer.Spec { Id = id, Points = path, Walk = CharacterAnimator.ZombieWalk, Idle = CharacterAnimator.ZombieIdle, WalkAnimSpeed = 0.9f,
                React = CharacterAnimator.ZombieScream, React_Sound = true, Speed = 0.55f, When = Wanderer.When.Night, Rest = 3f,
                Toast = toast ?? "…으어어. 밤의 물가를 헤매는 무언가가 있다." });

        static void Drinker(Vector3 at, float yaw) =>
            W(new Wanderer.Spec { Id = "worker", Name = "항만 노동자 김씨", Title = "퇴근길 한 캔", Points = new[] { at }, Yaw = yaw,
                Idle = CharacterAnimator.Drink, React = CharacterAnimator.ThumbsUp, When = Wanderer.When.Day, Tag = true });

        public static void Populate(FieldMapDef map, Transform world)
        {
            if (world == null || CharacterLibrary.Lib == null) return;
            _map = map;
            _root = new GameObject("ExtraNpcs").transform;
            _root.SetParent(world, false);
            var c = map as CoastMapDef;
            Vector3 shopFront = c != null ? c.S.ShopPos + new Vector3(4.5f, 0f, 4.5f) : Vector3.zero;

            switch (map.Id)
            {
                case "hangang":
                {
                    float z = HanRiverMap.RiverMinZ - 1.6f, y = HanRiverMap.LandY;
                    if (ExternalCharacters.Available("unitychan"))
                    {
                        NpcAngler.Spawn(_root, new Vector3(30f, y, z), -5f, new CharacterLook
                        {
                            External = "unitychan", Female = true, Name = "유니티짱", Title = "낚시 아이돌"
                        }, NpcAngler.Role.Angler);
                        var idol = new Vector3(HanRiverMap.Pier.center.x - 14f, y, HanRiverMap.RiverMinZ - 9f);
                        IdolRoutine.Spawn(_root, idol, 160f);
                        // 아이돌 팬: 옆에서 닭 춤, 다가가면 환호
                        W(new Wanderer.Spec { Id = "animekid", Name = "하늘이", Title = "아이돌 1호 팬", Points = new[] { idol + new Vector3(2.6f, 0f, -1.2f) }, Yaw = 200f,
                            Idle = CharacterAnimator.DanceChicken, React = CharacterAnimator.Cheer2, When = Wanderer.When.Day, Tag = true });
                    }
                    // 공원 벤치(아이스박스)에서 수다 떠는 두 사람
                    W(new Wanderer.Spec { Id = "teen", Name = "민지", Title = "수다 삼매경", Points = new[] { new Vector3(-52f, y, -79f) }, Yaw = 180f,
                        Idle = CharacterAnimator.SitTalk, Seat = true, When = Wanderer.When.Day, Tag = true });
                    W(new Wanderer.Spec { Id = "woman", Name = "수진", Title = "수다 삼매경", Points = new[] { new Vector3(-52f, y, -81.4f) }, Yaw = 0f,
                        Idle = CharacterAnimator.SitTalk, Seat = true, When = Wanderer.When.Day, Tag = true });
                    // 둔치 산책길의 의사 (당직 끝)
                    W(new Wanderer.Spec { Id = "doctor", Name = "닥터 강", Title = "당직 끝 산책", Points = new[] { new Vector3(-60f, y, -69f), new Vector3(10f, y, -69f) },
                        Walk = CharacterAnimator.Walk2, React = CharacterAnimator.ThumbsUp, When = Wanderer.When.Day, Tag = true, Rest = 6f });
                    Drinker(HanRiverMap.ShopPos + new Vector3(4.5f, 0f, 3.5f), 200f);
                    // 밤: 술 취한 아저씨, 다리 밑 광대, 데크 입구 댄서, 좀비들, 귀신
                    W(new Wanderer.Spec { Id = "worker", Name = "항만 노동자 김씨", Title = "딱 한 잔만…", Points = new[] { new Vector3(0f, y, -71f), new Vector3(45f, y, -71f) },
                        Walk = CharacterAnimator.DrunkWalk, Idle = CharacterAnimator.DrunkIdle, React = CharacterAnimator.DrunkIdle2, Speed = 0.8f,
                        When = Wanderer.When.Night, Tag = true, Toast = "김씨가 비틀거린다. \"딸꾹… 오늘 월척 했어…\"" });
                    W(new Wanderer.Spec { Id = "clown", Points = new[] { new Vector3(HanRiverMap.BridgeX - 6f, y, -66f) }, Yaw = 90f,
                        Idle = CharacterAnimator.Idle, React = CharacterAnimator.Greeting, ReactRadius = 9f, When = Wanderer.When.Night,
                        Toast = "다리 밑 어둠 속에서 하얀 얼굴의 광대가 손을 흔든다…" });
                    W(new Wanderer.Spec { Id = "neon", Name = "네온", Title = "한강 야간 댄서", Points = new[] { new Vector3(HanRiverMap.Pier.center.x + 4f, y, HanRiverMap.RiverMinZ - 6f) },
                        Yaw = 180f, Idle = CharacterAnimator.DanceClub, React = CharacterAnimator.Cheer2, When = Wanderer.When.Night, Tag = true });
                    Zombie("copzombie", new[] { new Vector3(60f, y, -70f), new Vector3(120f, y, -70f) });
                    Zombie("zombiegirl", new[] { new Vector3(-140f, y, -70f), new Vector3(-95f, y, -70f) });
                    NightVisitor.SpawnZombie(_root, new Vector3(-10f, y, -70f), new Vector3(40f, y, -70f));
                    NightVisitor.SpawnGhost(_root, new[]
                    {
                        new Vector3(HanRiverMap.BridgeX + 7f, y, HanRiverMap.RiverMinZ - 3f),
                        new Vector3(-30f, y, -66f),
                        new Vector3(95f, y, -64f),
                    });
                    break;
                }
                case "yeongheung":
                    Angler("ely", "엘리", "도시의 조사", -14f);
                    Drinker(At(shopFront.x, shopFront.z), 200f);
                    NightWalker("rotzombie", -25f, 15f, -4f);
                    break;
                case "taean":
                    Angler("knight", "기사 펠레그리니", "갑옷 입고 원투", -16f);
                    // 만리포 해수욕장에서 헤엄치는 두 사람
                    foreach (var (id, x, name) in new[] { ("teen", -70f, "민지"), ("woman", -84f, "수진") })
                    {
                        var p = Shallow(x);
                        if (p == null) { Debug.Log($"[Dev] 얕은 물 없음 x={x}"); continue; }
                        W(new Wanderer.Spec { Id = id, Name = name, Title = "해수욕 중", Points = new[] { p.Value, p.Value + new Vector3(9f, 0f, 3f) }, Water = true,
                            Walk = CharacterAnimator.Swim, Idle = CharacterAnimator.Tread, Speed = 0.7f, Rest = 6f, When = Wanderer.When.Day, Tag = true });
                    }
                    NightWalker("warzombie", -20f, 20f, -4f);
                    break;
                case "samcheok":
                    Angler("ninja", "닌자", "밤바다 오징어 사냥꾼", -14f);
                    NightWalker("girlscout", -22f, 18f, -4f, "…어린 소녀가 비틀비틀 걷고 있다. 무언가 이상하다.");
                    break;
                case "deungdae":
                    Patrol("soldier", "박 상병", "해안 경계 근무", -22f, 18f, -3f);
                    NightWalker("vampire", -18f, 14f, -4f, "등대 불빛 아래 창백한 사내가 서 있다…");
                    GhostOnCape(c);
                    break;
                case "heonhwa":
                    Angler("uriel", "우리엘", "천상의 낚시꾼", 14f);
                    NightWalker("rotzombie", -18f, 16f, -3.5f, "썩은 몸의 사내가 해안도로를 헤맨다…");
                    NightWalker("skinless", 20f, 40f, -3.5f);
                    break;
                case "yokji":
                    Angler("arissa", "아리사", "가두리의 그림자", -14f);
                    Drinker(At(shopFront.x, shopFront.z), 200f);
                    break;
                case "jeju":
                    Angler("kachujin", "카추진", "무늬오징어 무도가", -14f);
                    NightVisitor.SpawnGhost(_root, new[] { At(-40f, -58f), At(60f, -56f) });
                    break;
                case "cheongjin":
                    Patrol("swat", "특공대 최 경사", "항구 경비", -22f, 6f, -3f);
                    Patrol("soldier", "경계병 이 일병", "부두 경계", 8f, 30f, -3f);
                    W(new Wanderer.Spec { Id = "hazmat", Name = "방역 요원", Title = "명태 덕장 위생 점검", Points = new[] { At(shopFront.x - 8f, shopFront.z) }, Yaw = 160f,
                        Idle = CharacterAnimator.Idle, React = CharacterAnimator.Greeting, When = Wanderer.When.Day, Tag = true });
                    NightWalker("skeleton", -25f, 0f, -4f);
                    NightWalker("yaku", 5f, 30f, -4f);
                    NightWalker("brute", -10f, 15f, -7f, "거대한 그림자가 눈 덮인 부두를 쿵쿵 걷는다…");
                    NightVisitor.SpawnZombie(_root, At(2f, -53f), At(48f, -53f));
                    break;
                case "geomun":
                    Patrol("swat", "특공대 정 경장", "등대 섬 경비", -20f, 16f, -3f);
                    GhostOnCape(c);
                    NightWalker("nightshade", -16f, 12f, -4f, "검은 갑주의 무언가가 소리 없이 걷고 있다…");
                    break;
                case "halong":
                    Angler("elf", "엘프 리엔", "숲에서 온 조사", -14f);
                    W(new Wanderer.Spec { Id = "ely", Name = "선장 코스타", Title = "정크선 선장", Points = new[] { At(c.S.Spawn.x - 9f, c.S.Spawn.z - 3f) }, Yaw = 180f,
                        Idle = CharacterAnimator.Victory, React = CharacterAnimator.ThumbsUp, When = Wanderer.When.Always, Tag = true });
                    NightWalker("shadow", -18f, 16f, -4f, "석회암 봉우리 그림자 속에서 검은 형체가 걸어 나온다…");
                    break;
                case "santorini":
                    Angler("maria", "마리아", "칼데라의 기사", -12f);
                    Patrol("peasant", "엘레니", "이아 마을 빵집 딸", -14f, 14f, -5f);
                    NightWalker("raider", -16f, 12f, -4f);
                    break;
                case "nile":
                    Drinker(At(shopFront.x, shopFront.z), 200f);
                    NightWalker("parasite", -20f, 20f, -4f, "강물에서 무언가 기어 나와 강둑을 걷는다…");
                    break;
            }
        }

        /// <summary>큰 등대 언덕 (곶 등뼈 72% 지점에 등대) 둘레의 귀신.</summary>
        static void GhostOnCape(CoastMapDef c)
        {
            if (c == null || c.S.Capes == null || c.S.Capes.Length == 0) return;
            var cape = c.S.Capes[0];
            var top = Vector2.Lerp(cape.Base, cape.Tip, 0.72f);
            var mid = Vector2.Lerp(cape.Base, cape.Tip, 0.5f);
            var pts = new List<Vector3>();
            foreach (var p in new[] { top + new Vector2(-6f, -9f), mid + new Vector2(5f, 0f), top + new Vector2(8f, 6f) })
                pts.Add(At(p.x, p.y));
            NightVisitor.SpawnGhost(_root, pts.ToArray());
        }
    }

    /// <summary>유니티짱 "낚시 아이돌": 포즈 → 춤(힙합·룸바·카포에이라 돌아가며). 플레이어가 다가오면 바라보며 손 흔들어 인사.</summary>
    public class IdolRoutine : MonoBehaviour
    {
        CharacterAnimator _anim;
        float _next;
        int _step;
        bool _greeted;
        Quaternion _home;
        Transform _tag;
        static readonly string[] Dances = { CharacterAnimator.DanceHipHop, CharacterAnimator.DanceRumba, CharacterAnimator.DanceCapoeira };

        public static IdolRoutine Spawn(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("FishingIdol");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var ch = ExternalCharacters.Create("unitychan", go.transform, "낚시 아이돌");
            if (ch == null) { Destroy(go); return null; }
            var tag = CharacterFactory.AddNameTag(go.transform, "유니티짱", "♪ 낚시 아이돌 ♪", 1.85f);
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.height = 1.6f;
            col.radius = 0.3f;
            var r = go.AddComponent<IdolRoutine>();
            r._anim = ch.GetComponentInChildren<CharacterAnimator>();
            r._home = go.transform.localRotation;
            r._tag = tag;
            return r;
        }

        void LateUpdate()
        {
            // 이름표는 늘 카메라를 본다 (NpcAngler와 같이)
            if (_tag != null && Game.CameraRig != null)
                _tag.rotation = Quaternion.LookRotation(_tag.position - Game.CameraRig.transform.position);
        }

        void Update()
        {
            if (_anim == null) return;
            var player = Game.Player != null ? Game.Player.transform : null;
            float d = player != null ? Vector3.Distance(player.position, transform.position) : 999f;
            if (d < 7f)
            {
                // 바라보고, 처음 다가오면 손 흔들기
                var dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 4f);
                if (!_greeted)
                {
                    _greeted = true;
                    _anim.SetLoop(CharacterAnimator.PoseFemale);
                    _anim.PlayOnce(CharacterAnimator.Greeting);
                    _next = Time.time + 6f;
                }
                return;
            }
            if (d > 14f) _greeted = false;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _home, Time.deltaTime * 2f);
            if (Time.time < _next) return;
            // 포즈 6초 → 춤 12초 → 포즈 …
            bool dance = _step % 2 == 1;
            _anim.SetLoop(dance ? Dances[(_step / 2) % Dances.Length] : CharacterAnimator.PoseFemale);
            _next = Time.time + (dance ? 12f : 6f);
            _step++;
        }
    }

    /// <summary>
    /// 밤에만 나오는 손님: 좀비(두 점 사이를 비틀비틀 오간다, 가까이 가면 멈춰 비명) / 귀신(희미하게 서서 바라보다 다가가면 사라져 다른 곳에).
    /// 시간은 게임 시계(21~04시). 낮에는 모델을 꺼 둔다.
    /// </summary>
    public class NightVisitor : MonoBehaviour
    {
        enum Kind { Zombie, Ghost }

        Kind _kind;
        GameObject _model;
        CharacterAnimator _anim;
        Vector3[] _points;
        int _target = 1;
        float _pauseUntil, _nextScream, _hiddenUntil, _fade;
        bool _toasted;
        Material _ghostMat;

        static bool Night => Game.Clock != null && (Game.Clock.Hour >= 21 || Game.Clock.Hour < 4);

        public static void SpawnZombie(Transform parent, Vector3 a, Vector3 b)
        {
            var v = Make(parent, "zombie", "NightZombie", a, Kind.Zombie);
            if (v == null) return;
            v._points = new[] { a, b };
            v._anim.SetLoop(CharacterAnimator.ZombieWalk, 0.9f);
        }

        public static void SpawnGhost(Transform parent, Vector3[] points)
        {
            if (points == null || points.Length == 0) return;
            var v = Make(parent, "ghost", "NightGhost", points[0], Kind.Ghost);
            if (v == null) return;
            v._points = points;
            v._target = 0;
            v._anim.SetLoop(CharacterAnimator.PoseFemale, 0.6f);
            foreach (var r in v._model.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial != null && r.sharedMaterial.name == "Ghost") { v._ghostMat = r.sharedMaterial; break; }
            // 발밑에 번지는 푸른 빛
            var glow = new GameObject("GhostGlow").AddComponent<Light>();
            glow.transform.SetParent(v._model.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            glow.type = LightType.Point;
            glow.color = new Color(0.6f, 0.8f, 1f);
            glow.range = 5f;
            glow.intensity = 1.2f;
            glow.shadows = LightShadows.None;
        }

        static NightVisitor Make(Transform parent, string id, string name, Vector3 at, Kind kind)
        {
            if (!ExternalCharacters.Available(id)) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            var v = go.AddComponent<NightVisitor>();
            v._kind = kind;
            v._model = ExternalCharacters.Create(id, go.transform, name);
            v._anim = v._model.GetComponentInChildren<CharacterAnimator>();
            ModelVisibility.Set(v._model, false);
            return v;
        }

        void Update()
        {
            if (_model == null || _anim == null) return;
            bool night = Night && Time.time >= _hiddenUntil;
            if (_kind == Kind.Ghost) UpdateGhost(night);
            else UpdateZombie(night);
        }

        void UpdateZombie(bool night)
        {
            if (ModelVisibility.Shown(_model) != night) ModelVisibility.Set(_model, night);
            if (!night) return;
            var player = Game.Player != null ? Game.Player.transform : null;
            float d = player != null ? Vector3.Distance(player.position, transform.position) : 999f;
            if (d < 5f && Time.time > _nextScream)
            {
                // 멈춰서 돌아보고 비명 (4초), 25초에 한 번
                _nextScream = Time.time + 25f;
                _pauseUntil = Time.time + 4f;
                var dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
                _anim.SetLoop(CharacterAnimator.ZombieIdle);
                _anim.PlayOnce(CharacterAnimator.ZombieScream);
                Game.Sound?.UiError();
                if (!_toasted) { _toasted = true; Game.Toast("…으어어. 밤의 둔치를 헤매는 무언가가 있다.", ToastKind.Warn); }
            }
            if (Time.time < _pauseUntil) return;
            _anim.SetLoop(CharacterAnimator.ZombieWalk, 0.9f);
            var to = _points[_target] - transform.localPosition;
            to.y = 0f;
            if (to.magnitude < 0.5f) { _target = 1 - _target; _pauseUntil = Time.time + 2.5f; _anim.SetLoop(CharacterAnimator.ZombieIdle); return; }
            var step = to.normalized * 0.55f * Time.deltaTime;
            transform.localPosition += step;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(to), Time.deltaTime * 2f);
        }

        void UpdateGhost(bool night)
        {
            var player = Game.Player != null ? Game.Player.transform : null;
            float d = player != null ? Vector3.Distance(player.position, transform.position) : 999f;
            bool show = night && d > 6f;
            if (night && d <= 6f && _fade > 0.5f)
            {
                // 다가가면 사라졌다가 40~90초 뒤 다른 곳에
                _hiddenUntil = Time.time + Random.Range(40f, 90f);
                _target = (_target + 1 + Random.Range(0, Mathf.Max(1, _points.Length - 1))) % _points.Length;
                show = false;
            }
            _fade = Mathf.MoveTowards(_fade, show ? 1f : 0f, Time.deltaTime * (show ? 0.5f : 1.2f));
            bool visible = _fade > 0.001f;
            if (ModelVisibility.Shown(_model) != visible) ModelVisibility.Set(_model, visible);
            if (!visible)
            {
                transform.localPosition = _points[_target]; // 숨어 있는 동안 다음 자리로
                return;
            }
            if (d < 18f && !_toasted && show) { _toasted = true; Game.Toast("…방금, 하얀 것이 서 있었던 것 같다.", ToastKind.Warn); }
            // 살짝 떠서 오르내리고, 플레이어를 바라본다
            var basePos = _points[_target];
            transform.localPosition = basePos + Vector3.up * (0.12f + Mathf.Sin(Time.time * 1.1f) * 0.06f);
            if (player != null && d < 30f)
            {
                var dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 1.5f);
            }
            if (_ghostMat != null)
            {
                _ghostMat.SetFloat("_Fade", _fade);
                _ghostMat.SetFloat("_BaseY", transform.position.y);
            }
        }
    }

    /// <summary>
    /// 캐릭터 숨기기·보이기: 물체를 끄지 않고 렌더러만 끈다. Animator가 달린 물체를 SetActive(false)했다 켜면
    /// Playables 그래프가 다시 붙지 않아 T자 자세로 움직였다 (밤에만 나오는 좀비들). 숨긴 동안은 컬링 모드가 뼈 계산을 건너뛴다.
    /// </summary>
    public static class ModelVisibility
    {
        public static void Set(GameObject model, bool show)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.enabled = show;
            foreach (var l in model.GetComponentsInChildren<Light>(true)) l.enabled = show;
        }

        public static bool Shown(GameObject model)
        {
            var r = model.GetComponentInChildren<Renderer>(true);
            return r != null && r.enabled;
        }
    }
}
