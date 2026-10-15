using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 마디를 펼쳐서 쓰는 민물 낚싯대(뽑기대)와 스피닝 릴.
    ///  - 손잡이 마디(빗자루 길이) 안에 가는 마디들이 들어 있다가 끝 마디부터 차례로 뽑혀 나온다 (<see cref="Extension"/>)
    ///  - 끝으로 갈수록 가늘어지고, 마디 이음매에는 금속 링(페룰), 마디마다 줄 고리(가이드)가 있다
    ///  - 줄이 당기는 방향으로 휜다. 끝 마디일수록 많이 휘고, 연조대는 손잡이 쪽까지 휜다 (<see cref="Drive"/>)
    ///  - 릴은 장비 아이템마다 모양·색이 다르고, 감으면 손잡이와 로터가 돌고 줄이 풀리면 스풀이 거꾸로 돈다
    /// 좌표: 이 Transform의 +Y가 낚싯대 방향, 원점이 릴 시트(손으로 잡는 곳), +Z 쪽에 릴이 달린다.
    /// </summary>
    public class TelescopicRod : MonoBehaviour
    {
        public struct Style
        {
            public Color Blank, Ferrule, Grip;
            public float Length;      // 다 펼친 길이(m)
            public int Sections;      // 손잡이 마디 포함
            public float ButtDiameter, TipDiameter;
            public float Flex;
        }

        public static Style StyleFor(GearDef rod)
        {
            switch (rod?.Id)
            {
                case "rod_carbon":
                    return new Style { Blank = new Color(0.07f, 0.07f, 0.08f), Ferrule = new Color(0.85f, 0.68f, 0.25f), Grip = new Color(0.62f, 0.45f, 0.28f),
                        Length = 3.2f, Sections = 5, ButtDiameter = 0.034f, TipDiameter = 0.006f, Flex = rod.Flex };
                case "rod_bamboo":
                    return new Style { Blank = new Color(0.78f, 0.64f, 0.36f), Ferrule = new Color(0.35f, 0.22f, 0.12f), Grip = new Color(0.55f, 0.38f, 0.2f),
                        Length = 3.0f, Sections = 5, ButtDiameter = 0.036f, TipDiameter = 0.007f, Flex = rod.Flex };
                case "rod_heavy":
                    return new Style { Blank = new Color(0.1f, 0.15f, 0.36f), Ferrule = new Color(0.82f, 0.84f, 0.88f), Grip = new Color(0.12f, 0.12f, 0.12f),
                        Length = 3.6f, Sections = 4, ButtDiameter = 0.044f, TipDiameter = 0.01f, Flex = rod.Flex };
                case "rod_fly":
                    // 플라이대: 올리브색 블랭크, 코르크 손잡이, 세 마디
                    return new Style { Blank = new Color(0.28f, 0.34f, 0.2f), Ferrule = new Color(0.75f, 0.62f, 0.3f), Grip = new Color(0.72f, 0.56f, 0.36f),
                        Length = 2.7f, Sections = 3, ButtDiameter = 0.026f, TipDiameter = 0.004f, Flex = rod.Flex };
                case "rod_gege":
                    // 대회 보상: 거거(대왕조개) 껍데기처럼 진줏빛 흰 몸통에 금색 링
                    return new Style { Blank = new Color(0.9f, 0.88f, 0.82f), Ferrule = new Color(0.92f, 0.72f, 0.3f), Grip = new Color(0.32f, 0.2f, 0.12f),
                        Length = 3.3f, Sections = 5, ButtDiameter = 0.034f, TipDiameter = 0.006f, Flex = rod.Flex };
                case "rod_goldgege":
                    // 빙고 보상: 금빛 블랭크에 진주색 링
                    return new Style { Blank = new Color(0.86f, 0.66f, 0.22f), Ferrule = new Color(0.92f, 0.9f, 0.84f), Grip = new Color(0.3f, 0.18f, 0.1f),
                        Length = 3.3f, Sections = 5, ButtDiameter = 0.034f, TipDiameter = 0.006f, Flex = rod.Flex };
                case "rod_pungrim":
                    // 풍림화산: 검은 옻칠 블랭크, 붉은 링(띠), 금빛 손잡이
                    return new Style { Blank = new Color(0.05f, 0.04f, 0.04f), Ferrule = new Color(0.75f, 0.1f, 0.08f), Grip = new Color(0.78f, 0.6f, 0.2f),
                        Length = 3.4f, Sections = 5, ButtDiameter = 0.036f, TipDiameter = 0.007f, Flex = rod.Flex };
                case "rod_gt":
                    // GT 파핑대: 짧고 굵은 흰 블랭크, 검은 EVA 손잡이
                    return new Style { Blank = new Color(0.92f, 0.92f, 0.9f), Ferrule = new Color(0.2f, 0.2f, 0.22f), Grip = new Color(0.08f, 0.08f, 0.09f),
                        Length = 2.6f, Sections = 3, ButtDiameter = 0.042f, TipDiameter = 0.01f, Flex = rod.Flex };
                case "rod_nile":
                    // 나일 원투대: 모래색 블랭크, 청록 링 (나일강 펠루카 색)
                    return new Style { Blank = new Color(0.78f, 0.66f, 0.45f), Ferrule = new Color(0.15f, 0.5f, 0.55f), Grip = new Color(0.35f, 0.24f, 0.15f),
                        Length = 3.8f, Sections = 4, ButtDiameter = 0.044f, TipDiameter = 0.01f, Flex = rod.Flex };
                case "rod_gangbaram":
                    // Lv.5 보상: 강바람처럼 시원한 하늘색 블랭크, 은색 링, 검은 EVA 손잡이
                    return new Style { Blank = new Color(0.16f, 0.48f, 0.7f), Ferrule = new Color(0.82f, 0.84f, 0.88f), Grip = new Color(0.1f, 0.1f, 0.11f),
                        Length = 3.1f, Sections = 4, ButtDiameter = 0.032f, TipDiameter = 0.006f, Flex = rod.Flex };
                default:
                    return new Style { Blank = new Color(0.75f, 0.13f, 0.2f), Ferrule = new Color(0.8f, 0.8f, 0.84f), Grip = new Color(0.13f, 0.13f, 0.13f),
                        Length = 3.0f, Sections = 4, ButtDiameter = 0.032f, TipDiameter = 0.006f, Flex = rod?.Flex ?? 0.8f };
            }
        }

        const int JointsPerSection = 4;
        const float Overlap = 0.08f;     // 이음매에서 겹치는 길이 (마디 길이 비율)
        const float GripBelow = 0.32f;   // 릴 시트 아래 손잡이 길이

        public Transform Tip { get; private set; }
        public Transform Reel { get; private set; }
        public float Extension { get; private set; } = 1f;
        public Style Look { get; private set; }

        float _sectionLen;
        readonly List<Transform> _sectionRoots = new List<Transform>(); // [0] = 손잡이 마디
        readonly List<(Transform joint, float u)> _joints = new List<(Transform, float)>();

        // 휨 스프링
        float _bend, _bendVel;
        Vector3 _bendAxis = Vector3.right;

        // 릴
        Transform _handle, _rotor, _spool;
        float _handleTurns, _spoolSlip;

        float _reelScale = 1f;

        public static TelescopicRod Create(Transform parent, GearDef rod, GearDef reel, float lengthScale = 1f, float reelScale = 1f)
        {
            var go = new GameObject("TelescopicRod");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<TelescopicRod>();
            r._reelScale = reelScale;
            r.Build(StyleFor(rod), reel?.Id, lengthScale);
            return r;
        }

        /// <summary>장비를 바꿨을 때 다시 만든다.</summary>
        public void Rebuild(GearDef rod, GearDef reel, float lengthScale = 1f)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _sectionRoots.Clear();
            _joints.Clear();
            Build(StyleFor(rod), reel?.Id, lengthScale);
            SetExtension(Extension);
        }

        void Build(Style s, string reelId, float lengthScale)
        {
            s.Length *= lengthScale;
            Look = s;
            int n = Mathf.Max(2, s.Sections);
            _sectionLen = s.Length / (n - (n - 1) * Overlap);
            float sub = _sectionLen / JointsPerSection;

            var blank = Mats.Lit(s.Blank, 0.75f, 0.1f);
            var ferrule = Mats.Lit(s.Ferrule, 0.85f, 0.85f);
            var guideMat = Mats.Lit(new Color(0.85f, 0.86f, 0.9f), 0.9f, 0.9f);

            Transform parent = transform;
            for (int si = 0; si < n; si++)
            {
                float d0 = Mathf.Lerp(s.ButtDiameter, s.TipDiameter, si / (float)n);
                float d1 = Mathf.Lerp(s.ButtDiameter, s.TipDiameter, (si + 1) / (float)n);

                Transform first = null, joint = null;
                for (int j = 0; j < JointsPerSection; j++)
                {
                    var t = new GameObject(si == 0 ? $"Butt{j}" : $"S{si}J{j}").transform;
                    t.SetParent(j == 0 ? parent : joint, false);
                    if (j > 0) t.localPosition = new Vector3(0f, sub, 0f);
                    float d = Mathf.Lerp(d0, d1, (j + 0.5f) / JointsPerSection);
                    Mats.Prim(PrimitiveType.Cylinder, "Blank", t, new Vector3(0f, sub * 0.5f, 0f), new Vector3(d, sub * 0.5f, d), blank, false);
                    if (j > 0) Mats.Prim(PrimitiveType.Sphere, "Seam", t, Vector3.zero, Vector3.one * d, blank, false);
                    if (si > 0)
                    {
                        float u = ((si - 1) * (1f - Overlap) + j / (float)JointsPerSection) / ((n - 1) * (1f - Overlap));
                        _joints.Add((t, Mathf.Clamp01(u)));
                    }
                    if (j == 0) first = t;
                    joint = t;
                }

                if (si == 0)
                {
                    // 손잡이 마디: 릴 시트 아래로 손잡이, 위로 블랭크. 엉덩이 마개
                    first.localPosition = new Vector3(0f, -GripBelow, 0f);
                    var grip = Mats.Lit(s.Grip, 0.25f);
                    Mats.Prim(PrimitiveType.Cylinder, "Grip", first, new Vector3(0f, GripBelow * 0.5f - 0.02f, 0f),
                        new Vector3(s.ButtDiameter * 1.45f, GripBelow * 0.45f, s.ButtDiameter * 1.45f), grip, false);
                    Mats.Prim(PrimitiveType.Cylinder, "ForeGrip", first, new Vector3(0f, GripBelow + 0.09f, 0f),
                        new Vector3(s.ButtDiameter * 1.3f, 0.06f, s.ButtDiameter * 1.3f), grip, false);
                    Mats.Prim(PrimitiveType.Sphere, "ButtCap", first, new Vector3(0f, -0.005f, 0f),
                        new Vector3(s.ButtDiameter * 1.5f, 0.02f, s.ButtDiameter * 1.5f), ferrule, false);
                    Mats.Prim(PrimitiveType.Cylinder, "ReelSeat", first, new Vector3(0f, GripBelow, 0f),
                        new Vector3(s.ButtDiameter * 1.25f, 0.035f, s.ButtDiameter * 1.25f), ferrule, false);
                }
                else
                {
                    // 펼친 상태 기준 위치. SetExtension이 접힌 정도에 따라 뒤로 민다
                    first.localPosition = new Vector3(0f, sub - Overlap * _sectionLen, 0f);
                }

                // 마디 끝 금속 링 + 가이드
                var endJoint = joint;
                Mats.Prim(PrimitiveType.Cylinder, "Ferrule", endJoint, new Vector3(0f, sub - 0.012f, 0f),
                    new Vector3(d1 * 1.35f, 0.012f, d1 * 1.35f), ferrule, false);
                if (si > 0 || n == 1)
                {
                    float gs = Mathf.Max(0.012f, d1 * 2.2f);
                    var guide = Mats.Prim(PrimitiveType.Cylinder, "Guide", endJoint, new Vector3(0f, sub - 0.05f, d1 * 0.5f + gs * 0.5f),
                        new Vector3(gs, 0.0015f, gs), guideMat, false, Quaternion.Euler(90f, 0f, 0f));
                    guide.name = "Guide";
                }

                _sectionRoots.Add(first);
                parent = endJoint;
            }

            Tip = new GameObject("Tip").transform;
            Tip.SetParent(parent, false);
            Tip.localPosition = new Vector3(0f, sub, 0f);
            Mats.Prim(PrimitiveType.Sphere, "TipTop", Tip, Vector3.zero, Vector3.one * Mathf.Max(0.01f, s.TipDiameter * 1.8f), guideMat, false);

            BuildReel(reelId, s);
        }

        // ---------------- 릴 ----------------

        void BuildReel(string reelId, Style rod)
        {
            Color body, accent, spoolCol;
            float size;
            bool doubleKnob = false;
            switch (reelId)
            {
                case "reel_power":
                    body = new Color(0.08f, 0.08f, 0.09f); accent = new Color(0.9f, 0.7f, 0.25f); spoolCol = new Color(0.85f, 0.66f, 0.22f); size = 1.2f; break;
                case "reel_hispeed":
                    body = new Color(0.15f, 0.3f, 0.6f); accent = new Color(0.85f, 0.87f, 0.9f); spoolCol = new Color(0.75f, 0.78f, 0.85f); size = 1.1f; doubleKnob = true; break;
                case "reel_napal":
                    // 대회 보상: 나팔고둥 껍데기 색 (크림색 몸통, 주황 갈색 무늬)
                    body = new Color(0.93f, 0.85f, 0.7f); accent = new Color(0.72f, 0.36f, 0.16f); spoolCol = new Color(0.85f, 0.55f, 0.3f); size = 1.15f; break;
                case "reel_pungrim":
                    // 풍림화산: 검은 옻칠 몸통, 붉은 테, 금빛 스풀
                    body = new Color(0.05f, 0.04f, 0.04f); accent = new Color(0.78f, 0.1f, 0.08f); spoolCol = new Color(0.85f, 0.66f, 0.22f); size = 1.2f; doubleKnob = true; break;
                case "reel_jigging":
                    body = new Color(0.12f, 0.14f, 0.18f); accent = new Color(0.85f, 0.55f, 0.15f); spoolCol = new Color(0.8f, 0.82f, 0.86f); size = 1.35f; doubleKnob = true; break;
                case "reel_victory5":
                    // Lv.4 보상: 빨간 몸통에 은색 테
                    body = new Color(0.78f, 0.12f, 0.16f); accent = new Color(0.9f, 0.9f, 0.93f); spoolCol = new Color(0.7f, 0.72f, 0.76f); size = 1.05f; break;
                default:
                    body = new Color(0.7f, 0.71f, 0.74f); accent = new Color(0.2f, 0.2f, 0.22f); spoolCol = new Color(0.62f, 0.63f, 0.66f); size = 1f; break;
            }
            var bodyMat = Mats.Lit(body, 0.75f, 0.7f);
            var accentMat = Mats.Lit(accent, 0.8f, 0.8f);
            var spoolMat = Mats.Lit(spoolCol, 0.85f, 0.85f);
            var lineMat = Mats.Lit(new Color(0.85f, 0.95f, 0.85f), 0.5f);
            var knobMat = Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.4f);

            Reel = new GameObject("Reel").transform;
            Reel.SetParent(transform, false);
            Reel.localScale = Vector3.one * size * _reelScale;

            // 다리(시트에 물리는 부분)와 몸통
            Mats.Prim(PrimitiveType.Cube, "Foot", Reel, new Vector3(0f, 0f, 0.028f), new Vector3(0.012f, 0.05f, 0.04f), bodyMat, false);
            Mats.Prim(PrimitiveType.Sphere, "Body", Reel, new Vector3(0f, -0.005f, 0.07f), new Vector3(0.042f, 0.06f, 0.05f), bodyMat, false);
            Mats.Prim(PrimitiveType.Cylinder, "SidePlate", Reel, new Vector3(-0.022f, -0.005f, 0.07f), new Vector3(0.038f, 0.004f, 0.038f),
                accentMat, false, Quaternion.Euler(0f, 0f, 90f));

            // 로터(베일 포함): 낚싯대 방향(Y) 축으로 돈다
            _rotor = new GameObject("Rotor").transform;
            _rotor.SetParent(Reel, false);
            _rotor.localPosition = new Vector3(0f, 0.03f, 0.075f);
            Mats.Prim(PrimitiveType.Cylinder, "RotorCup", _rotor, Vector3.zero, new Vector3(0.042f, 0.012f, 0.042f), bodyMat, false);
            Mats.Prim(PrimitiveType.Cube, "BailArmL", _rotor, new Vector3(-0.024f, 0.022f, 0f), new Vector3(0.004f, 0.04f, 0.006f), accentMat, false);
            Mats.Prim(PrimitiveType.Cube, "BailArmR", _rotor, new Vector3(0.024f, 0.022f, 0f), new Vector3(0.004f, 0.04f, 0.006f), accentMat, false);
            Mats.Prim(PrimitiveType.Cylinder, "BailWire", _rotor, new Vector3(0f, 0.042f, 0.012f), new Vector3(0.003f, 0.025f, 0.003f),
                accentMat, false, Quaternion.Euler(0f, 0f, 90f));

            // 스풀(줄 감긴 원통): 드랙이 미끄러지면 거꾸로 돈다
            _spool = new GameObject("Spool").transform;
            _spool.SetParent(Reel, false);
            _spool.localPosition = new Vector3(0f, 0.052f, 0.075f);
            Mats.Prim(PrimitiveType.Cylinder, "SpoolRim", _spool, new Vector3(0f, 0.012f, 0f), new Vector3(0.04f, 0.003f, 0.04f), spoolMat, false);
            Mats.Prim(PrimitiveType.Cylinder, "Line", _spool, Vector3.zero, new Vector3(0.034f, 0.011f, 0.034f), lineMat, false);
            Mats.Prim(PrimitiveType.Cube, "DragKnob", _spool, new Vector3(0f, 0.018f, 0f), new Vector3(0.012f, 0.006f, 0.02f), accentMat, false);
            Mats.Prim(PrimitiveType.Cube, "SpoolMark", _spool, new Vector3(0.017f, 0f, 0f), new Vector3(0.002f, 0.022f, 0.006f), accentMat, false);

            // 손잡이: 왼쪽(-X)에 달려 X축으로 돈다
            _handle = new GameObject("Handle").transform;
            _handle.SetParent(Reel, false);
            _handle.localPosition = new Vector3(-0.03f, -0.005f, 0.07f);
            Mats.Prim(PrimitiveType.Cube, "Arm", _handle, new Vector3(-0.004f, 0f, 0.03f), new Vector3(0.005f, 0.008f, 0.062f), accentMat, false);
            Mats.Prim(PrimitiveType.Capsule, "Knob", _handle, new Vector3(-0.018f, 0f, 0.058f), new Vector3(0.012f, 0.014f, 0.012f),
                knobMat, false, Quaternion.Euler(0f, 0f, 90f));
            if (doubleKnob)
            {
                Mats.Prim(PrimitiveType.Cube, "Arm2", _handle, new Vector3(-0.004f, 0f, -0.03f), new Vector3(0.005f, 0.008f, 0.062f), accentMat, false);
                Mats.Prim(PrimitiveType.Capsule, "Knob2", _handle, new Vector3(-0.018f, 0f, -0.058f), new Vector3(0.012f, 0.014f, 0.012f),
                    knobMat, false, Quaternion.Euler(0f, 0f, 90f));
            }
        }

        /// <summary>릴 손잡이 끝(손이 쥐는 곳). 1인칭에서 왼손을 붙인다.</summary>
        public Transform HandleKnob => _handle != null ? _handle.Find("Knob") : null;

        /// <summary>
        /// 릴을 돌린다. turnsPerSec: 손잡이 회전(초당 바퀴, 감기), lineOutSpeed: 줄이 풀려 나가는 속도(m/s, 드랙).
        /// 기어비 5.2:1 이라 로터는 손잡이보다 5배 빨리 돈다.
        /// </summary>
        public void SpinReel(float turnsPerSec, float lineOutSpeed, float dt)
        {
            if (_handle == null) return;
            _handleTurns += turnsPerSec * dt;
            _spoolSlip -= lineOutSpeed * dt / (Mathf.PI * 0.034f); // 스풀 둘레만큼 풀릴 때 한 바퀴
            _handle.localRotation = Quaternion.Euler(_handleTurns * 360f, 0f, 0f);
            _rotor.localRotation = Quaternion.Euler(0f, _handleTurns * 360f * 5.2f, 0f);
            // 감을 때 스풀이 앞뒤로 오르내린다(오실레이션)
            float osc = Mathf.Sin(_handleTurns * Mathf.PI * 2f / 6f) * 0.006f;
            _spool.localPosition = new Vector3(0f, 0.052f + osc, 0.075f);
            _spool.localRotation = Quaternion.Euler(0f, _spoolSlip * 360f, 0f);
        }

        // ---------------- 펼치기 ----------------

        /// <summary>0이면 다 접힌 상태(손잡이 마디 길이), 1이면 다 펼친 상태. 끝 마디부터 차례로 뽑혀 나온다.</summary>
        public void SetExtension(float t)
        {
            Extension = Mathf.Clamp01(t);
            int moving = _sectionRoots.Count - 1;
            float sub = _sectionLen / JointsPerSection;
            for (int si = 1; si < _sectionRoots.Count; si++)
            {
                // 끝 마디(si = moving)가 먼저 나온다
                float e = Mathf.Clamp01(Extension * moving - (moving - si));
                e = e * e * (3f - 2f * e);
                float extended = sub - Overlap * _sectionLen;
                float collapsed = sub - _sectionLen + 0.01f;
                _sectionRoots[si].localPosition = new Vector3(0f, Mathf.Lerp(collapsed, extended, e), 0f);
            }
        }

        /// <summary>지금 몇 번째 마디까지 다 뽑혔는지 (소리 내기용).</summary>
        public int ExtendedSections => Mathf.FloorToInt(Extension * (_sectionRoots.Count - 1) + 0.001f);

        // ---------------- 휨 ----------------

        /// <summary>
        /// 낚싯대를 줄 방향으로 휜다. targetDeg: 목표 휨각, pullWorld: 줄이 당기는 방향(월드), jitterDeg: 떨림.
        /// 덜 감쇠된 스프링으로 따라가서 장력이 튀면 출렁인다. 끝 마디일수록 많이 휘고, 연조대는 손잡이 쪽까지 휜다.
        /// </summary>
        public void Drive(float targetDeg, Vector3 pullWorld, float jitterDeg, float dt)
        {
            dt = Mathf.Min(dt, 0.05f);
            targetDeg *= Extension; // 접혀 있으면 휘지 않는다
            const float stiffness = 140f;
            float damping = 2f * Mathf.Sqrt(stiffness) * 0.32f;
            _bendVel += ((targetDeg - _bend) * stiffness - _bendVel * damping) * dt;
            _bend += _bendVel * dt;

            var local = transform.InverseTransformDirection(pullWorld);
            var pull = new Vector3(local.x, 0f, local.z);
            if (pull.sqrMagnitude < 1e-4f) pull = Vector3.forward;
            var axis = Vector3.Cross(Vector3.up, pull.normalized);
            _bendAxis = Vector3.Slerp(_bendAxis, axis, 1f - Mathf.Exp(-8f * dt));

            float exponent = Mathf.Lerp(2.4f, 1.3f, Look.Flex);
            float total = 0f;
            foreach (var (_, u) in _joints) total += Mathf.Pow(Mathf.Max(u, 0.02f), exponent);
            float bend = Mathf.Max(0f, _bend + jitterDeg);
            foreach (var (joint, u) in _joints)
            {
                float w = Mathf.Pow(Mathf.Max(u, 0.02f), exponent) / total;
                joint.localRotation = Quaternion.AngleAxis(bend * w, _bendAxis);
            }
        }

        /// <summary>순간적으로 휨 속도를 더한다 (카운터, 챔질의 반동).</summary>
        public void Kick(float degPerSec) => _bendVel += degPerSec;
    }
}
