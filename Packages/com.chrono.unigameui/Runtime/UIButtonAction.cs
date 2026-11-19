using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("UniGameUI/UI Button Action")]
    public sealed class UIButtonAction : MonoBehaviour
    {
        [SerializeField] internal string action;
        private Button button;

        public string Action => action;

        private void OnEnable()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Dispatch);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Dispatch);
        }

        private async void Dispatch()
        {
            var view = GetComponentInParent<UIView>();
            var root = GetComponentInParent<UIRoot>();
            if (!view || !root)
            {
                var missing = !view ? "UIView" : "UIRoot";
                Debug.LogError($"UniGameUI: UIButtonAction '{(view ? view.Id : name)}'/'{action}' " +
                    $"找不到上層的 {missing}。（Scene: {gameObject.scene.name}）",
                    root ? (Object)root : this);
                return;
            }
            await root.TriggerAsync(view.Id, action);
        }
    }
}
