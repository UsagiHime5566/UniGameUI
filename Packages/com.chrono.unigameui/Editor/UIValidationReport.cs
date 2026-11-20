using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniGameUI.Editor
{
    /// <summary>
    /// 一個 Scene 的檢查結果。Scene 沒有工作區時 <see cref="HasWorkspace"/> 是 false，沒有任何問題。
    /// </summary>
    public sealed class UIValidationSceneReport
    {
        public string ScenePath { get; }
        public bool HasWorkspace { get; }
        public IReadOnlyList<UIIssue> Issues { get; }

        internal UIValidationSceneReport(string scenePath, bool hasWorkspace, IReadOnlyList<UIIssue> issues)
        {
            ScenePath = scenePath;
            HasWorkspace = hasWorkspace;
            Issues = issues ?? Array.Empty<UIIssue>();
        }
    }

    /// <summary>
    /// <see cref="UIValidation.CheckScenes"/> 的結果，也是命令列寫出的 JSON 報告的內容。
    /// </summary>
    public sealed class UIValidationReport
    {
        public const string ToolName = "UniGameUI.Validate";

        /// <summary>嚴格模式：Warning 也算沒有通過。</summary>
        public bool Strict { get; }

        internal const string NothingChecked = "指定的 Scene 都沒有 UniGameUI 工作區。";

        public IReadOnlyList<UIValidationSceneReport> Scenes { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }

        /// <summary>
        /// 沒辦法當成檢查過的原因：沒有任何一個 Scene 有 UniGameUI 工作區。正常情況是空字串。
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// 至少檢查到一個工作區，沒有 Error，嚴格模式下也沒有 Warning。
        /// 什麼都沒檢查到（<see cref="Error"/> 不是空的）時是 false。
        /// </summary>
        public bool Passed => Error.Length == 0 && ErrorCount == 0 && (!Strict || WarningCount == 0);

        internal UIValidationReport(bool strict, IReadOnlyList<UIValidationSceneReport> scenes)
        {
            Strict = strict;
            Scenes = scenes ?? Array.Empty<UIValidationSceneReport>();
            var issues = Scenes.SelectMany(scene => scene.Issues).ToArray();
            ErrorCount = issues.Count(issue => issue.Severity == UIIssueSeverity.Error);
            WarningCount = issues.Count(issue => issue.Severity == UIIssueSeverity.Warning);
            Error = Scenes.Any(scene => scene.HasWorkspace) ? "" : NothingChecked;
        }

        public string ToJson()
        {
            return ToJson(null);
        }

        // error：沒辦法執行時的原因（例如參數錯誤）。沒有給的時候用報告自己的 Error。
        internal string ToJson(string error)
        {
            var reason = string.IsNullOrEmpty(error) ? Error : error;
            return JsonUtility.ToJson(new ReportJson
            {
                tool = ToolName,
                version = UIValidation.PackageVersion,
                strict = Strict,
                passed = reason.Length == 0 && Passed,
                errorCount = ErrorCount,
                warningCount = WarningCount,
                error = reason,
                scenes = Scenes.Select(scene => new SceneJson
                {
                    scenePath = scene.ScenePath,
                    hasWorkspace = scene.HasWorkspace,
                    issues = scene.Issues.Select(UIValidation.IssueJson.From).ToList()
                }).ToList()
            }, true);
        }

        [Serializable]
        private sealed class ReportJson
        {
            public string tool;
            public string version;
            public bool strict;
            public bool passed;
            public int errorCount;
            public int warningCount;
            public string error;
            public List<SceneJson> scenes;
        }

        [Serializable]
        private sealed class SceneJson
        {
            public string scenePath;
            public bool hasWorkspace;
            public List<UIValidation.IssueJson> issues;
        }
    }

    // Validate 的命令列入口、多個 Scene 的檢查與報告。
    public static partial class UIValidation
    {
        internal const string DefaultReportPath = "Library/UniGameUI/validate.json";
        internal const string SceneArgument = "-unigameui-scene";
        internal const string ReportArgument = "-unigameui-report";
        private const string StrictArgument = "-unigameui-strict";

        internal static string PackageVersion
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UIValidation).Assembly);
                return package != null ? package.version : "0.0.0";
            }
        }

        // 報告裡的一筆問題。走訪的報告也用同一個格式。
        [Serializable]
        internal sealed class IssueJson
        {
            public string severity;
            public string code;
            public string viewId;
            public string elementId;
            public string message;

            public static IssueJson From(UIIssue issue)
            {
                return new IssueJson
                {
                    severity = issue.Severity.ToString(),
                    code = issue.Code,
                    viewId = issue.ViewId ?? "",
                    elementId = issue.ElementId ?? "",
                    message = issue.Message
                };
            }
        }

        internal sealed class CommandLine
        {
            public readonly List<string> Scenes = new List<string>();
            public string ReportPath;
            public bool Strict;
        }

        /// <summary>
        /// 依序開啟並檢查多個 Scene，結束後回到原本開著的 Scene。<paramref name="scenePaths"/> 是 null 或空的時候，
        /// 檢查 Build Settings 裡啟用的所有 Scene。沒有工作區的 Scene 會略過並記在報告裡；
        /// 每一個 Scene 都沒有工作區時，結果的 Passed 是 false、Error 是原因。同一個 Scene 寫了兩次只檢查一次。
        /// 目前開著的 Scene 有未儲存的修改時丟出例外，不會自動存檔。這個方法不會結束 Editor。
        /// </summary>
        public static UIValidationReport CheckScenes(IEnumerable<string> scenePaths = null, bool strict = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("請在 Edit Mode 執行檢查。");

            var paths = (scenePaths ?? Enumerable.Empty<string>()).ToArray();
            if (paths.Length == 0)
            {
                paths = EditorBuildSettings.scenes.Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                    .Select(scene => scene.path).ToArray();
            }
            if (paths.Length == 0)
                throw new ArgumentException("沒有指定要檢查的 Scene，Build Settings 裡也沒有啟用的 Scene。");

            // 同一個 Scene 寫了兩次（包含斜線的寫法不同）只檢查一次，否則問題的數量會重複計算。
            var unique = new List<string>();
            var seen = new HashSet<string>();
            foreach (var given in paths)
            {
                var path = (given ?? "").Replace('\\', '/');
                if (path.Length == 0 || !AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
                    throw new FileNotFoundException($"找不到 Scene「{given}」。請使用專案裡的路徑，例如 Assets/Scenes/Main.unity。", given);
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (seen.Add(string.IsNullOrEmpty(guid) ? path : guid))
                    unique.Add(path);
            }
            paths = unique.ToArray();

            var restore = RememberOpenScenes();
            var reports = new List<UIValidationSceneReport>();
            try
            {
                foreach (var path in paths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var root = UIAuthoring.FindWorkspace(scene);
                    reports.Add(root
                        ? new UIValidationSceneReport(path, true, Check(root))
                        : new UIValidationSceneReport(path, false, null));
                }
            }
            finally
            {
                restore();
            }
            return new UIValidationReport(strict, reports);
        }

        // 記下目前開著的 Scene，回傳把它們開回來的動作。有未儲存的修改時直接丟出例外。
        internal static Action RememberOpenScenes()
        {
            var saved = new List<SceneSetup>();
            var active = SceneManager.GetActiveScene();
            var untitledSetup = NewSceneSetup.EmptyScene;
            var hadUntitled = false;
            var untitledWasActive = false;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty)
                {
                    var name = string.IsNullOrEmpty(scene.path) ? "尚未儲存的 Scene" : scene.path;
                    throw new InvalidOperationException(
                        $"目前開著的「{name}」有未儲存的修改。請先存檔或捨棄修改，再執行檢查。");
                }
                if (string.IsNullOrEmpty(scene.path))
                {
                    // 沒存過、也沒改過的 Scene 沒有內容可以留，事後換回一個新的。
                    hadUntitled = true;
                    untitledWasActive |= scene == active;
                    if (scene.rootCount > 0)
                        untitledSetup = NewSceneSetup.DefaultGameObjects;
                    continue;
                }
                saved.Add(new SceneSetup { path = scene.path, isLoaded = scene.isLoaded, isActive = scene == active });
            }

            return () =>
            {
                if (saved.Count == 0)
                {
                    EditorSceneManager.NewScene(untitledSetup, NewSceneMode.Single);
                    return;
                }
                if (!saved.Any(setup => setup.isLoaded))
                    saved[0].isLoaded = true;
                if (!saved.Any(setup => setup.isActive && setup.isLoaded))
                {
                    foreach (var setup in saved)
                        setup.isActive = false;
                    saved.First(setup => setup.isLoaded).isActive = true;
                }
                EditorSceneManager.RestoreSceneManagerSetup(saved.ToArray());
                if (!hadUntitled)
                    return;
                // 未命名的 Scene 和存過檔的 Scene 一起開著：存過檔的開回來之後，再補回一個新的未命名 Scene。
                // 新建立的 Scene 會變成作用中的 Scene，所以原本作用中的是哪一個要另外設回去。
                var restoredActive = SceneManager.GetActiveScene();
                var untitled = EditorSceneManager.NewScene(untitledSetup, NewSceneMode.Additive);
                SceneManager.SetActiveScene(untitledWasActive ? untitled : restoredActive);
            };
        }

        /// <summary>
        /// 命令列入口。不要加 -quit：這個方法會自己結束 Editor，並用結束代碼表示結果
        /// （0 通過、1 有問題、2 沒辦法執行）。
        /// </summary>
        public static void RunFromCommandLine()
        {
            var code = 2;
            var commandLine = Environment.GetCommandLineArgs();
            // 參數寫錯時仍然盡量把報告寫到指定的位置。
            var reportPath = FindReportPath(commandLine);
            try
            {
                var arguments = ParseArguments(commandLine);
                reportPath = arguments.ReportPath ?? DefaultReportPath;
                var report = CheckScenes(arguments.Scenes, arguments.Strict);
                code = ExitCodeFor(report);
                WriteReport(reportPath, report.ToJson());
                Debug.Log(Summary(report, reportPath));
                if (report.Error.Length > 0)
                    Debug.LogError("UniGameUI Validate 沒辦法執行：" + report.Error);
            }
            catch (Exception exception)
            {
                code = 2;
                Debug.LogError("UniGameUI Validate 沒辦法執行：" + exception.Message);
                Debug.LogException(exception);
                TryWriteFailure(reportPath, exception.Message);
            }
            EditorApplication.Exit(code);
        }

        // 0：沒有 Error（嚴格模式下也沒有 Warning）。1：有。2：沒有任何 Scene 有工作區。
        internal static int ExitCodeFor(UIValidationReport report)
        {
            if (report == null || report.Error.Length > 0)
                return 2;
            return report.Passed ? 0 : 1;
        }

        internal static CommandLine ParseArguments(string[] arguments)
        {
            var parsed = new CommandLine();
            for (var index = 0; index < arguments.Length; index++)
            {
                switch (arguments[index])
                {
                    case SceneArgument:
                        parsed.Scenes.Add(ArgumentValue(arguments, ref index));
                        break;
                    case ReportArgument:
                        parsed.ReportPath = ArgumentValue(arguments, ref index);
                        break;
                    case StrictArgument:
                        parsed.Strict = true;
                        break;
                    default:
                        if (arguments[index].StartsWith("-unigameui-", StringComparison.Ordinal) &&
                            arguments[index] != "-unigameui-plan")
                            throw new ArgumentException($"Validate 不認得參數「{arguments[index]}」。");
                        break;
                }
            }
            return parsed;
        }

        // 不檢查其他參數，只找出報告的路徑。找不到時是 null。
        internal static string FindReportPath(string[] arguments)
        {
            var index = Array.IndexOf(arguments, ReportArgument);
            return index >= 0 && index + 1 < arguments.Length &&
                !arguments[index + 1].StartsWith("-", StringComparison.Ordinal)
                ? arguments[index + 1]
                : null;
        }

        // 取得參數後面的值。後面沒有東西，或接著另一個參數時報錯。
        internal static string ArgumentValue(string[] arguments, ref int index)
        {
            var name = arguments[index];
            if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("-", StringComparison.Ordinal))
                throw new ArgumentException($"參數「{name}」後面要接一個值。");
            return arguments[++index];
        }

        internal static void WriteReport(string path, string json)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private static void TryWriteFailure(string reportPath, string error)
        {
            try
            {
                WriteReport(reportPath ?? DefaultReportPath,
                    new UIValidationReport(false, null).ToJson(error));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        internal static string Summary(UIValidationReport report, string reportPath)
        {
            var text = new StringBuilder();
            text.Append("UniGameUI Validate：").Append(report.Passed ? "通過" : "沒有通過")
                .Append($"。Error {report.ErrorCount} 筆、Warning {report.WarningCount} 筆")
                .Append(report.Strict ? "（嚴格模式，Warning 也算沒有通過）" : "").Append("。");
            if (!string.IsNullOrEmpty(reportPath))
                text.Append("報告：").Append(reportPath);
            foreach (var scene in report.Scenes)
            {
                text.AppendLine();
                text.Append(scene.ScenePath).Append(scene.HasWorkspace ? "" : "：沒有工作區，略過");
                foreach (var issue in scene.Issues)
                    text.AppendLine().Append("  ").Append(issue);
            }
            return text.ToString();
        }

        // 藍圖視窗按 Validate 之後對話框顯示的內容：兩種嚴重度各幾筆，細節在 Console。
        internal static string DialogMessage(IReadOnlyCollection<UIIssue> issues)
        {
            if (issues.Count == 0)
                return "沒有發現問題。";

            const int listed = 8;
            var errors = issues.Count(issue => issue.Severity == UIIssueSeverity.Error);
            var text = new StringBuilder();
            text.Append($"Error {errors} 筆、Warning {issues.Count - errors} 筆。").AppendLine()
                .Append("每一筆問題連同代碼都記在 Console，點擊可以定位到相關的畫面物件。").AppendLine();
            foreach (var issue in issues.OrderByDescending(issue => issue.Severity).Take(listed))
                text.AppendLine().Append(issue);
            if (issues.Count > listed)
                text.AppendLine().AppendLine().Append($"其餘 {issues.Count - listed} 筆請看 Console。");
            return text.ToString();
        }

        /// <summary>
        /// 把每一筆問題連同代碼記到 Console。點擊訊息可以定位到相關的畫面物件。
        /// </summary>
        internal static void LogIssues(UIRoot root, IEnumerable<UIIssue> issues)
        {
            foreach (var issue in issues)
            {
                var context = issue.Context ? issue.Context : root;
                if (issue.Severity == UIIssueSeverity.Error)
                    Debug.LogError("UniGameUI " + issue, context);
                else
                    Debug.LogWarning("UniGameUI " + issue, context);
            }
        }
    }
}
