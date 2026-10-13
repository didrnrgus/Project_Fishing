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
