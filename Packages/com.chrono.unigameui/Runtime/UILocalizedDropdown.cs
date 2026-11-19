using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UniGameUI
{
    /// <summary>
    /// 配合 TMP 下拉選單使用的多語系鍵值，每個選項一個。空字串的位置維持選項原本的文字。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI Localized Dropdown")]
    public sealed class UILocalizedDropdown : UILocalizedElement
    {
        [SerializeField] internal string[] keys = Array.Empty<string>();

        private TMP_Dropdown dropdown;
        private string[] fallback;

        public IReadOnlyList<string> Keys => keys;

        internal override void Refresh(UIRoot root, UIView view)
        {
            if (keys == null || keys.Length == 0 || (!dropdown && !TryGetComponent(out dropdown)))
                return;

            var options = dropdown.options;
            if (fallback == null)
            {
                fallback = new string[options.Count];
                for (var index = 0; index < options.Count; index++)
                    fallback[index] = options[index].text;
            }

            var count = Mathf.Min(keys.Length, options.Count);
            for (var index = 0; index < count; index++)
            {
                if (string.IsNullOrEmpty(keys[index]))
                    continue;
                var original = index < fallback.Length ? fallback[index] : options[index].text;
                options[index].text = root.TryLocalizeText(keys[index], view, this, out var text) ? text : original;
            }
            dropdown.RefreshShownValue();
        }
    }
}
