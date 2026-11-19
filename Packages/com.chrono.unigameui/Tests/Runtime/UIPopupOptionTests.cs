using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    // Popup 的重用與點遮罩關閉。
    public sealed partial class UINavigationTests
    {
        [UnityTest]
        public IEnumerator ReusedPopupsKeepOneInstanceAndBehaveLikeFreshOnes()
        {
            CreateRoot();
            popupTemplate.reusesInstance = true;
            AddHandler(popupTemplate, "popup");
            var otherTemplate = CreateView("other", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "other", kind = UIEntryKind.Popup, prefab = otherTemplate });
            var shown = new List<object>();
            UIViewTestHandler.Observed = (handler, phase, data) =>
            {
                if (phase != "Showing" || handler.label != "popup")
                    return;
                shown.Add(data);
                if (Equals(data, "bad"))
                    throw new InvalidOperationException("showing-failed");
            };
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.OpenPopupAsync("popup", "one"), UINavResult.Completed);
            var instance = root.TopPopup;
            Assert.That(instance.ReusesInstance, Is.True);
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return null;
            Assert.That(instance && !instance.gameObject.activeSelf, Is.True, "關閉後停用並保留");
            Assert.That(root.TryGetView("popup", out _), Is.False);
            Assert.That(root.Popups, Is.Empty);
            Assert.That(root.TopPopup, Is.Null);

            // 再次開啟用同一個實例，移到 Popup 層的最上面，並收到新的資料與 OnShowing。
            yield return Expect(root.OpenPopupAsync("other"), UINavResult.Completed);
            var other = root.TopPopup;
            yield return Expect(root.OpenPopupAsync("popup", "two"), UINavResult.Completed);
            Assert.That(root.TopPopup, Is.SameAs(instance));
            Assert.That(instance.Data, Is.EqualTo("two"));
            Assert.That(shown, Is.EqualTo(new object[] { "one", "two" }));
            Assert.That(instance.transform.GetSiblingIndex(), Is.GreaterThan(other.transform.GetSiblingIndex()));
            Assert.That(instance.Group.interactable, Is.True);
            Assert.That(root.TryGetView("popup", out var found) && found == instance, Is.True);
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return null;
            Assert.That(other == null, Is.True, "沒有設定重用的 Popup 照舊銷毀");
            Assert.That(instance && !instance.gameObject.activeSelf, Is.True);

            // 等待結果的行為相同。
            var waiting = root.OpenPopupForResultAsync<string>("popup", "three");
            yield return null;
            Assert.That(root.TopPopup, Is.SameAs(instance));
            yield return Expect(instance.Close("yes"), UINavResult.Completed);
            yield return ExpectPopup(waiting, UINavResult.Completed, true, "yes");
            Assert.That(instance && !instance.gameObject.activeSelf, Is.True);

            // 顯示前出錯時取消開啟，實例仍然保留給下一次使用。
            ExpectFailure("OpenPopup 'popup' Showing 'popup'", "showing-failed");
            yield return Expect(root.OpenPopupAsync("popup", "bad"), UINavResult.Failed);
            Assert.That(root.TopPopup, Is.Null);
            Assert.That(instance && !instance.gameObject.activeSelf, Is.True);

            // 切頁時被關閉的也保留。
            yield return Expect(root.OpenPopupAsync("popup", "four"), UINavResult.Completed);
            Assert.That(root.TopPopup, Is.SameAs(instance));
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            Assert.That(root.Popups, Is.Empty);
            Assert.That(instance && !instance.gameObject.activeSelf, Is.True);

            // 保留中的實例被外部銷毀時，下次開啟重新建立。
            Object.DestroyImmediate(instance.gameObject);
            yield return Expect(root.OpenPopupAsync("popup", "five"), UINavResult.Completed);
            Assert.That(root.TopPopup, Is.Not.Null);
            Assert.That(root.TopPopup.Data, Is.EqualTo("five"));
            Assert.That(shown, Is.EqualTo(new object[] { "one", "two", "three", "bad", "four", "five" }));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PopupsAreRecreatedEachTimeUnlessTheyAskToBeReused()
        {
            CreateRoot();
            rootObject.SetActive(true);
            yield return StartPage();

            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            var firstInstance = root.TopPopup;
            Assert.That(firstInstance.ReusesInstance, Is.False);
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return null;
            Assert.That(firstInstance == null, Is.True);
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            Assert.That(root.TopPopup, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OverlayClicksCloseOnlyTheTopPopupThatAsksForIt()
        {
            UseInput();
            CreateRoot();
            AddOverlay(popupTemplate);
            AddNavButton(popupTemplate, "inside", Vector2.zero);
            var otherTemplate = CreateView("other", prefabContainer.transform);
            root.entries.Add(new UIEntry { id = "other", kind = UIEntryKind.Popup, prefab = otherTemplate });
            yield return StartPageWithInput();
            var corner = new Vector2(6, 6);
            var emptyContent = new Vector2(Screen.width * 0.5f, Screen.height * 0.32f);

            // 預設不關閉。
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            Assert.That(root.TopPopup.Overlay, Is.Not.Null);
            yield return ClickAt(corner);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);

            // 開啟選項之後，點在內容上不算：不論點到的是按鈕還是內容的空白處。
            popupTemplate.closesOnOverlayClick = true;
            var waiting = root.OpenPopupForResultAsync<string>("popup");
            yield return Frames();
            var popup = root.TopPopup;
            var clicks = 0;
            popup.Get<Button>("inside").onClick.AddListener(() => clicks++);
            yield return ClickAt(emptyContent);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            yield return Click(popup.Get<Button>("inside"));
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));

            // 不是最上層時無效。
            yield return Expect(root.OpenPopupAsync("other"), UINavResult.Completed);
            yield return ClickAt(corner);
            Assert.That(root.Popups.Count, Is.EqualTo(2));
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);

            // 點遮罩等同不帶結果的關閉。
            yield return ClickAt(corner);
            yield return ExpectPopup(waiting, UINavResult.Completed);
            Assert.That(root.CurrentPopupId, Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator ClickAt(Vector2 screenPoint)
        {
            yield return MoveMouse(screenPoint);
            yield return Tap(mouse.leftButton);
        }

        // 測試用的 View 沒有遮罩，這裡照編輯工具建立 Popup 的樣子補上：全螢幕遮罩，內容縮在中間。
        private static void AddOverlay(UIView view)
        {
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)overlay.transform;
            rect.SetParent(view.transform, false);
            rect.SetAsFirstSibling();
            UIRoot.Stretch(rect);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            view.overlay = rect;
            view.Motion.anchorMin = new Vector2(0.25f, 0.25f);
            view.Motion.anchorMax = new Vector2(0.75f, 0.75f);
        }
    }
}
