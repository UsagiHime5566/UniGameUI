using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI.Editor
{
    // 計畫裡 View 層級的選項。和背景、動畫一樣，只在新建 View 或 View 設了 updateExisting 時寫入。
    public static partial class UIPlan
    {
        private static void PreflightViewOptions(ViewSpec view, UIEntryKind kind, UIView existingReview,
            WidgetCatalog catalog)
        {
            PreflightDefaultSelected(view, existingReview, catalog);
        }

        private static void PreflightDefaultSelected(ViewSpec view, UIView existingReview, WidgetCatalog catalog)
        {
            // null 是沒寫，空字串是清除。
            if (string.IsNullOrEmpty(view.defaultSelected))
                return;

            var id = view.defaultSelected;
            var existing = existingReview ? FindElement(existingReview, id) : null;
            var planned = (view.elements ?? Array.Empty<ElementSpec>()).FirstOrDefault(element => element.id == id);
            bool selectable;
            if (existing)
            {
                selectable = existing.GetComponentInChildren<Selectable>(true);
            }
            else if (planned != null)
            {
                catalog.TryGet(KindName(planned), out var kind, out var prefab);
                // 還沒安裝的內建元件沒有 Prefab 可以檢查，依行為類別判斷。
                selectable = prefab
                    ? prefab.GetComponentInChildren<Selectable>(true)
                    : kind is UIWidgetKind.Button or UIWidgetKind.Toggle or UIWidgetKind.Slider
                        or UIWidgetKind.Dropdown or UIWidgetKind.InputField;
            }
            else
            {
                throw new ArgumentException($"畫面「{view.id}」的 defaultSelected 指到不存在的元件「{id}」。");
            }
            if (!selectable)
            {
                throw new ArgumentException(
                    $"畫面「{view.id}」的 defaultSelected 指到的元件「{id}」不能被選取（沒有 Selectable）。");
            }
        }

        private static void ApplyViewOptions(UIView view, ViewSpec spec)
        {
            if (spec.defaultSelected == null)
                return;

            Selectable target = null;
            if (spec.defaultSelected.Length > 0)
            {
                var element = FindElement(view, spec.defaultSelected);
                target = element ? element.GetComponentInChildren<Selectable>(true) : null;
                if (!target)
                {
                    throw new ArgumentException(
                        $"畫面「{view.Id}」的 defaultSelected 指到的元件「{spec.defaultSelected}」不能被選取。");
                }
            }
            Undo.RecordObject(view, "Set UI view options");
            view.defaultSelected = target;
            UIAuthoring.RecordOverride(view);
        }
    }
}
