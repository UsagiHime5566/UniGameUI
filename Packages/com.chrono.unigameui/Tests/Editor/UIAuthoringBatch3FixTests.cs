using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace UniGameUI.Editor.Tests
{
    // 第三批修正（第六輪審核的 J1～J7）。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void WrittenColorsStayWhenEveryComponentOfTheWidgetPrefabColorChanges()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            SceneManager.SetActiveScene(NewScene("Batch3Fix Pinned Colors"));
            UIWidgets.Install();
            // 讓底色可以用十六進位寫出完全相同的分量：計畫寫的顏色只和 Prefab 差一部分。
            var baseColor = (Color)new Color32(0x20, 0x40, 0x60, 0xFF);
            EditPrefab($"{widgets}/Panel.prefab", contents => contents.GetComponent<Image>().color = baseColor);
            EditPrefab($"{widgets}/Button.prefab", contents => contents.GetComponent<Image>().color = baseColor);
            Assert.That(Prefab("Image").GetComponent<Image>().color, Is.EqualTo(Color.white));
            Assert.That(Prefab("Text").GetComponent<TMP_Text>().color, Is.EqualTo(Color.white));

            var root = UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                { ""id"": ""icon"", ""kind"": ""Image"", ""color"": ""#FFD25A"" },
                { ""id"": ""frame"", ""kind"": ""Panel"", ""color"": ""#204080"" },
                { ""id"": ""go"", ""kind"": ""Button"", ""color"": ""#FF4060"", ""textColor"": ""#FFFF00"" },
                { ""id"": ""label"", ""color"": ""#FF0000"" },
                { ""id"": ""note"", ""textColor"": ""#00FF00"" },
                { ""id"": ""mute"", ""kind"": ""Toggle"", ""textColor"": ""#FF00FF"" },
                { ""id"": ""plainIcon"", ""kind"": ""Image"" },
                { ""id"": ""plainLabel"", ""text"": ""Plain"" },
                { ""id"": ""plainGo"", ""kind"": ""Button"" }
            ] }] }");
            var view = UIAuthoring.FindEntry(root, "home").Review;

            // 把元件 Prefab 的顏色四個分量都改掉，包含透明度。
            var changed = new Color(0f, 0f, 0f, 0.5f);
            EditPrefab($"{widgets}/Image.prefab", contents => contents.GetComponent<Image>().color = changed);
            EditPrefab($"{widgets}/Panel.prefab", contents => contents.GetComponent<Image>().color = changed);
            EditPrefab($"{widgets}/Button.prefab", contents =>
            {
                contents.GetComponent<Image>().color = changed;
                contents.GetComponentInChildren<TMP_Text>(true).color = changed;
            });
            EditPrefab($"{widgets}/Text.prefab", contents => contents.GetComponent<TMP_Text>().color = changed);
            EditPrefab($"{widgets}/Toggle.prefab",
                contents => contents.GetComponentInChildren<TMP_Text>(true).color = changed);

            Assert.That(Graphic<Image>("icon").color, Is.EqualTo(Html("#FFD25A")));
            Assert.That(Graphic<Image>("frame").color, Is.EqualTo(Html("#204080")));
            Assert.That(Graphic<Image>("go").color, Is.EqualTo(Html("#FF4060")));
            Assert.That(Label("go").color, Is.EqualTo(Html("#FFFF00")));
            Assert.That(Label("label").color, Is.EqualTo(Html("#FF0000")));
            Assert.That(Label("note").color, Is.EqualTo(Html("#00FF00")));
            Assert.That(Label("mute").color, Is.EqualTo(Html("#FF00FF")));

            // 計畫沒寫顏色的實例照舊跟著 Prefab。
            Assert.That(Graphic<Image>("plainIcon").color, Is.EqualTo(changed));
            Assert.That(Label("plainLabel").color, Is.EqualTo(changed));
            Assert.That(Graphic<Image>("plainGo").color, Is.EqualTo(changed));
            Assert.That(Label("plainGo").color, Is.EqualTo(changed));

            GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{widgets}/{name}.prefab");
            T Graphic<T>(string id) where T : Component => UIPlan.FindElement(view, id).GetComponent<T>();
            TMP_Text Label(string id) => UIPlan.FindElement(view, id).GetComponentInChildren<TMP_Text>(true);
        }

        [Test]
        public void WrittenOptionsAndKeysStayWhenTheWidgetPrefabListsChange()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            SceneManager.SetActiveScene(NewScene("Batch3Fix Pinned Options"));
            UIWidgets.Install();
            var starter = AssetDatabase.LoadAssetAtPath<GameObject>($"{widgets}/Dropdown.prefab")
                .GetComponent<TMP_Dropdown>().options.Select(option => option.text).ToArray();
            Assert.That(starter.Length, Is.EqualTo(3));
            EditPrefab($"{widgets}/Dropdown.prefab",
                contents => contents.GetComponent<UILocalizedDropdown>().keys = new[] { "a", "b", "c" });

            // 選項的數量和 Prefab 相同；「same」連文字與鍵值都和 Prefab 一樣。
            var root = UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""home"", ""elements"": [
                {{ ""id"": ""quality"", ""kind"": ""Dropdown"", ""options"": [""Low"", ""Mid"", ""High""],
                   ""optionKeys"": [""q.low"", ""q.mid"", ""q.high""], ""value"": 0 }},
                {{ ""id"": ""same"", ""kind"": ""Dropdown"",
                   ""options"": [{string.Join(", ", starter.Select(text => $@"""{text}"""))}],
                   ""optionKeys"": [""a"", ""b"", ""c""] }},
                {{ ""id"": ""plain"", ""kind"": ""Dropdown"" }}
            ] }},
            {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Options.prefab"", ""elements"": [
                {{ ""id"": ""mode"", ""kind"": ""Dropdown"", ""options"": [""One"", ""Two"", ""Three""] }}] }}
            ] }}");
            var view = UIAuthoring.FindEntry(root, "home").Review;
            var popup = UIAuthoring.FindEntry(root, "popup");
            var nested = UIPlan.FindElement(popup.Prefab, "mode");
            Assert.That(PrefabUtility.GetPropertyModifications(nested).Select(modification => modification.propertyPath)
                .Where(path => path.Contains("Array.data[3]")), Is.Empty, "Popup 的 Prefab 本體");
            foreach (var id in new[] { "quality", "same" })
            {
                var paths = PrefabUtility.GetPropertyModifications(UIPlan.FindElement(view, id))
                    .Select(modification => modification.propertyPath).ToArray();
                Assert.That(paths, Has.Some.EqualTo("m_Options.m_Options.Array.size"), id);
                Assert.That(paths, Has.Some.EqualTo("keys.Array.size"), id);
                Assert.That(paths.Where(path => path.Contains("Array.data[3]")), Is.Empty,
                    id + "：固定陣列長度時不可以留下多出來那一項的覆寫");
            }

            // 專案清空 Prefab 的範例選項，也換掉它的鍵值。
            EditPrefab($"{widgets}/Dropdown.prefab", contents =>
            {
                contents.GetComponent<TMP_Dropdown>().options.Clear();
                contents.GetComponent<UILocalizedDropdown>().keys = new[] { "x", "y", "z" };
            });
            Assert.That(Options("quality"), Is.EqualTo(new[] { "Low", "Mid", "High" }));
            Assert.That(Keys("quality"), Is.EqualTo(new[] { "q.low", "q.mid", "q.high" }));
            Assert.That(Options("same"), Is.EqualTo(starter));
            Assert.That(Keys("same"), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(Options("plain"), Is.Empty, "計畫沒寫選項的實例跟著 Prefab");
            Assert.That(Keys("plain"), Is.EqualTo(new[] { "x", "y", "z" }));
            Assert.That(nested.GetComponent<TMP_Dropdown>().options.Select(option => option.text),
                Is.EqualTo(new[] { "One", "Two", "Three" }), "Popup 的 Prefab 本體");
            Assert.That(UIAuthoring.HasReviewOverrides(popup), Is.False);

            string[] Options(string id)
            {
                return UIPlan.FindElement(view, id).GetComponent<TMP_Dropdown>().options
                    .Select(option => option.text).ToArray();
            }

            string[] Keys(string id) => UIPlan.FindElement(view, id).GetComponent<UILocalizedDropdown>().Keys.ToArray();
        }

        [Test]
        public void EventSystemRulesDependOnWhichLoadedSceneHoldsIt()
        {
            var scene = NewScene("Batch3Fix Input Own");
            var root = UIAuthoring.CreateWorkspace(scene);
            var own = scene.GetRootGameObjects()
                .Select(gameObject => gameObject.GetComponentInChildren<EventSystem>(true)).Single(found => found);
            Assert.That(InputIssues(root), Is.Empty);

            // 第一列：工作區自己的 Scene 有兩個。
            var second = InScene(scene, "Second EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var issue = InputIssues(root).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(issue.Message, Does.Contain(scene.name).And.Contain("2 個").And.Contain("Second EventSystem"));
            Object.DestroyImmediate(second);
            Assert.That(InputIssues(root), Is.Empty);

            // 第三列：其他載入的 Scene 也有一個，只是提醒。
            var other = NewScene("Batch3Fix Input Shared");
            var shared = InScene(other, "Shared EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            issue = InputIssues(root).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(issue.Message, Does.Contain("Shared EventSystem").And.Contain(other.name)
                .And.Contain("只能留一個"));
            // 輸入模組檢查的是工作區自己的那一個，另一個 Scene 的模組不對不算錯誤。
            Assert.That(InputErrors(root), Is.Empty);

            // 第四列：工作區的 Scene 沒有，其他 Scene 剛好一個；這時檢查的是那一個的輸入模組。
            own.gameObject.SetActive(false);
            issue = InputIssues(root).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(issue.Message, Does.Contain("Shared EventSystem").And.Contain(other.name)
                .And.Contain("InputSystemUIInputModule"));
            Object.DestroyImmediate(shared.GetComponent<StandaloneInputModule>());
            shared.AddComponent<InputSystemUIInputModule>();
            Assert.That(InputIssues(root), Is.Empty, "EventSystem 放在共用 Scene 是允許的做法");

            // 沒有寫在表格裡：工作區的 Scene 沒有，其他 Scene 有兩個，同樣提醒只能留一個。
            var third = InScene(other, "Third EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            issue = InputIssues(root).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(issue.Message, Does.Contain("Third EventSystem").And.Contain("只能留一個"));
            Object.DestroyImmediate(third);

            // 第二列：全部加起來沒有啟用的。訊息要分清楚「沒有啟用」和「沒有」。
            shared.GetComponent<EventSystem>().enabled = false;
            issue = InputIssues(root).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(issue.Message, Does.Contain("沒有啟用").And.Contain("Shared EventSystem")
                .And.Contain(own.name));
            Object.DestroyImmediate(shared);
            Object.DestroyImmediate(own.gameObject);
            issue = InputIssues(root).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(issue.Message, Does.Contain("沒有 EventSystem").And.Not.Contain("沒有啟用"));

            static GameObject InScene(Scene target, string name, params Type[] components)
            {
                var gameObject = new GameObject(name, components);
                SceneManager.MoveGameObjectToScene(gameObject, target);
                return gameObject;
            }
        }

        [Test]
        public void PlanReachesAnotherSceneWhileAWorkspaceSceneIsOpen()
        {
            RequireDOTween();
            var first = NewScene("Batch3Fix First Scene");
            SceneManager.SetActiveScene(first);
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"" }] }");

            // 建立第二個有 UI 的 Scene：它有自己的 EventSystem，檢查只是提醒。
            var createdPath = $"{folder}/Batch3Fix Created Scene.unity";
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("只能留一個"));
            var created = UIPlan.Apply($@"{{ ""scenePath"": ""{createdPath}"", ""createScene"": true,
                ""views"": [{{ ""id"": ""menu"" }}] }}", true);
            var createdScene = created.gameObject.scene;
            scenes.Add(createdScene);
            Assert.That(createdScene.path, Is.EqualTo(createdPath));
            Assert.That(File.Exists(createdPath), Is.True);
            Assert.That(first.isLoaded && createdScene.isLoaded, Is.True);
            Assert.That(createdScene.GetRootGameObjects()
                .Count(gameObject => gameObject.GetComponentInChildren<EventSystem>(true)), Is.EqualTo(1));
            Assert.That(created.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "menu" }));

            // 既有的另一個 Scene 也一樣：套用成功之後它維持附加開啟。
            Assert.That(EditorSceneManager.CloseScene(createdScene, true), Is.True);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("只能留一個"));
            var reopened = UIPlan.Apply($@"{{ ""scenePath"": ""{createdPath}"",
                ""views"": [{{ ""id"": ""settings"" }}] }}", true);
            scenes.Add(reopened.gameObject.scene);
            Assert.That(reopened.gameObject.scene.path, Is.EqualTo(createdPath));
            Assert.That(reopened.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "menu", "settings" }));
            Assert.That(LoadedScenes(), Is.EqualTo(new[] { first.path, createdPath }));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(first));
        }

        [Test]
        public void FailedPlanClosesTheScenesItOpened()
        {
            RequireDOTween();
            // 另一個既有的 Scene：裡面有兩個 EventSystem，套用完的檢查一定失敗。
            var brokenScene = NewScene("Batch3Fix Broken Scene");
            UIAuthoring.CreateWorkspace(brokenScene);
            SceneManager.MoveGameObjectToScene(
                new GameObject("Second EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)), brokenScene);
            Assert.That(EditorSceneManager.SaveScene(brokenScene), Is.True);
            var brokenPath = brokenScene.path;

            var first = NewScene("Batch3Fix Kept Scene");
            SceneManager.SetActiveScene(first);
            Assert.That(EditorSceneManager.CloseScene(brokenScene, true), Is.True);
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"" }] }");
            Assert.That(EditorSceneManager.SaveScene(first), Is.True);
            var before = LoadedScenes();
            Assert.That(before, Is.EqualTo(new[] { first.path }));

            // 套用到最後的檢查才失敗。
            var error = Assert.Throws<InvalidOperationException>(() => UIPlan.Apply(
                $@"{{ ""scenePath"": ""{brokenPath}"", ""views"": [{{ ""id"": ""menu"" }}] }}"));
            Assert.That(error.Message, Does.Contain("Second EventSystem"));
            Assert.That(LoadedScenes(), Is.EqualTo(before));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(first));

            // 預檢就失敗。
            Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                $@"{{ ""scenePath"": ""{brokenPath}"", ""startPage"": ""missing"" }}"));
            Assert.That(LoadedScenes(), Is.EqualTo(before));

            // 新建的 Scene 失敗時也關掉，而且沒有留下檔案。
            var newPath = $"{folder}/Batch3Fix Never Saved.unity";
            Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                $@"{{ ""scenePath"": ""{newPath}"", ""createScene"": true, ""startPage"": ""missing"" }}", true));
            Assert.That(LoadedScenes(), Is.EqualTo(before));
            Assert.That(File.Exists(newPath), Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(first));

            // 原本就開著的 Scene 沒有被動到，接著對它套用計畫可以成功。
            Assert.That(first.isDirty, Is.False);
            var root = UIPlan.Apply($@"{{ ""scenePath"": ""{first.path}"", ""views"": [{{ ""id"": ""next"" }}] }}");
            Assert.That(root.gameObject.scene, Is.EqualTo(first));
            Assert.That(root.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "home", "next" }));
            Assert.That(FunctionalErrors(root), Is.Empty);
        }

        [Test]
        public void FailedPlanPutsBackTheUntitledSceneItReplaced()
        {
            RequireDOTween();
            var brokenScene = NewScene("Batch3Fix Broken Single");
            UIAuthoring.CreateWorkspace(brokenScene);
            SceneManager.MoveGameObjectToScene(
                new GameObject("Second EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)), brokenScene);
            Assert.That(EditorSceneManager.SaveScene(brokenScene), Is.True);
            var brokenPath = brokenScene.path;

            // 只開著一個沒存過的空 Scene 時，目標 Scene 會取代它。
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(() => UIPlan.Apply(
                $@"{{ ""scenePath"": ""{brokenPath}"", ""views"": [{{ ""id"": ""menu"" }}] }}"));
            Assert.That(LoadedScenes(), Is.EqualTo(new[] { "" }));
            Assert.That(SceneManager.GetActiveScene().rootCount, Is.EqualTo(0));
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);

            // 之後還是可以正常建立別的 Scene。
            var path = $"{folder}/Batch3Fix After Failure.unity";
            var root = UIPlan.Apply($@"{{ ""scenePath"": ""{path}"", ""createScene"": true,
                ""views"": [{{ ""id"": ""home"" }}] }}", true);
            scenes.Add(root.gameObject.scene);
            Assert.That(LoadedScenes(), Is.EqualTo(new[] { path }));
        }

        [Test]
        public void PreflightReadsTheContentOfAnExistingViewPrefab()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            var confirmPath = $"{widgets}/Confirm.prefab";
            var before = File.ReadAllBytes(confirmPath);
            var root = WorkspaceWithHomePage("Batch3Fix Existing Prefab");

            // 確認框的 confirm 記錄的是 Button：用別的 kind 更新要在套用前被擋下。
            var error = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""other"" }},
                {{ ""id"": ""confirm"", ""kind"": ""Popup"", ""prefabPath"": ""{confirmPath}"", ""elements"": [
                    {{ ""id"": ""confirm"", ""kind"": ""Text"", ""updateExisting"": true,
                       ""text"": ""Yes"", ""alignment"": ""Left"" }}] }}] }}"));
            Assert.That(error.Message, Does.Contain("confirm").And.Contain("Button").And.Contain("Text"));
            AssertNothingWasApplied(root);
            Assert.That(File.ReadAllBytes(confirmPath), Is.EqualTo(before));

            // defaultSelected 指到 Prefab 裡不存在或不能選取的元件，一樣在套用前被擋下。
            foreach (var (target, reason) in new[] { ("missing", "不存在"), ("title", "不能被選取") })
            {
                error = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ ""views"": [
                    {{ ""id"": ""other"" }},
                    {{ ""id"": ""confirm"", ""kind"": ""Popup"", ""prefabPath"": ""{confirmPath}"",
                       ""defaultSelected"": ""{target}"" }}] }}"), target);
                Assert.That(error.Message, Does.Contain(target).And.Contain(reason));
                AssertNothingWasApplied(root);
            }

            // defaultSelected 指到 Prefab 裡的按鈕可以套用；同一個 kind 的 updateExisting 也可以。
            UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""confirm"", ""kind"": ""Popup"",
                ""prefabPath"": ""{confirmPath}"", ""defaultSelected"": ""confirm"", ""elements"": [
                    {{ ""id"": ""confirm"", ""kind"": ""Button"", ""updateExisting"": true, ""text"": ""Yes"" }}] }}] }}");
            var entry = UIAuthoring.FindEntry(root, "confirm");
            Assert.That(AssetDatabase.GetAssetPath(entry.Prefab), Is.EqualTo(confirmPath));
            Assert.That(entry.Review.DefaultSelected.GetComponentInParent<UIElementId>(true).Id, Is.EqualTo("confirm"));
            Assert.That(UIPlan.FindElement(entry.Review, "confirm").GetComponentInChildren<TMP_Text>(true).text,
                Is.EqualTo("Yes"));
            // 改動留在審核實例上等使用者套用，Prefab 本體仍然預設選取取消鍵。
            Assert.That(entry.Prefab.DefaultSelected.GetComponentInParent<UIElementId>(true).Id, Is.EqualTo("cancel"));
            Assert.That(File.ReadAllBytes(confirmPath), Is.EqualTo(before));

            // 登記過之後照舊以審核實例為準。
            error = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""confirm"", ""kind"": ""Popup"", ""elements"": [
                    {{ ""id"": ""cancel"", ""kind"": ""Toggle"", ""updateExisting"": true, ""state"": ""On"" }}] }}] }}"));
            Assert.That(error.Message, Does.Contain("cancel").And.Contain("Button").And.Contain("Toggle"));
        }

        [Test]
        public void StarterDropdownTemplateHasAVerticalScrollbarWithItsFullWidth()
        {
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            var scene = NewScene("Batch3Fix Dropdown Scrollbar");
            var parent = new GameObject("Parent", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(parent, scene);
            ((RectTransform)parent.transform).sizeDelta = new Vector2(800, 600);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>($"{widgets}/Dropdown.prefab"), parent.transform);

            var template = instance.GetComponent<TMP_Dropdown>().template;
            var scrollbar = template.GetComponent<ScrollRect>().verticalScrollbar;
            var bar = (RectTransform)scrollbar.transform;
            Assert.That(scrollbar.direction, Is.EqualTo(Scrollbar.Direction.BottomToTop));
            Assert.That(bar.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(bar.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(bar.pivot, Is.EqualTo(Vector2.one));
            Assert.That(bar.sizeDelta, Is.EqualTo(new Vector2(40f, 0f)));

            // 捲軸是貼在範本右側、和範本一樣高的直條。
            Assert.That(bar.rect.width, Is.EqualTo(40f).Within(0.01f));
            Assert.That(bar.rect.height, Is.EqualTo(template.rect.height).Within(0.01f));
            Assert.That(template.rect.height, Is.GreaterThan(100f));
            AssertInside(bar, template, "捲軸");

            // 滑動區域與把手都落在捲軸範圍內，把手和捲軸一樣寬。
            var handle = scrollbar.handleRect;
            var area = (RectTransform)handle.parent;
            Assert.That(area.rect.height, Is.EqualTo(bar.rect.height - 40f).Within(0.01f));
            AssertInside(area, bar, "滑動區域");
            Assert.That(handle.rect.width, Is.EqualTo(40f).Within(0.01f));
            AssertInside(handle, bar, "把手");

            static void AssertInside(RectTransform inner, RectTransform outer, string what)
            {
                var innerCorners = new Vector3[4];
                var outerCorners = new Vector3[4];
                inner.GetWorldCorners(innerCorners);
                outer.GetWorldCorners(outerCorners);
                Assert.That(innerCorners[0].x, Is.GreaterThanOrEqualTo(outerCorners[0].x - 0.01f), what + "的左邊");
                Assert.That(innerCorners[0].y, Is.GreaterThanOrEqualTo(outerCorners[0].y - 0.01f), what + "的下邊");
                Assert.That(innerCorners[2].x, Is.LessThanOrEqualTo(outerCorners[2].x + 0.01f), what + "的右邊");
                Assert.That(innerCorners[2].y, Is.LessThanOrEqualTo(outerCorners[2].y + 0.01f), what + "的上邊");
            }
        }

        [Test]
        public void ColorDerivedByTmpIsNotAPendingReviewOverride()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            foreach (var name in new[] { "Text", "Button" })
            {
                EditPrefab($"{widgets}/{name}.prefab",
                    contents => contents.GetComponentInChildren<TMP_Text>(true).color = Color.black);
            }
            SceneManager.SetActiveScene(NewScene("Batch3Fix Derived Color"));
            var root = UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""confirm"", ""kind"": ""Popup"",
                ""prefabPath"": ""{widgets}/Confirm.prefab"" }}] }}");
            var entry = UIAuthoring.FindEntry(root, "confirm");

            // TMP 產生網格時把 m_fontColor32 更新成 m_fontColor 的值。它和 Prefab 檔案裡的舊值不同，所以被記成覆寫。
            foreach (var text in entry.Review.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            var derived = Overrides();
            Assert.That(derived, Is.Not.Empty, "要先重現第六輪審核的情況");
            Assert.That(derived, Is.All.EqualTo("m_fontColor32.rgba"));
            Assert.That(UIAuthoring.HasReviewOverrides(entry), Is.False);

            // 真的改了審核實例的文字顏色，仍然算待套用。
            var title = UIPlan.FindElement(entry.Review, "title").GetComponentInChildren<TMP_Text>(true);
            title.color = Color.red;
            PrefabUtility.RecordPrefabInstancePropertyModifications(title);
            Assert.That(Overrides(), Has.Some.StartsWith("m_fontColor."));
            Assert.That(UIAuthoring.HasReviewOverrides(entry), Is.True);

            string[] Overrides()
            {
                return PrefabUtility.GetPropertyModifications(entry.Review.gameObject)
                    .Where(modification => !PrefabUtility.IsDefaultOverride(modification) &&
                        modification.propertyPath != "m_TextStyleHashCode")
                    .Select(modification => modification.propertyPath).ToArray();
            }
        }

        [Test]
        public void UpdatedDropdownOptionsAndKeysMustStayTheSameLength()
        {
            RequireDOTween();
            UseIsolatedWidgets();
            UIWidgets.Install();
            var root = WorkspaceWithHomePage("Batch3Fix Option Keys");
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                { ""id"": ""quality"", ""kind"": ""Dropdown"", ""options"": [""Low"", ""Mid"", ""High""],
                  ""optionKeys"": [""q.low"", ""q.mid"", ""q.high""] },
                { ""id"": ""plain"", ""kind"": ""Dropdown"", ""options"": [""A"", ""B"", ""C""] }] }] }");
            var view = UIAuthoring.FindEntry(root, "home").Review;
            Assert.That(Keys("plain"), Is.Empty);

            // 只換選項、數量和原本的鍵值不同：套用前報錯，請計畫同時提供 optionKeys。
            var error = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"",
                ""elements"": [
                    { ""id"": ""added"", ""text"": ""New"" },
                    { ""id"": ""quality"", ""kind"": ""Dropdown"", ""updateExisting"": true,
                      ""options"": [""On"", ""Off""] }] }] }"));
            Assert.That(error.Message, Does.Contain("home").And.Contain("quality").And.Contain("optionKeys"));
            Assert.That(UIPlan.FindElement(view, "added"), Is.Null);
            Assert.That(Options("quality"), Is.EqualTo(new[] { "Low", "Mid", "High" }));
            Assert.That(Keys("quality"), Is.EqualTo(new[] { "q.low", "q.mid", "q.high" }));

            // 數量相同時可以只換選項，鍵值留著。
            Update("quality", @"""options"": [""L"", ""M"", ""H""]");
            Assert.That(Options("quality"), Is.EqualTo(new[] { "L", "M", "H" }));
            Assert.That(Keys("quality"), Is.EqualTo(new[] { "q.low", "q.mid", "q.high" }));

            // 同時提供新的鍵值。
            Update("quality", @"""options"": [""On"", ""Off""], ""optionKeys"": [""s.on"", ""s.off""]");
            Assert.That(Options("quality"), Is.EqualTo(new[] { "On", "Off" }));
            Assert.That(Keys("quality"), Is.EqualTo(new[] { "s.on", "s.off" }));

            // 用空陣列清除所有鍵值。
            Update("quality", @"""options"": [""One"", ""Two"", ""Three""], ""optionKeys"": []");
            Assert.That(Options("quality"), Is.EqualTo(new[] { "One", "Two", "Three" }));
            Assert.That(Keys("quality"), Is.Empty);

            // 沒有鍵值的下拉選單不受影響。
            Update("plain", @"""options"": [""Only""]");
            Assert.That(Options("plain"), Is.EqualTo(new[] { "Only" }));
            Assert.That(Keys("plain"), Is.Empty);

            // 只給鍵值、數量和既有的選項不同，也在套用前報錯。
            error = Assert.Throws<ArgumentException>(() => Update("plain", @"""optionKeys"": [""a"", ""b""]"));
            Assert.That(error.Message, Does.Contain("plain").And.Contain("optionKeys"));
            Assert.That(Keys("plain"), Is.Empty);

            void Update(string id, string fields)
            {
                UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""home"", ""elements"": [
                    {{ ""id"": ""{id}"", ""kind"": ""Dropdown"", ""updateExisting"": true, {fields} }}] }}] }}");
            }

            string[] Options(string id)
            {
                return UIPlan.FindElement(view, id).GetComponent<TMP_Dropdown>().options
                    .Select(option => option.text).ToArray();
            }

            string[] Keys(string id) => UIPlan.FindElement(view, id).GetComponent<UILocalizedDropdown>().Keys.ToArray();
        }

        private static string[] LoadedScenes()
        {
            return Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(scene => scene.isLoaded).Select(scene => scene.path).ToArray();
        }

        // 和輸入有關的檢查結果，包含提醒。
        private static UIIssue[] InputIssues(UIRoot root)
        {
            return UIValidation.Check(root).Where(issue => issue.Message.Contains("EventSystem")).ToArray();
        }

        private static Color Html(string value)
        {
            Assert.That(ColorUtility.TryParseHtmlString(value, out var color), Is.True, value);
            return color;
        }
    }
}
