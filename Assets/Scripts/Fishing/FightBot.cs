using UnityEngine;

namespace Fishing.Angling
{
    /// <summary>
    /// 자동 파이팅 입력. 스크린샷 자동화와 에디터 밸런스 시뮬레이션에서 "적당히 잘하는 플레이어"로 쓴다.
    /// 장력을 하이텐션 구간(0.6~0.9)에 두고, 낚싯대는 물고기 반대쪽, 끝에 몰리면 가끔 카운터를 친다.
    /// </summary>
    public static class FightBot
    {
        public static FightInput Input(FishFight f, float counterChancePerTick = 0.08f)
        {
            float targetRod = -Mathf.Sign(f.FishX) * 0.8f;
            float dx = (targetRod - f.RodX) * 8f;
            if (f.AtEdge && Random.value < counterChancePerTick) dx = -Mathf.Sign(f.FishX) * 120f;
            return new FightInput
            {
                Releasing = f.Tension > 0.9f,
                Reeling = f.Tension < 0.72f,
                MouseDX = dx,
            };
        }
    }
}
