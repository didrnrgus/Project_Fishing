using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 계절이 화면에 보이는 것: 겨울이면 한강 남쪽 둔치 앞 강물이 얼고(걸어 들어가 얼음 구멍 낚시), 둔치에 눈이 덮인다.
    /// 계절이 바뀌거나 맵이 바뀌면 다시 맞춘다. 규칙(달·계절·어종 배율)은 <see cref="Seasons"/>.
    /// </summary>
    public class SeasonFx : MonoBehaviour
    {
        /// <summary>한강 얼음 범위 (남쪽 둔치에서 강 가운데 쪽으로 35m).</summary>
        public static readonly Rect HanIce = new Rect(-150f, HanRiverMap.RiverMinZ, 300f, 35f);

        static SeasonFx _instance;
        Season _applied = (Season)(-1);
        string _appliedMap;
        Transform _world;
        GameObject _ice;
        readonly List<GameObject> _holes = new List<GameObject>();
        readonly List<(Renderer r, Color baseColor)> _snowGround = new List<(Renderer, Color)>();

        public static SeasonFx Ensure()
        {
            if (_instance == null) _instance = new GameObject("SeasonFx").AddComponent<SeasonFx>();
            return _instance;
        }

        /// <summary>겨울 한강 얼음 위인지 (걸을 수 있고, 배·찌는 못 간다).</summary>
        public static bool OnIce(Vector3 p) =>
            _instance != null && _instance._ice != null && FieldMap.Id == "hangang" && HanIce.Contains(new Vector2(p.x, p.z))
            && !HanRiverMap.OnBridgePier(p);

        void Update()
        {
            if (Game.Clock == null) return;
            var season = Seasons.Current;
            if (season == _applied && FieldMap.Id == _appliedMap && Game.World == _world) return;
            bool changed = _applied != season && (int)_applied >= 0;
            _applied = season;
            _appliedMap = FieldMap.Id;
            _world = Game.World;
            Apply(season);
            if (changed)
                Game.Toast($"계절이 바뀌었습니다: {Seasons.Name(season)}" + (season == Season.Winter ? " — 한강이 얼었습니다! 얼음 위에서 빙어낚시를 해 보세요." : ""), ToastKind.Great);
        }

        void Apply(Season season)
        {
            foreach (var h in _holes) if (h != null) Destroy(h);
            _holes.Clear();
            if (_ice != null) Destroy(_ice);
            _ice = null;
            // 예전 맵의 눈 덮인 땅은 지워진 맵이라 비우기만
            foreach (var (r, c) in _snowGround) if (r != null) r.material.color = c;
            _snowGround.Clear();
            if (Game.World == null || FieldMap.Id != "hangang") return;

            bool winter = season == Season.Winter;
            if (winter)
            {
                // 얼음판: 수면 바로 위, 콜라이더가 있어 걸을 수 있다
                var mat = Mats.Lit(new Color(0.82f, 0.9f, 0.96f), 0.82f);
                _ice = Mats.Prim(PrimitiveType.Cube, "RiverIce", Game.World, new Vector3(HanIce.center.x, HanRiverMap.WaterY + 0.05f, HanIce.center.y),
                    new Vector3(HanIce.width, 0.3f, HanIce.height), mat);
                _ice.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // 얼음 위 눈 무늬와 금
                var snow = Mats.Lit(new Color(0.95f, 0.97f, 1f), 0.2f);
                var rng = new System.Random(77);
                for (int i = 0; i < 40; i++)
                {
                    float x = HanIce.xMin + (float)rng.NextDouble() * HanIce.width, z = HanIce.yMin + (float)rng.NextDouble() * HanIce.height;
                    float s = 2f + (float)rng.NextDouble() * 6f;
                    Mats.Prim(PrimitiveType.Cylinder, "SnowPatch", _ice.transform.parent, new Vector3(x, HanRiverMap.WaterY + 0.205f, z),
                        new Vector3(s, 0.005f, s * 0.6f), snow, false, Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f));
                }
            }
            // 둔치에 눈
            foreach (var name in new[] { "SouthBank", "NorthBank", "Walkway" })
            {
                var t = Game.World.Find(name);
                if (t == null) continue;
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                var c = r.sharedMaterial.color;
                _snowGround.Add((r, c));
                if (winter) r.material.color = Color.Lerp(c, new Color(1.25f, 1.28f, 1.32f), 0.75f);
            }
        }

        /// <summary>얼음 구멍 (낚시할 때 뚫는다).</summary>
        public static void MakeHole(Vector3 at)
        {
            if (_instance == null || _instance._ice == null) return;
            var rim = Mats.Prim(PrimitiveType.Cylinder, "IceHoleRim", _instance._ice.transform.parent, new Vector3(at.x, HanRiverMap.WaterY + 0.2f, at.z),
                new Vector3(1.0f, 0.02f, 1.0f), Mats.Lit(new Color(0.96f, 0.98f, 1f), 0.3f), false);
            var hole = Mats.Prim(PrimitiveType.Cylinder, "IceHole", rim.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.65f, 1f, 0.65f),
                Mats.Lit(new Color(0.04f, 0.1f, 0.14f), 0.95f), false);
            _instance._holes.Add(rim);
            if (_instance._holes.Count > 12)
            {
                Destroy(_instance._holes[0]);
                _instance._holes.RemoveAt(0);
            }
        }
    }
}
