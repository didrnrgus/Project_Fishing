using System.Collections.Generic;
using Fishing.World;
using UnityEngine;

namespace Fishing.Buddy
{
    /// <summary>
    /// 윤슬이 길을 안내할 때 쓰는 길 찾기: 1m 칸 A*. 맵에 NavMesh를 굽지 않아서, 칸은 처음 닿을 때만 검사한다
    /// (옆 칸 땅 높이에서 아래로 쏴 땅을 찾고, 물이 아니고, 몸 크기 캡슐이 벽·난간·건물에 안 걸리고, 옆 칸과 높이 차가 0.7m 이하).
    /// 찾은 길은 곧게 갈 수 있는 곳까지 건너뛰어 꺾는 점만 남긴다. 못 찾으면 null (곧장 걷는다).
    /// </summary>
    public static class BuddyPath
    {
        const int MaxNodes = 15000;
        const float MaxStep = 0.7f;
        const int Mask = ~(1 << 2); // Ignore Raycast(윤슬 자신) 빼고

        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        public static List<Vector3> Find(Vector3 from, Vector3 goal, float goalRadius)
        {
            var start = new Vector2Int(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.z));
            var height = new Dictionary<Vector2Int, float>(); // NaN = 못 가는 칸
            var cost = new Dictionary<Vector2Int, float> { [start] = 0f };
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var open = new SortedSet<(float f, int seq, int x, int z)>();
            int seq = 0;
            height[start] = from.y;
            open.Add((H(start, goal, goalRadius), seq++, start.x, start.y));
            var closed = new HashSet<Vector2Int>();

            while (open.Count > 0 && closed.Count < MaxNodes)
            {
                var top = open.Min;
                open.Remove(top);
                var c = new Vector2Int(top.x, top.z);
                if (!closed.Add(c)) continue;
                if (Flat(new Vector3(c.x, 0f, c.y) - goal) <= goalRadius) return Smooth(Build(c, parent, height));
                float h0 = height[c];
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (closed.Contains(n)) continue;
                    if (!height.TryGetValue(n, out float hn)) height[n] = hn = Probe(n, h0);
                    if (float.IsNaN(hn) || Mathf.Abs(hn - h0) > MaxStep) continue;
                    // 대각선은 옆 두 칸도 지나갈 수 있어야 (모서리를 깎아 벽에 끼지 않게)
                    if (d.x != 0 && d.y != 0 && (Blocked(c + new Vector2Int(d.x, 0), h0, height) || Blocked(c + new Vector2Int(0, d.y), h0, height))) continue;
                    float g = cost[c] + (d.x != 0 && d.y != 0 ? 1.414f : 1f);
                    if (cost.TryGetValue(n, out float old) && old <= g) continue;
                    cost[n] = g;
                    parent[n] = c;
                    open.Add((g + H(n, goal, goalRadius), seq++, n.x, n.y));
                }
            }
            return null;
        }

        static bool Blocked(Vector2Int c, float near, Dictionary<Vector2Int, float> height)
        {
            if (!height.TryGetValue(c, out float h)) height[c] = h = Probe(c, near);
            return float.IsNaN(h) || Mathf.Abs(h - near) > MaxStep;
        }

        /// <summary>칸의 땅 높이 (못 가면 NaN). near: 옆 칸 높이 — 그 높이 조금 위에서 쏴서 다리 위·아래를 섞지 않는다.</summary>
        static float Probe(Vector2Int c, float near)
        {
            var top = new Vector3(c.x, near + 1.6f, c.y);
            if (!Physics.Raycast(top, Vector3.down, out var hit, 3.5f, Mask, QueryTriggerInteraction.Ignore)) return float.NaN;
            var p = hit.point;
            if (!FieldMap.IsWalkable(p)) return float.NaN;
            if (Physics.CheckCapsule(p + Vector3.up * 0.6f, p + Vector3.up * 1.5f, 0.28f, Mask, QueryTriggerInteraction.Ignore)) return float.NaN;
            return p.y;
        }

        static float H(Vector2Int c, Vector3 goal, float r) => Mathf.Max(0f, Flat(new Vector3(c.x, 0f, c.y) - goal) - r);

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        static List<Vector3> Build(Vector2Int end, Dictionary<Vector2Int, Vector2Int> parent, Dictionary<Vector2Int, float> height)
        {
            var list = new List<Vector3>();
            for (var c = end; ; c = parent[c])
            {
                list.Add(new Vector3(c.x, height[c], c.y));
                if (!parent.ContainsKey(c)) break;
            }
            list.Reverse();
            return list;
        }

        /// <summary>곧게 갈 수 있는 점까지 건너뛴다 (몸 크기 캡슐이 안 걸리고, 사이가 다 물이 아니면). 한 번에 12칸까지만 본다.</summary>
        static List<Vector3> Smooth(List<Vector3> path)
        {
            var result = new List<Vector3> { path[0] };
            int i = 0;
            while (i < path.Count - 1)
            {
                int j = Mathf.Min(path.Count - 1, i + 12);
                while (j > i + 1 && !Straight(path[i], path[j])) j--;
                result.Add(path[j]);
                i = j;
            }
            return result;
        }

        static bool Straight(Vector3 a, Vector3 b)
        {
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return true;
            if (Physics.CapsuleCast(a + Vector3.up * 0.6f, a + Vector3.up * 1.5f, 0.28f, d / len, len, Mask, QueryTriggerInteraction.Ignore)) return false;
            // 사이 땅이 두 점 높이를 이은 선에서 1m 넘게 벗어나면(턱·낭떠러지) 곧게 가지 않는다
            for (float t = 0.5f; t < len; t += 0.5f)
            {
                var p = a + d * (t / len);
                if (!FieldMap.IsWalkable(p)) return false;
                if (!Physics.Raycast(p + Vector3.up * 1.6f, Vector3.down, out var hit, 3.5f, Mask, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y - p.y) > 1f) return false;
            }
            return true;
        }
    }
}
