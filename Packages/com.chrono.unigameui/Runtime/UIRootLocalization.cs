using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniGameUI
{
    // 多語系接口：框架決定什麼時候取值、取不到時顯示什麼，翻譯內容由專案的 IUILocalizer 提供。
    public sealed partial class UIRoot
    {
        private readonly HashSet<string> missingLocalizationKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<UILocalizedElement> localizedElements = new List<UILocalizedElement>();
        private IUILocalizer localizer;

        /// <summary>
        /// 專案的多語系來源。指定新的來源時，所有現存的畫面會重新取值。
        /// </summary>
        public IUILocalizer Localizer
        {
            get => localizer;
            set
            {
                localizer = value;
                RefreshLocalization();
            }
        }

        /// <summary>
        /// <see cref="RefreshLocalization"/> 重新取值、通知過各畫面之後觸發。
        /// </summary>
        public event Action LocalizationChanged;

        /// <summary>
        /// 切換語言後呼叫。所有現存的畫面實例重新取值，包含沒有顯示的頁面與保留中的 Popup；
        /// 接著通知各畫面根節點上的 <see cref="IUILocalizationHandler"/>，最後觸發 <see cref="LocalizationChanged"/>。
        /// </summary>
        public void RefreshLocalization()
        {
            if (!ready)
                return;

            var views = new List<UIView>();
            CollectExistingViews(views);
            foreach (var view in views)
            {
                if (view)
                    ApplyLocalization(view.transform, view);
            }
            foreach (var view in views)
            {
                if (!view)
                    continue;
                foreach (var handler in view.GetComponents<IUILocalizationHandler>())
                {
                    if (handler is not UnityEngine.Object component || !component)
                        continue;
                    try
                    {
                        handler.OnLocalizationChanged(view);
                    }
                    catch (Exception exception)
                    {
                        LogReceiverException($"RefreshLocalization '{view.Id}' " +
                            nameof(IUILocalizationHandler.OnLocalizationChanged), exception);
                    }
                }
            }
            UIEventDispatch.Invoke(LocalizationChanged, listener => listener(),
                exception => LogReceiverException(nameof(LocalizationChanged), exception));
        }

        /// <summary>
        /// 給頁面腳本組合動態文字用。查不到時回傳 <paramref name="fallback"/>；
        /// <paramref name="fallback"/> 是 null 時回傳鍵值本身，方便發現漏翻。
        /// </summary>
        public string Localize(string key, string fallback = null)
        {
            if (!string.IsNullOrEmpty(key) && TryLocalizeText(key, null, null, out var text))
                return text;
            return fallback ?? key;
        }

        private void CollectExistingViews(List<UIView> views)
        {
            foreach (var page in pages.Values)
            {
                if (page)
                    views.Add(page);
            }
            foreach (var popup in popups)
            {
                if (popup)
                    views.Add(popup);
            }
            foreach (var popup in retainedPopups.Values)
            {
                if (popup)
                    views.Add(popup);
            }
        }

        /// <summary>
        /// 對 <paramref name="scope"/> 底下所有記錄了鍵值的元件取值，包含未啟用的物件。
        /// </summary>
        internal void ApplyLocalization(Transform scope, UIView view)
        {
            // 取值會呼叫專案的程式；萬一它又觸發另一次取值，改用新的清單，不共用同一個暫存。
            var buffer = localizedElements.Count == 0 ? localizedElements : new List<UILocalizedElement>();
            scope.GetComponentsInChildren(true, buffer);
            try
            {
                for (var index = 0; index < buffer.Count; index++)
                {
                    if (buffer[index])
                        buffer[index].Refresh(this, view);
                }
            }
            finally
            {
                buffer.Clear();
            }
        }

        internal bool TryLocalizeText(string key, UIView view, Component element, out string text)
        {
            text = null;
            if (!HasLocalizer())
                return false;

            var found = false;
            try
            {
                found = localizer.TryGetText(key, out text) && text != null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            if (!found)
                WarnMissingKey(key, "文字", view, element);
            return found;
        }

        internal bool TryLocalizeSprite(string key, UIView view, Component element, out Sprite sprite)
        {
            sprite = null;
            if (!HasLocalizer())
                return false;

            var found = false;
            try
            {
                found = localizer.TryGetSprite(key, out sprite) && sprite;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            if (!found)
                WarnMissingKey(key, "圖片", view, element);
            return found;
        }

        private bool HasLocalizer()
        {
            return localizer != null && (localizer is not UnityEngine.Object source || source);
        }

        // 有設定 Localizer 而鍵值查不到時，每個鍵值只警告一次。
        private void WarnMissingKey(string key, string kind, UIView view, Component element)
        {
            if (!missingLocalizationKeys.Add($"{kind}:{key}"))
                return;

            var where = "";
            if (view || element)
            {
                var elementId = element ? element.GetComponentInParent<UIElementId>(true) : null;
                var elementName = elementId ? elementId.Id : element ? element.name : "(無)";
                where = $"（畫面 '{(view ? view.Id : "(無)")}'，元件 '{elementName}'）";
            }
            Debug.LogWarning(Diagnostic("Localize", $"找不到{kind}的多語系鍵值 '{key}'{where}，顯示預設內容。"),
                element ? element : this);
        }
    }
}
