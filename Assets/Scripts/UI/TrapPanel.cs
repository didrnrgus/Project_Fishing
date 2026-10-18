using System;
using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using Fishing.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 통발 설치/수거 INSTALL/HARVEST FISH TRAPS (원작 0:44~0:48, 2:20~2:26). 어부 NPC 대화의 [통발 설치하기]로 연다.
    ///  - 왼쪽: 설치된 통발 (n/2) — 칸마다 원형 통발 (설치 가능 / (진행 중) 남은 시간 / (수거가능) 설치장소·수거 비용)
    ///  - 오른쪽, 빈 칸을 고르면: 설치가능 지역 (n/m) — 장소·요구 레벨, 소요 시간·수거비용·소유금액, [설치하기] → "이대로 설치하여도 좋습니까?" 확인 띠
    ///  - 오른쪽, 다 된 통발을 고르면: 수거 할 물고기 (선택/10) — 어종·cm·kg 카드(퀘스트 어종은 녹색 테두리), [전체선택], [수거하기]
    ///    → 덤 아이템이 있으면 아이템 획득 창(<see cref="ItemBagPopup"/>)
    /// </summary>
    public class TrapPanel : UIPanel
    {
        const float LeftW = 410f;
        static readonly Color HeadColor = new Color(1f, 0.55f, 0.35f);

        RectTransform _left, _right;
        TMP_Text _leftHead;
        GameObject _confirm;
        TMP_Text _confirmValues;

        string _npcName = "";
        int _slot;
        int _spot = -1;
        TrapSave _selFor;
        readonly HashSet<int> _sel = new HashSet<int>();
        // 남은 시간을 1초마다 고쳐 쓰는 칸
        readonly List<(TrapSave t, TMP_Text text, Image bar)> _live = new List<(TrapSave, TMP_Text, Image)>();
        float _nextTick;

        public TrapPanel(Transform canvas)
        {
            BuildFrame(canvas, "TrapPanel", "통발 설치/수거  <size=15><color=#8fa3b5>INSTALL / HARVEST FISH TRAPS</color></size>", new Vector2(1040, 660));
            _leftHead = UIFactory.Text(Body, "", 19, HeadColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            _leftHead.rectTransform.At(new Vector2(0, 1), new Vector2(6, 0), new Vector2(LeftW, 30), new Vector2(0, 1));
            var leftBox = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "LeftBox", 0f).rectTransform;
            leftBox.anchorMin = new Vector2(0, 0);
            leftBox.anchorMax = new Vector2(0, 1);
            leftBox.pivot = new Vector2(0, 1);
            leftBox.offsetMin = new Vector2(0, 0);
            leftBox.offsetMax = new Vector2(LeftW, -38);
            _left = UIFactory.Node("Slots", leftBox).Fill(8, 14, 8, 8);
            _right = UIFactory.Node("Right", Body).Fill(LeftW + 22, 0, 0, 0);
            Root.AddComponent<PanelTicker>().Tick = Tick;
            BuildConfirm();
        }

        /// <summary>어부 대화에서 연다 (설치장소에 어부 이름이 붙는다).</summary>
        public void OpenFrom(string npcName)
        {
            _npcName = npcName ?? "";
            // 다 된 통발이 있으면 그 칸을, 아니면 빈 칸을 먼저 보여 준다
            var traps = FishTraps.All;
            _slot = traps.Count < FishTraps.Slots ? traps.Count : 0;
            for (int i = 0; i < traps.Count; i++) if (FishTraps.Ready(traps[i])) { _slot = i; break; }
            _spot = -1;
            if (_confirm != null) _confirm.SetActive(false);
            if (IsOpen) Refresh();
            else Game.UI.OpenExclusive(this);
        }

        public override void Close()
        {
            if (_confirm != null) _confirm.SetActive(false);
            base.Close();
        }

        // ---------------- 그리기 ----------------

        public override void Refresh()
        {
            _live.Clear();
            var traps = FishTraps.All;
            _slot = Mathf.Clamp(_slot, 0, FishTraps.Slots - 1);
            _leftHead.text = $"설치된 통발 ( {traps.Count}/{FishTraps.Slots} )";

            UIFactory.ClearChildren(_left);
            var rows = new RowCursor(_left, 0f, "TrapSlots");
            for (int i = 0; i < FishTraps.Slots; i++)
                SlotCard(rows.Next(96, 8), i, i < traps.Count ? traps[i] : null);

            UIFactory.ClearChildren(_right);
            var t = _slot < traps.Count ? traps[_slot] : null;
            if (t == null) InstallView();
            else if (FishTraps.Ready(t)) HarvestView(t);
            else ProgressView(t);
        }

        void SlotCard(RectTransform row, int index, TrapSave t)
        {
            bool selected = index == _slot;
            var bg = UIFactory.RowBg(row, selected);
            if (selected) UIFactory.AddBorder(bg, UIFactory.Accent, 1.6f, 4f);
            bg.raycastTarget = true;
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => { Game.Sound?.UiClick(); _slot = index; _spot = -1; Refresh(); });

            TrapIcon(row, 10f, 72f, t == null ? 0.55f : 1f);
            var text = UIFactory.Text(row, "", 15, UIFactory.TextMain, TextAnchor.UpperLeft);
            text.rectTransform.At(new Vector2(0, 1), new Vector2(94, -10), new Vector2(LeftW - 120, 80), new Vector2(0, 1));
            text.lineSpacing = 8f;
            if (t == null)
            {
                text.text = $"<color=#5ce0cc><b>{FishTraps.RoundTrapName}</b></color>  <size=13><color=#8fa3b5>설치 가능</color></size>\n" +
                            $"<color=#a0b0c0>어획량 : {FishTraps.CatchCount}마리\n소요 시간 : {FishTraps.DurationHours:0}시간</color>";
                return;
            }
            var spot = FishTraps.SpotOf(t);
            string place = $"설치장소 : {spot.Name}" + (string.IsNullOrEmpty(t.npcName) ? "" : $"({t.npcName})");
            if (FishTraps.Ready(t))
            {
                text.text = $"<color=#ffd060><b>(수거가능) {FishTraps.RoundTrapName}</b></color>\n<color=#c8d4de>{place}\n수거 비용 : {spot.Cost:N0}GOLD</color>";
                return;
            }
            text.text = $"<color=#7fd8ff><b>(진행 중) {FishTraps.RoundTrapName}</b></color>\n<color=#c8d4de>{place}</color>\n";
            var remain = UIFactory.Text(row, "", 14, UIFactory.TextDim, TextAnchor.MiddleLeft);
            remain.rectTransform.At(new Vector2(0, 0), new Vector2(94, 12), new Vector2(170, 22), new Vector2(0, 0));
            var bar = UIFactory.Bar(row, new Color(1f, 1f, 1f, 0.08f), UIFactory.Accent);
            bar.transform.parent.GetComponent<RectTransform>().At(new Vector2(0, 0), new Vector2(268, 19), new Vector2(LeftW - 300, 8), new Vector2(0, 0));
            _live.Add((t, remain, bar));
            UpdateLive(t, remain, bar);
        }

        /// <summary>원형 통발 그림 대신: 둥근 칸 안에 그물 무늬 (원작 아이콘은 쓰지 않는다).</summary>
        static void TrapIcon(RectTransform row, float x, float size, float alpha)
        {
            var box = UIFactory.Shape(row, new Color(0.08f, 0.32f, 0.33f, 0.9f * alpha), UIFactory.RadiusButton, "TrapIcon", 0f).rectTransform;
            box.At(new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(size, size), new Vector2(0, 0.5f));
            var ring = UIFactory.Image(box, new Color(0.45f, 0.95f, 0.85f, 0.85f * alpha), UIFactory.Ring, "Ring");
            ring.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.7f, size * 0.7f));
            UIFactory.Text(box, "#", size * 0.42f, new Color(0.7f, 1f, 0.95f, alpha), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
        }

        TMP_Text Head(string text)
        {
            var t = UIFactory.Text(_right, text, 19, HeadColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.At(new Vector2(0, 1), new Vector2(6, 0), new Vector2(360, 30), new Vector2(0, 1));
            return t;
        }

        RectTransform Box(float bottom)
        {
            var box = UIFactory.Shape(_right, UIFactory.Surface, UIFactory.RadiusCard, "Box", 0f).rectTransform;
            box.Fill(0, 0, 38, bottom);
            return UIFactory.Node("Inner", box).Fill(8, 14, 8, 8);
        }

        Button BottomButton(string label, Action onClick, bool interactable, Color? bg = null)
        {
            var b = UIFactory.Button(_right, label, onClick, bg ?? UIFactory.BtnPrimary, 20);
            ((RectTransform)b.transform).At(new Vector2(0.5f, 0), Vector2.zero, new Vector2(260, 50), new Vector2(0.5f, 0));
            b.interactable = interactable;
            return b;
        }

        // ---- 설치 ----

        void InstallView()
        {
            var spots = FishTraps.SpotsOf(FieldMap.Id);
            int avail = 0;
            foreach (var s in spots) if (FishTraps.CanInstallAt(s)) avail++;
            if (_spot < 0 || _spot >= spots.Count || !FishTraps.CanInstallAt(spots[_spot]))
            {
                _spot = -1;
                for (int i = 0; i < spots.Count; i++) if (FishTraps.CanInstallAt(spots[i])) { _spot = i; break; }
            }
            Head($"설치가능 지역 ( {avail}/{spots.Count} )");
            var list = Box(184);
            var rows = new RowCursor(list, 0f, "TrapSpots");
            for (int i = 0; i < spots.Count; i++)
            {
                var s = spots[i];
                var row = rows.Next(64, 6);
                bool ok = FishTraps.CanInstallAt(s);
                bool selected = i == _spot;
                var bg = UIFactory.RowBg(row, selected);
                if (selected) UIFactory.AddBorder(bg, UIFactory.Accent, 1.6f, 4f);
                if (ok)
                {
                    bg.raycastTarget = true;
                    var btn = bg.gameObject.AddComponent<Button>();
                    btn.targetGraphic = bg;
                    int index = i;
                    btn.onClick.AddListener(() => { Game.Sound?.UiClick(); _spot = index; Refresh(); });
                }
                string state = FishTraps.Occupied(s) ? "  <size=13><color=#ffd060>(설치됨)</color></size>" : "";
                string lv = FishTraps.LevelOk(s) ? $"요구 레벨 : Lv.{s.Level}이상 가능" : $"<color=#ff7a6a>요구 레벨 : Lv.{s.Level}이상 가능</color>";
                var t = UIFactory.Text(row, $"<color={(ok ? "#5ce0cc" : "#7f8c98")}><b>{s.Name}</b></color>{state}\n<size=14><color=#c8d4de>{lv}</color></size>",
                    17, UIFactory.TextMain, TextAnchor.MiddleLeft);
                t.rectTransform.Fill(14, 10, 4, 4);
            }

            // 소요 시간 · 수거비용 · 소유금액
            var info = UIFactory.Shape(_right, UIFactory.Surface, UIFactory.RadiusCard, "Info", 0f).rectTransform;
            info.At(new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(0, 108), new Vector2(0.5f, 0));
            info.anchorMin = new Vector2(0, 0);
            info.anchorMax = new Vector2(1, 0);
            info.sizeDelta = new Vector2(0, 108);
            var sp = _spot >= 0 ? spots[_spot] : spots[0];
            int gold = Game.Profile.Data.gold;
            var keys = UIFactory.Text(info, "소요 시간 :\n수거비용 :\n소유금액 :", 16, UIFactory.TextDim, TextAnchor.MiddleLeft);
            keys.rectTransform.Fill(16, 0, 8, 8);
            keys.lineSpacing = 10f;
            var vals = UIFactory.Text(info, $"<color=#5ce0cc>{FishTraps.DurationHours:0}시간</color>\n<color=#7fd8ff>{sp.Cost:N0} GOLD</color>\n" +
                                            $"<color={(gold >= sp.Cost ? "#7fd8ff" : "#ff7a6a")}>{gold:N0} GOLD</color>", 16, UIFactory.TextMain, TextAnchor.MiddleRight);
            vals.rectTransform.Fill(0, 16, 8, 8);
            vals.lineSpacing = 10f;

            string why = FishTraps.InstalledCount >= FishTraps.Slots ? "통발을 모두 설치했습니다" : avail == 0 ? "설치할 수 있는 지역이 없습니다" : null;
            BottomButton(why ?? "설치하기", ShowConfirm, why == null && _spot >= 0);
        }

        void BuildConfirm()
        {
            // 원작: 창 위를 가로지르는 어두운 띠에 "이대로 설치하여도 좋습니까?" + 설치 장소·수거비용·소요 시간·어획량 + 확인/취소
            var block = UIFactory.Image(Root.transform, new Color(0f, 0f, 0f, 0.25f), null, "ConfirmBlock");
            block.raycastTarget = true;
            block.rectTransform.Fill();
            _confirm = block.gameObject;
            var band = UIFactory.Image(block.transform, new Color(0.02f, 0.03f, 0.05f, 0.92f), null, "Band").rectTransform;
            band.anchorMin = new Vector2(0, 0.5f);
            band.anchorMax = new Vector2(1, 0.5f);
            band.pivot = new Vector2(0.5f, 0.5f);
            band.anchoredPosition = new Vector2(0, 30);
            band.sizeDelta = new Vector2(0, 250);
            foreach (float y in new[] { 125f, -125f })
                UIFactory.Image(band, new Color(1f, 1f, 1f, 0.18f), null, "Edge").rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(1100, 1));
            UIFactory.Text(band, "이대로 설치하여도 좋습니까?", 24, UIFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .At(new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(700, 40), new Vector2(0.5f, 1));
            TrapIconAt(band, new Vector2(-190, 10));
            var keys = UIFactory.Text(band, "설치 장소 :\n수거비용 :\n소요 시간 :\n어획량 :", 16, UIFactory.TextDim, TextAnchor.UpperLeft);
            keys.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-120, 10), new Vector2(120, 110));
            keys.lineSpacing = 6f;
            _confirmValues = UIFactory.Text(band, "", 16, UIFactory.TextMain, TextAnchor.UpperRight);
            _confirmValues.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(100, 10), new Vector2(240, 110));
            _confirmValues.lineSpacing = 6f;
            var ok = UIFactory.Button(band, "확인", ConfirmInstall, UIFactory.BtnPrimary, 19);
            ((RectTransform)ok.transform).At(new Vector2(0.5f, 0), new Vector2(-90, 18), new Vector2(170, 44), new Vector2(0.5f, 0));
            var cancel = UIFactory.Button(band, "취 소", () => _confirm.SetActive(false), UIFactory.BtnNeutral, 19);
            ((RectTransform)cancel.transform).At(new Vector2(0.5f, 0), new Vector2(90, 18), new Vector2(170, 44), new Vector2(0.5f, 0));
            _confirm.SetActive(false);
        }

        static void TrapIconAt(RectTransform parent, Vector2 pos)
        {
            var holder = UIFactory.Node("IconHolder", parent).At(new Vector2(0.5f, 0.5f), pos, new Vector2(64, 64));
            TrapIcon(holder, 0f, 64f, 1f);
        }

        /// <summary>자동 스크린샷용: 고른 장소로 설치 확인 띠를 띄운다.</summary>
        public void DebugConfirm() => ShowConfirm();

        void ShowConfirm()
        {
            var spots = FishTraps.SpotsOf(FieldMap.Id);
            if (_spot < 0 || _spot >= spots.Count) return;
            var s = spots[_spot];
            _confirmValues.text = $"{s.Name}\n<color=#7fd8ff>{s.Cost:N0} GOLD</color>\n{FishTraps.DurationHours:0}시간\n{FishTraps.CatchCount} 마리";
            _confirm.SetActive(true);
            _confirm.transform.SetAsLastSibling();
        }

        void ConfirmInstall()
        {
            _confirm.SetActive(false);
            var spots = FishTraps.SpotsOf(FieldMap.Id);
            if (_spot < 0 || _spot >= spots.Count) return;
            int slot = FishTraps.InstalledCount;
            if (FishTraps.Install(spots[_spot], _npcName))
            {
                Game.Toast($"{spots[_spot].Name}에 {FishTraps.RoundTrapName}을 설치했습니다. {FishTraps.DurationHours:0}시간 뒤에 수거하세요.", ToastKind.Good);
                _slot = slot;
                _spot = -1;
                Game.Quests?.OnTrapInstalled();
                Refresh();
            }
        }

        // ---- 진행 중 ----

        void ProgressView(TrapSave t)
        {
            var spot = FishTraps.SpotOf(t);
            Head("통발 상태");
            var box = Box(70);
            var text = UIFactory.Text(box, $"<color=#7fd8ff><b>(진행 중) {FishTraps.RoundTrapName}</b></color>\n\n" +
                                           $"<color=#8fa3b5>설치장소</color>   {spot.Name}{(string.IsNullOrEmpty(t.npcName) ? "" : $" ({t.npcName})")}\n" +
                                           $"<color=#8fa3b5>어획량</color>   {FishTraps.CatchCount}마리\n" +
                                           $"<color=#8fa3b5>수거비용</color>   {spot.Cost:N0} GOLD (수거할 때)\n",
                17, UIFactory.TextMain, TextAnchor.UpperLeft);
            text.rectTransform.Fill(14, 14, 14, 120);
            text.lineSpacing = 10f;
            var remain = UIFactory.Text(box, "", 18, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            remain.rectTransform.At(new Vector2(0, 0), new Vector2(14, 70), new Vector2(480, 30), new Vector2(0, 0));
            var bar = UIFactory.Bar(box, new Color(1f, 1f, 1f, 0.08f), UIFactory.Accent);
            bar.transform.parent.GetComponent<RectTransform>().At(new Vector2(0, 0), new Vector2(14, 52), new Vector2(480, 10), new Vector2(0, 0));
            UIFactory.Text(box, $"게임 시간 {FishTraps.DurationHours:0}시간(현실 약 {FishTraps.DurationMinutes / GameClock.GameMinutesPerSecond / 60f:0}분)이 지나면 물고기 {FishTraps.CatchCount}마리를 거둘 수 있습니다.\n게임을 꺼 둔 동안에도 시간이 흐릅니다.",
                13, UIFactory.TextDim, TextAnchor.LowerLeft).rectTransform.Fill(14, 14, 0, 6);
            _live.Add((t, remain, bar));
            UpdateLive(t, remain, bar);
            BottomButton("수거하기", null, false);
        }

        // ---- 수거 ----

        void HarvestView(TrapSave t)
        {
            FishTraps.EnsureRolled(t);
            if (_selFor != t)
            {
                // 처음 열면 살림망 빈 자리만큼 고른다
                _selFor = t;
                _sel.Clear();
                int free = Game.Profile.KeepNetSize - Game.Profile.Data.keepNet.Count;
                for (int i = 0; i < t.fish.Count && i < free; i++) _sel.Add(i);
            }
            var spot = FishTraps.SpotOf(t);
            Head($"수거 할 물고기 ( {_sel.Count}/{t.fish.Count} )");
            bool all = _sel.Count == t.fish.Count && t.fish.Count > 0;
            var toggle = UIFactory.Button(_right, (all ? "■ " : "□ ") + "전체선택", ToggleAll, all ? UIFactory.BtnBlue : UIFactory.BtnGhost, 14);
            ((RectTransform)toggle.transform).At(new Vector2(1, 1), new Vector2(0, 0), new Vector2(120, 30), new Vector2(1, 1));

            var list = Box(70);
            var rows = new RowCursor(list, 0f, "TrapFish");
            if (t.items != null && t.items.Count > 0)
            {
                var row = rows.Next(54, 8);
                UIFactory.RowBg(row, true);
                float x = 10f + ItemIcons.Place(row, t.items[0].id, 10f, 42f);
                Cell(row, "획득한 아이템이 있습니다.", Mathf.Max(x, 14f), 400, 16, UIFactory.Gold);
            }
            const int cols = 3;
            const float gap = 8f;
            for (int i = 0; i < t.fish.Count; i += cols)
            {
                var row = rows.Next(96, gap);
                for (int c = 0; c < cols && i + c < t.fish.Count; c++)
                    FishCard(row, i + c, t.fish[i + c], c, cols, gap);
            }
            int keepFree = Game.Profile.KeepNetSize - Game.Profile.Data.keepNet.Count;
            var note = UIFactory.Text(_right, $"살림망 빈 자리 {Mathf.Max(0, keepFree)}칸 · 고르지 않은 물고기는 놓아줍니다", 13,
                _sel.Count > keepFree ? UIFactory.Warn : UIFactory.TextDim, TextAnchor.MiddleCenter);
            note.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(520, 20), new Vector2(0.5f, 0));
            BottomButton($"수거하기  <size=14><color=#bfe8ff>-{spot.Cost:N0} GOLD</color></size>", () => DoHarvest(t), true);
        }

        void FishCard(RectTransform row, int index, CaughtFish f, int col, int cols, float gap)
        {
            var s = FishDatabase.Get(f.speciesId);
            bool on = _sel.Contains(index);
            bool quest = Game.Quests != null && Game.Quests.IsQuestSpecies(f.speciesId);
            var card = UIFactory.Shape(row, on ? UIFactory.SurfaceHi : new Color(1f, 1f, 1f, 0.03f), UIFactory.RadiusCard, "FishCard", 0f);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(col / (float)cols, 0);
            rt.anchorMax = new Vector2((col + 1) / (float)cols, 1);
            rt.offsetMin = new Vector2(col == 0 ? 0 : gap * 0.5f, 0);
            rt.offsetMax = new Vector2(col == cols - 1 ? 0 : -gap * 0.5f, 0);
            if (quest) UIFactory.AddBorder(card, new Color(0.45f, 0.95f, 0.3f), 2.4f, 5f);
            else if (on) UIFactory.AddBorder(card, new Color(1f, 1f, 1f, 0.35f), 1.2f);
            card.raycastTarget = true;
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card;
            btn.onClick.AddListener(() => { Game.Sound?.UiClick(); if (!_sel.Remove(index)) _sel.Add(index); Refresh(); });

            float a = on ? 1f : 0.45f;
            var name = UIFactory.Text(card.transform, s != null ? s.Name : f.speciesId, 16, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(160, 22), new Vector2(0.5f, 1));
            var size = UIFactory.Text(card.transform, $"{f.length:0.0} cm   {f.weight / 1000f:0.00} kg", 13, new Color(0.8f, 0.86f, 0.92f, a), TextAnchor.MiddleCenter);
            size.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(160, 18), new Vector2(0.5f, 1));
            // 어종 아이콘 (납품 창·도감과 같은 그림). 없으면 물고기 모양 도형
            var icon = s != null ? Fishing.Angling.FishModels.Icon(s) : null;
            var fish = UIFactory.Image(card.transform, icon != null ? new Color(1f, 1f, 1f, a) : new Color(1f, 1f, 1f, 0.75f * a), icon ?? UIFactory.Fish, "Fish");
            fish.preserveAspect = true;
            fish.raycastTarget = false;
            fish.rectTransform.At(new Vector2(0.5f, 0), new Vector2(-6, 4), new Vector2(110, 46), new Vector2(0.5f, 0));
            // 오른쪽 아래 선택 칸
            var box = UIFactory.Shape(card.transform, on ? UIFactory.BtnBlue : new Color(1f, 1f, 1f, 0.12f), 4f, "Check", 0f).rectTransform;
            box.At(new Vector2(1, 0), new Vector2(-8, 8), new Vector2(18, 18), new Vector2(1, 0));
        }

        void ToggleAll()
        {
            var traps = FishTraps.All;
            if (_slot >= traps.Count) return;
            var t = traps[_slot];
            if (_sel.Count == t.fish.Count) _sel.Clear();
            else for (int i = 0; i < t.fish.Count; i++) _sel.Add(i);
            Refresh();
        }

        void DoHarvest(TrapSave t)
        {
            var items = FishTraps.Harvest(t, _sel);
            if (items == null) return;
            Game.Sound?.UiClick();
            _selFor = null;
            _sel.Clear();
            Refresh();
            if (items.Count > 0) Game.UI.ItemBag.Show(items);
        }

        // ---------------- 시간 ----------------

        void Tick()
        {
            if (!IsOpen || Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 0.5f;
            bool becameReady = false;
            foreach (var (t, text, bar) in _live)
            {
                if (FishTraps.Ready(t)) { becameReady = true; break; }
                UpdateLive(t, text, bar);
            }
            if (becameReady) Refresh();
        }

        static void UpdateLive(TrapSave t, TMP_Text text, Image bar)
        {
            if (text != null) text.text = $"남은 시간 : {FishTraps.HoursText(FishTraps.Remaining(t))}";
            if (bar != null) bar.fillAmount = FishTraps.Progress01(t);
        }
    }

    /// <summary>창이 열려 있는 동안 매 프레임 부를 일 (GameUI를 고치지 않고 창 안에서 시간 표시를 갱신).</summary>
    public class PanelTicker : MonoBehaviour
    {
        public Action Tick;
        void Update() => Tick?.Invoke();
    }
}
