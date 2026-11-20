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
    public static partial class UIValidation
    {
        // 測試用：模擬 DOTween 還沒就緒的狀態。
        internal static UIDOTweenStatus? DOTweenStatusOverride;

        // 測試用：模擬 Player Settings 的 Active Input Handling（0 舊、1 新、2 兩者）。
        internal static int? ActiveInputHandlingOverride;

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
                issues.Add(new UIIssue(UIIssueCode.WorkspaceMissing, "這個 Scene 沒有 UniGameUI 工作區。", context: root));
                return issues;
            }

            var dotween = DOTweenStatusOverride ?? UIDOTweenSetup.Status;
            if (dotween != UIDOTweenStatus.Ready)
                issues.Add(new UIIssue(UIIssueCode.DOTweenNotReady, UIDOTweenSetup.SetupMessage(dotween), context: root));

            foreach (var entry in root.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id) || !views.TryAdd(entry.Id, entry))
                {
                    issues.Add(new UIIssue(UIIssueCode.ViewIdInvalid,
                        $"有畫面的 ID 是空的或重複（「{entry?.Id}」）。", entry?.Id, context: root));
                    continue;
                }

                var usesPrefab = root.Mode == UIAuthoringMode.Prefab || entry.Kind == UIEntryKind.Popup;
                if (!entry.Review || !entry.Review.Motion || !entry.Review.Content ||
                    !entry.Review.Group || entry.Review.Id != entry.Id ||
                    !entry.Review.transform.IsChildOf(root.reviewLayer))
                {
                    issues.Add(new UIIssue(UIIssueCode.ViewReviewMissing,
                        $"「{entry.Id}」在 Scene 裡的審核畫面遺失、缺少必要的引用，或畫面 ID 不符。",
                        entry.Id, context: ContextOf(root, entry)));
                }
                if (usesPrefab && (!entry.Prefab || !entry.Prefab.Motion || !entry.Prefab.Content ||
                    !entry.Prefab.Group || entry.Prefab.Id != entry.Id))
                {
                    issues.Add(new UIIssue(UIIssueCode.ViewPrefabMissing,
                        $"「{entry.Id}」執行期使用的 Prefab 遺失、缺少必要的引用，或畫面 ID 不符。",
                        entry.Id, context: ContextOf(root, entry)));
                }
                if (UIAuthoring.HasReviewOverrides(entry))
                {
                    issues.Add(new UIIssue(UIIssueCode.ViewOverridesPending,
                        $"「{entry.Id}」的審核畫面有待套用到 Prefab 的覆寫。執行期使用的是已儲存的 Prefab。",
                        entry.Id, context: ContextOf(root, entry)));
                }
                if (UIAuthoring.NeedsViewCanvas(entry.Review) ||
                    (usesPrefab && UIAuthoring.NeedsViewCanvas(entry.Prefab)))
                {
                    issues.Add(new UIIssue(UIIssueCode.ViewCanvasMissing,
                        $"「{entry.Id}」缺少子 Canvas 或 GraphicRaycaster，可在藍圖視窗按「補上子 Canvas」。",
                        entry.Id, context: ContextOf(root, entry)));
                }

                CheckTransitions(root, entry, entry.Review, "", issues);
                if (usesPrefab)
                    CheckTransitions(root, entry, entry.Prefab, " 的 Prefab 本體", issues);
                CheckViewOptions(root, entry, entry.Review, "", issues);
                if (usesPrefab)
                    CheckViewOptions(root, entry, entry.Prefab, " 的 Prefab 本體", issues);
            }

            if (!string.IsNullOrEmpty(root.StartPageId) &&
                (!views.TryGetValue(root.StartPageId, out var start) || start.Kind != UIEntryKind.Page))
            {
                issues.Add(new UIIssue(UIIssueCode.StartUnknown,
                    $"起始頁「{root.StartPageId}」不存在，或不是頁面。", root.StartPageId, context: root));
            }

            foreach (var route in root.Routes)
            {
                // 訊號路由的來源是「任何畫面」，不是某一個畫面。
                var isSignal = route != null && route.IsSignal;
                if (route == null || string.IsNullOrEmpty(route.Source) || string.IsNullOrEmpty(route.Action) ||
                    !routes.Add((route.Source, route.Action)) || (!isSignal && !views.ContainsKey(route.Source)))
                {
                    issues.Add(new UIIssue(UIIssueCode.RouteInvalid,
                        $"路由「{route?.Source}/{route?.Action}」的來源畫面不存在、動作是空的，" +
                        "或同一個來源有重複的動作。", isSignal ? null : route?.Source, context: root));
                    continue;
                }

                var sourceId = isSignal ? null : route.Source;
                var context = isSignal ? root : ContextOf(root, views[route.Source]);
                if (isSignal && !UIAuthoring.IsSignalKind(route.Kind))
                {
                    issues.Add(new UIIssue(UIIssueCode.RouteSignalKindInvalid,
                        $"路由「{route.Source}/{route.Action}」是「任何畫面」的訊號路由，種類只能是 ShowPage、" +
                        $"OpenPopup 或 Emit（目前是 {route.Kind}）。", context: root));
                }
                if (!UIAuthoring.AcceptsHistory(route.Kind, route.History))
                {
                    issues.Add(new UIIssue(UIIssueCode.RouteHistoryInvalid,
                        $"路由「{route.Source}/{route.Action}」是 {route.Kind}，history 不能是 {route.History}。" +
                        "OpenPopup 只能是 Push（可以返回）或 Clear（不可返回）。", sourceId, context: context));
                }
                if (UIRoute.TargetsEntry(route.Kind) &&
                    (string.IsNullOrEmpty(route.Target) || !views.TryGetValue(route.Target, out var target) ||
                     target.Kind != (route.Kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup)))
                {
                    var expected = route.Kind == UIRouteKind.ShowPage ? "頁面" : "Popup";
                    issues.Add(new UIIssue(UIIssueCode.RouteTargetMissing,
                        $"路由「{route.Source}/{route.Action}」的目的地「{route.Target}」不存在，或不是{expected}。",
                        sourceId, context: context));
                }
            }

            foreach (var entry in views.Values)
            {
                if (root.Mode == UIAuthoringMode.Prefab || entry.Kind == UIEntryKind.Popup)
                    CheckBindings(root, entry, entry.Prefab, routes, issues, true);
                if (!entry.Review)
                    continue;
                var elementIds = new HashSet<string>();
                foreach (var element in UIPlan.ElementsOf(entry.Review))
                {
                    if (string.IsNullOrEmpty(element.Id) || !elementIds.Add(element.Id))
                    {
                        issues.Add(new UIIssue(UIIssueCode.ElementIdDuplicate,
                            $"「{entry.Id}」裡物件「{element.name}」的元件 ID 是空的，或和其他元件重複（「{element.Id}」）。",
                            entry.Id, element.Id, element));
                    }
                }
                CheckBindings(root, entry, entry.Review, routes, issues, false);
            }

            CheckBlueprint(root, views, issues);
            CheckInput(root, issues);
            CheckWidgets(views.Values, issues);
            UIIdConstants.Check(root, scenePath, checkConstantsFile, issues);
            return issues;
        }

        // 在 Console 點擊問題時定位的物件：有審核畫面就用它，否則是工作區。
        private static Object ContextOf(UIRoot root, UIEntry entry)
        {
            return entry != null && entry.Review ? entry.Review : root;
        }

        // 物件所屬、而且登記在這個畫面上的元件 ID。沒有登記時是 null。
        internal static string ElementIdOf(Component component, UIView view)
        {
            var element = component ? component.GetComponentInParent<UIElementId>(true) : null;
            return element && view && element.transform.IsChildOf(view.transform) ? element.Id : null;
        }

        private static void CheckViewOptions(UIRoot root, UIEntry entry, UIView view, string where,
            List<UIIssue> issues)
        {
            if (!view)
                return;

            var context = ContextOf(root, entry);
            var selected = view.DefaultSelected;
            if (!ReferenceEquals(selected, null) && !selected)
            {
                issues.Add(new UIIssue(UIIssueCode.ViewDefaultSelectedMissing,
                    $"「{entry.Id}」{where}的預設選取元件已經遺失，請重新指定或清除。", entry.Id, context: context));
            }
            else if (selected && !selected.transform.IsChildOf(view.transform))
            {
                issues.Add(new UIIssue(UIIssueCode.ViewDefaultSelectedOutside,
                    $"「{entry.Id}」{where}的預設選取元件「{selected.name}」不在這個畫面裡。", entry.Id,
                    context: context));
            }

            if (entry.Kind == UIEntryKind.Page && (view.ReusesInstance || view.ClosesOnOverlayClick))
            {
                issues.Add(new UIIssue(UIIssueCode.ViewPopupOptionOnPage,
                    $"「{entry.Id}」{where}是頁面，卻開啟了只對 Popup 有意義的選項" +
                    "（關閉後保留並重用、點遮罩關閉）。請在 Inspector 關閉。", entry.Id, context: context));
            }
            if (entry.Kind == UIEntryKind.Popup && view.ClosesOnOverlayClick && !view.Overlay)
            {
                issues.Add(new UIIssue(UIIssueCode.ViewOverlayMissing,
                    $"「{entry.Id}」{where}設定了點遮罩關閉，但沒有遮罩物件。", entry.Id, context: context));
            }
        }

        private static void CheckWidgets(IEnumerable<UIEntry> entries, List<UIIssue> issues)
        {
            var sets = UIWidgets.FindAll();
            if (sets.Count == 0)
            {
                issues.Add(new UIIssue(UIIssueCode.WidgetsSetMissing,
                    "專案沒有元件集。請執行 Tools → UniGameUI → Install Starter Widgets。"));
                return;
            }
            if (sets.Count > 1)
            {
                issues.Add(new UIIssue(UIIssueCode.WidgetsSetMultiple, UIWidgets.MultipleSetsMessage(sets),
                    context: sets[0]));
                return;
            }

            var set = sets[0];
            var names = new HashSet<string>();
            foreach (var widget in set.Widgets)
            {
                if (widget == null || string.IsNullOrEmpty(widget.Name) || !names.Add(widget.Name))
                {
                    issues.Add(new UIIssue(UIIssueCode.WidgetsNameInvalid,
                        $"元件集裡有空白或重複的名稱「{widget?.Name}」。", context: set));
                    continue;
                }
                if (!widget.Prefab)
                {
                    issues.Add(new UIIssue(UIIssueCode.WidgetsPrefabMissing,
                        $"元件集的「{widget.Name}」沒有 Prefab。" +
                        "請重新指定，內建元件可以用 Install Starter Widgets 補回。", context: set));
                    continue;
                }
                var missing = UIWidgets.MissingRequirement(widget.Kind, widget.Prefab);
                if (missing != null)
                {
                    issues.Add(new UIIssue(UIIssueCode.WidgetsComponentMissing,
                        $"元件集的「{widget.Name}」比照 {widget.Kind}，但 {missing}", context: widget.Prefab));
                }
            }

            foreach (var entry in entries)
            {
                if (!entry.Review)
                    continue;
                foreach (var element in UIPlan.ElementsOf(entry.Review))
                {
                    if (!string.IsNullOrEmpty(element.Kind) && !names.Contains(element.Kind))
                    {
                        issues.Add(new UIIssue(UIIssueCode.ElementKindUnknown,
                            $"「{entry.Id}/{element.Id}」記錄的元件名稱「{element.Kind}」" +
                            $"不在元件集裡。可用的名稱：{string.Join("、", names.OrderBy(name => name))}。",
                            entry.Id, element.Id, element));
                    }
                }
            }
        }

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
                issues.Add(new UIIssue(UIIssueCode.InputEventSystemMultiple,
                    $"工作區所在的 Scene「{SceneLabel(scene)}」有 {own.Length} 個啟用的 EventSystem" +
                    $"（{Names(own)}），請只保留一個。", context: own[1]));
            }
            else if (all.Length == 0)
            {
                issues.Add(new UIIssue(UIIssueCode.InputEventSystemMissing,
                    "載入的 Scene 裡沒有 EventSystem。建立工作區時會自動加上；被刪掉時請在工作區的 Scene " +
                    "或執行期一起載入的共用 Scene 加回一個，輸入模組使用 Input System UI Input Module。",
                    context: root));
            }
            else if (active.Length == 0)
            {
                issues.Add(new UIIssue(UIIssueCode.InputEventSystemDisabled,
                    $"載入的 Scene 裡有 EventSystem（{Names(all)}），但沒有啟用。" +
                    "請啟用它的 GameObject 與 EventSystem 元件。", context: all[0]));
            }
            else if (own.Length == 1 && others.Length > 0)
            {
                issues.Add(new UIIssue(UIIssueCode.InputEventSystemShared,
                    $"工作區的 Scene「{SceneLabel(scene)}」和其他載入的 Scene 都有啟用的 EventSystem" +
                    $"（{Names(others)}）。執行期同時載入這些 Scene 時只能留一個。", context: others[0]));
            }
            else if (own.Length == 0 && others.Length > 1)
            {
                issues.Add(new UIIssue(UIIssueCode.InputEventSystemShared,
                    $"工作區的 Scene「{SceneLabel(scene)}」沒有 EventSystem，其他載入的 Scene 有 " +
                    $"{others.Length} 個啟用的 EventSystem（{Names(others)}）。執行期同時載入這些 Scene 時只能留一個。",
                    context: others[0]));
            }

            foreach (var eventSystem in used)
            {
                if (eventSystem.GetComponent<BaseInputModule>() is InputSystemUIInputModule)
                    continue;
                var where = eventSystem.gameObject.scene == scene
                    ? ""
                    : $"（在 Scene「{SceneLabel(eventSystem.gameObject.scene)}」）";
                issues.Add(new UIIssue(UIIssueCode.InputModuleInvalid,
                    $"EventSystem「{eventSystem.name}」{where}的輸入模組必須是 InputSystemUIInputModule。" +
                    "請移除其他輸入模組，再加上 Input System UI Input Module。", context: eventSystem));
            }

            if (ActiveInputHandling() == 0)
            {
                issues.Add(new UIIssue(UIIssueCode.InputHandlingLegacy,
                    "Player Settings 的 Active Input Handling 只啟用舊的 Input Manager。" +
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

        private static void CheckBindings(UIRoot root, UIEntry entry, UIView view,
            HashSet<(string, string)> routes, List<UIIssue> issues, bool isPrefab)
        {
            if (!view)
                return;
            foreach (var binding in view.GetComponentsInChildren<UIButtonAction>(true))
            {
                if (binding.GetComponentInParent<UIView>(true) != view || routes.Contains((entry.Id, binding.Action)))
                    continue;
                var message = $"「{entry.Id}/{binding.name}」的動作 '{binding.Action}' 找不到路由。";
                issues.Add(new UIIssue(UIIssueCode.ButtonActionUnrouted,
                    isPrefab ? $"Prefab 本體中{message}請修正審核實例的綁定並套用覆寫。" : message,
                    entry.Id, ElementIdOf(binding, view), isPrefab ? ContextOf(root, entry) : binding));
            }
        }

        private static void CheckTransitions(UIRoot root, UIEntry entry, UIView view, string where,
            List<UIIssue> issues)
        {
            if (!view)
                return;
#if DOTWEEN
            if ((view.Enter.Preset == UITransitionPreset.Custom || view.Exit.Preset == UITransitionPreset.Custom) &&
                !view.GetComponent<UIDOTweenAnimation>())
            {
                issues.Add(new UIIssue(UIIssueCode.ViewAnimationMissing,
                    $"「{entry.Id}」{where}的動畫選了 Custom，但沒有掛 UIDOTweenAnimation 的元件。",
                    entry.Id, context: ContextOf(root, entry)));
            }
#endif
        }
    }
}
