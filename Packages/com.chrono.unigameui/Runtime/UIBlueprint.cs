using System;
using System.Collections.Generic;
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
        /// 這個畫面在藍圖上標記為「由程式開啟」：進來的方式不靠路由。頁面標記之後照樣要有自己的出口路由。
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
        // 藍圖上這條線依序經過的中繼點（畫布座標）。只影響藍圖怎麼畫，執行期、Validate、走訪都不使用。
        [SerializeField, HideInInspector] internal List<Vector2> waypoints = new List<Vector2>();

        /// <summary>
        /// 藍圖上這條線依序經過的中繼點（藍圖畫布的座標）。只影響藍圖怎麼畫。
        /// 用計畫的 <c>waypoints</c> 或 Editor 的 <c>UIAuthoring.SetWaypoints</c> 設定。
        /// </summary>
        public IReadOnlyList<Vector2> Waypoints => WaypointList;

        // 舊的 Scene 沒有這個欄位時就是沒有中繼點。
        internal List<Vector2> WaypointList => waypoints ??= new List<Vector2>();

        /// <summary>來源畫面的 ID。<see cref="UIRoot.AnySource"/> 表示這是「任何畫面」的訊號路由。</summary>
        public string Source => source;
        public string Action => action;
        public UIRouteKind Kind => kind;
        public string Target => target;
        /// <summary>
        /// 跳過去之後能不能返回。ShowPage 可以是 Push、Replace、Clear；OpenPopup 可以是 Push 或 Clear，
        /// Clear 開啟的是不可返回的 Popup。
        /// </summary>
        public UIHistoryMode History => history;

        /// <summary>這是不是「任何畫面」的訊號路由。</summary>
        public bool IsSignal => source == UIRoot.AnySource;
        public string Result => result;

        public static bool TargetsEntry(UIRouteKind kind)
        {
            return kind == UIRouteKind.ShowPage || kind == UIRouteKind.OpenPopup;
        }
    }
}
