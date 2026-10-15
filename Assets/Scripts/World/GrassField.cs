using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 풀·들꽃 포기 (Grass Flowers Pack Free 그림 + <c>Fishing/Grass</c> 셰이더). 맵을 지은 뒤 한 번 심는다.
    /// - 어디에: 풀 텍스처를 쓰는 바닥만. Unity Terrain이면 풀 층 비율만큼, 메쉬(한강 둔치 잔디판 등)면 재질 텍스처가 grass인 곳.
    ///   위에서 레이캐스트해 처음 맞은 것이 그 바닥일 때만 심으므로 길·건물·데크 위에는 안 난다. 물가(수면 + 0.2m 아래)와 가파른 곳도 뺀다.
    /// - 모양: 포기마다 교차한 판 두 장(무작위 방향·크기·색), 들꽃은 드문드문.
    /// - 그리기: 48m 칸마다 메쉬 하나(그림마다 서브메쉬)로 월드 좌표에 굽고, 카메라에서 멀면(130m) 칸을 끈다(GrassCuller).
    /// </summary>
    public static class GrassField
    {
        const float Spacing = 1.15f, Cell = 48f, MaxClumps = 42000;

        public static void Plant(Transform world, float waterY, int seed)
        {
            var lib = VegetationLibrary.Lib;
            if (world == null || lib == null || lib.grass == null || lib.grass.Length == 0) return;
            Physics.SyncTransforms();
            var rng = new System.Random(seed);
            float Rf(float a, float b) => (float)(a + rng.NextDouble() * (b - a));

            // 재질: 풀 그림마다 하나 + 들꽃 그림마다 하나
            var texs = new List<Texture2D>(lib.grass);
            int grassCount = texs.Count;
            if (lib.flowers != null) texs.AddRange(lib.flowers);
            var shader = Shader.Find("Fishing/Grass");
            if (shader == null) return;
            var mats = new Material[texs.Count];
            for (int i = 0; i < texs.Count; i++)
                mats[i] = new Material(shader) { mainTexture = texs[i], name = "Grass_" + texs[i].name };

            // 심을 바닥: 지형(풀 층 번호) 또는 풀 텍스처 메쉬
            var targets = new List<(Collider col, Bounds b, Terrain terrain, int layer, float[,,] alpha)>();
            foreach (var t in world.GetComponentsInChildren<Terrain>())
            {
                var td = t.terrainData;
                int layer = -1;
                for (int i = 0; i < td.terrainLayers.Length; i++)
                    if (td.terrainLayers[i] != null && td.terrainLayers[i].diffuseTexture != null && td.terrainLayers[i].diffuseTexture.name.StartsWith("grass")) layer = i;
                if (layer < 0) continue;
                var col = t.GetComponent<TerrainCollider>();
                if (col == null) continue;
                targets.Add((col, col.bounds, t, layer, td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight)));
            }
            foreach (var r in world.GetComponentsInChildren<MeshRenderer>())
            {
                var m = r.sharedMaterial;
                if (m == null || m.mainTexture == null || !m.mainTexture.name.StartsWith("grass")) continue;
                var col = r.GetComponent<Collider>();
                if (col == null) continue;
                targets.Add((col, r.bounds, null, -1, null));
            }
            if (targets.Count == 0) return;

            // 칸별로 모은 뒤 메쉬로 굽는다
            var cells = new Dictionary<Vector2Int, List<(Vector3 p, float yaw, float w, float h, int tex, Color tint)>>();
            int total = 0;
            foreach (var (col, b, terrain, layer, alpha) in targets)
            {
                for (float x = b.min.x; x < b.max.x && total < MaxClumps; x += Spacing)
                for (float z = b.min.z; z < b.max.z && total < MaxClumps; z += Spacing)
                {
                    var o = new Vector3(x + Rf(-0.5f, 0.5f) * Spacing, b.max.y + 30f, z + Rf(-0.5f, 0.5f) * Spacing);
                    if (!Physics.Raycast(o, Vector3.down, out var hit, b.size.y + 60f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.collider != col) continue;              // 길·건물·데크·바위가 덮고 있다
                    if (hit.point.y < waterY + 0.2f) continue;      // 물가·갯벌
                    if (hit.normal.y < 0.82f) continue;             // 35°보다 가파른 비탈
                    float weight = 1f;
                    if (terrain != null)
                    {
                        var td = terrain.terrainData;
                        var lp = hit.point - terrain.transform.position;
                        int ax = Mathf.Clamp(Mathf.FloorToInt(lp.x / td.size.x * td.alphamapWidth), 0, td.alphamapWidth - 1);
                        int az = Mathf.Clamp(Mathf.FloorToInt(lp.z / td.size.z * td.alphamapHeight), 0, td.alphamapHeight - 1);
                        weight = alpha[az, ax, layer];
                    }
                    // 풀 층이 진할수록 빽빽하게, 군데군데 비어 있게 (큰 얼룩 노이즈)
                    float patch = Mathf.PerlinNoise(hit.point.x * 0.06f + 13f, hit.point.z * 0.06f + 7f);
                    if (rng.NextDouble() > Mathf.Clamp01((weight - 0.35f) * 1.6f) * Mathf.Lerp(0.25f, 1f, patch)) continue;
                    bool flower = texs.Count > grassCount && rng.NextDouble() < 0.06 + 0.1 * Mathf.Clamp01(patch - 0.55f);
                    int tex = flower ? grassCount + rng.Next(texs.Count - grassCount) : rng.Next(grassCount);
                    float h = flower ? Rf(0.35f, 0.6f) : Rf(0.35f, 0.75f) * Mathf.Lerp(0.7f, 1.15f, patch);
                    float w = h * (texs[tex].width >= texs[tex].height * 1.5f ? 2f : 1.25f);
                    var tint = Color.Lerp(new Color(0.78f, 0.82f, 0.62f), new Color(1.05f, 1.05f, 0.95f), Mathf.PerlinNoise(hit.point.x * 0.15f, hit.point.z * 0.15f)) * Rf(0.88f, 1.06f);
                    var key = new Vector2Int(Mathf.FloorToInt(hit.point.x / Cell), Mathf.FloorToInt(hit.point.z / Cell));
                    if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<(Vector3, float, float, float, int, Color)>();
                    list.Add((hit.point + Vector3.down * 0.03f, Rf(0f, Mathf.PI), w, h, tex, tint));
                    total++;
                }
            }

            var root = new GameObject("Grass").transform;
            root.SetParent(world, false);
            var culler = root.gameObject.AddComponent<GrassCuller>();
            foreach (var kv in cells) culler.Add(BuildCell(root, kv.Value, mats));
            Debug.Log($"[Grass] 포기 {total}개, 칸 {cells.Count}개");
        }

        static Renderer BuildCell(Transform root, List<(Vector3 p, float yaw, float w, float h, int tex, Color tint)> clumps, Material[] mats)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var norms = new List<Vector3>();
            var subs = new List<int>[mats.Length];
            for (int i = 0; i < mats.Length; i++) subs[i] = new List<int>();
            foreach (var c in clumps)
                for (int q = 0; q < 2; q++)
                {
                    float a = c.yaw + q * Mathf.PI * 0.5f;
                    var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (c.w * 0.5f);
                    int v0 = verts.Count;
                    verts.Add(c.p - d); verts.Add(c.p + d);
                    verts.Add(c.p + d + Vector3.up * c.h); verts.Add(c.p - d + Vector3.up * c.h);
                    uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                    for (int k = 0; k < 4; k++) { cols.Add(c.tint); norms.Add(Vector3.up); }
                    subs[c.tex].AddRange(new[] { v0, v0 + 2, v0 + 1, v0, v0 + 3, v0 + 2 });
                }
            var used = new List<int>();
            for (int i = 0; i < subs.Length; i++) if (subs[i].Count > 0) used.Add(i);
            var mesh = new Mesh { name = "GrassCell", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(cols);
            mesh.SetNormals(norms);
            mesh.subMeshCount = used.Count;
            var cellMats = new Material[used.Count];
            for (int i = 0; i < used.Count; i++)
            {
                mesh.SetTriangles(subs[used[i]], i);
                cellMats[i] = mats[used[i]];
            }
            mesh.RecalculateBounds();
            var go = new GameObject("GrassCell");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = cellMats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mr;
        }
    }

    /// <summary>카메라에서 먼 풀 칸을 끈다 (0.25초마다).</summary>
    public class GrassCuller : MonoBehaviour
    {
        const float Far = 130f;
        readonly List<Renderer> _cells = new List<Renderer>();
        float _next;

        public void Add(Renderer r) => _cells.Add(r);

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            var cam = Game.CameraRig != null ? Game.CameraRig.Cam : Camera.main;
            if (cam == null) return;
            var p = cam.transform.position;
            foreach (var r in _cells)
            {
                if (r == null) continue;
                var b = r.bounds;
                float d = Mathf.Sqrt(b.SqrDistance(p));
                r.enabled = d < Far;
            }
        }
    }
}
