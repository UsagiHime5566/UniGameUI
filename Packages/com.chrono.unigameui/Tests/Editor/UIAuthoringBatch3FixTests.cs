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

        private static Color Html(string value)
        {
            Assert.That(ColorUtility.TryParseHtmlString(value, out var color), Is.True, value);
            return color;
        }
    }
}
