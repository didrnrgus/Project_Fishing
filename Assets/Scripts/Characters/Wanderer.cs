using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 받은 캐릭터로 만든 배경 사람 하나: 점 사이를 오가거나(점이 하나면 제자리) 끝점에서 쉬는 동작을 하고,
    /// 플레이어가 가까이 오면 돌아보고 반응 동작(인사·엄지 척·비명 …)을 한 번 한다.
    /// - when: 늘 / 낮(06~20시) / 밤(21~04시)에만 보인다 (게임 시계)
    /// - water: 헤엄치는 사람. 골반 뼈 높이를 수면(FieldMap.WaterY)에 맞춘다 (헤엄 = 몸이 수면, 제자리 = 머리만 위로)
    /// - seat: 앉는 동작 밑에 아이스박스 의자 (SitSeat)
    /// - 땅 높이는 매 프레임 아래로 레이캐스트 (지형·데크·방파제 위를 걷는다)
    /// </summary>
    public class Wanderer : MonoBehaviour
    {
        public enum When { Always, Day, Night }

        public class Spec
        {
            public string Id, Name, Title;
            public Vector3[] Points;
            public float Yaw;
            public string Walk = CharacterAnimator.Walk;
            public string Idle = CharacterAnimator.Idle;
            public string React;            // 가까이 오면 한 번
            public string Toast;            // 처음 반응할 때 알림 (한 번)
            /// <summary>WalkAnimSpeed 0 = 기본 걷기(Human Basic Motions, 원래 2 m/s)면 이동 속도에 맞춰 자동 (발이 미끄러지지 않게), 아니면 1.</summary>
            public float Speed = 1.2f, WalkAnimSpeed = 0f, Rest = 4f, ReactRadius = 5f;
            public When When = When.Always;
            public bool Water, Seat, Tag;
            public bool React_Sound;
        }

        Spec _s;
        bool _shown;
        GameObject _model;
        CharacterAnimator _anim;
        Animator _animator;
        Transform _hips, _tag;
        int _target;
        float _restUntil, _reactUntil, _nextReact;
        bool _toasted, _resting = true;

        public static Wanderer Spawn(Transform parent, Spec s)
        {
            if (s.Points == null || s.Points.Length == 0 || !ExternalCharacters.Available(s.Id))
            {
                Debug.Log($"[Dev] 사람 못 놓음: {s.Id} (점 {(s.Points == null ? 0 : s.Points.Length)}, 에셋 {ExternalCharacters.Available(s.Id)})");
                return null;
            }
            var go = new GameObject("Wanderer_" + s.Id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = s.Points[0];
            go.transform.localRotation = Quaternion.Euler(0f, s.Yaw, 0f);
            var w = go.AddComponent<Wanderer>();
            if (s.WalkAnimSpeed <= 0f) s.WalkAnimSpeed = s.Walk == CharacterAnimator.Walk ? s.Speed / CharacterAnimator.BasicWalkSpeed : 1f;
            w._s = s;
            w._model = ExternalCharacters.Create(s.Id, go.transform, s.Name);
            if (w._model == null) { Destroy(go); return null; }
            w._anim = w._model.GetComponentInChildren<CharacterAnimator>();
            w._animator = w._model.GetComponentInChildren<Animator>();
            if (w._animator != null && w._animator.isHuman) w._hips = w._animator.GetBoneTransform(HumanBodyBones.Hips);
            w._target = s.Points.Length > 1 ? 1 : 0;
            w._restUntil = Time.time + Random.Range(0f, s.Rest);
            w._anim.SetLoop(s.Idle);
            if (s.Seat)
            {
                var seat = SitSeat.Attach(go.transform);
                if (seat != null) seat.Active = true;
            }
            if (!s.Water)
            {
                // 사람 몸 (플레이어가 뚫고 지나가지 않게). 땅 레이캐스트는 자기 콜라이더를 건너뛴다
                var col = new GameObject("Body").AddComponent<CapsuleCollider>();
                col.transform.SetParent(go.transform, false);
                col.center = new Vector3(0f, 0.85f, 0f);
                col.height = 1.7f;
                col.radius = 0.28f;
            }
            if (s.Tag && !string.IsNullOrEmpty(s.Name))
                w._tag = CharacterFactory.AddNameTag(go.transform, s.Name, s.Title ?? "", ExternalCharacters.HeightOf(s.Id) + 0.3f);
            w.ApplySchedule(true);
            return w;
        }

        bool Visible(int hour) => _s.When switch
        {
            When.Day => hour >= 6 && hour < 21,
            When.Night => hour >= 21 || hour < 4,
            _ => true
        };

        void ApplySchedule(bool force)
        {
            bool vis = Game.Clock == null || Visible(Game.Clock.Hour);
            if (force || _shown != vis)
            {
                ModelVisibility.Set(_model, vis);
                _shown = vis;
                if (_tag != null) _tag.gameObject.SetActive(vis);
                foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = vis;
                if (vis && _anim != null) _anim.SetLoop(_resting ? _s.Idle : _s.Walk, _resting ? 1f : _s.WalkAnimSpeed);
            }
        }

        void Update()
        {
            if (_model == null || _anim == null) return;
            ApplySchedule(false);
            if (!_shown) return;

            var player = Game.Player != null ? Game.Player.transform : null;
            float d = player != null ? Vector3.Distance(player.position, transform.position) : 999f;

            // 반응: 가까이 오면 돌아보고 한 번
            if (!string.IsNullOrEmpty(_s.React) && d < _s.ReactRadius && Time.time > _nextReact)
            {
                _nextReact = Time.time + 20f;
                _reactUntil = Time.time + 3.5f;
                _anim.SetLoop(_s.Idle);
                _anim.PlayOnce(_s.React);
                if (_s.React_Sound) Game.Sound?.UiError();
                if (!_toasted && !string.IsNullOrEmpty(_s.Toast)) { _toasted = true; Game.Toast(_s.Toast, ToastKind.Info); }
            }
            if (Time.time < _reactUntil && player != null)
            {
                Face(player.position - transform.position, 5f);
                Snap();
                return;
            }

            if (_s.Points.Length < 2)
            {
                // 제자리: 집 방향으로 천천히 돌아간다
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0f, _s.Yaw, 0f), Time.deltaTime * 2f);
                Snap();
                return;
            }
            if (_resting)
            {
                if (Time.time < _restUntil) { Snap(); return; }
                _resting = false;
                _anim.SetLoop(_s.Walk, _s.WalkAnimSpeed);
            }
            var to = _s.Points[_target] - transform.localPosition;
            to.y = 0f;
            if (to.magnitude < 0.4f)
            {
                _target = (_target + 1) % _s.Points.Length;
                _resting = true;
                _restUntil = Time.time + _s.Rest * Random.Range(0.6f, 1.4f);
                _anim.SetLoop(_s.Idle);
                Snap();
                return;
            }
            // 끝점에서 돌아설 때: 먼저 그 자리에서 몸을 돌리고 나서 걷는다 (예전에는 몸이 돌기 전에 움직여 뒷걸음질·옆걸음질로 미끄러졌다)
            var worldTo = transform.parent != null ? transform.parent.TransformDirection(to) : to;
            float facing = Vector3.Dot(transform.forward, worldTo.normalized);
            Face(worldTo, facing < 0.7f ? 5f : 3f);
            float go = Mathf.Clamp01((facing - 0.7f) / 0.25f);
            transform.localPosition += to.normalized * Mathf.Min(to.magnitude, _s.Speed * go * Time.deltaTime);
            Snap();
        }

        void Face(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }

        /// <summary>땅 높이에 발을 붙인다 (헤엄치는 사람은 수면 기준이라 건너뛴다).</summary>
        void Snap()
        {
            if (_s.Water) return;
            var o = transform.position + Vector3.up * 2.5f;
            float best = float.MinValue;
            foreach (var h in Physics.RaycastAll(o, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(transform)) continue;
                if (Game.Player != null && h.transform.IsChildOf(Game.Player.transform)) continue;
                if (h.collider.name == "Water") continue;
                if (h.point.y > best) best = h.point.y;
            }
            if (best > float.MinValue)
            {
                var p = transform.position;
                p.y = Mathf.Lerp(p.y, best, 1f - Mathf.Exp(-12f * Time.deltaTime));
                transform.position = p;
            }
        }

        void LateUpdate()
        {
            if (_tag != null && _tag.gameObject.activeSelf && Game.CameraRig != null)
                _tag.rotation = Quaternion.LookRotation(_tag.position - Game.CameraRig.transform.position);
            if (!_s.Water || _hips == null || !_shown) return;
            // 헤엄: 골반을 수면 바로 아래, 제자리 헤엄: 골반을 수면 0.75m 아래 (머리·어깨만 나온다)
            bool swimming = !_resting && _s.Points.Length > 1;
            float want = FieldMap.WaterY + (swimming ? -0.12f : -0.78f) + Mathf.Sin(Time.time * 1.7f + transform.position.x) * 0.04f;
            float err = want - _hips.position.y;
            var p = transform.position;
            p.y += err * (1f - Mathf.Exp(-6f * Time.deltaTime));
            transform.position = p;
        }
    }
}
