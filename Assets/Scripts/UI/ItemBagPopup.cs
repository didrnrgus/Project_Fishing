using System;
using System.Collections.Generic;
using Fishing.Core;
using Fishing.Data;
using TMPro;
using UnityEngine;

namespace Fishing.UI
{
    /// <summary>
    /// 아이템 획득 ITEM BAG (원작 0:48): 받은 아이템 목록(아이콘 · "이름(N 개)" · 줄마다 [획득]) + 아래 [모두획득] [모두버리기].
    /// 통발 수거 덤에 쓰고, 퀘스트 보상 등 다른 곳에서도 <see cref="Show"/>로 띄울 수 있다.
    /// 남은 아이템이 있는 채로 닫히면(ESC, 다른 창) 버리지 않고 모두 받는다 — 실수로 잃지 않게.
    /// </summary>
    public class ItemBagPopup : UIPanel
    {
        readonly List<ItemStack> _items = new List<ItemStack>();
        Action _done;
        RectTransform _list;
        TMP_Text _empty;

        public ItemBagPopup(Transform canvas)
        {
            BuildFrame(canvas, "ItemBagPopup", UIFactory.Caption("아이템 획득", "ITEM BAG"), new Vector2(520, 470), closable: false);
            _list = UIFactory.Node("List", Body).Fill(0, 0, 0, 64);
            _empty = UIFactory.Text(Body, "받을 아이템이 없습니다.", 17, UIFactory.TextDim, TextAnchor.MiddleCenter);
            _empty.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(400, 40));
            var take = UIFactory.Button(Body, "모두획득", TakeAll, UIFactory.BtnPrimary, 19);
            ((RectTransform)take.transform).At(new Vector2(0.5f, 0), new Vector2(-104, 0), new Vector2(200, 48), new Vector2(0.5f, 0));
            var drop = UIFactory.Button(Body, "모두버리기", DropAll, UIFactory.BtnNeutral, 19);
            ((RectTransform)drop.transform).At(new Vector2(0.5f, 0), new Vector2(104, 0), new Vector2(200, 48), new Vector2(0.5f, 0));
        }

        /// <summary>아이템 목록을 띄운다. 다 받거나 버리면 done을 부른다. 빈 목록이면 띄우지 않고 바로 done.</summary>
        public void Show(IEnumerable<ItemStack> items, Action done = null)
        {
            _items.Clear();
            if (items != null)
                foreach (var it in items)
                    if (it != null && it.count > 0) _items.Add(new ItemStack { id = it.id, count = it.count });
            _done = done;
            if (_items.Count == 0) { Finish(); return; }
            Open();
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(_list);
            _empty.gameObject.SetActive(_items.Count == 0);
            var rows = new RowCursor(_list, 0f, "ItemBag");
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                var row = rows.Next(64, 6);
                UIFactory.RowBg(row, i == 0);
                float x = 10f + ItemIcons.Place(row, it.id, 10f, 50f);
                if (x <= 10f)
                {
                    // 그림이 없으면 글자 칸
                    var box = UIFactory.Shape(row, UIFactory.SurfaceHi, UIFactory.RadiusButton, "IconBox", 0f).rectTransform;
                    box.At(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(50, 50), new Vector2(0, 0.5f));
                    UIFactory.Text(box, "◆", 22, UIFactory.Accent, TextAnchor.MiddleCenter).rectTransform.Fill();
                    x = 68f;
                }
                Cell(row, $"{ItemDatabase.Name(it.id)}<color=#8fa3b5>({it.count} 개)</color>", x, 300, 18);
                int index = i;
                var b = RowButton(row, "획득", () => TakeOne(index), 0, 84, UIFactory.BtnBlue, true, 15);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-12, 0);
                rt.sizeDelta = new Vector2(84, 38);
            }
        }

        void Give(ItemStack it)
        {
            Game.Profile.AddItem(it.id, it.count);
            Game.SysLog($"{ItemDatabase.Name(it.id)} {it.count}개를 획득했습니다.");
        }

        void TakeOne(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            Give(_items[index]);
            _items.RemoveAt(index);
            if (_items.Count == 0) Finish();
            else Refresh();
        }

        void TakeAll()
        {
            foreach (var it in _items) Give(it);
            _items.Clear();
            Finish();
        }

        void DropAll()
        {
            if (_items.Count > 0) Game.SysLog("아이템을 모두 버렸습니다.", "#a0b0c0");
            _items.Clear();
            Finish();
        }

        void Finish()
        {
            base.Close();
            var done = _done;
            _done = null;
            done?.Invoke();
        }

        /// <summary>남은 아이템이 있으면 모두 받고 닫는다.</summary>
        public override void Close()
        {
            if (_items.Count > 0) { TakeAll(); return; }
            base.Close();
        }
    }
}
