using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-10：藍圖排版的官方寫法。計畫的三個欄位、公開的 SetWaypoints、自動排列。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void PlanWritesBlueprintLayoutOnlyWhenItIsGiven()
        {
            RequireDOTween();
            var root = WorkspaceWithHomePage("Batch5 Layout Plan");
            var anyNode = root.AnyNodePosition;

            // 新建的畫面與路由：寫了就寫入，沒寫就照預設。
            UIPlan.Apply(@"{
                ""anyNodePosition"": { ""x"": 40, ""y"": 600 },
                ""views"": [
                    { ""id"": ""menu"", ""graphPosition"": { ""x"": 400, ""y"": 120 } },
                    { ""id"": ""shop"" }
                ],
                ""routes"": [
                    { ""source"": ""home"", ""action"": ""toMenu"", ""kind"": ""ShowPage"", ""target"": ""menu"",
                      ""waypoints"": [{ ""x"": 300, ""y"": -50 }, { ""x"": 350, ""y"": -60 }] },
                    { ""source"": ""home"", ""action"": ""toShop"", ""kind"": ""ShowPage"", ""target"": ""shop"" },
                    { ""source"": ""home"", ""action"": ""ping"", ""kind"": ""Emit"" }
                ] }");
            Assert.That(Position("menu"), Is.EqualTo(new Vector2(400, 120)));
            Assert.That(root.AnyNodePosition, Is.Not.EqualTo(anyNode));
            Assert.That(root.AnyNodePosition, Is.EqualTo(new Vector2(40, 600)));
            Assert.That(Route("toMenu").Waypoints, Is.EqualTo(new[] { new Vector2(300, -50), new Vector2(350, -60) }));
            Assert.That(Route("toShop").Waypoints, Is.Empty);
            var shop = Position("shop");

            // 沒寫：不動。replace 的路由沒寫 waypoints 時保留中繼點。
            UIPlan.Apply(@"{
                ""views"": [{ ""id"": ""menu"" }, { ""id"": ""shop"", ""updateExisting"": true }],
                ""routes"": [{ ""source"": ""home"", ""action"": ""toMenu"", ""replace"": true, ""history"": ""Clear"" }] }");
            Assert.That(Route("toMenu").History, Is.EqualTo(UIHistoryMode.Clear), "計畫有套用");
            Assert.That(Route("toMenu").Waypoints.Count, Is.EqualTo(2));
            Assert.That(Position("menu"), Is.EqualTo(new Vector2(400, 120)));
            Assert.That(Position("shop"), Is.EqualTo(shop));
            Assert.That(root.AnyNodePosition, Is.EqualTo(new Vector2(40, 600)));

            // 既有的畫面要設 updateExisting 才寫入；只寫 x 時 y 維持原值。
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""menu"", ""graphPosition"": { ""x"": 0, ""y"": 0 } }] }");
            Assert.That(Position("menu"), Is.EqualTo(new Vector2(400, 120)));
            UIPlan.Apply(@"{ ""views"": [{ ""id"": ""menu"", ""updateExisting"": true, ""graphPosition"": { ""x"": 50 } }] }");
            Assert.That(Position("menu"), Is.EqualTo(new Vector2(50, 120)));
            UIPlan.Apply(@"{ ""anyNodePosition"": { ""y"": 700 } }");
            Assert.That(root.AnyNodePosition, Is.EqualTo(new Vector2(40, 700)));

            // 既有的路由要設 replace 才寫入；[] 清除。
            UIPlan.Apply(@"{ ""routes"": [{ ""source"": ""home"", ""action"": ""toMenu"",
                ""waypoints"": [{ ""x"": 1, ""y"": 1 }] }] }");
            Assert.That(Route("toMenu").Waypoints.Count, Is.EqualTo(2));
            UIPlan.Apply(@"{ ""routes"": [{ ""source"": ""home"", ""action"": ""toMenu"", ""replace"": true,
                ""waypoints"": [{ ""x"": 1, ""y"": 1 }] }] }");
            Assert.That(Route("toMenu").Waypoints, Is.EqualTo(new[] { new Vector2(1, 1) }));
            UIPlan.Apply(@"{ ""routes"": [{ ""source"": ""home"", ""action"": ""toMenu"", ""replace"": true,
                ""waypoints"": [] }] }");
            Assert.That(Route("toMenu").Waypoints, Is.Empty);

            // 寫在沒有線的路由上：套用前報錯，指出路由與欄位，什麼都沒有寫入。
            foreach (var plan in new[]
            {
                @"{ ""routes"": [{ ""source"": ""home"", ""action"": ""ping"", ""replace"": true,
                    ""waypoints"": [{ ""x"": 1, ""y"": 1 }] }] }",
                @"{ ""views"": [{ ""id"": ""later"" }], ""routes"": [{ ""source"": ""menu"", ""action"": ""back"",
                    ""kind"": ""Back"", ""waypoints"": [] }] }",
            })
            {
                var exception = Assert.Throws<ArgumentException>(() => UIPlan.Apply(plan));
                Assert.That(exception.Message, Does.Contain("waypoints"));
                Assert.That(exception.Message, Does.Contain("home/ping").Or.Contain("menu/back"));
            }
            Assert.That(UIAuthoring.FindEntry(root, "later"), Is.Null);
            Assert.That(root.Routes.Any(route => route.Action == "back"), Is.False);

            Vector2 Position(string id) => UIAuthoring.FindEntry(root, id).GraphPosition;
            UIRoute Route(string action) => root.Routes.Single(route => route.Source == "home" && route.Action == action);
        }

        [Test]
        public void SetWaypointsIsUndoableAndChecksTheRoute()
        {
            var root = NewWaypointWorkspace("Batch5 Layout Api", out var scene, out _);
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            Undo.IncrementCurrentGroup();

            IReadOnlyList<Vector2> points = new[] { new Vector2(1, 2), new Vector2(3, 4) };
            UIAuthoring.SetWaypoints(root, "home", "next", points);
            Assert.That(Go().Waypoints, Is.EqualTo(points));
            Assert.That(scene.isDirty, Is.True);
            Undo.PerformUndo();
            Assert.That(Go().Waypoints, Is.Empty);

            // 訊號路由也可以有中繼點；傳入空的清除。
            UIAuthoring.SetWaypoints(root, UIRoot.AnySource, "lost", points);
            Assert.That(root.Routes.Single(route => route.IsSignal && route.Action == "lost").Waypoints.Count, Is.EqualTo(2));
            UIAuthoring.SetWaypoints(root, UIRoot.AnySource, "lost", Array.Empty<Vector2>());
            Assert.That(root.Routes.Single(route => route.IsSignal && route.Action == "lost").Waypoints, Is.Empty);

            // 路由不存在，或沒有線。
            foreach (var (source, action) in new[] { ("home", "absent"), ("home", "ping"), ("next", "back"), ("tip", "ok") })
            {
                var exception = Assert.Throws<ArgumentException>(() => UIAuthoring.SetWaypoints(root, source, action, points),
                    $"{source}/{action}");
                Assert.That(exception.Message, Does.Contain($"{source}/{action}"));
            }

            // 「任何畫面」節點的位置。
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(12, 34));
            Assert.That(root.AnyNodePosition, Is.EqualTo(new Vector2(12, 34)));

            UIRoute Go() => root.Routes.Single(route => route.Source == "home" && route.Action == "next");
        }

        [Test]
        public void ArrangeBlueprintLaysTheFlowOutFromLeftToRight()
        {
            var scene = NewScene("Batch5 Layout Arrange");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var root = UIAuthoring.CreateWorkspace(scene);
            foreach (var id in new[] { "title", "morning", "business", "closing", "dayEnd", "weeklyReview", "ending", "challenger" })
                UIAuthoring.CreatePage(root, id, id);
            UIAuthoring.CreatePopup(root, "picker", "Picker", $"{folder}/Picker.prefab");
            UIAuthoring.SetStartPage(root, "title");
            Connect("title", "newGame", "morning");
            Connect("morning", "open", "business");
            UIAuthoring.Connect(root, "morning", "pick", UIRouteKind.OpenPopup, "picker");
            UIAuthoring.Connect(root, "picker", "close", UIRouteKind.ClosePopup);
            Connect("business", "done", "closing");
            Connect("closing", "toDayEnd", "dayEnd");
            Connect("closing", "toEnding", "ending");
            Connect("dayEnd", "toMorning", "morning");
            Connect("dayEnd", "toWeeklyReview", "weeklyReview");
            Connect("dayEnd", "toEnding", "ending");
            Connect("weeklyReview", "toMorning", "morning");
            Connect("ending", "toTitle", "title");
            // 只由訊號進入的畫面。
            UIAuthoring.Connect(root, UIRoot.AnySource, "challenger", UIRouteKind.ShowPage, "challenger");
            Connect("challenger", "accept", "morning");
            // 亂排，加上幾個中繼點。
            var index = 0;
            foreach (var entry in root.Entries.ToArray())
                UIAuthoring.PositionNode(root, entry.Id, new Vector2(900 - index++ * 97, 333 * (index % 3)));
            UIAuthoring.SetWaypoints(root, "dayEnd", "toMorning", new[] { new Vector2(-500, 900) });
            UIAuthoring.SetWaypoints(root, UIRoot.AnySource, "challenger", new[] { new Vector2(10, 10) });
            Assert.That(EditorSceneManager.SaveScene(scene), Is.True);
            var messy = Positions();

            Undo.IncrementCurrentGroup();
            UIAuthoring.ArrangeBlueprint(root);
            Assert.That(scene.isDirty, Is.True);
            var arranged = Positions();

            // 主要的流程排成由左到右的一列。
            var flow = new[] { "title", "morning", "business", "closing", "dayEnd" };
            for (var step = 1; step < flow.Length; step++)
            {
                Assert.That(arranged[flow[step]].x, Is.GreaterThan(arranged[flow[step - 1]].x), flow[step]);
                Assert.That(arranged[flow[step]].y, Is.EqualTo(arranged["title"].y), flow[step]);
            }
            // 分支在下一欄：週結在日終結算的右邊；同一步的分支在同一欄的下面。
            Assert.That(arranged["weeklyReview"].x, Is.GreaterThan(arranged["dayEnd"].x));
            Assert.That(arranged["ending"].x, Is.EqualTo(arranged["dayEnd"].x));
            Assert.That(arranged["ending"].y, Is.GreaterThan(arranged["dayEnd"].y));
            Assert.That(arranged["picker"].x, Is.EqualTo(arranged["business"].x));
            Assert.That(arranged["picker"].y, Is.GreaterThan(arranged["business"].y));
            // 只由訊號進入的畫面在最右邊。
            Assert.That(arranged["challenger"].x,
                Is.GreaterThan(arranged.Where(pair => pair.Key != "challenger" && pair.Key != UIRoot.AnySource)
                    .Max(pair => pair.Value.x)));
            // 「任何畫面」在左下角。
            Assert.That(arranged[UIRoot.AnySource].x, Is.EqualTo(arranged.Values.Min(position => position.x)));
            Assert.That(arranged[UIRoot.AnySource].y, Is.GreaterThan(arranged
                .Where(pair => pair.Key != UIRoot.AnySource).Max(pair => pair.Value.y) + UIBlueprintWindow.NodeHeight));

            // 沒有節點重疊。
            var rects = arranged.ToDictionary(pair => pair.Key,
                pair => new Rect(pair.Value, new Vector2(UIBlueprintWindow.NodeWidth, UIBlueprintWindow.NodeHeight)));
            foreach (var a in rects)
            {
                foreach (var b in rects.Where(other => string.CompareOrdinal(other.Key, a.Key) > 0))
                    Assert.That(a.Value.Overlaps(b.Value), Is.False, $"{a.Key} 與 {b.Key} 重疊");
            }
            // 中繼點都清除了。
            Assert.That(root.Routes.All(route => route.Waypoints.Count == 0), Is.True);

            // 可以 Undo：回到亂排的樣子，中繼點也回來。
            Undo.PerformUndo();
            Assert.That(Positions(), Is.EqualTo(messy));
            Assert.That(root.Routes.Single(route => route.Action == "toMorning" && route.Source == "dayEnd").Waypoints.Count,
                Is.EqualTo(1));

            // 排兩次結果相同，和原本的位置無關。
            UIAuthoring.ArrangeBlueprint(root);
            UIAuthoring.ArrangeBlueprint(root);
            Assert.That(Positions(), Is.EqualTo(arranged));

            // 工具列的「整理藍圖」：要先確認；Play Mode 中停用。
            UIAuthoring.PositionNode(root, "title", new Vector2(-999, -999));
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                var button = window.rootVisualElement.Q<ToolbarButton>("arrange-blueprint");
                Assert.That(button.text, Is.EqualTo("整理藍圖"));
                Assert.That(button.enabledSelf, Is.True);
                Assert.That(UIBlueprintWindow.ArrangeConfirmMessage, Is.EqualTo("會重新排列所有節點並清除中繼點，可以用 Undo 還原。"));
                Assert.That(window.ArrangeBlueprint(() => false), Is.False);
                Assert.That(Positions()["title"], Is.EqualTo(new Vector2(-999, -999)));
                Assert.That(window.ArrangeBlueprint(() => true), Is.True);
                Assert.That(Positions(), Is.EqualTo(arranged));
                Assert.That(window.NodeRect("title").position, Is.EqualTo(arranged["title"]), "視窗跟著更新");

                window.ShowLiveStateForTests(LiveState("title", new string[0], new string[0]));
                Assert.That(window.rootVisualElement.Q<ToolbarButton>("arrange-blueprint").enabledSelf, Is.False);
                Assert.That(window.ArrangeBlueprint(() => true), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }

            void Connect(string source, string action, string target) =>
                UIAuthoring.Connect(root, source, action, UIRouteKind.ShowPage, target);

            Dictionary<string, Vector2> Positions()
            {
                var positions = root.Entries.ToDictionary(entry => entry.Id, entry => entry.GraphPosition);
                positions[UIRoot.AnySource] = root.AnyNodePosition;
                return positions;
            }
        }
    }
}
