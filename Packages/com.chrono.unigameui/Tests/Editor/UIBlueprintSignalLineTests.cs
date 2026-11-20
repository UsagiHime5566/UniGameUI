using System.Linq;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-7：訊號路由的線另外處理。測的是線的樣式、要不要畫、開關的狀態；畫出來的樣子要人工確認。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void SignalRouteLinesAreFaintDashedAndCanBeHidden()
        {
            var root = NewSidebarWorkspace("Batch5 Signal Lines", out var scene);
            UIAuthoring.PositionNode(root, "home", new Vector2(0, 0));
            UIAuthoring.PositionNode(root, "next", new Vector2(500, 0));
            UIAuthoring.PositionNode(root, "tip", new Vector2(500, 300));
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(0, 300));
            var go = root.Routes.Single(route => route.Source == "home" && route.Action == "next");
            var open = root.Routes.Single(route => route.Source == "home" && route.Action == "openTip");
            var lost = root.Routes.Single(route => route.IsSignal && route.Action == "lost");
            UIBlueprintSession.ShowSignalRoutes = true;
            const double now = 1000;

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            var second = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();

                // 一般路由是實線；訊號路由是比較淡、比較細的虛線，和「任何畫面」節點同色。
                var page = window.EdgeStyle(go, now);
                var signal = window.EdgeStyle(lost, now);
                Assert.That(page.Dashed, Is.False);
                Assert.That(window.EdgeStyle(open, now).Dashed, Is.False);
                Assert.That(signal.Dashed, Is.True);
                Assert.That(signal.Color.a, Is.LessThan(page.Color.a * 0.6f));
                Assert.That(signal.Width, Is.LessThan(page.Width));
                AssertColor(signal.Color, new Color32(240, 179, 90, 255));
                Assert.That(window.EdgesToDraw(now), Is.EquivalentTo(new[] { go, open, lost }), "Emit 的訊號沒有線");
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").ClassListContains("edge-signal"), Is.True);
                Assert.That(window.EdgeLabel("home", "next").ClassListContains("edge-signal"), Is.False);

                // 滑鼠靠近線或移到標籤上：那條線變清楚。
                var middle = window.View.CanvasToWindow(new Vector2((UIBlueprintWindow.NodeWidth + 500) / 2f,
                    300 + UIBlueprintWindow.NodeHeight / 2f));
                Assert.That(window.RouteNear(middle), Is.SameAs(lost));
                Assert.That(window.RouteNear(middle + new Vector2(0, 40)), Is.Null);
                window.SetHoveredRoute(lost);
                Assert.That(window.EdgeStyle(lost, now).Color.a, Is.EqualTo(1f));
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").ClassListContains("edge-emphasized"), Is.True);
                window.SetHoveredRoute(null);
                Assert.That(window.EdgeStyle(lost, now).Color.a, Is.EqualTo(signal.Color.a));
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").ClassListContains("edge-emphasized"), Is.False);

                // 選取「任何畫面」節點時也變清楚。
                PressNodeCenter(window, UIRoot.AnySource);
                Assert.That(window.EdgeStyle(lost, now).Color.a, Is.EqualTo(1f));
                window.ClickCanvas();
                Assert.That(window.EdgeStyle(lost, now).Color.a, Is.EqualTo(signal.Color.a));

                // 關掉：訊號路由的線與標籤都不畫，「任何畫面」節點照常顯示並標出有幾條。
                var toggle = window.rootVisualElement.Q<ToolbarToggle>("show-signals");
                Assert.That(toggle.text, Is.EqualTo("顯示訊號路由"));
                Assert.That(toggle.value, Is.True);
                Assert.That(Hidden(window).style.display.value, Is.EqualTo(DisplayStyle.None));
                window.ShowSignalRoutes = false;
                Assert.That(toggle.value, Is.False);
                Assert.That(window.EdgesToDraw(now), Is.EquivalentTo(new[] { go, open }));
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(window.EdgeLabel("home", "next").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(window.RouteNear(middle), Is.Null);
                Assert.That(window.rootVisualElement.Q("any-source-node"), Is.Not.Null);
                Assert.That(Hidden(window).text, Is.EqualTo("2 條路由（線已隱藏）"));
                Assert.That(Hidden(window).style.display.value, Is.EqualTo(DisplayStyle.Flex));

                // 狀態在重建視窗之後保留，另一個視窗也一樣。
                window.CreateGUI();
                second.CreateGUI();
                foreach (var target in new[] { window, second })
                {
                    Assert.That(target.ShowSignalRoutes, Is.False);
                    Assert.That(target.rootVisualElement.Q<ToolbarToggle>("show-signals").value, Is.False);
                    Assert.That(target.EdgesToDraw(now), Is.EquivalentTo(new[] { go, open }));
                    Assert.That(target.EdgeLabel(UIRoot.AnySource, "lost").style.display.value,
                        Is.EqualTo(DisplayStyle.None));
                }

                // 關掉的時候走了一條訊號路由：短暫畫出來，淡掉之後再收起來。
                window.FlashRoute(lost, now);
                Assert.That(window.EdgesToDraw(now + 0.5), Has.Member(lost));
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(window.EdgeStyle(lost, now + 0.1).Width, Is.GreaterThan(page.Width));
                Assert.That(window.EdgesToDraw(now + 2.5), Has.No.Member(lost));
                window.FlashRoute(go, now + 3);
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").style.display.value, Is.EqualTo(DisplayStyle.None));

                // 打開：回到原本的樣子。
                window.ShowSignalRoutes = true;
                Assert.That(window.EdgesToDraw(now + 10), Is.EquivalentTo(new[] { go, open, lost }));
                Assert.That(window.EdgeLabel(UIRoot.AnySource, "lost").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(Hidden(window).style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                UIBlueprintSession.ShowSignalRoutes = true;
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(second);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }

            Label Hidden(UIBlueprintWindow target) => target.rootVisualElement.Q<Label>("any-source-hidden");
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.01f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.01f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.01f));
        }
    }
}
