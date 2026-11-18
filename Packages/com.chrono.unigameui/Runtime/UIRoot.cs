using System;
using System.Collections.Generic;
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
        private UIView currentPage;
        private CancellationTokenSource lifetime;
        private bool ready;
        private bool busy;

        public UIAuthoringMode Mode => mode;
        public Vector2 ReferenceResolution => referenceResolution;
        public int ReviewColumns => reviewColumns;
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
            if (!reviewLayer)
            {
                Debug.LogError("Create a UniGameUI workspace with the Editor authoring tool.", this);
                return;
            }

            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transform.localScale = Vector3.one;
            GetComponent<CanvasScaler>().referenceResolution = referenceResolution;
            reviewLayer.gameObject.SetActive(false);
            pageLayer = CreateLayer("Pages");
            popupLayer = CreateLayer("Popups");

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !entriesById.TryAdd(entry.Id, entry))
                {
                    Debug.LogError("UniGameUI contains a missing or duplicate view ID.", this);
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
                if (route == null || !routesByAction.TryAdd((route.Source, route.Action), route))
                    Debug.LogError("UniGameUI contains a missing or duplicate route.", this);
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
            return RunAsync(token => ShowPageCoreAsync(id, data, token), cancellation);
        }

        public Awaitable<UINavResult> OpenPopupAsync(
            string id, object data = null, CancellationToken cancellation = default)
        {
            return RunAsync(token => OpenPopupCoreAsync(id, data, token), cancellation);
        }

        public Awaitable<UINavResult> ClosePopupAsync(CancellationToken cancellation = default)
        {
            return RunAsync(ClosePopupCoreAsync, cancellation);
        }

        public Awaitable<UINavResult> BackAsync(CancellationToken cancellation = default)
        {
            return RunAsync(BackCoreAsync, cancellation);
        }

        public async Awaitable<UINavResult> TriggerAsync(
            string source, string action, CancellationToken cancellation = default)
        {
            await Awaitable.MainThreadAsync();
            if (busy)
                return UINavResult.Busy;
            if (!CanNavigate())
                return UINavResult.Unavailable;
            if (cancellation.IsCancellationRequested)
                return UINavResult.Canceled;

            var active = popups.Count > 0 ? TopPopup : currentPage;
            if (!active || active.Id != source || !routesByAction.TryGetValue((source, action), out var route))
                return UINavResult.Unavailable;
            if (route.Kind != UIRouteKind.Emit)
                return await RunAsync(token => FollowRouteAsync(route, token), cancellation);

            try
            {
                actionEmitted.Invoke(source, action);
                return UINavResult.Completed;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return UINavResult.Unavailable;
            }
        }

        private async Awaitable<UINavResult> RunAsync(
            Func<CancellationToken, Awaitable<UINavResult>> operation, CancellationToken cancellation)
        {
            await Awaitable.MainThreadAsync();
            if (busy)
                return UINavResult.Busy;
            if (!CanNavigate())
                return UINavResult.Unavailable;

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
                Debug.LogException(exception, this);
                return UINavResult.Unavailable;
            }
            finally
            {
                busy = false;
                RefreshInput();
            }
        }

        private bool CanNavigate()
        {
            return this && ready && isActiveAndEnabled && lifetime != null;
        }

        private async Awaitable<UINavResult> FollowRouteAsync(UIRoute route, CancellationToken cancellation)
        {
            switch (route.Kind)
            {
                case UIRouteKind.ShowPage:
                    return await ShowPageCoreAsync(route.Target, null, cancellation);
                case UIRouteKind.OpenPopup:
                    return await OpenPopupCoreAsync(route.Target, null, cancellation);
                case UIRouteKind.Back:
                    return await BackCoreAsync(cancellation);
                case UIRouteKind.ClosePopup:
                    return await ClosePopupCoreAsync(cancellation);
                default:
                    return UINavResult.Unavailable;
            }
        }

        private async Awaitable<UINavResult> ShowPageCoreAsync(
            string id, object data, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(id) || CurrentPageId == id || !GetPage(id, out var next))
                return UINavResult.Unavailable;

            var previousId = CurrentPageId;
            while (popups.Count > 0)
                await ClosePopupCoreAsync(cancellation);

            await ChangePageAsync(next, data, cancellation);
            if (previousId != null)
                history.Add(previousId);
            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> BackCoreAsync(CancellationToken cancellation)
        {
            if (popups.Count > 0)
                return await ClosePopupCoreAsync(cancellation);
            if (history.Count == 0 || !GetPage(history[^1], out var previous))
                return UINavResult.Unavailable;

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
                if (previous)
                {
                    await previous.HideAsync(cancellation);
                    previous.gameObject.SetActive(false);
                }
                await next.ShowAsync(data, cancellation);
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
                throw;
            }

            currentPage = next;
            if (previous)
                previous.FinishHiding();
            next.FinishShowing();
        }

        private async Awaitable<UINavResult> OpenPopupCoreAsync(
            string id, object data, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(id) || !entriesById.TryGetValue(id, out var entry) ||
                entry.Kind != UIEntryKind.Popup || !entry.Prefab ||
                popups.Exists(popup => popup && popup.Id == id))
                return UINavResult.Unavailable;

            var popup = InstantiateView(entry, popupLayer);
            try
            {
                await popup.ShowAsync(data, cancellation);
                cancellation.ThrowIfCancellationRequested();
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
            popup.FinishShowing();
            return UINavResult.Completed;
        }

        private async Awaitable<UINavResult> ClosePopupCoreAsync(CancellationToken cancellation)
        {
            if (popups.Count == 0)
                return UINavResult.Unavailable;

            var popup = TopPopup;
            if (popup)
            {
                await popup.HideAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
            }

            popups.RemoveAt(popups.Count - 1);
            if (popup)
            {
                popup.FinishHiding();
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
            var view = Instantiate(entry.Prefab, reviewLayer, false);
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
