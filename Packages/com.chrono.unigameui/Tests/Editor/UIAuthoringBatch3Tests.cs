using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace UniGameUI.Editor.Tests
{
    public sealed partial class UIAuthoringTests
    {
        // 測試用的元件集放在自己的資料夾，不會動到專案在 Assets/UniGameUI/Widgets 的元件。
        private const string SharedWidgets = "Assets/UniGameUI-TestWidgets";
        private const string TmpFolder = "Assets/TextMesh Pro";
        private const string FixtureStarted = "UniGameUI.Editor.Tests.FixtureStarted";
        private const string FixtureHadTmp = "UniGameUI.Editor.Tests.HadTmp";

        private static readonly UIWidgetKind[] StarterKinds =
        {
            UIWidgetKind.Text, UIWidgetKind.Image, UIWidgetKind.Panel, UIWidgetKind.Button, UIWidgetKind.Toggle,
            UIWidgetKind.Slider, UIWidgetKind.ProgressBar, UIWidgetKind.Dropdown, UIWidgetKind.InputField,
            UIWidgetKind.ScrollList
        };

        // 狀態記在 SessionState：測試途中若發生 Domain Reload，靜態欄位會被清掉。
        private void PrepareWidgetFixtures()
        {
            if (!SessionState.GetBool(FixtureStarted, false))
            {
                SessionState.SetBool(FixtureStarted, true);
                SessionState.SetBool(FixtureHadTmp, Directory.Exists(TmpFolder) || File.Exists(TmpFolder + ".meta"));
                if (AssetDatabase.IsValidFolder(SharedWidgets))
                    AssetDatabase.DeleteAsset(SharedWidgets);
            }
            UIWidgets.FolderOverride = SharedWidgets;
        }

        [OneTimeTearDown]
        public void RemoveWidgetFixtures()
        {
            UIWidgets.FolderOverride = null;
            if (!SessionState.GetBool(FixtureStarted, false))
                return;
            if (AssetDatabase.IsValidFolder(SharedWidgets))
                AssetDatabase.DeleteAsset(SharedWidgets);
            if (!SessionState.GetBool(FixtureHadTmp, true) && AssetDatabase.IsValidFolder(TmpFolder))
                AssetDatabase.DeleteAsset(TmpFolder);
            SessionState.EraseBool(FixtureStarted);
            SessionState.EraseBool(FixtureHadTmp);
        }

        private string UseIsolatedWidgets()
        {
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            UIWidgets.FolderOverride = $"{folder}/Widgets";
            return UIWidgets.FolderOverride;
        }

        [Test]
        public void NewWorkspaceUsesTheInputSystemModuleAndValidationRejectsOtherInputSetups()
        {
            var scene = NewScene("Batch3 Input Setup");
            var root = UIAuthoring.CreateWorkspace(scene);
            var eventSystem = scene.GetRootGameObjects()
                .Select(gameObject => gameObject.GetComponentInChildren<EventSystem>(true)).Single(found => found);
            Assert.That(eventSystem.GetComponent<BaseInputModule>(), Is.TypeOf<InputSystemUIInputModule>());
            Assert.That(InputErrors(root), Is.Empty);

            Object.DestroyImmediate(eventSystem.GetComponent<InputSystemUIInputModule>());
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            Assert.That(InputErrors(root).Single(), Does.Contain(eventSystem.name).And.Contain("InputSystemUIInputModule"));

            Object.DestroyImmediate(eventSystem.GetComponent<StandaloneInputModule>());
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            Assert.That(InputErrors(root), Is.Empty);

            var extra = new GameObject("Second EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            try
            {
                Assert.That(InputErrors(root).Single(), Does.Contain("剛好一個").And.Contain("2 個"));
            }
            finally
            {
                Object.DestroyImmediate(extra);
            }

            eventSystem.gameObject.SetActive(false);
            Assert.That(InputErrors(root).Single(), Does.Contain("剛好一個").And.Contain("0 個"));
            eventSystem.gameObject.SetActive(true);

            UIValidation.ActiveInputHandlingOverride = 0;
            try
            {
                Assert.That(InputErrors(root).Single(), Does.Contain("Active Input Handling"));
            }
            finally
            {
                UIValidation.ActiveInputHandlingOverride = null;
            }
            foreach (var supported in new[] { 1, 2 })
            {
                UIValidation.ActiveInputHandlingOverride = supported;
                try
                {
                    Assert.That(InputErrors(root), Is.Empty);
                }
                finally
                {
                    UIValidation.ActiveInputHandlingOverride = null;
                }
            }
        }

        [Test]
        public void InstallCreatesTheStarterWidgetsOnceAndOnlyRestoresWhatIsMissing()
        {
            var widgets = UseIsolatedWidgets();
            Assert.That(UIWidgets.Find(), Is.Null);

            var set = UIWidgets.Install();
            Assert.That(UIWidgets.Find(), Is.SameAs(set));
            Assert.That(AssetDatabase.GetAssetPath(set), Does.StartWith(widgets + "/"));
            foreach (var kind in StarterKinds)
            {
                var entry = set.Find(kind.ToString());
                Assert.That(entry, Is.Not.Null, kind.ToString());
                Assert.That(entry.Kind, Is.EqualTo(kind));
                Assert.That(AssetDatabase.GetAssetPath(entry.Prefab), Is.EqualTo($"{widgets}/{kind}.prefab"));
                Assert.That(entry.Prefab.GetComponentInChildren(UIWidgets.RequiredComponent(kind), true),
                    Is.Not.Null, kind.ToString());
            }
            Assert.That(set.Widgets.Count, Is.EqualTo(StarterKinds.Length));
            Assert.That(AssetDatabase.GetAssetPath(set.ListItem), Is.EqualTo($"{widgets}/ListItem.prefab"));
            Assert.That(AssetDatabase.GetAssetPath(set.Confirm), Is.EqualTo($"{widgets}/Confirm.prefab"));
            Assert.That(UIWidgets.ConfirmPrefabPath, Is.EqualTo($"{widgets}/Confirm.prefab"));
            Assert.That(set.Find("ScrollList").Prefab.GetComponent<UIList>().ItemPrefab, Is.SameAs(set.ListItem));
            Assert.That(UIList.GetInItem<TMP_Text>(set.ListItem, "label"), Is.Not.Null);

            // 專案改過的美術要留著：先改一個 Prefab，再確認重複安裝不會動到任何檔案。
            var buttonPath = $"{widgets}/Button.prefab";
            EditPrefab(buttonPath, contents => contents.GetComponent<Image>().color = Color.magenta);
            var before = Snapshot(widgets);
            Assert.That(UIWidgets.Install(), Is.SameAs(set));
            Assert.That(Snapshot(widgets), Is.EqualTo(before));

            Assert.That(AssetDatabase.DeleteAsset($"{widgets}/Slider.prefab"), Is.True);
            before = Snapshot(widgets);
            UIWidgets.Install();
            var after = Snapshot(widgets);
            var changed = after.Keys.Where(path => !before.TryGetValue(path, out var stamp) || stamp != after[path])
                .Select(Path.GetFileName).OrderBy(name => name).ToArray();
            Assert.That(changed, Is.EqualTo(new[] { "Slider.prefab", "Slider.prefab.meta", "UIWidgetSet.asset" }));
            Assert.That(set.Find("Slider").Prefab, Is.Not.Null);
            Assert.That(set.Find("Slider").Prefab.GetComponent<Slider>(), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(buttonPath).GetComponent<Image>().color,
                Is.EqualTo(Color.magenta));
        }

        [Test]
        public void PlanCreatesTheTenWidgetsAsPrefabInstancesAndUpdatesOnlyTheGivenFields()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch3 Widgets"));
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var root = UIPlan.Apply(@"{
                ""views"": [{ ""id"": ""home"", ""elements"": [
                    { ""id"": ""title"", ""text"": ""Hello"", ""fontSize"": 40, ""color"": ""#FF0000"",
                      ""alignment"": ""Left"" },
                    { ""id"": ""icon"", ""kind"": ""Image"", ""color"": ""#00FF00"" },
                    { ""id"": ""frame"", ""kind"": ""Panel"", ""color"": ""#0000FF"",
                      ""rect"": { ""size"": { ""x"": 600, ""y"": 400 } } },
                    { ""id"": ""start"", ""kind"": ""Button"", ""text"": ""Start"", ""action"": ""start"",
                      ""color"": ""#112233"", ""textColor"": ""#FFFF00"", ""fontSize"": 30 },
                    { ""id"": ""mute"", ""kind"": ""Toggle"", ""text"": ""Mute"", ""state"": ""Off"" },
                    { ""id"": ""volume"", ""kind"": ""Slider"", ""min"": 0, ""max"": 10, ""value"": 7,
                      ""numberMode"": ""Whole"" },
                    { ""id"": ""loading"", ""kind"": ""ProgressBar"", ""value"": 0.25 },
                    { ""id"": ""quality"", ""kind"": ""Dropdown"", ""options"": [""Low"", ""High""], ""value"": 1 },
                    { ""id"": ""name"", ""kind"": ""InputField"", ""text"": ""Ada"", ""placeholder"": ""Name"",
                      ""fontSize"": 24 },
                    { ""id"": ""saves"", ""kind"": ""ScrollList"", ""direction"": ""Horizontal"", ""spacing"": 12 }
                ] }],
                ""routes"": [{ ""source"": ""home"", ""action"": ""start"", ""kind"": ""Emit"" }]
            }");
            var set = UIWidgets.Find();
            var view = UIAuthoring.FindEntry(root, "home").Review;
            var expected = new Dictionary<string, string>
            {
                ["title"] = "Text", ["icon"] = "Image", ["frame"] = "Panel", ["start"] = "Button",
                ["mute"] = "Toggle", ["volume"] = "Slider", ["loading"] = "ProgressBar",
                ["quality"] = "Dropdown", ["name"] = "InputField", ["saves"] = "ScrollList"
            };
            foreach (var (id, kind) in expected)
            {
                var element = UIPlan.FindElement(view, id);
                Assert.That(element, Is.Not.Null, id);
                Assert.That(element.name, Is.EqualTo(id));
                Assert.That(element.transform.parent, Is.EqualTo(view.Content));
                Assert.That(element.GetComponent<UIElementId>().Kind, Is.EqualTo(kind));
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(element),
                    Is.SameAs(set.Find(kind).Prefab), id);
            }

            var title = UIPlan.FindElement(view, "title").GetComponent<TMP_Text>();
            Assert.That(title.text, Is.EqualTo("Hello"));
            Assert.That(title.fontSize, Is.EqualTo(40));
            Assert.That(title.color, Is.EqualTo(Color.red));
            Assert.That(title.horizontalAlignment, Is.EqualTo(HorizontalAlignmentOptions.Left));
            Assert.That(((RectTransform)title.transform).sizeDelta,
                Is.EqualTo(((RectTransform)set.Find("Text").Prefab.transform).sizeDelta), "預設大小取自元件 Prefab");
            Assert.That(UIPlan.FindElement(view, "icon").GetComponent<Image>().color, Is.EqualTo(Color.green));
            var frame = UIPlan.FindElement(view, "frame");
            Assert.That(frame.GetComponent<Image>().color, Is.EqualTo(Color.blue));
            Assert.That(((RectTransform)frame.transform).sizeDelta, Is.EqualTo(new Vector2(600, 400)));

            var start = UIPlan.FindElement(view, "start");
            Assert.That(start.GetComponent<UIButtonAction>().Action, Is.EqualTo("start"));
            Assert.That(start.GetComponent<Image>().color, Is.EqualTo((Color)new Color32(0x11, 0x22, 0x33, 255)));
            var startLabel = start.GetComponentInChildren<TMP_Text>();
            Assert.That(startLabel.text, Is.EqualTo("Start"));
            Assert.That(startLabel.color, Is.EqualTo((Color)new Color32(255, 255, 0, 255)));
            Assert.That(startLabel.fontSize, Is.EqualTo(30));

            var mute = UIPlan.FindElement(view, "mute");
            Assert.That(mute.GetComponent<Toggle>().isOn, Is.False);
            Assert.That(mute.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Mute"));
            var volume = UIPlan.FindElement(view, "volume").GetComponent<Slider>();
            Assert.That((volume.minValue, volume.maxValue, volume.value, volume.wholeNumbers),
                Is.EqualTo((0f, 10f, 7f, true)));
            var loading = UIPlan.FindElement(view, "loading").GetComponent<UIProgressBar>();
            Assert.That(loading.Value, Is.EqualTo(0.25f));
            Assert.That(loading.Fill.fillAmount, Is.EqualTo(0.25f));
            var quality = UIPlan.FindElement(view, "quality").GetComponent<TMP_Dropdown>();
            Assert.That(quality.options.Select(option => option.text), Is.EqualTo(new[] { "Low", "High" }));
            Assert.That(quality.value, Is.EqualTo(1));
            Assert.That(quality.captionText.text, Is.EqualTo("High"));
            var input = UIPlan.FindElement(view, "name").GetComponent<TMP_InputField>();
            Assert.That(input.text, Is.EqualTo("Ada"));
            Assert.That(((TMP_Text)input.placeholder).text, Is.EqualTo("Name"));
            Assert.That(input.pointSize, Is.EqualTo(24));
            Assert.That(input.textComponent.fontSize, Is.EqualTo(24));
            var saves = UIPlan.FindElement(view, "saves").GetComponent<UIList>();
            Assert.That(saves.Direction, Is.EqualTo(UIListDirection.Horizontal));
            Assert.That(saves.Content.GetComponent<HorizontalOrVerticalLayoutGroup>().spacing, Is.EqualTo(12));
            Assert.That(saves.ItemPrefab, Is.SameAs(set.ListItem));
            Assert.That(FunctionalErrors(root), Is.Empty);

            // 部分更新：只有寫出來的欄位會變。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                { ""id"": ""title"", ""updateExisting"": true, ""text"": ""Bye"" },
                { ""id"": ""start"", ""kind"": ""Button"", ""updateExisting"": true, ""textColor"": ""#FFFFFF"" },
                { ""id"": ""mute"", ""kind"": ""Toggle"", ""updateExisting"": true, ""state"": ""On"" },
                { ""id"": ""volume"", ""kind"": ""Slider"", ""updateExisting"": true, ""value"": 3 },
                { ""id"": ""loading"", ""kind"": ""ProgressBar"", ""updateExisting"": true, ""value"": 1 },
                { ""id"": ""quality"", ""kind"": ""Dropdown"", ""updateExisting"": true, ""value"": 0 },
                { ""id"": ""name"", ""kind"": ""InputField"", ""updateExisting"": true, ""placeholder"": ""Who"" },
                { ""id"": ""saves"", ""kind"": ""ScrollList"", ""updateExisting"": true, ""spacing"": 4 },
                { ""id"": ""icon"", ""kind"": ""Image"", ""color"": ""#000000"" }
            ] }] }");
            Assert.That(title.text, Is.EqualTo("Bye"));
            Assert.That((title.fontSize, title.color, title.horizontalAlignment),
                Is.EqualTo((40f, Color.red, HorizontalAlignmentOptions.Left)));
            Assert.That(startLabel.color, Is.EqualTo(Color.white));
            Assert.That((startLabel.text, startLabel.fontSize), Is.EqualTo(("Start", 30f)));
            Assert.That(start.GetComponent<Image>().color, Is.EqualTo((Color)new Color32(0x11, 0x22, 0x33, 255)));
            Assert.That(mute.GetComponent<Toggle>().isOn, Is.True);
            Assert.That(mute.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Mute"));
            Assert.That((volume.minValue, volume.maxValue, volume.value, volume.wholeNumbers),
                Is.EqualTo((0f, 10f, 3f, true)));
            Assert.That(loading.Fill.fillAmount, Is.EqualTo(1f));
            Assert.That(quality.value, Is.EqualTo(0));
            Assert.That(quality.options.Count, Is.EqualTo(2));
            Assert.That(((TMP_Text)input.placeholder).text, Is.EqualTo("Who"));
            Assert.That((input.text, input.pointSize), Is.EqualTo(("Ada", 24f)));
            Assert.That(saves.Content.GetComponent<HorizontalOrVerticalLayoutGroup>().spacing, Is.EqualTo(4));
            Assert.That(saves.Direction, Is.EqualTo(UIListDirection.Horizontal));
            Assert.That(UIPlan.FindElement(view, "icon").GetComponent<Image>().color, Is.EqualTo(Color.green),
                "沒有 updateExisting 的既有元件不會被改動");
            Assert.That(sprite, Is.Not.Null);
        }

        private static readonly string[] MisplacedElements =
        {
            @"{ ""id"": ""bad"", ""kind"": ""Text"", ""min"": 1 }|min",
            @"{ ""id"": ""bad"", ""kind"": ""Slider"", ""text"": ""x"" }|text",
            @"{ ""id"": ""bad"", ""kind"": ""Image"", ""action"": ""go"" }|action",
            @"{ ""id"": ""bad"", ""kind"": ""ProgressBar"", ""options"": [""a""] }|options",
            @"{ ""id"": ""bad"", ""kind"": ""Dropdown"", ""placeholder"": ""x"" }|placeholder",
            @"{ ""id"": ""bad"", ""kind"": ""InputField"", ""state"": ""On"" }|state",
            @"{ ""id"": ""bad"", ""kind"": ""ScrollList"", ""value"": 1 }|value",
            @"{ ""id"": ""bad"", ""kind"": ""Button"", ""spacing"": 2 }|spacing",
            @"{ ""id"": ""bad"", ""kind"": ""Toggle"", ""direction"": ""Vertical"" }|direction",
            @"{ ""id"": ""bad"", ""kind"": ""Panel"", ""numberMode"": ""Whole"" }|numberMode",
            @"{ ""id"": ""bad"", ""kind"": ""Slider"", ""textKey"": ""a.b"" }|textKey",
            @"{ ""id"": ""bad"", ""kind"": ""Text"", ""spriteKey"": ""a.b"" }|spriteKey",
            @"{ ""id"": ""bad"", ""kind"": ""Button"", ""optionKeys"": [""a.b""] }|optionKeys",
            @"{ ""id"": ""bad"", ""kind"": ""Toggle"", ""placeholderKey"": ""a.b"" }|placeholderKey"
        };

        [TestCaseSource(nameof(MisplacedElements))]
        public void PlanRejectsFieldsOnWidgetsThatDoNotSupportThem(string testCase)
        {
            RequireDOTween();
            var parts = testCase.Split('|');
            var root = WorkspaceWithHomePage("Batch3 Misplaced Field");
            var exception = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                $@"{{ ""views"": [{{ ""id"": ""home"", ""elements"": [{{ ""id"": ""ok"" }}, {parts[0]}] }},
                    {{ ""id"": ""other"" }}] }}"));
            Assert.That(exception.Message, Does.Contain("home").And.Contain("bad").And.Contain(parts[1]));
            AssertNothingWasApplied(root);
        }

        [Test]
        public void PlanRejectsUnknownKindsAndKindChangesBeforeChangingAnything()
        {
            RequireDOTween();
            var root = WorkspaceWithHomePage("Batch3 Unknown Kind");
            var unknown = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                @"{ ""views"": [{ ""id"": ""home"", ""elements"": [{ ""id"": ""ok"" },
                    { ""id"": ""bad"", ""kind"": ""Fancy"" }] }, { ""id"": ""other"" }] }"));
            Assert.That(unknown.Message, Does.Contain("home").And.Contain("bad").And.Contain("Fancy")
                .And.Contain("Button").And.Contain("ScrollList"));
            AssertNothingWasApplied(root);

            // 名稱區分大小寫。
            Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                @"{ ""views"": [{ ""id"": ""home"", ""elements"": [{ ""id"": ""bad"", ""kind"": ""button"" }] }] }"));
            AssertNothingWasApplied(root);

            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                { ""id"": ""label"", ""text"": ""A"" }] }] }");
            var view = UIAuthoring.FindEntry(root, "home").Review;
            var label = UIPlan.FindElement(view, "label").GetComponent<TMP_Text>();
            var changedKind = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                @"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                    { ""id"": ""label"", ""kind"": ""Button"", ""updateExisting"": true, ""text"": ""B"" }] },
                    { ""id"": ""other"" }] }"));
            Assert.That(changedKind.Message, Does.Contain("label").And.Contain("Text").And.Contain("Button"));
            Assert.That(label.text, Is.EqualTo("A"));
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);

            var manual = new GameObject("manual", typeof(RectTransform));
            manual.transform.SetParent(view.Content, false);
            SetElementId(manual, "manual");
            var handMade = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                @"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                    { ""id"": ""manual"", ""updateExisting"": true, ""text"": ""B"" }] }, { ""id"": ""other"" }] }"));
            Assert.That(handMade.Message, Does.Contain("manual").And.Contain("updateExisting"));
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);

            // 沒有 updateExisting 時照舊略過既有元件，不報錯。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [{ ""id"": ""manual"" }] }] }");
            Assert.That(manual.GetComponent<TMP_Text>(), Is.Null);
        }

        [Test]
        public void WidgetPrefabChangesReachInstancesThatDidNotSetTheField()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            SceneManager.SetActiveScene(NewScene("Batch3 Prefab Link"));
            UIWidgets.Install();
            // 「same」寫出的顏色與字級剛好和元件 Prefab 目前的值相同，仍然要算是有設定。
            var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{widgets}/Button.prefab");
            var prefabColor = "#" + ColorUtility.ToHtmlStringRGBA(buttonPrefab.GetComponent<Image>().color);
            var prefabFontSize = buttonPrefab.GetComponentInChildren<TMP_Text>().fontSize;
            var root = UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""home"", ""elements"": [
                {{ ""id"": ""plain"", ""kind"": ""Button"", ""text"": ""Plain"" }},
                {{ ""id"": ""tinted"", ""kind"": ""Button"", ""color"": ""#FF0000"" }},
                {{ ""id"": ""same"", ""kind"": ""Button"", ""color"": ""{prefabColor}"",
                   ""fontSize"": {prefabFontSize} }}
            ] }}] }}");
            var view = UIAuthoring.FindEntry(root, "home").Review;
            var plain = UIPlan.FindElement(view, "plain").GetComponent<Image>();
            var tinted = UIPlan.FindElement(view, "tinted").GetComponent<Image>();
            var same = UIPlan.FindElement(view, "same").GetComponent<Image>();
            var sameColor = same.color;
            Assert.That(tinted.color, Is.EqualTo(Color.red));
            Assert.That(sameColor, Is.EqualTo(buttonPrefab.GetComponent<Image>().color));

            // 之後再更新同一個元件的其他欄位，先前寫過的欄位仍然是覆寫。
            var spritePath = CreateSprite("Badge");
            UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""home"", ""elements"": [
                {{ ""id"": ""same"", ""kind"": ""Button"", ""updateExisting"": true,
                   ""spritePath"": ""{spritePath}"", ""text"": ""Same"" }}] }}] }}");
            Assert.That(same.sprite, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath)));

            EditPrefab($"{widgets}/Button.prefab", contents =>
            {
                contents.GetComponent<Image>().color = Color.green;
                contents.GetComponentInChildren<TMP_Text>().fontSize = 51;
            });
            Assert.That(plain.color, Is.EqualTo(Color.green));
            Assert.That(tinted.color, Is.EqualTo(Color.red));
            Assert.That(same.color, Is.EqualTo(sameColor));
            Assert.That(plain.GetComponentInChildren<TMP_Text>().fontSize, Is.EqualTo(51));
            Assert.That(plain.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Plain"));
            Assert.That(tinted.GetComponentInChildren<TMP_Text>().fontSize, Is.EqualTo(51));
            Assert.That(same.GetComponentInChildren<TMP_Text>().fontSize, Is.EqualTo(prefabFontSize));
            Assert.That(same.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Same"));
        }

        private string CreateSprite(string name)
        {
            var path = $"{folder}/{name}.png";
            var texture = new Texture2D(4, 4);
            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
            return path;
        }

        [TestCase(UIAuthoringMode.SingleScene)]
        [TestCase(UIAuthoringMode.Prefab)]
        public void PrefabViewsKeepTheirElementsLinkedToTheWidgetPrefabs(UIAuthoringMode mode)
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch3 Nested " + mode));
            var pagePrefab = mode == UIAuthoringMode.Prefab ? $@", ""prefabPath"": ""{folder}/Page.prefab""" : "";
            var root = UIPlan.Apply($@"{{
                ""mode"": ""{mode}"",
                ""views"": [
                    {{ ""id"": ""home""{pagePrefab}, ""elements"": [
                        {{ ""id"": ""open"", ""kind"": ""Button"", ""text"": ""Open"", ""action"": ""open"" }}] }},
                    {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Popup.prefab"",
                       ""elements"": [
                        {{ ""id"": ""message"", ""text"": ""Hi"" }},
                        {{ ""id"": ""close"", ""kind"": ""Button"", ""text"": ""Close"", ""action"": ""close"" }},
                        {{ ""id"": ""amount"", ""kind"": ""Slider"", ""value"": 0.2 }}] }}
                ],
                ""routes"": [
                    {{ ""source"": ""home"", ""action"": ""open"", ""kind"": ""OpenPopup"", ""target"": ""popup"" }},
                    {{ ""source"": ""popup"", ""action"": ""close"", ""kind"": ""ClosePopup"" }}
                ]
            }}");
            var set = UIWidgets.Find();
            var views = mode == UIAuthoringMode.Prefab ? new[] { "home", "popup" } : new[] { "popup" };
            foreach (var id in views)
            {
                var entry = UIAuthoring.FindEntry(root, id);
                Assert.That(UIAuthoring.HasReviewOverrides(entry), Is.False, id);
                foreach (var element in entry.Prefab.GetComponentsInChildren<UIElementId>(true))
                {
                    Assert.That(PrefabUtility.IsAnyPrefabInstanceRoot(element.gameObject), Is.True, element.Id);
                    Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(element.gameObject),
                        Is.SameAs(set.Find(element.Kind).Prefab), element.Id);
                }
            }
            var popup = UIAuthoring.FindEntry(root, "popup").Prefab;
            Assert.That(popup.GetComponentsInChildren<UIElementId>(true).Select(element => element.Id),
                Is.EquivalentTo(new[] { "message", "close", "amount" }));
            Assert.That(popup.GetComponentInChildren<Slider>(true).value, Is.EqualTo(0.2f));
            Assert.That(popup.GetComponentInChildren<UIButtonAction>(true).Action, Is.EqualTo("close"));
            Assert.That(FunctionalErrors(root), Is.Empty);
        }

        [Test]
        public void CustomWidgetsUseTheFieldsOfTheKindTheyAreRegisteredLike()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            var primary = $"{folder}/PrimaryButton.prefab";
            Assert.That(AssetDatabase.CopyAsset($"{widgets}/Button.prefab", primary), Is.True);
            EditPrefab(primary, contents => contents.GetComponent<Image>().color = Color.yellow);
            var badge = $"{folder}/Badge.prefab";
            SavePrefab(badge, new GameObject("Badge", typeof(RectTransform), typeof(Image)));

            SceneManager.SetActiveScene(NewScene("Batch3 Custom Widgets"));
            var widgetList = $@"""widgets"": [
                {{ ""name"": ""PrimaryButton"", ""prefabPath"": ""{primary}"", ""like"": ""Button"" }},
                {{ ""name"": ""Badge"", ""prefabPath"": ""{badge}"" }}]";
            var root = UIPlan.Apply($@"{{ {widgetList}, ""views"": [{{ ""id"": ""home"", ""elements"": [
                {{ ""id"": ""confirm"", ""kind"": ""PrimaryButton"", ""text"": ""OK"", ""action"": ""ok"" }},
                {{ ""id"": ""badge"", ""kind"": ""Badge"",
                   ""rect"": {{ ""position"": {{ ""x"": 40, ""y"": 50 }}, ""size"": {{ ""x"": 32, ""y"": 32 }} }} }}
            ] }}], ""routes"": [{{ ""source"": ""home"", ""action"": ""ok"", ""kind"": ""Emit"" }}] }}");
            var set = UIWidgets.Find();
            Assert.That(set.Find("PrimaryButton").Kind, Is.EqualTo(UIWidgetKind.Button));
            Assert.That(set.Find("Badge").Kind, Is.EqualTo(UIWidgetKind.Custom));
            var view = UIAuthoring.FindEntry(root, "home").Review;
            var confirm = UIPlan.FindElement(view, "confirm");
            Assert.That(confirm.GetComponent<UIElementId>().Kind, Is.EqualTo("PrimaryButton"));
            Assert.That(confirm.GetComponent<Image>().color, Is.EqualTo(Color.yellow));
            Assert.That(confirm.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("OK"));
            Assert.That(confirm.GetComponent<UIButtonAction>().Action, Is.EqualTo("ok"));
            var badgeRect = (RectTransform)UIPlan.FindElement(view, "badge").transform;
            Assert.That(badgeRect.anchoredPosition, Is.EqualTo(new Vector2(40, 50)));
            Assert.That(badgeRect.sizeDelta, Is.EqualTo(new Vector2(32, 32)));

            // 內容相同的登記可以重跑；Custom 類別只接受 rect。
            var customField = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ {widgetList},
                ""views"": [{{ ""id"": ""home"", ""elements"": [
                    {{ ""id"": ""second"", ""kind"": ""Badge"", ""color"": ""#FF0000"" }}] }}] }}"));
            Assert.That(customField.Message, Does.Contain("second").And.Contain("Custom").And.Contain("color"));
            Assert.That(UIPlan.FindElement(view, "second"), Is.Null);
            UIWidgets.Register("PrimaryButton", primary, UIWidgetKind.Button);
            Assert.That(set.Widgets.Count, Is.EqualTo(StarterKinds.Length + 2));

            // 內容不同的登記會報錯而且不覆寫；比照的類別需要對應的元件。
            var different = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{
                ""widgets"": [{{ ""name"": ""PrimaryButton"", ""prefabPath"": ""{badge}"", ""like"": ""Image"" }}],
                ""views"": [{{ ""id"": ""other"" }}] }}"));
            Assert.That(different.Message, Does.Contain("PrimaryButton").And.Contain("不會覆寫"));
            Assert.That(set.Find("PrimaryButton").Prefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(primary)));
            var missing = Assert.Throws<ArgumentException>(() =>
                UIWidgets.Register("Meter", badge, UIWidgetKind.Slider));
            Assert.That(missing.Message, Does.Contain("Meter").And.Contain("Slider"));
            Assert.Throws<ArgumentException>(() => UIWidgets.Register("Button", badge, UIWidgetKind.Image));
            Assert.Throws<ArgumentException>(() => UIWidgets.Register("Ghost", $"{folder}/Missing.prefab"));
            Assert.That(set.Find("Meter"), Is.Null);
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);

            // 計畫後面的步驟失敗時，這次登記的元件也要還原。
            var extra = $"{folder}/Extra.prefab";
            SavePrefab(extra, new GameObject("Extra", typeof(RectTransform)));
            Assert.Throws<InvalidOperationException>(() => UIPlan.Apply($@"{{
                ""widgets"": [{{ ""name"": ""Extra"", ""prefabPath"": ""{extra}"" }}],
                ""views"": [{{ ""id"": ""home"", ""elements"": [
                    {{ ""id"": ""broken"", ""kind"": ""Button"", ""action"": ""noRoute"" }}] }}] }}"));
            Assert.That(set.Find("Extra"), Is.Null);
            Assert.That(UIPlan.FindElement(view, "broken"), Is.Null);
        }

        [Test]
        public void DefaultFontReplacesOnlyTheTextThatUsedThePreviousDefault()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            var set = UIWidgets.Install();
            var tmpDefault = TMP_Settings.defaultFontAsset;
            var mainPath = CopyFont(tmpDefault, "Main SDF");
            var customPath = CopyFont(tmpDefault, "Custom SDF");
            var main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(mainPath);
            var custom = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(customPath);
            EditPrefab($"{widgets}/Toggle.prefab", contents => contents.GetComponentInChildren<TMP_Text>().font = custom);

            UIWidgets.SetDefaultFont(mainPath);
            Assert.That(set.DefaultFont, Is.SameAs(main));
            Assert.That(FontOf(set, "Text"), Is.SameAs(main));
            Assert.That(FontOf(set, "Button"), Is.SameAs(main));
            Assert.That(FontOf(set, "Toggle"), Is.SameAs(custom));
            Assert.That(set.Find("InputField").Prefab.GetComponent<TMP_InputField>().fontAsset, Is.SameAs(main));
            Assert.That(set.Find("Dropdown").Prefab.GetComponentsInChildren<TMP_Text>(true)
                .All(text => text.font == main), Is.True);
            Assert.That(set.ListItem.GetComponentInChildren<TMP_Text>().font, Is.SameAs(main));

            // 之後補上的起始元件直接使用目前的預設字型。
            Assert.That(AssetDatabase.DeleteAsset($"{widgets}/Dropdown.prefab"), Is.True);
            UIWidgets.Install();
            Assert.That(set.Find("Dropdown").Prefab.GetComponentsInChildren<TMP_Text>(true)
                .All(text => text.font == main), Is.True);

            // 計畫的 defaultFontPath 走同一條路；空字串改回 TMP 的預設字型。
            SceneManager.SetActiveScene(NewScene("Batch3 Default Font"));
            // 換 Scene 時 Unity 會卸載沒有被引用的資產，先前拿到的元件集物件可能已經失效。
            set = UIWidgets.Find();
            UIPlan.Apply($@"{{ ""defaultFontPath"": ""{customPath}"", ""views"": [{{ ""id"": ""home"",
                ""elements"": [{{ ""id"": ""title"", ""text"": ""A"" }},
                               {{ ""id"": ""pinned"", ""text"": ""B"", ""fontPath"": ""{mainPath}"" }}] }}] }}");
            Assert.That(set.DefaultFont, Is.SameAs(custom));
            var view = UIAuthoring.FindEntry(UIAuthoring.FindWorkspace(SceneManager.GetActiveScene()), "home").Review;
            Assert.That(UIPlan.FindElement(view, "title").GetComponent<TMP_Text>().font, Is.SameAs(custom));
            Assert.That(UIPlan.FindElement(view, "pinned").GetComponent<TMP_Text>().font, Is.SameAs(main));
            Assert.That(FontOf(set, "Button"), Is.SameAs(custom));

            UIWidgets.SetDefaultFont("");
            Assert.That(set.DefaultFont, Is.Null);
            Assert.That(FontOf(set, "Button"), Is.SameAs(tmpDefault));
            Assert.That(UIPlan.FindElement(view, "title").GetComponent<TMP_Text>().font, Is.SameAs(tmpDefault));
            Assert.That(UIPlan.FindElement(view, "pinned").GetComponent<TMP_Text>().font, Is.SameAs(main));
            Assert.Throws<ArgumentException>(() => UIWidgets.SetDefaultFont($"{widgets}/Button.prefab"));
        }

        [Test]
        public void ListItemsKeepTheirIdsOutOfTheViewAndThePlanCanReplaceTheItemPrefab()
        {
            RequireDOTween();
            var scene = NewScene("Batch3 List Items");
            SceneManager.SetActiveScene(scene);
            var set = UIWidgets.Install();
            var rowPath = $"{folder}/Row.prefab";
            Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(set.ListItem), rowPath), Is.True);

            var root = UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""shop"", ""elements"": [
                {{ ""id"": ""goods"", ""kind"": ""ScrollList"", ""itemPrefabPath"": ""{rowPath}"" }},
                {{ ""id"": ""stock"", ""kind"": ""ScrollList"" }},
                {{ ""id"": ""title"", ""text"": ""Shop"" }}
            ] }}] }}");
            set = UIWidgets.Find();
            var view = UIAuthoring.FindEntry(root, "shop").Review;
            var goods = UIPlan.FindElement(view, "goods").GetComponent<UIList>();
            Assert.That(goods.ItemPrefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(rowPath)));
            Assert.That(UIPlan.FindElement(view, "stock").GetComponent<UIList>().ItemPrefab, Is.SameAs(set.ListItem));

            // 手動放進清單內容的項目帶有自己的元件 ID，不算在 View 的元件裡。
            var item = (GameObject)PrefabUtility.InstantiatePrefab(set.ListItem, goods.Content);
            Assert.That(item.GetComponentInChildren<UIElementId>().Id, Is.EqualTo("label"));
            Assert.That(UIPlan.FindElement(view, "label"), Is.Null);
            Assert.That(UIPlan.ElementsOf(view).Select(element => element.Id),
                Is.EquivalentTo(new[] { "goods", "stock", "title" }));
            Assert.That(FunctionalErrors(root), Is.Empty);
            var constants = File.ReadAllText(UIIdConstants.Generate(root));
            Assert.That(constants, Does.Contain("public const string Goods = \"goods\";")
                .And.Not.Contain("\"label\""));

            // 清單外面可以有同名的元件，兩者互不相干。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""shop"", ""elements"": [
                { ""id"": ""label"", ""text"": ""Outside"" }] }] }");
            Assert.That(UIPlan.FindElement(view, "label").transform.parent, Is.EqualTo(view.Content));
            Assert.That(FunctionalErrors(root), Is.Empty);
        }

        [Test]
        public void PlanWritesLocalizationKeysNextToTheDefaultContentAndUpdatesThemPartially()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch3 Keys"));
            var root = UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""home"", ""elements"": [
                    {{ ""id"": ""title"", ""text"": ""Start game"", ""textKey"": ""menu.start"" }},
                    {{ ""id"": ""logo"", ""kind"": ""Image"", ""spriteKey"": ""logo.main"" }},
                    {{ ""id"": ""frame"", ""kind"": ""Panel"", ""spriteKey"": ""frame.main"" }},
                    {{ ""id"": ""play"", ""kind"": ""Button"", ""text"": ""Play"", ""textKey"": ""menu.play"" }},
                    {{ ""id"": ""mute"", ""kind"": ""Toggle"", ""textKey"": ""settings.mute"" }},
                    {{ ""id"": ""quality"", ""kind"": ""Dropdown"", ""options"": [""Low"", ""High""],
                       ""optionKeys"": [""quality.low"", ""quality.high""] }},
                    {{ ""id"": ""name"", ""kind"": ""InputField"", ""placeholder"": ""Name"",
                       ""placeholderKey"": ""profile.name"" }},
                    {{ ""id"": ""plain"", ""text"": ""Plain"" }}
                ] }},
                {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Keys.prefab"", ""elements"": [
                    {{ ""id"": ""message"", ""text"": ""Saved"", ""textKey"": ""popup.saved"" }}] }}
            ] }}");
            var view = UIAuthoring.FindEntry(root, "home").Review;
            var title = UIPlan.FindElement(view, "title");
            Assert.That(title.GetComponent<UILocalizedText>().Key, Is.EqualTo("menu.start"));
            Assert.That(title.GetComponent<TMP_Text>().text, Is.EqualTo("Start game"), "預設內容照常保留");
            Assert.That(UIPlan.FindElement(view, "logo").GetComponent<UILocalizedImage>().Key, Is.EqualTo("logo.main"));
            Assert.That(UIPlan.FindElement(view, "frame").GetComponent<UILocalizedImage>().Key,
                Is.EqualTo("frame.main"));
            var play = UIPlan.FindElement(view, "play").GetComponentInChildren<TMP_Text>();
            Assert.That(play.GetComponent<UILocalizedText>().Key, Is.EqualTo("menu.play"));
            Assert.That(play.text, Is.EqualTo("Play"));
            var mute = UIPlan.FindElement(view, "mute").GetComponentInChildren<UILocalizedText>();
            Assert.That(mute.Key, Is.EqualTo("settings.mute"));
            var quality = UIPlan.FindElement(view, "quality");
            Assert.That(quality.GetComponent<UILocalizedDropdown>().Keys,
                Is.EqualTo(new[] { "quality.low", "quality.high" }));
            Assert.That(quality.GetComponent<TMP_Dropdown>().options.Select(option => option.text),
                Is.EqualTo(new[] { "Low", "High" }));
            var input = UIPlan.FindElement(view, "name").GetComponent<TMP_InputField>();
            Assert.That(input.placeholder.GetComponent<UILocalizedText>().Key, Is.EqualTo("profile.name"));
            Assert.That(((TMP_Text)input.placeholder).text, Is.EqualTo("Name"));
            Assert.That(input.textComponent.GetComponent<UILocalizedText>(), Is.Null);
            Assert.That(UIPlan.FindElement(view, "plain").GetComponent<UILocalizedText>().Key, Is.Null.Or.Empty);

            // 鍵值跟著套用到 Popup 的 Prefab 本體。
            var popup = UIAuthoring.FindEntry(root, "popup");
            Assert.That(popup.Prefab.GetComponentInChildren<UILocalizedText>(true).Key, Is.EqualTo("popup.saved"));
            Assert.That(UIAuthoring.HasReviewOverrides(popup), Is.False);

            // 部分更新：只改鍵值不動文字，只改文字不動鍵值；空字串清除鍵值。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                { ""id"": ""title"", ""updateExisting"": true, ""text"": ""New game"" },
                { ""id"": ""play"", ""kind"": ""Button"", ""updateExisting"": true, ""textKey"": ""menu.go"" },
                { ""id"": ""mute"", ""kind"": ""Toggle"", ""updateExisting"": true, ""textKey"": """" },
                { ""id"": ""quality"", ""kind"": ""Dropdown"", ""updateExisting"": true,
                  ""optionKeys"": ["""", ""quality.max""] },
                { ""id"": ""logo"", ""kind"": ""Image"", ""updateExisting"": true, ""color"": ""#FF0000"" }
            ] }] }");
            Assert.That(title.GetComponent<TMP_Text>().text, Is.EqualTo("New game"));
            Assert.That(title.GetComponent<UILocalizedText>().Key, Is.EqualTo("menu.start"));
            Assert.That(play.GetComponent<UILocalizedText>().Key, Is.EqualTo("menu.go"));
            Assert.That(play.text, Is.EqualTo("Play"));
            Assert.That(mute.Key, Is.Empty);
            Assert.That(quality.GetComponent<UILocalizedDropdown>().Keys, Is.EqualTo(new[] { "", "quality.max" }));
            Assert.That(quality.GetComponent<TMP_Dropdown>().options.Select(option => option.text),
                Is.EqualTo(new[] { "Low", "High" }));
            Assert.That(UIPlan.FindElement(view, "logo").GetComponent<UILocalizedImage>().Key, Is.EqualTo("logo.main"));

            // 鍵值的數量要和選項一樣多，不管選項是這次給的還是原本就有的。
            var withOptions = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"",
                ""elements"": [{ ""id"": ""extra"", ""kind"": ""Dropdown"", ""options"": [""A"", ""B""],
                                 ""optionKeys"": [""only.one""] }] }] }"));
            Assert.That(withOptions.Message, Does.Contain("extra").And.Contain("optionKeys"));
            var existingOptions = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [{
                ""id"": ""home"", ""elements"": [{ ""id"": ""quality"", ""kind"": ""Dropdown"",
                ""updateExisting"": true, ""optionKeys"": [""a"", ""b"", ""c""] }] }] }"));
            Assert.That(existingOptions.Message, Does.Contain("quality").And.Contain("optionKeys"));
            Assert.That(quality.GetComponent<UILocalizedDropdown>().Keys, Is.EqualTo(new[] { "", "quality.max" }));
            Assert.That(UIPlan.FindElement(view, "extra"), Is.Null);
        }

        [Test]
        public void PlanSetsTheDefaultSelectedElementAndRejectsOnesThatCannotBeSelected()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch3 Default Selected"));
            var root = UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""home"", ""defaultSelected"": ""start"", ""elements"": [
                    {{ ""id"": ""title"", ""text"": ""Menu"" }},
                    {{ ""id"": ""start"", ""kind"": ""Button"", ""text"": ""Start"" }},
                    {{ ""id"": ""volume"", ""kind"": ""Slider"" }}] }},
                {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Selected.prefab"",
                   ""defaultSelected"": ""no"", ""elements"": [
                    {{ ""id"": ""yes"", ""kind"": ""Button"" }},
                    {{ ""id"": ""no"", ""kind"": ""Button"" }}] }}
            ] }}");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            Assert.That(home.DefaultSelected, Is.SameAs(UIPlan.FindElement(home, "start").GetComponent<Button>()));
            var popup = UIAuthoring.FindEntry(root, "popup");
            Assert.That(popup.Prefab.DefaultSelected, Is.Not.Null);
            Assert.That(popup.Prefab.DefaultSelected.GetComponent<UIElementId>().Id, Is.EqualTo("no"));
            Assert.That(popup.Prefab.DefaultSelected.transform.IsChildOf(popup.Prefab.transform), Is.True);
            Assert.That(UIAuthoring.HasReviewOverrides(popup), Is.False);

            // 和背景、動畫一樣，既有的 View 要有 updateExisting 才會改。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""defaultSelected"": ""volume"" }] }");
            Assert.That(home.DefaultSelected.GetComponent<UIElementId>().Id, Is.EqualTo("start"));
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""updateExisting"": true,
                ""defaultSelected"": ""volume"" }] }");
            Assert.That(home.DefaultSelected, Is.SameAs(UIPlan.FindElement(home, "volume").GetComponent<Slider>()));
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""updateExisting"": true, ""background"": ""#000000"" }] }");
            Assert.That(home.DefaultSelected, Is.Not.Null, "沒寫的選項維持原狀");
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""updateExisting"": true, ""defaultSelected"": """" }] }");
            Assert.That(home.DefaultSelected, Is.Null);

            var missing = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""home"", ""updateExisting"": true, ""defaultSelected"": ""ghost"" }, { ""id"": ""other"" }] }"));
            Assert.That(missing.Message, Does.Contain("home").And.Contain("ghost").And.Contain("defaultSelected"));
            var notSelectable = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""home"", ""updateExisting"": true, ""defaultSelected"": ""title"" }, { ""id"": ""other"" }] }"));
            Assert.That(notSelectable.Message, Does.Contain("title").And.Contain("Selectable"));
            foreach (var kind in new[] { "Text", "Image", "Panel", "ProgressBar", "ScrollList" })
            {
                var planned = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ ""views"": [
                    {{ ""id"": ""other"", ""defaultSelected"": ""target"", ""elements"": [
                        {{ ""id"": ""target"", ""kind"": ""{kind}"" }}] }}] }}"), kind);
                Assert.That(planned.Message, Does.Contain("other").And.Contain("target"), kind);
            }
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);
            Assert.That(home.DefaultSelected, Is.Null);

            foreach (var kind in new[] { "Button", "Toggle", "Slider", "Dropdown", "InputField" })
            {
                UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""view{kind}"", ""defaultSelected"": ""target"",
                    ""elements"": [{{ ""id"": ""target"", ""kind"": ""{kind}"" }}] }}] }}");
                Assert.That(UIAuthoring.FindEntry(root, "view" + kind).Review.DefaultSelected, Is.Not.Null, kind);
            }
        }

        [Test]
        public void PlanSetsWhetherAViewAnswersTheBackKey()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch3 Back Key"));
            var root = UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""home"" }},
                {{ ""id"": ""loading"", ""backKey"": ""Ignore"" }},
                {{ ""id"": ""tutorial"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Tutorial.prefab"",
                   ""backKey"": ""Ignore"" }}
            ] }}");
            Assert.That(UIAuthoring.FindEntry(root, "home").Review.BlocksBackKey, Is.False);
            var loading = UIAuthoring.FindEntry(root, "loading").Review;
            Assert.That(loading.BlocksBackKey, Is.True);
            var tutorial = UIAuthoring.FindEntry(root, "tutorial");
            Assert.That(tutorial.Prefab.BlocksBackKey, Is.True);
            Assert.That(UIAuthoring.HasReviewOverrides(tutorial), Is.False);

            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""loading"", ""backKey"": ""Back"" }] }");
            Assert.That(loading.BlocksBackKey, Is.True, "既有的 View 要有 updateExisting 才會改");
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""loading"", ""updateExisting"": true, ""background"": ""#000000"" }] }");
            Assert.That(loading.BlocksBackKey, Is.True, "沒寫的選項維持原狀");
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""loading"", ""updateExisting"": true, ""backKey"": ""Back"" }] }");
            Assert.That(loading.BlocksBackKey, Is.False);

            var invalid = Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""home"", ""updateExisting"": true, ""backKey"": ""Never"" }, { ""id"": ""other"" }] }"));
            Assert.That(invalid.Message, Does.Contain("Never"));
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);
        }

        [Test]
        public void PlanSetsPopupReuseAndOverlayClickAndRejectsThemOnPages()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Batch3 Popup Options"));
            var root = UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""home"" }},
                {{ ""id"": ""quit"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Quit.prefab"",
                   ""popupCache"": ""Reuse"", ""overlayClick"": ""Close"" }},
                {{ ""id"": ""tip"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Tip.prefab"" }}
            ] }}");
            var quit = UIAuthoring.FindEntry(root, "quit");
            Assert.That(quit.Prefab.ReusesInstance, Is.True);
            Assert.That(quit.Prefab.ClosesOnOverlayClick, Is.True);
            Assert.That(quit.Prefab.Overlay, Is.Not.Null);
            Assert.That(quit.Prefab.Overlay.parent, Is.EqualTo(quit.Prefab.transform));
            Assert.That(quit.Prefab.Overlay.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(UIAuthoring.HasReviewOverrides(quit), Is.False);
            var tip = UIAuthoring.FindEntry(root, "tip");
            Assert.That(tip.Prefab.ReusesInstance, Is.False);
            Assert.That(tip.Prefab.ClosesOnOverlayClick, Is.False);
            Assert.That(tip.Prefab.Overlay, Is.Not.Null);
            Assert.That(UIAuthoring.FindEntry(root, "home").Review.Overlay, Is.Null);

            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""quit"", ""kind"": ""Popup"", ""popupCache"": ""Recreate"" }] }");
            Assert.That(quit.Review.ReusesInstance, Is.True, "既有的 View 要有 updateExisting 才會改");
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""quit"", ""kind"": ""Popup"", ""updateExisting"": true,
                ""popupCache"": ""Recreate"" }] }");
            Assert.That(quit.Review.ReusesInstance, Is.False);
            Assert.That(quit.Review.ClosesOnOverlayClick, Is.True, "沒寫的選項維持原狀");
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""quit"", ""updateExisting"": true, ""overlayClick"": ""Ignore"" }] }");
            Assert.That(quit.Review.ClosesOnOverlayClick, Is.False);

            foreach (var field in new[] { "popupCache\": \"Reuse", "overlayClick\": \"Close" })
            {
                var name = field.Split('"')[0];
                var onPage = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                    "{ \"views\": [{ \"id\": \"home\", \"updateExisting\": true, \"" + field +
                    "\" }, { \"id\": \"other\" }] }"), name);
                Assert.That(onPage.Message, Does.Contain("home").And.Contain(name).And.Contain("Popup"));
                var onNewPage = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                    "{ \"views\": [{ \"id\": \"other\", \"" + field + "\" }] }"), name);
                Assert.That(onNewPage.Message, Does.Contain("other").And.Contain(name));
            }
            Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""tip"", ""updateExisting"": true, ""popupCache"": ""Sometimes"" }, { ""id"": ""other"" }] }"));
            Assert.Throws<ArgumentException>(() => UIPlan.Apply(@"{ ""views"": [
                { ""id"": ""tip"", ""updateExisting"": true, ""overlayClick"": ""Maybe"" }, { ""id"": ""other"" }] }"));
            Assert.That(UIAuthoring.FindEntry(root, "other"), Is.Null);
            Assert.That(tip.Review.ReusesInstance || tip.Review.ClosesOnOverlayClick, Is.False);
        }

        [Test]
        public void StarterConfirmIsAPopupPrefabBuiltFromWidgetsAndRegistersLikeAnyPopup()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            var set = UIWidgets.Install();
            var confirm = set.Confirm;
            Assert.That(confirm.Id, Is.EqualTo(UIRoot.DefaultConfirmPopupId));
            Assert.That(confirm.GetComponent<UIConfirmPopup>(), Is.Not.Null);
            Assert.That(confirm.Overlay && confirm.Motion && confirm.Content && confirm.Group, Is.True);
            Assert.That(confirm.GetComponent<Canvas>() && confirm.GetComponent<GraphicRaycaster>(), Is.True);
            Assert.That((confirm.ReusesInstance, confirm.BlocksBackKey, confirm.ClosesOnOverlayClick),
                Is.EqualTo((true, false, false)));
            var elements = confirm.GetComponentsInChildren<UIElementId>(true).ToDictionary(element => element.Id);
            Assert.That(elements.Keys, Is.EquivalentTo(new[] { "title", "message", "confirm", "cancel" }));
            foreach (var (id, kind) in new[]
                { ("title", "Text"), ("message", "Text"), ("confirm", "Button"), ("cancel", "Button") })
            {
                Assert.That(elements[id].Kind, Is.EqualTo(kind), id);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(elements[id].gameObject),
                    Is.SameAs(set.Find(kind).Prefab), id);
            }
            Assert.That(confirm.DefaultSelected, Is.SameAs(elements["cancel"].GetComponent<Button>()));
            var confirmLabel = elements["confirm"].GetComponentInChildren<TMP_Text>(true);
            var cancelLabel = elements["cancel"].GetComponentInChildren<TMP_Text>(true);
            Assert.That((confirmLabel.text, confirmLabel.GetComponent<UILocalizedText>().Key),
                Is.EqualTo(("確定", "ui.confirm")));
            Assert.That((cancelLabel.text, cancelLabel.GetComponent<UILocalizedText>().Key),
                Is.EqualTo(("取消", "ui.cancel")));
            Assert.That(UIRoot.StarterConfirmPrefabPath, Is.EqualTo(UIWidgets.DefaultFolder + "/Confirm.prefab"));

            // 改按鈕元件的 Prefab，確認框裡的按鈕跟著變。
            EditPrefab($"{widgets}/Button.prefab", contents => contents.GetComponent<Image>().color = Color.magenta);
            Assert.That(elements["confirm"].GetComponent<Image>().color, Is.EqualTo(Color.magenta));

            // 和其他 Popup 一樣寫在計畫裡，指向起始 Prefab；多個 Scene 可以登記同一個。
            var plan = $@"{{ ""views"": [
                {{ ""id"": ""home"", ""elements"": [
                    {{ ""id"": ""quit"", ""kind"": ""Button"", ""text"": ""Quit"", ""action"": ""quit"" }}] }},
                {{ ""id"": ""confirm"", ""kind"": ""Popup"", ""prefabPath"": ""{UIWidgets.ConfirmPrefabPath}"" }}
            ], ""routes"": [{{ ""source"": ""home"", ""action"": ""quit"", ""kind"": ""Emit"" }}] }}";
            var firstScene = NewScene("Batch3 Confirm A");
            SceneManager.SetActiveScene(firstScene);
            var first = UIPlan.Apply(plan);
            var entry = UIAuthoring.FindEntry(first, "confirm");
            Assert.That(entry.Kind, Is.EqualTo(UIEntryKind.Popup));
            Assert.That(AssetDatabase.GetAssetPath(entry.Prefab), Is.EqualTo(UIWidgets.ConfirmPrefabPath));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(entry.Review.gameObject),
                Is.SameAs(entry.Prefab.gameObject));
            Assert.That(entry.Review.ReusesInstance, Is.True);
            Assert.That(UIAuthoring.HasReviewOverrides(entry), Is.False);
            Assert.That(FunctionalErrors(first), Is.Empty);
            Assert.That(File.ReadAllText(UIIdConstants.Generate(first)),
                Does.Contain("public static class ConfirmView").And.Contain("public const string Cancel = \"cancel\";")
                    .And.Contain("public const string Message = \"message\";"));

            Assert.That(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(firstScene), Is.True);
            var secondScene = NewScene("Batch3 Confirm B");
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(firstScene, true);
            SceneManager.SetActiveScene(secondScene);
            var second = UIPlan.Apply(plan);
            Assert.That(UIAuthoring.FindEntry(second, "confirm").Prefab, Is.SameAs(UIWidgets.Find().Confirm));
            Assert.That(FunctionalErrors(second), Is.Empty);

            // View Prefab 的畫面 ID 是固定的，不能用別的 ID 登記同一個檔案。
            var otherId = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""ask"", ""kind"": ""Popup"", ""prefabPath"": ""{UIWidgets.ConfirmPrefabPath}"" }}] }}"));
            Assert.That(otherId.Message, Does.Contain("confirm").And.Contain("ask").And.Contain("畫面 ID"));
            Assert.That(UIAuthoring.FindEntry(second, "ask"), Is.Null);
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                UIAuthoring.CreatePopup(second, "ask", "Ask", UIWidgets.ConfirmPrefabPath)).Message,
                Does.Contain("畫面 ID"));

            // 要有第二種樣式的確認框：建立一個新的 Popup，放進四個同名的元件，根節點掛上 UIConfirmPopup。
            var styled = UIPlan.Apply($@"{{ ""views"": [{{ ""id"": ""quitConfirm"", ""kind"": ""Popup"",
                ""prefabPath"": ""{folder}/QuitConfirm.prefab"", ""defaultSelected"": ""cancel"",
                ""popupCache"": ""Reuse"", ""elements"": [
                    {{ ""id"": ""title"", ""text"": ""Title"" }},
                    {{ ""id"": ""message"", ""text"": ""Message"" }},
                    {{ ""id"": ""confirm"", ""kind"": ""Button"", ""text"": ""Quit"" }},
                    {{ ""id"": ""cancel"", ""kind"": ""Button"", ""text"": ""Stay"" }}] }}] }}");
            var styledEntry = UIAuthoring.FindEntry(styled, "quitConfirm");
            Undo.AddComponent<UIConfirmPopup>(styledEntry.Review.gameObject);
            UIAuthoring.ApplyReviewOverrides(styled, "quitConfirm");
            Assert.That(styledEntry.Prefab.GetComponent<UIConfirmPopup>(), Is.Not.Null);
            Assert.That(FunctionalErrors(styled), Is.Empty);
        }

        [Test]
        public void ValidationReportsBrokenWidgetSetsAndViewOptions()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            var scene = NewScene("Batch3 Validation");
            SceneManager.SetActiveScene(scene);
            var root = UIPlan.Apply($@"{{ ""views"": [
                {{ ""id"": ""home"", ""elements"": [
                    {{ ""id"": ""start"", ""kind"": ""Button"", ""text"": ""Start"" }},
                    {{ ""id"": ""volume"", ""kind"": ""Slider"" }}] }},
                {{ ""id"": ""next"", ""elements"": [{{ ""id"": ""back"", ""kind"": ""Button"" }}] }},
                {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Validation.prefab"" }}
            ] }}");
            Assert.That(FunctionalErrors(root), Is.Empty);
            var home = UIAuthoring.FindEntry(root, "home").Review;
            var next = UIAuthoring.FindEntry(root, "next").Review;
            var popup = UIAuthoring.FindEntry(root, "popup").Review;

            // 預設選取元件不在這個畫面裡。
            home.defaultSelected = UIPlan.FindElement(next, "back").GetComponent<Button>();
            Assert.That(SingleError(root), Does.Contain("home").And.Contain("back").And.Contain("不在這個畫面裡"));
            home.defaultSelected = UIPlan.FindElement(home, "start").GetComponent<Button>();
            Assert.That(FunctionalErrors(root), Is.Empty);

            // 頁面開了只對 Popup 有意義的選項。
            home.reusesInstance = true;
            Assert.That(SingleError(root), Does.Contain("home").And.Contain("Popup"));
            home.reusesInstance = false;
            next.closesOnOverlayClick = true;
            Assert.That(SingleError(root), Does.Contain("next").And.Contain("Popup"));
            next.closesOnOverlayClick = false;
            popup.reusesInstance = true;
            popup.closesOnOverlayClick = true;
            Assert.That(FunctionalErrors(root), Is.Empty, "Popup 可以開啟這兩個選項");
            var overlay = popup.overlay;
            popup.overlay = null;
            Assert.That(SingleError(root), Does.Contain("popup").And.Contain("遮罩"));
            popup.overlay = overlay;

            // 元件記錄的名稱不在元件集裡。
            var start = UIPlan.FindElement(home, "start").GetComponent<UIElementId>();
            start.kind = "Vanished";
            Assert.That(SingleError(root), Does.Contain("home/start").And.Contain("Vanished").And.Contain("Button"));
            start.kind = "Button";

            // 元件 Prefab 缺少行為類別需要的元件。
            EditPrefab($"{widgets}/Slider.prefab", contents => Object.DestroyImmediate(contents.GetComponent<Slider>()));
            Assert.That(SingleError(root), Does.Contain("Slider").And.Contain("缺少"));
            Assert.That(AssetDatabase.DeleteAsset($"{widgets}/Slider.prefab"), Is.True);
            Assert.That(SingleError(root), Does.Contain("Slider").And.Contain("沒有 Prefab"));
            UIWidgets.Install();
            Assert.That(FunctionalErrors(root).Select(issue => issue.Message).Where(message => message.Contains("元件集")),
                Is.Empty);

            // 元件集超過一個，或不存在。
            var setPath = AssetDatabase.GetAssetPath(UIWidgets.Find());
            var copyPath = $"{widgets}/Second.asset";
            Assert.That(AssetDatabase.CopyAsset(setPath, copyPath), Is.True);
            var tooMany = FunctionalErrors(root).Select(issue => issue.Message)
                .Single(message => message.Contains("超過一個元件集"));
            Assert.That(tooMany, Does.Contain(setPath).And.Contain(copyPath));
            Assert.That(Assert.Throws<InvalidOperationException>(() => UIPlan.Apply(@"{ ""views"": [{ ""id"": ""more"" }] }"))
                .Message, Does.Contain("超過一個元件集"));
            Assert.That(AssetDatabase.DeleteAsset(copyPath), Is.True);
            Assert.That(AssetDatabase.DeleteAsset(setPath), Is.True);
            Assert.That(FunctionalErrors(root).Select(issue => issue.Message)
                .Count(message => message.Contains("沒有元件集")), Is.EqualTo(1));
        }

        [Test]
        public void InspectorsShowTheNewViewRootAndWidgetSetSettings()
        {
            var root = UIAuthoring.CreateWorkspace(NewScene("Batch3 Inspectors"));
            var page = UIAuthoring.CreatePage(root, "home", "Home");
            var popup = UIAuthoring.CreatePopup(root, "popup", "Popup", $"{folder}/Inspector.prefab");
            Assert.That(BoundPaths(root), Does.Contain("handlesBackKey").And.Contain("pageTransitionMode"));
            Assert.That(BoundPaths(page), Does.Contain("defaultSelected").And.Contain("blocksBackKey"));
            Assert.That(PopupOptions(page).style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(BoundPaths(popup), Does.Contain("reusesInstance").And.Contain("closesOnOverlayClick"));
            Assert.That(PopupOptions(popup).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(BoundPaths(UIWidgets.Find()),
                Does.Contain("widgets").And.Contain("listItem").And.Contain("confirm"));

            string[] BoundPaths(Object target)
            {
                var inspector = UnityEditor.Editor.CreateEditor(target);
                try
                {
                    return inspector.CreateInspectorGUI().Query<PropertyField>().ToList()
                        .Select(field => field.bindingPath).ToArray();
                }
                finally
                {
                    Object.DestroyImmediate(inspector);
                }
            }

            VisualElement PopupOptions(UIView view)
            {
                var inspector = UnityEditor.Editor.CreateEditor(view);
                try
                {
                    return inspector.CreateInspectorGUI().Query<PropertyField>().ToList()
                        .First(field => field.bindingPath == "reusesInstance").parent;
                }
                finally
                {
                    Object.DestroyImmediate(inspector);
                }
            }
        }

        private static string SingleError(UIRoot root)
        {
            return FunctionalErrors(root).Select(issue => issue.Message).Single();
        }

        private static TMP_FontAsset FontOf(UIWidgetSet set, string widget)
        {
            return set.Find(widget).Prefab.GetComponentInChildren<TMP_Text>(true).font;
        }

        private string CopyFont(TMP_FontAsset source, string name)
        {
            var path = $"{folder}/{name}.asset";
            Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path), Is.True);
            return path;
        }

        private UIRoot WorkspaceWithHomePage(string sceneName)
        {
            var scene = NewScene(sceneName);
            SceneManager.SetActiveScene(scene);
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            return root;
        }

        private static void AssertNothingWasApplied(UIRoot root)
        {
            Assert.That(root.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "home" }));
            Assert.That(UIAuthoring.FindEntry(root, "home").Review.GetComponentsInChildren<UIElementId>(true),
                Is.Empty);
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void SavePrefab(string path, GameObject source)
        {
            try
            {
                Assert.That(PrefabUtility.SaveAsPrefabAsset(source, path), Is.Not.Null, path);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        private static Dictionary<string, string> Snapshot(string assetFolder)
        {
            return Directory.GetFiles(assetFolder).ToDictionary(file => file.Replace('\\', '/'),
                file => $"{File.GetLastWriteTimeUtc(file).Ticks}:{Convert.ToBase64String(File.ReadAllBytes(file)).GetHashCode()}");
        }

        private static IEnumerable<string> InputErrors(UIRoot root)
        {
            return FunctionalErrors(root).Select(issue => issue.Message)
                .Where(message => message.Contains("EventSystem") || message.Contains("Active Input Handling"));
        }
    }
}
