using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI
{
    public enum UIListDirection { Vertical, Horizontal }

    /// <summary>
    /// 依資料產生項目的清單。所有項目都會建立，不做虛擬化。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UniGameUI/UI List")]
    public sealed class UIList : MonoBehaviour
    {
        [SerializeField] internal RectTransform content;
        [SerializeField] internal GameObject itemPrefab;
        [SerializeField] internal UIListDirection direction;

        private readonly List<GameObject> pool = new List<GameObject>();
        private readonly List<GameObject> items = new List<GameObject>();
        private IReadOnlyList<GameObject> readOnlyItems;
        private UIListDirection? appliedDirection;

        public RectTransform Content => content;
        public int Count => items.Count;
        public IReadOnlyList<GameObject> Items => readOnlyItems ??= items.AsReadOnly();

        /// <summary>
        /// 項目根節點有 Button 時，點擊項目會觸發這個事件並帶索引。
        /// </summary>
        public event Action<int> ItemClicked;

        /// <summary>
        /// 產生項目用的 Prefab。換成另一個 Prefab 時，已經建立的項目會全部銷毀。
        /// </summary>
        public GameObject ItemPrefab
        {
            get => itemPrefab;
            set
            {
                if (itemPrefab == value)
                    return;
                itemPrefab = value;
                DestroyItems();
            }
        }

        public UIListDirection Direction
        {
            get => direction;
            set
            {
                direction = value;
                ApplyDirection();
            }
        }

        private void OnEnable()
        {
            ApplyDirection();
        }

        public void SetItems<T>(IReadOnlyList<T> data, Action<GameObject, T, int> bind)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (bind == null)
                SetCount(data.Count, null);
            else
                SetCount(data.Count, (item, index) => bind(item, data[index], index));
        }

        public void SetCount(int count, Action<GameObject, int> bind)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "項目數量不可以是負數。");
            if (!content)
                throw new InvalidOperationException($"清單「{name}」沒有設定 Content。");
            if (count > 0 && !itemPrefab)
                throw new InvalidOperationException($"清單「{name}」沒有設定 ItemPrefab。");

            ApplyDirection();
            var view = GetComponentInParent<UIView>(true);
            var root = view && view.Root ? view.Root : GetComponentInParent<UIRoot>(true);
            items.Clear();
            for (var index = 0; index < count; index++)
            {
                var item = index < pool.Count ? pool[index] : null;
                if (!item)
                {
                    item = CreateItem(index);
                    if (index < pool.Count)
                        pool[index] = item;
                    else
                        pool.Add(item);
                }
                item.SetActive(true);
                items.Add(item);
                // 建立或重用項目時取一次多語系的值，之後 bind 仍然可以改寫。
                if (root)
                    root.ApplyLocalization(item.transform, view);
            }
            for (var index = count; index < pool.Count; index++)
            {
                if (pool[index])
                    pool[index].SetActive(false);
            }

            if (bind == null)
                return;
            for (var index = 0; index < items.Count; index++)
                bind(items[index], index);
        }

        public void Clear()
        {
            SetCount(0, null);
        }

        /// <summary>
        /// 在單一項目內依元件 ID 尋找元件。先找登記 ID 的物件本身，再找它的子物件。
        /// </summary>
        public static T GetInItem<T>(GameObject item, string elementId) where T : Component
        {
            if (!item)
                throw new ArgumentNullException(nameof(item));
            foreach (var element in item.GetComponentsInChildren<UIElementId>(true))
            {
                if (element.Id != elementId || IsInsideListItem(element.transform, item.transform))
                    continue;
                var component = element.GetComponentInChildren<T>(true);
                if (component)
                    return component;
            }
            throw new InvalidOperationException(
                $"清單項目「{item.name}」找不到元件 ID「{elementId}」的「{typeof(T)}」。");
        }

        /// <summary>
        /// 判斷物件是不是位於某個清單的項目裡。只檢查 <paramref name="boundary"/> 底下的清單。
        /// </summary>
        internal static bool IsInsideListItem(Transform element, Transform boundary)
        {
            var list = element.GetComponentInParent<UIList>(true);
            while (list && list.transform != boundary && list.transform.IsChildOf(boundary))
            {
                if (list.OwnsItem(element))
                    return true;
                var parent = list.transform.parent;
                list = parent ? parent.GetComponentInParent<UIList>(true) : null;
            }
            return false;
        }

        internal bool OwnsItem(Transform element)
        {
            return content && element != content && element.IsChildOf(content);
        }

        /// <summary>
        /// 把清單捲到看得見指定項目的位置。導覽模式下選取移到項目上時由工作區呼叫。
        /// </summary>
        internal void EnsureVisible(RectTransform target)
        {
            if (!content || !target || !TryGetComponent<ScrollRect>(out var scroll))
                return;

            var viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            var visible = viewport.rect;
            var offset = Vector2.zero;
            if (scroll.vertical)
            {
                if (bounds.max.y > visible.yMax)
                    offset.y = visible.yMax - bounds.max.y;
                else if (bounds.min.y < visible.yMin)
                    offset.y = visible.yMin - bounds.min.y;
            }
            if (scroll.horizontal)
            {
                if (bounds.min.x < visible.xMin)
                    offset.x = visible.xMin - bounds.min.x;
                else if (bounds.max.x > visible.xMax)
                    offset.x = visible.xMax - bounds.max.x;
            }
            if (offset == Vector2.zero)
                return;

            scroll.StopMovement();
            content.position += viewport.TransformVector(offset);
        }

        private GameObject CreateItem(int index)
        {
            var item = Instantiate(itemPrefab, content, false);
            item.name = $"{itemPrefab.name} {index}";
            item.transform.SetSiblingIndex(Mathf.Min(index, content.childCount - 1));
            // 項目依索引重用，所以索引在項目的生命週期內不會改變。
            if (item.TryGetComponent<Button>(out var button))
                button.onClick.AddListener(() => ItemClicked?.Invoke(index));
            return item;
        }

        private void DestroyItems()
        {
            foreach (var item in pool)
            {
                if (item)
                    Destroy(item);
            }
            pool.Clear();
            items.Clear();
        }

        private void ApplyDirection()
        {
            // 專案自己換掉版面配置元件時，清單只負責產生項目，不改動排列設定。
            if (appliedDirection == direction || !content || !content.TryGetComponent<UIListLayout>(out var layout))
                return;

            appliedDirection = direction;
            var vertical = direction == UIListDirection.Vertical;
            layout.SetVertical(vertical);
            if (TryGetComponent<ScrollRect>(out var scroll))
            {
                scroll.vertical = vertical;
                scroll.horizontal = !vertical;
            }
            if (content.TryGetComponent<ContentSizeFitter>(out var fitter))
            {
                fitter.verticalFit = vertical
                    ? ContentSizeFitter.FitMode.PreferredSize
                    : ContentSizeFitter.FitMode.Unconstrained;
                fitter.horizontalFit = vertical
                    ? ContentSizeFitter.FitMode.Unconstrained
                    : ContentSizeFitter.FitMode.PreferredSize;
            }

            content.anchorMin = vertical ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            content.anchorMax = vertical ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            content.pivot = vertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
        }
    }
}
