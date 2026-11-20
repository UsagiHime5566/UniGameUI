using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-9：線的中繼點。測的是曲線、命中、插入順序、資料與 Undo；
    // 雙擊、右鍵與拖曳的滑鼠事件接線、畫出來的樣子要人工確認。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void ReroutePointsAreInsertedInLineOrderAndTheCurvePassesThroughThem()
        {
            var root = NewWaypointWorkspace("Batch5 Waypoint Order", out var scene, out var go);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                Assert.That(window.RoutePath(go).Count, Is.EqualTo(1), "沒有中繼點時是一段曲線");

                // 在線的中間雙擊：新增第一個中繼點。
                var first = window.RoutePath(go)[0].Evaluate(0.5f);
                Assert.That(window.AddWaypointAt(go, window.View.CanvasToWindow(first)), Is.EqualTo(0));
                AssertClose(go.Waypoints.Single(), first);
                Assert.That(window.SelectedWaypoint, Is.EqualTo((go, 0)));

                // 在第一段（來源到第一個中繼點）上新增：插在最前面；在最後一段上新增：插在最後面。
                var before = window.RoutePath(go)[0].Evaluate(0.5f);
                Assert.That(window.AddWaypointAt(go, window.View.CanvasToWindow(before)), Is.EqualTo(0));
                var after = window.RoutePath(go)[2].Evaluate(0.5f);
                Assert.That(window.AddWaypointAt(go, window.View.CanvasToWindow(after)), Is.EqualTo(2));
                // 在 first 與 after 之間那一段新增：插在它們中間。
                var middle = window.RoutePath(go)[2].Evaluate(0.5f);
                Assert.That(window.AddWaypointAt(go, window.View.CanvasToWindow(middle)), Is.EqualTo(2));
                Assert.That(go.Waypoints.Count, Is.EqualTo(4));

                // 中繼點依它們在線上的位置排列：沿著線走，經過的順序就是清單的順序。
                var path = window.RoutePath(go);
                Assert.That(path.Count, Is.EqualTo(5));
                for (var index = 0; index < go.Waypoints.Count; index++)
                {
                    // 曲線穿過每一個中繼點。
                    AssertClose(path[index].End, go.Waypoints[index]);
                    AssertClose(path[index + 1].Start, go.Waypoints[index]);
                    Assert.That(UIBlueprintCurves.DistanceTo(path, go.Waypoints[index], out _), Is.LessThan(0.01f));
                    // 經過中繼點時兩邊的切線方向相同：沒有尖角。
                    var incoming = (path[index].End - path[index].Control2).normalized;
                    var outgoing = (path[index + 1].Control1 - path[index + 1].Start).normalized;
                    Assert.That(Vector2.Dot(incoming, outgoing), Is.GreaterThan(0.999f), $"第 {index} 個中繼點");
                }
                var order = new[] { before, first, middle, after };
                for (var index = 0; index < order.Length; index++)
                    AssertClose(go.Waypoints[index], order[index]);

                // 整條線從來源的連接點出發，進入目的地的左側，兩端都是水平的。
                AssertClose(path[0].Start, window.NodeRect("home").position +
                    new Vector2(UIBlueprintWindow.NodeWidth, UIBlueprintWindow.NodeHeight / 2f));
                AssertClose(path[^1].End, window.NodeRect("next").position + new Vector2(0, UIBlueprintWindow.NodeHeight / 2f));
                Assert.That(path[0].Control1.y, Is.EqualTo(path[0].Start.y).Within(0.01f));
                Assert.That(path[^1].Control2.y, Is.EqualTo(path[^1].End.y).Within(0.01f));
                Assert.That(window.rootVisualElement.Query(className: "route-waypoint").ToList().Count, Is.EqualTo(4));

                // 標籤放在整條線長度的中間：一個中繼點拉到正下方時，標籤就在它旁邊。
                var from = path[0].Start;
                var to = path[^1].End;
                var below = new Vector2((from.x + to.x) / 2f, from.y + 300);
                UIAuthoring.SetWaypoints(root, go, new[] { below }, "Test");
                window.CreateGUI();
                AssertClose(window.LabelAnchor(go), below, 2f);
                var label = window.EdgeLabel("home", "next");
                Assert.That(label.style.left.value.value, Is.EqualTo(window.LabelAnchor(go).x).Within(0.01f));
                Assert.That(label.style.top.value.value, Is.EqualTo(window.LabelAnchor(go).y - 16).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void LinesWithReroutePointsArePickedNearTheCurveAtAnyZoom()
        {
            var root = NewWaypointWorkspace("Batch5 Waypoint Pick", out var scene, out var go);
            UIAuthoring.SetWaypoints(root, go, new[] { new Vector2(400, -250), new Vector2(620, -250) }, "Test");
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                foreach (var zoom in new[] { 0.5f, 2f })
                {
                    window.SetZoom(zoom);
                    var path = window.RoutePath(go);
                    foreach (var (segment, t) in new[] { (0, 0.4f), (1, 0.5f), (2, 0.6f) })
                    {
                        var onLine = path[segment].Evaluate(t);
                        var tangent = (path[segment].Evaluate(t + 0.01f) - path[segment].Evaluate(t - 0.01f)).normalized;
                        var normal = new Vector2(-tangent.y, tangent.x);
                        var window0 = window.View.CanvasToWindow(onLine);
                        // 畫面上 4 像素內算在線上，9 像素外不算，和縮放無關。
                        Assert.That(window.RouteNear(window0 + normal * 4f), Is.SameAs(go), $"{zoom} 第 {segment} 段");
                        Assert.That(window.RouteNear(window0 + normal * 9f), Is.Null, $"{zoom} 第 {segment} 段");
                        // 在這一段新增時插在第 segment 個。
                        Assert.That(UIBlueprintCurves.InsertionIndex(path, onLine), Is.EqualTo(segment));
                    }
                }

                // 在線上單擊：等於點它的標籤。點空白處取消選取。
                var point = window.View.CanvasToWindow(window.RoutePath(go)[1].Evaluate(0.5f));
                Assert.That(window.PressLine(point), Is.True);
                Assert.That((window.SelectedId, window.SelectedAction), Is.EqualTo(("home", "next")));
                Assert.That(window.EdgeStyle(go, 0).Width, Is.GreaterThan(2f), "選取的線變粗");
                Assert.That(window.PressLine(point + new Vector2(0, 60)), Is.False);
                Assert.That(window.SelectedId, Is.Null.Or.Empty);
                Assert.That(go.Waypoints.Count, Is.EqualTo(2), "單擊不會新增");

                // 滑鼠移到線上：線變亮、變粗。
                Assert.That(window.EdgeStyle(go, 0).Width, Is.EqualTo(2f));
                window.SetHoveredRoute(window.RouteNear(point));
                Assert.That(window.EdgeStyle(go, 0).Width, Is.GreaterThan(2f));
                Assert.That(window.rootVisualElement.Q("blueprint-canvas").ClassListContains("line-hover"), Is.True);
                window.SetHoveredRoute(null);

                // 在線上雙擊：在那裡新增中繼點，插在第 1 個。
                Assert.That(window.PressLine(point, 2), Is.True);
                Assert.That(go.Waypoints.Count, Is.EqualTo(3));
                AssertClose(go.Waypoints[1], window.View.WindowToCanvas(point));

                // 右鍵選單：線上是「在這裡新增中繼點」與「清除這條線的中繼點」，中繼點上是「刪除中繼點」，空白處沒有。
                var near = window.View.CanvasToWindow(window.RoutePath(go)[0].Evaluate(0.5f));
                var items = window.ContextMenuItems(near);
                Assert.That(items.Select(item => item.Label), Is.EqualTo(new[] { "在這裡新增中繼點", "清除這條線的中繼點" }));
                items[0].Run();
                Assert.That(go.Waypoints.Count, Is.EqualTo(4));
                var dot = window.ContextMenuItems(near, go, 0);
                Assert.That(dot.Select(item => item.Label), Is.EqualTo(new[] { "刪除中繼點" }));
                dot[0].Run();
                Assert.That(go.Waypoints.Count, Is.EqualTo(3));
                Assert.That(window.ContextMenuItems(window.View.CanvasToWindow(new Vector2(-500, -800))), Is.Empty);
                window.ContextMenuItems(near)[1].Run();
                Assert.That(go.Waypoints, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void ReroutePointsAreMovedAndDeletedWithUndo()
        {
            var root = NewWaypointWorkspace("Batch5 Waypoint Undo", out var scene, out var go);
            var start = new Vector2(500, -200);
            UIAuthoring.SetWaypoints(root, go, new[] { start, new Vector2(650, -200) }, "Test");
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                window.SetZoom(2f);
                Undo.IncrementCurrentGroup();
                // Undo 會重新建立路由的物件，所以每次都從工作區重新找。
                UIRoute Go() => root.Routes.Single(route => route.Source == "home" && route.Action == "next");

                // 拖曳：滑鼠移動的距離除以縮放。途中只改畫面，放開才寫回，Scene 變成未儲存。
                var press = window.View.CanvasToWindow(start);
                window.PressWaypoint(go, 0, press);
                Assert.That((window.SelectedId, window.SelectedAction), Is.EqualTo(("home", "next")), "選取它所屬的路由");
                Assert.That(window.SelectedWaypoint, Is.EqualTo((go, 0)));
                Assert.That(window.rootVisualElement.Q<Label>("route-waypoints").text, Is.EqualTo("2 個"));
                window.MovePointer(press + new Vector2(40, 20));
                AssertClose(window.Waypoints(go)[0], start + new Vector2(20, 10));
                AssertClose(go.Waypoints[0], start);
                Assert.That(window.FrameRect.Contains(window.Waypoints(go)[0]), Is.True);
                window.ReleasePointer(press + new Vector2(40, 20));
                AssertClose(go.Waypoints[0], start + new Vector2(20, 10));
                Assert.That(scene.isDirty, Is.True);
                Undo.PerformUndo();
                AssertClose(Go().Waypoints[0], start);
                AssertClose(window.Waypoints(Go())[0], start);

                // 雙擊中繼點刪除；Undo 回來。
                Undo.IncrementCurrentGroup();
                window.PressWaypoint(Go(), 1, window.View.CanvasToWindow(Go().Waypoints[1]), 2);
                Assert.That(Go().Waypoints.Count, Is.EqualTo(1));
                Undo.PerformUndo();
                Assert.That(Go().Waypoints.Count, Is.EqualTo(2));

                // 選取之後按 Delete。
                var point = window.View.CanvasToWindow(Go().Waypoints[1]);
                window.PressWaypoint(Go(), 1, point);
                window.ReleasePointer(point);
                window.DeleteSelectedWaypoint();
                Assert.That(Go().Waypoints.Count, Is.EqualTo(1));
                AssertClose(Go().Waypoints[0], start);

                // 右欄的「清除中繼點」。
                Undo.IncrementCurrentGroup();
                window.SelectRoute("home", "next");
                ClickButton(window.rootVisualElement.Q<Button>("clear-waypoints"));
                Assert.That(Go().Waypoints, Is.Empty);
                Assert.That(window.rootVisualElement.Q<Label>("route-waypoints").text, Does.StartWith("沒有"));
                Assert.That(window.rootVisualElement.Q<Button>("clear-waypoints").enabledSelf, Is.False);
                Undo.PerformUndo();
                Assert.That(Go().Waypoints.Count, Is.EqualTo(1));

                // 改名、改目的地時保留；刪除路由時一起消失。
                UIAuthoring.UpdateRoute(root, "home", "next", "goOn", UIRouteKind.ShowPage, "next", UIHistoryMode.Clear);
                Assert.That(root.Routes.Single(route => route.Action == "goOn").Waypoints.Count, Is.EqualTo(1));
                UIAuthoring.Disconnect(root, "home", "goOn");
                UIAuthoring.Connect(root, "home", "goOn", UIRouteKind.ShowPage, "next");
                Assert.That(root.Routes.Single(route => route.Action == "goOn").Waypoints, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void PlanReplaceKeepsReroutePoints()
        {
            RequireDOTween();
            var root = NewWaypointWorkspace("Batch5 Waypoint Plan", out _, out var go);
            UIAuthoring.SetWaypoints(root, go, new[] { new Vector2(400, -100) }, "Test");
            UIPlan.Apply(@"{ ""routes"": [{ ""source"": ""home"", ""action"": ""next"", ""replace"": true,
                ""history"": ""Clear"" }] }");
            var route = root.Routes.Single(candidate => candidate.Source == "home" && candidate.Action == "next");
            Assert.That(route.History, Is.EqualTo(UIHistoryMode.Clear), "計畫有套用");
            Assert.That(route.Waypoints, Is.EqualTo(new[] { new Vector2(400, -100) }));
        }

        [Test]
        public void FrameAndShowAllIncludeReroutePoints()
        {
            var root = NewWaypointWorkspace("Batch5 Waypoint Frame", out var scene, out var go);
            var far = new Vector2(2600, 1900);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                Assert.That(window.FrameRect.Contains(far), Is.False);
                UIAuthoring.SetWaypoints(root, go, new[] { far }, "Test");
                window.CreateGUI();
                var frame = window.FrameRect;
                Assert.That(frame.xMax, Is.GreaterThanOrEqualTo(far.x + UIBlueprintWindow.FrameMargin - 0.01f));
                Assert.That(frame.yMax, Is.GreaterThanOrEqualTo(far.y + UIBlueprintWindow.FrameMargin - 0.01f));
                window.ShowAllNodes();
                var shown = window.View.CanvasToWindow(far);
                Assert.That(new Rect(Vector2.zero, window.ViewSize).Contains(shown), Is.True, shown.ToString());
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void ReroutePointsCannotBeChangedInPlayMode()
        {
            var root = NewWaypointWorkspace("Batch5 Waypoint Live", out var scene, out var go);
            var start = new Vector2(500, -200);
            UIAuthoring.SetWaypoints(root, go, new[] { start }, "Test");
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                window.ShowLiveStateForTests(LiveState("home", new string[0], new string[0]));

                // 看得到中繼點，但不能新增、移動或刪除。
                Assert.That(window.rootVisualElement.Query(className: "route-waypoint").ToList().Count, Is.EqualTo(1));
                var onLine = window.View.CanvasToWindow(window.RoutePath(go)[0].Evaluate(0.5f));
                Assert.That(window.AddWaypointAt(go, onLine), Is.EqualTo(-1));
                Assert.That(window.PressLine(onLine, 2), Is.True, "可以點線選取路由");
                Assert.That(window.SelectedAction, Is.EqualTo("next"));
                var point = window.View.CanvasToWindow(start);
                window.PressWaypoint(go, 0, point);
                window.MovePointer(point + new Vector2(80, 80));
                window.ReleasePointer(point + new Vector2(80, 80));
                window.PressWaypoint(go, 0, point, 2);
                window.DeleteSelectedWaypoint();
                window.RemoveWaypoint(go, 0);
                window.ClearWaypoints(go);
                Assert.That(window.ContextMenuItems(onLine), Is.Empty);
                Assert.That(window.ContextMenuItems(point, go, 0), Is.Empty);
                Assert.That(go.Waypoints, Is.EqualTo(new[] { start }));

                // 右欄看得到數量，沒有「清除中繼點」。
                window.SelectRoute("home", "next");
                Assert.That(window.rootVisualElement.Q<Label>("route-waypoints").text, Is.EqualTo("1 個"));
                Assert.That(window.rootVisualElement.Q("clear-waypoints"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void BackwardLinesStayNearTheirNodes()
        {
            // 往前的線：控制點的距離照舊跟著水平距離變大。
            Assert.That(UIBlueprintCurves.Reach(new Vector2(0, 0), new Vector2(1000, 0)), Is.EqualTo(450f).Within(0.01f));
            Assert.That(UIBlueprintCurves.Reach(new Vector2(0, 0), new Vector2(50, 300)),
                Is.EqualTo(UIBlueprintCurves.MinReach));
            // 回頭的線：有上限，不論隔多遠。
            Assert.That(UIBlueprintCurves.Reach(new Vector2(1000, 0), new Vector2(0, 200)),
                Is.EqualTo(UIBlueprintCurves.BackwardReach));
            Assert.That(UIBlueprintCurves.Reach(new Vector2(3000, 0), new Vector2(0, 0)),
                Is.EqualTo(UIBlueprintCurves.BackwardReach));
            Assert.That(UIBlueprintCurves.Reach(new Vector2(100, 0), new Vector2(0, 0)), Is.EqualTo(UIBlueprintCurves.MinReach));

            // 目的地在來源左邊很遠：整條線都在兩個節點附近，不會跑出範圍框。
            var root = NewWaypointWorkspace("Batch5 Backward Line", out var scene, out var go);
            UIAuthoring.PositionNode(root, "home", new Vector2(2400, 0));
            UIAuthoring.PositionNode(root, "next", new Vector2(0, 260));
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                var path = window.RoutePath(go);
                var from = path[0].Start;
                var to = path[^1].End;
                var frame = window.FrameRect;
                foreach (var point in UIBlueprintCurves.Sample(path))
                {
                    Assert.That(point.x, Is.LessThanOrEqualTo(from.x + UIBlueprintCurves.BackwardReach));
                    Assert.That(point.x, Is.GreaterThanOrEqualTo(to.x - UIBlueprintCurves.BackwardReach));
                    Assert.That(frame.Contains(point), Is.True, point.ToString());
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        // 和右欄的測試同一組畫面，節點排開，home/next 這條線在上方，不和其他線交錯。
        private UIRoot NewWaypointWorkspace(string name, out UnityEngine.SceneManagement.Scene scene, out UIRoute go)
        {
            var root = NewSidebarWorkspace(name, out scene);
            UIAuthoring.PositionNode(root, "home", new Vector2(0, 0));
            UIAuthoring.PositionNode(root, "next", new Vector2(800, 0));
            UIAuthoring.PositionNode(root, "tip", new Vector2(800, 600));
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(0, 900));
            go = root.Routes.Single(route => route.Source == "home" && route.Action == "next");
            return root;
        }

        private static void AssertClose(Vector2 actual, Vector2 expected, float tolerance)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(tolerance), $"{actual} 應該是 {expected}");
        }
    }
}
