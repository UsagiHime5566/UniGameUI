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

        void IUIViewHandler.OnShowing(UIView view, object data) => OnShowing(data);
        void IUIViewHandler.OnShown(UIView view) => OnShown();
        void IUIViewHandler.OnHiding(UIView view) => OnHiding();
        void IUIViewHandler.OnHidden(UIView view) => OnHidden();
        void IUIViewHandler.OnAction(UIView view, string action) => OnAction(action);
        void IUILocalizationHandler.OnLocalizationChanged(UIView view) => OnLocalizationChanged();
        bool IUIBackHandler.OnBackRequested(UIView view) => OnBackRequested();
    }
}
