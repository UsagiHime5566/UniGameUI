using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI
{
    /// <summary>
    /// <see cref="UIList"/> 內容物件使用的版面配置。排列方向由清單在執行期設定，
    /// 邊距、間距與對齊方式照一般的 Layout Group 在 Inspector 調整。
    /// </summary>
    [AddComponentMenu("UniGameUI/UI List Layout")]
    public sealed class UIListLayout : HorizontalOrVerticalLayoutGroup
    {
        [SerializeField] private bool vertical = true;

        public bool Vertical => vertical;

        /// <summary>
        /// 設定排列方向。排列方向上保留項目自己的尺寸，另一個方向撐滿內容物件。
        /// </summary>
        public void SetVertical(bool value)
        {
            vertical = value;
            m_ChildControlWidth = value;
            m_ChildForceExpandWidth = value;
            m_ChildControlHeight = !value;
            m_ChildForceExpandHeight = !value;
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, vertical);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, vertical);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, vertical);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, vertical);
        }
    }
}
