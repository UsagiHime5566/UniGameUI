using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    public sealed class UINavigationTests
    {
        private GameObject rootObject;
        private GameObject prefabContainer;
        private UIRoot root;
        private UIView first;
        private UIView next;
        private UIView popupTemplate;

        [TearDown]
        public void TearDown()
        {
            if (rootObject)
                Object.DestroyImmediate(rootObject);
            if (prefabContainer)
                Object.DestroyImmediate(prefabContainer);
        }

        [UnityTest]
        public IEnumerator BackDismissesPopupThenReturnsToPreviousSceneView()
        {
            CreateRoot();
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.TriggerAsync("home", "next"), UINavResult.Completed);
            yield return Expect(root.TriggerAsync("next", "popup"), UINavResult.Completed);
            Assert.That(next.Group.interactable, Is.False);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));

            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(next.Group.interactable, Is.True);

            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator AnimationPresetsStillNavigateWithoutDOTween()
        {
            if (UITransition.IsDOTweenReady)
                Assert.Ignore("This case exercises the no-DOTween fallback.");

            CreateRoot();
            first.enter.preset = UITransitionPreset.Slide;
            next.enter.preset = UITransitionPreset.Zoom;
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            yield return Expect(root.BackAsync(), UINavResult.Completed);
        }

        [UnityTest]
        public IEnumerator ConcurrentTransitionOverlapsAndRestoresZoomPose()
        {
            if (!UITransition.IsDOTweenReady)
                Assert.Ignore("Install DOTween to exercise real animation.");

            CreateRoot();
            root.pageTransitionMode = UIPageTransitionMode.Concurrent;
            first.exit.preset = UITransitionPreset.Fade;
            first.exit.duration = 0.2f;
            next.enter.preset = UITransitionPreset.Zoom;
            next.enter.duration = 0.3f;
            next.enter.zoomScale = 0.5f;
            var pose = new Vector3(1.2f, 1.1f, 1);
            next.Motion.localScale = pose;
            var events = new List<string>();
            first.Hiding.AddListener(() => events.Add("hiding"));
            next.Showing.AddListener(() => events.Add("showing"));
            first.Hidden.AddListener(() => events.Add("hidden"));
            next.Shown.AddListener(() => events.Add("shown"));
            rootObject.SetActive(true);
            yield return StartPage();

            var transition = root.ShowPageAsync("next");
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(next.gameObject.activeInHierarchy, Is.True);
            Assert.That(next.Motion.localScale, Is.EqualTo(pose * 0.5f));
            yield return Expect(root.BackAsync(), UINavResult.Busy);
            yield return Expect(transition, UINavResult.Completed);
            Assert.That(events, Is.EqualTo(new[] { "hiding", "showing", "hidden", "shown" }));
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(next.Motion.localScale, Is.EqualTo(pose));
        }

        [UnityTest]
        public IEnumerator DisablingRootDuringAnAnimatedTransitionRestoresPreviousView()
        {
            if (!UITransition.IsDOTweenReady)
                Assert.Ignore("Install DOTween to exercise real animation cancellation.");

            CreateRoot();
            root.pageTransitionMode = UIPageTransitionMode.Concurrent;
            first.exit.preset = UITransitionPreset.Fade;
            first.exit.duration = 0.5f;
            next.enter.preset = UITransitionPreset.Zoom;
            next.enter.duration = 0.5f;
            var commits = 0;
            first.Hidden.AddListener(() => commits++);
            next.Shown.AddListener(() => commits++);
            rootObject.SetActive(true);
            yield return StartPage();

            var transition = root.ShowPageAsync("next");
            Assert.That(next.gameObject.activeSelf, Is.True);
            rootObject.SetActive(false);
            yield return Expect(transition, UINavResult.Canceled);
            rootObject.SetActive(true);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(first.Group.interactable, Is.True);
            Assert.That(next.gameObject.activeSelf, Is.False);
            Assert.That(next.Motion.localScale, Is.EqualTo(Vector3.one));
            Assert.That(commits, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MissingViewIdsAndWrongViewKindsReturnNotFound()
        {
            CreateRoot();
            root.entries.Add(new UIEntry { id = "withoutPrefab", kind = UIEntryKind.Popup });
            rootObject.SetActive(true);
            yield return StartPage();

            foreach (var id in new[] { "", "missingPage", "popup" })
            {
                ExpectLog(LogType.Error, $"ShowPage '{id}'", $"找不到頁面 '{id}'");
                yield return Expect(root.ShowPageAsync(id), UINavResult.NotFound);
            }
            foreach (var id in new[] { "", "missingPopup", "home" })
            {
                ExpectLog(LogType.Error, $"OpenPopup '{id}'", $"找不到 Popup '{id}'");
                yield return Expect(root.OpenPopupAsync(id), UINavResult.NotFound);
            }
            ExpectLog(LogType.Error, "OpenPopup 'withoutPrefab'", "缺少 Prefab");
            yield return Expect(root.OpenPopupAsync("withoutPrefab"), UINavResult.NotFound);
        }

        [UnityTest]
        public IEnumerator RouteLogsMissingActionAndMissingTarget()
        {
            CreateRoot();
            root.routes.Add(new UIRoute
                { source = "home", action = "lostTarget", kind = UIRouteKind.ShowPage, target = "missingPage" });
            rootObject.SetActive(true);
            yield return StartPage();

            ExpectLog(LogType.Error, "Trigger 'home'/'unregistered'", "找不到路由");
            yield return Expect(root.TriggerAsync("home", "unregistered"), UINavResult.NotFound);
            ExpectLog(LogType.Error, "Trigger 'home'/'lostTarget'", "找不到頁面 'missingPage'");
            yield return Expect(root.TriggerAsync("home", "lostTarget"), UINavResult.NotFound);
        }

        [UnityTest]
        public IEnumerator UnknownRouteKindReturnsFailedAndLogsScene()
        {
            CreateRoot();
            root.routes.Add(new UIRoute { source = "home", action = "invalidKind", kind = (UIRouteKind)99 });
            rootObject.SetActive(true);
            yield return StartPage();

            ExpectFailure("Trigger 'home'/'invalidKind'", "無法識別路由種類 '99'");
            yield return Expect(root.TriggerAsync("home", "invalidKind"), UINavResult.Failed);
        }

        [UnityTest]
        public IEnumerator ValidRequestsCanBeUnavailableInTheCurrentState()
        {
            CreateRoot();
            ExpectLog(LogType.Warning, "ShowPage 'next'", "工作區停用或尚未就緒");
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Unavailable);

            rootObject.SetActive(true);
            yield return StartPage();
            ExpectLog(LogType.Warning, "ShowPage 'home'", "目前已在頁面 'home'");
            yield return Expect(root.ShowPageAsync("home"), UINavResult.Unavailable);
            ExpectLog(LogType.Warning, "ClosePopup '(無)'", "沒有 Popup 可關閉");
            yield return Expect(root.ClosePopupAsync(), UINavResult.Unavailable);
            ExpectLog(LogType.Warning, "Trigger 'next'/'popup'", "來源不是目前最上層的畫面");
            yield return Expect(root.TriggerAsync("next", "popup"), UINavResult.Unavailable);

            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            ExpectLog(LogType.Warning, "OpenPopup 'popup'", "Popup 'popup' 已經開啟");
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Unavailable);

            root.enabled = false;
            ExpectLog(LogType.Warning, "Trigger 'home'/'next'", "工作區停用或尚未就緒");
            yield return Expect(root.TriggerAsync("home", "next"), UINavResult.Unavailable);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EmptyHistoryAndCallerCancellationAreQuiet()
        {
            CreateRoot();
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            yield return Expect(root.ShowPageAsync("next", cancellation: cancellation.Token), UINavResult.Canceled);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ListenerFailureReturnsFailedAndRestoresThePage()
        {
            CreateRoot();
            root.routes.Add(new UIRoute { source = "home", action = "emit", kind = UIRouteKind.Emit });
            rootObject.SetActive(true);
            yield return StartPage();

            next.Showing.AddListener(() => throw new InvalidOperationException("before-show"));
            ExpectFailure("ShowPage 'next'", "before-show");
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Failed);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            next.Showing.RemoveAllListeners();

            next.Shown.AddListener(() => throw new InvalidOperationException("after-show"));
            ExpectFailure("ShowPage 'next'", "after-show");
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Failed);
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(next.gameObject.activeSelf, Is.False);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            yield return Expect(root.BackAsync(), UINavResult.Unavailable);

            root.ActionEmitted.AddListener((_, _) => throw new InvalidOperationException("emit-handler"));
            ExpectFailure("Trigger 'home'/'emit'", "emit-handler");
            yield return Expect(root.TriggerAsync("home", "emit"), UINavResult.Failed);
        }

        [UnityTest]
        public IEnumerator TransitionFailureReturnsFailedAndRestoresThePage()
        {
            if (!UITransition.IsDOTweenReady)
                Assert.Ignore("此案例需要 DOTween 才能執行自訂轉場。");
            CreateRoot();
            next.enter.preset = UITransitionPreset.Custom;
            rootObject.SetActive(true);
            yield return StartPage();

            ExpectFailure("ShowPage 'next'", "UIDOTweenAnimation");
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Failed);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            Assert.That(first.Group.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator InitializationIdentifiesMissingAndDuplicateIds()
        {
            CreateRoot();
            root.entries.Add(new UIEntry());
            root.entries.Add(new UIEntry { id = "home" });
            root.routes.Add(new UIRoute { source = "home", action = "next" });
            root.routes.Add(new UIRoute { source = "unknownSource", action = "broken" });
            root.routes.Add(new UIRoute { source = "home", action = "" });

            ExpectLog(LogType.Error, "Awake", "畫面 ID '' 缺漏");
            ExpectLog(LogType.Error, "Awake", "畫面 ID 'home' 重複");
            ExpectLog(LogType.Error, "Awake", "路由 'home'/'next' 重複");
            ExpectLog(LogType.Error, "Awake", "路由 'unknownSource'/'broken' 的來源或動作缺漏");
            ExpectLog(LogType.Error, "Awake", "路由 'home'/'' 的來源或動作缺漏");
            rootObject.SetActive(true);
            yield return StartPage();
        }

        [UnityTest]
        public IEnumerator ButtonActionLogsMissingParents()
        {
            CreateRoot();
            var buttonWithoutView = new GameObject("NoView", typeof(RectTransform), typeof(Button), typeof(UIButtonAction));
            var parent = new GameObject("Without root");
            try
            {
                var view = CreateView("outside", parent.transform);
                var buttonWithoutRoot = AddButton(view, "withoutRoot");
                buttonWithoutView.GetComponent<UIButtonAction>().action = "withoutView";
                ExpectLog(LogType.Error, "UIButtonAction 'NoView'/'withoutView'", "UIView");
                buttonWithoutView.GetComponent<Button>().onClick.Invoke();
                ExpectLog(LogType.Error, "UIButtonAction 'outside'/'withoutRoot'", "UIRoot");
                buttonWithoutRoot.onClick.Invoke();
                yield return null;
            }
            finally
            {
                Object.DestroyImmediate(buttonWithoutView);
                Object.DestroyImmediate(parent);
            }
        }

        [UnityTest]
        public IEnumerator ChildCanvasRaycastReachesOnlyTheTopView()
        {
            CreateRoot();
            var sceneButton = AddButton(first, "open");
            AddButton(popupTemplate, "close");
            var events = new GameObject("UniGameUI raycast", typeof(EventSystem));
            try
            {
                rootObject.SetActive(true);
                yield return StartPage();
                var current = events.GetComponent<EventSystem>();
                Assert.That(Raycast(current).Any(hit => hit.gameObject == sceneButton.gameObject), Is.True,
                    "頁面的子 Canvas 應能接收 raycast。");

                yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
                var popup = root.GetComponentsInChildren<UIView>().Single(view => view.Id == "popup");
                var popupButton = popup.GetComponentInChildren<Button>();
                var hits = Raycast(current);
                Assert.That(hits.Any(hit => hit.gameObject == popupButton.gameObject), Is.True);
                Assert.That(hits.Any(hit => hit.gameObject == sceneButton.gameObject), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(events);
            }
        }

        private static Button AddButton(UIView view, string id)
        {
            var gameObject = new GameObject(id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAction));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(view.Content, false);
            UIRoot.Stretch(rect);
            gameObject.GetComponent<UIButtonAction>().action = id;
            return gameObject.GetComponent<Button>();
        }

        private static List<RaycastResult> Raycast(EventSystem eventSystem)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(eventSystem)
                { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            return results;
        }

        private void ExpectFailure(string request, string detail)
        {
            ExpectLog(LogType.Error, request, detail);
            ExpectLog(LogType.Exception, request, detail);
        }

        private void ExpectLog(LogType type, string request, string detail)
        {
            LogAssert.Expect(type, new Regex(Regex.Escape(request) + ".*" + Regex.Escape(detail) +
                ".*" + Regex.Escape($"Scene: {rootObject.scene.name}")));
        }

        private void CreateRoot()
        {
            rootObject = new GameObject("UniGameUI tests", typeof(RectTransform));
            rootObject.SetActive(false);
            root = rootObject.AddComponent<UIRoot>();
            var review = new GameObject("Review", typeof(RectTransform));
            review.transform.SetParent(rootObject.transform, false);
            root.reviewLayer = (RectTransform)review.transform;
            first = CreateView("home", review.transform);
            next = CreateView("next", review.transform);
            prefabContainer = new GameObject("Inactive popup template");
            prefabContainer.SetActive(false);
            popupTemplate = CreateView("popup", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "home", kind = UIEntryKind.Page, review = first });
            root.entries.Add(new UIEntry { id = "next", kind = UIEntryKind.Page, review = next });
            root.entries.Add(new UIEntry { id = "popup", kind = UIEntryKind.Popup, prefab = popupTemplate });
            root.routes.Add(new UIRoute { source = "home", action = "next", kind = UIRouteKind.ShowPage, target = "next" });
            root.routes.Add(new UIRoute { source = "next", action = "popup", kind = UIRouteKind.OpenPopup, target = "popup" });
            root.startPageId = "home";
        }

        private static UIView CreateView(string id, Transform parent)
        {
            var gameObject = new GameObject(id, typeof(RectTransform), typeof(CanvasGroup),
                typeof(Canvas), typeof(GraphicRaycaster));
            gameObject.transform.SetParent(parent, false);
            var motion = new GameObject("Motion", typeof(RectTransform));
            motion.transform.SetParent(gameObject.transform, false);
            UIRoot.Stretch((RectTransform)motion.transform);
            var view = gameObject.AddComponent<UIView>();
            view.id = id;
            view.canvasGroup = gameObject.GetComponent<CanvasGroup>();
            view.motion = (RectTransform)motion.transform;
            view.content = view.motion;
            return view;
        }

        private IEnumerator StartPage()
        {
            var deadline = Time.realtimeSinceStartup + 3f;
            while (root.CurrentPageId != "home" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
        }

        private static IEnumerator Expect(Awaitable<UINavResult> operation, UINavResult expected)
        {
            var awaiter = operation.GetAwaiter();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!awaiter.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(awaiter.IsCompleted, Is.True, "Navigation timed out.");
            Assert.That(awaiter.GetResult(), Is.EqualTo(expected));
        }
    }
}
