using Fishing.Core;
using Fishing.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 장비 강화 창 (낚시점 주인에게 맡긴다). 장착한 낚싯대·릴을 +1 ~ +15까지 강화한다.
    /// 재료: 골드 + 반짝이는 비늘(고기를 해체해 얻음), 별모래를 넣으면 성공 확률 +15%.
    /// 실패해도 장비는 깨지지 않고, +7 이상에서만 10% 확률로 한 단계 떨어진다 (원작 개발실장 인터뷰 기준).
    /// </summary>
    public class UpgradePanel : UIPanel
    {
        GearSlot _slot = GearSlot.Rod;
        bool _useStar;
        float _resultUntil;
        string _resultText = "";
        Color _resultColor = Color.white;

        public UpgradePanel(Transform canvas)
        {
            BuildFrame(canvas, "UpgradePanel", "장비 강화", new Vector2(860, 640));
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var gear = p.EquippedBase(_slot);
            int lv = p.GearLevel(gear.Id);
            Title.text = $"장비 강화  <size=18><color=#a0b0c0>{Fishing.World.FieldMap.ShopName}</color></size>";
            UIFactory.ClearChildren(Body);

            var rows = new RowCursor(Body);
            // 탭: 낚싯대 / 릴
            var tabs = rows.Next(42, 8);
            TabButton(tabs, "낚싯대", GearSlot.Rod, 6);
            TabButton(tabs, "릴", GearSlot.Reel, 170);

            Cell(rows.Next(44), $"{gear.Name} <color=#ffd060>+{lv}</color>", 6, 800, 30, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);

            // 강화 단계 칸 (15칸)
            var pips = rows.Next(30, 10);
            for (int i = 0; i < RodUpgrade.MaxLevel; i++)
            {
                var c = i < lv ? (i >= 10 ? new Color(1f, 0.4f, 0.3f) : i >= 5 ? new Color(1f, 0.6f, 0.2f) : UIFactory.Gold) : new Color(1, 1, 1, 0.12f);
                var img = UIFactory.Image(pips, c, null, "Pip").rectTransform;
                img.anchorMin = img.anchorMax = new Vector2(0, 0.5f);
                img.pivot = new Vector2(0, 0.5f);
                img.anchoredPosition = new Vector2(6 + i * 52, 0);
                img.sizeDelta = new Vector2(46, 20);
            }

            var now = RodUpgrade.Apply(gear, lv);
            if (lv >= RodUpgrade.MaxLevel)
            {
                Cell(rows.Next(34), Stat(now), 6, 800, 21);
                Cell(rows.Next(50), "최고 단계입니다! 더 강화할 수 없습니다.", 6, 800, 24, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            else
            {
                var next = RodUpgrade.Apply(gear, lv + 1);
                bool hasStar = p.Count(RodUpgrade.StarSandId) > 0;
                if (!hasStar) _useStar = false;
                float rate = RodUpgrade.SuccessRate(lv, _useStar);
                int cost = RodUpgrade.Cost(gear, lv);
                int scales = RodUpgrade.Scales(lv);
                int haveScales = p.Count(RodUpgrade.ScaleId);
                Cell(rows.Next(34), $"{Stat(now)}  →  <color=#70e070>{Stat(next)}</color>", 6, 800, 20);
                Cell(rows.Next(32), $"성공 확률 <b>{rate * 100f:0}%</b>   ·   <color=#ffd060>{cost:N0} 골드</color> (보유 {p.Data.gold:N0})   ·   " +
                                    $"반짝이는 비늘 <color={(haveScales >= scales ? "#70e070" : "#ff7060")}>{scales}개</color> (보유 {haveScales})", 6, 800, 19);

                var starRow = rows.Next(40, 6);
                RowButton(starRow, _useStar ? "✔ 별모래 사용 (+15%)" : "별모래 사용 안 함", () => { _useStar = !_useStar; Refresh(); }, 6, 260,
                    _useStar ? new Color(0.45f, 0.35f, 0.7f) : new Color(0.2f, 0.22f, 0.28f), hasStar, 17);
                Cell(starRow, $"별모래 보유 {p.Count(RodUpgrade.StarSandId)}개", 280, 300, 16, UIFactory.TextDim);

                Cell(rows.Next(28), RodUpgrade.CanDowngrade(lv)
                        ? $"<color=#ffa070>+{RodUpgrade.DowngradeFrom} 이상은 실패하면 {RodUpgrade.DowngradeChance * 100f:0}% 확률로 한 단계 내려갑니다. 장비는 절대 깨지지 않습니다.</color>"
                        : "실패해도 단계는 내려가지 않습니다 (재료만 소모).", 6, 800, 16, UIFactory.TextDim);
                var btnRow = rows.Next(64, 12);
                bool can = p.Data.gold >= cost && haveScales >= scales;
                RowButton(btnRow, $"강화하기", Try, 6, 360, new Color(0.55f, 0.35f, 0.1f), can, 24);
                if (!can) Cell(btnRow, haveScales < scales ? "비늘이 부족합니다. 살림망(Z)에서 큰 고기를 해체하세요." : "골드가 부족합니다.", 380, 420, 16, UIFactory.Danger);
            }
            Cell(rows.Next(26), "장착한 장비만 강화할 수 있습니다. 다른 장비는 상점(F12)에서 장착을 바꾸세요.", 6, 800, 15, UIFactory.TextDim);

            var result = UIFactory.Text(Body, Time.unscaledTime < _resultUntil ? _resultText : "", 38, _resultColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            result.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(800, 60));
            result.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        }

        void TabButton(RectTransform row, string label, GearSlot slot, float x)
        {
            bool sel = _slot == slot;
            RowButton(row, label, () => { _slot = slot; Refresh(); }, x, 150, sel ? new Color(0.15f, 0.45f, 0.42f) : new Color(0.15f, 0.18f, 0.22f), true, 18);
        }

        static string Stat(GearDef g) => g.Slot == GearSlot.Reel
            ? $"감기 {g.ReelSpeed:0.00}m/s · 권사량 {g.Capacity:0}m"
            : $"힘 {g.Power:0.00} · 비거리 {g.CastRange:0.0}m";

        void Try()
        {
            var p = Game.Profile;
            var gear = p.EquippedBase(_slot);
            int before = p.GearLevel(gear.Id);
            switch (p.TryUpgrade(_slot, _useStar))
            {
                case UpgradeResult.Success:
                    Game.Sound.Success();
                    SetResult($"강화 성공!  +{before} → +{before + 1}", UIFactory.Gold);
                    Game.Toast($"{gear.Name} 강화 성공! +{before + 1}", ToastKind.Great);
                    break;
                case UpgradeResult.Fail:
                    Game.Sound.Snap();
                    SetResult("강화 실패... (재료만 날아갔다)", new Color(1f, 0.55f, 0.45f));
                    break;
                case UpgradeResult.Downgrade:
                    Game.Sound.Snap();
                    SetResult($"강화 실패!  +{before} → +{before - 1}", UIFactory.Danger);
                    break;
                case UpgradeResult.NoGold:
                    Game.Toast("골드가 부족합니다.", ToastKind.Warn);
                    break;
                case UpgradeResult.NoMaterial:
                    Game.Toast("반짝이는 비늘이 부족합니다. 큰 고기를 해체하세요.", ToastKind.Warn);
                    break;
            }
            Refresh();
        }

        void SetResult(string text, Color color)
        {
            _resultText = text;
            _resultColor = color;
            _resultUntil = Time.unscaledTime + 2.5f;
        }
    }
}
