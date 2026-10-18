using System.Collections.Generic;
using Fishing.Characters;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 필드 납품소 (원작 2013 홍현 영상 1:36: 방파제 입구의 빨간 우체통 모양 기계, 머리 위 초록 마름모 + "납품소").
    /// 맵을 지은 뒤 조합 연락관 둘레의 빈 땅에 세운다. 가까이서 대화 키(F)를 누르면 납품 창(<see cref="Fishing.UI.DeliveryPanel"/>)이 열린다.
    /// 프리미티브로 짓는다 (원작 모델은 쓰지 않는다).
    /// </summary>
    public class DeliveryKiosk : MonoBehaviour
    {
        public const string Label = "납품소";
        /// <summary>대화 키가 먹는 거리 (m).</summary>
        public const float UseRadius = 2.4f;

        public static readonly List<DeliveryKiosk> All = new List<DeliveryKiosk>();

        Transform _marker, _tag;
        float _phase;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>pos에서 UseRadius 안의 가장 가까운 납품소 (없으면 null).</summary>
        public static DeliveryKiosk Nearest(Vector3 pos, float radius = UseRadius)
        {
            DeliveryKiosk best = null;
            float bestD = radius * radius;
            foreach (var k in All)
            {
                if (k == null) continue;
                var d = k.transform.position - pos;
                d.y = 0f;
                if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = k; }
            }
            return best;
        }

        /// <summary>조합 연락관 옆 빈 땅에 납품소를 세운다 (MapLoader가 월드를 지은 뒤 부른다).</summary>
        public static DeliveryKiosk Place(FieldMapDef map, Transform world)
        {
            if (map == null || world == null) return null;
            NpcAngler union = null;
            foreach (var n in NpcAngler.Talkable)
                if (n != null && n.NpcId == "union" && n.transform.IsChildOf(world)) { union = n; break; }

            Vector3 basePos = union != null ? union.transform.position : map.Spawn;
            float yaw = union != null ? union.transform.eulerAngles.y : 0f;
            var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Physics.SyncTransforms();   // 방금 지은 소품·NPC 충돌체 위치를 반영해야 겹침 검사가 맞다
            Vector3? at = null;
            // 통발 어부가 연락관 오른쪽에 먼저 서므로 왼쪽·뒤쪽부터 찾는다
            foreach (var off in new[] { -right * 3.2f, -fwd * 2.8f - right * 2.2f, -fwd * 2.8f + right * 2.2f, -right * 4.6f, right * 4.6f, fwd * 2.6f - right * 2.6f, right * 3.2f })
            {
                var p = basePos + off;
                if (map is CoastMapDef c) p.y = c.HeightAt(p.x, p.z);
                if (map.IsWater(new Vector3(p.x, map.BaseWaterY, p.z)) || !map.IsWalkable(p)) continue;
                if (Physics.CheckSphere(p + Vector3.up * 0.8f, 0.55f, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool crowded = false;
                foreach (var n in NpcAngler.Talkable)
                    if (n != null && n.transform.IsChildOf(world) && (n.transform.position - p).sqrMagnitude < 2.2f * 2.2f) { crowded = true; break; }
                if (crowded) continue;
                at = p;
                break;
            }
            var pos = at ?? basePos - right * 3.2f;

            var go = new GameObject("DeliveryKiosk");
            go.transform.SetParent(world, false);
            go.transform.position = pos;
            // 연락관과 같은 쪽(사람들이 오는 쪽)을 보게
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var kiosk = go.AddComponent<DeliveryKiosk>();
            kiosk.Build();
            return kiosk;
        }

        void Build()
        {
            var red = Mats.Lit(new Color(0.78f, 0.12f, 0.1f), 0.55f);
            var white = Mats.Lit(new Color(0.93f, 0.92f, 0.88f), 0.4f);
            var dark = Mats.Lit(new Color(0.12f, 0.12f, 0.13f), 0.3f);
            var metal = Mats.Lit(new Color(0.45f, 0.47f, 0.5f), 0.6f, 0.6f);
            var t = transform;

            // 받침 · 몸통(빨간 원통) · 둥근 지붕 · 흰 띠
            Mats.Prim(PrimitiveType.Cylinder, "Base", t, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.05f, 0.9f), metal, false);
            Mats.Prim(PrimitiveType.Cylinder, "Body", t, new Vector3(0f, 0.66f, 0f), new Vector3(0.7f, 0.56f, 0.7f), red, false);
            Mats.Prim(PrimitiveType.Sphere, "Dome", t, new Vector3(0f, 1.22f, 0f), new Vector3(0.7f, 0.34f, 0.7f), red, false);
            Mats.Prim(PrimitiveType.Cylinder, "BandLow", t, new Vector3(0f, 0.4f, 0f), new Vector3(0.72f, 0.025f, 0.72f), white, false);
            Mats.Prim(PrimitiveType.Cylinder, "BandHigh", t, new Vector3(0f, 1.12f, 0f), new Vector3(0.72f, 0.025f, 0.72f), white, false);
            // 앞면: 투입구, 작은 화면(은은히 빛남), 버튼
            Mats.Prim(PrimitiveType.Cube, "Slot", t, new Vector3(0f, 0.98f, 0.33f), new Vector3(0.34f, 0.05f, 0.06f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "Screen", t, new Vector3(0f, 0.74f, 0.335f), new Vector3(0.3f, 0.2f, 0.03f), Mats.Emissive(new Color(0.55f, 0.85f, 1f), 0.9f), false);
            Mats.Prim(PrimitiveType.Sphere, "Button", t, new Vector3(0.1f, 0.55f, 0.34f), new Vector3(0.06f, 0.06f, 0.04f), Mats.Emissive(new Color(0.4f, 1f, 0.45f), 1.2f), false);
            Mats.Prim(PrimitiveType.Cube, "Tray", t, new Vector3(0f, 0.3f, 0.33f), new Vector3(0.36f, 0.08f, 0.1f), metal, false);
            // 부딪히는 몸 (플레이어가 뚫고 지나가지 않게)
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.height = 1.5f;
            col.radius = 0.4f;

            // 머리 위 초록 마름모 표시 (빙글 돌며 오르내린다)
            _marker = new GameObject("Marker").transform;
            _marker.SetParent(t, false);
            _marker.localPosition = new Vector3(0f, 2.55f, 0f);
            Mats.Prim(PrimitiveType.Cube, "Outer", _marker, Vector3.zero, new Vector3(0.3f, 0.3f, 0.06f), Mats.Lit(new Color(0.08f, 0.3f, 0.08f), 0.5f), false,
                Quaternion.Euler(0f, 0f, 45f));
            Mats.Prim(PrimitiveType.Cube, "Inner", _marker, Vector3.zero, new Vector3(0.22f, 0.22f, 0.08f), Mats.Emissive(new Color(0.55f, 1f, 0.2f), 1.6f), false,
                Quaternion.Euler(0f, 0f, 45f));

            // 이름표 "납품소" (주황, 원작처럼 마름모 아래)
            _tag = new GameObject("NameTag").transform;
            _tag.SetParent(t, false);
            _tag.localPosition = new Vector3(0f, 2.05f, 0f);
            var text = Fishing.UI.UIFactory.WorldText(_tag, Label, Vector3.zero, new Color(1f, 0.72f, 0.3f), 2.2f);
            text.fontStyle = TMPro.FontStyles.Bold;
            _tag.gameObject.AddComponent<NameTagFade>();
            _phase = Random.Range(0f, 6f);
        }

        void Update()
        {
            if (_marker != null)
            {
                _marker.localRotation = Quaternion.Euler(0f, Time.time * 90f + _phase * 30f, 0f);
                _marker.localPosition = new Vector3(0f, 2.55f + Mathf.Sin(Time.time * 2.2f + _phase) * 0.06f, 0f);
            }
            if (_tag != null && Game.CameraRig != null)
                _tag.gameObject.SetActive((Game.CameraRig.transform.position - _tag.position).sqrMagnitude < 45f * 45f);
        }
    }
}
