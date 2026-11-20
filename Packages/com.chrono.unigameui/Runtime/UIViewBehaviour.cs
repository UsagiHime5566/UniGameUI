using System.Threading;
using UnityEngine;

namespace UniGameUI
{
    public interface IUIViewHandler
    {
        void OnShowing(UIView view, object data);
        void OnShown(UIView view);
        void OnHiding(UIView view);
        void OnHidden(UIView view);
        void OnAction(UIView view, string action);
    }

    [RequireComponent(typeof(UIView))]
    public abstract class UIViewBehaviour : MonoBehaviour, IUIViewHandler, IUILocalizationHandler, IUIBackHandler
    {
        private UIView view;

        public UIView View => view ? view : view = GetComponent<UIView>();
        public UIRoot Root => View.Root;

        protected virtual void OnShowing(object data) { }
        protected virtual void OnShown() { }
        protected virtual void OnHiding() { }
        protected virtual void OnHidden() { }
        protected virtual void OnAction(string action) { }

        /// <summary>
        /// 語言切換、框架重新取值之後呼叫。頁面腳本自己組出來的文字在這裡更新。
        /// </summary>
        protected virtual void OnLocalizationChanged() { }

        /// <summary>
        /// 這個畫面在最上層時按下返回鍵會先呼叫這裡。回傳 true 表示已經處理，框架不再返回。
        /// 呼叫時沒有導航在進行，裡面可以直接導航。
        /// </summary>
        protected virtual bool OnBackRequested() => false;

        /// <summary>
        /// 走這個畫面自己的路由，等於 <c>Root.TriggerWhenIdleAsync(View.Id, action, cancellation)</c>。
        /// 「在這一頁發生、依遊戲邏輯的結果去不同地方」的流程用它：每個可能的去處在藍圖上各畫一條這一頁的路由，
        /// 程式只負責挑哪一條。有導航正在進行時等它結束再執行，不會回傳 Busy；
        /// 上面蓋著 Popup 時回傳 Unavailable 並記錄警告；等待期間已經切到別的畫面時安靜地回傳 Canceled。
        /// </summary>
        protected async Awaitable<UINavResult> NavigateAsync(string action, CancellationToken cancellation = default)
        {
            var root = Root;
            if (!root)
            {
                Debug.LogWarning($"UniGameUI: NavigateAsync '{action}' 沒有執行：這個畫面還沒有交給工作區管理。", this);
                return UINavResult.Unavailable;
            }
            return await root.TriggerWhenIdleAsync(View.Id, action, cancellation);
        }

        void IUIViewHandler.OnShowing(UIView view, object data) => OnShowing(data);
        void IUIViewHandler.OnShown(UIView view) => OnShown();
        void IUIViewHandler.OnHiding(UIView view) => OnHiding();
        void IUIViewHandler.OnHidden(UIView view) => OnHidden();
        void IUIViewHandler.OnAction(UIView view, string action) => OnAction(action);
        void IUILocalizationHandler.OnLocalizationChanged(UIView view) => OnLocalizationChanged();
        bool IUIBackHandler.OnBackRequested(UIView view) => OnBackRequested();
    }
}
