using System.Collections.Generic;
using Fishing.World;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 낚시터를 바꾼다: 이전 월드를 지우고 새 맵의 월드·가로등·어군을 만든 뒤 플레이어를 시작 위치로 옮긴다.
    /// 대기실(LobbyRoom)은 월드와 따로 있어서 그대로 남는다.
    /// </summary>
    public static class MapLoader
    {
        public static void Load(FieldMapDef map, bool movePlayer)
        {
            if (Game.World != null) Object.Destroy(Game.World.gameObject);
            FieldMap.Set(map);
            var lamps = new List<Light>();
            var bulbs = new List<Renderer>();
            WaterGlints.Clear();
            map.WaterY = map.BaseWaterY;
            Game.World = map.BuildWorld(lamps, bulbs);
            GrassField.Plant(Game.World, map.BaseWaterY, map.Id.GetHashCode());
            Fishing.Characters.ExtraNpcs.Populate(map, Game.World);
            FishTraps.SpawnFisher(map, Game.World);   // 통발 어부 (조합 연락관 옆)
            DeliveryKiosk.Place(map, Game.World);    // 납품소 (조합 연락관 옆, 원작 빨간 우체통 모양 기계)
            WorldBatcher.Batch(Game.World);
            WaterSurface.SetColors(map.WaterDeep, map.WaterShallow);
            WaterSurface.SetSeabed(map);
            // 가로등은 모두 물에 비친다 (다리·도시 불빛은 맵이 따로 넣는다)
            foreach (var l in lamps) if (l != null) WaterGlints.Add(l.transform.position, l.color, 1f);
            WaterGlints.Upload();
            Game.DayNight?.SetLampSet(lamps, bulbs);
            Game.Schools?.Reset(map.CreateSchools());
            if (Game.Profile != null) Game.Profile.Data.mapId = map.Id;
            if (movePlayer && Game.Player != null)
            {
                Game.Player.Teleport(map.Spawn, 0f);
                if (Game.CameraRig != null) Game.CameraRig.Yaw = 0f;
            }
        }
    }
}
