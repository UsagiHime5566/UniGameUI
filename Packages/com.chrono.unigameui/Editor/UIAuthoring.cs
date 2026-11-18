using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor
{
    public static class UIAuthoring
    {
        private const float ReviewScale = 0.01f;
        private const float ReviewSpacing = 96f;

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

        public static void Connect(UIRoot root, string source, string action, UIRouteKind kind, string target = null)
        {
            CheckRoot(root);
            if (FindEntry(root, source) == null)
                throw new ArgumentException($"Unknown source '{source}'.");
            CheckId(action);
            if (UIRoute.TargetsEntry(kind))
            {
                var expected = kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup;
                if (FindEntry(root, target)?.Kind != expected)
                    throw new ArgumentException($"'{target}' must be a registered {expected}.");
            }

            Undo.RecordObject(root, "Connect UI route");
            var route = root.routes.Find(item => item.Source == source && item.Action == action);
            if (route == null)
            {
                route = new UIRoute { source = source, action = action };
                root.routes.Add(route);
            }
            route.kind = kind;
            route.target = UIRoute.TargetsEntry(kind) ? target : "";
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
            var binding = button.GetComponent<UIButtonAction>() ??
                Undo.AddComponent<UIButtonAction>(button.gameObject);
            Undo.RecordObject(binding, "Bind UI button");
            binding.action = action;
            RecordOverride(binding);
            return binding;
        }

        public static void SetTransition(UIView view, bool entering, UITransitionPreset preset,
            float duration = 0.25f, UITransitionDirection direction = UITransitionDirection.Right,
            UITransitionEase ease = UITransitionEase.OutQuad, float distance = 120f, float angle = 12f)
        {
            if (!view || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Select a view in Edit Mode.");
            if (!float.IsFinite(duration) || !float.IsFinite(distance) || !float.IsFinite(angle) ||
                duration < 0 || distance < 0)
                throw new ArgumentException("Animation duration and distance must be non-negative and finite.");
            Undo.RecordObject(view, "Set UI transition");
            var transition = entering ? view.enter : view.exit;
            transition.preset = preset;
            transition.duration = duration;
            transition.direction = direction;
            transition.ease = ease;
            transition.distance = distance;
            transition.angle = angle;
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
            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
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
            var entry = FindEntry(root, id);
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

        internal static void RecordOverride(Object changed)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
            EditorUtility.SetDirty(changed);
            if (changed is Component component && component.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
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
            view.id = id;
            view.canvasGroup = rect.GetComponent<CanvasGroup>();
            if (kind == UIEntryKind.Popup)
            {
                var shade = CreateRect("Overlay", rect);
                UIRoot.Stretch(shade);
                shade.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.60f);
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
            var gameObject = new GameObject("EventSystem", typeof(EventSystem));
            SceneManager.MoveGameObjectToScene(gameObject, scene);
#if ENABLE_INPUT_SYSTEM
            var module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null)
                gameObject.AddComponent(module);
            else
                Debug.LogError("Install and enable an Input System UI input module for this EventSystem.", gameObject);
#else
            gameObject.AddComponent<StandaloneInputModule>();
#endif
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
