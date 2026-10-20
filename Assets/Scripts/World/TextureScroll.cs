using System.Collections.Generic;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>재질의 텍스처를 시간에 따라 밀어 흐르게 한다 (폭포 물줄기·급류). 맵 월드와 함께 지워진다.</summary>
    public class TextureScroll : MonoBehaviour
    {
        readonly List<(Material mat, Vector2 speed)> _items = new List<(Material, Vector2)>();

        public static TextureScroll On(Transform t) => t.GetComponent<TextureScroll>() ?? t.gameObject.AddComponent<TextureScroll>();

        public TextureScroll Add(Material m, Vector2 speed)
        {
            if (m != null) _items.Add((m, speed));
            return this;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var (m, sp) in _items)
                if (m != null) m.mainTextureOffset = new Vector2(Mathf.Repeat(m.mainTextureOffset.x + sp.x * dt, 1f), Mathf.Repeat(m.mainTextureOffset.y + sp.y * dt, 1f));
        }
    }
}
