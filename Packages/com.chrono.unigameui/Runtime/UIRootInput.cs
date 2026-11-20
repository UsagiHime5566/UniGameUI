using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UniGameUI
{
    public enum UIInputMode { Pointer, Navigation }

    /// <summary>
    /// 掛在 View 根節點上，在框架處理返回鍵之前先收到通知。回傳 true 表示已經處理，框架不再做事。
    /// </summary>
    public interface IUIBackHandler
    {
        bool OnBackRequested(UIView view);
    }

    // 輸入模式、手把與鍵盤的選取，以及返回鍵。輸入一律讀 InputSystemUIInputModule 上設定的動作。
    public sealed partial class UIRoot
    {
        private const int MoveSlot = 0;
        private const int SubmitSlot = 1;
        private const int CancelSlot = 2;
        private const int PointSlot = 3;
        private const int LeftClickSlot = 4;
        private const int RightClickSlot = 5;
        private const int MiddleClickSlot = 6;
        private const int ScrollSlot = 7;
        private const int SlotCount = 8;

        // 滑鼠點到輸入框之後，TMP 要到下一次更新才進入編輯狀態，這段期間不要清掉選取。
        private const int InputFieldFocusFrames = 2;

        private static readonly List<Selectable> SelectableBuffer = new List<Selectable>();
        private static readonly List<TMP_Dropdown> DropdownBuffer = new List<TMP_Dropdown>();

        [SerializeField] private bool handlesBackKey = true;

        private readonly InputAction[] hookedActions = new InputAction[SlotCount];
        private readonly bool[] pressedButtons = new bool[SlotCount];
        private Action<InputAction.CallbackContext>[] inputHandlers;
        private EventSystem hookedEventSystem;
        private InputSystemUIInputModule inputModule;
        private bool reportedMissingInput;
        private UIInputMode inputMode;
        private UIInputMode? requestedMode;
        private int requestedModeFrame;
        private bool backRequested;
        private Vector2 lastPointerPosition = new Vector2(float.NaN, float.NaN);
        private GameObject observedSelection;
        private int observedSelectionFrame;
        private GameObject reportedSelection;

        /// <summary>
        /// 是否把取消動作（Esc 與手把的取消鍵）當成返回。預設開啟。
        /// </summary>
        public bool HandlesBackKey
        {
            get => handlesBackKey;
            set => handlesBackKey = value;
        }

        /// <summary>
        /// 最後使用的輸入方式。起始為 <see cref="UIInputMode.Pointer"/>。
        /// </summary>
        public UIInputMode InputMode => inputMode;

        public event Action<UIInputMode> InputModeChanged;

        /// <summary>
        /// 在 <see cref="UIInputMode.Navigation"/> 模式下，選取的元件改變時觸發，帶所屬的 View 與被選取的物件。
        /// </summary>
        public event Action<UIView, GameObject> ElementSelected;

        /// <summary>
        /// 按下返回鍵、但沒有開著的 Popup 也沒有上一頁時觸發。
        /// </summary>
        public event Action BackRequestedAtRoot;

        private UIView TopView => TopPopup ? TopPopup : CurrentPage;

        private void LateUpdate()
        {
            if (!ready)
                return;

            var eventSystem = EventSystem.current;
            if (!SyncInput(eventSystem))
            {
                requestedMode = null;
                backRequested = false;
                return;
            }

            // 切到導覽模式的那一次輸入只用來建立選取。等輸入模組處理完那次輸入之後才選取，
            // 它就不會同時被當成移動或確定。
            if (requestedMode is { } mode &&
                (mode == UIInputMode.Pointer || Time.frameCount > requestedModeFrame))
            {
                requestedMode = null;
                SetInputMode(mode, eventSystem);
            }
            if (backRequested)
            {
                backRequested = false;
                HandleBackKey();
            }
            UpdateSelection(eventSystem);
        }

        private bool SyncInput(EventSystem eventSystem)
        {
            if (eventSystem != hookedEventSystem || !inputModule)
            {
                hookedEventSystem = eventSystem;
                inputModule = eventSystem ? eventSystem.GetComponent<InputSystemUIInputModule>() : null;
            }
            if (!inputModule || !inputModule.isActiveAndEnabled)
            {
                UnhookInput();
                if (!reportedMissingInput)
                {
                    reportedMissingInput = true;
                    Debug.LogError(Diagnostic("Input", "找不到啟用中的 EventSystem 與 InputSystemUIInputModule，" +
                        "手把與鍵盤導覽和返回鍵已停用。"), this);
                }
                return false;
            }

            inputHandlers ??= new Action<InputAction.CallbackContext>[]
            {
                OnMoveInput, OnSubmitInput, OnCancelInput, OnPointInput,
                context => OnClickInput(LeftClickSlot, context),
                context => OnClickInput(RightClickSlot, context),
                context => OnClickInput(MiddleClickSlot, context),
                OnScrollInput
            };
            // 專案可以隨時換掉模組上的動作，所以每個影格比對一次。
            Hook(MoveSlot, inputModule.move);
            Hook(SubmitSlot, inputModule.submit);
            Hook(CancelSlot, inputModule.cancel);
            Hook(PointSlot, inputModule.point);
            Hook(LeftClickSlot, inputModule.leftClick);
            Hook(RightClickSlot, inputModule.rightClick);
            Hook(MiddleClickSlot, inputModule.middleClick);
            Hook(ScrollSlot, inputModule.scrollWheel);
            return true;
        }

        private void Hook(int slot, InputActionReference reference)
        {
            var action = reference ? reference.action : null;
            var hooked = hookedActions[slot];
            if (hooked == action)
                return;

            var handler = inputHandlers[slot];
            var tracksRelease = slot >= LeftClickSlot && slot <= MiddleClickSlot;
            if (hooked != null)
            {
                hooked.performed -= handler;
                if (tracksRelease)
                    hooked.canceled -= handler;
            }
            hookedActions[slot] = action;
            pressedButtons[slot] = false;
            if (action == null)
                return;
            action.performed += handler;
            if (tracksRelease)
                action.canceled += handler;
        }

        private void UnhookInput()
        {
            if (inputHandlers == null)
                return;
            for (var slot = 0; slot < SlotCount; slot++)
            {
                var hooked = hookedActions[slot];
                if (hooked != null)
                {
                    hooked.performed -= inputHandlers[slot];
                    hooked.canceled -= inputHandlers[slot];
                }
                hookedActions[slot] = null;
                pressedButtons[slot] = false;
            }
        }

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            RequestMode(UIInputMode.Navigation);
        }

        private void OnSubmitInput(InputAction.CallbackContext context)
        {
            if (IsNavigationDevice(context))
                RequestMode(UIInputMode.Navigation);
        }

        private void OnCancelInput(InputAction.CallbackContext context)
        {
            if (IsNavigationDevice(context))
                RequestMode(UIInputMode.Navigation);
            // 輸入模組稍後才會把取消鍵送給選取中的物件。輸入框與下拉選單會自己處理，
            // 所以要在它們還沒收起來之前判斷。
            if (!IsEditingText() && !HasExpandedDropdown())
                backRequested = true;
        }

        private void OnPointInput(InputAction.CallbackContext context)
        {
            if (context.valueType != typeof(Vector2))
                return;
            var position = context.ReadValue<Vector2>();
            if (position == lastPointerPosition)
                return;
            lastPointerPosition = position;
            RequestMode(UIInputMode.Pointer);
        }

        private void OnClickInput(int slot, InputAction.CallbackContext context)
        {
            pressedButtons[slot] = !context.canceled && context.ReadValueAsButton();
            RequestMode(UIInputMode.Pointer);
        }

        private void OnScrollInput(InputAction.CallbackContext context)
        {
            RequestMode(UIInputMode.Pointer);
        }

        // 鍵盤的確定與取消不改變模式；滑鼠類裝置綁到這些動作時也一樣。其餘裝置（手把、搖桿）視為導覽輸入。
        private static bool IsNavigationDevice(InputAction.CallbackContext context)
        {
            var device = context.control?.device;
            return device != null && device is not Keyboard && device is not Pointer;
        }

        private void RequestMode(UIInputMode mode)
        {
            if (IsEditingText())
                return;
            requestedMode = mode;
            requestedModeFrame = Time.frameCount;
        }

        private static bool IsEditingText()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem ? eventSystem.currentSelectedGameObject : null;
            return selected && selected.TryGetComponent<TMP_InputField>(out var field) && field.isFocused;
        }

        private bool HasExpandedDropdown()
        {
            var top = TopView;
            if (!top)
                return false;
            top.GetComponentsInChildren(false, DropdownBuffer);
            var expanded = false;
            foreach (var dropdown in DropdownBuffer)
                expanded |= dropdown.IsExpanded;
            DropdownBuffer.Clear();
            return expanded;
        }

        private void SetInputMode(UIInputMode mode, EventSystem eventSystem)
        {
            if (inputMode == mode)
                return;
            inputMode = mode;
            if (mode == UIInputMode.Pointer)
            {
                eventSystem.SetSelectedGameObject(null);
                reportedSelection = null;
            }
            UIEventDispatch.Invoke(InputModeChanged, listener => listener(mode),
                exception => LogReceiverException($"{nameof(InputModeChanged)} {mode}", exception));
        }

        private void UpdateSelection(EventSystem eventSystem)
        {
            var selected = eventSystem.currentSelectedGameObject;
            if (selected != observedSelection)
            {
                observedSelection = selected;
                observedSelectionFrame = Time.frameCount;
            }

            if (inputMode == UIInputMode.Pointer)
            {
                // 滑鼠點過的元件不留在選取狀態。
                reportedSelection = null;
                var pressed = pressedButtons[LeftClickSlot] || pressedButtons[RightClickSlot] ||
                    pressedButtons[MiddleClickSlot];
                if (selected && !pressed && !KeepsPointerSelection(selected))
                {
                    eventSystem.SetSelectedGameObject(null);
                    observedSelection = null;
                }
                return;
            }

            if (busy)
                return;
            var top = TopView;
            if (!top)
                return;
            if (!IsSelectableIn(selected, top))
            {
                selected = IsSelectableIn(top.lastSelected, top) ? top.lastSelected : FindDefaultSelection(top);
                if (eventSystem.currentSelectedGameObject != selected)
                    eventSystem.SetSelectedGameObject(selected);
                observedSelection = selected;
            }
            if (selected == reportedSelection)
                return;

            reportedSelection = selected;
            if (!selected)
                return;
            if (!IsDropdownListItem(selected))
                top.lastSelected = selected;
            var list = selected.GetComponentInParent<UIList>();
            if (list && list.OwnsItem(selected.transform))
                list.EnsureVisible((RectTransform)selected.transform);
            UIEventDispatch.Invoke(ElementSelected, listener => listener(top, selected),
                exception => LogReceiverException($"{nameof(ElementSelected)} '{top.Id}'", exception));
        }

        private bool KeepsPointerSelection(GameObject selected)
        {
            if (selected.TryGetComponent<TMP_InputField>(out var field))
                return field.isFocused || Time.frameCount - observedSelectionFrame <= InputFieldFocusFrames;
            var dropdown = selected.GetComponentInParent<TMP_Dropdown>();
            return dropdown && dropdown.IsExpanded;
        }

        // 展開中的下拉選單，選項是暫時建立的物件，不當成畫面記住的選取。
        private static bool IsDropdownListItem(GameObject selected)
        {
            var dropdown = selected.GetComponentInParent<TMP_Dropdown>();
            return dropdown && dropdown.gameObject != selected && dropdown.IsExpanded;
        }

        private static bool IsSelectableIn(GameObject candidate, UIView view)
        {
            if (!candidate || !candidate.activeInHierarchy || !candidate.transform.IsChildOf(view.transform))
                return false;
            return !candidate.TryGetComponent<Selectable>(out var selectable) ||
                (selectable.isActiveAndEnabled && selectable.IsInteractable());
        }

        private static GameObject FindDefaultSelection(UIView view)
        {
            var preferred = view.DefaultSelected;
            if (preferred && IsSelectableIn(preferred.gameObject, view))
                return preferred.gameObject;

            GameObject first = null;
            view.GetComponentsInChildren(false, SelectableBuffer);
            foreach (var selectable in SelectableBuffer)
            {
                if (!selectable.isActiveAndEnabled || !selectable.IsInteractable() ||
                    selectable.navigation.mode == Navigation.Mode.None)
                    continue;
                first = selectable.gameObject;
                break;
            }
            SelectableBuffer.Clear();
            return first;
        }

        private void HandleBackKey()
        {
            if (!handlesBackKey || busy)
                return;

            var top = TopView;
            if (top)
            {
                foreach (var handler in top.GetComponents<IUIBackHandler>())
                {
                    if (handler is not UnityEngine.Object component || !component)
                        continue;
                    try
                    {
                        if (handler.OnBackRequested(top))
                            return;
                    }
                    catch (Exception exception)
                    {
                        // 出錯當成沒有處理，繼續往下判斷：不能因為一個例外讓玩家離不開畫面。
                        LogNavigationException($"Back '{top.Id}' {nameof(IUIBackHandler.OnBackRequested)}",
                            "接收者丟出例外，當成沒有處理。", exception);
                    }
                }
                // 不可返回的 Popup 在最上層時返回鍵無效；上面的 OnBackRequested 仍然先被呼叫過了。
                if (!top || top.BlocksBackKey || unreturnablePopups.Contains(top))
                    return;
            }
            if (busy)
                return;

            if (popups.Count == 0 && history.Count == 0)
            {
                UIEventDispatch.Invoke(BackRequestedAtRoot, listener => listener(),
                    exception => LogReceiverException(nameof(BackRequestedAtRoot), exception));
                return;
            }
            BackFromKey();
        }

        private async void BackFromKey()
        {
            try
            {
                await BackAsync(default, true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
