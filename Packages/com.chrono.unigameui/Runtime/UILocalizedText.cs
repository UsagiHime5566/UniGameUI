using TMPro;
using UnityEngine;

namespace UniGameUI
{
    /// <summary>
    /// 配合 TMP 文字使用的多語系鍵值。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI Localized Text")]
    public sealed class UILocalizedText : UILocalizedElement
    {
        [SerializeField] internal string key;

        private TMP_Text label;
        private string fallback;
        private bool captured;

        public string Key
        {
            get => key;
            set => key = value;
        }

        internal override void Refresh(UIRoot root, UIView view)
        {
            if (string.IsNullOrEmpty(key) || (!label && !TryGetComponent(out label)))
                return;
            if (!captured)
            {
                fallback = label.text;
                captured = true;
            }
            label.text = root.TryLocalizeText(key, view, this, out var text) ? text : fallback;
        }
    }
}
