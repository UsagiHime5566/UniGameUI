using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor
{
    public static class UIPlan
    {
        [Serializable]
        private sealed class Plan
        {
            public string scenePath;
            public bool createScene;
            public string mode;
            public Vector2 referenceResolution;
            public int reviewColumns;
            public string startPage;
            public string pageTransitionMode;
            public ViewSpec[] views;
            public RouteSpec[] routes;
        }

        [Serializable]
        private sealed class ViewSpec
        {
            public string id;
            public string title;
            public string kind;
            public string prefabPath;
            public bool updateExisting;
            public string background;
            public AnimationSpec enter;
            public AnimationSpec exit;
            public ElementSpec[] elements;
        }

        // JsonUtility instantiates missing nested objects instead of leaving them null.
        // Numeric fields start as NaN so omitted values can be told apart and keep the current value.
        [Serializable]
        private sealed class AnimationSpec
        {
            public string preset;
            public float duration = float.NaN;
            public string direction;
            public string ease;
            public float distance = float.NaN;
            public float angle = float.NaN;
            public float zoomScale = float.NaN;

            public bool IsEmpty =>
                string.IsNullOrEmpty(preset) && string.IsNullOrEmpty(direction) && string.IsNullOrEmpty(ease) &&
                float.IsNaN(duration) && float.IsNaN(distance) && float.IsNaN(angle) && float.IsNaN(zoomScale);
        }

        [Serializable]
        private sealed class ElementSpec
        {
            public string id;
            public string kind;
            public bool updateExisting;
            public string text;
            public string action;
            public RectSpec rect;
            public float fontSize;
            public string fontPath;
            public string color;
            public string textColor;
            public string spritePath;
        }

        [Serializable]
        private sealed class RectSpec
        {
            public Vector2 anchor = Unset;
            public Vector2 position = Unset;
            public Vector2 size = Unset;

            public bool IsEmpty => IsUnset(anchor) && IsUnset(position) && IsUnset(size);
        }

        private static readonly Vector2 Unset = new Vector2(float.NaN, float.NaN);
        private static readonly Vector2 DefaultAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 DefaultSize = new Vector2(240, 72);

        [Serializable]
        private sealed class RouteSpec
        {
            public string source;
            public string action;
            public string kind;
            public string target;
            public string history;
            public string result;
            public bool replace;
        }

        private enum ElementKind { Text, Button, Image }

        [MenuItem("Tools/UniGameUI/Apply JSON Plan…")]
        private static async void ApplyFromMenu()
        {
            var path = EditorUtility.OpenFilePanel("Apply UniGameUI plan", "", "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var root = await ApplyAsync(File.ReadAllText(path));
                SceneManager.SetActiveScene(root.gameObject.scene);
                UIAuthoring.FocusReview(root);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("UniGameUI", exception.Message, "關閉");
            }
        }

        public static void RunFromCommandLine()
        {
            var arguments = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(arguments, "-unigameui-plan");
            if (index < 0 || index == arguments.Length - 1)
                throw new ArgumentException("Supply -unigameui-plan followed by the path to a JSON plan.");
            Apply(File.ReadAllText(arguments[index + 1]), true);
        }

        public static UIRoot Apply(string json, bool saveScene = false)
        {
            return Apply(ReadPlan(json), saveScene, SceneManager.GetActiveScene());
        }

        public static async Awaitable<UIRoot> ApplyAsync(string json, bool saveScene = false)
        {
            var plan = ReadPlan(json);
            var active = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && NeedsText(plan))
                await UITmpResources.ImportWithEditorAsync();
            return Apply(plan, saveScene, active);
        }

        private static Plan ReadPlan(string json)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply an authoring plan in Edit Mode.");
            UIDOTweenSetup.CheckReady();
            return JsonUtility.FromJson<Plan>(json) ?? throw new ArgumentException("Invalid JSON plan.");
        }

        private static UIRoot Apply(Plan plan, bool saveScene, Scene originalActiveScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply an authoring plan in Edit Mode.");
            if (saveScene && string.IsNullOrEmpty(plan.scenePath))
                throw new ArgumentException("A saving plan must name the exact Scene asset path.");

            var scene = FindScene(plan, originalActiveScene, out var createdScene);
            var createdPrefabs = new List<string>();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply UniGameUI plan");
            UIRoot root;
            try
            {
                root = UIAuthoring.FindWorkspace(scene);
                var mode = Parse(plan.mode, root ? root.Mode : UIAuthoringMode.SingleScene);
                Preflight(plan, root, mode);
                if (NeedsText(plan))
                    UITmpResources.Ensure();

                root = UIAuthoring.CreateWorkspace(scene, mode);
                if (plan.referenceResolution != Vector2.zero || plan.reviewColumns != 0)
                    UIAuthoring.ConfigureReview(root,
                        plan.referenceResolution == Vector2.zero ? root.ReferenceResolution : plan.referenceResolution,
                        plan.reviewColumns == 0 ? root.ReviewColumns : plan.reviewColumns);

                foreach (var view in plan.views ?? Array.Empty<ViewSpec>())
                    ApplyView(root, view, createdPrefabs);
                foreach (var route in plan.routes ?? Array.Empty<RouteSpec>())
                {
                    var existing = FindRoute(root, route);
                    if (!route.replace && existing != null)
                        continue;
                    var kind = Parse(route.kind, existing?.Kind ?? UIRouteKind.ShowPage);
                    UIAuthoring.Connect(root, route.source, route.action,
                        kind, route.target ?? existing?.Target, ParseHistory(route, kind, existing),
                        route.result ?? existing?.Result);
                }
                if (plan.startPage != null)
                    UIAuthoring.SetStartPage(root, plan.startPage);
                if (!string.IsNullOrEmpty(plan.pageTransitionMode))
                    UIAuthoring.SetPageTransitionMode(root, Parse(plan.pageTransitionMode, root.PageTransitionMode));

                var issues = UIValidation.Check(root, saveScene || createdScene ? plan.scenePath : scene.path, false);
                var errors = issues.Where(issue => issue.Severity == UIIssueSeverity.Error);
                if (errors.Any())
                    throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                foreach (var issue in issues)
                    Debug.LogWarning($"UniGameUI {issue}", root);

                if ((saveScene || createdScene) && !EditorSceneManager.SaveScene(scene, plan.scenePath))
                    throw new IOException($"Could not save Scene '{plan.scenePath}'.");

                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var path in createdPrefabs)
                    AssetDatabase.DeleteAsset(path);
                if (createdScene && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                throw;
            }

            UIIdConstants.Generate(root);
            Debug.Log($"UniGameUI plan applied to {scene.path}: {root.Entries.Count} views, " +
                $"{root.Routes.Count} routes. DOTween: {UITransition.IsDOTweenReady}.", root);
            return root;
        }

        public static GameObject FindElement(UIView view, string id)
        {
            return view.GetComponentsInChildren<UIElementId>(true)
                .FirstOrDefault(element => element.Id == id)?.gameObject;
        }

        private static Scene FindScene(Plan plan, Scene originalActiveScene, out bool created)
        {
            created = false;
            if (string.IsNullOrEmpty(plan.scenePath))
                return originalActiveScene;
            UIAuthoring.CheckAssetPath(plan.scenePath, ".unity");
            var scene = SceneManager.GetSceneByPath(plan.scenePath);
            if (scene.IsValid() && scene.isLoaded)
                return scene;

            var active = SceneManager.GetActiveScene();
            var untitled = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(active.path);
            if (untitled && active.isDirty)
                throw new InvalidOperationException("Save the open untitled Scene before applying a plan to a different Scene.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(plan.scenePath))
                return EditorSceneManager.OpenScene(plan.scenePath, untitled ? OpenSceneMode.Single : OpenSceneMode.Additive);
            if (!plan.createScene)
                throw new FileNotFoundException("Scene asset does not exist. Set createScene to create it.", plan.scenePath);
            UIAuthoring.EnsureAssetFolder(plan.scenePath);
            created = true;
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                untitled ? NewSceneMode.Single : NewSceneMode.Additive);
        }

        private static void Preflight(Plan plan, UIRoot root, UIAuthoringMode mode)
        {
            if (root && root.Mode != mode)
                throw new ArgumentException("The plan's authoring mode differs from the existing workspace.");
            var kinds = new Dictionary<string, UIEntryKind>();
            if (root)
            {
                foreach (var entry in root.Entries)
                    kinds.Add(entry.Id, entry.Kind);
            }

            var viewIds = new HashSet<string>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in plan.views ?? Array.Empty<ViewSpec>())
            {
                UIAuthoring.CheckId(view.id);
                var kind = Parse(view.kind, kinds.TryGetValue(view.id, out var existing) ? existing : UIEntryKind.Page);
                if (!viewIds.Add(view.id) || (kinds.ContainsKey(view.id) && existing != kind))
                    throw new ArgumentException($"Duplicate or incompatible view '{view.id}'.");
                kinds[view.id] = kind;

                if (kind == UIEntryKind.Popup || mode == UIAuthoringMode.Prefab)
                {
                    var path = string.IsNullOrEmpty(view.prefabPath)
                        ? UIAuthoring.DefaultPrefabPath(kind, view.id)
                        : view.prefabPath;
                    UIAuthoring.CheckAssetPath(path, ".prefab");
                    if (!paths.Add(path))
                        throw new ArgumentException($"Two views use '{path}'.");
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset && (asset is not GameObject prefab || prefab.GetComponent<UIView>()?.Id != view.id))
                        throw new ArgumentException($"Prefab path is already in use: {path}");
                }
                else if (!string.IsNullOrEmpty(view.prefabPath))
                {
                    throw new ArgumentException($"Single Scene page '{view.id}' cannot have a Prefab path.");
                }

                var elementIds = new HashSet<string>();
                foreach (var element in view.elements ?? Array.Empty<ElementSpec>())
                {
                    UIAuthoring.CheckId(element.id);
                    if (!elementIds.Add(element.id))
                        throw new ArgumentException($"{view.id}: duplicate element ID '{element.id}'.");
                    var type = Parse(element.kind, ElementKind.Text);
                    if (!string.IsNullOrEmpty(element.action))
                    {
                        UIAuthoring.CheckId(element.action);
                        if (type != ElementKind.Button)
                            throw new ArgumentException($"{view.id}/{element.id}: only Buttons can dispatch actions.");
                    }
                    if (element.rect != null && (IsNonPositive(element.rect.size.x) || IsNonPositive(element.rect.size.y)))
                        throw new ArgumentException($"{view.id}/{element.id}: size must be positive.");
                }
            }

            if (!string.IsNullOrEmpty(plan.startPage) &&
                (!kinds.TryGetValue(plan.startPage, out var start) || start != UIEntryKind.Page))
                throw new ArgumentException($"Unknown start page '{plan.startPage}'.");

            var routeKeys = new HashSet<(string, string)>();
            foreach (var route in plan.routes ?? Array.Empty<RouteSpec>())
            {
                UIAuthoring.CheckId(route.action);
                var existing = FindRoute(root, route);
                var kind = Parse(route.kind, existing?.Kind ?? UIRouteKind.ShowPage);
                var targetId = route.target ?? existing?.Target;
                if (string.IsNullOrEmpty(route.source) || !kinds.ContainsKey(route.source) ||
                    !routeKeys.Add((route.source, route.action)))
                    throw new ArgumentException($"Missing source or duplicate route '{route.source}/{route.action}'.");
                if (route.history != null && kind != UIRouteKind.ShowPage)
                    throw new ArgumentException($"路由「{route.source}/{route.action}」的 history 欄位只適用於 ShowPage（目前為 {kind}）。");
                if (route.result != null && kind != UIRouteKind.ClosePopup)
                    throw new ArgumentException($"路由「{route.source}/{route.action}」的 result 欄位只適用於 ClosePopup（目前為 {kind}）。");
                if (UIRoute.TargetsEntry(kind) &&
                    (string.IsNullOrEmpty(targetId) || !kinds.TryGetValue(targetId, out var target) ||
                     target != (kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup)))
                    throw new ArgumentException($"{route.source}/{route.action}: invalid target '{targetId}'.");
                ParseHistory(route, kind, existing);
            }
        }

        private static UIRoute FindRoute(UIRoot root, RouteSpec route)
        {
            return root ? root.Routes.FirstOrDefault(existing =>
                existing.Source == route.source && existing.Action == route.action) : null;
        }

        private static UIHistoryMode ParseHistory(RouteSpec route, UIRouteKind kind, UIRoute existing)
        {
            return kind == UIRouteKind.ShowPage
                ? Parse(route.history, existing?.History ?? UIHistoryMode.Push)
                : UIHistoryMode.Push;
        }

        private static bool NeedsText(Plan plan)
        {
            return (plan.views ?? Array.Empty<ViewSpec>()).Any(view =>
                (view.elements ?? Array.Empty<ElementSpec>()).Any(element =>
                    element.text != null || Parse(element.kind, ElementKind.Text) == ElementKind.Text));
        }

        private static void ApplyView(UIRoot root, ViewSpec spec, List<string> createdPrefabs)
        {
            var existing = UIAuthoring.FindEntry(root, spec.id);
            var kind = Parse(spec.kind, existing?.Kind ?? UIEntryKind.Page);
            var newView = existing == null;
            var usesPrefab = kind == UIEntryKind.Popup || root.Mode == UIAuthoringMode.Prefab;
            var prefabPath = string.IsNullOrEmpty(spec.prefabPath)
                ? UIAuthoring.DefaultPrefabPath(kind, spec.id)
                : spec.prefabPath;
            var newPrefab = usesPrefab && newView && !AssetDatabase.LoadMainAssetAtPath(prefabPath);
            if (newPrefab)
                createdPrefabs.Add(prefabPath);

            var view = kind == UIEntryKind.Popup
                ? UIAuthoring.CreatePopup(root, spec.id, spec.title, spec.prefabPath)
                : UIAuthoring.CreatePage(root, spec.id, spec.title, spec.prefabPath);
            if (newView || spec.updateExisting)
            {
                if (!string.IsNullOrEmpty(spec.background))
                {
                    var background = GetOrAdd<Image>(view.Content.gameObject);
                    Undo.RecordObject(background, "Set UI background");
                    background.color = ParseColor(spec.background);
                    background.raycastTarget = false;
                    UIAuthoring.RecordOverride(background);
                }
                SetAnimation(view, true, spec.enter);
                SetAnimation(view, false, spec.exit);
            }

            foreach (var element in spec.elements ?? Array.Empty<ElementSpec>())
                ApplyElement(view, element);
            if (newPrefab)
                UIAuthoring.ApplyReviewOverrides(root, spec.id);
        }

        private static void ApplyElement(UIView view, ElementSpec spec)
        {
            var gameObject = FindElement(view, spec.id);
            var created = !gameObject;
            if (!created && !spec.updateExisting)
                return;
            var kind = Parse(spec.kind, ElementKind.Text);
            if (created)
            {
                gameObject = new GameObject(spec.id, typeof(RectTransform), typeof(UIElementId));
                gameObject.transform.SetParent(view.Content, false);
                gameObject.GetComponent<UIElementId>().id = spec.id;
                Undo.RegisterCreatedObjectUndo(gameObject, "Create UI element");
            }
            else
            {
                var existingKind = gameObject.GetComponent<Button>() ? ElementKind.Button :
                    gameObject.GetComponent<Image>() ? ElementKind.Image : ElementKind.Text;
                if (kind != existingKind)
                    throw new ArgumentException($"{view.Id}/{spec.id}: update the existing {existingKind} element.");
            }

            // Omitted rect fields keep the element's current layout; new elements start from the defaults.
            var layout = spec.rect ?? new RectSpec();
            if (created || !layout.IsEmpty)
            {
                var rect = (RectTransform)gameObject.transform;
                Undo.RecordObject(rect, "Set UI layout");
                if (created || !IsUnset(layout.anchor))
                {
                    var anchor = Merge(created ? DefaultAnchor : rect.anchorMin, layout.anchor);
                    rect.anchorMin = anchor;
                    rect.anchorMax = anchor;
                }
                if (created || !IsUnset(layout.position))
                    rect.anchoredPosition = Merge(created ? Vector2.zero : rect.anchoredPosition, layout.position);
                if (created || !IsUnset(layout.size))
                    rect.sizeDelta = Merge(created ? DefaultSize : rect.sizeDelta, layout.size);
                UIAuthoring.RecordOverride(rect);
            }

            if (kind == ElementKind.Text)
            {
                ApplyLabel(GetOrAdd<TextMeshProUGUI>(gameObject), spec, created, spec.textColor ?? spec.color);
                return;
            }

            var image = GetOrAdd<Image>(gameObject);
            if (created || spec.color != null || spec.spritePath != null)
            {
                Undo.RecordObject(image, "Set UI image");
                if (created)
                {
                    image.color = kind == ElementKind.Button ? new Color32(45, 106, 204, 255) : Color.white;
                    image.raycastTarget = kind == ElementKind.Button;
                }
                if (!string.IsNullOrEmpty(spec.color))
                    image.color = ParseColor(spec.color);
                if (!string.IsNullOrEmpty(spec.spritePath))
                    image.sprite = LoadAsset<Sprite>(spec.spritePath);
                UIAuthoring.RecordOverride(image);
            }
            if (kind != ElementKind.Button)
                return;

            var button = GetOrAdd<Button>(gameObject);
            if (!button.targetGraphic)
            {
                Undo.RecordObject(button, "Set button graphic");
                button.targetGraphic = image;
                UIAuthoring.RecordOverride(button);
            }
            if (!string.IsNullOrEmpty(spec.action))
                UIAuthoring.BindButton(button, spec.action);
            if (spec.text == null && spec.fontPath == null && spec.textColor == null && spec.fontSize <= 0)
                return;

            var label = gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            var createdLabel = !label;
            if (createdLabel)
            {
                if (spec.text == null)
                    return;
                var labelObject = new GameObject("Label", typeof(RectTransform));
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.SetParent(gameObject.transform, false);
                UIRoot.Stretch(labelRect);
                labelRect.offsetMin = new Vector2(12, 4);
                labelRect.offsetMax = new Vector2(-12, -4);
                Undo.RegisterCreatedObjectUndo(labelObject, "Create button label");
                label = Undo.AddComponent<TextMeshProUGUI>(labelObject);
            }
            ApplyLabel(label, spec, createdLabel, spec.textColor);
        }

        private static void ApplyLabel(TextMeshProUGUI label, ElementSpec spec, bool created, string color)
        {
            Undo.RecordObject(label, "Set UI text");
            if (created)
            {
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 32;
                label.color = Color.white;
                label.raycastTarget = false;
            }
            if (spec.text != null)
                label.text = spec.text;
            if (spec.fontSize > 0)
                label.fontSize = spec.fontSize;
            if (!string.IsNullOrEmpty(spec.fontPath))
                label.font = LoadAsset<TMP_FontAsset>(spec.fontPath);
            if (!string.IsNullOrEmpty(color))
                label.color = ParseColor(color);
            UIAuthoring.RecordOverride(label);
        }

        private static void SetAnimation(UIView view, bool entering, AnimationSpec animation)
        {
            // Omitted animation fields keep the view's current transition values.
            if (animation == null || animation.IsEmpty)
                return;
            var current = entering ? view.Enter : view.Exit;
            UIAuthoring.SetTransition(view, entering,
                Parse(animation.preset, current.Preset),
                Merge(current.Duration, animation.duration),
                Parse(animation.direction, current.Direction),
                Parse(animation.ease, current.Ease),
                Merge(current.Distance, animation.distance),
                Merge(current.Angle, animation.angle),
                Merge(current.ZoomScale, animation.zoomScale));
        }

        private static bool IsUnset(Vector2 value)
        {
            return float.IsNaN(value.x) && float.IsNaN(value.y);
        }

        private static bool IsNonPositive(float value)
        {
            return !float.IsNaN(value) && value <= 0;
        }

        private static float Merge(float current, float value)
        {
            return float.IsNaN(value) ? current : value;
        }

        private static Vector2 Merge(Vector2 current, Vector2 value)
        {
            return new Vector2(Merge(current.x, value.x), Merge(current.y, value.y));
        }

        private static T Parse<T>(string value, T fallback) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
                return fallback;
            if (Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(typeof(T), parsed))
                return parsed;
            throw new ArgumentException($"Unknown {typeof(T).Name} value '{value}'.");
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent<T>(out var component) ? component : Undo.AddComponent<T>(gameObject);
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset)
                throw new ArgumentException($"No {typeof(T).Name} exists at '{path}'.");
            return asset;
        }

        private static Color ParseColor(string value)
        {
            if (ColorUtility.TryParseHtmlString(value, out var color))
                return color;
            throw new ArgumentException($"'{value}' is not a valid HTML color.");
        }
    }
}
