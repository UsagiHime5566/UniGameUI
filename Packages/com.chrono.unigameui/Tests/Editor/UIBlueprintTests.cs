using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第五批：藍圖視窗的操作體驗。
    // 視窗的互動都拆成可以直接呼叫的函式（縮放、平移、拖曳節點、拖線、選取），這裡測的是那些函式的邏輯與
    // 介面的結構。滑鼠事件怎麼接到這些函式、畫面看起來的樣子，自動測試看不到，要人工確認。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void ViewportConvertsBetweenCanvasAndWindowCoordinates()
        {
            var view = new UIBlueprintViewport(new Vector2(40, -25), 1f);
            var cursor = new Vector2(310, 220);
            var anchor = view.WindowToCanvas(cursor);

            // 以游標為中心縮放：游標底下的畫布座標不變。
            foreach (var zoom in new[] { 0.5f, 1.3f, 2f, 0.25f, 1f })
            {
                view = view.ZoomedAround(cursor, zoom);
                Assert.That(view.Zoom, Is.EqualTo(zoom).Within(1e-5f));
                AssertClose(view.WindowToCanvas(cursor), anchor);
                AssertClose(view.CanvasToWindow(anchor), cursor);
            }

            // 比例夾在 25% 到 200%；夾住之後游標底下的那一點一樣不動。
            Assert.That(UIBlueprintViewport.MinZoom, Is.EqualTo(0.25f));
            Assert.That(UIBlueprintViewport.MaxZoom, Is.EqualTo(2f));
            var tooFar = view.ZoomedAround(cursor, 9f);
            Assert.That(tooFar.Zoom, Is.EqualTo(2f));
            AssertClose(tooFar.WindowToCanvas(cursor), anchor);
            var tooSmall = view.ZoomedAround(cursor, 0.01f);
            Assert.That(tooSmall.Zoom, Is.EqualTo(0.25f));
            AssertClose(tooSmall.WindowToCanvas(cursor), anchor);

            // 滾輪往上（負值）放大，往下縮小，來回一格回到原本的比例。
            Assert.That(UIBlueprintViewport.WheelFactor(-3f), Is.GreaterThan(1f));
            Assert.That(UIBlueprintViewport.WheelFactor(3f), Is.LessThan(1f));
            Assert.That(UIBlueprintViewport.WheelFactor(-3f) * UIBlueprintViewport.WheelFactor(3f),
                Is.EqualTo(1f).Within(1e-5f));

            // 平移：每個畫布上的點在視窗裡移動同樣的距離，比例不變。
            var zoomed = view.ZoomedAround(cursor, 0.5f);
            var panned = zoomed.Panned(new Vector2(30, -12));
            Assert.That(panned.Zoom, Is.EqualTo(0.5f));
            AssertClose(panned.CanvasToWindow(anchor) - zoomed.CanvasToWindow(anchor), new Vector2(30, -12));

            // 滑鼠移動的距離換算成畫布上的距離：除以縮放比例。
            AssertClose(zoomed.WindowToCanvasDelta(new Vector2(100, 40)), new Vector2(200, 80));
            AssertClose(tooFar.WindowToCanvasDelta(new Vector2(100, 40)), new Vector2(50, 20));
            var rect = zoomed.CanvasToWindow(new Rect(10, 20, 200, 100));
            AssertClose(rect.size, new Vector2(100, 50));
            AssertClose(rect.position, zoomed.CanvasToWindow(new Vector2(10, 20)));

            // 顯示全部：範圍完整落在視窗裡並置中；內容很少時不會放大超過 100%。
            var size = new Vector2(900, 600);
            var bounds = new Rect(-500, -300, 3000, 1800);
            var framed = UIBlueprintViewport.Framing(bounds, size, 40);
            var shown = framed.CanvasToWindow(bounds);
            Assert.That(shown.xMin, Is.GreaterThanOrEqualTo(40 - 0.01f));
            Assert.That(shown.yMin, Is.GreaterThanOrEqualTo(40 - 0.01f));
            Assert.That(shown.xMax, Is.LessThanOrEqualTo(size.x - 40 + 0.01f));
            Assert.That(shown.yMax, Is.LessThanOrEqualTo(size.y - 40 + 0.01f));
            AssertClose(shown.center, size * 0.5f);
            Assert.That(UIBlueprintViewport.Framing(new Rect(0, 0, 100, 100), size, 40).Zoom, Is.EqualTo(1f));
            Assert.That(UIBlueprintViewport.Framing(new Rect(0, 0, 90000, 90000), size, 40).Zoom, Is.EqualTo(0.25f));
        }

        [Test]
        public void BlueprintPansZoomsAndShowsAllWithoutTouchingTheScene()
        {
            var scene = NewScene("Batch5 Canvas");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            // 畫布沒有邊界：節點可以放在負的座標，也可以離得很遠。
            UIAuthoring.PositionNode(root, "home", new Vector2(-400, -260));
            UIAuthoring.PositionNode(root, "next", new Vector2(1800, 900));
            SceneManager.SetActiveScene(scene);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            UIBlueprintWindow restored = null;
            try
            {
                // 這個工作階段第一次開啟這個 Scene 的藍圖：自動顯示全部。
                window.CreateGUI();
                AssertAllNodesVisible(window, root);
                Assert.That(window.View.Zoom, Is.LessThan(1f), "節點離得很遠，要縮小才看得到全部");
                Assert.That(window.rootVisualElement.Q<ScrollView>(className: "blueprint-canvas"), Is.Null,
                    "畫布不再是有邊界的捲動區域");

                // 平移：節點在視窗裡的位置跟著移動，儲存的座標不變。
                var home = UIAuthoring.FindEntry(root, "home");
                var stored = home.GraphPosition;
                var before = window.View.CanvasToWindow(stored);
                window.PanBy(new Vector2(120, -45));
                AssertClose(window.View.CanvasToWindow(stored) - before, new Vector2(120, -45));
                Assert.That(home.GraphPosition, Is.EqualTo(stored));

                // 滾輪以游標為中心縮放。
                var cursor = new Vector2(200, 150);
                var anchor = window.View.WindowToCanvas(cursor);
                var zoom = window.View.Zoom;
                window.ZoomAt(cursor, -3f);
                Assert.That(window.View.Zoom, Is.GreaterThan(zoom));
                AssertClose(window.View.WindowToCanvas(cursor), anchor);
                for (var notch = 0; notch < 60; notch++)
                    window.ZoomAt(cursor, -3f);
                Assert.That(window.View.Zoom, Is.EqualTo(2f));
                AssertClose(window.View.WindowToCanvas(cursor), anchor);
                Assert.That(ZoomText(window), Is.EqualTo("200%"));
                for (var notch = 0; notch < 60; notch++)
                    window.ZoomAt(cursor, 3f);
                Assert.That(window.View.Zoom, Is.EqualTo(0.25f));
                AssertClose(window.View.WindowToCanvas(cursor), anchor);
                Assert.That(ZoomText(window), Is.EqualTo("25%"));

                // 內容那一層跟著視角平移與縮放，節點本身用畫布座標排。
                var content = window.rootVisualElement.Q("blueprint-content");
                Assert.That(content.style.scale.value.value.x, Is.EqualTo(0.25f).Within(1e-5f));
                Assert.That(content.style.translate.value.x.value, Is.EqualTo(window.View.pan.x).Within(1e-3f));
                Assert.That(content.style.translate.value.y.value, Is.EqualTo(window.View.pan.y).Within(1e-3f));

                // 工具列：顯示全部，以及點一下回到 100%。
                Assert.That(window.rootVisualElement.Q<ToolbarButton>("frame-all"), Is.Not.Null);
                window.ShowAllNodes();
                AssertAllNodesVisible(window, root);
                window.SetZoom(1f);
                Assert.That(ZoomText(window), Is.EqualTo("100%"));

                // 重建視窗之後視角不變。
                window.SetZoom(0.5f);
                window.PanBy(new Vector2(-64, 18));
                var view = window.View;
                window.CreateGUI();
                AssertSameView(window.View, view);
                Assert.That(ZoomText(window), Is.EqualTo("50%"));

                // 另一個視窗實例（例如 Domain 重新載入之後）從工作階段取回同樣的視角。
                restored = ScriptableObject.CreateInstance<UIBlueprintWindow>();
                restored.CreateGUI();
                AssertSameView(restored.View, view);

                // 只是看藍圖不會弄髒 Scene。
                Assert.That(scene.isDirty, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(window);
                if (restored)
                    Object.DestroyImmediate(restored);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void DraggingANodeMovesItByThePointerDistanceDividedByTheZoom()
        {
            var scene = NewScene("Batch5 Drag");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.PositionNode(root, "home", new Vector2(100, 80));
            SceneManager.SetActiveScene(scene);
            var home = UIAuthoring.FindEntry(root, "home");

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                foreach (var zoom in new[] { 0.5f, 2f, 1f })
                {
                    window.SetZoom(zoom);
                    var start = home.GraphPosition;
                    var grab = window.View.CanvasToWindow(start + new Vector2(20, 12));
                    window.BeginNodeDrag("home", grab);
                    window.DragNodeTo(grab + new Vector2(45, -10));
                    window.DragNodeTo(grab + new Vector2(90, -30));
                    // 拖曳途中節點跟著游標，但還沒有寫回工作區。
                    AssertClose(window.NodeRect("home").position - start, new Vector2(90, -30) / zoom);
                    Assert.That(home.GraphPosition, Is.EqualTo(start));
                    Assert.That(window.EndNodeDrag(), Is.True);
                    AssertClose(home.GraphPosition - start, new Vector2(90, -30) / zoom);
                }

                // 幾乎沒有移動：是點擊，不是拖曳。
                var resting = home.GraphPosition;
                var press = window.View.CanvasToWindow(resting + new Vector2(5, 5));
                window.BeginNodeDrag("home", press);
                window.DragNodeTo(press + new Vector2(1, 1));
                Assert.That(window.EndNodeDrag(), Is.False);
                Assert.That(home.GraphPosition, Is.EqualTo(resting));

                // 沒有「座標不可小於 12」的限制：可以拖到負的座標。
                window.BeginNodeDrag("home", press);
                window.DragNodeTo(press + new Vector2(-900, -700));
                Assert.That(window.EndNodeDrag(), Is.True);
                Assert.That(home.GraphPosition.x, Is.LessThan(0f));
                Assert.That(home.GraphPosition.y, Is.LessThan(0f));

                // 「任何畫面」節點一樣可以拖。
                var anyStart = root.anySourceGraphPosition;
                window.SetZoom(0.5f);
                var anyGrab = window.View.CanvasToWindow(anyStart + new Vector2(8, 8));
                window.BeginNodeDrag(UIRoot.AnySource, anyGrab);
                window.DragNodeTo(anyGrab + new Vector2(50, 50));
                Assert.That(window.EndNodeDrag(), Is.True);
                AssertClose(root.anySourceGraphPosition - anyStart, new Vector2(100, 100));
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void WiresCanBeDroppedOnNodesAtAnyZoom()
        {
            var scene = NewScene("Batch5 Wires");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.PositionNode(root, "home", new Vector2(0, 0));
            UIAuthoring.PositionNode(root, "next", new Vector2(600, 300));
            UIAuthoring.PositionNode(root, "tip", new Vector2(600, -240));
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(0, 500));
            SceneManager.SetActiveScene(scene);

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                window.SetZoom(0.5f);
                window.PanBy(new Vector2(77, 33));

                // 縮放 50%：從 home 的連接點拖到 next 的中央，建立切頁的路由。
                Drag("home", "next");
                var show = root.Routes.Single();
                Assert.That((show.Source, show.Action, show.Kind, show.Target),
                    Is.EqualTo(("home", "open_next", UIRouteKind.ShowPage, "next")));

                // 剛建立的路由還選著：再從同一個連接點拖線是重接這條路由，不是新增。
                // 縮放 200%，拖到 Popup 上：變成開啟 Popup。
                window.SetZoom(2f);
                Drag("home", "tip");
                var rewired = root.Routes.Single();
                Assert.That((rewired.Source, rewired.Action, rewired.Kind, rewired.Target),
                    Is.EqualTo(("home", "open_next", UIRouteKind.OpenPopup, "tip")));

                // 從另一個節點拖出去是新的路由。
                Drag("next", "tip");
                var open = root.Routes.Single(route => route.Source == "next");
                Assert.That((open.Action, open.Kind, open.Target), Is.EqualTo(("open_tip", UIRouteKind.OpenPopup, "tip")));

                // 放在空白的地方：什麼都不會發生。同一個視窗座標在不同的縮放比例下指到畫布上不同的地方，
                // 所以沒有換算的話會放錯位置。
                window.SetZoom(0.5f);
                var nextCenter = window.NodeRect("next").center;
                window.BeginWire("tip", window.View.CanvasToWindow(window.NodeRect("tip").max));
                window.MoveWire(window.View.CanvasToWindow(new Vector2(-900, -900)));
                window.DropWire(window.View.CanvasToWindow(new Vector2(-900, -900)));
                Assert.That(root.Routes.Count, Is.EqualTo(2));
                Assert.That(window.NodeRect("next").Contains(window.View.WindowToCanvas(nextCenter)), Is.False,
                    "把畫布座標直接當成視窗座標用，會指到別的地方");

                // 從「任何畫面」拖出去是訊號路由；沒有線可以連進「任何畫面」。
                Drag(UIRoot.AnySource, "next");
                Assert.That(root.Routes.Count(route => route.IsSignal && route.Target == "next"), Is.EqualTo(1));
                window.BeginWire("home", window.View.CanvasToWindow(window.NodeRect("home").max));
                window.DropWire(window.View.CanvasToWindow(window.NodeRect(UIRoot.AnySource).center));
                Assert.That(root.Routes.Count, Is.EqualTo(3));

                void Drag(string from, string to)
                {
                    var port = window.NodeRect(from);
                    window.BeginWire(from, window.View.CanvasToWindow(new Vector2(port.xMax, port.center.y)));
                    window.MoveWire(window.View.CanvasToWindow(window.NodeRect(to).center));
                    window.DropWire(window.View.CanvasToWindow(window.NodeRect(to).center));
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        // 5-2：範圍框。
        [Test]
        public void FrameSurroundsEveryNodeAndFollowsThem()
        {
            var scene = NewScene("Batch5 Frame");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.PositionNode(root, "home", new Vector2(-120, 40));
            UIAuthoring.PositionNode(root, "next", new Vector2(400, 300));
            UIAuthoring.PositionNode(root, "tip", new Vector2(380, -200));
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(900, 60));
            SceneManager.SetActiveScene(scene);

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                var frame = window.rootVisualElement.Q("blueprint-frame");
                var caption = window.rootVisualElement.Q<Label>("blueprint-frame-caption");
                Assert.That(frame, Is.Not.Null);

                // 只是標示：不接收滑鼠事件，不擋住底下的操作。
                Assert.That(frame.pickingMode, Is.EqualTo(PickingMode.Ignore));
                Assert.That(caption.pickingMode, Is.EqualTo(PickingMode.Ignore));

                // 框住每一個節點（包含「任何畫面」）加上邊距，而且剛好貼著最外圍的節點。
                AssertFrameFits();
                Assert.That(window.FrameRect.xMin, Is.EqualTo(-120 - UIBlueprintWindow.FrameMargin).Within(0.01f));
                Assert.That(window.FrameRect.yMin, Is.EqualTo(-200 - UIBlueprintWindow.FrameMargin).Within(0.01f));
                Assert.That(window.FrameRect.xMax,
                    Is.EqualTo(900 + UIBlueprintWindow.NodeWidth + UIBlueprintWindow.FrameMargin).Within(0.01f));
                Assert.That(caption.text, Is.EqualTo("Batch5 Frame · 2 個頁面 · 1 個 Popup"));

                // 拖曳節點時即時更新，放開之後也一樣。
                window.SetZoom(0.5f);
                var grab = window.View.CanvasToWindow(window.NodeRect("next").center);
                window.BeginNodeDrag("next", grab);
                window.DragNodeTo(grab + new Vector2(0, 400));
                Assert.That(window.FrameRect.yMax,
                    Is.EqualTo(300 + 800 + UIBlueprintWindow.NodeHeight + UIBlueprintWindow.FrameMargin).Within(0.01f));
                AssertFrameFits();
                Assert.That(window.EndNodeDrag(), Is.True);
                AssertFrameFits();

                // 新增畫面。
                UIAuthoring.CreatePage(root, "extra", "Extra");
                UIAuthoring.PositionNode(root, "extra", new Vector2(-900, -700));
                window.CreateGUI();
                frame = window.rootVisualElement.Q("blueprint-frame");
                AssertFrameFits();
                Assert.That(window.FrameRect.xMin, Is.EqualTo(-900 - UIBlueprintWindow.FrameMargin).Within(0.01f));
                Assert.That(window.rootVisualElement.Q<Label>("blueprint-frame-caption").text,
                    Is.EqualTo("Batch5 Frame · 3 個頁面 · 1 個 Popup"));

                // 畫面從藍圖移除之後。
                var serialized = new SerializedObject(root);
                var entries = serialized.FindProperty("entries");
                entries.DeleteArrayElementAtIndex(entries.arraySize - 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                window.CreateGUI();
                frame = window.rootVisualElement.Q("blueprint-frame");
                AssertFrameFits();
                Assert.That(window.FrameRect.xMin, Is.EqualTo(-120 - UIBlueprintWindow.FrameMargin).Within(0.01f));
                Assert.That(window.rootVisualElement.Q<Label>("blueprint-frame-caption").text,
                    Is.EqualTo("Batch5 Frame · 2 個頁面 · 1 個 Popup"));

                // 「顯示全部」連範圍框一起放進視窗。
                window.ShowAllNodes();
                var shown = window.View.CanvasToWindow(window.FrameRect);
                Assert.That(shown.xMin >= 0f && shown.yMin >= 0f && shown.xMax <= window.ViewSize.x &&
                    shown.yMax <= window.ViewSize.y, Is.True, shown.ToString());

                void AssertFrameFits()
                {
                    var rect = window.FrameRect;
                    foreach (var id in root.Entries.Select(entry => entry.Id).Append(UIRoot.AnySource))
                    {
                        var node = window.NodeRect(id);
                        Assert.That(node.xMin - rect.xMin >= UIBlueprintWindow.FrameMargin - 0.01f &&
                            node.yMin - rect.yMin >= UIBlueprintWindow.FrameMargin - 0.01f &&
                            rect.xMax - node.xMax >= UIBlueprintWindow.FrameMargin - 0.01f &&
                            rect.yMax - node.yMax >= UIBlueprintWindow.FrameMargin - 0.01f, Is.True,
                            $"節點「{id}」{node} 沒有被範圍框 {rect} 包住");
                    }
                    // 畫面上的元素和算出來的矩形一致。
                    Assert.That(frame.style.left.value.value, Is.EqualTo(rect.x).Within(0.01f));
                    Assert.That(frame.style.top.value.value, Is.EqualTo(rect.y).Within(0.01f));
                    Assert.That(frame.style.width.value.value, Is.EqualTo(rect.width).Within(0.01f));
                    Assert.That(frame.style.height.value.value, Is.EqualTo(rect.height).Within(0.01f));
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        // 5-3：節點的操作。
        [Test]
        public void NodesAreSelectedAndDraggedFromAnywhereOnThem()
        {
            var scene = NewScene("Batch5 Nodes");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.Connect(root, "home", "ping", UIRouteKind.Emit);
            UIAuthoring.PositionNode(root, "home", new Vector2(0, 0));
            UIAuthoring.PositionNode(root, "next", new Vector2(500, 40));
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(0, 400));
            SceneManager.SetActiveScene(scene);
            var home = UIAuthoring.FindEntry(root, "home");
            var next = UIAuthoring.FindEntry(root, "next");

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                window.SetZoom(0.5f);
                var focused = new System.Collections.Generic.List<string>();
                window.FocusRequested = focused.Add;

                var card = window.rootVisualElement.Q("node-home");
                var title = card.Q<Label>(className: "node-title");
                var id = card.Q<Label>(className: "node-id");
                var port = card.Q(className: "node-port");
                Assert.That(title.text, Is.EqualTo("Home"));
                Assert.That(id.text, Is.EqualTo("home"));
                Assert.That(port, Is.Not.Null);

                // 標題、說明文字、節點的空白處：按下都會選取，拖曳都會移動。
                foreach (var target in new VisualElement[] { title, id, card })
                {
                    window.ClickCanvas();
                    Assert.That(window.SelectedId, Is.Null.Or.Empty);
                    var start = home.GraphPosition;
                    var press = window.View.CanvasToWindow(start + new Vector2(30, 20));
                    window.PressNode("home", target, press);
                    Assert.That(window.SelectedId, Is.EqualTo("home"));
                    Assert.That(card.ClassListContains("selected-node"), Is.True);
                    window.MovePointer(press + new Vector2(40, 30));
                    window.ReleasePointer(press + new Vector2(40, 30));
                    AssertClose(home.GraphPosition - start, new Vector2(80, 60));
                }
                Assert.That(focused, Is.Empty, "單擊與拖曳都不會定位到 Scene");

                // 在連接點按下是拖線，不會移動節點。
                var resting = home.GraphPosition;
                var rect = window.NodeRect("home");
                var portPoint = window.View.CanvasToWindow(new Vector2(rect.xMax, rect.center.y));
                window.PressNode("home", port, portPoint);
                window.MovePointer(portPoint + new Vector2(80, 60));
                Assert.That(window.NodeRect("home").position, Is.EqualTo(resting));
                window.ReleasePointer(window.View.CanvasToWindow(window.NodeRect("next").center));
                Assert.That(home.GraphPosition, Is.EqualTo(resting));
                var route = root.Routes.Single(candidate => candidate.Target == "next");
                Assert.That((route.Source, route.Action, route.Kind), Is.EqualTo(("home", "open_next", UIRouteKind.ShowPage)));

                // 單擊只選取；雙擊才定位。
                window.ClickCanvas();
                var nextPosition = next.GraphPosition;
                var point = window.View.CanvasToWindow(window.NodeRect("next").center);
                window.PressNode("next", null, point);
                window.ReleasePointer(point);
                Assert.That(window.SelectedId, Is.EqualTo("next"));
                Assert.That(focused, Is.Empty);
                window.PressNode("next", null, point, 2);
                window.ReleasePointer(point);
                Assert.That(focused, Is.EqualTo(new[] { "next" }));
                Assert.That(next.GraphPosition, Is.EqualTo(nextPosition));

                // 「任何畫面」不是畫面：雙擊只會選取，沒有東西可以定位。
                var anyPoint = window.View.CanvasToWindow(window.NodeRect(UIRoot.AnySource).center);
                window.PressNode(UIRoot.AnySource, null, anyPoint, 2);
                window.ReleasePointer(anyPoint);
                Assert.That(window.SelectedId, Is.EqualTo(UIRoot.AnySource));
                Assert.That(focused.Count, Is.EqualTo(1));

                // 點空白的地方取消選取。
                window.ClickCanvas();
                Assert.That(window.SelectedId, Is.Null.Or.Empty);
                Assert.That(window.rootVisualElement.Query(className: "selected-node").ToList(), Is.Empty);

                // 線上的動作標籤可以點：選取來源節點，右欄選中這條路由。
                var label = window.EdgeLabel("home", "open_next");
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Is.EqualTo("open_next"));
                Assert.That(label.pickingMode, Is.EqualTo(PickingMode.Position));
                window.PressEdgeLabel(label);
                Assert.That(window.SelectedId, Is.EqualTo("home"));
                Assert.That(window.SelectedAction, Is.EqualTo("open_next"));
                Assert.That(window.rootVisualElement.Q("node-home").ClassListContains("selected-node"), Is.True);
                var action = window.rootVisualElement.Query<TextField>().ToList().First(field => field.label == "動作");
                Assert.That(action.value, Is.EqualTo("open_next"));
                // 不是切頁或開啟 Popup 的路由沒有畫線，也就沒有標籤。
                Assert.That(window.EdgeLabel("home", "ping"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void NodesShowTheirKindAndFlagsAsBadges()
        {
            var scene = NewScene("Batch5 Badges");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.SetNavigatedByCode(root, "tip", true);
            UIAuthoring.Connect(root, "home", "ping", UIRouteKind.Emit);
            UIAuthoring.Connect(root, "next", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            SceneManager.SetActiveScene(scene);

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                // 種類、起始頁、由程式控制各是一個徽章。
                Assert.That(Badges("node-home"), Is.EqualTo(new[] { "頁面", "起始頁" }));
                Assert.That(Badges("node-next"), Is.EqualTo(new[] { "頁面" }));
                Assert.That(Badges("node-tip"), Is.EqualTo(new[] { "Popup", "程式控制" }));
                Assert.That(Badges("any-source-node"), Is.EqualTo(new[] { "訊號" }));
                Assert.That(Node("node-home").ClassListContains("page-node"), Is.True);
                Assert.That(Node("node-tip").ClassListContains("popup-node"), Is.True);
                Assert.That(Node("any-source-node").ClassListContains("signal-node"), Is.True);
                Assert.That(Node("node-tip").Q<Label>(className: "badge-popup"), Is.Not.Null);
                Assert.That(Node("any-source-node").Q<Label>(className: "badge-signal"), Is.Not.Null);

                // 不會拉線的動作列在節點的最下面，用和右欄相同的說法。
                Assert.That(Node("node-home").Q<Label>(className: "node-actions").text, Is.EqualTo("ping（送出事件）"));
                Assert.That(Node("node-next").Q<Label>(className: "node-actions").text, Is.EqualTo("back（返回）"));
                Assert.That(Node("any-source-node").Q<Label>(className: "node-actions").text,
                    Is.EqualTo("tick（送出事件）"));
                Assert.That(Node("any-source-node").Q<Label>(className: "node-title").text,
                    Is.EqualTo(UIBlueprintWindow.AnySourceTitle));

                VisualElement Node(string name) => window.rootVisualElement.Q(name);

                string[] Badges(string name)
                {
                    return Node(name).Query<Label>(className: "badge").ToList().Select(badge => badge.text).ToArray();
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        private static string ZoomText(UIBlueprintWindow window)
        {
            return window.rootVisualElement.Q<ToolbarButton>("zoom-level").text;
        }

        private static void AssertAllNodesVisible(UIBlueprintWindow window, UIRoot root)
        {
            var visible = new Rect(Vector2.zero, window.ViewSize);
            foreach (var id in root.Entries.Select(entry => entry.Id).Append(UIRoot.AnySource))
            {
                var rect = window.View.CanvasToWindow(window.NodeRect(id));
                Assert.That(rect.xMin >= visible.xMin - 0.01f && rect.yMin >= visible.yMin - 0.01f &&
                    rect.xMax <= visible.xMax + 0.01f && rect.yMax <= visible.yMax + 0.01f, Is.True,
                    $"節點「{id}」在視窗裡的範圍是 {rect}，可視範圍是 {visible}");
            }
        }

        private static void AssertSameView(UIBlueprintViewport actual, UIBlueprintViewport expected)
        {
            Assert.That(actual.Zoom, Is.EqualTo(expected.Zoom).Within(1e-5f));
            AssertClose(actual.pan, expected.pan);
        }

        private static void AssertClose(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.01f), $"{actual} 應該是 {expected}");
        }
    }
}
