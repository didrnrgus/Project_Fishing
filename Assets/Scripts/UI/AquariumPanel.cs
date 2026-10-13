using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 수족관 창 (대기실). 왼쪽엔 카메라가 수조를 비추고, 오른쪽 창에서 물고기를 넣고·놓아주고·먹이 주고·장식을 산다.
    /// 먹이: 가방의 미끼 1개로 배고픈 물고기 한 마리를 배불리고, 경험치를 조금 얻는다 (원작 수족관과 같다).
    /// </summary>
    public class AquariumPanel : UIPanel
    {
        public AquariumPanel(Transform canvas)
        {
            BuildFrame(canvas, "AquariumPanel", "수족관", new Vector2(760, 860));
            Frame.anchoredPosition = new Vector2(520f, 0f);
            // 수조가 보이도록 뒤를 어둡게 덮지 않는다
            Root.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0f);
        }

        public override void Open()
        {
            base.Open();
            if (Game.Lobby != null && Game.Lobby.Tank != null)
                Game.CameraRig.SetFixed(Game.Lobby.Tank.ViewPos, Game.Lobby.Tank.ViewRot);
        }

        public override void Close()
        {
            base.Close();
            if (Game.Mode == GameMode.Lobby && Game.Lobby != null) Game.CameraRig.SetFixed(Game.Lobby.CameraPos, Game.Lobby.CameraRot);
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var d = p.Data;
            Title.text = $"수족관  <size=18><color=#a0b0c0>{d.aquarium.Count}/{Aquarium.Capacity}마리 · 장식 {d.aquariumDecor.Count}/{Aquarium.MaxDecor}</color></size>";
            UIFactory.ClearChildren(Body);
            var rows = new RowCursor(Body);

            Cell(rows.Next(26), "수조 속 물고기", 4, 300, 19, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (d.aquarium.Count == 0) Cell(rows.Next(26), "비어 있습니다. 아래 살림망 물고기를 넣어 보세요.", 14, 680, 16, UIFactory.TextDim);
            int hungry = 0;
            for (int i = 0; i < d.aquarium.Count; i++)
            {
                var f = d.aquarium[i];
                var s = FishDatabase.Get(f.speciesId);
                float h = Aquarium.Hunger(f);
                if (h >= 60f) hungry++;
                var row = rows.Next(34, 3);
                UIFactory.Image(row, new Color(1, 1, 1, 0.04f), null, "Bg").rectTransform.Fill();
                Cell(row, $"{s?.Name}  <size=14>{f.length:0.0}cm</size>", 10, 250, 17);
                Cell(row, h >= 60f ? $"<color=#ff9070>배고픔 {h:0}%</color>" : $"<color=#70e070>배부름</color> <size=13>{h:0}%</size>", 260, 160, 15);
                int index = i;
                RowButton(row, "먹이", () => Feed(index), 430, 110, new Color(0.2f, 0.4f, 0.3f), h >= 10f, 15);
                RowButton(row, "놓아주기", () => Release(index), 550, 120, new Color(0.3f, 0.25f, 0.25f), true, 15);
            }
            if (hungry > 1)
                RowButton(rows.Next(38, 4), $"배고픈 {hungry}마리 모두 먹이 주기 (미끼 {hungry}개)", FeedAll, 10, 420, new Color(0.2f, 0.45f, 0.3f), true, 16);

            rows.Next(6);
            Cell(rows.Next(26), "살림망에서 옮기기", 4, 300, 19, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            int shownNet = 0;
            for (int i = 0; i < d.keepNet.Count && shownNet < 6; i++)
            {
                var f = d.keepNet[i];
                var s = FishDatabase.Get(f.speciesId);
                if (s == null || s.IsJunk) continue;
                shownNet++;
                var row = rows.Next(30, 2);
                Cell(row, $"{s.Name}  <size=14>{f.length:0.0}cm</size>", 10, 300, 16);
                int index = i;
                RowButton(row, "수족관에 넣기", () => Add(index), 430, 240, new Color(0.18f, 0.38f, 0.5f), d.aquarium.Count < Aquarium.Capacity, 15);
            }
            if (shownNet == 0) Cell(rows.Next(26), "살림망에 물고기가 없습니다.", 14, 680, 16, UIFactory.TextDim);

            rows.Next(6);
            Cell(rows.Next(26), "장식 사기", 4, 300, 19, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            foreach (var e in Aquarium.DecorShop)
            {
                var row = rows.Next(30, 2);
                Cell(row, e.name, 10, 300, 16);
                var entry = e;
                RowButton(row, $"{e.price:N0}골드", () => BuyDecor(entry.id, entry.name, entry.price), 430, 240, new Color(0.45f, 0.35f, 0.12f),
                    d.gold >= e.price && d.aquariumDecor.Count < Aquarium.MaxDecor, 15);
            }
        }

        void Add(int keepIndex)
        {
            var d = Game.Profile.Data;
            if (d.aquarium.Count >= Aquarium.Capacity) return;
            var f = d.keepNet[keepIndex];
            d.keepNet.RemoveAt(keepIndex);
            d.aquarium.Add(new AquaFish { speciesId = f.speciesId, length = f.length, fedAt = Game.Clock.TotalMinutes });
            Game.Toast($"{FishDatabase.Get(f.speciesId)?.Name}을(를) 수족관에 넣었습니다.", ToastKind.Good);
            Changed();
        }

        void Release(int index)
        {
            var d = Game.Profile.Data;
            var f = d.aquarium[index];
            d.aquarium.RemoveAt(index);
            Game.Toast($"{FishDatabase.Get(f.speciesId)?.Name}을(를) 강으로 돌려보냈습니다.");
            Changed();
        }

        /// <summary>가방에서 아무 미끼나 하나 쓴다 (싼 것부터).</summary>
        static bool UseFeed()
        {
            var p = Game.Profile;
            BaitDef best = null;
            foreach (var b in GearDatabase.Baits)
                if (p.Count(b.Id) > 0 && (best == null || b.Price < best.Price)) best = b;
            return best != null && p.ConsumeItem(best.Id);
        }

        void Feed(int index)
        {
            if (!UseFeed()) { Game.Toast("먹이로 줄 미끼가 없습니다.", ToastKind.Warn); return; }
            var p = Game.Profile;
            var f = p.Data.aquarium[index];
            var s = FishDatabase.Get(f.speciesId);
            f.fedAt = Game.Clock.TotalMinutes;
            int exp = 3 + (s?.Rarity ?? 1) * 3;
            p.AddExp(exp);
            Game.Toast($"{s?.Name}에게 먹이를 줬습니다. 경험치 +{exp}", ToastKind.Good);
            Changed();
        }

        void FeedAll()
        {
            var d = Game.Profile.Data;
            for (int i = 0; i < d.aquarium.Count; i++)
                if (Aquarium.Hunger(d.aquarium[i]) >= 60f) Feed(i);
        }

        void BuyDecor(string id, string name, int price)
        {
            var p = Game.Profile;
            if (!p.TrySpend(price)) return;
            p.Data.aquariumDecor.Add(id);
            Game.Sound.Coin();
            Game.Toast($"{name}을(를) 수조에 놓았습니다.", ToastKind.Good);
            Changed();
        }

        void Changed()
        {
            Game.Profile.NotifyChanged();
            if (Game.Lobby != null && Game.Lobby.Tank != null) Game.Lobby.Tank.Rebuild();
            Refresh();
        }
    }
}
