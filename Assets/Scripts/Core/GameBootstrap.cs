using Fishing.Angling;
using Fishing.Audio;
using Fishing.Data;
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
            // 예전 저장 파일: Lv.5 이상인데 고무보트가 없으면 지급 (원작: 배는 레벨 5에서 얻는다)
            if (data.level >= 5 && !data.ownedGear.Contains("boat_rubber"))
            {
                data.ownedGear.Add("boat_rubber");
                if (string.IsNullOrEmpty(data.boatId)) { data.boatId = "boat_rubber"; data.boatFuel = 20f; }
            }

            Game.Clock = gameObject.AddComponent<GameClock>();
            Game.Clock.Set(data.clockDay, data.clockMinutes);
            Game.Weather = gameObject.AddComponent<WeatherSystem>();

            Game.DayNight = gameObject.AddComponent<DayNightCycle>();
            Game.DayNight.Init(new System.Collections.Generic.List<Light>(), new System.Collections.Generic.List<Renderer>());
            Game.Schools = gameObject.AddComponent<FishSchoolManager>();
            Game.Tournament = gameObject.AddComponent<TournamentSystem>();
            // 마지막으로 있던 낚시터 (입장 레벨이 안 되면 한강)
            var startMap = Maps.Get(data.mapId);
            if (data.level < FishingAreas.EntryLevel(startMap.Id)) startMap = Maps.HanRiver;
            MapLoader.Load(startMap, false);

            var playerGo = new GameObject("Player");
            Game.Player = playerGo.AddComponent<PlayerController>();
            Game.Boat = gameObject.AddComponent<BoatController>();
            var spawn = new Vector3(data.posX, data.posY, data.posZ);
            if (!FieldMap.IsWalkable(spawn)) spawn = FieldMap.Spawn;
            Game.Player.Teleport(spawn, data.yaw);

            Game.CameraRig = new GameObject("MainCamera").AddComponent<CameraRig>();
            Game.CameraRig.Yaw = data.yaw;
            Game.CameraRig.transform.position = spawn + new Vector3(0, 3, -6);
            PostFx.Attach(Game.CameraRig.Cam);

            Game.Sound = Game.CameraRig.gameObject.AddComponent<SoundFx>();
            Game.Fishing = gameObject.AddComponent<FishingController>();
            Game.UI = gameObject.AddComponent<GameUI>();
            Game.Lobby = LobbyRoom.Build();

            Game.Profile.LeveledUp += level =>
            {
                Game.Toast($"레벨 업! Lv.{level}  체력이 회복되었습니다.", ToastKind.Great);
                Game.Quests.CheckAchievements();
                foreach (var (lv, t) in AchievementDatabase.LevelTitles)
                    if (lv == level) Game.Toast($"새 칭호: 「{t}」 (퀘스트 창 [업적·칭호]에서 바꿀 수 있어요)", ToastKind.Good);
                Game.Sound.LevelUp();
                // 원작: 배는 레벨 5에서 얻는다. 조합이 고무보트를 지급하고 홍현 앞바다가 열린다
                if (level >= 5 && !Game.Profile.Owns("boat_rubber"))
                {
                    Game.Profile.Data.ownedGear.Add("boat_rubber");
                    if (string.IsNullOrEmpty(Game.Profile.Data.boatId)) Game.Profile.Equip(GearDatabase.GetGear("boat_rubber"));
                    Game.Toast("한국 낚시 조합이 고무보트를 지급했습니다! 물가에서 B로 꺼내 타세요.", ToastKind.Great);
                }
                if (level == FishingAreas.EntryLevel("honghyeon"))
                    Game.Toast("새 낚시터가 열렸습니다: 홍현 앞바다 (남해). 대기실 [낚시터 선택하기]에서 입장하세요.", ToastKind.Great);
            };

            if (devArgs.Enabled) gameObject.AddComponent<DevAutomation>().Init(devArgs);
        }

        void Start()
        {
            var q = Game.Quests.Current;
            Game.UI.EnterLobby();
            Game.Toast("한국 낚시 조합에 오신 걸 환영합니다! [낚시터 선택하기]에서 한강에 입장하세요.", ToastKind.Great);
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
