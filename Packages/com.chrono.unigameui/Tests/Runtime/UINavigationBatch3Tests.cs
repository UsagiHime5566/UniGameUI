using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
