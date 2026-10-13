using System;
using Fishing.Audio;
using Fishing.Angling;
using Fishing.PlayerSystems;
using Fishing.UI;
using Fishing.World;

namespace Fishing.Core
{
    public enum ToastKind { Info, Good, Warn, Great }

    /// <summary>대기실(한국 낚시 조합)과 낚시터(필드).</summary>
    public enum GameMode { Lobby, Field }

    /// <summary>씬 전역에서 접근하는 시스템 참조 모음. GameBootstrap이 채운다.</summary>
    public static class Game
    {
        public static PlayerProfile Profile;
        public static QuestSystem Quests;
        public static GameClock Clock;
        public static WeatherSystem Weather;
        public static FishSchoolManager Schools;
        public static PlayerController Player;
        public static CameraRig CameraRig;
        public static FishingController Fishing;
        public static GameUI UI;
        public static SoundFx Sound;
        public static LobbyRoom Lobby;
        public static BoatController Boat;
        public static GameMode Mode = GameMode.Field;
        public static int Channel = 1;

        /// <summary>true면 저장하지 않는다 (자동 스크린샷 등 개발용 실행).</summary>
        public static bool SaveDisabled;

        public static event Action<string, ToastKind> Toasted;

        public static void Toast(string message, ToastKind kind = ToastKind.Info) => Toasted?.Invoke(message, kind);

        /// <summary>시계, 위치를 SaveData에 반영하고 파일로 저장한다.</summary>
        public static void Save()
        {
            if (Profile == null || SaveDisabled) return;
            var d = Profile.Data;
            if (Clock != null)
            {
                d.clockDay = Clock.Day;
                d.clockMinutes = Clock.Minutes;
            }
            if (Player != null)
            {
                // 배 위에서 저장하면 물 위가 아니라 배를 띄운 둔치로 저장한다
                var pos = Boat != null && Boat.Riding ? Boat.LandPos : Player.transform.position;
                d.posX = pos.x;
                d.posY = pos.y;
                d.posZ = pos.z;
                d.yaw = Player.Yaw;
            }
            SaveSystem.Save(d);
        }
    }
}
