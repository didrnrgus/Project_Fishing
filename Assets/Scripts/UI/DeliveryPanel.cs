using System.Collections.Generic;
using System.Linq;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 납품 DELIVERY 창 (원작 2013 홍현 영상 1:36~2:04, 필드 납품소 · 대기실 납품 버튼).
    ///  - 왼쪽: 살림망 고기 카드 3×4칸 (어종 · cm · kg · 아이콘), 왼쪽 세로 페이지 번호. 누르면 선택(오른쪽 아래 네모).
    ///    대물·월척은 도장. 진행 중인 퀘스트가 요구하는 어종은 초록 테두리·초록 이름·"퀘스트" 태그로 알려 주기만 하고 막지 않는다
    ///    (2026-10-21 사용자 결정: 원작의 "퀘스트어종 비활성화" 잠금은 뺐다 — 퀘스트 고기도 납품할 수 있다).
    ///  - 위: "전체선택" 체크.
    ///  - 오른쪽: 세로 원통 납품 게이지(%), 고른 고기 값 "10,014 GOLD", "X2 납품게이지" 버튼, "납품하기".
    /// 값·게이지 규칙은 <see cref="Delivery"/>.
    /// </summary>
    public class DeliveryPanel : UIPanel
    {
        const int Cols = 3, RowsPerPage = 4, PerPage = Cols * RowsPerPage;
        const float CardW = 186f, CardH = 146f, Gap = 10f, GridX = 52f, GridY = -50f;
        const float SideX = 650f, TubeH = 340f, TubeW = 130f;

        static readonly Color QuestGreen = new Color(0.35f, 0.85f, 0.35f);
        static readonly Color Liquid = new Color(0.25f, 0.65f, 1f, 0.95f);

        readonly HashSet<CaughtFish> _selected = new HashSet<CaughtFish>();
        int _page;
        bool _useX2 = true;

        public DeliveryPanel(Transform canvas)
        {
            BuildFrame(canvas, "DeliveryPanel", "납품", new Vector2(1060, 780));
        }

        public override void Open()
        {
            _selected.Clear();
            _page = 0;
            _useX2 = true;
            base.Open();
        }

        /// <summary>자동 촬영용: 앞에서 n마리를 고른다.</summary>
        public void DebugSelect(int n)
        {
            _selected.Clear();
            foreach (var f in Game.Profile.Data.keepNet.Take(n)) _selected.Add(f);
            if (IsOpen) Refresh();
        }

        public override void Refresh()
        {
            var p = Game.Profile;
            var net = p.Data.keepNet;
            _selected.RemoveWhere(f => !net.Contains(f));
            int pages = Mathf.Max(1, Mathf.CeilToInt(net.Count / (float)PerPage));
            _page = Mathf.Clamp(_page, 0, pages - 1);
            Title.text = $"납품 <size=17><color=#8fa3b5>DELIVERY</color></size>   <size=20><color=#ffd060>{p.Data.gold:N0} GOLD</color></size>";
            UIFactory.ClearChildren(Body);

            BuildTopBar(p, net);
            BuildPages(pages);
            BuildGrid(net);
            BuildSide(net);
        }

        // ---------------- 위: 전체선택 ----------------

        void BuildTopBar(PlayerProfile p, List<CaughtFish> net)
        {
            var bar = UIFactory.Node("Top", Body).At(new Vector2(0, 1), new Vector2(0, 0), new Vector2(SideX - 20, 36));
            bool all = net.Count > 0 && net.All(_selected.Contains);
            CheckBox(bar, "전체선택", all, () =>
            {
                if (all) _selected.Clear();
                else foreach (var f in net) _selected.Add(f);
                Refresh();
            }, GridX, 130);
            var count = UIFactory.Text(bar, $"살림망 {net.Count}/{p.KeepNetSize}", 15, UIFactory.TextDim, TextAnchor.MiddleRight);
            count.rectTransform.Fill(GridX + 380, 0, 0, 0);
        }

        void CheckBox(RectTransform parent, string label, bool on, System.Action toggle, float x, float w)
        {
            var hit = UIFactory.Shape(parent, new Color(1f, 1f, 1f, 0f), 6f, "Check", 0f);
            hit.raycastTarget = true;
            hit.rectTransform.At(new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(w, 32), new Vector2(0, 0.5f));
            var btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { if (Game.Sound != null) Game.Sound.UiClick(); toggle(); });
            var box = UIFactory.Shape(hit.transform, new Color(1f, 1f, 1f, 0.08f), 5f, "Box", 0f);
            box.rectTransform.At(new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2(22, 22), new Vector2(0, 0.5f));
            UIFactory.AddBorder(box, new Color(1f, 1f, 1f, 0.4f), 1.2f);
            if (on) UIFactory.Shape(box.transform, UIFactory.Accent, 3f, "On", 0f).rectTransform.Fill(5, 5, 5, 5);
            UIFactory.Text(hit.transform, label, 16, on ? UIFactory.TextMain : UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Fill(32, 0, 0, 0);
        }

        // ---------------- 왼쪽: 페이지 번호 ----------------

        void BuildPages(int pages)
        {
            for (int i = 0; i < pages; i++)
            {
                int page = i;
                var b = UIFactory.Button(Body, (i + 1).ToString(), () => { _page = page; Refresh(); }, i == _page ? UIFactory.TabOn : UIFactory.TabOff, 18);
                ((RectTransform)b.transform).At(new Vector2(0, 1), new Vector2(0, GridY - i * 52f), new Vector2(40, 46));
            }
        }

        // ---------------- 고기 카드 ----------------

        void BuildGrid(List<CaughtFish> net)
        {
            if (net.Count == 0)
            {
                var empty = UIFactory.Text(Body, "살림망이 비어 있습니다.\n<size=15><color=#8fa3b5>물고기를 낚아 살림망에 보관한 뒤 납품하세요.</color></size>", 20,
                    UIFactory.TextDim, TextAnchor.MiddleCenter);
                empty.rectTransform.At(new Vector2(0, 1), new Vector2(GridX, GridY), new Vector2(Cols * CardW + (Cols - 1) * Gap, RowsPerPage * CardH));
                return;
            }
            int start = _page * PerPage;
            for (int i = start; i < net.Count && i < start + PerPage; i++)
            {
                int k = i - start;
                Card(net[i], k % Cols, k / Cols);
            }
        }

        void Card(CaughtFish f, int col, int row)
        {
            var s = FishDatabase.Get(f.speciesId);
            if (s == null) return;
            bool quest = Delivery.IsQuestSpecies(f.speciesId);
            bool picked = _selected.Contains(f);

            var img = UIFactory.Shape(Body, picked ? new Color(0.36f, 0.88f, 0.8f, 0.16f) : UIFactory.Surface, UIFactory.RadiusCard, "Card", 0f);
            img.raycastTarget = true;
            var card = img.rectTransform.At(new Vector2(0, 1), new Vector2(GridX + col * (CardW + Gap), GridY - row * (CardH + Gap)), new Vector2(CardW, CardH));
            if (picked) UIFactory.AddBorder(img, UIFactory.Accent, 2f);
            else if (quest) UIFactory.AddBorder(img, QuestGreen, 2f);
            else UIFactory.AddBorder(img, new Color(1f, 1f, 1f, 0.12f), 1f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                if (Game.Sound != null) Game.Sound.UiClick();
                if (!_selected.Remove(f)) _selected.Add(f);
                Refresh();
            });

            // 이름 · 길이 · 무게 (퀘스트 어종은 초록 이름)
            UIFactory.Text(card, s.Name, 17, quest ? QuestGreen : UIFactory.TextMain, TextAnchor.UpperCenter, FontStyle.Bold).rectTransform.Fill(6, 6, 8, 0);
            UIFactory.Text(card, $"{f.length:0.00} cm   {f.weight / 1000f:0.00} kg", 13, UIFactory.TextDim, TextAnchor.UpperCenter).rectTransform.Fill(6, 6, 31, 0);

            var icon = Fishing.Angling.FishModels.Icon(s);
            if (icon != null)
            {
                var fishImg = UIFactory.Image(card, Color.white, icon, "Icon");
                fishImg.preserveAspect = true;
                fishImg.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(CardW - 36, 82), new Vector2(0.5f, 0));
            }
            else
            {
                var fishImg = UIFactory.Image(card, new Color(1f, 1f, 1f, 0.7f), UIFactory.Fish, "Icon");
                fishImg.preserveAspect = true;
                fishImg.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(CardW - 70, 70), new Vector2(0.5f, 0));
            }

            // 값 (왼쪽 아래)
            UIFactory.Text(card, $"{PlayerProfile.SellPrice(f):N0} G", 12, UIFactory.Gold, TextAnchor.LowerLeft, FontStyle.Bold).rectTransform.Fill(10, 0, 0, 7);

            // 대물·월척 도장 (원작: 카드 오른쪽 아래 붉은 붓글씨 도장)
            if (s.IsTrophy(f.length)) Stamp(card, s.TrophyName);

            // 선택 네모 (오른쪽 아래)
            var box = UIFactory.Shape(card, new Color(0f, 0f, 0f, 0.35f), 4f, "Pick", 0f);
            box.rectTransform.At(new Vector2(1, 0), new Vector2(-8, 8), new Vector2(20, 20), new Vector2(1, 0));
            UIFactory.AddBorder(box, new Color(1f, 1f, 1f, 0.45f), 1.2f);
            if (picked) UIFactory.Shape(box.transform, UIFactory.Accent, 2f, "On", 0f).rectTransform.Fill(4, 4, 4, 4);

            // 퀘스트 어종: 왼쪽 위 "퀘스트" 태그 (알려 주기만, 납품은 막지 않는다)
            if (quest)
            {
                var chip = UIFactory.Chip(card, "퀘스트", new Color(0.2f, 0.5f, 0.2f, 0.95f), Color.white, 11);
                ((RectTransform)chip.transform.parent).At(new Vector2(0, 1), new Vector2(6, -6), new Vector2(48, 18));
            }
        }

        /// <summary>둥근 도장 (기울어진 붉은 테 + 글자).</summary>
        static void Stamp(RectTransform card, string text)
        {
            var c = new Color(0.9f, 0.22f, 0.2f, 0.85f);
            var ring = UIFactory.Image(card, c, UIFactory.Ring, "Stamp");
            ring.rectTransform.At(new Vector2(1, 0), new Vector2(-30, 30), new Vector2(66, 66), new Vector2(1, 0));
            ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var t = UIFactory.Text(ring.transform, text, 19, c, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Fill();
        }

        // ---------------- 오른쪽: 게이지 · 금액 · 버튼 ----------------

        void BuildSide(List<CaughtFish> net)
        {
            var sideImg = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "Side", 0f);
            var side = sideImg.rectTransform;
            side.anchorMin = new Vector2(0, 0);
            side.anchorMax = new Vector2(1, 1);
            side.offsetMin = new Vector2(SideX, 0);
            side.offsetMax = Vector2.zero;

            var picked = net.Where(_selected.Contains).ToList();
            int price = Delivery.Price(picked);
            bool full = Delivery.GaugeFull;
            bool x2 = full && _useX2;
            int pay = x2 ? price * 2 : price;
            float gauge = Delivery.Gauge;
            float add = x2 ? 0f : Mathf.Clamp01(gauge + price / (float)Delivery.GaugeCapacity) - gauge;

            UIFactory.Text(side, "납품 게이지", 15, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(300, 26));

            // 원통: 어두운 유리관 안에 하늘색 물이 차오른다. 고른 고기로 더 찰 만큼은 옅게 미리 보여 준다
            var tubeImg = UIFactory.Shape(side, new Color(0.02f, 0.05f, 0.08f, 0.85f), 28f, "Tube", 0f);
            UIFactory.AddBorder(tubeImg, new Color(1f, 1f, 1f, 0.28f), 1.5f);
            var tube = tubeImg.rectTransform.At(new Vector2(0, 1), new Vector2(46, -46), new Vector2(TubeW, TubeH));
            if (add > 0.001f) LiquidLevel(tube, gauge + add, new Color(Liquid.r, Liquid.g, Liquid.b, 0.32f), "Preview");
            if (gauge > 0.001f) LiquidLevel(tube, gauge, full ? new Color(1f, 0.78f, 0.3f, 0.95f) : Liquid, "Level");
            // 유리 반사 줄
            UIFactory.Image(tube, new Color(1f, 1f, 1f, 0.1f), null, "Shine").rectTransform
                .At(new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(14, TubeH - 40), new Vector2(0, 0.5f));
            // 위 뚜껑 · 아래 받침
            UIFactory.Shape(side, new Color(0.55f, 0.62f, 0.7f, 0.9f), UIFactory.Pill, "Cap", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(40, -38), new Vector2(TubeW + 12, 14));
            UIFactory.Shape(side, new Color(0.55f, 0.62f, 0.7f, 0.9f), UIFactory.Pill, "Base", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(40, -46 - TubeH + 6), new Vector2(TubeW + 12, 14));

            // 눈금 0~100
            for (int v = 0; v <= 100; v += 10)
            {
                float y = -46 - 8 - (TubeH - 16) * (1f - v / 100f);
                UIFactory.Image(side, new Color(1f, 1f, 1f, 0.3f), null, "Tick").rectTransform
                    .At(new Vector2(0, 1), new Vector2(46 + TubeW + 8, y), new Vector2(v % 50 == 0 ? 12 : 7, 1.5f), new Vector2(0, 0.5f));
                UIFactory.Text(side, v.ToString(), 12, UIFactory.TextFaint, TextAnchor.MiddleLeft).rectTransform
                    .At(new Vector2(0, 1), new Vector2(46 + TubeW + 24, y), new Vector2(40, 16), new Vector2(0, 0.5f));
            }
            // 오른쪽 설명
            var info = UIFactory.Text(side, $"납품한 금액만큼\n게이지가 찹니다.\n\n<color=#ffd060>가득 차면</color>\n다음 납품 1회\n<color=#ffd060>두 배</color>로 받고\n게이지는 비워집니다.\n\n<size=12><color=#8fa3b5>한 통 {Delivery.GaugeCapacity:N0} GOLD</color></size>",
                13, UIFactory.TextDim, TextAnchor.UpperLeft);
            info.rectTransform.At(new Vector2(0, 1), new Vector2(46 + TubeW + 64, -60), new Vector2(120, 300));

            // 퍼센트
            UIFactory.Text(side, $"{gauge * 100f:0}%" + (add > 0.001f ? $"  <size=14><color=#7fc8ff>→ {(gauge + add) * 100f:0}%</color></size>" : ""),
                    20, full ? UIFactory.Gold : UIFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(46, -46 - TubeH - 6), new Vector2(280, 30));

            // 받을 금액
            var goldBox = UIFactory.Shape(side, new Color(0f, 0f, 0f, 0.35f), 10f, "GoldBox", 0f);
            UIFactory.AddBorder(goldBox, new Color(1f, 0.82f, 0.35f, 0.35f), 1.2f);
            var gb = goldBox.rectTransform;
            gb.anchorMin = new Vector2(0, 1); gb.anchorMax = new Vector2(1, 1); gb.pivot = new Vector2(0.5f, 1);
            gb.offsetMin = new Vector2(16, -474 - 52); gb.offsetMax = new Vector2(-16, -474);
            UIFactory.Text(gb, $"선택 {picked.Count}마리" + (x2 ? "  <color=#ffd060>X2</color>" : ""), 13, UIFactory.TextDim, TextAnchor.MiddleLeft).rectTransform.Fill(12, 0, 0, 0);
            UIFactory.Text(gb, $"{pay:N0} GOLD", 24, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Fill(0, 14, 0, 0);

            // X2 납품게이지 (가득 차면 켜고 끌 수 있다: 큰 납품에 아껴 둘 때)
            string x2Label = !full ? $"<b>X2</b>  납품게이지  <size=14><color=#8fa3b5>{gauge * 100f:0}%</color></size>"
                : _useX2 ? "<b>X2</b>  납품게이지  <size=14>· 이번 납품에 사용</size>"
                : "<b>X2</b>  납품게이지  <size=14><color=#c0c8d0>· 아껴 두기</color></size>";
            var x2Btn = UIFactory.Button(side, x2Label, () => { _useX2 = !_useX2; Refresh(); }, full && _useX2 ? UIFactory.BtnGold : UIFactory.BtnNeutral, 18);
            x2Btn.interactable = full;
            Stretch((RectTransform)x2Btn.transform, -534, 48);

            var go = UIFactory.Button(side, "납품하기", DeliverSelected, UIFactory.BtnPrimary, 21);
            go.interactable = picked.Count > 0;
            Stretch((RectTransform)go.transform, -592, 56);
        }

        static void Stretch(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(16, top - height); rt.offsetMax = new Vector2(-16, top);
        }

        /// <summary>원통 안 물: 아래부터 level만큼 잘라 보여서 바닥은 둥글고 수면은 평평하다.</summary>
        static void LiquidLevel(RectTransform tube, float level, Color color, string name)
        {
            const float inset = 6f;
            float inner = TubeH - inset * 2f;
            var clip = UIFactory.Node(name, tube);
            clip.anchorMin = new Vector2(0, 0); clip.anchorMax = new Vector2(1, 0); clip.pivot = new Vector2(0.5f, 0);
            clip.anchoredPosition = new Vector2(0, inset);
            clip.sizeDelta = new Vector2(-inset * 2f, inner * Mathf.Clamp01(level));
            clip.gameObject.AddComponent<RectMask2D>();
            var water = UIFactory.Shape(clip, color, 22f, "Water", 0f).rectTransform;
            water.anchorMin = new Vector2(0, 0); water.anchorMax = new Vector2(1, 0); water.pivot = new Vector2(0.5f, 0);
            water.anchoredPosition = Vector2.zero;
            water.sizeDelta = new Vector2(0, inner);
            // 수면 빛
            if (level < 0.995f)
                UIFactory.Image(clip, new Color(1f, 1f, 1f, 0.35f), null, "Surface").rectTransform
                    .At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(TubeW - inset * 2f - 8f, 2f), new Vector2(0.5f, 1));
        }

        void DeliverSelected()
        {
            if (_selected.Count == 0) return;
            var picked = _selected.ToList();
            _selected.Clear();
            Delivery.Deliver(picked, _useX2);
            _useX2 = true;
            Refresh();
        }
    }
}
