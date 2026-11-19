using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("UniGameUI.Editor")]
[assembly: InternalsVisibleTo("UniGameUI.Runtime.Tests")]

namespace UniGameUI
{
    public enum UIAuthoringMode { SingleScene, Prefab }
    public enum UIPageTransitionMode { Sequential, Concurrent }
    public enum UIEntryKind { Page, Popup }
    public enum UIRouteKind { ShowPage, OpenPopup, Back, ClosePopup, Emit }
    public enum UINavResult { Completed, Busy, Canceled, Unavailable, NotFound, Failed }

    [Serializable]
    public sealed class UIEntry
    {
        [SerializeField] internal string id;
        [SerializeField] internal string title;
        [SerializeField] internal UIEntryKind kind;
        [SerializeField] internal UIView review;
        [SerializeField] internal UIView prefab;
        [SerializeField] internal Vector2 graphPosition;

        public string Id => id;
        public string Title => title;
        public UIEntryKind Kind => kind;
        public UIView Review => review;
        public UIView Prefab => prefab;
        public Vector2 GraphPosition => graphPosition;
    }

    [Serializable]
    public sealed class UIRoute
    {
        [SerializeField] internal string source;
        [SerializeField] internal string action;
        [SerializeField] internal UIRouteKind kind;
        [SerializeField] internal string target;

        public string Source => source;
        public string Action => action;
        public UIRouteKind Kind => kind;
        public string Target => target;

        public static bool TargetsEntry(UIRouteKind kind)
        {
            return kind == UIRouteKind.ShowPage || kind == UIRouteKind.OpenPopup;
        }
    }
}
