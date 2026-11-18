using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace UniGameUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("UniGameUI/UI View")]
    public sealed class UIView : MonoBehaviour
    {
        [SerializeField, HideInInspector] internal string id;
        [SerializeField, HideInInspector] internal RectTransform motion;
        [SerializeField, HideInInspector] internal RectTransform content;
        [SerializeField, HideInInspector] internal CanvasGroup canvasGroup;
        [SerializeField] internal UITransition enter = new UITransition();
        [SerializeField] internal UITransition exit = new UITransition();
        [SerializeField] private UnityEvent showing = new UnityEvent();
        [SerializeField] private UnityEvent showed = new UnityEvent();
        [SerializeField] private UnityEvent hiding = new UnityEvent();
        [SerializeField] private UnityEvent hidden = new UnityEvent();

        public string Id => id;
        public RectTransform Motion => motion;
        public RectTransform Content => content;
        public CanvasGroup Group => canvasGroup;
        public UITransition Enter => enter;
        public UITransition Exit => exit;
        public UnityEvent Showing => showing;
        public UnityEvent Showed => showed;
        public UnityEvent Hiding => hiding;
        public UnityEvent Hidden => hidden;
        public object Data { get; internal set; }

        internal async Awaitable ShowAsync(object data, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            Data = data;
            SetInput(false);
            gameObject.SetActive(true);
            Invoke(showing);
            await UITransition.PlayAsync(this, enter, true, cancellation);
        }

        internal async Awaitable HideAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            SetInput(false);
            Invoke(hiding);
            await UITransition.PlayAsync(this, exit, false, cancellation);
        }

        internal void FinishShowing()
        {
            Invoke(showed);
        }

        internal void FinishHiding()
        {
            gameObject.SetActive(false);
            Invoke(hidden);
        }

        internal void SetInput(bool enabled)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        private void Invoke(UnityEvent callback)
        {
            try
            {
                callback.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
