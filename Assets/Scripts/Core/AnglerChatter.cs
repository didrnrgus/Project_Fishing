using System.Collections.Generic;
using System.Linq;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 같은 채널의 다른 낚시꾼 (원작은 온라인이라 왼쪽 아래 채팅에 남의 조과가 흘렀다, clips docs 05 §5·09 §3):
    ///   "[유현덕]님이[놀래미] 17.07 cm, 0.22 kg 을 잡았습니다."   "[*아이린*] : 잠시만요"
    /// 솔로판은 그 낚시터에 사는 어종으로 가짜 조과·잡담 줄을 가끔 띄워 채널이 살아 있는 느낌만 낸다 (게임 규칙에는 영향 없음).
    /// </summary>
    public static class AnglerChatter
    {
        static readonly string[] Names =
        {
            "아이린", "마루", "유현덕", "바다사나이", "손맛중독", "새벽조사", "갯바위왕", "찌올림", "흑장갑", "BIOWATER",
            "붕어빵굽는곰", "월척기원", "이오리와코", "낚시왕김씨", "청돔사냥꾼", "물안개", "릴감는소리", "파도타기",
        };

        static readonly string[] Chats =
        {
            "잠시만요", "오늘 입질 좋네요", "어군 어디쯤 있나요?", "방금 대물 터졌어요 ㅠㅠ", "줄 끊어졌다...", "ㅋㅋㅋ 축하드려요",
            "미끼 뭐 쓰세요?", "새벽에 큰 놈 나온대요", "배 타고 나가봐요", "카운터 타이밍 어렵네", "와 월척!", "수고하세요~",
            "먼바다 쪽이 좋더라구요", "납품 게이지 다 찼다", "어묵 하나 먹고 옵니다", "비 오니까 잘 무네요",
        };

        static float _nextCatch = 25f, _nextChat = 60f;

        /// <summary>GameUI가 필드에 있을 때 매 프레임 부른다.</summary>
        public static void Tick()
        {
            if (Game.Mode != GameMode.Field || Game.Fishing == null) return;
            // 파이팅·결과 중에는 시스템 줄이 숨겨지므로 쌓지 않는다
            var st = Game.Fishing.State;
            if (st == Fishing.Angling.FishingState.Fighting || st == Fishing.Angling.FishingState.Result) return;
            float now = Time.unscaledTime;
            if (now >= _nextCatch)
            {
                _nextCatch = now + Random.Range(40f, 95f);
                Catch();
            }
            if (now >= _nextChat)
            {
                _nextChat = now + Random.Range(80f, 180f);
                Game.SysLog($"[*{Names[Random.Range(0, Names.Length)]}*] : {Chats[Random.Range(0, Chats.Length)]}", "#e8e8e8");
            }
        }

        static void Catch()
        {
            var list = FishDatabase.DexOf(FieldMap.Id).Where(s => !s.Boss).ToList();
            if (list.Count == 0) return;
            float total = 0f;
            foreach (var s in list) total += Weight(s);
            float r = Random.value * total;
            FishSpecies pick = list[0];
            foreach (var s in list)
            {
                r -= Weight(s);
                if (r <= 0f) { pick = s; break; }
            }
            float len = Mathf.Lerp(pick.MinLen, pick.MaxLen, Mathf.Pow(Random.value, 2.4f));
            float kg = pick.WeightGrams(len) / 1000f;
            string name = Names[Random.Range(0, Names.Length)];
            Game.SysLog($"[{name}]님이[{pick.Name}] {len:0.00} cm, {kg:0.00} kg 을 잡았습니다." + (pick.IsTrophy(len) ? $"  <color=#ffd060>{pick.TrophyName}!</color>" : ""), "#8fe070");
        }

        static float Weight(FishSpecies s) => s.Rarity switch { 1 => 1f, 2 => 0.5f, 3 => 0.18f, 4 => 0.05f, _ => 0.01f };
    }
}
