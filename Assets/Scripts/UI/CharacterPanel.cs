using System.Linq;
using Fishing.Core;
using Fishing.Data;
using LeTai.Asset.TranslucentImage;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 내 캐릭터(C). 원작 "내 캐릭터" 창처럼 왼쪽은 전신 미리보기, 탭은 [일반 슬롯]·[아바타 슬롯].
    ///  - 일반 슬롯: 미리보기 양옆에 장착한 낚싯대·릴·원줄 / 미끼·장신구·배 칸 (마우스를 올리면 아래 카드에 자세히),
    ///    오른쪽은 장비·강화·의상·목장갑·음식·체력 상태를 모두 합친 최종 능력치 표와 경험치
    ///  - 아바타 슬롯: 부위별 의상 목록 (사서 입으면 바로 캐릭터·초상화·대기실에 반영) + 입은 의상 능력치 레이더
    /// </summary>
    public class CharacterPanel : UIPanel
    {
        bool _avatar;
        CostumeSlot _slot = CostumeSlot.Hat;
        int _selected, _hover = -1;

        readonly RectTransform _tabs, _list;
        readonly GameObject _slotsRoot, _detailCard, _statCard;
        readonly TMP_Text _stats, _detailText;
        readonly Image _detailIcon;
        readonly ChartHost _radar;
        readonly GearSlotView[] _slots = new GearSlotView[6];
        static readonly string[] StatNames = { "체력", "대물", "데미지", "카운터", "비거리", "절약" };
        static readonly string[] SlotNames = { "낚싯대", "릴", "원줄", "미끼", "장신구", "배" };

        const float PreviewH = 380f;
        const float CardY = -50f - PreviewH - 12f;

        sealed class GearSlotView
        {
            public TranslucentImage Bg;
            public Image Icon;
            public TMP_Text Fallback, Badge;
        }

        public CharacterPanel(Transform canvas)
        {
            BuildFrame(canvas, "CharacterPanel", "내 캐릭터", new Vector2(1320, 800));
            var cam = PortraitCamera.FullBody ?? PortraitCamera.CreateFullBody();

            _tabs = UIFactory.Node("Tabs", Body).At(new Vector2(0, 1), Vector2.zero, new Vector2(420, 42), new Vector2(0, 1));

            // 전신 미리보기 (렌더 텍스처, 둥근 모서리). 3:4 비율
            var previewFrame = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard + 4f, "PreviewFrame", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(0, -50), new Vector2(420, PreviewH), new Vector2(0, 1));
            var preview = UIFactory.RoundedTexture(previewFrame, cam.Target, UIFactory.RadiusCard + 2f, "Preview");
            preview.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PreviewH * 0.75f, PreviewH - 8f));
            var spinL = UIFactory.IconButton(previewFrame, "◀", () => cam.Spin -= 45f, new Color(0, 0, 0, 0.45f), 18);
            ((RectTransform)spinL.transform).At(new Vector2(0.5f, 0), new Vector2(-28, 12), new Vector2(44, 44), new Vector2(0.5f, 0));
            var spinR = UIFactory.IconButton(previewFrame, "▶", () => cam.Spin += 45f, new Color(0, 0, 0, 0.45f), 18);
            ((RectTransform)spinR.transform).At(new Vector2(0.5f, 0), new Vector2(28, 12), new Vector2(44, 44), new Vector2(0.5f, 0));

            // 장비 칸: 왼쪽 낚싯대·릴·원줄, 오른쪽 미끼·장신구·배 (원작처럼 캐릭터 양옆)
            _slotsRoot = UIFactory.Node("GearSlots", previewFrame).Fill().gameObject;
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = BuildSlot((RectTransform)_slotsRoot.transform, i);

            // 일반 슬롯: 고른 칸 자세히
            var detail = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "DetailCard", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(0, CardY), new Vector2(420, 694 + CardY), new Vector2(0, 1));
            _detailCard = detail.gameObject;
            _detailIcon = UIFactory.Image(detail, Color.white, null, "Icon");
            _detailIcon.preserveAspect = true;
            _detailIcon.rectTransform.At(new Vector2(0, 1), new Vector2(14, -14), new Vector2(72, 72), new Vector2(0, 1));
            _detailText = UIFactory.Text(detail, "", 15, UIFactory.TextMain, TextAnchor.UpperLeft);
            _detailText.rectTransform.Fill(100, 14, 14, 10);
            _detailText.lineSpacing = 6f;

            // 아바타 슬롯: 입은 의상 능력치 레이더 (축마다 "부위별 최고 의상을 모두 입었을 때"를 100으로)
            var statCard = UIFactory.Shape(Body, UIFactory.Surface, UIFactory.RadiusCard, "StatCard", 0f).rectTransform
                .At(new Vector2(0, 1), new Vector2(0, CardY), new Vector2(420, 694 + CardY), new Vector2(0, 1));
            _statCard = statCard.gameObject;
            UIFactory.Text(statCard, "입은 의상 능력치", 15, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform
                .At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(200, 24));
            _radar = UIChart.Radar(statCard, new Vector2(210, 210), StatNames,
                new[] { new UIChart.Series("입은 의상", new float[StatNames.Length], UIFactory.Accent) }, 12);
            ((RectTransform)_radar.transform).At(new Vector2(0, 0), new Vector2(10, 6), new Vector2(210, 210), Vector2.zero);
            _stats = UIFactory.Text(statCard, "", 13, UIFactory.TextMain, TextAnchor.UpperLeft);
            _stats.rectTransform.At(new Vector2(0, 1), new Vector2(252, -40), new Vector2(160, 210), new Vector2(0, 1));
            _stats.lineSpacing = 8f;

            _list = UIFactory.Node("List", Body).At(new Vector2(1, 1), Vector2.zero, new Vector2(820, 694), new Vector2(1, 1));
        }

        GearSlotView BuildSlot(RectTransform parent, int i)
        {
            bool right = i >= 3;
            float y = -18f - (i % 3) * 112f;
            var v = new GearSlotView();
            var btn = UIFactory.Button(parent, "", () => { _selected = i; Refresh(); }, UIFactory.TabOff, 12);
            var rt = (RectTransform)btn.transform;
            rt.At(new Vector2(right ? 1 : 0, 1), new Vector2(right ? -10 : 10, y), new Vector2(62, 62), new Vector2(right ? 1 : 0, 1));
            v.Bg = (TranslucentImage)btn.targetGraphic;
            UIFactory.AddBorder(v.Bg, new Color(1f, 1f, 1f, 0.18f), 1.5f);
            v.Icon = UIFactory.Image(rt, Color.white, null, "Icon");
            v.Icon.preserveAspect = true;
            v.Icon.rectTransform.Fill(5, 5, 5, 5);
            v.Fallback = UIFactory.Text(rt, "", 12, UIFactory.TextDim, TextAnchor.MiddleCenter);
            v.Fallback.rectTransform.Fill(3, 3, 3, 3);
            v.Badge = UIFactory.Text(rt, "", 13, UIFactory.Gold, TextAnchor.LowerRight, FontStyle.Bold);
            v.Badge.rectTransform.Fill(2, 4, 2, 1);
            UIFactory.Outline(v.Badge, new Color(0, 0, 0, 0.85f));
            var cap = UIFactory.Text(parent, SlotNames[i], 12, UIFactory.TextDim, TextAnchor.MiddleCenter);
            cap.rectTransform.At(new Vector2(right ? 1 : 0, 1), new Vector2(right ? -10 : 10, y - 64), new Vector2(62, 18), new Vector2(right ? 1 : 0, 1));
            UIFactory.Outline(cap, new Color(0, 0, 0, 0.7f));

            // 마우스를 올리면 아래 카드에 그 칸을 보여 준다 (원작 툴팁 대신)
            var et = btn.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { _hover = i; RefreshDetail(); });
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (_hover == i) _hover = -1; RefreshDetail(); });
            et.triggers.Add(enter);
            et.triggers.Add(exit);
            return v;
        }

        /// <summary>다음에 열 때 보여 줄 탭 (낚시점 "의상 보기"는 아바타 슬롯으로 연다).</summary>
        public void ShowTab(bool avatar)
        {
            _avatar = avatar;
            if (IsOpen) Refresh();
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
            Title.text = $"내 캐릭터   <size=20><color=#a0b0c0>Lv.{p.Data.level} {p.CurrentTitle}</color>   <color=#ffd060>{p.Data.gold:N0} 골드</color></size>";

            UIFactory.ClearChildren(_tabs);
            RowButton(_tabs, "일반 슬롯", () => { _avatar = false; Refresh(); }, 0, 206, _avatar ? UIFactory.TabOff : UIFactory.TabOn, true, 18);
            RowButton(_tabs, "아바타 슬롯", () => { _avatar = true; Refresh(); }, 214, 206, _avatar ? UIFactory.TabOn : UIFactory.TabOff, true, 18);
            _slotsRoot.SetActive(!_avatar);
            _detailCard.SetActive(!_avatar);
            _statCard.SetActive(_avatar);

            UIFactory.ClearChildren(_list);
            if (_avatar) RefreshAvatar();
            else RefreshGear();
        }

        // ---- 일반 슬롯 ----

        void RefreshGear()
        {
            var p = Game.Profile;
            for (int i = 0; i < _slots.Length; i++)
            {
                var (id, badge, _) = SlotInfo(i);
                var v = _slots[i];
                var sprite = ItemIcons.Get(id);
                v.Icon.sprite = sprite;
                v.Icon.enabled = sprite != null;
                v.Fallback.text = id == null ? "비어 있음" : sprite == null ? ShortName(i) : "";
                v.Badge.text = badge;
                v.Bg.color = i == _selected ? UIFactory.SurfaceHi : new Color(0f, 0f, 0f, 0.35f);
            }
            RefreshDetail();

            var st = p.Costume;
            var rod = p.Rod;
            var reel = p.Reel;
            var line = p.Line;
            var rodBase = GearDatabase.GetGear(rod.Id);
            var reelBase = GearDatabase.GetGear(reel.Id);
            int rodLv = p.GearLevel(rod.Id), reelLv = p.GearLevel(reel.Id);
            string gloves = p.HasCottonGloves ? " · 목장갑 +5%" : "";
            float now = Game.Clock.TotalMinutes;

            var rows = new RowCursor(_list);

            UIFactory.SectionTitle(rows.Next(30), "체력 정보", 4, 19);
            Stat(rows, "체력", $"{p.Data.stamina:0} / {p.StaminaCap:0}", $"기본 {PlayerProfile.MaxStamina:0}" + (st.Stamina > 0f ? $" + 의상 {st.Stamina:0}" : ""));
            Stat(rows, "분당 체력 회복", p.Regenerating(now) ? $"+{p.Data.regenPerMin:0.#}" : "0",
                p.Regenerating(now) ? $"음식 효과 · 게임 {p.Data.regenUntil - now:0}분 남음" : "김밥 같은 음식을 먹으면 회복");
            Stat(rows, "체력 소모 감소", $"-{st.Drain * 100f:0}%", "의상");
            var (state, stateColor, stateNote) = p.StaminaTier switch
            {
                2 => ("탈진", UIFactory.Danger, "능력치 ×0.15 · 릴 속도 ×0.35"),
                1 => ("지침", UIFactory.Warn, "체력 20% 이하: 능력치 ×0.45 · 릴 속도 ×0.8"),
                _ => ("좋음", UIFactory.Good, "체력 20% 이하로 떨어지면 지친다"),
            };
            Stat(rows, "컨디션", state, stateNote, stateColor);
            rows.Next(6);

            UIFactory.SectionTitle(rows.Next(30), "데미지 정보", 4, 19);
            float dmgMul = p.FightPower * p.StaminaFactor;
            Stat(rows, "기본 데미지", $"{rod.Power * dmgMul * 100f:0.0}", "낚싯대 힘 × 데미지 배율 × 컨디션 (기본 낚싯대 = 100)");
            Stat(rows, "낚싯대 힘", $"{rod.Power:0.00}", $"{rodBase.Name} {rodBase.Power:0.00}" + (rodLv > 0 ? $" · 강화 +{rodLv} (+{rodLv * 3}%)" : ""));
            Stat(rows, "데미지 배율", $"×{p.FightPower:0.00}",
                (st.Damage > 0f ? $"의상 +{st.Damage * 100f:0}%" : "의상 0%") + gloves + (p.DurabilityFactor < 1f ? " · <color=#f05048>내구도 0: 절반</color>" : ""),
                p.DurabilityFactor < 1f ? UIFactory.Danger : (Color?)null);
            Stat(rows, "카운터", $"+{st.Counter * 100f:0}%", "의상 · 고기가 힘을 뺄 때 반격 데미지");
            Stat(rows, "대물 확률", $"+{st.BigFish * 100f:0}%", "의상 · 먼바다에선 +8% 더");
            rows.Next(6);

            UIFactory.SectionTitle(rows.Next(30), "컨트롤 정보", 4, 19);
            Stat(rows, "캐스팅 비거리", $"{rod.CastRange + st.Cast:0.0}m",
                $"낚싯대 {rodBase.CastRange:0}m" + (rodLv > 0 ? $" + 강화 {rodLv * 0.4f:0.0}m" : "") + (st.Cast > 0f ? $" + 의상 {st.Cast:0.0}m" : ""));
            Stat(rows, "릴 감는 속도", $"{reel.ReelSpeed * p.ReelFactor:0.00}m/s",
                $"{reelBase.Name} {reelBase.ReelSpeed:0.0}m/s" + (reelLv > 0 ? $" · 강화 +{reelLv}" : "") + gloves + (p.StaminaTier > 0 ? " · 지쳐서 느려짐" : ""));
            Stat(rows, "감을 수 있는 줄", $"{reel.Capacity:0}m", "릴 권사량 · 고기가 이보다 멀리 가면 끊긴다");
            Stat(rows, "낚싯대 탄성", FlexName(rod.Flex), "연조일수록 충격을 흡수하고, 경조일수록 장력이 빨리 오른다");
            rows.Next(6);

            UIFactory.SectionTitle(rows.Next(30), "안전성 정보", 4, 19);
            Stat(rows, "줄 강도", $"{line.Strength:0.0}", $"{line.Name} · 높을수록 장력을 버틴다");
            bool broken = p.Data.rodDurability <= 0f;
            Stat(rows, "낚싯대 내구도", $"{p.Data.rodDurability:0}%", broken ? "낚시점에서 수리하세요 (데미지 절반)" : "파이팅할수록 닳는다 · 낚시점에서 수리",
                broken ? UIFactory.Danger : p.Data.rodDurability < 30f ? UIFactory.Warn : (Color?)null);
            rows.Next(10);

            // 경험치 (원작 창 맨 아래 막대)
            var expRow = rows.Next(40);
            UIFactory.RowBg(expRow);
            Cell(expRow, "<b>경험치</b>", 16, 120, 16);
            bool max = p.Data.level >= PlayerProfile.MaxLevel;
            float ratio = max ? 1f : Mathf.Clamp01(p.Data.exp / (float)PlayerProfile.ExpToNext(p.Data.level));
            var bar = UIFactory.Bar(expRow, new Color(1f, 1f, 1f, 0.1f), UIFactory.Accent);
            ((RectTransform)bar.transform.parent).At(new Vector2(0, 0.5f), new Vector2(130, 0), new Vector2(480, 12), new Vector2(0, 0.5f));
            bar.fillAmount = ratio;
            Cell(expRow, max ? "최고 레벨" : $"{ratio * 100f:0}%  <size=13><color=#8fa3b5>{p.Data.exp:N0} / {PlayerProfile.ExpToNext(p.Data.level):N0}</color></size>",
                624, 180, 16);
        }

        void Stat(RowCursor rows, string label, string value, string note, Color? valueColor = null)
        {
            var row = rows.Next(32, 2);
            UIFactory.RowBg(row);
            Cell(row, label, 16, 170, 16, UIFactory.TextDim);
            Cell(row, $"<b>{value}</b>", 186, 130, 17, valueColor ?? UIFactory.TextMain, TextAnchor.MiddleRight);
            Cell(row, note, 336, 470, 13, UIFactory.TextFaint);
        }

        static string FlexName(float flex) => flex >= 0.6f ? "연조" : flex <= 0.35f ? "경조" : "중경조";

        /// <summary>칸 i: 아이콘 id (없으면 null), 칸 오른쪽 아래 글씨, 자세히 카드 글.</summary>
        (string id, string badge, string detail) SlotInfo(int i)
        {
            var p = Game.Profile;
            switch (i)
            {
                case 0:
                {
                    var rod = p.Rod;
                    int lv = p.GearLevel(rod.Id);
                    return (rod.Id, lv > 0 ? $"+{lv}" : "",
                        $"<b><size=19>{rod.Name}</size></b>\n<color=#a0d0a0>{rod.StatLine}</color>\n" +
                        $"강화 +{lv} / {RodUpgrade.MaxLevel} · 내구도 {p.Data.rodDurability:0}%\n<color=#8090a0>{GearDatabase.GetGear(rod.Id).Desc}</color>");
                }
                case 1:
                {
                    var reel = p.Reel;
                    int lv = p.GearLevel(reel.Id);
                    return (reel.Id, lv > 0 ? $"+{lv}" : "",
                        $"<b><size=19>{reel.Name}</size></b>\n<color=#a0d0a0>{reel.StatLine}</color>\n" +
                        $"강화 +{lv} / {RodUpgrade.MaxLevel}\n<color=#8090a0>{GearDatabase.GetGear(reel.Id).Desc}</color>");
                }
                case 2:
                {
                    var line = p.Line;
                    return (line.Id, "", $"<b><size=19>{line.Name}</size></b>\n<color=#a0d0a0>{line.StatLine}</color>\n<color=#8090a0>{line.Desc}</color>");
                }
                case 3:
                {
                    var bait = p.Bait;
                    int n = p.Count(bait.Id);
                    return (bait.Id, $"x{n}",
                        $"<b><size=19>{bait.Name}</size></b>  <color=#ffd060>x{n}</color>\n<color=#a0d0a0>입질 ×{bait.Potency:0.0#} · Lv.{bait.Level}부터</color>\n" +
                        $"<color=#8090a0>{ItemDatabase.Desc(bait.Id)}</color>\n가방(I)이나 숫자 키로 바꿔 끼운다");
                }
                case 4:
                    return p.HasCottonGloves
                        ? (PlayerProfile.CottonGlovesId, "", "<b><size=19>미끄러지지 않는 목장갑</size></b>\n<color=#a0d0a0>파이팅 힘 +5% · 릴 속도 +5%</color>\n<color=#8090a0>Lv.6 보상. 늘 끼고 있다.</color>")
                        : (null, "", "<b><size=19>장신구 없음</size></b>\n<color=#8090a0>Lv.6 보상: 미끄러지지 않는 목장갑</color>");
                default:
                {
                    var boat = p.Boat;
                    if (boat == null) return (null, "", "<b><size=19>배 없음</size></b>\n<color=#8090a0>Lv.5에 조합이 고무보트를 지급합니다.</color>");
                    return (boat.Id, boat.Sail ? "" : $"{p.Data.boatFuel:0}L",
                        $"<b><size=19>{boat.Name}</size></b>\n<color=#a0d0a0>{boat.StatLine}</color>\n" +
                        (boat.Sail ? "" : $"연료 {p.Data.boatFuel:0.0} / {boat.FuelCapacity:0}L\n") + "<color=#8090a0>물가에서 B로 꺼내 타기</color>");
                }
            }
        }

        string ShortName(int i) => i switch
        {
            0 => Game.Profile.Rod.Name,
            1 => Game.Profile.Reel.Name,
            2 => Game.Profile.Line.Name,
            3 => Game.Profile.Bait.Name,
            _ => SlotNames[i],
        };

        void RefreshDetail()
        {
            if (_avatar || Game.Profile == null) return;
            var (id, _, detail) = SlotInfo(_hover >= 0 ? _hover : _selected);
            var sprite = ItemIcons.Get(id);
            _detailIcon.sprite = sprite;
            _detailIcon.enabled = sprite != null;
            _detailText.rectTransform.offsetMin = new Vector2(sprite != null ? 100 : 16, _detailText.rectTransform.offsetMin.y);
            _detailText.text = detail;
        }

        // ---- 아바타 슬롯 (의상) ----

        void RefreshAvatar()
        {
            var p = Game.Profile;
            var st = p.Costume;
            _stats.text = $"<color=#8fa3b5>최대 체력</color> {p.StaminaCap:0} <size=12>(+{st.Stamina:0})</size>\n" +
                          $"<color=#8fa3b5>대물</color> +{st.BigFish * 100f:0}%\n" +
                          $"<color=#8fa3b5>데미지</color> +{st.Damage * 100f:0}%\n" +
                          $"<color=#8fa3b5>카운터</color> +{st.Counter * 100f:0}%\n" +
                          $"<color=#8fa3b5>비거리</color> +{st.Cast:0.0}m\n" +
                          $"<color=#8fa3b5>체력 소모</color> -{st.Drain * 100f:0}%";
            UIChart.SetValues(_radar.Serie(), Normalized(st));
            _radar.Refresh();

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
