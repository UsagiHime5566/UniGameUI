using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("UniGameUI.Editor")]
[assembly: InternalsVisibleTo("UniGameUI.Editor.Tests")]
[assembly: InternalsVisibleTo("UniGameUI.Runtime.Tests")]

namespace UniGameUI
{
    public enum UIAuthoringMode { SingleScene, Prefab }
    public enum UIPageTransitionMode { Sequential, Concurrent }
    public enum UIEntryKind { Page, Popup }
    public enum UIRouteKind { ShowPage, OpenPopup, Back, ClosePopup, Emit }
    public enum UINavResult { Completed, Busy, Canceled, Unavailable, NotFound, Failed }
    public enum UIHistoryMode { Push, Replace, Clear }

    [Serializable]
    public sealed class UIEntry
    {
        [SerializeField] internal string id;
        [SerializeField] internal string title;
        [SerializeField] internal UIEntryKind kind;
        [SerializeField] internal UIView review;
        [SerializeField] internal UIView prefab;
        [SerializeField] internal Vector2 graphPosition;
        [SerializeField] internal bool navigatedByCode;

        public string Id => id;
        public string Title => title;
        public UIEntryKind Kind => kind;
        public UIView Review => review;
        public UIView Prefab => prefab;
        public Vector2 GraphPosition => graphPosition;

        /// <summary>
        /// 這個畫面在藍圖上標記為「由程式控制進出」，不靠路由開啟或關閉。
        /// 只影響 Editor 的 Validate 與走訪，執行期的行為不變。
        /// </summary>
        public bool NavigatedByCode => navigatedByCode;
    }

    [Serializable]
    public sealed class UIRoute
    {
        [SerializeField] internal string source;
        [SerializeField] internal string action;
        [SerializeField] internal UIRouteKind kind;
        [SerializeField] internal string target;
        [SerializeField] internal UIHistoryMode history;
        [SerializeField] internal string result;

        public string Source => source;
        public string Action => action;
        public UIRouteKind Kind => kind;
        public string Target => target;
        public UIHistoryMode History => history;
        public string Result => result;

        public static bool TargetsEntry(UIRouteKind kind)
        {
            return kind == UIRouteKind.ShowPage || kind == UIRouteKind.OpenPopup;
        }
    }
}
