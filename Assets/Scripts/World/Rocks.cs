using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 절차적 바위 메쉬. 예전에는 구 프리미티브를 찌그러뜨려 바위로 썼는데, 매끈한 알 모양에 텍스처가 극점으로 몰려 늘어났다.
    /// 여기서는 정이십면체 구(3번 쪼갬)를 3D 노이즈로 울퉁불퉁하게 만들고, 무작위 평면 몇 개로 깎아 절리면을 내고,
    /// 밑은 눌러 평평하게 해서 물·땅에 앉게 한다. 텍스처 좌표는 삼각형마다 가장 가까운 축으로 투영해 늘어나지 않는다.
    /// 변형 8가지를 처음 쓸 때 만들어 공유한다 (위치로 고르므로 같은 맵은 늘 같은 모양).
    /// </summary>
    public static class Rocks
    {
        const int Variants = 8;
        static Mesh[] _meshes;

        /// <summary>눈 덮인 맵(청진포구)이면 맵을 짓는 동안 true — 실사 바위를 눈 덮인 판으로.</summary>
        public static bool Snowy;

        /// <summary>Mats.Prim(PrimitiveType.Sphere, …)과 같은 인자: 지름 1 기준 크기(scale)로 놓는다.
        /// 실사 바위 에셋(Rock and Boulders 2)이 있으면 그것을 같은 크기로 (재질은 에셋 것), 없으면 절차적 메쉬.</summary>
        public static GameObject Prim(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
        {
            if (VegetationLibrary.HasRocks)
            {
                var lib = VegetationLibrary.Lib;
                var list = Snowy && lib.snowRocks != null && lib.snowRocks.Length > 0 ? lib.snowRocks : lib.rocks;
                int k = Mathf.Abs(Mathf.FloorToInt(pos.x * 7.13f) * 31 + Mathf.FloorToInt(pos.z * 3.71f) * 17 + Mathf.FloorToInt(pos.y * 5.3f)) % list.Length;
                var rock = Object.Instantiate(list[k], parent, false);
                rock.name = name;
                foreach (var c in rock.GetComponentsInChildren<Collider>(true)) Object.Destroy(c); // 예전 바위처럼 몸 없음 (물가 캐스팅을 막지 않게)
                rock.transform.localPosition = Vector3.zero;
                rock.transform.localRotation = Quaternion.identity;
                rock.transform.localScale = Vector3.one;
                var b = KenneyProps.LocalBounds(rock);
                var size = new Vector3(Mathf.Max(0.01f, b.size.x), Mathf.Max(0.01f, b.size.y), Mathf.Max(0.01f, b.size.z));
                // 축마다 크기를 맞추되 모양이 너무 찌그러지지 않게 평균 쪽으로 반쯤 당긴다
                var sv = new Vector3(scale.x / size.x, scale.y / size.y, scale.z / size.z);
                float avg = (sv.x + sv.y + sv.z) / 3f;
                sv = Vector3.Lerp(sv, Vector3.one * avg, 0.5f);
                var q = rot ?? Quaternion.identity;
                rock.transform.localScale = sv;
                rock.transform.localRotation = q;
                rock.transform.localPosition = pos - q * Vector3.Scale(b.center, sv);
                return rock;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            int v = Mathf.Abs(Mathf.FloorToInt(pos.x * 7.13f) * 31 + Mathf.FloorToInt(pos.z * 3.71f) * 17 + Mathf.FloorToInt(pos.y * 5.3f)) % Variants;
            go.AddComponent<MeshFilter>().sharedMesh = Mesh(v);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static Mesh Mesh(int variant)
        {
            _meshes ??= new Mesh[Variants];
            variant = ((variant % Variants) + Variants) % Variants;
            return _meshes[variant] ??= Build(variant);
        }

        static Mesh Build(int seed)
        {
            var rng = new System.Random(9173 + seed * 7919);
            float Rf(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
            Icosphere(3, out var verts, out var tris);

            // 깎는 평면: 위·옆으로 3~5개 (절리면), 각 평면은 중심에서 0.55~0.8 거리
            int cuts = 3 + rng.Next(3);
            var planes = new List<(Vector3 n, float d)>();
            for (int i = 0; i < cuts; i++)
            {
                var n = new Vector3(Rf(-1f, 1f), Rf(-0.2f, 1f), Rf(-1f, 1f)).normalized;
                planes.Add((n, Rf(0.55f, 0.8f)));
            }
            var off = new Vector3(Rf(0f, 50f), Rf(0f, 50f), Rf(0f, 50f));
            for (int i = 0; i < verts.Count; i++)
            {
                var u = verts[i];
                // 큰 굴곡 + 잔 울퉁불퉁
                float d = 1f + 0.22f * (Fbm(u * 1.6f + off) - 0.5f) * 2f + 0.06f * (Noise(u * 6f + off) - 0.5f) * 2f;
                var p = u * d;
                foreach (var (n, pd) in planes)
                {
                    float over = Vector3.Dot(p, n) - pd;
                    if (over > 0f) p -= n * over * 0.9f;
                }
                // 밑면: 눌러서 평평하게
                if (p.y < -0.35f) p.y = -0.35f + (p.y + 0.35f) * 0.25f;
                verts[i] = p * 0.5f; // 지름 1
            }

            // 공유 정점으로 부드러운 법선을 구한 뒤, 삼각형마다 정점을 따로 떼어 축 투영 UV를 준다
            var smooth = new Mesh();
            smooth.SetVertices(verts);
            smooth.SetTriangles(tris, 0);
            smooth.RecalculateNormals();
            var sn = smooth.normals;
            Object.Destroy(smooth);

            var v = new List<Vector3>(tris.Count);
            var nrm = new List<Vector3>(tris.Count);
            var uv = new List<Vector2>(tris.Count);
            var t = new List<int>(tris.Count);
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                var fn = Vector3.Cross(b - a, c - a);
                float ax = Mathf.Abs(fn.x), ay = Mathf.Abs(fn.y), az = Mathf.Abs(fn.z);
                for (int k = 0; k < 3; k++)
                {
                    var p = verts[tris[i + k]];
                    v.Add(p);
                    nrm.Add(sn[tris[i + k]]);
                    uv.Add(ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y));
                    t.Add(i + k);
                }
            }
            var mesh = new Mesh { name = "Rock" + seed };
            mesh.SetVertices(v);
            mesh.SetNormals(nrm);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Icosphere(int subdiv, out List<Vector3> verts, out List<int> tris)
        {
            float g = (1f + Mathf.Sqrt(5f)) * 0.5f;
            verts = new List<Vector3>
            {
                new(-1, g, 0), new(1, g, 0), new(-1, -g, 0), new(1, -g, 0),
                new(0, -1, g), new(0, 1, g), new(0, -1, -g), new(0, 1, -g),
                new(g, 0, -1), new(g, 0, 1), new(-g, 0, -1), new(-g, 0, 1),
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            // 앞면 법선 = cross(b-a, c-a)가 바깥을 향하는 순서 (Unity 앞면)
            tris = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            for (int s = 0; s < subdiv; s++)
            {
                var mid = new Dictionary<long, int>();
                var vs = verts;
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (mid.TryGetValue(key, out int m)) return m;
                    vs.Add(((vs[a] + vs[b]) * 0.5f).normalized);
                    mid[key] = vs.Count - 1;
                    return vs.Count - 1;
                }
                var next = new List<int>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }
        }

        static float Hash(int x, int y, int z)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + z * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float Noise(Vector3 p)
        {
            int xi = Mathf.FloorToInt(p.x), yi = Mathf.FloorToInt(p.y), zi = Mathf.FloorToInt(p.z);
            float fx = p.x - xi, fy = p.y - yi, fz = p.z - zi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy); fz = fz * fz * (3f - 2f * fz);
            float L(float a, float b, float t2) => a + (b - a) * t2;
            return L(L(L(Hash(xi, yi, zi), Hash(xi + 1, yi, zi), fx), L(Hash(xi, yi + 1, zi), Hash(xi + 1, yi + 1, zi), fx), fy),
                     L(L(Hash(xi, yi, zi + 1), Hash(xi + 1, yi, zi + 1), fx), L(Hash(xi, yi + 1, zi + 1), Hash(xi + 1, yi + 1, zi + 1), fx), fy), fz);
        }

        static float Fbm(Vector3 p) => Noise(p) * 0.6f + Noise(p * 2.1f) * 0.28f + Noise(p * 4.3f) * 0.12f;
    }
}
