using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniGameUI.Editor
{
    /// <summary>節點上的一個執行中標示：文字與徽章的樣式。</summary>
    internal readonly struct UIBlueprintLiveMarker
    {
        public UIBlueprintLiveMarker(string text, string className)
        {
            Text = text;
            ClassName = className;
        }

        public string Text { get; }
        public string ClassName { get; }
    }

    /// <summary>
    /// 執行中的工作區在某一刻的狀態。藍圖視窗用它標示節點、填右欄的「執行中」區塊。
    /// 只是資料，可以在測試裡直接組出來。
    /// </summary>
    internal sealed class UIBlueprintLiveState
    {
        public string CurrentPage;
        /// <summary>開著的 Popup，由下到上。</summary>
        public readonly List<string> Popups = new List<string>();
        /// <summary>返回歷史，由近到遠。</summary>
        public readonly List<string> History = new List<string>();
        public bool Transitioning;
        public UIInputMode InputMode;
        /// <summary>正在進場的畫面。</summary>
        public readonly HashSet<string> Entering = new HashSet<string>();
        /// <summary>正在退場的畫面。</summary>
        public readonly HashSet<string> Leaving = new HashSet<string>();

        /// <summary>目前可以操作的畫面：最上層的 Popup，沒有 Popup 時是目前的頁面。</summary>
        public string Top => Popups.Count > 0 ? Popups[Popups.Count - 1] : CurrentPage;

        public static UIBlueprintLiveState Capture(UIRoot root, IEnumerable<string> entering,
            IEnumerable<string> leaving)
        {
            var state = new UIBlueprintLiveState
            {
                CurrentPage = root.CurrentPageId,
                Transitioning = root.IsTransitioning,
                InputMode = root.InputMode,
            };
            foreach (var popup in root.Popups)
            {
                if (popup)
                    state.Popups.Add(popup.Id);
            }
            var history = root.HistoryIds;
            for (var index = history.Count - 1; index >= 0; index--)
                state.History.Add(history[index]);
            if (state.Transitioning)
            {
                state.Entering.UnionWith(entering);
                state.Leaving.UnionWith(leaving);
            }
            return state;
        }

        /// <summary>節點的外框樣式：目前的頁面、開著的 Popup、最上層、轉場中。沒有任何一種時是空的。</summary>
        public IReadOnlyList<string> NodeClasses(string id)
        {
            var classes = new List<string>();
            if (string.IsNullOrEmpty(id))
                return classes;
            if (id == CurrentPage)
                classes.Add("live-current");
            if (Popups.Contains(id))
                classes.Add("live-popup");
            if (id == Top)
                classes.Add("live-top");
            if (Entering.Contains(id) || Leaving.Contains(id))
                classes.Add("live-transition");
            return classes;
        }

        /// <summary>節點上方的徽章：「目前」、「Popup 1」、「最上層」、「進場中」、「退場中」。</summary>
        public IReadOnlyList<UIBlueprintLiveMarker> Markers(string id)
        {
            var markers = new List<UIBlueprintLiveMarker>();
            if (string.IsNullOrEmpty(id))
                return markers;
            if (id == CurrentPage)
                markers.Add(new UIBlueprintLiveMarker("目前", "badge-live-current"));
            var popup = Popups.IndexOf(id);
            if (popup >= 0)
                markers.Add(new UIBlueprintLiveMarker($"Popup {popup + 1}", "badge-live-popup"));
            // 頁面上面蓋著 Popup 時，可以操作的是最上層的 Popup。
            if (popup >= 0 && id == Top)
                markers.Add(new UIBlueprintLiveMarker("最上層", "badge-live-top"));
            if (Entering.Contains(id))
                markers.Add(new UIBlueprintLiveMarker("進場中", "badge-live-transition"));
            else if (Leaving.Contains(id))
                markers.Add(new UIBlueprintLiveMarker("退場中", "badge-live-transition"));
            return markers;
        }

        public static string InputModeLabel(UIInputMode mode)
        {
            return mode == UIInputMode.Navigation ? "導覽（鍵盤或手把）" : "滑鼠";
        }
    }

    /// <summary>「最近的導航」的一筆。</summary>
    internal sealed class UIBlueprintNavRecord
    {
        public DateTime Time;
        /// <summary>來源：畫面的名稱、「任何畫面」或「程式」。</summary>
        public string Source;
        /// <summary>動作：路由的動作名稱，程式直接呼叫時是行為（切頁、開啟 Popup…）。</summary>
        public string Action;
        /// <summary>行為：路由的種類，程式直接呼叫時是對應的種類。</summary>
        public UIRouteKind Kind;
        /// <summary>目的地的名稱。沒有目的地的行為（返回、送出事件）時是 null。</summary>
        public string Target;
        public UINavResult Result;
        /// <summary>走的路由。程式直接呼叫時是 null。</summary>
        public UIRoute Route;
        /// <summary>程式直接呼叫時的目的地畫面 ID。</summary>
        public string TargetId;

        public bool IsProblem => Result != UINavResult.Completed;
        public bool ByCode => Route == null;
    }

    /// <summary>「最近的導航」：最多留 <see cref="Capacity"/> 筆，新的在前面。</summary>
    internal sealed class UIBlueprintNavLog
    {
        public const int Capacity = 20;
        private readonly List<UIBlueprintNavRecord> records = new List<UIBlueprintNavRecord>();

        public IReadOnlyList<UIBlueprintNavRecord> Records => records;
        public int Count => records.Count;

        public void Add(UIBlueprintNavRecord record)
        {
            records.Insert(0, record);
            if (records.Count > Capacity)
                records.RemoveRange(Capacity, records.Count - Capacity);
        }

        public void Clear()
        {
            records.Clear();
        }
    }

    /// <summary>藍圖上一條線的樣式。</summary>
    internal readonly struct UIBlueprintEdgeStyle
    {
        public UIBlueprintEdgeStyle(Color color, float width, bool dashed)
        {
            Color = color;
            Width = width;
            Dashed = dashed;
        }

        public Color Color { get; }
        public float Width { get; }
        public bool Dashed { get; }
    }

    internal static class UIBlueprintLive
    {
        /// <summary>剛走過的路由亮起來的時間（秒），之後回到原本的樣子。</summary>
        public const double FlashSeconds = 2;
        // 一開始維持最亮的時間，之後才開始變淡。
        private const double FlashHoldSeconds = 0.3;

        /// <summary>
        /// 剛走過的路由有多亮：剛走過時是 1，<see cref="FlashSeconds"/> 秒內淡到 0。
        /// <paramref name="elapsed"/> 是走過之後經過的秒數。
        /// </summary>
        public static float FlashLevel(double elapsed)
        {
            if (elapsed < 0 || elapsed >= FlashSeconds)
                return 0f;
            if (elapsed <= FlashHoldSeconds)
                return 1f;
            return Mathf.Clamp01(1f - (float)((elapsed - FlashHoldSeconds) / (FlashSeconds - FlashHoldSeconds)));
        }
    }
}
