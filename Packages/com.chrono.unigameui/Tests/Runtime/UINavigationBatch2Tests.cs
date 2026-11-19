using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif

namespace UniGameUI.Tests
{
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator LifecycleReceivesDataAndNotifiesInOrderOnPagesAndPopup()
        {
            CreateRoot();
            root.startPageId = null;
            AddHandler(first, "first");
            AddHandler(first, "second");
            AddHandler(next, "first");
            AddHandler(popupTemplate, "first");
            first.Content.gameObject.AddComponent<UIViewTestHandler>().label = "child";
            var timeline = new List<string>();
            var firstData = new object();
            var nextData = new object();
            var popupData = new object();

            UIViewTestHandler.Observed = (handler, phase, data) =>
            {
                var view = handler.View;
                Assert.That(handler.Root, Is.SameAs(root));
                if (phase == "Showing")
                {
                    Assert.That(view.gameObject.activeInHierarchy, Is.True);
                    var expectedData = view.Id == "popup" ? popupData : view.Id == "home" ? firstData : nextData;
                    Assert.That(data, Is.SameAs(expectedData));
                    Assert.That(view.GetData<object>(), Is.SameAs(expectedData));
                }
                timeline.Add($"{view.Id}/{phase}/{handler.label}");
            };

            AddUnityEvents(first);
            AddUnityEvents(next);
            root.ViewShowing += view =>
            {
                timeline.Add($"{view.Id}/Showing/root");
                if (view.Id == "popup")
                    AddUnityEvents(view);
            };
            root.ViewShown += view => timeline.Add($"{view.Id}/Shown/root");
            root.ViewHiding += view => timeline.Add($"{view.Id}/Hiding/root");
            root.ViewHidden += view => timeline.Add($"{view.Id}/Hidden/root");
            rootObject.SetActive(true);
            Assert.That(root.TryGetView("next", out var inactive), Is.True);
            Assert.That(inactive, Is.SameAs(next));
            Assert.That(inactive.Root, Is.SameAs(root));

            yield return Expect(root.ShowPageAsync("home", firstData), UINavResult.Completed);
            Assert.That(first.TryGetData(out object retained), Is.True);
            Assert.That(retained, Is.SameAs(firstData));
            Assert.That(first.TryGetData<int>(out _), Is.False);
            Assert.That(Assert.Throws<InvalidOperationException>(() => first.GetData<int>()).Message,
                Does.Contain("home").And.Contain("System.Int32"));
            Assert.That(next.TryGetData<object>(out _), Is.False);
            Assert.That(Assert.Throws<InvalidOperationException>(() => next.GetData<string>()).Message,
                Does.Contain("next").And.Contain("System.String").And.Contain("null"));
            timeline.Clear();

            yield return Expect(root.ShowPageAsync("next", nextData), UINavResult.Completed);
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "home/Hiding/first", "home/Hiding/second", "home/Hiding/event", "home/Hiding/root",
                "next/Showing/first", "next/Showing/event", "next/Showing/root",
                "home/Hidden/first", "home/Hidden/second", "home/Hidden/event", "home/Hidden/root",
                "next/Shown/first", "next/Shown/event", "next/Shown/root",
            }));
            timeline.Clear();
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "next/Hiding/first", "next/Hiding/event", "next/Hiding/root",
                "home/Showing/first", "home/Showing/second", "home/Showing/event", "home/Showing/root",
                "next/Hidden/first", "next/Hidden/event", "next/Hidden/root",
                "home/Shown/first", "home/Shown/second", "home/Shown/event", "home/Shown/root",
            }));
            timeline.Clear();
            yield return Expect(root.OpenPopupAsync("popup", popupData), UINavResult.Completed);
            Assert.That(root.TopPopup.Root, Is.SameAs(root));
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "popup/Showing/first", "popup/Showing/root",
                "popup/Shown/first", "popup/Shown/event", "popup/Shown/root",
                "popup/Hiding/first", "popup/Hiding/event", "popup/Hiding/root",
                "popup/Hidden/first", "popup/Hidden/event", "popup/Hidden/root",
            }));

            void AddUnityEvents(UIView view)
            {
                view.Showing.AddListener(() => timeline.Add($"{view.Id}/Showing/event"));
                view.Shown.AddListener(() => timeline.Add($"{view.Id}/Shown/event"));
                view.Hiding.AddListener(() => timeline.Add($"{view.Id}/Hiding/event"));
                view.Hidden.AddListener(() => timeline.Add($"{view.Id}/Hidden/event"));
            }
        }

        [UnityTest]
        public IEnumerator ShowingHandlerFailureRestoresDataHistoryAndTheOpenPopup()
        {
            CreateRoot();
            var third = AddPage("third");
            AddHandler(third, "first");
            AddHandler(third, "second");
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            var waiting = root.OpenPopupForResultAsync<string>("popup");
            var popup = root.TopPopup;
            var laterCalled = false;
            var previousNotifications = new List<string>();
            next.Hiding.AddListener(() => previousNotifications.Add("Hiding"));
            next.Hidden.AddListener(() => previousNotifications.Add("Hidden"));
            next.Showing.AddListener(() => previousNotifications.Add("Showing"));
            third.Showing.AddListener(() => laterCalled = true);
            root.ViewShowing += view => laterCalled |= view == third;
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (phase != "Showing")
                    return;
                if (handler.label == "first")
                    throw new OperationCanceledException("handler-before-show");
                laterCalled = true;
            };

            ExpectLog(LogType.Error, "ShowPage 'third' Showing 'third'", "handler-before-show");
            LogAssert.Expect(LogType.Exception, "OperationCanceledException: handler-before-show");
            yield return Expect(root.ShowPageAsync("third", "new data", UIHistoryMode.Clear), UINavResult.Failed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(root.TopPopup, Is.SameAs(popup));
            Assert.That(popup.gameObject.activeInHierarchy && popup.Group.interactable, Is.True);
            Assert.That(next.gameObject.activeInHierarchy, Is.True);
            Assert.That(third.gameObject.activeSelf, Is.False);
            Assert.That(third.Data, Is.Null);
            Assert.That(laterCalled, Is.False);
            Assert.That(previousNotifications, Is.EqualTo(new[] { "Hiding" }));
            Assert.That(waiting.GetAwaiter().IsCompleted, Is.False);
            yield return Expect(popup.Close("retained"), UINavResult.Completed);
            yield return ExpectPopup(waiting, UINavResult.Completed, true, "retained");
        }

        [UnityTest]
        public IEnumerator HidingShownAndHiddenFailuresKeepNavigationMoving()
        {
            CreateRoot();
            AddHandler(first, "first");
            AddHandler(next, "first");
            AddHandler(next, "second");
            AddHandler(popupTemplate, "first");
            rootObject.SetActive(true);
            yield return StartPage();
            var received = new List<string>();
            UIViewTestHandler.Observed = (handler, phase, _) =>
            {
                if (handler.View.Id == "home" && phase == "Hiding")
                    throw new InvalidOperationException("handler-hiding");
                if (handler.View.Id == "next" && phase == "Shown")
                {
                    if (handler.label == "first")
                        throw new InvalidOperationException("handler-shown");
                    received.Add("second-handler");
                }
                if (handler.View.Id == "popup" && phase == "Hidden")
                    throw new InvalidOperationException("handler-hidden");
            };
            first.Hiding.AddListener(() => throw new InvalidOperationException("event-hiding"));
            first.Hiding.AddListener(() => received.Add("hiding-event"));
            next.Shown.AddListener(() => throw new InvalidOperationException("event-shown"));
            next.Shown.AddListener(() => received.Add("shown-event"));
            root.ViewShown += _ => received.Add("shown-root");

            ExpectNotificationException("ShowPage 'next' Hiding 'home'", "handler-hiding");
            ExpectNotificationException("ShowPage 'next' Hiding 'home'", "event-hiding");
            ExpectNotificationException("ShowPage 'next' Shown 'next'", "handler-shown");
            ExpectNotificationException("ShowPage 'next' Shown 'next'", "event-shown");
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(next));
            Assert.That(received, Is.EqualTo(new[] { "hiding-event", "second-handler", "shown-event", "shown-root" }));

            var waiting = root.OpenPopupForResultAsync<string>("popup");
            var popup = root.TopPopup;
            popup.Hidden.AddListener(() => throw new InvalidOperationException("event-hidden"));
            popup.Hidden.AddListener(() => received.Add("hidden-event"));
            root.ViewHidden += _ => received.Add("hidden-root");
            ExpectNotificationException("Close 'popup' Hidden 'popup'", "handler-hidden");
            ExpectNotificationException("Close 'popup' Hidden 'popup'", "event-hidden");
            yield return Expect(popup.Close("yes"), UINavResult.Completed);
            yield return ExpectPopup(waiting, UINavResult.Completed, true, "yes");
            Assert.That(received.GetRange(received.Count - 2, 2), Is.EqualTo(new[] { "hidden-event", "hidden-root" }));
            Assert.That(root.TopPopup, Is.Null);
            Assert.That(next.Group.interactable, Is.True);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator InspectorListenersKeepTheirArgumentsAndOriginalExceptionStack()
        {
            CreateRoot();
            var handler = AddHandler(next, "persistent");
            UnityEventTools.AddStringPersistentListener(next.Shown, handler.ThrowFromInspector, "persistent-failure");
            UnityEventTools.AddStringPersistentListener(next.Shown, handler.ReportFromInspector, "persistent-argument");
            var seen = new List<string>();
            UIViewTestHandler.Observed = (_, phase, _) => seen.Add(phase);
            next.Shown.AddListener(() => seen.Add("runtime"));
            rootObject.SetActive(true);
            yield return StartPage();

            string originalStack = null;
            void Capture(string message, string stack, LogType type)
            {
                if (type == LogType.Exception && message.Contains("persistent-failure"))
                    originalStack = stack;
            }
            Application.logMessageReceived += Capture;
            try
            {
                ExpectLog(LogType.Error, "ShowPage 'next' Shown 'next'",
                    "接收者丟出例外，導航照常處理。persistent-failure");
                LogAssert.Expect(LogType.Exception, "FormatException: persistent-failure");
                yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }
            Assert.That(originalStack, Does.Contain(nameof(UIViewTestHandler.ThrowFromInspector)));
            Assert.That(seen, Is.EqualTo(new[] { "Showing", "Shown", "persistent-argument", "runtime" }));
        }
#endif

        [UnityTest]
        public IEnumerator ElementsResolveOnScenePagesAndPopupInstancesAndRefreshOnAMiss()
        {
            CreateRoot();
            var button = AddElement(first, "confirm");
            var own = button.AddComponent<Image>();
            AddElement(first, "other").AddComponent<Button>();
            var child = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(button.transform, false);
            child.SetActive(false);
            button.SetActive(false);
            AddElement(popupTemplate, "confirm").AddComponent<Button>();
            rootObject.SetActive(true);
            yield return StartPage();

            Assert.That(first.GetElement("confirm"), Is.SameAs(button));
            Assert.That(first.Get<Image>("confirm"), Is.SameAs(own));
            Assert.That(first.Get<Button>("confirm"), Is.SameAs(child.GetComponent<Button>()));
            Assert.That(first.TryGet("other", out Button _), Is.True);
            Assert.That(first.TryGet("confirm", out CanvasGroup _), Is.False);
            Assert.That(first.TryGet("missing", out Image _), Is.False);
            Assert.That(Assert.Throws<InvalidOperationException>(() => first.Get<CanvasGroup>("confirm")).Message,
                Does.Contain("home").And.Contain("confirm").And.Contain("CanvasGroup").And.Contain(first.gameObject.scene.name));
            Assert.That(Assert.Throws<InvalidOperationException>(() => first.GetElement("missing")).Message,
                Does.Contain("home").And.Contain("missing").And.Contain("GameObject").And.Contain(first.gameObject.scene.name));

            var added = AddElement(first, "new").AddComponent<Image>();
            Assert.That(first.Get<Image>("new"), Is.SameAs(added));
            Object.DestroyImmediate(button);
            var replacement = AddElement(first, "confirm");
            Assert.That(first.GetElement("confirm"), Is.SameAs(replacement));

            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            var popup = root.TopPopup;
            Assert.That(popup.Get<Button>("confirm").transform.IsChildOf(popup.transform), Is.True);
            Assert.That(popup.GetElement("confirm"), Is.Not.SameAs(replacement));
            Assert.That(popup.TryGet("other", out Button _), Is.False);
        }

        [UnityTest]
        public IEnumerator HistoryModesAndClearHistoryControlWhereBackGoes()
        {
            CreateRoot();
            var third = AddPage("third");
            root.routes.Add(new UIRoute { source = "next", action = "replace", kind = UIRouteKind.ShowPage,
                target = "third", history = UIHistoryMode.Replace });
            rootObject.SetActive(true);
            yield return StartPage();
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Expect(root.ShowPageAsync("next", null, UIHistoryMode.Push), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.TriggerAsync("next", "replace"), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(third));
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPage, Is.SameAs(first));
            Assert.That(root.HistoryCount, Is.Zero);

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            yield return Expect(root.ShowPageAsync("third", null, UIHistoryMode.Clear, canceled.Token), UINavResult.Canceled);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            yield return Expect(root.ShowPageAsync("third", null, UIHistoryMode.Clear), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.Zero);
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            yield return Expect(root.ShowPageAsync("home"), UINavResult.Completed);
            Assert.That(root.HistoryCount, Is.EqualTo(1));
            root.ClearHistory();
            Assert.That(root.HistoryCount, Is.Zero);
            Assert.That(root.CurrentPage, Is.SameAs(first));
        }

        [UnityTest]
        public IEnumerator PrefabPagesAreCreatedOnDemandAndEachPopupExistsOnlyWhileOpen()
        {
            CreateRoot();
            root.mode = UIAuthoringMode.Prefab;
            root.startPageId = null;
            root.entries[0].prefab = CreateView("home", prefabContainer.transform);
            root.entries[1].prefab = CreateView("next", prefabContainer.transform);
            rootObject.SetActive(true);
            Assert.That(root.TryGetView("home", out _), Is.False);
            Assert.That(root.TryGetView("popup", out _), Is.False);
            Assert.That(root.TryGetView(null, out _), Is.False);

            yield return Expect(root.ShowPageAsync("home"), UINavResult.Completed);
            Assert.That(root.TryGetView("home", out var home), Is.True);
            Assert.That(home, Is.SameAs(root.CurrentPage).And.Not.SameAs(root.entries[0].Prefab));
            Assert.That(home.Root, Is.SameAs(root));
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(root.TryGetView("home", out var retained), Is.True);
            Assert.That(retained, Is.SameAs(home));
            Assert.That(retained.gameObject.activeSelf, Is.False);

            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            Assert.That(root.TryGetView("popup", out var popup), Is.True);
            Assert.That(popup, Is.SameAs(root.TopPopup).And.Not.SameAs(popupTemplate));
            Assert.That(popup.Root, Is.SameAs(root));
            Assert.That(root.Popups, Is.EqualTo(new[] { popup }));
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            Assert.That(root.TryGetView("popup", out _), Is.False);
            Assert.That(root.Popups, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PopupResultsWaitForExitAnimationAndHiddenNotification()
        {
            CreateRoot();
            if (UITransition.IsDOTweenReady)
            {
                popupTemplate.exit.preset = UITransitionPreset.Fade;
                popupTemplate.exit.duration = 0.1f;
            }
            rootObject.SetActive(true);
            yield return StartPage();

            var waiting = root.OpenPopupForResultAsync<int>("popup", "prompt");
            var popup = root.TopPopup;
            Assert.That(popup.GetData<string>(), Is.EqualTo("prompt"));
            Assert.That(waiting.GetAwaiter().IsCompleted, Is.False);
            var hidden = false;
            root.ViewHidden += view =>
            {
                if (view == popup)
                {
                    Assert.That(waiting.GetAwaiter().IsCompleted, Is.False);
                    hidden = true;
                }
            };
            var closing = popup.Close(17);
            if (UITransition.IsDOTweenReady)
            {
                Assert.That(closing.GetAwaiter().IsCompleted, Is.False);
                Assert.That(waiting.GetAwaiter().IsCompleted, Is.False);
            }
            yield return Expect(closing, UINavResult.Completed);
            yield return ExpectPopup(waiting, UINavResult.Completed, true, 17);
            Assert.That(hidden, Is.True);
            Assert.That(root.IsTransitioning, Is.False);
        }

        [UnityTest]
        public IEnumerator PopupCloseRoutesBackAndPageChangesResolveTheWait()
        {
            CreateRoot();
            var upper = CreateView("upper", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "upper", kind = UIEntryKind.Popup, prefab = upper });
            root.routes.Add(new UIRoute { source = "popup", action = "accept", kind = UIRouteKind.ClosePopup, result = "yes" });
            rootObject.SetActive(true);
            yield return StartPage();

            var routed = root.OpenPopupForResultAsync<string>("popup");
            yield return Expect(root.TriggerAsync("popup", "accept"), UINavResult.Completed);
            yield return ExpectPopup(routed, UINavResult.Completed, true, "yes");

            var back = root.OpenPopupForResultAsync<string>("popup");
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            yield return ExpectPopup(back, UINavResult.Completed);

            var closed = root.OpenPopupForResultAsync<string>("popup");
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return ExpectPopup(closed, UINavResult.Completed);

            var switched = root.OpenPopupForResultAsync<string>("popup");
            var switchedUpper = root.OpenPopupForResultAsync<string>("upper");
            Assert.That(root.Popups.Count, Is.EqualTo(2));
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return ExpectPopup(switched, UINavResult.Completed);
            yield return ExpectPopup(switchedUpper, UINavResult.Completed);
            Assert.That(root.TopPopup, Is.Null);
        }

        [UnityTest]
        public IEnumerator PopupReportsOpenFailuresAndMismatchedTypes()
        {
            CreateRoot();
            rootObject.SetActive(true);
            yield return StartPage();
            ExpectLog(LogType.Error, "OpenPopupForResult 'missing'", "找不到 Popup");
            yield return ExpectPopup(root.OpenPopupForResultAsync<string>("missing"), UINavResult.NotFound);
            var waiting = root.OpenPopupForResultAsync<int>("popup");
            ExpectLog(LogType.Warning, "OpenPopupForResult 'popup'", "已經開啟");
            yield return ExpectPopup(root.OpenPopupForResultAsync<int>("popup"), UINavResult.Unavailable);
            ExpectLog(LogType.Error, "OpenPopupForResult 'popup'", "System.String」與期望型別「System.Int32");
            yield return Expect(root.TopPopup.Close("wrong"), UINavResult.Completed);
            yield return ExpectPopup(waiting, UINavResult.Completed);
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            yield return Expect(root.TopPopup.Close("no listener"), UINavResult.Completed);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClosingIsLimitedToTheTopPopupAndExternalDestructionUnblocksTheStack()
        {
            CreateRoot();
            var upper = CreateView("upper", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "upper", kind = UIEntryKind.Popup, prefab = upper });
            rootObject.SetActive(true);
            yield return StartPage();

            ExpectLog(LogType.Warning, "Close 'home'", "只有目前最上層");
            yield return Expect(first.Close(), UINavResult.Unavailable);
            var lower = root.OpenPopupForResultAsync<string>("popup");
            var lowerPopup = root.TopPopup;
            var top = root.OpenPopupForResultAsync<string>("upper");
            var topPopup = root.TopPopup;
            ExpectLog(LogType.Warning, "Close 'popup'", "只有目前最上層");
            yield return Expect(lowerPopup.Close(), UINavResult.Unavailable);

            Object.Destroy(topPopup.gameObject);
            yield return ExpectPopup(top, UINavResult.Canceled);
            Assert.That(root.TopPopup, Is.SameAs(lowerPopup));
            Assert.That(lowerPopup.Group.interactable, Is.True);
            Assert.That(root.TryGetView("upper", out _), Is.False);
            Assert.That(lower.GetAwaiter().IsCompleted, Is.False);
            ExpectLog(LogType.Warning, "Close 'upper'", "只有目前最上層");
            yield return Expect(topPopup.Close(), UINavResult.Unavailable);
            yield return Expect(lowerPopup.Close("lower"), UINavResult.Completed);
            yield return ExpectPopup(lower, UINavResult.Completed, true, "lower");
        }

        [UnityTest]
        public IEnumerator CallerCancellationAndRootDisableEndTheWaitWithoutClosing()
        {
            CreateRoot();
            rootObject.SetActive(true);
            yield return StartPage();

            using var cancellation = new CancellationTokenSource();
            var canceled = root.OpenPopupForResultAsync<string>("popup", cancellation: cancellation.Token);
            var popup = root.TopPopup;
            var thread = new Thread(cancellation.Cancel) { IsBackground = true };
            thread.Start();
            yield return ExpectPopup(canceled, UINavResult.Canceled);
            Assert.That(thread.Join(1000), Is.True);
            Assert.That(root.TopPopup, Is.SameAs(popup));
            yield return Expect(popup.Close("discarded"), UINavResult.Completed);

            var disabled = root.OpenPopupForResultAsync<string>("popup");
            popup = root.TopPopup;
            root.enabled = false;
            yield return ExpectPopup(disabled, UINavResult.Canceled);
            Assert.That(root.TopPopup, Is.SameAs(popup));
            root.enabled = true;
            yield return Expect(popup.Close(), UINavResult.Completed);

            var destroyed = root.OpenPopupForResultAsync<string>("popup");
            Object.Destroy(rootObject);
            yield return ExpectPopup(destroyed, UINavResult.Canceled);
        }

        [UnityTest]
        public IEnumerator EmitNotifiesOnlyTheSourceAndContinuesAfterFailures()
        {
            CreateRoot();
            AddHandler(first, "first");
            AddHandler(first, "second");
            AddHandler(next, "other-page");
            AddHandler(popupTemplate, "popup");
            root.routes.Add(new UIRoute { source = "home", action = "select", kind = UIRouteKind.Emit });
            root.routes.Add(new UIRoute { source = "popup", action = "select", kind = UIRouteKind.Emit });
            rootObject.SetActive(true);
            yield return StartPage();
            var received = new List<string>();
            UIViewTestHandler.Observed = (handler, phase, data) =>
            {
                if (phase != "Action")
                    return;
                if (handler.label == "first")
                    throw new InvalidOperationException("source-action");
                received.Add($"{handler.label}/{data}");
            };
            root.ActionEmitted.AddListener((source, action) => received.Add($"event/{source}/{action}"));
            ExpectFailure("Trigger 'home'/'select' OnAction", "source-action");
            yield return Expect(root.TriggerAsync("home", "select"), UINavResult.Failed);
            yield return Expect(root.TriggerAsync("home", "next"), UINavResult.Completed);
            yield return Expect(root.TriggerAsync("next", "popup"), UINavResult.Completed);
            yield return Expect(root.TriggerAsync("popup", "select"), UINavResult.Completed);
            Assert.That(received, Is.EqualTo(new[] { "second/select", "event/home/select", "popup/select", "event/popup/select" }));
        }

        [UnityTest]
        public IEnumerator RouteEventsReportExecutionResultsAndIsolateTheirSubscribers()
        {
            CreateRoot();
            var route = root.routes[0];
            rootObject.SetActive(true);
            yield return StartPage();
            using var cancellation = new CancellationTokenSource();
            var timeline = new List<string>();
            Awaitable<UINavResult> busyAttempt = null;
            Awaitable<UIPopupResult<string>> busyOpen = null;
            root.ActionTriggered += _ => throw new InvalidOperationException("global-triggered");
            root.ActionTriggered += fired =>
            {
                Assert.That(fired, Is.SameAs(route));
                Assert.That(root.CurrentPage, Is.SameAs(first));
                timeline.Add("triggered");
                busyAttempt = root.TriggerAsync("home", "next");
                busyOpen = root.OpenPopupForResultAsync<string>("popup");
            };
            root.ViewShowing += _ => throw new InvalidOperationException("global-showing");
            root.ViewShowing += view => timeline.Add($"showing:{view.Id}");
            root.ActionCompleted += (_, _) => throw new InvalidOperationException("global-completed");
            root.ActionCompleted += (fired, status) =>
            {
                Assert.That(fired, Is.SameAs(route));
                Assert.That(root.IsTransitioning, Is.False);
                timeline.Add(status.ToString());
            };

            ExpectLog(LogType.Error, "Trigger 'home'/'absent'", "找不到路由");
            yield return Expect(root.TriggerAsync("home", "absent"), UINavResult.NotFound);
            ExpectLog(LogType.Warning, "Trigger 'next'/'popup'", "來源不是目前最上層");
            yield return Expect(root.TriggerAsync("next", "popup"), UINavResult.Unavailable);
            Assert.That(timeline, Is.Empty);

            next.Showing.AddListener(() => throw new InvalidOperationException("route-failed"));
            ExpectTriggerNotices();
            ExpectFailure("Trigger 'home'/'next' Showing 'next'", "route-failed");
            ExpectNotificationException("Trigger 'home'/'next' ActionCompleted", "global-completed");
            yield return Expect(root.TriggerAsync("home", "next"), UINavResult.Failed);
            yield return Expect(busyAttempt, UINavResult.Busy);
            yield return ExpectPopup(busyOpen, UINavResult.Busy);
            next.Showing.RemoveAllListeners();

            void CancelOnTrigger(UIRoute _) => cancellation.Cancel();
            root.ActionTriggered += CancelOnTrigger;
            ExpectTriggerNotices();
            ExpectNotificationException("Trigger 'home'/'next' ActionCompleted", "global-completed");
            yield return Expect(root.TriggerAsync("home", "next", cancellation.Token), UINavResult.Canceled);
            yield return Expect(busyAttempt, UINavResult.Busy);
            yield return ExpectPopup(busyOpen, UINavResult.Busy);
            root.ActionTriggered -= CancelOnTrigger;

            ExpectTriggerNotices();
            ExpectNotificationException("Trigger 'home'/'next' ViewShowing 'next'", "global-showing");
            ExpectNotificationException("Trigger 'home'/'next' ActionCompleted", "global-completed");
            yield return Expect(root.TriggerAsync("home", "next"), UINavResult.Completed);
            yield return Expect(busyAttempt, UINavResult.Busy);
            yield return ExpectPopup(busyOpen, UINavResult.Busy);
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "triggered", "Failed", "triggered", "Canceled", "triggered", "showing:next", "Completed",
            }));

            void ExpectTriggerNotices()
            {
                const string navigation = "Trigger 'home'/'next'";
                ExpectNotificationException($"{navigation} ActionTriggered", "global-triggered");
                ExpectBusyWarning(navigation, navigation, "ActionTriggered", "home");
                ExpectBusyWarning("OpenPopupForResult 'popup'", navigation, "ActionTriggered", "home");
            }
        }

        private static UIViewTestHandler AddHandler(UIView view, string label)
        {
            var handler = view.gameObject.AddComponent<UIViewTestHandler>();
            handler.label = label;
            return handler;
        }

        private UIView AddPage(string id)
        {
            var view = CreateView(id, root.reviewLayer);
            root.entries.Add(new UIEntry { id = id, kind = UIEntryKind.Page, review = view });
            return view;
        }

        private static GameObject AddElement(UIView view, string id)
        {
            var element = new GameObject(id, typeof(RectTransform), typeof(UIElementId));
            element.transform.SetParent(view.Content, false);
            element.GetComponent<UIElementId>().id = id;
            return element;
        }

        private static IEnumerator ExpectPopup<T>(Awaitable<UIPopupResult<T>> operation,
            UINavResult status, bool hasValue = false, T value = default)
        {
            var awaiter = operation.GetAwaiter();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!awaiter.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(awaiter.IsCompleted, Is.True, "等待 Popup 結果逾時。");
            var result = awaiter.GetResult();
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.HasValue, Is.EqualTo(hasValue));
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.GetValueOrDefault(), Is.EqualTo(value));
        }
    }
}
