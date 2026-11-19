using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI
{
    /// <summary>
    /// 確認框要顯示的內容。也可以只傳一個字串當成訊息。
    /// </summary>
    public sealed class UIConfirmRequest
    {
        public string Message;

        /// <summary>null 時隱藏標題。</summary>
        public string Title;

        /// <summary>null 時使用 Prefab 上的文字。</summary>
        public string ConfirmText;

        /// <summary>null 時使用 Prefab 上的文字。</summary>
        public string CancelText;
    }

    /// <summary>
    /// 確認框的頁面腳本，掛在確認框 Popup 的根節點。兩個按鈕不走路由：
    /// 確定以 true 關閉，取消以 false 關閉。標準呼叫方式是 <see cref="UIRoot.ConfirmAsync"/>。
    /// </summary>
    [AddComponentMenu("UniGameUI/UI Confirm Popup")]
    public sealed class UIConfirmPopup : UIViewBehaviour
    {
        public const string TitleId = "title";
        public const string MessageId = "message";
        public const string ConfirmId = "confirm";
        public const string CancelId = "cancel";

        private UIConfirmRequest request;
        private string confirmFallback;
        private string cancelFallback;
        private bool listening;

        protected override void OnShowing(object data)
        {
            request = data switch
            {
                UIConfirmRequest typed => typed,
                string message => new UIConfirmRequest { Message = message },
                _ => throw new InvalidOperationException($"確認框「{View.Id}」的資料必須是 UIConfirmRequest 或字串，" +
                    $"收到的是「{data?.GetType().ToString() ?? "null"}」。")
            };

            if (!listening)
            {
                listening = true;
                View.Get<Button>(ConfirmId).onClick.AddListener(() => Finish(true));
                View.Get<Button>(CancelId).onClick.AddListener(() => Finish(false));
            }
            Apply();
        }

        protected override void OnLocalizationChanged()
        {
            // 重新取值會把按鈕改回多語系的預設文字，這次請求指定的文字要再寫回去。
            if (request != null && View.gameObject.activeInHierarchy)
                Apply();
        }

        private void Apply()
        {
            View.Get<TMP_Text>(MessageId).text = request.Message ?? "";
            var title = View.GetElement(TitleId);
            title.SetActive(request.Title != null);
            if (request.Title != null)
                View.Get<TMP_Text>(TitleId).text = request.Title;
            SetButtonText(View.Get<TMP_Text>(ConfirmId), request.ConfirmText, ref confirmFallback);
            SetButtonText(View.Get<TMP_Text>(CancelId), request.CancelText, ref cancelFallback);
        }

        // 確認框會重用，上一次指定的按鈕文字不可以留到下一次。
        // 有多語系鍵值的文字在每次顯示前都會由框架重設；沒有鍵值的由這裡改回原本的文字。
        private static void SetButtonText(TMP_Text label, string text, ref string fallback)
        {
            fallback ??= label.text;
            if (text != null)
                label.text = text;
            else if (!label.TryGetComponent<UILocalizedText>(out var localized) || string.IsNullOrEmpty(localized.Key))
                label.text = fallback;
        }

        private async void Finish(bool confirmed)
        {
            try
            {
                await View.Close(confirmed);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
