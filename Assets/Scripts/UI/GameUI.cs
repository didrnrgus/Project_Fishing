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
            _panels = new UIPanel[] { KeepNet, Dex, Shop, Quest, Help, Result };
        }

        void Start()
        {
            Game.Profile.Changed += RefreshOpenPanels;
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
            Hud.Update();
            Fight.Update();
            HandleKeys();
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

            var state = Game.Fishing.State;
            if (state == FishingState.Fighting) return;
            bool idle = state == FishingState.Idle;

            if (Input.GetKeyDown(KeyCode.F1)) OpenExclusive(Help);
            if (Input.GetKeyDown(KeyCode.B)) OpenExclusive(Dex);
            if (Input.GetKeyDown(KeyCode.Q)) OpenExclusive(Quest);
            if (Input.GetKeyDown(KeyCode.I)) OpenExclusive(KeepNet);
            if (Input.GetKeyDown(KeyCode.P))
            {
                bool near = Vector3.Distance(Game.Player.transform.position, HanRiverMap.ShopPos) < HanRiverMap.ShopRadius;
                if (Shop.IsOpen) Shop.Close();
                else if (!idle) Game.Toast("낚시 중에는 낚시점을 이용할 수 없습니다.", ToastKind.Warn);
                else if (!near) Game.Toast("낚시점 근처에서 이용할 수 있습니다. (미니맵의 노란 점)", ToastKind.Warn);
                else OpenExclusive(Shop);
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Game.Save();
                Game.Toast("저장했습니다.", ToastKind.Good);
            }

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
