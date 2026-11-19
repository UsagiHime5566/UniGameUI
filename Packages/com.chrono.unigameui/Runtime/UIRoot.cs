using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UniGameUI
{
    [Serializable]
    public sealed class UIActionEvent : UnityEvent<string, string> { }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [AddComponentMenu("UniGameUI/UI Root")]
    public sealed partial class UIRoot : MonoBehaviour
    {
        [SerializeField] internal UIAuthoringMode mode;
        [SerializeField] internal Vector2 referenceResolution = new Vector2(1920, 1080);
        [SerializeField, Min(1)] internal int reviewColumns = 3;
        [SerializeField, HideInInspector] internal UIPageTransitionMode pageTransitionMode;
        [SerializeField, HideInInspector] internal RectTransform reviewLayer;
        [SerializeField, HideInInspector] internal string startPageId;
        [SerializeField, HideInInspector] internal List<UIEntry> entries = new List<UIEntry>();
        [SerializeField, HideInInspector] internal List<UIRoute> routes = new List<UIRoute>();
        [SerializeField] private UIActionEvent actionEmitted = new UIActionEvent();

        private readonly Dictionary<string, UIEntry> entriesById = new Dictionary<string, UIEntry>();
        private readonly Dictionary<(string, string), UIRoute> routesByAction = new Dictionary<(string, string), UIRoute>();
        private readonly Dictionary<string, UIView> pages = new Dictionary<string, UIView>();
        private readonly List<string> history = new List<string>();
        private readonly List<UIView> popups = new List<UIView>();
        private readonly Dictionary<UIView, AwaitableCompletionSource<object>> popupResults =
            new Dictionary<UIView, AwaitableCompletionSource<object>>();
        private IReadOnlyList<UIView> popupList;
        private List<(AwaitableCompletionSource<object> Source, object Result)> closedPopupResults;
        private RectTransform pageLayer;
        private RectTransform popupLayer;
        private RectTransform stagingLayer;
        private UIView currentPage;
        private CancellationTokenSource lifetime;
        private (string Request, string Phase, string ViewId)? notification;
        private bool ready;
        private bool busy;

        public UIAuthoringMode Mode => mode;
        public Vector2 ReferenceResolution => referenceResolution;
        public int ReviewColumns => reviewColumns;
        public UIPageTransitionMode PageTransitionMode => pageTransitionMode;
        public IReadOnlyList<UIEntry> Entries => entries;
        public IReadOnlyList<UIRoute> Routes => routes;
        public string StartPageId => startPageId;
        public UIView CurrentPage => currentPage ? currentPage : null;
        public UIView TopPopup => popups.Count == 0 ? null : popups[^1];
        public IReadOnlyList<UIView> Popups => popupList ??= popups.AsReadOnly();
        public int HistoryCount => history.Count;
        public string CurrentPageId => CurrentPage ? CurrentPage.Id : null;
        public string CurrentPopupId => TopPopup ? TopPopup.Id : null;
        public bool IsTransitioning => busy;
        public UIActionEvent ActionEmitted => actionEmitted;

        public event Action<UIView> ViewShowing;
        public event Action<UIView> ViewShown;
        public event Action<UIView> ViewHiding;
        public event Action<UIView> ViewHidden;
        public event Action<UIRoute> ActionTriggered;
        public event Action<UIRoute, UINavResult> ActionCompleted;

        private void Awake()
        {
            if (reviewLayer)
            {
                reviewLayer.gameObject.SetActive(false);
                stagingLayer = reviewLayer;
            }

            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transform.localScale = Vector3.one;
            GetComponent<CanvasScaler>().referenceResolution = referenceResolution;
            pageLayer = CreateLayer("Pages");
            popupLayer = CreateLayer("Popups");

            foreach (var entry in entries)
            {
                var id = entry?.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    NotFound("Awake", $"畫面 ID '{id}' 缺漏。");
                    continue;
                }
                if (!entriesById.TryAdd(id, entry))
                {
                    NotFound("Awake", $"畫面 ID '{id}' 重複。");
                    continue;
                }

                if (entry.Kind == UIEntryKind.Page && mode == UIAuthoringMode.SingleScene && entry.Review)
                {
                    Prepare(entry.Review, pageLayer);
                    pages.Add(entry.Id, entry.Review);
                }
            }

            foreach (var route in routes)
            {
                var source = route?.Source;
                var action = route?.Action;
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(action) ||
                    !entriesById.ContainsKey(source))
                    NotFound("Awake", $"路由 '{source}'/'{action}' 的來源或動作缺漏。");
                else if (!routesByAction.TryAdd((source, action), route))
                    NotFound("Awake", $"路由 '{source}'/'{action}' 重複。");
            }
            ready = true;
        }

        private void OnEnable()
        {
            lifetime = new CancellationTokenSource();
        }

        private async void Start()
        {
            if (ready && !string.IsNullOrEmpty(startPageId))
                await ShowPageAsync(startPageId);
        }

        private void OnDisable()
        {
            var cancellation = lifetime;
            lifetime = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
        }

        public Awaitable<UINavResult> ShowPageAsync(
            string id, object data = null, CancellationToken cancellation = default)
        {
            return ShowPageAsync(id, data, UIHistoryMode.Push, cancellation);
        }

        public Awaitable<UINavResult> ShowPageAsync(
            string id, object data, UIHistoryMode history, CancellationToken cancellation = default)
        {
            var request = $"ShowPage '{id}'";
            return RunAsync(request, token => ShowPageCoreAsync(id, data, history, token, request), cancellation);
        }

        public bool TryGetView(string id, out UIView view)
        {
            view = null;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            if (pages.TryGetValue(id, out view) && view)
                return true;
            view = popups.Find(popup => popup && popup.Id == id);
            return view;
        }

        public void ClearHistory()
        {
            history.Clear();
        }

        public Awaitable<UINavResult> OpenPopupAsync(
            string id, object data = null, CancellationToken cancellation = default)
        {
            var request = $"OpenPopup '{id}'";
            return RunAsync(request, token => OpenPopupCoreAsync(id, data, token, request), cancellation);
        }

        public async Awaitable<UIPopupResult<T>> OpenPopupForResultAsync<T>(
            string id, object data = null, CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            var request = $"OpenPopupForResult '{id}'";
            var rootLifetime = lifetime?.Token ?? default;
            UIView popup = null;
            AwaitableCompletionSource<object> resultSource = null;
            var status = await RunAsync(request, token => OpenPopupCoreAsync(id, data, token, request, opened =>
            {
                popup = opened;
                resultSource = new AwaitableCompletionSource<object>();
                popupResults.Add(popup, resultSource);
            }), cancellation);
            if (status != UINavResult.Completed)
                return new UIPopupResult<T>(status);
            if (resultSource == null)
                return new UIPopupResult<T>(UINavResult.Canceled);

            using var waiting = CancellationTokenSource.CreateLinkedTokenSource(rootLifetime, cancellation);
            using var registration = waiting.Token.Register(() => resultSource.TrySetCanceled());
            object result;
            try
            {
                result = await resultSource.Awaitable;
            }
            catch (OperationCanceledException)
            {
                return new UIPopupResult<T>(UINavResult.Canceled);
            }
            finally
            {
                await Awaitable.MainThreadAsync();
                popupResults.Remove(popup);
            }

            if (result is T value)
                return new UIPopupResult<T>(UINavResult.Completed, true, value);
            if (result != null)
                Debug.LogError(Diagnostic(request, $"回傳型別「{result.GetType()}」與期望型別「{typeof(T)}」不符。"), this);
            return new UIPopupResult<T>(UINavResult.Completed);
        }

        public async Awaitable<UINavResult> ClosePopupAsync(CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            var request = $"ClosePopup '{CurrentPopupId ?? "(無)"}'";
            return await RunAsync(request, token => ClosePopupCoreAsync(token, request, null), cancellation);
        }

        internal async Awaitable<UINavResult> ClosePopupAsync(UIView popup, object result)
        {
            var request = $"Close '{popup.Id}'";
            if (busy && notification.HasValue)
                return Busy(request);
            if (!popup || popup != TopPopup)
                return Unavailable(request, "只有目前最上層且開著的 Popup 能關閉。");
            return await RunAsync(request, token => ClosePopupCoreAsync(token, request, result), default);
        }

        public async Awaitable<UINavResult> BackAsync(CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            var request = $"Back '{CurrentPopupId ?? CurrentPageId ?? "(無)"}'";
            return await RunAsync(request, token => BackCoreAsync(token, request), cancellation);
        }

        public async Awaitable<UINavResult> TriggerAsync(
            string source, string action, CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            var request = $"Trigger '{source}'/'{action}'";
            if (busy)
                return Busy(request);
            if (!CanNavigate())
                return Unavailable(request, "工作區停用或尚未就緒。");
            if (cancellation.IsCancellationRequested)
                return UINavResult.Canceled;

            if (!routesByAction.TryGetValue((source, action), out var route))
                return NotFound(request, "找不到路由。");
            var active = TopPopup ? TopPopup : currentPage;
            if (!active || active.Id != source)
                return Unavailable(request, "來源不是目前最上層的畫面。");

            void Triggered() => UIEventDispatch.Invoke(ActionTriggered, listener => listener(route),
                exception => LogNotificationException($"{request} {nameof(ActionTriggered)}", exception));

            UINavResult result;
            if (route.Kind == UIRouteKind.Emit)
            {
                var rootLifetime = lifetime.Token;
                Triggered();
                result = rootLifetime.IsCancellationRequested || cancellation.IsCancellationRequested
                    ? UINavResult.Canceled : Emit(active, route, request);
            }
            else
            {
                result = await RunAsync(request, async token =>
                {
                    NotifyDuringNavigation(request, nameof(ActionTriggered), source, Triggered);
                    token.ThrowIfCancellationRequested();
                    return await FollowRouteAsync(route, token, request);
                }, cancellation);
            }

            UIEventDispatch.Invoke(ActionCompleted, listener => listener(route, result),
                exception => LogNotificationException($"{request} {nameof(ActionCompleted)}", exception));
            return result;
        }

        private UINavResult Emit(UIView source, UIRoute route, string request)
        {
            try
            {
                var handlersSucceeded = source.Emit(route.Action,
                    exception => LogFailure($"{request} {nameof(IUIViewHandler.OnAction)}", exception));
                var eventSucceeded = UIEventDispatch.Invoke(actionEmitted,
                    exception => LogFailure($"{request} {nameof(ActionEmitted)}", exception), route.Source, route.Action);
                return handlersSucceeded && eventSucceeded ? UINavResult.Completed : UINavResult.Failed;
            }
            catch (Exception exception)
            {
                return Failed(request, exception);
            }
        }

        internal void NotifyShowing(UIView view, string request) => NotifyView(ViewShowing, view, request, nameof(ViewShowing));
        internal void NotifyShown(UIView view, string request) => NotifyView(ViewShown, view, request, nameof(ViewShown));
        internal void NotifyHiding(UIView view, string request) => NotifyView(ViewHiding, view, request, nameof(ViewHiding));
        internal void NotifyHidden(UIView view, string request) => NotifyView(ViewHidden, view, request, nameof(ViewHidden));

        internal void NotifyDuringNavigation(string request, string phase, string viewId, Action notify)
        {
            var previous = notification;
            notification = (request, phase, viewId);
            try
            {
                notify();
            }
            finally
            {
                notification = previous;
            }
        }

        private void NotifyView(Action<UIView> signal, UIView view, string request, string phase)
        {
            UIEventDispatch.Invoke(signal, listener => listener(view),
                exception => LogNotificationException($"{request} {phase} '{view.Id}'", exception));
        }

        private async Awaitable<UINavResult> RunAsync(
            string request, Func<CancellationToken, Awaitable<UINavResult>> operation, CancellationToken cancellation)
        {
            await Awaitable.MainThreadAsync();
            if (busy)
                return Busy(request);
            if (!CanNavigate())
                return Unavailable(request, "工作區停用或尚未就緒。");

            busy = true;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellation);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                RefreshInput();
                return await operation(linked.Token);
            }
            catch (OperationCanceledException)
            {
                return UINavResult.Canceled;
            }
            catch (UIView.ShowingException exception)
            {
                return Failed($"{request} Showing '{exception.ViewId}'", exception.InnerException);
            }
            catch (Exception exception)
            {
                return Failed(request, exception);
            }
            finally
            {
                var results = closedPopupResults;
                closedPopupResults = null;
                busy = false;
                RefreshInput();
                // Awaitable continuations run immediately, so release the navigation lock before resuming callers.
                if (results != null)
                {
                    foreach (var (source, result) in results)
                        source.TrySetResult(result);
                }
            }
        }

        private UINavResult NotFound(string request, string detail)
        {
            Debug.LogError(Diagnostic(request, detail), this);
            return UINavResult.NotFound;
        }

        private UINavResult Busy(string request)
        {
            if (notification is { } active)
            {
                Debug.LogWarning(Diagnostic(request, $"目前導航 {active.Request} 正在通知 {active.Phase}" +
                    $"（畫面 '{active.ViewId}'）。請等這次導航結束後再呼叫。"), this);
            }
            return UINavResult.Busy;
        }

        private UINavResult Unavailable(string request, string detail)
        {
            Debug.LogWarning(Diagnostic(request, detail), this);
            return UINavResult.Unavailable;
        }

        private UINavResult Failed(string request, Exception exception)
        {
            LogFailure(request, exception);
            return UINavResult.Failed;
        }

        private void LogFailure(string request, Exception exception)
        {
            LogNavigationException(request, "執行失敗。", exception);
        }

        internal void LogNotificationException(string request, Exception exception)
        {
            LogNavigationException(request, "接收者丟出例外，導航照常處理。", exception);
        }

        // 和導航無關的通知（語言切換、輸入模式等）出錯時使用。
        private void LogReceiverException(string request, Exception exception)
        {
            LogNavigationException(request, "接收者丟出例外，其餘接收者照常執行。", exception);
        }

        private void LogNavigationException(string request, string detail, Exception exception)
        {
            Debug.LogError(Diagnostic(request, detail + exception.Message), this);
            Debug.LogException(exception, this);
        }

        private string Diagnostic(string request, string detail)
        {
            var scene = this ? gameObject.scene.name : "(工作區已銷毀)";
            return $"UniGameUI: {request} {detail}（Scene: {scene}）";
        }

        private bool CanNavigate()
        {
            return this && ready && isActiveAndEnabled && lifetime != null;
        }

        private async Awaitable<UINavResult> FollowRouteAsync(
            UIRoute route, CancellationToken cancellation, string request)
        {
            switch (route.Kind)
            {
                case UIRouteKind.ShowPage:
                    return await ShowPageCoreAsync(route.Target, null, route.History, cancellation, request);
                case UIRouteKind.OpenPopup:
                    return await OpenPopupCoreAsync(route.Target, null, cancellation, request);
                case UIRouteKind.Back:
                    return await BackCoreAsync(cancellation, request);
                case UIRouteKind.ClosePopup:
                    return await ClosePopupCoreAsync(cancellation, request,
                        string.IsNullOrEmpty(route.Result) ? null : route.Result);
                default:
                    throw new InvalidOperationException($"無法識別路由種類 '{route.Kind}'。");
            }
        }

        private async Awaitable<UINavResult> ShowPageCoreAsync(
            string id, object data, UIHistoryMode historyMode, CancellationToken cancellation, string request)
        {
            if (string.IsNullOrWhiteSpace(id) || !GetPage(id, out var next))
                return NotFound(request, $"找不到頁面 '{id}'。");
            if (CurrentPageId == id)
                return Unavailable(request, $"目前已在頁面 '{id}'。");
            if (!Enum.IsDefined(typeof(UIHistoryMode), historyMode))
                throw new InvalidOperationException($"無法識別頁面歷史模式 '{historyMode}'。");

            var previousId = CurrentPageId;
            await ChangePageAsync(next, data, cancellation, request, () =>
            {
                if (historyMode == UIHistoryMode.Clear)
                    history.Clear();
                else if (historyMode == UIHistoryMode.Push && previousId != null)
                    history.Add(previousId);
            });
            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> BackCoreAsync(CancellationToken cancellation, string request)
        {
            if (popups.Count > 0)
                return await ClosePopupCoreAsync(cancellation, request, null);
            RemoveCurrentPageFromHistory();
            if (history.Count == 0)
                return UINavResult.Unavailable;
            if (!GetPage(history[^1], out var previous))
                return NotFound(request, $"找不到上一頁 '{history[^1]}'。");

            await ChangePageAsync(previous, previous.Data, cancellation, request, () =>
            {
                if (history.Count > 0)
                    history.RemoveAt(history.Count - 1);
            });
            return UINavResult.Completed;
        }

        private void RemoveCurrentPageFromHistory()
        {
            var id = CurrentPageId;
            while (history.Count > 0 && history[^1] == id)
                history.RemoveAt(history.Count - 1);
        }

        private async Awaitable ChangePageAsync(UIView next, object data, CancellationToken cancellation,
            string request, Action commitHistory)
        {
            var previous = currentPage;
            if (next == previous)
                throw new InvalidOperationException($"畫面 '{next.Id}' 已在顯示中，無法對同一個 View 切頁。");

            var previousData = next.Data;
            var openPopups = popups.ToArray();
            try
            {
                for (var index = openPopups.Length - 1; index >= 0; index--)
                {
                    var popup = openPopups[index];
                    if (popup)
                        await HidePopupAsync(popup, cancellation, request);
                }
                if (previous && pageTransitionMode == UIPageTransitionMode.Concurrent)
                {
                    await ChangePageConcurrentlyAsync(previous, next, data, cancellation, request);
                }
                else
                {
                    if (previous)
                        await HideViewAsync(previous, cancellation, request);
                    await next.ShowAsync(data, cancellation, request);
                }
                cancellation.ThrowIfCancellationRequested();
            }
            catch
            {
                if (next)
                {
                    next.gameObject.SetActive(false);
                    next.Data = previousData;
                }
                if (previous)
                    previous.gameObject.SetActive(true);
                foreach (var popup in openPopups)
                {
                    if (popup)
                        popup.gameObject.SetActive(true);
                }
                throw;
            }

            currentPage = next;
            commitHistory();
            RemoveCurrentPageFromHistory();
            for (var index = openPopups.Length - 1; index >= 0; index--)
            {
                if (openPopups[index])
                    FinishClosingPopup(openPopups[index], null, request);
            }
            if (previous)
                previous.FinishHiding(request);
            next.FinishShowing(request);
        }

        private static async Awaitable HideViewAsync(UIView view, CancellationToken cancellation, string request)
        {
            await view.HideAsync(cancellation, request);
            cancellation.ThrowIfCancellationRequested();
            view.gameObject.SetActive(false);
        }

        private static async Awaitable ChangePageConcurrentlyAsync(
            UIView previous, UIView next, object data, CancellationToken cancellation, string request)
        {
            next.transform.SetAsLastSibling();
            using var pair = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var completion = new AwaitableCompletionSource();
            ExceptionDispatchInfo failure = null;
            var remaining = 2;

            Observe(HideViewAsync(previous, pair.Token, request));
            Observe(next.ShowAsync(data, pair.Token, request));
            await completion.Awaitable;
            failure?.Throw();

            async void Observe(Awaitable animation)
            {
                try
                {
                    await animation;
                }
                catch (Exception exception)
                {
                    if (failure == null)
                    {
                        failure = ExceptionDispatchInfo.Capture(exception);
                        pair.Cancel();
                    }
                }
                finally
                {
                    if (--remaining == 0)
                        completion.TrySetResult();
                }
            }
        }

        private async Awaitable<UINavResult> OpenPopupCoreAsync(
            string id, object data, CancellationToken cancellation, string request, Action<UIView> onOpened = null)
        {
            if (string.IsNullOrWhiteSpace(id) || !entriesById.TryGetValue(id, out var entry) ||
                entry.Kind != UIEntryKind.Popup)
                return NotFound(request, $"找不到 Popup '{id}'。");
            if (!entry.Prefab)
                return NotFound(request, $"Popup '{id}' 缺少 Prefab。");
            if (popups.Exists(popup => popup && popup.Id == id))
                return Unavailable(request, $"Popup '{id}' 已經開啟。");

            var popup = InstantiateView(entry, popupLayer);
            using var transition = CancellationTokenSource.CreateLinkedTokenSource(cancellation, popup.destroyCancellationToken);
            try
            {
                await popup.ShowAsync(data, transition.Token, request);
                transition.Token.ThrowIfCancellationRequested();
            }
            catch
            {
                if (popup)
                {
                    popup.gameObject.SetActive(false);
                    Destroy(popup.gameObject);
                }
                throw;
            }

            popups.Add(popup);
            popup.FinishShowing(request);
            if (popup)
                onOpened?.Invoke(popup);
            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> ClosePopupCoreAsync(
            CancellationToken cancellation, string request, object result)
        {
            var popup = TopPopup;
            if (!popup)
                return Unavailable(request, "目前沒有 Popup 可關閉。");

            await HidePopupAsync(popup, cancellation, request);
            FinishClosingPopup(popup, result, request);
            return UINavResult.Completed;
        }

        private static async Awaitable HidePopupAsync(UIView popup, CancellationToken cancellation, string request)
        {
            using var transition = CancellationTokenSource.CreateLinkedTokenSource(cancellation, popup.destroyCancellationToken);
            await HideViewAsync(popup, transition.Token, request);
        }

        private void FinishClosingPopup(UIView popup, object result, string request)
        {
            popups.Remove(popup);
            if (popupResults.Remove(popup, out var pending))
            {
                closedPopupResults ??= new List<(AwaitableCompletionSource<object>, object)>();
                closedPopupResults.Add((pending, result));
            }
            popup.FinishHiding(request);
            if (popup)
                Destroy(popup.gameObject);
        }

        internal void OnViewDestroyed(UIView view)
        {
            if (popups.Remove(view))
                RefreshInput();
            if (popupResults.Remove(view, out var pending))
                pending.TrySetCanceled();
        }

        private bool GetPage(string id, out UIView page)
        {
            if (pages.TryGetValue(id, out page) && page)
                return true;
            if (mode != UIAuthoringMode.Prefab || !entriesById.TryGetValue(id, out var entry) ||
                entry.Kind != UIEntryKind.Page || !entry.Prefab)
                return false;

            page = InstantiateView(entry, pageLayer);
            pages[id] = page;
            return true;
        }

        private UIView InstantiateView(UIEntry entry, RectTransform parent)
        {
            if (!stagingLayer)
            {
                stagingLayer = CreateLayer("Prefab Staging");
                stagingLayer.gameObject.SetActive(false);
            }
            var view = Instantiate(entry.Prefab, stagingLayer, false);
            Prepare(view, parent);
            return view;
        }

        private void Prepare(UIView view, RectTransform parent)
        {
            view.Root = this;
            view.gameObject.SetActive(false);
            var rect = (RectTransform)view.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            view.SetInput(false);
        }

        private void RefreshInput()
        {
            if (currentPage)
                currentPage.SetInput(!busy && popups.Count == 0);
            for (var index = 0; index < popups.Count; index++)
            {
                if (popups[index])
                    popups[index].SetInput(!busy && index == popups.Count - 1);
            }
        }

        private RectTransform CreateLayer(string layerName)
        {
            var layer = (RectTransform)new GameObject(layerName, typeof(RectTransform)).transform;
            layer.SetParent(transform, false);
            Stretch(layer);
            return layer;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }
    }
}
