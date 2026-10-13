using Fishing.Core;
using Fishing.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>낚싯대 강화 창 (낚시점 김사장). 장착한 낚싯대를 +1 ~ +10까지 골드를 내고 강화한다.</summary>
    public class UpgradePanel : UIPanel
    {
        Text _result;
        float _resultUntil;
        string _resultText = "";
        Color _resultColor = Color.white;

        public UpgradePanel(Transform canvas)
        {
            BuildFrame(canvas, "UpgradePanel", "낚싯대 강화  ·  반포 낚시점", new Vector2(820, 600));
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var baseRod = GearDatabase.GetGear(p.Data.rodId);
            int lv = p.RodLevel(baseRod.Id);
            UIFactory.ClearChildren(Body);

            var rows = new RowCursor(Body);
            Cell(rows.Next(44), $"{baseRod.Name} <color=#ffd060>+{lv}</color>", 6, 760, 32, UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);

            // 강화 단계 칸 (10칸)
            var pips = rows.Next(34, 10);
            for (int i = 0; i < RodUpgrade.MaxLevel; i++)
            {
                var c = i < lv ? (i >= 7 ? new Color(1f, 0.55f, 0.2f) : UIFactory.Gold) : new Color(1, 1, 1, 0.12f);
                var img = UIFactory.Image(pips, c, null, "Pip").rectTransform;
                img.anchorMin = img.anchorMax = new Vector2(0, 0.5f);
                img.pivot = new Vector2(0, 0.5f);
                img.anchoredPosition = new Vector2(6 + i * 74, 0);
                img.sizeDelta = new Vector2(66, 22);
            }

            var now = RodUpgrade.Apply(baseRod, lv);
            if (lv >= RodUpgrade.MaxLevel)
            {
                Cell(rows.Next(34), $"힘 {now.Power:0.00}  ·  비거리 {now.CastRange:0.0}m", 6, 760, 22);
                Cell(rows.Next(50), "최고 단계입니다! 더 강화할 수 없습니다.", 6, 760, 24, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            else
            {
                var next = RodUpgrade.Apply(baseRod, lv + 1);
                float rate = RodUpgrade.SuccessRate(lv);
                int cost = RodUpgrade.Cost(baseRod, lv);
                Cell(rows.Next(34), $"힘 {now.Power:0.00} → <color=#70e070>{next.Power:0.00}</color>   ·   비거리 {now.CastRange:0.0}m → <color=#70e070>{next.CastRange:0.0}m</color>", 6, 760, 22);
                Cell(rows.Next(34), $"성공 확률 <b>{rate * 100f:0}%</b>   ·   비용 <color=#ffd060>{cost:N0} 골드</color>  (보유 {p.Data.gold:N0})", 6, 760, 22);
                Cell(rows.Next(30), RodUpgrade.FailDowngrades(lv)
                        ? $"<color=#ff7060>+{RodUpgrade.DowngradeFrom} 이상은 실패하면 한 단계 내려갑니다.</color>"
                        : "실패해도 단계는 내려가지 않습니다 (골드만 소모).", 6, 760, 17, UIFactory.TextDim);
                var btnRow = rows.Next(64, 14);
                RowButton(btnRow, $"강화하기  ({cost:N0} 골드)", Try, 6, 360, new Color(0.55f, 0.35f, 0.1f), p.Data.gold >= cost, 22);
            }
            Cell(rows.Next(26), "장착한 낚싯대만 강화할 수 있습니다. 다른 낚싯대는 상점(F12)에서 장착을 바꾸세요.", 6, 760, 15, UIFactory.TextDim);

            _result = UIFactory.Text(Body, "", 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(760, 60));
            _result.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            _result.text = Time.unscaledTime < _resultUntil ? _resultText : "";
            _result.color = _resultColor;
        }

        void Try()
        {
            var p = Game.Profile;
            int before = p.RodLevel(p.Data.rodId);
            var r = p.TryUpgradeRod();
            switch (r)
            {
                case UpgradeResult.Success:
                    Game.Sound.Success();
                    SetResult($"강화 성공!  +{before} → +{before + 1}", UIFactory.Gold);
                    Game.Toast($"낚싯대 강화 성공! +{before + 1}", ToastKind.Great);
                    break;
                case UpgradeResult.Fail:
                    Game.Sound.Snap();
                    SetResult("강화 실패... (골드만 날아갔다)", new Color(1f, 0.55f, 0.45f));
                    break;
                case UpgradeResult.Downgrade:
                    Game.Sound.Snap();
                    SetResult($"강화 실패!  +{before} → +{before - 1}", UIFactory.Danger);
                    break;
                case UpgradeResult.NoGold:
                    Game.Toast("골드가 부족합니다.", ToastKind.Warn);
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
