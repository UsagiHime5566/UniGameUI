using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// 自動走訪：進入 Play Mode，把藍圖上的每條路由實際觸發一次，確認走得通，回到 Edit Mode 之後寫出報告。
    /// 走訪會執行頁面腳本的生命週期方法，不傳資料、不模擬輸入、不檢查外觀。
    /// </summary>
    [InitializeOnLoad]
    public static partial class UIWalk
    {
        private const string SessionKey = "UniGameUI.Walk.Session";
        private const string LastReportKey = "UniGameUI.Walk.LastReport";
        // 命令列啟動後、還沒開始之前。這時也已經存在 SessionState 裡，並且有期限。
        private const string PhasePending = "Pending";
        private const string PhaseEntering = "Entering";
        private const string PhaseWalking = "Walking";
        private const string PhaseLeaving = "Leaving";
        private const string IncludeEmitArgument = "-unigameui-include-emit";
        private const string StepTimeoutArgument = "-unigameui-step-timeout";
        private const string TimeoutArgument = "-unigameui-timeout";
        private const float DefaultCommandLineTimeout = 300f;
        // 命令列啟動之後先讓 Editor 跑幾次更新再開始：Editor 自己排在啟動時的工作這時才會做完。
        private const int CommandLineStartTicks = 5;

        // 測試用：進入 Play Mode 之後先等幾秒再開始走。Domain 重新載入之後，測試要一點時間才接得上，
        // 會記錯誤的案例要等它準備好。
        internal const string WarmUpSecondsKey = "UniGameUI.Walk.WarmUpSeconds";
        // 測試用：命令列的走訪結束時不要結束 Editor；命令列排定之後至少等幾秒才開始。
        internal const string NoExitKey = "UniGameUI.Walk.NoExitForTests";
        internal const string StartDelaySecondsKey = "UniGameUI.Walk.StartDelaySecondsForTests";
        private const double EnterPlayModeGraceSeconds = 30;

        // 走訪從 Edit Mode 開始、在 Play Mode 進行、回到 Edit Mode 才結束，途中 Domain 可能重新載入，
        // 所以進行中的狀態存在 SessionState 裡。
        [Serializable]
        private sealed class Session
        {
            public string phase = "";
            public string scenePath = "";
            public string sceneName = "";
            public bool includeEmit;
            public float stepTimeout = 10f;
            public string reportPath = "";
            public bool commandLine;
            public bool fromWindow;
            // 命令列指定的整體時間上限（秒）。排定之後、真正開始時要用。
            public float timeout;
            public double startedAt;
            public double deadline;
            public UIWalkReport report = new UIWalkReport();
        }

        internal sealed class CommandLine
        {
            public readonly UIWalkOptions Options = new UIWalkOptions();
            public string ScenePath;
            public float TimeoutSeconds = DefaultCommandLineTimeout;
        }

        private static Session session;
        private static Walker walker;
        private static UIWalkReport lastReport;
        private static int pendingTicks;

        static UIWalk()
        {
            session = LoadSession();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.update += Watch;
        }

        /// <summary>
        /// 走訪正在進行：從 <see cref="Start"/> 到回到 Edit Mode、寫出報告為止。
        /// </summary>
        public static bool IsRunning => session != null;

        /// <summary>
        /// 這個 Editor 工作階段最後一次走訪的結果。還沒有走訪過時是 null。
        /// </summary>
        public static UIWalkReport LastReport
        {
            get
            {
                if (lastReport == null)
                {
                    var json = SessionState.GetString(LastReportKey, "");
                    if (json.Length > 0)
                        lastReport = JsonUtility.FromJson<UIWalkReport>(json);
                }
                return lastReport;
            }
        }

        /// <summary>
        /// 走訪結束、回到 Edit Mode 並寫出報告之後觸發。進入 Play Mode 時如果重新載入 Domain，
        /// 在那之前訂閱的方法會消失；這種情況請改讀 <see cref="LastReport"/> 或報告檔案。
        /// </summary>
        public static event Action<UIWalkReport> Finished;

        /// <summary>
        /// 對目前作用中的 Scene 走訪。要在 Edit Mode 呼叫；方法會立刻返回，走訪在之後的 Play Mode 進行。
        /// Validate 有 Error 或 Scene 沒有工作區時不進 Play Mode，結果是 NotRun。
        /// </summary>
        public static void Start(UIWalkOptions options = null)
        {
            Start(options, false, false, 0f);
        }

        internal static void StartFromWindow()
        {
            Start(null, false, true, 0f);
        }

        private static void Start(UIWalkOptions options, bool commandLine, bool fromWindow, float timeoutSeconds)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("請在 Edit Mode 開始走訪。");
            if (session != null)
                throw new InvalidOperationException("已經有一次走訪在進行。");
            options ??= new UIWalkOptions();
            if (!(options.StepTimeoutSeconds > 0) || float.IsInfinity(options.StepTimeoutSeconds))
                throw new ArgumentException("StepTimeoutSeconds 必須是大於 0 的秒數。");

            var scene = SceneManager.GetActiveScene();
            var starting = new Session
            {
                scenePath = scene.path ?? "",
                sceneName = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name,
                includeEmit = options.IncludeEmit,
                stepTimeout = options.StepTimeoutSeconds,
                reportPath = options.ReportPath ?? "",
                commandLine = commandLine,
                fromWindow = fromWindow,
                startedAt = EditorApplication.timeSinceStartup,
                report = NewReport(scene.path)
            };

            UIRoot root;
            try
            {
                root = UIAuthoring.FindWorkspace(scene);
            }
            catch (InvalidOperationException exception)
            {
                NotRun(starting, exception.Message);
                return;
            }
            if (!root)
            {
                NotRun(starting, "這個 Scene 沒有 UniGameUI 工作區。");
                return;
            }

            // 1. 先執行 Validate。有 Error 就不進 Play Mode。
            var issues = UIValidation.Check(root);
            var validation = starting.report.validation;
            validation.errorCount = issues.Count(issue => issue.Severity == UIIssueSeverity.Error);
            validation.warningCount = issues.Count - validation.errorCount;
            validation.issues = issues.Select(UIValidation.IssueJson.From).ToList();
            if (validation.errorCount > 0)
            {
                NotRun(starting, $"Validate 有 {validation.errorCount} 筆 Error，請先解決再走訪。");
                return;
            }

            // 2. 有待套用到 Prefab 的覆寫時不詢問，直接使用已儲存的 Prefab。
            var pending = UIReviewPlayMode.PendingReviews().Select(entry => entry.Id).Distinct().ToArray();
            if (pending.Length > 0)
            {
                starting.report.notes.Add("下列畫面有待套用到 Prefab 的覆寫，走訪使用的是已儲存的 Prefab：" +
                    string.Join("、", pending) + "。");
            }

            // 整體的時間上限。命令列由參數決定；其他情況給一個寬鬆的上限，避免頁面腳本卡住時永遠不結束。
            var limit = timeoutSeconds > 0
                ? timeoutSeconds
                : (root.Routes.Count + 2) * (options.StepTimeoutSeconds + 5f) + 60f;
            starting.deadline = starting.startedAt + limit;
            starting.phase = PhaseEntering;
            session = starting;
            SaveSession();
            EditorApplication.EnterPlaymode();
        }

        /// <summary>
        /// 命令列入口。不要加 -quit：走訪結束、回到 Edit Mode 之後會自己結束 Editor，
        /// 並用結束代碼表示結果（0 通過、1 有步驟失敗、2 沒有執行）。
        /// </summary>
        public static void RunFromCommandLine()
        {
            StartFromCommandLine(Environment.GetCommandLineArgs());
        }

        // 解析參數並排定開始。和 RunFromCommandLine 分開，測試才能直接給參數。
        internal static void StartFromCommandLine(string[] commandLine)
        {
            // 參數寫錯時仍然盡量把報告寫到指定的位置。
            var starting = new Session
            {
                commandLine = true,
                sceneName = "Untitled",
                reportPath = UIValidation.FindReportPath(commandLine) ?? "",
                report = NewReport("")
            };
            try
            {
                var arguments = ParseArguments(commandLine);
                starting.scenePath = arguments.ScenePath;
                starting.sceneName = Path.GetFileNameWithoutExtension(arguments.ScenePath);
                starting.report = NewReport(arguments.ScenePath);
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(arguments.ScenePath))
                {
                    throw new FileNotFoundException(
                        $"找不到 Scene「{arguments.ScenePath}」。請使用專案裡的路徑，例如 Assets/Scenes/Main.unity。");
                }

                // 先讓 Editor 跑幾次更新再開始（見 CommandLineStartTicks）。這段時間可能發生 Domain Reload，
                // 所以「等一下要開始」存在 SessionState 裡，整體的時間上限也從現在開始算：
                // 不論之後發生什麼，批次模式的 Editor 都會結束。
                starting.includeEmit = arguments.Options.IncludeEmit;
                starting.stepTimeout = arguments.Options.StepTimeoutSeconds;
                starting.timeout = arguments.TimeoutSeconds;
                starting.startedAt = EditorApplication.timeSinceStartup;
                starting.deadline = starting.startedAt + arguments.TimeoutSeconds;
                starting.phase = PhasePending;
                pendingTicks = 0;
                session = starting;
                SaveSession();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                NotRun(starting, exception.Message);
            }
        }

        // 命令列排定的走訪：開啟 Scene 並真正開始。
        private static void BeginPending()
        {
            var pending = session;
            session = null;
            SessionState.EraseString(SessionKey);
            try
            {
                EditorSceneManager.OpenScene(pending.scenePath, OpenSceneMode.Single);
                Start(new UIWalkOptions
                {
                    IncludeEmit = pending.includeEmit,
                    StepTimeoutSeconds = pending.stepTimeout,
                    ReportPath = string.IsNullOrEmpty(pending.reportPath) ? null : pending.reportPath
                }, true, false, pending.timeout);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                NotRun(pending, exception.Message);
            }
        }

        // 測試用：模擬走訪在某個階段時收到 Play Mode 狀態改變的通知。
        internal static void SimulatePlayModeChangeForTests(string phase, PlayModeStateChange change, string reportPath)
        {
            session = new Session
            {
                phase = phase,
                sceneName = "Test",
                reportPath = reportPath ?? "",
                startedAt = EditorApplication.timeSinceStartup,
                report = NewReport("")
            };
            OnPlayModeChanged(change);
        }

        /// <summary>
        /// 這筆 Console 訊息是不是 Unity Editor 自己的問題：堆疊裡每一行都在 UnityEditor 或 System 命名空間，
        /// 例如 Editor 建立搜尋索引失敗。它們和專案的 UI 無關，不算在走訪的步驟上。
        /// 專案的程式或這個套件記的訊息，堆疊裡一定有別的命名空間；沒有堆疊的訊息照常計算。
        /// </summary>
        internal static bool IsEditorInternalLog(string stackTrace)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
                return false;

            var fromEditor = false;
            foreach (var raw in stackTrace.Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("at ", StringComparison.Ordinal))
                    line = line.Substring(3);
                if (line.Length == 0 || line.StartsWith("(", StringComparison.Ordinal))
                    continue;
                if (line.StartsWith("UnityEditor.", StringComparison.Ordinal))
                {
                    fromEditor = true;
                    continue;
                }
                // System 是 .NET 自己的方法；後面幾個是 Debug.Log 本身留下的堆疊。
                if (line.StartsWith("System.", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.Debug", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.Logger", StringComparison.Ordinal) ||
                    line.StartsWith("UnityEngine.StackTraceUtility", StringComparison.Ordinal))
                    continue;
                return false;
            }
            return fromEditor;
        }

        // 測試用：案例失敗時把沒有結束的走訪丟掉，不寫報告。
        internal static void ResetForTests()
        {
            if (walker != null)
            {
                walker.Stop();
                walker = null;
            }
            session = null;
            SessionState.EraseString(SessionKey);
        }

        // 0：Passed。1：Failed。2：NotRun。
        internal static int ExitCodeFor(UIWalkReport report)
        {
            if (report == null)
                return 2;
            return report.Outcome switch
            {
                UIWalkOutcome.Passed => 0,
                UIWalkOutcome.Failed => 1,
                _ => 2
            };
        }

        internal static CommandLine ParseArguments(string[] arguments)
        {
            var parsed = new CommandLine();
            for (var index = 0; index < arguments.Length; index++)
            {
                switch (arguments[index])
                {
                    case UIValidation.SceneArgument:
                        if (parsed.ScenePath != null)
                            throw new ArgumentException("走訪一次只能指定一個 Scene。");
                        parsed.ScenePath = UIValidation.ArgumentValue(arguments, ref index);
                        break;
                    case UIValidation.ReportArgument:
                        parsed.Options.ReportPath = UIValidation.ArgumentValue(arguments, ref index);
                        break;
                    case IncludeEmitArgument:
                        parsed.Options.IncludeEmit = true;
                        break;
                    case StepTimeoutArgument:
                        parsed.Options.StepTimeoutSeconds = Seconds(arguments, ref index);
                        break;
                    case TimeoutArgument:
                        parsed.TimeoutSeconds = Seconds(arguments, ref index);
                        break;
                    default:
                        if (arguments[index].StartsWith("-unigameui-", StringComparison.Ordinal))
                            throw new ArgumentException($"走訪不認得參數「{arguments[index]}」。");
                        break;
                }
            }
            if (string.IsNullOrEmpty(parsed.ScenePath))
                throw new ArgumentException($"走訪需要用 {UIValidation.SceneArgument} 指定一個 Scene。");
            return parsed;
        }

        private static float Seconds(string[] arguments, ref int index)
        {
            var name = arguments[index];
            var text = UIValidation.ArgumentValue(arguments, ref index);
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
                !(seconds > 0) || float.IsInfinity(seconds))
                throw new ArgumentException($"參數「{name}」的值「{text}」必須是大於 0 的秒數。");
            return seconds;
        }

        private static UIWalkReport NewReport(string scenePath)
        {
            return new UIWalkReport { version = UIValidation.PackageVersion, scenePath = scenePath ?? "" };
        }

        private static string ReportPath(Session finished)
        {
            if (!string.IsNullOrEmpty(finished.reportPath))
                return finished.reportPath;
            var name = finished.sceneName;
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return $"Library/UniGameUI/walk-{name}.json";
        }

        private static Session LoadSession()
        {
            var json = SessionState.GetString(SessionKey, "");
            return json.Length == 0 ? null : JsonUtility.FromJson<Session>(json);
        }

        private static void SaveSession()
        {
            SessionState.SetString(SessionKey, JsonUtility.ToJson(session));
        }

        private static void NotRun(Session target, string reason)
        {
            target.report.outcome = UIWalkOutcome.NotRun.ToString();
            target.report.reason = reason;
            Conclude(target);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachBeforeStart(scene);
        }

        // 進入 Play Mode 時不重新載入 Scene 的專案（Enter Play Mode Options 關閉 Reload Scene）不會有 sceneLoaded。
        // 這個時間點一樣在 Awake、OnEnable 之後、Start 之前，所以起始頁的顯示與它記的訊息都接得到。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            if (session == null || session.phase != PhaseEntering || walker != null)
                return;
            AttachBeforeStart(string.IsNullOrEmpty(session.scenePath)
                ? SceneManager.GetActiveScene()
                : SceneManager.GetSceneByPath(session.scenePath));
        }

        private static void AttachBeforeStart(Scene scene)
        {
            // 這時 Scene 裡的物件已經 Awake，但還沒 Start。起始頁是工作區在 Start 顯示的，所以要趕在它之前接上。
            if (session == null || session.phase != PhaseEntering || !Application.isPlaying || walker != null)
                return;
            if (!scene.IsValid() ||
                (!string.IsNullOrEmpty(session.scenePath) && scene.path != session.scenePath))
                return;
            try
            {
                var root = UIAuthoring.FindWorkspace(scene);
                if (root)
                {
                    walker = new Walker(session);
                    walker.Attach(root);
                }
            }
            catch (InvalidOperationException)
            {
                // 進入 Play Mode 之後再回報。
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (session == null)
                return;
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (session.phase == PhaseEntering)
                        Begin();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    if (session.phase == PhaseWalking)
                        Interrupt("走訪還沒結束，Play Mode 就被停止了。");
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (session.phase == PhaseEntering)
                    {
                        // 還沒進 Play Mode 就回到 Edit Mode（例如別的工具取消了進入）：一步都沒有走，不可以算通過。
                        NotRun(session, "還沒進入 Play Mode 就回到 Edit Mode 了，可能是別的工具取消了進入 Play Mode。");
                        break;
                    }
                    if (session.phase == PhaseWalking)
                        Interrupt("走訪途中 Editor 離開了 Play Mode。");
                    Conclude(session);
                    break;
            }
        }

        private static void Begin()
        {
            session.phase = PhaseWalking;
            SaveSession();

            UIRoot root = null;
            string problem = null;
            try
            {
                var scene = string.IsNullOrEmpty(session.scenePath)
                    ? SceneManager.GetActiveScene()
                    : SceneManager.GetSceneByPath(session.scenePath);
                root = UIAuthoring.FindWorkspace(scene);
            }
            catch (InvalidOperationException exception)
            {
                problem = exception.Message;
            }

            walker ??= new Walker(session);
            walker.Run(root, problem);
        }

        // 走訪在 Play Mode 裡走完了：離開 Play Mode，回到 Edit Mode 之後才寫報告。
        private static void Complete(Walker finished)
        {
            if (session == null || finished != walker)
                return;
            session.report = finished.Report;
            session.phase = PhaseLeaving;
            SaveSession();
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            else
                Conclude(session);
        }

        // 走訪沒有走完就要結束：保留已經完成的步驟，結果是 NotRun。
        private static void Interrupt(string reason)
        {
            if (walker != null)
            {
                walker.Stop();
                session.report = walker.Report;
            }
            session.report.outcome = UIWalkOutcome.NotRun.ToString();
            session.report.reason = reason;
            session.phase = PhaseLeaving;
            session.deadline = 0;
            SaveSession();
        }

        private static void Watch()
        {
            if (session == null)
                return;
            if (session.deadline > 0 && EditorApplication.timeSinceStartup > session.deadline)
            {
                var seconds = Math.Round(session.deadline - session.startedAt);
                Interrupt($"走訪超過整體的時間上限（{seconds} 秒）。");
                // 命令列不等 Play Mode 結束，直接寫報告並結束 Editor，避免批次工作卡住。
                if (session.commandLine || !EditorApplication.isPlaying)
                    Conclude(session);
                else
                    EditorApplication.ExitPlaymode();
                return;
            }
            if (session.phase == PhasePending)
            {
                var delay = SessionState.GetFloat(StartDelaySecondsKey, 0f);
                if (++pendingTicks >= CommandLineStartTicks &&
                    EditorApplication.timeSinceStartup - session.startedAt >= delay)
                    BeginPending();
                return;
            }
            // 走訪期間 Editor 被暫停（Console 開著 Error Pause 時的 Debug.LogError、頁面腳本呼叫 Debug.Break）：
            // 讓它繼續，否則會停到整體逾時，結果變成 NotRun 而不是那一步失敗。
            if (session.phase == PhaseWalking && EditorApplication.isPaused)
            {
                EditorApplication.isPaused = false;
                const string note = "走訪期間 Editor 被暫停過（例如 Console 的 Error Pause），走訪自動讓它繼續。";
                if (!session.report.notes.Contains(note))
                    session.report.notes.Add(note);
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // 已經不在 Play Mode，卻沒有收到結束的通知。
            if (session.phase == PhaseEntering)
            {
                if (EditorApplication.timeSinceStartup - session.startedAt > EnterPlayModeGraceSeconds)
                    NotRun(session, "沒辦法進入 Play Mode。請先確認專案沒有編譯錯誤。");
                return;
            }
            if (session.phase == PhaseWalking)
                Interrupt("走訪途中 Editor 離開了 Play Mode。");
            Conclude(session);
        }

        // 結算、寫報告、通知。之後這一次走訪就結束了。
        private static void Conclude(Session finished)
        {
            if (walker != null)
            {
                walker.Stop();
                walker = null;
            }
            session = null;
            SessionState.EraseString(SessionKey);

            var report = finished.report;
            foreach (var step in report.steps.Where(step => string.IsNullOrEmpty(step.status)))
            {
                step.status = UIWalkStepStatus.Failed.ToString();
                step.reason = "走訪在這一步中斷。";
            }
            report.passedCount = report.steps.Count(step => step.Status == UIWalkStepStatus.Passed);
            report.failedCount = report.steps.Count(step => step.Status == UIWalkStepStatus.Failed);
            report.skippedCount = report.steps.Count(step => step.Status == UIWalkStepStatus.Skipped);
            if (string.IsNullOrEmpty(report.outcome))
            {
                report.outcome = (report.failedCount > 0 ? UIWalkOutcome.Failed : UIWalkOutcome.Passed).ToString();
            }

            var json = report.ToJson();
            var path = ReportPath(finished);
            try
            {
                UIValidation.WriteReport(path, json);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            lastReport = report;
            SessionState.SetString(LastReportKey, json);

            Debug.Log(Summary(report) + $"\n報告：{path}");
            foreach (var step in report.steps.Where(step => step.Status == UIWalkStepStatus.Failed))
                Debug.LogError($"UniGameUI 走訪：{Describe(step)} 失敗。{step.reason}");
            if (report.Outcome == UIWalkOutcome.NotRun)
                Debug.LogError($"UniGameUI 走訪沒有執行：{report.reason}");

            try
            {
                Finished?.Invoke(report);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            if (finished.fromWindow && !Application.isBatchMode)
                EditorUtility.DisplayDialog("UniGameUI 走訪", Summary(report), "關閉");
            if (finished.commandLine && !SessionState.GetBool(NoExitKey, false))
                EditorApplication.Exit(ExitCodeFor(report));
        }

        internal static string Describe(UIWalkStep step)
        {
            if (step.kind == UIWalkStep.StartKind)
                return $"起始頁「{step.target}」";
            var target = step.target.Length > 0 ? $" → {step.target}" : "";
            return $"「{step.source}/{step.action}」（{step.kind}{target}）";
        }

        // 藍圖視窗與 Console 顯示的摘要：通過、失敗、略過各幾步，以及失敗步驟的來源、動作與原因。
        internal static string Summary(UIWalkReport report)
        {
            var text = new StringBuilder();
            var outcome = report.Outcome switch
            {
                UIWalkOutcome.Passed => "通過",
                UIWalkOutcome.Failed => "有步驟失敗",
                _ => "沒有執行"
            };
            text.Append($"UniGameUI 走訪：{outcome}。通過 {report.passedCount} 步、失敗 {report.failedCount} 步、" +
                $"略過 {report.skippedCount} 步。");
            if (!string.IsNullOrEmpty(report.reason))
                text.AppendLine().Append(report.reason);
            foreach (var step in report.steps.Where(step => step.Status == UIWalkStepStatus.Failed))
                text.AppendLine().Append($"失敗：{Describe(step)} {step.reason}");
            foreach (var note in report.notes)
                text.AppendLine().Append(note);
            return text.ToString();
        }
    }
}
