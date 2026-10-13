using System.Linq;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 어류 도감. 원작처럼 같은 종을 여러 번 잡아야 정보가 열린다.
    /// 1마리: 이름·성질·기록 / 3마리: 선호 미끼 / 5마리: 활동 시간·서식지
    /// </summary>
    public class DexPanel : UIPanel
    {
        public const int BaitUnlock = 3;
        public const int HabitatUnlock = 5;

        string _tab;

        public DexPanel(Transform canvas)
        {
            BuildFrame(canvas, "DexPanel", "어류 도감", new Vector2(1320, 820));
        }

        public override void Open()
        {
            _tab = Fishing.World.FieldMap.Id;
            base.Open();
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            _tab ??= Fishing.World.FieldMap.Id;
            foreach (Transform child in Frame)
                if (child.name == "DexTab") Object.Destroy(child.gameObject);
            var species = FishDatabase.DexOf(_tab).ToList();
            int known = species.Count(s => p.Dex(s.Id) != null);
            Title.text = $"어류 도감 · {Fishing.World.Maps.Get(_tab).ShortName}  ({known}/{species.Count})";
            UIFactory.ClearChildren(Body);

            // 낚시터 탭
            var tabs = new[] { Fishing.World.Maps.HanRiver, Fishing.World.Maps.Honghyeon };
            for (int t = 0; t < tabs.Length; t++)
            {
                var map = tabs[t];
                bool sel = map.Id == _tab;
                var b = UIFactory.Button(Frame, map.ShortName, () => { _tab = map.Id; Refresh(); },
                    sel ? new Color(0.15f, 0.45f, 0.42f) : new Color(0.15f, 0.18f, 0.22f), 17);
                ((RectTransform)b.transform).At(new Vector2(1, 1), new Vector2(-70 - (tabs.Length - 1 - t) * 150, -16), new Vector2(140, 36));
                b.gameObject.name = "DexTab";
            }

            const int cols = 5;
            const float w = 246f, h = 236f, gap = 8f;
            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                var entry = p.Dex(s.Id);
                int col = i % cols, row = i / cols;
                var card = UIFactory.Panel(Body, "Card", entry != null ? UIFactory.PanelBgLight : new Color(0.08f, 0.1f, 0.13f, 0.9f)).rectTransform;
                card.At(new Vector2(0, 1), new Vector2(col * (w + gap), -row * (h + gap)), new Vector2(w, h));

                Cell(card, $"No.{i + 1:00}", 10, 60, 13, UIFactory.TextDim, TextAnchor.UpperLeft).rectTransform.offsetMax = new Vector2(70, -8);
                if (entry == null)
                {
                    var q = UIFactory.Text(card, "???", 40, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    q.rectTransform.Fill();
                    var rarity = UIFactory.Text(card, new string('★', s.Rarity), 14, new Color(1, 0.8f, 0.3f, 0.4f), TextAnchor.LowerCenter);
                    rarity.rectTransform.Fill(0, 0, 0, 10);
                    continue;
                }

                bool trophy = s.IsTrophy(entry.maxLength);
                var lines = $"<b><size=22>{s.Name}</size></b>  <color=#ffd060>{new string('★', s.Rarity)}</color>\n" +
                            $"<size=12><i>{s.Latin}</i></size>\n" +
                            $"<color=#a0b0c0>{s.Temper}</color>\n\n" +
                            $"낚은 수  {entry.count}마리\n" +
                            $"최대  {entry.maxLength:0.0}cm" + (trophy ? $" <color=#ff7090>{s.TrophyName}</color>" : "") + "\n" +
                            $"길이  {s.MinLen:0}~{s.MaxLen:0}cm\n";
                lines += entry.count >= BaitUnlock
                    ? $"미끼  {string.Join(", ", s.Baits.Select(Names.Of))}\n"
                    : $"<color=#607080>미끼  {BaitUnlock}마리 낚으면 공개</color>\n";
                lines += entry.count >= HabitatUnlock
                    ? $"시간  {string.Join(", ", s.Times.Select(Names.Of))}\n장소  {string.Join(", ", s.Habitats.Select(Names.Of))}"
                    : $"<color=#607080>시간/장소  {HabitatUnlock}마리 낚으면 공개</color>";

                var text = UIFactory.Text(card, lines, 15, UIFactory.TextMain, TextAnchor.UpperLeft);
                text.rectTransform.Fill(12, 10, 26, 8);
            }
        }
    }
}
