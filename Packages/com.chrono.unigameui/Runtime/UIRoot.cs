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
    public sealed class UIRoot : MonoBehaviour
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
        private RectTransform pageLayer;
        private RectTransform popupLayer;
        private RectTransform stagingLayer;
        private UIView currentPage;
        private CancellationTokenSource lifetime;
        private bool ready;
        private bool busy;

        public UIAuthoringMode Mode => mode;
        public Vector2 ReferenceResolution => referenceResolution;
        public int ReviewColumns => reviewColumns;
        public UIPageTransitionMode PageTransitionMode => pageTransitionMode;
        public IReadOnlyList<UIEntry> Entries => entries;
        public IReadOnlyList<UIRoute> Routes => routes;
        public string StartPageId => startPageId;
        public string CurrentPageId => currentPage ? currentPage.Id : null;
        public string CurrentPopupId => TopPopup ? TopPopup.Id : null;
        public bool IsTransitioning => busy;
        public UIActionEvent ActionEmitted => actionEmitted;

        private UIView TopPopup => popups.Count == 0 ? null : popups[^1];

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
            var request = $"ShowPage '{id}'";
            return RunAsync(request, token => ShowPageCoreAsync(id, data, token, request), cancellation);
        }

        public Awaitable<UINavResult> OpenPopupAsync(
            string id, object data = null, CancellationToken cancellation = default)
        {
            var request = $"OpenPopup '{id}'";
            return RunAsync(request, token => OpenPopupCoreAsync(id, data, token, request), cancellation);
        }

        public async Awaitable<UINavResult> ClosePopupAsync(CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            var request = $"ClosePopup '{CurrentPopupId ?? "(無)"}'";
            return await RunAsync(request, token => ClosePopupCoreAsync(token, request), cancellation);
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
                return UINavResult.Busy;
            if (!CanNavigate())
                return Unavailable(request, "工作區停用或尚未就緒。");
            if (cancellation.IsCancellationRequested)
                return UINavResult.Canceled;

            if (!routesByAction.TryGetValue((source, action), out var route))
                return NotFound(request, "找不到路由。");
            var active = popups.Count > 0 ? TopPopup : currentPage;
            if (!active || active.Id != source)
                return Unavailable(request, "來源不是目前最上層的畫面。");
            if (route.Kind != UIRouteKind.Emit)
                return await RunAsync(request, token => FollowRouteAsync(route, token, request), cancellation);

            try
            {
                actionEmitted.Invoke(source, action);
                return UINavResult.Completed;
            }
            catch (Exception exception)
            {
                return Failed(request, exception);
            }
        }

        private async Awaitable<UINavResult> RunAsync(
            string request, Func<CancellationToken, Awaitable<UINavResult>> operation, CancellationToken cancellation)
        {
            await Awaitable.MainThreadAsync();
            if (busy)
                return UINavResult.Busy;
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
            catch (Exception exception)
            {
                return Failed(request, exception);
            }
            finally
            {
                busy = false;
                RefreshInput();
            }
        }

        private UINavResult NotFound(string request, string detail)
        {
            Debug.LogError(Diagnostic(request, detail), this);
            return UINavResult.NotFound;
        }

        private UINavResult Unavailable(string request, string detail)
        {
            Debug.LogWarning(Diagnostic(request, detail), this);
            return UINavResult.Unavailable;
        }

        private UINavResult Failed(string request, Exception exception)
        {
            var message = Diagnostic(request, $"執行失敗。{exception.Message}");
            Debug.LogError(message, this);
            Debug.LogException(new InvalidOperationException($"{message}\n{exception}"), this);
            return UINavResult.Failed;
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
                    return await ShowPageCoreAsync(route.Target, null, cancellation, request);
                case UIRouteKind.OpenPopup:
                    return await OpenPopupCoreAsync(route.Target, null, cancellation, request);
                case UIRouteKind.Back:
                    return await BackCoreAsync(cancellation, request);
                case UIRouteKind.ClosePopup:
                    return await ClosePopupCoreAsync(cancellation, request);
                default:
                    throw new InvalidOperationException($"無法識別路由種類 '{route.Kind}'。");
            }
        }

        private async Awaitable<UINavResult> ShowPageCoreAsync(
            string id, object data, CancellationToken cancellation, string request)
        {
            if (string.IsNullOrWhiteSpace(id) || !GetPage(id, out var next))
                return NotFound(request, $"找不到頁面 '{id}'。");
            if (CurrentPageId == id)
                return Unavailable(request, $"目前已在頁面 '{id}'。");

            var previousId = CurrentPageId;
            while (popups.Count > 0)
                await ClosePopupCoreAsync(cancellation, request);

            await ChangePageAsync(next, data, cancellation);
            if (previousId != null)
                history.Add(previousId);
            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> BackCoreAsync(CancellationToken cancellation, string request)
        {
            if (popups.Count > 0)
                return await ClosePopupCoreAsync(cancellation, request);
            if (history.Count == 0)
                return UINavResult.Unavailable;
            if (!GetPage(history[^1], out var previous))
                return NotFound(request, $"找不到上一頁 '{history[^1]}'。");

            await ChangePageAsync(previous, previous.Data, cancellation);
            history.RemoveAt(history.Count - 1);
            return UINavResult.Completed;
        }

        private async Awaitable ChangePageAsync(UIView next, object data, CancellationToken cancellation)
        {
            var previous = currentPage;
            var previousData = next.Data;
            try
            {
                if (previous && pageTransitionMode == UIPageTransitionMode.Concurrent)
                {
                    await ChangePageConcurrentlyAsync(previous, next, data, cancellation);
                }
                else
                {
                    if (previous)
                        await HidePreviousPageAsync(previous, cancellation);
                    await next.ShowAsync(data, cancellation);
                }
                cancellation.ThrowIfCancellationRequested();
                currentPage = next;
                if (previous)
                    previous.FinishHiding();
                next.FinishShowing();
            }
            catch
            {
                currentPage = previous;
                if (next)
                {
                    next.gameObject.SetActive(false);
                    next.Data = previousData;
                }
                if (previous)
                    previous.gameObject.SetActive(true);
                throw;
            }
        }

        private static async Awaitable HidePreviousPageAsync(UIView view, CancellationToken cancellation)
        {
            await view.HideAsync(cancellation);
            cancellation.ThrowIfCancellationRequested();
            view.gameObject.SetActive(false);
        }

        private static async Awaitable ChangePageConcurrentlyAsync(
            UIView previous, UIView next, object data, CancellationToken cancellation)
        {
            next.transform.SetAsLastSibling();
            using var pair = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var completion = new AwaitableCompletionSource();
            ExceptionDispatchInfo failure = null;
            var remaining = 2;

            Observe(HidePreviousPageAsync(previous, pair.Token));
            Observe(next.ShowAsync(data, pair.Token));
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
            string id, object data, CancellationToken cancellation, string request)
        {
            if (string.IsNullOrWhiteSpace(id) || !entriesById.TryGetValue(id, out var entry) ||
                entry.Kind != UIEntryKind.Popup)
                return NotFound(request, $"找不到 Popup '{id}'。");
            if (!entry.Prefab)
                return NotFound(request, $"Popup '{id}' 缺少 Prefab。");
            if (popups.Exists(popup => popup && popup.Id == id))
                return Unavailable(request, $"Popup '{id}' 已經開啟。");

            var popup = InstantiateView(entry, popupLayer);
            try
            {
                await popup.ShowAsync(data, cancellation);
                cancellation.ThrowIfCancellationRequested();
                popups.Add(popup);
                popup.FinishShowing();
            }
            catch
            {
                popups.Remove(popup);
                if (popup)
                {
                    popup.gameObject.SetActive(false);
                    Destroy(popup.gameObject);
                }
                throw;
            }

            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> ClosePopupCoreAsync(CancellationToken cancellation, string request)
        {
            if (popups.Count == 0)
                return Unavailable(request, "目前沒有 Popup 可關閉。");

            var popup = TopPopup;
            if (popup)
            {
                await popup.HideAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
            }

            popups.RemoveAt(popups.Count - 1);
            if (popup)
            {
                try
                {
                    popup.FinishHiding();
                }
                catch
                {
                    popups.Add(popup);
                    if (popup)
                        popup.gameObject.SetActive(true);
                    throw;
                }
                Destroy(popup.gameObject);
            }
            return UINavResult.Completed;
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

        private static void Prepare(UIView view, RectTransform parent)
        {
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
