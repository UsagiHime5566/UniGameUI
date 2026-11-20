using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using GraphicRaycaster = UnityEngine.UI.GraphicRaycaster;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    public sealed partial class UIAuthoringTests
    {
        private const string GeneratedParent = "Assets/UniGameUI";
        private const string Generated = GeneratedParent + "/Generated";
        private const string GeneratedAssembly = Generated + "/UniGameUI.Generated.asmdef";
        private readonly List<Scene> scenes = new List<Scene>();
        private Scene original;
        private string folder;
        private bool hadGeneratedParent;
        private bool hadGeneratedFolder;
        private bool hadGeneratedAssembly;

        [SetUp]
        public void SetUp()
        {
            original = SceneManager.GetActiveScene();
            folder = "Assets/UniGameUI-Tests-" + Guid.NewGuid().ToString("N");
            PrepareWidgetFixtures();
            hadGeneratedParent = Directory.Exists(GeneratedParent);
            hadGeneratedFolder = Directory.Exists(Generated);
            hadGeneratedAssembly = File.Exists(GeneratedAssembly);
            EditorApplication.LockReloadAssemblies();
        }

        [TearDown]
        public void TearDown()
        {
            try
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
                DeleteGeneratedFixtures();
                if (AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.DeleteAsset(folder);
                UIWidgets.FolderOverride = SharedWidgets;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private void DeleteGeneratedFixtures()
        {
            if (!Directory.Exists(Generated))
                return;
            foreach (var file in Directory.GetFiles(Generated, "*.cs"))
            {
                if (File.ReadLines(file).Take(4).Any(line => line.StartsWith("// 場景路徑: " + folder + "/", StringComparison.Ordinal)))
                    AssetDatabase.DeleteAsset(file.Replace('\\', '/'));
            }
            if (!hadGeneratedAssembly && File.Exists(GeneratedAssembly))
                AssetDatabase.DeleteAsset(GeneratedAssembly);
            DeleteNewEmptyFolder(Generated, hadGeneratedFolder);
            DeleteNewEmptyFolder(GeneratedParent, hadGeneratedParent);
        }

        private static void DeleteNewEmptyFolder(string path, bool alreadyExisted)
        {
            if (!alreadyExisted && Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                Assert.That(AssetDatabase.DeleteAsset(path), Is.True, path);
        }

        [Test]
        public void PartialPlanKeepsHandEditedLayoutAndTransitions()
        {
            RequireDOTween();
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
            Assert.That(FunctionalErrors(root), Is.Empty);

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

        [Test]
        public void UndoClearsTheBlueprintRouteSelection()
        {
            var scene = NewScene("Batch1 Route Undo");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            SceneManager.SetActiveScene(scene);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup();
                UIAuthoring.Connect(root, "home", "newRoute", UIRouteKind.Emit);
                Undo.FlushUndoRecordObjects();
                var serialized = new SerializedObject(window);
                serialized.FindProperty("selectedId").stringValue = "home";
                serialized.FindProperty("selectedAction").stringValue = "newRoute";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                window.CreateGUI();
                Undo.PerformUndo();

                serialized.Update();
                Assert.That(root.Routes, Is.Empty);
                Assert.That(serialized.FindProperty("selectedId").stringValue, Is.EqualTo("home"));
                Assert.That(serialized.FindProperty("selectedAction").stringValue, Is.Null.Or.Empty);
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void RuntimePrefabBindingErrorsClearWhenOverridesAreApplied()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Batch1 Prefab Binding"));
            var popup = UIAuthoring.CreatePopup(root, "popup", "Popup", $"{folder}/Binding.prefab");
            CreateBinding(popup, "oldClose");
            UIAuthoring.ApplyReviewOverrides(root, "popup");
            UIAuthoring.Connect(root, "popup", "oldClose", UIRouteKind.ClosePopup);
            Assert.That(FunctionalErrors(root), Is.Empty);

            UIAuthoring.UpdateRoute(root, "popup", "oldClose", "close", UIRouteKind.ClosePopup);
            Assert.That(FunctionalErrors(root).Single().Message, Does.Contain("Prefab 本體").And.Contain("oldClose"));
            UIAuthoring.ApplyReviewOverrides(root, "popup");
            Assert.That(FunctionalErrors(root), Is.Empty);
        }

        [Test]
        public void TransitionInspectorUsesTheSerializedUndoableField()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Batch1 Inspector"));
            var inspector = UnityEditor.Editor.CreateEditor(root);
            try
            {
                var controls = inspector.CreateInspectorGUI();
                Assert.That(controls.Query<PropertyField>().ToList().Any(field => field.bindingPath == "pageTransitionMode"),
                    Is.True);
                Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup();
                var property = inspector.serializedObject.FindProperty("pageTransitionMode");
                property.enumValueIndex = (int)UIPageTransitionMode.Concurrent;
                inspector.serializedObject.ApplyModifiedProperties();
                Undo.FlushUndoRecordObjects();
                Assert.That(root.PageTransitionMode, Is.EqualTo(UIPageTransitionMode.Concurrent));
                Undo.PerformUndo();
                inspector.serializedObject.Update();
                Assert.That(property.enumValueIndex, Is.EqualTo((int)UIPageTransitionMode.Sequential));
            }
            finally
            {
                Object.DestroyImmediate(inspector);
            }
        }

        [Test]
        public void NonPlayerSceneProcessingKeepsReviewInstances()
        {
            var scene = NewScene("Batch1 Non Player");
            var root = UIAuthoring.CreateWorkspace(scene);
            var popup = UIAuthoring.CreatePopup(root, "popup", "Popup", $"{folder}/Retained.prefab");
            new UIReviewBuildProcessor().OnProcessScene(scene, null);
            Assert.That(popup != null, Is.True);
            Assert.That(popup.transform.IsChildOf(root.transform.Find("UI Review")), Is.True);
        }

        [Test]
        public void FocusReviewSelectsTheView()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("此案例需要圖形裝置才能建立 Scene View。");
            var root = UIAuthoring.CreateWorkspace(NewScene("Batch1 Focus"));
            var page = UIAuthoring.CreatePage(root, "home", "Home");
            var oldSelection = Selection.activeObject;
            var oldWindow = SceneView.lastActiveSceneView;
            try
            {
                UIAuthoring.FocusReview(root, "home");
                Assert.That(Selection.activeGameObject, Is.EqualTo(page.gameObject));
                Assert.That(EditorWindow.GetWindow<SceneView>().in2DMode, Is.True);
            }
            finally
            {
                Selection.activeObject = oldSelection;
                if (!oldWindow)
                    EditorWindow.GetWindow<SceneView>().Close();
            }
        }

        [Test]
        public void DOTweenValidationReportsOneWorkspaceErrorAndBlocksPlansWhenUnavailable()
        {
            var scene = NewScene("Batch1 DOTween");
            SceneManager.SetActiveScene(scene);
            var root = UIAuthoring.CreateWorkspace(scene);
            var page = UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.SetTransition(page, true, UITransitionPreset.Fade);
            UIAuthoring.SetTransition(page, false, UITransitionPreset.Zoom);
            var dependencyIssues = UIValidation.Check(root)
                .Where(issue => issue.Code == UIIssueCode.DOTweenNotReady).ToArray();
            if (UIDOTweenSetup.IsReady)
            {
                Assert.That(UIDOTweenSetup.Status, Is.EqualTo(UIDOTweenStatus.Ready));
                Assert.That(dependencyIssues, Is.Empty);
                return;
            }

            Assert.That(dependencyIssues.Length, Is.EqualTo(1));
            Assert.That(dependencyIssues[0].Severity, Is.EqualTo(UIIssueSeverity.Error));
            var exception = Assert.Throws<InvalidOperationException>(() =>
                UIPlan.Apply(@"{ ""views"": [{ ""id"": ""other"" }] }"));
            Assert.That(exception.Message, Does.Contain("Setup DOTween"));
            Assert.That(root.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void LocalDOTweenSearchFindsTheStandardPackageUnderAnyCategory()
        {
            var temp = Path.Combine(Path.GetTempPath(), "UniGameUI-Cache-" + Guid.NewGuid().ToString("N"));
            var category = Path.Combine(temp, "Demigiant", "任意分類", "Nested");
            try
            {
                Directory.CreateDirectory(category);
                File.WriteAllText(Path.Combine(category, "DOTween Pro.unitypackage"), "");
                File.WriteAllText(Path.Combine(category, "DOTween Editor Preview.unitypackage"), "");
                Assert.That(UIDOTweenSetup.FindLocalPackage(temp), Is.Null);
                var standard = Path.Combine(category, "DOTween HOTween v2.unitypackage");
                File.WriteAllText(standard, "");
                Assert.That(UIDOTweenSetup.FindLocalPackage(temp), Is.EqualTo(standard));
            }
            finally
            {
                if (Directory.Exists(temp))
                    Directory.Delete(temp, true);
            }
        }

        [Test]
        public void GeneratedConstantsTrackSaveApplyAndViewChangesWithoutRewriting()
        {
            RequireDOTween();
            var scene = NewScene("Batch1 Constants");
            var path = "Assets/UniGameUI/Generated/Batch1ConstantsUI.cs";
            SceneManager.SetActiveScene(scene);
            var root = UIAuthoring.CreateWorkspace(scene);
            var page = UIAuthoring.CreatePage(root, "workshop", "Workshop");
            var popup = UIAuthoring.CreatePopup(root, "archive", "Archive", $"{folder}/Archive.prefab");
            UIAuthoring.Connect(root, "workshop", "openArchive", UIRouteKind.OpenPopup, "archive");
            UIAuthoring.Connect(root, "archive", "close", UIRouteKind.ClosePopup);
            var button = CreateBinding(page, "openArchive");
            SetElementId(button.gameObject, "open_archive");
            var previewOnly = CreateBinding(popup, "previewOnly");
            SetElementId(previewOnly.gameObject, "preview-icon");

            Assert.That(UIValidation.Check(root).Any(issue => issue.Severity == UIIssueSeverity.Warning &&
                issue.Message.Contains(path)), Is.True);
            Assert.That(File.Exists(path), Is.False);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var originalSource = File.ReadAllText(path);
            Assert.That(originalSource, Does.StartWith("// <auto-generated>").And.Contain("namespace UniGameUI.Generated")
                .And.Contain("public static class Batch1ConstantsUI")
                .And.Contain("public const string Workshop = \"workshop\";")
                .And.Contain("public const string PreviewOnly = \"previewOnly\";")
                .And.Contain("public const string PreviewIcon = \"preview-icon\";"));

            File.SetLastWriteTimeUtc(path, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var previousTimestamp = File.GetLastWriteTimeUtc(path);
            Assert.That(UIIdConstants.Generate(root), Is.EqualTo(path));
            Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(previousTimestamp));

            var serialized = new SerializedObject(root);
            serialized.FindProperty("entries").DeleteArrayElementAtIndex(1);
            serialized.FindProperty("routes").ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""9-end"", ""title"": ""New"" }], ""routes"": [
                { ""source"": ""workshop"", ""action"": ""openArchive"", ""kind"": ""Emit"" }
            ] }");
            var updatedSource = File.ReadAllText(path);
            Assert.That(updatedSource, Does.Contain("public const string _9End = \"9-end\";")
                .And.Not.Contain("public const string Archive ="));
        }

        [Test]
        public void ElementCanShareItsViewIdAndGeneratedAssemblyIsWrittenOnlyOnce()
        {
            RequireDOTween();
            var scene = NewScene("Batch2 Constants");
            SceneManager.SetActiveScene(scene);
            var existingAssembly = hadGeneratedFolder
                ? Directory.EnumerateFiles(Generated, "*.asmdef").FirstOrDefault() : null;

            var root = UIPlan.Apply(@"{
                ""views"": [{ ""id"": ""confirm"", ""elements"": [{ ""id"": ""confirm"", ""kind"": ""Image"" }] }]
            }");
            var path = UIIdConstants.Generate(root);
            Assert.That(File.ReadAllText(path), Does.Contain("public static class ConfirmView\n" +
                "            {\n                public const string Confirm = \"confirm\";"));

            var asmdef = existingAssembly ?? GeneratedAssembly;
            var assemblySource = File.ReadAllText(asmdef);
            if (existingAssembly == null)
            {
                Assert.That(assemblySource, Does.Contain("\"name\": \"UniGameUI.Generated\""));
                Assert.That(AssetDatabase.LoadMainAssetAtPath(asmdef), Is.Not.Null);
                File.SetLastWriteTimeUtc(asmdef, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            }
            var timestamp = File.GetLastWriteTimeUtc(asmdef);
            UIIdConstants.Generate(root);
            Assert.That(File.GetLastWriteTimeUtc(asmdef), Is.EqualTo(timestamp));
            Assert.That(File.ReadAllText(asmdef), Is.EqualTo(assemblySource));
        }

        [Test]
        public void PlanAddsAndPartiallyUpdatesRouteHistoryAndPopupResult()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch2 Plan Routes"));
            var root = UIPlan.Apply($@"{{
                ""views"": [
                    {{ ""id"": ""home"" }},
                    {{ ""id"": ""next"" }},
                    {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Popup.prefab"" }}
                ],
                ""routes"": [
                    {{ ""source"": ""home"", ""action"": ""next"", ""kind"": ""ShowPage"",
                        ""target"": ""next"", ""history"": ""Replace"" }},
                    {{ ""source"": ""next"", ""action"": ""home"", ""target"": ""home"" }},
                    {{ ""source"": ""popup"", ""action"": ""confirm"", ""kind"": ""ClosePopup"", ""result"": ""yes"" }}
                ]
            }}");
            Assert.That(root.Routes[0].History, Is.EqualTo(UIHistoryMode.Replace));
            Assert.That(root.Routes[1].History, Is.EqualTo(UIHistoryMode.Push));
            Assert.That(root.Routes[2].Result, Is.EqualTo("yes"));

            UIPlan.Apply(@"{
                ""routes"": [
                    { ""source"": ""home"", ""action"": ""next"", ""history"": ""Clear"" },
                    { ""source"": ""popup"", ""action"": ""confirm"", ""replace"": true, ""result"": ""no"" }
                ]
            }");
            Assert.That(root.Routes[0].History, Is.EqualTo(UIHistoryMode.Replace));
            Assert.That(root.Routes[2].Kind, Is.EqualTo(UIRouteKind.ClosePopup));
            Assert.That(root.Routes[2].Result, Is.EqualTo("no"));

            UIPlan.Apply(@"{
                ""routes"": [
                    { ""source"": ""home"", ""action"": ""next"", ""replace"": true, ""history"": ""Clear"" },
                    { ""source"": ""popup"", ""action"": ""confirm"", ""replace"": true, ""result"": """" }
                ]
            }");
            Assert.That(root.Routes[0].Kind, Is.EqualTo(UIRouteKind.ShowPage));
            Assert.That(root.Routes[0].Target, Is.EqualTo("next"));
            Assert.That(root.Routes[0].History, Is.EqualTo(UIHistoryMode.Clear));
            Assert.That(root.Routes[2].Result, Is.Empty);
        }

        [TestCase(UIRouteKind.Back, "history", "\"invalid\"", UIRouteKind.ShowPage, false)]
        [TestCase(UIRouteKind.Emit, "result", "\"\"", UIRouteKind.ClosePopup, false)]
        [TestCase(UIRouteKind.Back, "history", "null", UIRouteKind.ShowPage, true)]
        public void PlanRejectsRouteOptionsThatDoNotApply(UIRouteKind kind, string field, string jsonValue,
            UIRouteKind expectedKind, bool replaceExisting)
        {
            RequireDOTween();
            var scene = NewScene("Batch2 Invalid Route Option");
            SceneManager.SetActiveScene(scene);
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            if (replaceExisting)
                UIAuthoring.Connect(root, "home", "choose", kind);
            var route = replaceExisting ? @"""replace"": true" : $@"""kind"": ""{kind}""";
            var plan = $@"{{
                ""views"": [{{ ""id"": ""newPage"" }}],
                ""routes"": [{{ ""source"": ""home"", ""action"": ""choose"", {route}, ""{field}"": {jsonValue} }}]
            }}";

            var exception = Assert.Throws<ArgumentException>(() => UIPlan.Apply(plan));
            Assert.That(exception.Message, Does.Contain("home/choose").And.Contain(field)
                .And.Contain(expectedKind.ToString()).And.Contain(kind.ToString()));
            Assert.That(UIAuthoring.FindEntry(root, "newPage"), Is.Null);
            Assert.That(root.Routes.Count, Is.EqualTo(replaceExisting ? 1 : 0));
        }

        [Test]
        public void BlueprintSavesAndReloadsRouteHistoryAndPopupResult()
        {
            var scene = NewScene("Batch2 Blueprint Routes");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "popup", "Popup", $"{folder}/Popup.prefab");
            UIAuthoring.Connect(root, "home", "next", UIRouteKind.ShowPage, "next");
            UIAuthoring.Connect(root, "popup", "confirm", UIRouteKind.ClosePopup);
            SceneManager.SetActiveScene(scene);

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                var click = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);
                SelectRoute("home", "next");
                var history = window.rootVisualElement.Q<PopupField<UIHistoryMode>>("route-history");
                Assert.That(history.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                history.value = UIHistoryMode.Replace;
                Save();
                Assert.That(root.Routes[0].History, Is.EqualTo(UIHistoryMode.Replace));
                Assert.That(window.rootVisualElement.Q<PopupField<UIHistoryMode>>("route-history").value,
                    Is.EqualTo(UIHistoryMode.Replace));

                SelectRoute("popup", "confirm");
                var result = window.rootVisualElement.Q<TextField>("route-result");
                Assert.That(result.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                result.value = "yes";
                Save();
                Assert.That(root.Routes[1].Result, Is.EqualTo("yes"));

                void SelectRoute(string source, string action)
                {
                    var serialized = new SerializedObject(window);
                    serialized.FindProperty("selectedId").stringValue = source;
                    serialized.FindProperty("selectedAction").stringValue = action;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    window.CreateGUI();
                }

                void Save()
                {
                    var button = window.rootVisualElement.Q<UnityEngine.UIElements.Button>("save-route");
                    click.Invoke(button.clickable, new object[] { null });
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
            }

            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var path = scene.path;
            var mode = SceneManager.sceneCount > 1 ? OpenSceneMode.Additive : OpenSceneMode.Single;
            if (mode == OpenSceneMode.Additive)
                Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True);
            var reopened = EditorSceneManager.OpenScene(path, mode);
            scenes.Add(reopened);
            var saved = UIAuthoring.FindWorkspace(reopened);
            Assert.That(saved.Routes[0].History, Is.EqualTo(UIHistoryMode.Replace));
            Assert.That(saved.Routes[1].Result, Is.EqualTo("yes"));
        }

        [Test]
        public void IDCollisionsInEachConstantGroupAreErrors()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Batch1 ID Collision"));
            var first = UIAuthoring.CreatePage(root, "open_archive", "First");
            UIAuthoring.CreatePage(root, "open-archive", "Second");
            UIAuthoring.Connect(root, first.Id, "action_one", UIRouteKind.Emit);
            UIAuthoring.Connect(root, first.Id, "action-one", UIRouteKind.Emit);
            SetElementId(CreateBinding(first, "action_one").gameObject, "card_icon");
            SetElementId(CreateBinding(first, "action-one").gameObject, "card-icon");

            var messages = FunctionalErrors(root).Select(issue => issue.Message).ToArray();
            Assert.That(messages.Length, Is.EqualTo(4), string.Join("\n", messages));
            Assert.That(messages.Any(message => message.Contains("Views") && message.Contains("open-archive")), Is.True);
            Assert.That(messages.Any(message => message.Contains("Actions") && message.Contains("action-one")), Is.True);
            Assert.That(messages.Any(message => message.Contains("OpenArchive") && message.Contains("card-icon")), Is.True);
            Assert.Throws<InvalidOperationException>(() => UIIdConstants.Generate(root));
        }

        [Test]
        public void SameNamedScenesWithWorkspacesConflictEvenAfterOneCloses()
        {
            var firstScene = NewScene("Batch1 Duplicate Scene");
            var first = UIAuthoring.CreateWorkspace(firstScene);
            Assert.That(EditorSceneManager.SaveScene(firstScene), Is.True);
            var other = NewScene("Batch1 Duplicate Scene", "Other");
            var otherPath = other.path;
            var second = UIAuthoring.CreateWorkspace(other);
            Assert.That(FunctionalErrors(second).Any(issue => issue.Message.Contains(firstScene.path)), Is.True);

            LogAssert.Expect(LogType.Error, new Regex("無法更新場景.*", RegexOptions.Singleline));
            Assert.That(EditorSceneManager.SaveScene(other), Is.True);
            Assert.That(EditorSceneManager.CloseScene(other, true), Is.True);
            Assert.That(FunctionalErrors(first).Any(issue => issue.Message.Contains(otherPath)), Is.True);
        }

        [Test]
        public void TMPPopupHasItsOwnCanvasWithoutFalseOverrides()
        {
            RequireDOTween();
            var scene = NewScene("Batch1 TMP Canvas");
            SceneManager.SetActiveScene(scene);
            var root = UIPlan.Apply($@"{{
                ""views"": [{{
                    ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/TextPopup.prefab"",
                    ""elements"": [{{""id"": ""message"", ""kind"": ""Text"", ""text"": ""Popup message""}}]
                }}]
            }}");
            var entry = UIAuthoring.FindEntry(root, "popup");
            entry.Review.GetComponentInChildren<TextMeshProUGUI>().ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            AssertChildCanvas(entry.Review);
            AssertChildCanvas(entry.Prefab);
            Assert.That(UIAuthoring.HasReviewOverrides(entry), Is.False);
        }

        [Test]
        public void CanvasRepairUpdatesPrefabComponentsAndKeepsUnrelatedOverrides()
        {
            var scene = NewScene("Batch1 Repair Canvas");
            var root = UIAuthoring.CreateWorkspace(scene);
            var page = UIAuthoring.CreatePage(root, "home", "Home");
            Object.DestroyImmediate(page.GetComponent<GraphicRaycaster>());
            Object.DestroyImmediate(page.GetComponent<Canvas>());
            var popupPath = $"{folder}/Repair.prefab";
            var popup = UIAuthoring.CreatePopup(root, "popup", "Popup", popupPath);
            var prefab = PrefabUtility.LoadPrefabContents(popupPath);
            try
            {
                Object.DestroyImmediate(prefab.GetComponent<GraphicRaycaster>());
                Object.DestroyImmediate(prefab.GetComponent<Canvas>());
                PrefabUtility.SaveAsPrefabAsset(prefab, popupPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            var overlay = popup.GetComponentInChildren<Image>();
            overlay.color = Color.magenta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(overlay);
            var entry = UIAuthoring.FindEntry(root, "popup");
            Assert.That(UIValidation.Check(root).Count(issue => issue.Message.Contains("缺少子 Canvas")), Is.EqualTo(2));
            UIAuthoring.RepairViewCanvases(root);
            AssertChildCanvas(page);
            AssertChildCanvas(entry.Review);
            AssertChildCanvas(entry.Prefab);
            Assert.That(overlay.color, Is.EqualTo(Color.magenta));
            Assert.That(entry.Prefab.GetComponentInChildren<Image>().color, Is.Not.EqualTo(Color.magenta));
            Assert.That(UIAuthoring.HasReviewOverrides(entry), Is.True);
        }

        [Test]
        public void ShownRestoresPersistentListenersFromThePreviousSerializedField()
        {
            var scene = NewScene("Batch1 Event Migration");
            var root = UIAuthoring.CreateWorkspace(scene);
            var page = UIAuthoring.CreatePage(root, "home", "Home");
            UnityEventTools.AddBoolPersistentListener(page.Shown, page.gameObject.SetActive, true);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var path = scene.path;
            var legacy = typeof(UIView).GetField("shown", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetCustomAttribute<FormerlySerializedAsAttribute>().oldName;
            var yaml = File.ReadAllText(path);
            Assert.That(yaml, Does.Contain("  shown:"));
            if (SceneManager.sceneCount == 1)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.CloseScene(scene, true);
            File.WriteAllText(path, yaml.Replace("  shown:", $"  {legacy}:"));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var mode = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(SceneManager.GetActiveScene().path)
                ? OpenSceneMode.Single : OpenSceneMode.Additive;
            var reopened = EditorSceneManager.OpenScene(path, mode);
            scenes.Add(reopened);
            var migrated = UIAuthoring.FindEntry(UIAuthoring.FindWorkspace(reopened), "home").Review;
            Assert.That(migrated.Shown.GetPersistentEventCount(), Is.EqualTo(1));
            Assert.That(migrated.Shown.GetPersistentTarget(0), Is.EqualTo(migrated.gameObject));
            Assert.That(migrated.Shown.GetPersistentMethodName(0), Is.EqualTo(nameof(GameObject.SetActive)));
        }

        private static void RequireDOTween()
        {
            if (!UIDOTweenSetup.IsReady)
                Assert.Ignore("此案例需要 DOTween，請先匯入並完成 Setup DOTween。");
        }

        private static IEnumerable<UIIssue> FunctionalErrors(UIRoot root)
        {
            return UIValidation.Check(root).Where(issue => issue.Severity == UIIssueSeverity.Error &&
                issue.Code != UIIssueCode.DOTweenNotReady);
        }

        private static void SetElementId(GameObject gameObject, string id)
        {
            var serialized = new SerializedObject(gameObject.AddComponent<UIElementId>());
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertChildCanvas(UIView view)
        {
            Assert.That(view.TryGetComponent<Canvas>(out var canvas), Is.True);
            Assert.That(canvas.overrideSorting, Is.False);
            Assert.That(view.TryGetComponent<GraphicRaycaster>(out _), Is.True);
        }

        private Scene NewScene(string name, string subfolder = null)
        {
            var active = SceneManager.GetActiveScene();
            var replaceUntitled = SceneManager.sceneCount == 1 &&
                string.IsNullOrEmpty(active.path) && !active.isDirty;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            scenes.Add(scene);
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            var destination = folder;
            if (subfolder != null)
            {
                if (!AssetDatabase.IsValidFolder($"{folder}/{subfolder}"))
                    AssetDatabase.CreateFolder(folder, subfolder);
                destination = $"{folder}/{subfolder}";
            }
            Assert.That(EditorSceneManager.SaveScene(scene, $"{destination}/{name}.unity"), Is.True);
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
