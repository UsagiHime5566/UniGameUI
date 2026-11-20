using System;
using UnityEngine;

namespace UniGameUI.Tests
{
    /// <summary>
    /// 自動走訪測試用的頁面腳本。行為由序列化的欄位決定，所以進入 Play Mode、Domain 重新載入之後仍然有效。
    /// </summary>
    public sealed class UIWalkTestPage : UIViewBehaviour
    {
        public enum Behaviour
        {
            None,
            ThrowOnShowing,
            LogErrorOnShown,
            LogWarningOnShown,
            NavigateOnShown,
            RequireData,
            LogWarningOnAction,
            // 開一個等結果的 Popup；它一被關掉，就馬上開始切到 target（計時器之類的腳本自己在導航）。
            NavigateWhenPopupCloses
        }

        public Behaviour behaviour;
        public string target;
        public string popup;

        protected override void OnShowing(object data)
        {
            if (behaviour == Behaviour.ThrowOnShowing)
                throw new InvalidOperationException($"walk-test: 「{View.Id}」的 OnShowing 丟出例外");
            // 沒有資料就顯示不出來的畫面：GetData 在資料是 null 時會丟出例外。
            if (behaviour == Behaviour.RequireData)
                View.GetData<string>();
        }

        protected override async void OnShown()
        {
            if (behaviour == Behaviour.LogErrorOnShown)
                Debug.LogError($"walk-test: 「{View.Id}」在 OnShown 記了一筆錯誤", this);
            if (behaviour == Behaviour.LogWarningOnShown)
                Debug.LogWarning($"walk-test: 「{View.Id}」在 OnShown 記了一筆警告", this);
            if (behaviour != Behaviour.NavigateOnShown && behaviour != Behaviour.NavigateWhenPopupCloses)
                return;

            // 和操作指南的 LaunchPage 相同：等一個影格再接著導航。
            try
            {
                var root = Root;
                await Awaitable.NextFrameAsync();
                if (!this || !root || root.CurrentPage != View)
                    return;
                if (behaviour == Behaviour.NavigateWhenPopupCloses)
                {
                    if (root.CurrentPopupId == popup)
                        return;
                    // Popup 的結果在關閉它的那次導航結束時送達，這裡接著開始的導航會讓工作區再度忙碌：
                    // 關掉 Popup 的人（走訪）下一個呼叫就會遇到 Busy。
                    var answer = await root.OpenPopupForResultAsync<string>(popup);
                    if (!this || !root || answer.Status != UINavResult.Completed)
                        return;
                    Debug.LogWarning($"walk-test: 「{View.Id}」在 Popup 關閉後馬上自己切頁", this);
                    await root.ShowPageAsync(target);
                    return;
                }
                await root.ShowPageAsync(target, null, UIHistoryMode.Clear);
            }
            catch (OperationCanceledException)
            {
                // Play Mode 結束。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        protected override void OnAction(string action)
        {
            if (behaviour == Behaviour.LogWarningOnAction)
                Debug.LogWarning($"walk-test: 「{View.Id}」收到動作 {action}", this);
        }
    }
}
