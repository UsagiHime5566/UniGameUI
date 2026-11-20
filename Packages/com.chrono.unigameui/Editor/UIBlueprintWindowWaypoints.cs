using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniGameUI.Editor
{
    // 線的中繼點：每條路由可以有任意個中繼點，線依序穿過它們。只是整理線的走向，沒有任何功能。
    // 在線上雙擊或按右鍵新增，拖曳移動，雙擊、按右鍵或選取之後按 Delete 刪除。Play Mode 中只能看。
    // 這裡的操作都拆成可以直接呼叫的函式；滑鼠與鍵盤事件只是轉給它們。
    public sealed partial class UIBlueprintWindow
    {
        /// <summary>滑鼠離線多近（視窗的像素）算是在線上。</summary>
        internal const float LineReach = 6f;
        // 中繼點的圓點在畫面上的直徑（視窗的像素，不跟著縮放）。
        private const float WaypointSize = 10f;

        // 中繼點目前的位置（畫布座標）。拖曳途中先改這裡，放開才寫回路由。
        private readonly Dictionary<UIRoute, List<Vector2>> waypointPositions = new Dictionary<UIRoute, List<Vector2>>();
        private readonly List<(UIRoute Route, int Index, VisualElement Dot)> waypointDots =
            new List<(UIRoute, int, VisualElement)>();
        private UIRoute dragWaypointRoute;
        private int dragWaypointIndex = -1;
        private Vector2 dragWaypointPointer;
        private Vector2 dragWaypointStart;
        private bool dragWaypointMoved;
        private UIRoute selectedWaypointRoute;
        private int selectedWaypointIndex = -1;

        /// <summary>這條路由目前的中繼點（畫布座標，依線經過的順序）。拖曳途中是拖曳中的位置。</summary>
        internal IReadOnlyList<Vector2> Waypoints(UIRoute route)
        {
            if (route != null && waypointPositions.TryGetValue(route, out var points))
                return points;
            return route?.Waypoints ?? (IReadOnlyList<Vector2>)Array.Empty<Vector2>();
        }

        /// <summary>選取中的中繼點。沒有時 Route 是 null。</summary>
        internal (UIRoute Route, int Index) SelectedWaypoint => (selectedWaypointRoute, selectedWaypointIndex);

        /// <summary>這條路由的線（畫布座標）：從來源的連接點、穿過中繼點、到目的地的左側。</summary>
        internal IReadOnlyList<UIBlueprintSegment> RoutePath(UIRoute route)
        {
            var from = positions[route.Source] + EdgeOutlet;
            var to = positions[route.Target] + EdgeInlet;
            return UIBlueprintCurves.Path(from, to, Waypoints(route));
        }

        /// <summary>線上動作標籤的位置（畫布座標）：整條線長度的中間。</summary>
        internal Vector2 LabelAnchor(UIRoute route)
        {
            return UIBlueprintCurves.Midpoint(RoutePath(route));
        }

        /// <summary>
        /// 在線上視窗座標的這一點新增中繼點，依它在線上的位置插入順序。回傳它是第幾個；Play Mode 中不能新增，回傳 -1。
        /// </summary>
        internal int AddWaypointAt(UIRoute route, Vector2 windowPoint)
        {
            if (liveMode || !HasLine(route))
                return -1;
            var point = view.WindowToCanvas(windowPoint);
            var index = UIBlueprintCurves.InsertionIndex(RoutePath(route), point);
            var points = Waypoints(route).ToList();
            index = Mathf.Clamp(index, 0, points.Count);
            points.Insert(index, point);
            if (!CommitWaypoints(route, points, "Add blueprint reroute point"))
                return -1;
            SelectWaypoint(route, index);
            return index;
        }

        /// <summary>刪除一個中繼點。Play Mode 中不能刪除。</summary>
        internal void RemoveWaypoint(UIRoute route, int index)
        {
            if (liveMode || route == null)
                return;
            var points = Waypoints(route).ToList();
            if (index < 0 || index >= points.Count)
                return;
            points.RemoveAt(index);
            if (selectedWaypointRoute == route)
                selectedWaypointRoute = null;
            CommitWaypoints(route, points, "Remove blueprint reroute point");
        }

        /// <summary>清除這條路由所有的中繼點。Play Mode 中不能清除。</summary>
        internal void ClearWaypoints(UIRoute route)
        {
            if (liveMode || route == null || Waypoints(route).Count == 0)
                return;
            if (selectedWaypointRoute == route)
                selectedWaypointRoute = null;
            CommitWaypoints(route, Array.Empty<Vector2>(), "Clear blueprint reroute points");
        }

        /// <summary>選取之後按 Delete：刪除選取中的中繼點。</summary>
        internal void DeleteSelectedWaypoint()
        {
            if (selectedWaypointRoute != null)
                RemoveWaypoint(selectedWaypointRoute, selectedWaypointIndex);
        }

        /// <summary>
        /// 在中繼點上按下滑鼠左鍵：選取它所屬的路由與這個中繼點並準備拖曳；雙擊是刪除。Play Mode 中只選取。
        /// </summary>
        internal void PressWaypoint(UIRoute route, int index, Vector2 windowPoint, int clickCount = 1)
        {
            SelectRoute(route.Source, route.Action);
            SelectWaypoint(route, index);
            if (liveMode)
                return;
            if (clickCount >= 2)
            {
                RemoveWaypoint(route, index);
                return;
            }
            BeginWaypointDrag(route, index, windowPoint);
        }

        /// <summary>開始拖曳中繼點。<paramref name="windowPoint"/> 是按下的位置（視窗座標）。</summary>
        internal void BeginWaypointDrag(UIRoute route, int index, Vector2 windowPoint)
        {
            if (liveMode || route == null || index < 0 || index >= Waypoints(route).Count)
                return;
            waypointPositions[route] = Waypoints(route).ToList();
            dragWaypointRoute = route;
            dragWaypointIndex = index;
            dragWaypointPointer = windowPoint;
            dragWaypointStart = waypointPositions[route][index];
            dragWaypointMoved = false;
        }

        /// <summary>拖曳途中：線跟著即時變。滑鼠移動的距離換算成畫布上的距離。</summary>
        internal void DragWaypointTo(Vector2 windowPoint)
        {
            if (dragWaypointRoute == null)
                return;
            var delta = windowPoint - dragWaypointPointer;
            if (!dragWaypointMoved && delta.magnitude < DragThreshold)
                return;
            dragWaypointMoved = true;
            waypointPositions[dragWaypointRoute][dragWaypointIndex] = dragWaypointStart + view.WindowToCanvasDelta(delta);
            UpdateStage();
        }

        /// <summary>放開。中繼點有移動時寫回路由並回傳 true。</summary>
        internal bool EndWaypointDrag()
        {
            var route = dragWaypointRoute;
            dragWaypointRoute = null;
            if (route == null || !dragWaypointMoved)
                return false;
            return CommitWaypoints(route, waypointPositions[route], "Move blueprint reroute point");
        }

        // 拖到一半被打斷：回到原位。
        private void CancelWaypointDrag()
        {
            if (dragWaypointRoute == null)
                return;
            if (dragWaypointMoved)
            {
                waypointPositions[dragWaypointRoute][dragWaypointIndex] = dragWaypointStart;
                UpdateStage();
            }
            dragWaypointRoute = null;
        }

        /// <summary>
        /// 在畫布上沒有節點、標籤、中繼點的地方按下滑鼠左鍵。靠近線時：單擊等於點它的標籤（選取來源節點與這條路由），
        /// 雙擊在那裡新增中繼點。不在線上時取消選取。回傳有沒有按到線。
        /// </summary>
        internal bool PressLine(Vector2 windowPoint, int clickCount = 1)
        {
            var route = RouteNear(windowPoint);
            if (route == null)
            {
                ClickCanvas();
                return false;
            }
            SelectRoute(route.Source, route.Action);
            if (clickCount >= 2)
                AddWaypointAt(route, windowPoint);
            return true;
        }

        /// <summary>
        /// 右鍵選單的項目。在中繼點上是「刪除中繼點」；在線上是「在這裡新增中繼點」，線有中繼點時另有「清除這條線的中繼點」。
        /// Play Mode 中沒有任何項目。
        /// </summary>
        internal IReadOnlyList<(string Label, Action Run)> ContextMenuItems(Vector2 windowPoint, UIRoute waypointRoute = null,
            int waypointIndex = -1)
        {
            var items = new List<(string, Action)>();
            if (liveMode)
                return items;
            if (waypointRoute != null)
            {
                items.Add(("刪除中繼點", () =>
                {
                    SelectRoute(waypointRoute.Source, waypointRoute.Action);
                    RemoveWaypoint(waypointRoute, waypointIndex);
                }));
                return items;
            }
            var route = RouteNear(windowPoint);
            if (route == null)
                return items;
            items.Add(("在這裡新增中繼點", () =>
            {
                SelectRoute(route.Source, route.Action);
                AddWaypointAt(route, windowPoint);
            }));
            if (Waypoints(route).Count > 0)
            {
                items.Add(("清除這條線的中繼點", () =>
                {
                    SelectRoute(route.Source, route.Action);
                    ClearWaypoints(route);
                }));
            }
            return items;
        }

        private void PopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            var point = WindowPoint(evt.mousePosition);
            IReadOnlyList<(string Label, Action Run)> items;
            if (evt.target is VisualElement { userData: ValueTuple<UIRoute, int> waypoint })
                items = ContextMenuItems(point, waypoint.Item1, waypoint.Item2);
            else if (evt.target == canvas)
                items = ContextMenuItems(point);
            else
                return;
            foreach (var (label, run) in items)
                evt.menu.AppendAction(label, _ => run());
        }

        private bool HasLine(UIRoute route)
        {
            return route != null && UIRoute.TargetsEntry(route.Kind) && positions.ContainsKey(route.Source) &&
                !string.IsNullOrEmpty(route.Target) && positions.ContainsKey(route.Target);
        }

        private void SelectWaypoint(UIRoute route, int index)
        {
            selectedWaypointRoute = route;
            selectedWaypointIndex = index;
            ApplyEdgeStates(UnityEditor.EditorApplication.timeSinceStartup);
        }

        // 寫回路由（可以 Undo，Scene 標成未儲存），再重建畫布。
        private bool CommitWaypoints(UIRoute route, IEnumerable<Vector2> points, string undoName)
        {
            try
            {
                UIAuthoring.SetWaypoints(workspace, route, points, undoName);
            }
            catch (Exception exception)
            {
                UnityEditor.EditorUtility.DisplayDialog("UniGameUI", exception.Message, "關閉");
                return false;
            }
            Rebuild();
            return true;
        }

        // 中繼點的圓點：顏色跟著它所屬的線，可以拖曳、雙擊刪除、按右鍵。
        private void CreateWaypointDots()
        {
            foreach (var route in workspace.Routes)
            {
                if (route.Waypoints.Count == 0)
                    continue;
                waypointPositions[route] = route.Waypoints.ToList();
                if (!UIRoute.TargetsEntry(route.Kind))
                    continue;
                for (var index = 0; index < route.Waypoints.Count; index++)
                {
                    var owner = route;
                    var position = index;
                    var dot = new VisualElement
                    {
                        name = "route-waypoint",
                        userData = (owner, position),
                        tooltip = liveMode
                            ? $"{owner.Action} 的中繼點。Play Mode 中不能修改。"
                            : $"{owner.Action} 的中繼點。拖曳移動，雙擊或選取之後按 Delete 刪除，也可以按右鍵。",
                    };
                    dot.AddToClassList("route-waypoint");
                    dot.AddToClassList(owner.IsSignal ? "waypoint-signal"
                        : owner.Kind == UIRouteKind.OpenPopup ? "waypoint-popup" : "waypoint-page");
                    dot.style.width = WaypointSize;
                    dot.style.height = WaypointSize;
                    RegisterWaypointPointer(dot, owner, position);
                    waypointDots.Add((owner, position, dot));
                    content.Add(dot);
                }
            }
        }

        private void RegisterWaypointPointer(VisualElement dot, UIRoute route, int index)
        {
            dot.RegisterCallback<PointerEnterEvent>(_ => SetHoveredRoute(route));
            dot.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (hoveredRoute == route)
                    SetHoveredRoute(null);
            });
            dot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.altKey)
                    return;
                canvas.Focus();
                PressWaypoint(route, index, WindowPoint(evt.position), evt.clickCount);
                if (dragWaypointRoute != null && dot.panel != null)
                    dot.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            dot.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (dot.HasPointerCapture(evt.pointerId))
                    DragWaypointTo(WindowPoint(evt.position));
            });
            dot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dot.HasPointerCapture(evt.pointerId))
                    return;
                releasing = true;
                dot.ReleasePointer(evt.pointerId);
                releasing = false;
                EndWaypointDrag();
                evt.StopPropagation();
            });
            dot.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (!releasing)
                    CancelWaypointDrag();
            });
        }

        // 圓點放在中繼點上，大小不跟著縮放。
        private void PlaceWaypointDots()
        {
            foreach (var (route, index, dot) in waypointDots)
            {
                var points = Waypoints(route);
                if (index >= points.Count)
                    continue;
                dot.style.left = points[index].x - WaypointSize * 0.5f;
                dot.style.top = points[index].y - WaypointSize * 0.5f;
                dot.style.scale = new Scale(Vector2.one / view.Zoom);
            }
        }

        // 中繼點跟著線：線不畫時不顯示；路由被選取或滑鼠移到線上時變明顯。
        private void ApplyWaypointStates(double now, bool showSignals)
        {
            foreach (var (route, index, dot) in waypointDots)
            {
                var visible = HasLine(route) && (!route.IsSignal || showSignals || RouteFlash(route, now) > 0f);
                dot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                dot.EnableInClassList("waypoint-emphasized", IsEmphasized(route));
                dot.EnableInClassList("waypoint-selected", route == selectedWaypointRoute && index == selectedWaypointIndex);
            }
        }
    }
}
