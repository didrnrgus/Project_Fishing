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

        public DexPanel(Transform canvas)
        {
            BuildFrame(canvas, "DexPanel", "어류 도감", new Vector2(1320, 820));
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var species = FishDatabase.DexSpecies.ToList();
            Title.text = $"어류 도감 · 한강  ({p.Data.dex.Count}/{species.Count})";
            UIFactory.ClearChildren(Body);

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
