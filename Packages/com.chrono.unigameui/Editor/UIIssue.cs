using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor
{
    public enum UIIssueSeverity { Warning, Error }

    /// <summary>
    /// Validate 回報的一筆問題。用 <see cref="Code"/> 判斷種類，訊息文字是給人看的，可能會調整。
    /// </summary>
    public readonly struct UIIssue
    {
        public UIIssueSeverity Severity { get; }

        /// <summary>穩定的問題代碼，全部列在 <see cref="UIIssueCode"/>。</summary>
        public string Code { get; }

        public string Message { get; }

        /// <summary>相關的畫面 ID。和畫面無關時是 null。</summary>
        public string ViewId { get; }

        /// <summary>相關的元件 ID。和元件無關，或那個物件沒有登記 ID 時是 null。</summary>
        public string ElementId { get; }

        // 在 Console 點擊這筆問題時要定位的物件。
        internal Object Context { get; }

        internal UIIssue(string code, string message, string viewId = null, string elementId = null,
            Object context = null)
        {
            Severity = UIIssueCode.SeverityOf(code);
            Code = code;
            Message = message;
            ViewId = string.IsNullOrEmpty(viewId) ? null : viewId;
            ElementId = string.IsNullOrEmpty(elementId) ? null : elementId;
            Context = context;
        }

        public override string ToString() => $"{Severity} [{Code}] {Message}";
    }

    /// <summary>
    /// Validate 的問題代碼。代碼以「主題.問題」命名，定下來之後不會更改；每個代碼的嚴重度是固定的。
    /// </summary>
    public static class UIIssueCode
    {
        public const string WorkspaceMissing = "workspace.missing";
        public const string DOTweenNotReady = "dotween.not-ready";

        public const string ViewIdInvalid = "view.id-invalid";
        public const string ViewReviewMissing = "view.review-missing";
        public const string ViewPrefabMissing = "view.prefab-missing";
        public const string ViewOverridesPending = "view.overrides-pending";
        public const string ViewCanvasMissing = "view.canvas-missing";
        public const string ViewAnimationMissing = "view.animation-missing";
        public const string ViewDefaultSelectedMissing = "view.default-selected-missing";
        public const string ViewDefaultSelectedOutside = "view.default-selected-outside";
        public const string ViewPopupOptionOnPage = "view.popup-option-on-page";
        public const string ViewOverlayMissing = "view.overlay-missing";

        public const string StartUnknown = "start.unknown";

        public const string RouteInvalid = "route.invalid";
        public const string RouteTargetMissing = "route.target-missing";

        public const string ElementIdDuplicate = "element.id-duplicate";
        public const string ElementKindUnknown = "element.kind-unknown";
        public const string ButtonActionUnrouted = "button.action-unrouted";

        public const string InputEventSystemMultiple = "input.event-system-multiple";
        public const string InputEventSystemMissing = "input.event-system-missing";
        public const string InputEventSystemDisabled = "input.event-system-disabled";
        public const string InputEventSystemShared = "input.event-system-shared";
        public const string InputModuleInvalid = "input.module-invalid";
        public const string InputHandlingLegacy = "input.handling-legacy";

        public const string WidgetsSetMissing = "widgets.set-missing";
        public const string WidgetsSetMultiple = "widgets.set-multiple";
        public const string WidgetsNameInvalid = "widgets.name-invalid";
        public const string WidgetsPrefabMissing = "widgets.prefab-missing";
        public const string WidgetsComponentMissing = "widgets.component-missing";

        public const string ConstantsFileOccupied = "constants.file-occupied";
        public const string ConstantsNameConflict = "constants.name-conflict";
        public const string ConstantsSceneConflict = "constants.scene-conflict";
        public const string ConstantsSceneUnsaved = "constants.scene-unsaved";
        public const string ConstantsOutdated = "constants.outdated";

        private static readonly Dictionary<string, UIIssueSeverity> Severities =
            new Dictionary<string, UIIssueSeverity>(StringComparer.Ordinal)
            {
                [WorkspaceMissing] = UIIssueSeverity.Error,
                [DOTweenNotReady] = UIIssueSeverity.Error,
                [ViewIdInvalid] = UIIssueSeverity.Error,
                [ViewReviewMissing] = UIIssueSeverity.Error,
                [ViewPrefabMissing] = UIIssueSeverity.Error,
                [ViewOverridesPending] = UIIssueSeverity.Warning,
                [ViewCanvasMissing] = UIIssueSeverity.Warning,
                [ViewAnimationMissing] = UIIssueSeverity.Error,
                [ViewDefaultSelectedMissing] = UIIssueSeverity.Error,
                [ViewDefaultSelectedOutside] = UIIssueSeverity.Error,
                [ViewPopupOptionOnPage] = UIIssueSeverity.Error,
                [ViewOverlayMissing] = UIIssueSeverity.Error,
                [StartUnknown] = UIIssueSeverity.Error,
                [RouteInvalid] = UIIssueSeverity.Error,
                [RouteTargetMissing] = UIIssueSeverity.Error,
                [ElementIdDuplicate] = UIIssueSeverity.Error,
                [ElementKindUnknown] = UIIssueSeverity.Error,
                [ButtonActionUnrouted] = UIIssueSeverity.Error,
                [InputEventSystemMultiple] = UIIssueSeverity.Error,
                [InputEventSystemMissing] = UIIssueSeverity.Error,
                [InputEventSystemDisabled] = UIIssueSeverity.Error,
                [InputEventSystemShared] = UIIssueSeverity.Warning,
                [InputModuleInvalid] = UIIssueSeverity.Error,
                [InputHandlingLegacy] = UIIssueSeverity.Error,
                [WidgetsSetMissing] = UIIssueSeverity.Error,
                [WidgetsSetMultiple] = UIIssueSeverity.Error,
                [WidgetsNameInvalid] = UIIssueSeverity.Error,
                [WidgetsPrefabMissing] = UIIssueSeverity.Error,
                [WidgetsComponentMissing] = UIIssueSeverity.Error,
                [ConstantsFileOccupied] = UIIssueSeverity.Error,
                [ConstantsNameConflict] = UIIssueSeverity.Error,
                [ConstantsSceneConflict] = UIIssueSeverity.Error,
                [ConstantsSceneUnsaved] = UIIssueSeverity.Warning,
                [ConstantsOutdated] = UIIssueSeverity.Warning
            };

        /// <summary>所有的問題代碼。</summary>
        public static IReadOnlyCollection<string> All => Severities.Keys;

        /// <summary>代碼的嚴重度。代碼不存在時丟出例外。</summary>
        public static UIIssueSeverity SeverityOf(string code)
        {
            if (code != null && Severities.TryGetValue(code, out var severity))
                return severity;
            throw new ArgumentException($"「{code}」不是 UniGameUI 的問題代碼。", nameof(code));
        }
    }
}
