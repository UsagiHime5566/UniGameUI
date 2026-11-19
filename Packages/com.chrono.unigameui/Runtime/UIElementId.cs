using UnityEngine;

namespace UniGameUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI Element Id")]
    public sealed class UIElementId : MonoBehaviour
    {
        [SerializeField] internal string id;

        public string Id => id;
    }
}
