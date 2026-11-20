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

    // 給 Editor 的藍圖視窗用（不是公開 API）：一次不是透過路由的導航，例如程式直接呼叫 ShowPageAsync。
    internal readonly struct UIDirectNavigation
    {
        internal UIDirectNavigation(UIRouteKind kind, string target, UINavResult result, bool backKey)
        {
            Kind = kind;
            Target = target;
            Result = result;
            BackKey = backKey;
        }

        // ShowPage、OpenPopup、ClosePopup 或 Back。
        internal UIRouteKind Kind { get; }
        internal string Target { get; }
        internal UINavResult Result { get; }
        // 由返回鍵觸發的 Back。
        internal bool BackKey { get; }
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [AddComponentMenu("UniGameUI/UI Root")]
    public sealed partial class UIRoot : MonoBehaviour
    {
        /// <summary>
        /// <see cref="ConfirmAsync"/> 預設開啟的 Popup ID，也是起始確認框 Prefab 的畫面 ID。
        /// </summary>
        public const string DefaultConfirmPopupId = "confirm";

        /// <summary>
        /// 藍圖上「任何畫面」節點的路由來源。從它拉出去的路由是訊號路由，不論目前顯示哪個畫面都成立，
        /// 用 <see cref="SignalAsync"/> 送出。
        /// </summary>
        public const string AnySource = "*";

        // 起始確認框的安裝位置，只用來組出登記用的提示。
        internal const string StarterConfirmPrefabPath = "Assets/UniGameUI/Widgets/Confirm.prefab";

        [SerializeField] internal UIAuthoringMode mode;
        [SerializeField] internal Vector2 referenceResolution = new Vector2(1920, 1080);
        [SerializeField, Min(1)] internal int reviewColumns = 3;
        [SerializeField, HideInInspector] internal UIPageTransitionMode pageTransitionMode;
        [SerializeField, HideInInspector] internal RectTransform reviewLayer;
        [SerializeField, HideInInspector] internal string startPageId;
        [SerializeField, HideInInspector] internal List<UIEntry> entries = new List<UIEntry>();
        [SerializeField, HideInInspector] internal List<UIRoute> routes = new List<UIRoute>();
        // 藍圖視窗裡「任何畫面」節點的位置。它不是畫面，不在 entries 裡。
        [SerializeField, HideInInspector] internal Vector2 anySourceGraphPosition = new Vector2(776, 32);
        [SerializeField] private UIActionEvent actionEmitted = new UIActionEvent();

        private readonly Dictionary<string, UIEntry> entriesById = new Dictionary<string, UIEntry>();
        private readonly Dictionary<(string, string), UIRoute> routesByAction = new Dictionary<(string, string), UIRoute>();
        private readonly Dictionary<string, UIView> pages = new Dictionary<string, UIView>();
        private readonly List<string> history = new List<string>();
        private readonly List<UIView> popups = new List<UIView>();
        private readonly Dictionary<UIView, AwaitableCompletionSource<object>> popupResults =
            new Dictionary<UIView, AwaitableCompletionSource<object>>();
        private readonly Dictionary<string, UIView> retainedPopups = new Dictionary<string, UIView>();
        // 以 history: Clear 開啟、目前還開著的 Popup。它們在最上層時不能返回。
        private readonly HashSet<UIView> unreturnablePopups = new HashSet<UIView>();
        // 排隊中的訊號，依送出的順序執行。
        private readonly List<object> signals = new List<object>();
        private IReadOnlyList<UIView> popupList;
        private IReadOnlyList<string> historyList;
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

        // 給 Editor 的藍圖視窗用（不是公開 API）：返回歷史裡的頁面，由遠到近。
        internal IReadOnlyList<string> HistoryIds => historyList ??= history.AsReadOnly();

        // 給 Editor 的藍圖視窗用（不是公開 API）：不是透過路由的導航結束時通知。
        // 透過路由的導航由 ActionCompleted 通知。沒有人訂閱時不做任何事，也不多一層等待。
        internal event Action<UIDirectNavigation> DirectNavigationFinished;

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
                    (source != AnySource && !entriesById.ContainsKey(source)))
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
            UnhookInput();
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
            var navigation = RunAsync(request, token => ShowPageCoreAsync(id, data, history, token, request), cancellation);
            return DirectNavigationFinished == null
                ? navigation
                : ReportDirectAsync(UIRouteKind.ShowPage, id, navigation);
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
            return OpenPopupAsync(id, data, UIHistoryMode.Push, cancellation);
        }

        /// <summary>
        /// 開啟 Popup 並指定能不能返回。<see cref="UIHistoryMode.Push"/> 是一般的 Popup，可以用返回關閉。
        /// <see cref="UIHistoryMode.Clear"/> 開啟「不可返回的 Popup」：開啟時清空歷史，它在最上層時返回無效，
        /// 它開著的期間切頁一律清空歷史；ClosePopup 路由、View.Close() 與點遮罩關閉照常可以關閉它。
        /// Popup 沒有 <see cref="UIHistoryMode.Replace"/>，傳入會丟出 <see cref="ArgumentException"/>。
        /// </summary>
        public Awaitable<UINavResult> OpenPopupAsync(
            string id, object data, UIHistoryMode history, CancellationToken cancellation = default)
        {
            if (history != UIHistoryMode.Push && history != UIHistoryMode.Clear)
                throw new ArgumentException($"OpenPopup 的 history 只能是 Push 或 Clear，不支援「{history}」。", nameof(history));
            var request = $"OpenPopup '{id}'";
            var navigation = RunAsync(request, token => OpenPopupCoreAsync(id, data, token, request, null, history),
                cancellation);
            return DirectNavigationFinished == null
                ? navigation
                : ReportDirectAsync(UIRouteKind.OpenPopup, id, navigation);
        }

        public Awaitable<UIPopupResult<T>> OpenPopupForResultAsync<T>(
            string id, object data = null, CancellationToken cancellation = default)
        {
            return OpenPopupForResultCoreAsync<T>(id, data, cancellation, $"OpenPopupForResult '{id}'", null);
        }

        /// <summary>
        /// 開啟確認框並等待玩家的選擇。只有按下確定時回傳 true；取消、返回鍵、等待被取消，
        /// 或確認框沒有顯示出來時都回傳 false。
        /// </summary>
        public async Awaitable<bool> ConfirmAsync(
            string message, string confirmText = null, string cancelText = null, string title = null,
            string popupId = DefaultConfirmPopupId, CancellationToken cancellation = default)
        {
            var request = $"Confirm '{popupId}'";
            var opened = UINavResult.Completed;
            var result = await OpenPopupForResultCoreAsync<bool>(popupId, new UIConfirmRequest
            {
                Message = message,
                Title = title,
                ConfirmText = confirmText,
                CancelText = cancelText
            }, cancellation, request, status => opened = status);
            if (opened == UINavResult.Completed)
                return result.HasValue && result.Value;

            var hint = opened == UINavResult.NotFound
                ? "請在計畫的 views 登記確認框，例如：" +
                  $"{{ \"id\": \"{popupId}\", \"kind\": \"Popup\", \"prefabPath\": \"{StarterConfirmPrefabPath}\" }}"
                : "";
            Debug.LogWarning(Diagnostic(request, $"確認框沒有顯示出來（{opened}），當成取消。{hint}"), this);
            return false;
        }

        private async Awaitable<UIPopupResult<T>> OpenPopupForResultCoreAsync<T>(
            string id, object data, CancellationToken cancellation, string request, Action<UINavResult> opened)
        {
            await Awaitable.MainThreadAsync();
            var rootLifetime = lifetime?.Token ?? default;
            UIView popup = null;
            AwaitableCompletionSource<object> resultSource = null;
            var status = await RunAsync(request, token => OpenPopupCoreAsync(id, data, token, request, opened =>
            {
                popup = opened;
                resultSource = new AwaitableCompletionSource<object>();
                popupResults.Add(popup, resultSource);
            }), cancellation);
            opened?.Invoke(status);
            ReportDirect(UIRouteKind.OpenPopup, id, status);
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
            var popupId = CurrentPopupId;
            var request = $"ClosePopup '{popupId ?? "(無)"}'";
            var status = await RunAsync(request, token => ClosePopupCoreAsync(token, request, null), cancellation);
            ReportDirect(UIRouteKind.ClosePopup, popupId, status);
            return status;
        }

        internal async Awaitable<UINavResult> ClosePopupAsync(UIView popup, object result)
        {
            var popupId = popup ? popup.Id : null;
            var request = $"Close '{popupId}'";
            UINavResult status;
            if (busy && notification.HasValue)
                status = Busy(request);
            else if (!popup || popup != TopPopup)
                status = Unavailable(request, "只有目前最上層且開著的 Popup 能關閉。");
            else
                status = await RunAsync(request, token => ClosePopupCoreAsync(token, request, result), default);
            ReportDirect(UIRouteKind.ClosePopup, popupId, status);
            return status;
        }

        public Awaitable<UINavResult> BackAsync(CancellationToken cancellation = default)
        {
            return BackAsync(cancellation, false);
        }

        private async Awaitable<UINavResult> BackAsync(CancellationToken cancellation, bool backKey)
        {
            await Awaitable.MainThreadAsync();
            var request = $"Back '{CurrentPopupId ?? CurrentPageId ?? "(無)"}'";
            var status = await RunAsync(request, token => BackCoreAsync(token, request), cancellation);
            if (DirectNavigationFinished != null)
                ReportDirect(new UIDirectNavigation(UIRouteKind.Back, null, status, backKey));
            return status;
        }

        private async Awaitable<UINavResult> ReportDirectAsync(
            UIRouteKind kind, string target, Awaitable<UINavResult> navigation)
        {
            var status = await navigation;
            ReportDirect(kind, target, status);
            return status;
        }

        private void ReportDirect(UIRouteKind kind, string target, UINavResult result)
        {
            if (DirectNavigationFinished != null)
                ReportDirect(new UIDirectNavigation(kind, target, result, false));
        }

        private void ReportDirect(UIDirectNavigation navigation)
        {
            UIEventDispatch.Invoke(DirectNavigationFinished, listener => listener(navigation),
                exception => LogReceiverException(nameof(DirectNavigationFinished), exception));
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

            if (source == AnySource)
                return Unavailable(request, "「任何畫面」的訊號路由要用 SignalAsync 送出。");
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

        /// <summary>
        /// 送出訊號：依藍圖上「任何畫面」節點的路由執行。不論目前顯示哪個畫面都成立。
        /// 訊號不會因為忙碌而被拒絕：有導航正在進行時等它結束再執行，多個訊號依送出的順序執行。
        /// ShowPage 與 OpenPopup 的訊號會把 <paramref name="data"/> 傳給目標畫面；Emit 的訊號不帶資料。
        /// 等待期間 token 被取消，或工作區被停用、銷毀時回傳 <see cref="UINavResult.Canceled"/>；
        /// 找不到這個動作的訊號路由時回傳 <see cref="UINavResult.NotFound"/>。
        /// </summary>
        public async Awaitable<UINavResult> SignalAsync(
            string action, object data = null, CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            var request = $"Signal '{action}'";
            if (!CanNavigate())
                return Unavailable(request, "工作區停用或尚未就緒。");
            if (cancellation.IsCancellationRequested)
                return UINavResult.Canceled;
            if (string.IsNullOrEmpty(action) || !routesByAction.TryGetValue((AnySource, action), out var route))
                return NotFound(request, "找不到這個動作的訊號路由。請在藍圖的「任何畫面」節點加上路由。");

            var ticket = new object();
            signals.Add(ticket);
            try
            {
                using var waiting = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellation);
                while (true)
                {
                    // 等進行中的導航與排在前面的訊號結束。
                    while (busy || !ReferenceEquals(signals[0], ticket))
                        await Awaitable.NextFrameAsync(waiting.Token);
                    var result = await RunSignalAsync(route, data, request, waiting.Token);
                    if (result != UINavResult.Busy)
                        return result;
                }
            }
            catch (OperationCanceledException)
            {
                return UINavResult.Canceled;
            }
            finally
            {
                signals.Remove(ticket);
            }
        }

        private async Awaitable<UINavResult> RunSignalAsync(
            UIRoute route, object data, string request, CancellationToken cancellation)
        {
            void Triggered() => UIEventDispatch.Invoke(ActionTriggered, listener => listener(route),
                exception => LogNotificationException($"{request} {nameof(ActionTriggered)}", exception));

            UINavResult result;
            switch (route.Kind)
            {
                case UIRouteKind.Emit:
                    if (!CanNavigate())
                        return UINavResult.Canceled;
                    if (data != null)
                        Debug.LogWarning(Diagnostic(request, "Emit 的訊號不帶資料，傳入的 data 沒有被使用。"), this);
                    Triggered();
                    if (!CanNavigate() || cancellation.IsCancellationRequested)
                        return UINavResult.Canceled;
                    result = Emit(TopPopup ? TopPopup : currentPage, route, request);
                    break;
                case UIRouteKind.ShowPage:
                case UIRouteKind.OpenPopup:
                    result = await RunAsync(request, async token =>
                    {
                        NotifyDuringNavigation(request, nameof(ActionTriggered), AnySource, Triggered);
                        token.ThrowIfCancellationRequested();
                        return route.Kind == UIRouteKind.ShowPage
                            ? await ShowPageCoreAsync(route.Target, data, route.History, token, request)
                            : await OpenPopupCoreAsync(route.Target, data, token, request, null, route.History);
                    }, cancellation);
                    // 剛好被別的導航搶先：回去排隊，等它結束再試。
                    if (result == UINavResult.Busy)
                        return result;
                    break;
                default:
                    return NotFound(request, $"訊號路由的種類只能是 ShowPage、OpenPopup 或 Emit（目前是 {route.Kind}）。");
            }

            UIEventDispatch.Invoke(ActionCompleted, listener => listener(route, result),
                exception => LogNotificationException($"{request} {nameof(ActionCompleted)}", exception));
            return result;
        }

        // source 是收到動作的畫面。訊號送出時可能還沒有任何畫面，這時只觸發 ActionEmitted。
        private UINavResult Emit(UIView source, UIRoute route, string request)
        {
            try
            {
                var handlersSucceeded = !source || source.Emit(route.Action,
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
                    return await OpenPopupCoreAsync(route.Target, null, cancellation, request, null, route.History);
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
            // 不可返回的 Popup 開著的期間，切頁一律清空歷史：底下的頁面不可以變成能返回的地方。
            var sealedByPopup = popups.Exists(popup => unreturnablePopups.Contains(popup));
            await ChangePageAsync(next, data, cancellation, request, () =>
            {
                // 往前切頁視為重新進入，從預設選取元件開始。返回時才恢復上次的選取。
                next.lastSelected = null;
                if (historyMode == UIHistoryMode.Clear || sealedByPopup)
                    history.Clear();
                else if (historyMode == UIHistoryMode.Push && previousId != null)
                    history.Add(previousId);
            });
            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> BackCoreAsync(CancellationToken cancellation, string request)
        {
            if (popups.Count > 0)
            {
                // 不可返回的 Popup 只能用 ClosePopup 路由、View.Close() 或點遮罩關閉。
                if (unreturnablePopups.Contains(TopPopup))
                    return UINavResult.Unavailable;
                return await ClosePopupCoreAsync(cancellation, request, null);
            }
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
            string id, object data, CancellationToken cancellation, string request, Action<UIView> onOpened = null,
            UIHistoryMode historyMode = UIHistoryMode.Push)
        {
            if (historyMode != UIHistoryMode.Push && historyMode != UIHistoryMode.Clear)
                throw new InvalidOperationException($"OpenPopup 的 history 只能是 Push 或 Clear（目前是 {historyMode}）。");
            if (string.IsNullOrWhiteSpace(id) || !entriesById.TryGetValue(id, out var entry) ||
                entry.Kind != UIEntryKind.Popup)
                return NotFound(request, $"找不到 Popup '{id}'。");
            if (!entry.Prefab)
                return NotFound(request, $"Popup '{id}' 缺少 Prefab。");
            if (popups.Exists(popup => popup && popup.Id == id))
                return Unavailable(request, $"Popup '{id}' 已經開啟。");

            // 設成重用的 Popup 關閉後會保留。下次開啟用同一個實例，並移到 Popup 層的最上面。
            var popup = retainedPopups.Remove(id, out var retained) && retained
                ? retained
                : InstantiateView(entry, popupLayer);
            popup.transform.SetAsLastSibling();
            popup.lastSelected = null;
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
                    ReleasePopup(popup);
                }
                throw;
            }

            popups.Add(popup);
            if (historyMode == UIHistoryMode.Clear)
            {
                // 不可返回的 Popup：開啟時清空歷史。這個狀態只屬於這一次開啟。
                history.Clear();
                unreturnablePopups.Add(popup);
            }
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
            unreturnablePopups.Remove(popup);
            if (popupResults.Remove(popup, out var pending))
            {
                closedPopupResults ??= new List<(AwaitableCompletionSource<object>, object)>();
                closedPopupResults.Add((pending, result));
            }
            popup.FinishHiding(request);
            if (popup)
                ReleasePopup(popup);
        }

        private void ReleasePopup(UIView popup)
        {
            if (popup.ReusesInstance && !string.IsNullOrEmpty(popup.Id))
                retainedPopups[popup.Id] = popup;
            else
                Destroy(popup.gameObject);
        }

        internal void OnViewDestroyed(UIView view)
        {
            if (!string.IsNullOrEmpty(view.Id) && retainedPopups.TryGetValue(view.Id, out var retained) &&
                ReferenceEquals(retained, view))
                retainedPopups.Remove(view.Id);
            unreturnablePopups.Remove(view);
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
            if (!view.overlay)
                return;
            // 遮罩要能接到點擊，才有辦法做「點遮罩關閉」。是否關閉由 View 的設定決定。
            if (!view.overlay.TryGetComponent<UIOverlayClick>(out var click))
                click = view.overlay.gameObject.AddComponent<UIOverlayClick>();
            click.view = view;
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
