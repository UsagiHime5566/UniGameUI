using UnityEngine;

namespace UniGameUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI Element Id")]
    public sealed class UIElementId : MonoBehaviour
    {
        [SerializeField] internal string id;
        [SerializeField] internal string kind;

        public string Id => id;

        /// <summary>
        /// 建立這個元件時使用的元件集名稱（JSON 計畫的 kind）。手動加上的 ID 沒有這個值。
        /// </summary>
        public string Kind => kind;
    }
}
