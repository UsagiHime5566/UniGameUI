using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniGameUI.Tests
{
    public sealed class UINavigationTests
    {
        private GameObject rootObject;
        private GameObject prefabContainer;
        private UIRoot root;
        private UIView first;
        private UIView next;

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
            next.Showed.AddListener(() => events.Add("showed"));
            rootObject.SetActive(true);
            yield return StartPage();

            var transition = root.ShowPageAsync("next");
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(next.gameObject.activeInHierarchy, Is.True);
            Assert.That(next.Motion.localScale, Is.EqualTo(pose * 0.5f));
            yield return Expect(root.BackAsync(), UINavResult.Busy);
            yield return Expect(transition, UINavResult.Completed);
            Assert.That(events, Is.EqualTo(new[] { "hiding", "showing", "hidden", "showed" }));
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
            next.Showed.AddListener(() => commits++);
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
            var popup = CreateView("popup", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "home", kind = UIEntryKind.Page, review = first });
            root.entries.Add(new UIEntry { id = "next", kind = UIEntryKind.Page, review = next });
            root.entries.Add(new UIEntry { id = "popup", kind = UIEntryKind.Popup, prefab = popup });
            root.routes.Add(new UIRoute { source = "home", action = "next", kind = UIRouteKind.ShowPage, target = "next" });
            root.routes.Add(new UIRoute { source = "next", action = "popup", kind = UIRouteKind.OpenPopup, target = "popup" });
            root.startPageId = "home";
        }

        private static UIView CreateView(string id, Transform parent)
        {
            var gameObject = new GameObject(id, typeof(RectTransform), typeof(CanvasGroup));
            gameObject.transform.SetParent(parent, false);
            var motion = new GameObject("Motion", typeof(RectTransform));
            motion.transform.SetParent(gameObject.transform, false);
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
