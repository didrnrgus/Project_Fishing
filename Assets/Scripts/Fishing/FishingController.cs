using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fishing.Angling
{
    public enum FishingState { Idle, Charging, Casting, Waiting, Bite, Fighting, Result }

    /// <summary>
    /// 낚시 한 번의 흐름을 관리한다.
    /// Idle → (좌클릭 누르고 있기) Charging → (떼기) Casting → Waiting → Bite → (좌클릭 챔질) Fighting → Result
    /// </summary>
    public class FishingController : MonoBehaviour
    {
        public FishingState State { get; private set; }
        public float Charge { get; private set; }
        public float CastDistance { get; private set; }
        public bool CastTargetIsWater { get; private set; }
        public FishFight Fight { get; private set; }
        public BiteResult PendingBite { get; private set; }
        public bool BigBite { get; private set; }
        public string CastCellLabel { get; private set; } = "";
        public Vector3 BobberPosition => _bobber != null ? _bobber.Position : Vector3.zero;

        /// <summary>개발용: 파이팅을 자동으로 진행한다 (스크린샷/밸런스 확인).</summary>
        public bool Autoplay;

        public event System.Action FightStarted;
        public event System.Action<FightOutcome> FightEnded;

        Bobber _bobber;
        FightRodView _rodView;
        LineRenderer _line;
        GameObject _marker;
        Renderer _markerRenderer;

        Vector3 _castTarget;
        Vector3 _castDir;
        BiteContext _ctx;
        string _cellName;
        float _chargeT;
        float _waitTimer;
        float _nextNibbleAt;
        int _nibblesLeft;
        bool _nibbleActive;
        float _nibbleWindowEnd;
        float _biteTimer;
        float _biteWindow;
        float _reelClickTimer;
        bool _cursorWasLocked;

        void Start()
        {
            _bobber = Bobber.Create();
            _rodView = FightRodView.Create(Game.CameraRig.Cam);

            _line = new GameObject("FishingLine").AddComponent<LineRenderer>();
            _line.positionCount = 14;
            _line.widthMultiplier = 0.01f;
            _line.sharedMaterial = Mats.Unlit(new Color(0.9f, 0.95f, 0.9f));
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.enabled = false;

            _marker = Mats.Prim(PrimitiveType.Cylinder, "CastMarker", null, Vector3.zero, new Vector3(1.2f, 0.01f, 1.2f),
                Mats.Transparent(new Color(0.3f, 1f, 0.7f, 0.45f), 0.5f, shared: false), collider: false);
            _markerRenderer = _marker.GetComponent<Renderer>();
            _marker.SetActive(false);
        }

        bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool UiBlocking => Game.UI != null && Game.UI.BlockingPanelOpen;

        void Update()
        {
            switch (State)
            {
                case FishingState.Idle: UpdateIdle(); break;
                case FishingState.Charging: UpdateCharging(); break;
                case FishingState.Casting: UpdateCasting(); break;
                case FishingState.Waiting: UpdateWaiting(); break;
                case FishingState.Bite: UpdateBite(); break;
                case FishingState.Fighting: UpdateFighting(); break;
            }
            UpdateLine();
        }

        // ---------- Idle / Charging / Casting ----------

        void UpdateIdle()
        {
            Game.Player.MovementLocked = false;
            Game.Player.RodPitch = 35f;
            if (UiBlocking || PointerOverUI) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var p = Game.Profile;
            if (p.Count(p.Bait.Id) <= 0)
            {
                Game.Toast($"{p.Bait.Name}이(가) 없습니다. 숫자키로 다른 미끼를 고르거나 낚시점(P)에서 사세요.", ToastKind.Warn);
                return;
            }
            if (p.Data.stamina <= 0f)
            {
                Game.Toast("체력이 바닥났습니다. 살림망(I)에서 요리를 하거나 김밥을 드세요.", ToastKind.Warn);
                return;
            }

            State = FishingState.Charging;
            _chargeT = 0f;
            Game.Player.MovementLocked = true;
            Game.Player.FaceYaw(Game.CameraRig.Yaw);
            _marker.SetActive(true);
        }

        void UpdateCharging()
        {
            _chargeT += Time.deltaTime;
            Charge = Mathf.PingPong(_chargeT / 0.9f, 1f);
            Game.Player.RodPitch = Mathf.Lerp(60f, 95f, Charge);
            Game.Player.FaceYaw(Game.CameraRig.Yaw);

            _castDir = Game.Player.Model.forward;
            var rod = Game.Profile.Rod;
            CastDistance = Mathf.Lerp(4f, rod.CastRange, Charge);
            _castTarget = Game.Player.transform.position + _castDir * CastDistance;
            _castTarget.y = HanRiverMap.WaterY + 0.01f;
            CastTargetIsWater = HanRiverMap.IsWater(_castTarget);
            _marker.transform.position = _castTarget;
            var c = CastTargetIsWater ? new Color(0.3f, 1f, 0.7f, 0.45f) : new Color(1f, 0.3f, 0.3f, 0.45f);
            _markerRenderer.material.color = c;

            if (Input.GetMouseButtonUp(0) || UiBlocking)
            {
                _marker.SetActive(false);
                if (UiBlocking) { ToIdle(); return; }
                if (!CastTargetIsWater)
                {
                    Game.Toast("물 위로 던져야 합니다.", ToastKind.Warn);
                    ToIdle();
                    return;
                }
                Game.Player.RodPitch = 20f;
                _bobber.Launch(Game.Player.RodTip.position, _castTarget);
                Game.Sound.Whoosh();
                State = FishingState.Casting;
            }
        }

        void UpdateCasting()
        {
            if (!_bobber.Landed) return;
            Game.Player.RodPitch = 28f;
            BeginWait(1f);
        }

        // ---------- Waiting / Bite ----------

        void BeginWait(float slowdown)
        {
            HanRiverMap.TryCell(_bobber.Position, out int col, out int row);
            _cellName = HanRiverMap.CellName(col, row);
            _ctx = new BiteContext
            {
                Habitat = HanRiverMap.HabitatAt(col, row),
                Bait = Game.Profile.Bait.Kind,
                Band = Game.Clock.Band,
                Weather = Game.Weather.Current,
                School = Game.Schools.At(col, row),
            };
            CastCellLabel = $"{_cellName} · {Names.Of(_ctx.Habitat)}" + (_ctx.School != null ? $" · {_ctx.School.Name}" : "");
            PendingBite = BiteSelector.Pick(_ctx);
            BigBite = false;

            _waitTimer = BiteSelector.WaitSeconds(_ctx) * slowdown;
            _nibblesLeft = PendingBite.Species.Aggression > 0.75f ? Random.Range(0, 2) : Random.Range(0, 4);
            _nextNibbleAt = _waitTimer * Random.Range(0.3f, 0.7f);
            _nibbleActive = false;
            _bobber.ResetFloat();
            State = FishingState.Waiting;
        }

        bool WantsRetrieve => Input.GetKeyDown(KeyCode.R) || Game.Player.MovedThisFrame;

        void UpdateWaiting()
        {
            if (WantsRetrieve) { Retrieve("찌를 회수했습니다."); return; }

            _waitTimer -= Time.deltaTime;
            if (_nibbleActive && Time.time > _nibbleWindowEnd) _nibbleActive = false;

            if (_nibblesLeft > 0 && _waitTimer <= _nextNibbleAt)
            {
                _nibblesLeft--;
                _bobber.Nibble();
                Game.Sound.Tick();
                _nibbleActive = true;
                _nibbleWindowEnd = Time.time + 0.3f;
                _nextNibbleAt = _waitTimer - Random.Range(0.8f, 2f);
            }

            if (_waitTimer <= 0f && _nibblesLeft <= 0)
            {
                State = FishingState.Bite;
                _biteWindow = 0.9f + 0.35f * Game.Profile.Rod.Flex;
                _biteTimer = 0f;
                BigBite = PendingBite.IsTrophy || PendingBite.Species.Rarity >= 4;
                _bobber.Sink();
                Game.Sound.Plop();
                if (BigBite) Game.Sound.Heartbeat();
                return;
            }

            if (!UiBlocking && !PointerOverUI && Input.GetMouseButtonDown(0))
            {
                // 깔짝일 때 챔질하면 헛챔질
                if (Random.value < 0.5f) Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                Game.Toast(_nibbleActive ? "헛챔질! 예신에 속았다..." : "헛챔질! 고기가 놀라 달아났다.", ToastKind.Warn);
                Game.Player.RodPitch = 60f;
                BeginWait(1.3f);
            }
        }

        void UpdateBite()
        {
            if (WantsRetrieve) { Retrieve("찌를 회수했습니다."); return; }

            _biteTimer += Time.deltaTime;
            if (Autoplay || (!UiBlocking && Input.GetMouseButtonDown(0)))
            {
                float quality = 1f - Mathf.Clamp01(_biteTimer / _biteWindow) * 0.6f;
                Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                Game.Toast(quality > 0.85f ? "완벽한 챔질!" : "챔질 성공!", ToastKind.Good);
                StartFight(PendingBite, Vector3.Distance(Game.Player.transform.position, _bobber.Position), quality);
                return;
            }
            if (_biteTimer > _biteWindow)
            {
                Game.Profile.ConsumeItem(Game.Profile.Bait.Id);
                Game.Toast("늦었다! 미끼만 따먹혔다. 다시 던지세요.", ToastKind.Warn);
                ToIdle();
            }
        }

        void Retrieve(string message)
        {
            Game.Toast(message);
            ToIdle();
        }

        void ToIdle()
        {
            State = FishingState.Idle;
            Charge = 0f;
            BigBite = false;
            _bobber.Hide();
            _marker.SetActive(false);
            Game.Player.MovementLocked = false;
            Game.Player.RodPitch = 35f;
        }

        // ---------- Fighting ----------

        public void StartFight(BiteResult bite, float distance, float hookQuality)
        {
            var p = Game.Profile;
            PendingBite = bite;
            if (_castDir == Vector3.zero) _castDir = Game.Player.Model.forward;
            Fight = new FishFight(bite.Species, bite.Length, Mathf.Max(4f, distance), hookQuality, p.Rod, p.Reel, p.Line);
            Fight.Countered += OnCounter;
            Fight.Burst += () => Game.Sound.Splash(1f);

            State = FishingState.Fighting;
            _bobber.Hide();
            _line.enabled = false;
            Game.Player.MovementLocked = true;
            Game.Player.SetVisible(false);
            Game.CameraRig.EnterFight(Game.Player.EyePos, _castDir);
            _rodView.Show(Fight, Game.Player.transform.position, _castDir);

            _cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FightStarted?.Invoke();
        }

        /// <summary>개발/테스트용: 캐스팅 없이 바로 파이팅을 시작한다.</summary>
        public void DebugStartFight(string speciesId, float length, float distance)
        {
            if (State != FishingState.Idle) ToIdle();
            _castDir = Game.Player.Model.forward;
            HanRiverMap.TryCell(Game.Player.transform.position + _castDir * distance, out int col, out int row);
            _cellName = HanRiverMap.CellName(Mathf.Clamp(col, 0, 5), Mathf.Clamp(row, 0, 3));
            _ctx = new BiteContext { Habitat = Habitat.Deep, Band = Game.Clock.Band };
            StartFight(new BiteResult { Species = FishDatabase.Get(speciesId), Length = length }, distance, 0.9f);
        }

        /// <summary>개발/테스트용: 바라보는 방향으로 찌를 던진다.</summary>
        public void DebugCast(float distance)
        {
            if (State != FishingState.Idle) ToIdle();
            _castDir = Game.Player.Model.forward;
            _castTarget = Game.Player.transform.position + _castDir * distance;
            Game.Player.MovementLocked = true;
            Game.Player.RodPitch = 20f;
            _bobber.Launch(Game.Player.RodTip.position, _castTarget);
            State = FishingState.Casting;
        }

        void OnCounter()
        {
            Game.Sound.Counter();
            Game.CameraRig.Shake(1.2f);
            Effects.Splash(_rodView.FishWorldPos(Fight), 30, 1.6f);
            Game.UI.Fight.ShowCounter(Fight.Combo);
        }

        void UpdateFighting()
        {
            var f = Fight;
            var input = Autoplay ? FightBot.Input(f) : new FightInput
            {
                Reeling = Input.GetMouseButton(0),
                Releasing = Input.GetMouseButton(1),
                MouseDX = Input.GetAxis("Mouse X"),
            };

            var p = Game.Profile;
            f.Tick(Time.deltaTime, input, p.StaminaFactor, p.DurabilityFactor);

            // 체력 소모: 하이텐션일수록 많이 지친다
            float drain = 0.35f + (f.IsHighTension ? 1.8f * f.Tension : 0f);
            p.ChangeStamina(-drain * Time.deltaTime);

            if (input.Reeling)
            {
                _reelClickTimer -= Time.deltaTime;
                if (_reelClickTimer <= 0f)
                {
                    _reelClickTimer = 0.07f;
                    Game.Sound.ReelClick(f.Tension);
                }
            }

            if ((Autoplay || Input.GetKeyDown(KeyCode.Space)) && f.CanLand) f.TryLand();
            if (f.Outcome != FightOutcome.None) EndFight(f.Outcome);
        }

        void EndFight(FightOutcome outcome)
        {
            var f = Fight;
            var p = Game.Profile;
            Cursor.lockState = _cursorWasLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = true;
            _rodView.Hide();
            Game.CameraRig.ExitFight();
            Game.Player.SetVisible(true);
            Game.Player.RodPitch = 35f;

            float wear = Mathf.Lerp(0.5f, 3f, f.Length / 100f) * (f.Tension > 0.9f ? 1.5f : 1f);
            p.Data.rodDurability = Mathf.Max(0f, p.Data.rodDurability - wear);
            if (p.Data.rodDurability <= 0f) Game.Toast("낚싯대가 망가졌습니다! 낚시점에서 수리하세요.", ToastKind.Warn);

            FightEnded?.Invoke(outcome);

            if (outcome != FightOutcome.Landed)
            {
                Game.Sound.Snap();
                Game.Toast(outcome switch
                {
                    FightOutcome.LineBroken => "툭! 줄이 끊어졌다... 장력을 조심하세요.",
                    FightOutcome.HookOut => "바늘이 빠졌다... 줄을 너무 느슨하게 했어요.",
                    FightOutcome.LineRunOut => "줄이 모두 풀려버렸다...",
                    _ => "놓쳤다..."
                }, ToastKind.Warn);
                Fight = null;
                ToIdle();
                p.NotifyChanged();
                return;
            }

            Game.Sound.Success();
            var species = f.Species;
            var caught = new CaughtFish
            {
                speciesId = species.Id,
                length = f.Length,
                weight = Mathf.Round(species.WeightGrams(f.Length)),
                day = Game.Clock.Day,
                zone = _cellName,
            };
            bool record = p.RecordDex(species, caught.length, caught.weight, caught.day);
            int exp = Mathf.RoundToInt(species.Exp * Mathf.Sqrt(f.Length / species.MinLen) * (species.IsTrophy(f.Length) ? 2f : 1f));
            p.Data.totalCatches++;
            p.AddExp(exp);
            Game.Quests.OnCatch(new CatchInfo { Species = species, Length = f.Length, Habitat = _ctx.Habitat, Band = Game.Clock.Band });

            State = FishingState.Result;
            Fight = null;
            Game.UI.Result.Show(caught, exp, record, keep =>
            {
                if (keep && !species.IsJunk)
                {
                    if (p.KeepNetFull)
                        Game.Toast("살림망이 가득 찼습니다. 낚시점에서 판매하거나 요리하세요.", ToastKind.Warn);
                    else
                        p.Data.keepNet.Add(caught);
                }
                else if (!species.IsJunk)
                {
                    int bonus = Mathf.Max(1, Mathf.RoundToInt(exp * 0.3f));
                    p.AddExp(bonus);
                    Game.Toast($"방생했습니다. 경험치 +{bonus}", ToastKind.Good);
                }
                p.NotifyChanged();
                ToIdle();
                Game.Save();
            });
        }

        // ---------- 3인칭 낚싯줄 ----------

        void UpdateLine()
        {
            bool show = State == FishingState.Casting || State == FishingState.Waiting || State == FishingState.Bite;
            _line.enabled = show;
            if (!show) return;

            var a = Game.Player.RodTip.position;
            var b = _bobber.Position + Vector3.up * 0.3f;
            float sag = State == FishingState.Bite ? 0.1f : Vector3.Distance(a, b) * 0.06f;
            var mid = (a + b) * 0.5f + Vector3.down * sag;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
                _line.SetPosition(i, p);
            }
        }
    }
}
