using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    [PrebuildSetup(typeof(UIWidgetTestAssets))]
    [PostBuildCleanup(typeof(UIWidgetTestAssets))]
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator StarterWidgetsResolveTheirComponentsById()
        {
            CreateRoot();
            foreach (var kind in new[]
            {
                "Text", "Image", "Panel", "Button", "Toggle", "Slider", "ProgressBar", "Dropdown", "InputField",
                "ScrollList"
            })
                AddWidget(first, "my" + kind, kind);
            rootObject.SetActive(true);
            yield return StartPage();

            Assert.That(first.Get<TMP_Text>("myText").text, Is.EqualTo("Text"));
            Assert.That(first.Get<Image>("myImage").gameObject, Is.SameAs(first.GetElement("myImage")));
            Assert.That(first.Get<Image>("myPanel").raycastTarget, Is.False);
            Assert.That(first.Get<Button>("myButton").targetGraphic, Is.Not.Null);
            Assert.That(first.Get<TMP_Text>("myButton").text, Is.EqualTo("Button"));
            Assert.That(first.Get<Toggle>("myToggle").graphic, Is.Not.Null);
            Assert.That(first.Get<Slider>("mySlider").fillRect, Is.Not.Null);
            Assert.That(first.Get<UIProgressBar>("myProgressBar").Fill, Is.Not.Null);
            Assert.That(first.Get<TMP_Dropdown>("myDropdown").template, Is.Not.Null);
            Assert.That(first.Get<TMP_InputField>("myInputField").textComponent, Is.Not.Null);
            Assert.That(first.Get<UIList>("myScrollList").Content, Is.Not.Null);
            Assert.That(first.Get<UIList>("myScrollList").ItemPrefab, Is.SameAs(UIWidgetTestAssets.Load("ListItem")));
            Assert.That(first.GetElement("myButton").GetComponent<UIElementId>().Kind, Is.EqualTo("Button"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ProgressBarShowsItsValueAndClampsIt()
        {
            CreateRoot();
            var bar = AddWidget(first, "loading", "ProgressBar").GetComponent<UIProgressBar>();
            rootObject.SetActive(true);
            yield return StartPage();

            Assert.That(bar.Fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(bar.Fill.sprite, Is.Not.Null);
            bar.Value = 0.3f;
            Assert.That(bar.Value, Is.EqualTo(0.3f));
            Assert.That(bar.Fill.fillAmount, Is.EqualTo(0.3f));
            bar.Value = 2f;
            Assert.That(bar.Value, Is.EqualTo(1f));
            Assert.That(bar.Fill.fillAmount, Is.EqualTo(1f));
            bar.Value = -1f;
            Assert.That(bar.Value, Is.Zero);
            Assert.That(bar.Fill.fillAmount, Is.Zero);
            bar.Value = 0.6f;
            bar.Value = float.NaN;
            Assert.That(bar.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ListCreatesReusesAndDeactivatesItems()
        {
            CreateRoot();
            var list = AddWidget(first, "saves", "ScrollList").GetComponent<UIList>();
            rootObject.SetActive(true);
            yield return StartPage();

            var bound = new List<string>();
            void Bind(GameObject item, string label, int index)
            {
                UIList.GetInItem<TMP_Text>(item, "label").text = label;
                bound.Add($"{index}:{label}");
            }

            Assert.That(list.Count, Is.Zero);
            list.SetItems(new[] { "A", "B", "C" }, Bind);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(bound, Is.EqualTo(new[] { "0:A", "1:B", "2:C" }));
            var created = list.Items.ToArray();
            Assert.That(created.All(item => item.activeSelf && item.transform.parent == list.Content), Is.True);
            Assert.That(created.Select(item => item.transform.GetSiblingIndex()), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(created.Select(item => UIList.GetInItem<TMP_Text>(item, "label").text),
                Is.EqualTo(new[] { "A", "B", "C" }));

            // 多的項目停用並保留，每次呼叫都會對啟用的項目執行 bind。
            bound.Clear();
            list.SetItems(new[] { "X" }, Bind);
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list.Items[0], Is.SameAs(created[0]));
            Assert.That(bound, Is.EqualTo(new[] { "0:X" }));
            Assert.That(created[1] && !created[1].activeSelf && created[2] && !created[2].activeSelf, Is.True);
            Assert.That(list.Content.childCount, Is.EqualTo(3));

            // 依索引重用，不夠才建立。
            bound.Clear();
            list.SetItems(new[] { "P", "Q", "R", "S" }, Bind);
            Assert.That(list.Items.Take(3), Is.EqualTo(created));
            Assert.That(list.Items.All(item => item.activeSelf), Is.True);
            Assert.That(list.Content.childCount, Is.EqualTo(4));
            Assert.That(bound, Is.EqualTo(new[] { "0:P", "1:Q", "2:R", "3:S" }));

            var counted = new List<int>();
            list.SetCount(2, (item, index) => counted.Add(index));
            Assert.That(counted, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(list.Items, Is.EqualTo(created.Take(2)));
            list.SetCount(2, null);
            list.Clear();
            Assert.That(list.Count, Is.Zero);
            Assert.That(list.Items, Is.Empty);
            Assert.That(list.Content.Cast<Transform>().All(item => !item.gameObject.activeSelf), Is.True);

            // 被外部銷毀的項目會在同一個索引重新建立。
            Object.DestroyImmediate(created[1]);
            list.SetItems(new[] { "A", "B", "C" }, Bind);
            Assert.That(list.Items[0], Is.SameAs(created[0]));
            Assert.That(list.Items[1], Is.Not.Null);
            Assert.That(list.Items.Select(item => item.transform.GetSiblingIndex()), Is.EqualTo(new[] { 0, 1, 2 }));

            Assert.Throws<ArgumentNullException>(() => list.SetItems<string>(null, Bind));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.SetCount(-1, null));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ListReportsItemClicksOnceAndKeepsItemIdsToItself()
        {
            CreateRoot();
            var list = AddWidget(first, "saves", "ScrollList").GetComponent<UIList>();
            var outside = AddElement(first, "label");
            rootObject.SetActive(true);
            yield return StartPage();

            var clicks = new List<int>();
            list.ItemClicked += clicks.Add;
            for (var repeat = 0; repeat < 3; repeat++)
            {
                list.SetItems(new[] { "A", "B", "C" },
                    (item, label, index) => UIList.GetInItem<TMP_Text>(item, "label").text = label);
            }
            list.Items[2].GetComponent<Button>().onClick.Invoke();
            Assert.That(clicks, Is.EqualTo(new[] { 2 }), "重複呼叫 SetItems 之後點一次只觸發一次");
            list.Items[0].GetComponent<Button>().onClick.Invoke();
            Assert.That(clicks, Is.EqualTo(new[] { 2, 0 }));

            // 項目裡的 ID 只在項目內有效：View 找到的是清單外面的那一個。
            Assert.That(first.GetElement("label"), Is.SameAs(outside));
            Object.DestroyImmediate(outside);
            Assert.That(first.TryGet<TMP_Text>("label", out _), Is.False);
            var missing = Assert.Throws<InvalidOperationException>(() => first.Get<TMP_Text>("label"));
            Assert.That(missing.Message, Does.Contain("label"));
            Assert.That(first.Get<UIList>("saves"), Is.SameAs(list));

            Assert.That(UIList.GetInItem<TMP_Text>(list.Items[1], "label").text, Is.EqualTo("B"));
            Assert.That(UIList.GetInItem<RectTransform>(list.Items[1], "label").gameObject,
                Is.SameAs(UIList.GetInItem<TMP_Text>(list.Items[1], "label").gameObject));
            var absent = Assert.Throws<InvalidOperationException>(
                () => UIList.GetInItem<TMP_Text>(list.Items[1], "price"));
            Assert.That(absent.Message, Does.Contain("price").And.Contain(list.Items[1].name));
            Assert.Throws<InvalidOperationException>(() => UIList.GetInItem<Slider>(list.Items[1], "label"));
        }

        [UnityTest]
        public IEnumerator ListAppliesItsDirectionAndRebuildsItemsForANewPrefab()
        {
            CreateRoot();
            var list = AddWidget(first, "saves", "ScrollList").GetComponent<UIList>();
            rootObject.SetActive(true);
            yield return StartPage();

            var scroll = list.GetComponent<ScrollRect>();
            var layout = list.Content.GetComponent<UIListLayout>();
            Assert.That((list.Direction, scroll.vertical, scroll.horizontal, layout.Vertical),
                Is.EqualTo((UIListDirection.Vertical, true, false, true)));
            list.SetCount(3, null);
            Canvas.ForceUpdateCanvases();
            Assert.That(list.Items[1].transform.localPosition.y, Is.LessThan(list.Items[0].transform.localPosition.y));

            list.Direction = UIListDirection.Horizontal;
            Canvas.ForceUpdateCanvases();
            Assert.That((scroll.vertical, scroll.horizontal, layout.Vertical), Is.EqualTo((false, true, false)));
            Assert.That(list.Items[1].transform.localPosition.x,
                Is.GreaterThan(list.Items[0].transform.localPosition.x));
            Assert.That(list.Items[1].transform.localPosition.y,
                Is.EqualTo(list.Items[0].transform.localPosition.y).Within(0.01f));

            var old = list.Items.ToArray();
            var replacement = new GameObject("Row", typeof(RectTransform), typeof(Image));
            try
            {
                list.ItemPrefab = replacement;
                Assert.That(list.Count, Is.Zero);
                list.SetCount(2, null);
                Assert.That(list.Items.All(item => item.name.StartsWith("Row") && !item.GetComponent<Button>()),
                    Is.True);
                yield return null;
                Assert.That(old.All(item => !item), Is.True, "換掉 ItemPrefab 之後舊項目會銷毀");
            }
            finally
            {
                Object.DestroyImmediate(replacement);
            }

            list.ItemPrefab = null;
            var noPrefab = Assert.Throws<InvalidOperationException>(() => list.SetCount(1, null));
            Assert.That(noPrefab.Message, Does.Contain("ItemPrefab"));
        }

        private static GameObject AddWidget(UIView view, string id, string kind)
        {
            var widget = Object.Instantiate(UIWidgetTestAssets.Load(kind), view.Content, false);
            widget.name = id;
            var element = widget.AddComponent<UIElementId>();
            element.id = id;
            element.kind = kind;
            return widget;
        }
    }
}
