using System.Collections.Generic;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.Angling
{
    public struct BiteContext
    {
        public Habitat Habitat;
        public BaitKind Bait;
        public TimeBand Band;
        public Weather Weather;
        public FishSchool School;
        /// <summary>지금 낚시터 (null이면 모든 어종, 밸런스 시뮬레이션용).</summary>
        public string MapId;
        /// <summary>미끼 효과 배율 (해체 미끼가 높다). 0이면 1로 본다.</summary>
        public float BaitPotency;
        /// <summary>의상 대물 확률 (0.05 = 5%).</summary>
        public float BigFishBonus;
        /// <summary>겨울 얼음 구멍 낚시 (빙어가 주인공, 다른 고기는 드물다).</summary>
        public bool IceFishing;
        /// <summary>찌가 먼바다 포인트에 있다 (<see cref="FishSpecies.FarSeaOnly"/>).</summary>
        public bool FarSea;
        /// <summary>계절 (기본값 봄). 밸런스 시뮬레이션은 계절 영향을 끈다.</summary>
        public Season Season;
        public bool UseSeason;
    }

    public struct BiteResult
    {
        public FishSpecies Species;
        public float Length;
        public bool IsTrophy => Species.IsTrophy(Length);
    }

    /// <summary>찌가 떨어진 곳의 서식지·미끼·시간·날씨·어군으로 어떤 고기가 물지 정한다.</summary>
    public static class BiteSelector
    {
        static readonly float[] RarityWeight = { 0.25f, 1f, 0.55f, 0.25f, 0.1f, 0.025f };
        /// <summary>물속 그림자 고기의 무게 (희귀도 대신). 맞는 미끼·시간이면 흔한 고기 셋만큼 = 입질의 절반쯤.</summary>
        public const float ShadowWeight = 3f;

        public static float Weight(FishSpecies s, BiteContext ctx)
        {
            if (!s.LivesIn(ctx.MapId)) return 0f;
            float w = RarityWeight[Mathf.Clamp(s.Rarity, 0, RarityWeight.Length - 1)];
            if (s.IsJunk) return w * s.JunkWeight;
            // 대물 퀘스트 고기는 그 어군 안에서만 문다
            if (s.SchoolOnly && (ctx.School == null || !ctx.School.Contains(s.Id))) return 0f;
            // 물속 그림자 고기는 바로 눈앞에 있으니 희귀도·서식지와 상관없이 문다 (미끼·시간은 따진다)
            bool shadow = ctx.School != null && ctx.School.Shadow && ctx.School.Contains(s.Id);
            if (shadow) w = ShadowWeight;
            w *= shadow || Contains(s.Habitats, ctx.Habitat) ? 1f : 0.12f;
            w *= Contains(s.Baits, ctx.Bait) ? Mathf.Max(1f, ctx.BaitPotency) : 0.15f;
            w *= Contains(s.Times, ctx.Band) ? 1f : 0.3f;
            w *= Contains(s.Weathers, ctx.Weather) ? 1.3f : 1f;
            if (!shadow && ctx.School != null && ctx.School.Contains(s.Id)) w *= ctx.School.BiteBoost;
            if (ctx.UseSeason) w *= Seasons.BiteFactor(s, ctx.Season);
            if (s.FarSeaOnly && !ctx.FarSea) return 0f;
            // 빙어는 얼음 구멍에서만, 얼음 구멍에서는 빙어가 주인공
            if (s.Id == "smelt") w *= ctx.IceFishing ? 8f : 0f;
            else if (ctx.IceFishing) w *= 0.2f;
            return w;
        }

        /// <summary>
        /// 잡동사니가 걸릴 확률. 예전에는 잡동사니 무게를 물고기 무게에 그냥 더해서, 희귀 어종이 많은 바다·해외 맵이나
        /// 서식지·시간이 안 맞을 때 입질의 25~80%가 깡통·운동화였다. 이제는 비율로 정한다:
        /// 맞는 고기가 넉넉하면(무게 합 ≥ 1 = 흔한 고기 한 종이 다 맞음) 4%, 맞는 고기가 거의 없으면 10%까지. 어군 안에서는 절반.
        /// </summary>
        public static float JunkChance(float fishWeight, BiteContext ctx)
        {
            float p = Mathf.Lerp(0.10f, 0.04f, Mathf.Clamp01(fishWeight));
            return ctx.School != null ? p * 0.5f : p;
        }

        public static BiteResult Pick(BiteContext ctx)
        {
            var fish = new List<(FishSpecies s, float w)>();
            var junk = new List<(FishSpecies s, float w)>();
            float fishTotal = 0f, junkTotal = 0f;
            foreach (var s in FishDatabase.All)
            {
                float w = Weight(s, ctx);
                if (w <= 0f) continue;
                if (s.IsJunk) { junk.Add((s, w)); junkTotal += w; }
                else { fish.Add((s, w)); fishTotal += w; }
            }

            bool pickJunk = fish.Count == 0 || (junk.Count > 0 && Random.value < JunkChance(fishTotal, ctx));
            var list = pickJunk ? junk : fish;
            float r = Random.value * (pickJunk ? junkTotal : fishTotal);
            var picked = list[list.Count - 1].s;
            foreach (var (s, w) in list)
            {
                r -= w;
                if (r <= 0f) { picked = s; break; }
            }
            return new BiteResult { Species = picked, Length = RollLength(picked, ctx) };
        }

        /// <summary>대부분은 작고, 가끔 대물이 나온다. 어군에서는 큰 고기 확률이 조금 오른다.</summary>
        public static float RollLength(FishSpecies s, BiteContext ctx)
        {
            if (s.MinLen >= s.MaxLen) return s.MinLen;
            float skew = ctx.School != null && ctx.School.Contains(s.Id) ? ctx.School.LengthSkew : 2.2f;
            // 좋은 미끼(해체 미끼)는 큰 고기가 잘 문다
            if (ctx.BaitPotency > 1f && Contains(s.Baits, ctx.Bait)) skew = Mathf.Max(1.2f, skew - (ctx.BaitPotency - 1f) * 0.8f);
            if (ctx.BigFishBonus > 0f) skew = Mathf.Max(1.1f, skew * (1f - ctx.BigFishBonus * 2.5f));
            float t = Mathf.Pow(Random.value, skew);
            return Mathf.Round(Mathf.Lerp(s.MinLen, s.MaxLen, t) * 10f) / 10f;
        }

        /// <summary>입질까지 기다리는 시간(초). 어군 안이면 빠르고, 맞는 미끼가 없으면 느리다.</summary>
        public static float WaitSeconds(BiteContext ctx)
        {
            float t = Random.Range(5f, 14f);
            if (ctx.School != null) t *= ctx.School.WaitFactor;

            bool anyMatch = false;
            foreach (var s in FishDatabase.All)
                if (!s.IsJunk && s.LivesIn(ctx.MapId) && Contains(s.Habitats, ctx.Habitat) && Contains(s.Baits, ctx.Bait) && Contains(s.Times, ctx.Band))
                {
                    anyMatch = true;
                    break;
                }
            if (!anyMatch) t *= 1.6f;
            return t;
        }

        static bool Contains<T>(T[] arr, T v) => arr != null && System.Array.IndexOf(arr, v) >= 0;
    }
}
