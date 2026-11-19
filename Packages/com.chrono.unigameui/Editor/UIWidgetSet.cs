using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UniGameUI.Editor
{
    /// <summary>
    /// 元件的行為類別，決定 JSON 計畫能對它寫哪些欄位。
    /// </summary>
    public enum UIWidgetKind
    {
        Custom, Text, Image, Panel, Button, Toggle, Slider, ProgressBar, Dropdown, InputField, ScrollList
    }

    [Serializable]
    public sealed class UIWidgetEntry
    {
        [SerializeField] internal string name;
        [SerializeField] internal UIWidgetKind kind;
        [SerializeField] internal GameObject prefab;

        /// <summary>計畫裡的 kind。區分大小寫，同一個元件集內不可重複。</summary>
        public string Name => name;

        /// <summary>行為類別。內建名稱就是自己；自訂元件是它比照的類別。</summary>
        public UIWidgetKind Kind => kind;

        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// 專案的元件集。一個專案只有一個，位置不限。畫面上的元件都是這裡登記的 Prefab 的實例。
    /// </summary>
    public sealed class UIWidgetSet : ScriptableObject
    {
        [SerializeField] internal TMP_FontAsset defaultFont;
        [SerializeField] internal List<UIWidgetEntry> widgets = new List<UIWidgetEntry>();
        [SerializeField] internal GameObject listItem;
        [SerializeField] internal UIView confirm;

        /// <summary>元件集的預設字型。空的時候使用 TMP 的預設字型。</summary>
        public TMP_FontAsset DefaultFont => defaultFont;

        public IReadOnlyList<UIWidgetEntry> Widgets => widgets;

        /// <summary>起始元件 ScrollList 預設使用的清單項目 Prefab。</summary>
        public GameObject ListItem => listItem;

        /// <summary>起始的確認框 Prefab。</summary>
        public UIView Confirm => confirm;

        public UIWidgetEntry Find(string name)
        {
            return widgets.Find(entry => entry != null && entry.name == name);
        }
    }
}
