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
        public static TournamentSystem Tournament;
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
        public static DayNightCycle DayNight;
        /// <summary>지금 지어진 낚시터 월드 루트.</summary>
        public static UnityEngine.Transform World;
        public static GameMode Mode = GameMode.Field;
        public static int Channel = 1;

        /// <summary>true면 저장하지 않는다 (자동 스크린샷 등 개발용 실행).</summary>
        public static bool SaveDisabled;

        public static event Action<string, ToastKind> Toasted;

        public static void Toast(string message, ToastKind kind = ToastKind.Info) => Toasted?.Invoke(message, kind);

        /// <summary>원작 화면 가운데 흰 글씨 진행 알림 ("[퀘스트] 참돔 잡아서 가져오기 1/5", "[업적] … [완 료]"). 3초쯤 떠 있다가 사라진다.</summary>
        public static event Action<string> Noticed;
        public static void Notice(string message) => Noticed?.Invoke(message);

        /// <summary>원작 왼쪽 아래 시스템 채팅 줄 ("6000 EXP 를 획득했습니다.", "[X] 퀘스트를 수락하셨습니다."). color는 html 색 (#rrggbb).</summary>
        public static event Action<string, string> Logged;
        public static void SysLog(string message, string color = "#f0c040") => Logged?.Invoke(message, color);

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
