using Fishing.Angling;
using Fishing.Core;
using Fishing.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fishing.UI
{
    /// <summary>모든 UI를 만들고 단축키를 처리한다.</summary>
    public class GameUI : MonoBehaviour
    {
        public HudView Hud { get; private set; }
        public FightHudView Fight { get; private set; }
        public ResultPanel Result { get; private set; }
        public KeepNetPanel KeepNet { get; private set; }
        public DexPanel Dex { get; private set; }
        public ShopPanel Shop { get; private set; }
        public QuestPanel Quest { get; private set; }
        public HelpPanel Help { get; private set; }
        public FishingAreaPanel Areas { get; private set; }
        public LobbyView LobbyHud { get; private set; }
        public AreaMapPanel AreaMap { get; private set; }

        UIPanel[] _panels;

        public bool BlockingPanelOpen
        {
            get
            {
                foreach (var p in _panels)
                    if (p.IsOpen) return true;
                return false;
            }
        }

        void Awake()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvas = UIFactory.CreateCanvas("UI", 10).transform;
            Hud = new HudView(canvas);
            Fight = new FightHudView(canvas);
            KeepNet = new KeepNetPanel(canvas);
            Dex = new DexPanel(canvas);
            Shop = new ShopPanel(canvas);
            Quest = new QuestPanel(canvas);
            Help = new HelpPanel(canvas);
            Result = new ResultPanel(canvas);
            LobbyHud = new LobbyView(canvas,
                openArea: () => OpenExclusive(Areas),
                openQuest: () => OpenExclusive(Quest),
                delivery: () => OpenExclusive(Shop),
                changeBase: () => Game.Toast("동남아·지중해 거점은 아직 준비 중입니다. 지금은 한국 낚시 조합만 열려 있어요.", ToastKind.Warn));
            Areas = new FishingAreaPanel(canvas);
            Areas.Enter = (area, channel) => EnterField(channel);
            AreaMap = new AreaMapPanel(canvas);
            _panels = new UIPanel[] { KeepNet, Dex, Shop, Quest, Help, Result, Areas, AreaMap };
            LobbyHud.Root.transform.SetAsFirstSibling();
        }

        void Start()
        {
            Game.Profile.Changed += RefreshOpenPanels;
            Game.Fishing.Casted += Hud.OnCasted;
        }

        void OnDestroy()
        {
            if (Game.Profile != null) Game.Profile.Changed -= RefreshOpenPanels;
            if (Hud != null) Game.Toasted -= Hud.Toast;
        }

        void RefreshOpenPanels()
        {
            foreach (var p in _panels)
                if (p.IsOpen && p != Result) p.Refresh();
        }

        void Update()
        {
            bool lobby = Game.Mode == GameMode.Lobby;
            LobbyHud.Root.SetActive(lobby && !BlockingPanelOpen);
            if (lobby) LobbyHud.Update();
            Hud.Update();
            Fight.Update();
            if (AreaMap.IsOpen) AreaMap.Tick();
            Result.Animate();
            HandleKeys();
        }

        /// <summary>한국 낚시 조합(대기실)으로 나간다.</summary>
        public void EnterLobby()
        {
            if (Game.Fishing.State == FishingState.Fighting || Game.Fishing.State == FishingState.Result)
            {
                Game.Toast("파이팅 중에는 나갈 수 없습니다.", ToastKind.Warn);
                return;
            }
            Game.Fishing.ForceIdle();
            foreach (var p in _panels) p.Close();
            Game.Mode = GameMode.Lobby;
            Game.CameraRig.SetFixed(Game.Lobby.CameraPos, Game.Lobby.CameraRot);
            Game.Save();
        }

        /// <summary>낚시터(한강)에 입장한다.</summary>
        public void EnterField(int channel)
        {
            foreach (var p in _panels) p.Close();
            Game.Channel = channel;
            Game.Mode = GameMode.Field;
            Game.CameraRig.ClearFixed();
            Game.Toast($"한강 CH.{channel}에 입장했습니다.", ToastKind.Good);
            if (Game.Profile.Data.totalCatches == 0 && !Game.SaveDisabled) OpenExclusive(Help);
        }

        public void OpenShop()
        {
            if (Shop.IsOpen) { Shop.Close(); return; }
            if (Game.Fishing.State != FishingState.Idle)
            {
                Game.Toast("낚시 중에는 상점을 이용할 수 없습니다.", ToastKind.Warn);
                return;
            }
            OpenExclusive(Shop);
        }

        public void ToggleTips()
        {
            var d = Game.Profile.Data;
            d.showTips = !d.showTips;
            Game.Toast(d.showTips ? "설명을 켰습니다. (U)" : "설명을 껐습니다. (U)");
        }

        public void SaveNow()
        {
            Game.Save();
            Game.Toast("저장했습니다.", ToastKind.Good);
        }

        void HandleKeys()
        {
            if (Result.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Result.ChooseByKey(true);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                foreach (var p in _panels) p.Close();
                return;
            }

            // 설명 끄기/켜기는 파이팅 중에도 된다 (원작: [설명 끄기 : U])
            if (Input.GetKeyDown(KeyCode.U)) ToggleTips();

            if (Game.Mode == GameMode.Lobby)
            {
                if (Input.GetKeyDown(KeyCode.L)) OpenExclusive(Quest);
                if (Input.GetKeyDown(KeyCode.P)) OpenExclusive(Dex);
                if (Input.GetKeyDown(KeyCode.Z)) OpenExclusive(KeepNet);
                if (Input.GetKeyDown(KeyCode.F1)) OpenExclusive(Help);
                return;
            }

            var state = Game.Fishing.State;
            if (state == FishingState.Fighting) return;
            bool idle = state == FishingState.Idle;

            // 원작 단축키: 퀘스트 L, 도감 P, 살림망 Z, 상점 F12, 지역 지도 M
            if (Input.GetKeyDown(KeyCode.F1)) OpenExclusive(Help);
            if (Input.GetKeyDown(KeyCode.M)) OpenExclusive(AreaMap);
            if (Input.GetKeyDown(KeyCode.P)) OpenExclusive(Dex);
            if (Input.GetKeyDown(KeyCode.L)) OpenExclusive(Quest);
            if (Input.GetKeyDown(KeyCode.Z)) OpenExclusive(KeepNet);
            if (Input.GetKeyDown(KeyCode.F12)) OpenShop();
            if (Input.GetKeyDown(KeyCode.F5)) SaveNow();

            if (idle && !BlockingPanelOpen)
                for (int i = 0; i < 6; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Hud.SelectBait(i);
        }

        public void OpenExclusive(UIPanel panel)
        {
            bool wasOpen = panel.IsOpen;
            foreach (var p in _panels)
                if (p != Result) p.Close();
            if (!wasOpen) panel.Open();
        }
    }
}
