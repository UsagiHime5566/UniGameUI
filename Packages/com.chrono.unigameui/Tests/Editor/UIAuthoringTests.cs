using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace UniGameUI.Editor.Tests
{
    public sealed class UIAuthoringTests
    {
        private readonly List<Scene> scenes = new List<Scene>();
        private Scene original;
        private string folder;

        [SetUp]
        public void SetUp()
        {
            original = SceneManager.GetActiveScene();
            folder = "Assets/UniGameUI-Tests-" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            if (original.IsValid() && original.isLoaded)
                SceneManager.SetActiveScene(original);
            foreach (var scene in scenes)
            {
                if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);
                else if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            scenes.Clear();
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
        }

        [Test]
        public void PartialPlanKeepsHandEditedLayoutAndTransitions()
        {
            var scene = NewScene("PartialPlan");
            SceneManager.SetActiveScene(scene);
            var root = UIPlan.Apply(@"{
                ""startPage"": ""home"",
                ""views"": [{
                    ""id"": ""home"",
                    ""enter"": { ""preset"": ""Slide"", ""duration"": 0.4, ""direction"": ""Left"",
                                 ""ease"": ""OutBack"", ""distance"": 300, ""zoomScale"": 0.67 },
                    ""elements"": [{ ""id"": ""button"", ""kind"": ""Button"" }]
                }]
            }");
            var page = UIAuthoring.FindEntry(root, "home").Review;
            var button = UIPlan.FindElement(page, "button");
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(111, 222);
            rect.sizeDelta = new Vector2(400, 90);

            UIPlan.Apply(@"{
                ""views"": [{
                    ""id"": ""home"", ""updateExisting"": true, ""background"": ""#FF0000"",
                    ""elements"": [{ ""id"": ""button"", ""kind"": ""Button"", ""updateExisting"": true,
                                     ""color"": ""#0000FF"" }]
                }]
            }");
            Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(111, 222)));
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(400, 90)));
            Assert.That(page.Enter.Preset, Is.EqualTo(UITransitionPreset.Slide));
            Assert.That(page.Enter.Duration, Is.EqualTo(0.4f));
            Assert.That(page.Enter.Direction, Is.EqualTo(UITransitionDirection.Left));
            Assert.That(page.Enter.Ease, Is.EqualTo(UITransitionEase.OutBack));
            Assert.That(page.Enter.Distance, Is.EqualTo(300));
            Assert.That(page.Enter.ZoomScale, Is.EqualTo(0.67f));
            Assert.That(button.GetComponent<Image>().color, Is.EqualTo(Color.blue));
            Assert.That(page.Content.GetComponent<Image>().color, Is.EqualTo(Color.red));

            UIPlan.Apply(@"{
                ""views"": [{
                    ""id"": ""home"", ""updateExisting"": true, ""enter"": { ""duration"": 0.9 },
                    ""elements"": [{ ""id"": ""button"", ""kind"": ""Button"", ""updateExisting"": true,
                                     ""rect"": { ""size"": { ""x"": 500 } } }]
                }]
            }");
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(500, 90)));
            Assert.That(page.Enter.Duration, Is.EqualTo(0.9f));
            Assert.That(page.Enter.Preset, Is.EqualTo(UITransitionPreset.Slide));
            Assert.That(page.Enter.ZoomScale, Is.EqualTo(0.67f));
        }

        [Test]
        public void RenamingRouteUpdatesAllBindingsAndUndoesTogether()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Route"));
            var page = UIAuthoring.CreatePage(root, "home", "Home");
            var target = UIAuthoring.CreatePage(root, "target", "Target");
            var first = CreateBinding(page, "old");
            var second = CreateBinding(page, "old");
            var elsewhere = CreateBinding(target, "old");
            UIAuthoring.Connect(root, "home", "old", UIRouteKind.ShowPage, "target");
            UIAuthoring.Connect(root, "home", "taken", UIRouteKind.Emit);
            UIAuthoring.Connect(root, "target", "old", UIRouteKind.Back);
            Undo.FlushUndoRecordObjects();

            Assert.Throws<ArgumentException>(() => UIAuthoring.UpdateRoute(
                root, "home", "old", "taken", UIRouteKind.ShowPage, "target"));
            Assert.That(first.Action, Is.EqualTo("old"));

            UIAuthoring.UpdateRoute(root, "home", "old", "renamed", UIRouteKind.ShowPage, "target");
            Undo.FlushUndoRecordObjects();
            Assert.That(root.Routes.Count, Is.EqualTo(3));
            Assert.That(root.Routes.Any(route => route.Source == "home" && route.Action == "old"), Is.False);
            Assert.That(first.Action, Is.EqualTo("renamed"));
            Assert.That(second.Action, Is.EqualTo("renamed"));
            Assert.That(elsewhere.Action, Is.EqualTo("old"));
            Assert.That(UIValidation.Check(root).Any(issue => issue.Severity == UIIssueSeverity.Error), Is.False);

            Undo.PerformUndo();
            Assert.That(root.Routes.Any(route => route.Source == "home" && route.Action == "old"), Is.True);
            Assert.That(first.Action, Is.EqualTo("old"));
            Assert.That(second.Action, Is.EqualTo("old"));
        }

        [Test]
        public void PlayReviewCollectsAndAppliesPrefabOverrides()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Pending"));
            var popup = UIAuthoring.CreatePopup(root, "popup", "Popup", $"{folder}/Popup.prefab");
            var entry = UIAuthoring.FindEntry(root, "popup");
            Assert.That(UIReviewPlayMode.PendingReviews().Contains(entry), Is.False);

            var overlay = popup.GetComponentInChildren<Image>(true);
            overlay.color = Color.magenta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(overlay);
            Assert.That(UIReviewPlayMode.PendingReviews().Contains(entry), Is.True);
            Assert.That(entry.Prefab.GetComponentInChildren<Image>(true).color, Is.Not.EqualTo(Color.magenta));

            UIReviewPlayMode.ApplyPendingReviews(new[] { entry });
            Assert.That(UIReviewPlayMode.PendingReviews().Contains(entry), Is.False);
            Assert.That(entry.Prefab.GetComponentInChildren<Image>(true).color, Is.EqualTo(Color.magenta));
        }

        [TestCase(UIAuthoringMode.SingleScene)]
        [TestCase(UIAuthoringMode.Prefab)]
        public void BuildStrippingKeepsOnlySceneOwnedViews(UIAuthoringMode mode)
        {
            var scene = NewScene("BuildScene");
            var root = UIAuthoring.CreateWorkspace(scene, mode);
            var page = UIAuthoring.CreatePage(root, "home", "Home",
                mode == UIAuthoringMode.Prefab ? $"{folder}/Page.prefab" : null);
            var popup = UIAuthoring.CreatePopup(root, "popup", "Popup", $"{folder}/Popup.prefab");
            var pageEntry = UIAuthoring.FindEntry(root, "home");
            var popupEntry = UIAuthoring.FindEntry(root, "popup");
            UIAuthoring.Connect(root, "home", "popup", UIRouteKind.OpenPopup, "popup");
            EditorSceneManager.SaveScene(scene);
            var originalFile = File.ReadAllText(scene.path);

            UIReviewBuildProcessor.StripReview(scene);
            Assert.That(root.transform.Find("UI Review"), Is.Null);
            Assert.That(popup == null, Is.True);
            Assert.That(popupEntry.Review == null, Is.True);
            Assert.That(popupEntry.Prefab, Is.Not.Null);
            Assert.That(root.Routes.Single().Target, Is.EqualTo("popup"));
            if (mode == UIAuthoringMode.SingleScene)
            {
                Assert.That(pageEntry.Review, Is.SameAs(page));
                Assert.That(page.transform.parent, Is.EqualTo(root.transform));
            }
            else
            {
                Assert.That(pageEntry.Review == null, Is.True);
                Assert.That(pageEntry.Prefab, Is.Not.Null);
            }
            Assert.That(File.ReadAllText(scene.path), Is.EqualTo(originalFile));
        }

        [Test]
        public void BlueprintTracksActiveAndClosedScenes()
        {
            var firstScene = NewScene("First");
            var first = UIAuthoring.CreateWorkspace(firstScene);
            var secondScene = NewScene("Second");
            var second = UIAuthoring.CreateWorkspace(secondScene);
            SceneManager.SetActiveScene(firstScene);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Q<ObjectField>().value, Is.SameAs(first));
                SceneManager.SetActiveScene(secondScene);
                Assert.That(window.rootVisualElement.Q<ObjectField>().value, Is.SameAs(second));
                EditorSceneManager.CloseScene(secondScene, true);
                var activeRoot = UIAuthoring.FindWorkspace(SceneManager.GetActiveScene());
                Assert.That(window.rootVisualElement.Q<ObjectField>().value, Is.EqualTo(activeRoot));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private Scene NewScene(string name)
        {
            var active = SceneManager.GetActiveScene();
            var replaceUntitled = SceneManager.sceneCount == 1 &&
                string.IsNullOrEmpty(active.path) && !active.isDirty;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            scenes.Add(scene);
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            Assert.That(EditorSceneManager.SaveScene(scene, $"{folder}/{name}.unity"), Is.True);
            return scene;
        }

        private static UIButtonAction CreateBinding(UIView parent, string action)
        {
            var child = new GameObject("Button", typeof(RectTransform), typeof(Button));
            child.transform.SetParent(parent.Content, false);
            return UIAuthoring.BindButton(child.GetComponent<Button>(), action);
        }
    }
}
