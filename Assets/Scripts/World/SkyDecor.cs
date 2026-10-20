using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 절차적 스카이박스 위에 얹는 하늘 장식: 구름층, 달, 별.
    /// 카메라를 따라다니고, DayNightCycle이 매 프레임 색과 양을 정해 준다.
    /// </summary>
    public class SkyDecor : MonoBehaviour
    {
        const float SkyRadius = 900f;

        Transform _moon, _sun, _stars, _clouds;
        Material _moonMat, _sunMat, _starMat, _cloudMat;
        bool _built;

        void Build()
        {
            var shader = Shader.Find("Fishing/Sky");
            if (shader == null) { enabled = false; return; }

            _moonMat = new Material(shader) { mainTexture = ProceduralTextures.Moon };
            _moonMat.SetFloat("_Mode", 0f);
            _moon = Quad("Moon", _moonMat, 70f);

            // 해: 스카이박스의 작은 해만으로는 잘 안 보여서 빛이 번지는 원판을 하나 더 (구름보다 먼저 그려 구름에 가린다)
            _sunMat = new Material(shader) { mainTexture = SunGlow() };
            _sunMat.SetFloat("_Mode", 0f);
            _sun = Quad("Sun", _sunMat, 150f);

            _starMat = new Material(shader) { mainTexture = ProceduralTextures.Star };
            _starMat.SetFloat("_Mode", 2f);
            _stars = new GameObject("Stars").transform;
            _stars.SetParent(transform, false);
            _stars.gameObject.AddComponent<MeshFilter>().sharedMesh = StarMesh(2200);
            var sr = _stars.gameObject.AddComponent<MeshRenderer>();
            sr.sharedMaterial = _starMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _cloudMat = new Material(shader) { mainTexture = ProceduralTextures.CloudNoise };
            _cloudMat.SetFloat("_Mode", 1f);
            _cloudMat.renderQueue = 2990;
            _clouds = new GameObject("Clouds").transform;
            _clouds.SetParent(transform, false);
            _clouds.gameObject.AddComponent<MeshFilter>().sharedMesh = CloudDome(56, 18, SkyRadius * 0.92f);
            var cr = _clouds.gameObject.AddComponent<MeshRenderer>();
            cr.sharedMaterial = _cloudMat;
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _built = true;
        }

        Transform Quad(string name, Material mat, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * size;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>해 원판 + 번지는 빛 (가운데 하얗고 둘레로 옅어지는 알파).</summary>
        static Texture2D SunGlow()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "SunGlow", wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float r = new Vector2(x - N * 0.5f + 0.5f, y - N * 0.5f + 0.5f).magnitude / (N * 0.5f);
                // Mathf.SmoothStep(a, b, t)는 a~b 사이 값을 돌려준다(셰이더 smoothstep과 다르다) → 0~1로 바꿔 쓴다
                float disk = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.19f, r)); // 해 원판
                float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 3f) * 0.55f;   // 둘레 빛
                px[y * N + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(disk + halo));
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>
        /// 카메라를 감싸는 반구. 구름 UV는 머리 위 평면에 투영한 좌표라서 지평선 쪽은 구름이 납작하게 모여 보인다.
        /// 버텍스 알파로 지평선 바로 위에서 서서히 사라진다.
        /// </summary>
        static Mesh CloudDome(int seg, int rings, float radius)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var colors = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float elev = Mathf.Lerp(-0.02f, Mathf.PI * 0.5f, r / (float)rings);
                for (int s = 0; s <= seg; s++)
                {
                    float az = s / (float)seg * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(elev) * Mathf.Cos(az), Mathf.Sin(elev), Mathf.Cos(elev) * Mathf.Sin(az));
                    verts.Add(d * radius);
                    var p = new Vector2(d.x, d.z) / (d.y + 0.22f);
                    uvs.Add(p * 0.12f + new Vector2(0.5f, 0.5f));
                    colors.Add(new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d.y / 0.18f))));
                }
            }
            int w = seg + 1;
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < seg; s++)
            {
                int i = r * w + s;
                tris.Add(i); tris.Add(i + w); tris.Add(i + 1);
                tris.Add(i + 1); tris.Add(i + w); tris.Add(i + w + 1);
            }
            var mesh = new Mesh { name = "CloudDome" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            return mesh;
        }

        /// <summary>
        /// 별마다 하늘 중심(카메라)을 바라보는 작은 사각형. 예전에는 점(Points)이라 1픽셀로만 찍혀 거의 안 보였다.
        /// 반지름 900m에서 1픽셀 ≈ 1.2m라 크기 3.2~10m(번짐 포함, 대부분 작고 몇 개만 크다). 색은 푸른 별·흰 별·노란 별을 섞고,
        /// uv2.x에 반짝임 위상을 넣어 셰이더(_Mode 2)가 별마다 다른 박자로 깜빡이게 한다. 은하수 띠 쪽에 더 촘촘하다.
        /// </summary>
        static Mesh StarMesh(int count)
        {
            var rng = new System.Random(4242);
            var verts = new Vector3[count * 4];
            var colors = new Color[count * 4];
            var uvs = new Vector2[count * 4];
            var uv2 = new Vector2[count * 4];
            var tris = new int[count * 6];
            // 은하수: 하늘을 비스듬히 가로지르는 큰 원 둘레
            var bandNormal = new Vector3(0.35f, 0.55f, 0.76f).normalized;
            for (int i = 0; i < count; i++)
            {
                // 위쪽 반구에만, 절반쯤은 은하수 띠 근처에 몰리게
                Vector3 d;
                bool band = i % 2 == 0;
                do d = new Vector3((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble(), (float)rng.NextDouble() * 2 - 1);
                while (d.sqrMagnitude > 1f || d.sqrMagnitude < 0.01f || d.normalized.y < 0.04f
                       || (band && Mathf.Abs(Vector3.Dot(d.normalized, bandNormal)) > 0.16f));
                d.Normalize();
                var c = d * SkyRadius;
                var right = Vector3.Cross(Vector3.up, d);
                right = right.sqrMagnitude < 1e-4f ? Vector3.right : right.normalized;
                var up = Vector3.Cross(d, right);

                float t = (float)rng.NextDouble();
                float size = Mathf.Lerp(3.2f, 10f, t * t * t);    // 큰 별은 드물다
                float b = Mathf.Lerp(0.45f, 1f, Mathf.Sqrt(t)) * (band ? 0.85f : 1f);
                float hue = (float)rng.NextDouble();
                var tint = hue < 0.22f ? new Color(0.75f, 0.85f, 1f) : hue > 0.86f ? new Color(1f, 0.88f, 0.68f) : Color.white;
                var col = new Color(tint.r, tint.g, tint.b, b);
                float phase = (float)rng.NextDouble();

                int v = i * 4;
                verts[v + 0] = c + (-right - up) * size * 0.5f;
                verts[v + 1] = c + (right - up) * size * 0.5f;
                verts[v + 2] = c + (right + up) * size * 0.5f;
                verts[v + 3] = c + (-right + up) * size * 0.5f;
                uvs[v + 0] = new Vector2(0, 0); uvs[v + 1] = new Vector2(1, 0);
                uvs[v + 2] = new Vector2(1, 1); uvs[v + 3] = new Vector2(0, 1);
                for (int k = 0; k < 4; k++) { colors[v + k] = col; uv2[v + k] = new Vector2(phase, 0f); }
                int ti = i * 6;
                tris[ti + 0] = v; tris[ti + 1] = v + 2; tris[ti + 2] = v + 1;
                tris[ti + 3] = v; tris[ti + 4] = v + 3; tris[ti + 5] = v + 2;
            }
            var mesh = new Mesh { name = "Stars" };
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.uv2 = uv2;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * SkyRadius * 2.2f);
            return mesh;
        }

        /// <param name="toSun">태양 쪽 방향(정규화)</param>
        /// <param name="night">0 = 낮, 1 = 한밤</param>
        /// <param name="cloudColor">구름 색 (노을이면 주황)</param>
        /// <param name="coverage">구름 양 0~1</param>
        /// <param name="warm">노을 정도 0~1 (해가 주황으로)</param>
        public void Apply(Camera cam, Vector3 toSun, float night, Color cloudColor, float coverage, float warm = 0f)
        {
            if (!_built) Build();
            if (!_built || cam == null) return;
            var camPos = cam.transform.position;
            transform.position = camPos;

            var toMoon = -toSun;
            _moon.position = camPos + toMoon * SkyRadius;
            _moon.rotation = Quaternion.LookRotation(toMoon);
            _moonMat.color = new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(night * 1.2f) * Mathf.Clamp01(toMoon.y * 6f + 0.3f));
            _moon.gameObject.SetActive(toMoon.y > -0.05f);

            _sun.position = camPos + toSun * SkyRadius;
            _sun.rotation = Quaternion.LookRotation(toSun);
            var sunCol = Color.Lerp(new Color(1f, 0.97f, 0.88f), new Color(1f, 0.58f, 0.3f), warm);
            sunCol.a = Mathf.Clamp01(toSun.y * 8f + 0.4f) * (1f - Mathf.Clamp01((coverage - 0.5f) * 2.2f)); // 흐리면 가린다
            _sunMat.color = sunCol;
            _sun.gameObject.SetActive(toSun.y > -0.06f && sunCol.a > 0.01f);

            _starMat.color = new Color(2.2f, 2.2f, 2.2f, Mathf.Clamp01(night * night * 1.3f) * (1f - Mathf.Max(0f, coverage - 0.4f) * 1.6f));
            _stars.gameObject.SetActive(night > 0.05f);
            _stars.rotation = Quaternion.Euler(0f, Time.time * 0.05f, 0f); // 별자리가 아주 천천히 돈다

            _clouds.position = camPos;
            _cloudMat.color = cloudColor;
            _cloudMat.SetFloat("_Coverage", coverage);
        }
    }
}
