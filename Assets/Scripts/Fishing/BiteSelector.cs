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

        public static float Weight(FishSpecies s, BiteContext ctx)
        {
            if (!s.LivesIn(ctx.MapId)) return 0f;
            float w = RarityWeight[Mathf.Clamp(s.Rarity, 0, RarityWeight.Length - 1)];
            if (s.IsJunk) return w * s.JunkWeight;
            // 대물 퀘스트 고기는 그 어군 안에서만 문다
            if (s.SchoolOnly && (ctx.School == null || !ctx.School.Contains(s.Id))) return 0f;
            w *= Contains(s.Habitats, ctx.Habitat) ? 1f : 0.12f;
            w *= Contains(s.Baits, ctx.Bait) ? 1f : 0.15f;
            w *= Contains(s.Times, ctx.Band) ? 1f : 0.3f;
            w *= Contains(s.Weathers, ctx.Weather) ? 1.3f : 1f;
            if (ctx.School != null && ctx.School.Contains(s.Id)) w *= ctx.School.BiteBoost;
            return w;
        }

        public static BiteResult Pick(BiteContext ctx)
        {
            var weights = new List<(FishSpecies s, float w)>();
            float total = 0f;
            foreach (var s in FishDatabase.All)
            {
                float w = Weight(s, ctx);
                if (w <= 0f) continue;
                weights.Add((s, w));
                total += w;
            }

            float r = Random.value * total;
            var picked = weights[weights.Count - 1].s;
            foreach (var (s, w) in weights)
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

        static bool Contains<T>(T[] arr, T v) => System.Array.IndexOf(arr, v) >= 0;
    }
}
