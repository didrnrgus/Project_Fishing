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
            Settings.Load();   // 기기 설정 (음량·감도·화면·키 배치)
            Rumble.Ensure();

            var devArgs = DevAutomation.Parse();
            Game.SaveDisabled = devArgs.Enabled;
            var data = devArgs.Enabled ? new SaveData() : SaveSystem.Load();
            Game.Profile = new PlayerProfile(data);
            Game.Quests = new QuestSystem(Game.Profile);
            // 시험 모드: 레벨 제한 없이 모든 맵 + 가장 좋은 장비 (자동 촬영은 보통 게임 그대로)
            if (devArgs.Enabled) TestMode.UnlockAll = false;
            else if (TestMode.UnlockAll) TestMode.GrantBestGear(Game.Profile);
            // 예전 저장 파일: Lv.5 이상인데 고무보트가 없으면 지급 (원작: 배는 레벨 5에서 얻는다)
            if (data.level >= 5 && !data.ownedGear.Contains("boat_rubber"))
            {
                data.ownedGear.Add("boat_rubber");
                if (string.IsNullOrEmpty(data.boatId)) { data.boatId = "boat_rubber"; data.boatFuel = 20f; }
            }

            // 예전 저장 파일 (lookVersion 0): 주인공 기본을 여자로 (2026-10-21). 이후에 캐릭터 창에서 남자로 고르면 그대로 남는다
            if (data.lookVersion < 1) { data.vrmFemale = true; data.lookVersion = 1; }

            Game.Clock = gameObject.AddComponent<GameClock>();
            Game.Clock.Set(data.clockDay, data.clockMinutes);
            Game.Weather = gameObject.AddComponent<WeatherSystem>();

            Game.DayNight = gameObject.AddComponent<DayNightCycle>();
            Game.DayNight.Init(new System.Collections.Generic.List<Light>(), new System.Collections.Generic.List<Renderer>());
            Game.Schools = gameObject.AddComponent<FishSchoolManager>();
            Game.Tournament = gameObject.AddComponent<TournamentSystem>();
            gameObject.AddComponent<Tide>();
            gameObject.AddComponent<BioluminescenceFx>();
            SeasonFx.Ensure();
            // 마지막으로 있던 낚시터 (입장 레벨이 안 되면 한강)
            var startMap = Maps.Get(data.mapId);
            if (!TestMode.LevelOk(FishingAreas.EntryLevel(startMap.Id))) startMap = Maps.HanRiver;
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
            var fx = PostFx.Attach(Game.CameraRig.Cam);
            if (fx != null) fx.Enabled = Settings.PostFx;

            Game.Sound = Game.CameraRig.gameObject.AddComponent<SoundFx>();
            Game.Fishing = gameObject.AddComponent<FishingController>();
            // 따라다니는 친구 윤슬 (두뇌는 UI보다 먼저: 대화창이 두뇌 이벤트를 받는다). 자동 촬영은 -autoshotBuddy일 때만 (다른 장면을 가리지 않게)
            bool buddy = !devArgs.Enabled || !string.IsNullOrEmpty(devArgs.Buddy);
            if (buddy) Fishing.Buddy.BuddyBrain.Ensure(gameObject);
            Fishing.Buddy.BuddyMcpServer.Ensure(gameObject);
            Game.UI = gameObject.AddComponent<GameUI>();
            if (buddy) Fishing.Buddy.BuddyBody.Spawn();
            Game.Lobby = LobbyRoom.Build();

            Game.Profile.LeveledUp += level =>
            {
                Game.Toast($"레벨 업! Lv.{level}  체력이 회복되었습니다.", ToastKind.Great);
                Game.Profile.Log($"레벨 업 — Lv.{level}");
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
                foreach (var msg in Game.Profile.GrantLevelRewards()) Game.Toast(msg + " (가방·장비에서 확인)", ToastKind.Great);
                foreach (var area in FishingAreas.All)
                    if (area.Open && area.Level == level && area.Level > 1)
                        Game.Toast($"새 낚시터가 열렸습니다: {area.Name}. 대기실 [낚시터 선택하기]에서 입장하세요.", ToastKind.Great);
            };

            // 인트로 (Unity 스플래시 → 제쓰로네 게임공방 → 청풍명월 타이틀). 끝나고 대기실이 보일 때 환영 알림을 띄운다
            IntroSequence.Play(Welcome);
            if (devArgs.Enabled) gameObject.AddComponent<DevAutomation>().Init(devArgs);
        }

        void Start()
        {
            Game.UI.EnterLobby();
            _nextAutoSave = Time.time + AutoSaveInterval;
            Bingo.EnsureCard();
        }

        /// <summary>인트로가 끝나 대기실이 보일 때: 환영·퀘스트·레벨 보상·출석 알림.</summary>
        void Welcome()
        {
            var q = Game.Quests.Current;
            Game.Toast($"한국 낚시 조합에 오신 걸 환영합니다! [낚시터 선택하기]에서 {Fishing.World.FieldMap.ShortName}에 입장하세요.", ToastKind.Great);
            if (q != null)
            {
                // 원작: 퀘스트는 NPC에게 수락한다. 받을 이야기 퀘스트가 있으면 누구에게 받는지 알려 준다
                var giver = Fishing.Data.QuestNpcs.Get(q.Giver);
                if (Game.Quests.IsActive(q)) Game.Toast($"이야기 퀘스트: {q.Title} - {Game.Quests.ObjectiveLine(q)}");
                else Game.Toast($"새 이야기 퀘스트 [{q.Title}] — {giver?.Where ?? "조합"}에게서 받을 수 있습니다." +
                                (giver != null && giver.InLobby ? " 오른쪽 아래 [퀘스트]를 눌러 보세요." : ""));
            }
            foreach (var msg in Game.Profile.GrantLevelRewards()) Game.Toast(msg, ToastKind.Good);
            // 출석 체크 (실제 날짜로 하루 한 번)
            var attend = Attendance.CheckIn();
            if (attend != null) Game.Toast(attend + "  · 퀘스트 창(L) [빙고·출석]", ToastKind.Great);
        }

        const float StaminaDrainPerSecond = 2f / 60f;

        void Update()
        {
            // 체력은 시간이 흐르면 조금씩 줄어든다 (현실 30초에 약 1 — 시계를 늦춰도 현실 시간 기준은 그대로)
            // 비를 맞으면 빨리 지치고, 맑으면 음식 회복이 빠르다 (원작 날씨 효과)
            Game.Profile.ChangeStamina(-Time.deltaTime * StaminaDrainPerSecond * Game.Weather.DrainFactor);
            Game.Profile.TickRegen(Game.Clock.TotalMinutes, Time.deltaTime * GameClock.GameMinutesPerSecond * Game.Weather.RegenFactor);
            Game.Profile.TickFocus(Time.unscaledDeltaTime);

            if (Time.time >= _nextAutoSave)
            {
                _nextAutoSave = Time.time + AutoSaveInterval;
                Game.Save();
            }
        }

        void OnApplicationQuit() => Game.Save();
    }
}
