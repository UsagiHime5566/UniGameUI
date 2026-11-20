using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Slider = UnityEngine.UI.Slider;

namespace UniGameUI.Editor.Tests
{
    // 第四批：驗收工具（問題代碼、由程式控制的畫面、Validate 新增的檢查）。
    public sealed partial class UIAuthoringTests
    {
        private static IEnumerable<string> IssueCodes =>
            UIIssueCode.All.OrderBy(code => code, StringComparer.Ordinal);

        [Test]
        public void IssuesCarryAStableCodeAndTheViewAndElementTheyAreAbout()
        {
            var root = WorkspaceWithHomePage("Batch4 Issue Shape");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            SetElementId(CreateBinding(home, "noRoute").gameObject, "start");

            var issue = UIValidation.Check(root).Single(found => found.Code == UIIssueCode.ButtonActionUnrouted);
            Assert.That(issue.Code, Is.EqualTo("button.action-unrouted"));
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(issue.ViewId, Is.EqualTo("home"));
            Assert.That(issue.ElementId, Is.EqualTo("start"));
            Assert.That(issue.Message, Does.Contain("home").And.Contain("noRoute"));
            Assert.That(issue.ToString(), Is.EqualTo("Error [button.action-unrouted] " + issue.Message));

            // 和畫面、元件無關的問題，兩個 ID 都是 null。
            UIValidation.ActiveInputHandlingOverride = 0;
            try
            {
                var legacy = UIValidation.Check(root).Single(found => found.Code == UIIssueCode.InputHandlingLegacy);
                Assert.That(legacy.ViewId, Is.Null);
                Assert.That(legacy.ElementId, Is.Null);
                Assert.That(legacy.ToString(), Does.StartWith("Error [input.handling-legacy] "));
            }
            finally
            {
                UIValidation.ActiveInputHandlingOverride = null;
            }

            // 嚴重度由代碼決定；代碼一律是小寫的「主題.問題」。
            foreach (var found in UIValidation.Check(root))
                Assert.That(found.Severity, Is.EqualTo(UIIssueCode.SeverityOf(found.Code)), found.Code);
            foreach (var listed in UIIssueCode.All)
                Assert.That(Regex.IsMatch(listed, @"^[a-z]+(-[a-z]+)*\.[a-z]+(-[a-z]+)*$"), Is.True, listed);
            Assert.That(UIIssueCode.All, Is.Unique);
            Assert.Throws<ArgumentException>(() => UIIssueCode.SeverityOf("no.such-code"));
        }

        [TestCaseSource(nameof(IssueCodes))]
        public void EveryIssueCodeIsReportedByAtLeastOneCase(string code)
        {
            var issues = Reproduce(code);
            var found = issues.Where(issue => issue.Code == code).ToArray();
            Assert.That(found, Is.Not.Empty, code + " 沒有被回報。實際回報的是：\n" + string.Join("\n", issues));
            foreach (var issue in found)
            {
                Assert.That(issue.Severity, Is.EqualTo(UIIssueCode.SeverityOf(code)));
                Assert.That(issue.Message, Is.Not.Empty);
                // 訊息一律是繁體中文：至少要有一個中文字。
                Assert.That(issue.Message.Any(character => character >= 0x4E00 && character <= 0x9FFF), Is.True,
                    issue.Message);
            }
        }

        [Test]
        public void AgentGuideListsEveryIssueCodeWithItsSeverity()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UIValidation).Assembly);
            Assert.That(package, Is.Not.Null);
            var guide = File.ReadAllText(Path.Combine(package.resolvedPath, "Documentation~", "AgentGuide.md"));
            var rows = Regex.Matches(guide, @"^\| `([a-z-]+\.[a-z-]+)` \| (Error|Warning) \|", RegexOptions.Multiline)
                .Cast<Match>().Select(match => (Code: match.Groups[1].Value, Severity: match.Groups[2].Value))
                .ToArray();

            Assert.That(rows.Select(row => row.Code), Is.Unique);
            Assert.That(rows.Select(row => row.Code), Is.EquivalentTo(UIIssueCode.All));
            foreach (var (code, severity) in rows)
                Assert.That(severity, Is.EqualTo(UIIssueCode.SeverityOf(code).ToString()), code);
        }

        [Test]
        public void PlanMarksViewsAsNavigatedByCodeAndConfirmPopupsCountAutomatically()
        {
            RequireDOTween();
            UseIsolatedWidgets();
            var root = WorkspaceWithHomePage("Batch4 Navigation");
            var scene = root.gameObject.scene;
            var tipPath = $"{folder}/Tip.prefab";

            // 新建的畫面：預設是 Routes。
            UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""loading"", ""navigation"": ""Code"" }},
                {{ ""id"": ""menu"" }},
                {{ ""id"": ""explicit"", ""navigation"": ""Routes"" }},
                {{ ""id"": ""tip"", ""kind"": ""Popup"", ""prefabPath"": ""{tipPath}"", ""navigation"": ""Code"" }},
                {{ ""id"": ""confirm"", ""kind"": ""Popup"", ""prefabPath"": ""{UIWidgets.ConfirmPrefabPath}"" }}] }}");
            Assert.That(Marked("loading"), Is.True);
            Assert.That(Marked("menu"), Is.False);
            Assert.That(Marked("explicit"), Is.False);
            Assert.That(Marked("tip"), Is.True);
            Assert.That(Marked("home"), Is.False);

            // 根節點掛了 UIConfirmPopup 的畫面自動視為由程式控制，藍圖上不必標記。
            var confirm = UIAuthoring.FindEntry(root, "confirm");
            Assert.That(confirm.NavigatedByCode, Is.False);
            Assert.That(UIAuthoring.IsNavigatedByCode(confirm), Is.True);
            Assert.That(UIAuthoring.IsNavigatedByCode(UIAuthoring.FindEntry(root, "tip")), Is.True);
            Assert.That(UIAuthoring.IsNavigatedByCode(UIAuthoring.FindEntry(root, "menu")), Is.False);

            // 設定存在工作區的藍圖裡，不在 View Prefab 裡。
            Assert.That(UIAuthoring.HasReviewOverrides(UIAuthoring.FindEntry(root, "tip")), Is.False);
            Assert.That(File.ReadAllText(tipPath), Does.Not.Contain("navigatedByCode"));

            // 既有的畫面沒有設 updateExisting 時不寫入。
            UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""loading"", ""navigation"": ""Routes"" }, { ""id"": ""menu"", ""navigation"": ""Code"" }] }");
            Assert.That(Marked("loading"), Is.True);
            Assert.That(Marked("menu"), Is.False);

            // 部分更新：只改有寫的畫面，沒寫 navigation 的維持原樣。
            UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""loading"", ""updateExisting"": true, ""navigation"": ""Routes"" },
                { ""id"": ""menu"", ""updateExisting"": true, ""navigation"": ""Code"" },
                { ""id"": ""tip"", ""kind"": ""Popup"", ""updateExisting"": true, ""popupCache"": ""Reuse"" }] }");
            Assert.That(Marked("loading"), Is.False);
            Assert.That(Marked("menu"), Is.True);
            Assert.That(Marked("tip"), Is.True);

            // 寫錯值在套用前報錯，沒有留下任何變更。
            var error = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""other"" }, { ""id"": ""menu"", ""updateExisting"": true, ""navigation"": ""Script"" }] }"));
            Assert.That(error.Message, Does.Contain("Script"));
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);
            Assert.That(Marked("menu"), Is.True);

            // C# 的入口。
            UIAuthoring.SetNavigatedByCode(root, "home", true);
            Assert.That(Marked("home"), Is.True);
            UIAuthoring.SetNavigatedByCode(root, "home", false);
            Assert.That(Marked("home"), Is.False);
            Assert.That(Assert.Throws<ArgumentException>(() => UIAuthoring.SetNavigatedByCode(root, "ghost", true))
                .Message, Does.Contain("ghost"));

            bool Marked(string id) => UIAuthoring.FindEntry(root, id).NavigatedByCode;
        }

        [Test]
        public void BlueprintStoresTheNavigatedByCodeSettingInTheWorkspace()
        {
            var scene = NewScene("Batch4 Blueprint Navigation");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            SceneManager.SetActiveScene(scene);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                Select("tip");
                var toggle = window.rootVisualElement.Q<UnityEngine.UIElements.Toggle>("navigated-by-code");
                Assert.That(toggle, Is.Not.Null);
                Assert.That(toggle.value, Is.False);
                Assert.That(toggle.enabledSelf, Is.True);

                // 右欄勾選之後記在工作區，Scene 變成未儲存，可以 Undo。
                // 測試裡所有操作都在同一個 Undo 群組，先分開，否則會連建立畫面一起還原。
                Undo.IncrementCurrentGroup();
                window.SetNavigatedByCode("tip", true);
                Assert.That(UIAuthoring.FindEntry(root, "tip").NavigatedByCode, Is.True);
                Assert.That(scene.isDirty, Is.True);
                Assert.That(window.rootVisualElement.Q<UnityEngine.UIElements.Toggle>("navigated-by-code").value,
                    Is.True);
                Undo.PerformUndo();
                Assert.That(UIAuthoring.FindEntry(root, "tip").NavigatedByCode, Is.False);
                window.SetNavigatedByCode("tip", true);

                Select("home");
                Assert.That(window.rootVisualElement.Q<UnityEngine.UIElements.Toggle>("navigated-by-code").value,
                    Is.False);

                void Select(string id)
                {
                    var serialized = new SerializedObject(window);
                    serialized.FindProperty("selectedId").stringValue = id;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    window.CreateGUI();
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
            }

            // 存檔後重新開啟，設定還在。
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var path = scene.path;
            var mode = SceneManager.sceneCount > 1 ? OpenSceneMode.Additive : OpenSceneMode.Single;
            if (mode == OpenSceneMode.Additive)
                Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True);
            var reopened = EditorSceneManager.OpenScene(path, mode);
            scenes.Add(reopened);
            var saved = UIAuthoring.FindWorkspace(reopened);
            Assert.That(UIAuthoring.FindEntry(saved, "tip").NavigatedByCode, Is.True);
            Assert.That(UIAuthoring.FindEntry(saved, "home").NavigatedByCode, Is.False);
        }

        [Test]
        public void BlueprintShowsConfirmPopupsAsNavigatedByCodeWithoutLettingThemBeUnmarked()
        {
            RequireDOTween();
            UseIsolatedWidgets();
            var root = WorkspaceWithHomePage("Batch4 Blueprint Confirm");
            UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""confirm"", ""kind"": ""Popup"",
                ""prefabPath"": ""{UIWidgets.ConfirmPrefabPath}"" }}] }}");

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                var serialized = new SerializedObject(window);
                serialized.FindProperty("selectedId").stringValue = "confirm";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                window.CreateGUI();
                var toggle = window.rootVisualElement.Q<UnityEngine.UIElements.Toggle>("navigated-by-code");
                Assert.That(toggle.value, Is.True);
                Assert.That(toggle.enabledSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        // 每個代碼各一個最小的重現案例，回傳 Validate 的結果。
        private IReadOnlyList<UIIssue> Reproduce(string code)
        {
            UIRoot root = null;
            UIView home = null;

            void Home()
            {
                root = WorkspaceWithHomePage("Batch4 Code " + code.Replace('.', ' '));
                home = UIAuthoring.FindEntry(root, "home").Review;
            }

            GameObject Child(UIView view, string name, params Type[] components)
            {
                var child = new GameObject(name, new[] { typeof(RectTransform) }.Concat(components).ToArray());
                child.transform.SetParent(view.Content, false);
                return child;
            }

            UIView Popup(string id)
            {
                return UIAuthoring.CreatePopup(root, id, id, $"{folder}/{id}.prefab");
            }

            EventSystem OwnEventSystem()
            {
                return root.gameObject.scene.GetRootGameObjects()
                    .Select(gameObject => gameObject.GetComponentInChildren<EventSystem>(true)).Single(found => found);
            }

            switch (code)
            {
                case UIIssueCode.WorkspaceMissing:
                    return UIValidation.Check(null);

                case UIIssueCode.DOTweenNotReady:
                    Home();
                    UIValidation.DOTweenStatusOverride = UIDOTweenStatus.Missing;
                    try
                    {
                        return UIValidation.Check(root);
                    }
                    finally
                    {
                        UIValidation.DOTweenStatusOverride = null;
                    }

                case UIIssueCode.ViewIdInvalid:
                    Home();
                    root.entries.Add(new UIEntry { id = "home", kind = UIEntryKind.Page });
                    break;

                case UIIssueCode.ViewReviewMissing:
                    Home();
                    Object.DestroyImmediate(home.gameObject);
                    break;

                case UIIssueCode.ViewPrefabMissing:
                    Home();
                    Popup("tip");
                    Assert.That(AssetDatabase.DeleteAsset($"{folder}/tip.prefab"), Is.True);
                    break;

                case UIIssueCode.ViewOverridesPending:
                    Home();
                    UIAuthoring.SetTransition(Popup("tip"), true, UITransitionPreset.Fade);
                    break;

                case UIIssueCode.ViewCanvasMissing:
                    Home();
                    Object.DestroyImmediate(home.GetComponent<GraphicRaycaster>());
                    break;

                case UIIssueCode.ViewAnimationMissing:
#if DOTWEEN
                    Home();
                    UIAuthoring.SetTransition(home, true, UITransitionPreset.Custom);
                    break;
#else
                    Assert.Ignore("沒有 DOTween 時不檢查自訂動畫。");
                    return null;
#endif

                case UIIssueCode.ViewDefaultSelectedMissing:
                    Home();
                    var doomed = Child(home, "Doomed", typeof(Image), typeof(Button));
                    home.defaultSelected = doomed.GetComponent<Button>();
                    Object.DestroyImmediate(doomed);
                    break;

                case UIIssueCode.ViewDefaultSelectedOutside:
                    Home();
                    var elsewhere = UIAuthoring.CreatePage(root, "other", "Other");
                    home.defaultSelected = Child(elsewhere, "Elsewhere", typeof(Image), typeof(Button))
                        .GetComponent<Button>();
                    break;

                case UIIssueCode.ViewPopupOptionOnPage:
                    Home();
                    home.reusesInstance = true;
                    break;

                case UIIssueCode.ViewOverlayMissing:
                    Home();
                    Popup("tip");
                    EditPrefab($"{folder}/tip.prefab", contents =>
                    {
                        var view = contents.GetComponent<UIView>();
                        Object.DestroyImmediate(view.Overlay.gameObject);
                        view.closesOnOverlayClick = true;
                    });
                    break;

                case UIIssueCode.StartUnknown:
                    Home();
                    root.startPageId = "ghost";
                    break;

                case UIIssueCode.StartNone:
                    Home();
                    break;

                case UIIssueCode.ViewUnreachable:
                    Home();
                    UIAuthoring.SetStartPage(root, "home");
                    UIAuthoring.CreatePage(root, "orphan", "Orphan");
                    break;

                case UIIssueCode.ViewNoExit:
                    Home();
                    UIAuthoring.CreatePage(root, "other", "Other");
                    break;

                case UIIssueCode.ButtonUnbound:
                    Home();
                    Child(home, "Bare", typeof(Image), typeof(Button));
                    break;

                case UIIssueCode.TextMissingGlyph:
                    Home();
                    Child(home, "Label", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>().text = "確定";
                    break;

                case UIIssueCode.RouteInvalid:
                    Home();
                    root.routes.Add(new UIRoute { source = "ghost", action = "go", kind = UIRouteKind.Emit });
                    break;

                case UIIssueCode.RouteTargetMissing:
                    Home();
                    root.routes.Add(new UIRoute
                    {
                        source = "home", action = "go", kind = UIRouteKind.ShowPage, target = "ghost"
                    });
                    break;

                case UIIssueCode.RouteSignalKindInvalid:
                    Home();
                    root.routes.Add(new UIRoute { source = UIRoot.AnySource, action = "go", kind = UIRouteKind.Back });
                    break;

                case UIIssueCode.RouteHistoryInvalid:
                    Home();
                    Popup("tip");
                    root.routes.Add(new UIRoute
                    {
                        source = "home", action = "go", kind = UIRouteKind.OpenPopup, target = "tip",
                        history = UIHistoryMode.Replace
                    });
                    break;

                case UIIssueCode.ElementIdDuplicate:
                    Home();
                    SetElementId(Child(home, "First"), "twin");
                    SetElementId(Child(home, "Second"), "twin");
                    break;

                case UIIssueCode.ElementKindUnknown:
                    Home();
                    var stray = Child(home, "Stray");
                    SetElementId(stray, "stray");
                    stray.GetComponent<UIElementId>().kind = "Vanished";
                    break;

                case UIIssueCode.ButtonActionUnrouted:
                    Home();
                    CreateBinding(home, "noRoute");
                    break;

                case UIIssueCode.InputEventSystemMultiple:
                    Home();
                    SceneManager.MoveGameObjectToScene(
                        new GameObject("Second EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)),
                        root.gameObject.scene);
                    break;

                case UIIssueCode.InputEventSystemMissing:
                    Home();
                    Object.DestroyImmediate(OwnEventSystem().gameObject);
                    break;

                case UIIssueCode.InputEventSystemDisabled:
                    Home();
                    OwnEventSystem().gameObject.SetActive(false);
                    break;

                case UIIssueCode.InputEventSystemShared:
                    Home();
                    SceneManager.MoveGameObjectToScene(
                        new GameObject("Shared EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)),
                        NewScene("Batch4 Code Shared Scene"));
                    break;

                case UIIssueCode.InputModuleInvalid:
                    Home();
                    var eventSystem = OwnEventSystem();
                    Object.DestroyImmediate(eventSystem.GetComponent<InputSystemUIInputModule>());
                    eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                    break;

                case UIIssueCode.InputHandlingLegacy:
                    Home();
                    UIValidation.ActiveInputHandlingOverride = 0;
                    try
                    {
                        return UIValidation.Check(root);
                    }
                    finally
                    {
                        UIValidation.ActiveInputHandlingOverride = null;
                    }

                case UIIssueCode.WidgetsSetMissing:
                    Home();
                    UIWidgets.FolderOverride = $"{folder}/NoWidgetsHere";
                    break;

                case UIIssueCode.WidgetsSetMultiple:
                {
                    var widgets = UseIsolatedWidgets();
                    Home();
                    Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(UIWidgets.Find()),
                        $"{widgets}/Second.asset"), Is.True);
                    break;
                }

                case UIIssueCode.WidgetsNameInvalid:
                {
                    UseIsolatedWidgets();
                    Home();
                    var set = UIWidgets.Find();
                    set.widgets.Add(new UIWidgetEntry
                    {
                        name = "Text", kind = UIWidgetKind.Text, prefab = set.Find("Text").Prefab
                    });
                    break;
                }

                case UIIssueCode.WidgetsPrefabMissing:
                    UseIsolatedWidgets();
                    Home();
                    UIWidgets.Find().Find("Slider").prefab = null;
                    break;

                case UIIssueCode.WidgetsComponentMissing:
                {
                    var widgets = UseIsolatedWidgets();
                    Home();
                    EditPrefab($"{widgets}/Slider.prefab",
                        contents => Object.DestroyImmediate(contents.GetComponent<Slider>()));
                    break;
                }

                case UIIssueCode.ConstantsFileOccupied:
                    return ReproduceOccupiedConstantsFile();

                case UIIssueCode.ConstantsNameConflict:
                    Home();
                    UIAuthoring.CreatePage(root, "open_archive", "First");
                    UIAuthoring.CreatePage(root, "open-archive", "Second");
                    break;

                case UIIssueCode.ConstantsSceneConflict:
                {
                    var firstScene = NewScene("Batch4 Code Twin");
                    UIAuthoring.CreateWorkspace(firstScene);
                    Assert.That(EditorSceneManager.SaveScene(firstScene), Is.True);
                    root = UIAuthoring.CreateWorkspace(NewScene("Batch4 Code Twin", "Other"));
                    break;
                }

                case UIIssueCode.ConstantsSceneUnsaved:
                    Home();
                    return UIValidation.Check(root, null, true);

                case UIIssueCode.ConstantsOutdated:
                    // Scene 在建立工作區之前存過檔，常數還沒有產生。
                    Home();
                    break;

                default:
                    Assert.Fail($"代碼「{code}」沒有重現的案例，請在這裡補上。");
                    return null;
            }

            return UIValidation.Check(root);
        }

        private IReadOnlyList<UIIssue> ReproduceOccupiedConstantsFile()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Batch4 Code Occupied"));
            var path = Generated + "/Batch4CodeOccupiedUI.cs";
            var createdParent = !Directory.Exists(GeneratedParent);
            var createdFolder = !Directory.Exists(Generated);
            Directory.CreateDirectory(Generated);
            File.WriteAllText(path, "// 專案自己放在這裡的檔案。\n");
            try
            {
                return UIValidation.Check(root);
            }
            finally
            {
                File.Delete(path);
                File.Delete(path + ".meta");
                if (createdFolder)
                    DeleteFolderOutsideAssetDatabase(Generated);
                if (createdParent)
                    DeleteFolderOutsideAssetDatabase(GeneratedParent);
            }
        }

        // 這些資料夾是直接在磁碟上建立的，沒有經過 AssetDatabase，所以也直接刪掉。
        private static void DeleteFolderOutsideAssetDatabase(string path)
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
            File.Delete(path + ".meta");
        }
    }
}
