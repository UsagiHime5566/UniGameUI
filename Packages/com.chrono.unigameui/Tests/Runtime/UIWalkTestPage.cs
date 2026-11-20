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
            LogWarningOnAction
        }

        public Behaviour behaviour;
        public string target;

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
            if (behaviour != Behaviour.NavigateOnShown)
                return;

            // 和操作指南的 LaunchPage 相同：等一個影格再接著導航。
            try
            {
                var root = Root;
                await Awaitable.NextFrameAsync();
                if (!this || !root || root.CurrentPage != View)
                    return;
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
