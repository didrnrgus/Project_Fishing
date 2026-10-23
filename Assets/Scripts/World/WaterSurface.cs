using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>잔물결이 일렁이는 수면. 버텍스 파도를 위해 촘촘한 격자 메쉬를 만든다.</summary>
    public static class WaterSurface
    {
        public static Material Material { get; private set; }
        /// <summary>지금 맵의 수면 (물때가 이걸 오르내린다).</summary>
        public static Transform Current { get; private set; }
        static Color? _defaultDeep, _defaultShallow;

        /// <summary>맵마다 물 색 (서해는 탁한 황록색). a가 0이면 기본색.</summary>
        public static void SetColors(Color deep, Color shallow)
        {
            if (Material == null) return;
            _defaultDeep ??= Material.GetColor("_DeepColor");
            _defaultShallow ??= Material.GetColor("_ShallowColor");
            Material.SetColor("_DeepColor", deep.a > 0f ? deep : _defaultDeep.Value);
            Material.SetColor("_ShallowColor", shallow.a > 0f ? shallow : _defaultShallow.Value);
        }

        static Texture2D _seabed;
        static float[] _seabedPx;
        static Rect _seabedArea;
        static int _seabedW, _seabedH;

        /// <summary>
        /// 물가 거품용 해저 높이 텍스처를 맵에서 굽는다 (1m 간격, 최대 512칸). 물 셰이더가 수심·경사로 물가 거품을 그린다.
        /// MapLoader가 월드를 지은 뒤 부른다 (지형 높이 함수가 준비된 뒤).
        /// </summary>
        public static void SetSeabed(FieldMapDef map)
        {
            if (Material == null) return;
            var area = map.SeabedArea;
            int w = Mathf.Clamp(Mathf.CeilToInt(area.width), 16, 512), h = Mathf.Clamp(Mathf.CeilToInt(area.height), 16, 512);
            if (_seabed == null || _seabed.width != w || _seabed.height != h)
            {
                if (_seabed != null) Object.Destroy(_seabed);
                _seabed = new Texture2D(w, h, TextureFormat.RFloat, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Seabed" };
            }
            var px = new float[w * h];
            for (int z = 0; z < h; z++)
            {
                float wz = area.yMin + (z + 0.5f) / h * area.height;
                for (int x = 0; x < w; x++)
                    px[z * w + x] = map.SeabedAt(area.xMin + (x + 0.5f) / w * area.width, wz);
            }
            _seabedPx = px;
            _seabedArea = area;
            _seabedW = w;
            _seabedH = h;
            _seabed.SetPixelData(px, 0);
            _seabed.Apply(false, false);
            Material.SetTexture("_SeabedTex", _seabed);
            Material.SetVector("_SeabedRect", new Vector4(area.xMin, area.yMin, 1f / area.width, 1f / area.height));
            Material.SetVector("_SeabedStep", new Vector4(1f / w, 1f / h, area.width / w, area.height / h));
            Material.SetFloat("_HasSeabed", 1f);
        }

        /// <summary>구워 둔 해저 높이 (물 셰이더가 읽는 것과 같은 값, 겹선형). 구운 범위 밖이면 false.</summary>
        public static bool SeabedHeight(float x, float z, out float height)
        {
            height = 0f;
            if (_seabedPx == null) return false;
            float u = (x - _seabedArea.xMin) / _seabedArea.width * _seabedW - 0.5f;
            float v = (z - _seabedArea.yMin) / _seabedArea.height * _seabedH - 0.5f;
            if (u < 0f || v < 0f || u > _seabedW - 1 || v > _seabedH - 1) return false;
            int x0 = Mathf.Min((int)u, _seabedW - 2), z0 = Mathf.Min((int)v, _seabedH - 2);
            float fx = u - x0, fz = v - z0;
            int i = z0 * _seabedW + x0;
            height = Mathf.Lerp(Mathf.Lerp(_seabedPx[i], _seabedPx[i + 1], fx), Mathf.Lerp(_seabedPx[i + _seabedW], _seabedPx[i + _seabedW + 1], fx), fz);
            return true;
        }

        /// <summary>
        /// 물 셰이더의 물가 거품과 같은 셈: 수심(음수면 물 밖)과, 해저 경사로 어림한 물가까지 수평 거리(m).
        /// 구운 범위 밖이면 false.
        /// </summary>
        public static bool ShoreDistance(float x, float z, out float depth, out float dist)
        {
            depth = dist = 0f;
            if (!SeabedHeight(x, z, out float bed)) return false;
            float sx = _seabedArea.width / _seabedW, sz = _seabedArea.height / _seabedH;
            SeabedHeight(x + sx, z, out float e); SeabedHeight(x - sx, z, out float w);
            SeabedHeight(x, z + sz, out float n); SeabedHeight(x, z - sz, out float s);
            float slope = Mathf.Max(new Vector2((e - w) / (2f * sx), (n - s) / (2f * sz)).magnitude, 0.03f);
            depth = FieldMap.WaterY - bed;
            dist = Mathf.Max(depth, 0f) / slope;
            return true;
        }

        public static GameObject Create(Transform parent, Vector3 center, Vector2 size, float cell)
        {
            int nx = Mathf.Max(2, Mathf.CeilToInt(size.x / cell)) + 1;
            int nz = Mathf.Max(2, Mathf.CeilToInt(size.y / cell)) + 1;
            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                float fx = x / (float)(nx - 1), fz = z / (float)(nz - 1);
                verts[z * nx + x] = new Vector3((fx - 0.5f) * size.x, 0f, (fz - 0.5f) * size.y);
                uvs[z * nx + x] = new Vector2(fx, fz);
            }
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int z = 0; z < nz - 1; z++)
            for (int x = 0; x < nx - 1; x++)
            {
                int i = z * nx + x;
                tris[t++] = i; tris[t++] = i + nx; tris[t++] = i + 1;
                tris[t++] = i + 1; tris[t++] = i + nx; tris[t++] = i + nx + 1;
            }
            var mesh = new Mesh { name = "Water" };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(size.x, 2f, size.y));

            var go = new GameObject("Water");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();

            var shader = Shader.Find("Fishing/Water");
            if (shader != null)
            {
                Material ??= new Material(shader);
                Material.SetTexture("_NormalTex", ProceduralTextures.WaterNormal);
                r.sharedMaterial = Material;
            }
            else r.sharedMaterial = Mats.Lit(new Color(0.13f, 0.3f, 0.38f), 0.93f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Current = go.transform;
            return go;
        }
    }
}
