using Fishing.Core;
using Fishing.World;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>찌. 날아가서 수면에 떠 있다가 입질이 오면 깔짝이거나 쭉 빨려 들어간다.</summary>
    public class Bobber : MonoBehaviour
    {
        enum State { Hidden, Flying, Floating }

        const float MaxDip = 0.5f;

        public bool Landed => _state == State.Floating;
        public Vector3 Position => transform.position;
        /// <summary>찌가 잠긴 정도 0~1. 예신은 0.15 정도, 본신은 1까지.</summary>
        public float SinkAmount => _state == State.Floating ? Mathf.Clamp01(_dip / MaxDip) : 0f;

        State _state = State.Hidden;
        Vector3 _from, _to;
        float _t, _duration, _arc;
        float _dip, _dipTarget;
        float _nibbleTimer;
        bool _sunk;

        public static Bobber Create()
        {
            var go = new GameObject("Bobber");
            var b = go.AddComponent<Bobber>();
            Mats.Prim(PrimitiveType.Sphere, "Top", go.transform, new Vector3(0f, 0.04f, 0f), Vector3.one * 0.14f, Mats.Lit(new Color(0.95f, 0.2f, 0.15f), 0.6f), false);
            Mats.Prim(PrimitiveType.Sphere, "Bottom", go.transform, new Vector3(0f, -0.03f, 0f), Vector3.one * 0.12f, Mats.Lit(new Color(0.97f, 0.97f, 0.95f), 0.6f), false);
            Mats.Prim(PrimitiveType.Cylinder, "Antenna", go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.02f, 0.13f, 0.02f), Mats.Lit(new Color(0.1f, 0.1f, 0.1f)), false);
            Mats.Prim(PrimitiveType.Sphere, "Tip", go.transform, new Vector3(0f, 0.34f, 0f), Vector3.one * 0.05f, Mats.Emissive(new Color(1f, 0.85f, 0.1f), 2f), false);
            // 실제 크기로는 15m 밖에서 거의 안 보여서 게임답게 키운다
            go.transform.localScale = Vector3.one * 2.2f;
            go.SetActive(false);
            return b;
        }

        public void Launch(Vector3 from, Vector3 to)
        {
            gameObject.SetActive(true);
            _from = from;
            _to = new Vector3(to.x, FieldMap.WaterY, to.z);
            float dist = Vector3.Distance(from, _to);
            _duration = Mathf.Lerp(0.45f, 1.1f, dist / 32f);
            _arc = dist * 0.2f + 1f;
            _t = 0f;
            _dip = _dipTarget = 0f;
            _sunk = false;
            _state = State.Flying;
            transform.position = from;
        }

        /// <summary>릴을 감아 떠 있는 찌를 끌어온다.</summary>
        public void DragTo(Vector3 pos)
        {
            if (_state != State.Floating) return;
            _to = new Vector3(pos.x, FieldMap.WaterY, pos.z);
        }

        GameObject _lure;
        bool _lureLook;

        /// <summary>
        /// 루어 낚시면 찌 대신 수면 바로 아래의 루어가 보인다 (스푼은 은빛, 에기는 주황, 플라이는 털바늘).
        /// 게임답게 키운 크기라 멀리서도 반짝인다.
        /// </summary>
        public void SetLureLook(bool lure, Fishing.Data.BaitKind kind)
        {
            _lureLook = lure;
            if (_lure != null) { Destroy(_lure); _lure = null; }
            foreach (Transform child in transform) child.gameObject.SetActive(!lure);
            if (!lure) return;
            _lure = new GameObject("Lure");
            _lure.transform.SetParent(transform, false);
            var color = kind switch
            {
                Fishing.Data.BaitKind.Egi => new Color(1f, 0.5f, 0.15f),
                Fishing.Data.BaitKind.Fly => new Color(0.95f, 0.85f, 0.55f),
                _ => new Color(0.9f, 0.9f, 0.95f),
            };
            var mat = kind == Fishing.Data.BaitKind.Lure ? Mats.Lit(color, 0.95f, 0.9f) : Mats.Lit(color, 0.5f);
            Mats.Prim(PrimitiveType.Sphere, "Body", _lure.transform, Vector3.zero, new Vector3(0.06f, 0.025f, 0.16f), mat, false);
            if (kind == Fishing.Data.BaitKind.Fly)
                Mats.Prim(PrimitiveType.Sphere, "Hackle", _lure.transform, new Vector3(0f, 0.02f, 0.06f), new Vector3(0.1f, 0.06f, 0.05f), Mats.Lit(new Color(0.5f, 0.35f, 0.2f)), false);
            Mats.Prim(PrimitiveType.Sphere, "Glint", _lure.transform, new Vector3(0f, 0.015f, 0f), Vector3.one * 0.03f, Mats.Emissive(new Color(1f, 0.95f, 0.8f), 1.2f), false);
        }

        public void Hide()
        {
            _state = State.Hidden;
            gameObject.SetActive(false);
        }

        /// <summary>약한 입질: 찌가 살짝 깔짝인다.</summary>
        public void Nibble()
        {
            _nibbleTimer = 0.25f;
            _dipTarget = 0.07f;
            Effects.Ripple(transform.position, 0.8f, 0.8f);
        }

        /// <summary>본 입질: 찌가 점점 빨려 들어간다. progress 0~1.</summary>
        public void SetSink(float progress)
        {
            if (!_sunk)
            {
                _sunk = true;
                Effects.Ripple(transform.position, 1.4f, 1f);
                Effects.Splash(transform.position, 8, 0.5f);
            }
            _dipTarget = MaxDip * Mathf.Clamp01(progress);
        }

        public void ResetFloat()
        {
            _sunk = false;
            _dipTarget = 0f;
        }

        void Update()
        {
            switch (_state)
            {
                case State.Flying:
                    _t += Time.deltaTime / _duration;
                    var p = Vector3.Lerp(_from, _to, _t);
                    p.y += Mathf.Sin(Mathf.Clamp01(_t) * Mathf.PI) * _arc;
                    transform.position = p;
                    if (_t >= 1f)
                    {
                        _state = State.Floating;
                        transform.position = _to;
                        Effects.Ripple(_to, 1.8f, 1.2f);
                        Effects.Splash(_to, 10, 0.7f);
                        if (Game.Sound != null) Game.Sound.Splash(0.6f);
                    }
                    break;

                case State.Floating:
                    if (_nibbleTimer > 0f)
                    {
                        _nibbleTimer -= Time.deltaTime;
                        if (_nibbleTimer <= 0f && !_sunk) _dipTarget = 0f;
                    }
                    _dip = Mathf.Lerp(_dip, _dipTarget, Time.deltaTime * (_sunk ? 14f : 18f));
                    float bob = Mathf.Sin(Time.time * 2.1f) * 0.012f;
                    transform.position = new Vector3(_to.x, FieldMap.WaterY + bob - _dip - (_lureLook ? 0.05f : 0f), _to.z);
                    transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f) * 4f, 0f, Mathf.Cos(Time.time * 1.1f) * 4f);
                    break;
            }
        }
    }
}
