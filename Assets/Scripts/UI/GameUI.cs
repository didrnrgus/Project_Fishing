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
        public SystemLogView SysLog { get; private set; }
        public LoadingScreen Loading { get; private set; }
        public EscMenuPanel EscMenu { get; private set; }
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
        /// <summary>NPC 퀘스트 목록·상세 (필드 [퀘스트 목록보기], 대기실 [퀘스트] 직원 줄).</summary>
        public QuestTalkPanel QuestTalk { get; private set; }
        /// <summary>통발 설치/수거 (어부 NPC), 아이템 획득 창.</summary>
        public TrapPanel Traps { get; private set; }
        public ItemBagPopup ItemBag { get; private set; }
        public UpgradePanel Upgrade { get; private set; }
        public InventoryPanel Inventory { get; private set; }
        public CharacterPanel Character { get; private set; }
        public ExchangePanel Exchange { get; private set; }
        public AquariumPanel Aquarium { get; private set; }
        public OptionsPanel Options { get; private set; }
        /// <summary>F로 말을 걸 수 있는 가장 가까운 NPC.</summary>
        public Fishing.Characters.NpcAngler NearbyNpc { get; private set; }
        /// <summary>납품 창 (필드 납품소 · 대기실 납품 버튼).</summary>
        public DeliveryPanel Delivery { get; private set; }
        /// <summary>F로 쓸 수 있는 가까운 납품소 (NPC보다 가까울 때만).</summary>
        public DeliveryKiosk NearbyKiosk { get; private set; }

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
            // 유리 UI: 메인 카메라 화면을 흐리게 만들어 창 뒤에 비춘다
            UIFactory.InitStyle(Game.CameraRig != null ? Game.CameraRig.Cam : Camera.main);
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
                openQuest: () => QuestTalk.OpenLobby(), // 원작: 대기실 [퀘스트] → 조합 직원 줄 + 대화 카드
                delivery: () => OpenExclusive(Delivery),
                openAquarium: () => OpenExclusive(Aquarium),
                changeBase: () => Game.Toast("동남아·지중해 거점은 아직 준비 중입니다. 지금은 한국 낚시 조합만 열려 있어요.", ToastKind.Warn));
            Areas = new FishingAreaPanel(canvas);
            Areas.Enter = (area, channel) => StartCoroutine(EnterWithLoading(area, channel));
            AreaMap = new AreaMapPanel(canvas);
            Dialog = new DialogPanel(canvas);
            QuestTalk = new QuestTalkPanel(canvas);
            Upgrade = new UpgradePanel(canvas);
            Inventory = new InventoryPanel(canvas);
            Character = new CharacterPanel(canvas);
            Exchange = new ExchangePanel(canvas);
            Aquarium = new AquariumPanel(canvas);
            Options = new OptionsPanel(canvas);
            Delivery = new DeliveryPanel(canvas);
            Traps = new TrapPanel(canvas);
            ItemBag = new ItemBagPopup(canvas);
            EscMenu = new EscMenuPanel(canvas,
                options: () => OpenExclusive(Options),
                startPoint: () =>
                {
                    if (Game.Fishing.State == FishingState.Fighting || Game.Fishing.State == FishingState.Result) { Game.Toast("파이팅 중에는 움직일 수 없습니다.", ToastKind.Warn); return; }
                    Game.Fishing.ForceIdle();
                    Game.Boat.StowToLand();
                    Game.Player.Teleport(FieldMap.Spawn, 0f);
                    Game.CameraRig.Yaw = 0f;
                },
                exitField: EnterLobby);
            _panels = new UIPanel[] { KeepNet, Dex, Shop, Quest, Help, Result, Areas, AreaMap, Dialog, QuestTalk, Upgrade,Inventory, Character, Exchange, Aquarium, Options, EscMenu, Traps, ItemBag, Delivery };
            LobbyHud.Root.transform.SetAsFirstSibling();
            SysLog = new SystemLogView(canvas);
            Loading = new LoadingScreen(canvas);
            // 토스트는 모든 창 위에
            Hud.ToastLayer.SetParent(canvas, false);
            Hud.ToastLayer.SetAsLastSibling();
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
            SysLog?.Dispose();
        }

        void RefreshOpenPanels()
        {
            foreach (var p in _panels)
                if (p.IsOpen && p != Result) p.Refresh();
        }

        void Update()
        {
            bool lobby = Game.Mode == GameMode.Lobby;
            LobbyHud.Root.SetActive(lobby && !BlockingPanelOpen && !Loading.Showing);
            if (lobby) LobbyHud.Update();
            Hud.Update();
            SysLog.Update();
            Fight.Update();
            if (AreaMap.IsOpen) AreaMap.Tick();
            if (Dex.IsOpen) Dex.Tick();
            NearbyNpc = FindNearbyNpc();
            NearbyKiosk = FindNearbyKiosk();
            // 대화 중이거나 카메라가 캐릭터에 바짝 붙는 연출(캐스팅·챔질·환호) 중에는 내 이름표를 숨긴다
            Game.Player.NameTagVisible = !Dialog.IsOpen && NearbyNpc == null && Game.CameraRig.Shot == Fishing.PlayerSystems.CameraRig.ShotKind.Follow;
            Result.Animate();
            if (!IntroSequence.Playing) HandleKeys();
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
            if (_inField) Game.SysLog($"{FieldMap.ShortName} CH.{Game.Channel} - 1 채널에서 퇴장 했습니다.");
            Game.SysLog("한국 채널에 입장 했습니다.");
            _inField = false;
            Game.Save();
        }

        bool _inField;

        /// <summary>
        /// 낚시터 목록에서 입장: 원작 로딩 화면(0:18)을 띄우고 월드를 지은 뒤, 물 위에서 마을 쪽을 비추는 풍경을 드러내며 막대를 채운다 (약 2초).
        /// 자동 촬영은 <see cref="EnterField(Fishing.Data.FishingArea,int)"/>을 바로 부른다.
        /// </summary>
        public System.Collections.IEnumerator EnterWithLoading(Fishing.Data.FishingArea area, int channel)
        {
            foreach (var p in _panels) p.Close();
            Loading.Begin(area);
            yield return null;
            yield return null; // 검은 화면과 글씨를 한 번 그린 뒤에 무거운 월드 짓기
            var map = Maps.Get(area.MapId);
            if (map.Id != FieldMap.Id) MapLoader.Load(map, true);
            else Game.Player.Teleport(map.Spawn, 0f);
            // 원작 그림처럼 물 위에서 뭍(마을·부두)을 바라본다
            var spawn = map.Spawn;
            var camPos = new Vector3(spawn.x + 6f, Mathf.Max(map.WaterY, spawn.y) + 3.2f, spawn.z + 20f);
            Game.CameraRig.SetFixed(camPos, Quaternion.LookRotation(new Vector3(spawn.x - 4f, spawn.y + 2.5f, spawn.z - 12f) - camPos));
            const float hold = 2f;
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
            {
                Loading.Set(Mathf.SmoothStep(0.15f, 1f, t / hold), Mathf.Clamp01(1f - t / 0.5f));
                yield return null;
            }
            Loading.End();
            EnterField(channel);
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
            // 원작 시스템 줄: 대기실 채널에서 나가고 낚시터 채널에 들어간다. 들어가면 지금 날씨 효과를 알린다
            Game.SysLog("한국 채널에서 퇴장 했습니다.");
            Game.SysLog($"{FieldMap.ShortName} CH.{channel} - 1 채널에 입장 했습니다.");
            Game.Weather.Announce();
            _inField = true;
            var d = Game.Profile.Data;
            if (!d.visitedMaps.Contains(FieldMap.Id))
            {
                d.visitedMaps.Add(FieldMap.Id);
                if (FieldMap.IsSea)
                {
                    // 처음 온 바다: 어촌계 환영 선물로 바다 미끼
                    Game.Profile.AddItem("bait_krill", 20);
                    Game.Profile.AddItem("bait_seaworm", 20);
                    bool egi = System.Array.IndexOf(Fishing.Data.FishSpecies.EgiMaps, FieldMap.Id) >= 0;
                    if (egi) Game.Profile.AddItem("bait_egi", 5);
                    Game.Toast($"{FieldMap.ShortName}에 처음 오셨군요! 어촌계 환영 선물: 크릴 20, 청갯지렁이 20" + (egi ? ", 에기 5" : ""), ToastKind.Great);
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
            // 옵션 창에서 새 키를 기다리는 중이면 다른 단축키는 받지 않는다
            if (Options.IsOpen && Options.Waiting != null)
            {
                Options.Tick();
                return;
            }
            if (Result.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) Result.ChooseByKey(true);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 열린 창이 있으면 닫고, 없으면 원작 ESC 메뉴 (파이팅 중엔 열지 않는다)
                bool any = BlockingPanelOpen;
                foreach (var p in _panels) p.Close();
                if (!any && !Loading.Showing && Game.Fishing.State != FishingState.Fighting) OpenExclusive(EscMenu);
                return;
            }

            // 설명 끄기/켜기는 파이팅 중에도 된다 (원작: [설명 끄기 : U])
            if (Settings.Down(KeyAction.Tips)) ToggleTips();
            if (Settings.Down(KeyAction.Options)) OpenExclusive(Options);

            if (Game.Mode == GameMode.Lobby)
            {
                if (Settings.Down(KeyAction.Quest)) OpenExclusive(Quest);
                if (Settings.Down(KeyAction.Dex)) OpenExclusive(Dex);
                if (Settings.Down(KeyAction.KeepNet)) OpenExclusive(KeepNet);
                if (Settings.Down(KeyAction.Inventory)) OpenExclusive(Inventory);
                if (Settings.Down(KeyAction.Costume)) OpenExclusive(Character);
                if (Settings.Down(KeyAction.Help)) OpenExclusive(Help);
                return;
            }

            var state = Game.Fishing.State;
            if (state == FishingState.Fighting) return;
            bool idle = state == FishingState.Idle;

            // 원작 단축키: 퀘스트 L, 도감 P, 살림망 Z, 상점 F12, 지역 지도 M (옵션에서 바꿀 수 있다)
            if (Settings.Down(KeyAction.Help)) OpenExclusive(Help);
            if (Settings.Down(KeyAction.AreaMap)) OpenExclusive(AreaMap);
            if (Settings.Down(KeyAction.Inventory)) OpenExclusive(Inventory);
            if (Settings.Down(KeyAction.Costume)) OpenExclusive(Character);
            if (Settings.Down(KeyAction.Talk) && NearbyNpc != null && idle && !BlockingPanelOpen) TalkTo(NearbyNpc);
            else if (Settings.Down(KeyAction.Talk) && NearbyKiosk != null && idle && !BlockingPanelOpen) OpenExclusive(Delivery);
            if (Settings.Down(KeyAction.Boat) && !BlockingPanelOpen) Game.Boat.Toggle();
            if (Settings.Down(KeyAction.Dex)) OpenExclusive(Dex);
            if (Settings.Down(KeyAction.Quest)) OpenExclusive(Quest);
            if (Settings.Down(KeyAction.KeepNet)) OpenExclusive(KeepNet);
            if (Settings.Down(KeyAction.Shop)) OpenShop();
            if (Settings.Down(KeyAction.Save)) SaveNow();

            if (idle && !BlockingPanelOpen)
                for (int i = 0; i < HudView.MaxBaitSlots; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Hud.SelectBait(i);
        }

        /// <summary>가까운 납품소. NPC(조합 연락관·통발 어부)와 둘 다 가까우면 더 가까운 쪽만 남긴다.</summary>
        DeliveryKiosk FindNearbyKiosk()
        {
            if (Game.Mode != GameMode.Field || Game.Player.OnBoat || Game.Fishing.State != FishingState.Idle) return null;
            var p = Game.Player.transform.position;
            var k = DeliveryKiosk.Nearest(p);
            if (k == null) return null;
            if (NearbyNpc != null)
            {
                var dk = k.transform.position - p;
                var dn = NearbyNpc.transform.position - p;
                dk.y = dn.y = 0f;
                if (dn.sqrMagnitude <= dk.sqrMagnitude) return null;
                NearbyNpc = null;
            }
            return k;
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
