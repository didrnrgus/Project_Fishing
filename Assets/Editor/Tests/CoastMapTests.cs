using System.Linq;
using Fishing.Data;
using Fishing.World;
using NUnit.Framework;
using UnityEngine;

namespace Fishing.EditorTools.Tests
{
    /// <summary>
    /// 낚시터 정의 검사 (월드를 짓지 않고 판정 함수만). 해안 맵 틀(CoastMapDef)을 새로 만들거나 고칠 때 깨지기 쉬운 것들.
    /// </summary>
    public class CoastMapTests
    {
        static float[] TideLevels(FieldMapDef m) => m.TideAmplitude > 0f ? new[] { -1f, 0f, 1f } : new[] { 0f };

        [Test]
        public void SpawnShopAndUnionAreWalkableAtEveryTide()
        {
            foreach (var m in Maps.All.OfType<CoastMapDef>())
            {
                foreach (float level in TideLevels(m))
                {
                    m.WaterY = m.BaseWaterY + level * m.TideAmplitude;
                    Assert.IsTrue(m.IsWalkable(m.Spawn), $"{m.Id} 시작 위치가 물때 {level}에서 못 걷는 곳");
                    Assert.IsTrue(m.IsWalkable(m.ShopPos + new Vector3(0f, 0f, 4f)), $"{m.Id} 낚시점 앞이 물때 {level}에서 못 걷는 곳");
                    var union = m.S.Npcs.First(n => n.Id == "union");
                    Assert.IsTrue(m.IsWalkable(union.Pos), $"{m.Id} 조합 연락관 자리가 물때 {level}에서 못 걷는 곳");
                }
                m.WaterY = m.BaseWaterY;
            }
        }

        [Test]
        public void CoastSpecsAreComplete()
        {
            foreach (var m in Maps.All.OfType<CoastMapDef>())
            {
                Assert.AreEqual(8, m.S.Grid.GetLength(0), m.Id);
                Assert.AreEqual(8, m.S.Grid.GetLength(1), m.Id);
                Assert.IsTrue(m.S.Npcs.Count(n => n.Id == "union") == 1, $"{m.Id} 조합 연락관이 하나여야 한다");
                Assert.IsNotNull(m.S.ShopOwner, m.Id);
                Assert.IsTrue(m.CreateSchools().Count >= 4, $"{m.Id} 어군이 너무 적다");
                Assert.IsNotNull(FishingAreas.All.FirstOrDefault(a => a.MapId == m.Id && a.Open), $"{m.Id}가 낚시터 목록에 열려 있지 않다");
                // 물: 격자 가운데 먼 줄은 바다여야 한다 (강은 맞은편 둑이 있으니 가운데 E4)
                int row = m.S.FarShoreZ > 0f ? 3 : 6;
                Assert.IsTrue(m.IsWater(m.CellCenter(4, row)), $"{m.Id} E{row + 1}이 물이 아니다");
                if (m.S.FarShoreZ > 0f) Assert.IsTrue(m.IsWalkable(new Vector3(0f, 0f, m.FarShoreAt(0f) + 20f)), $"{m.Id} 맞은편 둑을 걸을 수 없다");
            }
        }

        /// <summary>어종마다 사는 낚시터에서 실제로 낚을 수 있다: 서식지가 격자에 있고(어군 전용은 어군에 있고), 그곳에서 파는 미끼로 문다.</summary>
        [Test]
        public void EverySpeciesIsCatchableWhereItLives()
        {
            foreach (var s in FishDatabase.All.Where(s => !s.IsJunk && s.Maps != null))
            foreach (var id in s.Maps)
            {
                var m = Maps.Get(id);
                Assert.AreEqual(id, m.Id, $"{s.Name}: 없는 낚시터 {id}");
                if (s.SchoolOnly)
                    Assert.IsTrue(m.CreateSchools().Any(sc => sc.SpeciesIds.Contains(s.Id)), $"{s.Name}: {id}에 어군이 없다");
                else
                {
                    bool habitat = false;
                    for (int r = 0; r < m.Rows && !habitat; r++)
                    for (int c = 0; c < m.Cols && !habitat; c++)
                        habitat = s.Habitats.Contains(m.HabitatAt(c, r));
                    Assert.IsTrue(habitat, $"{s.Name}: {id} 격자에 서식지가 없다");
                }
                bool bait = s.Baits.Contains(BaitKind.CutBait) ||
                            GearDatabase.Baits.Any(b => !b.NotSold && b.SoldIn(id) && s.Baits.Contains(b.Kind));
                Assert.IsTrue(bait, $"{s.Name}: {id}에서 파는 미끼로는 안 문다");
            }
        }

        [Test]
        public void FishFarmWalkwaysAndPens()
        {
            var r = new Rect(0f, 0f, 48f, 36.4f);
            Assert.IsTrue(CoastMapDef.FarmWalkway(r, 0.5f, 10f), "왼쪽 테두리는 통로");
            Assert.IsTrue(CoastMapDef.FarmWalkway(r, 20f, 0.5f), "아래 테두리는 통로");
            Assert.IsTrue(CoastMapDef.FarmWalkway(r, 47.5f, 20f), "오른쪽 테두리는 통로");
            Assert.IsFalse(CoastMapDef.FarmWalkway(r, 1.6f + 5f, 1.6f + 5f), "그물 칸 가운데는 물");
        }

        [Test]
        public void SandBarOnlyAtLowTide()
        {
            var m = (CoastMapDef)Maps.Get("yeongheung");
            var isle = m.S.Islets.First(i => i.SandBar);
            var mid = new Vector3(isle.Center.x, 0f, (m.ShoreAt(isle.Center.x) + isle.Center.z) * 0.5f);
            m.WaterY = m.BaseWaterY - m.TideAmplitude;
            Assert.IsTrue(m.IsWalkable(mid), "간조에는 모래톱을 걷는다");
            m.WaterY = m.BaseWaterY + m.TideAmplitude;
            Assert.IsFalse(m.IsWalkable(mid), "만조에는 모래톱이 잠긴다");
            m.WaterY = m.BaseWaterY;
        }
    }
}
