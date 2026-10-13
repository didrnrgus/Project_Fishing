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
            Game.World = map.BuildWorld(lamps, bulbs);
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
