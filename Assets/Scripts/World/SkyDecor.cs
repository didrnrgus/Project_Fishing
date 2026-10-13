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

        Transform _moon, _stars, _clouds;
        Material _moonMat, _starMat, _cloudMat;
        bool _built;

        void Build()
        {
            var shader = Shader.Find("Fishing/Sky");
            if (shader == null) { enabled = false; return; }

            _moonMat = new Material(shader) { mainTexture = ProceduralTextures.Moon };
            _moonMat.SetFloat("_Mode", 0f);
            _moon = Quad("Moon", _moonMat, 70f);

            _starMat = new Material(shader);
            _starMat.SetFloat("_Mode", 0f);
            _stars = new GameObject("Stars").transform;
            _stars.SetParent(transform, false);
            _stars.gameObject.AddComponent<MeshFilter>().sharedMesh = StarMesh(1400);
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

        static Mesh StarMesh(int count)
        {
            var rng = new System.Random(4242);
            var verts = new Vector3[count];
            var colors = new Color[count];
            var idx = new int[count];
            for (int i = 0; i < count; i++)
            {
                // 위쪽 반구에만
                Vector3 d;
                do d = new Vector3((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble(), (float)rng.NextDouble() * 2 - 1);
                while (d.sqrMagnitude > 1f || d.y < 0.05f);
                verts[i] = d.normalized * SkyRadius;
                float b = 0.4f + (float)rng.NextDouble() * 0.6f;
                colors[i] = new Color(b, b, Mathf.Min(1f, b * 1.1f), 1f);
                idx[i] = i;
            }
            var mesh = new Mesh { name = "Stars" };
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.SetIndices(idx, MeshTopology.Points, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * SkyRadius * 2.2f);
            return mesh;
        }

        /// <param name="toSun">태양 쪽 방향(정규화)</param>
        /// <param name="night">0 = 낮, 1 = 한밤</param>
        /// <param name="cloudColor">구름 색 (노을이면 주황)</param>
        /// <param name="coverage">구름 양 0~1</param>
        public void Apply(Camera cam, Vector3 toSun, float night, Color cloudColor, float coverage)
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

            _starMat.color = new Color(1f, 1f, 1f, Mathf.Clamp01(night * night * 1.1f) * (1f - coverage * 0.8f));
            _stars.gameObject.SetActive(night > 0.05f);
            _stars.rotation = Quaternion.Euler(0f, Time.time * 0.15f, 0f);

            _clouds.position = camPos;
            _cloudMat.color = cloudColor;
            _cloudMat.SetFloat("_Coverage", coverage);
        }
    }
}
