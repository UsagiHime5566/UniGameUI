using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniGameUI.Tests
{
    // 4-7：訊號（「任何畫面」節點的路由）與不可返回的 Popup。
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator ShowPageSignalsWorkFromAnyViewAndDeliverData()
        {
            CreateRoot();
            var third = AddPage("third");
            var signal = AddSignal("challenger", UIRouteKind.ShowPage, "next");
            var timeline = new List<string>();
            root.ActionTriggered += route => timeline.Add($"triggered:{route.Source}/{route.Action}");
            root.ActionCompleted += (route, status) =>
            {
                Assert.That(route, Is.SameAs(signal));
                Assert.That(root.IsTransitioning, Is.False);
                timeline.Add(status.ToString());
            };
            rootObject.SetActive(true);
            yield return StartPage();

            Assert.That(UIRoot.AnySource, Is.EqualTo("*"));
            Assert.That(signal.IsSignal, Is.True);
            Assert.That(root.Routes[0].IsSignal, Is.False);

            // 從起始頁送出：顯示目標頁，資料送達，原本的頁面進歷史。
            yield return Expect(root.SignalAsync("challenger", "ken"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(next.Data, Is.EqualTo("ken"));
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            Assert.That(timeline, Is.EqualTo(new[] { "triggered:*/challenger", "Completed" }));

            // 已經在目標頁。
            ExpectLog(LogType.Warning, "Signal 'challenger'", "目前已在頁面 'next'");
            yield return Expect(root.SignalAsync("challenger", "ryu"), UINavResult.Unavailable);
            Assert.That(next.Data, Is.EqualTo("ken"));

            // 從另一個畫面送出同一個訊號，一樣到得了。
            yield return Expect(root.ShowPageAsync("third"), UINavResult.Completed);
            yield return Expect(root.SignalAsync("challenger", "ryu"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(next.Data, Is.EqualTo("ryu"));
            Assert.That(third.gameObject.activeSelf, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OpenPopupSignalsStackOnTopOfWhateverIsOpen()
        {
            CreateRoot();
            var otherTemplate = CreateView("other", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "other", kind = UIEntryKind.Popup, prefab = otherTemplate });
            AddSignal("alert", UIRouteKind.OpenPopup, "popup");
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.SignalAsync("alert", "lost connection"), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(root.TopPopup.Data, Is.EqualTo("lost connection"));
            Assert.That(root.CurrentPage, Is.SameAs(first));

            // 目標已經開著。
            ExpectLog(LogType.Warning, "Signal 'alert'", "Popup 'popup' 已經開啟");
            yield return Expect(root.SignalAsync("alert"), UINavResult.Unavailable);
            Assert.That(root.Popups.Count, Is.EqualTo(1));
            yield return Expect(root.BackAsync(), UINavResult.Completed);

            // 已經有別的 Popup 開著：新的疊在上面。
            yield return Expect(root.OpenPopupAsync("other"), UINavResult.Completed);
            yield return Expect(root.SignalAsync("alert", "again"), UINavResult.Completed);
            Assert.That(root.Popups.Count, Is.EqualTo(2));
            Assert.That(root.Popups[0].Id, Is.EqualTo("other"));
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(root.TopPopup.Data, Is.EqualTo("again"));

            // 預設可以返回：一次關一個。
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.EqualTo("other"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EmitSignalsReachTheTopViewAndKeepApartFromViewActions()
        {
            CreateRoot();
            AddHandler(first, "home");
            AddHandler(popupTemplate, "popup");
            var signal = AddSignal("tick", UIRouteKind.Emit, null);
            // 同名的畫面動作：和訊號互不相干。
            root.routes.Add(new UIRoute { source = "home", action = "tick", kind = UIRouteKind.Emit });
            var received = new List<string>();
            var emitted = new List<string>();
            var completed = new List<UIRoute>();
            UIViewTestHandler.Observed = (handler, phase, data) =>
            {
                if (phase == "Action")
                    received.Add($"{handler.label}:{data}");
            };
            root.ActionEmitted.AddListener((source, action) => emitted.Add($"{source}/{action}"));
            root.ActionCompleted += (route, _) => completed.Add(route);
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.SignalAsync("tick"), UINavResult.Completed);
            Assert.That(received, Is.EqualTo(new[] { "home:tick" }));
            Assert.That(emitted, Is.EqualTo(new[] { "*/tick" }));
            Assert.That(completed, Is.EqualTo(new[] { signal }));

            // 畫面自己的同名動作走的是另一條路由。
            yield return Expect(root.TriggerAsync("home", "tick"), UINavResult.Completed);
            Assert.That(emitted, Is.EqualTo(new[] { "*/tick", "home/tick" }));
            Assert.That(completed[1], Is.Not.SameAs(signal));

            // 有 Popup 開著時由最上層的 Popup 收到。Emit 不帶資料，有給時記錄警告。
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            received.Clear();
            ExpectLog(LogType.Warning, "Signal 'tick'", "Emit 的訊號不帶資料");
            yield return Expect(root.SignalAsync("tick", 42), UINavResult.Completed);
            Assert.That(received, Is.EqualTo(new[] { "popup:tick" }));
            Assert.That(emitted[^1], Is.EqualTo("*/tick"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SignalsWithoutAUsableRouteAreReported()
        {
            CreateRoot();
            AddSignal("challenger", UIRouteKind.ShowPage, "next");
            AddSignal("broken", UIRouteKind.Back, null);
            rootObject.SetActive(true);
            yield return StartPage();

            ExpectLog(LogType.Error, "Signal 'absent'", "找不到這個動作的訊號路由");
            yield return Expect(root.SignalAsync("absent"), UINavResult.NotFound);
            // 畫面自己的動作不是訊號。
            ExpectLog(LogType.Error, "Signal 'next'", "找不到這個動作的訊號路由");
            yield return Expect(root.SignalAsync("next"), UINavResult.NotFound);
            ExpectLog(LogType.Error, "Signal ''", "找不到這個動作的訊號路由");
            yield return Expect(root.SignalAsync(null), UINavResult.NotFound);
            // Validate 會擋下這種路由；真的出現在執行期時回報而不是照做。
            ExpectLog(LogType.Error, "Signal 'broken'", "訊號路由的種類只能是 ShowPage、OpenPopup 或 Emit");
            yield return Expect(root.SignalAsync("broken"), UINavResult.NotFound);
            Assert.That(root.CurrentPage, Is.SameAs(first));

            // 訊號路由不能當成某個畫面的動作來觸發。
            ExpectLog(LogType.Warning, "Trigger '*'/'challenger'", "SignalAsync");
            yield return Expect(root.TriggerAsync(UIRoot.AnySource, "challenger"), UINavResult.Unavailable);
            Assert.That(root.CurrentPage, Is.SameAs(first));

            // 已經取消的 token 與停用的工作區。
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            yield return Expect(root.SignalAsync("challenger", null, cancellation.Token), UINavResult.Canceled);
            root.enabled = false;
            ExpectLog(LogType.Warning, "Signal 'challenger'", "工作區停用或尚未就緒");
            yield return Expect(root.SignalAsync("challenger"), UINavResult.Unavailable);
            root.enabled = true;
            Assert.That(root.CurrentPage, Is.SameAs(first));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SignalsSentDuringANavigationRunAfterItInOrder()
        {
            CreateRoot();
            AddHandler(next, "next");
            var third = AddPage("third");
            AddSignal("alert", UIRouteKind.OpenPopup, "popup");
            AddSignal("third", UIRouteKind.ShowPage, "third");
            var timeline = new List<string>();
            root.ActionTriggered += route => timeline.Add($"triggered:{route.Action}");
            root.ActionCompleted += (route, status) => timeline.Add($"{status}:{route.Action}");
            Awaitable<UINavResult> alert = null;
            Awaitable<UINavResult> change = null;
            var rejected = new List<Awaitable<UINavResult>>();
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler.label != "next" || phase != "Shown" || alert != null)
                    return;
                // 在生命週期方法裡送出：兩個訊號都排隊，其他 API 照舊回傳 Busy。
                alert = root.SignalAsync("alert", "first");
                change = root.SignalAsync("third", "second");
                rejected.Add(root.ShowPageAsync("home"));
                rejected.Add(root.OpenPopupAsync("popup"));
                rejected.Add(root.TriggerAsync("next", "popup"));
                rejected.Add(root.BackAsync());
            };
            rootObject.SetActive(true);
            yield return StartPage();

            const string navigation = "ShowPage 'next'";
            ExpectBusyWarning("ShowPage 'home'", navigation, "Shown", "next");
            ExpectBusyWarning("OpenPopup 'popup'", navigation, "Shown", "next");
            ExpectBusyWarning("Trigger 'next'/'popup'", navigation, "Shown", "next");
            ExpectBusyWarning("Back 'next'", navigation, "Shown", "next");
            var transition = root.ShowPageAsync("next");
            // 這次導航結束時訊號還沒有執行。
            Assert.That(alert, Is.Not.Null);
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(timeline, Is.Empty);
            yield return Expect(transition, UINavResult.Completed);
            foreach (var operation in rejected)
                yield return Expect(operation, UINavResult.Busy);

            yield return Expect(alert, UINavResult.Completed);
            yield return Expect(change, UINavResult.Completed);
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "triggered:alert", "Completed:alert", "triggered:third", "Completed:third",
            }));
            Assert.That(root.CurrentPage, Is.SameAs(third));
            Assert.That(third.Data, Is.EqualTo("second"));
            Assert.That(root.CurrentPopupId, Is.Null, "切頁時開著的 Popup 照規則關閉");
            // home → next → third 都是 Push。
            Assert.That(root.HistoryCount, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SignalSentDuringATransitionWaitsForItAndReturnsItsOwnResult()
        {
            if (!UITransition.IsDOTweenReady)
                Assert.Ignore("此案例需要 DOTween 才有跨影格的轉場。");
            CreateRoot();
            next.enter.preset = UITransitionPreset.Fade;
            next.enter.duration = 0.6f;
            AddSignal("alert", UIRouteKind.OpenPopup, "popup");
            AddSignal("home", UIRouteKind.ShowPage, "home");
            rootObject.SetActive(true);
            yield return StartPage();
            var timeline = new List<string>();
            root.ViewShown += view => timeline.Add($"shown:{view.Id}");
            root.ActionTriggered += route => timeline.Add($"triggered:{route.Action}");
            root.ActionCompleted += (route, status) => timeline.Add($"{status}:{route.Action}");

            var transition = root.ShowPageAsync("next");
            Assert.That(root.IsTransitioning, Is.True);
            var alert = root.SignalAsync("alert", "late");
            // 送出時已經在目標頁的訊號，要等到執行時才判定：那時已經不在 home。
            var home = root.SignalAsync("home");
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Busy);
            yield return Frames();
            Assert.That(root.IsTransitioning, Is.True, "訊號要落在轉場期間");
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(timeline, Is.Empty);

            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(alert, UINavResult.Completed);
            yield return Expect(home, UINavResult.Completed);
            // 轉場結束之後才依序執行：先開 Popup，再切回 home（開著的 Popup 跟著關閉）。
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "shown:next", "triggered:alert", "shown:popup", "Completed:alert",
                "triggered:home", "shown:home", "Completed:home",
            }));
            Assert.That(root.CurrentPage, Is.SameAs(first));
            Assert.That(root.CurrentPopupId, Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WaitingSignalsAreCanceledByTheirTokenOrByDisablingTheWorkspace()
        {
            CreateRoot();
            AddHandler(next, "next");
            AddHandler(first, "home");
            AddSignal("alert", UIRouteKind.OpenPopup, "popup");
            using var cancellation = new CancellationTokenSource();
            Awaitable<UINavResult> waiting = null;
            var token = cancellation.Token;
            UIViewTestHandler.Observed = (_, phase, _) =>
            {
                if (phase == "Shown")
                    waiting = root.SignalAsync("alert", null, token);
            };
            rootObject.SetActive(true);
            yield return StartPage();
            // 起始頁顯示時送出的那一個照常執行。
            yield return Expect(waiting, UINavResult.Completed);
            yield return Expect(root.BackAsync(), UINavResult.Completed);

            // 等待中取消 token。
            waiting = null;
            var transition = root.ShowPageAsync("next");
            Assert.That(waiting, Is.Not.Null);
            cancellation.Cancel();
            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(waiting, UINavResult.Canceled);
            Assert.That(root.CurrentPopupId, Is.Null);

            // 等待中停用工作區。
            token = default;
            waiting = null;
            transition = root.BackAsync();
            Assert.That(waiting, Is.Not.Null);
            root.enabled = false;
            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(waiting, UINavResult.Canceled);
            root.enabled = true;
            yield return Frames();
            Assert.That(root.CurrentPopupId, Is.Null);

            // 取消的訊號不會卡住後面的訊號。
            UIViewTestHandler.Observed = null;
            yield return Expect(root.SignalAsync("alert"), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ShowPageSignalWithClearClosesPopupsAndLeavesNowhereToGoBack()
        {
            CreateRoot();
            var third = AddPage("third");
            AddSignal("reset", UIRouteKind.ShowPage, "third", UIHistoryMode.Clear);
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));

            yield return Expect(root.SignalAsync("reset"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            Assert.That(root.Popups, Is.Empty);
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UnreturnablePopupClearsHistoryAndIgnoresTheBackKey()
        {
            UseInput();
            CreateRoot();
            AddOverlay(popupTemplate);
            popupTemplate.closesOnOverlayClick = true;
            var consulted = 0;
            AddHandler(popupTemplate, "popup");
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler.label == "popup" && phase == "Back")
                    consulted++;
            };
            AddSignal("lock", UIRouteKind.OpenPopup, "popup", UIHistoryMode.Clear);
            var atRoot = 0;
            yield return StartPageWithInput();
            root.BackRequestedAtRoot += () => atRoot++;
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));

            // 規則一：開啟時清空歷史。
            yield return Expect(root.SignalAsync("lock", "challenger"), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(root.HistoryCount, Is.Zero);

            // 規則二：返回鍵不處理，但 OnBackRequested 仍然先被呼叫。
            yield return Tap(keyboard.escapeKey);
            Assert.That(consulted, Is.EqualTo(1));
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            yield return Tap(gamepad.buttonEast);
            Assert.That(consulted, Is.EqualTo(2));
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(atRoot, Is.Zero);
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));

            // 規則四：點遮罩關閉照常可以關閉它，關閉後歷史是空的。
            yield return ClickAt(new Vector2(6, 6));
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Tap(keyboard.escapeKey);
            Assert.That(atRoot, Is.EqualTo(1), "關閉後已經沒有地方可以返回");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BackRoutesFromAnUnreturnablePopupAreUnavailable()
        {
            CreateRoot();
            // 一般路由的 OpenPopup 也接受 history。
            root.routes.Add(new UIRoute
            {
                source = "next", action = "lock", kind = UIRouteKind.OpenPopup, target = "popup",
                history = UIHistoryMode.Clear,
            });
            root.routes.Add(new UIRoute { source = "popup", action = "back", kind = UIRouteKind.Back });
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);

            // 一般的 Popup：Back 路由可以關閉。
            yield return Expect(root.TriggerAsync("next", "popup"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.TriggerAsync("popup", "back"), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);

            yield return Expect(root.TriggerAsync("next", "lock"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Expect(root.TriggerAsync("popup", "back"), UINavResult.Unavailable);
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));

            // Popup 沒有 Replace。
            var rejected = Assert.Throws<ArgumentException>(() =>
            {
                root.OpenPopupAsync("popup", null, UIHistoryMode.Replace);
            });
            Assert.That(rejected.Message, Does.Contain("Push 或 Clear"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PageChangesFromAnUnreturnablePopupKeepTheHistoryEmpty()
        {
            CreateRoot();
            var third = AddPage("third");
            var otherTemplate = CreateView("other", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "other", kind = UIEntryKind.Popup, prefab = otherTemplate });
            // 這條路由寫的是 Push，但從不可返回的 Popup 切頁一律清空歷史。
            root.routes.Add(new UIRoute { source = "popup", action = "go", kind = UIRouteKind.ShowPage, target = "third" });
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Expect(root.OpenPopupAsync("popup", null, UIHistoryMode.Clear), UINavResult.Completed);

            yield return Expect(root.TriggerAsync("popup", "go"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            Assert.That(root.Popups, Is.Empty);
            Assert.That(root.HistoryCount, Is.Zero, "底下的頁面不會進歷史");
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            Assert.That(root.CurrentPage, Is.SameAs(third));

            // 同一條路由從一般的 Popup 觸發時，底下的頁面照常進歷史。
            yield return Expect(root.ShowPageAsync("next", null, UIHistoryMode.Clear), UINavResult.Completed);
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            yield return Expect(root.TriggerAsync("popup", "go"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));

            // 不可返回的 Popup 被別的 Popup 蓋住時，切頁一樣清空歷史。
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Expect(root.ShowPageAsync("home"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.OpenPopupAsync("popup", null, UIHistoryMode.Clear), UINavResult.Completed);
            yield return Expect(root.OpenPopupAsync("other"), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.EqualTo("other"));
            yield return Expect(root.ShowPageAsync("third"), UINavResult.Completed);
            Assert.That(root.Popups, Is.Empty);
            Assert.That(root.HistoryCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClosingAnUnreturnablePopupLeavesAnEmptyHistory()
        {
            CreateRoot();
            root.routes.Add(new UIRoute { source = "popup", action = "close", kind = UIRouteKind.ClosePopup });
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);

            // ClosePopup 路由。
            yield return Expect(root.OpenPopupAsync("popup", null, UIHistoryMode.Clear), UINavResult.Completed);
            yield return Expect(root.TriggerAsync("popup", "close"), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            Assert.That(root.CurrentPage, Is.SameAs(next));

            // View.Close()。
            yield return Expect(root.OpenPopupAsync("popup", null, UIHistoryMode.Clear), UINavResult.Completed);
            yield return Expect(root.TopPopup.Close(), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(root.HistoryCount, Is.Zero);

            // 關閉之後的切頁恢復正常，可以返回。
            yield return Expect(root.ShowPageAsync("home"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UnreturnableStateBelongsToOneOpeningEvenForReusedInstances()
        {
            CreateRoot();
            popupTemplate.reusesInstance = true;
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);

            yield return Expect(root.OpenPopupAsync("popup", null, UIHistoryMode.Clear), UINavResult.Completed);
            var instance = root.TopPopup;
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return null;
            Assert.That(instance && !instance.gameObject.activeSelf, Is.True, "關閉後保留同一個實例");

            // 同一個實例第二次以 Push 開啟：可以返回。
            yield return Expect(root.OpenPopupAsync("popup", null, UIHistoryMode.Push), UINavResult.Completed);
            Assert.That(root.TopPopup, Is.SameAs(instance));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);

            // 預設的多載也是可以返回的。
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            Assert.That(root.TopPopup, Is.SameAs(instance));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        private UIRoute AddSignal(string action, UIRouteKind kind, string target,
            UIHistoryMode history = UIHistoryMode.Push)
        {
            var route = new UIRoute
            {
                source = UIRoot.AnySource, action = action, kind = kind, target = target, history = history,
            };
            root.routes.Add(route);
            return route;
        }
    }
}
