using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    // 導覽與選取。用 Input System 的測試工具模擬鍵盤、手把與滑鼠，輸入照正常的影格順序送進去。
    public sealed partial class UINavigationTests
    {
        private InputTestFixture input;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;

        /// <summary>
        /// 要在 CreateRoot 之前呼叫：輸入模組啟用時才會接到測試用的輸入系統。
        /// </summary>
        private void UseInput()
        {
            input = new InputTestFixture();
            input.Setup();
            // 批次模式的 Editor 沒有視窗焦點，讓輸入模組照常處理輸入。
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();
        }

        private void ReleaseInput()
        {
            if (input == null)
                return;
            var fixture = input;
            input = null;
            fixture.TearDown();
        }

        private EventSystem Events => eventSystemObject.GetComponent<EventSystem>();
        private GameObject Selected => Events.currentSelectedGameObject;

        private static IEnumerator Frames(int count = 3)
        {
            for (var frame = 0; frame < count; frame++)
                yield return null;
        }

        // 事件只排進佇列，下一個影格開頭才處理，順序和實際執行時相同：輸入、輸入模組、工作區。
        private IEnumerator Tap(ButtonControl control)
        {
            input.Press(control, queueEventOnly: true);
            yield return Frames(2);
            input.Release(control, queueEventOnly: true);
            yield return Frames(3);
        }

        private IEnumerator MoveMouse(Vector2 position)
        {
            input.Set(mouse.position, position, queueEventOnly: true);
            yield return Frames(3);
        }

        private IEnumerator Click(Component target)
        {
            var rect = (RectTransform)target.transform;
            yield return MoveMouse(RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return Tap(mouse.leftButton);
        }

        private IEnumerator StartPageWithInput()
        {
            rootObject.SetActive(true);
            eventSystemObject.SendMessage("OnApplicationFocus", true);
            yield return StartPage();
            yield return Frames(2);
        }

        private static Button AddNavButton(UIView view, string id, Vector2 position)
        {
            var widget = AddWidget(view, id, "Button");
            Place(widget, position, new Vector2(160, 40));
            return widget.GetComponent<Button>();
        }

        private static void Place(GameObject widget, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)widget.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        [UnityTest]
        public IEnumerator InputModeFollowsTheLastUsedDevice()
        {
            UseInput();
            CreateRoot();
            AddNavButton(first, "a", Vector2.zero);
            var modes = new List<UIInputMode>();
            yield return StartPageWithInput();
            root.InputModeChanged += modes.Add;
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));

            // 鍵盤的確定與取消不改變模式。
            yield return Tap(keyboard.enterKey);
            yield return Tap(keyboard.escapeKey);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));
            Assert.That(modes, Is.Empty);

            yield return Tap(keyboard.downArrowKey);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));
            yield return MoveMouse(new Vector2(40, 40));
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));

            yield return Tap(gamepad.buttonSouth);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));
            yield return Tap(mouse.leftButton);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));

            yield return Tap(gamepad.dpad.up);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));
            input.Set(mouse.scroll, new Vector2(0, 120), queueEventOnly: true);
            yield return Frames();
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));
            input.Set(mouse.scroll, Vector2.zero, queueEventOnly: true);
            yield return Frames();

            yield return Tap(gamepad.buttonEast);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));
            input.Set(gamepad.leftStick, new Vector2(0, -1), queueEventOnly: true);
            yield return Frames(2);
            input.Set(gamepad.leftStick, Vector2.zero, queueEventOnly: true);
            yield return Frames();
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));

            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            input.BeginTouch(1, new Vector2(20, 20), queueEventOnly: true, screen: touchscreen);
            yield return Frames();
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));
            input.EndTouch(1, new Vector2(20, 20), queueEventOnly: true, screen: touchscreen);
            yield return Frames();

            Assert.That(modes, Is.EqualTo(new[]
            {
                UIInputMode.Navigation, UIInputMode.Pointer, UIInputMode.Navigation, UIInputMode.Pointer,
                UIInputMode.Navigation, UIInputMode.Pointer, UIInputMode.Navigation, UIInputMode.Pointer
            }));
        }

        [UnityTest]
        public IEnumerator EnteringNavigationSelectsTheDefaultElementWithoutActingOnIt()
        {
            UseInput();
            CreateRoot();
            var a = AddNavButton(first, "a", new Vector2(0, 60));
            var b = AddNavButton(first, "b", Vector2.zero);
            AddNavButton(first, "c", new Vector2(0, -60));
            AddNavButton(next, "x", new Vector2(0, 60));
            var y = AddNavButton(next, "y", Vector2.zero);
            next.defaultSelected = y;
            var clicks = new List<string>();
            a.onClick.AddListener(() => clicks.Add("a"));
            b.onClick.AddListener(() => clicks.Add("b"));
            var selections = new List<string>();
            yield return StartPageWithInput();
            root.ElementSelected += (view, selected) => selections.Add($"{view.Id}:{selected.name}");
            Assert.That(Selected, Is.Null);

            // 從 Pointer 進入導覽的那一次輸入只建立選取，不會同時移動。
            yield return Tap(gamepad.dpad.down);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));
            Assert.That(Selected, Is.SameAs(a.gameObject), "沒有指定時選取階層順序第一個可操作的元件");
            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(b.gameObject));
            yield return Tap(gamepad.buttonSouth);
            Assert.That(clicks, Is.EqualTo(new[] { "b" }));

            // 改用滑鼠時清掉選取；再用手把的確定鍵進入導覽，只恢復選取，不會按下去。
            yield return MoveMouse(new Vector2(30, 30));
            Assert.That(Selected, Is.Null);
            yield return Tap(gamepad.buttonSouth);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Navigation));
            Assert.That(Selected, Is.SameAs(b.gameObject), "回到上次在這個畫面選取的元件");
            Assert.That(clicks, Is.EqualTo(new[] { "b" }));

            // 最上層畫面改變時選取它的預設元件。
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Frames();
            Assert.That(next.DefaultSelected, Is.SameAs(y));
            Assert.That(Selected, Is.SameAs(y.gameObject));

            // 上次選取的元件已經不能用時，改選預設元件。
            b.interactable = false;
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            yield return Frames();
            Assert.That(Selected, Is.SameAs(a.gameObject));
            Assert.That(selections, Is.EqualTo(new[] { "home:a", "home:b", "home:b", "next:y", "home:a" }));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SelectionReturnsToEachViewAfterPopupsAndBack()
        {
            UseInput();
            CreateRoot();
            root.routes.Add(new UIRoute
                { source = "home", action = "openPopup", kind = UIRouteKind.OpenPopup, target = "popup" });
            AddNavButton(first, "a", new Vector2(0, 60));
            var b = AddNavButton(first, "b", Vector2.zero);
            b.gameObject.AddComponent<UIButtonAction>().action = "openPopup";
            var x = AddNavButton(next, "x", new Vector2(0, 60));
            var y = AddNavButton(next, "y", Vector2.zero);
            AddNavButton(popupTemplate, "ok", new Vector2(0, 60));
            popupTemplate.defaultSelected = AddNavButton(popupTemplate, "cancel", Vector2.zero);
            yield return StartPageWithInput();

            yield return Tap(gamepad.dpad.down);
            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(b.gameObject));

            // 用確定鍵開啟 Popup：選取移到 Popup 的預設元件。
            yield return Tap(gamepad.buttonSouth);
            Assert.That(root.CurrentPopupId, Is.EqualTo("popup"));
            Assert.That(Selected, Is.SameAs(root.TopPopup.Get<Button>("cancel").gameObject));
            yield return Tap(gamepad.dpad.up);
            Assert.That(Selected, Is.SameAs(root.TopPopup.Get<Button>("ok").gameObject));

            // 關閉之後回到頁面上次的選取。
            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return Frames();
            Assert.That(Selected, Is.SameAs(b.gameObject));

            // 往前切頁從預設元件開始；返回時恢復離開前的選取。
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Frames();
            Assert.That(Selected, Is.SameAs(x.gameObject));
            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(y.gameObject));
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            yield return Frames();
            Assert.That(Selected, Is.SameAs(b.gameObject));
            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Frames();
            Assert.That(Selected, Is.SameAs(x.gameObject), "再次往前進入時不沿用上次的選取");
        }

        [UnityTest]
        public IEnumerator PointerUseLeavesNothingSelectedExceptAnInputFieldBeingEdited()
        {
            UseInput();
            CreateRoot();
            var a = AddNavButton(first, "a", new Vector2(0, 60));
            var fieldObject = AddWidget(first, "name", "InputField");
            Place(fieldObject, new Vector2(0, -40), new Vector2(200, 40));
            var field = fieldObject.GetComponent<TMP_InputField>();
            var clicks = 0;
            a.onClick.AddListener(() => clicks++);
            yield return StartPageWithInput();

            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(a.gameObject));

            // 滑鼠移動後選取被清除。
            yield return MoveMouse(new Vector2(12, 12));
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));
            Assert.That(Selected, Is.Null);

            // 滑鼠點擊按鈕後不留選取。
            yield return Click(a);
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(Selected, Is.Null);

            // 點輸入框進入編輯：選取保持，鍵盤的方向鍵與手把都不會切換模式。
            yield return Click(field);
            yield return Frames(4);
            Assert.That(field.isFocused, Is.True);
            Assert.That(Selected, Is.SameAs(field.gameObject));
            yield return Tap(keyboard.downArrowKey);
            yield return Tap(gamepad.dpad.up);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));
            Assert.That(Selected, Is.SameAs(field.gameObject));
            Assert.That(field.isFocused, Is.True);

            // 結束編輯之後，輸入框也不留在選取狀態。
            field.DeactivateInputField();
            yield return Frames(5);
            Assert.That(field.isFocused, Is.False);
            Assert.That(Selected, Is.Null);
        }

        [UnityTest]
        public IEnumerator NavigationStaysInsideTheTopPopup()
        {
            UseInput();
            CreateRoot();
            AddNavButton(first, "up", new Vector2(0, 100));
            AddNavButton(first, "down", new Vector2(0, -100));
            AddNavButton(first, "left", new Vector2(-200, 0));
            AddNavButton(first, "right", new Vector2(200, 0));
            AddNavButton(popupTemplate, "only", Vector2.zero);
            yield return StartPageWithInput();
            yield return Expect(root.OpenPopupAsync("popup"), UINavResult.Completed);
            var only = root.TopPopup.Get<Button>("only").gameObject;

            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(only));
            foreach (var direction in new[]
                { gamepad.dpad.up, gamepad.dpad.down, gamepad.dpad.left, gamepad.dpad.right })
            {
                yield return Tap(direction);
                Assert.That(Selected, Is.SameAs(only), direction.name);
            }
            foreach (var key in new[]
                { keyboard.upArrowKey, keyboard.downArrowKey, keyboard.leftArrowKey, keyboard.rightArrowKey })
            {
                yield return Tap(key);
                Assert.That(Selected, Is.SameAs(only), key.name);
            }

            yield return Expect(root.ClosePopupAsync(), UINavResult.Completed);
            yield return Frames();
            Assert.That(Selected, Is.Not.Null);
            Assert.That(Selected.transform.IsChildOf(first.transform), Is.True);
        }

        [UnityTest]
        public IEnumerator NavigatingToAListItemOutsideTheViewportScrollsItIntoView()
        {
            UseInput();
            CreateRoot();
            var listObject = AddWidget(first, "saves", "ScrollList");
            Place(listObject, Vector2.zero, new Vector2(260, 150));
            var list = listObject.GetComponent<UIList>();
            yield return StartPageWithInput();
            list.SetCount(8, (item, index) => UIList.GetInItem<TMP_Text>(item, "label").text = $"Save {index}");
            Canvas.ForceUpdateCanvases();
            var viewport = list.GetComponent<ScrollRect>().viewport;
            Assert.That(IsInside(viewport, list.Items[5]), Is.False);
            Assert.That(list.Content.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));

            yield return Tap(gamepad.dpad.down);
            Assert.That(Selected, Is.SameAs(list.Items[0]));
            for (var index = 1; index <= 5; index++)
            {
                yield return Tap(gamepad.dpad.down);
                Assert.That(Selected, Is.SameAs(list.Items[index]), index.ToString());
                Assert.That(IsInside(viewport, list.Items[index]), Is.True, index.ToString());
            }
            Assert.That(list.Content.anchoredPosition.y, Is.GreaterThan(1f));

            // 往回走也會捲回去。
            for (var index = 4; index >= 0; index--)
            {
                yield return Tap(gamepad.dpad.up);
                Assert.That(Selected, Is.SameAs(list.Items[index]), index.ToString());
                Assert.That(IsInside(viewport, list.Items[index]), Is.True, index.ToString());
            }
            Assert.That(list.Content.anchoredPosition.y, Is.EqualTo(0f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator MissingInputModuleIsReportedOnceAndOnlyDisablesNavigation()
        {
            CreateRoot();
            Object.DestroyImmediate(eventSystemObject);
            LogAssert.Expect(LogType.Error, new Regex("Input.*找不到啟用中的 EventSystem 與 InputSystemUIInputModule"));
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Frames(4);

            yield return Expect(root.ShowPageAsync("next"), UINavResult.Completed);
            yield return Expect(root.BackAsync(), UINavResult.Completed);
            Assert.That(root.InputMode, Is.EqualTo(UIInputMode.Pointer));
            LogAssert.NoUnexpectedReceived();
        }

        private static bool IsInside(RectTransform viewport, GameObject item)
        {
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                viewport, (RectTransform)item.transform);
            var rect = viewport.rect;
            return bounds.min.x >= rect.xMin - 0.5f && bounds.max.x <= rect.xMax + 0.5f &&
                bounds.min.y >= rect.yMin - 0.5f && bounds.max.y <= rect.yMax + 0.5f;
        }
    }
}
