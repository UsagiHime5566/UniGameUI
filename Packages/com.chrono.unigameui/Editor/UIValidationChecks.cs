using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace UniGameUI.Editor
{
    // 0.5.0 新增的檢查。它們指出的是「看起來不對」，專案可能有正當的理由，所以都是 Warning，
    // 而且每一項都有不算的情況。「由程式控制」指藍圖上的標記，或根節點掛了 UIConfirmPopup 的畫面。
    public static partial class UIValidation
    {
        private const int MaxListedCharacters = 10;

        private static void CheckBlueprint(UIRoot root, Dictionary<string, UIEntry> views, List<UIIssue> issues)
        {
            var byCode = new HashSet<string>(views.Values.Where(UIAuthoring.IsNavigatedByCode)
                .Select(entry => entry.Id));
            var pages = views.Values.Where(entry => entry.Kind == UIEntryKind.Page).ToArray();
            var routes = root.Routes.Where(route => route != null && !string.IsNullOrEmpty(route.Source) &&
                views.ContainsKey(route.Source)).ToArray();
            // 「任何畫面」的訊號路由：目標算走得到，但它們不是任何畫面的出口。
            var signalTargets = root.Routes.Where(route => route != null && route.IsSignal &&
                    UIRoute.TargetsEntry(route.Kind) && !string.IsNullOrEmpty(route.Target) &&
                    views.ContainsKey(route.Target))
                .Select(route => route.Target).ToArray();

            CheckReachability(root, views, pages, routes, byCode, signalTargets, issues);
            CheckExits(root, views, pages, routes, byCode, issues);
            foreach (var entry in views.Values)
            {
                if (!entry.Review)
                    continue;
                if (!byCode.Contains(entry.Id))
                    CheckButtons(entry, issues);
                CheckGlyphs(entry, issues);
            }
        }

        private static void CheckReachability(UIRoot root, Dictionary<string, UIEntry> views, UIEntry[] pages,
            UIRoute[] routes, HashSet<string> byCode, string[] signalTargets, List<UIIssue> issues)
        {
            if (string.IsNullOrEmpty(root.StartPageId))
            {
                // 頁面全部由程式顯示時，沒有起始頁是正常的。
                if (pages.Any(page => !byCode.Contains(page.Id)))
                {
                    issues.Add(new UIIssue(UIIssueCode.StartNone,
                        "工作區沒有設定起始頁，載入 Scene 之後不會顯示任何頁面。請用計畫的 startPage 或藍圖指定；" +
                        "頁面全部由程式顯示時，把它們標記為由程式控制（計畫的 \"navigation\": \"Code\"）。",
                        context: root));
                }
                return;
            }
            if (!views.TryGetValue(root.StartPageId, out var start) || start.Kind != UIEntryKind.Page)
                return;

            // 由程式控制的畫面是程式打開的，訊號路由的目標是訊號帶過去的，所以它們和起始頁一樣是出發點。
            var reached = new HashSet<string>(byCode) { start.Id };
            reached.UnionWith(signalTargets);
            var pending = new Queue<string>(reached);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var route in routes)
                {
                    if (route.Source == current && UIRoute.TargetsEntry(route.Kind) &&
                        !string.IsNullOrEmpty(route.Target) && views.ContainsKey(route.Target) &&
                        reached.Add(route.Target))
                        pending.Enqueue(route.Target);
                }
            }

            foreach (var entry in views.Values)
            {
                if (reached.Contains(entry.Id))
                    continue;
                var kind = entry.Kind == UIEntryKind.Popup ? "Popup" : "頁面";
                issues.Add(new UIIssue(UIIssueCode.ViewUnreachable,
                    $"{kind}「{entry.Id}」從起始頁「{start.Id}」沿著 ShowPage 與 OpenPopup 路由走不到，" +
                    "也不是訊號路由的目標。" +
                    "請補上通往它的路由；由程式開啟的畫面請標記為由程式控制（計畫的 \"navigation\": \"Code\"）。",
                    entry.Id, context: ContextOf(root, entry)));
            }
        }

        private static void CheckExits(UIRoot root, Dictionary<string, UIEntry> views, UIEntry[] pages,
            UIRoute[] routes, HashSet<string> byCode, List<UIIssue> issues)
        {
            foreach (var entry in views.Values)
            {
                if (byCode.Contains(entry.Id))
                    continue;

                var own = routes.Where(route => route.Source == entry.Id).ToArray();
                if (entry.Kind == UIEntryKind.Page)
                {
                    // 只有一個頁面的工作區沒有別的地方可以去。
                    // 頁面自己沒有 ShowPage 或 Back，但它開得到的 Popup 有（例如暫停選單的「回標題」），也離得開。
                    if (pages.Length == 1 ||
                        own.Any(route => route.Kind is UIRouteKind.ShowPage or UIRouteKind.Back) ||
                        OpensPopupThatLeaves(entry.Id, routes, views))
                        continue;
                    issues.Add(new UIIssue(UIIssueCode.ViewNoExit,
                        $"頁面「{entry.Id}」沒有任何 ShowPage 或 Back 路由，它開得到的 Popup 也沒有切到別頁的 " +
                        "ShowPage 路由，進來之後離不開。" +
                        "請補上路由；由程式切換的頁面請標記為由程式控制（計畫的 \"navigation\": \"Code\"）。",
                        entry.Id, context: ContextOf(root, entry)));
                    continue;
                }

                // Popup 執行期用的是 Prefab；沒有 Prefab 時另有 Error，這裡用審核畫面判斷。
                var view = entry.Prefab ? entry.Prefab : entry.Review;
                if (own.Any(route => route.Kind is UIRouteKind.ClosePopup or UIRouteKind.Back or UIRouteKind.ShowPage) ||
                    (view && view.ClosesOnOverlayClick))
                    continue;
                issues.Add(new UIIssue(UIIssueCode.ViewNoExit,
                    $"Popup「{entry.Id}」沒有任何 ClosePopup、Back 或 ShowPage 路由，也沒有設定點遮罩關閉，" +
                    "開啟之後關不掉。請補上路由；由程式關閉的 Popup 請標記為由程式控制（計畫的 \"navigation\": \"Code\"）。",
                    entry.Id, context: ContextOf(root, entry)));
            }
        }

        // 從這個頁面沿著 OpenPopup 路由開得到的 Popup（Popup 再開的 Popup 也算）裡，
        // 有沒有切到別的頁面的 ShowPage 路由。切回這個頁面本身不算離開。
        private static bool OpensPopupThatLeaves(string pageId, UIRoute[] routes, Dictionary<string, UIEntry> views)
        {
            var seen = new HashSet<string> { pageId };
            var pending = new Queue<string>(seen);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var route in routes)
                {
                    if (route.Source != current)
                        continue;
                    if (current != pageId && route.Kind == UIRouteKind.ShowPage && route.Target != pageId)
                        return true;
                    if (route.Kind == UIRouteKind.OpenPopup && !string.IsNullOrEmpty(route.Target) &&
                        views.TryGetValue(route.Target, out var popup) && popup.Kind == UIEntryKind.Popup &&
                        seen.Add(popup.Id))
                        pending.Enqueue(popup.Id);
                }
            }
            return false;
        }

        private static void CheckButtons(UIEntry entry, List<UIIssue> issues)
        {
            var view = entry.Review;
            foreach (var button in view.GetComponentsInChildren<Button>(true))
            {
                // 清單項目裡的按鈕由 UIList 與頁面腳本在執行期接上。
                if (button.GetComponentInParent<UIView>(true) != view ||
                    UIList.IsInsideListItem(button.transform, view.transform))
                    continue;
                if (button.TryGetComponent<UIButtonAction>(out var binding) && !string.IsNullOrEmpty(binding.Action))
                    continue;
                if (HasInspectorListener(button))
                    continue;

                var elementId = ElementIdOf(button, view);
                issues.Add(new UIIssue(UIIssueCode.ButtonUnbound,
                    $"「{entry.Id}」的按鈕{Describe(elementId, button)}沒有動作，onClick 也沒有在 Inspector 接監聽，" +
                    "按了不會有反應。標準做法是給按鈕 action 並接一條路由；要由程式處理時用 Emit 路由，" +
                    "在頁面腳本的 OnAction 接收。",
                    entry.Id, elementId, button));
            }
        }

        private static bool HasInspectorListener(Button button)
        {
            var onClick = button.onClick;
            for (var index = 0; index < onClick.GetPersistentEventCount(); index++)
            {
                if (onClick.GetPersistentTarget(index) && !string.IsNullOrEmpty(onClick.GetPersistentMethodName(index)))
                    return true;
            }
            return false;
        }

        private static string Describe(string elementId, Component component)
        {
            return string.IsNullOrEmpty(elementId) ? $"物件「{component.name}」" : $"「{elementId}」";
        }

        // 只檢查預設內容：Scene 裡寫在元件上的文字與下拉選單的選項。多語系翻譯後的文字不在範圍內。
        private static void CheckGlyphs(UIEntry entry, List<UIIssue> issues)
        {
            var view = entry.Review;
            var glyphs = new GlyphLookup();
            foreach (var text in view.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<UIView>(true) != view ||
                    UIList.IsInsideListItem(text.transform, view.transform))
                    continue;
                ReportMissingGlyphs(entry, text, text.font, text.text, "文字", glyphs, issues);
            }
            foreach (var dropdown in view.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                if (dropdown.GetComponentInParent<UIView>(true) != view ||
                    UIList.IsInsideListItem(dropdown.transform, view.transform))
                    continue;
                // 選項展開時用的是項目文字的字型。
                var label = dropdown.itemText ? dropdown.itemText : dropdown.captionText;
                var options = string.Concat(dropdown.options.Select(option => option?.text ?? ""));
                ReportMissingGlyphs(entry, dropdown, label ? label.font : null, options, "選項", glyphs, issues);
            }
        }

        private static void ReportMissingGlyphs(UIEntry entry, Component owner, TMP_FontAsset font, string content,
            string what, GlyphLookup glyphs, List<UIIssue> issues)
        {
            if (string.IsNullOrEmpty(content))
                return;
            if (!font)
                font = GlyphLookup.DefaultFont;
            if (!font)
                return;

            var missing = new List<int>();
            for (var index = 0; index < content.Length; index++)
            {
                int unicode = content[index];
                if (char.IsHighSurrogate(content[index]) && index + 1 < content.Length &&
                    char.IsLowSurrogate(content[index + 1]))
                {
                    unicode = char.ConvertToUtf32(content[index], content[index + 1]);
                    index++;
                }
                else if (char.IsSurrogate(content[index]))
                {
                    continue;
                }
                if (IsInvisible(unicode) || missing.Contains(unicode) || glyphs.Has(font, (uint)unicode))
                    continue;
                missing.Add(unicode);
            }
            if (missing.Count == 0)
                return;

            var listed = new StringBuilder();
            foreach (var unicode in missing.Take(MaxListedCharacters))
                listed.Append('「').Append(char.ConvertFromUtf32(unicode)).Append('」');
            if (missing.Count > MaxListedCharacters)
                listed.Append($"等 {missing.Count} 個");

            var elementId = ElementIdOf(owner, entry.Review);
            issues.Add(new UIIssue(UIIssueCode.TextMissingGlyph,
                $"「{entry.Id}」的{Describe(elementId, owner)}，{what}裡有字型「{font.name}」和它的 fallback 都沒有的字元：" +
                $"{listed}。請設定含有這些字元的預設字型（計畫的 defaultFontPath 或 UIWidgets.SetDefaultFont），" +
                "或修改預設文字。",
                entry.Id, elementId, owner));
        }

        // 空白、控制字元與格式字元（例如零寬空格）本來就不畫出來。
        private static bool IsInvisible(int unicode)
        {
            var text = char.ConvertFromUtf32(unicode);
            if (char.IsWhiteSpace(text, 0) || char.IsControl(text, 0))
                return true;
            var category = char.GetUnicodeCategory(text, 0);
            return category is System.Globalization.UnicodeCategory.Format
                or System.Globalization.UnicodeCategory.Surrogate;
        }

        /// <summary>
        /// 判斷字型畫不畫得出一個字元，順序和 TMP 相同：字型本身、它的 fallback、TMP Settings 的全域 fallback，
        /// 最後是 TMP 的預設字型。**不會改動任何字型資產**：靜態字型只看已經在圖集裡的字元；
        /// 動態字型直接問來源字型檔有沒有這個字，不把它加進圖集。
        /// </summary>
        private sealed class GlyphLookup
        {
            private readonly Dictionary<(int Font, uint Unicode), bool> own =
                new Dictionary<(int, uint), bool>();
            private readonly HashSet<int> visited = new HashSet<int>();
            private readonly bool hasSettings = HasSettings;
            private int loadedFace;
            private bool loadedFaceIsValid;

            public static TMP_FontAsset DefaultFont => HasSettings ? TMP_Settings.defaultFontAsset : null;

            // TMP 的資源還沒匯入時，直接讀 TMP_Settings 會跳出匯入視窗。
            private static bool HasSettings => Resources.Load<TMP_Settings>("TMP Settings");

            public bool Has(TMP_FontAsset font, uint unicode)
            {
                visited.Clear();
                if (HasWithFallbacks(font, unicode))
                    return true;
                if (!hasSettings)
                    return false;

                var global = TMP_Settings.fallbackFontAssets;
                if (global != null && global.Any(fallback => HasWithFallbacks(fallback, unicode)))
                    return true;
                return HasWithFallbacks(TMP_Settings.defaultFontAsset, unicode);
            }

            private bool HasWithFallbacks(TMP_FontAsset font, uint unicode)
            {
                if (!font || !visited.Add(font.GetInstanceID()))
                    return false;
                if (HasOwn(font, unicode))
                    return true;
                var fallbacks = font.fallbackFontAssetTable;
                return fallbacks != null && fallbacks.Any(fallback => HasWithFallbacks(fallback, unicode));
            }

            private bool HasOwn(TMP_FontAsset font, uint unicode)
            {
                var key = (font.GetInstanceID(), unicode);
                if (own.TryGetValue(key, out var known))
                    return known;

                var lookup = font.characterLookupTable;
                var found = lookup != null && lookup.ContainsKey(unicode);
                if (!found && font.atlasPopulationMode != AtlasPopulationMode.Static)
                    found = LoadFace(font) && FontEngine.TryGetGlyphIndex(unicode, out var glyph) && glyph != 0;
                own[key] = found;
                return found;
            }

            // 字型引擎一次只載入一個字型。這裡只是讀取字型檔，不會動到字型資產。
            private bool LoadFace(TMP_FontAsset font)
            {
                var id = font.GetInstanceID();
                if (loadedFace == id)
                    return loadedFaceIsValid;

                FontEngine.InitializeFontEngine();
                var face = font.faceInfo;
                bool loaded;
                if (font.atlasPopulationMode == AtlasPopulationMode.DynamicOS)
                {
                    loaded = FontEngine.LoadFontFace(face.familyName, face.styleName, face.pointSize) ==
                        FontEngineError.Success;
                }
                else
                {
                    var source = SourceFont(font);
                    loaded = source && FontEngine.LoadFontFace(source, face.pointSize, FaceIndex(font)) ==
                        FontEngineError.Success;
                }
                loadedFace = id;
                loadedFaceIsValid = loaded;
                return loaded;
            }

            // 字型檔裡有多個字體時用的索引。它不是公開的屬性，從序列化的資料讀。
            private static int FaceIndex(TMP_FontAsset font)
            {
                var index = new UnityEditor.SerializedObject(font).FindProperty("m_FaceInfo.m_FaceIndex");
                return index != null ? index.intValue : 0;
            }

            // Editor 裡來源字型有時只記在 Editor 專用的欄位，讀的時候不寫回字型資產。
            private static Font SourceFont(TMP_FontAsset font)
            {
                if (font.sourceFontFile)
                    return font.sourceFontFile;
                var serialized = new UnityEditor.SerializedObject(font);
                var reference = serialized.FindProperty("m_SourceFontFile_EditorRef");
                if (reference != null && reference.objectReferenceValue is Font editorFont)
                    return editorFont;
                var guid = serialized.FindProperty("m_SourceFontFileGUID");
                if (guid == null || string.IsNullOrEmpty(guid.stringValue))
                    return null;
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guid.stringValue));
            }
        }
    }
}
