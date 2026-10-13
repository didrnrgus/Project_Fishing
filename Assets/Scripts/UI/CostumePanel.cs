using System.Linq;
using Fishing.Core;
using Fishing.Data;
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
        Text _stats;

        public CostumePanel(Transform canvas)
        {
            BuildFrame(canvas, "CostumePanel", "의상", new Vector2(1320, 800));
            var cam = PortraitCamera.FullBody ?? PortraitCamera.CreateFullBody();

            var previewFrame = UIFactory.Image(Body, new Color(0.14f, 0.2f, 0.26f), null, "PreviewFrame").rectTransform
                .At(new Vector2(0, 1), Vector2.zero, new Vector2(420, 560), new Vector2(0, 1));
            var raw = UIFactory.Node("Preview", previewFrame).Fill(4, 4, 4, 4).gameObject.AddComponent<RawImage>();
            raw.texture = cam.Target;
            raw.raycastTarget = false;
            var spinL = UIFactory.Button(previewFrame, "◀", () => cam.Spin -= 45f, new Color(0, 0, 0, 0.5f), 20);
            ((RectTransform)spinL.transform).At(new Vector2(0, 0), new Vector2(10, 10), new Vector2(48, 40), new Vector2(0, 0));
            var spinR = UIFactory.Button(previewFrame, "▶", () => cam.Spin += 45f, new Color(0, 0, 0, 0.5f), 20);
            ((RectTransform)spinR.transform).At(new Vector2(1, 0), new Vector2(-10, 10), new Vector2(48, 40), new Vector2(1, 0));

            _stats = UIFactory.Text(Body, "", 16, UIFactory.TextMain, TextAnchor.UpperLeft);
            _stats.rectTransform.At(new Vector2(0, 1), new Vector2(0, -572), new Vector2(420, 130), new Vector2(0, 1));

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
            _stats.text = $"<b>입은 의상 능력치</b>\n" +
                          $"최대 체력 {p.StaminaCap:0} (+{st.Stamina:0})   ·   대물 +{st.BigFish * 100f:0}%\n" +
                          $"데미지 +{st.Damage * 100f:0}%   ·   카운터 +{st.Counter * 100f:0}%\n" +
                          $"비거리 +{st.Cast:0.0}m   ·   체력 소모 -{st.Drain * 100f:0}%";

            UIFactory.ClearChildren(_list);
            var rows = new RowCursor(_list);
            var tabs = rows.Next(42, 10);
            string[] names = { "모자", "안경", "상의", "조끼", "하의", "신발" };
            for (int i = 0; i < names.Length; i++)
            {
                var slot = (CostumeSlot)i;
                bool sel = slot == _slot;
                RowButton(tabs, names[i], () => { _slot = slot; Refresh(); }, i * 134f, 128,
                    sel ? new Color(0.15f, 0.45f, 0.42f) : new Color(0.15f, 0.18f, 0.22f), true, 18);
            }

            string worn = p.Worn(_slot);
            foreach (var c in CostumeDatabase.InSlot(_slot).Where(c => !c.Reward || p.OwnsCostume(c)))
            {
                var row = rows.Next(66, 4);
                UIFactory.Image(row, new Color(1, 1, 1, c.Id == worn ? 0.1f : 0.04f), null, "Bg").rectTransform.Fill();
                var sw = UIFactory.Image(row, c.Style < 0 ? new Color(0.3f, 0.3f, 0.3f, 0.6f) : c.Color, UIFactory.Circle, "Swatch").rectTransform;
                sw.anchorMin = sw.anchorMax = new Vector2(0, 0.5f);
                sw.anchoredPosition = new Vector2(30, 0);
                sw.sizeDelta = new Vector2(38, 38);
                var label = Cell(row, $"<b>{c.Name}</b>{(c.Reward ? "  <color=#ff9070>보상</color>" : "")}\n<size=14><color=#a0d0a0>{c.StatLine}</color>" +
                                     (c.Desc.Length > 0 ? $"  <color=#8090a0>{c.Desc}</color>" : "") + "</size>", 60, 520, 19);
                label.alignment = TextAnchor.MiddleLeft;

                bool owned = p.OwnsCostume(c);
                if (c.Id == worn) RowButton(row, "입는 중", null, 600, 210, new Color(0.15f, 0.45f, 0.42f), false);
                else if (owned) RowButton(row, "입기", () => { p.Wear(c); Game.Sound.Coin(); }, 600, 210);
                else if (p.Data.level < c.Level) RowButton(row, $"Lv.{c.Level} 필요 · {c.Price:N0}G", null, 600, 210, null, false, 15);
                else RowButton(row, $"구매 {c.Price:N0}골드", () => Buy(c), 600, 210, new Color(0.45f, 0.35f, 0.12f), p.Data.gold >= c.Price);
            }
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
