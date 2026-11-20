using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI.Editor
{
    /// <summary>
    /// 元件集的入口：尋找、安裝起始元件、登記自訂元件、設定預設字型。
    /// </summary>
    public static class UIWidgets
    {
        public const string DefaultFolder = "Assets/UniGameUI/Widgets";
        private const string SetFileName = "UIWidgetSet.asset";
        private const string ListItemName = "ListItem";
        private const string ConfirmName = "Confirm";

        // 測試用：把安裝位置與搜尋範圍限制在指定資料夾，避免動到專案自己的元件集。
        internal static string FolderOverride;

        // 測試用：換字型處理每個 Prefab 之前呼叫，用來模擬換到一半失敗。
        internal static Action<string> ReplacingFont;

        internal static readonly UIWidgetKind[] BuiltIn =
        {
            UIWidgetKind.Text, UIWidgetKind.Image, UIWidgetKind.Panel, UIWidgetKind.Button, UIWidgetKind.Toggle,
            UIWidgetKind.Slider, UIWidgetKind.ProgressBar, UIWidgetKind.Dropdown, UIWidgetKind.InputField,
            UIWidgetKind.ScrollList
        };

        internal static string Folder => FolderOverride ?? DefaultFolder;

        /// <summary>
        /// 起始確認框 Prefab 的安裝位置。在計畫的 views 用這個路徑登記確認框。
        /// </summary>
        public static string ConfirmPrefabPath => $"{Folder}/{ConfirmName}.prefab";

        /// <summary>
        /// 取得專案的元件集。找不到時回傳 null。專案裡有多個時回傳路徑排序最前面的那一個，Validate 會回報錯誤。
        /// </summary>
        public static UIWidgetSet Find()
        {
            var sets = FindAll();
            return sets.Count == 0 ? null : sets[0];
        }

        internal static List<UIWidgetSet> FindAll()
        {
            var filter = "t:" + nameof(UIWidgetSet);
            string[] guids;
            if (FolderOverride == null)
                guids = AssetDatabase.FindAssets(filter);
            else if (AssetDatabase.IsValidFolder(FolderOverride))
                guids = AssetDatabase.FindAssets(filter, new[] { FolderOverride });
            else
                guids = Array.Empty<string>();

            return guids.Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<UIWidgetSet>).Where(set => set).ToList();
        }

        internal static string MultipleSetsMessage(IEnumerable<UIWidgetSet> sets)
        {
            return "專案裡有超過一個元件集，請只保留一個：" +
                string.Join("、", sets.Select(AssetDatabase.GetAssetPath));
        }

        /// <summary>
        /// 建立元件集並補上缺少的起始元件。已經存在的 Prefab 與項目不會被改動，可以重複執行。
        /// </summary>
        public static UIWidgetSet Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("請在 Edit Mode 安裝起始元件。");

            var sets = FindAll();
            if (sets.Count > 1)
                throw new InvalidOperationException(MultipleSetsMessage(sets));
            var set = sets.Count == 1 ? sets[0] : null;
            if (set && IsComplete(set))
                return set;

            UITmpResources.Ensure();
            var created = new List<string>();
            if (!set)
            {
                var path = $"{Folder}/{SetFileName}";
                if (AssetDatabase.LoadMainAssetAtPath(path))
                    throw new InvalidOperationException($"「{path}」已經被其他資產占用，無法建立元件集。");
                UIAuthoring.EnsureAssetFolder(path);
                set = ScriptableObject.CreateInstance<UIWidgetSet>();
                AssetDatabase.CreateAsset(set, path);
                created.Add(path);
            }

            if (!set.listItem)
            {
                var path = $"{Folder}/{ListItemName}.prefab";
                set.listItem = LoadOrBuild(path, created, () => UIStarterWidgets.BuildListItem(path, set));
            }
            foreach (var kind in BuiltIn)
            {
                var name = kind.ToString();
                var entry = set.Find(name);
                if (entry != null && entry.prefab)
                    continue;
                var path = $"{Folder}/{name}.prefab";
                var prefab = LoadOrBuild(path, created, () => UIStarterWidgets.BuildWidget(kind, path, set));
                if (entry == null)
                    set.widgets.Add(new UIWidgetEntry { name = name, kind = kind, prefab = prefab });
                else
                    entry.prefab = prefab;
            }
            if (!set.confirm)
            {
                // 確認框用到 Text 與 Button 兩個元件，所以放在它們之後。
                var path = ConfirmPrefabPath;
                var prefab = LoadOrBuild(path, created, () => UIStarterWidgets.BuildConfirm(path, set));
                set.confirm = prefab.GetComponent<UIView>();
                if (!set.confirm)
                    throw new InvalidOperationException($"「{path}」不是確認框的 View Prefab，無法登記到元件集。");
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssetIfDirty(set);
            if (created.Count > 0)
                Debug.Log("UniGameUI：已建立起始元件。\n" + string.Join("\n", created), set);
            return set;
        }

        /// <summary>
        /// 登記專案自訂的元件。<paramref name="like"/> 是它比照的內建類別，決定計畫能寫哪些欄位；
        /// <see cref="UIWidgetKind.Custom"/> 只能在計畫裡設定 rect。名稱已存在而且內容相同時不做事，內容不同時丟出例外。
        /// </summary>
        public static void Register(string name, string prefabPath, UIWidgetKind like = UIWidgetKind.Custom)
        {
            var prefab = CheckRegistration(Find(), name, prefabPath, like, out var alreadyRegistered);
            if (alreadyRegistered)
                return;

            var set = Install();
            set.widgets.Add(new UIWidgetEntry { name = name, kind = like, prefab = prefab });
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssetIfDirty(set);
        }

        /// <summary>
        /// 設定元件集的預設字型，並把元件集所有 Prefab 裡使用舊預設字型的文字換成新字型。
        /// 已經被改成其他字型的文字不會被改動。傳入 null 或空字串會改回 TMP 的預設字型。
        /// </summary>
        public static void SetDefaultFont(string fontAssetPath)
        {
            SetDefaultFont(fontAssetPath, out _);
        }

        /// <summary>
        /// 換字型會直接改寫元件 Prefab，Undo 還原不了。換到一半失敗時這裡會把已經改過的 Prefab 寫回原樣；
        /// <paramref name="undo"/> 讓呼叫端在自己後續的步驟失敗時，把這一次的變更整個還原。
        /// </summary>
        internal static void SetDefaultFont(string fontAssetPath, out Action undo)
        {
            var font = LoadFont(fontAssetPath);
            var set = Install();
            var previousDefault = set.defaultFont;
            var previous = previousDefault ? previousDefault : TMP_Settings.defaultFontAsset;
            var next = font ? font : TMP_Settings.defaultFontAsset;
            var backups = new List<(string Path, byte[] Bytes)>();

            void Restore()
            {
                foreach (var (path, bytes) in backups)
                {
                    File.WriteAllBytes(path, bytes);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                backups.Clear();
                if (!set || set.defaultFont == previousDefault)
                    return;
                set.defaultFont = previousDefault;
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssetIfDirty(set);
            }

            undo = Restore;
            try
            {
                if (previous != next && next)
                {
                    foreach (var path in PrefabPaths(set))
                    {
                        var bytes = File.ReadAllBytes(path);
                        if (ReplaceFont(path, previous, next))
                            backups.Add((path, bytes));
                    }
                }

                if (set.defaultFont == font)
                    return;
                set.defaultFont = font;
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssetIfDirty(set);
            }
            catch
            {
                Restore();
                throw;
            }
        }

        internal static TMP_FontAsset LoadFont(string fontAssetPath)
        {
            if (string.IsNullOrEmpty(fontAssetPath))
                return null;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (!font)
                throw new ArgumentException($"「{fontAssetPath}」不是 TMP Font Asset。");
            return font;
        }

        /// <summary>
        /// 檢查一筆登記是否合法，不寫入任何東西。回傳要登記的 Prefab。
        /// </summary>
        internal static GameObject CheckRegistration(UIWidgetSet set, string name, string prefabPath,
            UIWidgetKind like, out bool alreadyRegistered)
        {
            alreadyRegistered = false;
            if (string.IsNullOrWhiteSpace(name) || name.Any(character => !IsNameCharacter(character)))
                throw new ArgumentException($"元件名稱「{name}」只能使用英文字母、數字、底線或連字號。");
            if (!Enum.IsDefined(typeof(UIWidgetKind), like))
                throw new ArgumentException($"元件「{name}」比照的類別「{like}」無法識別。");
            var prefab = string.IsNullOrEmpty(prefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab)
                throw new ArgumentException($"元件「{name}」的 Prefab「{prefabPath}」不存在。");
            var missing = MissingRequirement(like, prefab);
            if (missing != null)
                throw new ArgumentException($"元件「{name}」比照 {like}，但 {missing}");

            var existing = set ? set.Find(name) : null;
            if (existing == null)
            {
                if (IsBuiltInName(name, out _))
                    throw new ArgumentException($"「{name}」是內建元件的名稱，自訂元件請使用其他名稱。");
                return prefab;
            }
            if (existing.prefab != prefab || existing.kind != like)
            {
                throw new ArgumentException($"元件集已經有名稱「{name}」（{existing.kind}，" +
                    $"{AssetDatabase.GetAssetPath(existing.prefab)}），內容與這次登記不同，不會覆寫。");
            }
            alreadyRegistered = true;
            return prefab;
        }

        internal static bool IsBuiltInName(string name, out UIWidgetKind kind)
        {
            foreach (var candidate in BuiltIn)
            {
                if (candidate.ToString() != name)
                    continue;
                kind = candidate;
                return true;
            }
            kind = UIWidgetKind.Custom;
            return false;
        }

        internal static Type RequiredComponent(UIWidgetKind kind)
        {
            return kind switch
            {
                UIWidgetKind.Text => typeof(TMP_Text),
                UIWidgetKind.Image => typeof(Image),
                UIWidgetKind.Panel => typeof(Image),
                UIWidgetKind.Button => typeof(Button),
                UIWidgetKind.Toggle => typeof(Toggle),
                UIWidgetKind.Slider => typeof(Slider),
                UIWidgetKind.ProgressBar => typeof(UIProgressBar),
                UIWidgetKind.Dropdown => typeof(TMP_Dropdown),
                UIWidgetKind.InputField => typeof(TMP_InputField),
                UIWidgetKind.ScrollList => typeof(UIList),
                _ => null
            };
        }

        /// <summary>
        /// Prefab 缺少行為類別需要的元件時回傳說明，否則回傳 null。
        /// </summary>
        internal static string MissingRequirement(UIWidgetKind kind, GameObject prefab)
        {
            var required = RequiredComponent(kind);
            if (required == null || !prefab || prefab.GetComponentInChildren(required, true))
                return null;
            return $"Prefab「{AssetDatabase.GetAssetPath(prefab)}」缺少 {required.Name}。";
        }

        private static bool IsComplete(UIWidgetSet set)
        {
            return set.listItem && set.confirm && BuiltIn.All(kind => set.Find(kind.ToString())?.prefab);
        }

        private static bool IsNameCharacter(char character)
        {
            return character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-';
        }

        private static GameObject LoadOrBuild(string path, List<string> created, Func<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing)
                return existing;
            if (AssetDatabase.LoadMainAssetAtPath(path))
                throw new InvalidOperationException($"「{path}」已經被其他資產占用，無法建立起始元件。");
            var prefab = build();
            created.Add(path);
            return prefab;
        }

        private static IEnumerable<string> PrefabPaths(UIWidgetSet set)
        {
            return set.widgets.Where(entry => entry != null && entry.prefab).Select(entry => entry.prefab)
                .Append(set.listItem).Append(set.confirm ? set.confirm.gameObject : null)
                .Where(prefab => prefab).Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
        }

        // 回傳這個 Prefab 有沒有被改寫。
        private static bool ReplaceFont(string path, TMP_FontAsset previous, TMP_FontAsset next)
        {
            ReplacingFont?.Invoke(path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var changed = false;
                foreach (var field in root.GetComponentsInChildren<TMP_InputField>(true))
                {
                    // 巢狀 Prefab 的內容由它自己的 Prefab 處理，這裡改了會變成固定住字型的覆寫。
                    if (PrefabUtility.IsPartOfPrefabInstance(field) || field.fontAsset != previous)
                        continue;
                    // 不用 field.fontAsset 的 setter：它會連輸入的文字和 placeholder 一起換，
                    // 包含專案已經改成別的字型的那一個。這裡只換輸入框自己記的字型，兩個文字由下面各自判斷。
                    var serialized = new SerializedObject(field);
                    serialized.FindProperty("m_GlobalFontAsset").objectReferenceValue = next;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(text) || text.font != previous)
                        continue;
                    text.font = next;
                    changed = true;
                }
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Tools/UniGameUI/Install Starter Widgets", priority = 31)]
        private static void InstallFromMenu()
        {
            try
            {
                var set = Install();
                Selection.activeObject = set;
                EditorGUIUtility.PingObject(set);
                Debug.Log($"UniGameUI：元件集位於「{AssetDatabase.GetAssetPath(set)}」。", set);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("UniGameUI", exception.Message, "關閉");
            }
        }
    }
}
