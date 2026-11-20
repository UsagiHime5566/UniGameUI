using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第四批 4-4：多個 Scene 的檢查、JSON 報告、命令列的參數與結束代碼。
    public sealed partial class UIAuthoringTests
    {
        [Serializable]
        private sealed class ValidateReportShape
        {
            public string tool;
            public string version;
            public bool strict;
            public bool passed;
            public int errorCount;
            public int warningCount;
            public string error;
            public ValidateSceneShape[] scenes;
        }

        [Serializable]
        private sealed class ValidateSceneShape
        {
            public string scenePath;
            public bool hasWorkspace;
            public IssueShape[] issues;
        }

        [Serializable]
        private sealed class IssueShape
        {
            public string severity;
            public string code;
            public string viewId;
            public string elementId;
            public string message;
        }

        [Test]
        public void CheckScenesOpensEachSceneAndReturnsToTheOnesThatWereOpen()
        {
            RequireDOTween();
            // 四個要檢查的 Scene：有 Error、只有 Warning、沒有問題、沒有工作區。
            var brokenPath = SaveWorkspaceScene("Batch4 Report Broken", root => root.startPageId = "ghost");
            var warnedPath = SaveWorkspaceScene("Batch4 Report Warned", root =>
            {
                var bare = new GameObject("Bare", typeof(RectTransform), typeof(Image), typeof(Button));
                bare.transform.SetParent(UIAuthoring.FindEntry(root, "home").Review.Content, false);
            });
            var cleanPath = SaveWorkspaceScene("Batch4 Report Clean", null);
            var plain = NewScene("Batch4 Report Plain");
            var plainPath = plain.path;

            // 檢查之前開著的是另一個 Scene。
            var kept = NewScene("Batch4 Report Kept");
            var keptPath = kept.path;
            SceneManager.MoveGameObjectToScene(new GameObject("Marker"), kept);
            Assert.That(EditorSceneManager.SaveScene(kept), Is.True);
            SceneManager.SetActiveScene(kept);
            var others = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(scene => scene != kept).ToArray();
            foreach (var other in others)
                Assert.That(EditorSceneManager.CloseScene(other, true), Is.True);
            Assert.That(LoadedScenes(), Is.EqualTo(new[] { keptPath }));

            var paths = new[] { brokenPath, warnedPath, cleanPath, plainPath };
            var report = UIValidation.CheckScenes(paths);
            Assert.That(report.Scenes.Select(scene => scene.ScenePath), Is.EqualTo(paths));
            Assert.That(report.Scenes.Select(scene => scene.HasWorkspace), Is.EqualTo(new[] { true, true, true, false }));
            Assert.That(report.Scenes[0].Issues.Select(issue => issue.Code), Is.EqualTo(new[] { UIIssueCode.StartUnknown }));
            Assert.That(report.Scenes[1].Issues.Select(issue => issue.Code), Is.EqualTo(new[] { UIIssueCode.ButtonUnbound }));
            Assert.That(report.Scenes[1].Issues[0].ViewId, Is.EqualTo("home"));
            Assert.That(report.Scenes[2].Issues, Is.Empty);
            Assert.That(report.Scenes[3].Issues, Is.Empty);
            Assert.That(report.Strict, Is.False);
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.Passed, Is.False);
            Assert.That(UIValidation.ExitCodeFor(report), Is.EqualTo(1));
            AssertBackToKept();

            // 嚴格模式把 Warning 也當成沒有通過。
            Assert.That(UIValidation.CheckScenes(new[] { warnedPath }).Passed, Is.True);
            var strict = UIValidation.CheckScenes(new[] { warnedPath }, true);
            Assert.That(strict.Strict, Is.True);
            Assert.That(strict.Passed, Is.False);
            Assert.That(UIValidation.CheckScenes(new[] { cleanPath, plainPath }, true).Passed, Is.True);
            AssertBackToKept();

            // 省略 Scene 時檢查 Build Settings 裡啟用的那些。
            var buildScenes = EditorBuildSettings.scenes;
            try
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(cleanPath, true),
                    new EditorBuildSettingsScene(brokenPath, false),
                    new EditorBuildSettingsScene(plainPath, true)
                };
                Assert.That(UIValidation.CheckScenes().Scenes.Select(scene => scene.ScenePath),
                    Is.EqualTo(new[] { cleanPath, plainPath }));
                Assert.That(UIValidation.CheckScenes(Array.Empty<string>(), true).Passed, Is.True);

                EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();
                Assert.Throws<ArgumentException>(() => UIValidation.CheckScenes());
            }
            finally
            {
                EditorBuildSettings.scenes = buildScenes;
            }
            AssertBackToKept();

            // Scene 不存在：還沒開啟任何 Scene 就報錯。
            var missing = Assert.Throws<FileNotFoundException>(() =>
                UIValidation.CheckScenes(new[] { cleanPath, $"{folder}/Missing.unity" }));
            Assert.That(missing.Message, Does.Contain("Missing.unity"));
            AssertBackToKept();

            // 開著的 Scene 有未儲存的修改：丟出例外，不會自動存檔，也不會切換 Scene。
            kept = SceneManager.GetSceneByPath(keptPath);
            SceneManager.MoveGameObjectToScene(new GameObject("Unsaved"), kept);
            EditorSceneManager.MarkSceneDirty(kept);
            var dirty = Assert.Throws<InvalidOperationException>(() => UIValidation.CheckScenes(new[] { cleanPath }));
            Assert.That(dirty.Message, Does.Contain(keptPath).And.Contain("未儲存"));
            Assert.That(kept.isLoaded && kept.isDirty, Is.True);
            Assert.That(kept.GetRootGameObjects().Select(gameObject => gameObject.name),
                Is.EquivalentTo(new[] { "Marker", "Unsaved" }));

            void AssertBackToKept()
            {
                Assert.That(LoadedScenes(), Is.EqualTo(new[] { keptPath }));
                var reopened = SceneManager.GetActiveScene();
                scenes.Add(reopened);
                Assert.That(reopened.path, Is.EqualTo(keptPath));
                Assert.That(reopened.isDirty, Is.False);
                Assert.That(reopened.GetRootGameObjects().Select(gameObject => gameObject.name),
                    Is.EqualTo(new[] { "Marker" }));
            }
        }

        // K6：什麼都沒檢查到不可以讀成通過；同一個 Scene 給兩次只算一次。
        [Test]
        public void CheckScenesDoesNotPassWhenNothingWasCheckedAndCountsEachSceneOnce()
        {
            RequireDOTween();
            var brokenPath = SaveWorkspaceScene("Batch4Fix Report Broken", root => root.startPageId = "ghost");
            var cleanPath = SaveWorkspaceScene("Batch4Fix Report Clean", null);
            var plainPath = NewScene("Batch4Fix Report Plain").path;
            var otherPlainPath = NewScene("Batch4Fix Report Plain 2").path;
            // 檢查之前只留一個存過檔的 Scene 開著。檢查會重新開啟 Scene，結束時把開著的記下來，案例結束才關得掉。
            var kept = NewScene("Batch4Fix Report Kept");
            foreach (var other in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != kept).ToArray())
                Assert.That(EditorSceneManager.CloseScene(other, true), Is.True);
            try
            {
                CheckNothingAndDuplicates();
            }
            finally
            {
                for (var index = 0; index < SceneManager.sceneCount; index++)
                    scenes.Add(SceneManager.GetSceneAt(index));
            }

            void CheckNothingAndDuplicates()
            {
                // 每一個 Scene 都沒有工作區：不算通過，C# 的結果與 JSON 一致，原因寫在 Error。
                var nothing = UIValidation.CheckScenes(new[] { plainPath, otherPlainPath });
                Assert.That(nothing.Scenes.Select(scene => scene.HasWorkspace), Is.EqualTo(new[] { false, false }));
                Assert.That(nothing.ErrorCount, Is.Zero);
                Assert.That(nothing.Passed, Is.False);
                Assert.That(nothing.Error, Does.Contain("工作區"));
                var json = JsonUtility.FromJson<ValidateReportShape>(nothing.ToJson());
                Assert.That(json.passed, Is.False);
                Assert.That(json.error, Is.EqualTo(nothing.Error));
                Assert.That(UIValidation.ExitCodeFor(nothing), Is.EqualTo(2));
                Assert.That(UIValidation.CheckScenes(new[] { plainPath }, true).Passed, Is.False);

                // 只要有一個 Scene 有工作區就照常判斷。
                var some = UIValidation.CheckScenes(new[] { plainPath, cleanPath });
                Assert.That(some.Passed, Is.True);
                Assert.That(some.Error, Is.Empty);
                Assert.That(JsonUtility.FromJson<ValidateReportShape>(some.ToJson()).error, Is.Empty);
                Assert.That(UIValidation.ExitCodeFor(some), Is.EqualTo(0));

                // 同一個 Scene 寫了三次（其中一次用反斜線）：只開一次，問題只算一次。
                var twice = UIValidation.CheckScenes(new[] { brokenPath, brokenPath.Replace('/', '\\'), brokenPath, cleanPath });
                Assert.That(twice.Scenes.Select(scene => scene.ScenePath), Is.EqualTo(new[] { brokenPath, cleanPath }));
                Assert.That(twice.ErrorCount, Is.EqualTo(1));
                Assert.That(twice.Passed, Is.False);
                Assert.That(twice.Error, Is.Empty);
            }
        }

        // K6：未命名的 Scene 和存過檔的 Scene 一起開著時，檢查完兩個都要回來。
        [Test]
        public void CheckScenesPutsBackAnUntitledSceneThatWasOpenWithSavedOnes()
        {
            RequireDOTween();
            var cleanPath = SaveWorkspaceScene("Batch4Fix Report Target", null);
            var kept = NewScene("Batch4Fix Report Kept With Untitled");
            var keptPath = kept.path;
            foreach (var other in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != kept).ToArray())
                Assert.That(EditorSceneManager.CloseScene(other, true), Is.True);
            var untitled = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scenes.Add(untitled);
            SceneManager.SetActiveScene(untitled);
            Assert.That(untitled.isDirty, Is.False);
            Assert.That(LoadedScenes(), Is.EqualTo(new[] { keptPath, "" }));

            try
            {
                Assert.That(UIValidation.CheckScenes(new[] { cleanPath }).Passed, Is.True);
                Assert.That(LoadedScenes(), Is.EqualTo(new[] { keptPath, "" }));
                Assert.That(SceneManager.GetActiveScene().path, Is.Empty, "原本作用中的是未命名的 Scene");

                // 作用中的是存過檔的那一個時，回來之後也一樣。
                SceneManager.SetActiveScene(SceneManager.GetSceneByPath(keptPath));
                Assert.That(UIValidation.CheckScenes(new[] { cleanPath }).Passed, Is.True);
                Assert.That(LoadedScenes(), Is.EqualTo(new[] { keptPath, "" }));
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(keptPath));
            }
            finally
            {
                // 檢查會重新開啟 Scene，把現在開著的記下來，案例結束時才關得掉。
                for (var index = 0; index < SceneManager.sceneCount; index++)
                    scenes.Add(SceneManager.GetSceneAt(index));
            }
        }

        [Test]
        public void ValidationReportSerializesToTheDocumentedJson()
        {
            var report = new UIValidationReport(true, new[]
            {
                new UIValidationSceneReport("Assets/Scenes/Workshop.unity", true, new[]
                {
                    new UIIssue(UIIssueCode.RouteTargetMissing, "路由「workshop/go」的目的地「ghost」不存在。", "workshop"),
                    new UIIssue(UIIssueCode.ButtonUnbound, "按鈕 \"Bare\" 沒有動作。\n第二行", "workshop", "bare")
                }),
                new UIValidationSceneReport("Assets/Scenes/Empty.unity", false, null)
            });
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));

            var json = report.ToJson();
            var parsed = JsonUtility.FromJson<ValidateReportShape>(json);
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UIValidation).Assembly);
            Assert.That(parsed.tool, Is.EqualTo("UniGameUI.Validate"));
            Assert.That(parsed.version, Is.EqualTo(package.version));
            Assert.That(parsed.strict, Is.True);
            Assert.That(parsed.passed, Is.False);
            Assert.That(parsed.errorCount, Is.EqualTo(1));
            Assert.That(parsed.warningCount, Is.EqualTo(1));
            Assert.That(parsed.error, Is.Empty);
            Assert.That(parsed.scenes.Length, Is.EqualTo(2));
            Assert.That(parsed.scenes[0].scenePath, Is.EqualTo("Assets/Scenes/Workshop.unity"));
            Assert.That(parsed.scenes[0].hasWorkspace, Is.True);
            Assert.That(parsed.scenes[0].issues.Length, Is.EqualTo(2));
            var first = parsed.scenes[0].issues[0];
            Assert.That((first.severity, first.code, first.viewId, first.elementId),
                Is.EqualTo(("Error", "route.target-missing", "workshop", "")));
            Assert.That(first.message, Is.EqualTo("路由「workshop/go」的目的地「ghost」不存在。"));
            var second = parsed.scenes[0].issues[1];
            Assert.That((second.severity, second.code, second.viewId, second.elementId),
                Is.EqualTo(("Warning", "button.unbound", "workshop", "bare")));
            Assert.That(second.message, Is.EqualTo("按鈕 \"Bare\" 沒有動作。\n第二行"));
            Assert.That(parsed.scenes[1].scenePath, Is.EqualTo("Assets/Scenes/Empty.unity"));
            Assert.That(parsed.scenes[1].hasWorkspace, Is.False);
            Assert.That(parsed.scenes[1].issues, Is.Empty);

            // 欄位名稱與順序和文件一致。
            var keys = Regex.Matches(json, "\"([A-Za-z]+)\":").Cast<Match>().Select(match => match.Groups[1].Value)
                .Distinct().ToArray();
            Assert.That(keys, Is.EqualTo(new[]
            {
                "tool", "version", "strict", "passed", "errorCount", "warningCount", "error", "scenes",
                "scenePath", "hasWorkspace", "issues", "severity", "code", "viewId", "elementId", "message"
            }));

            // 嚴格模式只影響通過與否。
            var relaxed = new UIValidationReport(false, new[]
            {
                new UIValidationSceneReport("Assets/A.unity", true,
                    new[] { new UIIssue(UIIssueCode.ButtonUnbound, "只是警告") })
            });
            Assert.That(JsonUtility.FromJson<ValidateReportShape>(relaxed.ToJson()).passed, Is.True);
            Assert.That(JsonUtility.FromJson<ValidateReportShape>(relaxed.ToJson()).strict, Is.False);
        }

        [Test]
        public void ValidateExitCodeFollowsTheReport()
        {
            // 0：沒有 Error。嚴格模式下也沒有 Warning。
            Assert.That(Exit(false, Scene(true)), Is.EqualTo(0));
            Assert.That(Exit(false, Scene(true, UIIssueCode.ButtonUnbound)), Is.EqualTo(0));
            Assert.That(Exit(true, Scene(true), Scene(false)), Is.EqualTo(0));

            // 1：有 Error，或嚴格模式下有 Warning。
            Assert.That(Exit(false, Scene(true, UIIssueCode.RouteTargetMissing)), Is.EqualTo(1));
            Assert.That(Exit(false, Scene(true), Scene(true, UIIssueCode.ButtonUnbound, UIIssueCode.StartUnknown)),
                Is.EqualTo(1));
            Assert.That(Exit(true, Scene(true, UIIssueCode.ButtonUnbound)), Is.EqualTo(1));

            // 2：沒有任何 Scene 有工作區，沒辦法檢查。
            Assert.That(Exit(false, Scene(false)), Is.EqualTo(2));
            Assert.That(Exit(true, Scene(false), Scene(false)), Is.EqualTo(2));
            Assert.That(Exit(false), Is.EqualTo(2));
            Assert.That(UIValidation.ExitCodeFor(null), Is.EqualTo(2));

            static int Exit(bool strict, params UIValidationSceneReport[] scenes)
            {
                return UIValidation.ExitCodeFor(new UIValidationReport(strict, scenes));
            }

            static UIValidationSceneReport Scene(bool hasWorkspace, params string[] codes)
            {
                return new UIValidationSceneReport("Assets/Scene.unity", hasWorkspace,
                    codes.Select(code => new UIIssue(code, "測試用的問題")).ToArray());
            }
        }

        [Test]
        public void ValidateCommandLineArgumentsAreParsed()
        {
            var parsed = UIValidation.ParseArguments(new[]
            {
                "Unity.exe", "-batchmode", "-projectPath", "C:/Project", "-logFile", "-",
                "-executeMethod", "UniGameUI.Editor.UIValidation.RunFromCommandLine",
                "-unigameui-scene", "Assets/Scenes/A.unity", "-unigameui-strict",
                "-unigameui-scene", "Assets/Scenes/B.unity", "-unigameui-report", "Temp/validate.json"
            });
            Assert.That(parsed.Scenes, Is.EqualTo(new[] { "Assets/Scenes/A.unity", "Assets/Scenes/B.unity" }));
            Assert.That(parsed.Strict, Is.True);
            Assert.That(parsed.ReportPath, Is.EqualTo("Temp/validate.json"));

            // 都省略：檢查 Build Settings 的 Scene、不是嚴格模式、報告寫到預設位置。
            var defaults = UIValidation.ParseArguments(new[] { "Unity.exe", "-batchmode" });
            Assert.That(defaults.Scenes, Is.Empty);
            Assert.That(defaults.Strict, Is.False);
            Assert.That(defaults.ReportPath, Is.Null);
            Assert.That(UIValidation.DefaultReportPath, Is.EqualTo("Library/UniGameUI/validate.json"));

            // 參數錯誤：少了值，或寫錯名稱。
            Assert.That(Assert.Throws<ArgumentException>(() =>
                UIValidation.ParseArguments(new[] { "Unity.exe", "-unigameui-scene" })).Message,
                Does.Contain("-unigameui-scene"));
            Assert.Throws<ArgumentException>(() =>
                UIValidation.ParseArguments(new[] { "Unity.exe", "-unigameui-report", "-unigameui-strict" }));
            Assert.That(Assert.Throws<ArgumentException>(() =>
                UIValidation.ParseArguments(new[] { "Unity.exe", "-unigameui-strct" })).Message,
                Does.Contain("-unigameui-strct"));
        }

        [Test]
        public void BlueprintValidateSummarisesTheCountsAndLogsEachIssueWithItsCode()
        {
            var root = WorkspaceWithHomePage("Batch4 Validate Dialog");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            var bare = new GameObject("Bare", typeof(RectTransform), typeof(Image), typeof(Button));
            bare.transform.SetParent(home.Content, false);
            root.startPageId = "ghost";

            var issues = UIValidation.Check(root).Where(issue => issue.Code != UIIssueCode.DOTweenNotReady).ToArray();
            var errors = issues.Count(issue => issue.Severity == UIIssueSeverity.Error);
            Assert.That(errors, Is.EqualTo(1));
            var message = UIValidation.DialogMessage(issues);
            Assert.That(message, Does.Contain("Error 1 筆").And.Contain($"Warning {issues.Length - 1} 筆")
                .And.Contain("Console").And.Contain("[start.unknown]"));
            Assert.That(UIValidation.DialogMessage(Array.Empty<UIIssue>()), Is.EqualTo("沒有發現問題。"));

            // 每一筆問題連同代碼記到 Console，並帶著可以定位的物件。
            LogAssert.Expect(LogType.Error, new Regex(@"^UniGameUI Error \[start\.unknown\] "));
            LogAssert.Expect(LogType.Warning, new Regex(@"^UniGameUI Warning \[button\.unbound\] .*Bare"));
            UIValidation.LogIssues(root, issues.Where(issue =>
                issue.Code is UIIssueCode.StartUnknown or UIIssueCode.ButtonUnbound));
            var unbound = issues.Single(issue => issue.Code == UIIssueCode.ButtonUnbound);
            Assert.That(unbound.Context, Is.SameAs(bare.GetComponent<Button>()));
        }

        // 建立一個只有 home 頁、起始頁是 home 的工作區，存檔後關閉，回傳 Scene 的路徑。
        private string SaveWorkspaceScene(string name, Action<UIRoot> edit)
        {
            var scene = NewScene(name);
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.SetStartPage(root, "home");
            edit?.Invoke(root);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var path = scene.path;
            if (SceneManager.sceneCount > 1)
                Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True);
            return path;
        }
    }
}
