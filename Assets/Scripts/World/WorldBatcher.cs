using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 맵을 지은 뒤 움직이지 않는 소품(집·데크·방파제·다리·바위·나무 등)을 정적 배칭으로 묶어 드로우 콜을 줄인다.
    /// 맵 빌더는 프리미티브 수백 개로 월드를 지어서, 배칭이 없으면 같은 재질끼리도 하나씩 그린다.
    ///
    /// 묶지 않는 것 (묶인 메쉬는 월드 좌표로 굳어서 움직이면 안 된다):
    /// - 스크립트가 붙은 물체와 그 아래 전부 (NPC 낚시꾼·어군·수족관·잔물결·배 등 런타임에 움직이는 것)
    /// - Animator 아래 (캐릭터), 꺼져 있는 물체, 읽을 수 없는 메쉬
    /// - Standard·Fishing/Ground(월드 좌표)가 아닌 셰이더 (물·하늘·물고기·파티클 셰이더는 오브젝트 좌표를 써서 배칭하면 깨진다)
    /// 재질 색을 나중에 바꾸는 것(SeasonFx의 눈 덮인 둔치, 밤 가로등 전구)은 배칭과 상관없이 동작한다.
    /// </summary>
    public static class WorldBatcher
    {
        /// <summary>마지막으로 묶은 렌더러 수 (로그·자동 촬영용).</summary>
        public static int LastBatched { get; private set; }

        public static void Batch(Transform root)
        {
            LastBatched = 0;
            if (root == null) return;
            var gos = new List<GameObject>();
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(false))
                if (CanBatch(mr, root)) gos.Add(mr.gameObject);
            if (gos.Count < 2) return;
            StaticBatchingUtility.Combine(gos.ToArray(), root.gameObject);
            LastBatched = gos.Count;
        }

        static bool CanBatch(MeshRenderer mr, Transform root)
        {
            if (!mr.enabled) return false;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return false;
            foreach (var m in mr.sharedMaterials)
                if (m == null || m.shader == null || (m.shader.name != "Standard" && m.shader.name != "Fishing/Ground")) return false;
            // 자기와 조상(월드 루트는 빼고) 중 스크립트·Animator가 있으면 런타임에 움직일 수 있다
            for (var t = mr.transform; t != null && t != root; t = t.parent)
            {
                if (t.GetComponent<MonoBehaviour>() != null || t.GetComponent<Animator>() != null) return false;
                if (t.GetComponent<Rigidbody>() != null) return false;
            }
            return true;
        }
    }
}
