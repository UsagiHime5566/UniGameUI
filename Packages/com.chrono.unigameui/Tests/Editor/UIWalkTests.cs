using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UniGameUI.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniGameUI.Editor.Tests
{
    // 第四批 4-5：自動走訪。這些案例會實際進出 Play Mode。
    // 進入 Play Mode 時 Domain 會重新載入，測試類別的欄位與區域變數都不會留下來，
    // 所以每個案例在走訪之後只從 UIWalk.LastReport 與報告檔案讀結果。
    public sealed class UIWalkTests
    {
        private const string Folder = "Assets/UniGameUI-WalkTests";
        private const string WidgetsFolder = Folder + "/Widgets";
        private const string TmpFolder = "Assets/TextMesh Pro";
        private const string ReportFile = "Temp/UniGameUI-walk-test.json";
        private const string HadTmpKey = "UniGameUI.Editor.Tests.Walk.HadTmp";
        private const string TmpRecordedKey = "UniGameUI.Editor.Tests.Walk.TmpRecorded";
        private const string OptionsKey = "UniGameUI.Editor.Tests.Walk.PlayModeOptions";
        private const string FinishedKey = "UniGameUI.Editor.Tests.Walk.Finished";

        // 開始走訪。Domain 重新載入時，測試從這個指令的下一行接著執行；不重新載入時等到走訪結束。
        private sealed class StartWalk : IEditModeTestYieldInstruction
        {
            private readonly UIWalkOptions options;

            public StartWalk(UIWalkOptions options)
            {
                this.options = options;
                ExpectDomainReload = !(EditorSettings.enterPlayModeOptionsEnabled &&
                    EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload));
            }

            public bool ExpectDomainReload { get; }
            public bool ExpectedPlaymodeState { get; private set; }

            public IEnumerator Perform()
            {
                yield return null;
                // 上一個案例如果在 Play Mode 裡失敗，Editor 可能還沒完全回到 Edit Mode。
                while (EditorApplication.isPlayingOrWillChangePlaymode)
                    yield return null;
                // 測試執行器鎖住了組件重載。和 EnterPlayMode 一樣，進 Play Mode 之前要先解開。
                EditorApplication.UnlockReloadAssemblies();
                UIWalk.Start(options);
                ExpectedPlaymodeState = UIWalk.IsRunning;
                while (UIWalk.IsRunning)
                    yield return null;
                EditorApplication.LockReloadAssemblies();
            }
        }

        // 等走訪結束、Editor 回到 Edit Mode。
        private sealed class WaitForWalk : IEditModeTestYieldInstruction
        {
            public bool ExpectDomainReload => false;
            public bool ExpectedPlaymodeState => false;

            public IEnumerator Perform()
            {
                var deadline = EditorApplication.timeSinceStartup + 150;
                while (UIWalk.IsRunning || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (EditorApplication.timeSinceStartup > deadline)
                        throw new TimeoutException("走訪沒有在時間內結束。");
                    yield return null;
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            UIWidgets.FolderOverride = null;
            UIWalk.ResetForTests();
            SessionState.EraseFloat(UIWalk.WarmUpSecondsKey);
            var saved = SessionState.GetString(OptionsKey, "");
            if (saved.Length > 0)
            {
                var parts = saved.Split('|');
                EditorSettings.enterPlayModeOptionsEnabled = parts[0] == "1";
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)int.Parse(parts[1]);
                SessionState.EraseString(OptionsKey);
            }
            SessionState.EraseBool(FinishedKey);
            if (File.Exists(ReportFile))
                File.Delete(ReportFile);
            // 案例失敗時 Editor 可能還在 Play Mode，這時不能換 Scene；測試執行器之後會結束 Play Mode。
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
        }

        [OneTimeTearDown]
        public void RemoveTmpResources()
        {
            if (SessionState.GetBool(TmpRecordedKey, false) && !SessionState.GetBool(HadTmpKey, true) &&
                AssetDatabase.IsValidFolder(TmpFolder))
                AssetDatabase.DeleteAsset(TmpFolder);
            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
            SessionState.EraseBool(HadTmpKey);
            SessionState.EraseBool(TmpRecordedKey);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator WalkJudgesEveryKindOfRouteAndSkipsEmitByDefault()
        {
            var root = NewWorkspace();
            Page(root, "home", UIWalkTestPage.Behaviour.LogWarningOnAction);
            Page(root, "settings");
            Popup(root, "tip");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "openSettings", UIRouteKind.ShowPage, "settings");
            UIAuthoring.Connect(root, "settings", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "home", "openTip", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, "tip", "close", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "tip", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "home", "ping", UIRouteKind.Emit);
            // 「任何畫面」的訊號路由也是一步，來源是 "*"。
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            AssertNoValidationErrors(root);

            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(report));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action} {step.Kind} {step.Target}"),
                Is.EqualTo(new[]
                {
                    "/ Start home", "home/openSettings ShowPage settings", "settings/back Back ",
                    "home/openTip OpenPopup tip", "tip/close ClosePopup ", "tip/back Back ", "home/ping Emit ",
                    "*/tick Emit "
                }));
            Assert.That(report.Steps.Take(6).Select(step => step.Status), Is.All.EqualTo(UIWalkStepStatus.Passed),
                Describe(report));
            Assert.That(report.Steps.Skip(1).Take(5).Select(step => step.Result), Is.All.EqualTo("Completed"));
            Assert.That(report.Steps.Select(step => step.Seconds), Is.All.GreaterThanOrEqualTo(0f));
            // Emit 預設略過，頁面腳本的 OnAction 沒有被呼叫。
            var emit = report.Steps[6];
            Assert.That(emit.Status, Is.EqualTo(UIWalkStepStatus.Skipped));
            Assert.That(emit.Result, Is.Empty);
            Assert.That(emit.Reason, Does.Contain("Emit").And.Contain("IncludeEmit"));
            Assert.That(emit.Logs, Is.Empty);
            Assert.That(report.Steps[7].Status, Is.EqualTo(UIWalkStepStatus.Skipped));
            Assert.That(report.Steps[7].Reason, Does.Contain("IncludeEmit"));
            Assert.That((report.PassedCount, report.FailedCount, report.SkippedCount), Is.EqualTo((6, 0, 2)));
            Assert.That(report.Views.Select(view => (view.Id, view.Kind, view.Visited)), Is.EquivalentTo(new[]
            {
                ("home", "Page", true), ("settings", "Page", true), ("tip", "Popup", true)
            }));
            Assert.That(report.Validation.ErrorCount, Is.EqualTo(0));
            Assert.That(UIWalk.ExitCodeFor(report), Is.EqualTo(0));
            Assert.That(EditorApplication.isPlaying, Is.False);

            // 報告檔案和 LastReport 是同一份內容，欄位名稱與文件一致。
            var json = File.ReadAllText(ReportFile);
            Assert.That(json, Is.EqualTo(report.ToJson()));
            var keys = Regex.Matches(json, "\"([A-Za-z]+)\":").Cast<Match>().Select(match => match.Groups[1].Value)
                .Distinct().ToArray();
            Assert.That(keys, Is.SupersetOf(new[]
            {
                "tool", "version", "scenePath", "outcome", "passedCount", "failedCount", "skippedCount", "steps",
                "source", "action", "kind", "target", "status", "result", "reason", "seconds", "logs",
                "views", "id", "visited", "validation", "errorCount", "warningCount", "issues"
            }));
            Assert.That(json, Does.Contain("\"tool\": \"UniGameUI.Walk\"").And.Contain("\"outcome\": \"Passed\""));

            // 開啟 IncludeEmit 之後 Emit 會觸發，頁面腳本收到動作。Scene 在走訪之後回到原本的樣子，可以再走一次。
            UIWidgets.FolderOverride = WidgetsFolder;
            yield return new StartWalk(new UIWalkOptions { IncludeEmit = true, ReportPath = ReportFile });
            yield return new WaitForWalk();

            var withEmit = UIWalk.LastReport;
            Assert.That(withEmit.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(withEmit));
            Assert.That((withEmit.PassedCount, withEmit.FailedCount, withEmit.SkippedCount), Is.EqualTo((8, 0, 0)));
            var triggered = withEmit.Steps[6];
            Assert.That(triggered.Status, Is.EqualTo(UIWalkStepStatus.Passed));
            Assert.That(triggered.Result, Is.EqualTo("Completed"));
            Assert.That(triggered.Logs.Select(log => (log.Type, log.Message.Contains("收到動作 ping"))),
                Is.EqualTo(new[] { ("Warning", true) }));
            // Emit 的訊號送到起始頁（準備時顯示的畫面）的 OnAction。
            var signaled = withEmit.Steps[7];
            Assert.That(signaled.Status, Is.EqualTo(UIWalkStepStatus.Passed));
            Assert.That(signaled.Result, Is.EqualTo("Completed"));
            Assert.That(signaled.Logs.Select(log => (log.Type, log.Message.Contains("「home」收到動作 tick"))),
                Is.EqualTo(new[] { ("Warning", true) }));
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator WalkCoversSignalRoutesAndChecksClearedHistory()
        {
            var root = NewWorkspace();
            Page(root, "home");
            Page(root, "challenger");
            Page(root, "lobby");
            Popup(root, "alert");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, UIRoot.AnySource, "joined", UIRouteKind.ShowPage, "challenger", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "alert", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "notice", UIRouteKind.OpenPopup, "alert");
            // 目標就是起始頁：準備時改從別的頁面出發。
            UIAuthoring.Connect(root, UIRoot.AnySource, "goHome", UIRouteKind.ShowPage, "home");
            UIAuthoring.Connect(root, "alert", "close", UIRouteKind.ClosePopup);
            // 一般路由的 Clear 也一樣確認。
            UIAuthoring.Connect(root, "home", "openLobby", UIRouteKind.ShowPage, "lobby", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, "home", "lock", UIRouteKind.OpenPopup, "alert", UIHistoryMode.Clear);
            AssertNoValidationErrors(root);

            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(report));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action} {step.Kind} {step.Target}={step.Status}"),
                Is.EqualTo(new[]
                {
                    "/ Start home=Passed", "*/joined ShowPage challenger=Passed", "*/lost OpenPopup alert=Passed",
                    "*/notice OpenPopup alert=Passed", "*/goHome ShowPage home=Passed", "alert/close ClosePopup =Passed",
                    "home/openLobby ShowPage lobby=Passed", "home/lock OpenPopup alert=Passed"
                }), Describe(report));
            Assert.That(report.Steps.Skip(1).Select(step => step.Result), Is.All.EqualTo("Completed"));
            Assert.That(report.Steps.SelectMany(step => step.Logs), Is.Empty, Describe(report));
            Assert.That((report.PassedCount, report.FailedCount, report.SkippedCount), Is.EqualTo((8, 0, 0)));
            Assert.That(report.Views.Where(view => view.Visited).Select(view => view.Id),
                Is.EquivalentTo(new[] { "home", "challenger", "lobby", "alert" }));
            Assert.That(File.ReadAllText(ReportFile), Does.Contain("\"source\": \"*\""));
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator WalkWaitsForNavigationsStartedByPageScripts()
        {
            var root = NewWorkspace();
            // 起始頁的腳本會開一個等結果的 Popup；Popup 一被關掉，它就自己開始切到 slow（有轉場）。
            // 走訪準備每一步時會關掉 Popup，所以下一個呼叫剛好遇到 Busy。
            Page(root, "timer", UIWalkTestPage.Behaviour.NavigateWhenPopupCloses, "slow", "ask");
            Page(root, "fine");
            Page(root, "end");
            UIAuthoring.SetTransition(Page(root, "slow"), true, UITransitionPreset.Fade, 0.5f);
            Popup(root, "ask");
            UIAuthoring.SetNavigatedByCode(root, "ask", true);
            UIAuthoring.SetStartPage(root, "timer");
            // 觸發時遇到 Busy：來源就是目前的頁面，準備不必切頁。
            UIAuthoring.Connect(root, "timer", "openFine", UIRouteKind.ShowPage, "fine");
            // 回到 timer，腳本再開一次 Popup。
            UIAuthoring.Connect(root, "fine", "back", UIRouteKind.Back);
            // 準備時遇到 Busy：要先切到來源頁。
            UIAuthoring.Connect(root, "fine", "openEnd", UIRouteKind.ShowPage, "end");
            AssertNoValidationErrors(root);

            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(report));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action}={step.Status}"), Is.EqualTo(new[]
            {
                "/=Passed", "timer/openFine=Passed", "fine/back=Passed", "fine/openEnd=Passed"
            }), Describe(report));
            Assert.That(report.Steps.Skip(1).Select(step => step.Result), Is.All.EqualTo("Completed"));
            // 兩步都真的遇到了頁面腳本自己的導航：警告是腳本在開始切頁之前記的。
            foreach (var index in new[] { 1, 3 })
            {
                Assert.That(report.Steps[index].Logs.Any(log => log.Type == "Warning" &&
                    log.Message.Contains("Popup 關閉後馬上自己切頁")), Is.True, Describe(report));
                // 等過 slow 的轉場才重試。
                Assert.That(report.Steps[index].Seconds, Is.GreaterThanOrEqualTo(0.4f), Describe(report));
            }
            Assert.That(report.Views.Single(view => view.Id == "slow").Visited, Is.True);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FailingStepsAreReportedAndTheWalkGoesOn()
        {
            var root = NewWorkspace();
            Page(root, "home");
            Page(root, "broken", UIWalkTestPage.Behaviour.ThrowOnShowing);
            Page(root, "noisy", UIWalkTestPage.Behaviour.LogErrorOnShown);
            Page(root, "warned", UIWalkTestPage.Behaviour.LogWarningOnShown);
            // 和操作指南的 LaunchPage 相同：顯示完成之後自己接著切到別頁。
            Page(root, "launch", UIWalkTestPage.Behaviour.NavigateOnShown, "home");
            Page(root, "fine");
            // 進場動畫比單步的時間上限還長。
            UIAuthoring.SetTransition(Page(root, "slow"), true, UITransitionPreset.Fade, 60f);
            // 沒有資料就顯示不出來，由程式開啟的 Popup。
            Popup(root, "needsData", UIWalkTestPage.Behaviour.RequireData);
            UIAuthoring.SetNavigatedByCode(root, "needsData", true);
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "openBroken", UIRouteKind.ShowPage, "broken");
            UIAuthoring.Connect(root, "home", "openNoisy", UIRouteKind.ShowPage, "noisy");
            UIAuthoring.Connect(root, "home", "openWarned", UIRouteKind.ShowPage, "warned");
            UIAuthoring.Connect(root, "home", "openLaunch", UIRouteKind.ShowPage, "launch");
            UIAuthoring.Connect(root, "home", "openFine", UIRouteKind.ShowPage, "fine");
            UIAuthoring.Connect(root, "needsData", "close", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "home", "openSlow", UIRouteKind.ShowPage, "slow");
            UIAuthoring.Connect(root, "fine", "back", UIRouteKind.Back);
            // 目標由程式控制、沒有資料就顯示不出來：一般路由與訊號路由都略過。
            UIAuthoring.Connect(root, "home", "openNeedsData", UIRouteKind.OpenPopup, "needsData");
            UIAuthoring.Connect(root, UIRoot.AnySource, "needsData", UIRouteKind.OpenPopup, "needsData");
            AssertNoValidationErrors(root);

            // 走訪期間頁面腳本會故意記錯誤，結束時失敗的步驟也會記到 Console，這個案例不檢查 Console。
            // Domain 重新載入之後要重新設定一次；讓走訪晚幾秒開始，等測試接上。
            SessionState.SetFloat(UIWalk.WarmUpSecondsKey, 5f);
            LogAssert.ignoreFailingMessages = true;
            yield return new StartWalk(new UIWalkOptions { StepTimeoutSeconds = 3f, ReportPath = ReportFile });
            LogAssert.ignoreFailingMessages = true;
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Failed), Describe(report));
            Assert.That(UIWalk.ExitCodeFor(report), Is.EqualTo(1));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action}={step.Status}"), Is.EqualTo(new[]
            {
                "/=Passed", "home/openBroken=Failed", "home/openNoisy=Failed", "home/openWarned=Passed",
                "home/openLaunch=Passed", "home/openFine=Passed", "needsData/close=Skipped",
                "home/openSlow=Failed", "fine/back=Passed", "home/openNeedsData=Skipped", "*/needsData=Skipped"
            }), Describe(report));
            Assert.That((report.PassedCount, report.FailedCount, report.SkippedCount), Is.EqualTo((5, 3, 3)));

            // 目標頁的 OnShowing 丟例外：這一步失敗，報告裡有例外訊息。
            var broken = report.Steps[1];
            Assert.That(broken.Result, Is.EqualTo("Failed"));
            Assert.That(broken.Reason, Does.Contain("Failed"));
            Assert.That(broken.Logs.Any(log => log.Type == "Exception" && log.Message.Contains("OnShowing 丟出例外")),
                Is.True, Describe(report));

            // 步驟期間有 Debug.LogError：導航本身完成了，這一步仍然失敗。
            var noisy = report.Steps[2];
            Assert.That(noisy.Result, Is.EqualTo("Completed"));
            Assert.That(noisy.Reason, Does.Contain("Console").And.Contain("OnShown 記了一筆錯誤"));
            Assert.That(noisy.Logs.Select(log => log.Type), Is.EqualTo(new[] { "Error" }));

            // 只有 Debug.LogWarning：通過，警告收在報告裡。
            var warned = report.Steps[3];
            Assert.That(warned.Reason, Is.Empty);
            Assert.That(warned.Logs.Select(log => (log.Type, log.Message.Contains("OnShown 記了一筆警告"))),
                Is.EqualTo(new[] { ("Warning", true) }));

            // 頁面腳本在 OnShown 之後接著導航：不影響這一步，也不影響後面的步驟。
            Assert.That(report.Steps[4].Result, Is.EqualTo("Completed"));
            Assert.That(report.Steps[4].Logs, Is.Empty);
            Assert.That(report.Steps[5].Logs, Is.Empty);

            // 由程式控制的來源畫面準備失敗：略過，原因是需要由程式提供資料。
            var needsData = report.Steps[6];
            Assert.That(needsData.Reason, Does.Contain("由程式控制").And.Contain("資料"));
            Assert.That(needsData.Result, Is.Empty);
            Assert.That(needsData.Logs.Any(log => log.Type == "Exception"), Is.True);

            // 目標畫面由程式控制而顯示失敗：略過，原因是需要由程式提供資料。導航的結果照實記錄。
            foreach (var skipped in new[] { report.Steps[9], report.Steps[10] })
            {
                Assert.That(skipped.Kind, Is.EqualTo("OpenPopup"));
                Assert.That(skipped.Result, Is.EqualTo("Failed"));
                Assert.That(skipped.Reason, Does.Contain("needsData").And.Contain("由程式控制").And.Contain("資料"));
                Assert.That(skipped.Logs.Any(log => log.Type == "Exception"), Is.True);
            }

            // 單步逾時：失敗，原因是逾時，接著繼續下一步。
            var slow = report.Steps[7];
            Assert.That(slow.Reason, Does.Contain("逾時"));
            Assert.That(slow.Seconds, Is.GreaterThanOrEqualTo(3f).And.LessThan(30f));
            Assert.That(report.Steps[8].Result, Is.EqualTo("Completed"));

            // 畫面清單：每個畫面有沒有顯示過，沒有的話是為什麼。
            var views = report.Views.ToDictionary(view => view.Id);
            Assert.That(views.Keys, Is.EquivalentTo(new[]
            {
                "home", "broken", "noisy", "warned", "launch", "fine", "slow", "needsData"
            }));
            Assert.That(views.Values.Where(view => view.Visited).Select(view => view.Id),
                Is.EquivalentTo(new[] { "home", "noisy", "warned", "launch", "fine" }));
            Assert.That(views["needsData"].Reason, Does.Contain("由程式控制"));
            Assert.That(views["broken"].Reason, Is.Not.Empty);
            Assert.That(views["home"].Reason, Is.Empty);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator SinglePageWorkspaceSkipsBackAndTheStartStepWithoutAStartPage()
        {
            var root = NewWorkspace();
            Page(root, "solo");
            Popup(root, "tip");
            // 由程式控制、而且沒有以它為來源的路由：不會被開啟，只列在畫面清單裡。
            Popup(root, "dialog", UIWalkTestPage.Behaviour.RequireData);
            UIAuthoring.SetNavigatedByCode(root, "dialog", true);
            UIAuthoring.Connect(root, "solo", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "solo", "openTip", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, "tip", "close", UIRouteKind.ClosePopup);
            AssertNoValidationErrors(root);

            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(report));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action}={step.Status}"), Is.EqualTo(new[]
            {
                "/=Skipped", "solo/back=Skipped", "solo/openTip=Passed", "tip/close=Passed"
            }), Describe(report));
            Assert.That(report.Steps[0].Kind, Is.EqualTo("Start"));
            Assert.That(report.Steps[0].Reason, Does.Contain("起始頁"));
            Assert.That(report.Steps[1].Reason, Does.Contain("只有一個頁面"));
            Assert.That((report.PassedCount, report.FailedCount, report.SkippedCount), Is.EqualTo((2, 0, 2)));
            var dialog = report.Views.Single(view => view.Id == "dialog");
            Assert.That(dialog.Visited, Is.False);
            Assert.That(dialog.Reason, Does.Contain("由程式控制").And.Contain("不在走訪範圍"));
            Assert.That(report.Views.Where(view => view.Visited).Select(view => view.Id),
                Is.EquivalentTo(new[] { "solo", "tip" }));
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator WalkWorksWithDomainReloadDisabled()
        {
            var root = NewWorkspace();
            Page(root, "home");
            Page(root, "next");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "go", UIRouteKind.ShowPage, "next");
            UIAuthoring.Connect(root, "next", "back", UIRouteKind.Back);
            AssertNoValidationErrors(root);

            SessionState.SetString(OptionsKey, (EditorSettings.enterPlayModeOptionsEnabled ? "1" : "0") + "|" +
                (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            // Domain 沒有重新載入，Finished 的訂閱會留著。
            UIWalk.Finished += RecordFinished;

            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            yield return new WaitForWalk();
            UIWalk.Finished -= RecordFinished;

            var report = UIWalk.LastReport;
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(report));
            Assert.That(report.Steps.Select(step => step.Status), Is.All.EqualTo(UIWalkStepStatus.Passed));
            Assert.That(report.Steps.Count, Is.EqualTo(3));
            Assert.That(SessionState.GetBool(FinishedKey, false), Is.True, "Finished 要在回到 Edit Mode 之後觸發");
        }

        // K1：警告再多，步驟裡的 Error 也不可以被丟掉。
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator ErrorsAreNotLostBehindManyWarnings()
        {
            var root = NewWorkspace();
            Page(root, "home");
            Page(root, "noisy", UIWalkTestPage.Behaviour.LogManyWarningsThenAnErrorOnShowing);
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "openNoisy", UIRouteKind.ShowPage, "noisy");
            UIAuthoring.Connect(root, "noisy", "back", UIRouteKind.Back);
            AssertNoValidationErrors(root);

            SessionState.SetFloat(UIWalk.WarmUpSecondsKey, 5f);
            LogAssert.ignoreFailingMessages = true;
            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            LogAssert.ignoreFailingMessages = true;
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Failed), Describe(report));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action}={step.Status}"), Is.EqualTo(new[]
            {
                "/=Passed", "home/openNoisy=Failed", "noisy/back=Failed"
            }), Describe(report));
            foreach (var step in report.Steps.Skip(1))
            {
                // 導航本身完成了；失敗是因為那筆 Error。
                Assert.That(step.Result, Is.EqualTo("Completed"));
                Assert.That(step.Reason, Does.Contain("Error").And.Contain("很多警告之後記了一筆錯誤"));
                // 報告最多留 50 筆：Error 一定在裡面，其餘是照順序的警告；沒有列出來的筆數另外寫。
                Assert.That(step.Logs.Count, Is.EqualTo(50));
                Assert.That(step.Logs.Count(log => log.Type == "Error"), Is.EqualTo(1));
                Assert.That(step.Logs.Last().Type, Is.EqualTo("Error"));
                Assert.That(step.Logs[0].Message, Does.Contain("第 1 筆警告"));
                Assert.That(step.Logs[48].Message, Does.Contain("第 49 筆警告"));
                Assert.That(step.LogsOmitted, Is.EqualTo(UIWalkTestPage.ManyWarnings + 1 - 50));
            }
            Assert.That(report.Steps[0].LogsOmitted, Is.Zero);
            Assert.That(File.ReadAllText(ReportFile), Does.Contain("\"logsOmitted\": 11"));
        }

        // K2：準備時先顯示的頁面不選由程式控制的；顯示不出來而被放棄的候選，它記的錯誤不算在這一步上。
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator PreparationCandidatesDoNotFailUnrelatedSteps()
        {
            var root = NewWorkspace();
            Page(root, "title");
            Page(root, "game");
            // 靠路由進出、但顯示不出來的頁面：以它為來源的步驟會失敗，別的步驟拿它當候選時不受影響。
            Page(root, "broken", UIWalkTestPage.Behaviour.ThrowOnShowing);
            // 由程式控制、需要資料的頁面：不會被拿來當候選。
            Page(root, "result", UIWalkTestPage.Behaviour.ThrowOnShowing);
            UIAuthoring.SetNavigatedByCode(root, "result", true);
            Popup(root, "pause");
            UIAuthoring.SetStartPage(root, "title");
            UIAuthoring.Connect(root, "broken", "go", UIRouteKind.ShowPage, "game");
            UIAuthoring.Connect(root, "result", "retry", UIRouteKind.ShowPage, "game");
            UIAuthoring.Connect(root, "result", "openPause", UIRouteKind.OpenPopup, "pause");
            UIAuthoring.Connect(root, "title", "play", UIRouteKind.ShowPage, "game");
            UIAuthoring.Connect(root, "game", "toTitle", UIRouteKind.ShowPage, "title");
            UIAuthoring.Connect(root, "game", "openPause", UIRouteKind.OpenPopup, "pause");
            UIAuthoring.Connect(root, "pause", "resume", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "game", "back", UIRouteKind.Back);
            AssertNoValidationErrors(root);

            SessionState.SetFloat(UIWalk.WarmUpSecondsKey, 5f);
            LogAssert.ignoreFailingMessages = true;
            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            LogAssert.ignoreFailingMessages = true;
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action}={step.Status}"), Is.EqualTo(new[]
            {
                "/=Passed", "broken/go=Failed", "result/retry=Skipped", "result/openPause=Skipped",
                "title/play=Passed", "game/toTitle=Passed", "game/openPause=Passed", "pause/resume=Passed",
                "game/back=Passed"
            }), Describe(report));

            // 來源是 game 的步驟：候選是 broken（有路由通到 game）與 title（起始頁），result 不在候選裡。
            // broken 顯示不出來，改用 title；broken 記的錯誤留在報告裡並註明，但這一步通過。
            foreach (var index in new[] { 5, 6, 8 })
            {
                var step = report.Steps[index];
                Assert.That(step.Reason, Is.Empty, Describe(report));
                Assert.That(step.Logs, Is.Not.Empty, Describe(report));
                Assert.That(step.Logs.Select(log => log.Note), Is.All.Contains("被放棄的候選頁面「broken」"));
                Assert.That(step.Logs.Any(log => log.Type == "Exception"), Is.True);
                Assert.That(step.Logs.Any(log => log.Message.Contains("「result」")), Is.False,
                    "由程式控制的頁面不應該被拿來試");
            }
            // 來源是 Popup 的步驟：底下的頁面選 game，不選由程式控制的 result。
            Assert.That(report.Steps[7].Logs.Any(log => log.Message.Contains("「result」")), Is.False);
            // 沒有用到候選的步驟，訊息沒有註記。
            Assert.That(report.Steps[1].Logs.Select(log => log.Note), Is.All.Empty);
            Assert.That(File.ReadAllText(ReportFile), Does.Contain("\"note\": \"準備時被放棄的候選頁面"));
        }

        // K2：來源是一般頁面的 Back，唯一的另一個頁面是開不起來的由程式控制頁面：略過，不是失敗。
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator BackIsSkippedWhenTheOnlyOtherPageNeedsDataFromCode()
        {
            var root = NewWorkspace();
            Page(root, "home");
            Page(root, "result", UIWalkTestPage.Behaviour.ThrowOnShowing);
            UIAuthoring.SetNavigatedByCode(root, "result", true);
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "back", UIRouteKind.Back);
            AssertNoValidationErrors(root);

            SessionState.SetFloat(UIWalk.WarmUpSecondsKey, 5f);
            LogAssert.ignoreFailingMessages = true;
            yield return new StartWalk(new UIWalkOptions { ReportPath = ReportFile });
            LogAssert.ignoreFailingMessages = true;
            yield return new WaitForWalk();

            var report = UIWalk.LastReport;
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.Passed), Describe(report));
            Assert.That(report.Steps.Select(step => $"{step.Source}/{step.Action}={step.Status}"), Is.EqualTo(new[]
            {
                "/=Passed", "home/back=Skipped"
            }), Describe(report));
            Assert.That(report.Steps[1].Reason, Does.Contain("由程式控制").And.Contain("Back"));
            Assert.That(report.Steps[1].Logs.Select(log => log.Note), Is.All.Contains("被放棄的候選頁面「result」"));
        }

        private static void RecordFinished(UIWalkReport report)
        {
            SessionState.SetBool(FinishedKey, !EditorApplication.isPlaying && !UIWalk.IsRunning &&
                ReferenceEquals(report, UIWalk.LastReport));
        }

        [Test]
        public void WalkDoesNotEnterPlayModeWhenValidationHasErrors()
        {
            var root = NewWorkspace();
            Page(root, "home");
            // 起始頁指到不存在的畫面：Validate 的 Error。
            root.startPageId = "ghost";

            UIWalkReport finished = null;
            void Remember(UIWalkReport report) => finished = report;
            UIWalk.Finished += Remember;
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("走訪沒有執行"));
                UIWalk.Start(new UIWalkOptions { ReportPath = ReportFile });
            }
            finally
            {
                UIWalk.Finished -= Remember;
            }

            Assert.That(UIWalk.IsRunning, Is.False);
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            var report = UIWalk.LastReport;
            Assert.That(finished, Is.SameAs(report));
            Assert.That(report.Outcome, Is.EqualTo(UIWalkOutcome.NotRun));
            Assert.That(report.Reason, Does.Contain("Validate").And.Contain("Error"));
            Assert.That(report.Steps, Is.Empty);
            Assert.That(report.Validation.ErrorCount, Is.EqualTo(1));
            Assert.That(UIWalk.ExitCodeFor(report), Is.EqualTo(2));
            // 報告附上 Validate 的結果。
            var json = File.ReadAllText(ReportFile);
            Assert.That(json, Does.Contain("\"outcome\": \"NotRun\"").And.Contain("\"code\": \"start.unknown\"")
                .And.Contain("\"errorCount\": 1"));

            // 沒有工作區的 Scene 也是 NotRun。
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LogAssert.Expect(LogType.Error, new Regex("走訪沒有執行"));
            UIWalk.Start(new UIWalkOptions { ReportPath = ReportFile });
            Assert.That(UIWalk.LastReport.Outcome, Is.EqualTo(UIWalkOutcome.NotRun));
            Assert.That(UIWalk.LastReport.Reason, Does.Contain("工作區"));
            Assert.That(UIWalk.IsRunning, Is.False);

            // 選項不合理時直接丟出例外。
            Assert.Throws<ArgumentException>(() => UIWalk.Start(new UIWalkOptions { StepTimeoutSeconds = 0f }));
        }

        [Test]
        public void WalkCommandLineArgumentsAndExitCodes()
        {
            var parsed = UIWalk.ParseArguments(new[]
            {
                "Unity.exe", "-batchmode", "-projectPath", "C:/Project", "-logFile", "-",
                "-executeMethod", "UniGameUI.Editor.UIWalk.RunFromCommandLine",
                "-unigameui-scene", "Assets/Scenes/Workshop.unity", "-unigameui-report", "Temp/walk.json",
                "-unigameui-include-emit", "-unigameui-step-timeout", "2.5", "-unigameui-timeout", "120"
            });
            Assert.That(parsed.ScenePath, Is.EqualTo("Assets/Scenes/Workshop.unity"));
            Assert.That(parsed.Options.ReportPath, Is.EqualTo("Temp/walk.json"));
            Assert.That(parsed.Options.IncludeEmit, Is.True);
            Assert.That(parsed.Options.StepTimeoutSeconds, Is.EqualTo(2.5f));
            Assert.That(parsed.TimeoutSeconds, Is.EqualTo(120f));

            // 預設值：Emit 略過、單步 10 秒、整體 300 秒、報告寫到預設位置。
            var defaults = UIWalk.ParseArguments(new[] { "Unity.exe", "-unigameui-scene", "Assets/A.unity" });
            Assert.That(defaults.Options.IncludeEmit, Is.False);
            Assert.That(defaults.Options.StepTimeoutSeconds, Is.EqualTo(10f));
            Assert.That(defaults.Options.ReportPath, Is.Null);
            Assert.That(defaults.TimeoutSeconds, Is.EqualTo(300f));

            // 參數錯誤：沒有 Scene、指定兩個 Scene、秒數不是正數、少了值、寫錯名稱。
            Assert.Throws<ArgumentException>(() => UIWalk.ParseArguments(new[] { "Unity.exe" }));
            Assert.Throws<ArgumentException>(() => UIWalk.ParseArguments(new[]
            {
                "Unity.exe", "-unigameui-scene", "Assets/A.unity", "-unigameui-scene", "Assets/B.unity"
            }));
            foreach (var seconds in new[] { "0", "-1", "soon" })
            {
                Assert.Throws<ArgumentException>(() => UIWalk.ParseArguments(new[]
                {
                    "Unity.exe", "-unigameui-scene", "Assets/A.unity", "-unigameui-step-timeout", seconds
                }), seconds);
                Assert.Throws<ArgumentException>(() => UIWalk.ParseArguments(new[]
                {
                    "Unity.exe", "-unigameui-scene", "Assets/A.unity", "-unigameui-timeout", seconds
                }), seconds);
            }
            Assert.Throws<ArgumentException>(() => UIWalk.ParseArguments(new[]
            {
                "Unity.exe", "-unigameui-scene", "Assets/A.unity", "-unigameui-timeout"
            }));
            Assert.That(Assert.Throws<ArgumentException>(() => UIWalk.ParseArguments(new[]
            {
                "Unity.exe", "-unigameui-scene", "Assets/A.unity", "-unigameui-strict"
            })).Message, Does.Contain("-unigameui-strict"));

            // 參數寫錯時仍然找得到報告的路徑。
            Assert.That(UIValidation.FindReportPath(new[] { "Unity.exe", "-unigameui-scene", "-unigameui-report", "Temp/out.json" }),
                Is.EqualTo("Temp/out.json"));
            Assert.That(UIValidation.FindReportPath(new[] { "Unity.exe", "-unigameui-report" }), Is.Null);
            Assert.That(UIValidation.FindReportPath(new[] { "Unity.exe", "-unigameui-strict" }), Is.Null);

            // 結束代碼：Passed 是 0，Failed 是 1，NotRun 或沒有報告是 2。
            Assert.That(UIWalk.ExitCodeFor(Outcome(UIWalkOutcome.Passed)), Is.EqualTo(0));
            Assert.That(UIWalk.ExitCodeFor(Outcome(UIWalkOutcome.Failed)), Is.EqualTo(1));
            Assert.That(UIWalk.ExitCodeFor(Outcome(UIWalkOutcome.NotRun)), Is.EqualTo(2));
            Assert.That(UIWalk.ExitCodeFor(null), Is.EqualTo(2));

            static UIWalkReport Outcome(UIWalkOutcome outcome)
            {
                return JsonUtility.FromJson<UIWalkReport>("{ \"outcome\": \"" + outcome + "\" }");
            }
        }

        [Test]
        public void ErrorsFromInsideTheUnityEditorDoNotCountAgainstAStep()
        {
            // 命令列實際執行時遇到的例子：Editor 啟動時建立搜尋索引失敗，和專案的 UI 無關。
            const string searchIndex =
                "System.Collections.Generic.List`1[T].get_Item (System.Int32 index) (at <41d29b352f6a475ab1bf7c6628b82790>:0)\n" +
                "UnityEditor.Search.SearchDatabase+<EnumerateAll>d__80.MoveNext () (at <5b34bb5dc7a045aaaa697e17baf62c78>:0)\n" +
                "System.Linq.Enumerable.First[TSource] (System.Collections.Generic.IEnumerable`1[T] source) (at <80564d689e48448fa2a7101993ee7201>:0)\n" +
                "UnityEditor.Search.SearchInit.IndexationOnStartup () (at <5b34bb5dc7a045aaaa697e17baf62c78>:0)\n" +
                "UnityEditor.EditorApplication.Internal_CallDelayFunctions () (at <cf2607b209aa47de987ba5ed55932aa8>:0)\n";
            Assert.That(UIWalk.IsEditorInternalLog(searchIndex), Is.True);
            Assert.That(UIWalk.IsEditorInternalLog(
                "  at System.Collections.Generic.List`1[T].get_Item (System.Int32 index) [0x00009] in <41d2>:0 \n" +
                "  at UnityEditor.Search.SearchInit.IndexationOnStartup () [0x0000c] in <5b34>:0 \n"), Is.True);
            Assert.That(UIWalk.IsEditorInternalLog(
                "UnityEngine.Debug:LogError (object)\nUnityEditor.SomeWindow:Refresh ()\n"), Is.True);

            // 專案的程式、這個套件，或沒有堆疊的訊息都照常計算。
            Assert.That(UIWalk.IsEditorInternalLog(
                "UnityEngine.Debug:LogError (object,UnityEngine.Object)\n" +
                "UniGameUI.Tests.UIWalkTestPage:OnShown () (at ./Packages/com.chrono.unigameui/Tests/Runtime/UIWalkTestPage.cs:38)\n" +
                "UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()\n"), Is.False);
            Assert.That(UIWalk.IsEditorInternalLog(
                "UnityEngine.Debug:LogError (object,UnityEngine.Object)\n" +
                "UniGameUI.UIRoot:NotFound (string,string) (at ./Packages/com.chrono.unigameui/Runtime/UIRoot.cs:417)\n"),
                Is.False);
            Assert.That(UIWalk.IsEditorInternalLog("System.InvalidOperationException\n  at Game.Shop.Open () [0x00001]"),
                Is.False);
            Assert.That(UIWalk.IsEditorInternalLog("System.Linq.Enumerable.First[TSource] ()\n"), Is.False);
            Assert.That(UIWalk.IsEditorInternalLog(""), Is.False);
            Assert.That(UIWalk.IsEditorInternalLog(null), Is.False);
        }

        private static UIRoot NewWorkspace()
        {
            if (!UIDOTweenSetup.IsReady)
                Assert.Ignore("此案例需要 DOTween，請先匯入並完成 Setup DOTween。");
            if (!SessionState.GetBool(TmpRecordedKey, false))
            {
                SessionState.SetBool(TmpRecordedKey, true);
                SessionState.SetBool(HadTmpKey, Directory.Exists(TmpFolder) || File.Exists(TmpFolder + ".meta"));
            }

            // 用沒有存檔的 Scene：存檔會產生 ID 常數的程式碼，測試途中不要觸發重新編譯。
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            UIWidgets.FolderOverride = WidgetsFolder;
            return UIAuthoring.CreateWorkspace(scene);
        }

        private static UIView Page(UIRoot root, string id,
            UIWalkTestPage.Behaviour behaviour = UIWalkTestPage.Behaviour.None, string target = null,
            string popup = null)
        {
            var view = UIAuthoring.CreatePage(root, id, id);
            AddScript(view, behaviour, target, popup);
            return view;
        }

        private static void Popup(UIRoot root, string id,
            UIWalkTestPage.Behaviour behaviour = UIWalkTestPage.Behaviour.None)
        {
            var view = UIAuthoring.CreatePopup(root, id, id, $"{Folder}/{id}.prefab");
            if (behaviour == UIWalkTestPage.Behaviour.None)
                return;
            // Popup 執行期用的是 Prefab，所以把腳本套用回去。
            AddScript(view, behaviour, null);
            UIAuthoring.ApplyReviewOverrides(root, id);
            Assert.That(UIAuthoring.FindEntry(root, id).Prefab.GetComponent<UIWalkTestPage>(), Is.Not.Null);
        }

        private static void AddScript(UIView view, UIWalkTestPage.Behaviour behaviour, string target,
            string popup = null)
        {
            if (behaviour == UIWalkTestPage.Behaviour.None)
                return;
            var script = view.gameObject.AddComponent<UIWalkTestPage>();
            script.behaviour = behaviour;
            script.target = target;
            script.popup = popup;
            EditorUtility.SetDirty(script);
        }

        private static void AssertNoValidationErrors(UIRoot root)
        {
            Assert.That(UIValidation.Check(root).Where(issue => issue.Severity == UIIssueSeverity.Error), Is.Empty);
        }

        private static string Describe(UIWalkReport report)
        {
            return report == null ? "沒有報告" : report.ToJson();
        }
    }
}
