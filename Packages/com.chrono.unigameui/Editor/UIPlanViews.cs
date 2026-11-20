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
        private enum BackKeyMode { Back, Ignore }
        private enum PopupCache { Recreate, Reuse }
        private enum OverlayClick { Ignore, Close }
        private enum NavigationMode { Routes, Code }

        // registered：這個工作區已經登記過這個畫面。登記過的畫面要設 updateExisting 才會寫入選項。
        private static void PreflightViewOptions(ViewSpec view, UIEntryKind kind, UIView existingReview,
            bool registered, WidgetCatalog catalog)
        {
            PreflightDefaultSelected(view, existingReview, catalog);
            Parse(view.backKey, BackKeyMode.Back);
            Parse(view.navigation, NavigationMode.Routes);
            foreach (var (field, value) in new[] { ("popupCache", view.popupCache), ("overlayClick", view.overlayClick) })
            {
                if (value != null && kind != UIEntryKind.Popup)
                    throw new ArgumentException($"畫面「{view.id}」是頁面，{field} 欄位只適用於 Popup。");
            }
            Parse(view.popupCache, PopupCache.Recreate);
            // 新建立的 Popup 一定有遮罩；既有的 Popup 沒有遮罩時無法設成 Close。
            if (Parse(view.overlayClick, OverlayClick.Ignore) == OverlayClick.Close && existingReview &&
                (!registered || view.updateExisting) && !FindOverlay(existingReview))
                throw new ArgumentException(MissingOverlay(view.id));
        }

        // 0.4.0 之前建立的 Popup 沒有記錄遮罩，依名稱尋找。
        private static RectTransform FindOverlay(UIView view)
        {
            return view.overlay ? view.overlay : view.transform.Find("Overlay") as RectTransform;
        }

        private static string MissingOverlay(string viewId)
        {
            return $"畫面「{viewId}」沒有遮罩物件，無法把 overlayClick 設成 Close。";
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
            if (spec.defaultSelected == null && string.IsNullOrEmpty(spec.backKey) &&
                string.IsNullOrEmpty(spec.popupCache) && string.IsNullOrEmpty(spec.overlayClick))
                return;

            Undo.RecordObject(view, "Set UI view options");
            if (spec.defaultSelected != null)
            {
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
                view.defaultSelected = target;
            }
            if (!string.IsNullOrEmpty(spec.backKey))
                view.blocksBackKey = Parse(spec.backKey, BackKeyMode.Back) == BackKeyMode.Ignore;
            if (!string.IsNullOrEmpty(spec.popupCache))
                view.reusesInstance = Parse(spec.popupCache, PopupCache.Recreate) == PopupCache.Reuse;
            if (!string.IsNullOrEmpty(spec.overlayClick))
            {
                var closes = Parse(spec.overlayClick, OverlayClick.Ignore) == OverlayClick.Close;
                if (closes && !view.overlay)
                    view.overlay = FindOverlay(view);
                if (closes && !view.overlay)
                    throw new ArgumentException(MissingOverlay(view.Id));
                view.closesOnOverlayClick = closes;
            }
            UIAuthoring.RecordOverride(view);
        }
    }
}
