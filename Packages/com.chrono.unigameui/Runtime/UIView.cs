using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("showed")] private UnityEvent shown = new UnityEvent();
        [SerializeField] private UnityEvent hiding = new UnityEvent();
        [SerializeField] private UnityEvent hidden = new UnityEvent();

        public string Id => id;
        public RectTransform Motion => motion;
        public RectTransform Content => content;
        public CanvasGroup Group => canvasGroup;
        public UITransition Enter => enter;
        public UITransition Exit => exit;
        public UnityEvent Showing => showing;
        public UnityEvent Shown => shown;
        public UnityEvent Hiding => hiding;
        public UnityEvent Hidden => hidden;
        public object Data { get; internal set; }

        internal async Awaitable ShowAsync(object data, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            Data = data;
            SetInput(false);
            gameObject.SetActive(true);
            showing.Invoke();
            await UITransition.PlayAsync(this, enter, true, cancellation);
        }

        internal async Awaitable HideAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            SetInput(false);
            hiding.Invoke();
            await UITransition.PlayAsync(this, exit, false, cancellation);
        }

        internal void FinishShowing()
        {
            shown.Invoke();
        }

        internal void FinishHiding()
        {
            gameObject.SetActive(false);
            hidden.Invoke();
        }

        internal void SetInput(bool enabled)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

    }
}
