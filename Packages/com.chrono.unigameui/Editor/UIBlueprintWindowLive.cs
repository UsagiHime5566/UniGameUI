using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor
{
    // 執行中的藍圖：Play Mode 時照常顯示藍圖，標示目前的畫面、開著的 Popup、轉場中的畫面與剛走過的路由。
    // 不能修改藍圖：Play Mode 中的修改離開時會被 Unity 還原。
    // 執行中的資料只從 UIRoot 的公開成員、事件與給 Editor 用的 internal 成員取得。
    // 只有視窗開著、而且在 Play Mode 時才訂閱；離開 Play Mode、換工作區、關閉視窗時都取消。
    public sealed partial class UIBlueprintWindow
    {
        private static readonly Color FlashColor = new Color32(255, 226, 122, 255);
        private static readonly Color EdgeLabelColor = new Color32(37, 45, 51, 255);
        private static readonly string[] LiveNodeClasses = { "live-current", "live-popup", "live-top", "live-transition" };
        // 找不到執行中的工作區時，多久再找一次（秒）。
        private const double WorkspaceSearchInterval = 0.5;
        // 亮起的路由淡掉時，多久重畫一次（秒）。
        private const double FlashFrameInterval = 1.0 / 30.0;

        private bool liveMode;
        // 目前訂閱中的工作區。用 ReferenceEquals 判斷有沒有，因為它可能已經被銷毀。
        private UIRoot liveRoot;
        // 上一次訂閱的工作區：換了工作區才清掉最近的導航。
        private UIRoot lastLiveRoot;
        private UIBlueprintLiveState liveState;
        // 測試用：直接指定的執行中狀態，不必進入 Play Mode。
        private UIBlueprintLiveState injectedLiveState;
        private readonly UIBlueprintNavLog navLog = new UIBlueprintNavLog();
        private readonly HashSet<string> entering = new HashSet<string>();
        private readonly HashSet<string> leaving = new HashSet<string>();
        private readonly Dictionary<UIRoute, double> flashes = new Dictionary<UIRoute, double>();
        private bool liveDirty;
        private bool liveUpdateRegistered;
        private bool liveLost;
        private double nextWorkspaceSearch;
        private double nextFlashFrame;
        private UIRoute pendingTrigger;
        private bool pendingTriggerReported;
        // 上一次檢查時的狀態。有變化才重畫，不必每個影格重建畫面。
        private UIView polledPage;
        private UIView polledTop;
        private int polledPopups;
        private int polledHistory;
        private bool polledBusy;
        private UIInputMode polledInput;
        private bool polledWalk;

        /// <summary>目前是不是執行中的藍圖（唯讀）。</summary>
        internal bool IsLive => liveMode;

        /// <summary>訂閱中的執行中工作區。沒有時是 null。</summary>
        internal UIRoot LiveRoot => ReferenceEquals(liveRoot, null) ? null : liveRoot;

        /// <summary>有沒有掛在 <see cref="EditorApplication.update"/> 上檢查狀態。</summary>
        internal bool IsWatchingLiveState => liveUpdateRegistered;

        /// <summary>最近一次取得的執行中狀態。</summary>
        internal UIBlueprintLiveState LiveState => liveState;

        /// <summary>「最近的導航」，新的在前面。</summary>
        internal IReadOnlyList<UIBlueprintNavRecord> NavigationRecords => navLog.Records;

        /// <summary>
        /// 測試用：不進入 Play Mode，直接用指定的狀態顯示執行中的藍圖。傳入 null 回到可以編輯的藍圖。
        /// </summary>
        internal void ShowLiveStateForTests(UIBlueprintLiveState state)
        {
            injectedLiveState = state;
            Rebuild();
        }

        /// <summary>在「最近的導航」加一筆。</summary>
        internal void RecordNavigation(UIBlueprintNavRecord record)
        {
            navLog.Add(record);
            liveDirty = true;
            if (injectedLiveState != null && sidebar != null)
                DrawSidebar();
        }

        /// <summary>讓一條路由亮起來，約 2 秒內淡回原本的樣子。</summary>
        internal void FlashRoute(UIRoute route, double time)
        {
            if (route == null)
                return;
            flashes[route] = time;
            UpdateFlashes(time);
        }

        /// <summary>路由在 <paramref name="now"/> 這個時間有多亮：0 是原本的樣子，1 是最亮。</summary>
        internal float RouteFlash(UIRoute route, double now)
        {
            return route != null && flashes.TryGetValue(route, out var start)
                ? UIBlueprintLive.FlashLevel(now - start)
                : 0f;
        }

        /// <summary>
        /// 右欄的「觸發」按鈕能不能按：一般路由的來源要是目前最上層的畫面，訊號路由隨時可以。走訪進行中都不行。
        /// </summary>
        internal bool TriggerEnabled(UIRoute route)
        {
            if (!liveMode || route == null || UIWalk.IsRunning)
                return false;
            return route.IsSignal || (liveState != null && liveState.Top == route.Source);
        }

        /// <summary>
        /// 「觸發」：一般路由用 TriggerAsync，訊號路由用 SignalAsync，都不帶資料。結果寫進「最近的導航」。
        /// </summary>
        internal async void TriggerRoute(UIRoute route)
        {
            var root = LiveRoot;
            if (!root || !TriggerEnabled(route))
                return;
            pendingTrigger = route;
            pendingTriggerReported = false;
            UINavResult result;
            try
            {
                result = route.IsSignal
                    ? await root.SignalAsync(route.Action)
                    : await root.TriggerAsync(route.Source, route.Action);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }
            if (!this)
                return;
            // 找到路由之後的結果由 ActionCompleted 記下來；在那之前就被拒絕（例如 Busy）時這裡補記。
            if (pendingTrigger == route && !pendingTriggerReported && root)
                RecordNavigation(RouteRecord(route, result));
            if (pendingTrigger == route)
                pendingTrigger = null;
            liveDirty = true;
        }

        /// <summary>
        /// 在 Hierarchy 選取執行中的畫面物件。畫面還沒有建立（例如沒開過的 Popup）時不做事，回傳 false。
        /// </summary>
        internal bool SelectRunningView(string id)
        {
            var root = LiveRoot;
            if (!root || string.IsNullOrEmpty(id) || !root.TryGetView(id, out var running) || !running)
                return false;
            Selection.activeGameObject = running.gameObject;
            EditorGUIUtility.PingObject(running.gameObject);
            return true;
        }

        private bool HasRunningView(string id)
        {
            var root = LiveRoot;
            return root && !string.IsNullOrEmpty(id) && root.TryGetView(id, out var running) && running;
        }

        // Play Mode：用執行中的那個 Scene 的工作區。目前的工作區已經不能用時另外找一個啟用中的。
        private void ResolveLiveWorkspace()
        {
            if (IsUsableLiveRoot(workspace))
                return;
            workspace = FindLiveWorkspace();
        }

        private static bool IsUsableLiveRoot(UIRoot root)
        {
            return root && root.isActiveAndEnabled && root.gameObject.scene.IsValid();
        }

        private static UIRoot FindLiveWorkspace()
        {
            var roots = Object.FindObjectsByType<UIRoot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(IsUsableLiveRoot).ToArray();
            var active = SceneManager.GetActiveScene();
            return roots.FirstOrDefault(root => root.gameObject.scene == active) ?? roots.FirstOrDefault();
        }

        // Rebuild 之後開始顯示執行中的狀態。
        private void BeginLive()
        {
            if (injectedLiveState != null)
            {
                liveState = injectedLiveState;
                ApplyLiveMarkers();
                return;
            }
            SubscribeLive(workspace);
            RememberPolled();
            liveState = UIBlueprintLiveState.Capture(liveRoot, entering, leaving);
            ApplyLiveMarkers();
            StartLiveUpdate();
        }

        private void SubscribeLive(UIRoot root)
        {
            UnsubscribeLive();
            if (!ReferenceEquals(lastLiveRoot, root))
            {
                navLog.Clear();
                entering.Clear();
                leaving.Clear();
                flashes.Clear();
                lastLiveRoot = root;
            }
            liveRoot = root;
            liveLost = false;
            root.ViewShowing += LiveViewShowing;
            root.ViewShown += LiveViewShown;
            root.ViewHiding += LiveViewHiding;
            root.ViewHidden += LiveViewHidden;
            root.ActionCompleted += LiveActionCompleted;
            root.InputModeChanged += LiveInputModeChanged;
            root.DirectNavigationFinished += LiveDirectNavigation;
        }

        private void UnsubscribeLive()
        {
            if (ReferenceEquals(liveRoot, null))
                return;
            // 工作區被銷毀之後 C# 的事件還在，照樣取消。
            var root = liveRoot;
            liveRoot = null;
            root.ViewShowing -= LiveViewShowing;
            root.ViewShown -= LiveViewShown;
            root.ViewHiding -= LiveViewHiding;
            root.ViewHidden -= LiveViewHidden;
            root.ActionCompleted -= LiveActionCompleted;
            root.InputModeChanged -= LiveInputModeChanged;
            root.DirectNavigationFinished -= LiveDirectNavigation;
        }

        private void StartLiveUpdate()
        {
            if (liveUpdateRegistered)
                return;
            EditorApplication.update += LiveUpdate;
            liveUpdateRegistered = true;
        }

        private void StopLiveUpdate()
        {
            if (!liveUpdateRegistered)
                return;
            EditorApplication.update -= LiveUpdate;
            liveUpdateRegistered = false;
        }

        // 離開 Play Mode：取消所有訂閱，清掉執行中的資料。
        private void EndLive()
        {
            StopLiveUpdate();
            UnsubscribeLive();
            lastLiveRoot = null;
            liveState = null;
            liveLost = false;
            navLog.Clear();
            entering.Clear();
            leaving.Clear();
            flashes.Clear();
            pendingTrigger = null;
        }

        private void LiveUpdate()
        {
            if (!EditorApplication.isPlaying || injectedLiveState != null)
            {
                StopLiveUpdate();
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            if (!ReferenceEquals(liveRoot, null) && !IsUsableLiveRoot(liveRoot))
            {
                // 工作區被停用或銷毀（例如換了 Scene）：顯示說明，之後找到新的工作區再切換過去。
                UnsubscribeLive();
                liveLost = true;
                workspace = null;
                nextWorkspaceSearch = now + WorkspaceSearchInterval;
                Rebuild();
                return;
            }
            if (ReferenceEquals(liveRoot, null))
            {
                if (now < nextWorkspaceSearch)
                    return;
                nextWorkspaceSearch = now + WorkspaceSearchInterval;
                var found = FindLiveWorkspace();
                if (found)
                    SetWorkspace(found);
                return;
            }

            if (liveDirty || LiveChanged())
                RefreshLive();
            if (flashes.Count > 0 && now >= nextFlashFrame)
            {
                nextFlashFrame = now + FlashFrameInterval;
                UpdateFlashes(now);
            }
        }

        private bool LiveChanged()
        {
            var root = liveRoot;
            return polledPage != root.CurrentPage || polledTop != root.TopPopup ||
                polledPopups != root.Popups.Count || polledHistory != root.HistoryCount ||
                polledBusy != root.IsTransitioning || polledInput != root.InputMode || polledWalk != UIWalk.IsRunning;
        }

        private void RememberPolled()
        {
            var root = liveRoot;
            polledPage = root.CurrentPage;
            polledTop = root.TopPopup;
            polledPopups = root.Popups.Count;
            polledHistory = root.HistoryCount;
            polledBusy = root.IsTransitioning;
            polledInput = root.InputMode;
            polledWalk = UIWalk.IsRunning;
        }

        private void RefreshLive()
        {
            liveDirty = false;
            var root = liveRoot;
            // 轉場結束了：導航被取消時不會有 Shown 或 Hidden，這裡一併清掉。
            if (!root.IsTransitioning)
            {
                entering.Clear();
                leaving.Clear();
            }
            RememberPolled();
            liveState = UIBlueprintLiveState.Capture(root, entering, leaving);
            ApplyLiveMarkers();
            if (sidebar != null)
                DrawSidebar();
        }

        // 節點的外框與上方的徽章。只改樣式，不重建節點。
        private void ApplyLiveMarkers()
        {
            foreach (var pair in cards)
            {
                var card = pair.Value;
                foreach (var className in LiveNodeClasses)
                    card.RemoveFromClassList(className);
                var row = card.Q(className: "node-live");
                row?.Clear();
                if (liveState == null)
                    continue;
                foreach (var className in liveState.NodeClasses(pair.Key))
                    card.AddToClassList(className);
                if (row == null)
                    continue;
                foreach (var marker in liveState.Markers(pair.Key))
                    AddBadge(row, marker.Text, marker.ClassName);
            }
        }

        private void UpdateFlashes(double now)
        {
            foreach (var expired in flashes.Where(pair => UIBlueprintLive.FlashLevel(now - pair.Value) <= 0f)
                .Select(pair => pair.Key).ToArray())
                flashes.Remove(expired);
            foreach (var (route, label) in edgeLabels)
            {
                var level = RouteFlash(route, now);
                label.EnableInClassList("edge-flash", level > 0f);
                if (level > 0f)
                    label.style.backgroundColor = Color.Lerp(EdgeLabelColor, FlashColor * 0.55f, level);
                else
                    label.style.backgroundColor = StyleKeyword.Null;
            }
            // 訊號路由關掉時，剛走過的那一條要短暫畫出來，淡掉之後再收起來。
            ApplyEdgeStates(now);
        }

        private void LiveViewShowing(UIView shown)
        {
            if (shown)
            {
                entering.Add(shown.Id);
                leaving.Remove(shown.Id);
            }
            liveDirty = true;
        }

        private void LiveViewShown(UIView shown)
        {
            if (shown)
                entering.Remove(shown.Id);
            liveDirty = true;
        }

        private void LiveViewHiding(UIView hidden)
        {
            if (hidden)
            {
                leaving.Add(hidden.Id);
                entering.Remove(hidden.Id);
            }
            liveDirty = true;
        }

        private void LiveViewHidden(UIView hidden)
        {
            if (hidden)
                leaving.Remove(hidden.Id);
            liveDirty = true;
        }

        private void LiveActionCompleted(UIRoute route, UINavResult result)
        {
            if (route == null)
                return;
            if (route == pendingTrigger)
                pendingTriggerReported = true;
            RecordNavigation(RouteRecord(route, result));
            if (result == UINavResult.Completed && UIRoute.TargetsEntry(route.Kind))
                FlashRoute(route, EditorApplication.timeSinceStartup);
        }

        private void LiveDirectNavigation(UIDirectNavigation navigation)
        {
            RecordNavigation(DirectRecord(navigation));
        }

        private void LiveInputModeChanged(UIInputMode mode)
        {
            liveDirty = true;
        }

        // 走了一條路由。訊號路由的來源是「任何畫面」。
        internal UIBlueprintNavRecord RouteRecord(UIRoute route, UINavResult result)
        {
            return new UIBlueprintNavRecord
            {
                Time = DateTime.Now,
                Source = DisplayView(route.Source),
                Action = route.Action,
                Kind = route.Kind,
                Target = UIRoute.TargetsEntry(route.Kind) ? DisplayView(route.Target) : null,
                TargetId = UIRoute.TargetsEntry(route.Kind) ? route.Target : null,
                Result = result,
                Route = route,
            };
        }

        // 不是透過路由的導航：程式直接呼叫，或返回鍵。
        internal UIBlueprintNavRecord DirectRecord(UIDirectNavigation navigation)
        {
            var hasTarget = navigation.Kind == UIRouteKind.ShowPage || navigation.Kind == UIRouteKind.OpenPopup ||
                navigation.Kind == UIRouteKind.ClosePopup;
            return new UIBlueprintNavRecord
            {
                Time = DateTime.Now,
                Source = navigation.BackKey ? "返回鍵" : "程式",
                Kind = navigation.Kind,
                Target = hasTarget && !string.IsNullOrEmpty(navigation.Target) ? DisplayView(navigation.Target) : null,
                TargetId = hasTarget ? navigation.Target : null,
                Result = navigation.Result,
            };
        }

        // 右欄最上面的「執行中」：目前的頁面、開著的 Popup、返回歷史、是否轉場中、輸入模式。
        private void DrawLiveSection()
        {
            var section = AddSection("live", "執行中");
            var state = liveState ?? new UIBlueprintLiveState();
            AddInfo(section, "目前的頁面", string.IsNullOrEmpty(state.CurrentPage) ? "（沒有）" : DisplayView(state.CurrentPage),
                "live-page");
            AddInfo(section, "Popup", PopupListText(state), "live-popups");
            AddInfo(section, "返回歷史", HistoryListText(state), "live-history");
            AddInfo(section, "轉場中", state.Transitioning ? "是" : "否", "live-transitioning");
            AddInfo(section, "輸入模式", UIBlueprintLiveState.InputModeLabel(state.InputMode), "live-input");
            AddHelp(section, "Play Mode 中藍圖是唯讀的：可以選取節點、查看路由，用「觸發」按鈕走一條路由。" +
                "雙擊節點會在 Hierarchy 選取執行中的畫面。", "live-hint");
        }

        private string PopupListText(UIBlueprintLiveState state)
        {
            if (state.Popups.Count == 0)
                return "（沒有）";
            var lines = state.Popups.Select((id, index) =>
                $"{index + 1}. {DisplayView(id)}{(index == state.Popups.Count - 1 ? "（最上層）" : "")}");
            return "由下到上：\n" + string.Join("\n", lines);
        }

        private string HistoryListText(UIBlueprintLiveState state)
        {
            if (state.History.Count == 0)
                return "（空的）";
            var lines = state.History.Select((id, index) => $"{index + 1}. {DisplayView(id)}");
            return $"{state.History.Count} 筆，由近到遠：\n" + string.Join("\n", lines);
        }

        // 「最近的導航」：最近 20 筆，新的在上面。結果不是 Completed 的用醒目的顏色。點一筆選取它的路由。
        private void DrawNavigationLogSection()
        {
            var section = AddSection("nav-log", "最近的導航");
            if (navLog.Count == 0)
            {
                AddHelp(section, "還沒有導航。切頁、開關 Popup、送出訊號之後會列在這裡，新的在上面。", "nav-log-empty");
                return;
            }
            foreach (var record in navLog.Records)
            {
                var row = new Button(() => SelectRecord(record)) { name = "nav-record", userData = record };
                row.AddToClassList("nav-record");
                row.EnableInClassList("nav-problem", record.IsProblem);
                row.tooltip = record.ByCode
                    ? "不是透過路由的導航（程式直接呼叫或返回鍵）。點一下選取目的地。"
                    : "點一下選取這條路由。";
                AddRecordText(row, record.Time.ToString("HH:mm:ss"), "nav-time");
                AddRecordText(row, record.Action == null ? record.Source : $"{record.Source} · {record.Action}",
                    "nav-main");
                AddBadge(row, KindLabel(record.Kind), "kind-" + record.Kind);
                if (record.Target != null)
                    AddRecordText(row, "→ " + record.Target, "nav-target");
                var result = AddRecordText(row, record.Result.ToString(), "nav-result");
                result.EnableInClassList("nav-result-problem", record.IsProblem);
                section.Add(row);
            }
        }

        private static Label AddRecordText(VisualElement row, string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            row.Add(label);
            return label;
        }

        private void SelectRecord(UIBlueprintNavRecord record)
        {
            if (record.Route != null && workspace && workspace.Routes.Contains(record.Route))
                SelectRoute(record.Route.Source, record.Route.Action);
            else if (!string.IsNullOrEmpty(record.TargetId) && cards.ContainsKey(record.TargetId))
                SelectNode(record.TargetId);
        }

        // 「從這裡出發的路由」每一列右邊的「觸發」按鈕。
        private void AddTriggerButton(VisualElement row, UIRoute route)
        {
            var trigger = new Button(() => TriggerRoute(route))
            {
                name = "trigger-route",
                text = "觸發",
                userData = route,
                tooltip = route.IsSignal
                    ? "用 SignalAsync 送出這個訊號，不帶資料。"
                    : "用 TriggerAsync 走這條路由。來源要是目前最上層的畫面才能按。",
            };
            trigger.AddToClassList("trigger-button");
            trigger.SetEnabled(TriggerEnabled(route));
            row.Add(trigger);
        }
    }
}
