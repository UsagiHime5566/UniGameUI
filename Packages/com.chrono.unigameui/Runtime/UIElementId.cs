using UnityEngine;

namespace UniGameUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class UIElementId : MonoBehaviour
    {
        [SerializeField] internal string id;

        public string Id => id;
    }
}
