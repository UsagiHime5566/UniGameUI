using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor
{
    public static class UIAuthoring
    {
        private const float ReviewScale = 0.01f;
        private const float ReviewSpacing = 96f;
        private const AdditionalCanvasShaderChannels TextShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        public static UIRoot FindWorkspace(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            var roots = scene.GetRootGameObjects()
                .SelectMany(gameObject => gameObject.GetComponentsInChildren<UIRoot>(true)).ToArray();
            if (roots.Length > 1)
                throw new InvalidOperationException($"Scene {scene.name} contains more than one UniGameUI root.");
            return roots.FirstOrDefault();
        }

        public static UIRoot CreateWorkspace(Scene scene, UIAuthoringMode mode = UIAuthoringMode.SingleScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded ||
                EditorSceneManager.IsPreviewScene(scene))
                throw new InvalidOperationException("Open the target Scene in Edit Mode.");

            var existing = FindWorkspace(scene);
            if (existing)
            {
                if (existing.Mode != mode)
                    throw new InvalidOperationException("The workspace already uses a different authoring mode.");
                return existing;
            }

            // 畫面上的元件都是元件集裡 Prefab 的實例，所以專案還沒有元件集時先安裝起始元件。
            if (!UIWidgets.Find())
                UIWidgets.Install();

            var gameObject = new GameObject("UniGameUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UIRoot));
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            var root = gameObject.GetComponent<UIRoot>();
            gameObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.mode = mode;
            root.reviewLayer = CreateRect("UI Review", gameObject.transform);
            UIRoot.Stretch(root.reviewLayer);
            ApplyCanvasSize(root);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create UniGameUI workspace");
            EnsureEventSystem(scene);
            MarkChanged(root);
            return root;
        }

        public static void ConfigureReview(UIRoot root, Vector2 resolution, int columns)
        {
            CheckRoot(root);
            if (!float.IsFinite(resolution.x) || !float.IsFinite(resolution.y) ||
                resolution.x < 1 || resolution.y < 1 || columns < 1)
                throw new ArgumentException("Review resolution and columns must be positive.");

            Undo.RecordObjects(new Object[] { root, root.transform, root.GetComponent<CanvasScaler>() },
                "Configure UI review");
            root.referenceResolution = resolution;
            root.reviewColumns = columns;
            ApplyCanvasSize(root);
            ArrangeReview(root);
        }

        public static UIView CreatePage(UIRoot root, string id, string title, string prefabPath = null)
        {
            return CreateEntry(root, id, title, UIEntryKind.Page, prefabPath);
        }

        public static UIView CreatePopup(UIRoot root, string id, string title, string prefabPath = null)
        {
            return CreateEntry(root, id, title, UIEntryKind.Popup, prefabPath);
        }

        public static UIEntry FindEntry(UIRoot root, string id)
        {
            return root.Entries.FirstOrDefault(entry => entry.Id == id);
        }

        public static void SetStartPage(UIRoot root, string id)
        {
            CheckRoot(root);
            if (!string.IsNullOrEmpty(id) && FindEntry(root, id)?.Kind != UIEntryKind.Page)
                throw new ArgumentException($"Unknown start page '{id}'.");
            Undo.RecordObject(root, "Set initial page");
            root.startPageId = id ?? "";
            MarkChanged(root);
        }

        /// <summary>
        /// 標記畫面由程式開啟：進來的方式不靠路由，例如用程式開啟、自己關閉的確認框，或需要程式給資料才能顯示的畫面。
        /// 標記之後 Validate 不檢查它走不走得到、裡面的按鈕有沒有動作；Popup 也不檢查有沒有出口，頁面照樣檢查。
        /// 走訪不要求它能單靠路由開啟。設定存在工作區的藍圖裡，不在 View Prefab 裡。
        /// </summary>
        public static void SetNavigatedByCode(UIRoot root, string id, bool value)
        {
            CheckRoot(root);
            var entry = FindEntry(root, id) ?? throw new ArgumentException($"找不到畫面「{id}」。");
            if (entry.navigatedByCode == value)
                return;
            Undo.RecordObject(root, "Set UI view navigation");
            entry.navigatedByCode = value;
            MarkChanged(root);
        }

        // 藍圖上有標記，或根節點掛了 UIConfirmPopup：確認框用 ConfirmAsync 開啟、自己關閉，不必另外標記。
        internal static bool IsNavigatedByCode(UIEntry entry)
        {
            if (entry == null)
                return false;
            return entry.NavigatedByCode || IsConfirmPopup(entry.Review) || IsConfirmPopup(entry.Prefab);
        }

        internal static bool IsConfirmPopup(UIView view)
        {
            return view && view.GetComponent<UIConfirmPopup>();
        }

        public static void SetPageTransitionMode(UIRoot root, UIPageTransitionMode mode)
        {
            CheckRoot(root);
            Undo.RecordObject(root, "Set page transition mode");
            root.pageTransitionMode = mode;
            MarkChanged(root);
        }

        public static void Connect(UIRoot root, string source, string action, UIRouteKind kind, string target = null,
            UIHistoryMode history = UIHistoryMode.Push, string result = null)
        {
            ValidateRoute(root, source, action, kind, target, history);
            Undo.RecordObject(root, "Connect UI route");
            var route = root.routes.Find(item => item.Source == source && item.Action == action);
            if (route == null)
            {
                route = new UIRoute { source = source, action = action };
                root.routes.Add(route);
            }
            SetRouteOptions(route, kind, target, history, result);
            MarkChanged(root);
        }

        public static void UpdateRoute(UIRoot root, string source, string originalAction, string action,
            UIRouteKind kind, string target = null, UIHistoryMode history = UIHistoryMode.Push, string result = null)
        {
            var entry = ValidateRoute(root, source, action, kind, target, history);
            var route = root.routes.Find(item => item.Source == source && item.Action == originalAction) ??
                throw new ArgumentException($"Unknown route '{source}/{originalAction}'.");
            if (root.routes.Exists(item => item != route && item.Source == source && item.Action == action))
                throw new ArgumentException($"Route '{source}/{action}' already exists.");

            var changed = new List<Object> { root };
            // 訊號路由沒有來源畫面，也就沒有要跟著改名的按鈕。
            if (action != originalAction && entry != null && entry.Review)
            {
                changed.AddRange(entry.Review.GetComponentsInChildren<UIButtonAction>(true)
                    .Where(binding => binding.Action == originalAction &&
                        binding.GetComponentInParent<UIView>(true) == entry.Review));
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Update UI route");
            Undo.RecordObjects(changed.ToArray(), "Update UI route");
            route.action = action;
            SetRouteOptions(route, kind, target, history, result);
            foreach (var binding in changed.OfType<UIButtonAction>())
            {
                binding.action = action;
                RecordOverride(binding);
            }
            MarkChanged(root);
            Undo.CollapseUndoOperations(group);
        }

        private static void SetRouteOptions(UIRoute route, UIRouteKind kind, string target,
            UIHistoryMode history, string result)
        {
            route.kind = kind;
            route.target = UIRoute.TargetsEntry(kind) ? target : "";
            route.history = kind == UIRouteKind.ShowPage || kind == UIRouteKind.OpenPopup
                ? history : UIHistoryMode.Push;
            route.result = kind == UIRouteKind.ClosePopup ? result ?? "" : "";
        }

        /// <summary>「任何畫面」的訊號路由可以用的種類。</summary>
        internal static bool IsSignalKind(UIRouteKind kind)
        {
            return kind == UIRouteKind.ShowPage || kind == UIRouteKind.OpenPopup || kind == UIRouteKind.Emit;
        }

        /// <summary>
        /// 這個種類的路由能不能用這個 history。ShowPage 三種都可以；OpenPopup 只有 Push（可以返回）
        /// 與 Clear（不可返回）；其他種類沒有 history。
        /// </summary>
        internal static bool AcceptsHistory(UIRouteKind kind, UIHistoryMode history)
        {
            switch (kind)
            {
                case UIRouteKind.ShowPage:
                    return Enum.IsDefined(typeof(UIHistoryMode), history);
                case UIRouteKind.OpenPopup:
                    return history == UIHistoryMode.Push || history == UIHistoryMode.Clear;
                default:
                    return true;
            }
        }

        // 回傳來源畫面。來源是 UIRoot.AnySource（訊號路由）時沒有來源畫面，回傳 null。
        private static UIEntry ValidateRoute(UIRoot root, string source, string action,
            UIRouteKind kind, string target, UIHistoryMode history)
        {
            CheckRoot(root);
            var isSignal = source == UIRoot.AnySource;
            var entry = isSignal
                ? null
                : FindEntry(root, source) ?? throw new ArgumentException($"Unknown source '{source}'.");
            CheckId(action);
            if (isSignal && !IsSignalKind(kind))
            {
                throw new ArgumentException(
                    $"「任何畫面」的訊號路由只能是 ShowPage、OpenPopup 或 Emit，不能是 {kind}。", nameof(kind));
            }
            if (kind == UIRouteKind.ShowPage && !Enum.IsDefined(typeof(UIHistoryMode), history))
                throw new ArgumentException($"不支援頁面歷史模式「{history}」。", nameof(history));
            if (kind == UIRouteKind.OpenPopup && !AcceptsHistory(kind, history))
            {
                throw new ArgumentException(
                    $"OpenPopup 的 history 只能是 Push（可以返回）或 Clear（不可返回），不能是 {history}。",
                    nameof(history));
            }
            if (UIRoute.TargetsEntry(kind))
            {
                var expected = kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup;
                if (FindEntry(root, target)?.Kind != expected)
                    throw new ArgumentException($"'{target}' must be a registered {expected}.");
            }
            return entry;
        }

        /// <summary>
        /// 設定藍圖上一條線的中繼點（藍圖畫布的座標，依線經過的順序），傳入空的清除。只影響藍圖怎麼畫。
        /// 可以 Undo，並把 Scene 標成未儲存。路由不存在，或它沒有線（不是 ShowPage 或 OpenPopup）時丟出例外。
        /// </summary>
        public static void SetWaypoints(UIRoot root, string source, string action, IEnumerable<Vector2> points)
        {
            CheckRoot(root);
            var route = root.routes.Find(item => item.Source == source && item.Action == action) ??
                throw new ArgumentException($"找不到路由「{source}/{action}」。");
            if (!UIRoute.TargetsEntry(route.Kind))
                throw new ArgumentException($"路由「{source}/{action}」是 {route.Kind}，藍圖上沒有線，不能有中繼點。");
            SetWaypoints(root, route, points ?? Array.Empty<Vector2>(), "Set blueprint reroute points");
        }

        // 藍圖視窗用：Undo 的名稱依操作而不同，沒有線的路由也可以清掉留下來的中繼點。
        internal static void SetWaypoints(UIRoot root, UIRoute route, IEnumerable<Vector2> points, string undoName)
        {
            CheckRoot(root);
            if (route == null || !root.routes.Contains(route))
                throw new ArgumentException("找不到這條路由。");
            var next = points.ToList();
            Undo.RecordObject(root, undoName);
            route.WaypointList.Clear();
            route.WaypointList.AddRange(next);
            MarkChanged(root);
        }

        public static void Disconnect(UIRoot root, string source, string action)
        {
            CheckRoot(root);
            Undo.RecordObject(root, "Disconnect UI route");
            root.routes.RemoveAll(route => route.Source == source && route.Action == action);
            MarkChanged(root);
        }

        public static UIButtonAction BindButton(Button button, string action)
        {
            CheckId(action);
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !button || !button.GetComponentInParent<UIView>(true))
                throw new InvalidOperationException("Bind a Button inside a UniGameUI view in Edit Mode.");
            if (!button.TryGetComponent<UIButtonAction>(out var binding))
                binding = Undo.AddComponent<UIButtonAction>(button.gameObject);
            Undo.RecordObject(binding, "Bind UI button");
            binding.action = action;
            RecordOverride(binding);
            return binding;
        }

        public static void SetTransition(UIView view, bool entering, UITransitionPreset preset,
            float duration = 0.25f, UITransitionDirection direction = UITransitionDirection.Right,
            UITransitionEase ease = UITransitionEase.OutQuad, float distance = 120f, float angle = 12f)
        {
            SetTransition(view, entering, preset, duration, direction, ease, distance, angle, null);
        }

        public static void SetTransition(UIView view, bool entering, UITransitionPreset preset,
            float duration, UITransitionDirection direction, UITransitionEase ease,
            float distance, float angle, float? zoomScale)
        {
            if (!view || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Select a view in Edit Mode.");
            if (!float.IsFinite(duration) || !float.IsFinite(distance) || !float.IsFinite(angle) ||
                duration < 0 || distance < 0)
                throw new ArgumentException("Animation duration and distance must be non-negative and finite.");
            if (zoomScale.HasValue && (!float.IsFinite(zoomScale.Value) || zoomScale.Value <= 0))
                throw new ArgumentException("Animation zoom scale must be positive and finite.");
            Undo.RecordObject(view, "Set UI transition");
            var transition = entering ? view.enter : view.exit;
            transition.preset = preset;
            transition.duration = duration;
            transition.direction = direction;
            transition.ease = ease;
            transition.distance = distance;
            transition.angle = angle;
            if (zoomScale.HasValue)
                transition.zoomScale = zoomScale.Value;
            RecordOverride(view);
        }

        // 自動排列藍圖時節點之間的間距（畫布單位）。欄距留給線上的動作標籤。
        internal const float BlueprintColumnGap = 150f;
        internal const float BlueprintRowGap = 56f;

        /// <summary>
        /// 自動排列藍圖：從起始頁出發，沿 ShowPage 與 OpenPopup 路由（不含訊號路由）算出每個畫面的步數，
        /// 步數相同的放在同一欄，由左到右排列；同一欄裡頁面在上、Popup 在下，主流程的頁面排成一直線；
        /// 只能由訊號或程式進入的畫面放在最右邊另一欄；「任何畫面」節點放在左下角。
        /// 排完之後清除所有路由的中繼點。可以 Undo，並把 Scene 標成未儲存。同一份藍圖排兩次結果相同。
        /// </summary>
        public static void ArrangeBlueprint(UIRoot root)
        {
            CheckRoot(root);
            var columns = BlueprintColumns(root);
            var step = new Vector2(UIBlueprintWindow.NodeWidth + BlueprintColumnGap,
                UIBlueprintWindow.NodeHeight + BlueprintRowGap);
            Undo.RecordObject(root, "Arrange blueprint");
            for (var column = 0; column < columns.Count; column++)
            {
                for (var row = 0; row < columns[column].Count; row++)
                    columns[column][row].graphPosition = new Vector2(column * step.x, row * step.y);
            }
            // 「任何畫面」在最高那一欄的下面，靠左。
            var rows = columns.Count == 0 ? 0 : columns.Max(column => column.Count);
            root.anySourceGraphPosition = new Vector2(0, rows * step.y + BlueprintRowGap);
            // 舊的中繼點對新的位置已經沒有意義。
            foreach (var route in root.routes)
                route.WaypointList.Clear();
            MarkChanged(root);
        }

        // 自動排列的欄：第 n 欄是從起始頁走 n 步到得了的畫面，最後一欄是走不到的（只能由訊號或程式進入）。
        internal static List<List<UIEntry>> BlueprintColumns(UIRoot root)
        {
            var entries = root.entries.Where(entry => entry != null && !string.IsNullOrEmpty(entry.Id))
                .GroupBy(entry => entry.Id).Select(group => group.First()).ToList();
            var byId = entries.ToDictionary(entry => entry.Id);
            // 一般路由：畫面之間的流程。訊號路由不算，它們不屬於任何畫面。
            var next = entries.ToDictionary(entry => entry.Id, entry => root.routes
                .Where(route => route != null && route.Source == entry.Id && UIRoute.TargetsEntry(route.Kind) &&
                    !string.IsNullOrEmpty(route.Target) && byId.ContainsKey(route.Target) && route.Target != entry.Id)
                .Select(route => route.Target).Distinct().ToList());

            // 出發點：起始頁。沒有起始頁時，用沒有一般路由通進來的頁面（依登記的順序）。
            var seeds = new List<string>();
            if (!string.IsNullOrEmpty(root.startPageId) && byId.TryGetValue(root.startPageId, out var start) &&
                start.Kind == UIEntryKind.Page)
            {
                seeds.Add(start.Id);
            }
            else
            {
                var targeted = new HashSet<string>(next.Values.SelectMany(targets => targets));
                seeds.AddRange(entries.Where(entry => entry.Kind == UIEntryKind.Page && !targeted.Contains(entry.Id))
                    .Select(entry => entry.Id));
            }

            var depth = new Dictionary<string, int>();
            var order = new List<string>();
            var pending = new Queue<string>();
            foreach (var seed in seeds)
            {
                depth[seed] = 0;
                order.Add(seed);
                pending.Enqueue(seed);
            }
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var target in next[current])
                {
                    if (depth.ContainsKey(target))
                        continue;
                    depth[target] = depth[current] + 1;
                    order.Add(target);
                    pending.Enqueue(target);
                }
            }

            var columns = new List<List<UIEntry>>();
            foreach (var id in order)
            {
                while (columns.Count <= depth[id])
                    columns.Add(new List<UIEntry>());
                columns[depth[id]].Add(byId[id]);
            }
            // 同一欄裡頁面在上、Popup 在下：Popup 不會佔走主流程的那一列。
            // 頁面之間、Popup 之間依來源在前一欄的位置排，線比較少交錯；一樣時維持被發現的順序。
            for (var column = 0; column < columns.Count; column++)
            {
                var previous = column == 0
                    ? new Dictionary<string, int>()
                    : columns[column - 1].Select((entry, row) => (entry.Id, row))
                        .ToDictionary(pair => pair.Id, pair => pair.row);
                var discovered = columns[column].Select((entry, index) => (entry, index)).ToList();
                columns[column] = discovered.OrderBy(pair => pair.entry.Kind == UIEntryKind.Popup ? 1 : 0)
                    .ThenBy(pair =>
                    {
                        var parents = previous.Where(parent => next[parent.Key].Contains(pair.entry.Id))
                            .Select(parent => (float)parent.Value).ToList();
                        return parents.Count == 0 ? float.MaxValue : parents.Average();
                    })
                    .ThenBy(pair => pair.index).Select(pair => pair.entry).ToList();
            }
            var unreached = entries.Where(entry => !depth.ContainsKey(entry.Id))
                .OrderBy(entry => entry.Kind == UIEntryKind.Popup ? 1 : 0).ToList();
            if (unreached.Count > 0)
                columns.Add(unreached);
            return columns;
        }

        public static void ArrangeReview(UIRoot root)
        {
            CheckRoot(root);
            for (var index = 0; index < root.Entries.Count; index++)
            {
                var view = root.Entries[index].Review;
                if (!view || view.transform.parent is not RectTransform frame)
                    continue;
                Undo.RecordObject(frame, "Arrange UI review");
                PlaceFrame(root, frame, index);
            }
            MarkChanged(root);
        }

        public static void PositionNode(UIRoot root, string id, Vector2 position)
        {
            CheckRoot(root);
            if (id == UIRoot.AnySource)
            {
                // 「任何畫面」節點不是畫面，位置存在工作區上。
                Undo.RecordObject(root, "Move blueprint node");
                root.anySourceGraphPosition = position;
                MarkChanged(root);
                return;
            }
            var entry = FindEntry(root, id) ?? throw new ArgumentException($"Unknown view '{id}'.");
            Undo.RecordObject(root, "Move blueprint node");
            entry.graphPosition = position;
            MarkChanged(root);
        }

        public static void FocusReview(UIRoot root, string id = null)
        {
            CheckRoot(root);
            var sceneView = SceneView.lastActiveSceneView;
            if (!sceneView)
                sceneView = EditorWindow.GetWindow<SceneView>();
            sceneView.in2DMode = true;
            if (!string.IsNullOrEmpty(id))
            {
                var entry = FindEntry(root, id);
                if (entry?.Review)
                {
                    Selection.activeGameObject = entry.Review.gameObject;
                    sceneView.Frame(RectBounds((RectTransform)entry.Review.transform), false);
                    return;
                }
            }

            Selection.activeGameObject = root.gameObject;
            var bounds = RectBounds((RectTransform)root.transform);
            foreach (var entry in root.Entries)
            {
                if (entry.Review)
                    bounds.Encapsulate(RectBounds((RectTransform)entry.Review.transform));
            }
            sceneView.Frame(bounds, false);
        }

        public static void PreviewPopupOnPage(UIRoot root, string popupId, string pageId)
        {
            CheckRoot(root);
            var popup = FindEntry(root, popupId);
            var page = FindEntry(root, pageId);
            if (popup?.Kind != UIEntryKind.Popup || page?.Kind != UIEntryKind.Page ||
                !popup.Review || !page.Review)
                throw new ArgumentException("Select a registered Popup and page.");
            var popupFrame = (RectTransform)popup.Review.transform.parent;
            var pageFrame = (RectTransform)page.Review.transform.parent;
            Undo.RegisterCompleteObjectUndo(root.reviewLayer, "Preview Popup on page");
            Undo.RecordObject(popupFrame, "Preview Popup on page");
            popupFrame.anchoredPosition = pageFrame.anchoredPosition;
            popupFrame.SetAsLastSibling();
            MarkChanged(root);
            FocusReview(root, popupId);
        }

        public static bool HasReviewOverrides(UIEntry entry)
        {
            if (entry == null || !entry.Prefab || !entry.Review)
                return false;

            var instance = entry.Review.gameObject;
            if (!PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false))
                return false;

            var modifications = PrefabUtility.GetPropertyModifications(instance);
            return (modifications != null && modifications.Any(modification =>
                       !PrefabUtility.IsDefaultOverride(modification) && !IsDerivedByTmp(modification))) ||
                   PrefabUtility.GetAddedGameObjects(instance).Count > 0 ||
                   PrefabUtility.GetRemovedGameObjects(instance).Count > 0 ||
                   PrefabUtility.GetAddedComponents(instance).Count > 0 ||
                   PrefabUtility.GetRemovedComponents(instance).Count > 0;
        }

        // TMP 自己算出來的欄位：樣式的雜湊值，以及產生網格時從 m_fontColor 複製過去的 m_fontColor32。
        // 它們會被記成覆寫，但不是使用者或計畫做的修改。真的改了文字顏色時，m_fontColor 也會是覆寫。
        private static bool IsDerivedByTmp(PropertyModification modification)
        {
            return modification.target is TMP_Text &&
                (modification.propertyPath == "m_TextStyleHashCode" ||
                 modification.propertyPath.StartsWith("m_fontColor32", StringComparison.Ordinal));
        }

        public static void ApplyReviewOverrides(UIRoot root, string id)
        {
            CheckRoot(root);
            ApplyReviewOverrides(FindEntry(root, id));
        }

        internal static bool NeedsViewCanvas(UIView view)
        {
            return view && (!view.TryGetComponent<Canvas>(out _) || !view.TryGetComponent<GraphicRaycaster>(out _));
        }

        internal static void RepairViewCanvases(UIRoot root)
        {
            CheckRoot(root);
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("修正子 Canvas");
            try
            {
                foreach (var entry in root.Entries)
                {
                    if (entry == null || !entry.Review ||
                        (!NeedsViewCanvas(entry.Review) && !NeedsViewCanvas(entry.Prefab)))
                        continue;

                    var prefab = entry.Prefab;
                    var path = prefab ? AssetDatabase.GetAssetPath(prefab) : null;
                    if (prefab && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(entry.Review.gameObject) != path)
                        throw new InvalidOperationException($"「{entry.Id}」的審核畫面與 Prefab 本體未連結。");

                    EnsureViewComponent<Canvas>(entry.Review, prefab, path, canvas =>
                    {
                        Undo.RecordObject(canvas, "設定子 Canvas");
                        ConfigureViewCanvas(canvas);
                    });
                    EnsureViewComponent<GraphicRaycaster>(entry.Review, prefab, path);
                }
                MarkChanged(root);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void EnsureViewComponent<T>(
            UIView view, UIView prefab, string path, Action<T> configure = null) where T : Component
        {
            var source = prefab ? prefab.GetComponent<T>() : null;
            if (!view.TryGetComponent<T>(out var component))
            {
                if (source)
                {
                    PrefabUtility.RevertRemovedComponent(view.gameObject, source, InteractionMode.UserAction);
                    return;
                }
                component = Undo.AddComponent<T>(view.gameObject);
                configure?.Invoke(component);
            }

            if (prefab && !source && PrefabUtility.IsAddedComponentOverride(component))
                PrefabUtility.ApplyAddedComponent(component, path, InteractionMode.UserAction);
        }

        internal static void ApplyReviewOverrides(UIEntry entry)
        {
            if (HasReviewOverrides(entry))
                PrefabUtility.ApplyPrefabInstance(entry.Review.gameObject, InteractionMode.UserAction);
        }

        internal static void CheckRoot(UIRoot root)
        {
            if (!root || !root.reviewLayer || !root.gameObject.scene.IsValid() ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Select an authored UniGameUI workspace in Edit Mode.");
        }

        internal static void CheckId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                id.Any(character => character is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or
                    >= '0' and <= '9' or '_' or '-')))
                throw new ArgumentException($"'{id}' must use letters, numbers, '_' or '-'.");
        }

        internal static void CheckAssetPath(string path, string extension)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                path.Split('/').Any(part => part.Length == 0 || part == "." || part == ".." ||
                    part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.Contains('\\')))
                throw new ArgumentException($"Use a project-relative Assets/ path ending in {extension}.");
        }

        internal static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var folder = parts[0];
            for (var index = 1; index < parts.Length - 1; index++)
            {
                var next = $"{folder}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(folder, parts[index]);
                folder = next;
            }
        }

        /// <summary>
        /// 記錄 Prefab 實例的覆寫。<paramref name="pinnedProperties"/> 是計畫明確寫出的欄位，
        /// 即使數值剛好和元件 Prefab 相同也要成為覆寫，之後修改元件 Prefab 才不會蓋掉它。
        /// </summary>
        internal static void RecordOverride(Object changed, params string[] pinnedProperties)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
            EditorUtility.SetDirty(changed);
            if (changed is Component component && component.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            if (pinnedProperties.Length > 0)
                PinOverrides(changed, pinnedProperties);
        }

        // Unity 只會把「和 Prefab 不同」的值記成覆寫，已經成立的覆寫則會保留。
        // 所以數值相同時，先暫時改成別的值讓覆寫成立，再改回來。
        private static void PinOverrides(Object target, string[] propertyPaths)
        {
            if (!target || !PrefabUtility.IsPartOfPrefabInstance(target))
                return;

            var serialized = new SerializedObject(target);
            var restore = new List<Action>();
            var lengthened = new List<(string Path, int Size)>();
            foreach (var path in propertyPaths)
            {
                var property = serialized.FindProperty(path);
                if (property == null)
                    continue;
                // 覆寫是逐個分量記錄的。顏色只要有一個分量和 Prefab 不同，整個欄位就算有覆寫，
                // 所以要一路看到最底層：顏色的 r、g、b、a，陣列的長度與每一項。
                foreach (var leaf in Leaves(property))
                {
                    if (leaf.prefabOverride)
                        continue;
                    if (leaf.propertyType == SerializedPropertyType.ArraySize)
                        lengthened.Add((leaf.propertyPath, leaf.intValue));
                    var action = Perturb(leaf);
                    if (action != null)
                        restore.Add(action);
                }
            }
            if (restore.Count == 0)
                return;

            serialized.ApplyModifiedProperties();
            // 倒著還原：陣列的長度最先被改，最後才改回來。
            for (var index = restore.Count - 1; index >= 0; index--)
                restore[index]();
            serialized.ApplyModifiedProperties();
            if (lengthened.Count > 0)
                RemoveOverridesBeyond(target, lengthened);
        }

        // 固定陣列長度時暫時多出來的那一項，改回長度之後它的覆寫還留著，要清掉。
        private static void RemoveOverridesBeyond(Object target, List<(string Path, int Size)> arrays)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
            var source = PrefabUtility.GetCorrespondingObjectFromSource(target);
            var modifications = root ? PrefabUtility.GetPropertyModifications(root) : null;
            if (!source || modifications == null)
                return;

            var kept = modifications.Where(modification => modification.target != source ||
                !arrays.Any(array => IsBeyond(modification.propertyPath, array.Path, array.Size))).ToArray();
            if (kept.Length != modifications.Length)
                PrefabUtility.SetPropertyModifications(root, kept);
        }

        // sizePath 是「….Array.size」，同一個陣列的項目是「….Array.data[索引]」。
        private static bool IsBeyond(string propertyPath, string sizePath, int size)
        {
            var prefix = sizePath.Substring(0, sizePath.Length - "size".Length) + "data[";
            if (!propertyPath.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            var end = propertyPath.IndexOf(']', prefix.Length);
            return end > 0 && int.TryParse(propertyPath.Substring(prefix.Length, end - prefix.Length), out var index) &&
                index >= size;
        }

        private static List<SerializedProperty> Leaves(SerializedProperty property)
        {
            var leaves = new List<SerializedProperty>();
            if (IsLeaf(property))
            {
                leaves.Add(property.Copy());
                return leaves;
            }

            var iterator = property.Copy();
            var end = property.GetEndProperty();
            var enterChildren = true;
            while (iterator.Next(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = !IsLeaf(iterator);
                if (!enterChildren)
                    leaves.Add(iterator.Copy());
            }
            return leaves;
        }

        // 字串與物件引用在序列化裡還有子節點，但覆寫是記在它們自己身上。
        private static bool IsLeaf(SerializedProperty property)
        {
            return property.propertyType is SerializedPropertyType.Integer or SerializedPropertyType.Boolean
                or SerializedPropertyType.Float or SerializedPropertyType.String
                or SerializedPropertyType.ObjectReference or SerializedPropertyType.Enum
                or SerializedPropertyType.ArraySize or SerializedPropertyType.Character
                or SerializedPropertyType.LayerMask || !property.hasChildren;
        }

        private static Action Perturb(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                {
                    var value = property.floatValue;
                    property.floatValue = value + 1f;
                    return () => property.floatValue = value;
                }
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.LayerMask:
                {
                    // Color32 的 rgba 是一個 32 位元的整數，用 longValue 才不會溢位。
                    var value = property.longValue;
                    property.longValue = value == 0 ? 1 : 0;
                    return () => property.longValue = value;
                }
                case SerializedPropertyType.Enum:
                {
                    var value = property.intValue;
                    property.intValue = value == 0 ? 1 : 0;
                    return () => property.intValue = value;
                }
                case SerializedPropertyType.ArraySize:
                {
                    // 只能加長：縮短會丟掉最後一項的內容。
                    var value = property.intValue;
                    property.intValue = value + 1;
                    return () => property.intValue = value;
                }
                case SerializedPropertyType.Boolean:
                {
                    var value = property.boolValue;
                    property.boolValue = !value;
                    return () => property.boolValue = value;
                }
                case SerializedPropertyType.String:
                {
                    var value = property.stringValue;
                    property.stringValue = value + "*";
                    return () => property.stringValue = value;
                }
                case SerializedPropertyType.ObjectReference:
                {
                    var value = property.objectReferenceValue;
                    if (!value)
                        return null;
                    property.objectReferenceValue = null;
                    return () => property.objectReferenceValue = value;
                }
                default:
                    return null;
            }
        }

        private static UIView CreateEntry(UIRoot root, string id, string title, UIEntryKind kind, string prefabPath)
        {
            CheckRoot(root);
            CheckId(id);
            var existing = FindEntry(root, id);
            if (existing != null)
            {
                if (existing.Kind != kind || !existing.Review ||
                    (!string.IsNullOrEmpty(prefabPath) && AssetDatabase.GetAssetPath(existing.Prefab) != prefabPath))
                    throw new InvalidOperationException($"'{id}' is already registered with a different configuration.");
                return existing.Review;
            }

            var usesPrefab = kind == UIEntryKind.Popup || root.Mode == UIAuthoringMode.Prefab;
            UIView prefab = null;
            if (usesPrefab)
            {
                if (string.IsNullOrEmpty(prefabPath))
                    prefabPath = DefaultPrefabPath(kind, id);
                CheckAssetPath(prefabPath, ".prefab");
                var existingAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);
                if (existingAsset != null)
                {
                    prefab = (existingAsset as GameObject)?.GetComponent<UIView>();
                    if (!prefab)
                        throw new InvalidOperationException($"Prefab path is already in use: {prefabPath}");
                    if (prefab.Id != id)
                        throw new InvalidOperationException(ViewIdMismatch(prefabPath, prefab.Id, id));
                }
            }
            else if (!string.IsNullOrEmpty(prefabPath))
            {
                throw new InvalidOperationException("Pages in a Single Scene workspace are Scene objects.");
            }

            title = string.IsNullOrWhiteSpace(title) ? id : title;
            var frame = CreateRect(title, root.reviewLayer);
            Undo.RegisterCreatedObjectUndo(frame.gameObject, "Create UI review frame");
            UIView review;
            if (prefab)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, frame);
                Undo.RegisterCreatedObjectUndo(instance, "Create UI prefab preview");
                review = instance.GetComponent<UIView>();
            }
            else
            {
                review = CreateView(frame, id, title, kind);
                if (usesPrefab)
                {
                    EnsureAssetFolder(prefabPath);
                    var asset = PrefabUtility.SaveAsPrefabAssetAndConnect(review.gameObject, prefabPath,
                        InteractionMode.UserAction);
                    if (!asset)
                        throw new InvalidOperationException($"Could not save prefab '{prefabPath}'.");
                    prefab = asset.GetComponent<UIView>();
                }
            }

            UIRoot.Stretch((RectTransform)review.transform);
            PlaceFrame(root, frame, root.Entries.Count);
            var index = root.Entries.Count;
            Undo.RecordObject(root, "Register UI view");
            root.entries.Add(new UIEntry
            {
                id = id,
                title = title,
                kind = kind,
                review = review,
                prefab = prefab,
                graphPosition = new Vector2(32 + index % 3 * 248, 32 + index / 3 * 130)
            });
            MarkChanged(root);
            return review;
        }

        // View Prefab 的畫面 ID 存在 Prefab 裡。複製出來的 Prefab 仍然是原本的 ID，只能用同一個 ID 登記。
        internal static string ViewIdMismatch(string prefabPath, string prefabId, string requestedId)
        {
            return $"Prefab「{prefabPath}」的畫面 ID 是「{prefabId}」，不能登記成「{requestedId}」。" +
                "View Prefab 的畫面 ID 是固定的，請用相同的 ID 登記，或另外建立一個新的 View。";
        }

        internal static string DefaultPrefabPath(UIEntryKind kind, string id)
        {
            var folder = kind == UIEntryKind.Popup ? "Popups" : "Pages";
            return $"Assets/UniGameUI/{folder}/{id}.prefab";
        }

        private static UIView CreateView(RectTransform frame, string id, string title, UIEntryKind kind)
        {
            var rect = CreateRect(title, frame);
            Undo.RegisterCreatedObjectUndo(rect.gameObject, "Create UI view");
            return BuildView(rect, id, kind);
        }

        /// <summary>
        /// 在指定的物件上組出 View 的固定結構：子 Canvas、遮罩（Popup）、Motion 與 Content。
        /// </summary>
        internal static UIView BuildView(RectTransform rect, string id, UIEntryKind kind)
        {
            var view = rect.gameObject.AddComponent<UIView>();
            ConfigureViewCanvas(rect.gameObject.AddComponent<Canvas>());
            rect.gameObject.AddComponent<GraphicRaycaster>();
            view.id = id;
            view.canvasGroup = rect.GetComponent<CanvasGroup>();
            if (kind == UIEntryKind.Popup)
            {
                var shade = CreateRect("Overlay", rect);
                UIRoot.Stretch(shade);
                shade.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.60f);
                view.overlay = shade;
            }

            view.motion = CreateRect("Motion", rect);
            UIRoot.Stretch(view.motion);
            if (kind == UIEntryKind.Popup)
            {
                view.motion.anchorMin = new Vector2(0.2f, 0.2f);
                view.motion.anchorMax = new Vector2(0.8f, 0.8f);
            }
            view.content = CreateRect("Content", view.motion);
            UIRoot.Stretch(view.content);
            UIRoot.Stretch(rect);
            return view;
        }

        private static void ConfigureViewCanvas(Canvas canvas)
        {
            canvas.additionalShaderChannels |= TextShaderChannels;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ApplyCanvasSize(UIRoot root)
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = root.referenceResolution;
            rect.localScale = Vector3.one * ReviewScale;
            root.GetComponent<CanvasScaler>().referenceResolution = root.referenceResolution;
        }

        private static void PlaceFrame(UIRoot root, RectTransform frame, int index)
        {
            var resolution = root.ReferenceResolution;
            var columns = Mathf.Max(1, root.ReviewColumns);
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = frame.anchorMin;
            frame.sizeDelta = resolution;
            frame.anchoredPosition = new Vector2(index % columns * (resolution.x + ReviewSpacing),
                -index / columns * (resolution.y + ReviewSpacing));
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<EventSystem>(true)))
                return;
            var gameObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create UI EventSystem");
        }

        private static Bounds RectBounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bounds = new Bounds(corners[0], Vector3.zero);
            foreach (var corner in corners)
                bounds.Encapsulate(corner);
            return bounds;
        }

        private static void MarkChanged(UIRoot root)
        {
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
    }
}
