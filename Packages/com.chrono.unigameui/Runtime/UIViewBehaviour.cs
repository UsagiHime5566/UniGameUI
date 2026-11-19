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
    public abstract class UIViewBehaviour : MonoBehaviour, IUIViewHandler
    {
        private UIView view;

        public UIView View => view ? view : view = GetComponent<UIView>();
        public UIRoot Root => View.Root;

        protected virtual void OnShowing(object data) { }
        protected virtual void OnShown() { }
        protected virtual void OnHiding() { }
        protected virtual void OnHidden() { }
        protected virtual void OnAction(string action) { }

        void IUIViewHandler.OnShowing(UIView view, object data) => OnShowing(data);
        void IUIViewHandler.OnShown(UIView view) => OnShown();
        void IUIViewHandler.OnHiding(UIView view) => OnHiding();
        void IUIViewHandler.OnHidden(UIView view) => OnHidden();
        void IUIViewHandler.OnAction(UIView view, string action) => OnAction(action);
    }
}
