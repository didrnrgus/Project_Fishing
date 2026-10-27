using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 절차 메쉬 버퍼 (위치·법선·꼭짓점 색): 모따기 블록, 볼록 단면 돌출, 매개변수 곡면을 모아 메쉬 하나로 굽는다.
    /// 꼭짓점 색은 틈·밑동 그늘과 조각마다 다른 색 → Fishing/Weathered의 _VertexAO(<see cref="Fishing.Core.Mats.VertexAO"/>)가 곱한다.
    /// 방파제·호안(<see cref="HarborKit"/>)과 집(<see cref="HouseKit"/>)이 같이 쓴다.
    /// </summary>
    public sealed class BlockMesh
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Color> _c = new List<Color>();
        readonly List<int> _t = new List<int>();
        public bool Empty => _t.Count == 0;

        int Add(Vector3 p, Vector3 n, Color c)
        {
            _v.Add(p);
            _n.Add(n);
            _c.Add(c);
            return _v.Count - 1;
        }

        /// <summary>삼각형 하나. 앞면이 n 쪽을 보도록 감는 방향을 맞춘다.</summary>
        void Tri(int a, int b, int c, Vector3 n)
        {
            if (Vector3.Dot(Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]), n) < 0f) (b, c) = (c, b);
            _t.Add(a);
            _t.Add(b);
            _t.Add(c);
        }

        void Quad(int a, int b, int c, int d, Vector3 n)
        {
            Tri(a, b, c, n);
            Tri(a, c, d, n);
        }

        /// <summary>
        /// 모따기 상자: 중심 c, 크기 size, 모따기 ch(m), 회전 rot. 꼭짓점 색은 밑(bottom)에서 위(top)로 번진다.
        /// 면마다 꼭짓점 4개를 두고 모서리 띠·귀퉁이 삼각형이 이웃 면의 꼭짓점을 잇는다 → 모서리 법선이 두 면 사이로 부드럽게 넘어간다.
        /// </summary>
        public void Block(Vector3 c, Vector3 size, float ch, Quaternion rot, Color top, Color bottom) =>
            Block(c, size, ch, rot, null, top, bottom);

        /// <summary>모따기 상자, 꼭짓점 색 = shade(버퍼 좌표 위치) — 이웃 블록과 이어지는 그늘 (개구부로 쪼갠 벽).</summary>
        public void Block(Vector3 c, Vector3 size, float ch, Quaternion rot, Func<Vector3, Color> shade) =>
            Block(c, size, ch, rot, shade, Color.white, Color.white);

        void Block(Vector3 c, Vector3 size, float ch, Quaternion rot, Func<Vector3, Color> shade, Color top, Color bottom)
        {
            var h = size * 0.5f;
            ch = Mathf.Max(0f, Mathf.Min(ch, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.9f));
            var e = h - Vector3.one * ch;
            // fv[면, 귀퉁이] : 면 = 축*2 + (양수면 1), 귀퉁이 = x·y·z 부호 비트
            var fv = new int[6, 8];
            for (int ax = 0; ax < 3; ax++)
            for (int s = 0; s < 2; s++)
            {
                var n = Vector3.zero;
                n[ax] = s == 0 ? -1f : 1f;
                var wn = rot * n;
                for (int k = 0; k < 8; k++)
                {
                    if (((k >> ax) & 1) != s) continue;
                    var p = Vector3.zero;
                    for (int q = 0; q < 3; q++)
                    {
                        float sg = ((k >> q) & 1) == 1 ? 1f : -1f;
                        p[q] = q == ax ? sg * h[q] : sg * e[q];
                    }
                    var wp = c + rot * p;
                    fv[ax * 2 + s, k] = Add(wp, wn, shade != null ? shade(wp) : Color.Lerp(bottom, top, (p.y + h.y) / (2f * h.y)));
                }
                // 면: 다른 두 축의 귀퉁이 네 개
                int u = (ax + 1) % 3, w = (ax + 2) % 3, f = ax * 2 + s, b0 = s << ax;
                Quad(fv[f, b0], fv[f, b0 | (1 << u)], fv[f, b0 | (1 << u) | (1 << w)], fv[f, b0 | (1 << w)], wn);
            }
            if (ch <= 0f) return;
            // 모서리 띠: 두 면(축 a, b)이 만나는 곳, 셋째 축 k를 따라
            for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 3; b++)
            {
                int k = 3 - a - b;
                for (int sa = 0; sa < 2; sa++)
                for (int sb = 0; sb < 2; sb++)
                {
                    int c0 = (sa << a) | (sb << b), c1 = c0 | (1 << k);
                    int fa = a * 2 + sa, fb = b * 2 + sb;
                    var n = (_n[fv[fa, c0]] + _n[fv[fb, c0]]).normalized;
                    Quad(fv[fa, c0], fv[fa, c1], fv[fb, c1], fv[fb, c0], n);
                }
            }
            // 귀퉁이 삼각형
            for (int k = 0; k < 8; k++)
            {
                int fx = (k & 1), fy = 2 + ((k >> 1) & 1), fz = 4 + ((k >> 2) & 1);
                Tri(fv[fx, k], fv[fy, k], fv[fz, k], (_n[fv[fx, k]] + _n[fv[fy, k]] + _n[fv[fz, k]]).normalized);
            }
        }

        /// <summary>네 점 면 하나 (n 쪽이 앞). 법선은 n 하나로 평평하게.</summary>
        public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color col) =>
            Quad(Add(a, n, col), Add(b, n, col), Add(c, n, col), Add(d, n, col), n);

        /// <summary>
        /// 매개변수 곡면 P(u, v), u·v ∈ [0, 1]을 nu × nv 칸으로 (골강판·기와·둥근 지붕). 법선은 곡면을 미분해서, hint 쪽이 앞.
        /// col(u, v) = 꼭짓점 색. shapeN이 있으면 법선은 그 곡면에서 (모양은 P대로 두고 음영만 완만하게 — 골강판 골 옆면이 해 반대쪽에서 새까매지지 않게).
        /// </summary>
        public void Surface(int nu, int nv, Func<float, float, Vector3> P, Vector3 hint, Func<float, float, Color> col, Func<float, float, Vector3> shapeN = null)
        {
            var N = shapeN ?? P;
            int b0 = _v.Count;
            const float e = 1e-3f;
            for (int j = 0; j <= nv; j++)
            for (int i = 0; i <= nu; i++)
            {
                float u = i / (float)nu, v = j / (float)nv;
                var du = N(Mathf.Min(1f, u + e), v) - N(Mathf.Max(0f, u - e), v);
                var dv = N(u, Mathf.Min(1f, v + e)) - N(u, Mathf.Max(0f, v - e));
                // du·dv를 먼저 정규화한다: 좁은 띠(기와 한 줄)는 미분이 0.7mm쯤이라 그냥 외적하면 크기가 1e-5 아래로 떨어져
                // Vector3.normalized가 0을 돌려줬다 → 법선이 없어 면이 아래를 보고 컬링됐다
                var n = Vector3.Cross(du.normalized, dv.normalized);
                n = n.sqrMagnitude > 1e-8f ? n.normalized : hint.normalized;
                if (Vector3.Dot(n, hint) < 0f) n = -n;
                Add(P(u, v), n, col(u, v));
            }
            int row = nu + 1;
            for (int j = 0; j < nv; j++)
            for (int i = 0; i < nu; i++)
            {
                int a = b0 + j * row + i, b = a + 1, c = a + row, d = c + 1;
                Quad(a, b, d, c, (_n[a] + _n[b] + _n[c] + _n[d]).normalized);
            }
        }

        /// <summary>
        /// 단면(prof: 로컬 x·y)을 z0~z1로 밀어 낸 기둥 (파라펫·박공 벽·아치 귀퉁이). 단면의 각 변은 평평한 면, 양 끝은 마개.
        /// 마개는 볼록하면 가운데에서, fanFirst면 첫 꼭짓점에서 부채꼴 (아치 귀퉁이처럼 한 꼭짓점에서 다 보이는 모양).
        /// 꼭짓점 색: 단면 아래쪽일수록 bottom.
        /// </summary>
        public void Extrude(IList<Vector2> prof, float z0, float z1, Vector3 origin, Quaternion rot, Color top, Color bottom, bool fanFirst = false)
        {
            var mid = Vector2.zero;
            float y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var p in prof)
            {
                mid += p;
                y0 = Mathf.Min(y0, p.y);
                y1 = Mathf.Max(y1, p.y);
            }
            mid /= prof.Count;
            Color Shade(float y) => Color.Lerp(bottom, top, Mathf.InverseLerp(y0, y1, y));
            Vector3 P(Vector2 p, float z) => origin + rot * new Vector3(p.x, p.y, z);
            int n = prof.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = prof[i], b = prof[(i + 1) % n];
                var d = (b - a).normalized;
                var nn = new Vector2(d.y, -d.x);
                if (Vector2.Dot(nn, (a + b) * 0.5f - mid) < 0f) nn = -nn;
                var wn = rot * new Vector3(nn.x, nn.y, 0f);
                Quad(Add(P(a, z0), wn, Shade(a.y)), Add(P(b, z0), wn, Shade(b.y)), Add(P(b, z1), wn, Shade(b.y)), Add(P(a, z1), wn, Shade(a.y)), wn);
            }
            foreach (var (z, sg) in new[] { (z0, -1f), (z1, 1f) })
            {
                var wn = rot * new Vector3(0f, 0f, sg);
                var fan = fanFirst ? prof[0] : mid;
                int ci = Add(P(fan, z), wn, Shade(fan.y));
                for (int i = fanFirst ? 1 : 0; i < (fanFirst ? n - 1 : n); i++)
                    Tri(ci, Add(P(prof[i], z), wn, Shade(prof[i].y)), Add(P(prof[(i + 1) % n], z), wn, Shade(prof[(i + 1) % n].y)), wn);
            }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, indexFormat = _v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(_v);
            m.SetNormals(_n);
            m.SetColors(_c);
            m.SetTriangles(_t, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>parent 아래 렌더러 하나로 내놓는다 (비었으면 null).</summary>
        public GameObject Emit(Transform parent, string name, Material mat)
        {
            if (Empty) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ToMesh(name);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }
    }
}
