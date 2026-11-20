using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniGameUI.Editor
{
    /// <summary>
    /// 自動走訪的選項。
    /// </summary>
    public sealed class UIWalkOptions
    {
        /// <summary>也觸發 Emit 路由。預設略過，所以頁面腳本的 OnAction 預設不會被呼叫。</summary>
        public bool IncludeEmit;

        /// <summary>每一步從準備到穩定的時間上限（秒）。</summary>
        public float StepTimeoutSeconds = 10f;

        /// <summary>報告的路徑。null 時寫到 Library/UniGameUI/walk-&lt;Scene 名稱&gt;.json。</summary>
        public string ReportPath;
    }

    public enum UIWalkOutcome
    {
        /// <summary>沒有失敗的步驟。可以有略過的步驟。</summary>
        Passed,

        /// <summary>至少有一步失敗。</summary>
        Failed,

        /// <summary>沒有走完：Validate 有 Error、沒有工作區、整體逾時，或走訪被中斷。</summary>
        NotRun
    }

    public enum UIWalkStepStatus { Passed, Failed, Skipped }

    // 以下的型別直接序列化成報告的 JSON，所以欄位用 JSON 的名稱，另外提供唯讀屬性給 C# 使用。

    /// <summary>
    /// 一步期間 Console 出現的一筆訊息。
    /// </summary>
    [Serializable]
    public sealed class UIWalkLog
    {
        [SerializeField] internal string type;
        [SerializeField] internal string message;
        [SerializeField] internal string note = "";

        /// <summary>Error、Exception、Assert 或 Warning。</summary>
        public string Type => type;

        public string Message => message;

        /// <summary>
        /// 這筆訊息不算進這一步的判定時，這裡是原因，例如它來自準備時被放棄的候選頁面。平常是空字串。
        /// </summary>
        public string Note => note;
    }

    /// <summary>
    /// 走訪的一步。第一步是「起始」（<see cref="Kind"/> 是 Start），之後每條路由一步。
    /// </summary>
    [Serializable]
    public sealed class UIWalkStep
    {
        internal const string StartKind = "Start";

        [SerializeField] internal string source = "";
        [SerializeField] internal string action = "";
        [SerializeField] internal string kind = "";
        [SerializeField] internal string target = "";
        [SerializeField] internal string status = "";
        [SerializeField] internal string result = "";
        [SerializeField] internal string reason = "";
        [SerializeField] internal float seconds;
        [SerializeField] internal List<UIWalkLog> logs = new List<UIWalkLog>();
        [SerializeField] internal int logsOmitted;

        public string Source => source;
        public string Action => action;

        /// <summary>路由的種類；起始那一步是 Start。</summary>
        public string Kind => kind;

        public string Target => target;
        public UIWalkStepStatus Status => UIWalkReport.Parse(status, UIWalkStepStatus.Failed);

        /// <summary>觸發路由得到的導航結果，例如 Completed。沒有觸發時是空字串。</summary>
        public string Result => result;

        /// <summary>失敗或略過的原因。通過時是空字串。</summary>
        public string Reason => reason;

        public float Seconds => seconds;

        /// <summary>
        /// 這一步期間 Console 的 Error、Exception 與 Warning。最多 50 筆，Error 與 Exception 優先保留；
        /// 沒有列出來的筆數是 <see cref="LogsOmitted"/>。步驟的結果看的是全部的訊息，不受這個上限影響。
        /// </summary>
        public IReadOnlyList<UIWalkLog> Logs => logs;

        /// <summary>這一步期間還有幾筆訊息沒有列在 <see cref="Logs"/> 裡。</summary>
        public int LogsOmitted => logsOmitted;
    }

    /// <summary>
    /// 藍圖上的一個畫面在走訪期間有沒有顯示過。
    /// </summary>
    [Serializable]
    public sealed class UIWalkView
    {
        [SerializeField] internal string id = "";
        [SerializeField] internal string kind = "";
        [SerializeField] internal bool visited;
        [SerializeField] internal string reason = "";

        public string Id => id;

        /// <summary>Page 或 Popup。</summary>
        public string Kind => kind;

        public bool Visited => visited;

        /// <summary>沒有顯示過的原因。</summary>
        public string Reason => reason;
    }

    /// <summary>
    /// 走訪之前對這個 Scene 執行 Validate 的結果。
    /// </summary>
    [Serializable]
    public sealed class UIWalkValidation
    {
        [SerializeField] internal int errorCount;
        [SerializeField] internal int warningCount;
        [SerializeField] internal List<UIValidation.IssueJson> issues = new List<UIValidation.IssueJson>();

        public int ErrorCount => errorCount;
        public int WarningCount => warningCount;
    }

    /// <summary>
    /// 自動走訪的結果，也是寫出的 JSON 報告的內容。
    /// </summary>
    [Serializable]
    public sealed class UIWalkReport
    {
        public const string ToolName = "UniGameUI.Walk";

        [SerializeField] internal string tool = ToolName;
        [SerializeField] internal string version = "";
        [SerializeField] internal string scenePath = "";
        [SerializeField] internal string outcome = "";
        [SerializeField] internal string reason = "";
        [SerializeField] internal int passedCount;
        [SerializeField] internal int failedCount;
        [SerializeField] internal int skippedCount;
        [SerializeField] internal List<UIWalkStep> steps = new List<UIWalkStep>();
        [SerializeField] internal List<UIWalkView> views = new List<UIWalkView>();
        [SerializeField] internal UIWalkValidation validation = new UIWalkValidation();
        [SerializeField] internal List<string> notes = new List<string>();

        public string ScenePath => scenePath;
        public UIWalkOutcome Outcome => Parse(outcome, UIWalkOutcome.NotRun);

        /// <summary>結果是 NotRun 時的原因。其他情況是空字串。</summary>
        public string Reason => reason;

        public int PassedCount => passedCount;
        public int FailedCount => failedCount;
        public int SkippedCount => skippedCount;
        public IReadOnlyList<UIWalkStep> Steps => steps;
        public IReadOnlyList<UIWalkView> Views => views;
        public UIWalkValidation Validation => validation;

        /// <summary>走訪時值得知道的事，例如有待套用的 Prefab 覆寫。</summary>
        public IReadOnlyList<string> Notes => notes;

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        internal static T Parse<T>(string value, T fallback) where T : struct, Enum
        {
            return Enum.TryParse<T>(value, out var parsed) ? parsed : fallback;
        }
    }
}
