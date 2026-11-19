using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UniGameUI.Tests
{
    // 返回鍵：Esc 與手把的取消鍵。五個判斷步驟各有對應的案例。
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator BackKeyClosesPopupsThenGoesBackAndReportsWhenThereIsNowhereToGo()
        {
            UseInput();
            CreateRoot();
            AddNavButton(first, "a", Vector2.zero);
            var atRoot = 0;
            var hidden = new List<string>();
            yield return StartPageWithInput();
            root.BackRequestedAtRoot += () => atRoot++;
            root.ViewHidden += view => hidden.Add(view.Id);

            // 沒有 Popup 也沒有上一頁：只發出事件，不呼叫 BackAsync。
            yield return Tap(keyboard.escapeKey);
            Assert.That(atRoot, Is.EqualTo(1));
            Assert.That(hidden, Is.Empty);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));

            // 有上一頁時返回。
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            Assert.That(atRoot, Is.EqualTo(1));

            // 手把的取消鍵相同：先關 Popup，再返回，最後回報已經在最底層。
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            yield return Tap(gamepad.buttonEast);
            Assert.That(root.CurrentPopupId, Is.Null);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            yield return Tap(gamepad.buttonEast);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            yield return Tap(gamepad.buttonEast);
            Assert.That(atRoot, Is.EqualTo(2));

            // 整個關掉時不處理，也不發出事件。
            root.HandlesBackKey = false;
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            Assert.That(atRoot, Is.EqualTo(2));
            root.HandlesBackKey = true;
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BackHandlersCanTakeOverAndAFailingOneCountsAsUnhandled()
        {
            UseInput();
            CreateRoot();
            var primary = AddHandler(next, "primary");
            var secondary = AddHandler(next, "secondary");
            var onOtherView = AddHandler(first, "home");
            var calls = new List<string>();
            var handled = true;
            primary.BackRequested = () =>
            {
                calls.Add("primary");
                return handled;
            };
            secondary.BackRequested = () =>
            {
                calls.Add("secondary");
                return false;
            };
            onOtherView.BackRequested = () =>
            {
                calls.Add("home");
                return true;
            };
            yield return StartPageWithInput();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);

            // 有一個回傳 true 就停止，框架不再返回。只問最上層畫面的接收者。
            yield return Tap(keyboard.escapeKey);
            Assert.That(calls, Is.EqualTo(new[] { "primary" }));
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));

            // 都回傳 false 時照常返回。
            calls.Clear();
            handled = false;
            yield return Tap(gamepad.buttonEast);
            Assert.That(calls, Is.EqualTo(new[] { "primary", "secondary" }));
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));

            // 丟出例外時記錄下來，當成沒有處理：出錯不能讓玩家離不開畫面。
            onOtherView.BackRequested = null;
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            calls.Clear();
            primary.BackRequested = () => throw new InvalidOperationException("back-failed");
            ExpectLog(LogType.Error, "Back 'next' OnBackRequested", "接收者丟出例外，當成沒有處理。", "back-failed");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: back-failed"));
            yield return Tap(keyboard.escapeKey);
            Assert.That(calls, Is.EqualTo(new[] { "secondary" }));
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));

            // 接收者被呼叫時沒有導航在進行，可以直接導航。
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Awaitable<UINavResult> opened = null;
            primary.BackRequested = () =>
            {
                opened = root.OpenPopupAsync("popup");
                return true;
            };
            yield return Tap(keyboard.escapeKey);
            yield return Expect(opened, UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ViewsCanIgnoreTheBackKeyWhileTheirRoutesStillWork()
        {
            UseInput();
            CreateRoot();
            next.blocksBackKey = true;
            popupTemplate.blocksBackKey = true;
            var consulted = 0;
            AddHandler(next, "next").BackRequested = () =>
            {
                consulted++;
                return false;
            };
            var atRoot = 0;
            yield return StartPageWithInput();
            root.BackRequestedAtRoot += () => atRoot++;
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);

            // 接收者先被詢問，接著才看畫面的設定。
            yield return Tap(keyboard.escapeKey);
            yield return Tap(gamepad.buttonEast);
            Assert.That(consulted, Is.EqualTo(2));
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));

            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            Assert.That(root.TopPopup.BlocksBackKey, Is.True);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(consulted, Is.EqualTo(2), "只問最上層的畫面");

            // 這個設定只管按鍵，程式與路由的返回不受影響。
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
            Assert.That(atRoot, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BackKeyLeavesTextEditingAndOpenDropdownsAlone()
        {
            UseInput();
            CreateRoot();
            var fieldObject = AddWidget(next, "name", "InputField");
            Place(fieldObject, new Vector2(0, 60), new Vector2(200, 40));
            var field = fieldObject.GetComponent<TMP_InputField>();
            var dropdownObject = AddWidget(next, "quality", "Dropdown");
            Place(dropdownObject, new Vector2(0, -20), new Vector2(200, 40));
            var dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            yield return StartPageWithInput();
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);

            // 輸入框正在編輯：框架不處理，取消鍵由輸入框自己用來結束編輯。
            yield return Click(field);
            yield return Frames(4);
            Assert.That(field.isFocused, Is.True);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            Assert.That(field.isFocused, Is.False, "輸入框自己結束編輯");

            yield return Click(field);
            yield return Frames(4);
            Assert.That(field.isFocused, Is.True);
            yield return Tap(gamepad.buttonEast);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            Assert.That(field.isFocused, Is.False);
            yield return Frames(5);

            // 下拉選單展開中：取消鍵只收起選單。
            dropdown.Show();
            yield return Frames(3);
            Assert.That(dropdown.IsExpanded, Is.True);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (dropdown.IsExpanded && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(dropdown.IsExpanded, Is.False, "下拉選單會自己收起");

            // 都結束之後，返回鍵恢復作用。
            yield return Frames(3);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.CurrentPageId, Is.EqualTo("home"));
        }

        [UnityTest]
        public IEnumerator BackKeyIsIgnoredWhileATransitionIsPlaying()
        {
            if (!UITransition.IsDOTweenReady)
                Assert.Ignore("此案例需要 DOTween 才有跨影格的轉場。");
            UseInput();
            CreateRoot();
            next.enter.preset = UITransitionPreset.Fade;
            next.enter.duration = 1.2f;
            var consulted = 0;
            AddHandler(first, "home").BackRequested = () =>
            {
                consulted++;
                return false;
            };
            yield return StartPageWithInput();

            var transition = root.ShowPageAsync("next");
            Assert.That(root.IsTransitioning, Is.True);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.IsTransitioning, Is.True, "按鍵要落在轉場期間");
            yield return Expect(transition, UINavResult.Completed);
            yield return Frames();
            Assert.That(consulted, Is.Zero);
            Assert.That(root.CurrentPageId, Is.EqualTo("next"));
            Assert.That(root.HistoryCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BackKeyClosesAPopupThatIsWaitingForAResultWithoutAValue()
        {
            UseInput();
            CreateRoot();
            yield return StartPageWithInput();

            var waiting = root.OpenPopupForResultAsync<string>("popup");
            yield return Frames();
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            yield return Tap(keyboard.escapeKey);
            yield return ExpectPopup(waiting, UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);

            waiting = root.OpenPopupForResultAsync<string>("popup");
            yield return Frames();
            yield return Tap(gamepad.buttonEast);
            yield return ExpectPopup(waiting, UINavResult.Completed);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
