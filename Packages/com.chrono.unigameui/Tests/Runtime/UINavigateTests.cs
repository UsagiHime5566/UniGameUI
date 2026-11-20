using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniGameUI.Tests
{
    // 第五批 5-8：頁面自己的流程。NavigateAsync 走這一頁自己的路由，和訊號共用排隊，不會回傳 Busy。
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator NavigateAsyncPicksOneOfThisPagesRoutesAfterTheGameLogic()
        {
            CreateRoot();
            var third = AddPage("third");
            var home = AddHandler(first, "home");
            // 「進入下一天」：按鈕用 Emit，OnAction 跑完遊戲邏輯之後依結果走這一頁的其中一條路由。
            root.routes.Add(new UIRoute { source = "home", action = "nextDay", kind = UIRouteKind.Emit });
            root.routes.Add(new UIRoute
            {
                source = "home", action = "toNext", kind = UIRouteKind.ShowPage, target = "next",
                history = UIHistoryMode.Clear,
            });
            root.routes.Add(new UIRoute
            {
                source = "home", action = "toThird", kind = UIRouteKind.ShowPage, target = "third",
                history = UIHistoryMode.Clear,
            });
            var outcome = "toNext";
            Awaitable<UINavResult> navigation = null;
            UIViewTestHandler.Observed = (handler, phase, data) =>
            {
                if (handler == home && phase == "Action" && (string)data == "nextDay")
                    navigation = home.Navigate(outcome);
            };
            var timeline = new List<string>();
            root.ActionCompleted += (route, status) => timeline.Add($"{route.Action}:{status}");
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.TriggerAsync("home", "nextDay"), UINavResult.Completed);
            Assert.That(navigation, Is.Not.Null);
            yield return Expect(navigation, UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(root.HistoryCount, Is.EqualTo(0), "路由的 history 是 Clear");
            Assert.That(timeline, Is.EqualTo(new[] { "toNext:Completed", "nextDay:Completed" }));

            // 另一種結果：同一顆按鈕走另一條路由。
            yield return Expect(root.ShowPageAsync("home"), UINavResult.Completed);
            outcome = "toThird";
            navigation = null;
            yield return Expect(root.TriggerAsync("home", "nextDay"), UINavResult.Completed);
            yield return Expect(navigation, UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NavigateAsyncWaitsForTheRunningNavigationAndQueuesWithSignals()
        {
            CreateRoot();
            var third = AddPage("third");
            var nextHandler = AddHandler(next, "next");
            root.routes.Add(new UIRoute { source = "next", action = "toThird", kind = UIRouteKind.ShowPage, target = "third" });
            AddSignal("alert", UIRouteKind.OpenPopup, "popup");
            var timeline = new List<string>();
            root.ActionTriggered += route => timeline.Add($"triggered:{route.Action}");
            root.ActionCompleted += (route, status) => timeline.Add($"{status}:{route.Action}");
            Awaitable<UINavResult> navigate = null;
            Awaitable<UINavResult> signal = null;
            Awaitable<UINavResult> rejected = null;
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler != nextHandler || phase != "Shown" || navigate != null)
                    return;
                // 在生命週期方法裡（導航還沒結束）呼叫：NavigateAsync 與訊號依送出的順序排隊，TriggerAsync 照舊回傳 Busy。
                navigate = nextHandler.Navigate("toThird");
                signal = root.SignalAsync("alert");
                rejected = root.TriggerAsync("next", "toThird");
            };
            rootObject.SetActive(true);
            yield return StartPage();

            ExpectBusyWarning("Trigger 'next'/'toThird'", "ShowPage 'next'", "Shown", "next");
            var transition = root.ShowPageAsync("next");
            Assert.That(navigate, Is.Not.Null);
            Assert.That(timeline, Is.Empty, "這次導航結束之前不執行");
            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(rejected, UINavResult.Busy);
            yield return Expect(navigate, UINavResult.Completed);
            yield return Expect(signal, UINavResult.Completed);

            // 先送出的先執行：先切到 third，再在 third 上開啟 Popup。
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "triggered:toThird", "Completed:toThird", "triggered:alert", "Completed:alert",
            }));
            Assert.That(root.CurrentPage, Is.SameAs(third));
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NavigateAsyncDuringATransitionReturnsItsOwnResult()
        {
            if (!UITransition.IsDOTweenReady)
                Assert.Ignore("此案例需要 DOTween 才有跨影格的轉場。");
            CreateRoot();
            var third = AddPage("third");
            var nextHandler = AddHandler(next, "next");
            root.routes.Add(new UIRoute { source = "next", action = "toThird", kind = UIRouteKind.ShowPage, target = "third" });
            next.enter.preset = UITransitionPreset.Fade;
            next.enter.duration = 0.6f;
            rootObject.SetActive(true);
            yield return StartPage();

            var transition = root.ShowPageAsync("next");
            Assert.That(root.IsTransitioning, Is.True);
            // 轉場還沒結束時 next 已經是要顯示的頁面，但還不是目前的頁面：等轉場結束後才判斷來源。
            var navigate = nextHandler.Navigate("toThird");
            yield return Expect(root.TriggerAsync("next", "toThird"), UINavResult.Busy);
            yield return Frames();
            Assert.That(root.IsTransitioning, Is.True, "要落在轉場期間");
            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(navigate, UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NavigateAsyncCancelsQuietlyWhenThePageIsGoneAndWarnsAboutAPopupOnTop()
        {
            CreateRoot();
            var home = AddHandler(first, "home");
            var third = AddPage("third");
            root.routes.Add(new UIRoute { source = "home", action = "toThird", kind = UIRouteKind.ShowPage, target = "third" });
            Awaitable<UINavResult> navigate = null;
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                // home 正在離開：這時送出的 NavigateAsync 要等這次導航結束，那時 home 已經不在畫面上。
                if (handler == home && phase == "Hiding" && navigate == null)
                    navigate = home.Navigate("toThird");
            };
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(navigate, Is.Not.Null);
            yield return Expect(navigate, UINavResult.Canceled);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(third.gameObject.activeSelf, Is.False);
            UIViewTestHandler.Observed = null;

            // 來源頁還在，但上面蓋著 Popup：Unavailable，警告指出是哪個 Popup。
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            root.routes.Add(new UIRoute { source = "home", action = "openPopup", kind = UIRouteKind.OpenPopup, target = "popup" });
            yield return Frames();
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            ExpectLog(LogType.Warning, "TriggerWhenIdle 'home'/'toThird'", "上面蓋著 Popup 'popup'");
            yield return Expect(home.Navigate("toThird"), UINavResult.Unavailable);
            Assert.That(root.CurrentPage, Is.SameAs(first));
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));

            // 關掉 Popup 之後就走得了。
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return Expect(home.Navigate("toThird"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NavigateAsyncReportsMissingRoutesAndStopsWhenCanceledOrDisabled()
        {
            CreateRoot();
            var home = AddHandler(first, "home");
            var nextHandler = AddHandler(next, "next");
            AddPage("third");
            root.routes.Add(new UIRoute { source = "next", action = "toThird", kind = UIRouteKind.ShowPage, target = "third" });
            AddSignal("alert", UIRouteKind.OpenPopup, "popup");
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            Awaitable<UINavResult> waiting = null;
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler == nextHandler && phase == "Shown" && waiting == null)
                    waiting = nextHandler.Navigate("toThird", token);
            };
            rootObject.SetActive(true);
            yield return StartPage();

            // 找不到路由。訊號路由不能用這個方法走。
            ExpectLog(LogType.Error, "TriggerWhenIdle 'home'/'absent'", "找不到路由");
            yield return Expect(home.Navigate("absent"), UINavResult.NotFound);
            ExpectLog(LogType.Warning, "TriggerWhenIdle '*'/'alert'", "SignalAsync");
            yield return Expect(root.TriggerWhenIdleAsync(UIRoot.AnySource, "alert"), UINavResult.Unavailable);

            // 已經取消的 token。
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                yield return Expect(home.Navigate("next", canceled.Token), UINavResult.Canceled);
            }
            Assert.That(root.CurrentPage, Is.SameAs(first));

            // 等待中取消 token。
            var transition = root.ShowPageAsync("next");
            Assert.That(waiting, Is.Not.Null);
            cancellation.Cancel();
            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(waiting, UINavResult.Canceled);
            Assert.That(root.CurrentPage, Is.SameAs(next));

            // 等待中停用工作區。
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            token = default;
            waiting = null;
            transition = root.ShowPageAsync("next");
            Assert.That(waiting, Is.Not.Null);
            root.enabled = false;
            yield return Expect(transition, UINavResult.Completed);
            yield return Expect(waiting, UINavResult.Canceled);

            // 工作區停用時呼叫：和其他導航一樣回傳 Unavailable。
            ExpectLog(LogType.Warning, "TriggerWhenIdle 'next'/'toThird'", "工作區停用或尚未就緒");
            yield return Expect(nextHandler.Navigate("toThird"), UINavResult.Unavailable);
            root.enabled = true;
            yield return Frames();

            // 取消的那幾個不會卡住後面的。
            UIViewTestHandler.Observed = null;
            yield return Expect(nextHandler.Navigate("toThird"), UINavResult.Completed);
            Assert.That(root.CurrentPageId, Is.EqualTo("third"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NavigateAsyncNeedsAWorkspace()
        {
            var loose = new GameObject("Loose view", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                loose.AddComponent<UIView>().id = "loose";
                var handler = loose.AddComponent<UIViewTestHandler>();
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    "NavigateAsync 'go' 沒有執行：這個畫面還沒有交給工作區管理"));
                yield return Expect(handler.Navigate("go"), UINavResult.Unavailable);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(loose);
            }
        }
    }
}
