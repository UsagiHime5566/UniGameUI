using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace UniGameUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("UniGameUI/UI View")]
    public sealed class UIView : MonoBehaviour
    {
        [SerializeField, HideInInspector] internal string id;
        [SerializeField, HideInInspector] internal RectTransform motion;
        [SerializeField, HideInInspector] internal RectTransform content;
        [SerializeField, HideInInspector] internal CanvasGroup canvasGroup;
        [SerializeField] internal UITransition enter = new UITransition();
        [SerializeField] internal UITransition exit = new UITransition();
        [SerializeField] private UnityEvent showing = new UnityEvent();
        [SerializeField, FormerlySerializedAs("showed")] private UnityEvent shown = new UnityEvent();
        [SerializeField] private UnityEvent hiding = new UnityEvent();
        [SerializeField] private UnityEvent hidden = new UnityEvent();

        private Dictionary<string, UIElementId> elements;

        public string Id => id;
        public RectTransform Motion => motion;
        public RectTransform Content => content;
        public CanvasGroup Group => canvasGroup;
        public UITransition Enter => enter;
        public UITransition Exit => exit;
        public UnityEvent Showing => showing;
        public UnityEvent Shown => shown;
        public UnityEvent Hiding => hiding;
        public UnityEvent Hidden => hidden;
        public object Data { get; internal set; }
        public UIRoot Root { get; internal set; }

        public T GetData<T>()
        {
            if (TryGetData<T>(out var value))
                return value;
            throw new InvalidOperationException($"畫面「{Id}」的資料型別為「{Data?.GetType().ToString() ?? "null"}」，" +
                $"期望「{typeof(T)}」。");
        }

        public bool TryGetData<T>(out T value)
        {
            if (Data is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public T Get<T>(string elementId) where T : Component
        {
            if (TryGet<T>(elementId, out var component))
                return component;
            throw MissingElement(elementId, typeof(T));
        }

        public bool TryGet<T>(string elementId, out T component) where T : Component
        {
            return TryFindElement(elementId, static element => element.GetComponentInChildren<T>(true), out component);
        }

        public GameObject GetElement(string elementId)
        {
            if (TryFindElement(elementId, static element => element, out GameObject match))
                return match;
            throw MissingElement(elementId, typeof(GameObject));
        }

        public async Awaitable<UINavResult> Close(object result = null)
        {
            await Awaitable.MainThreadAsync();
            if (Root)
                return await Root.ClosePopupAsync(this, result);

            var scene = this ? gameObject.scene.name : "(畫面已銷毀)";
            Debug.LogWarning($"UniGameUI: Close '{Id}' 找不到所屬工作區。（Scene: {scene}）", this);
            return UINavResult.Unavailable;
        }

        private bool TryFindElement<T>(string elementId, Func<GameObject, T> find, out T match) where T : UnityEngine.Object
        {
            match = null;
            if (string.IsNullOrWhiteSpace(elementId))
                return false;
            if (elements != null && TryFindCached(elementId, find, out match))
                return true;

            elements ??= new Dictionary<string, UIElementId>(StringComparer.Ordinal);
            elements.Clear();
            foreach (var element in GetComponentsInChildren<UIElementId>(true))
            {
                if (element && !string.IsNullOrWhiteSpace(element.Id))
                    elements.TryAdd(element.Id, element);
            }
            return TryFindCached(elementId, find, out match);
        }

        private bool TryFindCached<T>(string elementId, Func<GameObject, T> find, out T match) where T : UnityEngine.Object
        {
            match = null;
            if (!elements.TryGetValue(elementId, out var element) || !element ||
                element.Id != elementId || !element.transform.IsChildOf(transform))
                return false;
            match = find(element.gameObject);
            return match;
        }

        private InvalidOperationException MissingElement(string elementId, Type type)
        {
            return new InvalidOperationException($"畫面「{Id}」找不到元件 ID「{elementId}」的「{type}」" +
                $"（Scene: {gameObject.scene.name}）。");
        }

        private void OnDestroy()
        {
            if (Root)
                Root.OnViewDestroyed(this);
        }

        internal async Awaitable ShowAsync(object data, CancellationToken cancellation, string request)
        {
            cancellation.ThrowIfCancellationRequested();
            Data = data;
            SetInput(false);
            gameObject.SetActive(true);
            Root.NotifyDuringNavigation(request, nameof(Showing), Id, () =>
            {
                try
                {
                    InvokeHandlers(handler => handler.OnShowing(this, data), null);
                    UIEventDispatch.Invoke(showing, null);
                }
                catch (Exception exception)
                {
                    throw new ShowingException(Id, exception);
                }
                Root.NotifyShowing(this, request);
            });
            await UITransition.PlayAsync(this, enter, true, cancellation);
        }

        internal async Awaitable HideAsync(CancellationToken cancellation, string request)
        {
            cancellation.ThrowIfCancellationRequested();
            SetInput(false);
            Notify(hiding, handler => handler.OnHiding(this), Root.NotifyHiding, request, nameof(Hiding));
            await UITransition.PlayAsync(this, exit, false, cancellation);
        }

        internal void FinishShowing(string request)
        {
            Notify(shown, handler => handler.OnShown(this), Root.NotifyShown, request, nameof(Shown));
        }

        internal void FinishHiding(string request)
        {
            gameObject.SetActive(false);
            Notify(hidden, handler => handler.OnHidden(this), Root.NotifyHidden, request, nameof(Hidden));
        }

        internal bool Emit(string action, Action<Exception> report)
        {
            return InvokeHandlers(handler => handler.OnAction(this, action), report);
        }

        private void Notify(UnityEvent signal, Action<IUIViewHandler> invoke, Action<UIView, string> notifyRoot,
            string request, string phase)
        {
            void Report(Exception exception) => Root.LogNotificationException($"{request} {phase} '{Id}'", exception);

            Root.NotifyDuringNavigation(request, phase, Id, () =>
            {
                InvokeHandlers(invoke, Report);
                UIEventDispatch.Invoke(signal, Report);
                notifyRoot(this, request);
            });
        }

        private bool InvokeHandlers(Action<IUIViewHandler> invoke, Action<Exception> report)
        {
            var succeeded = true;
            foreach (var handler in GetComponents<IUIViewHandler>())
            {
                if (handler is not UnityEngine.Object component || !component)
                    continue;
                try
                {
                    invoke(handler);
                }
                catch (Exception exception) when (report != null)
                {
                    report(exception);
                    succeeded = false;
                }
            }
            return succeeded;
        }

        internal void SetInput(bool enabled)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        internal sealed class ShowingException : Exception
        {
            internal ShowingException(string viewId, Exception exception) : base(exception.Message, exception)
            {
                ViewId = viewId;
            }

            internal string ViewId { get; }
        }
    }
}
