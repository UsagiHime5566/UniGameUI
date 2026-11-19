using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI
{
    /// <summary>
    /// 進度條。用一張 Image Type 為 Filled 的圖片顯示 0～1 的進度。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI Progress Bar")]
    public sealed class UIProgressBar : MonoBehaviour
    {
        [SerializeField] internal Image fill;
        [SerializeField, Range(0f, 1f)] internal float value;

        public Image Fill => fill;

        /// <summary>
        /// 目前的進度，範圍 0～1。超出範圍的值會被夾住。
        /// </summary>
        public float Value
        {
            get => value;
            set
            {
                this.value = float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
                Refresh();
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Refresh();
        }
#endif

        private void Refresh()
        {
            if (fill)
                fill.fillAmount = value;
        }
    }
}
