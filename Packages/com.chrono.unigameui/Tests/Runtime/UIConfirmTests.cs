using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    // 確認框：用安裝出來的起始 Prefab 測試，和專案實際拿到的是同一份。
    public sealed partial class UINavigationTests
    {
        // View Prefab 的畫面 ID 是固定的。要測另一個 ID 的確認框時，複製一份並改掉它的 ID。
        private void RegisterConfirm(string id = UIRoot.DefaultConfirmPopupId)
        {
            var prefab = UIWidgetTestAssets.Load("Confirm").GetComponent<UIView>();
            if (id != UIRoot.DefaultConfirmPopupId)
            {
                prefab = Object.Instantiate(prefab, prefabContainer.transform);
                prefab.id = id;
            }
            root.entries.Add(new UIEntry { id = id, kind = UIEntryKind.Popup, prefab = prefab });
        }

        private static IEnumerator ExpectAnswer(Awaitable<bool> operation, bool expected)
        {
            var awaiter = operation.GetAwaiter();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!awaiter.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(awaiter.IsCompleted, Is.True, "等待確認框逾時。");
            Assert.That(awaiter.GetResult(), Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator ConfirmReturnsTrueOnlyWhenConfirmIsPressed()
        {
            UseInput();
            CreateRoot();
            RegisterConfirm();
            yield return StartPageWithInput();

            // 只給訊息：標題隱藏，按鈕用 Prefab 上的文字。
            var answer = root.ConfirmAsync("Delete the save?");
            yield return Frames();
            var popup = root.TopPopup;
            Assert.That(popup.Id, Is.EqualTo("confirm"));
            Assert.That(popup.GetComponent<UIConfirmPopup>(), Is.Not.Null);
            Assert.That(popup.Get<TMP_Text>("message").text, Is.EqualTo("Delete the save?"));
            Assert.That(popup.GetElement("title").activeSelf, Is.False);
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("確定"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("取消"));
            popup.Get<Button>("confirm").onClick.Invoke();
            yield return ExpectAnswer(answer, true);
            Assert.That(root.CurrentPopupId, Is.Null);

            // 取消。確認框設成重用，第二次是同一個實例。
            answer = root.ConfirmAsync("Quit?", "Leave", "Stay", "Really?");
            yield return Frames();
            Assert.That(root.TopPopup, Is.SameAs(popup));
            Assert.That(popup.Get<TMP_Text>("message").text, Is.EqualTo("Quit?"));
            Assert.That(popup.GetElement("title").activeSelf, Is.True);
            Assert.That(popup.Get<TMP_Text>("title").text, Is.EqualTo("Really?"));
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("Leave"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("Stay"));
            popup.Get<Button>("cancel").onClick.Invoke();
            yield return ExpectAnswer(answer, false);

            // 連續開兩次：上一次的標題與按鈕文字不會殘留。
            answer = root.ConfirmAsync("Again?");
            yield return Frames();
            Assert.That(popup.Get<TMP_Text>("message").text, Is.EqualTo("Again?"));
            Assert.That(popup.GetElement("title").activeSelf, Is.False);
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("確定"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("取消"));

            // 返回鍵等同取消。
            yield return Tap(keyboard.escapeKey);
            yield return ExpectAnswer(answer, false);
            answer = root.ConfirmAsync("Once more?");
            yield return Frames();
            yield return Tap(gamepad.buttonEast);
            yield return ExpectAnswer(answer, false);

            // 等待被取消也是 false；取消只結束等待，確認框還開著。
            using var cancellation = new CancellationTokenSource();
            answer = root.ConfirmAsync("Wait?", cancellation: cancellation.Token);
            yield return Frames();
            cancellation.Cancel();
            yield return ExpectAnswer(answer, false);
            Assert.That(root.CurrentPopupId, Is.EqualTo("confirm"));
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
        }

        [UnityTest]
        public IEnumerator ConfirmPopupAcceptsARequestOrAPlainStringAndRejectsOtherData()
        {
            CreateRoot();
            RegisterConfirm();
            rootObject.SetActive(true);
            yield return StartPage();

            var waiting = root.OpenPopupForResultAsync<bool>("confirm", new UIConfirmRequest
            {
                Message = "Overwrite?", Title = "Save", ConfirmText = "Overwrite", CancelText = "Keep"
            });
            yield return null;
            var popup = root.TopPopup;
            Assert.That(popup.Get<TMP_Text>("message").text, Is.EqualTo("Overwrite?"));
            Assert.That(popup.Get<TMP_Text>("title").text, Is.EqualTo("Save"));
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("Overwrite"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("Keep"));
            popup.Get<Button>("confirm").onClick.Invoke();
            yield return ExpectPopup(waiting, UINavResult.Completed, true, true);

            waiting = root.OpenPopupForResultAsync<bool>("confirm", "Just a message");
            yield return null;
            Assert.That(popup.Get<TMP_Text>("message").text, Is.EqualTo("Just a message"));
            Assert.That(popup.GetElement("title").activeSelf, Is.False);
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("確定"));
            popup.Get<Button>("cancel").onClick.Invoke();
            yield return ExpectPopup(waiting, UINavResult.Completed, true, false);

            // 其他型別的資料會取消開啟。
            ExpectFailure("OpenPopup 'confirm' Showing 'confirm'", "UIConfirmRequest");
            yield return Expect(root.OpenPopupAsync("confirm", 42), UINavResult.Failed);
            Assert.That(root.CurrentPopupId, Is.Null);
            ExpectFailure("OpenPopup 'confirm' Showing 'confirm'", "UIConfirmRequest");
            yield return Expect(root.OpenPopupAsync("confirm"), UINavResult.Failed);
        }

        [UnityTest]
        public IEnumerator ConfirmExplainsWhyItCouldNotBeShown()
        {
            CreateRoot();
            RegisterConfirm("styled");
            var warnings = new List<string>();
            void Capture(string message, string stackTrace, LogType type)
            {
                if (type == LogType.Warning && message.Contains("Confirm '"))
                    warnings.Add(message);
            }

            Application.logMessageReceived += Capture;
            try
            {
                rootObject.SetActive(true);
                yield return StartPage();

                // 沒有登記：回傳 false，並附上登記用的那一行計畫。
                ExpectLog(LogType.Error, "Confirm 'confirm'", "找不到 Popup 'confirm'");
                yield return ExpectAnswer(root.ConfirmAsync("Missing?"), false);
                Assert.That(warnings.Count, Is.EqualTo(1));
                Assert.That(warnings[0], Does.Contain("NotFound").And.Contain("\"id\": \"confirm\"")
                    .And.Contain("\"kind\": \"Popup\"").And.Contain("Assets/UniGameUI/Widgets/Confirm.prefab")
                    .And.Contain(rootObject.scene.name));

                // 用 popupId 指定專案自己的確認框。
                var answer = root.ConfirmAsync("Styled?", popupId: "styled");
                yield return null;
                Assert.That(root.CurrentPopupId, Is.EqualTo("styled"));
                root.TopPopup.Get<Button>("confirm").onClick.Invoke();
                yield return ExpectAnswer(answer, true);
                Assert.That(warnings.Count, Is.EqualTo(1));

                // 另一個導航進行中：回傳 false 並說明原因。
                Awaitable<bool> duringNavigation = null;
                next.Shown.AddListener(() => duringNavigation = root.ConfirmAsync("Busy?", popupId: "styled"));
                yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
                yield return ExpectAnswer(duringNavigation, false);
                Assert.That(warnings.Exists(message => message.Contains("Busy") && message.Contains("當成取消")),
                    Is.True, string.Join("\n", warnings));
                Assert.That(root.CurrentPopupId, Is.Null);
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }
        }

        [UnityTest]
        public IEnumerator ConfirmSelectsCancelByDefaultAndFollowsTheLocalizer()
        {
            UseInput();
            CreateRoot();
            RegisterConfirm();
            var localizer = new TestLocalizer();
            localizer.Texts["ui.confirm"] = "OK";
            localizer.Texts["ui.cancel"] = "Cancel";
            yield return StartPageWithInput();
            root.Localizer = localizer;

            var answer = root.ConfirmAsync("Reset?");
            yield return Frames();
            var popup = root.TopPopup;
            Assert.That(popup.DefaultSelected, Is.SameAs(popup.Get<Button>("cancel")));
            Assert.That(popup.ReusesInstance, Is.True);
            Assert.That(popup.BlocksBackKey, Is.False);
            Assert.That(popup.ClosesOnOverlayClick, Is.False);
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("OK"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("Cancel"));

            // 用手把操作：預設選取取消，移到確定再按下。
            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(popup.Get<Button>("cancel").gameObject));
            yield return Tap(gamepad.dpad.right);
            Assert.That(Selected, Is.SameAs(popup.Get<Button>("confirm").gameObject));
            yield return Tap(gamepad.buttonSouth);
            yield return ExpectAnswer(answer, true);

            // 重用的實例再次開啟時，仍然從取消開始；指定的按鈕文字優先於多語系的文字。
            answer = root.ConfirmAsync("Reset again?", confirmText: "Reset");
            yield return Frames();
            Assert.That(root.TopPopup, Is.SameAs(popup));
            Assert.That(Selected, Is.SameAs(popup.Get<Button>("cancel").gameObject));
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("Reset"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("Cancel"));

            // 開著的時候切換語言：沒有指定的按鈕跟著換，指定的文字保留。
            localizer.Texts["ui.cancel"] = "Abbrechen";
            root.RefreshLocalization();
            Assert.That(popup.Get<TMP_Text>("confirm").text, Is.EqualTo("Reset"));
            Assert.That(popup.Get<TMP_Text>("cancel").text, Is.EqualTo("Abbrechen"));
            Assert.That(popup.Get<TMP_Text>("message").text, Is.EqualTo("Reset again?"));
            yield return Tap(gamepad.buttonSouth);
            yield return ExpectAnswer(answer, false);
        }
    }
}
