using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fishing.UI
{
    /// <summary>
    /// 아이템 아이콘 (tools/blender/build_icons.py가 렌더한 256×256 투명 PNG).
    ///  - Icons/Items/&lt;아이템 Id&gt;: 장비(낚싯대·릴·줄·배), 미끼, 김밥, 재료
    ///  - Icons/Junk/&lt;잡동사니 Id&gt;: 잡동사니 (결과 화면)
    /// 그림이 없으면 null을 돌려주고, 부르는 쪽은 글자만 보여 준다 (하나씩 채워 나갈 수 있게).
    /// </summary>
    public static class ItemIcons
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Cache.TryGetValue(id, out var s)) return s;
            s = Resources.Load<Sprite>("Icons/Items/" + id) ?? Resources.Load<Sprite>("Icons/Junk/" + id);
            Cache[id] = s;
            return s;
        }

        /// <summary>
        /// 행 왼쪽(x)에 아이콘을 놓는다. 그림이 있으면 차지한 너비(size + 여백)를, 없으면 0을 돌려준다.
        /// 같은 행의 글자는 그만큼 오른쪽으로 민다.
        /// </summary>
        public static float Place(RectTransform row, string id, float x, float size, bool dim = false)
        {
            var sprite = Get(id);
            if (sprite == null) return 0f;
            var img = UIFactory.Image(row, dim ? new Color(1f, 1f, 1f, 0.4f) : Color.white, sprite, "Icon");
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);
            rt.sizeDelta = new Vector2(size, size);
            return size + 8f;
        }
    }
}
