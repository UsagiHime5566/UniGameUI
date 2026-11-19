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
            if (action != originalAction && entry.Review)
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
            route.history = kind == UIRouteKind.ShowPage ? history : UIHistoryMode.Push;
            route.result = kind == UIRouteKind.ClosePopup ? result ?? "" : "";
        }

        private static UIEntry ValidateRoute(UIRoot root, string source, string action,
            UIRouteKind kind, string target, UIHistoryMode history)
        {
            CheckRoot(root);
            var entry = FindEntry(root, source) ?? throw new ArgumentException($"Unknown source '{source}'.");
            CheckId(action);
            if (kind == UIRouteKind.ShowPage && !Enum.IsDefined(typeof(UIHistoryMode), history))
                throw new ArgumentException($"不支援頁面歷史模式「{history}」。", nameof(history));
            if (UIRoute.TargetsEntry(kind))
            {
                var expected = kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup;
                if (FindEntry(root, target)?.Kind != expected)
                    throw new ArgumentException($"'{target}' must be a registered {expected}.");
            }
            return entry;
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
                       !PrefabUtility.IsDefaultOverride(modification) &&
                       !(modification.target is TMP_Text && modification.propertyPath == "m_TextStyleHashCode"))) ||
                   PrefabUtility.GetAddedGameObjects(instance).Count > 0 ||
                   PrefabUtility.GetRemovedGameObjects(instance).Count > 0 ||
                   PrefabUtility.GetAddedComponents(instance).Count > 0 ||
                   PrefabUtility.GetRemovedComponents(instance).Count > 0;
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

                    EnsureViewComponent<Canvas>(entry.Review, prefab, path, ConfigureViewCanvas);
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
            foreach (var path in propertyPaths)
            {
                var property = serialized.FindProperty(path);
                if (property == null || property.prefabOverride)
                    continue;
                var action = Perturb(property);
                if (action != null)
                    restore.Add(action);
            }
            if (restore.Count == 0)
                return;

            serialized.ApplyModifiedProperties();
            foreach (var action in restore)
                action();
            serialized.ApplyModifiedProperties();
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
                case SerializedPropertyType.Enum:
                {
                    var value = property.intValue;
                    property.intValue = value == 0 ? 1 : 0;
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
                case SerializedPropertyType.Color:
                {
                    var value = property.colorValue;
                    property.colorValue = new Color(
                        value.r < 0.5f ? 1f : 0f, value.g < 0.5f ? 1f : 0f,
                        value.b < 0.5f ? 1f : 0f, value.a < 0.5f ? 1f : 0f);
                    return () => property.colorValue = value;
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
                    if (!prefab || prefab.Id != id)
                        throw new InvalidOperationException($"Prefab path is already in use: {prefabPath}");
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

        internal static string DefaultPrefabPath(UIEntryKind kind, string id)
        {
            var folder = kind == UIEntryKind.Popup ? "Popups" : "Pages";
            return $"Assets/UniGameUI/{folder}/{id}.prefab";
        }

        private static UIView CreateView(RectTransform frame, string id, string title, UIEntryKind kind)
        {
            var rect = CreateRect(title, frame);
            Undo.RegisterCreatedObjectUndo(rect.gameObject, "Create UI view");
            var view = Undo.AddComponent<UIView>(rect.gameObject);
            ConfigureViewCanvas(Undo.AddComponent<Canvas>(rect.gameObject));
            Undo.AddComponent<GraphicRaycaster>(rect.gameObject);
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
            Undo.RecordObject(canvas, "設定子 Canvas");
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
