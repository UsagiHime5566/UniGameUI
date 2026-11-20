using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第六輪審核 J8 的四點是讀程式碼發現的。這裡的案例先在修正前的程式上執行，確認是不是真的問題。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void FailedDefaultFontChangeLeavesTheSceneAndTheWidgetPrefabsAsTheyWere()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            var fontPath = CopyFont(TMP_Settings.defaultFontAsset, "Main SDF");
            var scene = NewScene("Batch3Fix Font Failure");
            SceneManager.SetActiveScene(scene);
            var scenePath = scene.path;
            UIPlan.Apply($@"{{ ""scenePath"": ""{scenePath}"", ""views"": [{{ ""id"": ""home"",
                ""elements"": [{{ ""id"": ""title"", ""text"": ""A"" }}] }}] }}", true);
            var sceneBefore = File.ReadAllText(scenePath);
            var prefabsBefore = Contents(widgets);
            var plan = $@"{{ ""scenePath"": ""{scenePath}"", ""defaultFontPath"": ""{fontPath}"", ""views"": [
                {{ ""id"": ""home"", ""elements"": [{{ ""id"": ""added"", ""text"": ""B"" }}] }},
                {{ ""id"": ""popup"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/FontPopup.prefab"" }}] }}";

            // 換到第三個 Prefab 時失敗：前兩個已經換過字型。
            var replaced = 0;
            UIWidgets.ReplacingFont = path =>
            {
                if (++replaced == 3)
                    throw new IOException("font-failed");
            };
            try
            {
                Assert.That(Assert.Throws<IOException>(() => UIPlan.Apply(plan, true)).Message,
                    Is.EqualTo("font-failed"));
            }
            finally
            {
                UIWidgets.ReplacingFont = null;
            }

            Assert.That(replaced, Is.EqualTo(3));
            Assert.That(File.ReadAllText(scenePath), Is.EqualTo(sceneBefore), "Scene 檔案要回到套用前");
            Assert.That(Contents(widgets), Is.EqualTo(prefabsBefore), "元件 Prefab 要回到套用前");
            Assert.That(UIWidgets.Find().DefaultFont, Is.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath($"{folder}/FontPopup.prefab"), Is.Null);
            var root = UIAuthoring.FindWorkspace(SceneManager.GetSceneByPath(scenePath));
            Assert.That(root.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "home" }));
            Assert.That(UIPlan.FindElement(UIAuthoring.FindEntry(root, "home").Review, "added"), Is.Null);

            // 之後同一份計畫可以正常套用。
            root = UIPlan.Apply(plan, true);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            Assert.That(UIWidgets.Find().DefaultFont, Is.SameAs(font));
            Assert.That(UIPlan.FindElement(UIAuthoring.FindEntry(root, "home").Review, "added")
                .GetComponent<TMP_Text>().font, Is.SameAs(font));
            Assert.That(File.ReadAllText(scenePath), Is.Not.EqualTo(sceneBefore));
        }

        [Test]
        public void PlaceholderFieldsNeedAPlaceholderToWriteTo()
        {
            RequireDOTween();
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            var bare = $"{folder}/BareInput.prefab";
            Assert.That(AssetDatabase.CopyAsset($"{widgets}/InputField.prefab", bare), Is.True);
            EditPrefab(bare, contents =>
            {
                var field = contents.GetComponent<TMP_InputField>();
                Object.DestroyImmediate(field.placeholder.gameObject);
                field.placeholder = null;
            });
            UIWidgets.Register("BareInput", bare, UIWidgetKind.InputField);
            var root = WorkspaceWithHomePage("Batch3Fix Placeholder");

            foreach (var field in new[] { @"""placeholder"": ""Name""", @"""placeholderKey"": ""profile.name""" })
            {
                var error = Assert.Throws<ArgumentException>(() => UIPlan.Apply($@"{{ ""views"": [
                    {{ ""id"": ""home"", ""elements"": [{{ ""id"": ""name"", ""kind"": ""BareInput"", {field} }}] }},
                    {{ ""id"": ""other"" }}] }}"), field);
                Assert.That(error.Message, Does.Contain("home").And.Contain("name").And.Contain("placeholder"));
                AssertNothingWasApplied(root);
            }

            // 沒有 placeholder 的輸入框仍然可以寫其他欄位。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""home"", ""elements"": [
                { ""id"": ""name"", ""kind"": ""BareInput"", ""text"": ""Ada"" }] }] }");
            var created = UIPlan.FindElement(UIAuthoring.FindEntry(root, "home").Review, "name");
            Assert.That(created.GetComponent<TMP_InputField>().text, Is.EqualTo("Ada"));
        }

        [Test]
        public void DefaultFontJudgesInputFieldTextAndPlaceholderSeparately()
        {
            var widgets = UseIsolatedWidgets();
            UIWidgets.Install();
            var tmpDefault = TMP_Settings.defaultFontAsset;
            var mainPath = CopyFont(tmpDefault, "Main SDF");
            var customPath = CopyFont(tmpDefault, "Custom SDF");
            var main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(mainPath);
            var custom = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(customPath);
            var path = $"{widgets}/InputField.prefab";

            // 專案把 placeholder 改成別的字型，輸入的文字維持預設字型。
            EditPrefab(path, contents => Placeholder(contents).font = custom);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(Placeholder(prefab).font, Is.SameAs(custom));
            Assert.That(prefab.GetComponent<TMP_InputField>().textComponent.font, Is.SameAs(tmpDefault));

            UIWidgets.SetDefaultFont(mainPath);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.GetComponent<TMP_InputField>().textComponent.font, Is.SameAs(main));
            Assert.That(Placeholder(prefab).font, Is.SameAs(custom), "專案改過的 placeholder 字型不可以被換掉");
            Assert.That(prefab.GetComponent<TMP_InputField>().fontAsset, Is.SameAs(main));

            // 反過來：文字是專案改過的字型，placeholder 用預設字型。
            EditPrefab(path, contents =>
            {
                contents.GetComponent<TMP_InputField>().textComponent.font = custom;
                Placeholder(contents).font = main;
            });
            UIWidgets.SetDefaultFont("");
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.GetComponent<TMP_InputField>().textComponent.font, Is.SameAs(custom));
            Assert.That(Placeholder(prefab).font, Is.SameAs(tmpDefault));

            static TMP_Text Placeholder(GameObject inputField)
            {
                return (TMP_Text)inputField.GetComponent<TMP_InputField>().placeholder;
            }
        }

        // 只比對內容。檔案被寫回原樣時修改時間會變，這裡不在意。
        private static string Contents(string assetFolder)
        {
            return string.Join("\n", Directory.GetFiles(assetFolder).OrderBy(file => file, StringComparer.Ordinal)
                .Select(file => $"{Path.GetFileName(file)}:{Convert.ToBase64String(File.ReadAllBytes(file)).GetHashCode()}"));
        }
    }
}
