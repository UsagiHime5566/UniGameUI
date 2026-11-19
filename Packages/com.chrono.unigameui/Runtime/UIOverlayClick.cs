using UnityEngine;
using UnityEngine.EventSystems;

namespace UniGameUI
{
    /// <summary>
    /// 由工作區在執行期加到 Popup 的遮罩上，把點擊遮罩轉成關閉 Popup。
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class UIOverlayClick : MonoBehaviour, IPointerClickHandler
    {
        internal UIView view;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !view || !view.ClosesOnOverlayClick)
                return;
            var root = view.Root;
            if (!root || root.TopPopup != view || root.IsTransitioning)
                return;
            // 內容區域沒有接收點擊的圖形時，點擊會穿透到遮罩。落在內容範圍內的不算點遮罩。
            if (view.Content && RectTransformUtility.RectangleContainsScreenPoint(
                    view.Content, eventData.position, eventData.pressEventCamera))
                return;
            Close();
        }

        private async void Close()
        {
            try
            {
                await view.Close();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
