using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI
{
    /// <summary>
    /// 配合 Image 使用的多語系鍵值。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI Localized Image")]
    public sealed class UILocalizedImage : UILocalizedElement
    {
        [SerializeField] internal string key;

        private Image image;
        private Sprite fallback;
        private bool captured;

        public string Key
        {
            get => key;
            set => key = value;
        }

        internal override void Refresh(UIRoot root, UIView view)
        {
            if (string.IsNullOrEmpty(key) || (!image && !TryGetComponent(out image)))
                return;
            if (!captured)
            {
                fallback = image.sprite;
                captured = true;
            }
            image.sprite = root.TryLocalizeSprite(key, view, this, out var sprite) ? sprite : fallback;
        }
    }
}
