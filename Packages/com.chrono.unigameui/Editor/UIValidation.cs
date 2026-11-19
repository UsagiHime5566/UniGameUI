using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
                foreach (var element in entry.Review.GetComponentsInChildren<UIElementId>(true))
                {
                    if (string.IsNullOrEmpty(element.Id) || !elementIds.Add(element.Id))
                        issues.Add(Error($"{entry.Id}: duplicate or empty element ID on {element.name}."));
                }
                CheckBindings(entry, entry.Review, routes, issues, false);
            }

            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length != 1 || !eventSystems[0].GetComponent<BaseInputModule>())
                issues.Add(Warning("Use one active EventSystem with an input module in the loaded scenes."));
            UIIdConstants.Check(root, scenePath, checkConstantsFile, issues);
            return issues;
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
