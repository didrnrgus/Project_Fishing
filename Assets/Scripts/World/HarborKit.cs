using System;
using System.Collections.Generic;
using Fishing.Core;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 포구 콘크리트 구조물(방파제·호안)을 늘린 상자 대신 실제 시공 단위로 짓는 공용 도구 (CoastMap·서천·홍현이 같이 쓴다).
    /// - 모따기 블록: 모서리를 깎고 법선을 면 쪽으로 남겨(weighted normal) 모서리에 빛이 맺힌다. 꼭짓점 24개짜리라 가볍다
    /// - 케이슨·상판·파라펫을 10m 타설 단위로 끊어 이음매를 진짜 틈(속은 짙은 실링)으로, 블록마다 타설 색이 조금씩 다르다
    /// - 꼭짓점 색에 밑동·틈 그늘을 구워 Fishing/Weathered(_VertexAO)가 곱한다
    /// - 파라펫: 1.3m 안팎, 두께 0.9m, 바다 쪽 위를 비스듬히 깎고 안쪽에 디딤단 (콜라이더 — 점프로 올라서 바깥을 보고 던진다)
    /// - 안쪽(배 대는 쪽): 낮은 연석과 노란 선, 계선주, 폐타이어 방충재, 손잡이가 상판 위로 휜 철 사다리, 파라펫 배수구,
    ///   낚시꾼이 두고 간 잡동사니(아이스박스·밑밥통·사려 둔 밧줄)
    /// - 호안 벽(<see cref="QuayWall"/>): 몸통 + 갓돌, 바다 쪽에 타이어·사다리. 앞마당 잡동사니(<see cref="QuayClutter"/>)
    /// 소품은 Blender(tools/blender/props_harbor.py)이고 없으면 그 자리만 비운다.
    /// </summary>
    public static class HarborKit
    {
        /// <summary>블록마다 다른 타설 색: 밝기 ±4%, 아주 옅게 누렇거나 푸르스름하게.</summary>
        public static Color PourTint(System.Random rng, float shade = 1f)
        {
            float v = shade * (0.96f + (float)rng.NextDouble() * 0.08f);
            float w = ((float)rng.NextDouble() - 0.5f) * 0.03f;
            return new Color(v * (1f + w), v, v * (1f - w), 1f);
        }

        // ---------------- 방파제 ----------------

        public struct BreakwaterSpec
        {
            /// <summary>평면 (x, z, 너비, 길이) — 맵 루트 기준.</summary>
            public Rect Rect;
            /// <summary>길이 방향이 z(해안에서 뻗는 몸통)인지, x(끝에서 꺾인 팔)인지.</summary>
            public bool AlongZ;
            /// <summary>파라펫 쪽: -1 = xMin(몸통)·zMin(팔), +1 = xMax·zMax, 0 = 없음. 안쪽(배 대는 쪽) 시설은 반대편에.</summary>
            public int ParapetSide;
            public float TopY, BottomY, WaterY;
            public float ParapetH, ParapetW;
            /// <summary>파라펫이 상판 시작·끝보다 더(+)/덜(-) 나가는 길이 — 꺾인 팔 파라펫 안쪽 면까지 이어 붙일 때.</summary>
            public float ParapetExtStart, ParapetExtEnd;
            /// <summary>이 점(맵 루트 좌표)에서는 안쪽 연석·계선주·방충재를 두지 않는다 (다른 몸통과 붙은 곳).</summary>
            public Func<Vector3, bool> NoInnerEdge;
            /// <summary>안쪽 시설(연석·노란 선·계선주·타이어·사다리)을 둘지.</summary>
            public bool Fittings;
            /// <summary>뿌리 쪽 안쪽에 인명구조함.</summary>
            public bool LifeRing;
            /// <summary>파라펫이 1m 넘어도 디딤단을 두지 않는다 (안쪽 면에 벽화가 있는 홍현).</summary>
            public bool NoStep;
            public Color Tint, ParapetTint;
        }

        static Material _seal, _paint, _steel;
        static Material Seal => _seal ??= Mats.Lit(new Color(0.16f, 0.16f, 0.15f), 0.1f);
        static Material Paint => _paint ??= Mats.Lit(new Color(0.93f, 0.76f, 0.16f), 0.25f);
        /// <summary>사다리 철: 노랗게 칠한 위로 녹 번진 결.</summary>
        public static Material Steel => _steel ??= Mats.Weathered("rust", new Color(0.82f, 0.66f, 0.22f), 1.2f, 0.4f, 1f, 0.3f, 0f, null, 0.5f, 0.3f);

        /// <summary>
        /// 방파제 한 토막: 케이슨 몸통 + 10m마다 끊긴 상판 + 파라펫 블록 + 안쪽 시설. 몸통 상자 콜라이더 하나 (위 = TopY).
        /// 반환: 토막 루트 (맵 루트 아래).
        /// </summary>
        public static Transform Breakwater(Transform parent, string name, BreakwaterSpec s, System.Random rng)
        {
            var r = s.Rect;
            float L = s.AlongZ ? r.height : r.width, W = s.AlongZ ? r.width : r.height;
            var rot = s.AlongZ ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            var center = new Vector3(r.center.x, 0f, r.center.y);
            // 로컬: 길이 = +z, 가로 = x. 팔(90° 회전)은 로컬 -x가 월드 +z라 파라펫 쪽 부호가 뒤집힌다
            int ps = s.AlongZ ? s.ParapetSide : -s.ParapetSide;
            int inner = ps != 0 ? -ps : -1;
            Vector3 World(Vector3 local) => center + rot * local;

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = center;
            root.localRotation = rot;
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, (s.TopY + s.BottomY) * 0.5f, 0f);
            col.size = new Vector3(W, s.TopY - s.BottomY, L);

            var body = new BlockMesh();
            var seal = new BlockMesh();
            var paint = new BlockMesh();
            var par = new BlockMesh();
            const float slabT = 0.32f;
            int n = Mathf.Max(1, Mathf.RoundToInt(L / 10f));
            float seg = L / n;
            var white = Color.white;
            for (int i = 0; i < n; i++)
            {
                float z0 = -L * 0.5f + i * seg, z1 = z0 + seg, zm = (z0 + z1) * 0.5f;
                // 케이슨: 상판보다 4cm 안으로 들어가 상판 모서리 밑에 그늘 줄이 진다. 블록마다 가로로 1~2cm 어긋난다
                float bh = s.TopY - slabT - s.BottomY;
                float jog = ((float)rng.NextDouble() - 0.5f) * 0.03f;
                var tint = PourTint(rng);
                body.Block(new Vector3(jog, s.BottomY + bh * 0.5f, zm), new Vector3(W - 0.08f, bh, seg - 0.05f), 0.05f, Quaternion.identity,
                    tint * 0.92f, tint * 0.55f);
                // 상판: 3cm 틈, 높이는 0~6mm 낮게 들쭉날쭉 (각자 타설한 판)
                float dip = (float)rng.NextDouble() * 0.006f;
                tint = PourTint(rng);
                body.Block(new Vector3(0f, s.TopY - slabT * 0.5f - dip, zm), new Vector3(W, slabT, seg - 0.03f), 0.025f, Quaternion.identity,
                    tint, tint * 0.72f);
                if (i < n - 1)
                {
                    // 이음매 속 실링 (틈으로 하늘이 비치지 않게)
                    seal.Block(new Vector3(0f, s.TopY - 0.03f, z1), new Vector3(W - 0.04f, 0.04f, 0.04f), 0f, Quaternion.identity, white, white);
                    seal.Block(new Vector3(0f, s.BottomY + bh * 0.5f, z1), new Vector3(W - 0.3f, bh, 0.06f), 0f, Quaternion.identity, white, white);
                }
            }

            // 파라펫: 바다 쪽 면은 몸통 바깥 면과 이어지고, 위는 바다 쪽으로 비스듬히 깎였다. 안쪽 면은 곧다 (홍현 벽화가 붙는다).
            // 1m 넘으면 안쪽 밑동에 디딤단 (실제 방파제처럼 올라서서 바깥 테트라포드 쪽을 본다). 다른 파라펫과 맞물리는 끝은 디딤단을 줄인다
            if (ps != 0)
            {
                float H = s.ParapetH, P = s.ParapetW, xo = ps * W * 0.5f;
                bool step = H >= 1f && !s.NoStep;
                float stepH = Mathf.Min(0.45f, H * 0.35f), stepD = 0.45f;
                var prof = new List<Vector2>();
                foreach (var (d, y) in new[] { (0f, -0.06f), (0f, H - 0.3f), (P * 0.36f, H), (P - 0.06f, H), (P, H - 0.06f), (P, -0.06f) })
                    prof.Add(new Vector2(xo - ps * d, s.TopY + y));
                float pz0 = -L * 0.5f - s.ParapetExtStart, pz1 = L * 0.5f + s.ParapetExtEnd;
                float sz0 = pz0 + (s.ParapetExtStart > 0f ? stepD : 0f), sz1 = pz1 - (s.ParapetExtEnd > 0f ? stepD : 0f);
                int pn = Mathf.Max(1, Mathf.RoundToInt((pz1 - pz0) / 10f));
                float pseg = (pz1 - pz0) / pn;
                float xin = xo - ps * P;
                for (int i = 0; i < pn; i++)
                {
                    float a = pz0 + i * pseg, b = a + pseg;
                    float a2 = a + (i > 0 ? 0.0125f : 0f), b2 = b - (i < pn - 1 ? 0.0125f : 0f);
                    var tint = PourTint(rng);
                    par.Extrude(prof, a2, b2, Vector3.zero, Quaternion.identity, tint, tint * 0.62f);
                    if (i < pn - 1)
                        seal.Block(new Vector3(xo - ps * P * 0.5f, s.TopY + H * 0.5f - 0.03f, b), new Vector3(P - 0.06f, H - 0.06f, 0.03f), 0f, Quaternion.identity, white, white);
                    if (step)
                    {
                        float c0 = Mathf.Max(a2, sz0), c1 = Mathf.Min(b2, sz1);
                        if (c1 - c0 > 0.3f)
                            par.Block(new Vector3(xin - ps * stepD * 0.5f, s.TopY + (stepH - 0.06f) * 0.5f, (c0 + c1) * 0.5f), new Vector3(stepD, stepH + 0.06f, c1 - c0), 0.045f,
                                Quaternion.identity, tint * 0.97f, tint * 0.6f);
                    }
                    // 배수구: 파라펫(디딤단) 밑동 안쪽 면에 5m마다 (이음매 자리는 피한다)
                    float xh = step ? xin - ps * stepD : xin;
                    for (float z = a + 2.5f; z < b - 1f; z += 5f)
                        seal.Block(new Vector3(xh - ps * 0.006f, s.TopY + 0.09f, z), new Vector3(0.02f, 0.15f, 0.3f), 0f, Quaternion.identity, white, white);
                }
                AddBox(root, "ParapetCol", new Vector3(xo - ps * P * 0.5f, s.TopY + H * 0.5f, (pz0 + pz1) * 0.5f), new Vector3(P, H, pz1 - pz0));
                if (step) AddBox(root, "StepCol", new Vector3(xin - ps * stepD * 0.5f, s.TopY + stepH * 0.5f, (sz0 + sz1) * 0.5f), new Vector3(stepD, stepH, sz1 - sz0));
            }

            if (s.Fittings) InnerFittings(root, s, rng, W, L, inner, World, body, paint);

            body.Emit(root, name + "Body", Mats.VertexAO(Mats.Quay(s.Tint)));
            par.Emit(root, name + "Parapet", Mats.VertexAO(Mats.Quay(s.ParapetTint)));
            seal.Emit(root, name + "Joints", Seal);
            paint.Emit(root, name + "Paint", Paint);
            return root;
        }

        static Transform AddBox(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<BoxCollider>();
            b.center = center;
            b.size = size;
            return go.transform;
        }

        /// <summary>
        /// 파라펫·디딤단 콜라이더: 사람만 막고 카메라(구 캐스트에 걸려 벽 앞으로 당겨졌다)·물 클릭·찌 날아가는 길은 통과시킨다.
        /// </summary>
        public static bool IsLowWall(Collider c) => c != null && (c.name == "ParapetCol" || c.name == "StepCol");

        /// <summary>파라펫과 디딤단이 안쪽으로 차지하는 너비 (가로등·NPC를 그 밖에 두려고).</summary>
        public static float ParapetFootprint(float parapetH, float parapetW, bool noStep = false) => parapetW + (parapetH >= 1f && !noStep ? 0.45f : 0f);

        /// <summary>안쪽 가장자리: 연석(2.4m 토막) + 노란 선, 14m마다 계선주, 6m 남짓마다 폐타이어, 26m마다 철 사다리, 뿌리에 인명구조함, 잡동사니.</summary>
        static void InnerFittings(Transform root, BreakwaterSpec s, System.Random rng, float W, float L, int inner, Func<Vector3, Vector3> world, BlockMesh body, BlockMesh paint)
        {
            float xe = inner * W * 0.5f; // 안쪽 모서리 (로컬 x)
            bool Skip(float z) => s.NoInnerEdge != null && s.NoInnerEdge(world(new Vector3(xe, 0f, z)));
            // 연석과 노란 선
            for (float z = -L * 0.5f + 0.2f; z < L * 0.5f - 0.2f; z += 2.4f)
            {
                float len = Mathf.Min(2.38f, L * 0.5f - 0.2f - z);
                if (len < 0.4f || Skip(z + len * 0.5f)) continue;
                var tint = PourTint(rng, 0.97f);
                body.Block(new Vector3(xe - inner * 0.12f, s.TopY + 0.05f, z + len * 0.5f), new Vector3(0.22f, 0.16f, len), 0.03f, Quaternion.identity, tint, tint * 0.75f);
                paint.Block(new Vector3(xe - inner * 0.38f, s.TopY + 0.004f, z + len * 0.5f), new Vector3(0.16f, 0.012f, len + 0.02f), 0f, Quaternion.identity, Color.white, Color.white);
            }
            // 계선주 (뭍에서 안쪽 모서리 0.65m 안)
            var ladders = new List<float>();
            for (float z = -L * 0.5f + 18f; z < L * 0.5f - 6f; z += 26f) if (!Skip(z)) ladders.Add(z);
            for (float z = -L * 0.5f + 7f; z < L * 0.5f - 4f; z += 14f)
            {
                if (Skip(z) || ladders.Exists(lz => Mathf.Abs(lz - z) < 1.5f)) continue;
                var go = KenneyProps.Spawn("Props/harbor_bollard", root, new Vector3(xe - inner * 0.65f, s.TopY, z), -inner * 90f);
                if (go == null)
                    Mats.Prim(PrimitiveType.Cylinder, "Bollard", root, new Vector3(xe - inner * 0.65f, s.TopY + 0.25f, z), new Vector3(0.3f, 0.25f, 0.3f),
                        Mats.Lit(new Color(0.15f, 0.16f, 0.18f), 0.45f, 0.3f));
                // 계선주 옆에 사려 둔 밧줄 (셋 중 하나)
                else if (rng.NextDouble() < 0.35)
                    KenneyProps.Spawn("Props/harbor_coil", root, new Vector3(xe - inner * 1.3f, s.TopY, z + 0.9f), (float)rng.NextDouble() * 360f, 0f, 0f, false);
            }
            // 낚시꾼이 두고 간 짐 (아이스박스 + 밑밥통): 안쪽 모서리 1~2m 안, 20~35m마다
            for (float z = -L * 0.5f + 10f + (float)rng.NextDouble() * 8f; z < L * 0.5f - 6f; z += 20f + (float)rng.NextDouble() * 15f)
            {
                if (Skip(z) || ladders.Exists(lz => Mathf.Abs(lz - z) < 2f)) continue;
                var at = new Vector3(xe - inner * (1.1f + (float)rng.NextDouble() * 0.8f), s.TopY, z);
                KenneyProps.Spawn("Props/harbor_cooler", root, at, inner * 90f + ((float)rng.NextDouble() - 0.5f) * 40f, 0f, 0f, false);
                KenneyProps.Spawn("Props/harbor_bucket", root, at + new Vector3(-inner * 0.15f, 0f, 0.55f), (float)rng.NextDouble() * 360f, 0f, 0f, false);
            }
            // 폐타이어 방충재: 상판 모서리 고리에 매달려 벽에 납작하게 붙는다 (사다리 자리 비움)
            for (float z = -L * 0.5f + 4f; z < L * 0.5f - 3f; z += 5.5f + (float)rng.NextDouble() * 1.5f)
            {
                if (Skip(z) || ladders.Exists(lz => Mathf.Abs(lz - z) < 1.2f)) continue;
                KenneyProps.Spawn("Props/harbor_tire", root, new Vector3(xe + inner * 0.08f, s.TopY - 0.03f, z), inner * 90f + ((float)rng.NextDouble() - 0.5f) * 8f, 0f, 0f, false);
            }
            // 철 사다리: 물속 1.2m에서 올라와 손잡이가 상판 위로 둥글게 넘어온다
            if (ladders.Count > 0)
            {
                var paths = new List<Vector3[]>();
                foreach (float z in ladders)
                    Ladder(paths, xe, inner, z, s.WaterY - 1.2f, s.TopY);
                var go = new GameObject("Ladders");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = MapKit.Tubes(paths, _ => 0.024f, 6);
                go.AddComponent<MeshRenderer>().sharedMaterial = Steel;
            }
            if (s.LifeRing)
            {
                float z = -L * 0.5f + 5f;
                if (!Skip(z)) KenneyProps.Spawn("Props/harbor_lifering", root, new Vector3(xe - inner * 1.1f, s.TopY, z), -inner * 90f); // 상판 쪽을 본다
            }
        }

        /// <summary>사다리 하나 (관 경로들): 레일 두 줄(위는 손잡이 고리), 30cm마다 디딤대. xe = 벽 면, inner = 벽 바깥 방향 부호.</summary>
        public static void Ladder(List<Vector3[]> paths, float xe, int inner, float z, float yBottom, float yTop)
        {
            float xw = xe + inner * 0.09f, xd = xe - inner * 0.32f;
            foreach (float dz in new[] { -0.23f, 0.23f })
            {
                float zz = z + dz;
                paths.Add(new[]
                {
                    new Vector3(xw, yBottom, zz), new Vector3(xw, yTop - 0.05f, zz), new Vector3(xw, yTop + 0.7f, zz),
                    new Vector3(xw - inner * 0.05f, yTop + 0.9f, zz), new Vector3(xe - inner * 0.1f, yTop + 0.98f, zz),
                    new Vector3(xd + inner * 0.06f, yTop + 0.88f, zz), new Vector3(xd, yTop + 0.65f, zz), new Vector3(xd, yTop - 0.02f, zz),
                });
                // 벽에 박은 받침 (레일과 벽 사이)
                for (float y = yBottom + 0.5f; y < yTop - 0.2f; y += 1.4f)
                    paths.Add(new[] { new Vector3(xe - inner * 0.02f, y, zz), new Vector3(xw, y, zz) });
            }
            for (float y = yBottom + 0.3f; y < yTop - 0.1f; y += 0.3f)
                paths.Add(new[] { new Vector3(xw, y, z - 0.25f), new Vector3(xw, y, z + 0.25f) });
        }

        // ---------------- 호안 ----------------

        /// <summary>
        /// 호안 벽 x0~x1: 8m 토막(벽 가운데 선 z = lineZ(x), 윗면 top)마다 몸통 + 갓돌, 토막마다 상자 콜라이더.
        /// fittings면 바다(+z) 쪽 면에 폐타이어, 세 토막마다 철 사다리.
        /// </summary>
        public static Transform QuayWall(Transform parent, float x0, float x1, Func<float, float> lineZ, float top, float bottom, float waterY, Material mat,
            System.Random rng, bool fittings, float thick = 1f)
        {
            var root = new GameObject("QuayWall").transform;
            root.SetParent(parent, false);
            var buf = new BlockMesh();
            var ladders = new List<Vector3[]>();
            int seg = 0;
            for (float x = x0; x < x1; x += 8f, seg++)
            {
                float xb = Mathf.Min(x + 8f, x1);
                var a = new Vector3(x, top, lineZ(x));
                var b = new Vector3(xb, top, lineZ(xb));
                var dir = b - a;
                if (dir.magnitude < 0.5f) continue;
                var fwd = dir.normalized;
                // 이웃 토막과 0.6m 겹쳐 굽은 해안에서도 틈이 안 벌어진다
                QuayBlock(buf, a - fwd * 0.3f, b + fwd * 0.3f, thick, top - bottom, seg, rng);
                var mid = (a + b) * 0.5f;
                var col = AddBox(root, "QuayCol", Vector3.zero, new Vector3(thick, top - bottom, dir.magnitude + 0.6f));
                col.localPosition = mid + Vector3.down * ((top - bottom) * 0.5f);
                col.localRotation = Quaternion.LookRotation(fwd);
                if (!fittings) continue;
                var sea = Vector3.Cross(fwd, Vector3.up); // 진행 방향(+x)의 왼쪽 = +z = 바다
                float face = thick * 0.5f + 0.1f;          // 갓돌 바다 쪽 면
                if (seg % 3 == 1)
                {
                    // 사다리 로컬(z = 해안선 방향, -x = 바다 쪽, y는 그대로) → 맵 좌표
                    var paths = new List<Vector3[]>();
                    Ladder(paths, -face, -1, 0f, waterY - 1.2f, top);
                    var m = Matrix4x4.TRS(new Vector3(mid.x, 0f, mid.z), Quaternion.LookRotation(fwd), Vector3.one);
                    foreach (var path in paths)
                    {
                        for (int i = 0; i < path.Length; i++) path[i] = m.MultiplyPoint3x4(path[i]);
                        ladders.Add(path);
                    }
                }
                else KenneyProps.Spawn("Props/harbor_tire", root, mid + sea * (face + 0.08f) + Vector3.down * 0.03f,
                    Quaternion.LookRotation(sea).eulerAngles.y + ((float)rng.NextDouble() - 0.5f) * 8f, 0f, 0f, false);
            }
            buf.Emit(root, "QuayWallMesh", Mats.VertexAO(mat));
            if (ladders.Count > 0)
            {
                var go = new GameObject("QuayLadders");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = MapKit.Tubes(ladders, _ => 0.024f, 6);
                go.AddComponent<MeshRenderer>().sharedMaterial = Steel;
            }
            return root;
        }

        static readonly string[] ClutterKinds = { "harbor_netpile", "harbor_crates", "harbor_buoys", "gear", "harbor_coil" };

        /// <summary>호안 앞마당 잡동사니 한 무더기 (그물 더미·생선 상자·부표 더미·아이스박스+밑밥통·밧줄). 부딪히지 않는다.</summary>
        public static void QuayClutter(Transform parent, Vector3 at, float yaw, System.Random rng)
        {
            string kind = ClutterKinds[rng.Next(ClutterKinds.Length)];
            var rot = Quaternion.Euler(0f, yaw, 0f);
            if (kind == "gear")
            {
                KenneyProps.Spawn("Props/harbor_cooler", parent, at, yaw, 0f, 0f, false);
                KenneyProps.Spawn("Props/harbor_bucket", parent, at + rot * new Vector3(0.55f, 0f, 0.1f), (float)rng.NextDouble() * 360f, 0f, 0f, false);
                return;
            }
            KenneyProps.Spawn("Props/" + kind, parent, at, yaw, 0f, 0f, false);
            // 생선 상자 옆에 부표를 하나 더 (어구를 모아 둔 자리)
            if (kind == "harbor_crates" && rng.NextDouble() < 0.5)
                KenneyProps.Spawn("Props/harbor_buoys", parent, at + rot * new Vector3(-1.3f, 0f, 0.3f), (float)rng.NextDouble() * 360f, 0f, 0f, false);
        }

        /// <summary>
        /// 호안 벽 한 토막 (a→b, 맵 루트 좌표, y = 벽 윗면): 몸통 블록 + 바다 쪽으로 10cm 내민 갓돌(코핑). 토막마다 1.5cm씩 두께·높이를 엇갈려
        /// 겹친 이음매가 깜빡이지 않는다. seaSide = 바다 쪽 수평 방향.
        /// </summary>
        public static void QuayBlock(BlockMesh buf, Vector3 a, Vector3 b, float thick, float height, int seg, System.Random rng)
        {
            var dir = b - a;
            dir.y = 0f;
            float len = dir.magnitude;
            if (len < 0.3f) return;
            var rot = Quaternion.LookRotation(dir / len);
            float st = (seg % 2) * 0.015f;
            var mid = (a + b) * 0.5f;
            var tint = PourTint(rng);
            float bodyH = height - 0.28f; // 갓돌(위 0.3m) 밑으로 2cm 물린다
            buf.Block(mid + Vector3.down * (0.28f + bodyH * 0.5f), new Vector3(thick + st, bodyH + st, len), 0.05f, rot, tint * 0.9f, tint * 0.5f);
            tint = PourTint(rng);
            buf.Block(mid + Vector3.down * (0.15f - st), new Vector3(thick + 0.2f + st, 0.3f, len - 0.03f), 0.04f, rot, tint, tint * 0.7f);
        }
    }
}
