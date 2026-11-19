using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    public sealed partial class UINavigationTests
    {
        private sealed class TestLocalizer : IUILocalizer
        {
            public readonly Dictionary<string, string> Texts = new Dictionary<string, string>();
            public readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
            public bool Throws;

            public bool TryGetText(string key, out string text)
            {
                if (Throws)
                    throw new FormatException("localizer-failed");
                return Texts.TryGetValue(key, out text);
            }

            public bool TryGetSprite(string key, out Sprite sprite)
            {
                return Sprites.TryGetValue(key, out sprite);
            }
        }

        [UnityTest]
        public IEnumerator LocalizedContentIsResolvedBeforeShowingAndRefreshedOnDemand()
        {
            CreateRoot();
            var texture = new Texture2D(4, 4);
            var english = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
            var german = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            try
            {
                var title = AddWidget(next, "title", "Text").GetComponent<TMP_Text>();
                title.text = "Default title";
                title.GetComponent<UILocalizedText>().Key = "title";
                var flag = AddWidget(next, "flag", "Image").GetComponent<Image>();
                flag.GetComponent<UILocalizedImage>().Key = "flag";
                var quality = AddWidget(next, "quality", "Dropdown").GetComponent<TMP_Dropdown>();
                quality.GetComponent<UILocalizedDropdown>().keys = new[] { "quality.low", "", "quality.high" };
                var homeButton = AddWidget(first, "start", "Button").GetComponentInChildren<TMP_Text>();
                homeButton.GetComponent<UILocalizedText>().Key = "start";
                var popupMessage = AddWidget(popupTemplate, "message", "Text").GetComponent<TMP_Text>();
                popupMessage.GetComponent<UILocalizedText>().Key = "message";

                var localizer = new TestLocalizer();
                localizer.Texts["title"] = "Settings";
                localizer.Texts["start"] = "Start";
                localizer.Texts["message"] = "Saved";
                localizer.Texts["quality.low"] = "Low";
                localizer.Texts["quality.high"] = "High";
                localizer.Sprites["flag"] = english;
                var handler = AddHandler(next, "next");
                var seenWhenShowing = new List<string>();
                UIViewTestHandler.Observed = (source, phase, _) =>
                {
                    if (source != handler || phase != "Showing")
                        return;
                    seenWhenShowing.Add(title.text);
                    seenWhenShowing.Add(quality.options[0].text);
                    // 頁面腳本在取值之後仍然可以改寫。
                    title.text = "Overridden";
                };

                rootObject.SetActive(true);
                root.Localizer = localizer;
                yield return StartPage();
                Assert.That(homeButton.text, Is.EqualTo("Start"));

                yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
                Assert.That(seenWhenShowing, Is.EqualTo(new[] { "Settings", "Low" }));
                Assert.That(title.text, Is.EqualTo("Overridden"));
                Assert.That(flag.sprite, Is.SameAs(english));
                Assert.That(quality.options.Select(option => option.text),
                    Is.EqualTo(new[] { "Low", "Option B", "High" }));
                Assert.That(quality.captionText.text, Is.EqualTo("Low"));

                yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
                var openPopup = root.TopPopup.Get<TMP_Text>("message");
                Assert.That(openPopup.text, Is.EqualTo("Saved"));

                // 切換語言：現存的畫面全部更新，包含沒有顯示的首頁。
                localizer.Texts["title"] = "Einstellungen";
                localizer.Texts["start"] = "Starten";
                localizer.Texts["message"] = "Gespeichert";
                localizer.Texts["quality.low"] = "Niedrig";
                localizer.Sprites["flag"] = german;
                var changes = 0;
                root.LocalizationChanged += () => changes++;
                root.RefreshLocalization();
                Assert.That(title.text, Is.EqualTo("Einstellungen"));
                Assert.That(first.gameObject.activeSelf, Is.False);
                Assert.That(homeButton.text, Is.EqualTo("Starten"));
                Assert.That(openPopup.text, Is.EqualTo("Gespeichert"));
                Assert.That(flag.sprite, Is.SameAs(german));
                Assert.That(quality.options[0].text, Is.EqualTo("Niedrig"));
                Assert.That(changes, Is.EqualTo(1));

                // 換掉來源也會重新取值；設成 null 時顯示預設內容。
                root.Localizer = null;
                Assert.That(title.text, Is.EqualTo("Default title"));
                Assert.That(homeButton.text, Is.EqualTo("Button"));
                Assert.That(flag.sprite, Is.Null);
                Assert.That(quality.options.Select(option => option.text),
                    Is.EqualTo(new[] { "Option A", "Option B", "Option C" }));
                Assert.That(changes, Is.EqualTo(2));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(english);
                Object.DestroyImmediate(german);
                Object.DestroyImmediate(texture);
            }
        }

        [UnityTest]
        public IEnumerator MissingKeysShowTheDefaultContentAndWarnOncePerKey()
        {
            CreateRoot();
            var title = AddWidget(first, "title", "Text").GetComponent<TMP_Text>();
            title.text = "Default title";
            title.GetComponent<UILocalizedText>().Key = "missing.title";
            var icon = AddWidget(first, "icon", "Image").GetComponent<Image>();
            icon.GetComponent<UILocalizedImage>().Key = "missing.icon";
            var warnings = new List<string>();
            void Capture(string message, string stackTrace, LogType type)
            {
                if (type == LogType.Warning && message.Contains("多語系鍵值"))
                    warnings.Add(message);
            }

            Application.logMessageReceived += Capture;
            try
            {
                // 沒有設定 Localizer 時不警告。
                rootObject.SetActive(true);
                yield return StartPage();
                Assert.That(title.text, Is.EqualTo("Default title"));
                Assert.That(root.Localize("missing.title"), Is.EqualTo("missing.title"));
                Assert.That(warnings, Is.Empty);

                var localizer = new TestLocalizer();
                localizer.Texts["known"] = "Known";
                root.Localizer = localizer;
                Assert.That(title.text, Is.EqualTo("Default title"));
                Assert.That(icon.sprite, Is.Null);
                Assert.That(warnings.Count, Is.EqualTo(2), string.Join("\n", warnings));
                Assert.That(warnings.Single(message => message.Contains("missing.title")),
                    Does.Contain("home").And.Contain("title").And.Contain(rootObject.scene.name));
                Assert.That(warnings.Single(message => message.Contains("missing.icon")),
                    Does.Contain("home").And.Contain("icon"));

                // 再顯示、再重新取值都不會重複警告同一個鍵值。
                yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
                yield return Expect(root.BackAsync(), UINavResult.Completed);
                root.RefreshLocalization();
                Assert.That(warnings.Count, Is.EqualTo(2));
                Assert.That(title.text, Is.EqualTo("Default title"));

                // Localize 的三種結果。
                Assert.That(root.Localize("known"), Is.EqualTo("Known"));
                Assert.That(root.Localize("absent", "Fallback"), Is.EqualTo("Fallback"));
                Assert.That(root.Localize("absent"), Is.EqualTo("absent"));
                Assert.That(root.Localize("known", "Fallback"), Is.EqualTo("Known"));
                Assert.That(warnings.Count, Is.EqualTo(3));
                Assert.That(root.Localize(null, "Fallback"), Is.EqualTo("Fallback"));

                // 來源自己丟例外時記錄下來，當成查不到。
                localizer.Throws = true;
                LogAssert.Expect(LogType.Exception, new Regex("FormatException: localizer-failed"));
                Assert.That(root.Localize("known", "Fallback"), Is.EqualTo("Fallback"));
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }
        }

        [UnityTest]
        public IEnumerator LocalizationHandlersAreNotifiedAfterRefreshAndFailuresStayIsolated()
        {
            CreateRoot();
            var title = AddWidget(first, "title", "Text").GetComponent<TMP_Text>();
            title.GetComponent<UILocalizedText>().Key = "title";
            var failing = AddHandler(first, "failing");
            var healthy = AddHandler(first, "healthy");
            var hidden = AddHandler(next, "hidden");
            AddHandler(popupTemplate, "popup");
            failing.LocalizationFailure = "handler-failed";
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);

            var localizer = new TestLocalizer();
            localizer.Texts["title"] = "Hello";
            var timeline = new List<string>();
            UIViewTestHandler.Observed = (source, phase, _) =>
            {
                if (phase == "Localization")
                    timeline.Add($"{source.label}:{title.text}");
            };
            root.LocalizationChanged += () => throw new InvalidOperationException("subscriber-failed");
            root.LocalizationChanged += () => timeline.Add("event");

            ExpectLog(LogType.Error, "RefreshLocalization 'home' OnLocalizationChanged",
                "接收者丟出例外，其餘接收者照常執行。", "handler-failed");
            LogAssert.Expect(LogType.Exception, new Regex("FormatException: handler-failed"));
            ExpectLog(LogType.Error, "LocalizationChanged", "接收者丟出例外，其餘接收者照常執行。",
                "subscriber-failed");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: subscriber-failed"));
            root.Localizer = localizer;

            // 重新取值在前，接著依序通知每個現存的畫面（頁面在前、Popup 在後），最後才是事件。
            Assert.That(timeline, Is.EqualTo(new[]
            {
                "failing:Hello", "healthy:Hello", "hidden:Hello", "popup:Hello", "event"
            }));
            Assert.That(hidden.View.gameObject.activeSelf, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ListItemsResolveTheirKeysWhenCreatedReusedAndRefreshed()
        {
            CreateRoot();
            var list = AddWidget(first, "shop", "ScrollList").GetComponent<UIList>();
            var template = Object.Instantiate(UIWidgetTestAssets.Load("ListItem"), prefabContainer.transform);
            template.GetComponentInChildren<UILocalizedText>(true).Key = "shop.buy";
            list.ItemPrefab = template;
            var localizer = new TestLocalizer();
            localizer.Texts["shop.buy"] = "Buy";
            rootObject.SetActive(true);
            root.Localizer = localizer;
            yield return StartPage();

            list.SetCount(2, null);
            Assert.That(Labels(), Is.EqualTo(new[] { "Buy", "Buy" }));

            // bind 在取值之後執行，可以改寫；重用時會再取一次值。
            list.SetCount(2, (item, index) =>
            {
                if (index == 1)
                    UIList.GetInItem<TMP_Text>(item, "label").text = "Sold out";
            });
            Assert.That(Labels(), Is.EqualTo(new[] { "Buy", "Sold out" }));
            list.SetCount(3, null);
            Assert.That(Labels(), Is.EqualTo(new[] { "Buy", "Buy", "Buy" }));

            localizer.Texts["shop.buy"] = "Kaufen";
            root.RefreshLocalization();
            Assert.That(Labels(), Is.EqualTo(new[] { "Kaufen", "Kaufen", "Kaufen" }));

            IEnumerable<string> Labels()
            {
                return list.Items.Select(item => UIList.GetInItem<TMP_Text>(item, "label").text);
            }
        }
    }
}
