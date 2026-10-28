using System;
using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 물 위에서 노를 저으며 돌아다니는 카약들 (탄천, 사용자 사진: 축제 날 탄천 카약 체험 — 주황 구명조끼, 알록달록한 앉아 타는 카약).
    /// 카약마다 물길 안의 정해진 구역에서 목표 지점을 골라 천천히 방향을 틀며 간다 (서로 너무 가까우면 비켜 간다).
    /// 노는 좌우로 번갈아 물에 담그며 젓고, 카약은 젓는 쪽으로 살짝 기울고 물결에 오르내린다. 충돌체는 없다 (풍경).
    /// </summary>
    public class KayakFleet : MonoBehaviour
    {
        class Boat
        {
            public Transform T;
            public List<Transform> Paddles = new List<Transform>();
            public Vector2 Pos, Target;
            public float Heading, Speed, Phase, Rate, Wait;
        }

        readonly List<Boat> _boats = new List<Boat>();
        public static KayakFleet Instance { get; private set; }
        /// <summary>자동 촬영: 선착장 위에 서서 물 쪽을 보는 자리 (Tancheon이 정한다).</summary>
        public static Vector3 DockView;
        float _water, _x0, _x1;
        Func<float, float> _zMin, _zMax;
        System.Random _rng;

        /// <summary>카약 count척을 x0~x1, zMin(x)~zMax(x) 물 위에 띄운다.</summary>
        public static KayakFleet Create(Transform parent, int count, float water, float x0, float x1, Func<float, float> zMin, Func<float, float> zMax, int seed)
        {
            var go = new GameObject("Kayaks");
            go.transform.SetParent(parent, false);
            var f = go.AddComponent<KayakFleet>();
            Instance = f;
            f._water = water; f._x0 = x0; f._x1 = x1; f._zMin = zMin; f._zMax = zMax;
            f._rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var hull = KayakKit.Colors[f._rng.Next(KayakKit.Colors.Length)];
                int seats = f._rng.NextDouble() < 0.55 ? 2 : 1;
                var t = KayakKit.Build(go.transform, hull, seats, seats, f._rng, out var paddles);
                var b = new Boat { T = t, Paddles = paddles, Pos = f.RandomSpot(), Heading = (float)f._rng.NextDouble() * 360f };
                b.Target = f.RandomSpot();
                b.Speed = 0.6f + (float)f._rng.NextDouble() * 0.5f;
                b.Phase = (float)f._rng.NextDouble() * 6.28f;
                b.Rate = 2.2f + (float)f._rng.NextDouble() * 0.9f;   // 젓는 박자 (rad/s, 한 번 저을 때 반 바퀴)
                f._boats.Add(b);
            }
            return f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>자동 촬영: 앞의 세 척을 p 둘레로 불러와 heading 쪽으로 젓게 한다.</summary>
        public void ParkForShot(Vector2 p, float heading)
        {
            var fwd = new Vector2(Mathf.Sin(heading * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad));
            for (int i = 0; i < Mathf.Min(3, _boats.Count); i++)
            {
                var b = _boats[i];
                b.Pos = p + new Vector2((i - 1) * 4.5f, (i % 2) * 3f);
                b.Heading = heading + (i - 1) * 12f;
                b.Target = b.Pos + fwd * 60f;
                b.Wait = 0f;
            }
        }

        Vector2 RandomSpot()
        {
            float x = Mathf.Lerp(_x0, _x1, (float)_rng.NextDouble());
            return new Vector2(x, Mathf.Lerp(_zMin(x), _zMax(x), (float)_rng.NextDouble()));
        }

        void Update()
        {
            float dt = Time.deltaTime, now = Time.time;
            foreach (var b in _boats)
            {
                // 목표에 닿으면 잠깐 쉬었다가 (노를 멈추고 떠 있는다) 새 목표로
                var to = b.Target - b.Pos;
                bool resting = b.Wait > 0f;
                if (resting) b.Wait -= dt;
                else if (to.magnitude < 3f)
                {
                    b.Target = RandomSpot();
                    if (_rng.NextDouble() < 0.4) b.Wait = 2f + (float)_rng.NextDouble() * 5f;
                }
                // 이웃과 너무 가까우면 비켜 간다
                var away = Vector2.zero;
                foreach (var o in _boats)
                {
                    if (o == b) continue;
                    var d = b.Pos - o.Pos;
                    float m = d.magnitude;
                    if (m < 6f && m > 0.01f) away += d / m * (6f - m);
                }
                var want = to.normalized + away * 0.35f;
                float wantHeading = Mathf.Atan2(want.x, want.y) * Mathf.Rad2Deg;
                b.Heading = Mathf.MoveTowardsAngle(b.Heading, wantHeading, 14f * dt);
                // 저을 때마다 조금씩 밀린다 (노가 물에 들어간 동안 빨라진다)
                float stroke = resting ? 0f : Mathf.Abs(Mathf.Sin(now * b.Rate + b.Phase));
                float v = resting ? 0.08f : b.Speed * (0.75f + 0.35f * stroke);
                var fwd = new Vector2(Mathf.Sin(b.Heading * Mathf.Deg2Rad), Mathf.Cos(b.Heading * Mathf.Deg2Rad));
                b.Pos += fwd * v * dt;
                b.Pos.x = Mathf.Clamp(b.Pos.x, _x0, _x1);
                b.Pos.y = Mathf.Clamp(b.Pos.y, _zMin(b.Pos.x), _zMax(b.Pos.x));
                float side = Mathf.Sin(now * b.Rate + b.Phase);   // + 오른쪽 날 / - 왼쪽 날이 물에
                float bob = Mathf.Sin(now * 1.3f + b.Phase) * 0.03f;
                b.T.localPosition = new Vector3(b.Pos.x, _water + bob, b.Pos.y);
                b.T.localRotation = Quaternion.Euler(Mathf.Sin(now * 0.9f + b.Phase) * 1.2f, b.Heading, (resting ? 0f : -side * 2.5f) + Mathf.Sin(now * 1.1f + b.Phase * 2f) * 1f);
                // 노: 앞뒤 축으로 굴려 좌우 날을 번갈아 물에 담근다 + 담근 날을 뒤로 당기며 살짝 돈다
                for (int k = 0; k < b.Paddles.Count; k++)
                {
                    float s = resting ? 0f : Mathf.Sin(now * b.Rate + b.Phase + k * 0.4f);
                    float c = resting ? 0f : Mathf.Cos(now * b.Rate + b.Phase + k * 0.4f);
                    b.Paddles[k].localRotation = Quaternion.Euler(0f, c * 22f, s * 32f);
                }
            }
        }
    }

    /// <summary>
    /// 앉아 타는 플라스틱 카약 (사용자 사진: 주황·분홍·민트·노랑·하늘·초록 대리석 무늬, 1~2인승) 절차 메쉬 + 구명조끼 입은 사람 + 양날 노.
    /// 카약은 길이 방향 +z. 선체 옆면은 갑판 가장자리에서 둥글게 용골로, 갑판엔 우묵한 좌석 자리. 정점 색으로 칠한다.
    /// </summary>
    public static class KayakKit
    {
        public static readonly Color[] Colors =
        {
            new Color(1f, 0.42f, 0.12f), new Color(0.98f, 0.55f, 0.62f), new Color(0.45f, 0.86f, 0.68f), new Color(1f, 0.82f, 0.2f),
            new Color(0.52f, 0.74f, 0.98f), new Color(0.55f, 0.88f, 0.3f), new Color(0.95f, 0.28f, 0.25f), new Color(0.78f, 0.62f, 0.92f),
        };

        static Material _mat;
        static readonly Color Vest = new Color(1f, 0.45f, 0.08f), Skin = new Color(0.96f, 0.78f, 0.64f);
        static readonly Color[] Shirts = { new Color(0.2f, 0.22f, 0.28f), new Color(0.9f, 0.9f, 0.88f), new Color(0.3f, 0.45f, 0.75f), new Color(0.55f, 0.2f, 0.25f) };
        static readonly Color[] Hats = { new Color(0.95f, 0.85f, 0.6f), new Color(0.15f, 0.15f, 0.18f), new Color(0.85f, 0.3f, 0.25f), new Color(0.25f, 0.18f, 0.12f) };

        /// <summary>꼭짓점 색 그대로 칠하는 반질한 플라스틱 (Fishing/VertexColor — 풍화 셰이더의 꼭짓점 색은 주황을 갈색으로 바꿨다).</summary>
        public static Material Mat
        {
            get
            {
                if (_mat != null) return _mat;
                var sh = Shader.Find("Fishing/VertexColor");
                _mat = sh != null ? new Material(sh) { name = "KayakPlastic" } : Mats.Lit(Color.white, 0.45f);
                return _mat;
            }
        }

        /// <summary>카약 한 척 (people명 탄 채로, 0이면 빈 카약). seats = 좌석 수 (1~2). paddles = 사람마다 노(굴리는 축).</summary>
        public static Transform Build(Transform parent, Color hull, int seats, int people, System.Random rng, out List<Transform> paddles)
        {
            paddles = new List<Transform>();
            float L = seats == 2 ? 3.8f : 2.9f, Wd = seats == 2 ? 0.86f : 0.76f;
            var root = new GameObject("Kayak").transform;
            root.SetParent(parent, false);
            var deckCol = Color.Lerp(hull, Color.white, 0.1f);
            var well = hull * 0.72f; well.a = 1f;
            // Sin(π)은 -8.7e-8 같은 작은 음수라 Pow가 NaN → 꼭짓점 하나가 NaN이면 경계 상자가 깨져 선체가 통째로 안 그려졌다. 0 아래로 못 가게
            float Bulge(float u) => Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01(u)));
            float Half(float u) => Wd * 0.5f * Mathf.Pow(Bulge(u), 0.55f);
            float Top(float u) => 0.24f + 0.1f * Mathf.Pow(Mathf.Abs(2f * u - 1f), 2.5f);   // 앞뒤 끝이 살짝 들린다
            float Keel(float u) => -0.1f * Mathf.Pow(Bulge(u), 0.5f);
            float Z(float u) => (u - 0.5f) * L;
            // 대리석 무늬: 길이·둘레를 따라 흰 줄이 굽이친다
            Color Marble(float u, float v, Color c)
            {
                float m = Mathf.Sin(u * 23f + Mathf.Sin(v * 9f + u * 7f) * 2.2f);
                return Color.Lerp(c, Color.white, Mathf.Clamp01(m * 1.6f - 1.1f) * 0.45f);
            }
            float[] seatU = seats == 2 ? new[] { 0.36f, 0.66f } : new[] { 0.5f };
            bool InWell(float u, float v)
            {
                foreach (var s in seatU) if (Mathf.Abs(u - s) < 0.085f && Mathf.Abs(v - 0.5f) < 0.3f) return true;
                return false;
            }
            // 닫힌 선체: 길이 방향 고리 Nu+1개, 고리마다 갑판(왼→오른, 가운데 볼록·좌석 자리는 우묵) + 오른 옆면 → 용골 → 왼 옆면 (둥글게)
            const int Nu = 32, Nd = 8, Ns = 6;
            int ring = Nd + 1 + 2 * Ns;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            for (int i = 0; i <= Nu; i++)
            {
                float u = i / (float)Nu, h = Half(u), top = Top(u), keel = Keel(u), z = Z(u);
                for (int j = 0; j <= Nd; j++)
                {
                    float v = j / (float)Nd;
                    float y = top + 0.025f * Mathf.Sin(Mathf.PI * v) - (InWell(u, v) ? 0.09f : 0f);
                    verts.Add(new Vector3(Mathf.Lerp(-h, h, v), y, z));
                    cols.Add(InWell(u, v) ? well : Marble(u, v + 3f, deckCol));
                }
                for (int j = 1; j <= Ns; j++)   // 오른 옆면: 갑판 가장자리 → 용골
                {
                    float a = j / (float)Ns * Mathf.PI * 0.5f;
                    verts.Add(new Vector3(h * Mathf.Cos(a), Mathf.Lerp(top, keel, Mathf.Sin(a)), z));
                    cols.Add(Marble(u, j * 0.2f, hull));
                }
                for (int j = Ns - 1; j >= 0; j--)   // 왼 옆면: 용골 → 갑판 가장자리 (마지막 점은 갑판 왼쪽 끝 바로 아래)
                {
                    float a = j / (float)Ns * Mathf.PI * 0.5f;
                    verts.Add(new Vector3(-h * Mathf.Cos(a), Mathf.Lerp(top, keel, Mathf.Sin(a)) - (j == 0 ? 0.001f : 0f), z));
                    cols.Add(Marble(u, j * 0.2f + 5f, hull));
                }
            }
            var tris = new List<int>();
            for (int i = 0; i < Nu; i++)
            for (int j = 0; j < ring; j++)
            {
                int a = i * ring + j, b = i * ring + (j + 1) % ring, c2 = a + ring, d = b + ring;
                tris.AddRange(new[] { a, c2, b, b, c2, d });
            }
            var mesh = new Mesh { name = "KayakHull" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            // 법선이 안쪽을 보면 감는 방향을 뒤집는다 (가운데 고리의 갑판 꼭짓점이 위를 봐야 한다)
            if (mesh.normals[Nu / 2 * ring + Nd / 2].y < 0f)
            {
                for (int k = 0; k < tris.Count; k += 3) (tris[k + 1], tris[k + 2]) = (tris[k + 2], tris[k + 1]);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
            var hullGo = new GameObject("KayakHull");
            hullGo.transform.SetParent(root, false);
            hullGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            hullGo.AddComponent<MeshRenderer>().sharedMaterial = Mat;

            for (int p = 0; p < Mathf.Min(people, seatU.Length); p++)
            {
                float z = Z(seatU[p]);
                bool kid = seats == 2 && p == 0 && rng.NextDouble() < 0.5;
                float sc = kid ? 0.78f : 1f;
                var body = new BlockMesh();
                var shirt = Shirts[rng.Next(Shirts.Length)];
                float seat = Top(seatU[p]) - 0.06f;
                // 다리 (앞으로 뻗어 갑판 위), 몸통 (주황 구명조끼), 목·머리, 모자/헬멧
                foreach (float sg in new[] { -1f, 1f })
                    body.Block(new Vector3(sg * 0.1f * sc, seat + 0.08f, z + 0.38f * sc), new Vector3(0.13f * sc, 0.13f * sc, 0.72f * sc), 0.04f, Quaternion.Euler(-6f, 0f, 0f), shirt * 0.8f, shirt * 0.8f);
                body.Block(new Vector3(0f, seat + 0.36f * sc, z - 0.02f), new Vector3(0.4f * sc, 0.52f * sc, 0.27f * sc), 0.07f, Quaternion.Euler(-6f, 0f, 0f), Vest, Vest * 0.9f);
                body.Block(new Vector3(0f, seat + 0.66f * sc, z - 0.03f), new Vector3(0.12f * sc, 0.08f * sc, 0.12f * sc), 0.03f, Quaternion.identity, Skin, Skin);
                body.Block(new Vector3(0f, seat + 0.8f * sc, z - 0.03f), new Vector3(0.21f * sc, 0.24f * sc, 0.22f * sc), 0.08f, Quaternion.identity, Skin, Skin);
                var hat = Hats[rng.Next(Hats.Length)];
                body.Block(new Vector3(0f, seat + 0.92f * sc, z - 0.03f), new Vector3(0.24f * sc, 0.08f * sc, 0.25f * sc), 0.04f, Quaternion.identity, hat, hat);
                if (rng.NextDouble() < 0.5)   // 챙
                    body.Block(new Vector3(0f, seat + 0.89f * sc, z + 0.11f * sc), new Vector3(0.22f * sc, 0.02f, 0.12f * sc), 0.01f, Quaternion.identity, hat, hat);
                body.Emit(root, "Paddler", Mat);

                // 노: 가슴 앞 피벗에서 앞뒤 축으로 굴린다. 팔은 노와 같이 움직인다
                var pv = new GameObject("Paddle").transform;
                pv.SetParent(root, false);
                pv.localPosition = new Vector3(0f, seat + 0.42f * sc, z + 0.32f * sc);
                var pm = new BlockMesh();
                var shaft = new Color(0.12f, 0.12f, 0.13f);
                var blade = rng.NextDouble() < 0.5 ? new Color(0.98f, 0.8f, 0.15f) : new Color(0.12f, 0.12f, 0.14f);
                float half = kid ? 0.95f : 1.1f;
                pm.Block(Vector3.zero, new Vector3(half * 2f, 0.035f, 0.035f), 0.01f, Quaternion.identity, shaft, shaft);
                foreach (float sg in new[] { -1f, 1f })
                {
                    pm.Block(new Vector3(sg * (half + 0.18f), 0f, 0f), new Vector3(0.42f, 0.03f, 0.17f), 0.015f, Quaternion.Euler(0f, 0f, sg * 8f), blade, blade);
                    // 팔: 어깨에서 노 손잡이로
                    var sh = new Vector3(sg * 0.2f * sc, 0.17f * sc, -0.32f * sc);
                    var hand = new Vector3(sg * 0.3f * sc, 0f, 0f);
                    var d = hand - sh;
                    pm.Block((sh + hand) * 0.5f, new Vector3(0.09f * sc, 0.09f * sc, d.magnitude + 0.04f), 0.03f, Quaternion.LookRotation(d.normalized), Vest, Vest);
                    pm.Block(hand, new Vector3(0.08f * sc, 0.08f * sc, 0.08f * sc), 0.03f, Quaternion.identity, Skin, Skin);
                }
                pm.Emit(pv, "PaddleMesh", Mat);
                paddles.Add(pv);
            }
            return root;
        }
    }
}
