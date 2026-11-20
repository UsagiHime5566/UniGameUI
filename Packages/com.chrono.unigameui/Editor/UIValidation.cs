using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor
{
    public enum UIIssueSeverity { Warning, Error }

    public readonly struct UIIssue
    {
        public UIIssueSeverity Severity { get; }
        public string Message { get; }

        internal UIIssue(UIIssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public override string ToString() => $"{Severity}: {Message}";
    }

    public static class UIValidation
    {
        public static IReadOnlyList<UIIssue> Check(UIRoot root)
        {
            return Check(root, root ? root.gameObject.scene.path : null, true);
        }

        internal static IReadOnlyList<UIIssue> Check(UIRoot root, string scenePath, bool checkConstantsFile)
        {
            var issues = new List<UIIssue>();
            var views = new Dictionary<string, UIEntry>();
            var routes = new HashSet<(string, string)>();
            if (!root || !root.reviewLayer)
            {
                issues.Add(Error("The scene has no authored UniGameUI workspace."));
                return issues;
            }

            var dotween = UIDOTweenSetup.Status;
            if (dotween != UIDOTweenStatus.Ready)
                issues.Add(Error(UIDOTweenSetup.SetupMessage(dotween)));

            foreach (var entry in root.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id) || !views.TryAdd(entry.Id, entry))
                {
                    issues.Add(Error("A view has an empty or duplicate ID."));
                    continue;
                }

                var usesPrefab = root.Mode == UIAuthoringMode.Prefab || entry.Kind == UIEntryKind.Popup;
                if (!entry.Review || !entry.Review.Motion || !entry.Review.Content ||
                    !entry.Review.Group || entry.Review.Id != entry.Id ||
                    !entry.Review.transform.IsChildOf(root.reviewLayer))
                    issues.Add(Error($"{entry.Id}: Scene review view or required references are missing."));
                if (usesPrefab && (!entry.Prefab || !entry.Prefab.Motion || !entry.Prefab.Content ||
                    !entry.Prefab.Group || entry.Prefab.Id != entry.Id))
                    issues.Add(Error($"{entry.Id}: runtime Prefab or required references are missing."));
                if (UIAuthoring.HasReviewOverrides(entry))
                    issues.Add(Warning($"{entry.Id}: review has overrides waiting to be applied to the Prefab."));
                if (UIAuthoring.NeedsViewCanvas(entry.Review) ||
                    (usesPrefab && UIAuthoring.NeedsViewCanvas(entry.Prefab)))
                    issues.Add(Warning($"「{entry.Id}」缺少子 Canvas 或 GraphicRaycaster，可在藍圖視窗按「補上子 Canvas」。"));

                CheckTransitions(entry.Review, entry.Id, issues);
                if (usesPrefab)
                    CheckTransitions(entry.Prefab, entry.Id + " Prefab", issues);
                CheckViewOptions(entry, entry.Review, "", issues);
                if (usesPrefab)
                    CheckViewOptions(entry, entry.Prefab, " 的 Prefab 本體", issues);
            }

            if (!string.IsNullOrEmpty(root.StartPageId) &&
                (!views.TryGetValue(root.StartPageId, out var start) || start.Kind != UIEntryKind.Page))
                issues.Add(Error($"Unknown initial page '{root.StartPageId}'."));

            foreach (var route in root.Routes)
            {
                if (route == null || string.IsNullOrEmpty(route.Source) || string.IsNullOrEmpty(route.Action) ||
                    !routes.Add((route.Source, route.Action)) || !views.ContainsKey(route.Source))
                {
                    issues.Add(Error("A blueprint route has a missing source or a duplicate or empty action."));
                    continue;
                }
                if (UIRoute.TargetsEntry(route.Kind) &&
                    (string.IsNullOrEmpty(route.Target) || !views.TryGetValue(route.Target, out var target) ||
                     target.Kind != (route.Kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup)))
                    issues.Add(Error($"{route.Source}/{route.Action}: invalid route target '{route.Target}'."));
            }

            foreach (var entry in views.Values)
            {
                if (root.Mode == UIAuthoringMode.Prefab || entry.Kind == UIEntryKind.Popup)
                    CheckBindings(entry, entry.Prefab, routes, issues, true);
                if (!entry.Review)
                    continue;
                var elementIds = new HashSet<string>();
                foreach (var element in UIPlan.ElementsOf(entry.Review))
                {
                    if (string.IsNullOrEmpty(element.Id) || !elementIds.Add(element.Id))
                        issues.Add(Error($"{entry.Id}: duplicate or empty element ID on {element.name}."));
                }
                CheckBindings(entry, entry.Review, routes, issues, false);
            }

            CheckInput(root, issues);
            CheckWidgets(views.Values, issues);
            UIIdConstants.Check(root, scenePath, checkConstantsFile, issues);
            return issues;
        }

        private static void CheckViewOptions(UIEntry entry, UIView view, string where, List<UIIssue> issues)
        {
            if (!view)
                return;

            var selected = view.DefaultSelected;
            if (!ReferenceEquals(selected, null) && !selected)
                issues.Add(Error($"「{entry.Id}」{where}的預設選取元件已經遺失，請重新指定或清除。"));
            else if (selected && !selected.transform.IsChildOf(view.transform))
                issues.Add(Error($"「{entry.Id}」{where}的預設選取元件「{selected.name}」不在這個畫面裡。"));

            if (entry.Kind == UIEntryKind.Page && (view.ReusesInstance || view.ClosesOnOverlayClick))
            {
                issues.Add(Error($"「{entry.Id}」{where}是頁面，卻開啟了只對 Popup 有意義的選項" +
                    "（關閉後保留並重用、點遮罩關閉）。請在 Inspector 關閉。"));
            }
            if (entry.Kind == UIEntryKind.Popup && view.ClosesOnOverlayClick && !view.Overlay)
                issues.Add(Error($"「{entry.Id}」{where}設定了點遮罩關閉，但沒有遮罩物件。"));
        }

        private static void CheckWidgets(IEnumerable<UIEntry> entries, List<UIIssue> issues)
        {
            var sets = UIWidgets.FindAll();
            if (sets.Count == 0)
            {
                issues.Add(Error("專案沒有元件集。請執行 Tools → UniGameUI → Install Starter Widgets。"));
                return;
            }
            if (sets.Count > 1)
            {
                issues.Add(Error(UIWidgets.MultipleSetsMessage(sets)));
                return;
            }

            var set = sets[0];
            var names = new HashSet<string>();
            foreach (var widget in set.Widgets)
            {
                if (widget == null || string.IsNullOrEmpty(widget.Name) || !names.Add(widget.Name))
                {
                    issues.Add(Error($"元件集裡有空白或重複的名稱「{widget?.Name}」。"));
                    continue;
                }
                if (!widget.Prefab)
                {
                    issues.Add(Error($"元件集的「{widget.Name}」沒有 Prefab。" +
                        "請重新指定，內建元件可以用 Install Starter Widgets 補回。"));
                    continue;
                }
                var missing = UIWidgets.MissingRequirement(widget.Kind, widget.Prefab);
                if (missing != null)
                    issues.Add(Error($"元件集的「{widget.Name}」比照 {widget.Kind}，但 {missing}"));
            }

            foreach (var entry in entries)
            {
                if (!entry.Review)
                    continue;
                foreach (var element in UIPlan.ElementsOf(entry.Review))
                {
                    if (!string.IsNullOrEmpty(element.Kind) && !names.Contains(element.Kind))
                    {
                        issues.Add(Error($"「{entry.Id}/{element.Id}」記錄的元件名稱「{element.Kind}」" +
                            $"不在元件集裡。可用的名稱：{string.Join("、", names.OrderBy(name => name))}。"));
                    }
                }
            }
        }

        // 測試用：模擬 Player Settings 的 Active Input Handling（0 舊、1 新、2 兩者）。
        internal static int? ActiveInputHandlingOverride;

        // EventSystem 可以放在工作區自己的 Scene，也可以放在執行期一起載入的共用 Scene。
        private static void CheckInput(UIRoot root, List<UIIssue> issues)
        {
            var scene = root.gameObject.scene;
            var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var active = all.Where(eventSystem => eventSystem.enabled && eventSystem.gameObject.activeInHierarchy)
                .ToArray();
            var own = active.Where(eventSystem => eventSystem.gameObject.scene == scene).ToArray();
            var others = active.Where(eventSystem => eventSystem.gameObject.scene != scene).ToArray();
            // 輸入模組只檢查執行期會用到的那一個：工作區的 Scene 有就是它，沒有就是其他 Scene 的。
            var used = own.Length == 1 ? own : own.Length == 0 ? others : Array.Empty<EventSystem>();

            if (own.Length > 1)
            {
                issues.Add(Error($"工作區所在的 Scene「{SceneLabel(scene)}」有 {own.Length} 個啟用的 EventSystem" +
                    $"（{Names(own)}），請只保留一個。"));
            }
            else if (all.Length == 0)
            {
                issues.Add(Error("載入的 Scene 裡沒有 EventSystem。建立工作區時會自動加上；被刪掉時請在工作區的 Scene " +
                    "或執行期一起載入的共用 Scene 加回一個，輸入模組使用 Input System UI Input Module。"));
            }
            else if (active.Length == 0)
            {
                issues.Add(Error($"載入的 Scene 裡有 EventSystem（{Names(all)}），但沒有啟用。" +
                    "請啟用它的 GameObject 與 EventSystem 元件。"));
            }
            else if (own.Length == 1 && others.Length > 0)
            {
                issues.Add(Warning($"工作區的 Scene「{SceneLabel(scene)}」和其他載入的 Scene 都有啟用的 EventSystem" +
                    $"（{Names(others)}）。執行期同時載入這些 Scene 時只能留一個。"));
            }
            else if (own.Length == 0 && others.Length > 1)
            {
                issues.Add(Warning($"工作區的 Scene「{SceneLabel(scene)}」沒有 EventSystem，其他載入的 Scene 有 " +
                    $"{others.Length} 個啟用的 EventSystem（{Names(others)}）。執行期同時載入這些 Scene 時只能留一個。"));
            }

            foreach (var eventSystem in used)
            {
                if (eventSystem.GetComponent<BaseInputModule>() is InputSystemUIInputModule)
                    continue;
                var where = eventSystem.gameObject.scene == scene
                    ? ""
                    : $"（在 Scene「{SceneLabel(eventSystem.gameObject.scene)}」）";
                issues.Add(Error($"EventSystem「{eventSystem.name}」{where}的輸入模組必須是 InputSystemUIInputModule。" +
                    "請移除其他輸入模組，再加上 Input System UI Input Module。"));
            }

            if (ActiveInputHandling() == 0)
            {
                issues.Add(Error("Player Settings 的 Active Input Handling 只啟用舊的 Input Manager。" +
                    "請改成 Input System Package (New) 或 Both，並重新啟動 Editor。"));
            }
        }

        private static string Names(IEnumerable<EventSystem> eventSystems)
        {
            return string.Join("、", eventSystems.Select(eventSystem =>
                $"「{eventSystem.name}」在「{SceneLabel(eventSystem.gameObject.scene)}」"));
        }

        private static string SceneLabel(UnityEngine.SceneManagement.Scene scene)
        {
            return string.IsNullOrEmpty(scene.name) ? "未儲存的 Scene" : scene.name;
        }

        private static int ActiveInputHandling()
        {
            if (ActiveInputHandlingOverride.HasValue)
                return ActiveInputHandlingOverride.Value;

            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            var property = settings.Length == 0 || !settings[0]
                ? null
                : new SerializedObject(settings[0]).FindProperty("activeInputHandler");
            if (property != null)
                return property.intValue;
#if ENABLE_INPUT_SYSTEM
            return 1;
#else
            return 0;
#endif
        }

        private static void CheckBindings(UIEntry entry, UIView view, HashSet<(string, string)> routes,
            List<UIIssue> issues, bool isPrefab)
        {
            if (!view)
                return;
            foreach (var binding in view.GetComponentsInChildren<UIButtonAction>(true))
            {
                if (binding.GetComponentInParent<UIView>(true) != view || routes.Contains((entry.Id, binding.Action)))
                    continue;
                var message = $"「{entry.Id}/{binding.name}」的動作 '{binding.Action}' 找不到路由。";
                issues.Add(Error(isPrefab
                    ? $"Prefab 本體中{message}請修正審核實例的綁定並套用覆寫。"
                    : message));
            }
        }

        private static void CheckTransitions(UIView view, string label, List<UIIssue> issues)
        {
            if (!view)
                return;
#if DOTWEEN
            if ((view.Enter.Preset == UITransitionPreset.Custom || view.Exit.Preset == UITransitionPreset.Custom) &&
                !view.GetComponent<UIDOTweenAnimation>())
                issues.Add(Error($"{label}: Custom animation needs a UIDOTweenAnimation component."));
#endif
        }

        private static UIIssue Error(string message) => new UIIssue(UIIssueSeverity.Error, message);
        private static UIIssue Warning(string message) => new UIIssue(UIIssueSeverity.Warning, message);
    }
}
