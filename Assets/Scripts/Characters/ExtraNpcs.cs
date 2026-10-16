using System.Collections.Generic;
using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 받은 휴머노이드 캐릭터로 만든 NPC를 맵마다 놓는다 (MapLoader가 월드를 지은 직후).
    /// - 유니티짱: 한강 둔치 낚시꾼(NpcAngler) + 공원 "낚시 아이돌"(<see cref="IdolRoutine"/>: 춤·포즈, 다가가면 손 흔들어 인사)
    /// - 좀비(<see cref="NightVisitor"/>): 밤(21~04시)에만 나와 둔치·포구를 비틀비틀 오간다. 가까이 가면 멈춰 비명을 지르고 다시 걷는다 (해치지 않는다)
    /// - 귀신(X Bot + Fishing/Ghost): 밤에만 희미하게 나타나 플레이어를 바라본다. 6m 안으로 다가가면 사라졌다가 잠시 뒤 다른 곳에 나타난다
    /// 에셋이 없으면(CharacterLibrary 비어 있음) 아무것도 놓지 않는다.
    /// </summary>
    public static class ExtraNpcs
    {
        public static void Populate(FieldMapDef map, Transform world)
        {
            if (world == null || CharacterLibrary.Lib == null) return;
            var root = new GameObject("ExtraNpcs").transform;
            root.SetParent(world, false);
            float H(float x, float z) => map is CoastMapDef c ? c.HeightAt(x, z) : map.LandY;

            switch (map.Id)
            {
                case "hangang":
                {
                    float z = HanRiverMap.RiverMinZ - 1.6f;
                    if (ExternalCharacters.Available("unitychan"))
                    {
                        NpcAngler.Spawn(root, new Vector3(30f, HanRiverMap.LandY, z), -5f, new CharacterLook
                        {
                            External = "unitychan", Female = true, Name = "유니티짱", Title = "낚시 아이돌"
                        }, NpcAngler.Role.Angler);
                        IdolRoutine.Spawn(root, new Vector3(HanRiverMap.Pier.center.x - 14f, HanRiverMap.LandY, HanRiverMap.RiverMinZ - 9f), 160f);
                    }
                    NightVisitor.SpawnZombie(root, new Vector3(-10f, HanRiverMap.LandY, -70f), new Vector3(40f, HanRiverMap.LandY, -70f));
                    NightVisitor.SpawnGhost(root, new[]
                    {
                        new Vector3(HanRiverMap.BridgeX + 7f, HanRiverMap.LandY, HanRiverMap.RiverMinZ - 3f),
                        new Vector3(-30f, HanRiverMap.LandY, -66f),
                        new Vector3(95f, HanRiverMap.LandY, -64f),
                    });
                    break;
                }
                case "cheongjin":
                    NightVisitor.SpawnZombie(root, new Vector3(2f, map.LandY, -53f), new Vector3(48f, map.LandY, -53f));
                    break;
                case "geomun":
                case "deungdae":
                {
                    // 큰 등대 언덕 (곶 등뼈 72% 지점에 등대) 둘레
                    if (map is CoastMapDef c && c.S.Capes != null && c.S.Capes.Length > 0)
                    {
                        var cape = c.S.Capes[0];
                        var top = Vector2.Lerp(cape.Base, cape.Tip, 0.72f);
                        var mid = Vector2.Lerp(cape.Base, cape.Tip, 0.5f);
                        var pts = new List<Vector3>();
                        foreach (var p in new[] { top + new Vector2(-6f, -9f), mid + new Vector2(5f, 0f), top + new Vector2(8f, 6f) })
                            pts.Add(new Vector3(p.x, H(p.x, p.y), p.y));
                        NightVisitor.SpawnGhost(root, pts.ToArray());
                    }
                    break;
                }
                case "jeju":
                    // 돌하르방 옆 밤 바닷가의 하얀 그림자
                    NightVisitor.SpawnGhost(root, new[] { new Vector3(-40f, H(-40f, -58f), -58f), new Vector3(60f, H(60f, -56f), -56f) });
                    break;
            }
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
            v._model.SetActive(false);
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
            if (_model.activeSelf != night) _model.SetActive(night);
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
            if (_model.activeSelf != visible) _model.SetActive(visible);
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
}
