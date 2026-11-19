using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace UniGameUI.Tests
{
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator ReplaceThenBackKeepsCurrentViewInteractiveInSequentialMode()
        {
            return ReplaceThenBackKeepsCurrentViewInteractive(UIPageTransitionMode.Sequential);
        }

        [UnityTest]
        public IEnumerator ReplaceThenBackKeepsCurrentViewInteractiveInConcurrentMode()
        {
            return ReplaceThenBackKeepsCurrentViewInteractive(UIPageTransitionMode.Concurrent);
        }

        private IEnumerator ReplaceThenBackKeepsCurrentViewInteractive(UIPageTransitionMode mode)
        {
            CreateRoot();
            root.pageTransitionMode = mode;
            if (UITransition.IsDOTweenReady)
            {
                first.enter.preset = first.exit.preset = UITransitionPreset.Fade;
                first.enter.duration = first.exit.duration = 0.06f;
                next.enter.preset = next.exit.preset = UITransitionPreset.Fade;
                next.enter.duration = next.exit.duration = 0.06f;
            }
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.ShowPageAsync("home", null, UIHistoryMode.Replace), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.Zero);

            var notified = false;
            root.ViewHiding += _ => notified = true;
            root.ViewShowing += _ => notified = true;
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            Assert.That(notified, Is.False);
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(first.Group.interactable && first.Group.blocksRaycasts, Is.True);
            Assert.That(root.CurrentPage, Is.SameAs(first));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ReplaceWithHistoryTopKeepsEarlierPageAvailable()
        {
            CreateRoot();
            AddPage("third");
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Expect(root.ShowPageAsync("third"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(2));
            yield return Expect(root.ShowPageAsync("next", null, UIHistoryMode.Replace), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(first));
            Assert.That(root.HistoryCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BackSkipsRepeatedCurrentIdsEvenIfHistoryIsAlreadyStale()
        {
            CreateRoot();
            rootObject.SetActive(true);
            yield return StartPage();

            var field = typeof(UIRoot).GetField("history", BindingFlags.Instance | BindingFlags.NonPublic);
            var history = (List<string>)field.GetValue(root);
            history.AddRange(new[] { "next", "next", "home", "home" });
            var shown = new List<string>();
            root.ViewShowing += view => shown.Add(view.Id);

            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(root.HistoryCount, Is.Zero);
            Assert.That(shown, Is.EqualTo(new[] { "next" }));
            Assert.That(next.Group.interactable, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryNavigationApiReportsBusyDuringLifecycleNotifications()
        {
            CreateRoot();
            AddHandler(first, "from");
            AddHandler(next, "to");
            rootObject.SetActive(true);
            yield return StartPage();
            var rejected = new List<Awaitable<UINavResult>>();
            Awaitable<UIPopupResult<string>> rejectedPopup = null;

            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler.View == first && phase == "Hiding")
                    rejected.Add(first.Close());
                if (handler.View == first && phase == "Hidden")
                    rejected.Add(root.BackAsync());
                if (handler.View == next && phase == "Shown")
                    rejected.Add(root.ShowPageAsync("home"));
            };
            next.Showing.AddListener(() => rejected.Add(root.OpenPopupAsync("popup")));
            next.Shown.AddListener(() => rejected.Add(root.ClosePopupAsync()));
            root.ViewShowing += _ => rejectedPopup = root.OpenPopupForResultAsync<string>("popup");
            root.ViewShown += _ => rejected.Add(root.TriggerAsync("next", "popup"));

            const string navigation = "ShowPage 'next'";
            ExpectBusyWarning("Close 'home'", navigation, "Hiding", "home");
            ExpectBusyWarning("OpenPopup 'popup'", navigation, "Showing", "next");
            ExpectBusyWarning("OpenPopupForResult 'popup'", navigation, "Showing", "next");
            ExpectBusyWarning("Back 'next'", navigation, "Hidden", "home");
            ExpectBusyWarning("ShowPage 'home'", navigation, "Shown", "next");
            ExpectBusyWarning("ClosePopup '(無)'", navigation, "Shown", "next");
            ExpectBusyWarning("Trigger 'next'/'popup'", navigation, "Shown", "next");
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(rejected.Count, Is.EqualTo(6));
            foreach (var operation in rejected)
                yield return Expect(operation, UINavResult.Busy);
            yield return ExpectPopup(rejectedPopup, UINavResult.Busy);
            yield return null;
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(root.TopPopup, Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnityEventReflectionDispatchContinuesAfterAListenerThrows()
        {
            var signal = new UIActionEvent();
            var failure = new FormatException("反射派送測試");
            Exception reported = null;
            (string Source, string Action) received = default;
            signal.AddListener((_, _) => throw failure);
            signal.AddListener((source, action) => received = (source, action));

            Assert.That(UIEventDispatch.Invoke(signal, exception => reported = exception, "home", "choose"), Is.False);
            Assert.That(reported, Is.SameAs(failure));
            Assert.That(received, Is.EqualTo(("home", "choose")));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnityEventFallbackWarnsOnceAndPreservesNavigationExceptionPolicy()
        {
            // 「只警告一次」的旗標是靜態欄位，關閉 Domain Reload 時會留到下一次執行。
            UIEventDispatch.warnedFallback = false;
            var signal = new UnityEvent();
            var failure = new FormatException("公開 Invoke 測試");
            Exception reported = null;
            var laterCalled = false;
            signal.AddListener(() => throw failure);
            signal.AddListener(() => laterCalled = true);

            LogAssert.Expect(LogType.Warning, new Regex("找不到逐一派送 UnityEvent 所需的方法"));
            Assert.That(UIEventDispatch.InvokeFallback(signal.Invoke, exception => reported = exception), Is.False);
            Assert.That(reported, Is.SameAs(failure));
            Assert.That(Assert.Throws<FormatException>(() => UIEventDispatch.InvokeFallback(signal.Invoke, null)),
                Is.SameAs(failure));
            Assert.That(laterCalled, Is.False);

            signal.RemoveAllListeners();
            signal.AddListener(() => laterCalled = true);
            Assert.That(UIEventDispatch.InvokeFallback(signal.Invoke, null), Is.True);
            Assert.That(laterCalled, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FirstShowingPrecedesStartWithAnAnimatedEntry()
        {
            return VerifyLifecycleOnFirstShowing(animate: true);
        }

        [UnityTest]
        public IEnumerator InstantShowingNotifiesDisabledReceiversBeforeStart()
        {
            return VerifyLifecycleOnFirstShowing(animate: false);
        }

        private IEnumerator VerifyLifecycleOnFirstShowing(bool animate)
        {
            if (animate && !UITransition.IsDOTweenReady)
                Assert.Ignore("此案例需要 DOTween 才能驗證跨影格的進場。");

            CreateRoot();
            next.gameObject.SetActive(false);
            if (animate)
            {
                next.enter.preset = UITransitionPreset.Fade;
                next.enter.duration = 0.15f;
            }
            var timeline = new List<string>();
            var disabledTimeline = new List<string>();
            var enabled = AddHandler(next, "enabled");
            var disabled = AddHandler(next, "disabled");
            disabled.enabled = false;
            enabled.UnityLifecycle = timeline.Add;
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler == enabled)
                    timeline.Add(phase);
                if (handler == disabled)
                    disabledTimeline.Add(phase);
            };
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return null;
            Assert.That(timeline, Is.EqualTo(animate
                ? new[] { "Awake", "Showing", "Start", "Shown" }
                : new[] { "Awake", "Showing", "Shown", "Start" }));
            Assert.That(disabledTimeline, Is.EqualTo(new[] { "Showing", "Shown" }));
        }

        [UnityTest]
        public IEnumerator BackRouteCanBeAcceptedWithNoHistory()
        {
            CreateRoot();
            var route = new UIRoute { source = "home", action = "back", kind = UIRouteKind.Back };
            root.routes.Add(route);
            rootObject.SetActive(true);
            yield return StartPage();
            var timeline = new List<string>();
            root.ActionTriggered += accepted =>
            {
                Assert.That(accepted, Is.SameAs(route));
                timeline.Add("triggered");
            };
            root.ActionCompleted += (completed, result) =>
            {
                Assert.That(completed, Is.SameAs(route));
                timeline.Add(result.ToString());
            };

            yield return Expect(root.TriggerAsync("home", "back"), UINavResult.Unavailable);
            Assert.That(timeline, Is.EqualTo(new[] { "triggered", "Unavailable" }));
            Assert.That(root.CurrentPage, Is.SameAs(first));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PopupResultCanStartNavigationBeforeTheCloseRouteCompletes()
        {
            CreateRoot();
            root.routes.Add(new UIRoute
                { source = "popup", action = "accept", kind = UIRouteKind.ClosePopup, result = "yes" });
            rootObject.SetActive(true);
            yield return StartPage();
            var timeline = new List<string>();
            root.ViewShowing += view => timeline.Add($"showing:{view.Id}");
            root.ActionCompleted += (route, result) => timeline.Add($"route:{route.Action}/{result}");
            var caller = OpenAndNavigate();

            yield return Expect(root.TriggerAsync("popup", "accept"), UINavResult.Completed);
            yield return Expect(caller, UINavResult.Completed);
            Assert.That(timeline, Is.EqualTo(new[] { "showing:popup", "result", "showing:next", "route:accept/Completed" }));

            async Awaitable<UINavResult> OpenAndNavigate()
            {
                var result = await root.OpenPopupForResultAsync<string>("popup");
                Assert.That(result.Status, Is.EqualTo(UINavResult.Completed));
                Assert.That(result.Value, Is.EqualTo("yes"));
                timeline.Add("result");
                return await root.ShowPageAsync("next");
            }
        }

        private void ExpectBusyWarning(string rejected, string navigation, string phase, string viewId)
        {
            ExpectLog(LogType.Warning, rejected,
                $"目前導航 {navigation} 正在通知 {phase}（畫面 '{viewId}'）。請等這次導航結束後再呼叫。");
        }
    }
}
