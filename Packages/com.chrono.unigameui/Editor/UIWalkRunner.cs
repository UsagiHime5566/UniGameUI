using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace UniGameUI.Editor
{
    // 在 Play Mode 裡實際走訪的部分。
    public static partial class UIWalk
    {
        private sealed class Walker
        {
            // 連續幾個影格沒有導航在進行，才算穩定。
            private const int StableFrames = 3;
            // 逾時取消之後，給還在收尾的導航一點時間。
            private const double CancelGraceSeconds = 2;

            private readonly Session owner;
            private readonly HashSet<string> shown = new HashSet<string>();
            private readonly StepLogs logs = new StepLogs();
            private readonly Dictionary<string, UIEntry> entries = new Dictionary<string, UIEntry>();
            private readonly List<UIEntry> pages = new List<UIEntry>();
            private readonly HashSet<string> byCode = new HashSet<string>();
            private UIRoute[] routes = Array.Empty<UIRoute>();
            private UIRoot root;
            private bool attached;
            private bool stopped;

            public Walker(Session owner)
            {
                this.owner = owner;
                Report = owner.report;
            }

            public UIWalkReport Report { get; }

            private static double Now => Time.realtimeSinceStartupAsDouble;

            // 盡早接上工作區：起始頁在工作區的 Start 顯示，Console 的訊息也從這時開始收。
            public void Attach(UIRoot target)
            {
                if (attached || !target)
                    return;
                root = target;
                attached = true;
                root.ViewShown += OnViewShown;
                Application.logMessageReceived += OnLog;
                // 接上的時候已經顯示著的畫面也算顯示過。
                if (root.CurrentPage)
                    shown.Add(root.CurrentPageId);
                foreach (var popup in root.Popups)
                {
                    if (popup)
                        shown.Add(popup.Id);
                }
            }

            public void Stop()
            {
                stopped = true;
                Detach();
            }

            private void Detach()
            {
                if (!attached)
                    return;
                attached = false;
                Application.logMessageReceived -= OnLog;
                if (root)
                    root.ViewShown -= OnViewShown;
            }

            private void OnViewShown(UIView view)
            {
                if (view)
                    shown.Add(view.Id);
            }

            private void OnLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Log || IsEditorInternalLog(stackTrace))
                    return;
                logs.Add(type, condition);
            }

            public async void Run(UIRoot target, string problem)
            {
                try
                {
                    Attach(target);
                    if (!root)
                    {
                        Report.outcome = UIWalkOutcome.NotRun.ToString();
                        Report.reason = problem ?? "進入 Play Mode 之後找不到 UniGameUI 工作區。";
                        return;
                    }
                    await WalkAsync();
                }
                catch (OperationCanceledException)
                {
                    // Play Mode 結束時，還在等待的影格會被取消。
                    if (!stopped)
                    {
                        Report.outcome = UIWalkOutcome.NotRun.ToString();
                        Report.reason = "走訪途中 Play Mode 結束了。";
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Report.outcome = UIWalkOutcome.NotRun.ToString();
                    Report.reason = "走訪本身發生例外：" + exception.Message;
                }
                finally
                {
                    Detach();
                    if (!stopped)
                        Complete(this);
                }
            }

            private async Awaitable WalkAsync()
            {
                foreach (var entry in root.Entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Id) || !entries.TryAdd(entry.Id, entry))
                        continue;
                    if (entry.Kind == UIEntryKind.Page)
                        pages.Add(entry);
                    if (UIAuthoring.IsNavigatedByCode(entry))
                        byCode.Add(entry.Id);
                }
                routes = root.Routes.Where(route => route != null).ToArray();

                var warmUp = UnityEditor.SessionState.GetFloat(WarmUpSecondsKey, 0f);
                for (var until = Now + warmUp; Now < until;)
                    await Awaitable.NextFrameAsync();

                await StartStep();
                // 4. 依藍圖的順序，每條路由走一步。一步失敗不影響後面的步驟。
                foreach (var route in routes)
                {
                    if (stopped)
                        return;
                    await RouteStep(route);
                }
                FillViews();
            }

            // 3. 等起始頁顯示完成。沒有起始頁時這一步略過。
            private async Awaitable StartStep()
            {
                var start = root.StartPageId ?? "";
                var step = new UIWalkStep { kind = UIWalkStep.StartKind, target = start };
                Report.steps.Add(step);
                var started = Now;
                var deadline = started + owner.stepTimeout;
                try
                {
                    if (start.Length == 0)
                    {
                        Finish(step, UIWalkStepStatus.Skipped, "工作區沒有設定起始頁。");
                        await WaitIdle(deadline);
                        return;
                    }

                    var quiet = 0;
                    while (true)
                    {
                        var displayed = shown.Contains(start) || root.CurrentPageId == start;
                        quiet = root.IsTransitioning ? 0 : quiet + 1;
                        if (displayed && quiet >= StableFrames)
                            break;
                        // 起始頁顯示失敗時工作區會記錯誤，之後就閒置了，不必等到逾時。
                        if (!displayed && quiet >= StableFrames * 4 && logs.HasError)
                        {
                            Finish(step, UIWalkStepStatus.Failed, $"起始頁「{start}」沒有顯示出來。" + logs.ErrorText());
                            return;
                        }
                        if (Now > deadline)
                        {
                            Finish(step, UIWalkStepStatus.Failed, displayed
                                ? Timeout("等起始頁顯示完成")
                                : $"起始頁「{start}」沒有在 {owner.stepTimeout:0.##} 秒內顯示出來。");
                            return;
                        }
                        await Awaitable.NextFrameAsync();
                    }

                    if (logs.HasError)
                        Finish(step, UIWalkStepStatus.Failed, logs.ErrorText());
                    else
                        Finish(step, UIWalkStepStatus.Passed, "");
                }
                finally
                {
                    Close(step, started);
                }
            }

            private async Awaitable RouteStep(UIRoute route)
            {
                var step = new UIWalkStep
                {
                    source = route.Source ?? "",
                    action = route.Action ?? "",
                    kind = route.Kind.ToString(),
                    target = UIRoute.TargetsEntry(route.Kind) ? route.Target ?? "" : ""
                };
                Report.steps.Add(step);
                logs.Clear();
                var started = Now;
                StepClock clock = null;
                try
                {
                    // 訊號路由的來源是「任何畫面」，沒有來源畫面。
                    var isSignal = route.IsSignal;
                    UIEntry source = null;
                    if (!isSignal && !entries.TryGetValue(step.source, out source))
                    {
                        Finish(step, UIWalkStepStatus.Failed, $"藍圖裡沒有來源畫面「{step.source}」。");
                        return;
                    }
                    if (route.Kind == UIRouteKind.Emit && !owner.includeEmit)
                    {
                        Finish(step, UIWalkStepStatus.Skipped,
                            "Emit 路由預設略過。要觸發請開啟 IncludeEmit（命令列是 -unigameui-include-emit）。");
                        return;
                    }
                    if (route.Kind == UIRouteKind.Back && source != null && source.Kind == UIEntryKind.Page &&
                        pages.Count <= 1)
                    {
                        Finish(step, UIWalkStepStatus.Skipped, "工作區只有一個頁面，沒有上一頁可以返回。");
                        return;
                    }

                    clock = new StepClock(owner.stepTimeout);
                    Prepared prepared;
                    var result = UINavResult.Busy;
                    while (true)
                    {
                        // 準備：關掉所有 Popup、清空歷史，讓來源畫面成為最上層。準備時不傳資料。
                        // 訊號路由沒有來源畫面，準備時顯示起始頁。
                        prepared = isSignal ? await PrepareSignal(route, clock) : await Prepare(source, route, clock);
                        var busy = prepared.Busy;
                        if (!busy)
                        {
                            if (!prepared.Ok)
                            {
                                if (clock.TimedOut)
                                {
                                    Finish(step, UIWalkStepStatus.Failed, Timeout("準備來源畫面"));
                                }
                                else if (prepared.Skip)
                                {
                                    Finish(step, UIWalkStepStatus.Skipped, prepared.Reason);
                                }
                                else if (source != null && byCode.Contains(source.Id))
                                {
                                    // 由程式開啟的畫面需要程式給資料才開得起來，走訪不傳資料，所以不算失敗。
                                    Finish(step, UIWalkStepStatus.Skipped,
                                        "來源畫面標記為由程式開啟，需要由程式提供資料才能開啟。" + prepared.Reason);
                                }
                                else
                                {
                                    // 靠路由到得了的畫面本來就收不到資料，沒有資料就顯示不出來是真的問題。
                                    Finish(step, UIWalkStepStatus.Failed, "準備來源畫面失敗：" + prepared.Reason);
                                }
                                await Settle(clock);
                                return;
                            }

                            // 觸發。訊號不傳資料：走訪沒有遊戲的資料可以給。
                            result = isSignal
                                ? await root.SignalAsync(route.Action, null, clock.Token)
                                : await root.TriggerAsync(route.Source, route.Action, clock.Token);
                            busy = result == UINavResult.Busy;
                        }
                        if (!busy)
                            break;

                        // 頁面腳本剛好自己在導航（例如計時器）：等這次導航結束，從準備重新來過。逾時才算失敗。
                        if (clock.TimedOut || !await WaitIdle(clock.Deadline))
                        {
                            Finish(step, UIWalkStepStatus.Failed, Timeout("等頁面腳本自己的導航結束"));
                            await Settle(clock);
                            return;
                        }
                    }

                    // 判定在導航一結束就做，之後頁面腳本接著導航不影響結果。
                    step.result = result.ToString();
                    var verdict = Judge(route, source, prepared.Previous, result) ??
                        await JudgeClearedHistory(route, clock);
                    if (verdict != null && prepared.OnlyOpensOn != null && result == UINavResult.Unavailable)
                    {
                        // 準備時沒有為了讓這一步通過而把 Popup 開在別的頁面上：實際遊玩時它就是開在這裡。
                        verdict = $"Popup「{step.source}」只會開在「{prepared.OnlyOpensOn}」上面" +
                            $"（沿著 OpenPopup 路由只有這一頁開得到它），而這條路由要切到「{prepared.OnlyOpensOn}」。" +
                            "那時已經在這一頁，導航結果是 Unavailable，實際遊玩時這個動作不會有反應。";
                    }
                    await Settle(clock);

                    var targetByCode = UIRoute.TargetsEntry(route.Kind) && !string.IsNullOrEmpty(route.Target) &&
                        byCode.Contains(route.Target);
                    if (clock.TimedOut)
                        Finish(step, UIWalkStepStatus.Failed, Timeout("觸發路由"));
                    else if (result == UINavResult.Failed && targetByCode)
                    {
                        // 由程式開啟的目標畫面沒有資料就顯示不出來，走訪不傳資料，所以不算失敗。
                        Finish(step, UIWalkStepStatus.Skipped,
                            $"目標畫面「{route.Target}」標記為由程式開啟，需要由程式提供資料才能顯示。" + logs.ErrorText());
                    }
                    else if (verdict != null)
                        Finish(step, UIWalkStepStatus.Failed, verdict);
                    else if (logs.HasError)
                        Finish(step, UIWalkStepStatus.Failed, logs.ErrorText());
                    else
                        Finish(step, UIWalkStepStatus.Passed, "");
                }
                catch (OperationCanceledException)
                {
                    Finish(step, UIWalkStepStatus.Failed, "走訪在這一步中斷。");
                    throw;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Finish(step, UIWalkStepStatus.Failed, "走訪這一步時發生例外：" + exception.Message);
                }
                finally
                {
                    clock?.Dispose();
                    Close(step, started);
                }
            }

            private static void Finish(UIWalkStep step, UIWalkStepStatus status, string reason)
            {
                step.status = status.ToString();
                step.reason = reason ?? "";
            }

            private void Close(UIWalkStep step, double started)
            {
                step.seconds = (float)Math.Round(Now - started, 2);
                logs.WriteTo(step);
                logs.Clear();
            }

            private string Timeout(string doing)
            {
                return $"逾時：{doing}超過 {owner.stepTimeout:0.##} 秒沒有結束。";
            }

            private string Judge(UIRoute route, UIEntry source, string previous, UINavResult result)
            {
                if (result != UINavResult.Completed)
                    return $"導航結果是 {result}，預期是 Completed。";
                switch (route.Kind)
                {
                    case UIRouteKind.ShowPage:
                        return root.CurrentPageId == route.Target
                            ? null
                            : $"完成時目前頁是「{root.CurrentPageId ?? "(無)"}」，預期是「{route.Target}」。";
                    case UIRouteKind.OpenPopup:
                        return root.CurrentPopupId == route.Target
                            ? null
                            : $"完成時最上層的 Popup 是「{root.CurrentPopupId ?? "(無)"}」，預期是「{route.Target}」。";
                    case UIRouteKind.ClosePopup:
                        return IsOpen(source.Id) ? $"Popup「{source.Id}」還開著。" : null;
                    case UIRouteKind.Back:
                        if (source.Kind == UIEntryKind.Popup)
                            return IsOpen(source.Id) ? $"返回之後 Popup「{source.Id}」還開著。" : null;
                        return root.CurrentPageId == previous
                            ? null
                            : $"返回之後目前頁是「{root.CurrentPageId ?? "(無)"}」，預期回到「{previous}」。";
                    default:
                        return null;
                }
            }

            private bool IsOpen(string popupId)
            {
                return root.Popups.Any(popup => popup && popup.Id == popupId);
            }

            // history 是 Clear 的路由另外確認：完成後歷史是空的；不可返回的 Popup 還要確認 BackAsync() 無效。
            // 一般路由與訊號路由都適用。前面的判定沒有通過時不會執行到這裡。
            private async Awaitable<string> JudgeClearedHistory(UIRoute route, StepClock clock)
            {
                if (route.History != UIHistoryMode.Clear ||
                    (route.Kind != UIRouteKind.ShowPage && route.Kind != UIRouteKind.OpenPopup))
                    return null;
                if (root.HistoryCount != 0)
                    return $"路由的 history 是 Clear，完成後歷史裡卻還有 {root.HistoryCount} 筆。";
                if (route.Kind != UIRouteKind.OpenPopup)
                    return null;

                var back = await root.BackAsync(clock.Token);
                // 頁面腳本剛好自己在導航時沒辦法確認，不算失敗。
                if (back == UINavResult.Unavailable || back == UINavResult.Busy)
                    return null;
                return $"不可返回的 Popup「{route.Target}」開啟之後，BackAsync() 的結果是 {back}，預期是 Unavailable。";
            }

            // 準備的結果。Busy：準備途中遇到頁面腳本自己的導航，要等它結束後整個重來。
            // Skip：準備不起來，但原因不是問題（需要由程式提供資料），這一步記為略過。
            private readonly struct Prepared
            {
                public readonly bool Ok;
                public readonly bool Skip;
                public readonly bool Busy;
                public readonly string Previous;
                public readonly string Reason;
                // 來源的 Popup 只會開在這一頁上面，而這一步的路由正好要切到這一頁。平常是 null。
                public readonly string OnlyOpensOn;

                private Prepared(bool ok, bool skip, bool busy, string previous, string reason,
                    string onlyOpensOn = null)
                {
                    Ok = ok;
                    Skip = skip;
                    Busy = busy;
                    Previous = previous;
                    Reason = reason ?? "";
                    OnlyOpensOn = onlyOpensOn;
                }

                public static Prepared Ready(string previous, string onlyOpensOn = null) =>
                    new Prepared(true, false, false, previous, "", onlyOpensOn);

                public static Prepared Failed(string reason) => new Prepared(false, false, false, null, reason);
                public static Prepared Skipped(string reason) => new Prepared(false, true, false, null, reason);
                public static Prepared Interrupted() => new Prepared(false, false, true, null, "");
            }

            // 關掉所有 Popup、清空歷史。
            private async Awaitable<Prepared> ClosePopups(StepClock clock)
            {
                if (!await WaitIdle(clock.Deadline))
                    return Prepared.Failed("工作區一直有導航在進行。");

                for (var guard = 0; root.TopPopup && guard < 64; guard++)
                {
                    var open = root.CurrentPopupId;
                    var closed = await root.ClosePopupAsync(clock.Token);
                    if (closed == UINavResult.Busy)
                        return Prepared.Interrupted();
                    if (closed != UINavResult.Completed)
                        return Prepared.Failed($"關閉 Popup「{open}」的結果是 {closed}。");
                }
                root.ClearHistory();
                return Prepared.Ready(null);
            }

            // 訊號路由的準備：顯示起始頁，讓訊號從一個確定的畫面出發。
            // ShowPage 的目標剛好是起始頁時改用別的頁面，否則訊號會因為「已經在那一頁」而無法執行。
            private async Awaitable<Prepared> PrepareSignal(UIRoute route, StepClock clock)
            {
                var avoid = route.Kind == UIRouteKind.ShowPage ? route.Target : null;
                // 起始頁優先，其次是其他頁面。由程式開啟的頁面可能需要資料，沒有其他候選時才用。
                var candidates = PreferRouted(new[] { root.StartPageId }
                    .Concat(pages.Select(page => page.Id))
                    .Where(id => !string.IsNullOrEmpty(id) && id != avoid &&
                        entries.TryGetValue(id, out var entry) && entry.Kind == UIEntryKind.Page)
                    .Distinct().ToArray(), true);
                if (candidates.Length == 0)
                {
                    if (route.Kind == UIRouteKind.ShowPage)
                        return Prepared.Skipped("沒有其他頁面可以當出發點：訊號要切到的就是唯一的頁面。");
                    // 工作區沒有頁面：關掉 Popup 之後直接送出。
                    return await ClosePopups(clock);
                }

                string reason = null;
                foreach (var id in candidates)
                {
                    var attempt = logs.Mark();
                    var prepared = await Prepare(entries[id], route, clock);
                    if (prepared.Ok || prepared.Busy)
                        return prepared;
                    reason ??= prepared.Reason;
                    if (clock.TimedOut)
                        return Prepared.Failed(reason);
                    // 這個頁面當不成出發點，改試下一個。它記的訊息不算進這一步的判定。
                    logs.Abandon(attempt, AbandonedNote(id));
                }
                // 能當出發點的頁面全部由程式開啟、沒有資料開不起來：不算失敗。
                return candidates.All(byCode.Contains)
                    ? Prepared.Skipped("能當出發點的頁面都標記為由程式開啟，需要由程式提供資料才能顯示。" + reason)
                    : Prepared.Failed(reason);
            }

            // 途中任何一次導航遇到 Busy，表示頁面腳本自己在導航（例如計時器）：回報 Busy，
            // 由呼叫端等它結束後整個重來。
            private async Awaitable<Prepared> Prepare(UIEntry source, UIRoute route, StepClock clock)
            {
                var cleared = await ClosePopups(clock);
                if (!cleared.Ok)
                    return cleared;

                if (source.Kind == UIEntryKind.Page)
                {
                    // 先顯示另一個頁面，再顯示來源頁，讓歷史裡有一筆，Back 路由才有地方回去。
                    // 另一個頁面優先選有路由通到來源頁的，沒有就用起始頁。只有一個頁面時不必。
                    // history 是 Clear 的路由也盡量先放一筆，才看得出歷史真的被清空了。
                    var needsPrevious = route.Kind == UIRouteKind.Back;
                    var wantsPrevious = needsPrevious || route.History == UIHistoryMode.Clear;
                    var others = OtherPages(source, wantsPrevious, needsPrevious);
                    string previous = null;
                    foreach (var candidate in others)
                    {
                        var attempt = logs.Mark();
                        var shownCandidate = await Show(candidate, UIHistoryMode.Clear, clock);
                        if (shownCandidate == UINavResult.Busy)
                            return Prepared.Interrupted();
                        if (shownCandidate == UINavResult.Completed)
                        {
                            previous = candidate;
                            break;
                        }
                        if (clock.TimedOut)
                            return Prepared.Failed($"顯示頁面「{candidate}」逾時。");
                        // 這個候選顯示不出來，改試下一個。它記的訊息留在報告裡，但不算進這一步的判定：
                        // 那是那個頁面的問題，以它為來源或目標的步驟會回報。
                        logs.Abandon(attempt, AbandonedNote(candidate));
                    }
                    if (previous == null && needsPrevious)
                    {
                        // 能當上一頁的只有由程式開啟、沒有資料開不起來的頁面：不是這條路由的問題。
                        if (others.Length > 0 && others.All(byCode.Contains))
                        {
                            return Prepared.Skipped("能當上一頁的頁面都標記為由程式開啟，需要由程式提供資料才能顯示，" +
                                "Back 沒有上一頁可以回去。");
                        }
                        return Prepared.Failed("沒有其他頁面可以先顯示，Back 沒有上一頁可以回去。");
                    }

                    var shownSource = await Show(source.Id, previous == null ? UIHistoryMode.Clear : UIHistoryMode.Push,
                        clock);
                    if (shownSource == UINavResult.Busy)
                        return Prepared.Interrupted();
                    if (shownSource != UINavResult.Completed)
                        return Prepared.Failed($"顯示頁面「{source.Id}」的結果是 {shownSource}。");
                    if (root.CurrentPageId != source.Id || root.TopPopup)
                        return Prepared.Failed($"顯示之後最上層的畫面不是「{source.Id}」。");
                    return Prepared.Ready(previous);
                }

                // 來源是 Popup：先顯示一個頁面，再開啟這個 Popup。
                // 頁面選有 OpenPopup 路由指到它的，沒有就用起始頁。
                var under = PagesUnder(source, route);
                foreach (var candidate in under.Pages)
                {
                    var attempt = logs.Mark();
                    var shownCandidate = await Show(candidate, UIHistoryMode.Clear, clock);
                    if (shownCandidate == UINavResult.Busy)
                        return Prepared.Interrupted();
                    if (shownCandidate == UINavResult.Completed)
                        break;
                    if (clock.TimedOut)
                        return Prepared.Failed($"顯示頁面「{candidate}」逾時。");
                    logs.Abandon(attempt, AbandonedNote(candidate));
                }
                var opened = await root.OpenPopupAsync(source.Id, null, clock.Token);
                if (opened == UINavResult.Busy)
                    return Prepared.Interrupted();
                if (opened != UINavResult.Completed)
                    return Prepared.Failed($"開啟 Popup「{source.Id}」的結果是 {opened}。");
                if (root.CurrentPopupId != source.Id)
                    return Prepared.Failed($"開啟之後最上層的畫面不是「{source.Id}」。");
                return Prepared.Ready(root.CurrentPageId,
                    under.OnlyOpensOn != null && root.CurrentPageId == under.OnlyOpensOn ? under.OnlyOpensOn : null);
            }

            // 已經在那一頁時不必再切；工作區對「切到目前頁」回傳 Unavailable。
            private async Awaitable<UINavResult> Show(string pageId, UIHistoryMode history, StepClock clock)
            {
                if (root.CurrentPageId == pageId)
                {
                    if (history == UIHistoryMode.Clear)
                        root.ClearHistory();
                    return UINavResult.Completed;
                }
                return await root.ShowPageAsync(pageId, null, history, clock.Token);
            }

            private static string AbandonedNote(string pageId)
            {
                return $"準備時被放棄的候選頁面「{pageId}」，不算進這一步的判定。";
            }

            // 候選頁面先排除由程式開啟的頁面：它們可能要資料才顯示得出來。
            // 沒有其他候選時，allowByCode 才把它們拿來用。
            private string[] PreferRouted(string[] candidates, bool allowByCode)
            {
                var routed = candidates.Where(id => !byCode.Contains(id)).ToArray();
                return routed.Length > 0 || !allowByCode ? routed : candidates;
            }

            // 準備來源頁之前先顯示的「另一個頁面」：有路由通到來源頁的頁面，再來是起始頁。
            // anyPage：想要有上一頁時，前兩種都沒有，其餘的頁面也拿來試。
            // needed：非有上一頁不可（Back）。這時沒有其他候選，才會用由程式開啟的頁面。
            private string[] OtherPages(UIEntry source, bool anyPage, bool needed)
            {
                var candidates = routes
                    .Where(route => route.Kind == UIRouteKind.ShowPage && route.Target == source.Id)
                    .Select(route => route.Source)
                    .Append(root.StartPageId);
                if (anyPage)
                    candidates = candidates.Concat(pages.Select(page => page.Id));
                return PreferRouted(candidates.Where(id => !string.IsNullOrEmpty(id) && id != source.Id &&
                    entries.TryGetValue(id, out var entry) && entry.Kind == UIEntryKind.Page).Distinct().ToArray(),
                    needed);
            }

            // 來源是 Popup 時，底下要先顯示的頁面。照實際遊玩時它會開在哪裡來選：
            // 有 OpenPopup 路由通到它的頁面（經由別的 Popup 開啟的也算）。有好幾個時，ShowPage 的路由先用目標以外的那些。
            // 這個 Popup 只會開在目標頁上面時，就照實開在那一頁：這條路由實際上不會有反應，這一步要如實失敗。
            // 沒有任何頁面的路由會開啟它（由程式或訊號開啟）時，它可能開在任何頁面上：用起始頁，
            // 起始頁剛好是目標時才改用其他頁面。
            private (string[] Pages, string OnlyOpensOn) PagesUnder(UIEntry popup, UIRoute route)
            {
                var target = route.Kind == UIRouteKind.ShowPage ? route.Target : null;
                var openers = PagesThatOpen(popup.Id, out var opensAnywhere);
                if (openers.Length > 0 && !opensAnywhere)
                {
                    var others = openers.Where(id => id != target).ToArray();
                    return others.Length > 0 ? (PreferRouted(others, true), null) : (openers, target);
                }

                var anywhere = openers.Append(root.StartPageId).Concat(pages.Select(page => page.Id))
                    .Where(id => !string.IsNullOrEmpty(id) && entries.TryGetValue(id, out var entry) &&
                        entry.Kind == UIEntryKind.Page).Distinct().ToArray();
                return (PreferRouted(anywhere.Where(id => id != target).ToArray(), true)
                    .Concat(anywhere.Where(id => id == target)).ToArray(), null);
            }

            // 沿著 OpenPopup 路由往回找：哪些頁面開得到這個 Popup。由別的 Popup 開啟時，算在那個 Popup 底下的頁面。
            // opensAnywhere：它也會由訊號或程式開啟，可能出現在任何頁面上。
            private string[] PagesThatOpen(string popupId, out bool opensAnywhere)
            {
                opensAnywhere = false;
                var found = new List<string>();
                var seen = new HashSet<string> { popupId };
                var pending = new Queue<string>();
                pending.Enqueue(popupId);
                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    if (byCode.Contains(current))
                        opensAnywhere = true;
                    foreach (var opener in routes)
                    {
                        if (opener.Kind != UIRouteKind.OpenPopup || opener.Target != current)
                            continue;
                        if (opener.IsSignal)
                        {
                            opensAnywhere = true;
                            continue;
                        }
                        if (!entries.TryGetValue(opener.Source ?? "", out var from))
                            continue;
                        if (from.Kind == UIEntryKind.Page)
                        {
                            if (!found.Contains(from.Id))
                                found.Add(from.Id);
                        }
                        else if (seen.Add(from.Id))
                        {
                            pending.Enqueue(from.Id);
                        }
                    }
                }
                return found.ToArray();
            }

            // 等到連續幾個影格沒有導航在進行。頁面腳本可能在 OnShown 之後接著導航，這不算失敗。
            private async Awaitable<bool> WaitIdle(double deadline)
            {
                var quiet = 0;
                while (true)
                {
                    quiet = root.IsTransitioning ? 0 : quiet + 1;
                    if (quiet >= StableFrames)
                        return true;
                    if (Now > deadline)
                        return false;
                    await Awaitable.NextFrameAsync();
                }
            }

            private async Awaitable Settle(StepClock clock)
            {
                var deadline = clock.TimedOut ? Now + CancelGraceSeconds : clock.Deadline;
                if (!await WaitIdle(deadline) && !clock.TimedOut)
                    clock.Expire();
            }

            // 報告裡的畫面清單：每個畫面在走訪期間有沒有顯示過。
            private void FillViews()
            {
                foreach (var entry in entries.Values)
                {
                    var visited = shown.Contains(entry.Id);
                    var reason = "";
                    if (!visited)
                    {
                        var isSource = routes.Any(route => route.Source == entry.Id);
                        var isTarget = routes.Any(route => UIRoute.TargetsEntry(route.Kind) && route.Target == entry.Id);
                        if (byCode.Contains(entry.Id) && !isSource)
                            reason = "標記為由程式開啟，而且沒有以它為來源的路由，不在走訪範圍。";
                        else if (byCode.Contains(entry.Id))
                            reason = "標記為由程式開啟，沒有資料就開不起來。";
                        else if (!isSource && !isTarget && entry.Id != root.StartPageId)
                            reason = "沒有任何路由通到它，也沒有以它為來源的路由。";
                        else
                            reason = "走訪期間沒有顯示成功。";
                    }
                    Report.views.Add(new UIWalkView
                    {
                        id = entry.Id, kind = entry.Kind.ToString(), visited = visited, reason = reason
                    });
                }
            }
        }

        // 一步期間 Console 的 Error、Exception、Assert 與 Warning。
        // 「這一步有沒有 Error」和「報告裡留幾筆」是兩回事：判定看的是每一筆收到的訊息，不受上限影響；
        // 報告最多留 MaxReported 筆，Error、Exception 優先保留，其餘省略的只記筆數。
        private sealed class StepLogs
        {
            internal const int MaxReported = 50;
            // 留在記憶體裡的上限，避免頁面腳本不停記訊息時無限成長。超過的只計數，Error 仍然算進判定。
            private const int MaxKeptPerKind = 200;

            private sealed class Entry
            {
                public LogType Type;
                public string Message;
                // 不算進判定的訊息會註明原因，例如準備時被放棄的候選頁面。
                public string Note = "";
                public bool Counted = true;
                public bool IsError => Type != LogType.Warning;
            }

            private readonly List<Entry> entries = new List<Entry>();
            private int total;
            private int keptErrors;
            private int keptWarnings;
            // 超過記憶體上限、沒有留下內容的 Error。
            private int unkeptErrors;

            /// <summary>這一步期間有沒有算進判定的 Error、Exception 或 Assert。</summary>
            public bool HasError => unkeptErrors > 0 || entries.Any(entry => entry.IsError && entry.Counted);

            /// <summary>記下目前的位置，之後可以用 <see cref="Abandon"/> 把這之後收到的訊息排除在判定之外。</summary>
            public (int Index, int UnkeptErrors) Mark()
            {
                return (entries.Count, unkeptErrors);
            }

            /// <summary>
            /// 從 <paramref name="mark"/> 之後收到的訊息不算進這一步的判定。它們仍然留在報告裡，並註明原因。
            /// </summary>
            public void Abandon((int Index, int UnkeptErrors) mark, string note)
            {
                for (var index = mark.Index; index < entries.Count; index++)
                {
                    entries[index].Counted = false;
                    entries[index].Note = note;
                }
                unkeptErrors = mark.UnkeptErrors;
            }

            public void Clear()
            {
                entries.Clear();
                total = 0;
                keptErrors = 0;
                keptWarnings = 0;
                unkeptErrors = 0;
            }

            public void Add(LogType type, string message)
            {
                total++;
                var entry = new Entry { Type = type, Message = message ?? "" };
                if (entry.IsError)
                {
                    if (keptErrors >= MaxKeptPerKind)
                    {
                        unkeptErrors++;
                        return;
                    }
                    keptErrors++;
                }
                else
                {
                    if (keptWarnings >= MaxKeptPerKind)
                        return;
                    keptWarnings++;
                }
                entries.Add(entry);
            }

            /// <summary>第一筆 Error 的說明，給失敗原因用。沒有 Error 時是空字串。</summary>
            public string ErrorText()
            {
                var error = entries.FirstOrDefault(entry => entry.IsError && entry.Counted);
                if (error == null)
                    return HasError ? "這一步期間 Console 出現錯誤。" : "";
                var line = error.Message.IndexOf('\n');
                return $"這一步期間 Console 出現 {error.Type}：" +
                    (line < 0 ? error.Message : error.Message.Substring(0, line));
            }

            // 寫進報告：算進判定的 Error、Exception 優先，再來是其他的 Error，剩下的位置給 Warning；
            // 留下來的照原本的順序。
            public void WriteTo(UIWalkStep step)
            {
                var keep = new bool[entries.Count];
                var slots = MaxReported;
                for (var pass = 0; pass < 3 && slots > 0; pass++)
                {
                    for (var index = 0; index < entries.Count && slots > 0; index++)
                    {
                        var entry = entries[index];
                        var rank = !entry.IsError ? 2 : entry.Counted ? 0 : 1;
                        if (keep[index] || rank != pass)
                            continue;
                        keep[index] = true;
                        slots--;
                    }
                }

                step.logs = new List<UIWalkLog>();
                for (var index = 0; index < entries.Count; index++)
                {
                    if (!keep[index])
                        continue;
                    step.logs.Add(new UIWalkLog
                    {
                        type = entries[index].Type.ToString(), message = entries[index].Message,
                        note = entries[index].Note
                    });
                }
                step.logsOmitted = total - step.logs.Count;
            }
        }

        // 一步的時間上限。到期時取消還在進行的導航，讓下一步有機會繼續。
        private sealed class StepClock : IDisposable
        {
            private readonly CancellationTokenSource source = new CancellationTokenSource();
            private bool disposed;

            public StepClock(float seconds)
            {
                Deadline = Time.realtimeSinceStartupAsDouble + Math.Max(0.05f, seconds);
                Tick();
            }

            public double Deadline { get; }
            public bool TimedOut { get; private set; }
            public CancellationToken Token => source.Token;

            public void Expire()
            {
                if (disposed || TimedOut)
                    return;
                TimedOut = true;
                source.Cancel();
            }

            private async void Tick()
            {
                try
                {
                    while (!disposed && Time.realtimeSinceStartupAsDouble < Deadline)
                        await Awaitable.NextFrameAsync();
                    Expire();
                }
                catch (OperationCanceledException)
                {
                    // Play Mode 結束。
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                source.Dispose();
            }
        }
    }
}
