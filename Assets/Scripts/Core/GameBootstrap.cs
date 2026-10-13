using Fishing.Angling;
using Fishing.Audio;
using Fishing.Dev;
using Fishing.PlayerSystems;
using Fishing.UI;
using Fishing.World;
using UnityEngine;

namespace Fishing.Core
{
    /// <summary>
    /// 게임 진입점. 씬에는 이 컴포넌트 하나만 있으면 되고, 나머지는 전부 코드로 만든다.
    /// 씬에 없어도 실행 시 자동으로 생성된다.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        const float AutoSaveInterval = 60f;
        float _nextAutoSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() == null)
                new GameObject("Game").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.shadowDistance = 90f;

            var devArgs = DevAutomation.Parse();
            Game.SaveDisabled = devArgs.Enabled;
            var data = devArgs.Enabled ? new SaveData() : SaveSystem.Load();
            Game.Profile = new PlayerProfile(data);
            Game.Quests = new QuestSystem(Game.Profile);

            Game.Clock = gameObject.AddComponent<GameClock>();
            Game.Clock.Set(data.clockDay, data.clockMinutes);
            Game.Weather = gameObject.AddComponent<WeatherSystem>();

            var world = new WorldBuilder();
            world.Build();
            gameObject.AddComponent<DayNightCycle>().Init(world.Lamps, world.LampBulbs);
            Game.Schools = gameObject.AddComponent<FishSchoolManager>();

            var playerGo = new GameObject("Player");
            Game.Player = playerGo.AddComponent<PlayerController>();
            var spawn = new Vector3(data.posX, data.posY, data.posZ);
            if (!HanRiverMap.IsWalkable(spawn)) spawn = HanRiverMap.Spawn;
            Game.Player.Teleport(spawn, data.yaw);

            Game.CameraRig = new GameObject("MainCamera").AddComponent<CameraRig>();
            Game.CameraRig.Yaw = data.yaw;
            Game.CameraRig.transform.position = spawn + new Vector3(0, 3, -6);

            Game.Sound = Game.CameraRig.gameObject.AddComponent<SoundFx>();
            Game.Fishing = gameObject.AddComponent<FishingController>();
            Game.UI = gameObject.AddComponent<GameUI>();

            Game.Profile.LeveledUp += level =>
            {
                Game.Toast($"레벨 업! Lv.{level}  체력이 회복되었습니다.", ToastKind.Great);
                Game.Sound.LevelUp();
            };

            if (devArgs.Enabled) gameObject.AddComponent<DevAutomation>().Init(devArgs);
        }

        void Start()
        {
            var q = Game.Quests.Current;
            if (Game.Profile.Data.totalCatches == 0 && !Game.SaveDisabled)
            {
                Game.UI.OpenExclusive(Game.UI.Help);
                Game.Toast("한강에 오신 걸 환영합니다! 설명은 U, 도움말은 F1입니다.", ToastKind.Great);
            }
            if (q != null) Game.Toast($"조합 퀘스트: {q.Title} - {q.GoalText}");
            _nextAutoSave = Time.time + AutoSaveInterval;
        }

        void Update()
        {
            // 체력은 시간이 흐르면 조금씩 줄어든다 (게임 1시간에 약 1)
            Game.Profile.ChangeStamina(-Time.deltaTime * GameClock.GameMinutesPerSecond / 60f);

            if (Time.time >= _nextAutoSave)
            {
                _nextAutoSave = Time.time + AutoSaveInterval;
                Game.Save();
            }
        }

        void OnApplicationQuit() => Game.Save();
    }
}
