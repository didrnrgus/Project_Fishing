using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 등대 모델. 예전에는 원통 프리미티브 몇 개를 쌓아 "도형 같다"는 지적을 받아, 회전체(lathe) 메쉬로 다시 만든다:
    /// 위로 갈수록 가늘어지는 탑, 꼭대기 받침(코벨), 난간 기둥·난간대가 있는 전망대, 유리 등롱과 창살, 돔 지붕·환기구 공·피뢰침,
    /// 아래 철문과 작은 창. 몸통은 콘크리트 텍스처(Fishing/Ground, 얼룩)에 칠한 색이라 플라스틱 같지 않다.
    /// 등불(밤에 켜지는 전구 렌더러)과 불빛 높이는 예전과 같다.
    /// </summary>
    public static class LighthouseBuilder
    {
        public const float SmallLampY = 8.5f, GrandLampY = 16.4f;

        /// <summary>방파제 등대(약 9.7m): body = 몸통 색(빨강·흰색). 등불 렌더러를 돌려준다.</summary>
        public static Renderer Small(Transform lh, Color body)
        {
            bool whiteBody = body.r > 0.9f && body.g > 0.9f;
            var paint = Painted(body);
            var trim = whiteBody ? Painted(new Color(0.2f, 0.45f, 0.35f)) : Painted(new Color(0.95f, 0.95f, 0.93f));
            var dark = Mats.Lit(new Color(0.16f, 0.18f, 0.2f), 0.45f, 0.5f);
            var concrete = Mats.Textured("concrete", new Color(0.8f, 0.79f, 0.76f), new Vector2(3f, 1f), 0.15f);

            // 팔각 기단 + 탑 (0.9 → 0.62, 위 받침이 살짝 벌어진다)
            Part(lh, "Plinth", Lathe(8, (1.45f, 0f), (1.45f, 0.55f), (1.45f, 0.55f), (0.9f, 0.55f)), concrete);
            Part(lh, "Tower", Lathe(28, (0.9f, 0.5f), (0.76f, 4f), (0.62f, 7.5f), (0.62f, 7.5f), (0.84f, 7.78f), (0.84f, 7.78f), (0.84f, 7.86f)), paint);
            // 띠: 탑 둘레보다 조금 두껍게
            Part(lh, "Band", Lathe(28, (R(5.1f) + 0.012f, 5.1f), (R(5.6f) + 0.012f, 5.6f)), trim);
            Gallery(lh, 7.86f, 1.15f, 12, 0.85f, dark);
            Lantern(lh, 7.92f, 0.58f, 0.9f, 8, paint, dark);
            Part(lh, "Roof", Lathe(24, (0.7f, 8.95f), (0.7f, 9.03f), (0.7f, 9.03f), (0.52f, 9.25f), (0.28f, 9.45f), (0f, 9.53f)), paint);
            Mats.Prim(PrimitiveType.Sphere, "VentBall", lh, new Vector3(0f, 9.62f, 0f), Vector3.one * 0.2f, dark, false);
            Mats.Prim(PrimitiveType.Cylinder, "Rod", lh, new Vector3(0f, 9.95f, 0f), new Vector3(0.025f, 0.3f, 0.025f), dark, false);
            // 철문 · 작은 창 두 개 (반대쪽으로 엇갈려)
            Door(lh, R(1.2f), 1.25f, 0.68f, 1.5f, 0f);
            Window(lh, R(3.6f), 3.6f, 0.26f, 0.4f, 160f);
            Window(lh, R(6.2f), 6.2f, 0.24f, 0.36f, 20f);
            var lamp = Mats.Prim(PrimitiveType.Sphere, "Lamp", lh, new Vector3(0f, SmallLampY, 0f), Vector3.one * 0.62f, Mats.Lit(new Color(0.95f, 0.95f, 0.93f), 0.5f), false);
            var col = lh.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 4.8f, 0f);
            col.radius = 1f;
            col.height = 9.6f;
            return lamp.GetComponent<Renderer>();

            float R(float y) => Mathf.Lerp(0.9f, 0.62f, Mathf.InverseLerp(0.5f, 7.5f, y));
        }

        /// <summary>큰 유인 등대(약 18m, 거문도·동해 등대섬): 흰 팔각 탑, 붉은 등롱·돔, 옆에 박공지붕 등대지기 숙소.</summary>
        public static Renderer Grand(Transform lh)
        {
            var white = Painted(new Color(0.97f, 0.97f, 0.95f));
            var red = Painted(new Color(0.78f, 0.15f, 0.13f));
            var dark = Mats.Lit(new Color(0.16f, 0.18f, 0.2f), 0.45f, 0.5f);
            var concrete = Mats.Textured("concrete", new Color(0.82f, 0.81f, 0.77f), new Vector2(6f, 1f), 0.15f);

            Part(lh, "Base", Lathe(8, (3.3f, 0f), (3.3f, 1f), (3.3f, 1f), (2.05f, 1f)), concrete);
            Part(lh, "Tower", Lathe(8, (2.05f, 1f), (1.75f, 8f), (1.45f, 14.8f), (1.45f, 14.8f), (2.1f, 15.12f), (2.1f, 15.12f), (2.1f, 15.2f)), white);
            Gallery(lh, 15.2f, 2.35f, 20, 0.95f, dark);
            Lantern(lh, 15.28f, 1.15f, 1.7f, 10, red, dark);
            Part(lh, "Dome", Lathe(24, (1.3f, 17.0f), (1.3f, 17.12f), (1.3f, 17.12f), (0.95f, 17.5f), (0.5f, 17.82f), (0f, 17.92f)), red);
            Mats.Prim(PrimitiveType.Sphere, "VentBall", lh, new Vector3(0f, 18.05f, 0f), Vector3.one * 0.32f, dark, false);
            Mats.Prim(PrimitiveType.Cylinder, "Vane", lh, new Vector3(0f, 18.6f, 0f), new Vector3(0.05f, 0.5f, 0.05f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "VaneArrow", lh, new Vector3(0f, 18.85f, 0.15f), new Vector3(0.04f, 0.12f, 0.6f), dark, false);
            float R(float y) => Mathf.Lerp(2.05f, 1.45f, Mathf.InverseLerp(1f, 14.8f, y)) * 0.93f; // 팔각 면 가운데(꼭짓점보다 안쪽)
            Door(lh, R(2f), 2.2f, 1.1f, 2.2f, 0f);
            for (int i = 0; i < 3; i++) Window(lh, R(4.5f + i * 3.4f), 4.5f + i * 3.4f, 0.5f, 0.75f, i % 2 == 0 ? 180f : 0f);

            // 등대지기 숙소: 흰 벽, 붉은 박공지붕, 문·창, 굴뚝
            var keeper = new GameObject("Keeper").transform;
            keeper.SetParent(lh, false);
            keeper.localPosition = new Vector3(7f, 0f, 0f);
            Mats.Prim(PrimitiveType.Cube, "Wall", keeper, new Vector3(0f, 1.6f, 0f), new Vector3(7f, 3.2f, 5f), white);
            Mats.Prim(PrimitiveType.Cube, "Skirt", keeper, new Vector3(0f, 0.25f, 0f), new Vector3(7.1f, 0.5f, 5.1f), concrete, false);
            var roof = new GameObject("Roof");
            roof.transform.SetParent(keeper, false);
            roof.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            roof.transform.localScale = new Vector3(7.7f, 1.6f, 5.7f); // 용마루가 긴 변(x)을 따라
            roof.AddComponent<MeshFilter>().sharedMesh = CoastProps.GableMesh();
            roof.AddComponent<MeshRenderer>().sharedMaterial = red;
            Mats.Prim(PrimitiveType.Cube, "Chimney", keeper, new Vector3(2f, 4.3f, 0.8f), new Vector3(0.5f, 1.4f, 0.5f), concrete, false);
            var glass = Mats.Lit(new Color(0.14f, 0.2f, 0.26f), 0.85f);
            for (int i = 0; i < 3; i++)
            {
                float x = -2.2f + i * 2.2f;
                Mats.Prim(PrimitiveType.Cube, "WinFrame", keeper, new Vector3(x, 1.8f, 2.52f), new Vector3(1.1f, 1.0f, 0.04f), dark, false);
                Mats.Prim(PrimitiveType.Cube, i == 1 ? "Door" : "Window", keeper, i == 1 ? new Vector3(x, 1.15f, 2.55f) : new Vector3(x, 1.8f, 2.55f),
                    i == 1 ? new Vector3(0.95f, 2.0f, 0.04f) : new Vector3(0.95f, 0.85f, 0.04f), i == 1 ? red : glass, false);
            }
            var col = lh.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 9f, 0f);
            col.radius = 2.2f;
            col.height = 18f;
            var kc = keeper.gameObject.AddComponent<BoxCollider>();
            kc.center = new Vector3(0f, 2f, 0f);
            kc.size = new Vector3(7f, 4f, 5f);
            var lamp = Mats.Prim(PrimitiveType.Sphere, "Lamp", lh, new Vector3(0f, GrandLampY, 0f), Vector3.one * 1.0f, Mats.Lit(new Color(0.96f, 0.96f, 0.94f), 0.45f), false);
            return lamp.GetComponent<Renderer>();
        }

        /// <summary>칠한 콘크리트: 텍스처 위에 색을 곱한다 (텍스처가 회색이라 조금 밝게 보정).</summary>
        static Material Painted(Color c) =>
            Mats.Textured("concrete", CoastProps.PaintTint(c), new Vector2(4f, 3f), 0.35f);

        /// <summary>전망대: 바닥판 + 난간 기둥 + 위·가운데 난간대.</summary>
        static void Gallery(Transform lh, float y, float r, int posts, float h, Material mat)
        {
            Part(lh, "GalleryDeck", Lathe(32, (r, y - 0.06f), (r, y + 0.08f), (r, y + 0.08f), (0f, y + 0.08f)), mat);
            for (int i = 0; i < posts; i++)
            {
                float a = i * Mathf.PI * 2f / posts;
                Mats.Prim(PrimitiveType.Cylinder, "Post", lh, new Vector3(Mathf.Cos(a) * (r - 0.05f), y + 0.08f + h * 0.5f, Mathf.Sin(a) * (r - 0.05f)),
                    new Vector3(0.04f, h * 0.5f, 0.04f), mat, false);
            }
            foreach (float ry in new[] { y + 0.08f + h, y + 0.08f + h * 0.5f })
                Part(lh, "Rail", Lathe(32, (r - 0.08f, ry - 0.025f), (r - 0.02f, ry - 0.025f), (r - 0.02f, ry + 0.025f), (r - 0.08f, ry + 0.025f), (r - 0.08f, ry - 0.025f)), mat);
        }

        /// <summary>등롱: 낮은 벽 + 유리 원통 + 세로 창살 + 위아래 테.</summary>
        static void Lantern(Transform lh, float y, float r, float h, int bars, Material wall, Material frame)
        {
            float wallH = h * 0.25f;
            Part(lh, "LanternWall", Lathe(24, (r, y), (r, y + wallH), (r, y + wallH), (r - 0.06f, y + wallH)), wall);
            var glass = Mats.Transparent(new Color(0.55f, 0.7f, 0.78f, 0.35f), 0.95f);
            Part(lh, "Glass", Lathe(24, (r - 0.05f, y + wallH), (r - 0.05f, y + h)), glass);
            for (int i = 0; i < bars; i++)
            {
                float a = i * Mathf.PI * 2f / bars;
                Mats.Prim(PrimitiveType.Cube, "Bar", lh, new Vector3(Mathf.Cos(a) * (r - 0.04f), y + wallH + (h - wallH) * 0.5f, Mathf.Sin(a) * (r - 0.04f)),
                    new Vector3(0.05f, h - wallH, 0.05f), frame, false, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
            }
            Part(lh, "LanternRing", Lathe(24, (r - 0.02f, y + h - 0.04f), (r + 0.03f, y + h - 0.04f), (r + 0.03f, y + h + 0.03f), (r - 0.02f, y + h + 0.03f)), frame);
        }

        /// <summary>탑 앞(+Z) 철문: 문틀 + 문짝 + 손잡이 + 앞 계단 한 칸.</summary>
        static void Door(Transform lh, float r, float y, float w, float h, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var frame = Mats.Lit(new Color(0.85f, 0.85f, 0.82f), 0.3f);
            var door = Mats.Lit(new Color(0.22f, 0.32f, 0.42f), 0.45f, 0.4f);
            Mats.Prim(PrimitiveType.Cube, "DoorFrame", lh, rot * new Vector3(0f, y, r - 0.02f), new Vector3(w + 0.14f, h + 0.1f, 0.1f), frame, false, rot);
            Mats.Prim(PrimitiveType.Cube, "Door", lh, rot * new Vector3(0f, y - 0.02f, r + 0.02f), new Vector3(w, h, 0.06f), door, false, rot);
            Mats.Prim(PrimitiveType.Sphere, "DoorKnob", lh, rot * new Vector3(w * 0.32f, y - 0.05f, r + 0.07f), Vector3.one * 0.06f, frame, false);
            Mats.Prim(PrimitiveType.Cube, "Step", lh, rot * new Vector3(0f, y - h * 0.5f - 0.08f, r + 0.25f), new Vector3(w + 0.3f, 0.16f, 0.45f), frame, false, rot);
        }

        /// <summary>탑 둘레의 작은 창 (yaw 방향).</summary>
        static void Window(Transform lh, float r, float y, float w, float h, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Mats.Prim(PrimitiveType.Cube, "WinFrame", lh, rot * new Vector3(0f, y, r - 0.01f), new Vector3(w + 0.1f, h + 0.1f, 0.08f),
                Mats.Lit(new Color(0.9f, 0.9f, 0.88f), 0.3f), false, rot);
            Mats.Prim(PrimitiveType.Cube, "Window", lh, rot * new Vector3(0f, y, r + 0.025f), new Vector3(w, h, 0.04f), Mats.Lit(new Color(0.12f, 0.17f, 0.22f), 0.85f), false, rot);
        }

        static void Part(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        /// <summary>
        /// 회전체 메쉬: 옆선 (반지름, 높이) 점들을 Y축으로 돌린다. 같은 점을 두 번 넣으면 그 자리에서 모서리가 각진다(법선을 나눔).
        /// 옆선을 아래·바깥에서 위·안쪽으로 그리면 바깥면이 앞면이다. UV: u = 둘레(m)/2, v = 높이(m)/2.
        /// </summary>
        static Mesh Lathe(int seg, params (float r, float y)[] prof)
        {
            var key = seg + ":" + string.Join(";", System.Array.ConvertAll(prof, p => $"{p.r:0.###},{p.y:0.###}"));
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            // 같은 점이 연달아 나오면 그 사이는 면을 만들지 않는다 (모서리 나눔용)
            for (int i = 0; i < prof.Length; i++)
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * prof[i].r, prof[i].y, Mathf.Sin(a) * prof[i].r));
                    uv.Add(new Vector2(s / (float)seg * Mathf.Max(0.5f, prof[i].r * Mathf.PI), prof[i].y * 0.5f + prof[i].r * 0.2f));
                }
            for (int i = 0; i + 1 < prof.Length; i++)
            {
                if (Mathf.Approximately(prof[i].r, prof[i + 1].r) && Mathf.Approximately(prof[i].y, prof[i + 1].y)) continue;
                for (int s = 0; s < seg; s++)
                {
                    int a0 = i * (seg + 1) + s, a1 = a0 + 1, b0 = a0 + seg + 1, b1 = b0 + 1;
                    tri.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
            }
            var m = new Mesh { name = "Lathe" };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            // 둘레 이음매(첫·마지막 정점이 같은 자리)의 법선을 평균해 세로 줄이 안 보이게
            var n = m.normals;
            for (int i = 0; i < prof.Length; i++)
            {
                int s0 = i * (seg + 1), s1 = s0 + seg;
                var avg = (n[s0] + n[s1]).normalized;
                n[s0] = n[s1] = avg;
            }
            m.normals = n;
            m.RecalculateTangents();
            m.RecalculateBounds();
            Cache[key] = m;
            return m;
        }
    }
}
