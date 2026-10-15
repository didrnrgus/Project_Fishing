using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 1인칭 오른손: 낚싯대 손잡이를 실제로 감아 쥔 주먹 메쉬. 예전 주먹(구 + 작은 캡슐)은 손가락이 손바닥 구에 묻혀
    /// 검은 공 덩어리로만 보였다.
    /// 좌표는 낚싯대 로컬(축 +Y, 릴이 매달린 쪽 +Z)이고, 손가락 네 개는 손등 쪽(+X·−Z)의 손가락 뿌리에서 시작해
    /// 손잡이 아래(+Z)를 돌아 반대편(−X)까지 감긴다. 둘째 마디까지는 반장갑(천), 끝마디는 맨살. 엄지는 손잡이 위(−Z)에 얹어 앞으로 뻗는다.
    /// 손가락은 굽은 튜브 메쉬(마디마다 가늘어짐, 끝은 둥글게).
    /// </summary>
    public static class FpHand
    {
        const float GripR = 0.022f;   // 앞손잡이 반지름 (ButtDiameter 0.034 × 1.3 / 2)

        /// <summary>오른손: 낚싯대 앞손잡이를 쥔다 (낚싯대 로컬). 손목 점(팔이 이어질 곳)을 돌려준다.</summary>
        public static Transform BuildRight(Transform rod, Material glove, Material pad, Material skin, out Transform wrist)
        {
            var h = new GameObject("HandRight").transform;
            h.SetParent(rod, false);
            BuildFist(h, GripR, new[] { 0.122f, 0.103f, 0.084f, 0.066f }, 0.075f, 0.163f, glove, pad, skin, out wrist);
            return h;
        }

        /// <summary>
        /// 왼손: 릴 손잡이 노브(반지름 knobR)를 손끝으로 감아 쥔다. 로컬 +Y = 노브 축, 손등은 +X 쪽을 거울로 뒤집어(왼손) 쓴다.
        /// 자리·방향은 FightRodView가 매 프레임 노브에 맞춘다.
        /// </summary>
        public static Transform BuildLeft(Transform parent, float knobR, Material glove, Material pad, Material skin, out Transform wrist)
        {
            var h = new GameObject("HandLeft").transform;
            h.SetParent(parent, false);
            var m = new GameObject("Mirror").transform;
            m.SetParent(h, false);
            m.localScale = new Vector3(-1f, 1f, 1f); // 왼손 = 오른손 거울 (Unity가 음수 크기는 면 방향을 알아서 뒤집는다)
            BuildFist(m, knobR, new[] { 0.026f, 0.009f, -0.008f, -0.024f }, -0.02f, 0.05f, glove, pad, skin, out wrist);
            return h;
        }

        /// <summary>
        /// 손잡이(축 +Y, 반지름 gripR)를 감아 쥔 주먹. ys = 검지~새끼 손가락 높이, 엄지는 thumbY0에서 thumbY1까지 손잡이 위(−Z)로 뻗는다.
        /// </summary>
        static void BuildFist(Transform h, float gripR, float[] ys, float thumbY0, float thumbY1, Material glove, Material pad, Material skin, out Transform wrist)
        {
            float[] len = { 1f, 1.06f, 1f, 0.86f };
            float[] thick = { 0.0085f, 0.009f, 0.0085f, 0.0075f };
            // 손잡이가 가늘면(노브) 손가락이 더 많이 감긴다 → 각도를 둘레 길이로 맞춘다
            float arc = Mathf.Rad2Deg / (gripR + 0.009f);  // 1m 길이당 도
            float ymid = (ys[0] + ys[3]) * 0.5f;
            for (int f = 0; f < 4; f++)
            {
                float y = ys[f], k = len[f], t = thick[f];
                float rf = gripR + t * 0.95f;
                var knuckle = P(-28f, gripR + 0.02f, y + 0.004f);
                float a1 = 62f, a2 = a1 + Mathf.Min(150f, 0.034f * k * arc), a3 = a2 + Mathf.Min(110f, 0.024f * k * arc);
                var j1 = P(a1, rf, y);
                var j2 = P(a2, rf, y - 0.002f);
                var tip = P(a3, rf - 0.001f, y - 0.004f);
                Part(h, "Finger" + f, Tube(Smooth(new[] { knuckle, j1, j2 }, 5), t, t * 0.92f), glove);
                Part(h, "FingerTip" + f, Tube(Smooth(new[] { j2, tip }, 4), t * 0.9f, t * 0.72f, roundStart: true), skin);
                Part(h, "Knuckle" + f, Tube(new[] { P(-24f, gripR + 0.026f, y + 0.004f), P(-8f, gripR + 0.025f, y + 0.003f) }, t * 0.55f, t * 0.5f), pad);
            }

            // 손등: 손가락 뿌리 바로 뒤의 납작한 판 (예전처럼 크고 둥근 공이면 덩어리로 보인다)
            var palmC = P(-42f, gripR + 0.026f, ymid);
            var radial = new Vector3(palmC.x, 0f, palmC.z).normalized;
            var rot = Quaternion.LookRotation(radial, Vector3.up);
            Blob(h, "Palm", palmC, rot, new Vector3(0.05f, ys[0] - ys[3] + 0.03f, 0.026f), glove);
            Blob(h, "BackPad", palmC + radial * 0.011f, rot, new Vector3(0.036f, ys[0] - ys[3] + 0.01f, 0.01f), pad);

            // 엄지: 손바닥 아래쪽 뿌리에서 손잡이 위(−Z)로 올라와 앞(+Y)으로 뻗는다
            var t0 = P(-70f, gripR + 0.026f, thumbY0);
            var t1 = P(-90f, gripR + 0.011f, Mathf.Lerp(thumbY0, thumbY1, 0.42f));
            var t2 = P(-98f, gripR + 0.009f, Mathf.Lerp(thumbY0, thumbY1, 0.76f));
            var t3 = P(-100f, gripR + 0.008f, thumbY1);
            Part(h, "Thumb", Tube(Smooth(new[] { t0, t1, t2 }, 4), 0.0105f, 0.0095f), glove);
            Part(h, "ThumbTip", Tube(Smooth(new[] { t2, t3 }, 3), 0.009f, 0.0075f, roundStart: true), skin);

            // 손목: 손등 뒤로 빠져나가며 장갑 끝 (팔은 FightRodView가 이 점까지 잇는다)
            wrist = new GameObject("Wrist").transform;
            wrist.SetParent(h, false);
            // 손목은 손등보다 카메라 쪽으로 튀어나오면 아래팔이 손을 덮는다 → 손잡이 끝(−Y) 쪽 아래로
            wrist.localPosition = new Vector3(palmC.x * 0.75f, ys[3] - 0.075f, palmC.z * 0.75f);
            Part(h, "Cuff", Tube(new[] { palmC + Vector3.down * 0.02f, wrist.localPosition }, 0.022f, 0.021f, roundStart: true), glove);
        }

        static void Blob(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>낚싯대 로컬 좌표: 축(+Y) 둘레 각도(도, 0 = +X, 90 = +Z 릴 쪽), 반지름, 높이.</summary>
        static Vector3 P(float deg, float r, float y)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }

        static void Part(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Catmull-Rom으로 점 사이를 seg칸씩 나눠 부드러운 곡선으로.</summary>
        static Vector3[] Smooth(Vector3[] p, int seg)
        {
            var o = new List<Vector3>();
            for (int i = 0; i < p.Length - 1; i++)
            {
                var p0 = p[Mathf.Max(0, i - 1)];
                var p1 = p[i];
                var p2 = p[i + 1];
                var p3 = p[Mathf.Min(p.Length - 1, i + 2)];
                for (int s = 0; s < seg; s++)
                {
                    float t = s / (float)seg, t2 = t * t, t3 = t2 * t;
                    o.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            o.Add(p[p.Length - 1]);
            return o.ToArray();
        }

        /// <summary>
        /// 곡선을 따라가는 튜브: 반지름 r0 → r1로 가늘어지고, 끝은 반구처럼 둥글게 닫는다 (roundStart면 시작도).
        /// 단면 방향은 평행 이동으로 이어 붙여 꼬이지 않게.
        /// </summary>
        static Mesh Tube(Vector3[] pts, float r0, float r1, bool roundStart = false)
        {
            const int sides = 12, capRings = 3;
            var v = new List<Vector3>();
            var tri = new List<int>();
            int n = pts.Length;
            var tang = new Vector3[n];
            for (int i = 0; i < n; i++) tang[i] = (pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
            var normal = Vector3.Cross(tang[0], Mathf.Abs(tang[0].y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var rings = new List<(Vector3 c, Vector3 nrm, Vector3 bin, float r)>();
            // 시작 마개
            if (roundStart)
                for (int c = capRings; c >= 1; c--)
                {
                    float a = c / (float)capRings * Mathf.PI * 0.5f;
                    rings.Add((pts[0] - tang[0] * r0 * Mathf.Sin(a), normal, Vector3.Cross(tang[0], normal), r0 * Mathf.Cos(a) + 0.0001f));
                }
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    var q = Quaternion.FromToRotation(tang[i - 1], tang[i]);
                    normal = (q * normal).normalized;
                }
                float r = Mathf.Lerp(r0, r1, i / (float)(n - 1));
                rings.Add((pts[i], normal, Vector3.Cross(tang[i], normal), r));
            }
            // 끝 마개
            for (int c = 1; c <= capRings; c++)
            {
                float a = c / (float)capRings * Mathf.PI * 0.5f;
                rings.Add((pts[n - 1] + tang[n - 1] * r1 * Mathf.Sin(a), normal, Vector3.Cross(tang[n - 1], normal), r1 * Mathf.Cos(a) + 0.0001f));
            }
            foreach (var (c, nrm, bin, r) in rings)
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    v.Add(c + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * r);
                }
            for (int i = 0; i < rings.Count - 1; i++)
            for (int s = 0; s < sides; s++)
            {
                int a = i * (sides + 1) + s, b = a + sides + 1;
                tri.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
            var m = new Mesh { name = "FingerTube" };
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
