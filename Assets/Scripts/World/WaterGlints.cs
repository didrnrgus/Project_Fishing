using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 밤 물 위에 길게 비치는 불빛 (가로등·다리 조명·건너편 도시 불빛). 로드맵 "밤 강물 다리·도시 불빛 반사".
    /// 맵을 지을 때 광원 위치를 <see cref="Add"/>로 모아 두면, Fishing/Water 셰이더가 반사 방향과 광원 방향의 차이로
    /// 가로는 좁고 세로(보는 쪽)로 긴 빛 기둥을 그린다. 잔물결 노멀 때문에 기둥이 자글자글 흔들린다.
    /// 밤 정도(<see cref="SetNight"/>)는 DayNightCycle이 가로등을 켜고 끄는 값과 같게 준다.
    /// </summary>
    public static class WaterGlints
    {
        public const int Max = 48;   // Water.shader의 배열 크기와 같아야 한다

        static readonly List<Vector4> Pos = new List<Vector4>();
        static readonly List<Vector4> Col = new List<Vector4>();
        static readonly Vector4[] PosBuf = new Vector4[Max];
        static readonly Vector4[] ColBuf = new Vector4[Max];

        /// <summary>맵을 새로 지을 때 비운다 (MapLoader).</summary>
        public static void Clear()
        {
            Pos.Clear();
            Col.Clear();
            Upload();
        }

        /// <summary>광원 하나. strength = 밝기 배율 (가로등 1, 다리 조명 0.7, 먼 도시 불빛 0.5 …).</summary>
        public static void Add(Vector3 pos, Color color, float strength = 1f)
        {
            if (Pos.Count >= Max) return;
            Pos.Add(new Vector4(pos.x, pos.y, pos.z, 1f));
            Col.Add(new Vector4(color.r, color.g, color.b, strength));
        }

        /// <summary>모은 광원을 셰이더 전역 배열로 보낸다 (맵 짓기가 끝나면 한 번).</summary>
        public static void Upload()
        {
            for (int i = 0; i < Max; i++)
            {
                PosBuf[i] = i < Pos.Count ? Pos[i] : Vector4.zero;
                ColBuf[i] = i < Col.Count ? Col[i] : Vector4.zero;
            }
            Shader.SetGlobalVectorArray("_GlintPos", PosBuf);
            Shader.SetGlobalVectorArray("_GlintCol", ColBuf);
            Shader.SetGlobalFloat("_GlintCount", Pos.Count);
        }

        /// <summary>0(낮) ~ 1(밤, 불이 켜짐).</summary>
        public static void SetNight(float night) => Shader.SetGlobalFloat("_GlintNight", Mathf.Clamp01(night));
    }
}
