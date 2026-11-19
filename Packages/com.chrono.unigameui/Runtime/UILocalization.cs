using UnityEngine;

namespace UniGameUI
{
    /// <summary>
    /// 專案的多語系系統實作這個介面，指定給 <see cref="UIRoot.Localizer"/>。
    /// 框架只負責什麼時候取值、取不到時顯示什麼。
    /// </summary>
    public interface IUILocalizer
    {
        bool TryGetText(string key, out string text);
        bool TryGetSprite(string key, out Sprite sprite);
    }

    /// <summary>
    /// 掛在 View 根節點上，在語言切換、框架重新取值之後收到通知。頁面腳本自己組出來的文字在這裡更新。
    /// </summary>
    public interface IUILocalizationHandler
    {
        void OnLocalizationChanged(UIView view);
    }

    /// <summary>
    /// 記錄多語系鍵值的元件。編輯時寫在目標元件上的內容就是預設內容，查不到鍵值時顯示它。
    /// </summary>
    public abstract class UILocalizedElement : MonoBehaviour
    {
        private protected UILocalizedElement()
        {
        }

        internal abstract void Refresh(UIRoot root, UIView view);
    }
}
