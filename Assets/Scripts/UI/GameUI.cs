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
        public DialogPanel Dialog { get; private set; }
        public UpgradePanel Upgrade { get; private set; }
        public InventoryPanel Inventory { get; private set; }
        public CostumePanel Costume { get; private set; }
        /// <summary>F로 말을 걸 수 있는 가장 가까운 NPC.</summary>
        public Fishing.Characters.NpcAngler NearbyNpc { get; private set; }

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

            PortraitCamera.Create();
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
            Areas.Enter = (area, channel) => EnterField(area, channel);
            AreaMap = new AreaMapPanel(canvas);
            Dialog = new DialogPanel(canvas);
            Upgrade = new UpgradePanel(canvas);
            Inventory = new InventoryPanel(canvas);
            Costume = new CostumePanel(canvas);
            _panels = new UIPanel[] { KeepNet, Dex, Shop, Quest, Help, Result, Areas, AreaMap, Dialog, Upgrade, Inventory, Costume };
            LobbyHud.Root.transform.SetAsFirstSibling();
        }

        void Start()
        {
            Game.Profile.Changed += RefreshOpenPanels;
            // 의상을 바꾸면 필드 캐릭터·대기실 캐릭터를 다시 입힌다
            Game.Profile.LookChanged += () =>
            {
                Game.Player.ApplyLook();
                if (Game.Lobby != null) Game.Lobby.RefreshPlayer();
            };
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
            NearbyNpc = FindNearbyNpc();
            Game.Player.NameTagVisible = !Dialog.IsOpen;
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
            Game.Boat.StowToLand();
            foreach (var p in _panels) p.Close();
            Game.Mode = GameMode.Lobby;
            Game.Player.CollapseRod();
            Game.CameraRig.SetFixed(Game.Lobby.CameraPos, Game.Lobby.CameraRot);
            Game.Save();
        }

        /// <summary>낚시터에 입장한다. 다른 낚시터면 월드를 새로 짓는다.</summary>
        public void EnterField(Fishing.Data.FishingArea area, int channel)
        {
            var map = Maps.Get(area.MapId);
            if (map.Id != FieldMap.Id) MapLoader.Load(map, true);
            EnterField(channel);
        }

        /// <summary>지금 낚시터에 입장한다.</summary>
        public void EnterField(int channel)
        {
            foreach (var p in _panels) p.Close();
            Game.Channel = channel;
            Game.Mode = GameMode.Field;
            Game.CameraRig.ClearFixed();
            Game.Player.ExtendRod();
            Game.Toast($"{FieldMap.ShortName} CH.{channel}에 입장했습니다.", ToastKind.Good);
            var d = Game.Profile.Data;
            if (!d.visitedMaps.Contains(FieldMap.Id))
            {
                d.visitedMaps.Add(FieldMap.Id);
                if (FieldMap.IsSea)
                {
                    // 처음 온 바다: 어촌계 환영 선물로 바다 미끼
                    Game.Profile.AddItem("bait_krill", 20);
                    Game.Profile.AddItem("bait_seaworm", 20);
                    Game.Toast($"{FieldMap.ShortName}에 처음 오셨군요! 어촌계 환영 선물: 크릴 20, 청갯지렁이 20", ToastKind.Great);
                }
            }
            // 이 낚시터에서 쓰지 않는 미끼를 들고 있으면 맞는 미끼로 바꾼다
            if (!Game.Profile.Bait.SoldIn(FieldMap.Id))
            {
                foreach (var b in HudView.MapBaits())
                    if (Game.Profile.Count(b.Id) > 0) { Game.Profile.SelectBait(b.Id); break; }
            }
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
                if (Input.GetKeyDown(KeyCode.I)) OpenExclusive(Inventory);
            if (Input.GetKeyDown(KeyCode.C)) OpenExclusive(Costume);
                if (Input.GetKeyDown(KeyCode.C)) OpenExclusive(Costume);
                if (Input.GetKeyDown(KeyCode.F1)) OpenExclusive(Help);
                return;
            }

            var state = Game.Fishing.State;
            if (state == FishingState.Fighting) return;
            bool idle = state == FishingState.Idle;

            // 원작 단축키: 퀘스트 L, 도감 P, 살림망 Z, 상점 F12, 지역 지도 M
            if (Input.GetKeyDown(KeyCode.F1)) OpenExclusive(Help);
            if (Input.GetKeyDown(KeyCode.M)) OpenExclusive(AreaMap);
            if (Input.GetKeyDown(KeyCode.I)) OpenExclusive(Inventory);
            if (Input.GetKeyDown(KeyCode.F) && NearbyNpc != null && idle && !BlockingPanelOpen) TalkTo(NearbyNpc);
            if (Input.GetKeyDown(KeyCode.B) && !BlockingPanelOpen) Game.Boat.Toggle();
            if (Input.GetKeyDown(KeyCode.P)) OpenExclusive(Dex);
            if (Input.GetKeyDown(KeyCode.L)) OpenExclusive(Quest);
            if (Input.GetKeyDown(KeyCode.Z)) OpenExclusive(KeepNet);
            if (Input.GetKeyDown(KeyCode.F12)) OpenShop();
            if (Input.GetKeyDown(KeyCode.F5)) SaveNow();

            if (idle && !BlockingPanelOpen)
                for (int i = 0; i < 6; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Hud.SelectBait(i);
        }

        Fishing.Characters.NpcAngler FindNearbyNpc()
        {
            if (Game.Mode != GameMode.Field || Game.Player.OnBoat || Game.Fishing.State != FishingState.Idle) return null;
            var p = Game.Player.transform.position;
            Fishing.Characters.NpcAngler best = null;
            float bestD = 3.5f * 3.5f;
            foreach (var npc in Fishing.Characters.NpcAngler.Talkable)
            {
                var d = npc.transform.position - p;
                d.y = 0f;
                if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = npc; }
            }
            return best;
        }

        public void TalkTo(Fishing.Characters.NpcAngler npc)
        {
            foreach (var p in _panels) if (p != Result) p.Close();
            // 어깨 너머로 NPC가 보이게 카메라를 돌린다
            var d = npc.transform.position - Game.Player.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
            {
                Game.CameraRig.Yaw = Quaternion.LookRotation(d).eulerAngles.y;
                Game.CameraRig.Distance = Mathf.Min(Game.CameraRig.Distance, 4.5f);
                Game.Player.FaceYaw(Game.CameraRig.Yaw);
            }
            Dialog.Open(npc);
        }

        /// <summary>한국어 조사: 받침이 있으면 withFinal(과/을/은), 없으면 withoutFinal(와/를/는).</summary>
        public static string Josa(string word, string withFinal, string withoutFinal)
        {
            if (string.IsNullOrEmpty(word)) return withoutFinal;
            char c = word[word.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return withoutFinal;
            return (c - 0xAC00) % 28 != 0 ? withFinal : withoutFinal;
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
