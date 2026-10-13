using System.Linq;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 의상(C). 왼쪽은 내 캐릭터 전신 미리보기(두 번째 카메라), 오른쪽은 부위별 의상 목록.
    /// 사서 입으면 바로 캐릭터·초상화·대기실에 반영되고, 능력치(체력·대물·데미지·카운터·비거리·체력 소모)가 붙는다.
    /// </summary>
    public class CostumePanel : UIPanel
    {
        CostumeSlot _slot = CostumeSlot.Hat;
        RectTransform _list;
        TMP_Text _stats;
        ChartHost _radar;
        static readonly string[] StatNames = { "체력", "대물", "데미지", "카운터", "비거리", "절약" };

        public CostumePanel(Transform canvas)
        {
            BuildFrame(canvas, "CostumePanel", "의상", new Vector2(1320, 800));
            var cam = PortraitCamera.FullBody ?? PortraitCamera.CreateFullBody();

            // 전신 미리보기 (렌더 텍스처, 둥근 모서리). 원래 3:4 비율 유지
            var previewFrame = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard + 4f, "PreviewFrame", 0f).rectTransform
                .At(new Vector2(0, 1), Vector2.zero, new Vector2(420, 400), new Vector2(0, 1));
            var preview = UIFactory.RoundedTexture(previewFrame, cam.Target, UIFactory.RadiusCard + 2f, "Preview");
            preview.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(294, 392));
            var spinL = UIFactory.IconButton(previewFrame, "◀", () => cam.Spin -= 45f, new Color(0, 0, 0, 0.45f), 18);
            ((RectTransform)spinL.transform).At(new Vector2(0, 0), new Vector2(12, 12), new Vector2(44, 44), new Vector2(0, 0));
            var spinR = UIFactory.IconButton(previewFrame, "▶", () => cam.Spin += 45f, new Color(0, 0, 0, 0.45f), 18);
            ((RectTransform)spinR.transform).At(new Vector2(1, 0), new Vector2(-12, 12), new Vector2(44, 44), new Vector2(1, 0));

            // 입은 의상 능력치: 레이더 (축마다 "부위별 최고 의상을 모두 입었을 때"를 100으로)
            var statCard = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "StatCard", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(0, -412), new Vector2(420, 282), new Vector2(0, 1));
            UIFactory.Text(statCard, "입은 의상 능력치", 15, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(200, 24));
            _radar = UIChart.Radar(statCard, new Vector2(232, 232), StatNames,
                new[] { new UIChart.Series("입은 의상", new float[StatNames.Length], UIFactory.Accent) }, 12);
            ((RectTransform)_radar.transform).At(new Vector2(0, 0), new Vector2(4, 10), new Vector2(232, 232), Vector2.zero);
            _stats = UIFactory.Text(statCard, "", 13, UIFactory.TextMain, TextAnchor.UpperLeft);
            _stats.rectTransform.At(new Vector2(0, 1), new Vector2(252, -48), new Vector2(160, 230), new Vector2(0, 1));
            _stats.lineSpacing = 10f;

            _list = UIFactory.Node("List", Body).At(new Vector2(1, 1), Vector2.zero, new Vector2(820, 700), new Vector2(1, 1));
        }

        public override void Open()
        {
            base.Open();
            PortraitCamera.FullBody.Spin = 0f;
            PortraitCamera.FullBody.SetActive(true);
        }

        public override void Close()
        {
            base.Close();
            if (PortraitCamera.FullBody != null) PortraitCamera.FullBody.SetActive(false);
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            Title.text = $"의상   <size=20><color=#ffd060>{p.Data.gold:N0} 골드</color></size>";
            var st = p.Costume;
            _stats.text = $"<color=#8fa3b5>최대 체력</color> {p.StaminaCap:0} <size=12>(+{st.Stamina:0})</size>\n" +
                          $"<color=#8fa3b5>대물</color> +{st.BigFish * 100f:0}%\n" +
                          $"<color=#8fa3b5>데미지</color> +{st.Damage * 100f:0}%\n" +
                          $"<color=#8fa3b5>카운터</color> +{st.Counter * 100f:0}%\n" +
                          $"<color=#8fa3b5>비거리</color> +{st.Cast:0.0}m\n" +
                          $"<color=#8fa3b5>체력 소모</color> -{st.Drain * 100f:0}%";
            UIChart.SetValues(_radar.Serie(), Normalized(st));
            _radar.Refresh();

            UIFactory.ClearChildren(_list);
            var rows = new RowCursor(_list);
            var tabs = rows.Next(42, 10);
            string[] names = { "모자", "안경", "상의", "조끼", "하의", "신발" };
            for (int i = 0; i < names.Length; i++)
            {
                var slot = (CostumeSlot)i;
                bool sel = slot == _slot;
                RowButton(tabs, names[i], () => { _slot = slot; Refresh(); }, i * 134f, 128,
                    sel ? UIFactory.TabOn : UIFactory.TabOff, true, 18);
            }

            string worn = p.Worn(_slot);
            foreach (var c in CostumeDatabase.InSlot(_slot).Where(c => !c.Reward || p.OwnsCostume(c)))
            {
                var row = rows.Next(66, 4);
                UIFactory.RowBg(row, c.Id == worn);
                var swImg = UIFactory.Shape(row, c.Style < 0 ? new Color(0.3f, 0.3f, 0.3f, 0.6f) : c.Color, UIFactory.Pill, "Swatch", 8f);
                UIFactory.AddBorder(swImg, new Color(1f, 1f, 1f, 0.25f), 1.5f);
                var sw = swImg.rectTransform;
                sw.anchorMin = sw.anchorMax = new Vector2(0, 0.5f);
                sw.anchoredPosition = new Vector2(30, 0);
                sw.sizeDelta = new Vector2(38, 38);
                var label = Cell(row, $"<b>{c.Name}</b>{(c.Reward ? "  <color=#ff9070>보상</color>" : "")}\n<size=14><color=#a0d0a0>{c.StatLine}</color>" +
                                     (c.Desc.Length > 0 ? $"  <color=#8090a0>{c.Desc}</color>" : "") + "</size>", 60, 520, 19);
                label.alignment = UIFactory.Align(TextAnchor.MiddleLeft);

                bool owned = p.OwnsCostume(c);
                if (c.Id == worn) RowButton(row, "입는 중", null, 600, 210, UIFactory.TabOn, false);
                else if (owned) RowButton(row, "입기", () => { p.Wear(c); Game.Sound.Coin(); }, 600, 210);
                else if (p.Data.level < c.Level) RowButton(row, $"Lv.{c.Level} 필요 · {c.Price:N0}G", null, 600, 210, null, false, 15);
                else RowButton(row, $"구매 {c.Price:N0}골드", () => Buy(c), 600, 210, UIFactory.BtnGold, p.Data.gold >= c.Price);
            }
        }

        static float[] _max;

        /// <summary>능력치를 0~100으로: 부위마다 가장 좋은 의상을 골라 입었을 때의 합을 100으로 본다.</summary>
        static float[] Normalized(CostumeStats st)
        {
            if (_max == null)
            {
                _max = new float[6];
                foreach (CostumeSlot slot in System.Enum.GetValues(typeof(CostumeSlot)))
                {
                    var list = CostumeDatabase.InSlot(slot).ToList();
                    if (list.Count == 0) continue;
                    _max[0] += list.Max(c => c.Stamina);
                    _max[1] += list.Max(c => c.BigFish);
                    _max[2] += list.Max(c => c.Damage);
                    _max[3] += list.Max(c => c.Counter);
                    _max[4] += list.Max(c => c.Cast);
                    _max[5] += list.Max(c => c.Drain);
                }
            }
            float[] v = { st.Stamina, st.BigFish, st.Damage, st.Counter, st.Cast, st.Drain };
            for (int i = 0; i < v.Length; i++) v[i] = _max[i] > 0f ? Mathf.Clamp(v[i] / _max[i] * 100f, 0f, 100f) : 0f;
            return v;
        }

        void Buy(CostumeDef c)
        {
            var p = Game.Profile;
            if (!p.TrySpend(c.Price)) { Game.Toast("골드가 부족합니다.", ToastKind.Warn); return; }
            p.GiveCostume(c.Id);
            p.Wear(c);
            Game.Sound.Coin();
            Game.Toast($"{c.Name}을(를) 사서 입었습니다. ({c.StatLine})", ToastKind.Good);
        }
    }
}
